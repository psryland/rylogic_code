using System;
using System.Diagnostics;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>A reserve of a certain amount of currency</summary>
	[DebuggerDisplay("{Volume}")]
	public class FundHold
	{
		public FundHold(Guid id, Unit<double> amount, Func<IBalance, bool> still_needed)
		{
			Id = id;
			Volume = amount;
			StillNeeded = still_needed;
		}

		/// <summary>The Id of the reserver</summary>
		public Guid Id { get; }

		/// <summary>How much to reserve</summary>
		public Unit<double> Volume { get; }

		/// <summary>Callback function to test whether the reserve is still required</summary>
		public Func<IBalance, bool> StillNeeded { get; set; }
	}
}
