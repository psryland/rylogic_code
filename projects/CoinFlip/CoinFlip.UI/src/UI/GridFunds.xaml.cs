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
using CoinFlip.Settings;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	public partial class GridFunds : Grid, IDockable, IDisposable, INotifyPropertyChanged
	{
		static GridFunds()
		{
			FilterThresholdProperty = Gui_.DPRegister<GridFunds>(nameof(FilterThreshold));
			FilterTypeProperty = Gui_.DPRegister<GridFunds>(nameof(FilterType));
		}
		public GridFunds(Model model)
		{
			InitializeComponent();
			DockControl = new DockControl(this, "Funds");
			Exchanges = new ListCollectionView(new List<Exchange>());
			Coins = new ListCollectionView(model.Coins);
			Model = model;

			Exchanges.CurrentChanged += delegate { Coins.Refresh(); };

			// Commands
			CreateNewFund = Command.Create(this, CreateNewFundInternal);
			RemoveFund = Command.Create(this, RemoveFundInternal);
			ShowFundAllocationsUI = Command.Create(this, ShowFundAllocationsUIInternal);

			CreateFundColumns();
			DataContext = this;
		}
		public void Dispose()
		{
			Model = null;
			DockControl = null;
		}

		/// <summary>Logic</summary>
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
					m_model.Funds.CollectionChanged -= HandleFundsChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.Funds.CollectionChanged += HandleFundsChanged;
					m_model.Coins.CollectionChanged += HandleCoinsChanged;
					m_model.Exchanges.CollectionChanged += HandleExchangesChanged;
				}

				// Handle funds being created or destroyed
				void HandleFundsChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					CreateFundColumns();
				}
				void HandleCoinsChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					Coins.Refresh();
				}
				void HandleExchangesChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					var list = (List<Exchange>)Exchanges.SourceCollection;
					using (Scope.Create(() => Exchanges.CurrentItem, ci => Exchanges.MoveCurrentTo(ci)))
						list.Assign(Model.TradingExchanges);

					Exchanges.Refresh();
					Coins.Refresh();
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
		public ICollectionView Exchanges { get; }

		/// <summary>The data source for coin data</summary>
		public ICollectionView Coins { get; }

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

				/// <summary>Filter for the 'Coins' collection view</summary>
				bool CoinThresholdFilter(object obj)
				{
					var coin_data = (CoinData)obj;
					var exch = (Exchange)Exchanges.CurrentItem;
					if (exch == null)
						return false;

					var coin = exch.Coins[coin_data.Symbol];
					if (coin == null)
						return false;

					switch (FilterType)
					{
					default: throw new Exception("Unknown Filter Type");
					case ECoinFilterType.Value:
						{
							var live_price = coin.LivePriceAvailable ? (decimal)coin.ValueOf(1m) : 0m;
							return live_price > FilterThreshold;
						}
					case ECoinFilterType.Available:
						{
							var avail_sum = Model.Funds.Sum(x => (decimal)x[coin].Available);
							return avail_sum > FilterThreshold;
						}
					case ECoinFilterType.Total:
						{
							var total_sum = Model.Funds.Sum(x => (decimal)x[coin].Total);
							return total_sum > FilterThreshold;
						}
					case ECoinFilterType.Balance:
						{
							var total_sum = Model.Funds.Sum(x => (decimal)x[coin].Total);
							var live_price = coin.LivePriceAvailable ? (decimal)coin.ValueOf(1m) : 0m;
							return total_sum * live_price > FilterThreshold;
						}
					}
				}
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

		/// <summary>What to filter on</summary>
		public ECoinFilterType FilterType
		{
			get { return (ECoinFilterType)GetValue(FilterTypeProperty); }
			set { SetValue(FilterTypeProperty, value); }
		}
		private void FilterType_Changed() => Coins.Refresh();
		public static readonly DependencyProperty FilterTypeProperty;

		/// <summary>Reset the grid columns to contain a column per fund</summary>
		private void CreateFundColumns()
		{
			// Delete the columns
			for (; m_grid.Columns.Count > 1;)
				m_grid.Columns.RemoveAt(1);

			// Create a column for each fund
			var conv = new CoinDataToFundBalanceConverter(Exchanges);
			var templ = (DataTemplate)FindResource("FundBalanceDataTemplate");
			foreach (var fund in Model.Funds)
			{
				var col = m_grid.Columns.Add2(new DataGridBoundTemplateColumn
				{
					Header = fund.Id,
					Width = new DataGridLength(1.0, DataGridLengthUnitType.Star),
					CellTemplate = templ,
					Binding = new Binding()
					{
						Mode = BindingMode.OneWay,
						Converter = conv,
						ConverterParameter = fund,
					},
				});
			}
		}

		/// <summary>Create a new Fund</summary>
		public Command CreateNewFund { get; }
		private void CreateNewFundInternal()
		{
			var dlg = new PromptUI(Window.GetWindow(this))
			{
				Title = "Fund Name",
				Prompt = "Choose a name for the Fund",
				Validate = x =>
					!x.HasValue() ? new ValidationResult(false, "No value") :
					Model.Funds[x] != null ? new ValidationResult(false, "Fund name already exists") :
					ValidationResult.ValidResult,
			};
			if (dlg.ShowDialog() == true)
			{
				Model.Funds[dlg.Value] = new Fund(dlg.Value);
			}
		}

		/// <summary>Remove a fund and return it's allocation to 'Main'</summary>
		public Command RemoveFund { get; }
		private void RemoveFundInternal()
		{
			var dlg = new ListUI(Window.GetWindow(this))
			{
				Title = "Remove Fund",
				Prompt = "Select the Funds to remove",
				SelectionMode = SelectionMode.Extended,
				AllowCancel = true,
			};
			dlg.Items.AddRange(Model.Funds.Where(x => x.Id != Fund.Main));
			if (dlg.ShowDialog() == true)
			{
				foreach (var fund in dlg.SelectedItems.Cast<Fund>())
					Model.Funds.Remove(fund);
			}
		}

		/// <summary>Show the UI for proportioning funds</summary>
		public Command ShowFundAllocationsUI { get; }
		private void ShowFundAllocationsUIInternal()
		{
			var dlg = new FundAllocationsUI(Window.GetWindow(this), Model)
			{
			};
			dlg.ShowDialog();
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>Converter for displaying a coin balance per exchange, per fund</summary>
		private class CoinDataToFundBalanceConverter : IValueConverter
		{
			private readonly ICollectionView m_exchanges;
			public CoinDataToFundBalanceConverter(ICollectionView exchanges)
			{
				m_exchanges = exchanges;
			}
			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				var coin_data = (CoinData)value;
				var fund = (Fund)parameter;

				// Get the currently selected exchange
				var exch = (Exchange)m_exchanges.CurrentItem;
				if (exch == null)
					return null;

				// Find the coin on the exchange
				var coin = exch.Coins[coin_data.Symbol];
				if (coin == null)
					return null;

				// Get the balance associated with this coin on this exchange in this fund
				var bal = fund[coin];
				return bal;
			}
			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>Extend DataGridTemplateColumn to alloow binding</summary>
		private class DataGridBoundTemplateColumn : DataGridTemplateColumn
		{
			public Binding Binding { get; set; }

			protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
			{
				var element = base.GenerateEditingElement(cell, dataItem);
				element.SetBinding(ContentPresenter.ContentProperty, Binding);
				return element;
			}
			protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
			{
				var element = base.GenerateElement(cell, dataItem);
				element.SetBinding(ContentPresenter.ContentProperty, Binding);
				return element;
			}
		}
	}
}
