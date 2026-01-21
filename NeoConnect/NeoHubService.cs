using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace NeoConnect
{
    public class NeoHubService : INeoHubService, IDisposable
    {
        private readonly ILogger<NeoHubService> _logger;
        private readonly IConfiguration _config;
        private readonly Uri _uri;
        private readonly string _key;
        private int _inc = 0;

        private ClientWebSocketWrapper _ws;
        private bool disposedValue;

        public NeoHubService()
        {                
        }

        public NeoHubService(ILogger<NeoHubService> logger, IConfiguration config, ClientWebSocketWrapper ws)
        {
            _logger = logger;
            _config = config;
            _ws = ws;

            _uri = _config.GetValue<Uri>("NeoHub:Uri") ?? throw new ArgumentNullException("Config value for NeoHub.Uri is required");
            _key = _config.GetValue<string>("NeoHub:ApiKey") ?? throw new ArgumentNullException("Config value for NeoHub.ApiKey is required");
        }

        public async Task Connect(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Connecting to NeoHub.");            

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
            
            if (_ws.State == WebSocketState.Open)
            {
                try
                {
                    await _ws.CloseAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Incomplete WebSocket disconnection: " + ex.Message);
                }
            }
        }

        public async Task<List<NeoDevice>> GetDevices(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching Devices.");

            await SendMessage("GET_LIVE_DATA", "0", _inc++, cancellationToken);

            var result = await ReceiveMessage(cancellationToken);
            return JsonSerializer.Deserialize(result.ResponseJson, NeoConnectJsonContext.Default.NeoHubLiveData)?.Devices ?? throw new Exception($"Error parsing GET_LIVE_DATA json: {result.ResponseJson}");
        }

        public async Task<Dictionary<string, EngineersData>> GetEngineersData(CancellationToken cancellationToken)
        {
            await SendMessage("GET_ENGINEERS", "0", _inc++, cancellationToken);

            var result = await ReceiveMessage(cancellationToken);
            return JsonSerializer.Deserialize(result.ResponseJson, NeoConnectJsonContext.Default.DictionaryStringEngineersData) ?? throw new Exception($"Error parsing GET_ENGINEERS json: {result.ResponseJson}");
        }

        public async Task<Dictionary<int, Profile>> GetAllProfiles(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching Profiles.");

            await SendMessage("GET_PROFILES", "0", _inc++, cancellationToken);

            var result = await ReceiveMessage(cancellationToken);

            var profiles = JsonSerializer.Deserialize(result.ResponseJson, NeoConnectJsonContext.Default.DictionaryStringProfile) ?? throw new Exception($"Error parsing GET_PROFILES json: {result.ResponseJson}");
            return profiles.ToDictionary(kvp => kvp.Value.ProfileId, kvp => kvp.Value);
        }

        public async Task<Dictionary<string, int>> GetROCData(string[] devices, CancellationToken cancellationToken)
        {
            await SendMessage("VIEW_ROC", $"[{string.Join(',', devices.Select(d => $"'{d}'"))}]", _inc++, cancellationToken);

            var result = await ReceiveMessage(cancellationToken);
            return JsonSerializer.Deserialize(result.ResponseJson, NeoConnectJsonContext.Default.DictionaryStringInt32) ?? throw new Exception($"Error parsing VIEW_ROC json: {result.ResponseJson}");
        }

        public async Task RunRecipe(string recipeName, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Running recipe: {recipeName}.");

            await SendMessage("RUN_RECIPE", $"['{recipeName}']", _inc++, cancellationToken);

            await ReceiveMessage(cancellationToken);
        }

        public async Task SetPreheatDuration(string zoneName, int maxPreheatDuration, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Setting preheat duration for {zoneName} to {maxPreheatDuration} hours.");

            await SendMessage("SET_PREHEAT", $"[{maxPreheatDuration}, '{zoneName}']", _inc++, cancellationToken);

            await ReceiveMessage(cancellationToken);
        }

        public async Task Hold(string id, string[] devices, double temp, int hours, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Holding {string.Join(',', devices)} at {temp}c for {hours} hours.");

            await SendMessage("HOLD", $"[{{'temp': {temp}, 'hours': {hours}, 'minutes': 0, 'id': '{id}'}},[{string.Join(',', devices.Select(d => $"'{d}'"))}]]", _inc++, cancellationToken);

            await ReceiveMessage(cancellationToken);
        }

        public async Task Boost(string[] devices, int hours, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Boosting {string.Join(',', devices)} for {hours} hours.");

            await SendMessage("BOOST_ON", $"[{{'hours': {hours}, 'minutes': 0 }},[{string.Join(',', devices.Select(d => $"'{d}'"))}]]", _inc++, cancellationToken);

            await ReceiveMessage(cancellationToken);
        }

        public ComfortLevel? GetNextComfortLevel(ProfileSchedule schedule, DateTime? relativeTo)
        {
            var date = relativeTo ?? DateTime.Now;
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                // Use weekend schedule
                return GetNextComfortLevel(schedule.Weekends, TimeOnly.FromDateTime(date));
            }
            else
            {
                // Use weekday schedule
                return GetNextComfortLevel(schedule.Weekdays, TimeOnly.FromDateTime(date));
            }
        }

        private ComfortLevel? GetNextComfortLevel(ProfileScheduleGroup scheduleGroup, TimeOnly? relativeTo)
        {
            return new ComfortLevel[] 
            { 
                new ComfortLevel(scheduleGroup.Wake),
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

            if (_ws.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("WebSocket is not connected.");
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Sending Command:\r\n" + message);
            }
            
            await _ws.SendAllAsync(message, cancellationToken);
        }

        private async Task<NeoHubResponse> ReceiveMessage(CancellationToken cancellationToken)
        {            
            var responseJson = await _ws.ReceiveAllAsync(cancellationToken);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Received message:\r\n" + responseJson);
            }

            return JsonSerializer.Deserialize(responseJson, NeoConnectJsonContext.Default.NeoHubResponse) ?? throw new Exception($"Could not parse json: {responseJson}");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _ws.Dispose();
                }
                
                disposedValue = true;
            }
        }        

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}