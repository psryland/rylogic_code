//***************************************************************************************************
// View 3D
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************
#pragma once

#ifdef VIEW3D_EXPORTS
#define VIEW3D_API __declspec(dllexport)
#else
#define VIEW3D_API __declspec(dllimport)
#endif

#include <windows.h>
#include <guiddef.h>
#include <d3d11.h>

#ifdef VIEW3D_EXPORTS
	namespace pr
	{
		namespace ldr
		{
			struct LdrObject;
			struct LdrGizmo;
		}
		namespace rdr
		{
			struct Texture2D;
			struct TextureCube;
		}
	}
	namespace view3d
	{
		using Object = pr::ldr::LdrObject;
		using Gizmo = pr::ldr::LdrGizmo;
		using Texture = pr::rdr::Texture2D;
		using CubeMap = pr::rdr::TextureCube;
		struct Window;
	}
	using View3DContext = unsigned char*;
	using View3DWindow = view3d::Window*;
	using View3DObject = view3d::Object*;
	using View3DGizmo = view3d::Gizmo*;
	using View3DTexture = view3d::Texture*;
	using View3DCubeMap = view3d::CubeMap*;
#else
	using View3DContext = void*;
	using View3DWindow = void*;
	using View3DObject = void*;
	using View3DGizmo = void*;
	using View3DTexture = void*;
	using View3DCubeMap = void*;
#endif

extern "C"
{
	// Forward declarations
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
	using View3D_ReportErrorCB = void (__stdcall *)(void* ctx, wchar_t const* msg);

	enum class EView3DResult :int
	{
		Success,
		Failed,
	};
	enum class EView3DFillMode :int
	{
		Default = 0,
		SolidWire = 1,
		Wireframe = D3D11_FILL_MODE::D3D11_FILL_WIREFRAME,
		Solid = D3D11_FILL_MODE::D3D11_FILL_SOLID,
	};
	enum class EView3DCullMode :int
	{
		Default = 0,
		None = D3D11_CULL_MODE::D3D11_CULL_NONE,
		Front = D3D11_CULL_MODE::D3D11_CULL_FRONT,
		Back = D3D11_CULL_MODE::D3D11_CULL_BACK,
	};
	enum class EView3DGeom :int // pr::rdr::EGeom
	{
		Unknown = 0,
		Vert    = 1 << 0, // Object space 3D position
		Colr    = 1 << 1, // Diffuse base colour
		Norm    = 1 << 2, // Object space 3D normal
		Tex0    = 1 << 3, // Diffuse texture
		_bitwise_operators_allowed,
	};
	enum class EView3DPrim :int
	{
		Invalid      = D3D11_PRIMITIVE_TOPOLOGY_UNDEFINED,
		PointList    = D3D11_PRIMITIVE_TOPOLOGY_POINTLIST,
		LineList     = D3D11_PRIMITIVE_TOPOLOGY_LINELIST,
		LineStrip    = D3D11_PRIMITIVE_TOPOLOGY_LINESTRIP,
		TriList      = D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST,
		TriStrip     = D3D11_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP,
		LineListAdj  = D3D11_PRIMITIVE_TOPOLOGY_LINELIST_ADJ,
		LineStripAdj = D3D11_PRIMITIVE_TOPOLOGY_LINESTRIP_ADJ,
		TriListAdj   = D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST_ADJ,
		TriStripAdj  = D3D11_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP_ADJ,
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
		Reset, // Reset the the 'time' value
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
		None         = 0U,
		All          = ~0U,
		Name         = 1 << 0,
		Model        = 1 << 1,
		Transform    = 1 << 2,
		Children     = 1 << 3,
		Colour       = 1 << 4,
		ColourMask   = 1 << 5,
		Reflectivity = 1 << 6,
		Flags        = 1 << 7,
		Animation    = 1 << 8,
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
		None      = 0,
		Translate = 1 << 0,
		Rotate    = 1 << 1,
		Zoom      = 1 << 2,
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
	};
	enum class EView3DFlags :int
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

		// Set when an object is selected. The meaning of 'selected' is up to the application
		Selected = 1 << 8,

		// Doesn't contribute to the bounding box on an object.
		BBoxExclude = 1 << 9,

		// Should not be included when determining the bounds of a scene.
		SceneBoundsExclude = 1 << 10,

		// Ignored for hit test ray casts
		HitTestExclude = 1 << 11,

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
		PreAlpha = Default+16, // Last group before the alpha groups
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

		General                     = 1 << 16,
		General_FocusPointVisible   = General | 1 << 0,
		General_OriginPointVisible  = General | 1 << 1,
		General_BBoxesVisible       = General | 1 << 2,
		General_SelectionBoxVisible = General | 1 << 3,

		Scene                  = 1 << 17,
		Scene_BackgroundColour = Scene | 1 << 0,
		Scene_Multisampling    = Scene | 1 << 1,
		Scene_FilllMode        = Scene | 1 << 2,
		Scene_CullMode         = Scene | 1 << 3,
		Scene_Viewport         = Scene | 1 << 4,

		Camera              = 1 << 18,
		Camera_Position     = Camera | 1 << 0,
		Camera_FocusDist    = Camera | 1 << 1,
		Camera_Orthographic = Camera | 1 << 2,
		Camera_Aspect       = Camera | 1 << 3,
		Camera_Fov          = Camera | 1 << 4,
		Camera_ClipPlanes   = Camera | 1 << 5,
		Camera_LockMask     = Camera | 1 << 6,
		Camera_AlignAxis    = Camera | 1 << 7,

		Lighting     = 1 << 19,
		Lighting_All = Lighting | 1 << 0,

		_bitwise_operators_allowed = 0x7FFFFFF,
	};

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
		EView3DPrim     m_topo;
		EView3DGeom     m_geom;
		EView3DCullMode m_cull_mode;
		EView3DFillMode m_fill_mode;
		UINT32          m_v0, m_v1;       // Vertex buffer range. Set to 0,0 to mean the whole buffer
		UINT32          m_i0, m_i1;       // Index buffer range. Set to 0,0 to mean the whole buffer
		UINT32          m_flags;          // Nugget flags (EView3DNuggetFlag)
		BOOL            m_range_overlaps; // True if the nugget V/I range overlaps earlier nuggets
		View3DMaterial  m_mat;
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
		EView3DLight m_type;
		View3DV4     m_position;
		View3DV4     m_direction;
		View3DColour m_ambient;
		View3DColour m_diffuse;
		View3DColour m_specular;
		float        m_specular_power;
		float        m_inner_angle;
		float        m_outer_angle;
		float        m_range;
		float        m_falloff;
		float        m_cast_shadow;
		BOOL         m_on;
		BOOL         m_cam_relative;
	};
	struct View3DTextureOptions
	{
		View3DM4x4                 m_t2s;
		DXGI_FORMAT                m_format;
		UINT                       m_mips;
		D3D11_FILTER               m_filter;
		D3D11_TEXTURE_ADDRESS_MODE m_addrU;
		D3D11_TEXTURE_ADDRESS_MODE m_addrV;
		D3D11_BIND_FLAG            m_bind_flags;
		D3D11_RESOURCE_MISC_FLAG   m_misc_flags;
		UINT                       m_multisamp;
		UINT                       m_colour_key;
		BOOL                       m_has_alpha;
		BOOL                       m_gdi_compatible;
		char const*                m_dbg_name;
	};
	struct View3DCubeMapOptions
	{
		View3DM4x4                 m_cube2w;
		DXGI_FORMAT                m_format;
		D3D11_FILTER               m_filter;
		D3D11_TEXTURE_ADDRESS_MODE m_addrU;
		D3D11_TEXTURE_ADDRESS_MODE m_addrV;
		D3D11_BIND_FLAG            m_bind_flags;
		D3D11_RESOURCE_MISC_FLAG   m_misc_flags;
		char const*                m_dbg_name;
	};
	struct View3DWindowOptions
	{
		View3D_ReportErrorCB m_error_cb;
		void*                m_error_cb_ctx;
		BOOL                 m_gdi_compatible_backbuffer;
		int                  m_multisampling;
		char const*          m_dbg_name;
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

	using View3D_SettingsChangedCB     = void (__stdcall *)(void* ctx, View3DWindow window, EView3DSettings setting);
	using View3D_EnumGuidsCB           = BOOL (__stdcall *)(void* ctx, GUID const& context_id);
	using View3D_EnumObjectsCB         = BOOL (__stdcall *)(void* ctx, View3DObject object);
	using View3D_AddFileProgressCB     = void (__stdcall *)(void* ctx, GUID const& context_id, wchar_t const* filepath, long long file_offset, BOOL complete, BOOL* cancel);
	using View3D_OnAddCB               = void (__stdcall *)(void* ctx, GUID const& context_id, BOOL before);
	using View3D_SourcesChangedCB      = void (__stdcall *)(void* ctx, EView3DSourcesChangedReason reason, BOOL before);
	using View3D_InvalidatedCB         = void (__stdcall *)(void* ctx, View3DWindow window);
	using View3D_RenderCB              = void (__stdcall *)(void* ctx, View3DWindow window);
	using View3D_SceneChangedCB        = void (__stdcall *)(void* ctx, View3DWindow window, View3DSceneChanged const&);
	using View3D_AnimationCB           = void (__stdcall *)(void* ctx, View3DWindow window, EView3DAnimCommand command, double clock);
	using View3D_GizmoMovedCB          = void (__stdcall *)(void* ctx, View3DGizmo gizmo, EView3DGizmoState state);
	using View3D_EditObjectCB          = void (__stdcall *)(
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
	using View3D_EmbeddedCodeHandlerCB = BOOL (__stdcall *)(
		void* ctx,              // User callback context pointer
		wchar_t const* code,    // The source code from the embedded code block.
		wchar_t const* support, // The support code from earlier embedded code blocks.
		BSTR& result,           // The string result of running the source code (execution code blocks only)
		BSTR& errors);          // Any errors in the compilation of the code

	// Context
	VIEW3D_API View3DContext __stdcall View3D_Initialise            (View3D_ReportErrorCB initialise_error_cb, void* ctx, D3D11_CREATE_DEVICE_FLAG device_flags);
	VIEW3D_API void          __stdcall View3D_Shutdown              (View3DContext context);
	VIEW3D_API void          __stdcall View3D_GlobalErrorCBSet      (View3D_ReportErrorCB error_cb, void* ctx, BOOL add);
	VIEW3D_API void          __stdcall View3D_SourceEnumGuids       (View3D_EnumGuidsCB enum_guids_cb, void* ctx);
	VIEW3D_API GUID          __stdcall View3D_LoadScript            (wchar_t const* ldr_script, BOOL is_file, GUID const* context_id, View3DIncludes const* includes, View3D_OnAddCB on_add, void* ctx);
	VIEW3D_API void          __stdcall View3D_ReloadScriptSources   ();
	VIEW3D_API void          __stdcall View3D_ObjectsDeleteAll      ();
	VIEW3D_API void          __stdcall View3D_ObjectsDeleteById     (GUID const* context_ids, int include_count, int exclude_count);
	VIEW3D_API void          __stdcall View3D_ObjectsDeleteUnused   (GUID const* context_ids, int include_count, int exclude_count);
	VIEW3D_API void          __stdcall View3D_CheckForChangedSources();
	VIEW3D_API void          __stdcall View3D_AddFileProgressCBSet  (View3D_AddFileProgressCB progress_cb, void* ctx, BOOL add);
	VIEW3D_API void          __stdcall View3D_SourcesChangedCBSet   (View3D_SourcesChangedCB sources_changed_cb, void* ctx, BOOL add);
	VIEW3D_API void          __stdcall View3D_EmbeddedCodeCBSet     (wchar_t const* lang, View3D_EmbeddedCodeHandlerCB embedded_code_cb, void* ctx, BOOL add);
	VIEW3D_API BOOL          __stdcall View3D_ContextIdFromFilepath (wchar_t const* filepath, GUID& id);

	// Windows
	VIEW3D_API View3DWindow __stdcall View3D_WindowCreate             (HWND hwnd, View3DWindowOptions const& opts);
	VIEW3D_API void         __stdcall View3D_WindowDestroy            (View3DWindow window);
	VIEW3D_API void         __stdcall View3D_WindowErrorCBSet         (View3DWindow window, View3D_ReportErrorCB error_cb, void* ctx, BOOL add);
	VIEW3D_API char const*  __stdcall View3D_WindowSettingsGet        (View3DWindow window);
	VIEW3D_API void         __stdcall View3D_WindowSettingsSet        (View3DWindow window, char const* settings);
	VIEW3D_API void         __stdcall View3D_WindowSettingsChangedCB  (View3DWindow window, View3D_SettingsChangedCB settings_changed_cb, void* ctx, BOOL add);
	VIEW3D_API void         __stdcall View3D_WindowInvalidatedCB      (View3DWindow window, View3D_InvalidatedCB invalidated_cb, void* ctx, BOOL add);
	VIEW3D_API void         __stdcall View3D_WindowRenderingCB        (View3DWindow window, View3D_RenderCB rendering_cb, void* ctx, BOOL add);
	VIEW3D_API void         __stdcall View3d_WindowSceneChangedCB     (View3DWindow window, View3D_SceneChangedCB scene_changed_cb, void* ctx, BOOL add);
	VIEW3D_API void         __stdcall View3D_WindowAddObject          (View3DWindow window, View3DObject object);
	VIEW3D_API void         __stdcall View3D_WindowRemoveObject       (View3DWindow window, View3DObject object);
	VIEW3D_API void         __stdcall View3D_WindowRemoveAllObjects   (View3DWindow window);
	VIEW3D_API BOOL         __stdcall View3D_WindowHasObject          (View3DWindow window, View3DObject object, BOOL search_children);
	VIEW3D_API int          __stdcall View3D_WindowObjectCount        (View3DWindow window);
	VIEW3D_API void         __stdcall View3D_WindowEnumGuids          (View3DWindow window, View3D_EnumGuidsCB enum_guids_cb, void* ctx);
	VIEW3D_API void         __stdcall View3D_WindowEnumObjects        (View3DWindow window, View3D_EnumObjectsCB enum_objects_cb, void* ctx);
	VIEW3D_API void         __stdcall View3D_WindowEnumObjectsById    (View3DWindow window, View3D_EnumObjectsCB enum_objects_cb, void* ctx, GUID const* context_ids, int include_count, int exclude_count);
	VIEW3D_API void         __stdcall View3D_WindowAddObjectsById     (View3DWindow window, GUID const* context_ids, int include_count, int exclude_count);
	VIEW3D_API void         __stdcall View3D_WindowRemoveObjectsById  (View3DWindow window, GUID const* context_ids, int include_count, int exclude_count);
	VIEW3D_API void         __stdcall View3D_WindowAddGizmo           (View3DWindow window, View3DGizmo giz);
	VIEW3D_API void         __stdcall View3D_WindowRemoveGizmo        (View3DWindow window, View3DGizmo giz);
	VIEW3D_API View3DBBox   __stdcall View3D_WindowSceneBounds        (View3DWindow window, EView3DSceneBounds bounds, int except_count, GUID const* except);
	VIEW3D_API BOOL         __stdcall View3D_WindowAnimating          (View3DWindow window);
	VIEW3D_API double       __stdcall View3D_WindowAnimTimeGet        (View3DWindow window);
	VIEW3D_API void         __stdcall View3D_WindowAnimTimeSet        (View3DWindow window, double time_s);
	VIEW3D_API void         __stdcall View3D_WindowAnimControl        (View3DWindow window, EView3DAnimCommand command, double time);
	VIEW3D_API void         __stdcall View3D_WindowAnimEventCBSet     (View3DWindow window, View3D_AnimationCB anim_cb, void* ctx, BOOL add);
	VIEW3D_API void         __stdcall View3D_WindowHitTest            (View3DWindow window, View3DHitTestRay const* rays, View3DHitTestResult* hits, int ray_count, float snap_distance, EView3DHitTestFlags flags, GUID const* context_ids, int include_count, int exclude_count);
	VIEW3D_API void         __stdcall View3D_WindowEnvMapSet          (View3DWindow window, View3DCubeMap env_map);

	// Camera
	VIEW3D_API void                  __stdcall View3D_CameraToWorldGet      (View3DWindow window, View3DM4x4& c2w);
	VIEW3D_API void                  __stdcall View3D_CameraToWorldSet      (View3DWindow window, View3DM4x4 const& c2w);
	VIEW3D_API void                  __stdcall View3D_CameraPositionSet     (View3DWindow window, View3DV4 position, View3DV4 lookat, View3DV4 up);
	VIEW3D_API void                  __stdcall View3D_CameraCommit          (View3DWindow window);
	VIEW3D_API BOOL                  __stdcall View3D_CameraOrthographicGet (View3DWindow window);
	VIEW3D_API void                  __stdcall View3D_CameraOrthographicSet (View3DWindow window, BOOL on);
	VIEW3D_API float                 __stdcall View3D_CameraFocusDistanceGet(View3DWindow window);
	VIEW3D_API void                  __stdcall View3D_CameraFocusDistanceSet(View3DWindow window, float dist);
	VIEW3D_API void                  __stdcall View3D_CameraFocusPointGet   (View3DWindow window, View3DV4& position);
	VIEW3D_API void                  __stdcall View3D_CameraFocusPointSet   (View3DWindow window, View3DV4 position);
	VIEW3D_API void                  __stdcall View3D_CameraViewRectSet     (View3DWindow window, float width, float height, float dist);
	VIEW3D_API float                 __stdcall View3D_CameraAspectGet       (View3DWindow window);
	VIEW3D_API void                  __stdcall View3D_CameraAspectSet       (View3DWindow window, float aspect);
	VIEW3D_API float                 __stdcall View3D_CameraFovXGet         (View3DWindow window);
	VIEW3D_API void                  __stdcall View3D_CameraFovXSet         (View3DWindow window, float fovX);
	VIEW3D_API float                 __stdcall View3D_CameraFovYGet         (View3DWindow window);
	VIEW3D_API void                  __stdcall View3D_CameraFovYSet         (View3DWindow window, float fovY);
	VIEW3D_API void                  __stdcall View3D_CameraFovSet          (View3DWindow window, float fovX, float fovY);
	VIEW3D_API void                  __stdcall View3D_CameraBalanceFov      (View3DWindow window, float fov);
	VIEW3D_API void                  __stdcall View3D_CameraClipPlanesGet   (View3DWindow window, float& near_, float& far_, BOOL focus_relative);
	VIEW3D_API void                  __stdcall View3D_CameraClipPlanesSet   (View3DWindow window, float near_, float far_, BOOL focus_relative);
	VIEW3D_API EView3DCameraLockMask __stdcall View3D_CameraLockMaskGet     (View3DWindow window);
	VIEW3D_API void                  __stdcall View3D_CameraLockMaskSet     (View3DWindow window, EView3DCameraLockMask mask);
	VIEW3D_API View3DV4              __stdcall View3D_CameraAlignAxisGet    (View3DWindow window);
	VIEW3D_API void                  __stdcall View3D_CameraAlignAxisSet    (View3DWindow window, View3DV4 axis);
	VIEW3D_API void                  __stdcall View3D_CameraResetZoom       (View3DWindow window);
	VIEW3D_API float                 __stdcall View3D_CameraZoomGet         (View3DWindow window);
	VIEW3D_API void                  __stdcall View3D_CameraZoomSet         (View3DWindow window, float zoom);
	VIEW3D_API void                  __stdcall View3D_ResetView             (View3DWindow window, View3DV4 forward, View3DV4 up, float dist, BOOL preserve_aspect, BOOL commit);
	VIEW3D_API void                  __stdcall View3D_ResetViewBBox         (View3DWindow window, View3DBBox bbox, View3DV4 forward, View3DV4 up, float dist, BOOL preserve_aspect, BOOL commit);
	VIEW3D_API View3DV2              __stdcall View3D_ViewArea              (View3DWindow window, float dist);
	VIEW3D_API BOOL                  __stdcall View3D_MouseNavigate         (View3DWindow window, View3DV2 ss_pos, EView3DNavOp nav_op, BOOL nav_start_or_end);
	VIEW3D_API BOOL                  __stdcall View3D_MouseNavigateZ        (View3DWindow window, View3DV2 ss_pos, float delta, BOOL along_ray);
	VIEW3D_API BOOL                  __stdcall View3D_Navigate              (View3DWindow window, float dx, float dy, float dz);
	VIEW3D_API View3DV2              __stdcall View3D_SSPointToNSSPoint     (View3DWindow window, View3DV2 screen);
	VIEW3D_API View3DV4              __stdcall View3D_NSSPointToWSPoint     (View3DWindow window, View3DV4 screen);
	VIEW3D_API View3DV4              __stdcall View3D_WSPointToNSSPoint     (View3DWindow window, View3DV4 world);
	VIEW3D_API void                  __stdcall View3D_NSSPointToWSRay       (View3DWindow window, View3DV4 screen, View3DV4& ws_point, View3DV4& ws_direction);
	VIEW3D_API EView3DNavOp          __stdcall View3D_MouseBtnToNavOp       (int mk);

	// Lights
	VIEW3D_API BOOL        __stdcall View3D_LightPropertiesGet       (View3DWindow window, View3DLight& light);
	VIEW3D_API void        __stdcall View3D_LightPropertiesSet       (View3DWindow window, View3DLight const& light);
	VIEW3D_API void        __stdcall View3D_LightSource              (View3DWindow window, View3DV4 position, View3DV4 direction, BOOL camera_relative);
	VIEW3D_API void        __stdcall View3D_LightShowDialog          (View3DWindow window);

	// Objects
	VIEW3D_API GUID              __stdcall View3D_ObjectContextIdGet       (View3DObject object);
	VIEW3D_API View3DObject      __stdcall View3D_ObjectCreateLdr          (wchar_t const* ldr_script, BOOL file, GUID const* context_id, View3DIncludes const* includes);
	VIEW3D_API View3DObject      __stdcall View3D_ObjectCreate             (char const* name, View3DColour colour, int vcount, int icount, int ncount, View3DVertex const* verts, UINT16 const* indices, View3DNugget const* nuggets, GUID const& context_id);
	VIEW3D_API View3DObject      __stdcall View3D_ObjectCreateEditCB       (char const* name, View3DColour colour, int vcount, int icount, int ncount, View3D_EditObjectCB edit_cb, void* ctx, GUID const& context_id);
	VIEW3D_API View3DObject      __stdcall View3D_ObjectCreateInstance     (View3DObject object);
	VIEW3D_API void              __stdcall View3D_ObjectEdit               (View3DObject object, View3D_EditObjectCB edit_cb, void* ctx);
	VIEW3D_API void              __stdcall View3D_ObjectUpdate             (View3DObject object, wchar_t const* ldr_script, EView3DUpdateObject flags);
	VIEW3D_API void              __stdcall View3D_ObjectDelete             (View3DObject object);
	VIEW3D_API View3DObject      __stdcall View3D_ObjectGetRoot            (View3DObject object);
	VIEW3D_API View3DObject      __stdcall View3D_ObjectGetParent          (View3DObject object);
	VIEW3D_API View3DObject      __stdcall View3D_ObjectGetChildByName     (View3DObject object, char const* name);
	VIEW3D_API View3DObject      __stdcall View3D_ObjectGetChildByIndex    (View3DObject object, int index);
	VIEW3D_API int               __stdcall View3D_ObjectChildCount         (View3DObject object);
	VIEW3D_API void              __stdcall View3D_ObjectEnumChildren       (View3DObject object, View3D_EnumObjectsCB enum_objects_cb, void* ctx);
	VIEW3D_API BSTR              __stdcall View3D_ObjectNameGetBStr        (View3DObject object);
	VIEW3D_API char const*       __stdcall View3D_ObjectNameGet            (View3DObject object);
	VIEW3D_API void              __stdcall View3D_ObjectNameSet            (View3DObject object, char const* name);
	VIEW3D_API BSTR              __stdcall View3D_ObjectTypeGetBStr        (View3DObject object);
	VIEW3D_API char const*       __stdcall View3D_ObjectTypeGet            (View3DObject object);
	VIEW3D_API View3DM4x4        __stdcall View3D_ObjectO2WGet             (View3DObject object, char const* name);
	VIEW3D_API void              __stdcall View3D_ObjectO2WSet             (View3DObject object, View3DM4x4 const& o2w, char const* name);
	VIEW3D_API View3DM4x4        __stdcall View3D_ObjectO2PGet             (View3DObject object, char const* name);
	VIEW3D_API void              __stdcall View3D_ObjectO2PSet             (View3DObject object, View3DM4x4 const& o2p, char const* name);
	VIEW3D_API BOOL              __stdcall View3D_ObjectVisibilityGet      (View3DObject object, char const* name);
	VIEW3D_API void              __stdcall View3D_ObjectVisibilitySet      (View3DObject obj, BOOL visible, char const* name);
	VIEW3D_API EView3DFlags      __stdcall View3D_ObjectFlagsGet           (View3DObject object, char const* name);
	VIEW3D_API void              __stdcall View3D_ObjectFlagsSet           (View3DObject object, EView3DFlags flags, BOOL state, char const* name);
	VIEW3D_API EView3DSortGroup  __stdcall View3D_ObjectSortGroupGet       (View3DObject object, char const* name);
	VIEW3D_API void              __stdcall View3D_ObjectSortGroupSet       (View3DObject object, EView3DSortGroup group, char const* name);
	VIEW3D_API EView3DNuggetFlag __stdcall View3D_ObjectNuggetFlagsGet     (View3DObject object, char const* name, int index);
	VIEW3D_API void              __stdcall View3D_ObjectNuggetFlagsSet     (View3DObject object, EView3DNuggetFlag flags, BOOL state, char const* name, int index);
	VIEW3D_API View3DColour      __stdcall View3D_ObjectNuggetTintGet      (View3DObject object, char const* name, int index);
	VIEW3D_API void              __stdcall View3D_ObjectNuggetTintSet      (View3DObject object, View3DColour colour, char const* name, int index);
	VIEW3D_API View3DColour      __stdcall View3D_ObjectColourGet          (View3DObject object, BOOL base_colour, char const* name);
	VIEW3D_API void              __stdcall View3D_ObjectColourSet          (View3DObject object, View3DColour colour, UINT32 mask, char const* name, EView3DColourOp op, float op_value);
	VIEW3D_API float             __stdcall View3D_ObjectReflectivityGet    (View3DObject object, char const* name);
	VIEW3D_API void              __stdcall View3D_ObjectReflectivitySet    (View3DObject object, float reflectivity, char const* name);
	VIEW3D_API BOOL              __stdcall View3D_ObjectWireframeGet       (View3DObject object, char const* name);
	VIEW3D_API void              __stdcall View3D_ObjectWireframeSet       (View3DObject object, BOOL wireframe, char const* name);
	VIEW3D_API void              __stdcall View3D_ObjectResetColour        (View3DObject object, char const* name);
	VIEW3D_API void              __stdcall View3D_ObjectSetTexture         (View3DObject object, View3DTexture tex, char const* name);
	VIEW3D_API View3DBBox        __stdcall View3D_ObjectBBoxMS             (View3DObject object, int include_children);

	// Materials
	VIEW3D_API View3DTexture __stdcall View3D_TextureFromStock            (EView3DStockTexture tex);
	VIEW3D_API View3DTexture __stdcall View3D_TextureCreate               (UINT32 width, UINT32 height, void const* data, UINT32 data_size, View3DTextureOptions const& options);
	VIEW3D_API View3DTexture __stdcall View3D_TextureCreateFromUri        (wchar_t const* resource, UINT32 width, UINT32 height, View3DTextureOptions const& options);
	VIEW3D_API View3DCubeMap __stdcall View3D_CubeMapCreateFromUri        (wchar_t const* resource, UINT32 width, UINT32 height, View3DCubeMapOptions const& options);
	VIEW3D_API void          __stdcall View3D_TextureLoadSurface          (View3DTexture tex, int level, char const* tex_filepath, RECT const* dst_rect, RECT const* src_rect, UINT32 filter, View3DColour colour_key);
	VIEW3D_API void          __stdcall View3D_TextureRelease              (View3DTexture tex);
	VIEW3D_API void          __stdcall View3D_TextureGetInfo              (View3DTexture tex, View3DImageInfo& info);
	VIEW3D_API EView3DResult __stdcall View3D_TextureGetInfoFromFile      (char const* tex_filepath, View3DImageInfo& info);
	VIEW3D_API void          __stdcall View3D_TextureSetFilterAndAddrMode (View3DTexture tex, D3D11_FILTER filter, D3D11_TEXTURE_ADDRESS_MODE addrU, D3D11_TEXTURE_ADDRESS_MODE addrV);
	VIEW3D_API HDC           __stdcall View3D_TextureGetDC                (View3DTexture tex, BOOL discard);
	VIEW3D_API void          __stdcall View3D_TextureReleaseDC            (View3DTexture tex);
	VIEW3D_API void          __stdcall View3D_TextureResize               (View3DTexture tex, UINT32 width, UINT32 height, BOOL all_instances, BOOL preserve);
	VIEW3D_API void          __stdcall View3d_TexturePrivateDataGet       (View3DTexture tex, GUID const& guid, UINT& size, void* data);
	VIEW3D_API void          __stdcall View3d_TexturePrivateDataSet       (View3DTexture tex, GUID const& guid, UINT size, void const* data);
	VIEW3D_API void          __stdcall View3d_TexturePrivateDataIFSet     (View3DTexture tex, GUID const& guid, IUnknown* pointer);
	VIEW3D_API ULONG         __stdcall View3D_TextureRefCount             (View3DTexture tex);
	VIEW3D_API View3DTexture __stdcall View3D_TextureRenderTarget         (View3DWindow window);
	VIEW3D_API void          __stdcall View3D_TextureResolveAA            (View3DTexture dst, View3DTexture src);
	VIEW3D_API View3DTexture __stdcall View3D_TextureFromShared           (IUnknown* shared_resource, View3DTextureOptions const& options);
	VIEW3D_API View3DTexture __stdcall View3D_CreateDx9RenderTarget       (HWND hwnd, UINT width, UINT height, View3DTextureOptions const& options, HANDLE* shared_handle);

	// Rendering
	VIEW3D_API void            __stdcall View3D_Invalidate             (View3DWindow window, BOOL erase);
	VIEW3D_API void            __stdcall View3D_InvalidateRect         (View3DWindow window, RECT const* rect, BOOL erase);
	VIEW3D_API void            __stdcall View3D_Render                 (View3DWindow window);
	VIEW3D_API void            __stdcall View3D_Present                (View3DWindow window);
	VIEW3D_API void            __stdcall View3D_Validate               (View3DWindow window);
	VIEW3D_API void            __stdcall View3D_RenderTargetRestore    (View3DWindow window);
	VIEW3D_API void            __stdcall View3D_RenderTargetSet        (View3DWindow window, View3DTexture render_target, View3DTexture depth_buffer);
	VIEW3D_API void            __stdcall View3D_RenderTargetSaveAsMain (View3DWindow window);
	VIEW3D_API void            __stdcall View3D_BackBufferSizeGet      (View3DWindow window, int& width, int& height);
	VIEW3D_API void            __stdcall View3D_BackBufferSizeSet      (View3DWindow window, int width, int height);
	VIEW3D_API View3DViewport  __stdcall View3D_Viewport               (View3DWindow window);
	VIEW3D_API void            __stdcall View3D_SetViewport            (View3DWindow window, View3DViewport vp);
	VIEW3D_API EView3DFillMode __stdcall View3D_FillModeGet            (View3DWindow window);
	VIEW3D_API void            __stdcall View3D_FillModeSet            (View3DWindow window, EView3DFillMode mode);
	VIEW3D_API EView3DCullMode __stdcall View3D_CullModeGet            (View3DWindow window);
	VIEW3D_API void            __stdcall View3D_CullModeSet            (View3DWindow window, EView3DCullMode mode);
	VIEW3D_API unsigned int    __stdcall View3D_BackgroundColourGet    (View3DWindow window);
	VIEW3D_API void            __stdcall View3D_BackgroundColourSet    (View3DWindow window, unsigned int aarrggbb);
	VIEW3D_API int             __stdcall View3D_MultiSamplingGet       (View3DWindow window);
	VIEW3D_API void            __stdcall View3D_MultiSamplingSet       (View3DWindow window, int multisampling);

	// Tools
	VIEW3D_API void __stdcall View3D_ObjectManagerShow        (View3DWindow window, BOOL show);
	VIEW3D_API BOOL __stdcall View3D_MeasureToolVisible       (View3DWindow window);
	VIEW3D_API void __stdcall View3D_ShowMeasureTool          (View3DWindow window, BOOL show);
	VIEW3D_API BOOL __stdcall View3D_AngleToolVisible         (View3DWindow window);
	VIEW3D_API void __stdcall View3D_ShowAngleTool            (View3DWindow window, BOOL show);

	// Gizmos
	VIEW3D_API View3DGizmo      __stdcall View3D_GizmoCreate       (EView3DGizmoMode mode, View3DM4x4 const& o2w);
	VIEW3D_API void             __stdcall View3D_GizmoDelete       (View3DGizmo gizmo);
	VIEW3D_API void             __stdcall View3D_GizmoMovedCBSet   (View3DGizmo gizmo, View3D_GizmoMovedCB cb, void* ctx, BOOL add);
	VIEW3D_API void             __stdcall View3D_GizmoAttach       (View3DGizmo gizmo, View3DObject obj);
	VIEW3D_API void             __stdcall View3D_GizmoDetach       (View3DGizmo gizmo, View3DObject obj);
	VIEW3D_API float            __stdcall View3D_GizmoScaleGet     (View3DGizmo gizmo);
	VIEW3D_API void             __stdcall View3D_GizmoScaleSet     (View3DGizmo gizmo, float scale);
	VIEW3D_API EView3DGizmoMode __stdcall View3D_GizmoGetMode      (View3DGizmo gizmo);
	VIEW3D_API void             __stdcall View3D_GizmoSetMode      (View3DGizmo gizmo, EView3DGizmoMode mode);
	VIEW3D_API View3DM4x4       __stdcall View3D_GizmoGetO2W       (View3DGizmo gizmo);
	VIEW3D_API void             __stdcall View3D_GizmoSetO2W       (View3DGizmo gizmo, View3DM4x4 const& o2w);
	VIEW3D_API View3DM4x4       __stdcall View3D_GizmoGetOffset    (View3DGizmo gizmo);
	VIEW3D_API BOOL             __stdcall View3D_GizmoEnabled      (View3DGizmo gizmo);
	VIEW3D_API void             __stdcall View3D_GizmoSetEnabled   (View3DGizmo gizmo, BOOL enabled);
	VIEW3D_API BOOL             __stdcall View3D_GizmoManipulating (View3DGizmo gizmo);

	// Miscellaneous
	VIEW3D_API void       __stdcall View3D_Flush                    ();
	VIEW3D_API BOOL       __stdcall View3D_TranslateKey             (View3DWindow window, int key_code);
	VIEW3D_API BOOL       __stdcall View3D_DepthBufferEnabledGet    (View3DWindow window);
	VIEW3D_API void       __stdcall View3D_DepthBufferEnabledSet    (View3DWindow window, BOOL enabled);
	VIEW3D_API BOOL       __stdcall View3D_FocusPointVisibleGet     (View3DWindow window);
	VIEW3D_API void       __stdcall View3D_FocusPointVisibleSet     (View3DWindow window, BOOL show);
	VIEW3D_API void       __stdcall View3D_FocusPointSizeSet        (View3DWindow window, float size);
	VIEW3D_API BOOL       __stdcall View3D_OriginVisibleGet         (View3DWindow window);
	VIEW3D_API void       __stdcall View3D_OriginVisibleSet         (View3DWindow window, BOOL show);
	VIEW3D_API void       __stdcall View3D_OriginSizeSet            (View3DWindow window, float size);
	VIEW3D_API BOOL       __stdcall View3D_BBoxesVisibleGet         (View3DWindow window);
	VIEW3D_API void       __stdcall View3D_BBoxesVisibleSet         (View3DWindow window, BOOL visible);
	VIEW3D_API BOOL       __stdcall View3D_SelectionBoxVisibleGet   (View3DWindow window);
	VIEW3D_API void       __stdcall View3D_SelectionBoxVisibleSet   (View3DWindow window, BOOL visible);
	VIEW3D_API void       __stdcall View3D_SelectionBoxPosition     (View3DWindow window, View3DBBox const& bbox, View3DM4x4 const& o2w);
	VIEW3D_API void       __stdcall View3D_SelectionBoxFitToSelected(View3DWindow window);
	VIEW3D_API GUID       __stdcall View3D_DemoSceneCreate          (View3DWindow window);
	VIEW3D_API void       __stdcall View3D_DemoSceneDelete          ();
	VIEW3D_API BSTR       __stdcall View3D_ExampleScriptBStr        ();
	VIEW3D_API BSTR       __stdcall View3D_AutoCompleteTemplatesBStr();
	VIEW3D_API void       __stdcall View3D_DemoScriptShow           (View3DWindow window);
	VIEW3D_API View3DM4x4 __stdcall View3D_ParseLdrTransform        (wchar_t const* ldr_script);
	VIEW3D_API BSTR       __stdcall View3D_ObjectAddressAt          (wchar_t const* ldr_script, int64_t position);
	VIEW3D_API ULONG      __stdcall View3D_RefCount                 (IUnknown* pointer);

	// Ldr Editor Ctrl
	VIEW3D_API HWND __stdcall View3D_LdrEditorCreate          (HWND parent);
	VIEW3D_API void __stdcall View3D_LdrEditorDestroy         (HWND hwnd);
	VIEW3D_API void __stdcall View3D_LdrEditorCtrlInit        (HWND scintilla_control, BOOL dark);
}

// Conversion to/from maths types
#ifdef __cplusplus
namespace view3d
{
	// View3D to custom type conversion.
	// Specialise this to convert to/from View3D types to a custom type
	// Include "pr/view3d/pr_conv.h" if using pr types
	template <typename TTo, typename TFrom> struct Convert
	{
		static TTo To(TFrom const&) { static_assert(false, "No conversion from this type available"); }
	};
	template <typename TTo, typename TFrom> inline TTo To(TFrom const& from)
	{
		return Convert<TTo,TFrom>::To(from);
	}

	struct Vertex :View3DVertex
	{
		void set(View3DV4 p, View3DColour c, View3DV4 n, View3DV2 const& t) noexcept
		{
			pos = p;
			col = c;
			norm = n;
			tex = t;
		}
	};
	struct WindowOptions :View3DWindowOptions
	{
		WindowOptions() noexcept
			:View3DWindowOptions()
		{
			m_error_cb                  = nullptr;
			m_error_cb_ctx              = nullptr;
			m_gdi_compatible_backbuffer = FALSE;
			m_multisampling             = 4;
			m_dbg_name                  = "";
		}
	};
	struct TextureOptions :View3DTextureOptions
	{
		TextureOptions() noexcept
			:View3DTextureOptions()
		{
			m_format         = DXGI_FORMAT_R8G8B8A8_UNORM;
			m_mips           = 0;
			m_filter         = D3D11_FILTER_MIN_MAG_MIP_LINEAR;
			m_addrU          = D3D11_TEXTURE_ADDRESS_CLAMP;
			m_addrV          = D3D11_TEXTURE_ADDRESS_CLAMP;
			m_bind_flags     = D3D11_BIND_FLAG(0);
			m_misc_flags     = D3D11_RESOURCE_MISC_FLAG(0);
			m_colour_key     = 0;
			m_has_alpha      = false;
			m_gdi_compatible = false;
		}
	};
	struct UpdateModelKeep :View3DUpdateModelKeep
	{
		enum class EKeep { None, All };
		UpdateModelKeep(EKeep keep = EKeep::None) noexcept
		{
			m_name        = keep == EKeep::All;
			m_transform   = keep == EKeep::All;
			m_context_id  = keep == EKeep::All;
			m_children    = keep == EKeep::All;
			m_colour      = keep == EKeep::All;
			m_colour_mask = keep == EKeep::All;
			m_wireframe   = keep == EKeep::All;
			m_visibility  = keep == EKeep::All;
			m_animation   = keep == EKeep::All;
			m_step_data   = keep == EKeep::All;
			m_user_data   = keep == EKeep::All;
		}
	};
}
#endif

