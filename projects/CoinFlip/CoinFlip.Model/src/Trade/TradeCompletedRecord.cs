namespace CoinFlip
{
	internal class TradeCompletedRecord
	{
		// Notes:
		//  - This is a domain object used to stored a history of trades in a DB Table
		//  - This type is used instead of 'TradeCompleted' because it doesn't have
		//    Unit<double> properties and DateTimeOffset's etc. It's easier to store
		//    in a DB table.

		public TradeCompletedRecord()
		{ }
		public TradeCompletedRecord(TradeCompleted trade)
		{
			TradeId = trade.TradeId;
			OrderId = trade.OrderId;
			Created = trade.Created.Ticks;
			Updated = trade.Updated.Ticks;
			AmountIn = (double)(decimal)trade.AmountIn;
			AmountOut = (double)(decimal)trade.AmountOut;
			Commission = (double)(decimal)trade.Commission;
			CommissionCoin = trade.CommissionCoin.Symbol;
		}

		/// <summary>The trade id</summary>
		public long TradeId { get; set; }

		/// <summary>The id of the order that was filled (possible partially) by this trade</summary>
		public long OrderId { get; set; }

		/// <summary>When the trade occurred (in Ticks)</summary>
		public long Created { get; set; }

		/// <summary>When this trade was last updated from the server</summary>
		public long Updated { get; set; }

		/// <summary>The amount sold</summary>
		public double AmountIn { get; set; }

		/// <summary>The amount received</summary>
		public double AmountOut { get; set; }

		/// <summary>The amount charged as commission on the trade</summary>
		public double Commission { get; set; }

		/// <summary>The currency that the commission was charged in</summary>
		public string CommissionCoin { get; set; }
	}
}
