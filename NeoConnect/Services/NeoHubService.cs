using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace NeoConnect
{
    public class NeoHubService : INeoHubService
    {
        private readonly ILogger<NeoHubService> _logger;
        private readonly IConfiguration _config;
        private readonly INeoConnectionFactory _neoConnectionFactory;

        private readonly Uri _uri;
        private readonly string _key;
        private int _inc = 0;        

        public NeoHubService(ILogger<NeoHubService> logger, IConfiguration config, INeoConnectionFactory neoConnectionFactory)
        {
            _logger = logger;
            _config = config;
            _neoConnectionFactory = neoConnectionFactory;

            _uri = _config.GetValue<Uri>("NeoHub:Uri") ?? throw new ArgumentNullException("Config value for NeoHub.Uri is required");
            _key = _config.GetValue<string>("NeoHub:ApiKey") ?? throw new ArgumentNullException("Config value for NeoHub.ApiKey is required");
        }

        public async Task<INeoConnection> CreateConnection(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Connecting to NeoHub.");

            var connection = _neoConnectionFactory.Create();

            try
            {                
                await connection.ConnectAsync(_uri, cancellationToken);

                _logger.LogInformation("Connected.");

                return connection;
            }
            catch (Exception)
            {
                _logger.LogError($"Error connecting to NeoHub at {_uri}.");
                throw;
            }
        }        

        public async Task<List<NeoDevice>> GetDevices(INeoConnection connection, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching Devices.");

            await SendMessage(connection, "GET_LIVE_DATA", "0", _inc++, cancellationToken);

            var result = await ReceiveMessage(connection, cancellationToken);
            return JsonSerializer.Deserialize(result.ResponseJson, NeoConnectJsonContext.Default.NeoHubLiveData)?.Devices ?? throw new Exception($"Error parsing GET_LIVE_DATA json: {result.ResponseJson}");
        }

        public async Task<Dictionary<string, EngineersData>> GetEngineersData(INeoConnection connection, CancellationToken cancellationToken)
        {
            await SendMessage(connection, "GET_ENGINEERS", "0", _inc++, cancellationToken);

            var result = await ReceiveMessage(connection, cancellationToken);
            return JsonSerializer.Deserialize(result.ResponseJson, NeoConnectJsonContext.Default.DictionaryStringEngineersData) ?? throw new Exception($"Error parsing GET_ENGINEERS json: {result.ResponseJson}");
        }

        public async Task<Dictionary<int, Profile>> GetAllProfiles(INeoConnection connection, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching Profiles.");

            await SendMessage(connection, "GET_PROFILES", "0", _inc++, cancellationToken);

            var result = await ReceiveMessage(connection, cancellationToken);

            var profiles = JsonSerializer.Deserialize(result.ResponseJson, NeoConnectJsonContext.Default.DictionaryStringProfile) ?? throw new Exception($"Error parsing GET_PROFILES json: {result.ResponseJson}");
            return profiles.ToDictionary(kvp => kvp.Value.ProfileId, kvp => kvp.Value);
        }

        public async Task<Dictionary<string, int>> GetROCData(INeoConnection connection, string[] devices, CancellationToken cancellationToken)
        {
            await SendMessage(connection, "VIEW_ROC", FormatDeviceArray(devices), _inc++, cancellationToken);

            var result = await ReceiveMessage(connection, cancellationToken);
            return JsonSerializer.Deserialize(result.ResponseJson, NeoConnectJsonContext.Default.DictionaryStringInt32) ?? throw new Exception($"Error parsing VIEW_ROC json: {result.ResponseJson}");
        }

        public async Task RunRecipe(INeoConnection connection, string recipeName, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Running recipe: {recipeName}.");

            await SendMessage(connection, "RUN_RECIPE", $"['{recipeName}']", _inc++, cancellationToken);

            await ReceiveMessage(connection, cancellationToken);
        }

        public async Task SetPreheatDuration(INeoConnection connection, string zoneName, int maxPreheatDuration, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Setting preheat duration for {zoneName} to {maxPreheatDuration} hours.");

            await SendMessage(connection, "SET_PREHEAT", $"[{maxPreheatDuration}, '{zoneName}']", _inc++, cancellationToken);

            await ReceiveMessage(connection, cancellationToken);
        }

        public async Task Hold(INeoConnection connection, string id, string[] devices, double temp, int hours, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Holding {string.Join(',', devices)} at {temp}c for {hours} hours.");

            await SendMessage(connection, "HOLD", $"[{{'temp': {temp}, 'hours': {hours}, 'minutes': 0, 'id': '{id}'}},{ FormatDeviceArray(devices) }]", _inc++, cancellationToken);

            await ReceiveMessage(connection, cancellationToken);
        }

        public async Task Boost(INeoConnection connection, string[] devices, int hours, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Boosting {string.Join(',', devices)} for {hours} hours.");

            await SendMessage(connection, "BOOST_ON", $"[{{'hours': {hours}, 'minutes': 0 }},[{string.Join(',', devices.Select(d => $"'{d}'"))}]]", _inc++, cancellationToken);

            await ReceiveMessage(connection, cancellationToken);
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

        private static string FormatDeviceArray(string[] devices)
        {
            if (devices.Length == 0) return "[]";
            if (devices.Length == 1) return $"['{devices[0]}']";

            // Use StringBuilder for better performance
            var sb = new System.Text.StringBuilder(devices.Length * 20);
            sb.Append('[');

            for (int i = 0; i < devices.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('\'').Append(devices[i]).Append('\'');
            }

            sb.Append(']');
            return sb.ToString();
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

        private async Task SendMessage(INeoConnection connection, string commandName, string commandValue, int commandId, CancellationToken cancellationToken)
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

            if (connection.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("WebSocket is not connected.");
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Sending Command:\r\n" + message);
            }
            
            await connection.SendAllAsync(message, cancellationToken);
        }

        private async Task<NeoHubResponse> ReceiveMessage(INeoConnection connection, CancellationToken cancellationToken)
        {            
            var responseJson = await connection.ReceiveAllAsync(cancellationToken);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Received message:\r\n" + responseJson);
            }

            return JsonSerializer.Deserialize(responseJson, NeoConnectJsonContext.Default.NeoHubResponse) ?? throw new Exception($"Could not parse json: {responseJson}");
        }
    }
}