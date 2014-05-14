//***************************************************************************************************
// View 3D
//  Copyright © Rylogic Ltd 2009
//***************************************************************************************************
#pragma once
#ifndef PR_VIEW3D_VIEW3D_H
#define PR_VIEW3D_VIEW3D_H

#ifdef VIEW3D_EXPORTS
#	define VIEW3D_API __declspec(dllexport)
#else
#	define VIEW3D_API __declspec(dllimport)
#endif

#include <windows.h>
#include <d3d11.h>

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

namespace pr
{
	namespace ldr { struct LdrObject; }
	namespace rdr { struct Texture2D; }
}
namespace view3d
{
	typedef pr::ldr::LdrObject Object;
	typedef pr::rdr::Texture2D Texture;
	struct Drawset;

	// View3D to custom type conversion.
	// Specialise this to convert to/from View3D types to a custom type
	template <typename TTo, typename TFrom> struct Convert
	{
		static TTo To(TFrom const&) { static_assert(false, "No conversion from this type available"); }
	};
	template <typename TTo, typename TFrom> inline TTo To(TFrom const& from)
	{
		return Convert<TTo,TFrom>::To(from);
	}
}
typedef view3d::Drawset* View3DDrawset;
typedef view3d::Object*  View3DObject;
typedef view3d::Texture* View3DTexture;
typedef unsigned int View3DColour;
struct View3DV2
{
	float x, y;

	// Make convertable to/from any vector2 type with x,y members
	template <typename T> static View3DV2 make(T const& t)    { return view3d::To<View3DV2>(t); }
	template <typename T> View3DV2& operator = (T const& t)   { return *this = make(t); }
	template <typename T> operator T() const                  { return view3d::To<T>(*this); }
};
struct View3DV4
{
	float x, y, z, w;

	// Make convertable to/from any vector4 type with x,y,z,w members
	template <typename T> static View3DV4 make(T const& t)    { return view3d::To<View3DV4>(t); }
	template <typename T> View3DV4& operator = (T const& t)   { return *this = make(t); }
	template <typename T> operator T() const                  { return view3d::To<T>(*this); }
};
struct View3DM4x4
{
	View3DV4 x, y, z, w;

	// Make convertable to/from any matrix4x4 type with x,y,z,w vector members
	template <typename T> static View3DM4x4 make(T const& t)  { return view3d::To<View3DM4x4>(t); }
	template <typename T> View3DM4x4& operator = (T const& t) { return *this = make(t); }
	template <typename T> operator T() const                  { return view3d::To<T>(*this); }
};
struct View3DBBox
{
	View3DV4 centre;
	View3DV4 radius;

	// Make convertable to/from any bounding box type with m_centre/m_radius vector members
	template <typename T> static View3DBBox make(T const& t)  { return view3d::To<View3DBBox>(t); }
	template <typename T> View3DBBox& operator = (T const& t) { return *this = make(t); }
	template <typename T> operator T() const                  { return view3d::To<T>(*this); }
};
struct View3DVertex
{
	View3DV4 pos;
	View3DV4 norm;
	View3DV2 tex;
	View3DColour col;
	UINT32 pad;
	void set(View3DV4 p, View3DColour c, View3DV4 n, View3DV2 const& t) { pos = p; col = c; norm = n; tex = t; }
};
struct View3DImageInfo
{
	UINT32 m_width;
	UINT32 m_height;
	UINT32 m_depth;
	UINT32 m_mips;
	DXGI_FORMAT m_format;
	UINT32 m_image_file_format;//D3DXIMAGE_FILEFORMAT
};
struct View3DLight
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
	BOOL         m_cast_shadows;
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
	UINT                       m_colour_key;
	BOOL                       m_has_alpha;
	BOOL                       m_gdi_compatible;

	static View3DTextureOptions Default()
	{
		View3DTextureOptions opts = {DXGI_FORMAT_R8G8B8A8_UNORM, 0, D3D11_FILTER_MIN_MAG_MIP_LINEAR, D3D11_TEXTURE_ADDRESS_CLAMP, D3D11_TEXTURE_ADDRESS_CLAMP, D3D11_BIND_FLAG(0), D3D11_RESOURCE_MISC_FLAG(0), 0, false, false};
		return opts;
	}
};
struct View3DMaterial
{
	View3DTexture m_diff_tex;
	View3DTexture m_env_map;
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

typedef void (__stdcall *View3D_SettingsChanged)();
typedef void (__stdcall *View3D_RenderCB)();
typedef void (__stdcall *View3D_ReportErrorCB)(char const* msg);
typedef void (__stdcall *View3D_LogOutputCB)(EView3DLogLevel level, long long timestamp, char const* msg);
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

extern "C"
{
	// Initialise/shutdown the dll
	VIEW3D_API EView3DResult           __stdcall View3D_Initialise(HWND hwnd, View3D_RenderCB render_cb, View3D_ReportErrorCB error_cb, View3D_LogOutputCB log_cb, View3D_SettingsChanged settings_changed_cb);
	VIEW3D_API void                    __stdcall View3D_Shutdown();

	// Draw sets
	VIEW3D_API char const*             __stdcall View3D_GetSettings              (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_SetSettings              (View3DDrawset drawset, char const* settings);
	VIEW3D_API void                    __stdcall View3D_DrawsetRender            (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_DrawsetAddObjectsById    (View3DDrawset drawset, int context_id);
	VIEW3D_API void                    __stdcall View3D_DrawsetRemoveObjectsById (View3DDrawset drawset, int context_id);
	VIEW3D_API EView3DResult           __stdcall View3D_DrawsetCreate            (View3DDrawset& drawset);
	VIEW3D_API void                    __stdcall View3D_DrawsetDelete            (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_DrawsetAddObject         (View3DDrawset drawset, View3DObject object);
	VIEW3D_API void                    __stdcall View3D_DrawsetRemoveObject      (View3DDrawset drawset, View3DObject object);
	VIEW3D_API void                    __stdcall View3D_DrawsetRemoveAllObjects  (View3DDrawset drawset);
	VIEW3D_API int                     __stdcall View3D_DrawsetObjectCount       (View3DDrawset drawset);
	VIEW3D_API BOOL                    __stdcall View3D_DrawsetHasObject         (View3DDrawset drawset, View3DObject object);

	// Camera
	VIEW3D_API void                    __stdcall View3D_CameraToWorld          (View3DDrawset drawset, View3DM4x4& c2w);
	VIEW3D_API void                    __stdcall View3D_SetCameraToWorld       (View3DDrawset drawset, View3DM4x4 const& c2w);
	VIEW3D_API void                    __stdcall View3D_PositionCamera         (View3DDrawset drawset, View3DV4 position, View3DV4 lookat, View3DV4 up);
	VIEW3D_API float                   __stdcall View3D_FocusDistance          (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_SetFocusDistance       (View3DDrawset drawset, float dist);
	VIEW3D_API float                   __stdcall View3D_CameraAspect           (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_SetCameraAspect        (View3DDrawset drawset, float aspect);
	VIEW3D_API float                   __stdcall View3D_CameraFovX             (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_SetCameraFovX          (View3DDrawset drawset, float fovX);
	VIEW3D_API float                   __stdcall View3D_CameraFovY             (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_SetCameraFovY          (View3DDrawset drawset, float fovY);
	VIEW3D_API void                    __stdcall View3D_MouseNavigate          (View3DDrawset drawset, View3DV2 point, int button_state, BOOL nav_start_or_end);
	VIEW3D_API void                    __stdcall View3D_Navigate               (View3DDrawset drawset, float dx, float dy, float dz);
	VIEW3D_API void                    __stdcall View3D_ResetZoom              (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_CameraAlignAxis        (View3DDrawset drawset, View3DV4& axis);
	VIEW3D_API void                    __stdcall View3D_AlignCamera            (View3DDrawset drawset, View3DV4 axis);
	VIEW3D_API void                    __stdcall View3D_ResetView              (View3DDrawset drawset, View3DV4 forward, View3DV4 up);
	VIEW3D_API View3DV2                __stdcall View3D_ViewArea               (View3DDrawset drawset, float dist);
	VIEW3D_API void                    __stdcall View3D_GetFocusPoint          (View3DDrawset drawset, View3DV4& position);
	VIEW3D_API void                    __stdcall View3D_SetFocusPoint          (View3DDrawset drawset, View3DV4 position);
	VIEW3D_API View3DV4                __stdcall View3D_WSPointFromNormSSPoint (View3DDrawset drawset, View3DV4 screen);
	VIEW3D_API View3DV4                __stdcall View3D_NormSSPointFromWSPoint (View3DDrawset drawset, View3DV4 world);
	VIEW3D_API void                    __stdcall View3D_WSRayFromNormSSPoint   (View3DDrawset drawset, View3DV4 screen, View3DV4& ws_point, View3DV4& ws_direction);

	// Lights
	VIEW3D_API View3DLight             __stdcall View3D_LightProperties          (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_SetLightProperties       (View3DDrawset drawset, View3DLight const& light);
	VIEW3D_API void                    __stdcall View3D_LightSource              (View3DDrawset drawset, View3DV4 position, View3DV4 direction, BOOL camera_relative);
	VIEW3D_API void                    __stdcall View3D_ShowLightingDlg          (View3DDrawset drawset, HWND parent);

	// Objects
	VIEW3D_API EView3DResult           __stdcall View3D_ObjectsCreateFromFile    (char const* ldr_filepath, int context_id, BOOL async);
	VIEW3D_API EView3DResult           __stdcall View3D_ObjectCreateLdr          (char const* ldr_script, int context_id, View3DObject& object, BOOL async);
	VIEW3D_API EView3DResult           __stdcall View3D_ObjectCreate             (char const* name, View3DColour colour, int icount, int vcount, View3D_EditObjectCB edit_cb, void* ctx, int context_id, View3DObject& object);
	VIEW3D_API EView3DResult           __stdcall View3D_ObjectUpdateModel        (View3DObject object, char const* ldr_script, BOOL async);
	VIEW3D_API void                    __stdcall View3D_ObjectEdit               (View3DObject object, View3D_EditObjectCB edit_cb, void* ctx);
	VIEW3D_API void                    __stdcall View3D_ObjectsDeleteById        (int context_id);
	VIEW3D_API void                    __stdcall View3D_ObjectDelete             (View3DObject object);
	VIEW3D_API View3DM4x4              __stdcall View3D_ObjectGetO2P             (View3DObject object);
	VIEW3D_API void                    __stdcall View3D_ObjectSetO2P             (View3DObject object, View3DM4x4 const& o2p);
	VIEW3D_API void                    __stdcall View3D_ObjectSetColour          (View3DObject object, View3DColour colour, UINT32 mask, char const* name);
	VIEW3D_API void                    __stdcall View3D_ObjectSetTexture         (View3DObject object, View3DTexture tex, char const* name);
	VIEW3D_API View3DBBox              __stdcall View3D_ObjectBBoxMS             (View3DObject object);

	// Materials
	VIEW3D_API EView3DResult           __stdcall View3D_TextureCreate               (UINT32 width, UINT32 height, void const* data, UINT32 data_size, View3DTextureOptions const& options, View3DTexture& tex);
	VIEW3D_API EView3DResult           __stdcall View3D_TextureCreateFromFile       (char const* tex_filepath, UINT32 width, UINT32 height, View3DTextureOptions const& options, View3DTexture& tex);
	VIEW3D_API EView3DResult           __stdcall View3D_TextureLoadSurface          (View3DTexture tex, int level, char const* tex_filepath, RECT const* dst_rect, RECT const* src_rect, UINT32 filter, View3DColour colour_key);
	VIEW3D_API void                    __stdcall View3D_TextureDelete               (View3DTexture tex);
	VIEW3D_API void                    __stdcall View3D_TextureGetInfo              (View3DTexture tex, View3DImageInfo& info);
	VIEW3D_API EView3DResult           __stdcall View3D_TextureGetInfoFromFile      (char const* tex_filepath, View3DImageInfo& info);
	VIEW3D_API void                    __stdcall View3D_TextureSetFilterAndAddrMode (View3DTexture tex, D3D11_FILTER filter, D3D11_TEXTURE_ADDRESS_MODE addrU, D3D11_TEXTURE_ADDRESS_MODE addrV);
	VIEW3D_API HDC                     __stdcall View3D_TextureGetDC                (View3DTexture tex);
	VIEW3D_API void                    __stdcall View3D_TextureReleaseDC            (View3DTexture tex);
	VIEW3D_API void                    __stdcall View3D_TextureResize               (View3DTexture tex, UINT32 width, UINT32 height, BOOL all_instances, BOOL preserve);
	VIEW3D_API View3DTexture           __stdcall View3D_TextureRenderTarget         ();

	// Rendering
	VIEW3D_API void                    __stdcall View3D_Present                  ();
	VIEW3D_API void                    __stdcall View3D_RenderTargetSize         (int& width, int& height);
	VIEW3D_API void                    __stdcall View3D_SetRenderTargetSize      (int width, int height);
	VIEW3D_API View3DViewport          __stdcall View3D_Viewport                 ();
	VIEW3D_API void                    __stdcall View3D_SetViewport              (View3DViewport vp);
	VIEW3D_API EView3DFillMode         __stdcall View3D_FillMode                 (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_SetFillMode              (View3DDrawset drawset, EView3DFillMode mode);
	VIEW3D_API BOOL                    __stdcall View3D_Orthographic             (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_SetOrthographic          (View3DDrawset drawset, BOOL render2d);
	VIEW3D_API int                     __stdcall View3D_BackgroundColour         (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_SetBackgroundColour      (View3DDrawset drawset, int aarrggbb);

	// Tools
	VIEW3D_API BOOL                    __stdcall View3D_MeasureToolVisible       ();
	VIEW3D_API void                    __stdcall View3D_ShowMeasureTool          (View3DDrawset drawset, BOOL show);
	VIEW3D_API BOOL                    __stdcall View3D_AngleToolVisible         ();
	VIEW3D_API void                    __stdcall View3D_ShowAngleTool            (View3DDrawset drawset, BOOL show);

	// Miscellaneous
	VIEW3D_API void                    __stdcall View3D_RestoreMainRT            ();
	VIEW3D_API void                    __stdcall View3D_CreateDemoScene          (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_ShowDemoScript           ();
	VIEW3D_API BOOL                    __stdcall View3D_FocusPointVisible        (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_ShowFocusPoint           (View3DDrawset drawset, BOOL show);
	VIEW3D_API void                    __stdcall View3D_SetFocusPointSize        (View3DDrawset drawset, float size);
	VIEW3D_API BOOL                    __stdcall View3D_OriginVisible            (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_ShowOrigin               (View3DDrawset drawset, BOOL show);
	VIEW3D_API void                    __stdcall View3D_SetOriginSize            (View3DDrawset drawset, float size);
	VIEW3D_API void                    __stdcall View3D_ShowObjectManager        (BOOL show);
	VIEW3D_API View3DM4x4              __stdcall View3D_ParseLdrTransform        (char const* ldr_script);
}

#endif
