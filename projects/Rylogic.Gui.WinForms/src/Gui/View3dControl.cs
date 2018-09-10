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
			:this(name, bgra_compatibility:true, gdi_compatible_backbuffer:false)
		{}
		public View3dControl(string name, bool bgra_compatibility, bool gdi_compatible_backbuffer)
		{
			try
			{
				BackColor = Color.Gray;
				if (this.IsInDesignMode()) return;
				SetStyle(ControlStyles.Selectable, false);

				m_view3d = View3d.Create(bgra_compatibility);
				var opts = new View3d.WindowOptions(HandleReportError, IntPtr.Zero, gdi_compatible_backbuffer) { DbgName = name ?? string.Empty };
				m_impl_wnd = new View3d.Window(View3d, Handle, opts);

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
			Utility.Util.BreakIf(m_impl_wnd != null && Utility.Util.IsGCFinalizerThread);
			Utility.Util.BreakIf(m_view3d != null && Utility.Util.IsGCFinalizerThread);
			Util.Dispose(ref m_impl_wnd);
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
			[DebuggerStepThrough] get { return m_impl_wnd; }
		}
		private View3d.Window m_impl_wnd;

		/// <summary>The main camera</summary>
		public View3d.Camera Camera
		{
			[DebuggerStepThrough] get { return Window.Camera; }
		}

		/// <summary>Called whenever an error is generated in view3d</summary>
		public event EventHandler<ReportErrorEventArgs> ReportError;
		public class ReportErrorEventArgs :EventArgs
		{
			/// <summary>Error message</summary>
			public string Message { get; private set; }
			internal ReportErrorEventArgs(string msg) { Message = msg; }
		}

		/// <summary>Event notifying whenever rendering settings have changed</summary>
		public event EventHandler OnSettingsChanged
		{
			add    { Window.OnSettingsChanged += value; }
			remove { Window.OnSettingsChanged -= value; }
		}

		/// <summary>Event called just before displaying the context menu to allow users to add custom options to the menu</summary>
		public event EventHandler<CustomContextMenuEventArgs> CustomiseContextMenu;
		public class CustomContextMenuEventArgs :EventArgs
		{
			/// <summary>The context menu to be customised</summary>
			public ContextMenuStrip Menu { get; private set; }
			internal CustomContextMenuEventArgs(ContextMenuStrip menu) { Menu = menu; }
		}

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
					args.Handled = Window.TranslateKey((int)args.KeyCode);
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
			if (Window.MouseNavigate(e.Location, (EMouseBtns)e.Button, true))
				Invalidate();
		}
		public void OnMouseUp(object sender, MouseEventArgs e)
		{
			if (Window == null) return;
			Cursor = Cursors.Default;
			Capture = false;
			if (Window.MouseNavigate(e.Location, (EMouseBtns)e.Button, View3d.ENavOp.None, true))
				Invalidate();

			// Short clicks bring up the context menu
			if (e.Button == MouseButtons.Right && Environment.TickCount - m_mouse_down_at < ClickTimeMS)
				ShowContextMenu();
		}
		public void OnMouseMove(object sender, MouseEventArgs e)
		{
			if (Window == null) return;
			if (Window.MouseNavigate(e.Location, (EMouseBtns)e.Button, false))
				Invalidate();
		}
		public void OnMouseWheel(object sender, MouseEventArgs e)
		{
			if (Window == null) return;
			if (Window.MouseNavigateZ(e.Location, (EMouseBtns)e.Button, e.Delta, true))
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
				var view_menu = new ToolStripMenuItem("View");
				context_menu.Items.Add(view_menu);
				{// Show focus
					var show_focus_menu = new ToolStripMenuItem("Show Focus");
					view_menu.DropDownItems.Add(show_focus_menu);
					show_focus_menu.Checked = Window.FocusPointVisible;
					show_focus_menu.Click += (s,a) => { Window.FocusPointVisible = !Window.FocusPointVisible; Refresh(); };
				}
				{// Show Origin
					var show_origin_menu = new ToolStripMenuItem("Show Origin");
					view_menu.DropDownItems.Add(show_origin_menu);
					show_origin_menu.Checked = Window.OriginPointVisible;
					show_origin_menu.Click += (s,a) => { Window.OriginPointVisible = !Window.OriginPointVisible; Refresh(); };
				}
				{// Show coords
				}
				{// Axis Views
					var view_options = new ToolStripComboBox("Views") { DropDownStyle = ComboBoxStyle.DropDownList };
					view_menu.DropDownItems.Add(view_options);
					view_options.Items.Add("Views");
					view_options.Items.Add("Axis +X");
					view_options.Items.Add("Axis -X");
					view_options.Items.Add("Axis +Y");
					view_options.Items.Add("Axis -Y");
					view_options.Items.Add("Axis +Z");
					view_options.Items.Add("Axis -Z");
					view_options.Items.Add("Axis -X,-Y,-Z");
					view_options.SelectedIndex = 0;
					view_options.SelectedIndexChanged += delegate
					{
						var pos = Camera.FocusPoint;
						switch (view_options.SelectedIndex)
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
				var rdr_menu = new ToolStripMenuItem("Navigation");
				context_menu.Items.Add(rdr_menu);
				{// Reset View
					var reset_menu = new ToolStripMenuItem("Reset View");
					rdr_menu.DropDownItems.Add(reset_menu);
					reset_menu.Click += delegate { Camera.ResetView(); Refresh(); };
				}
				{// Align to
					var align_menu = new ToolStripMenuItem("Align");
					rdr_menu.DropDownItems.Add(align_menu);
					{
						var align_options = new ToolStripComboBox("Aligns") { DropDownStyle = ComboBoxStyle.DropDownList };
						align_menu.DropDownItems.Add(align_options);
						align_options.Items.Add("None");
						align_options.Items.Add("X");
						align_options.Items.Add("Y");
						align_options.Items.Add("Z");

						var axis = Camera.AlignAxis;
						if      (Math_.FEql(axis, v4.XAxis)) align_options.SelectedIndex = 1;
						else if (Math_.FEql(axis, v4.YAxis)) align_options.SelectedIndex = 2;
						else if (Math_.FEql(axis, v4.ZAxis)) align_options.SelectedIndex = 3;
						else                                 align_options.SelectedIndex = 0;
						align_options.SelectedIndexChanged += delegate
						{
							switch (align_options.SelectedIndex)
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
				//	var orbit_menu = new ToolStripMenuItem("Orbit");
				//	rdr_menu.DropDownItems.Add(orbit_menu);
				//	orbit_menu.Click += delegate {};
				}
			}
			{// Tools
				var tools_menu = new ToolStripMenuItem("Tools");
				context_menu.Items.Add(tools_menu);
				{// Measure
					var option = new ToolStripMenuItem{Text = "Measure..."};
					tools_menu.DropDownItems.Add(option);
					option.Click += delegate { Window.ShowMeasureTool = true; };
				}
				{// Angle
					var option = new ToolStripMenuItem{Text = "Angle..."};
					tools_menu.DropDownItems.Add(option);
					option.Click += delegate { Window.ShowAngleTool = true; };
				}
			}
			{// Rendering
				var rdr_menu = new ToolStripMenuItem("Rendering");
				context_menu.Items.Add(rdr_menu);
				{// Solid/Wireframe/Solid+Wire
					var option = new ToolStripComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
					rdr_menu.DropDownItems.Add(option);
					option.Items.AddRange(Enum<View3d.EFillMode>.Names.Cast<object>().ToArray());
					option.SelectedIndex = (int)Window.FillMode;
					option.SelectedIndexChanged += delegate { Window.FillMode = (View3d.EFillMode)option.SelectedIndex; Refresh(); };
				}
				{// Render2D
					var option = new ToolStripMenuItem{Text = Window.Orthographic ? "Perspective" : "Orthographic"};
					rdr_menu.DropDownItems.Add(option);
					option.Click += delegate { var _2d = Window.Orthographic; Window.Orthographic = !_2d; option.Text = _2d ? "Perspective" : "Orthographic"; Refresh(); };
				}
				{// Lighting...
					var lighting_menu = new ToolStripMenuItem("Lighting...");
					rdr_menu.DropDownItems.Add(lighting_menu);
					lighting_menu.Click += delegate { Window.ShowLightingDlg(); };
				}
				{// Background colour
					var bk_colour_menu = new ToolStripMenuItem("Background Colour");
					rdr_menu.DropDownItems.Add(bk_colour_menu);
					{
						var option = new ToolStripButton(" ");
						bk_colour_menu.DropDownItems.Add(option);
						option.AutoToolTip = false;
						option.BackColor = Window.BackgroundColour;
						option.Click += delegate { var cd = new ColourUI(); if (cd.ShowDialog() == DialogResult.OK) option.BackColor = cd.Colour; };
						option.BackColorChanged += delegate { Window.BackgroundColour = option.BackColor; Refresh(); };
					}
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
				Window.RenderTargetSize = new Size(Width-2, Height-2);
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
			Window.RenderTargetSize = new Size(Width-2, Height-2);
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
			Window.Render();
			Window.Present();
		}

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
