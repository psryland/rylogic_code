using System.Net.WebSockets;
using System.Threading;

namespace ExchApi.Common
{
	public interface IExchangeApi
	{
		/// <summary>Shutdown token</summary>
		CancellationToken Shutdown { get; }

		/// <summary>Blocking method for throttling requests</summary>
		RequestThrottle RequestThrottle { get; }

		/// <summary>The web socket client</summary>
		ClientWebSocket WebSocket { get; }
	}
}
