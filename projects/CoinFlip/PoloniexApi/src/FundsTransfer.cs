using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Poloniex.API
{
	[DebuggerDisplay("Deposits={Deposits.Count} Withdrawals={Withdrawals.Count}")]
	public class FundsTransfer
	{
		public FundsTransfer()
		{
			Deposits = new List<Deposit>();
			Withdrawals = new List<Withdrawal>();
		}

		/// <summary>Deposits</summary>
		[JsonProperty("deposits")] public List<Deposit> Deposits { get; private set; }

		/// <summary></summary>
		[JsonProperty("withdrawals")] public List<Withdrawal> Withdrawals { get; private set; }
	}

	/// <summary></summary>
	public class Deposit
	{
		/// <summary>The currency deposited</summary>
		[JsonProperty("currency")]
		public string Currency { get; private set; }

		/// <summary>The address that was used for the deposit</summary>
		[JsonProperty("address")]
		public string Address { get; private set; }

		/// <summary></summary>
		[JsonProperty("amount")]
		public decimal Amount { get; private set; }

		/// <summary></summary>
		[JsonProperty("confirmations")]
		public int Confirmations { get; private set; }

		/// <summary></summary>
		[JsonProperty("txid")]
		public string TransactionId { get; private set; }

		/// <summary>Timestamp</summary>
		public DateTimeOffset Timestamp { get; private set; }
		[JsonProperty("timestamp")] private ulong TimestampInternal
		{
			set { Timestamp = Misc.ToDateTimeOffset(value); }
		}

		/// <summary></summary>
		[JsonProperty("status")]
		public string Status { get; private set; }
	}

	/// <summary></summary>
	public class Withdrawal
	{
		/// <summary></summary>
		[JsonProperty("withdrawalNumber")]
		public long WithdrawalNumber { get; private set; }

		/// <summary>The currency deposited</summary>
		[JsonProperty("currency")]
		public string Currency { get; private set; }

		/// <summary>The address that was used for the deposit</summary>
		[JsonProperty("address")]
		public string Address { get; private set; }

		/// <summary></summary>
		[JsonProperty("amount")]
		public decimal Amount { get; private set; }

		/// <summary>Timestamp</summary>
		public DateTimeOffset Timestamp { get; private set; }
		[JsonProperty("timestamp")] private ulong TimestampInternal
		{
			set { Timestamp = Misc.ToDateTimeOffset(value); }
		}

		/// <summary></summary>
		[JsonProperty("status")]
		public string Status { get; private set; }

		/// <summary></summary>
		[JsonProperty("ipAddress")]
		public string IPAddress { get; private set; }

	}
}
