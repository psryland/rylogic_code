using System.Security.Authentication;

namespace Binance.API
{
	public class WebSocket :WebSocketSharp.WebSocket
	{
		public WebSocket(string endpoint)
			: base(endpoint)
		{
			SslConfiguration.EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;
		}
	}
}
