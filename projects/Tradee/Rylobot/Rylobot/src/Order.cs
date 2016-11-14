using System.Diagnostics;
using cAlgo.API;
using cAlgo.API.Internals;
using pr.maths;

namespace Rylobot
{

	/// <summary>
	/// A helper for treating Positions, PendingOrders, or virtual trades as one type.
	/// Used when calculating maximum risk/profit.</summary>
	[DebuggerDisplay("{TradeType} ep={EntryPrice} sl={StopLoss} tp={TakeProfit} volume={Volume} active={TreatAsActive}")]
	public class Order
	{
		public Order(Position pos, bool active = true)
		{
			Position = pos;
			TreatAsActive = active;
		}
		public Order(PendingOrder ord, bool active = false)
		{
			PendingOrder = ord;
			TreatAsActive = false;
		}
		public Order(Trade trd, bool active)
		{
			Trade = trd;
			TreatAsActive = active;
		}

		public Position     Position     { get; private set; }
		public PendingOrder PendingOrder { get; private set; }
		public Trade        Trade        { get; private set; }

		/// <summary>Unique order id</summary>
		public int Id
		{
			get
			{
				return
					Position     != null ? Position    .Id :
					PendingOrder != null ? PendingOrder.Id :
					Trade        != null ? Trade       .Id :
					-1;
			}
		}

		/// <summary>The trade type, buy or sell</summary>
		public TradeType TradeType
		{
			get
			{
				return
					Position     != null ? Position    .TradeType :
					PendingOrder != null ? PendingOrder.TradeType :
					Trade        != null ? Trade       .TradeType :
					(TradeType)-1;
			}
		}

		/// <summary>The symbol being traded</summary>
		public string SymbolCode
		{
			get
			{
				return
					Position     != null ? Position    .SymbolCode :
					PendingOrder != null ? PendingOrder.SymbolCode :
					Trade        != null ? Trade       .Instrument.SymbolCode :
					string.Empty;
			}
		}

		/// <summary>The entry price of the trade</summary>
		public QuoteCurrency EntryPrice
		{
			get
			{
				return
					Position     != null ? Position    .EntryPrice :
					PendingOrder != null ? PendingOrder.TargetPrice :
					Trade        != null ? Trade       .EP :
					0;
			}
		}

		/// <summary>The stop loss value (absolute, in quote currency)</summary>
		public QuoteCurrency StopLoss
		{
			get
			{
				return
					Position     != null && Position    .StopLoss != null ? Position    .StopLoss.Value :
					PendingOrder != null && PendingOrder.StopLoss != null ? PendingOrder.StopLoss.Value :
					Trade        != null && Trade.SL != Trade.EP          ? Trade       .SL :
					(TradeType == TradeType.Buy ? double.NegativeInfinity : double.PositiveInfinity);
			}
		}

		/// <summary>The take profit value (absolute, in quote currency)</summary>
		public QuoteCurrency TakeProfit
		{
			get
			{
				return
					Position     != null && Position    .TakeProfit != null ? Position    .TakeProfit.Value :
					PendingOrder != null && PendingOrder.TakeProfit != null ? PendingOrder.TakeProfit.Value :
					Trade        != null && Trade.TP != Trade.EP            ? Trade       .TP :
					(TradeType == TradeType.Buy ? double.PositiveInfinity : double.NegativeInfinity);
			}
		}

		/// <summary>The volume traded</summary>
		public long Volume
		{
			get
			{
				return
					Position     != null ? Position    .Volume :
					PendingOrder != null ? PendingOrder.Volume :
					Trade        != null ? Trade       .Volume :
					0;
			}
		}

		/// <summary>A string tag for the trade</summary>
		public string Label
		{
			get
			{
				return
					Position     != null ? Position    .Label :
					PendingOrder != null ? PendingOrder.Label :
					Trade        != null ? Trade       .Label :
					string.Empty;
			}
		}

		/// <summary>
		/// Return the stop loss as a signed quote price value relative to the entry price.
		/// Positive values mean on the losing side (e.g. buy => sign = +1, entry_price - sign*SL = lower price)
		/// Negative values mean on the winning side (e.g. buy => sign = +1, entry_price - sign*SL = higher price)
		/// 0 means no stop loss</summary>
		public QuoteCurrency StopLossRel
		{
			get { return TradeType.Sign() * (EntryPrice - StopLoss); }
		}

		/// <summary>
		/// Return the take profit as a signed price value relative to the entry price.
		/// Positive values mean on the winning side (e.g. buy => sign = +1, entry_price + sign*TP = higher price)
		/// Negative values mean on the losing side (e.g. buy => sign = +1, entry_price + sign*TP = lower price)
		/// 0 means no take profit</summary>
		public QuoteCurrency TakeProfitRel
		{
			get { return TradeType.Sign() * (TakeProfit - EntryPrice); }
		}

		/// <summary>Return the value (in quote currency) of this order when the price is at 'price'</summary>
		public QuoteCurrency ValueAt(QuoteCurrency price, bool consider_sl, bool consider_tp)
		{
			var sign = TradeType.Sign();

			// If 'price' is beyond the stop loss, clamp at the stop loss value
			if (consider_sl && StopLoss != null && Maths.Sign((double)(StopLoss - price)) == sign)
				price = StopLoss;

			// If 'price' is beyond the take profit, clamp at the take profit value
			if (consider_tp && TakeProfit != null && Maths.Sign((double)(price - TakeProfit)) == sign)
				price = TakeProfit;

			// Return the position value in quote currency
			var dprice = price - EntryPrice;
			return sign * dprice * Volume;
		}

		/// <summary>A flag for orders that causes them to be considered as if they are active positions</summary>
		public bool TreatAsActive { get; set; }
	}
}
