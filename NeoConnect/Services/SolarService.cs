using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NeoConnect
{
    public class SolarService : ISolarService
    {        
        private readonly ILogger<SolarService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly string _token;
        private readonly string _deviceSerialNr;
        private readonly string _host;

        // Solar Variables
        private const string GenerationPower = "generationPower";
        private const string FeedinPower = "feedinPower";
        private const string SoC = "SoC";
        private const string LoadPower = "loadsPower";

        public SolarService(IConfiguration configuration, ILogger<SolarService> logger, IHttpClientFactory httpClientFactory)
        {
            _token = configuration.GetValue<string>("FoxEssCloud:ApiKey");
            _deviceSerialNr = configuration.GetValue<string>("FoxEssCloud:DeviceSerialNr");
            _host = configuration.GetValue<string>("FoxEssCloud:Host");
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<SolarData> GetRealtimeData(CancellationToken stoppingToken)
        {
            var path = "/op/v1/device/real/query";
            var body = new
            {
                sns = new string[] { _deviceSerialNr },
                variables = new string[] { GenerationPower, FeedinPower, SoC, LoadPower }
            };

            var responseContent = await SendFoxEssRequestAsync<List<ResultType>>(path, body, stoppingToken);
            var result = responseContent.Result[0];
            return new SolarData
            {
                GeneratedPower = result.Datas.Find(d => d.Variable == GenerationPower).Value,
                FeedInPower = result.Datas.Find(d => d.Variable == FeedinPower).Value,
                SoC = result.Datas.Find(d => d.Variable == SoC).Value,
                Load = result.Datas.Find(d => d.Variable == LoadPower).Value,
                Timestamp = DateTime.Parse(result.Time.Substring(0, 19))
            };
        }

        public async Task SetForceChargeWindow(int startHour, int startMinute, int endHour, int endMinute, int targetSoc, CancellationToken stoppingToken)
        {
            var path = "/op/v3/device/scheduler/enable";

            var body = new
            {
                deviceSN = _deviceSerialNr,
                groups = new object[]
                {
                    new
                    {
                        startHour,
                        startMinute,
                        endHour,
                        endMinute,
                        workMode = "ForceCharge",
                        extraParam = new
                        {
                            fdSoc = targetSoc
                        }
                    }
                }
            };

            var responseContent = await SendFoxEssRequestAsync<object>(path, body, stoppingToken);
        }

        private string CreateSignature(string token, long timestamp, string path)
        {
            // Use CRLF between parts and the supplied token
            var signature = $"{path}\r\n{token}\r\n{timestamp}";
            //create a hash of the signature string using the MD5 algorithm
            return CreateHash(signature);
        }

        private async Task<FoxEssApiResponse<T>> SendFoxEssRequestAsync<T>(string path, object body, CancellationToken cancellationToken) where T : class
        {
            using (var client = _httpClientFactory.CreateClient())
            {
                var content = JsonContent.Create(body);

                _logger.LogDebug($"Posting request to foxesscloud: {path}...");

                var url = new Uri(_host + path);
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var signature = CreateSignature(_token, timestamp, url.AbsolutePath);

                content.Headers.Add("token", _token);
                content.Headers.Add("timestamp", timestamp.ToString());
                content.Headers.Add("signature", signature);
                content.Headers.Add("lang", "en");

                var response = await client.PostAsync(url, content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception("Request failed with status code: " + response.StatusCode);
                }

                var responseContent = await response.Content.ReadFromJsonAsync<FoxEssApiResponse<T>>(cancellationToken: cancellationToken);
                if (responseContent == null)
                {
                    throw new Exception("Unable to succesfully deserialize the response into a FoxEssApiResponse object.");
                }

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    _logger.LogDebug($"FoxESS Response: {JsonSerializer.Serialize(responseContent, options)}");
                }

                if (responseContent.Errno != 0)
                {
                    throw new HttpRequestException($"Error calling FoxESS Cloud API: ({responseContent.Errno}) {responseContent.Msg}");
                }

                return responseContent;
            }
        }

        private string CreateHash(string inputString)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputString);

            // Create an instance of the MD5 algorithm
            using (MD5 md5 = MD5.Create())
            {
                // Compute the hash value from the input bytes
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to a hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }

                // Output the MD5 hash
                return sb.ToString();
            }
        }        
    }
}