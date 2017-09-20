using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Cryptopia.API
{
	public class Balance
	{
		/// <summary></summary>
		[JsonProperty]
		public int CurrencyId { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public string Symbol { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal Total { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal Available { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal Unconfirmed { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal HeldForTrades { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal PendingWithdraw { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public string Address { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public string Status { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public string StatusMessage { get; set; }
	}

	public class Transaction
	{
		/// <summary></summary>
		[JsonProperty]
		public int Id { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public string Currency { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public string TxId { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public string Type { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal Amount { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public decimal Fee { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public string Status { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public int Confirmations { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public DateTime Timestamp { get; set; }
	}

	public class DepositAddress
	{
		/// <summary></summary>
		[JsonProperty]
		public string Currency { get; set; }

		/// <summary></summary>
		[JsonProperty]
		public string Address { get; set; }
	}

	public class BalanceResponse
	{
		/// <summary></summary>
		[JsonProperty("Success")] public bool Success { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Error")] public string Error { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Data")] public List<Balance> Data { get; set; }
	}

	public class TransactionResponse
	{
		/// <summary></summary>
		[JsonProperty("Success")] public bool Success { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Error")] public string Error { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Data")] public List<Transaction> Data { get; set; }
	}

	public class DepositAddressResponse
	{
		/// <summary></summary>
		[JsonProperty("Success")] public bool Success { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Error")] public string Error { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Data")] public DepositAddress Data { get; set; }
	}

	public class SubmitWithdrawResponse
	{
		/// <summary></summary>
		[JsonProperty("Success")] public bool Success { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Error")] public string Error { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Data")] public int? Data { get; set; }
	}
}
