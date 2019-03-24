using System;
using System.Diagnostics;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>Basic record of a trade</summary>
	[DebuggerDisplay("{Description,nq}")]
	public class TradeRecord
	{
		// Notes:
		//  - This is a domain object used to stored a history of trades in a DB Table

		public TradeRecord()
		{ }
		public TradeRecord(TradeCompleted his)
		{
			TradeId = his.TradeId;
			OrderId = his.OrderId;
			Created = his.Created.Ticks;
			Updated = his.Updated.Ticks;
			Pair = his.Pair.Name;
			TradeType = his.TradeType.ToString();
			PriceQ2B = (double)(decimal)his.PriceQ2B;
			AmountBase = (double)(decimal)his.AmountBase;
			CommissionQuote = (double)(decimal)his.CommissionQuote;
		}

		/// <summary>The trade id</summary>
		public long TradeId { get; set; }

		/// <summary>The id of the order that was filled (possible partially) by this trade</summary>
		public long OrderId { get; set; }

		/// <summary>When the trade occurred (in Ticks)</summary>
		public long Created { get; set; }

		/// <summary>When this trade was last updated from the server</summary>
		public long Updated { get; set; }

		/// <summary>The name of the pair that was traded</summary>
		public string Pair { get; set; }

		/// <summary>The direction of the trade</summary>
		public string TradeType { get; set; }

		/// <summary>The price that the trade occurred at</summary>
		public double PriceQ2B { get; set; }

		/// <summary>The amount traded (in base currency)</summary>
		public double AmountBase { get; set; }

		/// <summary>The amount charged as commission on the trade</summary>
		public double CommissionQuote { get; set; }

		/// <summary>Convert from DB record to 'TradeCompleted'</summary>
		public TradeCompleted ToTradeCompleted(Exchange exch)
		{
			var pair = exch.Pairs[Pair];
			var tt = Enum<ETradeType>.Parse(TradeType);
			var created = new DateTimeOffset(Created, TimeSpan.Zero);
			var updated = new DateTimeOffset(Updated, TimeSpan.Zero);
			var price_q2b = ((decimal)PriceQ2B)._(pair.RateUnits);
			var amount_base = ((decimal)AmountBase)._(pair.Base);
			var commission_quote = ((decimal)CommissionQuote)._(pair.Quote);
			return new TradeCompleted(OrderId, TradeId, pair, tt, price_q2b, amount_base, commission_quote, created, updated);
		}
	}
}
