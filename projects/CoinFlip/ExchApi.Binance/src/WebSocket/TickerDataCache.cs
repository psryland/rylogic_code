using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Binance.API.DomainObjects;
using ExchApi.Common.JsonConverter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rylogic.Extn;
using Rylogic.Utility;
using WebSocketSharp;

namespace Binance.API
{
	public class TickerDataCache :IDisposable
	{
		public TickerDataCache(BinanceApi api)
		{
			Api = api;
		}
		public void Dispose()
		{
			Socket = null;
		}
		public async Task InitAsync()
		{
			m_ticker = await Api.GetTicker();
			m_ticker.Sort(x => x.Pair.Id);

			// Create the socket
			Socket = new WebSocket(EndPoint);
			Socket.Connect();
			await Task.CompletedTask;
		}

		/// <summary>The owning API instance</summary>
		private BinanceApi Api { get; }

		/// <summary>Account balance data</summary>
		public List<Ticker> AllTickers() // Worker thread context
		{
			lock (m_ticker)
				return new List<Ticker>(m_ticker);
		}
		private List<Ticker> m_ticker;

		/// <summary>Return the spot price for the given pair</summary>
		public Ticker this[CurrencyPair pair] // Worker thread context
		{
			get
			{
				lock (m_ticker)
				{
					var sym = pair.Id;
					var idx = m_ticker.BinarySearch(x => x.Pair.Id.CompareTo(sym));
					return idx >= 0 ? new Ticker(m_ticker[idx]) : null;
				}
			}
		}

		/// <summary>The Url endpoint for the user data stream</summary>
		private string EndPoint => $"{Api.UrlSocketAddress}ws/!ticker@arr";

		/// <summary></summary>
		private WebSocket Socket
		{
			get { return m_socket; }
			set
			{
				if (m_socket == value) return;
				if (m_socket != null)
				{
					m_socket.OnClose -= HandleClosed;
					m_socket.OnError -= HandleError;
					m_socket.OnMessage -= HandleMessage;
					m_socket.OnOpen -= HandleOpened;
					Util.Dispose(ref m_socket);
				}
				m_socket = value;
				if (m_socket != null)
				{
					m_socket.OnOpen += HandleOpened;
					m_socket.OnMessage += HandleMessage;
					m_socket.OnError += HandleError;
					m_socket.OnClose += HandleClosed;
				}

				// Handlers
				void HandleOpened(object sender, EventArgs e)
				{
					BinanceApi.Log.Write(ELogLevel.Debug, $"WebSocket stream opened for ticker data");
				}
				void HandleClosed(object sender, CloseEventArgs e)
				{
					BinanceApi.Log.Write(ELogLevel.Debug, $"WebSocket stream closed for ticker data");
					Socket = null;
				}
				void HandleError(object sender, ErrorEventArgs e)
				{
					BinanceApi.Log.Write(ELogLevel.Error, e.Exception, $"WebSocket stream error for ticker data");
					Socket = null;
					return;
				}
				void HandleMessage(object sender, MessageEventArgs e)
				{
					ApplyUpdate(JToken.Parse(e.Data).ToObject<List<TickerUpdate>>());
				}
			}
		}
		private WebSocket m_socket;

		/// <summary></summary>
		private void ApplyUpdate(List<TickerUpdate> updates)
		{
			lock (m_ticker)
			{
				foreach (var update in updates)
				{
					var sym = update.Ticker.Pair.Id;
					var idx = m_ticker.BinarySearch(x => x.Pair.Id.CompareTo(sym));
					if (idx >= 0)
						m_ticker[idx] = update.Ticker;
					else
						m_ticker.Insert(~idx, update.Ticker);
				}
			}
		}

		/// <summary></summary>
		private class TickerUpdate
		{
			public TickerUpdate()
			{
				Ticker = new Ticker();
			}
			
			/// <summary></summary>
			public Ticker Ticker { get; }

			[JsonProperty("e")]
			public string EventType { get; set; }

			[JsonProperty("E"), JsonConverter(typeof(UnixMSToDateTimeOffset))]
			public DateTimeOffset EventTime { get; set; }

			[JsonProperty("s")]
			private string PairInternal { set => Ticker.Pair = CurrencyPair.Parse(value); }

			[JsonProperty("p")]
			private double PriceChange { set => Ticker.PriceChange = value; }

			[JsonProperty("P")]
			private double PriceChangePercent { set => Ticker.PriceChangePercent = value; }

			[JsonProperty("w")]
			private double WeightedAvgPrice { set => Ticker.WeightedAvgPrice = value; }

			[JsonProperty("x")]
			private double PrevClosePrice { set => Ticker.PrevClosePrice = value; }

			[JsonProperty("c")]
			private double LastPrice { set => Ticker.LastPrice = value; }

			[JsonProperty("Q")]
			private double LastQty { set => Ticker.LastQty = value; }

			[JsonProperty("b")]
			private double PriceB2Q { set => Ticker.PriceB2Q = value; }

			[JsonProperty("a")]
			private double PriceQ2B { set => Ticker.PriceQ2B = value; }

			[JsonProperty("B")]
			public double B2QVolumeQuote { get; set; }

			[JsonProperty("A")]
			public double Q2BVolumeQuote { get; set; }

			[JsonProperty("o")]
			private double OpenPrice { set => Ticker.OpenPrice = value; }

			[JsonProperty("h")]
			private double HighPrice { set => Ticker.HighPrice = value; }

			[JsonProperty("l")]
			private double LowPrice { set => Ticker.LowPrice = value; }

			[JsonProperty("v")]
			private double VolumeBase { set => Ticker.VolumeBase = value; }

			[JsonProperty("q")]
			private double VolumeQuote { set => Ticker.VolumeQuote = value; }

			[JsonProperty("openTime"), JsonConverter(typeof(UnixMSToDateTimeOffset))]
			public DateTimeOffset OpenTime { set => Ticker.OpenTime = value; }

			[JsonProperty("closeTime"), JsonConverter(typeof(UnixMSToDateTimeOffset))]
			public DateTimeOffset CloseTime { set => Ticker.CloseTime = value; }

			[JsonProperty("F")]
			private long FirstTradeId { set => Ticker.FirstTradeId = value; }

			[JsonProperty("L")]
			private long LastTradeId { set => Ticker.LastTradeId = value; }

			[JsonProperty("n")]
			private long TradeCount { set => Ticker.TradeCount = value; }
		}
	}
}
