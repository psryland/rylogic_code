using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pr.container;
using pr.extn;
using pr.maths;
using pr.util;

namespace Tradee
{
	public class Trade :IDisposable
	{
		// Notes:
		// - A Trade is a complete history of everything that happened in a trade from
		//   prep, to opening, to closing. The idea is to provide a decent diagnostic
		//   review of how a trade went.
		// - A trade can consist of multiple Orders/Positions (e.g hedged trades, etc).
		// - A trade is closed once all Orders/Positions are closed or cancelled.
		// - A trade has an associated chart, used to display everything about the trade

		public Trade(MainModel model, Instrument instrument, ETradeType initial_trade_type, ChartUI chart = null)
		{
			Model = model;
			Instrument = instrument;
			Chart = chart ?? new ChartUI(Model, instrument, new ChartSettings());
			TradeElement = new TradeChartElement(this);
			EventHistory = new BindingSource<TradeEvent> { DataSource = new BindingListEx<TradeEvent>() };
			EventHistory.Add(new TradeEvent_Created());

			// Set the initial trade type
			TradeType = initial_trade_type;
		}
		public virtual void Dispose()
		{
			TradeElement = null;
			Chart = null;
		}

		/// <summary>The App logic</summary>
		public MainModel Model
		{
			get;
			private set;
		}

		/// <summary>The instrument being traded</summary>
		public Instrument Instrument
		{
			get { return m_instrument; }
			private set
			{
				if (m_instrument == value) return;
				m_instrument = value;
			}
		}
		private Instrument m_instrument;

		/// <summary>The chart that this trade is visualised on</summary>
		public ChartUI Chart
		{
			get { return m_impl_chart; }
			private set
			{
				if (m_impl_chart == value) return;
				if (m_impl_chart != null)
				{
					Debug.Assert(m_impl_chart.Trade == this);
					m_impl_chart.Trade = null;
					Util.Dispose(ref m_impl_chart);
				}
				m_impl_chart = value;
				if (m_impl_chart != null)
				{
					m_impl_chart.Trade = this;
					Model.Owner.AddChart(value);
				}
			}
		}
		private ChartUI m_impl_chart;

		/// <summary>The trade chart element</summary>
		public TradeChartElement TradeElement
		{
			get { return m_impl_trade_element; }
			private set
			{
				if (m_impl_trade_element == value) return;
				if (m_impl_trade_element != null)
				{
					m_impl_trade_element.Chart = null;
					Util.Dispose(ref m_impl_trade_element);
				}
				m_impl_trade_element = value;
				if (m_impl_trade_element != null)
				{
					m_impl_trade_element.Chart = Chart.ChartCtrl;
				}
			}
		}
		private TradeChartElement m_impl_trade_element;

		/// <summary>The symbol that this order is on</summary>
		public string Symbol
		{
			get { return Instrument.SymbolCode; }
		}

		/// <summary>The trade type</summary>
		public ETradeType TradeType
		{
			get { return m_trade_type; }
			set
			{
				if (m_trade_type == value) return;
				m_trade_type = value;
				OnTradeTypeChanged();
			}
		}
		private ETradeType m_trade_type;

		/// <summary>Raised whenever the trade type changes</summary>
		public event EventHandler TradeTypeChanged;
		protected virtual void OnTradeTypeChanged()
		{
			TradeTypeChanged.Raise(this);
		}

		/// <summary>State flags for this trade</summary>
		public EState State
		{
			get { return m_impl_state; }
			set
			{
				if (m_impl_state == value) return;
				Debug.Assert(Bit.AllSet(value, EState.Experimental) != Bit.AnySet(value, EState.PendingOrder|EState.ActivePosition|EState.Closed), "Experimental is mutually exclusive");
				var old = m_impl_state;
				var nue = value;
				m_impl_state = value;
				OnStateChanged(new StateChangedEventArgs(old, nue));
			}
		}
		[Flags] public enum EState
		{
			/// <summary>Set if the trade has no open, closed, or pending orders</summary>
			Experimental = 1 << 0,

			/// <summary>Set if there are pending orders with the broker</summary>
			PendingOrder = 1 << 1,

			/// <summary>Set if there are active positions associated with this trade</summary>
			ActivePosition = 1 << 2,

			/// <summary>Set if there are closed positions associated with this trade</summary>
			Closed = 1 << 3,
		}
		private EState m_impl_state;

		/// <summary>Raised when the value of 'State' changes</summary>
		public event EventHandler<StateChangedEventArgs> StateChanged;
		protected virtual void OnStateChanged(StateChangedEventArgs args)
		{
			StateChanged.Raise(this, args);
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

		/// <summary>Add an order/position to this trade</summary>
		public void AddOrder(ETradeType tt)
		{
		}

		/// <summary>Create the elements to display on the chart for this order</summary>
		private void CreateChartElements()
		{
		}
	}

	#region EventArgs
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
