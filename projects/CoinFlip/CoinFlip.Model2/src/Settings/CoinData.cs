using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Utility;

namespace CoinFlip.Settings
{
	[Serializable]
	[TypeConverter(typeof(TyConv))]
	[DebuggerDisplay("{Symbol} {Value} {OfInterest}")]
	public class CoinData : SettingsXml<CoinData>
	{
		public CoinData()
			: this(string.Empty, 1m)
		{ }
		public CoinData(string symbol, decimal value)
		{
			Symbol = symbol;
			AssignedValue = value;
			Order = 0;
			OfInterest = false;
			AutoTradingLimit = 1m;
			LivePriceSymbols = "USDT";
			ShowLivePrices = true;
			DefaultTradeAmount = 1m;
			BackTestingInitialBalance = 1m;
		}
		public CoinData(XElement node)
			: base(node)
		{ }

		/// <summary>The symbol name for the coin</summary>
		public string Symbol
		{
			get { return get<string>(nameof(Symbol)); }
			private set { set(nameof(Symbol), value); }
		}

		/// <summary>Value assigned to this coin</summary>
		public decimal AssignedValue
		{
			get { return get<decimal>(nameof(AssignedValue)); }
			set { set(nameof(AssignedValue), value); }
		}

		/// <summary>The order to display the coins in</summary>
		public int Order
		{
			get { return get<int>(nameof(Order)); }
			set { set(nameof(Order), value); }
		}

		/// <summary>True if coins of this type should be included in loops</summary>
		public bool OfInterest
		{
			get { return get<bool>(nameof(OfInterest)); }
			set { set(nameof(OfInterest), value); }
		}

		/// <summary>The maximum amount of this coin to automatically trade in one go</summary>
		public decimal AutoTradingLimit
		{
			get { return get<decimal>(nameof(AutoTradingLimit)); }
			set { set(nameof(AutoTradingLimit), value); }
		}

		/// <summary>A comma separated list of currencies used to convert this coin to a live price value</summary>
		public string LivePriceSymbols
		{
			get { return get<string>(nameof(LivePriceSymbols)); }
			set { set(nameof(LivePriceSymbols), value); }
		}

		/// <summary>Display the nett worth as a live price</summary>
		public bool ShowLivePrices
		{
			get { return get<bool>(nameof(ShowLivePrices)); }
			set { set(nameof(ShowLivePrices), value); }
		}

		/// <summary>The amount to initialise trades with</summary>
		public decimal DefaultTradeAmount
		{
			get { return get<decimal>(nameof(DefaultTradeAmount)); }
			set { set(nameof(DefaultTradeAmount), value); }
		}

		/// <summary>The initial balance for this currency when back testing</summary>
		public decimal BackTestingInitialBalance
		{
			get { return get<decimal>(nameof(BackTestingInitialBalance)); }
			set { set(nameof(BackTestingInitialBalance), value); }
		}

		private class TyConv : GenericTypeConverter<CoinData> { }
	}
}
