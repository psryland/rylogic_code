using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CoinFlip;
using CoinFlip.Bots;
using Rylogic.Container;
using Rylogic.Plugin;

namespace Bot.Arbitrage
{
	[Plugin(typeof(IBot))]
	public class Bot :IBot
	{
		// Notes:
		//  - This is an arbitrage bot designed to take advantage of noticable price differences
		//    in the BTC/USDT and BTC/USDC pairs

		public Bot(CoinFlip.Settings.BotData bot_data, Model model)
			:base(bot_data, model)
		{
			// Load the bot settings
			Settings = new SettingsData(SettingsFilepath);

			Model.PairsChanging += HandlePairsChanging;
		}
		public override void Dispose()
		{
			Model.PairsChanging -= HandlePairsChanging;
			base.Dispose();
		}

		/// <summary>Bot settings</summary>
		public SettingsData Settings { get; }

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

		public override TimeSpan LoopPeriod => TimeSpan.FromMinutes(1);

		/// <summary>Step the bot</summary>
		protected override Task StepInternal()
		{
			var exch = Model.Exchanges.FirstOrDefault(x => x.Name == "Binance");
			if (exch == null)
				return Task.CompletedTask;

			var btcusdt = exch.Pairs["BTC", "USDT"];
			var btcusdc = exch.Pairs["BTC", "USDC"];
			if (btcusdt == null || btcusdc == null)
				return Task.CompletedTask;

			// usdc -> btc -> usdt
			var p1 = btcusdc.SpotPrice[ETradeType.Q2B] / btcusdt.SpotPrice[ETradeType.B2Q];

			// usdt -> btc -> usdc
			var p2 = btcusdt.SpotPrice[ETradeType.Q2B] / btcusdc.SpotPrice[ETradeType.B2Q];

			if ((decimal)p1 > 1.05m)
				Model.Log.Write(Rylogic.Utility.ELogLevel.Debug, $"usdc -> usdt: {p1}");
			if ((decimal)p2 > 1.05m)
				Model.Log.Write(Rylogic.Utility.ELogLevel.Debug, $"usdt -> usdc: {p2}");

			return Task.CompletedTask;
		}

		/// <summary>Cause the set of loops to be regenerated</summary>
		public void SignalLoopSearch()
		{
			// If an update is already pending, ignore
			if (m_rebuild_loops_pending) return;
			m_rebuild_loops_pending = true;
			Misc.RunOnMainThread(FindLoops);
		}
		private bool m_rebuild_loops_pending;

		/// <summary>Find the available trade loops</summary>
		private void FindLoops()
		{
			//var finder = new LoopFinder(Model);

		}

		/// <summary>Handle a pair being added/removed to/from an exchange</summary>
		private void HandlePairsChanging(object sender, ListChgEventArgs<TradePair> e)
		{
			if (e.Before) return;
			SignalLoopSearch();
		}
	}
}
