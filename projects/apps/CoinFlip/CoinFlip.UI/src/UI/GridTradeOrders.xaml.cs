using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	public partial class GridTradeOrders : Grid, IDockable, IDisposable, INotifyPropertyChanged
	{
		public GridTradeOrders(Model model)
		{
			InitializeComponent();
			DockControl = new DockControl(this, "Orders");
			Model = model;
			Orders = new ListCollectionView(Array.Empty<Order>());

			CancelOrder = Command.Create(this, CancelOrderInternal);
			ModifyOrder = Command.Create(this, ModifyOrderInternal);

			m_grid.MouseRightButtonUp += DataGrid_.ColumnVisibility;
			m_grid.SelectionChanged += (s, a) =>
			{
				if (m_selecting_orders != 0) return;
				using (Scope.Create(() => ++m_selecting_orders, () => --m_selecting_orders))
				{
					Model.SelectedOpenOrders.Clear();
					foreach (var item in a.AddedItems.Cast<Order>())
						Model.SelectedOpenOrders.Add(item);
				}
			};
			m_grid.MouseDoubleClick += (s, a) =>
			{
				var chart = Model.Charts.ActiveChart;
				if (chart != null)
				{
					ShowCurrentOrderOnChart(chart);
					a.Handled = true;
				}
			};

			DataContext = this;
		}
		public void Dispose()
		{
			Model = null!;
			DockControl = null!;
		}
		private int m_selecting_orders;

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
					m_model.SelectedOpenOrders.CollectionChanged -= HandleSelectedOrdersChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.SelectedOpenOrders.CollectionChanged += HandleSelectedOrdersChanged;
					m_model.Charts.CollectionChanged += HandleChartCollectionChanged;
					Exchanges = CollectionViewSource.GetDefaultView(m_model.Exchanges);
				}

				// Handlers
				void HandleSelectedOrdersChanged(object? sender, NotifyCollectionChangedEventArgs e)
				{
					if (m_selecting_orders != 0) return;
					using (Scope.Create(() => ++m_selecting_orders, () => --m_selecting_orders))
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
				void HandleChartCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
				{
					m_menu_show_on_chart.Items.Clear();
					foreach (var chart in Model.Charts)
					{
						var opt = m_menu_show_on_chart.Items.Add2(new MenuItem { Header = chart.ChartName });
						opt.Click += (s, a) => ShowCurrentOrderOnChart(chart);
					}
				}
			}
		}
		private Model m_model = null!;

		/// <summary>Provides support for the DockContainer</summary>
		public DockControl DockControl
		{
			get => m_dock_control;
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
					Orders = new ListCollectionView(Array.Empty<Exchange>());

					var orders = Exchanges?.CurrentAs<Exchange>()?.Orders;
					if (orders != null)
					{
						var view = new ListCollectionView(orders);
						view.SortDescriptions.Add(new SortDescription(nameof(OrderCompleted.Created), ListSortDirection.Descending));
						Orders = view;

						NotifyPropertyChanged(nameof(Orders));
					}
				}
			}
		}
		private ICollectionView m_exchanges = null!;

		/// <summary>The view of the current live orders</summary>
		public ICollectionView Orders { get; private set; }

		/// <summary>The currently selected exchange</summary>
		public Order? Current
		{
			get => Orders.CurrentAs<Order>();
			set => Orders.MoveCurrentTo(value);
		}

		/// <summary>Cancel the selected order</summary>
		public Command CancelOrder { get; }
		private async void CancelOrderInternal()
		{
			if (Current == null) return;
			try { await Current.CancelOrder(Model.Shutdown.Token); }
			catch (OperationCanceledException) { }
			catch (Exception ex)
			{
				Model.Log.Write(ELogLevel.Error, ex, "Cancelling trade failed");
				await Misc.RunOnMainThread(() =>
				{
					MsgBox.Show(Window.GetWindow(this), ex.MessageFull(), "Cancel Order", MsgBox.EButtons.OK, MsgBox.EIcon.Error);
				});
			}
		}

		/// <summary>Modify an existing order</summary>
		public Command ModifyOrder { get; }
		private void ModifyOrderInternal()
		{
			if (Current == null) return;
			Model.EditTrade(Current);
		}

		/// <summary>Show the chart for the selected trade pair</summary>
		private void ShowCurrentOrderOnChart(IChartView chart)
		{
			if (Current == null) return;
			chart.Exchange = Current.Exchange;
			chart.Pair = Current.Pair;
			chart.TimeFrame = chart.TimeFrame != ETimeFrame.None ? chart.TimeFrame : ETimeFrame.Hour1;
			chart.EnsureActiveContent();
		}

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}
