using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CoinFlip.Settings;
using Rylogic.Container;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	public partial class GridCoins : Grid, IDockable, IDisposable, INotifyPropertyChanged
	{
		// Notes:
		//  - This grid shows balance and coin information based on averages over
		//    all exchanges, the currently selected exchange, or a specific exchange.

		static GridCoins()
		{
			CurrentProperty = Gui_.DPRegister<GridCoins>(nameof(Current));
			FilterThresholdProperty = Gui_.DPRegister<GridCoins>(nameof(FilterThreshold));
			IsValueFilterProperty = Gui_.DPRegister<GridCoins>(nameof(IsValueFilter));
		}
		public GridCoins(Model model)
		{
			InitializeComponent();
			DockControl = new DockControl(this, "Coins");
			ExchangeSource = new ListCollectionView(new List<string> { SpecialExchangeSources.All, SpecialExchangeSources.Current });
			Coins = new ListCollectionView(ListAdapter.Create(model.Coins, c => new CoinDataAdaptor(c, model, ExchangeSource), c => c?.CoinData));
			Model = model;

			// Commands
			AddCoin = Command.Create(this, () =>
			{
				var dlg = new PromptUI(Window.GetWindow(this))
				{
					Title = "Add Coin",
					Prompt = "Enter the Symbol Code for the coin to add",
				};
				if (dlg.ShowDialog() == true)
					Model.Coins.Add(new CoinData(dlg.Value, 1m, of_interest:true));
			});
			RemoveCoin = Command.Create(this, () =>
			{
				if (Current == null) return;
				Model.Coins.Remove(Current.CoinData);
			});
			EditLiveValueConversion = Command.Create(this, () =>
			{
				if (Current == null) return;
				var dlg = new PromptUI(Window.GetWindow(this))
				{
					Title = "Live Price Conversion",
					Prompt = "Set the symbols used to convert to a live price value (comma separated)",
					Value = Current.CoinData.LivePriceSymbols,
				};
				if (dlg.ShowDialog() == true)
					Current.CoinData.LivePriceSymbols = dlg.Value;
			});

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
			set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.Exchanges.CollectionChanged -= HandleExchangesChanged;
					m_model.Coins.CollectionChanged -= HandleCoinsChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.Coins.CollectionChanged += HandleCoinsChanged;
					m_model.Exchanges.CollectionChanged += HandleExchangesChanged;
				}

				// Handlers
				void HandleCoinsChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					Coins.Refresh();
				}
				void HandleExchangesChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					// Preserve the current item
					var list = (List<string>)ExchangeSource.SourceCollection;
					using (Scope.Create(() => ExchangeSource.CurrentItem, ci => ExchangeSource.MoveCurrentTo(ci)))
					{
						list.Clear();
						list.Add(SpecialExchangeSources.All);
						list.Add(SpecialExchangeSources.Current);
						list.AddRange(Model.TradingExchanges.Select(x => x.Name));
						ExchangeSource.Refresh();
					}
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

		/// <summary>The data source for coin data</summary>
		public ICollectionView ExchangeSource { get; }

		/// <summary>The view of the available exchanges</summary>
		public ICollectionView Coins { get; }

		/// <summary>The currently selected exchange</summary>
		public CoinDataAdaptor Current
		{
			get { return (CoinDataAdaptor)GetValue(CurrentProperty); }
			set { SetValue(CurrentProperty, value); }
		}
		public string CurrentCoinName => Current?.Symbol ?? "Coin";
		public static readonly DependencyProperty CurrentProperty;

		/// <summary>True if coins should be filtered</summary>
		public bool FilterEnabled
		{
			get { return m_filter_enabled; }
			set
			{
				if (m_filter_enabled == value) return;
				m_filter_enabled = value;
				Coins.Filter = value ? CoinThresholdFilter : (Predicate<object>)null;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilterEnabled)));
			}
		}
		private bool m_filter_enabled;

		/// <summary>Filter balances with a value or amount less than this threshold</summary>
		public decimal FilterThreshold
		{
			get { return (decimal)GetValue(FilterThresholdProperty); }
			set { SetValue(FilterThresholdProperty, value); }
		}
		private void FilterThreshold_Changed() => Coins.Refresh();
		public static readonly DependencyProperty FilterThresholdProperty;

		/// <summary>True if the filter should exclude based on normalised value, rather than absolute amount</summary>
		public bool IsValueFilter
		{
			get { return (bool)GetValue(IsValueFilterProperty); }
			set { SetValue(IsValueFilterProperty, value); }
		}
		private void IsValueFilter_Changed() => Coins.Refresh();
		public static readonly DependencyProperty IsValueFilterProperty;

		/// <summary>Add a coin to the collection</summary>
		public Command AddCoin { get; }

		/// <summary>Remove a coin from the collection</summary>
		public Command RemoveCoin { get; }

		/// <summary>Edit the sequence used to obtain the live value of a coin</summary>
		public Command EditLiveValueConversion { get; }

		/// <summary>Filter for the 'Coins' collection view</summary>
		private bool CoinThresholdFilter(object obj)
		{
			var cda = (CoinDataAdaptor)obj;
			return (IsValueFilter ? cda.Balance : cda.Total) > FilterThreshold;
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;
	}
}
