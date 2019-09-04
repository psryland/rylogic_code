namespace CoinFlip
{
	public class OrderRecord
	{
		// Notes:
		//  - This type persists details about a live order that is not available from the exchanges
		//    such as the fund the order is associated with.

		public OrderRecord()
		{ }
		public OrderRecord(Order order)
			:this(order.OrderId, order.Fund.Id, order.CreatorName)
		{}
		public OrderRecord(long order_id, string fund_id, string creator_name)
		{
			OrderId = order_id;
			FundId = fund_id;
			CreatorName = creator_name;
		}

		/// <summary>The order id</summary>
		public long OrderId { get; private set; }

		/// <summary>The fund the order is associated with</summary>
		public string FundId { get; private set; }

		/// <summary>The name of the entity that created the order</summary>
		public string CreatorName { get; private set; }
	}
}