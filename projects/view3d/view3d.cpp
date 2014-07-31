//***************************************************************************************************
// View 3D
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************

#include "stdafx.h"
#include "pr/view3d/view3d.h"
#include "pr/view3d/prmaths.h"
#include "view3d/context.h"
#include "view3d/window.h"

using namespace pr::rdr;
using namespace pr::log;
using namespace view3d;

// The view3d dll is loaded once per application, although an application may have
// multiple windows and may call Initialise/Shutdown a number of times.
// We want to be able to create View3d objects in isolation which means one
// global context within the dll. This also means one renderer, one list of objects
static Context* g_ctx = nullptr;
static Context& Dll() { if (g_ctx) return *g_ctx; throw std::exception("View3d not initialised"); }

#define DllLockGuard LockGuard lock(Dll().m_mutex)
#define DllReportError Dll().ReportError

// Initialise the dll
// The first call to initialise sets the error and log callbacks.
// Subsequent calls merely add references
VIEW3D_API View3DContext __stdcall View3D_Initialise(View3D_ReportErrorCB error_cb, View3D_LogOutputCB log_cb)
{
	try
	{
		if (g_ctx == nullptr)
			g_ctx = new Context(error_cb, log_cb);

		static View3DContext context = nullptr;
		g_ctx->m_inits.insert(++context);
		return context;
	}
	catch (std::exception const& e)
	{
		error_cb(pr::FmtS("Failed to initialise View3D.\nReason: %s\n", e.what()));
		return nullptr;
	}
	catch (...)
	{
		error_cb("Failed to initialise View3D.\nReason: An unknown exception occurred\n");
		return nullptr;
	}
}
VIEW3D_API void __stdcall View3D_Shutdown(View3DContext context)
{
	if (!g_ctx) return;

	g_ctx->m_inits.erase(context);
	if (!g_ctx->m_inits.empty())
		return;

	delete g_ctx;
	g_ctx = nullptr;
}

// Create/Destroy a window
VIEW3D_API View3DWindow __stdcall View3D_CreateWindow(HWND hwnd, BOOL gdi_compat, View3D_SettingsChanged settings_cb, View3D_RenderCB render_cb)
{
	try
	{
		std::unique_ptr<view3d::Window> win(new view3d::Window(Dll().m_rdr, hwnd, gdi_compat, settings_cb, render_cb));

		DllLockGuard;
		Dll().m_wnd_cont.insert(win.get());
		return win.release();
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_CreateWindow failed", ex);
		return nullptr;
	}
}
VIEW3D_API void __stdcall View3D_DestroyWindow(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		Dll().m_wnd_cont.erase(window);

		// We need to close and destroy any associated tool windows.
		window->Close();
		delete window;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_DestroyWindow failed", ex);
	}
}

// Generate/Parse a settings string for the view
VIEW3D_API char const* __stdcall View3D_GetSettings(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");
		
		std::stringstream out;
		//out << "*SceneSettings {" << window->m_obj_cont_ui.Settings() << "}\n";
		out << "*Light {\n" << window->m_light.Settings() << "}\n";
		
		window->m_settings = out.str();
		return window->m_settings.c_str();
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_GetSettings failed", ex);
		return "";
	}
}
VIEW3D_API void __stdcall View3D_SetSettings(View3DWindow window, char const* settings)
{
	try
	{
		if (!window) throw std::exception("window is null");

		// Parse the settings
		pr::script::Reader reader;
		pr::script::PtrSrc src(settings);
		reader.AddSource(src);

		for (pr::script::string kw; reader.NextKeywordS(kw);)
		{
			if (pr::str::EqualI(kw, "SceneSettings"))
			{
				pr::string<> desc;
				reader.ExtractSection(desc, false);
				//window->m_obj_cont_ui.Settings(desc.c_str());
				continue;
			}
			if (pr::str::EqualI(kw, "Light"))
			{
				pr::string<> desc;
				reader.ExtractSection(desc, false);
				window->m_light.Settings(desc.c_str());
				continue;
			}
		}

		// Notify of settings changed
		window->NotifySettingsChanged();
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_SetSettings failed", ex);
	}
}

// Add/Remove objects to/from a window
VIEW3D_API void __stdcall View3D_AddObject(View3DWindow window, View3DObject object)
{
	try
	{
		if (!window) throw std::exception("window is null");
		if (!object) throw std::exception("object is null");
		
		DllLockGuard;
		auto iter = window->m_objects.find(object);
		if (iter == window->m_objects.end())
			window->m_objects.insert(iter, object);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_AddObject failed", ex);
	}
}
VIEW3D_API void __stdcall View3D_RemoveObject(View3DWindow window, View3DObject object)
{
	try
	{
		if (!window) throw std::exception("window is null");
		if (!object) return;

		DllLockGuard;
		window->m_objects.erase(object);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_RemoveObject failed", ex);
	}
}
VIEW3D_API void __stdcall View3D_RemoveAllObjects(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_objects.clear();
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_RemoveAllObjects failed", ex);
	}
}

// Return true if 'object' is amoung 'window's objects
VIEW3D_API BOOL __stdcall View3D_HasObject(View3DWindow window, View3DObject object)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		return window->m_objects.find(object) != std::end(window->m_objects);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_HasObject failed", ex);
		return false;
	}
}

// Return the number of objects assigned to 'window'
VIEW3D_API int __stdcall View3D_ObjectCount(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		return int(window->m_objects.size());
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_ObjectCount failed", ex);
		return 0;
	}
}

// Add/Remove objects by context id
VIEW3D_API void __stdcall View3D_AddObjectsById(View3DWindow window, int context_id)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		for (auto obj : Dll().m_obj_cont)
			if (obj->m_context_id == context_id)
				View3D_AddObject(window, obj.m_ptr);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_AddObjectsById failed", ex);
	}
}
VIEW3D_API void __stdcall View3D_RemoveObjectsById(View3DWindow window, int context_id)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		pr::ldr::LdrObject::MatchId in_this_context(context_id);
		for (auto obj : window->m_objects)
			if (obj->m_context_id == context_id)
				window->m_objects.erase(obj);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_RemoveObjectsById failed", ex);
	}
}

// Camera ********************************************************

// Return the camera to world transform
VIEW3D_API void __stdcall View3D_CameraToWorld(View3DWindow window, View3DM4x4& c2w)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		c2w = view3d::To<View3DM4x4>(window->m_camera.m_c2w);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_CameraToWorld failed", ex);
	}
}

// Set the camera to world transform
VIEW3D_API void __stdcall View3D_SetCameraToWorld(View3DWindow window, View3DM4x4 const& c2w)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_camera.m_c2w = view3d::To<pr::m4x4>(c2w);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_SetCameraToWorld failed", ex);
	}
}

// Position the camera for a window
VIEW3D_API void __stdcall View3D_PositionCamera(View3DWindow window, View3DV4 position, View3DV4 lookat, View3DV4 up)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_camera.LookAt(view3d::To<pr::v4>(position), view3d::To<pr::v4>(lookat), view3d::To<pr::v4>(up), true);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_PositionCamera failed", ex);
	}
}

// Return the distance to the camera focus point
VIEW3D_API float __stdcall View3D_CameraFocusDistance(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		return window->m_camera.FocusDist();
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_CameraFocusDistance failed", ex);
		return 0.0f;
	}
}

// Set the camera focus distance
VIEW3D_API void __stdcall View3D_CameraSetFocusDistance(View3DWindow window, float dist)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_camera.FocusDist(dist);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_CameraSetFocusDistance failed", ex);
	}
}

// Return the aspect ratio for the camera field of view
VIEW3D_API float __stdcall View3D_CameraAspect(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		return window->m_camera.Aspect();
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_CameraAspect failed", ex);
		return 1.0f;
	}
}

// Set the aspect ratio for the camera field of view
VIEW3D_API void __stdcall View3D_CameraSetAspect(View3DWindow window, float aspect)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_camera.Aspect(aspect);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_CameraSetAspect failed", ex);
	}
}

// Return the horizontal field of view (in radians).
VIEW3D_API float __stdcall View3D_CameraFovX(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		return window->m_camera.FovX();
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_CameraFovX failed", ex);
		return 0.0f;
	}
}

// Set the horizontal field of view (in radians). Note aspect ratio is preserved, setting FovX changes FovY and visa versa
VIEW3D_API void __stdcall View3D_CameraSetFovX(View3DWindow window, float fovX)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_camera.FovX(fovX);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_CameraSetFovX failed", ex);
	}
}

// Return the vertical field of view (in radians).
VIEW3D_API float __stdcall View3D_CameraFovY(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		return window->m_camera.FovY();
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_CameraFovY failed", ex);
		return 0.0f;
	}
}

// Set the vertical field of view (in radians). Note aspect ratio is preserved, setting FovY changes FovX and visa versa
VIEW3D_API void __stdcall View3D_CameraSetFovY(View3DWindow window, float fovY)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_camera.FovY(fovY);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_CameraSetFovY failed", ex);
	}
}

// Set the near and far clip planes for the camera
VIEW3D_API void __stdcall View3D_CameraSetClipPlanes(View3DWindow window, float near_, float far_, BOOL focus_relative)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_camera.ClipPlanes(near_, far_, focus_relative != 0);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_CameraSetClipPlanes failed", ex);
	}
}

// General mouse navigation
// void OnMouseDown(UINT nFlags, CPoint point) { View3D_Navigate(m_drawset, pr::NormalisePoint(m_hWnd, point, -1.0f), nFlags, TRUE); }
// void OnMouseMove(UINT nFlags, CPoint point) { View3D_Navigate(m_drawset, pr::NormalisePoint(m_hWnd, point, -1.0f), nFlags, FALSE); } if 'nFlags' is zero, this will have no effect
// void OnMouseUp  (UINT nFlags, CPoint point) { View3D_Navigate(m_drawset, pr::NormalisePoint(m_hWnd, point, -1.0f), 0, TRUE); }
// BOOL OnMouseWheel(UINT nFlags, short zDelta, CPoint) { if (nFlags == 0) View3D_Navigate(m_drawset, 0, 0, zDelta / 120.0f); return TRUE; }
VIEW3D_API void __stdcall View3D_MouseNavigate(View3DWindow window, View3DV2 point, int button_state, BOOL nav_start_or_end)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_camera.MouseControl(view3d::To<pr::v2>(point), button_state, nav_start_or_end != 0);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_MouseNavigate failed", ex);
	}
}

// Direct movement of the camera
VIEW3D_API void __stdcall View3D_Navigate(View3DWindow window, float dx, float dy, float dz)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_camera.Translate(dx, dy, dz);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_Navigate failed", ex);
	}
}

// Reset to the default zoom
VIEW3D_API void __stdcall View3D_ResetZoom(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_camera.ResetZoom();
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_ResetZoom failed", ex);
	}
}

// Return the camera align axis
VIEW3D_API void __stdcall View3D_CameraAlignAxis(View3DWindow window, View3DV4& axis)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		axis = view3d::To<View3DV4>(window->m_camera.m_align);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_CameraAlignAxis failed", ex);
	}
}

// Align the camera to an axis
VIEW3D_API void __stdcall View3D_AlignCamera(View3DWindow window, View3DV4 axis)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_camera.SetAlign(view3d::To<pr::v4>(axis));
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_AlignCamera failed", ex);
	}
}

// Move the camera to a position that can see the whole scene
VIEW3D_API void __stdcall View3D_ResetView(View3DWindow window, View3DV4 forward, View3DV4 up)
{
	try
	{
		if (!window) throw std::exception("window is null");
		DllLockGuard;

		// The bounding box for the scene
		auto bbox = pr::BBoxReset;
		for (auto obj : window->m_objects)
			pr::Encompass(bbox, obj->BBoxWS(true));
		if (bbox == pr::BBoxReset) bbox = pr::BBoxUnit;
		window->m_camera.View(bbox, view3d::To<pr::v4>(forward), view3d::To<pr::v4>(up), true);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_ResetView failed", ex);
	}
}

// Return the size of the perpendicular area visible to the camera at 'dist' (in world space)
VIEW3D_API View3DV2 __stdcall View3D_ViewArea(View3DWindow window, float dist)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		return view3d::To<View3DV2>(window->m_camera.ViewArea(dist));
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_ViewArea failed", ex);
		return view3d::To<View3DV2>(pr::v2Zero);
	}
}

// Get/Set the camera focus point position
VIEW3D_API void __stdcall View3D_GetFocusPoint(View3DWindow window, View3DV4& position)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		position = view3d::To<View3DV4>(window->m_camera.FocusPoint());
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_GetFocusPoint failed", ex);
	}
}
VIEW3D_API void __stdcall View3D_SetFocusPoint(View3DWindow window, View3DV4 position)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_camera.FocusPoint(view3d::To<pr::v4>(position));
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_SetFocusPoint failed", ex);
	}
}

// Return a point in world space corresponding to a normalised screen space point.
// The x,y components of 'screen' should be in normalised screen space, i.e. (-1,-1)->(1,1)
// The z component should be the world space distance from the camera
VIEW3D_API View3DV4 __stdcall View3D_WSPointFromNormSSPoint(View3DWindow window, View3DV4 screen)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		return view3d::To<View3DV4>(window->m_camera.WSPointFromNormSSPoint(view3d::To<pr::v4>(screen)));
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_WSPointFromNormSSPoint failed", ex);
		return view3d::To<View3DV4>(pr::v4Zero);
	}
}

// Return a point in normalised screen space corresponding to a world space point.
// The returned z component will be the world space distance from the camera.
VIEW3D_API View3DV4 __stdcall View3D_NormSSPointFromWSPoint(View3DWindow window, View3DV4 world)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		return view3d::To<View3DV4>(window->m_camera.NormSSPointFromWSPoint(view3d::To<pr::v4>(world)));
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_NormSSPointFromWSPoint failed", ex);
		return view3d::To<View3DV4>(pr::v4Zero);
	}
}

// Return a point and direction in world space corresponding to a normalised sceen space point.
// The x,y components of 'screen' should be in normalised screen space, i.e. (-1,-1)->(1,1)
// The z component should be the world space distance from the camera
VIEW3D_API void __stdcall View3D_WSRayFromNormSSPoint(View3DWindow window, View3DV4 screen, View3DV4& ws_point, View3DV4& ws_direction)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		pr::v4 pt,dir;
		window->m_camera.WSRayFromNormSSPoint(view3d::To<pr::v4>(screen), pt, dir);
		ws_point = view3d::To<View3DV4>(pt);
		ws_direction = view3d::To<View3DV4>(dir);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_WSRayFromNormSSPoint failed", ex);
	}
}

// Lighting ********************************************************

// Return the configuration of the single light source
VIEW3D_API View3DLight __stdcall View3D_LightProperties(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		View3DLight light = {};
		light.m_position        =  view3d::To<View3DV4>(window->m_light.m_position);
		light.m_direction       =  view3d::To<View3DV4>(window->m_light.m_direction);
		light.m_type            =  static_cast<EView3DLight>(window->m_light.m_type.value);
		light.m_ambient         =  window->m_light.m_ambient;
		light.m_diffuse         =  window->m_light.m_diffuse;
		light.m_specular        =  window->m_light.m_specular;
		light.m_specular_power  =  window->m_light.m_specular_power;
		light.m_inner_cos_angle =  window->m_light.m_inner_cos_angle;
		light.m_outer_cos_angle =  window->m_light.m_outer_cos_angle;
		light.m_range           =  window->m_light.m_range;
		light.m_falloff         =  window->m_light.m_falloff;
		light.m_cast_shadow     =  window->m_light.m_cast_shadow;
		light.m_on              =  window->m_light.m_on;
		return light;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_LightProperties failed", ex);
		return View3DLight();
	}
}

// Configure the single light source
VIEW3D_API void __stdcall View3D_SetLightProperties(View3DWindow window, View3DLight const& light)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_light.m_position        = view3d::To<pr::v4>(light.m_position);
		window->m_light.m_direction       = view3d::To<pr::v4>(light.m_direction);
		window->m_light.m_type            = pr::rdr::ELight::From(light.m_type);
		window->m_light.m_ambient         = light.m_ambient;
		window->m_light.m_diffuse         = light.m_diffuse;
		window->m_light.m_specular        = light.m_specular;
		window->m_light.m_specular_power  = light.m_specular_power;
		window->m_light.m_inner_cos_angle = light.m_inner_cos_angle;
		window->m_light.m_outer_cos_angle = light.m_outer_cos_angle;
		window->m_light.m_range           = light.m_range;
		window->m_light.m_falloff         = light.m_falloff;
		window->m_light.m_cast_shadow     = light.m_cast_shadow;
		window->m_light.m_on              = light.m_on != 0;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_SetLightProperties failed", ex);
	}
}

// Set up a single light source for a window
VIEW3D_API void __stdcall View3D_LightSource(View3DWindow window, View3DV4 position, View3DV4 direction, BOOL camera_relative)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_light.m_position = view3d::To<pr::v4>(position);
		window->m_light.m_direction = view3d::To<pr::v4>(direction);
		window->m_light_is_camera_relative = camera_relative != 0;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_LightSource failed", ex);
	}
}

// Show the lighting UI
struct PreviewLighting
{
	View3DWindow m_window;
	PreviewLighting(View3DWindow window) :m_window(window) {}
	void operator()(Light const& light, bool camera_relative)
	{
		Light prev_light          = m_window->m_light;
		bool prev_camera_relative = m_window->m_light_is_camera_relative;

		m_window->m_light                    = light;
		m_window->m_light_is_camera_relative = camera_relative;

		View3D_Render(m_window);
		View3D_Present(m_window);

		m_window->m_light                    = prev_light;
		m_window->m_light_is_camera_relative = prev_camera_relative;
	}
};
VIEW3D_API void __stdcall View3D_ShowLightingDlg(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		PreviewLighting pv(window);
		LightingDlg<PreviewLighting> dlg(pv);
		dlg.m_light           = window->m_light;
		dlg.m_camera_relative = window->m_light_is_camera_relative;
		if (dlg.DoModal(window->m_wnd.m_hwnd) != IDOK) return;
		window->m_light                    = dlg.m_light;
		window->m_light_is_camera_relative = dlg.m_camera_relative;
		
		View3D_Render(window);
		View3D_Present(window);

		window->NotifySettingsChanged();
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_ShowLightingDlg failed", ex);
	}
}

// Objects **********************************************************

// Create objects given in a file.
// These objects will not have handles but can be added/removed by their context id.
// 'include_paths' is a comma separated list of include paths to use to resolve #include directives (or nullptr)
// Returns the number of objects added.
VIEW3D_API int __stdcall View3D_ObjectsCreateFromFile(char const* ldr_filepath, char const* include_paths, int context_id, BOOL async)
{
	try
	{
		DllLockGuard;
		pr::ldr::ObjectCont cont;
		pr::ldr::AddFile(Dll().m_rdr, ldr_filepath, include_paths, cont, context_id, async != 0, 0, &Dll().m_lua);
		Dll().m_obj_cont.insert(std::end(Dll().m_obj_cont), std::begin(cont), std::end(cont));
		return int(cont.size());
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_ObjectsCreateFromFile failed", ex);
		return 0;
	}
}

// If multiple objects are created, the handle returned is to the last object only
// 'include_paths' is a comma separated list of include paths to use to resolve #include directives (or nullptr)
VIEW3D_API View3DObject __stdcall View3D_ObjectCreateLdr(char const* ldr_script, char const* include_paths, int context_id, BOOL async)
{
	try
	{
		DllLockGuard;
		pr::ldr::ObjectCont cont;
		pr::ldr::AddString(Dll().m_rdr, ldr_script, include_paths, cont, context_id, async != 0, 0, &Dll().m_lua);
		Dll().m_obj_cont.insert(std::end(Dll().m_obj_cont), std::begin(cont), std::end(cont));
		return !cont.empty() ? cont.back().m_ptr : nullptr;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_ObjectCreateLdr failed", ex);
		return nullptr;
	}
}

// Modify the geometry of an existing object
struct ObjectEditCBData
{
	View3D_EditObjectCB edit_cb;
	void* ctx;
};
void __stdcall ObjectEditCB(ModelPtr model, void* ctx, pr::Renderer&)
{
	PR_ASSERT(PR_DBG, model != 0, "");
	if (!model) throw std::exception("model is null");
	ObjectEditCBData& cbdata = *static_cast<ObjectEditCBData*>(ctx);

	// Create buffers to be filled by the user callback
	auto vrange = model->m_vrange;
	auto irange = model->m_irange;
	pr::vector<View3DVertex> verts   (vrange.size());
	pr::vector<pr::uint16>   indices (irange.size());

	// Get default values for the topo, geom, and material
	auto model_type = EView3DPrim::Invalid;
	auto geom_type  = EView3DGeom::Vert;
	View3DMaterial v3dmat = {0,0};

	// If the model already has nuggets grab some defaults from it
	if (!model->m_nuggets.empty())
	{
		auto nug = model->m_nuggets.front();
		model_type = static_cast<EView3DPrim>(nug.m_topo.value);
		geom_type  = static_cast<EView3DGeom>(nug.m_geom.value);
		v3dmat.m_diff_tex = nug.m_tex_diffuse.m_ptr;
		v3dmat.m_env_map  = nullptr;
	}

	// Get the user to generate the model
	UINT32 new_vcount, new_icount;
	cbdata.edit_cb(UINT32(vrange.size()), UINT32(irange.size()), &verts[0], &indices[0], new_vcount, new_icount, model_type, geom_type, v3dmat, cbdata.ctx);
	PR_ASSERT(PR_DBG, new_vcount <= vrange.size(), "");
	PR_ASSERT(PR_DBG, new_icount <= irange.size(), "");
	PR_ASSERT(PR_DBG, model_type != EView3DPrim::Invalid, "");
	PR_ASSERT(PR_DBG, geom_type != EView3DGeom::Unknown, "");

	// Update the material
	NuggetProps mat;
	mat.m_topo = static_cast<EPrim::Enum_>(model_type);
	mat.m_geom = static_cast<EGeom::Enum_>(geom_type);
	mat.m_tex_diffuse = v3dmat.m_diff_tex;
	mat.m_vrange = vrange;
	mat.m_irange = irange;
	mat.m_vrange.resize(new_vcount);
	mat.m_irange.resize(new_icount);

	{// Lock and update the model
		MLock mlock(model, D3D11_MAP_WRITE_DISCARD);
		model->m_bbox.reset();

		// Copy the model data into the model
		auto vin = std::begin(verts);
		auto vout = mlock.m_vlock.ptr<Vert>();
		for (size_t i = 0; i != new_vcount; ++i, ++vin)
		{
			SetPCNT(*vout++, view3d::To<pr::v4>(vin->pos), pr::Colour32::make(vin->col), view3d::To<pr::v4>(vin->norm), view3d::To<pr::v2>(vin->tex));
			pr::Encompass(model->m_bbox, view3d::To<pr::v4>(vin->pos));
		}
		auto iin = std::begin(indices);
		auto iout = mlock.m_ilock.ptr<pr::uint16>();
		for (size_t i = 0; i != new_icount; ++i, ++iin)
		{
			*iout++ = *iin;
		}
	}

	// Re-create the render nuggets
	model->DeleteNuggets();
	model->CreateNugget(mat);
}

// Create an object via callback
VIEW3D_API View3DObject __stdcall View3D_ObjectCreate(char const* name, View3DColour colour, int icount, int vcount, View3D_EditObjectCB edit_cb, void* ctx, int context_id)
{
	try
	{
		DllLockGuard;
		ObjectEditCBData cbdata = {edit_cb, ctx};
		pr::ldr::ObjectAttributes attr(pr::ldr::ELdrObject::Custom, name, pr::Colour32::make(colour));
		auto obj = pr::ldr::Add(Dll().m_rdr, attr, icount, vcount, ObjectEditCB, &cbdata, context_id);
		if (obj) Dll().m_obj_cont.push_back(obj);
		return obj.m_ptr;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_ObjectCreate failed", ex);
		return nullptr;
	}
}

// Replace the model and all child objects of 'obj' with the results of 'ldr_script'
VIEW3D_API void __stdcall View3D_ObjectUpdate(View3DObject object, char const* ldr_script, EView3DUpdateObject flags)
{
	try
	{
		if (!object) throw std::exception("object is null");

		DllLockGuard;
		pr::ldr::Update(Dll().m_rdr, object, ldr_script, static_cast<pr::ldr::EUpdateObject::Enum_>(flags));
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_ObjectUpdate failed", ex);
	}
}

// Edit an existing model
VIEW3D_API void __stdcall View3D_ObjectEdit(View3DObject object, View3D_EditObjectCB edit_cb, void* ctx)
{
	try
	{
		if (!object) throw std::exception("Object is null");

		DllLockGuard;
		ObjectEditCBData cbdata = {edit_cb, ctx};
		pr::ldr::Edit(Dll().m_rdr, object, ObjectEditCB, &cbdata);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_ObjectEdit failed", ex);
	}
}

// Delete all objects matching a context id
VIEW3D_API void __stdcall View3D_ObjectsDeleteById(int context_id)
{
	try
	{
		DllLockGuard;

		// Remove objects from any windows they might be assigned to
		for (auto wnd : Dll().m_wnd_cont)
			View3D_RemoveObjectsById(wnd, context_id);

		pr::ldr::Remove(Dll().m_obj_cont, &context_id, 1, 0, 0);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_ObjectsDeleteById failed", ex);
	}
}

// Delete an object
VIEW3D_API void __stdcall View3D_ObjectDelete(View3DObject object)
{
	try
	{
		if (!object) return;
		
		DllLockGuard;

		// Remove the object from any windows it's in
		for (auto wnd : Dll().m_wnd_cont)
			View3D_RemoveObject(wnd, object);
		
		// Delete the object from the object container
		pr::ldr::Remove(Dll().m_obj_cont, object);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_ObjectDelete failed", ex);
	}
}

// Get/Set the object to parent transform for an object
// This is the object to world transform for objects without parents
// Note: In "*Box b { 1 1 1 *o2w{*pos{1 2 3}} }" setting this transform overwrites the "*o2w{*pos{1 2 3}}"
VIEW3D_API View3DM4x4 __stdcall View3D_ObjectGetO2P(View3DObject object)
{
	try
	{
		if (!object) throw std::exception("object is null");

		DllLockGuard;
		return view3d::To<View3DM4x4>(object->m_o2p);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_ObjectGetO2P failed", ex);
		return view3d::To<View3DM4x4>(pr::m4x4Identity);
	}
}
VIEW3D_API void __stdcall View3D_ObjectSetO2P(View3DObject object, View3DM4x4 const& o2p)
{
	try
	{
		if (!object) throw std::exception("Object is null");
		if (!pr::FEql(o2p.w.w,1.0f)) throw std::exception("invalid object to parent transform");

		DllLockGuard;
		object->m_o2p = view3d::To<pr::m4x4>(o2p);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_ObjectSetO2P failed", ex);
	}
}

// Set the object visibility
// See LdrObject::Apply for docs on the format of 'name'
VIEW3D_API void __stdcall View3D_SetVisibility(View3DObject object, BOOL visible, char const* name)
{
	try
	{
		if (!object) throw std::exception("Object is null");

		DllLockGuard;
		object->Visible(visible != 0, name);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_SetVisibility failed", ex);
	}
}

// Set the object colour
// See LdrObject::Apply for docs on the format of 'name'
VIEW3D_API void __stdcall View3D_ObjectSetColour(View3DObject object, View3DColour colour, UINT32 mask, char const* name)
{
	try
	{
		if (!object) throw std::exception("Object is null");

		DllLockGuard;
		object->SetColour(pr::Colour32::make(colour), mask, name);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_ObjectSetColour failed", ex);
	}
}

// Set the texture
// See LdrObject::Apply for docs on the format of 'name'
VIEW3D_API void __stdcall View3D_ObjectSetTexture(View3DObject object, View3DTexture tex, char const* name)
{
	try
	{
		if (!object) throw std::exception("Object is null");

		DllLockGuard;
		object->SetTexture(tex, name);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_ObjectSetTexture failed", ex);
	}
}

// Return the model space bounding box for 'object'
VIEW3D_API View3DBBox __stdcall View3D_ObjectBBoxMS(View3DObject object)
{
	try
	{
		if (!object) throw std::exception("Object is null");

		DllLockGuard;
		return view3d::To<View3DBBox>(object->BBoxMS(true));
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_ObjectBBoxMS failed", ex);
		return view3d::To<View3DBBox>(pr::BBoxUnit);
	}
}

// Materials ***************************************************************

// Create a texture from data in memory.
// Set 'data' to 0 to leave the texture uninitialised, if not 0 then data must point to width x height pixel data
// of the size appropriate for the given format. e.g. pr::uint px_data[width * height] for D3DFMT_A8R8G8B8
// Note: careful with stride, 'data' is expected to have the appropriate stride for pr::rdr::BytesPerPixel(format) * width
VIEW3D_API View3DTexture __stdcall View3D_TextureCreate(UINT32 width, UINT32 height, void const* data, UINT32 data_size, View3DTextureOptions const& options)
{
	try
	{
		Image src = Image::make(width, height, data, options.m_format);
		if (src.m_pixels != nullptr && src.m_pitch.x * src.m_pitch.y != data_size)
			throw std::exception("Incorrect data size provided");

		TextureDesc tdesc(src);
		tdesc.Format = options.m_format;
		tdesc.MipLevels = options.m_mips;
		tdesc.BindFlags = options.m_bind_flags | (options.m_gdi_compatible ? D3D11_BIND_SHADER_RESOURCE|D3D11_BIND_RENDER_TARGET : 0);
		tdesc.MiscFlags = options.m_misc_flags | (options.m_gdi_compatible ? D3D11_RESOURCE_MISC_GDI_COMPATIBLE : 0);

		SamplerDesc sdesc;
		sdesc.AddressU = options.m_addrU;
		sdesc.AddressV = options.m_addrV;
		sdesc.Filter = options.m_filter;

		DllLockGuard;
		Texture2DPtr t = options.m_gdi_compatible
			? Dll().m_rdr.m_tex_mgr.CreateTextureGdi(AutoId, src, tdesc, sdesc)
			: Dll().m_rdr.m_tex_mgr.CreateTexture2D(AutoId, src, tdesc, sdesc);

		t->m_has_alpha = options.m_has_alpha != 0;
		auto tex = t.m_ptr; t.m_ptr = nullptr; // rely on the caller for correct reference counting
		return tex;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_TextureCreate failed", ex);
		return nullptr;
	}
}

// Load a texture from file. Specify width == 0, height == 0 to use the dimensions of the file
VIEW3D_API View3DTexture __stdcall View3D_TextureCreateFromFile(char const* tex_filepath, UINT32 width, UINT32 height, View3DTextureOptions const& options)
{
	try
	{
		(void)width,height; //todo

		SamplerDesc sdesc;
		sdesc.AddressU = options.m_addrU;
		sdesc.AddressV = options.m_addrV;
		sdesc.Filter   = options.m_filter;

		DllLockGuard;
		Texture2DPtr t = Dll().m_rdr.m_tex_mgr.CreateTexture2D(AutoId, sdesc, tex_filepath);
		auto tex = t.m_ptr; t.m_ptr = nullptr; // rely on the caller for correct reference counting
		return tex;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_TextureCreateFromFile failed", ex);
		return nullptr;
	}
}

// Get/Release a DC for the texture. Must be a TextureGdi texture
VIEW3D_API HDC __stdcall View3D_TextureGetDC(View3DTexture tex)
{
	try
	{
		if (!tex) throw std::exception("Texture is null");
		return tex->GetDC();
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_TextureGetDC failed", ex);
		return nullptr;
	}
}
VIEW3D_API void __stdcall View3D_TextureReleaseDC(View3DTexture tex)
{
	try
	{
		if (!tex) throw std::exception("Texture is null");
		tex->ReleaseDC();
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_TextureReleaseDC failed", ex);
	}
}

// Load a texture surface from file
VIEW3D_API void __stdcall View3D_TextureLoadSurface(View3DTexture tex, int level, char const* tex_filepath, RECT const* dst_rect, RECT const* src_rect, UINT32 filter, View3DColour colour_key)
{
	try
	{
		(void)tex,level,tex_filepath,dst_rect,src_rect,filter,colour_key;
		throw std::exception("not implemented");
		//try
		//{
		//	tex->LoadSurfaceFromFile(tex_filepath, level, dst_rect, src_rect, filter, colour_key);
		//	return EView3DResult::Success;
		//}
		//catch (std::exception const& e)
		//{
		//	PR_LOGE(Rdr().m_log, Exception, e, "Failed to load texture surface from file");
		//	return EView3DResult::Failed;
		//}
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_TextureLoadSurface failed", ex);
	}
}

// Release a texture to free memory
VIEW3D_API void __stdcall View3D_TextureDelete(View3DTexture tex)
{
	try
	{
		if (!tex) throw std::exception("Texture is null");
		tex->Release();
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_TextureDelete failed", ex);
	}
}

// Read the properties of an existing texture
VIEW3D_API void __stdcall View3D_TextureGetInfo(View3DTexture tex, View3DImageInfo& info)
{
	try
	{
		if (!tex) throw std::exception("texture is null");
		auto tex_info = tex->TexDesc();
		info.m_width             = tex_info.Width;
		info.m_height            = tex_info.Height;
		info.m_depth             = 0;
		info.m_mips              = tex_info.MipLevels;
		info.m_format            = tex_info.Format;
		info.m_image_file_format = 0;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_TextureGetInfo failed", ex);
	}
}

// Read the properties of an image file
VIEW3D_API EView3DResult __stdcall View3D_TextureGetInfoFromFile(char const* tex_filepath, View3DImageInfo& info)
{
	try
	{
		(void)tex_filepath,info;
		//D3DXIMAGE_INFO tex_info;
		//if (pr::Failed(Dll().m_rdr.m_mat_mgr.TextureInfo(tex_filepath, tex_info)))
		//	return EView3DResult::Failed;

		//info.m_width             = tex_info.Width;
		//info.m_height            = tex_info.Height;
		//info.m_depth             = tex_info.Depth;
		//info.m_mips              = tex_info.MipLevels;
		//info.m_format            = tex_info.Format;
		//info.m_image_file_format = tex_info.ImageFileFormat;
		throw std::exception("not implemented");
		//return EView3DResult::Success;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_TextureGetInfoFromFile failed", ex);
		return EView3DResult::Failed;
	}
}

// Set the filtering and addressing modes to use on the texture
VIEW3D_API void __stdcall View3D_TextureSetFilterAndAddrMode(View3DTexture tex, D3D11_FILTER filter, D3D11_TEXTURE_ADDRESS_MODE addrU, D3D11_TEXTURE_ADDRESS_MODE addrV)
{
	try
	{
		if (!tex) throw std::exception("Texture is null");
		
		DllLockGuard;

		SamplerDesc desc;
		tex->m_samp->GetDesc(&desc);
		desc.Filter = filter;
		desc.AddressU = addrU;
		desc.AddressV = addrV;

		D3DPtr<ID3D11SamplerState> samp;
		pr::Throw(Dll().m_rdr.Device()->CreateSamplerState(&desc, &samp.m_ptr));
		tex->m_samp = samp;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_TextureGetInfoFromFile failed", ex);
	}
}

// Resize a texture to 'size' optionally preserving it's content
VIEW3D_API void __stdcall View3D_TextureResize(View3DTexture tex, UINT32 width, UINT32 height, BOOL all_instances, BOOL preserve)
{
	try
	{
		if (!tex) throw std::exception("Texture is null");

		DllLockGuard;
		tex->Resize(width, height, all_instances != 0, preserve != 0);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_TextureResize failed", ex);
	}
}

// Return the render target as a texture
VIEW3D_API View3DTexture __stdcall View3D_TextureRenderTarget(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		return window->m_wnd.m_main_tex.m_ptr;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_TextureResize failed", ex);
		return nullptr;
	}
}

// Rendering ***************************************************************

// Render a window. Remember to call View3D_Present() after all render calls
VIEW3D_API void __stdcall View3D_Render(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");
		auto& wnd = *window;
		auto& scene = wnd.m_scene;
		DllLockGuard;

		// Reset the drawlist
		scene.ClearDrawlists();

		// Add objects from the window to the viewport
		for (auto& obj : wnd.m_objects)
			obj->AddToScene(scene);

		// Add the measure tool objects if the window is visible
		if (wnd.m_measure_tool_ui.IsWindowVisible() && wnd.m_measure_tool_ui.Gfx())
			wnd.m_measure_tool_ui.Gfx()->AddToScene(scene);

		// Add the angle tool objects if the window is visible
		if (wnd.m_angle_tool_ui.IsWindowVisible() && wnd.m_angle_tool_ui.Gfx())
			wnd.m_angle_tool_ui.Gfx()->AddToScene(scene);

		// Position the focus point
		if (wnd.m_focus_point_visible)
		{
			float scale = wnd.m_focus_point_size * wnd.m_camera.FocusDist();
			wnd.m_focus_point.m_i2w = pr::Scale4x4(scale, wnd.m_camera.FocusPoint());
			scene.AddInstance(wnd.m_focus_point);
		}
		// Scale the origin point
		if (wnd.m_origin_point_visible)
		{
			float scale = wnd.m_origin_point_size * pr::Length3(wnd.m_camera.CameraToWorld().pos);
			wnd.m_origin_point.m_i2w = pr::Scale4x4(scale, pr::v4Origin);
			scene.AddInstance(wnd.m_origin_point);
		}

		// Set the view and projection matrices
		pr::Camera& cam = wnd.m_camera;
		scene.SetView(cam);

		// Set the light source
		Light& light = scene.m_global_light;
		light = wnd.m_light;
		if (wnd.m_light_is_camera_relative)
		{
			light.m_direction = wnd.m_camera.CameraToWorld() * wnd.m_light.m_direction;
			light.m_position  = wnd.m_camera.CameraToWorld() * wnd.m_light.m_position;
		}

		// Set the background colour
		scene.m_bkgd_colour = wnd.m_background_colour;

		// Set the global fill mode
		switch (wnd.m_fill_mode) {
		case EView3DFillMode::Solid:     scene.m_rsb.Set(ERS::FillMode, D3D11_FILL_SOLID); break;
		case EView3DFillMode::Wireframe: scene.m_rsb.Set(ERS::FillMode, D3D11_FILL_WIREFRAME); break;
		case EView3DFillMode::SolidWire: scene.m_rsb.Set(ERS::FillMode, D3D11_FILL_SOLID); break;
		}

		// Render the scene
		scene.Render();

		// Render wire frame over solid for 'SolidWire' mode
		if (wnd.m_fill_mode == EView3DFillMode::SolidWire)
		{
			auto& fr = scene.RStep<ForwardRender>();
			scene.m_rsb.Set(ERS::FillMode, D3D11_FILL_WIREFRAME);
			scene.m_bsb.Set(EBS::BlendEnable, FALSE, 0);
			fr.m_clear_bb = false;

			scene.Render();

			fr.m_clear_bb = true;
			scene.m_rsb.Clear(ERS::FillMode);
			scene.m_bsb.Clear(EBS::BlendEnable, 0);
		}
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_Render failed", ex);
	}
}

// Finish rendering with a back buffer flip
VIEW3D_API void __stdcall View3D_Present(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_wnd.Present();
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_Present failed", ex);
	}
}

// Get/Set the dimensions of the render target
// In set, if 'width' and 'height' are zero, the RT is resized to the associated window automatically.
VIEW3D_API void __stdcall View3D_RenderTargetSize(View3DWindow window, int& width, int& height)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		auto area = window->m_wnd.RenderTargetSize();
		width     = area.x;
		height    = area.y;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_RenderTargetSize failed", ex);
	}
}
VIEW3D_API void __stdcall View3D_SetRenderTargetSize(View3DWindow window, int width, int height)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		if (width  < 0) width  = 0;
		if (height < 0) height = 0;
		window->m_wnd.RenderTargetSize(pr::iv2::make(width, height));
		auto size = window->m_wnd.RenderTargetSize();

		// Update the window aspect ratio
		float aspect = (size.x == 0 || size.y == 0) ? 1.0f : size.x / float(size.y);
		window->m_camera.Aspect(aspect);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_SetRenderTargetSize failed", ex);
	}
}

// Get/Set the viewport within the render target
VIEW3D_API View3DViewport __stdcall View3D_Viewport(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		auto& scene_vp = window->m_scene.m_viewport;
		View3DViewport vp = {};
		vp.m_x         = scene_vp.TopLeftX;
		vp.m_y         = scene_vp.TopLeftY;
		vp.m_width     = scene_vp.Width;
		vp.m_height    = scene_vp.Height;
		vp.m_min_depth = scene_vp.MinDepth;
		vp.m_max_depth = scene_vp.MaxDepth;
		return vp;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_Viewport failed", ex);
		return View3DViewport();
	}
}
VIEW3D_API void __stdcall View3D_SetViewport(View3DWindow window, View3DViewport vp)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		auto& scene_vp = window->m_scene.m_viewport;
		scene_vp.TopLeftX = vp.m_x        ;
		scene_vp.TopLeftY = vp.m_y        ;
		scene_vp.Width    = vp.m_width    ;
		scene_vp.Height   = vp.m_height   ;
		scene_vp.MinDepth = vp.m_min_depth;
		scene_vp.MaxDepth = vp.m_max_depth;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_SetViewport failed", ex);
	}
}

// Get/Set the fill mode for a window
VIEW3D_API EView3DFillMode __stdcall View3D_FillMode(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		return window->m_fill_mode;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_FillMode failed", ex);
		return EView3DFillMode();
	}
}
VIEW3D_API void __stdcall View3D_SetFillMode(View3DWindow window, EView3DFillMode mode)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_fill_mode = mode;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_SetFillMode failed", ex);
	}
}

// Selected between perspective and orthographic projection
VIEW3D_API BOOL __stdcall View3D_Orthographic(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		return window->m_camera.m_orthographic;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_Orthographic failed", ex);
		return false;
	}
}
VIEW3D_API void __stdcall View3D_SetOrthographic(View3DWindow window, BOOL render2d)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_camera.m_orthographic = render2d != 0;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_SetOrthographic failed", ex);
	}
}

// Get/Set the background colour for a window
VIEW3D_API int __stdcall View3D_BackgroundColour(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		return window->m_background_colour;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_BackgroundColour failed", ex);
		return 0;
	}
}
VIEW3D_API void __stdcall View3D_SetBackgroundColour(View3DWindow window, int aarrggbb)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_background_colour = pr::Colour32::make(aarrggbb);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_SetBackgroundColour failed", ex);
	}
}

// Show the measurement tool
VIEW3D_API BOOL __stdcall View3D_MeasureToolVisible(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		return window->m_measure_tool_ui.IsWindowVisible();
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_MeasureToolVisible failed", ex);
		return false;
	}
}
VIEW3D_API void __stdcall View3D_ShowMeasureTool(View3DWindow window, BOOL show)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_measure_tool_ui.SetReadPointCtx(window);
		window->m_measure_tool_ui.Show(show != 0);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_ShowMeasureTool failed", ex);
	}
}

// Show the angle tool
VIEW3D_API BOOL __stdcall View3D_AngleToolVisible(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		return window->m_angle_tool_ui.IsWindowVisible();
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_AngleToolVisible failed", ex);
		return false;
	}
}
VIEW3D_API void __stdcall View3D_ShowAngleTool(View3DWindow window, BOOL show)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_angle_tool_ui.SetReadPointCtx(window);
		window->m_angle_tool_ui.Show(show != 0);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_ShowAngleTool failed", ex);
	}
}

// Restore the main render target and depth buffer
VIEW3D_API void __stdcall View3D_RestoreMainRT(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_wnd.RestoreRT();
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_RestoreMainRT failed", ex);
	}
}

// Returns true if the depth buffer is enabled
VIEW3D_API BOOL __stdcall View3D_DepthBufferEnabled(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		return window->m_scene.m_dsb.Desc().DepthEnable;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_DepthBufferEnabled failed", ex);
		return TRUE;
	}
}

// Enables or disables the depth buffer
VIEW3D_API void __stdcall View3D_SetDepthBufferEnabled(View3DWindow window, BOOL enabled)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_scene.m_dsb.Set(EDS::DepthEnable, enabled);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_SetDepthBufferEnabled failed", ex);
	}
}

// Return true if the focus point is visible
VIEW3D_API BOOL __stdcall View3D_FocusPointVisible(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		return window->m_focus_point_visible;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_FocusPointVisible failed", ex);
		return false;
	}
}

// Add the focus point to a window
VIEW3D_API void __stdcall View3D_ShowFocusPoint(View3DWindow window, BOOL show)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_focus_point_visible = show != 0;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_ShowFocusPoint failed", ex);
	}
}

// Set the size of the focus point
VIEW3D_API void __stdcall View3D_SetFocusPointSize(View3DWindow window, float size)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_focus_point_size = size;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_SetFocusPointSize failed", ex);
	}
}

// Return true if the origin is visible
VIEW3D_API BOOL __stdcall View3D_OriginVisible(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		return window->m_origin_point_visible;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_OriginVisible failed", ex);
		return false;
	}
}

// Add the focus point to a window
VIEW3D_API void __stdcall View3D_ShowOrigin(View3DWindow window, BOOL show)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_origin_point_visible = show != 0;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_ShowOrigin failed", ex);
	}
}

// Set the size of the focus point
VIEW3D_API void __stdcall View3D_SetOriginSize(View3DWindow window, float size)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_origin_point_size = size;
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_SetOriginSize failed", ex);
	}
}

// Create a scene showing the capabilities of view3d (actually of ldr_object_manager)
VIEW3D_API void __stdcall View3D_CreateDemoScene(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		pr::ldr::ObjectCont cont;
		pr::ldr::AddString(Dll().m_rdr, pr::ldr::CreateDemoScene().c_str(), nullptr, cont, pr::ldr::DefaultContext, true, 0, &Dll().m_lua);
		for (auto& obj : cont)
			View3D_AddObject(window, obj.m_ptr);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_CreateDemoScene failed", ex);
	}
}

// Show a window containing the demo scene script
VIEW3D_API void __stdcall View3D_ShowDemoScript(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
//		window->m_obj_cont_ui.ShowScript(pr::ldr::CreateDemoScene(), 0);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_ShowDemoScript failed", ex);
	}
}

// Display the object manager ui
VIEW3D_API void __stdcall View3D_ShowObjectManager(View3DWindow window, BOOL show)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		(void)show;
//		window->m_obj_cont_ui.Show(show != 0);
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_ShowObjectManager failed", ex);
	}
}

// Parse an ldr *o2w {} description returning the transform
VIEW3D_API View3DM4x4 __stdcall View3D_ParseLdrTransform(char const* ldr_script)
{
	try
	{
		pr::script::PtrSrc src(ldr_script);
		pr::script::Reader reader(src);
		return view3d::To<View3DM4x4>(pr::ldr::ParseLdrTransform(reader));
	}
	catch (std::exception const& ex)
	{
		DllReportError("View3D_ParseLdrTransform failed", ex);
		return view3d::To<View3DM4x4>(pr::m4x4Identity);
	}
}
