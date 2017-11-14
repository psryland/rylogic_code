using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Xml.Linq;
using pr.common;
using pr.extn;
using pr.maths;
using pr.util;
using pr.win32;
using HContext = System.IntPtr;
using HGizmo = System.IntPtr;
using HMODULE = System.IntPtr;
using HObject = System.IntPtr;
using HTexture = System.IntPtr;
using HWindow = System.IntPtr;
using HWND = System.IntPtr;

namespace pr.gfx
{
	/// <summary>.NET wrapper for View3D.dll</summary>
	public class View3d :IDisposable
	{
		// Notes:
		// - Each process should create a single View3d instance that is an isolated context.
		// - Ldr objects are created and owned by the context.
		// - A View3d.Object is a reference to a specific object owned by the context.
		// - 
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
			PointSpritesGS,
			ThickLineListGS,
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
		public enum ECullMode
		{
			None,
			Back,
			Front,
		}
		[Flags] public enum ENavOp
		{
			None      = 0,
			Translate = 1 << 0,
			Rotate    = 1 << 1,
			Zoom      = 1 << 2,
		}
		[Flags] public enum ECameraLockMask
		{
			None           = 0,
			TransX         = 1 << 0,
			TransY         = 1 << 1,
			TransZ         = 1 << 2,
			RotX           = 1 << 3,
			RotY           = 1 << 4,
			RotZ           = 1 << 5,
			Zoom           = 1 << 6,
			CameraRelative = 1 << 7,
			All            = (1 << 7) - 1, // Not including camera relative
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
		[Flags] public enum EFlags
		{
			None = 0,

			// Set when an object is selected. The meaning of 'selected' is up to the application
			Selected = 1 << 0,

			// Doesn't contribute to the bounding box on an object.
			BBoxExclude = 1 << 1,

			// Should not be included when determining the bounds of a scene.
			SceneBoundsExclude = 1 << 2,

			All = ~0,
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
		[Flags] public enum EHitTestFlags
		{
			SnapToFaces = 1 << 0,
			SnapToEdges = 1 << 1,
			SnapToVerts = 1 << 2,
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

			public Vertex(v4 vert)                            { m_pos = vert; m_col = ~0U; m_norm = v4.Zero; m_uv = v2.Zero; pad = 0; }
			public Vertex(v4 vert, uint col)                  { m_pos = vert; m_col = col; m_norm = v4.Zero; m_uv = v2.Zero; pad = 0; }
			public Vertex(v4 vert, v4 norm, uint col, v2 tex) { m_pos = vert; m_col = col; m_norm = norm;    m_uv = tex;     pad = 0; }
			public override string ToString()                 { return "V:<{0}> C:<{1}>".Fmt(m_pos, m_col.ToString("X8")); }
		}

		[Serializable, StructLayout(LayoutKind.Sequential)]
		public struct Material
		{
			[DebuggerDisplay("{Description,nq}"), Serializable, StructLayout(LayoutKind.Sequential)] public struct ShaderSet
			{
				public ShaderSet(EShaderVS vs, EShaderGS gs, EShaderPS ps, EShaderCS cs)
				{
					m_vs = vs;
					m_gs = gs;
					m_ps = ps;
					m_cs = cs;
					m_vs_data = new byte[16];
					m_gs_data = new byte[16];
					m_ps_data = new byte[16];
					m_cs_data = new byte[16];
				}

				public EShaderVS m_vs;
				public EShaderGS m_gs;
				public EShaderPS m_ps;
				public EShaderCS m_cs;

				[MarshalAs(UnmanagedType.ByValArray, SizeConst=16)] public byte[] m_vs_data;
				[MarshalAs(UnmanagedType.ByValArray, SizeConst=16)] public byte[] m_gs_data;
				[MarshalAs(UnmanagedType.ByValArray, SizeConst=16)] public byte[] m_ps_data;
				[MarshalAs(UnmanagedType.ByValArray, SizeConst=16)] public byte[] m_cs_data;

				/// <summary>Description</summary>
				public string Description
				{
					get { return $"VS={m_vs} GS={m_gs} PS={m_ps} CS={m_cs}"; }
				}
			}
			[DebuggerDisplay("Smap"), Serializable, StructLayout(LayoutKind.Sequential)] public struct ShaderMap
			{
				public static ShaderMap New()
				{
					return new ShaderMap{ m_rstep = Array_.New((int)ERenderStep._number_of, i => new ShaderSet()) };
				}

				/// <summary>Get the shader set for the given render step</summary>
				[MarshalAs(UnmanagedType.ByValArray, SizeConst=(int)ERenderStep._number_of)]
				public ShaderSet[] m_rstep;
			}

			public static Material New()
			{
				return new Material(null);
			}
			public Material(HTexture? diff_tex = null, HTexture? env_map = null, ShaderMap? shdr_map = null)
			{
				m_diff_tex = diff_tex ?? HTexture.Zero;
				m_env_map  = env_map ?? HTexture.Zero;
				m_smap     = shdr_map ?? ShaderMap.New();
			}

			/// <summary>Material diffuse texture</summary>
			public HTexture m_diff_tex;

			/// <summary>Material environment map</summary>
			public HTexture m_env_map;

			/// <summary>Shader overrides</summary>
			public ShaderMap m_smap;
			// ? Replace this with a text description of the shader params?
			// It would make it human readable, byte order independent, backwards compatible,
			// smaller, variable size...

			/// <summary>Set the shader to use along with the parameters it requires</summary>
			public void Use(ERenderStep rstep, EShaderVS shdr, params object[] args)
			{
				m_smap.m_rstep[(int)rstep].m_vs = shdr;
				m_smap.m_rstep[(int)rstep].m_vs_data = GetData(args);
			}
			public void Use(ERenderStep rstep, EShaderGS shdr, params object[] args)
			{
				m_smap.m_rstep[(int)rstep].m_gs = shdr;
				m_smap.m_rstep[(int)rstep].m_gs_data = GetData(args);
			}
			public void Use(ERenderStep rstep, EShaderPS shdr, params object[] args)
			{
				m_smap.m_rstep[(int)rstep].m_ps = shdr;
				m_smap.m_rstep[(int)rstep].m_ps_data = GetData(args);
			}
			private byte[] GetData(params object[] args)
			{
				using (var ms = new MemoryStream())
				{
					Util.Serialise(ms, args);
					for (;ms.Length < 16;) ms.WriteByte(0);
					ms.Seek(0, SeekOrigin.Begin);
					return ms.ToArray();
				}
			}
		}

		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct Nugget
		{
			public EPrim m_topo;
			public EGeom m_geom;
			public uint m_v0, m_v1;       // Vertex buffer range. Set to 0,0 to mean the whole buffer
			public uint m_i0, m_i1;       // Index buffer range. Set to 0,0 to mean the whole buffer
			public bool m_has_alpha;      // True of the nugget contains transparent elements
			public bool m_range_overlaps; // True if the nugget V/I range overlaps earlier nuggets
			public Material m_mat;

			public Nugget(EPrim topo, EGeom geom)
				:this(topo, geom, false)
			{}
			public Nugget(EPrim topo, EGeom geom, bool has_alpha)
				:this(topo, geom, 0, 0, 0, 0, has_alpha, false, null)
			{}
			public Nugget(EPrim topo, EGeom geom, bool has_alpha, Material? mat)
				:this(topo, geom, 0, 0, 0, 0, has_alpha, false, mat)
			{}
			public Nugget(EPrim topo, EGeom geom, bool has_alpha, bool range_overlaps, Material? mat)
				:this(topo, geom, 0, 0, 0, 0, has_alpha, range_overlaps, mat)
			{}
			public Nugget(EPrim topo, EGeom geom, uint v0, uint v1, uint i0, uint i1)
				:this(topo, geom, v0, v1, i0, i1, false)
			{}
			public Nugget(EPrim topo, EGeom geom, uint v0, uint v1, uint i0, uint i1, bool has_alpha)
				:this(topo, geom, v0, v1, i0, i1, has_alpha, false, null)
			{}
			public Nugget(EPrim topo, EGeom geom, uint v0, uint v1, uint i0, uint i1, bool has_alpha, bool range_overlaps, Material? mat)
			{
				Debug.Assert(mat == null || mat.Value.m_smap.m_rstep != null, "Don't use default(Material)");
				m_topo           = topo;
				m_geom           = geom;
				m_v0             = v0;
				m_v1             = v1;
				m_i0             = i0;
				m_i1             = i1;
				m_has_alpha      = has_alpha;
				m_range_overlaps = range_overlaps;
				m_mat            = mat ?? new Material(null);
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
			public bool GdiCompatibleBackBuffer;
			public int Multisampling;
			public string DbgName;
			public WindowOptions(ReportErrorCB error_cb, IntPtr error_cb_ctx, bool gdi_compatible_bb = false)
			{
				ErrorCB                 = error_cb;
				ErrorCBCtx              = error_cb_ctx;
				GdiCompatibleBackBuffer = gdi_compatible_bb;
				Multisampling           = gdi_compatible_bb ? 1 : 4;
				DbgName                 = string.Empty;
			}
		}

		/// <summary>Light source properties</summary>
		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct LightInfo
		{
			public ELight   m_type;
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
			public bool     m_on;
			public bool     m_cam_relative;

			/// <summary>Return properties for an ambient light source</summary>
			public static LightInfo Ambient(Colour32 ambient)
			{
				return new LightInfo
				{
					m_type           = ELight.Ambient,
					m_position       = v4.Origin,
					m_direction      = v4.Zero,
					m_ambient        = ambient,
					m_diffuse        = Colour32.Zero,
					m_specular       = Colour32.Zero,
					m_specular_power = 0f,
					m_cast_shadow    = 0f,
					m_on             = true,
					m_cam_relative   = false,
				};
			}

			/// <summary>Return properties for a directional light source</summary>
			public static LightInfo Directional(v4 direction, Colour32 ambient, Colour32 diffuse, Colour32 specular, float spec_power, float cast_shadow)
			{
				return new LightInfo
				{
					m_type           = ELight.Directional,
					m_position       = v4.Origin,
					m_direction      = v4.Normalise3(direction),
					m_ambient        = ambient,
					m_diffuse        = diffuse,
					m_specular       = specular,
					m_specular_power = spec_power,
					m_cast_shadow    = cast_shadow,
					m_on             = true,
					m_cam_relative   = false,
				};
			}

			/// <summary>Return properties for a point light source</summary>
			public static LightInfo Point(v4 position, Colour32 ambient, Colour32 diffuse, Colour32 specular, float spec_power, float cast_shadow)
			{
				return new LightInfo
				{
					m_type           = ELight.Point,
					m_position       = position,
					m_direction      = v4.Zero,
					m_ambient        = ambient,
					m_diffuse        = diffuse,
					m_specular       = specular,
					m_specular_power = spec_power,
					m_cast_shadow    = cast_shadow,
					m_on             = true,
					m_cam_relative   = false,
				};
			}
		}

		/// <summary>A ray description for hit testing in a 3d scene</summary>
		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct HitTestRay
		{
			// The world space origin and direction of the ray (normalisation not required)
			public v4 m_ws_origin;
			public v4 m_ws_direction;
		}

		/// <summary>The result of a ray cast hit test in a 3d scene</summary>
		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct HitTestResult
		{
			// The origin and direction of the cast ray (in world space)
			public v4 m_ws_ray_origin;
			public v4 m_ws_ray_direction;

			// The intercept point (in world space)
			public v4 m_ws_intercept;

			// The object that was hit (or null)
			public Object HitObject
			{
				get { return IsHit ? new Object(m_obj) : null; }
			}
			private IntPtr m_obj;

			/// <summary>True if something was hit</summary>
			public bool IsHit
			{
				get { return m_obj != IntPtr.Zero; }
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
			[MarshalAs(UnmanagedType.LPWStr)] public string m_include_paths;

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

		private readonly List<Window> m_windows;              // Groups of objects to render
		private readonly HContext     m_context;              // Unique id per Initialise call
		private readonly Dispatcher   m_dispatcher;           // Thread marshaller
		private readonly int          m_thread_id;            // The main thread id
		private ReportErrorCB         m_error_cb;             // Reference to callback
		private AddFileProgressCB     m_add_file_progress_cb; // Reference to callback
		private SourcesChangedCB      m_sources_changed_cb;   // Reference to callback
		private EmbeddedCSHandler     m_embedded_cs_handler;  // Handler object for embedded C# code

		public View3d()
			:this(true)
		{}
		public View3d(bool bgra_compatibility)
		{
			if (!ModuleLoaded)
				throw new Exception("View3d.dll has not been loaded");

			m_windows = new List<Window>();
			m_dispatcher = Dispatcher.CurrentDispatcher;
			m_thread_id = Thread.CurrentThread.ManagedThreadId;

			// Initialise view3d
			string init_error = null;
			ReportErrorCB error_cb = (ctx, msg) => init_error = msg;
			m_context = View3D_Initialise(error_cb, IntPtr.Zero, bgra_compatibility);
			if (m_context == HContext.Zero)
				throw new Exception(init_error ?? "Failed to initialised View3d");

			// Attach the global error handler
			View3D_GlobalErrorCBSet(m_error_cb = HandleError, IntPtr.Zero, true);

			// Sign up for progress reports
			View3D_AddFileProgressCBSet(m_add_file_progress_cb = HandleAddFileProgress, IntPtr.Zero, true);

			// Sign up for notification of the sources changing
			View3D_SourcesChangedCBSet(m_sources_changed_cb = HandleSourcesChanged, IntPtr.Zero, true);

			// Install a C# embedded code handler
			m_embedded_cs_handler = new EmbeddedCSHandler(this);
		}
		public void Dispose()
		{
			Util.BreakIf(Util.IsGCFinalizerThread, "Disposing in the GC finaliser thread");

			// Unsubscribe
			Util.Dispose(ref m_embedded_cs_handler);
			View3D_SourcesChangedCBSet(m_sources_changed_cb, IntPtr.Zero, false);
			View3D_AddFileProgressCBSet(m_add_file_progress_cb, IntPtr.Zero, false);
			View3D_GlobalErrorCBSet(m_error_cb, IntPtr.Zero, false);

			while (m_windows.Count != 0)
				m_windows[0].Dispose();

			View3D_Shutdown(m_context);
		}

		/// <summary>Event call on errors. Note: can be called in a background thread context</summary>
		public event EventHandler<MessageEventArgs> Error;
		private void HandleError(IntPtr ctx, string msg)
		{
			if (m_thread_id != Thread.CurrentThread.ManagedThreadId)
				m_dispatcher.BeginInvoke(m_error_cb, ctx, msg);
			else
				Error.Raise(this, new MessageEventArgs(msg));
		}

		/// <summary>Progress update when a file is being parsed</summary>
		public event EventHandler<AddFileProgressEventArgs> AddFileProgress;
		private bool HandleAddFileProgress(IntPtr ctx, ref Guid context_id, string filepath, long foffset, bool complete)
		{
			var args = new AddFileProgressEventArgs(context_id, filepath, foffset, complete);
			AddFileProgress.Raise(this, args);
			return args.Cancel;
		}

		/// <summary>Event notifying whenever sources are loaded/reloaded</summary>
		public event EventHandler<SourcesChangedEventArgs> OnSourcesChanged;
		private void HandleSourcesChanged(IntPtr ctx, ESourcesChangedReason reason, bool before)
		{
			if (m_thread_id != Thread.CurrentThread.ManagedThreadId)
				m_dispatcher.BeginInvoke(m_sources_changed_cb, ctx, reason, before);
			else
				OnSourcesChanged.Raise(this, new SourcesChangedEventArgs(reason, before));
		}

		/// <summary>
		/// Create multiple objects from a source script file and store the script file in the collection of sources.
		/// The objects are created with the context Id that is returned from this function.
		/// Callers should then add objects to a window using AddObjectsById.
		/// 'include_paths' is a comma separate list of include paths to use to resolve #include directives (or nullptr)</summary>
		public Guid LoadScriptSource(string ldr_filepath, bool additional = false, string[] include_paths = null)
		{
			var inc = new View3DIncludes { m_include_paths = string.Join(",", include_paths ?? new string[0]) };
			return View3D_LoadScriptSource(ldr_filepath, additional, ref inc);
		}

		/// <summary>
		/// Add an ldr script string. This will create all objects declared in 'ldr_script'
		/// with context id 'context_id' if given, otherwise an id will be created.
		/// 'include_paths' is a comma separate list of include paths to use to resolve #include directives (or nullptr)
		/// This is different to 'LoadScriptSource' because if 'ldr_script' is a file it is not added to
		/// the collection of script sources. It's a one-off method to add objects</summary>
		public Guid LoadScript(string ldr_script, bool file, Guid? context_id, string[] include_paths = null)
		{
			var inc = new View3DIncludes { m_include_paths = string.Join(",", include_paths ?? new string[0]) };
			var ctx = context_id ?? Guid.NewGuid();
			return View3D_LoadScript(ldr_script, file, ref ctx, ref inc);
		}

		/// <summary>Force a reload of all script sources</summary>
		public void ReloadScriptSources()
		{
			View3D_ReloadScriptSources();
		}

		/// <summary>Poll for changed script source files, and reload any that have changed. (designed as a Timer.Tick handler)</summary>
		public void CheckForChangedSources(object sender = null, EventArgs args = null)
		{
			View3D_CheckForChangedSources();
		}

		/// <summary>
		/// Release all created objects
		/// *WARNING* Careful with this function, make sure all C# references to View3D objects have been set to null
		/// otherwise, disposing them will result in memory corruption</summary>
		public void DeleteAllObjects()
		{
			View3D_ObjectsDeleteAll();
		}

		/// <summary>Release all created objects with context id 'context_id'</summary>
		public void DeleteObjects(Guid context_id, bool all_except)
		{
			DeleteObjects(new []{ context_id }, all_except);
		}
		public void DeleteObjects(Guid[] context_ids, bool all_except)
		{
			View3D_ObjectsDeleteById(context_ids, context_ids.Length, all_except);
		}

		/// <summary>Return the context id for objects created from file 'filepath' (or null if 'filepath' is not an existing source)</summary>
		public Guid? ContextIdFromFilepath(string filepath)
		{
			Guid id;
			return View3D_ContextIdFromFilepath(filepath, out id) ? id : (Guid?)null;
		}

		/// <summary>Enumerate the guids in the store</summary>
		public void EnumGuids(Action<Guid> cb)
		{
			EnumGuids(guid => { cb(guid); return true; });
		}
		public void EnumGuids(Func<Guid, bool> cb)
		{
			View3D_SourceEnumGuids((c,guid) => cb(guid), IntPtr.Zero);
		}

		/// <summary>Return the example Ldr script</summary>
		public string ExampleScript
		{
			get { return View3D_ExampleScriptBStr(); }
		}

		/// <summary>Binds a 3D scene to a window</summary>
		public class Window :IDisposable
		{
			private readonly View3d m_view;
			private readonly WindowOptions m_opts;              // The options used to create the window (contains references to the user provided error cb)
			private readonly ReportErrorCB m_error_cb;          // A reference to prevent the GC from getting it
			private readonly SettingsChangedCB m_settings_cb;   // A local reference to prevent the callback being garbage collected
			private readonly RenderCB m_render_cb;              // A local reference to prevent the callback being garbage collected
			private readonly SceneChangedCB m_scene_changed_cb; // A local reference to prevent the callback being garbage collected
			private HWindow m_handle;

			public Window(View3d view, HWND hwnd, WindowOptions opts)
			{
				m_view = view;
				m_opts = opts;

				// Create the window
				m_handle = View3D_WindowCreate(hwnd, ref opts);
				if (m_handle == null)
					throw new Exception("Failed to create View3D window");

				// Attach the global error handler
				m_error_cb = (ctx, msg) => Error.Raise(this, new ErrorEventArgs(msg));
				View3D_WindowErrorCBSet(m_handle, m_error_cb, IntPtr.Zero, true);

				// Set up a callback for when settings are changed
				m_settings_cb = (c,w) => OnSettingsChanged.Raise(this, EventArgs.Empty);
				View3D_WindowSettingsChangedCB(m_handle, m_settings_cb, IntPtr.Zero, true);

				// Set up a callback for when a render is about to happen
				m_render_cb = (c,w) => OnRendering.Raise(this, EventArgs.Empty);
				View3D_WindowRenderingCB(m_handle, m_render_cb, IntPtr.Zero, true);

				// Set up a callback for when the object store for this window changes
				m_scene_changed_cb = (c,w,ids,cnt) => OnSceneChanged.Raise(this, new SceneChangedEventArgs(ids));
				View3d_WindowSceneChangedCB(m_handle, m_scene_changed_cb, IntPtr.Zero, true);

				// Set up the light source
				SetLightSource(v4.Origin, -v4.ZAxis, true);

				// Display the focus point
				FocusPointVisible = true;

				// Position the camera
				Camera = new Camera(this);
				Camera.SetPosition(new v4(0f, 0f, -2f, 1f), v4.Origin, v4.YAxis);
			}
			public void Dispose()
			{
				Util.BreakIf(Util.IsGCFinalizerThread, "Disposing in the GC finalizer thread");
				if (m_handle == HWindow.Zero) return;
				View3d_WindowSceneChangedCB(m_handle, m_scene_changed_cb, IntPtr.Zero, false);
				View3D_WindowRenderingCB(m_handle, m_render_cb, IntPtr.Zero, false);
				View3D_WindowSettingsChangedCB(m_handle, m_settings_cb, IntPtr.Zero, false);
				View3D_WindowErrorCBSet(m_handle, m_error_cb, IntPtr.Zero, false);
				View3D_WindowDestroy(m_handle);
				m_handle = HWindow.Zero;
			}

			/// <summary>Event call on errors. Note: can be called in a background thread context</summary>
			public event EventHandler<ErrorEventArgs> Error;

			/// <summary>Event notifying whenever rendering settings have changed</summary>
			public event EventHandler OnSettingsChanged;

			/// <summary>Event notifying whenever objects are added/removed from the scene (Not raised during Render however)</summary>
			public event EventHandler<SceneChangedEventArgs> OnSceneChanged;

			/// <summary>Event notifying of a render about to happen</summary>
			public event EventHandler OnRendering;

			/// <summary>Cause a redraw to happen the near future. This method can be called multiple times</summary>
			public void Invalidate()
			{
				View3D_Invalidate(m_handle, false);
			}

			/// <summary>The associated view3d object</summary>
			public View3d View
			{
				[DebuggerStepThrough]
				get { return m_view; }
			}

			/// <summary>Get the native view3d handle for the window</summary>
			public HWindow Handle
			{
				[DebuggerStepThrough]
				get { return m_handle; }
			}

			/// <summary>Camera controls</summary>
			public Camera Camera
			{
				[DebuggerStepThrough]
				get; private set;
			}

			/// <summary>
			/// Mouse navigation and/or object manipulation.
			/// 'point' is a point in client rect space.
			/// 'nav_op' is logical navigation operation to perform
			/// 'mouse_btns' is the state of the mouse buttons (MK_LBUTTON etc)
			/// 'nav_beg_or_end' should be true on mouse button down or up, and false during mouse movement
			/// Returns true if the scene requires refreshing</summary>
			public bool MouseNavigate(PointF point, ENavOp nav_op, bool nav_beg_or_end)
			{
				// This function is not in the CameraControls object because it is not solely used
				// for camera navigation. It can also be used to manipulate objects in the scene.
				if (m_in_mouse_navigate != 0) return false;
				using (Scope.Create(() => ++m_in_mouse_navigate, () => --m_in_mouse_navigate))
				{
					// Notify of navigating, allowing client code to make changes
					var args = new MouseNavigateEventArgs(point, nav_op, nav_beg_or_end);
					MouseNavigating.Raise(this, args);

					return View3D_MouseNavigate(Handle, v2.From(args.Point), args.NavOp, args.NavBegOrEnd);
				}
			}
			public bool MouseNavigate(PointF point, MouseButtons btn, bool nav_beg_or_end)
			{
				var op = Camera.MouseBtnToNavOp(btn);
				return MouseNavigate(point, op, nav_beg_or_end);
			}
			private int m_in_mouse_navigate;

			/// <summary>
			/// Zoom using the mouse.
			/// 'point' is a point in client rect space.
			/// 'delta' is the mouse wheel scroll delta value (i.e. 120 = 1 click)
			/// 'along_ray' is true if the camera should move along a ray cast through 'point'
			/// Returns true if the scene requires refreshing</summary>
			public bool MouseNavigateZ(PointF point, float delta, bool along_ray)
			{
				if (m_in_mouse_navigate != 0) return false;
				using (Scope.Create(() => ++m_in_mouse_navigate, () => --m_in_mouse_navigate))
				{
					// Notify of navigating, allowing client code to make changes
					var args = new MouseNavigateEventArgs(point, delta, along_ray);
					MouseNavigating.Raise(this, args);

					return View3D_MouseNavigateZ(Handle, v2.From(args.Point), args.Delta, args.AlongRay);
				}
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

			/// <summary>Raised just before a mouse navigation happens</summary>
			public event EventHandler<MouseNavigateEventArgs> MouseNavigating;

			/// <summary>Perform a hit test in the scene</summary>
			public HitTestResult HitTest(HitTestRay ray, float snap_distance, EHitTestFlags flags)
			{
				View3D_WindowHitTest(Handle, ref ray, snap_distance, flags, out HitTestResult hit);
				return hit;
			}

			/// <summary>Get the render target texture</summary>
			public Texture RenderTarget
			{
				get { return new Texture(View3D_TextureRenderTarget(m_handle)); }
			}

			/// <summary>Import/Export a settings string</summary>
			public string Settings
			{
				get { return Marshal.PtrToStringAnsi(View3D_WindowSettingsGet(m_handle)); }
				set { View3D_WindowSettingsSet(m_handle, value); }
			}

			/// <summary>Enumerate the guids associated with this window</summary>
			public void EnumGuids(Action<Guid> cb)
			{
				EnumGuids(guid => { cb(guid); return true; });
			}
			public void EnumGuids(Func<Guid, bool> cb)
			{
				View3D_WindowEnumGuids(m_handle, (c,guid) => cb(guid), IntPtr.Zero);
			}

			/// <summary>Enumerate the objects associated with this window</summary>
			public void EnumObjects(Action<Object> cb)
			{
				EnumObjects(obj => { cb(obj); return true; });
			}
			public void EnumObjects(Func<Object, bool> cb)
			{
				View3D_WindowEnumObjects(m_handle, (c,obj) => cb(new Object(obj)), IntPtr.Zero);
			}
			public void EnumObjects(Action<Object> cb, Guid context_id, bool all_except = false)
			{
				EnumObjects(obj => { cb(obj); return true; }, context_id, all_except);
			}
			public void EnumObjects(Func<Object, bool> cb, Guid context_id, bool all_except = false)
			{
				EnumObjects(cb, new [] { context_id }, all_except);
			}
			public void EnumObjects(Action<Object> cb, Guid[] context_id, bool all_except = false)
			{
				EnumObjects(obj => { cb(obj); return true; }, context_id, all_except);
			}
			public void EnumObjects(Func<Object, bool> cb, Guid[] context_id, bool all_except = false)
			{
				View3D_WindowEnumObjectsById(m_handle, (c,obj) => cb(new Object(obj)), IntPtr.Zero, context_id, context_id.Length, all_except);
			}

			/// <summary>Return the objects associated with this window</summary>
			public Object[] Objects
			{
				get
				{
					var objs = new List<Object>();
					EnumObjects(x => objs.Add(x));
					return objs.ToArray();
				}
			}

			/// <summary>Suspend scene changed events</summary>
			public Scope SuspendSceneChanged()
			{
				return Scope.Create(
					() => View3D_WindowSceneChangedSuspend(m_handle, true),
					() => View3D_WindowSceneChangedSuspend(m_handle, false));
			}

			/// <summary>Add an object to the window</summary>
			public void AddObject(Object obj)
			{
				View3D_WindowAddObject(m_handle, obj.m_handle);
			}

			/// <summary>Add a gizmo to the window</summary>
			public void AddGizmo(Gizmo giz)
			{
				View3D_WindowAddGizmo(m_handle, giz.m_handle);
			}

			/// <summary>Add multiple objects by context id</summary>
			public void AddObjects(Guid context_id, bool all_except = false)
			{
				AddObjects(new[]{ context_id });
			}
			public void AddObjects(Guid[] context_ids, bool all_except = false)
			{
				View3D_WindowAddObjectsById(m_handle, context_ids, context_ids.Length, all_except);
			}

			/// <summary>Add a collection of objects to the window</summary>
			public void AddObjects(IEnumerable<Object> objects)
			{
				foreach (var obj in objects)
					View3D_WindowAddObject(m_handle, obj.m_handle);
			}

			/// <summary>Remove an object from the window</summary>
			public void RemoveObject(Object obj)
			{
				View3D_WindowRemoveObject(m_handle, obj.m_handle);
			}

			/// <summary>Remove a gizmo from the window</summary>
			public void RemoveGizmo(Gizmo giz)
			{
				View3D_WindowRemoveGizmo(m_handle, giz.m_handle);
			}

			/// <summary>Remove multiple objects by context id</summary>
			public void RemoveObjects(Guid context_id, bool all_except = false)
			{
				RemoveObjects(new []{ context_id }, all_except);
			}
			public void RemoveObjects(Guid[] context_ids, bool all_except = false)
			{
				View3D_WindowRemoveObjectsById(m_handle, context_ids, context_ids.Length, all_except);
			}

			/// <summary>Remove a collection of objects from the window</summary>
			public void RemoveObjects(IEnumerable<Object> objects)
			{
				foreach (var obj in objects)
					View3D_WindowRemoveObject(m_handle, obj.m_handle);
			}

			/// <summary>Remove all instances from the window</summary>
			public void RemoveAllObjects()
			{
				View3D_WindowRemoveAllObjects(m_handle);
			}

			/// <summary>Return the number of objects in a window</summary>
			public int ObjectCount
			{
				get { return View3D_WindowObjectCount(m_handle); }
			}

			/// <summary>True if 'obj' is a member of this window</summary>
			public bool HasObject(Object obj)
			{
				return View3D_WindowHasObject(m_handle, obj.m_handle);
			}

			/// <summary>Return a bounding box of the objects in this window</summary>
			public BBox SceneBounds(ESceneBounds bounds, Guid[] except = null)
			{
				return View3D_WindowSceneBounds(m_handle, bounds, except?.Length ?? 0, except);
			}

			/// <summary>Show/Hide the focus point</summary>
			public bool FocusPointVisible
			{
				get { return View3D_FocusPointVisibleGet(m_handle); }
				set { View3D_FocusPointVisibleSet(m_handle, value); }
			}

			/// <summary>Set the size of the focus point graphic</summary>
			public float FocusPointSize
			{
				set { View3D_FocusPointSizeSet(m_handle, value); }
			}

			/// <summary>Show/Hide the origin point</summary>
			public bool OriginPointVisible
			{
				get { return View3D_OriginVisibleGet(m_handle); }
				set { View3D_OriginVisibleSet(m_handle, value); }
			}

			/// <summary>Set the size of the origin graphic</summary>
			public float OriginPointSize
			{
				set { View3D_OriginSizeSet(m_handle, value); }
			}

			/// <summary>Get/Set whether object bounding boxes are visible</summary>
			public bool BBoxesVisible
			{
				get { return View3D_BBoxesVisibleGet(m_handle); }
				set { View3D_BBoxesVisibleSet(m_handle, value); }
			}

			/// <summary>Get/Set whether the selection box is visible</summary>
			public bool SelectionBoxVisible
			{
				get { return View3D_SelectionBoxVisibleGet(m_handle); }
				set { View3D_SelectionBoxVisibleSet(m_handle, value); }
			}

			/// <summary>Set the position of the selection box</summary>
			public void SelectionBoxPosition(BBox box, m4x4 o2w)
			{
				View3D_SelectionBoxPosition(m_handle, ref box, ref o2w);
			}

			/// <summary>Set the size and position of the selection box to bound the selected objects in this view</summary>
			public void SelectionBoxFitToSelected()
			{
				View3D_SelectionBoxFitToSelected(m_handle);
			}

			/// <summary>Get/Set the render mode</summary>
			public EFillMode FillMode
			{
				get { return View3D_FillModeGet(m_handle); }
				set { View3D_FillModeSet(m_handle, value); }
			}

			/// <summary>Get/Set the face culling mode</summary>
			public ECullMode CullMode
			{
				get { return View3D_CullModeGet(m_handle); }
				set { View3D_CullModeSet(m_handle, value); }
			}

			/// <summary>Get/Set the multi-sampling level for the window</summary>
			public int MultiSampling
			{
				get { return View3D_MultiSamplingGet(m_handle); }
				set { View3D_MultiSamplingSet(m_handle, value); }
			}

			/// <summary>Get/Set the light properties. Note returned value is a value type</summary>
			public LightInfo LightProperties
			{
				get { LightInfo light; View3D_LightProperties(m_handle, out light); return light; }
				set { View3D_SetLightProperties(m_handle, ref value); }
			}

			/// <summary>Show the lighting dialog</summary>
			public void ShowLightingDlg()
			{
				View3D_ShowLightingDlg(m_handle);
			}

			/// <summary>Set the single light source</summary>
			public void SetLightSource(v4 position, v4 direction, bool camera_relative)
			{
				View3D_LightSource(m_handle, position, direction, camera_relative);
			}

			/// <summary>Show/Hide the measuring tool</summary>
			public bool ShowMeasureTool
			{
				get { return View3D_MeasureToolVisible(m_handle); }
				set { View3D_ShowMeasureTool(m_handle, value); }
			}

			/// <summary>Show/Hide the angle tool</summary>
			public bool ShowAngleTool
			{
				get { return View3D_AngleToolVisible(m_handle); }
				set { View3D_ShowAngleTool(m_handle, value); }
			}

			/// <summary>Get/Set orthographic rendering mode</summary>
			public bool Orthographic
			{
				get { return View3D_Orthographic(m_handle); }
				set { View3D_SetOrthographic(m_handle, value); }
			}

			/// <summary>The background colour for the window</summary>
			public Color BackgroundColour
			{
				get { return Color.FromArgb(View3D_BackgroundColour(m_handle)); }
				set { View3D_SetBackgroundColour(m_handle, value.ToArgb()); }
			}

			/// <summary>Get/Set the animation clock</summary>
			public float AnimTime
			{
				get { return View3D_WindowAnimTimeGet(m_handle); }
				set { View3D_WindowAnimTimeSet(m_handle, value); }
			}

			/// <summary>Cause the window to be rendered. Remember to call Present when done</summary>
			public void Render()
			{
				View3D_Render(m_handle);
			}

			/// <summary>Called to flip the back buffer to the screen after all window have been rendered</summary>
			public void Present()
			{
				View3D_Present(m_handle);
			}

			/// <summary>Resize the render target</summary>
			public Size RenderTargetSize
			{
				get { int w,h; View3D_RenderTargetSize(m_handle, out w, out h); return new Size(w,h); }
				set { View3D_SetRenderTargetSize(m_handle, value.Width, value.Height); }
			}

			/// <summary>Restore the render target as the main output</summary>
			public void RestoreRT()
			{
				View3D_RestoreMainRT(m_handle);
			}

			/// <summary>
			/// Render the current scene into 'render_target'. If no 'depth_buffer' is given a temporary one will be created.
			/// Note: Make sure the render target is not used as a texture for an object in the scene to be rendered.
			/// Either remove that object from the scene, or detach the texture from the object. 'render_target' cannot be
			/// a source and destination texture at the same time</summary>
			public void RenderTo(Texture render_target, Texture depth_buffer = null)
			{
				View3D_RenderTo(m_handle, render_target.Handle, depth_buffer != null ? depth_buffer.Handle : IntPtr.Zero);
			}

			/// <summary>Get/Set the size/position of the viewport within the render target</summary>
			public Viewport Viewport
			{
				get { return View3D_Viewport(m_handle); }
				set { View3D_SetViewport(m_handle, value); }
			}

			/// <summary>Get/Set whether the depth buffer is enabled</summary>
			public bool DepthBufferEnabled
			{
				get { return View3D_DepthBufferEnabledGet(m_handle); }
				set { View3D_DepthBufferEnabledSet(m_handle, value); }
			}

			/// <summary>Convert a screen space point to a normalised point</summary>
			public v2 SSPointToNSSPoint(PointF screen)
			{
				return View3D_SSPointToNSSPoint(m_handle, v2.From(screen));
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

			/// <summary>Handy method for creating random objects</summary>
			public Guid CreateDemoScene()
			{
				return View3D_DemoSceneCreate(Handle);

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
				View3D_DemoScriptShow(m_handle);
			}

			/// <summary>Show/Hide the object manager UI</summary>
			public void ShowObjectManager(bool show)
			{
				View3D_ObjectManagerShow(m_handle, show);
			}

			#region Equals
			public static bool operator == (Window lhs, Window rhs)
			{
				return ReferenceEquals(lhs,rhs) || Equals(lhs, rhs);
			}
			public static bool operator != (Window lhs, Window rhs)
			{
				return !(lhs == rhs);
			}
			public bool Equals(Window rhs)
			{
				return rhs != null && m_handle == rhs.m_handle;
			}
			public override bool Equals(object rhs)
			{
				return Equals(rhs as Window);
			}
			public override int GetHashCode()
			{
				return m_handle.ToInt32();
			}
			#endregion

			#region Event Args
			public class ErrorEventArgs :EventArgs
			{
				public ErrorEventArgs(string msg)
				{
					Message = msg;
				}

				/// <summary>The error message</summary>
				public string Message { get; private set; }
			}
			public class MouseNavigateEventArgs :EventArgs
			{
				public MouseNavigateEventArgs(PointF point, ENavOp nav_op, bool nav_beg_or_end)
				{
					ZNavigation         = false;
					Point       = point;
					NavOp       = nav_op;
					NavBegOrEnd = nav_beg_or_end;
				}
				public MouseNavigateEventArgs(PointF point, float delta, bool along_ray)
				{
					ZNavigation      = true;
					Point    = point;
					Delta    = delta;
					AlongRay = along_ray;
				}

				/// <summary>True if this is a Z axis navigation</summary>
				public bool ZNavigation { get; private set; }

				/// <summary>The mouse pointer in client rect space</summary>
				public PointF Point { get; private set; }

				/// <summary>The navigation operation to perform</summary>
				public ENavOp NavOp { get; private set; }

				/// <summary>True if this is the beginning or end of the navigation, false if during</summary>
				public bool NavBegOrEnd { get; private set; }

				/// <summary>The mouse wheel scroll delta</summary>
				public float Delta { get; private set; }

				/// <summary>True if Z axis navigation moves the camera along a ray through the mouse pointer</summary>
				public bool AlongRay { get; private set; }
			}
			#endregion
		}

		/// <summary>Namespace for the camera controls</summary>
		[DebuggerDisplay("{O2W.pos} FPoint={FocusPoint} FDist={FocusDist}")]
		public class Camera
		{
			private readonly Window m_window;
			public Camera(Window window)
			{
				m_window = window;
			}
			public Camera(Window window, XElement node)
				:this(window)
			{
				Load(node);
			}
			public XElement ToXml(XElement node)
			{
				node.Add2(nameof(O2W         ), O2W         , false);
				node.Add2(nameof(FocusDist   ), FocusDist   , false);
				node.Add2(nameof(Orthographic), Orthographic, false);
				node.Add2(nameof(Aspect      ), Aspect      , false);
				node.Add2(nameof(FovY        ), FovY        , false);
				node.Add2(nameof(AlignAxis   ), AlignAxis   , false);
				return node;
			}
			public void Load(XElement node)
			{
				O2W          = node.Element(nameof(O2W         )).As(O2W         );
				FocusDist    = node.Element(nameof(FocusDist   )).As(FocusDist   );
				Orthographic = node.Element(nameof(Orthographic)).As(Orthographic);
				Aspect       = node.Element(nameof(Aspect      )).As(Aspect      );
				FovY         = node.Element(nameof(FovY        )).As(FovY        );
				AlignAxis    = node.Element(nameof(AlignAxis   )).As(AlignAxis   );
				Commit();
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
				get { return View3D_CameraAlignAxisGet(m_window.Handle); }
				set { View3D_CameraAlignAxisSet(m_window.Handle, value); }
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
				get { return View3D_CameraFovXGet(m_window.Handle); }
				set { View3D_CameraFovXSet(m_window.Handle, value); }
			}

			/// <summary>Get/Set the camera vertical field of view (in radians). Note aspect ratio is preserved, setting FovY changes FovX and visa versa</summary>
			public float FovY
			{
				get { return View3D_CameraFovYGet(m_window.Handle); }
				set { View3D_CameraFovYSet(m_window.Handle, value); }
			}

			/// <summary>Set both the X and Y field of view (i.e. change the aspect ratio)</summary>
			public void SetFov(float fovX, float fovY)
			{
				View3D_CameraSetFov(m_window.Handle, fovX, fovY);
			}

			/// <summary>Adjust the FocusDist, FovX, and FovY so that the average FOV equals 'fov'</summary>
			public void BalanceFov(float fov)
			{
				View3D_CameraBalanceFov(m_window.Handle, fov);
			}

			/// <summary>Get/Set the camera near plane distance. Focus relative</summary>
			public float NearPlane
			{
				get { ClipPlanes(out var n, out var f, true); return n; }
				set { ClipPlanes(value, FarPlane, true); }
			}

			/// <summary>Get/Set the camera far plane distance. Focus relative</summary>
			public float FarPlane
			{
				get { ClipPlanes(out var n, out var f, true); return f; }
				set { ClipPlanes(NearPlane, value, true); }
			}

			/// <summary>Get/Set the camera clip plane distances</summary>
			public void ClipPlanes(out float near, out float far, bool focus_relative)
			{
				View3D_CameraClipPlanesGet(m_window.Handle, out near, out far, focus_relative);
			}
			public void ClipPlanes(float near, float far, bool focus_relative)
			{
				View3D_CameraClipPlanesSet(m_window.Handle, near, far, focus_relative);
			}

			/// <summary>Get/Set the position of the camera focus point (in world space, relative to the world origin)</summary>
			public v4 FocusPoint
			{
				get { v4 pos; View3D_FocusPointGet(m_window.Handle, out pos); return pos; }
				set { View3D_FocusPointSet(m_window.Handle, value); }
			}

			/// <summary>Get/Set the distance to the camera focus point</summary>
			public float FocusDist
			{
				get { return View3D_CameraFocusDistance(m_window.Handle); }
				set
				{
					Debug.Assert(value >= 0, "Focus distance cannot be negative");
					View3D_CameraSetFocusDistance(m_window.Handle, value);
				}
			}

			/// <summary>Get/Set the camera to world transform. Note: use SetPosition to set the focus distance at the same time</summary>
			public m4x4 O2W
			{
				get { m4x4 c2w; View3D_CameraToWorldGet(m_window.Handle, out c2w); return c2w; }
				set { View3D_CameraToWorldSet(m_window.Handle, ref value); }
			}
			public m4x4 W2O
			{
				get { return m4x4.InvertFast(O2W); }
			}

			/// <summary>Set the current O2W transform as the reference point</summary>
			public void Commit()
			{
				View3D_CameraCommit(m_window.Handle);
			}

			/// <summary>Set the camera to world transform and focus distance.</summary>
			public void SetPosition(v4 position, v4 lookat, v4 up)
			{
				View3D_CameraPositionSet(m_window.Handle, position, lookat, up);
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

			/// <summary>Move the camera to a position that can see the whole scene</summary>
			public void ResetView()
			{
				var up = AlignAxis;
				if (up.Length3Sq == 0f) up = v4.YAxis;
				var forward = up.z > up.y ? -v4.YAxis : -v4.ZAxis;
				ResetView(forward, up);
			}

			/// <summary>Move the camera to a position that can see the whole scene given camera direction 'forward'</summary>
			public void ResetView(v4 forward)
			{
				var up = AlignAxis;
				if (up.Length3Sq == 0f) up = v4.YAxis;
				if (v4.Parallel(up, forward)) up = v4.Perpendicular(forward);
				ResetView(forward, up);
			}

			/// <summary>Move the camera to a position that can see the whole scene given camera directions 'forward' and 'up'</summary>
			public void ResetView(v4 forward, v4 up, float dist = 0f, bool preserve_aspect = true, bool commit = true)
			{
				View3D_ResetView(m_window.Handle, forward, up, dist, preserve_aspect, commit);
			}

			/// <summary>Reset the camera to view a bbox</summary>
			public void ResetView(BBox bbox, v4 forward, v4 up, float dist = 0f, bool preserve_aspect = true, bool commit = true)
			{
				View3D_ResetViewBBox(m_window.Handle, bbox, forward, up, dist, preserve_aspect, commit);
			}

			/// <summary>Reset the zoom factor to 1f</summary>
			public void ResetZoom()
			{
				View3D_CameraResetZoom(m_window.Handle);
			}

			/// <summary>Get/Set the camera FOV zoom</summary>
			public float Zoom
			{
				get { return View3D_CameraZoomGet(m_window.Handle); }
				set { View3D_CameraZoomSet(m_window.Handle, value); }
			}

			/// <summary>Get/Set the camera movement lock mask</summary>
			public ECameraLockMask LockMask
			{
				get { return View3D_CameraLockMaskGet(m_window.Handle); }
				set { View3D_CameraLockMaskSet(m_window.Handle, value); }
			}

			/// <summary>
			/// Return a point in world space corresponding to a normalised screen space point.
			/// The x,y components of 'screen' should be in normalised screen space, i.e. (-1,-1)->(1,1)
			/// The z component should be the world space distance from the camera.</summary>
			public v4 NSSPointToWSPoint(v4 screen)
			{
				return View3D_NSSPointToWSPoint(m_window.Handle, screen);
			}
			public v4 SSPointToWSPoint(PointF screen)
			{
				var nss = SSPointToNSSPoint(screen);
				return NSSPointToWSPoint(new v4(nss.x, nss.y, View3D_CameraFocusDistance(m_window.Handle), 1.0f));
			}

			/// <summary>Return the normalised screen space point corresponding to a screen space point</summary>
			public v2 SSPointToNSSPoint(PointF screen)
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

			/// <summary>Convert a mouse button to the default navigation operation</summary>
			public static ENavOp MouseBtnToNavOp(MouseButtons btns)
			{
				return MouseBtnToNavOp(Win32.ToMKey(btns));
			}
			public static ENavOp MouseBtnToNavOp(int mk)
			{
				return View3D_MouseBtnToNavOp(mk);
			}

			#region Equals
			public static bool operator == (Camera lhs, Camera rhs)
			{
				return ReferenceEquals(lhs,rhs) || Equals(lhs, rhs);
			}
			public static bool operator != (Camera lhs, Camera rhs)
			{
				return !(lhs == rhs);
			}
			public bool Equals(Camera rhs)
			{
				return rhs != null && m_window == rhs.m_window;
			}
			public override bool Equals(object obj)
			{
				return Equals(obj as Camera);
			}
			public override int GetHashCode()
			{
				return m_window.GetHashCode() * 137;
			}
			#endregion
		}

		/// <summary>Object resource wrapper</summary>
		[DebuggerDisplay("{Description}")]
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
				:this("*group{}", false, null)
			{}
			public Object(string ldr_script, bool file, Guid? context_id)
				:this(ldr_script, file, context_id, null)
			{}
			public Object(string ldr_script, bool file, Guid? context_id, View3DIncludes? includes)
			{
				m_owned = true;
				var inc = includes ?? new View3DIncludes();
				var ctx = context_id ?? Guid.NewGuid();
				m_handle = View3D_ObjectCreateLdr(ldr_script, file, ref ctx, ref inc);
				if (m_handle == HObject.Zero)
					throw new Exception("Failed to create object from script\r\n{0}".Fmt(ldr_script.Summary(100)));
			}

			/// <summary>Create from buffer</summary>
			public Object(string name, uint colour, int vcount, int icount, int ncount, Vertex[] verts, ushort[] indices, Nugget[] nuggets, Guid? context_id)
			{
				m_owned = true;
				var ctx = context_id ?? Guid.NewGuid();

				// Serialise the verts/indices to a memory buffer
				using (var vbuf = Marshal_.ArrayToPtr(verts))
				using (var ibuf = Marshal_.ArrayToPtr(indices))
				using (var nbuf = Marshal_.ArrayToPtr(nuggets))
				{
					m_handle = View3D_ObjectCreate(name, colour, vcount, icount, ncount, vbuf.Value.Ptr, ibuf.Value.Ptr, nbuf.Value.Ptr, ref ctx);
					if (m_handle == HObject.Zero) throw new System.Exception("Failed to create object '{0}' from provided buffers".Fmt(name));
				}
			}

			/// <summary>Create an object via callback</summary>
			public Object(string name, uint colour, int vcount, int icount, int ncount, EditObjectCB edit_cb, Guid? context_id)
			{
				m_owned = true;
				var ctx = context_id ?? Guid.NewGuid();
				m_handle = View3D_ObjectCreateEditCB(name, colour, vcount, icount, ncount, edit_cb, IntPtr.Zero, ref ctx);
				if (m_handle == HObject.Zero) throw new Exception("Failed to create object '{0}' via edit callback".Fmt(name));
			}

			/// <summary>Attach to an existing object handle</summary>
			public Object(HObject handle)
				:this(handle, false)
			{}
			private Object(HObject handle, bool owned)
			{
				m_owned = owned;
				m_handle = handle;
			}
			public virtual void Dispose()
			{
				Util.BreakIf(Util.IsGCFinalizerThread, "Disposing in the GC finaliser thread");
				if (m_handle == HObject.Zero) return;
				if (m_owned) View3D_ObjectDelete(m_handle);
				m_handle = HObject.Zero;
			}

			/// <summary>Object name</summary>
			public string Name
			{
				get { return View3D_ObjectNameGetBStr(m_handle); }
				set { View3D_ObjectNameSet(m_handle, value); }
			}

			/// <summary>Get the type of Ldr object this is</summary>
			public string Type
			{
				get { return View3D_ObjectTypeGetBStr(m_handle); }
			}

			/// <summary>Get/Set the visibility of this object (set applies to all child objects as well)</summary>
			public bool Visible
			{
				get { return VisibleGet(string.Empty); }
				set { VisibleSet(value, string.Empty); }
			}

			/// <summary>Get/Set the visibility of this object (set applies to all child objects as well)</summary>
			public Colour32 Colour
			{
				get { return ColourGet(false, string.Empty); }
				set { ColourSet(value, string.Empty); }
			}

			/// <summary>Get/Set wire-frame mode of this object (set applies to all child objects as well)</summary>
			public bool Wireframe
			{
				get { return WireframeGet(string.Empty); }
				set { WireframeSet(value, string.Empty); }
			}

			/// <summary>Get the state flags of this object</summary>
			public EFlags Flags
			{
				get { return FlagsGet(string.Empty); }
			}

			/// <summary>The context id that this object belongs to</summary>
			public Guid ContextId
			{
				get { return View3D_ObjectContextIdGet(m_handle); }
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
				get { return View3D_ObjectO2PGet(m_handle, null); }
				set
				{
					Util.BreakIf(value.w.w != 1.0f, "Invalid object transform");
					View3D_ObjectO2PSet(m_handle, ref value, null);
				}
			}

			/// <summary>Get the model space bounding box of this object</summary>
			public BBox BBoxMS(bool include_children)
			{
				return View3D_ObjectBBoxMS(m_handle, include_children);
			}

			/// <summary>Return the object that is the root parent of this object (possibly itself)</summary>
			public Object Root
			{
				get
				{
					var root = View3D_ObjectGetRoot(m_handle);
					return root != HObject.Zero ? new Object(root, false) : null;
				}
			}

			/// <summary>Return the object that is the immediate parent of this object</summary>
			public Object Parent
			{
				get
				{
					var parent = View3D_ObjectGetParent(m_handle);
					return parent != HObject.Zero ? new Object(parent, false) : null;
				}
			}

			/// <summary>Return a child object of this object</summary>
			public Object Child(string name)
			{
				var child = View3D_ObjectGetChildByName(m_handle, name);
				return child != HObject.Zero ? new Object(child, false) : null;
			}
			public Object Child(int index)
			{
				var child = View3D_ObjectGetChildByIndex(m_handle, index);
				return child != HObject.Zero ? new Object(child, false) : null;
			}

			/// <summary>Return the number of child objects of this object</summary>
			public int ChildCount
			{
				get { return View3D_ObjectChildCount(m_handle); }
			}

			/// <summary>Return a list of all the child objects of this object</summary>
			public IList<Object> Children
			{
				get
				{
					if (m_handle == HObject.Zero) return new Object[0];
					var objects = new List<Object>(capacity:ChildCount);
					View3D_ObjectEnumChildren(m_handle, (ctx,obj) => { objects.Add(new Object(obj)); return true; }, IntPtr.Zero);
					return objects.ToArray();
				}
			}

			/// <summary>Create a new Object that shares the model (but not transform) of this object</summary>
			public Object CreateInstance()
			{
				var handle = View3D_ObjectCreateInstance(m_handle);
				if (handle == HObject.Zero)
					throw new Exception("Failed to create object instance");

				return new Object(handle, true);
			}

			/// <summary>
			/// Get/Set the object to world transform for this object or the first child object that matches 'name'.
			/// If 'name' is null, then the state of the root object is returned
			/// If 'name' begins with '#' then the remainder of the name is treated as a regular expression.
			/// Note, setting the o2w for a child object results in a transform that is relative to it's immediate parent</summary>
			public m4x4 O2WGet(string name = null)
			{
				return View3D_ObjectO2WGet(m_handle, name);
			}
			public void O2WSet(m4x4 o2w, string name = null)
			{
				View3D_ObjectO2WSet(m_handle, ref o2w, name);
			}

			/// <summary>
			/// Get/Set the object to parent transform for this object or any of its child objects that match 'name'.
			/// If 'name' is null, then the state change is applied to this object only
			/// If 'name' is "", then the state change is applied to this object and all children recursively
			/// Otherwise, the state change is applied to all child objects that match name.
			/// If 'name' begins with '#' then the remainder of the name is treated as a regular expression</summary>
			public m4x4 O2PGet(string name = null)
			{
				return View3D_ObjectO2PGet(m_handle, name);
			}
			public void O2PSet(m4x4 o2p, string name = null)
			{
				View3D_ObjectO2PSet(m_handle, ref o2p, name);
			}

			/// <summary>
			/// Get/Set the visibility of this object or any of its child objects that match 'name'.
			/// If 'name' is null, then the state change is applied to this object only
			/// If 'name' is "", then the state change is applied to this object and all children recursively
			/// Otherwise, the state change is applied to all child objects that match name.
			/// If 'name' begins with '#' then the remainder of the name is treated as a regular expression</summary>
			public bool VisibleGet(string name = null)
			{
				return View3D_ObjectVisibilityGet(m_handle, name);
			}
			public void VisibleSet(bool vis, string name = null)
			{
				View3D_ObjectVisibilitySet(m_handle, vis, name);
			}

			/// <summary>
			/// Get/Set wireframe mode of this object or any of its child objects that match 'name'.
			/// If 'name' is null, then the state change is applied to this object only
			/// If 'name' is "", then the state change is applied to this object and all children recursively
			/// Otherwise, the state change is applied to all child objects that match name.
			/// If 'name' begins with '#' then the remainder of the name is treated as a regular expression</summary>
			public bool WireframeGet(string name = null)
			{
				return View3D_ObjectWireframeGet(m_handle, name);
			}
			public void WireframeSet(bool vis, string name = null)
			{
				View3D_ObjectWireframeSet(m_handle, vis, name);
			}
		
			/// <summary>
			/// Get/Set the object flags
			/// See LdrObject::Apply for docs on the format of 'name'</summary>
			public EFlags FlagsGet(string name = null)
			{
				return View3D_ObjectFlagsGet(m_handle, name);
			}
			public void FlagsSet(EFlags flags, bool state, string name = null)
			{
				View3D_ObjectFlagsSet(m_handle, flags, state, name);
			}

			/// <summary>
			/// Get the colour of this object or the first child object that matches 'name'.
			/// 'base_colour', if true returns the objects base colour, if false, returns the current colour</summary>
			public Colour32 ColourGet(bool base_colour, string name = null)
			{
				return View3D_ObjectColourGet(m_handle, base_colour, name);
			}

			/// <summary>
			/// Set the colour of this object or any of its child objects that match 'name'.
			/// If 'name' is null, then the state change is applied to this object only
			/// If 'name' is "", then the state change is applied to this object and all children recursively
			/// Otherwise, the state change is applied to all child objects that match name.
			/// If 'name' begins with '#' then the remainder of the name is treated as a regular expression</summary>
			public void ColourSet(Colour32 colour, uint mask, string name = null)
			{
				View3D_ObjectColourSet(m_handle, colour, mask, name);
			}
			public void ColourSet(Colour32 colour, string name = null)
			{
				ColourSet(colour, 0xFFFFFFFF, name);
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
				View3D_ObjectSetTexture(m_handle, tex.Handle, name);
			}

			/// <summary>String description of the object</summary>
			public string Description
			{
				get { return $"{Name} ChildCount={ChildCount} Vis={Visible} Flags={Flags}"; }
			}
			public override string ToString()
			{
				return Description;
			}

			#region Equals
			public static bool operator == (Object lhs, Object rhs)
			{
				return ReferenceEquals(lhs,rhs) || Equals(lhs, rhs);
			}
			public static bool operator != (Object lhs, Object rhs)
			{
				return !(lhs == rhs);
			}
			public bool Equals(Object rhs)
			{
				return rhs != null && m_handle == rhs.m_handle;
			}
			public override bool Equals(object rhs)
			{
				return Equals(rhs as Object);
			}
			public override int GetHashCode()
			{
				return m_handle.GetHashCode();
			}
			#endregion
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
				Util.BreakIf(Util.IsGCFinalizerThread, "Disposing in the GC finalizer thread");
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

			/// <summary>Get/Set the scale of the gizmo</summary>
			public float Scale
			{
				get { return View3D_GizmoScaleGet(m_handle); }
				set { View3D_GizmoScaleSet(m_handle, value); }
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

			#region Equals
			public static bool operator == (Gizmo lhs, Gizmo rhs)
			{
				return ReferenceEquals(lhs,rhs) || Equals(lhs, rhs);
			}
			public static bool operator != (Gizmo lhs, Gizmo rhs)
			{
				return !(lhs == rhs);
			}
			public bool Equals(Gizmo rhs)
			{
				return rhs != null && m_handle == rhs.m_handle;
			}
			public override bool Equals(object rhs)
			{
				return Equals(rhs as Gizmo);
			}
			public override int GetHashCode()
			{
				return m_handle.GetHashCode();
			}
			#endregion
		}

		/// <summary>Texture resource wrapper</summary>
		public class Texture :IDisposable
		{
			private bool m_owned;

			/// <summary>Create a texture from an existing texture resource</summary>
			internal Texture(HTexture handle)
			{
				m_owned = false;
				Handle = handle;
				View3D_TextureGetInfo(Handle, out Info);
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
				Handle = View3D_TextureCreate(width, height, data, data_size, ref options);
				if (Handle == HTexture.Zero) throw new Exception("Failed to create {0}x{1} texture".Fmt(width,height));
				
				View3D_TextureGetInfo(Handle, out Info);
				View3D_TextureSetFilterAndAddrMode(Handle, options.Filter, options.AddrU, options.AddrV);
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
				Handle = View3D_TextureCreateFromFile(tex_filepath, width, height, ref options);
				if (Handle == HTexture.Zero) throw new Exception("Failed to create texture from {0}".Fmt(tex_filepath));
				View3D_TextureGetInfo(Handle, out Info);
				View3D_TextureSetFilterAndAddrMode(Handle, options.Filter, options.AddrU, options.AddrV);
			}
			
			public virtual void Dispose()
			{
				Util.BreakIf(Util.IsGCFinalizerThread, "Disposing in the GC finalizer thread");
				if (Handle == HTexture.Zero) return;
				if (m_owned) View3D_TextureDelete(Handle);
				Handle = HTexture.Zero;
			}

			/// <summary>View3d texture handle</summary>
			public HTexture Handle;

			/// <summary>Texture format information</summary>
			public ImageInfo Info;

			/// <summary>Get/Set the texture size. Set does not preserve the texture content</summary>
			public Size Size
			{
				get { return new Size((int)Info.m_width, (int)Info.m_height); }
				set
				{
					if (Size == value) return;
					Resize((uint)value.Width, (uint)value.Height, false, false);
				}
			}

			/// <summary>User Data</summary>
			public object Tag { get; set; }

			/// <summary>Resize the texture optionally preserving content</summary>
			public void Resize(uint width, uint height, bool all_instances, bool preserve)
			{
				View3D_TextureResize(Handle, width, height, all_instances, preserve);
				View3D_TextureGetInfo(Handle, out Info);
			}
			
			/// <summary>Set the filtering and addressing modes to be used on the texture</summary>
			public void SetFilterAndAddrMode(EFilter filter, EAddrMode addrU, EAddrMode addrV)
			{
				View3D_TextureSetFilterAndAddrMode(Handle, filter, addrU, addrV);
			}

			/// <summary>Fill a surface of this texture from a file</summary>
			public void LoadSurface(string tex_filepath, int level)
			{
				View3D_TextureLoadSurface(Handle, level, tex_filepath, null, null, EFilter.D3D11_FILTER_MIN_MAG_MIP_LINEAR, 0);
				View3D_TextureGetInfo(Handle, out Info);
			}

			/// <summary>Fill a surface of this texture from a file</summary>
			public void LoadSurface(string tex_filepath, int level, EFilter filter, uint colour_key)
			{
				View3D_TextureLoadSurface(Handle, level, tex_filepath, null, null, filter, colour_key);
				View3D_TextureGetInfo(Handle, out Info);
			}

			/// <summary>Fill a surface of this texture from a file</summary>
			public void LoadSurface(string tex_filepath, int level, Rectangle src_rect, Rectangle dst_rect, EFilter filter, uint colour_key)
			{
				View3D_TextureLoadSurface(Handle, level, tex_filepath, new []{dst_rect}, new []{src_rect}, filter, colour_key);
				View3D_TextureGetInfo(Handle, out Info);
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

				/// <summary>
				/// Lock 'tex' making 'Gfx' available.
				/// Note: if 'tex' is the render target of a window, you need to call Window.RestoreRT when finished</summary>
				public Lock(Texture tex, bool discard)
				{
					m_tex = tex.Handle;
					var dc = View3D_TextureGetDC(m_tex, discard);
					if (dc == IntPtr.Zero) throw new Exception("Failed to get Texture DC. Check the texture is a GdiCompatible texture");
					Gfx = Graphics.FromHdc(dc);
				}
				public void Dispose()
				{
					View3D_TextureReleaseDC(m_tex);
				}

				/// <summary>GDI+ graphics interface</summary>
				public Graphics Gfx { get; private set; }
			}

			/// <summary>Lock the texture for drawing on</summary>
			[DebuggerHidden]
			public Lock LockSurface(bool discard)
			{
				// This is a method to prevent the debugger evaluating it cause multiple GetDC calls
				return new Lock(this, discard);
			}

			#region Equals
			public static bool operator == (Texture lhs, Texture rhs)
			{
				return ReferenceEquals(lhs,rhs) || Equals(lhs, rhs);
			}
			public static bool operator != (Texture lhs, Texture rhs)
			{
				return !(lhs == rhs);
			}
			public bool Equals(Texture rhs)
			{
				return rhs != null && Handle == rhs.Handle;
			}
			public override bool Equals(object rhs)
			{
				return Equals(rhs as Texture);
			}
			public override int GetHashCode()
			{
				return Handle.GetHashCode();
			}
			#endregion
		}

		/// <summary>Reference type wrapper of a view3d light</summary>
		public class Light :INotifyPropertyChanged
		{
			public Light()
			{
				m_info = LightInfo.Ambient(0xFFFFFFFF);
			}
			public Light(LightInfo light)
			{
				m_info = light;
			}
			public Light(Colour32 ambient, Colour32 diffuse, Colour32 specular, float spec_power = 1000f, v4? direction = null, v4? position = null)
				:this(
					direction != null ? LightInfo.Directional(direction.Value, ambient, diffuse, specular, spec_power, 0f) :
					position  != null ? LightInfo.Point(position.Value, ambient, diffuse, specular, spec_power, 0f) :
					LightInfo.Ambient(ambient))
			{}
			public Light(XElement node) :this()
			{
				Type           = node.Element(nameof(Type          )).As(Type          );
				Position       = node.Element(nameof(Position      )).As(Position      );
				Direction      = node.Element(nameof(Direction     )).As(Direction     );
				Ambient        = node.Element(nameof(Ambient       )).As(Ambient       );
				Diffuse        = node.Element(nameof(Diffuse       )).As(Diffuse       );
				Specular       = node.Element(nameof(Specular      )).As(Specular      );
				SpecularPower  = node.Element(nameof(SpecularPower )).As(SpecularPower );
				InnerAngle     = node.Element(nameof(InnerAngle    )).As(InnerAngle    );
				OuterAngle     = node.Element(nameof(OuterAngle    )).As(OuterAngle    );
				Range          = node.Element(nameof(Range         )).As(Range         );
				Falloff        = node.Element(nameof(Falloff       )).As(Falloff       );
				CastShadow     = node.Element(nameof(CastShadow    )).As(CastShadow    );
				On             = node.Element(nameof(On            )).As(On            );
				CameraRelative = node.Element(nameof(CameraRelative)).As(CameraRelative);
			}
			public XElement ToXml(XElement node)
			{
				node.Add2(nameof(Type          ), Type          , false);
				node.Add2(nameof(Position      ), Position      , false);
				node.Add2(nameof(Direction     ), Direction     , false);
				node.Add2(nameof(Ambient       ), Ambient       , false);
				node.Add2(nameof(Diffuse       ), Diffuse       , false);
				node.Add2(nameof(Specular      ), Specular      , false);
				node.Add2(nameof(SpecularPower ), SpecularPower , false);
				node.Add2(nameof(InnerAngle    ), InnerAngle    , false);
				node.Add2(nameof(OuterAngle    ), OuterAngle    , false);
				node.Add2(nameof(Range         ), Range         , false);
				node.Add2(nameof(Falloff       ), Falloff       , false);
				node.Add2(nameof(CastShadow    ), CastShadow    , false);
				node.Add2(nameof(On            ), On            , false);
				node.Add2(nameof(CameraRelative), CameraRelative, false);
				return node;
			}

			/// <summary>The View3d Light data</summary>
			public LightInfo Data
			{
				get { return m_info; }
				set { SetProp(ref m_info, value, nameof(Data)); }
			}
			private LightInfo m_info;

			/// <summary>The type of light source</summary>
			public ELight Type
			{
				get { return m_info.m_type; }
				set { SetProp(ref m_info.m_type, value, nameof(Type)); }
			}

			/// <summary>The position of the light source</summary>
			public v4 Position
			{
				get { return m_info.m_position; }
				set { SetProp(ref m_info.m_position, value, nameof(Position)); }
			}

			/// <summary>The direction of the light source</summary>
			public v4 Direction
			{
				get { return m_info.m_direction; }
				set { SetProp(ref m_info.m_direction, value, nameof(Direction)); }
			}

			/// <summary>The colour of the ambient component of the light</summary>
			public Colour32 Ambient
			{
				get { return m_info.m_ambient; }
				set { SetProp(ref m_info.m_ambient, value, nameof(Ambient)); }
			}

			/// <summary>The colour of the diffuse component of the light</summary>
			public Colour32 Diffuse
			{
				get { return m_info.m_diffuse; }
				set { SetProp(ref m_info.m_diffuse, value, nameof(Diffuse)); }
			}

			/// <summary>The colour of the specular component of the light</summary>
			public Colour32 Specular
			{
				get { return m_info.m_specular; }
				set { SetProp(ref m_info.m_specular, value, nameof(Specular)); }
			}

			/// <summary>The specular power</summary>
			public float SpecularPower
			{
				get { return m_info.m_specular_power; }
				set { SetProp(ref m_info.m_specular_power, value, nameof(SpecularPower)); }
			}

			/// <summary>The inner spot light cone angle (in degrees)</summary>
			public float InnerAngle
			{
				get { return (float)Maths.RadiansToDegrees(Math.Acos(m_info.m_inner_cos_angle)); }
				set { SetProp(ref m_info.m_inner_cos_angle, (float)Math.Cos(Maths.DegreesToRadians(value)), nameof(InnerAngle)); }
			}

			/// <summary>The outer spot light cone angle (in degrees)</summary>
			public float OuterAngle
			{
				get { return (float)Maths.RadiansToDegrees(Math.Acos(m_info.m_outer_cos_angle)); }
				set { SetProp(ref m_info.m_outer_cos_angle, (float)Math.Cos(Maths.DegreesToRadians(value)), nameof(OuterAngle)); }
			}

			/// <summary>The range of the light</summary>
			public float Range
			{
				get { return m_info.m_range; }
				set { SetProp(ref m_info.m_range, value, nameof(Range)); }
			}

			/// <summary>The attenuation of the light with distance</summary>
			public float Falloff
			{
				get { return m_info.m_falloff; }
				set { SetProp(ref m_info.m_falloff, value, nameof(Falloff)); }
			}

			/// <summary>The maximum distance from the light source in which objects cast shadows</summary>
			public float CastShadow
			{
				get { return m_info.m_cast_shadow; }
				set { SetProp(ref m_info.m_cast_shadow, value, nameof(CastShadow)); }
			}

			/// <summary>Whether the light is active or not</summary>
			public bool On
			{
				get { return m_info.m_on; }
				set { SetProp(ref m_info.m_on, value, nameof(On)); }
			}

			/// <summary>Whether the light moves with the camera or not</summary>
			public bool CameraRelative
			{
				get { return m_info.m_cam_relative; }
				set { SetProp(ref m_info.m_cam_relative, value, nameof(CameraRelative)); }
			}

			/// <summary>Notify property value changed</summary>
			public event PropertyChangedEventHandler PropertyChanged;
			private void SetProp<T>(ref T prop, T value, string name)
			{
				if (Equals(prop, value)) return;
				prop = value;
				PropertyChanged.Raise(this, new PropertyChangedEventArgs(name));
			}

			/// <summary>Implicit conversion to the value type</summary>
			public static implicit operator LightInfo(Light light) { return light.m_info; }
		}

		/// <summary>A wrapper class for handling embedded C# in ldr script</summary>
		private class EmbeddedCSHandler :IDisposable
		{
			private StringBuilder m_support;
			private EmbeddedCodeHandlerCB m_cb;  // Callback reference

			public EmbeddedCSHandler(View3d view3d)
			{
				m_support = new StringBuilder();
				View3D_EmbeddedCodeCBSet(m_cb = Handler, IntPtr.Zero, true);
			}
			public void Dispose()
			{
				View3D_EmbeddedCodeCBSet(m_cb, IntPtr.Zero, false);
			}

			/// <summary>Ldr script embedded C# code handler</summary>
			private bool Handler(IntPtr ctx, bool reset, string lang, string code, out string result, out string errors)
			{
				result = null;
				errors = null;

				// If reset is requested, reset
				if (reset)
				{
					m_support.Clear();
					return true;
				}

				// Check we handle this language
				lang = lang.ToLowerInvariant();
				if (!lang.StartsWith("csharp"))
					return false;

				// IF the embedded code is support code, there is nothing to execute
				if (lang == "csharpimpl")
				{
					m_support.Append(code);
					return true;
				}

				try
				{
					var src =
						#region Embedded C# Source
$@"//
//Assembly: System.dll
//Assembly: System.Drawing.dll
//Assembly: System.IO.dll
//Assembly: System.Linq.dll
//Assembly: System.Windows.Forms.dll
//Assembly: System.Xml.dll
//Assembly: System.Xml.Linq.dll
//Assembly: Rylogic.dll
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using pr.common;
using pr.container;
using pr.extn;
using pr.ldr;
using pr.maths;
using pr.util;

namespace ldr
{{
	public class EmbeddedScriptGen
	{{
		private StringBuilder Out = new StringBuilder();
		{m_support.ToString()}
		public string Execute()
		{{
			{code}
			return Out.ToString();
		}}
	}}
}}
";
					#endregion

					// Create a runtime assembly from the embedded code
					var ass = RuntimeAssembly.FromString("ldr.EmbeddedScriptGen", src);
					result = ass.Invoke<string>("Execute");
				}
				catch (CompileException ex)
				{
					errors = ex.ErrorReport();
				}
				catch (System.Exception ex)
				{
					errors = ex.Message;
				}
				return true;
			}
		}

		#region Event Args
		public class AddFileProgressEventArgs :CancelEventArgs
		{
			public AddFileProgressEventArgs(Guid context_id, string filepath, long file_offset, bool complete)
			{
				ContextId  = context_id;
				Filepath   = filepath;
				FileOffset = file_offset;
				Complete   = complete;
			}

			/// <summary>An anonymous pointer unique to each 'AddFile' call</summary>
			public Guid ContextId { get; private set; }

			/// <summary>The file currently being parsed</summary>
			public string Filepath { get; private set; }

			/// <summary>How far through the current file parsing is up to</summary>
			public long FileOffset { get; private set; }

			/// <summary>Last progress update notification</summary>
			public bool Complete { get; private set; }
		}

		public class SourcesChangedEventArgs :EventArgs
		{
			public SourcesChangedEventArgs(ESourcesChangedReason reason, bool before)
			{
				Reason = reason;
				Before = before;
			}

			/// <summary>The cause of the source changes</summary>
			public ESourcesChangedReason Reason { get; private set; }

			/// <summary>True if files are about to change</summary>
			public bool Before { get; private set; }
		}

		public class SceneChangedEventArgs :EventArgs
		{
			public SceneChangedEventArgs(Guid[] context_ids)
			{
				ContextIds = context_ids ?? new Guid[0];
			}

			/// <summary>The context ids of the objects that were changed in the scene</summary>
			public Guid[] ContextIds { get; private set; }
		}

		#endregion

		#region DLL extern functions

		/// <summary>True if the view3d dll has been loaded</summary>
		private const string Dll = "view3d";
		public static bool ModuleLoaded { get { return m_module != IntPtr.Zero; } }
		private static IntPtr m_module = IntPtr.Zero;

		/// <summary>Helper method for loading the view3d.dll from a platform specific path</summary>
		public static void LoadDll(string dir = @".\lib\$(platform)")
		{
			if (ModuleLoaded) return;
			m_module = Win32.LoadDll(Dll+".dll", dir);
		}

		/// <summary>Report errors callback</summary>
		public delegate void ReportErrorCB(IntPtr ctx, [MarshalAs(UnmanagedType.LPWStr)] string msg);

		/// <summary>Report settings changed callback</summary>
		public delegate void SettingsChangedCB(IntPtr ctx, HWindow wnd);

		/// <summary>Enumerate guids callback</summary>
		public delegate bool EnumGuidsCB(IntPtr ctx, Guid guid);

		/// <summary>Enumerate objects callback</summary>
		public delegate bool EnumObjectsCB(IntPtr ctx, HObject obj);

		/// <summary>Callback for progress updates during AddFile / Reload</summary>
		public delegate bool AddFileProgressCB(IntPtr ctx, ref Guid context_id, [MarshalAs(UnmanagedType.LPWStr)] string filepath, long file_offset, bool complete);

		/// <summary>Callback when the sources are reloaded</summary>
		public delegate void SourcesChangedCB(IntPtr ctx, ESourcesChangedReason reason, bool before);

		/// <summary>Called just prior to rendering</summary>
		public delegate void RenderCB(IntPtr ctx, HWindow wnd);

		/// <summary>Callback for when the collection of objects associated with a window changes</summary>
		public delegate void SceneChangedCB(IntPtr ctx, HWindow wnd, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] Guid[] context_ids, int count);

		/// <summary>Edit object callback</summary>
		public delegate void EditObjectCB(IntPtr ctx, int vcount, int icount, int ncount,
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)][Out] Vertex[] verts,
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)][Out] ushort[] indices,
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex=2)][Out] Nugget[] nuggets,
			out int new_vcount, out int new_icount, out int new_ncount);

		/// <summary>Embedded code handler callback</summary>
		public delegate bool EmbeddedCodeHandlerCB(IntPtr ctx,
			bool reset,
			[MarshalAs(UnmanagedType.LPWStr)] string lang,
			[MarshalAs(UnmanagedType.LPWStr)] string code,
			[MarshalAs(UnmanagedType.BStr)] out string result,
			[MarshalAs(UnmanagedType.BStr)] out string errors);

		// Context
		[DllImport(Dll)] private static extern HContext        View3D_Initialise            (ReportErrorCB initialise_error_cb, IntPtr ctx, bool gdi_compatibility);
		[DllImport(Dll)] private static extern void            View3D_Shutdown              (HContext context);
		[DllImport(Dll)] private static extern void            View3D_GlobalErrorCBSet      (ReportErrorCB error_cb, IntPtr ctx, bool add);
		[DllImport(Dll)] private static extern void            View3D_SourceEnumGuids       (EnumGuidsCB enum_guids_cb, IntPtr ctx);
		[DllImport(Dll)] private static extern Guid            View3D_LoadScriptSource      ([MarshalAs(UnmanagedType.LPWStr)] string ldr_filepath, bool additional, ref View3DIncludes includes);
		[DllImport(Dll)] private static extern Guid            View3D_LoadScript            ([MarshalAs(UnmanagedType.LPWStr)] string ldr_script, bool file, ref Guid context_id, ref View3DIncludes includes);
		[DllImport(Dll)] private static extern void            View3D_ReloadScriptSources   ();
		[DllImport(Dll)] private static extern void            View3D_ObjectsDeleteAll      ();
		[DllImport(Dll)] private static extern void            View3D_ObjectsDeleteById     ([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Guid[] context_ids, int count, bool all_except);
		[DllImport(Dll)] private static extern void            View3D_CheckForChangedSources();
		[DllImport(Dll)] private static extern void            View3D_AddFileProgressCBSet  (AddFileProgressCB progress_cb, IntPtr ctx, bool add);
		[DllImport(Dll)] private static extern void            View3D_SourcesChangedCBSet   (SourcesChangedCB sources_changed_cb, IntPtr ctx, bool add);
		[DllImport(Dll)] private static extern void            View3D_EmbeddedCodeCBSet     (EmbeddedCodeHandlerCB embedded_code_cb, IntPtr ctx, bool add);
		[DllImport(Dll)] private static extern bool            View3D_ContextIdFromFilepath ([MarshalAs(UnmanagedType.LPWStr)] string filepath, out Guid id);

		// Windows
		[DllImport(Dll)] private static extern HWindow         View3D_WindowCreate              (HWND hwnd, ref WindowOptions opts);
		[DllImport(Dll)] private static extern void            View3D_WindowDestroy             (HWindow window);
		[DllImport(Dll)] private static extern void            View3D_WindowErrorCBSet          (HWindow window, ReportErrorCB error_cb, IntPtr ctx, bool add);
		[DllImport(Dll)] private static extern IntPtr          View3D_WindowSettingsGet         (HWindow window);
		[DllImport(Dll)] private static extern void            View3D_WindowSettingsSet         (HWindow window, string settings);
		[DllImport(Dll)] private static extern void            View3D_WindowSettingsChangedCB   (HWindow window, SettingsChangedCB settings_changed_cb, IntPtr ctx, bool add);
		[DllImport(Dll)] private static extern void            View3D_WindowRenderingCB         (HWindow window, RenderCB rendering_cb, IntPtr ctx, bool add);
		[DllImport(Dll)] private static extern void            View3d_WindowSceneChangedCB      (HWindow window, SceneChangedCB scene_changed_cb, IntPtr ctx, bool add);
		[DllImport(Dll)] private static extern void            View3D_WindowSceneChangedSuspend (HWindow window, bool suspend);
		[DllImport(Dll)] private static extern void            View3D_WindowAddObject           (HWindow window, HObject obj);
		[DllImport(Dll)] private static extern void            View3D_WindowRemoveObject        (HWindow window, HObject obj);
		[DllImport(Dll)] private static extern void            View3D_WindowRemoveAllObjects    (HWindow window);
		[DllImport(Dll)] private static extern bool            View3D_WindowHasObject           (HWindow window, HObject obj);
		[DllImport(Dll)] private static extern int             View3D_WindowObjectCount         (HWindow window);
		[DllImport(Dll)] private static extern void            View3D_WindowEnumGuids           (HWindow window, EnumGuidsCB enum_guids_cb, IntPtr ctx);
		[DllImport(Dll)] private static extern void            View3D_WindowEnumObjects         (HWindow window, EnumObjectsCB enum_objects_cb, IntPtr ctx);
		[DllImport(Dll)] private static extern void            View3D_WindowEnumObjectsById     (HWindow window, EnumObjectsCB enum_objects_cb, IntPtr ctx, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] Guid[] context_id, int count, bool all_except);
		[DllImport(Dll)] private static extern void            View3D_WindowAddObjectsById      (HWindow window, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] Guid[] context_id, int count, bool all_except);
		[DllImport(Dll)] private static extern void            View3D_WindowRemoveObjectsById   (HWindow window, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] Guid[] context_id, int count, bool all_except);
		[DllImport(Dll)] private static extern void            View3D_WindowAddGizmo            (HWindow window, HGizmo giz);
		[DllImport(Dll)] private static extern void            View3D_WindowRemoveGizmo         (HWindow window, HGizmo giz);
		[DllImport(Dll)] private static extern BBox            View3D_WindowSceneBounds         (HWindow window, ESceneBounds bounds, int except_count, Guid[] except);
		[DllImport(Dll)] private static extern float           View3D_WindowAnimTimeGet         (HWindow window);
		[DllImport(Dll)] private static extern void            View3D_WindowAnimTimeSet         (HWindow window, float time_s);
		[DllImport(Dll)] private static extern void            View3D_WindowHitTest             (HWindow window, ref HitTestRay ray, float snap_distance, EHitTestFlags flags, out HitTestResult hit);

		// Camera
		[DllImport(Dll)] private static extern void            View3D_CameraToWorldGet       (HWindow window, out m4x4 c2w);
		[DllImport(Dll)] private static extern void            View3D_CameraToWorldSet       (HWindow window, ref m4x4 c2w);
		[DllImport(Dll)] private static extern void            View3D_CameraPositionSet      (HWindow window, v4 position, v4 lookat, v4 up);
		[DllImport(Dll)] private static extern void            View3D_CameraCommit           (HWindow window);
		[DllImport(Dll)] private static extern bool            View3D_CameraOrthographic     (HWindow window);
		[DllImport(Dll)] private static extern void            View3D_CameraOrthographicSet  (HWindow window, bool on);
		[DllImport(Dll)] private static extern float           View3D_CameraFocusDistance    (HWindow window);
		[DllImport(Dll)] private static extern void            View3D_CameraSetFocusDistance (HWindow window, float dist);
		[DllImport(Dll)] private static extern void            View3D_CameraSetViewRect      (HWindow window, float width, float height, float dist);
		[DllImport(Dll)] private static extern float           View3D_CameraAspect           (HWindow window);
		[DllImport(Dll)] private static extern void            View3D_CameraSetAspect        (HWindow window, float aspect);
		[DllImport(Dll)] private static extern float           View3D_CameraFovXGet          (HWindow window);
		[DllImport(Dll)] private static extern void            View3D_CameraFovXSet          (HWindow window, float fovX);
		[DllImport(Dll)] private static extern float           View3D_CameraFovYGet          (HWindow window);
		[DllImport(Dll)] private static extern void            View3D_CameraFovYSet          (HWindow window, float fovY);
		[DllImport(Dll)] private static extern void            View3D_CameraSetFov           (HWindow window, float fovX, float fovY);
		[DllImport(Dll)] private static extern void            View3D_CameraBalanceFov       (HWindow window, float fov);
		[DllImport(Dll)] private static extern void            View3D_CameraClipPlanesGet    (HWindow window, out float near, out float far, bool focus_relative);
		[DllImport(Dll)] private static extern void            View3D_CameraClipPlanesSet    (HWindow window, float near, float far, bool focus_relative);
		[DllImport(Dll)] private static extern void            View3D_CameraResetZoom        (HWindow window);
		[DllImport(Dll)] private static extern float           View3D_CameraZoomGet          (HWindow window);
		[DllImport(Dll)] private static extern void            View3D_CameraZoomSet          (HWindow window, float zoom);
		[DllImport(Dll)] private static extern ECameraLockMask View3D_CameraLockMaskGet      (HWindow window);
		[DllImport(Dll)] private static extern void            View3D_CameraLockMaskSet      (HWindow window, ECameraLockMask mask);
		[DllImport(Dll)] private static extern v4              View3D_CameraAlignAxisGet     (HWindow window);
		[DllImport(Dll)] private static extern void            View3D_CameraAlignAxisSet     (HWindow window, v4 axis);
		[DllImport(Dll)] private static extern void            View3D_ResetView              (HWindow window, v4 forward, v4 up, float dist, bool preserve_aspect, bool commit);
		[DllImport(Dll)] private static extern void            View3D_ResetViewBBox          (HWindow window, BBox bbox, v4 forward, v4 up, float dist, bool preserve_aspect, bool commit);
		[DllImport(Dll)] private static extern v2              View3D_ViewArea               (HWindow window, float dist);
		[DllImport(Dll)] private static extern bool            View3D_MouseNavigate          (HWindow window, v2 ss_point, ENavOp nav_op, bool nav_start_or_end);
		[DllImport(Dll)] private static extern bool            View3D_MouseNavigateZ         (HWindow window, v2 ss_point, float delta, bool along_ray);
		[DllImport(Dll)] private static extern bool            View3D_Navigate               (HWindow window, float dx, float dy, float dz);
		[DllImport(Dll)] private static extern void            View3D_FocusPointGet          (HWindow window, out v4 position);
		[DllImport(Dll)] private static extern void            View3D_FocusPointSet          (HWindow window, v4 position);
		[DllImport(Dll)] private static extern v2              View3D_SSPointToNSSPoint      (HWindow window, v2 screen);
		[DllImport(Dll)] private static extern v4              View3D_NSSPointToWSPoint      (HWindow window, v4 screen);
		[DllImport(Dll)] private static extern v4              View3D_WSPointToNSSPoint      (HWindow window, v4 world);
		[DllImport(Dll)] private static extern void            View3D_NSSPointToWSRay        (HWindow window, v4 screen, out v4 ws_point, out v4 ws_direction);
		[DllImport(Dll)] private static extern ENavOp          View3D_MouseBtnToNavOp        (int mk);

		// Lights
		[DllImport(Dll)] private static extern void              View3D_LightProperties          (HWindow window, out LightInfo light);
		[DllImport(Dll)] private static extern void              View3D_SetLightProperties       (HWindow window, ref LightInfo light);
		[DllImport(Dll)] private static extern void              View3D_LightSource              (HWindow window, v4 position, v4 direction, bool camera_relative);
		[DllImport(Dll)] private static extern void              View3D_ShowLightingDlg          (HWindow window);

		// Objects
		[DllImport(Dll)] private static extern Guid              View3D_ObjectContextIdGet       (HObject obj);
		[DllImport(Dll)] private static extern HObject           View3D_ObjectCreateLdr          ([MarshalAs(UnmanagedType.LPWStr)] string ldr_script, bool file, ref Guid context_id, ref View3DIncludes includes);
		[DllImport(Dll)] private static extern HObject           View3D_ObjectCreate             (string name, uint colour, int vcount, int icount, int ncount, IntPtr verts, IntPtr indices, IntPtr nuggets, ref Guid context_id);
		[DllImport(Dll)] private static extern HObject           View3D_ObjectCreateEditCB       (string name, uint colour, int vcount, int icount, int ncount, EditObjectCB edit_cb, IntPtr ctx, ref Guid context_id);
		[DllImport(Dll)] private static extern HObject           View3D_ObjectCreateInstance     (HObject obj);
		[DllImport(Dll)] private static extern void              View3D_ObjectUpdate             (HObject obj, [MarshalAs(UnmanagedType.LPWStr)] string ldr_script, EUpdateObject flags);
		[DllImport(Dll)] private static extern void              View3D_ObjectEdit               (HObject obj, EditObjectCB edit_cb, IntPtr ctx);
		[DllImport(Dll)] private static extern void              View3D_ObjectDelete             (HObject obj);
		[DllImport(Dll)] private static extern HObject           View3D_ObjectGetRoot            (HObject obj);
		[DllImport(Dll)] private static extern HObject           View3D_ObjectGetParent          (HObject obj);
		[DllImport(Dll)] private static extern HObject           View3D_ObjectGetChildByName     (HObject obj, string name);
		[DllImport(Dll)] private static extern HObject           View3D_ObjectGetChildByIndex    (HObject obj, int index);
		[DllImport(Dll)] private static extern int               View3D_ObjectChildCount         (HObject obj);
		[DllImport(Dll)] private static extern void              View3D_ObjectEnumChildren       (HObject obj, EnumObjectsCB enum_objects_cb, IntPtr ctx);
		[return:MarshalAs(UnmanagedType.BStr)]
		[DllImport(Dll)] private static extern string            View3D_ObjectNameGetBStr        (HObject obj);
		[DllImport(Dll)] private static extern void              View3D_ObjectNameSet            (HObject obj, [MarshalAs(UnmanagedType.LPStr)] string name);
		[return:MarshalAs(UnmanagedType.BStr)]
		[DllImport(Dll)] private static extern string            View3D_ObjectTypeGetBStr        (HObject obj);
		[DllImport(Dll)] private static extern m4x4              View3D_ObjectO2WGet             (HObject obj, string name);
		[DllImport(Dll)] private static extern void              View3D_ObjectO2WSet             (HObject obj, ref m4x4 o2w, string name);
		[DllImport(Dll)] private static extern m4x4              View3D_ObjectO2PGet             (HObject obj, string name);
		[DllImport(Dll)] private static extern void              View3D_ObjectO2PSet             (HObject obj, ref m4x4 o2p, string name);
		[DllImport(Dll)] private static extern bool              View3D_ObjectVisibilityGet      (HObject obj, string name);
		[DllImport(Dll)] private static extern void              View3D_ObjectVisibilitySet      (HObject obj, bool visible, string name);
		[DllImport(Dll)] private static extern EFlags            View3D_ObjectFlagsGet           (HObject obj, string name);
		[DllImport(Dll)] private static extern void              View3D_ObjectFlagsSet           (HObject obj, EFlags flags, bool state, string  name);
		[DllImport(Dll)] private static extern uint              View3D_ObjectColourGet          (HObject obj, bool base_colour, string name);
		[DllImport(Dll)] private static extern void              View3D_ObjectColourSet          (HObject obj, uint colour, uint mask, string name);
		[DllImport(Dll)] private static extern bool              View3D_ObjectWireframeGet       (HObject obj, string name);
		[DllImport(Dll)] private static extern void              View3D_ObjectWireframeSet       (HObject obj, bool wireframe, string name);
		[DllImport(Dll)] private static extern void              View3D_ObjectResetColour        (HObject obj, string name);
		[DllImport(Dll)] private static extern void              View3D_ObjectSetTexture         (HObject obj, HTexture tex, string name);
		[DllImport(Dll)] private static extern BBox              View3D_ObjectBBoxMS             (HObject obj, bool include_children);

		// Materials
		[DllImport(Dll)] private static extern HTexture          View3D_TextureCreate               (uint width, uint height, IntPtr data, uint data_size, ref TextureOptions options);
		[DllImport(Dll)] private static extern HTexture          View3D_TextureCreateFromFile       ([MarshalAs(UnmanagedType.LPWStr)] string tex_filepath, uint width, uint height, ref TextureOptions options);
		[DllImport(Dll)] private static extern void              View3D_TextureLoadSurface          (HTexture tex, int level, string tex_filepath, Rectangle[] dst_rect, Rectangle[] src_rect, EFilter filter, uint colour_key);
		[DllImport(Dll)] private static extern void              View3D_TextureDelete               (HTexture tex);
		[DllImport(Dll)] private static extern void              View3D_TextureGetInfo              (HTexture tex, out ImageInfo info);
		[DllImport(Dll)] private static extern EResult           View3D_TextureGetInfoFromFile      (string tex_filepath, out ImageInfo info);
		[DllImport(Dll)] private static extern void              View3D_TextureSetFilterAndAddrMode (HTexture tex, EFilter filter, EAddrMode addrU, EAddrMode addrV);
		[DllImport(Dll)] private static extern IntPtr            View3D_TextureGetDC                (HTexture tex, bool discard);
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
		[DllImport(Dll)] private static extern EFillMode         View3D_FillModeGet              (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_FillModeSet              (HWindow window, EFillMode mode);
		[DllImport(Dll)] private static extern ECullMode         View3D_CullModeGet              (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_CullModeSet              (HWindow window, ECullMode mode);
		[DllImport(Dll)] private static extern bool              View3D_Orthographic             (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_SetOrthographic          (HWindow window, bool render2d);
		[DllImport(Dll)] private static extern int               View3D_BackgroundColour         (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_SetBackgroundColour      (HWindow window, int aarrggbb);
		[DllImport(Dll)] private static extern int               View3D_MultiSamplingGet         (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_MultiSamplingSet         (HWindow window, int multisampling);

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
		[DllImport(Dll)] private static extern float             View3D_GizmoScaleGet            (HGizmo gizmo);
		[DllImport(Dll)] private static extern void              View3D_GizmoScaleSet            (HGizmo gizmo, float scale);
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
		[DllImport(Dll)] private static extern bool              View3D_DepthBufferEnabledGet    (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_DepthBufferEnabledSet    (HWindow window, bool enabled);
		[DllImport(Dll)] private static extern bool              View3D_FocusPointVisibleGet     (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_FocusPointVisibleSet     (HWindow window, bool show);
		[DllImport(Dll)] private static extern void              View3D_FocusPointSizeSet        (HWindow window, float size);
		[DllImport(Dll)] private static extern bool              View3D_OriginVisibleGet         (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_OriginVisibleSet         (HWindow window, bool show);
		[DllImport(Dll)] private static extern void              View3D_OriginSizeSet            (HWindow window, float size);
		[DllImport(Dll)] private static extern bool              View3D_BBoxesVisibleGet         (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_BBoxesVisibleSet         (HWindow window, bool visible);
		[DllImport(Dll)] private static extern bool              View3D_SelectionBoxVisibleGet   (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_SelectionBoxVisibleSet   (HWindow window, bool visible);
		[DllImport(Dll)] private static extern void              View3D_SelectionBoxPosition     (HWindow window, ref BBox box, ref m4x4 o2w);
		[DllImport(Dll)] private static extern void              View3D_SelectionBoxFitToSelected(HWindow window);
		[DllImport(Dll)] private static extern Guid              View3D_DemoSceneCreate          (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_DemoSceneDelete          ();
		[DllImport(Dll)] private static extern void              View3D_DemoScriptShow           (HWindow window);
		[DllImport(Dll)] private static extern void              View3D_ObjectManagerShow        (HWindow window, bool show);
		[DllImport(Dll)] private static extern m4x4              View3D_ParseLdrTransform        (string ldr_script);
		[return:MarshalAs(UnmanagedType.BStr)]
		[DllImport(Dll)] private static extern string            View3D_ExampleScriptBStr();

		[DllImport(Dll)] private static extern HWND              View3D_LdrEditorCreate          (HWND parent);
		[DllImport(Dll)] private static extern void              View3D_LdrEditorDestroy         (HWND hwnd);
		[DllImport(Dll)] private static extern void              View3D_LdrEditorCtrlInit        (HWND scintilla_control, bool dark);
		#endregion
	}
}
