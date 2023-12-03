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
		struct Sampler;
	}

	namespace view3d
	{
		using DllHandle = unsigned char const*;
		using Object = pr::rdr12::LdrObject*;
		using Gizmo = pr::rdr12::LdrGizmo*;
		using Texture = pr::rdr12::Texture2D*;
		using CubeMap = pr::rdr12::TextureCube*;
		using Sampler = pr::rdr12::Sampler*;
		using Window = rdr12::V3dWindow*;
		using ReportErrorCB = void(__stdcall *)(void* ctx, char const* msg, char const* filepath, int line, int64_t pos);

		#pragma region Enumerations
		enum class EResult :int
		{
			Success,
			Failed,
		};
		enum class EFillMode :int
		{
			Default   = 0,
			Points    = 1,
			Wireframe = D3D12_FILL_MODE::D3D12_FILL_MODE_WIREFRAME,
			Solid     = D3D12_FILL_MODE::D3D12_FILL_MODE_SOLID,
			SolidWire = 4,
		};
		enum class ECullMode :int
		{
			Default = 0,
			None    = D3D12_CULL_MODE::D3D12_CULL_MODE_NONE,
			Front   = D3D12_CULL_MODE::D3D12_CULL_MODE_FRONT,
			Back    = D3D12_CULL_MODE::D3D12_CULL_MODE_BACK,
		};
		enum class EGeom :int // pr::rdr12::EGeom
		{
			Unknown = 0,
			Vert    = 1 << 0, // Object space 3D position
			Colr    = 1 << 1, // Diffuse base colour
			Norm    = 1 << 2, // Object space 3D normal
			Tex0    = 1 << 3, // Diffuse texture
			_flags_enum = 0,
		};
		enum class ETopo :int // pr::rdr12::ETopo
		{
			Invalid      = D3D_PRIMITIVE_TOPOLOGY::D3D_PRIMITIVE_TOPOLOGY_UNDEFINED,
			PointList    = D3D_PRIMITIVE_TOPOLOGY::D3D_PRIMITIVE_TOPOLOGY_POINTLIST,
			LineList     = D3D_PRIMITIVE_TOPOLOGY::D3D_PRIMITIVE_TOPOLOGY_LINELIST,
			LineStrip    = D3D_PRIMITIVE_TOPOLOGY::D3D_PRIMITIVE_TOPOLOGY_LINESTRIP,
			TriList      = D3D_PRIMITIVE_TOPOLOGY::D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST,
			TriStrip     = D3D_PRIMITIVE_TOPOLOGY::D3D_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP,
			LineListAdj  = D3D_PRIMITIVE_TOPOLOGY::D3D_PRIMITIVE_TOPOLOGY_LINELIST_ADJ,
			LineStripAdj = D3D_PRIMITIVE_TOPOLOGY::D3D_PRIMITIVE_TOPOLOGY_LINESTRIP_ADJ,
			TriListAdj   = D3D_PRIMITIVE_TOPOLOGY::D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST_ADJ,
			TriStripAdj  = D3D_PRIMITIVE_TOPOLOGY::D3D_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP_ADJ,
		};
		enum class ENuggetFlag :int // pr::rdr12::ENuggetFlag
		{
			None = 0,

			// Exclude this nugget when rendering a model
			Hidden = 1 << 0,

			// Set if the geometry data for the nugget contains alpha colours
			GeometryHasAlpha = 1 << 1,

			// Set if the tint colour contains alpha
			TintHasAlpha = 1 << 2,

			// Set if the diffuse texture contains alpha (and we want alpha blending, not just thresholding)
			TexDiffuseHasAlpha = 1 << 3,

			// Excluded from shadow map render steps
			ShadowCastExclude = 1 << 4,

			// Can overlap with other nuggets.
			// Set this flag to true if you want to add a nugget that overlaps the range
			// of an existing nugget. For simple models, overlapping nugget ranges is
			// usually an error, but in advanced cases it isn't.
			RangesCanOverlap = 1 << 5,

			_flags_enum = 0,
		};
		#if 0 //todo
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
		#endif
		enum class EStockTexture :int // pr::rdr12::EStockTexture
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
		enum class ECameraLockMask :int // pr::camera::ELockMask
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
		enum class EColourOp :int // pr::rdr12::EColourOp
		{
			Overwrite,
			Add,
			Subtract,
			Multiply,
			Lerp,
		};
		enum class ELdrFlags :int // sync with 'ELdrFlags'
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
		enum class ESortGroup :int // pr::rdr12::ESortGroup
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
		enum class ESceneBounds :int
		{
			All,
			Selected,
			Visible,
		};
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
		enum class EHitTestFlags :int
		{
			Faces = 1 << 0,
			Edges = 1 << 1,
			Verts = 1 << 2,
			_flags_enum = 0,
		};
		enum class ESnapType :int
		{
			NoSnap,
			Vert,
			EdgeMiddle,
			FaceCentre,
			Edge,
			Face,
		};
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
		struct Material
		{
#if 0 //todo
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
			ShaderMap     m_shader_map;
#endif
			Texture m_diff_tex;
			Colour  m_tint;
			float   m_relative_reflectivity;
		};
		struct Nugget
		{
			ETopo       m_topo;
			EGeom       m_geom;
			ECullMode   m_cull_mode;
			EFillMode   m_fill_mode;
			int         m_v0, m_v1;       // Vertex buffer range. Set to 0,0 to mean the whole buffer
			int         m_i0, m_i1;       // Index buffer range. Set to 0,0 to mean the whole buffer
			ENuggetFlag m_nflags;         // Nugget flags
			Material    m_mat;
		};
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
#if 0 // todo
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
		struct TextureOptions
		{
			Mat4x4               m_t2s;
			DXGI_FORMAT          m_format;
			int                  m_mips;
			D3D12_RESOURCE_FLAGS m_usage;
			D3D12_CLEAR_VALUE    m_clear_value;
			int                  m_multisamp;
			UINT                 m_colour_key;
			BOOL                 m_has_alpha;
			char const*          m_dbg_name;
		};
		struct CubeMapOptions
		{
			Mat4x4      m_cube2w;
			char const* m_dbg_name;
		};
		struct SamplerOptions
		{
			D3D12_FILTER               m_filter;
			D3D12_TEXTURE_ADDRESS_MODE m_addrU;
			D3D12_TEXTURE_ADDRESS_MODE m_addrV;
			D3D12_TEXTURE_ADDRESS_MODE m_addrW;
			char const*                m_dbg_name;
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
		#endif
		struct HitTestRay
		{
			// The world space origin and direction of the ray (normalisation not required)
			Vec4 m_ws_origin;
			Vec4 m_ws_direction;
		};
		struct HitTestResult
		{
			// The origin and direction of the cast ray (in world space)
			Vec4 m_ws_ray_origin;
			Vec4 m_ws_ray_direction;

			// The intercept point (in world space)
			Vec4 m_ws_intercept;

			// The object that was hit (or null)
			Object m_obj;

			// The distance from ray origin to hit point
			float m_distance;

			// How the hit point was snapped (if at all)
			ESnapType m_snap_type;
		};
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
			char const* m_include_paths; // A comma or semicolon separated list of search directories
			HMODULE     m_modules[16];   // An array of binary modules that contain resources. '0' means 'this' module
			int         m_module_count;  // The number of valid module values in 'm_modules'
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
		struct AnimEvent
		{
			// The state change type
			EAnimCommand m_command;

			// The current animation clock value
			double m_clock;
		};
		#pragma endregion

		// Callbacks
		using SettingsChangedCB     = void(__stdcall *)(void* ctx, Window window, ESettings setting);
		using AddFileProgressCB     = void(__stdcall *)(void* ctx, GUID const& context_id, char const* filepath, long long file_offset, BOOL complete, BOOL& cancel);
		using SourcesChangedCB      = void(__stdcall *)(void* ctx, ESourcesChangedReason reason, BOOL before);
		using EnumGuidsCB           = bool(__stdcall *)(void* ctx, GUID const& context_id);
		using EnumObjectsCB         = bool(__stdcall *)(void* ctx, Object object);
		using OnAddCB               = void(__stdcall *)(void* ctx, GUID const& context_id, BOOL before);
		using InvalidatedCB         = void(__stdcall *)(void* ctx, Window window);
		using RenderingCB           = void(__stdcall *)(void* ctx, Window window);
		using SceneChangedCB        = void(__stdcall *)(void* ctx, Window window, SceneChanged const&);
		using AnimationCB           = void(__stdcall *)(void* ctx, Window window, EAnimCommand command, double clock);
		using EmbeddedCodeHandlerCB = bool(__stdcall *)(void* ctx, wchar_t const*, wchar_t const*, BSTR&, BSTR&);


		#if 0 // todo
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

	// Set the callback for progress events when script sources are loaded or updated
	VIEW3D_API void __stdcall View3D_AddFileProgressCBSet(pr::view3d::AddFileProgressCB progress_cb, void* ctx, BOOL add);
	
	// Set the callback that is called when the sources are reloaded
	VIEW3D_API void __stdcall View3D_SourcesChangedCBSet(pr::view3d::SourcesChangedCB sources_changed_cb, void* ctx, BOOL add);

	// Add/Remove a callback for handling embedded code within scripts
	VIEW3D_API void __stdcall View3D_EmbeddedCodeCBSet(char const* lang, pr::view3d::EmbeddedCodeHandlerCB embedded_code_cb, void* ctx, BOOL add);

	// Return the context id for objects created from 'filepath' (if filepath is an existing source)
	VIEW3D_API BOOL __stdcall View3D_ContextIdFromFilepath(char const* filepath, GUID& id);

	// Data Sources ***************************

	// Add an ldr script source. This will create all objects with context id 'context_id' (if given, otherwise an id will be created). Concurrent calls are thread safe.
	VIEW3D_API GUID __stdcall View3D_LoadScriptFromString(char const* ldr_script, GUID const* context_id, pr::view3d::Includes const* includes, pr::view3d::OnAddCB on_add_cb, void* ctx);
	VIEW3D_API GUID __stdcall View3D_LoadScriptFromFile(char const* ldr_file, GUID const* context_id, pr::view3d::Includes const* includes, pr::view3d::OnAddCB on_add_cb, void* ctx);

	// Delete all objects and object sources
	VIEW3D_API void __stdcall View3D_DeleteAllObjects();

	// Delete all objects matching (or not matching) a context id
	VIEW3D_API void __stdcall View3D_DeleteById(GUID const* context_ids, int include_count, int exclude_count);

	// Delete all objects not displayed in any windows
	VIEW3D_API void __stdcall View3D_DeleteUnused(GUID const* context_ids, int include_count, int exclude_count);

	// Enumerate the Guids of objects in the sources collection
	VIEW3D_API void __stdcall View3D_SourceEnumGuids(pr::view3d::EnumGuidsCB enum_guids_cb, void* ctx);

	// Reload script sources. This will delete all objects associated with the script sources then reload the files creating new objects with the same context ids.
	VIEW3D_API void __stdcall View3D_ReloadScriptSources();

	// Poll for changed script sources and reload any that have changed
	VIEW3D_API void __stdcall View3D_CheckForChangedSources();

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

	// Add/Remove a callback that is called when the collection of objects associated with 'window' changes
	VIEW3D_API void __stdcall View3D_WindowSceneChangedCB(pr::view3d::Window window, pr::view3d::SceneChangedCB scene_changed_cb, void* ctx, BOOL add);

	// Add/Remove a callback that is called just prior to rendering the window
	VIEW3D_API void __stdcall View3D_WindowRenderingCB(pr::view3d::Window window, pr::view3d::RenderingCB rendering_cb, void* ctx, BOOL add);

	// Add/Remove an object to/from a window
	VIEW3D_API void __stdcall View3D_WindowAddObject(pr::view3d::Window window, pr::view3d::Object object);
	VIEW3D_API void __stdcall View3D_WindowRemoveObject(pr::view3d::Window window, pr::view3d::Object object);

	// Add/Remove a gizmo to/from a window
	VIEW3D_API void __stdcall View3D_WindowAddGizmo(pr::view3d::Window window, pr::view3d::Gizmo giz);
	VIEW3D_API void __stdcall View3D_WindowRemoveGizmo(pr::view3d::Window window, pr::view3d::Gizmo giz);

	// Add/Remove objects by context id. This function can be used to add all objects either in, or not in 'context_ids'
	VIEW3D_API void __stdcall View3D_WindowAddObjectsById(pr::view3d::Window window, GUID const* context_ids, int include_count, int exclude_count);
	VIEW3D_API void __stdcall View3D_WindowRemoveObjectsById(pr::view3d::Window window, GUID const* context_ids, int include_count, int exclude_count);

	// Remove all objects 'window'
	VIEW3D_API void __stdcall View3D_WindowRemoveAllObjects(pr::view3d::Window window);

	// Enumerate the object collection guids associated with 'window'
	VIEW3D_API void __stdcall View3D_WindowEnumGuids(pr::view3d::Window window, pr::view3d::EnumGuidsCB enum_guids_cb, void* ctx);

	// Enumerate the objects associated with 'window'
	VIEW3D_API void __stdcall View3D_WindowEnumObjects(pr::view3d::Window window, pr::view3d::EnumObjectsCB enum_objects_cb, void* ctx);
	VIEW3D_API void __stdcall View3D_WindowEnumObjectsById(pr::view3d::Window window, pr::view3d::EnumObjectsCB enum_objects_cb, void* ctx, GUID const* context_ids, int include_count, int exclude_count);

	// Return true if 'object' is among 'window's objects
	VIEW3D_API BOOL __stdcall View3D_WindowHasObject(pr::view3d::Window window, pr::view3d::Object object, BOOL search_children);

	// Return the number of objects assigned to 'window'
	VIEW3D_API int __stdcall View3D_WindowObjectCount(pr::view3d::Window window);

	// Return the bounds of a scene
	VIEW3D_API pr::view3d::BBox __stdcall View3D_WindowSceneBounds(pr::view3d::Window window, pr::view3d::ESceneBounds bounds, int except_count, GUID const* except);

	// Render the window
	VIEW3D_API void __stdcall View3D_WindowRender(pr::view3d::Window window);

	// Clear the 'invalidated' state of the window.
	VIEW3D_API void __stdcall View3D_Validate(pr::view3d::Window window);

	// Signal the window is invalidated. This does not automatically trigger rendering. Use InvalidatedCB.
	VIEW3D_API void __stdcall View3D_WindowInvalidate(pr::view3d::Window window, BOOL erase);
	VIEW3D_API void __stdcall View3D_WindowInvalidateRect(pr::view3d::Window window, RECT const* rect, BOOL erase);

	// Register a callback for when the window is invalidated. This can be used to render in response to invalidation, rather than rendering on a polling cycle.
	VIEW3D_API void __stdcall View3D_WindowInvalidatedCB(pr::view3d::Window window, pr::view3d::InvalidatedCB invalidated_cb, void* ctx, BOOL add);

	// Get/Set the window background colour
	VIEW3D_API unsigned int __stdcall View3D_WindowBackgroundColourGet(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_WindowBackgroundColourSet(pr::view3d::Window window, unsigned int argb);

	// Get/Set the fill mode for the window
	VIEW3D_API pr::view3d::EFillMode __stdcall View3D_WindowFillModeGet(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_WindowFillModeSet(pr::view3d::Window window, pr::view3d::EFillMode mode);

	// Get/Set the cull mode for a faces in window
	VIEW3D_API pr::view3d::ECullMode __stdcall View3D_WindowCullModeGet(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_WindowCullModeSet(pr::view3d::Window window, pr::view3d::ECullMode mode);

	// Get/Set the multi-sampling mode for a window
	VIEW3D_API int __stdcall View3D_MultiSamplingGet(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_MultiSamplingSet(pr::view3d::Window window, int multisampling);

	// Control animation
	VIEW3D_API void __stdcall View3D_WindowAnimControl(pr::view3d::Window window, pr::view3d::EAnimCommand command, double time);

	// Get/Set the animation time
	VIEW3D_API BOOL __stdcall View3D_WindowAnimating(pr::view3d::Window window);
	VIEW3D_API double __stdcall View3D_WindowAnimTimeGet(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_WindowAnimTimeSet(pr::view3d::Window window, double time_s);

	// Set the callback for animation events
	VIEW3D_API void __stdcall View3D_WindowAnimEventCBSet(pr::view3d::Window window, pr::view3d::AnimationCB anim_cb, void* ctx, BOOL add);

	// Return the DPI of the monitor that 'window' is displayed on
	VIEW3D_API pr::view3d::Vec2 __stdcall View3D_WindowDpiScale(pr::view3d::Window window);

	// Set the global environment map for the window
	VIEW3D_API void __stdcall View3D_WindowEnvMapSet(pr::view3d::Window window, pr::view3d::CubeMap env_map);

	// Cast a ray into the scene, returning information about what it hit.
	VIEW3D_API void __stdcall View3D_WindowHitTestObjects(pr::view3d::Window window, pr::view3d::HitTestRay const* rays, pr::view3d::HitTestResult* hits, int ray_count, float snap_distance, pr::view3d::EHitTestFlags flags, pr::view3d::Object const* objects, int object_count);
	VIEW3D_API void __stdcall View3D_WindowHitTestByCtx(pr::view3d::Window window, pr::view3d::HitTestRay const* rays, pr::view3d::HitTestResult* hits, int ray_count, float snap_distance, pr::view3d::EHitTestFlags flags, GUID const* context_ids, int include_count, int exclude_count);

	#if 0
	// Rendering
	VIEW3D_API void            __stdcall View3D_Present                (pr::view3d::Window window);
	VIEW3D_API void            __stdcall View3D_RenderTargetRestore    (pr::view3d::Window window);
	VIEW3D_API void            __stdcall View3D_RenderTargetSet        (pr::view3d::Window window, pr::view3d::Texture render_target, pr::view3d::Texture depth_buffer, BOOL is_new_main_rt);
	#endif

	// Camera *********************************

	// Position the camera and focus distance
	VIEW3D_API void __stdcall View3D_CameraPositionSet(pr::view3d::Window window, pr::view3d::Vec4 position, pr::view3d::Vec4 lookat, pr::view3d::Vec4 up);

	// Get/Set the current camera to world transform
	VIEW3D_API pr::view3d::Mat4x4 __stdcall View3D_CameraToWorldGet(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_CameraToWorldSet(pr::view3d::Window window, pr::view3d::Mat4x4 const& c2w);

	// Move the camera to a position that can see the whole scene. Set 'dist' to 0 to preserve the FoV, or a distance to set the FoV
	VIEW3D_API void __stdcall View3D_ResetView(pr::view3d::Window window, pr::view3d::Vec4 forward, pr::view3d::Vec4 up, float dist, BOOL preserve_aspect, BOOL commit);

	// Reset the camera to view a bbox. Set 'dist' to 0 to preserve the FoV, or a distance to set the FoV
	VIEW3D_API void __stdcall View3D_ResetViewBBox(pr::view3d::Window window, pr::view3d::BBox bbox, pr::view3d::Vec4 forward, pr::view3d::Vec4 up, float dist, BOOL preserve_aspect, BOOL commit);

	// Enable/Disable orthographic projection
	VIEW3D_API BOOL __stdcall View3D_CameraOrthographicGet(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_CameraOrthographicSet(pr::view3d::Window window, BOOL on);

	// Get/Set the distance to the camera focus point
	VIEW3D_API float __stdcall View3D_CameraFocusDistanceGet(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_CameraFocusDistanceSet(pr::view3d::Window window, float dist);

	// Get/Set the camera focus point position
	VIEW3D_API pr::view3d::Vec4 __stdcall View3D_CameraFocusPointGet(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_CameraFocusPointSet(pr::view3d::Window window, pr::view3d::Vec4 position);

	// Get/Set the aspect ratio for the camera field of view
	VIEW3D_API float __stdcall View3D_CameraAspectGet(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_CameraAspectSet(pr::view3d::Window window, float aspect);
	
	// Get/Set both the X and Y fields of view (i.e. set the aspect ratio). Null fov means don't change the current value.
	VIEW3D_API pr::view3d::Vec2 __stdcall View3D_CameraFovGet(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_CameraFovSet(pr::view3d::Window window, float* fovX, float* fovY);

	// Adjust the FocusDist, FovX, and FovY so that the average FOV equals 'fov'
	VIEW3D_API void __stdcall View3D_CameraBalanceFov(pr::view3d::Window window, float fov);

	// Get/Set (using fov and focus distance) the size of the perpendicular area visible to the camera at 'dist' (in world space). Use 'focus_dist != 0' to set a specific focus distance
	VIEW3D_API pr::view3d::Vec2 __stdcall View3D_CameraViewRectAtDistanceGet(pr::view3d::Window window, float dist);
	VIEW3D_API void __stdcall View3D_CameraViewRectAtDistanceSet(pr::view3d::Window window, pr::view3d::Vec2, float focus_dist);

	// Get/Set the near and far clip planes for the camera
	VIEW3D_API pr::view3d::Vec2 __stdcall View3D_CameraClipPlanesGet(pr::view3d::Window window, BOOL focus_relative);
	VIEW3D_API void __stdcall View3D_CameraClipPlanesSet(pr::view3d::Window window, float* near_, float* far_, BOOL focus_relative);

	// Get/Set the scene camera lock mask
	VIEW3D_API pr::view3d::ECameraLockMask __stdcall View3D_CameraLockMaskGet(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_CameraLockMaskSet(pr::view3d::Window window, pr::view3d::ECameraLockMask mask);
	
	// Get/Set the camera align axis
	VIEW3D_API pr::view3d::Vec4 __stdcall View3D_CameraAlignAxisGet(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_CameraAlignAxisSet(pr::view3d::Window window, pr::view3d::Vec4 axis);
	
	// Reset to the default zoom
	VIEW3D_API void __stdcall View3D_CameraResetZoom(pr::view3d::Window window);

	// Get/Set the FOV zoom
	VIEW3D_API float __stdcall View3D_CameraZoomGet(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_CameraZoomSet(pr::view3d::Window window, float zoom);

	// Commit the current O2W position as the reference position
	VIEW3D_API void __stdcall View3D_CameraCommit(pr::view3d::Window window);

	// Navigation *****************************

	// Direct movement of the camera
	VIEW3D_API BOOL __stdcall View3D_Navigate(pr::view3d::Window window, float dx, float dy, float dz);

	// Move the scene camera using the mouse
	VIEW3D_API BOOL __stdcall View3D_MouseNavigate(pr::view3d::Window window, pr::view3d::Vec2 ss_pos, pr::view3d::ENavOp nav_op, BOOL nav_start_or_end);
	VIEW3D_API BOOL __stdcall View3D_MouseNavigateZ(pr::view3d::Window window, pr::view3d::Vec2 ss_pos, float delta, BOOL along_ray);

	// Convert an MK_ macro to a default navigation operation
	VIEW3D_API pr::view3d::ENavOp __stdcall View3D_MouseBtnToNavOp(int mk);

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

	// Lights *********************************

	// Get/Set the properties of the global light
	VIEW3D_API pr::view3d::Light __stdcall View3D_LightPropertiesGet(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_LightPropertiesSet(pr::view3d::Window window, pr::view3d::Light const& light);
	
	// Set the global light source for a window
	VIEW3D_API void __stdcall View3D_LightSource(pr::view3d::Window window, pr::view3d::Vec4 position, pr::view3d::Vec4 direction, BOOL camera_relative);

	#if 0 // todo
	VIEW3D_API void __stdcall View3D_LightShowDialog(pr::view3d::Window window);
	#endif

	// Objects ********************************

	// Notes:
	// 'name' parameter on object get/set functions:
	//   If 'name' is null, then the state of the root object is returned
	//   If 'name' begins with '#' then the remainder of the name is treated as a regular expression

	// Create an object from provided buffers
	VIEW3D_API pr::view3d::Object __stdcall View3D_ObjectCreate(char const* name, pr::view3d::Colour colour, int vcount, int icount, int ncount, pr::view3d::Vertex const* verts, UINT16 const* indices, pr::view3d::Nugget const* nuggets, GUID const& context_id);

	// Create an graphics object from ldr script, either a string or a file 
	VIEW3D_API pr::view3d::Object __stdcall View3D_ObjectCreateLdrW(wchar_t const* ldr_script, BOOL file, GUID const* context_id, pr::view3d::Includes const* includes);
	VIEW3D_API pr::view3d::Object __stdcall View3D_ObjectCreateLdrA(char const* ldr_script, BOOL file, GUID const* context_id, pr::view3d::Includes const* includes);

	// Load a p3d model file as a view3d object
	VIEW3D_API pr::view3d::Object __stdcall View3D_ObjectCreateP3DFile(char const* name, pr::view3d::Colour colour, char const* p3d_filepath, GUID const* context_id);

	// Load a p3d model in memory as a view3d object
	VIEW3D_API pr::view3d::Object __stdcall View3D_ObjectCreateP3DStream(char const* name, pr::view3d::Colour colour, size_t size, void const* p3d_data, GUID const* context_id);

	// Delete an object, freeing its resources
	VIEW3D_API void __stdcall View3D_ObjectDelete(pr::view3d::Object object);

	// Create an instance of 'obj'
	VIEW3D_API pr::view3d::Object __stdcall View3D_ObjectCreateInstance(pr::view3d::Object object);

	// Return the context id that this object belongs to
	VIEW3D_API GUID __stdcall View3D_ObjectContextIdGet (pr::view3d::Object object);

	// Return the root object of 'object'(possibly itself)
	VIEW3D_API pr::view3d::Object __stdcall View3D_ObjectGetRoot(pr::view3d::Object object);

	// Return the immediate parent of 'object'
	VIEW3D_API pr::view3d::Object __stdcall View3D_ObjectGetParent(pr::view3d::Object object);

	// Return a child object of 'object'
	VIEW3D_API pr::view3d::Object __stdcall View3D_ObjectGetChildByName(pr::view3d::Object object, char const* name);
	VIEW3D_API pr::view3d::Object __stdcall View3D_ObjectGetChildByIndex(pr::view3d::Object object, int index);

	// Return the number of child objects of 'object'
	VIEW3D_API int __stdcall View3D_ObjectChildCount (pr::view3d::Object object);

	// Enumerate the child objects of 'object'. (Not recursive)
	VIEW3D_API void __stdcall View3D_ObjectEnumChildren (pr::view3d::Object object, pr::view3d::EnumObjectsCB enum_objects_cb, void* ctx);

	// Get/Set the name of 'object'
	VIEW3D_API BSTR __stdcall View3D_ObjectNameGetBStr(pr::view3d::Object object);
	VIEW3D_API char const* __stdcall View3D_ObjectNameGet(pr::view3d::Object object);
	VIEW3D_API void __stdcall View3D_ObjectNameSet(pr::view3d::Object object, char const* name);

	// Get the type of 'object'
	VIEW3D_API BSTR __stdcall View3D_ObjectTypeGetBStr(pr::view3d::Object object);
	VIEW3D_API char const* __stdcall View3D_ObjectTypeGet(pr::view3d::Object object);

	// Get/Set the current or base colour of an object(the first object to match 'name') (See LdrObject::Apply)
	VIEW3D_API pr::view3d::Colour __stdcall View3D_ObjectColourGet(pr::view3d::Object object, BOOL base_colour, char const* name);
	VIEW3D_API void __stdcall View3D_ObjectColourSet(pr::view3d::Object object, pr::view3d::Colour colour, UINT32 mask, char const* name, pr::view3d::EColourOp op, float op_value);
	
	// Reset the object colour back to its default
	VIEW3D_API void __stdcall View3D_ObjectResetColour(pr::view3d::Object object, char const* name);

	// Get/Set the object's o2w transform
	VIEW3D_API pr::view3d::Mat4x4 __stdcall View3D_ObjectO2WGet(pr::view3d::Object object, char const* name);
	VIEW3D_API void __stdcall View3D_ObjectO2WSet(pr::view3d::Object object, pr::view3d::Mat4x4 const& o2w, char const* name);

	// Get/Set the object to parent transform for an object.
	// This is the object to world transform for objects without parents.
	// Note: In "*Box b { 1 1 1 *o2w{*pos{1 2 3}} }" setting this transform overwrites the "*o2w{*pos{1 2 3}}".
	VIEW3D_API pr::view3d::Mat4x4 __stdcall View3D_ObjectO2PGet(pr::view3d::Object object, char const* name);
	VIEW3D_API void __stdcall View3D_ObjectO2PSet(pr::view3d::Object object, pr::view3d::Mat4x4 const& o2p, char const* name);

	// Return the model space bounding box for 'object'
	VIEW3D_API pr::view3d::BBox __stdcall View3D_ObjectBBoxMS(pr::view3d::Object object, int include_children);

	// Get/Set the object visibility. See LdrObject::Apply for docs on the format of 'name'
	VIEW3D_API BOOL __stdcall View3D_ObjectVisibilityGet(pr::view3d::Object object, char const* name);
	VIEW3D_API void __stdcall View3D_ObjectVisibilitySet(pr::view3d::Object object, BOOL visible, char const* name);

	// Get/Set wireframe mode for an object (the first object to match 'name'). (See LdrObject::Apply)
	VIEW3D_API BOOL __stdcall View3D_ObjectWireframeGet(pr::view3d::Object object, char const* name);
	VIEW3D_API void __stdcall View3D_ObjectWireframeSet(pr::view3d::Object object, BOOL wireframe, char const* name);

	// Get/Set the object flags. See LdrObject::Apply for docs on the format of 'name'
	VIEW3D_API pr::view3d::ELdrFlags __stdcall View3D_ObjectFlagsGet(pr::view3d::Object object, char const* name);
	VIEW3D_API void __stdcall View3D_ObjectFlagsSet(pr::view3d::Object object, pr::view3d::ELdrFlags flags, BOOL state, char const* name);

	// Get/Set the reflectivity of an object (the first object to match 'name') (See LdrObject::Apply)
	VIEW3D_API float __stdcall View3D_ObjectReflectivityGet(pr::view3d::Object object, char const* name);
	VIEW3D_API void __stdcall View3D_ObjectReflectivitySet(pr::view3d::Object object, float reflectivity, char const* name);

	// Get/Set the sort group for the object or its children. (See LdrObject::Apply)
	VIEW3D_API pr::view3d::ESortGroup __stdcall View3D_ObjectSortGroupGet(pr::view3d::Object object, char const* name);
	VIEW3D_API void __stdcall View3D_ObjectSortGroupSet(pr::view3d::Object object, pr::view3d::ESortGroup group, char const* name);

	// Get/Set 'show normals' mode for an object (the first object to match 'name') (See LdrObject::Apply)
	VIEW3D_API BOOL __stdcall View3D_ObjectNormalsGet(pr::view3d::Object object, char const* name);
	VIEW3D_API void __stdcall View3D_ObjectNormalsSet(pr::view3d::Object object, BOOL show, char const* name);

	// Set the texture/sampler for all nuggets of 'object' or its children. (See LdrObject::Apply)
	VIEW3D_API void __stdcall View3D_ObjectSetTexture(pr::view3d::Object object, pr::view3d::Texture tex, char const* name);
	VIEW3D_API void __stdcall View3D_ObjectSetSampler(pr::view3d::Object object, pr::view3d::Sampler sam, char const* name);

	// Get/Set the nugget flags on an object or its children (See LdrObject::Apply)
	VIEW3D_API pr::view3d::ENuggetFlag __stdcall View3D_ObjectNuggetFlagsGet(pr::view3d::Object object, char const* name, int index);
	VIEW3D_API void __stdcall View3D_ObjectNuggetFlagsSet(pr::view3d::Object object, pr::view3d::ENuggetFlag flags, BOOL state, char const* name, int index);

	// Get/Set the tint colour for a nugget within the model of an object or its children. (See LdrObject::Apply)
	VIEW3D_API pr::view3d::Colour __stdcall View3D_ObjectNuggetTintGet(pr::view3d::Object object, char const* name, int index);
	VIEW3D_API void __stdcall View3D_ObjectNuggetTintSet(pr::view3d::Object object, pr::view3d::Colour colour, char const* name, int index);


	#if 0 // todo
	VIEW3D_API pr::view3d::Object      __stdcall View3D_ObjectCreateEditCB       (char const* name, pr::view3d::Colour colour, int vcount, int icount, int ncount, View3D_EditObjectCB edit_cb, void* ctx, GUID const& context_id);
	VIEW3D_API void              __stdcall View3D_ObjectEdit               (pr::view3d::Object object, View3D_EditObjectCB edit_cb, void* ctx);
	VIEW3D_API void              __stdcall View3D_ObjectUpdate             (pr::view3d::Object object, wchar_t const* ldr_script, EView3DUpdateObject flags);
#endif

	// Materials ******************************

	// Create a texture from data in memory.
	// Set 'data' to nullptr to leave the texture uninitialised, if not null then data must point to width x height pixel data
	// of the size appropriate for the given format. e.g. uint32_t px_data[width * height] for D3DFMT_A8R8G8B8
	// Note: careful with stride, 'data' is expected to have the appropriate stride for BytesPerPixel(format) * width
	VIEW3D_API pr::view3d::Texture __stdcall View3D_TextureCreate(int width, int height, void const* data, size_t data_size, pr::view3d::TextureOptions const& options);

	// Create one of the stock textures
	VIEW3D_API pr::view3d::Texture __stdcall View3D_TextureCreateStock(pr::view3d::EStockTexture stock_texture);

	// Load a texture from file, embedded resource, or stock assets. Specify width == 0, height == 0 to use the dimensions of the file
	VIEW3D_API pr::view3d::Texture __stdcall View3D_TextureCreateFromUri(char const* resource, int width, int height, pr::view3d::TextureOptions const& options);

	// Load a cube map from file, embedded resource, or stock assets. Specify width == 0, height == 0 to use the dimensions of the file
	VIEW3D_API pr::view3d::CubeMap __stdcall View3D_CubeMapCreateFromUri(char const* resource, pr::view3d::CubeMapOptions const& options);

	// Create a texture sampler
	VIEW3D_API pr::view3d::Sampler __stdcall View3D_SamplerCreate(pr::view3d::SamplerOptions const& options);

	// Release a reference to a resources
	VIEW3D_API void __stdcall View3D_TextureRelease(pr::view3d::Texture tex);
	VIEW3D_API void __stdcall View3D_CubeMapRelease(pr::view3d::CubeMap tex);
	VIEW3D_API void __stdcall View3D_SamplerRelease(pr::view3d::Sampler sam);

#if 0
	VIEW3D_API void          __stdcall View3D_TextureLoadSurface          (pr::view3d::Texture tex, int level, wchar_t const* tex_filepath, RECT const* dst_rect, RECT const* src_rect, UINT32 filter, pr::view3d::Colour colour_key);
	VIEW3D_API void          __stdcall View3D_TextureGetInfo              (pr::view3d::Texture tex, View3DImageInfo& info);
	VIEW3D_API EView3DResult __stdcall View3D_TextureGetInfoFromFile      (wchar_t const* tex_filepath, View3DImageInfo& info);
	VIEW3D_API void          __stdcall View3D_TextureSetFilterAndAddrMode (pr::view3d::Texture tex, D3D11_FILTER filter, D3D11_TEXTURE_ADDRESS_MODE addrU, D3D11_TEXTURE_ADDRESS_MODE addrV);
	VIEW3D_API HDC           __stdcall View3D_TextureGetDC                (pr::view3d::Texture tex, BOOL discard);
	VIEW3D_API void          __stdcall View3D_TextureReleaseDC            (pr::view3d::Texture tex);
	VIEW3D_API void          __stdcall View3D_TextureResize               (pr::view3d::Texture tex, UINT32 width, UINT32 height, BOOL all_instances, BOOL preserve);
	VIEW3D_API void          __stdcall View3d_TexturePrivateDataGet       (pr::view3d::Texture tex, GUID const& guid, UINT& size, void* data);
	VIEW3D_API void          __stdcall View3d_TexturePrivateDataSet       (pr::view3d::Texture tex, GUID const& guid, UINT size, void const* data);
	VIEW3D_API void          __stdcall View3d_TexturePrivateDataIFSet     (pr::view3d::Texture tex, GUID const& guid, IUnknown* pointer);
	VIEW3D_API ULONG         __stdcall View3D_TextureRefCount             (pr::view3d::Texture tex);
	VIEW3D_API pr::view3d::Texture __stdcall View3D_TextureRenderTarget         (pr::view3d::Window window);
	VIEW3D_API void          __stdcall View3D_TextureResolveAA            (pr::view3d::Texture dst, pr::view3d::Texture src);
	VIEW3D_API pr::view3d::Texture __stdcall View3D_TextureFromShared           (IUnknown* shared_resource, pr::view3d::TextureOptions const& options);
	VIEW3D_API pr::view3d::Texture __stdcall View3D_CreateDx9RenderTarget       (HWND hwnd, UINT width, UINT height, pr::view3d::TextureOptions const& options, HANDLE* shared_handle);
#endif

	// Gizmos *********************************

#if 0
	VIEW3D_API View3DGizmo      __stdcall View3D_GizmoCreate       (EView3DGizmoMode mode, View3DM4x4 const& o2w);
	VIEW3D_API void             __stdcall View3D_GizmoDelete       (View3DGizmo gizmo);
	VIEW3D_API void             __stdcall View3D_GizmoMovedCBSet   (View3DGizmo gizmo, View3D_GizmoMovedCB cb, void* ctx, BOOL add);
	VIEW3D_API void             __stdcall View3D_GizmoAttach       (View3DGizmo gizmo, pr::view3d::Object obj);
	VIEW3D_API void             __stdcall View3D_GizmoDetach       (View3DGizmo gizmo, pr::view3d::Object obj);
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
#endif

	// Diagnostics ****************************

	// Get/Set whether object bounding boxes are visible
	VIEW3D_API BOOL __stdcall View3D_DiagBBoxesVisibleGet(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_DiagBBoxesVisibleSet(pr::view3d::Window window, BOOL visible);

	// Get/Set the length of the vertex normals
	VIEW3D_API float __stdcall View3D_DiagNormalsLengthGet(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_DiagNormalsLengthSet(pr::view3d::Window window, float length);

	// Get/Set the colour of the vertex normals
	VIEW3D_API pr::view3d::Colour __stdcall View3D_DiagNormalsColourGet(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_DiagNormalsColourSet(pr::view3d::Window window, pr::view3d::Colour colour);

	VIEW3D_API pr::view3d::Vec2 __stdcall View3D_DiagFillModePointsSizeGet(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_DiagFillModePointsSizeSet(pr::view3d::Window window, pr::view3d::Vec2 size);

	// Miscellaneous **************************

	// Return true if the focus point is visible. Add/Remove the focus point to a window.
	VIEW3D_API BOOL __stdcall View3D_FocusPointVisibleGet(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_FocusPointVisibleSet(pr::view3d::Window window, BOOL show);

	// Create/Delete the demo scene in the given window
	VIEW3D_API GUID __stdcall View3D_DemoSceneCreate(pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_DemoSceneDelete();

#if 0
	VIEW3D_API void       __stdcall View3D_Flush                    ();
	VIEW3D_API BOOL       __stdcall View3D_TranslateKey             (pr::view3d::Window window, int key_code);
	VIEW3D_API BOOL       __stdcall View3D_DepthBufferEnabledGet    (pr::view3d::Window window);
	VIEW3D_API void       __stdcall View3D_DepthBufferEnabledSet    (pr::view3d::Window window, BOOL enabled);
	VIEW3D_API void       __stdcall View3D_FocusPointSizeSet        (pr::view3d::Window window, float size);
	VIEW3D_API BOOL       __stdcall View3D_OriginVisibleGet         (pr::view3d::Window window);
	VIEW3D_API void       __stdcall View3D_OriginVisibleSet         (pr::view3d::Window window, BOOL show);
	VIEW3D_API void       __stdcall View3D_OriginSizeSet            (pr::view3d::Window window, float size);
	VIEW3D_API BOOL       __stdcall View3D_SelectionBoxVisibleGet   (pr::view3d::Window window);
	VIEW3D_API void       __stdcall View3D_SelectionBoxVisibleSet   (pr::view3d::Window window, BOOL visible);
	VIEW3D_API void       __stdcall View3D_SelectionBoxPosition     (pr::view3d::Window window, View3DBBox const& bbox, View3DM4x4 const& o2w);
	VIEW3D_API void       __stdcall View3D_SelectionBoxFitToSelected(pr::view3d::Window window);
	VIEW3D_API BSTR       __stdcall View3D_ExampleScriptBStr        ();
	VIEW3D_API BSTR       __stdcall View3D_AutoCompleteTemplatesBStr();
	VIEW3D_API void       __stdcall View3D_DemoScriptShow           (pr::view3d::Window window);
	VIEW3D_API View3DM4x4 __stdcall View3D_ParseLdrTransform        (wchar_t const* ldr_script);
	VIEW3D_API BSTR       __stdcall View3D_ObjectAddressAt          (wchar_t const* ldr_script, int64_t position);
	VIEW3D_API ULONG      __stdcall View3D_RefCount                 (IUnknown* pointer);
#endif

	// Tools **********************************

#if 0
	VIEW3D_API void __stdcall View3D_ObjectManagerShow        (pr::view3d::Window window, BOOL show);
	VIEW3D_API BOOL __stdcall View3D_MeasureToolVisible       (pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_ShowMeasureTool          (pr::view3d::Window window, BOOL show);
	VIEW3D_API BOOL __stdcall View3D_AngleToolVisible         (pr::view3d::Window window);
	VIEW3D_API void __stdcall View3D_ShowAngleTool            (pr::view3d::Window window, BOOL show);
#endif

#if 0
	// Ldr Editor Ctrl
	VIEW3D_API HWND __stdcall View3D_LdrEditorCreate          (HWND parent);
	VIEW3D_API void __stdcall View3D_LdrEditorDestroy         (HWND hwnd);
	VIEW3D_API void __stdcall View3D_LdrEditorCtrlInit        (HWND scintilla_control, BOOL dark);
#endif
}
