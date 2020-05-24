using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Maths;
using Rylogic.Utility;

namespace TestWPF
{
	/// <summary>
	/// Interaction logic for ChartUI.xaml
	/// </summary>
	public partial class ChartUI : Window
	{
		private ChartDataSeries m_series;
		private ChartDataLegend m_legend;
		private View3d.Object m_obj0;

		public ChartUI()
		{
			InitializeComponent();

			m_chart.Title = "My Chart Title";
			m_chart.XAxis.Label = "My X Axis";
			m_chart.YAxis.Label = "My Y Axis";
			//m_chart.XAxis.Options.Side = Dock.Top;
			//m_chart.YAxis.Options.Side = Dock.Right;

			m_chart.Options.Orthographic = true;

			m_obj0 = new View3d.Object(
				"test_object", 0xFFFFFFFF, 5, 18, 1,
				new View3d.Vertex[]
				{
					new View3d.Vertex(new v4(+0, +1, +0, 1), new v4(+0.00f, +1.00f, +0.00f, 0), 0xffff0000, new v2(0.50f, 1)),
					new View3d.Vertex(new v4(-1, -1, -1, 1), new v4(-0.57f, -0.57f, -0.57f, 0), 0xff00ff00, new v2(0.00f, 0)),
					new View3d.Vertex(new v4(-1, -1, +1, 1), new v4(-0.57f, -0.57f, +0.57f, 0), 0xff0000ff, new v2(0.25f, 0)),
					new View3d.Vertex(new v4(+1, -1, +1, 1), new v4(+0.57f, -0.57f, +0.57f, 0), 0xffff00ff, new v2(0.50f, 0)),
					new View3d.Vertex(new v4(+1, -1, -1, 1), new v4(+0.57f, -0.57f, -0.57f, 0), 0xff00ffff, new v2(0.75f, 0)),
				},
				new ushort[]
				{
					0, 1, 2,
					0, 2, 3,
					0, 3, 4,
					0, 4, 1,
					4, 3, 2,
					2, 1, 4,
				},
				new View3d.Nugget[]
				{
					new View3d.Nugget(View3d.ETopo.TriList, View3d.EGeom.Vert|View3d.EGeom.Norm|View3d.EGeom.Colr),
				},
				null);

			m_chart.BuildScene += (s, a) =>
			{
				m_chart.Scene.Window.AddObject(m_obj0);
			};

			m_series = new ChartDataSeries("waves", ChartDataSeries.EFormat.XRealYReal);
			using (var lk = m_series.Lock())
			{
				for (int i = 0; i != 100000; ++i)
					lk.Add(new ChartDataSeries.Pt(0.01 * i, Math.Sin(0.01 * i * Math_.Tau)));
			}
			m_series.Options.Colour = Colour32.Blue;
			m_series.Options.PlotType = ChartDataSeries.EPlotType.Bar;
			m_series.Options.PointStyle = EPointStyle.Triangle;
			m_series.Options.PointSize = 50f;
			m_series.Options.LineWidth = 3f;
			m_series.Chart = m_chart;

			m_legend = new ChartDataLegend();
			m_chart.Elements.Add(m_legend);
		}
		protected override void OnClosed(EventArgs e)
		{
			Util.Dispose(ref m_legend);
			Util.Dispose(ref m_series);
			Util.Dispose(ref m_obj0);
			Gui_.DisposeChildren(this, EventArgs.Empty);
			base.OnClosed(e);
		}
	}
}
