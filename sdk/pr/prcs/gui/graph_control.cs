//***************************************************
// Copyright © Rylogic Ltd 2008
//***************************************************
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace pr.gui
{
	/// <summary>Custom control for rendering a graph</summary>
	public class GraphControl :UserControl ,ISupportInitialize
	{
		/// <summary>Graph data points</summary>
		public class GraphValue
		{
			public readonly float m_valueX;     // Data point X value
			public readonly float m_valueY;     // Data point Y value
			public readonly float m_upper_err;  // Upper error bar for the y value
			public readonly float m_lower_err;  // Lower error bar for the y value
			public object m_tag;                // Allow users to attach data to each value in the graph
			public override string ToString() { return "(" + m_valueX + "," + m_valueY + ")"; }

			public GraphValue(double valueX, double valueY)                                                 :this((float)valueX, (float)valueY, 0f, 0f, null) {}
			public GraphValue(double valueX, double valueY, object tag)                                     :this((float)valueX, (float)valueY, 0f, 0f, tag) {}
			public GraphValue(double valueX, double valueY, double lower_err, double upper_err, object tag) :this((float)valueX, (float)valueY, (float)lower_err, (float)upper_err, tag) {}
			public GraphValue(float valueX, float valueY)                                                   :this(valueX, valueY, 0f, 0f, null) {}
			public GraphValue(float valueX, float valueY, object tag)                                       :this(valueX, valueY, 0f, 0f, tag) {}
			public GraphValue(float valueX, float valueY, float lower_err, float upper_err, object tag) 
			{
				Debug.Assert(!float.IsNaN(valueX) && !float.IsNaN(valueY));
				m_valueX = valueX;
				m_valueY = valueY;
				m_lower_err = lower_err;
				m_upper_err = upper_err;
				m_tag = tag;
			}
		}
		class SortX :IComparer<GraphValue>
		{
			public int Compare(GraphValue lhs, GraphValue rhs) { return (lhs.m_valueX > rhs.m_valueX ? 1 : 0) - (lhs.m_valueX < rhs.m_valueX ? 1 : 0); }
		}

		/// <summary>Graph data series</summary>
		public class Series
		{
			public class RdrOpts
			{
				public enum PlotType { Point, Line, Bar }
				public enum PlotZeros { Draw, Hide, Skip }
				public bool      m_visible          = true;
				public bool      m_draw_data        = true;
				public PlotZeros m_draw_zeros       = PlotZeros.Draw;
				public bool      m_draw_error_bars  = true;
				public PlotType  m_plot_type        = PlotType.Line;
				public Color     m_point_colour     = Color.FromArgb(0xff, 0x80, 0, 0);
				public float     m_point_size       = 5f;
				public Color     m_line_colour      = Color.FromArgb(0xff, 0, 0, 0);
				public float     m_line_width       = 1f;
				public Color     m_bar_colour       = Color.FromArgb(0xff, 0x80, 0, 0);
				public float     m_bar_width        = 0.8f;
				public Color     m_error_bar_colour = Color.FromArgb(0x80, 0xff, 0, 0);
				public bool      m_draw_moving_avr  = false;
				public int       m_ma_window_size   = 10;
				public Color     m_ma_line_colour   = Color.FromArgb(0xff, 0, 0, 0xFF);
				public float     m_ma_line_width    = 3f;
				public RdrOpts Clone() { return (RdrOpts)MemberwiseClone(); }
			}
			private readonly List<GraphValue> m_values = new List<GraphValue>();
			private RdrOpts m_rdr_options = new RdrOpts();
			private string m_name = "";
			private bool m_allow_delete = false;
			private bool m_sorted = true;           // Assume sorted because this is more efficient for rendering

			public Series()                                            {}
			public Series(string name)                                 { m_name = name; }
			public Series(string name, int capacity)                   { m_name = name; m_values = new List<GraphValue>(capacity); }
			public Series(string name, int capacity, RdrOpts rdr_opts) { m_name = name; m_values = new List<GraphValue>(capacity); m_rdr_options = rdr_opts; }
			public Series(Series src)                                  { m_name = src.m_name; m_rdr_options = src.m_rdr_options.Clone(); m_values = new List<GraphValue>(src.m_values); }
			public string           Name          { get { return m_name; } set { m_name = value; } }
			public List<GraphValue> Values        { get { return m_values; } }
			public RdrOpts          RenderOptions { get { return m_rdr_options; } set { m_rdr_options = value; } }
			public bool             AllowDelete   { get { return m_allow_delete; } set { m_allow_delete = value; } }
			public bool             Sorted        { get { return m_sorted; } set { m_sorted = value; } }
			public void             Sort()        { m_values.Sort(new SortX()); m_sorted = true; }
		}

		/// <summary>Graph axis data</summary>
		public class Axis
		{
			/// <summary>The range of values along this axis</summary>
			public class Range
			{
				public float m_min;    // The minimum value of the axis
				public float m_max;    // The maximum value of the axis
				public float m_step;   // The step size along the axis

				public Range()                                 { m_min = 0f; m_max = 1f; m_step = 0.1f; }
				public Range(float min, float max, float step) { m_min = min; m_max = max; m_step = step; }
				public float m_span                            { get {return Math.Max(m_max-m_min, float.Epsilon);} set {float c = m_centre; m_min = c - 0.5f*value; m_max = c + 0.5f*value;} }
				public float m_centre                          { get {return m_min + m_span*0.5f;} set {float d = value - m_centre; m_min += d; m_max += d; } }
				public Range Clone()                           { return (Range)MemberwiseClone(); }
			}
			
			/// <summary>Options related to rendering this axis</summary>
			public class RdrOpts
			{
				public Font m_label_font    = new Font("tahoma", 10, FontStyle.Regular);
				public Font m_tick_font     = new Font("tahoma", 8, FontStyle.Regular);
				public Color m_axis_colour  = Color.Black;   // The colour of the main axes
				public Color m_label_colour = Color.Black;   // The colour of the label text
				public Color m_tick_colour  = Color.Black;   // The colour of the tick text
				public int m_tick_length    = 5;             // The length of the tick marks
				public RdrOpts Clone()      { return (RdrOpts)MemberwiseClone(); }
			}
			private string           m_label;         // A label for the axis
			private readonly Range   m_range;         // The range of values along this axis
			private readonly RdrOpts m_rdr_options;   // Rendering options for this axis
			private bool             m_allow_scroll;  // Allow scrolling on this axis
			private bool             m_allow_zoom;    // Allow zooming on this axis
			private bool             m_lock_range;    // 
			
			public string  Label         { get { return m_label;        } set { m_label = value; } }
			public Range   Rng           { get { return m_range;        } set { Min = value.m_min; Max = value.m_max; Step = value.m_step; } }
			public float   Min           { get { return m_range.m_min;  } set { if (!float.IsNaN(value) && !float.IsInfinity(value)) m_range.m_min = value; } }
			public float   Max           { get { return m_range.m_max;  } set { if (!float.IsNaN(value) && !float.IsInfinity(value)) m_range.m_max = value; } }
			public float   Step          { get { return m_range.m_step; } set { if (!float.IsNaN(value) && !float.IsInfinity(value)) m_range.m_step = Math.Max(value, float.Epsilon); } }
			public float   Span          { get { return m_range.m_span; } set { if (!float.IsNaN(value) && !float.IsInfinity(value)) m_range.m_span = Math.Max(value, float.Epsilon); } }
			public bool    AllowScroll   { get { return m_allow_scroll; } set { m_allow_scroll = value; } }
			public bool    AllowZoom     { get { return m_allow_zoom;   } set { m_allow_zoom = value; } }
			public bool    LockRange     { get { return m_lock_range;   } set { m_lock_range = value; } }
			public RdrOpts RenderOptions { get { return m_rdr_options;  } }

			public Func<double,string> TickText; // Convert the axis value to a string

			public Axis()
			{
				m_label        = "";
				m_range        = new Range();
				m_rdr_options  = new RdrOpts();
				m_allow_scroll = true;
				m_allow_zoom   = true;
				m_lock_range   = false;
				TickText       = (x)=> {return x.ToString("G5");};
			}
			public Axis(Axis rhs)
			{
				m_label        = rhs.m_label;
				m_range        = rhs.m_range.Clone();
				m_rdr_options  = rhs.m_rdr_options.Clone();
				m_allow_scroll = rhs.m_allow_scroll;
				m_allow_zoom   = rhs.m_allow_zoom;
				m_lock_range   = rhs.m_lock_range;
				TickText       = rhs.TickText;
			}
			public void Shift(float delta)
			{
				if (!AllowScroll) return;
				Min += delta;
				Max += delta;
			}
			public void SetSuitableStepSize(float max_ticks)
			{
				float[] scale = {50f, 20f, 10f, 5f, 4f, 2f, 1f, 0.5f, 0.25f, 0.2f, 0.1f, 0.05f};
				float range = Span > 0f ? Span : 1f;
				Step = (float)Math.Pow(10, (int)Math.Log10(range));
				foreach (float s in scale)
				{
					if (range * s / Step > max_ticks) continue;
					Step /= s;
					return;
				}
			}
		}

		/// <summary>Rendering options</summary>
		public class RdrOptions
		{
			// Colours for graph elements
			public Color m_bg_colour      = Color.WhiteSmoke;               // The fill colour of the background of the graph
			public Color m_title_colour   = Color.Black;                    // The colour of the title text
			public Color m_grid_colour    = Color.FromArgb(230, 230, 230);  // The colour of the grid lines
		
			// Graph margins and constants
			public float m_left_margin    = 0.01f; // Fractional distance from the left edge to the graph region
			public float m_right_margin   = 0.01f;
			public float m_top_margin     = 0.01f;
			public float m_bottom_margin  = 0.01f;
			public float m_title_top      = 0.01f;          // Fractional distance  down from the top of the client area to the top of the title text
			public Font  m_title_font     = new Font("tahoma", 12, FontStyle.Bold);    // Font to use for the title text
			public Font  m_note_font      = new Font("tahoma",  8, FontStyle.Regular); // Font to use for graph notes
			public RdrOptions Clone() { return (RdrOptions)MemberwiseClone(); }
		}
		
		/// <summary>A snapshot of the current graph, used during dragging/zooming</summary>
		private class Snapshot
		{
			public Bitmap     m_bm  = null;
			public Axis.Range m_xrange;
			public Axis.Range m_yrange;
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

		// Members
		private Bitmap                    m_bm;             // The bitmap containing the graph image. This should only be modified in the main UI thread
		private string                    m_title;          // The graph title
		private readonly Axis.Range       m_base_xrange;    // Initial values for the x axis 
		private readonly Axis.Range       m_base_yrange;    // Initial values for the y axis 
		private readonly RdrOptions       m_rdr_options;    // Rendering options
		private readonly List<Series>     m_data;           // The source data for the graph
		private readonly Axis             m_xaxis;          // Details of the x axis 
		private readonly Axis             m_yaxis;          // Details of the y axis
		private readonly Axis.Range       m_zoom;           // The amount of zoom (plus limits)
		private readonly ToolTip          m_tooltip;        // A tool tip to display the mouse location value
		private readonly ManualResetEvent m_rdr_idle;       // An event that is set when rendering is available
		private readonly Snapshot         m_snap;           // A copy of the current graph used to preview the graph position while dragging/zooming
		private readonly List<Note>       m_notes;          // A collection of notes to add to the graph
		private bool                      m_dirty;          // Indicates that the bitmap needs to be regenerated
		private volatile int              m_rdr_id;         // Render issue number, renders are cancelled if this id changes
		private PointF                    m_grab_location;  // The location in graph space of where the graph was "grabbed"
		private Rectangle                 m_selection;      // Area selection, has width, height of zero when the user isn't selecting

		// Clients should draw in graph space using the scale values
		// e.g. gfx.DrawLine(Pens.Black, sender.XAxis.Min*scale_x, sender.YAxis.Min*scale_y, sender.XAxis.Max*scale_x, sender.YAxis.Max*scale_y);
		public delegate void AddOverlaysEventHandler(GraphControl sender, Graphics gfx, float scale_x, float scale_y);

		/// <summary>Called during rendering of the graph to allow clients to add graphics to the cached bitmap.</summary>
		public event AddOverlaysEventHandler AddOverlaysOnRender;
		
		/// <summary>Called each time the bitmap is drawn to the control to allow clients to add graphics. The overlays can change without affecting cached graph bitmap</summary>
		public event AddOverlaysEventHandler AddOverlaysOnPaint;

		/// <summary>Event allowing callers to add options to the context menu</summary>
		public event AddUserMenuOptionsHandler AddUserMenuOptions;
		public delegate void AddUserMenuOptionsHandler(GraphControl sender, ContextMenuStrip context_menu);

		// Constructors
		public GraphControl()
		:this("", new Axis.Range(), new Axis.Range(), new Axis(), new Axis(), new RdrOptions(), new List<Series>())
		{}
		public GraphControl(GraphControl src)
		:this(src.Title, src.m_base_xrange, src.m_base_yrange, new Axis(src.m_xaxis), new Axis(src.m_yaxis), src.m_rdr_options.Clone(), new List<Series>(src.m_data))
		{
			AddOverlaysOnRender = src.AddOverlaysOnRender;
			AddOverlaysOnPaint = src.AddOverlaysOnPaint;
		}
		private GraphControl(string title, Axis.Range base_xrange, Axis.Range base_yrange, Axis xaxis, Axis yaxis, RdrOptions rdr_options, List<Series> data)
		{
			InitializeComponent();

			DoubleBuffered = true;

			m_bm = null;
			m_title = title;
			m_base_xrange = base_xrange;
			m_base_yrange = base_yrange;
			m_xaxis = xaxis;
			m_yaxis = yaxis;
			m_rdr_options = rdr_options;
			m_data = data;
			m_zoom = new Axis.Range(float.Epsilon, float.MaxValue, 1f);
			m_dirty = true;
			m_rdr_id = 0;

			m_tooltip = new ToolTip();
			m_tooltip.ShowAlways = false;
			m_tooltip.UseAnimation = false;
			m_tooltip.UseFading = false;
			m_tooltip.Tag = false;

			m_rdr_idle = new ManualResetEvent(true);
			m_snap     = new Snapshot();
			m_notes    = new List<Note>();

			MouseNavigation = true;
		}

		// Public graph data
		public string       Title          { get { return m_title; } set { m_title = value; m_dirty = true; } }
		public List<Series> Data           { get { while (IsBusy) {} return m_data; } }
		public Axis         XAxis          { get { return m_xaxis; } }
		public Axis         YAxis          { get { return m_yaxis; } }
		public RdrOptions   RenderOptions  { get { return m_rdr_options; } }
		public List<Note>   Notes          { get { return m_notes; } }

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

		// Mouse navigation - public to allow users to forward mouse calls to us.
		public void OnMouseDown(object sender, MouseEventArgs e)
		{
			m_selection.Location = e.Location;
			m_selection.Width = 0;
			m_selection.Height = 0;
			switch( e.Button )
			{
			case MouseButtons.Left:
				m_grab_location = PointToGraph(e.Location);
				Cursor = Cursors.SizeAll;
				MouseMove -= OnMouseGraphDrag;
				MouseMove += OnMouseGraphDrag;
				break;

			case MouseButtons.Right:
				MouseMove -= OnMouseAreaSelect;
				MouseMove += OnMouseAreaSelect;
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
				break;

			case MouseButtons.Right:
				MouseMove -= OnMouseAreaSelect;
			
				// If the selection has area, rescale the graph
				if (Math.Abs(m_selection.Width) > 0 && Math.Abs(m_selection.Height) > 0)
				{
					// Normalise the rectangle
					if (m_selection.Width  < 0) { m_selection.X += m_selection.Width;  m_selection.Width  = -m_selection.Width;  }
					if (m_selection.Height < 0) { m_selection.Y += m_selection.Height; m_selection.Height = -m_selection.Height; }
				
					// Rescale the graph
					PointF lower = PointToGraph(m_selection.Location + new Size(0, m_selection.Height));
					PointF upper = PointToGraph(m_selection.Location + new Size(m_selection.Width, 0));
					m_xaxis.Min = lower.X;
					m_xaxis.Max = upper.X;
					m_yaxis.Min = lower.Y;
					m_yaxis.Max = upper.Y;
					SetSuitableStepSizes(ClientSize);
					m_dirty = true;
				
					// Clear the selection
					m_selection.Width  = 0;
					m_selection.Height = 0;
				}
				else
				{
					ShowContextMenu(e.Location);
				}
				break;
			}
			Refresh();
		}
		public void OnMouseWheel(object sender, MouseEventArgs e)
		{
			if (!GraphRegion(ClientSize).Contains(e.Location)) return;
			PointF point = PointToGraph(e.Location);
			int delta = e.Delta < -999 ? -999 : e.Delta > 999 ? 999 : e.Delta;
			Zoom *= (1.0f + delta * 0.001f);
			PositionGraph(e.Location, point);
			Refresh();
		}

		/// <summary>Get/Set the centre of the graph. Remember to call refresh</summary>
		public PointF Centre
		{
			get { return new PointF(m_xaxis.Min + m_xaxis.Span/2f, m_yaxis.Min + m_yaxis.Span/2f); }
			set
			{
				TakeSnapshot();
				m_xaxis.Min = value.X - m_xaxis.Span/2f;
				m_yaxis.Min = value.Y - m_yaxis.Span/2f;
				m_dirty = true;
			}
		}

		/// <summary>Zoom in/out on the graph. Remember to call refresh.
		/// Zoom is a floating point value where 1f = no zoom, 2f = 2x magnification</summary>
		public float Zoom
		{
			get
			{
				return 
					m_xaxis.AllowZoom ? m_xaxis.Span / m_base_xrange.m_span :
					m_yaxis.AllowZoom ? m_yaxis.Span / m_base_yrange.m_span : 1f;
			}
			set
			{
				TakeSnapshot();
				value = Math.Min(m_zoom.m_max, Math.Max(m_zoom.m_min, value));
				float aspect = (m_yaxis.Span * m_base_xrange.m_span) / (m_base_yrange.m_span * m_xaxis.Span);
				aspect = Math.Min(1000f, Math.Max(0.001f, float.IsNaN(aspect) ? 1f : aspect ));
				if (m_xaxis.AllowZoom) m_xaxis.Span = m_base_xrange.m_span * value         ;
				if (m_yaxis.AllowZoom) m_yaxis.Span = m_base_yrange.m_span * value * aspect;
				SetSuitableStepSizes(ClientSize);
				m_dirty = true;
			}
		}

		/// <summary>Zoom limits</summary>
		public float ZoomMin { get {return m_zoom.m_min;} set {Debug.Assert(value > 0f); m_zoom.m_min = value;} }
		public float ZoomMax { get {return m_zoom.m_max;} set {Debug.Assert(value > 0f); m_zoom.m_max = value;} }

		/// <summary>Get the rectangular area of the graph for a given client area size.</summary>
		public Rectangle GraphRegion(Size size)
		{
			int marginL = (int)(size.Width  * RenderOptions.m_left_margin  );
			int marginT = (int)(size.Height * RenderOptions.m_top_margin   );
			int marginR = (int)(size.Width  * RenderOptions.m_right_margin );
			int marginB = (int)(size.Height * RenderOptions.m_bottom_margin);

			if (m_title.Length != 0)       { marginT += TextRenderer.MeasureText(m_title      ,       RenderOptions.m_title_font).Height; }
			if (m_yaxis.Label.Length != 0) { marginL += TextRenderer.MeasureText(m_yaxis.Label, YAxis.RenderOptions.m_label_font).Height; }
			if (m_xaxis.Label.Length != 0) { marginB += TextRenderer.MeasureText(m_xaxis.Label, XAxis.RenderOptions.m_label_font).Height; }
			
			Size sx = TextRenderer.MeasureText(YAxis.TickText(AestheticTickValue(9999999.9)), YAxis.RenderOptions.m_tick_font);
			Size sy = TextRenderer.MeasureText(XAxis.TickText(AestheticTickValue(9999999.9)), XAxis.RenderOptions.m_tick_font);
			marginL += YAxis.RenderOptions.m_tick_length + sx.Width;
			marginB += XAxis.RenderOptions.m_tick_length + sy.Height;

			int x      = Math.Max(0, marginL);
			int y      = Math.Max(0, marginT);
			int width  = Math.Max(0, size.Width  - (marginL + marginR));
			int height = Math.Max(0, size.Height - (marginT + marginB));
			return new Rectangle(x, y, width, height);
		}

		/// <summary>Return true if the graph is currently being rendered</summary>
		public bool IsBusy
		{
			get { return !m_rdr_idle.WaitOne(0); }
		}

		/// <summary>Cancel a background render that is in progress</summary>
		public void CancelRender()
		{
			++m_rdr_id;
		}

		/// <summary>Force the graph to redraw</summary>
		public void Render()
		{
			m_dirty = true;
			Refresh();
		}

		/// <summary>Returns a point in graph space from a point in client space
		/// Use to convert mouse (client-space) locations to graph coordinates</summary>
		public PointF PointToGraph(Point point)
		{
			Rectangle rect = GraphRegion(ClientSize);
			return new PointF(
				m_xaxis.Min + (point.X - rect.Left  ) * m_xaxis.Span / rect.Width,
				m_yaxis.Min - (point.Y - rect.Bottom) * m_yaxis.Span / rect.Height);
		}

		/// <summary>Returns a point in client space from a point in graph space. Inverse of PointToGraph</summary>
		public Point GraphToPoint(PointF gs_point)
		{
			Rectangle rect = GraphRegion(ClientSize);
			return new Point(
				(int)(rect.Left   + (gs_point.X - m_xaxis.Min) * rect.Width  / m_xaxis.Span),
				(int)(rect.Bottom - (gs_point.Y - m_yaxis.Min) * rect.Height / m_yaxis.Span));
		}

		/// <summary>Shifts the X and Y range of the graph so that graph space position
		/// 'gs_point' is at client space position 'cs_point'</summary>
		public void PositionGraph(Point cs_point, PointF gs_point)
		{
			TakeSnapshot();
			PointF dst = PointToGraph(cs_point);
			m_xaxis.Shift(gs_point.X - dst.X);
			m_yaxis.Shift(gs_point.Y - dst.Y);
			m_dirty = true;
		}

		/// <summary>Set the title and axis labels of the graph</summary>
		public void SetLabels(string title, string xaxis_text, string yaxis_text)
		{
			m_title = title;
			m_xaxis.Label = xaxis_text;
			m_yaxis.Label = yaxis_text;
		}

		/// <summary>Find the appropriate range for all data in the graph
		/// Call ResetToDefaultRange() to zoom the graph to this range</summary>
		public void FindDefaultRange()
		{
			m_base_xrange.m_min =  float.MaxValue;
			m_base_xrange.m_max = -float.MaxValue;
			m_base_yrange.m_min =  float.MaxValue;
			m_base_yrange.m_max = -float.MaxValue;
			foreach (Series series in m_data)
			{
				if (!series.RenderOptions.m_visible) continue;
				foreach (GraphValue gv in series.Values)
				{
					m_base_xrange.m_min = Math.Min(m_base_xrange.m_min, gv.m_valueX);
					m_base_xrange.m_max = Math.Max(m_base_xrange.m_max, gv.m_valueX);
					m_base_yrange.m_min = Math.Min(m_base_yrange.m_min, gv.m_valueY);
					m_base_yrange.m_max = Math.Max(m_base_yrange.m_max, gv.m_valueY);
				}
			}
			if (m_base_xrange.m_span <= 0.0f) { m_base_xrange.m_span = 1f; }
			if (m_base_yrange.m_span <= 0.0f) { m_base_yrange.m_span = 1f; }
			{ float extra = m_base_xrange.m_span * 0.05f; m_base_xrange.m_min -= extra; m_base_xrange.m_max += extra; }
			{ float extra = m_base_yrange.m_span * 0.05f; m_base_yrange.m_min -= extra; m_base_yrange.m_max += extra; }
			SetSuitableStepSizes(ClientSize);
		}

		/// <summary>Reset the axis ranges to the default
		/// Call FindDefaultRange() to set the default range</summary>
		public void ResetToDefaultRange()
		{
			if (!m_xaxis.LockRange) { m_xaxis.Rng = m_base_xrange; }
			if (!m_yaxis.LockRange) { m_yaxis.Rng = m_base_yrange; }
			m_dirty = true;
		}

		/// <summary>Choose the number of ticks and their size based on the area of the graph</summary>
		private void SetSuitableStepSizes(Size size)
		{
			const float pixels_per_tick_x = 30f;
			const float pixels_per_tick_y = 24f;

			Rectangle region = GraphRegion(size);
			m_xaxis.SetSuitableStepSize(region.Width  / pixels_per_tick_x);
			m_yaxis.SetSuitableStepSize(region.Height / pixels_per_tick_y);
		}

		/// <summary>Returns the 'Y' value for a given 'X' value in a series in the graph</summary>
		public float GetValueAt(int series_index, float x)
		{
			// Binary search on X for the nearest data point
			Debug.Assert(series_index < m_data.Count, "Series index out of range");
			List<GraphValue> values = m_data[series_index].Values;
			if (values.Count == 0) return 0f;
			
			// Find the nearest element to 'x'
			int i0 = values.BinarySearch(new GraphValue(x, 0, null), new SortX());
			if (i0 < 0) i0 = ~i0;

			// find the next element with an x value greater than values[i0].x
			int i1; for (i1 = i0; i1 != values.Count && values[i1].m_valueX <= values[i0].m_valueX; ++i1) {}
			if (i1 == values.Count) return values[i0].m_valueY;
			
			// linearly interpolate between 'i0' and 'i1'
			float t = (x - values[i0].m_valueX) / (values[i1].m_valueX - values[i0].m_valueX);
			return (1f - t) * values[i0].m_valueY + (t) * values[i1].m_valueY;
		}

		/// <summary>Returns the nearest graph data point to 'location' given
		/// a selection tolerance. 'location' should be in graph space.
		/// Returns null if no point is within the selection tolerance</summary>
		public GraphValue GetValueAt(int series_index, PointF location)
		{
			Debug.Assert(series_index < m_data.Count, "Series index out of range");

			// Convert 'location' into graph space
			Rectangle graph_region = GraphRegion(ClientSize);
			GraphValue item = new GraphValue(location.X, location.Y, null);
		
			// Selection tolerance is a distance in pixels
			const double selection_tolerance = 5;

			// Binary search on X for the nearest data point
			List<GraphValue> values = m_data[series_index].Values;
			int nearest = values.BinarySearch(item, new SortX());
			if (nearest < 0) nearest = ~nearest;
			int max_i = Math.Max(nearest, values.Count - nearest);

			// Search outwards from the nearest on x for a point within the selection tolerance
			for (int i = 0; i != max_i; ++i)
			{
				// Left side, then right side
				for (int j = -1; j <= 1; j += 2)
				{
					GraphValue gv = values[nearest + j*i];

					// Convert the data point to bitmap space
					double x = graph_region.Left   + ((gv.m_valueX - m_xaxis.Min) / m_xaxis.Span) * graph_region.Width	 - location.X;
					double y = graph_region.Bottom - ((gv.m_valueY - m_yaxis.Min) / m_yaxis.Span) * graph_region.Height - location.Y;
					double dist = Math.Sqrt(x*x + y*y);

					// If within the selection tolerance then found one
					if (dist < selection_tolerance)
						return gv;
				
					// If the distance along x is greater than the selection tolerance, then give up.
					if (Math.Abs(x) > selection_tolerance)
						return null;
				}
			}
			return null;
		}

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
				Graphics gfx = Graphics.FromImage(bm);
				Rectangle region = GraphRegion(size);
				RenderGraph(gfx, size, region);
				
				// Add a note to indicate the graph is still rendering
				const string rdring_msg = "rendering...";
				SizeF text_size = gfx.MeasureString(rdring_msg, m_rdr_options.m_title_font);
				Brush brush = new SolidBrush(Color.FromArgb(0x80, Color.Black));
				PointF pt = new PointF(region.X + (region.Width - text_size.Width)*0.5f, region.Y + (region.Height - text_size.Height)*0.5f);
				gfx.DrawString(rdring_msg, m_rdr_options.m_title_font, brush, pt);
				
				// Spawn a thread to render the graph into 'm_tmp_bm'
				ThreadPool.QueueUserWorkItem((rdr_id)=>
				{
					Bitmap tmp_bm = new Bitmap(size.Width, size.Height);
					bool cancelled = GenerateGraphBitmap(tmp_bm, ()=>{return m_rdr_id != (int)rdr_id;});
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
			Graphics gfx = Graphics.FromImage(bm);
			Rectangle region = GraphRegion(bm.Size);
			
			// Get transforms that we can use to draw in unscaled graph space. Can't use a scale transform here
			// though because the lines and points will also be scaled, into elipses or caligraphy etc
			float scale_x = region.Width  / m_xaxis.Span;
			float scale_y = region.Height / m_yaxis.Span;
			if (float.IsInfinity(scale_x)) {scale_x = scale_x >= 0 ? float.MaxValue : float.MinValue;}
			if (float.IsInfinity(scale_y)) {scale_y = scale_y >= 0 ? float.MaxValue : float.MinValue;}
			Matrix data_xfrm = new Matrix(1f, 0f, 0f, -1f, region.Left - m_xaxis.Min * scale_x, region.Bottom + m_yaxis.Min * scale_y);
			Matrix text_xfrm = new Matrix(1f, 0f, 0f,  1f, region.Left - m_xaxis.Min * scale_x, region.Bottom + m_yaxis.Min * scale_y);

			try
			{
				// Render the basic graph
				RenderGraph(gfx, bm.Size, region);
				gfx.SetClip(region);

				// Render the data onto the graph
				gfx.Transform = data_xfrm;
				foreach (Series s in m_data)
				{
					if (s.RenderOptions.m_visible)
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
				foreach (Note note in m_notes)
				{
					gfx.DrawString(note.m_msg, m_rdr_options.m_note_font, new SolidBrush(note.m_colour), new PointF(note.m_loc.X*scale_x, note.m_loc.Y*scale_y));
					if (cancel_pending()) break;
				}

				gfx.ResetTransform();
				gfx.ResetClip();
			}
			catch (Exception ex)
			{
				// There is a problem in the .NET graphics object that can cause these exceptions if the range is extreme
				gfx.Transform = text_xfrm;
				gfx.DrawString("Rendering error occured: "+ex.Message, m_rdr_options.m_title_font, new SolidBrush(Color.FromArgb(0x80, Color.Black)), new PointF());
			}
			return cancel_pending();
		}

		/// <summary>Render the basic graph without any data</summary>
		private void RenderGraph(Graphics gfx, Size size, Rectangle region)
		{
			// This is not enforced in the axis.Min/Max accessors because it's useful
			// to be able to change the min/max independently of each other, set them
			// to float max etc. It's only invalid to render a graph with a negative range
			Debug.Assert(m_xaxis.Span > 0, "Negative x range");
			Debug.Assert(m_yaxis.Span > 0, "Negative y range");
			
			// Clear the bitmap to the background colour
			gfx.Clear(Color.FromArgb(0xff, BackColor));

			// Draw the graph background
			gfx.FillRectangle(new SolidBrush(m_rdr_options.m_bg_colour), region);

			// Draw the graph title and labels
			if( m_title.Length != 0 )
			{
				SizeF text_size = gfx.MeasureString(m_title, m_rdr_options.m_title_font);
				float textX = (size.Width - text_size.Width) * 0.5f;
				gfx.DrawString(m_title, m_rdr_options.m_title_font, new SolidBrush(m_rdr_options.m_title_colour), textX, m_rdr_options.m_title_top);
			}
			if( m_xaxis.Label.Length != 0 )
			{
				SizeF text_size = gfx.MeasureString(m_xaxis.Label, m_xaxis.RenderOptions.m_label_font);
				float textX = (size.Width - text_size.Width) * 0.5f;
				gfx.DrawString(m_xaxis.Label, m_xaxis.RenderOptions.m_label_font, new SolidBrush(m_xaxis.RenderOptions.m_label_colour), textX, size.Height - text_size.Height);
			}
			if( m_yaxis.Label.Length != 0 )
			{
				gfx.RotateTransform(-90);
				SizeF text_size = gfx.MeasureString(m_yaxis.Label, m_yaxis.RenderOptions.m_label_font);
				float textX = (size.Height + text_size.Width) * 0.5f;
				gfx.DrawString(m_yaxis.Label, m_yaxis.RenderOptions.m_label_font, new SolidBrush(m_yaxis.RenderOptions.m_label_colour), -textX, 0);
				gfx.RotateTransform(90);
			}

			Pen grid_line_pen = new Pen(m_rdr_options.m_grid_colour);
			
			// Draw the x axis
			{
				Axis  axis       = m_xaxis;
				Pen	  axis_pen   = new Pen(axis.RenderOptions.m_axis_colour);
				Brush text_brush = new SolidBrush(axis.RenderOptions.m_tick_colour);
				double x         = (axis.Min - Math.IEEERemainder(axis.Min, axis.Step)) - axis.Min;  if (x < 0.0) {x += axis.Step;}
				double xmax      = axis.Span * 1.0001f;
				double xstep     = axis.Step;
	
				if (x + xstep == x)          xstep = (xmax - x) / 100;
				if (xmax - x > xstep * 1000) xstep = (xmax - x) / 100;
				Debug.Assert(x + axis.Step != x);
				Debug.Assert((xmax - x) / axis.Step < 1000);

				for (; x < xmax; x += xstep)
				{
					// Find the pixel position of 'x'
					int X = (int)(region.Left + x * region.Width / axis.Span);
					
					// Draw the grid line
					gfx.DrawLine(grid_line_pen, X, region.Top, X, region.Bottom);
					
					// Draw the tick
					gfx.DrawLine(axis_pen, X, region.Bottom, X, region.Bottom + axis.RenderOptions.m_tick_length);

					// Draw the text
					string text = axis.TickText(AestheticTickValue(x + axis.Min));
					SizeF text_size = gfx.MeasureString(text, axis.RenderOptions.m_tick_font);
					float textY	= region.Bottom + axis.RenderOptions.m_tick_length;
					gfx.DrawString(text, axis.RenderOptions.m_tick_font, text_brush, X - text_size.Width*0.5f, textY);
				}
				gfx.DrawLine(axis_pen, region.Left , region.Top, region.Left , region.Bottom);
				gfx.DrawLine(axis_pen, region.Right, region.Top, region.Right, region.Bottom);
			}

			// Draw the y axis
			{
				Axis axis        = m_yaxis;
				Pen axis_pen     = new Pen(axis.RenderOptions.m_axis_colour);
				Brush text_brush = new SolidBrush(axis.RenderOptions.m_tick_colour);
				double y         = (axis.Min - Math.IEEERemainder(axis.Min, axis.Step)) - axis.Min;  if (y < 0.0) {y += axis.Step;}
				double ymax      = axis.Span * 1.0001f;
				double ystep     = axis.Step;
				
				if (y + ystep == y)          ystep = (ymax - y) / 100;
				if (ymax - y > ystep * 1000) ystep = (ymax - y) / 100;
				Debug.Assert(y + ystep != y);
				Debug.Assert((ymax - y) / ystep < 1000);
				
				for (; y < ymax; y += ystep)
				{
					// Find the pixel position of Y
					int Y = (int)(region.Bottom - y * region.Height / axis.Span);
					
					// Draw the grid line
					gfx.DrawLine(grid_line_pen, region.Left, Y, region.Right, Y);

					// Draw the tick
					gfx.DrawLine(axis_pen, region.Left, Y, region.Left - axis.RenderOptions.m_tick_length, Y);

					// Draw the text
					string text = axis.TickText(AestheticTickValue(y + axis.Min));
					SizeF text_size = gfx.MeasureString(text, axis.RenderOptions.m_tick_font);
					float textX = region.Left - axis.RenderOptions.m_tick_length - text_size.Width;
					gfx.DrawString(text, axis.RenderOptions.m_tick_font, text_brush, textX, Y - text_size.Height * 0.5f);
				}
				gfx.DrawLine(axis_pen, region.Left, region.Top   , region.Right, region.Top   );
				gfx.DrawLine(axis_pen, region.Left, region.Bottom, region.Right, region.Bottom);
			}
		}

		/// <summary>Renders the data set onto the graph</summary>
		private void RenderData(Series series, Graphics gfx, float scale_x, float scale_y, Func<bool> cancel_pending)
		{
			Brush pt_brush  = new SolidBrush(series.RenderOptions.m_point_colour);
			Brush bar_brush = new SolidBrush(series.RenderOptions.m_bar_colour);
			Brush err_brush = new SolidBrush(series.RenderOptions.m_error_bar_colour);
			Pen   ln_pen    = new Pen(series.RenderOptions.m_line_colour, series.RenderOptions.m_line_width);
			Pen   ma_pen    = new Pen(series.RenderOptions.m_ma_line_colour, series.RenderOptions.m_ma_line_width);

			float pt_size = series.RenderOptions.m_point_size;
			float pt_radius = 0.5f * pt_size;
			
			bool first = true;      // True for the first point only
			bool first_zero = true; // True until we find a non-zero point
			GraphValue prev_gv = new GraphValue(m_xaxis.Min, m_yaxis.Min);
			GraphValue prev_gv_nonzero = prev_gv;
			float prev_ma_y = 0;

			// Find the first point within the x range of the graph
			// Note: if the x values of a series aren't sorted then you'll see clipping problems
			// Set the 'unsorted' property for any unsorted series
			GraphValue left_bound = new GraphValue(m_xaxis.Min, m_yaxis.Min, null);
			int i = 0;
			if (series.Sorted)
			{
				int ii = series.Values.BinarySearch(left_bound, new SortX());
				i = Math.Max(0, (ii < 0 ? ~ii : ii) - 1);
			}
			
			// Plot the data.
			for (; i != series.Values.Count; ++i)
			{
				if ((i % 50) == 0 && cancel_pending()) break;
				GraphValue gv = series.Values[i];
				Series.RdrOpts rdr = series.RenderOptions;
				
				// If the range is sorted we can stop drawing once we've gone passed the Max X graph value
				if (series.Sorted)
				{
					float last_drawn_x_value = (series.RenderOptions.m_draw_zeros == Series.RdrOpts.PlotZeros.Skip) ? prev_gv_nonzero.m_valueX : prev_gv.m_valueX;
					if (last_drawn_x_value > m_xaxis.Max) break;
				}
				
				// Get the data point in screen space
				float x = gv.m_valueX * scale_x;
				float y = gv.m_valueY * scale_y;

				// Render the data point
				switch (series.RenderOptions.m_plot_type)
				{
				// Draw the data point
				case Series.RdrOpts.PlotType.Point:
					{
						// If the point is a zero and we're not drawing zeros, skip
						if (gv.m_valueY == 0f && rdr.m_draw_zeros != Series.RdrOpts.PlotZeros.Draw)
							break;

						// Draw error bars is on
						if (rdr.m_draw_error_bars && (gv.m_upper_err - gv.m_lower_err) > 0)
							gfx.FillRectangle(err_brush, x - pt_radius, gv.m_lower_err * scale_y, pt_size, (gv.m_upper_err - gv.m_lower_err) * scale_y);
							
						// Plot the data point
						if (rdr.m_draw_data)
						{
							// If the point lies on the previous one, then don't bother drawing it
							float prev_x = prev_gv.m_valueX * scale_x;
							float prev_y = prev_gv.m_valueY * scale_y;
							if (x != prev_x || y != prev_y)
								gfx.FillEllipse(pt_brush, x - pt_radius, y - pt_radius, pt_size, pt_size);
						}
					}break;
				
				// Draw the data point and connect with a line
				case Series.RdrOpts.PlotType.Line:
					{
						// If the point is a zero and we're not drawing zeros, skip
						if (gv.m_valueY == 0f && rdr.m_draw_zeros != Series.RdrOpts.PlotZeros.Draw)
							break;

						// Draw error bars is on
						if (rdr.m_draw_error_bars && (gv.m_upper_err - gv.m_lower_err) > 0)
							gfx.FillRectangle(err_brush, x - pt_radius, gv.m_lower_err * scale_y, pt_size, (gv.m_upper_err - gv.m_lower_err) * scale_y);
		
						// Plot the point and line
						if (rdr.m_draw_data)
						{
							// Plot the point - only draw it if the size is greater than zero
							// otherwise we'll be covering it with the line anyway
							if (pt_size > 0)
								gfx.FillEllipse(pt_brush, x - pt_radius, y - pt_radius, pt_size, pt_size);
							
							// Draw the line
							if (rdr.m_draw_zeros == Series.RdrOpts.PlotZeros.Draw && !first)
							{
								float prev_x = prev_gv.m_valueX * scale_x;
								float prev_y = prev_gv.m_valueY * scale_y;
							
								// If the line starts and stops on the same pixel, then don't bother drawing it
								if (x != prev_x || y != prev_y)
									gfx.DrawLine(ln_pen, prev_x, prev_y, x, y);
							}
							else if (rdr.m_draw_zeros == Series.RdrOpts.PlotZeros.Skip && !first_zero)
							{
								float prev_x = prev_gv_nonzero.m_valueX * scale_x;
								float prev_y = prev_gv_nonzero.m_valueY * scale_y;
								
								// If the line starts and stops on the same pixel, then don't bother drawing it
								if (x != prev_x || y != prev_y)
									gfx.DrawLine(ln_pen, prev_x, prev_y, x, y);
							}
						}
					}break;
				case Series.RdrOpts.PlotType.Bar:
					{
						// If the point is a zero and we're not drawing zeros, skip
						if (gv.m_valueY == 0f && rdr.m_draw_zeros != Series.RdrOpts.PlotZeros.Draw)
							break;

						// Calc the left and right side of the bar
						float width_scale = series.RenderOptions.m_bar_width;
						float lhs = 0f, rhs = 0f;
						if (i   != 0)                   { lhs = scale_x * width_scale * (gv.m_valueX                 - prev_gv.m_valueX) / 2f; }
						if (i+1 != series.Values.Count) { rhs = scale_x * width_scale * (series.Values[i+1].m_valueX - gv.m_valueX     ) / 2f; }
						if (lhs == 0f) lhs = rhs;
						if (rhs == 0f) rhs = lhs;

						// Draw error bars is on
						if (rdr.m_draw_error_bars && (gv.m_upper_err - gv.m_lower_err) > 0)
							gfx.FillRectangle(err_brush, x - lhs, gv.m_lower_err * scale_y, rhs + lhs, (gv.m_upper_err - gv.m_lower_err) * scale_y);
				
						// Plot the bar
						if (rdr.m_draw_data)
						{
							if      (y < 0) gfx.FillRectangle(bar_brush, x - lhs, y, rhs + lhs, Math.Abs(y));
							else if (y > 0) gfx.FillRectangle(bar_brush, x - lhs, 0, rhs + lhs, y);
							else            gfx.DrawLine(new Pen(bar_brush, 0),  x - lhs, 0, x + rhs, 0);
						}
					}break;
				}

				// Add a moving average line
				if (series.RenderOptions.m_draw_moving_avr)
				{
					// Find the sum of the values around 'i' for the moving average
					float sum = 0;
					int count = 0;
					int avr_i0 = Math.Max(0                   ,i - series.RenderOptions.m_ma_window_size / 2);
					int avr_i1 = Math.Min(series.Values.Count ,i + series.RenderOptions.m_ma_window_size / 2);
					for (int j = avr_i0; j != avr_i1; ++j)
					{
						// Don't include zeros if they're not being drawn
						if (series.Values[j].m_valueY == 0 && series.RenderOptions.m_draw_zeros != Series.RdrOpts.PlotZeros.Draw) continue;
						sum += series.Values[j].m_valueY;
						++count;
					}
					if (count != 0)
					{
						// Get the data point in screen space
						float curr_x = gv     .m_valueX * scale_x;
						float prev_x = prev_gv.m_valueX * scale_x;

						// Draw a segment of the moving average line
						float ma_y = (sum / count) * scale_y;
						if (!first && (prev_ma_y != 0 || series.RenderOptions.m_draw_zeros == Series.RdrOpts.PlotZeros.Draw))
							gfx.DrawLine(ma_pen, prev_x, prev_ma_y, curr_x, ma_y);
						
						prev_ma_y = ma_y;
					}
				}

				first = false;
				prev_gv = gv;
				if (gv.m_valueY != 0f)
				{
					first_zero = false;
					prev_gv_nonzero = gv;
				}
			}
		}

		/// <summary>Turn a tick value into an aesthetic string</summary>
		private static double AestheticTickValue(double value)
		{
			return Math.Round(value,3);
		}

		/// <summary>Create and display a context menu</summary>
		public void ShowContextMenu(Point location)
		{
			ContextMenuStrip context_menu = new ContextMenuStrip();
			context_menu.Closed += delegate { Refresh(); };
		
			// Allow users to add menu options
			if (AddUserMenuOptions != null)
				AddUserMenuOptions(this, context_menu);
			
			{// Add floating tooltip options
				ToolStripMenuItem show_value = new ToolStripMenuItem("Show Value");
				context_menu.Items.Add(show_value);
				show_value.Checked = (bool)m_tooltip.Tag;
				show_value.Click += delegate
				{
					m_tooltip.Hide(this);
					if( (bool)m_tooltip.Tag ) MouseMove -= OnMouseMoveTooltip;
					m_tooltip.Tag = !(bool)m_tooltip.Tag;
					if( (bool)m_tooltip.Tag ) MouseMove += OnMouseMoveTooltip;
				};
			}

			{// Add zoom options
				ToolStripMenuItem zoom_menu = new ToolStripMenuItem("Zoom");
				context_menu.Items.Add(zoom_menu);
				{
					ToolStripMenuItem default_zoom = new ToolStripMenuItem("Default");
					zoom_menu.DropDownItems.Add(default_zoom);
					default_zoom.Click += delegate
					{
						FindDefaultRange();
						ResetToDefaultRange();
						Refresh();
					};

					ToolStripMenuItem zoom_in = new ToolStripMenuItem("In");
					zoom_menu.DropDownItems.Add(zoom_in);
					zoom_in.Click += delegate
					{
						PointF point = PointToGraph(m_selection.Location);
						Zoom *= 2.0f;
						PositionGraph(m_selection.Location, point);
						Refresh();
					};

					ToolStripMenuItem zoom_out = new ToolStripMenuItem("Out");
					zoom_menu.DropDownItems.Add(zoom_out);
					zoom_out.Click += delegate
					{
						PointF point = PointToGraph(m_selection.Location);
						Zoom *= 0.5f;
						PositionGraph(m_selection.Location, point);
						Refresh();
					};
				}
			}

			// Add options for each series
			if (m_data.Count != 0)
			{
				{// All series
					ToolStripMenuItem series_menu = new ToolStripMenuItem("Series: All");
					context_menu.Items.Add(series_menu);
					{// Draw
						ToolStripMenuItem option = new ToolStripMenuItem("Visible");
						series_menu.DropDownItems.Add(option);
						int state = 0;
						foreach (Series s in m_data) state |= s.RenderOptions.m_visible ? 2 : 1;
						option.CheckState = state == 2 ? CheckState.Checked : state == 1 ? CheckState.Unchecked : CheckState.Indeterminate;
						option.Click += delegate
						{
							option.CheckState = option.CheckState == CheckState.Checked ? CheckState.Unchecked : CheckState.Checked;
							foreach (Series s in m_data) { s.RenderOptions.m_visible = option.CheckState == CheckState.Checked; }
							m_dirty = true; Refresh();
						};
					}
				}
				for (int index = 0; index != m_data.Count; ++index)
				{
					Series series = m_data[index];
					ToolStripMenuItem series_menu = new ToolStripMenuItem("Series: " + series.Name);
					context_menu.Items.Add(series_menu);
					if		(series.RenderOptions.m_plot_type == Series.RdrOpts.PlotType.Point) series_menu.ForeColor = series.RenderOptions.m_point_colour;
					else if (series.RenderOptions.m_plot_type == Series.RdrOpts.PlotType.Line) series_menu.ForeColor = series.RenderOptions.m_line_colour;
					else if (series.RenderOptions.m_plot_type == Series.RdrOpts.PlotType.Bar) series_menu.ForeColor = series.RenderOptions.m_bar_colour;
					series_menu.Checked = series.RenderOptions.m_visible;
					series_menu.Tag = index;
					series_menu.Click += delegate { series.RenderOptions.m_visible = !series.RenderOptions.m_visible; series_menu.Checked = series.RenderOptions.m_visible; m_dirty = true; Refresh(); };

					{//Visibility
						ToolStripMenuItem elements_menu = new ToolStripMenuItem("Elements");
						series_menu.DropDownItems.Add(elements_menu);
						{// Draw main data
							ToolStripMenuItem option = new ToolStripMenuItem("Series data");
							elements_menu.DropDownItems.Add(option);
							option.Checked = series.RenderOptions.m_draw_data;
							option.Click += delegate { series.RenderOptions.m_draw_data = !series.RenderOptions.m_draw_data; m_dirty = true; Refresh(); };
						}
						{// Draw error bars
							ToolStripMenuItem option = new ToolStripMenuItem("Error Bars");
							elements_menu.DropDownItems.Add(option);
							option.Checked = series.RenderOptions.m_draw_error_bars;
							option.Click += delegate { series.RenderOptions.m_draw_error_bars = !series.RenderOptions.m_draw_error_bars; m_dirty = true; Refresh(); };
						}
						{// Draw zeros
							ToolStripComboBox option = new ToolStripComboBox();
							elements_menu.DropDownItems.Add(option);
							foreach (string s in Enum.GetNames(typeof(Series.RdrOpts.PlotZeros))) { option.Items.Add(s + " Zeros"); }
							option.SelectedIndex = (int)series.RenderOptions.m_draw_zeros;
							option.SelectedIndexChanged += delegate { series.RenderOptions.m_draw_zeros = (Series.RdrOpts.PlotZeros)option.SelectedIndex; m_dirty = true; Refresh(); };
							option.KeyDown += delegate (object sender, KeyEventArgs e) { if(e.KeyCode == Keys.Return) context_menu.Close(); };
						}
					}
					{// Plot Type
						ToolStripMenuItem plot_type_menu = new ToolStripMenuItem("Plot Type");
						series_menu.DropDownItems.Add(plot_type_menu);
						{
							ToolStripComboBox option = new ToolStripComboBox();
							plot_type_menu.DropDownItems.Add(option);
							option.Items.AddRange(Enum.GetNames(typeof(Series.RdrOpts.PlotType)));
							option.SelectedIndex = (int)series.RenderOptions.m_plot_type;
							option.SelectedIndexChanged += delegate { series.RenderOptions.m_plot_type = (Series.RdrOpts.PlotType)option.SelectedIndex; m_dirty = true; Refresh(); };
							option.KeyDown += delegate (object sender, KeyEventArgs e) { if(e.KeyCode == Keys.Return) context_menu.Close(); };
						}
					}
					{// Appearance
						ToolStripMenuItem appearance_menu = new ToolStripMenuItem("Appearance");
						series_menu.DropDownItems.Add(appearance_menu);

						// Points
						if (series.RenderOptions.m_plot_type == Series.RdrOpts.PlotType.Point ||
							series.RenderOptions.m_plot_type == Series.RdrOpts.PlotType.Line)
						{
							ToolStripMenuItem point_menu = new ToolStripMenuItem("Points");
							appearance_menu.DropDownItems.Add(point_menu);
							{// Size
								ToolStripMenuItem size_menu = new ToolStripMenuItem("Size");
								point_menu.DropDownItems.Add(size_menu);
								{
									ToolStripTextBox option = new ToolStripTextBox();
									size_menu.DropDownItems.Add(option);
									option.Text = series.RenderOptions.m_point_size.ToString("0.00");
									option.TextChanged += delegate { float size; if( float.TryParse(option.Text, out size) ) { series.RenderOptions.m_point_size = size; m_dirty = true; Refresh(); } };
								}
							}
							{// Colour
								ToolStripMenuItem colour_menu = new ToolStripMenuItem("Colour");
								point_menu.DropDownItems.Add(colour_menu);
								{
									ToolStripButton option = new ToolStripButton(" ");
									colour_menu.DropDownItems.Add(option);
									option.AutoToolTip = false;
									option.BackColor = series.RenderOptions.m_point_colour;
									option.Click += delegate { ColorDialog cd = new ColorDialog(); if (cd.ShowDialog() == DialogResult.OK) option.BackColor = cd.Color; };
									option.BackColorChanged += delegate { series.RenderOptions.m_point_colour = Color.FromArgb(series.RenderOptions.m_point_colour.A, option.BackColor); m_dirty = true; Refresh(); };
								}
								{
									ToolStripTextBox option = new ToolStripTextBox();
									colour_menu.DropDownItems.Add(option);
									option.Text = series.RenderOptions.m_point_colour.A.ToString();
									option.TextChanged += delegate { byte alp; if (byte.TryParse(option.Text, out alp)) { series.RenderOptions.m_point_colour = Color.FromArgb(alp, series.RenderOptions.m_point_colour); m_dirty = true; Refresh(); } };
								}
							}
						}

						// Lines
						if (series.RenderOptions.m_plot_type == Series.RdrOpts.PlotType.Line)
						{
							ToolStripMenuItem line_menu = new ToolStripMenuItem("Lines");
							appearance_menu.DropDownItems.Add(line_menu);
							{// Width
								ToolStripMenuItem width_menu = new ToolStripMenuItem("Width");
								line_menu.DropDownItems.Add(width_menu);
								{
									ToolStripTextBox option = new ToolStripTextBox();
									width_menu.DropDownItems.Add(option);
									option.Text = series.RenderOptions.m_line_width.ToString("0.00");
									option.TextChanged += delegate { float width; if( float.TryParse(option.Text, out width) ) { series.RenderOptions.m_line_width = width; m_dirty = true; Refresh(); } };
								}
							}
							{// Colour
								ToolStripMenuItem colour_menu = new ToolStripMenuItem("Colour");
								line_menu.DropDownItems.Add(colour_menu);
								{
									ToolStripButton option = new ToolStripButton(" ");
									colour_menu.DropDownItems.Add(option);
									option.AutoToolTip = false;
									option.BackColor = series.RenderOptions.m_line_colour;
									option.Click += delegate { ColorDialog cd = new ColorDialog(); if (cd.ShowDialog() == DialogResult.OK) option.BackColor = cd.Color; };
									option.BackColorChanged += delegate { series.RenderOptions.m_line_colour = Color.FromArgb(series.RenderOptions.m_line_colour.A, option.BackColor); m_dirty = true; Refresh(); };
								}
								{
									ToolStripTextBox option = new ToolStripTextBox();
									colour_menu.DropDownItems.Add(option);
									option.Text = series.RenderOptions.m_line_colour.A.ToString();
									option.TextChanged += delegate { byte alp; if (byte.TryParse(option.Text, out alp)) { series.RenderOptions.m_line_colour = Color.FromArgb(alp, series.RenderOptions.m_line_colour); m_dirty = true; Refresh(); } };
								}
							}
						}

						// Bars
						if (series.RenderOptions.m_plot_type == Series.RdrOpts.PlotType.Bar)
						{
							ToolStripMenuItem bar_menu = new ToolStripMenuItem("Bars");
							appearance_menu.DropDownItems.Add(bar_menu);
							{// Width
								ToolStripMenuItem width_menu = new ToolStripMenuItem("Width");
								bar_menu.DropDownItems.Add(width_menu);
								{
									ToolStripTextBox option = new ToolStripTextBox();
									width_menu.DropDownItems.Add(option);
									option.Text = series.RenderOptions.m_bar_width.ToString("0.00");
									option.TextChanged += delegate { float width; if (float.TryParse(option.Text, out width)) { series.RenderOptions.m_bar_width = width; m_dirty = true; Refresh(); } };
									option.KeyDown += delegate (object sender, KeyEventArgs e) { if(e.KeyCode == Keys.Return) context_menu.Close(); };
								}
							}
							{// Bar colour
								ToolStripMenuItem colour_menu = new ToolStripMenuItem("Colour");
								bar_menu.DropDownItems.Add(colour_menu); 
								{
									ToolStripButton option = new ToolStripButton(" ");
									colour_menu.DropDownItems.Add(option);
									option.AutoToolTip = false;
									option.BackColor = series.RenderOptions.m_bar_colour;
									option.Click += delegate { ColorDialog cd = new ColorDialog(); if (cd.ShowDialog() == DialogResult.OK) option.BackColor = cd.Color; };
									option.BackColorChanged += delegate { series.RenderOptions.m_bar_colour = Color.FromArgb(series.RenderOptions.m_bar_colour.A, option.BackColor); m_dirty = true; Refresh(); };
								}
								{
									ToolStripTextBox option = new ToolStripTextBox();
									colour_menu.DropDownItems.Add(option);
									option.Text = series.RenderOptions.m_bar_colour.A.ToString();
									option.TextChanged += delegate { byte alp; if (byte.TryParse(option.Text, out alp)) { series.RenderOptions.m_bar_colour = Color.FromArgb(alp, series.RenderOptions.m_bar_colour); m_dirty = true; Refresh(); } };
									option.KeyDown += delegate (object sender, KeyEventArgs e) { if(e.KeyCode == Keys.Return) context_menu.Close(); };
								}
							}
						}
					}
					{// Moving average
						ToolStripMenuItem mv_avr_menu = new ToolStripMenuItem("Moving Average");
						series_menu.DropDownItems.Add(mv_avr_menu);
						{
							ToolStripMenuItem option = new ToolStripMenuItem("Draw Moving Average");
							mv_avr_menu.DropDownItems.Add(option);
							option.Checked = series.RenderOptions.m_draw_moving_avr;
							option.Click += delegate { series.RenderOptions.m_draw_moving_avr = !series.RenderOptions.m_draw_moving_avr; m_dirty = true; Refresh(); };
						}
						{
							ToolStripMenuItem win_size_menu = new ToolStripMenuItem("Window Size");
							mv_avr_menu.DropDownItems.Add(win_size_menu);
							{
								ToolStripTextBox option = new ToolStripTextBox();
								win_size_menu.DropDownItems.Add(option);
								option.Text = series.RenderOptions.m_ma_window_size.ToString();
								option.TextChanged += delegate { int size; if (int.TryParse(option.Text, out size)) { series.RenderOptions.m_ma_window_size = size; m_dirty = true; Refresh(); } };
							}
						}
						{
							ToolStripMenuItem line_width_menu = new ToolStripMenuItem("Line Width");
							mv_avr_menu.DropDownItems.Add(line_width_menu);
							{
								ToolStripTextBox option = new ToolStripTextBox();
								line_width_menu.DropDownItems.Add(option);
								option.Text = series.RenderOptions.m_ma_line_width.ToString();
								option.TextChanged += delegate { int size; if (int.TryParse(option.Text, out size)) { series.RenderOptions.m_ma_line_width = size; m_dirty = true; Refresh(); } };
							}
						}
						{
							ToolStripMenuItem line_colour_menu = new ToolStripMenuItem("Line Colour");
							mv_avr_menu.DropDownItems.Add(line_colour_menu);
							{
								ToolStripButton option = new ToolStripButton(" ");
								line_colour_menu.DropDownItems.Add(option);
								option.AutoToolTip = false;
								option.BackColor = series.RenderOptions.m_ma_line_colour;
								option.Click += delegate { ColorDialog cd = new ColorDialog(); if (cd.ShowDialog() == DialogResult.OK) option.BackColor = cd.Color; };
								option.BackColorChanged += delegate { series.RenderOptions.m_ma_line_colour = option.BackColor; m_dirty = true; Refresh(); };
							}
						}
					}
					// Delete series
					if (series.AllowDelete)
					{
						ToolStripMenuItem delete_menu = new ToolStripMenuItem("Delete");
						series_menu.DropDownItems.Add(delete_menu); 
						delete_menu.Click += delegate
						{
							if (MessageBox.Show(this, "Confirm delete?", "Delete Series", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
							m_data.RemoveAt((int)series_menu.Tag);
							FindDefaultRange();
							Refresh();
						};
					}
				}
			}
			{// Rendering options
				ToolStripMenuItem appearance_menu = new ToolStripMenuItem("Appearance");
				context_menu.Items.Add(appearance_menu);
				{
					ToolStripMenuItem bk_colour_menu = new ToolStripMenuItem("Background Colour");
					appearance_menu.DropDownItems.Add(bk_colour_menu);
					{
						ToolStripButton option = new ToolStripButton(" ");
						bk_colour_menu.DropDownItems.Add(option);
						option.AutoToolTip = false;
						option.BackColor = m_rdr_options.m_bg_colour;
						option.Click += delegate { ColorDialog cd = new ColorDialog(); if (cd.ShowDialog() == DialogResult.OK) option.BackColor = cd.Color; };
						option.BackColorChanged += delegate { m_rdr_options.m_bg_colour = option.BackColor; m_dirty = true; Refresh(); };
					}
				}
				if (ParentForm != null)
				{
					ToolStripMenuItem opacity_menu = new ToolStripMenuItem("Opacity");
					appearance_menu.DropDownItems.Add(opacity_menu);
					{
						ToolStripTextBox option = new ToolStripTextBox();
						opacity_menu.DropDownItems.Add(option);
						option.Text = ParentForm.Opacity.ToString("0.00");
						option.TextChanged += delegate { double opc; if (ParentForm != null && double.TryParse(option.Text, out opc)) { ParentForm.Opacity = Math.Max(0.1, Math.Min(1.0, opc)); } };
					}
				}
			}
			{// Add axis options
				ToolStripMenuItem axes_menu = new ToolStripMenuItem("Axes");
				context_menu.Items.Add(axes_menu);
				{
					ToolStripMenuItem x_axis_menu = new ToolStripMenuItem("X Axis");
					axes_menu.DropDownItems.Add(x_axis_menu);
					{
						ToolStripMenuItem min_menu = new ToolStripMenuItem("Min");
						x_axis_menu.DropDownItems.Add(min_menu);
						{
							ToolStripTextBox option = new ToolStripTextBox();
							min_menu.DropDownItems.Add(option);
							option.Text = XAxis.Min.ToString("G5");
							option.TextChanged += delegate { float v; if (float.TryParse(option.Text, out v)) { XAxis.Min = v; m_dirty = true; Refresh(); } };
						}
						ToolStripMenuItem max_menu = new ToolStripMenuItem("Max");
						x_axis_menu.DropDownItems.Add(max_menu);
						{
							ToolStripTextBox option = new ToolStripTextBox();
							max_menu.DropDownItems.Add(option);
							option.Text = XAxis.Max.ToString("G5");
							option.TextChanged += delegate { float v; if (float.TryParse(option.Text, out v)) { XAxis.Max = v; m_dirty = true; Refresh(); } };
						}
						{
							ToolStripMenuItem allow_scroll = new ToolStripMenuItem{Text="Allow Scroll",Checked=m_xaxis.AllowScroll};
							x_axis_menu.DropDownItems.Add(allow_scroll);
							allow_scroll.Click += (s,e)=> {m_xaxis.AllowScroll = !m_xaxis.AllowScroll;};
						}
						{
							ToolStripMenuItem allow_zoom = new ToolStripMenuItem{Text="Allow Zoom",Checked=m_xaxis.AllowZoom};
							x_axis_menu.DropDownItems.Add(allow_zoom);
							allow_zoom.Click += (s,e)=> {m_xaxis.AllowZoom = !m_xaxis.AllowZoom;};
						}
					}
					ToolStripMenuItem y_axis_menu = new ToolStripMenuItem("Y Axis");
					axes_menu.DropDownItems.Add(y_axis_menu);
					{
						ToolStripMenuItem min_menu = new ToolStripMenuItem("Min");
						y_axis_menu.DropDownItems.Add(min_menu);
						{
							ToolStripTextBox option = new ToolStripTextBox();
							min_menu.DropDownItems.Add(option);
							option.Text = YAxis.Min.ToString("G5");
							option.TextChanged += delegate { float v; if (float.TryParse(option.Text, out v)) { YAxis.Min = v; m_dirty = true; Refresh(); } };
						}
						ToolStripMenuItem max_menu = new ToolStripMenuItem("Max");
						y_axis_menu.DropDownItems.Add(max_menu);
						{
							ToolStripTextBox option = new ToolStripTextBox();
							max_menu.DropDownItems.Add(option);
							option.Text = YAxis.Max.ToString("G5");
							option.TextChanged += delegate { float v; if (float.TryParse(option.Text, out v)) { YAxis.Max = v; m_dirty = true; Refresh(); } };
						}
						{
							ToolStripMenuItem allow_scroll = new ToolStripMenuItem{Text="Allow Scroll",Checked=m_yaxis.AllowScroll};
							y_axis_menu.DropDownItems.Add(allow_scroll);
							allow_scroll.Click += (s,e)=> {m_yaxis.AllowScroll = !m_yaxis.AllowScroll;};
						}
						{
							ToolStripMenuItem allow_zoom = new ToolStripMenuItem{Text="Allow Zoom",Checked=m_yaxis.AllowZoom};
							y_axis_menu.DropDownItems.Add(allow_zoom);
							allow_zoom.Click += (s,e)=> {m_yaxis.AllowZoom = !m_yaxis.AllowZoom;};
						}
					}
				}
			}
			{// Add Note
				ToolStripMenuItem note_menu = new ToolStripMenuItem("Notes");
				context_menu.Items.Add(note_menu);
				{
					ToolStripMenuItem add_menu = new ToolStripMenuItem("Add");
					note_menu.DropDownItems.Add(add_menu);
					add_menu.Click += (s,e)=>
					{
						Form form = new Form{Text="Add Note", FormBorderStyle=FormBorderStyle.SizableToolWindow, StartPosition=FormStartPosition.Manual, Location=PointToScreen(location), Size=new Size(160,100), KeyPreview=true};
						form.KeyDown += (o,a)=>{ if (a.KeyCode == Keys.Enter && (a.Modifiers&Keys.Control) == 0) form.DialogResult = DialogResult.OK; };
						TextBox tb = new TextBox{Dock=DockStyle.Fill, Multiline=true, AcceptsReturn=false};
						form.Controls.Add(tb);
						if (form.ShowDialog(this) != DialogResult.OK || tb.Text.Length == 0) return;
						m_notes.Add(new Note(tb.Text, PointToGraph(location)));
						m_dirty = true;
						Refresh();
					};
				}
				{
					Note nearest = null; float dist = 100f;
					foreach (Note note in m_notes)
					{
						Point sep = GraphToPoint(note.m_loc) - (Size)location;
						float d = (float)Math.Sqrt(sep.X*sep.X + sep.Y*sep.Y);
						if (d < dist) { dist = d; nearest = note; }
					}
					if (nearest != null)
					{
						string quote = nearest.m_msg.Length < 12 ? nearest.m_msg : nearest.m_msg.Substring(0,12) + "...";
						ToolStripMenuItem del_menu = new ToolStripMenuItem("Delete '"+quote+"'");
						note_menu.DropDownItems.Add(del_menu);
						del_menu.Click += (s,e)=>{ m_notes.Remove(nearest); };
						m_dirty = true;
						Refresh();
					}
				}
			}
			{//Export
				ToolStripMenuItem export_menu = new ToolStripMenuItem("Export");
				context_menu.Items.Add(export_menu);
				export_menu.Click += delegate { ExportCSV(); };
			}
			{//Import
				ToolStripMenuItem import_menu = new ToolStripMenuItem("Import");
				context_menu.Items.Add(import_menu);
				import_menu.Click += delegate { ImportCSV(null); };
			}
			// Show the context menu
			context_menu.Show(this, location);
		}

		/// <summary>Handle mouse move events while the tooltip is visible</summary>
		private void OnMouseMoveTooltip(object sender, MouseEventArgs e)
		{
			if (GraphRegion(ClientSize).Contains(e.Location))
			{
				PointF pt = PointToGraph(e.Location);
				string old_str = m_tooltip.GetToolTip(this);
				string new_str = pt.ToString();
				if (old_str != new_str) // Fix a flickering problem for non-moving mouse
					m_tooltip.Show(new_str, this, e.Location + new Size(20, 0));
			}
			else
				m_tooltip.Hide(this);
		}

		/// <summary>Handle mouse dragging the graph around</summary>
		private void OnMouseGraphDrag(object sender, MouseEventArgs e)
		{
			Point grab_loc = GraphToPoint(m_grab_location);
			int dx = e.Location.X - grab_loc.X;
			int dy = e.Location.Y - grab_loc.Y;
			if (dx*dx + dy*dy < 25) return; // must drag at least 5 pixels
			PositionGraph(e.Location, m_grab_location);
			Refresh();
		}

		/// <summary>Handle mouse move events while dragging</summary>
		private void OnMouseAreaSelect(object sender, MouseEventArgs e)
		{
			const int MinAreaSelectDistance = 3;
			m_selection.Width  = e.Location.X - m_selection.X;
			m_selection.Height = e.Location.Y - m_selection.Y;
			if (Math.Abs(m_selection.Width)  < MinAreaSelectDistance) m_selection.Width  = 0;
			if (Math.Abs(m_selection.Height) < MinAreaSelectDistance) m_selection.Height = 0;
			Refresh();
		}

		/// <summary>Snapshot the current graph to use for dragging/zooming</summary>
		private void TakeSnapshot()
		{
			if (m_bm == null || m_snap.m_bm != null) return;
			Rectangle rect = GraphRegion(ClientSize); rect.Inflate(-1,-1); rect.Offset(1,1);
			m_snap.m_bm = m_bm.Clone(rect, m_bm.PixelFormat);
			m_snap.m_xrange = m_xaxis.Rng.Clone();
			m_snap.m_yrange = m_yaxis.Rng.Clone();
		}

		// Resize
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			SetSuitableStepSizes(ClientSize);
			m_dirty = true;
			Refresh();
		}
		
		// Absorb this event
		protected override void OnPaintBackground(PaintEventArgs e) {}

		// Paint the control
		protected override void OnPaint(PaintEventArgs e)
		{
			try
			{
				// If the local bitmap does not exist, or its size is wrong, redraw it
				m_dirty |= m_bm == null || m_bm.Size != ClientSize;
				if (m_dirty)
				{
					// When rendering the graph bitmap completes, trigger another refresh of the control
					m_bm = GetGraphBitmap(ClientSize, true, (snd,bm)=>{ m_bm = bm; Refresh(); }, m_bm);
					m_dirty = false;
				}
				else
				{
					m_snap.m_bm = null;
				}

				// Render the graph
				if (m_bm != null)
					e.Graphics.DrawImage(m_bm, 0, 0);

				// If a snapshot exists render the captured bitmap
				if (m_snap.m_bm != null)
				{
					Rectangle src_rect = new Rectangle(Point.Empty, m_snap.m_bm.Size);
					Rectangle dst_rect = GraphRegion(ClientSize); dst_rect.Inflate(-1,-1); dst_rect.Offset(1,1);
					e.Graphics.SetClip(dst_rect);
					dst_rect.Location = GraphToPoint(new PointF(m_snap.m_xrange.m_min, m_snap.m_yrange.m_max));
					dst_rect.Width    = (int)(dst_rect.Width  * m_snap.m_xrange.m_span / m_xaxis.Rng.m_span);
					dst_rect.Height   = (int)(dst_rect.Height * m_snap.m_yrange.m_span / m_yaxis.Rng.m_span);

					e.Graphics.DrawImage(m_snap.m_bm, dst_rect, src_rect, GraphicsUnit.Pixel);
					e.Graphics.ResetClip();
				}

				// Allow clients to draw in graph space
				if (AddOverlaysOnPaint != null)
				{
					Rectangle region = GraphRegion(ClientSize);
					float scale_x = region.Width  / m_xaxis.Span;
					float scale_y = region.Height / m_yaxis.Span;
					e.Graphics.SetClip(region);
				
					// Can't use inverted y scale here because the text comes out upside down
					e.Graphics.Transform = new Matrix(1f, 0f, 0f, 1f, region.Left - m_xaxis.Min * scale_x, region.Bottom + m_yaxis.Min * scale_y);
					scale_y = -scale_y;
					AddOverlaysOnPaint(this, e.Graphics, scale_x, scale_y);
					e.Graphics.ResetTransform();
					e.Graphics.ResetClip(); // If a selection is in progress, draw the selection box
				}

				if (m_selection.Width != 0 && m_selection.Height != 0)
				{
					Pen pen = new Pen(Color.Black);
					pen.DashStyle = DashStyle.Dot;
					Rectangle rect = m_selection;
					if( rect.Width  < 0 ) { rect.X += rect.Width;  rect.Width  = -rect.Width;  }
					if( rect.Height < 0 ) { rect.Y += rect.Height; rect.Height = -rect.Height; }
					e.Graphics.DrawRectangle(pen, rect);
				}
			}
			catch (OverflowException)
			{
				// There is a problem in the .NET graphics object that can cause these exceptions if the range is extreme
				e.Graphics.DrawString("Rendering error within .NET", m_rdr_options.m_title_font, new SolidBrush(Color.FromArgb(0x80, Color.Black)), new PointF());
			}
		}

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
		
			// ReSharper disable DoNotCallOverridableMethodsInConstructor
			public ExportForm()
			{
				SuspendLayout();
				
				// m_lbl_export_path
				m_lbl_export_path = new Label();
				m_lbl_export_path.AutoSize = true;
				m_lbl_export_path.Location = new Point(12, 9);
				m_lbl_export_path.Name = "m_lbl_export_path";
				m_lbl_export_path.Size = new Size(65, 13);
				m_lbl_export_path.TabIndex = 0;
				m_lbl_export_path.Text = "Export Path:";

				// m_edit_export_path
				m_edit_export_path = new TextBox();
				m_edit_export_path.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
				m_edit_export_path.Location = new Point(12, 25);
				m_edit_export_path.Name = "m_edit_export_path";
				m_edit_export_path.Size = new Size(143, 20);
				m_edit_export_path.TabIndex = 1;
				
				// m_btn_browse
				m_btn_browse = new Button();
				m_btn_browse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
				m_btn_browse.Location = new Point(159, 23);
				m_btn_browse.Name = "m_btn_browse";
				m_btn_browse.Size = new Size(75, 23);
				m_btn_browse.TabIndex = 2;
				m_btn_browse.Text = "Browse...";
				m_btn_browse.UseVisualStyleBackColor = true;
				m_btn_browse.Click += delegate
				{
					SaveFileDialog fd = new SaveFileDialog();
					fd.Filter = "CSV file (*.csv)|*.csv";
					if (fd.ShowDialog() != DialogResult.OK) return;
					m_edit_export_path.Text = fd.FileName;
				};
				
				// m_lbl_series_to_export
				m_lbl_series_to_export = new Label();
				m_lbl_series_to_export.AutoSize = true;
				m_lbl_series_to_export.Location = new Point(9, 48);
				m_lbl_series_to_export.Name = "m_lbl_series_to_export";
				m_lbl_series_to_export.Size = new Size(83, 13);
				m_lbl_series_to_export.TabIndex = 3;
				m_lbl_series_to_export.Text = "Series to export:";
				
				// m_list_series_list
				m_list_series_list = new CheckedListBox();
				m_list_series_list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
				m_list_series_list.FormattingEnabled = true;
				m_list_series_list.Location = new Point(12, 64);
				m_list_series_list.Name = "m_list_series_list";
				m_list_series_list.Size = new Size(227, 79);
				m_list_series_list.TabIndex = 4;

				// m_btn_ok
				m_btn_ok = new Button();
				m_btn_ok.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
				m_btn_ok.DialogResult = DialogResult.OK;
				m_btn_ok.Location = new Point(164, 151);
				m_btn_ok.Name = "m_btn_ok";
				m_btn_ok.Size = new Size(75, 23);
				m_btn_ok.TabIndex = 7;
				m_btn_ok.Text = "OK";
				m_btn_ok.UseVisualStyleBackColor = true;
				
				// m_btn_cancel
				m_btn_cancel = new Button();
				m_btn_cancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
				m_btn_cancel.DialogResult = DialogResult.Cancel;
				m_btn_cancel.Location = new Point(83, 151);
				m_btn_cancel.Name = "m_btn_cancel";
				m_btn_cancel.Size = new Size(75, 23);
				m_btn_cancel.TabIndex = 8;
				m_btn_cancel.Text = "Cancel";
				m_btn_cancel.UseVisualStyleBackColor = true;

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
			// ReSharper restore DoNotCallOverridableMethodsInConstructor
		}

		/// <summary>Occurs if an error occurs while exporting CSV data</summary>
		public Action<Exception> ExportCSVError;

		/// <summary>Occurs if an error occurs while importing CSV data</summary>
		public Action<Exception> ImportCSVError;

		/// <summary>Export the graph data to an CSV file</summary>
		private void ExportCSV()
		{
			ExportForm exp = new ExportForm();
			foreach (Series series in m_data)
			{
				exp.m_list_series_list.Items.Add(series.Name, true);
			}
			if (exp.ShowDialog() != DialogResult.OK) return;

			// Write the file
			try
			{
				using (TextWriter file = new StreamWriter(exp.m_edit_export_path.Text))
				{
					file.WriteLine(m_title);

					for (int i = 0; i != m_data.Count; ++i)
					{
						if (exp.m_list_series_list.GetItemCheckState(i) != CheckState.Checked)
							continue;

						file.WriteLine(m_data[i].Name);
						file.WriteLine(m_xaxis.Label+","+m_yaxis.Label+",Lower Bound,Upper Bound");
						foreach (GraphValue gv in m_data[i].Values)
							file.WriteLine(gv.m_valueX+","+gv.m_valueY+","+gv.m_lower_err+","+gv.m_upper_err);
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
				OpenFileDialog fd = new OpenFileDialog();
				fd.Filter = "CSV file (*.csv)|*.csv";
				fd.Multiselect = true;
				if (fd.ShowDialog() != DialogResult.OK) return;
				filenames = fd.FileNames;
			}
			foreach (string filename in filenames)
			{
				try
				{
					using (TextReader file = new StreamReader(filename))
					{
						Series series = new Series();
						Action<Series> AddSeries = (s)=>
						{
							if (s.Values.Count == 0) return;
							m_data.Add(s);
						};

						// Read a line from the file,
						for (string line = file.ReadLine(); line != null; line = file.ReadLine())
						{
							// how many tokens can it be split into?
							string[] tokens = line.Split(',', '\n');

							// 1->a title/name, the start of another series
							if (tokens.Length == 1)
							{
								AddSeries(series);
								series = new Series();
								series.Name = tokens[0];
								series.AllowDelete = true;
							}
							// 4-> data
							else if (tokens.Length == 4)
							{
								double v0, v1, v2, v3;
								if (double.TryParse(tokens[0], out v0) &&
									double.TryParse(tokens[1], out v1) &&
									double.TryParse(tokens[2], out v2) &&
									double.TryParse(tokens[3], out v3))
									series.Values.Add(new GraphValue(v0, v1, v2, v3, null));
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

		#region Component Designer generated code
		// Required designer variable.
		private System.ComponentModel.IContainer components = null;

		// Clean up any resources being used.
		protected override void Dispose(bool disposing)
		{
			if( disposing && (components != null) )
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.SuspendLayout();
			// 
			// GraphControl
			// 
			this.Name = "GraphControl";
			this.ResumeLayout(false);

		}
		#endregion

		public void BeginInit(){}
		public void EndInit(){}
	}
}