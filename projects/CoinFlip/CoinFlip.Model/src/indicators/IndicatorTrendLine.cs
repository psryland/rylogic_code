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

		/// <summary>Update the graphics model for this indicator</summary>
		protected override void UpdateGfxCore()
		{
			var line_colour = Selected ? Settings.Colour.Lerp(Colour32.Gray, 0.5f) : Settings.Colour;

			var ldr = Str.Build(
				$"*Line {Name} {line_colour}\n",
				$"{{\n",
				$" 0 0 0  1 0 0\n",
				$" *Point grab {{ 0 0 0  1 0 0  *Width {{{20}}} *Hidden }}\n",
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
				var tf = Instrument.TimeFrame;
				var extrap = Settings.Extrapolate;
				var bx = Misc.TicksToTimeFrame(Settings.BegX, tf);
				var ex = Misc.TicksToTimeFrame(Settings.EndX, tf);
				var by = Settings.BegY;
				var ey = Settings.EndY;
				var x = new v4((float)(ex - bx), (float)(ey - by), 1f, 0f);
				var y = v4.Cross3(v4.ZAxis, x);
				var z = v4.ZAxis;
				var p = new v4((float)bx, (float)by, ZOrder.Indicators, 1f);

				// Create the transform
				Gfx.O2P = new m4x4(x, y, z, p);
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
			// todo, hit test and include the grab location
			return null;
		}

		/// <summary>Allow this indicator to be dragged</summary>
		public override bool Dragable { get { return true; } }
		public override ChartControl.MouseOp CreateDragMouseOp(ChartControl.HitTestResult hit)
		{
			return new DragLine(this, hit);
		}

		/// <summary>Grab locations on the line</summary>
		private enum EGrab { Start, Line, End }

		/// <summary>A mouse operation for dragging the horizontal line around</summary>
		public class DragLine :ChartControl.MouseOp
		{
			private readonly IndicatorTrendLine m_line;
			private EGrab m_grab;

			public DragLine(IndicatorTrendLine line, ChartControl.HitTestResult hit)
				:base(line.Chart)
			{
				m_line = line;
				m_line.Selected = true;

				m_grab = EGrab.Line; //(EGrab)hit.Hits[0].Context
			}
			public override void Dispose()
			{
				m_line.Selected = false;
				base.Dispose();
			}
			public override void MouseDown(MouseEventArgs e)
			{
				base.MouseDown(e);
			}
			public override void MouseMove(MouseEventArgs e)
			{
				var chart_pt = m_chart.ClientToChart(e.Location);
				switch (m_grab)
				{
				case EGrab.Start:
					{
						m_line.Settings.BegX = (long)Misc.TimeFrameToTicks(chart_pt.X, m_line.Instrument.TimeFrame);
						m_line.Settings.BegY = (decimal)chart_pt.Y;
						break;
					}
				case EGrab.End:
					{
						m_line.Settings.EndX = (long)Misc.TimeFrameToTicks(chart_pt.X, m_line.Instrument.TimeFrame);
						m_line.Settings.EndY = (decimal)chart_pt.Y;
						break;
					}
				case EGrab.Line:
					{
				//		m_line.Settings.BegX = (long)Misc.TimeFrameToTicks(chart_pt.X, m_line.Instrument.TimeFrame);
				//		m_line.Settings.BegY = (decimal)chart_pt.Y;
				//		m_line.Settings.EndX = (long)Misc.TimeFrameToTicks(chart_pt.X, m_line.Instrument.TimeFrame);
				//		m_line.Settings.EndY = (decimal)chart_pt.Y;
						break;
					}
				}
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
