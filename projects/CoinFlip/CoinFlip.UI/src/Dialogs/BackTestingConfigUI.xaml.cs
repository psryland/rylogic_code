using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using CoinFlip.Settings;
using Rylogic.Container;
using Rylogic.Gui.WPF;

namespace CoinFlip.UI
{
	public partial class BackTestingConfigUI :Window
	{
		private class CoinToBalance :LazyDictionary<string, double> { };
		private class ExchToBalance :LazyDictionary<string, CoinToBalance> { };

		public BackTestingConfigUI(Window owner, Model model)
		{
			InitializeComponent();
			Owner = owner;
			Icon = owner?.Icon;

			InitialBalances = new ExchToBalance();
			Exchanges = new ListCollectionView(model.TradingExchanges.ToList());
			Coins = new ListCollectionView(ListAdapter.Create(model.Coins, x => new CoinDataAdapter(this, x), x => x.CoinData));

			Exchanges.CurrentChanged += delegate { Coins.Refresh(); };

			// Commands
			Accept = Command.Create(this, AcceptInternal);

			// Initialise the initial balance from settings
			foreach (var exch_data in SettingsData.Settings.BackTesting.AccountBalances.Exchanges)
				foreach (var bal_data in exch_data.Balances)
					InitialBalances[exch_data.ExchangeName][bal_data.Symbol] = bal_data.Total;

			DataContext = this;
		}
		protected override void OnClosed(EventArgs e)
		{
			var exch_data = new List<FundData.ExchData>();
			foreach (var exch in InitialBalances)
			{
				var bal_data = new List<FundData.BalData>();
				foreach (var bal in exch.Value)
				{
					if (bal.Value == 0) continue;
					bal_data.Add(new FundData.BalData(bal.Key, bal.Value));
				}
				if (bal_data.Count == 0) continue;
				exch_data.Add(new FundData.ExchData(exch.Key, bal_data.ToArray()));
			}
			var balance_data = new FundData(string.Empty, exch_data.ToArray());
			SettingsData.Settings.BackTesting.AccountBalances = balance_data;
			base.OnClosed(e);
		}

		/// <summary>Available Exchanges</summary>
		public ICollectionView Exchanges { get; }

		/// <summary>The available coins</summary>
		public ICollectionView Coins { get; }

		/// <summary>Collects initial balance data</summary>
		private ExchToBalance InitialBalances { get; }

		/// <summary>Accept settings</summary>
		public Command Accept { get; }
		private void AcceptInternal()
		{
			DialogResult = true;
			Close();
		}

		/// <summary></summary>
		private class CoinDataAdapter
		{
			private readonly BackTestingConfigUI m_me;
			public CoinDataAdapter(BackTestingConfigUI me, CoinData cd)
			{
				m_me = me;
				CoinData = cd;
			}

			/// <summary>Wrapped CoinData</summary>
			public CoinData CoinData { get; }

			/// <summary>The selected exchange</summary>
			private Exchange Exchange => (Exchange)m_me.Exchanges.CurrentItem;

			/// <summary>Get/Set the initial balance</summary>
			public double Balance
			{
				get
				{
					if (Exchange == null) return 0;
					return m_me.InitialBalances[Exchange.Name][CoinData.Symbol];
				}
				set
				{
					if (Exchange == null) return;
					m_me.InitialBalances[Exchange.Name][CoinData.Symbol] = value;
				}
			}
		}
	}
}
