using System;
using System.Diagnostics;
using cAlgo.API;
using pr.maths;

namespace Rylobot
{
	[DebuggerDisplay("{TradeType} ep={EP} sl={SL} tp={TP} volume={Volume} active={TreatAsActive}")]
	public class Order :ITrade
	{
		// Notes:
		//  A helper for treating Positions, PendingOrders, or Trades as one type.
		//  Used when calculating maximum risk/profit.

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
		public Order(ITrade trd, bool active)
		{
			Trade = trd;
			TreatAsActive = active;
		}

		public Position     Position     { get; private set; }
		public PendingOrder PendingOrder { get; private set; }
		public ITrade       Trade        { get; private set; }

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
					Trade        != null ? Trade       .SymbolCode :
					string.Empty;
			}
		}

		/// <summary>The sign of this order</summary>
		public int Sign
		{
			get { return TradeType.Sign(); }
		}

		/// <summary>The entry price of the trade</summary>
		public QuoteCurrency EP
		{
			get
			{
				return
					Position     != null ? (QuoteCurrency)Position    .EntryPrice :
					PendingOrder != null ? (QuoteCurrency)PendingOrder.TargetPrice :
					Trade        != null ? (QuoteCurrency)Trade       .EP :
					(QuoteCurrency)0;
			}
		}

		/// <summary>The stop loss value (absolute, in quote currency)</summary>
		public QuoteCurrency? SL
		{
			get
			{
				return
					Position     != null ? (QuoteCurrency?)Position    .StopLoss :
					PendingOrder != null ? (QuoteCurrency?)PendingOrder.StopLoss :
					Trade        != null ? (QuoteCurrency?)Trade       .SL       :
					(TradeType == TradeType.Buy ? (QuoteCurrency?)double.NegativeInfinity : (QuoteCurrency?)double.PositiveInfinity);
			}
		}

		/// <summary>The take profit value (absolute, in quote currency)</summary>
		public QuoteCurrency? TP
		{
			get
			{
				return
					Position     != null ? (QuoteCurrency?)Position    .TakeProfit :
					PendingOrder != null ? (QuoteCurrency?)PendingOrder.TakeProfit :
					Trade        != null ? (QuoteCurrency?)Trade       .TP         :
					(TradeType == TradeType.Buy ? (QuoteCurrency?)double.PositiveInfinity : (QuoteCurrency?)double.NegativeInfinity);
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
			get { return SL != null ? TradeType.Sign() * (EP - SL.Value) : 0; }
		}

		/// <summary>
		/// Return the take profit as a signed price value relative to the entry price.
		/// Positive values mean on the winning side (e.g. buy => sign = +1, entry_price + sign*TP = higher price)
		/// Negative values mean on the losing side (e.g. buy => sign = +1, entry_price + sign*TP = lower price)
		/// 0 means no take profit</summary>
		public QuoteCurrency TakeProfitRel
		{
			get { return TP != null ? TradeType.Sign() * (TP.Value - EP) : 0; }
		}

		/// <summary>Return the value (in quote currency) of this order when the price is at 'price' (not scaled by volume)
		/// Positive values mean in profit. Negative values mean loss</summary>
		public QuoteCurrency ValueAt(QuoteCurrency price, bool consider_sl, bool consider_tp)
		{
			var sign = TradeType.Sign();

			// If 'price' is beyond the stop loss, clamp at the stop loss value
			if (consider_sl && SL != null && Maths.Sign(SL.Value - price) == sign)
				price = SL.Value;

			// If 'price' is beyond the take profit, clamp at the take profit value
			if (consider_tp && TP != null && Maths.Sign(price - TP.Value) == sign)
				price = TP.Value;

			// Return the position value in quote currency
			return sign * (price - EP);
		}

		/// <summary>Return the value of this order if it was to be closed at the given price tick</summary>
		public QuoteCurrency ValueAt(PriceTick price, bool consider_sl, bool consider_tp)
		{
			// Closing a Buy means selling to the highest *bid*er
			var p = TradeType == TradeType.Buy ? price.Bid : price.Ask;
			return ValueAt(p, consider_sl, consider_tp);
		}

		/// <summary>Return the normalised value ([-1,+1]) of this order at 'price'. Only valid if the order has SL and TP levels</summary>
		public double ValueFrac(PriceTick price)
		{
			if (SL == null || TP == null)
				return 0.0;

			var sign = TradeType.Sign();
			var win = Maths.Frac(EP, price.Price(+sign), TP.Value);
			var los = Maths.Frac(EP, price.Price(-sign), SL.Value);

			if (win < 0) return -los;
			if (los < 0) return +win;
			return win > los ? win : -los;
		}

		/// <summary>A flag for orders that causes them to be considered as if they are active positions</summary>
		public bool TreatAsActive { get; set; }
	}
}
