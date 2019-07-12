using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace ExchApi.Common
{
	public interface IExchangeApi
	{
		/// <summary>Async initialisation of the API</summary>
		Task InitAsync();

		/// <summary>Shutdown token</summary>
		CancellationToken Shutdown { get; }

		/// <summary>Blocking method for throttling requests</summary>
		RequestThrottle RequestThrottle { get; }
	}
}
