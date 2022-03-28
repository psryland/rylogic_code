//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
module;

#include "src/dll/forward.h"
#include "pr/view3d-12/view3d.h"

export module View3d.dll;
import :Forward;
import :Context;
import :Window;

// The view3d dll is loaded once per application, although an application may have
// multiple windows and may call Initialise/Shutdown a number of times.
// Ldr object can be created independently to windows. This means we need one global
// context within the dll, one renderer, and one list of objects.

// Error/Log handling:
//  Each window represents a separate context from the callers point of view, this
//  means we need an error handler per window. Also, within a window, callers may
//  want to temporarily push a different error handler. Each window maintains a
//  stack of error handlers.

using namespace pr;
using namespace pr::rdr12;
using namespace pr::log;
using namespace pr::view3d;

#ifdef _MANAGED
#pragma managed(push, off)
#endif
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
#ifdef _MANAGED
#pragma managed(pop)
#endif

static pr::view3d::Context* g_ctx = nullptr;
static pr::view3d::Context& Dll()
{
	if (g_ctx) return *g_ctx;
	throw std::runtime_error("View3d not initialised");
}

#define DllLockGuard LockGuard lock(Dll().m_mutex)
#define CatchAndReport(func_name, wnd, ret)\
	catch (std::exception const& ex) { Dll().ReportAPIError(#func_name, View3DWindow(wnd), &ex); }\
	catch (...)                      { Dll().ReportAPIError(#func_name, View3DWindow(wnd), nullptr); }\
	return ret

// Initialise the dll
// Initialise calls are reference counted and must be matched with Shutdown calls
// 'initialise_error_cb' is used to report dll initialisation errors only (i.e. it isn't stored)
// Note: this function is not thread safe, avoid race calls
VIEW3D_API View3DContext __stdcall View3D_Initialise(View3D_ReportErrorCB global_error_cb, void* ctx, UINT debug_flags)
{
	auto error_cb = pr::StaticCallBack(global_error_cb, ctx);
	try
	{
		// Create the dll context on the first call
		if (g_ctx == nullptr)
			g_ctx = new Context(g_hInstance, error_cb, static_cast<EDebugLayer>(debug_flags));

		// Generate a unique handle per Initialise call, used to match up with Shutdown calls
		static View3DContext context = nullptr;
		g_ctx->m_inits.insert(++context);
		return context;
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
VIEW3D_API void __stdcall View3D_Shutdown(View3DContext context)
{
	if (!g_ctx) return;

	g_ctx->m_inits.erase(context);
	if (!g_ctx->m_inits.empty())
		return;

	delete g_ctx;
	g_ctx = nullptr;
}

