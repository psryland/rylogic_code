namespace CoinFlip
{
	/// <summary>Buy/Sell</summary>
	public enum ETradeType
	{
		/// <summary>Quote->Base, Buy, Bid, Long, the highest one on a chart</summary>
		Q2B,

		/// <summary>Base->Quote, Sell, Ask, Short, the lowest one on a chart</summary>
		B2Q,
	}
}
