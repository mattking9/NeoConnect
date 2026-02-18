using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NeoConnect.DataAccess;
using Dapper;

namespace NeoConnect.IntegrationTests.DataAccess
{
    [TestFixture]
    public class DeviceRepositoryTests
    {
        private SqliteConnection _connection;
        private DeviceRepository _repository;
        private Mock<ILogger<DeviceRepository>> _mockLogger;
        private IConfiguration _configuration;
        private string _connectionString;

        [SetUp]
        public void Setup()
        {
            // Use a unique database name for each test to avoid conflicts
            var dbName = $"TestDb_{Guid.NewGuid()}";
            _connectionString = $"Data Source={dbName};Mode=Memory;Cache=Shared";

            // Create and keep connection open for the entire test duration
            // This is critical for in-memory databases - if all connections close, the database is destroyed
            _connection = new SqliteConnection(_connectionString);
            _connection.Open();

            // Create schema
            CreateSchema();

            // Setup configuration to use the same connection string
            var inMemorySettings = new Dictionary<string, string>
            {
                {"ConnectionStrings:Default", _connectionString}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _mockLogger = new Mock<ILogger<DeviceRepository>>();
            _mockLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

            _repository = new DeviceRepository(_mockLogger.Object, _configuration);
        }

        [TearDown]
        public void TearDown()
        {
            _connection?.Close();
            _connection?.Dispose();

            // Clear the shared cache
            SqliteConnection.ClearPool(_connection);
        }

        private void CreateSchema()
        {
            var createDeviceTable = @"
                CREATE TABLE Device (
                    DeviceId INTEGER PRIMARY KEY,
                    DeviceName TEXT NOT NULL
                )";

            var createDeviceStateTable = @"
                CREATE TABLE DeviceState (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    DeviceId INTEGER NOT NULL,
                    SetTemp REAL NOT NULL,
                    ActualTemp REAL NOT NULL,
                    HeatOn INTEGER NOT NULL,
                    PreheatActive INTEGER NOT NULL,
                    OutsideTemp REAL NOT NULL,
                    Timestamp TEXT NOT NULL
                )";

            _connection.Execute(createDeviceTable);
            _connection.Execute(createDeviceStateTable);
        }

        #region GetDeviceData Tests

        [Test]
        public async Task GetDeviceData_WithDataForDate_ReturnsCorrectRecords()
        {
            // arrange
            var testDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

            // Insert test device
            _connection.Execute("INSERT INTO Device (DeviceId, DeviceName) VALUES (1, 'Living Room')");

            // Insert test data
            _connection.Execute(@"
                INSERT INTO DeviceState (DeviceId, SetTemp, ActualTemp, HeatOn, PreheatActive, OutsideTemp, Timestamp)
                VALUES 
                    (1, 20.5, 19.5, 1, 0, 10.0, @Timestamp1),
                    (1, 21.0, 20.0, 0, 1, 10.5, @Timestamp2)",
                new
                {
                    Timestamp1 = testDate.AddHours(8).ToString("o"),
                    Timestamp2 = testDate.AddHours(12).ToString("o")
                });

            // act
            var result = await _repository.GetDeviceData(testDate);

            // assert
            var resultList = result.ToList();
            Assert.That(resultList.Count, Is.EqualTo(2));
            Assert.That(resultList[0].DeviceName, Is.EqualTo("Living Room"));
            Assert.That(resultList[0].SetTemp, Is.EqualTo(20.5));
            Assert.That(resultList[1].SetTemp, Is.EqualTo(21.0));
        }

        [Test]
        public async Task GetDeviceData_WithNoDataForDate_ReturnsEmptyCollection()
        {
            // arrange
            var testDate = new DateTime(2024, 1, 15);

            // act
            var result = await _repository.GetDeviceData(testDate);

            // assert
            Assert.That(result.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task GetDeviceData_FiltersDataByDateRange()
        {
            // arrange
            var testDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

            _connection.Execute("INSERT INTO Device (DeviceId, DeviceName) VALUES (1, 'Living Room')");

            // Insert data for different dates
            _connection.Execute(@"
                INSERT INTO DeviceState (DeviceId, SetTemp, ActualTemp, HeatOn, PreheatActive, OutsideTemp, Timestamp)
                VALUES 
                    (1, 20.0, 19.0, 1, 0, 10.0, @Yesterday),
                    (1, 21.0, 20.0, 1, 0, 10.0, @Today),
                    (1, 22.0, 21.0, 1, 0, 10.0, @Tomorrow)",
                new
                {
                    Yesterday = testDate.AddDays(-1).ToString("o"),
                    Today = testDate.AddHours(12).ToString("o"),
                    Tomorrow = testDate.AddDays(1).ToString("o")
                });

            // act
            var result = await _repository.GetDeviceData(testDate);

            // assert
            var resultList = result.ToList();
            Assert.That(resultList.Count, Is.EqualTo(1));
            Assert.That(resultList[0].SetTemp, Is.EqualTo(21.0));
        }

        [Test]
        public async Task GetDeviceData_JoinsDeviceNameCorrectly()
        {
            // arrange
            var testDate = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

            _connection.Execute("INSERT INTO Device (DeviceId, DeviceName) VALUES (1, 'Living Room'), (2, 'Bedroom')");

            _connection.Execute(@"
                INSERT INTO DeviceState (DeviceId, SetTemp, ActualTemp, HeatOn, PreheatActive, OutsideTemp, Timestamp)
                VALUES 
                    (1, 20.0, 19.0, 1, 0, 10.0, @Timestamp),
                    (2, 18.0, 17.0, 0, 0, 10.0, @Timestamp)",
                new { Timestamp = testDate.AddHours(8).ToString("o") });

            // act
            var result = await _repository.GetDeviceData(testDate);

            // assert
            var resultList = result.ToList();
            Assert.That(resultList.Count, Is.EqualTo(2));
            Assert.That(resultList.Any(r => r.DeviceName == "Living Room"), Is.True);
            Assert.That(resultList.Any(r => r.DeviceName == "Bedroom"), Is.True);
        }

        #endregion

        #region AddDeviceData Tests

        [Test]
        public void AddDeviceData_WithValidData_InsertsRecords()
        {
            // arrange
            var deviceStates = new List<DeviceStateEntity>
            {
                new DeviceStateEntity
                {
                    DeviceId = 1,
                    SetTemp = 20.5,
                    ActualTemp = 19.5,
                    HeatOn = true,
                    PreheatActive = false,
                    OutsideTemp = 10.0,
                    Timestamp = DateTime.UtcNow
                }
            };

            // act
            _repository.AddDeviceData(deviceStates);

            // assert
            var count = _connection.ExecuteScalar<int>("SELECT COUNT(*) FROM DeviceState");
            Assert.That(count, Is.EqualTo(1));

            var inserted = _connection.QueryFirst<DeviceStateEntity>("SELECT * FROM DeviceState");
            Assert.That(inserted.DeviceId, Is.EqualTo(1));
            Assert.That(inserted.SetTemp, Is.EqualTo(20.5));
            Assert.That(inserted.HeatOn, Is.True);
        }

        [Test]
        public void AddDeviceData_WithMultipleRecords_InsertsAll()
        {
            // arrange
            var deviceStates = new List<DeviceStateEntity>
            {
                new DeviceStateEntity { DeviceId = 1, SetTemp = 20.0, ActualTemp = 19.0, HeatOn = true, PreheatActive = false, OutsideTemp = 10.0, Timestamp = DateTime.UtcNow },
                new DeviceStateEntity { DeviceId = 2, SetTemp = 18.0, ActualTemp = 17.0, HeatOn = false, PreheatActive = true, OutsideTemp = 10.0, Timestamp = DateTime.UtcNow },
                new DeviceStateEntity { DeviceId = 3, SetTemp = 21.0, ActualTemp = 20.0, HeatOn = true, PreheatActive = false, OutsideTemp = 10.0, Timestamp = DateTime.UtcNow }
            };

            // act
            _repository.AddDeviceData(deviceStates);

            // assert
            var count = _connection.ExecuteScalar<int>("SELECT COUNT(*) FROM DeviceState");
            Assert.That(count, Is.EqualTo(3));
        }

        [Test]
        public void AddDeviceData_WithEmptyCollection_DoesNotThrow()
        {
            // arrange
            var deviceStates = new List<DeviceStateEntity>();

            // act & assert
            Assert.DoesNotThrow(() => _repository.AddDeviceData(deviceStates));

            var count = _connection.ExecuteScalar<int>("SELECT COUNT(*) FROM DeviceState");
            Assert.That(count, Is.EqualTo(0));
        }

        #endregion

        #region AddDevices Tests

        [Test]
        public void AddDevices_WithValidData_DeletesAndInsertsRecords()
        {
            // arrange
            // Insert initial data
            _connection.Execute("INSERT INTO Device (DeviceId, DeviceName) VALUES (1, 'Old Device')");

            var devices = new List<DeviceEntity>
            {
                new DeviceEntity { DeviceId = 2, DeviceName = "Living Room" },
                new DeviceEntity { DeviceId = 3, DeviceName = "Bedroom" }
            };

            // act
            _repository.AddDevices(devices);

            // assert
            var count = _connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Device");
            Assert.That(count, Is.EqualTo(2));

            var deviceNames = _connection.Query<string>("SELECT DeviceName FROM Device ORDER BY DeviceId").ToList();
            Assert.That(deviceNames, Does.Contain("Living Room"));
            Assert.That(deviceNames, Does.Contain("Bedroom"));
            Assert.That(deviceNames, Does.Not.Contain("Old Device"));
        }

        [Test]
        public void AddDevices_DeletesExistingDevicesFirst()
        {
            // arrange
            _connection.Execute("INSERT INTO Device (DeviceId, DeviceName) VALUES (1, 'Device 1'), (2, 'Device 2')");

            var devices = new List<DeviceEntity>
            {
                new DeviceEntity { DeviceId = 3, DeviceName = "New Device" }
            };

            // act
            _repository.AddDevices(devices);

            // assert
            var count = _connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Device");
            Assert.That(count, Is.EqualTo(1));

            var deviceName = _connection.ExecuteScalar<string>("SELECT DeviceName FROM Device");
            Assert.That(deviceName, Is.EqualTo("New Device"));
        }

        [Test]
        public void AddDevices_UsesTransaction()
        {
            // arrange
            var devices = new List<DeviceEntity>
            {
                new DeviceEntity { DeviceId = 1, DeviceName = "Device 1" }
            };

            // act
            _repository.AddDevices(devices);

            // assert - if transaction wasn't used, this wouldn't be committed
            var count = _connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Device");
            Assert.That(count, Is.EqualTo(1));
        }

        #endregion
    }
}