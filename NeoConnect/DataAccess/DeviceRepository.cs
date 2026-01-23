using Dapper;
using Microsoft.Data.Sqlite;

namespace NeoConnect.DataAccess
{
    public class DeviceRepository
    {
        private const string _connectionString = "Data Source=neoconnect.db";

        //todo: get data per day
        public async Task<IEnumerable<DeviceState>> GetDeviceData(DateTime dateToDisplay)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                return await connection.QueryAsync<DeviceState>("SELECT * FROM DeviceState " +
                                                            $"WHERE DATE(timestamp) >= DATE('{dateToDisplay.ToString("yyyy-MM-dd")}') " +
                                                            $"AND DATE(timestamp) < DATE('{dateToDisplay.ToString("yyyy-MM-dd")}', '+1 day')");
            }
        }

        public void AddDeviceData(IEnumerable<DeviceState> deviceStates)
        {
            using (var connection = new SqliteConnection(_connectionString))
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
        }
    }
}
