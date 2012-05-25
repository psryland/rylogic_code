using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using pr.gfx;
using pr.maths;
using HWND = System.IntPtr;
using HDrawset = System.IntPtr;
using HObject = System.IntPtr;
using HTexture = System.IntPtr;

namespace pr.gui
{
	public class View3D :UserControl
	{
		private const string View3Ddll = "view3d.dll";
		public const int DefaultContextId = 0;

		public enum EResult
		{
			Success,
			Failed,
		}
		[Flags] public enum ED3DCreateFlags :uint
		{
			D3DCREATE_FPU_PRESERVE                  = 0x00000002U,
			D3DCREATE_MULTITHREADED                 = 0x00000004U,
			D3DCREATE_PUREDEVICE                    = 0x00000010U,
			D3DCREATE_SOFTWARE_VERTEXPROCESSING     = 0x00000020U,
			D3DCREATE_HARDWARE_VERTEXPROCESSING     = 0x00000040U,
			D3DCREATE_MIXED_VERTEXPROCESSING        = 0x00000080U,
			D3DCREATE_DISABLE_DRIVER_MANAGEMENT     = 0x00000100U,
			D3DCREATE_ADAPTERGROUP_DEVICE           = 0x00000200U,
			D3DCREATE_DISABLE_DRIVER_MANAGEMENT_EX  = 0x00000400U,
			D3DCREATE_NOWINDOWCHANGES               = 0x00000800U,
		}
		public enum EPrimType
		{
			D3DPT_POINTLIST     = 1,
			D3DPT_LINELIST      = 2,
			D3DPT_LINESTRIP     = 3,
			D3DPT_TRIANGLELIST  = 4,
			D3DPT_TRIANGLESTRIP = 5,
			D3DPT_TRIANGLEFAN   = 6,
			D3DPT_FORCE_DWORD   = 0x7FFFFFFF,
		}
		public enum EFormat :uint
		{
			D3DFMT_UNKNOWN              =  0,
			D3DFMT_R8G8B8               = 20,
			D3DFMT_A8R8G8B8             = 21,
			D3DFMT_X8R8G8B8             = 22,
			D3DFMT_R5G6B5               = 23,
			D3DFMT_X1R5G5B5             = 24,
			D3DFMT_A1R5G5B5             = 25,
			D3DFMT_A4R4G4B4             = 26,
			D3DFMT_R3G3B2               = 27,
			D3DFMT_A8                   = 28,
			D3DFMT_A8R3G3B2             = 29,
			D3DFMT_X4R4G4B4             = 30,
			D3DFMT_A2B10G10R10          = 31,
			D3DFMT_A8B8G8R8             = 32,
			D3DFMT_X8B8G8R8             = 33,
			D3DFMT_G16R16               = 34,
			D3DFMT_A2R10G10B10          = 35,
			D3DFMT_A16B16G16R16         = 36,
			D3DFMT_A8P8                 = 40,
			D3DFMT_P8                   = 41,
			D3DFMT_L8                   = 50,
			D3DFMT_A8L8                 = 51,
			D3DFMT_A4L4                 = 52,
			D3DFMT_V8U8                 = 60,
			D3DFMT_L6V5U5               = 61,
			D3DFMT_X8L8V8U8             = 62,
			D3DFMT_Q8W8V8U8             = 63,
			D3DFMT_V16U16               = 64,
			D3DFMT_A2W10V10U10          = 67,
			D3DFMT_UYVY                 = ((byte)'U') | ((byte)'Y'<<8) | ((byte)'V'<<16) | ((byte)'Y'<<24),
			D3DFMT_R8G8_B8G8            = ((byte)'R') | ((byte)'G'<<8) | ((byte)'B'<<16) | ((byte)'G'<<24),
			D3DFMT_YUY2                 = ((byte)'Y') | ((byte)'U'<<8) | ((byte)'Y'<<16) | ((byte)'2'<<24),
			D3DFMT_G8R8_G8B8            = ((byte)'G') | ((byte)'R'<<8) | ((byte)'G'<<16) | ((byte)'B'<<24),
			D3DFMT_DXT1                 = ((byte)'D') | ((byte)'X'<<8) | ((byte)'T'<<16) | ((byte)'1'<<24),
			D3DFMT_DXT2                 = ((byte)'D') | ((byte)'X'<<8) | ((byte)'T'<<16) | ((byte)'2'<<24),
			D3DFMT_DXT3                 = ((byte)'D') | ((byte)'X'<<8) | ((byte)'T'<<16) | ((byte)'3'<<24),
			D3DFMT_DXT4                 = ((byte)'D') | ((byte)'X'<<8) | ((byte)'T'<<16) | ((byte)'4'<<24),
			D3DFMT_DXT5                 = ((byte)'D') | ((byte)'X'<<8) | ((byte)'T'<<16) | ((byte)'5'<<24),
			D3DFMT_D16_LOCKABLE         = 70,
			D3DFMT_D32                  = 71,
			D3DFMT_D15S1                = 73,
			D3DFMT_D24S8                = 75,
			D3DFMT_D24X8                = 77,
			D3DFMT_D24X4S4              = 79,
			D3DFMT_D16                  = 80,
			D3DFMT_D32F_LOCKABLE        = 82,
			D3DFMT_D24FS8               = 83,
			D3DFMT_L16                  = 81,
			D3DFMT_VERTEXDATA           = 100,
			D3DFMT_INDEX16              = 101,
			D3DFMT_INDEX32              = 102,
			D3DFMT_Q16W16V16U16         = 110,
			D3DFMT_MULTI2_ARGB8         = ((byte)'M') | ((byte)'E'<<8) | ((byte)'T'<<16) | ((byte)'1'<<24),
			D3DFMT_R16F                 = 111,
			D3DFMT_G16R16F              = 112,
			D3DFMT_A16B16G16R16F        = 113,
			D3DFMT_R32F                 = 114,
			D3DFMT_G32R32F              = 115,
			D3DFMT_A32B32G32R32F        = 116,
			D3DFMT_CxV8U8               = 117,
		}
		public enum EView3DLight
		{
			Ambient,
			Directional,
			Point,
			Spot
		}
		public enum EView3DGeom
		{
			EInvalid = 0,
			EVertex  = 1 << 0,
			ENormal  = 1 << 1,
			EColour  = 1 << 2,
			ETexture = 1 << 3,
			EAll     =(1 << 4) - 1,
			EVN      = EVertex | ENormal,
			EVC      = EVertex           | EColour,
			EVT      = EVertex                     | ETexture,
			EVNC     = EVertex | ENormal | EColour,
			EVNT     = EVertex | ENormal           | ETexture,
			EVCT     = EVertex           | EColour | ETexture,
			EVNCT    = EVertex | ENormal | EColour | ETexture,
		}
		public enum ERenderMode
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

		[StructLayout(LayoutKind.Sequential)]
		public struct Vertex
		{
			public v4   m_vert;
			public v4   m_norm;
			public uint m_col;
			public v2   m_tex;
			public Vertex(v4 vert)                            { m_vert = vert; m_norm = v4.Zero; m_col = 0; m_tex = v2.Zero; }
			public Vertex(v4 vert, uint col)                  { m_vert = vert; m_norm = v4.Zero; m_col = col; m_tex = v2.Zero; }
			public Vertex(v4 vert, v4 norm, uint col, v2 tex) { m_vert = vert; m_norm = norm; m_col = col; m_tex = tex; }
			public override string ToString()                 { return "V:<" + m_vert + "> C:<" + m_col.ToString("X8") + ">"; }
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct ImageInfo
		{
			public uint    m_width;
			public uint    m_height;
			public uint    m_depth;
			public uint    m_mips;
			public EFormat m_format; //D3DFORMAT
			public uint    m_image_file_format;//D3DXIMAGE_FILEFORMAT
			public float   m_aspect { get {return (float)m_width / m_height;} }
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct Material
		{
			public ushort m_geom_type; // see EView3DGeom
			public IntPtr m_diff_tex;
			public IntPtr m_env_map;
		}
	
		// Exception
		public class Exception : System.Exception
		{
			public EResult m_code = EResult.Success;
			public Exception() :this(EResult.Success) {}
			public Exception(EResult code) :this("", code) {}
			public Exception(string message) :this(message, EResult.Success) {}
			public Exception(string message, EResult code) :base(message) { m_code = code; }
		};

		public delegate void ReportErrorCB(string msg);
		public delegate void SettingsChangedCB();
		public delegate void OutputTerrainDataCB(IntPtr data, int size, IntPtr ctx);

		public delegate void EditObjectCB(
			int vcount,
			int icount,
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)][Out] Vertex[] verts,
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)][Out] ushort[] indices,
			ref int new_vcount,
			ref int new_icount,
			ref EPrimType prim_type,
			ref Material mat,
			IntPtr ctx);

		// Initialise/shutdown the dll
		[DllImport(View3Ddll)] private static extern EResult           View3D_Initialise(HWND hwnd, uint d3dcreate_flags, ReportErrorCB error_cb, SettingsChangedCB settings_changed_cb);
		[DllImport(View3Ddll)] private static extern void              View3D_Shutdown();
		
		// Draw sets
		[DllImport(View3Ddll)] private static extern IntPtr            View3D_GetSettings              (HDrawset drawset);
		[DllImport(View3Ddll)] private static extern void              View3D_SetSettings              (HDrawset drawset, string settings);
		[DllImport(View3Ddll)] private static extern void              View3D_DrawsetAddObjectsById    (HDrawset drawset, int context_id);
		[DllImport(View3Ddll)] private static extern void              View3D_DrawsetRemoveObjectsById (HDrawset drawset, int context_id);
		[DllImport(View3Ddll)] private static extern EResult           View3D_DrawsetCreate            (out HDrawset handle);
		[DllImport(View3Ddll)] private static extern void              View3D_DrawsetDelete            (HDrawset drawset);
		[DllImport(View3Ddll)] private static extern void              View3D_DrawsetAddObject         (HDrawset drawset, HObject obj);
		[DllImport(View3Ddll)] private static extern void              View3D_DrawsetRemoveObject      (HDrawset drawset, HObject obj);
		[DllImport(View3Ddll)] private static extern void              View3D_DrawsetRemoveAllObjects  (HDrawset drawset);
		[DllImport(View3Ddll)] private static extern int               View3D_DrawsetObjectCount       (HDrawset drawset);

		// Camera
		[DllImport(View3Ddll)] private static extern void              View3D_CameraToWorld            (HDrawset drawset, out m4x4 c2w);
		[DllImport(View3Ddll)] private static extern void              View3D_SetCameraToWorld         (HDrawset drawset, ref m4x4 c2w);
		[DllImport(View3Ddll)] private static extern void              View3D_PositionCamera           (HDrawset drawset, ref v4 position, ref v4 lookat, ref v4 up);
		[DllImport(View3Ddll)] private static extern float             View3D_FocusDistance            (HDrawset drawset);
		[DllImport(View3Ddll)] private static extern void              View3D_SetFocusDistance         (HDrawset drawset, float dist);
		[DllImport(View3Ddll)] private static extern float             View3D_CameraAspect             (HDrawset drawset);
		[DllImport(View3Ddll)] private static extern void              View3D_SetCameraAspect          (HDrawset drawset, float aspect);
		[DllImport(View3Ddll)] private static extern float             View3D_CameraFovX               (HDrawset drawset);
		[DllImport(View3Ddll)] private static extern void              View3D_SetCameraFovX            (HDrawset drawset, float fovX);
		[DllImport(View3Ddll)] private static extern float             View3D_CameraFovY               (HDrawset drawset);
		[DllImport(View3Ddll)] private static extern void              View3D_SetCameraFovY            (HDrawset drawset, float fovY);
		[DllImport(View3Ddll)] private static extern void              View3D_Navigate                 (HDrawset drawset, v2 point, int button_state, bool nav_start_or_end);
		[DllImport(View3Ddll)] private static extern void              View3D_NavigateZ                (HDrawset drawset, float delta);
		[DllImport(View3Ddll)] private static extern void              View3D_ResetZoom                (HDrawset drawset);
		[DllImport(View3Ddll)] private static extern void              View3D_CameraAlignAxis          (HDrawset drawset, out v4 axis);
		[DllImport(View3Ddll)] private static extern void              View3D_AlignCamera              (HDrawset drawset, ref v4 axis);
		[DllImport(View3Ddll)] private static extern void              View3D_ResetView                (HDrawset drawset, ref v4 forward, ref v4 up);
		[DllImport(View3Ddll)] private static extern void              View3D_GetFocusPoint            (HDrawset drawset, out v4 position);
		[DllImport(View3Ddll)] private static extern void              View3D_SetFocusPoint            (HDrawset drawset, ref v4 position);
		[DllImport(View3Ddll)] private static extern void              View3D_WSRayFromScreenPoint     (HDrawset drawset, ref v2 screen, out v4 ws_point, out v4 ws_direction);

		// Lights
		[DllImport(View3Ddll)] private static extern View3DLight       View3D_LightProperties          (HDrawset drawset);
		[DllImport(View3Ddll)] private static extern void              View3D_SetLightProperties       (HDrawset drawset, ref View3DLight light);
		[DllImport(View3Ddll)] private static extern void              View3D_LightSource              (HDrawset drawset, ref v4 position, ref v4 direction, bool camera_relative);
		[DllImport(View3Ddll)] private static extern void              View3D_ShowLightingDlg          (HDrawset drawset, HWND parent);
	
		// Objects
		[DllImport(View3Ddll)] private static extern EResult           View3D_ObjectsCreateFromFile    (string ldr_filepath, int context_id, bool async);
		[DllImport(View3Ddll)] private static extern EResult           View3D_ObjectCreateLdr          (string ldr_script, int context_id, out HObject obj, bool async);
		[DllImport(View3Ddll)] private static extern EResult           View3D_ObjectCreate             (string name, uint colour, int icount, int vcount, EditObjectCB edit_cb, IntPtr ctx, int context_id, out HObject obj);
		[DllImport(View3Ddll)] private static extern void              View3D_ObjectsDeleteById        (int context_id);
		[DllImport(View3Ddll)] private static extern void              View3D_ObjectDelete             (HObject obj);
		[DllImport(View3Ddll)] private static extern void              View3D_ObjectEdit               (HObject obj, EditObjectCB edit_cb, IntPtr ctx);
		[DllImport(View3Ddll)] private static extern m4x4              View3D_ObjectGetO2P             (HObject obj);
		[DllImport(View3Ddll)] private static extern void              View3D_ObjectSetO2P             (HObject obj, ref m4x4 o2p);
		[DllImport(View3Ddll)] private static extern void              View3D_ObjectSetTexture         (HObject obj, HTexture tex);
		[DllImport(View3Ddll)] private static extern BBox              View3D_ObjectBBoxMS             (HObject obj);

		// Materials
		[DllImport(View3Ddll)] private static extern EResult           View3D_TextureCreate            (IntPtr data, uint data_size, uint width, uint height, uint mips, EFormat format, out HTexture tex);
		[DllImport(View3Ddll)] private static extern EResult           View3D_TextureCreateFromFile    (string tex_filepath, uint width, uint height, uint mips, uint filter, uint mip_filter, uint colour_key, out HTexture tex);
		[DllImport(View3Ddll)] private static extern EResult           View3D_TextureLoadSurface       (HTexture tex, int level, string tex_filepath, Rectangle[] dst_rect, Rectangle[] src_rect, uint filter, uint colour_key);
		[DllImport(View3Ddll)] private static extern void              View3D_TextureDelete            (HTexture tex);
		[DllImport(View3Ddll)] private static extern void              View3D_TextureGetInfo           (HTexture tex, out ImageInfo info);
		[DllImport(View3Ddll)] private static extern EResult           View3D_TextureGetInfoFromFile   (string tex_filepath, out ImageInfo info);

		// Rendering
		[DllImport(View3Ddll)] private static extern void              View3D_Resize                   (int width, int height);
		[DllImport(View3Ddll)] private static extern void              View3D_Render                   (HDrawset drawset);
		[DllImport(View3Ddll)] private static extern ERenderMode       View3D_RenderMode               (HDrawset drawset);
		[DllImport(View3Ddll)] private static extern bool              View3D_Orthographic             (HDrawset drawset);
		[DllImport(View3Ddll)] private static extern void              View3D_SetOrthographic          (HDrawset drawset, bool render2d);
		[DllImport(View3Ddll)] private static extern void              View3D_SetRenderMode            (HDrawset drawset, ERenderMode mode);
		[DllImport(View3Ddll)] private static extern int               View3D_BackGroundColour         (HDrawset drawset);
		[DllImport(View3Ddll)] private static extern void              View3D_SetBackgroundColour      (HDrawset drawset, int aarrggbb);

		// Tools
		[DllImport(View3Ddll)] private static extern bool              View3D_MeasureToolVisible       ();
		[DllImport(View3Ddll)] private static extern void              View3D_ShowMeasureTool          (HDrawset drawset, bool show);
		[DllImport(View3Ddll)] private static extern bool              View3D_AngleToolVisible         ();
		[DllImport(View3Ddll)] private static extern void              View3D_ShowAngleTool            (HDrawset drawset, bool show);

		// Miscellaneous
		[DllImport(View3Ddll)] private static extern void              View3D_CreateDemoScene          (HDrawset drawset);
		[DllImport(View3Ddll)] private static extern void              View3D_ShowDemoScript           ();
		[DllImport(View3Ddll)] private static extern bool              View3D_FocusPointVisible        (HDrawset drawset);
		[DllImport(View3Ddll)] private static extern void              View3D_ShowFocusPoint           (HDrawset drawset, bool show);
		[DllImport(View3Ddll)] private static extern void              View3D_SetFocusPointSize        (HDrawset drawset, float size);
		[DllImport(View3Ddll)] private static extern bool              View3D_OriginVisible            (HDrawset drawset);
		[DllImport(View3Ddll)] private static extern void              View3D_ShowOrigin               (HDrawset drawset, bool show);
		[DllImport(View3Ddll)] private static extern void              View3D_SetOriginSize            (HDrawset drawset, float size);
		[DllImport(View3Ddll)] private static extern void              View3D_ShowObjectManager        (bool show);

		/// <summary>Assign a handler to 'OnError' to hide the default message box</summary>
		public event ReportErrorCB OnError;

		/// <summary>Event notifying whenever rendering settings have changed</summary>
		public event SettingsChangedCB OnSettingsChanged;

		private IContainer components;                            // Required designer variable.
		private HDrawset m_drawset = HDrawset.Zero;               // The currently selected drawset
		private readonly ReportErrorCB m_error_cb;                // A local reference to prevent the callback being garbage collected
		private readonly SettingsChangedCB m_settings_changed_cb; // A local reference to prevent the callback being garbage collected
		private readonly bool m_dll_found;
		private bool m_mouse_navigation;
		private int m_click_time_ms = 180;
		private int m_mouse_down_at;

		// Note about keyboard events:
		//	Users should hook up to the KeyDown event with the handler they want.
		// E.g. "Refresh"
		//	view3d.KeyDown += delegate (object sender, KeyEventArgs e)
		//	{
		//		if (e.KeyCode == Keys.F5)
		//			ReloadScene();
		//	};

		public View3D() :this(null, 0) {}
		public View3D(ReportErrorCB error_cb, ED3DCreateFlags d3dcreate_flags)
		{
			InitializeComponent();
			if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
				return;

			m_error_cb = ErrorCB;
			m_settings_changed_cb = SettingsChgCB;
			if (error_cb != null) OnError += error_cb;
			
			// Initialise the renderer
			IntPtr hwnd = Handle;
			EResult res = View3D_Initialise(hwnd, (uint)d3dcreate_flags, m_error_cb, m_settings_changed_cb);
			if (res != EResult.Success) throw new Exception(res);
			m_dll_found = true;

			MouseNavigation = true;
			DefaultKeyboardShortcuts = true;

			// Update the size of the control whenever we're added to a new parent
			ParentChanged += delegate { View3D_Resize(Width-2, Height-2); };

			// Create a default drawset
			DrawsetCreate();
		}

		/// <summary>Set the background colour of the control</summary>
		public override Color BackColor
		{
			get { return base.BackColor; }
			set { base.BackColor = value; if (m_dll_found) View3D_SetBackgroundColour(m_drawset, value.ToArgb()); }
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
		public int ClickTimeMS
		{
			get { return m_click_time_ms; }
			set { m_click_time_ms = value; }
		}
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
			Navigate(e.Location, e.Button, false);
			View3D_Render(m_drawset);
		}
		public void OnMouseWheel(object sender, MouseEventArgs e)
		{
			NavigateZ(e.Delta / 120f);
			View3D_Render(m_drawset);
		}
		public void OnMouseDblClick(object sender, MouseEventArgs e)
		{
			int btn_state = ButtonState(e.Button);
			if (Bit.AllSet(btn_state, (int)ENavBtn.Middle) || Bit.AllSet(btn_state, (int)(ENavBtn.Left|ENavBtn.Right)))
				View3D_ResetZoom(m_drawset);
			View3D_Render(m_drawset);
		}

		/// <summary>Direct X/Y navigation. 'point' is a point in client rect space.</summary>
		public void Navigate(Point point, MouseButtons mouse_btns, bool nav_start_or_end)
		{
			View3D_Navigate(m_drawset, NormalisePoint(point), ButtonState(mouse_btns), nav_start_or_end);
		}

		// Direct Z navigation
		public void NavigateZ(float delta)
		{
			View3D_NavigateZ(m_drawset, delta);
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

		/// <summary>Standard keyboard shortcuts</summary>
		public void TranslateKey(object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
			default:break;
			case Keys.F7:
				ResetView();
				View3D_Render(m_drawset);
				break;
			case Keys.Space:
				ShowObjectManager(true);
				break;
			case Keys.W:
				if ((e.Modifiers & Keys.Control) != 0)
				{
					int mode = (int)View3D_RenderMode(Drawset) + 1;
					int modes = Enum.GetValues(typeof(ERenderMode)).Length;
					RenderMode = (ERenderMode)(mode % modes);
					View3D_Render(m_drawset);
				}break;
			}
		}

		/// <summary>Shutdown the renderer</summary>
		public void Close()
		{
			DrawsetDelete(Drawset);
			if (m_dll_found) View3D_Shutdown();
		}

		/// <summary>Import/Export a settings string for view3d</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string Settings
		{
			get { return m_dll_found ? Marshal.PtrToStringAnsi(View3D_GetSettings(Drawset)) : ""; }
			set { if (m_dll_found) View3D_SetSettings(Drawset, value); }
		}

		/// <summary>Set the currently active drawset</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public HDrawset Drawset
		{
			get { return m_drawset; }
			set
			{
				MouseNavigation = false;

				m_drawset = value;
				if (m_drawset == HDrawset.Zero) return;

				MouseNavigation = true;
			}
		}

		/// <summary>Create a drawset. Sets the current drawset to the one created</summary>
		public HDrawset DrawsetCreate()
		{
			// Create a drawset
			HDrawset drawset;
			EResult res = View3D_DrawsetCreate(out drawset);
			if (res != EResult.Success) throw new Exception(res);
			if (Drawset != IntPtr.Zero) DrawsetDelete(Drawset);
			Drawset = drawset;

			// Display the focus point
			FocusPointVisible = true;

			// Create a light source
			SetLightSource(v4.Origin, -v4.ZAxis, true);

			// Position the camera
			PositionCamera(new v4(0f, 0f, -2f, 1f), v4.Origin, v4.YAxis);

			return m_drawset;
		}
		public void DrawsetDelete(HDrawset drawset)
		{
			if (m_drawset == drawset) Drawset = HDrawset.Zero;
			View3D_DrawsetDelete(drawset);
		}
		
		/// <summary>Add multiple objects by context id</summary>
		public void DrawsetAddObjects(int context_id)
		{
			View3D_DrawsetAddObjectsById(m_drawset, context_id);
		}

		/// <summary>Remove multiple objects by context id</summary>
		public void DrawsetRemoveObjects(int context_id)
		{
			View3D_DrawsetRemoveObjectsById(m_drawset, context_id);
		}
	
		/// <summary>Add an object to the drawset</summary>
		public void DrawsetAddObject(Object obj)
		{
			View3D_DrawsetAddObject(m_drawset, obj.m_handle);
		}

		/// <summary>Add a collection of objects to the drawset</summary>
		public void DrawsetAddObjects(IEnumerable<Object> objects)
		{
			foreach (Object obj in objects)
				View3D_DrawsetAddObject(m_drawset, obj.m_handle);
		}

		/// <summary>Remove an object from the drawset</summary>
		public void DrawsetRemoveObject(Object obj)
		{
			View3D_DrawsetRemoveObject(m_drawset, obj.m_handle);
		}

		/// <summary>Remove a collection of objects from the drawset</summary>
		public void DrawsetRemoveObjects(IEnumerable<Object> objects)
		{
			foreach (Object obj in objects)
				View3D_DrawsetRemoveObject(m_drawset, obj.m_handle);
		}

		/// <summary>Remove all instances from the drawset</summary>
		public void DrawsetRemoveAllObjects()
		{
			View3D_DrawsetRemoveAllObjects(m_drawset);
		}

		/// <summary>Return the number of objects in a drawset</summary>
		public int DrawsetObjectCount()
		{
			return View3D_DrawsetObjectCount(m_drawset);
		}

		/// <summary>Get/Set the position of the camera focus point</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public v4 FocusPoint
		{
			get { v4 pos = v4.Origin; if (m_dll_found) View3D_GetFocusPoint(m_drawset, out pos); return pos; }
			set { if (m_dll_found) View3D_SetFocusPoint(m_drawset, ref value); }
		}

		/// <summary>Get/Set the camera position. Note: use LookAt to set the focus distance at the same time</summary>
		[Browsable(false)]
		public m4x4 CameraToWorld
		{
			get { m4x4 c2w = m4x4.Identity; if (m_dll_found) View3D_CameraToWorld(m_drawset, out c2w); return c2w; }
			set { if (m_dll_found) View3D_SetCameraToWorld(m_drawset, ref value); }
		}
		public void PositionCamera(v4 position, v4 lookat, v4 up)
		{
			View3D_PositionCamera(m_drawset, ref position, ref lookat, ref up);
		}
		public void ResetView(v4 forward, v4 up)
		{
			View3D_ResetView(m_drawset, ref forward, ref up);
		}
		public void ResetView(v4 forward)
		{
			v4 up; View3D_CameraAlignAxis(m_drawset, out up);
			if (up.Length3Sq == 0f) up = v4.YAxis;
			if (v4.Parallel(up, forward)) up = v4.Perpendicular(forward);
			ResetView(forward, up);
		}
		public void ResetView()
		{
			v4 up; View3D_CameraAlignAxis(m_drawset, out up);
			if (up.Length3Sq == 0f) up = v4.YAxis;
			v4 forward = up.z > up.y ? v4.YAxis : v4.ZAxis;
			ResetView(forward, up);
		}
		public void Align(v4 axis)
		{
			View3D_AlignCamera(m_drawset, ref axis);
		}

		/// <summary>Get/Set the distance to the camera focus point</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public float CameraFocusDist
		{
			get { return m_dll_found ? View3D_FocusDistance(m_drawset) : 1f; }
			set { if (m_dll_found) View3D_SetFocusDistance(m_drawset, value); }
		}

		/// <summary>Get/Set the camera view aspect ratio = Width/Height</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public float CameraAspect
		{
			get { return m_dll_found ? View3D_CameraAspect(m_drawset) : 1f; }
			set { if (m_dll_found) View3D_SetCameraAspect(m_drawset, value); }
		}

		/// <summary>Get/Set the camera horizontal field of view (in radians). Note aspect ratio is preserved, setting FovX changes FovY and visa versa</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public float CameraFovX
		{
			get { return m_dll_found ? View3D_CameraFovX(m_drawset) : 1f; }
			set { if (m_dll_found) View3D_SetCameraFovX(m_drawset, value); }
		}

		/// <summary>Get/Set the camera vertical field of view (in radians). Note aspect ratio is preserved, setting FovY changes FovX and visa versa</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public float CameraFovY
		{
			get { return m_dll_found ? View3D_CameraFovY(m_drawset) : 1f; }
			set { if (m_dll_found) View3D_SetCameraFovY(m_drawset, value); }
		}

		/// <summary>Convert a screen space point into a position and direction in world space 'screen' should be in normalised screen space, i.e. (-1,-1)->(1,1) (lower left to upper right)</summary>
		public void WSRayFromScreenPoint(HDrawset drawset, v2 screen, out v4 ws_point, out v4 ws_direction)
		{
			View3D_WSRayFromScreenPoint(drawset, ref screen, out ws_point, out ws_direction);
		}

		/// <summary>Show/Hide the focus point</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool FocusPointVisible
		{
			get { return m_dll_found ? View3D_FocusPointVisible(m_drawset) : false; }
			set { if (m_dll_found) View3D_ShowFocusPoint(m_drawset, value); }
		}

		/// <summary>Set the size of the focus point graphic</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public float FocusPointSize
		{
			set { View3D_SetFocusPointSize(Drawset, value); }
		}

		/// <summary>Show/Hide the origin point</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool OriginVisible
		{
			get { return m_dll_found ? View3D_OriginVisible(m_drawset) : false; }
			set { if (m_dll_found) View3D_ShowOrigin(m_drawset, value); }
		}

		/// <summary>Set the size of the origin graphic</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public float OriginSize
		{
			set { View3D_SetOriginSize(Drawset, value); }
		}

		/// <summary>Get/Set the render mode</summary>
		[Browsable(false)]
		public ERenderMode RenderMode
		{
			get { return m_dll_found ? View3D_RenderMode(Drawset) : ERenderMode.Solid; }
			set { if (m_dll_found) View3D_SetRenderMode(Drawset, value); }
		}

		/// <summary>Show/Hide the object manager UI</summary>
		public void ShowObjectManager(bool show)
		{
			if (m_dll_found) View3D_ShowObjectManager(show);
		}

		/// <summary>Get/Set the light properties. Note returned value is a value type</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public View3DLight LightProperties
		{
			get { return m_dll_found ? View3D_LightProperties(m_drawset) : new View3DLight(); }
			set { if (m_dll_found) View3D_SetLightProperties(m_drawset, ref value); }
		}

		/// <summary>Set the single light source</summary>
		public void SetLightSource(v4 position, v4 direction, bool camera_relative)
		{
			if (m_dll_found) View3D_LightSource(m_drawset, ref position, ref direction, camera_relative);
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

		/// <summary>Show/Hide the measuring tool</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool ShowMeasureTool
		{
			get { return m_dll_found ? View3D_MeasureToolVisible() : false; }
			set { if (m_dll_found) View3D_ShowMeasureTool(m_drawset, value); }
		}

		/// <summary>Show/Hide the angle tool</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool ShowAngleTool
		{
			get { return m_dll_found ? View3D_AngleToolVisible() : false; }
			set { if (m_dll_found) View3D_ShowAngleTool(m_drawset, value); }
		}

		/// <summary>Show a window containing and example ldr script file</summary>
		public void ShowExampleScript()
		{
			View3D_ShowDemoScript();
		}

		/// <summary>Example for creating objects</summary>
		public void CreateDemoScene()
		{
			View3D_CreateDemoScene(m_drawset);

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
			ContextMenuStrip context_menu = new ContextMenuStrip();
			context_menu.Closed += delegate { Refresh(); };

			{// View
				ToolStripMenuItem view_menu = new ToolStripMenuItem("View");
				context_menu.Items.Add(view_menu);
				{// Show focus
					ToolStripMenuItem show_focus_menu = new ToolStripMenuItem("Show Focus");
					view_menu.DropDownItems.Add(show_focus_menu);
					show_focus_menu.Checked = FocusPointVisible;
					show_focus_menu.Click += delegate { FocusPointVisible = !FocusPointVisible; Refresh(); };
				}
				{// Show Origin
					ToolStripMenuItem show_origin_menu = new ToolStripMenuItem("Show Origin");
					view_menu.DropDownItems.Add(show_origin_menu);
					show_origin_menu.Checked = OriginVisible;
					show_origin_menu.Click += delegate { OriginVisible = !OriginVisible; Refresh(); };
				}
				{// Show coords
				}
				{// Axis Views
					ToolStripComboBox view_options = new ToolStripComboBox("Views");
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
						v4 pos = FocusPoint;
						switch (view_options.SelectedIndex)
						{
						default: break;
						case 1: ResetView( v4.XAxis); break;
						case 2: ResetView(-v4.XAxis); break;
						case 3: ResetView( v4.YAxis); break;
						case 4: ResetView(-v4.YAxis); break;
						case 5: ResetView( v4.ZAxis); break;
						case 6: ResetView(-v4.ZAxis); break;
						case 7: ResetView(-v4.XAxis-v4.YAxis-v4.ZAxis); break;
						}
						FocusPoint = pos;
						Refresh();
					};
				}
				{// Object Manager UI
					ToolStripMenuItem obj_mgr_ui = new ToolStripMenuItem("Object Manager");
					view_menu.DropDownItems.Add(obj_mgr_ui);
					obj_mgr_ui.Click += delegate { ShowObjectManager(true); };
				}
			}
			{// Navigation
				ToolStripMenuItem rdr_menu = new ToolStripMenuItem("Navigation");
				context_menu.Items.Add(rdr_menu);
				{// Reset View
					ToolStripMenuItem reset_menu = new ToolStripMenuItem("Reset View");
					rdr_menu.DropDownItems.Add(reset_menu);
					reset_menu.Click += delegate { ResetView(); Refresh(); };
				}
				{// Align to
					ToolStripMenuItem align_menu = new ToolStripMenuItem("Align");
					rdr_menu.DropDownItems.Add(align_menu);
					{
						ToolStripComboBox align_options = new ToolStripComboBox("Aligns");
						align_menu.DropDownItems.Add(align_options);
						align_options.Items.Add("None");
						align_options.Items.Add("X");
						align_options.Items.Add("Y");
						align_options.Items.Add("Z");
						
						v4 axis;
						View3D_CameraAlignAxis(m_drawset, out axis);
						if      (v4.FEql3(axis, v4.XAxis))	{ align_options.SelectedIndex = 1; }
						else if (v4.FEql3(axis, v4.YAxis))	{ align_options.SelectedIndex = 2; }	
						else if (v4.FEql3(axis, v4.ZAxis))	{ align_options.SelectedIndex = 3; }
						else								{ align_options.SelectedIndex = 0; }
						align_options.SelectedIndexChanged += delegate
						{
							switch (align_options.SelectedIndex)
							{
							default: Align(v4.Zero);  break;
							case 1:  Align(v4.XAxis); break;
							case 2:  Align(v4.YAxis); break;
							case 3:  Align(v4.ZAxis); break;
							}
							Refresh();
						};
					}
				}
				{// Motion lock
				}
				{// Orbit
					ToolStripMenuItem orbit_menu = new ToolStripMenuItem("Orbit");
					rdr_menu.DropDownItems.Add(orbit_menu);
					orbit_menu.Click += delegate {};
				}
			}
			{// Tools
				ToolStripMenuItem tools_menu = new ToolStripMenuItem("Tools");
				context_menu.Items.Add(tools_menu);
				{// Measure
					ToolStripMenuItem option = new ToolStripMenuItem{Text = "Measure..."};
					tools_menu.DropDownItems.Add(option);
					option.Click += delegate { View3D_ShowMeasureTool(m_drawset, true); };
				}
				{// Angle
					ToolStripMenuItem option = new ToolStripMenuItem{Text = "Angle..."};
					tools_menu.DropDownItems.Add(option);
					option.Click += delegate { View3D_ShowAngleTool(m_drawset, true); };
				}
			}
			{// Rendering
				ToolStripMenuItem rdr_menu = new ToolStripMenuItem("Rendering");
				context_menu.Items.Add(rdr_menu);
				{// Solid/Wireframe/Solid+Wire
					ToolStripComboBox option = new ToolStripComboBox();
					rdr_menu.DropDownItems.Add(option);
					option.Items.AddRange(Enum.GetNames(typeof(ERenderMode)));
					option.SelectedIndex = (int)View3D_RenderMode(m_drawset);
					option.SelectedIndexChanged += delegate { View3D_SetRenderMode(m_drawset, (ERenderMode)option.SelectedIndex); Refresh(); };
				}
				{// Render2D
					ToolStripMenuItem option = new ToolStripMenuItem{Text = View3D_Orthographic(m_drawset) ? "Perspective" : "Orthographic"};
					rdr_menu.DropDownItems.Add(option);
					option.Click += delegate { bool _2d = View3D_Orthographic(m_drawset); View3D_SetOrthographic(m_drawset, !_2d); option.Text = _2d ? "Perspective" : "Orthographic"; Refresh(); };
				}
				{// Lighting...
					ToolStripMenuItem lighting_menu = new ToolStripMenuItem("Lighting...");
					rdr_menu.DropDownItems.Add(lighting_menu);
					lighting_menu.Click += delegate { View3D_ShowLightingDlg(m_drawset, ParentForm != null ? ParentForm.Handle : IntPtr.Zero); };
				}
				{// Background colour
					ToolStripMenuItem bk_colour_menu = new ToolStripMenuItem("Background Colour");
					rdr_menu.DropDownItems.Add(bk_colour_menu);
					{
						ToolStripButton option = new ToolStripButton(" ");
						bk_colour_menu.DropDownItems.Add(option);
						option.AutoToolTip = false;
						option.BackColor = Color.FromArgb(View3D_BackGroundColour(m_drawset));
						option.Click += delegate { ColorDialog cd = new ColorDialog(); if (cd.ShowDialog() == DialogResult.OK) option.BackColor = cd.Color; };
						option.BackColorChanged += delegate { View3D_SetBackgroundColour(m_drawset, option.BackColor.ToArgb()); Refresh(); };
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
			if (m_drawset == HDrawset.Zero) return;
			View3D_Resize(Width-2, Height-2);
			Refresh();
		}

		/// <summary>Absorb PaintBackground events</summary>
		protected override void OnPaintBackground(PaintEventArgs e)
		{
			if (m_drawset == HDrawset.Zero) base.OnPaintBackground(e);
		}

		/// <summary>Paint the control</summary>
		protected override void OnPaint(PaintEventArgs e)
		{
			// Render the control
			if (m_drawset == HDrawset.Zero) base.OnPaint(e);
			else View3D_Render(m_drawset);
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

		/// <summary>Required method for Designer support - do not modify the contents of this method with the code editor.</summary>
		private void InitializeComponent()
		{
			components = new Container();
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
				EResult res = View3D_TextureLoadSurface(m_handle, level, tex_filepath, new Rectangle[]{dst_rect}, new Rectangle[]{src_rect}, (uint)filter, colour_key);
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

		/// <summary>Light source properties</summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct View3DLight
		{
			public EView3DLight m_type;
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
					m_type           = EView3DLight.Directional,
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
	}
}
