using System;
using System.Diagnostics;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>A single deposit or withdrawal</summary>
	[DebuggerDisplay("{Description,nq}")]
	public class Transfer
	{
		// Notes:
		//  - A transfer of funds on or off an exchange (i.e. deposit or withdrawal)

		public Transfer(string id, ETransfer type, Coin coin, Unit<double> amount, DateTimeOffset created, EStatus status)
		{
			TransactionId = id;
			Type          = type;
			Coin          = coin;
			Amount        = amount;
			Created       = created;
			Status        = status;
		}

		/// <summary>Unique Id for the transaction</summary>
		public string TransactionId { get; }

		/// <summary>Transfer direction</summary>
		public ETransfer Type { get; }

		/// <summary>The currency moved</summary>
		public Coin Coin { get; }

		/// <summary>The amount moved</summary>
		public Unit<double> Amount { get; }

		/// <summary>The timestamp of the transfer (in ticks)</summary>
		public DateTimeOffset Created { get; }

		/// <summary>The transaction status</summary>
		public EStatus Status { get; }

		/// <summary>String description of the transfer</summary>
		public string Description => $"{Type} {Coin} {Amount.ToString(Coin.Meta.SD, true)}";

		/// <summary>Transfer transaction status</summary>
		public enum EStatus
		{
			Unknown,
			Complete,
			Pending,
			Cancelled,
		}
	}
}
