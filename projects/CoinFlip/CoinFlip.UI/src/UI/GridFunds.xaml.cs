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
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	public partial class GridFunds : Grid, IDockable, IDisposable
	{
		public GridFunds(Model model)
		{
			InitializeComponent();
			m_grid.MouseRightButtonUp += DataGrid_.ColumnVisibility;
			DockControl = new DockControl(this, "Funds");

			Model = model;
			Filter = new CoinFilter(x => (CoinDataAdapter)x);
			Exchanges = CollectionViewSource.GetDefaultView(model.Exchanges);// { Filter = x => !(x is CrossExchange) };
			Coins = new ListCollectionView(ListAdapter.Create(model.Coins, x => new CoinDataAdapter(this, x), x => x.CoinData)) { Filter = Filter.Predicate };
			Funds = new ListCollectionView(model.Funds);

			//Exchanges.MoveCurrentToFirst();
			Exchanges.CurrentChanged += delegate { Coins.Refresh(); };
			Filter.PropertyChanged += delegate { Coins.Refresh(); };

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
					m_model.Exchanges.CollectionChanged -= HandleExchangeCollectionChanged;
					m_model.Coins.CollectionChanged -= HandleCoinsCollectionChanged;
					m_model.Funds.CollectionChanged -= HandleFundCollectionChanged;
					SettingsData.Settings.SettingChange -= HandleSettingChange;
					Model.BackTestingChange -= HandleBackTestingChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					Model.BackTestingChange += HandleBackTestingChanged;
					SettingsData.Settings.SettingChange += HandleSettingChange;
					m_model.Funds.CollectionChanged += HandleFundCollectionChanged;
					m_model.Coins.CollectionChanged += HandleCoinsCollectionChanged;
					m_model.Exchanges.CollectionChanged += HandleExchangeCollectionChanged;
				}

				// Handle funds being created or destroyed
				void HandleBackTestingChanged(object sender, PrePostEventArgs e)
				{
					if (e.Before) return;
					CreateFundColumns();
					Coins.Refresh();
				}
				void HandleSettingChange(object sender, SettingChangeEventArgs e)
				{
					if (e.Before) return;
					if (e.Key == nameof(SettingsData.LiveFunds) || e.Key == nameof(BackTestingSettings.TestFunds))
					{
						CreateFundColumns();
						Coins.Refresh();
					}
				}
				void HandleFundCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					CreateFundColumns();
					Coins.Refresh();
				}
				void HandleCoinsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					Coins.Refresh();
				}
				void HandleExchangeCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
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

		/// <summary>The available funds.</summary>
		public ICollectionView Funds { get; }

		/// <summary>Filter on 'Coins'</summary>
		public CoinFilter Filter { get; }

		/// <summary>True if there are funds other than the Main fund</summary>
		public bool HasUserFunds => Funds.Cast<Fund>().Any(x => x.Id != Fund.Main);

		/// <summary>Create a new Fund</summary>
		public Command CreateNewFund { get; }
		private void CreateNewFundInternal()
		{
			var dlg = new PromptUI(Window.GetWindow(this))
			{
				Title = "Fund Name",
				Prompt = "Choose a name for the Fund",
				ShowWrapCheckbox = false,
				Validate = x =>
					!x.HasValue() ? new ValidationResult(false, "No value") :
					Model.Funds[x] != null ? new ValidationResult(false, "Fund name already exists") :
					ValidationResult.ValidResult,
			};
			if (dlg.ShowDialog() == true)
			{
				Model.Funds[dlg.Value] = new Fund(dlg.Value);
				Model.SaveFundBalances();
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
				DisplayMember = nameof(Fund.Id),
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
			var dlg = new FundAllocationsUI(Window.GetWindow(this), Model);
			dlg.Exchanges.MoveCurrentTo(Exchanges.CurrentItem);
			dlg.Show();
		}

		/// <summary>Reset the grid columns to contain a column per fund</summary>
		private void CreateFundColumns()
		{
			// Delete the columns
			for (; m_grid.Columns.Count > 1;)
				m_grid.Columns.RemoveAt(1);

			// Create a column for each fund.
			// Rows are bound to a collection of 'CoinDataAdapter'.
			// Each column wants the balance for it's associated fund.
			// The column binding is essentially a factory for creating bindings
			// for each cell in the column.
			// The bindings in 'FundBalanceDataTemplate' are to the properties
			// of the object returned from the converter (which is an IBalance)
			var conv = new CoinDataAdapterToIBalanceConverter();
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

		/// <summary>Wraps 'CoinData' to expose properties for binding</summary>
		private class CoinDataAdapter :IValueTotalAvail, INotifyPropertyChanged
		{
			private readonly GridFunds m_me;
			public CoinDataAdapter(GridFunds me, CoinData cd)
			{
				m_me = me;
				CoinData = cd;
			}

			/// <summary>The wrapped coin data</summary>
			public CoinData CoinData { get; }

			/// <summary>The name of the coin</summary>
			public string Symbol => CoinData.Symbol;

			/// <summary>The currently selected exchange</summary>
			private Exchange Exchange => (Exchange)m_me.Exchanges.CurrentItem;

			/// <summary>The coin on the currently selected exchange (can be null if there is no selected Exchange)</summary>
			public Coin Coin => Exchange?.Coins[Symbol];

			/// <summary>Value of the coin (probably in USD)</summary>
			public decimal Value => Coin?.ValueOf(1m) ?? 0m;

			/// <summary>The total amount of the coin (in coin currency)</summary>
			public Unit<decimal> Total => Coin?.Balances.NettTotal ?? 0m._(Symbol);

			/// <summary>The available amount of the coin (in coin currency)</summary>
			public Unit<decimal> Available => Coin?.Balances.NettAvailable ?? 0m._(Symbol);

			/// <summary>The amount that is locked</summary>
			public Unit<decimal> Held => Coin?.Balances.NettHeld ?? 0m._(Symbol);

			/// <summary></summary>
			public event PropertyChangedEventHandler PropertyChanged;
			public void NotifyPropertyChanged(string prop_name)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
			}
		}

		/// <summary>Converter for converting 'CoinDataAdapter' to an 'IBalance', per exchange, per fund</summary>
		private class CoinDataAdapterToIBalanceConverter : IValueConverter
		{
			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				// Get the balance associated with this coin on this exchange in this fund
				var adpt = (CoinDataAdapter)value;
				var fund = (Fund)parameter;
				var coin = adpt.Coin;
				return coin != null ? fund[coin] : null;
			}
			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			{
				throw new NotImplementedException();
			}
		}
	}
}
