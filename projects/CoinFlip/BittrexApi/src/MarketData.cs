using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Bittrex.API
{
	public class Market
	{
		/// <summary></summary>
		[JsonProperty("MarketCurrency")]
		public string MarketCurrency { get; internal set; }

		/// <summary></summary>
		[JsonProperty("BaseCurrency")]
		public string BaseCurrency { get; internal set; }

		/// <summary></summary>
		[JsonProperty("MarketCurrencyLong")]
		public string MarketCurrencyLong { get; internal set; }

		/// <summary></summary>
		[JsonProperty("BaseCurrencyLong")]
		public string BaseCurrencyLong { get; internal set; }

		/// <summary></summary>
		[JsonProperty("MinTradeSize")]
		public double MinTradeSize { get; internal set; }

		/// <summary></summary>
		public CurrencyPair Pair { get; private set; }
		[JsonProperty("MarketName")] private string MarketName
		{
			set { Pair = CurrencyPair.Parse(value); }
		}

		/// <summary></summary>
		[JsonProperty("IsActive")]
		public bool IsActive { get; internal set; }

		/// <summary></summary>
		public DateTimeOffset CreationDate { get; private set; }
		[JsonProperty("Created")] private string CreatedInternal
		{
			set { CreationDate = DateTimeOffset.Parse(value); }
		}

		/// <summary></summary>
		[JsonProperty("Notice")]
		public string Notice { get; internal set; }

		/// <summary></summary>
		[JsonProperty("IsSponsored")]
		public string IsSponsored { get; internal set; }

		/// <summary></summary>
		[JsonProperty("LogoUrl")]
		public string LogoUrl { get; internal set; }
	}

	public class MarketsResponse
	{
		/// <summary></summary>
		[JsonProperty("success")] public bool Success { get; internal set; }

		/// <summary></summary>
		[JsonProperty("message")] public string Message { get; internal set; }

		/// <summary></summary>
		[JsonProperty("result")] public List<Market> Data { get; internal set; }
	}
}
