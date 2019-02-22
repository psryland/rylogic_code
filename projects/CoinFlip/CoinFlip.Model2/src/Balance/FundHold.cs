using System;
using System.Diagnostics;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>A reserve of a certain amount of currency</summary>
	[DebuggerDisplay("{Volume}")]
	public class FundHold
	{
		public FundHold(Guid id, Unit<decimal> amount, Func<FundBalance, bool> still_needed)
		{
			Id = id;
			Volume = amount;
			StillNeeded = still_needed;
		}

		/// <summary>The Id of the reserver</summary>
		public Guid Id { get; }

		/// <summary>How much to reserve</summary>
		public Unit<decimal> Volume { get; }

		/// <summary>Callback function to test whether the reserve is still required</summary>
		public Func<FundBalance,bool> StillNeeded { get; set; }
	}
}
