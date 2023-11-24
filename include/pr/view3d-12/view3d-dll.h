//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
// This is the dll API header.
#pragma once

#ifdef VIEW3D_EXPORTS
#define VIEW3D_API __declspec(dllexport)
#else
#define VIEW3D_API __declspec(dllimport)
#endif

// ** No dependencies except standard includes **
#include <cstdint>
#include <windows.h>
#include <d3d12.h>

namespace pr
{
	// Forward declare types
	namespace rdr12
	{
		struct Context;
		struct V3dWindow;
		struct LdrObject;
		struct LdrGizmo;
		struct Texture2D;
		struct TextureCube;
	}

	namespace view3d
	{
		using DllHandle = unsigned char const*;
		using Object = pr::rdr12::LdrObject*;
		using Gizmo = pr::rdr12::LdrGizmo*;
		using Texture = pr::rdr12::Texture2D*;
		using CubeMap = pr::rdr12::TextureCube*;
		using Window = rdr12::V3dWindow*;
		using ReportErrorCB = void (__stdcall *)(void* ctx, wchar_t const* msg, wchar_t const* filepath, int line, int64_t pos);

		#pragma region Enumerations
		enum class EResult :int
		{
			Success,
			Failed,
		};
		#if 0 //todo
		enum class EView3DFillMode :int
		{
			Default   = 0,
			Points    = 1,
			Wireframe = D3D11_FILL_MODE::D3D11_FILL_WIREFRAME,
			Solid     = D3D11_FILL_MODE::D3D11_FILL_SOLID,
			SolidWire = 4,
		};
		enum class EView3DCullMode :int
		{
			Default = 0,
			None    = D3D11_CULL_MODE::D3D11_CULL_NONE,
			Front   = D3D11_CULL_MODE::D3D11_CULL_FRONT,
			Back    = D3D11_CULL_MODE::D3D11_CULL_BACK,
		};
		enum class EView3DGeom :int // pr::rdr::EGeom
		{
			Unknown = 0,
			Vert    = 1 << 0, // Object space 3D position
			Colr    = 1 << 1, // Diffuse base colour
			Norm    = 1 << 2, // Object space 3D normal
			Tex0    = 1 << 3, // Diffuse texture
			_flags_enum = 0,
		};
		enum class EView3DTopo :int
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

			// Excluded from shadow map render steps
			ShadowCastExclude = 1 << 3,

			_flags_enum = 0,
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
		#endif
		enum class ELight :int
		{
			Ambient,
			Directional,
			Point,
			Spot
		};
		enum class EAnimCommand : int
		{
			Reset, // Reset the the 'time' value
			Play,  // Run continuously using 'time' as the step size, or real time if 'time' == 0
			Stop,  // Stop at the current time.
			Step,  // Step by 'time' (can be positive or negative)
		};
		#if 0 // todo
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
			_flags_enum = 0,
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
		#endif
		enum class ENavOp :int // pr::camera::ENavOp
		{
			None      = 0,
			Translate = 1 << 0,
			Rotate    = 1 << 1,
			Zoom      = 1 << 2,
			_flags_enum = 0,
		};
		#if 0 // todo
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
			_flags_enum = 0,
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
			_arith_enum = 0,
		};
		enum class EView3DSceneBounds :int
		{
			All,
			Selected,
			Visible,
		};
		#endif
		enum class ESourcesChangedReason :int
		{
			NewData,
			Reload,
			Removal,
		};
		enum class ESceneChanged :int
		{
			ObjectsAdded,
			ObjectsRemoved,
			GizmoAdded,
			GizmoRemoved,
		};
		#if 0 // todo
		enum class EView3DHitTestFlags :int
		{
			Faces = 1 << 0,
			Edges = 1 << 1,
			Verts = 1 << 2,
			_flags_enum = 0,
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
		#endif
		enum class ESettings :int
		{
			// Upper 2-bytes = category
			// Lower 2-bytes = specific property that changed.
			None = 0,

			General                     = 1 << 16,
			General_FocusPointVisible   = General | 1 << 0,
			General_OriginPointVisible  = General | 1 << 1,
			General_SelectionBoxVisible = General | 1 << 2,

			Scene                  = 1 << 17,
			Scene_BackgroundColour = Scene | 1 << 0,
			Scene_Multisampling    = Scene | 1 << 1,
			Scene_FillMode         = Scene | 1 << 2,
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

			_flags_enum = 0,
		};
		#pragma endregion

		#pragma region Structures
		using Colour = unsigned int;
		struct Vec2
		{
			float x, y;
		};
		struct Vec4
		{
			float x, y, z, w;
		};
		struct Mat4x4
		{
			Vec4 x, y, z, w;
		};
		struct BBox
		{
			Vec4 centre;
			Vec4 radius;
		};
		struct Vertex
		{
			Vec4 pos;
			Vec4 norm;
			Vec2 tex;
			Colour col;
			uint32_t pad;
		};
		#if 0 // todo
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
		#endif
		struct Light
		{
			Vec4 m_position;
			Vec4 m_direction;
			ELight m_type;
			Colour m_ambient;
			Colour m_diffuse;
			Colour m_specular;
			float m_specular_power;
			float m_range;
			float m_falloff;
			float m_inner_angle;
			float m_outer_angle;
			float m_cast_shadow;
			BOOL m_cam_relative;
			BOOL m_on;
		};
		#if 0
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
		#endif
		struct CubeMapOptions
		{
			Mat4x4      m_cube2w;
			char const* m_dbg_name;
		};
		struct WindowOptions
		{
			ReportErrorCB m_error_cb;
			void*         m_error_cb_ctx;
			BOOL          m_gdi_compatible_backbuffer;
			int           m_multisampling;
			char const*   m_dbg_name;
		};
		#if 0 // todo
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
		#endif
		struct Viewport
		{
			float m_x;         // (x,y,x+width,y+width) is in backbuffer pixels, *NOT* window DIP.
			float m_y;         // Typically the backbuffer is the same size as the true screen pixels
			float m_width;     // Typically the BB width
			float m_height;    // Typically the BB height
			float m_min_depth; // Typically 0.0f
			float m_max_depth; // Typically 1.0f
			int m_screen_w;    // The screen width in DIP
			int m_screen_h;    // The screen height in DIP
		};
		struct Includes
		{
			wchar_t const* m_include_paths; // A comma or semicolon separated list of search directories
			HMODULE        m_modules[16];   // An array of binary modules that contain resources. '0' means 'this' module
			int            m_module_count;  // The number of valid module values in 'm_modules'
			// (ToDo) A string lookup table
		};
		struct SceneChanged
		{
			// How the scene was changed
			ESceneChanged m_change_type;

			// An array of the context ids that changed
			GUID const* m_ctx_ids;

			// The length of the 'm_ctx_ids' array
			int m_count;

			// Pointer to the object that changed (for single object changes only)
			Object m_object;
		};
		#if 0 //todo
		struct View3DAnimEvent
		{
			// The state change type
			EView3DAnimCommand m_command;

			// The current animation clock value
			double m_clock;
		};
		#endif
		#pragma endregion

		// Callbacks
		using ReportErrorCB     = void(__stdcall *)(void* ctx, wchar_t const* msg, wchar_t const* filepath, int line, int64_t pos);
		using SettingsChangedCB = void(__stdcall *)(void* ctx, Window window, ESettings setting);
		using AddFileProgressCB = void(__stdcall *)(void* ctx, GUID const& context_id, wchar_t const* filepath, long long file_offset, BOOL complete, BOOL* cancel);
		using SourcesChangedCB  = void(__stdcall *)(void* ctx, ESourcesChangedReason reason, BOOL before);
		using InvalidatedCB     = void(__stdcall *)(void* ctx, Window window);
		using EnumGuidsCB       = bool(__stdcall *)(void* ctx, GUID const& context_id);
		using EnumObjectsCB     = bool(__stdcall *)(void* ctx, Object object);
		#if 0 // todo
		using View3D_OnAddCB               = void (__stdcall *)(void* ctx, GUID const& context_id, BOOL before);
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
	#endif
	}
}

// C linkage for API functions
extern "C"
{
	// Dll Context ****************************
	
	// Initialise calls are reference counted and must be matched with Shutdown calls
	// 'initialise_error_cb' is used to report dll initialisation errors only (i.e. it isn't stored)
	// Note: this function is not thread safe, avoid race calls
	VIEW3D_API pr::view3d::DllHandle __stdcall View3D_Initialise(pr::view3d::ReportErrorCB global_error_cb, void* ctx);
	VIEW3D_API void __stdcall View3D_Shutdown(pr::view3d::DllHandle context);

	// This error callback is called for errors that are associated with the dll (rather than with a window).
	VIEW3D_API void __stdcall View3D_GlobalErrorCBSet(pr::view3d::ReportErrorCB error_cb, void* ctx, BOOL add);

	#if 0 // todo
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
	#endif

	// Windows ********************************

	// Create/Destroy a window
	VIEW3D_API pr::view3d::Window __stdcall View3D_WindowCreate(HWND hwnd, pr::view3d::WindowOptions const& opts);
	VIEW3D_API void __stdcall View3D_WindowDestroy(pr::view3d::Window window);
	
	// Add/Remove a window error callback. Note: The callback function can be called in a worker thread context.
	VIEW3D_API void __stdcall View3D_WindowErrorCBSet(pr::view3d::Window window, pr::view3d::ReportErrorCB error_cb, void* ctx, BOOL add);

	// Get/Set the window settings (as ldr script string)
	VIEW3D_API wchar_t const* __stdcall View3D_WindowSettingsGet(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_WindowSettingsSet(pr::view3d::Window window, wchar_t const* settings);

	// Get/Set the dimensions of the render target. Note: Not equal to window size for non-96 dpi screens!
	// In set, if 'width' and 'height' are zero, the RT is resized to the associated window automatically.
	VIEW3D_API BOOL __stdcall View3D_WindowBackBufferSizeGet(pr::view3d::Window window, int& width, int& height);
	VIEW3D_API void __stdcall View3D_WindowBackBufferSizeSet(pr::view3d::Window window, int width, int height);

	// Get/Set the window viewport (and clipping area)
	VIEW3D_API pr::view3d::Viewport __stdcall View3D_WindowViewportGet(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_WindowViewportSet(pr::view3d::Window window, pr::view3d::Viewport const& vp);

	// Set a notification handler for when a window setting changes
	VIEW3D_API void __stdcall View3D_WindowSettingsChangedCB(pr::view3d::Window window, pr::view3d::SettingsChangedCB settings_changed_cb, void* ctx, BOOL add);

	// Add an object to a window
	VIEW3D_API void __stdcall View3D_WindowAddObject(pr::view3d::Window window, pr::view3d::Object object);

	// Enumerate the object collection guids associated with 'window'
	VIEW3D_API void __stdcall View3D_WindowEnumGuids(pr::view3d::Window window, pr::view3d::EnumGuidsCB enum_guids_cb, void* ctx);

	// Enumerate the objects associated with 'window'
	VIEW3D_API void __stdcall View3D_WindowEnumObjects(pr::view3d::Window window, pr::view3d::EnumObjectsCB enum_objects_cb, void* ctx);
	VIEW3D_API void __stdcall View3D_WindowEnumObjectsById(pr::view3d::Window window, pr::view3d::EnumObjectsCB enum_objects_cb, void* ctx, GUID const* context_ids, int include_count, int exclude_count);

	// Render the window
	VIEW3D_API void __stdcall View3D_WindowRender(pr::view3d::Window window);

	// Signal the window is invalidated. This does not automatically trigger rendering. Use InvalidatedCB.
	VIEW3D_API void __stdcall View3D_WindowInvalidate(pr::view3d::Window window, BOOL erase);
	VIEW3D_API void __stdcall View3D_WindowInvalidateRect(pr::view3d::Window window, RECT const* rect, BOOL erase);

	// Register a callback for when the window is invalidated.
	// This can be used to render in response to invalidation, rather than rendering on a polling cycle.
	VIEW3D_API void __stdcall View3D_WindowInvalidatedCB(pr::view3d::Window window, pr::view3d::InvalidatedCB invalidated_cb, void* ctx, BOOL add);

	// Get/Set the window background colour
	VIEW3D_API unsigned int __stdcall View3D_WindowBackgroundColourGet(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_WindowBackgroundColourSet(pr::view3d::Window window, unsigned int argb);

	#if 0 // todo
	VIEW3D_API void           __stdcall View3D_WindowRenderingCB        (View3DWindow window, View3D_RenderCB rendering_cb, void* ctx, BOOL add);
	VIEW3D_API void           __stdcall View3D_WindowSceneChangedCB     (View3DWindow window, View3D_SceneChangedCB scene_changed_cb, void* ctx, BOOL add);
	VIEW3D_API void           __stdcall View3D_WindowRemoveObject       (View3DWindow window, View3DObject object);
	VIEW3D_API void           __stdcall View3D_WindowRemoveAllObjects   (View3DWindow window);
	VIEW3D_API BOOL           __stdcall View3D_WindowHasObject          (View3DWindow window, View3DObject object, BOOL search_children);
	VIEW3D_API int            __stdcall View3D_WindowObjectCount        (View3DWindow window);
	VIEW3D_API void           __stdcall View3D_WindowAddObjectsById     (View3DWindow window, GUID const* context_ids, int include_count, int exclude_count);
	VIEW3D_API void           __stdcall View3D_WindowRemoveObjectsById  (View3DWindow window, GUID const* context_ids, int include_count, int exclude_count);
	VIEW3D_API void           __stdcall View3D_WindowAddGizmo           (View3DWindow window, View3DGizmo giz);
	VIEW3D_API void           __stdcall View3D_WindowRemoveGizmo        (View3DWindow window, View3DGizmo giz);
	VIEW3D_API View3DBBox     __stdcall View3D_WindowSceneBounds        (View3DWindow window, EView3DSceneBounds bounds, int except_count, GUID const* except);
	VIEW3D_API BOOL           __stdcall View3D_WindowAnimating          (View3DWindow window);
	VIEW3D_API double         __stdcall View3D_WindowAnimTimeGet        (View3DWindow window);
	VIEW3D_API void           __stdcall View3D_WindowAnimTimeSet        (View3DWindow window, double time_s);
	VIEW3D_API void           __stdcall View3D_WindowAnimControl        (View3DWindow window, EView3DAnimCommand command, double time);
	VIEW3D_API void           __stdcall View3D_WindowAnimEventCBSet     (View3DWindow window, View3D_AnimationCB anim_cb, void* ctx, BOOL add);
	VIEW3D_API void           __stdcall View3D_WindowHitTestObjects     (View3DWindow window, View3DHitTestRay const* rays, View3DHitTestResult* hits, int ray_count, float snap_distance, EView3DHitTestFlags flags, View3DObject const* objects, int object_count);
	VIEW3D_API void           __stdcall View3D_WindowHitTestByCtx       (View3DWindow window, View3DHitTestRay const* rays, View3DHitTestResult* hits, int ray_count, float snap_distance, EView3DHitTestFlags flags, GUID const* context_ids, int include_count, int exclude_count);
	VIEW3D_API View3DV2       __stdcall View3D_WindowDpiScale           (View3DWindow window);
	#endif
	VIEW3D_API void __stdcall View3D_WindowEnvMapSet(pr::view3d::Window window, pr::view3d::CubeMap env_map);

	#if 0
	// Rendering
	VIEW3D_API void            __stdcall View3D_Present                (View3DWindow window);
	VIEW3D_API void            __stdcall View3D_Validate               (View3DWindow window);
	VIEW3D_API void            __stdcall View3D_RenderTargetRestore    (View3DWindow window);
	VIEW3D_API void            __stdcall View3D_RenderTargetSet        (View3DWindow window, View3DTexture render_target, View3DTexture depth_buffer, BOOL is_new_main_rt);
	VIEW3D_API EView3DFillMode __stdcall View3D_FillModeGet            (View3DWindow window);
	VIEW3D_API void            __stdcall View3D_FillModeSet            (View3DWindow window, EView3DFillMode mode);
	VIEW3D_API EView3DCullMode __stdcall View3D_CullModeGet            (View3DWindow window);
	VIEW3D_API void            __stdcall View3D_CullModeSet            (View3DWindow window, EView3DCullMode mode);
	VIEW3D_API int             __stdcall View3D_MultiSamplingGet       (View3DWindow window);
	VIEW3D_API void            __stdcall View3D_MultiSamplingSet       (View3DWindow window, int multisampling);
	#endif

	// Camera *********************************

	// Position the camera and focus distance
	VIEW3D_API void __stdcall View3D_CameraPositionSet(pr::view3d::Window window, pr::view3d::Vec4 position, pr::view3d::Vec4 lookat, pr::view3d::Vec4 up);

	// Get/Set the current camera to world transform
	VIEW3D_API pr::view3d::Mat4x4 __stdcall View3D_CameraToWorldGet(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_CameraToWorldSet(pr::view3d::Window window, pr::view3d::Mat4x4 const& c2w);

	// Move the scene camera using the mouse
	VIEW3D_API BOOL __stdcall View3D_MouseNavigate(pr::view3d::Window window, pr::view3d::Vec2 ss_pos, pr::view3d::ENavOp nav_op, BOOL nav_start_or_end);
	VIEW3D_API BOOL __stdcall View3D_MouseNavigateZ(pr::view3d::Window window, pr::view3d::Vec2 ss_pos, float delta, BOOL along_ray);

	// Convert a point between 'window' screen space and normalised screen space
	VIEW3D_API pr::view3d::Vec2 __stdcall View3D_SSPointToNSSPoint(pr::view3d::Window window, pr::view3d::Vec2 screen);
	VIEW3D_API pr::view3d::Vec2 __stdcall View3D_NSSPointToSSPoint(pr::view3d::Window window, pr::view3d::Vec2 nss_point);

	// Convert a point between world space and normalised screen space.
	// The x,y components of 'screen' should be in normalised screen space, i.e. (-1,-1)->(1,1)
	// The z component should be the world space distance from the camera
	VIEW3D_API pr::view3d::Vec4 __stdcall View3D_NSSPointToWSPoint(pr::view3d::Window window, pr::view3d::Vec4 screen);
	VIEW3D_API pr::view3d::Vec4 __stdcall View3D_WSPointToNSSPoint(pr::view3d::Window window, pr::view3d::Vec4 world);

	// Return a point and direction in world space corresponding to a normalised screen space point.
	// The x,y components of 'screen' should be in normalised screen space, i.e. (-1,-1)->(1,1)
	// The z component should be the world space distance from the camera
	VIEW3D_API void __stdcall View3D_NSSPointToWSRay(pr::view3d::Window window, pr::view3d::Vec4 screen, pr::view3d::Vec4& ws_point, pr::view3d::Vec4& ws_direction);
	#if 0 // todo
	VIEW3D_API BOOL                  __stdcall View3D_Navigate              (View3DWindow window, float dx, float dy, float dz);
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
	VIEW3D_API EView3DNavOp          __stdcall View3D_MouseBtnToNavOp       (int mk);
	#endif

	// Lights *********************************

	// Get/Set the properties of the global light
	VIEW3D_API pr::view3d::Light __stdcall View3D_LightPropertiesGet(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_LightPropertiesSet(pr::view3d::Window window, pr::view3d::Light const& light);
	
	// Set the global light source for a window
	VIEW3D_API void __stdcall View3D_LightSource(pr::view3d::Window window, pr::view3d::Vec4 position, pr::view3d::Vec4 direction, BOOL camera_relative);
	#if 0 // todo
	VIEW3D_API void        __stdcall View3D_LightShowDialog          (View3DWindow window);
	#endif

	// Objects ********************************

	// Notes:
	// 'name' parameter on object get/set functions:
	//   If 'name' is null, then the state of the root object is returned
	//   If 'name' begins with '#' then the remainder of the name is treated as a regular expression

	// Create an graphics object from ldr script, either a string or a file 
	VIEW3D_API pr::view3d::Object __stdcall View3D_ObjectCreateLdrW(wchar_t const* ldr_script, BOOL file, GUID const* context_id, pr::view3d::Includes const* includes);
	VIEW3D_API pr::view3d::Object __stdcall View3D_ObjectCreateLdrA(char const* ldr_script, BOOL file, GUID const* context_id, pr::view3d::Includes const* includes);

	// Delete an object, freeing its resources
	VIEW3D_API void __stdcall View3D_ObjectDelete(pr::view3d::Object object);

	// Get/Set the object's o2w transform
	VIEW3D_API pr::view3d::Mat4x4 __stdcall View3D_ObjectO2WGet(pr::view3d::Object object, char const* name);
	VIEW3D_API void __stdcall View3D_ObjectO2WSet(pr::view3d::Object object, pr::view3d::Mat4x4 const& o2w, char const* name);

	// Get/Set the reflectivity of an object (the first object to match 'name') (See LdrObject::Apply)
	VIEW3D_API float __stdcall View3D_ObjectReflectivityGet(pr::view3d::Object object, char const* name);
	VIEW3D_API void __stdcall View3D_ObjectReflectivitySet(pr::view3d::Object object, float reflectivity, char const* name);

	#if 0 // todo
	VIEW3D_API View3DObject      __stdcall View3D_ObjectCreateP3DFile      (char const* name, View3DColour colour, wchar_t const* p3d_filepath, GUID const* context_id);
	VIEW3D_API View3DObject      __stdcall View3D_ObjectCreateP3DStream    (char const* name, View3DColour colour, size_t size, void const* p3d_data, GUID const* context_id);
	VIEW3D_API View3DObject      __stdcall View3D_ObjectCreate             (char const* name, View3DColour colour, int vcount, int icount, int ncount, View3DVertex const* verts, UINT16 const* indices, View3DNugget const* nuggets, GUID const& context_id);
	VIEW3D_API View3DObject      __stdcall View3D_ObjectCreateEditCB       (char const* name, View3DColour colour, int vcount, int icount, int ncount, View3D_EditObjectCB edit_cb, void* ctx, GUID const& context_id);
	VIEW3D_API View3DObject      __stdcall View3D_ObjectCreateInstance     (View3DObject object);
	VIEW3D_API void              __stdcall View3D_ObjectEdit               (View3DObject object, View3D_EditObjectCB edit_cb, void* ctx);
	VIEW3D_API void              __stdcall View3D_ObjectUpdate             (View3DObject object, wchar_t const* ldr_script, EView3DUpdateObject flags);
	VIEW3D_API GUID              __stdcall View3D_ObjectContextIdGet       (View3DObject object);
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
	VIEW3D_API View3DM4x4        __stdcall View3D_ObjectO2PGet             (View3DObject object, char const* name);
	VIEW3D_API void              __stdcall View3D_ObjectO2PSet             (View3DObject object, View3DM4x4 const& o2p, char const* name);
	VIEW3D_API BOOL              __stdcall View3D_ObjectVisibilityGet      (View3DObject object, char const* name);
	VIEW3D_API void              __stdcall View3D_ObjectVisibilitySet      (View3DObject object, BOOL visible, char const* name);
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
	VIEW3D_API BOOL              __stdcall View3D_ObjectWireframeGet       (View3DObject object, char const* name);
	VIEW3D_API void              __stdcall View3D_ObjectWireframeSet       (View3DObject object, BOOL wireframe, char const* name);
	VIEW3D_API BOOL              __stdcall View3D_ObjectNormalsGet         (View3DObject object, char const* name);
	VIEW3D_API void              __stdcall View3D_ObjectNormalsSet         (View3DObject object, BOOL show, char const* name);
	VIEW3D_API void              __stdcall View3D_ObjectResetColour        (View3DObject object, char const* name);
	VIEW3D_API void              __stdcall View3D_ObjectSetTexture         (View3DObject object, View3DTexture tex, char const* name);
	VIEW3D_API View3DBBox        __stdcall View3D_ObjectBBoxMS             (View3DObject object, int include_children);
#endif

	// Materials
	VIEW3D_API void __stdcall View3D_TextureRelease(pr::view3d::Texture tex);
	VIEW3D_API void __stdcall View3D_CubeMapRelease(pr::view3d::CubeMap tex);
	VIEW3D_API pr::view3d::CubeMap __stdcall View3D_CubeMapCreateFromUri(char const* resource, pr::view3d::CubeMapOptions const& options);
#if 0
	VIEW3D_API View3DTexture __stdcall View3D_TextureFromStock            (EView3DStockTexture tex);
	VIEW3D_API View3DTexture __stdcall View3D_TextureCreate               (UINT32 width, UINT32 height, void const* data, UINT32 data_size, View3DTextureOptions const& options);
	VIEW3D_API View3DTexture __stdcall View3D_TextureCreateFromStock      (EView3DStockTexture tex, View3DTextureOptions const& options);
	VIEW3D_API View3DTexture __stdcall View3D_TextureCreateFromUri        (wchar_t const* resource, UINT32 width, UINT32 height, View3DTextureOptions const& options);
	VIEW3D_API void          __stdcall View3D_TextureLoadSurface          (View3DTexture tex, int level, wchar_t const* tex_filepath, RECT const* dst_rect, RECT const* src_rect, UINT32 filter, View3DColour colour_key);
	VIEW3D_API void          __stdcall View3D_TextureGetInfo              (View3DTexture tex, View3DImageInfo& info);
	VIEW3D_API EView3DResult __stdcall View3D_TextureGetInfoFromFile      (wchar_t const* tex_filepath, View3DImageInfo& info);
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

	// Diagnostics
	VIEW3D_API BOOL         __stdcall View3D_DiagBBoxesVisibleGet     (View3DWindow window);
	VIEW3D_API void         __stdcall View3D_DiagBBoxesVisibleSet     (View3DWindow window, BOOL visible);
	VIEW3D_API float        __stdcall View3D_DiagNormalsLengthGet     (View3DWindow window);
	VIEW3D_API void         __stdcall View3D_DiagNormalsLengthSet     (View3DWindow window, float length);
	VIEW3D_API View3DColour __stdcall View3D_DiagNormalsColourGet     (View3DWindow window);
	VIEW3D_API void         __stdcall View3D_DiagNormalsColourSet     (View3DWindow window, View3DColour colour);
	VIEW3D_API View3DV2     __stdcall View3D_DiagFillModePointsSizeGet(View3DWindow window);
	VIEW3D_API void         __stdcall View3D_DiagFillModePointsSizeSet(View3DWindow window, View3DV2 size);
	#endif

	// Miscellaneous **************************

	// Create/Delete the demo scene in the given window
	VIEW3D_API GUID __stdcall View3D_DemoSceneCreate(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_DemoSceneDelete();

	#if 0
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
	VIEW3D_API BOOL       __stdcall View3D_SelectionBoxVisibleGet   (View3DWindow window);
	VIEW3D_API void       __stdcall View3D_SelectionBoxVisibleSet   (View3DWindow window, BOOL visible);
	VIEW3D_API void       __stdcall View3D_SelectionBoxPosition     (View3DWindow window, View3DBBox const& bbox, View3DM4x4 const& o2w);
	VIEW3D_API void       __stdcall View3D_SelectionBoxFitToSelected(View3DWindow window);
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


	#endif
}
