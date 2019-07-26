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
			GfxEquity = new GfxObjects.Equity();
			Legend = new ChartDataLegend { Chart = Chart, BackColour = 0xFFE0E0E0 };
			Model = model;

			DataContext = this;
		}
		public void Dispose()
		{
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
					SettingsData.Settings.SettingChange -= HandleSettingChange;
					m_model.DataChanging -= HandleDataChanging;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.DataChanging += HandleDataChanging;
					SettingsData.Settings.SettingChange += HandleSettingChange;
				}
				GfxEquity?.Update(Model);

				// Handler
				void HandleDataChanging(object sender, DataChangingEventArgs e)
				{
					GfxEquity.Update(Model);
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
				}

				// Handlers
				string HandleChartXAxisTickText(double x, double step)
				{
					// Convert the x axis value to a date time.
					var dt_curr = Misc.CryptoCurrencyEpoch + TimeSpan.FromDays(x);
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
						var first_trade = new DateTimeOffset(Model.Exchanges.Min(x => x.HistoryInterval.Beg), TimeSpan.Zero);
						var beg = (float)(first_trade - Misc.CryptoCurrencyEpoch).TotalDays;
						var end = (float)(Model.UtcNow - Misc.CryptoCurrencyEpoch).TotalDays;
						var y0 = 0f;
						var y1 = (float)Model.NettWorth * 1.2f;
						bb = BBox.Encompass(bb, new v4(beg, y0, -ZOrder.Max, 1f));
						bb = BBox.Encompass(bb, new v4(end, y1, -ZOrder.Max, 1f));
					}
					else if (e.Axes.HasFlag(ChartControl.EAxis.YAxis))
					{
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
		public ChartDataLegend Legend { get; }

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

			// Equity
			GfxEquity.BuildScene(Chart, window, m_chart_overlay);
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
