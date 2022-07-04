using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	public partial class GridTradeHistory : Grid, IDockable, IDisposable, INotifyPropertyChanged
	{
		// Notes:
		//   - Historic trades are instances of 'OrderCompleted' classes. They contain
		//     one or more 'TradeCompleted' classes.

		public GridTradeHistory(Model model)
		{
			InitializeComponent();
			DockControl = new DockControl(this, "History");
			PairNames = new ListCollectionView(new List<string>());
			Model = model;

			m_grid.MouseRightButtonUp += DataGrid_.ColumnVisibility;
			m_grid.SelectionChanged += (s, a) =>
			{
				if (m_selecting != 0) return;
				using (Scope.Create(() => ++m_selecting, () => --m_selecting))
				{
					Model.SelectedCompletedOrders.Clear();
					foreach (var item in a.AddedItems.Cast<OrderCompleted>())
						Model.SelectedCompletedOrders.Add(item);
				}
			};
			m_grid.MouseDoubleClick += (s, a) =>
			{
				var chart = Model.Charts.ActiveChart;
				if (chart != null)
					ShowCurrentCompletedOrderOnChart(chart);
			};

			DataContext = this;
		}
		public void Dispose()
		{
			Model = null!;
			DockControl = null!;
		}
		private int m_selecting;

		/// <summary></summary>
		public Model Model
		{
			get => m_model;
			set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					Exchanges = CollectionViewSource.GetDefaultView(null);
					m_model.Charts.CollectionChanged -= HandleChartCollectionChanged;
					m_model.SelectedCompletedOrders.CollectionChanged -= HandleSelectedOrdersChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.SelectedCompletedOrders.CollectionChanged += HandleSelectedOrdersChanged;
					m_model.Charts.CollectionChanged += HandleChartCollectionChanged;
					Exchanges = CollectionViewSource.GetDefaultView(m_model.Exchanges);
				}

				// Handler
				void HandleSelectedOrdersChanged(object? sender, NotifyCollectionChangedEventArgs e)
				{
					if (e.Action == NotifyCollectionChangedAction.Add)
					{
						if (m_selecting != 0) return;
						using (Scope.Create(() => ++m_selecting, () => --m_selecting))
						{
							switch (e.Action)
							{
								case NotifyCollectionChangedAction.Reset:
								{
									m_grid.SelectedItems.Clear();
									break;
								}
								case NotifyCollectionChangedAction.Add:
								{
									foreach (var item in e.NewItems())
										m_grid.SelectedItems.Add(item);
									break;
								}
								case NotifyCollectionChangedAction.Remove:
								{
									foreach (var item in e.OldItems())
										m_grid.SelectedItems.Remove(item);
									break;
								}
							}
						}
					}
				}
				void HandleChartCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
				{
					m_menu_show_on_chart.Items.Clear();
					foreach (var chart in Model.Charts)
					{
						var opt = m_menu_show_on_chart.Items.Add2(new MenuItem { Header = chart.ChartName });
						opt.Click += (s, a) => ShowCurrentCompletedOrderOnChart(chart);
					}
				}
			}
		}
		private Model m_model = null!;

		/// <summary>Provides support for the DockContainer</summary>
		public DockControl DockControl
		{
			get { return m_dock_control; }
			private set
			{
				if (m_dock_control == value) return;
				Util.Dispose(ref m_dock_control!);
				m_dock_control = value;
			}
		}
		private DockControl m_dock_control = null!;

		/// <summary>The global exchanges view. Provides the source of the "Current" exchange </summary>
		private ICollectionView Exchanges
		{
			get => m_exchanges;
			set
			{
				if (m_exchanges == value) return;
				if (m_exchanges != null)
				{
					m_exchanges.CurrentChanged -= HandleCurrentChanged;
				}
				m_exchanges = value;
				if (m_exchanges != null)
				{
					m_exchanges.CurrentChanged += HandleCurrentChanged;
				}
				HandleCurrentChanged(null, EventArgs.Empty);

				// Handler
				void HandleCurrentChanged(object? sender, EventArgs e)
				{
					History = null!;// new ListCollectionView(new OrdersCompletedCollection());

					var history = Exchanges?.CurrentAs<Exchange>()?.History;
					if (history != null)
					{
						var view = new ListCollectionView(history);
						view.SortDescriptions.Add(new SortDescription(nameof(OrderCompleted.Created), ListSortDirection.Descending));
						History = view;

						NotifyPropertyChanged(nameof(History));
					}
				}
			}
		}
		private ICollectionView m_exchanges = null!;

		/// <summary>The history of the currently selected exchange</summary>
		public ICollectionView History
		{
			get => m_history;
			private set
			{
				if (m_history == value) return;
				if (m_history != null)
				{
					m_history.Source<OrdersCompletedCollection>().CollectionChanged -= HandleHistoryCollectionChanged;
				}
				m_history = value;
				if (m_history != null)
				{
					m_history.Source<OrdersCompletedCollection>().CollectionChanged += HandleHistoryCollectionChanged;
				}
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(History)));
				UpdatePairsFilter();

				// Handlers
				void HandleHistoryCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
				{
				}
				void UpdatePairsFilter()
				{
					// Update the pairs filter
					using (Scope.Create(() => PairNames.CurrentItem, n => PairNames.MoveCurrentToOrFirst(n)))
					{
						var list = (List<string>)PairNames.SourceCollection;
						var pairs = Exchanges?.CurrentAs<Exchange>()?.Pairs;
						list.Assign(pairs != null ? pairs.Select(x => x.Name).Prepend("All Pairs") : new string[0]);
					}
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PairNames)));
				}
			}
		}
		private ICollectionView m_history = null!;

		/// <summary>The names of pairs to filter the list by</summary>
		public ICollectionView PairNames { get; }

		/// <summary>The currently selected exchange</summary>
		public OrderCompleted Current
		{
			get => History.CurrentAs<OrderCompleted>();
			set => History.MoveCurrentTo(value);
		}

		/// <summary>Show the chart for the selected trade pair</summary>
		private void ShowCurrentCompletedOrderOnChart(IChartView chart)
		{
			if (Current == null) return;
			chart.Exchange = Current.Exchange;
			chart.Pair = Current.Pair;
			chart.TimeFrame = chart.TimeFrame != ETimeFrame.None ? chart.TimeFrame : ETimeFrame.Hour1;
			chart.EnsureActiveContent();
			chart.ScrollToTime(Current.Created);
		}

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}
