using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NeoConnect
{
    public class ImmersionService : IImmersionService
    {
        private readonly ILogger<ImmersionService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        
        private readonly string _accessId;
        private readonly string _accessSecret;
        private readonly string _deviceId;
        private readonly string _tuyaHost;

        public ImmersionService(IConfiguration configuration, ILogger<ImmersionService> logger, IHttpClientFactory httpClientFactory)
        {
            _accessId = configuration.GetValue<string>("Tuya:AccessId");
            _accessSecret = configuration.GetValue<string>("Tuya:AccessSecret");
            _deviceId = configuration.GetValue<string>("Tuya:DeviceId");
            _tuyaHost = configuration.GetValue<string>("Tuya:Host");
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task TurnOnDevice(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Turning ON Immersion");
            await TurnSwitchOnOrOff(true, stoppingToken);
        }

        public async Task TurnOffDevice(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Turning OFF Immersion");
            await TurnSwitchOnOrOff(false, stoppingToken);
        }

        private async Task<string> GetAccessToken(HttpClient client, CancellationToken stoppingToken)
        {
            var path = "/v1.0/token?grant_type=1";
            var method = "GET";
            var timestamp = GetTimestamp();

            var sign = SignRequest(
                _accessId,
                _accessSecret,
                timestamp,
                method,
                path,
                ""
            );

            var request = new HttpRequestMessage(HttpMethod.Get, _tuyaHost + path);
            request.Headers.Add("client_id", _accessId);
            request.Headers.Add("sign", sign);
            request.Headers.Add("t", timestamp);
            request.Headers.Add("sign_method", "HMAC-SHA256");

            var response = await client.SendAsync(request, stoppingToken);
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement
                      .GetProperty("result")
                      .GetProperty("access_token")
                      .GetString()!;
        }

        private async Task TurnSwitchOnOrOff(bool state, CancellationToken stoppingToken)
        {
            using var client = _httpClientFactory.CreateClient();

            var accessToken = await GetAccessToken(client, stoppingToken);

            var path = $"/v1.0/devices/{_deviceId}/commands";
            var method = "POST";
            var body = JsonSerializer.Serialize(new
            {
                commands = new[]
                {
                    new { code = "switch_1", value = state }
                }
            });

            var timestamp = GetTimestamp();
            var sign = SignRequest(
                _accessId,
                _accessSecret,
                timestamp,
                method,
                path,
                body,
                accessToken
            );

            var request = new HttpRequestMessage(HttpMethod.Post, _tuyaHost + path);
            request.Headers.Add("client_id", _accessId);
            request.Headers.Add("access_token", accessToken);
            request.Headers.Add("sign", sign);
            request.Headers.Add("t", timestamp);
            request.Headers.Add("sign_method", "HMAC-SHA256");

            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request, stoppingToken);

            if (response.IsSuccessStatusCode)
            {
                // Read the response content
                var responseContent = await response.Content.ReadFromJsonAsync<TuyaApiResponse>();
                if (responseContent == null)
                {
                    throw new Exception("Unable to succesfully deserialize the response into a TuyaApiResponse object.");
                }                

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    _logger.LogDebug($"Tuya Response: {JsonSerializer.Serialize(responseContent, options)}");
                }

                if (!responseContent.Success)
                {
                    throw new HttpRequestException($"Failed to turn {(state ? "ON" : "OFF")} Immersion switch. Message: {responseContent.Msg}.");
                }
            }
            else
            {
                throw new Exception("Tuya API request failed with status code: " + response.StatusCode);
            }

        }

        static string GetTimestamp()
            => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        static string SignRequest(
            string clientId,
            string secret,
            string timestamp,
            string method,
            string path,
            string body,
            string? accessToken = null)
        {
            var contentHash = Sha256Hex(body);
            var stringToSign =
                method + "\n" +
                contentHash + "\n\n" +
                path;

            var signInput = clientId +
                            (accessToken ?? "") +
                            timestamp +
                            stringToSign;

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signInput));
            return BitConverter.ToString(hash).Replace("-", "").ToUpper();
        }

        static string Sha256Hex(string input)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}