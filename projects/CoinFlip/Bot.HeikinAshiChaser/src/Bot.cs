﻿using System;
using System.Threading.Tasks;
using System.Windows;
using CoinFlip;
using CoinFlip.Bots;
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
		public SettingsData Settings { get; }

		/// <summary></summary>
		public Instrument Instrument
		{
			get => m_instrument;
			set
			{
				if (m_instrument == value) return;
				if (m_instrument != null)
				{
					//	m_instrument.DataChanged -= HandleDataChanged;
				}
				m_instrument = value;
				if (m_instrument != null)
				{
					m_instrument.CandleStyle = ECandleStyle.HeikinAshi;
					//	m_instrument.DataChanged += HandleDataChanged;
				}

				// Handler
				//void HandleDataChanged(object sender, DataEventArgs e)
				//{
				//	// Update the 
				//}
			}
		}
		private Instrument m_instrument;

		/// <summary>Step this bot after each candle close</summary>
		public override DateTimeOffset NextStepTime
		{
			get
			{
				var latest = Instrument.Latest;
				return latest != null ? latest.CloseTime(Instrument.TimeFrame) : base.NextStepTime;
			}
		}

		/// <summary>True if the bot is ok to run</summary>
		protected override bool CanActivateInternal => Settings.Validate(Model, Fund) == null;

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

		/// <summary>Step the bot</summary>
		protected override Task StepInternal()
		{
			// Buy on a green candle with the SAR below the price
			// Sell on a colour flip

			if (Instrument == null)
			{
				var exchange = Model.Exchanges[Settings.Exchange];
				var pair = exchange.Pairs[Settings.Pair];
				var price_data = Model.PriceData[pair, Settings.TimeFrame];
				Instrument = new Instrument(Name, price_data);
			}
			if (Instrument.Count == 0)
			{
				return Task.CompletedTask;
			}

			// 


			return Task.CompletedTask;
		}
	}
}