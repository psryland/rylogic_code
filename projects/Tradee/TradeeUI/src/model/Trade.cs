using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;
using pr.container;
using pr.extn;
using pr.util;

namespace Tradee
{
	[DebuggerDisplay("{SymbolCode} {State}")]
	public class Trade :IPosition, IDisposable
	{
		// Notes:
		// - A Trade is a set of Orders for an instrument that overlap in time.
		// - A Trade is a complete history of everything that happened in a trade from
		//   prep, to opening, to closing. The idea is to provide a decent diagnostic
		//   review of how a trade went.
		// - A trade can consist of multiple Orders/Positions (e.g hedged trades, etc).
		// - A trade is closed once all Orders/Positions are closed or cancelled.
		// - A trade has an associated chart, used to display everything about the trade
		// - Trades are low level application objects, they don't know about charts, accounts,
		//   etc. Only about orders.

		[Flags] public enum EState
		{
			None = 0,

			/// <summary>Set if the trade has no open, closed, or pending orders</summary>
			Visualising = 1 << 0,

			/// <summary>Set if there are pending orders with the broker</summary>
			PendingOrder = 1 << 1,

			/// <summary>Set if there are active positions associated with this trade</summary>
			ActivePosition = 1 << 2,

			/// <summary>Set if there are closed positions associated with this trade</summary>
			Closed = 1 << 3,
		}

		public Trade(MainModel model, Instrument instrument)
		{
			Id           = Guid.NewGuid();
			Model        = model;
			Instrument   = instrument;
			Orders       = new BindingSource<Order>      { DataSource = new BindingListEx<Order>(), PerItemClear = true };
			EventHistory = new BindingSource<TradeEvent> { DataSource = new BindingListEx<TradeEvent>() };

			// Create the trade
			EventHistory.Add(new TradeEvent_Created());
		}
		public virtual void Dispose()
		{
			Orders = null;
			Model = null;
		}

		/// <summary>Unique ID for this trade</summary>
		public Guid Id
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>The App logic</summary>
		public MainModel Model
		{
			[DebuggerStepThrough] get { return m_model; }
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{}
				m_model = value;
				if (m_model != null)
				{}
			}
		}
		private MainModel m_model;

		/// <summary>Application settings</summary>
		public Settings Settings
		{
			get { return Model.Settings; }
		}

		/// <summary>The orders associated with this trade</summary>
		public BindingSource<Order> Orders
		{
			// Note: The Trade doesn't own the orders, the owner is the 'Positions' object
			[DebuggerStepThrough] get { return m_orders; }
			private set
			{
				if (m_orders == value) return;
				if (m_orders != null)
				{
					m_orders.Clear();
					m_orders.ListChanging -= HandleOrderListChanging;
				}
				m_orders = value;
				if (m_orders != null)
				{
					m_orders.ListChanging += HandleOrderListChanging;
				}
			}
		}
		private BindingSource<Order> m_orders;

		/// <summary>The instrument being traded</summary>
		public Instrument Instrument
		{
			[DebuggerStepThrough] get { return m_instrument; }
			private set
			{
				if (m_instrument == value) return;
				m_instrument = value;
			}
		}
		private Instrument m_instrument;

		/// <summary>The symbol that this order is on</summary>
		public string SymbolCode
		{
			[DebuggerStepThrough] get { return Instrument.SymbolCode; }
		}

		/// <summary>The type of trade</summary>
		public ETradeType TradeType
		{
			get
			{
				var tt = ETradeType.None;
				if (Orders.Any(x => x.TradeType == ETradeType.Long)) tt |= ETradeType.Long;
				if (Orders.Any(x => x.TradeType == ETradeType.Short)) tt |= ETradeType.Short;
				return tt;
			}
		}

		/// <summary>State flags for this trade</summary>
		public EState State
		{
			get
			{
				var state = EState.None; 
				Orders.ForEach(x => state |= x.State);
				return state;
			}
		}

		/// <summary>The entry time of the earliest trade in the group (in UTC).</summary>
		public DateTimeOffset EntryTimeUTC
		{
			[DebuggerStepThrough] get { return m_entry_time ?? (m_entry_time = Orders.Min(x => x.EntryTimeUTC)).Value; }
		}
		private DateTimeOffset? m_entry_time;

		/// <summary>The exit time of the last trade in the group (in UTC)</summary>
		public DateTimeOffset ExitTimeUTC
		{
			[DebuggerStepThrough] get { return m_exit_time ?? (m_exit_time = Orders.Max(x => x.ExitTimeUTC)). Value; }
		}
		private DateTimeOffset? m_exit_time;

		/// <summary>Combined volume</summary>
		public long Volume
		{
			get { return Orders.Sum(x => x.Volume); }
		}

		/// <summary>Combined gross profit</summary>
		public double GrossProfit
		{
			get { return Orders.Sum(x => x.GrossProfit); }
		}

		/// <summary>Combined net profit</summary>
		public double NetProfit
		{
			get { return Orders.Sum(x => x.NetProfit); }
		}

		/// <summary>All the things that happened to this order since its creation</summary>
		public BindingSource<TradeEvent> EventHistory
		{
			get { return m_event_history; }
			private set
			{
				if (m_event_history == value) return;
				if (m_event_history != null)
				{
					m_event_history.ListChanging -= HandleEventHistoryListChanging;
				}
				m_event_history = value;
				if (m_event_history != null)
				{
					m_event_history.ListChanging += HandleEventHistoryListChanging;
				}
			}
		}
		private BindingSource<TradeEvent> m_event_history;

		/// <summary>Raised when the value of 'State' changes</summary>
		public event EventHandler<StateChangedEventArgs> StateChanged;
		protected virtual void OnStateChanged(StateChangedEventArgs args)
		{
			StateChanged.Raise(this, args);
		}

		/// <summary>Raised whenever the trade changes</summary>
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

		/// <summary>Set a property if different and raise 'Changed'</summary>
		private void SetProp<T>(ref T prop, T value, string names)
		{
			if (Equals(prop, value)) return;
			prop = value;
			SignalChanged();
		}

		/// <summary>Create the elements to display on the chart for this order</summary>
		private void CreateChartElements()
		{
		}

		/// <summary>Invalidate cache values that are based on Orders</summary>
		public void Invalidate()
		{
			m_entry_time = null;
			m_exit_time = null;
		}

		/// <summary>Handle orders being associated with this trade</summary>
		private void HandleOrderListChanging(object sender, ListChgEventArgs<Order> e)
		{
			switch (e.ChangeType)
			{
			case ListChg.Reset:
				{
					foreach (var order in Orders)
					{
						order.Changed -= SignalChanged;
						order.Changed += SignalChanged;
					}
					break;
				}
			case ListChg.ItemAdded:
				{
					e.Item.Changed += SignalChanged;
					break;
				}
			case ListChg.ItemRemoved:
				{
					e.Item.Changed -= SignalChanged;
					break;
				}
			}

			// Invalidate cached values based on Orders
			if (e.IsPostEvent)
				Invalidate();
		}

		/// <summary>Optimise the event history as it changes</summary>
		private void HandleEventHistoryListChanging(object sender, ListChgEventArgs<TradeEvent> e)
		{
			switch (e.ChangeType)
			{
			case ListChg.ItemAdded:
				{
					break;
				}
			}
		}
	}

	#region EventArgs
	public class TradeChangedEventArgs :EventArgs
	{
		public TradeChangedEventArgs(bool orders = false, ETradeType? trade_type = null)
		{
			Orders    = orders;
			TradeType = trade_type;
		}

		/// <summary>True when the collection of orders has changed</summary>
		public bool Orders { get; private set; }

		/// <summary>Trade type changed</summary>
		public ETradeType? TradeType { get; private set; }
	}

	public class StateChangedEventArgs :EventArgs
	{
		public StateChangedEventArgs(Trade.EState old_state, Trade.EState new_state)
		{
			OldState = old_state;
			NewState = new_state;
		}

		/// <summary>Out going trade state</summary>
		public Trade.EState OldState { get; private set; }

		/// <summary>New trade state</summary>
		public Trade.EState NewState { get; private set; }
	}
	#endregion
}
