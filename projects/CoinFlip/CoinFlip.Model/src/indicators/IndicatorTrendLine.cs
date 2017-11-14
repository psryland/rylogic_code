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

namespace CoinFlip
{
	public class IndicatorTrendLine :Indicator
	{
		// todo:
		// support dragging the start, end, or whole line
		// finish the EditTrendLineUI 
		// Add a glow region to the line
		// Extrapolate

		public IndicatorTrendLine(SettingsData settings = null)
			:base(Guid.NewGuid(), "TrendLine", settings ?? new SettingsData())
		{}
		public IndicatorTrendLine(XElement node)
			:base(node)
		{}
		protected override void Dispose(bool disposing)
		{
			Gfx = null;
			base.Dispose(disposing);
		}
		protected override void SetChartCore(ChartControl chart)
		{
			if (Chart != null)
			{
				Chart.MouseDown -= HandleMouseDown;
			}
			base.SetChartCore(chart);
			if (Chart != null)
			{
				Chart.MouseDown += HandleMouseDown;
			}

			// Handlers
			void HandleMouseDown(object sender, MouseEventArgs args)
			{
				if (Hovered && args.Button == MouseButtons.Left)
				{
					var t = ClosestClientPoint(args.Location, out var dist);
					var grab = t == 0f ? EGrab.Beg : t == 1f ? EGrab.End : EGrab.Line;
					Chart.MouseOperations.SetPending(MouseButtons.Left, new DragLine(this, grab));
				}
			}
		}

		/// <summary>Settings for this indicator</summary>
		public SettingsData Settings
		{
			[DebuggerStepThrough] get { return (SettingsData)SettingsInternal; }
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

		/// <summary>The start point of the trend line in chart space</summary>
		public v2 ChartBeg
		{
			get
			{
				// Convert the trend line start/end points to screen space
				var bx = (float)Instrument.FIndexAt(new TimeFrameTime(Settings.BegX, Instrument.TimeFrame));
				var by = (float)Settings.BegY;
				return new v2(bx, by);
			}
		}

		/// <summary>The end point of the trend line in chart space</summary>
		public v2 ChartEnd
		{
			get
			{
				// Convert the trend line start/end points to screen space
				var ex = (float)Instrument.FIndexAt(new TimeFrameTime(Settings.EndX, Instrument.TimeFrame));
				var ey = (float)Settings.EndY;
				return new v2(ex, ey);
			}
		}

		/// <summary>Update the graphics model for this indicator</summary>
		protected override void UpdateGfxCore()
		{
			var line_colour = Settings.Colour;
			var ldr = Str.Build(
				$"*Group {Name} ",
				$"{{",
				$"  *Line line {line_colour} {{0 0 0  1 0 0}}\n",
				$"  *Line halo {line_colour.Alpha(0.25f)} {{0 0 0  1 0 0 *Width {{{16}}} }}\n",
				$"  *Point grab {line_colour} {{ 0 0 0  1 0 0  *Width {{{20}}} *Style{{Circle}} }}\n",
				$"}}\n");

			Gfx = new View3d.Object(ldr, false, Id);
		}

		/// <summary>Update the graphics for this indicator and add it to the scene</summary>
		protected override void UpdateSceneCore(View3d.Window window)
		{
			base.UpdateSceneCore(window);
			if (Gfx == null)
				return;

			// Add to the scene
			if (Visible)
			{
				Gfx.Child("halo").Visible = Hovered || m_dragging;
				Gfx.Child("grab").Visible = Hovered || m_dragging;

				var tf = Instrument.TimeFrame;
				var b = ChartBeg;
				var e = ChartEnd;
				var d = e - b;
				if (Settings.Extrapolate)
				{
					// X bound, else Y bound
					var scale = d.x * Chart.YAxis.Span > d.y * Chart.XAxis.Span
						? (float)(Chart.XAxis.Span / Math.Abs(d.x))  // X-bound
						: (float)(Chart.YAxis.Span / Math.Abs(d.y)); // Y-bound

					Gfx.Child("line").O2P = m4x4.Scale(scale, 1f, 1f, v4.Origin);
				}
	
				// Create the transform
				Gfx.O2P = new m4x4(
					(!Maths.FEql(d.Length2Sq,0) ? m3x4.Rotation(v4.XAxis, new v4(d,0,0)) : m3x4.Identity) *
					m3x4.Scale(d.Length2,1,1),
					new v4(b, ZOrder.Indicators, 1f));

				window.AddObject(Gfx);
			}
			else
			{
				window.RemoveObject(Gfx);
			}
		}

		/// <summary>Hit test this indicator</summary>
		public override ChartControl.HitTestResult.Hit HitTest(PointF chart_point, Point client_point, Keys modifier_keys, View3d.Camera cam)
		{
			var t = ClosestClientPoint(client_point, out var dist);
			if (dist > Chart.Options.MinSelectionDistance)
				return null;

			if (t == 0f) return new ChartControl.HitTestResult.Hit(this, ChartBeg, null);
			if (t == 1f) return new ChartControl.HitTestResult.Hit(this, ChartEnd, null);
			return new ChartControl.HitTestResult.Hit(this, (1-t)*ChartBeg + t*ChartEnd, null);
		}

		/// <summary>Test a screen space point against this line</summary>
		private float ClosestClientPoint(PointF client_point, out float dist)
		{
			dist = float.MaxValue;
			if (Instrument == null || Instrument.Count == 0)
				return 0f;

			// Convert the trend line start/end points to screen space
			var b_chart = ChartBeg;
			var e_chart = ChartEnd;

			// Screen space beg, end, line, point
			var b = new v2(Chart.ChartToClient(b_chart.ToPointF()));
			var e = new v2(Chart.ChartToClient(e_chart.ToPointF()));
			var p = new v2(client_point);

			// Prioritise the end points over the line
			dist = (e - p).Length2;
			if (dist < Chart.Options.MinSelectionDistance)
				return 1f;

			dist = (b - p).Length2;
			if (dist < Chart.Options.MinSelectionDistance)
				return 0f;

			// Find the closest point, on the line 'b->e' to 'p'.
			var t = Geometry.ClosestPoint(b, e, p);
			t = Maths.Clamp(t, 0f, Settings.Extrapolate ? t : 1f);
			dist = (b + t*(e - b) - p).Length2;
			return t;
		}

		/// <summary>Allow this indicator to be dragged</summary>
		public override bool Dragable { get { return true; } }
		public override ChartControl.MouseOp CreateDragMouseOp(ChartControl.HitTestResult hit)
		{
			var t = ClosestClientPoint(hit.ClientPoint, out var dist);
			var grab = t == 0f ? EGrab.Beg : t == 1f ? EGrab.End : EGrab.Line;
			return new DragLine(this, grab);
		}
		private bool m_dragging;

		/// <summary>A mouse operation for dragging the horizontal line around</summary>
		public class DragLine :ChartControl.MouseOp
		{
			private readonly IndicatorTrendLine m_line;
			private v2 m_beg, m_end;
			private EGrab m_grab;

			public DragLine(IndicatorTrendLine line, EGrab grab)
				:base(line.Chart)
			{
				m_line = line;
				m_line.Selected = true;
				m_beg = line.ChartBeg;
				m_end = line.ChartEnd;
				m_grab = grab;
				m_line.m_dragging = true;
			}
			public override void Dispose()
			{
				m_line.m_dragging = false;
				m_line.Selected = false;
				base.Dispose();
			}
			public override void MouseMove(MouseEventArgs e)
			{
				var chart_pt = new v2(m_chart.ClientToChart(e.Location));
				var chart_grab = new v2(m_grab_chart);
				switch (m_grab)
				{
				case EGrab.Beg:
					{
						m_line.Settings.BegX = m_line.Instrument.TimeAtFIndex(chart_pt.x);
						m_line.Settings.BegY = (decimal)chart_pt.y;
						break;
					}
				case EGrab.End:
					{
						m_line.Settings.EndX = m_line.Instrument.TimeAtFIndex(chart_pt.x);
						m_line.Settings.EndY = (decimal)chart_pt.y;
						break;
					}
				case EGrab.Line:
					{
						var ofs = chart_pt - m_grab_chart;
						m_line.Settings.BegX = m_line.Instrument.TimeAtFIndex(m_beg.x + ofs.x);
						m_line.Settings.BegY = (decimal)(m_beg.y + ofs.y);
						m_line.Settings.EndX = m_line.Instrument.TimeAtFIndex(m_end.x + ofs.x);
						m_line.Settings.EndY = (decimal)(m_end.y + ofs.y);
						break;
					}
				}
				m_chart.Invalidate();
				m_chart.Update();
			}
		}

		/// <summary>Grab locations on the line</summary>
		public enum EGrab { Beg, Line, End }

		#region Settings
		[TypeConverter(typeof(TyConv))]
		public class SettingsData :SettingsXml<SettingsData>
		{
			public SettingsData()
			{
				Width  = 1.0;
				Colour = Colour32.Blue.Lerp(Colour32.Black, 0.5f);
				BegX   = 0;
				BegY   = 0m;
				EndX   = 0;
				EndY   = 0m;
				Extrapolate = true;
			}
			public SettingsData(XElement node)
				:base(node)
			{}

			/// <summary>The width of the line</summary>
			public double Width
			{
				get { return get<double>(nameof(Width)); }
				set { set(nameof(Width), value); }
			}

			/// <summary>Colour of the line</summary>
			public Colour32 Colour
			{
				get { return get<Colour32>(nameof(Colour)); }
				set { set(nameof(Colour), value); }
			}

			/// <summary>The X-position of the start of the trend line (in ticks)</summary>
			public long BegX
			{
				get { return get<long>(nameof(BegX)); }
				set { set(nameof(BegX), value); }
			}

			/// <summary>The Y-position of the start of the trend line (in PriceQ2B)</summary>
			public decimal BegY
			{
				get { return get<decimal>(nameof(BegY)); }
				set { set(nameof(BegY), value); }
			}

			/// <summary>The X-Position of the end of the trend line (in ticks)</summary>
			public long EndX
			{
				get { return get<long>(nameof(EndX)); }
				set { set(nameof(EndX), value); }
			}

			/// <summary>The Y-Position of the end of the trend line (in PriceQ2B)</summary>
			public decimal EndY
			{
				get { return get<decimal>(nameof(EndY)); }
				set { set(nameof(EndY), value); }
			}

			/// <summary>Extrapolate the line beyond the end point</summary>
			public bool Extrapolate
			{
				get { return get<bool>(nameof(Extrapolate)); }
				set { set(nameof(Extrapolate), value); }
			}

			private class TyConv :GenericTypeConverter<SettingsData> {}
		}
		#endregion
	}
}
