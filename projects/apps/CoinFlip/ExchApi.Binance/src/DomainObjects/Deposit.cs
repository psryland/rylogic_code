using System;
using ExchApi.Common.JsonConverter;
using Newtonsoft.Json;

namespace Binance.API.DomainObjects
{
	public class Deposit
	{
		/// <summary></summary>
		[JsonProperty("insertTime"), JsonConverter(typeof(UnixMSToDateTimeOffset))]
		public DateTimeOffset InsertTime { get; set; }

		/// <summary>The amount deposited</summary>
		[JsonProperty("amount")]
		public decimal Amount { get; set; }

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

		/// <summary>The transaction id</summary>
		[JsonProperty("status"), JsonConverter(typeof(ToEnum<EDepositStatus>))]
		public EDepositStatus Status { get; set; }
	}
}
