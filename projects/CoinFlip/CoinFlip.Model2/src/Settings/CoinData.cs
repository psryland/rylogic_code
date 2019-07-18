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
	[DebuggerDisplay("{Symbol} {AssignedValue} {OfInterest}")]
	public class CoinData : SettingsXml<CoinData>
	{
		public CoinData()
			: this(string.Empty)
		{ }
		public CoinData(string symbol)
		{
			Symbol = symbol;
			SD = 8;
			AssignedValue = 0m;
			OfInterest = false;
			AutoTradingLimit = 1m;
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

		/// <summary>The number of significant digits used by this currency</summary>
		public int SD
		{
			get { return get<int>(nameof(SD)); }
			set { set(nameof(SD), value); }
		}

		/// <summary>Value assigned to this coin</summary>
		public decimal AssignedValue
		{
			get { return get<decimal>(nameof(AssignedValue)); }
			set { set(nameof(AssignedValue), value); }
		}

		/// <summary>The maximum amount of this coin to automatically trade in one go</summary>
		public decimal AutoTradingLimit
		{
			get { return get<decimal>(nameof(AutoTradingLimit)); }
			set { set(nameof(AutoTradingLimit), value); }
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

		/// <summary>True if coins of this type should be included in loops</summary>
		public bool OfInterest
		{
			get { return get<bool>(nameof(OfInterest)); }
			set { set(nameof(OfInterest), value); }
		}

		/// <summary>Raised when the live price of 'coin' changes</summary>
		public static event EventHandler<CoinEventArgs> LivePriceChanged;
		public static void NotifyLivePriceChanged(Coin coin)
		{
			LivePriceChanged?.Invoke(null, new CoinEventArgs(coin));
		}

		/// <summary>Raised when the balance of 'coin' has changed on an exchange</summary>
		public static event EventHandler<CoinEventArgs> BalanceChangeded;
		public static void NotifyBalanceChanged(Coin coin)
		{
			// Note: rather than one global event for "Any coin balance changed", prefer to attach
			// weak handlers to 'BalanceChanged' for the coins you care about. See GridCoins for an example.
			BalanceChangeded?.Invoke(null, new CoinEventArgs(coin));
		}

		private class TyConv : GenericTypeConverter<CoinData> { }
	}

	#region EventArgs
	public class CoinEventArgs :EventArgs
	{
		public CoinEventArgs(Coin coin)
		{
			Coin = coin;
		}

		/// <summary>The coin whose balance has changed</summary>
		public Coin Coin { get; }
	}
	#endregion
}
