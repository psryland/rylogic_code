//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once

#ifdef VIEW3D_EXPORTS
#define VIEW3D_API __declspec(dllexport)
#else
#define VIEW3D_API __declspec(dllimport)
#endif

#include <cstdint>
#include <windows.h>
#include <guiddef.h>
#include <d3d12.h>

#ifdef VIEW3D_EXPORTS
namespace pr
{
	namespace ldr
	{
		struct LdrObject;
		struct LdrGizmo;
	}
	namespace rdr12
	{
		struct Texture2D;
		struct TextureCube;
	}
	namespace view3d
	{
		using Object = pr::ldr::LdrObject;
		using Gizmo = pr::ldr::LdrGizmo;
		using Texture = pr::rdr12::Texture2D;
		using CubeMap = pr::rdr12::TextureCube;
		struct Window;
	}
}
using View3DContext = unsigned char*;
using View3DWindow  = pr::view3d::Window*;
using View3DObject  = pr::view3d::Object*;
using View3DGizmo   = pr::view3d::Gizmo*;
using View3DTexture = pr::view3d::Texture*;
using View3DCubeMap = pr::view3d::CubeMap*;
#else
using View3DContext = void*;
using View3DWindow = void*;
using View3DObject = void*;
using View3DGizmo = void*;
using View3DTexture = void*;
using View3DCubeMap = void*;
#endif

// Forward declarations
extern "C"
{
	struct View3DV2;
	struct View3DV4;
	struct View3DM4x4;
	struct View3DBBox;
	struct View3DVertex;
	struct View3DImageInfo;
	struct View3DTextureOptions;
	struct View3DWindowOptions;
	struct View3DUpdateModelKeep;
	struct View3DMaterial;
	struct View3DViewport;
	struct View3DGizmoEvent;
	using View3DColour = unsigned int;
	using View3D_ReportErrorCB = void(__stdcall*)(void* ctx, wchar_t const* msg, wchar_t const* filepath, int line, int64_t pos);
}

// Enumerations/Constants
extern "C"
{
	enum class EView3DResult :int
	{
		Success,
		Failed,
	};
	enum class EView3DFillMode :int
	{
		Default = 0,
		Points = 1,
		Wireframe = D3D12_FILL_MODE::D3D12_FILL_MODE_WIREFRAME,
		Solid = D3D12_FILL_MODE::D3D12_FILL_MODE_SOLID,
		SolidWire = 4,
	};
	enum class EView3DCullMode :int
	{
		Default = 0,
		None = D3D12_CULL_MODE::D3D12_CULL_MODE_NONE,
		Front = D3D12_CULL_MODE::D3D12_CULL_MODE_FRONT,
		Back = D3D12_CULL_MODE::D3D12_CULL_MODE_BACK,
	};
	enum class EView3DGeom :int // pr::rdr::EGeom
	{
		Unknown = 0,
		Vert = 1 << 0, // Object space 3D position
		Colr = 1 << 1, // Diffuse base colour
		Norm = 1 << 2, // Object space 3D normal
		Tex0 = 1 << 3, // Diffuse texture
		_bitwise_operators_allowed,
	};
	enum class EView3DTopo :int
	{
		Invalid = D3D12_PRIMITIVE_TOPOLOGY_TYPE::D3D12_PRIMITIVE_TOPOLOGY_TYPE_UNDEFINED,
		Point = D3D12_PRIMITIVE_TOPOLOGY_TYPE::D3D12_PRIMITIVE_TOPOLOGY_TYPE_POINT,
		Line = D3D12_PRIMITIVE_TOPOLOGY_TYPE::D3D12_PRIMITIVE_TOPOLOGY_TYPE_LINE,
		Triangle = D3D12_PRIMITIVE_TOPOLOGY_TYPE::D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE,
		Patch = D3D12_PRIMITIVE_TOPOLOGY_TYPE::D3D12_PRIMITIVE_TOPOLOGY_TYPE_PATCH,
	};
	enum class EView3DNuggetFlag :int
	{
		None = 0,

		// Exclude this nugget when rendering a model
		Hidden = 1 << 0,

		// Set if the geometry data for the nugget contains alpha colours
		GeometryHasAlpha = 1 << 1,

		// Set if the tint colour contains alpha
		TintHasAlpha = 1 << 2,

		// Excluded from shadow map render steps
		ShadowCastExclude = 1 << 3,

		_bitwise_operators_allowed,
	};
	enum class EView3DShaderVS :int
	{
		Standard = 0,
	};
	enum class EView3DShaderPS :int
	{
		Standard = 0,

		// Radial fade params:
		//  *Type {Spherical|Cylindrical}
		//  *Radius {min,max}
		//  *Centre {x,y,z} (optional, defaults to camera position)
		//  *Absolute (optional, default false) - True if 'radius' is absolute, false if 'radius' should be scaled by the focus distance
		RadialFadePS,
	};
	enum class EView3DShaderGS :int
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
	};
	enum class EView3DShaderCS :int
	{
		None = 0,
	};
	enum class EView3DRenderStep :int
	{
		Invalid = 0,
		ForwardRender,
		GBuffer,
		DSLighting,
		ShadowMap,
		RayCast,
		_number_of,
	};
	enum class EView3DStockTexture :int
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
		EnvMapProjection,
	};
	enum class EView3DLight :int
	{
		Ambient,
		Directional,
		Point,
		Spot
	};
	enum class EView3DAnimCommand : int
	{
		Reset, // Reset the 'time' value
		Play,  // Run continuously using 'time' as the step size, or real time if 'time' == 0
		Stop,  // Stop at the current time.
		Step,  // Step by 'time' (can be positive or negative)
	};
	enum class EView3DLogLevel :int
	{
		Debug,
		Info,
		Warn,
		Error,
	};
	enum class EView3DUpdateObject :unsigned int // Flags for partial update of a model
	{
		None = 0U,
		All = ~0U,
		Name = 1 << 0,
		Model = 1 << 1,
		Transform = 1 << 2,
		Children = 1 << 3,
		Colour = 1 << 4,
		ColourMask = 1 << 5,
		Reflectivity = 1 << 6,
		Flags = 1 << 7,
		Animation = 1 << 8,
		_bitwise_operators_allowed,
	};
	enum class EView3DGizmoState :int // ELdrGizmoEvent 
	{
		StartManip,
		Moving,
		Commit,
		Revert,
	};
	enum class EView3DGizmoMode :int
	{
		Translate,
		Rotate,
		Scale,
	};
	enum class EView3DNavOp :int // pr::camera::ENavOp
	{
		None = 0,
		Translate = 1 << 0,
		Rotate = 1 << 1,
		Zoom = 1 << 2,
		_bitwise_operators_allowed,
	};
	enum class EView3DColourOp :int // pr::ldr::EColourOp
	{
		Overwrite,
		Add,
		Subtract,
		Multiply,
		Lerp,
	};
	enum class EView3DCameraLockMask :int // pr::camera::ELockMask
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
		All = (1 << 7) - 1, // Not including camera relative
	};
	enum class EView3DFlags :int // sync with 'ELdrFlags'
	{
		None = 0,

		// The object is hidden
		Hidden = 1 << 0,

		// The object is filled in wireframe mode
		Wireframe = 1 << 1,

		// Render the object without testing against the depth buffer
		NoZTest = 1 << 2,

		// Render the object without effecting the depth buffer
		NoZWrite = 1 << 3,

		// The object has normals shown
		Normals = 1 << 4,

		// The object to world transform is not an affine transform
		NonAffine = 1 << 5,

		// Set when an object is selected. The meaning of 'selected' is up to the application
		Selected = 1 << 8,

		// Doesn't contribute to the bounding box on an object.
		BBoxExclude = 1 << 9,

		// Should not be included when determining the bounds of a scene.
		SceneBoundsExclude = 1 << 10,

		// Ignored for hit test ray casts
		HitTestExclude = 1 << 11,

		// Doesn't cast a shadow
		ShadowCastExclude = 1 << 12,

		// Bitwise operators supported
		_bitwise_operators_allowed,
	};
	enum class EView3DSortGroup :int
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

		// Arithmetic operators supported
		_arithmetic_operators_allowed,
	};
	enum class EView3DSceneBounds :int
	{
		All,
		Selected,
		Visible,
	};
	enum class EView3DSourcesChangedReason :int
	{
		NewData,
		Reload,
		Removal,
	};
	enum class EView3DSceneChanged :int
	{
		ObjectsAdded,
		ObjectsRemoved,
		GizmoAdded,
		GizmoRemoved,
	};
	enum class EView3DHitTestFlags :int
	{
		Faces = 1 << 0,
		Edges = 1 << 1,
		Verts = 1 << 2,
		_bitwise_operators_allowed = 0x7FFFFFF,
	};
	enum class EView3DSnapType :int
	{
		NoSnap,
		Vert,
		EdgeMiddle,
		FaceCentre,
		Edge,
		Face,
	};
	enum class EView3DSettings :int
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

		_bitwise_operators_allowed = 0x7FFFFFF,
	};
}

// Structures
extern "C"
{
	struct View3DV2
	{
		float x, y;
	};
	struct View3DV4
	{
		float x, y, z, w;
	};
	struct View3DM4x4
	{
		View3DV4 x, y, z, w;
	};
	struct View3DBBox
	{
		View3DV4 centre;
		View3DV4 radius;
	};
	struct View3DVertex
	{
		View3DV4 pos;
		View3DV4 norm;
		View3DV2 tex;
		View3DColour col;
		UINT32 pad;
	};
	struct View3DMaterial
	{
		struct ShaderSet
		{
			struct { EView3DShaderVS shdr; char const* params; } m_vs;
			struct { EView3DShaderGS shdr; char const* params; } m_gs;
			struct { EView3DShaderPS shdr; char const* params; } m_ps;
			struct { EView3DShaderCS shdr; char const* params; } m_cs;
		};
		struct ShaderMap
		{
			// The set of shaders for each render step
			ShaderSet m_rstep[(int)EView3DRenderStep::_number_of];
		};

		View3DTexture m_diff_tex;
		ShaderMap     m_shader_map;
		UINT32        m_tint;
		float         m_relative_reflectivity;
	};
	struct View3DNugget
	{
		EView3DTopo       m_topo;
		EView3DGeom       m_geom;
		EView3DCullMode   m_cull_mode;
		EView3DFillMode   m_fill_mode;
		UINT32            m_v0, m_v1;       // Vertex buffer range. Set to 0,0 to mean the whole buffer
		UINT32            m_i0, m_i1;       // Index buffer range. Set to 0,0 to mean the whole buffer
		EView3DNuggetFlag m_nflags;         // Nugget flags
		BOOL              m_range_overlaps; // True if the nugget V/I range overlaps earlier nuggets
		View3DMaterial    m_mat;
	};
	struct View3DImageInfo
	{
		UINT32      m_width;
		UINT32      m_height;
		UINT32      m_depth;
		UINT32      m_mips;
		DXGI_FORMAT m_format;
		UINT32      m_image_file_format;//D3DXIMAGE_FILEFORMAT
	};
	struct View3DLight
	{
		View3DV4     m_position;
		View3DV4     m_direction;
		EView3DLight m_type;
		View3DColour m_ambient;
		View3DColour m_diffuse;
		View3DColour m_specular;
		float        m_specular_power;
		float        m_range;
		float        m_falloff;
		float        m_inner_angle;
		float        m_outer_angle;
		float        m_cast_shadow;
		BOOL         m_cam_relative;
		BOOL         m_on;
	};
	struct View3DTextureOptions
	{
		View3DM4x4                 m_t2s;
		DXGI_FORMAT                m_format;
		UINT                       m_mips;
		D3D12_FILTER               m_filter;
		D3D12_TEXTURE_ADDRESS_MODE m_addrU;
		D3D12_TEXTURE_ADDRESS_MODE m_addrV;
		//D3D12_BIND_FLAG            m_bind_flags;
		//D3D12_RESOURCE_MISC_FLAG   m_misc_flags;
		UINT                       m_multisamp;
		UINT                       m_colour_key;
		BOOL                       m_has_alpha;
		BOOL                       m_gdi_compatible;
		char const* m_dbg_name;
	};
	struct View3DCubeMapOptions
	{
		View3DM4x4                 m_cube2w;
		DXGI_FORMAT                m_format;
		D3D12_FILTER               m_filter;
		D3D12_TEXTURE_ADDRESS_MODE m_addrU;
		D3D12_TEXTURE_ADDRESS_MODE m_addrV;
		//D3D12_BIND_FLAG            m_bind_flags;
		//D3D12_RESOURCE_MISC_FLAG   m_misc_flags;
		char const* m_dbg_name;
	};
	struct View3DWindowOptions
	{
		View3D_ReportErrorCB m_error_cb;
		void* m_error_cb_ctx;
		BOOL                 m_gdi_compatible_backbuffer;
		int                  m_multisampling;
		char const* m_dbg_name;
	};
	struct View3DUpdateModelKeep
	{
		BOOL m_name;
		BOOL m_transform;
		BOOL m_context_id;
		BOOL m_children;
		BOOL m_colour;
		BOOL m_colour_mask;
		BOOL m_wireframe;
		BOOL m_visibility;
		BOOL m_animation;
		BOOL m_step_data;
		BOOL m_user_data;
	};
	struct View3DHitTestRay
	{
		// The world space origin and direction of the ray (normalisation not required)
		View3DV4 m_ws_origin;
		View3DV4 m_ws_direction;
	};
	struct View3DHitTestResult
	{
		// The origin and direction of the cast ray (in world space)
		View3DV4 m_ws_ray_origin;
		View3DV4 m_ws_ray_direction;

		// The intercept point (in world space)
		View3DV4 m_ws_intercept;

		// The object that was hit (or null)
		View3DObject m_obj;

		// The distance from ray origin to hit point
		float m_distance;

		// How the hit point was snapped (if at all)
		EView3DSnapType m_snap_type;
	};
	struct View3DViewport
	{
		float m_x;
		float m_y;
		float m_width;
		float m_height;
		float m_min_depth;
		float m_max_depth;
		int m_screen_w;
		int m_screen_h;
	};
	struct View3DIncludes
	{
		wchar_t const* m_include_paths; // A comma or semicolon separated list of search directories
		HMODULE        m_modules[16];   // An array of binary modules that contain resources. '0' means 'this' module
		int            m_module_count;  // The number of valid module values in 'm_modules'
		// (ToDo) A string lookup table
	};
	struct View3DSceneChanged
	{
		// How the scene was changed
		EView3DSceneChanged m_change_type;

		// An array of the context ids that changed
		GUID const* m_ctx_ids;

		// The length of the 'm_ctx_ids' array
		int m_count;

		// Pointer to the object that changed (for single object changes only)
		View3DObject m_object;
	};
	struct View3DAnimEvent
	{
		// The state change type
		EView3DAnimCommand m_command;

		// The current animation clock value
		double m_clock;
	};
}

// Call-backs
extern "C"
{
	using View3D_SettingsChangedCB = void(__stdcall*)(void* ctx, View3DWindow window, EView3DSettings setting);
	using View3D_EnumGuidsCB = BOOL(__stdcall*)(void* ctx, GUID const& context_id);
	using View3D_EnumObjectsCB = BOOL(__stdcall*)(void* ctx, View3DObject object);
	using View3D_AddFileProgressCB = void(__stdcall*)(void* ctx, GUID const& context_id, wchar_t const* filepath, long long file_offset, BOOL complete, BOOL* cancel);
	using View3D_OnAddCB = void(__stdcall*)(void* ctx, GUID const& context_id, BOOL before);
	using View3D_SourcesChangedCB = void(__stdcall*)(void* ctx, EView3DSourcesChangedReason reason, BOOL before);
	using View3D_InvalidatedCB = void(__stdcall*)(void* ctx, View3DWindow window);
	using View3D_RenderCB = void(__stdcall*)(void* ctx, View3DWindow window);
	using View3D_SceneChangedCB = void(__stdcall*)(void* ctx, View3DWindow window, View3DSceneChanged const&);
	using View3D_AnimationCB = void(__stdcall*)(void* ctx, View3DWindow window, EView3DAnimCommand command, double clock);
	using View3D_GizmoMovedCB = void(__stdcall*)(void* ctx, View3DGizmo gizmo, EView3DGizmoState state);
	using View3D_EditObjectCB = void(__stdcall*)(
		void* ctx,             // User callback context pointer
		UINT32 vcount,         // The maximum size of 'verts'
		UINT32 icount,         // The maximum size of 'indices'
		UINT32 ncount,         // The maximum size of 'nuggets'
		View3DVertex* verts,   // The vert buffer to be filled
		UINT16* indices,       // The index buffer to be filled
		View3DNugget* nuggets, // The nugget buffer to be filled
		UINT32& new_vcount,    // The number of verts in the updated model
		UINT32& new_icount,    // The number indices in the updated model
		UINT32& new_ncount);   // The number nuggets in the updated model
	using View3D_EmbeddedCodeHandlerCB = BOOL(__stdcall*)(
		void* ctx,              // User callback context pointer
		wchar_t const* code,    // The source code from the embedded code block.
		wchar_t const* support, // The support code from earlier embedded code blocks.
		BSTR& result,           // The string result of running the source code (execution code blocks only)
		BSTR& errors);          // Any errors in the compilation of the code
}

// Dll Interface
extern "C"
{
	// Context
	VIEW3D_API View3DContext __stdcall View3D_Initialise(View3D_ReportErrorCB initialise_error_cb, void* ctx, UINT debug_flags);
	VIEW3D_API void          __stdcall View3D_Shutdown(View3DContext context);
}