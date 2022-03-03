using System;
using ExchApi.Common.JsonConverter;
using Newtonsoft.Json;

namespace Binance.API.DomainObjects
{
	public class Withdrawal
	{
		/// <summary>The transaction id</summary>
		[JsonProperty("id")]
		public string? Id { get; set; }

		/// <summary>The amount deposited</summary>
		[JsonProperty("amount")]
		public decimal Amount { get; set; }

		/// <summary>The amount deposited</summary>
		[JsonProperty("transactionFee")]
		public decimal TransactionFee { get; set; }

		/// <summary>The address to withdraw to</summary>
		[JsonProperty("address")]
		public string Address
		{
			get => m_address ?? string.Empty;
			set => m_address = value;
		}
		private string? m_address;

		/// <summary>The currency deposited</summary>
		[JsonProperty("asset")]
		public string Asset
		{
			get => m_asset ?? string.Empty;
			set => m_asset = value;
		}
		private string? m_asset;

		/// <summary>The transaction id</summary>
		[JsonProperty("txId")]
		public string TxId
		{
			get => m_txid ?? string.Empty;
			set => m_txid = value;
		}
		private string? m_txid;

		/// <summary></summary>
		[JsonProperty("applyTime"), JsonConverter(typeof(UnixMSToDateTimeOffset))]
		public DateTimeOffset ApplyTime { get; set; }

		/// <summary>The transaction id</summary>
		[JsonProperty("status"), JsonConverter(typeof(ToEnum<EWithdrawalStatus>))]
		public EWithdrawalStatus Status { get; set; }
	}
}
