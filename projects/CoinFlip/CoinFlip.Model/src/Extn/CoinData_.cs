using System.Collections.Generic;
using CoinFlip.Settings;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip
{
	public static class CoinData_
	{
		/// <summary>Convert to Unit{double}</summary>
		public static Unit<double> _(this double value, CoinData cd)
		{
			return value._(cd.Symbol);
		}

		/// <summary>Return the average value of this coin across the given exchanges</summary>
		public static Unit<double> AverageValue(this CoinData cd, IEnumerable<Exchange> source_exchanges)
		{
			// Find the average price on the available exchanges
			var value = new Average<double>();
			foreach (var exch in source_exchanges)
			{
				// If the exchange doesn't have this coin, skip it
				var coin = exch.Coins[cd.Symbol];
				if (coin == null)
					continue;

				// If the exchange doesn't know the value of this coin yet, skip it
				var val = coin.ValueOf(1.0);
				if (val == 0)
					continue;

				// Add the value to the average
				value.Add(val);
			}

			// Return the average value
			return value.Count != 0
				? value.Mean._(SettingsData.Settings.ValuationCurrency)
				: cd.AssignedValue._(SettingsData.Settings.ValuationCurrency);
		}

		/// <summary>The sum of account balances across the given exchanges for this coin</summary>
		public static Unit<double> NettTotal(this CoinData cd, IEnumerable<Exchange> source_exchanges)
		{
			var total = 0.0;
			foreach (var exch in source_exchanges)
			{
				// If the exchange doesn't have this coin, skip it
				var coin = exch.Coins[cd.Symbol];
				if (coin == null)
					continue;

				// Add the balance on this exchange to the total
				total += coin.Balances.NettTotal;
			}

			// Return the total balance
			return total._(cd.Symbol);
		}

		/// <summary>The sum of available balance across all exchanges</summary>
		public static Unit<double> NettAvailable(this CoinData cd, IEnumerable<Exchange> source_exchanges)
		{
			var avail = 0.0;
			foreach (var exch in source_exchanges)
			{
				// If the exchange doesn't have this coin, skip it
				var coin = exch.Coins[cd.Symbol];
				if (coin == null)
					continue;

				// Add the available balance to the total
				avail += coin.Balances.NettAvailable;
			}

			// Return the total available balance
			return avail._(cd.Symbol);
		}
	}
}
