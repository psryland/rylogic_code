using System;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rylogic.Utility;

namespace ExchApi.Common
{
	public class ExchangeApi :IDisposable, IExchangeApi
	{
		// Notes:
		//  - Common code for all exchange APIs
		//  - No common web sockets support, since every exchange seems to do it differently

		protected ExchangeApi(string key, string secret, CancellationToken shutdown, double request_rate, string base_address, string wss_address)
		{
			// Initialise the main thread id, assuming this object is constructed in the main thread
			Debug.Assert(Misc.AssertMainThread());

			Key = key;
			Secret = secret;
			Shutdown = shutdown;
			RequestThrottle = new RequestThrottle(request_rate);
			UrlRestAddress = base_address;
			UrlSocketAddress = wss_address;
			Client = new HttpClient();
			Sync = SynchronizationContext.Current ?? throw new NullReferenceException("No synchronisation context available");
		}
		public virtual void Dispose()
		{
			Client = null!;
		}

		/// <summary>Async initialisation</summary>
		public virtual Task InitAsync()
		{
			return Task.CompletedTask;
		}

		/// <summary>API key</summary>
		protected string Key { get; }

		/// <summary>API secret</summary>
		protected string Secret { get; }

		/// <summary></summary>
		public string UrlRestAddress { get; }

		/// <summary></summary>
		public string UrlSocketAddress { get; }

		/// <summary>Blocking method for throttling requests</summary>
		public RequestThrottle RequestThrottle { get; }

		/// <summary>Shutdown token</summary>
		public CancellationToken Shutdown { get; }

		/// <summary>The Http client for REST requests</summary>
		protected HttpClient Client
		{
			get;
			private set
			{
				if (field == value) return;
				if (field != null)
				{
					Util.Dispose(ref field!);
				}
				field = value;
				if (field != null)
				{
					field.BaseAddress = new Uri(UrlRestAddress);
					field.Timeout = TimeSpan.FromSeconds(10);
				}
			}
		} = null!;

		/// <summary>For marshalling to the main thread</summary>
		public SynchronizationContext Sync { get; }
	}
	public class ExchangeApi<THasher> :ExchangeApi where THasher : HMAC
	{
		protected ExchangeApi(string key, string secret, CancellationToken shutdown, double request_rate, string base_address, string wss_address)
			:base(key, secret, shutdown, request_rate, base_address, wss_address)
		{
			Hasher = Secret != null
				? Util<THasher>.New(Encoding.ASCII.GetBytes(Secret))
				: Util<THasher>.New();
		}

		/// <summary>Hasher</summary>
		protected THasher Hasher { get; }
	}
}
