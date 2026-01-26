using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace NeoConnect
{
    public class ClientWebSocketWrapper : IDisposable
    {        
        private ClientWebSocket _ws;        

        public virtual WebSocketState State => _ws?.State ?? WebSocketState.None;

        public virtual async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {            
            _ws = new ClientWebSocket();
            _ws.Options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            await _ws.ConnectAsync(uri, cancellationToken);
        }

        public virtual async Task CloseAsync(CancellationToken cancellationToken)
        {            
            _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed", cancellationToken);
        }        

        public virtual async Task SendAllAsync(string message, CancellationToken cancellationToken)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            var segment = new ArraySegment<byte>(bytes);

            await _ws.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
        }

        public virtual async Task<string> ReceiveAllAsync(CancellationToken cancellationToken)
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
                    await this.CloseAsync(cancellationToken);
                    break;
                }

                responseJson.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                readComplete = result.EndOfMessage;
            }

            return responseJson.ToString();
        }        

        public virtual void Dispose()
        {
            if (_ws != null)
            {
                _ws.Dispose();
            }
        }
    }
}
