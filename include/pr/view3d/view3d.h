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
		namespace ldr { struct LdrObject; }
		namespace ldr { struct LdrGizmo; }
		namespace rdr { struct Texture2D; }
	}
	namespace view3d
	{
		typedef pr::ldr::LdrObject Object;
		typedef pr::ldr::LdrGizmo  Gizmo;
		typedef pr::rdr::Texture2D Texture;
		struct Window;
	}
	typedef unsigned char*   View3DContext;
	typedef view3d::Window*  View3DWindow;
	typedef view3d::Object*  View3DObject;
	typedef view3d::Gizmo*   View3DGizmo;
	typedef view3d::Texture* View3DTexture;
#else
	typedef void* View3DContext;
	typedef void* View3DWindow;
	typedef void* View3DObject;
	typedef void* View3DGizmo;
	typedef void* View3DTexture;
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
		Solid,
		Wireframe,
		SolidWire,
	};
	enum class EView3DCullMode :int
	{
		None,
		Back,
		Front,
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
		Invalid   = D3D11_PRIMITIVE_TOPOLOGY_UNDEFINED,
		PointList = D3D11_PRIMITIVE_TOPOLOGY_POINTLIST,
		LineList  = D3D11_PRIMITIVE_TOPOLOGY_LINELIST,
		LineStrip = D3D11_PRIMITIVE_TOPOLOGY_LINESTRIP,
		TriList   = D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST,
		TriStrip  = D3D11_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP,
	};
	enum class EView3DShaderVS :int
	{
		Standard = 0,
	};
	enum class EView3DShaderPS :int
	{
		Standard = 0,
	};
	enum class EView3DShaderGS :int
	{
		Standard = 0,

		// Point sprite data: v2(width,height) bool(depth)
		PointSpritesGS,

		// Thick line data: float(width)
		ThickLineListGS,

		// Arrow head: float(size)
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
	enum class EView3DLight :int
	{
		Ambient,
		Directional,
		Point,
		Spot
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
		None       = 0U,
		All        = ~0U,
		Name       = 1 << 0,
		Model      = 1 << 1,
		Transform  = 1 << 2,
		Children   = 1 << 3,
		Colour     = 1 << 4,
		ColourMask = 1 << 5,
		Flags      = 1 << 6,
		Animation  = 1 << 7,
		_bitwise_operators_allowed,
	};
	enum class EView3DGizmoEvent :int // ELdrGizmoEvent 
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

		// Bitwise operators
		_bitwise_operators_allowed,
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
	enum class EView3DWindowSettings :int
	{
		BackgroundColour,
		Lighting,
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
			EView3DShaderVS m_vs;
			EView3DShaderGS m_gs;
			EView3DShaderPS m_ps;
			EView3DShaderCS m_cs;
			uint8_t m_vs_data[16];
			uint8_t m_gs_data[16];
			uint8_t m_ps_data[16];
			uint8_t m_cs_data[16];
		};
		struct ShaderMap
		{
			ShaderSet m_rstep[(int)EView3DRenderStep::_number_of];
		};

		View3DTexture m_diff_tex;
		View3DTexture m_env_map;
		ShaderMap     m_smap;
	};
	struct View3DNugget
	{
		EView3DPrim    m_topo;
		EView3DGeom    m_geom;
		UINT32         m_v0, m_v1;       // Vertex buffer range. Set to 0,0 to mean the whole buffer
		UINT32         m_i0, m_i1;       // Index buffer range. Set to 0,0 to mean the whole buffer
		BOOL           m_has_alpha;      // True of the nugget contains transparent elements
		BOOL           m_range_overlaps; // True if the nugget V/I range overlaps earlier nuggets
		View3DMaterial m_mat;
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
		float        m_inner_cos_angle;
		float        m_outer_cos_angle;
		float        m_range;
		float        m_falloff;
		float        m_cast_shadow;
		BOOL         m_on;
		BOOL         m_cam_relative;
	};
	struct View3DTextureOptions
	{
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
	struct View3DGizmoEvent
	{
		View3DGizmo       m_gizmo;
		EView3DGizmoEvent m_state;
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

	using View3D_SettingsChangedCB     = void (__stdcall *)(void* ctx, View3DWindow window, EView3DWindowSettings setting);
	using View3D_EnumGuidsCB           = BOOL (__stdcall *)(void* ctx, GUID const& context_id);
	using View3D_EnumObjectsCB         = BOOL (__stdcall *)(void* ctx, View3DObject object);
	using View3D_AddFileProgressCB     = BOOL (__stdcall *)(void* ctx, GUID const& context_id, wchar_t const* filepath, long long file_offset, BOOL complete);
	using View3D_SourcesChangedCB      = void (__stdcall *)(void* ctx, EView3DSourcesChangedReason reason, BOOL before);
	using View3D_InvalidatedCB         = void (__stdcall *)(void* ctx, View3DWindow window);
	using View3D_RenderCB              = void (__stdcall *)(void* ctx, View3DWindow window);
	using View3D_SceneChangedCB        = void (__stdcall *)(void* ctx, View3DWindow window, View3DSceneChanged const&);
	using View3D_GizmoMovedCB          = void (__stdcall *)(void* ctx, View3DGizmoEvent const& args);
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
	VIEW3D_API GUID          __stdcall View3D_LoadScriptSource      (wchar_t const* filepath, BOOL additional, View3DIncludes const* includes);
	VIEW3D_API GUID          __stdcall View3D_LoadScript            (wchar_t const* ldr_script, BOOL file, GUID const* context_id, View3DIncludes const* includes);
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
	VIEW3D_API void         __stdcall View3D_WindowSceneChangedSuspend(View3DWindow window, BOOL suspend);
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
	VIEW3D_API float        __stdcall View3D_WindowAnimTimeGet        (View3DWindow window);
	VIEW3D_API void         __stdcall View3D_WindowAnimTimeSet        (View3DWindow window, float time_s);
	VIEW3D_API void         __stdcall View3D_WindowHitTest            (View3DWindow window, View3DHitTestRay const* rays, View3DHitTestResult* hits, int ray_count, float snap_distance, EView3DHitTestFlags flags, GUID const* context_ids, int include_count, int exclude_count);

	// Camera
	VIEW3D_API void                  __stdcall View3D_CameraToWorldGet      (View3DWindow window, View3DM4x4& c2w);
	VIEW3D_API void                  __stdcall View3D_CameraToWorldSet      (View3DWindow window, View3DM4x4 const& c2w);
	VIEW3D_API void                  __stdcall View3D_CameraPositionSet     (View3DWindow window, View3DV4 position, View3DV4 lookat, View3DV4 up);
	VIEW3D_API void                  __stdcall View3D_CameraCommit          (View3DWindow window);
	VIEW3D_API BOOL                  __stdcall View3D_CameraOrthographic    (View3DWindow window);
	VIEW3D_API void                  __stdcall View3D_CameraOrthographicSet (View3DWindow window, BOOL on);
	VIEW3D_API float                 __stdcall View3D_CameraFocusDistance   (View3DWindow window);
	VIEW3D_API void                  __stdcall View3D_CameraSetFocusDistance(View3DWindow window, float dist);
	VIEW3D_API void                  __stdcall View3D_CameraSetViewRect     (View3DWindow window, float width, float height, float dist);
	VIEW3D_API float                 __stdcall View3D_CameraAspect          (View3DWindow window);
	VIEW3D_API void                  __stdcall View3D_CameraSetAspect       (View3DWindow window, float aspect);
	VIEW3D_API float                 __stdcall View3D_CameraFovXGet         (View3DWindow window);
	VIEW3D_API void                  __stdcall View3D_CameraFovXSet         (View3DWindow window, float fovX);
	VIEW3D_API float                 __stdcall View3D_CameraFovYGet         (View3DWindow window);
	VIEW3D_API void                  __stdcall View3D_CameraFovYSet         (View3DWindow window, float fovY);
	VIEW3D_API void                  __stdcall View3D_CameraSetFov          (View3DWindow window, float fovX, float fovY);
	VIEW3D_API void                  __stdcall View3D_CameraBalanceFov      (View3DWindow window, float fov);
	VIEW3D_API void                  __stdcall View3D_CameraClipPlanesGet   (View3DWindow window, float& near_, float& far_, BOOL focus_relative);
	VIEW3D_API void                  __stdcall View3D_CameraClipPlanesSet   (View3DWindow window, float near_, float far_, BOOL focus_relative);
	VIEW3D_API void                  __stdcall View3D_CameraResetZoom       (View3DWindow window);
	VIEW3D_API float                 __stdcall View3D_CameraZoomGet         (View3DWindow window);
	VIEW3D_API void                  __stdcall View3D_CameraZoomSet         (View3DWindow window, float zoom);
	VIEW3D_API EView3DCameraLockMask __stdcall View3D_CameraLockMaskGet     (View3DWindow window);
	VIEW3D_API void                  __stdcall View3D_CameraLockMaskSet     (View3DWindow window, EView3DCameraLockMask mask);
	VIEW3D_API View3DV4              __stdcall View3D_CameraAlignAxisGet    (View3DWindow window);
	VIEW3D_API void                  __stdcall View3D_CameraAlignAxisSet    (View3DWindow window, View3DV4 axis);
	VIEW3D_API void                  __stdcall View3D_ResetView             (View3DWindow window, View3DV4 forward, View3DV4 up, float dist, BOOL preserve_aspect, BOOL commit);
	VIEW3D_API void                  __stdcall View3D_ResetViewBBox         (View3DWindow window, View3DBBox bbox, View3DV4 forward, View3DV4 up, float dist, BOOL preserve_aspect, BOOL commit);
	VIEW3D_API View3DV2              __stdcall View3D_ViewArea              (View3DWindow window, float dist);
	VIEW3D_API BOOL                  __stdcall View3D_MouseNavigate         (View3DWindow window, View3DV2 ss_pos, EView3DNavOp nav_op, BOOL nav_start_or_end);
	VIEW3D_API BOOL                  __stdcall View3D_MouseNavigateZ        (View3DWindow window, View3DV2 ss_pos, float delta, BOOL along_ray);
	VIEW3D_API BOOL                  __stdcall View3D_Navigate              (View3DWindow window, float dx, float dy, float dz);
	VIEW3D_API void                  __stdcall View3D_FocusPointGet         (View3DWindow window, View3DV4& position);
	VIEW3D_API void                  __stdcall View3D_FocusPointSet         (View3DWindow window, View3DV4 position);
	VIEW3D_API View3DV2              __stdcall View3D_SSPointToNSSPoint     (View3DWindow window, View3DV2 screen);
	VIEW3D_API View3DV4              __stdcall View3D_NSSPointToWSPoint     (View3DWindow window, View3DV4 screen);
	VIEW3D_API View3DV4              __stdcall View3D_WSPointToNSSPoint     (View3DWindow window, View3DV4 world);
	VIEW3D_API void                  __stdcall View3D_NSSPointToWSRay       (View3DWindow window, View3DV4 screen, View3DV4& ws_point, View3DV4& ws_direction);
	VIEW3D_API EView3DNavOp          __stdcall View3D_MouseBtnToNavOp       (int mk);

	// Lights
	VIEW3D_API void        __stdcall View3D_LightProperties          (View3DWindow window, View3DLight& light);
	VIEW3D_API void        __stdcall View3D_SetLightProperties       (View3DWindow window, View3DLight const& light);
	VIEW3D_API void        __stdcall View3D_LightSource              (View3DWindow window, View3DV4 position, View3DV4 direction, BOOL camera_relative);
	VIEW3D_API void        __stdcall View3D_ShowLightingDlg          (View3DWindow window);

	// Objects
	VIEW3D_API GUID            __stdcall View3D_ObjectContextIdGet       (View3DObject object);
	VIEW3D_API View3DObject    __stdcall View3D_ObjectCreateLdr          (wchar_t const* ldr_script, BOOL file, GUID const* context_id, View3DIncludes const* includes);
	VIEW3D_API View3DObject    __stdcall View3D_ObjectCreate             (char const* name, View3DColour colour, int vcount, int icount, int ncount, View3DVertex const* verts, UINT16 const* indices, View3DNugget const* nuggets, GUID const& context_id);
	VIEW3D_API View3DObject    __stdcall View3D_ObjectCreateEditCB       (char const* name, View3DColour colour, int vcount, int icount, int ncount, View3D_EditObjectCB edit_cb, void* ctx, GUID const& context_id);
	VIEW3D_API View3DObject    __stdcall View3D_ObjectCreateInstance     (View3DObject object);
	VIEW3D_API void            __stdcall View3D_ObjectEdit               (View3DObject object, View3D_EditObjectCB edit_cb, void* ctx);
	VIEW3D_API void            __stdcall View3D_ObjectUpdate             (View3DObject object, wchar_t const* ldr_script, EView3DUpdateObject flags);
	VIEW3D_API void            __stdcall View3D_ObjectDelete             (View3DObject object);
	VIEW3D_API View3DObject    __stdcall View3D_ObjectGetRoot            (View3DObject object);
	VIEW3D_API View3DObject    __stdcall View3D_ObjectGetParent          (View3DObject object);
	VIEW3D_API View3DObject    __stdcall View3D_ObjectGetChildByName     (View3DObject object, char const* name);
	VIEW3D_API View3DObject    __stdcall View3D_ObjectGetChildByIndex    (View3DObject object, int index);
	VIEW3D_API int             __stdcall View3D_ObjectChildCount         (View3DObject object);
	VIEW3D_API void            __stdcall View3D_ObjectEnumChildren       (View3DObject object, View3D_EnumObjectsCB enum_objects_cb, void* ctx);
	VIEW3D_API BSTR            __stdcall View3D_ObjectNameGetBStr        (View3DObject object);
	VIEW3D_API char const*     __stdcall View3D_ObjectNameGet            (View3DObject object);
	VIEW3D_API void            __stdcall View3D_ObjectNameSet            (View3DObject object, char const* name);
	VIEW3D_API BSTR            __stdcall View3D_ObjectTypeGetBStr        (View3DObject object);
	VIEW3D_API char const*     __stdcall View3D_ObjectTypeGet            (View3DObject object);
	VIEW3D_API View3DM4x4      __stdcall View3D_ObjectO2WGet             (View3DObject object, char const* name);
	VIEW3D_API void            __stdcall View3D_ObjectO2WSet             (View3DObject object, View3DM4x4 const& o2w, char const* name);
	VIEW3D_API View3DM4x4      __stdcall View3D_ObjectO2PGet             (View3DObject object, char const* name);
	VIEW3D_API void            __stdcall View3D_ObjectO2PSet             (View3DObject object, View3DM4x4 const& o2p, char const* name);
	VIEW3D_API BOOL            __stdcall View3D_ObjectVisibilityGet      (View3DObject object, char const* name);
	VIEW3D_API void            __stdcall View3D_ObjectVisibilitySet      (View3DObject obj, BOOL visible, char const* name);
	VIEW3D_API EView3DFlags    __stdcall View3D_ObjectFlagsGet           (View3DObject object, char const* name);
	VIEW3D_API void            __stdcall View3D_ObjectFlagsSet           (View3DObject object, EView3DFlags flags, BOOL state, char const* name);
	VIEW3D_API View3DColour    __stdcall View3D_ObjectColourGet          (View3DObject object, BOOL base_colour, char const* name);
	VIEW3D_API void            __stdcall View3D_ObjectColourSet          (View3DObject object, View3DColour colour, UINT32 mask, char const* name);
	VIEW3D_API BOOL            __stdcall View3D_ObjectWireframeGet       (View3DObject object, char const* name);
	VIEW3D_API void            __stdcall View3D_ObjectWireframeSet       (View3DObject object, BOOL wireframe, char const* name);
	VIEW3D_API void            __stdcall View3D_ObjectResetColour        (View3DObject object, char const* name);
	VIEW3D_API void            __stdcall View3D_ObjectSetTexture         (View3DObject object, View3DTexture tex, char const* name);
	VIEW3D_API View3DBBox      __stdcall View3D_ObjectBBoxMS             (View3DObject object, int include_children);

	// Materials
	VIEW3D_API View3DTexture __stdcall View3D_TextureCreate               (UINT32 width, UINT32 height, void const* data, UINT32 data_size, View3DTextureOptions const& options);
	VIEW3D_API View3DTexture __stdcall View3D_TextureCreateFromFile       (wchar_t const* tex_filepath, UINT32 width, UINT32 height, View3DTextureOptions const& options);
	VIEW3D_API void          __stdcall View3D_TextureLoadSurface          (View3DTexture tex, int level, char const* tex_filepath, RECT const* dst_rect, RECT const* src_rect, UINT32 filter, View3DColour colour_key);
	VIEW3D_API void          __stdcall View3D_TextureDelete               (View3DTexture tex);
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
	VIEW3D_API View3DGizmo      __stdcall View3D_GizmoCreate              (EView3DGizmoMode mode, View3DM4x4 const& o2w);
	VIEW3D_API void             __stdcall View3D_GizmoDelete              (View3DGizmo gizmo);
	VIEW3D_API void             __stdcall View3D_GizmoAttachCB            (View3DGizmo gizmo, View3D_GizmoMovedCB cb, void* ctx);
	VIEW3D_API void             __stdcall View3D_GizmoDetachCB            (View3DGizmo gizmo, View3D_GizmoMovedCB cb);
	VIEW3D_API void             __stdcall View3D_GizmoAttach              (View3DGizmo gizmo, View3DObject obj);
	VIEW3D_API void             __stdcall View3D_GizmoDetach              (View3DGizmo gizmo, View3DObject obj);
	VIEW3D_API float            __stdcall View3D_GizmoScaleGet            (View3DGizmo gizmo);
	VIEW3D_API void             __stdcall View3D_GizmoScaleSet            (View3DGizmo gizmo, float scale);
	VIEW3D_API EView3DGizmoMode __stdcall View3D_GizmoGetMode             (View3DGizmo gizmo);
	VIEW3D_API void             __stdcall View3D_GizmoSetMode             (View3DGizmo gizmo, EView3DGizmoMode mode);
	VIEW3D_API View3DM4x4       __stdcall View3D_GizmoGetO2W              (View3DGizmo gizmo);
	VIEW3D_API void             __stdcall View3D_GizmoSetO2W              (View3DGizmo gizmo, View3DM4x4 const& o2w);
	VIEW3D_API View3DM4x4       __stdcall View3D_GizmoGetOffset           (View3DGizmo gizmo);
	VIEW3D_API BOOL             __stdcall View3D_GizmoEnabled             (View3DGizmo gizmo);
	VIEW3D_API void             __stdcall View3D_GizmoSetEnabled          (View3DGizmo gizmo, BOOL enabled);
	VIEW3D_API BOOL             __stdcall View3D_GizmoManipulating        (View3DGizmo gizmo);

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
	VIEW3D_API void       __stdcall View3D_DemoScriptShow           (View3DWindow window);
	VIEW3D_API View3DM4x4 __stdcall View3D_ParseLdrTransform        (char const* ldr_script);
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
		void set(View3DV4 p, View3DColour c, View3DV4 n, View3DV2 const& t) { pos = p; col = c; norm = n; tex = t; }
	};
	struct WindowOptions :View3DWindowOptions
	{
		WindowOptions() :View3DWindowOptions()
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
		TextureOptions() :View3DTextureOptions()
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
		UpdateModelKeep(EKeep keep = EKeep::None)
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

