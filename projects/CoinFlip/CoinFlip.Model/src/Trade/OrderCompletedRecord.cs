namespace CoinFlip
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
		public long OrderId { get; private set; }

		/// <summary>The fund the order was associated with</summary>
		public string FundId { get; private set; }
		public Fund Fund => new Fund(FundId);

		/// <summary>The direction of the trade</summary>
		public string TradeType { get; private set; }

		/// <summary>The name of the pair that was traded</summary>
		public string Pair { get; private set; }
	}
}
