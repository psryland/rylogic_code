using System;

namespace Bittrex.API
{
	/// <summary>Exception type for PoloniexAPI errors</summary>
	public class BittrexException : Exception
	{
		public BittrexException(EErrorCode code, string message)
			: base(message)
		{
			ErrorCode = code;
		}

		/// <summary></summary>
		public EErrorCode ErrorCode { get; }
	}
}
