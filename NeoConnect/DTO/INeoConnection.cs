
using System.Net.WebSockets;

namespace NeoConnect
{
    public interface INeoConnection : IDisposable
    {
        WebSocketState State { get; }

        Task ConnectAsync(Uri uri, CancellationToken cancellationToken);
        Task<string> ReceiveAllAsync(CancellationToken cancellationToken);
        Task SendAllAsync(string message, CancellationToken cancellationToken);
    }
}