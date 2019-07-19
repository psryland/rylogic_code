using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Bittrex.API.DomainObjects
{
	public class Transfer
	{
		/// <summary></summary>
		[JsonProperty("PaymentUuid")]
		public string PaymentUuid { get; private set; }

		/// <summary>The currency deposited</summary>
		[JsonProperty("currency")]
		public string Currency { get; private set; }

		/// <summary></summary>
		[JsonProperty("amount")]
		public double Amount { get; private set; }

		/// <summary>The address that was used for the deposit</summary>
		[JsonProperty("address")]
		public string Address { get; private set; }

		/// <summary>Timestamp</summary>
		[JsonProperty("Opened")]
		public DateTimeOffset Timestamp { get; private set; }

		/// <summary></summary>
		[JsonProperty("Authorized")]
		public bool Authorized { get; private set; }

		/// <summary></summary>
		[JsonProperty("PendingPayment")]
		public bool PendingPayment { get; private set; }

		/// <summary></summary>
		[JsonProperty("TxCost")]
		public double TxCost { get; private set; }

		/// <summary></summary>
		[JsonProperty("TxId")]
		public string TxId { get; private set; }

		/// <summary></summary>
		[JsonProperty("Canceled")]
		public bool Cancelled { get; private set; }

		/// <summary></summary>
		[JsonProperty("InvalidAddress")]
		public bool InvalidAddress { get; private set; }
	}
}
