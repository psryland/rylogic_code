using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ExchApi.Common;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json.Linq;
using Rylogic.Utility;

namespace Bittrex.API.Subscriptions
{
	public class BittrexWebSocket : IDisposable
	{
		// Notes:
		//  - Bittrex only support Aspnet.SignalR, not Aspnetcore.SignalR

		public BittrexWebSocket(string address, CancellationToken shutdown)
		{
			Shutdown = shutdown;
			HubConnection = new HubConnection(address);
			HubProxy = HubConnection.CreateHubProxy("c2");
		}
		public virtual void Dispose()
		{
			HubConnection = null;
		}
		public async Task InitAsync()
		{
			// Create connection to c2 SignalR hub
			await HubConnection.Start();
		}

		/// <summary>Shutdown token</summary>
		public CancellationToken Shutdown { get; }

		/// <summary>The socket wrapper</summary>
		private HubConnection HubConnection
		{
			get { return m_hub_connection; }
			set
			{
				if (m_hub_connection == value) return;
				if (m_hub_connection != null)
				{
					if (m_market_data_update_sub != null ||
						m_wallet_update_sub != null ||
						m_order_update_sub != null)
						throw new Exception("Subscriptions should be disposed first");

					m_hub_connection.Closed -= HandleClosed;
					m_hub_connection.Error -= HandleError;
					//m_hub_connection.Stop(); // This blocks for some reason :-(
					//Util.Dispose(ref m_hub_connection); // This blocks for some reason :-(
				}
				m_hub_connection = value;
				if (m_hub_connection != null)
				{
					m_hub_connection.Error += HandleError;
					m_hub_connection.Closed += HandleClosed;
				}

				// Handlers
				async void HandleClosed()
				{
					// Attempt to reconnect if the connection is dropped unexpectedly
					if (Shutdown.IsCancellationRequested) return;
					await Task.Delay(new Random().Next(0, 5) * 1000, Shutdown);
					await m_hub_connection.Start();
				}
				void HandleError(Exception ex)
				{
					BittrexApi.Log.Write(ELogLevel.Error, ex.Message);
				}
			}
		}
		private HubConnection m_hub_connection;

		/// <summary>The SignalR interface</summary>
		public IHubProxy HubProxy { get; }

		/// <summary>Authenticate the connection</summary>
		public async Task Authenticate(string apikey, string secret)
		{
			BittrexApi.Log.Write(ELogLevel.Debug, "BittrexWebSocket: Authentication starting");

			// Get the challenge string
			var challenge = await HubProxy.Invoke<string>("GetAuthContext", apikey);

			// Get hash by using secret as key, and challenge as data
			var hasher = new HMACSHA512(Encoding.ASCII.GetBytes(secret));
			var hash = hasher.ComputeHash(Encoding.ASCII.GetBytes(challenge));
			var signed = Misc.ToStringHex(hash);

			// Pass the signed data to the Authenticate call
			await HubProxy.Invoke<bool>("Authenticate", apikey, signed);

			BittrexApi.Log.Write(ELogLevel.Debug, "BittrexWebSocket: Authentication complete");
		}

		/// <summary>Called whenever market data updates are received</summary>
		public event Action<JToken> MarketDataUpdate
		{
			add
			{
				// Add a callback on the first subscription
				if (m_market_data_update == null)
					m_market_data_update_sub = HubProxy.On<string>(CallbackCode.Market, x => m_market_data_update?.Invoke(Decode(x)));

				m_market_data_update += value;
			}
			remove
			{
				m_market_data_update -= value;

				// Remove the callback when the last subscription is removed
				if (m_market_data_update == null)
					Util.Dispose(ref m_market_data_update_sub);
			}
		}
		private event Action<JToken> m_market_data_update;
		private IDisposable m_market_data_update_sub;

		/// <summary>Subscribe for wallet data updates</summary>
		public event Action<JToken> WalletUpdate
		{
			add
			{
				// Register callback for 'uB' (balance status change) events
				if (m_wallet_update == null)
					m_wallet_update_sub = HubProxy.On<string>(CallbackCode.Balances, x => m_wallet_update.Invoke(Decode(x)));

				m_wallet_update += value;
			}
			remove
			{
				m_wallet_update -= value;

				// Un-register callback
				if (m_wallet_update == null)
					Util.Dispose(ref m_wallet_update_sub);
			}
		}
		private event Action<JToken> m_wallet_update;
		private IDisposable m_wallet_update_sub;

		/// <summary>Subscribe for wallet data updates</summary>
		public event Action<JToken> OrderUpdate
		{
			add
			{
				// Register callback for uO (order status change) events
				if (m_order_update == null)
					m_order_update_sub = HubProxy.On<string>(CallbackCode.Orders, x => m_order_update.Invoke(Decode(x)));

				m_order_update += value;
			}
			remove
			{
				m_order_update -= value;

				// Un-register callback
				if (m_order_update == null)
					Util.Dispose(ref m_order_update_sub);
			}
		}
		private event Action<JToken> m_order_update;
		private IDisposable m_order_update_sub;

		/// <summary>Request a full snapshot to initialise the order book</summary>
		public async Task<JToken> QueryExchangeState(string market_name)
		{
			Shutdown.ThrowIfCancellationRequested();
			var snapshot = await HubProxy.Invoke<string>("QueryExchangeState", market_name);
			return Decode(snapshot);
		}

		/// <summary>Add a subscription for updates to 'market_name'</summary>
		public async Task SubscribeToExchangeDeltas(string market_name)
		{
			if (await HubProxy.Invoke<bool>("SubscribeToExchangeDeltas", market_name)) return;
			throw new Exception($"{nameof(BittrexWebSocket)}: '{nameof(SubscribeToExchangeDeltas)}' failed");
		}

		/// <summary>Decompress data received over the socket</summary>
		private JToken Decode(string data)
		{
			// Decode converts Bittrex CoreHub2 socket wire protocol data into JSON.
			// Data goes from base64 encoded to 'gzip' (byte[]) to 'minifed' JSON.

			// Base64 decode the data into a 'gzip' blob
			var gzip_data = Convert.FromBase64String(data);

			// Decompress the 'gzip' blob into 'minified JSON'
			using (var gzip_stream = new MemoryStream(gzip_data))
			using (var data_stream = new DeflateStream(gzip_stream, CompressionMode.Decompress))
			using (var sr = new StreamReader(data_stream))
				return JToken.Parse(sr.ReadToEnd());
		}

		/// <summary>Codes for callback functions</summary>
		private static class CallbackCode
		{
			public const string Orders = "uO";
			public const string Balances = "uB";
			public const string Market = "uE";
		}
	}
}
