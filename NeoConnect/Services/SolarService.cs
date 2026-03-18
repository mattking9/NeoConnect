using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NeoConnect
{
    public class SolarService : ISolarService
    {
        //private static readonly Regex _timeRegex = new Regex(@"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2}) (?<hour>\d{2}):(?<minute>\d{2}):(?<second>\d{2}) (?<timezoneAbbreviation>\w+)(?<offsetSign>[-+])(?<offsetHours>\d{2})(?<offsetMinutes>\d{2})$", RegexOptions.Compiled);

        private readonly ILogger<SolarService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly string _token;
        private readonly string _deviceSerialNr;
        private readonly string _host;

        public SolarService(IConfiguration configuration, ILogger<SolarService> logger, IHttpClientFactory httpClientFactory)
        {
            _token = configuration.GetValue<string>("FoxEssCloud:ApiKey");
            _deviceSerialNr = configuration.GetValue<string>("FoxEssCloud:DeviceSerialNr");
            _host = configuration.GetValue<string>("FoxEssCloud:Host");
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<SolarData> GetRealtimeData()
        {
            var path = "/op/v1/device/real/query";

            using (var client = _httpClientFactory.CreateClient())
            {
                var content = JsonContent.Create(new
                {
                    sns = new string[] {
                        _deviceSerialNr,
                    },
                    variables = new string[] {
                        "generationPower",
                        "feedinPower",
                        "SoC",
                        "loadsPower"
                    }
                });

                _logger.LogDebug("Posting request to foxesscloud for realtime data...");

                var url = new Uri(_host + path);
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var signature = CreateSignature(_token, timestamp, url.AbsolutePath);

                content.Headers.Add("token", _token);
                content.Headers.Add("timestamp", timestamp.ToString());
                content.Headers.Add("signature", signature);
                content.Headers.Add("lang", "en");

                var response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    // Read the response content
                    var responseContent = await response.Content.ReadFromJsonAsync<FoxEssApiResponse>();
                    if (responseContent == null)
                    {
                        throw new Exception("Unable to succesfully deserialize the response into a FoxEssApiResponse object.");
                    }

                    if (_logger.IsEnabled(LogLevel.Trace))
                    {
                        var options = new JsonSerializerOptions
                        {
                            WriteIndented = true
                        };
                        _logger.LogTrace($"Response: {JsonSerializer.Serialize(responseContent, options)}");
                    }

                    if (responseContent.Errno != 0)
                    {
                        throw new HttpRequestException($"Error calling FoxESS Cloud API: ({responseContent.Errno}) {responseContent.Msg}");
                    }

                    var result = responseContent.Result[0];
                    return new SolarData
                    {
                        //Timestamp = ParseDateTime(result.Time),
                        GeneratedPower = result.Datas.Find(d => d.Variable == "generationPower").Value,
                        FeedInPower = result.Datas.Find(d => d.Variable == "feedinPower").Value,
                        SoC = result.Datas.Find(d => d.Variable == "SoC").Value,
                        Load = result.Datas.Find(d => d.Variable == "loadsPower").Value,                        
                    };
                }
                else
                {
                    throw new Exception("Request failed with status code: " + response.StatusCode);
                }
            }
        }

        private string CreateSignature(string token, long timestamp, string path)
        {
            var signature = $@"{path}\r\n{_token}\r\n{timestamp}";
            //create a hash of the signature string using the MD5 algorithm
            return CreateHash(signature);
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

        //public static DateTime ParseDateTime(string value)
        //{
        //    var match = _timeRegex.Match(value);
        //    if (match.Success)
        //    {
        //        int year = int.Parse(match.Groups["year"].Value);
        //        int month = int.Parse(match.Groups["month"].Value);
        //        int day = int.Parse(match.Groups["day"].Value);
        //        int hour = int.Parse(match.Groups["hour"].Value);
        //        int minute = int.Parse(match.Groups["minute"].Value);
        //        int second = int.Parse(match.Groups["second"].Value);

        //        return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local);
        //    }
        //    else throw new FormatException("The time string is not in the expected format.");
        //}
    }
}