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

		public Transfer(string id, ETransfer type, Coin coin, Unit<decimal> amount, long timestamp, EStatus status)
		{
			TransactionId = id;
			Type          = type;
			Coin          = coin;
			Amount        = amount;
			Timestamp     = timestamp;
			Status        = status;
		}

		/// <summary>Unique Id for the transaction</summary>
		public string TransactionId { get; }

		/// <summary>Transfer direction</summary>
		public ETransfer Type { get; }

		/// <summary>The currency moved</summary>
		public Coin Coin { get; }

		/// <summary>The amount moved</summary>
		public Unit<decimal> Amount { get; }

		/// <summary>The timestamp of the transfer (in ticks)</summary>
		public long Timestamp { get; }
		public DateTimeOffset TimestampUTC => new DateTimeOffset(Timestamp, TimeSpan.Zero);

		/// <summary>The transaction status</summary>
		public EStatus Status { get; }

		/// <summary>String description of the transfer</summary>
		public string Description => $"{Type} {Coin} {Amount.ToString("G8", true)}";

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
