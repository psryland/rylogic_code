using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class ChartControl
	{
		/// <summary>The 2D size of the chart</summary>
		public sealed class RangeData : IDisposable
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
				m_chart = rhs.m_chart;
				XAxis = new Axis(rhs.XAxis);
				YAxis = new Axis(rhs.YAxis);
			}
			public void Dispose()
			{
				XAxis = null!;
				YAxis = null!;
			}

			/// <summary>The chart X axis</summary>
			public Axis XAxis
			{
				get => m_xaxis;
				internal set
				{
					if (m_xaxis == value) return;
					if (m_xaxis != null)
					{
						m_xaxis.Scroll -= HandleAxisScrolled;
						m_xaxis.Zoomed -= HandleAxisZoomed;
						Util.Dispose(ref m_xaxis!);
					}
					m_xaxis = value;
					if (m_xaxis != null)
					{
						m_xaxis.Zoomed += HandleAxisZoomed;
						m_xaxis.Scroll += HandleAxisScrolled;
					}
				}
			}
			private Axis m_xaxis = null!;

			/// <summary>The chart Y axis</summary>
			public Axis YAxis
			{
				get => m_yaxis;
				internal set
				{
					if (m_yaxis == value) return;
					if (m_yaxis != null)
					{
						m_yaxis.Scroll -= HandleAxisScrolled;
						m_yaxis.Zoomed -= HandleAxisZoomed;
						Util.Dispose(ref m_yaxis!);
					}
					m_yaxis = value;
					if (m_yaxis != null)
					{
						m_yaxis.Zoomed += HandleAxisZoomed;
						m_yaxis.Scroll += HandleAxisScrolled;
					}
				}
			}
			private Axis m_yaxis = null!;

			/// <summary>The aspect ratio of the axes</summary>
			public double Aspect
			{
				get { return XAxis.Span / YAxis.Span; }
				set
				{
					if (Aspect == value) return;
					if (XAxis.Span > YAxis.Span)
						YAxis.Span = XAxis.Span / value;
					else
						XAxis.Span = YAxis.Span * value;
				}
			}

			/// <summary>Add the graphics associated with the axes to the scene</summary>
			internal void UpdateScene(View3d.Window window)
			{
				// Position the grid lines so that they line up with the axis tick marks
				// Grid lines are modelled from the bottom left corner
				if (XAxis.Options.ShowGridLines)
					XAxis.AddToScene(window);
				else if (XAxis.GridLineGfx != null)
					window.RemoveObject(XAxis.GridLineGfx);

				if (YAxis.Options.ShowGridLines)
					YAxis.AddToScene(window);
				else if (YAxis.GridLineGfx != null)
					window.RemoveObject(YAxis.GridLineGfx);
			}

			/// <summary>Notify of the axis zooming</summary>
			private void HandleAxisZoomed(object? sender, EventArgs e)
			{
				// Invalidate the cached grid line graphics on zoom for both axes, since the
				// model will need to change size even if only zoomed on one axis.
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
			private void HandleAxisScrolled(object? sender, EventArgs e)
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
				if (m_moved_args == null) return;
				m_chart.OnChartMoved(m_moved_args);
				m_moved_args = null;
			}
			private ChartMovedEventArgs? m_moved_args;

			/// <summary>A axis on the chart (typically X or Y)</summary>
			[DebuggerDisplay("{AxisType} [{Min} , {Max}]")]
			public sealed class Axis : IDisposable
			{
				public delegate string TickTextCB(double x, double? step = null);

				public Axis(EAxis axis, ChartControl chart)
					: this(axis, chart, 0f, 1f)
				{ }
				public Axis(EAxis axis, ChartControl chart, double min, double max)
				{
					Debug.Assert(axis == EAxis.XAxis || axis == EAxis.YAxis);
					Chart = chart;
					RangeLimits = new RangeF(-1e10, +1e10);
					Set(min, max);
					AxisType = axis;
					Label = string.Empty;
					AllowScroll = true;
					AllowZoom = true;
					TickText = DefaultTickText;
				}
				public Axis(Axis rhs)
				{
					Chart = rhs.Chart;
					Set(rhs.Min, rhs.Max);
					AxisType = rhs.AxisType;
					Label = rhs.Label;
					AllowScroll = rhs.AllowScroll;
					AllowZoom = rhs.AllowZoom;
					TickText = rhs.TickText;
				}
				public void Dispose()
				{
					LinkTo = null;
					GridLineGfx = null;
				}

				/// <summary>Render options for the axis</summary>
				public OptionsData.Axis Options =>
					AxisType == EAxis.XAxis ? Chart.Options.XAxis :
					AxisType == EAxis.YAxis ? Chart.Options.YAxis :
					throw new Exception($"Unknown axis type: {AxisType}");

				/// <summary>The chart that owns this axis</summary>
				public ChartControl Chart { get; }

				/// <summary>Which axis this is</summary>
				public EAxis AxisType { get; }

				/// <summary>The axis label</summary>
				public string Label
				{
					get => m_label ?? "X Axis";
					set
					{
						if (m_label == value) return;
						m_label = value;
						Chart.NotifyPropertyChanged(nameof(Label));
					}
				}
				private string? m_label;

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

				/// <summary>The min/max value that the axis can scroll to</summary>
				public RangeF RangeLimits
				{
					get { return m_range_limits; }
					set
					{
						if (m_range_limits == value) return;
						m_range_limits = value;
						Set(value.Beg, value.End);
					}
				}
				private RangeF m_range_limits;

				/// <summary>Allow scrolling on this axis</summary>
				public bool AllowScroll { get; set; }

				/// <summary>Allow zooming on this axis</summary>
				public bool AllowZoom { get; set; }

				/// <summary>The context menu associated with this axis</summary>
				public ContextMenu ContextMenu =>
					AxisType == EAxis.XAxis ? Chart.m_xaxis_panel.ContextMenu :
					AxisType == EAxis.YAxis ? Chart.m_yaxis_panel.ContextMenu :
					throw new Exception("Unknown axis type");

				/// <summary>Convert the axis value to a string. "string TickText(double tick_value, double step_size)" </summary>
				public TickTextCB TickText;

				/// <summary>Set the range without risk of an assert if 'min' is greater than 'Max' or visa versa</summary>
				public void Set(double min, double max)
				{
					if (m_in_set != 0) return;
					using (Scope.Create(() => ++m_in_set, () => --m_in_set))
					{
						Debug.Assert(min < max, "Range must be positive and non-zero");
						var zoomed = !Math_.FEql(max - min, m_max - m_min);
						var scroll = !Math_.FEql((max + min) * 0.5, (m_max + m_min) * 0.5);

						m_min = Math_.Clamp(min, RangeLimits.Beg, RangeLimits.End);
						m_max = Math_.Clamp(max, RangeLimits.Beg, RangeLimits.End);
						if (m_max - m_min < Math_.TinyD) m_max = m_min + Math_.TinyD;

						if (zoomed || scroll) OnMoved();
						if (zoomed) OnZoomed();
						if (scroll) OnScroll();
					}
				}
				public void Set(RangeF range)
				{
					Set(range.Beg, range.End);
				}
				private int m_in_set;

				/// <summary>Raised whenever the range scales</summary>
				public event EventHandler? Moved;
				private void OnMoved()
				{
					Moved?.Invoke(this, EventArgs.Empty);
				}

				/// <summary>Raised whenever the range scales</summary>
				public event EventHandler? Zoomed;
				private void OnZoomed()
				{
					Zoomed?.Invoke(this, EventArgs.Empty);
				}

				/// <summary>Raised whenever the range shifts</summary>
				public event EventHandler? Scroll;
				private void OnScroll()
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
						AxisType == EAxis.XAxis ? Chart.Scene.ActualWidth :
						AxisType == EAxis.YAxis ? Chart.Scene.ActualHeight : 0.0;

					var max_ticks = axis_length / Options.PixelsPerTick;

					// Choose a step size that is a 'nice' size and that maximises the number of steps up to 'max_ticks'
					// 'span_mag' is the order of magnitude of 'Span', e.g if Span is 7560, span_mag is 1000, so 'Span/span_mag'
					// will be some values between [1, 10). 's' are common step sizes. Choose the step size that maximised the
					// number of steps up to the limit 'max_ticks'.
					double span_mag = Math.Pow(10.0, (int)Math.Log10(Span));
					var step_sizes = new[] { 0.01, 0.02, 0.025, 0.05, 0.1, 0.2, 0.25, 0.5, 1.0, 2.0, 2.5, 5.0, 10.0, 20.0, 50.0 };

					// Aim for a step size that is 'span_mag * s' and for which 'Span / step' is just less than 'max_ticks'
					step = span_mag * 100.0;
					for (int i = step_sizes.Length; i-- != 0;)
					{
						// if 'Span / s' is still less than 'max_ticks' try the next step size
						var s = span_mag * step_sizes[i];
						if (Span > max_ticks * s) break;
						step = s;
					}

					min = (Min - Math.IEEERemainder(Min, step)) - Min;
					max = Span * 1.0001;
					if (min < 0.0) min += step;

					// Protect against increments smaller than can be represented
					if (min + step == min)
						step = (max - min) * 0.01;

					// Protect against too many ticks along the axis
					if (max - min > step * 100)
						step = (max - min) * 0.01;
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
				internal View3d.Object? GridLineGfx
				{
					get => m_gridlines;
					set
					{
						if (m_gridlines == value) return;
						Util.Dispose(ref m_gridlines);
						m_gridlines = value;
					}
				}
				private View3d.Object? m_gridlines;
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

					// Choose a suitable grid colour
					var colour = Chart.Scene.BackgroundColour.Lerp(Chart.Scene.BackgroundColour.InvertBW(), 0.15);

					// Grid verts
					if (AxisType == EAxis.XAxis)
					{
						name = "xaxis_grid";
						var x = 0f; var y0 = 0f; var y1 = (float)Chart.YAxis.Span;
						for (int l = 0; l != num_lines; ++l)
						{
							verts[v++] = new View3d.Vertex(new v4(x, y0, 0f, 1f), colour);
							verts[v++] = new View3d.Vertex(new v4(x, y1, 0f, 1f), colour);
							x += (float)step;
						}
					}
					if (AxisType == EAxis.YAxis)
					{
						name = "yaxis_grid";
						var y = 0f; var x0 = 0f; var x1 = (float)Chart.XAxis.Span;
						for (int l = 0; l != num_lines; ++l)
						{
							verts[v++] = new View3d.Vertex(new v4(x0, y, 0f, 1f), colour);
							verts[v++] = new View3d.Vertex(new v4(x1, y, 0f, 1f), colour);
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
					var gridlines = new View3d.Object(name, 0xFFFFFFFF, verts.Length, indices.Length, nuggets.Length, verts, indices, nuggets, CtxId);
					gridlines.FlagsSet(View3d.EFlags.SceneBoundsExclude | View3d.EFlags.NoZWrite, true);
					return gridlines;
				}

				/// <summary>Add graphics for this axis to the scene</summary>
				internal void AddToScene(View3d.Window window)
				{
					GridLineGfx ??= CreateGridLineGfx();

					var cam = window.Camera;
					var wh = cam.ViewArea(cam.FocusDist);
					GridLines(out var min, out _, out _);
					var pos =
						AxisType == EAxis.XAxis ? new v4((float)(wh.x / 2 - min), wh.y / 2, (float)(cam.FocusDist * Chart.Options.GridZOffset), 0) :
						AxisType == EAxis.YAxis ? new v4(wh.x / 2, (float)(wh.y / 2 - min), (float)(cam.FocusDist * Chart.Options.GridZOffset), 0) :
						throw new Exception("Unknown axis type");

					var o2w = cam.O2W;
					o2w.pos = cam.FocusPoint - o2w * pos;

					GridLineGfx.O2WSet(o2w);
					window.AddObject(GridLineGfx);
				}

				/// <summary>Default value to text conversion</summary>
				public string DefaultTickText(double x, double? step = null)
				{
					// This solves the rounding problem for values near zero when the axis span could be anything
					return !Math_.FEql(x / Span, 0.0) ? Math_.RoundSD(x, 5).ToString("G8") : "0";
				}

				/// <summary>Link this axis to another axis so that this axis always follows the other axis. Note: circular links are allowed</summary>
				public AxisLinkData? LinkTo
				{
					get => m_link_to;
					set
					{
						// Usage:
						//  Assign an instance to this property containing the axis you want to mirror, with
						//  an optional conversion delegate for handling axes at different scales/units.
						//  To unlink, just assign to null.
						//  Circular links are supported.

						if (m_link_to == value) return;
						if (m_link_to != null)
						{
							m_link_to.Axis.Moved -= HandleMoved;
						}
						m_link_to = value;
						if (m_link_to != null)
						{
							m_link_to.Axis.Moved += HandleMoved;
						}

						void HandleMoved(object? sender, EventArgs e)
						{
							// Ignore if this is a recursive call
							if (m_in_set != 0 || m_link_to == null)
								return;

							var range = m_link_to.Convert(m_link_to.Axis.Range);
							Set(range.Beg, range.End);
							Chart.SetCameraFromRange();
							Chart.Invalidate();
						}
					}
				}
				private AxisLinkData? m_link_to;

				/// <summary>Friendly string view</summary>
				public override string ToString()
				{
					return $"{Label} [{Min} , {Max}]";
				}

				#region Equals
				public bool Equals(Axis? rhs)
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
				public override bool Equals(object? obj)
				{
					return Equals(obj as Axis);
				}
				public override int GetHashCode()
				{
					return new { AxisType, Label, Min, Max }.GetHashCode();
				}
				#endregion
			}
		}

		/// <summary>Data used to link axes</summary>
		public class AxisLinkData
		{
			public AxisLinkData(RangeData.Axis axis, Func<RangeF, RangeF>? convert = null)
			{
				Axis = axis;
				Convert = convert ?? (x => x);
			}

			/// <summary>The chart that owns the axis</summary>
			public ChartControl Chart => Axis.Chart;

			/// <summary>The Axis to copy from</summary>
			public RangeData.Axis Axis { get; }

			/// <summary>Conversion function to map 'Axis's range to the desired range</summary>
			public Func<RangeF, RangeF> Convert { get; }
		}
	}
}