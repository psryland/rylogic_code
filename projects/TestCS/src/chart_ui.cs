using System;
using System.Windows.Forms;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WinForms;
using Rylogic.Maths;
using Util = Rylogic.Utility.Util;

namespace TestCS
{
	public class ChartUI :Form
	{
		private ChartControl m_chart;
		private ChartDataSeries m_series;
		private ChartDataLegend m_legend;
		private View3d.Object m_obj0;

		public ChartUI()
		{
			InitializeComponent();
			m_chart = Controls.Add2(new ChartControl { Dock = DockStyle.Fill, Title = "My Chart" });
			m_chart.Options.Orthographic = true;
			m_chart.XAxis.Label = "X Axis";
			m_chart.YAxis.Label = "Y Axis";

			m_obj0 = new View3d.Object(
				"test_object", 0xFFFFFFFF, 5, 18, 1,
				new View3d.Vertex[]
				{
					new View3d.Vertex(new v4(+0, +1, +0, 1), new v4(+0.00f, +1.00f, +0.00f, 0), 0xffff0000, new v2(0.50f, 1)),
					new View3d.Vertex(new v4(-1, -1, -1, 1), new v4(-0.57f, -0.57f, -0.57f, 0), 0xff00ff00, new v2(0.00f, 0)),
					new View3d.Vertex(new v4(+1, -1, -1, 1), new v4(+0.57f, -0.57f, -0.57f, 0), 0xff0000ff, new v2(0.25f, 0)),
					new View3d.Vertex(new v4(+1, -1, +1, 1), new v4(+0.57f, -0.57f, +0.57f, 0), 0xffff00ff, new v2(0.50f, 0)),
					new View3d.Vertex(new v4(-1, -1, +1, 1), new v4(-0.57f, -0.57f, +0.57f, 0), 0xff00ffff, new v2(0.75f, 0)),
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
					new View3d.Nugget(View3d.EPrim.TriList, View3d.EGeom.Vert|View3d.EGeom.Norm|View3d.EGeom.Colr),
				}, null);
			m_chart.ChartRendering += (s,a) =>
			{
				a.AddToScene(m_obj0);
			};

			m_series = new ChartDataSeries("waves", ChartDataSeries.EFormat.XRealYReal);
			using (var lk = m_series.Lock())
			{
				for (int i = 0; i != 100000; ++i)
					lk.Add(new ChartDataSeries.Pt(0.01*i, Math.Sin(0.01*i*Math_.Tau)));
			}
			m_series.Options.Colour = Colour32.Blue;
			m_series.Options.PlotType = ChartDataSeries.EPlotType.Bar;
			m_series.Options.PointStyle = ChartDataSeries.EPointStyle.Triangle;
			m_series.Options.PointSize = 50f;
			m_series.Options.LineWidth = 3f;
			m_series.Chart = m_chart;

			m_legend = new ChartDataLegend();
			m_chart.Elements.Add(m_legend);
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref m_series);
			Util.Dispose(ref m_obj0);
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.SuspendLayout();
			// 
			// ChartUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(456, 482);
			this.Name = "ChartUI";
			this.Text = "ChartUI";
			this.ResumeLayout(false);

		}
		#endregion
	}
}
