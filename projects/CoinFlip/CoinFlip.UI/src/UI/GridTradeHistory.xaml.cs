using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
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
			MouseRightButtonUp += DataGrid_.ColumnVisibility;
			DockControl = new DockControl(this, "History");
			PairNames = new ListCollectionView(new List<string>());
			Model = model;


			m_grid.SelectionChanged += (s, a) =>
			{
				foreach (var item in a.RemovedItems.Cast<OrderCompleted>())
					Model.SelectedCompletedOrders.Remove(item);
				foreach (var item in a.AddedItems.Cast<OrderCompleted>())
					Model.SelectedCompletedOrders.Add(item);
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
					Exchanges = CollectionViewSource.GetDefaultView(null);
					m_model.Charts.CollectionChanged -= HandleChartCollectionChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.Charts.CollectionChanged += HandleChartCollectionChanged;
					Exchanges = CollectionViewSource.GetDefaultView(m_model.Exchanges);
				}

				// Handler
				void HandleChartCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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

		/// <summary>The global exchanges view. Provides the source of the "Current" exchange </summary>
		private ICollectionView Exchanges
		{
			get { return m_exchanges; }
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
				HandleCurrentChanged(null, null);

				// Handler
				void HandleCurrentChanged(object sender, EventArgs e)
				{
					var history = ((Exchange)Exchanges?.CurrentItem)?.History;
					History = history != null ? new ListCollectionView(history) : null;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(History)));

					using (Scope.Create(() => PairNames.CurrentItem, n => PairNames.MoveCurrentToOrFirst(n)))
					{
						var list = (List<string>)PairNames.SourceCollection;
						var pairs = ((Exchange)Exchanges?.CurrentItem)?.Pairs.Values;
						list.Assign(pairs != null ? pairs.Select(x => x.Name).Prepend("All Pairs") : new string[0]);
					}
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PairNames)));
				}
			}
		}
		private ICollectionView m_exchanges;

		/// <summary>The view of the trade history</summary>
		public ICollectionView History { get; private set; }

		/// <summary>The names of pairs to filter the list by</summary>
		public ICollectionView PairNames { get; }

		/// <summary>The currently selected exchange</summary>
		public OrderCompleted Current
		{
			get => (OrderCompleted)History?.CurrentItem;
			set => History?.MoveCurrentTo(value);
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

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;
	}
}
