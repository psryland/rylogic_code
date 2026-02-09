using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;
using Bittrex.API.DomainObjects;
using Bittrex.API.Subscriptions;
using ExchApi.Common;
using ExchApi.Common.JsonConverter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Bittrex.API
{
	public class MarketDataCache : IDisposable
	{
		// Notes:
		//  - Maintains a mirror of the current state of the market for registered pairs.
		//  - Allows callers to assume they're querying the exchange directly.
		//  - Web socket updates arrive on worker threads
		//
		// Algorithm:
		//    WebSocket nonces are server-specific, it's crucial to maintain state on a per-connection basis.
		//    To resynchronise a market order book:
		//    - Drop existing web socket connections and flush accumulated data and state (e.g.market nonces).
		//    - Re-establish the web socket connection.
		//    - Subscribe to the market deltas, buffering received data, ordered by nonce.
		//    - Query the full market state.
		//    - Apply buffered deltas sequentially, starting with nonces greater than that received in step 4.

		public MarketDataCache(BittrexApi api)
		{
			Api = api;
			Streams = new Dictionary<CurrencyPair, MarketStream>();
			Socket = Api.WebSocket;
		}
		public void Dispose()
		{
			Util.DisposeRange(Streams.Values);
			Streams.Clear();
			Socket = null;
		}

		/// <summary>The socket connection to the exchange</summary>
		private BittrexWebSocket Socket
		{
			get;
			set
			{
				if (field == value) return;
				if (field != null)
				{
					field.MarketDataUpdate -= HandleMarketUpdate;
				}
				field = value;
				if (field != null)
				{
					field.MarketDataUpdate += HandleMarketUpdate;
				}

				// Handlers
				void HandleMarketUpdate(JToken jtok)
				{
					var delta = jtok.ToObject<MarketDelta>();

					try
					{
						var stream = (MarketStream)null;
						lock (Streams)
						{
							if (!Streams.TryGetValue(delta.Pair, out stream))
								return;
						}
						stream.ApplyUpdate(delta);
					}
					catch (Exception ex)
					{
						BittrexApi.Log.Write(ELogLevel.Error, ex, $"Market update error for {delta.Pair.Id}");
						lock (Streams) Streams.Remove(delta.Pair);
					}
				}
			}
		}

		/// <summary>The owning API instance</summary>
		private BittrexApi Api { get; }

		/// <summary>Market data streams per currency</summary>
		private Dictionary<CurrencyPair, MarketStream> Streams { get; }

		/// <summary>Return the order book for the given pair</summary>
		public OrderBook this[CurrencyPair pair] // Any thread context
		{
			get
			{
				try
				{
					var stream = (MarketStream)null;
					var subscribe = false;
					lock (Streams)
					{
						// Look for the stream for 'pair'. Create if not found.
						if (!Streams.TryGetValue(pair, out stream))
						{
							stream = Streams[pair] = new MarketStream(pair, this);
							subscribe = true;
						}
					}
					
					// This cannot be done in the constructor or MarketStream because the Socket calls will deadlock 
					if (subscribe)
					{
						// Add a new subscription for updates to 'pair'
						Socket.SubscribeToExchangeDeltas(pair.Id).Wait();

						// Request a full snapshot to initialise the order book
						var jtok = Socket.QueryExchangeState(pair.Id).Result;
						var snapshot = jtok.ToObject<MarketSnapshot>();
						stream.ApplyUpdate(snapshot);
					}

					lock (stream.OrderBook)
					{
						// Return a copy since any thread can call this function.
						return new OrderBook(stream.OrderBook);
					}
				}
				catch (Exception ex)
				{
					BittrexApi.Log.Write(ELogLevel.Error, $"Subscribing to market updates for {pair} failed. {ex.Message}");
					lock (Streams) Streams.Remove(pair);
					return new OrderBook(pair, 0);
				}
			}
		}

		/// <summary>Per-currency pair market stream</summary>
		private class MarketStream :IDisposable
		{
			private const int MaxBufferedUpdates = 1024;
			private readonly List<IMarketUpdate> m_pending;
			public MarketStream(CurrencyPair pair, MarketDataCache owner)
			{
				try
				{
					Cache = owner;
					Pair = pair;
					OrderBook = new OrderBook(pair, 0L);
					m_pending = new List<IMarketUpdate>();
				}
				catch
				{
					Dispose();
					throw;
				}
			}
			public void Dispose()
			{
				// There is no unsubscribe. The recommendation is to drop the connection.
			}

			/// <summary>The owning cache</summary>
			private MarketDataCache Cache { get; }

			/// <summary></summary>
			private BittrexApi Api => Cache.Api;

			/// <summary>The pair this stream is for</summary>
			public CurrencyPair Pair { get; }

			/// <summary>The order book being maintained by this stream</summary>
			public OrderBook OrderBook { get; }

			/// <summary>Parse a market data update message</summary>
			public void ApplyUpdate(IMarketUpdate update) // Worker thread context
			{
				lock (OrderBook)
				{
					m_pending.Add(update);
					m_pending.Sort(x => x.Nonce);

					// Remove updates older than the OrderBook nonce
					// Note, snapshots and deltas in the pending list can have the same nonce value.
					var cnt = m_pending.CountWhile(x => x.Nonce < OrderBook.Nonce || (x.Nonce == OrderBook.Nonce && x is MarketSnapshot));
					m_pending.RemoveRange(0, cnt);

					// If we're still waiting for the first snapshot, leave
					if (m_pending.Count == 0 || (!(m_pending[0] is MarketSnapshot) && OrderBook.Nonce == 0))
					{
						if (m_pending.Count < MaxBufferedUpdates) return;
						throw new Exception($"Market data snapshot never arrived for {Pair.Id}");
					}

					// If the first update is a snapshot, we can dump any previous book
					// data and repopulate it from the snapshot and subsequent deltas.
					if (m_pending[0] is MarketSnapshot snap)
					{
						OrderBook.BuyOffers.Clear();
						OrderBook.SellOffers.Clear();
						PopulateBook(OrderBook.BuyOffers, snap.Buys, -1);
						PopulateBook(OrderBook.SellOffers, snap.Sells, +1);
						OrderBook.Nonce = snap.Nonce;
						m_pending.RemoveAt(0);
					}

					// Apply the rest of the delta updates to the order book
					foreach (var upd in m_pending)
					{
						if (!(upd is MarketDelta delta))
							throw new Exception("There should be a maximum of one snapshot in a group of updates");
						if (!(OrderBook.Nonce <= delta.Nonce))
							throw new Exception("Updates should have increasing nonce values");

						UpdateBook(OrderBook.BuyOffers, delta.Buys, -1);
						UpdateBook(OrderBook.SellOffers, delta.Sells, +1);
						OrderBook.Nonce = delta.Nonce;
					}

					// All pending updates have been applied
					m_pending.Clear();
				}

				// Handlers
				void UpdateBook(List<OrderBook.Offer> book, List<MarketDelta.Offer> changes, int sign)
				{
					// Merge the 'changes' into 'book'
					foreach (var chg in changes)
					{
						// Find the index of 'price' in 'book'
						var order = new OrderBook.Offer(chg.Rate, chg.Amount);
						var cmp = Comparer<OrderBook.Offer>.Create((l, r) => sign * l.PriceQ2B.CompareTo(r.PriceQ2B));
						var idx = book.BinarySearch(order, cmp);

						switch (chg.Type) {
						default: throw new Exception($"Unknown change type: {chg.Type}");
						case MarketDelta.EUpdateType.Add:    if (idx < 0) book.Insert(~idx, order); else book[idx] = order; break;
						case MarketDelta.EUpdateType.Update: if (idx >= 0) book[idx] = order; break;
						case MarketDelta.EUpdateType.Remove: if (idx >= 0) book.RemoveAt(idx); break;
						}
					}
				}
				void PopulateBook(List<OrderBook.Offer> book, List<MarketSnapshot.Offer> offers, int sign)
				{
					// Populate 'book' from 'offers'
					offers.Sort(x => sign * x.Rate);
					book.AddRange(offers.Select(x => new OrderBook.Offer(x.Rate, x.Amount)));
				}
			}
		}

		/// <summary>Market snapshot or update</summary>
		private interface IMarketUpdate
		{
			CurrencyPair Pair { get; }

			long Nonce { get; }
		}

		/// <summary>An incremental order book update</summary>
		private class MarketDelta : IMarketUpdate
		{
			/// <summary>Market name</summary>
			[JsonProperty("M"), JsonConverter(typeof(ParseMethod<CurrencyPair>))]
			public CurrencyPair Pair { get; set; }

			/// <summary>Nonce</summary>
			[JsonProperty("N")]
			public long Nonce { get; set; }

			/// <summary>Buy offers</summary>
			[JsonProperty("Z")]
			public List<Offer> Buys { get; set; }

			/// <summary>Sell offers</summary>
			[JsonProperty("S")]
			public List<Offer> Sells { get; set; }

			/// <summary></summary>
			[JsonProperty("f")]
			public List<Fill> Fills { get; set; }

			public enum EUpdateType
			{
				Add = 0,
				Remove = 1,
				Update = 2,
			}
			[DebuggerDisplay("{Description,nq}")]
			public class Offer
			{
				/// <summary>Update type</summary>
				[JsonProperty("TY"), JsonConverter(typeof(ToEnum<EUpdateType>))]
				public EUpdateType Type { get; set; }

				/// <summary>Price of the order</summary>
				[JsonProperty("R")]
				public decimal Rate { get; set; }

				/// <summary>Total amount at this price</summary>
				[JsonProperty("Q")]
				public decimal Amount { get; set; }

				/// <summary></summary>
				public string Description => $"{Type} Price={Rate} Amount={Amount}";
			}
			public class Fill
			{
				/// <summary></summary>
				[JsonProperty("FI")]
				public long FillId { get; set; }

				/// <summary>Order type (side)</summary>
				[JsonProperty("OT"), JsonConverter(typeof(ToEnum<EOrderSide>))]
				public EOrderSide Type { get; set; }

				/// <summary></summary>
				[JsonProperty("R")]
				public decimal Rate { get; set; }

				/// <summary></summary>
				[JsonProperty("Q")]
				public decimal Amount { get; set; }

				/// <summary></summary>
				[JsonProperty("T"), JsonConverter(typeof(UnixMSToDateTimeOffset))]
				public DateTimeOffset Timestamp { get; set; }
			}
		}

		/// <summary>A full order book update</summary>
		private class MarketSnapshot : IMarketUpdate
		{
			/// <summary>Market name</summary>
			[JsonProperty("M"), JsonConverter(typeof(ParseMethod<CurrencyPair>))]
			public CurrencyPair Pair { get; set; }

			/// <summary>Nonce</summary>
			[JsonProperty("N")]
			public long Nonce { get; set; }

			/// <summary>Buy offers</summary>
			[JsonProperty("Z")]
			public List<Offer> Buys { get; set; }

			/// <summary>Sell offers</summary>
			[JsonProperty("S")]
			public List<Offer> Sells { get; set; }

			/// <summary></summary>
			[JsonProperty("f")]
			public List<Fill> Fills { get; set; }

			public enum EFillType
			{
				Fill,
				PartialFill
			}
			[DebuggerDisplay("{Description,nq}")]
			public class Offer
			{
				/// <summary></summary>
				[JsonProperty("R")]
				public decimal Rate { get; set; }

				/// <summary></summary>
				[JsonProperty("Q")]
				public decimal Amount { get; set; }

				/// <summary></summary>
				public string Description => $"Price={Rate} Amount={Amount}";
			}
			public class Fill
			{
				/// <summary>Order ID</summary>
				[JsonProperty("I")]
				public long OrderId { get; set; }

				/// <summary>Timestamp</summary>
				[JsonProperty("T"), JsonConverter(typeof(UnixMSToDateTimeOffset))]
				public DateTimeOffset Timestamp { get; set; }

				/// <summary></summary>
				[JsonProperty("Q")]
				public decimal Amount { get; set; }

				/// <summary></summary>
				[JsonProperty("P")]
				public decimal Price { get; set; }

				/// <summary>Total price of the order</summary>
				[JsonProperty("t")]
				public decimal Total { get; set; }

				/// <summary>The side of the order</summary>
				[JsonProperty("F"), JsonConverter(typeof(ToEnum<EFillType>))]
				public EFillType FillType { get; set; }

				/// <summary>The side of the order</summary>
				[JsonProperty("OT"), JsonConverter(typeof(ToEnum<EOrderSide>))]
				public EOrderSide Type { get; set; }

				/// <summary></summary>
				[JsonProperty("U")]
				public Guid Uuid { get; set; }
			}
		}
	}
}
