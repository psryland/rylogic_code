using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Bittrex.API
{
	public class MarketsResponse
	{
		/// <summary></summary>
		[JsonProperty("success")] public bool Success { get; internal set; }

		/// <summary></summary>
		[JsonProperty("message")] public string Message { get; internal set; }

		/// <summary></summary>
		[JsonProperty("result")] public List<Market> Data { get; internal set; }

		public class Market
		{
			[JsonProperty("MarketCurrency")]
			public string MarketCurrency { get; internal set; }

			[JsonProperty("BaseCurrency")]
			public string BaseCurrency { get; internal set; }

			[JsonProperty("MarketCurrencyLong")]
			public string MarketCurrencyLong { get; internal set; }

			[JsonProperty("BaseCurrencyLong")]
			public string BaseCurrencyLong { get; internal set; }

			[JsonProperty("MinTradeSize")]
			public double MinTradeSize { get; internal set; }

			public CurrencyPair Pair { get; private set; }
			[JsonProperty("MarketName")] private string MarketName
			{
				set { Pair = CurrencyPair.Parse(value); }
			}

			[JsonProperty("IsActive")]
			public bool IsActive { get; internal set; }

			public DateTimeOffset CreationDate { get; private set; }
			[JsonProperty("Created")] private string CreatedInternal
			{
				set { CreationDate = DateTimeOffset.Parse(value); }
			}

			[JsonProperty("Notice")]
			public string Notice { get; internal set; }

			[JsonProperty("IsSponsored")]
			public string IsSponsored { get; internal set; }

			[JsonProperty("LogoUrl")]
			public string LogoUrl { get; internal set; }
		}
	}
}
