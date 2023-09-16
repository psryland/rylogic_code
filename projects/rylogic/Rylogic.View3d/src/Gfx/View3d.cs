//#define PR_VIEW3D_CREATE_STACKTRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows.Threading;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Interop.Win32;
using Rylogic.Maths;
using Rylogic.Script;
using Rylogic.Utility;
using HContext = System.IntPtr;
using HCubeMap = System.IntPtr;
using HGizmo = System.IntPtr;
using HMODULE = System.IntPtr;
using HObject = System.IntPtr;
using HTexture = System.IntPtr;
using HWindow = System.IntPtr;
using HWND = System.IntPtr;

namespace Rylogic.Gfx
{
	/// <summary>.NET wrapper for View3D.dll</summary>
	public sealed partial class View3d :IDisposable
	{
		// Notes:
		// - Each process should create a single View3d instance that is an isolated context.
		// - Ldr objects are created and owned by the context.

		#region Enumerations
		public enum EResult
		{
			Success,
			Failed,
			InvalidValue,
		}
		public enum ELogLevel
		{
			Debug,
			Info,
			Warn,
			Error,
		}
		[Flags] public enum EGeom
		{
			Unknown = 0,
			Vert = 1 << 0,
			Colr = 1 << 1,
			Norm = 1 << 2,
			Tex0 = 1 << 3,
		}
		public enum ETopo :uint
		{
			Undefined = 0,
			PointList,
			LineList,
			LineStrip,
			TriList,
			TriStrip,
			LineListAdj,
			LineStripAdj,
			TriListAdj,
			TriStripAdj,
		}
		[Flags] public enum ENuggetFlag :uint
		{
			None = 0,

			/// <summary>Exclude this nugget when rendering a model</summary>
			Hidden = 1 << 0,

			/// <summary>Set if the geometry data for the nugget contains alpha colours</summary>
			GeometryHasAlpha = 1 << 1,

			/// <summary>Set if the tint colour contains alpha</summary>
			TintHasAlpha = 1 << 2,

			/// <summary>Excluded from shadow map render steps</summary>
			ShadowCastExclude = 1 << 3,
		}
		public enum EShaderVS
		{
			Standard = 0,
		}
		public enum EShaderPS
		{
			Standard = 0,
		}
		public enum EShaderGS
		{
			Standard = 0,

			// Point sprite params: *PointSize {w,h} *Depth {true|false}
			PointSpritesGS,

			// Thick line params: *LineWidth {width}
			ThickLineListGS,

			// Thick line params: *LineWidth {width}
			ThickLineStripGS,

			// Arrow params: *Size {size}
			ArrowHeadGS,
		}
		public enum EShaderCS
		{
			None = 0,
		}
		public enum ERenderStep :int
		{
			Invalid = 0,
			ForwardRender,
			GBuffer,
			DSLighting,
			ShadowMap,
			RayCast,
			_number_of,
		};
		public enum EStockTexture :int
		{
			Invalid = 0,
			Black,
			White,
			Gray,
			Checker,
			Checker2,
			Checker3,
			WhiteSpot,
			WhiteTriangle,
		}
		[Flags] public enum ECreateDeviceFlags :uint
		{
			D3D11_CREATE_DEVICE_SINGLETHREADED = 0x1,
			D3D11_CREATE_DEVICE_DEBUG = 0x2,
			D3D11_CREATE_DEVICE_SWITCH_TO_REF = 0x4,
			D3D11_CREATE_DEVICE_PREVENT_INTERNAL_THREADING_OPTIMIZATIONS = 0x8,
			D3D11_CREATE_DEVICE_BGRA_SUPPORT = 0x20,
			D3D11_CREATE_DEVICE_DEBUGGABLE = 0x40,
			D3D11_CREATE_DEVICE_PREVENT_ALTERING_LAYER_SETTINGS_FROM_REGISTRY = 0x80,
			D3D11_CREATE_DEVICE_DISABLE_GPU_TIMEOUT = 0x100,
			D3D11_CREATE_DEVICE_VIDEO_SUPPORT = 0x800
		}
		public enum EFormat :uint
		{
			DXGI_FORMAT_UNKNOWN = 0,
			DXGI_FORMAT_R32G32B32A32_TYPELESS = 1,
			DXGI_FORMAT_R32G32B32A32_FLOAT = 2,
			DXGI_FORMAT_R32G32B32A32_UINT = 3,
			DXGI_FORMAT_R32G32B32A32_SINT = 4,
			DXGI_FORMAT_R32G32B32_TYPELESS = 5,
			DXGI_FORMAT_R32G32B32_FLOAT = 6,
			DXGI_FORMAT_R32G32B32_UINT = 7,
			DXGI_FORMAT_R32G32B32_SINT = 8,
			DXGI_FORMAT_R16G16B16A16_TYPELESS = 9,
			DXGI_FORMAT_R16G16B16A16_FLOAT = 10,
			DXGI_FORMAT_R16G16B16A16_UNORM = 11,
			DXGI_FORMAT_R16G16B16A16_UINT = 12,
			DXGI_FORMAT_R16G16B16A16_SNORM = 13,
			DXGI_FORMAT_R16G16B16A16_SINT = 14,
			DXGI_FORMAT_R32G32_TYPELESS = 15,
			DXGI_FORMAT_R32G32_FLOAT = 16,
			DXGI_FORMAT_R32G32_UINT = 17,
			DXGI_FORMAT_R32G32_SINT = 18,
			DXGI_FORMAT_R32G8X24_TYPELESS = 19,
			DXGI_FORMAT_D32_FLOAT_S8X24_UINT = 20,
			DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS = 21,
			DXGI_FORMAT_X32_TYPELESS_G8X24_UINT = 22,
			DXGI_FORMAT_R10G10B10A2_TYPELESS = 23,
			DXGI_FORMAT_R10G10B10A2_UNORM = 24,
			DXGI_FORMAT_R10G10B10A2_UINT = 25,
			DXGI_FORMAT_R11G11B10_FLOAT = 26,
			DXGI_FORMAT_R8G8B8A8_TYPELESS = 27,
			DXGI_FORMAT_R8G8B8A8_UNORM = 28,
			DXGI_FORMAT_R8G8B8A8_UNORM_SRGB = 29,
			DXGI_FORMAT_R8G8B8A8_UINT = 30,
			DXGI_FORMAT_R8G8B8A8_SNORM = 31,
			DXGI_FORMAT_R8G8B8A8_SINT = 32,
			DXGI_FORMAT_R16G16_TYPELESS = 33,
			DXGI_FORMAT_R16G16_FLOAT = 34,
			DXGI_FORMAT_R16G16_UNORM = 35,
			DXGI_FORMAT_R16G16_UINT = 36,
			DXGI_FORMAT_R16G16_SNORM = 37,
			DXGI_FORMAT_R16G16_SINT = 38,
			DXGI_FORMAT_R32_TYPELESS = 39,
			DXGI_FORMAT_D32_FLOAT = 40,
			DXGI_FORMAT_R32_FLOAT = 41,
			DXGI_FORMAT_R32_UINT = 42,
			DXGI_FORMAT_R32_SINT = 43,
			DXGI_FORMAT_R24G8_TYPELESS = 44,
			DXGI_FORMAT_D24_UNORM_S8_UINT = 45,
			DXGI_FORMAT_R24_UNORM_X8_TYPELESS = 46,
			DXGI_FORMAT_X24_TYPELESS_G8_UINT = 47,
			DXGI_FORMAT_R8G8_TYPELESS = 48,
			DXGI_FORMAT_R8G8_UNORM = 49,
			DXGI_FORMAT_R8G8_UINT = 50,
			DXGI_FORMAT_R8G8_SNORM = 51,
			DXGI_FORMAT_R8G8_SINT = 52,
			DXGI_FORMAT_R16_TYPELESS = 53,
			DXGI_FORMAT_R16_FLOAT = 54,
			DXGI_FORMAT_D16_UNORM = 55,
			DXGI_FORMAT_R16_UNORM = 56,
			DXGI_FORMAT_R16_UINT = 57,
			DXGI_FORMAT_R16_SNORM = 58,
			DXGI_FORMAT_R16_SINT = 59,
			DXGI_FORMAT_R8_TYPELESS = 60,
			DXGI_FORMAT_R8_UNORM = 61,
			DXGI_FORMAT_R8_UINT = 62,
			DXGI_FORMAT_R8_SNORM = 63,
			DXGI_FORMAT_R8_SINT = 64,
			DXGI_FORMAT_A8_UNORM = 65,
			DXGI_FORMAT_R1_UNORM = 66,
			DXGI_FORMAT_R9G9B9E5_SHAREDEXP = 67,
			DXGI_FORMAT_R8G8_B8G8_UNORM = 68,
			DXGI_FORMAT_G8R8_G8B8_UNORM = 69,
			DXGI_FORMAT_BC1_TYPELESS = 70,
			DXGI_FORMAT_BC1_UNORM = 71,
			DXGI_FORMAT_BC1_UNORM_SRGB = 72,
			DXGI_FORMAT_BC2_TYPELESS = 73,
			DXGI_FORMAT_BC2_UNORM = 74,
			DXGI_FORMAT_BC2_UNORM_SRGB = 75,
			DXGI_FORMAT_BC3_TYPELESS = 76,
			DXGI_FORMAT_BC3_UNORM = 77,
			DXGI_FORMAT_BC3_UNORM_SRGB = 78,
			DXGI_FORMAT_BC4_TYPELESS = 79,
			DXGI_FORMAT_BC4_UNORM = 80,
			DXGI_FORMAT_BC4_SNORM = 81,
			DXGI_FORMAT_BC5_TYPELESS = 82,
			DXGI_FORMAT_BC5_UNORM = 83,
			DXGI_FORMAT_BC5_SNORM = 84,
			DXGI_FORMAT_B5G6R5_UNORM = 85,
			DXGI_FORMAT_B5G5R5A1_UNORM = 86,
			DXGI_FORMAT_B8G8R8A8_UNORM = 87,
			DXGI_FORMAT_B8G8R8X8_UNORM = 88,
			DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM = 89,
			DXGI_FORMAT_B8G8R8A8_TYPELESS = 90,
			DXGI_FORMAT_B8G8R8A8_UNORM_SRGB = 91,
			DXGI_FORMAT_B8G8R8X8_TYPELESS = 92,
			DXGI_FORMAT_B8G8R8X8_UNORM_SRGB = 93,
			DXGI_FORMAT_BC6H_TYPELESS = 94,
			DXGI_FORMAT_BC6H_UF16 = 95,
			DXGI_FORMAT_BC6H_SF16 = 96,
			DXGI_FORMAT_BC7_TYPELESS = 97,
			DXGI_FORMAT_BC7_UNORM = 98,
			DXGI_FORMAT_BC7_UNORM_SRGB = 99,
			DXGI_FORMAT_AYUV = 100,
			DXGI_FORMAT_Y410 = 101,
			DXGI_FORMAT_Y416 = 102,
			DXGI_FORMAT_NV12 = 103,
			DXGI_FORMAT_P010 = 104,
			DXGI_FORMAT_P016 = 105,
			DXGI_FORMAT_420_OPAQUE = 106,
			DXGI_FORMAT_YUY2 = 107,
			DXGI_FORMAT_Y210 = 108,
			DXGI_FORMAT_Y216 = 109,
			DXGI_FORMAT_NV11 = 110,
			DXGI_FORMAT_AI44 = 111,
			DXGI_FORMAT_IA44 = 112,
			DXGI_FORMAT_P8 = 113,
			DXGI_FORMAT_A8P8 = 114,
			DXGI_FORMAT_B4G4R4A4_UNORM = 115,
			DXGI_FORMAT_FORCE_UINT = 0xffffffff
		}
		public enum EFilter :uint //D3D11_FILTER
		{
			D3D11_FILTER_MIN_MAG_MIP_POINT = 0,
			D3D11_FILTER_MIN_MAG_POINT_MIP_LINEAR = 0x1,
			D3D11_FILTER_MIN_POINT_MAG_LINEAR_MIP_POINT = 0x4,
			D3D11_FILTER_MIN_POINT_MAG_MIP_LINEAR = 0x5,
			D3D11_FILTER_MIN_LINEAR_MAG_MIP_POINT = 0x10,
			D3D11_FILTER_MIN_LINEAR_MAG_POINT_MIP_LINEAR = 0x11,
			D3D11_FILTER_MIN_MAG_LINEAR_MIP_POINT = 0x14,
			D3D11_FILTER_MIN_MAG_MIP_LINEAR = 0x15,
			D3D11_FILTER_ANISOTROPIC = 0x55,
			D3D11_FILTER_COMPARISON_MIN_MAG_MIP_POINT = 0x80,
			D3D11_FILTER_COMPARISON_MIN_MAG_POINT_MIP_LINEAR = 0x81,
			D3D11_FILTER_COMPARISON_MIN_POINT_MAG_LINEAR_MIP_POINT = 0x84,
			D3D11_FILTER_COMPARISON_MIN_POINT_MAG_MIP_LINEAR = 0x85,
			D3D11_FILTER_COMPARISON_MIN_LINEAR_MAG_MIP_POINT = 0x90,
			D3D11_FILTER_COMPARISON_MIN_LINEAR_MAG_POINT_MIP_LINEAR = 0x91,
			D3D11_FILTER_COMPARISON_MIN_MAG_LINEAR_MIP_POINT = 0x94,
			D3D11_FILTER_COMPARISON_MIN_MAG_MIP_LINEAR = 0x95,
			D3D11_FILTER_COMPARISON_ANISOTROPIC = 0xd5,
			D3D11_FILTER_MINIMUM_MIN_MAG_MIP_POINT = 0x100,
			D3D11_FILTER_MINIMUM_MIN_MAG_POINT_MIP_LINEAR = 0x101,
			D3D11_FILTER_MINIMUM_MIN_POINT_MAG_LINEAR_MIP_POINT = 0x104,
			D3D11_FILTER_MINIMUM_MIN_POINT_MAG_MIP_LINEAR = 0x105,
			D3D11_FILTER_MINIMUM_MIN_LINEAR_MAG_MIP_POINT = 0x110,
			D3D11_FILTER_MINIMUM_MIN_LINEAR_MAG_POINT_MIP_LINEAR = 0x111,
			D3D11_FILTER_MINIMUM_MIN_MAG_LINEAR_MIP_POINT = 0x114,
			D3D11_FILTER_MINIMUM_MIN_MAG_MIP_LINEAR = 0x115,
			D3D11_FILTER_MINIMUM_ANISOTROPIC = 0x155,
			D3D11_FILTER_MAXIMUM_MIN_MAG_MIP_POINT = 0x180,
			D3D11_FILTER_MAXIMUM_MIN_MAG_POINT_MIP_LINEAR = 0x181,
			D3D11_FILTER_MAXIMUM_MIN_POINT_MAG_LINEAR_MIP_POINT = 0x184,
			D3D11_FILTER_MAXIMUM_MIN_POINT_MAG_MIP_LINEAR = 0x185,
			D3D11_FILTER_MAXIMUM_MIN_LINEAR_MAG_MIP_POINT = 0x190,
			D3D11_FILTER_MAXIMUM_MIN_LINEAR_MAG_POINT_MIP_LINEAR = 0x191,
			D3D11_FILTER_MAXIMUM_MIN_MAG_LINEAR_MIP_POINT = 0x194,
			D3D11_FILTER_MAXIMUM_MIN_MAG_MIP_LINEAR = 0x195,
			D3D11_FILTER_MAXIMUM_ANISOTROPIC = 0x1d5
		}
		public enum EAddrMode :uint //D3D11_TEXTURE_ADDRESS_MODE
		{
			D3D11_TEXTURE_ADDRESS_WRAP = 1,
			D3D11_TEXTURE_ADDRESS_MIRROR = 2,
			D3D11_TEXTURE_ADDRESS_CLAMP = 3,
			D3D11_TEXTURE_ADDRESS_BORDER = 4,
			D3D11_TEXTURE_ADDRESS_MIRROR_ONCE = 5,
		}
		[Flags] public enum EBindFlags :uint //D3D11_BIND_FLAG
		{
			NONE = 0,
			D3D11_BIND_VERTEX_BUFFER = 0x1,
			D3D11_BIND_INDEX_BUFFER = 0x2,
			D3D11_BIND_CONSTANT_BUFFER = 0x4,
			D3D11_BIND_SHADER_RESOURCE = 0x8,
			D3D11_BIND_STREAM_OUTPUT = 0x10,
			D3D11_BIND_RENDER_TARGET = 0x20,
			D3D11_BIND_DEPTH_STENCIL = 0x40,
			D3D11_BIND_UNORDERED_ACCESS = 0x80,
			D3D11_BIND_DECODER = 0x200,
			D3D11_BIND_VIDEO_ENCODER = 0x400
		}
		[Flags] public enum EResMiscFlags :uint//D3D11_RESOURCE_MISC_FLAG
		{
			NONE = 0,
			D3D11_RESOURCE_MISC_GENERATE_MIPS = 0x1,
			D3D11_RESOURCE_MISC_SHARED = 0x2,
			D3D11_RESOURCE_MISC_TEXTURECUBE = 0x4,
			D3D11_RESOURCE_MISC_DRAWINDIRECT_ARGS = 0x10,
			D3D11_RESOURCE_MISC_BUFFER_ALLOW_RAW_VIEWS = 0x20,
			D3D11_RESOURCE_MISC_BUFFER_STRUCTURED = 0x40,
			D3D11_RESOURCE_MISC_RESOURCE_CLAMP = 0x80,
			D3D11_RESOURCE_MISC_SHARED_KEYEDMUTEX = 0x100,
			D3D11_RESOURCE_MISC_GDI_COMPATIBLE = 0x200,
			D3D11_RESOURCE_MISC_SHARED_NTHANDLE = 0x800,
			D3D11_RESOURCE_MISC_RESTRICTED_CONTENT = 0x1000,
			D3D11_RESOURCE_MISC_RESTRICT_SHARED_RESOURCE = 0x2000,
			D3D11_RESOURCE_MISC_RESTRICT_SHARED_RESOURCE_DRIVER = 0x4000,
			D3D11_RESOURCE_MISC_GUARDED = 0x8000,
			D3D11_RESOURCE_MISC_TILE_POOL = 0x20000,
			D3D11_RESOURCE_MISC_TILED = 0x40000
		}
		public enum ELight
		{
			Ambient,
			Directional,
			Point,
			Spot
		}
		public enum EAnimCommand
		{
			Reset, // Reset the the 'time' value
			Play,  // Run continuously using 'time' as the step size, or real time if 'time' == 0
			Stop,  // Stop at the current time.
			Step,  // Step by 'time' (can be positive or negative)
		}
		public enum EFillMode
		{
			Default = 0,
			Points = 1,
			Wireframe = 2, // D3D11_FILL_WIREFRAME
			Solid = 3, // D3D11_FILL_SOLID
			SolidWire = 4,
		}
		public enum ECullMode
		{
			Default = 0,
			None = 1, // D3D11_CULL_NONE
			Front = 2, // D3D11_CULL_FRONT
			Back = 3, // D3D11_CULL_BACK
		}
		[Flags] public enum ENavOp
		{
			None = 0,
			Translate = 1 << 0,
			Rotate = 1 << 1,
			Zoom = 1 << 2,
		}
		public enum EColourOp
		{
			Overwrite,
			Add,
			Subtract,
			Multiply,
			Lerp,
		}
		[Flags] public enum ECameraLockMask
		{
			None = 0,
			TransX = 1 << 0,
			TransY = 1 << 1,
			TransZ = 1 << 2,
			RotX = 1 << 3,
			RotY = 1 << 4,
			RotZ = 1 << 5,
			Zoom = 1 << 6,
			CameraRelative = 1 << 7,
			Translation = TransX | TransY | TransZ,
			Rotation = RotX | RotY | RotZ,
			All = (1 << 7) - 1, // Not including camera relative
		}
		[Flags] public enum EUpdateObject :uint // Flags for partial update of a model
		{
			None = 0U,
			Name = 1 << 0,
			Model = 1 << 1,
			Transform = 1 << 2,
			Children = 1 << 3,
			Colour = 1 << 4,
			ColourMask = 1 << 5,
			Flags = 1 << 6,
			Animation = 1 << 7,
			All = Name | Model | Transform | Children | Colour | ColourMask | Flags | Animation,
		}
		[Flags] public enum EFlags // sync with 'EView3DFlags'
		{
			None = 0,

			/// <summary>The object is hidden</summary>
			Hidden = 1 << 0,

			/// <summary>The object is filled in wireframe mode</summary>
			Wireframe = 1 << 1,

			/// <summary>Render the object without testing against the depth buffer</summary>
			NoZTest = 1 << 2,

			/// <summary>Render the object without effecting the depth buffer</summary>
			NoZWrite = 1 << 3,

			/// <summary>The object has normals shown</summary>
			Normals = 1 << 4,

			/// <summary>The object to world transform is not an affine transform</summary>
			NonAffine = 1 << 5,

			/// <summary>Set when an object is selected. The meaning of 'selected' is up to the application</summary>
			Selected = 1 << 8,

			/// <summary>Doesn't contribute to the bounding box on an object.</summary>
			BBoxExclude = 1 << 9,

			/// <summary>Should not be included when determining the bounds of a scene.</summary>
			SceneBoundsExclude = 1 << 10,

			/// <summary>Ignored for hit test ray casts</summary>
			HitTestExclude = 1 << 11,

			/// <summary>Doesn't cast a shadow</summary>
			ShadowCastExclude = 1 << 12,
		}
		public enum ESortGroup :int
		{
			Min = 0,               // The minimum sort group value
			PreOpaques = 63,       // 
			Default = 64,          // Make opaques the middle group
			Skybox,                // Sky-box after opaques
			PostOpaques,           // 
			PreAlpha = Default + 16, // Last group before the alpha groups
			AlphaBack,             // 
			AlphaFront,            // 
			PostAlpha,             // First group after the alpha groups
			Max = 127,             // The maximum sort group value
		}
		public enum ESceneBounds
		{
			All,
			Selected,
			Visible,
		}
		public enum ESourcesChangedReason
		{
			NewData,
			Reload,
		}
		public enum ESceneChanged
		{
			ObjectsAdded,
			ObjectsRemoved,
			GizmoAdded,
			GizmoRemoved,
		}
		[Flags] public enum EHitTestFlags
		{
			Faces = 1 << 0,
			Edges = 1 << 1,
			Verts = 1 << 2,
		}
		public enum ESnapType
		{
			NoSnap,
			Vert,
			Edge,
			Face,
			EdgeCentre,
			FaceCentre,
		}
		[Flags] public enum ESettings :int
		{
			// Upper 2-bytes = category
			// Lower 2-bytes = specific property that changed.
			None = 0,

			General = 1 << 16,
			General_FocusPointVisible = General | 1 << 0,
			General_OriginPointVisible = General | 1 << 1,
			General_SelectionBoxVisible = General | 1 << 2,

			Scene = 1 << 17,
			Scene_BackgroundColour = Scene | 1 << 0,
			Scene_Multisampling = Scene | 1 << 1,
			Scene_FilllMode = Scene | 1 << 2,
			Scene_CullMode = Scene | 1 << 3,
			Scene_Viewport = Scene | 1 << 4,

			Camera = 1 << 18,
			Camera_Position = Camera | 1 << 0,
			Camera_FocusDist = Camera | 1 << 1,
			Camera_Orthographic = Camera | 1 << 2,
			Camera_Aspect = Camera | 1 << 3,
			Camera_Fov = Camera | 1 << 4,
			Camera_ClipPlanes = Camera | 1 << 5,
			Camera_LockMask = Camera | 1 << 6,
			Camera_AlignAxis = Camera | 1 << 7,

			Lighting = 1 << 19,
			Lighting_All = Lighting | 1 << 0,

			Diagnostics = 1 << 20,
			Diagnostics_BBoxesVisible = Diagnostics | 1 << 0,
			Diagnostics_NormalsLength = Diagnostics | 1 << 1,
			Diagnostics_NormalsColour = Diagnostics | 1 << 2,
			Diagnostics_FillModePointsSize = Diagnostics | 1 << 3,
		}
		#endregion

		#region Structs

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct Vertex
		{
			public v4 m_pos;
			public v4 m_norm;
			public v2 m_uv;
			public uint m_col;
			public uint pad;

			public Vertex(v4 vert) { m_pos = vert; m_col = ~0U; m_norm = v4.Zero; m_uv = v2.Zero; pad = 0; }
			public Vertex(v4 vert, uint col) { m_pos = vert; m_col = col; m_norm = v4.Zero; m_uv = v2.Zero; pad = 0; }
			public Vertex(v4 vert, v4 norm, uint col, v2 tex) { m_pos = vert; m_col = col; m_norm = norm; m_uv = tex; pad = 0; }

			public override readonly string ToString() { return $"V:<{m_pos}> C:<{m_col:X8}>"; }
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct Material
		{
			[DebuggerDisplay("{Description,nq}"), StructLayout(LayoutKind.Sequential)]
			public struct ShaderSet
			{
				[StructLayout(LayoutKind.Sequential)]
				public struct ShaderVS { public EShaderVS shdr;[MarshalAs(UnmanagedType.LPStr)] public string parms; }
				[StructLayout(LayoutKind.Sequential)]
				public struct ShaderGS { public EShaderGS shdr;[MarshalAs(UnmanagedType.LPStr)] public string parms; }
				[StructLayout(LayoutKind.Sequential)]
				public struct ShaderPS { public EShaderPS shdr;[MarshalAs(UnmanagedType.LPStr)] public string parms; }
				[StructLayout(LayoutKind.Sequential)]
				public struct ShaderCS { public EShaderCS shdr;[MarshalAs(UnmanagedType.LPStr)] public string parms; }

				public ShaderVS m_vs;
				public ShaderGS m_gs;
				public ShaderPS m_ps;
				public ShaderCS m_cs;

				/// <summary>Description</summary>
				public string Description => $"VS={m_vs.shdr} GS={m_gs.shdr} PS={m_ps.shdr} CS={m_cs.shdr}";
			}

			[DebuggerDisplay("Smap"), StructLayout(LayoutKind.Sequential)]
			public struct ShaderMap
			{
				public static ShaderMap New()
				{
					return new ShaderMap { m_rstep = Array_.New((int)ERenderStep._number_of, i => new ShaderSet()) };
				}

				/// <summary>Get the shader set for the given render step</summary>
				[MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)ERenderStep._number_of)]
				public ShaderSet[] m_rstep;
			}

			public static Material New()
			{
				return new Material(null);
			}
			public Material(Colour32? colour = null, HTexture? diff_tex = null, ShaderMap? shdr_map = null, float? relative_reflectivity = null)
			{
				m_diff_tex = diff_tex ?? HTexture.Zero;
				m_shader_map = shdr_map ?? ShaderMap.New();
				m_tint = colour ?? Colour32.White;
				m_relative_reflectivity = relative_reflectivity ?? 1f;
			}

			/// <summary>Material diffuse texture</summary>
			public HTexture m_diff_tex;

			/// <summary>Shader overrides</summary>
			public ShaderMap m_shader_map;

			/// <summary>Tint colour</summary>
			public Colour32 m_tint;

			/// <summary>How reflective this nugget is relative to the over all model</summary>
			public float m_relative_reflectivity;

			/// <summary>Set the shader to use along with the parameters it requires</summary>
			public void Use(ERenderStep rstep, EShaderVS shdr, string parms)
			{
				m_shader_map.m_rstep[(int)rstep].m_vs.shdr = shdr;
				m_shader_map.m_rstep[(int)rstep].m_vs.parms = parms;
			}
			public void Use(ERenderStep rstep, EShaderGS shdr, string parms)
			{
				m_shader_map.m_rstep[(int)rstep].m_gs.shdr = shdr;
				m_shader_map.m_rstep[(int)rstep].m_gs.parms = parms;
			}
			public void Use(ERenderStep rstep, EShaderPS shdr, string parms)
			{
				m_shader_map.m_rstep[(int)rstep].m_ps.shdr = shdr;
				m_shader_map.m_rstep[(int)rstep].m_ps.parms = parms;
			}
			public void Use(ERenderStep rstep, EShaderCS shdr, string parms)
			{
				m_shader_map.m_rstep[(int)rstep].m_cs.shdr = shdr;
				m_shader_map.m_rstep[(int)rstep].m_cs.parms = parms;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct Nugget
		{
			public ETopo m_topo;
			public EGeom m_geom;
			public ECullMode m_cull_mode;
			public EFillMode m_fill_mode;
			public uint m_v0, m_v1;       // Vertex buffer range. Set to 0,0 to mean the whole buffer
			public uint m_i0, m_i1;       // Index buffer range. Set to 0,0 to mean the whole buffer
			public ENuggetFlag m_nflags;   // Nugget flags (ENuggetFlag)
			public bool m_range_overlaps; // True if the nugget V/I range overlaps earlier nuggets
			public Material m_mat;

			public Nugget(ETopo topo, EGeom geom, uint v0 = 0, uint v1 = 0, uint i0 = 0, uint i1 = 0, ENuggetFlag flags = ENuggetFlag.None, bool range_overlaps = false, Material? mat = null, ECullMode cull_mode = ECullMode.Back, EFillMode fill_mode = EFillMode.Solid)
			{
				Debug.Assert(mat == null || mat.Value.m_shader_map.m_rstep != null, "Don't use default(Material)");
				m_topo = topo;
				m_geom = geom;
				m_cull_mode = cull_mode;
				m_fill_mode = fill_mode;
				m_v0 = v0;
				m_v1 = v1;
				m_i0 = i0;
				m_i1 = i1;
				m_nflags = flags;
				m_range_overlaps = range_overlaps;
				m_mat = mat ?? new Material(null);
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public readonly struct ImageInfo
		{
			public readonly uint m_width;
			public readonly uint m_height;
			public readonly uint m_depth;
			public readonly uint m_mips;
			public readonly EFormat m_format; //DXGI_FORMAT
			public readonly uint m_image_file_format;//D3DXIMAGE_FILEFORMAT
			public readonly float m_aspect => (float)m_width / m_height;
		}

		public class TextureOptions
		{
			[StructLayout(LayoutKind.Sequential)]
			public struct InteropData
			{
				public m4x4 T2S;
				public EFormat Format;
				public uint Mips;
				public EFilter Filter;
				public EAddrMode AddrU;
				public EAddrMode AddrV;
				public EBindFlags BindFlags;
				public EResMiscFlags MiscFlags;
				public uint MultiSamp;
				public uint ColourKey;
				public bool HasAlpha;
				public bool GdiCompatible;
				public string DbgName;
			}
			public InteropData Data;

			public TextureOptions(
				m4x4? t2s = null,
				EFormat format = EFormat.DXGI_FORMAT_R8G8B8A8_UNORM,
				EFilter filter = EFilter.D3D11_FILTER_MIN_MAG_MIP_LINEAR,
				EAddrMode addrU = EAddrMode.D3D11_TEXTURE_ADDRESS_CLAMP,
				EAddrMode addrV = EAddrMode.D3D11_TEXTURE_ADDRESS_CLAMP,
				EBindFlags bind_flags = EBindFlags.D3D11_BIND_SHADER_RESOURCE,
				EResMiscFlags misc_flags = EResMiscFlags.NONE,
				uint msaa = 1U,
				uint mips = 0U,
				uint colour_key = 0U,
				bool has_alpha = false,
				string? dbg_name = null)
			{
				Data = new InteropData
				{
					T2S = t2s ?? m4x4.Identity,
					Format = format,
					Mips = mips,
					Filter = filter,
					AddrU = addrU,
					AddrV = addrV,
					MultiSamp = msaa,
					BindFlags = bind_flags,
					MiscFlags = misc_flags,
					ColourKey = colour_key,
					HasAlpha = has_alpha,
					DbgName = dbg_name ?? string.Empty,
					GdiCompatible = false,
				};
			}

			public m4x4 T2S
			{
				get => Data.T2S;
				set => Data.T2S = value;
			}
			public EFormat Format
			{
				get => Data.Format;
				set => Data.Format = value;
			}
			public uint Mips
			{
				get => Data.Mips;
				set => Data.Mips = value;
			}
			public EFilter Filter
			{
				get => Data.Filter;
				set => Data.Filter = value;
			}
			public EAddrMode AddrU
			{
				get => Data.AddrU;
				set => Data.AddrU = value;
			}
			public EAddrMode AddrV
			{
				get => Data.AddrV;
				set => Data.AddrV = value;
			}
			public EBindFlags BindFlags
			{
				get => Data.BindFlags;
				set => Data.BindFlags = value;
			}
			public EResMiscFlags MiscFlags
			{
				get => Data.MiscFlags;
				set => Data.MiscFlags = value;
			}
			public uint MultiSamp
			{
				get => Data.MultiSamp;
				set => Data.MultiSamp = Math.Max(1U, value);
			}
			public uint ColourKey
			{
				get => Data.ColourKey;
				set => Data.ColourKey = value;
			}
			public bool HasAlpha
			{
				get => Data.HasAlpha;
				set => Data.HasAlpha = value;
			}
			public bool GdiCompatible
			{
				get => (Data.MiscFlags & EResMiscFlags.D3D11_RESOURCE_MISC_GDI_COMPATIBLE) != 0;
				set
				{
					if (value)
					{
						// Don't make GDI textures automatically 'HasAlpha'.
						// Leave that decision to the caller.
						Data.Format = EFormat.DXGI_FORMAT_B8G8R8A8_UNORM;
						Data.BindFlags |= EBindFlags.D3D11_BIND_SHADER_RESOURCE | EBindFlags.D3D11_BIND_RENDER_TARGET;
						Data.MiscFlags |= EResMiscFlags.D3D11_RESOURCE_MISC_GDI_COMPATIBLE;
						Data.MultiSamp = 1U;
						Data.Mips = 1U;
					}
					else
					{
						Data.MiscFlags &= ~EResMiscFlags.D3D11_RESOURCE_MISC_GDI_COMPATIBLE;
					}
				}
			}
			public string DbgName
			{
				get => Data.DbgName;
				set => Data.DbgName = value ?? string.Empty;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct WindowOptions
		{
			public ReportErrorCB ErrorCB;
			public IntPtr ErrorCBCtx;
			public bool GdiCompatibleBackBuffer;
			public int Multisampling;
			public string DbgName;
			public WindowOptions(ReportErrorCB error_cb, IntPtr error_cb_ctx, bool gdi_compatible_bb = false)
			{
				ErrorCB = error_cb;
				ErrorCBCtx = error_cb_ctx;
				GdiCompatibleBackBuffer = gdi_compatible_bb;
				Multisampling = gdi_compatible_bb ? 1 : 4;
				DbgName = string.Empty;
			}
		}

		/// <summary>Light source properties</summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct LightInfo
		{
			/// <summary>Position, only valid for point and spot lights</summary>
			public v4 Position;

			/// <summary>Direction, only valid for directional and spot lights</summary>
			public v4 Direction;

			/// <summary>Tbe light source type. One of ambient, directional, point, spot</summary>
			public ELight Type;

			/// <summary>Ambient light colour</summary>
			public Colour32 AmbientColour;

			/// <summary>Main light colour</summary>
			public Colour32 DiffuseColour;

			/// <summary>Specular light colour</summary>
			public Colour32 SpecularColour;

			/// <summary>Specular power (controls specular spot size)</summary>
			public float SpecularPower;

			/// <summary>Light range</summary>
			public float Range;

			/// <summary>Intensity falloff per unit distance</summary>
			public float Falloff;

			/// <summary>Spot light inner angle 100% light (in radians)</summary>
			public float InnerAngle;

			/// <summary>Spot light outer angle 0% light (in radians)</summary>
			public float OuterAngle;

			/// <summary>Shadow cast range, 0 for off</summary>
			public float CastShadow;

			/// <summary>True if the light should move with the camera</summary>
			public bool CameraRelative;

			/// <summary>True if this light is on</summary>
			public bool On;

			/// <summary>Default light info</summary>
			public static LightInfo Default()
			{
				return new LightInfo
				{
					On = true,
					Type = ELight.Ambient,
					Position = v4.Origin,
					Direction = -v4.ZAxis,
					AmbientColour = 0xFF404040,
					DiffuseColour = 0xFF404040,
					SpecularColour = 0xFF808080,
					SpecularPower = 1000f,
					InnerAngle = Math_.TauBy4F,
					OuterAngle = Math_.TauBy4F,
					Range = 1000f,
					Falloff = 0f,
					CastShadow = 0f,
					CameraRelative = false,
				};
			}

			/// <summary>Return properties for an ambient light source</summary>
			public static LightInfo Ambient(Colour32? ambient = null)
			{
				var light = Default();
				light.Type = ELight.Ambient;
				light.AmbientColour = ambient ?? light.AmbientColour;
				return light;
			}

			/// <summary>Return properties for a directional light source</summary>
			public static LightInfo Directional(v4 direction, Colour32? ambient = null, Colour32? diffuse = null, Colour32? specular = null, float? spec_power = null, float? cast_shadow = null, bool camera_relative = false)
			{
				var light = Default();
				light.Type = ELight.Directional;
				light.Direction = Math_.Normalise(direction);
				light.AmbientColour = ambient ?? light.AmbientColour;
				light.DiffuseColour = diffuse ?? light.DiffuseColour;
				light.SpecularColour = specular ?? light.SpecularColour;
				light.SpecularPower = spec_power ?? light.SpecularPower;
				light.CastShadow = cast_shadow ?? light.CastShadow;
				light.CameraRelative = camera_relative;
				return light;
			}

			/// <summary>Return properties for a point light source</summary>
			public static LightInfo Point(v4 position, Colour32? ambient = null, Colour32? diffuse = null, Colour32? specular = null, float? spec_power = null, float? cast_shadow = null, bool camera_relative = false)
			{
				var light = Default();
				light.Type = ELight.Point;
				light.Position = position;
				light.AmbientColour = ambient ?? light.AmbientColour;
				light.DiffuseColour = diffuse ?? light.DiffuseColour;
				light.SpecularColour = specular ?? light.SpecularColour;
				light.SpecularPower = spec_power ?? light.SpecularPower;
				light.CastShadow = cast_shadow ?? light.CastShadow;
				light.CameraRelative = camera_relative;
				return light;
			}
		}

		/// <summary>A ray description for hit testing in a 3d scene</summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct HitTestRay
		{
			// The world space origin and direction of the ray (normalisation not required)
			public v4 m_ws_origin;
			public v4 m_ws_direction;
		}

		/// <summary>The result of a ray cast hit test in a 3d scene</summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct HitTestResult
		{
			// The origin and direction of the cast ray (in world space)
			public v4 m_ws_ray_origin;
			public v4 m_ws_ray_direction;

			// The intercept point (in world space)
			public v4 m_ws_intercept;

			// The object that was hit (or null)
			public Object? HitObject => IsHit ? new Object(m_obj) : null;
			private IntPtr m_obj;

			/// <summary>The distance from the ray origin to the hit point</summary>
			public float m_distance;

			/// <summary>How the hit point was snapped (if at all)</summary>
			public ESnapType m_snap_type;

			/// <summary>True if something was hit</summary>
			public readonly bool IsHit => m_obj != IntPtr.Zero;
		}

		/// <summary>The viewport volume in render target space (i.e. not normalised)</summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct Viewport
		{
			// Notes:
			//  - The viewport is in "render target" space, so if the render target is a
			//    different size to the target window, 'Width/Height' do not equal the window size.
			public float X;
			public float Y;
			public float Width;
			public float Height;
			public float DepthMin;
			public float DepthMax;
			public int ScreenW;
			public int ScreenH;

			public Viewport(float x, float y, float w, float h)
				: this(x, y, w, h, (int)w, (int)h, 0f, 1f)
			{ }
			public Viewport(float x, float y, float w, float h, int sw, int sh, float min, float max)
			{
				X = x;
				Y = y;
				Width = w;
				Height = h;
				DepthMin = min;
				DepthMax = max;
				ScreenW = sw;
				ScreenH = sh;
			}
			public float Aspect => Width / Height;
			public Size ToSize() => new((int)Math.Round(Width), (int)Math.Round(Height));
			public SizeF ToSizeF() => new(Width, Height);
			public Rectangle ToRect() => new((int)X, (int)Y, (int)Math.Round(Width), (int)Math.Round(Height));
			public RectangleF ToRectF() => new(X, Y, Width, Height);
		}

		/// <summary>Include paths/sources for Ldr script #include resolving</summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct View3DIncludes
		{
			/// <summary>
			/// Create an includes object.
			/// 'paths' should be null or a comma or semi colon separated list of directories
			/// Remember module == IntPtr.Zero means "this" module</summary>
			public View3DIncludes(string paths)
				: this(paths, null)
			{ }
			public View3DIncludes(HMODULE module, IEnumerable<string> paths)
				: this(paths, new[] { module })
			{ }
			public View3DIncludes(IEnumerable<string> paths, IEnumerable<HMODULE> modules)
				: this(paths != null ? string.Join(",", paths) : null, modules.Take(16).ToArray())
			{ }
			public View3DIncludes(string? paths, HMODULE[]? modules)
			{
				m_include_paths = paths;
				m_modules = new HMODULE[16];
				m_module_count = 0;

				if (modules != null)
				{
					Array.Copy(modules, m_modules, modules.Length);
					m_module_count = modules.Length;
				}
			}

			/// <summary>A comma or semicolon separated list of search directories</summary>
			public string? IncludePaths
			{
				get => m_include_paths;
				set => m_include_paths = value;
			}
			[MarshalAs(UnmanagedType.LPWStr)]
			public string? m_include_paths;

			/// <summary>An array of binary modules that contain resources</summary>
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			public readonly IntPtr[] m_modules;
			public readonly int m_module_count;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct View3DSceneChanged
		{
			/// <summary>How the scene was changed</summary>
			public readonly ESceneChanged ChangeType;

			/// <summary>The context ids involved in the change</summary>
			public readonly Guid[] ContextIds => Marshal_.PtrToArray<Guid>(m_ctx_ids, m_count);
			private readonly IntPtr m_ctx_ids;
			private readonly int m_count;

			/// <summary>The object that changed (for single object changes only)</summary>
			public readonly Object? Object => m_object != IntPtr.Zero ? new Object(m_object) : null;
			private readonly HObject m_object;
		}

		#endregion

		#region Callback Functions

		/// <summary>Report errors callback</summary>
		public delegate void ReportErrorCB(IntPtr ctx, [MarshalAs(UnmanagedType.LPWStr)] string msg, [MarshalAs(UnmanagedType.LPWStr)] string filepath, int line, long pos);

		/// <summary>Report settings changed callback</summary>
		public delegate void SettingsChangedCB(IntPtr ctx, HWindow wnd, ESettings setting);

		/// <summary>Enumerate guids callback</summary>
		public delegate bool EnumGuidsCB(IntPtr ctx, Guid guid);

		/// <summary>Enumerate objects callback</summary>
		public delegate bool EnumObjectsCB(IntPtr ctx, HObject obj);

		/// <summary>Callback for progress updates during AddFile / Reload</summary>
		public delegate void AddFileProgressCB(IntPtr ctx, ref Guid context_id, [MarshalAs(UnmanagedType.LPWStr)] string filepath, long file_offset, bool complete, ref bool cancel);

		/// <summary>Callback for continuations after adding scripts/files</summary>
		public delegate void OnAddCB(IntPtr ctx, ref Guid context_id, bool before);
		public delegate void LoadScriptCompleteCB(Guid context_id, bool before);

		/// <summary>Callback when the sources are reloaded</summary>
		public delegate void SourcesChangedCB(IntPtr ctx, ESourcesChangedReason reason, bool before);

		/// <summary>Callback for when the collection of objects associated with a window changes</summary>
		public delegate void SceneChangedCB(IntPtr ctx, HWindow wnd, ref View3DSceneChanged args);

		/// <summary>Callback notification of animation commands</summary>
		public delegate void AnimationCB(IntPtr ctx, HWindow wnd, EAnimCommand command, double clock);

		/// <summary>Callback for when the window is invalidated</summary>
		public delegate void InvalidatedCB(IntPtr ctx, HWindow wnd);

		/// <summary>Called just prior to rendering</summary>
		public delegate void RenderCB(IntPtr ctx, HWindow wnd);

		/// <summary>Edit object callback</summary>
		public delegate void EditObjectCB(IntPtr ctx, int vcount, int icount, int ncount,
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)][In, Out] Vertex[] verts,
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)][In, Out] ushort[] indices,
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)][In, Out] Nugget[] nuggets,
			out int new_vcount, out int new_icount, out int new_ncount);

		/// <summary>Embedded code handler callback</summary>
		public delegate bool EmbeddedCodeHandlerCB(IntPtr ctx,
			[MarshalAs(UnmanagedType.LPWStr)] string code,
			[MarshalAs(UnmanagedType.LPWStr)] string support,
			[MarshalAs(UnmanagedType.BStr)] out string? result,
			[MarshalAs(UnmanagedType.BStr)] out string? errors);

		#endregion

		[Serializable]
		public class Exception :System.Exception
		{
			public EResult m_code = EResult.Success;
			public Exception() : this(EResult.Success) { }
			public Exception(EResult code) : this("", code) { }
			public Exception(string message) : this(message, EResult.Success) { }
			public Exception(string message, EResult code) : base(message) { m_code = code; }
			public Exception(string message, System.Exception innerException) : base(message, innerException) {}
			protected Exception(SerializationInfo serializationInfo, StreamingContext streamingContext) {throw new NotImplementedException();}
		}

		private readonly List<Window> m_windows;          // Groups of objects to render
		private readonly HContext m_context;              // Unique id per Initialise call
		private readonly Dispatcher m_dispatcher;         // Thread marshaller
		private readonly int m_thread_id;                 // The main thread id
		private ReportErrorCB m_error_cb;                 // Reference to callback
		private AddFileProgressCB m_add_file_progress_cb; // Reference to callback
		private OnAddCB m_on_add_cb;                      // Reference to callback
		private SourcesChangedCB m_sources_changed_cb;    // Reference to callback
		private Dictionary<string, EmbeddedCodeHandlerCB> m_embedded_code_handlers;
		private List<LoadScriptCompleteCB> m_load_script_handlers;

		#if PR_VIEW3D_CREATE_STACKTRACE
		private static List<StackTrace> m_create_stacktraces = new List<StackTrace>();
		#endif

		/// <summary>Initialise with support for BGRA textures</summary>
		public static ECreateDeviceFlags CreateDeviceFlags = ECreateDeviceFlags.D3D11_CREATE_DEVICE_BGRA_SUPPORT;

		/// <summary>Create a reference to the View3d singleton instance for this process</summary>
		public static View3d Create()
		{
			#if PR_VIEW3D_CREATE_STACKTRACE
			m_create_stacktraces.Add(new StackTrace(true));
			#endif
			++m_ref_count;
			return m_singleton ??= new View3d();
		}
		private static View3d? m_singleton;
		private static int m_ref_count;

		/// <summary></summary>
		private View3d()
		{
			if (!ModuleLoaded)
				throw new Exception("View3d.dll has not been loaded");

			m_windows = new List<Window>();
			m_dispatcher = Dispatcher.CurrentDispatcher;
			m_thread_id = Thread.CurrentThread.ManagedThreadId;
			m_embedded_code_handlers = new Dictionary<string, EmbeddedCodeHandlerCB>();
			m_load_script_handlers = new List<LoadScriptCompleteCB>();
			EmbeddedCSharpBoilerPlate = EmbeddedCSharpBoilerPlateDefault;

			try
			{
				// Initialise view3d
				m_context = View3D_Initialise(m_error_cb = HandleError, IntPtr.Zero, CreateDeviceFlags);
				if (m_context == HContext.Zero) throw LastError ?? new Exception("Failed to initialised View3d");
				void HandleError(IntPtr ctx, string msg, string filepath, int line, long pos)
				{
					if (m_thread_id != Thread.CurrentThread.ManagedThreadId)
					{
						m_dispatcher.Invoke(m_error_cb, ctx, msg, filepath, line, pos);
						return;
					}

					LastError = new Exception(msg);
					Error?.Invoke(this, new ErrorEventArgs(msg, filepath, line, pos));
				}

				// Sign up for progress reports
				View3D_AddFileProgressCBSet(m_add_file_progress_cb = HandleAddFileProgress, IntPtr.Zero, true);
				void HandleAddFileProgress(IntPtr ctx, ref Guid context_id, string filepath, long foffset, bool complete, ref bool cancel)
				{
					var args = new AddFileProgressEventArgs(context_id, filepath, foffset, complete);
					AddFileProgress?.Invoke(this, args);
					cancel = args.Cancel;
				}

				// Load script 'OnAdd' callbacks
				m_on_add_cb = HandleLoadScript;
				void HandleLoadScript(IntPtr ctx, ref Guid context_id, bool before)
				{
					var cb = ctx != IntPtr.Zero ? Marshal.GetDelegateForFunctionPointer<LoadScriptCompleteCB>(ctx) : null;
					if (cb != null && !before) m_load_script_handlers.Remove(cb);
					cb?.Invoke(context_id, before);
				}

				// Sign up for notification of the sources changing
				View3D_SourcesChangedCBSet(m_sources_changed_cb = HandleSourcesChanged, IntPtr.Zero, true);
				void HandleSourcesChanged(IntPtr ctx, ESourcesChangedReason reason, bool before)
				{
					if (m_thread_id != Thread.CurrentThread.ManagedThreadId)
						m_dispatcher.BeginInvoke(m_sources_changed_cb, ctx, reason, before);
					else
						OnSourcesChanged?.Invoke(this, new SourcesChangedEventArgs(reason, before));
				}

				// Add a C# code handler
				SetEmbeddedCodeHandler("CSharp", HandleEmbeddedCSharp);
				bool HandleEmbeddedCSharp(IntPtr ctx, string code, string support, out string? result, out string? errors) // worker thread context
				{
					// This function may be called simultaneously in multiple threads
					result = null;
					errors = null;

					// Create the source code to build
					var src = TemplateReplacer.Process(new StringSrc(EmbeddedCSharpBoilerPlate), @"(?<indent>[ \t]*)<<<(?<section>.*?)>>>", (tr, match) =>
					{
						var indent = match.Result("${indent}");
						var section = match.Groups["section"];
						if (section.Success && section.Value == "support")
							tr.PushSource(new AddIndents(new StringSrc(support), indent, true));
						if (section.Success && section.Value == "code")
							tr.PushSource(new AddIndents(new StringSrc(code), indent, true));
						return string.Empty;
					});

					try
					{
						// Create a runtime assembly from the embedded code
						using var ass = RuntimeAssembly.Compile(src, new[] { Util.ResolveAppPath() });
						using var inst = ass.New("ldr.Main");
						result = inst.Invoke<string>("Execute");
					}
					catch (CompileException ex)
					{
						errors = ex.ErrorReport();
						errors += "\r\nGenerated Code:\r\n" + src;
					}
					catch (Exception ex)
					{
						errors = ex.Message;
						errors += "\r\nGenerated Code:\r\n" + src;
					}
					return true;
				}
			}
			catch
			{
				Dispose();
				throw;
			}
		}
		public void Dispose()
		{
			Util.BreakIf(Util.IsGCFinalizerThread, "Disposing in the GC finaliser thread");
			if (--m_ref_count != 0)
			{
				Util.BreakIf(m_ref_count < 0);
				return;
			}
			if (m_singleton != null)
			{
				// Unsubscribe
				foreach (var emb in m_embedded_code_handlers)
					View3D_EmbeddedCodeCBSet(emb.Key, emb.Value, IntPtr.Zero, false);

				View3D_SourcesChangedCBSet(m_sources_changed_cb, IntPtr.Zero, false);
				View3D_AddFileProgressCBSet(m_add_file_progress_cb, IntPtr.Zero, false);
				View3D_GlobalErrorCBSet(m_error_cb, IntPtr.Zero, false);

				while (m_windows.Count != 0)
					m_windows[0].Dispose();

				View3D_Shutdown(m_context);
				m_singleton = null;
			}
			GC.SuppressFinalize(this);
		}

		/// <summary>The last error reported from View3d</summary>
		public Exception? LastError { get; private set; }

		/// <summary>Event call on errors. Note: can be called in a background thread context</summary>
		public event EventHandler<ErrorEventArgs>? Error;

		/// <summary>Progress update when a file is being parsed</summary>
		public event EventHandler<AddFileProgressEventArgs>? AddFileProgress;

		/// <summary>Event notifying whenever sources are loaded/reloaded</summary>
		public event EventHandler<SourcesChangedEventArgs>? OnSourcesChanged;

		/// <summary>Install/Remove an embedded code handler for language 'lang'</summary>
		public void SetEmbeddedCodeHandler(string lang, EmbeddedCodeHandlerCB handler)
		{
			if (handler != null)
			{
				m_embedded_code_handlers[lang] = handler;
				View3D_EmbeddedCodeCBSet(lang, handler, IntPtr.Zero, true);
			}
			else if (m_embedded_code_handlers.TryGetValue(lang, out handler!))
			{
				m_embedded_code_handlers.Remove(lang);
				View3D_EmbeddedCodeCBSet(lang, handler, IntPtr.Zero, false);
			}
		}

		/// <summary>
		/// Add objects from an ldr file or string. This will create all objects declared in 'ldr_script'
		/// with context id 'context_id' if given, otherwise an id will be created.
		/// 'include_paths' is a list of include paths to use to resolve #include directives (or null).</summary>
		public Guid LoadScript(string ldr_script, bool file, Guid? context_id = null, string[]? include_paths = null, LoadScriptCompleteCB? on_add = null)
		{
			// Note: this method is asynchronous, it returns before objects have been added to the object manager
			// in view3d. The 'on_add' callback should be used to add objects to windows once they are available.
			var ctx = context_id ?? Guid.NewGuid();
			if (on_add != null) m_load_script_handlers.Add(on_add);
			var on_add_ctx = on_add != null ? Marshal.GetFunctionPointerForDelegate(on_add) : IntPtr.Zero;
			var inc = new View3DIncludes { m_include_paths = string.Join(",", include_paths ?? Array.Empty<string>()) };
			return View3D_LoadScript(ldr_script, file, ref ctx, ref inc, m_on_add_cb, on_add_ctx);
		}

		/// <summary>Force a reload of all script sources</summary>
		public void ReloadScriptSources()
		{
			View3D_ReloadScriptSources();
		}

		/// <summary>Poll for changed script source files, and reload any that have changed. (designed as a Timer.Tick handler)</summary>
		public void CheckForChangedSources(object? sender = null, EventArgs? args = null)
		{
			View3D_CheckForChangedSources();
		}

		/// <summary>
		/// Delete all objects
		/// *WARNING* Careful with this function, make sure all C# references to View3D objects have been set to null
		/// otherwise, disposing them will result in memory corruption</summary>
		public void DeleteAllObjects()
		{
			View3D_ObjectsDeleteAll();
		}

		/// <summary>Delete all objects, filtered by 'context_ids'</summary>
		public void DeleteObjects(Guid[] context_ids, int include_count, int exclude_count)
		{
			Debug.Assert(include_count + exclude_count == context_ids.Length);
			using var ids = Marshal_.Pin(context_ids, GCHandleType.Pinned);
			View3D_ObjectsDeleteById(ids.Pointer, include_count, exclude_count);
		}

		/// <summary>Release all objects not displayed in any windows (filtered by 'context_ids')</summary>
		public void DeleteUnused(Guid[] context_ids, int include_count, int exclude_count)
		{
			Debug.Assert(include_count + exclude_count == context_ids.Length);
			using var ids = Marshal_.Pin(context_ids, GCHandleType.Pinned);
			View3D_ObjectsDeleteUnused(ids.Pointer, include_count, exclude_count);
		}

		/// <summary>Return the context id for objects created from file 'filepath' (or null if 'filepath' is not an existing source)</summary>
		public Guid? ContextIdFromFilepath(string filepath)
		{
			return View3D_ContextIdFromFilepath(filepath, out var id) ? id : (Guid?)null;
		}

		/// <summary>Enumerate the guids in the store</summary>
		public void EnumGuids(Action<Guid> cb)
		{
			EnumGuids(guid => { cb(guid); return true; });
		}
		public void EnumGuids(Func<Guid, bool> cb)
		{
			View3D_SourceEnumGuids((c, guid) => cb(guid), IntPtr.Zero);
		}

		/// <summary>Return the example Ldr script</summary>
		public static string ExampleScript => View3D_ExampleScriptBStr();

		/// <summary>Template descriptions for auto complete of LDraw script</summary>
		public static string AutoCompleteTemplates => View3D_AutoCompleteTemplatesBStr();

		/// <summary>Boilerplate C# code used for embedded C#</summary>
		public string EmbeddedCSharpBoilerPlate { get; set; }
		public static string EmbeddedCSharpBoilerPlateDefault =>
		#region Embedded C# Source
$@"//
//Assembly: netstandard.dll
//Assembly: System.dll
//Assembly: System.Drawing.dll
//Assembly: System.IO.dll
//Assembly: System.Linq.dll
//Assembly: System.Runtime.dll
//Assembly: System.Runtime.Extensions.dll
//Assembly: System.ValueTuple.dll
//Assembly: System.Xml.dll
//Assembly: System.Xml.Linq.dll
//Assembly: .\Rylogic.Core.dll
//Assembly: .\Rylogic.Core.Windows.dll
//Assembly: .\Rylogic.View3d.dll
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.LDraw;
using Rylogic.Maths;
using Rylogic.Utility;

namespace ldr
{{
	public class Main
	{{
		private StringBuilder Out = new StringBuilder();
		<<<support>>>

		public string Execute()
		{{
			<<<code>>>
			return Out.ToString();
		}}
	}}
}}
";
		#endregion

		/// <summary>Return the address (form: keyword.keyword...) within a script at 'position'</summary>
		public static string AddressAt(string ldr_script, long position = -1)
		{
			// 'script' should start from a root level position.
			// 'position' should be relative to 'script'
			return View3D_ObjectAddressAt(ldr_script, position != -1 ? position : ldr_script.Length) ?? string.Empty;
		}

		/// <summary>Flush any pending commands to the graphics card</summary>
		public static void Flush()
		{
			View3D_Flush();
		}

		#region DLL extern functions

		private const string Dll = "view3d";

		/// <summary>True if the view3d dll has been loaded</summary>
		public static bool ModuleLoaded => m_module != IntPtr.Zero;
		private static IntPtr m_module = IntPtr.Zero;

		/// <summary>The exception created if the module fails to load</summary>
		public static System.Exception? LoadError;

		/// <summary>Helper method for loading the view3d.dll from a platform specific path</summary>
		public static bool LoadDll(string dir = @".\lib\$(platform)\$(config)", bool throw_if_missing = true)
		{
			if (ModuleLoaded) return true;
			m_module = Win32.LoadDll(Dll + ".dll", out LoadError, dir, throw_if_missing);
			return m_module != IntPtr.Zero;
		}

		// Context
		[DllImport(Dll)]
		private static extern HContext View3D_Initialise(ReportErrorCB global_error_cb, IntPtr ctx, ECreateDeviceFlags device_flags);
		[DllImport(Dll)]
		private static extern void View3D_Shutdown(HContext context);
		[DllImport(Dll)]
		private static extern void View3D_GlobalErrorCBSet(ReportErrorCB error_cb, IntPtr ctx, bool add);
		[DllImport(Dll)]
		private static extern void View3D_SourceEnumGuids(EnumGuidsCB enum_guids_cb, IntPtr ctx);
		[DllImport(Dll)]
		private static extern Guid View3D_LoadScript([MarshalAs(UnmanagedType.LPWStr)] string ldr_script, bool is_file, ref Guid context_id, ref View3DIncludes includes, OnAddCB? on_add_complete, IntPtr ctx);
		[DllImport(Dll)]
		private static extern void View3D_ReloadScriptSources();
		[DllImport(Dll)]
		private static extern void View3D_ObjectsDeleteAll();
		[DllImport(Dll)]
		private static extern void View3D_ObjectsDeleteById(IntPtr context_ids, int include_count, int exclude_count);
		[DllImport(Dll)]
		private static extern void View3D_ObjectsDeleteUnused(IntPtr context_ids, int include_count, int exclude_count);
		[DllImport(Dll)]
		private static extern void View3D_CheckForChangedSources();
		[DllImport(Dll)]
		private static extern void View3D_AddFileProgressCBSet(AddFileProgressCB progress_cb, IntPtr ctx, bool add);
		[DllImport(Dll)]
		private static extern void View3D_SourcesChangedCBSet(SourcesChangedCB sources_changed_cb, IntPtr ctx, bool add);
		[DllImport(Dll)]
		private static extern void View3D_EmbeddedCodeCBSet([MarshalAs(UnmanagedType.LPWStr)] string lang, EmbeddedCodeHandlerCB embedded_code_cb, IntPtr ctx, bool add);
		[DllImport(Dll)]
		private static extern bool View3D_ContextIdFromFilepath([MarshalAs(UnmanagedType.LPWStr)] string filepath, out Guid id);

		// Windows
		[DllImport(Dll)]
		private static extern HWindow View3D_WindowCreate(HWND hwnd, ref WindowOptions opts);
		[DllImport(Dll)]
		private static extern void View3D_WindowDestroy(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_WindowErrorCBSet(HWindow window, ReportErrorCB error_cb, IntPtr ctx, bool add);
		[DllImport(Dll, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.LPWStr)]
		private static extern string View3D_WindowSettingsGet(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_WindowSettingsSet(HWindow window, [MarshalAs(UnmanagedType.LPWStr)] string settings);
		[DllImport(Dll)]
		private static extern void View3D_WindowSettingsChangedCB(HWindow window, SettingsChangedCB settings_changed_cb, IntPtr ctx, bool add);
		[DllImport(Dll)]
		private static extern void View3D_WindowInvalidatedCB(HWindow window, InvalidatedCB invalidated_cb, IntPtr ctx, bool add);
		[DllImport(Dll)]
		private static extern void View3D_WindowRenderingCB(HWindow window, RenderCB rendering_cb, IntPtr ctx, bool add);
		[DllImport(Dll)]
		private static extern void View3D_WindowSceneChangedCB(HWindow window, SceneChangedCB scene_changed_cb, IntPtr ctx, bool add);
		[DllImport(Dll)]
		private static extern void View3D_WindowAddObject(HWindow window, HObject obj);
		[DllImport(Dll)]
		private static extern void View3D_WindowRemoveObject(HWindow window, HObject obj);
		[DllImport(Dll)]
		private static extern void View3D_WindowRemoveAllObjects(HWindow window);
		[DllImport(Dll)]
		private static extern bool View3D_WindowHasObject(HWindow window, HObject obj, bool search_children);
		[DllImport(Dll)]
		private static extern int View3D_WindowObjectCount(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_WindowEnumGuids(HWindow window, EnumGuidsCB enum_guids_cb, IntPtr ctx);
		[DllImport(Dll)]
		private static extern void View3D_WindowEnumObjects(HWindow window, EnumObjectsCB enum_objects_cb, IntPtr ctx);
		[DllImport(Dll)]
		private static extern void View3D_WindowEnumObjectsById(HWindow window, EnumObjectsCB enum_objects_cb, IntPtr ctx, IntPtr context_ids, int include_count, int exclude_count);
		[DllImport(Dll)]
		private static extern void View3D_WindowAddObjectsById(HWindow window, IntPtr context_ids, int include_count, int exclude_count);
		[DllImport(Dll)]
		private static extern void View3D_WindowRemoveObjectsById(HWindow window, IntPtr context_ids, int include_count, int exclude_count);
		[DllImport(Dll)]
		private static extern void View3D_WindowAddGizmo(HWindow window, HGizmo giz);
		[DllImport(Dll)]
		private static extern void View3D_WindowRemoveGizmo(HWindow window, HGizmo giz);
		[DllImport(Dll)]
		private static extern BBox View3D_WindowSceneBounds(HWindow window, ESceneBounds bounds, int except_count, Guid[]? except);
		[DllImport(Dll)]
		private static extern bool View3D_WindowAnimating(HWindow window);
		[DllImport(Dll)]
		private static extern double View3D_WindowAnimTimeGet(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_WindowAnimTimeSet(HWindow window, double time_s);
		[DllImport(Dll)]
		private static extern void View3D_WindowAnimControl(HWindow window, EAnimCommand command, double time_s);
		[DllImport(Dll)]
		private static extern void View3D_WindowAnimEventCBSet(HWindow window, AnimationCB anim_cb, IntPtr ctx, bool add);
		[DllImport(Dll)]
		private static extern void View3D_WindowHitTestObjects(HWindow window, IntPtr rays, IntPtr hits, int ray_count, float snap_distance, EHitTestFlags flags, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 7)] HObject[] objects, int object_count);
		[DllImport(Dll)]
		private static extern void View3D_WindowHitTestByCtx(HWindow window, IntPtr rays, IntPtr hits, int ray_count, float snap_distance, EHitTestFlags flags, IntPtr context_ids, int include_count, int exclude_count);
		[DllImport(Dll)]
		private static extern v2 View3D_WindowDpiScale(HWindow window);

		// Camera
		[DllImport(Dll)]
		private static extern void View3D_CameraToWorldGet(HWindow window, out m4x4 c2w);
		[DllImport(Dll)]
		private static extern void View3D_CameraToWorldSet(HWindow window, ref m4x4 c2w);
		[DllImport(Dll)]
		private static extern void View3D_CameraPositionSet(HWindow window, v4 position, v4 lookat, v4 up);
		[DllImport(Dll)]
		private static extern void View3D_CameraCommit(HWindow window);
		[DllImport(Dll)]
		private static extern bool View3D_CameraOrthographicGet(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_CameraOrthographicSet(HWindow window, bool on);
		[DllImport(Dll)]
		private static extern float View3D_CameraFocusDistanceGet(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_CameraFocusDistanceSet(HWindow window, float dist);
		[DllImport(Dll)]
		private static extern void View3D_CameraFocusPointGet(HWindow window, out v4 position);
		[DllImport(Dll)]
		private static extern void View3D_CameraFocusPointSet(HWindow window, v4 position);
		[DllImport(Dll)]
		private static extern void View3D_CameraViewRectSet(HWindow window, float width, float height, float dist);
		[DllImport(Dll)]
		private static extern float View3D_CameraAspectGet(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_CameraAspectSet(HWindow window, float aspect);
		[DllImport(Dll)]
		private static extern float View3D_CameraFovXGet(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_CameraFovXSet(HWindow window, float fovX);
		[DllImport(Dll)]
		private static extern float View3D_CameraFovYGet(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_CameraFovYSet(HWindow window, float fovY);
		[DllImport(Dll)]
		private static extern void View3D_CameraFovSet(HWindow window, float fovX, float fovY);
		[DllImport(Dll)]
		private static extern void View3D_CameraBalanceFov(HWindow window, float fov);
		[DllImport(Dll)]
		private static extern void View3D_CameraClipPlanesGet(HWindow window, out float near, out float far, bool focus_relative);
		[DllImport(Dll)]
		private static extern void View3D_CameraClipPlanesSet(HWindow window, float near, float far, bool focus_relative);
		[DllImport(Dll)]
		private static extern ECameraLockMask View3D_CameraLockMaskGet(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_CameraLockMaskSet(HWindow window, ECameraLockMask mask);
		[DllImport(Dll)]
		private static extern v4 View3D_CameraAlignAxisGet(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_CameraAlignAxisSet(HWindow window, v4 axis);
		[DllImport(Dll)]
		private static extern void View3D_CameraResetZoom(HWindow window);
		[DllImport(Dll)]
		private static extern float View3D_CameraZoomGet(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_CameraZoomSet(HWindow window, float zoom);
		[DllImport(Dll)]
		private static extern void View3D_ResetView(HWindow window, v4 forward, v4 up, float dist, bool preserve_aspect, bool commit);
		[DllImport(Dll)]
		private static extern void View3D_ResetViewBBox(HWindow window, BBox bbox, v4 forward, v4 up, float dist, bool preserve_aspect, bool commit);
		[DllImport(Dll)]
		private static extern v2 View3D_ViewArea(HWindow window, float dist);
		[DllImport(Dll)]
		private static extern bool View3D_MouseNavigate(HWindow window, v2 ss_point, ENavOp nav_op, bool nav_start_or_end);
		[DllImport(Dll)]
		private static extern bool View3D_MouseNavigateZ(HWindow window, v2 ss_point, float delta, bool along_ray);
		[DllImport(Dll)]
		private static extern bool View3D_Navigate(HWindow window, float dx, float dy, float dz);
		[DllImport(Dll)]
		private static extern v2 View3D_SSPointToNSSPoint(HWindow window, v2 screen);
		[DllImport(Dll)]
		private static extern v4 View3D_NSSPointToWSPoint(HWindow window, v4 screen);
		[DllImport(Dll)]
		private static extern v4 View3D_WSPointToNSSPoint(HWindow window, v4 world);
		[DllImport(Dll)]
		private static extern void View3D_NSSPointToWSRay(HWindow window, v4 screen, out v4 ws_point, out v4 ws_direction);
		[DllImport(Dll)]
		private static extern ENavOp View3D_MouseBtnToNavOp(EMouseBtns mk);

		// Lights
		[DllImport(Dll)]
		private static extern bool View3D_LightPropertiesGet(HWindow window, out LightInfo light);
		[DllImport(Dll)]
		private static extern void View3D_LightPropertiesSet(HWindow window, ref LightInfo light);
		[DllImport(Dll)]
		private static extern void View3D_LightSource(HWindow window, v4 position, v4 direction, bool camera_relative);
		[DllImport(Dll)]
		private static extern void View3D_LightShowDialog(HWindow window);

		// Objects
		[DllImport(Dll)]
		private static extern Guid View3D_ObjectContextIdGet(HObject obj);
		[DllImport(Dll, CharSet = CharSet.Unicode)]
		private static extern HObject View3D_ObjectCreateLdr([MarshalAs(UnmanagedType.LPWStr)] string ldr_script, bool file, ref Guid context_id, ref View3DIncludes includes);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern HObject View3D_ObjectCreateP3DFile([MarshalAs(UnmanagedType.LPStr)] string name, uint colour, [MarshalAs(UnmanagedType.LPWStr)] string p3d_filepath, ref Guid context_id);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern HObject View3D_ObjectCreateP3DStream([MarshalAs(UnmanagedType.LPStr)] string name, uint colour, int size, IntPtr p3d_data, ref Guid context_id);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern HObject View3D_ObjectCreate([MarshalAs(UnmanagedType.LPStr)] string name, uint colour, int vcount, int icount, int ncount, IntPtr verts, IntPtr indices, IntPtr nuggets, ref Guid context_id);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern HObject View3D_ObjectCreateEditCB([MarshalAs(UnmanagedType.LPStr)] string name, uint colour, int vcount, int icount, int ncount, EditObjectCB edit_cb, IntPtr ctx, ref Guid context_id);
		[DllImport(Dll)]
		private static extern HObject View3D_ObjectCreateInstance(HObject obj);
		[DllImport(Dll)]
		private static extern void View3D_ObjectUpdate(HObject obj, [MarshalAs(UnmanagedType.LPWStr)] string ldr_script, EUpdateObject flags);
		[DllImport(Dll)]
		private static extern void View3D_ObjectEdit(HObject obj, EditObjectCB edit_cb, IntPtr ctx);
		[DllImport(Dll)]
		private static extern void View3D_ObjectDelete(HObject obj);
		[DllImport(Dll)]
		private static extern HObject View3D_ObjectGetRoot(HObject obj);
		[DllImport(Dll)]
		private static extern HObject View3D_ObjectGetParent(HObject obj);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern HObject View3D_ObjectGetChildByName(HObject obj, [MarshalAs(UnmanagedType.LPStr)] string name);
		[DllImport(Dll)]
		private static extern HObject View3D_ObjectGetChildByIndex(HObject obj, int index);
		[DllImport(Dll)]
		private static extern int View3D_ObjectChildCount(HObject obj);
		[DllImport(Dll)]
		private static extern void View3D_ObjectEnumChildren(HObject obj, EnumObjectsCB enum_objects_cb, IntPtr ctx);
		[DllImport(Dll, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.BStr)]
		private static extern string View3D_ObjectNameGetBStr(HObject obj);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern void View3D_ObjectNameSet(HObject obj, [MarshalAs(UnmanagedType.LPStr)] string name);
		[DllImport(Dll, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.BStr)]
		private static extern string View3D_ObjectTypeGetBStr(HObject obj);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern m4x4 View3D_ObjectO2WGet(HObject obj, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern void View3D_ObjectO2WSet(HObject obj, ref m4x4 o2w, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern m4x4 View3D_ObjectO2PGet(HObject obj, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern void View3D_ObjectO2PSet(HObject obj, ref m4x4 o2p, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern bool View3D_ObjectVisibilityGet(HObject obj, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern void View3D_ObjectVisibilitySet(HObject obj, bool visible, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern EFlags View3D_ObjectFlagsGet(HObject obj, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern void View3D_ObjectFlagsSet(HObject obj, EFlags flags, bool state, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern ESortGroup View3D_ObjectSortGroupGet(HObject obj, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern void View3D_ObjectSortGroupSet(HObject obj, ESortGroup group, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern ENuggetFlag View3D_ObjectNuggetFlagsGet(HObject obj, [MarshalAs(UnmanagedType.LPStr)] string? name, int index);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern void View3D_ObjectNuggetFlagsSet(HObject obj, ENuggetFlag flags, bool state, [MarshalAs(UnmanagedType.LPStr)] string? name, int index);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern Colour32 View3D_ObjectNuggetTintGet(HObject obj, [MarshalAs(UnmanagedType.LPStr)] string? name, int index);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern void View3D_ObjectNuggetTintSet(HObject obj, Colour32 colour, [MarshalAs(UnmanagedType.LPStr)] string? name, int index);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern uint View3D_ObjectColourGet(HObject obj, bool base_colour, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern void View3D_ObjectColourSet(HObject obj, uint colour, uint mask, [MarshalAs(UnmanagedType.LPStr)] string? name, EColourOp op, float op_value);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern float View3D_ObjectReflectivityGet(HObject obj, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern void View3D_ObjectReflectivitySet(HObject obj, float reflectivity, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern bool View3D_ObjectWireframeGet(HObject obj, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern void View3D_ObjectWireframeSet(HObject obj, bool wireframe, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern bool View3D_ObjectNormalsGet(HObject obj, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern void View3D_ObjectNormalsSet(HObject obj, bool show, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern void View3D_ObjectResetColour(HObject obj, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)]
		private static extern void View3D_ObjectSetTexture(HObject obj, HTexture tex, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll)]
		private static extern BBox View3D_ObjectBBoxMS(HObject obj, bool include_children);

		// Materials
		[DllImport(Dll)]
		private static extern HTexture View3D_TextureFromStock(EStockTexture tex);
		[DllImport(Dll)]
		private static extern HTexture View3D_TextureCreate(uint width, uint height, IntPtr data, uint data_size, ref TextureOptions.InteropData options);
		[DllImport(Dll)]
		private static extern HTexture View3D_TextureCreateFromUri([MarshalAs(UnmanagedType.LPWStr)] string resource, uint width, uint height, ref TextureOptions.InteropData options);
		[DllImport(Dll)]
		private static extern HCubeMap View3D_CubeMapCreateFromUri([MarshalAs(UnmanagedType.LPWStr)] string resource, uint width, uint height, ref TextureOptions.InteropData options);
		[DllImport(Dll)]
		private static extern void View3D_TextureLoadSurface(HTexture tex, int level, [MarshalAs(UnmanagedType.LPWStr)] string tex_filepath, Rectangle[]? dst_rect, Rectangle[]? src_rect, EFilter filter, uint colour_key);
		[DllImport(Dll)]
		private static extern void View3D_TextureRelease(HTexture tex);
		[DllImport(Dll)]
		private static extern void View3D_TextureGetInfo(HTexture tex, out ImageInfo info);
		[DllImport(Dll)]
		private static extern EResult View3D_TextureGetInfoFromFile([MarshalAs(UnmanagedType.LPWStr)] string tex_filepath, out ImageInfo info);
		[DllImport(Dll)]
		private static extern void View3D_TextureSetFilterAndAddrMode(HTexture tex, EFilter filter, EAddrMode addrU, EAddrMode addrV);
		[DllImport(Dll)]
		private static extern IntPtr View3D_TextureGetDC(HTexture tex, bool discard);
		[DllImport(Dll)]
		private static extern void View3D_TextureReleaseDC(HTexture tex);
		[DllImport(Dll)]
		private static extern void View3D_TextureResize(HTexture tex, uint width, uint height, bool all_instances, bool preserve);
		[DllImport(Dll)]
		private static extern void View3d_TexturePrivateDataGet(HTexture tex, Guid guid, ref uint size, IntPtr data);
		[DllImport(Dll)]
		private static extern void View3d_TexturePrivateDataSet(HTexture tex, Guid guid, uint size, IntPtr data);
		[DllImport(Dll)]
		private static extern void View3d_TexturePrivateDataIFSet(HTexture tex, Guid guid, IntPtr pointer);
		[DllImport(Dll)]
		private static extern ulong View3D_TextureRefCount(HTexture tex);
		[DllImport(Dll)]
		private static extern HTexture View3D_TextureRenderTarget(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_TextureResolveAA(HTexture dst, HTexture src);
		[DllImport(Dll)]
		private static extern HTexture View3D_TextureFromShared(IntPtr shared_resource, ref TextureOptions.InteropData options);
		[DllImport(Dll)]
		private static extern HTexture View3D_CreateDx9RenderTarget(HWND hwnd, uint width, uint height, ref TextureOptions.InteropData options, out IntPtr shared_handle);

		// Rendering
		[DllImport(Dll)]
		private static extern void View3D_Invalidate(HWindow window, bool erase);
		[DllImport(Dll)]
		private static extern void View3D_InvalidateRect(HWindow window, ref Win32.RECT rect, bool erase);
		[DllImport(Dll)]
		private static extern void View3D_Render(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_Present(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_Validate(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_RenderTargetRestore(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_RenderTargetSet(HWindow window, HTexture render_target, HTexture depth_buffer, bool is_new_main_rt);
		[DllImport(Dll)]
		private static extern void View3D_BackBufferSizeGet(HWindow window, out int width, out int height);
		[DllImport(Dll)]
		private static extern void View3D_BackBufferSizeSet(HWindow window, int width, int height);
		[DllImport(Dll)]
		private static extern Viewport View3D_Viewport(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_SetViewport(HWindow window, Viewport vp);
		[DllImport(Dll)]
		private static extern EFillMode View3D_FillModeGet(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_FillModeSet(HWindow window, EFillMode mode);
		[DllImport(Dll)]
		private static extern ECullMode View3D_CullModeGet(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_CullModeSet(HWindow window, ECullMode mode);
		[DllImport(Dll)]
		private static extern uint View3D_BackgroundColourGet(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_BackgroundColourSet(HWindow window, uint aarrggbb);
		[DllImport(Dll)]
		private static extern int View3D_MultiSamplingGet(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_MultiSamplingSet(HWindow window, int multisampling);

		// Tools
		[DllImport(Dll)]
		private static extern bool View3D_MeasureToolVisible(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_ShowMeasureTool(HWindow window, bool show);
		[DllImport(Dll)]
		private static extern bool View3D_AngleToolVisible(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_ShowAngleTool(HWindow window, bool show);

		// Gizmos
		[DllImport(Dll)]
		private static extern HGizmo View3D_GizmoCreate(Gizmo.EMode mode, ref m4x4 o2w);
		[DllImport(Dll)]
		private static extern void View3D_GizmoDelete(HGizmo gizmo);
		[DllImport(Dll)]
		private static extern void View3D_GizmoMovedCBSet(HGizmo gizmo, Gizmo.Callback cb, IntPtr ctx, bool add);
		[DllImport(Dll)]
		private static extern void View3D_GizmoAttach(HGizmo gizmo, HObject obj);
		[DllImport(Dll)]
		private static extern void View3D_GizmoDetach(HGizmo gizmo, HObject obj);
		[DllImport(Dll)]
		private static extern float View3D_GizmoScaleGet(HGizmo gizmo);
		[DllImport(Dll)]
		private static extern void View3D_GizmoScaleSet(HGizmo gizmo, float scale);
		[DllImport(Dll)]
		private static extern Gizmo.EMode View3D_GizmoGetMode(HGizmo gizmo);
		[DllImport(Dll)]
		private static extern void View3D_GizmoSetMode(HGizmo gizmo, Gizmo.EMode mode);
		[DllImport(Dll)]
		private static extern m4x4 View3D_GizmoGetO2W(HGizmo gizmo);
		[DllImport(Dll)]
		private static extern void View3D_GizmoSetO2W(HGizmo gizmo, ref m4x4 o2w);
		[DllImport(Dll)]
		private static extern m4x4 View3D_GizmoGetOffset(HGizmo gizmo);
		[DllImport(Dll)]
		private static extern bool View3D_GizmoEnabled(HGizmo gizmo);
		[DllImport(Dll)]
		private static extern void View3D_GizmoSetEnabled(HGizmo gizmo, bool enabled);
		[DllImport(Dll)]
		private static extern bool View3D_GizmoManipulating(HGizmo gizmo);

		// Diagnostics
		[DllImport(Dll)]
		private static extern bool View3D_DiagBBoxesVisibleGet(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_DiagBBoxesVisibleSet(HWindow window, bool visible);
		[DllImport(Dll)]
		private static extern float View3D_DiagNormalsLengthGet(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_DiagNormalsLengthSet(HWindow window, float length);
		[DllImport(Dll)]
		private static extern Colour32 View3D_DiagNormalsColourGet(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_DiagNormalsColourSet(HWindow window, Colour32 colour);
		[DllImport(Dll)]
		private static extern v2 View3D_DiagFillModePointsSizeGet(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_DiagFillModePointsSizeSet(HWindow window, v2 size);

		// Miscellaneous
		[DllImport(Dll)]
		private static extern void View3D_Flush();
		[DllImport(Dll)]
		private static extern bool View3D_TranslateKey(HWindow window, EKeyCodes key_code);
		[DllImport(Dll)]
		private static extern bool View3D_DepthBufferEnabledGet(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_DepthBufferEnabledSet(HWindow window, bool enabled);
		[DllImport(Dll)]
		private static extern bool View3D_FocusPointVisibleGet(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_FocusPointVisibleSet(HWindow window, bool show);
		[DllImport(Dll)]
		private static extern void View3D_FocusPointSizeSet(HWindow window, float size);
		[DllImport(Dll)]
		private static extern bool View3D_OriginVisibleGet(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_OriginVisibleSet(HWindow window, bool show);
		[DllImport(Dll)]
		private static extern void View3D_OriginSizeSet(HWindow window, float size);
		[DllImport(Dll)]
		private static extern bool View3D_SelectionBoxVisibleGet(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_SelectionBoxVisibleSet(HWindow window, bool visible);
		[DllImport(Dll)]
		private static extern void View3D_SelectionBoxPosition(HWindow window, ref BBox box, ref m4x4 o2w);
		[DllImport(Dll)]
		private static extern void View3D_SelectionBoxFitToSelected(HWindow window);
		[DllImport(Dll)]
		private static extern Guid View3D_DemoSceneCreate(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_DemoSceneDelete();
		[DllImport(Dll)]
		private static extern void View3D_DemoScriptShow(HWindow window);
		[DllImport(Dll)]
		private static extern void View3D_ObjectManagerShow(HWindow window, bool show);
		[DllImport(Dll)]
		private static extern m4x4 View3D_ParseLdrTransform([MarshalAs(UnmanagedType.LPWStr)] string ldr_script);
		[DllImport(Dll)]
		private static extern ulong View3D_RefCount(IntPtr pointer);

		[DllImport(Dll, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.BStr)]
		private static extern string View3D_ObjectAddressAt([MarshalAs(UnmanagedType.LPWStr)] string ldr_script, long position);

		[DllImport(Dll, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.BStr)]
		private static extern string View3D_ExampleScriptBStr();

		[DllImport(Dll, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.BStr)]
		private static extern string View3D_AutoCompleteTemplatesBStr();

		[DllImport(Dll)]
		private static extern HWND View3D_LdrEditorCreate(HWND parent);
		[DllImport(Dll)]
		private static extern void View3D_LdrEditorDestroy(HWND hwnd);
		[DllImport(Dll)]
		private static extern void View3D_LdrEditorCtrlInit(HWND scintilla_control, bool dark);

		#endregion
	}
}
