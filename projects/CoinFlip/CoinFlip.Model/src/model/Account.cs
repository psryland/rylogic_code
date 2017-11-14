using System;
using System.Collections.Generic;
using System.Linq;
using pr.util;

namespace CoinFlip
{
	public partial class Model
	{
		/// <summary>Return the sum of all balances, weighted by their values</summary>
		public decimal NettWorth
		{
			get
			{
				var worth = 0m;
				foreach (var exch in TradingExchanges)
				{
					foreach (var bal in exch.Balance.Values)
					{
						if (!Coins[bal.Coin].OfInterest) continue;
						worth += bal.Coin.ValueOf(bal.Total);
					}
				}
				return worth;
			}
		}

		/// <summary>Return the sum of all tokens</summary>
		public decimal TokenTotal
		{
			get
			{
				var sum = 0m;
				foreach (var exch in TradingExchanges)
				{
					foreach (var bal in exch.Balance.Values)
					{
						var amount = bal.Total;
						sum += amount;
					}
				}
				return sum;
			}
		}

		/// <summary>Return the sum across all exchanges of the total coin 'sym'</summary>
		public decimal SumOfTotal(string sym)
		{
			var sum = 0m;
			foreach (var exch in TradingExchanges)
			{
				var coin = exch.Coins[sym];
				sum += coin?.Balance.Total ?? 0;
			}
			return sum;
		}

		/// <summary>Return the sum across all exchanges of the available coin 'sym'</summary>
		public decimal SumOfAvailable(string sym)
		{
			var sum = 0m;
			foreach (var exch in TradingExchanges)
			{
				var coin = exch.Coins[sym];
				sum += coin?.Balance.Available ?? 0;
			}
			return sum;
		}

		/// <summary>True if we can get a live price for 'sym' from at least one exchange</summary>
		public bool LivePriceAvailable(string sym)
		{
			var available = false;
			foreach (var exch in TradingExchanges)
			{
				var coin = exch.Coins[sym];
				if (coin == null) continue;
				if (!coin.LivePriceAvailable) continue;
				available = true;
				break;
			}
			return available;
		}

		/// <summary>Find the maximum price for the given currency on the available exchanges</summary>
		public decimal MaxLiveValue(string sym)
		{
			var value = 0m._(sym);
			foreach (var exch in TradingExchanges)
			{
				var coin = exch.Coins[sym];
				if (coin == null) continue;
				value = Math.Max(value, coin.ValueOf(1m._(sym)));
			}
			return value;
		}

		/// <summary>Return all positions on the given pair across all exchanges</summary>
		public IEnumerable<Position> AllPositions(string pair_name)
		{
			return TradingExchanges.SelectMany(x => x.Positions.Values).Where(x => x.Pair.Name == pair_name);
		}

		/// <summary>Return all historic trades on the given pair across all exchanges</summary>
		public IEnumerable<PositionFill> AllHistory(string pair_name)
		{
			return TradingExchanges.SelectMany(x => x.History.Values).Where(x => x.Pair.Name == pair_name);
		}
	}
}
