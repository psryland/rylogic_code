using System.Diagnostics;
using System.Runtime.Serialization;

namespace Cryptopia.API.Models
{
	[DataContract]
	[DebuggerDisplay("{SymbolBase,nq}/{SymbolQuote,nq}")]
	public class TradePairResult
	{
		/// <summary>A unique ID for the pair</summary>
		[DataMember]
		public int Id { get; set; }

		/// <summary>Currency label 'Base/Quote' (e.g. BTC/USDT)</summary>
		[DataMember]
		public string Label { get; set; }

		/// <summary>The descriptive name of the base currency (e.g. 'Bitcoin')</summary>
		[DataMember(Name = "Currency")]
		public string CurrencyBase { get; set; }

		/// <summary>The standard symbol of the base currency</summary>
		[DataMember(Name = "Symbol")]
		public string SymbolBase { get; set; }

		/// <summary>The descriptive name of the quote currency (e.g. 'Tether')</summary>
		[DataMember(Name = "BaseCurrency")]
		public string CurrencyQuote { get; set; }

		/// <summary>The standard symbol of the quote currency</summary>
		[DataMember(Name = "BaseSymbol")]
		public string SymbolQuote { get; set; }

		/// <summary>Status of the trading pair, whether it's ok to trade, or frozen</summary>
		[DataMember]
		public string Status { get; set; }

		/// <summary>A message associated with the current status</summary>
		[DataMember]
		public string StatusMessage { get; set; }

		/// <summary>The transaction cost as a percentage (e.g. 0.2%)</summary>
		[DataMember]
		public decimal TradeFee { get; set; }

		/// <summary>The minimum trade volume of base currency</summary>
		[DataMember(Name = "MinimumTrade")]
		public decimal MinimumTradeBase { get; set; }

		/// <summary>The maximum trade volume of base currency</summary>
		[DataMember(Name = "MaximumTrade")]
		public decimal MaximumTradeBase { get; set; }

		/// <summary>The minimum trade volume of quote currency</summary>
		[DataMember(Name = "MinimumBaseTrade")]
		public decimal MinimumTradeQuote { get; set; }

		/// <summary>The maximum trade volume of quote currency</summary>
		[DataMember(Name = "MaximumBaseTrade")]
		public decimal MaximumTradeQuote { get; set; }

		/// <summary>The minimum allowed price (in quote currency) for a trade</summary>
		[DataMember]
		public decimal MinimumPrice { get; set; }

		/// <summary>The maximum allowed price (in quote currency) for a trade</summary>
		[DataMember]
		public decimal MaximumPrice { get; set; }
	}
}
