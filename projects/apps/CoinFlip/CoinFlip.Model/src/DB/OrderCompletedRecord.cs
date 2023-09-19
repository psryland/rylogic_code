namespace CoinFlip.DB
{
	internal class OrderCompletedRecord
	{
		// Notes:
		//  - This type is used instead of 'OrderCompleted' because it doesn't have
		//    Unit<double> properties and DateTimeOffset's etc. It's easier to store
		//    in a DB table.

		public OrderCompletedRecord()
		{ }
		public OrderCompletedRecord(OrderCompleted order)
		{
			OrderId = order.OrderId;
			FundId = order.Fund.Id;
			TradeType = order.TradeType.ToString();
			Pair = order.Pair.Name;
		}

		/// <summary>The ID of the order that was completed</summary>
		public long OrderId { get; }

		/// <summary>The fund the order was associated with</summary>
		public string FundId { get; } = string.Empty;
		public Fund Fund => new(FundId);

		/// <summary>The direction of the trade</summary>
		public string TradeType { get; } = string.Empty;

		/// <summary>The name of the pair that was traded</summary>
		public string Pair { get; } = string.Empty;
	}
}
