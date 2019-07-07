using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CoinFlip.Settings;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
#if false
	/// <summary>The balance of a currency on an exchange, attributed to a fund</summary>
	[DebuggerDisplay("{Description,nq}")]
	public class FundBalance2
	{
		// Notes:
		// - Negative balances are allowed because non-main funds can be assigned any balance.
		//   The main fund balance is the exchange balance minus the sum of other fund balances.
		// - Saving Fund balances to the settings uses a "push" system. Callers that change the
		//   fund balance need to trigger a save of the fund data. (use Model.SaveFundBalances())

		public FundBalance2(string fund_id, Coin coin)
			:this(fund_id, coin, 0m._(coin), 0m._(coin), DateTimeOffset.MinValue)
		{}
		public FundBalance2(string fund_id, Coin coin, Unit<decimal> total, Unit<decimal> held, DateTimeOffset last_updated)
		{
			FundId      = fund_id;
			Coin        = coin;
			Total       = total;
			HeldOnExch  = held;
			LastUpdated = last_updated;

			if (fund_id != Fund.Main)
			{
				// Initialise the balance from settings data.
				var fund_data = SettingsData.Settings.Funds.FirstOrDefault(x => x.Id == fund_id);
				var exch_data = fund_data?.Exchanges.FirstOrDefault(x => x.Name == coin.Exchange.Name);
				var bal_data  = exch_data?.Balances.FirstOrDefault(x => x.Symbol == coin.Symbol);
				if (bal_data != null)
					Update(LastUpdated, total: bal_data.Total._(coin));
			}
		}




		/// <summary>Update this fund balance. Only specified values are changed</summary>
		public void Update(DateTimeOffset last_updated, Unit<decimal>? total = null, Unit<decimal>? held_on_exch = null)
		{
			// Update the balance values
			if (total != null) Total = total.Value;
			if (held_on_exch != null) HeldOnExch = held_on_exch.Value;
			if (!Model.AllowTrades) Total += FakeCash;
			LastUpdated = last_updated;

			// Check the holds are still valid.
			CheckHolds();
		}
	}
#endif
}
