using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WinForms;
using Rylogic.Maths;
using Rylogic.Utility;
using Util = Rylogic.Utility.Util;

namespace CoinFlip
{
	public class IndicatorVertLine :Indicator
	{
		public IndicatorVertLine(SettingsData settings = null)
			:base(Guid.NewGuid(), "VertLine", settings ?? new SettingsData())
		{}
		public IndicatorVertLine(XElement node)
			:base(node)
		{}
		protected override void Dispose(bool disposing)
		{
			Gfx = null;
			base.Dispose(disposing);
		}

		/// <summary>Settings for this indicator</summary>
		public SettingsData Settings
		{
			[DebuggerStepThrough] get { return (SettingsData)SettingsInternal; }
		}

		/// <summary>The fractional candle index that the line is at</summary>
		public double FIndex
		{
			get { return Settings.FIndex; }
			set
			{
				if (FIndex == value) return;
				Settings.FIndex = value;
				Invalidate();
			}
		}

		/// <summary>The graphics for the indicator</summary>
		public View3d.Object Gfx
		{
			[DebuggerStepThrough] get { return m_gfx; }
			set
			{
				if (m_gfx == value) return;
				Util.Dispose(ref m_gfx);
				m_gfx = value;
			}
		}
		private View3d.Object m_gfx;

		/// <summary>Invalidate if the chart moves beyond the range of the graphics</summary>
		protected override void HandleChartMoved(object sender, ChartControl.ChartMovedEventArgs e)
		{
			base.HandleChartMoved(sender, e);

			if (Bit.AnySet(e.MoveType, ChartControl.EMoveType.XZoomed|ChartControl.EMoveType.YZoomed))
				Invalidate();
		}

		/// <summary>Update the graphics model for this indicator</summary>
		protected override void UpdateGfxCore()
		{
			var line_colour   = (Selected ? Settings.Colour      .Lerp(Color.Gray, 0.5f) : Settings.Colour      ).ToArgbU();
			var region_colour = (Selected ? Settings.RegionColour.Lerp(Color.Gray, 0.5f) : Settings.RegionColour).ToArgbU();
			var region_size   = Settings.RegionSize;
			var height        = (float)Chart.YAxis.Span;

			// Create graphics for the vertical line
			m_vbuf.Resize(2 + (region_size != 0 ? 4 : 0));
			m_ibuf.Resize(2 + (region_size != 0 ? 6 : 0));
			m_nbuf.Resize(1 + (region_size != 0 ? 1 : 0));

			m_vbuf[0] = new View3d.Vertex(new v4(0, 0, 0, 1), line_colour);
			m_vbuf[1] = new View3d.Vertex(new v4(0, height, 0, 1), line_colour);
			m_ibuf[0] = 0;
			m_ibuf[1] = 1;
			m_nbuf[0] = new View3d.Nugget(View3d.EPrim.LineList, View3d.EGeom.Vert|View3d.EGeom.Colr, 0, 2, 0, 2);

			if (region_size != 0)
			{
				var half_width = (float)(region_size / (2.0 * Chart.XAxis.Span));
				m_vbuf[2] = new View3d.Vertex(new v4(-half_width, 0     , 0, 1), region_colour);
				m_vbuf[3] = new View3d.Vertex(new v4(-half_width, height, 0, 1), region_colour);
				m_vbuf[4] = new View3d.Vertex(new v4(+half_width, 0     , 0, 1), region_colour);
				m_vbuf[5] = new View3d.Vertex(new v4(+half_width, height, 0, 1), region_colour);
				m_ibuf[2] = 2;
				m_ibuf[3] = 3;
				m_ibuf[4] = 5;
				m_ibuf[5] = 5;
				m_ibuf[6] = 4;
				m_ibuf[7] = 2;
				m_nbuf[1] = new View3d.Nugget(View3d.EPrim.TriList, View3d.EGeom.Vert|View3d.EGeom.Colr, 2, 6, 2, 8, !Bit.AllSet(region_colour, 0xFF000000));
			}

			// Create the graphics
			Gfx = new View3d.Object(Name, 0xFFFFFFFF, m_vbuf.Count, m_ibuf.Count, m_nbuf.Count, m_vbuf.ToArray(), m_ibuf.ToArray(), m_nbuf.ToArray(), Id);

			base.UpdateGfxCore();
		}

		/// <summary>Update the graphics for this indicator and add it to the scene</summary>
		protected override void UpdateSceneCore(View3d.Window window)
		{
			base.UpdateSceneCore(window);
			if (Gfx == null) return;

			// Add to the scene
			if (Visible)
			{
				// Graphics are created at the origin, position at YAxis.Min
				Gfx.O2P = m4x4.Translation((float)FIndex, (float)Chart.YAxis.Min, ZOrder.Indicators);
				window.AddObject(Gfx);
			}
			else
			{
				window.RemoveObject(Gfx);
			}
		}

		/// <summary>Perform a hit test on this object. Returns null for no hit. 'point' is in client space because typically hit testing uses pixel tolerances</summary>
		public override ChartControl.HitTestResult.Hit HitTest(PointF chart_point, Point client_point, Keys modifier_keys, View3d.Camera cam)
		{
			// Find the nearest point to 'client_point' on the line
			var chart_pt  = chart_point;
			var client_pt = Chart.ChartToClient(new PointF((float)FIndex, chart_pt.Y));

			// If clicked within 'tolerance' of the line
			if (Math.Abs(client_pt.X - client_point.X) < Chart.Options.MinSelectionDistance)
				return new ChartControl.HitTestResult.Hit(this, new PointF((float)FIndex, chart_pt.Y), null);

			return null;
		}

		/// <summary>Allow this indicator to be dragged</summary>
		public override bool Dragable { get { return true; } }
		public override ChartControl.MouseOp CreateDragMouseOp(ChartControl.HitTestResult hit)
		{
			return new DragLine(this);
		}

		/// <summary>A mouse operation for dragging the horizontal line around</summary>
		public class DragLine :ChartControl.MouseOp
		{
			private readonly IndicatorVertLine m_line;
			public DragLine(IndicatorVertLine line)
				:base(line.Chart)
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
				var chart_pt = m_chart.ClientToChart(e.Location);
				m_line.Settings.FIndex = chart_pt.X;
				m_chart.Invalidate();
				m_chart.Update();
			}
		}

		#region Settings
		[TypeConverter(typeof(TyConv))]
		public class SettingsData :SettingsXml<SettingsData>
		{
			public SettingsData()
			{
				FIndex       = 0f;
				Width        = 1.0;
				Colour       = Color.DarkBlue;
				RegionColour = Color.LightBlue.Alpha(0.5f);
				RegionSize   = 0;
			}
			public SettingsData(XElement node)
				:base(node)
			{}

			/// <summary>The X value of this line</summary>
			public double FIndex
			{
				get { return get<double>(nameof(FIndex)); }
				set { set(nameof(FIndex), value); }
			}

			/// <summary>The width of the line</summary>
			public double Width
			{
				get { return get<double>(nameof(Width)); }
				set { set(nameof(Width), value); }
			}

			/// <summary>Colour of the line</summary>
			public Color Colour
			{
				get { return get<Color>(nameof(Colour)); }
				set { set(nameof(Colour), value); }
			}

			/// <summary>Colour of the surrounding region</summary>
			public Color RegionColour
			{
				get { return get<Color>(nameof(RegionColour)); }
				set { set(nameof(RegionColour), value); }
			}

			/// <summary>The size of the region area</summary>
			public double RegionSize
			{
				get { return get<double>(nameof(RegionSize)); }
				set { set(nameof(RegionSize), value); }
			}

			private class TyConv :GenericTypeConverter<SettingsData> {}
		}
		#endregion
	}
}
