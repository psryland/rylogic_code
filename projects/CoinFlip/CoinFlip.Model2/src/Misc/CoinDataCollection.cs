using System.Diagnostics;
using System.Linq;
using CoinFlip.Settings;
using Rylogic.Extn;

namespace CoinFlip
{
	public class CoinDataCollection
	{
		// Notes:
		//  - This is a helper wrapper around the 'Settings.Coins' array.
		//    It allows new coins to be added implicitly.

		public CoinDataCollection(SettingsData settings)
		{
			Settings = settings;
		}

		/// <summary>Application settings</summary>
		private SettingsData Settings { get; }

		/// <summary>Return the meta data for the coin with name 'sym'</summary>
		public CoinData this[string sym]
		{
			get
			{
				var idx = Settings.Coins.IndexOf(x => x.Symbol == sym);
				if (idx < 0)
				{
					Debug.Assert(idx == -1);
					var coins = Settings.Coins.Concat(new CoinData(sym, 1m)).ToArray();
					foreach (var coin in coins)
						coin.Order = ++idx;

					// Record the coins in the settings
					Settings.Coins = coins;
					
					// The COI have changed, we'll need to update pairs.
					// todo - watch for settings saved 'Coins' => Model.TriggerPairsUpdate();
				}
				return Settings.Coins[idx];
			}
		}
	}
}
