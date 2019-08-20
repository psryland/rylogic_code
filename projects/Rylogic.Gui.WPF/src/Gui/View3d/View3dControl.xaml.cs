using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Rylogic.Attrib;
using Rylogic.Extn;
using Rylogic.Extn.Windows;
using Rylogic.Gfx;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class View3dControl : Image, IDisposable, INotifyPropertyChanged
	{
		// Notes:
		//  - This control subclasses 'Image' because the D3DImage is an 'ImageSource'

		private View3d.ReportErrorCB m_error_cb;

		static View3dControl()
		{
			BackgroundColorProperty = Gui_.DPRegister<View3dControl>(nameof(BackgroundColor), def: Colors.LightGray);
			View3d.LoadDll(throw_if_missing:false);
		}
		public View3dControl()
		{
			try
			{
				InitializeComponent();
				Stretch = Stretch.Fill;
				StretchDirection = StretchDirection.Both;
				UseLayoutRounding = true;
				Focusable = true;

				if (DesignerProperties.GetIsInDesignMode(this))
					return;

				// Initialise View3d in off-screen only mode (i.e. no window handle)
				View3d = View3d.Create();
				Window = new View3d.Window(View3d, IntPtr.Zero, new View3d.WindowOptions(m_error_cb = (ctx, msg) => OnReportError(new ReportErrorEventArgs(msg)), IntPtr.Zero));
				Camera.SetPosition(new v4(0, 0, 10, 1), v4.Origin, v4.YAxis);
				Camera.ClipPlanes(0.01f, 1000f, true);

				// Create a D3D11 off-screen render target image source
				Source = D3DImage = new D3D11Image();

				// Initialise commands
				ToggleOriginPoint = Command.Create(this, ToggleOriginPointInternal);
				ToggleFocusPoint = Command.Create(this, ToggleFocusPointInternal);
				ToggleOrthographic = Command.Create(this, ToggleOrthographicInternal);
				ToggleAntialiasing = Command.Create(this, ToggleAntialiasingInternal);
				ResetView = Command.Create(this, ResetViewInternal);
				SetBackgroundColour = Command.Create(this, SetBackgroundColourInternal);
				ShowMeasureTool = Command.Create(this, ShowMeasureToolInternal);
				ShowLightingUI = Command.Create(this, ShowLightingUIInternal);
				ShowObjectManager = Command.Create(this, ShowObjectManagerInternal);

				ViewPresets = new ListCollectionView(Enum<EViewPresets>.ValuesArray);
				AlignDirections = new ListCollectionView(Enum<EAlignDirections>.ValuesArray);

				// Set defaults
				BackgroundColor = Colors.LightGray;
				DesiredPixelAspect = 1;
				ClickTimeMS = 180;
				MouseNavigation = true;
				DefaultKeyboardShortcuts = true;
			}
			catch
			{
				Dispose();
				throw;
			}
		}
		public virtual void Dispose()
		{
			Source = null;
			D3DImage = null;
			Window = null;
			View3d = null;
		}
		protected override void OnRenderSizeChanged(SizeChangedInfo size_info)
		{
			m_resized = true;
			base.OnRenderSizeChanged(size_info);
			D3DImage.Invalidate();
			var resize_issue = ++m_resize_issue;
			Dispatcher_.BeginInvokeDelayed(() =>
			{
				if (resize_issue != m_resize_issue) return;
				Invalidate();
			}, TimeSpan.FromMilliseconds(10), System.Windows.Threading.DispatcherPriority.Background);
		}
		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			Keyboard.Focus(this);
			base.OnMouseDown(e);
		}
		private int m_resize_issue;
		private bool m_resized;

		/// <summary>View3d context reference</summary>
		public View3d View3d
		{
			get { return m_view3d; }
			private set
			{
				if (m_view3d == value) return;
				Util.Dispose(ref m_view3d);
				m_view3d = value;
			}
		}
		private View3d m_view3d;

		/// <summary>View3d window instance</summary>
		public View3d.Window Window
		{
			get { return m_window; }
			private set
			{
				if (m_window == value) return;
				if (m_window != null)
				{
					m_window.OnInvalidated -= HandleInvalidated;
					m_window.OnSettingsChanged -= HandleSettingsChanged;
					Util.Dispose(ref m_window);
				}
				m_window = value;
				if (m_window != null)
				{
					m_window.OnSettingsChanged += HandleSettingsChanged;
					m_window.OnInvalidated += HandleInvalidated;
				}

				// Handlers
				void HandleSettingsChanged(object sender, View3d.SettingChangeEventArgs e)
				{
					switch (e.Setting)
					{
					case View3d.EWindowSettings.BackgroundColour:
						BackgroundColor = Window.BackgroundColour.ToMediaColor();
						break;
					}
				}
				void HandleInvalidated(object sender, EventArgs e)
				{
					if (m_render_pending) return;
					m_render_pending = true;
					Dispatcher.BeginInvoke(Render);
				}
			}
		}
		private View3d.Window m_window;

		/// <summary>The camera used to view the scene</summary>
		public View3d.Camera Camera => Window.Camera;

		/// <summary>The D3D render target texture</summary>
		public View3d.Texture RenderTarget => D3DImage.RenderTarget;

		/// <summary>An interop object providing an off-screen render target</summary>
		private D3D11Image D3DImage
		{
			get { return m_d3d_image; }
			set
			{
				if (m_d3d_image == value) return;
				if (m_d3d_image != null)
				{
					Loaded -= OnLoaded;
					Unloaded -= OnUnloaded;
					m_d3d_image.RenderTargetChanged -= HandleRTChanged;
					Util.Dispose(ref m_d3d_image);
				}
				m_d3d_image = value;
				if (m_d3d_image != null)
				{
					Loaded += OnLoaded;
					Unloaded += OnUnloaded;
					m_d3d_image.RenderTargetChanged += HandleRTChanged;
				}

				void OnLoaded(object sender, EventArgs arg)
				{
					// When the control is loaded, attach the main win.
					// Detach when unloaded (not reliable though)
					var win = System.Windows.Window.GetWindow(this);
					D3DImage.WindowOwner = win != null ? new WindowInteropHelper(win).Handle : IntPtr.Zero;
					Invalidate();
				}
				void OnUnloaded(object sender, EventArgs arg)
				{
					// When the control is unloaded, detach the window
					D3DImage.WindowOwner = IntPtr.Zero;
				}
				void HandleRTChanged(object sender, EventArgs arg)
				{
					if (m_d3d_image.RenderTarget != null)
						OnRenderTargetChanged();

					Window.SetRT(D3DImage.RenderTarget);
					Window.SaveAsMainRT();
				}
			}
		}
		private D3D11Image m_d3d_image;

		/// <summary>Trigger a redraw of the view3d scene</summary>
		public void Invalidate() => Window.Invalidate();

		/// <summary>Set the background colour of the control</summary>
		public Color BackgroundColor
		{
			get { return (Color)GetValue(BackgroundColorProperty); }
			set { SetValue(BackgroundColorProperty, value); }
		}
		private void BackgroundColor_Changed(Color new_value)
		{
			Window.BackgroundColour = new Colour32(new_value.ToArgbU());
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BackgroundColor)));
		}
		public Brush BackgroundColorBrush => new SolidColorBrush(BackgroundColor);
		public static readonly DependencyProperty BackgroundColorProperty;

		/// <summary>The render target multi-sampling</summary>
		public int MultiSampling
		{
			get { return D3DImage.MultiSampling; }
			set
			{
				D3DImage.MultiSampling = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MultiSampling)));
			}
		}

		/// <summary>The time between mouse down->up that is considered a mouse click</summary>
		public int ClickTimeMS
		{
			get { return m_click_time_ms; }
			set
			{
				if (m_click_time_ms == value) return;
				m_click_time_ms = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ClickTimeMS)));
			}
		}
		private int m_click_time_ms;

		/// <summary>Enable/Disable default keyboard shortcuts</summary>
		public bool DefaultKeyboardShortcuts
		{
			get { return m_default_keyshortcuts; }
			set
			{
				using (Scope.Create(null, () => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DefaultKeyboardShortcuts)))))
				{
					KeyDown -= HandleKeyDown;
					if (!(m_default_keyshortcuts = value)) return;
					KeyDown += HandleKeyDown;

					void HandleKeyDown(object sender, KeyEventArgs args)
					{
						args.Handled = Window.TranslateKey(args.Key.ToKeyCode());
					}
				}
			}
		}
		private bool m_default_keyshortcuts;

		/// <summary>Hook up mouse navigation</summary>
		public bool MouseNavigation
		{
			get { return m_mouse_navigation; }
			set
			{
				using (Scope.Create(null, () => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MouseNavigation)))))
				{
					MouseDown -= OnMouseDown;
					MouseUp -= OnMouseUp;
					MouseMove -= OnMouseMove;
					MouseWheel -= OnMouseWheel;
					if (!(m_mouse_navigation = value)) return;
					MouseWheel += OnMouseWheel;
					MouseMove += OnMouseMove;
					MouseUp += OnMouseUp;
					MouseDown += OnMouseDown;
				}
			}
		}
		private bool m_mouse_navigation;

		/// <summary>The desired aspect ratio of pixels in the view</summary>
		public double DesiredPixelAspect
		{
			get { return m_desired_pixel_aspect; }
			set
			{
				if (DesiredPixelAspect == value) return;
				if (double.IsNaN(value))
					throw new ArgumentException("The desired pixel aspect cannot be NaN");
				if (double.IsInfinity(value))
					throw new ArgumentException("The desired pixel aspect cannot be infinite");
				if (value == 0)
					throw new ArgumentException("The desired pixel aspect cannot be 0");
				m_desired_pixel_aspect = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DesiredPixelAspect)));
			}
		}
		private double m_desired_pixel_aspect;

		/// <summary>The current pixel aspect ratio</summary>
		public double ActualPixelAspect
		{
			get
			{
				var sz = RenderSize;
				return Camera.Aspect * sz.Height / sz.Width;
			}
			set
			{
				var sz = RenderSize;
				if (sz.Width != 0 && sz.Height != 0)
					Camera.Aspect = (float)(value * sz.Width / sz.Height);
			}
		}

		/// <summary>Mouse navigation - public to allow users to forward mouse calls to us.</summary>
		public void OnMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (Window == null) return;
			if (CaptureMouse())
			{
				Cursor = Cursors.SizeAll;
				m_mouse_down_at = Environment.TickCount;
				if (Window.MouseNavigate(e.GetPosition(this).ToPointF(), e.ToMouseBtns(), true))
					Invalidate();
			}
		}
		public void OnMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (Window == null) return;
			if (IsMouseCaptured)
			{
				Cursor = Cursors.Arrow;
				if (Window.MouseNavigate(e.GetPosition(this).ToPointF(), e.ToMouseBtns(), View3d.ENavOp.None, true))
					Invalidate();

				ReleaseMouseCapture();
			}

			// Click detected
			if (Environment.TickCount - m_mouse_down_at < ClickTimeMS)
			{
				if (e.ChangedButton == MouseButton.Middle && e.MiddleButton == MouseButtonState.Released)
				{
					Camera.ResetZoom();
					Invalidate();
				}
			}
			else
			{
				e.Handled = true;
			}
		}
		public void OnMouseMove(object sender, MouseEventArgs e)
		{
			if (Window == null) return;
			if (IsMouseCaptured)
			{
				if (Window.MouseNavigate(e.GetPosition(this).ToPointF(), e.ToMouseBtns(), false))
					Invalidate();
			}
		}
		public void OnMouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (Window == null) return;
			if (Window.MouseNavigateZ(e.GetPosition(this).ToPointF(), e.ToMouseBtns(), e.Delta, true))
				Invalidate();
		}
		private int m_mouse_down_at;

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>Called whenever an error is generated in view3d</summary>
		public event EventHandler<ReportErrorEventArgs> ReportError;
		protected virtual void OnReportError(ReportErrorEventArgs e)
		{
			ReportError?.Invoke(this, e);
		}

		/// <summary>Default handling of render target changes. Set the viewport and camera aspect</summary>
		public event EventHandler RenderTargetChanged;
		protected virtual void OnRenderTargetChanged()
		{
			// Set the viewport to match the render target size
			Window.Viewport = new View3d.Viewport(0, 0, RenderTarget.Info.m_width, RenderTarget.Info.m_height);

			// Notify of a new render target. Don't directly subscribe to
			// D3DImage.RenderTargetChanged because that will leak references.
			RenderTargetChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Render</summary>
		private void Render()
		{
			// Don't make this public, use 'Invalidate'
			if (!m_render_pending) return;
			using (Scope.Create(null, () => m_render_pending = false))
			{
				// Ignore renders until the D3DImage has a render target
				if (D3DImage?.RenderTarget == null || !D3DImage.IsFrontBufferAvailable)
				{
					// 'Validate' the window so that future Invalidate() calls to trigger the call back.
					Window?.Validate();
					return;
				}

				// If the size has changed, update the back buffer
				if (m_resized)
				{
					D3DImage.SetRenderTargetSize(this);
					m_resized = false;
				}

				// Set the camera aspect to achieve the desired pixel aspect
				ActualPixelAspect = DesiredPixelAspect;

				// Allow objects to be added/removed from the scene
				OnBuildScene();

				// Render the scene
				Window.RestoreRT();
				Window.Render();
				Window.Present();

				// Notify that the D3DImage has changed
				D3DImage.Invalidate();
			}
		}
		private bool m_render_pending;

		// Allow objects to be added/removed from the scene
		public event EventHandler<BuildSceneEventArgs> BuildScene;
		protected virtual void OnBuildScene()
		{
			BuildScene?.Invoke(this, new BuildSceneEventArgs(Window));
		}

		/// <summary>Toggle visibility of the origin point</summary>
		public Command ToggleOriginPoint { get; }
		private void ToggleOriginPointInternal()
		{
			Window.OriginPointVisible = !Window.OriginPointVisible;
			Invalidate();
		}

		/// <summary>Toggle visibility of the focus point</summary>
		public Command ToggleFocusPoint { get; }
		private void ToggleFocusPointInternal()
		{
			Window.FocusPointVisible = !Window.FocusPointVisible;
			Invalidate();
		}

		/// <summary>Toggle between orthographic and perspective projection</summary>
		public Command ToggleOrthographic { get; }
		private void ToggleOrthographicInternal()
		{
			Camera.Orthographic = !Camera.Orthographic;
			Invalidate();
		}

		/// <summary>Toggle Antialiasing on or off</summary>
		public Command ToggleAntialiasing { get; }
		private void ToggleAntialiasingInternal()
		{
			MultiSampling = MultiSampling == 1 ? 4 : 1;
			Invalidate();
		}

		/// <summary>Reset the camera to the default view for the scene</summary>
		public Command ResetView { get; }
		private void ResetViewInternal()
		{
			Camera.ResetView();
			Invalidate();
		}

		/// <summary>Show a dialog for changing the background colour</summary>
		public Command SetBackgroundColour { get; }
		private void SetBackgroundColourInternal()
		{
			var bg = Window.BackgroundColour;
			var dlg = new ColourPickerUI
			{
				Title = "Background Colour",
				Owner = System.Windows.Window.GetWindow(this),
				Colour = bg,
			};
			dlg.ColorChanged += (s, a) =>
			{
				Window.BackgroundColour = a.Colour;
			};
			if (dlg.ShowDialog() == true)
				Window.BackgroundColour = dlg.Colour;
			else
				Window.BackgroundColour = bg;
		}

		/// <summary>Show a measurement tool window</summary>
		public Command ShowMeasureTool { get; }
		private void ShowMeasureToolInternal()
		{
			m_measurement_ui = m_measurement_ui ?? new View3dMeasurementUI(this);
			m_measurement_ui.Closed += (s, a) => { m_measurement_ui = null; Invalidate(); };
			m_measurement_ui.Show();
			m_measurement_ui.Focus();
		}
		private View3dMeasurementUI m_measurement_ui;

		/// <summary>Show the UI for changing the lighting</summary>
		public Command ShowLightingUI { get; }
		private void ShowLightingUIInternal()
		{
			var light = new View3d.Light(Window.LightProperties);
			light.PropertyChanged += (s, a) =>
			{
				Window.LightProperties = m_lighting_ui.Light;
				Invalidate();
			};

			m_lighting_ui = m_lighting_ui ?? new View3dLightingUI(this);
			m_lighting_ui.Closed += (s, a) => m_lighting_ui = null;
			m_lighting_ui.Light = light;
			m_lighting_ui.Show();
			m_lighting_ui.Focus();
		}
		private View3dLightingUI m_lighting_ui;

		/// <summary>Show the UI for objects in the scene</summary>
		public Command ShowObjectManager { get; }
		private void ShowObjectManagerInternal()
		{
			m_object_manager_ui = m_object_manager_ui ?? new View3dObjectManagerUI(this);
			m_object_manager_ui.Closed += (s, a) => m_object_manager_ui = null;
			m_object_manager_ui.Show();
			m_object_manager_ui.Focus();
		}
		private View3dObjectManagerUI m_object_manager_ui;

		/// <summary>Pre-set view directions</summary>
		public ICollectionView ViewPresets
		{
			get { return m_view_presets; }
			private set
			{
				if (m_view_presets == value) return;
				if (m_view_presets != null)
				{
					m_view_presets.CurrentChanged -= HandleViewChanged;
				}
				m_view_presets = value;
				if (m_view_presets != null)
				{
					m_view_presets.CurrentChanged += HandleViewChanged;
				}

				// Handler
				void HandleViewChanged(object sender, EventArgs e)
				{
					var view = (EViewPresets)ViewPresets.CurrentItem;
					if (view == EViewPresets.Current)
						return;

					var pos = Camera.FocusPoint;
					switch (view)
					{
					default: throw new Exception($"Unknown view pre-set: {view}");
					case EViewPresets.PosX: Camera.ResetView(+v4.XAxis); break;
					case EViewPresets.NegX: Camera.ResetView(-v4.XAxis); break;
					case EViewPresets.PosY: Camera.ResetView(+v4.YAxis); break;
					case EViewPresets.NegY: Camera.ResetView(-v4.YAxis); break;
					case EViewPresets.PosZ: Camera.ResetView(+v4.ZAxis); break;
					case EViewPresets.NegZ: Camera.ResetView(-v4.ZAxis); break;
					case EViewPresets.PosXYZ: Camera.ResetView(+v4.XAxis + v4.YAxis + v4.ZAxis); break;
					case EViewPresets.NegXYZ: Camera.ResetView(-v4.XAxis - v4.YAxis - v4.ZAxis); break;
					}
					Camera.FocusPoint = pos;
					Invalidate();
				};
			}
		}
		private ICollectionView m_view_presets;

		/// <summary>Directions to align the camera up-axis to</summary>
		public ICollectionView AlignDirections
		{
			get { return m_align_directions; }
			private set
			{
				if (m_align_directions == value) return;
				if (m_align_directions != null)
				{
					m_align_directions.CurrentChanged -= HandleAlignAxisChanged;
				}
				m_align_directions = value;
				if (m_align_directions != null)
				{
					// Set the current align direction
					if      (Camera.AlignAxis == +v4.XAxis) m_align_directions.MoveCurrentTo(EAlignDirections.PosX);
					else if (Camera.AlignAxis == -v4.XAxis) m_align_directions.MoveCurrentTo(EAlignDirections.NegX);
					else if (Camera.AlignAxis == +v4.YAxis) m_align_directions.MoveCurrentTo(EAlignDirections.PosY);
					else if (Camera.AlignAxis == -v4.YAxis) m_align_directions.MoveCurrentTo(EAlignDirections.NegY);
					else if (Camera.AlignAxis == +v4.ZAxis) m_align_directions.MoveCurrentTo(EAlignDirections.PosZ);
					else if (Camera.AlignAxis == -v4.ZAxis) m_align_directions.MoveCurrentTo(EAlignDirections.NegZ);
					else                                    m_align_directions.MoveCurrentTo(EAlignDirections.None);
					m_align_directions.CurrentChanged += HandleAlignAxisChanged;
				}

				// Handlers
				void HandleAlignAxisChanged(object sender, EventArgs e)
				{
					var align = (EAlignDirections)AlignDirections.CurrentItem;
					switch (align)
					{
					default: throw new Exception($"Unknown align axis direction: {align}");
					case EAlignDirections.None: Camera.AlignAxis = v4.Zero; break;
					case EAlignDirections.PosX: Camera.AlignAxis = +v4.XAxis; break;
					case EAlignDirections.NegX: Camera.AlignAxis = -v4.XAxis; break;
					case EAlignDirections.PosY: Camera.AlignAxis = +v4.YAxis; break;
					case EAlignDirections.NegY: Camera.AlignAxis = -v4.YAxis; break;
					case EAlignDirections.PosZ: Camera.AlignAxis = +v4.ZAxis; break;
					case EAlignDirections.NegZ: Camera.AlignAxis = -v4.ZAxis; break;
					}
					Invalidate();
				}
			}
		}
		private ICollectionView m_align_directions;

		/// <summary>Pre-set view directions</summary>
		public enum EViewPresets
		{
			[Desc("Current View")] Current,
			[Desc("+X Axis")] PosX,
			[Desc("-X Axis")] NegX,
			[Desc("+Y Axis")] PosY,
			[Desc("-Y Axis")] NegY,
			[Desc("+Z Axis")] PosZ,
			[Desc("-Z Axis")] NegZ,
			[Desc("+X,+Y,+Z Axis")] PosXYZ,
			[Desc("-X,-Y,-Z Axis")] NegXYZ,
		}

		/// <summary>Directions to align the camera up axis to</summary>
		public enum EAlignDirections
		{
			[Desc("No Align")] None,
			[Desc("+X Axis")] PosX,
			[Desc("-X Axis")] NegX,
			[Desc("+Y Axis")] PosY,
			[Desc("-Y Axis")] NegY,
			[Desc("+Z Axis")] PosZ,
			[Desc("-Z Axis")] NegZ,
		}

		#region EventArgs
		public class CustomContextMenuEventArgs : EventArgs
		{
			internal CustomContextMenuEventArgs(ContextMenu menu)
			{
				Menu = menu;
			}

			/// <summary>The context menu to be customised</summary>
			public ContextMenu Menu { get; }
		}
		public class ReportErrorEventArgs : EventArgs
		{
			internal ReportErrorEventArgs(string msg)
			{
				Message = msg;
			}

			/// <summary>Error message</summary>
			public string Message { get; }
		}
		public class BuildSceneEventArgs : EventArgs
		{
			public BuildSceneEventArgs(View3d.Window window)
			{
				Window = window;
			}

			/// <summary>The chart panel</summary>
			public View3d.Window Window { get; }

			/// <summary>Current camera position</summary>
			public View3d.Camera Camera => Window.Camera;
		}
		#endregion
	}
}
