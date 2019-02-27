using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Rylogic.Common;
using Rylogic.Gfx;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class ChartControl
	{
		/// <summary>The 2D size of the chart</summary>
		public class RangeData : IDisposable
		{
			private readonly ChartControl m_chart;
			public RangeData(ChartControl chart)
			{
				m_chart = chart;
				XAxis = new Axis(EAxis.XAxis, m_chart);
				YAxis = new Axis(EAxis.YAxis, m_chart);
			}
			public RangeData(RangeData rhs)
			{
				XAxis = new Axis(rhs.XAxis);
				YAxis = new Axis(rhs.YAxis);
			}
			public virtual void Dispose()
			{
				XAxis = null;
				YAxis = null;
			}

			/// <summary>The chart X axis</summary>
			public Axis XAxis
			{
				[DebuggerStepThrough]
				get { return m_xaxis; }
				internal set
				{
					if (m_xaxis == value) return;
					if (m_xaxis != null)
					{
						m_xaxis.Scroll -= HandleAxisScrolled;
						m_xaxis.Zoomed -= HandleAxisZoomed;
						Util.Dispose(ref m_xaxis);
					}
					m_xaxis = value;
					if (m_xaxis != null)
					{
						m_xaxis.Zoomed += HandleAxisZoomed;
						m_xaxis.Scroll += HandleAxisScrolled;
					}
				}
			}
			private Axis m_xaxis;

			/// <summary>The chart Y axis</summary>
			public Axis YAxis
			{
				[DebuggerStepThrough]
				get { return m_yaxis; }
				internal set
				{
					if (m_yaxis == value) return;
					if (m_yaxis != null)
					{
						m_yaxis.Scroll -= HandleAxisScrolled;
						m_yaxis.Zoomed -= HandleAxisZoomed;
						Util.Dispose(ref m_yaxis);
					}
					m_yaxis = value;
					if (m_yaxis != null)
					{
						m_yaxis.Zoomed += HandleAxisZoomed;
						m_yaxis.Scroll += HandleAxisScrolled;
					}
				}
			}
			private Axis m_yaxis;

			/// <summary>Add the graphics associated with the axes to the scene</summary>
			internal void AddToScene(View3d.Window window)
			{
				var cam = m_chart.Scene.Camera;
				var wh = cam.ViewArea(m_chart.Scene.Camera.FocusDist);

				// Position the grid lines so that they line up with the axis tick marks
				// Grid lines are modelled from the bottom left corner
				if (XAxis.GridLineGfx != null)
				{
					if (m_chart.Options.ShowGridLines && XAxis.Options.ShowGridLines)
					{
						XAxis.GridLines(out var min, out var max, out var step);

						var o2w = cam.O2W;
						o2w.pos = cam.FocusPoint - o2w * new v4((float)(wh.x / 2 - min), wh.y / 2, m_chart.Options.GridZOffset, 0);

						XAxis.GridLineGfx.O2WSet(o2w);
						window.AddObject(XAxis.GridLineGfx);
					}
					else
					{
						window.RemoveObject(XAxis.GridLineGfx);
					}
				}
				if (YAxis.GridLineGfx != null)
				{
					if (m_chart.Options.ShowGridLines && YAxis.Options.ShowGridLines)
					{
						YAxis.GridLines(out var min, out var max, out var step);

						var o2w = cam.O2W;
						o2w.pos = cam.FocusPoint - o2w * new v4(wh.x / 2, (float)(wh.y / 2 - min), m_chart.Options.GridZOffset, 0);

						YAxis.GridLineGfx.O2WSet(o2w);
						window.AddObject(YAxis.GridLineGfx);
					}
					else
					{
						window.RemoveObject(YAxis.GridLineGfx);
					}
				}
			}

			/// <summary>Notify of the axis zooming</summary>
			private void HandleAxisZoomed(object sender, EventArgs e)
			{
				// Invalidate the cached grid line graphics on zoom (for both axes), since the model will need to change size
				XAxis.GridLineGfx = null;
				YAxis.GridLineGfx = null;

				// If a moved event is pending, ensure zoomed is added to the args
				if (m_moved_args == null)
				{
					m_moved_args = new ChartMovedEventArgs(EMoveType.None);
					m_chart.Dispatcher.BeginInvoke(NotifyMoved);
				}
				if (sender == XAxis) m_moved_args.MoveType |= EMoveType.XZoomed;
				if (sender == YAxis) m_moved_args.MoveType |= EMoveType.YZoomed;
			}

			/// <summary>Notify of the axis scrolled</summary>
			private void HandleAxisScrolled(object sender, EventArgs e)
			{
				if (m_moved_args == null)
				{
					m_moved_args = new ChartMovedEventArgs(EMoveType.None);
					m_chart.Dispatcher.BeginInvoke(NotifyMoved);
				}
				if (sender == XAxis) m_moved_args.MoveType |= EMoveType.XScrolled;
				if (sender == YAxis) m_moved_args.MoveType |= EMoveType.YScrolled;
			}

			/// <summary>Raise the ChartMoved event on the chart</summary>
			private void NotifyMoved()
			{
				m_chart.OnChartMoved(m_moved_args);
				m_moved_args = null;
			}
			private ChartMovedEventArgs m_moved_args;

			/// <summary>A axis on the chart (typically X or Y)</summary>
			public class Axis : IDisposable
			{
				private readonly ChartControl m_chart;
				public Axis(EAxis axis, ChartControl chart)
					: this(axis, chart, 0f, 1f)
				{ }
				public Axis(EAxis axis, ChartControl chart, double min, double max)
				{
					Debug.Assert(axis == EAxis.XAxis || axis == EAxis.YAxis);
					Debug.Assert(chart != null);
					m_chart = chart;
					Set(min, max);
					AxisType = axis;
					Label = string.Empty;
					AllowScroll = true;
					AllowZoom = true;
					LockRange = false;
					TickText = DefaultTickText;
				}
				public Axis(Axis rhs)
				{
					m_chart = rhs.m_chart;
					Set(rhs.Min, rhs.Max);
					AxisType = rhs.AxisType;
					Label = rhs.Label;
					AllowScroll = rhs.AllowScroll;
					AllowZoom = rhs.AllowZoom;
					LockRange = rhs.LockRange;
					TickText = rhs.TickText;
				}
				public void Dispose()
				{
					m_disposed = true;
					GridLineGfx = null;
				}
				private bool m_disposed;

				/// <summary>Render options for the axis</summary>
				public OptionsData.Axis Options =>
					AxisType == EAxis.XAxis ? m_chart.Options.XAxis :
					AxisType == EAxis.YAxis ? m_chart.Options.YAxis :
					throw new Exception($"Unknown axis type: {AxisType}");

				/// <summary>Which axis this is</summary>
				public EAxis AxisType { get; }

				/// <summary>The axis label</summary>
				public string Label
				{
					[DebuggerStepThrough]
					get { return m_label ?? "X Axis"; }// string.Empty; }
					set
					{
						if (m_label == value) return;
						m_label = value;
						m_chart.NotifyPropertyChanged(nameof(Label));
					}
				}
				private string m_label;

				/// <summary>The minimum axis value</summary>
				public double Min
				{
					[DebuggerStepThrough]
					get { return m_min; }
					set
					{
						if (m_min == value) return;
						Set(value, Max);
					}
				}
				private double m_min;

				/// <summary>The maximum axis value</summary>
				public double Max
				{
					[DebuggerStepThrough]
					get { return m_max; }
					set
					{
						if (m_max == value) return;
						Set(Min, value);
					}
				}
				private double m_max;

				/// <summary>The total range of this axis (max - min)</summary>
				public double Span
				{
					[DebuggerStepThrough]
					get { return Max - Min; }
					set
					{
						if (Span == value) return;
						if (value <= 0) throw new Exception($"Invalid axis span: {value}");
						Set(Centre - 0.5 * value, Centre + 0.5 * value);
					}
				}

				/// <summary>The centre value of the range</summary>
				public double Centre
				{
					[DebuggerStepThrough]
					get { return (Min + Max) * 0.5; }
					set
					{
						if (Centre == value) return;
						Set(m_min + value - Centre, m_max + value - Centre);
					}
				}

				/// <summary>The min/max limits as a range</summary>
				public RangeF Range
				{
					[DebuggerStepThrough]
					get { return new RangeF(Min, Max); }
					set
					{
						if (Equals(Range, value)) return;
						Set(value.Beg, value.End);
					}
				}

				/// <summary>Allow scrolling on this axis</summary>
				public bool AllowScroll
				{
					get { return m_allow_scroll && !LockRange; }
					set
					{
						if (m_allow_scroll == value) return;
						m_allow_scroll = value;
					}
				}
				private bool m_allow_scroll;

				/// <summary>Allow zooming on this axis</summary>
				public bool AllowZoom
				{
					get { return m_allow_zoom && !LockRange; }
					set
					{
						if (m_allow_zoom == value) return;
						m_allow_zoom = value;
					}
				}
				private bool m_allow_zoom;

				/// <summary>Get/Set whether the range can be changed by user input</summary>
				public bool LockRange { get; set; }

				/// <summary>Convert the axis value to a string. "string TickText(double tick_value, double step_size)" </summary>
				public Func<double, double, string> TickText;

				/// <summary>Set the range without risk of an assert if 'min' is greater than 'Max' or visa versa</summary>
				public void Set(double min, double max)
				{
					Debug.Assert(min < max, "Range must be positive and non-zero");
					var zoomed = !Math_.FEql(max - min, m_max - m_min);
					var scroll = !Math_.FEql((max + min) * 0.5, (m_max + m_min) * 0.5);

					m_min = min;
					m_max = max;

					if (zoomed) OnZoomed();
					if (scroll) OnScroll();
				}
				public void Set(RangeF range)
				{
					Set(range.Beg, range.End);
				}

				/// <summary>Raised whenever the range scales</summary>
				public event EventHandler Zoomed;
				protected virtual void OnZoomed()
				{
					Zoomed?.Invoke(this, EventArgs.Empty);
				}

				/// <summary>Raised whenever the range shifts</summary>
				public event EventHandler Scroll;
				protected virtual void OnScroll()
				{
					Scroll?.Invoke(this, EventArgs.Empty);
				}

				/// <summary>Scroll the axis by 'delta'</summary>
				public void Shift(double delta)
				{
					if (!AllowScroll) return;
					Centre += delta;
				}

				/// <summary>Return the position of the grid lines for this axis</summary>
				public void GridLines(out double min, out double max, out double step)
				{
					var axis_length =
						AxisType == EAxis.XAxis ? m_chart.Scene.ActualWidth :
						AxisType == EAxis.YAxis ? m_chart.Scene.ActualHeight : 0.0;
					var max_ticks = axis_length / Options.PixelsPerTick;

					// Choose step sizes
					var span = Span;
					double step_base = Math.Pow(10.0, (int)Math.Log10(Span)); step = step_base;
					foreach (var s in new[] { 0.05f, 0.1f, 0.2f, 0.25f, 0.5f, 1.0f, 2.0f, 5.0f, 10.0f, 20.0f, 50.0f })
					{
						if (s * span > max_ticks * step_base) continue;
						step = step_base / s;
					}

					min = (Min - Math.IEEERemainder(Min, step)) - Min;
					max = Span * 1.0001;
					if (min < 0.0) min += step;

					// Protect against increments smaller than can be represented
					if (min + step == min)
						step = (max - min) * 0.01f;

					// Protect against too many ticks along the axis
					if (max - min > step * 100)
						step = (max - min) * 0.01f;
				}
				public IEnumerable<double> EnumGridLines
				{
					get
					{
						GridLines(out var min, out var max, out var step);
						for (var x = min; x < max; x += step)
							yield return x;
					}
				}

				/// <summary>The graphics object used for grid lines</summary>
				internal View3d.Object GridLineGfx
				{
					get
					{
						if (m_disposed) throw new Exception();
						return m_gridlines ?? (m_gridlines = CreateGridLineGfx());
					}
					set
					{
						if (m_gridlines == value) return;
						Util.Dispose(ref m_gridlines);
					}
				}
				private View3d.Object m_gridlines;
				private View3d.Object CreateGridLineGfx()
				{
					// Create a model for the grid lines
					// Need to allow for one step in either direction because we only create the
					// grid lines model when scaling and we can translate by a max of one step in
					// either direction.
					GridLines(out var min, out var max, out var step);
					var num_lines = (int)(2 + (max - min) / step);

					// Create the grid lines at the origin, they get positioned as the camera moves
					var verts = new View3d.Vertex[num_lines * 2];
					var indices = new ushort[num_lines * 2];
					var nuggets = new View3d.Nugget[1];
					var name = string.Empty;
					var v = 0;
					var i = 0;

					// Grid verts
					if (AxisType == EAxis.XAxis)
					{
						name = "xaxis_grid";
						var x = 0f; var y0 = 0f; var y1 = (float)m_chart.YAxis.Span;
						for (int l = 0; l != num_lines; ++l)
						{
							verts[v++] = new View3d.Vertex(new v4(x, y0, 0f, 1f), Options.GridColour.ARGB);
							verts[v++] = new View3d.Vertex(new v4(x, y1, 0f, 1f), Options.GridColour.ARGB);
							x += (float)step;
						}
					}
					if (AxisType == EAxis.YAxis)
					{
						name = "yaxis_grid";
						var y = 0f; var x0 = 0f; var x1 = (float)m_chart.XAxis.Span;
						for (int l = 0; l != num_lines; ++l)
						{
							verts[v++] = new View3d.Vertex(new v4(x0, y, 0f, 1f), Options.GridColour.ARGB);
							verts[v++] = new View3d.Vertex(new v4(x1, y, 0f, 1f), Options.GridColour.ARGB);
							y += (float)step;
						}
					}

					// Grid indices
					for (int l = 0; l != num_lines; ++l)
					{
						indices[i] = (ushort)i++;
						indices[i] = (ushort)i++;
					}

					// Grid nugget
					nuggets[0] = new View3d.Nugget(View3d.EPrim.LineList, View3d.EGeom.Vert | View3d.EGeom.Colr);
					var gridlines = new View3d.Object(name, 0xFFFFFFFF, verts.Length, indices.Length, nuggets.Length, verts, indices, nuggets, ChartTools.Id);
					gridlines.FlagsSet(View3d.EFlags.SceneBoundsExclude | View3d.EFlags.NoZWrite, true);
					return gridlines;
				}

				/// <summary>Show the axis context menu</summary>
				public void ShowContextMenu(System.Windows.Point location, ChartControl.HitTestResult hit_result)
				{
					//var cmenu = new ContextMenuStrip();
					//using (cmenu.SuspendLayout(true))
					//{
					//	{// Lock Range
					//		var opt = cmenu.Items.Add2(new ToolStripMenuItem("Lock") { Checked = LockRange, CheckOnClick = true });
					//		opt.CheckedChanged += (s, a) =>
					//		{
					//			LockRange = opt.Checked;
					//		};
					//	}
					//
					//	// Customise the menu
					//	var type = AxisType == EAxis.XAxis ? AddUserMenuOptionsEventArgs.EType.XAxis : AddUserMenuOptionsEventArgs.EType.YAxis;
					//	Owner.OnAddUserMenuOptions(new AddUserMenuOptionsEventArgs(type, cmenu, hit_result));
					//}
					//cmenu.Items.TidySeparators();
					//if (cmenu.Items.Count != 0)
					//	cmenu.Show(Owner, location);
				}

				/// <summary>Default value to text conversion</summary>
				public string DefaultTickText(double x, double step)
				{
					// This solves the rounding problem for values near zero when the axis span could be anything
					return !Math_.FEql(x / Span, 0.0) ? Math_.RoundSF(x, 5).ToString("G8") : "0";
				}

				///// <summary>Default tick text measurement</summary>
				//public SizeF DefaultMeasureTickText(Graphics gfx)
				//{
				//	// Can't use 'GridLines' here because creates an infinite recursion.
				//	// Using TickText(Min/Max, 0.0) causes the axes to jump around.
				//	// The best option is to use one fixed length string.
				//	return gfx.MeasureString("-9.99999", Options.TickFont);
				//}

				/// <summary>Friendly string view</summary>
				public override string ToString()
				{
					return $"{Label} - [{Min}:{Max}]";
				}

				#region Equals
				public bool Equals(Axis rhs)
				{
					// Value equal if the axes represent the same range
					// but not necessarily the same options.
					return
						rhs != null &&
						AxisType == rhs.AxisType &&
						Label == rhs.Label &&
						Min == rhs.Min &&
						Max == rhs.Max;
				}
				public override bool Equals(object obj)
				{
					return Equals(obj as Axis);
				}
				public override int GetHashCode()
				{
					return new { AxisType, Label, Min, Max, Options }.GetHashCode();
				}
				#endregion
			}
		}
	}
}