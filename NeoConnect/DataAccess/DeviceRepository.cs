using Dapper;
using Microsoft.Data.Sqlite;

namespace NeoConnect.DataAccess
{
    public class DeviceRepository
    {
        private readonly ILogger<DeviceRepository> _logger;
        private readonly IConfiguration _config;

        private const string _connectionString = "Data Source=./data/neoconnect.db";

        public DeviceRepository(ILogger<DeviceRepository> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        //todo: get data per day
        public async Task<IEnumerable<DeviceState>> GetDeviceData(DateTime dateToDisplay)
        {
            using (var connection = new SqliteConnection(_config.GetConnectionString("Default") ?? _connectionString))
            {
                try
                {
                    return await connection.QueryAsync<DeviceState>("SELECT * FROM DeviceState " +
                                                                $"WHERE DATE(timestamp) >= DATE('{dateToDisplay.ToString("yyyy-MM-dd")}') " +
                                                                $"AND DATE(timestamp) < DATE('{dateToDisplay.ToString("yyyy-MM-dd")}', '+1 day')");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error Fetching Device State Data");
                    throw;
                }
            }
        }

        public void AddDeviceData(IEnumerable<DeviceState> deviceStates)
        {
            using (var connection = new SqliteConnection(_config.GetConnectionString("Default") ?? _connectionString))
            {
                try
                {
                    const string sql = "INSERT INTO DeviceState (DeviceId, SetTemp, ActualTemp, HeatOn, PreheatActive, OutsideTemp, Timestamp) " +
                                   "VALUES (@DeviceId, @SetTemp, @ActualTemp, @HeatOn, @PreheatActive, @OutsideTemp, @Timestamp)";

                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        connection.Execute(sql, deviceStates, transaction);
                        transaction.Commit();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error Adding Device State Data");
                    throw;
                }
            }
        }
    }
}
