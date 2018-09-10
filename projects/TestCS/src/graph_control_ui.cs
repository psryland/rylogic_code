using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Rylogic.Extn;
using Rylogic.Gui.WinForms;
using Rylogic.Maths;
using Util = Rylogic.Utility.Util;

namespace TestCS
{
	public class GraphControlUI :Form
	{
		private GraphControl m_graph;

		public GraphControlUI()
		{
			InitializeComponent();

			m_graph.SetLabels("Test Graph", "X Axis", "Y Axis");
			m_graph.BorderStyle = BorderStyle.FixedSingle;

			Add("Sine Wave", 1000
				,i =>
					{
						var x = i * Math_.Tau / 1000.0;
						var y = Math.Sin(x);
						return new { X = x, Y = y, ErrLo = 0, ErrHi = 0 };
					}
				,new GraphControl.Series.RdrOptions
					{
						PlotType = GraphControl.Series.RdrOptions.EPlotType.Line,
						LineWidth = 2f,
						LineColour = Color.Blue,
						PointSize = 0,
					});

			Add("Cosine Wave", 1000
				,i =>
					{
						var x = i * Math_.Tau / 1000.0;
						var y = Math.Cos(x);
						return new { X = x, Y = y, ErrLo = 0, ErrHi = 0 };
					}
				,new GraphControl.Series.RdrOptions
					{
						PlotType = GraphControl.Series.RdrOptions.EPlotType.Line,
						LineWidth = 2f,
						LineColour = Color.Red,
						PointSize = 0,
					});

			Add("Points", 1000
				,i =>
					{
						var x = i * Math_.Tau / 1000.0;
						var y = Math.Cos(x) * Math.Sin(x);
						var errlo = Math.Abs(0.1 * Math.Cos(x - Math_.TauBy16));
						var errhi = Math.Abs(0.1 * Math.Cos(x + Math_.TauBy16));
						return new { X = x, Y = y, ErrLo = -errlo, ErrHi = +errhi };
					}
				,new GraphControl.Series.RdrOptions
					{
						PlotType = GraphControl.Series.RdrOptions.EPlotType.Point,
						PointColour = Color.Green,
						PointSize = 2f,
					});

			Add("Bars", 10
				,i =>
					{
						var x = i + 10;
						var y = i;// * Math.Sin(i * Math_.Tau / 1000.0);
						return new { X = x, Y = y, ErrLo = 0, ErrHi = 0 };
					}
				,new GraphControl.Series.RdrOptions
					{
						PlotType = GraphControl.Series.RdrOptions.EPlotType.Bar,
						PointSize = 0,
						BarColour = Color.SteelBlue,
					});

			m_graph.FindDefaultRange();
			m_graph.ResetToDefaultRange();
			m_graph.AddOverlayOnPaint += OverlayOnPaint;
			m_graph.AddOverlayOnRender += OverlayOnRender;
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>Draw graphics that float over the graph</summary>
		private void OverlayOnPaint(object sender, GraphControl.OverlaysEventArgs args)
		{
			// Updated on every paint
			var pt = args.G2C.TransformPoint(new PointF(5,5), 1);
			args.Gfx.DrawString("Point (5,5)", SystemFonts.DefaultFont, Brushes.DarkGreen, pt);
		}

		/// <summary>Embed graphics into the graph</summary>
		private void OverlayOnRender(object sender, GraphControl.OverlaysEventArgs args)
		{
			// A 20 pixel radius circle at 5,5 on the graph
			// Only updated when the graph is rendered
			var pt = args.G2C.TransformPoint(new PointF(5,5), 1);
			args.Gfx.DrawEllipse(Pens.DarkGreen, pt.X - 20, pt.Y - 20, 40, 40);
		}

		/// <summary>Add a function as a series</summary>
		private void Add(string title, int points, Func<int,dynamic> Func, GraphControl.Series.RdrOptions opts)
		{
			m_graph.Data.Add(new GraphControl.Series(title, points, opts, int_.Range(points).Select(i =>
			{
				var d = Func(i);
				return new GraphControl.GraphValue(d.X,d.Y,d.ErrLo,d.ErrHi,null);
			})));
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GraphControlUI));
			this.m_graph = new Rylogic.Gui.WinForms.GraphControl();
			this.SuspendLayout();
			//
			// m_graph
			//
			this.m_graph.Centre = ((System.Drawing.PointF)(resources.GetObject("m_graph.Centre")));
			this.m_graph.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_graph.Location = new System.Drawing.Point(0, 0);
			this.m_graph.Name = "m_graph";
			this.m_graph.Size = new System.Drawing.Size(552, 566);
			this.m_graph.TabIndex = 0;
			this.m_graph.Title = "";
			this.m_graph.Zoom = 1F;
			this.m_graph.ZoomMax = 3.402823E+38F;
			this.m_graph.ZoomMin = 1.401298E-45F;
			//
			// GraphControl
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(552, 566);
			this.Controls.Add(this.m_graph);
			this.Name = "GraphControl";
			this.Text = "GraphControl";
			this.ResumeLayout(false);
		}
		#endregion
	}
}
