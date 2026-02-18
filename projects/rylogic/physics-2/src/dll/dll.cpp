//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
// The physics-2 DLL is a thin wrapper around the static library,
// providing a C-linkage API for use from other languages or as
// a dynamically loaded library.
#include "pr/physics-2/physics-dll.h"

// @Copilot, move these to a dll_forward.h header (like view3d-12 has)
#include <unordered_set>
#include <mutex>
#include <format>

using namespace pr;
using namespace pr::physics;

// DLL entry point
HINSTANCE g_hInstance;
BOOL APIENTRY DllMain(HMODULE hInstance, DWORD reason, LPVOID)
{
	switch (reason)
	{
		case DLL_PROCESS_ATTACH: g_hInstance = hInstance; break;
		case DLL_PROCESS_DETACH: g_hInstance = nullptr; break;
		case DLL_THREAD_ATTACH:  break;
		case DLL_THREAD_DETACH:  break;
	}
	return TRUE;
}

// Global DLL context
struct Context// @Copilot, move this to dll/context.cpp/.h files (like view3d-12 has)
{
	using InitSet = std::unordered_set<DllHandle>;

	InitSet              m_inits;
	std::recursive_mutex m_mutex;
	ReportErrorCB        m_error_cb;

	Context(ReportErrorCB error_cb)
		: m_inits()
		, m_mutex()
		, m_error_cb(error_cb)
	{}
};
static Context* g_ctx = nullptr;

// Initialise calls are reference counted and must be matched with Shutdown calls.
PHYSICS_API DllHandle __stdcall Physics_Initialise(ReportErrorCB global_error_cb)
{
	try
	{
		// Create the dll context on the first call
		if (g_ctx == nullptr)
			g_ctx = new Context(global_error_cb);

		// Generate a unique handle per Initialise call, used to match up with Shutdown calls
		static DllHandle handles = nullptr;
		g_ctx->m_inits.insert(++handles);
		return handles;
	}
	catch (std::exception const& e)
	{
		global_error_cb(std::format("Failed to initialise Physics.\nReason: %s\n", e.what()), "", 0, 0);
		return nullptr;
	}
	catch (...)
	{
		global_error_cb("Failed to initialise Physics.\nReason: An unknown exception occurred\n", "", 0, 0);
		return nullptr;
	}
}
PHYSICS_API void __stdcall Physics_Shutdown(DllHandle context)
{
	if (!g_ctx) return;

	g_ctx->m_inits.erase(context);
	if (!g_ctx->m_inits.empty())
		return;

	delete g_ctx;
	g_ctx = nullptr;
}
