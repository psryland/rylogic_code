using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class View3dControl : Image, IDisposable, INotifyPropertyChanged
	{
		private View3d.ReportErrorCB m_error_cb;

		static View3dControl()
		{
			View3d.LoadDll(throw_if_missing:false);
		}
		public View3dControl()
		{
			try
			{
				InitializeComponent();
				Stretch = Stretch.Fill;

				if (DesignerProperties.GetIsInDesignMode(this))
					return;

				// Initialise View3d in off-screen only mode (i.e. no window handle)
				View3d = View3d.Create();
				Window = new View3d.Window(View3d, IntPtr.Zero, new View3d.WindowOptions(m_error_cb = (ctx, msg) => OnReportError(new ReportErrorEventArgs(msg)), IntPtr.Zero));
				Camera.SetPosition(new v4(0, 0, 10, 1), v4.Origin, v4.YAxis);
				Camera.ClipPlanes(0.01f, 1000f, true);

				// Create a D3D11 off-screen render target image source
				Source = D3DImage = new D3D11Image();

				// Set defaults
				BackgroundColour = Colour32.Gray;
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
		public void Dispose()
		{
			Source = null;
			D3DImage = Util.Dispose(D3DImage);
			Window = Util.Dispose(Window);
			View3d = Util.Dispose(View3d);
		}
		protected override void OnRenderSizeChanged(SizeChangedInfo size_info)
		{
			base.OnRenderSizeChanged(size_info);
			m_resized = true;
			Invalidate();
		}
		private bool m_resized;

		/// <summary>View3d context instance</summary>
		public View3d View3d { get; private set; }

		/// <summary>View3d window instance</summary>
		public View3d.Window Window { get; private set; }

		/// <summary>The camera used to view the scene</summary>
		public View3d.Camera Camera => Window.Camera;

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
					D3DImage.WindowOwner = new WindowInteropHelper(win).Handle;
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
					{
						var info = m_d3d_image.RenderTarget.Info;
						Window.Viewport = new View3d.Viewport(0, 0, info.m_width, info.m_height);
						Camera.Aspect = Math_.Div(info.m_width, info.m_height, 1f);
					}

					Window.SetRT(D3DImage.RenderTarget);
					Window.SaveAsMainRT();
				}
			}
		}
		private D3D11Image m_d3d_image;

		/// <summary>Trigger a redraw of the view3d scene</summary>
		public void Invalidate()
		{
			if (m_render_pending) return;
			m_render_pending = true;
			OnInvalidated();
		}
		protected virtual void OnInvalidated()
		{
			Dispatcher.BeginInvoke(Render);
		}
		private bool m_render_pending;

		/// <summary>Set the background colour of the control</summary>
		public Colour32 BackgroundColour
		{
			get { return Window.BackgroundColour; }
			set
			{
				if (BackgroundColour == value) return;
				Window.BackgroundColour = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BackgroundColour)));
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
						args.Handled = Window.TranslateKey((KeyCodes)args.Key);
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
				if (e.MiddleButton == MouseButtonState.Pressed || (e.LeftButton == MouseButtonState.Pressed && e.RightButton == MouseButtonState.Pressed))
				{
					Camera.ResetZoom();
					Invalidate();
				}
				else if (e.RightButton == MouseButtonState.Pressed)
				{
					ShowContextMenu();
				}
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

		/// <summary>Show the view3D context menu</summary>
		public void ShowContextMenu()
		{
			if (Window == null)
				return;

			// Create a context menu
			var cmenu = new ContextMenu();
			cmenu.Closed += (s, a) => Invalidate();

			{// View
				var view_menu = cmenu.Items.Add2(new MenuItem { Header = "View" });
				{// Show focus
					var opt = view_menu.Items.Add2(new MenuItem { Header = "Show Focus" });
					opt.IsChecked = Window.FocusPointVisible;
					opt.Click += (s, a) =>
					{
						Window.FocusPointVisible = !Window.FocusPointVisible;
						Invalidate();
					};
				}
				{// Show Origin
					var opt = view_menu.Items.Add2(new MenuItem { Header = "Show Origin" });
					opt.IsChecked = Window.OriginPointVisible;
					opt.Click += (s, a) =>
					{
						Window.OriginPointVisible = !Window.OriginPointVisible;
						Invalidate();
					};
				}
				{// Show coords
				}
				{// Axis Views
					var view_options_menu = view_menu.Items.Add2(new MenuItem { Header = "Views" });
					//{
					//	view_options_menu.Items.Add("Views");
					//	view_options.Items.Add("Axis +X");
					//	view_options.Items.Add("Axis -X");
					//	view_options.Items.Add("Axis +Y");
					//	view_options.Items.Add("Axis -Y");
					//	view_options.Items.Add("Axis +Z");
					//	view_options.Items.Add("Axis -Z");
					//	view_options.Items.Add("Axis -X,-Y,-Z");
					//	view_options.SelectedIndex = 0;
					//	view_options.SelectedIndexChanged += delegate
					//	{
					//		var pos = Camera.FocusPoint;
					//		switch (view_options.SelectedIndex)
					//		{
					//		case 1: Camera.ResetView(v4.XAxis); break;
					//		case 2: Camera.ResetView(-v4.XAxis); break;
					//		case 3: Camera.ResetView(v4.YAxis); break;
					//		case 4: Camera.ResetView(-v4.YAxis); break;
					//		case 5: Camera.ResetView(v4.ZAxis); break;
					//		case 6: Camera.ResetView(-v4.ZAxis); break;
					//		case 7: Camera.ResetView(-v4.XAxis - v4.YAxis - v4.ZAxis); break;
					//		}
					//		Camera.FocusPoint = pos;
					//		Refresh();
					//	};
					//}
				}
				{// Object Manager UI
				 //var obj_mgr_ui = new MenuItem{ Header = "Object Manager" };
				 //view_menu.Items.Add(obj_mgr_ui);
				 //obj_mgr_ui.Click += (s,a) => Window.ShowObjectManager(true);
				}
			}
			{// Navigation
				var rdr_menu = cmenu.Items.Add2(new MenuItem { Header = "Navigation" });
				{// Reset View
					var opt = rdr_menu.Items.Add2(new MenuItem { Header = "Reset View" });
					opt.Click += (s, a) =>
					{
						Camera.ResetView();
						Invalidate();
					};
				}
				{// Align to
					var align_menu = new MenuItem { Header = "Align" };
					rdr_menu.Items.Add(align_menu);
					//{
					//	var align_options = new ToolStripComboBox("Aligns") { DropDownStyle = ComboBoxStyle.DropDownList };
					//	align_menu.Items.Add(align_options);
					//	align_options.Items.Add("None");
					//	align_options.Items.Add("X");
					//	align_options.Items.Add("Y");
					//	align_options.Items.Add("Z");
					//
					//	var axis = Camera.AlignAxis;
					//	if (Math_.FEql(axis, v4.XAxis)) align_options.SelectedIndex = 1;
					//	else if (Math_.FEql(axis, v4.YAxis)) align_options.SelectedIndex = 2;
					//	else if (Math_.FEql(axis, v4.ZAxis)) align_options.SelectedIndex = 3;
					//	else align_options.SelectedIndex = 0;
					//	align_options.SelectedIndexChanged += delegate
					//	{
					//		switch (align_options.SelectedIndex)
					//		{
					//		default: Camera.AlignAxis = v4.Zero; break;
					//		case 1: Camera.AlignAxis = v4.XAxis; break;
					//		case 2: Camera.AlignAxis = v4.YAxis; break;
					//		case 3: Camera.AlignAxis = v4.ZAxis; break;
					//		}
					//		Refresh();
					//	};
					//}
				}
				{// Motion lock
				}
				{// Orbit
				 //	var orbit_menu = new MenuItem{ Header = "Orbit" };
				 //	rdr_menu.Items.Add2(orbit_menu);
				 //	orbit_menu.Click += delegate {};
				}
			}
			{// Tools
				var tools_menu = cmenu.Items.Add2(new MenuItem { Header = "Tools" });
				{// Measure
					var opt = tools_menu.Items.Add2(new MenuItem { Header = "Measure..." });
					opt.Click += (s, a) =>
					{
						Window.ShowMeasureTool = true;
					};
				}
				//{// Angle
				//	var option = new ToolStripMenuItem { Text = "Angle..." };
				//	tools_menu.Items.Add2(option);
				//	option.Click += delegate { Window.ShowAngleTool = true; };
				//}
			}
			{// Rendering
				var rdr_menu = cmenu.Items.Add2(new MenuItem { Header = "Rendering" });
				{// Solid/Wireframe/Solid+Wire
					var opt = rdr_menu.Items.Add2(new ComboBox { /*DropDownStyle = ComboBoxStyle.DropDownList*/ });
					opt.Items.AddRange(Enum<View3d.EFillMode>.Names.Cast<object>().ToArray());
					opt.SelectedIndex = (int)Window.FillMode;
					//opt.SelectedIndexChanged += (s, a) =>
					//{
					//	Window.FillMode = (View3d.EFillMode)option.SelectedIndex;
					//	Refresh();
					//};
				}
				{// Render2D
					var opt = rdr_menu.Items.Add2(new MenuItem { Header = Window.Camera.Orthographic ? "Perspective" : "Orthographic" });
					opt.Click += (s, a) =>
					{
						Window.Camera.Orthographic = !Window.Camera.Orthographic;
						Invalidate();
					};
				}
				{// Lighting...
					var lighting_menu = rdr_menu.Items.Add2(new MenuItem { Header = "Lighting..." });
					lighting_menu.Click += (s, a) =>
					{
						Window.ShowLightingDlg();
					};
				}
				{// Background colour
				 //var bk_colour_menu = new MenuItem{ Header = "Background Colour" };
				 //rdr_menu.Items.Add2(bk_colour_menu);
				 //{
				 //	var opt = bk_colour_menu.Items.Add2(new Button { });
				 //	opt.Background = new SolidColorBrush(Window.BackgroundColour.ToMediaColor());
				 //	opt. += delegate { Window.BackgroundColour = option.BackColor; Refresh(); };
				 //	opt.Click += (s,a) =>
				 //	{
				 //		var cd = new ColourUI();
				 //		if (cd.ShowDialog() == DialogResult.OK)
				 //			option.BackColor = cd.Colour;
				 //	};
				 //}
				}
			}

			// Allow users to add custom menu options to the context menu
			// Do this last so that users have the option of removing options they don't want displayed
			OnCustomiseContextMenu(new CustomContextMenuEventArgs(cmenu));

			// Show the menu
			cmenu.PlacementTarget = this;
			cmenu.Placement = PlacementMode.MousePoint;
			cmenu.IsOpen = true;
		}

		/// <summary>Event called just before displaying the context menu to allow users to add custom options to the menu</summary>
		public event EventHandler<CustomContextMenuEventArgs> CustomiseContextMenu;
		protected virtual void OnCustomiseContextMenu(CustomContextMenuEventArgs e)
		{
			CustomiseContextMenu?.Invoke(this, e);
		}

		/// <summary>Called whenever an error is generated in view3d</summary>
		public event EventHandler<ReportErrorEventArgs> ReportError;
		protected virtual void OnReportError(ReportErrorEventArgs e)
		{
			ReportError?.Invoke(this, e);
		}

		/// <summary>Render</summary>
		private void Render()
		{
			using (Scope.Create(null, () => m_render_pending = false))
			{
				// Ignore renders until the D3DImage has a render target
				if (D3DImage.RenderTarget == null)
					return;

				// If the size has changed, update the back buffer
				if (m_resized)
				{
					D3DImage.SetRenderTargetSize(this);
					m_resized = false;
				}

				// Allow objects to be added/removed from the scene
				OnBuildScene();

				using (D3DImage.LockScope())
				{
					// Render the scene
					Window.RestoreRT();
					Window.Render();
					Window.Present();

					// Notify that the D3DImage has changed
					D3DImage.Invalidate();
				}
			}
		}

		// Allow objects to be added/removed from the scene
		public event EventHandler BuildScene;
		protected virtual void OnBuildScene()
		{
			BuildScene?.Invoke(this, EventArgs.Empty);
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;

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
		#endregion
	}
}
