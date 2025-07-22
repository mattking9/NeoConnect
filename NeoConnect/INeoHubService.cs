
namespace NeoConnect
{
    public interface INeoHubService
    {
        Task Connect(CancellationToken cancellationToken);
        Task Disconnect(CancellationToken cancellationToken);
        Task<List<Profile>> GetAllProfiles(CancellationToken cancellationToken);
        Task<List<NeoDevice>> GetDevices(CancellationToken cancellationToken);
        Task<Dictionary<string, EngineersData>> GetEngineersData(CancellationToken cancellationToken);
        Task RunRecipe(string recipeName, CancellationToken cancellationToken);
        Task SetPreheatDuration(string zoneName, int maxPreheatDuration, CancellationToken cancellationToken);
    }
}