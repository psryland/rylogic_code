using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Cryptopia.API
{
	[DebuggerDisplay("{SymbolBase,nq}/{SymbolQuote,nq}")]
	public class TradePair
	{
		/// <summary>A unique ID for the pair</summary>
		[JsonProperty]
		public int Id { get; set; }

		/// <summary>Currency label 'Base/Quote' (e.g. BTC/USDT)</summary>
		[JsonProperty]
		public string Label { get; set; }

		/// <summary>The descriptive name of the base currency (e.g. 'Bitcoin')</summary>
		[JsonProperty("Currency")]
		public string CurrencyBase { get; set; }

		/// <summary>The standard symbol of the base currency</summary>
		[JsonProperty("Symbol")]
		public string SymbolBase { get; set; }

		/// <summary>The descriptive name of the quote currency (e.g. 'Tether')</summary>
		[JsonProperty("BaseCurrency")]
		public string CurrencyQuote { get; set; }

		/// <summary>The standard symbol of the quote currency</summary>
		[JsonProperty("BaseSymbol")]
		public string SymbolQuote { get; set; }

		/// <summary>Status of the trading pair, whether it's ok to trade, or frozen</summary>
		[JsonProperty]
		public string Status { get; set; }

		/// <summary>A message associated with the current status</summary>
		[JsonProperty]
		public string StatusMessage { get; set; }

		/// <summary>The transaction cost as a percentage (e.g. 0.2%)</summary>
		[JsonProperty]
		public decimal TradeFee { get; set; }

		/// <summary>The minimum trade volume of base currency</summary>
		[JsonProperty("MinimumTrade")]
		public decimal MinimumTradeBase { get; set; }

		/// <summary>The maximum trade volume of base currency</summary>
		[JsonProperty("MaximumTrade")]
		public decimal MaximumTradeBase { get; set; }

		/// <summary>The minimum trade volume of quote currency</summary>
		[JsonProperty("MinimumBaseTrade")]
		public decimal MinimumTradeQuote { get; set; }

		/// <summary>The maximum trade volume of quote currency</summary>
		[JsonProperty("MaximumBaseTrade")]
		public decimal MaximumTradeQuote { get; set; }

		/// <summary>The minimum allowed price (in quote currency) for a trade</summary>
		[JsonProperty]
		public decimal MinimumPrice { get; set; }

		/// <summary>The maximum allowed price (in quote currency) for a trade</summary>
		[JsonProperty]
		public decimal MaximumPrice { get; set; }
	}

	public class TradePairsResponse
	{
		/// <summary></summary>
		[JsonProperty("Success")] public bool Success { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Error")] public string Error { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Data")]  List<TradePair> Data { get; set; }
	}
}
