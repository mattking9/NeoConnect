namespace NeoConnect
{
    public interface IImmersionService
    {
        Task<bool> TurnOffDevice();
        Task<bool> TurnOnDevice();
    }
}