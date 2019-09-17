using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using CoinFlip.Settings;
using CoinFlip.UI.Dialogs;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	public partial class GridCoins :Grid, IDockable, IDisposable, INotifyPropertyChanged
	{
		// Notes:
		//  - This grid shows balance and coin information based on averages over
		//    all exchanges, the currently selected exchange, or a specific exchange.
		//  - The underlying Model.Coins collection should be ordered by DisplayOrder
		//  - Binding is tricky:
		//      Option 1: Maintain a mirror of Model.Coins contains wrappers for each CoinData.
		//        Con: needs to mirror in both directions src <-> mirror <-> grid,
		//      Option 2: Bind directly to 'Model.CoinData' and use a Converter to supply the
		//       values that aren't available on 'CoinData'

		public GridCoins(Model model)
		{
			InitializeComponent();
			m_grid.MouseRightButtonUp += DataGrid_.ColumnVisibility;

			Model = model;
			DockControl = new DockControl(this, "Coins");
			ExchangeNames = new ListCollectionView(SourceExchangeNames.ToList());
			Filter = new CoinFilter(x => (CoinDataAdapter)x);
			Coins = new List<CoinDataAdapter>(model.Coins.Select(x => new CoinDataAdapter(x, this)));
			CoinsView = new ListCollectionView(Coins);

			// Commands
			AddCoin = Command.Create(this, AddCoinInternal);
			RemoveCoin = Command.Create(this, RemoveCoinInternal);
			ResetSort = Command.Create(this, ResetSortInternal);
			SetBackTestingBalances = Command.Create(this, SetBackTestingBalancesInternal);
			PromptValuationPath = Command.Create(this, PromptValuationPathInternal);

			m_grid.ContextMenu.Opened += delegate { m_grid.ContextMenu.Items.TidySeparators(); };
			DataContext = this;
		}
		public void Dispose()
		{
			Filter = null;
			Model = null;
			DockControl = null;
		}

		/// <summary></summary>
		public Model Model
		{
			get => m_model;
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					Model.Exchanges.CollectionChanged -= HandleExchangesChanged;
					Model.Coins.CollectionChanged -= HandleCoinsChanged;
					Model.AllowTradesChanged -= HandleAllowTradesChanged;
					Model.BackTestingChange -= HandleBackTestingChange;
					CoinData.BalanceChanged -= HandleBalanceChanged;
					CoinData.LivePriceChanged -= HandleBalanceChanged;
					Coins.Clear();
					CoinsView.Refresh();
				}
				m_model = value;
				if (m_model != null)
				{
					CoinData.LivePriceChanged += HandleBalanceChanged;
					CoinData.BalanceChanged += HandleBalanceChanged;
					Model.BackTestingChange += HandleBackTestingChange;
					Model.AllowTradesChanged += HandleAllowTradesChanged;
					Model.Coins.CollectionChanged += HandleCoinsChanged;
					Model.Exchanges.CollectionChanged += HandleExchangesChanged;
				}

				// Handlers
				void HandleExchangesChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					// Preserve the current item
					using (Scope.Create(() => ExchangeNames.CurrentItem, ci => ExchangeNames.MoveCurrentToOrFirst(ci)))
					{
						var list = (List<string>)ExchangeNames.SourceCollection;
						list.Assign(SourceExchangeNames);
						ExchangeNames.Refresh();
					}

					CoinsView?.Refresh();
				}
				void HandleCoinsChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					using (Scope.Create(() => CoinsView.CurrentItem, ci => CoinsView.MoveCurrentToOrFirst(ci)))
					{
						Coins.Assign(Model.Coins.Select(x => new CoinDataAdapter(x, this)));
						CoinsView.Refresh();
					}
				}
				void HandleBalanceChanged(object sender, CoinEventArgs e)
				{
					var coin = Coins.FirstOrDefault(x => x.CoinData == e.Coin.Meta);
					coin?.Invalidate();
				}
				void HandleBackTestingChange(object sender, PrePostEventArgs e)
				{
					NotifyPropertyChanged(nameof(BackTesting));
				}
				void HandleAllowTradesChanged(object sender, EventArgs e)
				{
					NotifyPropertyChanged(nameof(AllowTrades));
				}
			}
		}
		private Model m_model;

		/// <summary>Provides support for the DockContainer</summary>
		public DockControl DockControl
		{
			get => m_dock_control;
			private set
			{
				if (m_dock_control == value) return;
				Util.Dispose(ref m_dock_control);
				m_dock_control = value;
			}
		}
		private DockControl m_dock_control;

		/// <summary>A mirror of 'Model.Coins' for data binding</summary>
		private List<CoinDataAdapter> Coins { get; }

		/// <summary>The options for exchange sources</summary>
		private IEnumerable<string> SourceExchangeNames
		{
			get
			{
				yield return SpecialExchangeSources.All;
				yield return SpecialExchangeSources.Current;
				foreach (var exch in Model.TradingExchanges)
					yield return exch.Name;
			}
		}

		/// <summary>The view of the available exchanges</summary>
		public ICollectionView ExchangeNames { get; }

		/// <summary>The data source for coin data</summary>
		public ICollectionView CoinsView { get; }

		/// <summary>True when back testing is enabled</summary>
		public bool BackTesting => Model.BackTesting;

		/// <summary>True when live trading is enabled</summary>
		public bool AllowTrades => Model.AllowTrades;

		/// <summary>The currently selected exchange</summary>
		private CoinDataAdapter Current
		{
			get => (CoinDataAdapter)CoinsView.CurrentItem;
			set => CoinsView.MoveCurrentTo(value);
		}

		/// <summary>Filter support for 'Coins'</summary>
		public CoinFilter Filter
		{
			get => m_filter;
			set
			{
				if (m_filter == value) return;
				if (m_filter != null)
				{
					m_filter.PropertyChanged -= HandlePropertyChanged;
				}
				m_filter = value;
				if (m_filter != null)
				{
					m_filter.PropertyChanged += HandlePropertyChanged;
				}

				// Handler
				void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
				{
					CoinsView.Filter = Filter.Enabled ? Filter.Predicate : (Predicate<object>)null;
					CoinsView.Refresh();
				}
			}
		}
		private CoinFilter m_filter;

		/// <summary>Add a coin to the collection</summary>
		public Command AddCoin { get; }
		private void AddCoinInternal()
		{
			// Prompt for the coin to add
			var dlg = new PromptUI(Window.GetWindow(this))
			{
				Title = "Add Coin",
				Prompt = "Enter the Symbol Code for the coin to add",
			};
			if (dlg.ShowDialog() == true)
				Model.Coins.Add(new CoinData(dlg.Value));
		}

		/// <summary>Remove a coin from the collection</summary>
		public Command RemoveCoin { get; }
		private void RemoveCoinInternal()
		{
			if (Current == null) return;
			Model.Coins.Remove(Current.CoinData);
		}

		/// <summary>Remove sorting from the coin list</summary>
		public Command ResetSort { get; }
		private void ResetSortInternal()
		{
			CoinsView.SortDescriptions.Clear();
			CoinsView.GroupDescriptions.Clear();
		}

		/// <summary>Set initial balances for back testing</summary>
		public Command SetBackTestingBalances { get; }
		private void SetBackTestingBalancesInternal()
		{
			var dlg = new BackTestingBalancesUI(Window.GetWindow(this), Model);
			if (dlg.ShowDialog() == true)
			{
				Model.Simulation.Reset();
			}
		}

		/// <summary>Display a prompt showing the valuation path for this coin</summary>
		public Command PromptValuationPath { get; }
		private void PromptValuationPathInternal()
		{
			if (Current == null) return;
			var prompt = new PromptUI(Window.GetWindow(this))
			{
				Prompt = $"The currency pairs used to determine the value in {SettingsData.Settings.ValuationCurrency}",
			};
			if (prompt.ShowDialog() == true)
			{
			}
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}

		/// <summary>A wrapper for CoinData to provide live value data</summary>
		private class CoinDataAdapter :INotifyPropertyChanged, IValueTotalAvail
		{
			// Notes:
			//  - This CoinData wrapper is used with multiple Exchanges showing averaged/summed data

			private readonly GridCoins m_owner;
			public CoinDataAdapter(CoinData cd, GridCoins owner)
			{
				CoinData = cd;
				m_owner = owner;
			}

			/// <summary>App logic</summary>
			private Model Model => m_owner.Model;

			/// <summary>All exchanges</summary>
			private ICollectionView ExchangeNames => m_owner.ExchangeNames;

			/// <summary>Returns the exchanges to consider when averaging/summing values</summary>
			private IEnumerable<Exchange> SourceExchanges
			{
				get
				{
					var src = (string)ExchangeNames.CurrentItem;
					if (src == SpecialExchangeSources.All)
					{
						return Model.TradingExchanges;
					}
					else if (src == SpecialExchangeSources.Current)
					{
						var exch = (Exchange)CollectionViewSource.GetDefaultView(Model.Exchanges).CurrentItem;
						if (exch != null && !(exch is CrossExchange))
							return new[] { exch };
					}
					else
					{
						var exch = Model.TradingExchanges.FirstOrDefault(x => x.Name == src);
						if (exch != null)
							return new[] { exch };
					}
					return Enumerable.Empty<Exchange>();
				}
			}

			/// <summary>The wrapped coin data</summary>
			public CoinData CoinData { get; }

			/// <summary>Coin symbol code</summary>
			public string Symbol => CoinData.Symbol;

			/// <summary>The order to display the coins in</summary>
			public int DisplayOrder => CoinData.DisplayOrder;

			/// <summary>The value of all holdings of this coin on all source exchanges</summary>
			public decimal Balance => Value * Total;

			/// <summary>The average value of this coin across all exchanges</summary>
			public decimal Value => CoinData.AverageValue(SourceExchanges);

			/// <summary>The sum of account balances across all exchanges for this coin</summary>
			public decimal Total => CoinData.NettTotal(SourceExchanges);

			/// <summary>The sum of available balance across all exchanges</summary>
			public decimal Available => CoinData.NettAvailable(SourceExchanges);

			/// <summary>True if a live value could be calculated</summary>
			public bool LiveValueAvailable => SourceExchanges.Any(x => x.Coins[Symbol]?.LivePriceAvailable ?? false);

			/// <summary></summary>
			public void Invalidate()
			{
				NotifyPropertyChanged(nameof(Value));
				NotifyPropertyChanged(nameof(Balance));
				NotifyPropertyChanged(nameof(Total));
				NotifyPropertyChanged(nameof(Available));
				NotifyPropertyChanged(nameof(LiveValueAvailable));
			}

			/// <summary></summary>
			public event PropertyChangedEventHandler PropertyChanged;
			public void NotifyPropertyChanged(string prop_name)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
			}
		}
	}
}
