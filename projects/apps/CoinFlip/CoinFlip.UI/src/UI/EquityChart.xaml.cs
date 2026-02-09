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
			History = new ListCollectionView(Array.Empty<Exchange>());
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
			GfxEquity = null!;
			GfxCompletedOrders = null!;
			GfxTransfers = null!;
			Chart = null!;
			Equity = null!;
			Model = null!;
			DockControl = null!;
		}

		/// <summary>Logic</summary>
		public Model Model
		{
			get;
			private set
			{
				if (field == value) return;
				if (field != null)
				{
					field.OrdersChanging -= RefreshChart;
					field.HistoryChanging -= RefreshChart;
					field.SelectedCompletedOrders.CollectionChanged -= RefreshChart;
					SettingsData.Settings.SettingChange -= HandleSettingChange;
					Exchanges = CollectionViewSource.GetDefaultView(null);
				}
				field = value;
				if (field != null)
				{
					Exchanges = CollectionViewSource.GetDefaultView(field.Exchanges);
					SettingsData.Settings.SettingChange += HandleSettingChange;
					field.SelectedCompletedOrders.CollectionChanged += RefreshChart;
					field.HistoryChanging += RefreshChart;
					field.OrdersChanging += RefreshChart;
				}

				// Handler
				void HandleSettingChange(object? sender, SettingChangeEventArgs e)
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
				void RefreshChart(object? sender, EventArgs args)
				{
					Chart?.Invalidate();
				}
			}
		} = null!;

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
				void HandleEquityChanged(object? sender, EventArgs e)
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
		private Equity m_equity = null!;
		private int m_equity_count;

		/// <summary>Provides support for the DockContainer</summary>
		public DockControl DockControl
		{
			get;
			private set
			{
				if (field == value) return;
				Util.Dispose(ref field!);
				field = value;
			}
		} = null!;

		/// <summary>The chart control</summary>
		public ChartControl Chart
		{
			get;
			private set
			{
				if (field == value) return;
				if (field != null)
				{
					field.ChartMoved -= HandleMoved;
					field.BuildScene -= HandleBuildScene;
					//field.MouseDown -= HandleMouseDown;
					field.AutoRanging -= HandleAutoRanging;
					field.XAxis.TickText = field.XAxis.DefaultTickText;
					field.YAxis.TickText = field.YAxis.DefaultTickText;
					Util.Dispose(ref field!);
				}
				field = value;
				if (field != null)
				{
					// Customise the chart for candles
					field.Options.Antialiasing = true;
					field.Options.Orthographic = true;
					field.Options.SelectionColour = new Colour32(0x8092A1B1);
					//field.Options.CrossHairZOffset = ZOrder.Cursors;
					field.XAxis.Options.PixelsPerTick = 50.0;
					field.XAxis.Options.TickTextTemplate = "XX:XX\r\nXXX XX XXXX";
					field.YAxis.Options.TickTextTemplate = "X.XXXX";
					field.XAxis.TickText = HandleChartXAxisTickText;
					//field.YAxis.TickText = HandleChartYAxisTickText;
					//field.YAxis.Options.Side = Dock.Right;
					field.AutoRanging += HandleAutoRanging;
					//field.MouseDown += HandleMouseDown;
					field.BuildScene += HandleBuildScene;
					field.ChartMoved += HandleMoved;
					field.PreviewKeyDown += (s, a) =>
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
				void HandleAutoRanging(object? sender, ChartControl.AutoRangeEventArgs e)
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
				void HandleBuildScene(object? sender, View3dControl.BuildSceneEventArgs e)
				{
					BuildScene();
				}
				void HandleMoved(object? sender, ChartControl.ChartMovedEventArgs e)
				{
					NotifyPropertyChanged(nameof(VisibleTimeSpan));
				}
			}
		} = null!;

		/// <summary>The global exchanges view. Provides the source of the "Current" exchange </summary>
		private ICollectionView Exchanges
		{
			get;
			set
			{
				if (field == value) return;
				if (field != null)
				{
					field.CurrentChanged -= HandleCurrentChanged;
				}
				field = value;
				if (field != null)
				{
					field.CurrentChanged += HandleCurrentChanged;
				}
				HandleCurrentChanged(null, EventArgs.Empty);

				// Handler
				void HandleCurrentChanged(object? sender, EventArgs e)
				{
					var history = Exchanges.CurrentAs<Exchange>()?.History;
					History = history != null ? new ListCollectionView(history) : new ListCollectionView(Array.Empty<Exchange>());
					NotifyPropertyChanged(nameof(History));
				}
			}
		} = null!;

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

						GfxCompletedOrders.BuildScene(orders, Enumerable.Empty<GfxObjects.Confetti.IItem>(), Chart);
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
					default:
					{
						throw new Exception($"Unknown 'ShowCompletedOrders' mode: {ShowCompletedOrders}");
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

						GfxTransfers.BuildScene(transfers, Enumerable.Empty<GfxObjects.Confetti.IItem>(), Chart);
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
					default:
					{
						throw new Exception($"Unknown 'ShowTransfers' mode: {ShowTransfers}");
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
			get;
			set
			{
				if (field == value) return;
				Util.Dispose(ref field!);
				field = value;
			}
		} = null!;

		/// <summary>Graphics for completed orders</summary>
		private GfxObjects.Confetti GfxCompletedOrders
		{
			get;
			set
			{
				if (field == value) return;
				Util.Dispose(ref field!);
				field = value;
			}
		} = null!;

		/// <summary>Graphics for transfers</summary>
		private GfxObjects.Confetti GfxTransfers
		{
			get;
			set
			{
				if (field == value) return;
				Util.Dispose(ref field!);
				field = value;
			}
		} = null!;

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

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));

		/// <summary>Adapaters</summary>
		private class OrderCompletedConfettiAdapter : GfxObjects.Confetti.IItem
		{
			public OrderCompletedConfettiAdapter(OrderCompleted order)
			{
				Order = order;
			}
			public OrderCompleted Order { get; }
			public Polygon Icon => new()
			{
				Stroke = Brushes.Black,
				Points = Order.TradeType == ETradeType.Q2B
					? new PointCollection(new[] { new Point(0, 0), new Point(-5, +5), new Point(+5, +5) })
					: new PointCollection(new[] { new Point(0, 0), new Point(-5, -5), new Point(+5, -5) }),
			};
			public TextBlock Label => new()
			{
				Text = Order.Description,
				FontSize = 8.0,
			};
			public Colour32 Colour => Order.TradeType == ETradeType.Q2B
				? SettingsData.Settings.Chart.Q2BColour.Darken(0.5f)
				: SettingsData.Settings.Chart.B2QColour.Darken(0.5f);

			#region Equals
			public static bool operator ==(OrderCompletedConfettiAdapter? lhs, OrderCompletedConfettiAdapter? rhs)
			{
				if (lhs == null && rhs == null) return true;
				if (lhs == null || rhs == null) return false;
				return ReferenceEquals(lhs.Order, rhs.Order);
			}
			public static bool operator !=(OrderCompletedConfettiAdapter? lhs, OrderCompletedConfettiAdapter? rhs)
			{
				return !(lhs == rhs);
			}
			public override bool Equals(object? obj)
			{
				return obj is OrderCompletedConfettiAdapter rhs && Order == rhs.Order;
			}
			public override int GetHashCode()
			{
				return Order.GetHashCode();
			}
			#endregion
		}
		private class TransfersConfettiAdapter : GfxObjects.Confetti.IItem
		{
			public TransfersConfettiAdapter(Transfer transfer)
			{
				Transfer = transfer;
			}
			public Transfer Transfer { get; }
			public Polygon Icon => new()
			{
				Stroke = Brushes.Black,
				Points = Transfer.Type == ETransfer.Deposit
						? new PointCollection(new[] { new Point(0, 0), new Point(-5, +5), new Point(+5, +5) })
						: new PointCollection(new[] { new Point(0, 0), new Point(-5, -5), new Point(+5, -5) }),
			};
			public TextBlock Label => new()
			{
				Text = Transfer.Description,
				FontSize = 8.0,
			};
			public Colour32 Colour => Transfer.Type == ETransfer.Deposit
				? SettingsData.Settings.Chart.Q2BColour.Darken(0.5f)
				: SettingsData.Settings.Chart.B2QColour.Darken(0.5f);
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
