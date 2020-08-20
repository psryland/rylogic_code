using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Extn.Windows;
using Rylogic.Gfx;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	using ChartDetail;

	/// <summary>A view 3d based chart control</summary>
	public partial class ChartControl :UserControl, IDisposable, INotifyPropertyChanged, IChartCMenu, IView3dCMenu
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
		// - Client space is the control's coordinates. The View3D part typically has a non-zero
		//   offset from the control origin. e.g. SceneBounds = [30,10,100,100] say.
		//   Note: the overlay has a transform applied to it so that points in client space are
		//   correctly positioned.
		static ChartControl()
		{
			View3d.LoadDll(throw_if_missing: false);
		}
		public ChartControl()
			: this(string.Empty, new OptionsData())
		{ }
		public ChartControl(string title, OptionsData options)
		{
			InitializeComponent();

			Title = title;
			Options = options;
			Scene.Chart = this;
			Range = new RangeData(this);
			BaseRangeX = new RangeF(0.0, 1.0);
			BaseRangeY = new RangeF(0.0, 1.0);
			MouseOperations = new MouseOps();
			Elements = new ElementCollection(this);
			Selected = new SelectedCollection(this);
			Hovered = new HoveredCollection(this);

			AllowSelection = false;
			AllowElementDragging = false;
			DefaultMouseControl = true;
			DefaultKeyboardShortcuts = true;

			Overlay.PreviewKeyDown += (s, a) => OnKeyDown(a);
			Overlay.PreviewKeyUp += (s, a) => OnKeyUp(a);
			Scene.BuildScene += OnBuildScene;

			// Prevent the object manager seeing anything created in the 'CtxId' context
			View3d.ObjectManager.ExcludeCtxIds.Add(CtxId);
			if (DesignerProperties.GetIsInDesignMode(this))
				return;

			try
			{
				View3d = View3d.Create();

				InitCMenus();
				InitCommands();
				InitNavigation();
				DataContext = this;
			}
			catch
			{
				Dispose();
				throw;
			}
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool _)
		{
			MouseOperations = null!;
			Range = null!;
			Options = null!;
			Scene.Dispose();
			View3d = null!;
		}

		/// <summary>Rendering options for the chart</summary>
		public OptionsData Options
		{
			get => (OptionsData)GetValue(OptionsProperty);
			set => SetValue(OptionsProperty, value);
		}
		private void Options_Changed(OptionsData new_value, OptionsData old_value)
		{
			if (old_value != null)
			{
				old_value.XAxis.PropertyChanged -= HandleAxisOptionsChanged;
				old_value.YAxis.PropertyChanged -= HandleAxisOptionsChanged;
				old_value.PropertyChanged -= HandleOptionsChanged;
			}
			if (new_value != null)
			{
				// Apply the options to the scene
				Camera.Orthographic = Options.Orthographic;
				Window.FillMode = Options.FillMode;
				Window.CullMode = Options.CullMode;
				Window.BackgroundColour = Options.BackgroundColour;
				Window.FocusPointVisible = Options.FocusPointVisible;
				Window.OriginPointVisible = Options.OriginPointVisible;
				Scene.MultiSampling = Options.Antialiasing ? 4 : 1;
				PositionAxisPanels();

				new_value.PropertyChanged += HandleOptionsChanged;
				new_value.XAxis.PropertyChanged += HandleAxisOptionsChanged;
				new_value.YAxis.PropertyChanged += HandleAxisOptionsChanged;
			}

			void HandleOptionsChanged(object sender, PropertyChangedEventArgs e)
			{
				var view3d_cmenu = Scene.ContextMenu?.DataContext as IView3dCMenu;
				var chart_cmenu = Scene.ContextMenu?.DataContext as IChartCMenu;
				switch (e.PropertyName)
				{
					case nameof(OptionsData.ShowGridLines):
					{
						chart_cmenu?.NotifyPropertyChanged(nameof(IChartCMenu.ShowGridLines));
						break;
					}
					case nameof(OptionsData.ShowAxes):
					{
						m_xaxis_panel.Invalidate();
						m_yaxis_panel.Invalidate();
						NotifyPropertyChanged(nameof(XAxisLabelVisibility));
						NotifyPropertyChanged(nameof(YAxisLabelVisibility));
						chart_cmenu?.NotifyPropertyChanged(nameof(IChartCMenu.ShowAxes));
						break;
					}
					case nameof(OptionsData.Antialiasing):
					{
						Scene.Antialiasing = Options.Antialiasing;
						view3d_cmenu?.NotifyPropertyChanged(nameof(IView3dCMenu.Antialiasing));
						Invalidate();
						break;
					}
					case nameof(OptionsData.Orthographic):
					{
						Camera.Orthographic = Options.Orthographic;
						view3d_cmenu?.NotifyPropertyChanged(nameof(IView3dCMenu.Orthographic));
						Invalidate();
						break;
					}
					case nameof(OptionsData.FillMode):
					{
						Window.FillMode = Options.FillMode;
						view3d_cmenu?.NotifyPropertyChanged(nameof(IView3dCMenu.FillMode));
						Invalidate();
						break;
					}
					case nameof(OptionsData.CullMode):
					{
						Window.CullMode = Options.CullMode;
						view3d_cmenu?.NotifyPropertyChanged(nameof(IView3dCMenu.CullMode));
						Invalidate();
						break;
					}
					case nameof(OptionsData.BackgroundColour):
					{
						Window.BackgroundColour = Options.BackgroundColour;
						//NotifyPropertyChanged(nameof(ChartBackground));
						view3d_cmenu?.NotifyPropertyChanged(nameof(IView3dCMenu.BackgroundColour));

						// Invalidate grid lines and crosshair because their colour depends on the background colour
						XAxis.Gfx.Invalidate();
						YAxis.Gfx.Invalidate();
						if (ShowCrossHair)
						{
							ShowCrossHair = false;
							ShowCrossHair = true;
						}
						Invalidate();
						break;
					}
					case nameof(OptionsData.FocusPointVisible):
					{
						Window.FocusPointVisible = Options.FocusPointVisible;
						view3d_cmenu?.NotifyPropertyChanged(nameof(IView3dCMenu.FocusPointVisible));
						Invalidate();
						break;
					}
					case nameof(OptionsData.OriginPointVisible):
					{
						Window.OriginPointVisible = Options.OriginPointVisible;
						view3d_cmenu?.NotifyPropertyChanged(nameof(IView3dCMenu.OriginPointVisible));
						Invalidate();
						break;
					}
					case nameof(OptionsData.NavigationMode):
					{
						chart_cmenu?.NotifyPropertyChanged(nameof(IChartCMenu.NavigationMode));
						break;
					}
					case nameof(OptionsData.LockAspect):
					{
						chart_cmenu?.NotifyPropertyChanged(nameof(IChartCMenu.LockAspect));
						break;
					}
					case nameof(OptionsData.MouseCentredZoom):
					{
						chart_cmenu?.NotifyPropertyChanged(nameof(IChartCMenu.MouseCentredZoom));
						break;
					}
				}
			}
			void HandleAxisOptionsChanged(object sender, PropertyChangedEventArgs e)
			{
				switch (e.PropertyName)
				{
					case nameof(OptionsData.Axis.Side):
					{
						PositionAxisPanels();
						break;
					}
				}
			}
		}
		public static readonly DependencyProperty OptionsProperty = Gui_.DPRegister<ChartControl>(nameof(Options), new OptionsData());

		/// <summary>Scene background colour</summary>
		public SolidColorBrush SceneBackground
		{
			get => Options.BackgroundColour.ToMediaBrush();
			set => Options.BackgroundColour = value.Color.ToColour32();
		}

		/// <summary>The title of the chart</summary>
		public string Title
		{
			get => m_title ?? string.Empty;
			set
			{
				if (m_title == value) return;
				m_title = value;
				NotifyPropertyChanged(nameof(Title));
			}
		}
		private string? m_title;

		/// <summary>All chart objects</summary>
		public ElementCollection Elements { get; }

		/// <summary>The set of selected chart elements</summary>
		public SelectedCollection Selected { get; }

		/// <summary>The set of hovered chart elements</summary>
		public HoveredCollection Hovered { get; }

		/// <summary>The current X/Y axis range of the chart</summary>
		public RangeData Range
		{
			get => m_range;
			private set
			{
				if (value == m_range) return;
				Util.Dispose(ref m_range!);
				m_range = value;
				if (m_range != null)
				{
					NotifyPropertyChanged(nameof(Range));
					NotifyPropertyChanged(nameof(XAxis));
					NotifyPropertyChanged(nameof(YAxis));
				}
			}
		}
		private RangeData m_range = null!;

		/// <summary>Accessor to the current X axis</summary>
		public RangeData.Axis XAxis => Range.XAxis;
		public AxisPanel XAxisPanel => m_xaxis_panel;

		/// <summary>Accessor to the current Y axis</summary>
		public RangeData.Axis YAxis => Range.YAxis;
		public AxisPanel YAxisPanel => m_yaxis_panel;

		/// <summary>Accessor to the current Y axis</summary>
		public RangeData.Axis GetAxis(EAxis axis)
		{
			return
				axis == EAxis.XAxis ? XAxis :
				axis == EAxis.YAxis ? YAxis :
				throw new Exception($"Cannot return the axis associated with {axis}");
		}
		public AxisPanel GetAxisPanel(EAxis axis)
		{
			return
				axis == EAxis.XAxis ? XAxisPanel :
				axis == EAxis.YAxis ? YAxisPanel :
				throw new Exception($"Cannot return the axis panel associated with {axis}");
		}

		/// <summary>Default X axis range of the chart</summary>
		public RangeF BaseRangeX { get; set; }

		/// <summary>Default Y axis range of the chart</summary>
		public RangeF BaseRangeY { get; set; }

		/// <summary>The view3d part of the chart</summary>
		public ChartPanel Scene => m_chart_panel;

		/// <summary>A WPF canvas that overlays the 3d part of the chart</summary>
		public Canvas Overlay => m_chart_overlay;

		/// <summary>The camera view of the scene</summary>
		public View3d.Camera Camera => Scene.Camera;

		/// <summary>The view3d window associated with 'Scene'</summary>
		private View3d.Window Window => Scene.Window; // Keep private, clients should be using 'Scene'

		/// <summary>A control to use as the legend.</summary>
		public FrameworkElement? Legend
		{
			get => m_legend;
			set
			{
				if (m_legend == value) return;
				if (m_legend != null)
				{
					Overlay.Children.Remove(m_legend);
				}
				m_legend = value;
				if (m_legend != null)
				{
					// If the legend is 'ChartLegend' (i.e. not some custom thing),
					// then default the ItemsSource to the chart elements.
					if (m_legend is ChartLegend legend) legend.ItemsSource ??= Elements;
					Overlay.Children.Add(m_legend);
				}
			}
		}
		private FrameworkElement? m_legend = null;

		/// <summary>View3d context reference</summary>
		private View3d View3d
		{
			get => m_view3d;
			set
			{
				if (m_view3d == value) return;
				Util.Dispose(ref m_view3d!);
				m_view3d = value;
			}
		}
		private View3d m_view3d = null!;

		/// <summary>Raised just before the chart renders, allowing users to add custom graphics</summary>
		public event EventHandler<View3dControl.BuildSceneEventArgs>? BuildScene;
		private void OnBuildScene(object? sender, View3dControl.BuildSceneEventArgs e)
		{
			// Notes:
			//  - 'Elements' manage their graphics directly in the View3d.Window. Those
			//    with overlay controls also have direct access to 'Chart.Overlay'
			//  - BuildScene is really just for graphics elements that aren't derived
			//    from 'Chart.Element'.

			// The offset in client space to the overlay origin
			var pt = TransformToDescendant(Scene).Transform(new Point());
			Canvas.SetLeft(Overlay, pt.X);
			Canvas.SetTop(Overlay, pt.Y);

			// Allow external additions to the scene
			BuildScene?.Invoke(this, e);
		}

		/// <summary>Invalidate the chart, triggering a redraw</summary>
		public void Invalidate()
		{
			Scene.Invalidate();
		}
		public void Invalidate(object sender, EventArgs args)
		{
			Invalidate();
		}

		/// <summary>Raised whenever the view3d area of the chart is scrolled or zoomed</summary>
		public event EventHandler<ChartMovedEventArgs>? ChartMoved;
		protected virtual void OnChartMoved(ChartMovedEventArgs args)
		{
			ChartMoved?.Invoke(this, args);
		}

		/// <summary>Raised whenever elements in the chart have been edited or moved</summary>
		public event EventHandler<ChartChangedEventArgs>? ChartChanged;
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

		/// <summary>Called after the chart has painted, allowing users to add graphics on top of the chart</summary>
		public event EventHandler<AddOverlaysOnPaintEventArgs>? AddOverlaysOnPaint;
		protected virtual void OnAddOverlaysOnPaint(AddOverlaysOnPaintEventArgs args)
		{
			AddOverlaysOnPaint?.Invoke(this, args);
		}

		/// <summary>Called during AutoRange to allow handlers to set the auto range</summary>
		public event EventHandler<AutoRangeEventArgs>? AutoRanging;
		protected virtual void OnAutoRanging(AutoRangeEventArgs args)
		{
			AutoRanging?.Invoke(this, args);
		}

		/// <summary>Returns a point in chart space from a point in client space. Use to convert mouse (client-space) locations to chart coordinates</summary>
		public Point ClientToChart(Point client_point)
		{
			if (SceneBounds.Width == 0 || SceneBounds.Height == 0)
				throw new Exception("Chart size is zero, cannot convert from client space to chart space");

			return new Point(
				(XAxis.Min + (client_point.X - SceneBounds.Left) * XAxis.Span / SceneBounds.Width),
				(YAxis.Min - (client_point.Y - SceneBounds.Bottom) * YAxis.Span / SceneBounds.Height));
		}
		public Size ClientToChart(Size client_size)
		{
			if (SceneBounds.Width == 0 || SceneBounds.Height == 0)
				throw new Exception("Chart size is zero, cannot convert from client space to chart space");

			return new Size(
				(client_size.Width * XAxis.Span / SceneBounds.Width),
				(client_size.Height * YAxis.Span / SceneBounds.Height));
		}
		public Vector ClientToChart(Vector client_vec)
		{
			if (SceneBounds.Width == 0 || SceneBounds.Height == 0)
				throw new Exception("Chart size is zero, cannot convert from client space to chart space");

			return new Vector(
				(+client_vec.X * XAxis.Span / SceneBounds.Width),
				(-client_vec.Y * YAxis.Span / SceneBounds.Height));
		}
		public Rect ClientToChart(Rect client_rect)
		{
			return new Rect(
				ClientToChart(client_rect.Location),
				ClientToChart(client_rect.Size));
		}

		/// <summary>Returns a point in client space from a point in chart space. Inverse of ClientToChart</summary>
		public Point ChartToClient(Point chart_point)
		{
			if (SceneBounds.Width == 0 || SceneBounds.Height == 0)
				throw new Exception("Chart size is zero, cannot convert from chart space to client space");

			return new Point(
				(SceneBounds.Left + (chart_point.X - XAxis.Min) * SceneBounds.Width / XAxis.Span),
				(SceneBounds.Bottom - (chart_point.Y - YAxis.Min) * SceneBounds.Height / YAxis.Span));
		}
		public Size ChartToClient(Size chart_size)
		{
			if (SceneBounds.Width == 0 || SceneBounds.Height == 0)
				throw new Exception("Chart size is zero, cannot convert from chart space to client space");

			return new Size(
				(chart_size.Width * SceneBounds.Width / XAxis.Span),
				(chart_size.Height * SceneBounds.Height / YAxis.Span));
		}
		public Vector ChartToClient(Vector chart_vec)
		{
			if (SceneBounds.Width == 0 || SceneBounds.Height == 0)
				throw new Exception("Chart size is zero, cannot convert from chart space to client space");

			return new Vector(
				(+chart_vec.X * SceneBounds.Width / XAxis.Span),
				(-chart_vec.Y * SceneBounds.Height / YAxis.Span));
		}
		public Rect ChartToClient(Rect chart_rect)
		{
			return new Rect(
				ChartToClient(chart_rect.Location),
				ChartToClient(chart_rect.Size));
		}

		/// <summary>Return a point in camera space from a point in chart space (Z = focus plane)</summary>
		public v4 ChartToCamera(Point chart_point)
		{
			// Remove the camera to origin offset
			var origin_cs = Camera.W2O.pos.xy;
			return new v4((float)(chart_point.X + origin_cs.x), (float)(chart_point.Y + origin_cs.y), -Camera.FocusDist, 1f);
		}

		/// <summary>Return a point in chart space from a point in camera space</summary>
		public Point CameraToChart(v4 camera_point)
		{
			Debug.Assert(camera_point.z != 0);

			// Project the camera space point onto the focus plane
			var proj = -Camera.FocusDist / camera_point.z;
			var pt = new v2(camera_point.x * proj, camera_point.y * proj);

			// Add the offset of the camera from the origin
			var origin_cs = Camera.W2O.pos.xy;
			return new Point(pt.x - origin_cs.x, pt.y - origin_cs.y);
		}

		/// <summary>
		/// Get the scale and translation transform from chart space to client space.
		/// e.g. chart2client * Point(x_min, y_min) = plot_area.BottomLeft()
		///      chart2client * Point(x_max, y_max) = plot_area.TopRight()</summary>
		public m4x4 ChartToClientSpace(Rect plot_area)
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
		public m4x4 ClientToChartSpace(Rect plot_area)
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

			#if false
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
		public Point ClientToNSS(Point client_point)
		{
			return new Point(
				((client_point.X - SceneBounds.Left) / SceneBounds.Width) * 2 - 1,
				((SceneBounds.Bottom - client_point.Y) / SceneBounds.Height) * 2 - 1);
		}
		public Size ClientToNSS(Size client_size)
		{
			return new Size(
				(client_size.Width / SceneBounds.Width) * 2,
				(client_size.Height / SceneBounds.Height) * 2);
		}
		public Rect ClientToNSS(Rect client_rect)
		{
			return new Rect(
				ClientToNSS(client_rect.Location),
				ClientToNSS(client_rect.Size));
		}
		public Point NSSToClient(Point nss_point)
		{
			return new Point(
				SceneBounds.Left + 0.5 * (nss_point.X + 1) * SceneBounds.Width,
				SceneBounds.Bottom - 0.5 * (nss_point.Y + 1) * SceneBounds.Height);
		}
		public Size NSSToClient(Size nss_size)
		{
			return new Size(
				0.5 * nss_size.Width * SceneBounds.Width,
				0.5 * nss_size.Height * SceneBounds.Height);
		}
		public Rect NSSToClient(Rect nss_rect)
		{
			return new Rect(
				NSSToClient(nss_rect.Location),
				NSSToClient(nss_rect.Size));
		}

		/// <summary>Perform a hit test on the chart but only test which zone is hit (faster)</summary>
		public HitTestResult HitTestZoneCS(Point client_point, ModifierKeys modifier_keys, EMouseBtns mouse_btns)
		{
			// Determine the hit zone of the control
			var zone = EZone.None;
			if (SceneBounds.Contains(client_point)) zone |= EZone.Chart;
			if (XAxisBounds.Contains(client_point)) zone |= EZone.XAxis;
			if (YAxisBounds.Contains(client_point)) zone |= EZone.YAxis;
			if (TitleBounds.Contains(client_point)) zone |= EZone.Title;

			// The hit test point in chart space
			var chart_point = ClientToChart(client_point);

			return new HitTestResult(zone, client_point, chart_point, modifier_keys, mouse_btns, new List<HitTestResult.Hit>(), Scene.Camera);
		}

		/// <summary>Perform a hit test on the chart and all elements within the chart</summary>
		public HitTestResult HitTestCS(Point client_point, ModifierKeys modifier_keys, EMouseBtns mouse_btns, Func<Element, bool>? pred)
		{
			var result = HitTestZoneCS(client_point, modifier_keys, mouse_btns);

			// Find elements that overlap 'client_point'
			if (result.Zone.HasFlag(EZone.Chart))
			{
				var elements = pred != null ? Elements.Where(pred) : Elements;
				foreach (var elem in elements)
				{
					var hit = elem.HitTest(result.ChartPoint, result.ClientPoint, result.ModifierKeys, result.MouseBtns, Scene.Camera);
					if (hit != null)
						result.Hits.Add(hit);
				}

				// Sort the results by z order
				result.Hits.Sort((l, r) => -l.Element.PositionZ.CompareTo(r.Element.PositionZ));
			}

			return result;
		}

		/// <summary>Find the default range, then reset to the default range</summary>
		public void AutoRange(View3d.ESceneBounds who = View3d.ESceneBounds.All, EAxis axes = EAxis.Both)
		{
			// Allow the auto range to be handled by event
			var args = new AutoRangeEventArgs(who, axes);
			OnAutoRanging(args);

			// Get the bounding box, either from the event or from the scene bounds
			var bbox = args.Handled ? args.ViewBBox : Window.SceneBounds(who);

			// Check it's valid
			if (!bbox.IsValid || bbox.Radius == v4.Zero)
				throw new Exception($"View bounding box is invalid: {bbox}");

			// Position the camera to view the bounding box
			var up = Camera.AlignAxis != v4.Zero ? Camera.AlignAxis : Options.ResetUp;
			var fwd = !Math_.Parallel(Options.ResetForward, up) ? Options.ResetForward : Math_.Perpendicular(up);
			Camera.ResetView(bbox, fwd, up, dist: 0f, preserve_aspect: LockAspect, commit: true);

			// Set the desired pixel aspect ratio.
			if (Scene.RenderTarget != null)
			{
				// In WPF, controls don't get an 'owning window' until they are visible.
				// The view3d control uses a D3D11Image which sets it's back buffer once the window
				// handle is known. The size of the back buffer defines the size of the viewport.
				// So, if we don't have an owning window the viewport will have width/height = 0/0.
				var viewport = Scene.Window.Viewport;
				Scene.DesiredPixelAspect = Camera.Aspect * viewport.Height / viewport.Width;
			}

			// Set the axis range from the camera position
			SetRangeFromCamera();
			Invalidate();
		}

		/// <summary>Set the axis range based on the position of the camera and the field of view</summary>
		public void SetRangeFromCamera()
		{
			// The grid is always parallel to the image plane of the camera.
			// The camera forward vector points at the centre of the grid.

			// Project the camera to world vector into camera space to determine the centre of the X/Y axis. 
			var w2c = Math_.InvertFast(Scene.Camera.O2W);

			// The span of the X/Y axis is determined by the FoV and the focus point distance.
			// Set the projection mode before 'ViewArea' because it depends on the projection mode.
			Scene.Camera.Orthographic = Options.Orthographic;
			Scene.ActualPixelAspect = Scene.DesiredPixelAspect;
			var wh = Scene.Camera.ViewArea(Scene.Camera.FocusDist);

			// Set the axes range
			var xmin = -w2c.pos.x - wh.x * 0.5;
			var xmax = -w2c.pos.x + wh.x * 0.5;
			var ymin = -w2c.pos.y - wh.y * 0.5;
			var ymax = -w2c.pos.y + wh.y * 0.5;
			if (xmin < xmax) XAxis.Set(xmin, xmax);
			if (ymin < ymax) YAxis.Set(ymin, ymax);

			SaveNavCheckpoint();
		}

		/// <summary>Position the camera based on the axis range</summary>
		public void SetCameraFromRange()
		{
			// Set the new desired pixel aspect based on the
			// axis ranges and the current size of the scene.
			var sz = SceneBounds;
			if (sz.Width != 0 && sz.Height != 0)
			{
				Scene.DesiredPixelAspect = Range.Aspect * sz.Height / sz.Width;
				Scene.ActualPixelAspect = Scene.DesiredPixelAspect;
			}

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

			SaveNavCheckpoint();
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
				elem.PositionZ = HighestZ += Camera.FocusDist * 0.001f;
		}

		/// <summary>True if users are allowed to select elements on the diagram</summary>
		public bool AllowSelection { get; set; }

		/// <summary>True if users are allowed to drag the selected elements around on the chart</summary>
		public bool AllowElementDragging { get; set; }

		/// <summary>Return a string representation of a location on the chart. 'location' is in ChartControl client space</summary>
		public string LocationText(Point location)
		{
			if (SceneBounds.Width == 0 || SceneBounds.Height == 0)
				return string.Empty;

			var pt = ClientToChart(location);
			XAxis.GridLines(out var minx, out var maxx, out var stepx);
			YAxis.GridLines(out var miny, out var maxy, out var stepy);
			var xtick = XAxis.TickText(pt.X, stepx);
			var ytick = YAxis.TickText(pt.Y, stepy);
			return $"{xtick} , {ytick}";
		}

		/// <summary>Return the current mouse pointer location as a string</summary>
		public string PointerLocationText => LocationText(Mouse.GetPosition(this));

		/// <summary>The client space rectangle that contains the View3d part of the control</summary>
		public Rect SceneBounds => Scene.RenderArea(this);

		/// <summary>The client space area of the XAxis area of the chart</summary>
		public Rect XAxisBounds => Gui_.FindVisualChild<AxisPanel>(this, x => x.Axis == XAxis)?.RenderArea(this) ?? Rect_.Zero;

		/// <summary>The client space area of the YAxis area of the chart</summary>
		public Rect YAxisBounds => Gui_.FindVisualChild<AxisPanel>(this, x => x.Axis == YAxis)?.RenderArea(this) ?? Rect_.Zero;

		/// <summary>The client space area of the chart title</summary>
		public Rect TitleBounds => m_title_label.RenderArea(this);

		/// <summary>Per button current mouse operation</summary>
		public MouseOps MouseOperations
		{
			get => m_mouse_ops;
			private set
			{
				if (m_mouse_ops == value) return;
				Util.Dispose(ref m_mouse_ops!);
				m_mouse_ops = value;
			}
		}
		private MouseOps m_mouse_ops = null!;

		/// <summary>
		/// Select elements that are wholly within 'rect'. (rect is in chart space)
		/// If no modifier keys are down, elements not in 'rect' are deselected.
		/// If 'shift' is down, elements within 'rect' are selected in addition to the existing selection
		/// If 'ctrl' is down, elements within 'rect' are removed from the existing selection.</summary>
		public void SelectElements(Rect rect, ModifierKeys modifier_keys, EMouseBtns mouse_btns)
		{
			if (!AllowSelection)
				return;

			// Normalise the selection
			var c = rect.Centre();
			var s = rect.Size;
			var r = new BBox(new v4((float)c.X, (float)c.Y, 0f, 1f), new v4(Math_.Abs(new v2(0.5f * (float)s.Width, 0.5f * (float)s.Height)), 1f, 0f));

			// If the area of selection is less than the min drag distance, assume click selection
			var is_click = r.DiametreSq < Math_.Sqr(Options.MinDragPixelDistance);
			if (is_click)
			{
				var pt = ChartToClient(rect.Location);
				var hits = HitTestCS(pt, modifier_keys, mouse_btns, x => x.Enabled);

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

			InvalidateArrange();
		}

		/// <summary>Position the axis panels based on their 'Side' values</summary>
		private void PositionAxisPanels()
		{
			{// XAxis
				Grid.SetRow(m_xaxis_label, Options.XAxis.Side == Dock.Top ? 1 : 5);
				Grid.SetRow(m_xaxis_panel, Options.XAxis.Side == Dock.Top ? 2 : 4);
			}
			{// YAxis
				Grid.SetColumn(m_yaxis_label, Options.YAxis.Side == Dock.Left ? 0 : 4);
				Grid.SetColumn(m_yaxis_panel, Options.YAxis.Side == Dock.Left ? 1 : 3);
			}
		}

		/// <summary>The chart coordinates at the current mouse pointer location</summary>
		public string ValueAtPointer => LocationText(Mouse.GetPosition(this));

		/// <summary>Callback for formatting the text display by the tape measure</summary>
		public Func<Point, Point, TapeMeasure.LabelText> TapeMeasureStringFormat { get; set; } = DefaultTapeMeasureStringFormat;
		public static TapeMeasure.LabelText DefaultTapeMeasureStringFormat(Point beg, Point end)
		{
			var dx = end.X - beg.X;
			var dy = end.Y - beg.Y;
			return new TapeMeasure.LabelText
			{
				LabelX = $"{dx:F3}",
				LabelY = $"{dy:F3}",
				LabelD = $"{Math_.Len2(dx, dy):F3}",
			};
		}

		/// <summary>Binding helpers</summary>
		public Visibility XAxisLabelVisibility => Options.ShowAxes && XAxis.Label.HasValue() ? Visibility.Visible : Visibility.Collapsed;
		public Visibility YAxisLabelVisibility => Options.ShowAxes && YAxis.Label.HasValue() ? Visibility.Visible : Visibility.Collapsed;

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}

		/// <summary>Chart control graphics context id</summary>
		public static readonly Guid CtxId = new Guid("62D495BB-36D1-4B52-A067-1B7DB4011831");

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
			if (Elements.Ids.Count != Elements.Count)
				throw new Exception("Elements collection and Element Id lookup table don't match");
			foreach (var elem in Elements)
				if (Elements.Ids.GetOrDefault(elem.Id) != elem)
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
