using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using pr.extn;
using pr.gfx;
using pr.maths;
using pr.util;
using HWND = System.IntPtr;

using HDrawset = System.IntPtr;
using HObject = System.IntPtr;
using HTexture = System.IntPtr;

namespace pr.gui
{
	public class View3D :UserControl
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

		public const int DefaultContextId = 0;

		#region Enumerations
		public enum EResult
		{
			Success,
			Failed,
			InvalidValue,
		}
		[Flags] public enum EGeom
		{
			Unknown  = 0,
			Vert     = 1 << 0,
			Colr     = 1 << 1,
			Norm     = 1 << 2,
			Tex0     = 1 << 3,
		}
		public enum EPrim :uint
		{
			D3D_PRIMITIVE_TOPOLOGY_UNDEFINED     = 0,
			D3D_PRIMITIVE_TOPOLOGY_POINTLIST     = 1,
			D3D_PRIMITIVE_TOPOLOGY_LINELIST      = 2,
			D3D_PRIMITIVE_TOPOLOGY_LINESTRIP     = 3,
			D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST  = 4,
			D3D_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP = 5,
		}
		public enum EFormat :uint
		{
			DXGI_FORMAT_UNKNOWN	                    = 0,
			DXGI_FORMAT_R32G32B32A32_TYPELESS       = 1,
			DXGI_FORMAT_R32G32B32A32_FLOAT          = 2,
			DXGI_FORMAT_R32G32B32A32_UINT           = 3,
			DXGI_FORMAT_R32G32B32A32_SINT           = 4,
			DXGI_FORMAT_R32G32B32_TYPELESS          = 5,
			DXGI_FORMAT_R32G32B32_FLOAT             = 6,
			DXGI_FORMAT_R32G32B32_UINT              = 7,
			DXGI_FORMAT_R32G32B32_SINT              = 8,
			DXGI_FORMAT_R16G16B16A16_TYPELESS       = 9,
			DXGI_FORMAT_R16G16B16A16_FLOAT          = 10,
			DXGI_FORMAT_R16G16B16A16_UNORM          = 11,
			DXGI_FORMAT_R16G16B16A16_UINT           = 12,
			DXGI_FORMAT_R16G16B16A16_SNORM          = 13,
			DXGI_FORMAT_R16G16B16A16_SINT           = 14,
			DXGI_FORMAT_R32G32_TYPELESS             = 15,
			DXGI_FORMAT_R32G32_FLOAT                = 16,
			DXGI_FORMAT_R32G32_UINT                 = 17,
			DXGI_FORMAT_R32G32_SINT                 = 18,
			DXGI_FORMAT_R32G8X24_TYPELESS           = 19,
			DXGI_FORMAT_D32_FLOAT_S8X24_UINT        = 20,
			DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS    = 21,
			DXGI_FORMAT_X32_TYPELESS_G8X24_UINT     = 22,
			DXGI_FORMAT_R10G10B10A2_TYPELESS        = 23,
			DXGI_FORMAT_R10G10B10A2_UNORM           = 24,
			DXGI_FORMAT_R10G10B10A2_UINT            = 25,
			DXGI_FORMAT_R11G11B10_FLOAT             = 26,
			DXGI_FORMAT_R8G8B8A8_TYPELESS           = 27,
			DXGI_FORMAT_R8G8B8A8_UNORM              = 28,
			DXGI_FORMAT_R8G8B8A8_UNORM_SRGB         = 29,
			DXGI_FORMAT_R8G8B8A8_UINT               = 30,
			DXGI_FORMAT_R8G8B8A8_SNORM              = 31,
			DXGI_FORMAT_R8G8B8A8_SINT               = 32,
			DXGI_FORMAT_R16G16_TYPELESS             = 33,
			DXGI_FORMAT_R16G16_FLOAT                = 34,
			DXGI_FORMAT_R16G16_UNORM                = 35,
			DXGI_FORMAT_R16G16_UINT                 = 36,
			DXGI_FORMAT_R16G16_SNORM                = 37,
			DXGI_FORMAT_R16G16_SINT                 = 38,
			DXGI_FORMAT_R32_TYPELESS                = 39,
			DXGI_FORMAT_D32_FLOAT                   = 40,
			DXGI_FORMAT_R32_FLOAT                   = 41,
			DXGI_FORMAT_R32_UINT                    = 42,
			DXGI_FORMAT_R32_SINT                    = 43,
			DXGI_FORMAT_R24G8_TYPELESS              = 44,
			DXGI_FORMAT_D24_UNORM_S8_UINT           = 45,
			DXGI_FORMAT_R24_UNORM_X8_TYPELESS       = 46,
			DXGI_FORMAT_X24_TYPELESS_G8_UINT        = 47,
			DXGI_FORMAT_R8G8_TYPELESS               = 48,
			DXGI_FORMAT_R8G8_UNORM                  = 49,
			DXGI_FORMAT_R8G8_UINT                   = 50,
			DXGI_FORMAT_R8G8_SNORM                  = 51,
			DXGI_FORMAT_R8G8_SINT                   = 52,
			DXGI_FORMAT_R16_TYPELESS                = 53,
			DXGI_FORMAT_R16_FLOAT                   = 54,
			DXGI_FORMAT_D16_UNORM                   = 55,
			DXGI_FORMAT_R16_UNORM                   = 56,
			DXGI_FORMAT_R16_UINT                    = 57,
			DXGI_FORMAT_R16_SNORM                   = 58,
			DXGI_FORMAT_R16_SINT                    = 59,
			DXGI_FORMAT_R8_TYPELESS                 = 60,
			DXGI_FORMAT_R8_UNORM                    = 61,
			DXGI_FORMAT_R8_UINT                     = 62,
			DXGI_FORMAT_R8_SNORM                    = 63,
			DXGI_FORMAT_R8_SINT                     = 64,
			DXGI_FORMAT_A8_UNORM                    = 65,
			DXGI_FORMAT_R1_UNORM                    = 66,
			DXGI_FORMAT_R9G9B9E5_SHAREDEXP          = 67,
			DXGI_FORMAT_R8G8_B8G8_UNORM             = 68,
			DXGI_FORMAT_G8R8_G8B8_UNORM             = 69,
			DXGI_FORMAT_BC1_TYPELESS                = 70,
			DXGI_FORMAT_BC1_UNORM                   = 71,
			DXGI_FORMAT_BC1_UNORM_SRGB              = 72,
			DXGI_FORMAT_BC2_TYPELESS                = 73,
			DXGI_FORMAT_BC2_UNORM                   = 74,
			DXGI_FORMAT_BC2_UNORM_SRGB              = 75,
			DXGI_FORMAT_BC3_TYPELESS                = 76,
			DXGI_FORMAT_BC3_UNORM                   = 77,
			DXGI_FORMAT_BC3_UNORM_SRGB              = 78,
			DXGI_FORMAT_BC4_TYPELESS                = 79,
			DXGI_FORMAT_BC4_UNORM                   = 80,
			DXGI_FORMAT_BC4_SNORM                   = 81,
			DXGI_FORMAT_BC5_TYPELESS                = 82,
			DXGI_FORMAT_BC5_UNORM                   = 83,
			DXGI_FORMAT_BC5_SNORM                   = 84,
			DXGI_FORMAT_B5G6R5_UNORM                = 85,
			DXGI_FORMAT_B5G5R5A1_UNORM              = 86,
			DXGI_FORMAT_B8G8R8A8_UNORM              = 87,
			DXGI_FORMAT_B8G8R8X8_UNORM              = 88,
			DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM  = 89,
			DXGI_FORMAT_B8G8R8A8_TYPELESS           = 90,
			DXGI_FORMAT_B8G8R8A8_UNORM_SRGB         = 91,
			DXGI_FORMAT_B8G8R8X8_TYPELESS           = 92,
			DXGI_FORMAT_B8G8R8X8_UNORM_SRGB         = 93,
			DXGI_FORMAT_BC6H_TYPELESS               = 94,
			DXGI_FORMAT_BC6H_UF16                   = 95,
			DXGI_FORMAT_BC6H_SF16                   = 96,
			DXGI_FORMAT_BC7_TYPELESS                = 97,
			DXGI_FORMAT_BC7_UNORM                   = 98,
			DXGI_FORMAT_BC7_UNORM_SRGB              = 99,
			DXGI_FORMAT_AYUV                        = 100,
			DXGI_FORMAT_Y410                        = 101,
			DXGI_FORMAT_Y416                        = 102,
			DXGI_FORMAT_NV12                        = 103,
			DXGI_FORMAT_P010                        = 104,
			DXGI_FORMAT_P016                        = 105,
			DXGI_FORMAT_420_OPAQUE                  = 106,
			DXGI_FORMAT_YUY2                        = 107,
			DXGI_FORMAT_Y210                        = 108,
			DXGI_FORMAT_Y216                        = 109,
			DXGI_FORMAT_NV11                        = 110,
			DXGI_FORMAT_AI44                        = 111,
			DXGI_FORMAT_IA44                        = 112,
			DXGI_FORMAT_P8                          = 113,
			DXGI_FORMAT_A8P8                        = 114,
			DXGI_FORMAT_B4G4R4A4_UNORM              = 115,
			DXGI_FORMAT_FORCE_UINT                  = 0xffffffff
		}

		/// <summary>Texture filtering flags</summary>
		[Flags] public enum EFilter :uint
		{
			D3DX_DEFAULT                 = 0xFFFFFFFF,
			D3DX_FILTER_NONE             = (1 << 0),
			D3DX_FILTER_POINT            = (2 << 0),
			D3DX_FILTER_LINEAR           = (3 << 0),
			D3DX_FILTER_TRIANGLE         = (4 << 0),
			D3DX_FILTER_BOX              = (5 << 0),

			D3DX_FILTER_MIRROR_U         = (1 << 16),
			D3DX_FILTER_MIRROR_V         = (2 << 16),
			D3DX_FILTER_MIRROR_W         = (4 << 16),
			D3DX_FILTER_MIRROR           = (7 << 16),

			D3DX_FILTER_DITHER           = (1 << 19),
			D3DX_FILTER_DITHER_DIFFUSION = (2 << 19),

			D3DX_FILTER_SRGB_IN          = (1 << 21),
			D3DX_FILTER_SRGB_OUT         = (2 << 21),
			D3DX_FILTER_SRGB             = (3 << 21),
		}

		public enum ELight
		{
			Ambient,
			Directional,
			Point,
			Spot
		}
		public enum EFillMode
		{
			Solid,
			Wireframe,
			SolidWire,
		}
		[Flags] public enum ENavBtn
		{
			Left   = 1 << 0,
			Right  = 1 << 1,
			Middle = 1 << 2
		}
		#endregion

		#region Structs

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct Vertex
		{
			public v4   m_pos;
			public v4   m_norm;
			public v2   m_uv;
			public uint m_col;
			public uint pad;
			public Vertex(v4 vert)                            { m_pos = vert; m_col = 0;   m_norm = v4.Zero; m_uv = v2.Zero; pad = 0; }
			public Vertex(v4 vert, uint col)                  { m_pos = vert; m_col = col; m_norm = v4.Zero; m_uv = v2.Zero; pad = 0; }
			public Vertex(v4 vert, v4 norm, uint col, v2 tex) { m_pos = vert; m_col = col; m_norm = norm;    m_uv = tex;     pad = 0; }
			public override string ToString()                 { return "V:<{0}> C:<{1}>".Fmt(m_pos, m_col.ToString("X8")); }
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct ImageInfo
		{
			public uint    m_width;
			public uint    m_height;
			public uint    m_depth;
			public uint    m_mips;
			public EFormat m_format; //DXGI_FORMAT
			public uint    m_image_file_format;//D3DXIMAGE_FILEFORMAT
			public float   m_aspect { get {return (float)m_width / m_height;} }
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct Material
		{
			public IntPtr m_diff_tex;
			public IntPtr m_env_map;
		}

		/// <summary>Light source properties</summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct View3DLight
		{
			public ELight       m_type;
			public bool         m_on;
			public v4           m_position;
			public v4           m_direction;
			public Colour32     m_ambient;
			public Colour32     m_diffuse;
			public Colour32     m_specular;
			public float        m_specular_power;
			public float        m_inner_cos_angle;
			public float        m_outer_cos_angle;
			public float        m_range;
			public float        m_falloff;
			public bool         m_cast_shadows;

			/// <summary>Return properties for a directional light source</summary>
			public static View3DLight Directional(v4 direction, Colour32 ambient, Colour32 diffuse, Colour32 specular, float spec_power, bool cast_shadows)
			{
				return new View3DLight
				{
					m_type           = ELight.Directional,
					m_on             = true,
					m_position       = v4.Origin,
					m_direction      = direction,
					m_ambient        = ambient,
					m_diffuse        = diffuse,
					m_specular       = specular,
					m_specular_power = spec_power,
					m_cast_shadows   = cast_shadows,
				};
			}
		}

		#endregion

		public class Exception : System.Exception
		{
			public EResult m_code = EResult.Success;
			public Exception() :this(EResult.Success) {}
			public Exception(EResult code) :this("", code) {}
			public Exception(string message) :this(message, EResult.Success) {}
			public Exception(string message, EResult code) :base(message) { m_code = code; }
		}

		public delegate void EditObjectCB(
			int vcount,
			int icount,
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)][Out] Vertex[] verts,
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)][Out] ushort[] indices,
			out int new_vcount,
			out int new_icount,
			out EPrim prim_type,
			out EGeom geom_type,
			ref Material mat,
			IntPtr ctx);

		/// <summary>Assign a handler to 'OnError' to hide the default message box</summary>
		public event ReportErrorCB OnError;
		public delegate void ReportErrorCB(string msg);

		/// <summary>Event notifying whenever rendering settings have changed</summary>
		public event SettingsChangedCB OnSettingsChanged;
		public delegate void SettingsChangedCB();

		//public delegate void OutputTerrainDataCB(IntPtr data, int size, IntPtr ctx);

		private IContainer components;                            // Required designer variable.
		private readonly ReportErrorCB m_error_cb;                // A local reference to prevent the callback being garbage collected
		private readonly SettingsChangedCB m_settings_changed_cb; // A local reference to prevent the callback being garbage collected
		private readonly List<DrawsetInterface> m_drawsets;
		private bool m_mouse_navigation;
		private int m_mouse_down_at;

		public View3D() :this(null) {}

		/// <summary>Provides an error callback. A reference is held within View3D, so callers don't need to hold one</summary>
		public View3D(ReportErrorCB error_cb)
		{
			if (Util.DesignTime) return;

			InitializeComponent();

			m_error_cb = ErrorCB;
			m_settings_changed_cb = SettingsChgCB;
			m_drawsets = new List<DrawsetInterface>();
			if (error_cb != null) OnError += error_cb;

			// Initialise the renderer
			var res = View3D_Initialise(Handle, m_error_cb, m_settings_changed_cb);
			if (res != EResult.Success) throw new Exception(res);

			ClickTimeMS = 180;
			MouseNavigation = true;
			DefaultKeyboardShortcuts = true;

			// Update the size of the control whenever we're added to a new parent
			ParentChanged += (s,a) => View3D_Resize(Width-2, Height-2);

			// Create a default drawset
			Drawset = DrawsetCreate();
		}

		/// <summary>Clean up any resources being used.</summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing) Close();
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>Shutdown the renderer</summary>
		public void Close()
		{
			while (m_drawsets.Count != 0)
				m_drawsets[0].Dispose();

			View3D_Shutdown();
		}

		/// <summary>Callback function from the Dll when an error occurs</summary>
		private void ErrorCB(string msg)
		{
			if (OnError != null) OnError(msg);
			else MessageBox.Show(msg, "View3D Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		/// <summary>Callback function from the Dll whenever the settings are changed</summary>
		private void SettingsChgCB()
		{
			// Forward changed settings notification to anyone that cares
			if (OnSettingsChanged != null) OnSettingsChanged();
		}

		/// <summary>Get/Set the currently active drawset</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DrawsetInterface Drawset
		{
			get { return m_current_drawset; }
			set
			{
				MouseNavigation = false;

				m_current_drawset = value;
				if (value == null) return;

				MouseNavigation = true;
			}
		}
		private DrawsetInterface m_current_drawset;

		/// <summary>Set the background colour of the control</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override Color BackColor
		{
			get { return base.BackColor; }
			set
			{
				base.BackColor = value;
				foreach (var ds in m_drawsets)
					ds.BackgroundColour = value;
			}
		}

		/// <summary>Enable/Disable default keyboard shortcuts</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool DefaultKeyboardShortcuts
		{
			set
			{
				KeyDown -= TranslateKey;
				if (value) KeyDown += TranslateKey;
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

		/// <summary>Mouse navigation - public to allow users to forward mouse calls to us.</summary>
		public void OnMouseDown(object sender, MouseEventArgs e)
		{
			Cursor = Cursors.SizeAll;
			MouseMove -= OnMouseMove;
			MouseMove += OnMouseMove;
			Capture = true;
			Navigate(e.Location, e.Button, true);
			m_mouse_down_at = Environment.TickCount;
		}
		public void OnMouseUp(object sender, MouseEventArgs e)
		{
			Cursor = Cursors.Default;
			MouseMove -= OnMouseMove;
			Capture = false;
			Navigate(e.Location, 0, true);

			// Short clicks bring up the context menu
			if (e.Button == MouseButtons.Right && Environment.TickCount - m_mouse_down_at < ClickTimeMS)
				ShowContextMenu();
		}
		public void OnMouseMove(object sender, MouseEventArgs e)
		{
			if (Drawset == null) return;
			Navigate(e.Location, e.Button, false);
			Drawset.Render();
		}
		public void OnMouseWheel(object sender, MouseEventArgs e)
		{
			if (Drawset == null) return;
			NavigateZ(e.Delta / 120f);
			Drawset.Render();
		}
		public void OnMouseDblClick(object sender, MouseEventArgs e)
		{
			if (Drawset == null) return;
			var btn_state = ButtonState(e.Button);
			if (Bit.AllSet(btn_state, (int)ENavBtn.Middle) || Bit.AllSet(btn_state, (int)(ENavBtn.Left|ENavBtn.Right)))
				Drawset.ResetZoom();
			Drawset.Render();
		}

		/// <summary>Direct X/Y navigation. 'point' is a point in client rect space.</summary>
		public void Navigate(Point point, MouseButtons mouse_btns, bool nav_start_or_end)
		{
			if (Drawset == null) return;
			View3D_Navigate(Drawset.Handle, NormalisePoint(point), ButtonState(mouse_btns), nav_start_or_end);
		}

		/// <summary>Direct Z navigation</summary>
		public void NavigateZ(float delta)
		{
			if (Drawset == null) return;
			View3D_NavigateZ(Drawset.Handle, delta);
		}

		/// <summary>Standard keyboard shortcuts</summary>
		public void TranslateKey(object sender, KeyEventArgs e)
		{
			if (Drawset == null) return;
			switch (e.KeyCode)
			{
			case Keys.F7:
				{
					Drawset.Camera.ResetView();
					Drawset.Render();
					break;
				}
			case Keys.Space:
				{
					ShowObjectManager(true);
					break;
				}
			case Keys.W:
				{
					if ((e.Modifiers & Keys.Control) != 0)
					{
						Drawset.FillMode = Enum<EFillMode>.Cycle(Drawset.FillMode);
						Drawset.Render();
					}
					break;
				}
			}
		}

		/// <summary>Create a drawset. Sets the current drawset to the one created</summary>
		public DrawsetInterface DrawsetCreate()
		{
			return new DrawsetInterface(this);
		}

		/// <summary>Show/Hide the object manager UI</summary>
		public void ShowObjectManager(bool show)
		{
			View3D_ShowObjectManager(show);
		}

		/// <summary>Create multiple objects from a source file and associate them with 'context_id'</summary>
		public void ObjectsCreateFromFile(string ldr_filepath, int context_id, bool async)
		{
			View3D_ObjectsCreateFromFile(ldr_filepath, context_id, async);
		}
		public void ObjectsCreateFromFile(string ldr_filepath, bool async)
		{
			ObjectsCreateFromFile(ldr_filepath, DefaultContextId, async);
		}

		/// <summary>Delete all objects matching 'context_id'</summary>
		public void ObjectsDelete(int context_id)
		{
			View3D_ObjectsDeleteById(context_id);
		}

		/// <summary>Show a window containing and example ldr script file</summary>
		public void ShowExampleScript()
		{
			View3D_ShowDemoScript();
		}

		/// <summary>Example for creating objects</summary>
		public void CreateDemoScene()
		{
			if (Drawset == null) return;
			View3D_CreateDemoScene(Drawset.Handle);

			//{// Create an object using ldr script
			//    HObject obj = ObjectCreate("*Box ldr_box FFFF00FF {1 2 3}");
			//    DrawsetAddObject(obj);
			//}

			//{// Create a box model, an instance for it, and add it to the drawset
			//    HModel model = CreateModelBox(new v4(0.3f, 0.2f, 0.4f, 0f), m4x4.Identity, 0xFFFF0000);
			//    HInstance instance = CreateInstance(model, m4x4.Identity);
			//    AddInstance(drawset, instance);
			//}

			//{// Create a mesh
			//    // Mesh data
			//    Vertex[] vert = new Vertex[]
			//    {
			//        new Vertex(new v4( 0f, 0f, 0f, 1f), v4.ZAxis, 0xFFFF0000, v2.Zero),
			//        new Vertex(new v4( 0f, 1f, 0f, 1f), v4.ZAxis, 0xFF00FF00, v2.Zero),
			//        new Vertex(new v4( 1f, 0f, 0f, 1f), v4.ZAxis, 0xFF0000FF, v2.Zero),
			//    };
			//    ushort[] face = new ushort[]
			//    {
			//        0, 1, 2
			//    };

			//    HModel model = CreateModel(vert.Length, face.Length, vert, face, EPrimType.D3DPT_TRIANGLELIST);
			//    HInstance instance = CreateInstance(model, m4x4.Identity);
			//    AddInstance(drawset, instance);
			//}
		}

		/// <summary>Convert a button enum to a button state int</summary>
		private static int ButtonState(MouseButtons button)
		{
			int btn_state = 0;
			if (Bit.AllSet((uint)button, (uint)MouseButtons.Left  )) btn_state |= (int)ENavBtn.Left;
			if (Bit.AllSet((uint)button, (uint)MouseButtons.Right )) btn_state |= (int)ENavBtn.Right;
			if (Bit.AllSet((uint)button, (uint)MouseButtons.Middle)) btn_state |= (int)ENavBtn.Middle;
			return btn_state;
		}

		/// <summary>Convert a screen space point to a normalised point</summary>
		private v2 NormalisePoint(Point pt)
		{
			return new v2(2f * pt.X / Width - 1f, 1f - 2f * pt.Y / Height);
		}

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
					obj_mgr_ui.Click += delegate { ShowObjectManager(true); };
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
					option.Items.AddRange(Enum<EFillMode>.Names.Cast<object>().ToArray());
					option.SelectedIndex = (int)Drawset.FillMode;
					option.SelectedIndexChanged += delegate { Drawset.FillMode = (EFillMode)option.SelectedIndex; Refresh(); };
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
			base.OnResize(e);
			View3D_Resize(Width-2, Height-2);
			Refresh();
		}

		/// <summary>Absorb PaintBackground events</summary>
		protected override void OnPaintBackground(PaintEventArgs e)
		{
			if (Drawset == null) base.OnPaintBackground(e);
		}

		/// <summary>Paint the control</summary>
		protected override void OnPaint(PaintEventArgs e)
		{
			// Render the control
			if (Drawset == null) base.OnPaint(e);
			else Drawset.Render();
		}

		/// <summary>Required method for Designer support - do not modify the contents of this method with the code editor.</summary>
		private void InitializeComponent()
		{
			components = new Container();
		}

		/// <summary>Namespace for the camera controls</summary>
		public class CameraControls
		{
			private readonly DrawsetInterface m_ds;
			public CameraControls(DrawsetInterface ds)
			{
				m_ds = ds;
			}

			/// <summary>Get/Set the camera align axis (camera up axis). Zero vector means no align axis is set</summary>
			public v4 AlignAxis
			{
				get { v4 up; View3D_CameraAlignAxis(m_ds.Handle, out up); return up; }
				set { View3D_AlignCamera(m_ds.Handle, ref value); }
			}

			/// <summary>Get/Set the camera view aspect ratio = Width/Height</summary>
			public float Aspect
			{
				get { return View3D_CameraAspect(m_ds.Handle); }
				set { View3D_SetCameraAspect(m_ds.Handle, value); }
			}

			/// <summary>Get/Set the camera horizontal field of view (in radians). Note aspect ratio is preserved, setting FovX changes FovY and visa versa</summary>
			public float FovX
			{
				get { return View3D_CameraFovX(m_ds.Handle); }
				set { View3D_SetCameraFovX(m_ds.Handle, value); }
			}

			/// <summary>Get/Set the camera vertical field of view (in radians). Note aspect ratio is preserved, setting FovY changes FovX and visa versa</summary>
			public float FovY
			{
				get { return View3D_CameraFovY(m_ds.Handle); }
				set { View3D_SetCameraFovY(m_ds.Handle, value); }
			}

			/// <summary>Get/Set the position of the camera focus point</summary>
			public v4 FocusPoint
			{
				get { v4 pos; View3D_GetFocusPoint(m_ds.Handle, out pos); return pos; }
				set { View3D_SetFocusPoint(m_ds.Handle, ref value); }
			}

			/// <summary>Get/Set the distance to the camera focus point</summary>
			public float FocusDist
			{
				get { return View3D_FocusDistance(m_ds.Handle); }
				set { View3D_SetFocusDistance(m_ds.Handle, value); }
			}

			/// <summary>Get/Set the camera to world transform. Note: use SetPosition to set the focus distance at the same time</summary>
			public m4x4 O2W
			{
				get { m4x4 c2w; View3D_CameraToWorld(m_ds.Handle, out c2w); return c2w; }
				set { View3D_SetCameraToWorld(m_ds.Handle, ref value); }
			}

			/// <summary>Set the camera to world transform and focus distance.</summary>
			public void SetPosition(v4 position, v4 lookat, v4 up)
			{
				View3D_PositionCamera(m_ds.Handle, ref position, ref lookat, ref up);
			}

			/// <summary>Move the camera to a position that can see the whole scene given camera directions 'forward' and 'up'</summary>
			public void ResetView(v4 forward, v4 up)
			{
				View3D_ResetView(m_ds.Handle, ref forward, ref up);
			}

			/// <summary>Move the camera to a position that can see the whole scene given camera direction 'forward'</summary>
			public void ResetView(v4 forward)
			{
				var up = AlignAxis;
				if (up.Length3Sq == 0f) up = v4.YAxis;
				if (v4.Parallel(up, forward)) up = v4.Perpendicular(forward);
				ResetView(forward, up);
			}

			/// <summary>Move the camera to a position that can see the whole scene</summary>
			public void ResetView()
			{
				var up = AlignAxis;
				if (up.Length3Sq == 0f) up = v4.YAxis;
				var forward = up.z > up.y ? v4.YAxis : v4.ZAxis;
				ResetView(forward, up);
			}
		}

		/// <summary>Methods for adding/removing objects from a drawset</summary>
		public class DrawsetInterface :IDisposable
		{
			private readonly View3D m_view;
			private readonly HDrawset m_ds;
			public DrawsetInterface(View3D view)
			{
				m_view = view;
				var res = View3D_DrawsetCreate(out m_ds);
				if (res != EResult.Success) throw new Exception(res);
				m_view.m_drawsets.Add(this);

				// Create a light source
				SetLightSource(v4.Origin, -v4.ZAxis, true);

				// Display the focus point
				FocusPointVisible = true;

				// Position the camera
				Camera = new CameraControls(this);
				Camera.SetPosition(new v4(0f, 0f, -2f, 1f), v4.Origin, v4.YAxis);
			}
			public void Dispose()
			{
				m_view.m_drawsets.Remove(this);
				if (m_view.Drawset == this) m_view.Drawset = null;
				View3D_DrawsetDelete(m_ds);
			}

			/// <summary>Get the native view3d handle for the drawset</summary>
			public IntPtr Handle { get { return m_ds; } }

			/// <summary>Import/Export a settings string for view3d</summary>
			public string Settings
			{
				get { return Marshal.PtrToStringAnsi(View3D_GetSettings(m_ds)); }
				set { View3D_SetSettings(m_ds, value); }
			}

			/// <summary>Camera controls</summary>
			public readonly CameraControls Camera;

			/// <summary>Add an object to the drawset</summary>
			public void AddObject(Object obj)
			{
				View3D_DrawsetAddObject(m_ds, obj.m_handle);
			}

			/// <summary>Add multiple objects by context id</summary>
			public void AddObjects(int context_id)
			{
				View3D_DrawsetAddObjectsById(m_ds, context_id);
			}

			/// <summary>Add a collection of objects to the drawset</summary>
			public void AddObjects(IEnumerable<Object> objects)
			{
				foreach (var obj in objects)
					View3D_DrawsetAddObject(m_ds, obj.m_handle);
			}

			/// <summary>Remove an object from the drawset</summary>
			public void RemoveObject(Object obj)
			{
				View3D_DrawsetRemoveObject(m_ds, obj.m_handle);
			}

			/// <summary>Remove multiple objects by context id</summary>
			public void RemoveObjects(int context_id)
			{
				View3D_DrawsetRemoveObjectsById(m_ds, context_id);
			}

			/// <summary>Remove a collection of objects from the drawset</summary>
			public void RemoveObjects(IEnumerable<Object> objects)
			{
				foreach (var obj in objects)
					View3D_DrawsetRemoveObject(m_ds, obj.m_handle);
			}

			/// <summary>Remove all instances from the drawset</summary>
			public void RemoveAllObjects()
			{
				View3D_DrawsetRemoveAllObjects(m_ds);
			}

			/// <summary>Return the number of objects in a drawset</summary>
			public int ObjectCount
			{
				get { return View3D_DrawsetObjectCount(m_ds); }
			}

			/// <summary>Show/Hide the focus point</summary>
			public bool FocusPointVisible
			{
				get { return View3D_FocusPointVisible(m_ds); }
				set { View3D_ShowFocusPoint(m_ds, value); }
			}

			/// <summary>Set the size of the focus point graphic</summary>
			public float FocusPointSize
			{
				set { View3D_SetFocusPointSize(m_ds, value); }
			}

			/// <summary>Show/Hide the origin point</summary>
			public bool OriginVisible
			{
				get { return View3D_OriginVisible(m_ds); }
				set { View3D_ShowOrigin(m_ds, value); }
			}

			/// <summary>Set the size of the origin graphic</summary>
			public float OriginSize
			{
				set { View3D_SetOriginSize(m_ds, value); }
			}

			/// <summary>Get/Set the render mode</summary>
			public EFillMode FillMode
			{
				get { return View3D_FillMode(m_ds); }
				set { View3D_SetFillMode(m_ds, value); }
			}

			/// <summary>Get/Set the light properties. Note returned value is a value type</summary>
			public View3DLight LightProperties
			{
				get { return View3D_LightProperties(m_ds); }
				set { View3D_SetLightProperties(m_ds, ref value); }
			}

			/// <summary>Show the lighting dialog</summary>
			public void ShowLightingDlg(Form parent)
			{
				View3D_ShowLightingDlg(m_ds, parent != null ? parent.Handle : IntPtr.Zero);
			}

			/// <summary>Set the single light source</summary>
			public void SetLightSource(v4 position, v4 direction, bool camera_relative)
			{
				View3D_LightSource(m_ds, ref position, ref direction, camera_relative);
			}

			/// <summary>Show/Hide the measuring tool</summary>
			public bool ShowMeasureTool
			{
				get { return View3D_MeasureToolVisible(); }
				set { View3D_ShowMeasureTool(m_ds, value); }
			}

			/// <summary>Show/Hide the angle tool</summary>
			public bool ShowAngleTool
			{
				get { return View3D_AngleToolVisible(); }
				set { View3D_ShowAngleTool(m_ds, value); }
			}

			/// <summary>Convert a screen space point into a position and direction in world space 'screen' should be in normalised screen space, i.e. (-1,-1)->(1,1) (lower left to upper right)</summary>
			public void WSRayFromScreenPoint(v2 screen, out v4 ws_point, out v4 ws_direction)
			{
				View3D_WSRayFromScreenPoint(m_ds, ref screen, out ws_point, out ws_direction);
			}

			/// <summary>Get/Set orthographic rendering mode</summary>
			public bool Orthographic
			{
				get { return View3D_Orthographic(m_ds); }
				set { View3D_SetOrthographic(m_ds, value); }
			}

			/// <summary>The background colour for the drawset</summary>
			public Color BackgroundColour
			{
				get { return Color.FromArgb(View3D_BackgroundColour(m_ds)); }
				set { View3D_SetBackgroundColour(m_ds, value.ToArgb()); }
			}

			/// <summary>Reset the zoom factor to 1f</summary>
			public void ResetZoom()
			{
				View3D_ResetZoom(m_ds);
			}

			/// <summary>Cause the drawset to be rendered</summary>
			public void Render()
			{
				View3D_Render(m_ds);
			}
		}

		/// <summary>Object resource wrapper</summary>
		public class Object :IDisposable
		{
			public HObject m_handle;
			public object Tag {get;set;}

			/// <summary>Create an object from a ldr script description</summary>
			public Object(string ldr_script, int context_id, bool async)
			{
				EResult res = View3D_ObjectCreateLdr(ldr_script, context_id, out m_handle, async);
				if (res != EResult.Success) throw new Exception(res);
			}
			public Object(string ldr_script)
			:this(ldr_script, DefaultContextId, false)
			{
			}

			/// <summary>Create an object via callback</summary>
			public Object(string name, uint colour, int icount, int vcount, EditObjectCB edit_cb, int context_id)
			{
				EResult res = View3D_ObjectCreate(name, colour, icount, vcount, edit_cb, IntPtr.Zero, context_id, out m_handle);
				if (res != EResult.Success) throw new Exception(res);
			}
			public Object(string name, uint colour, int icount, int vcount, EditObjectCB edit_cb)
			:this(name, colour, icount, vcount, edit_cb, DefaultContextId)
			{
			}

			/// <summary>Modify the model of this object</summary>
			public void Edit(EditObjectCB edit_cb)
			{
				View3D_ObjectEdit(m_handle, edit_cb, IntPtr.Zero);
			}

			/// <summary>Get/Set the object to parent transform</summary>
			public m4x4 O2P
			{
				get { return View3D_ObjectGetO2P(m_handle); }
				set { View3D_ObjectSetO2P(m_handle, ref value); }
			}

			/// <summary>Get the model space bounding box of this object</summary>
			public BBox BBoxMS
			{
				get { return View3D_ObjectBBoxMS(m_handle); }
			}

			/// <summary>Assign a texture to this object</summary>
			public void SetTexture(Texture tex)
			{
				View3D_ObjectSetTexture(m_handle, tex.m_handle);
			}

			/// <summary>Delete this object</summary>
			public void Dispose()
			{
				if (m_handle == IntPtr.Zero) return;
				View3D_ObjectDelete(m_handle);
				m_handle = IntPtr.Zero;
			}
		}

		/// <summary>Texture resource wrapper</summary>
		public class Texture :IDisposable
		{
			public HTexture m_handle;
			public ImageInfo m_info;
			public object Tag {get;set;}

			/// <summary>Construct an uninitialised texture</summary>
			public Texture(IntPtr data, uint data_size, uint width, uint height, uint mips, EFormat format)
			{
				EResult res = View3D_TextureCreate(data, data_size, width, height, mips, format, out m_handle);
				if (res != EResult.Success) throw new Exception(res);
				View3D_TextureGetInfo(m_handle, out m_info);
			}

			/// <summary>Construct a texture from a file</summary>
			public Texture(string tex_filepath)
			{
				EResult res = View3D_TextureCreateFromFile(tex_filepath, 0, 0, 0, (uint)EFilter.D3DX_DEFAULT, (uint)EFilter.D3DX_DEFAULT, 0, out m_handle);
				if (res != EResult.Success) throw new Exception(res);
				View3D_TextureGetInfo(m_handle, out m_info);
			}

			/// <summary>Construct a texture from a file</summary>
			public Texture(string tex_filepath, uint width, uint height, uint mips, EFilter filter, EFilter mip_filter, uint colour_key)
			{
				EResult res = View3D_TextureCreateFromFile(tex_filepath, width, height, mips, (uint)filter, (uint)mip_filter, colour_key, out m_handle);
				if (res != EResult.Success) throw new Exception(res);
				View3D_TextureGetInfo(m_handle, out m_info);
			}

			/// <summary>Fill a surface of this texture from a file</summary>
			public void LoadSurface(string tex_filepath, int level)
			{
				EResult res = View3D_TextureLoadSurface(m_handle, level, tex_filepath, null, null, (uint)EFilter.D3DX_DEFAULT, 0);
				if (res != EResult.Success) throw new Exception(res);
				View3D_TextureGetInfo(m_handle, out m_info);
			}

			/// <summary>Fill a surface of this texture from a file</summary>
			public void LoadSurface(string tex_filepath, int level, EFilter filter, uint colour_key)
			{
				EResult res = View3D_TextureLoadSurface(m_handle, level, tex_filepath, null, null, (uint)filter, colour_key);
				if (res != EResult.Success) throw new Exception(res);
				View3D_TextureGetInfo(m_handle, out m_info);
			}

			/// <summary>Fill a surface of this texture from a file</summary>
			public void LoadSurface(string tex_filepath, int level, Rectangle src_rect, Rectangle dst_rect, EFilter filter, uint colour_key)
			{
				var res = View3D_TextureLoadSurface(m_handle, level, tex_filepath, new []{dst_rect}, new []{src_rect}, (uint)filter, colour_key);
				if (res != EResult.Success) throw new Exception(res);
				View3D_TextureGetInfo(m_handle, out m_info);
			}

			/// <summary>Release resources held by this texture</summary>
			public void Dispose()
			{
				if (m_handle == IntPtr.Zero) return;
				View3D_TextureDelete(m_handle);
				m_handle = IntPtr.Zero;
			}

			/// <summary>Return properties of the texture</summary>
			public static ImageInfo GetInfo(string tex_filepath)
			{
				ImageInfo info;
				EResult res = View3D_TextureGetInfoFromFile(tex_filepath, out info);
				if (res != EResult.Success) throw new Exception(res);
				return info;
			}

			public override bool Equals(object obj) { return obj is Texture && m_handle.Equals(((Texture)obj).m_handle); }
			public override int GetHashCode()       { return m_handle.GetHashCode(); }
		}

		#region DLL extern functions

		private const string Dll = "view3d";
		private static IntPtr m_module;

		// A good idea is to add a static method in the class that is using this control
		// e.g.
		//  static MyThing()
		//  {
		//      View3D.SelectDll(System.Environment.Is64BitProcess
		//          ? @".\libs\x64\view3d.dll"
		//          : @".\libs\x86\view3d.dll");
		//  }

		/// <summary>Call this method to load a specific version of the view3d.dll. Call this before any DllImport'd functions</summary>
		public static void SelectDll(string dllpath)
		{
			m_module = LoadLibrary(dllpath);
			if (m_module == IntPtr.Zero)
				throw new System.Exception(string.Format("Failed to load dll {0}", dllpath));
		}
		[DllImport("Kernel32.dll")] private static extern IntPtr LoadLibrary(string path);

		// Initialise / shutdown the dll
		[DllImport(Dll)] private static extern EResult           View3D_Initialise(HWND hwnd, ReportErrorCB error_cb, SettingsChangedCB settings_changed_cb);
		[DllImport(Dll)] private static extern void              View3D_Shutdown();

		// Draw sets
		[DllImport(Dll)] private static extern IntPtr            View3D_GetSettings              (HDrawset drawset);
		[DllImport(Dll)] private static extern void              View3D_SetSettings              (HDrawset drawset, string settings);
		[DllImport(Dll)] private static extern void              View3D_DrawsetAddObjectsById    (HDrawset drawset, int context_id);
		[DllImport(Dll)] private static extern void              View3D_DrawsetRemoveObjectsById (HDrawset drawset, int context_id);
		[DllImport(Dll)] private static extern EResult           View3D_DrawsetCreate            (out HDrawset handle);
		[DllImport(Dll)] private static extern void              View3D_DrawsetDelete            (HDrawset drawset);
		[DllImport(Dll)] private static extern void              View3D_DrawsetAddObject         (HDrawset drawset, HObject obj);
		[DllImport(Dll)] private static extern void              View3D_DrawsetRemoveObject      (HDrawset drawset, HObject obj);
		[DllImport(Dll)] private static extern void              View3D_DrawsetRemoveAllObjects  (HDrawset drawset);
		[DllImport(Dll)] private static extern int               View3D_DrawsetObjectCount       (HDrawset drawset);

		// Camera
		[DllImport(Dll)] private static extern void              View3D_CameraToWorld            (HDrawset drawset, out m4x4 c2w);
		[DllImport(Dll)] private static extern void              View3D_SetCameraToWorld         (HDrawset drawset, ref m4x4 c2w);
		[DllImport(Dll)] private static extern void              View3D_PositionCamera           (HDrawset drawset, ref v4 position, ref v4 lookat, ref v4 up);
		[DllImport(Dll)] private static extern float             View3D_FocusDistance            (HDrawset drawset);
		[DllImport(Dll)] private static extern void              View3D_SetFocusDistance         (HDrawset drawset, float dist);
		[DllImport(Dll)] private static extern float             View3D_CameraAspect             (HDrawset drawset);
		[DllImport(Dll)] private static extern void              View3D_SetCameraAspect          (HDrawset drawset, float aspect);
		[DllImport(Dll)] private static extern float             View3D_CameraFovX               (HDrawset drawset);
		[DllImport(Dll)] private static extern void              View3D_SetCameraFovX            (HDrawset drawset, float fovX);
		[DllImport(Dll)] private static extern float             View3D_CameraFovY               (HDrawset drawset);
		[DllImport(Dll)] private static extern void              View3D_SetCameraFovY            (HDrawset drawset, float fovY);
		[DllImport(Dll)] private static extern void              View3D_Navigate                 (HDrawset drawset, v2 point, int button_state, bool nav_start_or_end);
		[DllImport(Dll)] private static extern void              View3D_NavigateZ                (HDrawset drawset, float delta);
		[DllImport(Dll)] private static extern void              View3D_ResetZoom                (HDrawset drawset);
		[DllImport(Dll)] private static extern void              View3D_CameraAlignAxis          (HDrawset drawset, out v4 axis);
		[DllImport(Dll)] private static extern void              View3D_AlignCamera              (HDrawset drawset, ref v4 axis);
		[DllImport(Dll)] private static extern void              View3D_ResetView                (HDrawset drawset, ref v4 forward, ref v4 up);
		[DllImport(Dll)] private static extern void              View3D_GetFocusPoint            (HDrawset drawset, out v4 position);
		[DllImport(Dll)] private static extern void              View3D_SetFocusPoint            (HDrawset drawset, ref v4 position);
		[DllImport(Dll)] private static extern void              View3D_WSRayFromScreenPoint     (HDrawset drawset, ref v2 screen, out v4 ws_point, out v4 ws_direction);

		// Lights
		[DllImport(Dll)] private static extern View3DLight       View3D_LightProperties          (HDrawset drawset);
		[DllImport(Dll)] private static extern void              View3D_SetLightProperties       (HDrawset drawset, ref View3DLight light);
		[DllImport(Dll)] private static extern void              View3D_LightSource              (HDrawset drawset, ref v4 position, ref v4 direction, bool camera_relative);
		[DllImport(Dll)] private static extern void              View3D_ShowLightingDlg          (HDrawset drawset, HWND parent);

		// Objects
		[DllImport(Dll)] private static extern EResult           View3D_ObjectsCreateFromFile    (string ldr_filepath, int context_id, bool async);
		[DllImport(Dll)] private static extern EResult           View3D_ObjectCreateLdr          (string ldr_script, int context_id, out HObject obj, bool async);
		[DllImport(Dll)] private static extern EResult           View3D_ObjectCreate             (string name, uint colour, int icount, int vcount, EditObjectCB edit_cb, IntPtr ctx, int context_id, out HObject obj);
		[DllImport(Dll)] private static extern void              View3D_ObjectsDeleteById        (int context_id);
		[DllImport(Dll)] private static extern void              View3D_ObjectDelete             (HObject obj);
		[DllImport(Dll)] private static extern void              View3D_ObjectEdit               (HObject obj, EditObjectCB edit_cb, IntPtr ctx);
		[DllImport(Dll)] private static extern m4x4              View3D_ObjectGetO2P             (HObject obj);
		[DllImport(Dll)] private static extern void              View3D_ObjectSetO2P             (HObject obj, ref m4x4 o2p);
		[DllImport(Dll)] private static extern void              View3D_ObjectSetTexture         (HObject obj, HTexture tex);
		[DllImport(Dll)] private static extern BBox              View3D_ObjectBBoxMS             (HObject obj);

		// Materials
		[DllImport(Dll)] private static extern EResult           View3D_TextureCreate            (IntPtr data, uint data_size, uint width, uint height, uint mips, EFormat format, out HTexture tex);
		[DllImport(Dll)] private static extern EResult           View3D_TextureCreateFromFile    (string tex_filepath, uint width, uint height, uint mips, uint filter, uint mip_filter, uint colour_key, out HTexture tex);
		[DllImport(Dll)] private static extern EResult           View3D_TextureLoadSurface       (HTexture tex, int level, string tex_filepath, Rectangle[] dst_rect, Rectangle[] src_rect, uint filter, uint colour_key);
		[DllImport(Dll)] private static extern void              View3D_TextureDelete            (HTexture tex);
		[DllImport(Dll)] private static extern void              View3D_TextureGetInfo           (HTexture tex, out ImageInfo info);
		[DllImport(Dll)] private static extern EResult           View3D_TextureGetInfoFromFile   (string tex_filepath, out ImageInfo info);

		// Rendering
		[DllImport(Dll)] private static extern void              View3D_Resize                   (int width, int height);
		[DllImport(Dll)] private static extern void              View3D_Render                   (HDrawset drawset);
		[DllImport(Dll)] private static extern EFillMode         View3D_FillMode                 (HDrawset drawset);
		[DllImport(Dll)] private static extern bool              View3D_Orthographic             (HDrawset drawset);
		[DllImport(Dll)] private static extern void              View3D_SetOrthographic          (HDrawset drawset, bool render2d);
		[DllImport(Dll)] private static extern void              View3D_SetFillMode              (HDrawset drawset, EFillMode mode);
		[DllImport(Dll)] private static extern int               View3D_BackgroundColour         (HDrawset drawset);
		[DllImport(Dll)] private static extern void              View3D_SetBackgroundColour      (HDrawset drawset, int aarrggbb);

		// Tools
		[DllImport(Dll)] private static extern bool              View3D_MeasureToolVisible       ();
		[DllImport(Dll)] private static extern void              View3D_ShowMeasureTool          (HDrawset drawset, bool show);
		[DllImport(Dll)] private static extern bool              View3D_AngleToolVisible         ();
		[DllImport(Dll)] private static extern void              View3D_ShowAngleTool            (HDrawset drawset, bool show);

		// Miscellaneous
		[DllImport(Dll)] private static extern void              View3D_CreateDemoScene          (HDrawset drawset);
		[DllImport(Dll)] private static extern void              View3D_ShowDemoScript           ();
		[DllImport(Dll)] private static extern bool              View3D_FocusPointVisible        (HDrawset drawset);
		[DllImport(Dll)] private static extern void              View3D_ShowFocusPoint           (HDrawset drawset, bool show);
		[DllImport(Dll)] private static extern void              View3D_SetFocusPointSize        (HDrawset drawset, float size);
		[DllImport(Dll)] private static extern bool              View3D_OriginVisible            (HDrawset drawset);
		[DllImport(Dll)] private static extern void              View3D_ShowOrigin               (HDrawset drawset, bool show);
		[DllImport(Dll)] private static extern void              View3D_SetOriginSize            (HDrawset drawset, float size);
		[DllImport(Dll)] private static extern void              View3D_ShowObjectManager        (bool show);
		#endregion
	}
}
