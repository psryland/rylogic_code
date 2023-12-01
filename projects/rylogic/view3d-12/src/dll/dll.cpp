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
	try
	{
		// Create the dll context on the first call
		if (g_ctx == nullptr)
			g_ctx = new Context(g_hInstance, { global_error_cb, ctx });

		// Generate a unique handle per Initialise call, used to match up with Shutdown calls
		static DllHandle handles = nullptr;
		g_ctx->m_inits.insert(++handles);
		return handles;
	}
	catch (std::exception const& e)
	{
		global_error_cb(ctx, FmtS("Failed to initialise View3D.\nReason: %s\n", e.what()), "", 0, 0);
		return nullptr;
	}
	catch (...)
	{
		global_error_cb(ctx, "Failed to initialise View3D.\nReason: An unknown exception occurred\n", "", 0, 0);
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
			Dll().ReportError += {error_cb, ctx};
		else
			Dll().ReportError -= {error_cb, ctx};
	}
	CatchAndReport(View3D_GlobalErrorCBSet, , );
}

// Set the callback for progress events when script sources are loaded or updated
VIEW3D_API void __stdcall View3D_AddFileProgressCBSet(view3d::AddFileProgressCB progress_cb, void* ctx, BOOL add)
{
	try
	{
		DllLockGuard;
		if (add)
			Dll().OnAddFileProgress += {progress_cb, ctx};
		else
			Dll().OnAddFileProgress -= {progress_cb, ctx};
	}
	CatchAndReport(View3D_AddFileProgressCBSet,,);
}

// Set the callback that is called when the sources are reloaded
VIEW3D_API void __stdcall View3D_SourcesChangedCBSet(view3d::SourcesChangedCB sources_changed_cb, void* ctx, BOOL add)
{
	try
	{
		DllLockGuard;
		if (add)
			Dll().OnSourcesChanged += {sources_changed_cb, ctx};
		else
			Dll().OnSourcesChanged -= {sources_changed_cb, ctx};
	}
	CatchAndReport(View3D_SourcesChangedCBSet,,);
}

// Add/Remove a callback for handling embedded code within scripts
VIEW3D_API void __stdcall View3D_EmbeddedCodeCBSet(char const* lang, view3d::EmbeddedCodeHandlerCB embedded_code_cb, void* ctx, BOOL add)
{
	try
	{
		DllLockGuard;
		Dll().SetEmbeddedCodeHandler(lang, { embedded_code_cb, ctx }, add != 0);
	}
	CatchAndReport(View3D_EmbeddedCodeCBSet, , );
}

// Return the context id for objects created from 'filepath' (if filepath is an existing source)
VIEW3D_API BOOL __stdcall View3D_ContextIdFromFilepath(char const* filepath, GUID& id)
{
	try
	{
		DllLockGuard;
		auto guid = Dll().ContextIdFromFilepath(filepath);
		id = guid ? *guid : GuidZero;
		return guid != nullptr;
	}
	CatchAndReport(View3D_ContextIdFromFilepath,,FALSE);
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
		ScriptSources::OnAddCB on_add = [=](Guid const& id, bool before) { on_add_cb(ctx, id, before); };
		return Dll().LoadScript(std::string_view(ldr_script), false, EEncoding::utf8, context_id, GetIncludes(includes), on_add_cb ? on_add : (ScriptSources::OnAddCB)nullptr);
	}
	CatchAndReport(View3D_LoadScriptFromString, (view3d::Window)nullptr, GuidZero);
}
VIEW3D_API GUID __stdcall View3D_LoadScriptFromFile(char const* ldr_file, GUID const* context_id, view3d::Includes const* includes, view3d::OnAddCB on_add_cb, void* ctx)
{
	try
	{
		// Concurrent entry is allowed
		ScriptSources::OnAddCB on_add = [=](Guid const& id, bool before) { on_add_cb(ctx, id, before); };
		return Dll().LoadScript(std::string_view(ldr_file), true, EEncoding::auto_detect, context_id, GetIncludes(includes), on_add_cb ? on_add : (ScriptSources::OnAddCB)nullptr);
	}
	CatchAndReport(View3D_LoadScriptFromFile, (view3d::Window)nullptr, GuidZero);
}

// Enumerate the Guids of objects in the sources collection
VIEW3D_API void __stdcall View3D_SourceEnumGuids(view3d::EnumGuidsCB enum_guids_cb, void* ctx)
{
	try
	{
		DllLockGuard;
		Dll().SourceEnumGuids({ enum_guids_cb, ctx });
	}
	CatchAndReport(View3D_SourceEnumGuids,, );
}

// Reload script sources. This will delete all objects associated with the script sources then reload the files creating new objects with the same context ids.
VIEW3D_API void __stdcall View3D_ReloadScriptSources()
{
	try
	{
		DllLockGuard;
		return Dll().ReloadScriptSources();
	}
	CatchAndReport(View3D_ReloadScriptSources,,);
}

// Delete all objects and object sources
VIEW3D_API void __stdcall View3D_DeleteAllObjects()
{
	try
	{
		DllLockGuard;
		Dll().DeleteAllObjects();
	}
	CatchAndReport(View3D_DeleteAllObjects, ,);
}

// Delete all objects matching (or not matching) a context id
VIEW3D_API void __stdcall View3D_DeleteById(GUID const* context_ids, int include_count, int exclude_count)
{
	try
	{
		DllLockGuard;
		Dll().DeleteAllObjectsById(context_ids, include_count, exclude_count);
	}
	CatchAndReport(View3D_DeleteById, ,);
}

// Delete all objects not displayed in any windows
VIEW3D_API void __stdcall View3D_DeleteUnused(GUID const* context_ids, int include_count, int exclude_count)
{
	try
	{
		DllLockGuard;
		Dll().DeleteUnused(context_ids, include_count, exclude_count);
	}
	CatchAndReport(View3D_DeleteUnused, ,);
}

// Poll for changed script sources and reload any that have changed
VIEW3D_API void __stdcall View3D_CheckForChangedSources()
{
	try
	{
		DllLockGuard;
		return Dll().CheckForChangedSources();
	}
	CatchAndReport(View3D_CheckForChangedSources,,);
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
			window->ReportError += {error_cb, ctx};
		else
			window->ReportError -= {error_cb, ctx};
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
			window->OnSettingsChanged += {settings_changed_cb, ctx};
		else
			window->OnSettingsChanged -= {settings_changed_cb, ctx};
	}
	CatchAndReport(View3D_WindowSettingsChangedCB, window,);
}

// Add/Remove a callback that is called when the collection of objects associated with 'window' changes
VIEW3D_API void __stdcall View3D_WindowSceneChangedCB(view3d::Window window, view3d::SceneChangedCB scene_changed_cb, void* ctx, BOOL add)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		if (add)
			window->OnSceneChanged += {scene_changed_cb, ctx};
		else
			window->OnSceneChanged -= {scene_changed_cb, ctx};
	}
	CatchAndReport(View3D_WindowSceneChangedCB, window, );
}

// Add/Remove a callback that is called just prior to rendering the window
VIEW3D_API void __stdcall View3D_WindowRenderingCB(view3d::Window window, view3d::RenderingCB rendering_cb, void* ctx, BOOL add)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		if (add)
			window->OnRendering += {rendering_cb, ctx};
		else
			window->OnRendering -= {rendering_cb, ctx};
	}
	CatchAndReport(View3D_WindowRenderingCB, window,);
}

// Add/Remove an object to/from a window
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
VIEW3D_API void __stdcall View3D_WindowRemoveObject(view3d::Window window, view3d::Object object)
{
	try
	{
		if (!object) return;
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->Remove(object);
	}
	CatchAndReport(View3D_WindowRemoveObject, window,);
}

// Add/Remove an gizmo to/from a window
VIEW3D_API void __stdcall View3D_WindowAddGizmo(view3d::Window window, view3d::Gizmo gizmo)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");
		if (!gizmo) throw std::runtime_error("gizmo is null");
		
		DllLockGuard;
		window->Add(gizmo);
	}
	CatchAndReport(View3D_WindowAddGizmo, window,);
}
VIEW3D_API void __stdcall View3D_WindowRemoveGizmo(view3d::Window window, view3d::Gizmo gizmo)
{
	try
	{
		if (!gizmo) return;
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->Remove(gizmo);
	}
	CatchAndReport(View3D_WindowRemoveGizmo, window,);
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

// Remove all objects 'window'
VIEW3D_API void __stdcall View3D_WindowRemoveAllObjects(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->RemoveAllObjects();
	}
	CatchAndReport(View3D_WindowRemoveAllObjects, window,);
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

// Return true if 'object' is among 'window's objects
VIEW3D_API BOOL __stdcall View3D_WindowHasObject(view3d::Window window, view3d::Object object, BOOL search_children)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->Has(object, search_children != 0);
	}
	CatchAndReport(View3D_WindowHasObject, window, false);
}

// Return the number of objects assigned to 'window'
VIEW3D_API int __stdcall View3D_WindowObjectCount(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->ObjectCount();
	}
	CatchAndReport(View3D_WindowObjectCount, window, 0);
}

// Return the bounds of a scene
VIEW3D_API view3d::BBox __stdcall View3D_WindowSceneBounds(view3d::Window window, view3d::ESceneBounds bounds, int except_count, GUID const* except)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return To<view3d::BBox>(window->SceneBounds(bounds, except_count, except));
	}
	CatchAndReport(View3D_WindowSceneBounds, window, To<view3d::BBox>(pr::BBox::Unit()));
}

// Clear the 'invalidated' state of the window.
VIEW3D_API void __stdcall View3D_Validate(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->Validate();
	}
	CatchAndReport(View3D_Validate, window, );
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

// Register a callback for when the window is invalidated. This can be used to render in response to invalidation, rather than rendering on a polling cycle.
VIEW3D_API void __stdcall View3D_WindowInvalidatedCB(view3d::Window window, view3d::InvalidatedCB invalidated_cb, void* ctx, BOOL add)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		if (add)
			window->OnInvalidated += {invalidated_cb, ctx};
		else
			window->OnInvalidated -= {invalidated_cb, ctx};
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

// Get/Set the fill mode for the window
VIEW3D_API view3d::EFillMode __stdcall View3D_WindowFillModeGet(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return static_cast<view3d::EFillMode>(window->FillMode());
	}
	CatchAndReport(View3D_WindowFillModeGet, window, {});
}
VIEW3D_API void __stdcall View3D_WindowFillModeSet(view3d::Window window, view3d::EFillMode mode)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->FillMode(static_cast<pr::rdr12::EFillMode>(mode));
	}
	CatchAndReport(View3D_WindowFillModeSet, window,);
}

// Get/Set the cull mode for a faces in window
VIEW3D_API view3d::ECullMode __stdcall View3D_WindowCullModeGet(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return static_cast<view3d::ECullMode>(window->CullMode());
	}
	CatchAndReport(View3D_CullModeGet, window, {});
}
VIEW3D_API void __stdcall View3D_WindowCullModeSet(view3d::Window window, view3d::ECullMode mode)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->CullMode(static_cast<pr::rdr12::ECullMode>(mode));
	}
	CatchAndReport(View3D_CullModeSet, window,);
}

// Get/Set the multi-sampling mode for a window
VIEW3D_API int  __stdcall View3D_MultiSamplingGet(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->MultiSampling();
	}
	CatchAndReport(View3D_MultiSamplingGet, window, {});
}
VIEW3D_API void __stdcall View3D_MultiSamplingSet(view3d::Window window, int multisampling)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->MultiSampling(multisampling);
	}
	CatchAndReport(View3D_MultiSamplingSet, window, );
}

// Control animation
VIEW3D_API void __stdcall View3D_WindowAnimControl(view3d::Window window, view3d::EAnimCommand command, double time_s)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->AnimControl(command, seconds_t(time_s));
	}
	CatchAndReport(View3D_WindowAnimControl, window, );
}

// Set the callback for animation events
VIEW3D_API void __stdcall View3D_WindowAnimEventCBSet(view3d::Window window, view3d::AnimationCB anim_cb, void* ctx, BOOL add)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		if (add)
			window->OnAnimationEvent += {anim_cb, ctx};
		else
			window->OnAnimationEvent -= {anim_cb, ctx};
	}
	CatchAndReport(View3D_AnimationEventCBSet, , );
}

// Get/Set the animation time
VIEW3D_API BOOL __stdcall View3D_WindowAnimating(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->Animating();
	}
	CatchAndReport(View3D_WindowAnimating, window, FALSE);
}
VIEW3D_API double __stdcall View3D_WindowAnimTimeGet(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->AnimTime().count();
	}
	CatchAndReport(View3D_WindowAnimTimeGet, window, 0.0f);
}
VIEW3D_API void __stdcall View3D_WindowAnimTimeSet(view3d::Window window, double time_s)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->AnimTime(seconds_t(time_s));
	}
	CatchAndReport(View3D_WindowAnimTimeSet, window, );
}

// Return the DPI of the monitor that 'window' is displayed on
VIEW3D_API view3d::Vec2 __stdcall View3D_WindowDpiScale(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return To<view3d::Vec2>(DIPtoPhysical(v2::One(), window->Dpi()));
	}
	CatchAndReport(View3d_WindowDPI, window, {});
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

// Cast a ray into the scene, returning information about what it hit.
// 'rays' - is an input buffer of rays to cast for hit testing
// 'hits' - are the nearest intercepts with the given rays
// 'ray_count' - is the length of the 'rays' array
// 'snap_distance' - the world space distance to snap to
// 'flags' - what can be hit.
// 'objects' - An array of objects to hit test
// 'object_count' - The length of the 'objects' array.
VIEW3D_API void __stdcall View3D_WindowHitTestObjects(view3d::Window window, view3d::HitTestRay const* rays, view3d::HitTestResult* hits, int ray_count, float snap_distance, view3d::EHitTestFlags flags, view3d::Object const* objects, int object_count)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		// todo: add the non-immediate version of this function
		// to allow continuous hit-testing during constant rendering.

		DllLockGuard;
		window->HitTest({ rays, s_cast<size_t>(ray_count) }, { hits, s_cast<size_t>(ray_count) }, snap_distance, flags, objects, object_count);
	}
	CatchAndReport(View3D_WindowHitTestObjects, window, );
}

// Cast a ray into the scene, returning information about what it hit
// 'rays' - is an input buffer of rays to cast for hit testing
// 'hits' - are the nearest intercepts with the given rays
// 'ray_count' - is the length of the 'rays' array
// 'snap_distance' - the world space distance to snap to
// 'flags' - what can be hit.
// 'context_ids' - context ids for objects to include/exclude from hit testing
// 'include_count' - the number of context ids that should be included
// 'exclude_count' - the number of context ids that should be excluded
// 'include_count+exclude_count' = the length of the 'context_ids' array. If 0, then all context ids are included for hit testing
VIEW3D_API void __stdcall View3D_WindowHitTestByCtx(view3d::Window window, view3d::HitTestRay const* rays, view3d::HitTestResult* hits, int ray_count, float snap_distance, view3d::EHitTestFlags flags, GUID const* context_ids, int include_count, int exclude_count)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		// todo: add the non-immediate version of this function
		// to allow continuous hit-testing during constant rendering.

		DllLockGuard;
		window->HitTest({ rays, s_cast<size_t>(ray_count) }, { hits, s_cast<size_t>(ray_count) }, snap_distance, flags, context_ids, include_count, exclude_count);
	}
	CatchAndReport(View3D_WindowHitTestByCtx, window, );
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

// Move the camera to a position that can see the whole scene. Set 'dist' to 0 to preserve the FoV, or a distance to set the FoV
VIEW3D_API void __stdcall View3D_ResetView(view3d::Window window, view3d::Vec4 forward, view3d::Vec4 up, float dist, BOOL preserve_aspect, BOOL commit)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->ResetView(To<v4>(forward), To<v4>(up), dist, preserve_aspect != 0, commit != 0);
	}
	CatchAndReport(View3D_ResetView, window,);
}

// Reset the camera to view a bbox. Set 'dist' to 0 to preserve the FoV, or a distance to set the FoV
VIEW3D_API void __stdcall View3D_ResetViewBBox(view3d::Window window, view3d::BBox bbox, view3d::Vec4 forward, view3d::Vec4 up, float dist, BOOL preserve_aspect, BOOL commit)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->ResetView(To<pr::BBox>(bbox), To<v4>(forward), To<v4>(up), dist, preserve_aspect != 0, commit != 0);
	}
	CatchAndReport(View3D_ResetViewBBox, window,);
}

// Enable/Disable orthographic projection
VIEW3D_API BOOL __stdcall View3D_CameraOrthographicGet(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->Orthographic();
	}
	CatchAndReport(View3D_CameraOrthographicGet, window, FALSE);
}
VIEW3D_API void __stdcall View3D_CameraOrthographicSet(view3d::Window window, BOOL on)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->Orthographic(on != 0);
	}
	CatchAndReport(View3D_CameraOrthographicSet, window,);
}

// Get/Set the distance to the camera focus point
VIEW3D_API float __stdcall View3D_CameraFocusDistanceGet(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->FocusDistance();
	}
	CatchAndReport(View3D_CameraFocusDistanceGet, window, 0.0f);
}
VIEW3D_API void __stdcall View3D_CameraFocusDistanceSet(view3d::Window window, float dist)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->FocusDistance(dist);
	}
	CatchAndReport(View3D_CameraFocusDistanceSet, window,);
}

// Get/Set the camera focus point position
VIEW3D_API view3d::Vec4 __stdcall View3D_CameraFocusPointGet(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return To<view3d::Vec4>(window->FocusPoint());
	}
	CatchAndReport(View3D_CameraFocusPointGet, window, {});
}
VIEW3D_API void __stdcall View3D_CameraFocusPointSet(view3d::Window window, view3d::Vec4 position)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->FocusPoint(To<v4>(position));
	}
	CatchAndReport(View3D_CameraFocusPointSet, window,);
}

// Get/Set the aspect ratio for the camera field of view
VIEW3D_API float __stdcall View3D_CameraAspectGet(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->Aspect();
	}
	CatchAndReport(View3D_CameraAspectGet, window, {});
}
VIEW3D_API void __stdcall View3D_CameraAspectSet(view3d::Window window, float aspect)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->Aspect(aspect);
	}
	CatchAndReport(View3D_CameraAspectSet, window,);
}

// Get/Set both the X and Y fields of view (i.e. set the aspect ratio). Null fov means don't change the current value.
VIEW3D_API view3d::Vec2 __stdcall View3D_CameraFovGet(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return To<view3d::Vec2>(window->Fov());
	}
	CatchAndReport(View3D_CameraFovSet, window, {});
}
VIEW3D_API void __stdcall View3D_CameraFovSet(view3d::Window window, float* fovX, float* fovY)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->Fov(fovX, fovY);
	}
	CatchAndReport(View3D_CameraFovSet, window,);
}

// Adjust the FocusDist, FovX, and FovY so that the average FOV equals 'fov'
VIEW3D_API void __stdcall View3D_CameraBalanceFov(view3d::Window window, float fov)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->BalanceFov(fov);
	}
	CatchAndReport(View3D_CameraBalanceFov, window,);
}

// Get/Set (using fov and focus distance) the size of the perpendicular area visible to the camera at 'dist' (in world space). Use 'focus_dist != 0' to set a specific focus distance
VIEW3D_API view3d::Vec2 __stdcall View3D_CameraViewRectAtDistanceGet(view3d::Window window, float dist)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return To<view3d::Vec2>(window->ViewRectAtDistance(dist));
	}
	CatchAndReport(View3D_ViewArea, window, {});
}
VIEW3D_API void __stdcall View3D_CameraViewRectAtDistanceSet(view3d::Window window, view3d::Vec2 rect, float focus_dist)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->ViewRectAtDistance(To<v2>(rect), focus_dist);
	}
	CatchAndReport(View3D_CameraViewRectSet, window,);
}

// Get/Set the near and far clip planes for the camera
VIEW3D_API view3d::Vec2 __stdcall View3D_CameraClipPlanesGet(view3d::Window window, BOOL focus_relative)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return To<view3d::Vec2>(window->ClipPlanes(focus_relative != 0));
	}
	CatchAndReport(View3D_CameraClipPlanesGet, window, {});
}
VIEW3D_API void __stdcall View3D_CameraClipPlanesSet(view3d::Window window, float* near_, float* far_, BOOL focus_relative)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->ClipPlanes(near_, far_, focus_relative != 0);
	}
	CatchAndReport(View3D_CameraClipPlanesSet, window,);
}

// Get/Set the scene camera lock mask
VIEW3D_API view3d::ECameraLockMask __stdcall View3D_CameraLockMaskGet(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return s_cast<view3d::ECameraLockMask>(window->LockMask());
	}
	CatchAndReport(View3D_CameraLockMaskGet, window, {});
}
VIEW3D_API void __stdcall View3D_CameraLockMaskSet(view3d::Window window, view3d::ECameraLockMask mask)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->LockMask(s_cast<camera::ELockMask>(mask));
	}
	CatchAndReport(View3D_CameraLockMaskSet, window,);
}

// Get/Set the camera align axis
VIEW3D_API view3d::Vec4 __stdcall View3D_CameraAlignAxisGet(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return To<view3d::Vec4>(window->AlignAxis());
	}
	CatchAndReport(View3D_CameraAlignAxisGet, window, {});
}
VIEW3D_API void __stdcall View3D_CameraAlignAxisSet(view3d::Window window, view3d::Vec4 axis)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->AlignAxis(To<v4>(axis));
	}
	CatchAndReport(View3D_CameraAlignAxisSet, window,);
}

// Reset to the default zoom
VIEW3D_API void __stdcall View3D_CameraResetZoom(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->ResetZoom();
	}
	CatchAndReport(View3D_CameraResetZoom, window,);
}

// Get/Set the FOV zoom
VIEW3D_API float __stdcall View3D_CameraZoomGet(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->Zoom();
	}
	CatchAndReport(View3D_CameraZoomGet, window, {});
}
VIEW3D_API void __stdcall View3D_CameraZoomSet(view3d::Window window, float zoom)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->Zoom(zoom);
	}
	CatchAndReport(View3D_CameraZoomSet, window,);
}

// Commit the current O2W position as the reference position
VIEW3D_API void __stdcall View3D_CameraCommit(view3d::Window window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_scene.m_cam.Commit();
	}
	CatchAndReport(View3D_CameraCommit, window,);
}

// Navigation *****************************

// Direct movement of the camera
VIEW3D_API BOOL __stdcall View3D_Navigate(view3d::Window window, float dx, float dy, float dz)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->m_scene.m_cam.Translate(dx, dy, dz);
	}
	CatchAndReport(View3D_Navigate, window, FALSE);
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

// Convert an MK_ macro to a default navigation operation
VIEW3D_API view3d::ENavOp __stdcall View3D_MouseBtnToNavOp(int mk)
{
	return static_cast<view3d::ENavOp>(camera::MouseBtnToNavOp(mk));
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

// Create an object from provided buffers
VIEW3D_API view3d::Object __stdcall View3D_ObjectCreate(char const* name, view3d::Colour colour, int vcount, int icount, int ncount, view3d::Vertex const* verts, UINT16 const* indices, view3d::Nugget const* nuggets, GUID const& context_id)
{
	try
	{
		DllLockGuard;
		return Dll().ObjectCreate(name, colour, { verts, s_cast<size_t>(vcount) }, { indices, s_cast<size_t>(icount) }, { nuggets, s_cast<size_t>(ncount) }, context_id);
	}
	CatchAndReport(View3D_ObjectCreate, , nullptr);
}

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

// Load a p3d model file as a view3d object
VIEW3D_API view3d::Object __stdcall View3D_ObjectCreateP3DFile(char const* name, view3d::Colour colour, char const* p3d_filepath, GUID const* context_id)
{
	try
	{
		DllLockGuard;
		return Dll().ObjectCreateP3D(name, colour, p3d_filepath, context_id);
	}
	CatchAndReport(View3D_ObjectCreateP3D, , {});
}

// Load a p3d model in memory as a view3d object
VIEW3D_API view3d::Object __stdcall View3D_ObjectCreateP3DStream(char const* name, view3d::Colour colour, size_t size, void const* p3d_data, GUID const* context_id)
{
	try
	{
		DllLockGuard;
		return Dll().ObjectCreateP3D(name, colour, size, p3d_data, context_id);
	}
	CatchAndReport(View3D_ObjectCreateP3D, , {});
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
