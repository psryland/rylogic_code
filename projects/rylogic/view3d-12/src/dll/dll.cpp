//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
// The view3d dll is loaded once per application, although an application may have
// multiple windows and may call Initialise/Shutdown a number of times.
// Ldr objects can be created independently to windows. This means we need one global
// context within the dll, one renderer, and one list of objects.
//
// Error/Log handling:
//  Each window represents a separate context from the callers point of view, this
//  means we need an error handler per window. Also, within a window, callers may
//  want to temporarily push a different error handler. Each window maintains a
//  stack of error handlers.
#include "pr/view3d-12/view3d-dll.h"
#include "pr/view3d-12/model/model.h"
#include "pr/view3d-12/texture/texture_desc.h"
#include "pr/view3d-12/texture/texture_2d.h"
#include "pr/view3d-12/texture/texture_cube.h"
#include "pr/view3d-12/utility/conversion.h"
#include "pr/view3d-12/utility/utility.h"
#include "pr/view3d-12/ldraw/ldr_object.h"
#include "pr/view3d-12/ldraw/ldr_gizmo.h"
#include "view3d-12/src/dll/dll_forward.h"
#include "view3d-12/src/dll/context.h"
#include "view3d-12/src/dll/v3d_window.h"

using namespace pr;
using namespace pr::rdr12;
using namespace pr::view3d;
//using namespace pr::log;

// DLL entry point
HINSTANCE g_hInstance;
BOOL APIENTRY DllMain(HINSTANCE hInstance, DWORD ul_reason_for_call, LPVOID)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH: g_hInstance = hInstance; break;
	case DLL_PROCESS_DETACH: g_hInstance = nullptr; break;
	case DLL_THREAD_ATTACH:  break;
	case DLL_THREAD_DETACH:  break;
	}
	return TRUE;
}

// Global DLL context
static Context* g_ctx = nullptr;
static Context& Dll()
{
	if (g_ctx) return *g_ctx;
	throw std::runtime_error("View3d not initialised");
}

// Types
using LockGuard = std::lock_guard<std::recursive_mutex>;

// Helper macros for exception trapping in API functions
#define DllLockGuard LockGuard lock(Dll().m_mutex)
#define CatchAndReport(func_name, wnd, ret)\
	catch (std::exception const& ex) { Dll().ReportAPIError(#func_name, view3d::Window(wnd), &ex); }\
	catch (...)                      { Dll().ReportAPIError(#func_name, view3d::Window(wnd), nullptr); }\
	return ret

// Dll Context ****************************

// Initialise calls are reference counted and must be matched with Shutdown calls
// 'initialise_error_cb' is used to report dll initialisation errors only (i.e. it isn't stored)
// Note: this function is not thread safe, avoid race calls
VIEW3D_API DllHandle  __stdcall View3D_Initialise(view3d::ReportErrorCB global_error_cb, void* ctx)
{
	auto error_cb = StaticCallback(global_error_cb, ctx);
	try
	{
		// Create the dll context on the first call
		if (g_ctx == nullptr)
			g_ctx = new Context(g_hInstance, error_cb);

		// Generate a unique handle per Initialise call, used to match up with Shutdown calls
		static DllHandle handles = nullptr;
		g_ctx->m_inits.insert(++handles);
		return handles;
	}
	catch (std::exception const& e)
	{
		error_cb(FmtS(L"Failed to initialise View3D.\nReason: %S\n", e.what()), L"", 0, 0);
		return nullptr;
	}
	catch (...)
	{
		error_cb(L"Failed to initialise View3D.\nReason: An unknown exception occurred\n", L"", 0, 0);
		return nullptr;
	}
}
VIEW3D_API void __stdcall View3D_Shutdown(DllHandle context)
{
	if (!g_ctx) return;

	g_ctx->m_inits.erase(context);
	if (!g_ctx->m_inits.empty())
		return;

	delete g_ctx;
	g_ctx = nullptr;
}

// Replace the global error handler
VIEW3D_API void __stdcall View3D_GlobalErrorCBSet(view3d::ReportErrorCB error_cb, void* ctx, BOOL add)
{
	try
	{
		DllLockGuard;
		if (add)
			Dll().ReportError += StaticCallback(error_cb, ctx);
		else
			Dll().ReportError -= StaticCallback(error_cb, ctx);
	}
	CatchAndReport(View3D_GlobalErrorCBSet, , );
}

// Data Sources ***************************

// Create an include handler that can load from directories or embedded resources
static script::Includes GetIncludes(view3d::Includes const* includes)
{
	if (includes == nullptr)
		return script::Includes{};

	script::Includes inc;
	if (includes->m_include_paths != nullptr)
		inc.SearchPathList(includes->m_include_paths);

	if (includes->m_module_count != 0)
		inc.ResourceModules(std::initializer_list<HMODULE>(includes->m_modules, includes->m_modules + includes->m_module_count));

	return inc;
}

// Add an ldr script source. This will create all objects with context id 'context_id' (if given, otherwise an id will be created). Concurrent calls are thread safe.
VIEW3D_API GUID __stdcall View3D_LoadScriptFromString(char const* ldr_script, GUID const* context_id, view3d::Includes const* includes, view3d::OnAddCB on_add_cb, void* ctx)
{
	try
	{
		// Concurrent entry is allowed
		auto on_add = [=](Guid const& id, bool before) { on_add_cb(ctx, id, before); };
		return Dll().LoadScript(std::string_view(ldr_script), false, EEncoding::utf8, context_id, GetIncludes(includes), on_add_cb ? on_add : (rdr12::OnAddCB)nullptr);
	}
	CatchAndReport(View3D_LoadScriptFromString, (view3d::Window)nullptr, GuidZero);
}
VIEW3D_API GUID __stdcall View3D_LoadScriptFromFile(char const* ldr_file, GUID const* context_id, view3d::Includes const* includes, view3d::OnAddCB on_add_cb, void* ctx)
{
	try
	{
		// Concurrent entry is allowed
		auto on_add = [=](Guid const& id, bool before) { on_add_cb(ctx, id, before); };
		return Dll().LoadScript(std::string_view(ldr_file), true, EEncoding::auto_detect, context_id, GetIncludes(includes), on_add_cb ? on_add : (rdr12::OnAddCB)nullptr);
	}
	CatchAndReport(View3D_LoadScriptFromFile, (view3d::Window)nullptr, GuidZero);
}

// Enumerate the Guids of objects in the sources collection
VIEW3D_API void __stdcall View3D_SourceEnumGuids(view3d::EnumGuidsCB enum_guids_cb, void* ctx)
{
	try
	{
		DllLockGuard;
		Dll().SourceEnumGuids(StaticCallback(enum_guids_cb, ctx));
	}
	CatchAndReport(View3D_SourceEnumGuids,, );
}

// Windows ********************************

// Create/Destroy a window
VIEW3D_API view3d::Window __stdcall View3D_WindowCreate(HWND hwnd, view3d::WindowOptions const& opts)
{
	try
	{
		DllLockGuard;
		return Dll().WindowCreate(hwnd, opts);
	}
	CatchAndReport(View3D_WindowCreate,, nullptr);
}
VIEW3D_API void __stdcall View3D_WindowDestroy(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		Dll().WindowDestroy(window);
	}
	CatchAndReport(View3D_WindowDestroy,window,);
}

// Add/Remove a window error callback. Note: The callback function can be called in a worker thread context.
VIEW3D_API void __stdcall View3D_WindowErrorCBSet(view3d::Window window, view3d::ReportErrorCB error_cb, void* ctx, BOOL add)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");
		if (add)
			window->ReportError += StaticCallback(error_cb, ctx);
		else
			window->ReportError -= StaticCallback(error_cb, ctx);
	}
	CatchAndReport(View3D_WindowErrorCBSet, window, );
}

// Get/Set the window settings (as ldr script string)
VIEW3D_API wchar_t const* __stdcall View3D_WindowSettingsGet(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");
		return window->Settings();
	}
	CatchAndReport(View3D_WindowSettingsGet, window, L"");
}
VIEW3D_API void __stdcall View3D_WindowSettingsSet(view3d::Window window, wchar_t const* settings)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");
		window->Settings(settings);
	}
	CatchAndReport(View3D_WindowSettingsSet, window,);
}

// Get/Set the dimensions of the render target
// In set, if 'width' and 'height' are zero, the RT is resized to the associated window automatically.
VIEW3D_API BOOL __stdcall View3D_WindowBackBufferSizeGet(view3d::Window window, int& width, int& height)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		auto area = window->BackBufferSize();
		width = area.x;
		height = area.y;
		return TRUE;
	}
	CatchAndReport(View3D_WindowBackBufferSizeGet, window, FALSE);
}
VIEW3D_API void __stdcall View3D_WindowBackBufferSizeSet(view3d::Window window, int width, int height)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->BackBufferSize(iv2{width, height});
	}
	CatchAndReport(View3D_WindowBackBufferSizeSet, window,);
}

// Get/Set the window viewport (and clipping area)
VIEW3D_API view3d::Viewport __stdcall View3D_WindowViewportGet(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->Viewport();
	}
	CatchAndReport(View3D_WindowViewportGet, window, view3d::Viewport{});
}
VIEW3D_API void __stdcall View3D_WindowViewportSet(view3d::Window window, view3d::Viewport const& vp)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->Viewport(vp);
	}
	CatchAndReport(View3D_WindowViewportSet, window,);
}

// Set a notification handler for when a window setting changes
VIEW3D_API void __stdcall View3D_WindowSettingsChangedCB(view3d::Window window, view3d::SettingsChangedCB settings_changed_cb, void* ctx, BOOL add)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");
		if (add)
			window->OnSettingsChanged += StaticCallback(settings_changed_cb, ctx);
		else
			window->OnSettingsChanged -= StaticCallback(settings_changed_cb, ctx);
	}
	CatchAndReport(View3D_WindowSettingsChangedCB, window,);
}

// Add an object to a window
VIEW3D_API void __stdcall View3D_WindowAddObject(view3d::Window window, view3d::Object object)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");
		if (!object) throw std::runtime_error("object is null");
		
		DllLockGuard;
		window->Add(object);
	}
	CatchAndReport(View3D_WindowAddObject, window,);
}

// Add/Remove objects by context id. This function can be used to add all objects either in, or not in 'context_ids'
VIEW3D_API void __stdcall View3D_WindowAddObjectsById(view3d::Window window, GUID const* context_ids, int include_count, int exclude_count)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->Add(context_ids, include_count, exclude_count);
	}
	CatchAndReport(View3D_WindowAddObjectsById, window,);
}
VIEW3D_API void __stdcall View3D_WindowRemoveObjectsById(view3d::Window window, GUID const* context_ids, int include_count, int exclude_count)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->Remove(context_ids, include_count, exclude_count, false);
	}
	CatchAndReport(View3D_WindowRemoveObjectsById, window,);
}

// Enumerate the object collection guids associated with 'window'
VIEW3D_API void __stdcall View3D_WindowEnumGuids(view3d::Window window, view3d::EnumGuidsCB enum_guids_cb, void* ctx)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->EnumGuids({ enum_guids_cb, ctx });
	}
	CatchAndReport(View3D_WindowEnumGuids, window, );
}

// Enumerate the objects associated with 'window'
VIEW3D_API void __stdcall View3D_WindowEnumObjects(view3d::Window window, view3d::EnumObjectsCB enum_objects_cb, void* ctx)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->EnumObjects({ enum_objects_cb, ctx });
	}
	CatchAndReport(View3D_WindowEnumObjects, window, );
}
VIEW3D_API void __stdcall View3D_WindowEnumObjectsById(view3d::Window window, view3d::EnumObjectsCB enum_objects_cb, void* ctx, GUID const* context_ids, int include_count, int exclude_count)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->EnumObjects({ enum_objects_cb, ctx }, context_ids, include_count, exclude_count);
	}
	CatchAndReport(View3D_WindowEnumObjectsById, window, );
}

// Render the window
VIEW3D_API void __stdcall View3D_WindowRender(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->Render();
	}
	CatchAndReport(View3D_WindowRender, window,);
}

// Call InvalidateRect on the HWND associated with 'window'
VIEW3D_API void __stdcall View3D_WindowInvalidate(view3d::Window window, BOOL erase)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");
		window->Invalidate(erase != 0);
	}
	CatchAndReport(View3D_Invalidate, window, );
}

// Call InvalidateRect on the HWND associated with 'window'
VIEW3D_API void __stdcall View3D_WindowInvalidateRect(view3d::Window window, RECT const* rect, BOOL erase)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");
		window->InvalidateRect(rect, erase != 0);
	}
	CatchAndReport(View3D_InvalidateRect, window,);
}

// Register a callback for when the window is invalidated.
// This can be used to render in response to invalidation, rather than rendering on a polling cycle.
VIEW3D_API void __stdcall View3D_WindowInvalidatedCB(view3d::Window window, view3d::InvalidatedCB invalidated_cb, void* ctx, BOOL add)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		if (add)
			window->OnInvalidated += StaticCallback(invalidated_cb, ctx);
		else
			window->OnInvalidated -= StaticCallback(invalidated_cb, ctx);
	}
	CatchAndReport(View3D_WindowInvalidatedCB, window,);
}

// Get/Set the window background colour
VIEW3D_API unsigned int __stdcall View3D_WindowBackgroundColourGet(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->BackgroundColour().argb().argb;
	}
	CatchAndReport(View3D_WindowBackgroundColourGet, window, 0U);
}
VIEW3D_API void __stdcall View3D_WindowBackgroundColourSet(view3d::Window window, unsigned int argb)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->BackgroundColour(pr::Colour(Colour32(argb)));
	}
	CatchAndReport(View3D_WindowBackgroundColourSet, window,);
}

// Set the global environment map for the window
VIEW3D_API void __stdcall View3D_WindowEnvMapSet(view3d::Window window, view3d::CubeMap env_map)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->EnvMap(env_map);
	}
	CatchAndReport(View3D_WindowEnvMapSet, window, );
}

// Camera *********************************

// Position the camera and focus distance
VIEW3D_API void __stdcall View3D_CameraPositionSet(view3d::Window window, view3d::Vec4 position, view3d::Vec4 lookat, view3d::Vec4 up)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_scene.m_cam.LookAt(To<v4>(position), To<v4>(lookat), To<v4>(up), true);
	}
	CatchAndReport(View3D_CameraPositionSet, window,);
}


// Get/Set the current camera to world transform
VIEW3D_API view3d::Mat4x4 __stdcall View3D_CameraToWorldGet(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return To<view3d::Mat4x4>(window->m_scene.m_cam.CameraToWorld());
	}
	CatchAndReport(View3D_CameraToWorldGet, window, view3d::Mat4x4{});
}
VIEW3D_API void __stdcall View3D_CameraToWorldSet(view3d::Window window, view3d::Mat4x4 const& c2w)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_scene.m_cam.CameraToWorld(To<m4x4>(c2w));
	}
	CatchAndReport(View3D_CameraToWorldSet, window,);
}

// General mouse navigation
// 'ss_pos' is the mouse pointer position in 'window's screen space
// 'nav_op' is the navigation type
// 'nav_start_or_end' should be TRUE on mouse down/up events, FALSE for mouse move events
// void OnMouseDown(UINT nFlags, CPoint point) { View3D_MouseNavigate(win, point, nav_op, TRUE); }
// void OnMouseMove(UINT nFlags, CPoint point) { View3D_MouseNavigate(win, point, nav_op, FALSE); } if 'nav_op' is None, this will have no effect
// void OnMouseUp  (UINT nFlags, CPoint point) { View3D_MouseNavigate(win, point, 0, TRUE); }
// BOOL OnMouseWheel(UINT nFlags, short zDelta, CPoint) { if (nFlags == 0) View3D_MouseNavigateZ(win, 0, 0, zDelta / 120.0f); return TRUE; }
VIEW3D_API BOOL __stdcall View3D_MouseNavigate(view3d::Window window, view3d::Vec2 ss_pos, view3d::ENavOp nav_op, BOOL nav_start_or_end)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->MouseNavigate(To<v2>(ss_pos), static_cast<camera::ENavOp>(nav_op), nav_start_or_end != 0);
	}
	CatchAndReport(View3D_MouseNavigate, window, FALSE);
}
VIEW3D_API BOOL __stdcall View3D_MouseNavigateZ(view3d::Window window, view3d::Vec2 ss_pos, float delta, BOOL along_ray)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->MouseNavigateZ(To<v2>(ss_pos), delta, along_ray != 0);
	}
	CatchAndReport(View3D_MouseNavigate, window, FALSE);
}

// Convert a point between 'window' screen space and normalised screen space
VIEW3D_API view3d::Vec2 __stdcall View3D_SSPointToNSSPoint(view3d::Window window, view3d::Vec2 screen)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return To<view3d::Vec2>(window->m_scene.m_viewport.SSPointToNSSPoint(To<v2>(screen)));
	}
	CatchAndReport(View3D_SSPointToNSSPoint, window, view3d::Vec2{});
}
VIEW3D_API view3d::Vec2 __stdcall View3D_NSSPointToSSPoint(view3d::Window window, view3d::Vec2 nss_point)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return To<view3d::Vec2>(window->m_scene.m_viewport.NSSPointToSSPoint(To<v2>(nss_point)));
	}
	CatchAndReport(View3D_NSSPointToSSPoint, window, view3d::Vec2{});
}

// Convert a point between world space and normalised screen space.
// The x,y components of 'screen' should be in normalised screen space, i.e. (-1,-1)->(1,1)
// The z component should be the world space distance from the camera
VIEW3D_API view3d::Vec4 __stdcall View3D_NSSPointToWSPoint(view3d::Window window, view3d::Vec4 screen)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return To<view3d::Vec4>(window->m_scene.m_cam.NSSPointToWSPoint(To<v4>(screen)));
	}
	CatchAndReport(View3D_NSSPointToWSPoint, window, view3d::Vec4());
}
VIEW3D_API view3d::Vec4 __stdcall View3D_WSPointToNSSPoint(view3d::Window window, view3d::Vec4 world)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return To<view3d::Vec4>(window->m_scene.m_cam.WSPointToNSSPoint(To<v4>(world)));
	}
	CatchAndReport(View3D_WSPointToNSSPoint, window, view3d::Vec4{});
}

// Return a point and direction in world space corresponding to a normalised screen space point.
// The x,y components of 'screen' should be in normalised screen space, i.e. (-1,-1)->(1,1)
// The z component should be the world space distance from the camera
VIEW3D_API void __stdcall View3D_NSSPointToWSRay(view3d::Window window, view3d::Vec4 screen, view3d::Vec4& ws_point, view3d::Vec4& ws_direction)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		auto [pt, dir] = window->m_scene.m_cam.NSSPointToWSRay(To<v4>(screen));
		ws_point = To<view3d::Vec4>(pt);
		ws_direction = To<view3d::Vec4>(dir);
	}
	CatchAndReport(View3D_NSSPointToWSRay, window,);
}

// Lights *********************************

// Get/Set the properties of the global light
VIEW3D_API view3d::Light __stdcall View3D_LightPropertiesGet(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		auto global_light = window->GlobalLight();
		return view3d::Light {
			.m_position       = To<view3d::Vec4>(global_light.m_position),
			.m_direction      = To<view3d::Vec4>(global_light.m_direction),
			.m_type           = s_cast<view3d::ELight>(global_light.m_type),
			.m_ambient        = global_light.m_ambient.argb,
			.m_diffuse        = global_light.m_diffuse.argb,
			.m_specular       = global_light.m_specular.argb,
			.m_specular_power = global_light.m_specular_power,
			.m_range          = global_light.m_range,
			.m_falloff        = global_light.m_falloff,
			.m_inner_angle    = global_light.m_inner_angle,
			.m_outer_angle    = global_light.m_outer_angle,
			.m_cast_shadow    = global_light.m_cast_shadow,
			.m_cam_relative   = global_light.m_cam_relative,
			.m_on             = global_light.m_on,
		};
	}
	CatchAndReport(View3D_LightPropertiesGet, window, {});
}
VIEW3D_API void __stdcall View3D_LightPropertiesSet(view3d::Window window, view3d::Light const& light)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");
		assert(light.m_position.w == 1);

		DllLockGuard;
		rdr12::Light global_light;
		global_light.m_position       = To<v4>(light.m_position);
		global_light.m_direction      = To<v4>(light.m_direction);
		global_light.m_type           = Enum<rdr12::ELight>::From(light.m_type);
		global_light.m_ambient        = light.m_ambient;
		global_light.m_diffuse        = light.m_diffuse;
		global_light.m_specular       = light.m_specular;
		global_light.m_specular_power = light.m_specular_power;
		global_light.m_range          = light.m_range;
		global_light.m_falloff        = light.m_falloff;
		global_light.m_inner_angle    = light.m_inner_angle;
		global_light.m_outer_angle    = light.m_outer_angle;
		global_light.m_cast_shadow    = light.m_cast_shadow;
		global_light.m_cam_relative   = light.m_cam_relative != 0;
		global_light.m_on             = light.m_on != 0;
		window->GlobalLight(global_light);
	}
	CatchAndReport(View3D_LightPropertiesSet, window,);
}

// Set the global light source for a window
VIEW3D_API void __stdcall View3D_LightSource(view3d::Window window, view3d::Vec4 position, view3d::Vec4 direction, BOOL camera_relative)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");
		assert(position.w == 1);

		DllLockGuard;
		auto global_light = window->GlobalLight();
		global_light.m_position = To<v4>(position);
		global_light.m_direction = To<v4>(direction);
		global_light.m_cam_relative = camera_relative != 0;
		window->GlobalLight(global_light);
	}
	CatchAndReport(View3D_LightSource, window,);
}

// Objects ********************************

// Create objects given in an ldr string or file.
// If multiple objects are created, the handle returned is to the first object only.
// 'ldr_script' - an ldr string, or filepath to a file containing ldr script
// 'file' - TRUE if 'ldr_script' is a filepath, FALSE if 'ldr_script' is a string containing ldr script
// 'context_id' - the context id to create the LdrObjects with
// 'includes' - information used to resolve include directives in 'ldr_script'
VIEW3D_API view3d::Object __stdcall View3D_ObjectCreateLdrW(wchar_t const* ldr_script, BOOL file, GUID const* context_id, view3d::Includes const* includes)
{
	try
	{
		DllLockGuard;
		auto is_file = file != 0;
		auto enc = is_file ? EEncoding::auto_detect : EEncoding::utf16_le;
		return Dll().ObjectCreateLdr<wchar_t>(ldr_script, is_file, enc, context_id, includes);
	}
	CatchAndReport(View3D_ObjectCreateLdr, , nullptr);
}
VIEW3D_API view3d::Object __stdcall View3D_ObjectCreateLdrA(char const* ldr_script, BOOL file, GUID const* context_id, view3d::Includes const* includes)
{
	try
	{
		DllLockGuard;
		auto is_file = file != 0;
		auto enc = is_file ? EEncoding::auto_detect : EEncoding::utf8;
		return Dll().ObjectCreateLdr<char>(ldr_script, is_file, enc, context_id, includes);
	}
	CatchAndReport(View3D_ObjectCreateLdr, , nullptr);
}

// Delete an object, freeing its resources
VIEW3D_API void __stdcall View3D_ObjectDelete(view3d::Object object)
{
	try
	{
		// Delete is idempotent
		if (!object) return;
		
		DllLockGuard;
		Dll().DeleteObject(object);
	}
	CatchAndReport(View3D_ObjectDelete, ,);
}

// Get/Set the object to world transform for this object or the first child object that matches 'name'.
// Note, setting the o2w for a child object positions the object in world space rather than parent space
// (internally the appropriate O2P transform is calculated to put the object at the given O2W location)
VIEW3D_API view3d::Mat4x4 __stdcall View3D_ObjectO2WGet(view3d::Object object, char const* name)
{
	try
	{
		if (!object) throw std::runtime_error("object is null");

		DllLockGuard;
		return To<view3d::Mat4x4>(object->O2W(name));
	}
	CatchAndReport(View3D_ObjectO2WGet, , To<view3d::Mat4x4>(m4x4::Identity()));
}
VIEW3D_API void __stdcall View3D_ObjectO2WSet(view3d::Object object, view3d::Mat4x4 const& o2w, char const* name)
{
	try
	{
		if (object == nullptr) throw std::runtime_error("Object is null");
		
		auto o2w_ = To<m4x4>(o2w);
		if (!IsAffine(o2w_))
			throw std::runtime_error("invalid object to world transform");

		DllLockGuard;
		object->O2W(o2w_, name);
	}
	CatchAndReport(View3D_ObjectO2WSet, ,);
}

// Get/Set the reflectivity of an object (the first object to match 'name') (See LdrObject::Apply)
VIEW3D_API float __stdcall View3D_ObjectReflectivityGet(view3d::Object object, char const* name)
{
	try
	{
		if (!object) throw std::runtime_error("Object is null");

		DllLockGuard;
		return object->Reflectivity(name);
	}
	CatchAndReport(View3D_ObjectReflectivityGet, ,0);
}
VIEW3D_API void __stdcall View3D_ObjectReflectivitySet(view3d::Object object, float reflectivity, char const* name)
{
	try
	{
		if (!object) throw std::runtime_error("Object is null");

		DllLockGuard;
		object->Reflectivity(reflectivity, name);
	}
	CatchAndReport(View3D_ObjectReflectivitySet, ,);
}

// Materials ******************************

// Release a reference to a texture
VIEW3D_API void __stdcall View3D_TextureRelease(view3d::Texture tex)
{
	try
	{
		// Release is idempotent
		if (!tex) return;
		tex->Release();
	}
	CatchAndReport(View3D_TextureRelease, ,);
}
VIEW3D_API void __stdcall View3D_CubeMapRelease(view3d::CubeMap tex)
{
	try
	{
		// Release is idempotent
		if (!tex) return;
		tex->Release();
	}
	CatchAndReport(View3D_CubeMapRelease, ,);
}

// Load a cube map from file, embedded resource, or stock assets. Specify width == 0, height == 0 to use the dimensions of the file
VIEW3D_API view3d::CubeMap __stdcall View3D_CubeMapCreateFromUri(char const* resource, view3d::CubeMapOptions const& options)
{
	try
	{
		DllLockGuard;
		auto tdesc = TextureDesc(rdr12::AutoId, ResDesc::TexCube({}));
		auto tex = Dll().m_rdr.res().CreateTextureCube(resource, tdesc);

		// Set the cube map to world transform
		if (m4x4 cube2w; (cube2w = To<m4x4>(options.m_cube2w)) != m4x4::Zero())
		{
			if (!IsAffine(cube2w)) throw std::runtime_error("Invalid cube map orientation transform");
			tex->m_cube2w = cube2w;
		}

		// Rely on the caller for correct reference counting
		return tex.release();
	}
	CatchAndReport(View3D_CubeMapCreateFromUri, , nullptr);
}

// Miscellaneous **************************

// Create/Delete the demo scene in the given window
VIEW3D_API GUID __stdcall View3D_DemoSceneCreate(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		// Get the string of all ldr objects
		auto scene = rdr12::CreateDemoScene();

		DllLockGuard;

		// Add the demo objects to the sources
		Dll().LoadScript(std::string_view(scene), false, EEncoding::utf8, &Context::GuidDemoSceneObjects, script::Includes(), [=](Guid const& id, bool before)
		{
			if (before)
				window->Remove(&id, 1, 0);
			else
				window->Add(&id, 1, 0);
		});

		// Position the camera to look at the scene
		//todo View3D_ResetView(window, View3DV4{0.0f, 0.0f, -1.0f, 0.0f}, View3DV4{0.0f, 1.0f, 0.0f, 0.0f}, 0, TRUE, TRUE);
		return Context::GuidDemoSceneObjects;
	}
	CatchAndReport(View3D_DemoSceneCreate, window, Context::GuidDemoSceneObjects);
}
VIEW3D_API void __stdcall View3D_DemoSceneDelete()
{
	try
	{
		DllLockGuard;
		Dll().DeleteAllObjectsById(&Context::GuidDemoSceneObjects, 1, 0);
	}
	CatchAndReport(View3D_DemoSceneDelete,,);
}
