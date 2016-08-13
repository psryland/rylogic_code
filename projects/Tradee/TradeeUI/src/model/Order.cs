using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;
using pr.extn;
using pr.maths;
using pr.util;

namespace Tradee
{
	/// <summary>A single order to buy or sell</summary>
	[DebuggerDisplay("{SymbolCode} {State} {TradeType} {EntryPrice}")]
	public class Order :IPosition
	{
		// Notes:
		// - Represents a single buy or sell order with the broker.
		// - A trade can have more than one of these.
		// - An order can be pending or active.
		// - Orders don't have associated graphics because they may be part
		//   of an more complex trade
		// - Orders are low level application objects, they don't know about charts, accounts, etc.

		public Order(int id, Instrument instr, ETradeType trade_type, Trade.EState state)
		{
			Id                = id;
			Instrument        = instr;
			TradeType         = trade_type;
			EntryPrice        = 0;
			ExitPrice         = 0;
			EntryTimeUTC      = DefaultEntryTime;
			ExitTimeUTC       = DefaultExitTime;
			StopLossAbs       = 0;
			TakeProfitAbs     = 0;
			Volume            = 0;
			GrossProfit       = 0;
			NetProfit         = 0;
			Commissions       = 0;
			Swap              = 0;
			Comment           = string.Empty;
			State             = state;
		}

		/// <summary>Unique ID for the order</summary>
		public int Id
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>Application settings</summary>
		public Settings Settings
		{
			get { return Instrument.Model.Settings; }
		}

		/// <summary>The instrument being traded</summary>
		public Instrument Instrument
		{
			[DebuggerStepThrough] get;
			set;
		}

		/// <summary>Instrument symbol code</summary>
		public string SymbolCode
		{
			get { return Instrument.SymbolCode; }
		}

		/// <summary>The type of trade</summary>
		public ETradeType TradeType
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>The position entry price</summary>
		public double EntryPrice
		{
			[DebuggerStepThrough] get { return m_entry_price; }
			set { SetProp(ref m_entry_price, value, nameof(EntryPrice)); }
		}
		private double m_entry_price;

		/// <summary>The position exit price</summary>
		public double ExitPrice
		{
			[DebuggerStepThrough] get { return m_exit_price; }
			set { SetProp(ref m_exit_price, value, nameof(ExitPrice)); }
		}
		private double m_exit_price;

		/// <summary>
		/// The position entry time in UTC.
		/// If the order is in EState Visualising or PendingOrder then this is the time that the server
		/// would become aware of the trade (i.e. a pending order would be submitted). If Active or Closed
		/// then this is the time that the trade was triggered.</summary>
		public DateTimeOffset EntryTimeUTC
		{
			[DebuggerStepThrough] get { return m_entry_time; }
			set { SetProp(ref m_entry_time, value, nameof(EntryTimeUTC)); }
		}
		private DateTimeOffset m_entry_time;

		/// <summary>
		/// The position exit time in UTC. 
		/// In the Visualising and Pending states, this is the expiry time.
		/// For Active orders this property always returns a time in the future (based on instrument time frame).
		/// For Closed orders, this is the time that the position was closed.</summary>
		public DateTimeOffset ExitTimeUTC
		{
			[DebuggerStepThrough] get
			{
				if (State != Trade.EState.ActivePosition) return m_exit_time;
				var latest = DateTimeOffset_.Max(Instrument.Latest.TimestampUTC, EntryTimeUTC);
				var time_frame = Instrument.TimeFrame != ETimeFrame.None ? Instrument.TimeFrame : ETimeFrame.Hour1;
				return latest + Misc.TimeFrameToTimeSpan(Settings.Chart.ViewCandlesAhead/2, time_frame);
			}
			set { SetProp(ref m_exit_time, value, nameof(ExitTimeUTC)); }
		}
		private DateTimeOffset m_exit_time;

		/// <summary>The stop loss price (in base currency)</summary>
		public double StopLossAbs
		{
			[DebuggerStepThrough] get { return m_stop_loss; }
			set { SetProp(ref m_stop_loss, value, nameof(StopLossAbs)); }
		}
		private double m_stop_loss;

		/// <summary>The take profit price (in base currency)</summary>
		public double TakeProfitAbs
		{
			[DebuggerStepThrough] get { return m_take_profit; }
			set { SetProp(ref m_take_profit, value, nameof(TakeProfitAbs)); }
		}
		private double m_take_profit;

		/// <summary>
		/// The stop loss (in base currency) relative to the entry price.
		/// 0 means no stop loss, positive means on the losing side, negative means on the winning side.</summary>
		public double StopLossRel
		{
			get
			{
				switch (TradeType) {
				default: throw new Exception("Unknown trade type");
				case ETradeType.Long:  return EntryPrice - StopLossAbs;
				case ETradeType.Short: return StopLossAbs - EntryPrice;
				}
			}
			set
			{
				switch (TradeType) {
				default: throw new Exception("Unknown trade type");
				case ETradeType.Long:  StopLossAbs = EntryPrice - value; break;
				case ETradeType.Short: StopLossAbs = EntryPrice + value; break;
				}
			}
		}

		/// <summary>
		/// The take profit (in base currency) relative to the entry price.
		/// 0 means no take profit, positive means on the winning side, negative means on the losing side.</summary>
		public double TakeProfitRel
		{
			get
			{
				switch (TradeType) {
				default: throw new Exception("Unknown trade type");
				case ETradeType.Long:  return TakeProfitAbs - EntryPrice;
				case ETradeType.Short: return EntryPrice - TakeProfitAbs;
				}
			}
			set
			{
				switch (TradeType) {
				default: throw new Exception("Unknown trade type");
				case ETradeType.Long:  TakeProfitAbs = EntryPrice + value; break;
				case ETradeType.Short: TakeProfitAbs = EntryPrice - value; break;
				}
			}
		}

		/// <summary>The amount traded by the position</summary>
		public long Volume
		{
			[DebuggerStepThrough] get { return m_volume; }
			set { SetProp(ref m_volume, value, nameof(Volume)); }
		}
		private long m_volume;

		/// <summary>Gross profit accrued by the order associated with a position</summary>
		public double GrossProfit
		{
			[DebuggerStepThrough] get { return m_gross_profit; }
			set { SetProp(ref m_gross_profit, value, nameof(GrossProfit)); }
		}
		private double m_gross_profit;

		/// <summary>The Net profit of the position (in base currency)/summary>
		public double NetProfit
		{
			[DebuggerStepThrough] get { return m_net_profit; }
			set { SetProp(ref m_net_profit, value, nameof(NetProfit)); }
		}
		private double m_net_profit;

		/// <summary>Commission Amount of the request to trade one way(Buy/Sell) associated with this position.</summary>
		public double Commissions
		{
			[DebuggerStepThrough] get { return m_commissions; }
			set { SetProp(ref m_commissions, value, nameof(Commissions)); }
		}
		private double m_commissions;

		/// <summary>Swap is the overnight interest rate if any, accrued on the position.</summary>
		public double Swap
		{
			[DebuggerStepThrough] get { return m_swap; }
			set { SetProp(ref m_swap, value, nameof(Swap)); }
		}
		private double m_swap;

		/// <summary>Notes for the trade</summary>
		public string Comment
		{
			[DebuggerStepThrough] get { return m_comment; }
			set { SetProp(ref m_comment, value, nameof(Comment)); }
		}
		private string m_comment;

		/// <summary>The state of this order</summary>
		public Trade.EState State
		{
			get { return m_impl_state; }
			set
			{
				Debug.Assert(Bit.CountBits((int)value) == 1, "An order can only be in one state");
				Debug.Assert(!(m_impl_state == Trade.EState.ActivePosition && value == Trade.EState.PendingOrder), "Orders can't go from active back to pending");
				SetProp(ref m_impl_state, value, nameof(State));
			}
		}
		private Trade.EState m_impl_state;

		/// <summary>Raised when the order changes</summary>
		public event EventHandler Changed;
		protected virtual void OnChanged()
		{
			m_sig_changed = false;
			Changed.Raise(this);
		}
		protected void SignalChanged(object sender = null, EventArgs e = null)
		{
			if (m_sig_changed) return;
			m_sig_changed = true;
			Dispatcher.CurrentDispatcher.BeginInvoke(OnChanged);
		}
		private bool m_sig_changed;

		/// <summary>Set a property if different and raise 'Changed'</summary>
		private void SetProp<T>(ref T prop, T value, string name)
		{
			if (Equals(prop, value)) return;
			prop = value;
			SignalChanged();
		}

		/// <summary>The default entry time for an order</summary>
		private DateTimeOffset DefaultEntryTime
		{
			get { return Instrument.Latest.TimestampUTC; }
		}

		/// <summary>The default exit time for an order (at some point in the future)</summary>
		private DateTimeOffset DefaultExitTime
		{
			get { return Instrument.Latest.TimestampUTC + Misc.TimeFrameToTimeSpan(Settings.Chart.ViewCandlesAhead/2, Instrument.TimeFrame != ETimeFrame.None ? Instrument.TimeFrame : ETimeFrame.Hour1); }
		}

		/// <summary>Update the state of this order from 'position'</summary>
		public void Update(Position position)
		{
			if (m_updates_suspended != 0)
				return;

			Debug.Assert(Id == position.Id);
			TradeType         = position.TradeType;
			EntryPrice        = position.EntryPrice;
			ExitPrice         = this.ExitPrice;
			EntryTimeUTC      = new DateTimeOffset(position.EntryTime, TimeSpan.Zero);
			ExitTimeUTC       = this.ExitTimeUTC;
			StopLossAbs       = position.StopLossAbs;
			TakeProfitAbs     = position.TakeProfitAbs;
			Volume            = position.Volume;
			GrossProfit       = position.GrossProfit;
			NetProfit         = position.NetProfit; 
			Commissions       = position.Commissions;
			Swap              = position.Swap;
			Comment           = position.Comment;
			State             = Trade.EState.ActivePosition;
		}

		/// <summary>Update the state of this order from 'pending'</summary>
		public void Update(PendingOrder pending)
		{
			if (m_updates_suspended != 0)
				return;

			Debug.Assert(Id == pending.Id);
			TradeType         = pending.TradeType;
			EntryPrice        = pending.EntryPrice;
			ExitPrice         = this.ExitPrice;
			EntryTimeUTC      = this.EntryTimeUTC;
			ExitTimeUTC       = pending.ExpirationTime != 0 ? new DateTimeOffset(pending.ExpirationTime, TimeSpan.Zero) : DefaultExitTime;
			StopLossAbs       = pending.StopLossAbs;
			TakeProfitAbs     = pending.TakeProfitAbs;
			Volume            = pending.Volume;
			GrossProfit       = 0;
			NetProfit         = 0; 
			Commissions       = 0;
			Swap              = 0;
			Comment           = pending.Comment;
			State             = Trade.EState.PendingOrder;
		}

		/// <summary>Update the state of this order from 'closed'</summary>
		public void Update(ClosedOrder closed)
		{
			if (m_updates_suspended != 0)
				return;

			Debug.Assert(Id == closed.Id);
			TradeType         = closed.TradeType;
			EntryPrice        = closed.EntryPrice;
			ExitPrice         = closed.ExitPrice;
			EntryTimeUTC      = new DateTimeOffset(closed.EntryTimeUTC, TimeSpan.Zero);
			ExitTimeUTC       = new DateTimeOffset(closed.ExitTimeUTC , TimeSpan.Zero);
			StopLossAbs       = this.StopLossAbs;       // unchanged
			TakeProfitAbs     = this.TakeProfitAbs;     // unchanged
			Volume            = closed.Volume;
			GrossProfit       = closed.GrossProfit;
			NetProfit         = closed.NetProfit; 
			Commissions       = closed.Commissions;
			Swap              = closed.Swap;
			Comment           = closed.Comment;
			State             = Trade.EState.Closed;
		}

		/// <summary>Block updates to the state of this order</summary>
		public Scope SuspendUpdate()
		{
			return Scope.Create(() => ++m_updates_suspended, () => --m_updates_suspended);
		}
		private int m_updates_suspended;

		/// <summary>
		/// Push the state of this order to the trade data source.
		/// Only applies to trades in the PendingOrder or ActivePosition states.</summary>
		public void Commit()
		{
			switch (State)
			{
			case Trade.EState.None:
			case Trade.EState.Visualising:
			case Trade.EState.Closed:
				{
					// The Trade data source doesn't know about orders in this state
					// or, the order cannot be changed.
					break;
				}
			case Trade.EState.PendingOrder:
				{
					// Update the entry price, stop loss, take profit, expiry time
					break;
				}
			case Trade.EState.ActivePosition:
				{
					// Update the stop loss, take profit
					break;
				}
			}
		}
	}
}
