using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Interop;
using pr.common;
using pr.extn;
using pr.maths;
using pr.util;
using pr.win32;
using HContext = System.IntPtr;
using HGizmo = System.IntPtr;
using HObject = System.IntPtr;
using HTexture = System.IntPtr;
using HWindow = System.IntPtr;
using HWND = System.IntPtr;
using HMODULE = System.IntPtr;

namespace pr.gfx
{
	/// <summary>.NET wrapper for View3D.dll</summary>
	public class View3d :IDisposable
	{
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
			Invalid   = 0,
			PointList = 1,
			LineList  = 2,
			LineStrip = 3,
			TriList   = 4,
			TriStrip  = 5,
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
		public enum EFilter :uint //D3D11_FILTER
		{
			D3D11_FILTER_MIN_MAG_MIP_POINT                          = 0,
			D3D11_FILTER_MIN_MAG_POINT_MIP_LINEAR                   = 0x1,
			D3D11_FILTER_MIN_POINT_MAG_LINEAR_MIP_POINT             = 0x4,
			D3D11_FILTER_MIN_POINT_MAG_MIP_LINEAR                   = 0x5,
			D3D11_FILTER_MIN_LINEAR_MAG_MIP_POINT                   = 0x10,
			D3D11_FILTER_MIN_LINEAR_MAG_POINT_MIP_LINEAR            = 0x11,
			D3D11_FILTER_MIN_MAG_LINEAR_MIP_POINT                   = 0x14,
			D3D11_FILTER_MIN_MAG_MIP_LINEAR                         = 0x15,
			D3D11_FILTER_ANISOTROPIC                                = 0x55,
			D3D11_FILTER_COMPARISON_MIN_MAG_MIP_POINT               = 0x80,
			D3D11_FILTER_COMPARISON_MIN_MAG_POINT_MIP_LINEAR        = 0x81,
			D3D11_FILTER_COMPARISON_MIN_POINT_MAG_LINEAR_MIP_POINT  = 0x84,
			D3D11_FILTER_COMPARISON_MIN_POINT_MAG_MIP_LINEAR        = 0x85,
			D3D11_FILTER_COMPARISON_MIN_LINEAR_MAG_MIP_POINT        = 0x90,
			D3D11_FILTER_COMPARISON_MIN_LINEAR_MAG_POINT_MIP_LINEAR = 0x91,
			D3D11_FILTER_COMPARISON_MIN_MAG_LINEAR_MIP_POINT        = 0x94,
			D3D11_FILTER_COMPARISON_MIN_MAG_MIP_LINEAR              = 0x95,
			D3D11_FILTER_COMPARISON_ANISOTROPIC                     = 0xd5,
			D3D11_FILTER_MINIMUM_MIN_MAG_MIP_POINT                  = 0x100,
			D3D11_FILTER_MINIMUM_MIN_MAG_POINT_MIP_LINEAR           = 0x101,
			D3D11_FILTER_MINIMUM_MIN_POINT_MAG_LINEAR_MIP_POINT     = 0x104,
			D3D11_FILTER_MINIMUM_MIN_POINT_MAG_MIP_LINEAR           = 0x105,
			D3D11_FILTER_MINIMUM_MIN_LINEAR_MAG_MIP_POINT           = 0x110,
			D3D11_FILTER_MINIMUM_MIN_LINEAR_MAG_POINT_MIP_LINEAR    = 0x111,
			D3D11_FILTER_MINIMUM_MIN_MAG_LINEAR_MIP_POINT           = 0x114,
			D3D11_FILTER_MINIMUM_MIN_MAG_MIP_LINEAR                 = 0x115,
			D3D11_FILTER_MINIMUM_ANISOTROPIC                        = 0x155,
			D3D11_FILTER_MAXIMUM_MIN_MAG_MIP_POINT                  = 0x180,
			D3D11_FILTER_MAXIMUM_MIN_MAG_POINT_MIP_LINEAR           = 0x181,
			D3D11_FILTER_MAXIMUM_MIN_POINT_MAG_LINEAR_MIP_POINT     = 0x184,
			D3D11_FILTER_MAXIMUM_MIN_POINT_MAG_MIP_LINEAR           = 0x185,
			D3D11_FILTER_MAXIMUM_MIN_LINEAR_MAG_MIP_POINT           = 0x190,
			D3D11_FILTER_MAXIMUM_MIN_LINEAR_MAG_POINT_MIP_LINEAR    = 0x191,
			D3D11_FILTER_MAXIMUM_MIN_MAG_LINEAR_MIP_POINT           = 0x194,
			D3D11_FILTER_MAXIMUM_MIN_MAG_MIP_LINEAR                 = 0x195,
			D3D11_FILTER_MAXIMUM_ANISOTROPIC                        = 0x1d5
		}
		public enum EAddrMode :uint //D3D11_TEXTURE_ADDRESS_MODE
		{
			D3D11_TEXTURE_ADDRESS_WRAP        = 1,
			D3D11_TEXTURE_ADDRESS_MIRROR      = 2,
			D3D11_TEXTURE_ADDRESS_CLAMP       = 3,
			D3D11_TEXTURE_ADDRESS_BORDER      = 4,
			D3D11_TEXTURE_ADDRESS_MIRROR_ONCE = 5,
		}
		[Flags] public enum EBindFlags :uint //D3D11_BIND_FLAG
		{
			NONE                        = 0,
			D3D11_BIND_VERTEX_BUFFER    = 0x1,
			D3D11_BIND_INDEX_BUFFER     = 0x2,
			D3D11_BIND_CONSTANT_BUFFER  = 0x4,
			D3D11_BIND_SHADER_RESOURCE  = 0x8,
			D3D11_BIND_STREAM_OUTPUT    = 0x10,
			D3D11_BIND_RENDER_TARGET    = 0x20,
			D3D11_BIND_DEPTH_STENCIL    = 0x40,
			D3D11_BIND_UNORDERED_ACCESS = 0x80,
			D3D11_BIND_DECODER          = 0x200,
			D3D11_BIND_VIDEO_ENCODER    = 0x400
		}
		[Flags] public enum EResMiscFlags :uint//D3D11_RESOURCE_MISC_FLAG
		{
			NONE                                                = 0,
			D3D11_RESOURCE_MISC_GENERATE_MIPS                   = 0x1,
			D3D11_RESOURCE_MISC_SHARED                          = 0x2,
			D3D11_RESOURCE_MISC_TEXTURECUBE                     = 0x4,
			D3D11_RESOURCE_MISC_DRAWINDIRECT_ARGS               = 0x10,
			D3D11_RESOURCE_MISC_BUFFER_ALLOW_RAW_VIEWS          = 0x20,
			D3D11_RESOURCE_MISC_BUFFER_STRUCTURED               = 0x40,
			D3D11_RESOURCE_MISC_RESOURCE_CLAMP                  = 0x80,
			D3D11_RESOURCE_MISC_SHARED_KEYEDMUTEX               = 0x100,
			D3D11_RESOURCE_MISC_GDI_COMPATIBLE                  = 0x200,
			D3D11_RESOURCE_MISC_SHARED_NTHANDLE                 = 0x800,
			D3D11_RESOURCE_MISC_RESTRICTED_CONTENT              = 0x1000,
			D3D11_RESOURCE_MISC_RESTRICT_SHARED_RESOURCE        = 0x2000,
			D3D11_RESOURCE_MISC_RESTRICT_SHARED_RESOURCE_DRIVER = 0x4000,
			D3D11_RESOURCE_MISC_GUARDED                         = 0x8000,
			D3D11_RESOURCE_MISC_TILE_POOL                       = 0x20000,
			D3D11_RESOURCE_MISC_TILED                           = 0x40000
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
		[Flags] public enum EBtn
		{
			None     = 0,
			Left     = 1 << 0,
			Right    = 1 << 1,
			Middle   = 1 << 2,
			XButton1 = 1 << 3,
			XButton2 = 1 << 4,
		}
		public enum EBtnIdx
		{
			Left     = 0,
			Right    = 1,
			Middle   = 2,
			XButton1 = 3,
			XButton2 = 4,
			None     = 5,
		}
		public enum ELogLevel
		{
			Debug,
			Info,
			Warn,
			Error,
		}
		[Flags] public enum EUpdateObject :int // Flags for partial update of a model
		{
			None        = 0     ,
			All         = ~0    ,
			Name        = 1 << 0,
			Model       = 1 << 1,
			Transform   = 1 << 2,
			Children    = 1 << 3,
			Colour      = 1 << 4,
			ColourMask  = 1 << 5,
			Wireframe   = 1 << 6,
			Visibility  = 1 << 7,
			Animation   = 1 << 8,
			StepData    = 1 << 9,
		}
		#endregion

		#region Structs

		[Serializable]
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

		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct Material
		{
			public IntPtr m_diff_tex;
			public IntPtr m_env_map;
		}

		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct Nugget
		{
			public EPrim m_topo;
			public EGeom m_geom;
			public uint m_v0, m_v1;    // Vertex buffer range. Set to 0,0 to mean the whole buffer
			public uint m_i0, m_i1;    // Index buffer range. Set to 0,0 to mean the whole buffer
			public Material m_mat;

			public Nugget(EPrim topo, EGeom geom)
				:this(topo, geom, 0, 0, 0, 0)
			{}
			public Nugget(EPrim topo, EGeom geom, uint v0, uint v1, uint i0, uint i1)
				:this(topo, geom, v1, v1, i0, i1, default(Material))
			{}
			public Nugget(EPrim topo, EGeom geom, uint v0, uint v1, uint i0, uint i1, Material mat)
			{
				m_topo = topo;
				m_geom = geom;
				m_v0   = v0;
				m_v1   = v1;
				m_i0   = i0;
				m_i1   = i1;
				m_mat  = mat;
			}
		}

		[Serializable]
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

		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct TextureOptions
		{
			public EFormat Format;
			public uint Mips;
			public EFilter Filter;
			public EAddrMode AddrU;
			public EAddrMode AddrV;
			public EBindFlags BindFlags;
			public EResMiscFlags MiscFlags;
			public uint ColourKey;
			public bool HasAlpha;
			public bool GdiCompatible;
			public string DbgName;

			public TextureOptions(bool gdi_compatible)
			{
				Format        = gdi_compatible ? EFormat.DXGI_FORMAT_B8G8R8A8_UNORM : EFormat.DXGI_FORMAT_R8G8B8A8_UNORM;
				Mips          = gdi_compatible ? 1U : 0U;
				Filter        = EFilter.D3D11_FILTER_MIN_MAG_MIP_LINEAR;
				AddrU         = EAddrMode.D3D11_TEXTURE_ADDRESS_CLAMP;
				AddrV         = EAddrMode.D3D11_TEXTURE_ADDRESS_CLAMP;
				BindFlags     = gdi_compatible ? EBindFlags.D3D11_BIND_SHADER_RESOURCE|EBindFlags.D3D11_BIND_RENDER_TARGET : EBindFlags.NONE;
				MiscFlags     = gdi_compatible ? EResMiscFlags.D3D11_RESOURCE_MISC_GDI_COMPATIBLE : EResMiscFlags.NONE;
				ColourKey     = 0;
				HasAlpha      = false;
				GdiCompatible = gdi_compatible;
				DbgName       = string.Empty;
			}
		}

		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct WindowOptions
		{
			public ReportErrorCB ErrorCB;
			public IntPtr ErrorCBCtx;
			public bool GdiCompatible;
			public int Multisampling;
			public string DbgName;
			public WindowOptions(bool gdi_compatible, ReportErrorCB error_cb, IntPtr error_cb_ctx)
			{
				ErrorCB       = error_cb;
				ErrorCBCtx    = error_cb_ctx;
				GdiCompatible = gdi_compatible;
				Multisampling = 4;
				DbgName       = string.Empty;
			}
		}

		/// <summary>Light source properties</summary>
		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct Light
		{
			public ELight   m_type;
			public bool     m_on;
			public v4       m_position;
			public v4       m_direction;
			public Colour32 m_ambient;
			public Colour32 m_diffuse;
			public Colour32 m_specular;
			public float    m_specular_power;
			public float    m_inner_cos_angle;
			public float    m_outer_cos_angle;
			public float    m_range;
			public float    m_falloff;
			public float    m_cast_shadow;

			/// <summary>Return properties for an ambient light source</summary>
			public static Light Ambient(Colour32 ambient)
			{
				return new Light
				{
					m_type           = ELight.Ambient,
					m_on             = true,
					m_position       = v4.Origin,
					m_direction      = v4.Zero,
					m_ambient        = ambient,
					m_diffuse        = Colour32.Zero,
					m_specular       = Colour32.Zero,
					m_specular_power = 0f,
					m_cast_shadow    = 0f,
				};
			}

			/// <summary>Return properties for a directional light source</summary>
			public static Light Directional(v4 direction, Colour32 ambient, Colour32 diffuse, Colour32 specular, float spec_power, float cast_shadow)
			{
				return new Light
				{
					m_type           = ELight.Directional,
					m_on             = true,
					m_position       = v4.Origin,
					m_direction      = direction,
					m_ambient        = ambient,
					m_diffuse        = diffuse,
					m_specular       = specular,
					m_specular_power = spec_power,
					m_cast_shadow    = cast_shadow,
				};
			}

			/// <summary>Return properties for a point light source</summary>
			public static Light Point(v4 position, Colour32 ambient, Colour32 diffuse, Colour32 specular, float spec_power, float cast_shadow)
			{
				return new Light
				{
					m_type           = ELight.Point,
					m_on             = true,
					m_position       = position,
					m_direction      = v4.Zero,
					m_ambient        = ambient,
					m_diffuse        = diffuse,
					m_specular       = specular,
					m_specular_power = spec_power,
					m_cast_shadow    = cast_shadow,
				};
			}
		}

		/// <summary>The viewport volume in render target space (i.e. screen coords, not normalised)</summary>
		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct Viewport
		{
			public float m_x;
			public float m_y;
			public float m_width;
			public float m_height;
			public float m_min_depth;
			public float m_max_depth;

			public Viewport(float x, float y, float w, float h, float min = 0f, float max = 1f) { m_x = x; m_y = y; m_width = w; m_height = h; m_min_depth = min; m_max_depth = max; }
			public Size ToSize() { return new Size((int)Math.Round(m_width), (int)Math.Round(m_height)); }
			public SizeF ToSizeF() { return new SizeF(m_width, m_height); }
			public Rectangle ToRect() { return new Rectangle((int)m_x, (int)m_y, (int)Math.Round(m_width), (int)Math.Round(m_height)); }
			public RectangleF ToRectF() { return new RectangleF(m_x, m_y, m_width, m_height); }
		}

		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct View3DIncludes
		{
			/// <summary>A comma or semicolon separated list of search directories</summary>
			public string m_include_paths;

			/// <summary>An array of binary modules that contain resources</summary>
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public IntPtr[] m_modules;
			public int m_module_count;

			/// <summary>Create an includes object. Remember module == IntPtr.Zero means "this" module</summary>
			public View3DIncludes(IEnumerable<string> paths = null, IEnumerable<HMODULE> modules = null)
			{
				m_include_paths = paths != null ? string.Join(",", paths) : null;
				m_modules = new HMODULE[16];
				m_module_count = 0;

				if (modules != null)
				{
					foreach (var m in modules.Take(m_modules.Length))
						m_modules[m_module_count++] = m;
				}
			}
			public View3DIncludes(HMODULE module, IEnumerable<string> paths = null)
				:this(paths, new[] { module })
			{}
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

		/// <summary>Report errors callback</summary>
		public delegate void ReportErrorCB(IntPtr ctx, string msg);

		/// <summary>Report settings changed callback</summary>
		public delegate void SettingsChangedCB(IntPtr ctx, HWindow wnd);

		/// <summary>Edit object callback</summary>
		public delegate void EditObjectCB(
			int vcount,
			int icount,
			int ncount,
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)][Out] Vertex[] verts,
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)][Out] ushort[] indices,
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex=2)][Out] Nugget[] nuggets,
			out int new_vcount,
			out int new_icount,
			out int new_ncount,
			IntPtr ctx);

		private readonly List<Window>  m_windows;  // Groups of objects to render
		private readonly HContext      m_context;  // Unique id per Initialise call

		public View3d()
		{
			if (!ModuleLoaded)
				throw new Exception("View3d.dll has not been loaded");

			m_windows = new List<Window>();

			// Initialise view3d
			string init_error = null;
			ReportErrorCB error_cb = (ctx,msg) => init_error = msg;
			m_context = View3D_Initialise(error_cb, IntPtr.Zero);
			if (m_context == HContext.Zero)
				throw new Exception(init_error ?? "Failed to initialised View3d");
		}
		public void Dispose()
		{
			while (m_windows.Count != 0)
				m_windows[0].Dispose();

			View3D_Shutdown(m_context);
		}

		/// <summary>Add a global error callback, returned object pops the error callback when disposed</summary>
		public static Scope PushGlobalErrorCB(ReportErrorCB error_cb)
		{
			return Scope.Create(
				() => View3D_PushGlobalErrorCB(error_cb, IntPtr.Zero),
				() => View3D_PopGlobalErrorCB(error_cb));
		}

		/// <summary>Convert a button enum to a button state int</summary>
		public static EBtn ButtonState(MouseButtons button)
		{
			EBtn state = EBtn.None;
			if (Bit.AllSet((uint)button, (uint)MouseButtons.Left    )) state |= EBtn.Left;
			if (Bit.AllSet((uint)button, (uint)MouseButtons.Right   )) state |= EBtn.Right;
			if (Bit.AllSet((uint)button, (uint)MouseButtons.Middle  )) state |= EBtn.Middle;
			if (Bit.AllSet((uint)button, (uint)MouseButtons.XButton1)) state |= EBtn.XButton1;
			if (Bit.AllSet((uint)button, (uint)MouseButtons.XButton2)) state |= EBtn.XButton2;
			return state;
		}

		/// <summary>Convert the forms MouseButtons enum into an index of first button that is down</summary>
		public static EBtnIdx ButtonIndex(MouseButtons button)
		{
			if (Bit.AllSet((uint)button, (uint)MouseButtons.Left    )) return EBtnIdx.Left;
			if (Bit.AllSet((uint)button, (uint)MouseButtons.Right   )) return EBtnIdx.Right;
			if (Bit.AllSet((uint)button, (uint)MouseButtons.Middle  )) return EBtnIdx.Middle;
			if (Bit.AllSet((uint)button, (uint)MouseButtons.XButton1)) return EBtnIdx.XButton1;
			if (Bit.AllSet((uint)button, (uint)MouseButtons.XButton2)) return EBtnIdx.XButton2;
			return EBtnIdx.None;
		}

		/// <summary>Binds a 3D scene to a window</summary>
		public class Window :IDisposable
		{
			private readonly View3d m_view;
			private readonly ReportErrorCB m_error_cb;        // A reference to prevent the GC from getting it
			private readonly SettingsChangedCB m_settings_cb; // A local reference to prevent the callback being garbage collected
			private HWindow m_wnd;

			public Window(View3d view, HWND hwnd, WindowOptions opts)
			{
				m_view = view;

				// Create a default error callback
				if (opts.ErrorCB == null)
					opts.ErrorCB = (c,m) => { throw new Exception(m); };
				m_error_cb = opts.ErrorCB;

				// Create the window
				m_wnd = View3D_CreateWindow(hwnd, ref opts);
				if (m_wnd == null)
					throw new Exception("Failed to create View3D window");

				// Set up a callback for when settings are changed
				m_settings_cb = (c,w) => OnSettingsChanged.Raise(this, EventArgs.Empty);
				View3D_SettingsChanged(m_wnd, m_settings_cb, IntPtr.Zero, true);

				// Set up the light source
				SetLightSource(v4.Origin, -v4.ZAxis, true);

				// Display the focus point
				FocusPointVisible = true;

				// Position the camera
				Camera = new CameraControls(this);
				Camera.SetPosition(new v4(0f, 0f, -2f, 1f), v4.Origin, v4.YAxis);
			}
			public void Dispose()
			{
				if (m_wnd != HWindow.Zero)
				{
					View3D_SettingsChanged(m_wnd, m_settings_cb, IntPtr.Zero, false);
					View3D_DestroyWindow(m_wnd);
					m_wnd = HWindow.Zero;
				}
			}

			/// <summary>Add an error callback. returned object pops the error callback when disposed</summary>
			public Scope PushErrorCB(ReportErrorCB error_cb)
			{
				return Scope.Create(
					() => View3D_PushErrorCB(m_wnd, error_cb, IntPtr.Zero),
					() => View3D_PopErrorCB(m_wnd, error_cb));
			}

			/// <summary>Event notifying whenever rendering settings have changed</summary>
			public event EventHandler OnSettingsChanged;

			/// <summary>Cause a redraw to happen the near future. This method can be called multiple times</summary>
			public void Invalidate()
			{
				View3D_Invalidate(m_wnd, false);
			}

			/// <summary>The associated view3d object</summary>
			public View3d View { get { return m_view; } }

			/// <summary>Get the native view3d handle for the window</summary>
			public IntPtr Handle { get { return m_wnd; } }

			/// <summary>Camera controls</summary>
			public CameraControls Camera { get; private set; }

			/// <summary>
			/// Mouse navigation and/or object manipulation.
			/// 'point' is a point in client rect space.
			/// 'mouse_btns' is the state of the mouse buttons (MK_LBUTTON etc)
			/// 'nav_start_or_end' should be true on mouse button down or up, and false during mouse movement
			/// Returns true if the scene requires refreshing</summary>
			public bool MouseNavigate(Point point, MouseButtons mouse_btns, bool nav_start_or_end)
			{
				// This function is not in the CameraControls object because it is not solely used
				// for camera navigation. It can also be used to manipulate objects in the scene.
				return View3D_MouseNavigate(Handle, v2.From(point), (int)ButtonState(mouse_btns), nav_start_or_end);
			}

			/// <summary>
			/// Direct camera relative navigation or manipulation.
			/// Returns true if the scene requires refreshing</summary>
			public bool Navigate(float dx, float dy, float dz)
			{
				// This function is not in the CameraControls object because it is not solely used
				// for camera navigation. It can also be used to manipulate objects in the scene.
				return View3D_Navigate(Handle, dx, dy, dz);
			}

			/// <summary>Get the render target texture</summary>
			public Texture RenderTarget
			{
				get { return new Texture(View3D_TextureRenderTarget(m_wnd)); }
			}

			/// <summary>Import/Export a settings string</summary>
			public string Settings
			{
				get { return Marshal.PtrToStringAnsi(View3D_GetSettings(m_wnd)); }
				set { View3D_SetSettings(m_wnd, value); }
			}

			/// <summary>Add an object to the window</summary>
			public void AddObject(Object obj)
			{
				View3D_AddObject(m_wnd, obj.m_handle);
			}

			/// <summary>Add a gizmo to the window</summary>
			public void AddGizmo(Gizmo giz)
			{
				View3D_AddGizmo(m_wnd, giz.m_handle);
			}

			/// <summary>Add multiple objects by context id</summary>
			public void AddObjects(Guid context_id)
			{
				View3D_AddObjectsById(m_wnd, ref context_id);
			}

			/// <summary>Add a collection of objects to the window</summary>
			public void AddObjects(IEnumerable<Object> objects)
			{
				foreach (var obj in objects)
					View3D_AddObject(m_wnd, obj.m_handle);
			}

			/// <summary>Remove an object from the window</summary>
			public void RemoveObject(Object obj)
			{
				View3D_RemoveObject(m_wnd, obj.m_handle);
			}

			/// <summary>Remove a gizmo from the window</summary>
			public void RemoveGizmo(Gizmo giz)
			{
				View3D_RemoveGizmo(m_wnd, giz.m_handle);
			}

			/// <summary>Remove multiple objects by context id</summary>
			public void RemoveObjects(bool all_except, Guid context_id)
			{
				View3D_RemoveObjectsById(m_wnd, all_except, ref context_id);
			}

			/// <summary>Remove a collection of objects from the window</summary>
			public void RemoveObjects(IEnumerable<Object> objects)
			{
				foreach (var obj in objects)
					View3D_RemoveObject(m_wnd, obj.m_handle);
			}

			/// <summary>Remove all instances from the window</summary>
			public void RemoveAllObjects()
			{
				View3D_RemoveAllObjects(m_wnd);
			}

			/// <summary>Return the number of objects in a window</summary>
			public int ObjectCount
			{
				get { return View3D_ObjectCount(m_wnd); }
			}

			/// <summary>True if 'obj' is a member of this window</summary>
			public bool HasObject(Object obj)
			{
				return View3D_HasObject(m_wnd, obj.m_handle);
			}

			/// <summary>Show/Hide the focus point</summary>
			public bool FocusPointVisible
			{
				get { return View3D_FocusPointVisible(m_wnd); }
				set { View3D_ShowFocusPoint(m_wnd, value); }
			}

			/// <summary>Set the size of the focus point graphic</summary>
			public float FocusPointSize
			{
				set { View3D_SetFocusPointSize(m_wnd, value); }
			}

			/// <summary>Show/Hide the origin point</summary>
			public bool OriginVisible
			{
				get { return View3D_OriginVisible(m_wnd); }
				set { View3D_ShowOrigin(m_wnd, value); }
			}

			/// <summary>Set the size of the origin graphic</summary>
			public float OriginSize
			{
				set { View3D_SetOriginSize(m_wnd, value); }
			}

			/// <summary>Get/Set the render mode</summary>
			public EFillMode FillMode
			{
				get { return View3D_FillMode(m_wnd); }
				set { View3D_SetFillMode(m_wnd, value); }
			}

			/// <summary>Get/Set the light properties. Note returned value is a value type</summary>
			public Light LightProperties
			{
				get { return View3D_LightProperties(m_wnd); }
				set { View3D_SetLightProperties(m_wnd, ref value); }
			}

			/// <summary>Show the lighting dialog</summary>
			public void ShowLightingDlg()
			{
				View3D_ShowLightingDlg(m_wnd);
			}

			/// <summary>Set the single light source</summary>
			public void SetLightSource(v4 position, v4 direction, bool camera_relative)
			{
				View3D_LightSource(m_wnd, position, direction, camera_relative);
			}

			/// <summary>Show/Hide the measuring tool</summary>
			public bool ShowMeasureTool
			{
				get { return View3D_MeasureToolVisible(m_wnd); }
				set { View3D_ShowMeasureTool(m_wnd, value); }
			}

			/// <summary>Show/Hide the angle tool</summary>
			public bool ShowAngleTool
			{
				get { return View3D_AngleToolVisible(m_wnd); }
				set { View3D_ShowAngleTool(m_wnd, value); }
			}

			/// <summary>Get/Set orthographic rendering mode</summary>
			public bool Orthographic
			{
				get { return View3D_Orthographic(m_wnd); }
				set { View3D_SetOrthographic(m_wnd, value); }
			}

			/// <summary>The background colour for the window</summary>
			public Color BackgroundColour
			{
				get { return Color.FromArgb(View3D_BackgroundColour(m_wnd)); }
				set { View3D_SetBackgroundColour(m_wnd, value.ToArgb()); }
			}

			/// <summary>Cause the window to be rendered. Remember to call Present when done</summary>
			public void Render()
			{
				View3D_Render(m_wnd);
			}

			/// <summary>Called to flip the back buffer to the screen after all window have been rendered</summary>
			public void Present()
			{
				View3D_Present(m_wnd);
			}

			/// <summary>Resize the render target</summary>
			public Size RenderTargetSize
			{
				get { int w,h; View3D_RenderTargetSize(m_wnd, out w, out h); return new Size(w,h); }
				set { View3D_SetRenderTargetSize(m_wnd, value.Width, value.Height); }
			}

			/// <summary>Restore the render target as the main output</summary>
			public void RestoreRT()
			{
				View3D_RestoreMainRT(m_wnd);
			}

			/// <summary>
			/// Render the current scene into 'render_target'. If no 'depth_buffer' is given a temporary one will be created.
			/// Note: Make sure the render target is not used as a texture for an object in the scene to be rendered.
			/// Either remove that object from the scene, or detach the texture from the object. 'render_target' cannot be
			/// a source and destination texture at the same time</summary>
			public void RenderTo(Texture render_target, Texture depth_buffer = null)
			{
				View3D_RenderTo(m_wnd, render_target.m_handle, depth_buffer != null ? depth_buffer.m_handle : IntPtr.Zero);
			}

			/// <summary>Get/Set the size/position of the viewport within the render target</summary>
			public Viewport Viewport
			{
				get { return View3D_Viewport(m_wnd); }
				set { View3D_SetViewport(m_wnd, value); }
			}

			/// <summary>Get/Set whether the depth buffer is enabled</summary>
			public bool DepthBufferEnabled
			{
				get { return View3D_DepthBufferEnabled(m_wnd); }
				set { View3D_SetDepthBufferEnabled(m_wnd, value); }
			}

			/// <summary>Convert a screen space point to a normalised point</summary>
			public v2 SSPointToNSSPoint(Point screen)
			{
				return View3D_SSPointToNSSPoint(m_wnd, v2.From(screen));
			}

			/// <summary>Convert a normalised point into a screen space point</summary>
			public v2 ScreenSpacePointF(v2 pt)
			{
				var da = RenderTargetSize;
				return new v2((pt.x + 1f) * da.Width / 2f, (1f - pt.y) * da.Height / 2f);
			}
			public Point ScreenSpacePoint(v2 pt)
			{
				var p = ScreenSpacePointF(pt);
				return new Point((int)Math.Round(p.x), (int)Math.Round(p.y));
			}

			/// <summary>Standard keyboard shortcuts</summary>
			public void TranslateKey(object sender, KeyEventArgs e)
			{
				if (View3D_TranslateKey(Handle, (int)e.KeyCode))
					e.Handled = true;
			}

			/// <summary>Example for creating objects</summary>
			public void CreateDemoScene()
			{
				View3D_CreateDemoScene(Handle);

				#if false
				{// Create an object using ldr script
				    HObject obj = ObjectCreate("*Box ldr_box FFFF00FF {1 2 3}");
				    DrawsetAddObject(obj);
				}

				{// Create a box model, an instance for it, and add it to the window
				    HModel model = CreateModelBox(new v4(0.3f, 0.2f, 0.4f, 0f), m4x4.Identity, 0xFFFF0000);
				    HInstance instance = CreateInstance(model, m4x4.Identity);
				    AddInstance(window, instance);
				}

				{// Create a mesh
				    // Mesh data
				    Vertex[] vert = new Vertex[]
				    {
				        new Vertex(new v4( 0f, 0f, 0f, 1f), v4.ZAxis, 0xFFFF0000, v2.Zero),
				        new Vertex(new v4( 0f, 1f, 0f, 1f), v4.ZAxis, 0xFF00FF00, v2.Zero),
				        new Vertex(new v4( 1f, 0f, 0f, 1f), v4.ZAxis, 0xFF0000FF, v2.Zero),
				    };
				    ushort[] face = new ushort[]
				    {
				        0, 1, 2
				    };

				    HModel model = CreateModel(vert.Length, face.Length, vert, face, EPrimType.D3DPT_TRIANGLELIST);
				    HInstance instance = CreateInstance(model, m4x4.Identity);
				    AddInstance(window, instance);
				}
				#endif
			}

			/// <summary>Show a window containing and example ldr script file</summary>
			public void ShowExampleScript()
			{
				View3D_ShowDemoScript(m_wnd);
			}

			/// <summary>Show/Hide the object manager UI</summary>
			public void ShowObjectManager(bool show)
			{
				View3D_ShowObjectManager(m_wnd, show);
			}
		}

		/// <summary>Namespace for the camera controls</summary>
		public class CameraControls
		{
			private readonly Window m_window;
			public CameraControls(Window window)
			{
				m_window = window;
			}

			/// <summary>Get/Set Orthographic projection mode</summary>
			public bool Orthographic
			{
				get { return View3D_CameraOrthographic(m_window.Handle); }
				set { View3D_CameraOrthographicSet(m_window.Handle, value); }
			}

			/// <summary>Return the world space size of the camera view area at 'dist' in front of the camera</summary>
			public v2 ViewArea(float dist)
			{
				return View3D_ViewArea(m_window.Handle, dist);
			}
			public v2 ViewArea()
			{
				return ViewArea(FocusDist);
			}

			/// <summary>Get/Set the camera align axis (camera up axis). Zero vector means no align axis is set</summary>
			public v4 AlignAxis
			{
				get { v4 up; View3D_CameraAlignAxis(m_window.Handle, out up); return up; }
				set { View3D_AlignCamera(m_window.Handle, value); }
			}

			/// <summary>Get/Set the camera view aspect ratio = Width/Height</summary>
			public float Aspect
			{
				get { return View3D_CameraAspect(m_window.Handle); }
				set { View3D_CameraSetAspect(m_window.Handle, value); }
			}

			/// <summary>Get/Set the camera horizontal field of view (in radians). Note aspect ratio is preserved, setting FovX changes FovY and visa versa</summary>
			public float FovX
			{
				get { return View3D_CameraFovX(m_window.Handle); }
				set { View3D_CameraSetFovX(m_window.Handle, value); }
			}

			/// <summary>Get/Set the camera vertical field of view (in radians). Note aspect ratio is preserved, setting FovY changes FovX and visa versa</summary>
			public float FovY
			{
				get { return View3D_CameraFovY(m_window.Handle); }
				set { View3D_CameraSetFovY(m_window.Handle, value); }
			}

			/// <summary>Set the camera near plane distance. Note aspect ratio is preserved, setting FovY changes FovX and visa versa</summary>
			public void SetClipPlanes(float near, float far, bool focus_relative)
			{
				View3D_CameraSetClipPlanes(m_window.Handle, near, far, focus_relative);
			}

			/// <summary>Get/Set the position of the camera focus point</summary>
			public v4 FocusPoint
			{
				get { v4 pos; View3D_GetFocusPoint(m_window.Handle, out pos); return pos; }
				set { View3D_SetFocusPoint(m_window.Handle, value); }
			}

			/// <summary>Get/Set the distance to the camera focus point</summary>
			public float FocusDist
			{
				get { return View3D_CameraFocusDistance(m_window.Handle); }
				set { View3D_CameraSetFocusDistance(m_window.Handle, value); }
			}

			/// <summary>Get/Set the camera to world transform. Note: use SetPosition to set the focus distance at the same time</summary>
			public m4x4 O2W
			{
				get { m4x4 c2w; View3D_CameraToWorld(m_window.Handle, out c2w); return c2w; }
				set { View3D_SetCameraToWorld(m_window.Handle, ref value); }
			}

			/// <summary>Set the camera to world transform and focus distance.</summary>
			public void SetPosition(v4 position, v4 lookat, v4 up)
			{
				View3D_PositionCamera(m_window.Handle, position, lookat, up);
			}

			/// <summary>Set the camera position such that it's still looking at the current focus point</summary>
			public void SetPosition(v4 position)
			{
				var up = AlignAxis;
				if (up.Length3Sq == 0f) up = v4.YAxis;
				SetPosition(position, FocusPoint, up);
			}

			/// <summary>Set the camera fields of view (H and V) and focus distance such that a rectangle (w/h) exactly fills the view</summary>
			public void SetView(float width, float height, float dist)
			{
				View3D_CameraSetViewRect(m_window.Handle, width, height, dist);
			}

			/// <summary>Move the camera to a position that can see the whole scene given camera directions 'forward' and 'up'</summary>
			public void ResetView(v4 forward, v4 up)
			{
				View3D_ResetView(m_window.Handle, forward, up);
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

			/// <summary>Reset the zoom factor to 1f</summary>
			public void ResetZoom()
			{
				View3D_ResetZoom(m_window.Handle);
			}

			/// <summary>
			/// Return a point in world space corresponding to a normalised screen space point.
			/// The x,y components of 'screen' should be in normalised screen space, i.e. (-1,-1)->(1,1)
			/// The z component should be the world space distance from the camera.</summary>
			public v4 NSSPointToWSPoint(v4 screen)
			{
				return View3D_NSSPointToWSPoint(m_window.Handle, screen);
			}
			public v4 SSPointToWSPoint(Point screen)
			{
				var nss = SSPointToNSSPoint(screen);
				return NSSPointToWSPoint(new v4(nss.x, nss.y, View3D_CameraFocusDistance(m_window.Handle), 1.0f));
			}

			/// <summary>Return the normalised screen space point corresponding to a screen space point</summary>
			public v2 SSPointToNSSPoint(Point screen)
			{
				return m_window.SSPointToNSSPoint(screen);
			}

			/// <summary>
			/// Return a point in normalised screen space corresponding to 'world'
			/// The returned 'z' component will be the world space distance from the camera.</summary>
			public v4 WSPointToNSSPoint(v4 world)
			{
				return View3D_WSPointToNSSPoint(m_window.Handle, world);
			}
			public Point WSPointToSSPoint(v4 world)
			{
				var nss = WSPointToNSSPoint(world);
				return m_window.ScreenSpacePoint(new v2(nss.x, nss.y));
			}
			public Point WSPointToSSPoint(v2 world)
			{
				return WSPointToSSPoint(new v4(world, 0, 1));
			}

			/// <summary>
			/// Return a screen space vector that is the world space line a->b
			/// projected onto the screen.</summary>
			public v2 WSVecToSSVec(v4 s, v4 e)
			{
				return v2.From(WSPointToSSPoint(e)) - v2.From(WSPointToSSPoint(s));
			}
			public v2 WSVecToSSVec(v2 s, v2 e)
			{
				return v2.From(WSPointToSSPoint(e)) - v2.From(WSPointToSSPoint(s));
			}

			/// <summary>
			/// Return a world space vector that is the screen space line a->b
			/// at the focus depth from the camera.</summary>
			public v4 SSVecToWSVec(Point s, Point e)
			{
				var nss_s = SSPointToNSSPoint(s);
				var nss_e = SSPointToNSSPoint(e);
				var z = View3D_CameraFocusDistance(m_window.Handle);
				return
					NSSPointToWSPoint(new v4(nss_e.x, nss_e.y, z, 1.0f)) -
					NSSPointToWSPoint(new v4(nss_s.x, nss_s.y, z, 1.0f));
			}

			/// <summary>
			/// Convert a screen space point into a position and direction in world space.
			/// 'screen' should be in normalised screen space, i.e. (-1,-1)->(1,1) (lower left to upper right)
			/// The z component of 'screen' should be the world space distance from the camera</summary>
			public void NSSPointToWSRay(v4 screen, out v4 ws_point, out v4 ws_direction)
			{
				View3D_NSSPointToWSRay(m_window.Handle, screen, out ws_point, out ws_direction);
			}
			public void SSPointToWSRay(Point screen, out v4 ws_point, out v4 ws_direction)
			{
				var nss = SSPointToNSSPoint(screen);
				NSSPointToWSRay(new v4(nss.x, nss.y, View3D_CameraFocusDistance(m_window.Handle), 1.0f), out ws_point, out ws_direction);
			}
		}

		/// <summary>Object resource wrapper</summary>
		public class Object :IDisposable
		{
			public HObject m_handle;
			private bool m_owned;
			public object Tag { get; set; }

			/// <summary>
			/// Create objects given in an ldr string or file.
			/// If multiple objects are created, the handle returned is to the first object only
			/// 'ldr_script' - an ldr string, or filepath to a file containing ldr script
			/// 'file' - TRUE if 'ldr_script' is a filepath, FALSE if 'ldr_script' is a string containing ldr script
			/// 'context_id' - the context id to create the LdrObjects with
			/// 'async' - if objects should be created by a background thread
			/// 'include_paths' - is a comma separated list of include paths to use to resolve #include directives (or nullptr)
			/// 'module' - if non-zero causes includes to be resolved from the resources in that module</summary>
			public Object()
				:this("*group{}", false)
			{}
			public Object(string ldr_script, bool file)
				:this(ldr_script, file, null)
			{}
			public Object(string ldr_script, bool file, View3DIncludes? includes)
				:this(ldr_script, file, Guid.Empty, false, includes)
			{}
			public Object(string ldr_script, bool file, Guid context_id, bool async, View3DIncludes? includes)
			{
				m_owned = true;
				var inc = includes ?? new View3DIncludes();
				m_handle = View3D_ObjectCreateLdr(ldr_script, file, ref context_id, async, ref inc);
				if (m_handle == HObject.Zero)
					throw new Exception("Failed to create object from script\r\n{0}".Fmt(ldr_script.Summary(100)));
			}

			/// <summary>Create from buffer</summary>
			public Object(string name, uint colour, int vcount, int icount, int ncount, Vertex[] verts, ushort[] indices, Nugget[] nuggets)
				:this(name, colour, vcount, icount, ncount, verts, indices, nuggets, Guid.Empty)
			{}
			public Object(string name, uint colour, int vcount, int icount, int ncount, Vertex[] verts, ushort[] indices, Nugget[] nuggets, Guid context_id)
			{
				m_owned = true;

				// Serialise the verts/indices to a memory buffer
				using (var vbuf = Marshal_.ArrayToPtr(verts))
				using (var ibuf = Marshal_.ArrayToPtr(indices))
				using (var nbuf = Marshal_.ArrayToPtr(nuggets))
				{
					m_handle = View3D_ObjectCreate(name, colour, vcount, icount, ncount, vbuf.Value.Ptr, ibuf.Value.Ptr, nbuf.Value.Ptr, ref context_id);
					if (m_handle == HObject.Zero) throw new System.Exception("Failed to create object '{0}' from provided buffers".Fmt(name));
				}
			}

			/// <summary>Create an object via callback</summary>
			public Object(string name, uint colour, int vcount, int icount, int ncount, EditObjectCB edit_cb)
				:this(name, colour, vcount, icount, ncount, edit_cb, Guid.Empty)
			{}
			public Object(string name, uint colour, int vcount, int icount, int ncount, EditObjectCB edit_cb, Guid context_id)
			{
				m_owned = true;
				m_handle = View3D_ObjectCreateEditCB(name, colour, vcount, icount, ncount, edit_cb, IntPtr.Zero, ref context_id);
				if (m_handle == HObject.Zero) throw new Exception("Failed to create object '{0}' via edit callback".Fmt(name));
			}

			/// <summary>Attach to an existing object handle</summary>
			private Object(HObject handle, bool owned)
			{
				m_owned = owned;
				m_handle = handle;
			}

			public virtual void Dispose()
			{
				if (m_handle == HObject.Zero) return;
				if (m_owned) View3D_ObjectDelete(m_handle);
				m_handle = HObject.Zero;
			}

			/// <summary>
			/// Create multiple objects from a source file and associate them with 'context_id'.
			/// 'include_paths' is a comma separate list of include paths to use to resolve #include directives (or nullptr)
			/// Note, these objects cannot be accessed other than by context id.
			/// This method is intended for creating static scenery</summary>
			public static void CreateFromFile(string ldr_filepath, string[] include_paths, Guid context_id, bool async)
			{
				var inc = new View3DIncludes();
				inc.m_include_paths = string.Join(",", include_paths ?? new string[0]);
				View3D_ObjectsCreateFromFile(ldr_filepath, ref context_id, async, ref inc);
			}
			public static void CreateFromFile(string ldr_filepath, string[] include_paths, bool async)
			{
				CreateFromFile(ldr_filepath, include_paths, Guid.Empty, async);
			}

			/// <summary>Delete all objects matching 'context_id'</summary>
			public static void DeleteAll(Guid context_id)
			{
				View3D_ObjectsDeleteById(ref context_id);
			}

			/// <summary>Change the model for this object</summary>
			public void UpdateModel(string ldr_script, EUpdateObject flags = EUpdateObject.All)
			{
				View3D_ObjectUpdate(m_handle, ldr_script, flags);
			}

			/// <summary>Modify the model of this object</summary>
			public void Edit(EditObjectCB edit_cb)
			{
				View3D_ObjectEdit(m_handle, edit_cb, IntPtr.Zero);
			}

			/// <summary>Get/Set the object to parent transform of the root object</summary>
			public m4x4 O2P
			{
				get { return View3D_ObjectGetO2P(m_handle, null); }
				set { View3D_ObjectSetO2P(m_handle, ref value, null); }
			}

			/// <summary>Get the model space bounding box of this object</summary>
			public BBox BBoxMS
			{
				get { return View3D_ObjectBBoxMS(m_handle); }
			}

			/// <summary>Return the object that is the immediate parent of this object</summary>
			public Object Parent
			{
				get { return new Object(View3D_ObjectGetParent(m_handle), false); }
			}

			/// <summary>Return a child object of this object</summary>
			public Object Child(string name)
			{
				return new Object(View3D_ObjectGetChild(m_handle, name), false);
			}

			/// <summary>
			/// Get/Set the object to world transform for this object or the first child object that matches 'name'.
			/// If 'name' is null, then the state of the root object is returned
			/// If 'name' begins with '#' then the remainder of the name is treated as a regular expression.
			/// Note, setting the o2w for a child object results in a transform that is relative to it's immediate parent</summary>
			public m4x4 GetO2W(string name = null)
			{
				return View3D_ObjectGetO2W(m_handle, name);
			}
			public void SetO2W(m4x4 o2w, string name = null)
			{
				View3D_ObjectSetO2W(m_handle, ref o2w, name);
			}

			/// <summary>
			/// Get/Set the object to parent transform for this object or any of its child objects that match 'name'.
			/// If 'name' is null, then the state change is applied to this object only
			/// If 'name' is "", then the state change is applied to this object and all children recursively
			/// Otherwise, the state change is applied to all child objects that match name.
			/// If 'name' begins with '#' then the remainder of the name is treated as a regular expression</summary>
			public m4x4 GetO2P(string name = null)
			{
				return View3D_ObjectGetO2P(m_handle, name);
			}
			public void SetO2P(m4x4 o2p, string name = null)
			{
				View3D_ObjectSetO2P(m_handle, ref o2p, name);
			}

			/// <summary>
			/// Get/Set the visibility of this object or any of its child objects that match 'name'.
			/// If 'name' is null, then the state change is applied to this object only
			/// If 'name' is "", then the state change is applied to this object and all children recursively
			/// Otherwise, the state change is applied to all child objects that match name.
			/// If 'name' begins with '#' then the remainder of the name is treated as a regular expression</summary>
			public bool GetVisible(string name = null)
			{
				return View3D_ObjectGetVisibility(m_handle, name);
			}
			public void SetVisible(bool vis, string name = null)
			{
				View3D_ObjectSetVisibility(m_handle, vis, name);
			}

			/// <summary>
			/// Get the colour of this object or the first child object that matches 'name'.
			/// 'base_colour', if true returns the objects base colour, if false, returns the current colour</summary>
			public uint GetColour(bool base_colour, string name = null)
			{
				return View3D_ObjectGetColour(m_handle, base_colour, name);
			}

			/// <summary>
			/// Set the colour of this object or any of its child objects that match 'name'.
			/// If 'name' is null, then the state change is applied to this object only
			/// If 'name' is "", then the state change is applied to this object and all children recursively
			/// Otherwise, the state change is applied to all child objects that match name.
			/// If 'name' begins with '#' then the remainder of the name is treated as a regular expression</summary>
			public void SetColour(uint colour, uint mask, string name = null)
			{
				View3D_ObjectSetColour(m_handle, colour, mask, name);
			}
			public void SetColour(uint colour, string name = null)
			{
				SetColour(colour, 0xFFFFFFFF, name);
			}
			public void SetColour(Color colour, uint mask, string name = null)
			{
				SetColour(unchecked((uint)colour.ToArgb()), mask, name);
			}
			public void SetColour(Color colour, string name = null)
			{
				SetColour(colour, 0xFFFFFFFF, name);
			}

			/// <summary>
			/// Reset the colour to the base colour for this object or any of its child objects that match 'name'.
			/// If 'name' is null, then the state change is applied to this object only
			/// If 'name' is "", then the state change is applied to this object and all children recursively
			/// Otherwise, the state change is applied to all child objects that match name.
			/// If 'name' begins with '#' then the remainder of the name is treated as a regular expression</summary>
			public void ResetColour(string name = null)
			{
				View3D_ObjectResetColour(m_handle, name);
			}

			/// <summary>
			/// Set the texture on this object or any of its child objects that match 'name'.
			/// If 'name' is null, then the state change is applied to this object only
			/// If 'name' is "", then the state change is applied to this object and all children recursively
			/// Otherwise, the state change is applied to all child objects that match name.
			/// If 'name' begins with '#' then the remainder of the name is treated as a regular expression</summary>
			public void SetTexture(Texture tex, string name = null)
			{
				View3D_ObjectSetTexture(m_handle, tex.m_handle, name);
			}
		}

		/// <summary>A 3D manipulation gizmo</summary>
		public class Gizmo :IDisposable
		{
			// Use:
			//  Create a gizmo, attach objects or other gizmos to it,
			//  add it to a window to make it visible, enable it to have
			//  it watch for mouse interaction. Moving the gizmo automatically
			//  moves attached objects as well. MouseControl of the gizmo
			//  is provided by the window it's been added to

			public enum EMode { Translate, Rotate, Scale, };
			public enum EEvent { StartManip, Moving, Commit, Revert };
			
			public HGizmo m_handle; // The handle to the gizmo
			private Callback m_cb;
			private bool m_owned;   // True if 'm_handle' was created with this class

			public Gizmo(EMode mode, m4x4 o2w)
			{
				m_handle = View3D_GizmoCreate(mode, ref o2w);
				if (m_handle == IntPtr.Zero) throw new Exception("View3D.Gizmo creation failed");
				m_owned = true;
				View3D_GizmoAttachCB(m_handle, m_cb = HandleGizmoMoved, IntPtr.Zero);
			}
			public Gizmo(HGizmo handle)
			{
				if (handle == IntPtr.Zero) throw new ArgumentNullException("handle");
				m_handle = handle;
				m_owned = false;
				View3D_GizmoAttachCB(m_handle, m_cb = HandleGizmoMoved, IntPtr.Zero);
			}
			public virtual void Dispose()
			{
				if (m_handle == HObject.Zero) return;
				View3D_GizmoDetachCB(m_handle, m_cb);
				if (m_owned) View3D_GizmoDelete(m_handle);
				m_cb = null;
				m_handle = HObject.Zero;
			}

			/// <summary>Get/Set whether the gizmo is looking for mouse interaction</summary>
			public bool Enabled
			{
				get { return View3D_GizmoEnabled(m_handle); }
				set { View3D_GizmoSetEnabled(m_handle, value); }
			}

			/// <summary>True while manipulation is in progress</summary>
			public bool Manipulating
			{
				get { return View3D_GizmoManipulating(m_handle); }
			}

			/// <summary>Get/Set the mode of the gizmo between translate, rotate, scale</summary>
			public EMode Mode
			{
				get { return View3D_GizmoGetMode(m_handle); }
				set { View3D_GizmoSetMode(m_handle, value); }
			}

			/// <summary>Get/Set the gizmo object to world transform (scale is allowed)</summary>
			public m4x4 O2W
			{
				get { return View3D_GizmoGetO2W(m_handle); }
				set { View3D_GizmoSetO2W(m_handle, ref value); }
			}

			/// <summary>
			/// Get the offset transform that represents the difference between the gizmo's
			/// transform at the start of manipulation and now</summary>
			public m4x4 Offset
			{
				get { return View3D_GizmoGetOffset(m_handle); }
			}

			/// <summary>Raised whenever the gizmo is manipulated</summary>
			public event EventHandler<MovedEventArgs> Moved;
			public class MovedEventArgs :EventArgs
			{
				/// <summary>The type of movement event this is</summary>
				public EEvent Type { get; private set; }

				[System.Diagnostics.DebuggerStepThrough] public MovedEventArgs(EEvent type)
				{
					Type = type;
				}
			}

			/// <summary>Attach an object directly to the gizmo that will move with it</summary>
			public void Attach(Object obj)
			{
				View3D_GizmoAttach(m_handle, obj.m_handle);
			}

			/// <summary>Detach an object from the gizmo</summary>
			public void Detach(Object obj)
			{
				View3D_GizmoDetach(m_handle, obj.m_handle);
			}

			/// <summary>Handle the callback from the native code for when the gizmo moves</summary>
			private void HandleGizmoMoved(HGizmo ctx, ref Evt_Gizmo args)
			{
				if (args.m_gizmo != m_handle) throw new Exception("Gizmo move event from a different gizmo instance received");
				Moved.Raise(this, new MovedEventArgs(args.m_state));
			}

			/// <summary>Callback function type and data from the native gizmo object</summary>
			internal delegate void Callback(IntPtr ctx, ref Evt_Gizmo args);
			[StructLayout(LayoutKind.Sequential)]
			internal struct Evt_Gizmo
			{
				public IntPtr m_gizmo;
				public EEvent m_state;
			}
		}

		/// <summary>Texture resource wrapper</summary>
		public class Texture :IDisposable
		{
			private bool m_owned;
			public HTexture m_handle;
			public ImageInfo m_info;
			public object Tag {get;set;}

			/// <summary>Create a texture from an existing texture resource</summary>
			internal Texture(HTexture handle)
			{
				m_owned = false;
				m_handle = handle;
				View3D_TextureGetInfo(m_handle, out m_info);
			}

			/// <summary>Construct an uninitialised texture</summary>
			public Texture(uint width, uint height)
				:this(width, height, IntPtr.Zero, 0, new TextureOptions(true))
			{}
			public Texture(uint width, uint height, TextureOptions options)
				:this(width, height, IntPtr.Zero, 0, options)
			{}
			public Texture(uint width, uint height, IntPtr data, uint data_size, TextureOptions options)
			{
				m_owned = true;
				m_handle = View3D_TextureCreate(width, height, data, data_size, ref options);
				if (m_handle == HTexture.Zero) throw new Exception("Failed to create {0}x{1} texture".Fmt(width,height));
				
				View3D_TextureGetInfo(m_handle, out m_info);
				View3D_TextureSetFilterAndAddrMode(m_handle, options.Filter, options.AddrU, options.AddrV);
			}

			/// <summary>Construct a texture from a file</summary>
			public Texture(string tex_filepath)
				:this(tex_filepath, 0, 0, new TextureOptions(true))
			{}
			public Texture(string tex_filepath, TextureOptions options)
				:this(tex_filepath, 0, 0, options)
			{}
			public Texture(string tex_filepath, uint width, uint height)
				:this(tex_filepath, width, height, new TextureOptions(true))
			{}
			public Texture(string tex_filepath, uint width, uint height, TextureOptions options)
			{
				m_owned = true;
				m_handle = View3D_TextureCreateFromFile(tex_filepath, width, height, options.Mips, options.Filter, options.Filter, options.ColourKey);
				if (m_handle == HTexture.Zero) throw new Exception("Failed to create texture from {0}".Fmt(tex_filepath));
				View3D_TextureGetInfo(m_handle, out m_info);
				View3D_TextureSetFilterAndAddrMode(m_handle, options.Filter, options.AddrU, options.AddrV);
			}
			
			public void Dispose()
			{
				if (m_handle != HTexture.Zero)
				{
					if (m_owned) View3D_TextureDelete(m_handle);
					m_handle = HTexture.Zero;
				}
			}

			/// <summary>Get/Set the texture size. Set does not preserve the texture content</summary>
			public Size Size
			{
				get { return new Size((int)m_info.m_width, (int)m_info.m_height); }
				set
				{
					if (Size == value) return;
					Resize((uint)value.Width, (uint)value.Height, false, false);
				}
			}
			
			/// <summary>Resize the texture optionally preserving content</summary>
			public void Resize(uint width, uint height, bool all_instances, bool preserve)
			{
				View3D_TextureResize(m_handle, width, height, all_instances, preserve);
				View3D_TextureGetInfo(m_handle, out m_info);
			}
			
			/// <summary>Set the filtering and addressing modes to be used on the texture</summary>
			public void SetFilterAndAddrMode(EFilter filter, EAddrMode addrU, EAddrMode addrV)
			{
				View3D_TextureSetFilterAndAddrMode(m_handle, filter, addrU, addrV);
			}

			/// <summary>Fill a surface of this texture from a file</summary>
			public void LoadSurface(string tex_filepath, int level)
			{
				View3D_TextureLoadSurface(m_handle, level, tex_filepath, null, null, EFilter.D3D11_FILTER_MIN_MAG_MIP_LINEAR, 0);
				View3D_TextureGetInfo(m_handle, out m_info);
			}

			/// <summary>Fill a surface of this texture from a file</summary>
			public void LoadSurface(string tex_filepath, int level, EFilter filter, uint colour_key)
			{
				View3D_TextureLoadSurface(m_handle, level, tex_filepath, null, null, filter, colour_key);
				View3D_TextureGetInfo(m_handle, out m_info);
			}

			/// <summary>Fill a surface of this texture from a file</summary>
			public void LoadSurface(string tex_filepath, int level, Rectangle src_rect, Rectangle dst_rect, EFilter filter, uint colour_key)
			{
				View3D_TextureLoadSurface(m_handle, level, tex_filepath, new []{dst_rect}, new []{src_rect}, filter, colour_key);
				View3D_TextureGetInfo(m_handle, out m_info);
			}
			
			/// <summary>Return properties of the texture</summary>
			public static ImageInfo GetInfo(string tex_filepath)
			{
				ImageInfo info;
				EResult res = View3D_TextureGetInfoFromFile(tex_filepath, out info);
				if (res != EResult.Success) throw new Exception(res);
				return info;
			}

			/// <summary>An RAII object used to lock the texture for drawing with GDI+ methods</summary>
			public class Lock :IDisposable
			{
				private readonly HTexture m_tex;

				/// <summary>GDI+ graphics interface</summary>
				public Graphics Gfx { get; private set; }

				/// <summary>
				/// Lock 'tex' making 'Gfx' available.
				/// Note: if 'tex' is the render target of a window, you need to call Window.RestoreRT when finished</summary>
				public Lock(Texture tex)
				{
					m_tex = tex.m_handle;
					var dc = View3D_TextureGetDC(m_tex);
					if (dc == IntPtr.Zero) throw new Exception("Failed to get Texture DC. Check the texture is a GdiCompatible texture");
					Gfx = Graphics.FromHdc(dc);
				}
				public void Dispose()
				{
					View3D_TextureReleaseDC(m_tex);
				}
			}

			/// <summary>Lock the texture for drawing on</summary>
			[DebuggerHidden]
			public Lock LockSurface()
			{
				// This is a method to prevent the debugger evaluating it cause multiple GetDC calls
				return new Lock(this);
			}

			public override bool Equals(object obj) { return obj is Texture && m_handle.Equals(((Texture)obj).m_handle); }
			public override int GetHashCode()       { return m_handle.GetHashCode(); }
		}

		/// <summary>An ldr script editor window.</summary>
		public class Editor :IDisposable
		{
			public Editor(IntPtr parent)
			{
				HWnd = View3D_LdrEditorCreate(parent);
				if (HWnd == IntPtr.Zero)
					throw new Exception("Failed to create editor window");
			}
			public void Dispose()
			{
				if (HWnd != IntPtr.Zero) View3D_LdrEditorDestroy(HWnd);
				HWnd = IntPtr.Zero;
			}

			/// <summary>The native window handle</summary>
			public HWND HWnd { get; private set; }

			public void Show()
			{
				Win32.ShowWindow(HWnd, Win32.SW_SHOW);
			}
		}

		/// <summary>
		/// An ldr script editor control. A very lightweight wrapper of a scintilla control.
		/// To use this in a WinForms application, create a System.Windows.Forms.Integration.ElementHost
		/// and assign an instance of this class to it's 'Child' property</summary>
		public class HostableEditor :HwndHost ,IKeyboardInputSink
		{
			// See: http://blogs.msdn.com/b/ivo_manolov/archive/2007/10/07/5354351.aspx
			private ElementHost m_host;
			private IntPtr m_wrap;
			private IntPtr m_ctrl;
			private uint m_ctrl_id;

			/// <summary>
			/// 'host' is the WPF control host that will contain this editor.
			/// 'dark' selects between the light and dark style for ldr script
			/// 'ctrl_id' sets the id of the control</summary>
			public HostableEditor(ElementHost host, bool dark, ushort ctrl_id = 0)
			{
				m_host = host;
				m_ctrl_id = (uint)ctrl_id;

				// Create the control at construction time so that the text content can be set before the control is displayed.
				m_wrap = Win32.CreateWindowEx(0, "static", "", Win32.WS_CHILD, 0, 0, 190, 190, m_host.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
				if (m_wrap == IntPtr.Zero)
					throw new Exception("Failed to create editor control. Error (0x{0:8X}) : {1}".Fmt(Win32.GetLastError(), Win32.GetLastErrorString()));

				m_ctrl = Win32.CreateWindowEx(0, "Scintilla", "", Win32.WS_CHILD|Win32.WS_VISIBLE|Win32.WS_HSCROLL|Win32.WS_VSCROLL, 0, 0, 190, 190, m_wrap, (IntPtr)m_ctrl_id, IntPtr.Zero, IntPtr.Zero);
				if (m_ctrl == IntPtr.Zero)
					throw new Exception("Failed to create editor control. Error (0x{0:8X}) : {1}".Fmt(Win32.GetLastError(), Win32.GetLastErrorString()));

				// Initialise the scintilla control in "Ldr mode"
				View3D_LdrEditorCtrlInit(m_ctrl, dark);

				m_host.Child = this;
			}
			protected override HandleRef BuildWindowCore(HandleRef parent)
			{
				Win32.SetParent(m_wrap, parent.Handle);
				return new HandleRef(this, m_wrap);
			}
			protected override void DestroyWindowCore(System.Runtime.InteropServices.HandleRef hwnd)
			{
				Win32.DestroyWindow(hwnd.Handle);
			}
			protected override IntPtr WndProc(HWND hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
			{
				handled = false;

				switch ((uint)msg)
				{
				// Since this is the WindowProc of the parent HWND, we need do extra work in order to 
				// resize the child control upon resize of the parent HWND.
				case Win32.WM_SIZE:
					{
						int width  = (int)Win32.LoWord(lParam.ToInt32());
						int height = (int)Win32.HiWord(lParam.ToInt32());
						Win32.SetWindowPos(m_ctrl, IntPtr.Zero, 0, 0, width, height, (uint)(Win32.SWP_NOACTIVATE | Win32.SWP_NOMOVE | Win32.SWP_NOZORDER));
						handled = true;
						break;
					}

				// Watch for edit notifications
				case Win32.WM_COMMAND:
					{
						var notif = Win32.HiWord(wParam.ToInt32());
						var id    = Win32.LoWord(wParam.ToInt32());
						if (notif == Win32.EN_CHANGE && id == m_ctrl_id)
							TextChanged.Raise(this);
						break;
					}

				case Win32.WM_DESTROY:
					{
						// Copy the control text before WM_DESTROY
						m_text = Text;
						break;
					}
				}
				
				if (handled)
					return IntPtr.Zero;
				else
					return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
			}

			#region IKeyboardInputSink
			bool IKeyboardInputSink.TranslateAccelerator(ref MSG msg, System.Windows.Input.ModifierKeys modifiers)
			{
				bool handled = false;
				if (msg.message == Win32.WM_KEYDOWN)
				{
					if (Win32.GetFocus() == m_ctrl)
					{
						// We want tabs, arrow keys, and return/enter when the control is focused
						if (msg.wParam == (IntPtr)Win32.VK_TAB ||
							msg.wParam == (IntPtr)Win32.VK_UP ||
							msg.wParam == (IntPtr)Win32.VK_DOWN ||
							msg.wParam == (IntPtr)Win32.VK_LEFT ||
							msg.wParam == (IntPtr)Win32.VK_RIGHT ||
							msg.wParam == (IntPtr)Win32.VK_RETURN)
						{
							Win32.SendMessage(m_ctrl, (uint)msg.message, msg.wParam, msg.lParam);
							handled = true;
						}
					}
				}
				return handled;
			}
			bool IKeyboardInputSink.TabInto(System.Windows.Input.TraversalRequest request)
			{
				Win32.SetFocus(m_ctrl);
				return true;
			}
			#endregion

			/// <summary>Read the text out of the control</summary>
			private string GetText()
			{
				var len = TextLength;
				using (var bytes = Marshal_.AllocHGlobal(len + 1))
				{
					var num = Win32.SendMessage(m_ctrl, pr.gui.Sci.SCI_GETTEXT, (IntPtr)(len + 1), bytes.Value.Ptr);
					return Marshal.PtrToStringAnsi(bytes.Value.Ptr, num);
				}
			}

			/// <summary>Set the text in the control</summary>
			private void SetText(string text)
			{
				if (!text.HasValue())
					ClearAll();
				else
				{
					using (var str = Marshal_.AllocAnsiString(text))
						Win32.SendMessage(m_ctrl, pr.gui.Sci.SCI_SETTEXT, IntPtr.Zero, str);
				}

				TextChanged.Raise(this);
			}

			/// <summary>Clear all text from the control</summary>
			public void ClearAll()
			{
				Win32.SendMessage(m_ctrl, pr.gui.Sci.SCI_CLEARALL, IntPtr.Zero, IntPtr.Zero);
			}

			/// <summary>Gets the length of the text in the control</summary>
			public int TextLength
			{
				get { return m_text != null ? m_text.Length : Win32.SendMessage(m_ctrl, pr.gui.Sci.SCI_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero); }
			}

			/// <summary>Gets or sets the current text</summary>
			public string Text
			{
				get { return m_text != null ? m_text : GetText(); }
				set { SetText(value); }
			}
			private string m_text;

			/// <summary>Raised when text in the control is changed</summary>
			public event EventHandler TextChanged;
		}

		#region DLL extern functions

		// A good idea is to add a static method in the class that is using this class
		// e.g.
		//  static MyThing()
		//  {
		//      View3d.LoadDll(@".\libs\$(platform)");
		//  }

		private const string Dll = "view3d";

		/// <summary>True if the view3d dll has been loaded</summary>
		public static bool ModuleLoaded { get { return m_module != IntPtr.Zero; } }
		private static IntPtr m_module = IntPtr.Zero;

		/// <summary>Helper method for loading the view3d.dll from a platform specific path</summary>
		public static void LoadDll(string dir = @".\lib\$(platform)")
		{
			if (ModuleLoaded) return;
			m_module = Win32.LoadDll(Dll+".dll", dir);
		}

		// Initialise / shutdown the dll
		[DllImport(Dll)] private static extern HContext          View3D_Initialise             (ReportErrorCB initialise_error_cb, IntPtr ctx);
		[DllImport(Dll)] private static extern void              View3D_Shutdown               (HContext context);
		[DllImport(Dll)] private static extern void              View3D_PushGlobalErrorCB      (ReportErrorCB error_cb, IntPtr ctx);
		[DllImport(Dll)] private static extern void              View3D_PopGlobalErrorCB       (ReportErrorCB error_cb);

		// Windows
		[DllImport(Dll)] private static extern HWindow           View3D_CreateWindow           (HWND hwnd, ref WindowOptions opts);
		[DllImport(Dll)] private static extern void              View3D_DestroyWindow          (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_PushErrorCB            (HWindow window, ReportErrorCB error_cb, IntPtr ctx);
		[DllImport(Dll)] private static extern void              View3D_PopErrorCB             (HWindow window, ReportErrorCB error_cb);
		[DllImport(Dll)] private static extern IntPtr            View3D_GetSettings            (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_SetSettings            (HWindow window, string settings);
		[DllImport(Dll)] private static extern void              View3D_SettingsChanged        (HWindow window, SettingsChangedCB settings_changed_cb, IntPtr ctx, bool add);
		[DllImport(Dll)] private static extern void              View3D_AddObject              (HWindow window, HObject obj);
		[DllImport(Dll)] private static extern void              View3D_RemoveObject           (HWindow window, HObject obj);
		[DllImport(Dll)] private static extern void              View3D_RemoveAllObjects       (HWindow window);
		[DllImport(Dll)] private static extern bool              View3D_HasObject              (HWindow window, HObject obj);
		[DllImport(Dll)] private static extern int               View3D_ObjectCount            (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_AddObjectsById         (HWindow window, ref Guid context_id);
		[DllImport(Dll)] private static extern void              View3D_RemoveObjectsById      (HWindow window, bool all_except, ref Guid context_id);
		[DllImport(Dll)] private static extern void              View3D_AddGizmo               (HWindow window, HGizmo giz);
		[DllImport(Dll)] private static extern void              View3D_RemoveGizmo            (HWindow window, HGizmo giz);

		// Camera
		[DllImport(Dll)] private static extern void              View3D_CameraToWorld          (HWindow window, out m4x4 c2w);
		[DllImport(Dll)] private static extern void              View3D_SetCameraToWorld       (HWindow window, ref m4x4 c2w);
		[DllImport(Dll)] private static extern void              View3D_PositionCamera         (HWindow window, v4 position, v4 lookat, v4 up);
		[DllImport(Dll)] private static extern bool              View3D_CameraOrthographic     (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_CameraOrthographicSet  (HWindow window, bool on);
		[DllImport(Dll)] private static extern float             View3D_CameraFocusDistance    (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_CameraSetFocusDistance (HWindow window, float dist);
		[DllImport(Dll)] private static extern void              View3D_CameraSetViewRect      (HWindow window, float width, float height, float dist);
		[DllImport(Dll)] private static extern float             View3D_CameraAspect           (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_CameraSetAspect        (HWindow window, float aspect);
		[DllImport(Dll)] private static extern float             View3D_CameraFovX             (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_CameraSetFovX          (HWindow window, float fovX);
		[DllImport(Dll)] private static extern float             View3D_CameraFovY             (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_CameraSetFovY          (HWindow window, float fovY);
		[DllImport(Dll)] private static extern void              View3D_CameraSetClipPlanes    (HWindow window, float near, float far, bool focus_relative);
		[DllImport(Dll)] private static extern bool              View3D_MouseNavigate          (HWindow window, v2 ss_point, int button_state, bool nav_start_or_end);
		[DllImport(Dll)] private static extern bool              View3D_Navigate               (HWindow window, float dx, float dy, float dz);
		[DllImport(Dll)] private static extern void              View3D_ResetZoom              (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_CameraAlignAxis        (HWindow window, out v4 axis);
		[DllImport(Dll)] private static extern void              View3D_AlignCamera            (HWindow window, v4 axis);
		[DllImport(Dll)] private static extern void              View3D_ResetView              (HWindow window, v4 forward, v4 up);
		[DllImport(Dll)] private static extern v2                View3D_ViewArea               (HWindow window, float dist);
		[DllImport(Dll)] private static extern void              View3D_GetFocusPoint          (HWindow window, out v4 position);
		[DllImport(Dll)] private static extern void              View3D_SetFocusPoint          (HWindow window, v4 position);
		[DllImport(Dll)] private static extern v2                View3D_SSPointToNSSPoint      (HWindow window, v2 screen);
		[DllImport(Dll)] private static extern v4                View3D_NSSPointToWSPoint      (HWindow window, v4 screen);
		[DllImport(Dll)] private static extern v4                View3D_WSPointToNSSPoint      (HWindow window, v4 world);
		[DllImport(Dll)] private static extern void              View3D_NSSPointToWSRay        (HWindow window, v4 screen, out v4 ws_point, out v4 ws_direction);

		// Lights
		[DllImport(Dll)] private static extern Light             View3D_LightProperties          (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_SetLightProperties       (HWindow window, ref Light light);
		[DllImport(Dll)] private static extern void              View3D_LightSource              (HWindow window, v4 position, v4 direction, bool camera_relative);
		[DllImport(Dll)] private static extern void              View3D_ShowLightingDlg          (HWindow window);

		// Objects
		[DllImport(Dll)] private static extern int               View3D_ObjectsCreateFromFile    (string ldr_filepath, ref Guid context_id, bool async, ref View3DIncludes includes);
		[DllImport(Dll)] private static extern HObject           View3D_ObjectCreateLdr          (string ldr_script, bool file, ref Guid context_id, bool async, ref View3DIncludes includes);
		[DllImport(Dll)] private static extern HObject           View3D_ObjectCreate             (string name, uint colour, int vcount, int icount, int ncount, IntPtr verts, IntPtr indices, IntPtr nuggets, ref Guid context_id);
		[DllImport(Dll)] private static extern HObject           View3D_ObjectCreateEditCB       (string name, uint colour, int vcount, int icount, int ncount, EditObjectCB edit_cb, IntPtr ctx, ref Guid context_id);
		[DllImport(Dll)] private static extern void              View3D_ObjectUpdate             (HObject obj, string ldr_script, EUpdateObject flags);
		[DllImport(Dll)] private static extern void              View3D_ObjectEdit               (HObject obj, EditObjectCB edit_cb, IntPtr ctx);
		[DllImport(Dll)] private static extern void              View3D_ObjectsDeleteAll         ();
		[DllImport(Dll)] private static extern void              View3D_ObjectsDeleteById        (ref Guid context_id);
		[DllImport(Dll)] private static extern void              View3D_ObjectDelete             (HObject obj);
		[DllImport(Dll)] private static extern HObject           View3D_ObjectGetParent          (HObject obj);
		[DllImport(Dll)] private static extern HObject           View3D_ObjectGetChild           (HObject obj, string name);
		[DllImport(Dll)] private static extern m4x4              View3D_ObjectGetO2W             (HObject obj, string name);
		[DllImport(Dll)] private static extern void              View3D_ObjectSetO2W             (HObject obj, ref m4x4 o2w, string name);
		[DllImport(Dll)] private static extern m4x4              View3D_ObjectGetO2P             (HObject obj, string name);
		[DllImport(Dll)] private static extern void              View3D_ObjectSetO2P             (HObject obj, ref m4x4 o2p, string name);
		[DllImport(Dll)] private static extern bool              View3D_ObjectGetVisibility      (HObject obj, string name);
		[DllImport(Dll)] private static extern void              View3D_ObjectSetVisibility      (HObject obj, bool visible, string name);
		[DllImport(Dll)] private static extern uint              View3D_ObjectGetColour          (HObject obj, bool base_colour, string name);
		[DllImport(Dll)] private static extern void              View3D_ObjectSetColour          (HObject obj, uint colour, uint mask, string name);
		[DllImport(Dll)] private static extern void              View3D_ObjectResetColour        (HObject obj, string name);
		[DllImport(Dll)] private static extern void              View3D_ObjectSetTexture         (HObject obj, HTexture tex, string name);
		[DllImport(Dll)] private static extern BBox              View3D_ObjectBBoxMS             (HObject obj);

		// Materials
		[DllImport(Dll)] private static extern HTexture          View3D_TextureCreate               (uint width, uint height, IntPtr data, uint data_size, ref TextureOptions options);
		[DllImport(Dll)] private static extern HTexture          View3D_TextureCreateFromFile       (string tex_filepath, uint width, uint height, uint mips, EFilter filter, EFilter mip_filter, uint colour_key);
		[DllImport(Dll)] private static extern void              View3D_TextureLoadSurface          (HTexture tex, int level, string tex_filepath, Rectangle[] dst_rect, Rectangle[] src_rect, EFilter filter, uint colour_key);
		[DllImport(Dll)] private static extern void              View3D_TextureDelete               (HTexture tex);
		[DllImport(Dll)] private static extern void              View3D_TextureGetInfo              (HTexture tex, out ImageInfo info);
		[DllImport(Dll)] private static extern EResult           View3D_TextureGetInfoFromFile      (string tex_filepath, out ImageInfo info);
		[DllImport(Dll)] private static extern void              View3D_TextureSetFilterAndAddrMode (HTexture tex, EFilter filter, EAddrMode addrU, EAddrMode addrV);
		[DllImport(Dll)] private static extern IntPtr            View3D_TextureGetDC                (HTexture tex);
		[DllImport(Dll)] private static extern void              View3D_TextureReleaseDC            (HTexture tex);
		[DllImport(Dll)] private static extern void              View3D_TextureResize               (HTexture tex, uint width, uint height, bool all_instances, bool preserve);
		[DllImport(Dll)] private static extern HTexture          View3D_TextureRenderTarget         (HWindow window);

		// Rendering
		[DllImport(Dll)] private static extern void              View3D_Invalidate               (HWindow window, bool erase);
		[DllImport(Dll)] private static extern void              View3D_InvalidateRect           (HWindow window, ref Win32.RECT rect, bool erase);
		[DllImport(Dll)] private static extern void              View3D_Render                   (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_Present                  (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_RenderTo                 (HWindow window, HTexture render_target, HTexture depth_buffer);
		[DllImport(Dll)] private static extern void              View3D_RenderTargetSize         (HWindow window, out int width, out int height);
		[DllImport(Dll)] private static extern void              View3D_SetRenderTargetSize      (HWindow window, int width, int height);
		[DllImport(Dll)] private static extern Viewport          View3D_Viewport                 (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_SetViewport              (HWindow window, Viewport vp);
		[DllImport(Dll)] private static extern EFillMode         View3D_FillMode                 (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_SetFillMode              (HWindow window, EFillMode mode);
		[DllImport(Dll)] private static extern bool              View3D_Orthographic             (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_SetOrthographic          (HWindow window, bool render2d);
		[DllImport(Dll)] private static extern int               View3D_BackgroundColour         (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_SetBackgroundColour      (HWindow window, int aarrggbb);

		// Tools
		[DllImport(Dll)] private static extern bool              View3D_MeasureToolVisible       (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_ShowMeasureTool          (HWindow window, bool show);
		[DllImport(Dll)] private static extern bool              View3D_AngleToolVisible         (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_ShowAngleTool            (HWindow window, bool show);

		// Gizmos
		[DllImport(Dll)] private static extern HGizmo            View3D_GizmoCreate              (Gizmo.EMode mode, ref m4x4 o2w);
		[DllImport(Dll)] private static extern void              View3D_GizmoDelete              (HGizmo gizmo);
		[DllImport(Dll)] private static extern void              View3D_GizmoAttachCB            (HGizmo gizmo, Gizmo.Callback cb, IntPtr ctx);
		[DllImport(Dll)] private static extern void              View3D_GizmoDetachCB            (HGizmo gizmo, Gizmo.Callback cb);
		[DllImport(Dll)] private static extern void              View3D_GizmoAttach              (HGizmo gizmo, HObject obj);
		[DllImport(Dll)] private static extern void              View3D_GizmoDetach              (HGizmo gizmo, HObject obj);
		[DllImport(Dll)] private static extern Gizmo.EMode       View3D_GizmoGetMode             (HGizmo gizmo);
		[DllImport(Dll)] private static extern void              View3D_GizmoSetMode             (HGizmo gizmo, Gizmo.EMode mode);
		[DllImport(Dll)] private static extern m4x4              View3D_GizmoGetO2W              (HGizmo gizmo);
		[DllImport(Dll)] private static extern void              View3D_GizmoSetO2W              (HGizmo gizmo, ref m4x4 o2w);
		[DllImport(Dll)] private static extern m4x4              View3D_GizmoGetOffset           (HGizmo gizmo);
		[DllImport(Dll)] private static extern bool              View3D_GizmoEnabled             (HGizmo gizmo);
		[DllImport(Dll)] private static extern void              View3D_GizmoSetEnabled          (HGizmo gizmo, bool enabled);
		[DllImport(Dll)] private static extern bool              View3D_GizmoManipulating        (HGizmo gizmo);

		// Miscellaneous
		[DllImport(Dll)] private static extern bool              View3D_TranslateKey             (HWindow window, int key_code);
		[DllImport(Dll)] private static extern void              View3D_RestoreMainRT            (HWindow window);
		[DllImport(Dll)] private static extern bool              View3D_DepthBufferEnabled       (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_SetDepthBufferEnabled    (HWindow window, bool enabled);
		[DllImport(Dll)] private static extern bool              View3D_FocusPointVisible        (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_ShowFocusPoint           (HWindow window, bool show);
		[DllImport(Dll)] private static extern void              View3D_SetFocusPointSize        (HWindow window, float size);
		[DllImport(Dll)] private static extern bool              View3D_OriginVisible            (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_ShowOrigin               (HWindow window, bool show);
		[DllImport(Dll)] private static extern void              View3D_SetOriginSize            (HWindow window, float size);
		[DllImport(Dll)] private static extern void              View3D_CreateDemoScene          (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_DeleteDemoScene          ();
		[DllImport(Dll)] private static extern void              View3D_ShowDemoScript           (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_ShowObjectManager        (HWindow window, bool show);
		[DllImport(Dll)] private static extern m4x4              View3D_ParseLdrTransform        (string ldr_script);

		[DllImport(Dll)] private static extern HWND              View3D_LdrEditorCreate          (HWND parent);
		[DllImport(Dll)] private static extern void              View3D_LdrEditorDestroy         (HWND hwnd);
		[DllImport(Dll)] private static extern void              View3D_LdrEditorCtrlInit        (HWND scintilla_control, bool dark);
		#endregion
	}
}
