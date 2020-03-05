//***************************************************
// Graph Control
// Copyright (C) Rylogic Ltd 2008
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
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Utility;
using Rylogic.Interop.Win32;
using Matrix = System.Drawing.Drawing2D.Matrix;
using System.Windows.Threading;

namespace Rylogic.Gui.WinForms
{
	/// <summary>Custom control for rendering a graph</summary>
	public class GraphControl :UserControl
	{
		/// <summary>Axis type</summary>
		[Flags] public enum EAxis
		{
			None = 0,
			XAxis = 1 << 0,
			YAxis = 1 << 1,
			Both = XAxis | YAxis,
		}

		// Constructors
		public GraphControl()
			:this(string.Empty, new RdrOptions())
		{ }
		public GraphControl(string title, RdrOptions options)
		{
			try
			{
				Options = options;
				Title   = title;
				Range           = new RangeData(this);
				BaseRangeX      = new RangeF(0.0, 1.0);
				BaseRangeY      = new RangeF(0.0, 1.0);
				m_impl_zoom     = new RangeF(float.Epsilon, float.MaxValue);
				MutexRendering  = new object();
				Data            = new BindingListEx<Series>();
				Notes           = new List<Note>();
				m_snap          = new Snapshot();
				m_tmp           = new Snapshot();

				m_grab_location = PointF.Empty;
				m_selection     = Rectangle.Empty;
				m_tooltip       = new ToolTip{ShowAlways = false, UseAnimation = false, UseFading = false, Tag = false};
				m_plot_area     = Rectangle.Empty;

				MouseNavigation = true;
				Dirty           = true;
				DoubleBuffered  = true;
				Legend          = new LegendUI(this);
			}
			catch
			{
				Dispose();
				throw;
			}
		}
		public GraphControl(GraphControl rhs) :this()
		{
			Title              = rhs.Title;
			Range              = new RangeData(rhs.Range);
			BaseRangeX         = rhs.BaseRangeX;
			BaseRangeY         = rhs.BaseRangeY;
			Options            = new RdrOptions(rhs.Options);
			AddOverlayOnRender = rhs.AddOverlayOnRender;
			AddOverlayOnPaint  = rhs.AddOverlayOnPaint;
		}
		protected override void Dispose(bool disposing)
		{
			// Cancel any running background renders
			++m_rdr_issue;
			Legend = Util.Dispose(Legend);
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

		/// <summary>Rendering options for the graph</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public RdrOptions Options { get; private set; }

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

		/// <summary>The current X/Y axis range of the chart</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public RangeData Range
		{
			get { return m_range; }
			private set
			{
				if (value == m_range) return;
				Util.Dispose(ref m_range);
				m_range = value;
			}
		}
		private RangeData m_range;

		/// <summary>Accessor to the current X axis</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public RangeData.Axis XAxis
		{
			get { return Range.XAxis; }
		}

		/// <summary>Accessor to the current Y axis</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public RangeData.Axis YAxis
		{
			get { return Range.YAxis; }
		}

		/// <summary>Default X axis range of the chart</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public RangeF BaseRangeX
		{
			get;
			set;
		}

		/// <summary>Default Y axis range of the chart</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public RangeF BaseRangeY
		{
			get;
			set;
		}

		/// <summary>Set the title and axis labels of the graph</summary>
		public void SetLabels(string title, string xaxis_text, string yaxis_text)
		{
			Title = title;
			XAxis.Label = xaxis_text;
			YAxis.Label = yaxis_text;
		}

		/// <summary>
		/// Find the appropriate range for data on the interval [i0,i1) in the graph.
		/// Call ResetToDefaultRange() to zoom the graph to this range</summary>
		public void FindDefaultRange(EAxis axis = EAxis.Both, int i0 = 0, int i1 = int.MaxValue, bool visible_only = true)
		{
			var do_x = axis.HasFlag(EAxis.XAxis);
			var do_y = axis.HasFlag(EAxis.YAxis);

			var xrange = do_x ? FindRange(gv => gv.x, Data, i0, i1, visible_only) : RangeF.Zero;
			var yrange = do_y ? FindRange(gv => gv.y, Data, i0, i1, visible_only) : RangeF.Zero;

			// Allow users to adjust the default range
			var args = new FindingDefaultRangeEventArgs(axis, xrange, yrange);
			OnFindingDefaultRange(args);
			xrange = args.XRange;
			yrange = args.YRange;

			// Scale up the ranges to leave a tasteful margin around the default range
			if (xrange.Size > 0 && do_x) xrange = xrange.Inflate(1.05f); else xrange = new RangeF(0.0, 1.0);
			if (yrange.Size > 0 && do_y) yrange = yrange.Inflate(1.05f); else yrange = new RangeF(0.0, 1.0);

			// Save the default range
			if (do_x) BaseRangeX = xrange;
			if (do_y) BaseRangeY = yrange;
		}

		/// <summary>Find the appropriate range on a single axis</summary>
		public static RangeF FindRange(Func<GraphValue,double> selector, IEnumerable<Series> data, int i0 = 0, int i1 = int.MaxValue, bool visible_only = true)
		{
			var range = new RangeF(double.MaxValue, -double.MaxValue);
			foreach (var series in data)
			{
				if (visible_only && !series.Options.Visible)
					continue;

				// Note: series.Sorted doesn't help because we still need to scan all the Y values.
				using (var s = series.Lock())
				{
					foreach (var value in s.Values(i0, i1))
					{
						var v = selector(value);
						if (v < range.Beg) range.Beg = v;
						if (v > range.End) range.End = v;
					}
				}
			}

			return range;
		}

		/// <summary>Raised when the default range is being found</summary>
		public event EventHandler<FindingDefaultRangeEventArgs> FindingDefaultRange;
		protected virtual void OnFindingDefaultRange(FindingDefaultRangeEventArgs args)
		{
			FindingDefaultRange?.Invoke(this, args);
		}

		/// <summary>
		/// Reset the axis ranges to the default.
		/// Call FindDefaultRange() to set the default range</summary>
		public void ResetToDefaultRange(EAxis axis = EAxis.Both)
		{
			if (!XAxis.LockRange && axis.HasFlag(EAxis.XAxis)) XAxis.Set(BaseRangeX);
			if (!YAxis.LockRange && axis.HasFlag(EAxis.YAxis)) YAxis.Set(BaseRangeY);
			Dirty = true;
		}

		/// <summary>Return the layout sizes of the graph</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public GraphDims GraphDimensions
		{
			get { return new GraphDims(this); }
		}

		#region Data Series

		/// <summary>Graph data points</summary>
		[Serializable] public struct GraphValue
		{
			public GraphValue(double x, double y)                           :this(x, y, 0, 0, null)       {}
			public GraphValue(double x, double y, object tag)               :this(x, y, 0, 0, tag)        {}
			public GraphValue(double x, double y, double y_lo, double y_hi) :this(x, y, y_lo, y_hi, null) {}
			public GraphValue(double x, double y, double y_lo, double y_hi, object tag)
			{
				Debug.Assert(Math_.IsFinite(x) && Math_.IsFinite(y));
				this.x     = x;
				this.y     = y;
				this.ylo   = y_lo;
				this.yhi   = y_hi;
				this.m_tag = tag;
			}

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

			/// <summary></summary>
			public override string ToString()
			{
				return $"({x},{y})";
			}
			public static Comparer<GraphValue> SortX
			{
				get { return Comparer<GraphValue>.Create((lhs,rhs) => lhs.x.CompareTo(rhs.x)); }
			}
		}

		/// <summary>Graph data series</summary>
		[Serializable] public class Series
		{
			private List<GraphValue> m_data;
			private bool m_sorted;
			private object m_lock;

			public Series()
				:this(string.Empty)
			{}
			public Series(string name)
				:this(string.Empty, 100)
			{}
			public Series(string name, int capacity)
				:this(name, capacity, new RdrOptions())
			{}
			public Series(string name, int capacity, RdrOptions rdr_opts)
				:this(name, capacity, rdr_opts, new GraphValue[0])
			{}
			public Series(string name, int capacity, RdrOptions rdr_opts, IEnumerable<GraphValue> data)
			{
				m_data = new List<GraphValue>(capacity);
				m_data.AddRange(data);

				m_sorted    = true; // Assume sorted because this is more efficient for rendering
				m_lock      = new object();
				Name        = name;
				Options     = rdr_opts;
				AllowDelete = false;
				UserData    = new UserData();
			}
			public Series(Series rhs)
				:this(rhs.Name, rhs.m_data.Capacity, new RdrOptions(rhs.Options))
			{
				using (var s = rhs.Lock())
				{
					m_data.AddRange(rhs.m_data);
					m_sorted = rhs.m_sorted;
				}
				UserData = new UserData(rhs.UserData);
				AllowDelete = rhs.AllowDelete;
			}

			/// <summary>A label for the series</summary>
			public string Name { get; set; }

			/// <summary>The colour of this series on the graph</summary>
			public Color Colour
			{
				get
				{
					switch (Options.PlotType) {
					default: throw new Exception("Unknown plot type");
					case RdrOptions.EPlotType.Point: return Options.PointColour;
					case RdrOptions.EPlotType.Line:  return Options.LineColour;
					case RdrOptions.EPlotType.Bar:   return Options.BarColour;
					}
				}
				set
				{
					switch (Options.PlotType) {
					default: throw new Exception("Unknown plot type");
					case RdrOptions.EPlotType.Point: Options.PointColour = value; break;
					case RdrOptions.EPlotType.Line:  Options.LineColour  = value; break;
					case RdrOptions.EPlotType.Bar:   Options.BarColour   = value; break;
					}
				}
			}

			/// <summary>Options for rendering this series</summary>
			[Browsable(false)]
			[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
			public RdrOptions Options { get; set; }

			/// <summary>Allow delete from this series</summary>
			public bool AllowDelete { get; set; }

			/// <summary>Allow users to attach additional data to the series</summary>
			public UserData UserData { get; private set; }

			/// <summary>Sync access to the series data</summary>
			public LockData Lock() { return new LockData(this); }
			public class LockData :IDisposable
			{
				private readonly Series m_series;

				public LockData(Series series)
				{
					m_series = series;
					Monitor.Enter(m_series.m_lock);
				}
				public void Dispose()
				{
					Monitor.Exit(m_series.m_lock);
				}

				/// <summary>The number of items in the series</summary>
				public int Count
				{
					get { return Data.Count; }
				}

				/// <summary>Reset the series</summary>
				public void Clear()
				{
					Data.Clear();
				}

				/// <summary>Access the container of graph values</summary>
				public List<GraphValue> Data
				{
					get { return m_series.m_data; }
				}

				///<summary>
				/// Return the range of indices that need to be considered when plotting from 'xmin' to 'xmax'
				/// Note: in general, this range should include one point to the left of 'xmin' and one to the right
				/// of 'xmax' so that line graphs plot a line up to the border of the plot area</summary>
				public void IndexRange(double xmin, double xmax, out int imin, out int imax)
				{
					var lwr = new GraphValue(xmin, 0.0f);
					var upr = new GraphValue(xmax, 0.0f);

					imin = Data.BinarySearch(0, Count, lwr, GraphValue.SortX);
					if (imin < 0) imin = ~imin;
					if (imin != 0) --imin;

					imax = Data.BinarySearch(imin, Count - imin, upr, GraphValue.SortX);
					if (imax < 0) imax = ~imax;
					if (imax != Count) ++imax;
				}

				/// <summary>Enumerable access to the data given an index range</summary>
				public IEnumerable<GraphValue> Values(int i0 = 0, int i1 = int.MaxValue)
				{
					Debug.Assert(i0 >= 0 && i0 <= Count);
					Debug.Assert(i1 >= i0);
					for (int i = i0, iend = Math.Min(i1, Count); i != iend; ++i)
					{
						var gv = Data[i];
						yield return gv;
					}
				}

				/// <summary>
				/// Returns the range of series data to consider when plotting from 'xmin' to 'xmax'
				/// Note: in general, this range should include one point to the left of 'xmin' and one to
				/// the right of 'xmax' so that line graphs plot a line up to the border of the plot area</summary>
				public IEnumerable<GraphValue> Values(double xmin, double xmax)
				{
					IndexRange(xmin, xmax, out var i0, out var i1);
					return Values(i0, i1);
				}

				/// <summary>
				/// Returns the range of indices to consider when plotting from 'xmin' to 'xmax'
				/// Note: in general, this range should include one point to the left of 'xmin' and one to
				/// the right of 'xmax' so that line graphs plot a line up to the border of the plot area</summary>
				public IEnumerable<int> Indices(double xmin, double xmax)
				{
					IndexRange(xmin, xmax, out var i0, out var i1);
					return Enumerable.Range(i0, i1 - i0);
				}

				/// <summary>Sort the series by it's X value</summary>
				public void SortX()
				{
					Data.Sort(Cmp<GraphValue>.From((l,r) => l.x < r.x));
					Sorted = true;
				}

				/// <summary>True if this series can be considered sorted (on X)</summary>
				public bool Sorted
				{
					get { return m_series.m_sorted; }
					set { m_series.m_sorted = value; }
				}
			}

			/// <summary>Thread-safe helper for adding data</summary>
			public void AddRange(IEnumerable<GraphValue> data, bool clear = false)
			{
				using (var s = Lock())
				{
					if (clear) s.Data.Clear();
					s.Data.AddRange(data);
				}
			}

			/// <summary>ToString</summary>
			public override string ToString()
			{
				return $"{Name} - count:{m_data.Count}";
			}

			/// <summary>Plot colour generator</summary>
			public static Color GenerateColour(int i)
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

			[Serializable]
			public class RdrOptions
			{
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
				public RdrOptions(RdrOptions rhs)
				{
					Visible        = rhs.Visible;
					DrawData       = rhs.DrawData;
					DrawErrorBars  = rhs.DrawErrorBars;
					PlotType       = rhs.PlotType;
					PointColour    = rhs.PointColour;
					PointSize      = rhs.PointSize;
					LineColour     = rhs.LineColour;
					LineWidth      = rhs.LineWidth;
					BarColour      = rhs.BarColour;
					BarWidth       = rhs.BarWidth;
					ErrorBarColour = rhs.ErrorBarColour;
					DrawMovingAvr  = rhs.DrawMovingAvr;
					MAWindowSize   = rhs.MAWindowSize;
					MALineColour   = rhs.MALineColour;
					MALineWidth    = rhs.MALineWidth;
				}
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
			}
		}

		/// <summary>The collection of series' that make up the graph data</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public BindingListEx<Series> Data
		{
			[DebuggerStepThrough] get { return m_impl_data; }
			set
			{
				if (m_impl_data == value) return;
				lock (MutexRendering)
				{
					if (m_impl_data != null)
					{
						m_impl_data.ListChanging -= HandleDataChanging;
					}
					m_impl_data = value;
					if (m_impl_data != null)
					{
						m_impl_data.ListChanging += HandleDataChanging;
					}
					Dirty = true;
				}
			}
		}
		private BindingListEx<Series> m_impl_data;
		private void HandleDataChanging(object sender, ListChgEventArgs<Series> e)
		{
			if (e.After)
				Dirty = true;
		}

		/// <summary>Returns the 'Y' value for a given 'X' value in a series in the graph</summary>
		public double GetValueAt(int series_index, double x)
		{
			if (series_index < 0 || series_index >= Data.Count)
				throw new Exception("series index out of range");

			using (var s = Data[series_index].Lock())
			{
				// Binary search on X for the nearest data point
				var series = s.Data;
				if (series.Count == 0)
					return 0;

				// Find the index range that contains 'x'
				s.IndexRange(x, x, out var i0, out var i1);

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
		}

		/// <summary>Returns the nearest graph data point to 'location' given
		/// a selection tolerance. 'location' should be in graph space.
		/// Returns null if no point is within the selection tolerance</summary>
		public bool GetValueAt(int series_index, PointF pt, out GraphValue gv, int px_tol = 5)
		{
			if (series_index < 0 || series_index >= Data.Count)
				throw new Exception("series index out of range");

			using (var s = Data[series_index].Lock())
			{
				var tol = px_tol * m_plot_area.Width / XAxis.Span;
				var dist_sq = tol * tol;

				gv = new GraphValue();
				foreach (var v in s.Values(pt.X - tol, pt.X + tol))
				{
					var tmp = new v2((float)v.x, (float)v.y);
					var d = (tmp - pt).LengthSq;
					if (d < dist_sq)
					{
						dist_sq = d;
						gv = v;
					}
				}
				return dist_sq < tol * tol;
			}
		}

		#endregion

		#region RdrOptions
		[Serializable]
		[TypeConverter(typeof(TyConv))]
		public class RdrOptions :INotifyPropertyChanged
		{
			private class TyConv :GenericTypeConverter<RdrOptions> {}

			public RdrOptions()
			{
				BkColour        = SystemColors.Control;
				PlotBkColour    = Color.White;
				AxisColour      = Color.Black;
				GridColour      = Color.FromArgb(230, 230, 230);
				TitleColour     = Color.Black;
				TitleFont       = new Font("tahoma", 12, FontStyle.Bold);
				TitleTransform  = new Matrix(1f, 0f, 0f, 1f, 0f, 0f);
				Margin          = new Padding(3);
				NoteFont        = new Font("tahoma",  8, FontStyle.Regular);
				SelectionColour = Color.DarkGray;
				XAxis           = new Axis();
				YAxis           = new Axis();
			}
			public RdrOptions(RdrOptions rhs)
			{
				BkColour        = rhs.BkColour;
				PlotBkColour    = rhs.PlotBkColour;
				AxisColour      = rhs.AxisColour;
				GridColour      = rhs.GridColour;
				TitleColour     = rhs.TitleColour;
				TitleFont       = (Font)rhs.TitleFont.Clone();
				TitleTransform  = rhs.TitleTransform;
				Margin          = rhs.Margin;
				NoteFont        = (Font)rhs.NoteFont.Clone();
				SelectionColour = rhs.SelectionColour;
				XAxis           = new Axis(rhs.XAxis);
				YAxis           = new Axis(rhs.YAxis);
			}
			public RdrOptions(XElement node) :this()
			{
				BkColour        = node.Element(XmlTag.BkColour       ).As(BkColour       );
				PlotBkColour    = node.Element(XmlTag.PlotBkColour   ).As(PlotBkColour   );
				AxisColour      = node.Element(XmlTag.AxisColour     ).As(AxisColour     );
				GridColour      = node.Element(XmlTag.GridColour     ).As(GridColour     );
				TitleColour     = node.Element(XmlTag.TitleColour    ).As(TitleColour    );
				TitleFont       = node.Element(XmlTag.TitleFont      ).As(TitleFont      );
				TitleTransform  = node.Element(XmlTag.TitleTransform ).As(TitleTransform );
				Margin          = node.Element(XmlTag.Margin         ).As(Margin         );
				NoteFont        = node.Element(XmlTag.NoteFont       ).As(NoteFont       );
				SelectionColour = node.Element(XmlTag.SelectionColour).As(SelectionColour);
				XAxis           = node.Element(XmlTag.XAxis          ).As(XAxis          );
				YAxis           = node.Element(XmlTag.YAxis          ).As(YAxis          );
			}
			public XElement ToXml(XElement node)
			{
				node.Add2(XmlTag.BkColour        , BkColour        , false);
				node.Add2(XmlTag.PlotBkColour    , PlotBkColour    , false);
				node.Add2(XmlTag.AxisColour      , AxisColour      , false);
				node.Add2(XmlTag.GridColour      , GridColour      , false);
				node.Add2(XmlTag.TitleColour     , TitleColour     , false);
				node.Add2(XmlTag.TitleFont       , TitleFont       , false);
				node.Add2(XmlTag.TitleTransform  , TitleTransform  , false);
				node.Add2(XmlTag.Margin          , Margin          , false);
				node.Add2(XmlTag.NoteFont        , NoteFont        , false);
				node.Add2(XmlTag.SelectionColour , SelectionColour , false);
				node.Add2(XmlTag.XAxis           , XAxis           , false);
				node.Add2(XmlTag.YAxis           , YAxis           , false);
				return node;
			}

			/// <summary>Property changed</summary>
			public event PropertyChangedEventHandler PropertyChanged;
			private void SetProp<T>(ref T prop, T value, string name)
			{
				if (Equals(prop, value)) return;
				prop = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
			}

			/// <summary>The fill colour of the background of the chart</summary>
			public Color BkColour
			{
				get { return m_BkColour; }
				set { SetProp(ref m_BkColour, value, nameof(BkColour)); }
			}
			private Color m_BkColour;

			/// <summary>The fill colour of the chart background</summary>
			public Color PlotBkColour
			{
				get { return m_PlotBkColour; }
				set { SetProp(ref m_PlotBkColour, value, nameof(PlotBkColour)); }
			}
			private Color m_PlotBkColour;

			/// <summary>The colour of the title text</summary>
			public Color TitleColour
			{
				get { return m_TitleColour; }
				set { SetProp(ref m_TitleColour, value, nameof(TitleColour)); }
			}
			private Color m_TitleColour;

			/// <summary>The colour of the axes</summary>
			public Color AxisColour
			{
				get { return m_AxisColour; }
				set { SetProp(ref m_AxisColour, value, nameof(AxisColour)); }
			}
			private Color m_AxisColour;

			/// <summary>The colour of the grid lines</summary>
			public Color GridColour
			{
				get { return m_GridColour; }
				set { SetProp(ref m_GridColour, value, nameof(GridColour)); }
			}
			private Color m_GridColour;

			/// <summary>Font to use for the title text</summary>
			public Font TitleFont
			{
				get { return m_TitleFont; }
				set { SetProp(ref m_TitleFont, value, nameof(TitleFont)); }
			}
			private Font m_TitleFont;

			/// <summary>Transform for position the graph title, offset from top centre</summary>
			public Matrix TitleTransform
			{
				get { return m_TitleTransform; }
				set { SetProp(ref m_TitleTransform, value, nameof(TitleTransform)); }
			}
			private Matrix m_TitleTransform;

			/// <summary>The distances from the edge of the control to the graph area</summary>
			public Padding Margin
			{
				get { return m_Margin; }
				set { SetProp(ref m_Margin, value, nameof(Margin)); }
			}
			private Padding m_Margin;

			/// <summary>Font to use for graph notes</summary>
			public Font NoteFont
			{
				get { return m_NoteFont; }
				set { SetProp(ref m_NoteFont, value, nameof(NoteFont)); }
			}
			private Font m_NoteFont;

			/// <summary>Area selection colour</summary>
			public Color SelectionColour
			{
				get { return m_SelectionColour; }
				set { SetProp(ref m_SelectionColour, value, nameof(SelectionColour)); }
			}
			private Color m_SelectionColour;

			/// <summary>XAxis rendering options</summary>
			public Axis XAxis 
			{
				get { return m_XAxis; }
				private set
				{
					if (m_XAxis == value) return;
					if (m_XAxis != null) m_XAxis.PropertyChanged -= HandleXAxisPropertyChanged;
					m_XAxis = value;
					if (m_XAxis != null) m_XAxis.PropertyChanged -= HandleXAxisPropertyChanged;
				}
			}
			private Axis m_XAxis;
			private void HandleXAxisPropertyChanged(object sender, PropertyChangedEventArgs e)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(XAxis)));
			}

			/// <summary>YAxis rendering options</summary>
			public Axis YAxis
			{
				get { return m_YAxis; }
				private set
				{
					if (m_YAxis == value) return;
					if (m_YAxis != null) m_YAxis.PropertyChanged -= HandleYAxisPropertyChanged;
					m_YAxis = value;
					if (m_YAxis != null) m_YAxis.PropertyChanged -= HandleYAxisPropertyChanged;
				}
			}
			private Axis m_YAxis;
			private void HandleYAxisPropertyChanged(object sender, PropertyChangedEventArgs e)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(YAxis)));
			}

			[Serializable]
			[TypeConverter(typeof(TyConv))]
			public class Axis
			{
				private class TyConv :GenericTypeConverter<Axis> {}

				public Axis()
				{
					LabelFont      = new Font("tahoma", 10, FontStyle.Regular);
					TickFont       = new Font("tahoma", 8, FontStyle.Regular);
					AxisColour     = Color.Black;
					LabelColour    = Color.Black;
					TickColour     = Color.Black;
					TickLength     = 5;
					MinTickSize    = TickFont.Height * 1.2f;
					DrawTickMarks  = true;
					DrawTickLabels = true;
					LabelTransform = new Matrix(1f, 0f, 0f, 1f, 0f, 0f);
					PixelsPerTick  = 30.0;
				}
				public Axis(Axis rhs)
				{
					LabelFont      = (Font)rhs.LabelFont.Clone();
					TickFont       = (Font)rhs.TickFont.Clone();
					AxisColour     = rhs.AxisColour;
					LabelColour    = rhs.LabelColour;
					TickColour     = rhs.TickColour;
					TickLength     = rhs.TickLength;
					MinTickSize    = rhs.MinTickSize;
					DrawTickMarks  = rhs.DrawTickMarks;
					DrawTickLabels = rhs.DrawTickLabels;
					LabelTransform = rhs.LabelTransform;
					PixelsPerTick  = rhs.PixelsPerTick;
				}
				public Axis(XElement node) :this()
				{
					LabelFont      = node.Element(XmlTag.LabelFont     ).As(LabelFont     );
					TickFont       = node.Element(XmlTag.TickFont      ).As(TickFont      );
					AxisColour     = node.Element(XmlTag.AxisColour    ).As(AxisColour    );
					LabelColour    = node.Element(XmlTag.LabelColour   ).As(LabelColour   );
					TickColour     = node.Element(XmlTag.TickColour    ).As(TickColour    );
					TickLength     = node.Element(XmlTag.TickLength    ).As(TickLength    );
					MinTickSize    = node.Element(XmlTag.MinTickSize   ).As(MinTickSize   );
					DrawTickMarks  = node.Element(XmlTag.DrawTickMarks ).As(DrawTickMarks );
					DrawTickLabels = node.Element(XmlTag.DrawTickLabels).As(DrawTickLabels);
					LabelTransform = node.Element(XmlTag.LabelTransform).As(LabelTransform);
					PixelsPerTick  = node.Element(XmlTag.PixelsPerTick ).As(PixelsPerTick );
				}
				public XElement ToXml(XElement node)
				{
					node.Add2(XmlTag.LabelFont      , LabelFont      , false);
					node.Add2(XmlTag.TickFont       , TickFont       , false);
					node.Add2(XmlTag.AxisColour     , AxisColour     , false);
					node.Add2(XmlTag.LabelColour    , LabelColour    , false);
					node.Add2(XmlTag.TickColour     , TickColour     , false);
					node.Add2(XmlTag.TickLength     , TickLength     , false);
					node.Add2(XmlTag.MinTickSize   , MinTickSize   , false);
					node.Add2(XmlTag.DrawTickMarks  , DrawTickMarks  , false);
					node.Add2(XmlTag.DrawTickLabels , DrawTickLabels , false);
					node.Add2(XmlTag.LabelTransform , LabelTransform , false);
					node.Add2(XmlTag.PixelsPerTick  , PixelsPerTick  , false);
					return node;
				}

				/// <summary>Property changed</summary>
				public event PropertyChangedEventHandler PropertyChanged;
				private void SetProp<T>(ref T prop, T value, string name)
				{
					if (Equals(prop, value)) return;
					prop = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
				}

				/// <summary>The font to use for the axis label</summary>
				public Font LabelFont
				{
					get { return m_LabelFont; }
					set { SetProp(ref m_LabelFont, value, nameof(LabelFont)); }
				}
				private Font m_LabelFont;

				/// <summary>The font to use for tick labels</summary>
				public Font TickFont
				{
					get { return m_TickFont; }
					set { SetProp(ref m_TickFont, value, nameof(TickFont)); }
				}
				private Font m_TickFont;

				/// <summary>The colour of the main axes</summary>
				public Color AxisColour
				{
					get { return m_AxisColour; }
					set { SetProp(ref m_AxisColour, value, nameof(AxisColour)); }
				}
				private Color m_AxisColour;

				/// <summary>The colour of the label text</summary>
				public Color LabelColour
				{
					get { return m_LabelColour; }
					set { SetProp(ref m_LabelColour, value, nameof(LabelColour)); }
				}
				private Color m_LabelColour;

				/// <summary>The colour of the tick text</summary>
				public Color TickColour
				{
					get { return m_TickColour; }
					set { SetProp(ref m_TickColour, value, nameof(TickColour)); }
				}
				private Color m_TickColour;

				/// <summary>The length of the tick marks</summary>
				public int TickLength
				{
					get { return m_TickLength; }
					set { SetProp(ref m_TickLength, value, nameof(TickLength)); }
				}
				private int m_TickLength;

				/// <summary>The minimum space reserved for tick marks and labels</summary>
				public float MinTickSize
				{
					get { return m_MinTickSize; }
					set { SetProp(ref m_MinTickSize, value, nameof(MinTickSize)); }
				}
				private float m_MinTickSize;

				/// <summary>True if tick marks should be drawn</summary>
				public bool DrawTickMarks
				{
					get { return m_DrawTickMarks; }
					set { SetProp(ref m_DrawTickMarks, value, nameof(DrawTickMarks)); }
				}
				private bool m_DrawTickMarks;

				/// <summary>True if tick labels should be drawn</summary>
				public bool DrawTickLabels
				{
					get { return m_DrawTickLabels; }
					set { SetProp(ref m_DrawTickLabels, value, nameof(DrawTickLabels)); }
				}
				private bool m_DrawTickLabels;

				/// <summary>Offset transform from default label position</summary>
				public Matrix LabelTransform
				{
					get { return m_LabelTransform; }
					set { SetProp(ref m_LabelTransform, value, nameof(LabelTransform)); }
				}
				private Matrix m_LabelTransform;

				/// <summary>The preferred number of pixels between each grid line</summary>
				public double PixelsPerTick
				{
					get { return m_PixelsPerTick; }
					set { SetProp(ref m_PixelsPerTick, value, nameof(PixelsPerTick)); }
				}
				private double m_PixelsPerTick;
			}
		}

		/// <summary>A UI for setting these rendering properties</summary>
		public class RdrOptionsUI :ToolForm
		{
			private readonly GraphControl m_graph;
			private readonly RdrOptions m_opts;

			public RdrOptionsUI(GraphControl graph, RdrOptions opts)
				:base(graph, EPin.Centre, Point.Empty, new Size(500,400), true)
			{
				m_graph  = graph;
				m_opts   = opts;
				ShowIcon = (graph.TopLevelControl as Form)?.ShowIcon ?? false;
				Icon     = (graph.TopLevelControl as Form)?.Icon;
				Text     = "Chart Properties";
				SetupUI();
			}
			private void SetupUI()
			{
				var pg = Controls.Add2(new PropertyGrid
				{
					SelectedObject = m_opts,
					Dock = DockStyle.Fill,
				});
				pg.PropertyValueChanged += (s,a) =>
				{
					m_graph.Invalidate();
				};
			}
		}
		#endregion

		#region Range Data

		/// <summary>The axes of the graph</summary>
		public class RangeData :IDisposable
		{
			public RangeData(GraphControl owner)
			{
				XAxis = new Axis(EAxis.XAxis, owner);
				YAxis = new Axis(EAxis.YAxis, owner);
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

			/// <summary>The chart that owns this axis</summary>
			public GraphControl Owner { [DebuggerStepThrough] get; private set; }

			/// <summary>The graph X axis</summary>
			public Axis XAxis
			{
				get { return m_xaxis; }
				internal set
				{
					if (m_xaxis == value) return;
					if (m_xaxis != null)
					{
						Util.Dispose(ref m_xaxis);
					}
					m_xaxis = value;
					if (m_xaxis != null)
					{}
				}
			}
			private Axis m_xaxis;

			/// <summary>The chart Y axis</summary>
			public Axis YAxis
			{
				get { return m_yaxis; }
				internal set
				{
					if (m_yaxis == value) return;
					if (m_yaxis != null)
					{
						Util.Dispose(ref m_yaxis);
					}
					m_yaxis = value;
					if (m_yaxis != null)
					{}
				}
			}
			private Axis m_yaxis;

			/// <summary>Graph axis data</summary>
			[DebuggerDisplay("lbl={Label} rng=[{Min} : {Max}]")]
			public class Axis :IDisposable
			{
				public Axis(EAxis axis, GraphControl owner)
					: this(axis, owner, 0f, 1f)
				{ }
				public Axis(EAxis axis, GraphControl owner, double min, double max)
					: this(axis, owner, min, max, new RdrOptions())
				{ }
				public Axis(EAxis axis, GraphControl owner, double min, double max, RdrOptions options)
				{
					Debug.Assert(axis == EAxis.XAxis || axis == EAxis.YAxis);
					Debug.Assert(owner != null);
					Set(min, max);
					AxisType        = axis;
					Owner           = owner;
					Label           = string.Empty;
					AllowScroll     = true;
					AllowZoom       = true;
					LockRange       = false;
					TickText        = (x,step) => Math.Round(x, 4, MidpointRounding.AwayFromZero).ToString("F2");
				}
				public Axis(Axis rhs)
				{
					Set(rhs.Min, rhs.Max);
					AxisType        = rhs.AxisType;
					Owner           = rhs.Owner;
					Label           = rhs.Label;
					AllowScroll     = rhs.AllowScroll;
					AllowZoom       = rhs.AllowZoom;
					LockRange       = rhs.LockRange;
					TickText        = rhs.TickText;
				}
				public virtual void Dispose()
				{}

				/// <summary>Render options for the axis</summary>
				public RdrOptions.Axis Options
				{
					get
					{
						if (Owner.XAxis == this) return Owner.Options.XAxis;
						if (Owner.YAxis == this) return Owner.Options.YAxis;
						throw new Exception("Owner is not the owner of this axis");
					}
				}

				/// <summary>Which axis this is</summary>
				public EAxis AxisType { get; private set; }

				/// <summary>The chart that owns this axis</summary>
				public GraphControl Owner { get; private set; }

				/// <summary>Axis label</summary>
				public string Label
				{
					get { return m_label ?? string.Empty; }
					set
					{
						if (m_label == value) return;
						m_label = value;
					}
				}
				private string m_label;

				/// <summary>The minimum axis value</summary>
				public double Min
				{
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
					get { return Max - Min; }
					set
					{
						if (Span == value) return;
						Set(Centre - 0.5*value, Centre + 0.5*value);
					}
				}

				/// <summary>The centre value of the range</summary>
				public double Centre
				{
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
					get { return new RangeF(Min, Max); }
					set
					{
						if (Equals(Range, value)) return;
						Set(value.Beg, value.End);
					}
				}

				/// <summary>Allow scrolling on this axis</summary>
				public bool AllowScroll { get; set; }

				/// <summary>Allow zooming on this axis</summary>
				public bool AllowZoom { get; set; }

				/// <summary>Get/Set whether the range can be changed by user input</summary>
				public bool LockRange { get; set; }

				/// <summary>Convert the axis value to a string. "string TickText(double tick_value, double step_size)" </summary>
				public Func<double, double, string> TickText;

				/// <summary>Set the range without risk of an assert if 'min' is greater than 'Max' or visa versa</summary>
				public void Set(double min, double max)
				{
					if (min >= max) throw new Exception("Range must be positive and non-zero");
					var zoomed = !Math_.FEql(max - min, m_max - m_min);
					var scroll = !Math_.FEql((max + min)*0.5, (m_max + m_min)*0.5);

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
					var dims = Owner.GraphDimensions;
					var axis_length = Owner.XAxis == this ? dims.ChartArea.Width : Owner.YAxis == this ? dims.ChartArea.Height : 0.0;
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
					if (max - min > step*100)
						step = (max - min) * 0.01f;
				}
			}
		}

		#endregion

		#region Navigation

		/// <summary>Enable/Disable default mouse control of the graph</summary>
		[Browsable(false)]
		public bool MouseNavigation
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
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public PointF Centre
		{
			get { return new PointF((float)(XAxis.Min + XAxis.Span*0.5), (float)(YAxis.Min + YAxis.Span*0.5)); }
			set
			{
				XAxis.Min = value.X - XAxis.Span*0.5;
				YAxis.Min = value.Y - YAxis.Span*0.5;
				Dirty = true;
			}
		}

		/// <summary>Zoom in/out on the chart. Remember to call refresh. Zoom is a floating point value where 1f = no zoom, 2f = 2x magnification</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public double Zoom
		{
			get
			{
				return
					XAxis.AllowZoom ? XAxis.Span / BaseRangeX.Size :
					YAxis.AllowZoom ? YAxis.Span / BaseRangeY.Size : 1f;
			}
			set
			{
				// Limit zoom amount
				value = Math_.Clamp(value, ZoomMin, ZoomMax);

				// If both axes allow zoom, maintain the aspect ratio
				if (XAxis.AllowZoom && YAxis.AllowZoom)
				{
					var aspect = (YAxis.Span * BaseRangeX.Size) / (BaseRangeY.Size * XAxis.Span);
					aspect = Math_.Clamp(Math_.IsFinite(aspect) ? aspect : 1.0, 0.001, 1000);
					XAxis.Span = BaseRangeX.Size * value         ;
					YAxis.Span = BaseRangeY.Size * value * aspect;
				}
				else if (XAxis.AllowZoom)
				{
					XAxis.Span = BaseRangeX.Size * value;
				}
				else if (YAxis.AllowZoom)
				{
					YAxis.Span = BaseRangeY.Size * value;
				}
				Dirty = true;
				Invalidate();
			}
		}
		private RangeF m_impl_zoom;

		/// <summary>Minimum zoom limit</summary>
		public double ZoomMin
		{
			get { return m_impl_zoom.Beg; }
			set
			{
				Debug.Assert(value > 0f);
				m_impl_zoom.Beg = value;
			}
		}

		/// <summary>Maximum zoom limit</summary>
		public double ZoomMax
		{
			get { return m_impl_zoom.End; }
			set
			{
				Debug.Assert(value > 0f);
				m_impl_zoom.End = value;
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
					if (XAxis.AllowZoom) XAxis.Min = lower.X;
					if (XAxis.AllowZoom) XAxis.Max = upper.X;
					if (YAxis.AllowZoom) YAxis.Min = lower.Y;
					if (YAxis.AllowZoom) YAxis.Max = upper.Y;
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
			var delta = Math_.Clamp(e.Delta, -999, 999);
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

		/// <summary>The calculated areas of the control</summary>
		public struct GraphDims
		{
			public GraphDims(GraphControl graph)
			{
				Owner = graph;
				using (var gfx = graph.CreateGraphics())
				{
					RectangleF rect = graph.ClientRectangle;
					var r = SizeF.Empty;

					Area = rect.ToRect();

					// Add margins
					rect.X      += graph.Options.Margin.Left;
					rect.Y      += graph.Options.Margin.Top;
					rect.Width  -= graph.Options.Margin.Left + graph.Options.Margin.Right;
					rect.Height -= graph.Options.Margin.Top + graph.Options.Margin.Bottom;

					// Add space for tick marks
					if (graph.YAxis.Options.DrawTickMarks)
					{
						rect.X      += graph.YAxis.Options.TickLength;
						rect.Width  -= graph.YAxis.Options.TickLength;
					}
					if (graph.XAxis.Options.DrawTickMarks)
					{
						rect.Height -= graph.XAxis.Options.TickLength;
					}

					// Add space for the title and axis labels
					if (graph.Title.HasValue())
					{
						r = gfx.MeasureString(graph.Title, graph.Options.TitleFont);
						rect.Y      += r.Height;
						rect.Height -= r.Height;
					}
					if (graph.XAxis.Label.HasValue())
					{
						r = gfx.MeasureString(graph.XAxis.Label, graph.XAxis.Options.LabelFont);
						rect.Height -= r.Height;
					}
					if (graph.YAxis.Label.HasValue())
					{
						r = gfx.MeasureString(graph.Range.YAxis.Label, graph.YAxis.Options.LabelFont);
						rect.X     += r.Height; // will be rotated by 90deg
						rect.Width -= r.Height;
					}

					// Add space for the tick labels
					const string lbl = "9.999";
					if (graph.XAxis.Options.DrawTickLabels)
					{
						r = gfx.MeasureString(lbl, graph.XAxis.Options.TickFont);
						rect.Height -= r.Height;
					}
					if (graph.YAxis.Options.DrawTickLabels)
					{
						r = gfx.MeasureString(lbl, graph.YAxis.Options.TickFont);
						rect.X     += r.Width;
						rect.Width -= r.Width;
					}

					ChartArea = rect.ToRect();
				}
			}

			/// <summary>The graph that these dimensions were calculated from</summary>
			public GraphControl Owner { get; private set; }

			/// <summary>The size of the control</summary>
			public Rectangle Area { get; private set; }

			/// <summary>The area of the view3d part of the chart</summary>
			public Rectangle ChartArea { get; private set; }
		}

		/// <summary>A snapshot of the current graph, used during dragging/zooming</summary>
		private class Snapshot
		{
			public Bitmap m_bm;
			public RangeF m_xrange;
			public RangeF m_yrange;

			public Snapshot()
			{
				m_bm = null;
				m_xrange = new RangeF(0.0, 1.0);
				m_yrange = new RangeF(0.0, 1.0);
			}

			public Size Size
			{
				get { return new Size(m_bm != null ? m_bm.Width : 0, m_bm != null ? m_bm.Height : 0); }
			}

			public Rectangle Rect
			{
				get { return new Rectangle(Point.Empty, Size); }
			}
		}

		/// <summary>
		/// A mutex that is locked by the control during rendering.
		/// This should be used to synchronise access to the source data with rendering</summary>
		public readonly object MutexRendering;

		/// <summary>A snapshot of the plot area, used as a temporary copy during drag/zoom operations</summary>
		private Snapshot m_snap;

		/// <summary>A temporary bitmap used for background thread rendering</summary>
		private Snapshot m_tmp;

		/// <summary>True when the bitmap needs to be regenerated</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool Dirty
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

		/// <summary>Issue counter for rendering</summary>
		private volatile int m_rdr_issue;

		/// <summary>The screen space size of the plot part of the control</summary>
		private Rectangle m_plot_area;

		/// <summary>Render the graph synchronously.</summary>
		public void RenderGraph(Graphics gfx, Rectangle area, out Rectangle plot_area)
		{
			plot_area = PlotArea(gfx, area);
			RenderGraphFrame(gfx, area, plot_area);
			RenderData(gfx, plot_area, m_rdr_issue);
		}
		public void RenderGraph(Graphics gfx, Rectangle area)
		{
			Rectangle plot_area;
			RenderGraph(gfx, area, out plot_area);
		}

		/// <summary>
		/// Get the scale and translation transform from graph space to client space.
		/// e.g. g2c * Point(x_min, y_min) = plot_area.BottomLeft()
		///      g2c * Point(x_max, y_max) = plot_area.TopRight()</summary>
		public Matrix3x3 GraphToClientSpace(Rectangle plot_area)
		{
			var scale_x = +(plot_area.Width  / XAxis.Span);
			var scale_y = -(plot_area.Height / YAxis.Span);
			var offset_x = plot_area.Left   - XAxis.Min * scale_x;
			var offset_y = plot_area.Bottom - YAxis.Min * scale_y;

			var g2c = new Matrix3x3(
				scale_x  , 0.0     , 
				0.0      , scale_y ,
				offset_x , offset_y);

			#if false
			// Check the XAxis corners map to the expected client space locations
			var g_pt0 = new Vector3(XAxis.Min, YAxis.Min, 1.0);
			var g_pt1 = new Vector3(XAxis.Max, YAxis.Max, 1.0);
			var c_pt0 = g2c.TransformPoint(g_pt0);
			var c_pt1 = g2c.TransformPoint(g_pt1);
			Debug.Assert(Math.Abs(c_pt0.x - plot_area.Left  ) < 0.0001);
			Debug.Assert(Math.Abs(c_pt0.y - plot_area.Bottom) < 0.0001);
			Debug.Assert(Math.Abs(c_pt1.x - plot_area.Right ) < 0.0001);
			Debug.Assert(Math.Abs(c_pt1.y - plot_area.Top   ) < 0.0001);
			#endif

			return g2c;
		}
		public Matrix3x3 GraphToClientSpace()
		{
			return GraphToClientSpace(m_plot_area);
		}

		/// <summary>
		/// Get the scale and translation transform from client space to graph space.
		/// e.g. c2g * plot_area.BottomLeft() = Point(x_min, y_min)
		///      c2g * plot_area.TopRight()   = Point(x_max, y_max)</summary>
		public Matrix3x3 ClientToGraphSpace(Rectangle plot_area)
		{
			var scale_x = +(XAxis.Span / plot_area.Width );
			var scale_y = -(YAxis.Span / plot_area.Height);
			var offset_x = XAxis.Min - plot_area.Left   * scale_x;
			var offset_y = YAxis.Min - plot_area.Bottom * scale_y;

			var c2g = new Matrix3x3(
				scale_x  , 0.0     , 
				0.0      , scale_y ,
				offset_x , offset_y);

			#if false
			// Check the plot_area corners map to the expected graph space locations
			var c_pt0 = new Vector3(plot_area.Left , plot_area.Bottom, 1.0);
			var c_pt1 = new Vector3(plot_area.Right, plot_area.Top   , 1.0);
			var g_pt0 = c2g.TransformPoint(c_pt0);
			var g_pt1 = c2g.TransformPoint(c_pt1);
			Debug.Assert(Math.Abs(g_pt0.x - XAxis.Min) < 0.0001f);
			Debug.Assert(Math.Abs(g_pt0.y - YAxis.Min) < 0.0001f);
			Debug.Assert(Math.Abs(g_pt1.x - XAxis.Max) < 0.0001f);
			Debug.Assert(Math.Abs(g_pt1.y - YAxis.Max) < 0.0001f);
			#endif

			return c2g;
		}
		public Matrix3x3 ClientToGraphSpace()
		{
			return ClientToGraphSpace(m_plot_area);
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
					++m_rdr_issue;
					m_impl_dirty = false;

					// Make sure the temporary bitmap and the snapshot bitmap are the correct size
					var plot_size = m_plot_area.Size;
					if (m_tmp.Size != plot_size)
					{
						Util.Dispose(ref m_tmp.m_bm);
						m_tmp.m_bm = new Bitmap(plot_size.Width, plot_size.Height);
					}
					m_tmp.m_xrange = XAxis.Range;
					m_tmp.m_yrange = YAxis.Range;

					var dispatcher = Dispatcher.CurrentDispatcher;

					// Plot rendering (done in a background thread).
					// This thread renders the plot into the bitmap in 'm_tmp' using readonly access to the series data.
					ThreadPool.QueueUserWorkItem(x =>
					{
						// Check the issue number
						var rdr_issue = (int)x;
						if (rdr_issue != m_rdr_issue)
							return;

						// Hold the rendering CS. Clients should hold this if they want to
						// modify the data while the graph is potentially rendering
						// Render the plot into 'm_tmp'
						lock (MutexRendering)
						{
							var g = Graphics.FromImage(m_tmp.m_bm);
							RenderData(g, m_tmp.Rect, rdr_issue);
						}

						// Swap the bitmaps on the GUI thread
						dispatcher.BeginInvoke(() =>
						{
							// If the render was cancelled, ignore the result
							if (rdr_issue != m_rdr_issue)
								return;

							// Otherwise get the main thread to do something with the plot bitmap
							Util.Swap(ref m_snap, ref m_tmp);

							// Cause a refresh
							Refresh();
						});
					}, m_rdr_issue);
				}

				// In the mean time, compose the graph in 'm_bm' by rendering the frame
				// synchronously and copy the last snapshot into the plot area
				RenderGraphFrame(gfx, area, m_plot_area);

				// Paint the plot area bitmap
				gfx.SetClip(m_plot_area.Shifted(1,1).Inflated(0,0,-1,-1));
				gfx.SmoothingMode = SmoothingMode.HighQuality;
				if (m_snap.m_bm != null)
				{
					var tl = GraphToPoint(new PointF((float)m_snap.m_xrange.Beg, (float)m_snap.m_yrange.End));
					var br = GraphToPoint(new PointF((float)m_snap.m_xrange.End, (float)m_snap.m_yrange.Beg));
					var dst_rect = Rectangle.FromLTRB((int)tl.X, (int)tl.Y, (int)br.X, (int)br.Y);
					var src_rect = m_snap.Rect;
					gfx.DrawImage(m_snap.m_bm, dst_rect, (int)src_rect.X, (int)src_rect.Y, (int)src_rect.Width, (int)src_rect.Height, GraphicsUnit.Pixel);
				}

				// Allow clients to draw on the graph
				var g2c = GraphToClientSpace(m_plot_area);
				OnAddOverlayOnPaint(new OverlaysEventArgs(gfx, g2c, m_plot_area));

				// Draw the selection rubber band
				if (m_selection.Width != 0 && m_selection.Height != 0)
				{
					var sel = m_selection.NormalizeRect();
					using (var pen = new Pen(Options.SelectionColour))
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
					gfx.DrawString("Rendering error within .NET", Options.TitleFont, bsh, PointF.Empty);
			}
		}

		/// <summary>Get the rectangular area of the graph for a given client area</summary>
		private Rectangle PlotArea(Graphics gfx, Rectangle area)
		{
			var rect = new RectangleF(0f, 0f, area.Width, area.Height);
			SizeF r;

			// Add margins
			rect.X      += Options.Margin.Left;
			rect.Y      += Options.Margin.Top;
			rect.Width  -= Options.Margin.Left + Options.Margin.Right;
			rect.Height -= Options.Margin.Top + Options.Margin.Bottom;

			// Add space for tick marks
			if (YAxis.Options.DrawTickMarks)
			{
				rect.X      += YAxis.Options.TickLength;
				rect.Width  -= YAxis.Options.TickLength;
			}
			if (XAxis.Options.DrawTickMarks)
			{
				rect.Height -= XAxis.Options.TickLength;
			}

			// Add space for the title and axis labels
			if (Title.HasValue())
			{
				r = gfx.MeasureString(Title, Options.TitleFont);
				rect.Y      += r.Height;
				rect.Height -= r.Height;
			}
			if (XAxis.Label.HasValue())
			{
				r = gfx.MeasureString(XAxis.Label, XAxis.Options.LabelFont);
				rect.Height -= r.Height;
			}
			if (YAxis.Label.HasValue())
			{
				r = gfx.MeasureString(YAxis.Label, YAxis.Options.LabelFont);
				rect.X     += r.Height; // will be rotated by 90deg
				rect.Width -= r.Height;
			}

			// Add space for the tick labels
			if (XAxis.Options.DrawTickLabels)
			{
				var tick_text = XAxis.TickText(XAxis.Min, 0.0);
				var sz = gfx.MeasureString(tick_text, XAxis.Options.TickFont);
				rect.Height -= Math.Max(sz.Height, XAxis.Options.MinTickSize);
			}
			if (YAxis.Options.DrawTickLabels)
			{
				var tick_text = YAxis.TickText(YAxis.Min, 0.0);
				var sz = gfx.MeasureString(tick_text, YAxis.Options.TickFont);
				rect.X     += Math_.Max(sz.Width, YAxis.Options.MinTickSize);
				rect.Width -= Math_.Max(sz.Width, YAxis.Options.MinTickSize);
			}

			return new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
		}

		/// <summary>Return the min, max, and step size for the x/y axes</summary>
		private void PlotGrid(Rectangle plot_area, out v2 min, out v2 max, out v2 step)
		{
			// Choose step sizes
			var max_ticks_x = plot_area.Width  / XAxis.Options.PixelsPerTick;
			var max_ticks_y = plot_area.Height / YAxis.Options.PixelsPerTick;
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
			gfx.Clear(Options.BkColour);

			// Draw the graph title and labels
			if (Title.HasValue())
			{
				using (var bsh = new SolidBrush(Options.TitleColour))
				{
					var r = gfx.MeasureString(Title, Options.TitleFont);
					var x = (float)(area.Width - r.Width) * 0.5f;
					var y = (float)(area.Top + Options.Margin.Top);
					gfx.TranslateTransform(x, y);
					gfx.MultiplyTransform(Options.TitleTransform);
					gfx.DrawString(Title, Options.TitleFont, bsh, PointF.Empty);
					gfx.ResetTransform();
				}
			}
			if (XAxis.Label.HasValue())
			{
				using (var bsh = new SolidBrush(XAxis.Options.LabelColour))
				{
					var r = gfx.MeasureString(XAxis.Label, XAxis.Options.LabelFont);
					var x = (float)(area.Width - r.Width) * 0.5f;
					var y = (float)(area.Bottom - Options.Margin.Bottom - r.Height);
					gfx.TranslateTransform(x, y);
					gfx.MultiplyTransform(XAxis.Options.LabelTransform);
					gfx.DrawString(XAxis.Label, XAxis.Options.LabelFont, bsh, PointF.Empty);
					gfx.ResetTransform();
				}
			}
			if (YAxis.Label.HasValue())
			{
				using (var bsh = new SolidBrush(YAxis.Options.LabelColour))
				{
					var r = gfx.MeasureString(YAxis.Label, YAxis.Options.LabelFont);
					var x = (float)(area.Left + Options.Margin.Left);
					var y = (float)(area.Height + r.Width) * 0.5f;
					gfx.TranslateTransform(x, y);
					gfx.RotateTransform(-90.0f);
					gfx.MultiplyTransform(YAxis.Options.LabelTransform);
					gfx.DrawString(YAxis.Label, YAxis.Options.LabelFont, bsh, PointF.Empty);
					gfx.ResetTransform();
				}
			}

			// Draw the graph frame and background
			using (var bsh_bkgd = new SolidBrush(Options.BkColour))
			using (var bsh_axis = new SolidBrush(Options.AxisColour))
			using (var pen_axis = new Pen(Options.AxisColour, 0.0f))
			{
				// Background
				RenderPlotBkgd(gfx, plot_area);

				v2 min,max,step;
				PlotGrid(plot_area, out min, out max, out step);

				// Tick marks and labels
				using (var bsh_xtick = new SolidBrush(XAxis.Options.TickColour))
				using (var bsh_ytick = new SolidBrush(YAxis.Options.TickColour))
				{
					var lblx = (float)(plot_area.Left - YAxis.Options.TickLength - 1);
					var lbly = (float)(plot_area.Top + plot_area.Height + XAxis.Options.TickLength + 1);
					if (XAxis.Options.DrawTickLabels || XAxis.Options.DrawTickMarks)
					{
						for (float x = min.x; x < max.x; x += step.x)
						{
							var X = (int)(plot_area.Left + x * plot_area.Width / XAxis.Span);
							var s = XAxis.TickText(x + XAxis.Min, step.x);
							var r = gfx.MeasureString(s, XAxis.Options.TickFont);
							if (XAxis.Options.DrawTickLabels)
								gfx.DrawString(s, XAxis.Options.TickFont, bsh_xtick, new PointF(X - r.Width*0.5f, lbly));
							if (XAxis.Options.DrawTickMarks)
								gfx.DrawLine(pen_axis, X, plot_area.Top + plot_area.Height, X, plot_area.Top + plot_area.Height + XAxis.Options.TickLength);
						}
					}
					if (YAxis.Options.DrawTickLabels || YAxis.Options.DrawTickMarks)
					{
						for (float y = min.y; y < max.y; y += step.y)
						{
							var Y = (int)(plot_area.Top + plot_area.Height - y * plot_area.Height / YAxis.Span);
							var s = YAxis.TickText(y + YAxis.Min, step.y);
							var r = gfx.MeasureString(s, YAxis.Options.TickFont);
							if (YAxis.Options.DrawTickLabels)
								gfx.DrawString(s, YAxis.Options.TickFont, bsh_ytick, new PointF(lblx - r.Width, Y - r.Height*0.5f));
							if (YAxis.Options.DrawTickMarks)
								gfx.DrawLine(pen_axis, plot_area.Left - YAxis.Options.TickLength, Y, plot_area.Left, Y);
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

		/// <summary>Render the plot background including grid lines</summary>
		private void RenderPlotBkgd(Graphics gfx, Rectangle plot_area)
		{
			using (var bsh_plot = new SolidBrush(Options.PlotBkColour))
			using (var pen_grid = new Pen(Options.GridColour, 0.0f))
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
		private void RenderData(Graphics gfx, Rectangle plot_area, int rdr_issue)
		{
			try
			{
				// Set clip bounds so we only draw in the graph area
				gfx.SetClip(plot_area.Shifted(1,1).Inflated(0, 0, -1,-1));

				// Render the background colour and grid lines
				RenderPlotBkgd(gfx, plot_area);

				// Not setting 'gfx.Transform' because we use double precision for the graph
				// data, but 'gfx.Transform' uses float which can cause overflow errors.
				// Instead, provide a double precision matrix that callers can use to convert to client space
				var g2c = GraphToClientSpace(plot_area);
				var scale_x = g2c.x.x;

				// Plot each series
				foreach (var series in Data)
				{
					// Give up if there's a newer DoPaint call
					if (rdr_issue != m_rdr_issue)
						break;

					var opts = series.Options;
					if (!opts.Visible)
						continue;

					using (var bsh_pt   = new SolidBrush(opts.PointColour))
					using (var bsh_bar  = new SolidBrush(opts.BarColour))
					using (var bsh_err  = new SolidBrush(opts.ErrorBarColour))
					using (var pen_line = new Pen(opts.LineColour, opts.LineWidth))
					using (var pen_bar = new Pen(opts.BarColour, 0.0f))
					using (var s = series.Lock())
					{
						// Loop over data points
						var indices = s.Indices(XAxis.Min, XAxis.Max);
						for (var iter = indices.GetIterator(); !iter.AtEnd;)
						{
							// Give up if there's a newer DoPaint call
							if (rdr_issue != m_rdr_issue)
								break;

							// Get the next data point
							var pt = new ScreenPoint(s.Data, plot_area, g2c, iter);

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
							s.IndexRange(XAxis.Min, XAxis.Max, out var i0, out var i1);
							i0 = Math.Max(0, i0 - opts.MAWindowSize);
							i1 = Math.Min(s.Data.Count, i1 + opts.MAWindowSize);
							PlotMovingAverage(gfx, opts, g2c, s.Values(i0, i1).GetIterator());
						}
					}
				}

				// Allow clients to draw on the graph
				OnAddOverlayOnRender(new OverlaysEventArgs(gfx, g2c, plot_area));

				gfx.ResetClip();
			}
			catch (Exception)
			{
				gfx.Clear(Options.BkColour);
				gfx.DrawString("Rendering failed, scale too extreme", Options.TitleFont, Brushes.Black, Point.Empty);
			}
		}

		/// <summary>Double precision 2D affine vector/matrix</summary>
		[DebuggerDisplay("{x} {y} {w}")]
		public struct Vector3
		{
			public double x, y, w;
			public Vector3(double x_, double y_, double w_)
			{
				Debug.Assert(w_ == 0.0 || w_ == 1);
				x = x_;
				y = y_;
				w = w_;
			}
			public Vector3(Point pt, double w)
				:this(pt.X, pt.Y, w)
			{}
			public Vector3(PointF pt, double w)
				:this(pt.X, pt.Y, w)
			{}
			public Point ToPoint()
			{
				return new Point((int)x, (int)y);
			}
			public PointF ToPointF()
			{
				return new PointF((float)x, (float)y);
			}
		}
		[DebuggerDisplay("{x} {y} {w}")]
		public struct Matrix3x3
		{
			public Vector3 x, y, w;

			public Matrix3x3(
				double _xx, double _xy,
				double _yx, double _yy,
				double _wx, double _wy)
			{
				x.x = _xx;  x.y = _xy;  x.w = 0.0;
				y.x = _yx;  y.y = _yy;  y.w = 0.0;
				w.x = _wx;  w.y = _wy;  w.w = 1.0;
			}

			/// <summary>Apply the transform to 'rhs'</summary>
			public Vector3 TransformPoint(Vector3 rhs)
			{
				return new Vector3(
					x.x*rhs.x + y.x*rhs.y + w.x*rhs.w,
					x.y*rhs.x + y.y*rhs.y + w.y*rhs.w,
					x.w*rhs.x + y.w*rhs.y + w.w*rhs.w);
			}
			public Vector3 TransformPoint(GraphValue rhs, double w)
			{
				return TransformPoint(new Vector3(rhs.x, rhs.y, w));
			}
			public PointF TransformPoint(PointF rhs, double w)
			{
				var pt = TransformPoint(new Vector3(rhs.X, rhs.Y, w));
				return new PointF((float)pt.x, (float)pt.y);
			}
		}

		/// <summary>A helper for rendering that finds the bounds of all points at the same screen space X position</summary>
		private class ScreenPoint
		{
			public IList<GraphValue> m_data;         // The series that this point came from
			public Rectangle         m_plot_area;    // The screen space area that the point must be within
			public Matrix3x3         m_g2c;          // Graph to client space transform
			public int               m_imin, m_imax; // The index range in 'm_series' of the data points included
			public double            m_xmin, m_xmax; // The graph-space X-range of data values that fall on the same client space X position
			public double            m_ymin, m_ymax; // The graph-space Y-range of data values that fall on the same client space X position
			public double            m_ylo , m_yhi ; // The graph-space bounds on the error bars of the Y values

			/// <summary>Scan 'iter' forward to the next data point that has a screen space X value not equal to that of the 'iter' data point</summary>
			public ScreenPoint(IList<GraphValue> data, Rectangle plot_area, Matrix3x3 g2c, Iterator<int> iter)
			{
				// Get the data point
				var gv0 = data[iter.Current];
				var cx0 = (long)(gv0.x * g2c.x.x);

				// Init members
				m_data      = data;
				m_plot_area = plot_area;
				m_g2c       = g2c;
				m_imin      = m_imax = iter.Current;
				m_xmin      = m_xmax = gv0.x;
				m_ymin      = m_ymax = gv0.y;
				m_ylo       = gv0.y + gv0.ylo;
				m_yhi       = gv0.y + gv0.yhi;

				// While the data point still represents the same X coordinate on-screen
				// scan forward until 'x != sx', finding the bounds on points that fall at this X.
				for (iter.MoveNext(); !iter.AtEnd; iter.MoveNext())
				{
					// Get the next data value, and find it's client space X value
					// If the client-space X value is not equal to 'cx0' then leave.
					var gv1 = data[iter.Current];
					var cx1 = (long)(gv1.x * g2c.x.x);
					if (cx1 != cx0) break;

					// Accumulate this graph data value
					m_imax = iter.Current;
					m_xmax = gv1.x;
					m_ymin = Math.Min(m_ymin , gv1.y);
					m_ymax = Math.Max(m_ymax , gv1.y);
					m_ylo  = Math.Min(m_ylo  , gv1.y + gv1.ylo);
					m_yhi  = Math.Max(m_yhi  , gv1.y + gv1.yhi);
				}
			}

			/// <summary>True if this is a single point, false if it represents multiple points</summary>
			public bool IsSingle
			{
				get { return m_imin == m_imax; }
			}

			/// <summary>
			/// Get the graph data point in client space.
			/// The graph data, compressed into a single X pixel, is a line.
			/// The width of the returned rectangle</summary>
			public RectangleF PointCS(bool error_bars = false, bool extend_to_zero = false, float width_scale = 0.0f)
			{
				// Get the graph space points as 'Vector3'
				var gs_min = new Vector3(m_xmin, (error_bars ? m_ylo : m_ymin), 1.0);
				var gs_max = new Vector3(m_xmin, (error_bars ? m_yhi : m_ymax), 1.0);
				
				// If returning a 'bar', extend the bar to the zero line
				if (extend_to_zero)
				{
					gs_min.y = Math.Min(gs_min.y, 0.0);
					gs_max.y = Math.Max(gs_max.y, 0.0f);
				}

				// Transform the graph space points to client space
				var pt_min = m_g2c.TransformPoint(gs_min);
				var pt_max = m_g2c.TransformPoint(gs_max);

				// Calculate bar widths
				float lhs = 0f, rhs = 0f;
				if (width_scale != 0)
				{
					// Calc the left and right side of the bar
					// Left side minimum size is 0, right side is 1. This is so that
					// the display doesn't alias like crazy when zoomed right out.
					if (m_imin != 0)
					{
						var prev_x = m_data[m_imin - 1].x;
						lhs = (float)Math.Max(0, 0.5*(m_xmin - prev_x) * m_g2c.x.x * width_scale);
					}
					if (m_imax+1 != m_data.Count)
					{
						var next_x = m_data[m_imax + 1].x;
						rhs = (float)Math.Max(1, 0.5*(next_x - m_xmax) * m_g2c.x.x * width_scale);
					}
					if (lhs == 0) lhs = rhs; // i_min == 0 case
					if (rhs == 0) rhs = lhs; // i_max == Count-1 case
				}

				// Return a rectangle representing the point in client space
				return RectangleF.FromLTRB(
					(float)(pt_min.x - lhs),  // X position - left side bar width
					(float)(pt_max.y      ),  // Y max position
					(float)(pt_max.x + rhs),  // X position + right side bar width
					(float)(pt_min.y      )); // Y min position
			}
		}

		/// <summary>Plot a point on the graph</summary>
		private void PlotPoint(Graphics gfx, ScreenPoint pt, Series.RdrOptions opts, SolidBrush bsh_pt, SolidBrush bsh_err)
		{
			// Draw error bars is on
			if (opts.DrawErrorBars)
				gfx.FillRectangle(bsh_err, pt.PointCS(error_bars:true, width_scale:1.0f));

			// Draw a vertical line if the screen point represents multiple points
			var r = pt.PointCS();
			if (!pt.IsSingle)
			{
				// Draw the stretched out point
				gfx.FillRectangle(bsh_pt, r.Left - opts.PointSize*0.5f, r.Top, opts.PointSize, r.Height);
				gfx.FillEllipse(bsh_pt, r.Left - opts.PointSize*0.5f, r.Top    - opts.PointSize*0.5f, opts.PointSize, opts.PointSize);
				gfx.FillEllipse(bsh_pt, r.Left - opts.PointSize*0.5f, r.Bottom - opts.PointSize*0.5f, opts.PointSize, opts.PointSize);
			}
			else
			{
				gfx.FillEllipse(bsh_pt, r.X - opts.PointSize*0.5f, r.Y - opts.PointSize*0.5f, opts.PointSize, opts.PointSize);
			}
		}

		/// <summary>Plot a line segment on the graph</summary>
		private void PlotLine(Graphics gfx, ScreenPoint pt, Series.RdrOptions opts, SolidBrush bsh_pt, Pen pen_line, SolidBrush bsh_err)
		{
			// Draw error bars is on
			if (opts.DrawErrorBars)
				gfx.FillRectangle(bsh_err, pt.PointCS(error_bars:true, width_scale:1.0f));

			// Plot the point and line
			if (opts.DrawData)
			{
				var r = pt.PointCS();

				// Draw the line from the previous point
				if (pt.m_imin != 0) // if this is not the first point
				{
					var prev = pt.m_g2c.TransformPoint(pt.m_data[pt.m_imin - 1], 1.0).ToPointF();
					var curr = pt.m_g2c.TransformPoint(pt.m_data[pt.m_imin    ], 1.0).ToPointF();
					gfx.DrawLine(pen_line, prev.X, prev.Y, curr.X, curr.Y);
				}

				// Draw a vertical line if the screen point represents multiple points
				if (!pt.IsSingle)
				{
					gfx.DrawLine(pen_line, r.Left, r.Top, r.Right, r.Bottom);

					// Draw the stretched out point
					if (opts.PointSize > 0)
					{
						gfx.FillRectangle(bsh_pt, r.Left - opts.PointSize*0.5f, r.Top, opts.PointSize, r.Height);
						gfx.FillEllipse(bsh_pt, r.Left - opts.PointSize*0.5f, r.Top    - opts.PointSize*0.5f, opts.PointSize, opts.PointSize);
						gfx.FillEllipse(bsh_pt, r.Left - opts.PointSize*0.5f, r.Bottom - opts.PointSize*0.5f, opts.PointSize, opts.PointSize);
					}
				}
				else
				{
					// Plot the point (if it's size is non-zero)
					if (opts.PointSize > 0)
						gfx.FillEllipse(bsh_pt, r.X - opts.PointSize*0.5f, r.Y - opts.PointSize*0.5f, opts.PointSize, opts.PointSize);
				}
			}
		}

		/// <summary>Plot a single bar on the graph</summary>
		private void PlotBar(Graphics gfx, ScreenPoint pt, Series.RdrOptions opts, SolidBrush bsh_bar, Pen pen_bar, SolidBrush bsh_err)
		{
			// Draw error bars is on
			if (opts.DrawErrorBars)
				gfx.FillRectangle(bsh_err, pt.PointCS(error_bars:true, width_scale:opts.BarWidth));

			// Plot the bar
			if (opts.DrawData)
			{
				var r = pt.PointCS(extend_to_zero:true, width_scale:opts.BarWidth);
				if (r.Height != 0)
					gfx.FillRectangle(bsh_bar, r);
				else 
					gfx.DrawLine(pen_bar, r.Left, r.Top, r.Right, r.Bottom);
			}
		}

		/// <summary>Plot a moving average curve over the data</summary>
		private void PlotMovingAverage(Graphics gfx, Series.RdrOptions opts, Matrix3x3 g2c, Iterator<GraphValue> iter)
		{
			var ema = new ExponentialMovingAverage(opts.MAWindowSize);
			using (var ma_pen = new Pen(opts.MALineColour, opts.MALineWidth))
			{
				bool first = true;
				var prev = Point.Empty;
				for (;!iter.AtEnd; iter.MoveNext())
				{
					var gv = iter.Current;
					ema.Add(gv.y);

					var curr = g2c.TransformPoint(new Vector3(gv.x, ema.Mean, 1.0)).ToPoint();
					if (first)
					{
						first = false;
						prev = curr;
					}
					else if (curr.X != prev.X)
					{
						gfx.DrawLine(ma_pen, prev.X, prev.Y, curr.X, curr.Y);
						prev = curr;
					}
				}
			}
		}
		#endregion

		#region Notes

		/// <summary>A note to add to the graph</summary>
		public class Note
		{
			public string m_msg;
			public PointF m_loc;
			public Color  m_colour;
			public Note(string msg, PointF loc) :this(msg, loc,  Color.Black) {}
			public Note(string msg, PointF loc, Color colour) {m_msg = msg; m_loc = loc; m_colour = colour;}
		}

		/// <summary>Notes to add to the graph</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public List<Note> Notes { get; private set; }

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
			var cmenu = new ContextMenuStrip { Renderer = new ContextMenuRenderer() };
			using (this.ChangeCursor(Cursors.WaitCursor))
			using (cmenu.SuspendLayout(true))
			{
				#region Tools
				{
					var tools_menu = cmenu.Items.Add2(new ToolStripMenuItem("Tools"));
					#region Show Value
					{
						var opt = tools_menu.DropDownItems.Add2(new ToolStripMenuItem("Show Value"));
						opt.Checked = (bool)m_tooltip.Tag;
						opt.Click += (s,a) =>
						{
							m_tooltip.Hide(this);
							if ((bool)m_tooltip.Tag) MouseMove -= OnMouseMoveTooltip;
							m_tooltip.Tag = !(bool)m_tooltip.Tag;
							if ((bool)m_tooltip.Tag) MouseMove += OnMouseMoveTooltip;
						};
					}
					#endregion
					#region Legend
					{
						var opt = tools_menu.DropDownItems.Add2(new ToolStripMenuItem("Legend"));
						opt.Checked = Legend.Visible;
						opt.Click += (s,a) =>
						{
							Legend.Visible = !Legend.Visible;
							if (Legend.Visible)
								Legend.BringToFront();
						};
					}
					#endregion
					#region Notes
					{
						var note_menu = tools_menu.DropDownItems.Add2(new ToolStripMenuItem("Notes"));
						#region Add
						{
							var opt = note_menu.DropDownItems.Add2(new ToolStripMenuItem("Add"));
							opt.Click += (s,e) =>
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
								var opt = note_menu.DropDownItems.Add2(new ToolStripMenuItem($"Delete '{nearest.m_msg.Summary(12)}'"));
								opt.Click += (s,e) =>
								{
									Notes.Remove(nearest);
									Dirty = true;
								};
							}
						}
						#endregion
					}
					#endregion
					#region Export
					{
						var opt = tools_menu.DropDownItems.Add2(new ToolStripMenuItem("Export"));
						opt.Click += (s,a) =>
						{
							ExportCSV();
						};
					}
					#endregion
					#region Import
					{
						var opt = tools_menu.DropDownItems.Add2(new ToolStripMenuItem("Import"));
						opt.Click += (s,a) =>
						{
							ImportCSV(null);
						};
					}
					#endregion
				}
				#endregion
				#region Zoom Menu
				{
					var zoom_menu = cmenu.Items.Add2(new ToolStripMenuItem("Zoom"));
					{
						var opt = zoom_menu.DropDownItems.Add2(new ToolStripMenuItem("Default"));
						opt.Click += (s,a) =>
						{
							FindDefaultRange();
							ResetToDefaultRange();
							Refresh();
						};
					}
					{
						var opt = zoom_menu.DropDownItems.Add2(new ToolStripMenuItem("In"));
						opt.Click += (s,a) =>
						{
							var point = PointToGraph(m_selection.Location);
							Zoom *= 0.5;
							PositionGraph(m_selection.Location,point);
							Refresh();
						};
					}
					{
						var opt = zoom_menu.DropDownItems.Add2(new ToolStripMenuItem("Out"));
						opt.Click += (s,a) =>
						{
							var point = PointToGraph(m_selection.Location);
							Zoom *= 2.0;
							PositionGraph(m_selection.Location,point);
							Refresh();
						};
					}
				}
				#endregion
				#region Series menus
				if (Data.Count != 0)
				{
					#region All Series
					{
						var menu = cmenu.Items.Add2(new ToolStripMenuItem("Series: All"));
						#region Visible
						{
							var state = Data.Aggregate(0, (x,s) => x | (s.Options.Visible ? 2 : 1));
							var option = menu.DropDownItems.Add2(new ToolStripMenuItem("Visible"));
							option.CheckState = state == 2 ? CheckState.Checked : state == 1 ? CheckState.Unchecked : CheckState.Indeterminate;
							option.Click += (s,a) =>
							{
								option.CheckState = option.CheckState == CheckState.Checked ? CheckState.Unchecked : CheckState.Checked;
								foreach (var x in Data) x.Options.Visible = option.CheckState == CheckState.Checked;
								Dirty = true;
							};
						}
						#endregion
					}
					#endregion
					#region Individual Series
					for (var index = 0; index != Data.Count; ++index)
					{
						var series = Data[index];

						// Create a sub menu for each series
						var menu = cmenu.Items.Add2(new ToolStripMenuItem("Series: " + series.Name));
						menu.ForeColor = series.Colour;
						menu.Checked = series.Options.Visible;
						menu.Tag = index;
						menu.Click += (s,a) =>
						{
							series.Options.Visible = !series.Options.Visible;
							menu.Checked = series.Options.Visible;
							Dirty = true;
						};

						#region Elements
						{
							var elements_menu = menu.DropDownItems.Add2(new ToolStripMenuItem("Elements"));
							#region Draw main data
							{
								var opt = elements_menu.DropDownItems.Add2(new ToolStripMenuItem("Series data"));
								opt.Checked = series.Options.DrawData;
								opt.Click += (s,a) =>
								{
									series.Options.DrawData = !series.Options.DrawData;
									Dirty = true;
								};
							}
							#endregion
							#region Draw error bars
							{
								var opt = elements_menu.DropDownItems.Add2(new ToolStripMenuItem("Error Bars"));
								opt.Checked = series.Options.DrawErrorBars;
								opt.Click += (s,a) =>
								{
									series.Options.DrawErrorBars = !series.Options.DrawErrorBars;
									Dirty = true;
								};
							}
							#endregion
						}
						#endregion
						#region Plot Type Menu
						{
							var plot_type_menu = menu.DropDownItems.Add2(new ToolStripMenuItem("Plot Type"));
							{
								var opt = plot_type_menu.DropDownItems.Add2(new ToolStripComboBox { DropDownStyle = ComboBoxStyle.DropDownList });
								opt.Items.AddRange(Enum<Series.RdrOptions.EPlotType>.Names.Cast<object>().ToArray());
								opt.SelectedIndex = (int)series.Options.PlotType;
								opt.SelectedIndexChanged += (s,a) =>
								{
									series.Options.PlotType = (Series.RdrOptions.EPlotType)opt.SelectedIndex;
									Dirty = true;
								};
								opt.KeyDown += (s,a) =>
								{
									if (a.KeyCode == Keys.Return)
										cmenu.Close();
								};
							}
						}
						#endregion
						#region Appearance Menu
						{
							var appearance_menu = menu.DropDownItems.Add2(new ToolStripMenuItem("Appearance"));
							#region Points
							if (series.Options.PlotType == Series.RdrOptions.EPlotType.Point || series.Options.PlotType == Series.RdrOptions.EPlotType.Line)
							{
								var point_menu = appearance_menu.DropDownItems.Add2(new ToolStripMenuItem("Points"));
								#region Size
								{
									var size_menu = point_menu.DropDownItems.Add2(new ToolStripMenuItem("Size"));
									{
										var opt = size_menu.DropDownItems.Add2(new ToolStripTextBox{AcceptsReturn = false});
										opt.Text = series.Options.PointSize.ToString("0.00");
										opt.TextChanged += (s,a) =>
										{
											float size;
											if (!float.TryParse(opt.Text,out size)) return;
											series.Options.PointSize = size;
											Dirty = true;
										};
									}
								}
								#endregion
								#region Colour
								{
									var colour_menu = point_menu.DropDownItems.Add2(new ToolStripMenuItem("Colour"));
									{
										var opt = colour_menu.DropDownItems.Add2(new NoHighlightToolStripMenuItem("     "){DisplayStyle = ToolStripItemDisplayStyle.Text, BackColor = series.Options.PointColour});
										opt.Click += (s,a) =>
										{
											var cd = new ColourUI{InitialColour = opt.BackColor};
											if (cd.ShowDialog() != DialogResult.OK) return;
											series.Options.PointColour = cd.Colour;
											Dirty = true;
										};
									}
								}
								#endregion
							}
							#endregion
							#region Lines
							if (series.Options.PlotType == Series.RdrOptions.EPlotType.Line)
							{
								var line_menu = appearance_menu.DropDownItems.Add2(new ToolStripMenuItem("Lines"));
								#region Width
								{
									var width_menu = line_menu.DropDownItems.Add2(new ToolStripMenuItem("Width"));
									{
										var opt = width_menu.DropDownItems.Add2(new ToolStripTextBox());
										opt.Text = series.Options.LineWidth.ToString("0.00");
										opt.TextChanged += (s,a) =>
										{
											float width;
											if (!float.TryParse(opt.Text,out width)) return;
											series.Options.LineWidth = width;
											Dirty = true;
										};
									}
								}
								#endregion
								#region Colour
								{
									var colour_menu = appearance_menu.DropDownItems.Add2(new ToolStripMenuItem("Colour"));
									{
										var opt = colour_menu.DropDownItems.Add2(new NoHighlightToolStripMenuItem("     "){DisplayStyle = ToolStripItemDisplayStyle.Text, BackColor = series.Options.LineColour});
										opt.Click += (s,a) =>
										{
											var cd = new ColourUI{InitialColour = opt.BackColor};
											if (cd.ShowDialog() != DialogResult.OK) return;
											series.Options.LineColour = cd.Colour;
											Dirty = true;
										};
									}
								}
								#endregion
							}
							#endregion
							#region Bars
							if (series.Options.PlotType == Series.RdrOptions.EPlotType.Bar)
							{
								var bar_menu = menu.DropDownItems.Add2(new ToolStripMenuItem("Bars"));
								#region Width
								{
									var width_menu = bar_menu.DropDownItems.Add2(new ToolStripMenuItem("Width"));
									{
										var opt = width_menu.DropDownItems.Add2(new ToolStripTextBox());
										opt.Text = series.Options.BarWidth.ToString("0.00");
										opt.TextChanged += (s,a) =>
										{
											float width;
											if (!float.TryParse(opt.Text,out width)) return;
											series.Options.BarWidth = width;
											Dirty = true;
										};
										opt.KeyDown += (s,a) =>
										{
											if (a.KeyCode == Keys.Return)
												cmenu.Close();
										};
									}
								}
								#endregion
								#region Bar Colour
								{
									var colour_menu = bar_menu.DropDownItems.Add2(new ToolStripMenuItem("Colour"));
									{
										var opt = colour_menu.DropDownItems.Add2(new NoHighlightToolStripMenuItem("     "){DisplayStyle = ToolStripItemDisplayStyle.Text, BackColor = series.Options.BarColour});
										opt.Click += (s,a) =>
										{
											var cd = new ColourUI{InitialColour = opt.BackColor};
											if (cd.ShowDialog() != DialogResult.OK) return;
											series.Options.BarColour = cd.Colour;
											Dirty = true;
										};
									}
								}
								#endregion
							}
							#endregion
							#region Error Bars
							if (series.Options.DrawErrorBars)
							{
								var errorbar_menu = appearance_menu.DropDownItems.Add2(new ToolStripMenuItem("Error Bars"));
								#region Colour
								{
									var colour_menu = errorbar_menu.DropDownItems.Add2(new ToolStripMenuItem("Colour"));
									{
										var opt = colour_menu.DropDownItems.Add2(new NoHighlightToolStripMenuItem("     "){DisplayStyle = ToolStripItemDisplayStyle.Text, BackColor = series.Options.ErrorBarColour});
										opt.Click += (s,a) =>
										{
											var cd = new ColourUI{InitialColour = opt.BackColor};
											if (cd.ShowDialog() != DialogResult.OK) return;
											series.Options.ErrorBarColour = cd.Colour;
											Dirty = true;
										};
									}
								}
								#endregion
							}
							#endregion
						}
						#endregion
						#region Moving Average
						{
							var mv_avr_menu = menu.DropDownItems.Add2(new ToolStripMenuItem("Moving Average"));
							#region Draw Moving Average
							{
								var opt = mv_avr_menu.DropDownItems.Add2(new ToolStripMenuItem("Draw Moving Average"));
								opt.Checked = series.Options.DrawMovingAvr;
								opt.Click += (s,a) =>
								{
									series.Options.DrawMovingAvr = !series.Options.DrawMovingAvr;
									Dirty = true;
								};
							}
							#endregion
							#region Window Size
							{
								var win_size_menu = mv_avr_menu.DropDownItems.Add2(new ToolStripMenuItem("Window Size"));
								{
									var opt = win_size_menu.DropDownItems.Add2(new ToolStripTextBox());
									opt.Text = series.Options.MAWindowSize.ToString(CultureInfo.InvariantCulture);
									opt.TextChanged += (s,a) =>
									{
										int size;
										if (!int.TryParse(opt.Text,out size)) return;
										series.Options.MAWindowSize = size;
										Dirty = true;
									};
								}
							}
							#endregion
							#region Line Width
							{
								var line_width_menu = mv_avr_menu.DropDownItems.Add2(new ToolStripMenuItem("Line Width"));
								{
									var opt = line_width_menu.DropDownItems.Add2(new ToolStripTextBox());
									opt.Text = series.Options.MALineWidth.ToString(CultureInfo.InvariantCulture);
									opt.TextChanged += (s,a) =>
									{
										int size;
										if (!int.TryParse(opt.Text,out size)) return;
										series.Options.MALineWidth = size;
										Dirty = true;
									};
								}
							}
							#endregion
							#region Line Colour
							{
								var line_colour_menu = mv_avr_menu.DropDownItems.Add2(new ToolStripMenuItem("Line Colour"));
								{
									var opt = line_colour_menu.DropDownItems.Add2(new NoHighlightToolStripMenuItem("     "){DisplayStyle = ToolStripItemDisplayStyle.Text, BackColor = series.Options.MALineColour});
									opt.Click += (s,a) =>
									{
										var cd = new ColourUI{InitialColour = opt.BackColor};
										if (cd.ShowDialog() != DialogResult.OK) return;
										series.Options.MALineColour = cd.Colour;
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
							var opt = menu.DropDownItems.Add2(new ToolStripMenuItem("Delete"));
							opt.Click += (s,a) =>
							{
								if (MessageBox.Show(this,"Confirm delete?","Delete Series",MessageBoxButtons.YesNo,MessageBoxIcon.Warning) != DialogResult.Yes) return;
								Data.RemoveAt((int)menu.Tag);
								FindDefaultRange();
								Dirty = true;
							};
						}
						#endregion
					}
					#endregion
				}
				#endregion
				#region Rendering Options
				{
					var appearance_menu = cmenu.Items.Add2(new ToolStripMenuItem("Appearance"));
					#region Frame Background Colour
					{
						var fbc_menu = appearance_menu.DropDownItems.Add2(new ToolStripMenuItem("Frame Background Colour"));
						{
							var opt = fbc_menu.DropDownItems.Add2(new NoHighlightToolStripMenuItem("     "){DisplayStyle = ToolStripItemDisplayStyle.Text, BackColor = Options.BkColour});
							opt.Click += (s,a) =>
							{
								var cd = new ColourUI{InitialColour = opt.BackColor};
								if (cd.ShowDialog() != DialogResult.OK) return;
								Options.BkColour = cd.Colour;
								Dirty = true;
							};
						}
					}
					#endregion
					#region Plot Background Colour
					{
						var pbc_menu = appearance_menu.DropDownItems.Add2(new ToolStripMenuItem("Plot Background Colour"));
						{
							var opt = pbc_menu.DropDownItems.Add2(new NoHighlightToolStripMenuItem("     "){DisplayStyle = ToolStripItemDisplayStyle.Text, BackColor = Options.BkColour});
							opt.Click += (s,a) =>
							{
								var cd = new ColourUI{InitialColour = opt.BackColor};
								if (cd.ShowDialog() != DialogResult.OK) return;
								Options.PlotBkColour = cd.Colour;
								Dirty = true;
							};
						}
					}
					#endregion
					#region Opacity
					if (ParentForm != null)
					{
						var opacity_menu = appearance_menu.DropDownItems.Add2(new ToolStripMenuItem("Opacity"));
						{
							var opt = opacity_menu.DropDownItems.Add2(new ToolStripTextBox());
							opt.Text = ParentForm.Opacity.ToString("0.00");
							opt.TextChanged += (s,a) =>
							{
								double opc;
								if (ParentForm != null && double.TryParse(opt.Text,out opc))
									ParentForm.Opacity = Math.Max(0.1,Math.Min(1.0,opc));
							};
						}
					}
					#endregion
				}
				#endregion
				#region Axis Options
				{
					var axes_menu = cmenu.Items.Add2(new ToolStripMenuItem("Axes"));
					#region X Axis
					{
						var xaxis_menu = axes_menu.DropDownItems.Add2(new ToolStripMenuItem("X Axis"));
						#region Min
						{
							var min_menu = xaxis_menu.DropDownItems.Add2(new ToolStripMenuItem("Min"));
							{
								var opt = min_menu.DropDownItems.Add2(new ToolStripTextBox());
								opt.Text = XAxis.Min.ToString("G5");
								opt.TextChanged += (s,a) =>
								{
									float v;
									if (!float.TryParse(opt.Text,out v)) return;
									XAxis.Min = v;
									Dirty = true;
								};
							}
						}
						#endregion
						#region Max
						{
							var max_menu = xaxis_menu.DropDownItems.Add2(new ToolStripMenuItem("Max"));
							{
								var opt = max_menu.DropDownItems.Add2(new ToolStripTextBox());
								opt.Text = XAxis.Max.ToString("G5");
								opt.TextChanged += (s,a) =>
								{
									float v;
									if (!float.TryParse(opt.Text,out v)) return;
									XAxis.Max = v;
									Dirty = true;
								};
							}
						}
						#endregion
						#region Allow Scroll
						{
							var opt = xaxis_menu.DropDownItems.Add2(new ToolStripMenuItem { Text = "Allow Scroll", Checked = XAxis.AllowScroll });
							opt.Click += (s,a) =>
							{
								XAxis.AllowScroll = !XAxis.AllowScroll;
							};
						}
						#endregion
						#region Allow Zoom
						{
							var opt = xaxis_menu.DropDownItems.Add2(new ToolStripMenuItem { Text = "Allow Zoom", Checked = XAxis.AllowZoom });
							opt.Click += (s,e) =>
							{
								XAxis.AllowZoom = !XAxis.AllowZoom;
							};
						}
						#endregion
					}
					#endregion
					#region Y Axis
					{
						var yaxis_menu = axes_menu.DropDownItems.Add2(new ToolStripMenuItem("Y Axis"));
						#region Min
						{
							var min_menu = yaxis_menu.DropDownItems.Add2(new ToolStripMenuItem("Min"));
							{
								var opt = min_menu.DropDownItems.Add2(new ToolStripTextBox());
								opt.Text = YAxis.Min.ToString("G5");
								opt.TextChanged += (s,a) =>
								{
									float v;
									if (!float.TryParse(opt.Text,out v)) return;
									YAxis.Min = v;
									Dirty = true;
								};
							}
						}
						#endregion
						#region Max
						{
							var max_menu = axes_menu.DropDownItems.Add2(new ToolStripMenuItem("Max"));
							{
								var opt = max_menu.DropDownItems.Add2(new ToolStripTextBox());
								opt.Text = YAxis.Max.ToString("G5");
								opt.TextChanged += (s,a) =>
								{
									float v;
									if (!float.TryParse(opt.Text,out v)) return;
									YAxis.Max = v;
									Dirty = true;
								};
							}
						}
						#endregion
						#region Allow Scroll
						{
							var opt = yaxis_menu.DropDownItems.Add2(new ToolStripMenuItem { Text = "Allow Scroll", Checked = YAxis.AllowScroll });
							opt.Click += (s,e) =>
							{
								YAxis.AllowScroll = !YAxis.AllowScroll;
							};
						}
						#endregion
						#region Allow Zoom
						{
							var opt = yaxis_menu.DropDownItems.Add2(new ToolStripMenuItem { Text = "Allow Zoom", Checked = YAxis.AllowZoom });
							opt.Click += (s,e) =>
							{
								YAxis.AllowZoom = !YAxis.AllowZoom;
							};
						}
						#endregion
					}
					#endregion
				}
				#endregion

				// Allow users to add menu options
				OnAddUserMenuOptions(new AddUserMenuOptionsEventArgs(cmenu));
			}
			cmenu.Closed += (s,a) => Refresh();
			cmenu.Show(this, location);
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
		/// Rendering is in client space, use 'g2c' to transform from graph to client space</summary>
		public event EventHandler<OverlaysEventArgs> AddOverlayOnRender;
		protected virtual void OnAddOverlayOnRender(OverlaysEventArgs args)
		{
			AddOverlayOnRender?.Invoke(this, args);
		}

		/// <summary>
		/// Called each time the cached plot bitmap is drawn to the control to allow clients to add graphics.<para/>
		/// The overlays can change without affecting cached graph bitmap.<para/>
		/// Rendering is in client space, use 'g2c' to transform from graph to client space</summary>
		public event EventHandler<OverlaysEventArgs> AddOverlayOnPaint;
		protected virtual void OnAddOverlayOnPaint(OverlaysEventArgs args)
		{
			AddOverlayOnPaint?.Invoke(this, args);
		}

		/// <summary>Event allowing callers to add options to the context menu</summary>
		public event EventHandler<AddUserMenuOptionsEventArgs> AddUserMenuOptions;
		protected virtual void OnAddUserMenuOptions(AddUserMenuOptionsEventArgs args)
		{
			AddUserMenuOptions?.Invoke(this, args);
		}

		/// <summary>User overlay event args</summary>
		public class OverlaysEventArgs :EventArgs
		{
			public OverlaysEventArgs(Graphics gfx, Matrix3x3 g2c, Rectangle plot_area)
			{
				Gfx = gfx;
				G2C = g2c;
				PlotArea = plot_area;
			}

			/// <summary>The graphics interface to use for drawing</summary>
			public Graphics Gfx { get; private set; }

			/// <summary>The graph to client space transform</summary>
			public Matrix3x3 G2C { get; private set; }

			/// <summary>Client-space plot area of the graph</summary>
			public Rectangle PlotArea { get; private set; }
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

		/// <summary>Event args for FindingDefaultRange</summary>
		public class FindingDefaultRangeEventArgs :EventArgs
		{
			public FindingDefaultRangeEventArgs(EAxis axis, RangeF xrange, RangeF yrange)
			{
				Axis = axis;
				XRange = xrange;
				YRange = yrange;
			}

			/// <summary>The axes being ranged</summary>
			public EAxis Axis { get; private set; }

			/// <summary>The XAxis range calculated from known chart elements data</summary>
			public RangeF XRange { get; set; }

			/// <summary>The YAxis range calculated from known chart elements data</summary>
			public RangeF YRange { get; set; }
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
			if (exp.ShowDialog() != DialogResult.OK)
				return;

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
						using (var s = Data[i].Lock())
							foreach (var gv in s.Data)
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
					using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
					using (var sr = new StreamReader(fs))
					{
						for (;;)
						{
							var series = new Series{ AllowDelete = true };
							using (var s = series.Lock())
							{
								// Peek at the first line
								var fpos = fs.Position;
								var line = sr.ReadLine();
								if (line == null) break;

								// How many tokens can it be split into?
								var tokens = line.Split(',', '\n');

								// If there is only 1 token, then this is the series name
								// Otherwise, there is no series name and the first line is probably data
								if (tokens.Length == 1)
									series.Name = tokens[0];
								else
									fs.Position = fpos;

								// Read the rest of the series data
								for (;;)
								{
									fpos = fs.Position;
									line = sr.ReadLine();
									if (line == null) break;

									// How many tokens can it be split into?
									tokens = line.Split(',', '\n');
									if (tokens.Length == 1)
									{
										// One token is the start of the next series
										fs.Position = fpos;
										break;
									}
									else if (tokens.Length == 4)
									{
										if (double.TryParse(tokens[0], out var v0) &&
											double.TryParse(tokens[1], out var v1) &&
											double.TryParse(tokens[2], out var v2) &&
											double.TryParse(tokens[3], out var v3))
											s.Data.Add(new GraphValue(v0, v1, v2, v3, null));
									}
								}

								// Add the series
								if (s.Data.Count != 0)
									Data.Add(series);
							}
						}
					}
				}
				catch (Exception ex)
				{
					if (ImportCSVError != null) ImportCSVError(ex);
					else MessageBox.Show(this, $"Import failed for '{filename}'.\n{ex.Message}", "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
			}
			FindDefaultRange();
		}

		#endregion

		#region Legend

		/// <summary>A floating legend window</summary>
		public ToolForm Legend { get; private set; }
		public class LegendUI :ToolForm
		{
			private readonly GraphControl m_graph;
			private BorderResizer m_border_resizer;
			private BindingSource<Series> m_bs;
			private Grid m_grid;

			public LegendUI(GraphControl graph) :base(graph, EPin.TopRight)
			{
				Font = new Font(FontFamily.GenericSansSerif, 6f, FontStyle.Regular, GraphicsUnit.Point);
				FormBorderStyle = FormBorderStyle.None;
				ShowInTaskbar = false;
				ShowIcon = false;
				AutoFade = true;
				FadeRange = new RangeF(0.5f, 1.0f);
				HideOnClose = true;
				Padding = new Padding(5);

				m_graph = graph;
				m_bs = new BindingSource<Series> { DataSource = m_graph.Data };
				m_grid = new Grid { DataSource = m_bs };
				m_border_resizer = new BorderResizer(this, 5);
				Controls.Add(m_grid);

				ClientSize = new Size(80, m_grid.ColumnHeadersHeight + 8 * m_grid.RowTemplate.Height);
			}
			protected override void Dispose(bool disposing)
			{
				Util.Dispose(ref m_border_resizer);
				base.Dispose(disposing);
			}
			protected override CreateParams CreateParams
			{
				get
				{
					var cp = base.CreateParams;
					cp.ClassStyle |= Win32.CS_DROPSHADOW;
					return cp;
				}
			}

			private class Grid :DataGridView
			{
				public Grid()
				{
					Dock = DockStyle.Fill;
					AllowUserToAddRows = false;
					AllowUserToOrderColumns = false;
					AllowUserToResizeRows = false;
					AutoGenerateColumns = false;
					BackgroundColor = SystemColors.Window;
					BorderStyle = BorderStyle.None;
					CellBorderStyle = DataGridViewCellBorderStyle.None;
					SelectionMode = DataGridViewSelectionMode.FullRowSelect;
					AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
					RowHeadersVisible = false;
					ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
					ColumnHeadersHeight = Font.Height + 8;
					RowTemplate = new DataGridViewRow
					{
						Height = Font.Height + 4,
					};
					DefaultCellStyle = new DataGridViewCellStyle
					{
						Font = Font,
					};

					Columns.Add(new DataGridViewTextBoxColumn
					{
						HeaderText = "Series",
						DataPropertyName = nameof(Series.Name),
						SortMode = DataGridViewColumnSortMode.NotSortable,
					});
				}
				protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs args)
				{
					base.OnCellFormatting(args);
					args.CellStyle.ForeColor = ((IList<Series>)DataSource)[args.RowIndex].Colour;
					args.CellStyle.SelectionForeColor = args.CellStyle.ForeColor;
					args.CellStyle.SelectionBackColor = args.CellStyle.BackColor;
				}
				protected override void OnCellMouseDown(DataGridViewCellMouseEventArgs e)
				{
					if (e.RowIndex == -1)
					{
						m_ofs = Point_.Subtract(MousePosition, TopLevelControl.Location);
						Capture = true;
					}
					base.OnCellMouseDown(e);
				}
				protected override void OnCellMouseMove(DataGridViewCellMouseEventArgs e)
				{
					if (m_ofs != null)
					{
						TopLevelControl.Location = MousePosition - m_ofs.Value;
					}
					base.OnCellMouseMove(e);
				}
				protected override void OnCellMouseUp(DataGridViewCellMouseEventArgs e)
				{
					m_ofs = null;
					Capture = false;
					base.OnCellMouseUp(e);
				}
				private Size? m_ofs;
			}
		}

		#endregion

		#region Misc
		private static class XmlTag
		{
			public const string BkColour        = "bk_colour";
			public const string PlotBkColour    = "plot_bk_colour";
			public const string AxisColour      = "axis_colour";
			public const string GridColour      = "grid_colour";
			public const string TitleColour     = "title_colour";
			public const string TitleFont       = "title_font";
			public const string TitleTransform  = "title_transform";
			public const string Margin          = "margin";
			public const string NoteFont        = "note_font";
			public const string SelectionColour = "selection_colour";
			public const string XAxis           = "x_axis";
			public const string YAxis           = "y_axis";
			public const string LabelFont       = "label_font";
			public const string TickFont        = "tick_font";
			public const string LabelColour     = "label_colour";
			public const string TickColour      = "tick_colour";
			public const string TickLength      = "tick_length";
			public const string MinTickSize     = "min_tick_size";
			public const string DrawTickMarks   = "draw_tick_marks";
			public const string DrawTickLabels  = "draw_tick_labels";
			public const string LabelTransform  = "label_transform";
			public const string PixelsPerTick   = "pixels_per_tick";
		}
		#endregion

	}
}