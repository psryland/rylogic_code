//***************************************************
// Copyright (c) Rylogic Ltd 2008
//***************************************************
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.maths;
using pr.util;

namespace pr.gui
{
	/// <summary>Custom control for rendering a graph</summary>
	public class GraphControl :UserControl
	{
		/// <summary>Graph data points</summary>
		public struct GraphValue
		{
			/// <summary>Data point X value</summary>
			public double x;

			/// <summary>Data point Y value</summary>
			public double y;

			/// <summary>Lower error bound for the y value. Use delta, i.e plotted as y + y_lo, i.e. probably a negative number</summary>
			public double ylo;

			/// <summary>Upper error bound for the y value. Use delta, i.e plotted as y + y_hi, i.e. probably a positive number</summary>
			public double yhi;

			/// <summary>Allow users to attach data to each value in the graph</summary>
			public object m_tag;

			public GraphValue(double x, double y)                           :this(x, y, 0, 0, null)       {}
			public GraphValue(double x, double y, object tag)               :this(x, y, 0, 0, tag)        {}
			public GraphValue(double x, double y, double y_lo, double y_hi) :this(x, y, y_lo, y_hi, null) {}
			public GraphValue(double x, double y, double y_lo, double y_hi, object tag)
			{
				Debug.Assert(Maths.IsFinite(x) && Maths.IsFinite(y));
				this.x       = x;
				this.y       = y;
				this.ylo    = y_lo;
				this.yhi    = y_hi;
				this.m_tag   = tag;
			}
			public override string ToString()
			{
				return "({0},{1})".Fmt(x,y);
			}
			public static Comparer<GraphValue> SortX
			{
				get { return Cmp<GraphValue>.From((lhs,rhs) => (lhs.x > rhs.x ? 1 : 0) - (lhs.x < rhs.x ? 1 : 0)); }
			}
		}

		/// <summary>Rendering options</summary>
		public class RdrOptions
		{
			public RdrOptions()
			{
				BkColour        = SystemColors.Control;
				PlotBkColour    = Color.White;
				TitleColour     = Color.Black;
				TitleFont       = new Font("tahoma", 12, FontStyle.Bold);
				TitleTransform  = new Matrix(1f, 0f, 0f, 1f, 0f, 0f);
				AxisColour      = Color.Black;
				GridColour      = Color.FromArgb(230, 230, 230);
				PixelsPerTick   = new v2(30f, 24f);
				LeftMargin      = 3;
				RightMargin     = 3;
				TopMargin       = 3;
				BottomMargin    = 3;
				NoteFont        = new Font("tahoma",  8, FontStyle.Regular);
				SelectionColour = Color.DarkGray;
			}

			/// <summary>The fill colour of the background of the graph</summary>
			public Color BkColour { get; set; }

			/// <summary>The fill colour of the plot background</summary>
			public Color PlotBkColour { get; set; }

			/// <summary>The colour of the title text</summary>
			public Color TitleColour { get; set; }

			/// <summary>Font to use for the title text</summary>
			public Font TitleFont { get; set; }

			/// <summary>Transform for position the graph title, offset from top centre</summary>
			public Matrix TitleTransform { get; set; }

			/// <summary>The colour of the axes</summary>
			public Color AxisColour { get; set; }

			/// <summary>The colour of the grid lines</summary>
			public Color GridColour { get; set; }

			/// <summary></summary>
			public v2 PixelsPerTick { get; set; }

			/// <summary>Graph margin in pixels</summary>
			public int LeftMargin   { get; set; }

			/// <summary>Graph margin in pixels</summary>
			public int RightMargin  { get; set; }

			/// <summary>Graph margin in pixels</summary>
			public int TopMargin    { get; set; }

			/// <summary>Graph margin in pixels</summary>
			public int BottomMargin { get; set; }

			/// <summary>Font to use for graph notes</summary>
			public Font NoteFont { get; set; }

			/// <summary>Area selection colour</summary>
			public Color SelectionColour { get; set; }

			/// <summary>Create a new copy</summary>
			public RdrOptions Clone()
			{
				return (RdrOptions)MemberwiseClone();
			}
		}

		/// <summary>Graph data series</summary>
		[Serializable]
		public class Series :List<GraphValue>
		{
			public class RdrOptions
			{
				public enum EPlotType { Point, Line, Bar }
				public bool       Visible        { get; set; }
				public bool       DrawData       { get; set; }
				public bool       DrawErrorBars  { get; set; }
				public EPlotType  PlotType       { get; set; }
				public Color      PointColour    { get; set; }
				public float      PointSize      { get; set; }
				public Color      LineColour     { get; set; }
				public float      LineWidth      { get; set; }
				public Color      BarColour      { get; set; }
				public float      BarWidth       { get; set; }
				public Color      ErrorBarColour { get; set; }
				public bool       DrawMovingAvr  { get; set; }
				public int        MAWindowSize   { get; set; }
				public Color      MALineColour   { get; set; }
				public float      MALineWidth    { get; set; }

				public RdrOptions()
				{
					Visible        = true;
					DrawData       = true;
					DrawErrorBars  = false;
					PlotType       = EPlotType.Line;
					PointColour    = Color.FromArgb(0xff, 0x80, 0, 0);
					PointSize      = 5f;
					LineColour     = Color.FromArgb(0xff, 0, 0, 0);
					LineWidth      = 1f;
					BarColour      = Color.FromArgb(0xff, 0x80, 0, 0);
					BarWidth       = 0.8f;
					ErrorBarColour = Color.FromArgb(0x80, 0xff, 0, 0);
					DrawMovingAvr  = false;
					MAWindowSize   = 10;
					MALineColour   = Color.FromArgb(0xff, 0, 0, 0xFF);
					MALineWidth    = 3f;
				}
				public RdrOptions Clone() { return (RdrOptions)MemberwiseClone(); }
			}

			public Series() :this(string.Empty) {}
			public Series(string name) :this(string.Empty, 100) {}
			public Series(string name, int capacity) :this(name, capacity, new RdrOptions()) {}
			public Series(string name, int capacity, RdrOptions rdr_opts) :base(capacity)
			{
				Name          = name;
				RenderOptions = rdr_opts;
				AllowDelete   = false;
				Sorted        = true; // Assume sorted because this is more efficient for rendering
			}
			public Series(Series src) :base(src)
			{
				Name          = src.Name;
				RenderOptions = src.RenderOptions.Clone();
				AllowDelete   = src.AllowDelete;
				Sorted        = src.Sorted;
			}

			///<summary>
			/// Return the range of indices that need to be considered when plotting from 'xmin' to 'xmax'
			/// Note: in general, this range should include one point to the left of xmin and one to the right
			/// of xmax so that line graphs plot a line up to the border of the plot area</summary>
			public void IndexRange(double xmin, double xmax, out int imin, out int imax)
			{
				var lwr = new GraphValue(xmin, 0.0f);
				var upr = new GraphValue(xmax, 0.0f);

				imin = BinarySearch(0, Count, lwr, GraphValue.SortX);
				if (imin < 0) imin = ~imin;
				if (imin != 0) --imin;

				imax = BinarySearch(imin, Count - imin, upr, GraphValue.SortX);
				if (imax < 0) imax = ~imax;
				if (imax != Count) ++imax;
			}

			/// <summary>Enumerable access to the data given an index range</summary>
			public IEnumerable<GraphValue> Values(int i0 = 0, int i1 = int.MaxValue)
			{
				for (int i = i0, iend = Math.Min(i1, Count); i != iend; ++i)
				{
					var gv = this[i];
					yield return gv;
				}
			}

			/// <summary>
			/// Returns the range of series data to consider when plotting from 'xmin' to 'xmax'
			/// Note: in general, this range should include one point to the left of xmin and one to
			/// the right of xmax so that line graphs plot a line up to the border of the plot area</summary>
			public IEnumerable<GraphValue> Values(double xmin, double xmax)
			{
				int i0,i1;
				IndexRange(xmin, xmax, out i0, out i1);
				return Values(i0, i1);
			}

			/// <summary>
			/// Returns the range of indices to consider when plotting from 'xmin' to 'xmax'
			/// Note: in general, this range should include one point to the left of xmin and one to
			/// the right of xmax so that line graphs plot a line up to the border of the plot area</summary>
			public IEnumerable<int> Indices(double xmin, double xmax)
			{
				int i0,i1;
				IndexRange(xmin, xmax, out i0, out i1);
				return Enumerable.Range(i0, i1 - i0);
			}

			/// <summary>A label for the series</summary>
			public string Name { get; set; }

			/// <summary>Options for renderering this series</summary>
			public RdrOptions RenderOptions { get; set; }

			/// <summary></summary>
			public bool AllowDelete { get; set; }

			/// <summary>True if this series can be considered sorted (on X)</summary>
			public bool Sorted { get; set; }

			/// <summary>Sort the series by it's X value</summary>
			public void SortX()
			{
				Sort(Cmp<GraphValue>.From((l,r) => l.x < r.x));
				Sorted = true;
			}

			/// <summary>ToString</summary>
			public override string ToString()
			{
				return "{0} - count:{1}".Fmt(Name, Count);
			}

			/// <summary>Plot colour generator</summary>
			public static Color Colour(int i)
			{
				return m_colours[i % m_colours.Length];
			}
			private static Color[] m_colours =
			{
				Color.Black     ,
				Color.Blue      , Color.Red       , Color.Green      ,
				Color.DarkBlue  , Color.DarkRed   , Color.DarkGreen  ,
				Color.Purple    , Color.Turquoise , Color.Magenta    ,
				Color.Orange    , Color.Yellow    ,
				Color.LightBlue , Color.LightSalmon , Color.LightGreen ,
			};
		}

		/// <summary>Graph axis data</summary>
		public class Axis
		{
			/// <summary>The range of values along this axis</summary>
			public struct Range
			{
				/// <summary>The minimum value of the range</summary>
				public double m_min;

				/// <summary>The maximum value of the axis</summary>
				public double m_max;

				/// <summary>The length of the range (i.e. max - min)</summary>
				public double m_span
				{
					get { return m_max - m_min; }
					set
					{
						var c = m_centre;
						m_min = c - 0.5*value;
						m_max = c + 0.5*value;
					}
				}

				/// <summary>The centre value of the range</summary>
				public double m_centre
				{
					get { return m_min + m_span*0.5; }
					set
					{
						var d = value - m_centre;
						m_min += d;
						m_max += d;
					}
				}

				public static Range Default
				{
					get { return new Range(0.0, 1.0); }
				}
				public Range(double min, double max)
				{
					m_min = min;
					m_max = max;
				}
				public Range(Range rhs)
				{
					m_min = rhs.m_min;
					m_max = rhs.m_max;
				}
				public override string ToString()
				{
					return "[{0}:{1}]".Fmt(m_min, m_max);
				}
			}

			/// <summary>Options related to rendering this axis</summary>
			public class RdrOpts
			{
				public RdrOpts()
				{
					LabelFont      = new Font("tahoma", 10, FontStyle.Regular);
					TickFont       = new Font("tahoma", 8, FontStyle.Regular);
					AxisColour     = Color.Black;
					LabelColour    = Color.Black;
					TickColour     = Color.Black;
					TickLength     = 5;
					DrawTickMarks  = true;
					DrawTickLabels = true;
					LabelTransform = new Matrix(1f, 0f, 0f, 1f, 0f, 0f);
				}

				/// <summary>The font to use for the axis label</summary>
				public Font LabelFont { get; set; }

				/// <summary>The font to use for tick labels</summary>
				public Font TickFont { get; set; }

				/// <summary>The colour of the main axes</summary>
				public Color AxisColour { get; set; }

				/// <summary>The colour of the label text</summary>
				public Color LabelColour { get; set; }

				/// <summary>The colour of the tick text</summary>
				public Color TickColour { get; set; }

				/// <summary>The length of the tick marks</summary>
				public int TickLength { get; set; }

				/// <summary>True if tick marks should be drawn</summary>
				public bool DrawTickMarks { get; set; }

				/// <summary>True if tick labels should be drawn</summary>
				public bool DrawTickLabels { get; set; }

				/// <summary>Offset transform from default label position</summary>
				public Matrix LabelTransform { get; set; }

				/// <summary>Copy</summary>
				public RdrOpts Clone()
				{
					return (RdrOpts)MemberwiseClone();
				}
			}

			public Axis()
			{
				Label         = string.Empty;
				Rng           = Range.Default;
				RenderOptions = new RdrOpts();
				AllowScroll   = true;
				AllowZoom     = true;
				LockRange     = false;
				TickText      = x => Math.Round(x, 4, MidpointRounding.AwayFromZero).ToString("G4");
			}
			public Axis(Axis rhs)
			{
				Label         = rhs.Label         ;
				Rng           = rhs.Rng           ;
				RenderOptions = rhs.RenderOptions .Clone();
				AllowScroll   = rhs.AllowScroll   ;
				AllowZoom     = rhs.AllowZoom     ;
				LockRange     = rhs.LockRange     ;
				TickText      = rhs.TickText      ;
			}

			/// <summary>Axis label</summary>
			public string Label { get; set; }

			/// <summary>The range of values along this axis</summary>
			public Range Rng
			{
				get { return m_impl_rng; }
				set
				{
					Debug.Assert(value.m_span > 0, "Range must be positive and non-zero");
					m_impl_rng = value;
				}
			}
			private Range m_impl_rng;

			/// <summary>The minimum axis value</summary>
			public double Min
			{
				get { return Rng.m_min; }
				set { Rng = new Range(value, Max); }
			}

			/// <summary>The maximum axis value</summary>
			public double Max
			{
				get { return Rng.m_max; }
				set { Rng = new Range(Min, value); }
			}

			/// <summary>The total range of this axis (max - min)</summary>
			public double Span
			{
				get { return Rng.m_span; }
				set { Rng = new Range(Rng){m_span = value}; }
			}

			/// <summary>Allow scrolling on this axis</summary>
			public bool AllowScroll { get; set; }

			/// <summary>Allow zooming on this axis</summary>
			public bool AllowZoom { get; set; }

			/// <summary>Get/Set whether the range can be changed by user input</summary>
			public bool LockRange { get; set; }

			/// <summary>Rendering options for this axis</summary>
			public RdrOpts RenderOptions { get; private set; }

			/// <summary>Convert the axis value to a string</summary>
			public Func<double,string> TickText;

			/// <summary>Scroll the axis by 'delta'</summary>
			public void Shift(float delta)
			{
				if (!AllowScroll) return;
				Rng = new Range(Min + delta, Max + delta);
			}

			public override string ToString()
			{
				return "{0} - [{1}:{2}]".Fmt(Label, Min, Max);
			}
		}

		/// <summary>A snapshot of the current graph, used during dragging/zooming</summary>
		private class Snapshot
		{
			public Bitmap     m_bm;
			public Axis.Range m_xrange;
			public Axis.Range m_yrange;
			public Size Size
			{
				get { return new Size(m_bm != null ? m_bm.Width : 0, m_bm != null ? m_bm.Height : 0); }
			}
			public Rectangle Rect
			{
				get { return new Rectangle(Point.Empty, Size); }
			}
			public Snapshot()
			{
				m_bm = null;
				m_xrange = new Axis.Range();
				m_yrange = new Axis.Range();
			}
		}

		/// <summary>A note to add to the graph</summary>
		public class Note
		{
			public string m_msg;
			public PointF m_loc;
			public Color  m_colour;
			public Note(string msg, PointF loc) :this(msg, loc,  Color.Black) {}
			public Note(string msg, PointF loc, Color colour) {m_msg = msg; m_loc = loc; m_colour = colour;}
		}

		// Constructors
		public GraphControl()
			:this(string.Empty, Axis.Range.Default, Axis.Range.Default, new Axis(), new Axis(), new RdrOptions(), new List<Series>())
		{}
		public GraphControl(GraphControl src)
			:this(src.Title, src.BaseRangeX, src.BaseRangeY, new Axis(src.XAxis), new Axis(src.YAxis), src.RenderOptions.Clone(), new List<Series>(src.Data))
		{
			AddOverlayOnRender = src.AddOverlayOnRender;
			AddOverlayOnPaint = src.AddOverlayOnPaint;
		}
		private GraphControl(string title, Axis.Range base_xrange, Axis.Range base_yrange, Axis xaxis, Axis yaxis, RdrOptions rdr_options, List<Series> data)
		{
			MutexRendering  = new object();
			m_rdr_thread    = null;
			m_snap          = new Snapshot();
			m_tmp           = new Snapshot();
			Title           = title;
			XAxis           = xaxis;
			YAxis           = yaxis;
			RenderOptions   = rdr_options;
			m_impl_data     = data;
			Notes           = new List<Note>();
			BaseRangeX      = base_xrange;
			BaseRangeY      = base_yrange;
			m_grab_location = PointF.Empty;
			m_selection     = Rectangle.Empty;
			m_impl_zoom     = new Axis.Range(float.Epsilon, float.MaxValue);
			m_tooltip       = new ToolTip{ShowAlways = false, UseAnimation = false, UseFading = false, Tag = false};
			m_mutex_snap    = new object();
			m_plot_area     = Rectangle.Empty;
			MouseNavigation = true;
			Dirty           = true;
			DoubleBuffered  = true;
		}
		protected override void Dispose(bool disposing)
		{
			m_rdr_cancel = true;
			if (m_rdr_thread != null)
				m_rdr_thread.Join();

			Util.Dispose(ref m_tooltip);
			base.Dispose(disposing);
		}
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			Dirty = true;
		}
		protected override void OnPaintBackground(PaintEventArgs e)
		{
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			DoPaint(e.Graphics, ClientRectangle);
		}

		/// <summary>
		/// A mutex that is locked by the control during rendering
		/// This should be used to synchronise access to the source data with rendering</summary>
		public readonly object MutexRendering;

		/// <summary>A snapshot of the plot area, used as a temporary copy during drag/zoom operations</summary>
		private Snapshot m_snap;

		/// <summary>A temporary bitmap used for background thread rendering</summary>
		private Snapshot m_tmp;

		/// <summary>The graph title</summary>
		public string Title
		{
			get { return m_impl_title; }
			set
			{
				if (m_impl_title == value) return;
				m_impl_title = value;
				Dirty = true;
			}
		}
		private string m_impl_title;

		/// <summary>The graph X axis</summary>
		public Axis XAxis { get; private set; }

		/// <summary>The graph Y axis</summary>
		public Axis YAxis { get; private set; }

		/// <summary>Rendering options for the graph</summary>
		public RdrOptions RenderOptions { get; private set; }

		/// <summary>The colleciton of series' that make up the graph data</summary>
		[Browsable(false)]
		public List<Series> Data
		{
			get { return m_impl_data; }
			set
			{
				if (m_impl_data == value) return;
				lock (MutexRendering)
					m_impl_data = value;
			}
		}
		private List<Series> m_impl_data;

		/// <summary>Notes to add to the graph</summary>
		[Browsable(false)]
		public List<Note> Notes { get; private set; }

		/// <summary>Set the title and axis labels of the graph</summary>
		public void SetLabels(string title, string xaxis_text, string yaxis_text)
		{
			Title = title;
			XAxis.Label = xaxis_text;
			YAxis.Label = yaxis_text;
		}

		#region Data Access

		/// <summary>Returns the 'Y' value for a given 'X' value in a series in the graph</summary>
		public double GetValueAt(int series_index, double x)
		{
			if (series_index < 0 || series_index >= Data.Count)
				throw new Exception("series index out of range");

			// Binary search on X for the nearest data point
			var series = Data[series_index];
			if (series.Count == 0)
				return 0;

			// Find the index range that cotnains 'x'
			int i0,i1;
			series.IndexRange(x, x, out i0, out i1);

			// Search for the nearest on the left and right of 'x'
			// (note: not assuming the series is sorted here)
			var lhs = 0;
			var rhs = series.Count - 1;
			for (var i = i0; i != i1; ++i)
			{
				var tmp = series[i];
				if (series[lhs].x < tmp.x && tmp.x < x) lhs = i;
				if (series[rhs].x > tmp.x && tmp.x > x) rhs = i;
			}
			if (series[lhs].x > x && series[rhs].x < x) return 0;
			if (series[lhs].x > x) return series[rhs].y;
			if (series[rhs].x < x) return series[lhs].y;
			var t = (x - series[lhs].x) / (series[rhs].x - series[lhs].x);
			return (1f - t) * series[lhs].y + (t) * series[rhs].y;
		}

		/// <summary>Returns the nearest graph data point to 'location' given
		/// a selection tolerance. 'location' should be in graph space.
		/// Returns null if no point is within the selection tolerance</summary>
		public bool GetValueAt(int series_index, PointF pt, out GraphValue gv, int px_tol = 5)
		{
			if (series_index < 0 || series_index >= Data.Count)
				throw new Exception("series index out of range");

			var series = Data[series_index];
			var tol = px_tol * m_plot_area.Width / XAxis.Span;
			var dist_sq = tol * tol;

			gv = new GraphValue();
			foreach (var v in series.Values(pt.X - tol, pt.X + tol))
			{
				var tmp = new v2((float)v.x, (float)v.y);
				var d = (tmp - pt).Length2Sq;
				if (d < dist_sq)
				{
					dist_sq = d;
					gv = v;
				}
			}
			return dist_sq < tol * tol;
		}

		#endregion

		#region Navigation

		/// <summary>Default range for the x axis</summary>
		public Axis.Range BaseRangeX
		{
			get { return m_base_range_x; }
			set
			{
				Debug.Assert(value.m_span > 0, "Range must be positive and non-zero");
				m_base_range_x = value;
			}
		}
		private Axis.Range m_base_range_x;

		/// <summary>Default range for the y axis</summary>
		public Axis.Range BaseRangeY
		{
			get { return m_base_range_y; }
			set
			{
				Debug.Assert(value.m_span > 0, "Range must be positive and non-zero");
				m_base_range_y = value;
			}
		}
		private Axis.Range m_base_range_y;

		/// <summary>
		/// Find the appropriate range for all data in the graph.
		/// Call ResetToDefaultRange() to zoom the graph to this range</summary>
		public void FindDefaultRange()
		{
			var xrng = new Axis.Range(double.MaxValue, -double.MaxValue);
			var yrng = new Axis.Range(double.MaxValue, -double.MaxValue);
			foreach (var series in Data)
			{
				if (m_rdr_cancel)
					break;

				if (!series.RenderOptions.Visible)
					continue;

				// note: series.Sorted doesn't help because we
				// still need to scan all the Y values.
				foreach (var v in series.Values())
				{
					if (v.x < xrng.m_min) xrng.m_min = v.x;
					if (v.x > xrng.m_max) xrng.m_max = v.x;
					if (v.y < yrng.m_min) yrng.m_min = v.y;
					if (v.y > yrng.m_max) yrng.m_max = v.y;
				}
			}
			if (xrng.m_span > 0) xrng.m_span = xrng.m_span * 1.05f; else xrng = Axis.Range.Default;
			if (yrng.m_span > 0) yrng.m_span = yrng.m_span * 1.05f; else yrng = Axis.Range.Default;
			BaseRangeX = xrng;
			BaseRangeY = yrng;
		}

		/// <summary>
		/// Reset the axis ranges to the default.
		/// Call FindDefaultRange() to set the default range</summary>
		public void ResetToDefaultRange()
		{
			if (!XAxis.LockRange) { XAxis.Rng = BaseRangeX; }
			if (!YAxis.LockRange) { YAxis.Rng = BaseRangeY; }
			Dirty = true;
		}

		/// <summary>
		/// Returns a point in graph space from a point in client space
		/// Use to convert mouse (client-space) locations to graph coordinates</summary>
		public PointF PointToGraph(Point point)
		{
			return new PointF(
				(float)(XAxis.Min + (point.X - m_plot_area.Left  ) * XAxis.Span / m_plot_area.Width ),
				(float)(YAxis.Min - (point.Y - m_plot_area.Bottom) * YAxis.Span / m_plot_area.Height));
		}

		/// <summary>Returns a point in client space from a point in graph space. Inverse of PointToGraph</summary>
		public Point GraphToPoint(PointF gs_point)
		{
			return new Point(
				(int)(m_plot_area.Left   + (gs_point.X - XAxis.Min) * m_plot_area.Width  / XAxis.Span),
				(int)(m_plot_area.Bottom - (gs_point.Y - YAxis.Min) * m_plot_area.Height / YAxis.Span));
		}

		/// <summary>Shifts the X and Y range of the graph so that graph space position 'gs_point' is at client space position 'cs_point'</summary>
		public void PositionGraph(Point cs_point, PointF gs_point)
		{
			var dst = PointToGraph(cs_point);
			XAxis.Shift(gs_point.X - dst.X);
			YAxis.Shift(gs_point.Y - dst.Y);
			Dirty = true;
		}

		/// <summary>Get/Set the centre of the graph</summary>
		[Browsable(false)] public PointF Centre
		{
			get { return new PointF((float)(XAxis.Min + XAxis.Span*0.5), (float)(YAxis.Min + YAxis.Span*0.5)); }
			set
			{
				XAxis.Min = value.X - XAxis.Span*0.5;
				YAxis.Min = value.Y - YAxis.Span*0.5;
				Dirty = true;
			}
		}

		/// <summary>Zoom in/out on the graph. Remember to call refresh. Zoom is a floating point value where 1f = no zoom, 2f = 2x magnification</summary>
		[Browsable(false)] public double Zoom
		{
			get
			{
				return
					XAxis.AllowZoom ? XAxis.Span / BaseRangeX.m_span :
					YAxis.AllowZoom ? YAxis.Span / BaseRangeY.m_span : 1f;
			}
			set
			{
				var aspect = (YAxis.Span * BaseRangeX.m_span) / (BaseRangeY.m_span * XAxis.Span);
				aspect = Maths.Clamp(Maths.IsFinite(aspect) ? aspect : 1.0, 0.001, 1000);
				
				value = Maths.Clamp(value, m_impl_zoom.m_min, m_impl_zoom.m_max);
				if (XAxis.AllowZoom) XAxis.Span = BaseRangeX.m_span * value         ;
				if (YAxis.AllowZoom) YAxis.Span = BaseRangeY.m_span * value * aspect;
				Dirty = true;
			}
		}
		private Axis.Range m_impl_zoom;

		/// <summary>Minimum zoom limit</summary>
		public double ZoomMin
		{
			get { return m_impl_zoom.m_min; }
			set
			{
				Debug.Assert(value > 0f);
				m_impl_zoom.m_min = value;
			}
		}

		/// <summary>Maximum zoom limit</summary>
		public double ZoomMax
		{
			get { return m_impl_zoom.m_max; }
			set
			{
				Debug.Assert(value > 0f);
				m_impl_zoom.m_max = value;
			}
		}

		/// <summary>Enable/Disable default mouse control of the graph</summary>
		[Browsable(false)] public bool MouseNavigation
		{
			set
			{
				MouseDown  -= OnMouseDown;
				MouseUp    -= OnMouseUp;
				MouseWheel -= OnMouseWheel;
				if (value)
				{
					MouseDown  += OnMouseDown;
					MouseUp    += OnMouseUp;
					MouseWheel += OnMouseWheel;
				}
			}
		}

		/// <summary>The location in graph space of where the graph was "grabbed"</summary>
		private PointF m_grab_location;

		/// <summary>Area selection, has width, height of zero when the user isn't selecting</summary>
		private Rectangle m_selection;

		// Mouse navigation - public to allow users to forward mouse calls to us.
		public void OnMouseDown(object sender, MouseEventArgs e)
		{
			m_selection = new Rectangle(e.Location, Size.Empty);
			switch (e.Button)
			{
			case MouseButtons.Left:
				m_grab_location = PointToGraph(e.Location);
				Cursor = Cursors.SizeAll;
				MouseMove -= OnMouseGraphDrag;
				MouseMove += OnMouseGraphDrag;
				Capture = true;
				break;

			case MouseButtons.Right:
				MouseMove -= OnMouseAreaSelect;
				MouseMove += OnMouseAreaSelect;
				Capture = true;
				break;
			}
		}
		public void OnMouseUp(object sender, MouseEventArgs e)
		{
			switch (e.Button)
			{
			case MouseButtons.Left:
				Cursor = Cursors.Default;
				MouseMove -= OnMouseGraphDrag;
				Capture = false;
				break;

			case MouseButtons.Right:
				MouseMove -= OnMouseAreaSelect;

				// If the selection has area, rescale the graph
				if (Math.Abs(m_selection.Width) > 0 && Math.Abs(m_selection.Height) > 0)
				{
					// Normalise the rectangle
					var sel = m_selection.NormalizeRect();

					// Rescale the graph
					var lower = PointToGraph(new Point(sel.Left, sel.Bottom));
					var upper = PointToGraph(new Point(sel.Right, sel.Top));
					XAxis.Min = lower.X;
					XAxis.Max = upper.X;
					YAxis.Min = lower.Y;
					YAxis.Max = upper.Y;
					Dirty = true;

					// Clear the selection
					m_selection.Width  = 0;
					m_selection.Height = 0;
				}
				else
				{
					ShowContextMenu(e.Location);
				}

				Capture = false;
				break;
			}
		}
		public void OnMouseWheel(object sender, MouseEventArgs e)
		{
			if (!m_plot_area.Contains(e.Location))
				return;

			var point = PointToGraph(e.Location);
			var delta = Maths.Clamp(e.Delta, -999, 999);
			Zoom *= (1.0f - delta * 0.001f);
			PositionGraph(e.Location, point);
			Dirty = true;
		}

		/// <summary>Handle mouse dragging the graph around</summary>
		private void OnMouseGraphDrag(object sender, MouseEventArgs e)
		{
			var grab_loc = GraphToPoint(m_grab_location);
			var dx = e.Location.X - grab_loc.X;
			var dy = e.Location.Y - grab_loc.Y;
			if (dx*dx + dy*dy >= 25f) // must drag at least 5 pixels
				PositionGraph(e.Location, m_grab_location);
		}

		/// <summary>Handle mouse move events while dragging</summary>
		private void OnMouseAreaSelect(object sender, MouseEventArgs e)
		{
			const int MinAreaSelectDistance = 3;
			m_selection.Width  = e.Location.X - m_selection.X;
			m_selection.Height = e.Location.Y - m_selection.Y;
			if (Math.Abs(m_selection.Width)  < MinAreaSelectDistance) m_selection.Width  = 0;
			if (Math.Abs(m_selection.Height) < MinAreaSelectDistance) m_selection.Height = 0;
			Invalidate();
		}

		#endregion

		#region Rendering

		/// <summary>True when the bitmap needs to be regenerated</summary>
		[Browsable(false)] public bool Dirty
		{
			get { return m_impl_dirty; }
			set
			{
				// Note, don't bother with an event batcher, Refresh is fast once the bitmap is generated
				m_impl_dirty |= value;
				if (m_impl_dirty)
					Invalidate(true);
			}
		}
		private bool m_impl_dirty;

		/// <summary>A background thread used to render the plot</summary>
		private Thread m_rdr_thread;

		/// <summary>Atomic flag for cancelling renderering (yes, bool is atomic in C#)</summary>
		private volatile bool m_rdr_cancel;

		/// <summary>Mutex that synchronises access to the Snapshot objects</summary>
		private object m_mutex_snap;

		/// <summary>The screen space size of the plot part of the control</summary>
		private Rectangle m_plot_area;

		/// <summary>Render the graph synchronously.</summary>
		public void RenderGraph(Graphics gfx, Rectangle area, out Rectangle plot_area)
		{
			plot_area = PlotArea(gfx, area);
			RenderGraphFrame(gfx, area, plot_area);
			RenderData(gfx, plot_area);
		}
		public void RenderGraph(Graphics gfx, Rectangle area)
		{
			Rectangle plot_area;
			RenderGraph(gfx, area, out plot_area);
		}

		///<summary>
		/// Get the transform from client space to graph space.
		/// Returns the x/y scaling factor in 'scale'
		/// Note: The returned transform has no scale because lines and
		/// points would also be scaled turning them into elipses or caligraphy etc</summary>
		public void ClientToGraphSpace(Rectangle plot_area, out Matrix c2g, out v2 scale)
		{
			var plot = plot_area.Shifted(1,1).Inflated(0, 0, -1,-1);
			scale = new v2((float)(plot.Width / XAxis.Span), (float)(plot.Height / YAxis.Span));
			if (!Maths.IsFinite(scale.x)) scale.x = scale.x >= 0 ? float.MaxValue : -float.MaxValue;
			if (!Maths.IsFinite(scale.y)) scale.y = scale.y >= 0 ? float.MaxValue : -float.MaxValue;
			c2g = new Matrix(1f, 0f, 0f, -1f, (float)(plot.Left - XAxis.Min * scale.x), (float)(plot.Bottom + YAxis.Min * scale.y));
		}
		public void ClientToGraphSpace(out Matrix c2g, out v2 scale)
		{
			ClientToGraphSpace(m_plot_area, out c2g, out scale);
		}
		public void ClientToGraphSpace(out Matrix c2g)
		{
			v2 scale;
			ClientToGraphSpace(out c2g, out scale);
		}

		/// <summary>Render the graph control into 'gfx' within 'area'</summary>
		private void DoPaint(Graphics gfx, Rectangle area)
		{
			try
			{
				m_plot_area = PlotArea(gfx, area);

				// If the graph is dirty, begin an asynchronous render of the plot into 'm_tmp'
				if (Dirty)
				{
					m_rdr_cancel = true;
					if (m_rdr_thread != null) m_rdr_thread.Join();
					m_rdr_cancel = false;

					// Make sure the temporary bitmap and the snapshot bitmp are the correct size
					var plot_size = m_plot_area.Size;
					if (m_tmp.Size != plot_size)
					{
						Util.Dispose(ref m_tmp.m_bm);
						m_tmp.m_bm = new Bitmap(plot_size.Width, plot_size.Height);
					}
					m_tmp.m_xrange = XAxis.Rng;
					m_tmp.m_yrange = YAxis.Rng;

					// Plot rendering (done in a background thread).
					// This thread renders the plot into the bitmap in 'm_tmp' using readonly access to the series data.
					m_rdr_thread = new Thread(() =>
					{
						// Hold the rendering CS. Clients should hold this if they want to
						// modify the data while the graph is potentially rendering
						// Render the plot into 'm_tmp'
						lock (MutexRendering)
						{
							var g = Graphics.FromImage(m_tmp.m_bm);
							RenderData(g, m_tmp.Rect);
						}

						// If the render was cancelled, ignore the result
						if (m_rdr_cancel)
							return;

						// Otherwise get the main thread to do something with the plot bitmap
						lock (m_mutex_snap)
							Util.Swap(ref m_snap, ref m_tmp);

						// Cause a refresh
						Invalidate();
					});
					m_rdr_thread.Start();

					m_impl_dirty = false;
				}

				// In the mean time, compose the graph in 'm_bm' by rendering the frame
				// synchronously and blt'ing the last snapshot into the plot area
				RenderGraphFrame(gfx, area, m_plot_area);

				gfx.SetClip(m_plot_area.Shifted(1,1).Inflated(0,0,-1,-1));
				gfx.SmoothingMode = SmoothingMode.HighQuality;
				lock (m_mutex_snap)
				{
					if (m_snap.m_bm != null)
					{
						var tl = GraphToPoint(new PointF((float)m_snap.m_xrange.m_min, (float)m_snap.m_yrange.m_max));
						var br = GraphToPoint(new PointF((float)m_snap.m_xrange.m_max, (float)m_snap.m_yrange.m_min));
						var dst_rect = Rectangle.FromLTRB((int)tl.X, (int)tl.Y, (int)br.X, (int)br.Y);
						var src_rect = m_snap.Rect;
						gfx.DrawImage(m_snap.m_bm, dst_rect, (int)src_rect.X, (int)src_rect.Y, (int)src_rect.Width, (int)src_rect.Height, GraphicsUnit.Pixel);
					}
				}

				// Allow clients to draw on the graph
				AddOverlayOnPaint.Raise(this, new OverlaysEventArgs(gfx));

				// Draw the selection rubber band
				if (m_selection.Width != 0 && m_selection.Height != 0)
				{
					var sel = m_selection.NormalizeRect();
					using (var pen = new Pen(RenderOptions.SelectionColour))
					{
						pen.DashStyle = DashStyle.Dot;
						gfx.DrawRectangle(pen, sel);
					}
				}

				gfx.ResetClip();
			}
			catch (OverflowException)
			{
				// There is a problem in the .NET graphics object that can cause these exceptions if the range is extreme
				using (var bsh = new SolidBrush(Color.FromArgb(0x80, Color.Black)))
					gfx.DrawString("Rendering error within .NET", RenderOptions.TitleFont, bsh, PointF.Empty);
			}
		}

		/// <summary>Get the rectangular area of the graph for a given client area</summary>
		private Rectangle PlotArea(Graphics gfx, Rectangle area)
		{
			var rect = new RectangleF(0f, 0f, area.Width, area.Height);
			SizeF r;

			// Add margins
			rect.X      += RenderOptions.LeftMargin;
			rect.Y      += RenderOptions.TopMargin;
			rect.Width  -= RenderOptions.LeftMargin + RenderOptions.RightMargin;
			rect.Height -= RenderOptions.TopMargin  + RenderOptions.BottomMargin;

			// Add space for tick marks
			if (YAxis.RenderOptions.DrawTickMarks)
			{
				rect.X      += YAxis.RenderOptions.TickLength;
				rect.Width  -= YAxis.RenderOptions.TickLength;
			}
			if (XAxis.RenderOptions.DrawTickMarks)
			{
				rect.Height -= XAxis.RenderOptions.TickLength;
			}

			// Add space for the title and axis labels
			if (Title.HasValue())
			{
				r = gfx.MeasureString(Title, RenderOptions.TitleFont);
				rect.Y      += r.Height;
				rect.Height -= r.Height;
			}
			if (XAxis.Label.HasValue())
			{
				r = gfx.MeasureString(XAxis.Label, XAxis.RenderOptions.LabelFont);
				rect.Height -= r.Height;
			}
			if (YAxis.Label.HasValue())
			{
				r = gfx.MeasureString(YAxis.Label, YAxis.RenderOptions.LabelFont);
				rect.X     += r.Height; // will be rotated by 90deg
				rect.Width -= r.Height;
			}

			// Add space for the tick labels
			const string lbl = "9.999";
			if (XAxis.RenderOptions.DrawTickLabels)
			{
				r = gfx.MeasureString(lbl, XAxis.RenderOptions.TickFont);
				rect.Height -= r.Height;
			}
			if (YAxis.RenderOptions.DrawTickLabels)
			{
				r = gfx.MeasureString(lbl, YAxis.RenderOptions.TickFont);
				rect.X     += r.Width;
				rect.Width -= r.Width;
			}

			return new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
		}

		/// <summary>Return the min, max, and step size for the x/y axes</summary>
		private void PlotGrid(Rectangle plot_area, out v2 min, out v2 max, out v2 step)
		{
			// Choose step sizes
			var max_ticks_x = plot_area.Width  / RenderOptions.PixelsPerTick.x;
			var max_ticks_y = plot_area.Height / RenderOptions.PixelsPerTick.y;
			var xspan = XAxis.Span;
			var yspan = YAxis.Span;
			var step_x = (float)Math.Pow(10.0, (int)Math.Log10(xspan)); step.x = step_x;
			var step_y = (float)Math.Pow(10.0, (int)Math.Log10(yspan)); step.y = step_y;
			foreach (var s in new[]{0.05f, 0.1f, 0.2f, 0.25f, 0.5f, 1.0f, 2.0f, 5.0f, 10.0f, 20.0f, 50.0f})
			{
				if (s * xspan / step_x <= max_ticks_x) step.x = step_x / s;
				if (s * yspan / step_y <= max_ticks_y) step.y = step_y / s;
			}

			min.x  = (float)((XAxis.Min - Math.IEEERemainder(XAxis.Min, step.x)) - XAxis.Min); if (min.x < 0.0) min.x += step.x;
			min.y  = (float)((YAxis.Min - Math.IEEERemainder(YAxis.Min, step.y)) - YAxis.Min); if (min.y < 0.0) min.y += step.y;
			max.x  = (float)XAxis.Span * 1.0001f;
			max.y  = (float)YAxis.Span * 1.0001f;

			// protect against increments smaller than can be represented by a float
			if (min.x + step.x == min.x)    step.x = (max.x - min.x) * 0.01f;
			if (min.y + step.y == min.y)    step.y = (max.y - min.y) * 0.01f;

			// protect against too many ticks along the axis
			if (max.x - min.x > step.x*100) step.x = (max.x - min.x) * 0.01f;
			if (max.y - min.y > step.y*100) step.y = (max.y - min.y) * 0.01f;
		}

		/// <summary>Render the basic graph, axes, title, labels, etc</summary>
		private void RenderGraphFrame(Graphics gfx, Rectangle area, Rectangle plot_area)
		{
			// This is not enforced in the axis.Min/Max accessors because it's useful
			// to be able to change the min/max independently of each other, set them
			// to float max etc. It's only invalid to render a graph with a negative range
			Debug.Assert(XAxis.Span > 0, "Negative x range");
			Debug.Assert(YAxis.Span > 0, "Negative y range");

			// Clear to the background colour
			gfx.Clear(RenderOptions.BkColour);

			// Draw the graph title and labels
			if (Title.HasValue())
			{
				using (var bsh = new SolidBrush(RenderOptions.TitleColour))
				{
					var r = gfx.MeasureString(Title, RenderOptions.TitleFont);
					var x = (float)(area.Width - r.Width) * 0.5f;
					var y = (float)(area.Top + RenderOptions.TopMargin);
					gfx.TranslateTransform(x, y);
					gfx.MultiplyTransform(RenderOptions.TitleTransform);
					gfx.DrawString(Title, RenderOptions.TitleFont, bsh, PointF.Empty);
					gfx.ResetTransform();
				}
			}
			if (XAxis.Label.HasValue())
			{
				using (var bsh = new SolidBrush(XAxis.RenderOptions.LabelColour))
				{
					var r = gfx.MeasureString(XAxis.Label, XAxis.RenderOptions.LabelFont);
					var x = (float)(area.Width - r.Width) * 0.5f;
					var y = (float)(area.Bottom - RenderOptions.BottomMargin - r.Height);
					gfx.TranslateTransform(x, y);
					gfx.MultiplyTransform(XAxis.RenderOptions.LabelTransform);
					gfx.DrawString(XAxis.Label, XAxis.RenderOptions.LabelFont, bsh, PointF.Empty);
					gfx.ResetTransform();
				}
			}
			if (YAxis.Label.HasValue())
			{
				using (var bsh = new SolidBrush(YAxis.RenderOptions.LabelColour))
				{
					var r = gfx.MeasureString(YAxis.Label, YAxis.RenderOptions.LabelFont);
					var x = (float)(area.Left + RenderOptions.LeftMargin);
					var y = (float)(area.Height + r.Width) * 0.5f;
					gfx.TranslateTransform(x, y);
					gfx.RotateTransform(-90.0f);
					gfx.MultiplyTransform(YAxis.RenderOptions.LabelTransform);
					gfx.DrawString(YAxis.Label, YAxis.RenderOptions.LabelFont, bsh, PointF.Empty);
					gfx.ResetTransform();
				}
			}

			// Draw the graph frame and background
			using (var bsh_bkgd = new SolidBrush(RenderOptions.BkColour))
			using (var bsh_axis = new SolidBrush(RenderOptions.AxisColour))
			using (var pen_axis = new Pen(RenderOptions.AxisColour, 0.0f))
			{
				// Background
				RenderPlotBkgd(gfx, plot_area);

				v2 min,max,step;
				PlotGrid(plot_area, out min, out max, out step);

				// Tick marks and labels
				using (var bsh_xtick = new SolidBrush(XAxis.RenderOptions.TickColour))
				using (var bsh_ytick = new SolidBrush(YAxis.RenderOptions.TickColour))
				{
					var lblx = (float)(plot_area.Left - YAxis.RenderOptions.TickLength - 1);
					var lbly = (float)(plot_area.Top + plot_area.Height + XAxis.RenderOptions.TickLength + 1);
					if (XAxis.RenderOptions.DrawTickLabels || XAxis.RenderOptions.DrawTickMarks)
					{
						for (float x = min.x; x < max.x; x += step.x)
						{
							var X = (int)(plot_area.Left + x * plot_area.Width / XAxis.Span);
							var s = XAxis.TickText(x + XAxis.Min);
							var r = gfx.MeasureString(s, XAxis.RenderOptions.TickFont);
							if (XAxis.RenderOptions.DrawTickLabels)
								gfx.DrawString(s, XAxis.RenderOptions.TickFont, bsh_xtick, new PointF(X - r.Width*0.5f, lbly));
							if (XAxis.RenderOptions.DrawTickMarks)
								gfx.DrawLine(pen_axis, X, plot_area.Top + plot_area.Height, X, plot_area.Top + plot_area.Height + XAxis.RenderOptions.TickLength);
						}
					}
					if (YAxis.RenderOptions.DrawTickLabels || YAxis.RenderOptions.DrawTickMarks)
					{
						for (float y = min.y; y < max.y; y += step.y)
						{
							var Y = (int)(plot_area.Top + plot_area.Height - y * plot_area.Height / YAxis.Span);
							var s = YAxis.TickText(y + YAxis.Min);
							var r = gfx.MeasureString(s, YAxis.RenderOptions.TickFont);
							if (YAxis.RenderOptions.DrawTickLabels)
								gfx.DrawString(s, YAxis.RenderOptions.TickFont, bsh_ytick, new PointF(lblx - r.Width, Y - r.Height*0.5f));
							if (YAxis.RenderOptions.DrawTickMarks)
								gfx.DrawLine(pen_axis, plot_area.Left - YAxis.RenderOptions.TickLength, Y, plot_area.Left, Y);
						}
					}
				}

				// Graph border
				gfx.DrawRectangle(pen_axis, plot_area);
			}

			// Control border
			switch (BorderStyle)
			{
			default: throw new NotSupportedException("Border style not supported");
			case BorderStyle.None: break;
			case BorderStyle.FixedSingle:
				{
					using (var pen_border = new Pen(Color.Black, 0.0f))
						gfx.DrawRectangle(pen_border, area.Inflated(0,0,-1,-1));
					break;
				}
			}
		}

		/// <summary>Render the plot background including gridlines</summary>
		private void RenderPlotBkgd(Graphics gfx, Rectangle plot_area)
		{
			using (var bsh_plot = new SolidBrush(RenderOptions.PlotBkColour))
			using (var pen_grid = new Pen(RenderOptions.GridColour, 0.0f))
			{
				v2 min,max,step;
				PlotGrid(plot_area, out min, out max, out step);

				// Background
				gfx.FillRectangle(bsh_plot, plot_area);

				// Grid lines
				for (float x = min.x; x < max.x; x += step.x)
				{
					var X = (int)(plot_area.Left + x * plot_area.Width / XAxis.Span);
					gfx.DrawLine(pen_grid, X, plot_area.Top, X, plot_area.Bottom);
				}
				for (float y = min.y; y < max.y; y += step.y)
				{
					var Y = (int)(plot_area.Bottom - y * plot_area.Height / YAxis.Span);
					gfx.DrawLine(pen_grid, plot_area.Left, Y, plot_area.Right, Y);
				}
			}
		}

		/// <summary>Render the series data into the graph (within 'area')</summary>
		private void RenderData(Graphics gfx, Rectangle plot_area)
		{
			var plot = plot_area.Shifted(1,1).Inflated(0, 0, -1,-1);
			gfx.SetClip(plot);

			RenderPlotBkgd(gfx, plot_area);

			// Set the transform so that we can draw directly in graph space.
			// Note: We can't use a scale transform here because the lines and
			// points will also be scaled, into elipses or caligraphy etc
			Matrix c2g; v2 scale;
			ClientToGraphSpace(plot_area, out c2g, out scale);
			gfx.Transform = c2g;

			// Plot each series
			foreach (var series in Data)
			{
				if (m_rdr_cancel)
					break;

				var opts = series.RenderOptions;
				if (!opts.Visible)
					continue;

				using (var bsh_pt   = new SolidBrush(opts.PointColour))
				using (var bsh_bar  = new SolidBrush(opts.BarColour))
				using (var bsh_err  = new SolidBrush(opts.ErrorBarColour))
				using (var pen_line = new Pen(opts.LineColour, opts.LineWidth))
				using (var pen_bar  = new Pen(opts.BarColour, 0.0f))
				{
					// Loop over data points
					var indices = series.Indices(XAxis.Min, XAxis.Max);
					for (var iter = indices.GetIterator(); !iter.AtEnd;)
					{
						if (m_rdr_cancel)
							break;

						// Get the next data point
						var pt = new ScreenPoint(series, plot_area, scale, iter);

						// Render the data point
						switch (opts.PlotType)
						{
						// Draw the data point
						case Series.RdrOptions.EPlotType.Point:
							PlotPoint(gfx, pt, opts, bsh_pt, bsh_err);
							break;

						// Draw the data point and connect with a line
						case Series.RdrOptions.EPlotType.Line:
							PlotLine(gfx, pt, opts, bsh_pt, pen_line, bsh_err);
							break;

						// Draw the data as columns in a bar graph
						case Series.RdrOptions.EPlotType.Bar:
							PlotBar(gfx, pt, opts, bsh_bar, pen_bar, bsh_err);
							break;
						}
					}

					// Add a moving average line
					if (opts.DrawMovingAvr)
					{
						int i0, i1;
						series.IndexRange(XAxis.Min, XAxis.Max, out i0, out i1);
						i0 = Math.Max(0           , i0 - opts.MAWindowSize);
						i1 = Math.Min(series.Count, i1 + opts.MAWindowSize);
						PlotMovingAverage(gfx, opts, scale, series.Values(i0,i1).GetIterator());
					}
				}
			}

			gfx.ResetTransform();

			// Allow clients to draw on the graph
			AddOverlayOnRender.Raise(this, new OverlaysEventArgs(gfx));

			gfx.ResetClip();
		}

		/// <summary>A helper for rendering that finds the bounds of all points at the same screen space X position</summary>
		private class ScreenPoint
		{
			public Series    m_series;    // The series that this point came from
			public Rectangle m_plot_area; // The screen space area that the point must be within
			public v2        m_scale;     // Graph space to screen space scaling factors
			public int    m_imin, m_imax; // The index range of the data points included
			public double m_xmin, m_xmax; // The range of data point X values
			public double m_ymin, m_ymax; // The range of data point Y values
			public double m_ylo , m_yhi ; // The bounds on the error bars of the Y values

			/// <summary>Scan 'i' forward to the next data point that has a screen space X value not equal to that of the i'th data point</summary>
			public ScreenPoint(Series series, Rectangle plot_area, v2 scale, Iterator<int> iter)
			{
				// Get the data point
				var gv = series[iter.Current];
				var sx = (int)(gv.x * scale.x);

				// Init members
				this.m_series    = series;
				this.m_plot_area = plot_area;
				this.m_scale     = scale;
				this.m_imin     = this.m_imax = iter.Current;
				this.m_xmin     = this.m_xmax = gv.x;
				this.m_ymin     = this.m_ymax = gv.y;
				this.m_ylo      = gv.y + gv.ylo;
				this.m_yhi      = gv.y + gv.yhi;

				// While the data point still represents the same X coordinate on-screen
				// scan forward until x != sx, finding the bounds on points that fall at this X
				for (iter.MoveNext(); !iter.AtEnd; iter.MoveNext())
				{
					gv = series[iter.Current];
					var x = (int)(gv.x * scale.x);
					if (x != sx) break;

					this.m_imax = iter.Current;
					this.m_xmax = gv.x;
					this.m_ymin = Math.Min(this.m_ymin , gv.y);
					this.m_ymax = Math.Max(this.m_ymax , gv.y);
					this.m_ylo  = Math.Min(this.m_ylo  , gv.y + gv.ylo);
					this.m_yhi  = Math.Max(this.m_yhi  , gv.y + gv.yhi);
				}
			}

			/// <summary>True if this is a single point, false if it represents multiple points</summary>
			public bool IsSingle { get { return m_imin == m_imax; } }

			// Error bars/Bar graph width in screen space
			public int lhs, rhs;
			public void CalcBarWidth(float width_scale = 1.0f)
			{
				// Calc the left and right side of the bar
				if (m_imin != 0)
				{
					var prev_x = m_series[m_imin - 1].x;
					lhs = (int)Math.Max(0, 0.5*(m_xmin - prev_x) * width_scale * m_scale.x);
				}
				if (m_imax+1 != m_series.Count)
				{
					var next_x = m_series[m_imax + 1].x;
					rhs = (int)Math.Max(1, 0.5*(next_x - m_xmax) * width_scale * m_scale.x);
				}
				if (lhs == 0) lhs = rhs; // i_min == 0 case
				if (rhs == 0) rhs = lhs; // i_max == Count-1 case
			}
		}

		/// <summary>Draw error bars. lhs/rhs are the screen space size of the bar</summary>
		private void PlotErrorBars(Graphics gfx, ScreenPoint pt, int lhs, int rhs, SolidBrush bsh_err)
		{
			var x   = (int)(pt.m_xmin * pt.m_scale.x);
			var ylo = (int)(pt.m_ylo * pt.m_scale.y);
			var yhi = (int)(pt.m_yhi * pt.m_scale.y);
			if (yhi - ylo > 0)
				gfx.FillRectangle(bsh_err, new Rectangle(x - lhs, ylo, lhs + rhs, yhi - ylo));
		}

		/// <summary>Plot a point on the graph</summary>
		private void PlotPoint(Graphics gfx, ScreenPoint pt, Series.RdrOptions opts, SolidBrush bsh_pt, SolidBrush bsh_err)
		{
			// Draw error bars is on
			if (opts.DrawErrorBars)
			{
				pt.CalcBarWidth();
				PlotErrorBars(gfx, pt, pt.lhs, pt.rhs, bsh_err);
			}

			// Plot the data point
			if (opts.DrawData)
			{
				var x = (int)(pt.m_xmin * pt.m_scale.x);
				var y = (int)(pt.m_ymin * pt.m_scale.y);
				var h = (int)((pt.m_ymax - pt.m_ymin) * pt.m_scale.y);
				gfx.FillEllipse(bsh_pt, new RectangleF(x - opts.PointSize*0.5f, y - opts.PointSize*0.5f, opts.PointSize, h + opts.PointSize));
			}
		}

		/// <summary>Plot a line segment on the graph</summary>
		private void PlotLine(Graphics gfx, ScreenPoint pt, Series.RdrOptions opts, SolidBrush bsh_pt, Pen pen_line, SolidBrush bsh_err)
		{
			// Draw error bars is on
			if (opts.DrawErrorBars)
			{
				pt.CalcBarWidth();
				PlotErrorBars(gfx, pt, pt.lhs, pt.rhs, bsh_err);
			}

			// Plot the point and line
			if (opts.DrawData)
			{
				// Draw the line from the previous point
				if (pt.m_imin != 0) // if this is not the first point
				{
					var px = (int)(pt.m_series[pt.m_imin - 1].x * pt.m_scale.x);
					var py = (int)(pt.m_series[pt.m_imin - 1].y * pt.m_scale.y);
					var x  = (int)(pt.m_series[pt.m_imin    ].x * pt.m_scale.x);
					var y  = (int)(pt.m_series[pt.m_imin    ].y * pt.m_scale.y);
					gfx.DrawLine(pen_line, px, py, x, y);
				}

				// Draw a vertical line if the screen point represents multiple points
				if (!pt.IsSingle)
				{
					var x   = (int)(pt.m_xmin * pt.m_scale.x);
					var ylo = (int)(pt.m_ymin * pt.m_scale.y);
					var yhi = (int)(pt.m_ymax * pt.m_scale.y);
					gfx.DrawLine(pen_line, x, ylo, x, yhi);
				}

				// Plot the point (if it's size is non-zero)
				if (opts.PointSize > 0)
				{
					var x = (int)(pt.m_xmin * pt.m_scale.x);
					var y = (int)(pt.m_ymin * pt.m_scale.y);
					var h = (int)((pt.m_ymax - pt.m_ymin) * pt.m_scale.y);
					gfx.FillEllipse(bsh_pt, new RectangleF(x - opts.PointSize*0.5f, y - opts.PointSize*0.5f, opts.PointSize, h + opts.PointSize));
				}
			}
		}

		/// <summary>Plot a single bar on the graph</summary>
		private void PlotBar(Graphics gfx, ScreenPoint pt, Series.RdrOptions opts, SolidBrush bsh_bar, Pen pen_bar, SolidBrush bsh_err)
		{
			// Calc the left and right side of the bar
			pt.CalcBarWidth(opts.BarWidth);

			// Draw error bars is on
			if (opts.DrawErrorBars)
				PlotErrorBars(gfx, pt, pt.lhs, pt.rhs, bsh_err);

			// Plot the bar
			if (opts.DrawData)
			{
				var x   = Maths.Clamp((int)( pt.m_xmin                       * pt.m_scale.x), pt.m_plot_area.Left, pt.m_plot_area.Right);
				var ylo = Maths.Clamp((int)((pt.m_ymin > 0 ? 0.0 : pt.m_ymin) * pt.m_scale.y), pt.m_plot_area.Top, pt.m_plot_area.Bottom);
				var yhi = Maths.Clamp((int)((pt.m_ymax < 0 ? 0.0 : pt.m_ymax) * pt.m_scale.y), pt.m_plot_area.Top, pt.m_plot_area.Bottom);
				
				if (yhi - ylo > 0)
					gfx.FillRectangle(bsh_bar, new Rectangle(x - pt.lhs, ylo, Math.Max(1, pt.lhs + pt.rhs), yhi - ylo));
				else 
					gfx.DrawLine(pen_bar, x - pt.lhs, 0f, pt.lhs + pt.rhs, 0f);
			}
		}

		/// <summary>Plot a moving average curve over the data</summary>
		private void PlotMovingAverage(Graphics gfx, Series.RdrOptions opts, v2 scale, Iterator<GraphValue> iter)
		{
			var max = new ExpMovingAvr((uint)opts.MAWindowSize);
			var may = new ExpMovingAvr((uint)opts.MAWindowSize);
			using (var ma_pen = new Pen(opts.MALineColour, opts.MALineWidth))
			{
				bool first = true;
				int px = 0, py = 0;
				for (;!iter.AtEnd; iter.MoveNext())
				{
					var gv = iter.Current;
					max.Add(gv.x);
					may.Add(gv.y);

					int x = (int)(max.Mean * scale.x);
					int y = (int)(may.Mean * scale.y);
					if (first)
					{
						first = false;
						px = x;
						py = y;
					}
					else if (x != px)
					{
						gfx.DrawLine(ma_pen, px, py, x, y);
						px = x;
						py = y;
					}
				}
			}
		}
		#endregion

		#region Show Value Tooltip

		/// <summary>A tool tip to display the mouse location value</summary>
		private ToolTip m_tooltip;

		/// <summary>Handle mouse move events while the tooltip is visible</summary>
		private void OnMouseMoveTooltip(object sender, MouseEventArgs e)
		{
			if (m_plot_area.Contains(e.Location))
			{
				var pt = PointToGraph(e.Location);
				var old_str = m_tooltip.GetToolTip(this);
				var new_str = pt.ToString();
				if (old_str != new_str) // Fix a flickering problem for non-moving mouse
					m_tooltip.Show(new_str, this, e.Location + new Size(20, 0));
			}
			else
				m_tooltip.Hide(this);
		}

		#endregion

		#region Context Menu

		/// <summary>Create and display a context menu</summary>
		public void ShowContextMenu(Point location)
		{
			// Note: using lists and AddRange here for performance reasons
			// Using 'DropDownItems.Add' causes lots of PerformLayout calls

			var context_menu = new ContextMenuStrip { Renderer = new ContextMenuRenderer() };
			var lvl0 = new List<ToolStripItem>();

			using (this.ChangeCursor(Cursors.WaitCursor))
			using (context_menu.SuspendLayout(false))
			{
				// Allow users to add menu options
				AddUserMenuOptions.Raise(this, new AddUserMenuOptionsEventArgs(context_menu));

				#region Show Value
				{
					var show_value = lvl0.Add2(new ToolStripMenuItem("Show Value"));
					show_value.Checked = (bool)m_tooltip.Tag;
					show_value.Click += (s,a) =>
						{
							m_tooltip.Hide(this);
							if ((bool)m_tooltip.Tag) MouseMove -= OnMouseMoveTooltip;
							m_tooltip.Tag = !(bool)m_tooltip.Tag;
							if ((bool)m_tooltip.Tag) MouseMove += OnMouseMoveTooltip;
						};
				}
				#endregion

				#region Zoom Menu
				{
					var zoom_menu = lvl0.Add2(new ToolStripMenuItem("Zoom"));
					var lvl1 = new List<ToolStripItem>();

					var default_zoom = lvl1.Add2(new ToolStripMenuItem("Default"));
					default_zoom.Click += (s,a) =>
						{
							FindDefaultRange();
							ResetToDefaultRange();
							Refresh();
						};

					var zoom_in = lvl1.Add2(new ToolStripMenuItem("In"));
					zoom_in.Click += (s,a) =>
						{
							var point = PointToGraph(m_selection.Location);
							Zoom *= 0.5;
							PositionGraph(m_selection.Location,point);
							Refresh();
						};

					var zoom_out = lvl1.Add2(new ToolStripMenuItem("Out"));
					zoom_out.Click += (s,a) =>
						{
							var point = PointToGraph(m_selection.Location);
							Zoom *= 2.0;
							PositionGraph(m_selection.Location,point);
							Refresh();
						};

					zoom_menu.DropDownItems.AddRange(lvl1.ToArray());
				}
				#endregion

				#region Series menus
				if (Data.Count != 0)
				{
					#region All Series
					{
						var series_menu = lvl0.Add2(new ToolStripMenuItem("Series: All"));
						var lvl1 = new List<ToolStripItem>();

						#region Visible
						{
							var state = Data.Aggregate(0, (x,s) => x | (s.RenderOptions.Visible ? 2 : 1));
							var option = lvl1.Add2(new ToolStripMenuItem("Visible"));
							option.CheckState = state == 2 ? CheckState.Checked : state == 1 ? CheckState.Unchecked : CheckState.Indeterminate;
							option.Click += (s,a) =>
								{
									option.CheckState = option.CheckState == CheckState.Checked ? CheckState.Unchecked : CheckState.Checked;
									foreach (var x in Data) x.RenderOptions.Visible = option.CheckState == CheckState.Checked;
									Dirty = true;
								};
						}
						#endregion

						series_menu.DropDownItems.AddRange(lvl1.ToArray());
					}
					#endregion
					#region Individual Series
					for (var index = 0; index != Data.Count; ++index)
					{
						var series = Data[index];

						var series_menu = lvl0.Add2(new ToolStripMenuItem("Series: " + series.Name));
						var lvl1 = new List<ToolStripItem>();

						if      (series.RenderOptions.PlotType == Series.RdrOptions.EPlotType.Point) series_menu.ForeColor = series.RenderOptions.PointColour;
						else if (series.RenderOptions.PlotType == Series.RdrOptions.EPlotType.Line ) series_menu.ForeColor = series.RenderOptions.LineColour;
						else if (series.RenderOptions.PlotType == Series.RdrOptions.EPlotType.Bar  ) series_menu.ForeColor = series.RenderOptions.BarColour;
						series_menu.Checked = series.RenderOptions.Visible;
						series_menu.Tag = index;
						series_menu.Click += (s,a) =>
							{
								series.RenderOptions.Visible = !series.RenderOptions.Visible;
								series_menu.Checked = series.RenderOptions.Visible;
								Dirty = true;
							};

						#region Elements
						{
							var elements_menu = lvl1.Add2(new ToolStripMenuItem("Elements"));
							var lvl2 = new List<ToolStripItem>();

							#region Draw main data
							{
								var option = lvl2.Add2(new ToolStripMenuItem("Series data"));
								option.Checked = series.RenderOptions.DrawData;
								option.Click += (s,a) =>
									{
										series.RenderOptions.DrawData = !series.RenderOptions.DrawData;
										Dirty = true;
									};
							}
							#endregion
							#region Draw error bars
							{
								var option = lvl2.Add2(new ToolStripMenuItem("Error Bars"));
								option.Checked = series.RenderOptions.DrawErrorBars;
								option.Click += (s,a) =>
									{
										series.RenderOptions.DrawErrorBars = !series.RenderOptions.DrawErrorBars;
										Dirty = true;
									};
							}
							#endregion

							elements_menu.DropDownItems.AddRange(lvl2.ToArray());
						}
						#endregion

						#region Plot Type Menu
						{
							var plot_type_menu = lvl1.Add2(new ToolStripMenuItem("Plot Type"));
							{
								var option = new ToolStripComboBox();
								plot_type_menu.DropDownItems.Add(option);
								option.Items.AddRange(Enum<Series.RdrOptions.EPlotType>.Names.Cast<object>().ToArray());
								option.SelectedIndex = (int)series.RenderOptions.PlotType;
								option.SelectedIndexChanged += (s,a) =>
									{
										series.RenderOptions.PlotType = (Series.RdrOptions.EPlotType)option.SelectedIndex;
										Dirty = true;
									};
								option.KeyDown += (s,a) =>
									{
										if (a.KeyCode == Keys.Return)
											context_menu.Close();
									};
							}
						}
						#endregion

						#region Appearance Menu
						{
							var appearance_menu = lvl1.Add2(new ToolStripMenuItem("Appearance"));
							var lvl2 = new List<ToolStripItem>();

							#region Points
							if (series.RenderOptions.PlotType == Series.RdrOptions.EPlotType.Point || series.RenderOptions.PlotType == Series.RdrOptions.EPlotType.Line)
							{
								var point_menu = lvl2.Add2(new ToolStripMenuItem("Points"));
								var lvl3 = new List<ToolStripItem>();

								#region Size
								{
									var size_menu = lvl3.Add2(new ToolStripMenuItem("Size"));
									{
										var option = new ToolStripTextBox{AcceptsReturn = false};
										size_menu.DropDownItems.Add(option);
										option.Text = series.RenderOptions.PointSize.ToString("0.00");
										option.TextChanged += (s,a) =>
											{
												float size;
												if (!float.TryParse(option.Text,out size)) return;
												series.RenderOptions.PointSize = size;
												Dirty = true;
											};
									}
								}
								#endregion
								#region Colour
								{
									var colour_menu = lvl3.Add2(new ToolStripMenuItem("Colour"));
									{
										var option = new NoHighlightToolStripMenuItem("     "){DisplayStyle = ToolStripItemDisplayStyle.Text, BackColor = series.RenderOptions.PointColour};
										colour_menu.DropDownItems.Add(option);
										option.Click += (s,a) =>
											{
												var cd = new ColourUI{InitialColour = option.BackColor};
												if (cd.ShowDialog() != DialogResult.OK) return;
												series.RenderOptions.PointColour = cd.Colour;
												Dirty = true;
											};
									}
								}
								#endregion

								point_menu.DropDownItems.AddRange(lvl3.ToArray());
							}
							#endregion
							#region Lines
							if (series.RenderOptions.PlotType == Series.RdrOptions.EPlotType.Line)
							{
								var line_menu = lvl2.Add2(new ToolStripMenuItem("Lines"));
								var lvl3 = new List<ToolStripItem>();

								#region Width
								{
									var width_menu = lvl3.Add2(new ToolStripMenuItem("Width"));
									{
										var option = new ToolStripTextBox();
										width_menu.DropDownItems.Add(option);
										option.Text = series.RenderOptions.LineWidth.ToString("0.00");
										option.TextChanged += (s,a) =>
											{
												float width;
												if (!float.TryParse(option.Text,out width)) return;
												series.RenderOptions.LineWidth = width;
												Dirty = true;
											};
									}
								}
								#endregion
								#region Colour
								{
									var colour_menu = lvl3.Add2(new ToolStripMenuItem("Colour"));
									{
										var option = new NoHighlightToolStripMenuItem("     "){DisplayStyle = ToolStripItemDisplayStyle.Text, BackColor = series.RenderOptions.LineColour};
										colour_menu.DropDownItems.Add(option);
										option.Click += (s,a) =>
											{
												var cd = new ColourUI{InitialColour = option.BackColor};
												if (cd.ShowDialog() != DialogResult.OK) return;
												series.RenderOptions.LineColour = cd.Colour;
												Dirty = true;
											};
									}
								}
								#endregion

								line_menu.DropDownItems.AddRange(lvl3.ToArray());
							}
							#endregion
							#region Bars
							if (series.RenderOptions.PlotType == Series.RdrOptions.EPlotType.Bar)
							{
								var bar_menu = lvl2.Add2(new ToolStripMenuItem("Bars"));
								var lvl3 = new List<ToolStripItem>();

								#region Width
								{
									var width_menu = lvl3.Add2(new ToolStripMenuItem("Width"));
									{
										var option = new ToolStripTextBox();
										width_menu.DropDownItems.Add(option);
										option.Text = series.RenderOptions.BarWidth.ToString("0.00");
										option.TextChanged += (s,a) =>
											{
												float width;
												if (!float.TryParse(option.Text,out width)) return;
												series.RenderOptions.BarWidth = width;
												Dirty = true;
											};
										option.KeyDown += (s,a) =>
											{
												if (a.KeyCode == Keys.Return)
													context_menu.Close();
											};
									}
								}
								#endregion
								#region Bar Colour
								{
									var colour_menu = lvl3.Add2(new ToolStripMenuItem("Colour"));
									{
										var option = new NoHighlightToolStripMenuItem("     "){DisplayStyle = ToolStripItemDisplayStyle.Text, BackColor = series.RenderOptions.BarColour};
										colour_menu.DropDownItems.Add(option);
										option.Click += (s,a) =>
											{
												var cd = new ColourUI{InitialColour = option.BackColor};
												if (cd.ShowDialog() != DialogResult.OK) return;
												series.RenderOptions.BarColour = cd.Colour;
												Dirty = true;
											};
									}
								}
								#endregion

								bar_menu.DropDownItems.AddRange(lvl3.ToArray());
							}
							#endregion
							#region Error Bars
							if (series.RenderOptions.DrawErrorBars)
							{
								var errorbar_menu = lvl2.Add2(new ToolStripMenuItem("Error Bars"));
								var lvl3 = new List<ToolStripItem>();

								#region Colour
								{
									var colour_menu = lvl3.Add2(new ToolStripMenuItem("Colour"));
									{
										var option = new NoHighlightToolStripMenuItem("     "){DisplayStyle = ToolStripItemDisplayStyle.Text, BackColor = series.RenderOptions.ErrorBarColour};
										colour_menu.DropDownItems.Add(option);
										option.Click += (s,a) =>
											{
												var cd = new ColourUI{InitialColour = option.BackColor};
												if (cd.ShowDialog() != DialogResult.OK) return;
												series.RenderOptions.ErrorBarColour = cd.Colour;
												Dirty = true;
											};
									}
								}
								#endregion

								errorbar_menu.DropDownItems.AddRange(lvl3.ToArray());
							}
							#endregion

							appearance_menu.DropDownItems.AddRange(lvl2.ToArray());
						}
						#endregion

						#region Moving Average
						{
							var mv_avr_menu = lvl1.Add2(new ToolStripMenuItem("Moving Average"));
							#region Draw Moving Average
							{
								var option = new ToolStripMenuItem("Draw Moving Average");
								mv_avr_menu.DropDownItems.Add(option);
								option.Checked = series.RenderOptions.DrawMovingAvr;
								option.Click += (s,a) =>
									{
										series.RenderOptions.DrawMovingAvr = !series.RenderOptions.DrawMovingAvr;
										Dirty = true;
									};
							}
							#endregion
							#region Window Size
							{
								var win_size_menu = new ToolStripMenuItem("Window Size");
								mv_avr_menu.DropDownItems.Add(win_size_menu);
								{
									var option = new ToolStripTextBox();
									win_size_menu.DropDownItems.Add(option);
									option.Text = series.RenderOptions.MAWindowSize.ToString(CultureInfo.InvariantCulture);
									option.TextChanged += (s,a) =>
										{
											int size;
											if (!int.TryParse(option.Text,out size)) return;
											series.RenderOptions.MAWindowSize = size;
											Dirty = true;
										};
								}
							}
							#endregion
							#region Line Width
							{
								var line_width_menu = new ToolStripMenuItem("Line Width");
								mv_avr_menu.DropDownItems.Add(line_width_menu);
								{
									var option = new ToolStripTextBox();
									line_width_menu.DropDownItems.Add(option);
									option.Text = series.RenderOptions.MALineWidth.ToString(CultureInfo.InvariantCulture);
									option.TextChanged += (s,a) =>
										{
											int size;
											if (!int.TryParse(option.Text,out size)) return;
											series.RenderOptions.MALineWidth = size;
											Dirty = true;
										};
								}
							}
							#endregion
							#region Line Colour
							{
								var line_colour_menu = new ToolStripMenuItem("Line Colour");
								mv_avr_menu.DropDownItems.Add(line_colour_menu);
								{
									var option = new NoHighlightToolStripMenuItem("     "){DisplayStyle = ToolStripItemDisplayStyle.Text, BackColor = series.RenderOptions.MALineColour};
									line_colour_menu.DropDownItems.Add(option);
									option.Click += (s,a) =>
										{
											var cd = new ColourUI{InitialColour = option.BackColor};
											if (cd.ShowDialog() != DialogResult.OK) return;
											series.RenderOptions.MALineColour = cd.Colour;
											Dirty = true;
										};
								}
							}
							#endregion
						}
						#endregion

						#region Delete Series
						if (series.AllowDelete)
						{
							var delete_menu = lvl1.Add2(new ToolStripMenuItem("Delete"));
							delete_menu.Click += (s,a) =>
								{
									if (MessageBox.Show(this,"Confirm delete?","Delete Series",MessageBoxButtons.YesNo,MessageBoxIcon.Warning) != DialogResult.Yes) return;
									Data.RemoveAt((int)series_menu.Tag);
									FindDefaultRange();
									Dirty = true;
								};
						}
						#endregion

						series_menu.DropDownItems.AddRange(lvl1.ToArray());
					}
					#endregion
				}
				#endregion

				#region Rendering Options
				{
					var appearance_menu = lvl0.Add2(new ToolStripMenuItem("Appearance"));
					var lvl1 = new List<ToolStripItem>();

					#region Frame Background Colour
					{
						var item = lvl1.Add2(new ToolStripMenuItem("Frame Background Colour"));
						{
							var option = new NoHighlightToolStripMenuItem("     "){DisplayStyle = ToolStripItemDisplayStyle.Text, BackColor = RenderOptions.BkColour};
							item.DropDownItems.Add(option);
							option.Click += (s,a) =>
								{
									var cd = new ColourUI{InitialColour = option.BackColor};
									if (cd.ShowDialog() != DialogResult.OK) return;
									RenderOptions.BkColour = cd.Colour;
									Dirty = true;
								};
						}
					}
					#endregion
					#region Plot Background Colour
					{
						var item = lvl1.Add2(new ToolStripMenuItem("Plot Background Colour"));
						{
							var option = new NoHighlightToolStripMenuItem("     "){DisplayStyle = ToolStripItemDisplayStyle.Text, BackColor = RenderOptions.BkColour};
							item.DropDownItems.Add(option);
							option.Click += (s,a) =>
								{
									var cd = new ColourUI{InitialColour = option.BackColor};
									if (cd.ShowDialog() != DialogResult.OK) return;
									RenderOptions.PlotBkColour = cd.Colour;
									Dirty = true;
								};
						}
					}
					#endregion
					#region Opacity
					if (ParentForm != null)
					{
						var item = lvl1.Add2(new ToolStripMenuItem("Opacity"));
						{
							var option = new ToolStripTextBox();
							item.DropDownItems.Add(option);
							option.Text = ParentForm.Opacity.ToString("0.00");
							option.TextChanged += (s,a) =>
								{
									double opc;
									if (ParentForm != null && double.TryParse(option.Text,out opc))
										ParentForm.Opacity = Math.Max(0.1,Math.Min(1.0,opc));
								};
						}
					}
					#endregion

					appearance_menu.DropDownItems.AddRange(lvl1.ToArray());
				}
				#endregion

				#region Axis Options
				{
					var axes_menu = lvl0.Add2(new ToolStripMenuItem("Axes"));
					var lvl1 = new List<ToolStripItem>();

					#region X Axis
					{
						var x_axis_menu = lvl1.Add2(new ToolStripMenuItem("X Axis"));
						var lvl2 = new List<ToolStripItem>();

						#region Min
						{
							var min_menu = lvl2.Add2(new ToolStripMenuItem("Min"));
							{
								var option = new ToolStripTextBox();
								min_menu.DropDownItems.Add(option);
								option.Text = XAxis.Min.ToString("G5");
								option.TextChanged += (s,a) =>
									{
										float v;
										if (!float.TryParse(option.Text,out v)) return;
										XAxis.Min = v;
										Dirty = true;
									};
							}
						}
						#endregion
						#region Max
						{
							var max_menu = lvl2.Add2(new ToolStripMenuItem("Max"));
							{
								var option = new ToolStripTextBox();
								max_menu.DropDownItems.Add(option);
								option.Text = XAxis.Max.ToString("G5");
								option.TextChanged += (s,a) =>
									{
										float v;
										if (!float.TryParse(option.Text,out v)) return;
										XAxis.Max = v;
										Dirty = true;
									};
							}
						}
						#endregion
						#region Allow Scroll
						{
							var allow_scroll = lvl2.Add2(new ToolStripMenuItem { Text = "Allow Scroll", Checked = XAxis.AllowScroll });
							allow_scroll.Click += (s,a) => XAxis.AllowScroll = !XAxis.AllowScroll;
						}
						#endregion
						#region Allow Zoom
						{
							var allow_zoom = lvl2.Add2(new ToolStripMenuItem { Text = "Allow Zoom", Checked = XAxis.AllowZoom });
							allow_zoom.Click += (s,e) => XAxis.AllowZoom = !XAxis.AllowZoom;
						}
						#endregion

						x_axis_menu.DropDownItems.AddRange(lvl2.ToArray());
					}
					#endregion
					#region Y Axis
					{
						var y_axis_menu = lvl1.Add2(new ToolStripMenuItem("Y Axis"));
						var lvl2 = new List<ToolStripItem>();

						#region Min
						{
							var min_menu = lvl2.Add2(new ToolStripMenuItem("Min"));
							{
								var option = new ToolStripTextBox();
								min_menu.DropDownItems.Add(option);
								option.Text = YAxis.Min.ToString("G5");
								option.TextChanged += (s,a) =>
									{
										float v;
										if (!float.TryParse(option.Text,out v)) return;
										YAxis.Min = v;
										Dirty = true;
									};
							}
						}
						#endregion
						#region Max
						{
							var max_menu = lvl2.Add2(new ToolStripMenuItem("Max"));
							{
								var option = new ToolStripTextBox();
								max_menu.DropDownItems.Add(option);
								option.Text = YAxis.Max.ToString("G5");
								option.TextChanged += (s,a) =>
									{
										float v;
										if (!float.TryParse(option.Text,out v)) return;
										YAxis.Max = v;
										Dirty = true;
									};
							}
						}
						#endregion
						#region Allow Scroll
						{
							var allow_scroll = lvl2.Add2(new ToolStripMenuItem { Text = "Allow Scroll", Checked = YAxis.AllowScroll });
							allow_scroll.Click += (s,e) => YAxis.AllowScroll = !YAxis.AllowScroll;
						}
						#endregion
						#region Allow Zoom
						{
							var allow_zoom = lvl2.Add2(new ToolStripMenuItem { Text = "Allow Zoom", Checked = YAxis.AllowZoom });
							allow_zoom.Click += (s,e) => YAxis.AllowZoom = !YAxis.AllowZoom;
						}
						#endregion

						y_axis_menu.DropDownItems.AddRange(lvl2.ToArray());
					}
					#endregion

					axes_menu.DropDownItems.AddRange(lvl1.ToArray());
				}
				#endregion

				#region Notes
				{
					var note_menu = lvl0.Add2(new ToolStripMenuItem("Notes"));
					var lvl1 = new List<ToolStripItem>();

					#region Add
					{
						var option = lvl1.Add2(new ToolStripMenuItem("Add"));
						option.Click += (s,e) =>
							{
								var form = new Form
									{
										Text = "Add Note",
										FormBorderStyle = FormBorderStyle.SizableToolWindow,
										StartPosition = FormStartPosition.Manual,
										Location = PointToScreen(location),
										Size = new Size(160,100),
										KeyPreview = true
									};
								form.KeyDown += (o,a) =>
									{
										if (a.KeyCode == Keys.Enter && (a.Modifiers & Keys.Control) == 0)
											form.DialogResult = DialogResult.OK;
									};
								var tb = new TextBox
									{
										Dock = DockStyle.Fill,
										Multiline = true,
										AcceptsReturn = false
									};
								form.Controls.Add(tb);
								if (form.ShowDialog(this) != DialogResult.OK || tb.Text.Length == 0) return;
								Notes.Add(new Note(tb.Text,PointToGraph(location)));
								Dirty = true;
							};
					}
					#endregion
					#region Delete
					{
						var dist = 100f;
						Note nearest = null;
						foreach (var note in Notes)
						{
							var sep = GraphToPoint(note.m_loc) - (Size)location;
							var d = (float)Math.Sqrt(sep.X*sep.X + sep.Y*sep.Y);
							if (d >= dist) continue;
							dist = d;
							nearest = note;
						}
						if (nearest != null)
						{
							var option = lvl1.Add2(new ToolStripMenuItem("Delete '{0}'".Fmt(nearest.m_msg.Summary(12))));
							option.Click += (s,e) =>
								{
									Notes.Remove(nearest);
									Dirty = true;
								};
						}
					}
					#endregion

					note_menu.DropDownItems.AddRange(lvl1.ToArray());
				}
				#endregion

				#region Export
				{
					var export_menu = lvl0.Add2(new ToolStripMenuItem("Export"));
					export_menu.Click += (s,a) => ExportCSV();
				}
				#endregion

				#region Import
				{
					var import_menu = lvl0.Add2(new ToolStripMenuItem("Import"));
					import_menu.Click += (s,a) => ImportCSV(null);
				}
				#endregion

				context_menu.Items.AddRange(lvl0.ToArray());
			}

			context_menu.Closed += (s,a) => Refresh();
			context_menu.Show(this, location);
		}

		/// <summary>Special menu item that doesn't draw highlighted</summary>
		private class NoHighlightToolStripMenuItem :ToolStripMenuItem
		{
			public NoHighlightToolStripMenuItem(string text) :base(text) {}
		}

		/// <summary>Custom button renderer because the office 'checked' state buttons look crap</summary>
		private class ContextMenuRenderer :ToolStripProfessionalRenderer
		{
			protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
			{
				var item = e.Item as NoHighlightToolStripMenuItem;
				if (item == null) { base.OnRenderMenuItemBackground(e); return; }

				e.Graphics.FillRectangle(new SolidBrush(item.BackColor), e.Item.ContentRectangle);
				e.Graphics.DrawRectangle(Pens.Black, e.Item.ContentRectangle);
			}
		}

		#endregion

		#region Events

		/// <summary>
		/// Called during rendering of the graph to allow clients to add graphics to the cached bitmap.<para/>
		/// Rendering is in screen space, use GraphToPoint()/PointToGraph()<para/>
		/// Use ClientToGraphSpace() to get the transform and scale that allows drawing in graph space</summary>
		public event EventHandler<OverlaysEventArgs> AddOverlayOnRender;

		/// <summary>
		/// Called each time the cached plot bitmap is drawn to the control to allow clients to add graphics.<para/>
		/// The overlays can change without affecting cached graph bitmap.<para/>
		/// Rendering is in screen space, use GraphToPoint()/PointToGraph()<para/>
		/// Use ClientToGraphSpace() to get the transform and scale that allows drawing in graph space</summary>
		public event EventHandler<OverlaysEventArgs> AddOverlayOnPaint;

		/// <summary>Event allowing callers to add options to the context menu</summary>
		public event EventHandler<AddUserMenuOptionsEventArgs> AddUserMenuOptions;

		public class OverlaysEventArgs :EventArgs
		{
			/// <summary>The graphics interface to use for drawing</summary>
			public Graphics Gfx { get; private set; }

			public OverlaysEventArgs(Graphics gfx)
			{
				Gfx = gfx;
			}
		}
		public class AddUserMenuOptionsEventArgs :EventArgs
		{
			/// <summary>The menu to add menu items to</summary>
			public ContextMenuStrip Menu { get; private set; }

			public AddUserMenuOptionsEventArgs(ContextMenuStrip menu)
			{
				Menu = menu;
			}
		}

		#endregion

		#region Import/Export CSV

		// Dialog for exporting data
		private class ExportForm :Form
		{
			private readonly Label m_lbl_export_path;
			public readonly TextBox m_edit_export_path;
			private readonly Button m_btn_browse;
			private readonly Label m_lbl_series_to_export;
			public readonly CheckedListBox m_list_series_list;
			private readonly Button m_btn_ok;
			private readonly Button m_btn_cancel;

			public ExportForm()
			{
				SuspendLayout();

				// m_lbl_export_path
				m_lbl_export_path = new Label
				{
					AutoSize = true,
					Location = new Point(12,9),
					Name = "m_lbl_export_path",
					Size = new Size(65,13),
					TabIndex = 0,
					Text = "Export Path:"
				};

				// m_edit_export_path
				m_edit_export_path = new TextBox
				{
					Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
					Location = new Point(12,25),
					Name = "m_edit_export_path",
					Size = new Size(143,20),
					TabIndex = 1
				};

				// m_btn_browse
				m_btn_browse = new Button
				{
					Anchor = AnchorStyles.Top | AnchorStyles.Right,
					Location = new Point(159,23),
					Name = "m_btn_browse",
					Size = new Size(75,23),
					TabIndex = 2,
					Text = "Browse...",
					UseVisualStyleBackColor = true
				};
				m_btn_browse.Click += delegate
				{
					var fd = new SaveFileDialog { Filter = "CSV file (*.csv)|*.csv" };
					if (fd.ShowDialog() != DialogResult.OK) return;
					m_edit_export_path.Text = fd.FileName;
				};

				// m_lbl_series_to_export
				m_lbl_series_to_export = new Label
				{
					AutoSize = true,
					Location = new Point(9,48),
					Name = "m_lbl_series_to_export",
					Size = new Size(83,13),
					TabIndex = 3,
					Text = "Series to export:"
				};

				// m_list_series_list
				m_list_series_list = new CheckedListBox
				{
					Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
					FormattingEnabled = true,
					Location = new Point(12,64),
					Name = "m_list_series_list",
					Size = new Size(227,79),
					TabIndex = 4
				};

				// m_btn_ok
				m_btn_ok = new Button
				{
					Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
					DialogResult = DialogResult.OK,
					Name = "m_btn_ok",
					Location = new Point(83,151),
					Size = new Size(75,23),
					TabIndex = 7,
					Text = "OK",
					UseVisualStyleBackColor = true
				};

				// m_btn_cancel
				m_btn_cancel = new Button
				{
					Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
					DialogResult = DialogResult.Cancel,
					Name = "m_btn_cancel",
					Location = new Point(164,151),
					Size = new Size(75,23),
					TabIndex = 8,
					Text = "Cancel",
					UseVisualStyleBackColor = true
				};

				// Form
				AutoScaleDimensions = new SizeF(6F, 13F);
				AutoScaleMode = AutoScaleMode.Font;
				ClientSize = new Size(251, 186);
				Controls.Add(m_btn_cancel);
				Controls.Add(m_btn_ok);
				Controls.Add(m_lbl_series_to_export);
				Controls.Add(m_list_series_list);
				Controls.Add(m_btn_browse);
				Controls.Add(m_edit_export_path);
				Controls.Add(m_lbl_export_path);
				FormBorderStyle = FormBorderStyle.FixedDialog;
				AcceptButton = m_btn_ok;
				Name = "ExportForm";
				Text = "Export to CSV:";
				ResumeLayout(false);
				PerformLayout();
			}
		}

		/// <summary>Occurs if an error occurs while exporting CSV data</summary>
		public Action<Exception> ExportCSVError;

		/// <summary>Occurs if an error occurs while importing CSV data</summary>
		public Action<Exception> ImportCSVError;

		/// <summary>Export the graph data to an CSV file</summary>
		private void ExportCSV()
		{
			var exp = new ExportForm();
			foreach (var series in Data)
			{
				exp.m_list_series_list.Items.Add(series.Name, true);
			}
			if (exp.ShowDialog() != DialogResult.OK) return;

			// Write the file
			try
			{
				using (var file = new StreamWriter(exp.m_edit_export_path.Text))
				{
					file.WriteLine(Title);

					for (var i = 0; i != Data.Count; ++i)
					{
						if (exp.m_list_series_list.GetItemCheckState(i) != CheckState.Checked)
							continue;

						file.WriteLine(Data[i].Name);
						file.WriteLine(XAxis.Label+","+YAxis.Label+",Lower Bound,Upper Bound");
						foreach (var gv in Data[i].Values())
							file.WriteLine(gv.x+","+gv.y+","+gv.ylo+","+gv.yhi);
					}
				}
			}
			catch (Exception ex)
			{
				if (ExportCSVError != null) ExportCSVError(ex);
				else MessageBox.Show(this, "Export failed.\nReason: " + ex.Message, "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		/// <summary>Import graph data from a CSV file</summary>
		private void ImportCSV(string[] filenames)
		{
			if (filenames == null)
			{
				var fd = new OpenFileDialog { Filter = "CSV file (*.csv)|*.csv", Multiselect = true };
				if (fd.ShowDialog() != DialogResult.OK) return;
				filenames = fd.FileNames;
			}
			foreach (var filename in filenames)
			{
				try
				{
					using (TextReader file = new StreamReader(filename))
					{
						var series = new Series();
						Action<Series> AddSeries = s =>
						{
							if (s.Count == 0) return;
							Data.Add(s);
						};

						// Read a line from the file,
						for (var line = file.ReadLine(); line != null; line = file.ReadLine())
						{
							// how many tokens can it be split into?
							var tokens = line.Split(',', '\n');

							// 1->a title/name, the start of another series
							if (tokens.Length == 1)
							{
								AddSeries(series);
								series = new Series { Name = tokens[0], AllowDelete = true };
							}

							// 4-> data
							else if (tokens.Length == 4)
							{
								double v0, v1, v2, v3;
								if (double.TryParse(tokens[0], out v0) &&
									double.TryParse(tokens[1], out v1) &&
									double.TryParse(tokens[2], out v2) &&
									double.TryParse(tokens[3], out v3))
									series.Add(new GraphValue(v0, v1, v2, v3, null));
							}
						}
						AddSeries(series);
					}
				}
				catch (Exception ex)
				{
					if (ImportCSVError != null) ImportCSVError(ex);
					else MessageBox.Show(this, "Import failed for '" + filename + "'.\nReason: " + ex.Message, "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
			}
			FindDefaultRange();
		}

		#endregion
	}
}

/*
			//try
			//{
			//	// If the local bitmap does not exist, or its size is wrong, redraw it
			//	m_regen_bitmap_needed |= m_bm == null || m_bm.Size != ClientSize;
			//	if (m_regen_bitmap_needed)
			//	{
			//		// When rendering the graph bitmap completes, trigger another refresh of the control
			//		m_bm = GetGraphBitmap(ClientSize, true, (snd,bm)=>{ m_bm = bm; Refresh(); }, m_bm);
			//		m_regen_bitmap_needed = false;
			//	}
			//	else
			//	{
			//		m_snap.m_bm = null;
			//	}

			//	// Render the graph
			//	if (m_bm != null)
			//		e.Graphics.DrawImage(m_bm, 0, 0);

			//	// If a snapshot exists render the captured bitmap
			//	if (m_snap.m_bm != null)
			//	{
			//		var src_rect = new Rectangle(Point.Empty, m_snap.m_bm.Size);
			//		var dst_rect = GraphRegion(ClientSize).Inflated(-1,-1).Shifted(1,1);
			//		e.Graphics.SetClip(dst_rect);
			//		dst_rect.Location = GraphToPoint(new PointF((float)m_snap.m_xrange.m_min, (float)m_snap.m_yrange.m_max));
			//		dst_rect.Width    = (int)(dst_rect.Width  * m_snap.m_xrange.m_span / m_xaxis.Rng.m_span);
			//		dst_rect.Height   = (int)(dst_rect.Height * m_snap.m_yrange.m_span / m_yaxis.Rng.m_span);

			//		e.Graphics.DrawImage(m_snap.m_bm, dst_rect, src_rect, GraphicsUnit.Pixel);
			//		e.Graphics.ResetClip();
			//	}

			//	// Allow clients to draw in graph space
			//	if (AddOverlaysOnPaint != null)
			//	{
			//		// Get a transform that can be used to draw in unscaled graph space. Can't use a scale transform here
			//		// though because the lines and points will also be scaled, into ellipses or calligraphy etc.
			//		var region = GraphRegion(ClientSize);
			//		e.Graphics.SetClip(region);

			//		var scale_x = (float)(region.Width  / m_xaxis.Span);
			//		var scale_y = (float)(region.Height / m_yaxis.Span);
			//		if (float.IsInfinity(scale_x)) { scale_x = scale_x >= 0 ? float.MaxValue : float.MinValue; }
			//		if (float.IsInfinity(scale_y)) { scale_y = scale_y >= 0 ? float.MaxValue : float.MinValue; }

			//		// Can't use inverted y scale here because the text comes out upside down
			//		e.Graphics.Transform = new Matrix(1f, 0f, 0f, 1f, (float)(region.Left - m_xaxis.Min * scale_x), (float)(region.Bottom + m_yaxis.Min * scale_y));
			//		scale_y = -scale_y;
			//		AddOverlaysOnPaint(this, e.Graphics, scale_x, scale_y);
			//		e.Graphics.ResetTransform();
			//		e.Graphics.ResetClip(); // If a selection is in progress, draw the selection box
			//	}

			//	if (m_selection.Width != 0 && m_selection.Height != 0)
			//	{
			//		var pen = new Pen(Color.Black) {DashStyle = DashStyle.Dot};
			//		var rect = m_selection;
			//		if( rect.Width  < 0 ) { rect.X += rect.Width;  rect.Width  = -rect.Width;  }
			//		if( rect.Height < 0 ) { rect.Y += rect.Height; rect.Height = -rect.Height; }
			//		e.Graphics.DrawRectangle(pen, rect);
			//	}
			//}
			//catch (OverflowException)
			//{
			//	// There is a problem in the .NET graphics object that can cause these exceptions if the range is extreme
			//	e.Graphics.DrawString("Rendering error within .NET", m_rdr_options.TitleFont, new SolidBrush(Color.FromArgb(0x80, Color.Black)), new PointF());
			//}

		/// <summary>Begins rendering of the graph into a bitmap.
		/// If 'async' is true, then this method returns immediately with a placeholder bitmap
		/// otherwise the finished bitmap is returned. If 'async' is true then 'render_complete'
		/// is called (if not null) once the finished bitmap is complete. Note: 'render_complete'
		/// is always called in the UI thread context</summary>
		public Bitmap GetGraphBitmap(Size size, bool async, Action<GraphControl, Bitmap> render_complete, Bitmap bm)
		{
			Debug.Assert(!size.IsEmpty);

			// Signal the start of a new render. This should cause existing renders to cancel
			++m_rdr_id;
			m_rdr_idle.WaitOne(); // Wait for existing threads to exit
			m_rdr_idle.Reset();

			// Create a bitmap of the correct size
			if (bm == null || bm.Size != size)
				bm = new Bitmap(size.Width, size.Height);

			// Set the axis step sizes appropriate for 'size'
			SetSuitableStepSizes(size);

			if (!async)
			{
				// Generate the graph in 'bm' synchronously
				GenerateGraphBitmap(bm, ()=>{return false;});
				if (render_complete != null) render_complete(this, bm);
				m_rdr_idle.Set();
			}
			else
			{
				// If rendering async, then create a quick placeholder bitmap
				var gfx = Graphics.FromImage(bm);
				var region = GraphRegion(size);
				RenderGraph(gfx, size, region);

				// Add a note to indicate the graph is still rendering
				const string rdring_msg = "rendering...";
				var text_size = gfx.MeasureString(rdring_msg, m_rdr_options.TitleFont);
				Brush brush = new SolidBrush(Color.FromArgb(0x80, Color.Black));
				var pt = new PointF(region.X + (region.Width - text_size.Width)*0.5f, region.Y + (region.Height - text_size.Height)*0.5f);
				gfx.DrawString(rdring_msg, m_rdr_options.TitleFont, brush, pt);

				// Spawn a thread to render the graph into 'm_tmp_bm'
				ThreadPool.QueueUserWorkItem(rdr_id =>
					{
						var tmp_bm = new Bitmap(size.Width, size.Height);
						var cancelled = GenerateGraphBitmap(tmp_bm, ()=>{return m_rdr_id != (int)rdr_id;});
						if (!cancelled && render_complete != null) BeginInvoke(render_complete, this, tmp_bm);
						m_rdr_idle.Set();
					}, m_rdr_id);
			}
			return bm;
		}
		public Bitmap GetGraphBitmap(Size size) { return GetGraphBitmap(size, false, null, null); }

		/// <summary>Synchronously render the graph into the bitmap 'bm'</summary>
		private bool GenerateGraphBitmap(Bitmap bm, Func<bool> cancel_pending)
		{
			var gfx = Graphics.FromImage(bm);
			var region = GraphRegion(bm.Size);

			// Get transforms that we can use to draw in unscaled graph space. Can't use a scale transform here
			// though because the lines and points will also be scaled, into ellipses or calligraphy etc.
			var scale_x = (float)(region.Width  / m_xaxis.Span);
			var scale_y = (float)(region.Height / m_yaxis.Span);
			if (float.IsInfinity(scale_x)) { scale_x = scale_x >= 0 ? float.MaxValue : float.MinValue; }
			if (float.IsInfinity(scale_y)) { scale_y = scale_y >= 0 ? float.MaxValue : float.MinValue; }
			var data_xfrm = new Matrix(1f, 0f, 0f, -1f, (float)(region.Left - m_xaxis.Min * scale_x), (float)(region.Bottom + m_yaxis.Min * scale_y));
			var text_xfrm = new Matrix(1f, 0f, 0f,  1f, (float)(region.Left - m_xaxis.Min * scale_x), (float)(region.Bottom + m_yaxis.Min * scale_y));

			try
			{
				// Render the basic graph
				RenderGraph(gfx, bm.Size, region);
				gfx.SetClip(region);

				// Render the data onto the graph
				gfx.Transform = data_xfrm;
				foreach (var s in m_data)
				{
					if (s.RenderOptions.Visible)
						RenderData(s, gfx, scale_x, scale_y, cancel_pending);

					if (cancel_pending()) break;
				}

				// Can't use inverted y scale here because the text comes out upside down
				gfx.Transform = text_xfrm;
				scale_y = -scale_y;

				// Allow clients to draw in graph space
				if (AddOverlaysOnRender != null && !cancel_pending())
					AddOverlaysOnRender(this, gfx, scale_x, scale_y);

				// Add notes to the graph
				foreach (var note in m_notes)
				{
					gfx.DrawString(note.m_msg, m_rdr_options.NoteFont, new SolidBrush(note.m_colour), new PointF(note.m_loc.X*scale_x, note.m_loc.Y*scale_y));
					if (cancel_pending()) break;
				}

				gfx.ResetTransform();
				gfx.ResetClip();
			}
			catch (Exception ex)
			{
				// There is a problem in the .NET graphics object that can cause these exceptions if the range is extreme
				gfx.Transform = text_xfrm;
				gfx.DrawString("Rendering error occured: "+ex.Message, m_rdr_options.TitleFont, new SolidBrush(Color.FromArgb(0x80, Color.Black)), new PointF());
			}
			return cancel_pending();
		} 
			/// <summary>Returns a suitable step size for the given range of values</summary>
			public static double SetSuitableStepSize(float max_ticks, double span)
			{
				var range = span > 0.0 ? span : 1.0;
				var step = Math.Pow(10, (int)Math.Log10(range));
				foreach (var s in new []{50.0, 20.0, 10.0, 5.0, 4.0, 2.0, 1.0, 0.5, 0.25, 0.2, 0.1, 0.05})
				{
					if (range * s / step > max_ticks) continue;
					step /= s;
					return step;
				}
				return step;
			}
 * */