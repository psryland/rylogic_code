using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CoinFlip.Settings;
using CoinFlip.UI.Dialogs;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	public partial class EquityChart :Grid, IDockable, IDisposable, INotifyPropertyChanged
	{
		// Notes:
		//  - This chart is used to show comparitive account value over time.
		//  - The axes are common value vs. time.
		//  - Time0 = The bitcoin epoch. 1 unit = 1 day
		//  - Use current prices
		public EquityChart(Model model)
		{
			InitializeComponent();
			DockControl = new DockControl(this, "Equity");
			Chart = m_chart_equity;
			Equity = new Equity(model);
			GfxEquity = new GfxObjects.Equity(Equity);
			GfxCompletedOrders = new GfxObjects.Confetti();
			GfxTransfers = new GfxObjects.Confetti();
			Model = model;

			// Commands
			ShowChartOptions = Command.Create(this, ShowChartOptionsInternal);
			ToggleIncludeTransfers = Command.Create(this, ToggleIncludeTransfersInternal);
			ToggleShowCompletedOrders = Command.Create(this, ToggleShowCompletedOrdersInternal);
			ToggleShowTransfers = Command.Create(this, ToggleShowTransfersInternal);
			AutoRange = Command.Create(this, AutoRangeInternal);

			IsVisibleChanged += delegate { AutoRangeIfNeeded(); };
			ModifyContextMenus();
			DataContext = this;
		}
		public void Dispose()
		{
			GfxEquity = null;
			GfxCompletedOrders = null;
			GfxTransfers = null;
			Chart = null;
			Equity = null;
			Model = null;
			DockControl = null;
		}

		/// <summary>Logic</summary>
		public Model Model
		{
			get => m_model;
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.OrdersChanging -= RefreshChart;
					m_model.HistoryChanging -= RefreshChart;
					m_model.SelectedCompletedOrders.CollectionChanged -= RefreshChart;
					SettingsData.Settings.SettingChange -= HandleSettingChange;
					Exchanges = CollectionViewSource.GetDefaultView(null);
				}
				m_model = value;
				if (m_model != null)
				{
					Exchanges = CollectionViewSource.GetDefaultView(m_model.Exchanges);
					SettingsData.Settings.SettingChange += HandleSettingChange;
					m_model.SelectedCompletedOrders.CollectionChanged += RefreshChart;
					m_model.HistoryChanging += RefreshChart;
					m_model.OrdersChanging += RefreshChart;
				}

				// Handler
				void HandleSettingChange(object sender, SettingChangeEventArgs e)
				{
					switch (e.Key)
					{
					case nameof(EquitySettings.Since):
						NotifyPropertyChanged(nameof(Since));
						Chart.Scene.Invalidate();
						Chart.AutoRange();
						break;
					case nameof(EquitySettings.IncludeTransfers):
						NotifyPropertyChanged(nameof(IncludeTransfers));
						Chart.Scene.Invalidate();
						Chart.AutoRange();
						break;
					case nameof(EquitySettings.ShowCompletedOrders):
						NotifyPropertyChanged(nameof(ShowCompletedOrders));
						Chart.Scene.Invalidate();
						break;
					case nameof(EquitySettings.ShowTransfers):
						NotifyPropertyChanged(nameof(ShowTransfers));
						Chart.Scene.Invalidate();
						break;
					case nameof(ChartSettings.XAxisLabelMode):
						Chart.XAxisPanel.Invalidate();
						break;
					case nameof(ChartSettings.ConfettiLabelSize):
						Chart.Scene.Invalidate();
						break;
					case nameof(ChartSettings.ConfettiLabelTransparency):
						Chart.Scene.Invalidate();
						break;
					case nameof(ChartSettings.ConfettiDescriptionsVisible):
						Chart.Scene.Invalidate();
						break;
					case nameof(ChartSettings.ConfettiLabelsToTheLeft):
						Chart.Scene.Invalidate();
						break;
					}
				}
				void RefreshChart(object sender, EventArgs args)
				{
					Chart?.Invalidate();
				}
			}
		}
		private Model m_model;

		/// <summary>The source equity data</summary>
		public Equity Equity
		{
			get => m_equity;
			set
			{
				if (m_equity == value) return;
				if (m_equity != null)
				{
					m_equity.EquityChanged -= HandleEquityChanged;
				}
				m_equity = value;
				if (m_equity != null)
				{
					m_equity.EquityChanged += HandleEquityChanged;
				}

				// Handler
				void HandleEquityChanged(object sender = null, EventArgs e = null)
				{
					// Auto range on the first new data
					if (m_equity_count == 0 && (m_equity_count = Equity.Count) != 0)
						Chart.AutoRange();
					else
						AutoRangeIfNeeded();

					Chart.Invalidate();
				}
			}
		}
		private Equity m_equity;
		private int m_equity_count;

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

		/// <summary>The chart control</summary>
		public ChartControl Chart
		{
			get { return m_chart; }
			private set
			{
				if (m_chart == value) return;
				if (m_chart != null)
				{
					m_chart.ChartMoved -= HandleMoved;
					m_chart.BuildScene -= HandleBuildScene;
					//m_chart.MouseDown -= HandleMouseDown;
					m_chart.AutoRanging -= HandleAutoRanging;
					m_chart.XAxis.TickText = m_chart.XAxis.DefaultTickText;
					m_chart.YAxis.TickText = m_chart.YAxis.DefaultTickText;
					Util.Dispose(ref m_chart);
				}
				m_chart = value;
				if (m_chart != null)
				{
					// Customise the chart for candles
					m_chart.Options.Antialiasing = true;
					m_chart.Options.Orthographic = true;
					m_chart.Options.SelectionColour = new Colour32(0x8092A1B1);
					//m_chart.Options.CrossHairZOffset = ZOrder.Cursors;
					m_chart.XAxis.Options.PixelsPerTick = 50.0;
					m_chart.XAxis.Options.TickTextTemplate = "XX:XX\r\nXXX XX XXXX";
					m_chart.YAxis.Options.TickTextTemplate = "X.XXXX";
					m_chart.XAxis.TickText = HandleChartXAxisTickText;
					//m_chart.YAxis.TickText = HandleChartYAxisTickText;
					//m_chart.YAxis.Options.Side = Dock.Right;
					m_chart.AutoRanging += HandleAutoRanging;
					//m_chart.MouseDown += HandleMouseDown;
					m_chart.BuildScene += HandleBuildScene;
					m_chart.ChartMoved += HandleMoved;
					m_chart.PreviewKeyDown += (s, a) =>
					{
						if (a.Key == Key.F5)
							Equity.Invalidate();
					};
				}

				// Handlers
				string HandleChartXAxisTickText(double x, double? step = null)
				{
					if (SettingsData.Settings.Chart.XAxisLabelMode == EXAxisLabelMode.AxisIndex)
						return x.ToString();

					var idx = (int)x;
					if (idx < 0 || idx > Equity.Count)
						return string.Empty;

					var dt = idx == Equity.Count ? Model.UtcNow : Equity.BalanceChanges[idx].Time;
					dt = (SettingsData.Settings.Chart.XAxisLabelMode == EXAxisLabelMode.LocalTime)
						? dt.LocalDateTime
						: dt.UtcDateTime;
					
					return dt.ToString("HH:mm'\r\n'dd-MMM-yy");
				}
				void HandleAutoRanging(object sender, ChartControl.AutoRangeEventArgs e)
				{
					var bb = BBox.Reset;
					if (e.Axes.HasFlag(ChartControl.EAxis.XAxis) && e.Axes.HasFlag(ChartControl.EAxis.YAxis))
					{
						// Display the full equity history
						var xrange = new RangeF(0, Equity.BalanceChanges.Count);
						var yrange = RangeF.Invalid;
						foreach (var chg in Equity.NettWorthHistory())
							yrange.Grow(chg.Worth.ToDouble());

						bb = BBox.Union(bb, new v4((float)xrange.Beg, (float)yrange.Beg, -ZOrder.Max, 1f));
						bb = BBox.Union(bb, new v4((float)xrange.End, (float)yrange.End, +ZOrder.Max, 1f));
					}
					else if (e.Axes.HasFlag(ChartControl.EAxis.YAxis))
					{
						throw new NotImplementedException();
						//var idx_min = (int)Chart.XAxis.Min;
						//var idx_max = (int)Chart.XAxis.Max;
						//foreach (var candle in Instrument.CandleRange(idx_min, idx_max))
						//{
						//	bb = BBox.Union(bb, new v4(idx_min, (float)candle.Low, -ZOrder.Max, 1f));
						//	bb = BBox.Union(bb, new v4(idx_max, (float)candle.High, +ZOrder.Max, 1f));
						//}
					}
					else if (e.Axes.HasFlag(ChartControl.EAxis.XAxis))
					{
						throw new NotImplementedException();
						//// Display the last few candles @ N pixels per candle
						//var width = (int)(Chart.Scene.ActualWidth / 6); // in candles
						//var idx_min = Instrument.Count - width * 4 / 5;
						//var idx_max = Instrument.Count + width * 1 / 5;
						//bb = BBox.Union(bb, new v4(idx_min, (float)Chart.YAxis.Min, -ZOrder.Max, 1f));
						//bb = BBox.Union(bb, new v4(idx_max, (float)Chart.YAxis.Max, +ZOrder.Max, 1f));
					}
					else
					{
						bb = BBox.Union(bb, new v4((float)Chart.XAxis.Min, (float)Chart.YAxis.Min, -ZOrder.Max, 1f));
						bb = BBox.Union(bb, new v4((float)Chart.XAxis.Max, (float)Chart.YAxis.Max, +ZOrder.Max, 1f));
					}

					// Swell the box a little for margins
					if (bb.IsValid)
					{
						bb.Radius = new v4(bb.Radius.x, bb.Radius.y * 1.1f, bb.Radius.z, 0f);
						e.ViewBBox = bb;
						e.Handled = true;
					}
				}
				void HandleBuildScene(object sender, View3dControl.BuildSceneEventArgs e)
				{
					BuildScene();
				}
				void HandleMoved(object sender, ChartControl.ChartMovedEventArgs e)
				{
					NotifyPropertyChanged(nameof(VisibleTimeSpan));
				}
			}
		}
		private ChartControl m_chart;

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
					NotifyPropertyChanged(nameof(History));
				}
			}
		}
		private ICollectionView m_exchanges;

		/// <summary>The view of the trade history</summary>
		public ICollectionView History { get; private set; }

		/// <summary>The maximum range of the equity data</summary>
		public DateTimeOffset MinEquityTime => Equity.Count != 0 ? Equity.BalanceChanges.Front().Time : Misc.CryptoCurrencyEpoch;
		public DateTimeOffset MaxEquityTime => Model.UtcNow;

		/// <summary>A string description of the period of time shown in the chart</summary>
		public string VisibleTimeSpan
		{
			get
			{
				try
				{
					var span = Chart.XAxis.Range.Size;
					return TimeSpan.FromDays(span).ToPrettyString();
				}
				catch
				{
					return string.Empty;
				}
			}
		}

		/// <summary>The start time to display the equity from</summary>
		public DateTimeOffset Since
		{
			get => SettingsData.Settings.Equity.Since;
			set => SettingsData.Settings.Equity.Since = value;
		}

		/// <summary>Include deposits/withdrawals in the equity data</summary>
		public bool IncludeTransfers
		{
			get => SettingsData.Settings.Equity.IncludeTransfers;
			set => SettingsData.Settings.Equity.IncludeTransfers = value;
		}

		/// <summary>Show completed orders on the chart</summary>
		public EShowItems ShowCompletedOrders
		{
			get => SettingsData.Settings.Equity.ShowCompletedOrders;
			set => SettingsData.Settings.Equity.ShowCompletedOrders = value;
		}

		/// <summary>Show completed orders on the chart</summary>
		public EShowItems ShowTransfers
		{
			get => SettingsData.Settings.Equity.ShowTransfers;
			set => SettingsData.Settings.Equity.ShowTransfers = value;
		}

		/// <summary>Add graphics and elements to the chart</summary>
		private void BuildScene()
		{
			Chart.Scene.Window.RemoveObjects(new[] { CtxId }, 1, 0);
			if (!DockControl.IsVisible)
				return;

			// Line plot
			GfxEquity.BuildScene(Chart);

			// Get a snapshot of the nett worth history
			var visible_time_range = Equity.TimeRange(Chart.XAxis.Range);
			var nett_worth_history_snapshot = Equity.NettWorthHistory().Select(x => x).ToList();
			nett_worth_history_snapshot.Reverse();

			// Completed order markers
			{
				GfxCompletedOrders.Position = OrderToPosition;

				// Convert the visible axis range to a time range
				switch (ShowCompletedOrders)
				{
				default: throw new Exception($"Unknown 'ShowCompletedOrders' mode: {ShowCompletedOrders}");
				case EShowItems.Disabled:
					{
						GfxCompletedOrders.ClearScene();
						break;
					}
				case EShowItems.Selected:
					{
						var orders = Model.SelectedCompletedOrders
							.Where(x => visible_time_range.Contains(x.Created.Ticks))
							.Select(x => new OrderCompletedConfettiAdapter(x));

						GfxCompletedOrders.BuildScene(orders, null, Chart);
						break;
					}
				case EShowItems.All:
					{
						var exchange = Exchanges.CurrentAs<Exchange>();
						if (exchange != null)
						{
							var orders = exchange.History
								.Where(x => visible_time_range.Contains(x.Created.Ticks))
								.Select(x => new OrderCompletedConfettiAdapter(x));
							var highlighted = Model.SelectedCompletedOrders
								.Where(x => visible_time_range.Contains(x.Created.Ticks))
								.Select(x => new OrderCompletedConfettiAdapter(x));

							GfxCompletedOrders.BuildScene(orders, highlighted, Chart);
						}
						break;
					}
				}

				// Callbacks for positioning order graphics
				v4 OrderToPosition(GfxObjects.Confetti.IItem item)
				{
					var order = (OrderCompletedConfettiAdapter)item;
					if (nett_worth_history_snapshot.Count == 0) return v4.Origin;
					var X = nett_worth_history_snapshot.BinarySearch(x => x.Time.CompareTo(order.Order.Created), find_insert_position: true);
					var Y = nett_worth_history_snapshot[Math.Min(X, nett_worth_history_snapshot.Count-1)].Worth;
					return new v4((float)X, (float)Y.ToDouble(), 0, 1);
				}
			}

			// Transfers
			if (IncludeTransfers)
			{
				GfxTransfers.Position = OrderToPosition;

				// Convert the visible axis range to a time range
				switch (ShowTransfers)
				{
				default: throw new Exception($"Unknown 'ShowTransfers' mode: {ShowTransfers}");
				case EShowItems.Disabled:
					{
						GfxTransfers.ClearScene();
						break;
					}
				case EShowItems.Selected:
					{
						var transfers = Model.SelectedTransfers
							.Where(x => visible_time_range.Contains(x.Created.Ticks))
							.Select(x => new TransfersConfettiAdapter(x))
							.ToList();

						GfxTransfers.BuildScene(transfers, null, Chart);
						break;
					}
				case EShowItems.All:
					{
						var exchange = Exchanges.CurrentAs<Exchange>();
						if (exchange != null)
						{
							var transfers = exchange.Transfers
								.Where(x => visible_time_range.Contains(x.Created.Ticks))
								.Select(x => new TransfersConfettiAdapter(x))
								.ToList();
							var highlighted = Model.SelectedTransfers
								.Where(x => visible_time_range.Contains(x.Created.Ticks))
								.Select(x => new TransfersConfettiAdapter(x))
								.ToList();

							GfxTransfers.BuildScene(transfers, highlighted, Chart);
						}
						break;
					}
				}

				// Callbacks for positioning order graphics
				v4 OrderToPosition(GfxObjects.Confetti.IItem item)
				{
					var xfer = (TransfersConfettiAdapter)item;
					if (nett_worth_history_snapshot.Count == 0) return v4.Origin;
					var X = nett_worth_history_snapshot.BinarySearch(x => x.Time.CompareTo(xfer.Transfer.Created), find_insert_position: true);
					var Y = nett_worth_history_snapshot[Math.Min(X, nett_worth_history_snapshot.Count - 1)].Worth;
					return new v4((float)X, (float)Y.ToDouble(), 0, 1);
				}
			}
			else
			{
				GfxTransfers.ClearScene();
			}
		}

		/// <summary>Graphics objects for the candle data</summary>
		private GfxObjects.Equity GfxEquity
		{
			get { return m_gfx_equity; }
			set
			{
				if (m_gfx_equity == value) return;
				Util.Dispose(ref m_gfx_equity);
				m_gfx_equity = value;
			}
		}
		private GfxObjects.Equity m_gfx_equity;

		/// <summary>Graphics for completed orders</summary>
		private GfxObjects.Confetti GfxCompletedOrders
		{
			get => m_gfx_completed_orders;
			set
			{
				if (m_gfx_completed_orders == value) return;
				Util.Dispose(ref m_gfx_completed_orders);
				m_gfx_completed_orders = value;
			}
		}
		private GfxObjects.Confetti m_gfx_completed_orders;
		private class OrderCompletedConfettiAdapter :GfxObjects.Confetti.IItem
		{
			public OrderCompletedConfettiAdapter(OrderCompleted order)
			{
				Order = order;
			}
			public OrderCompleted Order { get; }
			public Polygon Icon => new Polygon
			{
				Stroke = Brushes.Black,
				Points = Order.TradeType == ETradeType.Q2B
					? new PointCollection(new[] { new Point(0, 0), new Point(-5, +5), new Point(+5, +5) })
					: new PointCollection(new[] { new Point(0, 0), new Point(-5, -5), new Point(+5, -5) }),
			};
			public TextBlock Label => new TextBlock
			{
				Text = Order.Description,
				FontSize = 8.0,
			};
			public Colour32 Colour => Order.TradeType == ETradeType.Q2B
				? SettingsData.Settings.Chart.Q2BColour.Darken(0.5f)
				: SettingsData.Settings.Chart.B2QColour.Darken(0.5f);

			#region Equals
			public static bool operator ==(OrderCompletedConfettiAdapter lhs, OrderCompletedConfettiAdapter rhs)
			{
				return ReferenceEquals(lhs.Order, rhs.Order);
			}
			public static bool operator !=(OrderCompletedConfettiAdapter lhs, OrderCompletedConfettiAdapter rhs)
			{
				return !ReferenceEquals(lhs.Order, rhs.Order);
			}
			public override bool Equals(object obj)
			{
				return obj is OrderCompletedConfettiAdapter rhs && Order == rhs.Order;
			}
			public override int GetHashCode()
			{
				return Order.GetHashCode();
			}
			#endregion
		}

		/// <summary>Graphics for transfers</summary>
		private GfxObjects.Confetti GfxTransfers
		{
			get => m_gfx_transfers;
			set
			{
				if (m_gfx_transfers == value) return;
				Util.Dispose(ref m_gfx_transfers);
				m_gfx_transfers = value;
			}
		}
		private GfxObjects.Confetti m_gfx_transfers;
		private class TransfersConfettiAdapter :GfxObjects.Confetti.IItem
		{
			public TransfersConfettiAdapter(Transfer transfer)
			{
				Transfer = transfer;
			}
			public Transfer Transfer { get; }
			public Polygon Icon => new Polygon
			{
				Stroke = Brushes.Black,
				Points = Transfer.Type == ETransfer.Deposit
						? new PointCollection(new[] { new Point(0, 0), new Point(-5, +5), new Point(+5, +5) })
						: new PointCollection(new[] { new Point(0, 0), new Point(-5, -5), new Point(+5, -5) }),
			};
			public TextBlock Label => new TextBlock
			{
				Text = Transfer.Description,
				FontSize = 8.0,
			};
			public Colour32 Colour => Transfer.Type == ETransfer.Deposit
				? SettingsData.Settings.Chart.Q2BColour.Darken(0.5f)
				: SettingsData.Settings.Chart.B2QColour.Darken(0.5f);
		}

		/// <summary>Auto range the chart if not spanning the data</summary>
		private void AutoRangeIfNeeded()
		{
			// If the chart X axis range does not overlap the equity data range, auto range.
			if (Chart.XAxis.Range.Intersect(new RangeI(0, Equity.Count)).Empty)
				AutoRange.Execute();
		}

		/// <summary>Auto range the chart (if needed)</summary>
		public Command AutoRange { get; }
		private void AutoRangeInternal()
		{
			Chart.AutoRange();
		}

		/// <summary>Include deposits/withdrawals in the equity data</summary>
		public Command ToggleIncludeTransfers { get; }
		private void ToggleIncludeTransfersInternal()
		{
			IncludeTransfers = !IncludeTransfers;
		}

		/// <summary>Cycle the 'show completed orders' state</summary>
		public Command ToggleShowCompletedOrders { get; }
		private void ToggleShowCompletedOrdersInternal()
		{
			ShowCompletedOrders = Enum<EShowItems>.Cycle(ShowCompletedOrders);
		}

		/// <summary>Cycle the 'show transfers' state</summary>
		public Command ToggleShowTransfers { get; }
		private void ToggleShowTransfersInternal()
		{
			ShowTransfers = Enum<EShowItems>.Cycle(ShowTransfers);
		}

		/// <summary>Show the options dialog for chart behaviour</summary>
		public Command ShowChartOptions { get; }
		private void ShowChartOptionsInternal()
		{
			var owner = Window.GetWindow(this);
			var pt = owner.PointToScreen(Mouse.GetPosition(owner));
			ChartOptionsUI.Show(owner, pt);
		}

		/// <summary>Replace the chart context menus</summary>
		private void ModifyContextMenus()
		{
			// Modify the main chart area menu
			{
				var cmenu = Chart.Scene.ContextMenu;

				// // Move the existing chart menu into a sub menu
				// var chart_options_menu = new MenuItem { Header = "Chart Options" };
				// {
				// 	var items = cmenu.Items.Cast<object>().ToList();
				// 	cmenu.Items.Clear();
				// 	chart_options_menu.Items.AddRange(items);
				// }

				cmenu.Items.Add(new Separator());

				// Add the chart options menu at the end
			//	cmenu.Items.Add(chart_options_menu);

				// Add this last so it occurs after the other handlers attached to 'Opened'
				cmenu.Opened += Gui_.TidySeparators;
			}

			// Modify the XAxis context menu
			{
				var cmenu = Chart.XAxis.ContextMenu;

				// Insert an option for changing the units of the XAxis
				var idx = 0;
				{
					var opt = cmenu.Items.Insert2(idx++, new MenuItem { Header = "Label Mode", DataContext = SettingsData.Settings.Chart });
					{
						var cb = opt.Items.Add2(new ComboBox
						{
							Style = FindResource(System.Windows.Controls.ToolBar.ComboBoxStyleKey) as Style,
							ItemsSource = Enum<EXAxisLabelMode>.ValuesArray,
							Background = SystemColors.ControlBrush,
							BorderThickness = new Thickness(0),
							Margin = new Thickness(1, 1, 20, 1),
							MinWidth = 80,
						});
						cb.SetBinding(ComboBox.SelectedItemProperty, new Binding(nameof(ChartSettings.XAxisLabelMode)) { Mode = BindingMode.TwoWay });
					}
				}
			}
		}

		/// <summary>Accessors for setting the context menus in XAML</summary>
		public ContextMenu SceneCMenu
		{
			get => Chart.Scene.ContextMenu;
			set => Chart.Scene.ContextMenu = value;
		}
		public ContextMenu XAxisCMenu
		{
			get => Chart.XAxisPanel.ContextMenu;
			set => Chart.XAxisPanel.ContextMenu = value;
		}
		public ContextMenu YAxisCMenu
		{
			get => Chart.YAxisPanel.ContextMenu;
			set => Chart.YAxisPanel.ContextMenu = value;
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}

		/// <summary>Z-values for chart elements</summary>
		public static class ZOrder
		{
			// Grid lines are drawn at 0
			public const float Min = 0f;
			public const float Equity = 0.005f;
			public const float Max = 0.1f;
		}

		/// <summary>Context for chart graphics</summary>
		public static readonly Guid CtxId = Guid.NewGuid();
	}
}
