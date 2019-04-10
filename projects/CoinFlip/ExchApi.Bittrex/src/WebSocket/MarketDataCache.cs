using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bittrex.API.DomainObjects;
using Bittrex.API.Subscriptions;
using ExchApi.Common;
using ExchApi.Common.JsonConverter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rylogic.Common;
using Rylogic.Extn;

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

		private const int MaxBufferedUpdates = 1024;
		private readonly Dictionary<CurrencyPair, OrderBook> m_data;
		private readonly List<IMarketUpdate> m_pending;
		public MarketDataCache(BittrexWebSocket bws)
		{
			Debug.Assert(Misc.AssertMainThread());
			m_data = new Dictionary<CurrencyPair, OrderBook>();
			m_pending = new List<IMarketUpdate>();
			Socket = bws;
		}
		public void Dispose()
		{
			Socket = null;
		}

		/// <summary>The socket connection to the exchange</summary>
		private BittrexWebSocket Socket
		{
			get { return m_socket; }
			set
			{
				if (m_socket == value) return;
				if (m_socket != null)
				{
					m_socket.MarketDataUpdate -= HandleMarketUpdate;
				}
				m_socket = value;
				if (m_socket != null)
				{
					m_socket.MarketDataUpdate += HandleMarketUpdate;
				}

				// Handlers
				void HandleMarketUpdate(JToken jtok)
				{
					var delta = jtok.ToObject<MarketDelta>();
					ApplyPendingUpdates(delta.Pair, delta);
				}
			}
		}
		private BittrexWebSocket m_socket;

		/// <summary>Return the order book for the given pair</summary>
		public OrderBook this[CurrencyPair pair] // Any thread context
		{
			get
			{
				// If it's not there add a subscription for the pair
				try
				{
					lock (m_data)
					{
						// Try to find 'pair' in our local cache. 
						// Return a copy since any thread can call this function.
						if (m_data.TryGetValue(pair, out var ob))
							return new OrderBook(ob);
					}

					// Add a new subscription for updates to 'pair'
					Socket.SubscribeToExchangeDeltas(pair.Id).Wait();

					// Request a full snapshot to initialise the order book
					var jtok = Socket.QueryExchangeState(pair.Id).Result;
					var snapshot = jtok.ToObject<MarketSnapshot>();
					ApplyPendingUpdates(pair, snapshot);
					Debug.Assert(m_data.ContainsKey(pair));

					// Return a copy of the order book
					lock (m_data)
						return new OrderBook(m_data[pair]);
				}
				catch (Exception ex)
				{
					m_data.Remove(pair);
					BittrexApi.Log(Rylogic.Utility.ELogLevel.Error, $"Subscribing to market updates for {pair} failed. {ex.Message}");
					return new OrderBook(pair, 0);
				}
			}
		}

		/// <summary>Parse a market data update message</summary>
		private void ApplyPendingUpdates(CurrencyPair pair, IMarketUpdate update) // Worker thread context
		{
			lock (m_data)
			{
				m_pending.Add(update);

				// Try to get the existing order book for 'pair'
				var order_book = m_data.TryGetValue(pair, out var ob) ? ob : null;
				var nonce = order_book?.Nonce ?? 0L;

				// Create a list of the updates that apply to 'pair'
				var updates = m_pending.Where(x => x.Pair.Equals(pair)).ToList();

				// Get the newest snapshot, if it's newer than 'order_book.Nonce'
				var snap = updates.OfType<MarketSnapshot>().MaxByOrDefault(x => x.Nonce);
				snap = snap?.Nonce > nonce ? snap : null;
				nonce = snap?.Nonce ?? nonce;

				// Remove updates older than 'nonce'.
				// Note, snapshots and deltas in the pending list can have the same nonce value.
				updates.RemoveIf(x => x.Nonce <= nonce && x != snap);
				if (updates.Count == 0)
					return;

				// Order by nonce value.
				updates.Sort(x => x.Nonce);
				Debug.Assert(updates.IsOrdered(ESequenceOrder.StrictlyIncreasing, Cmp<IMarketUpdate>.From(x => x.Nonce)));

				// If there is no order book for 'pair' yet and we don't have
				// a newer snapshot then give up, we need a snapshot to start with.
				if (order_book == null && snap == null)
					return;

				// If the first update is a snapshot, we can dump any previous book
				// and repopulate it from the snapshot and subsequent deltas.
				if (snap != null)
				{
					m_data.Remove(pair);
					order_book = m_data.Add2(pair, new OrderBook(pair, snap.Nonce));
					PopulateBook(order_book.BuyOffers, snap.Buys, -1);
					PopulateBook(order_book.SellOffers, snap.Sells, +1);
					updates.RemoveAt(0);
				}

				// Apply the rest of the delta updates to the order book
				foreach (var upd in updates)
				{
					if (!(upd is MarketDelta delta))
						throw new Exception("There should be a maximum of one snapshot in a group of updates");
					if (order_book.Nonce >= delta.Nonce)
						throw new Exception("Updates should have increasing nonce values");

					UpdateBook(order_book.BuyOffers, delta.Buys, -1);
					UpdateBook(order_book.SellOffers, delta.Sells, +1);
					order_book.Nonce = delta.Nonce;
				}

				// Remove all updates for 'pair' with a nonce <= 'order_book.Nonce'
				m_pending.RemoveIf(x => x.Nonce <= order_book.Nonce);
			}

			// Handlers
			void UpdateBook(List<OrderBook.Offer> book, List<MarketDelta.Offer> changes, int sign)
			{
				// Merge the 'changes' into 'book'
				foreach (var chg in changes)
				{
					// Find the index of 'price' in 'book'
					var order = new OrderBook.Offer(chg.Rate, chg.Amount);
					var cmp = Comparer<OrderBook.Offer>.Create((l, r) => sign * l.Price.CompareTo(r.Price));
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
