//***************************************************
// Chart Control
// Copyright (C) Rylogic Ltd 2016
//***************************************************
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.attrib;
using pr.common;
using pr.container;
using pr.extn;
using pr.gfx;
using pr.ldr;
using pr.maths;
using pr.util;
using pr.win32;
using Matrix = System.Drawing.Drawing2D.Matrix;

namespace pr.gui
{
	/// <summary>A view 3d based chart control</summary>
	public class ChartControl :UserControl
	{
		// Notes:
		// - Two methods of camera control; 1. directly position the camera, or 2. set the
		//   axis range, then determine the camera position from that. Both systems are used.
		//   for mouse control, the camera is positioned and the axis range updated based on
		//   the camera position. For ranging, or fixed chart positioning, the camera position
		//   is moved to match the axis range.
		// - After setting the axis range, call SetCameraFromRange()
		// - After setting the camera position, call SetRangeFromCamera()
		// - Mouse wheel moves the camera in the camera z direction
		// - Mouse wheel on an axis (changing the aspect ratio) changes the FoV

		/// <summary>Axis type</summary>
		[Flags] public enum EAxis
		{
			None = 0,
			XAxis = 1 << 0,
			YAxis = 1 << 1,
			Both = XAxis | YAxis,
		}

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
				Scene           = new ChartPanel(this); // Must come after 'Range'
				MouseOperations = new MouseOps();
				Tools           = new ChartTools(Options);
				m_tt_show_value = new HintBalloon { AutoSizeMode = AutoSizeMode.GrowOnly };

				Elements = new BindingListEx<Element> { PerItemClear = true, UseHashSet = true };
				Selected = new BindingListEx<Element> { PerItemClear = false };
				Hovered  = new BindingListEx<Element> { PerItemClear = false };
				ElemIds  = new Dictionary<Guid, Element>();

				Elements.ListChanging += (s,a) => OnElementListChanging(a);
				Selected.ListChanging += (s,a) => OnSelectionListChanging(a);
				Hovered .ListChanging += (s,a) => OnHoveredListChanging(a);

				AllowEditing = false;
				AllowSelection = false;
				DefaultMouseControl = true;
			}
			catch
			{
				Dispose();
				throw;
			}
		}
		protected override void Dispose(bool disposing)
		{
			MouseOperations = null;
			Tools = null;
			Range = null;
			Scene = null;
			Options = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnLayout(LayoutEventArgs e)
		{
			if (Scene != null && !DesignMode)
			{
				using (this.SuspendLayout(false))
				{
					var dims = ChartDimensions;
					Scene.Bounds = dims.ChartArea;
					if (Options.LockAspect != null && dims.ChartArea.Area() != 0)
						Aspect = Options.LockAspect.Value;
				}
			}
			base.OnLayout(e);
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			if (DesignMode) return;
			DoPaint(e.Graphics);
		}
		protected override void OnInvalidated(InvalidateEventArgs e)
		{
			Scene?.Invalidate();
			base.OnInvalidated(e);
		}

		/// <summary>Rendering options for the chart</summary>
		public RdrOptions Options
		{
			[DebuggerStepThrough] get { return m_rdr_options; }
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
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public RangeData Range
		{
			[DebuggerStepThrough] get { return m_range; }
			private set
			{
				if (value == m_range) return;
				Util.Dispose(ref m_range);
				m_range = value;
			}
		}
		private RangeData m_range;

		/// <summary>Accessor to the current X axis</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public RangeData.Axis XAxis
		{
			[DebuggerStepThrough] get { return Range.XAxis; }
		}

		/// <summary>Accessor to the current Y axis</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public RangeData.Axis YAxis
		{
			[DebuggerStepThrough] get { return Range.YAxis; }
		}

		/// <summary>Default X axis range of the chart</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public RangeF BaseRangeX
		{
			[DebuggerStepThrough] get;
			set;
		}

		/// <summary>Default Y axis range of the chart</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public RangeF BaseRangeY
		{
			[DebuggerStepThrough] get;
			set;
		}

		/// <summary>Get/Set the aspect ratio of the chart area</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public double Aspect
		{
			get { return Camera.Aspect * Scene.Bounds.Height / Scene.Bounds.Width; }
			set
			{
				if (Aspect == value) return;
				if (Options.LockAspect != null) Options.LockAspect = value;
				Camera.Aspect = (float)(value * Scene.Bounds.Width / Scene.Bounds.Height);
				SetRangeFromCamera();
				Invalidate();
			}
		}

		/// <summary>The view3d part of the chart</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ChartPanel Scene
		{
			[DebuggerStepThrough] get { return m_impl_scene; }
			private set
			{
				if (m_impl_scene == value) return;
				using (this.SuspendLayout(false))
				{
					if (m_impl_scene != null)
					{
						Controls.Remove(m_impl_scene);
						Util.Dispose(ref m_impl_scene);
					}
					m_impl_scene = value;
					if (m_impl_scene != null)
					{
						Controls.Add(m_impl_scene);
					}
				}
			}
		}
		private ChartPanel m_impl_scene;

		/// <summary>Renderer</summary>
		public View3d View3d
		{
			[DebuggerStepThrough] get { return Scene?.View3d; }
		}

		/// <summary>The view3d window for this control instance</summary>
		public View3d.Window Window
		{
			[DebuggerStepThrough] get { return Scene?.Window; }
		}

		/// <summary>The view of the chart</summary>
		public View3d.CameraControls Camera
		{
			[DebuggerStepThrough] get { return Scene?.Camera; }
		}

		/// <summary>All chart objects</summary>
		public BindingListEx<Element> Elements { get; private set; }

		/// <summary>The set of selected chart elements</summary>
		public BindingListEx<Element> Selected { get; private set; }

		/// <summary>The set of hovered chart elements</summary>
		public BindingListEx<Element> Hovered { get; private set; }

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
			return ClientToChart(new PointF(client_point.X, client_point.Y));
		}
		public PointF ClientToChart(PointF client_point)
		{
			return new PointF(
				(float)(XAxis.Min + (client_point.X - Scene.Bounds.Left  ) * XAxis.Span / Scene.Bounds.Width ),
				(float)(YAxis.Min - (client_point.Y - Scene.Bounds.Bottom) * YAxis.Span / Scene.Bounds.Height));
		}

		/// <summary>Returns a point in client space from a point in chart space. Inverse of ClientToChart</summary>
		public Point ChartToClient(PointF chart_point)
		{
			return new Point(
				(int)(Scene.Bounds.Left   + (chart_point.X - XAxis.Min) * Scene.Bounds.Width  / XAxis.Span),
				(int)(Scene.Bounds.Bottom - (chart_point.Y - YAxis.Min) * Scene.Bounds.Height / YAxis.Span));
		}

		/// <summary>Return a point in camera space from a point in chart space (Z = focus plane)</summary>
		public v4 ChartToCamera(PointF chart_point)
		{
			// Remove the camera to origin offset
			var origin_cs = Camera.W2O.pos.xy;
			var pt = new v2(chart_point) + origin_cs;
			return new v4(pt, -Camera.FocusDist, 1f);
		}

		/// <summary>Return a point in chart space from a point in camera space</summary>
		public PointF CameraToChart(v4 camera_point)
		{
			Debug.Assert(camera_point.z != 0);

			// Project the camera space point onto the focus plane
			var proj = -Camera.FocusDist / camera_point.z;
			var pt = new v2(camera_point.x * proj, camera_point.y * proj);

			// Add the offset of the camera from the origin
			var origin_cs = Camera.W2O.pos.xy;
			return pt - origin_cs;
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
			return ChartToClientSpace(Scene.Bounds);
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
			return ClientToChartSpace(Scene.Bounds);
		}

		/// <summary>Perform a hit test on the chart</summary>
		public HitTestResult HitTestCS(Point client_point, Keys modifier_keys, Func<Element, bool> pred)
		{
			// Determine the hit zone of the control
			var zone = HitTestResult.EZone.None;
			if (ChartBounds.Contains(client_point)) zone |= HitTestResult.EZone.Chart;
			if (XAxisBounds.Contains(client_point)) zone |= HitTestResult.EZone.XAxis;
			if (YAxisBounds.Contains(client_point)) zone |= HitTestResult.EZone.YAxis;
			if (TitleBounds.Contains(client_point)) zone |= HitTestResult.EZone.Title;

			// The hit test point in chart space
			var chart_point = ClientToChart(client_point);

			// Find elements that overlap 'client_point'
			var hits = new List<HitTestResult.Hit>();
			if (zone.HasFlag(HitTestResult.EZone.Chart))
			{
				var elements = pred != null ? Elements.Where(pred) : Elements;
				foreach (var elem in elements)
				{
					var hit = elem.HitTest(client_point, modifier_keys, Scene.Camera);
					if (hit != null)
						hits.Add(hit);
				}

				// Sort the results by z order
				hits.Sort((l,r) => -l.Element.PositionZ.CompareTo(r.Element.PositionZ));
			}

			return new HitTestResult(zone, client_point, chart_point, modifier_keys, hits, Scene.Camera);
		}

		///// <summary>Find the appropriate range for all data in the chart. Call ResetToDefaultRange() to zoom the chart to this range.</summary>
		//public void FindDefaultRange(EAxis axis = EAxis.Both, bool visible_only = true)
		//{
		//	// Notes:
		//	//  - Find the range on camera X/Y regardless of the aspect ratio. It's up to
		//	//   'ResetToDefaultRange()' to apply the aspect ratio. This means 'FindDefaultRange()'
		//	//    does not need to called after changing the aspect ratio.

		//	var do_x = axis.HasFlag(EAxis.XAxis);
		//	var do_y = axis.HasFlag(EAxis.YAxis);

		//	// Measure elements? or just use the scene bounding box?
		//	// Measure the range on each axis
		//	//var xrange = do_x ? FindRange(e => new RangeF(e.Bounds.MinX, e.Bounds.MaxX), Elements, visible_only) : RangeF.Zero;
		//	//var yrange = do_y ? FindRange(e => new RangeF(e.Bounds.MinY, e.Bounds.MaxY), Elements, visible_only) : RangeF.Zero;

		//	// Set the range based on the scene bounding box
		//	// Determine the X/Y range spanned by the bounding box as viewed from the camera
		//	var bbox = Window.SceneBounds(View3d.ESceneBounds.All);
		//	var bbox_cs = m4x4.InvertFast(Camera.O2W) * bbox;
		//	var xrange = do_x ? new RangeF(bbox_cs.MinX, bbox_cs.MaxX) : RangeF.Zero;
		//	var yrange = do_y ? new RangeF(bbox_cs.MinY, bbox_cs.MaxY) : RangeF.Zero;

		//	// Allow users to adjust the default range
		//	var args = new FindingDefaultRangeEventArgs(axis, xrange, yrange);
		//	OnFindingDefaultRange(args);
		//	xrange = args.XRange;
		//	yrange = args.YRange;

		//	// Scale up the ranges to leave a margin around the default range
		//	const float MarginScale = 1.05f;
		//	if (do_x && xrange.Size > 0) xrange = xrange.Scale(MarginScale); else xrange = new RangeF(0.0, 1.0);
		//	if (do_y && yrange.Size > 0) yrange = yrange.Scale(MarginScale); else yrange = new RangeF(0.0, 1.0);
		//	if (do_x) BaseRangeX = xrange;
		//	if (do_y) BaseRangeY = yrange;
		//}

		///// <summary>Find the appropriate range on a single axis</summary>
		//public static RangeF FindRange(Func<Element, RangeF> selector, IEnumerable<Element> elements, bool visible_only)
		//{
		//	var range = new RangeF(double.MaxValue, -double.MaxValue);
		//	foreach (var elem in elements)
		//	{
		//		if (!elem.VisibleToFindRange)
		//			continue;
		//		if (visible_only && !elem.Visible)
		//			continue;

		//		// Get the bounding box of the chart element
		//		var bnds = selector(elem);
		//		if (bnds.Begin < range.Begin) range.Begin = bnds.Begin;
		//		if (bnds.End   > range.End  ) range.End   = bnds.End;
		//	}

		//	return range;
		//}

		///// <summary>Raised when the default range is being found</summary>
		//public event EventHandler<FindingDefaultRangeEventArgs> FindingDefaultRange;
		//protected virtual void OnFindingDefaultRange(FindingDefaultRangeEventArgs args)
		//{
		//	FindingDefaultRange.Raise(this, args);
		//}

		///// <summary>Reset the axis ranges to the default. Call FindDefaultRange() to set the default range</summary>
		//public void ResetToDefaultRange(EAxis axis = EAxis.Both)
		//{
		//	if (!XAxis.LockRange && axis.HasFlag(EAxis.XAxis)) Range.XAxis.Set(BaseRangeX);
		//	if (!YAxis.LockRange && axis.HasFlag(EAxis.YAxis)) Range.YAxis.Set(BaseRangeY);
		//	if (Options.LockAspect != null) Aspect = Options.LockAspect.Value;
		//	SetCameraFromRange();
		//	Invalidate();
		//}

		/// <summary>Find the default range, then reset to the default range</summary>
		public void AutoRange(View3d.ESceneBounds who = View3d.ESceneBounds.All)
		{
			var bbox = Window.SceneBounds(who, except:new[] { ChartTools.Id });
			Camera.ResetView(bbox, Options.ResetForward, Options.ResetUp,
				dist: 0f,//Options.NavigationMode == ENavMode.Chart2D ? Camera.FocusDist : 0,
				preserve_aspect: LockAspect,
				commit: true);

			SetRangeFromCamera();
			Invalidate();
		}

		/// <summary>Get/Set whether the aspect ratio is locked to the current value</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool LockAspect
		{
			get { return Options.LockAspect != null; }
			set
			{
				if (LockAspect == value) return;
				if (value)
					Options.LockAspect = (XAxis.Span * Scene.Bounds.Height) / (YAxis.Span * Scene.Bounds.Width);
				else
					Options.LockAspect = null;
			}
		}

		/// <summary>Set the axis range based on the position of the camera and the field of view</summary>
		public void SetRangeFromCamera()
		{
			// The grid is always parallel to the image plane of the camera.
			// The camera forward vector points at the centre of the grid.

			// Project the camera to world vector into camera space to determine the centre of the X/Y axis. 
			var w2c = m4x4.InvertFast(Scene.Camera.O2W);

			// The span of the X/Y axis is determined by the FoV and the focus point distance.
			var wh = Scene.Camera.ViewArea(Scene.Camera.FocusDist);

			// Set the axes range
			var xmin = -w2c.pos.x - wh.x * 0.5;
			var xmax = -w2c.pos.x + wh.x * 0.5;
			var ymin = -w2c.pos.y - wh.y * 0.5;
			var ymax = -w2c.pos.y + wh.y * 0.5;
			if (xmin < xmax) XAxis.Set(xmin, xmax);
			if (ymin < ymax) YAxis.Set(ymin, ymax);
		}

		/// <summary>Position the camera based on the axis range</summary>
		public void SetCameraFromRange()
		{
			// Set the aspect ratio from the axis range and scene bounds
			Scene.Camera.Aspect = (float)(XAxis.Span / YAxis.Span);

			// Find the world space position of the new focus point.
			// Move the focus point within the focus plane (parallel to the camera XY plane)
			// and adjust the focus distance so that the view has a width equal to XAxis.Span.
			// The camera aspect ratio should ensure the YAxis is correct.
			var c2w = Scene.Camera.O2W;
			var focus =
				c2w.x * (float)XAxis.Centre +
				c2w.y * (float)YAxis.Centre +
				c2w.z * v4.Dot3(Scene.Camera.FocusPoint - v4.Origin, c2w.z);

			// Move the camera in the camera Z axis direction so that the width at the focus dist
			// matches the XAxis range. Tan(FovX/2) = (XAxis.Span/2)/d
			Scene.Camera.FocusDist = (float)(XAxis.Span / (2.0 * Math.Tan(Scene.Camera.FovX / 2.0)));
			c2w.pos = focus.w1 + Scene.Camera.FocusDist * c2w.z;
			
			Scene.Camera.O2W = c2w;
			Scene.Camera.Commit();
		}

		/// <summary>Paint the control</summary>
		private void DoPaint(Graphics gfx)
		{
			try
			{
				// Set the projection mode (this must come before 'SetRangeFromCamera' because
				// the 'ViewArea' function depends on the projection mode.
				Scene.Camera.Orthographic = Options.Orthographic;

				// Navigation moves the camera and axis ranges are derived from the camera position/view
				SetRangeFromCamera();

				// Get the areas to draw in
				var dims = ChartDimensions;

				// Ensure the scene is rendered
				Scene.Bounds = dims.ChartArea;
				Scene.Update();

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
			if (XAxis.Label.HasValue() && Options.ShowAxes)
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
			if (YAxis.Label.HasValue() && Options.ShowAxes)
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
			if (Options.ShowAxes)
			{
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

		/// <summary>True if users are allowed to add/remove/edit elements on the diagram</summary>
		public bool AllowEditing
		{
			get { return m_impl_allow_editing; }
			set
			{
				if (m_impl_allow_editing == value) return;
				m_impl_allow_editing = value;
				//UpdateEditToolbar();
			}
		}
		private bool m_impl_allow_editing;

		/// <summary>True if users are allowed to select elements on the diagram</summary>
		public bool AllowSelection { get; set; }

		/// <summary>Return a string representation of a location on the chart</summary>
		public string LocationText(Point location)
		{
			var pt = ClientToChart(location);

			double min, max, step;
			XAxis.GridLines(out min, out max, out step); var xtick = XAxis.TickText(pt.X, step);
			YAxis.GridLines(out min, out max, out step); var ytick = YAxis.TickText(pt.Y, step);
			return "{0} , {1}".Fmt(xtick, ytick);
		}

		/// <summary>Return the current mouse pointer location as a string</summary>
		public string PointerLocationText
		{
			get { return LocationText(PointToClient(MousePosition)); }
		}

		#region Chart Panel
		public class ChartPanel :Control
		{
			private ChartControl m_owner;         // The containing chart control
			private bool         m_render_needed; // True when the scene needs rendering again

			public ChartPanel(ChartControl owner)
			{
				SetStyle(ControlStyles.Selectable, false);
				if (this.IsInDesignMode())
					return;

				try
				{
					var opts = new View3d.WindowOptions(false, null, IntPtr.Zero) { DbgName = "Chart", Multisampling = owner.Options.AntiAliasing ? 4 : 1 };
					m_owner = owner;
					m_view3d = new View3d();
					m_window = new View3d.Window(m_view3d, Handle, opts);
					m_window.LightProperties = View3d.LightInfo.Directional(-v4.ZAxis, Colour32.Zero, Colour32.Gray, Colour32.Zero, 0f, 0f);
					m_window.FocusPointVisible = false;
					m_window.OriginPointVisible = false;
					m_window.Orthographic = true;
					m_camera = m_window.Camera;
					m_camera.Orthographic = true;
					m_camera.SetPosition(new v4(0, 0, 10, 1), v4.Origin, v4.YAxis);
					m_camera.ClipPlanes(0.01f, 1000f, true);
				}
				catch
				{
					Dispose();
					throw;
				}
			}
			protected override void Dispose(bool disposing)
			{
				Util.Dispose(ref m_window);
				Util.Dispose(ref m_view3d);
				base.Dispose(disposing);
			}
			protected override void OnResize(EventArgs e)
			{
				base.OnResize(e);
				if (Window != null)
					Window.RenderTargetSize = ClientSize;
			}
			protected override void OnPaintBackground(PaintEventArgs e)
			{
				// Swallow
			}
			protected override void OnPaint(PaintEventArgs e)
			{
				base.OnPaint(e);
				if (m_render_needed) DoPaint();
				Present();
			}
			protected override void OnInvalidated(InvalidateEventArgs e)
			{
				m_render_needed = true;
				Window?.Invalidate();
				base.OnInvalidated(e);
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

			/// <summary>Renderer</summary>
			public View3d View3d
			{
				[DebuggerStepThrough] get { return m_view3d; }
			}
			private View3d m_view3d;

			/// <summary>The view3d window for this control instance</summary>
			public View3d.Window Window
			{
				[DebuggerStepThrough] get { return m_window; }
			}
			private View3d.Window m_window;

			/// <summary>The view of the chart</summary>
			public View3d.CameraControls Camera
			{
				[DebuggerStepThrough] get { return m_camera; }
			}
			private View3d.CameraControls m_camera;

			/// <summary>Add an object to the scene</summary>
			public void AddObject(View3d.Object obj)
			{
				Window.AddObject(obj);
			}

			/// <summary>Remove an object from the scene</summary>
			public void RemoveObject(View3d.Object obj)
			{
				Window.RemoveObject(obj);
			}

			/// <summary>Render the chart 3d scene</summary>
			public void DoPaint()
			{
				if (Window == null || this.IsInDesignMode())
					return;

				// Clear the scene
				Window.RemoveObjects(all_except:true, context_id:ChartTools.Id);

				// Add axis graphics
				m_owner.Range.AddToScene(Window);

				// Add all chart elements
				foreach (var elem in m_owner.Elements)
				{
					if (!elem.Visible) continue;
					elem.AddToScene(Window);
				}

				// Add user graphics
				m_owner.RaiseChartRendering(new ChartRenderingEventArgs(Window));

				// Start the render
				Window.BackgroundColour = m_owner.Options.ChartBkColour;
				Window.FillMode = m_owner.Options.FillMode;
				Window.CullMode = m_owner.Options.CullMode;
				Window.Render();
			}
			public void Present()
			{
				Window?.Present();
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

					// Add space for the title
					if (chart.Title.HasValue())
					{
						r = gfx.MeasureString(chart.Title, chart.Options.TitleFont);
						rect.Y      += r.Height;
						rect.Height -= r.Height;
					}

					// Add space for the axes
					if (chart.Options.ShowAxes)
					{
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

						// Add space for the axis labels
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
							var h = Math.Max(chart.XAxis.MeasureTickText(gfx, false), chart.XAxis.Options.MinTickSize);
							rect.Height -= h;
						}
						if (chart.YAxis.Options.DrawTickLabels)
						{
							// Measure the width of the tick text
							var w = Math.Max(chart.YAxis.MeasureTickText(gfx, true), chart.YAxis.Options.MinTickSize);
							rect.X     += w;
							rect.Width -= w;
						}
					}

					if (rect.Width < 0) rect.Width = 0;
					if (rect.Height < 0) rect.Height = 0;
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
			get { return Scene.Bounds; }
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

		/// <summary>The client space area of the chart title</summary>
		private Rectangle TitleBounds
		{
			get { return Rectangle.FromLTRB(ChartBounds.Left, 0, ChartBounds.Right, ChartBounds.Top); }
		}

		#endregion

		#region RdrOptions
		[TypeConverter(typeof(TyConv))]
		public class RdrOptions :INotifyPropertyChanged
		{
			private class TyConv :GenericTypeConverter<RdrOptions> {}

			public RdrOptions()
			{
				NavigationMode            = ENavMode.Chart2D;
				LockAspect                = null;
				BkColour                  = SystemColors.Control;
				ChartBkColour             = Color.White;
				TitleColour               = Color.Black;
				TitleFont                 = new Font("tahoma", 12, FontStyle.Bold);
				TitleTransform            = new Matrix(1f, 0f, 0f, 1f, 0f, 0f);
				Margin                    = new Padding(3);
				NoteFont                  = new Font("tahoma", 8, FontStyle.Regular);
				SelectionColour           = Color.FromArgb(0x80, Color.DarkGray);
				ShowGridLines             = true;
				GridZOffset               = 0f;
				ShowAxes                  = true;
				AntiAliasing              = true;
				FillMode                  = View3d.EFillMode.Solid;
				CullMode                  = View3d.ECullMode.Back;
				Orthographic              = false;
				MinSelectionDistance      = 10f;
				MinDragPixelDistance      = 5f;
				PerpendicularZTranslation = false;
				ResetForward              = -v4.ZAxis;
				ResetUp                   = +v4.YAxis;
				XAxis                     = new Axis();
				YAxis                     = new Axis();
				// Don't forget to add new members to the other constructors!
			}
			public RdrOptions(RdrOptions rhs)
			{
				NavigationMode            = rhs.NavigationMode;
				LockAspect                = rhs.LockAspect;
				BkColour                  = rhs.BkColour;
				ChartBkColour             = rhs.ChartBkColour;
				TitleColour               = rhs.TitleColour;
				TitleFont                 = (Font)rhs.TitleFont.Clone();
				TitleTransform            = rhs.TitleTransform;
				Margin                    = rhs.Margin;
				NoteFont                  = (Font)rhs.NoteFont.Clone();
				SelectionColour           = rhs.SelectionColour;
				ShowGridLines             = rhs.ShowGridLines;
				GridZOffset               = rhs.GridZOffset;
				ShowAxes                  = rhs.ShowAxes;
				AntiAliasing              = rhs.AntiAliasing;
				FillMode                  = rhs.FillMode;
				CullMode                  = rhs.CullMode;
				Orthographic              = rhs.Orthographic;
				MinSelectionDistance      = rhs.MinSelectionDistance;
				MinDragPixelDistance      = rhs.MinDragPixelDistance;
				PerpendicularZTranslation = rhs.PerpendicularZTranslation;
				ResetForward              = rhs.ResetForward;
				ResetUp                   = rhs.ResetUp;
				XAxis                     = new Axis(rhs.XAxis);
				YAxis                     = new Axis(rhs.YAxis);
			}
			public RdrOptions(XElement node) :this()
			{
				NavigationMode            = node.Element(nameof(NavigationMode           )).As(NavigationMode           );
				LockAspect                = node.Element(nameof(LockAspect               )).As(LockAspect               );
				BkColour                  = node.Element(nameof(BkColour                 )).As(BkColour                 );
				ChartBkColour             = node.Element(nameof(ChartBkColour            )).As(ChartBkColour            );
				TitleColour               = node.Element(nameof(TitleColour              )).As(TitleColour              );
				TitleTransform            = node.Element(nameof(TitleTransform           )).As(TitleTransform           );
				Margin                    = node.Element(nameof(Margin                   )).As(Margin                   );
				TitleFont                 = node.Element(nameof(TitleFont                )).As(TitleFont                );
				NoteFont                  = node.Element(nameof(NoteFont                 )).As(NoteFont                 );
				SelectionColour           = node.Element(nameof(SelectionColour          )).As(SelectionColour          );
				ShowAxes                  = node.Element(nameof(ShowAxes                 )).As(ShowAxes                 );
				ShowGridLines             = node.Element(nameof(ShowGridLines            )).As(ShowGridLines            );
				GridZOffset               = node.Element(nameof(GridZOffset              )).As(GridZOffset              );
				AntiAliasing              = node.Element(nameof(AntiAliasing             )).As(AntiAliasing             );
				FillMode                  = node.Element(nameof(FillMode                 )).As(FillMode                 );
				CullMode                  = node.Element(nameof(CullMode                 )).As(CullMode                 );
				Orthographic              = node.Element(nameof(Orthographic             )).As(Orthographic             );
				MinSelectionDistance      = node.Element(nameof(MinSelectionDistance     )).As(MinSelectionDistance     );
				MinDragPixelDistance      = node.Element(nameof(MinDragPixelDistance     )).As(MinDragPixelDistance     );
				PerpendicularZTranslation = node.Element(nameof(PerpendicularZTranslation)).As(PerpendicularZTranslation);
				ResetForward              = node.Element(nameof(ResetForward             )).As(ResetForward             );
				ResetUp                   = node.Element(nameof(ResetUp                  )).As(ResetUp                  );
				XAxis                     = node.Element(nameof(XAxis                    )).As(XAxis                    );
				YAxis                     = node.Element(nameof(YAxis                    )).As(YAxis                    );
			}
			public XElement ToXml(XElement node)
			{
				node.Add2(nameof(NavigationMode           ) , NavigationMode            , false);
				node.Add2(nameof(LockAspect               ) , LockAspect                , false);
				node.Add2(nameof(BkColour                 ) , BkColour                  , false);
				node.Add2(nameof(ChartBkColour            ) , ChartBkColour             , false);
				node.Add2(nameof(TitleColour              ) , TitleColour               , false);
				node.Add2(nameof(TitleTransform           ) , TitleTransform            , false);
				node.Add2(nameof(Margin                   ) , Margin                    , false);
				node.Add2(nameof(TitleFont                ) , TitleFont                 , false);
				node.Add2(nameof(NoteFont                 ) , NoteFont                  , false);
				node.Add2(nameof(SelectionColour          ) , SelectionColour           , false);
				node.Add2(nameof(ShowGridLines            ) , ShowGridLines             , false);
				node.Add2(nameof(GridZOffset              ) , GridZOffset               , false);
				node.Add2(nameof(ShowAxes                 ) , ShowAxes                  , false);
				node.Add2(nameof(AntiAliasing             ) , AntiAliasing              , false);
				node.Add2(nameof(FillMode                 ) , FillMode                  , false);
				node.Add2(nameof(CullMode                 ) , CullMode                  , false);
				node.Add2(nameof(Orthographic             ) , Orthographic              , false);
				node.Add2(nameof(MinSelectionDistance     ) , MinSelectionDistance      , false);
				node.Add2(nameof(MinDragPixelDistance     ) , MinDragPixelDistance      , false);
				node.Add2(nameof(PerpendicularZTranslation) , PerpendicularZTranslation , false);
				node.Add2(nameof(ResetForward             ) , ResetForward              , false);
				node.Add2(nameof(ResetUp                  ) , ResetUp                   , false);
				node.Add2(nameof(XAxis                    ) , XAxis                     , false);
				node.Add2(nameof(YAxis                    ) , YAxis                     , false);
				return node;
			}
			public override string ToString()
			{
				return "Rendering Options";
			}

			/// <summary>Property changed</summary>
			public event PropertyChangedEventHandler PropertyChanged;
			private void SetProp<T>(ref T prop, T value, string name)
			{
				if (Equals(prop, value)) return;
				prop = value;
				PropertyChanged.Raise(this, new PropertyChangedEventArgs(name));
			}

			/// <summary>The control method used for shifting the camera</summary>
			public ENavMode NavigationMode
			{
				get { return m_NavigationMode; }
				set { SetProp(ref m_NavigationMode, value, nameof(NavigationMode)); }
			}
			private ENavMode m_NavigationMode;

			/// <summary>Lock the aspect ratio for the chart (null means unlocked)</summary>
			public double? LockAspect
			{
				get { return m_LockAspect; }
				set { SetProp(ref m_LockAspect, value, nameof(LockAspect)); }
			}
			private double? m_LockAspect;

			/// <summary>The fill colour of the non-chart area of the control (e.g. behind the axis labels)</summary>
			public Color BkColour
			{
				get { return m_BkColour; }
				set { SetProp(ref m_BkColour, value, nameof(BkColour)); }
			}
			private Color m_BkColour;

			/// <summary>The fill colour of the chart plot area</summary>
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

			/// <summary>The offset from the origin for the grid, in the forward direction of the camera</summary>
			public float GridZOffset
			{
				get { return m_GridZOffset; }
				set { SetProp(ref m_GridZOffset, value, nameof(GridZOffset)); }
			}
			private float m_GridZOffset;

			/// <summary>Show/Hide the chart axes</summary>
			public bool ShowAxes
			{
				get { return m_ShowAxes; }
				set { SetProp(ref m_ShowAxes, value, nameof(ShowAxes)); }
			}
			private bool m_ShowAxes;

			/// <summary>Enable/Disable multi-sampling in the view3d view. Can only be changed before the view is created</summary>
			public bool AntiAliasing
			{
				get { return m_AntiAliasing; }
				set { SetProp(ref m_AntiAliasing, value, nameof(AntiAliasing)); }
			}
			private bool m_AntiAliasing;

			/// <summary>Fill mode, solid, wire, or both</summary>
			public View3d.EFillMode FillMode
			{
				get { return m_FillMode; }
				set { SetProp(ref m_FillMode, value, nameof(FillMode)); }
			}
			private View3d.EFillMode m_FillMode;

			/// <summary>Fill mode, solid, wire, or both</summary>
			public View3d.ECullMode CullMode
			{
				get { return m_CullMode; }
				set { SetProp(ref m_CullMode, value, nameof(CullMode)); }
			}
			private View3d.ECullMode m_CullMode;

			/// <summary>Get/Set orthographic camera projection</summary>
			public bool Orthographic
			{
				get { return m_Orthographic; }
				set { SetProp(ref m_Orthographic, value, nameof(Orthographic)); }
			}
			private bool m_Orthographic;

			/// <summary>How close a click has to be for selection to occur (in client space)</summary>
			public float MinSelectionDistance
			{
				get { return m_MinSelectionDistance; }
				set { SetProp(ref m_MinSelectionDistance, value, nameof(MinSelectionDistance)); }
			}
			private float m_MinSelectionDistance;

			/// <summary>Minimum distance in pixels before the chart starts dragging</summary>
			public float MinDragPixelDistance
			{
				get { return m_MinDragPixelDistance; }
				set { SetProp(ref m_MinDragPixelDistance, value, nameof(MinDragPixelDistance)); }
			}
			private float m_MinDragPixelDistance;

			/// <summary>True if the camera should move along a ray cast through the mouse point</summary>
			public bool PerpendicularZTranslation
			{
				get { return m_PerpendicularZTranslation; }
				set { SetProp(ref m_PerpendicularZTranslation, value, nameof(PerpendicularZTranslation)); }
			}
			private bool m_PerpendicularZTranslation;

			/// <summary>The forward direction of the camera when reset</summary>
			public v4 ResetForward
			{
				get { return m_ResetForward; }
				set { SetProp(ref m_ResetForward, value, nameof(ResetForward)); }
			}
			public v4 m_ResetForward;

			/// <summary>The up direction of the camera when reset</summary>
			public v4 ResetUp
			{
				get { return m_ResetUp; }
				set { SetProp(ref m_ResetUp, value, nameof(ResetUp)); }
			}
			public v4 m_ResetUp;

			/// <summary>XAxis rendering options</summary>
			public Axis XAxis 
			{
				get { return m_XAxis; }
				private set
				{
					if (m_XAxis == value) return;
					if (m_XAxis != null) m_XAxis.PropertyChanged -= HandleXAxisPropertyChanged;
					m_XAxis = value;
					if (m_XAxis != null) m_XAxis.PropertyChanged += HandleXAxisPropertyChanged;
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
					if (m_YAxis != null) m_YAxis.PropertyChanged += HandleYAxisPropertyChanged;
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
					AxisColour     = node.Element(nameof(AxisColour    )).As(AxisColour    );
					LabelColour    = node.Element(nameof(LabelColour   )).As(LabelColour   );
					GridColour     = node.Element(nameof(GridColour    )).As(GridColour    );
					TickColour     = node.Element(nameof(TickColour    )).As(TickColour    );
					LabelFont      = node.Element(nameof(LabelFont     )).As(LabelFont     );
					TickFont       = node.Element(nameof(TickFont      )).As(TickFont      );
					DrawTickMarks  = node.Element(nameof(DrawTickMarks )).As(DrawTickMarks );
					DrawTickLabels = node.Element(nameof(DrawTickLabels)).As(DrawTickLabels);
					TickLength     = node.Element(nameof(TickLength    )).As(TickLength    );
					MinTickSize    = node.Element(nameof(MinTickSize   )).As(MinTickSize   );
					LabelTransform = node.Element(nameof(LabelTransform)).As(LabelTransform);
					AxisThickness  = node.Element(nameof(AxisThickness )).As(AxisThickness );
					PixelsPerTick  = node.Element(nameof(PixelsPerTick )).As(PixelsPerTick );
					ShowGridLines  = node.Element(nameof(ShowGridLines )).As(ShowGridLines );
				}
				public XElement ToXml(XElement node)
				{
					node.Add2(nameof(AxisColour    ), AxisColour    , false);
					node.Add2(nameof(LabelColour   ), LabelColour   , false);
					node.Add2(nameof(GridColour    ), GridColour    , false);
					node.Add2(nameof(TickColour    ), TickColour    , false);
					node.Add2(nameof(LabelFont     ), LabelFont     , false);
					node.Add2(nameof(TickFont      ), TickFont      , false);
					node.Add2(nameof(DrawTickMarks ), DrawTickMarks , false);
					node.Add2(nameof(DrawTickLabels), DrawTickLabels, false);
					node.Add2(nameof(TickLength    ), TickLength    , false);
					node.Add2(nameof(MinTickSize   ), MinTickSize   , false);
					node.Add2(nameof(LabelTransform), LabelTransform, false);
					node.Add2(nameof(AxisThickness ), AxisThickness , false);
					node.Add2(nameof(PixelsPerTick ), PixelsPerTick , false);
					node.Add2(nameof(ShowGridLines ), ShowGridLines , false);
					return node;
				}
				public override string ToString()
				{
					return "Axis Options";
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

		#region Range

		/// <summary>The 2D size of the chart</summary>
		public class RangeData :IDisposable
		{
			public RangeData(ChartControl owner)
			{
				Owner = owner;
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
			public ChartControl Owner { [DebuggerStepThrough] get; private set; }

			/// <summary>The chart X axis</summary>
			public Axis XAxis
			{
				[DebuggerStepThrough] get { return m_xaxis; }
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
				[DebuggerStepThrough] get { return m_yaxis; }
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
				var cam = Owner.Scene.Camera;
				var wh = cam.ViewArea(Owner.Scene.Camera.FocusDist);
					
				// Position the grid lines so that they line up with the axis tick marks
				// Grid lines are modelled from the bottom left corner
				if (XAxis.Owner.Options.ShowGridLines && XAxis.Options.ShowGridLines)
				{
					double min, max, step;
					XAxis.GridLines(out min, out max, out step);

					var o2w = cam.O2W;
					o2w.pos = cam.FocusPoint - o2w * new v4((float)(wh.x/2 - min), wh.y/2, -Owner.Options.GridZOffset, 0);

					XAxis.GridLineGfx.O2WSet(o2w);
					window.AddObject(XAxis.GridLineGfx);
				}
				if (YAxis.Owner.Options.ShowGridLines && YAxis.Options.ShowGridLines)
				{
					double min, max, step;
					YAxis.GridLines(out min, out max, out step);

					var o2w = cam.O2W;
					o2w.pos = cam.FocusPoint - o2w * new v4(wh.x/2, (float)(wh.y/2 - min), -Owner.Options.GridZOffset, 0);

					YAxis.GridLineGfx.O2WSet(o2w);
					window.AddObject(YAxis.GridLineGfx);
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
					Owner.BeginInvoke(NotifyMoved);
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
					Owner.BeginInvoke(NotifyMoved);
				}
				if (sender == XAxis) m_moved_args.MoveType |= EMoveType.XScrolled;
				if (sender == YAxis) m_moved_args.MoveType |= EMoveType.YScrolled;
			}

			/// <summary>Raise the ChartMoved event on the chart</summary>
			private void NotifyMoved()
			{
				Owner.OnChartMoved(m_moved_args);
				m_moved_args = null;
			}
			private ChartMovedEventArgs m_moved_args;

			/// <summary>A axis on the chart (typically X or Y)</summary>
			public class Axis :IDisposable
			{
				public Axis(EAxis axis, ChartControl owner)
					:this(axis, owner, 0f, 1f)
				{ }
				public Axis(EAxis axis, ChartControl owner, double min, double max)
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
					TickText        = DefaultTickText;
					MeasureTickText = DefaultMeasureTickText;
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
					MeasureTickText = rhs.MeasureTickText;
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

				/// <summary>Which axis this is</summary>
				public EAxis AxisType { get; private set; }

				/// <summary>The chart that owns this axis</summary>
				public ChartControl Owner { get; private set; }

				/// <summary>The axis label</summary>
				public string Label
				{
					[DebuggerStepThrough] get { return m_label ?? string.Empty; }
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
					[DebuggerStepThrough] get { return m_min; }
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
					[DebuggerStepThrough] get { return m_max; }
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
					[DebuggerStepThrough] get { return Max - Min; }
					set
					{
						if (Span == value) return;
						Set(Centre - 0.5*value, Centre + 0.5*value);
					}
				}

				/// <summary>The centre value of the range</summary>
				public double Centre
				{
					[DebuggerStepThrough] get { return (Min + Max) * 0.5; }
					set
					{
						if (Centre == value) return;
						Set(m_min + value - Centre, m_max + value - Centre);
					}
				}

				/// <summary>The min/max limits as a range</summary>
				public RangeF Range
				{
					[DebuggerStepThrough] get { return new RangeF(Min, Max); }
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

				/// <summary>Return the width/height to reserve for the tick text. "float MeasureTickText(Graphics gfx, bool width)"</summary>
				public Func<Graphics, bool, float> MeasureTickText;

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
					Set(range.Beg, range.End);
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
							// Need to allow for one step in either direction because we only create the
							// grid lines model when scaling and we can translate by a max of one step in
							// either direction.
							double min, max, step;
							GridLines(out min, out max, out step);
							var num_lines = (int)(2 + (max - min) / step);

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
							m_gridlines.FlagsSet(View3d.EFlags.BBoxInvisible); 
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

				/// <summary>Show the axis context menu</summary>
				public void ShowContextMenu(Point location, HitTestResult hit_result)
				{
					var cmenu = new ContextMenuStrip();
					using (cmenu.SuspendLayout(true))
					{
						{// Lock Range
							var opt = cmenu.Items.Add2(new ToolStripMenuItem("Lock") { Checked = LockRange, CheckOnClick = true });
							opt.CheckedChanged += (s,a) =>
							{
								LockRange = opt.Checked;
							};
						}

						// Customise the menu
						var type = AxisType == EAxis.XAxis ? AddUserMenuOptionsEventArgs.EType.XAxis : AddUserMenuOptionsEventArgs.EType.YAxis;
						Owner.OnAddUserMenuOptions(new AddUserMenuOptionsEventArgs(type, cmenu, hit_result));
					}
					cmenu.Items.TidySeparators();
					if (cmenu.Items.Count != 0)
						cmenu.Show(Owner, location);
				}

				/// <summary>Default value to text conversion</summary>
				public string DefaultTickText(double x, double step)
				{
					return Maths.RoundSF(x, 4).ToString("G8");
				}

				/// <summary>Default tick text measurement</summary>
				public float DefaultMeasureTickText(Graphics gfx, bool width)
				{
					var area = gfx.MeasureString("0.000E+00", Options.TickFont);
					return width ? area.Width : area.Height;
				}

				/// <summary>Friendly string view</summary>
				public override string ToString()
				{
					return "{0} - [{1}:{2}]".Fmt(Label, Min, Max);
				}
			}
		}

		/// <summary>Raised whenever the view3d area of the chart is scrolled or zoomed</summary>
		public event EventHandler<ChartMovedEventArgs> ChartMoved;
		protected virtual void OnChartMoved(ChartMovedEventArgs args)
		{
			ChartMoved.Raise(this, args);
		}

		#endregion

		#region Navigation

		// Prefer camera matrix operations for navigation, then call
		// SetRangeFromCamera to update the X/Y axis since these work
		// for 2D or 3D.

		/// <summary>Navigation methods for moving the camera</summary>
		public enum ENavMode
		{
			[Desc("2D Chart")]
			Chart2D,

			[Desc("3D Scene")]
			Scene3D,
		}

		/// <summary>Enable/Disable mouse navigation</summary>
		[Browsable(false)]
		public bool DefaultMouseControl
		{
			get { return m_default_mouse_control; }
			set
			{
				if (m_default_mouse_control == value) return;
				m_default_mouse_control = value;
			}
		}
		private bool m_default_mouse_control;

		/// <summary>Enable/Disable keyboard shortcuts for navigation</summary>
		public bool DefaultKeyboardShortcuts
		{
			get { return m_default_keyboard_shortcuts; }
			set
			{
				if (m_default_keyboard_shortcuts == value) return;
				m_default_keyboard_shortcuts = value;
			}
		}
		private bool m_default_keyboard_shortcuts;

		/// <summary>Mouse events on the chart</summary>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);

			// If a mouse op is already active, ignore mouse down
			if (MouseOperations.Active != null)
				return;

			// Look for the mouse op to perform
			if (MouseOperations.Pending(e.Button) == null && DefaultMouseControl)
			{
				switch (e.Button)
				{
				default: return;
				case MouseButtons.Left:   MouseOperations.SetPending(e.Button, new MouseOpDefaultLButton(this)); break;
				case MouseButtons.Middle: MouseOperations.SetPending(e.Button, new MouseOpDefaultMButton(this)); break;
				case MouseButtons.Right:  MouseOperations.SetPending(e.Button, new MouseOpDefaultRButton(this)); break;
				}
			}

			// Start the next mouse op
			MouseOperations.BeginOp(e.Button);

			// Get the mouse op, save mouse location and hit test data, then call op.MouseDown()
			var op = MouseOperations.Active;
			if (op != null && !op.Cancelled)
			{
				op.m_btn_down    = true;
				op.m_grab_client = e.Location; // Note: in ChartControl space, not ChartPanel space
				op.m_grab_chart  = ClientToChart(op.m_grab_client);
				op.m_hit_result  = HitTestCS(op.m_grab_client, ModifierKeys, null);
				op.MouseDown(e);
				Capture = true;
			}
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			// Look for the mouse op to perform
			var op = MouseOperations.Active;
			if (op != null)
			{
				if (!op.Cancelled)
					op.MouseMove(e);
			}
			// Otherwise, provide mouse hover detection
			else
			{
				var hit = HitTestCS(e.Location, ModifierKeys, null);
				var hovered = hit.Hits.Select(x => x.Element).ToHashSet();

				// Remove elements that are no longer hovered
				// and remove existing hovered's from the set.
				for (int i = Hovered.Count; i-- != 0;)
				{
					if (hovered.Contains(Hovered[i]))
						hovered.Remove(Hovered[i]);
					else
						Hovered.RemoveAt(i);
				}
				
				// Add elements that are now hovered
				Hovered.AddRange(hovered);
			}
		}
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);

			// Only release the mouse when all buttons are up
			if (MouseButtons == MouseButtons.None)
				Capture = false;

			// Look for the mouse op to perform
			var op = MouseOperations.Active;
			if (op != null && !op.Cancelled)
				op.MouseUp(e);
			
			MouseOperations.EndOp(e.Button);
		}
		protected override void OnMouseWheel(MouseEventArgs e)
		{
			base.OnMouseWheel(e);

			var chart_bounds = Scene.Bounds;
			if (chart_bounds.Contains(e.Location))
			{
				// If there is a mouse op in progress, ignore the wheel
				var op = MouseOperations.Active;
				if (op == null || op.Cancelled)
				{
					// Translate the camera along a ray through 'point'
					var loc = Control_.MapPoint(this, Scene, e.Location);
					var along_ray = Options.PerpendicularZTranslation == (ModifierKeys == Keys.Alt);
					Scene.Window.MouseNavigateZ(loc, e.Delta, along_ray);
					Invalidate();
				}
			}
			else if (Options.ShowAxes)
			{
				var scale = 0.001f;
				if (ModifierKeys.HasFlag(Keys.Shift)) scale *= 0.1f;
				if (ModifierKeys.HasFlag(Keys.Alt  )) scale *= 0.01f;
				var delta = Maths.Clamp(e.Delta * scale, -999f, 999f);

				// Change the aspect ratio by zooming on the XAxis
				var chg = false;
				var xaxis_bounds = Rectangle.FromLTRB(chart_bounds.Left, chart_bounds.Bottom, chart_bounds.Right, ClientSize.Height);
				if (xaxis_bounds.Contains(e.Location) && !XAxis.LockRange)
				{
					if (ModifierKeys == Keys.Control && XAxis.AllowScroll)
					{
						XAxis.Shift(XAxis.Span * delta);
						chg = true;
					}
					if (ModifierKeys != Keys.Control && XAxis.AllowZoom)
					{
						XAxis.Span *= (1.0f - delta);
						if (Options.LockAspect != null)
							YAxis.Span *= (1.0f - delta);
						chg = true;
					}
				}

				// Check the aspect ratio by zooming on the YAxis
				var yaxis_bounds = Rectangle.FromLTRB(0, chart_bounds.Top, chart_bounds.Left, chart_bounds.Bottom);
				if (yaxis_bounds.Contains(e.Location) && !YAxis.LockRange)
				{
					if (ModifierKeys == Keys.Control && YAxis.AllowScroll)
					{
						YAxis.Shift(YAxis.Span * delta);
						chg = true;;
					}
					if (ModifierKeys != Keys.Control && YAxis.AllowZoom)
					{
						YAxis.Span *= (1.0f - delta);
						if (Options.LockAspect != null)
							XAxis.Span *= (1.0f - delta);
						chg = true;
					}
				}

				// Set the camera position from the Axis ranges
				if (chg)
				{
					SetCameraFromRange();
					Invalidate();
				}
			}
		}
		protected override void OnKeyDown(KeyEventArgs e)
		{
			SetCursor();

			var op = MouseOperations.Active;
			if (op != null && !e.Handled)
				op.OnKeyDown(e);

			// Allow derived classes to handle the key
			base.OnKeyDown(e);

			// If the current mouse operation doesn't use the key,
			// see if it's a default keyboard shortcut.
			if (!e.Handled && DefaultKeyboardShortcuts)
				TranslateKey(e);

		}
		protected override void OnKeyUp(KeyEventArgs e)
		{
			SetCursor();

			var op = MouseOperations.Active;
			if (op != null && !e.Handled)
				op.OnKeyUp(e);

			base.OnKeyUp(e);
		}

		/// <summary>Set the mouse cursor based on key state</summary>
		protected virtual void SetCursor()
		{
			// Sub class to do something like this:
			// if (ModifierKeys.HasFlag(Keys.Shift))   Cursor = Cursors.ArrowPlus;
			// if (ModifierKeys.HasFlag(Keys.Control)) Cursor = Cursors.ArrowMinus;
			Cursor = Cursors.Default;
		}

		/// <summary>Raised when the chart is clicked with the mouse</summary>
		public event EventHandler<ChartClickedEventArgs> ChartClicked;
		protected virtual void OnChartClicked(ChartClickedEventArgs args)
		{
			ChartClicked.Raise(this, args);
		}

		/// <summary>Raised when the chart is area selected</summary>
		public event EventHandler<ChartAreaSelectEventArgs> ChartAreaSelect;
		protected virtual void OnChartAreaSelect(ChartAreaSelectEventArgs args)
		{
			ChartAreaSelect.Raise(this, args);

			// Select chart elements by default
			if (!args.Handled)
			{
				var rect = new RectangleF(args.SelectionArea.MinX, args.SelectionArea.MinY, args.SelectionArea.SizeX, args.SelectionArea.SizeY);
				SelectElements(rect, ModifierKeys);
			}
		}

		/// <summary>Shifts the X and Y range of the chart so that chart space position 'gs_point' is at client space position 'cs_point'</summary>
		public void PositionChart(Point cs_point, PointF gs_point)
		{
			var dst = ClientToChart(cs_point);
			var c2w = Scene.Camera.O2W;
			c2w.pos += c2w.x * (gs_point.X - dst.X) + c2w.y * (gs_point.Y - dst.Y);
			Scene.Camera.O2W = c2w;
			//XAxis.Shift(gs_point.X - dst.X);
			//YAxis.Shift(gs_point.Y - dst.Y);
			//SetCameraFromRange();
			Invalidate();
		}

		/// <summary>Handle navigation keyboard shortcuts</summary>
		public virtual void TranslateKey(KeyEventArgs e)
		{
			if (e.Handled) return;
			switch (e.KeyCode)
			{
			case Keys.Escape:
				#region
				{
					if (AllowSelection)
					{
						Selected.Clear();
						Invalidate();
					}
					break;
				}
				#endregion
			case Keys.Delete:
				#region
				{
					if (AllowEditing)
					{
						//// Allow the caller to cancel the deletion or change the selection
						//var res = new DiagramChangedRemoveElementsEventArgs(Selected.ToArray());
						//if (!RaiseDiagramChanged(res).Cancel)
						//{
						//	foreach (var elem in res.Elements)
						//	{
						//		var node = elem as Node;
						//		if (node != null)
						//			node.DetachConnectors();
								
						//		var conn = elem as Connector;
						//		if (conn != null)
						//			conn.DetachNodes();

						//		Elements.Remove(elem);
						//	}
						//	Invalidate();
						//}
					}
					break;
				}
				#endregion
			case Keys.F5:
				#region
				{
					//InvalidateAllElements();
					Invalidate();
					break;
				}
				#endregion
			case Keys.F7:
				#region
				{
					AutoRange();
					break;
				}
				#endregion
			case Keys.A:
				#region
				{
					if ((e.Modifiers & Keys.Control) != 0)
					{
						Selected.Clear();
						Selected.AddRange(Elements);
						Invalidate();
						Debug.Assert(CheckConsistency());
					}
					break;
				}
				#endregion
			}
		}

		#endregion

		#region Mouse Operations

		/// <summary>Per button current mouse operation</summary>
		public MouseOps MouseOperations
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>Manages per-button mouse operations</summary>
		public class MouseOps
		{
			/// <summary>The current mouse operation in effect for each mouse button</summary>
			private readonly Dictionary<MouseButtons, MouseOp> m_ops;

			/// <summary>The next mouse operation for each mouse button</summary>
			private readonly Dictionary<MouseButtons, MouseOp> m_pending;

			public MouseOps()
			{
				m_ops     = Enum<MouseButtons>.Values.ToDictionary(k => k, k => (MouseOp)null);
				m_pending = Enum<MouseButtons>.Values.ToDictionary(k => k, k => (MouseOp)null);
			}

			/// <summary>The currently active mouse op</summary>
			public MouseOp Active { get; private set; }

			/// <summary>Return the op pending for button 'idx'</summary>
			public MouseOp Pending(MouseButtons btn)
			{
				return m_pending[btn];
			}

			/// <summary>Add a mouse op to be started on the next mouse down event for button 'idx'</summary>
			public void SetPending(MouseButtons btn, MouseOp op)
			{
				if (m_pending[btn] != null) m_pending[btn].Dispose();
				m_pending[btn] = op;
			}

			/// <summary>Start/End the next mouse op for button 'idx'</summary>
			public void BeginOp(MouseButtons btn)
			{
				Active = m_pending[btn];
				m_pending[btn] = null;

				// If the op starts immediately without a mouse down, fake
				// a mouse down event as soon as it becomes active.
				if (Active != null)
				{
					if (!Active.StartOnMouseDown)
						Active.MouseDown(null);
				}
			}
			public void EndOp(MouseButtons btn)
			{
				if (Active != null)
				{
					if (Active.Cancelled) Active.NotifyCancelled();
					Active.Dispose();
				}
				Active = null;

				// If the next op starts immediately, begin it now
				if (m_pending[btn] != null && !m_pending[btn].StartOnMouseDown)
					BeginOp(btn);
			}
		}

		/// <summary>Base class for a mouse operation performed with the mouse 'down -> [drag] -> up' sequence</summary>
		public abstract class MouseOp :IDisposable
		{
			// The general process goes:
			//  A mouse op is created and set as the pending operation in 'MouseOps'.
			//  MouseDown on the chart calls 'BeginOp' which moves the pending op to 'Active'.
			//  Mouse events on the chart are forwarded to the active op
			//  MouseUp ends the current Active op, if the pending op should start immediately
			//  then mouse up causes the next op to start (with a faked MouseDown event).
			//  If at any point a mouse op is cancelled, no further mouse events are forwarded
			//  to the op. When EndOp is called, a notification can be sent by the op to indicate cancelled.

			/// <summary>The owning chart</summary>
			protected ChartControl m_chart;
			protected Scope m_suspend_scope;

			// Selection data for a mouse button
			public bool          m_btn_down;    // True while the corresponding mouse button is down
			public Point         m_grab_client; // The client space location of where the chart was "grabbed" (note: ChartControl, not ChartPanel space)
			public PointF        m_grab_chart;  // The chart space location of where the chart was "grabbed"
			public HitTestResult m_hit_result;  // The hit test result on mouse down

			public MouseOp(ChartControl chart)
			{
				m_chart = chart;
				StartOnMouseDown = true;
				Cancelled = false;
			}
			public virtual void Dispose()
			{
				Util.Dispose(ref m_suspend_scope);
			}

			/// <summary>True if mouse down starts the op, false if the op should start as soon as possible</summary>
			public bool StartOnMouseDown { get; set; }

			/// <summary>True if the op was aborted</summary>
			public bool Cancelled { get; protected set; }

			/// <summary>True if the mouse down event should be treated as a click (so far)</summary>
			protected bool IsClick(Point location)
			{
				var grab = v2.From(m_grab_client);
				var diff = v2.From(location) - grab;
				return diff.Length2Sq < Maths.Sqr(m_chart.Options.MinDragPixelDistance);
			}

			/// <summary>Called on mouse down</summary>
			public virtual void MouseDown(MouseEventArgs e) { }

			/// <summary>Called on mouse move</summary>
			public virtual void MouseMove(MouseEventArgs e) { }

			/// <summary>Called on mouse up</summary>
			public virtual void MouseUp(MouseEventArgs e) { }

			/// <summary>Called on key down</summary>
			public virtual void OnKeyDown(KeyEventArgs e) { }

			/// <summary>Called on key up</summary>
			public virtual void OnKeyUp(KeyEventArgs e) { }

			/// <summary>Called when the mouse operation is cancelled</summary>
			public virtual void NotifyCancelled() {}
		}

		/// <summary>A mouse operation for dragging selected elements around, area selecting, or left clicking (Left Button)</summary>
		public class MouseOpDefaultLButton :MouseOp
		{
			private HitTestResult.Hit m_hit_selected;
			private bool m_selection_graphic_added;
			private EAxis m_hit_axis;

			public MouseOpDefaultLButton(ChartControl chart) :base(chart)
			{
				m_selection_graphic_added = false;
			}
			public override void MouseDown(MouseEventArgs e)
			{
				// See where mouse down occurred
				if (m_chart.ChartBounds.Contains(e.Location)) m_hit_axis = EAxis.None;
				if (m_chart.XAxisBounds.Contains(e.Location)) m_hit_axis = EAxis.XAxis;
				if (m_chart.YAxisBounds.Contains(e.Location)) m_hit_axis = EAxis.YAxis;

				// Look for a selected object that the mouse operation starts on
				m_hit_selected = m_hit_result.Hits.FirstOrDefault(x => x.Element.Selected);

				// Record the drag start positions for selected objects
				foreach (var elem in m_chart.Selected)
					elem.DragStartPosition = elem.Position;

				// For 3D scenes, left mouse rotates if mouse down is within the chart bounds
				if (m_chart.Options.NavigationMode == ENavMode.Scene3D && m_hit_axis == EAxis.None)
					m_chart.Scene.Window.MouseNavigate(e.Location, View3d.ENavOp.Rotate, true);

				// Prevent events while dragging the elements around
				m_suspend_scope = m_chart.SuspendChartChanged(raise_on_resume:true);
			}
			public override void MouseMove(MouseEventArgs e)
			{
				// If we haven't dragged, treat it as a click instead (i.e. ignore till it's a drag operation)
				if (IsClick(e.Location) && !m_selection_graphic_added)
					return;

				var drag_selected = m_chart.AllowEditing && m_hit_selected != null;
				if (drag_selected)
				{
					// If the drag operation started on a selected element then drag the
					// selected elements within the diagram.
					var delta = Drawing_.Subtract(m_chart.ClientToChart(e.Location), m_grab_chart);
					m_chart.DragSelected(v2.From(delta), false);
				}
				else if (m_chart.Options.NavigationMode == ENavMode.Chart2D)
				{
					// Otherwise change the selection area
					if (!m_selection_graphic_added)
					{
						m_chart.Scene.AddObject(m_chart.Tools.AreaSelect);
						m_selection_graphic_added = true;
					}

					// Position the selection graphic
					var selection_area = BRect.FromBounds(m_grab_chart, m_chart.ClientToChart(e.Location));
					m_chart.Tools.AreaSelect.O2P = m4x4.Scale(
						selection_area.SizeX,
						selection_area.SizeY,
						1f,
						new v4(selection_area.Centre, m_chart.HighestZ, 1));
				}
				else if (m_chart.Options.NavigationMode == ENavMode.Scene3D)
				{
					// MouseButtons.Right provides rotation, which we want for left mouse button.
					m_chart.Scene.Window.MouseNavigate(e.Location, View3d.ENavOp.Rotate, false);
				}
				m_chart.Invalidate();
				m_chart.Update();
			}
			public override void MouseUp(MouseEventArgs e)
			{
				Util.Dispose(ref m_suspend_scope);

				// If this is a single click...
				if (IsClick(e.Location))
				{
					// Pass the click event out to users first
					var args = new ChartClickedEventArgs(m_hit_result, e);
					m_chart.OnChartClicked(args);

					// If a selected element was hit on mouse down, see if it handles the click
					if (!args.Handled && m_hit_selected != null)
					{
						m_hit_selected.Element.HandleClicked(args);
					}

					// If no selected element was hit, try hovered elements
					if (!args.Handled && m_hit_result.Hits.Count != 0)
					{
						for (int i = 0; i != m_hit_result.Hits.Count && !args.Handled; ++i)
							m_hit_result.Hits[i].Element.HandleClicked(args);
					}

					// If the click is still unhandled, use the click to try to select something (if within the chart)
					if (!args.Handled && m_hit_result.Zone.HasFlag(HitTestResult.EZone.Chart))
					{
						var selection_area = new BRect(m_grab_chart, v2.Zero);
						m_chart.SelectElements(selection_area, ModifierKeys);
					}
				}
				// Otherwise this is a drag action
				else
				{
					// If an element was selected, drag it around
					if (m_hit_selected != null && m_chart.AllowEditing)
					{
						var delta = Drawing_.Subtract(m_chart.ClientToChart(e.Location), m_grab_chart);
						m_chart.DragSelected(delta, true);
					}
					else if (m_chart.Options.NavigationMode == ENavMode.Chart2D)
					{
						// Otherwise create an area selection if the click started within the chart
						if (m_hit_result.Zone.HasFlag(HitTestResult.EZone.Chart))
						{
							var selection_area = BBox.From(new v4(m_grab_chart, 0, 1f), new v4(m_chart.ClientToChart(e.Location), 0f, 1f));
							m_chart.OnChartAreaSelect(new ChartAreaSelectEventArgs(selection_area));
						}
					}
					else if (m_chart.Options.NavigationMode == ENavMode.Scene3D)
					{
						// For 3D scenes, left mouse rotates
						m_chart.Scene.Window.MouseNavigate(e.Location, View3d.ENavOp.Rotate, true);
					}
				}

				// Remove the area selection graphic
				if (m_selection_graphic_added)
					m_chart.Scene.RemoveObject(m_chart.Tools.AreaSelect);

				m_chart.Cursor = Cursors.Default;
				m_chart.Invalidate();
			}
		}

		/// <summary>A mouse operation for zooming (Middle Button)</summary>
		public class MouseOpDefaultMButton :MouseOp
		{
			private HintBalloon m_tape_measure_balloon;
			private bool m_tape_measure_graphic_added;

			public MouseOpDefaultMButton(ChartControl chart) :base(chart)
			{
				m_tape_measure_balloon = new HintBalloon
				{
					AutoSizeMode = AutoSizeMode.GrowOnly,
					Font = new Font(FontFamily.GenericMonospace, 8f),
					Owner = chart.TopLevelControl as Form,
				};
				m_tape_measure_graphic_added = false;
			}
			public override void Dispose()
			{
				base.Dispose();
			}
			public override void MouseDown(MouseEventArgs e)
			{
				// If mouse down occurred within the chart, record it
				if (m_chart.ChartBounds.Contains(e.Location))
				{
					m_chart.Cursor = Cursors.CrossHair;
				}
			}
			public override void MouseMove(MouseEventArgs e)
			{
				// If we haven't dragged, treat it as a click instead (i.e. ignore till it's a drag operation)
				if (IsClick(e.Location) && !m_tape_measure_graphic_added)
					return;

				// Position the tape measure graphic
				var pt0 = m_chart.Camera.O2W * m_chart.ChartToCamera(m_grab_chart);
				var pt1 = m_chart.Camera.O2W * m_chart.ChartToCamera(m_chart.ClientToChart(e.Location));
				var delta = pt1 - pt0;
				m_chart.Tools.TapeMeasure.O2P = m4x4.OriFromDir(delta, AxisId.PosZ, pt0) * m4x4.Scale(1f, 1f, delta.Length3, v4.Origin);
				m_tape_measure_balloon.Location = m_chart.PointToScreen(e.Location);
				m_tape_measure_balloon.Text = Str.Build(
					"dX: {0}\r\n".Fmt(delta.x),
					"dY: {0}\r\n".Fmt(delta.y),
					"dZ: {0}\r\n".Fmt(delta.z),
					"Len: {0}\r\n".Fmt(delta.Length3));

				// Show the tape measure graphic (after the text has been initialised)
				if (!m_tape_measure_graphic_added)
				{
					m_chart.Scene.AddObject(m_chart.Tools.TapeMeasure);
					m_tape_measure_graphic_added = true;
					m_tape_measure_balloon.Visible = true;
				}

				m_chart.Invalidate();
				m_chart.Update();
			}
			public override void MouseUp(MouseEventArgs e)
			{
				Util.Dispose(ref m_suspend_scope);

				// If this is a single click...
				if (IsClick(e.Location))
				{
					// Pass the click event out to users first
					var args = new ChartClickedEventArgs(m_hit_result, e);
					m_chart.OnChartClicked(args);

					if (!args.Handled)
					{
						if (m_hit_result.Zone.HasFlag(HitTestResult.EZone.Chart))
						{ }
						else if (m_hit_result.Zone.HasFlag(HitTestResult.EZone.XAxis))
						{ }
						else if (m_hit_result.Zone.HasFlag(HitTestResult.EZone.YAxis))
						{ }
					}
				}
				// Otherwise this is a drag action
				else
				{
				}

				// Remove the tape measure graphic
				if (m_tape_measure_graphic_added)
				{
					m_chart.Scene.RemoveObject(m_chart.Tools.TapeMeasure);
					m_tape_measure_balloon.Visible = false;
				}

				m_chart.Cursor = Cursors.Default;
				m_chart.Invalidate();
			}
		}

		/// <summary>A mouse operation for dragging the chart around or right clicking (Right Button)</summary>
		public class MouseOpDefaultRButton :MouseOp
		{
			/// <summary>The allowed motion based on where the chart was grabbed</summary>
			private EAxis m_drag_axis_allow;

			public MouseOpDefaultRButton(ChartControl chart)
				:base(chart)
			{}
			public override void MouseDown(MouseEventArgs e)
			{
				if (m_chart.ChartBounds.Contains(e.Location)) m_drag_axis_allow = EAxis.Both;
				if (m_chart.XAxisBounds.Contains(e.Location)) m_drag_axis_allow = EAxis.XAxis;
				if (m_chart.YAxisBounds.Contains(e.Location)) m_drag_axis_allow = EAxis.YAxis;

				// Right mouse translates for 2D and 3D scene
				var loc = Control_.MapPoint(m_chart, m_chart.Scene, e.Location);
				m_chart.Scene.Window.MouseNavigate(loc, View3d.ENavOp.Translate, true); 
			}
			public override void MouseMove(MouseEventArgs e)
			{
				// If we haven't dragged, treat it as a click instead (i.e. ignore till it's a drag operation)
				if (IsClick(e.Location))
					return;

				// Change the cursor once dragging
				m_chart.Cursor = Cursors.SizeAll;

				// Limit the drag direction
				var drop_loc = Control_.MapPoint(m_chart, m_chart.Scene, e.Location);
				var grab_loc = Control_.MapPoint(m_chart, m_chart.Scene, m_grab_client);
				if (!m_drag_axis_allow.HasFlag(EAxis.XAxis) || m_chart.XAxis.LockRange) drop_loc.X = grab_loc.X;
				if (!m_drag_axis_allow.HasFlag(EAxis.YAxis) || m_chart.YAxis.LockRange) drop_loc.Y = grab_loc.Y;

				m_chart.Scene.Window.MouseNavigate(drop_loc, View3d.ENavOp.Translate, false);
				m_chart.SetRangeFromCamera();
				m_chart.Invalidate();
				m_chart.Update();
			}
			public override void MouseUp(MouseEventArgs e)
			{
				m_chart.Cursor = Cursors.Default;

				// If we haven't dragged, treat it as a click instead
				if (IsClick(e.Location))
				{
					var args = new ChartClickedEventArgs(m_hit_result, e);
					m_chart.OnChartClicked(args);

					if (!args.Handled)
					{
						// Show the context menu on right click
						if (args.Button == MouseButtons.Right)
						{
							if (m_hit_result.Zone.HasFlag(HitTestResult.EZone.Chart))
								m_chart.ShowContextMenu(args.Location, args.HitResult);
							else if (m_hit_result.Zone.HasFlag(HitTestResult.EZone.XAxis))
								m_chart.XAxis.ShowContextMenu(args.Location, args.HitResult);
							else if (m_hit_result.Zone.HasFlag(HitTestResult.EZone.YAxis))
								m_chart.YAxis.ShowContextMenu(args.Location, args.HitResult);
						}
					}
					m_chart.Invalidate();
				}
				else
				{
					// Limit the drag direction
					var drop_loc = Control_.MapPoint(m_chart, m_chart.Scene, e.Location);
					var grab_loc = Control_.MapPoint(m_chart, m_chart.Scene, m_grab_client);
					if (!m_drag_axis_allow.HasFlag(EAxis.XAxis) || m_chart.XAxis.LockRange) drop_loc.X = grab_loc.X;
					if (!m_drag_axis_allow.HasFlag(EAxis.YAxis) || m_chart.YAxis.LockRange) drop_loc.Y = grab_loc.Y;

					m_chart.Scene.Window.MouseNavigate(drop_loc, View3d.ENavOp.None, true);
					m_chart.SetRangeFromCamera();
					m_chart.Invalidate();
				}
			}
		}

		#endregion

		#region Elements

		/// <summary>Base class for anything on a chart</summary>
		[DebuggerDisplay("{Name} {Id} {GetType().Name}")]
		public abstract class Element :INotifyPropertyChanged ,IDisposable
		{
			/// <summary>Buffers for creating the chart graphics</summary>
			protected List<View3d.Vertex> m_vbuf;
			protected List<ushort>        m_ibuf;
			protected List<View3d.Nugget> m_nbuf;

			protected Element(Guid id, m4x4 position, string name)
			{
				m_vbuf = new List<View3d.Vertex>();
				m_ibuf = new List<ushort>();
				m_nbuf = new List<View3d.Nugget>();

				Id                           = id;
				Name                         = name;
				m_impl_chart                 = null;
				m_impl_position              = position;
				m_impl_bounds                = new BBox(position.pos, v4.Zero);
				m_impl_selected              = false;
				m_impl_hovered               = false;
				m_impl_visible               = true;
				m_impl_visible_to_find_range = true;
				m_impl_enabled               = true;
				m_invalidated                = true;
				UserData                     = new Dictionary<Guid, object>();
			}
			protected Element(XElement node)
				:this(Guid.Empty, m4x4.Identity, string.Empty)
			{
				// Note: Bounds, size, etc are not stored, the implementation
				// of the element provides those (typically in UpdateGfx)
				Id       = node.Element(nameof(Id      )).As(Id);
				Position = node.Element(nameof(Position)).As(Position);
				Name     = node.Element(nameof(Name    )).As(Name);
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
					Debug.Assert(!m_impl_chart.Elements.Contains(this), "Element already in the Chart's Elements collection");
					m_impl_chart.Elements.Add(this);
				}

				Debug.Assert(CheckConsistency());
				Invalidate();
			}

			/// <summary>Add or remove this element from 'chart'</summary>
			protected virtual void SetChartCore(ChartControl chart)
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

			/// <summary>Debugging name for the element</summary>
			public string Name { get; set; }

			/// <summary>Export to XML</summary>
			public virtual XElement ToXml(XElement node)
			{
				node.Add2(nameof(Id      ), Id      , false);
				node.Add2(nameof(Position), Position, false);
				node.Add2(nameof(Name    ), Name    , false);
				return node;
			}

			/// <summary>Import from XML. Used to update the state of this element without having to delete/recreate it</summary>
			protected virtual void FromXml(XElement node)
			{
				Position = node.Element(nameof(Position)).As(Position);
				Name     = node.Element(nameof(Name    )).As(Name);
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

			/// <summary>Raised whenever a property of this Element changes</summary>
			public event PropertyChangedEventHandler PropertyChanged;
			protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
			{
				PropertyChanged.Raise(this, args);
			}
			protected void SetProp<T>(ref T prop, T value, string name, bool invalidate_graphics, bool invalidate_chart)
			{
				if (Equals(prop, value)) return;
				prop = value;
				OnPropertyChanged(new PropertyChangedEventArgs(name));
				if (invalidate_graphics) Invalidate();
				if (invalidate_chart) InvalidateChart();
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

			/// <summary>Raised whenever the element is hovered over with the mouse</summary>
			public event EventHandler HoveredChanged;
			protected virtual void OnHoveredChanged()
			{
				HoveredChanged.Raise(this);
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

				SetProp(ref m_impl_selected, selected, nameof(Selected), true, true);

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

			/// <summary>Get/Set the hovered state</summary>
			public virtual bool Hovered
			{
				get { return m_impl_hovered; }
				set { SetHoveredInternal(value, true); }
			}
			private bool m_impl_hovered;

			/// <summary>Set the selected state of this element</summary>
			internal void SetHoveredInternal(bool hovered, bool update_hovered_collection)
			{
				// This allows the chart to set the hovered state without
				// adding/removing from the chart's 'Hovered' collection.
				if (m_impl_hovered == hovered) return;
				if (m_impl_hovered && update_hovered_collection)
				{
					Chart?.Hovered.Remove(this);
				}

				SetProp(ref m_impl_hovered, hovered, nameof(Hovered), true, true);

				if (m_impl_hovered && update_hovered_collection)
				{
					// Hovered state is changed by assigning to this property or by
					// addition/removal from the chart's 'Hovered' collection.
					Chart?.Hovered.Add(this);
				}

				// Notify observers about the hovered change
				OnHoveredChanged();
				Invalidate();
			}

			/// <summary>Get/Set whether this element is visible in the chart</summary>
			public bool Visible
			{
				get { return m_impl_visible; }
				set { SetProp(ref m_impl_visible, value, nameof(Visible), false, true); } // Make an element visible/invisible, doesn't invalidate it, only the chart that is displaying it.
			}
			private bool m_impl_visible;

			/// <summary>Get/Set whether this element is enabled</summary>
			public bool Enabled
			{
				get { return m_impl_enabled; }
				set { SetProp(ref m_impl_enabled, value, nameof(Enabled), true, true); } // Changing the enabled data of an element should result in the graphics changing in some way that implies 'Enabled/Disabled'
			}
			private bool m_impl_enabled;

			/// <summary>True if this element is included when finding the range of data in the chart</summary>
			public bool VisibleToFindRange
			{
				get { return m_impl_visible_to_find_range; }
				set { SetProp(ref m_impl_visible_to_find_range, value, nameof(VisibleToFindRange), false, true); } // Make an element visible/invisible, doesn't invalidate it, only the chart that is displaying it.
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
				OnPropertyChanged(new PropertyChangedEventArgs(nameof(Position)));
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
				protected set { SetProp(ref m_impl_bounds, value, nameof(Bounds), false, true); } // Doesn't invalidate graphics because bounds is usually set after graphics have been created
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

			/// <summary>Perform a hit test on this object. Returns null for no hit. 'point' is in client space because typically hit testing uses pixel tolerances</summary>
			public virtual HitTestResult.Hit HitTest(Point client_point, Keys modifier_keys, View3d.CameraControls cam)
			{
				return null;
			}

			/// <summary>Handle a click event on this element</summary>
			public virtual void HandleClicked(ChartClickedEventArgs args)
			{}

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
				{
					UpdateGfxCore();
					m_invalidated = false;
				}
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
				throw new ArgumentException("element belongs to another chart");

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

		/// <summary>Handle elements added/removed from the hovered list</summary>
		protected virtual void OnHoveredListChanging(ListChgEventArgs<Element> args)
		{
			var elem = args.Item;
			if (elem != null && (elem.Chart != null && elem.Chart != this))
				throw new ArgumentException("element belongs to another chart");

			switch (args.ChangeType)
			{
			case ListChg.Reset:
				{
					Elements.ForEach(x => x.SetHoveredInternal(false, false));
					Hovered .ForEach(x => x.SetHoveredInternal(true, false));
					Debug.Assert(CheckConsistency());
					Invalidate();
					break;
				}
			case ListChg.ItemAdded:
				{
					elem.SetHoveredInternal(true, false);
					Debug.Assert(CheckConsistency());
					Invalidate();
					break;
				}
			case ListChg.ItemRemoved:
				{
					elem.SetHoveredInternal(false, false);
					Debug.Assert(CheckConsistency());
					Invalidate();
					break;
				}
			}
		}

		/// <summary>Move the selected elements by 'delta'</summary>
		private void DragSelected(v2 delta, bool commit)
		{
			if (!AllowEditing) return;
			foreach (var elem in Selected)
				elem.Drag(delta, commit);
		}

		/// <summary>
		/// Select elements that are wholly within 'rect'. (rect is in chart space)
		/// If no modifier keys are down, elements not in 'rect' are deselected.
		/// If 'shift' is down, elements within 'rect' are selected in addition to the existing selection
		/// If 'ctrl' is down, elements within 'rect' are removed from the existing selection.</summary>
		public void SelectElements(RectangleF rect, Keys modifier_keys)
		{
			if (!AllowSelection)
				return;

			// Normalise the selection
			var r = new BBox(new v4(rect.Centre(), 0f, 1f), new v4(v2.Abs(v2.From(rect.Size))*0.5f, 1f, 0f));

			// If the area of selection is less than the min drag distance, assume click selection
			var is_click = r.DiametreSq < Maths.Sqr(Options.MinDragPixelDistance);
			if (is_click)
			{
				var hits = HitTestCS(ChartToClient(rect.Location), modifier_keys, x => x.Enabled);

				// If control is down, deselect the first selected element in the hit list
				if (Bit.AllSet((int)modifier_keys, (int)Keys.Control))
				{
					var first = hits.Hits.FirstOrDefault(x => x.Element.Selected);
					if (first != null)
						first.Element.Selected = false;
				}
				// If shift is down, select the first element not already selected in the hit list
				else if (Bit.AllSet((int)modifier_keys, (int)Keys.Shift))
				{
					var first = hits.Hits.FirstOrDefault(x => !x.Element.Selected);
					if (first != null)
						first.Element.Selected = true;
				}
				// Otherwise select the next element below the current selection or the top
				// element if nothing is selected. Clear all other selections
				else
				{
					// Find the element to select
					var to_select = hits.Hits.FirstOrDefault();
					for (int i = 0; i < hits.Hits.Count - 1; ++i)
					{
						if (!hits.Hits[i].Element.Selected) continue;
						to_select = hits.Hits[i+1];
						break;
					}

					// Deselect all elements and select the next element
					Selected.Clear();
					if (to_select != null)
						to_select.Element.Selected = true;
				}
			}
			// Otherwise it's area selection
			else
			{
				using (Selected.SuspendEvents(true))
				{
					// If control is down, those in the selection area become deselected
					if (Bit.AllSet((int)modifier_keys, (int)Keys.Control))
					{
						// Only need to look in the selected list
						foreach (var elem in Selected.Where(x => r.IsWithin(x.Bounds, 0f)).ToArray())
							elem.Selected = false;
					}
					// If shift is down, those in the selection area become selected
					else if (Bit.AllSet((int)modifier_keys, (int)Keys.Shift))
					{
						foreach (var elem in Elements.Where(x => !x.Selected && r.IsWithin(x.Bounds, 0f)))
							elem.Selected = true;
					}
					// Otherwise, the existing selection is cleared, and those within the selection area become selected
					else
					{
						foreach (var elem in Elements)
							elem.Selected = r.IsWithin(elem.Bounds, 0f);
					}
				}
			}

			Invalidate();
		}

		#endregion

		#region Context Menu

		/// <summary>Create and display a context menu</summary>
		public void ShowContextMenu(Point location, HitTestResult hit_result)
		{
			var cmenu = new ContextMenuStrip { Renderer = new ContextMenuRenderer() };

			using (this.ChangeCursor(Cursors.WaitCursor))
			using (cmenu.SuspendLayout(true))
			{
				#region Objects
				{
					var objects_menu = cmenu.Items.Add2(new ToolStripMenuItem("Objects") { Name = CMenu.Objects });
					{
						var opt = objects_menu.DropDownItems.Add2(new ToolStripMenuItem("Origin") { Name = CMenu.ObjectsMenu.Origin });
						opt.Checked = Scene.Window.OriginPointVisible;
						opt.Click += (s,a) =>
						{
							Scene.Window.OriginPointVisible = !Scene.Window.OriginPointVisible;
							Invalidate();
						};
					}
					{
						var opt = objects_menu.DropDownItems.Add2(new ToolStripMenuItem("Focus") { Name = CMenu.ObjectsMenu.Focus });
						opt.Checked = Scene.Window.FocusPointVisible;
						opt.Click += (s,a) =>
						{
							Scene.Window.FocusPointVisible = !Scene.Window.FocusPointVisible;
							Invalidate();
						};
					}
					{
						var opt = objects_menu.DropDownItems.Add2(new ToolStripMenuItem("Grid Lines") { Name = CMenu.ObjectsMenu.GridLines });
						opt.Checked = Options.ShowGridLines;
						opt.Click += (s,a) =>
						{
							Options.ShowGridLines = !Options.ShowGridLines;
							Invalidate();
						};
					}
					{
						var opt = objects_menu.DropDownItems.Add2(new ToolStripMenuItem("Axes") { Name = CMenu.ObjectsMenu.GridLines });
						opt.Checked = Options.ShowAxes;
						opt.Click += (s,a) =>
						{
							Options.ShowAxes = !Options.ShowAxes;
							Invalidate();
						};
					}
				}
				#endregion
				#region Tools
				{
					var tools_menu = cmenu.Items.Add2(new ToolStripMenuItem("Tools") { Name = CMenu.Tools });
					#region Show Value
					{
						var opt = tools_menu.DropDownItems.Add2(new ToolStripMenuItem("Show Value") { Name = CMenu.ToolsMenu.ShowValue });
						opt.Checked = m_show_value;
						opt.Click += (s, a) =>
						{
							if (m_show_value)
							{
								MouseMove -= OnMouseMoveTooltip;
								MouseWheel -= OnMouseWheelTooltip;
							}
							m_show_value = !m_show_value;
							m_tt_show_value.Size = m_tt_show_value.GetPreferredSize(Size.Empty);
							m_tt_show_value.Visible = m_show_value;
							if (m_show_value)
							{
								MouseMove += OnMouseMoveTooltip;
								MouseWheel += OnMouseWheelTooltip;
							}
						};
					}
					#endregion
					#region Show Cross Hair
					{
						var opt = tools_menu.DropDownItems.Add2(new ToolStripMenuItem("Show Cross Hair") { Name = CMenu.ToolsMenu.ShowXHair });
						opt.Checked = CrossHairVisible;
						opt.Click += (s, a) =>
						{
							CrossHairVisible = !CrossHairVisible;
							Invalidate();
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
							AutoRange();
						};
					}
					{
						var opt = zoom_menu.DropDownItems.Add2(new ToolStripMenuItem("Aspect 1:1") { Name = CMenu.ZoomMenu.Aspect1to1 });
						opt.Click += (s,a) =>
						{
							Aspect = 1.0f;
						};
					}
					{
						var opt = zoom_menu.DropDownItems.Add2(new ToolStripMenuItem("Lock Aspect") { Name = CMenu.ZoomMenu.LockAspect });
						opt.Checked = Options.LockAspect != null;
						opt.Click += (s,a) =>
						{
							LockAspect = !LockAspect;
						};
					}
					{
						var opt = zoom_menu.DropDownItems.Add2(new ToolStripMenuItem("Perpendicular Z Translation") { Name = CMenu.ZoomMenu.PerpZTrans });
						opt.Checked = Options.PerpendicularZTranslation;
						opt.Click += (s,a) =>
						{
							Options.PerpendicularZTranslation = !Options.PerpendicularZTranslation;
						};
					}
				}
				#endregion
				#region Rendering
				{
					var rendering_menu = cmenu.Items.Add2(new ToolStripMenuItem("Rendering") { Name = CMenu.Rendering });
					{
						var opt = rendering_menu.DropDownItems.Add2(new ToolStripComboBox("Navigation Mode") { Name = CMenu.RenderingMenu.NavigationMode });
						opt.DropDownStyle = ComboBoxStyle.DropDownList;
						opt.ComboBox.DataSource = Enum<ENavMode>.ValuesArray;
						opt.SelectedItem = Options.NavigationMode;
						opt.ComboBox.Format += (s,a) =>
						{
							a.Value = ((ENavMode)a.ListItem).Desc();
						};
						opt.SelectedIndexChanged += (s,a) =>
						{
							Options.NavigationMode = (ENavMode)opt.SelectedItem;
							Invalidate();
						};
					}
					{
						var opt = rendering_menu.DropDownItems.Add2(new ToolStripMenuItem("Orthographic") { Name = CMenu.RenderingMenu.Orthographic });
						opt.Checked = Options.Orthographic;
						opt.Click += (s,a) =>
						{
							Options.Orthographic = !Options.Orthographic;
							Invalidate();
						};
					}
					{
						var fillmode = Enum<View3d.EFillMode>.Cycle(Scene.Window.FillMode);
						var opt = rendering_menu.DropDownItems.Add2(new ToolStripMenuItem(fillmode.ToString()) { Name = CMenu.RenderingMenu.Wireframe });
						opt.Click += (s,a) =>
						{
							Options.FillMode = fillmode;
							Invalidate();
						};
					}
					{
						var opt = rendering_menu.DropDownItems.Add2(new ToolStripMenuItem("Anti-Aliasing") { Name = CMenu.RenderingMenu.AntiAliasing });
						opt.Checked = Options.AntiAliasing;
						opt.Click += (s,a) =>
						{
							Options.AntiAliasing = !Options.AntiAliasing;
							Scene.Window.MultiSampling = Options.AntiAliasing ? 4 : 1;
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
				OnAddUserMenuOptions(new AddUserMenuOptionsEventArgs(AddUserMenuOptionsEventArgs.EType.Chart, cmenu, hit_result));
			}
			cmenu.Items.TidySeparators();
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
		private HintBalloon m_tt_show_value;
		private bool m_show_value;

		/// <summary>Handle mouse move events while the tooltip is visible</summary>
		private void OnMouseMoveTooltip(object sender, MouseEventArgs e)
		{
			SetValueToolTip(e.Location);
		}
		private void OnMouseWheelTooltip(object sender, MouseEventArgs e)
		{
			SetValueToolTip(e.Location);
		}

		/// <summary>Update the text in the 'show value' hint balloon. 'location' is in client space</summary>
		private void SetValueToolTip(Point location)
		{
			if (Scene.Bounds.Contains(location))
			{
				m_tt_show_value.Text = LocationText(location);
				m_tt_show_value.Location = PointToScreen(location);
				m_tt_show_value.Owner = TopLevelControl as Form;
				m_tt_show_value.Visible = true;
			}
			else
			{
				m_tt_show_value.Visible = false;
			}
		}

		#endregion

		#region Show Cross Hair

		/// <summary>True while the cross hair is visible</summary>
		public bool CrossHairVisible
		{
			get { return m_cross_hair_visible; }
			set
			{
				if (m_cross_hair_visible == value) return;
				if (m_cross_hair_visible)
				{
					MouseMove -= OnMouseMoveCrossHair;
					MouseWheel -= OnMouseWheelCrossHair;
					Scene.RemoveObject(m_tools.CrossHairH);
					Scene.RemoveObject(m_tools.CrossHairV);
				}
				m_cross_hair_visible = value;
				if (m_cross_hair_visible)
				{
					Scene.AddObject(m_tools.CrossHairH);
					Scene.AddObject(m_tools.CrossHairV);
					MouseWheel += OnMouseWheelCrossHair;
					MouseMove += OnMouseMoveCrossHair;
				}
				Invalidate();
			}
		}
		private bool m_cross_hair_visible;

		/// <summary>The chart-space location of the cross hair</summary>
		public PointF CrossHairLocation
		{
			get { return m_cross_hair_location; }
			set
			{
				if (m_cross_hair_location == value) return;
				m_cross_hair_location = value;

				// Scale to fill the view
				var view = Camera.ViewArea(Camera.FocusDist);
				var pt_cs = ChartToCamera(m_cross_hair_location);
				if (m_tools.CrossHairH != null)
				{
					// Shift the position to the centre of the camera view
					var o2w = new m4x4(Camera.O2W.rot, Camera.O2W * new v4(0, pt_cs.y, pt_cs.z, 1f));
					m_tools.CrossHairH.O2P = o2w * m3x4.Scale(view.x, 1f, 1f).m4x4;
				}
				if (m_tools.CrossHairV != null)
				{
					// Shift the position to the centre of the camera view
					var o2w = new m4x4(Camera.O2W.rot, Camera.O2W * new v4(pt_cs.x, 0, pt_cs.z, 1f));
					m_tools.CrossHairV.O2P = o2w * m3x4.Scale(1f, view.y, 1f).m4x4;
				}

				// Notify of the new cross hair location
				CrossHairMoved.Raise(this);
				Invalidate();
				Update();
			}
		}
		private PointF m_cross_hair_location;

		/// <summary>Update the cross hair on mouse move</summary>
		private void OnMouseMoveCrossHair(object sender, MouseEventArgs e)
		{
			if (Scene.Bounds.Contains(e.Location))
			{
				CrossHairLocation = ClientToChart(e.Location);
				Invalidate();
			}
		}
		private void OnMouseWheelCrossHair(object sender, MouseEventArgs e)
		{
			if (Scene.Bounds.Contains(e.Location))
			{
				CrossHairLocation = ClientToChart(e.Location);
				Invalidate();
			}
		}

		/// <summary>Raised when the cross hair is visible and moved</summary>
		public event EventHandler CrossHairMoved;
		#endregion

		#region Tools

		/// <summary>Chart graphics</summary>
		private ChartTools Tools
		{
			[DebuggerStepThrough] get { return m_tools; }
			set
			{
				if (m_tools == value) return;
				Util.Dispose(ref m_tools);
				m_tools = value;
			}
		}
		private ChartTools m_tools;

		/// <summary>A collection of graphics used by the chart itself</summary>
		public class ChartTools :IDisposable
		{
			public static readonly Guid Id = new Guid("62D495BB-36D1-4B52-A067-1B7DB4011831");

			public ChartTools(RdrOptions opts)
			{
				if (Util.IsInDesignMode)
					return;

				Options     = opts;
				AreaSelect  = CreateAreaSelect();
				Resizer     = Util.NewArray(8, i => new ResizeGrabber(i));
				CrossHairH  = CreateCrossHair(true);
				CrossHairV  = CreateCrossHair(false);
				TapeMeasure = CreateTapeMeasure();
			}
			public void Dispose()
			{
				Options     = null;
				AreaSelect  = null;
				Resizer     = null;
				CrossHairH  = null;
				CrossHairV  = null;
				TapeMeasure = null;
			}

			/// <summary>Chart options</summary>
			private RdrOptions Options
			{
				[DebuggerStepThrough] get { return m_options; }
				set
				{
					if (m_options == value) return;
					if (m_options != null) m_options.PropertyChanged -= HandleOptionsChanged;
					m_options = value;
					if (m_options != null) m_options.PropertyChanged += HandleOptionsChanged;
				}
			}
			private RdrOptions m_options;

			/// <summary>Graphic for area selection</summary>
			public View3d.Object AreaSelect
			{
				get { return m_area_select; }
				private set
				{
					if (m_area_select == value) return;
					Util.Dispose(ref m_area_select);
					m_area_select = value;
				}
			}
			private View3d.Object m_area_select;
			private View3d.Object CreateAreaSelect()
			{
				var ldr = Ldr.Rect("selection", Options.SelectionColour, AxisId.PosZ, 1f, 1f, true, pos:v4.Origin);
				return new View3d.Object(ldr, false, Id, null);
			}

			/// <summary>Graphics for the resizing grab zones</summary>
			public ResizeGrabber[] Resizer
			{
				get { return m_resizer; }
				private set
				{
					if (m_resizer == value) return;
					Util.DisposeAll(m_resizer);
					m_resizer = value;
				}
			}
			private ResizeGrabber[] m_resizer;
			public class ResizeGrabber :View3d.Object
			{
				public ResizeGrabber(int corner) :base("*Box resizer_{0} {{5}}".Fmt(corner), false, Id, null)
				{
					switch (corner)
					{
					case 0:
						Cursor = Cursors.SizeNESW;
						Direction = v2.Normalise2(new v2(-1,-1));
						Update = (b,z) => O2P = m4x4.Translation(b.Lower.x, b.Lower.y, z);
						break;
					case 1:
						Cursor = Cursors.SizeNESW;
						Direction = v2.Normalise2(new v2(+1,+1));
						Update = (b,z) => O2P = m4x4.Translation(b.Upper.x, b.Upper.y, z);
						break;
					case 2:
						Cursor = Cursors.SizeNWSE;
						Direction = v2.Normalise2(new v2(-1,+1));
						Update = (b,z) => O2P = m4x4.Translation(b.Lower.x, b.Upper.y, z);
						break;
					case 3:
						Cursor = Cursors.SizeNWSE;
						Direction = v2.Normalise2(new v2(+1,-1));
						Update = (b,z) => O2P = m4x4.Translation(b.Upper.x, b.Lower.y, z);
						break;
					case 4:
						Direction = new v2(-1,0);
						Cursor = Cursors.SizeWE;
						Update = (b,z) => O2P = m4x4.Translation(b.Lower.x, b.Centre.y, z);
						break;
					case 5:
						Direction = new v2(+1,0);
						Cursor = Cursors.SizeWE;
						Update = (b,z) => O2P = m4x4.Translation(b.Upper.x, b.Centre.y, z);
						break;
					case 6:
						Direction = new v2(0,-1);
						Cursor = Cursors.SizeNS;
						Update = (b,z) => O2P = m4x4.Translation(b.Centre.x, b.Lower.y, z);
						break;
					case 7:
						Direction = new v2(0,+1);
						Cursor = Cursors.SizeNS;
						Update = (b,z) => O2P = m4x4.Translation(b.Centre.x, b.Upper.y, z);
						break;
					}
				}

				/// <summary>The direction that this grabber can resize in </summary>
				public v2 Direction { get; private set; }

				/// <summary>The cursor to display when this grabber is used</summary>
				public Cursor Cursor { get; set; }

				/// <summary>Updates the position of the grabber</summary>
				public Action<BRect,float> Update;
			}

			/// <summary>A vertical and horizontal line</summary>
			public View3d.Object CrossHairH
			{
				get { return m_cross_hair_h; }
				private set
				{
					if (m_cross_hair_h == value) return;
					Util.Dispose(ref m_cross_hair_h);
					m_cross_hair_h = value;
				}
			}
			public View3d.Object CrossHairV
			{
				get { return m_cross_hair_v; }
				private set
				{
					if (m_cross_hair_v == value) return;
					Util.Dispose(ref m_cross_hair_v);
					m_cross_hair_v = value;
				}
			}
			private View3d.Object m_cross_hair_h;
			private View3d.Object m_cross_hair_v;
			private View3d.Object CreateCrossHair(bool horiz)
			{
				var col = Options.ChartBkColour.ToV4().Length3 > 0.5 ? 0xFFFFFFFF : 0xFF000000;
				var str = horiz
					? Ldr.Line("chart_cross_hair_h", col, new v4(-0.5f, 0, 0, 1f), new v4(+0.5f, 0, 0, 1f))
					: Ldr.Line("chart_cross_hair_v", col, new v4(0, -0.5f, 0, 1f), new v4(0, +0.5f, 0, 1f));
				return new View3d.Object(str, false, Id, null);
			}

			/// <summary>A line for measuring distances</summary>
			public View3d.Object TapeMeasure
			{
				get { return m_tape_measure; }
				private set
				{
					if (m_tape_measure == value) return;
					Util.Dispose(ref m_tape_measure);
					m_tape_measure = value;
				}
			}
			public View3d.Object m_tape_measure;
			private View3d.Object CreateTapeMeasure()
			{
				var col = Options.ChartBkColour.ToV4().Length3 > 0.5 ? 0xFFFFFFFF : 0xFF000000;
				var str = Ldr.Line("tape_measure", col, new v4(0, 0, 0, 1f), new v4(0, 0, 1f, 1f));
				return new View3d.Object(str, false, Id, null);
			}

			/// <summary>Update the chart tools when the options change</summary>
			private void HandleOptionsChanged(object sender, PropertyChangedEventArgs e)
			{
				if (e.PropertyName == nameof(RdrOptions.SelectionColour))
				{
					AreaSelect.ColourSet(Options.SelectionColour);
				}
				if (e.PropertyName == nameof(RdrOptions.ChartBkColour))
				{
					CrossHairH = CreateCrossHair(true);
					CrossHairV = CreateCrossHair(false);
				}
			}
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

		/// <summary>Flags for how a chart was scrolled</summary>
		[Flags] public enum EMoveType
		{
			None = 0,
			XZoomed   = 1 << 0,
			YZoomed   = 1 << 1,
			XScrolled = 1 << 2,
			YScrolled = 1 << 3,
		}

		/// <summary>Results collection for a hit test</summary>
		public class HitTestResult
		{
			public HitTestResult(EZone zone, Point client_point, PointF chart_point, Keys modifier_keys, IEnumerable<Hit> hits, View3d.CameraControls cam)
			{
				Zone         = zone;
				ClientPoint  = client_point;
				ChartPoint   = chart_point;
				Hits         = hits.ToList();
				ModifierKeys = modifier_keys;
				Camera       = cam;
			}

			/// <summary>The zone on the chart that was hit</summary>
			public EZone Zone { get; private set; }
			[Flags] public enum EZone
			{
				None,
				Chart = 1 << 0,
				XAxis = 1 << 1,
				YAxis = 1 << 2,
				Title = 1 << 3,
			}

			/// <summary>The client space location of where the hit test was performed</summary>
			public Point ClientPoint { get; private set; }

			/// <summary>The chart space location of where the hit test was performed (EZone.Chart only)</summary>
			public PointF ChartPoint { get; private set; }

			/// <summary>The collection of hit objects</summary>
			public List<Hit> Hits { get; private set; }

			/// <summary>Keys held down during the hit test</summary>
			public Keys ModifierKeys { get; private set; }

			/// <summary>The camera position when the hit test was performed (needed for chart to screen space conversion)</summary>
			public View3d.CameraControls Camera { get; private set; }

			public class Hit
			{
				public Hit(Element elem, PointF elem_point, object context)
				{
					Element  = elem;
					Point    = elem_point;
					Location = elem.Position;
					Context  = context;
				}

				/// <summary>The element that was hit</summary>
				public Element Element { get; private set; }

				/// <summary>Where on the element it was hit (in element space)</summary>
				public PointF Point { get; private set; }

				/// <summary>The element's chart location at the time it was hit</summary>
				public m4x4 Location { get; private set; }

				/// <summary>Optional context information collected during the hit test</summary>
				public object Context { get; private set; }

				public override string ToString()
				{
					return Element.ToString();
				}
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

		/// <summary>Cursors for the chart</summary>
		public static class Cursors
		{
			public static readonly Cursor Default    = System.Windows.Forms.Cursors.Default;
			public static readonly Cursor WaitCursor = System.Windows.Forms.Cursors.WaitCursor;
			public static readonly Cursor SizeWE     = System.Windows.Forms.Cursors.SizeWE;
			public static readonly Cursor SizeNS     = System.Windows.Forms.Cursors.SizeNS;
			public static readonly Cursor SizeNESW   = System.Windows.Forms.Cursors.SizeNESW;
			public static readonly Cursor SizeNWSE   = System.Windows.Forms.Cursors.SizeNWSE;
			public static readonly Cursor SizeAll    = System.Windows.Forms.Cursors.SizeAll;
			public static readonly Cursor Arrow      = Resources.cursor_arrow.ToCursor(Point.Empty);
			public static readonly Cursor ArrowPlus  = Resources.cursor_arrow_plus.ToCursor(Point.Empty);
			public static readonly Cursor ArrowMinus = Resources.cursor_arrow_minus.ToCursor(Point.Empty);
			public static readonly Cursor CrossHair  = System.Windows.Forms.Cursors.Cross;
		}

		///// <summary>String constants used in XML export/import</summary>
		//public static class XmlTag
		//{
		//	public const string Root                 = "root";
		//	public const string TypeAttribute        = "ty";
		//	public const string Element              = "elem";
		//	public const string Id                   = "id";
		//	public const string Name                 = "name";
		//	public const string Position             = "pos";
		//	public const string XAxis                = "xaxis";
		//	public const string YAxis                = "yaxis";
		//	public const string BkColour             = "bk_colour";
		//	public const string ChartBkColour        = "chart_bk_colour";
		//	public const string TitleColour          = "title_colour";
		//	public const string TitleTransform       = "title_xform";
		//	public const string Margin               = "margin";
		//	public const string TitleFont            = "title_font";
		//	public const string NoteFont             = "note_font";
		//	public const string SelectionColour      = "selection_colour";
		//	public const string ShowGridLines        = "show_grid_lines";
		//	public const string AntiAliasing         = "antialiasing";
		//	public const string MinSelectionDistance = "min_selection_distance";
		//	public const string MinDragPixelDistance = "min_drag_pixel_distance";
		//	public const string AxisColour           = "axis_colour";
		//	public const string LabelColour          = "label_colour";
		//	public const string GridColour           = "grid_colour";
		//	public const string TickColour           = "tick_colour";
		//	public const string LabelFont            = "label_font";
		//	public const string TickFont             = "tick_font";
		//	public const string DrawTickMarks        = "draw_tick_marks";
		//	public const string DrawTickLabels       = "draw_tick_labels";
		//	public const string TickLength           = "tick_length";
		//	public const string MinTickSize          = "min_tick_size";
		//	public const string LabelTransform       = "label_xform";
		//	public const string AxisThickness        = "axis_thickness";
		//	public const string PixelsPerTick        = "pixels_per_tick";
		//}

		/// <summary>Names for context menu items to allow users to identify them</summary>
		public static class CMenu
		{
			public const string Tools = "tools";
			public static class ToolsMenu
			{
				public const string ShowValue = "show_value";
				public const string ShowXHair = "show_cross_hair";
			}
			public const string Zoom = "zoom";
			public static class ZoomMenu
			{
				public const string Default = "default";
				public const string Aspect1to1 = "aspect1:1";
				public const string LockAspect = "lock_aspect";
				public const string PerpZTrans = "perp_z_trans";
			}
			public const string Objects = "objects";
			public static class ObjectsMenu
			{
				public const string Origin = "origin";
				public const string Focus = "focus";
				public const string GridLines = "grid_lines";
				public const string Axes = "axes";
			}
			public const string Rendering = "rendering";
			public static class RenderingMenu
			{
				public const string NavigationMode = "nav_mode";
				public const string Orthographic = "orthographic";
				public const string Wireframe = "wireframe";
				public const string AntiAliasing = "anti_aliasing";
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

		/// <summary>Event args for the ChartMoved event</summary>
		public class ChartMovedEventArgs :EventArgs
		{
			public ChartMovedEventArgs(EMoveType move_type)
			{
				MoveType = move_type;
			}

			/// <summary>How the chart was moved</summary>
			public EMoveType MoveType { get; internal set; }
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

		/// <summary>Event args for the ChartClicked event</summary>
		public class ChartClickedEventArgs :MouseEventArgs
		{
			public ChartClickedEventArgs(HitTestResult hits, MouseEventArgs e)
				:base(e.Button, e.Clicks, e.X, e.Y, e.Delta)
			{
				HitResult = hits;
				Handled = false;
			}

			/// <summary>Results of a hit test performed at the click location</summary>
			public HitTestResult HitResult { get; private set; }

			/// <summary>Set to true to suppress default chart click behaviour</summary>
			public bool Handled { get; set; }
		}

		/// <summary>Event args for area selection</summary>
		public class ChartAreaSelectEventArgs :EventArgs
		{
			public ChartAreaSelectEventArgs(BBox selection_area)
			{
				SelectionArea = selection_area;
				Handled = false;
			}

			/// <summary>The area (actually volume if you include Z) of the selection</summary>
			public BBox SelectionArea { get; private set; }

			/// <summary>Set to true to suppress default chart click behaviour</summary>
			public bool Handled { get; set; }
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

			/// <summary>The axis being ranged</summary>
			public EAxis Axis { get; private set; }

			/// <summary>The XAxis range calculated from known chart elements data</summary>
			public RangeF XRange { get; set; }

			/// <summary>The YAxis range calculated from known chart elements data</summary>
			public RangeF YRange { get; set; }
		}

		/// <summary>Customise context menu event args</summary>
		public class AddUserMenuOptionsEventArgs :EventArgs
		{
			public AddUserMenuOptionsEventArgs(EType type, ContextMenuStrip menu, HitTestResult hit_result)
			{
				Type      = type;
				Menu      = menu;
				HitResult = hit_result;
			}

			/// <summary>The context menu type</summary>
			public EType Type { get; private set; }
			public enum EType
			{
				Chart,
				XAxis,
				YAxis,
			}

			/// <summary>The menu to add menu items to</summary>
			public ContextMenuStrip Menu { get; private set; }

			/// <summary>The hit test result at the click location</summary>
			public HitTestResult HitResult { get; private set; }
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
		[Browsable(false)] public bool ConsistencyCheckSuspended
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
