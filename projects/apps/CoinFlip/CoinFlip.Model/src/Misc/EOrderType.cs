using Rylogic.Attrib;

namespace CoinFlip
{
	public enum EOrderType
	{
		/// <summary>Place the order at the current price, whatever it is</summary>
		[Desc("Market Price")] Market,

		/// <summary>Place the order when the order book for an instrument has orders available at the given price level</summary>
		[Desc("Limit Order")] Limit,

		/// <summary>Place the order when the spot price of an instrument reaches a given price level. </summary>
		[Desc("Stop Order")] Stop,
	}
}
