using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Rylogic.Extn;
using Rylogic.Extn.Windows;
using Rylogic.Gfx;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class View3dControl :Image, IDisposable, INotifyPropertyChanged, IView3dCMenuContext
	{
		// Notes:
		//  - This control subclasses 'Image' because the D3DImage is an 'ImageSource'
		//  - View3dControl does not have a 'Settings' class, state changes are immediate
		//    and storing the state is left to the caller. (Unlike ChartControl).

		static View3dControl()
		{
			View3d.LoadDll(throw_if_missing: false);
		}
		public View3dControl()
		{
			InitializeComponent();
			Stretch = Stretch.Fill;
			StretchDirection = StretchDirection.Both;
			UseLayoutRounding = true;
			Focusable = true;

			if (DesignerProperties.GetIsInDesignMode(this))
				return;

			try
			{
				// Initialise View3d in off-screen only mode (i.e. no window handle)
				View3d = View3d.Create();
				Window = new View3d.Window(View3d, IntPtr.Zero);
				Window.Error += (s,a) => OnReportError(new ReportErrorEventArgs(a.Message));
				Camera.Lookat(new v4(0, 0, 10, 1), v4.Origin, v4.YAxis);
				Camera.ClipPlanes(0.01f, 1000f, true);

				// Create a D3D11 off-screen render target image source
				Source = D3DImage = new D3D11Image();

				// Set defaults
				BackgroundColour = Window.BackgroundColour;
				DesiredPixelAspect = 1;
				ClickTimeMS = 180;
				MouseNavigation = true;
				DefaultKeyboardShortcuts = true;
				ViewPreset = EViewPreset.Current;

				// Default context menu implementation
				if (ContextMenu != null && ContextMenu.DataContext == null)
					ContextMenu.DataContext = this;

				InitCommands();
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
			++m_resize_issue;
			Disposing?.Invoke(this, EventArgs.Empty);
			Source = null;
			D3DImage = null!;
			Window = null!;
			View3d = null!;
		}
		protected override void OnRenderSizeChanged(SizeChangedInfo size_info)
		{
			m_resized = true;
			base.OnRenderSizeChanged(size_info);
			D3DImage.Invalidate();

			// Invalidate after the last resize
			if (!m_resize_invalidate_pending)
			{
				m_resize_invalidate_pending = true;
				var resize_issue = ++m_resize_issue;
				Dispatcher_.BeginInvokeDelayed(FinalInvalidate, TimeSpan.FromMilliseconds(1), DispatcherPriority.Background);
				void FinalInvalidate()
				{
					m_resize_invalidate_pending = false;
					if (resize_issue != m_resize_issue || Window == null) return;
					Invalidate();
				}
			}
		}
		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			Keyboard.Focus(this);
			base.OnMouseDown(e);
		}
		private bool m_resize_invalidate_pending;
		private int m_resize_issue;
		private bool m_resized;

		/// <summary>View3d context reference</summary>
		public View3d View3d
		{
			get => m_view3d;
			private set
			{
				if (m_view3d == value) return;
				Util.Dispose(ref m_view3d!);
				m_view3d = value;
			}
		}
		private View3d m_view3d = null!;

		/// <summary>View3d window instance</summary>
		public View3d.Window Window
		{
			get => m_window;
			private set
			{
				if (m_window == value) return;
				if (m_window != null)
				{
					m_window.OnInvalidated -= HandleInvalidated;
					m_window.OnSettingsChanged -= HandleSettingsChanged;
					Util.Dispose(ref m_window!);
				}
				m_window = value;
				if (m_window != null)
				{
					m_window.OnSettingsChanged += HandleSettingsChanged;
					m_window.OnInvalidated += HandleInvalidated;
				}

				// Handlers
				void HandleSettingsChanged(object? sender, View3d.SettingChangeEventArgs e)
				{
					if (ContextMenu?.DataContext is IView3dCMenu cmenu)
					{
						if (Bit.AllSet(e.Setting, View3d.ESettings.General))
						{
							if (Bit.AllSet(e.Setting, View3d.ESettings.General_OriginPointVisible))
								cmenu.NotifyPropertyChanged(nameof(IView3dCMenu.OriginPointVisible));
							if (Bit.AllSet(e.Setting, View3d.ESettings.General_FocusPointVisible))
								cmenu.NotifyPropertyChanged(nameof(IView3dCMenu.FocusPointVisible));
							if (Bit.AllSet(e.Setting, View3d.ESettings.General_SelectionBoxVisible))
								cmenu.NotifyPropertyChanged(nameof(IView3dCMenu.SelectionBoxVisible));
						}
						if (Bit.AllSet(e.Setting, View3d.ESettings.Scene))
						{
							if (Bit.AllSet(e.Setting, View3d.ESettings.Scene_BackgroundColour))
								cmenu.NotifyPropertyChanged(nameof(IView3dCMenu.BackgroundColour));
							if (Bit.AllSet(e.Setting, View3d.ESettings.Scene_Multisampling))
								cmenu.NotifyPropertyChanged(nameof(IView3dCMenu.Antialiasing));
							if (Bit.AllSet(e.Setting, View3d.ESettings.Scene_FilllMode))
								cmenu.NotifyPropertyChanged(nameof(IView3dCMenu.FillMode));
							if (Bit.AllSet(e.Setting, View3d.ESettings.Scene_CullMode))
								cmenu.NotifyPropertyChanged(nameof(IView3dCMenu.CullMode));
						}
						if (Bit.AllSet(e.Setting, View3d.ESettings.Camera))
						{
							if (Bit.AllSet(e.Setting, View3d.ESettings.Camera_Orthographic))
								cmenu.NotifyPropertyChanged(nameof(IView3dCMenu.Orthographic));
							if (Bit.AllSet(e.Setting, View3d.ESettings.Camera_AlignAxis))
								cmenu.NotifyPropertyChanged(nameof(IView3dCMenu.AlignDirection));
						}
						if (Bit.AllSet(e.Setting, View3d.ESettings.Lighting))
						{
						}
						if (Bit.AllSet(e.Setting, View3d.ESettings.Diagnostics))
						{
							if (Bit.AllSet(e.Setting, View3d.ESettings.Diagnostics_BBoxesVisible))
								cmenu.NotifyPropertyChanged(nameof(IView3dCMenu.BBoxesVisible));
							if (Bit.AllSet(e.Setting, View3d.ESettings.Diagnostics_NormalsLength))
								cmenu.NotifyPropertyChanged(nameof(IView3dCMenu.NormalsLength));
							if (Bit.AllSet(e.Setting, View3d.ESettings.Diagnostics_NormalsColour))
								cmenu.NotifyPropertyChanged(nameof(IView3dCMenu.NormalsColour));
							if (Bit.AllSet(e.Setting, View3d.ESettings.Diagnostics_FillModePointsSize))
								cmenu.NotifyPropertyChanged(nameof(IView3dCMenu.FillModePointsSize));
						}
					}
					if (Bit.AllSet(e.Setting, View3d.ESettings.Camera_AlignAxis))
					{
						AlignDirection = AlignDirection_.FromAxis(Camera.AlignAxis);
						NotifyPropertyChanged(nameof(AlignDirection));
					}
					if (Bit.AllSet(e.Setting, View3d.ESettings.Scene_BackgroundColour))
					{
						BackgroundColour = Window.BackgroundColour;
						NotifyPropertyChanged(nameof(BackgroundColour));
					}
				}
				void HandleInvalidated(object? sender, EventArgs e)
				{
					if (m_render_pending) return;
					m_render_pending = true;
					Dispatcher.BeginInvoke(new Action(Render));
				}
			}
		}
		private View3d.Window m_window = null!;

		/// <summary>The camera used to view the scene</summary>
		public View3d.Camera Camera => Window.Camera;

		/// <summary>The D3D render target texture</summary>
		public View3d.Texture? RenderTarget => D3DImage.RenderTarget;

		/// <summary>An interop object providing an off-screen render target</summary>
		private D3D11Image D3DImage
		{
			get => m_d3d_image;
			set
			{
				if (m_d3d_image == value) return;
				if (m_d3d_image != null)
				{
					Loaded -= OnLoaded;
					Unloaded -= OnUnloaded;
					m_d3d_image.RenderTargetChanged -= HandleRTChanged;
					Util.Dispose(ref m_d3d_image!);
				}
				m_d3d_image = value;
				if (m_d3d_image != null)
				{
					Loaded += OnLoaded;
					Unloaded += OnUnloaded;
					m_d3d_image.RenderTargetChanged += HandleRTChanged;
				}

				void OnLoaded(object? sender, EventArgs arg)
				{
					// When the control is loaded, attach the main win.
					// Detach when unloaded (not reliable though)
					var win = System.Windows.Window.GetWindow(this);
					D3DImage.WindowOwner = win != null ? new WindowInteropHelper(win).Handle : IntPtr.Zero;
					Invalidate();
				}
				void OnUnloaded(object? sender, EventArgs arg)
				{
					// When the control is unloaded, detach the window
					D3DImage.WindowOwner = IntPtr.Zero;
				}
				void HandleRTChanged(object? sender, EventArgs arg)
				{
					if (m_d3d_image.RenderTarget != null)
						OnRenderTargetChanged();

					Window.SetRT(D3DImage.RenderTarget, null, true);
				}
			}
		}
		private D3D11Image m_d3d_image = null!;

		/// <summary>Trigger a redraw of the view3d scene</summary>
		public void Invalidate() => Window.Invalidate();

		/// <summary>The render target multi-sampling</summary>
		public int MultiSampling
		{
			get => D3DImage.MultiSampling;
			set
			{
				// Since, in WPF, we're rendering to an off-screen render target, multisampling
				// is done by changing the render target size, not the Dx11 multisampling settings.
				// This means the normal View3d.Window multisampling setting isn't changed.
				D3DImage.MultiSampling = value;
				NotifyPropertyChanged(nameof(MultiSampling));
				NotifyPropertyChanged(nameof(Antialiasing));
				if (ContextMenu?.DataContext is IView3dCMenu cmenu)
					cmenu.NotifyPropertyChanged(nameof(IView3dCMenu.Antialiasing));
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
				NotifyPropertyChanged(nameof(ClickTimeMS));
			}
		}
		private int m_click_time_ms;

		/// <summary>Enable/Disable default keyboard shortcuts</summary>
		public bool DefaultKeyboardShortcuts
		{
			get { return m_default_keyshortcuts; }
			set
			{
				using (Scope.Create(null, () => NotifyPropertyChanged(nameof(DefaultKeyboardShortcuts))))
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
			get => m_mouse_navigation;
			set
			{
				using (Scope.Create(null, () => NotifyPropertyChanged(nameof(MouseNavigation))))
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
			get => m_desired_pixel_aspect;
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
				NotifyPropertyChanged(nameof(DesiredPixelAspect));
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
				{
					var aspect = (float)(value * sz.Width / sz.Height);
					if (!Math_.FEql(Camera.Aspect, aspect))
						Camera.Aspect = aspect;
				}
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

		/// <summary>Called whenever an error is generated in view3d</summary>
		public event EventHandler<ReportErrorEventArgs>? ReportError;
		protected virtual void OnReportError(ReportErrorEventArgs e)
		{
			ReportError?.Invoke(this, e);
		}

		/// <summary>Default handling of render target changes. Set the viewport and camera aspect</summary>
		public event EventHandler? RenderTargetChanged;
		protected virtual void OnRenderTargetChanged()
		{
			if (RenderTarget == null)
				return;

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
			using var render_pending = Scope.Create(null, () => m_render_pending = false);
		
			// Ignore renders until we have a non-zero size, and the D3DImage has a render target
			if (ActualWidth == 0 || ActualHeight == 0 ||
				D3DImage?.RenderTarget == null || !D3DImage.IsFrontBufferAvailable)
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
			try { OnBuildScene(); }
			catch (OperationCanceledException) { }
			catch (Exception ex) { OnReportError(new ReportErrorEventArgs($"Error during build scene: {ex.Message}")); }

			// Render the scene
			Window.RestoreRT();
			Window.Render();
			Window.Present();

			// Notify that the D3DImage has changed
			D3DImage.Invalidate();
		}
		private bool m_render_pending;

		/// <summary>Allow objects to be added/removed from the scene</summary>
		public event EventHandler<BuildSceneEventArgs>? BuildScene;
		protected virtual void OnBuildScene()
		{
			BuildScene?.Invoke(this, new BuildSceneEventArgs(Window));
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string prop_name)
		{
			// Note: Notify is called from the SettingsChanged handler, not when the property is changed
			// because this catches all sources of a property changing, not just when it is explicity set.
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}

		/// <summary>Raised when the control is disposed</summary>
		public event EventHandler? Disposing;

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
