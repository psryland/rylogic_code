using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pr.util;

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
		public Exchange Exchange { get; private set; }

		/// <summary>Deposits into the account on this exchange</summary>
		public List<Transfer> Deposits { get; private set; }

		/// <summary>Withdrawals from the account on this exchange</summary>
		public List<Transfer> Withdrawals { get; private set; }
	}

	/// <summary>A single deposit or withdrawal</summary>
	[DebuggerDisplay("{Description,nq}")]
	public class Transfer
	{
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
		public string TransactionId { get; private set; }

		/// <summary>Transfer direction</summary>
		public ETransfer Type { get; private set; }

		/// <summary>The currency moved</summary>
		public Coin Coin { get; private set; }

		/// <summary>The amount moved</summary>
		public Unit<decimal> Amount { get; private set; }

		/// <summary>The timestamp of the transfer (in ticks)</summary>
		public long Timestamp { get; private set; }
		public DateTimeOffset TimestampUTC
		{
			get { return new DateTimeOffset(Timestamp, TimeSpan.Zero); }
			set { Timestamp = value.Ticks; }
		}

		/// <summary>The transaction status</summary>
		public EStatus Status { get; private set; }
		public enum EStatus
		{
			Unknown,
			Complete,
			Pending,
			Cancelled,
		}

		/// <summary>String description of the transfer</summary>
		public string Description
		{
			get { return $"{Type} {Coin} {Amount.ToString("G8",true)}"; }
		}
	}
}
