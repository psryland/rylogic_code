using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using CoinFlip;
using CoinFlip.Bots;
using Rylogic.Maths;
using Rylogic.Plugin;
using Rylogic.Utility;

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

		private IDisposable m_monitor;
		public Bot(CoinFlip.Settings.BotData bot_data, Model model)
			: base(bot_data, model)
		{
			// Load the bot settings
			Settings = new SettingsData(SettingsFilepath);
			m_monitor = Settings.PendingOrders.Register(model);
		}
		public override void Dispose()
		{
			Util.Dispose(ref m_monitor);
			base.Dispose();
		}

		/// <summary>Bot settings</summary>
		public SettingsData Settings { get; }

		/// <summary>Step rate of this bot</summary>
		public override TimeSpan LoopPeriod => TimeSpan.FromMinutes(60);

		/// <summary>True if the bot is ok to run</summary>
		protected override bool CanActivateInternal
		{
			get
			{
				var err = Settings.Validate(Model, Fund);
				if (err == null) return true;
				return false;
			}
		}

		/// <summary>Configure</summary>
		protected override Task ConfigureInternal(object owner)
		{
			new ConfigureUI((Window)owner, this).Show();
			return Task.CompletedTask;
		}

		/// <summary>Step the bot</summary>
		protected override async Task StepInternal()
		{
			// If there are orders on the exchange, wait from them to be filled
			if (Settings.PendingOrders.Count != 0)
				return;

			// Get the pair we're monitoring
			var pair = Pair;
			if (pair == null)
				return;

			// Check for an adjustment in either direction
			var trade = (Trade)null;
			foreach (var tt in new[] { ETradeType.Q2B, ETradeType.B2Q })
			{
				// Get the current spot price
				var price = pair.SpotPrice[tt] ?? 0.0._(pair.RateUnits);
				if (price == 0)
					continue;

				// Determine where the current price is within the price range
				var price_ratio = PriceFrac(price);

				// The value (in quote) of all holdings
				var total_holdings = HoldingsQuote + HoldingsBase * price;

				// Calculate the ratio of quote holdings to total holdings
				var quote_holdings_ratio = HoldingsQuote / total_holdings;

				// See if the difference is greater than the rebalance threshold
				if (tt == ETradeType.Q2B)
				{
					// This is a buy adjustment and we "buy low" so the amount
					// of quote needs to be too high, i.e. quote_holdings_ratio > price_frac
					if (quote_holdings_ratio - price_ratio > Settings.RebalanceThreshold)
					{
						// The amount to buy is the amount that will bring the quote holdings
						// ratio down to match the price ratio. We're trading quote for base
						// so clamp 'quote' the amount available
						var target_quote_holdings = price_ratio * total_holdings;
						var amount_in = HoldingsQuote - target_quote_holdings;
						var amount_out = amount_in / price;
						if (Fund[pair.Quote].Available >= amount_in &&
							Pair.AmountRangeQuote.Contains(amount_in) &&
							Pair.AmountRangeBase.Contains(amount_out))
						{
							// Place the rebalance order
							trade = new Trade(Fund, pair, EOrderType.Market, tt, amount_in, amount_out, creator: Name);
							Debug.Assert(trade.Validate() == EValidation.Valid);
							break;
						}
					}
				}
				else
				{
					// This is a sell adjustment and we "sell high" so the amount
					// of quote needs to be too low, i.e. quote_holdings_ratio < price_frac
					if (price_ratio - quote_holdings_ratio > Settings.RebalanceThreshold)
					{
						// The amount to sell is the amount that will bring the quote holdings
						// ratio up to match the price ratio.
						var target_quote_holdings = price_ratio * total_holdings;
						var amount_out = target_quote_holdings - HoldingsQuote;
						var amount_in = amount_out / price;
						if (Fund[pair.Base].Available >= amount_in &&
							Pair.AmountRangeBase.Contains(amount_in) &&
							Pair.AmountRangeQuote.Contains(amount_out))
						{
							// Place the rebalance order
							trade = new Trade(Fund, pair, EOrderType.Market, tt, amount_in, amount_out, creator: Name);
							Debug.Assert(trade.Validate() == EValidation.Valid);
							break;
						}
					}
				}
			}

			// No adjustment needed, done
			if (trade == null)
				return;

			// An adjustment trade is needed, place the order now
			Log.Write(ELogLevel.Debug, $"Rebalance trade created: {trade.Description}");
			var order = await trade.CreateOrder(Cancel.Token);
			if (order != null)
				Settings.PendingOrders.Add(order);
			else
				Log.Write(ELogLevel.Error, $"Trade not created: {trade.Description}");
		}

		/// <summary>The exchange we're trading on</summary>
		private Exchange Exchange => Model.Exchanges[Settings.Exchange];

		/// <summary>The pair being traded</summary>
		private TradePair Pair => Exchange?.Pairs[Settings.Pair];

		/// <summary>The amount held in base currency</summary>
		private Unit<double> HoldingsBase => Fund[Pair.Base].Total;

		/// <summary>The amount held in quote currency</summary>
		private Unit<double> HoldingsQuote => Fund[Pair.Quote].Total;

		/// <summary>Get the fractional position of 'price' within the price range. [0,1] = [AllIn,AllOut]</summary>
		private double PriceFrac(Unit<double> price)
		{
			var numer = (double)price - Settings.AllInPrice;
			var denom = Settings.AllOutPrice - Settings.AllInPrice;
			return Math_.Clamp(numer / denom, 0.0, 1.0);
		}
	}
}
