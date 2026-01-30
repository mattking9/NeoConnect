

namespace NeoConnect
{
    public interface INeoHubService
    {
        Task<INeoConnection> CreateConnection(CancellationToken cancellationToken);
        Task<Dictionary<int, Profile>> GetAllProfiles(INeoConnection connection, CancellationToken cancellationToken);
        Task<List<NeoDevice>> GetDevices(INeoConnection connection, CancellationToken cancellationToken);
        Task<Dictionary<string, EngineersData>> GetEngineersData(INeoConnection connection, CancellationToken cancellationToken);
        Task<Dictionary<string, int>> GetROCData(INeoConnection connection, string[] devices, CancellationToken cancellationToken);
        ComfortLevel? GetNextComfortLevel(ProfileSchedule schedule, DateTime? relativeTo);
        Task RunRecipe(INeoConnection connection, string recipeName, CancellationToken cancellationToken);
        Task SetPreheatDuration(INeoConnection connection, string zoneName, int maxPreheatDuration, CancellationToken cancellationToken);
        Task Hold(INeoConnection connection, string id, string[] devices, double temp, int hours, CancellationToken cancellationToken);
        Task Boost(INeoConnection connection, string[] devices, int hours, CancellationToken cancellationToken);
    }
}