namespace Bittrex.API
{
	/// <summary>Represents the type of an order.</summary>
	public enum EOrderSide
	{
		/// <summary>A.k.a Q2B, Bid, Short</summary>
		Buy,
		LIMIT_BUY = Buy,

		/// <summary>A.k.a B2Q, Ask, Long</summary>
		Sell,
		LIMIT_SELL = Sell,
	}

	/// <summary>Error codes to distinguish replies</summary>
	public enum EErrorCode
	{
		Success,
		Failure,
		TooMuchDataRequested,
		ReplyWasNotAJsonObject,
	}
}
