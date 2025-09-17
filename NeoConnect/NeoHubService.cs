using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace NeoConnect
{
    public class NeoHubService : INeoHubService
    {
        private readonly ILogger<NeoHubService> _logger;
        private readonly IConfiguration _config;
        private readonly Uri _uri;
        private readonly string _key;

        private ClientWebSocket _ws;

        public NeoHubService()
        {                
        }

        public NeoHubService(ILogger<NeoHubService> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;

            _uri = _config.GetValue<Uri>("NeoHub:Uri") ?? throw new ArgumentNullException("Config value for NeoHub.Uri is required");
            _key = _config.GetValue<string>("NeoHub:ApiKey") ?? throw new ArgumentNullException("Config value for NeoHub.ApiKey is required");
        }

        public async Task Connect(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Connecting to NeoHub.");

            _ws = new ClientWebSocket();
            _ws.Options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            try
            {
                await _ws.ConnectAsync(_uri, cancellationToken);

                _logger.LogInformation("Connected.");

                return;
            }
            catch (Exception)
            {
                _logger.LogError($"Error connecting to NeoHub at {_uri}. Connection State was {_ws.State}");
                throw;
            }
        }

        public async Task Disconnect(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Closing NeoHub connection.");

            if (_ws == null)
            {
                return;
            }

            if (_ws.State == WebSocketState.Open)
            {
                try
                {
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed", cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Incomplete WebSocket disconnection: " + ex.Message);
                }
            }
            _ws.Dispose();
            _ws = null;
        }

        public async Task<List<NeoDevice>> GetDevices(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching Devices.");

            await SendMessage("GET_LIVE_DATA", "0", 1, cancellationToken);

            var result = await ReceiveMessage(cancellationToken);
            return JsonSerializer.Deserialize<NeoHubLiveData>(result.ResponseJson)?.Devices ?? throw new Exception($"Error parsing GET_LIVE_DATA json: {result.ResponseJson}");
        }

        public async Task<Dictionary<string, EngineersData>> GetEngineersData(CancellationToken cancellationToken)
        {
            await SendMessage("GET_ENGINEERS", "0", 3, cancellationToken);

            var result = await ReceiveMessage(cancellationToken);
            return JsonSerializer.Deserialize<Dictionary<string, EngineersData>>(result.ResponseJson) ?? throw new Exception($"Error parsing GET_ENGINEERS json: {result.ResponseJson}");
        }

        public async Task<Dictionary<int, Profile>> GetAllProfiles(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching Profiles.");

            await SendMessage("GET_PROFILES", "0", 2, cancellationToken);

            var result = await ReceiveMessage(cancellationToken);

            var profiles = JsonSerializer.Deserialize<Dictionary<string, Profile>>(result.ResponseJson) ?? throw new Exception($"Error parsing GET_PROFILES json: {result.ResponseJson}");
            return profiles.ToDictionary(kvp => kvp.Value.ProfileId, kvp => kvp.Value);
        }

        public async Task RunRecipe(string recipeName, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Running recipe: {recipeName}.");

            await SendMessage("RUN_RECIPE", $"['{recipeName}']", 4, cancellationToken);

            await ReceiveMessage(cancellationToken);

        }

        public async Task SetPreheatDuration(string zoneName, int maxPreheatDuration, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Setting preheat duration for {zoneName} to {maxPreheatDuration} hours.");

            await SendMessage("SET_PREHEAT", $"[{maxPreheatDuration}, '{zoneName}']", 5, cancellationToken);

            await ReceiveMessage(cancellationToken);

        }

        public ComfortLevel? GetNextSwitchingInterval(ProfileSchedule schedule, DateTime? relativeTo)
        {
            var date = relativeTo ?? DateTime.Now;
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                // Use weekend schedule
                return GetNextSwitchingInterval(schedule.Weekends, TimeOnly.FromDateTime(date));
            }
            else
            {
                // Use weekday schedule
                return GetNextSwitchingInterval(schedule.Weekdays, TimeOnly.FromDateTime(date));
            }
        }

        private ComfortLevel? GetNextSwitchingInterval(ProfileScheduleGroup scheduleGroup, TimeOnly? relativeTo)
        {
            return new ComfortLevel[] 
            { 
                new ComfortLevel(scheduleGroup.Wake, true),
                new ComfortLevel(scheduleGroup.Leave),
                new ComfortLevel(scheduleGroup.Return),
                new ComfortLevel(scheduleGroup.Sleep)
            }
            .OrderBy(i => i.Time)
            .FirstOrDefault(i => i.Time > relativeTo);
        }

        private async Task SendMessage(string commandName, string commandValue, int commandId, CancellationToken cancellationToken)
        {
            var message = $@"
            {{
                ""message_type"":""hm_get_command_queue"",
                ""message"":
                ""{{
                    \""token\"":\""{_key}\"",
                    \""COMMANDS\"":
                    [
                        {{
                            \""COMMAND\"":\""{{'{commandName}':{commandValue}}}\"",
                            \""COMMANDID\"":{commandId}
                        }}
                    ]
                }}""
            }}";

            if (_ws == null || _ws.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("WebSocket is not connected.");
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Sending Command:\r\n" + message);
            }

            var bytes = Encoding.UTF8.GetBytes(message);
            var segment = new ArraySegment<byte>(bytes);
            await _ws.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
        }

        private async Task<NeoHubResponse> ReceiveMessage(CancellationToken cancellationToken)
        {
            if (_ws == null || _ws.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("WebSocket is not connected.");
            }

            var buffer = new byte[1024];
            var segment = new ArraySegment<byte>(buffer);

            var readComplete = false;
            var responseJson = new StringBuilder();

            while (!readComplete)
            {
                var result = await _ws.ReceiveAsync(segment, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
                    break;
                }

                responseJson.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                readComplete = result.EndOfMessage;
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Received message:\r\n" + responseJson);
            }

            return JsonSerializer.Deserialize<NeoHubResponse>(responseJson.ToString()) ?? throw new Exception($"Could not parse json: {responseJson}");
        }
    }
}
