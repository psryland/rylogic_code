using System;
using System.Collections.Generic;
using System.Linq;
using ExchApi.Common.JsonConverter;
using Newtonsoft.Json;
using Rylogic.Extn;

namespace Binance.API.DomainObjects
{
	public class ServerRulesData
	{
		/// <summary></summary>
		[JsonProperty("timezone")]
		public string TimeZone { get; private set; }

		/// <summary></summary>
		[JsonProperty("serverTime"), JsonConverter(typeof(UnixMSToDateTimeOffset))]
		public DateTimeOffset ServerTime { get; private set; }

		/// <summary></summary>
		[JsonProperty("rateLimits")]
		public List<RateLimit> RateLimits { get; private set; }

		/// <summary></summary>
		[JsonProperty("exchangeFilters")]
		public List<ExchangeFilter> ExchangeFilters { get; private set; }

		/// <summary></summary>
		[JsonProperty("symbols")]
		public List<SymbolData> Symbols { get; private set; }

		/// <summary></summary>
		public class RateLimit
		{
			/// <summary></summary>
			public ERateLimitType RateLimitType { get; private set; }
			[JsonProperty("rateLimitType")] private string RateLimitTypeInternal
			{
				set { RateLimitType = Enum<ERateLimitType>.Parse(value); }
			}

			/// <summary></summary>
			public string Interval { get; private set; }
			[JsonProperty("interval")] private string IntervalInternal
			{
				set { Interval = value; }
			}

			/// <summary></summary>
			[JsonProperty("intervalNum")]
			public int IntervalNumber { get; private set; }

			/// <summary></summary>
			[JsonProperty("limit")]
			public int Limit { get; private set; }
		}

		/// <summary></summary>
		public class ExchangeFilter
		{
		}

		/// <summary></summary>
		public class SymbolData
		{
			/// <summary></summary>
			[JsonProperty("symbol")]
			public string Symbol { get; private set; }

			/// <summary></summary>
			public ESymbolStatus Status { get; private set; }
			[JsonProperty("status")] private string StatusInternal
			{
				set { Status = Enum<ESymbolStatus>.Parse(value); }
			}

			/// <summary></summary>
			[JsonProperty("baseAsset")]
			public string BaseAsset { get; private set; }

			/// <summary></summary>
			[JsonProperty("baseAssetPrecision")]
			public int BaseAssetPrecision { get; private set; }

			/// <summary></summary>
			[JsonProperty("quoteAsset")]
			public string QuoteAsset { get; private set; }

			/// <summary></summary>
			[JsonProperty("quotePrecision")]
			public int QuoteAssetPrecision { get; private set; }

			/// <summary></summary>
			public List<EOrderType> OrderTypes { get; private set; }
			[JsonProperty("orderTypes")] private List<string> OrderTypesInternal
			{
				set { OrderTypes = value.Select(x => Enum<EOrderType>.Parse(x)).ToList(); }
			}

			/// <summary></summary>
			[JsonProperty("icebergAllowed")]
			public bool IcebergAllowed { get; private set; }

			/// <summary></summary>
			[JsonProperty("isSpotTradingAllowed")]
			public bool IsSpotTradingAllowed { get; private set; }

			/// <summary></summary>
			[JsonProperty("isMarginTradingAllowed")]
			public bool IsMarginTradingAllowed { get; private set; }

			/// <summary></summary>
			[JsonProperty("filters")]
			public List<Filter> Filters { get; private set; }

			/// <summary></summary>
			public class Filter
			{
				/// <summary></summary>
				public EFilterType FilterType { get; private set; }
				[JsonProperty("filterType")] private string FilterTypeInternal
				{
					set { FilterType = Enum<EFilterType>.Parse(value); }
				}

				/// <summary></summary>
				[JsonProperty("minPrice")]
				public decimal? MinPrice { get; private set; }

				/// <summary></summary>
				[JsonProperty("maxPrice")]
				public decimal? MaxPrice { get; private set; }

				/// <summary></summary>
				[JsonProperty("tickSize")]
				public decimal? TickSize { get; private set; }

				/// <summary></summary>
				[JsonProperty("multiplierUp")]
				public double? MultiplierUp { get; private set; }

				/// <summary></summary>
				[JsonProperty("multiplierDown")]
				public double? MultiplierDown { get; private set; }

				/// <summary></summary>
				[JsonProperty("avgPriceMins")]
				public int? avgPriceMins { get; private set; }

				/// <summary></summary>
				[JsonProperty("minQty")]
				public decimal? MinQuantity { get; private set; }

				/// <summary></summary>
				[JsonProperty("maxQty")]
				public decimal? MaxQuantity { get; private set; }

				/// <summary></summary>
				[JsonProperty("stepSize")]
				public decimal? StepSize { get; private set; }

				/// <summary></summary>
				[JsonProperty("minNotional")]
				public decimal? MinNotional { get; private set; }

				/// <summary></summary>
				[JsonProperty("applyToMarket")]
				public bool? ApplyToMarket { get; private set; }

				/// <summary></summary>
				[JsonProperty("limit")]
				public int? Limit { get; private set; }

				/// <summary></summary>
				[JsonProperty("maxNumAlgoOrders")]
				public int? MaxNumAlgoOrders { get; private set; }
			}
		}
	}

}
