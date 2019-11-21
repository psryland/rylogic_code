using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Interop.Win32;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WinForms
{
	// Notes:
	//  Keyboard events:
	//  Users should hook up to the KeyDown event with the handlers they want.
	//  The View3D class has public methods for some common behaviours
	//  E.g. "Refresh"
	//   view3d.KeyDown += (s,a) =>
	//       {
	//           if (e.KeyCode == Keys.F5)
	//               ReloadScene();
	//       };

	public class View3dControl :UserControl
	{
		public View3dControl()
			:this(string.Empty)
		{}
		public View3dControl(string name)
			:this(name, gdi_compatible_backbuffer:false)
		{}
		public View3dControl(string name, bool gdi_compatible_backbuffer)
		{
			try
			{
				BackColor = Color.Gray;
				if (this.IsInDesignMode()) return;
				SetStyle(ControlStyles.Selectable, false);

				m_view3d = View3d.Create();
				var opts = new View3d.WindowOptions(HandleReportError, IntPtr.Zero, gdi_compatible_backbuffer) { DbgName = name ?? string.Empty };
				m_window = new View3d.Window(View3d, Handle, opts);

				InitializeComponent();

				Name = name;
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
		protected override void Dispose(bool disposing)
		{
			ShowMeasurementUI = false;
			Util.BreakIf(m_window != null && Util.IsGCFinalizerThread);
			Util.BreakIf(m_view3d != null && Util.IsGCFinalizerThread);
			Util.Dispose(ref m_window);
			Util.Dispose(ref m_view3d);
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnInvalidated(InvalidateEventArgs e)
		{
			Window?.Invalidate();
			base.OnInvalidated(e);
		}

		/// <summary>The underlying interop wrapper</summary>
		public View3d View3d
		{
			[DebuggerStepThrough] get { return m_view3d; }
		}
		private View3d m_view3d;

		/// <summary>The binding to this control</summary>
		public View3d.Window Window
		{
			[DebuggerStepThrough] get { return m_window; }
		}
		private View3d.Window m_window;

		/// <summary>The main camera</summary>
		public View3d.Camera Camera
		{
			[DebuggerStepThrough] get { return Window.Camera; }
		}

		/// <summary>Called whenever an error is generated in view3d</summary>
		public event EventHandler<ReportErrorEventArgs> ReportError;

		/// <summary>Event notifying whenever rendering settings have changed</summary>
		[Obsolete("Sign up to the window one directly")] public event EventHandler<View3d.SettingChangeEventArgs> OnSettingsChanged
		{
			add    { Window.OnSettingsChanged += value; }
			remove { Window.OnSettingsChanged -= value; }
		}

		/// <summary>Event called just before displaying the context menu to allow users to add custom options to the menu</summary>
		public event EventHandler<CustomContextMenuEventArgs> CustomiseContextMenu;

		/// <summary>Set the background colour of the control</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override Color BackColor
		{
			get { return base.BackColor; }
			set
			{
				base.BackColor = value;
				
				// Need to handle Window == null because the designer still sets the base member
				if (Window != null)
					Window.BackgroundColour = value;
			}
		}

		/// <summary>Enable/Disable default keyboard shortcuts</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool DefaultKeyboardShortcuts
		{
			set
			{
				KeyDown -= HandleKeyDown;
				if (!value) return;
				KeyDown += HandleKeyDown;

				void HandleKeyDown(object sender, KeyEventArgs args)
				{
					args.Handled = Window.TranslateKey((EKeyCodes)args.KeyCode);
				}
			}
		}

		/// <summary>The time between mouse down->up that is considered a mouse click</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int ClickTimeMS { get; set; }

		/// <summary>Hook up mouse navigation</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool MouseNavigation
		{
			get { return m_mouse_navigation; }
			set
			{
				// Remove first to prevent duplicate handlers
				MouseDown        -= OnMouseDown;
				MouseUp          -= OnMouseUp;
				MouseMove        -= OnMouseMove;
				MouseWheel       -= OnMouseWheel;
				MouseDoubleClick -= OnMouseDblClick;
				if (value)
				{
					MouseDown        += OnMouseDown;
					MouseUp          += OnMouseUp;
					MouseMove        += OnMouseMove;
					MouseWheel       += OnMouseWheel;
					MouseDoubleClick += OnMouseDblClick;
				}
				m_mouse_navigation = value;
			}
		}
		private bool m_mouse_navigation;

		/// <summary>Mouse navigation - public to allow users to forward mouse calls to us.</summary>
		public void OnMouseDown(object sender, MouseEventArgs e)
		{
			if (Window == null) return;
			Cursor = Cursors.SizeAll;
			Capture = true;
			m_mouse_down_at = Environment.TickCount;
			if (Window.MouseNavigate(e.Location, e.Button.ToMouseBtns(), true))
				Invalidate();
		}
		public void OnMouseUp(object sender, MouseEventArgs e)
		{
			if (Window == null) return;
			Cursor = Cursors.Default;
			Capture = false;
			if (Window.MouseNavigate(e.Location, e.Button.ToMouseBtns(), View3d.ENavOp.None, true))
				Invalidate();

			// Short clicks bring up the context menu
			if (e.Button == MouseButtons.Right && Environment.TickCount - m_mouse_down_at < ClickTimeMS)
				ShowContextMenu();
		}
		public void OnMouseMove(object sender, MouseEventArgs e)
		{
			if (Window == null) return;
			if (Window.MouseNavigate(e.Location, e.Button.ToMouseBtns(), false))
				Invalidate();
		}
		public void OnMouseWheel(object sender, MouseEventArgs e)
		{
			if (Window == null) return;
			if (Window.MouseNavigateZ(e.Location, e.Button.ToMouseBtns(), e.Delta, true))
				Invalidate();
		}
		public void OnMouseDblClick(object sender, MouseEventArgs e)
		{
			if (Window == null) return;
			if (Bit.AllSet((int)e.Button, (int)MouseButtons.Middle) ||
				Bit.AllSet((int)e.Button, (int)(MouseButtons.Left|MouseButtons.Right)))
			{
				Camera.ResetZoom();
				Invalidate();
			}
		}
		private int m_mouse_down_at;

		/// <summary>Show the view3D context menu</summary>
		public void ShowContextMenu()
		{
			if (Window == null) return;
			var context_menu = new ContextMenuStrip();
			context_menu.Closed += (s,a) => Refresh();

			{// View
				var view_menu = context_menu.Items.Add2(new ToolStripMenuItem("View") { Name = CMenuItems.View });
				{// Show focus
					var opt = view_menu.DropDownItems.Add2(new ToolStripMenuItem("Show Focus") { Name = CMenuItems.ViewMenu.ShowFocus });
					opt.Checked = Window.FocusPointVisible;
					opt.Click += (s,a) =>
					{
						Window.FocusPointVisible = !Window.FocusPointVisible;
						Refresh();
					};
				}
				{// Show Origin
					var opt = view_menu.DropDownItems.Add2(new ToolStripMenuItem("Show Origin") { Name = CMenuItems.ViewMenu.ShowOrigin });
					opt.Checked = Window.OriginPointVisible;
					opt.Click += (s,a) =>
					{
						Window.OriginPointVisible = !Window.OriginPointVisible;
						Refresh();
					};
				}
				{// Show coords
				}
				{// Axis Views
					var opt = view_menu.DropDownItems.Add2(new ToolStripComboBox("Views") { Name = CMenuItems.ViewMenu.Views, DropDownStyle = ComboBoxStyle.DropDownList });
					opt.Items.Add("Views");
					opt.Items.Add("Axis +X");
					opt.Items.Add("Axis -X");
					opt.Items.Add("Axis +Y");
					opt.Items.Add("Axis -Y");
					opt.Items.Add("Axis +Z");
					opt.Items.Add("Axis -Z");
					opt.Items.Add("Axis -X,-Y,-Z");
					opt.SelectedIndex = 0;
					opt.SelectedIndexChanged += (s, a) =>
					{
						var pos = Camera.FocusPoint;
						switch (opt.SelectedIndex)
						{
						case 1: Camera.ResetView( v4.XAxis); break;
						case 2: Camera.ResetView(-v4.XAxis); break;
						case 3: Camera.ResetView( v4.YAxis); break;
						case 4: Camera.ResetView(-v4.YAxis); break;
						case 5: Camera.ResetView( v4.ZAxis); break;
						case 6: Camera.ResetView(-v4.ZAxis); break;
						case 7: Camera.ResetView(-v4.XAxis-v4.YAxis-v4.ZAxis); break;
						}
						Camera.FocusPoint = pos;
						Refresh();
					};
				}
				{// Object Manager UI
					//var obj_mgr_ui = new ToolStripMenuItem("Object Manager");
					//view_menu.DropDownItems.Add(obj_mgr_ui);
					//obj_mgr_ui.Click += (s,a) => Window.ShowObjectManager(true);
				}
			}
			{// Navigation
				var rdr_menu = context_menu.Items.Add2(new ToolStripMenuItem("Navigation") { Name = CMenuItems.Navigation });
				{// Reset View
					var opt = rdr_menu.DropDownItems.Add2(new ToolStripMenuItem("Reset View") { Name = CMenuItems.NavMenu.ResetView });
					opt.Click += (s,a) =>
					{
						Camera.ResetView();
						Refresh();
					};
				}
				{// Align to
					var align_menu = rdr_menu.DropDownItems.Add2(new ToolStripMenuItem("Align") { Name = CMenuItems.NavMenu.Align });
					{
						var opt = align_menu.DropDownItems.Add2(new ToolStripComboBox("Aligns") { Name = CMenuItems.NavMenu.AlignMenu.Aligns, DropDownStyle = ComboBoxStyle.DropDownList });
						opt.Items.Add("None");
						opt.Items.Add("X");
						opt.Items.Add("Y");
						opt.Items.Add("Z");

						var axis = Camera.AlignAxis;
						if      (Math_.FEql(axis, v4.XAxis)) opt.SelectedIndex = 1;
						else if (Math_.FEql(axis, v4.YAxis)) opt.SelectedIndex = 2;
						else if (Math_.FEql(axis, v4.ZAxis)) opt.SelectedIndex = 3;
						else                                 opt.SelectedIndex = 0;
						opt.SelectedIndexChanged += (s,a) =>
						{
							switch (opt.SelectedIndex)
							{
							default: Camera.AlignAxis = v4.Zero;  break;
							case 1:  Camera.AlignAxis = v4.XAxis; break;
							case 2:  Camera.AlignAxis = v4.YAxis; break;
							case 3:  Camera.AlignAxis = v4.ZAxis; break;
							}
							Refresh();
						};
					}
				}
				{// Motion lock
				}
				{// Orbit
					//var orbit_menu = new ToolStripMenuItem("Orbit");
					//rdr_menu.DropDownItems.Add(orbit_menu);
					//orbit_menu.Click += delegate {};
				}
			}
			{// Tools
				var tools_menu = context_menu.Items.Add2(new ToolStripMenuItem("Tools") { Name = CMenuItems.Tools });
				{// Measure
					var opt = tools_menu.DropDownItems.Add2(new ToolStripMenuItem("Measure...") { Name = CMenuItems.ToolsMenu.Measure });
					opt.Click += (s, a) =>
					{
						ShowMeasurementUI = true;
					};
				}
				{// Angle
					var opt = tools_menu.DropDownItems.Add2(new ToolStripMenuItem("Angle...") { Name = CMenuItems.ToolsMenu.Angle });
					opt.Click += (s, a)=>
					{
						Window.ShowAngleTool = true;
					};
				}
			}
			{// Rendering
				var rdr_menu = context_menu.Items.Add2(new ToolStripMenuItem("Rendering") { Name = CMenuItems.Rendering });
				{// Solid/Wireframe/Solid+Wire
					var opt = rdr_menu.DropDownItems.Add2(new ToolStripComboBox { Name = CMenuItems.RenderingMenu.FillMode, DropDownStyle = ComboBoxStyle.DropDownList });
					opt.Items.AddRange(Enum<View3d.EFillMode>.Names.Cast<object>().ToArray());
					opt.SelectedIndex = (int)Window.FillMode;
					opt.SelectedIndexChanged += (s,a) =>
					{
						Window.FillMode = (View3d.EFillMode)opt.SelectedIndex;
						Refresh();
					};
				}
				{// Render2D
					var opt = rdr_menu.DropDownItems.Add2(new ToolStripMenuItem(Window.Camera.Orthographic ? "Perspective" : "Orthographic") { Name = CMenuItems.RenderingMenu.Orthographic });
					opt.Click += (s,a) =>
					{
						var _2d = Window.Camera.Orthographic;
						Window.Camera.Orthographic = !_2d;
						opt.Text = _2d ? "Perspective" : "Orthographic";
						Refresh();
					};
				}
				{// Lighting...
					var opt = rdr_menu.DropDownItems.Add2(new ToolStripMenuItem("Lighting...") { Name = CMenuItems.RenderingMenu.Lighting });
					opt.Click += (s,a) =>
					{
						Window.ShowLightingDlg();
					};
				}
				{// Background colour
					var bk_colour_menu = rdr_menu.DropDownItems.Add2(new ToolStripMenuItem("Background Colour") { Name = CMenuItems.RenderingMenu.Background });
					var opt = bk_colour_menu.DropDownItems.Add2(new ToolStripButton(" "));
					opt.AutoToolTip = false;
					opt.BackColor = Window.BackgroundColour;
					opt.Click += (s,a) =>
					{
						var cd = new ColourUI();
						if (cd.ShowDialog() == DialogResult.OK)
							opt.BackColor = cd.Colour;
					};
					opt.BackColorChanged += (s,a) =>
					{
						Window.BackgroundColour = opt.BackColor;
						Refresh();
					};
				}
			}

			// Allow users to add custom menu options to the context menu
			// Do this last so that users have the option of removing options they don't want displayed
			OnCustomiseContextMenu(new CustomContextMenuEventArgs(context_menu));

			context_menu.Show(MousePosition);
		}

		/// <summary>Allow Invalidate to be signed up to an event handler</summary>
		public void Invalidate(object sender, EventArgs args)
		{
			base.Invalidate();
		}

		/// <summary>On Resize</summary>
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			if (Window != null && !this.IsInDesignMode())
				Window.BackBufferSize = new Size(Width-2, Height-2);
		}

		/// <summary>Absorb PaintBackground events</summary>
		protected override void OnPaintBackground(PaintEventArgs e)
		{
			if (Window == null || this.IsInDesignMode())
				base.OnPaintBackground(e);
		}

		/// <summary>Paint the control</summary>
		protected override void OnPaint(PaintEventArgs e)
		{
			if (Window != null && !this.IsInDesignMode())
			{
				Render();
				Win32.ValidateRect(Handle, IntPtr.Zero);
			}
			base.OnPaint(e);
		}

		/// <summary>Update the size of the control whenever we're added to a new parent</summary>
		protected override void OnParentChanged(EventArgs e)
		{
			base.OnParentChanged(e);
			if (View3d == null) return;
			Window.BackBufferSize = new Size(Width-2, Height-2);
		}

		/// <summary>Raises the CustomiseContextMenu event</summary>
		protected virtual void OnCustomiseContextMenu(CustomContextMenuEventArgs e)
		{
			CustomiseContextMenu?.Invoke(this, e);
		}

		/// <summary>Raises the Error event</summary>
		protected virtual void OnReportError(ReportErrorEventArgs e)
		{
			ReportError?.Invoke(this, e);
		}
		private void HandleReportError(IntPtr ctx, string msg)
		{
			OnReportError(new ReportErrorEventArgs(msg));
		}

		/// <summary>Render and present the scene</summary>
		private void Render()
		{
			Window.RestoreRT();
			Window.Render();
			Window.Present();
		}

		#region Measurement UI

		/// <summary>Enable/Disable the measurement UI</summary>
		public bool ShowMeasurementUI
		{
			get { return m_measure_ui != null; }
			set
			{
				if (ShowMeasurementUI == value) return;
				if (m_measure_ui != null)
				{
					Util.Dispose(ref m_measure_ui);
				}
				m_measure_ui = value ? new MeasurementUI(this) : null;
				if (m_measure_ui != null)
				{
					m_measure_ui.Show(this);
				}
			}
		}
		private MeasurementUI m_measure_ui;

		/// <summary>Tool window wrapper of the measurement control</summary>
		private class MeasurementUI : ToolForm
		{
			public MeasurementUI(View3dControl owner)
				: base(owner, EPin.TopLeft, new Point(10, 10), new Size(264, 330), false)
			{
				FormBorderStyle = FormBorderStyle.SizableToolWindow;
				ShowInTaskbar = false;
				HideOnClose = false;
				Text = "Measure";

				Controls.Add2(new View3dMeasurementUI
				{
					Dock = DockStyle.Fill,
					CtxId = Guid.NewGuid(),
					Window = owner.Window,
				});

				FormClosed += (s, a) =>
				{
					if (a.CloseReason == CloseReason.UserClosing)
						owner.ShowMeasurementUI = false;
				};
			}
		}

		#endregion

		#region CMenuItems
		public static class CMenuItems
		{
			public const string View = "View";
			public static class ViewMenu
			{
				public const string ShowFocus = "ShowFocus";
				public const string ShowOrigin = "ShowOrigin";
				public const string Views = "Views";
			}
			public const string Navigation = "Navigation";
			public static class NavMenu
			{
				public const string ResetView = "ResetView";
				public const string Align = "Align";
				public static class AlignMenu
				{
					public const string Aligns = "Aligns";
				}
			}
			public const string Tools = "Tools";
			public static class ToolsMenu
			{
				public const string Measure = "Measure";
				public const string Angle = "Angle";
			}
			public const string Rendering = "Rendering";
			public static class RenderingMenu
			{
				public const string FillMode = "FillMode";
				public const string Orthographic = "Orthographic";
				public const string Lighting = "Lighting";
				public const string Background = "Background";
			}
		}
		#endregion

		#region EventArgs

		public class ReportErrorEventArgs : EventArgs
		{
			/// <summary>Error message</summary>
			public string Message { get; private set; }
			internal ReportErrorEventArgs(string msg) { Message = msg; }
		}

		public class CustomContextMenuEventArgs : EventArgs
		{
			/// <summary>The context menu to be customised</summary>
			public ContextMenuStrip Menu { get; private set; }
			internal CustomContextMenuEventArgs(ContextMenuStrip menu) { Menu = menu; }
		}

		#endregion

		#region Component Designer generated code

		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>Required method for Designer support - do not modify the contents of this method with the code editor.</summary>
		private void InitializeComponent()
		{
			this.SuspendLayout();
			// 
			// View3dControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Name = "View3dControl";
			this.ResumeLayout(false);
		}

		#endregion
	}
}
