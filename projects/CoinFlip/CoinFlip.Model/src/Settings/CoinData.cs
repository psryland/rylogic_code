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
	[DebuggerDisplay("{Symbol} {AssignedValue}")]
	public class CoinData : SettingsSet<CoinData>
	{
		public CoinData()
			: this(string.Empty)
		{ }
		public CoinData(string symbol)
		{
			Symbol = symbol;
			DisplayOrder = 0;
			SD = 8;
			AssignedValue = 0;
			CreateCrossExchangePairs = false;
			AutoTradingLimit = 1;
			DefaultTradeAmount = 1;
			BackTestingInitialBalance = 1;
		}
		public CoinData(XElement node)
			: base(node)
		{ }

		/// <summary>The symbol name for the coin</summary>
		public string Symbol
		{
			get => get<string>(nameof(Symbol));
			private set => set(nameof(Symbol), value);
		}

		/// <summary>The order to display this coin</summary>
		public int DisplayOrder
		{
			get => get<int>(nameof(DisplayOrder));
			set => set(nameof(DisplayOrder), value);
		}

		/// <summary>The number of significant digits used by this currency</summary>
		public int SD
		{
			get => get<int>(nameof(SD));
			set => set(nameof(SD), value);
		}

		/// <summary>Value assigned to this coin</summary>
		public decimal AssignedValue
		{
			get => get<decimal>(nameof(AssignedValue));
			set => set(nameof(AssignedValue), value);
		}

		/// <summary>The maximum amount of this coin to automatically trade in one go</summary>
		public decimal AutoTradingLimit
		{
			get => get<decimal>(nameof(AutoTradingLimit));
			set => set(nameof(AutoTradingLimit), value);
		}

		/// <summary>The amount to initialise trades with</summary>
		public decimal DefaultTradeAmount
		{
			get => get<decimal>(nameof(DefaultTradeAmount));
			set => set(nameof(DefaultTradeAmount), value);
		}

		/// <summary>The initial balance for this currency when back testing</summary>
		public decimal BackTestingInitialBalance
		{
			get => get<decimal>(nameof(BackTestingInitialBalance));
			set => set(nameof(BackTestingInitialBalance), value);
		}

		/// <summary>True if cross-exchange pairs should be created for this coin</summary>
		public bool CreateCrossExchangePairs
		{
			get => get<bool>(nameof(CreateCrossExchangePairs));
			set => set(nameof(CreateCrossExchangePairs), value);
		}

		/// <summary>Raised when the live price of 'coin' changes</summary>
		public static event EventHandler<CoinEventArgs> LivePriceChanged;
		public static void NotifyLivePriceChanged(Coin coin)
		{
			LivePriceChanged?.Invoke(null, new CoinEventArgs(coin));
		}

		/// <summary>Raised when the balance of 'coin' has changed on an exchange</summary>
		public static event EventHandler<CoinEventArgs> BalanceChanged;
		public static void NotifyBalanceChanged(Coin coin)
		{
			BalanceChanged?.Invoke(null, new CoinEventArgs(coin));
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

		public enum EBalance
		{
			Total,
			Available,
			Held,
		}
	}
	#endregion
}
