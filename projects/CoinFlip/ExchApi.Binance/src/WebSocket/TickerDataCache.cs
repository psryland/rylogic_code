using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Binance.API.DomainObjects;
using ExchApi.Common;
using ExchApi.Common.JsonConverter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rylogic.Extn;
using Rylogic.Net;
using Rylogic.Utility;

namespace Binance.API
{
	public sealed class TickerDataCache :IDisposable
	{
		// Notes:
		//  - This is the simplest example of a websocket stream. The design is
		//    A cache of ticker data that contains an instance of a stream object
		//    that handles the interaction with the WebSocket. Clients access data
		//    in the cache which lazily creates the stream object. If a socket error
		//    occurs, the stream object disposes itself. The next client access will
		//    create a new stream object.
		//  - The general flow of a stream object is to take a snapshot of the data,
		//    connect the web socket, and handle messages to keep the data up to date.
		//  - There shouldn't be any need for a "watchdog" mechanism, each stream object
		//    should be able to tell when its connection is in a bad state and dispose
		//    itself. Reconnection isn't necessary, the client triggers that with the
		//    next access.

		public TickerDataCache(BinanceApi api)
		{
			Api = api;
			Streams = new Dictionary<int, TickerStream>();
		}
		public void Dispose()
		{
			Util.DisposeRange(Streams.Values);
			Streams.Clear();
		}

		/// <summary>The owning API instance</summary>
		private BinanceApi Api { get; }

		/// <summary>Container of ticker stream instances</summary>
		private Dictionary<int, TickerStream> Streams { get; set; }

		/// <summary>Return the spot price for the given pair</summary>
		public Ticker this[CurrencyPair pair] // Worker thread context
		{
			get
			{
				const int key = 0;
				try
				{
					var stream = (TickerStream)null;
					lock (Streams)
					{
						if (!Streams.TryGetValue(key, out stream) || stream.Socket == null)
							stream = Streams[key] = new TickerStream(this);
					}
					lock (stream.TickerData)
					{
						var sym = pair.Id;
						//Debug.Assert(stream.TickerData.IsOrdered(x => x.Pair.Id.CompareTo(sym)));
						var idx = stream.TickerData.BinarySearch(x => x.Pair.Id.CompareTo(sym));
						return idx >= 0 ? new Ticker(stream.TickerData[idx]) : null;
					}
				}
				catch (Exception ex)
				{
					BinanceApi.Log.Write(ELogLevel.Error, ex, $"Subscribing to ticker data failed.");
					lock (Streams) Streams.Remove(key);
					return null;
				}
			}
		}

		/// <summary>Ticker data</summary>
		private class TickerStream :IDisposable
		{
			public TickerStream(TickerDataCache owner)
			{
				try
				{
					Cache = owner;

					// Request a snapshot to initialise the ticker data
					TickerData = Api.GetTicker().Result;
					TickerData.Sort(x => x.Pair.Id);

					// Create the socket
					Socket = new WebSocket(Api.Shutdown);
				}
				catch
				{
					Dispose();
					throw;
				}
			}
			public void Dispose()
			{
				Socket = null;
			}

			/// <summary>The owning cache</summary>
			private TickerDataCache Cache { get; }

			/// <summary></summary>
			private BinanceApi Api => Cache.Api;

			/// <summary>The ticker data, sorted by pair</summary>
			public List<Ticker> TickerData { get; }

			/// <summary>The Url endpoint for the user data stream</summary>
			private string EndPoint => $"{Api.UrlSocketAddress}ws/!ticker@arr";

			/// <summary></summary>
			public WebSocket Socket
			{
				get => m_socket;
				private set
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
						using (Task_.NoSyncContext())
							m_socket.Connect(EndPoint).Wait();
					}

					// Handlers
					void HandleOpened(object sender, WebSocket.OpenEventArgs e)
					{
						BinanceApi.Log.Write(ELogLevel.Debug, $"WebSocket stream opened for ticker data");
					}
					void HandleClosed(object sender, WebSocket.CloseEventArgs e)
					{
						BinanceApi.Log.Write(ELogLevel.Debug, $"WebSocket stream closed for ticker data. {e.Reason}");
						Dispose();
					}
					void HandleError(object sender, WebSocket.ErrorEventArgs e)
					{
						BinanceApi.Log.Write(ELogLevel.Error, e.Exception, $"WebSocket stream error for ticker data");
						Dispose();
					}
					void HandleMessage(object sender, WebSocket.MessageEventArgs e)
					{
						try { ApplyUpdate(JToken.Parse(e.Text).ToObject<List<TickerUpdate>>()); }
						catch (OperationCanceledException) { Dispose(); }
						catch (Exception ex)
						{
							BinanceApi.Log.Write(ELogLevel.Error, ex, $"WebSocket message error for ticker data");
							Dispose();
						}
					}
				}
			}
			private WebSocket m_socket;

			/// <summary></summary>
			private void ApplyUpdate(List<TickerUpdate> updates)
			{
				lock (TickerData)
				{
					foreach (var update in updates)
					{
						var sym = update.Ticker.Pair.Id;
						//Debug.Assert(TickerData.IsOrdered(x => x.Pair.Id.CompareTo(sym)));
						var idx = TickerData.BinarySearch(x => x.Pair.Id.CompareTo(sym));
						if (idx >= 0)
							TickerData[idx] = update.Ticker;
						else
							TickerData.Insert(~idx, update.Ticker);
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
				private decimal PriceChange { set => Ticker.PriceChange = value; }

				[JsonProperty("P")]
				private decimal PriceChangePercent { set => Ticker.PriceChangePercent = value; }

				[JsonProperty("w")]
				private decimal WeightedAvgPrice { set => Ticker.WeightedAvgPrice = value; }

				[JsonProperty("x")]
				private decimal PrevClosePrice { set => Ticker.PrevClosePrice = value; }

				[JsonProperty("c")]
				private decimal LastPrice { set => Ticker.LastPrice = value; }

				[JsonProperty("Q")]
				private decimal LastQty { set => Ticker.LastQty = value; }

				[JsonProperty("b")]
				private decimal PriceB2Q { set => Ticker.PriceB2Q = value; }

				[JsonProperty("a")]
				private decimal PriceQ2B { set => Ticker.PriceQ2B = value; }

				[JsonProperty("B")]
				public decimal B2QVolumeQuote { get; set; }

				[JsonProperty("A")]
				public decimal Q2BVolumeQuote { get; set; }

				[JsonProperty("o")]
				private decimal OpenPrice { set => Ticker.OpenPrice = value; }

				[JsonProperty("h")]
				private decimal HighPrice { set => Ticker.HighPrice = value; }

				[JsonProperty("l")]
				private decimal LowPrice { set => Ticker.LowPrice = value; }

				[JsonProperty("v")]
				private decimal VolumeBase { set => Ticker.VolumeBase = value; }

				[JsonProperty("q")]
				private decimal VolumeQuote { set => Ticker.VolumeQuote = value; }

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
}
