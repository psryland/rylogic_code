namespace CoinFlip
{
	public enum EPlaceOrderType
	{
		/// <summary>Place the order at the current price, whatever it is</summary>
		Market,

		/// <summary>Place the order when the order book for an instrument has orders available at the given price level</summary>
		Limit,

		/// <summary>Place the order when the spot price of an instrument reaches a given price level. </summary>
		Stop,
	}
}
