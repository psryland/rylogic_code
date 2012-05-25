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
#include "pr/maths/maths.h"
#include "pr/common/colour.h"
// Note: the d3d headers are not included here and are not part of this interface
// This allows the caller to use the renderer with referencing d3d

namespace EView3DResult
{
	enum Type
	{
		Success,
		Failed,
	};
}
namespace EView3DRenderMode
{
	enum Type
	{
		Solid,
		Wireframe,
		SolidWire,
	};
}
namespace EView3DPrim
{
	enum Type
	{
		PointList     = 1,//D3DPT_POINTLIST,
		LineList      = 2,//D3DPT_LINELIST,
		LineStrip     = 3,//D3DPT_LINESTRIP,
		TriangleList  = 4,//D3DPT_TRIANGLELIST,
		TriangleStrip = 5,//D3DPT_TRIANGLESTRIP,
		TriangleFan   = 6,//D3DPT_TRIANGLEFAN,
		Invalid       = 0x7FFFFFFF,//D3DPT_FORCE_DWORD
	};
}
namespace EView3DLight
{
	enum Type
	{
		Ambient,
		Directional,
		Point,
		Spot
	};
}
namespace EView3DGeom
{
	enum Type
	{
		EInvalid = 0,
		EVertex  = 1 << 0,
		ENormal  = 1 << 1,
		EColour  = 1 << 2,
		ETexture = 1 << 3,
		EAll     =(1 << 4) - 1,
		EVN      = EVertex | ENormal,
		EVC      = EVertex           | EColour,
		EVT      = EVertex                     | ETexture,
		EVNC     = EVertex | ENormal | EColour,
		EVNT     = EVertex | ENormal           | ETexture,
		EVCT     = EVertex           | EColour | ETexture,
		EVNCT    = EVertex | ENormal | EColour | ETexture,
	};
}

namespace pr
{
	namespace ldr { struct LdrObject; }
	namespace rdr { struct Texture; }
}
namespace view3d
{
	typedef pr::ldr::LdrObject Object;
	typedef pr::rdr::Texture Texture;
	struct Drawset;
}
typedef view3d::Drawset* View3DDrawset;
typedef view3d::Object*  View3DObject;
typedef view3d::Texture* View3DTexture;
	
struct View3DVertex
{
	pr::v4 pos;
	pr::v4 norm;
	pr::uint col;
	pr::v2 tex;
	void set(pr::v4 const& p, pr::v4 const& n, pr::uint c, pr::v2 const& t) { pos = p; norm = n; col = c; tex = t; }
};
struct View3DImageInfo
{
	pr::uint m_width;
	pr::uint m_height;
	pr::uint m_depth;
	pr::uint m_mips;
	pr::uint m_format; //D3DFORMAT
	pr::uint m_image_file_format;//D3DXIMAGE_FILEFORMAT
};
struct View3DLight
{
	EView3DLight::Type m_type;
	BOOL               m_on;
	pr::v4             m_position;
	pr::v4             m_direction;
	pr::Colour32       m_ambient;
	pr::Colour32       m_diffuse;
	pr::Colour32       m_specular;
	float              m_specular_power;
	float              m_inner_cos_angle;
	float              m_outer_cos_angle;
	float              m_range;
	float              m_falloff;
	BOOL               m_cast_shadows;
};
struct View3DMaterial
{
	pr::uint16         m_geom_type; // see EView3DGeom::Type
	View3DTexture      m_diff_tex;
	View3DTexture      m_env_map;
};
	
typedef void (__stdcall *View3D_SettingsChanged)();
typedef void (__stdcall *View3D_ReportErrorCB)(char const* msg);
typedef void (__stdcall *View3D_EditObjectCB)(
	std::size_t vcount,
	std::size_t icount,
	View3DVertex* verts,
	pr::uint16* indices,
	std::size_t& new_vcount,
	std::size_t& new_icount,
	EView3DPrim::Type& model_type,
	View3DMaterial& mat,
	void* ctx);
	
extern "C"
{
	// Initialise/shutdown the dll
	VIEW3D_API EView3DResult::Type     __stdcall View3D_Initialise(HWND hwnd, pr::uint d3dcreate_flags, View3D_ReportErrorCB error_cb, View3D_SettingsChanged settings_changed_cb);
	VIEW3D_API void                    __stdcall View3D_Shutdown();

	// Draw sets
	VIEW3D_API char const*             __stdcall View3D_GetSettings              (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_SetSettings              (View3DDrawset drawset, char const* settings);
	VIEW3D_API void                    __stdcall View3D_DrawsetAddObjectsById    (View3DDrawset drawset, int context_id);
	VIEW3D_API void                    __stdcall View3D_DrawsetRemoveObjectsById (View3DDrawset drawset, int context_id);
	VIEW3D_API EView3DResult::Type     __stdcall View3D_DrawsetCreate            (View3DDrawset& drawset);
	VIEW3D_API void                    __stdcall View3D_DrawsetDelete            (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_DrawsetAddObject         (View3DDrawset drawset, View3DObject object);
	VIEW3D_API void                    __stdcall View3D_DrawsetRemoveObject      (View3DDrawset drawset, View3DObject object);
	VIEW3D_API void                    __stdcall View3D_DrawsetRemoveAllObjects  (View3DDrawset drawset);
	VIEW3D_API int                     __stdcall View3D_DrawsetObjectCount       (View3DDrawset drawset);
	VIEW3D_API BOOL                    __stdcall View3D_DrawsetHasObject         (View3DDrawset drawset, View3DObject object);

	// Camera
	VIEW3D_API void                    __stdcall View3D_CameraToWorld            (View3DDrawset drawset, pr::m4x4& c2w);
	VIEW3D_API void                    __stdcall View3D_SetCameraToWorld         (View3DDrawset drawset, pr::m4x4 const& c2w);
	VIEW3D_API void                    __stdcall View3D_PositionCamera           (View3DDrawset drawset, pr::v4 const& position, pr::v4 const& lookat, pr::v4 const& up);
	VIEW3D_API float                   __stdcall View3D_FocusDistance            (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_SetFocusDistance         (View3DDrawset drawset, float dist);
	VIEW3D_API float                   __stdcall View3D_CameraAspect             (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_SetCameraAspect          (View3DDrawset drawset, float aspect);
	VIEW3D_API float                   __stdcall View3D_CameraFovX               (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_SetCameraFovX            (View3DDrawset drawset, float fovX);
	VIEW3D_API float                   __stdcall View3D_CameraFovY               (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_SetCameraFovY            (View3DDrawset drawset, float fovY);
	VIEW3D_API void                    __stdcall View3D_Navigate                 (View3DDrawset drawset, pr::v2 point, int button_state, BOOL nav_start_or_end);
	VIEW3D_API void                    __stdcall View3D_NavigateZ                (View3DDrawset drawset, float delta);
	VIEW3D_API void                    __stdcall View3D_ResetZoom                (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_CameraAlignAxis          (View3DDrawset drawset, pr::v4& axis);
	VIEW3D_API void                    __stdcall View3D_AlignCamera              (View3DDrawset drawset, pr::v4 const& axis);
	VIEW3D_API void                    __stdcall View3D_ResetView                (View3DDrawset drawset, pr::v4 const& forward, pr::v4 const& up);
	VIEW3D_API void                    __stdcall View3D_GetFocusPoint            (View3DDrawset drawset, pr::v4& position);
	VIEW3D_API void                    __stdcall View3D_SetFocusPoint            (View3DDrawset drawset, pr::v4 const& position);
	VIEW3D_API void                    __stdcall View3D_WSRayFromScreenPoint     (View3DDrawset drawset, pr::v2 screen, pr::v4& ws_point, pr::v4& ws_direction);

	// Lights
	VIEW3D_API View3DLight             __stdcall View3D_LightProperties          (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_SetLightProperties       (View3DDrawset drawset, View3DLight const& light);
	VIEW3D_API void                    __stdcall View3D_LightSource              (View3DDrawset drawset, pr::v4 const& position, pr::v4 const& direction, BOOL camera_relative);
	VIEW3D_API void                    __stdcall View3D_ShowLightingDlg          (View3DDrawset drawset, HWND parent);
	
	// Objects
	VIEW3D_API EView3DResult::Type     __stdcall View3D_ObjectsCreateFromFile    (char const* ldr_filepath, int context_id, BOOL async);
	VIEW3D_API EView3DResult::Type     __stdcall View3D_ObjectCreateLdr          (char const* ldr_script, int context_id, View3DObject& object, BOOL async);
	VIEW3D_API EView3DResult::Type     __stdcall View3D_ObjectCreate             (char const* name, pr::uint colour, int icount, int vcount, View3D_EditObjectCB edit_cb, void* ctx, int context_id, View3DObject& object);
	VIEW3D_API void                    __stdcall View3D_ObjectEdit               (View3DObject object, View3D_EditObjectCB edit_cb, void* ctx);
	VIEW3D_API void                    __stdcall View3D_ObjectsDeleteById        (int context_id);
	VIEW3D_API void                    __stdcall View3D_ObjectDelete             (View3DObject object);
	VIEW3D_API pr::m4x4                __stdcall View3D_ObjectGetO2P             (View3DObject object);
	VIEW3D_API void                    __stdcall View3D_ObjectSetO2P             (View3DObject object, pr::m4x4 const& o2p);
	VIEW3D_API void                    __stdcall View3D_ObjectSetColour          (View3DObject object, pr::uint colour, pr::uint mask, BOOL include_children);
	VIEW3D_API void                    __stdcall View3D_ObjectSetTexture         (View3DObject object, View3DTexture tex);
	VIEW3D_API pr::BoundingBox         __stdcall View3D_ObjectBBoxMS             (View3DObject object);

	// Materials
	VIEW3D_API EView3DResult::Type     __stdcall View3D_TextureCreate            (void const* data, pr::uint data_size, pr::uint width, pr::uint height, pr::uint mips, pr::uint format, View3DTexture& tex);
	VIEW3D_API EView3DResult::Type     __stdcall View3D_TextureCreateFromFile    (char const* tex_filepath, pr::uint width, pr::uint height, pr::uint mips, pr::uint filter, pr::uint mip_filter, pr::uint colour_key, View3DTexture& tex);
	VIEW3D_API EView3DResult::Type     __stdcall View3D_TextureLoadSurface       (View3DTexture tex, int level, char const* tex_filepath, RECT const* dst_rect, RECT const* src_rect, pr::uint filter, pr::uint colour_key);
	VIEW3D_API void                    __stdcall View3D_TextureDelete            (View3DTexture tex);
	VIEW3D_API void                    __stdcall View3D_TextureGetInfo           (View3DTexture tex, View3DImageInfo& info);
	VIEW3D_API EView3DResult::Type     __stdcall View3D_TextureGetInfoFromFile   (char const* tex_filepath, View3DImageInfo& info);
	
	// Rendering
	VIEW3D_API void                    __stdcall View3D_Refresh                  ();
	VIEW3D_API void                    __stdcall View3D_Resize                   (int width, int height);
	VIEW3D_API void                    __stdcall View3D_Render                   (View3DDrawset drawset);
	VIEW3D_API EView3DRenderMode::Type __stdcall View3D_RenderMode               (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_SetRenderMode            (View3DDrawset drawset, EView3DRenderMode::Type mode);
	VIEW3D_API BOOL                    __stdcall View3D_Orthographic             (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_SetOrthographic          (View3DDrawset drawset, BOOL render2d);
	VIEW3D_API int                     __stdcall View3D_BackGroundColour         (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_SetBackgroundColour      (View3DDrawset drawset, int aarrggbb);

	// Tools
	VIEW3D_API BOOL                    __stdcall View3D_MeasureToolVisible       ();
	VIEW3D_API void                    __stdcall View3D_ShowMeasureTool          (View3DDrawset drawset, BOOL show);
	VIEW3D_API BOOL                    __stdcall View3D_AngleToolVisible         ();
	VIEW3D_API void                    __stdcall View3D_ShowAngleTool            (View3DDrawset drawset, BOOL show);

	// Miscellaneous
	VIEW3D_API void                    __stdcall View3D_CreateDemoScene          (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_ShowDemoScript           ();
	VIEW3D_API BOOL                    __stdcall View3D_FocusPointVisible        (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_ShowFocusPoint           (View3DDrawset drawset, BOOL show);
	VIEW3D_API void                    __stdcall View3D_SetFocusPointSize        (View3DDrawset drawset, float size);
	VIEW3D_API BOOL                    __stdcall View3D_OriginVisible            (View3DDrawset drawset);
	VIEW3D_API void                    __stdcall View3D_ShowOrigin               (View3DDrawset drawset, BOOL show);
	VIEW3D_API void                    __stdcall View3D_SetOriginSize            (View3DDrawset drawset, float size);
	VIEW3D_API void                    __stdcall View3D_ShowObjectManager        (BOOL show);
	VIEW3D_API pr::m4x4                __stdcall View3D_ParseLdrTransform        (char const* ldr_script);
}

#endif
