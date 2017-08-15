using System;
using System.Collections.Generic;
using System.Diagnostics;
using pr.extn;
using pr.util;

namespace CoinFlip
{
	/// <summary>A Coin, owned by an exchange</summary>
	[DebuggerDisplay("{Symbol} ({Exchange})")]
	public class Coin
	{
		public Coin(string sym, Exchange exch)
		{
			Pairs = new HashSet<TradePair>();
			Meta = exch.Model.Coins[sym];
			Exchange = exch;
		}

		/// <summary>Meta data for the coin</summary>
		public Settings.CoinData Meta { get; private set; }

		/// <summary>App logic</summary>
		public Model Model
		{
			get { return Exchange.Model; }
		}

		/// <summary>Coin name</summary>
		public string Symbol
		{
			[DebuggerStepThrough] get { return Meta.Symbol; }
		}

		/// <summary>Return the Coin with the exchange</summary>
		public string SymbolWithExchange
		{
			get { return "{0} - {1}".Fmt(Symbol, Exchange.Name); }
		}

		/// <summary>The Exchange trading this coin</summary>
		public Exchange Exchange { get; private set; }

		/// <summary>Trade pairs involving this coin</summary>
		public HashSet<TradePair> Pairs { get; private set; }

		/// <summary>Return the balance for this coin on its associated exchange</summary>
		public Balance Balance
		{
			get { return Exchange.Balance[this]; }
		}

		/// <summary>Return the value of 'amount' units of this currency</summary>
		public decimal Value(decimal amount)
		{
			// Live price is only available for coins of interest
			if (Meta.OfInterest && Model.Settings.ShowLivePrices)
			{
				// Calculate the live price using the sequence of currencies in the meta data
				var coin = this;
				var valid = true;
				var value = amount._(coin);
				var symbols = Meta.LivePriceSymbols.Split(new[]{','}, StringSplitOptions.RemoveEmptyEntries);
				foreach (var sym in symbols)
				{
					// Find the pair to convert 'coin' to 'sym'
					var pair = Exchange.Pairs[coin, sym];
					if (pair == null)
					{
						valid = false;
						break;
					}

					// Use spot prices
					value = (coin == pair.Base)
						? value * pair.BaseToQuote(0m._(pair.Base)).Price
						: value * pair.QuoteToBase(0m._(pair.Quote)).Price;

					coin = pair.OtherCoin(coin);
				}

				// Return the live price if it could be determined
				if (valid)
					return value;
			}

			// Otherwise, fall back to using the given value for the currency
			return Meta.Value * amount;
		}

		/// <summary>The maximum amount to automatically trade</summary>
		public Unit<decimal> AutoTradeLimit
		{
			get { return Meta.AutoTradingLimit._(Symbol); }
			set { Meta.AutoTradingLimit = value; }
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
