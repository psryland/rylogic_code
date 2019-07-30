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
			GfxCompletedOrders = new GfxObjects.Orders(IOrderToXValue, IOrderToYValue);
			Legend = new ChartDataLegend { Chart = Chart, BackColour = 0xFFE0E0E0 };
			Model = model;

			ModifyContextMenus();
			DataContext = this;
		}
		public void Dispose()
		{
			Legend = null;
			GfxEquity = null;
			Chart = null;
			Model = null;
			DockControl = null;
		}

		/// <summary>Logic</summary>
		public Model Model
		{
			get { return m_model; }
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.SelectedCompletedOrders.CollectionChanged -= HandleSelectedCompletedOrdersChanged;
					SettingsData.Settings.SettingChange -= HandleSettingChange;
				}
				m_model = value;
				if (m_model != null)
				{
					SettingsData.Settings.SettingChange += HandleSettingChange;
					m_model.SelectedCompletedOrders.CollectionChanged += HandleSelectedCompletedOrdersChanged;
				}

				// Handler
				void HandleSelectedCompletedOrdersChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					Chart.Scene.Invalidate();
				}
				void HandleSettingChange(object sender, SettingChangeEventArgs e)
				{
					//switch (e.Key)
					//{
					//case nameof(ChartSettings.ShowOpenOrders):
					//	PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowOpenOrders)));
					//	Chart.Scene.Invalidate();
					//	break;
					//case nameof(ChartSettings.ShowCompletedOrders):
					//	PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowCompletedOrders)));
					//	Chart.Scene.Invalidate();
					//	break;
					//case nameof(ChartSettings.ShowMarketDepth):
					//	PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowMarketDepth)));
					//	GfxMarketDepth.Invalidate();
					//	Chart.Scene.Invalidate();
					//	break;
					//case nameof(ChartSettings.XAxisLabelMode):
					//	Chart.XAxisPanel.Invalidate();
					//	break;
					//case nameof(ChartSettings.TradeLabelSize):
					//	Chart.Scene.Invalidate();
					//	break;
					//case nameof(ChartSettings.TradeLabelTransparency):
					//	Chart.Scene.Invalidate();
					//	break;
					//case nameof(ChartSettings.ShowTradeDescriptions):
					//	Chart.Scene.Invalidate();
					//	break;
					//}
				}
			}
		}
		private Model m_model;

		/// <summary>The source equity data</summary>
		public Equity Equity { get; }

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
					m_chart.Options.AntiAliasing = true;
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
				string HandleChartXAxisTickText(double x, double step)
				{
					if (SettingsData.Settings.Equity.XAxisLabelMode == EXAxisLabelMode.AxisIndex)
						return x.ToString();

					if (x < 0 || x > (Model.UtcNow - Misc.CryptoCurrencyEpoch).TotalDays + 365)
						return string.Empty;

					// Convert the x axis value to a date time.
					var dt_curr = Misc.CryptoCurrencyEpoch + TimeSpan.FromDays(x);
					if (x - step < 0)
						return Misc.ShortTimeString(dt_curr, dt_curr, true);

					var dt_prev = Misc.CryptoCurrencyEpoch + TimeSpan.FromDays(x - step);

					// Get the date time values in the correct time zone
					dt_curr = (SettingsData.Settings.Equity.XAxisLabelMode == EXAxisLabelMode.LocalTime)
						? dt_curr.LocalDateTime
						: dt_curr.UtcDateTime;
					dt_prev = (SettingsData.Settings.Chart.XAxisLabelMode == EXAxisLabelMode.LocalTime)
						? dt_prev.LocalDateTime
						: dt_prev.UtcDateTime;

					// First tick on the x axis
					var first_tick = x - step < Chart.XAxis.Min;

					// Show more of the time stamp depending on how it differs from the previous time stamp
					return Misc.ShortTimeString(dt_curr, dt_prev, first_tick);
				}
				void HandleAutoRanging(object sender, ChartControl.AutoRangeEventArgs e)
				{
					var bb = BBox.Reset;
					if (e.Axes.HasFlag(ChartControl.EAxis.XAxis) && e.Axes.HasFlag(ChartControl.EAxis.YAxis))
					{
						// Display the full equity history
						var utc_now = Model.UtcNow;
						var first = utc_now;
						foreach (var exch in Model.Exchanges)
						{
							foreach (var his in exch.History.Values)
								first = DateTimeOffset_.Min(first, his.Created);
							foreach (var xfr in exch.Transfers.Values)
								first = DateTimeOffset_.Min(first, xfr.Created);
						}
						if (first == utc_now)
							first = utc_now - TimeSpan.FromDays(365);

						var beg = (float)(first - Misc.CryptoCurrencyEpoch).TotalDays;
						var end = (float)(utc_now - Misc.CryptoCurrencyEpoch).TotalDays;
						var y0 = 0f;
						var y1 = (float)Model.NettWorth * 2f;
						bb = BBox.Encompass(bb, new v4(beg, y0, -ZOrder.Max, 1f));
						bb = BBox.Encompass(bb, new v4(end, y1, +ZOrder.Max, 1f));
					}
					else if (e.Axes.HasFlag(ChartControl.EAxis.YAxis))
					{
						throw new NotImplementedException();
						//var idx_min = (int)Chart.XAxis.Min;
						//var idx_max = (int)Chart.XAxis.Max;
						//foreach (var candle in Instrument.CandleRange(idx_min, idx_max))
						//{
						//	bb = BBox.Encompass(bb, new v4(idx_min, (float)candle.Low, -ZOrder.Max, 1f));
						//	bb = BBox.Encompass(bb, new v4(idx_max, (float)candle.High, +ZOrder.Max, 1f));
						//}
					}
					else if (e.Axes.HasFlag(ChartControl.EAxis.XAxis))
					{
						throw new NotImplementedException();
						//// Display the last few candles @ N pixels per candle
						//var width = (int)(Chart.Scene.ActualWidth / 6); // in candles
						//var idx_min = Instrument.Count - width * 4 / 5;
						//var idx_max = Instrument.Count + width * 1 / 5;
						//bb = BBox.Encompass(bb, new v4(idx_min, (float)Chart.YAxis.Min, -ZOrder.Max, 1f));
						//bb = BBox.Encompass(bb, new v4(idx_max, (float)Chart.YAxis.Max, +ZOrder.Max, 1f));
					}
					else
					{
						bb = BBox.Encompass(bb, new v4((float)Chart.XAxis.Min, (float)Chart.YAxis.Min, -ZOrder.Max, 1f));
						bb = BBox.Encompass(bb, new v4((float)Chart.XAxis.Max, (float)Chart.YAxis.Max, +ZOrder.Max, 1f));
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
					BuildScene(e.Window);
				}
				void HandleMoved(object sender, ChartControl.ChartMovedEventArgs e)
				{
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VisibleTimeSpan)));
				}
			}
		}
		private ChartControl m_chart;

		/// <summary>Chart data legend</summary>
		public ChartDataLegend Legend
		{
			get => m_legend;
			private set
			{
				if (m_legend == value) return;
				Util.Dispose(ref m_legend);
				m_legend = value;
			}

		}
		private ChartDataLegend m_legend;

		/// <summary>A string description of the period of time shown in the chart</summary>
		public string VisibleTimeSpan
		{
			get
			{
				var span = Chart.XAxis.Range.Size;
				return TimeSpan.FromDays(span).ToPrettyString();
			}
		}

		/// <summary>Add graphics and elements to the chart</summary>
		private void BuildScene(View3d.Window window)
		{
			window.RemoveObjects(new[] { CtxId }, 1, 0);
			if (!DockControl.IsVisible)
				return;

			GfxEquity.BuildScene(Chart, window, m_chart_overlay);
			GfxCompletedOrders.BuildScene(Model.SelectedCompletedOrders, Chart, m_chart_overlay);

			// Put the call out so others can draw on this chart
			//BuildScene?.Invoke(this, args);
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
		private GfxObjects.Orders GfxCompletedOrders
		{
			get { return m_gfx_completed_orders; }
			set
			{
				if (m_gfx_completed_orders == value) return;
				Util.Dispose(ref m_gfx_completed_orders);
				m_gfx_completed_orders = value;
			}
		}
		private GfxObjects.Orders m_gfx_completed_orders;

		/// <summary>Callbacks for positioning order graphics</summary>
		private double IOrderToXValue(IOrder order)
		{
			return (order.Created.Value - Misc.CryptoCurrencyEpoch).TotalDays;
		}
		private double IOrderToYValue(IOrder order)
		{
			return Equity.NettWorthHistory().FirstOrDefault(x => x.Time == order.Created).Worth;
		}

		/// <summary>Replace the chart context menus</summary>
		private void ModifyContextMenus()
		{
			// Modify the main chart area menu
			{
				var cmenu = Chart.Scene.ContextMenu;

				// Move the existing chart menu into a sub menu
				var chart_options_menu = new MenuItem { Header = "Chart Options" };
				{
					var items = cmenu.Items.Cast<MenuItem>().ToList();
					cmenu.Items.Clear();
					chart_options_menu.Items.AddRange(items);
				}

				cmenu.Items.Add(new Separator());

				// Add the chart options menu at the end
				cmenu.Items.Add(chart_options_menu);

				// Add this last so it occurs after the other handlers attached to 'Opened'
				cmenu.Opened += Gui_.TidySeparators;
			}

			// Modify the XAxis context menu
			{
				var cmenu = Chart.XAxis.ContextMenu;

				// Insert an option for changing the units of the XAxis
				var idx = 0;
				{
					var opt = cmenu.Items.Insert2(idx++, new MenuItem { Header = "Label Mode" });
					{
						var cb = opt.Items.Add2(new ComboBox
						{
							ItemsSource = Enum<EXAxisLabelMode>.ValuesArray,
							Style = FindResource(System.Windows.Controls.ToolBar.ComboBoxStyleKey) as Style,
							Background = SystemColors.ControlBrush,
							BorderThickness = new Thickness(0),
							Margin = new Thickness(1, 1, 20, 1),
							MinWidth = 80,
						});
						cb.SetBinding(ComboBox.SelectedItemProperty, new Binding(nameof(EquitySettings.XAxisLabelMode)) { Source = SettingsData.Settings.Equity });
						cb.SelectionChanged += (s, a) =>
						{
							SettingsData.Settings.Equity.XAxisLabelMode = (EXAxisLabelMode)cb.SelectedItem;
						};
					}
				}
			}
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;

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
