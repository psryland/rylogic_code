using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CoinFlip.Settings;
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

		public GridCoins(Model model)
		{
			InitializeComponent();
			m_grid.MouseRightButtonUp += DataGrid_.ColumnVisibility;

			DockControl = new DockControl(this, "Coins");
			Filter = new CoinFilter(x => (CoinDataAdapter)x);
			ExchangeNames = new ListCollectionView(new List<string>());
			Coins = new ListCollectionView(ListAdapter.Create(model.Coins, c => new CoinDataAdapter(c, model, ExchangeNames), c => c?.CoinData)) { Filter = Filter.Predicate };
			Model = model;

			Filter.PropertyChanged += delegate { Coins.Refresh(); };
			m_grid.ContextMenu.Opened += delegate { m_grid.ContextMenu.Items.TidySeparators(); };

			// Commands
			AddCoin = Command.Create(this, AddCoinInternal);
			RemoveCoin = Command.Create(this, RemoveCoinInternal);
			SetBackTestingBalances = Command.Create(this, SetBackTestingBalancesInternal);

			DataContext = this;
		}
		public void Dispose()
		{
			Model = null;
			DockControl = null;
		}

		/// <summary></summary>
		public Model Model
		{
			get { return m_model; }
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					Model.AllowTradesChanged -= HandleAllowTradesChanged;
					Model.BackTestingChange -= HandleBackTestingChange;
					CoinData.BalanceChanged -= HandleBalanceChanged;
					CoinData.LivePriceChanged -= HandleBalanceChanged;
					m_model.Coins.CollectionChanged -= HandleCoinsChanged;
					m_model.Exchanges.CollectionChanged -= HandleExchangesChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.Exchanges.CollectionChanged += HandleExchangesChanged;
					m_model.Coins.CollectionChanged += HandleCoinsChanged;
					CoinData.LivePriceChanged += HandleBalanceChanged;
					CoinData.BalanceChanged += HandleBalanceChanged;
					Model.BackTestingChange += HandleBackTestingChange;
					Model.AllowTradesChanged += HandleAllowTradesChanged;
					HandleExchangesChanged(null, null);
				}

				// Handlers
				void HandleExchangesChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					// Preserve the current item
					var list = (List<string>)ExchangeNames.SourceCollection;
					using (Scope.Create(() => ExchangeNames.CurrentItem, ci => ExchangeNames.MoveCurrentToOrFirst(ci)))
					{
						list.Clear();
						list.Add(SpecialExchangeSources.All);
						list.Add(SpecialExchangeSources.Current);
						list.AddRange(Model.TradingExchanges.Select(x => x.Name));
						ExchangeNames.Refresh();
					}

					Coins.Refresh();
				}
				void HandleCoinsChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					using (Scope.Create(() => Coins.CurrentItem, ci => Coins.MoveCurrentToOrFirst(ci)))
						Coins.Refresh();
				}
				void HandleBalanceChanged(object sender, CoinEventArgs e)
				{
					var coin = Coins.Cast<CoinDataAdapter>().FirstOrDefault(x => x.CoinData.Symbol == e.Coin.Symbol);
					coin?.Invalidate();
				}
				void HandleBackTestingChange(object sender, PrePostEventArgs e)
				{
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BackTesting)));
				}
				void HandleAllowTradesChanged(object sender, EventArgs e)
				{
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AllowTrades)));
				}
			}
		}
		private Model m_model;

		/// <summary>Provides support for the DockContainer</summary>
		public DockControl DockControl
		{
			get { return m_dock_control; }
			private set
			{
				if (m_dock_control == value) return;
				Util.Dispose(ref m_dock_control);
				m_dock_control = value;
			}
		}
		private DockControl m_dock_control;

		/// <summary>The view of the available exchanges</summary>
		public ICollectionView ExchangeNames { get; }

		/// <summary>The data source for coin data</summary>
		public ICollectionView Coins { get; }

		/// <summary>True when back testing is enabled</summary>
		public bool BackTesting => Model.BackTesting;

		/// <summary>True when live trading is enabled</summary>
		public bool AllowTrades => Model.AllowTrades;

		/// <summary>The currently selected exchange</summary>
		private CoinDataAdapter Current
		{
			get => (CoinDataAdapter)Coins.CurrentItem;
			set => Coins.MoveCurrentTo(value);
		}

		/// <summary>Filter support for 'Coins'</summary>
		public CoinFilter Filter { get; }

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

		/// <summary>Set initial balances for back testing</summary>
		public Command SetBackTestingBalances { get; }
		private void SetBackTestingBalancesInternal()
		{
			var dlg = new BackTestingConfigUI(Window.GetWindow(this), Model);
			if (dlg.ShowDialog() == true)
			{
				Model.Simulation.Reset();
			}
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>A wrapper for CoinData to provide live value data</summary>
		private class CoinDataAdapter :INotifyPropertyChanged, IValueTotalAvail
		{
			// Notes:
			//  - This CoinData wrapper is used with multiple Exchanges showing averaged/summed data

			public CoinDataAdapter(CoinData cd, Model model, ICollectionView exchange_names)
			{
				CoinData = cd;
				Model = model;
				ExchangeNames = exchange_names;
			}

			/// <summary>App logic</summary>
			private Model Model { get; }

			/// <summary>All exchanges</summary>
			private ICollectionView ExchangeNames { get; }

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

			/// <summary>The value of all holdings of this coin on all source exchanges</summary>
			public double Balance => Value * Total;

			/// <summary>The average value of this coin across all exchanges</summary>
			public double Value => CoinData.AverageValue(SourceExchanges);

			/// <summary>The sum of account balances across all exchanges for this coin</summary>
			public Unit<double> Total => CoinData.NettTotal(SourceExchanges);

			/// <summary>The sum of available balance across all exchanges</summary>
			public Unit<double> Available => CoinData.NettAvailable(SourceExchanges);

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
