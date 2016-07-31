//***************************************************
// Chart Control
// Copyright (C) Rylogic Ltd 2016
//***************************************************
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using pr.util;
using pr.gfx;
using pr.maths;
using pr.extn;
using pr.container;
using System.Xml.Linq;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using pr.common;
using pr.win32;
using System.Linq;

namespace pr.gui
{
	/// <summary>A view 3d based chart control</summary>
	public class ChartControl :UserControl
	{
		#region Constants
		public static readonly Guid GridId = new Guid("1783F2BF-2782-4C39-98AF-C5935B1003E9");
		private const float GridLinesZ = -0.001f;
		#endregion

		public ChartControl()
			:this(string.Empty, new RdrOptions())
		{ }
		public ChartControl(string title, RdrOptions options)
		{
			SetStyle(ControlStyles.ResizeRedraw, true);
			DoubleBuffered = true;
			InitializeComponent();
			try
			{
				Options         = options;
				Title           = title;
				Range           = new RangeData(this);
				BaseRangeX      = new RangeF(0.0, 1.0);
				BaseRangeY      = new RangeF(0.0, 1.0);
				Chart           = new ChartPanel(this); // Must come after 'Range'
				m_impl_zoom     = new RangeF(float.Epsilon, float.MaxValue);
				m_tt_show_value = new ToolTip { ShowAlways = false, UseAnimation = false, UseFading = false, Tag = false };

				Elements = new BindingListEx<Element> { PerItemClear = true, UseHashSet = true };
				Selected = new BindingListEx<Element> { PerItemClear = false };
				ElemIds  = new Dictionary<Guid, Element>();

				Elements.ListChanging += (s,a) => OnElementListChanging(a);
				Selected.ListChanging += (s,a) => OnSelectionListChanging(a);

				MouseNavigation = true;
				FindDefaultRange(visible_only:false);
			}
			catch
			{
				Dispose();
				throw;
			}
		}
		protected override void Dispose(bool disposing)
		{
			Range = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnLayout(LayoutEventArgs e)
		{
			if (Chart != null && !this.IsInDesignMode())
			{
				using (this.SuspendLayout(false))
				{
					var dims = ChartDimensions;
					Chart.Bounds = dims.ChartArea;
				}
			}
			base.OnLayout(e);
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			DoPaint(e.Graphics);
		}
		protected override void OnInvalidated(InvalidateEventArgs e)
		{
			base.OnInvalidated(e);
			Chart?.Invalidate();
		}

		/// <summary>Rendering options for the chart</summary>
		public RdrOptions Options
		{
			get { return m_rdr_options; }
			set
			{
				if (m_rdr_options == value) return;
				if (m_rdr_options != null) m_rdr_options.PropertyChanged -= HandleRdrOptionsChanged;
				m_rdr_options = value;
				if (m_rdr_options != null) m_rdr_options.PropertyChanged += HandleRdrOptionsChanged;
			}
		}
		private RdrOptions m_rdr_options;
		public event EventHandler OptionsChanged;
		protected virtual void OnRdrOptionsChanged()
		{
			OptionsChanged.Raise(this);
		}
		private void HandleRdrOptionsChanged(object sender, PropertyChangedEventArgs e)
		{
			OnRdrOptionsChanged();
		}

		/// <summary>The title of the chart</summary>
		public string Title
		{
			get { return m_title ?? string.Empty; }
			set
			{
				if (m_title == value) return;
				m_title = value;
				Invalidate();
			}
		}
		private string m_title;

		/// <summary>The current X/Y axis range of the chart</summary>
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
		public RangeData.Axis XAxis
		{
			get { return Range.XAxis; }
		}

		/// <summary>Accessor to the current Y axis</summary>
		public RangeData.Axis YAxis
		{
			get { return Range.YAxis; }
		}

		/// <summary>Default X axis range of the chart</summary>
		public RangeF BaseRangeX
		{
			get;
			set;
		}

		/// <summary>Default Y axis range of the chart</summary>
		public RangeF BaseRangeY
		{
			get;
			set;
		}

		/// <summary>The view3d part of the chart</summary>
		public ChartPanel Chart
		{
			get { return m_impl_chart; }
			set
			{
				if (m_impl_chart == value) return;
				if (m_impl_chart != null)
				{
					Controls.Remove(m_impl_chart);
					Util.Dispose(ref m_impl_chart);
				}
				m_impl_chart = value;
				if (m_impl_chart != null)
				{
					Controls.Add(m_impl_chart);
				}
			}
		}
		private ChartPanel m_impl_chart;

		/// <summary>All chart objects</summary>
		public BindingListEx<Element> Elements { get; private set; }

		/// <summary>The set of selected chart elements</summary>
		public BindingListEx<Element> Selected { get; private set; }

		/// <summary>Associative array from Element Ids to Elements</summary>
		public Dictionary<Guid, Element> ElemIds { get; private set; }

		/// <summary>Raised whenever elements in the chart have been edited or moved</summary>
		public event EventHandler<ChartChangedEventArgs> ChartChaged;
		protected virtual void OnChartChanged(ChartChangedEventArgs args)
		{
			ChartChaged.Raise(this, args);
		}
		private ChartChangedEventArgs RaiseChartChanged(ChartChangedEventArgs args)
		{
			if (!ChartChaged.IsSuspended())
				OnChartChanged(args);

			return args;
		}
		public Scope SuspendChartChanged(bool raise_on_resume = true)
		{
			return ChartChaged.Suspend(raise_if_signalled: raise_on_resume, sender: this, args: new ChartChangedEventArgs(EChangeType.Edited));
		}

		/// <summary>Called just before the chart renders. Used to add View3d objects to the scene</summary>
		public event EventHandler<ChartRenderingEventArgs> ChartRendering;
		protected virtual void OnChartRendering(ChartRenderingEventArgs args)
		{
			ChartRendering.Raise(this, args);
		}
		private void RaiseChartRendering(ChartRenderingEventArgs args)
		{
			if (!ChartRendering.IsSuspended())
				OnChartRendering(args);
		}

		/// <summary>Called after the chart has painted, allowing users to add graphics on top of the chart</summary>
		public event EventHandler<AddOverlaysOnPaintEventArgs> AddOverlaysOnPaint;
		protected virtual void OnAddOverlaysOnPaint(AddOverlaysOnPaintEventArgs args)
		{
			AddOverlaysOnPaint.Raise(this, args);
		}
		private void RaiseAddOverlaysOnPaint(AddOverlaysOnPaintEventArgs args)
		{
			if (!AddOverlaysOnPaint.IsSuspended())
				OnAddOverlaysOnPaint(args);
		}

		/// <summary>Returns a point in chart space from a point in client space. Use to convert mouse (client-space) locations to chart coordinates</summary>
		public PointF ClientToChart(Point client_point)
		{
			return new PointF(
				(float)(XAxis.Min + (client_point.X - Chart.Bounds.Left) * XAxis.Span / Chart.Bounds.Width),
				(float)(YAxis.Min - (client_point.Y - Chart.Bounds.Bottom) * YAxis.Span / Chart.Bounds.Height));
		}

		/// <summary>Returns a point in client space from a point in chart space. Inverse of PointToChart</summary>
		public Point ChartToClient(PointF chart_point)
		{
			return new Point(
				(int)(Chart.Bounds.Left   + (chart_point.X - XAxis.Min) * Chart.Bounds.Width  / XAxis.Span),
				(int)(Chart.Bounds.Bottom - (chart_point.Y - YAxis.Min) * Chart.Bounds.Height / YAxis.Span));
		}

		/// <summary>
		/// Get the scale and translation transform from chart space to client space.
		/// e.g. chart2client * Point(x_min, y_min) = plot_area.BottomLeft()
		///      chart2client * Point(x_max, y_max) = plot_area.TopRight()</summary>
		public m4x4 ChartToClientSpace(Rectangle plot_area)
		{
			var scale_x  = (float)+(plot_area.Width  / XAxis.Span);
			var scale_y  = (float)-(plot_area.Height / YAxis.Span);
			var offset_x = (float)+(plot_area.Left   - XAxis.Min * scale_x);
			var offset_y = (float)+(plot_area.Bottom - YAxis.Min * scale_y);

			// C = chart, c = client
			var C2c = new m4x4(
				new v4(scale_x  , 0        , 0, 0),
				new v4(0        , scale_y  , 0, 0),
				new v4(0        , 0        , 1, 0),
				new v4(offset_x , offset_y , 0, 1));

			#if false
			// Check the XAxis corners map to the expected client space locations
			var C_pt0 = new v4((float)XAxis.Min, (float)YAxis.Min, 0, 1);
			var C_pt1 = new v4((float)XAxis.Max, (float)YAxis.Max, 0, 1);
			var c_pt0 = C2c * C_pt0;
			var c_pt1 = C2c * C_pt1;
			Debug.Assert(Math.Abs(c_pt0.x - plot_area.Left  ) < 0.001);
			Debug.Assert(Math.Abs(c_pt0.y - plot_area.Bottom) < 0.001);
			Debug.Assert(Math.Abs(c_pt1.x - plot_area.Right ) < 0.001);
			Debug.Assert(Math.Abs(c_pt1.y - plot_area.Top   ) < 0.001);
			#endif

			return C2c;
		}
		public m4x4 ChartToClientSpace()
		{
			return ChartToClientSpace(Chart.Bounds);
		}

		/// <summary>
		/// Get the scale and translation transform from client space to chart space.
		/// e.g. client2chart * plot_area.BottomLeft() = Point(x_min, y_min)
		///      client2chart * plot_area.TopRight()   = Point(x_max, y_max)</summary>
		public m4x4 ClientToChartSpace(Rectangle plot_area)
		{
			var scale_x  = (float)+(XAxis.Span / plot_area.Width );
			var scale_y  = (float)-(YAxis.Span / plot_area.Height);
			var offset_x = (float)+(XAxis.Min - plot_area.Left   * scale_x);
			var offset_y = (float)+(YAxis.Min - plot_area.Bottom * scale_y);

			// C = chart, c = client
			var c2C = new m4x4(
				new v4(scale_x  , 0        , 0, 0),
				new v4(0        , scale_y  , 0, 0),
				new v4(0        , 0        , 1, 0),
				new v4(offset_x , offset_y , 0, 1));

			#if true
			// Check the plot_area corners map to the expected graph space locations
			var c_pt0 = new v4((float)plot_area.Left , (float)plot_area.Bottom, 0, 1);
			var c_pt1 = new v4((float)plot_area.Right, (float)plot_area.Top   , 0, 1);
			var C_pt0 = c2C * c_pt0;
			var C_pt1 = c2C * c_pt1;
			Debug.Assert(Math.Abs(C_pt0.x - XAxis.Min) < 0.001f);
			Debug.Assert(Math.Abs(C_pt0.y - YAxis.Min) < 0.001f);
			Debug.Assert(Math.Abs(C_pt1.x - XAxis.Max) < 0.001f);
			Debug.Assert(Math.Abs(C_pt1.y - YAxis.Max) < 0.001f);
			#endif

			return c2C;
		}
		public m4x4 ClientToChartSpace()
		{
			return ClientToChartSpace(Chart.Bounds);
		}

		/// <summary>
		/// Find the appropriate range for all data in the chart.
		/// Call ResetToDefaultRange() to zoom the chart to this range</summary>
		public void FindDefaultRange(bool visible_only)
		{
			// Measure the range on each axis
			var xrange = FindRange(e => new RangeF(e.Bounds.MinX, e.Bounds.MaxX), Elements, visible_only);
			var yrange = FindRange(e => new RangeF(e.Bounds.MinY, e.Bounds.MaxY), Elements, visible_only);

			// Allow users to adjust the default range
			var args = new FindingDefaultRangeEventArgs(xrange, yrange);
			OnFindingDefaultRange(args);
			xrange = args.XRange;
			yrange = args.YRange;

			// Scale up the ranges to leave a margin around the default range
			const float MarginScale = 1.00f;//1.05f
			if (xrange.Size > 0) xrange = xrange.Scale(MarginScale); else xrange = new RangeF(0.0, 1.0);
			if (yrange.Size > 0) yrange = yrange.Scale(MarginScale); else yrange = new RangeF(0.0, 1.0);
			BaseRangeX = xrange;
			BaseRangeY = yrange;
		}

		/// <summary>Find the appropriate range on a single axis</summary>
		public static RangeF FindRange(Func<Element, RangeF> selector, IEnumerable<Element> elements, bool visible_only)
		{
			var range = new RangeF(double.MaxValue, -double.MaxValue);
			foreach (var elem in elements)
			{
				if (!elem.VisibleToFindRange)
					continue;
				if (visible_only && !elem.Visible)
					continue;

				// Get the bounding box of the chart element
				var bnds = selector(elem);
				if (bnds.Begin < range.Begin) range.Begin = bnds.Begin;
				if (bnds.End   > range.End  ) range.End   = bnds.End;
			}

			return range;
		}

		/// <summary>Raised when the default range is being found</summary>
		public event EventHandler<FindingDefaultRangeEventArgs> FindingDefaultRange;
		protected virtual void OnFindingDefaultRange(FindingDefaultRangeEventArgs args)
		{
			FindingDefaultRange.Raise(this, args);
		}

		/// <summary>
		/// Reset the axis ranges to the default.
		/// Call FindDefaultRange() to set the default range</summary>
		public void ResetToDefaultRange()
		{
			if (!XAxis.LockRange) { Range.XAxis.Set(BaseRangeX); }
			if (!YAxis.LockRange) { Range.YAxis.Set(BaseRangeY); }
			Invalidate();
		}

		/// <summary>Paint the control</summary>
		private void DoPaint(Graphics gfx)
		{
			try
			{
				// Get the areas to draw in
				var dims = ChartDimensions;

				// Render the 3d scene
				Chart.Bounds = dims.ChartArea;
				Chart.DoPaint();

				// Draw the chart frame
				DoPaintFrame(gfx, dims);

				// Add user graphics over the chart
				RaiseAddOverlaysOnPaint(new AddOverlaysOnPaintEventArgs(gfx, dims, ChartToClientSpace()));
			}
			catch (OverflowException)
			{
				// There is a problem in the .NET graphics object that can cause these exceptions if the range is extreme
				using (var bsh = new SolidBrush(Color.FromArgb(0x80, Color.Black)))
					gfx.DrawString("Rendering error within .NET", Options.TitleFont, bsh, PointF.Empty);
			}
		}

		/// <summary>Draw the titles, axis labels, ticks, etc around the chart</summary>
		private void DoPaintFrame(Graphics gfx, ChartDims dims)
		{
			// This is not enforced in the axis.Min/Max accessors because it's useful
			// to be able to change the min/max independently of each other, set them
			// to float max etc. It's only invalid to render a chart with a negative range
			Debug.Assert(XAxis.Span > 0, "Negative x range");
			Debug.Assert(YAxis.Span > 0, "Negative y range");

			// Clear to the background colour
			gfx.Clear(Options.BkColour);

			// Draw the chart title and labels
			if (Title.HasValue())
			{
				using (var bsh = new SolidBrush(Options.TitleColour))
				{
					var r = gfx.MeasureString(Title, Options.TitleFont);
					var x = (dims.Area.Width - r.Width) * 0.5f;
					var y = (dims.Area.Top + Options.Margin.Top) * 1f;
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
					var x = (dims.Area.Width - r.Width) * 0.5f;
					var y = (dims.Area.Bottom - Options.Margin.Bottom - r.Height) * 1f;
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
					var x = (dims.Area.Left + Options.Margin.Left) * 1f;
					var y = (dims.Area.Height + r.Width) * 0.5f;
					gfx.TranslateTransform(x, y);
					gfx.RotateTransform(-90.0f);
					gfx.MultiplyTransform(YAxis.Options.LabelTransform);
					gfx.DrawString(YAxis.Label, YAxis.Options.LabelFont, bsh, PointF.Empty);
					gfx.ResetTransform();
				}
			}

			// Tick marks and labels
			var lblx = (float)(dims.ChartArea.Left - YAxis.Options.TickLength - 1);
			var lbly = (float)(dims.ChartArea.Top + dims.ChartArea.Height + XAxis.Options.TickLength + 1);
			if (XAxis.Options.DrawTickLabels || XAxis.Options.DrawTickMarks)
			{
				using (var pen = new Pen(XAxis.Options.TickColour))
				using (var bsh = new SolidBrush(XAxis.Options.TickColour))
				{
					double min, max, step;
					XAxis.GridLines(out min, out max, out step);
					for (var x = min; x < max; x += step)
					{
						var X = (int)(dims.ChartArea.Left + x * dims.ChartArea.Width / XAxis.Span);
						var s = XAxis.TickText(x + XAxis.Min, step);
						var r = gfx.MeasureString(s, XAxis.Options.TickFont);
						if (XAxis.Options.DrawTickLabels)
							gfx.DrawString(s, XAxis.Options.TickFont, bsh, new PointF(X - r.Width*0.5f, lbly));
						if (XAxis.Options.DrawTickMarks)
							gfx.DrawLine(pen, X, dims.ChartArea.Top + dims.ChartArea.Height, X, dims.ChartArea.Top + dims.ChartArea.Height + XAxis.Options.TickLength);
					}
				}
			}
			if (YAxis.Options.DrawTickLabels || YAxis.Options.DrawTickMarks)
			{
				using (var pen = new Pen(YAxis.Options.TickColour))
				using (var bsh = new SolidBrush(YAxis.Options.TickColour))
				{
					double min, max, step;
					YAxis.GridLines(out min, out max, out step);
					for (var y = min; y < max; y += step)
					{
						var Y = (int)(dims.ChartArea.Top + dims.ChartArea.Height - y * dims.ChartArea.Height / YAxis.Span);
						var s = YAxis.TickText(y + YAxis.Min, step);
						var r = gfx.MeasureString(s, YAxis.Options.TickFont);
						if (YAxis.Options.DrawTickLabels)
							gfx.DrawString(s, YAxis.Options.TickFont, bsh, new PointF(lblx - r.Width, Y - r.Height*0.5f));
						if (YAxis.Options.DrawTickMarks)
							gfx.DrawLine(pen, dims.ChartArea.Left - YAxis.Options.TickLength, Y, dims.ChartArea.Left, Y);
					}
				}
			}

			// Axes
			using (var pen = new Pen(XAxis.Options.AxisColour, XAxis.Options.AxisThickness))
			{
				var y = dims.ChartArea.Bottom;
				gfx.DrawLine(pen, new Point(dims.ChartArea.Left, y), new Point(dims.ChartArea.Right, y));
			}
			using (var pen = new Pen(YAxis.Options.AxisColour, YAxis.Options.AxisThickness))
			{
				var x = (int)(dims.ChartArea.Left - XAxis.Options.AxisThickness*0.5f);
				gfx.DrawLine(pen, new Point(x, dims.ChartArea.Top), new Point(x, dims.ChartArea.Bottom));
			}
		}

		/// <summary>The Z value of the highest element in the diagram</summary>
		private float HighestZ { get; set; }

		/// <summary>The Z value of the lowest element in the diagram</summary>
		private float LowestZ { get; set; }

		/// <summary>Set the Z value for all elements.</summary>
		private void UpdateElementZOrder()
		{
			LowestZ = 0f;
			HighestZ = 0f;

			foreach (var elem in Elements)
				elem.PositionZ = HighestZ += 0.001f;
		}

		/// <summary>Return the layout sizes of the chart</summary>
		public ChartDims ChartDimensions
		{
			get { return new ChartDims(this); }
		}

		#region Chart Panel
		public class ChartPanel :Control
		{
			private ChartControl          m_owner;         // The containing chart control
			private View3d                m_view3d;        // Renderer
			private View3d.Window         m_window;        // A view3d window for this control instance
			private View3d.CameraControls m_camera;        // The virtual window over the diagram
			private bool                  m_render_needed; // True when the scene needs rendering again

			public ChartPanel(ChartControl owner)
			{
				SetStyle(ControlStyles.Selectable, false);
				if (this.IsInDesignMode())
					return;

				try
				{
					m_owner = owner;
					m_view3d = new View3d();
					m_window = new View3d.Window(m_view3d, Handle, new View3d.WindowOptions(false, null, IntPtr.Zero) { DbgName = "Chart" });
					m_window.LightProperties = View3d.Light.Directional(-v4.ZAxis, Colour32.Zero, Colour32.Gray, Colour32.Zero, 0f, 0f);
					m_window.FocusPointVisible = false;
					m_window.OriginVisible = false;
					m_window.Orthographic = true;
					m_camera = m_window.Camera;
					m_camera.Orthographic = true;
					m_camera.SetClipPlanes(0.5f, 1.5f, true);
				}
				catch
				{
					Dispose();
					throw;
				}
			}
			protected override void Dispose(bool disposing)
			{
				Util.Dispose(ref m_view3d);
				base.Dispose(disposing);
			}
			protected override void OnResize(EventArgs e)
			{
				base.OnResize(e);
				if (m_window != null)
					m_window.RenderTargetSize = ClientSize;
			}
			protected override void OnPaintBackground(PaintEventArgs e)
			{
				// Swallow
			}
			protected override void OnPaint(PaintEventArgs e)
			{
				base.OnPaint(e);
				if (m_render_needed) DoPaint();
				m_window?.Present();
			}
			protected override void OnInvalidated(InvalidateEventArgs e)
			{
				base.OnInvalidated(e);
				m_render_needed = true;
				m_window?.Invalidate();
			}
			protected override void WndProc(ref Message m)
			{
				switch (m.Msg)
				{
				case Win32.WM_NCHITTEST:
					// Transparent to input events, let the owner control handle them
					m.Result = (IntPtr)Win32.HitTest.HTTRANSPARENT;
					return;
				}
				base.WndProc(ref m);
			}

			/// <summary>Render the chart 3d scene</summary>
			public void DoPaint()
			{
				if (m_window == null || this.IsInDesignMode())
					return;

				// Clear the scene
				m_window.RemoveAllObjects();

				// Add axis graphics
				m_owner.Range.AddToScene(m_window);

				// Add all chart elements
				foreach (var elem in m_owner.Elements)
				{
					if (!elem.Visible) continue;
					elem.AddToScene(m_window);
				}

				// Add user graphics
				m_owner.RaiseChartRendering(new ChartRenderingEventArgs(m_window));

				// Position the camera based on the axis range.
				var centre = new v4((float)m_owner.XAxis.Centre, (float)m_owner.YAxis.Centre, 0f, 1f);
				m_camera.SetView((float)m_owner.XAxis.Span, (float)m_owner.YAxis.Span, 1.0f);
				m_camera.SetPosition(centre + v4.ZAxis * m_camera.FocusDist, centre, v4.YAxis);

				// Start the render
				m_window.BackgroundColour = m_owner.Options.ChartBkColour;
				m_window.Render();
				m_window.Present();
			}

			/// <summary>Returns a point in chart space from a point in ChartPanel-client space.</summary>
			private PointF ClientToChart(Point point)
			{
				return m_owner.ClientToChart(Control_.MapPoint(this, m_owner, point));
			}

			/// <summary>Returns a point in ChartPanel-client space from a point in chart space. Inverse of ClientToChart</summary>
			private Point ChartToClient(PointF point)
			{
				return Control_.MapPoint(m_owner, this, m_owner.ChartToClient(point));
			}
		}
		#endregion

		#region Chart Dims

		/// <summary>The calculated areas of the control</summary>
		public struct ChartDims
		{
			public ChartDims(ChartControl chart)
			{
				Chart = chart;
				using (var gfx = chart.CreateGraphics())
				{
					RectangleF rect = chart.ClientRectangle;
					var r = SizeF.Empty;

					Area = rect.ToRect();

					// Add margins
					rect.X      += chart.Options.Margin.Left;
					rect.Y      += chart.Options.Margin.Top;
					rect.Width  -= chart.Options.Margin.Left + chart.Options.Margin.Right;
					rect.Height -= chart.Options.Margin.Top + chart.Options.Margin.Bottom;

					// Add space for tick marks
					if (chart.YAxis.Options.DrawTickMarks)
					{
						rect.X      += chart.YAxis.Options.TickLength;
						rect.Width  -= chart.YAxis.Options.TickLength;
					}
					if (chart.XAxis.Options.DrawTickMarks)
					{
						rect.Height -= chart.XAxis.Options.TickLength;
					}

					// Add space for the title and axis labels
					if (chart.Title.HasValue())
					{
						r = gfx.MeasureString(chart.Title, chart.Options.TitleFont);
						rect.Y      += r.Height;
						rect.Height -= r.Height;
					}
					if (chart.XAxis.Label.HasValue())
					{
						r = gfx.MeasureString(chart.XAxis.Label, chart.XAxis.Options.LabelFont);
						rect.Height -= r.Height;
					}
					if (chart.YAxis.Label.HasValue())
					{
						r = gfx.MeasureString(chart.Range.YAxis.Label, chart.YAxis.Options.LabelFont);
						rect.X     += r.Height; // will be rotated by 90deg
						rect.Width -= r.Height;
					}

					// Add space for the tick labels
					// Note: If you're having trouble with the axis jumping around
					// check the 'TickText' callback is returning fixed length strings
					if (chart.XAxis.Options.DrawTickLabels)
					{
						// Measure the height of the tick text
						var h = chart.XAxis.Options.MinTickSize;
						for (double x = chart.XAxis.Min, step = chart.XAxis.Span/20; x < chart.XAxis.Max; x += step)
						{
							var s = chart.XAxis.TickText(x, step);
							h = Math.Max(h, gfx.MeasureString(s, chart.XAxis.Options.TickFont).Height);
						}
						rect.Height -= h;
					}
					if (chart.YAxis.Options.DrawTickLabels)
					{
						// Measure the width of the tick text
						var w = chart.XAxis.Options.MinTickSize;
						for (double y = chart.YAxis.Min, step = chart.YAxis.Span/20; y < chart.YAxis.Max; y += step)
						{
							var s = chart.YAxis.TickText(y, step);
							w = Math.Max(w, gfx.MeasureString(s, chart.YAxis.Options.TickFont).Width);
						}
						rect.X     += w;
						rect.Width -= w;
					}

					ChartArea = rect.ToRect();
				}
			}

			/// <summary>The chart that these dimensions were calculated from</summary>
			public ChartControl Chart { get; private set; }

			/// <summary>The size of the control</summary>
			public Rectangle Area { get; private set; }

			/// <summary>The area of the view3d part of the chart</summary>
			public Rectangle ChartArea { get; private set; }
		}

		/// <summary>The client space area of the view3d part of the chart</summary>
		private Rectangle ChartBounds
		{
			get { return Chart.Bounds; }
		}

		/// <summary>The client space area of the XAxis area of the chart</summary>
		private Rectangle XAxisBounds
		{
			get { return Rectangle.FromLTRB(ChartBounds.Left, ChartBounds.Bottom, ChartBounds.Right, ClientSize.Height); }
		}

		/// <summary>The client space area of the YAxis area of the chart</summary>
		private Rectangle YAxisBounds
		{
			get { return Rectangle.FromLTRB(0, ChartBounds.Top, ChartBounds.Left, ChartBounds.Bottom); }
		}

		#endregion

		#region RdrOptions
		[TypeConverter(typeof(TyConv))]
		public class RdrOptions :INotifyPropertyChanged
		{
			private class TyConv :GenericTypeConverter<RdrOptions> {}

			public RdrOptions()
			{
				BkColour        = SystemColors.Control;
				ChartBkColour   = Color.White;
				TitleColour     = Color.Black;
				TitleFont       = new Font("tahoma", 12, FontStyle.Bold);
				TitleTransform  = new Matrix(1f, 0f, 0f, 1f, 0f, 0f);
				Margin          = new Padding(3);
				NoteFont        = new Font("tahoma", 8, FontStyle.Regular);
				SelectionColour = Color.DarkGray;
				ShowGridLines   = true;
				XAxis           = new Axis();
				YAxis           = new Axis();
			}
			public RdrOptions(RdrOptions rhs)
			{
				BkColour        = rhs.BkColour;
				ChartBkColour   = rhs.ChartBkColour;
				TitleColour     = rhs.TitleColour;
				TitleFont       = (Font)rhs.TitleFont.Clone();
				TitleTransform  = rhs.TitleTransform;
				Margin          = rhs.Margin;
				NoteFont        = (Font)rhs.NoteFont.Clone();
				SelectionColour = rhs.SelectionColour;
				ShowGridLines   = rhs.ShowGridLines;
				XAxis           = new Axis(rhs.XAxis);
				YAxis           = new Axis(rhs.YAxis);
			}
			public RdrOptions(XElement node) :this()
			{
				BkColour        = node.Element(XmlTag.BkColour       ).As(BkColour       );
				ChartBkColour   = node.Element(XmlTag.ChartBkColour  ).As(ChartBkColour  );
				TitleColour     = node.Element(XmlTag.TitleColour    ).As(TitleColour    );
				TitleTransform  = node.Element(XmlTag.TitleTransform ).As(TitleTransform );
				Margin          = node.Element(XmlTag.Margin         ).As(Margin         );
				TitleFont       = node.Element(XmlTag.TitleFont      ).As(TitleFont      );
				NoteFont        = node.Element(XmlTag.NoteFont       ).As(NoteFont       );
				SelectionColour = node.Element(XmlTag.SelectionColour).As(SelectionColour);
				ShowGridLines   = node.Element(XmlTag.ShowGridLines  ).As(ShowGridLines  );
				XAxis           = node.Element(XmlTag.XAxis          ).As(XAxis          );
				YAxis           = node.Element(XmlTag.YAxis          ).As(YAxis          );
			}
			public XElement ToXml(XElement node)
			{
				node.Add2(XmlTag.BkColour       , BkColour       , false);
				node.Add2(XmlTag.ChartBkColour  , ChartBkColour  , false);
				node.Add2(XmlTag.TitleColour    , TitleColour    , false);
				node.Add2(XmlTag.TitleTransform , TitleTransform , false);
				node.Add2(XmlTag.Margin         , Margin         , false);
				node.Add2(XmlTag.TitleFont      , TitleFont      , false);
				node.Add2(XmlTag.NoteFont       , NoteFont       , false);
				node.Add2(XmlTag.SelectionColour, SelectionColour, false);
				node.Add2(XmlTag.ShowGridLines  , ShowGridLines  , false);
				node.Add2(XmlTag.XAxis          , XAxis          , false);
				node.Add2(XmlTag.YAxis          , YAxis          , false);
				return node;
			}

			/// <summary>Property changed</summary>
			public event PropertyChangedEventHandler PropertyChanged;
			private void SetProp<T>(ref T prop, T value, string name)
			{
				if (Equals(prop, value)) return;
				prop = value;
				PropertyChanged.Raise(this, new PropertyChangedEventArgs(name));
			}

			/// <summary>The fill colour of the background of the chart</summary>
			public Color BkColour
			{
				get { return m_BkColour; }
				set { SetProp(ref m_BkColour, value, nameof(BkColour)); }
			}
			private Color m_BkColour;

			/// <summary>The fill colour of the chart background</summary>
			public Color ChartBkColour
			{
				get { return m_ChartBkColour; }
				set { SetProp(ref m_ChartBkColour, value, nameof(ChartBkColour)); }
			}
			private Color m_ChartBkColour;

			/// <summary>The colour of the title text</summary>
			public Color TitleColour
			{
				get { return m_TitleColour; }
				set { SetProp(ref m_TitleColour, value, nameof(TitleColour)); }
			}
			private Color m_TitleColour;

			/// <summary>Transform for position the chart title, offset from top centre</summary>
			public Matrix TitleTransform
			{
				get { return m_TitleTransform; }
				set { SetProp(ref m_TitleTransform, value, nameof(TitleTransform)); }
			}
			private Matrix m_TitleTransform;

			/// <summary>The distances from the edge of the control to the chart area</summary>
			public Padding Margin
			{
				get { return m_Margin; }
				set { SetProp(ref m_Margin, value, nameof(Margin)); }
			}
			private Padding m_Margin;

			/// <summary>Font to use for the title text</summary>
			public Font TitleFont
			{
				get { return m_TitleFont; }
				set { SetProp(ref m_TitleFont, value, nameof(TitleFont)); }
			}
			private Font m_TitleFont;

			/// <summary>Font to use for chart notes</summary>
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

			/// <summary>Show grid lines (False overrides per-axis options)</summary>
			public bool ShowGridLines
			{
				get { return m_ShowGridLines; }
				set { SetProp(ref m_ShowGridLines, value, nameof(ShowGridLines)); }
			}
			private bool m_ShowGridLines;

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
				PropertyChanged.Raise(this, new PropertyChangedEventArgs(nameof(XAxis)));
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
				PropertyChanged.Raise(this, new PropertyChangedEventArgs(nameof(YAxis)));
			}

			[TypeConverter(typeof(TyConv))]
			public class Axis :INotifyPropertyChanged
			{
				private class TyConv :GenericTypeConverter<Axis> {}

				public Axis()
				{
					AxisColour     = Color.Black;
					LabelColour    = Color.Black;
					GridColour     = Color.Gray;
					TickColour     = Color.Black;
					LabelFont      = new Font("tahoma", 10, FontStyle.Regular);
					TickFont       = new Font("tahoma", 8, FontStyle.Regular);
					DrawTickMarks  = true;
					DrawTickLabels = true;
					TickLength     = 5;
					MinTickSize    = 30;
					LabelTransform = new Matrix(1f, 0f, 0f, 1f, 0f, 0f);
					AxisThickness  = 1f;
					PixelsPerTick  = 30.0;
					ShowGridLines  = true;
				}
				public Axis(Axis rhs)
				{
					AxisColour     = rhs.AxisColour;
					LabelColour    = rhs.LabelColour;
					GridColour     = rhs.GridColour;
					TickColour     = rhs.TickColour;
					LabelFont      = (Font)rhs.LabelFont.Clone();
					TickFont       = (Font)rhs.TickFont.Clone();
					DrawTickMarks  = rhs.DrawTickMarks;
					DrawTickLabels = rhs.DrawTickLabels;
					TickLength     = rhs.TickLength;
					MinTickSize    = rhs.MinTickSize;
					LabelTransform = rhs.LabelTransform;
					AxisThickness  = rhs.AxisThickness;
					PixelsPerTick  = rhs.PixelsPerTick;
					ShowGridLines  = rhs.ShowGridLines;
				}
				public Axis(XElement node) :this()
				{
					AxisColour     = node.Element(XmlTag.AxisColour    ).As(AxisColour    );
					LabelColour    = node.Element(XmlTag.LabelColour   ).As(LabelColour   );
					GridColour     = node.Element(XmlTag.GridColour    ).As(GridColour    );
					TickColour     = node.Element(XmlTag.TickColour    ).As(TickColour    );
					LabelFont      = node.Element(XmlTag.LabelFont     ).As(LabelFont     );
					TickFont       = node.Element(XmlTag.TickFont      ).As(TickFont      );
					DrawTickMarks  = node.Element(XmlTag.DrawTickMarks ).As(DrawTickMarks );
					DrawTickLabels = node.Element(XmlTag.DrawTickLabels).As(DrawTickLabels);
					TickLength     = node.Element(XmlTag.TickLength    ).As(TickLength    );
					MinTickSize    = node.Element(XmlTag.MinTickSize   ).As(MinTickSize   );
					LabelTransform = node.Element(XmlTag.LabelTransform).As(LabelTransform);
					AxisThickness  = node.Element(XmlTag.AxisThickness ).As(AxisThickness );
					PixelsPerTick  = node.Element(XmlTag.PixelsPerTick ).As(PixelsPerTick );
					ShowGridLines  = node.Element(XmlTag.ShowGridLines ).As(ShowGridLines );
				}
				public XElement ToXml(XElement node)
				{
					node.Add2(XmlTag.AxisColour    , AxisColour    , false);
					node.Add2(XmlTag.LabelColour   , LabelColour   , false);
					node.Add2(XmlTag.GridColour    , GridColour    , false);
					node.Add2(XmlTag.TickColour    , TickColour    , false);
					node.Add2(XmlTag.LabelFont     , LabelFont     , false);
					node.Add2(XmlTag.TickFont      , TickFont      , false);
					node.Add2(XmlTag.DrawTickMarks , DrawTickMarks , false);
					node.Add2(XmlTag.DrawTickLabels, DrawTickLabels, false);
					node.Add2(XmlTag.TickLength    , TickLength    , false);
					node.Add2(XmlTag.MinTickSize   , MinTickSize   , false);
					node.Add2(XmlTag.LabelTransform, LabelTransform, false);
					node.Add2(XmlTag.AxisThickness , AxisThickness , false);
					node.Add2(XmlTag.PixelsPerTick , PixelsPerTick , false);
					node.Add2(XmlTag.ShowGridLines , ShowGridLines , false);
					return node;
				}

				/// <summary>Property changed</summary>
				public event PropertyChangedEventHandler PropertyChanged;
				private void SetProp<T>(ref T prop, T value, string name)
				{
					if (Equals(prop, value)) return;
					prop = value;
					PropertyChanged.Raise(this, new PropertyChangedEventArgs(name));
				}

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

				/// <summary>The colour of the grid lines</summary>
				public Color GridColour
				{
					get { return m_GridColour; }
					set { SetProp(ref m_GridColour, value, nameof(GridColour)); }
				}
				private Color m_GridColour;

				/// <summary>The colour of the tick text</summary>
				public Color TickColour
				{
					get { return m_TickColour; }
					set { SetProp(ref m_TickColour, value, nameof(TickColour)); }
				}
				private Color m_TickColour;

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

				/// <summary>Offset transform from default label position</summary>
				public Matrix LabelTransform
				{
					get { return m_LabelTransform; }
					set { SetProp(ref m_LabelTransform, value, nameof(LabelTransform)); }
				}
				private Matrix m_LabelTransform;

				/// <summary>The thickness of the axis line</summary>
				public float AxisThickness
				{
					get { return m_AxisThickness; }
					set { SetProp(ref m_AxisThickness, value, nameof(AxisThickness)); }
				}
				private float m_AxisThickness;

				/// <summary>The preferred number of pixels between each grid line</summary>
				public double PixelsPerTick
				{
					get { return m_PixelsPerTick; }
					set { SetProp(ref m_PixelsPerTick, value, nameof(PixelsPerTick)); }
				}
				private double m_PixelsPerTick;

				/// <summary>Show grid lines for this axis. (This settings is overruled by the main chart options)</summary>
				public bool ShowGridLines
				{
					get { return m_ShowGridLines; }
					set { SetProp(ref m_ShowGridLines, value, nameof(ShowGridLines)); }
				}
				private bool m_ShowGridLines;
			}
		}

		/// <summary>A UI for setting these rendering properties</summary>
		public class RdrOptionsUI :ToolForm
		{
			private readonly ChartControl m_chart;
			private readonly RdrOptions m_opts;

			public RdrOptionsUI(ChartControl chart, RdrOptions opts)
				:base(chart, EPin.Centre, Point.Empty, new Size(500,400), true)
			{
				m_chart  = chart;
				m_opts   = opts;
				ShowIcon = (chart.TopLevelControl as Form)?.ShowIcon ?? false;
				Icon     = (chart.TopLevelControl as Form)?.Icon;
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
					m_chart.Invalidate();
					m_chart.XAxis.GridLineGfx = null;
					m_chart.YAxis.GridLineGfx = null;
				};
			}
		}
		#endregion

		#region Navigation

		[Flags] private enum EAxis
		{
			XAxis = 1 << 0,
			YAxis = 1 << 1,
			Both  = XAxis | YAxis,
		}

		/// <summary>Enable/Disable mouse navigation</summary>
		[Browsable(false)]
		public bool MouseNavigation
		{
			get { return m_mouse_nav; }
			set
			{
				if (m_mouse_nav == value) return;
				if (m_mouse_nav)
				{
					MouseDown  -= OnMouseDown;
					MouseUp    -= OnMouseUp;
					MouseWheel -= OnMouseWheel;
				}
				m_mouse_nav = value;
				if (m_mouse_nav)
				{
					MouseDown  += OnMouseDown;
					MouseUp    += OnMouseUp;
					MouseWheel += OnMouseWheel;
				}
			}
		}
		private bool m_mouse_nav;

		/// <summary>The location in chart space of where the chart was "grabbed"</summary>
		private PointF m_grab_location;

		/// <summary>The allowed motion based on where the chart was grabbed</summary>
		private EAxis m_drag_axis_allow;

		/// <summary>Area selection, has width, height of zero when the user isn't selecting</summary>
		private Rectangle m_selection;

		// Mouse navigation - public to allow users to forward mouse calls to us.
		public void OnMouseDown(object sender, MouseEventArgs e)
		{
			m_selection = new Rectangle(e.Location, Size.Empty);
			switch (e.Button)
			{
			case MouseButtons.Left:
				m_grab_location = ClientToChart(e.Location);
				if (ChartBounds.Contains(e.Location)) m_drag_axis_allow = EAxis.Both;
				if (XAxisBounds.Contains(e.Location)) m_drag_axis_allow = EAxis.XAxis;
				if (YAxisBounds.Contains(e.Location)) m_drag_axis_allow = EAxis.YAxis;
				Cursor = Cursors.SizeAll;
				MouseMove -= OnMouseChartDrag;
				MouseMove += OnMouseChartDrag;
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
				MouseMove -= OnMouseChartDrag;
				Capture = false;
				break;

			case MouseButtons.Right:
				MouseMove -= OnMouseAreaSelect;

				// If the selection has area, rescale the chart
				if (Math.Abs(m_selection.Width) > 0 && Math.Abs(m_selection.Height) > 0)
				{
					// Normalise the rectangle
					var sel = m_selection.NormalizeRect();

					// Rescale the chart
					var lower = ClientToChart(new Point(sel.Left, sel.Bottom));
					var upper = ClientToChart(new Point(sel.Right, sel.Top));
					XAxis.Min = lower.X;
					XAxis.Max = upper.X;
					YAxis.Min = lower.Y;
					YAxis.Max = upper.Y;
					Invalidate();

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
			var delta = Maths.Clamp(e.Delta, -999, 999);
			var chart_bounds = Chart.Bounds;
			if (chart_bounds.Contains(e.Location))
			{
				var point = ClientToChart(e.Location);
				Zoom *= (1.0f - delta * 0.001f);
				PositionChart(e.Location, point);
				Invalidate();
				return;
			}
			var xaxis_bounds = Rectangle.FromLTRB(chart_bounds.Left, chart_bounds.Bottom, chart_bounds.Right, ClientSize.Height);
			if (xaxis_bounds.Contains(e.Location))
			{
				if (ModifierKeys == Keys.Control && XAxis.AllowScroll)
				{
					XAxis.Shift(XAxis.Span * delta * 0.001);
					Invalidate();
				}
				if (ModifierKeys != Keys.Control && XAxis.AllowZoom)
				{
					XAxis.Span *= (1.0f - delta * 0.001);
					Invalidate();
				}
			}
			var yaxis_bounds = Rectangle.FromLTRB(0, chart_bounds.Top, chart_bounds.Left, chart_bounds.Bottom);
			if (yaxis_bounds.Contains(e.Location))
			{
				if (ModifierKeys == Keys.Control && YAxis.AllowScroll)
				{
					YAxis.Shift(YAxis.Span * delta * 0.001);
					Invalidate();
				}
				if (ModifierKeys != Keys.Control && YAxis.AllowZoom)
				{
					YAxis.Span *= (1.0f - delta * 0.001);
					Invalidate();
				}
			}
		}

		/// <summary>Handle mouse dragging the chart around</summary>
		private void OnMouseChartDrag(object sender, MouseEventArgs e)
		{
			var grab_loc = ChartToClient(m_grab_location);
			var drop_loc = e.Location;

			// Limit the drag direction
			if (!m_drag_axis_allow.HasFlag(EAxis.XAxis)) drop_loc.X = grab_loc.X;
			if (!m_drag_axis_allow.HasFlag(EAxis.YAxis)) drop_loc.Y = grab_loc.Y;

			// must drag at least 5 pixels
			var dx = drop_loc.X - grab_loc.X;
			var dy = drop_loc.Y - grab_loc.Y;
			if (dx*dx + dy*dy >= 25f)
				PositionChart(drop_loc, m_grab_location);
		}

		/// <summary>Handle mouse dragging to resize the area selection</summary>
		private void OnMouseAreaSelect(object sender, MouseEventArgs e)
		{
			const int MinAreaSelectDistance = 3;
			m_selection.Width  = e.Location.X - m_selection.X;
			m_selection.Height = e.Location.Y - m_selection.Y;
			if (Math.Abs(m_selection.Width)  < MinAreaSelectDistance) m_selection.Width  = 0;
			if (Math.Abs(m_selection.Height) < MinAreaSelectDistance) m_selection.Height = 0;
			Invalidate();
		}

		/// <summary>Shifts the X and Y range of the chart so that chart space position 'gs_point' is at client space position 'cs_point'</summary>
		public void PositionChart(Point cs_point, PointF gs_point)
		{
			var dst = ClientToChart(cs_point);
			XAxis.Shift(gs_point.X - dst.X);
			YAxis.Shift(gs_point.Y - dst.Y);
			Invalidate();
		}

		/// <summary>Get/Set the centre of the chart</summary>
		[Browsable(false)]
		public PointF Centre
		{
			get { return new PointF((float)(XAxis.Min + XAxis.Span*0.5), (float)(YAxis.Min + YAxis.Span*0.5)); }
			set
			{
				XAxis.Min = value.X - XAxis.Span*0.5;
				YAxis.Min = value.Y - YAxis.Span*0.5;
				Invalidate();
			}
		}

		/// <summary>Zoom in/out on the chart. Remember to call refresh. Zoom is a floating point value where 1f = no zoom, 2f = 2x magnification</summary>
		[Browsable(false)]
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
				value = Maths.Clamp(value, ZoomMin, ZoomMax);

				// If both axes allow zoom, maintain the aspect ratio
				if (XAxis.AllowZoom && YAxis.AllowZoom)
				{
					var aspect = (YAxis.Span * BaseRangeX.Size) / (BaseRangeY.Size * XAxis.Span);
					aspect = Maths.Clamp(Maths.IsFinite(aspect) ? aspect : 1.0, 0.001, 1000);
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
				Invalidate();
			}
		}
		private RangeF m_impl_zoom;

		/// <summary>Minimum zoom limit</summary>
		public double ZoomMin
		{
			get { return m_impl_zoom.Begin; }
			set { Debug.Assert(value > 0f); m_impl_zoom.Begin = value; }
		}

		/// <summary>Maximum zoom limit</summary>
		public double ZoomMax
		{
			get { return m_impl_zoom.End; }
			set { Debug.Assert(value > 0f); m_impl_zoom.End = value; }
		}

		#endregion

		#region Range

		/// <summary>The 2D size of the chart</summary>
		public class RangeData :IDisposable
		{
			public RangeData(ChartControl owner)
			{
				XAxis = new Axis(owner);
				YAxis = new Axis(owner);
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
				get { return m_xaxis; }
				internal set
				{
					if (m_xaxis == value) return;
					if (m_xaxis != null)
					{
						m_xaxis.Zoomed -= HandleAxisZoomed;
						Util.Dispose(ref m_xaxis);
					}
					m_xaxis = value;
					if (m_xaxis != null)
					{
						m_xaxis.Zoomed += HandleAxisZoomed;
					}
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
						m_yaxis.Zoomed -= HandleAxisZoomed;
						Util.Dispose(ref m_yaxis);
					}
					m_yaxis = value;
					if (m_yaxis != null)
					{
						m_yaxis.Zoomed += HandleAxisZoomed;
					}
				}
			}
			private Axis m_yaxis;

			/// <summary>Add the graphics associated with the axes to the scene</summary>
			internal void AddToScene(View3d.Window window)
			{
				// Position the grid lines for each axis
				if (XAxis.Owner.Options.ShowGridLines && XAxis.Options.ShowGridLines)
				{
					double min, max, step;
					XAxis.GridLines(out min, out max, out step);
					var o2w = m4x4.Translation(new v4((float)(XAxis.Min + min), (float)YAxis.Min, GridLinesZ, 1f));

					// Position the grid lines so that they line up with the axis tick marks
					XAxis.GridLineGfx.SetO2W(o2w);
					window.AddObject(XAxis.GridLineGfx);
				}
				if (YAxis.Owner.Options.ShowGridLines && YAxis.Options.ShowGridLines)
				{
					double min, max, step;
					YAxis.GridLines(out min, out max, out step);
					var o2w = m4x4.Translation(new v4((float)XAxis.Min, (float)(YAxis.Min + min), GridLinesZ, 1f));

					YAxis.GridLineGfx.SetO2W(o2w);
					window.AddObject(YAxis.GridLineGfx);
				}
			}

			/// <summary>Invalidate the cached grid line graphics on zoom (for both axes), since the model will need to change size</summary>
			private void HandleAxisZoomed(object sender, EventArgs e)
			{
				XAxis.GridLineGfx = null;
				YAxis.GridLineGfx = null;
			}

			/// <summary>A axis on the chart (typically X or Y)</summary>
			public class Axis :IDisposable
			{
				public Axis(ChartControl owner)
					: this(owner, 0f, 1f)
				{ }
				public Axis(ChartControl owner, double min, double max)
				{
					Debug.Assert(owner != null);
					Set(min, max);
					Owner         = owner;
					Label         = string.Empty;
					AllowScroll   = true;
					AllowZoom     = true;
					LockRange     = false;
					TickText      = (x,step) => Math.Round(x, 4, MidpointRounding.AwayFromZero).ToString("G4");
				}
				public Axis(Axis rhs)
				{
					Set(rhs.Min, rhs.Max);
					Owner         = rhs.Owner;
					Label         = rhs.Label;
					AllowScroll   = rhs.AllowScroll;
					AllowZoom     = rhs.AllowZoom;
					LockRange     = rhs.LockRange;
					TickText      = rhs.TickText;
				}
				public virtual void Dispose()
				{
					GridLineGfx = null;
				}

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

				/// <summary>The chart that owns this axis</summary>
				public ChartControl Owner { get; private set; }

				/// <summary>The axis label</summary>
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
						Set(value.Begin, value.End);
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
					Debug.Assert(min < max, "Range must be positive and non-zero");
					var zoomed = !Maths.FEql(max - min, m_max - m_min);
					var scroll = !Maths.FEql((max + min)*0.5, (m_max + m_min)*0.5);

					m_min = min;
					m_max = max;

					if (zoomed) OnZoomed();
					if (scroll) OnScroll();
				}
				public void Set(RangeF range)
				{
					Set(range.Begin, range.End);
				}

				/// <summary>Raised whenever the range scales</summary>
				public event EventHandler Zoomed;
				protected virtual void OnZoomed()
				{
					Zoomed.Raise(this);
				}

				/// <summary>Raised whenever the range shifts</summary>
				public event EventHandler Scroll;
				protected virtual void OnScroll()
				{
					Scroll.Raise(this);
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
					var dims = Owner.ChartDimensions;
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
				public IEnumerable<double> EnumGridLines
				{
					get
					{
						double min, max, step;
						GridLines(out min, out max, out step);
						for (var x = min; x < max; x += step)
							yield return x;
					}
				}

				/// <summary>The graphics object used for grid lines</summary>
				internal View3d.Object GridLineGfx
				{
					get
					{
						Debug.Assert(Options.ShowGridLines);
						if (m_gridlines == null)
						{
							// Create a model for the grid lines
							double min, max, step;
							GridLines(out min, out max, out step);
							var num_lines = (int)(1 + (max - min) / step);

							// Create the grid lines at the origin, they get positioned as the camera moves
							var verts = new View3d.Vertex[num_lines * 2];
							var indices = new ushort[num_lines * 2];
							var nuggets = new View3d.Nugget[1];
							var name = string.Empty;
							var v = 0;
							var i = 0;

							// Grid verts
							if (this == Owner.XAxis)
							{
								name = "xaxis_grid";
								var x = 0f; var y0 = 0f; var y1 = (float)Owner.YAxis.Span;
								for (int l = 0; l != num_lines; ++l)
								{
									verts[v++] = new View3d.Vertex(new v4(x, y0, 0f, 1f), Options.GridColour.ToArgbU());
									verts[v++] = new View3d.Vertex(new v4(x, y1, 0f, 1f), Options.GridColour.ToArgbU());
									x += (float)step;
								}
							}
							if (this == Owner.YAxis)
							{
								name = "yaxis_grid";
								var y = 0f; var x0 = 0f; var x1 = (float)Owner.XAxis.Span;
								for (int l = 0; l != num_lines; ++l)
								{
									verts[v++] = new View3d.Vertex(new v4(x0, y, 0f, 1f), Options.GridColour.ToArgbU());
									verts[v++] = new View3d.Vertex(new v4(x1, y, 0f, 1f), Options.GridColour.ToArgbU());
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
							nuggets[0] = new View3d.Nugget(View3d.EPrim.LineList, View3d.EGeom.Vert|View3d.EGeom.Colr);
							m_gridlines = new View3d.Object(name, 0xFFFFFFFF, verts.Length, indices.Length, nuggets.Length, verts, indices, nuggets, Guid.Empty);
						}
						return m_gridlines;
					}
					set
					{
						Debug.Assert(value == null);
						Util.Dispose(ref m_gridlines);
					}
				}
				private View3d.Object m_gridlines;

				/// <summary>Friendly string view</summary>
				public override string ToString()
				{
					return "{0} - [{1}:{2}]".Fmt(Label, Min, Max);
				}
			}
		}

		#endregion

		#region Elements

		/// <summary>Base class for anything on a chart</summary>
		public abstract class Element :IDisposable
		{
			protected Element(Guid id, m4x4 position)
			{
				Id                           = id;
				m_impl_chart                 = null;
				m_impl_position              = position;
				m_impl_bounds                = new BBox(position.pos, v4.Zero);
				m_impl_selected              = false;
				m_impl_visible               = true;
				m_impl_visible_to_find_range = true;
				m_impl_enabled               = true;
				m_invalidated                = true;
				UserData                     = new Dictionary<Guid, object>();
			}
			protected Element(XElement node)
				:this(Guid.Empty, m4x4.Identity)
			{
				// Note: Bounds, size, etc are not stored, the implementation
				// of the element provides those (typically in UpdateGfx)
				Id       = node.Element(XmlTag.Id).As(Id);
				Position = node.Element(XmlTag.Position).As(Position);
			}
			public void Dispose()
			{
				Dispose(true); // Mimic the WinForms Control disposing pattern
			}
			protected bool Disposed { get; private set; }
			protected virtual void Dispose(bool disposing)
			{
				Util.BreakIf(!Disposed && Util.IsGCFinalizerThread);
				Chart = null;
				Invalidated = null;
				PositionChanged = null;
				DataChanged = null;
				SelectedChanged = null;
				Disposed = true;
			}

			/// <summary>Non-null when the element has been added to a chart. Not virtual, override 'SetChartCore' instead</summary>
			public ChartControl Chart
			{
				get { return m_impl_chart; }
				set { SetChartInternal(value, true); }
			}
			private ChartControl m_impl_chart;

			/// <summary>Assign the chart for this element</summary>
			internal void SetChartInternal(ChartControl chart, bool update)
			{
				// An Element can be added to a chart by assigning to the Chart property
				// or by adding it to the Elements collection. It can be removed by setting
				// this property to null, by removing it from the Elements collection, or
				// by Disposing the element. Note: the chart does not own the elements, 
				// elements should only be disposed by the caller.
				if (m_impl_chart == chart) return;

				// Detach from the old chart
				if (m_impl_chart != null && update)
				{
					m_impl_chart.Elements.Remove(this);
				}

				// Assign to the new chart
				SetChartCore(chart);

				// Attach to the new chart
				if (m_impl_chart != null && update)
				{
					m_impl_chart.Elements.Add(this);
				}

				Debug.Assert(CheckConsistency());
				Invalidate();
			}

			/// <summary>Add or remove this element from 'chart'</summary>
			public virtual void SetChartCore(ChartControl chart)
			{
				// Note: don't suspend events on Chart.Elements.
				// User code maybe watching for ListChanging events.
				Debug.Assert(!Disposed);

				// Remove this element from any selected collection on the outgoing chart
				// This also clears the 'selected' state for the element
				Selected = false;

				// Set the new chart
				m_impl_chart = chart;
			}

			/// <summary>Unique id for this element</summary>
			public Guid Id { get; private set; }

			/// <summary>Export to XML</summary>
			public virtual XElement ToXml(XElement node)
			{
				node.Add2(XmlTag.Id, Id, false);
				node.Add2(XmlTag.Position, Position, false);
				return node;
			}

			/// <summary>Import from XML. Used to update the state of this element without having to delete/recreate it</summary>
			protected virtual void FromXml(XElement node)
			{
				Position = node.Element(XmlTag.Position).As(Position);
			}

			/// <summary>Replace the contents of this element with data from 'node'</summary>
			internal void Update(XElement node)
			{
				// Don't validate the TypeAttribute as users may have sub-classed the element
				using (SuspendEvents())
					FromXml(node);

				Invalidate();
			}

			/// <summary>RAII object for suspending events on this element</summary>
			public Scope SuspendEvents()
			{
				return Scope.Create(
					() =>
					{
						return new[]
						{
							Invalidated.Suspend(),
							DataChanged.Suspend(),
							SelectedChanged.Suspend(),
							PositionChanged.Suspend(),
							SizeChanged.Suspend(),
						};
					},
					arr =>
					{
						Util.DisposeAll(arr);
					});
			}

			/// <summary>Raised whenever the element needs to be redrawn</summary>
			public event EventHandler Invalidated;
			protected virtual void OnInvalidated()
			{
				m_invalidated = true;
				InvalidateChart();
				Invalidated.Raise(this);
			}

			/// <summary>Raised whenever data associated with the element changes</summary>
			public event EventHandler DataChanged;
			protected virtual void OnDataChanged()
			{
				// Raise data changed on this element, and propagate
				// the event to the containing chart as well.
				DataChanged.Raise(this);
				RaiseChartChanged();
			}

			/// <summary>Raised whenever the element is moved</summary>
			public event EventHandler PositionChanged;
			protected void OnPositionChanged()
			{
				PositionChanged.Raise(this);
				RaiseChartChanged();
			}

			/// <summary>Raised whenever the element changes size</summary>
			public event EventHandler SizeChanged;
			protected void OnSizeChanged()
			{
				SizeChanged.Raise(this, EventArgs.Empty);
				RaiseChartChanged();
			}

			/// <summary>Raised whenever the element is selected or deselected</summary>
			public event EventHandler SelectedChanged;
			protected virtual void OnSelectedChanged()
			{
				SelectedChanged.Raise(this);
			}

			/// <summary>Signal that the chart needs laying out</summary>
			protected virtual void RaiseChartChanged()
			{
				if (Chart == null) return;
				Chart.RaiseChartChanged(new ChartChangedEventArgs(EChangeType.Edited));
			}

			/// <summary>Call 'Invalidate' on the containing chart</summary>
			protected virtual void InvalidateChart()
			{
				if (Chart == null) return;
				Chart.Invalidate();
			}

			/// <summary>Indicate that the graphics for this element needs to be recreated or modified</summary>
			public void Invalidate(object sender = null, EventArgs args = null)
			{
				if (m_invalidated) return;
				OnInvalidated();
			}
			private bool m_invalidated;

			/// <summary>Get/Set the selected state</summary>
			public virtual bool Selected
			{
				get { return m_impl_selected; }
				set { SetSelectedInternal(value, true); }
			}
			private bool m_impl_selected;

			/// <summary>Set the selected state of this element</summary>
			internal void SetSelectedInternal(bool selected, bool update_selected_collection)
			{
				// This allows the chart to set the selected state without
				// adding/removing from the chart's 'Selected' collection.
				if (m_impl_selected == selected) return;
				if (m_impl_selected && update_selected_collection)
				{
					Chart?.Selected.Remove(this);
				}

				m_impl_selected = selected;

				if (m_impl_selected && update_selected_collection)
				{
					// Selection state is changed by assigning to this property or by
					// addition/removal from the chart's 'Selected' collection.
					Chart?.Selected.Add(this);
				}

				// Notify observers about the selection change
				OnSelectedChanged();
				Invalidate();
			}

			/// <summary>Get/Set whether this element is visible in the chart</summary>
			public bool Visible
			{
				get { return m_impl_visible; }
				set
				{
					if (m_impl_visible == value) return;
					m_impl_visible = value;

					// Make an element visible/invisible, doesn't invalidate it,
					// only the chart that is displaying it.
					InvalidateChart();
				}
			}
			private bool m_impl_visible;

			/// <summary>Get/Set whether this element is enabled</summary>
			public bool Enabled
			{
				get { return m_impl_enabled; }
				set
				{
					if (m_impl_enabled == value) return;
					m_impl_enabled = value;

					// Changing the enabled data of an element should result
					// in the graphics changing in some way that implies 'Enabled/Disabled'
					Invalidate();
				}
			}
			private bool m_impl_enabled;

			/// <summary>True if this element is included when finding the range of data in the chart</summary>
			public bool VisibleToFindRange
			{
				get { return m_impl_visible_to_find_range; }
				set
				{
					if (m_impl_visible_to_find_range == value) return;
					m_impl_visible_to_find_range = value;

					// Make an element visible/invisible, doesn't invalidate it,
					// only the chart that is displaying it.
					InvalidateChart();
				}
			}
			private bool m_impl_visible_to_find_range;

			/// <summary>Allow users to bind arbitrary data to the chart element</summary>
			public IDictionary<Guid, object> UserData
			{
				get;
				private set;
			}

			/// <summary>Send this element to the bottom of the Z-order</summary>
			public void SendToBack()
			{
				// Z order is determined by position in the Elements collection
				if (Chart == null)
					return;

				// Save the chart pointer because removing the element will remove
				// it from the chart causing 'Chart' to become null before Insert is called.
				var chart = Chart;
				using (chart.Elements.SuspendEvents())
				{
					chart.Elements.Remove(this);
					chart.Elements.Insert(0, this);
				}
				chart.UpdateElementZOrder();
				InvalidateChart();
			}

			/// <summary>Bring this element to top of the stack.</summary>
			public void BringToFront()
			{
				// Z order is determined by position in the Elements collection
				if (Chart == null)
					return;

				// Save the chart pointer because removing the element will remove
				// it from the Chart causing 'Chart' to become null before Insert is called
				var chart = Chart;
				using (chart.Elements.SuspendEvents())
				{
					chart.Elements.Remove(this);
					chart.Elements.Add(this);
				}
				chart.UpdateElementZOrder();
				InvalidateChart();
			}

			/// <summary>The element to chart transform</summary>
			public m4x4 Position
			{
				get { return m_impl_position; }
				set
				{
					if (m4x4.FEql(m_impl_position, value)) return;
					SetPosition(value);
				}
			}
			private m4x4 m_impl_position;

			/// <summary>Internal set position and raise event</summary>
			protected virtual void SetPosition(m4x4 pos)
			{
				m_impl_position = pos;
				OnPositionChanged();
			}

			/// <summary>Get/Set the XY position of the element</summary>
			public v2 PositionXY
			{
				get { return Position.pos.xy; }
				set
				{
					var o2p = Position;
					Position = new m4x4(o2p.rot, new v4(value, o2p.pos.z, o2p.pos.w));
				}
			}

			/// <summary>Get/Set the z position of the element</summary>
			public float PositionZ
			{
				get { return Position.pos.z; }
				set
				{
					var o2p = Position;
					o2p.pos.z = value;
					Position = o2p;
				}
			}

			/// <summary>BBox for the element in chart space</summary>
			public virtual BBox Bounds
			{
				get { return m_impl_bounds; }
				protected set
				{
					if (m_impl_bounds == value) return;
					m_impl_bounds = value;
					Invalidate();
				}
			}
			private BBox m_impl_bounds;

			/// <summary>Get/Set the centre point of the element (in chart space)</summary>
			public v4 Centre
			{
				get { return Bounds.Centre; }
				set { Position = new m4x4(Position.rot, value + (Position.pos - Centre)); }
			}

			/// <summary>True if this element can be resized</summary>
			public virtual bool Resizeable
			{
				get { return false; }
			}

			/// <summary>Perform a hit test on this object. Returns null for no hit. 'point' is in chart space projected from the camera</summary>
			public virtual HitTestResult.Hit HitTest(v2 point, View3d.CameraControls cam)
			{
				return null;
			}

			/// <summary>Handle a click event on this element</summary>
			internal virtual bool HandleClicked(HitTestResult.Hit hit, View3d.CameraControls cam)
			{
				return false;
			}

			/// <summary>Drag the element 'delta' from the DragStartPosition</summary>
			public virtual void Drag(v2 delta, bool commit)
			{
				var p = DragStartPosition;
				p.pos.x += delta.x;
				p.pos.y += delta.y;
				Position = p;
				if (commit)
					DragStartPosition = Position;
			}

			/// <summary>Position recorded at the time dragging starts</summary>
			internal m4x4 DragStartPosition { get; set; }

			/// <summary>Update the graphics and object transforms associated with this element</summary>
			public void UpdateGfx(object sender = null, EventArgs args = null)
			{
				if (m_impl_updating_gfx != 0) return; // Protect against reentrancy
				using (Scope.Create(() => ++m_impl_updating_gfx, () => --m_impl_updating_gfx))
					UpdateGfxCore();
			}
			protected virtual void UpdateGfxCore() { }
			private int m_impl_updating_gfx;

			/// <summary>Add the graphics associated with this element to the window</summary>
			internal void AddToScene(View3d.Window window)
			{
				if (m_invalidated) UpdateGfx();
				AddToSceneCore(window);
			}
			protected virtual void AddToSceneCore(View3d.Window window)
			{
			}

			/// <summary>Check the self consistency of this element</summary>
			public virtual bool CheckConsistency()
			{
				return true;
			}
		}

		/// <summary>Handle elements added/removed from the elements list</summary>
		protected virtual void OnElementListChanging(ListChgEventArgs<Element> args)
		{
			var elem = args.Item;
			if (elem != null && (elem.Chart != null && elem.Chart != this))
				throw new ArgumentException("element belongs to another chart");

			switch (args.ChangeType)
			{
			case ListChg.Reset:
				{
					Elements.ForEach(x => x.SetChartInternal(this, false));
					ElemIds.Clear();
					Elements.ForEach(x => ElemIds.Add(x.Id, x));
					Debug.Assert(CheckConsistency());
					break;
				}
			case ListChg.ItemAdded:
				{
					elem.SetChartInternal(this, false);
					ElemIds.Add(elem.Id, elem);
					Debug.Assert(CheckConsistency());
					break;
				}
			case ListChg.ItemRemoved:
				{
					elem.SetChartInternal(null, false);
					ElemIds.Remove(elem.Id);
					Debug.Assert(CheckConsistency());
					break;
				}
			}
		}

		/// <summary>Handle elements added/removed from the selection list</summary>
		protected virtual void OnSelectionListChanging(ListChgEventArgs<Element> args)
		{
			var elem = args.Item;
			if (elem != null && (elem.Chart != null && elem.Chart != this))
				throw new ArgumentException("element belongs to another diagram");

			switch (args.ChangeType)
			{
			case ListChg.Reset:
				{
					Elements.ForEach(x => x.SetSelectedInternal(false, false));
					Selected.ForEach(x => x.SetSelectedInternal(true, false));
					Debug.Assert(CheckConsistency());
					break;
				}
			case ListChg.ItemAdded:
				{
					elem.SetSelectedInternal(true, false);
					Debug.Assert(CheckConsistency());
					break;
				}
			case ListChg.ItemRemoved:
				{
					elem.SetSelectedInternal(false, false);
					Debug.Assert(CheckConsistency());
					break;
				}
			}
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
					var tools_menu = cmenu.Items.Add2(new ToolStripMenuItem("Tools") { Name = CMenu.Tools });
					#region Show Value
					{
						var opt = tools_menu.DropDownItems.Add2(new ToolStripMenuItem("Show Value") { Name = CMenu.ToolsMenu.ShowValue });
						opt.Checked = (bool)m_tt_show_value.Tag;
						opt.Click += (s, a) =>
						{
							m_tt_show_value.Hide(this);
							if ((bool)m_tt_show_value.Tag) MouseMove -= OnMouseMoveTooltip;
							m_tt_show_value.Tag = !(bool)m_tt_show_value.Tag;
							if ((bool)m_tt_show_value.Tag) MouseMove += OnMouseMoveTooltip;
						};
					}
					#endregion
				}
				#endregion
				#region Zoom Menu
				{
					var zoom_menu = cmenu.Items.Add2(new ToolStripMenuItem("Zoom") { Name = CMenu.Zoom });
					{
						var opt = zoom_menu.DropDownItems.Add2(new ToolStripMenuItem("Default") { Name = CMenu.ZoomMenu.Default });
						opt.Click += (s, a) =>
						{
							FindDefaultRange(visible_only:false);
							ResetToDefaultRange();
							Invalidate();
						};
					}
				}
				#endregion
				#region Properties
				{
					var opt = cmenu.Items.Add2(new ToolStripMenuItem("Properties") { Name = CMenu.Properties });
					opt.Click += (s,a) =>
					{
						new RdrOptionsUI(this, Options).Show(this);
					};
				}
				#endregion

				// Allow users to add menu options
				OnAddUserMenuOptions(new AddUserMenuOptionsEventArgs(cmenu));
			}
			cmenu.Closed += (s, a) => Refresh();
			cmenu.Show(this, location);
		}

		/// <summary>Event allowing callers to add options to the context menu</summary>
		public event EventHandler<AddUserMenuOptionsEventArgs> AddUserMenuOptions;
		protected virtual void OnAddUserMenuOptions(AddUserMenuOptionsEventArgs args)
		{
			AddUserMenuOptions.Raise(this, args);
		}

		#endregion

		#region Show Value Tooltip

		/// <summary>A tool tip to display the mouse location value</summary>
		private ToolTip m_tt_show_value;

		/// <summary>Handle mouse move events while the tooltip is visible</summary>
		private void OnMouseMoveTooltip(object sender, MouseEventArgs e)
		{
			if (Chart.Bounds.Contains(e.Location))
			{
				var pt = ClientToChart(e.Location);
				var new_str = "{0} {1}".Fmt(XAxis.TickText(pt.X, 0.0), YAxis.TickText(pt.Y, 0.0));
				var old_str = m_tt_show_value.GetToolTip(this);
				if (old_str != new_str) // Fix a flickering problem for non-moving mouse
					m_tt_show_value.Show(new_str, this, e.Location + new Size(20, 0));
			}
			else
				m_tt_show_value.Hide(this);
		}

		#endregion

		#region Nested Classes

		/// <summary>Chart change types</summary>
		public enum EChangeType
		{
			/// <summary>
			/// Raised after elements in the chart have been moved, resized, or had their content changed.
			/// This event will be raised in addition to the more detailed modification events below</summary>
			Edited,

			/// <summary>
			/// Elements are about to be deleted from the chart by the user.
			/// Setting 'Cancel' for this event will abort the deletion.</summary>
			RemovingElements,
		}

		/// <summary>Results collection for a hit test</summary>
		public class HitTestResult
		{
			public class Hit
			{
				/// <summary>The element that was hit</summary>
				public Element Element { get; private set; }

				/// <summary>Where on the element it was hit (in element space)</summary>
				public PointF Point { get; private set; }

				/// <summary>The element's chart location at the time it was hit</summary>
				public m4x4 Location { get; private set; }

				public Hit(Element elem, PointF pt)
				{
					Element  = elem;
					Point    = pt;
					Location = elem.Position;
				}
				public override string ToString()
				{
					return Element.ToString();
				}
			}

			/// <summary>The collection of hit objects</summary>
			public List<Hit> Hits { get; private set; }

			/// <summary>The camera position when the hit test was performed (needed for chart to screen space conversion)</summary>
			public View3d.CameraControls Camera { get; private set; }

			public HitTestResult(View3d.CameraControls cam)
			{
				Hits = new List<Hit>();
				Camera = cam;
			}
		}

		/// <summary>Special menu item that doesn't draw highlighted</summary>
		private class NoHighlightToolStripMenuItem :ToolStripMenuItem
		{
			public NoHighlightToolStripMenuItem(string text) : base(text) { }
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

		/// <summary>String constants used in XML export/import</summary>
		public static class XmlTag
		{
			public const string Root            = "root";
			public const string TypeAttribute   = "ty";
			public const string Element         = "elem";
			public const string Id              = "id";
			public const string Position        = "pos";
			public const string XAxis           = "xaxis";
			public const string YAxis           = "yaxis";
			public const string BkColour        = "bk_colour";
			public const string ChartBkColour   = "chart_bk_colour";
			public const string TitleColour     = "title_colour";
			public const string TitleTransform  = "title_xform";
			public const string Margin          = "margin";
			public const string TitleFont       = "title_font";
			public const string NoteFont        = "note_font";
			public const string SelectionColour = "selection_colour";
			public const string ShowGridLines   = "show_grid_lines";
			public const string AxisColour      = "axis_colour";
			public const string LabelColour     = "label_colour";
			public const string GridColour      = "grid_colour";
			public const string TickColour      = "tick_colour";
			public const string LabelFont       = "label_font";
			public const string TickFont        = "tick_font";
			public const string DrawTickMarks   = "draw_tick_marks";
			public const string DrawTickLabels  = "draw_tick_labels";
			public const string TickLength      = "tick_length";
			public const string MinTickSize     = "min_tick_size";
			public const string LabelTransform  = "label_xform";
			public const string AxisThickness   = "axis_thickness";
			public const string PixelsPerTick   = "pixels_per_tick";
		}

		/// <summary>Names for context menu items to allow users to identify them</summary>
		public static class CMenu
		{
			public const string Tools = "tools";
			public static class ToolsMenu
			{
				public const string ShowValue = "show_value";
			}
			public const string Zoom = "zoom";
			public static class ZoomMenu
			{
				public const string Default = "default";
			}
			public const string Properties = "appearance";
			public static class AppearanceMenu
			{
				public const string BkColour = "bk_colour";
				public const string ChartBkColour = "chart_bk_colour";
			}
			public const string Axes = "axes";
			public static class AxesMenu
			{
				public const string XAxis = "x_axis";
				public static class XAxisMenu
				{
					public const string Min = "min";
					public const string Max = "max";
					public const string AllowScroll = "allow_scroll";
					public const string AllowZoom = "allow_zoom";
				}
				public const string YAxis = "y_axis";
				public static class YAxisMenu
				{
					public const string Min = "min";
					public const string Max = "max";
					public const string AllowScroll = "allow_scroll";
					public const string AllowZoom = "allow_zoom";
				}
			}
		}

		#endregion

		#region Event Args

		/// <summary>Event args for the chart changed event</summary>
		public class ChartChangedEventArgs :EventArgs
		{
			// Note: many events are available by attaching to the Elements binding list
			public ChartChangedEventArgs(EChangeType ty)
			{
				ChgType  = ty;
				Cancel   = false;
			}

			/// <summary>The type of change that occurred</summary>
			public EChangeType ChgType { get; private set; }

			/// <summary>A cancel property for "about to change" events</summary>
			public bool Cancel { get; set; }
		}

		/// <summary>Event args for the ChartRendering event</summary>
		public class ChartRenderingEventArgs :EventArgs
		{
			private View3d.Window m_window;
			public ChartRenderingEventArgs(View3d.Window window)
			{
				m_window = window;
			}

			/// <summary>Add a view3d object to the chart scene</summary>
			public void AddToScene(View3d.Object obj)
			{
				m_window.AddObject(obj);
			}
		}

		/// <summary>Event args for FindingDefaultRange</summary>
		public class FindingDefaultRangeEventArgs :EventArgs
		{
			public FindingDefaultRangeEventArgs(RangeF xrange, RangeF yrange)
			{
				XRange = xrange;
				YRange = yrange;
			}

			/// <summary>The XAxis range calculated from known chart elements data</summary>
			public RangeF XRange { get; set; }

			/// <summary>The YAxis range calculated from known chart elements data</summary>
			public RangeF YRange { get; set; }
		}

		/// <summary>Customise context menu event args</summary>
		public class AddUserMenuOptionsEventArgs :EventArgs
		{
			public AddUserMenuOptionsEventArgs(ContextMenuStrip menu)
			{
				Menu = menu;
			}

			/// <summary>The menu to add menu items to</summary>
			public ContextMenuStrip Menu { get; private set; }
		}

		/// <summary>Event args for the post-paint add overlays call</summary>
		public class AddOverlaysOnPaintEventArgs :EventArgs
		{
			public AddOverlaysOnPaintEventArgs(Graphics gfx, ChartDims dims, m4x4 chart_to_client)
			{
				Gfx           = gfx;
				Dims          = dims;
				ChartToClient = chart_to_client;
			}

			/// <summary>The device context to draw to</summary>
			public Graphics Gfx { get; private set; }

			/// <summary>Layout info about the chart</summary>
			public ChartDims Dims { get; private set; }

			/// <summary>The chart being drawn on</summary>
			public ChartControl Chart { get { return Dims.Chart; } }

			/// <summary>Transform from Chart space to client space</summary>
			public m4x4 ChartToClient { get; private set; }
		}

		#endregion

		#region Self Consistency

		/// <summary>Check the self consistency of elements</summary>
		public bool CheckConsistency()
		{
			if (ConsistencyCheckSuspended) return true;
			try { CheckConsistencyInternal(); }
			catch (Exception ex)
			{
				Debug.WriteLine(ex.MessageFull());
				return false;
			}
			return true;
		}

		/// <summary>Check consistency of the diagram elements</summary>
		protected virtual void CheckConsistencyInternal()
		{
			// Take a copy to prevent events being raised
			var elements = Elements.ToArray();

			// The elements collection should be distinct
			elements.Sort(ByGuid);
			for (int i = 0; i < elements.Length - 1; ++i)
			{
				if (!Equals(elements[i].Id, elements[i+1].Id)) continue;
				throw new Exception("Element {0} is in the Elements collection more than once".Fmt(elements[i].ToString()));
			}

			// All elements in the chart should have their Chart property set to this chart
			foreach (var elem in Elements)
			{
				if (elem.Chart == this) continue;
				throw new Exception("Element {0} is in the Elements collection but does not have its Chart property set correctly".Fmt(elem.ToString()));
			}

			// The selected collection contains all that is selected and no more
			var selected0 = elements.Where(x => x.Selected).ToArray().Sort(ByGuid);
			var selected1 = Selected.ToArray().Sort(ByGuid);
			if (!selected0.SequenceEqual(selected1, ByGuid))
				throw new Exception("Selected elements collection is inconsistent with the selected state of the elements");

			// The 'ElemIds' should match the Elements collection
			if (ElemIds.Count != Elements.Count)
				throw new Exception("Elements collection and Element Id lookup table don't match");
			foreach (var elem in Elements)
				if (ElemIds.GetOrDefault(elem.Id) != elem)
					throw new Exception("Element {0} is not in the Element Id lookup table".Fmt(elem.ToString()));

			// Check the consistency of all elements
			foreach (var elem in Elements)
				elem.CheckConsistency();
		}

		/// <summary>True while consistency checks are suspended (Set calls are reference counted)</summary>
		public bool ConsistencyCheckSuspended
		{
			get { return m_consistency_check_ref_count != 0; }
			set
			{
				m_consistency_check_ref_count += value ? +1 : -1;
				Debug.Assert(m_consistency_check_ref_count >= 0);
			}
		}
		private int m_consistency_check_ref_count;

		/// <summary>Temporarily disable the consistency check</summary>
		public Scope SuspendConsistencyCheck(bool check_on_exit = true)
		{
			return Scope.Create(
				() => ConsistencyCheckSuspended = true,
				() =>
				{
					ConsistencyCheckSuspended = false;
					if (check_on_exit && !ConsistencyCheckSuspended)
						Debug.Assert(CheckConsistency());
				});
		}

		/// <summary>Element sorting predicates</summary>
		protected Cmp<Element> ByGuid   = Cmp<Element>.From((l,r) => l.Id.CompareTo(r.Id));

		#endregion

		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		}
		#endregion
	}
}
