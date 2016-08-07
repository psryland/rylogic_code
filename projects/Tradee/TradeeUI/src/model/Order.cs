using System;
using System.ComponentModel;
using System.Diagnostics;
using pr.extn;
using pr.maths;
using pr.util;

namespace Tradee
{
	/// <summary>A single order to buy or sell</summary>
	[DebuggerDisplay("{SymbolCode} {State} {TradeType} {EntryPrice}")]
	public class Order :INotifyPropertyChanged ,IPosition
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
			EntryTimeUTC      = instr.Latest.TimestampUTC;
			ExpirationTimeUTC = instr.Latest.TimestampUTC + TimeSpan.FromDays(1);
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
		/// The order Expiration time in UTC.
		/// If the order is in EState Visualising, or PendingOrder then this is the time that the pending
		/// order should be cancelled. If Active, then this value is ignored. If Closed, then this is the order close time</summary>
		public DateTimeOffset ExpirationTimeUTC
		{
			[DebuggerStepThrough] get { return m_expiry_time; }
			set { SetProp(ref m_expiry_time, value, nameof(ExpirationTimeUTC)); }
		}
		private DateTimeOffset m_expiry_time;

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

		/// <summary>Raised when a property changes</summary>
		public event PropertyChangedEventHandler PropertyChanged;
		private void SetProp<T>(ref T prop, T value, params string[] names)
		{
			if (Equals(prop, value)) return;
			prop = value;
			foreach (var name in names)
				PropertyChanged.Raise(this, new PropertyChangedEventArgs(name));
		}

		/// <summary>Update the state of this order from 'position'</summary>
		public void Update(Position position)
		{
			Debug.Assert(Id == position.Id);
			TradeType         = position.TradeType;
			EntryPrice        = position.EntryPrice;
			EntryTimeUTC      = new DateTimeOffset(position.EntryTime, TimeSpan.Zero);
			ExpirationTimeUTC = ExpirationTimeUTC; // Don't change this for active positions
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
			Debug.Assert(Id == pending.Id);
			TradeType         = pending.TradeType;
			EntryPrice        = pending.EntryPrice;
			EntryTimeUTC      = EntryTimeUTC; // Don't change this for pending orders
			ExpirationTimeUTC = new DateTimeOffset(pending.ExpirationTime, TimeSpan.Zero);
			StopLossAbs       = pending.StopLossAbs;
			TakeProfitAbs     = pending.TakeProfitAbs;
			Volume            = pending.Volume;
			GrossProfit       = 0;
			NetProfit         = 0; 
			Commissions       = 0;
			Swap              = 0;
			Comment           = pending.Comment;
			State = Trade.EState.PendingOrder;
		}
	}
}
