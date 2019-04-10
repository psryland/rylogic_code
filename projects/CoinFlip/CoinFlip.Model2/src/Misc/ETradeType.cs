using Rylogic.Attrib;

namespace CoinFlip
{
	/// <summary>Buy/Sell</summary>
	public enum ETradeType
	{
		/// <summary>Quote->Base, Buy, Bid, Long, the highest one on a chart</summary>
		[Desc("Quote→Base")] Q2B,

		/// <summary>Base->Quote, Sell, Ask, Short, the lowest one on a chart</summary>
		[Desc("Base→Quote")] B2Q,
	}
}
