using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using CoinFlip;
using pr.common;
using pr.extn;
using pr.gui;
using pr.maths;
using pr.util;

namespace Bot.ReturnToMean
{
	[Plugin(typeof(IBot))]
    public class ReturnToMean :IBot
    {
		// Notes:
		// - Start with an amount of Base currency, called 'BaseHolding'.
		// - If the price is below the mean, increase the amount of Base currency held.
		// - If the price is above the mean, decrease the amount of Base currency held.
		// - Limit the change in about to some +/- fraction of the initial amount.

		public ReturnToMean(Model model, XElement settings_xml)
			:base("ReturnToMean", model, new SettingsData(settings_xml))
		{
			UI = new ReturnToMeanUI(this);
		}
		protected override void Dispose(bool disposing)
		{
			UI = null;
			MA = null;
			base.Dispose(disposing);
		}
		protected override void OnSettingChanged(SettingChangedEventArgs args)
		{
			switch (args.Key) {
			default: base.OnSettingChanged(args); return;
			case nameof(SettingsData.Exchange): Exchange = null; return;
			case nameof(SettingsData.Pair):     Pair = null; return;
			case nameof(SettingsData.Currency): Coin = null; return;
			}
		}
		protected override void HandlePairsUpdated(object sender, EventArgs e)
		{
			base.HandlePairsUpdated(sender, e);
			Pair = null;
		}

		/// <summary>Settings for this strategy</summary>
		public new SettingsData Settings
		{
			get { return (SettingsData)base.Settings; }
		}

		/// <summary>The UI for monitoring this bot</summary>
		public ReturnToMeanUI UI
		{
			get { return m_ui; }
			private set
			{
				if (m_ui == value) return;
				Util.Dispose(ref m_ui);
				m_ui = value;
			}
		}
		private ReturnToMeanUI m_ui;

		/// <summary>The exchange to trade on</summary>
		public Exchange Exchange
		{
			get { return m_exch ?? (m_exch = Model.Exchanges[Settings.Exchange]); }
			private set
			{
				Debug.Assert(value == null);
				m_exch = value;
				Pair = null;
			}
		}
		private Exchange m_exch;

		/// <summary>The pair to trade</summary>
		public TradePair Pair
		{
			get
			{
				if (m_pair == null)
				{
					var exch = Model.Exchanges.FirstOrDefault(x => x.Name == Settings.Exchange);
					m_pair = exch?.Pairs.Values.FirstOrDefault(x => x.Name == Settings.Pair);
				}
				return m_pair;
			}
			private set
			{
				Debug.Assert(value == null);
				m_pair = value;
				Coin = null;
			}
		}
		private TradePair m_pair;

		/// <summary>The holding currency</summary>
		public Coin Coin
		{
			get
			{
				if (m_coin == null)
				{
					var pair = Pair;
					m_coin =
						pair?.Base  == Settings.Currency ? pair.Base :
						pair?.Quote == Settings.Currency ? pair.Quote :
						null;
				}
				return m_coin;
			}
			private set
			{
				Debug.Assert(value == null);
				m_coin = value;
			}
		}
		private Coin m_coin;

		/// <summary>Moving average data for 'Instrument'</summary>
		public IndicatorMA MA
		{
			get { return m_ma; }
			private set
			{
				if (m_ma == value) return;
				if (m_ma != null)
				{
					m_ma.Instrument = Util.Dispose(m_ma.Instrument);
					Util.Dispose(ref m_ma);
				}
				m_ma = value;
				if (m_ma != null)
				{
					m_ma.Instrument = new Instrument("ReturnToMean", Model.PriceData[Pair, Settings.TimeFrame]);
				}
			}
		}
		private IndicatorMA m_ma;

		/// <summary>The value of the current holding</summary>
		private Unit<decimal> CurrentHolding
		{
			// This does not have to match the current balance, it's a logical amount
			// that this bot uses as a holding amount.
			get;
			set;
		}

		/// <summary>The holding for when the spot price equals the mean price</summary>
		private Unit<decimal> AverageHolding
		{
			get { return Settings.AverageHolding._(Coin); }
		}

		/// <summary>The amount per std.dev. to change the holding by</summary>
		private Unit<decimal> HoldingChange
		{
			get { return Settings.HoldingChange._(Coin); }
		}

		/// <summary>Start the bot</summary>
		public override bool OnStart()
		{
			// If a pair has not been set, show the UI to that one can be chosen
			if (Pair == null || Coin == null)
			{
				Model.AddToUI(UI);
				UI.DockControl.IsActiveContent = true;
				return false;
			}

			// Create a moving average
			MA = new IndicatorMA(new IndicatorMA.SettingsData{});

			// Set the initial holding
			CurrentHolding = AverageHolding;

			return true;
		}

		/// <summary>Main loop step</summary>
		public override void Step()
		{
			// Stop the bot if there is no pair or currency
			if (Pair == null || Coin == null)
			{
				Active = false;
				return;
			}

			// Get the current spot prices
			var spot_b2q = Pair.SpotPrice(ETradeType.B2Q);
			var spot_q2b = Pair.SpotPrice(ETradeType.Q2B);
			if (spot_q2b == null || spot_b2q == null) return;
			var spot = (spot_b2q.Value + spot_q2b.Value) / 2m;

			// Get the average price and std.dev.
			if (MA.Count == 0) return;
			var price_mean = MA.PriceMean;
			var price_sd = MA.PriceStdDev;
			if (price_sd == 0) return;

			// Compare the current price to the MA.
			// Increase holding when price is below average
			// Decrease holding when price is above average
			var diff = (spot - price_mean) / price_sd;
			var ideal_holding = AverageHolding - diff * HoldingChange;
			Debug.WriteLine($"ideal holding: {ideal_holding.ToString("G8",true)}   current holding: {CurrentHolding.ToString("G8",true)}");

			// If there is an existing order..
			if (MonitoredTrades.Count != 0)
			{
				// Wait for the monitored order to be filled or cancelled
				Debug.Assert(MonitoredTrades.Count == 1);

				// Get the existing order and apply it to the current holding to see
				// if the result of the order is near enough to the ideal order.
				var res = MonitoredTrades.First();
				var pos = Model.Positions.FirstOrDefault(x => x.OrderId == res.OrderId);
				if (pos != null)
				{
					var current_holding = CurrentHolding;
					if (Coin == pos.CoinIn)
						current_holding -= pos.VolumeIn;
					if (Coin == pos.CoinOut)
						current_holding += pos.VolumeOutNett;

					var holding_diff = current_holding - ideal_holding;
					if (Maths.Abs(holding_diff) > HoldingChange)
					{
						// Cancel the order, we need a larger or smaller order
						Exchange.CancelOrder(Pair, res.OrderId);
					}
				}
			}
			else
			{
				// Only change the holding when the ideal holding differs by more than the holding change amount
				var holding_diff = CurrentHolding - ideal_holding;
				if (Maths.Abs(holding_diff) > HoldingChange)
				{
					// Increase or decrease holding
					Trade trade;
					if (holding_diff > 0)
					{
						// Current holding is too high, sell some 'Coin'
						var tt = Coin == Pair.Base ? ETradeType.B2Q : ETradeType.Q2B;

						// Sell in multiples of the holding change amount
						var vol_in = (decimal)(int)(decimal)(holding_diff / HoldingChange) * HoldingChange;

						// Create a trade to sell 'vol_in' at 'spot'
						trade = new Trade(Fund.Id, tt, Pair, spot, tt.VolumeBase(spot, volume_in:vol_in));
					}
					else
					{
						// Current holding is too low, buy some 'Coin'
						var tt = Coin == Pair.Quote ? ETradeType.B2Q : ETradeType.Q2B;

						// Buy in multiples of the holding change amount
						var vol_out = (decimal)(int)(decimal)(-holding_diff / HoldingChange) * HoldingChange;

						// Create a trade to buy 'vol_out' at 'spot'
						trade = new Trade(Fund.Id, tt, Pair, spot, tt.VolumeBase(spot, volume_out:vol_out));
					}

					// Place the trade
					var validate = trade.Validate();
					if (validate == EValidation.Valid)
					{
						MonitoredTrades.Add2(trade.CreateOrder());
						m_suppress_not_created = false;
					}
					else if (!m_suppress_not_created)
					{
						Log.Write(ELogLevel.Warn, $"Order skipped. {trade.Description} - {validate}");
						m_suppress_not_created = true;
					}
				}
			}
		}
		private bool m_suppress_not_created;

		/// <summary>When a monitored order is filled, adjust the CurrentHolding</summary>
		protected override void OnPositionFilled(ulong order_id, OrderFill his)
		{
			base.OnPositionFilled(order_id, his);
			if (Coin == his.CoinIn)
				CurrentHolding -= his.VolumeIn;
			if (Coin == his.CoinOut)
				CurrentHolding += his.VolumeNett;
		}

		/// <summary>Return items to add to the context menu for this bot</summary>
		public override void CMenuItems(ContextMenuStrip cmenu)
		{
			cmenu.Items.AddSeparator();
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Monitor"));
				opt.Click += (s,a) =>
				{
					Model.AddToUI(UI);
					UI.DockControl.IsActiveContent = true;
				};
			}
		}

		/// <summary>Data needed to save a fishing instance in the settings</summary>
		[TypeConverter(typeof(TyConv))]
		public class SettingsData :SettingsBase<SettingsData>
		{
			public SettingsData()
			{
				Exchange       = string.Empty;
				Pair           = string.Empty;
				Currency       = string.Empty;
				TimeFrame      = ETimeFrame.Min30;
				AverageHolding = 0m;
				HoldingChange  = 0m;
			}
			public SettingsData(XElement node)
				:base(node)
			{}

			/// <summary>The name of the exchange to trade on</summary>
			public string Exchange
			{
				get { return get<string>(nameof(Exchange)); }
				set { set(nameof(Exchange), value); }
			}

			/// <summary>The name of the pair to trade</summary>
			public string Pair
			{
				get { return get<string>(nameof(Pair)); }
				set { set(nameof(Pair), value); }
			}

			/// <summary>The currency to hold at a fixed level</summary>
			public string Currency
			{
				get { return get<string>(nameof(Currency)); }
				set { set(nameof(Currency), value); }
			}

			/// <summary>The time-frame to use for the moving average</summary>
			public ETimeFrame TimeFrame
			{
				get { return get<ETimeFrame>(nameof(TimeFrame)); }
				set { set(nameof(TimeFrame), value); }
			}

			/// <summary>The amount of currency to hold when price is at the mean value</summary>
			public decimal AverageHolding
			{
				get { return get<decimal>(nameof(AverageHolding)); }
				set { set(nameof(AverageHolding), value); }
			}

			/// <summary>The amount to increase or decrease the holding by (per std.dev.)</summary>
			public decimal HoldingChange
			{
				get { return get<decimal>(nameof(HoldingChange)); }
				set { set(nameof(HoldingChange), value); }
			}

			private class TyConv :GenericTypeConverter<SettingsData> {}
		}
	}
}
