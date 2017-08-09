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

		/// <summary>Coin name</summary>
		public string Symbol
		{
			get { return Meta.Symbol; }
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

		/// <summary>The value of this currency in units set by the user</summary>
		public decimal NormalisedValue
		{
			get { return Meta.Value; }
			set { Meta.Value = value; }
		}

		/// <summary>Return 'amount' converted to normalised value</summary>
		public decimal ApproximateValue(decimal amount)
		{
			return NormalisedValue * amount;
		}

		/// <summary>The maximum amount to automatically trade</summary>
		public Unit<decimal> AutoTradeLimit
		{
			get { return Meta.AutoTradingLimit._(Symbol); }
			set { Meta.AutoTradingLimit = value; }
		}

		/// <summary>Returns the value of '1' unit of this coin in the target currency</summary>
		public decimal LiveValue(Unit<decimal> amount)
		{
			// Live price is only available for coins of interest
			if (Meta.OfInterest)
			{
				var coin = this;
				var valid = true;
				var value = amount;
				var symbols = Meta.LivePriceSymbols.Split(new[]{','}, StringSplitOptions.RemoveEmptyEntries);
				foreach (var sym in symbols)
				{
					var pair = Exchange.Pairs[coin, sym];
					if (pair == null) { valid = false; break; }
					if (coin == pair.Base) value = value * pair.BaseToQuote(0m._(value)).Price;
					else                   value = value * pair.QuoteToBase(0m._(value)).Price;
					coin = pair.OtherCoin(coin);
				}
				if (valid)
					return value;
			}
			return ApproximateValue(amount);
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
