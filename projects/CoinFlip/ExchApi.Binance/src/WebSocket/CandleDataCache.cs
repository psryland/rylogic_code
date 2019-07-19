using System;
using System.Collections.Generic;
using System.Threading;
using Binance.API.DomainObjects;
using ExchApi.Common.JsonConverter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rylogic.Attrib;
using Rylogic.Extn;
using Rylogic.Utility;
using WebSocketSharp;

namespace Binance.API
{
	public class CandleDataCache :IDisposable
	{
		public CandleDataCache(BinanceApi api)
		{
			Api = api;
			Streams = new Dictionary<PairAndTF, CandleStream>();
		}
		public void Dispose()
		{
			Util.DisposeRange(Streams.Values);
			Streams.Clear();
		}

		/// <summary>The owning API instance</summary>
		private BinanceApi Api { get; }

		/// <summary>Socket connections to streams. One per currency pair</summary>
		private Dictionary<PairAndTF, CandleStream> Streams { get; }

		/// <summary>Access the order book for the given pair</summary>
		public List<MarketChartData> this[CurrencyPair pair, EMarketPeriod period, UnixMSec time_beg, UnixMSec time_end, CancellationToken? cancel = null] // Worker thread context
		{
			get
			{
				var key = new PairAndTF { Pair = pair, TimeFrame = period };
				try
				{
					var stream = (CandleStream)null;
					lock (Streams)
					{
						// Look for the stream for 'pair'. Create if not found.
						// Return a copy since any thread can call this function.
						if (!Streams.TryGetValue(key, out stream) || stream.Socket == null)
							stream = Streams[key] = new CandleStream(pair, period, this);
					}
					lock (stream.CandleData)
					{
						// If the range is outside the cached data range, fall back to the REST call
						if (stream.CandleData.Count == 0 || time_beg < stream.CandleData[0].Time)
							return Api.GetChartData(pair, period, time_beg, time_end, cancel).Result;

						// Return the requested range
						var ibeg = stream.CandleData.BinarySearch(x => x.Time.CompareTo(time_beg), find_insert_position: true);
						var iend = stream.CandleData.BinarySearch(x => x.Time.CompareTo(time_end), find_insert_position: true);
						return stream.CandleData.GetRange(ibeg, iend - ibeg);
					}
				}
				catch (Exception ex)
				{
					BinanceApi.Log.Write(ELogLevel.Error, $"Subscribing to candle data for {pair}/{period} failed. {ex.Message}");
					lock (Streams) Streams.Remove(key);
					return new List<MarketChartData>();
				}
			}
		}

		/// <summary>Candle data for a currency pair</summary>
		private class CandleStream :IDisposable
		{
			private readonly List<CandleUpdate> m_pending;
			public CandleStream(CurrencyPair pair, EMarketPeriod period, CandleDataCache owner)
			{
				try
				{
					Cache = owner;
					Pair = pair;
					Period = period;
					CandleData = new List<MarketChartData>();

					// Create the stream connection and start buffering events
					m_pending = new List<CandleUpdate>();
					Socket = new WebSocket(EndPoint);
					Socket.Connect();

					// Request a snapshot to initialise the candle data
					// We can only buffer a limited range of candles. Outside that range forward to the rest api
					lock (CandleData)
					{
						// Get the most recent candle data
						CandleData = Api.GetChartData(Pair, period).Result;
						CandleData.Sort(x => x.Time);
					}
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
			private CandleDataCache Cache { get; }

			/// <summary></summary>
			private BinanceApi Api => Cache.Api;

			/// <summary>The pair this stream is for</summary>
			public CurrencyPair Pair { get; }

			/// <summary>The time frame that the candle data is for</summary>
			public EMarketPeriod Period { get; }

			/// <summary>Candle data for the pair</summary>
			public List<MarketChartData> CandleData { get; }

			/// <summary>The Url endpoint for the user data stream</summary>
			private string EndPoint => $"{Api.UrlSocketAddress}ws/{Pair.Id.ToLower()}@kline_{Period.Assoc<string>()}";

			/// <summary></summary>
			public WebSocket Socket
			{
				get { return m_socket; }
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
					}

					// Handlers
					void HandleOpened(object sender, EventArgs e)
					{
						BinanceApi.Log.Write(ELogLevel.Debug, $"WebSocket stream opened for Candle Data {Pair.Id}");
					}
					void HandleClosed(object sender, CloseEventArgs e)
					{
						BinanceApi.Log.Write(ELogLevel.Debug, $"WebSocket stream closed for Candle Data {Pair.Id}");
						Socket = null;
					}
					void HandleError(object sender, ErrorEventArgs e)
					{
						BinanceApi.Log.Write(ELogLevel.Error, e.Exception, $"WebSocket stream error for Candle Data {Pair.Id}");
						Socket = null;
						return;
					}
					void HandleMessage(object sender, MessageEventArgs e)
					{
						ApplyUpdate(JObject.Parse(e.Data).ToObject<CandleUpdate>());
					}
				}
			}
			private WebSocket m_socket;

			/// <summary>Parse a market data update message</summary>
			private void ApplyUpdate(CandleUpdate update) // Worker thread context
			{
				lock (CandleData)
				{
					// Push the update to the buffer
					m_pending.Add(update);

					// If the data hasn't had the initial snapshot applied yet, we're done
					if (CandleData.Count == 0)
						return;

					// Apply updates 
					foreach (var upd in m_pending)
					{
						var idx = CandleData.BinarySearch(x => x.Time.CompareTo(upd.Kline.StartTime));
						if (idx >= 0)
							CandleData[idx] = upd.Kline;
						else
							CandleData.Insert(~idx, upd.Kline);
					}

					// Cap the length of the candle data
					if (CandleData.Count > 1500)
						CandleData.RemoveRange(0, 500);

					// All pending updates have been applied
					m_pending.Clear();
				}
			}
		}

		/// <summary>Incremental candle data update</summary>
		private class CandleUpdate
		{
			[JsonProperty("e")]
			public string EventType { get; set; }

			[JsonProperty("E"), JsonConverter(typeof(UnixMSToDateTimeOffset))]
			public DateTimeOffset EventTime { get; set; }

			[JsonProperty("s")]
			public string Symbol { get; set; }

			[JsonProperty("K")]
			public Candle Kline { get; set; }

			/// <summary></summary>
			public class Candle
			{
				[JsonProperty("t"), JsonConverter(typeof(UnixMSToDateTimeOffset))]
				public DateTimeOffset StartTime { get; set; }

				[JsonProperty("T"), JsonConverter(typeof(UnixMSToDateTimeOffset))]
				public DateTimeOffset EndTime { get; set; }

				[JsonProperty("s")]
				public string Symbol { get; set; }

				public EMarketPeriod Interval { get; set; }
				[JsonProperty("i")] private string IntervalInternal { set => Interval = Enum<EMarketPeriod>.FromAssoc(value) ?? EMarketPeriod.None; }

				[JsonProperty("f")]
				public long FirstTradeId { get; set; }

				[JsonProperty("L")]
				public long LastTradeId { get; set; }

				[JsonProperty("o")]
				public double Open { get; set; }

				[JsonProperty("c")]
				public double Close { get; set; }

				[JsonProperty("h")]
				public double High { get; set; }

				[JsonProperty("l")]
				public double Low { get; set; }

				[JsonProperty("v")]
				public double Volume { get; set; }

				[JsonProperty("n")]
				public int NumberOfTrades { get; set; }

				[JsonProperty("x")]
				public bool IsBarFinal { get; set; }

				[JsonProperty("q")]
				public double QuoteVolume { get; set; }

				[JsonProperty("V")]
				public double VolumeOfActivyBuy { get; set; }

				[JsonProperty("Q")]
				public double QuoteVolumeOfActivyBuy { get; set; }

				public static implicit operator MarketChartData(Candle c)
				{
					return new MarketChartData(c.StartTime, c.Open, c.High, c.Low, c.Close, c.Volume, c.EndTime, c.QuoteVolume, c.NumberOfTrades, c.VolumeOfActivyBuy, c.QuoteVolumeOfActivyBuy);
				}
			}
		}

		/// <summary></summary>
		private struct PairAndTF
		{
			public CurrencyPair Pair;
			public EMarketPeriod TimeFrame;
		}
	}
}
