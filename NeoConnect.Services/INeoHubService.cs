

namespace NeoConnect.Services
{
    public interface INeoHubService
    {
        Task Connect(CancellationToken cancellationToken);
        Task Disconnect(CancellationToken cancellationToken);
        Task<Dictionary<int, Profile>> GetAllProfiles(CancellationToken cancellationToken);
        Task<List<NeoDevice>> GetDevices(CancellationToken cancellationToken);
        Task<Dictionary<string, EngineersData>> GetEngineersData(CancellationToken cancellationToken);
        Task<Dictionary<string, int>> GetROCData(string[] devices, CancellationToken cancellationToken);
        ComfortLevel? GetNextSwitchingInterval(ProfileSchedule schedule, DateTime? relativeTo);
        Task RunRecipe(string recipeName, CancellationToken cancellationToken);
        Task SetPreheatDuration(string zoneName, int maxPreheatDuration, CancellationToken cancellationToken);
    }
}