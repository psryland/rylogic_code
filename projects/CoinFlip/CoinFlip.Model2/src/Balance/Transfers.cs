using System.Collections.Generic;
using System.Diagnostics;

namespace CoinFlip
{
	/// <summary>A collection of funds transfers</summary>
	[DebuggerDisplay("Deposits={Deposits.Count} Withdrawals={Withdrawals.Count}")]
	public class Transfers
	{
		public Transfers(Exchange exch)
		{
			Exchange = exch;
			Deposits = new List<Transfer>();
			Withdrawals = new List<Transfer>();
		}

		/// <summary>The exchange on which the transfers occurred</summary>
		public Exchange Exchange { get; }

		/// <summary>Deposits into the account on this exchange</summary>
		public List<Transfer> Deposits { get; }

		/// <summary>Withdrawals from the account on this exchange</summary>
		public List<Transfer> Withdrawals { get; }
	}
}
