using System;
using System.Drawing;
using System.Windows.Forms;
using pr.gui;
using pr.maths;

namespace TestCS
{
	public class GraphControlUI :Form
	{
		private GraphControl m_graph;

		public GraphControlUI()
		{
			InitializeComponent();

			m_graph.SetLabels("Test Graph", "X Axis", "Y Axis");
/*			Add("Sine Wave", 1000
				,i =>
					{
						var x = i * Maths.Tau / 1000.0;
						var y = Math.Sin(x);
						return new { X = x, Y = y, ErrLo = y, ErrHi = y };
					}
				,new GraphControl.Series.RdrOpts
					{
						m_line_width = 2f,
						m_line_colour = Color.Blue,
					});

			Add("Cosine Wave", 1000
				,i =>
					{
						var x = i * Maths.Tau / 1000.0;
						var y = Math.Cos(x);
						return new { X = x, Y = y, ErrLo = y, ErrHi = y };
					}
				,new GraphControl.Series.RdrOpts
					{
						m_line_width = 2f,
						m_line_colour = Color.Red,
					});

			Add("Thing", 1000
				,i =>
					{
						var x = i * Maths.Tau / 1000.0;
						var y = Math.Cos(x) * Math.Sin(x);
						var errlo = Math.Abs(0.1 * Math.Cos(x - Maths.TauBy16));
						var errhi = Math.Abs(0.1 * Math.Cos(x + Maths.TauBy16));
						return new { X = x, Y = y, ErrLo = y - errlo, ErrHi = y + errhi };
					}
				,new GraphControl.Series.RdrOpts
					{
						m_line_width = 2f,
						m_line_colour = Color.Green,
					});

			m_graph.FindDefaultRange();
			m_graph.ResetToDefaultRange();*/
			m_graph.AddOverlaysOnPaint += DrawOverlays;
		}

		private void DrawOverlays(GraphControl sender,Graphics gfx,float scale_x,float scale_y)
		{
			gfx.DrawEllipse(Pens.Red,0.25f*scale_x,0.25f*scale_y,0.5f*scale_x,0.5f*scale_y);
		}

		/// <summary>Add a function as a series</summary>
		private void Add(string title, int points, Func<int,dynamic> Func, GraphControl.Series.RdrOpts opts)
		{
			var series = new GraphControl.Series(title, points, opts);
			for (var i = 0; i != 1000; ++i)
			{
				var d = Func(i);
				series.Values.Add(new GraphControl.GraphValue(d.X,d.Y,d.ErrLo,d.ErrHi,null));
			}
			m_graph.Data.Add(series);
		}
		#region Windows Form Designer generated code

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GraphControlUI));
			this.m_graph = new pr.gui.GraphControl();
			((System.ComponentModel.ISupportInitialize)(this.m_graph)).BeginInit();
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
			((System.ComponentModel.ISupportInitialize)(this.m_graph)).EndInit();
			this.ResumeLayout(false);
		}

		#endregion
	}
}
