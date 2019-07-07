using Rylogic.Attrib;

namespace CoinFlip
{
	/// <summary>Buy/Sell</summary>
	public enum ETradeType
	{
		// The bid price is what buyers are willing to pay for it.
		// The ask price is what sellers are willing to take for it.
		// If you are selling a stock, you are going to get the bid price.
		// if you are buying a stock you are going to get the ask price.

		/// <summary>Quote->Base, Buy Price, Bid, Long, the highest one on a chart</summary>
		[Desc("Quote→Base")] Q2B,

		/// <summary>Base->Quote, Sell Price, Ask, Short, the lowest one on a chart</summary>
		[Desc("Base→Quote")] B2Q,
	}
}
