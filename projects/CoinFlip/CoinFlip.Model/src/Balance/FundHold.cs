using System;
using System.Diagnostics;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>A reserve of a certain amount of currency</summary>
	[DebuggerDisplay("{Description,nq}")]
	public class FundHold
	{
		public FundHold(Guid id, long? order_id, Unit<decimal> amount, bool local)
		{
			Id = id;
			OrderId = order_id;
			Amount = amount;
			Local = local;
		}

		/// <summary>The Id of the reserver</summary>
		public Guid Id { get; }

		/// <summary>The order that the hold is associated with (if known)</summary>
		public long? OrderId { get; set; }

		/// <summary>How much to reserve</summary>
		public Unit<decimal> Amount { get; set; }

		/// <summary>True if this hold is not part of the held balance reported by the exchange</summary>
		public bool Local { get; set; }

		/// <summary></summary>
		public string Description => $"[{OrderId}] {Amount}";
	}
}
