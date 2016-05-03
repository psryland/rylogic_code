//***************************************************************************************************
// View 3D
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************

#include "view3d/forward.h"
#include "pr/view3d/view3d.h"
#include "pr/view3d/prmaths.h"
#include "view3d/context.h"
#include "view3d/window.h"

using namespace pr::rdr;
using namespace pr::log;
using namespace view3d;

// The view3d dll is loaded once per application, although an application may have
// multiple windows and may call Initialise/Shutdown a number of times.
// Ldr object can be created independently to windows. This means we need one global
// context within the dll, one renderer, and one list of objects.

// Error/Log handling:
//  Each window represents a separate context from the callers point of view, this
//  means we need an error handler per window. Also, within a window, callers may
//  want to temporarily push a different error handler. Each window maintains a
//  stack of error handlers.

CAppModule g_module;

#ifdef _MANAGED
#pragma managed(push, off)
#endif
BOOL APIENTRY DllMain(HMODULE hInstance, DWORD ul_reason_for_call, LPVOID)
{
	switch (ul_reason_for_call)
	{
	case DLL_THREAD_ATTACH:  break;
	case DLL_THREAD_DETACH:  break;
	case DLL_PROCESS_ATTACH: g_module.Init(0, hInstance); break;
	case DLL_PROCESS_DETACH: g_module.Term(); break;
	}
	return TRUE;
}
#ifdef _MANAGED
#pragma managed(pop)
#endif

static Context* g_ctx = nullptr;
static Context& Dll()
{
	if (g_ctx) return *g_ctx;
	throw std::exception("View3d not initialised");
}

// Default error callback
void __stdcall DefaultErrorCB(void*, char const* msg) { std::cerr << msg << std::endl; }

// Report an error message via the window error callback
inline void ReportError(char const* func_name, View3DWindow wnd, std::exception const* ex)
{
	// Find the callback to use
	auto error_cb = ReportErrorCB(DefaultErrorCB, nullptr);
	if (!Dll().m_error_cb.empty()) error_cb = Dll().m_error_cb.back();
	if (wnd != nullptr && !wnd->m_error_cb.empty()) error_cb = wnd->m_error_cb.back();

	// Report the error
	pr::string<> msg = pr::FmtS("%s failed.\n%s", func_name, ex ? ex->what() : "Unknown exception occurred.");
	if (msg.last() != '\n') msg.push_back('\n');
	error_cb(msg.c_str());
}

#define DllLockGuard LockGuard lock(Dll().m_mutex)
#define CatchAndReport(func_name, wnd, ret)\
	catch (std::exception const& ex) { ReportError(#func_name, View3DWindow(wnd), &ex); }\
	catch (...)                      { ReportError(#func_name, View3DWindow(wnd), nullptr); }\
	return ret

// Initialise the dll
// Initialise calls are reference counted and must be matched with Shutdown calls
// 'initialise_error_cb' is used to report dll initialisation errors only (i.e. it isn't stored)
// Note: this function is not thread safe, avoid race calls
VIEW3D_API View3DContext __stdcall View3D_Initialise(View3D_ReportErrorCB initialise_error_cb, void* ctx)
{
	try
	{
		// Create the dll context on the first call
		if (g_ctx == nullptr)
			g_ctx = new Context();

		// Generate a unique handle per Initialise call, used to match up with Shutdown calls
		static View3DContext context = nullptr;
		g_ctx->m_inits.insert(++context);
		return context;
	}
	catch (std::exception const& e)
	{
		if (initialise_error_cb) initialise_error_cb(ctx, pr::FmtS("Failed to initialise View3D.\nReason: %s\n", e.what()));
		return nullptr;
	}
	catch (...)
	{
		if (initialise_error_cb) initialise_error_cb(ctx, "Failed to initialise View3D.\nReason: An unknown exception occurred\n");
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

// Push/Pop global error callback
VIEW3D_API void __stdcall View3D_PushGlobalErrorCB(View3D_ReportErrorCB error_cb, void* ctx)
{
	try
	{
		Dll().PushErrorCB(error_cb, ctx);
	}
	CatchAndReport(View3D_PushGlobalErrorCB,,);
}
VIEW3D_API void __stdcall View3D_PopGlobalErrorCB(View3D_ReportErrorCB error_cb)
{
	try
	{
		Dll().PopErrorCB(error_cb);
	}
	CatchAndReport(View3D_PopGlobalErrorCB,,);
}

// Create/Destroy a window
// 'error_cb' must be a valid function pointer for the lifetime of the window
VIEW3D_API View3DWindow __stdcall View3D_CreateWindow(HWND hwnd, View3DWindowOptions const& opts)
{
	try
	{
		auto win = std::unique_ptr<view3d::Window>(new view3d::Window(Dll().m_rdr, hwnd, opts));

		DllLockGuard;
		Dll().m_wnd_cont.insert(win.get());
		return win.release();
	}
	catch (std::exception const& e)
	{
		if (opts.m_error_cb) opts.m_error_cb(opts.m_error_cb_ctx, pr::FmtS("Failed to create View3D Window.\n%s", e.what()));
		return nullptr;
	}
	catch (...)
	{
		if (opts.m_error_cb) opts.m_error_cb(opts.m_error_cb_ctx, pr::FmtS("Failed to create View3D Window.\nUnknown reason"));
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
	CatchAndReport(View3D_DestroyWindow,window,);
}

// Push/Pop window error callback
VIEW3D_API void __stdcall View3D_PushErrorCB(View3DWindow window, View3D_ReportErrorCB error_cb, void* ctx)
{
	try
	{
		if (!window) throw std::exception("window is null");
		window->PushErrorCB(error_cb, ctx);
	}
	CatchAndReport(View3D_PushGlobalErrorCB,window,);
}
VIEW3D_API void __stdcall View3D_PopErrorCB(View3DWindow window, View3D_ReportErrorCB error_cb)
{
	try
	{
		if (!window) throw std::exception("window is null");
		window->PopErrorCB(error_cb);
	}
	CatchAndReport(View3D_PopGlobalErrorCB, window,);
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
	CatchAndReport(View3D_GetSettings, window, "");
}
VIEW3D_API void __stdcall View3D_SetSettings(View3DWindow window, char const* settings)
{
	try
	{
		if (!window) throw std::exception("window is null");

		// Parse the settings
		pr::script::PtrA<> src(settings);
		pr::script::Reader reader(src);

		for (pr::script::string kw; reader.NextKeywordS(kw);)
		{
			if (pr::str::EqualI(kw, "SceneSettings"))
			{
				pr::string<> desc;
				reader.Section(desc, false);
				//window->m_obj_cont_ui.Settings(desc.c_str());
				continue;
			}
			if (pr::str::EqualI(kw, "Light"))
			{
				pr::string<> desc;
				reader.Section(desc, false);
				window->m_light.Settings(desc.c_str());
				continue;
			}
		}

		// Notify of settings changed
		window->NotifySettingsChanged();
	}
	CatchAndReport(View3D_SetSettings, window,);
}

// Add/Remove a callback that is called when settings change
VIEW3D_API void __stdcall View3D_SettingsChanged(View3DWindow window, View3D_SettingsChangedCB settings_changed_cb, void* ctx, BOOL add)
{
	try
	{
		if (!window) throw std::exception("window is null");
		if (add)
			window->OnSettingsChanged += pr::StaticCallBack(settings_changed_cb, ctx);
		else
			window->OnSettingsChanged -= pr::StaticCallBack(settings_changed_cb, ctx);
	}
	CatchAndReport(View3D_SettingsChanged, window,);
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
	CatchAndReport(View3D_AddObject, window,);
}
VIEW3D_API void __stdcall View3D_RemoveObject(View3DWindow window, View3DObject object)
{
	try
	{
		if (!object) return;
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_objects.erase(object);
	}
	CatchAndReport(View3D_RemoveObject, window,);
}
VIEW3D_API void __stdcall View3D_RemoveAllObjects(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_objects.clear();
	}
	CatchAndReport(View3D_RemoveAllObjects, window,);
}

// Return true if 'object' is among 'window's objects
VIEW3D_API BOOL __stdcall View3D_HasObject(View3DWindow window, View3DObject object)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		return window->m_objects.find(object) != std::end(window->m_objects);
	}
	CatchAndReport(View3D_HasObject, window, false);
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
	CatchAndReport(View3D_ObjectCount, window, 0);
}

// Add/Remove objects by context id
VIEW3D_API void __stdcall View3D_AddObjectsById(View3DWindow window, GUID const& context_id)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		for (auto obj : Dll().m_obj_cont)
			if (obj->m_context_id == context_id)
				View3D_AddObject(window, obj.m_ptr);
	}
	CatchAndReport(View3D_AddObjectsById, window,);
}
VIEW3D_API void __stdcall View3D_RemoveObjectsById(View3DWindow window, GUID const& context_id)
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
	CatchAndReport(View3D_RemoveObjectsById, window,);
}

// Add/Remove a gizmo from 'window'
VIEW3D_API void __stdcall View3D_AddGizmo(View3DWindow window, View3DGizmo gizmo)
{
	try
	{
		if (!window) throw std::exception("window is null");
		if (!gizmo) throw std::exception("gizmo is null");
		
		DllLockGuard;
		auto iter = window->m_gizmos.find(gizmo);
		if (iter == end(window->m_gizmos))
			window->m_gizmos.insert(iter, gizmo);
	}
	CatchAndReport(View3D_AddGizmo, window,);
}
VIEW3D_API void __stdcall View3D_RemoveGizmo(View3DWindow window, View3DGizmo gizmo)
{
	try
	{
		if (!gizmo) return;
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_gizmos.erase(gizmo);
	}
	CatchAndReport(View3D_RemoveGizmo, window,);
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
	CatchAndReport(View3D_CameraToWorld, window,);
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
	CatchAndReport(View3D_SetCameraToWorld, window,);
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
	CatchAndReport(View3D_PositionCamera, window,);
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
	CatchAndReport(View3D_CameraFocusDistance, window, 0.0f);
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
	CatchAndReport(View3D_CameraSetFocusDistance, window,);
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
	CatchAndReport(View3D_CameraAspect, window, 1.0f);
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
	CatchAndReport(View3D_CameraSetAspect, window,);
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
	CatchAndReport(View3D_CameraFovX, window, 0.0f);
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
	CatchAndReport(View3D_CameraSetFovX, window,);
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
	CatchAndReport(View3D_CameraFovY, window, 0.0f);
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
	CatchAndReport(View3D_CameraSetFovY, window,);
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
	CatchAndReport(View3D_CameraSetClipPlanes, window,);
}

// General mouse navigation
// 'ss_pos' is the mouse pointer position in 'window's screen space
// 'button_state' is the state of the mouse buttons and control keys (i.e. MF_LBUTTON, etc)
// 'nav_start_or_end' should be TRUE on mouse down/up events, FALSE for mouse move events
// void OnMouseDown(UINT nFlags, CPoint point) { View3D_Navigate(m_drawset, point, nFlags, TRUE); }
// void OnMouseMove(UINT nFlags, CPoint point) { View3D_Navigate(m_drawset, point, nFlags, FALSE); } if 'nFlags' is zero, this will have no effect
// void OnMouseUp  (UINT nFlags, CPoint point) { View3D_Navigate(m_drawset, point, 0, TRUE); }
// BOOL OnMouseWheel(UINT nFlags, short zDelta, CPoint) { if (nFlags == 0) View3D_Navigate(m_drawset, 0, 0, zDelta / 120.0f); return TRUE; }
VIEW3D_API BOOL __stdcall View3D_MouseNavigate(View3DWindow window, View3DV2 ss_pos, int button_state, BOOL nav_start_or_end)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		auto ss_point = view3d::To<pr::v2>(ss_pos);
		auto nss_point = window->SSPointToNSSPoint(ss_point);

		auto refresh = false;
		auto gizmo_in_use = false;

		// Check any gizmos in the scene for interaction with the mouse
		for (auto& giz : window->m_gizmos)
		{
			refresh |= giz->MouseControl(window->m_camera, nss_point, button_state, nav_start_or_end != 0);
			gizmo_in_use |= giz->m_manipulating;
			if (gizmo_in_use) break;
		}

		// If no gizmos are using the mouse, use standard mouse control
		if (!gizmo_in_use)
		{
			if (window->m_camera.MouseControl(nss_point, button_state, nav_start_or_end != 0))
				refresh |= true;
		}

		return refresh;
	}
	CatchAndReport(View3D_MouseNavigate, window, FALSE);
}

// Direct movement of the camera
VIEW3D_API BOOL __stdcall View3D_Navigate(View3DWindow window, float dx, float dy, float dz)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		return window->m_camera.Translate(dx, dy, dz);
	}
	CatchAndReport(View3D_Navigate, window, FALSE);
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
	CatchAndReport(View3D_ResetZoom, window,);
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
	CatchAndReport(View3D_CameraAlignAxis, window,);
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
	CatchAndReport(View3D_AlignCamera, window,);
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
	CatchAndReport(View3D_ResetView, window,);
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
	CatchAndReport(View3D_ViewArea, window, view3d::To<View3DV2>(pr::v2Zero));
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
	CatchAndReport(View3D_GetFocusPoint, window,);
}
VIEW3D_API void __stdcall View3D_SetFocusPoint(View3DWindow window, View3DV4 position)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_camera.FocusPoint(view3d::To<pr::v4>(position));
	}
	CatchAndReport(View3D_SetFocusPoint, window,);
}

// Convert a point in 'window' screen space to normalised screen space
VIEW3D_API View3DV2 __stdcall View3D_SSPointToNSSPoint(View3DWindow window, View3DV2 screen)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		return view3d::To<View3DV2>(window->SSPointToNSSPoint(view3d::To<pr::v2>(screen)));
	}
	CatchAndReport(View3D_NSSPointToWSPoint, window, View3DV2());
}

// Return a point in world space corresponding to a normalised screen space point.
// The x,y components of 'screen' should be in normalised screen space, i.e. (-1,-1)->(1,1)
// The z component should be the world space distance from the camera
VIEW3D_API View3DV4 __stdcall View3D_NSSPointToWSPoint(View3DWindow window, View3DV4 screen)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		return view3d::To<View3DV4>(window->m_camera.NSSPointToWSPoint(view3d::To<pr::v4>(screen)));
	}
	CatchAndReport(View3D_NSSPointToWSPoint, window, View3DV4());
}

// Return a point in normalised screen space corresponding to a world space point.
// The returned z component will be the world space distance from the camera.
VIEW3D_API View3DV4 __stdcall View3D_WSPointToNSSPoint(View3DWindow window, View3DV4 world)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		return view3d::To<View3DV4>(window->m_camera.WSPointToNSSPoint(view3d::To<pr::v4>(world)));
	}
	CatchAndReport(View3D_WSPointToNSSPoint, window, view3d::To<View3DV4>(pr::v4Zero));
}

// Return a point and direction in world space corresponding to a normalised screen space point.
// The x,y components of 'screen' should be in normalised screen space, i.e. (-1,-1)->(1,1)
// The z component should be the world space distance from the camera
VIEW3D_API void __stdcall View3D_NSSPointToWSRay(View3DWindow window, View3DV4 screen, View3DV4& ws_point, View3DV4& ws_direction)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		pr::v4 pt,dir;
		window->m_camera.NSSPointToWSRay(view3d::To<pr::v4>(screen), pt, dir);
		ws_point = view3d::To<View3DV4>(pt);
		ws_direction = view3d::To<View3DV4>(dir);
	}
	CatchAndReport(View3D_NSSPointToWSRay, window,);
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
	CatchAndReport(View3D_LightProperties, window, View3DLight());
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
	CatchAndReport(View3D_SetLightProperties, window,);
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
	CatchAndReport(View3D_LightSource, window,);
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
		LightingUI<PreviewLighting> dlg(window->m_hwnd, pv);
		dlg.m_light           = window->m_light;
		dlg.m_camera_relative = window->m_light_is_camera_relative;
		if (dlg.ShowDialog(window->m_wnd.m_hwnd) != pr::gui::EDialogResult::Ok) return;
		window->m_light                    = dlg.m_light;
		window->m_light_is_camera_relative = dlg.m_camera_relative;
		
		View3D_Render(window);
		View3D_Present(window);

		window->NotifySettingsChanged();
	}
	CatchAndReport(View3D_ShowLightingDlg, window,);
}

// Objects **********************************************************

// Create an include handler that can load from directories or embedded resources
pr::script::Includes<> GetIncludes(View3DIncludes const* includes)
{
	using namespace pr::script;

	Includes<> inc(Includes<>::EType::None);
	if (includes != nullptr)
	{
		if (includes->m_include_paths != nullptr)
			inc.SearchPaths(pr::script::string(includes->m_include_paths));

		if (includes->m_module_count != 0)
			inc.ResourceModules(std::initializer_list<HMODULE>(includes->m_modules, includes->m_modules + includes->m_module_count));
	}
	return std::move(inc);
}

// Create objects given in a file.
// These objects will not have handles but can be added/removed by their context id.
// 'include_paths' is a comma separated list of include paths to use to resolve #include directives (or nullptr)
// Returns the number of objects added.
VIEW3D_API int __stdcall View3D_ObjectsCreateFromFile(char const* ldr_filepath, GUID const& context_id, BOOL async, View3DIncludes const* includes)
{
	using namespace pr::script;
	try
	{
		DllLockGuard;

		// Create a reader to parse the script files
		FileSrc<> src(ldr_filepath);
		auto inc = GetIncludes(includes);
		Reader reader(src, false, &inc, nullptr, &Dll().m_lua);

		pr::ldr::ParseResult out;
		pr::ldr::Parse(Dll().m_rdr, reader, out, async != 0, context_id);
		Dll().m_obj_cont.insert(std::end(Dll().m_obj_cont), std::begin(out.m_objects), std::end(out.m_objects));
		return int(out.m_objects.size());
	}
	CatchAndReport(View3D_ObjectsCreateFromFile, , 0);
}

// Create objects given in an ldr string or file.
// If multiple objects are created, the handle returned is to the first object only
// 'ldr_script' - an ldr string, or filepath to a file containing ldr script
// 'file' - TRUE if 'ldr_script' is a filepath, FALSE if 'ldr_script' is a string containing ldr script
// 'context_id' - the context id to create the LdrObjects with
// 'async' - if objects should be created by a background thread
// 'includes' - information used to resolve include directives in 'ldr_script'
VIEW3D_API View3DObject __stdcall View3D_ObjectCreateLdr(char const* ldr_script, BOOL file, GUID const& context_id, BOOL async, View3DIncludes const* includes)
{
	using namespace pr::script;
	try
	{
		DllLockGuard;

		// Parse the description
		pr::ldr::ParseResult out;
		if (file)
		{
			FileSrc<> src(ldr_script);
			auto inc = GetIncludes(includes);
			Reader reader(src, false, &inc, nullptr, &Dll().m_lua);
			pr::ldr::Parse(Dll().m_rdr, reader, out, async != 0, context_id);
		}
		else // string
		{
			PtrA<> src(ldr_script);
			auto inc = GetIncludes(includes); 
			Reader reader(src, false, &inc, nullptr, &Dll().m_lua);
			pr::ldr::Parse(Dll().m_rdr, reader, out, async != 0, context_id);
		}

		// Return the first object
		Dll().m_obj_cont.insert(std::end(Dll().m_obj_cont), std::begin(out.m_objects), std::end(out.m_objects));
		return !out.m_objects.empty() ? out.m_objects.front().m_ptr : nullptr;
	}
	CatchAndReport(View3D_ObjectCreateLdr, , nullptr);
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

	// Get default values for the 'topo', 'geom', and 'material'
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
			SetPCNT(*vout++, view3d::To<pr::v4>(vin->pos), pr::Colour32(vin->col), view3d::To<pr::v4>(vin->norm), view3d::To<pr::v2>(vin->tex));
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
VIEW3D_API View3DObject __stdcall View3D_ObjectCreate(char const* name, View3DColour colour, int icount, int vcount, View3D_EditObjectCB edit_cb, void* ctx, GUID const& context_id)
{
	try
	{
		DllLockGuard;
		ObjectEditCBData cbdata = {edit_cb, ctx};
		pr::ldr::ObjectAttributes attr(pr::ldr::ELdrObject::Custom, name, pr::Colour32(colour));
		auto obj = pr::ldr::Add(Dll().m_rdr, attr, icount, vcount, ObjectEditCB, &cbdata, context_id);
		if (obj) Dll().m_obj_cont.push_back(obj);
		return obj.m_ptr;
	}
	CatchAndReport(View3D_ObjectCreate, , nullptr);
}

// Replace the model and all child objects of 'obj' with the results of 'ldr_script'
VIEW3D_API void __stdcall View3D_ObjectUpdate(View3DObject object, char const* ldr_script, EView3DUpdateObject flags)
{
	try
	{
		if (!object) throw std::exception("object is null");

		DllLockGuard;

		pr::script::PtrA<> src(ldr_script);
		pr::script::Reader reader(src, false);
		pr::ldr::Update(Dll().m_rdr, object, reader, static_cast<pr::ldr::EUpdateObject::Enum_>(flags));
	}
	CatchAndReport(View3D_ObjectUpdate, ,);
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
	CatchAndReport(View3D_ObjectEdit, ,);
}

// Delete all objects matching a context id
VIEW3D_API void __stdcall View3D_ObjectsDeleteById(GUID const& context_id)
{
	try
	{
		DllLockGuard;

		// Remove objects from any windows they might be assigned to
		for (auto wnd : Dll().m_wnd_cont)
			View3D_RemoveObjectsById(wnd, context_id);

		pr::ldr::Remove(Dll().m_obj_cont, &context_id, 1, 0, 0);
	}
	CatchAndReport(View3D_ObjectsDeleteById, ,);
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
	CatchAndReport(View3D_ObjectDelete, ,);
}

// Return the immediate parent of 'object'
VIEW3D_API View3DObject __stdcall View3D_ObjectGetParent(View3DObject object)
{
	try
	{
		if (!object) throw std::exception("object is null");

		DllLockGuard;
		return object->m_parent;
	}
	CatchAndReport(View3D_ObjectGetParent, , nullptr);
}

// Return a child object of 'object'
VIEW3D_API View3DObject __stdcall View3D_ObjectGetChild(View3DObject object, char const* name)
{
	try
	{
		if (!object) throw std::exception("object is null");

		DllLockGuard;
		auto ptr = object->Child(name);
		return ptr.m_ptr;
	}
	CatchAndReport(View3D_ObjectGetChild, , nullptr);
}

// Get/Set the object to world transform for this object or the first child object that matches 'name'.
// If 'name' is null, then the state of the root object is returned
// If 'name' begins with '#' then the remainder of the name is treated as a regular expression
/// Note, setting the o2w for a child object results in a transform that is relative to it's immediate parent
VIEW3D_API View3DM4x4 __stdcall View3D_ObjectGetO2W(View3DObject object, char const* name)
{
	try
	{
		if (!object) throw std::exception("object is null");

		DllLockGuard;
		return view3d::To<View3DM4x4>(object->O2W(name));
	}
	CatchAndReport(View3D_ObjectGetO2W, , view3d::To<View3DM4x4>(pr::m4x4Identity));
}
VIEW3D_API void __stdcall View3D_ObjectSetO2W(View3DObject object, View3DM4x4 const& o2w, char const* name)
{
	try
	{
		if (!object) throw std::exception("Object is null");
		if (!pr::FEql(o2w.w.w,1.0f)) throw std::exception("invalid object to world transform");

		DllLockGuard;
		object->O2W(view3d::To<pr::m4x4>(o2w), name);
	}
	CatchAndReport(View3D_ObjectSetO2W, ,);
}

// Get/Set the object to parent transform for an object
// This is the object to world transform for objects without parents
// Note: In "*Box b { 1 1 1 *o2w{*pos{1 2 3}} }" setting this transform overwrites the "*o2w{*pos{1 2 3}}"
VIEW3D_API View3DM4x4 __stdcall View3D_ObjectGetO2P(View3DObject object, char const* name)
{
	try
	{
		if (!object) throw std::exception("object is null");

		DllLockGuard;
		return view3d::To<View3DM4x4>(object->O2P(name));
	}
	CatchAndReport(View3D_ObjectGetO2P, , view3d::To<View3DM4x4>(pr::m4x4Identity));
}
VIEW3D_API void __stdcall View3D_ObjectSetO2P(View3DObject object, View3DM4x4 const& o2p, char const* name)
{
	try
	{
		if (!object) throw std::exception("Object is null");
		if (!pr::FEql(o2p.w.w,1.0f)) throw std::exception("invalid object to parent transform");

		DllLockGuard;
		object->O2P(view3d::To<pr::m4x4>(o2p), name);
	}
	CatchAndReport(View3D_ObjectSetO2P, ,);
}

// Get/Set the object visibility
// See LdrObject::Apply for docs on the format of 'name'
VIEW3D_API BOOL __stdcall View3D_ObjectGetVisibility(View3DObject object, char const* name)
{
	try
	{
		if (!object) throw std::exception("Object is null");

		DllLockGuard;
		return object->Visible(name);
	}
	CatchAndReport(View3D_ObjectGetVisibility, ,FALSE);
}
VIEW3D_API void __stdcall View3D_ObjectSetVisibility(View3DObject object, BOOL visible, char const* name)
{
	try
	{
		if (!object) throw std::exception("Object is null");

		DllLockGuard;
		object->Visible(visible != 0, name);
	}
	CatchAndReport(View3D_ObjectSetVisibility, ,);
}

// Return the current or base colour of an object (the first object to match 'name')
// See LdrObject::Apply for docs on the format of 'name'
VIEW3D_API View3DColour __stdcall View3D_ObjectGetColour(View3DObject object, BOOL base_colour, char const* name)
{
	try
	{
		if (!object) throw std::exception("Object is null");

		DllLockGuard;
		return object->Colour(base_colour != 0, name);
	}
	CatchAndReport(View3D_ObjectGetColour, ,View3DColour(0xFFFFFFFF));
}

// Set the object colour
// See LdrObject::Apply for docs on the format of 'name'
VIEW3D_API void __stdcall View3D_ObjectSetColour(View3DObject object, View3DColour colour, UINT32 mask, char const* name)
{
	try
	{
		if (!object) throw std::exception("Object is null");

		DllLockGuard;
		object->Colour(pr::Colour32(colour), mask, name);
	}
	CatchAndReport(View3D_ObjectSetColour, ,);
}

// Reset the object colour back to its default
VIEW3D_API void __stdcall View3D_ObjectResetColour(View3DObject object, char const* name)
{
	try
	{
		if (!object) throw std::exception("Object is null");

		DllLockGuard;
		object->ResetColour(name);
	}
	CatchAndReport(View3D_ObjectResetColour, ,);
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
	CatchAndReport(View3D_ObjectSetTexture, ,);
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
	CatchAndReport(View3D_ObjectBBoxMS, , view3d::To<View3DBBox>(pr::BBoxUnit));
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
		Image src(width, height, data, options.m_format);
		if (src.m_pixels != nullptr && src.m_pitch.x * src.m_pitch.y != pr::s_cast<int>(data_size))
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

		auto name = options.m_dbg_name;

		DllLockGuard;
		Texture2DPtr t = options.m_gdi_compatible
			? Dll().m_rdr.m_tex_mgr.CreateTextureGdi(AutoId, src, tdesc, sdesc, name)
			: Dll().m_rdr.m_tex_mgr.CreateTexture2D(AutoId, src, tdesc, sdesc, name);

		t->m_has_alpha = options.m_has_alpha != 0;
		auto tex = t.m_ptr; t.m_ptr = nullptr; // rely on the caller for correct reference counting
		return tex;
	}
	CatchAndReport(View3D_TextureCreate, , nullptr);
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

		auto name = options.m_dbg_name;

		DllLockGuard;
		Texture2DPtr t = Dll().m_rdr.m_tex_mgr.CreateTexture2D(AutoId, sdesc, tex_filepath, name);
		auto tex = t.m_ptr; t.m_ptr = nullptr; // rely on the caller for correct reference counting
		return tex;
	}
	CatchAndReport(View3D_TextureCreateFromFile, , nullptr);
}

// Get/Release a DC for the texture. Must be a TextureGdi texture
VIEW3D_API HDC __stdcall View3D_TextureGetDC(View3DTexture tex)
{
	try
	{
		if (!tex) throw std::exception("Texture is null");
		return tex->GetDC();
	}
	CatchAndReport(View3D_TextureGetDC, , nullptr);
}
VIEW3D_API void __stdcall View3D_TextureReleaseDC(View3DTexture tex)
{
	try
	{
		if (!tex) throw std::exception("Texture is null");
		tex->ReleaseDC();
	}
	CatchAndReport(View3D_TextureReleaseDC, ,);
}

// Load a texture surface from file
VIEW3D_API void __stdcall View3D_TextureLoadSurface(View3DTexture tex, int level, char const* tex_filepath, RECT const* dst_rect, RECT const* src_rect, UINT32 filter, View3DColour colour_key)
{
	try
	{
		(void)tex,level,tex_filepath,dst_rect,src_rect,filter,colour_key;
		throw std::exception("not implemented");
		#if 0
		try
		{
			tex->LoadSurfaceFromFile(tex_filepath, level, dst_rect, src_rect, filter, colour_key);
			return EView3DResult::Success;
		}
		catch (std::exception const& e)
		{
			PR_LOGE(Rdr().m_log, Exception, e, "Failed to load texture surface from file");
			return EView3DResult::Failed;
		}
		#endif
	}
	CatchAndReport(View3D_TextureLoadSurface, ,);
}

// Release a texture to free memory
VIEW3D_API void __stdcall View3D_TextureDelete(View3DTexture tex)
{
	try
	{
		if (!tex) throw std::exception("Texture is null");
		tex->Release();
	}
	CatchAndReport(View3D_TextureDelete, ,);
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
	CatchAndReport(View3D_TextureGetInfo, ,);
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
	CatchAndReport(View3D_TextureGetInfoFromFile, , EView3DResult::Failed);
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
	CatchAndReport(View3D_TextureGetInfoFromFile, ,);
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
	CatchAndReport(View3D_TextureResize, ,);
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
	CatchAndReport(View3D_TextureResize, window, nullptr);
}

// Rendering ***************************************************************

// Call InvalidateRect on the HWND associated with 'window'
VIEW3D_API void __stdcall View3D_Invalidate(View3DWindow window, BOOL erase)
{
	View3D_InvalidateRect(window, nullptr, erase);
}

// Call InvalidateRect on the HWND associated with 'window'
VIEW3D_API void __stdcall View3D_InvalidateRect(View3DWindow window, RECT const* rect, BOOL erase)
{
	try
	{
		if (!window) throw std::exception("window is null");
		window->InvalidateRect(rect, erase != 0);
	}
	CatchAndReport(View3D_InvalidateRect, window,);
}

// Render 'wnd' into whatever render target is currently set
void RenderWindow(view3d::Window& wnd)
{
	auto& scene = wnd.m_scene;

	// Reset the drawlist
	scene.ClearDrawlists();

	// Add objects from the window to the scene
	for (auto& obj : wnd.m_objects)
		obj->AddToScene(scene);

	// Add gizmos from the window to the scene
	for (auto& giz : wnd.m_gizmos)
		giz->AddToScene(scene);

	// Add the measure tool objects if the window is visible
	if (wnd.m_measure_tool_ui.Visible() && wnd.m_measure_tool_ui.Gfx())
		wnd.m_measure_tool_ui.Gfx()->AddToScene(scene);

	// Add the angle tool objects if the window is visible
	if (wnd.m_angle_tool_ui.Visible() && wnd.m_angle_tool_ui.Gfx())
		wnd.m_angle_tool_ui.Gfx()->AddToScene(scene);

	// Position the focus point
	if (wnd.m_focus_point_visible)
	{
		float scale = wnd.m_focus_point_size * wnd.m_camera.FocusDist();
		wnd.m_focus_point.m_i2w = pr::m4x4::Scale(scale, wnd.m_camera.FocusPoint());
		scene.AddInstance(wnd.m_focus_point);
	}
	// Scale the origin point
	if (wnd.m_origin_point_visible)
	{
		float scale = wnd.m_origin_point_size * pr::Length3(wnd.m_camera.CameraToWorld().pos);
		wnd.m_origin_point.m_i2w = pr::m4x4::Scale(scale, pr::v4Origin);
		scene.AddInstance(wnd.m_origin_point);
	}

	// Set the view and projection matrices
	pr::Camera& cam = wnd.m_camera;
	scene.SetView(cam);
	cam.m_moved = false;

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

	//
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

// Render a window. Remember to call View3D_Present() after all render calls
VIEW3D_API void __stdcall View3D_Render(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");
		auto& wnd = *window;

		DllLockGuard;

		wnd.m_wnd.RestoreRT();
		RenderWindow(wnd);
	}
	CatchAndReport(View3D_Render, window,);
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
	CatchAndReport(View3D_Present, window,);
}

// Render a window into a texture
// 'render_target' is the texture that is rendered onto
// 'depth_buffer' is an optional texture that will receive the depth information (can be null)
VIEW3D_API void __stdcall View3D_RenderTo(View3DWindow window, View3DTexture render_target, View3DTexture depth_buffer)
{
	try
	{
		if (window == nullptr) throw std::exception("window is null");
		if (render_target == nullptr) throw std::exception("Render target texture is null");
		auto& wnd = window->m_wnd;

		DllLockGuard;

		// Get the description of the render target texture
		TextureDesc rtdesc;
		render_target->m_tex->GetDesc(&rtdesc);

		// Get a render target view of the render target texture
		D3DPtr<ID3D11RenderTargetView> rtv;
		pr::Throw(wnd.Device()->CreateRenderTargetView(render_target->m_tex.m_ptr, nullptr, &rtv.m_ptr));

		// If no depth buffer is given, create a temporary depth buffer
		D3DPtr<ID3D11Texture2D> db;
		if (depth_buffer == nullptr)
		{
			TextureDesc dbdesc;
			dbdesc.Width          = rtdesc.Width;
			dbdesc.Height         = rtdesc.Height;
			dbdesc.Format         = wnd.m_db_format;
			dbdesc.SampleDesc     = rtdesc.SampleDesc;
			dbdesc.Usage          = D3D11_USAGE_DEFAULT;
			dbdesc.BindFlags      = D3D11_BIND_DEPTH_STENCIL;
			dbdesc.CPUAccessFlags = 0;
			dbdesc.MiscFlags      = 0;
			pr::Throw(wnd.Device()->CreateTexture2D(&dbdesc, nullptr, &db.m_ptr));
		}
		else
		{
			db = depth_buffer->m_tex;
		}

		// Create a depth stencil view of the depth buffer
		D3DPtr<ID3D11DepthStencilView> dsv = nullptr;
		pr::Throw(wnd.Device()->CreateDepthStencilView(db.m_ptr, nullptr, &dsv.m_ptr));

		// Set the render target
		wnd.SetRT(rtv, dsv);

		// Render the scene
		RenderWindow(*window);
	}
	CatchAndReport(View3D_RenderTo, window,);
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
	CatchAndReport(View3D_RenderTargetSize, window,);
}
VIEW3D_API void __stdcall View3D_SetRenderTargetSize(View3DWindow window, int width, int height)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		if (width  < 0) width  = 0;
		if (height < 0) height = 0;
		window->m_wnd.RenderTargetSize(pr::iv2(width, height));
		auto size = window->m_wnd.RenderTargetSize();

		// Update the window aspect ratio
		float aspect = (size.x == 0 || size.y == 0) ? 1.0f : size.x / float(size.y);
		window->m_camera.Aspect(aspect);
	}
	CatchAndReport(View3D_SetRenderTargetSize, window,);
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
	CatchAndReport(View3D_Viewport, window, View3DViewport());
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
	CatchAndReport(View3D_SetViewport, window,);
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
	CatchAndReport(View3D_FillMode, window, EView3DFillMode());
}
VIEW3D_API void __stdcall View3D_SetFillMode(View3DWindow window, EView3DFillMode mode)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_fill_mode = mode;
	}
	CatchAndReport(View3D_SetFillMode, window,);
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
	CatchAndReport(View3D_Orthographic, window, false);
}
VIEW3D_API void __stdcall View3D_SetOrthographic(View3DWindow window, BOOL render2d)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_camera.m_orthographic = render2d != 0;
	}
	CatchAndReport(View3D_SetOrthographic, window,);
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
	CatchAndReport(View3D_BackgroundColour, window, 0);
}
VIEW3D_API void __stdcall View3D_SetBackgroundColour(View3DWindow window, int aarrggbb)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_background_colour = pr::Colour32(aarrggbb);
	}
	CatchAndReport(View3D_SetBackgroundColour, window,);
}

// Tools *******************************************************************

// Show the measurement tool
VIEW3D_API BOOL __stdcall View3D_MeasureToolVisible(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		return window->m_measure_tool_ui.Visible();
	}
	CatchAndReport(View3D_MeasureToolVisible, window, false);
}
VIEW3D_API void __stdcall View3D_ShowMeasureTool(View3DWindow window, BOOL show)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_measure_tool_ui.SetReadPoint(&view3d::Window::ReadPoint, window);
		window->m_measure_tool_ui.Visible(show != 0);
	}
	CatchAndReport(View3D_ShowMeasureTool, window,);
}

// Show the angle tool
VIEW3D_API BOOL __stdcall View3D_AngleToolVisible(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		return window->m_angle_tool_ui.Visible();
	}
	CatchAndReport(View3D_AngleToolVisible, window, false);
}
VIEW3D_API void __stdcall View3D_ShowAngleTool(View3DWindow window, BOOL show)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_angle_tool_ui.SetReadPoint(&view3d::Window::ReadPoint, window);
		window->m_angle_tool_ui.Visible(show != 0);
	}
	CatchAndReport(View3D_ShowAngleTool, window,);
}

// Gizmos *******************************************************************

// Create a new instance of a gizmo
VIEW3D_API View3DGizmo __stdcall View3D_GizmoCreate(EView3DGizmoMode mode, View3DM4x4 const& o2w)
{
	using namespace pr::ldr;

	try
	{
		DllLockGuard;
		auto giz = LdrGizmoPtr(new LdrGizmo(Dll().m_rdr, static_cast<LdrGizmo::EMode>(mode), view3d::To<pr::m4x4>(o2w)));
		Dll().m_giz_cont.push_back(giz);
		return giz.m_ptr;
	}
	CatchAndReport(View3D_GizmoCreate, , nullptr);
}

// Delete a gizmo instance
VIEW3D_API void __stdcall View3D_GizmoDelete(View3DGizmo gizmo)
{
	try
	{
		if (!gizmo) return;
		
		DllLockGuard;

		// Remove the gizmo from any windows it's in
		for (auto wnd : Dll().m_wnd_cont)
			View3D_RemoveGizmo(wnd, gizmo);
		
		// Delete the gizmo from the gizmo container (removing the last reference)
		pr::erase_first(Dll().m_giz_cont, [&](pr::ldr::LdrGizmoPtr p){ return p.m_ptr == gizmo; });
	}
	CatchAndReport(View3D_GizmoDelete, ,);
}

// Attach?Detach callbacks that are called when the gizmo moves
VIEW3D_API void __stdcall View3D_GizmoAttachCB(View3DGizmo gizmo, View3D_GizmoMovedCB cb, void* ctx)
{
	try
	{
		if (!gizmo) throw std::exception("Gizmo is null");
		if (!cb) throw std::exception("Callback function is null");

		DllLockGuard;
		gizmo->Attach(reinterpret_cast<pr::ldr::LdrGizmoCB::Func>(cb), ctx);
	}
	CatchAndReport(View3D_GizmoAttachCB, ,);
}
VIEW3D_API void __stdcall View3D_GizmoDetachCB(View3DGizmo gizmo, View3D_GizmoMovedCB cb)
{
	try
	{
		if (!gizmo) throw std::exception("Gizmo is null");
		if (!cb) throw std::exception("Callback function is null");

		DllLockGuard;
		gizmo->Detach(reinterpret_cast<pr::ldr::LdrGizmoCB::Func>(cb));
	}
	CatchAndReport(View3D_GizmoDetachCB, ,);
}

// Attach/Detach an object to the gizmo that will be moved as the gizmo moves
VIEW3D_API void __stdcall View3D_GizmoAttach(View3DGizmo gizmo, View3DObject obj)
{
	try
	{
		if (!gizmo) throw std::exception("Gizmo is null");
		if (!obj) throw std::exception("Object is null");

		DllLockGuard;
		gizmo->Attach(obj->m_o2p);
	}
	CatchAndReport(View3D_GizmoAttach, ,);
}
VIEW3D_API void __stdcall View3D_GizmoDetach(View3DGizmo gizmo, View3DObject obj)
{
	try
	{
		if (!gizmo) throw std::exception("Gizmo is null");

		DllLockGuard;
		gizmo->Detach(obj->m_o2p);
	}
	CatchAndReport(View3D_GizmoDetach, ,);
}

// Get/Set the current mode of the gizmo
VIEW3D_API EView3DGizmoMode __stdcall View3D_GizmoGetMode(View3DGizmo gizmo)
{
	try
	{
		if (!gizmo) throw std::exception("Gizmo is null");
		return static_cast<EView3DGizmoMode>(gizmo->Mode());
	}
	CatchAndReport(View3D_GizmoGetMode, ,static_cast<EView3DGizmoMode>(-1));
}
VIEW3D_API void __stdcall View3D_GizmoSetMode(View3DGizmo gizmo, EView3DGizmoMode mode)
{
	try
	{
		if (!gizmo) throw std::exception("Gizmo is null");
		gizmo->Mode(static_cast<pr::ldr::LdrGizmo::EMode>(mode));
	}
	CatchAndReport(View3D_GizmoSetMode, ,);
}

// Get/Set the object to world transform for the gizmo
VIEW3D_API View3DM4x4 __stdcall View3D_GizmoGetO2W(View3DGizmo gizmo)
{
	try
	{
		if (!gizmo) throw std::exception("Gizmo is null");
		return view3d::To<View3DM4x4>(gizmo->O2W());
	}
	CatchAndReport(View3D_GizmoGetO2W, ,View3DM4x4());
}
VIEW3D_API void __stdcall View3D_GizmoSetO2W(View3DGizmo gizmo, View3DM4x4 const& o2w)
{
	try
	{
		if (!gizmo) throw std::exception("Gizmo is null");
		gizmo->O2W(view3d::To<pr::m4x4>(o2w));
	}
	CatchAndReport(View3D_GizmoSetO2W, ,);
}

// Get the offset transform that represents the difference between
// the gizmo's transform at the start of manipulation and now.
VIEW3D_API View3DM4x4 __stdcall View3D_GizmoGetOffset(View3DGizmo gizmo)
{
	try
	{
		if (!gizmo) throw std::exception("Gizmo is null");
		return view3d::To<View3DM4x4>(gizmo->Offset());
	}
	CatchAndReport(View3D_GizmoGetOffset, ,View3DM4x4());
}

// Get/Set whether the gizmo is active to mouse interaction
VIEW3D_API BOOL __stdcall View3D_GizmoEnabled(View3DGizmo gizmo)
{
	try
	{
		if (!gizmo) throw std::exception("Gizmo is null");
		return gizmo->Enabled();
	}
	CatchAndReport(View3D_GizmoEnabled, ,FALSE);
}
VIEW3D_API void __stdcall View3D_GizmoSetEnabled(View3DGizmo gizmo, BOOL enabled)
{
	try
	{
		if (!gizmo) throw std::exception("Gizmo is null");
		gizmo->Enabled(enabled != 0);
	}
	CatchAndReport(View3D_GizmoSetEnabled, ,);
}

// Returns true while manipulation is in progress
VIEW3D_API BOOL __stdcall View3D_GizmoManipulating(View3DGizmo gizmo)
{
	try
	{
		if (!gizmo) throw std::exception("Gizmo is null");
		return gizmo->Manipulating();
	}
	CatchAndReport(View3D_GizmoManipulating, ,FALSE);
}

// Miscellaneous **********************************************************************

// Restore the main render target and depth buffer
VIEW3D_API void __stdcall View3D_RestoreMainRT(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		window->m_wnd.RestoreRT();
	}
	CatchAndReport(View3D_RestoreMainRT, window,);
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
	CatchAndReport(View3D_DepthBufferEnabled, window, TRUE);
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
	CatchAndReport(View3D_SetDepthBufferEnabled, window,);
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
	CatchAndReport(View3D_FocusPointVisible, window, false);
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
	CatchAndReport(View3D_ShowFocusPoint, window,);
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
	CatchAndReport(View3D_SetFocusPointSize, window,);
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
	CatchAndReport(View3D_OriginVisible, window, false);
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
	CatchAndReport(View3D_ShowOrigin, window,);
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
	CatchAndReport(View3D_SetOriginSize, window,);
}

// Create a scene showing the capabilities of view3d (actually of ldr_object_manager)
VIEW3D_API void __stdcall View3D_CreateDemoScene(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		
		auto scene = pr::ldr::CreateDemoScene();
		pr::script::PtrW<> src(scene.c_str());
		pr::script::Reader reader(src, false, nullptr, nullptr, &Dll().m_lua);

		pr::ldr::ParseResult out;
		pr::ldr::Parse(Dll().m_rdr, reader, out, true, pr::GuidZero);
		for (auto& obj : out.m_objects)
			View3D_AddObject(window, obj.m_ptr);
	}
	CatchAndReport(View3D_CreateDemoScene, window,);
}

// Show a window containing the demo scene script
VIEW3D_API void __stdcall View3D_ShowDemoScript(View3DWindow window)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		auto& ui = window->m_editor_ui;
		ui.Show(window->m_hwnd);
		ui.Text(pr::ldr::CreateDemoScene().c_str());
	}
	CatchAndReport(View3D_ShowDemoScript, window,);
}

// Display the object manager UI
VIEW3D_API void __stdcall View3D_ShowObjectManager(View3DWindow window, BOOL show)
{
	try
	{
		if (!window) throw std::exception("window is null");

		DllLockGuard;
		auto& ui = window->m_obj_cont_ui;
		ui.Show();
		ui.Populate(window->m_objects);
		ui.Visible(show != 0);
	}
	CatchAndReport(View3D_ShowObjectManager, window,);
}

// Parse an ldr *o2w {} description returning the transform
VIEW3D_API View3DM4x4 __stdcall View3D_ParseLdrTransform(char const* ldr_script)
{
	try
	{
		pr::script::PtrA<> src(ldr_script);
		pr::script::Reader reader(src);
		return view3d::To<View3DM4x4>(pr::ldr::ParseLdrTransform(reader));
	}
	CatchAndReport(View3D_ParseLdrTransform, , view3d::To<View3DM4x4>(pr::m4x4Identity));
}

// Create/Destroy a scintilla editor window set up for ldr script editing
VIEW3D_API HWND __stdcall View3D_LdrEditorCreate(HWND parent)
{
	try
	{
		// Create an instance of an editor window and save its pointer
		// in the user data for the window. This means the 'hwnd' is
		// effectively a handle for the allocated window.
		// Do nothing other than create the window here, callers can
		// then restyle, move, show/hide, etc the window as they want.
		auto editor = std::make_unique<pr::ldr::ScriptEditorDlg>(parent);
		HWND hwnd = *editor;
		::SetLastError(0);
		auto prev = ::SetWindowLongPtrA(hwnd, GWLP_USERDATA, LONG_PTR(editor.get()));
		if (prev != 0 || ::GetLastError() != 0) throw std::exception("Error while creating editor window");
		editor.release();
		return hwnd;
	}
	CatchAndReport(View3D_LdrEditorCreate, , 0);
}
VIEW3D_API void __stdcall View3D_LdrEditorDestroy(HWND hwnd)
{
	try
	{
		if (hwnd == 0) return;

		EditorPtr edt(reinterpret_cast<pr::ldr::ScriptEditorDlg*>(::GetWindowLongPtrA(hwnd, GWLP_USERDATA)));
		if (!edt) throw std::exception("No back reference pointer found for this window");
		::SetWindowLongPtrA(hwnd, GWLP_USERDATA, 0);
		// 'edt' going out of scope should delete it
	}
	CatchAndReport(View3D_LdrEditorDestroy, ,);
}

// Initialise a scintilla control ready for ldr script
VIEW3D_API void __stdcall View3D_LdrEditorCtrlInit(HWND scintilla_control, BOOL dark)
{
	try
	{
		if (!scintilla_control) throw std::exception("scintilla control handle is null");

		pr::gui::ScintillaCtrl s;
		s.Attach(scintilla_control);
		s.InitLdrStyle(dark != 0);
		s.Detach();
	}
	CatchAndReport(View3D_LdrEditorCtrlInit, ,);
}

#pragma region API Constants Consistency

template <typename T, typename U> struct equal_size_and_alignment
{
	enum { value = sizeof(T) == sizeof(U) && std::alignment_of<T>::value == std::alignment_of<U>::value };
};

// EView3DFillMode - only used in this file

static_assert(int(EView3DGeom::Unknown) == int(pr::rdr::EGeom::Invalid), "");
static_assert(int(EView3DGeom::Vert   ) == int(pr::rdr::EGeom::Vert   ), "");
static_assert(int(EView3DGeom::Colr   ) == int(pr::rdr::EGeom::Colr   ), "");
static_assert(int(EView3DGeom::Norm   ) == int(pr::rdr::EGeom::Norm   ), "");
static_assert(int(EView3DGeom::Tex0   ) == int(pr::rdr::EGeom::Tex0   ), "");

static_assert(int(EView3DGizmoEvent::StartManip) == int(pr::ldr::ELdrGizmoEvent::StartManip), "");
static_assert(int(EView3DGizmoEvent::Moving    ) == int(pr::ldr::ELdrGizmoEvent::Moving    ), "");
static_assert(int(EView3DGizmoEvent::Commit    ) == int(pr::ldr::ELdrGizmoEvent::Commit    ), "");
static_assert(int(EView3DGizmoEvent::Revert    ) == int(pr::ldr::ELdrGizmoEvent::Revert    ), "");


static_assert(int(EView3DPrim::Invalid  ) == int(pr::rdr::EPrim::Invalid  ), "");
static_assert(int(EView3DPrim::PointList) == int(pr::rdr::EPrim::PointList), "");
static_assert(int(EView3DPrim::LineList ) == int(pr::rdr::EPrim::LineList ), "");
static_assert(int(EView3DPrim::LineStrip) == int(pr::rdr::EPrim::LineStrip), "");
static_assert(int(EView3DPrim::TriList  ) == int(pr::rdr::EPrim::TriList  ), "");
static_assert(int(EView3DPrim::TriStrip ) == int(pr::rdr::EPrim::TriStrip ), "");

static_assert(int(EView3DLight::Ambient    ) == int(pr::rdr::ELight::Ambient    ), "");
static_assert(int(EView3DLight::Directional) == int(pr::rdr::ELight::Directional), "");
static_assert(int(EView3DLight::Point      ) == int(pr::rdr::ELight::Point      ), "");
static_assert(int(EView3DLight::Spot       ) == int(pr::rdr::ELight::Spot       ), "");
	
// EView3DLogLevel - unused?

static_assert(int(EView3DUpdateObject::None      ) == int(pr::ldr::EUpdateObject::None      ), "");
static_assert(int(EView3DUpdateObject::All       ) == int(pr::ldr::EUpdateObject::All       ), "");
static_assert(int(EView3DUpdateObject::Name      ) == int(pr::ldr::EUpdateObject::Name      ), "");
static_assert(int(EView3DUpdateObject::Model     ) == int(pr::ldr::EUpdateObject::Model     ), "");
static_assert(int(EView3DUpdateObject::Transform ) == int(pr::ldr::EUpdateObject::Transform ), "");
static_assert(int(EView3DUpdateObject::Children  ) == int(pr::ldr::EUpdateObject::Children  ), "");
static_assert(int(EView3DUpdateObject::Colour    ) == int(pr::ldr::EUpdateObject::Colour    ), "");
static_assert(int(EView3DUpdateObject::ColourMask) == int(pr::ldr::EUpdateObject::ColourMask), "");
static_assert(int(EView3DUpdateObject::Wireframe ) == int(pr::ldr::EUpdateObject::Wireframe ), "");
static_assert(int(EView3DUpdateObject::Visibility) == int(pr::ldr::EUpdateObject::Visibility), "");
static_assert(int(EView3DUpdateObject::Animation ) == int(pr::ldr::EUpdateObject::Animation ), "");
static_assert(int(EView3DUpdateObject::StepData  ) == int(pr::ldr::EUpdateObject::StepData  ), "");

static_assert(int(EView3DGizmoEvent::StartManip) == int(pr::ldr::ELdrGizmoEvent::StartManip), "");
static_assert(int(EView3DGizmoEvent::Moving    ) == int(pr::ldr::ELdrGizmoEvent::Moving    ), "");
static_assert(int(EView3DGizmoEvent::Commit    ) == int(pr::ldr::ELdrGizmoEvent::Commit    ), "");
static_assert(int(EView3DGizmoEvent::Revert    ) == int(pr::ldr::ELdrGizmoEvent::Revert    ), "");

static_assert(int(EView3DGizmoMode::Translate) == int(pr::ldr::LdrGizmo::EMode::Translate), "");
static_assert(int(EView3DGizmoMode::Rotate   ) == int(pr::ldr::LdrGizmo::EMode::Rotate   ), "");
static_assert(int(EView3DGizmoMode::Scale    ) == int(pr::ldr::LdrGizmo::EMode::Scale    ), "");

// Specifically used to avoid alignment problems
static_assert(sizeof(View3DV2    ) == sizeof(pr::v2       ), "");
static_assert(sizeof(View3DV4    ) == sizeof(pr::v4       ), "");
static_assert(sizeof(View3DM4x4  ) == sizeof(pr::m4x4     ), "");
static_assert(sizeof(View3DBBox  ) == sizeof(pr::BBox     ), "");
// View3DVertex - only used in this file
// View3DImageInfo - only used in this file
// View3DLight - only used in this file
// View3DTextureOptions - only used in this file
// View3DUpdateModelKeep - only used in this file
// View3DMaterial - only used in this file
// View3DViewport - only used in this file
static_assert(equal_size_and_alignment<View3DGizmoEvent, pr::ldr::Evt_Gizmo>::value, "");

#pragma endregion