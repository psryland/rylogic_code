using System;
using System.Diagnostics;
using System.Linq;
using pr.container;
using pr.extn;
using pr.maths;
using static pr.common.Sqlite;

namespace Tradee
{
	/// <summary>A collection of all orders</summary>
	public class Positions :IDisposable
	{
		public Positions(MainModel model)
		{
			Model  = model;
			Trades = new BindingSource<Trade> { DataSource = new BindingListEx<Trade>() , PerItemClear = true };
			Orders = new BindingSource<Order> { DataSource = new BindingListEx<Order>() , PerItemClear = true };
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
				}
				m_model = value;
				if (m_model != null)
				{
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

		/// <summary>All orders</summary>
		public BindingSource<Order> Orders
		{
			[DebuggerStepThrough] get { return m_orders; }
			private set
			{
				if (m_orders == value) return;
				if (m_orders != null)
				{
					m_orders.ListChanging -= HandleOrdersListChanging;
				}
				m_orders = value;
				if (m_orders != null)
				{
					m_orders.ListChanging += HandleOrdersListChanging;
				}
			}
		}
		private BindingSource<Order> m_orders;

		/// <summary>Raised whenever a trade or order is added/removed/modified</summary>
		public event EventHandler<PositionDataChangedEventArgs> DataChanged;
		protected virtual void OnDataChanged(PositionDataChangedEventArgs args)
		{
			DataChanged.Raise(this, args);
		}

		/// <summary>Create a new order</summary>
		public Trade GetOrCreateTrade(Instrument instrument)
		{
			var idx = Trades.IndexOf(x => x.SymbolCode == instrument.SymbolCode);
			var trade = idx >= 0 ? Trades[idx] : Trades.Add2(new Trade(Model, instrument));
			return trade;
		}

		/// <summary>Update the current active positions</summary>
		public void Update(Position[] positions)
		{
			foreach (var pos in positions)
			{
				var idx = Orders.IndexOf(x => x.Id == pos.Id);
				var instr = Model.MarketData[pos.SymbolCode];
				var order = idx >= 0 ? Orders[idx] : Orders.Add2(new Order(pos.Id, instr, pos.TradeType, Trade.EState.ActivePosition));
				order.Update(pos);
			}
		}

		/// <summary>Update the current active positions</summary>
		public void Update(PendingOrder[] pending)
		{
			foreach (var pend in pending)
			{
				var idx = Orders.IndexOf(x => x.Id == pend.Id);
				var instr = Model.MarketData[pend.SymbolCode];
				var order = idx >= 0 ? Orders[idx] : Orders.Add2(new Order(pend.Id, instr, pend.TradeType, Trade.EState.PendingOrder));
				order.Update(pend);
			}
		}

		/// <summary>Handle trades added or removed from the Trades collection</summary>
		private void HandleTradesListChanging(object sender, ListChgEventArgs<Trade> e)
		{
			var trade = e.Item;
			switch (e.ChangeType)
			{
			case ListChg.ItemAdded:
				{
					break;
				}
			case ListChg.ItemRemoved:
				{
					// Safety first...
					if (Bit.AnySet(trade.State, Trade.EState.PendingOrder|Trade.EState.ActivePosition))
						throw new Exception("Cannot delete a trade with active or pending orders");

					trade.Dispose();
					break;
				}
			}
			if (e.IsPostEvent)
			{
				OnDataChanged(new PositionDataChangedEventArgs(trade));
			}
		}

		/// <summary>Handle orders being added/removed from the collection</summary>
		private void HandleOrdersListChanging(object sender, ListChgEventArgs<Order> e)
		{
			// Every Order should belong to a Trade, create trades if necessary
			switch (e.ChangeType)
			{
			case ListChg.ItemAdded:
				{
					// When an order is added, add it to the appropriate trade
					var trade = GetOrCreateTrade(e.Item.Instrument);
					trade.Orders.AddIfUnique(e.Item);
					break;
				}
			case ListChg.ItemRemoved:
				{
					var trade = GetOrCreateTrade(e.Item.Instrument);
					trade.Orders.Remove(e.Item);
					break;
				}
			}
			if (e.IsPostEvent)
			{
				var trade = GetOrCreateTrade(e.Item.Instrument);
				OnDataChanged(new PositionDataChangedEventArgs(trade, e.Item));
			}
		}
	}

	/// <summary>Common interface for Trades and Orders</summary>
	public interface IPosition
	{
		/// <summary>The type of trade</summary>
		ETradeType TradeType { get; }

		/// <summary>The amount traded by the position</summary>
		long Volume { get; }

		/// <summary>Gross profit accrued by the order associated with a position</summary>
		double GrossProfit { get; }

		/// <summary>The Net profit of the position (in base currency)/summary>
		double NetProfit { get; }

		/// <summary>The state of this order</summary>
		Trade.EState State { get; }
	}

	#region Event Args
	public class PositionDataChangedEventArgs :EventArgs
	{
		public PositionDataChangedEventArgs(Trade trade = null, Order order = null)
		{
			Trade = trade;
			Order = order;
		}

		/// <summary>The trade that changed</summary>
		public Trade Trade { get; private set; }

		/// <summary>The order that changed</summary>
		public Order Order { get; private set; }
	}
	#endregion
}
