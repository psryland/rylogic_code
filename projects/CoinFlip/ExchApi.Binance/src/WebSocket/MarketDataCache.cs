using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Binance.API.DomainObjects;
using ExchApi.Common;
using ExchApi.Common.JsonConverter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Binance.API
{
	public class MarketDataCache :IDisposable
	{
		// Notes:
		//  - See TickerDataCache for the simplest example
		//  - Using one socket per currency pair because there is no mechanism
		//    for adding/removing subscriptions to streams.

		public MarketDataCache(BinanceApi api)
		{
			Api = api;
			Streams = new Dictionary<CurrencyPair, MarketStream>();
		}
		public void Dispose()
		{
			Util.DisposeRange(Streams.Values);
			Streams.Clear();
		}

		/// <summary>The owning API instance</summary>
		private BinanceApi Api { get; }

		/// <summary>Socket connections to streams. One per currency pair</summary>
		private Dictionary<CurrencyPair, MarketStream> Streams { get; }

		/// <summary>Returns the currency pairs that market data is already enabled for</summary>
		public IList<CurrencyPair> Cached
		{
			get
			{
				lock (Streams)
					return Streams.Keys.ToList();
			}
		}

		/// <summary>Access the order book for the given pair</summary>
		public MarketDepth this[CurrencyPair pair] // Worker thread context
		{
			get
			{
				try
				{
					// The locking idea here is, lock to get the stream for 'pair'.
					// Then lock the orderbook of that stream and make a copy.
					var stream = (MarketStream)null;
					lock (Streams)
					{
						// Look for the stream for 'pair'. Create if not found.
						if (!Streams.TryGetValue(pair, out stream) || stream.Socket == null)
							stream = Streams[pair] = new MarketStream(pair, this);
					}
					lock (stream.MarketDepth)
					{
						// Return a copy since any thread can call this function.
						return new MarketDepth(stream.MarketDepth);
					}
				}
				catch (Exception ex)
				{
					BinanceApi.Log.Write(ELogLevel.Error, ex, $"Subscribing to market updates for {pair.Id} failed.");
					lock (Streams) Streams.Remove(pair);
					return new MarketDepth(pair);
				}
			}
		}

		/// <summary>Stop market data updates for 'pair'</summary>
		public void Forget(CurrencyPair pair)
		{
			lock (Streams)
				Streams.Remove(pair);
		}

		/// <summary>Stream of currency pair market data</summary>
		private class MarketStream :IDisposable
		{
			// Notes:
			//  - This class maintains the orderbook for a single currency pair

			private const int MaxBufferedUpdates = 1024;
			private readonly List<MarketUpdate> m_pending;
			public MarketStream(CurrencyPair pair, MarketDataCache owner)
			{
				try
				{
					Cache = owner;
					Pair = pair;
					MarketDepth = new MarketDepth(pair);

					// Create the stream connection and start buffering events
					m_pending = new List<MarketUpdate>();
					Socket = new WebSocket(Api.Shutdown);

					// Request a full snapshot to initialise the order book
					var ob = Api.GetOrderBook(Pair, 1000, Api.Shutdown).Result;
					lock (MarketDepth)
					{
						MarketDepth.B2QOffers.Assign(ob.B2QOffers);
						MarketDepth.Q2BOffers.Assign(ob.Q2BOffers);
						MarketDepth.SequenceNo = ob.SequenceNo;
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
			private MarketDataCache Cache { get; }

			/// <summary></summary>
			private BinanceApi Api => Cache.Api;

			/// <summary>The pair this stream is for</summary>
			public CurrencyPair Pair { get; }

			/// <summary>The order book being maintained by this stream</summary>
			public MarketDepth MarketDepth { get; }

			/// <summary>The Url endpoint for this stream</summary>
			public string EndPoint => $"{Api.UrlSocketAddress}ws/{Pair.Id.ToLower()}@depth";

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
						Socket.Connect(EndPoint).Wait();
					}

					// Handlers
					void HandleOpened(object sender, WebSocket.OpenEventArgs e)
					{
						BinanceApi.Log.Write(ELogLevel.Debug, $"WebSocket stream opened for Market Data {Pair.Id}");
					}
					void HandleClosed(object sender, WebSocket.CloseEventArgs e)
					{
						BinanceApi.Log.Write(ELogLevel.Debug, $"WebSocket stream closed for Market Data {Pair.Id}. {e.Reason}");
						Dispose();
					}
					void HandleError(object sender, WebSocket.ErrorEventArgs e)
					{
						BinanceApi.Log.Write(ELogLevel.Error, e.Exception, $"WebSocket stream error for Market Data {Pair.Id}");
						Dispose();
					}
					void HandleMessage(object sender, WebSocket.MessageEventArgs e)
					{
						try { ApplyUpdate(JObject.Parse(e.Text).ToObject<MarketUpdate>()); }
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

			/// <summary>Parse a market data update message</summary>
			private void ApplyUpdate(MarketUpdate update) // Worker thread context
			{
				lock (MarketDepth)
				{
					// Push the update to the buffer
					m_pending.Add(update);

					// If the order book hasn't had the initial snapshot applied yet, we're done
					if (MarketDepth.SequenceNo == 0)
					{
						if (m_pending.Count < MaxBufferedUpdates) return;
						throw new Exception($"Market data snapshot never arrived for {Pair.Id}");
					}

					// Sort the pending updates in order of sequence no.
					// Each update actually represents a range of sequence numbers.
					m_pending.Sort(x => x.SequenceNoEnd);

					// Apply updates from oldest to newest
					foreach (var upd in m_pending)
					{
						// Ignore updates from before the snapshot
						if (upd.SequenceNoEnd <= MarketDepth.SequenceNo)
							continue;

						if (!(MarketDepth.SequenceNo + 1).WithinInclusive(upd.SequenceNoBeg, upd.SequenceNoEnd))
							BinanceApi.Log.Write(ELogLevel.Warn, $"Gap in market data updates for {Pair.Id}");

						// The data in the update is absolute price
						UpdateBook(MarketDepth.B2QOffers, upd.Buys, -1);
						UpdateBook(MarketDepth.Q2BOffers, upd.Sells, +1);
						MarketDepth.SequenceNo = upd.SequenceNoEnd;
					}

					// All pending updates have been applied
					m_pending.Clear();
				}

				// Handlers
				void UpdateBook(IList<MarketDepth.Offer> book, IList<MarketUpdate.Offer> changes, int sign)
				{
					// Merge the 'changes' into 'book'
					var cmp = Comparer<MarketDepth.Offer>.Create((l, r) => sign * l.PriceQ2B.CompareTo(r.PriceQ2B));
					foreach (var chg in changes)
					{
						// Find the index of 'price' in 'book'
						var order = new MarketDepth.Offer(chg.Price, chg.Quantity);
						var idx = book.BinarySearch(order, cmp);

						if (chg.Quantity != 0)
						{
							if (idx >= 0)
								book[idx] = order;
							else
								book.Insert(~idx, order);
						}
						else
						{
							if (idx >= 0)
								book.RemoveAt(idx);
						}
					}
				}
			}
		}

		/// <summary>Incremental market data update</summary>
		private class MarketUpdate
		{
			// Notes:
			// - Each update represents a range of sequence numbers:
			//   [SequenceNoBeg, SequenceNoEnd] (inclusive)

			[JsonProperty("e")]
			public string EventType { get; set; }

			[JsonProperty("E"), JsonConverter(typeof(UnixMSToDateTimeOffset))]
			public DateTimeOffset EventTime { get; set; }

			[JsonProperty("s")]
			public string Symbol { get; set; }

			[JsonProperty("U")]
			public long SequenceNoBeg { get; set; }

			[JsonProperty("u")]
			public long SequenceNoEnd { get; set; }

			public List<Offer> Buys { get; set; }
			[JsonProperty("b")] private List<string[]> BuysInternal { set => Buys = ParseOffers(value); }

			public List<Offer> Sells { get; set; }
			[JsonProperty("a")] private List<string[]> SellsInternal { set => Sells = ParseOffers(value); }

			/// <summary></summary>
			private static List<Offer> ParseOffers(List<string[]> offers)
			{
				return offers.Select(x => new Offer
				{
					Price = decimal.Parse(x[0], CultureInfo.InvariantCulture),
					Quantity = decimal.Parse(x[1], CultureInfo.InvariantCulture),
				}).ToList();
			}

			/// </summary>
			public class Offer
			{
				public decimal Price { get; set; }
				public decimal Quantity { get; set; }
			}

			private class DepthDeltaArrayConverter :JsonConverter
			{
				public override bool CanConvert(Type objectType) => throw new NotImplementedException();
				public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
				{
					var list = new List<Offer>();
					foreach (var tradePrice in JArray.Load(reader))
					{
						var price = tradePrice.ElementAt(0).ToObject<decimal>();
						var quantity = tradePrice.ElementAt(1).ToObject<decimal>();
						list.Add(new Offer { Price = price, Quantity = quantity });
					}
					return list;
				}
				public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
				{
					throw new NotImplementedException();
				}
			}
		}
	}
}
