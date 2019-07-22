using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	public partial class EquityChart :Grid, IDockable, IDisposable
	{
		public EquityChart(Model model)
		{
			InitializeComponent();
			DockControl = new DockControl(this, "Equity");
			Chart = m_chart_control;
			Model = model;
		}
		public void Dispose()
		{
			//GfxOpenOrders = null;
			//GfxCompletedOrders = null;
			//GfxUpdatingText = null;
			//GfxCandles = null;
			//ChartSelector = null;
			//Instrument = null;
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
				}
				m_model = value;
				if (m_model != null)
				{
					SettingsData.Settings.SettingChange += HandleSettingChange;
				}

				// Handler
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
					//m_chart.ChartMoved -= HandleMoved;
					//m_chart.BuildScene -= HandleBuildScene;
					//m_chart.MouseDown -= HandleMouseDown;
					//m_chart.AutoRanging -= HandleAutoRanging;
					m_chart.XAxis.TickText = m_chart.XAxis.DefaultTickText;
					m_chart.YAxis.TickText = m_chart.YAxis.DefaultTickText;
					Util.Dispose(ref m_chart);
				}
				m_chart = value;
				if (m_chart != null)
				{
					// Customise the chart for candles
					m_chart.Options.AntiAliasing = false;
					m_chart.Options.Orthographic = true;
					m_chart.Options.SelectionColour = new Colour32(0x8092A1B1);
					//m_chart.Options.CrossHairZOffset = ZOrder.Cursors;
					m_chart.XAxis.Options.PixelsPerTick = 50.0;
					m_chart.XAxis.Options.TickTextTemplate = "XX:XX\r\nXXX XX XXXX";
					m_chart.YAxis.Options.TickTextTemplate = "X.XXXX";
					//m_chart.XAxis.TickText = HandleChartXAxisTickText;
					//m_chart.YAxis.TickText = HandleChartYAxisTickText;
					//m_chart.YAxis.Options.Side = Dock.Right;
					//m_chart.AutoRanging += HandleAutoRanging;
					//m_chart.MouseDown += HandleMouseDown;
					//m_chart.BuildScene += HandleBuildScene;
					//m_chart.ChartMoved += HandleMoved;
				}
			}
		}
		private ChartControl m_chart;
	}
}
