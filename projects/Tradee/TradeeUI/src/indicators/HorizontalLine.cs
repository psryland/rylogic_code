using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.common;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.maths;
using pr.util;

namespace Tradee
{
	/// <summary>Base for line indicators</summary>
	public class HorizontalLine :IndicatorBase
	{
		public HorizontalLine(string name, double price, double width_in_pips)
			:base(Guid.NewGuid(), name, new HorizontalLineSettings { Price = price, RegionHeight = width_in_pips })
		{}
		public HorizontalLine(XElement node)
			:base(node)
		{}
		protected override void Dispose(bool disposing)
		{
			Gfx = null;
			base.Dispose(disposing);
		}
		protected override void SetChartCore(ChartControl chart)
		{
			// Invalidate the graphics whenever the x axis moves
			if (Chart != null)
			{
				Chart.XAxis.Zoomed -= Invalidate;
			}
			base.SetChartCore(chart);
			if (Chart != null)
			{
				Chart.XAxis.Zoomed += Invalidate;
			}
		}
		protected override void UpdateGfxCore()
		{
			var line_colour   = (Selected ? Settings.Colour      .Lerp(Color.Gray, 0.5f) : Settings.Colour      ).ToArgbU();
			var region_colour = (Selected ? Settings.RegionColour.Lerp(Color.Gray, 0.5f) : Settings.RegionColour).ToArgbU();
			var region_height = Settings.RegionHeight;
			var width         = (float)Chart.XAxis.Span;

			// Create graphics for the horizontal line
			m_vbuf.Resize(2 + (region_height != 0 ? 4 : 0));
			m_ibuf.Resize(2 + (region_height != 0 ? 6 : 0));
			m_nbuf.Resize(1 + (region_height != 0 ? 1 : 0));

			m_vbuf[0] = new View3d.Vertex(new v4(0, 0, 0, 1), line_colour);
			m_vbuf[1] = new View3d.Vertex(new v4(width, 0, 0, 1), line_colour);
			m_ibuf[0] = 0;
			m_ibuf[1] = 1;
			m_nbuf[0] = new View3d.Nugget(View3d.EPrim.LineList, View3d.EGeom.Vert|View3d.EGeom.Colr, 0, 2, 0, 2);

			if (region_height != 0)
			{
				var half_height = (float)(Instrument.PriceData.PipSize * region_height / 2.0);
				m_vbuf[2] = new View3d.Vertex(new v4(0    , -half_height, 0, 1), region_colour);
				m_vbuf[3] = new View3d.Vertex(new v4(width, -half_height, 0, 1), region_colour);
				m_vbuf[4] = new View3d.Vertex(new v4(0    , +half_height, 0, 1), region_colour);
				m_vbuf[5] = new View3d.Vertex(new v4(width, +half_height, 0, 1), region_colour);
				m_ibuf[2] = 2;
				m_ibuf[3] = 3;
				m_ibuf[4] = 5;
				m_ibuf[5] = 5;
				m_ibuf[6] = 4;
				m_ibuf[7] = 2;
				m_nbuf[1] = new View3d.Nugget(View3d.EPrim.TriList, View3d.EGeom.Vert|View3d.EGeom.Colr, 2, 6, 2, 8, !Bit.AllSet(region_colour, 0xFF000000));
			}

			// Create the graphics
			Gfx = new View3d.Object(Name, 0xFFFFFFFF, m_vbuf.Count, m_ibuf.Count, m_nbuf.Count, m_vbuf.ToArray(), m_ibuf.ToArray(), m_nbuf.ToArray());

			base.UpdateGfxCore();
		}
		protected override void AddToSceneCore(View3d.Window window)
		{
			if (Gfx != null)
			{
				// Graphics are created at the origin, position at XAxis.Max
				Gfx.O2P = m4x4.Translation((float)Chart.XAxis.Min, (float)Settings.Price, ChartUI.Z.SnR);
				window.AddObject(Gfx);
			}
		}

		/// <summary>Settings for this indicator</summary>
		public HorizontalLineSettings Settings
		{
			[DebuggerStepThrough] get { return (HorizontalLineSettings)IndicatorSettingsInternal; }
		}

		/// <summary>The Ask price line</summary>
		public View3d.Object Gfx
		{
			[DebuggerStepThrough] get { return m_impl_gfx; }
			set
			{
				if (m_impl_gfx == value) return;
				Util.Dispose(ref m_impl_gfx);
				m_impl_gfx = value;
			}
		}
		private View3d.Object m_impl_gfx;

		/// <summary>Perform a hit test on this object. Returns null for no hit. 'point' is in client space because typically hit testing uses pixel tolerances</summary>
		public override ChartControl.HitTestResult.Hit HitTest(Point client_point, Keys modifier_keys, View3d.CameraControls cam)
		{
			// Find the nearest point to 'client_point' on the line
			var chart_pt  = Chart.ClientToChart(client_point);
			var client_pt = Chart.ChartToClient(new PointF(chart_pt.X, (float)Settings.Price));

			// If clicked within 'tolerance' of the line
			const int tolerance = 3;
			if (Math.Abs(client_pt.Y - client_point.Y) < tolerance)
				return new ChartControl.HitTestResult.Hit(this, new PointF(chart_pt.X, (float)Settings.Price), null);

			return null;
		}

		/// <summary>Allow this indicator to be dragged</summary>
		public override bool Dragable { get { return true; } }
		public override ChartControl.MouseOp CreateDragMouseOp()
		{
			return new DragLine(this);
		}

		/// <summary>A mouse operation for dragging the horizontal line around</summary>
		public class DragLine :ChartControl.MouseOp
		{
			private readonly HorizontalLine m_line;
			public DragLine(HorizontalLine line) : base(line.Chart)
			{
				m_line = line;
				m_line.Selected = true;
			}
			public override void Dispose()
			{
				m_line.Selected = false;
				base.Dispose();
			}
			public override void MouseMove(MouseEventArgs e)
			{
				var chart_pt = m_line.Chart.ClientToChart(e.Location);
				m_line.Settings.Price = chart_pt.Y;
				m_line.InvalidateChart();
				m_chart.Update();
			}
		}
	}

	#region Settings

	/// <summary>Settings for Support and Resistance</summary>
	[TypeConverter(typeof(TyConv))]
	public class HorizontalLineSettings :SettingsXml<HorizontalLineSettings>
	{
		public HorizontalLineSettings()
		{
			Price        = 0;
			Colour       = Color.LightBlue;
			RegionColour = Color.LightBlue.Alpha(0.5f);
			RegionHeight = 0;
		}
		public HorizontalLineSettings(XElement node)
			:base(node)
		{}

		/// <summary>The Y value of this line</summary>
		public double Price
		{
			get { return get(x => x.Price); }
			set { set(x => x.Price, value); }
		}

		/// <summary>Colour of the line</summary>
		public Color Colour
		{
			get { return get(x => x.Colour); }
			set { set(x => x.Colour, value); }
		}

		/// <summary>Colour of the surrounding region</summary>
		public Color RegionColour
		{
			get { return get(x => x.RegionColour); }
			set { set(x => x.RegionColour, value); }
		}

		/// <summary>The height (in pips) of the region area</summary>
		public double RegionHeight
		{
			get { return get(x => x.RegionHeight); }
			set { set(x => x.RegionHeight, value); }
		}

		private class TyConv :GenericTypeConverter<HorizontalLineSettings> {}
	}

	#endregion
}
