namespace CoinFlip
{
	internal class TradeRecord
	{
		// Notes:
		//  - This is a domain object used to stored a history of trades in a DB Table
		//  - This type is used instead of 'TradeCompleted' because it doesn't have
		//    Unit<decimal> properties and DateTimeOffset's etc. It's easier to store
		//    in a DB table.
		public TradeRecord()
		{ }
		public TradeRecord(TradeCompleted trade)
		{
			TradeId = trade.TradeId;
			OrderId = trade.OrderId;
			Created = trade.Created.Ticks;
			Updated = trade.Updated.Ticks;
			PriceQ2B = (double)(decimal)trade.PriceQ2B;
			AmountBase = (double)(decimal)trade.AmountBase;
			CommissionQuote = (double)(decimal)trade.CommissionQuote;
		}

		/// <summary>The trade id</summary>
		public long TradeId { get; set; }

		/// <summary>The id of the order that was filled (possible partially) by this trade</summary>
		public long OrderId { get; set; }

		/// <summary>When the trade occurred (in Ticks)</summary>
		public long Created { get; set; }

		/// <summary>When this trade was last updated from the server</summary>
		public long Updated { get; set; }

		/// <summary>The price that the trade occurred at</summary>
		public double PriceQ2B { get; set; }

		/// <summary>The amount traded (in base currency)</summary>
		public double AmountBase { get; set; }

		/// <summary>The amount charged as commission on the trade</summary>
		public double CommissionQuote { get; set; }
	}
}
