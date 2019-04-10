using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ExchApi.Common;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json.Linq;
using Rylogic.Utility;

namespace Bittrex.API.Subscriptions
{
	public class BittrexWebSocket : IDisposable
	{
		public BittrexWebSocket(string address)
		{
			// Create connection to c2 SignalR hub
			HubConnection = new HubConnection(address);
			HubProxy = HubConnection.CreateHubProxy("c2");
			HubConnection.Start().Wait();
		}
		public virtual void Dispose()
		{
			HubConnection = null;
		}

		/// <summary>The socket wrapper</summary>
		private HubConnection HubConnection
		{
			get { return m_hub_connection; }
			set
			{
				if (m_hub_connection == value) return;
				if (m_hub_connection != null)
				{
					m_hub_connection.Stop();
					m_hub_connection.Error -= HandleError;
					Util.Dispose(ref m_hub_connection);
				}
				m_hub_connection = value;
				if (m_hub_connection != null)
				{
					m_hub_connection.Error += HandleError;
				}

				// Handlers
				void HandleError(Exception ex)
				{
					BittrexApi.Log(ELogLevel.Error, ex.Message);
				}
			}
		}
		private HubConnection m_hub_connection;

		/// <summary>The SignalR interface</summary>
		public IHubProxy HubProxy { get; }

		/// <summary>Authenticate the connection</summary>
		public async Task Authenticate(string apikey, string secret)
		{
			BittrexApi.Log(ELogLevel.Debug, "BittrexWebSocket: Authentication starting");

			// Get the challenge string
			var challenge = await HubProxy.Invoke<string>("GetAuthContext", apikey);

			// Get hash by using secret as key, and challenge as data
			var hasher = new HMACSHA512(Encoding.ASCII.GetBytes(secret));
			var hash = hasher.ComputeHash(Encoding.ASCII.GetBytes(challenge));
			var signed = Misc.ToStringHex(hash);

			// Pass the signed data to the Authenticate call
			await HubProxy.Invoke<bool>("Authenticate", apikey, signed);

			BittrexApi.Log(ELogLevel.Debug, "BittrexWebSocket: Authentication complete");
		}

		/// <summary>Called whenever market data updates are received</summary>
		public event Action<JToken> MarketDataUpdate
		{
			add
			{
				// Add a callback on the first subscription
				if (m_market_data_update == null)
					m_market_data_update_sub = HubProxy.On<string>(CallbackCode.Market, x => m_market_data_update.Invoke(Decode(x)));

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
