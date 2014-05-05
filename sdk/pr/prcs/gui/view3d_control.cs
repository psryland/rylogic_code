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

using HWND     = System.IntPtr;
using HDrawset = System.IntPtr;
using HObject  = System.IntPtr;
using HTexture = System.IntPtr;

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
		private IContainer components;                            // Required designer variable.
		private int m_mouse_down_at;

		public View3dControl() :this(null) {}

		/// <summary>Provides an error callback. A reference is held within View3D, so callers don't need to hold one</summary>
		public View3dControl(View3d.ReportErrorCB error_cb, View3d.LogOutputCB log_cb = null)
		{
			if (this.IsInDesignMode()) return;
			View3d = new View3d(Handle, error_cb, log_cb);
			Camera = new View3d.CameraControls(View3d.Drawset);

			InitializeComponent();

			ClickTimeMS = 180;
			MouseNavigation = true;
			DefaultKeyboardShortcuts = true;

			// Update the size of the control whenever we're added to a new parent
			ParentChanged += (s,a) => View3d.RenderTargetSize = new Size(Width-2, Height-2);
		}

		/// <summary>Clean up any resources being used.</summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing && View3d != null)
			{
				View3d.Dispose();
			}
			if (disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>The underlying interop wrapper</summary>
		public View3d View3d { get; private set; }

		/// <summary>The main camera</summary>
		public View3d.CameraControls Camera { get; private set; }

		/// <summary>Cause a redraw to happen the near future. This method can be called multiple times</summary>
		public void SignalRefresh()
		{
			View3d.SignalRefresh();
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
			add    { View3d.OnSettingsChanged += value; }
			remove { View3d.OnSettingsChanged -= value; }
		}

		/// <summary>Get/Set the currently active drawset</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public View3d.DrawsetInterface Drawset
		{
			get { return View3d.Drawset; }
			set
			{
				MouseNavigation = false;

				View3d.Drawset = value;
				if (value == null) return;

				MouseNavigation = true;
			}
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
				Drawset.BackgroundColour = value;
			}
		}

		/// <summary>Enable/Disable default keyboard shortcuts</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool DefaultKeyboardShortcuts
		{
			set
			{
				KeyDown -= View3d.TranslateKey;
				if (value) KeyDown += View3d.TranslateKey;
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
			Cursor = Cursors.SizeAll;
			MouseMove -= OnMouseMove;
			MouseMove += OnMouseMove;
			Capture = true;
			Camera.MouseNavigate(e.Location, e.Button, true);
			m_mouse_down_at = Environment.TickCount;
		}
		public void OnMouseUp(object sender, MouseEventArgs e)
		{
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
			if (Drawset == null) return;
			Camera.MouseNavigate(e.Location, e.Button, false);
			Drawset.Render();
		}
		public void OnMouseWheel(object sender, MouseEventArgs e)
		{
			if (Drawset == null) return;
			Camera.Navigate(0f, 0f, e.Delta / 120f);
			Drawset.Render();
		}
		public void OnMouseDblClick(object sender, MouseEventArgs e)
		{
			if (Drawset == null) return;
			if (Bit.AllSet((int)e.Button, (int)MouseButtons.Middle) ||
				Bit.AllSet((int)e.Button, (int)(MouseButtons.Left|MouseButtons.Right)))
				Camera.ResetZoom();
			Drawset.Render();
		}

		///// <summary>Create a drawset. Sets the current drawset to the one created</summary>
		//public DrawsetInterface DrawsetCreate()
		//{
		//	return new DrawsetInterface(this);
		//}

		///// <summary>Show/Hide the object manager UI</summary>
		//public void ShowObjectManager(bool show)
		//{
		//	View3D_ShowObjectManager(show);
		//}

		///// <summary>Create multiple objects from a source file and associate them with 'context_id'</summary>
		//public void ObjectsCreateFromFile(string ldr_filepath, int context_id, bool async)
		//{
		//	View3D_ObjectsCreateFromFile(ldr_filepath, context_id, async);
		//}
		//public void ObjectsCreateFromFile(string ldr_filepath, bool async)
		//{
		//	ObjectsCreateFromFile(ldr_filepath, DefaultContextId, async);
		//}

		///// <summary>Delete all objects matching 'context_id'</summary>
		//public void ObjectsDelete(int context_id)
		//{
		//	View3D_ObjectsDeleteById(context_id);
		//}

		///// <summary>Show a window containing and example ldr script file</summary>
		//public void ShowExampleScript()
		//{
		//	View3D_ShowDemoScript();
		//}

		///// <summary>Example for creating objects</summary>
		//public void CreateDemoScene()
		//{
		//	if (Drawset == null) return;
		//	View3D_CreateDemoScene(Drawset.Handle);

		//	//{// Create an object using ldr script
		//	//    HObject obj = ObjectCreate("*Box ldr_box FFFF00FF {1 2 3}");
		//	//    DrawsetAddObject(obj);
		//	//}

		//	//{// Create a box model, an instance for it, and add it to the drawset
		//	//    HModel model = CreateModelBox(new v4(0.3f, 0.2f, 0.4f, 0f), m4x4.Identity, 0xFFFF0000);
		//	//    HInstance instance = CreateInstance(model, m4x4.Identity);
		//	//    AddInstance(drawset, instance);
		//	//}

		//	//{// Create a mesh
		//	//    // Mesh data
		//	//    Vertex[] vert = new Vertex[]
		//	//    {
		//	//        new Vertex(new v4( 0f, 0f, 0f, 1f), v4.ZAxis, 0xFFFF0000, v2.Zero),
		//	//        new Vertex(new v4( 0f, 1f, 0f, 1f), v4.ZAxis, 0xFF00FF00, v2.Zero),
		//	//        new Vertex(new v4( 1f, 0f, 0f, 1f), v4.ZAxis, 0xFF0000FF, v2.Zero),
		//	//    };
		//	//    ushort[] face = new ushort[]
		//	//    {
		//	//        0, 1, 2
		//	//    };

		//	//    HModel model = CreateModel(vert.Length, face.Length, vert, face, EPrimType.D3DPT_TRIANGLELIST);
		//	//    HInstance instance = CreateInstance(model, m4x4.Identity);
		//	//    AddInstance(drawset, instance);
		//	//}
		//}

		/// <summary>Show the view3D context menu</summary>
		public void ShowContextMenu()
		{
			if (Drawset == null) return;
			var context_menu = new ContextMenuStrip();
			context_menu.Closed += (s,a) => Refresh();

			{// View
				var view_menu = new ToolStripMenuItem("View");
				context_menu.Items.Add(view_menu);
				{// Show focus
					var show_focus_menu = new ToolStripMenuItem("Show Focus");
					view_menu.DropDownItems.Add(show_focus_menu);
					show_focus_menu.Checked = Drawset.FocusPointVisible;
					show_focus_menu.Click += (s,a) => { Drawset.FocusPointVisible = !Drawset.FocusPointVisible; Refresh(); };
				}
				{// Show Origin
					var show_origin_menu = new ToolStripMenuItem("Show Origin");
					view_menu.DropDownItems.Add(show_origin_menu);
					show_origin_menu.Checked = Drawset.OriginVisible;
					show_origin_menu.Click += (s,a) => { Drawset.OriginVisible = !Drawset.OriginVisible; Refresh(); };
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
						var pos = Drawset.Camera.FocusPoint;
						switch (view_options.SelectedIndex)
						{
						case 1: Drawset.Camera.ResetView( v4.XAxis); break;
						case 2: Drawset.Camera.ResetView(-v4.XAxis); break;
						case 3: Drawset.Camera.ResetView( v4.YAxis); break;
						case 4: Drawset.Camera.ResetView(-v4.YAxis); break;
						case 5: Drawset.Camera.ResetView( v4.ZAxis); break;
						case 6: Drawset.Camera.ResetView(-v4.ZAxis); break;
						case 7: Drawset.Camera.ResetView(-v4.XAxis-v4.YAxis-v4.ZAxis); break;
						}
						Drawset.Camera.FocusPoint = pos;
						Refresh();
					};
				}
				{// Object Manager UI
					var obj_mgr_ui = new ToolStripMenuItem("Object Manager");
					view_menu.DropDownItems.Add(obj_mgr_ui);
					obj_mgr_ui.Click += (s,a) => View3d.ShowObjectManager(true);
				}
			}
			{// Navigation
				var rdr_menu = new ToolStripMenuItem("Navigation");
				context_menu.Items.Add(rdr_menu);
				{// Reset View
					var reset_menu = new ToolStripMenuItem("Reset View");
					rdr_menu.DropDownItems.Add(reset_menu);
					reset_menu.Click += delegate { Drawset.Camera.ResetView(); Refresh(); };
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

						var axis = Drawset.Camera.AlignAxis;
						if      (v4.FEql3(axis, v4.XAxis)) align_options.SelectedIndex = 1;
						else if (v4.FEql3(axis, v4.YAxis)) align_options.SelectedIndex = 2;
						else if (v4.FEql3(axis, v4.ZAxis)) align_options.SelectedIndex = 3;
						else                               align_options.SelectedIndex = 0;
						align_options.SelectedIndexChanged += delegate
						{
							switch (align_options.SelectedIndex)
							{
							default: Drawset.Camera.AlignAxis = v4.Zero;  break;
							case 1:  Drawset.Camera.AlignAxis = v4.XAxis; break;
							case 2:  Drawset.Camera.AlignAxis = v4.YAxis; break;
							case 3:  Drawset.Camera.AlignAxis = v4.ZAxis; break;
							}
							Refresh();
						};
					}
				}
				{// Motion lock
				}
				{// Orbit
					var orbit_menu = new ToolStripMenuItem("Orbit");
					rdr_menu.DropDownItems.Add(orbit_menu);
					orbit_menu.Click += delegate {};
				}
			}
			{// Tools
				var tools_menu = new ToolStripMenuItem("Tools");
				context_menu.Items.Add(tools_menu);
				{// Measure
					var option = new ToolStripMenuItem{Text = "Measure..."};
					tools_menu.DropDownItems.Add(option);
					option.Click += delegate { Drawset.ShowMeasureTool = true; };
				}
				{// Angle
					var option = new ToolStripMenuItem{Text = "Angle..."};
					tools_menu.DropDownItems.Add(option);
					option.Click += delegate { Drawset.ShowAngleTool = true; };
				}
			}
			{// Rendering
				var rdr_menu = new ToolStripMenuItem("Rendering");
				context_menu.Items.Add(rdr_menu);
				{// Solid/Wireframe/Solid+Wire
					var option = new ToolStripComboBox();
					rdr_menu.DropDownItems.Add(option);
					option.Items.AddRange(Enum<View3d.EFillMode>.Names.Cast<object>().ToArray());
					option.SelectedIndex = (int)Drawset.FillMode;
					option.SelectedIndexChanged += delegate { Drawset.FillMode = (View3d.EFillMode)option.SelectedIndex; Refresh(); };
				}
				{// Render2D
					var option = new ToolStripMenuItem{Text = Drawset.Orthographic ? "Perspective" : "Orthographic"};
					rdr_menu.DropDownItems.Add(option);
					option.Click += delegate { var _2d = Drawset.Orthographic; Drawset.Orthographic = !_2d; option.Text = _2d ? "Perspective" : "Orthographic"; Refresh(); };
				}
				{// Lighting...
					var lighting_menu = new ToolStripMenuItem("Lighting...");
					rdr_menu.DropDownItems.Add(lighting_menu);
					lighting_menu.Click += delegate { Drawset.ShowLightingDlg(ParentForm); };
				}
				{// Background colour
					var bk_colour_menu = new ToolStripMenuItem("Background Colour");
					rdr_menu.DropDownItems.Add(bk_colour_menu);
					{
						var option = new ToolStripButton(" ");
						bk_colour_menu.DropDownItems.Add(option);
						option.AutoToolTip = false;
						option.BackColor = Drawset.BackgroundColour;
						option.Click += delegate { var cd = new ColourUI(); if (cd.ShowDialog() == DialogResult.OK) option.BackColor = cd.Colour; };
						option.BackColorChanged += delegate { Drawset.BackgroundColour = option.BackColor; Refresh(); };
					}
				}
			}

			// Allow users to add custom menu options to the context menu
			if (CustomiseContextMenu != null)
				CustomiseContextMenu(context_menu);

			context_menu.Show(MousePosition);
		}

		/// <summary>Event called just before displaying the context menu to allow users to add custom options to the menu</summary>
		public event Action<ContextMenuStrip> CustomiseContextMenu;

		/// <summary>On Resize</summary>
		protected override void OnResize(EventArgs e)
		{
			if (this.IsInDesignMode()) { base.OnResize(e); return; }

			base.OnResize(e);
			View3d.RenderTargetSize = new Size(Width-2, Height-2);
			//Refresh();
		}

		/// <summary>Absorb PaintBackground events</summary>
		protected override void OnPaintBackground(PaintEventArgs e)
		{
			if (this.IsInDesignMode()) { base.OnPaintBackground(e); return; }

			if (Drawset == null)
				base.OnPaintBackground(e);
		}

		/// <summary>Paint the control</summary>
		protected override void OnPaint(PaintEventArgs e)
		{
			if (this.IsInDesignMode()) { base.OnPaint(e); return; }

			if (Drawset == null) base.OnPaint(e);
			else Drawset.Render();
		}

		/// <summary>Required method for Designer support - do not modify the contents of this method with the code editor.</summary>
		private void InitializeComponent()
		{
			components = new Container();
		}
	}
}
