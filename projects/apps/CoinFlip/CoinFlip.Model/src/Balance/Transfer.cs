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

		public Transfer(string transaction_id, ETransfer type, Coin coin, Unit<decimal> amount, DateTimeOffset created, EStatus status)
		{
			TransactionId = transaction_id;
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
		public Unit<decimal> Amount { get; }

		/// <summary>The timestamp of the transfer</summary>
		public DateTimeOffset Created { get; }

		/// <summary>The transaction status</summary>
		public EStatus Status { get; private set; }

		/// <summary>The exchange that this transfer occurred on</summary>
		public Exchange Exchange => Coin.Exchange;

		/// <summary>String description of the transfer</summary>
		public string Description => $"{Type} {Coin} {Amount.ToString(Coin.Meta.SD, true)}";

		/// <summary>Update the state of this order (with data received from the exchange)</summary>
		public void Update(Transfer update)
		{
			if (TransactionId != update.TransactionId )
				throw new Exception($"Update is not for this transfer");
			if (Coin != update.Coin)
				throw new Exception($"Update cannot change the asset");
			if (Type != update.Type)
				throw new Exception($"Update cannot change transfer type");
			if (Amount != update.Amount)
				throw new Exception($"Update cannot change transfer amount");

			// Update fields
			Status = update.Status;
		}

		/// <summary>Transfer transaction status</summary>
		public enum EStatus
		{
			Unknown,
			Complete,
			Pending,
			Cancelled,
			Failed,
		}
	}
}
