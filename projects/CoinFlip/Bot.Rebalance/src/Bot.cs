using System;
using System.Threading.Tasks;
using System.Windows;
using CoinFlip;
using CoinFlip.Bots;
using Rylogic.Plugin;

namespace Bot.Rebalance
{
	[Plugin(typeof(IBot))]
	public class Bot : IBot
	{
		// Notes:
		//  - For this bot, the user defines a range that spans prices on an instrument.
		//    The highest price is the "all-out" price, the lowest is the "all-in" price.
		//  - The fund has initial amounts of the base and quote currency.
		// e.g.
		//   Bot on BTC\USD
		//    Initial balance = $100USD + 0.3 BTC
		//    All-In price = 100 USD/BTC
		//    All-Out price = 1000USD/BTC
		//
		// Current Price: 400 USD/BTC
		// Account Value: = $100USD + 0.3BTC * 400 = $220USD
		//   Price Ratio: = (400 - 100) / (1000 - 100) = 0.333
		//     Rebalance: $220 * 0.333 = $73.33USD worth of USD
		//                $220 * 0.666 = $146.66USD worth of BTC
		//   Trade: Buy $26.66 (= $100 - $73.333) worth of BTC at $400 = 0.0666 BTC
		//  New Acct Value: = $73.333USD + 0.3666BTC = $220USD
		//
		//  Wait for price to move
		//  When Price Ratio != Acct Ratio within X%, Rebalance again.
		//  All-In and All-Out do not have to mean 0% and 100%

		public Bot(Guid id, Model model)
			: base(id, model)
		{
			Name = "Rebalance";

			// Load the bot settings
			Settings = new SettingsData(SettingsFilepath);
		}
		public override void Dispose()
		{
			base.Dispose();
		}

		/// <summary>Bot settings</summary>
		public SettingsData Settings { get; }

		/// <summary>True if the bot is ok to run</summary>
		public override bool CanActivate => Settings.Validate(Model) == null;

		/// <summary>Configure</summary>
		public override Task Configure(object owner)
		{
			new ConfigureUI((Window)owner, Model, Settings).Show();
			return Task.CompletedTask;
		}

		// Step the bot
		protected override async Task Step()
		{
			// Get the pair we're monitoring
			var exch = Model.Exchanges[Settings.Exchange];
			var pair = exch?.Pairs[Settings.Pair];
			if (pair == null)
				return;

			// Get the current spot price
			var spot_price = pair.SpotPrice(ETradeType.Q2B);
			if (spot_price == null)
				return;


			await Task.CompletedTask;
		}
	}
}
