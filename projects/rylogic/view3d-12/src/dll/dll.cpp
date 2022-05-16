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
	auto error_cb = StaticCallBack(global_error_cb, ctx);
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
		error_cb(pr::FmtS(L"Failed to initialise View3D.\nReason: %S\n", e.what()), L"", 0, 0);
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
			Dll().ReportError += StaticCallBack(error_cb, ctx);
		else
			Dll().ReportError -= StaticCallBack(error_cb, ctx);
	}
	CatchAndReport(View3D_GlobalErrorCBSet, , );
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
			window->ReportError += StaticCallBack(error_cb, ctx);
		else
			window->ReportError -= StaticCallBack(error_cb, ctx);
	}
	CatchAndReport(View3D_WindowErrorCBSet, window, );
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
			window->OnInvalidated += StaticCallBack(invalidated_cb, ctx);
		else
			window->OnInvalidated -= StaticCallBack(invalidated_cb, ctx);
	}
	CatchAndReport(View3D_WindowInvalidatedCB, window,);
}


// Lights *********************************

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

