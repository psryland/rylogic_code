using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CoinFlip.Settings;
using Rylogic.Extn;

namespace CoinFlip
{
	public class FundContainer : ObservableCollection<Fund>
	{
		// Notes:
		//  - Fund partitions are saved in isolated sets in the settings data.
		//    For live trading the funds are called 'LiveFunds'
		//    For back testing the funds are called 'TestFunds' (in the back testing settings)

		public FundContainer()
		{
			// Get the appropriate settings data based on the back testing state
			AssignFunds(Model.BackTesting
				? SettingsData.Settings.LiveFunds
				: SettingsData.Settings.BackTesting.TestFunds);
		}

		/// <summary>Create funds from the provided fund data</summary>
		public void AssignFunds(IList<FundData> fund_data)
		{
			Clear();
			Add(Fund.Main);
			foreach (var fd in fund_data)
			{
				if (fd.Id == Fund.Main.Id) continue;
				Add(new Fund(fd.Id));
			}
		}

		/// <summary>Return the fund associated with 'fund_id' (or null)</summary>
		public Fund this[string fund_id]
		{
			get
			{
				var idx = this.IndexOf(x => x.Id == fund_id);
				return idx >= 0 ? this[idx] : Fund.Unknown;
			}
			set
			{
				var idx = this.IndexOf(x => x.Id == fund_id);
				if (idx >= 0)
					this[idx] = value;
				else
					Add(value);
			}
		}

		/// <summary>Save all balances to settings</summary>
		public void SaveToSettings(IEnumerable<Exchange> exchanges)
		{
			var fund_data = new List<FundData>();
			foreach (var fund in this)
			{
				// Return the non-zero balances associated with 'fund' on each exchange.
				// Don't bother with balances for the main fund because it is overwritten by updates.
				if (fund == Fund.Main)
					continue;

				var exch_data = new List<FundData.ExchData>();
				foreach (var exch in exchanges)
				{
					// Save the value of each non-zero balance
					var bal_data = new List<FundData.BalData>();
					foreach (var coin_balance in exch.Balance)
					{
						// 'coin_balance' contains the balance info for a coin.
						// Save the amount allocated to each fund (if non zero)
						var balance = coin_balance[fund];
						if (balance.Total == 0) continue;
						bal_data.Add(new FundData.BalData(coin_balance.Coin.Symbol, balance.Total));
					}
					if (bal_data.Count != 0)
						exch_data.Add(new FundData.ExchData(exch.Name, bal_data.ToArray()));
				}
				fund_data.Add(new FundData(fund.Id, exch_data.ToArray()));
			}

			// Save to the appropriate location
			if (Model.BackTesting)
				SettingsData.Settings.BackTesting.TestFunds = fund_data.ToArray();
			else
				SettingsData.Settings.LiveFunds = fund_data.ToArray();
		}
	}
}
