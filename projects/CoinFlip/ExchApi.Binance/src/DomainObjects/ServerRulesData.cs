﻿using System;
using System.Collections.Generic;
using System.Linq;
using ExchApi.Common.JsonConverter;
using Newtonsoft.Json;
using Rylogic.Extn;
using Rylogic.Maths;

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
			[JsonProperty("status"), JsonConverter(typeof(ToEnum<ESymbolStatus>))]
			public ESymbolStatus Status { get; private set; }

			/// <summary></summary>
			[JsonProperty("baseAsset")]
			public string BaseAsset { get; private set; }

			/// <summary></summary>
			[JsonProperty("quoteAsset")]
			public string QuoteAsset { get; private set; }

			/// <summary></summary>
			[JsonProperty("baseAssetPrecision")]
			public int BaseAssetPrecision { get; private set; }

			/// <summary></summary>
			[JsonProperty("quotePrecision")]
			public int PricePrecision { get; private set; }

			/// <summary></summary>
			[JsonProperty("orderTypes", ItemConverterType = typeof(ToEnum<EOrderType>))]
			public List<EOrderType> OrderTypes { get; private set; }

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
			[JsonProperty("filters", ItemConverterType = typeof(SymbolFilterConverter))]
			public List<Filter> Filters { get; private set; }
		}

		/// <summary>Filters</summary>
		public class Filter
		{
			/// <summary></summary>
			[JsonProperty("filterType"), JsonConverter(typeof(ToEnum<EFilterType>))]
			public EFilterType FilterType { get; private set; }
		}
		public class FilterPrice :Filter
		{
			/// <summary></summary>
			[JsonProperty("minPrice")]
			public decimal MinPrice { get; private set; }

			/// <summary></summary>
			[JsonProperty("maxPrice")]
			public decimal MaxPrice { get; private set; }

			/// <summary></summary>
			[JsonProperty("tickSize")]
			public decimal TickSize { get; private set; }

			/// <summary>Round a given price to pass this filter</summary>
			public decimal Round(decimal price)
			{
				// Clamp to the valid range
				var quantised_price = Math_.Clamp(price, MinPrice, MaxPrice);

				// Align (down) to the tick size
				var remainder = quantised_price % TickSize;
				quantised_price -= remainder;

				return quantised_price;
			}
		}
		public class FilterPercentPrice :Filter
		{
			/// <summary></summary>
			[JsonProperty("multiplierUp")]
			public decimal MultiplierUp { get; private set; }

			/// <summary></summary>
			[JsonProperty("multiplierDown")]
			public decimal MultiplierDown { get; private set; }

			/// <summary></summary>
			[JsonProperty("avgPriceMins")]
			public decimal AveragePriceWindowInMinutes { get; private set; }

			/// <summary>Round the given price to pass this filter</summary>
			public decimal Round(decimal price, decimal weighted_average_price)
			{
				var lo = weighted_average_price * MultiplierDown;
				var hi = weighted_average_price * MultiplierUp;
				return Math_.Clamp(price, lo, hi);
			}
		}
		public class FilterLotSize :Filter
		{
			/// <summary></summary>
			[JsonProperty("minQty")]
			public decimal MinQuantity { get; private set; }

			/// <summary></summary>
			[JsonProperty("maxQty")]
			public decimal MaxQuantity { get; private set; }

			/// <summary></summary>
			[JsonProperty("stepSize")]
			public decimal StepSize { get; private set; }

			/// <summary>Round a given amount to pass this filter</summary>
			public decimal Round(decimal amount)
			{
				// Clamp to the valid range
				var quantised_amount = Math_.Clamp(amount, MinQuantity, MaxQuantity);

				// Align (down) to the step size
				var remainder = quantised_amount % StepSize;
				quantised_amount -= remainder;

				return quantised_amount;
			}
		}
		public class FilterMinNotional :Filter
		{
			/// <summary></summary>
			[JsonProperty("minNotional")]
			public decimal MinNotional { get; private set; }

			/// <summary></summary>
			[JsonProperty("applyToMarket")]
			public bool ApplyToMarketOrders { get; private set; }
		}
		public class FilterLimit :Filter
		{
			/// <summary></summary>
			[JsonProperty("limit")]
			public int Limit { get; private set; }

			[JsonProperty("maxNumAlgoOrders")]
			private int MaxNumAlgoOrders { set => Limit = value; }
			[JsonProperty("maxNumIcebergOrders")]
			private int MaxNumIcebergOrders { set => Limit = value; }
		}
	}
}
