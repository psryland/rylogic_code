using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CoinFlip.Settings;
using Rylogic.Extn;

namespace CoinFlip
{
	public class FundContainer : ObservableCollection<Fund>
	{
		public FundContainer()
		{
			// Ensure the 'Main' fund exists and initialise the funds from the settings
			Add(new Fund(Fund.Main));
			foreach (var fund_data in SettingsData.Settings.Funds)
			{
				if (fund_data.Id == Fund.Main) continue;
				Add(new Fund(fund_data.Id));
			}
		}

		/// <summary>Return the fund associated with 'fund_id' (or null)</summary>
		public Fund this[string fund_id]
		{
			get
			{
				var idx = this.IndexOf(x => x.Id == fund_id);
				return idx >= 0 ? this[idx] : null;
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
				if (fund.Id == Fund.Main)
					continue;

				var exch_data = new List<FundData.ExchData>();
				foreach (var exch in exchanges)
				{
					// Save the value of each non-zero balance
					var bal_data = new List<FundData.BalData>();
					foreach (var coin_balance in exch.Balance.Values)
					{
						// 'coin_balance' contains the balance info for a coin.
						// Save the amount allocated to each fund (if non zero)
						var balance = coin_balance[fund.Id];
						if (balance.Total == 0) continue;
						bal_data.Add(new FundData.BalData(coin_balance.Coin.Symbol, balance.Total, balance.HeldOnExch));
					}
					if (bal_data.Count != 0)
						exch_data.Add(new FundData.ExchData(exch.Name, bal_data.ToArray()));
				}
				fund_data.Add(new FundData(fund.Id, exch_data.ToArray()));
			}
			SettingsData.Settings.Funds = fund_data.ToArray();
		}
	}
}
