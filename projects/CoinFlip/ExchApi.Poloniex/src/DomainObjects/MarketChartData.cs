﻿using System;
using ExchApi.Common.JsonConverter;
using Newtonsoft.Json;
using Rylogic.Utility;

namespace Poloniex.API.DomainObjects
{
	public class MarketChartData
	{
		/// <summary>Time stamp</summary>
		[JsonProperty("date"), JsonConverter(typeof(ToUnixSec))]
		public UnixSec Time { get; private set; }

		[JsonProperty("open")]
		public decimal Open { get; private set; }

		[JsonProperty("close")]
		public decimal Close { get; private set; }

		[JsonProperty("high")]
		public decimal High { get; private set; }

		[JsonProperty("low")]
		public decimal Low { get; private set; }

		[JsonProperty("volume")]
		public decimal VolumeBase { get; private set; }

		[JsonProperty("quoteVolume")]
		public decimal VolumeQuote { get; private set; }

		[JsonProperty("weightedAverage")]
		public decimal WeightedAverage { get; private set; }

		/// <summary>An invalid candle returned by Poloniex</summary>
		public bool Invalid => Open == 0 && Close == 0 && High == 0 && Low == 0;
	}
}