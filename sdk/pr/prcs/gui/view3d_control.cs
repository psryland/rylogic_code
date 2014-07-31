using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.gfx;
using pr.maths;
using pr.util;

namespace pr.gui
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
		public View3dControl() :this(false, null) {}
		public View3dControl(bool gdi_compat, View3d.ReportErrorCB error_cb, View3d.LogOutputCB log_cb = null)
		{
			if (this.IsInDesignMode()) return;

			// A reference is held within View3d to the callbacks, so callers don't need to hold one
			m_impl_view3d = new View3d(error_cb, log_cb);
			m_impl_wnd = new View3d.Window(View3d, Handle, gdi_compat, Render);

			InitializeComponent();

			ClickTimeMS = 180;
			MouseNavigation = true;
			DefaultKeyboardShortcuts = true;
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref m_impl_wnd);
			Util.Dispose(ref m_impl_view3d);
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>The underlying interop wrapper</summary>
		public View3d View3d { get { return m_impl_view3d; } }
		private View3d m_impl_view3d;

		/// <summary>The binding to this control</summary>
		public View3d.Window Window { get { return m_impl_wnd; } }
		private View3d.Window m_impl_wnd;

		/// <summary>The main camera</summary>
		public View3d.CameraControls Camera { get { return Window.Camera; } }

		/// <summary>Cause a redraw to happen the near future. This method can be called multiple times</summary>
		public void SignalRefresh()
		{
			Window.SignalRefresh();
		}

		/// <summary>Assign a handler to 'OnError' to hide the default message box</summary>
		public event View3d.ReportErrorCB OnError
		{
			add    { View3d.OnError += value; }
			remove { View3d.OnError -= value; }
		}

		/// <summary>Assign a handler to 'OnLog' to receive log output</summary>
		public event View3d.LogOutputCB OnLog
		{
			add    { View3d.OnLog += value; }
			remove { View3d.OnLog -= value; }
		}

		/// <summary>Event notifying whenever rendering settings have changed</summary>
		public event View3d.SettingsChangedCB OnSettingsChanged
		{
			add    { Window.OnSettingsChanged += value; }
			remove { Window.OnSettingsChanged -= value; }
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
				KeyDown -= Window.TranslateKey;
				if (value)
					KeyDown += Window.TranslateKey;
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
			MouseMove -= OnMouseMove;
			MouseMove += OnMouseMove;
			Capture = true;
			Camera.MouseNavigate(e.Location, e.Button, true);
			m_mouse_down_at = Environment.TickCount;
		}
		public void OnMouseUp(object sender, MouseEventArgs e)
		{
			if (Window == null) return;
			Cursor = Cursors.Default;
			MouseMove -= OnMouseMove;
			Capture = false;
			Camera.MouseNavigate(e.Location, 0, true);

			// Short clicks bring up the context menu
			if (e.Button == MouseButtons.Right && Environment.TickCount - m_mouse_down_at < ClickTimeMS)
				ShowContextMenu();
		}
		public void OnMouseMove(object sender, MouseEventArgs e)
		{
			if (Window == null) return;
			Camera.MouseNavigate(e.Location, e.Button, false);
			SignalRefresh();
		}
		public void OnMouseWheel(object sender, MouseEventArgs e)
		{
			if (Window == null) return;
			Camera.Navigate(0f, 0f, e.Delta / 120f);
			SignalRefresh();
		}
		public void OnMouseDblClick(object sender, MouseEventArgs e)
		{
			if (Window == null) return;
			if (Bit.AllSet((int)e.Button, (int)MouseButtons.Middle) ||
				Bit.AllSet((int)e.Button, (int)(MouseButtons.Left|MouseButtons.Right)))
				Camera.ResetZoom();
			SignalRefresh();
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
					show_origin_menu.Checked = Window.OriginVisible;
					show_origin_menu.Click += (s,a) => { Window.OriginVisible = !Window.OriginVisible; Refresh(); };
				}
				{// Show coords
				}
				{// Axis Views
					var view_options = new ToolStripComboBox("Views");
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
					var obj_mgr_ui = new ToolStripMenuItem("Object Manager");
					view_menu.DropDownItems.Add(obj_mgr_ui);
					obj_mgr_ui.Click += (s,a) => Window.ShowObjectManager(true);
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
						var align_options = new ToolStripComboBox("Aligns");
						align_menu.DropDownItems.Add(align_options);
						align_options.Items.Add("None");
						align_options.Items.Add("X");
						align_options.Items.Add("Y");
						align_options.Items.Add("Z");

						var axis = Camera.AlignAxis;
						if      (v4.FEql3(axis, v4.XAxis)) align_options.SelectedIndex = 1;
						else if (v4.FEql3(axis, v4.YAxis)) align_options.SelectedIndex = 2;
						else if (v4.FEql3(axis, v4.ZAxis)) align_options.SelectedIndex = 3;
						else                               align_options.SelectedIndex = 0;
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
					var option = new ToolStripComboBox();
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
			OnCustomiseContextMenu(new CustomContextMenuEventArgs(context_menu));

			context_menu.Show(MousePosition);
		}

		/// <summary>Event called just before displaying the context menu to allow users to add custom options to the menu</summary>
		public event EventHandler<CustomContextMenuEventArgs> CustomiseContextMenu;
		public class CustomContextMenuEventArgs :EventArgs
		{
			/// <summary>The context menu to be customised</summary>
			public ContextMenuStrip Menu { get; private set; }

			internal CustomContextMenuEventArgs(ContextMenuStrip menu)
			{
				Menu = menu;
			}
		}

		/// <summary>On Resize</summary>
		protected override void OnResize(EventArgs e)
		{
			if (this.IsInDesignMode()) { base.OnResize(e); return; }

			base.OnResize(e);
			if (Window != null)
				Window.RenderTargetSize = new Size(Width-2, Height-2);
		}

		/// <summary>Absorb PaintBackground events</summary>
		protected override void OnPaintBackground(PaintEventArgs e)
		{
			if (this.IsInDesignMode()) { base.OnPaintBackground(e); return; }

			if (Window == null)
				base.OnPaintBackground(e);
		}

		/// <summary>Paint the control</summary>
		protected override void OnPaint(PaintEventArgs e)
		{
			if (this.IsInDesignMode()) { base.OnPaint(e); return; }

			if (Window == null)
				base.OnPaint(e);
			else
				Render();
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
			CustomiseContextMenu.Raise(this, e);
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
