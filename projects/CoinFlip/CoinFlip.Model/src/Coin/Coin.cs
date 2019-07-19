using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CoinFlip.Settings;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>A Coin, owned by an exchange</summary>
	[DebuggerDisplay("{Description}")]
	public class Coin :IComparable
	{
		public Coin(string sym, Exchange exch)
		{
			Symbol = sym;
			Exchange = exch;
			Meta = SettingsData.Settings.Coins.FirstOrDefault(x => x.Symbol == sym) ?? new CoinData(sym);
			Pairs = new HashSet<TradePair>();
			ValuationPath = new List<TradePair>();

			// Don't call this from here, because coins are created before the trade pairs.
			// An explicit call to 'UpdateValuationPaths' should happen after updating the pairs for the exchange.
			// UpdateValuationPaths();
		}

		/// <summary>Coin name</summary>
		public string Symbol { get; }

		/// <summary>The Exchange trading this coin</summary>
		public Exchange Exchange { get; }

		/// <summary>Meta data for the coin</summary>
		public CoinData Meta { get; }

		/// <summary>Trade pairs involving this coin</summary>
		public HashSet<TradePair> Pairs { get; }

		/// <summary>Significant figures for this coin</summary>
		public int SD => Meta.SD;

		/// <summary>Return the Coin with the exchange</summary>
		public string SymbolWithExchange => $"{Symbol} - {Exchange.Name}";

		/// <summary>The default amount to use when creating a trade from this Coin</summary>
		public Unit<double> DefaultTradeAmount => Meta.DefaultTradeAmount._(Symbol);

		/// <summary>True if this coin type is of interest</summary>
		public bool OfInterest => Meta.OfInterest;

		/// <summary>Return the balance for this coin on its associated exchange</summary>
		public Balances Balances => Exchange.Balance[this];

		/// <summary>True if the live price can be found using the LivePriceSymbols</summary>
		public bool LivePriceAvailable => ValuationPath.Count != 0 || Symbol == SettingsData.Settings.ValuationCurrency;

		/// <summary>The value of 1 unit of this currency</summary>
		public Unit<double> Value
		{
			get
			{
				var coin = this;
				var value = (Unit<double>?)1.0._(coin);

				if (!LivePriceAvailable && !UpdateValuationPaths())
					return 0.0._(SettingsData.Settings.ValuationCurrency);

				foreach (var pair in ValuationPath)
				{
					// Use spot prices
					value =
						coin == pair.Base ? value * pair.SpotPrice[ETradeType.B2Q] :
						coin == pair.Quote ? value / pair.SpotPrice[ETradeType.Q2B] :
						throw new Exception($"Invalid valuation pair ({pair.Name}) when converting {coin} to {SettingsData.Settings.ValuationCurrency}");

					if (value == null) break;
					coin = pair.OtherCoin(coin);
				}

				Debug.Assert(value == null || value >= 0.0._(SettingsData.Settings.ValuationCurrency));
				ValueApprox = value ?? 0.0._(SettingsData.Settings.ValuationCurrency);
				return ValueApprox;
			}
		}

		/// <summary>The last known live value (doesn't recheck the live value)</summary>
		public Unit<double> ValueApprox { get; private set; }

		/// <summary>Return the value of 'amount' units of this currency</summary>
		public Unit<double> ValueOf(double amount)
		{
			return amount * Value;
		}

		/// <summary>The pairs to use to find the value in valuation currency</summary>
		private List<TradePair> ValuationPath { get; }

		/// <summary>The maximum amount to automatically trade</summary>
		public Unit<double> AutoTradeLimit
		{
			get { return Meta.AutoTradingLimit._(Symbol); }
			set { Meta.AutoTradingLimit = value; }
		}

		/// <summary>The assigned amount this coin type is worth</summary>
		public double AssignedValue
		{
			get { return Meta.AssignedValue; }
			set { Meta.AssignedValue = value; }
		}

		/// <summary>Ensure this coin can be valuated in the valuation currency</summary>
		public bool UpdateValuationPaths()
		{
			// Check the current path still valid
			var still_valid =
				(Symbol == SettingsData.Settings.ValuationCurrency) ||
				(ValuationPath.Count != 0 && ValuationPath.All(x => Exchange.Pairs.ContainsKey(x.UniqueKey)));
			if (still_valid)
				return true;

			// Rebuild the valuation path
			ValuationPath.Clear();
			if (Exchange.Pairs.Count == 0)
				return false;

			// Look for direct conversions first
			var pair = Exchange.Pairs[Symbol, SettingsData.Settings.ValuationCurrency];
			if (pair != null)
			{
				ValuationPath.Add(pair);
				return true;
			}

			// Look for likely common intermediate currencies
			foreach (var intermediate in new[] { "BTC", "USDT", "USDC", "ETH" }.Where(x => x != SettingsData.Settings.ValuationCurrency))
			{
				var step0 = Exchange.Pairs[Symbol, intermediate];
				var step1 = Exchange.Pairs[intermediate, SettingsData.Settings.ValuationCurrency];
				if (step0 != null && step1 != null)
				{
					ValuationPath.Add(step0);
					ValuationPath.Add(step1);
					return true;
				}
			}

			// If that fails try an exhaustive search... or give up
			// throw new NotImplementedException();
			return false;
		}

		/// <summary></summary>
		public override string ToString()
		{
			return Symbol;
		}

		/// <summary></summary>
		private string Description => $"{Symbol} ({Exchange}) NettTotal={Balances.NettTotal}";

		/// <summary>Allow implicit conversion to string symbol name</summary>
		[DebuggerStepThrough] public static implicit operator string(Coin coin)
		{
			return coin?.Symbol;
		}

		#region Equals
		[DebuggerStepThrough] public static bool operator == (Coin lhs, Coin rhs)
		{
			if ((object)lhs == null && (object)rhs == null) return true;
			if ((object)lhs == null || (object)rhs == null) return false;
			return lhs.Equals(rhs);
		}
		[DebuggerStepThrough] public static bool operator != (Coin lhs, Coin rhs)
		{
			return !(lhs == rhs);
		}
		public bool Equals(Coin rhs)
		{
			return
				rhs != null &&
				Symbol == rhs.Symbol &&
				Exchange == rhs.Exchange;
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as Coin);
		}
		public override int GetHashCode()
		{
			return new { Symbol, Exchange }.GetHashCode();
		}
		#endregion

		#region IComparable
		public int CompareTo(Coin rhs)
		{
			return Symbol.CompareTo(rhs.Symbol);
		}
		int IComparable.CompareTo(object obj)
		{
			return CompareTo((Coin)obj);
		}
		#endregion
	}
}
