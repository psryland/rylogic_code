namespace Poloniex.API
{
	/// <summary>Exception type for PoloniexAPI errors</summary>
	public class PoloniexException : System.Exception
	{
		public PoloniexException(EErrorCode code, string message)
			:base(message)
		{
			ErrorCode = code;
		}

		/// <summary></summary>
		public EErrorCode ErrorCode { get; }
	}
}
