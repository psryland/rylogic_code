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
#include <d3d11.h>

#ifdef VIEW3D_EXPORTS
	namespace pr
	{
		namespace ldr { struct LdrObject; }
		namespace rdr { struct Texture2D; }
	}
	namespace view3d
	{
		typedef pr::ldr::LdrObject Object;
		typedef pr::rdr::Texture2D Texture;
		struct Window;
	}
	typedef unsigned char* View3DContext;
	typedef view3d::Window*  View3DWindow;
	typedef view3d::Object*  View3DObject;
	typedef view3d::Texture* View3DTexture;
#else
	typedef void* View3DContext;
	typedef void* View3DWindow;
	typedef void* View3DObject;
	typedef void* View3DTexture;
#endif

extern "C"
{
	typedef unsigned int View3DColour;

	enum class EView3DResult
	{
		Success,
		Failed,
	};
	enum class EView3DFillMode
	{
		Solid,
		Wireframe,
		SolidWire,
	};
	enum class EView3DGeom // pr::rdr::EGeom
	{
		Unknown = 0,
		Vert    = 1 << 0, // Object space 3D position
		Colr    = 1 << 1, // Diffuse base colour
		Norm    = 1 << 2, // Object space 3D normal
		Tex0    = 1 << 3, // Diffuse texture
	};
	enum class EView3DPrim
	{
		Invalid   = D3D11_PRIMITIVE_TOPOLOGY_UNDEFINED,
		PointList = D3D11_PRIMITIVE_TOPOLOGY_POINTLIST,
		LineList  = D3D11_PRIMITIVE_TOPOLOGY_LINELIST,
		LineStrip = D3D11_PRIMITIVE_TOPOLOGY_LINESTRIP,
		TriList   = D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST,
		TriStrip  = D3D11_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP,
	};
	enum class EView3DLight
	{
		Ambient,
		Directional,
		Point,
		Spot
	};
	enum class EView3DLogLevel
	{
		Debug,
		Info,
		Warn,
		Error,
	};
	enum class EView3DUpdateObject :int// Flags for partial update of a model
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
	};

	typedef struct
	{
		float x, y;
	} View3DV2;
	
	typedef struct
	{
		float x, y, z, w;
	} View3DV4;
	
	typedef struct
	{
		View3DV4 x, y, z, w;
	} View3DM4x4;
	
	typedef struct
	{
		View3DV4 centre;
		View3DV4 radius;
	} View3DBBox;
	
	typedef struct
	{
		View3DV4 pos;
		View3DV4 norm;
		View3DV2 tex;
		View3DColour col;
		UINT32 pad;
	} View3DVertex;
	
	typedef struct
	{
		UINT32 m_width;
		UINT32 m_height;
		UINT32 m_depth;
		UINT32 m_mips;
		DXGI_FORMAT m_format;
		UINT32 m_image_file_format;//D3DXIMAGE_FILEFORMAT
	} View3DImageInfo;

	typedef struct
	{
		EView3DLight m_type;
		BOOL         m_on;
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
	} View3DLight;
	
	typedef struct
	{
		DXGI_FORMAT                m_format;
		UINT                       m_mips;
		D3D11_FILTER               m_filter;
		D3D11_TEXTURE_ADDRESS_MODE m_addrU;
		D3D11_TEXTURE_ADDRESS_MODE m_addrV;
		D3D11_BIND_FLAG            m_bind_flags;
		D3D11_RESOURCE_MISC_FLAG   m_misc_flags;
		UINT                       m_colour_key;
		BOOL                       m_has_alpha;
		BOOL                       m_gdi_compatible;
	} View3DTextureOptions;

	typedef struct
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
	} View3DUpdateModelKeep;
	
	typedef struct
	{
		View3DTexture m_diff_tex;
		View3DTexture m_env_map;
	} View3DMaterial;
	
	typedef struct
	{
		float m_x;
		float m_y;
		float m_width;
		float m_height;
		float m_min_depth;
		float m_max_depth;
	} View3DViewport;

	typedef void (__stdcall *View3D_SettingsChanged)(View3DWindow window);
	typedef void (__stdcall *View3D_RenderCB)();
	typedef void (__stdcall *View3D_ReportErrorCB)(char const* msg, void* ctx);
	typedef void (__stdcall *View3D_EditObjectCB)(
		UINT32 vcount,      // The maximum size of 'verts'
		UINT32 icount,      // The maximum size of 'indices'
		View3DVertex* verts,     // The vert buffer to be filled
		UINT16* indices,     // The index buffer to be filled
		UINT32& new_vcount, // The number of verts in the updated model
		UINT32& new_icount, // The number indices in the updated model
		EView3DPrim& model_type, // The primitive type of the updated model
		EView3DGeom& geom_type,  // The geometry type of the updated model (used to determine the appropriate shader)
		View3DMaterial& mat,     // The material to use for the updated model
		void* ctx);              // User context data

	// Initialise/shutdown the dll
	VIEW3D_API View3DContext           __stdcall View3D_Initialise       (View3D_ReportErrorCB error_cb, void* ctx);
	VIEW3D_API void                    __stdcall View3D_Shutdown         (View3DContext context);
	VIEW3D_API void                    __stdcall View3D_PushGlobalErrorCB(View3D_ReportErrorCB error_cb, void* ctx);
	VIEW3D_API void                    __stdcall View3D_PopGlobalErrorCB (View3D_ReportErrorCB error_cb);

	// Windows
	VIEW3D_API View3DWindow            __stdcall View3D_CreateWindow      (HWND hwnd, BOOL gdi_compat, View3D_SettingsChanged settings_cb, View3D_RenderCB render_cb);
	VIEW3D_API void                    __stdcall View3D_DestroyWindow     (View3DWindow window);
	VIEW3D_API void                    __stdcall View3D_PushErrorCB       (View3DWindow window, View3D_ReportErrorCB error_cb, void* ctx);
	VIEW3D_API void                    __stdcall View3D_PopErrorCB        (View3DWindow window, View3D_ReportErrorCB error_cb);

	VIEW3D_API char const*             __stdcall View3D_GetSettings       (View3DWindow window);
	VIEW3D_API void                    __stdcall View3D_SetSettings       (View3DWindow window, char const* settings);
	VIEW3D_API void                    __stdcall View3D_AddObject         (View3DWindow window, View3DObject object);
	VIEW3D_API void                    __stdcall View3D_RemoveObject      (View3DWindow window, View3DObject object);
	VIEW3D_API void                    __stdcall View3D_RemoveAllObjects  (View3DWindow window);
	VIEW3D_API BOOL                    __stdcall View3D_HasObject         (View3DWindow window, View3DObject object);
	VIEW3D_API int                     __stdcall View3D_ObjectCount       (View3DWindow window);
	VIEW3D_API void                    __stdcall View3D_AddObjectsById    (View3DWindow window, int context_id);
	VIEW3D_API void                    __stdcall View3D_RemoveObjectsById (View3DWindow window, int context_id);

	// Camera
	VIEW3D_API void                    __stdcall View3D_CameraToWorld          (View3DWindow window, View3DM4x4& c2w);
	VIEW3D_API void                    __stdcall View3D_SetCameraToWorld       (View3DWindow window, View3DM4x4 const& c2w);
	VIEW3D_API void                    __stdcall View3D_PositionCamera         (View3DWindow window, View3DV4 position, View3DV4 lookat, View3DV4 up);
	VIEW3D_API float                   __stdcall View3D_CameraFocusDistance    (View3DWindow window);
	VIEW3D_API void                    __stdcall View3D_CameraSetFocusDistance (View3DWindow window, float dist);
	VIEW3D_API float                   __stdcall View3D_CameraAspect           (View3DWindow window);
	VIEW3D_API void                    __stdcall View3D_CameraSetAspect        (View3DWindow window, float aspect);
	VIEW3D_API float                   __stdcall View3D_CameraFovX             (View3DWindow window);
	VIEW3D_API void                    __stdcall View3D_CameraSetFovX          (View3DWindow window, float fovX);
	VIEW3D_API float                   __stdcall View3D_CameraFovY             (View3DWindow window);
	VIEW3D_API void                    __stdcall View3D_CameraSetFovY          (View3DWindow window, float fovY);
	VIEW3D_API void                    __stdcall View3D_CameraSetClipPlanes    (View3DWindow window, float near_, float far_, BOOL focus_relative);
	VIEW3D_API void                    __stdcall View3D_MouseNavigate          (View3DWindow window, View3DV2 point, int button_state, BOOL nav_start_or_end);
	VIEW3D_API void                    __stdcall View3D_Navigate               (View3DWindow window, float dx, float dy, float dz);
	VIEW3D_API void                    __stdcall View3D_ResetZoom              (View3DWindow window);
	VIEW3D_API void                    __stdcall View3D_CameraAlignAxis        (View3DWindow window, View3DV4& axis);
	VIEW3D_API void                    __stdcall View3D_AlignCamera            (View3DWindow window, View3DV4 axis);
	VIEW3D_API void                    __stdcall View3D_ResetView              (View3DWindow window, View3DV4 forward, View3DV4 up);
	VIEW3D_API View3DV2                __stdcall View3D_ViewArea               (View3DWindow window, float dist);
	VIEW3D_API void                    __stdcall View3D_GetFocusPoint          (View3DWindow window, View3DV4& position);
	VIEW3D_API void                    __stdcall View3D_SetFocusPoint          (View3DWindow window, View3DV4 position);
	VIEW3D_API View3DV4                __stdcall View3D_WSPointFromNormSSPoint (View3DWindow window, View3DV4 screen);
	VIEW3D_API View3DV4                __stdcall View3D_NormSSPointFromWSPoint (View3DWindow window, View3DV4 world);
	VIEW3D_API void                    __stdcall View3D_WSRayFromNormSSPoint   (View3DWindow window, View3DV4 screen, View3DV4& ws_point, View3DV4& ws_direction);

	// Lights
	VIEW3D_API View3DLight             __stdcall View3D_LightProperties          (View3DWindow window);
	VIEW3D_API void                    __stdcall View3D_SetLightProperties       (View3DWindow window, View3DLight const& light);
	VIEW3D_API void                    __stdcall View3D_LightSource              (View3DWindow window, View3DV4 position, View3DV4 direction, BOOL camera_relative);
	VIEW3D_API void                    __stdcall View3D_ShowLightingDlg          (View3DWindow window);

	// Objects
	VIEW3D_API int                     __stdcall View3D_ObjectsCreateFromFile    (char const* ldr_filepath, int context_id, BOOL async, char const* include_paths);
	VIEW3D_API View3DObject            __stdcall View3D_ObjectCreateLdr          (char const* ldr_script, int context_id, BOOL async, char const* include_paths, HMODULE module);
	VIEW3D_API View3DObject            __stdcall View3D_ObjectCreate             (char const* name, View3DColour colour, int icount, int vcount, View3D_EditObjectCB edit_cb, void* ctx, int context_id);
	VIEW3D_API void                    __stdcall View3D_ObjectUpdate             (View3DObject object, char const* ldr_script, EView3DUpdateObject flags);
	VIEW3D_API void                    __stdcall View3D_ObjectEdit               (View3DObject object, View3D_EditObjectCB edit_cb, void* ctx);
	VIEW3D_API void                    __stdcall View3D_ObjectsDeleteById        (int context_id);
	VIEW3D_API void                    __stdcall View3D_ObjectDelete             (View3DObject object);
	VIEW3D_API View3DM4x4              __stdcall View3D_ObjectGetO2P             (View3DObject object);
	VIEW3D_API void                    __stdcall View3D_ObjectSetO2P             (View3DObject object, View3DM4x4 const& o2p);
	VIEW3D_API void                    __stdcall View3D_SetVisibility            (View3DObject obj, BOOL visible, char const* name);
	VIEW3D_API void                    __stdcall View3D_ObjectSetColour          (View3DObject object, View3DColour colour, UINT32 mask, char const* name);
	VIEW3D_API void                    __stdcall View3D_ObjectSetTexture         (View3DObject object, View3DTexture tex, char const* name);
	VIEW3D_API View3DBBox              __stdcall View3D_ObjectBBoxMS             (View3DObject object);

	// Materials
	VIEW3D_API View3DTexture           __stdcall View3D_TextureCreate               (UINT32 width, UINT32 height, void const* data, UINT32 data_size, View3DTextureOptions const& options);
	VIEW3D_API View3DTexture           __stdcall View3D_TextureCreateFromFile       (char const* tex_filepath, UINT32 width, UINT32 height, View3DTextureOptions const& options);
	VIEW3D_API void                    __stdcall View3D_TextureLoadSurface          (View3DTexture tex, int level, char const* tex_filepath, RECT const* dst_rect, RECT const* src_rect, UINT32 filter, View3DColour colour_key);
	VIEW3D_API void                    __stdcall View3D_TextureDelete               (View3DTexture tex);
	VIEW3D_API void                    __stdcall View3D_TextureGetInfo              (View3DTexture tex, View3DImageInfo& info);
	VIEW3D_API EView3DResult           __stdcall View3D_TextureGetInfoFromFile      (char const* tex_filepath, View3DImageInfo& info);
	VIEW3D_API void                    __stdcall View3D_TextureSetFilterAndAddrMode (View3DTexture tex, D3D11_FILTER filter, D3D11_TEXTURE_ADDRESS_MODE addrU, D3D11_TEXTURE_ADDRESS_MODE addrV);
	VIEW3D_API HDC                     __stdcall View3D_TextureGetDC                (View3DTexture tex);
	VIEW3D_API void                    __stdcall View3D_TextureReleaseDC            (View3DTexture tex);
	VIEW3D_API void                    __stdcall View3D_TextureResize               (View3DTexture tex, UINT32 width, UINT32 height, BOOL all_instances, BOOL preserve);
	VIEW3D_API View3DTexture           __stdcall View3D_TextureRenderTarget         (View3DWindow window);

	// Rendering
	VIEW3D_API void                    __stdcall View3D_Render                   (View3DWindow window);
	VIEW3D_API void                    __stdcall View3D_Present                  (View3DWindow window);
	VIEW3D_API void                    __stdcall View3D_RenderTargetSize         (View3DWindow window, int& width, int& height);
	VIEW3D_API void                    __stdcall View3D_SetRenderTargetSize      (View3DWindow window, int width, int height);
	VIEW3D_API View3DViewport          __stdcall View3D_Viewport                 (View3DWindow window);
	VIEW3D_API void                    __stdcall View3D_SetViewport              (View3DWindow window, View3DViewport vp);
	VIEW3D_API EView3DFillMode         __stdcall View3D_FillMode                 (View3DWindow window);
	VIEW3D_API void                    __stdcall View3D_SetFillMode              (View3DWindow window, EView3DFillMode mode);
	VIEW3D_API BOOL                    __stdcall View3D_Orthographic             (View3DWindow window);
	VIEW3D_API void                    __stdcall View3D_SetOrthographic          (View3DWindow window, BOOL render2d);
	VIEW3D_API int                     __stdcall View3D_BackgroundColour         (View3DWindow window);
	VIEW3D_API void                    __stdcall View3D_SetBackgroundColour      (View3DWindow window, int aarrggbb);

	// Tools
	VIEW3D_API BOOL                    __stdcall View3D_MeasureToolVisible       (View3DWindow window);
	VIEW3D_API void                    __stdcall View3D_ShowMeasureTool          (View3DWindow window, BOOL show);
	VIEW3D_API BOOL                    __stdcall View3D_AngleToolVisible         (View3DWindow window);
	VIEW3D_API void                    __stdcall View3D_ShowAngleTool            (View3DWindow window, BOOL show);

	// Miscellaneous
	VIEW3D_API void                    __stdcall View3D_RestoreMainRT            (View3DWindow window);
	VIEW3D_API BOOL                    __stdcall View3D_DepthBufferEnabled       (View3DWindow window);
	VIEW3D_API void                    __stdcall View3D_SetDepthBufferEnabled    (View3DWindow window, BOOL enabled);
	VIEW3D_API BOOL                    __stdcall View3D_FocusPointVisible        (View3DWindow window);
	VIEW3D_API void                    __stdcall View3D_ShowFocusPoint           (View3DWindow window, BOOL show);
	VIEW3D_API void                    __stdcall View3D_SetFocusPointSize        (View3DWindow window, float size);
	VIEW3D_API BOOL                    __stdcall View3D_OriginVisible            (View3DWindow window);
	VIEW3D_API void                    __stdcall View3D_ShowOrigin               (View3DWindow window, BOOL show);
	VIEW3D_API void                    __stdcall View3D_SetOriginSize            (View3DWindow window, float size);
	VIEW3D_API void                    __stdcall View3D_CreateDemoScene          (View3DWindow window);
	VIEW3D_API void                    __stdcall View3D_ShowDemoScript           (View3DWindow window);
	VIEW3D_API void                    __stdcall View3D_ShowObjectManager        (View3DWindow window, BOOL show);
	VIEW3D_API View3DM4x4              __stdcall View3D_ParseLdrTransform        (char const* ldr_script);

	// Ldr Editor Ctrl
	VIEW3D_API HWND                    __stdcall View3D_LdrEditorCreate          (HWND parent);
	VIEW3D_API void                    __stdcall View3D_LdrEditorDestroy         (HWND hwnd);
}

// Conversion to/from maths types
#ifdef __cplusplus
namespace view3d
{
	// View3D to custom type conversion.
	// Specialise this to convert to/from View3D types to a custom type
	// Include "pr/view3d/prmaths.h" if using pr maths types
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

