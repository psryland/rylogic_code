using System;
using System.Collections.Generic;
using ExchApi.Common.JsonConverter;
using Newtonsoft.Json;

namespace Binance.API.DomainObjects
{
	public class BalancesData
	{
		public BalancesData()
		{
			Balances = new List<Balance>();
			UpdateTime = default;
		}
		public BalancesData(BalancesData rhs)
		{
			MakerCommission = rhs.MakerCommission;
			TakerCommission = rhs.TakerCommission;
			BuyerCommission = rhs.BuyerCommission;
			SellerCommission = rhs.SellerCommission;
			CanTrade = rhs.CanTrade;
			CanWithdraw = rhs.CanWithdraw;
			CanDeposit = rhs.CanDeposit;
			UpdateTime = rhs.UpdateTime;
			Balances = new List<Balance>(rhs.Balances);
		}

		/// <summary></summary>
		[JsonProperty("makerCommission")]
		public decimal MakerCommission { get; set; }

		/// <summary></summary>
		[JsonProperty("takerCommission")]
		public decimal TakerCommission { get; set; }

		/// <summary></summary>
		[JsonProperty("buyerCommission")]
		public decimal BuyerCommission { get; set; }

		/// <summary></summary>
		[JsonProperty("sellerCommission")]
		public decimal SellerCommission { get; set; }

		/// <summary></summary>
		[JsonProperty("canTrade")]
		public bool CanTrade { get; set; }

		/// <summary></summary>
		[JsonProperty("canWithdraw")]
		public bool CanWithdraw { get; set; }

		/// <summary></summary>
		[JsonProperty("canDeposit")]
		public bool CanDeposit { get; set; }

		/// <summary></summary>
		[JsonProperty("updateTime"), JsonConverter(typeof(UnixMSToDateTimeOffset))]
		public DateTimeOffset UpdateTime { get; set; }

		/// <summary></summary>
		[JsonProperty("balances")]
		public List<Balance> Balances { get; set; }

		public class Balance
		{
			/// <summary></summary>
			[JsonProperty("asset")]
			public string Asset
			{
				get => m_asset ?? string.Empty;
				set => m_asset = value;
			}
			private string? m_asset;

			/// <summary></summary>
			[JsonProperty("free")]
			public decimal Free { get; set; }

			/// <summary></summary>
			[JsonProperty("locked")]
			public decimal Locked { get; set; }

			/// <summary></summary>
			public decimal Total => Free + Locked;
    	}
	}
}
