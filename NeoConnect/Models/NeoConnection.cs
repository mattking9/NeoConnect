using System.Net.WebSockets;
using System.Text;

namespace NeoConnect
{
    public class NeoConnectionFactory : INeoConnectionFactory
    {        
        public INeoConnection Create()
        {
            return new NeoConnection();
        }
    }

    public class NeoConnection : IDisposable, INeoConnection
    {
        private ClientWebSocket _ws;

        public WebSocketState State => _ws?.State ?? WebSocketState.None;

        public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            _ws = new ClientWebSocket();
            _ws.Options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            await _ws.ConnectAsync(uri, cancellationToken);
        }

        public async Task SendAllAsync(string message, CancellationToken cancellationToken)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            var segment = new ArraySegment<byte>(bytes);

            await _ws.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
        }

        public async Task<string> ReceiveAllAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[1024];
            var segment = new ArraySegment<byte>(buffer);

            var readComplete = false;
            var responseJson = new StringBuilder();

            while (!readComplete)
            {
                var result = await _ws.ReceiveAsync(segment, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                responseJson.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                readComplete = result.EndOfMessage;
            }

            return responseJson.ToString();
        }

        public void Dispose()
        {
            if (_ws != null)
            {
                _ws.Dispose();
            }
        }
    }
}
