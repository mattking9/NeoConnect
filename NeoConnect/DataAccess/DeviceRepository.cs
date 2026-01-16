using Dapper;
using Microsoft.Data.Sqlite;

namespace NeoConnect.DataAccess
{
    public class DeviceRepository
    {        
        private const string _connectionString = "Data Source=neoconnect.db";        

        //todo: get data per day
        public List<DeviceState> GetDeviceData()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                return connection.Query<DeviceState>("SELECT * FROM DeviceState").ToList();
            }
        }

        public void AddDeviceData(IEnumerable<DeviceState> deviceStates)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                foreach (var deviceState in deviceStates)
                {
                    connection.Execute(
                    "INSERT INTO DeviceState (DeviceId, SetTemp, ActualTemp, HeatOn, PreheatActive, OutsideTemp, Timestamp) " +
                    "VALUES (@DeviceId, @SetTemp, @ActualTemp, @HeatOn, @PreheatActive, @OutsideTemp, @Timestamp)",
                    deviceState);
                }
            }
        }
    }
}
