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
			Settings.PendingOrders.OrderCompleted += HandleOrderCompleted;
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
		public override TimeSpan LoopPeriod => TimeSpan.FromMinutes(10);

		/// <summary>True if the bot is ok to run</summary>
		protected override bool CanActivateInternal => Settings.Validate(Model) == null;

		/// <summary>Configure</summary>
		protected override Task ConfigureInternal(object owner)
		{
			new ConfigureUI((Window)owner, this).Show();
			return Task.CompletedTask;
		}

		/// <summary>Step the bot</summary>
		protected override async Task StepInternal()
		{
			Debug.Assert(CanActivate);

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
				var price = pair.SpotPrice[tt] ?? 0m._(pair.RateUnits);
				if (price == 0)
					continue;

				// Determine where the current price is within the price range
				var price_ratio = PriceFrac(price);

				// The value (in quote) of all holdings
				var total_holdings = HoldingsQuote + HoldingsBase * price;

				// Calculate the ratio of quote holdings to total holdings
				var quote_holdings_ratio = (double)(decimal)(HoldingsQuote / total_holdings);

				// See if the difference is greater than the rebalance threshold
				if (tt == ETradeType.Q2B)
				{
					// This is a buy adjustment and we "buy low" so the amount
					// of quote needs to be too high, i.e. quote_holdings_ratio > price_frac
					if (quote_holdings_ratio - price_ratio > Settings.RebalanceThreshold)
					{
						// The amount to buy is the amount that will bring the quote holdings
						// ratio down to match the price ratio.
						var target_quote_holdings = (decimal)price_ratio * total_holdings;
						var quote_holdings_difference = HoldingsQuote - target_quote_holdings;
						Debug.Assert(quote_holdings_difference > 0._(Pair.Quote));

						// Place the rebalance order
						trade = new Trade(Fund.Id, tt, pair, price, quote_holdings_difference / price);
						break;
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
						var target_quote_holdings = (decimal)price_ratio * total_holdings;
						var quote_holdings_difference = target_quote_holdings - HoldingsQuote;
						Debug.Assert(quote_holdings_difference > 0._(Pair.Quote));

						// Place the rebalance order
						trade = new Trade(Fund.Id, tt, pair, price, quote_holdings_difference / price);
						break;
					}
				}
			}

			// No adjustment needed, done
			if (trade == null)
				return;

			// An adjustment trade is needed, place the order now
			Log.Write(ELogLevel.Debug, $"Rebalance trade created: {trade.Description}");
			var order = await trade.CreateOrder(Cancel.Token);
			Settings.PendingOrders.Add(order);
		}

		/// <summary>The exchange we're trading on</summary>
		private Exchange Exchange => Model.Exchanges[Settings.Exchange];

		/// <summary>The pair being traded</summary>
		private TradePair Pair => Exchange?.Pairs[Settings.Pair];

		/// <summary>The amount held in base currency</summary>
		private Unit<decimal> HoldingsBase
		{
			get => Settings.BaseCurrencyBalance._(Pair.Base);
			set => Settings.BaseCurrencyBalance = value;
		}

		/// <summary>The amount held in quote currency</summary>
		private Unit<decimal> HoldingsQuote
		{
			get => Settings.QuoteCurrencyBalance._(Pair.Quote);
			set => Settings.QuoteCurrencyBalance = value;
		}

		/// <summary>Get the fractional position of 'price' within the price range. [0,1] = [AllIn,AllOut]</summary>
		private double PriceFrac(Unit<decimal> price)
		{
			var numer = (double)((decimal)price - Settings.AllInPrice);
			var denom = (double)(Settings.AllOutPrice - Settings.AllInPrice);
			return Math_.Clamp(numer / denom, 0.0, 1.0);
		}

		/// <summary>Handle an order being filled</summary>
		private void HandleOrderCompleted(object sender, MonitoredOrderEventArgs e)
		{
			// When an order is completed, update the holdings
			var his = e.Exchange.History[e.OrderId];
			if (his == null)
				throw new NotSupportedException("Completed order not found in history");

			// Update the holdings
			switch (his.TradeType)
			{
			default: throw new Exception("Unknown trade type");
			case ETradeType.Q2B:
				{
					HoldingsQuote -= his.AmountNett;
					HoldingsBase += his.AmountIn;
					break;
				}
			case ETradeType.B2Q:
				{
					HoldingsQuote += his.AmountNett;
					HoldingsBase -= his.AmountIn;
					break;
				}
			}
		}
	}
}
