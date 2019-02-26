using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Rylogic.Attrib;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Core.Windows;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Maths;
using Rylogic.Utility;
using Rectangle = System.Drawing.Rectangle;

namespace Rylogic.Gui.WPF
{
	using System.Windows;
	using System.Windows.Interop;
	using ChartDetail;

	/// <summary>A view 3d based chart control</summary>
	public partial class ChartControl : Canvas
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

		static ChartControl()
		{
			View3d.LoadDll();
		}
		public ChartControl()
			: this(string.Empty, new RdrOptions())
		{ }
		public ChartControl(string title, RdrOptions options)
		{
			InitializeComponent();
			//SetStyle(ControlStyles.ResizeRedraw, true);

			Options = options;
			Title = title;
			Range = new RangeData(this);
			BaseRangeX = new RangeF(0.0, 1.0);
			BaseRangeY = new RangeF(0.0, 1.0);
			//Scene = new ChartPanel(this); // Must come after 'Range'
			MouseOperations = new MouseOps();
			Tools = new ChartTools(this, Options);
			//m_tt_show_value = new HintBalloon { AutoSizeMode = AutoSizeMode.GrowOnly };
			m_chart_frame = new ChartFrame(this);

			Elements = new BindingListEx<Element> { PerItem = true, UseHashSet = true };
			Selected = new BindingListEx<Element> { PerItem = false };
			Hovered = new BindingListEx<Element> { PerItem = false };
			ElemIds = new Dictionary<Guid, Element>();

			Elements.ListChanging += (s, a) => OnElementListChanging(a);
			Selected.ListChanging += (s, a) => OnSelectionListChanging(a);
			Hovered.ListChanging += (s, a) => OnHoveredListChanging(a);

			AllowEditing = false;
			AllowSelection = false;
			DefaultMouseControl = true;
			DefaultKeyboardShortcuts = true;
			AreaSelectMode = EAreaSelectMode.Zoom;
		}
		//protected override void Dispose(bool disposing)
		//{
		//	MouseOperations = null;
		//	Tools = null;
		//	Range = null;
		//	Scene = null;
		//	Options = null;
		//	Util.Dispose(ref components);
		//	base.Dispose(disposing);
		//}
		//protected override void OnLayout(LayoutEventArgs e)
		//{
		//	if (Scene != null && !DesignMode)
		//	{
		//		using (this.SuspendLayout(false))
		//		{
		//			var dims = ChartDimensions;
		//			SceneBounds = dims.ChartArea;
		//			if (Options.LockAspect != null && dims.ChartArea.Area() != 0)
		//				Aspect = Options.LockAspect.Value;
		//		}
		//	}
		//	base.OnLayout(e);
		//}
		//protected override void OnPaint(PaintEventArgs e)
		//{
		//	base.OnPaint(e);
		//	if (DesignMode) return;
		//	DoPaint(e.Graphics);
		//}
		//protected override void OnInvalidated(InvalidateEventArgs e)
		//{
		//	Scene?.Invalidate();
		//	base.OnInvalidated(e);
		//}
		protected override void OnRender(DrawingContext dc)
		{
			base.OnRender(dc);
			//	DoPaint(e.Graphics);
		}

		/// <summary>Rendering options for the chart</summary>
		public RdrOptions Options
		{
			[DebuggerStepThrough]
			get { return m_rdr_options; }
			set
			{
				if (m_rdr_options == value) return;
				if (m_rdr_options != null)
				{
					m_rdr_options.PropertyChanged -= HandleRdrOptionsChanged;
				}
				m_rdr_options = value;
				if (m_rdr_options != null)
				{
					m_rdr_options.PropertyChanged += HandleRdrOptionsChanged;
				}

				void HandleRdrOptionsChanged(object sender, PropertyChangedEventArgs e)
				{
					OnOptionsChanged();
				}
			}
		}
		private RdrOptions m_rdr_options;

		/// <summary>Raised when options change</summary>
		public event EventHandler OptionsChanged;
		protected virtual void OnOptionsChanged()
		{
			OptionsChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>The title of the chart</summary>
		public string Title
		{
			get { return m_title ?? string.Empty; }
			set
			{
				if (m_title == value) return;
				m_title = value;
				//InvalidateArrange();
			}
		}
		private string m_title;

		/// <summary>The current X/Y axis range of the chart</summary>
		public RangeData Range
		{
			[DebuggerStepThrough]
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
		public RangeData.Axis XAxis => Range.XAxis;

		/// <summary>Accessor to the current Y axis</summary>
		public RangeData.Axis YAxis => Range.YAxis;

		/// <summary>Default X axis range of the chart</summary>
		public RangeF BaseRangeX { get; set; }

		/// <summary>Default Y axis range of the chart</summary>
		public RangeF BaseRangeY { get; set; }

		/// <summary>Get/Set the aspect ratio of the chart area</summary>
		public double Aspect
		{
			get { return Camera.Aspect * SceneBounds.Height / SceneBounds.Width; }
			set
			{
				if (Aspect == value) return;
				if (Options.LockAspect != null) Options.LockAspect = value;
				Camera.Aspect = (float)(value * SceneBounds.Width / SceneBounds.Height);
				SetRangeFromCamera();
				//Invalidate();
			}
		}

		/// <summary>The view3d part of the chart</summary>
		public ChartPanel Scene => m_chart_panel;
		//{
		//	[DebuggerStepThrough]
		//	get { return m_impl_scene; }
		//	private set
		//	{
		//		if (m_impl_scene == value) return;
		//		//using (this.SuspendLayout(false))
		//		//{
		//			if (m_impl_scene != null)
		//			{
		//				Children.Remove(m_impl_scene);
		//				Util.Dispose(ref m_impl_scene);
		//			}
		//			m_impl_scene = value;
		//			if (m_impl_scene != null)
		//			{
		//				Children.Add(m_impl_scene);
		//			}
		//		//}
		//	}
		//}
		private ChartPanel m_impl_scene;
		private ChartFrame m_chart_frame;

		/// <summary>Renderer</summary>
		public View3d View3d => Scene?.View3d;

		/// <summary>The view3d window for this control instance</summary>
		public View3d.Window Window => Scene?.Window;

		/// <summary>The view of the chart</summary>
		public View3d.Camera Camera => Scene?.Camera;

		/// <summary>All chart objects</summary>
		public BindingListEx<Element> Elements { get; private set; }

		/// <summary>The set of selected chart elements</summary>
		public BindingListEx<Element> Selected { get; private set; }

		/// <summary>The set of hovered chart elements</summary>
		public BindingListEx<Element> Hovered { get; private set; }

		/// <summary>Associative array from Element Ids to Elements</summary>
		public Dictionary<Guid, Element> ElemIds { get; private set; }

		/// <summary>Raised whenever the view3d area of the chart is scrolled or zoomed</summary>
		public event EventHandler<ChartMovedEventArgs> ChartMoved;
		protected virtual void OnChartMoved(ChartMovedEventArgs args)
		{
			ChartMoved?.Invoke(this, args);
		}
		//private void RaiseChartMoved(ChartMovedEventArgs args)
		//{
		//	if (!ChartMoved.IsSuspended())
		//		OnChartMoved(args);
		//}

		/// <summary>Raised whenever elements in the chart have been edited or moved</summary>
		public event EventHandler<ChartChangedEventArgs> ChartChanged;
		protected virtual void OnChartChanged(ChartChangedEventArgs args)
		{
			if (m_chart_changed_suspended != 0) return;
			ChartChanged?.Invoke(this, args);
		}
		public Scope SuspendChartChanged(bool raise_on_resume = true)
		{
			return Scope.Create(
				() => { return ++m_chart_changed_suspended; },
				cc => { if (--m_chart_changed_suspended == 0) OnChartChanged(new ChartChangedEventArgs(EChangeType.Edited)); });
		}
		private int m_chart_changed_suspended;
		//private ChartChangedEventArgs RaiseChartChanged(ChartChangedEventArgs args)
		//{
		//	if (!ChartChanged.IsSuspended())
		//		OnChartChanged(args);
		//
		//	return args;
		//}

		/// <summary>Called just before the chart renders. Used to add View3d objects to the scene</summary>
		public event EventHandler<ChartRenderingEventArgs> ChartRendering;
		protected virtual void OnChartRendering(ChartRenderingEventArgs args)
		{
			ChartRendering?.Invoke(this, args);
		}
		//private void RaiseChartRendering(ChartRenderingEventArgs args)
		//{
		//	if (!ChartRendering.IsSuspended())
		//		OnChartRendering(args);
		//}

		/// <summary>Called after the chart has painted, allowing users to add graphics on top of the chart</summary>
		public event EventHandler<AddOverlaysOnPaintEventArgs> AddOverlaysOnPaint;
		protected virtual void OnAddOverlaysOnPaint(AddOverlaysOnPaintEventArgs args)
		{
			AddOverlaysOnPaint?.Invoke(this, args);
		}
		//private void RaiseAddOverlaysOnPaint(AddOverlaysOnPaintEventArgs args)
		//{
		//	if (!AddOverlaysOnPaint.IsSuspended())
		//		OnAddOverlaysOnPaint(args);
		//}

		/// <summary>Called during AutoRange to allow handlers to set the auto range</summary>
		public event EventHandler<AutoRangeEventArgs> AutoRanging;
		protected virtual void OnAutoRanging(AutoRangeEventArgs args)
		{
			AutoRanging?.Invoke(this, args);
		}
		//private void RaiseOnAutoRanging(AutoRangeEventArgs args)
		//{
		//	if (!AutoRanging.IsSuspended())
		//		OnAutoRanging(args);
		//}

		/// <summary>The client space rectangle that contains the View3d part of the control</summary>
		public RectangleF SceneBounds
		{
			get
			{
				return new RectangleF(0, 0, 0, 0);//todo
			}
			//private set { Scene.Bounds = value; }
			//get { return Scene.Bounds; }
			//private set { Scene.Bounds = value; }
		}

		/// <summary>Returns a point in chart space from a point in client space. Use to convert mouse (client-space) locations to chart coordinates</summary>
		public PointF ClientToChart(PointF client_point)
		{
			return new PointF(
				(float)(XAxis.Min + (client_point.X - SceneBounds.Left) * XAxis.Span / SceneBounds.Width),
				(float)(YAxis.Min - (client_point.Y - SceneBounds.Bottom) * YAxis.Span / SceneBounds.Height));
		}
		public SizeF ClientToChart(SizeF client_size)
		{
			return new SizeF(
				(float)(client_size.Width * XAxis.Span / SceneBounds.Width),
				(float)(client_size.Height * YAxis.Span / SceneBounds.Height));
		}
		public RectangleF ClientToChart(RectangleF client_rect)
		{
			return new RectangleF(
				ClientToChart(client_rect.Location),
				ClientToChart(client_rect.Size));
		}

		/// <summary>Returns a point in client space from a point in chart space. Inverse of ClientToChart</summary>
		public PointF ChartToClient(PointF chart_point)
		{
			return new PointF(
				(float)(SceneBounds.Left + (chart_point.X - XAxis.Min) * SceneBounds.Width / XAxis.Span),
				(float)(SceneBounds.Bottom - (chart_point.Y - YAxis.Min) * SceneBounds.Height / YAxis.Span));
		}
		public SizeF ChartToClient(SizeF chart_size)
		{
			return new SizeF(
				(float)(chart_size.Width * SceneBounds.Width / XAxis.Span),
				(float)(chart_size.Height * SceneBounds.Height / YAxis.Span));
		}
		public RectangleF ChartToClient(RectangleF chart_rect)
		{
			return new RectangleF(
				ChartToClient(chart_rect.Location),
				ChartToClient(chart_rect.Size));
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
		public m4x4 ChartToClientSpace(RectangleF plot_area)
		{
			var scale_x = (float)+(plot_area.Width / XAxis.Span);
			var scale_y = (float)-(plot_area.Height / YAxis.Span);
			var offset_x = (float)+(plot_area.Left - XAxis.Min * scale_x);
			var offset_y = (float)+(plot_area.Bottom - YAxis.Min * scale_y);

			// C = chart, c = client
			var C2c = new m4x4(
				new v4(scale_x, 0, 0, 0),
				new v4(0, scale_y, 0, 0),
				new v4(0, 0, 1, 0),
				new v4(offset_x, offset_y, 0, 1));

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
			return ChartToClientSpace(SceneBounds);
		}

		/// <summary>
		/// Get the scale and translation transform from client space to chart space.
		/// e.g. client2chart * plot_area.BottomLeft() = Point(x_min, y_min)
		///      client2chart * plot_area.TopRight()   = Point(x_max, y_max)</summary>
		public m4x4 ClientToChartSpace(RectangleF plot_area)
		{
			var scale_x = (float)+(XAxis.Span / plot_area.Width);
			var scale_y = (float)-(YAxis.Span / plot_area.Height);
			var offset_x = (float)+(XAxis.Min - plot_area.Left * scale_x);
			var offset_y = (float)+(YAxis.Min - plot_area.Bottom * scale_y);

			// C = chart, c = client
			var c2C = new m4x4(
				new v4(scale_x, 0, 0, 0),
				new v4(0, scale_y, 0, 0),
				new v4(0, 0, 1, 0),
				new v4(offset_x, offset_y, 0, 1));

#if true
			// Check the plot_area corners map to the expected graph space locations
			var c_pt0 = new v4((float)plot_area.Left, (float)plot_area.Bottom, 0, 1);
			var c_pt1 = new v4((float)plot_area.Right, (float)plot_area.Top, 0, 1);
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
			return ClientToChartSpace(SceneBounds);
		}

		/// <summary>Convert between client space and normalised screen space</summary>
		public PointF ClientToNSS(PointF client_point)
		{
			return new PointF(
				((client_point.X - SceneBounds.Left) / SceneBounds.Width) * 2f - 1f,
				((SceneBounds.Bottom - client_point.Y) / SceneBounds.Height) * 2f - 1f);
		}
		public SizeF ClientToNSS(SizeF client_size)
		{
			return new SizeF(
				(client_size.Width / SceneBounds.Width) * 2f,
				(client_size.Height / SceneBounds.Height) * 2f);
		}
		public RectangleF ClientToNSS(RectangleF client_rect)
		{
			return new RectangleF(
				ClientToNSS(client_rect.Location),
				ClientToNSS(client_rect.Size));
		}
		public PointF NSSToClient(PointF nss_point)
		{
			return new PointF(
				SceneBounds.Left + 0.5f * (nss_point.X + 1f) * SceneBounds.Width,
				SceneBounds.Bottom - 0.5f * (nss_point.Y + 1f) * SceneBounds.Height);
		}
		public SizeF NSSToClient(SizeF nss_size)
		{
			return new SizeF(
				0.5f * nss_size.Width * SceneBounds.Width,
				0.5f * nss_size.Height * SceneBounds.Height);
		}
		public RectangleF NSSToClient(RectangleF nss_rect)
		{
			return new RectangleF(
				NSSToClient(nss_rect.Location),
				NSSToClient(nss_rect.Size));
		}

		/// <summary>Perform a hit test on the chart</summary>
		public HitTestResult HitTestCS(PointF client_point, ModifierKeys modifier_keys, Func<Element, bool> pred)
		{
			// Determine the hit zone of the control
			var zone = EZone.None;
			if (SceneBounds.Contains(client_point)) zone |= EZone.Chart;
			if (XAxisBounds.Contains(client_point)) zone |= EZone.XAxis;
			if (YAxisBounds.Contains(client_point)) zone |= EZone.YAxis;
			if (TitleBounds.Contains(client_point)) zone |= EZone.Title;

			// The hit test point in chart space
			var chart_point = ClientToChart(client_point);

			// Find elements that overlap 'client_point'
			var hits = new List<HitTestResult.Hit>();
			if (zone.HasFlag(EZone.Chart))
			{
				var elements = pred != null ? Elements.Where(pred) : Elements;
				foreach (var elem in elements)
				{
					var hit = elem.HitTest(chart_point, client_point, modifier_keys, Scene.Camera);
					if (hit != null)
						hits.Add(hit);
				}

				// Sort the results by z order
				hits.Sort((l, r) => -l.Element.PositionZ.CompareTo(r.Element.PositionZ));
			}

			return new HitTestResult(zone, client_point, chart_point, modifier_keys, hits, Scene.Camera);
		}

		/// <summary>Find the default range, then reset to the default range</summary>
		public void AutoRange(View3d.ESceneBounds who = View3d.ESceneBounds.All)
		{
			// Allow the auto range to be handled by event
			var args = new AutoRangeEventArgs(who);
			OnAutoRanging(args);
			if (args.Handled && (!args.ViewBBox.IsValid || args.ViewBBox.Radius == v4.Zero))
				throw new Exception($"Caller provided view bounding box is invalid: {args.ViewBBox}");

			// Get the bounding box to fit into the view
			var bbox = args.Handled
				? args.ViewBBox
				: Window.SceneBounds(who, except: new[] { ChartTools.Id });

			// Position the camera to view the bounding box
			Camera.ResetView(bbox, Options.ResetForward, Options.ResetUp,
				dist: 0f,
				preserve_aspect: LockAspect,
				commit: true);

			// Set the axis range from the camera position
			SetRangeFromCamera();
			//Invalidate();
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
					Options.LockAspect = (XAxis.Span * SceneBounds.Height) / (YAxis.Span * SceneBounds.Width);
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
			var w2c = Math_.InvertFast(Scene.Camera.O2W);

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
				c2w.z * Math_.Dot(Scene.Camera.FocusPoint - v4.Origin, c2w.z);

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

				// Draw the chart frame
				//m_chart_frame.DoPaint(gfx, dims);

				// Ensure the scene is rendered
				//SceneBounds = dims.ChartArea;
				//Scene.Update();

				// Add user graphics over the chart
				//OnAddOverlaysOnPaint(new AddOverlaysOnPaintEventArgs(gfx, dims, ChartToClientSpace(), ~EZone.None ^ EZone.Chart));
			}
			catch (OverflowException)
			{
				// There is a problem in the .NET graphics object that can cause these exceptions if the range is extreme
				//using (var bsh = new SolidBrush(Color.FromArgb(0x80, Color.Black)))
				//	gfx.DrawString("Rendering error within .NET", Options.TitleFont, bsh, PointF.Empty);
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
		public string LocationText(PointF location)
		{
			var pt = ClientToChart(location);
			XAxis.GridLines(out var minx, out var maxx, out var stepx);
			YAxis.GridLines(out var miny, out var maxy, out var stepy);
			var xtick = XAxis.TickText(pt.X, stepx);
			var ytick = YAxis.TickText(pt.Y, stepy);
			return $"{xtick} , {ytick}";
		}

		/// <summary>Return the current mouse pointer location as a string</summary>
		public string PointerLocationText => LocationText(Mouse.GetPosition(this).ToPointF());

		/// <summary>The client space area of the XAxis area of the chart</summary>
		private RectangleF XAxisBounds
		{
			get { return RectangleF.FromLTRB(SceneBounds.Left, SceneBounds.Bottom, SceneBounds.Right, (float)Height); }
		}

		/// <summary>The client space area of the YAxis area of the chart</summary>
		private RectangleF YAxisBounds
		{
			get { return RectangleF.FromLTRB(0, SceneBounds.Top, SceneBounds.Left, SceneBounds.Bottom); }
		}

		/// <summary>The client space area of the chart title</summary>
		private RectangleF TitleBounds
		{
			get { return RectangleF.FromLTRB(SceneBounds.Left, 0, SceneBounds.Right, SceneBounds.Top); }
		}


		#region Mouse Operations

		/// <summary>Per button current mouse operation</summary>
		public MouseOps MouseOperations { get; private set; }

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
					foreach (var e in Elements)
						e.SetChartInternal(this, false);
					ElemIds.Clear();
					foreach (var e in Elements)
						ElemIds.Add(e.Id, e);

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
			case ListChg.ItemPreRemove:
				{
					elem.Selected = false;
					elem.Hovered = false;
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
					foreach (var e in Elements)
						e.SetHoveredInternal(false, false);
					foreach (var h in Hovered)
						h.SetHoveredInternal(true, false);
					Debug.Assert(CheckConsistency());
					//Invalidate();
					break;
				}
			case ListChg.ItemAdded:
				{
					elem.SetHoveredInternal(true, false);
					Debug.Assert(CheckConsistency());
					//Invalidate();
					break;
				}
			case ListChg.ItemRemoved:
				{
					elem.SetHoveredInternal(false, false);
					Debug.Assert(CheckConsistency());
					//Invalidate();
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
		public void SelectElements(RectangleF rect, ModifierKeys modifier_keys)
		{
			if (!AllowSelection)
				return;

			// Normalise the selection
			var r = new BBox(new v4(rect.Centre(), 0f, 1f), new v4(Math_.Abs(v2.From(rect.Size)) * 0.5f, 1f, 0f));

			// If the area of selection is less than the min drag distance, assume click selection
			var is_click = r.DiametreSq < Math_.Sqr(Options.MinDragPixelDistance);
			if (is_click)
			{
				var pt = ChartToClient(rect.Location).ToPoint();
				var hits = HitTestCS(pt, modifier_keys, x => x.Enabled);

				// If control is down, deselect the first selected element in the hit list
				if (modifier_keys.HasFlag(ModifierKeys.Control))
				{
					var first = hits.Hits.FirstOrDefault(x => x.Element.Selected);
					if (first != null)
						first.Element.Selected = false;
				}
				// If shift is down, select the first element not already selected in the hit list
				else if (modifier_keys.HasFlag(ModifierKeys.Shift))
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
						to_select = hits.Hits[i + 1];
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
					if (modifier_keys.HasFlag(ModifierKeys.Control))
					{
						// Only need to look in the selected list
						foreach (var elem in Selected.Where(x => r.IsWithin(x.Bounds, 0f)).ToArray())
							elem.Selected = false;
					}
					// If shift is down, those in the selection area become selected
					else if (modifier_keys.HasFlag(ModifierKeys.Shift))
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

			//Invalidate();
		}

		#endregion


		#region Show Value Tooltip

		/// <summary>A tool tip to display the mouse location value</summary>
		//private HintBalloon m_tt_show_value;
		private bool m_show_value;

		/// <summary>Update the text in the 'show value' hint balloon. 'location' is in client space</summary>
		private void SetValueToolTip(Point location)
		{
			//if (SceneBounds.Contains(location))
			//{
			//	m_tt_show_value.Text = LocationText(location);
			//	m_tt_show_value.Location = PointToScreen(location);
			//	m_tt_show_value.Owner = TopLevelControl as Form;
			//	m_tt_show_value.Visible = true;
			//}
			//else
			//{
			//	m_tt_show_value.Visible = false;
			//}
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
				//Invalidate();

				// Handlers
				void OnMouseMoveCrossHair(object sender, MouseEventArgs e)
				{
					var location = e.GetPosition(this).ToPointF();
					if (SceneBounds.Contains(location))
					{
						CrossHairLocation = ClientToChart(location);
						//Invalidate();
					}
				}
				void OnMouseWheelCrossHair(object sender, MouseEventArgs e)
				{
					var location = e.GetPosition(this).ToPointF();
					if (SceneBounds.Contains(location))
					{
						CrossHairLocation = ClientToChart(location);
						//Invalidate();
					}
				}
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
				CrossHairMoved?.Invoke(this, EventArgs.Empty);
				//Invalidate();
				//Update();
			}
		}
		private PointF m_cross_hair_location;

		/// <summary>Raised when the cross hair is visible and moved</summary>
		public event EventHandler CrossHairMoved;

		#endregion

		/// <summary>Chart graphics</summary>
		private ChartTools Tools
		{
			[DebuggerStepThrough]
			get { return m_tools; }
			set
			{
				if (m_tools == value) return;
				Util.Dispose(ref m_tools);
				m_tools = value;
			}
		}
		private ChartTools m_tools;

		#region Nested Classes

		///// <summary>Special menu item that doesn't draw highlighted</summary>
		//private class NoHighlightToolStripMenuItem : ToolStripMenuItem
		//{
		//	public NoHighlightToolStripMenuItem(string text) : base(text) { }
		//}
		//
		///// <summary>Custom button renderer because the office 'checked' state buttons look crap</summary>
		//private class ContextMenuRenderer : ToolStripProfessionalRenderer
		//{
		//	protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
		//	{
		//		var item = e.Item as NoHighlightToolStripMenuItem;
		//		if (item == null) { base.OnRenderMenuItemBackground(e); return; }
		//
		//		e.Graphics.FillRectangle(new SolidBrush(item.BackColor), e.Item.ContentRectangle);
		//		e.Graphics.DrawRectangle(Pens.Black, e.Item.ContentRectangle);
		//	}
		//}
		//
		///// <summary>Cursors for the chart</summary>
		//public static class Cursors
		//{
		//	public static readonly Cursor Default = System.Windows.Forms.Cursors.Default;
		//	public static readonly Cursor WaitCursor = System.Windows.Forms.Cursors.WaitCursor;
		//	public static readonly Cursor SizeWE = System.Windows.Forms.Cursors.SizeWE;
		//	public static readonly Cursor SizeNS = System.Windows.Forms.Cursors.SizeNS;
		//	public static readonly Cursor SizeNESW = System.Windows.Forms.Cursors.SizeNESW;
		//	public static readonly Cursor SizeNWSE = System.Windows.Forms.Cursors.SizeNWSE;
		//	public static readonly Cursor SizeAll = System.Windows.Forms.Cursors.SizeAll;
		//	public static readonly Cursor Arrow = Resources.cursor_arrow.ToCursor(Point.Empty);
		//	public static readonly Cursor ArrowPlus = Resources.cursor_arrow_plus.ToCursor(Point.Empty);
		//	public static readonly Cursor ArrowMinus = Resources.cursor_arrow_minus.ToCursor(Point.Empty);
		//	public static readonly Cursor CrossHair = System.Windows.Forms.Cursors.Cross;
		//}

		#endregion

		#region Self Consistency

		/// <summary>Check the self consistency of elements</summary>
		public bool CheckConsistency()
		{
			if (m_consistency_check_ref_count != 0) return true;
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
				if (!Equals(elements[i].Id, elements[i + 1].Id)) continue;
				throw new Exception($"Element {elements[i]} is in the Elements collection more than once");
			}

			// All elements in the chart should have their Chart property set to this chart
			foreach (var elem in Elements)
			{
				if (elem.Chart == this) continue;
				throw new Exception($"Element {elem} is in the Elements collection but does not have its Chart property set correctly");
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
					throw new Exception($"Element {elem} is not in the Element Id lookup table");

			// Check the consistency of all elements
			foreach (var elem in Elements)
				elem.CheckConsistency();
		}

		/// <summary>Temporarily disable the consistency check</summary>
		public Scope SuspendConsistencyCheck(bool check_on_exit = true)
		{
			return Scope.Create(
				() => ++m_consistency_check_ref_count,
				() =>
				{
					--m_consistency_check_ref_count;
					if (check_on_exit && m_consistency_check_ref_count == 0)
						Debug.Assert(CheckConsistency());
				});
		}
		private int m_consistency_check_ref_count;

		/// <summary>Element sorting predicates</summary>
		protected Cmp<Element> ByGuid = Cmp<Element>.From((l, r) => l.Id.CompareTo(r.Id));

		#endregion
	}
}
