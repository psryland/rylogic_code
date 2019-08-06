using System;
using System.Diagnostics;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>A reserve of a certain amount of currency</summary>
	[DebuggerDisplay("{Description,nq}")]
	public class FundHold
	{
		public FundHold(Guid id, long? order_id, Unit<double> amount, Func<IBalance, bool> still_needed)
		{
			Id = id;
			OrderId = order_id;
			Amount = amount;
			StillNeeded = still_needed;
		}

		/// <summary>The Id of the reserver</summary>
		public Guid Id { get; }

		/// <summary>The order that the hold is associated with (if known)</summary>
		public long? OrderId { get; set; }

		/// <summary>How much to reserve</summary>
		public Unit<double> Amount { get; }

		/// <summary>Callback function to test whether the reserve is still required</summary>
		public Func<IBalance, bool> StillNeeded { get; set; }

		/// <summary></summary>
		public string Description => $"[{OrderId}] {Amount}";
	}
}
