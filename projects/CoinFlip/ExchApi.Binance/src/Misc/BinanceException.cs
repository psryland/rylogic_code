namespace Binance.API
{
	/// <summary>Exception type for PoloniexAPI errors</summary>
	public class BinanceException : System.Exception
	{
		public BinanceException(EErrorCode code, string message)
			: base(message)
		{
			ErrorCode = code;
		}

		/// <summary></summary>
		public EErrorCode ErrorCode { get; }
	}
}
