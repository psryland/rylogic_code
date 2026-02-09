using System;
using System.Threading.Tasks;
using System.Windows;
using CoinFlip;
using CoinFlip.Bots;
using Rylogic.Common;
using Rylogic.Plugin;
using Rylogic.Utility;

namespace Bot.HeikinAshiChaser
{
	[Plugin(typeof(IBot))]
	public class Bot :IBot
	{
		// Notes:
		//  - This bot is based on the fact that HA candles seem to produce nice obvious trends.
		//  - The idea is to have a trailing stop order that follows the open of the previous HA candle.
		//  - When the trend changes and fills the stop order, add a new stop order on the other side.

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
			Instrument = null;
			Util.Dispose(ref m_monitor);
			base.Dispose();
		}

		/// <summary>Bot settings</summary>
		public SettingsData Settings
		{
			get;
			set
			{
				if (field == value) return;
				if (field != null)
				{
					field.SettingChange -= HandleSettingChange;
				}
				field = value;
				if (field != null)
				{
					SetInstrument();
					field.SettingChange += HandleSettingChange;
				}

				// Handle setting change
				void HandleSettingChange(object sender, SettingChangeEventArgs e)
				{
					if (e.Before) return;
					switch (e.Key)
					{
					case nameof(SettingsData.Exchange):
					case nameof(SettingsData.Pair):
					case nameof(SettingsData.TimeFrame):
						{
							SetInstrument();
							break;
						}
					}
				}
			}
		}

		/// <summary></summary>
		public Exchange Exchange => !string.IsNullOrEmpty(Settings.Exchange) ? Model.Exchanges[Settings.Exchange] : null;

		/// <summary></summary>
		public TradePair Pair => Exchange != null && !string.IsNullOrEmpty(Settings.Pair) ? Exchange.Pairs[Settings.Pair] : null;

		/// <summary></summary>
		public ETimeFrame TimeFrame => Settings.TimeFrame;

		/// <summary></summary>
		public Instrument Instrument
		{
			get;
			set
			{
				if (field == value) return;
				if (field != null)
				{
					//	field.DataChanged -= HandleDataChanged;
				}
				field = value;
				if (field != null)
				{
					field.CandleStyle = ECandleStyle.HeikinAshi;
					//	field.DataChanged += HandleDataChanged;
				}

				// Handler
				//void HandleDataChanged(object sender, DataEventArgs e)
				//{
				//	// Update the 
				//}
			}
		}
		private void SetInstrument()
		{
			Instrument = Pair != null && TimeFrame != ETimeFrame.None
				? new Instrument(Name, Model.PriceData[Pair, TimeFrame])
				: null;
		}

		/// <summary>Configure</summary>
		protected override Task ConfigureInternal(object owner)
		{
			if (m_config_ui == null)
			{
				m_config_ui = new ConfigureUI((Window)owner, this);
				m_config_ui.Closed += delegate { m_config_ui = null; };
				m_config_ui.Show();
			}
			m_config_ui.Focus();
			return Task.CompletedTask;
		}
		private ConfigureUI m_config_ui;

		/// <summary>Step this bot after each candle close</summary>
		public override DateTimeOffset NextStepTime => m_next_step_time ?? base.NextStepTime;
		private DateTimeOffset? m_next_step_time;

		/// <summary>True if the bot is ok to run</summary>
		protected override bool CanActivateInternal => Settings.Validate(Model, Fund) == null;

		/// <summary>Step the bot</summary>
		protected override async Task StepInternal()
		{
			if (Instrument?.Count == 0)
				return;

			var latest = Instrument.Latest;
			m_next_step_time = latest.CloseTime(TimeFrame);

			// Use the fund balance to determine if we're long/short


			// No current pending order
			if (Settings.PendingOrders.Count == 0)
			{

				// Create a trade in the directory indicated by the candle
				if (latest.Bullish)
				{
					var trade = new Trade(Fund, Pair, EOrderType.Market, ETradeType.Q2B, 0, 0);
					var result = await trade.CreateOrder(Shutdown);
					Settings.PendingOrders.Add(result);
				}
				else
				{
					var trade = new Trade(Fund, Pair, EOrderType.Market, ETradeType.B2Q, 0, 0);
					var result = await trade.CreateOrder(Shutdown);
					Settings.PendingOrders.Add(result);
				}
			}
			else
			{
				// If the candle trend doesn't match the current trade

			}
		}
	}
}
