//#define PR_VIEW3D_CREATE_STACKTRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
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
using HSampler = System.IntPtr;
using HTexture = System.IntPtr;
using HShader = System.IntPtr;
using HWindow = System.IntPtr;
using HWND = System.IntPtr;

namespace Rylogic.Gfx
{
	/// <summary>.NET wrapper for View3D.dll</summary>
	public sealed partial class View3d : IDisposable
	{
		// Notes:
		// - Each process should create a single View3d instance that is an isolated context.
		// - Ldr objects are created and owned by the context.

		#region Enumerations
		public enum EResult : int
		{
			Success,
			Failed,
		}
		public enum EFillMode : int
		{
			Default = 0,
			Points = 1,
			Wireframe = 2, // D3D11_FILL_WIREFRAME
			Solid = 3, // D3D11_FILL_SOLID
			SolidWire = 4,
		}
		public enum ECullMode : int
		{
			Default = 0,
			None = 1, // D3D11_CULL_NONE
			Front = 2, // D3D11_CULL_FRONT
			Back = 3, // D3D11_CULL_BACK
		}
		[Flags] public enum EGeom : int
		{
			Unknown = 0,
			Vert = 1 << 0,
			Colr = 1 << 1,
			Norm = 1 << 2,
			Tex0 = 1 << 3,
		}
		public enum ETopo : int
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
		[Flags] public enum ENuggetFlag : int
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

			/// <summary>
			/// Can overlap with other nuggets.
			/// Set this flag to true if you want to add a nugget that overlaps the range
			/// of an existing nugget. For simple models, overlapping nugget ranges is
			/// usually an error, but in advanced cases it isn't.</summary>
			RangesCanOverlap = 1 << 5,
		}
		public enum ERenderStep :int
		{
			Invalid = 0,
			ForwardRender,
			GBuffer,
			DSLighting,
			ShadowMap,
			RayCast,
		}
		public enum EStockTexture : int
		{
			Invalid = 0,
			Black,
			White,
			Gray,
			Checker,
			Checker2,
			Checker3,
			WhiteDot,
			WhiteSpot,
			WhiteTriangle,
			EnvMapProjection,
		}
		public enum EStockSampler : int
		{
			Invalid = 0,
			PointClamp,
			PointWrap,
			LinearClamp,
			LinearWrap,
			AnisotropicClamp,
			AnisotropicWrap,
		}
		public enum EStockShader : int
		{
			// Forward rendering shaders
			StandardVS = 0,
			StandardPS = 0,

			// Radial fade params:
			//  *Type {Spherical|Cylindrical}
			//  *Radius {min,max}
			//  *Centre {x,y,z} (optional, defaults to camera position)
			//  *Absolute (optional, default false) - True if 'radius' is absolute, false if 'radius' should be scaled by the focus distance
			RadialFadePS,

			// Point sprite params: *PointSize {w,h} *Depth {true|false}
			PointSpritesGS,

			// Thick line params: *LineWidth {width}
			ThickLineListGS,

			// Thick line params: *LineWidth {width}
			ThickLineStripGS,

			// Arrow params: *Size {size}
			ArrowHeadGS,
		}
		public enum ELight : int
		{
			Ambient,
			Directional,
			Point,
			Spot
		}
		public enum EAnimCommand : int
		{
			Reset, // Reset the 'time' value
			Play,  // Run continuously using 'time' as the step size, or real time if 'time' == 0
			Stop,  // Stop at the current time.
			Step,  // Step by 'time' (can be positive or negative)
		}
		public enum EGizmoMode :int
		{
			Translate,
			Rotate,
			Scale,
		}
		public enum EGizmoState : int // ELdrGizmoEvent 
		{
			StartManip,
			Moving,
			Commit,
			Revert,
		}
		[Flags] public enum EStockObject : int
		{
			None = 0,
			FocusPoint = 1 << 0,
			OriginPoint = 1 << 1,
			SelectionBox = 1 << 2,
			_flags_enum = 0,
		};
		[Flags] public enum ENavOp : int
		{
			None = 0,
			Translate = 1 << 0,
			Rotate = 1 << 1,
			Zoom = 1 << 2,
		}
		[Flags] public enum ECameraLockMask : int
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
		[Flags] public enum EClipPlanes : int
		{
			None = 0,
			Near = 1 << 0,
			Far = 1 << 1,
			CameraRelative = 1 << 2,
			Both = Near | Far,
			_flags_enum = 0,
		}
		public enum EColourOp : int
		{
			Overwrite,
			Add,
			Subtract,
			Multiply,
			Lerp,
		}
		[Flags] public enum ELdrFlags : int
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
		[Flags] public enum EUpdateObject :int // Flags for partial update of a model
		{
			None         = 0,
			Name         = 1 << 0,
			Model        = 1 << 1,
			Transform    = 1 << 2,
			Children     = 1 << 3,
			Colour       = 1 << 4,
			ColourMask   = 1 << 5,
			Reflectivity = 1 << 6,
			Flags        = 1 << 7,
			Animation    = 1 << 8,
			All          = 0x1FF,
			_flags_enum = 0,
		}
		public enum ESortGroup : int
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
		public enum ESceneBounds : int
		{
			All,
			Selected,
			Visible,
		}
		public enum ESourcesChangedReason : int
		{
			NewData,
			Reload,
			Removal,
		}
		public enum ESceneChanged : int
		{
			ObjectsAdded,
			ObjectsRemoved,
			GizmoAdded,
			GizmoRemoved,
		}
		[Flags] public enum EHitTestFlags : int
		{
			Faces = 1 << 0,
			Edges = 1 << 1,
			Verts = 1 << 2,
		}
		public enum ESnapType : int
		{
			NoSnap,
			Vert,
			Edge,
			Face,
			EdgeCentre,
			FaceCentre,
		}
		[Flags] public enum ESettings : int
		{
			// Upper 2-bytes = category
			// Lower 2-bytes = specific property that changed.
			None = 0,

			General                     = 1 << 16,
			General_FocusPointVisible   = General | 1 << 0,
			General_OriginPointVisible  = General | 1 << 1,
			General_SelectionBoxVisible = General | 1 << 2,
			General_FocusPointSize      = General | 1 << 3,
			General_OriginPointSize     = General | 1 << 4,
			General_SelectionBox        = General | 1 << 5,

			Scene                  = 1 << 17,
			Scene_BackgroundColour = Scene | 1 << 0,
			Scene_Multisampling    = Scene | 1 << 1,
			Scene_FilllMode        = Scene | 1 << 2,
			Scene_CullMode         = Scene | 1 << 3,
			Scene_Viewport         = Scene | 1 << 4,
			Scene_EnvMap           = Scene | 1 << 5,

			Camera              = 1 << 18,
			Camera_Position     = Camera | 1 << 0,
			Camera_FocusDist    = Camera | 1 << 1,
			Camera_Orthographic = Camera | 1 << 2,
			Camera_Aspect       = Camera | 1 << 3,
			Camera_Fov          = Camera | 1 << 4,
			Camera_ClipPlanes   = Camera | 1 << 5,
			Camera_LockMask     = Camera | 1 << 6,
			Camera_AlignAxis    = Camera | 1 << 7,

			Lighting           = 1 << 19,
			Lighting_Type      = Lighting | 1 << 0,
			Lighting_Position  = Lighting | 1 << 1,
			Lighting_Direction = Lighting | 1 << 2,
			Lighting_Colour    = Lighting | 1 << 3,
			Lighting_Range     = Lighting | 1 << 4,
			Lighting_Shadows   = Lighting | 1 << 5,
			Lighting_All       = Lighting | Lighting_Type | Lighting_Position | Lighting_Direction | Lighting_Colour | Lighting_Range | Lighting_Shadows,

			Diagnostics                    = 1 << 20,
			Diagnostics_BBoxesVisible      = Diagnostics | 1 << 0,
			Diagnostics_NormalsLength      = Diagnostics | 1 << 1,
			Diagnostics_NormalsColour      = Diagnostics | 1 << 2,
			Diagnostics_FillModePointsSize = Diagnostics | 1 << 3,
		}
		#endregion
		#region D3D Enumerations
		public enum EFormat : uint
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
		[Flags] public enum EResFlags
		{
			D3D12_RESOURCE_FLAG_NONE = 0,
			D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET = 0x1,
			D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL = 0x2,
			D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS = 0x4,
			D3D12_RESOURCE_FLAG_DENY_SHADER_RESOURCE = 0x8,
			D3D12_RESOURCE_FLAG_ALLOW_CROSS_ADAPTER = 0x10,
			D3D12_RESOURCE_FLAG_ALLOW_SIMULTANEOUS_ACCESS = 0x20,
			D3D12_RESOURCE_FLAG_VIDEO_DECODE_REFERENCE_ONLY = 0x40,
			D3D12_RESOURCE_FLAG_VIDEO_ENCODE_REFERENCE_ONLY = 0x80,
			D3D12_RESOURCE_FLAG_RAYTRACING_ACCELERATION_STRUCTURE = 0x100
		}
		[Flags] public enum EResState : int
		{
			D3D12_RESOURCE_STATE_COMMON = 0,
			D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER = 0x1,
			D3D12_RESOURCE_STATE_INDEX_BUFFER = 0x2,
			D3D12_RESOURCE_STATE_RENDER_TARGET = 0x4,
			D3D12_RESOURCE_STATE_UNORDERED_ACCESS = 0x8,
			D3D12_RESOURCE_STATE_DEPTH_WRITE = 0x10,
			D3D12_RESOURCE_STATE_DEPTH_READ = 0x20,
			D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE = 0x40,
			D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE = 0x80,
			D3D12_RESOURCE_STATE_STREAM_OUT = 0x100,
			D3D12_RESOURCE_STATE_INDIRECT_ARGUMENT = 0x200,
			D3D12_RESOURCE_STATE_COPY_DEST = 0x400,
			D3D12_RESOURCE_STATE_COPY_SOURCE = 0x800,
			D3D12_RESOURCE_STATE_RESOLVE_DEST = 0x1000,
			D3D12_RESOURCE_STATE_RESOLVE_SOURCE = 0x2000,
			D3D12_RESOURCE_STATE_RAYTRACING_ACCELERATION_STRUCTURE = 0x400000,
			D3D12_RESOURCE_STATE_SHADING_RATE_SOURCE = 0x1000000,
			D3D12_RESOURCE_STATE_GENERIC_READ = (((((0x1 | 0x2) | 0x40) | 0x80) | 0x200) | 0x800),
			D3D12_RESOURCE_STATE_ALL_SHADER_RESOURCE = (0x40 | 0x80),
			D3D12_RESOURCE_STATE_PRESENT = 0,
			D3D12_RESOURCE_STATE_PREDICATION = 0x200,
			D3D12_RESOURCE_STATE_VIDEO_DECODE_READ = 0x10000,
			D3D12_RESOURCE_STATE_VIDEO_DECODE_WRITE = 0x20000,
			D3D12_RESOURCE_STATE_VIDEO_PROCESS_READ = 0x40000,
			D3D12_RESOURCE_STATE_VIDEO_PROCESS_WRITE = 0x80000,
			D3D12_RESOURCE_STATE_VIDEO_ENCODE_READ = 0x200000,
			D3D12_RESOURCE_STATE_VIDEO_ENCODE_WRITE = 0x800000
		}
		public enum EFilter : int
		{
			D3D12_FILTER_MIN_MAG_MIP_POINT = 0,
			D3D12_FILTER_MIN_MAG_POINT_MIP_LINEAR = 0x1,
			D3D12_FILTER_MIN_POINT_MAG_LINEAR_MIP_POINT = 0x4,
			D3D12_FILTER_MIN_POINT_MAG_MIP_LINEAR = 0x5,
			D3D12_FILTER_MIN_LINEAR_MAG_MIP_POINT = 0x10,
			D3D12_FILTER_MIN_LINEAR_MAG_POINT_MIP_LINEAR = 0x11,
			D3D12_FILTER_MIN_MAG_LINEAR_MIP_POINT = 0x14,
			D3D12_FILTER_MIN_MAG_MIP_LINEAR = 0x15,
			D3D12_FILTER_ANISOTROPIC = 0x55,
			D3D12_FILTER_COMPARISON_MIN_MAG_MIP_POINT = 0x80,
			D3D12_FILTER_COMPARISON_MIN_MAG_POINT_MIP_LINEAR = 0x81,
			D3D12_FILTER_COMPARISON_MIN_POINT_MAG_LINEAR_MIP_POINT = 0x84,
			D3D12_FILTER_COMPARISON_MIN_POINT_MAG_MIP_LINEAR = 0x85,
			D3D12_FILTER_COMPARISON_MIN_LINEAR_MAG_MIP_POINT = 0x90,
			D3D12_FILTER_COMPARISON_MIN_LINEAR_MAG_POINT_MIP_LINEAR = 0x91,
			D3D12_FILTER_COMPARISON_MIN_MAG_LINEAR_MIP_POINT = 0x94,
			D3D12_FILTER_COMPARISON_MIN_MAG_MIP_LINEAR = 0x95,
			D3D12_FILTER_COMPARISON_ANISOTROPIC = 0xd5,
			D3D12_FILTER_MINIMUM_MIN_MAG_MIP_POINT = 0x100,
			D3D12_FILTER_MINIMUM_MIN_MAG_POINT_MIP_LINEAR = 0x101,
			D3D12_FILTER_MINIMUM_MIN_POINT_MAG_LINEAR_MIP_POINT = 0x104,
			D3D12_FILTER_MINIMUM_MIN_POINT_MAG_MIP_LINEAR = 0x105,
			D3D12_FILTER_MINIMUM_MIN_LINEAR_MAG_MIP_POINT = 0x110,
			D3D12_FILTER_MINIMUM_MIN_LINEAR_MAG_POINT_MIP_LINEAR = 0x111,
			D3D12_FILTER_MINIMUM_MIN_MAG_LINEAR_MIP_POINT = 0x114,
			D3D12_FILTER_MINIMUM_MIN_MAG_MIP_LINEAR = 0x115,
			D3D12_FILTER_MINIMUM_ANISOTROPIC = 0x155,
			D3D12_FILTER_MAXIMUM_MIN_MAG_MIP_POINT = 0x180,
			D3D12_FILTER_MAXIMUM_MIN_MAG_POINT_MIP_LINEAR = 0x181,
			D3D12_FILTER_MAXIMUM_MIN_POINT_MAG_LINEAR_MIP_POINT = 0x184,
			D3D12_FILTER_MAXIMUM_MIN_POINT_MAG_MIP_LINEAR = 0x185,
			D3D12_FILTER_MAXIMUM_MIN_LINEAR_MAG_MIP_POINT = 0x190,
			D3D12_FILTER_MAXIMUM_MIN_LINEAR_MAG_POINT_MIP_LINEAR = 0x191,
			D3D12_FILTER_MAXIMUM_MIN_MAG_LINEAR_MIP_POINT = 0x194,
			D3D12_FILTER_MAXIMUM_MIN_MAG_MIP_LINEAR = 0x195,
			D3D12_FILTER_MAXIMUM_ANISOTROPIC = 0x1d5
		}
		public enum EAddrMode
		{
			D3D12_TEXTURE_ADDRESS_MODE_WRAP = 1,
			D3D12_TEXTURE_ADDRESS_MODE_MIRROR = 2,
			D3D12_TEXTURE_ADDRESS_MODE_CLAMP = 3,
			D3D12_TEXTURE_ADDRESS_MODE_BORDER = 4,
			D3D12_TEXTURE_ADDRESS_MODE_MIRROR_ONCE = 5
		}
		#endregion

		#region Structures

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct VICount
		{
			public int m_vcount;
			public int m_icount;
		};

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential)]
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

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct Nugget(
			ETopo topo, EGeom geom,
			int v0 = 0, int v1 = 0,
			int i0 = 0, int i1 = 0,
			HTexture? tex_diffuse = null,
			HSampler? sam_diffuse = null,
			IEnumerable<Nugget.Shader>? shaders = null,
			ENuggetFlag? flags = null,
			ECullMode? cull_mode = null,
			EFillMode? fill_mode = null,
			Colour32? tint = null,
			float? rel_reflec = null)
		{
			[StructLayout(LayoutKind.Sequential)] public struct Shader(ERenderStep rdr_step, HShader shader)
			{
				public ERenderStep m_rdr_step = rdr_step;
				public HShader m_shader = shader;
			}
			[StructLayout(LayoutKind.Sequential)] public struct Shaders
			{
				[MarshalAs(UnmanagedType.LPArray)] public Shader[] m_shaders;
				public int m_count;

				public Shaders(IEnumerable<Shader> shaders)
				{
					m_shaders = shaders.ToArray();
					m_count = m_shaders.Length;
				}
			}

			public ETopo m_topo           = topo;                                          // Model topology
			public EGeom m_geom           = geom;                                          // Model geometry type
			public int m_v0               = v0, m_v1 = v1;                                 // Vertex buffer range. Set to 0,0 to mean the whole buffer
			public int m_i0               = i0, m_i1 = i1;                                 // Index buffer range. Set to 0,0 to mean the whole buffer
			public HTexture m_tex_diffuse = tex_diffuse ?? IntPtr.Zero;                    // Diffuse texture
			public HSampler m_sam_diffuse = sam_diffuse ?? IntPtr.Zero;                    // Sampler for the diffuse texture
			public Shaders m_shaders      = new Shaders(shaders ?? Array.Empty<Shader>()); // Shader overrides for this nugget
			public ENuggetFlag m_nflags   = flags ?? ENuggetFlag.None;                     // Nugget flags (ENuggetFlag)
			public ECullMode m_cull_mode  = cull_mode ?? ECullMode.Back;                   // Face culling mode
			public EFillMode m_fill_mode  = fill_mode ?? EFillMode.Solid;                  // Model fill mode
			public Colour32 m_tint        = tint ?? Colour32.White;                        // Tint colour
			public float m_rel_reflec     = rel_reflec ?? 1f;                              // How reflective this nugget is relative to the over all model
		}

		/// <summary>Light source properties</summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct LightInfo
		{
			/// <summary>Position, only valid for point and spot lights</summary>
			public v4 Position;

			/// <summary>Direction, only valid for directional and spot lights</summary>
			public v4 Direction;

			/// <summary>The light source type. One of ambient, directional, point, spot</summary>
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

		/// <summary>Light source properties</summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct MultiSamp(int? count = null, int? quality = null) // DXGI_SAMPLE_DESC
		{
			public int Count = count ?? 1;
			public int Quality = quality ?? 0;

			public static int BestQuality(int count, EFormat format)
			{
				return View3D_MSAAQuality(count, format);
			}
		}

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct TextureOptions(
			m4x4? t2s = null, 
			EFormat? format = null, 
			int? mips = null, 
			EResFlags? usage = null, 
			EResState? state = null, 
			ClearValue? clear_value = null, 
			MultiSamp? msaa = null, 
			uint? colour_key = null, 
			bool? has_alpha = null, 
			string? dbg_name = null)
		{
			public m4x4 T2S = t2s ?? m4x4.Identity;
			public EFormat Format = format ?? EFormat.DXGI_FORMAT_R8G8B8A8_UNORM;
			public int Mips = mips ?? 0;
			public EResFlags Usage = usage ?? EResFlags.D3D12_RESOURCE_FLAG_NONE;
			public EResState State = state ?? EResState.D3D12_RESOURCE_STATE_COMMON;
			public ClearValue ClearValue = clear_value ?? new ClearValue(format ?? EFormat.DXGI_FORMAT_R8G8B8A8_UNORM);
			public MultiSamp MultiSamp = msaa ?? new MultiSamp();
			public uint ColourKey = colour_key ?? 0U;
			public bool HasAlpha = has_alpha ?? false;
			public string DbgName = dbg_name ?? string.Empty;
		}

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		public struct CubeMapOptions
		{
			public m4x4 m_cube2w;
			[MarshalAs(UnmanagedType.LPStr)] public string m_dbg_name;
		};

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct SamplerOptions
		{
			public EFilter m_filter;
			public EAddrMode m_addrU;
			public EAddrMode m_addrV;
			public EAddrMode m_addrW;
			[MarshalAs(UnmanagedType.LPStr)] public string m_dbg_name;
		};

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct ShaderOptions
		{
			// todo
		}

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct WindowOptions
		{
			public ReportErrorCB ErrorCB;
			public IntPtr ErrorCBCtx;
			public Colour32 BackgroundColour;
			public bool GdiCompatibleBackBuffer;
			public int Multisampling;
			public string DbgName;

			public static WindowOptions New(ReportErrorCB? error_cb = null, IntPtr? error_cb_ctx = null, Colour32? background_colour = null, bool? gdi_compatible_bb = null, string? dbg_name = null)
			{
				static void ErrorSink(IntPtr ctx, string msg, string filepath, int line, long pos) => throw new Exception(msg);
				return new()
				{
					ErrorCB = error_cb ?? ErrorSink,
					ErrorCBCtx = error_cb_ctx ?? IntPtr.Zero,
					BackgroundColour = background_colour ?? Colour32.Gray,
					GdiCompatibleBackBuffer = gdi_compatible_bb ?? false,
					Multisampling = (gdi_compatible_bb ?? false) ? 1 : 4,
					DbgName = dbg_name ?? string.Empty,
				};
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
			public readonly Object? HitObject => IsHit ? new Object(m_obj) : null;
			private readonly IntPtr m_obj;

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
			public readonly float Aspect => Width / Height;
			public readonly Size ToSize() => new((int)Math.Round(Width), (int)Math.Round(Height));
			public readonly SizeF ToSizeF() => new(Width, Height);
			public readonly Rectangle ToRect() => new((int)X, (int)Y, (int)Math.Round(Width), (int)Math.Round(Height));
			public readonly RectangleF ToRectF() => new(X, Y, Width, Height);
		}

		/// <summary>Include paths/sources for Ldr script #include resolving</summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct Includes
		{
			/// <summary>
			/// Create an includes object.
			/// 'paths' should be null or a comma or semi colon separated list of directories
			/// Remember module == IntPtr.Zero means "this" module</summary>
			public Includes(string paths)
				: this(paths, null)
			{ }
			public Includes(HMODULE module, IEnumerable<string> paths)
				: this(paths, new[] { module })
			{ }
			public Includes(IEnumerable<string> paths, IEnumerable<HMODULE> modules)
				: this(string.Join(",", paths), modules.Take(16).ToArray())
			{ }
			public Includes(string paths, HMODULE[]? modules)
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
			public string IncludePaths
			{
				readonly get => m_include_paths;
				set => m_include_paths = value;
			}
			[MarshalAs(UnmanagedType.LPStr)]
			public string m_include_paths;

			/// <summary>An array of binary modules that contain resources</summary>
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			public readonly IntPtr[] m_modules;
			public readonly int m_module_count;
		}

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential)]
		public readonly struct SceneChanged
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

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct AnimEvent
		{
			/// <summary>The state change type</summary>
			public EAnimCommand m_command;

			/// <summary>The current animation clock value</summary>
			public double m_clock;
		};

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct ImageInfo
		{
			public ulong Width;
			public uint Height;
			public ushort Depth;
			public ushort Mips;
			public EFormat Format;
			public uint ImageFileFormat; // D3DXIMAGE_FILEFORMAT
		};

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct BackBuffer
		{
			public IntPtr RenderTarget; // ID3D12Resource*
			public IntPtr DepthStencil; // ID3D12Resource*
			public Size Dim;
		};
		
		#endregion
		#region D3D Structures

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct DepthStencilValue
		{
			public float Depth;
			public byte Stencil;
		}

		/// <summary></summary>
		[StructLayout(LayoutKind.Explicit)]
		public struct ClearValue(EFormat format, Colour128 colour = default)
		{
			[FieldOffset(0)] public EFormat Format = format;
			[FieldOffset(4)] public Colour128 Colour = colour;
			[FieldOffset(4)] public DepthStencilValue DepthStencil;
		}

		#endregion

		#region Callback Functions

		/// <summary>Report errors callback</summary>
		public delegate void ReportErrorCB(IntPtr ctx, [MarshalAs(UnmanagedType.LPStr)] string msg, [MarshalAs(UnmanagedType.LPStr)] string filepath, int line, long pos);

		/// <summary>Report settings changed callback</summary>
		public delegate void SettingsChangedCB(IntPtr ctx, HWindow wnd, ESettings setting);

		/// <summary>Callback for progress updates during AddFile / Reload</summary>
		public delegate void AddFileProgressCB(IntPtr ctx, ref Guid context_id, [MarshalAs(UnmanagedType.LPStr)] string filepath, long file_offset, bool complete, ref bool cancel);

		/// <summary>Callback when the sources are reloaded</summary>
		public delegate void SourcesChangedCB(IntPtr ctx, ESourcesChangedReason reason, bool before);

		/// <summary>Enumerate GUIDs callback</summary>
		public delegate bool EnumGuidsCB(IntPtr ctx, ref Guid guid);

		/// <summary>Enumerate objects callback</summary>
		public delegate bool EnumObjectsCB(IntPtr ctx, HObject obj);

		/// <summary>Callback for continuations after adding scripts/files</summary>
		public delegate void OnAddCB(IntPtr ctx, ref Guid context_id, bool before);
		public delegate void LoadScriptCompleteCB(Guid context_id, bool before);

		/// <summary>Callback for when the window is invalidated</summary>
		public delegate void InvalidatedCB(IntPtr ctx, HWindow wnd);

		/// <summary>Called just prior to rendering</summary>
		public delegate void RenderingCB(IntPtr ctx, HWindow wnd);

		/// <summary>Callback for when the collection of objects associated with a window changes</summary>
		public delegate void SceneChangedCB(IntPtr ctx, HWindow wnd, ref SceneChanged args);

		/// <summary>Callback notification of animation commands</summary>
		public delegate void AnimationCB(IntPtr ctx, HWindow wnd, EAnimCommand command, double clock);

		/// <summary>Callback notification of gizmo moved</summary>
		public delegate void GizmoMovedCB(IntPtr ctx, HGizmo gizmo, EGizmoState state);

		/// <summary>Callback for emitting nuggets during EditObject</summary>
		public delegate void AddNuggetCB(IntPtr ctx, Nugget nugget);

		/// <summary>Edit object callback</summary>
		public delegate VICount EditObjectCB(IntPtr ctx,
			int vcount, int icount,
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)][In, Out] Vertex[] verts,
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)][In, Out] ushort[] indices,
			AddNuggetCB out_nugget, IntPtr out_nugget_ctx);

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
		}

		private readonly List<Window> m_windows;          // Groups of objects to render
		private readonly HContext m_context;              // Unique id per Initialise call
		private readonly SynchronizationContext m_sync;   // Thread marshaller
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
			m_sync = SynchronizationContext.Current ?? new SynchronizationContext();
			m_thread_id = Environment.CurrentManagedThreadId;
			m_embedded_code_handlers = new Dictionary<string, EmbeddedCodeHandlerCB>();
			m_load_script_handlers = new List<LoadScriptCompleteCB>();
			EmbeddedCSharpBoilerPlate = EmbeddedCSharpBoilerPlateDefault;

			try
			{
				// Initialise view3d
				m_context = View3D_Initialise(m_error_cb = HandleError, IntPtr.Zero);
				if (m_context == HContext.Zero) throw LastError ?? new Exception("Failed to initialised View3d");
				void HandleError(IntPtr ctx, string msg, string filepath, int line, long pos)
				{
					if (m_thread_id != Thread.CurrentThread.ManagedThreadId)
					{
						m_sync.Send(_ => HandleError(ctx, msg, filepath, line, pos), null);
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
						m_sync.Post(_ => HandleSourcesChanged(ctx, reason, before), null);
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

#if false
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
#endif
					throw new NotImplementedException();
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
			Util.BreakIf(Util.IsGCFinalizerThread, "Disposing in the GCFinaliserThread");
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
		public Guid LoadScriptFromString(string ldr_script, Guid? context_id = null, string[]? include_paths = null, LoadScriptCompleteCB? on_add = null)
		{
			// Note: this method is asynchronous, it returns before objects have been added to the object manager
			// in view3d. The 'on_add' callback should be used to add objects to windows once they are available.
			var ctx = context_id ?? Guid.NewGuid();
			if (on_add != null) m_load_script_handlers.Add(on_add);
			var on_add_ctx = on_add != null ? Marshal.GetFunctionPointerForDelegate(on_add) : IntPtr.Zero;
			var inc = new Includes { m_include_paths = string.Join(",", include_paths ?? Array.Empty<string>()) };
			return View3D_LoadScriptFromString(ldr_script, ref ctx, ref inc, m_on_add_cb, on_add_ctx);
		}

		/// <summary>
		/// Add objects from an ldr file. This will create all objects declared in 'ldr_script'
		/// with context id 'context_id' if given, otherwise an id will be created.
		/// 'include_paths' is a list of include paths to use to resolve #include directives (or null).</summary>
		public Guid LoadScriptFromFile(string ldr_script_file, Guid? context_id = null, string[]? include_paths = null, LoadScriptCompleteCB? on_add = null)
		{
			// Note: this method is asynchronous, it returns before objects have been added to the object manager
			// in view3d. The 'on_add' callback should be used to add objects to windows once they are available.
			var ctx = context_id ?? Guid.NewGuid();
			if (on_add != null) m_load_script_handlers.Add(on_add);
			var on_add_ctx = on_add != null ? Marshal.GetFunctionPointerForDelegate(on_add) : IntPtr.Zero;
			var inc = new Includes { m_include_paths = string.Join(",", include_paths ?? Array.Empty<string>()) };
			return View3D_LoadScriptFromFile(ldr_script_file, ref ctx, ref inc, m_on_add_cb, on_add_ctx);
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
			View3D_DeleteAllObjects();
		}

		/// <summary>Delete all objects, filtered by 'context_ids'</summary>
		public void DeleteObjects(Guid[] context_ids, int include_count, int exclude_count)
		{
			Debug.Assert(include_count + exclude_count == context_ids.Length);
			using var ids = Marshal_.Pin(context_ids, GCHandleType.Pinned);
			View3D_DeleteById(ids.Pointer, include_count, exclude_count);
		}

		/// <summary>Release all objects not displayed in any windows (filtered by 'context_ids')</summary>
		public void DeleteUnused(Guid[] context_ids, int include_count, int exclude_count)
		{
			Debug.Assert(include_count + exclude_count == context_ids.Length);
			using var ids = Marshal_.Pin(context_ids, GCHandleType.Pinned);
			View3D_DeleteUnused(ids.Pointer, include_count, exclude_count);
		}

		/// <summary>Return the context id for objects created from file 'filepath' (or null if 'filepath' is not an existing source)</summary>
		public Guid? ContextIdFromFilepath(string filepath)
		{
			return View3D_ContextIdFromFilepath(filepath, out var id) ? id : (Guid?)null;
		}

		/// <summary>Enumerate the GUIDs in the store</summary>
		public void EnumGuids(Action<Guid> cb)
		{
			EnumGuids(guid => { cb(guid); return true; });
		}
		public void EnumGuids(Func<Guid, bool> cb)
		{
			EnumGuidsCB enum_guids_cb = (IntPtr ctx, ref Guid guid) => cb(guid);
			View3D_SourceEnumGuids(enum_guids_cb, IntPtr.Zero);
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
//Assembly: .\Rylogic.Gfx.dll
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

		///// <summary>Flush any pending commands to the graphics card</summary>
		//public static void Flush()
		//{
		//	View3D_Flush();
		//}

		#region DLL extern functions

		private const string Dll = "view3d-12";

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

		// Dll Context ****************************

		// Initialise calls are reference counted and must be matched with Shutdown calls
		// 'initialise_error_cb' is used to report dll initialisation errors only (i.e. it isn't stored)
		// Note: this function is not thread safe, avoid race calls
		[DllImport(Dll)] private static extern HContext View3D_Initialise(ReportErrorCB global_error_cb, IntPtr ctx);
		[DllImport(Dll)] private static extern void View3D_Shutdown(HContext context);

		// This error callback is called for errors that are associated with the dll (rather than with a window).
		[DllImport(Dll)] private static extern void View3D_GlobalErrorCBSet(ReportErrorCB error_cb, IntPtr ctx, bool add);

		// Set the callback for progress events when script sources are loaded or updated
		[DllImport(Dll)] private static extern void View3D_AddFileProgressCBSet(AddFileProgressCB progress_cb, IntPtr ctx, bool add);

		// Set the callback that is called when the sources are reloaded
		[DllImport(Dll)] private static extern void View3D_SourcesChangedCBSet(SourcesChangedCB sources_changed_cb, IntPtr ctx, bool add);

		// Add/Remove a callback for handling embedded code within scripts
		[DllImport(Dll)] private static extern void View3D_EmbeddedCodeCBSet([MarshalAs(UnmanagedType.LPWStr)] string lang, EmbeddedCodeHandlerCB embedded_code_cb, IntPtr ctx, bool add);

		// Return the context id for objects created from 'filepath' (if filepath is an existing source)
		[DllImport(Dll)] private static extern bool View3D_ContextIdFromFilepath([MarshalAs(UnmanagedType.LPWStr)] string filepath, out Guid id);

		// Data Sources ***************************

		// Add an ldr script source. This will create all objects with context id 'context_id' (if given, otherwise an id will be created). Concurrent calls are thread safe.
		[DllImport(Dll)] private static extern Guid View3D_LoadScriptFromString([MarshalAs(UnmanagedType.LPStr)] string ldr_script, ref Guid context_id, ref Includes includes, OnAddCB? on_add_cb, IntPtr ctx);
		[DllImport(Dll)] private static extern Guid View3D_LoadScriptFromFile([MarshalAs(UnmanagedType.LPStr)] string ldr_file, ref Guid context_id, ref Includes includes, OnAddCB? on_add_cb, IntPtr ctx);
		
		// Delete all objects and object sources
		[DllImport(Dll)] private static extern void View3D_DeleteAllObjects();
		
		// Delete all objects matching (or not matching) a context id
		[DllImport(Dll)] private static extern void View3D_DeleteById(IntPtr context_ids, int include_count, int exclude_count);

		// Delete all objects not displayed in any windows
		[DllImport(Dll)] private static extern void View3D_DeleteUnused(IntPtr context_ids, int include_count, int exclude_count);

		// Enumerate the GUIDs of objects in the sources collection
		[DllImport(Dll)] private static extern void View3D_SourceEnumGuids(EnumGuidsCB enum_guids_cb, IntPtr ctx);

		// Reload script sources. This will delete all objects associated with the script sources then reload the files creating new objects with the same context ids.
		[DllImport(Dll)] private static extern void View3D_ReloadScriptSources();

		// Poll for changed script sources and reload any that have changed
		[DllImport(Dll)] private static extern void View3D_CheckForChangedSources();

		// Windows ********************************

		// Create/Destroy a window
		[DllImport(Dll)] private static extern HWindow View3D_WindowCreate(HWND hwnd, ref WindowOptions opts);
		[DllImport(Dll)] private static extern void View3D_WindowDestroy(HWindow window);

		// Add/Remove a window error callback. Note: The callback function can be called in a worker thread context.
		[DllImport(Dll)] private static extern void View3D_WindowErrorCBSet(HWindow window, ReportErrorCB error_cb, IntPtr ctx, bool add);

		// Get/Set the window settings (as ldr script string)
		[DllImport(Dll, CharSet = CharSet.Unicode)][return: MarshalAs(UnmanagedType.LPWStr)] private static extern string View3D_WindowSettingsGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_WindowSettingsSet(HWindow window, [MarshalAs(UnmanagedType.LPWStr)] string settings);

		// Get/Set the dimensions of the render target. Note: Not equal to window size for non-96 dpi screens!
		// In set, if 'width' and 'height' are zero, the RT is resized to the associated window automatically.
		[DllImport(Dll)] private static extern Size View3D_WindowBackBufferSizeGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_WindowBackBufferSizeSet(HWindow window, Size size);

		// Get/Set the window viewport (and clipping area)
		[DllImport(Dll)] private static extern Viewport View3D_WindowViewportGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_WindowViewportSet(HWindow window, ref Viewport vp);

		// Set a notification handler for when a window setting changes
		[DllImport(Dll)] private static extern void View3D_WindowSettingsChangedCB(HWindow window, SettingsChangedCB settings_changed_cb, IntPtr ctx, bool add);

		// Add/Remove a callback that is called when the collection of objects associated with 'window' changes
		[DllImport(Dll)] private static extern void View3D_WindowSceneChangedCB(HWindow window, SceneChangedCB scene_changed_cb, IntPtr ctx, bool add);

		// Add/Remove a callback that is called just prior to rendering the window
		[DllImport(Dll)] private static extern void View3D_WindowRenderingCB(HWindow window, RenderingCB rendering_cb, IntPtr ctx, bool add);

		// Add/Remove an object to/from a window
		[DllImport(Dll)] private static extern void View3D_WindowAddObject(HWindow window, HObject obj);
		[DllImport(Dll)] private static extern void View3D_WindowRemoveObject(HWindow window, HObject obj);

		// Add/Remove a gizmo to/from a window
		[DllImport(Dll)] private static extern void View3D_WindowAddGizmo(HWindow window, HGizmo giz);
		[DllImport(Dll)] private static extern void View3D_WindowRemoveGizmo(HWindow window, HGizmo giz);

		// Add/Remove objects by context id. This function can be used to add all objects either in, or not in 'context_ids'
		[DllImport(Dll)] private static extern void View3D_WindowAddObjectsById(HWindow window, IntPtr context_ids, int include_count, int exclude_count);
		[DllImport(Dll)] private static extern void View3D_WindowRemoveObjectsById(HWindow window, IntPtr context_ids, int include_count, int exclude_count);

		// Remove all objects 'window'
		[DllImport(Dll)] private static extern void View3D_WindowRemoveAllObjects(HWindow window);

		// Enumerate the object collection GUIDs associated with 'window'
		[DllImport(Dll)] private static extern void View3D_WindowEnumGuids(HWindow window, EnumGuidsCB enum_guids_cb, IntPtr ctx);

		// Enumerate the objects associated with 'window'
		[DllImport(Dll)] private static extern void View3D_WindowEnumObjects(HWindow window, EnumObjectsCB enum_objects_cb, IntPtr ctx);
		[DllImport(Dll)] private static extern void View3D_WindowEnumObjectsById(HWindow window, EnumObjectsCB enum_objects_cb, IntPtr ctx, IntPtr context_ids, int include_count, int exclude_count);

		// Return true if 'object' is among 'window's objects
		[DllImport(Dll)] private static extern bool View3D_WindowHasObject(HWindow window, HObject obj, bool search_children);

		// Return the number of objects assigned to 'window'
		[DllImport(Dll)] private static extern int View3D_WindowObjectCount(HWindow window);

		// Return the bounds of a scene
		[DllImport(Dll)] private static extern BBox View3D_WindowSceneBounds(HWindow window, ESceneBounds bounds, int except_count, [MarshalAs(UnmanagedType.LPArray)] Guid[]? except);

		// Render the window
		[DllImport(Dll)] private static extern void View3D_WindowRender(HWindow window);

		// Wait for any previous frames to complete rendering within the GPU
		[DllImport(Dll)] private static extern void View3D_WindowGSyncWait(HWindow window);

		// Replace the swap chain buffers with 'targets'
		[DllImport(Dll)] private static extern void View3D_WindowCustomSwapChain(HWindow window, int count, [MarshalAs(UnmanagedType.LPArray)] HTexture[] targets);

		// Get the MSAA back buffer (render target + depth stencil)
		[DllImport(Dll)] private static extern BackBuffer View3D_WindowRenderTargetGet(HWindow window);

		// Signal the window is invalidated. This does not automatically trigger rendering. Use InvalidatedCB.
		[DllImport(Dll)] private static extern void View3D_WindowInvalidate(HWindow window, bool erase);
		[DllImport(Dll)] private static extern void View3D_WindowInvalidateRect(HWindow window, ref Win32.RECT rect, bool erase);

		// Register a callback for when the window is invalidated. This can be used to render in response to invalidation, rather than rendering on a polling cycle.
		[DllImport(Dll)] private static extern void View3D_WindowInvalidatedCB(HWindow window, InvalidatedCB invalidated_cb, IntPtr ctx, bool add);

		// Clear the 'invalidated' state of the window.
		[DllImport(Dll)] private static extern void View3D_WindowValidate(HWindow window);

		// Get/Set the window background colour
		[DllImport(Dll)] private static extern uint View3D_WindowBackgroundColourGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_WindowBackgroundColourSet(HWindow window, uint argb);

		// Get/Set the fill mode for the window
		[DllImport(Dll)] private static extern EFillMode View3D_WindowFillModeGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_WindowFillModeSet(HWindow window, EFillMode mode);

		// Get/Set the cull mode for a faces in window
		[DllImport(Dll)] private static extern ECullMode View3D_WindowCullModeGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_WindowCullModeSet(HWindow window, ECullMode mode);

		// Get/Set the multi-sampling mode for a window
		[DllImport(Dll)] private static extern int View3D_MultiSamplingGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_MultiSamplingSet(HWindow window, int multisampling);

		// Control animation
		[DllImport(Dll)] private static extern void View3D_WindowAnimControl(HWindow window, EAnimCommand command, double time_s);

		// Get/Set the animation time
		[DllImport(Dll)] private static extern bool View3D_WindowAnimating(HWindow window);
		[DllImport(Dll)] private static extern double View3D_WindowAnimTimeGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_WindowAnimTimeSet(HWindow window, double time_s);

		// Set the callback for animation events
		[DllImport(Dll)] private static extern void View3D_WindowAnimEventCBSet(HWindow window, AnimationCB anim_cb, IntPtr ctx, bool add);

		// Return the DPI of the monitor that 'window' is displayed on
		[DllImport(Dll)] private static extern v2 View3D_WindowDpiScale(HWindow window);

		// Set the global environment map for the window
		[DllImport(Dll)] private static extern void View3D_WindowEnvMapSet(HWindow window, CubeMap env_map);

		// Enable/Disable the depth buffer
		[DllImport(Dll)] private static extern bool View3D_DepthBufferEnabledGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_DepthBufferEnabledSet(HWindow window, bool enabled);

		// Cast a ray into the scene, returning information about what it hit.
		[DllImport(Dll)] private static extern void View3D_WindowHitTestObjects(HWindow window, IntPtr rays, IntPtr hits, int ray_count, float snap_distance, EHitTestFlags flags, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 7)] HObject[] objects, int object_count);
		[DllImport(Dll)] private static extern void View3D_WindowHitTestByCtx(HWindow window, IntPtr rays, IntPtr hits, int ray_count, float snap_distance, EHitTestFlags flags, IntPtr context_ids, int include_count, int exclude_count);

		// Camera *********************************

		// Position the camera and focus distance
		[DllImport(Dll)] private static extern void View3D_CameraPositionSet(HWindow window, v4 position, v4 lookat, v4 up);

		// Get/Set the current camera to world transform
		[DllImport(Dll)] private static extern m4x4 View3D_CameraToWorldGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_CameraToWorldSet(HWindow window, ref m4x4 c2w);

		// Move the camera to a position that can see the whole scene. Set 'dist' to 0 to preserve the FoV, or a distance to set the FoV
		[DllImport(Dll)] private static extern void View3D_ResetView(HWindow window, v4 forward, v4 up, float dist, bool preserve_aspect, bool commit);

		// Reset the camera to view a bbox. Set 'dist' to 0 to preserve the FoV, or a distance to set the FoV
		[DllImport(Dll)] private static extern void View3D_ResetViewBBox(HWindow window, BBox bbox, v4 forward, v4 up, float dist, bool preserve_aspect, bool commit);

		// Enable/Disable orthographic projection
		[DllImport(Dll)] private static extern bool View3D_CameraOrthographicGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_CameraOrthographicSet(HWindow window, bool on);

		// Get/Set the distance to the camera focus point
		[DllImport(Dll)] private static extern float View3D_CameraFocusDistanceGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_CameraFocusDistanceSet(HWindow window, float dist);

		// Get/Set the camera focus point position
		[DllImport(Dll)] private static extern v4 View3D_CameraFocusPointGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_CameraFocusPointSet(HWindow window, v4 position);

		// Get/Set the aspect ratio for the camera field of view
		[DllImport(Dll)] private static extern float View3D_CameraAspectGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_CameraAspectSet(HWindow window, float aspect);

		// Get/Set both the X and Y fields of view (i.e. set the aspect ratio). Null fov means don't change the current value.
		[DllImport(Dll)] private static extern v2 View3D_CameraFovGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_CameraFovSet(HWindow window, v2 fov);

		// Adjust the FocusDist, FovX, and FovY so that the average FOV equals 'fov'
		[DllImport(Dll)] private static extern void View3D_CameraBalanceFov(HWindow window, float fov);

		// Get/Set (using fov and focus distance) the size of the perpendicular area visible to the camera at 'dist' (in world space). Use 'focus_dist != 0' to set a specific focus distance
		[DllImport(Dll)] private static extern v2 View3D_CameraViewRectAtDistanceGet(HWindow window, float dist);
		[DllImport(Dll)] private static extern void View3D_CameraViewRectAtDistanceSet(HWindow window, v2 rect, float focus_dist);

		// Get/Set the near and far clip planes for the camera
		[DllImport(Dll)] private static extern v2 View3D_CameraClipPlanesGet(HWindow window, EClipPlanes flags);
		[DllImport(Dll)] private static extern void View3D_CameraClipPlanesSet(HWindow window, float near, float far, EClipPlanes flags);

		// Get/Set the scene camera lock mask
		[DllImport(Dll)] private static extern ECameraLockMask View3D_CameraLockMaskGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_CameraLockMaskSet(HWindow window, ECameraLockMask mask);
	
		// Get/Set the camera align axis
		[DllImport(Dll)] private static extern v4 View3D_CameraAlignAxisGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_CameraAlignAxisSet(HWindow window, v4 axis);
	
		// Reset to the default zoom
		[DllImport(Dll)] private static extern void View3D_CameraResetZoom(HWindow window);

		// Get/Set the FOV zoom
		[DllImport(Dll)] private static extern float View3D_CameraZoomGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_CameraZoomSet(HWindow window, float zoom);

		// Commit the current O2W position as the reference position
		[DllImport(Dll)] private static extern void View3D_CameraCommit(HWindow window);

		// Navigation *****************************

		// Direct movement of the camera
		[DllImport(Dll)] private static extern bool View3D_Navigate(HWindow window, float dx, float dy, float dz);

		// Move the scene camera using the mouse
		[DllImport(Dll)] private static extern bool View3D_MouseNavigate(HWindow window, v2 ss_point, ENavOp nav_op, bool nav_start_or_end);
		[DllImport(Dll)] private static extern bool View3D_MouseNavigateZ(HWindow window, v2 ss_point, float delta, bool along_ray);

		// Convert an MK_ macro to a default navigation operation
		[DllImport(Dll)] private static extern ENavOp View3D_MouseBtnToNavOp(EMouseBtns mk);

		// Convert a point between 'window' screen space and normalised screen space
		[DllImport(Dll)] private static extern v2 View3D_SSPointToNSSPoint(HWindow window, v2 screen);
		[DllImport(Dll)] private static extern v2 View3D_NSSPointToSSPoint(HWindow window, v2 nss_point);

		// Convert a point between world space and normalised screen space.
		// The x,y components of 'screen' should be in normalised screen space, i.e. (-1,-1)->(1,1)
		// The z component should be the world space distance from the camera
		[DllImport(Dll)] private static extern v4 View3D_NSSPointToWSPoint(HWindow window, v4 screen);
		[DllImport(Dll)] private static extern v4 View3D_WSPointToNSSPoint(HWindow window, v4 world);

		// Return a point and direction in world space corresponding to a normalised screen space point.
		// The x,y components of 'screen' should be in normalised screen space, i.e. (-1,-1)->(1,1)
		// The z component should be the world space distance from the camera
		[DllImport(Dll)] private static extern void View3D_NSSPointToWSRay(HWindow window, v4 screen, out v4 ws_point, out v4 ws_direction);

		// Lights *********************************

		// Get/Set the properties of the global light
		[DllImport(Dll)] private static extern LightInfo View3D_LightPropertiesGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_LightPropertiesSet(HWindow window, ref LightInfo light);
	
		// Set the global light source for a window
		[DllImport(Dll)] private static extern void View3D_LightSource(HWindow window, v4 position, v4 direction, bool camera_relative);

		// Objects ********************************

		// Notes:
		// 'name' parameter on object get/set functions:
		//   If 'name' is null, then the state of the root object is returned
		//   If 'name' begins with '#' then the remainder of the name is treated as a regular expression

		// Create an object from provided buffers
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern HObject View3D_ObjectCreate([MarshalAs(UnmanagedType.LPStr)] string name, uint colour, int vcount, int icount, int ncount, IntPtr verts, IntPtr indices, IntPtr nuggets, ref Guid context_id);

		// Create an graphics object from ldr script, either a string or a file 
		[DllImport(Dll, CharSet = CharSet.Unicode)] private static extern HObject View3D_ObjectCreateLdrW([MarshalAs(UnmanagedType.LPWStr)] string ldr_script, bool file, ref Guid context_id, ref Includes includes);
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern HObject View3D_ObjectCreateLdrA([MarshalAs(UnmanagedType.LPStr)] string ldr_script, bool file, ref Guid context_id, ref Includes includes);

		// Load a p3d model file as a view3d object
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern HObject View3D_ObjectCreateP3DFile([MarshalAs(UnmanagedType.LPStr)] string name, uint colour, [MarshalAs(UnmanagedType.LPStr)] string p3d_filepath, ref Guid context_id);

		// Load a p3d model in memory as a view3d object
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern HObject View3D_ObjectCreateP3DStream([MarshalAs(UnmanagedType.LPStr)] string name, uint colour, int size, IntPtr p3d_data, ref Guid context_id);

		// Create an ldr object using a callback to populate the model data.
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern HObject View3D_ObjectCreateWithCallback([MarshalAs(UnmanagedType.LPStr)] string name, uint colour, int vcount, int icount, int ncount, EditObjectCB edit_cb, IntPtr ctx, ref Guid context_id);
		[DllImport(Dll)] private static extern void View3D_ObjectEdit(HObject obj, EditObjectCB edit_cb, IntPtr ctx);

		// Replace the model and all child objects of 'obj' with the results of 'ldr_script'
		[DllImport(Dll)] private static extern void View3D_ObjectUpdate(HObject obj, [MarshalAs(UnmanagedType.LPWStr)] string ldr_script, EUpdateObject flags);

		// Delete an object, freeing its resources
		[DllImport(Dll)] private static extern void View3D_ObjectDelete(HObject obj);

		// Create an instance of 'obj'
		[DllImport(Dll)] private static extern HObject View3D_ObjectCreateInstance(HObject obj);

		// Return the context id that this object belongs to
		[DllImport(Dll)] private static extern Guid View3D_ObjectContextIdGet(HObject obj);

		// Return the root object of 'object'(possibly itself)
		[DllImport(Dll)] private static extern HObject View3D_ObjectGetRoot(HObject obj);

		// Return the immediate parent of 'object'
		[DllImport(Dll)] private static extern HObject View3D_ObjectGetParent(HObject obj);

		// Return a child object of 'object'
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern HObject View3D_ObjectGetChildByName(HObject obj, [MarshalAs(UnmanagedType.LPStr)] string name);
		[DllImport(Dll)] private static extern HObject View3D_ObjectGetChildByIndex(HObject obj, int index);

		// Return the number of child objects of 'object'
		[DllImport(Dll)] private static extern int View3D_ObjectChildCount(HObject obj);

		// Enumerate the child objects of 'object'. (Not recursive)
		[DllImport(Dll)] private static extern void View3D_ObjectEnumChildren(HObject obj, EnumObjectsCB enum_objects_cb, IntPtr ctx);

		// Get/Set the name of 'object'
		[DllImport(Dll, CharSet = CharSet.Unicode)][return: MarshalAs(UnmanagedType.BStr)] private static extern string View3D_ObjectNameGetBStr(HObject obj);
		[DllImport(Dll)][return: MarshalAs(UnmanagedType.LPStr)] private static extern string View3D_ObjectNameGet(HObject obj);
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern void View3D_ObjectNameSet(HObject obj, [MarshalAs(UnmanagedType.LPStr)] string name);

		// Get the type of 'object'
		[DllImport(Dll, CharSet = CharSet.Unicode)][return: MarshalAs(UnmanagedType.BStr)] private static extern string View3D_ObjectTypeGetBStr(HObject obj);
		[DllImport(Dll, CharSet = CharSet.Ansi)][return: MarshalAs(UnmanagedType.LPStr)] private static extern string View3D_ObjectTypeGet(HObject obj);

		// Get/Set the current or base colour of an object(the first object to match 'name') (See LdrObject::Apply)
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern uint View3D_ObjectColourGet(HObject obj, bool base_colour, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern void View3D_ObjectColourSet(HObject obj, uint colour, uint mask, [MarshalAs(UnmanagedType.LPStr)] string? name, EColourOp op, float op_value);

		// Reset the object colour back to its default
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern void View3D_ObjectResetColour(HObject obj, [MarshalAs(UnmanagedType.LPStr)] string? name);

		// Get/Set the object's o2w transform
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern m4x4 View3D_ObjectO2WGet(HObject obj, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern void View3D_ObjectO2WSet(HObject obj, ref m4x4 o2w, [MarshalAs(UnmanagedType.LPStr)] string? name);

		// Get/Set the object to parent transform for an object.
		// This is the object to world transform for objects without parents.
		// Note: In "*Box b { 1 1 1 *o2w{*pos{1 2 3}} }" setting this transform overwrites the "*o2w{*pos{1 2 3}}".
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern m4x4 View3D_ObjectO2PGet(HObject obj, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern void View3D_ObjectO2PSet(HObject obj, ref m4x4 o2p, [MarshalAs(UnmanagedType.LPStr)] string? name);

		// Return the model space bounding box for 'object'
		[DllImport(Dll)] private static extern BBox View3D_ObjectBBoxMS(HObject obj, bool include_children);

		// Get/Set the object visibility. See LdrObject::Apply for docs on the format of 'name'
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern bool View3D_ObjectVisibilityGet(HObject obj, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern void View3D_ObjectVisibilitySet(HObject obj, bool visible, [MarshalAs(UnmanagedType.LPStr)] string? name);

		// Get/Set wireframe mode for an object (the first object to match 'name'). (See LdrObject::Apply)
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern bool View3D_ObjectWireframeGet(HObject obj, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern void View3D_ObjectWireframeSet(HObject obj, bool wireframe, [MarshalAs(UnmanagedType.LPStr)] string? name);

		// Get/Set the object flags. See LdrObject::Apply for docs on the format of 'name'
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern ELdrFlags View3D_ObjectFlagsGet(HObject obj, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern void View3D_ObjectFlagsSet(HObject obj, ELdrFlags flags, bool state, [MarshalAs(UnmanagedType.LPStr)] string? name);

		// Get/Set the reflectivity of an object (the first object to match 'name') (See LdrObject::Apply)
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern float View3D_ObjectReflectivityGet(HObject obj, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern void View3D_ObjectReflectivitySet(HObject obj, float reflectivity, [MarshalAs(UnmanagedType.LPStr)] string? name);

		// Get/Set the sort group for the object or its children. (See LdrObject::Apply)
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern ESortGroup View3D_ObjectSortGroupGet(HObject obj, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern void View3D_ObjectSortGroupSet(HObject obj, ESortGroup group, [MarshalAs(UnmanagedType.LPStr)] string? name);

		// Get/Set 'show normals' mode for an object (the first object to match 'name') (See LdrObject::Apply)
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern bool View3D_ObjectNormalsGet(HObject obj, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern void View3D_ObjectNormalsSet(HObject obj, bool show, [MarshalAs(UnmanagedType.LPStr)] string? name);

		// Set the texture/sampler for all nuggets of 'object' or its children. (See LdrObject::Apply)
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern void View3D_ObjectSetTexture(HObject obj, HTexture tex, [MarshalAs(UnmanagedType.LPStr)] string? name);
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern void View3D_ObjectSetSampler(HObject obj, HSampler sam, [MarshalAs(UnmanagedType.LPStr)] string? name);

		// Get/Set the nugget flags on an object or its children (See LdrObject::Apply)
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern ENuggetFlag View3D_ObjectNuggetFlagsGet(HObject obj, [MarshalAs(UnmanagedType.LPStr)] string? name, int index);
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern void View3D_ObjectNuggetFlagsSet(HObject obj, ENuggetFlag flags, bool state, [MarshalAs(UnmanagedType.LPStr)] string? name, int index);

		// Get/Set the tint colour for a nugget within the model of an object or its children. (See LdrObject::Apply)
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern Colour32 View3D_ObjectNuggetTintGet(HObject obj, [MarshalAs(UnmanagedType.LPStr)] string? name, int index);
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern void View3D_ObjectNuggetTintSet(HObject obj, Colour32 colour, [MarshalAs(UnmanagedType.LPStr)] string? name, int index);

		// Materials ******************************

		// Create a texture from data in memory.
		// Set 'data' to nullptr to leave the texture uninitialised, if not null then data must point to width x height pixel data
		// of the size appropriate for the given format. 'e.g. uint32_t px_data[width * height] for D3DFMT_A8R8G8B8'
		// Note: careful with stride, 'data' is expected to have the appropriate stride for BytesPerPixel(format) * width
		[DllImport(Dll)] private static extern HTexture View3D_TextureCreate(int width, int height, IntPtr data, int data_size, ref TextureOptions options);

		// Create one of the stock textures
		[DllImport(Dll)] private static extern HTexture View3D_TextureCreateStock(EStockTexture stock_texture);

		// Load a texture from file, embedded resource, or stock assets. Specify width == 0, height == 0 to use the dimensions of the file
		[DllImport(Dll)] private static extern HTexture View3D_TextureCreateFromUri([MarshalAs(UnmanagedType.LPStr)] string resource, int width, int height, ref TextureOptions options);

		// Load a cube map from file, embedded resource, or stock assets. Specify width == 0, height == 0 to use the dimensions of the file
		[DllImport(Dll)] private static extern HCubeMap View3D_CubeMapCreateFromUri([MarshalAs(UnmanagedType.LPStr)] string resource, ref CubeMapOptions options);

		// Create a texture sampler
		[DllImport(Dll)] private static extern HSampler View3D_SamplerCreate(ref SamplerOptions options);

		// Create one of the stock samplers
		[DllImport(Dll)] private static extern HSampler View3D_SamplerCreateStock(EStockSampler stock_sampler);

		// Create a shader
		[DllImport(Dll)] private static extern HSampler View3D_ShaderCreate(ref ShaderOptions options);

		// Create one of the stock shaders
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern HSampler View3D_ShaderCreateStock(EStockShader stock_shader, [MarshalAs(UnmanagedType.LPStr)] string config);

		// Read the properties of an existing texture
		[DllImport(Dll)] private static extern ImageInfo View3D_TextureGetInfo(HTexture tex);
		[DllImport(Dll)] private static extern EResult View3D_TextureGetInfoFromFile([MarshalAs(UnmanagedType.LPStr)] string tex_filepath, out ImageInfo info);

		// Release a reference to a resources
		[DllImport(Dll)] private static extern void View3D_TextureRelease(HTexture tex);
		[DllImport(Dll)] private static extern void View3D_CubeMapRelease(HCubeMap tex);
		[DllImport(Dll)] private static extern void View3D_SamplerRelease(HSampler sam);
		[DllImport(Dll)] private static extern void View3D_ShaderRelease(HShader shdr);

		// Resize this texture to 'size'
		[DllImport(Dll)] private static extern void View3D_TextureResize(HTexture tex, ulong width, uint height, ushort depth_or_array_len);

		// Return the ref count of 'tex'
		[DllImport(Dll)] private static extern ulong View3D_TextureRefCount(HTexture tex);

		// Get/Set the private data associated with 'guid' for 'tex'
		[DllImport(Dll)] private static extern void View3d_TexturePrivateDataGet(HTexture tex, Guid guid, ref uint size, IntPtr data);
		[DllImport(Dll)] private static extern void View3d_TexturePrivateDataSet(HTexture tex, Guid guid, uint size, IntPtr data);
		[DllImport(Dll)] private static extern void View3d_TexturePrivateDataIFSet(HTexture tex, Guid guid, IntPtr pointer);

		// Resolve a MSAA texture into a non-MSAA texture
		[DllImport(Dll)] private static extern void View3D_TextureResolveAA(HTexture dst, HTexture src);

		// Gizmos *********************************

		// Create the 3D manipulation gizmo
		[DllImport(Dll)] private static extern HGizmo View3D_GizmoCreate(Gizmo.EMode mode, ref m4x4 o2w);

		// Delete a 3D manipulation gizmo
		[DllImport(Dll)] private static extern void View3D_GizmoDelete(HGizmo gizmo);

		// Attach/Detach callbacks that are called when the gizmo moves
		[DllImport(Dll)] private static extern void View3D_GizmoMovedCBSet(HGizmo gizmo, Gizmo.Callback cb, IntPtr ctx, bool add);

		// Attach/Detach an object to the gizmo that will be moved as the gizmo moves
		[DllImport(Dll)] private static extern void View3D_GizmoAttach(HGizmo gizmo, HObject obj);
		[DllImport(Dll)] private static extern void View3D_GizmoDetach(HGizmo gizmo, HObject obj);

		// Get/Set the scale factor for the gizmo
		[DllImport(Dll)] private static extern float View3D_GizmoScaleGet(HGizmo gizmo);
		[DllImport(Dll)] private static extern void View3D_GizmoScaleSet(HGizmo gizmo, float scale);

		// Get/Set the current mode of the gizmo
		[DllImport(Dll)] private static extern Gizmo.EMode View3D_GizmoModeGet(HGizmo gizmo);
		[DllImport(Dll)] private static extern void View3D_GizmoModeSet(HGizmo gizmo, Gizmo.EMode mode);

		// Get/Set the object to world transform for the gizmo
		[DllImport(Dll)] private static extern m4x4 View3D_GizmoO2WGet(HGizmo gizmo);
		[DllImport(Dll)] private static extern void View3D_GizmoO2WSet(HGizmo gizmo, ref m4x4 o2w);

		// Get the offset transform that represents the difference between the gizmo's transform at the start of manipulation and now.
		[DllImport(Dll)] private static extern m4x4 View3D_GizmoOffsetGet(HGizmo gizmo);

		// Get/Set whether the gizmo is active to mouse interaction
		[DllImport(Dll)] private static extern bool View3D_GizmoEnabledGet(HGizmo gizmo);
		[DllImport(Dll)] private static extern void View3D_GizmoEnabledSet(HGizmo gizmo, bool enabled);

		// Returns true while manipulation is in progress
		[DllImport(Dll)] private static extern bool View3D_GizmoManipulating(HGizmo gizmo);

		// Diagnostics ****************************

		// Get/Set whether object bounding boxes are visible
		[DllImport(Dll)] private static extern bool View3D_DiagBBoxesVisibleGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_DiagBBoxesVisibleSet(HWindow window, bool visible);

		// Get/Set the length of the vertex normals
		[DllImport(Dll)] private static extern float View3D_DiagNormalsLengthGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_DiagNormalsLengthSet(HWindow window, float length);

		// Get/Set the colour of the vertex normals
		[DllImport(Dll)] private static extern Colour32 View3D_DiagNormalsColourGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_DiagNormalsColourSet(HWindow window, Colour32 colour);

		[DllImport(Dll)] private static extern v2 View3D_DiagFillModePointsSizeGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_DiagFillModePointsSizeSet(HWindow window, v2 size);

		// Miscellaneous **************************
		
		// Create a render target texture on a D3D9 device. Intended for WPF D3DImage
		[DllImport(Dll)] private static extern HTexture View3D_CreateDx9RenderTarget(HWND hwnd, uint width, uint height, ref TextureOptions options, out IntPtr shared_handle);

		// Create a Texture instance from a shared d3d resource (created on a different d3d device)
		[DllImport(Dll)] private static extern HTexture View3D_CreateTextureFromSharedResource(IntPtr shared_resource, ref TextureOptions options);

		// Return the supported MSAA quality for the given multi-sampling count
		[DllImport(Dll)] private static extern int View3D_MSAAQuality(int count, EFormat format);

		// Return true if the focus point is visible. Add/Remove the focus point to a window.
		[DllImport(Dll)] private static extern bool View3D_StockObjectVisibleGet(HWindow window, EStockObject stock_object);
		[DllImport(Dll)] private static extern void View3D_StockObjectVisibleSet(HWindow window, EStockObject stock_object, bool show);

		// Get/Set the size of the focus point
		[DllImport(Dll)] private static extern float View3D_FocusPointSizeGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_FocusPointSizeSet(HWindow window, float size);
		
		// Get/Set the size of the origin point
		[DllImport(Dll)] private static extern float View3D_OriginPointSizeGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_OriginPointSizeSet(HWindow window, float size);

		// Get/Set the position and size of the selection box
		[DllImport(Dll)] private static extern void View3D_SelectionBoxGet(HWindow window, out BBox box, out m4x4 o2w);
		[DllImport(Dll)] private static extern void View3D_SelectionBoxSet(HWindow window, ref BBox box, ref m4x4 o2w);

		// Set the selection box to encompass all selected objects
		[DllImport(Dll)] private static extern void View3D_SelectionBoxFitToSelected(HWindow window);

		// Create/Delete the demo scene in the given window
		[DllImport(Dll)] private static extern Guid View3D_DemoSceneCreate(HWindow window);
		[DllImport(Dll)] private static extern void View3D_DemoSceneDelete();
		
		// Show a window containing the demo script
		[DllImport(Dll)] private static extern void View3D_DemoScriptShow(HWindow window);

		// Return the example Ldr script as a BSTR
		[DllImport(Dll, CharSet = CharSet.Unicode)][return: MarshalAs(UnmanagedType.BStr)] private static extern string View3D_ExampleScriptBStr();

		// Return the auto complete templates as a BSTR
		[DllImport(Dll, CharSet = CharSet.Unicode)][return: MarshalAs(UnmanagedType.BStr)] private static extern string View3D_AutoCompleteTemplatesBStr();

		// Return the hierarchy "address" for a position in an ldr script file.
		[DllImport(Dll, CharSet = CharSet.Unicode)][return: MarshalAs(UnmanagedType.BStr)] private static extern string View3D_ObjectAddressAt([MarshalAs(UnmanagedType.LPWStr)] string ldr_script, long position);

		// Parse a transform description using the Ldr script syntax
		[DllImport(Dll, CharSet = CharSet.Ansi)] private static extern m4x4 View3D_ParseLdrTransform([MarshalAs(UnmanagedType.LPStr)] string ldr_script);

		// Handle standard keyboard shortcuts. 'key_code' should be a standard VK_ key code with modifiers included in the hi word. See 'EKeyCodes'
		[DllImport(Dll)] private static extern bool View3D_TranslateKey(HWindow window, EKeyCodes key_code);

		// Return the reference count of a COM interface
		[DllImport(Dll)] private static extern ulong View3D_RefCount(IntPtr pointer);

		// Tools **********************************

		// Show/Hide the object manager tool
		[DllImport(Dll)] private static extern bool View3D_ObjectManagerVisibleGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_ObjectManagerVisibleSet(HWindow window, bool show);

		// Show/Hide the script editor tool
		[DllImport(Dll)] private static extern bool View3D_ScriptEditorVisibleGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_ScriptEditorVisibleSet(HWindow window, bool show);

		// Show/Hide the measurement tool
		[DllImport(Dll)] private static extern bool View3D_MeasureToolVisibleGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_MeasureToolVisibleSet(HWindow window, bool show);

		// Show/Hide the angle measurement tool
		[DllImport(Dll)] private static extern bool View3D_AngleToolVisibleGet(HWindow window);
		[DllImport(Dll)] private static extern void View3D_AngleToolVisibleSet(HWindow window, bool show);

		// Show/Hide the lighting controls UI
		[DllImport(Dll)] private static extern void View3D_LightingControlsUI(HWindow window);

#if false

		// Camera
		[DllImport(Dll)]
		private static extern void View3D_CameraViewRectSet(HWindow window, float width, float height, float dist);
		[DllImport(Dll)]
		private static extern v2 View3D_ViewArea(HWindow window, float dist);

		// Materials
		[DllImport(Dll)]
		private static extern void View3D_TextureLoadSurface(HTexture tex, int level, [MarshalAs(UnmanagedType.LPWStr)] string tex_filepath, Rectangle[]? dst_rect, Rectangle[]? src_rect, EFilter filter, uint colour_key);
		[DllImport(Dll)]
		private static extern IntPtr View3D_TextureGetDC(HTexture tex, bool discard);
		[DllImport(Dll)]
		private static extern void View3D_TextureReleaseDC(HTexture tex);

		// Miscellaneous
		[DllImport(Dll)]
		private static extern void View3D_Flush();

		[DllImport(Dll)]
		private static extern HWND View3D_LdrEditorCreate(HWND parent);
		[DllImport(Dll)]
		private static extern void View3D_LdrEditorDestroy(HWND hwnd);
		[DllImport(Dll)]
		private static extern void View3D_LdrEditorCtrlInit(HWND scintilla_control, bool dark);
#endif
#endregion
	}
}
