using System;
using System.Collections.Generic;
using ExchApi.Common.JsonConverter;
using Newtonsoft.Json;

namespace Bittrex.API.DomainObjects
{
	public class MarketData
	{
		/// <summary></summary>
		[JsonProperty("MarketCurrency")]
		public string MarketCurrency { get; set; }

		/// <summary></summary>
		[JsonProperty("BaseCurrency")]
		public string BaseCurrency { get; set; }

		/// <summary></summary>
		[JsonProperty("MarketCurrencyLong")]
		public string MarketCurrencyLong { get; set; }

		/// <summary></summary>
		[JsonProperty("BaseCurrencyLong")]
		public string BaseCurrencyLong { get; set; }

		/// <summary></summary>
		[JsonProperty("MinTradeSize")]
		public double MinTradeSize { get; set; }

		/// <summary></summary>
		[JsonProperty("MarketName"), JsonConverter(typeof(ParseMethod<CurrencyPair>))]
		public CurrencyPair Pair { get; set; }

		/// <summary></summary>
		[JsonProperty("IsActive")]
		public bool IsActive { get; internal set; }

		/// <summary></summary>
		[JsonProperty("IsRestricted")]
		public bool IsRestricted { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Created")]
		public DateTimeOffset Created { get; private set; }

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
}
