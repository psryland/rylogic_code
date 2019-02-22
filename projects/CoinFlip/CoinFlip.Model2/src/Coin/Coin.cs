using System;
using System.Collections.Generic;
using System.Diagnostics;
using CoinFlip.Settings;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>A Coin, owned by an exchange</summary>
	[DebuggerDisplay("{Symbol} ({Exchange})")]
	public class Coin
	{
		public Coin(string sym, Exchange exch)
		{
			Symbol = sym;
			Exchange = exch;
			Meta = new CoinDataCollection(SettingsData.Settings)[sym];
			Pairs = new HashSet<TradePair>();
		}

		/// <summary>Coin name</summary>
		public string Symbol { [DebuggerStepThrough] get; }

		/// <summary>The Exchange trading this coin</summary>
		public Exchange Exchange { [DebuggerStepThrough] get; }

		/// <summary>Meta data for the coin</summary>
		private CoinData Meta { [DebuggerStepThrough] get; }

		/// <summary>Trade pairs involving this coin</summary>
		public HashSet<TradePair> Pairs { get; }

		/// <summary>Return the Coin with the exchange</summary>
		public string SymbolWithExchange => $"{Symbol} - {Exchange.Name}";

		/// <summary>The default amount to use when creating a trade from this Coin</summary>
		public Unit<decimal> DefaultTradeAmount => Meta.DefaultTradeAmount._(Symbol);

		/// <summary>The display order of the coin</summary>
		public int Order => Meta.Order;

		/// <summary>True if this coin type is of interest</summary>
		public bool OfInterest => Meta.OfInterest;

		/// <summary>Return the balance for this coin on its associated exchange</summary>
		public Balances Balances => Exchange.Balance[this];

		/// <summary>The value of 1 unit of this currency (Live if available, falling back to assigned)</summary>
		public decimal Value => ValueOf(1m);

		/// <summary>Return the value of 'amount' units of this currency</summary>
		public Unit<decimal> ValueOf(decimal amount)
		{
			// Live price is only available for coins of interest
			if (Meta.ShowLivePrices)
			{
				var coin = this;
				var valid = true;

				// Calculate the live price using the sequence of currencies in the meta data
				var value = (Unit<decimal>?)amount._(coin);
				var symbols = Meta.LivePriceSymbols.Split(new[]{','}, StringSplitOptions.RemoveEmptyEntries);
				foreach (var sym in symbols)
				{
					// Skip degenerate conversions
					if (sym == coin)
						continue;

					// Find the pair to convert 'coin' to 'sym'
					var pair = Exchange.Pairs[coin, sym];
					if (pair == null)
					{
						valid = false;
						break;
					}

					// Use spot prices
					value = (coin == pair.Base)
						? value * pair.SpotPrice(ETradeType.B2Q)
						: value * pair.SpotPrice(ETradeType.Q2B);

					coin = pair.OtherCoin(coin);
				}

				// Return the live price if it could be determined
				if (valid && value.HasValue)
					return value.Value._(Symbol);
			}

			// Otherwise, fall back to using the given value for the currency
			return (Meta.AssignedValue * amount)._(Symbol);
		}

		/// <summary>True if the live price can be found using the LivePriceSymbols</summary>
		public bool LivePriceAvailable
		{
			get
			{
				var coin = this;
				var available = false;
				var symbols = Meta.LivePriceSymbols.Split(new[]{','}, StringSplitOptions.RemoveEmptyEntries);
				foreach (var sym in symbols)
				{
					if (sym == coin)
					{
						available = true;
						continue;
					}

					// Find the pair to convert 'coin' to 'sym'
					var pair = Exchange.Pairs[coin, sym];
					available = pair != null && (
						(coin == pair.Base  && pair.SpotPrice(ETradeType.B2Q) != null) ||
						(coin == pair.Quote && pair.SpotPrice(ETradeType.Q2B) != null));
					if (!available)
						break;

					coin = pair.OtherCoin(coin);
				}
				return available;
			}
		}

		/// <summary>The maximum amount to automatically trade</summary>
		public Unit<decimal> AutoTradeLimit
		{
			get { return Meta.AutoTradingLimit._(Symbol); }
			set { Meta.AutoTradingLimit = value; }
		}

		/// <summary>The assigned amount this coin type is worth</summary>
		public decimal AssignedValue
		{
			get { return Meta.AssignedValue; }
			set { Meta.AssignedValue = value; }
		}

		/// <summary></summary>
		public override string ToString()
		{
			return Symbol;
		}

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
	}
}
