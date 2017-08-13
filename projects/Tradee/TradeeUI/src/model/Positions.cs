using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;
using pr.container;
using pr.extn;
using pr.maths;
using static pr.db.Sqlite;

namespace Tradee
{
	/// <summary>A collection of all orders</summary>
	public class Positions :IDisposable
	{
		// Notes:
		// - Trades are basically containers of orders for the same instrument
		// - Trades are created/destroyed by this class, clients should add/remove orders

		public Positions(MainModel model)
		{
			Model  = model;
			Trades = new BindingSource<Trade> { DataSource = new BindingListEx<Trade>() , PerItemClear = true };
			Orders = new BindingSource<Order> { DataSource = new BindingListEx<Order>() , PerItemClear = true };
			OrderLookup = new Dictionary<int, Order>();
		}
		public virtual void Dispose()
		{
			Trades = null;
			Orders = null;
			Model = null;
		}

		/// <summary>The App logic</summary>
		public MainModel Model
		{
			[DebuggerStepThrough] get { return m_model; }
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					Model.Acct.AccountChanged -= HandleAcctChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					Model.Acct.AccountChanged += HandleAcctChanged;
				}
			}
		}
		private MainModel m_model;

		/// <summary>All trades, past, present, and future</summary>
		public BindingSource<Trade> Trades
		{
			[DebuggerStepThrough]get { return m_impl_trades; }
			private set
			{
				if (m_impl_trades == value) return;
				if (m_impl_trades != null)
				{
					m_impl_trades.ListChanging -= HandleTradesListChanging;
				}
				m_impl_trades = value;
				if (m_impl_trades != null)
				{
					m_impl_trades.ListChanging += HandleTradesListChanging;
				}
			}
		}
		private BindingSource<Trade> m_impl_trades;

		/// <summary>All orders, visualising, pending, active, closed</summary>
		public BindingSource<Order> Orders
		{
			[DebuggerStepThrough] get { return m_impl_orders; }
			private set
			{
				if (m_impl_orders == value) return;
				if (m_impl_orders != null)
				{
					m_impl_orders.ListChanging -= HandleOrdersListChanging;
				}
				m_impl_orders = value;
				if (m_impl_orders != null)
				{
					m_impl_orders.ListChanging += HandleOrdersListChanging;
				}
			}
		}
		private BindingSource<Order> m_impl_orders;

		/// <summary>A map from order Id to order</summary>
		public Dictionary<int, Order> OrderLookup
		{
			[DebuggerStepThrough] get { return m_order_lookup; }
			private set
			{
				if (m_order_lookup == value) return;
				m_order_lookup = value;
			}
		}
		private Dictionary<int, Order> m_order_lookup;

		/// <summary>Raised whenever a trade or order is added/removed/modified</summary>
		public event EventHandler Changed;
		protected virtual void OnChanged()
		{
			m_sig_changed = false;
			Changed.Raise(this);
		}
		private void SignalChanged(object sender = null, EventArgs e = null)
		{
			if (m_sig_changed) return;
			m_sig_changed = true;
			Dispatcher.CurrentDispatcher.BeginInvoke(OnChanged);
		}
		private bool m_sig_changed;

		/// <summary>Update the current active positions</summary>
		public void Update(Position[] positions)
		{
			var valid = positions.ToHashSet(x => x.Id);

			// Remove any active positions that are not in 'positions'
			Orders.RemoveIf(x => x.State == Trade.EState.ActivePosition && !valid.Contains(x.Id));

			// Add or update the current positions
			foreach (var pos in positions)
			{
				var idx = Orders.IndexOf(x => x.State == Trade.EState.ActivePosition && x.Id == pos.Id);
				var instr = Model.MarketData[pos.SymbolCode];
				var order = idx >= 0 ? Orders[idx] : Orders.Add2(new Order(pos.Id, instr, pos.TradeType, Trade.EState.ActivePosition));
				order.Update(pos);
			}
			InvalidateTrades();
		}

		/// <summary>Update the pending orders</summary>
		public void Update(PendingOrder[] pending)
		{
			var valid = pending.ToHashSet(x => x.Id);

			// Remove any pending orders that are not in 'pending'
			Orders.RemoveIf(x => x.State == Trade.EState.PendingOrder && !valid.Contains(x.Id));

			// Add or update the pending orders
			foreach (var pos in pending)
			{
				var idx = Orders.IndexOf(x => x.State == Trade.EState.PendingOrder && x.Id == pos.Id);
				var instr = Model.MarketData[pos.SymbolCode];
				var order = idx >= 0 ? Orders[idx] : Orders.Add2(new Order(pos.Id, instr, pos.TradeType, Trade.EState.PendingOrder));
				order.Update(pos);
			}
			InvalidateTrades();
		}

		/// <summary>Update the closed positions</summary>
		public void Update(ClosedOrder[] closed)
		{
			var valid = closed.ToHashSet(x => x.Id);

			// Remove any closed orders that are not in 'pending'
			Orders.RemoveIf(x => x.State == Trade.EState.Closed && !valid.Contains(x.Id));

			// Add or update the closed orders
			foreach (var pos in closed)
			{
				var idx = Orders.IndexOf(x => x.State == Trade.EState.Closed && x.Id == pos.Id);
				var instr = Model.MarketData[pos.SymbolCode];
				var order = idx >= 0 ? Orders[idx] : Orders.Add2(new Order(pos.Id, instr, pos.TradeType, Trade.EState.Closed));
				order.Update(pos);
			}
			InvalidateTrades();
		}

		/// <summary>Refresh the set of trades</summary>
		private void UpdateTrades()
		{
			m_trades_invalidated = false;
			if (Model == null) return;

			// Create lists of trades per instrument.
			// Once complete, go through existing trades looking for matches.
			// Remove any trades not in 'trades', and any trades not in 'Trades'.
			var trades_map = new Dictionary<Instrument, List<Trade>>();

			// Remove orders from trades that are no longer in 'Orders'
			foreach (var trade in Trades)
				trade.Orders.RemoveIf(x => !OrderLookup.ContainsKey(x.Id));

			// Remove trades that contain no orders
			Trades.RemoveIf(x => x.Orders.Count == 0);

			// Group the orders into instruments
			foreach (var order_grp in Orders.GroupBy(x => x.Instrument))
			{
				// Create an ordered list of trades based on non-overlapping time ranges
				var instr = order_grp.Key;
				var trades = trades_map.GetOrAdd(instr, k => new List<Trade>());
				foreach (var order in order_grp)
				{
					// Look for a trade in the list that intersects the time range
					var idx = trades.BinarySearch(r =>
						order.ExitTimeUTC  < r.EntryTimeUTC ? -1 :
						order.EntryTimeUTC > r.ExitTimeUTC  ? +1 : 0);

					// If index is positive, then it's the index of a trade that intersects the time range.
					// If not, then there is no trade yet that covers this order so create one.
					var trade = (idx >= 0) ? trades[idx] : trades.Insert2(idx = ~idx, new Trade(Model, instr));
					trade.Orders.Add(order);

					// Sanity check that the trade time range overlaps the order time range
					Debug.Assert(
						order.EntryTimeUTC >= trade.EntryTimeUTC &&
						order.ExitTimeUTC  <= trade.ExitTimeUTC  &&
						trade.Orders.Count(x => x == order) == 1);

					// Merge adjacent trades if their time ranges overlap
					for (int i = idx; i-- != 0 && trades[i].ExitTimeUTC >= trades[idx].ExitTimeUTC;)
					{
						// Trade automatically updates its Entry/Exit time when its 'Orders' collection is changed
						trades[i].Orders.AddRange(trades[idx].Orders);
						trades[idx].Dispose();
						trades.RemoveAt(idx);
						idx = i;
					}
					for (int i = idx; ++i != trades.Count && trades[idx].ExitTimeUTC >= trades[i].EntryTimeUTC;)
					{
						// Trade automatically updates its Entry/Exit time when its 'Orders' collection is changed
						trades[idx].Orders.AddRange(trades[i].Orders);
						trades[i].Dispose();
						trades.RemoveAt(i);
						i = idx;
					}
				}

				// Check the ranges are non-overlapping
				#if DEBUG
				for (int i = 0; i != trades.Count; ++i)
				{
					Debug.Assert(trades[i].EntryTimeUTC <= trades[i].ExitTimeUTC);
					Debug.Assert(i == 0 || trades[i-1].EntryTimeUTC < trades[i].EntryTimeUTC);
					Debug.Assert(i == 0 || trades[i-1].ExitTimeUTC  < trades[i].EntryTimeUTC);
				}
				#endif
			}

			// Remove trades from the trades map that match existing trades.
			// Want to avoid deleting existing trades where possible.
			foreach (var existing in Trades)
			{
				// There shouldn't be any existing trades for instruments that aren't in 'trades_map'
				var trades = trades_map[existing.Instrument];

				// Try to find a match in trades for 'existing'
				var idx = trades.IndexOf(trade => trade.Orders.SequenceEqualUnordered(existing.Orders));

				// Matching existing trade found, remove from trades list in the map
				if (idx != -1)
				{
					trades[idx].Dispose();
					trades.RemoveAt(idx);
				}

				// No more trades for this instrument? Remove the entry from the map
				if (trades.Count == 0)
					trades_map.Remove(existing.Instrument);
			}

			// Add all trades left in the trades map to the current trades list
			foreach (var trades in trades_map.Values)
				Trades.AddRange(trades);
		}

		/// <summary>Signal that the trades collection is out of date, and queue up a refresh</summary>
		private void InvalidateTrades()
		{
			if (m_trades_invalidated) return;
			m_trades_invalidated = true;
			Dispatcher.CurrentDispatcher.BeginInvoke(UpdateTrades);
		}
		private bool m_trades_invalidated;

		/// <summary>When the account changes, flush all the position data</summary>
		private void HandleAcctChanged(object sender, EventArgs e)
		{
			Orders.Clear();
			Trades.Clear();
		}

		/// <summary>Handle trades added or removed from the Trades collection</summary>
		private void HandleTradesListChanging(object sender, ListChgEventArgs<Trade> e)
		{
			switch (e.ChangeType)
			{
			case ListChg.Reset:
				{
					// Watch for order changed
					foreach (var trade in Trades)
					{
						trade.Changed -= SignalChanged;
						trade.Changed += SignalChanged;
					}
					SignalChanged();
					break;
				}
			case ListChg.ItemAdded:
				{
					e.Item.Changed += SignalChanged;
					SignalChanged();
					break;
				}
			case ListChg.ItemRemoved:
				{
					e.Item.Changed -= SignalChanged;
					e.Item.Dispose();
					SignalChanged();
					break;
				}
			}
		}

		/// <summary>Handle orders being added/removed from the collection</summary>
		private void HandleOrdersListChanging(object sender, ListChgEventArgs<Order> e)
		{
			// Every Order should belong to a Trade, create trades if necessary
			switch (e.ChangeType)
			{
			case ListChg.Reset:
				{
					// Refresh the order lookup map
					OrderLookup.Clear();
					foreach (var order in Orders)
						OrderLookup.Add(order.Id, order);

					// Watch for order changed
					foreach (var order in Orders)
					{
						order.Changed -= SignalChanged;
						order.Changed += SignalChanged;
					}

					InvalidateTrades();
					SignalChanged();
					break;
				}
			case ListChg.ItemAdded:
				{
					OrderLookup[e.Item.Id] = e.Item;
					e.Item.Changed += SignalChanged;

					InvalidateTrades();
					SignalChanged();
					break;
				}
			case ListChg.ItemRemoved:
				{
					OrderLookup.Remove(e.Item.Id);
					e.Item.Changed -= SignalChanged;

					InvalidateTrades();
					SignalChanged();
					break;
				}
			}
		}
	}

	/// <summary>Common interface for Trades and Orders</summary>
	public interface IPosition
	{
		/// <summary>The instrument being traded</summary>
		Instrument Instrument { get; }

		/// <summary>The symbol that this order is on</summary>
		string SymbolCode { get; }

		/// <summary>The type of trade</summary>
		ETradeType TradeType { get; }

		/// <summary>
		/// The position entry time in UTC.
		/// If the order is in EState Visualising or PendingOrder then this is the time that the server
		/// would become aware of the trade (i.e. a pending order would be submitted). If Active or Closed
		/// then this is the time that the trade was triggered.</summary>
		DateTimeOffset EntryTimeUTC { get; }

		/// <summary>Get/Set the amount lost (in base currency) if the stop loss is hit</summary>
		double StopLossValue { get; }

		/// <summary>Get/Set the amount gained (in account currency) if the take profit is hit</summary>
		double TakeProfitValue { get; }

		/// <summary>The amount traded by the position</summary>
		long Volume { get; }

		/// <summary>Gross profit accrued by the order associated with a position</summary>
		double GrossProfit { get; }

		/// <summary>The Net profit of the position (in base currency)/summary>
		double NetProfit { get; }

		/// <summary>The state of this order</summary>
		Trade.EState State { get; }
	}
}
