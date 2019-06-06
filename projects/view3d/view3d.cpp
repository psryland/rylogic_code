//***************************************************************************************************
// View 3D
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************

#include "view3d/forward.h"
#include "pr/view3d/view3d.h"
#include "pr/view3d/pr_conv.h"
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

static Context* g_ctx = nullptr;
static Context& Dll()
{
	if (g_ctx) return *g_ctx;
	throw std::runtime_error("View3d not initialised");
}

// Default error callback
void __stdcall DefaultErrorCB(void*, wchar_t const* msg)
{
	std::wcerr << msg << std::endl;
}

// Find the error callback to use
pr::MultiCast<ReportErrorCB>& GetErrorCB(View3DWindow wnd)
{
	return (wnd != nullptr && wnd->OnError != nullptr) ? wnd->OnError : Dll().OnError;
}

// Report a basic error message
inline void ReportError(wchar_t const* msg, View3DWindow wnd)
{
	GetErrorCB(wnd).Raise(msg);
}

// Report an error message via the window error callback
inline void ReportError(char const* func_name, View3DWindow wnd, std::exception const* ex)
{
	// Report the error
	auto msg = pr::Fmt<pr::string<wchar_t>>(L"%S failed.\n%S", func_name, ex ? ex->what() : "Unknown exception occurred.");
	if (msg.last() != '\n') msg.push_back('\n');
	GetErrorCB(wnd).Raise(msg.c_str());
}

// Maths type traits
namespace pr
{
	namespace maths
	{
		template <> struct is_vec<View3DV2> :std::true_type
		{
			using elem_type = float;
			using cp_type = float;
			static int const dim = 2;
		};
		template <> struct is_vec<View3DV4> :std::true_type
		{
			using elem_type = float;
			using cp_type = float;
			static int const dim = 4;
		};
		template <> struct is_vec<View3DM4x4> :std::true_type
		{
			using elem_type = View3DV4;
			using cp_type = typename is_vec<View3DV4>::cp_type;
			static int const dim = 4;
		};
	}
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
VIEW3D_API View3DContext __stdcall View3D_Initialise(View3D_ReportErrorCB initialise_error_cb, void* ctx, D3D11_CREATE_DEVICE_FLAG device_flags)
{
	try
	{
		// Create the dll context on the first call
		if (g_ctx == nullptr)
			g_ctx = new Context(g_hInstance, device_flags);

		// Generate a unique handle per Initialise call, used to match up with Shutdown calls
		static View3DContext context = nullptr;
		g_ctx->m_inits.insert(++context);
		return context;
	}
	catch (std::exception const& e)
	{
		if (initialise_error_cb) initialise_error_cb(ctx, pr::FmtS(L"Failed to initialise View3D.\nReason: %S\n", e.what()));
		return nullptr;
	}
	catch (...)
	{
		if (initialise_error_cb) initialise_error_cb(ctx, L"Failed to initialise View3D.\nReason: An unknown exception occurred\n");
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

// Add/Remove a global error callback.
// Note: The callback function can be called in a worker thread context if errors occur during LoadScriptSource
VIEW3D_API void __stdcall View3D_GlobalErrorCBSet(View3D_ReportErrorCB error_cb, void* ctx, BOOL add)
{
	try
	{
		if (add)
			Dll().OnError += pr::StaticCallBack(error_cb, ctx);
		else
			Dll().OnError -= pr::StaticCallBack(error_cb, ctx);
	}
	CatchAndReport(View3D_GlobalErrorCBSet,,);
}

// Enumerate the Guids of objects in the sources collection
VIEW3D_API void __stdcall View3D_SourceEnumGuids(View3D_EnumGuidsCB enum_guids_cb, void* ctx)
{
	try
	{
		DllLockGuard;
		Dll().SourceEnumGuids(enum_guids_cb, ctx);
	}
	CatchAndReport(View3D_SourceEnumGuids,, );
}

// Create an include handler that can load from directories or embedded resources
pr::script::Includes GetIncludes(View3DIncludes const* includes)
{
	using namespace pr::script;

	Includes inc;
	if (includes != nullptr)
	{
		if (includes->m_include_paths != nullptr)
			inc.SearchPaths(pr::script::string(includes->m_include_paths));

		if (includes->m_module_count != 0)
			inc.ResourceModules(std::initializer_list<HMODULE>(includes->m_modules, includes->m_modules + includes->m_module_count));
	}
	return std::move(inc);
}

// Add an Ldr source file. This file will be watched and the object store updated whenever
// it, or any of it's included dependencies change. The returned GUID is the context id for
// all objects added as a result of 'filepath' and its dependencies.
VIEW3D_API GUID __stdcall View3D_LoadScriptSource(wchar_t const* filepath, BOOL additional, View3DIncludes const* includes)
{
	try
	{
		// Concurrent entry is allowed.
		//'DllLockGuard;
		return Dll().LoadScriptSource(filepath, additional != 0, GetIncludes(includes));
	}
	CatchAndReport(View3D_LoadScriptSource, (View3DWindow)nullptr, pr::GuidZero);
}

// Add an ldr script string. This will create all objects declared in 'ldr_script'
// with context id 'context_id' if given, otherwise an id will be created
VIEW3D_API GUID __stdcall View3D_LoadScript(wchar_t const* ldr_script, BOOL file, GUID const* context_id, View3DIncludes const* includes)
{
	try
	{
		DllLockGuard;
		return Dll().LoadScript(ldr_script, file != 0, context_id, GetIncludes(includes));
	}
	CatchAndReport(View3D_LoadScript, (View3DWindow)nullptr, pr::GuidZero);
}

// Reload script sources. This will delete all objects associated with the script sources
// then reload the files creating new objects with the same context ids
VIEW3D_API void __stdcall View3D_ReloadScriptSources()
{
	try
	{
		DllLockGuard;
		return Dll().ReloadScriptSources();
	}
	CatchAndReport(View3D_ReloadScriptSources,,);
}

// Delete all objects
VIEW3D_API void __stdcall View3D_ObjectsDeleteAll()
{
	try
	{
		DllLockGuard;
		Dll().DeleteAllObjects();
	}
	CatchAndReport(View3D_ObjectsDeleteAll, ,);
}

// Delete all objects matching a context id
VIEW3D_API void __stdcall View3D_ObjectsDeleteById(GUID const* context_ids, int include_count, int exclude_count)
{
	try
	{
		DllLockGuard;
		Dll().DeleteAllObjectsById(context_ids, include_count, exclude_count);
	}
	CatchAndReport(View3D_ObjectsDeleteById, ,);
}

// Delete all objects not displayed in any windows
VIEW3D_API void __stdcall View3D_ObjectsDeleteUnused(GUID const* context_ids, int include_count, int exclude_count)
{
	try
	{
		DllLockGuard;
		Dll().DeleteUnused(context_ids, include_count, exclude_count);
	}
	CatchAndReport(View3D_ObjectsDeleteUnused, ,);
}

// Poll for changed script source files, and reload any that have changed
VIEW3D_API void __stdcall View3D_CheckForChangedSources()
{
	try
	{
		DllLockGuard;
		return Dll().CheckForChangedSources();
	}
	CatchAndReport(View3D_CheckForChangedSources,,);
}

// Set the callback for progress events when script sources are loaded or updated
VIEW3D_API void __stdcall View3D_AddFileProgressCBSet(View3D_AddFileProgressCB progress_cb, void* ctx, BOOL add)
{
	try
	{
		DllLockGuard;
		if (add)
			Dll().OnAddFileProgress += pr::StaticCallBack(progress_cb, ctx);
		else
			Dll().OnAddFileProgress -= pr::StaticCallBack(progress_cb, ctx);
	}
	CatchAndReport(View3D_AddFileProgressCBSet,,);
}

// Set the callback called when the sources are reloaded
VIEW3D_API void __stdcall View3D_SourcesChangedCBSet(View3D_SourcesChangedCB sources_changed_cb, void* ctx, BOOL add)
{
	try
	{
		DllLockGuard;
		if (add)
			Dll().OnSourcesChanged += pr::StaticCallBack(sources_changed_cb, ctx);
		else
			Dll().OnSourcesChanged -= pr::StaticCallBack(sources_changed_cb, ctx);
	}
	CatchAndReport(View3D_SourcesChangedCBSet,,);
}

// Add/Remove a callback for handling embedded code within scripts
VIEW3D_API void __stdcall View3D_EmbeddedCodeCBSet(wchar_t const* lang, View3D_EmbeddedCodeHandlerCB embedded_code_cb, void* ctx, BOOL add)
{
	try
	{
		DllLockGuard;
		Dll().SetEmbeddedCodeHandler(lang, embedded_code_cb, ctx, add != 0);
	}
	CatchAndReport(View3D_EmbeddedCodeCBSet, , );
}

// Return the context id for objects created from 'filepath' (if filepath is an existing source)
VIEW3D_API BOOL __stdcall View3D_ContextIdFromFilepath(wchar_t const* filepath, GUID& id)
{
	try
	{
		DllLockGuard;
		auto guid = Dll().ContextIdFromFilepath(filepath);
		if (guid != nullptr) id = *guid;
		return guid != nullptr;
	}
	CatchAndReport(View3D_ContextIdFromFilepath,,FALSE);
}

// Create/Destroy a window
// 'error_cb' must be a valid function pointer for the lifetime of the window
VIEW3D_API View3DWindow __stdcall View3D_WindowCreate(HWND hwnd, View3DWindowOptions const& opts)
{
	try
	{
		DllLockGuard;
		return Dll().WindowCreate(hwnd, opts);
	}
	CatchAndReport(View3D_WindowCreate,, nullptr);
}
VIEW3D_API void __stdcall View3D_WindowDestroy(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		Dll().WindowDestroy(window);
	}
	CatchAndReport(View3D_WindowDestroy,window,);
}

// Add/Remove a window error callback
// Note: The callback function can be called in a worker thread context if errors occur during LoadScriptSource
VIEW3D_API void __stdcall View3D_WindowErrorCBSet(View3DWindow window, View3D_ReportErrorCB error_cb, void* ctx, BOOL add)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");
		if (add)
			window->OnError += pr::StaticCallBack(error_cb, ctx);
		else
			window->OnError -= pr::StaticCallBack(error_cb, ctx);
	}
	CatchAndReport(View3D_WindowErrorCBSet,window,);
}

// Generate/Parse a settings string for the view
VIEW3D_API char const* __stdcall View3D_WindowSettingsGet(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");
		
		std::stringstream out;
		//out << "*SceneSettings {" << window->m_obj_cont_ui.Settings() << "}\n";
		out << "*Light {\n" << window->m_light.Settings() << "}\n";
		
		window->m_settings = out.str();
		return window->m_settings.c_str();
	}
	CatchAndReport(View3D_WindowSettingsGet, window, "");
}
VIEW3D_API void __stdcall View3D_WindowSettingsSet(View3DWindow window, char const* settings)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		// Parse the settings
		pr::script::PtrA src(settings);
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
				window->NotifySettingsChanged(EView3DWindowSettings::Lighting);
				continue;
			}
		}
	}
	CatchAndReport(View3D_WindowSettingsSet, window,);
}

// Add/Remove a callback that is called when settings change
VIEW3D_API void __stdcall View3D_WindowSettingsChangedCB(View3DWindow window, View3D_SettingsChangedCB settings_changed_cb, void* ctx, BOOL add)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");
		if (add)
			window->OnSettingsChanged += pr::StaticCallBack(settings_changed_cb, ctx);
		else
			window->OnSettingsChanged -= pr::StaticCallBack(settings_changed_cb, ctx);
	}
	CatchAndReport(View3D_WindowSettingsChangedCB, window,);
}

// Add/Remove a callback that is called when the window is invalidated
VIEW3D_API void __stdcall View3D_WindowInvalidatedCB(View3DWindow window, View3D_InvalidatedCB invalidated_cb, void* ctx, BOOL add)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");
		if (add)
			window->OnInvalidated += pr::StaticCallBack(invalidated_cb, ctx);
		else
			window->OnInvalidated -= pr::StaticCallBack(invalidated_cb, ctx);
	}
	CatchAndReport(View3D_WindowInvalidatedCB, window,);
}

// Add/Remove a callback that is called just prior to rendering the window
VIEW3D_API void __stdcall View3D_WindowRenderingCB(View3DWindow window, View3D_RenderCB rendering_cb, void* ctx, BOOL add)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");
		if (add)
			window->OnRendering += pr::StaticCallBack(rendering_cb, ctx);
		else
			window->OnRendering -= pr::StaticCallBack(rendering_cb, ctx);
	}
	CatchAndReport(View3D_WindowRenderingCB, window,);
}

// Add/Remove a callback that is called when the collection of objects associated with 'window' changes
VIEW3D_API void __stdcall View3d_WindowSceneChangedCB(View3DWindow window, View3D_SceneChangedCB scene_changed_cb, void* ctx, BOOL add)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");
		if (add)
			window->OnSceneChanged += pr::StaticCallBack(scene_changed_cb, ctx);
		else
			window->OnSceneChanged -= pr::StaticCallBack(scene_changed_cb, ctx);
	}
	CatchAndReport(View3d_WindowSceneChangedCB, window, );
}

// Suspend/Resume the scene changed notification event. Typically used during rendering a frame
VIEW3D_API void __stdcall View3D_WindowSceneChangedSuspend(View3DWindow window, BOOL suspend)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		pr::MultiCast<SceneChangedCB>::Lock lock(window->OnSceneChanged);
		if (suspend != 0)
			lock.suspend();
		else
			lock.resume();
	}
	CatchAndReport(View3D_WindowSceneChangedSuspend, window, );
}

// Add/Remove objects to/from a window
VIEW3D_API void __stdcall View3D_WindowAddObject(View3DWindow window, View3DObject object)
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
VIEW3D_API void __stdcall View3D_WindowRemoveObject(View3DWindow window, View3DObject object)
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
VIEW3D_API void __stdcall View3D_WindowRemoveAllObjects(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->RemoveAllObjects();
	}
	CatchAndReport(View3D_WindowRemoveAllObjects, window,);
}

// Return true if 'object' is among 'window's objects
VIEW3D_API BOOL __stdcall View3D_WindowHasObject(View3DWindow window, View3DObject object, BOOL search_children)
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
VIEW3D_API int __stdcall View3D_WindowObjectCount(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->ObjectCount();
	}
	CatchAndReport(View3D_WindowObjectCount, window, 0);
}

// Enumerate the guids associated with 'window'
VIEW3D_API void __stdcall View3D_WindowEnumGuids(View3DWindow window, View3D_EnumGuidsCB enum_guids_cb, void* ctx)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->EnumGuids(enum_guids_cb, ctx);
	}
	CatchAndReport(View3D_WindowEnumGuids, window, );
}

// Enumerate the objects associated with 'window'
VIEW3D_API void __stdcall View3D_WindowEnumObjects(View3DWindow window, View3D_EnumObjectsCB enum_objects_cb, void* ctx)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->EnumObjects(enum_objects_cb, ctx);
	}
	CatchAndReport(View3D_WindowEnumObjects, window, );
}
VIEW3D_API void __stdcall View3D_WindowEnumObjectsById(View3DWindow window, View3D_EnumObjectsCB enum_objects_cb, void* ctx, GUID const* context_ids, int include_count, int exclude_count)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->EnumObjects(enum_objects_cb, ctx, context_ids, include_count, exclude_count);
	}
	CatchAndReport(View3D_WindowEnumObjectsById, window, );
}

// Add/Remove objects by context id
VIEW3D_API void __stdcall View3D_WindowAddObjectsById(View3DWindow window, GUID const* context_ids, int include_count, int exclude_count)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->AddObjectsById(context_ids, include_count, exclude_count);
	}
	CatchAndReport(View3D_WindowAddObjectsById, window,);
}
VIEW3D_API void __stdcall View3D_WindowRemoveObjectsById(View3DWindow window, GUID const* context_ids, int include_count, int exclude_count)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->RemoveObjectsById(context_ids, include_count, exclude_count, false);
	}
	CatchAndReport(View3D_WindowRemoveObjectsById, window,);
}

// Add/Remove a gizmo from 'window'
VIEW3D_API void __stdcall View3D_WindowAddGizmo(View3DWindow window, View3DGizmo gizmo)
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
VIEW3D_API void __stdcall View3D_WindowRemoveGizmo(View3DWindow window, View3DGizmo gizmo)
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

// Return the bounds of a scene
VIEW3D_API View3DBBox __stdcall View3D_WindowSceneBounds(View3DWindow window, EView3DSceneBounds bounds, int except_count, GUID const* except)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return view3d::To<View3DBBox>(window->SceneBounds(bounds, except_count, except));
	}
	CatchAndReport(View3D_WindowSceneBounds, window, view3d::To<View3DBBox>(pr::BBoxUnit));
}

// Get/Set the animation time
VIEW3D_API float __stdcall View3D_WindowAnimTimeGet(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->m_anim_time_s;
	}
	CatchAndReport(View3D_WindowAnimTimeGet, window, 0.0f);
}
VIEW3D_API void __stdcall View3D_WindowAnimTimeSet(View3DWindow window, float time_s)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_anim_time_s = time_s;
	}
	CatchAndReport(View3D_WindowAnimTimeSet, window, );
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
VIEW3D_API void __stdcall View3D_WindowHitTest(View3DWindow window, View3DHitTestRay const* rays, View3DHitTestResult* hits, int ray_count, float snap_distance, EView3DHitTestFlags flags, GUID const* context_ids, int include_count, int exclude_count)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		// todo: add the non-immediate version of this function
		// to allow continuous hit-testing during constant rendering.

		DllLockGuard;
		window->HitTest(rays, hits, ray_count, snap_distance, flags, context_ids, include_count, exclude_count);
	}
	CatchAndReport(View3D_WindowMouseTrackGet, window, );
}

//  ********************************************************

// Return the camera to world transform
VIEW3D_API void __stdcall View3D_CameraToWorldGet(View3DWindow window, View3DM4x4& c2w)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		c2w = view3d::To<View3DM4x4>(window->m_camera.m_c2w);
	}
	CatchAndReport(View3D_CameraToWorldGet, window,);
}

// Set the camera to world transform
VIEW3D_API void __stdcall View3D_CameraToWorldSet(View3DWindow window, View3DM4x4 const& c2w)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_camera.m_c2w = view3d::To<pr::m4x4>(c2w);
	}
	CatchAndReport(View3D_CameraToWorldSet, window,);
}

// Position the camera for a window
VIEW3D_API void __stdcall View3D_CameraPositionSet(View3DWindow window, View3DV4 position, View3DV4 lookat, View3DV4 up)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_camera.LookAt(view3d::To<pr::v4>(position), view3d::To<pr::v4>(lookat), view3d::To<pr::v4>(up), true);
	}
	CatchAndReport(View3D_CameraPositionSet, window,);
}

// Commit the current O2W position as the reference position
VIEW3D_API void __stdcall View3D_CameraCommit(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_camera.Commit();
	}
	CatchAndReport(View3D_CameraCommit, window,);
}

// Enable/Disable orthographic projection
VIEW3D_API BOOL __stdcall View3D_CameraOrthographic(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->m_camera.m_orthographic;
	}
	CatchAndReport(View3D_CameraOrthographic, window, FALSE);
}
VIEW3D_API void __stdcall View3D_CameraOrthographicSet(View3DWindow window, BOOL on)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_camera.m_orthographic = on != 0;
	}
	CatchAndReport(View3D_CameraOrthographicSet, window,);
}

// Return the distance to the camera focus point
VIEW3D_API float __stdcall View3D_CameraFocusDistance(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

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
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_camera.FocusDist(dist);
	}
	CatchAndReport(View3D_CameraSetFocusDistance, window,);
}

// Set the camera distance and H/V field of view to exactly view a rectangle with dimensions 'width'/'height'
VIEW3D_API void __stdcall View3D_CameraSetViewRect(View3DWindow window, float width, float height, float dist)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_camera.View(width, height, dist);
	}
	CatchAndReport(View3D_CameraSetViewRect, window,);
}

// Return the aspect ratio for the camera field of view
VIEW3D_API float __stdcall View3D_CameraAspect(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

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
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_camera.Aspect(aspect);
	}
	CatchAndReport(View3D_CameraSetAspect, window,);
}

// Return the horizontal field of view (in radians).
VIEW3D_API float __stdcall View3D_CameraFovXGet(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->m_camera.FovX();
	}
	CatchAndReport(View3D_CameraFovXGet, window, 0.0f);
}

// Set the horizontal field of view (in radians). Note aspect ratio is preserved, setting FovX changes FovY and visa versa
VIEW3D_API void __stdcall View3D_CameraFovXSet(View3DWindow window, float fovX)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_camera.FovX(fovX);
	}
	CatchAndReport(View3D_CameraFovXSet, window,);
}

// Return the vertical field of view (in radians).
VIEW3D_API float __stdcall View3D_CameraFovYGet(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->m_camera.FovY();
	}
	CatchAndReport(View3D_CameraFovYGet, window, 0.0f);
}

// Set the vertical field of view (in radians). Note aspect ratio is preserved, setting FovY changes FovX and visa versa
VIEW3D_API void __stdcall View3D_CameraFovYSet(View3DWindow window, float fovY)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_camera.FovY(fovY);
	}
	CatchAndReport(View3D_CameraFovYSet, window,);
}

// Set both the X and Y fields of view (i.e. set the aspect ratio)
VIEW3D_API void __stdcall View3D_CameraSetFov(View3DWindow window, float fovX, float fovY)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_camera.Fov(fovX, fovY);
	}
	CatchAndReport(View3D_CameraSetFov, window,);
}

// Adjust the FocusDist, FovX, and FovY so that the average FOV equals 'fov'
VIEW3D_API void __stdcall View3D_CameraBalanceFov(View3DWindow window, float fov)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_camera.BalanceFov(fov);
	}
	CatchAndReport(View3D_CameraBalanceFov, window,);
}

// Get/Set the near and far clip planes for the camera
VIEW3D_API void __stdcall View3D_CameraClipPlanesGet(View3DWindow window, float& near_, float& far_, BOOL focus_relative)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		auto cp = window->m_camera.ClipPlanes(focus_relative != 0);
		near_ = cp.x;
		far_  = cp.y;
	}
	CatchAndReport(View3D_CameraClipPlanesGet, window,);
}
VIEW3D_API void __stdcall View3D_CameraClipPlanesSet(View3DWindow window, float near_, float far_, BOOL focus_relative)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_camera.ClipPlanes(near_, far_, focus_relative != 0);
	}
	CatchAndReport(View3D_CameraClipPlanesSet, window,);
}

// Reset to the default zoom
VIEW3D_API void __stdcall View3D_CameraResetZoom(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_camera.ResetZoom();
	}
	CatchAndReport(View3D_CameraResetZoom, window,);
}

// Get/Set the FOV zoom
VIEW3D_API float __stdcall View3D_CameraZoomGet(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->m_camera.Zoom();
	}
	CatchAndReport(View3D_CameraZoomGet, window, 1.0f);
}
VIEW3D_API void __stdcall View3D_CameraZoomSet(View3DWindow window, float zoom)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_camera.Zoom(zoom, true);
	}
	CatchAndReport(View3D_CameraZoomSet, window,);
}

// Get/Set the scene camera lock mask
VIEW3D_API EView3DCameraLockMask __stdcall View3D_CameraLockMaskGet(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return static_cast<EView3DCameraLockMask>(window->m_camera.m_lock_mask);
	}
	CatchAndReport(View3D_CameraLockMaskGet, window, EView3DCameraLockMask::None);
}
VIEW3D_API void __stdcall View3D_CameraLockMaskSet(View3DWindow window, EView3DCameraLockMask mask)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_camera.m_lock_mask = static_cast<pr::camera::ELockMask>(mask);
	}
	CatchAndReport(View3D_CameraLockMaskSet, window,);
}

// Return the camera align axis
VIEW3D_API View3DV4 __stdcall View3D_CameraAlignAxisGet(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return view3d::To<View3DV4>(window->m_camera.m_align);
	}
	CatchAndReport(View3D_CameraAlignAxisGet, window, view3d::To<View3DV4>(pr::v4Zero));
}

// Align the camera to an axis
VIEW3D_API void __stdcall View3D_CameraAlignAxisSet(View3DWindow window, View3DV4 axis)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_camera.Align(view3d::To<pr::v4>(axis));
	}
	CatchAndReport(View3D_CameraAlignAxisSet, window,);
}

// Move the camera to a position that can see the whole scene. Set 'dist' to 0 to preserve the FoV, or a distance to set the FoV
VIEW3D_API void __stdcall View3D_ResetView(View3DWindow window, View3DV4 forward, View3DV4 up, float dist, BOOL preserve_aspect, BOOL commit)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");
		DllLockGuard;
		window->ResetView(view3d::To<pr::v4>(forward), view3d::To<pr::v4>(up), dist, preserve_aspect != 0, commit != 0);
	}
	CatchAndReport(View3D_ResetView, window,);
}

// Reset the camera to view a bbox. Set 'dist' to 0 to preserve the FoV, or a distance to set the FoV
VIEW3D_API void __stdcall View3D_ResetViewBBox(View3DWindow window, View3DBBox bbox, View3DV4 forward, View3DV4 up, float dist, BOOL preserve_aspect, BOOL commit)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");
		DllLockGuard;
		window->ResetView(view3d::To<pr::BBox>(bbox), view3d::To<pr::v4>(forward), view3d::To<pr::v4>(up), dist, preserve_aspect != 0, commit != 0);
	}
	CatchAndReport(View3D_ResetViewBBox, window,);
}

// Return the size of the perpendicular area visible to the camera at 'dist' (in world space)
VIEW3D_API View3DV2 __stdcall View3D_ViewArea(View3DWindow window, float dist)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return view3d::To<View3DV2>(window->m_camera.ViewArea(dist));
	}
	CatchAndReport(View3D_ViewArea, window, view3d::To<View3DV2>(pr::v2Zero));
}

// General mouse navigation
// 'ss_pos' is the mouse pointer position in 'window's screen space
// 'button_state' is the state of the mouse buttons and control keys (i.e. MF_LBUTTON, etc)
// 'nav_start_or_end' should be TRUE on mouse down/up events, FALSE for mouse move events
// void OnMouseDown(UINT nFlags, CPoint point) { View3D_Navigate(m_drawset, point, nFlags, TRUE); }
// void OnMouseMove(UINT nFlags, CPoint point) { View3D_Navigate(m_drawset, point, nFlags, FALSE); } if 'nFlags' is zero, this will have no effect
// void OnMouseUp  (UINT nFlags, CPoint point) { View3D_Navigate(m_drawset, point, 0, TRUE); }
// BOOL OnMouseWheel(UINT nFlags, short zDelta, CPoint) { if (nFlags == 0) View3D_Navigate(m_drawset, 0, 0, zDelta / 120.0f); return TRUE; }
VIEW3D_API BOOL __stdcall View3D_MouseNavigate(View3DWindow window, View3DV2 ss_pos, EView3DNavOp nav_op, BOOL nav_start_or_end)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		auto ss_point = view3d::To<pr::v2>(ss_pos);
		auto nss_point = window->SSPointToNSSPoint(ss_point);
		auto op = static_cast<pr::camera::ENavOp>(nav_op);

		auto refresh = false;
		auto gizmo_in_use = false;

		// Check any gizmos in the scene for interaction with the mouse
		for (auto& giz : window->m_gizmos)
		{
			refresh |= giz->MouseControl(window->m_camera, nss_point, op, nav_start_or_end != 0);
			gizmo_in_use |= giz->m_manipulating;
			if (gizmo_in_use) break;
		}

		// If no gizmos are using the mouse, use standard mouse control
		if (!gizmo_in_use)
		{
			if (window->m_camera.MouseControl(nss_point, op, nav_start_or_end != 0))
				refresh |= true;
		}

		return refresh;
	}
	CatchAndReport(View3D_MouseNavigate, window, FALSE);
}

// <summary>
// Zoom using the mouse.
// 'point' is a point in client rect space.
// 'delta' is the mouse wheel scroll delta value (i.e. 120 = 1 click)
// Returns true if the scene requires refreshing
VIEW3D_API BOOL __stdcall View3D_MouseNavigateZ(View3DWindow window, View3DV2 ss_pos, float delta, BOOL along_ray)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		auto ss_point = view3d::To<pr::v2>(ss_pos);
		auto nss_point = window->SSPointToNSSPoint(ss_point);

		auto refresh = false;
		auto gizmo_in_use = false;

		// Check any gizmos in the scene for interaction with the mouse
		#if 0 // todo, gizmo mouse wheel behaviour
		for (auto& giz : window->m_gizmos)
		{
			refresh |= giz->MouseControlZ(window->m_camera, nss_point, dist);
			gizmo_in_use |= giz->m_manipulating;
			if (gizmo_in_use) break;
		}
		#endif

		// If no gizmos are using the mouse, use standard mouse control
		if (!gizmo_in_use)
		{
			if (window->m_camera.MouseControlZ(nss_point, delta, along_ray != 0))
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
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->m_camera.Translate(dx, dy, dz);
	}
	CatchAndReport(View3D_Navigate, window, FALSE);
}

// Get/Set the camera focus point position
VIEW3D_API void __stdcall View3D_FocusPointGet(View3DWindow window, View3DV4& position)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		position = view3d::To<View3DV4>(window->m_camera.FocusPoint());
	}
	CatchAndReport(View3D_FocusPointGet, window,);
}
VIEW3D_API void __stdcall View3D_FocusPointSet(View3DWindow window, View3DV4 position)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_camera.FocusPoint(view3d::To<pr::v4>(position));
	}
	CatchAndReport(View3D_FocusPointSet, window,);
}

// Convert a point in 'window' screen space to normalised screen space
VIEW3D_API View3DV2 __stdcall View3D_SSPointToNSSPoint(View3DWindow window, View3DV2 screen)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

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
		if (!window) throw std::runtime_error("window is null");

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
		if (!window) throw std::runtime_error("window is null");

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
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		pr::v4 pt,dir;
		window->m_camera.NSSPointToWSRay(view3d::To<pr::v4>(screen), pt, dir);
		ws_point = view3d::To<View3DV4>(pt);
		ws_direction = view3d::To<View3DV4>(dir);
	}
	CatchAndReport(View3D_NSSPointToWSRay, window,);
}

// Convert an MK_ macro to a default navigation operation
VIEW3D_API EView3DNavOp __stdcall View3D_MouseBtnToNavOp(int mk)
{
	return static_cast<EView3DNavOp>(pr::camera::MouseBtnToNavOp(mk));
}

// Lighting ********************************************************

// Return the configuration of the single light source
VIEW3D_API void __stdcall View3D_LightProperties(View3DWindow window, View3DLight& light)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		light.m_position        = view3d::To<View3DV4>(window->m_light.m_position);
		light.m_direction       = view3d::To<View3DV4>(window->m_light.m_direction);
		light.m_type            = static_cast<EView3DLight>(window->m_light.m_type);
		light.m_ambient         = window->m_light.m_ambient;
		light.m_diffuse         = window->m_light.m_diffuse;
		light.m_specular        = window->m_light.m_specular;
		light.m_specular_power  = window->m_light.m_specular_power;
		light.m_inner_cos_angle = window->m_light.m_inner_cos_angle;
		light.m_outer_cos_angle = window->m_light.m_outer_cos_angle;
		light.m_range           = window->m_light.m_range;
		light.m_falloff         = window->m_light.m_falloff;
		light.m_cast_shadow     = window->m_light.m_cast_shadow;
		light.m_on              = window->m_light.m_on;
		light.m_cam_relative    = window->m_light.m_cam_relative;
	}
	CatchAndReport(View3D_LightProperties, window, );
}

// Configure the single light source
VIEW3D_API void __stdcall View3D_SetLightProperties(View3DWindow window, View3DLight const& light)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_light.m_position        = view3d::To<pr::v4>(light.m_position);
		window->m_light.m_direction       = view3d::To<pr::v4>(light.m_direction);
		window->m_light.m_type            = pr::Enum<pr::rdr::ELight>::From(light.m_type);
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
		window->m_light.m_cam_relative    = light.m_cam_relative != 0;
	}
	CatchAndReport(View3D_SetLightProperties, window,);
}

// Set up a single light source for a window
VIEW3D_API void __stdcall View3D_LightSource(View3DWindow window, View3DV4 position, View3DV4 direction, BOOL camera_relative)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_light.m_position = view3d::To<pr::v4>(position);
		window->m_light.m_direction = view3d::To<pr::v4>(direction);
		window->m_light.m_cam_relative = camera_relative != 0;
	}
	CatchAndReport(View3D_LightSource, window,);
}

// Show the lighting UI
VIEW3D_API void __stdcall View3D_ShowLightingDlg(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;

		auto pv = [&](Light const& light)
		{
			auto prev_light = window->m_light;
			window->m_light = light;

			View3D_Render(window);
			View3D_Present(window);

			window->m_light = prev_light;
		};

		LightingUI dlg(window->m_hwnd, window->m_light, pv);
		if (dlg.ShowDialog(window->m_wnd.m_hwnd) != pr::gui::EDialogResult::Ok)
			return;

		window->m_light = dlg.m_light;

		View3D_Render(window);
		View3D_Present(window);

		window->NotifySettingsChanged(EView3DWindowSettings::Lighting);
	}
	CatchAndReport(View3D_ShowLightingDlg, window,);
}

// Objects **********************************************************

// Return the context id that this object belongs to
VIEW3D_API GUID __stdcall View3D_ObjectContextIdGet(View3DObject object)
{
	try
	{
		if (!object) throw std::runtime_error("object is null");

		DllLockGuard;
		return object->m_context_id;
	}
	CatchAndReport(View3D_ObjectContextIdGet, , GUID());
}

// Create objects given in an ldr string or file.
// If multiple objects are created, the handle returned is to the first object only
// 'ldr_script' - an ldr string, or filepath to a file containing ldr script
// 'file' - TRUE if 'ldr_script' is a filepath, FALSE if 'ldr_script' is a string containing ldr script
// 'context_id' - the context id to create the LdrObjects with
// 'async' - if objects should be created by a background thread
// 'includes' - information used to resolve include directives in 'ldr_script'
VIEW3D_API View3DObject __stdcall View3D_ObjectCreateLdr(wchar_t const* ldr_script, BOOL file, GUID const* context_id, View3DIncludes const* includes)
{
	try
	{
		DllLockGuard;
		return Dll().ObjectCreateLdr(ldr_script, file != 0, context_id, GetIncludes(includes));
	}
	CatchAndReport(View3D_ObjectCreateLdr, , nullptr);
}

// Create an object from provided buffers
VIEW3D_API View3DObject __stdcall View3D_ObjectCreate(char const* name, View3DColour colour, int vcount, int icount, int ncount, View3DVertex const* verts, UINT16 const* indices, View3DNugget const* nuggets, GUID const& context_id)
{
	try
	{
		DllLockGuard;
		return Dll().ObjectCreate(name, colour, vcount, icount, ncount, verts, indices, nuggets, context_id);
	}
	CatchAndReport(View3D_ObjectCreate, , nullptr);
}

// Create an object via callback
VIEW3D_API View3DObject __stdcall View3D_ObjectCreateEditCB(char const* name, View3DColour colour, int vcount, int icount, int ncount, View3D_EditObjectCB edit_cb, void* ctx, GUID const& context_id)
{
	try
	{
		DllLockGuard;
		Context::ObjectEditCBData cbdata = {edit_cb, ctx};
		pr::ldr::ObjectAttributes attr(pr::ldr::ELdrObject::Custom, name, pr::Colour32(colour));
		auto obj = pr::ldr::CreateEditCB(Dll().m_rdr, attr, vcount, icount, ncount, Context::ObjectEditCB, &cbdata, context_id);
		if (obj)
			Dll().m_sources.Add(obj);

		return obj.m_ptr;
	}
	CatchAndReport(View3D_ObjectCreateEditCB, , nullptr);
}

// Create an instance of 'obj'
VIEW3D_API View3DObject __stdcall View3D_ObjectCreateInstance(View3DObject existing)
{
	try
	{
		DllLockGuard;
		auto obj = pr::ldr::CreateInstance(existing);
		if (obj) Dll().m_sources.Add(obj);
		return obj.get();
	}
	CatchAndReport(View3D_ObjectCreateInstance, , nullptr);
}

// Edit an existing model
VIEW3D_API void __stdcall View3D_ObjectEdit(View3DObject object, View3D_EditObjectCB edit_cb, void* ctx)
{
	try
	{
		if (!object) throw std::runtime_error("Object is null");

		DllLockGuard;
		Dll().EditObject(object, edit_cb, ctx);
	}
	CatchAndReport(View3D_ObjectEdit, ,);
}

// Replace the model and all child objects of 'obj' with the results of 'ldr_script'
VIEW3D_API void __stdcall View3D_ObjectUpdate(View3DObject object, wchar_t const* ldr_script, EView3DUpdateObject flags)
{
	try
	{
		if (!object) throw std::runtime_error("object is null");

		DllLockGuard;
		Dll().UpdateObject(object, ldr_script, static_cast<pr::ldr::EUpdateObject>(flags));
	}
	CatchAndReport(View3D_ObjectUpdate, ,);
}

// Delete an object
VIEW3D_API void __stdcall View3D_ObjectDelete(View3DObject object)
{
	try
	{
		if (!object) return;
		
		DllLockGuard;
		Dll().DeleteObject(object);
	}
	CatchAndReport(View3D_ObjectDelete, ,);
}

// Return the root object of 'object' (possibly itself)
VIEW3D_API View3DObject __stdcall View3D_ObjectGetRoot(View3DObject object)
{
	try
	{
		if (!object) throw std::runtime_error("object is null");

		DllLockGuard;
		auto p = object;
		for (; p->m_parent != nullptr; p = p->m_parent) {}
		return p;
	}
	CatchAndReport(View3D_ObjectGetRoot, , nullptr);
}

// Return the immediate parent of 'object'
VIEW3D_API View3DObject __stdcall View3D_ObjectGetParent(View3DObject object)
{
	try
	{
		if (!object) throw std::runtime_error("object is null");

		DllLockGuard;
		return object->m_parent;
	}
	CatchAndReport(View3D_ObjectGetParent, , nullptr);
}

// Return a child object of 'object'
VIEW3D_API View3DObject __stdcall View3D_ObjectGetChildByName(View3DObject object, char const* name)
{
	try
	{
		if (!object) throw std::runtime_error("object is null");

		DllLockGuard;
		return object->Child(name);
	}
	CatchAndReport(View3D_ObjectGetChildByName, , nullptr);
}
VIEW3D_API View3DObject __stdcall View3D_ObjectGetChildByIndex(View3DObject object, int index)
{
	try
	{
		if (!object) throw std::runtime_error("object is null");

		DllLockGuard;
		return object->Child(index);
	}
	CatchAndReport(View3D_ObjectGetChildByIndex, , nullptr);
}

// Return the number of child objects of 'object'
VIEW3D_API int __stdcall View3D_ObjectChildCount(View3DObject object)
{
	try
	{
		if (!object) throw std::runtime_error("object is null");

		DllLockGuard;
		return int(object->m_child.size());
	}
	CatchAndReport(View3D_ObjectChildCount, object, 0);
}

// Enumerate the child objects of 'object'
VIEW3D_API void __stdcall View3D_ObjectEnumChildren(View3DObject object, View3D_EnumObjectsCB enum_objects_cb, void* ctx)
{
	try
	{
		if (!object) throw std::runtime_error("object is null");

		DllLockGuard;
		for (auto& child : object->m_child)
		{
			if (enum_objects_cb(ctx, child.get())) continue;
			break;
		}
	}
	CatchAndReport(View3D_ObjectEnumChildren, object, );
}

// Get/Set the name of 'object'
VIEW3D_API BSTR __stdcall View3D_ObjectNameGetBStr(View3DObject object)
{
	try
	{
		DllLockGuard;
		auto name = Widen(object->m_name);
		return ::SysAllocStringLen(name.c_str(), UINT(name.size()));
	}
	CatchAndReport(View3D_ObjectNameGetBStr, , BSTR());
}
VIEW3D_API char const* __stdcall View3D_ObjectNameGet(View3DObject object)
{
	try
	{
		DllLockGuard;
		return object->m_name.c_str();
	}
	CatchAndReport(View3D_ObjectNameGet, , nullptr);
}
VIEW3D_API void __stdcall View3D_ObjectNameSet(View3DObject object, char const* name)
{
	try
	{
		DllLockGuard;
		object->m_name.assign(name);
	}
	CatchAndReport(View3D_ObjectNameGet, ,);
}

// Get the type of 'object'
VIEW3D_API BSTR __stdcall View3D_ObjectTypeGetBStr(View3DObject object)
{
	try
	{
		DllLockGuard;
		auto name = ToStringW(object->m_type);
		return ::SysAllocStringLen(name, UINT(wcslen(name)));
	}
	CatchAndReport(View3D_ObjectTypeGetBStr, , BSTR());
}
VIEW3D_API char const*  __stdcall View3D_ObjectTypeGet(View3DObject object)
{
	try
	{
		DllLockGuard;
		return ToStringA(object->m_type);
	}
	CatchAndReport(View3D_ObjectTypeGet, , nullptr);
}

// Get/Set the object to world transform for this object or the first child object that matches 'name'.
// If 'name' is null, then the state of the root object is returned
// If 'name' begins with '#' then the remainder of the name is treated as a regular expression
// Note, setting the o2w for a child object positions the object in world space rather than parent space
// (internally the appropriate O2P transform is calculated to put the object at the given O2W location)
VIEW3D_API View3DM4x4 __stdcall View3D_ObjectO2WGet(View3DObject object, char const* name)
{
	try
	{
		if (!object) throw std::runtime_error("object is null");

		DllLockGuard;
		return view3d::To<View3DM4x4>(object->O2W(name));
	}
	CatchAndReport(View3D_ObjectGetO2W, , view3d::To<View3DM4x4>(pr::m4x4Identity));
}
VIEW3D_API void __stdcall View3D_ObjectO2WSet(View3DObject object, View3DM4x4 const& o2w, char const* name)
{
	try
	{
		if (object == nullptr) throw std::runtime_error("Object is null");
		if (!pr::FEql(o2w.w.w,1.0f)) throw std::runtime_error("invalid object to world transform");

		DllLockGuard;
		object->O2W(view3d::To<pr::m4x4>(o2w), name);
	}
	CatchAndReport(View3D_ObjectSetO2W, ,);
}

// Get/Set the object to parent transform for an object
// This is the object to world transform for objects without parents
// Note: In "*Box b { 1 1 1 *o2w{*pos{1 2 3}} }" setting this transform overwrites the "*o2w{*pos{1 2 3}}"
VIEW3D_API View3DM4x4 __stdcall View3D_ObjectO2PGet(View3DObject object, char const* name)
{
	try
	{
		if (!object) throw std::runtime_error("object is null");

		DllLockGuard;
		return view3d::To<View3DM4x4>(object->O2P(name));
	}
	CatchAndReport(View3D_ObjectGetO2P, , view3d::To<View3DM4x4>(pr::m4x4Identity));
}
VIEW3D_API void __stdcall View3D_ObjectO2PSet(View3DObject object, View3DM4x4 const& o2p, char const* name)
{
	try
	{
		if (!object) throw std::runtime_error("Object is null");
		if (!pr::FEql(o2p.w.w,1.0f)) throw std::runtime_error("invalid object to parent transform");

		DllLockGuard;
		object->O2P(view3d::To<pr::m4x4>(o2p), name);
	}
	CatchAndReport(View3D_ObjectSetO2P, ,);
}

// Get/Set the object visibility
// See LdrObject::Apply for docs on the format of 'name'
VIEW3D_API BOOL __stdcall View3D_ObjectVisibilityGet(View3DObject object, char const* name)
{
	try
	{
		if (!object) throw std::runtime_error("Object is null");

		DllLockGuard;
		return const_cast<pr::ldr::LdrObject const*>(object)->Visible(name);
	}
	CatchAndReport(View3D_ObjectGetVisibility, ,FALSE);
}
VIEW3D_API void __stdcall View3D_ObjectVisibilitySet(View3DObject object, BOOL visible, char const* name)
{
	try
	{
		if (!object) throw std::runtime_error("Object is null");

		DllLockGuard;
		object->Visible(visible != 0, name);
	}
	CatchAndReport(View3D_ObjectSetVisibility, ,);
}

// Get/Set the object flags
// See LdrObject::Apply for docs on the format of 'name'
VIEW3D_API EView3DFlags __stdcall View3D_ObjectFlagsGet(View3DObject object, char const* name)
{
	try
	{
		if (!object) throw std::runtime_error("Object is null");

		DllLockGuard;
		return static_cast<EView3DFlags>(object->Flags(name));
	}
	CatchAndReport(View3D_ObjectFlagsGet, ,EView3DFlags::None);
}
VIEW3D_API void __stdcall View3D_ObjectFlagsSet(View3DObject object, EView3DFlags flags, BOOL state, char const* name)
{
	try
	{
		if (!object) throw std::runtime_error("Object is null");

		DllLockGuard;
		object->Flags(static_cast<pr::ldr::ELdrFlags>(flags), state != 0, name);
	}
	CatchAndReport(View3D_ObjectFlagsSet, ,);
}

// Get/Set the current or base colour of an object (the first object to match 'name') (See LdrObject::Apply)
VIEW3D_API View3DColour __stdcall View3D_ObjectColourGet(View3DObject object, BOOL base_colour, char const* name)
{
	try
	{
		if (!object) throw std::runtime_error("Object is null");

		DllLockGuard;
		return object->Colour(base_colour != 0, name);
	}
	CatchAndReport(View3D_ObjectGetColour, ,View3DColour(0xFFFFFFFF));
}
VIEW3D_API void __stdcall View3D_ObjectColourSet(View3DObject object, View3DColour colour, UINT32 mask, char const* name, EView3DColourOp op, float op_value)
{
	try
	{
		if (!object) throw std::runtime_error("Object is null");

		DllLockGuard;
		object->Colour(pr::Colour32(colour), mask, name, static_cast<pr::ldr::EColourOp>(op), op_value);
	}
	CatchAndReport(View3D_ObjectSetColour, ,);
}

// Get/Set wireframe mode for an object (the first object to match 'name') (See LdrObject::Apply)
VIEW3D_API BOOL __stdcall View3D_ObjectWireframeGet(View3DObject object, char const* name)
{
	try
	{
		if (!object) throw std::runtime_error("Object is null");

		DllLockGuard;
		return const_cast<pr::ldr::LdrObject const*>(object)->Wireframe(name);
	}
	CatchAndReport(View3D_ObjectWireframeGet, , FALSE);
}
VIEW3D_API void __stdcall View3D_ObjectWireframeSet(View3DObject object, BOOL wire_frame, char const* name)
{
	try
	{
		if (!object) throw std::runtime_error("Object is null");

		DllLockGuard;
		object->Wireframe(wire_frame != 0, name);
	}
	CatchAndReport(View3D_ObjectWireframeSet, , );
}

// Reset the object colour back to its default
VIEW3D_API void __stdcall View3D_ObjectResetColour(View3DObject object, char const* name)
{
	try
	{
		if (!object) throw std::runtime_error("Object is null");

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
		if (!object) throw std::runtime_error("Object is null");

		DllLockGuard;
		object->SetTexture(tex, name);
	}
	CatchAndReport(View3D_ObjectSetTexture, ,);
}

// Return the model space bounding box for 'object'
VIEW3D_API View3DBBox __stdcall View3D_ObjectBBoxMS(View3DObject object, int include_children)
{
	try
	{
		if (!object) throw std::runtime_error("Object is null");

		DllLockGuard;
		return view3d::To<View3DBBox>(object->BBoxMS(include_children != 0));
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
			throw std::runtime_error("Incorrect data size provided");

		Texture2DDesc tdesc(src);
		tdesc.Format = options.m_format;
		tdesc.MipLevels = options.m_mips;
		tdesc.SampleDesc = pr::rdr::MultiSamp(options.m_multisamp, 0U);
		tdesc.BindFlags = options.m_bind_flags | (options.m_gdi_compatible ? D3D11_BIND_SHADER_RESOURCE|D3D11_BIND_RENDER_TARGET : 0);
		tdesc.MiscFlags = options.m_misc_flags | (options.m_gdi_compatible ? D3D11_RESOURCE_MISC_GDI_COMPATIBLE : 0);

		SamplerDesc sdesc;
		sdesc.AddressU = options.m_addrU;
		sdesc.AddressV = options.m_addrV;
		sdesc.Filter = options.m_filter;

		auto name = options.m_dbg_name;
		auto has_alpha = options.m_has_alpha != 0;

		DllLockGuard;
		auto t = options.m_gdi_compatible
			? Dll().m_rdr.m_tex_mgr.CreateTextureGdi(AutoId, src, tdesc, sdesc, has_alpha, name)
			: Dll().m_rdr.m_tex_mgr.CreateTexture2D(AutoId, src, tdesc, sdesc, has_alpha, name);

		// Rely on the caller for correct reference counting
		return t.release();
	}
	CatchAndReport(View3D_TextureCreate, , nullptr);
}

// Load a texture from file, embedded resource, or stock assets. Specify width == 0, height == 0 to use the dimensions of the file
VIEW3D_API View3DTexture __stdcall View3D_TextureCreateFromUri(wchar_t const* resource, UINT32 width, UINT32 height, View3DTextureOptions const& options)
{
	try
	{
		(void)width,height; //todo

		SamplerDesc sdesc;
		sdesc.AddressU = options.m_addrU;
		sdesc.AddressV = options.m_addrV;
		sdesc.Filter   = options.m_filter;

		auto name = options.m_dbg_name;
		auto has_alpha = options.m_has_alpha != 0;

		DllLockGuard;
		auto t = Dll().m_rdr.m_tex_mgr.CreateTexture2D(AutoId, resource, sdesc, has_alpha, name);
		
		// Rely on the caller for correct reference counting
		return t.release();
	}
	CatchAndReport(View3D_TextureCreateFromFile, , nullptr);
}

// Get/Release a DC for the texture. Must be a TextureGdi texture
VIEW3D_API HDC __stdcall View3D_TextureGetDC(View3DTexture tex, BOOL discard)
{
	try
	{
		if (!tex) throw std::runtime_error("Texture is null");
		return tex->GetDC(discard != 0);
	}
	CatchAndReport(View3D_TextureGetDC, , nullptr);
}
VIEW3D_API void __stdcall View3D_TextureReleaseDC(View3DTexture tex)
{
	try
	{
		if (!tex) throw std::runtime_error("Texture is null");
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
		throw std::runtime_error("not implemented");
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
		if (!tex) throw std::runtime_error("Texture is null");
		tex->Release();
	}
	CatchAndReport(View3D_TextureDelete, ,);
}

// Read the properties of an existing texture
VIEW3D_API void __stdcall View3D_TextureGetInfo(View3DTexture tex, View3DImageInfo& info)
{
	try
	{
		if (!tex) throw std::runtime_error("texture is null");
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
		throw std::runtime_error("not implemented");
		//return EView3DResult::Success;
	}
	CatchAndReport(View3D_TextureGetInfoFromFile, , EView3DResult::Failed);
}

// Set the filtering and addressing modes to use on the texture
VIEW3D_API void __stdcall View3D_TextureSetFilterAndAddrMode(View3DTexture tex, D3D11_FILTER filter, D3D11_TEXTURE_ADDRESS_MODE addrU, D3D11_TEXTURE_ADDRESS_MODE addrV)
{
	try
	{
		if (!tex) throw std::runtime_error("Texture is null");
		
		DllLockGuard;
		tex->SetFilterAndAddrMode(filter, addrU, addrV);
	}
	CatchAndReport(View3D_TextureSetFilterAndAddrMode, ,);
}

// Resize a texture to 'size' optionally preserving it's content
VIEW3D_API void __stdcall View3D_TextureResize(View3DTexture tex, UINT32 width, UINT32 height, BOOL all_instances, BOOL preserve)
{
	try
	{
		if (!tex) throw std::runtime_error("Texture is null");

		DllLockGuard;
		tex->Resize(width, height, all_instances != 0, preserve != 0);
	}
	CatchAndReport(View3D_TextureResize, ,);
}

// Get/Set the private data associated with 'guid' for 'tex'
VIEW3D_API void __stdcall View3d_TexturePrivateDataGet(View3DTexture tex, GUID const& guid, UINT& size, void* data)
{
	try
	{
		// 'size' should be the size of the data pointed to by 'data'
		if (!tex) throw std::runtime_error("texture is null");
		pr::Throw(tex->m_res->GetPrivateData(guid, &size, data));
	}
	CatchAndReport(View3d_TexturePrivateDataGet, ,);
}
VIEW3D_API void __stdcall View3d_TexturePrivateDataSet(View3DTexture tex, GUID const& guid, UINT size, void const* data)
{
	try
	{
		if (!tex) throw std::runtime_error("texture is null");
		pr::Throw(tex->m_res->SetPrivateData(guid, size, data));
	}
	CatchAndReport(View3d_TexturePrivateDataSet, ,);
}
VIEW3D_API void __stdcall View3d_TexturePrivateDataIFSet(View3DTexture tex, GUID const& guid, IUnknown* pointer)
{
	try
	{
		if (!tex) throw std::runtime_error("texture is null");
		pr::Throw(tex->m_res->SetPrivateDataInterface(guid, pointer));
	}
	CatchAndReport(View3d_TexturePrivateDataIFSet, ,);
}

// Get the current ref count of 'tex'
VIEW3D_API ULONG __stdcall View3D_TextureRefCount(View3DTexture tex)
{
	try
	{
		if (!tex) throw std::runtime_error("texture is null");
		return tex->m_ref_count;
	}
	CatchAndReport(View3D_TextureRefCount, , 0);
}

// Return the render target as a texture
VIEW3D_API View3DTexture __stdcall View3D_TextureRenderTarget(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->m_wnd.m_main_tex.m_ptr;
	}
	CatchAndReport(View3D_TextureResize, window, nullptr);
}

// Resolve a MSAA texture into a non-MSAA texture
VIEW3D_API void __stdcall View3D_TextureResolveAA(View3DTexture dst, View3DTexture src)
{
	try
	{
		if (!src) throw std::runtime_error("Source texture pointer is null");
		if (!dst) throw std::runtime_error("Destination texture pointer is null");
		
		auto src_tdesc = src->TexDesc();
		auto dst_tdesc = dst->TexDesc();
		if (src_tdesc.Format != dst_tdesc.Format)
			throw std::runtime_error("Source and destination textures must has the same format");

		pr::Renderer::Lock lock(Dll().m_rdr);
		lock.ImmediateDC()->ResolveSubresource(dst->m_res.get(), 0U, src->m_res.get(), 0U, src_tdesc.Format);
	}
	CatchAndReport(View3D_TextureResolveAA, , );
}

// Create a Texture instance from a shared d3d resource (created on a different d3d device)
VIEW3D_API View3DTexture __stdcall View3D_TextureFromShared(IUnknown* shared_resource, View3DTextureOptions const& options)
{
	try
	{
		if (!shared_resource) throw std::runtime_error("resource pointer is null");

		DllLockGuard;

		SamplerDesc sdesc;
		sdesc.AddressU = options.m_addrU;
		sdesc.AddressV = options.m_addrV;
		sdesc.Filter = options.m_filter;

		auto t = Dll().m_rdr.m_tex_mgr.CreateTexture2D(AutoId, shared_resource, sdesc, options.m_has_alpha != 0, options.m_dbg_name);
		return t.release();
	}
	CatchAndReport(View3D_TextureFromExisting, , nullptr);
}

// Create a render target texture on a D3D9 device. Intended for WPF D3DImage
VIEW3D_API View3DTexture __stdcall View3D_CreateDx9RenderTarget(HWND hwnd, UINT width, UINT height, View3DTextureOptions const& options, HANDLE* shared_handle)
{
	try
	{
		if (hwnd == nullptr) throw std::runtime_error("DirectX 9 requires a window handle");
		
		// Convert the DXGI format to a Dx9 one
		auto fmt = Dx9Context::ConvertFormat(options.m_format);
		if (fmt == D3DFMT_UNKNOWN)
			throw std::runtime_error(pr::FmtS("No compatible DirectX 9 texture format for DXGI format %d", options.m_format));
		
		// Initialise 'handle' from the optional 'shared_handle'
		// If '*shared_handle != nullptr', then CreateTexture will create a Dx9 texture that uses the shared resource.
		// If 'shared_handle == nullptr', then the caller doesn't care about the shared handle, but we still need it so
		// that the created texture will be shared and we can create a Dx11 texture from it.
		HANDLE handle = shared_handle ? *shared_handle : nullptr;

		// Create the shared Dx9 texture
		Dx9Context dx9(hwnd);
		auto tex = dx9.CreateTexture(width, height, 1, D3DUSAGE_RENDERTARGET, fmt, D3DPOOL_DEFAULT, &handle);
		
		// Access the main surface of the render target texture
		D3DPtr<IDirect3DSurface9> surf0;
		tex->GetSurfaceLevel(0, &surf0.m_ptr);

		// Save the shared handle if the caller wants it.
		if (shared_handle != nullptr)
			*shared_handle = handle;

		SamplerDesc sdesc;
		sdesc.AddressU = options.m_addrU;
		sdesc.AddressV = options.m_addrV;
		sdesc.Filter = options.m_filter;

		DllLockGuard;

		// Create a Dx11 texture using the shared resource
		auto t = Dll().m_rdr.m_tex_mgr.OpenSharedTexture2D(AutoId, handle, sdesc, options.m_has_alpha != 0, options.m_dbg_name);

		// Save the surface 0 pointer in the private data of the texture. (Adds a reference)
		t->m_res->SetPrivateDataInterface(pr::rdr::Texture2D::Surface0Pointer, surf0.get());

		// Add a handler to clean up this reference when the texture is destroyed
		auto surf0_ptr = surf0.get(); // Don't capture the RefPtr
		t->OnDestruction += [=](TextureBase&, pr::EmptyArgs const&){ surf0_ptr->Release(); };

		return t.release();
	}
	CatchAndReport(View3D_CreateDx9RenderTarget, , nullptr);
}

// Rendering ***************************************************************

// Call InvalidateRect on the HWND associated with 'window'
VIEW3D_API void __stdcall View3D_Invalidate(View3DWindow window, BOOL erase)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");
		window->Invalidate(erase != 0);
	}
	CatchAndReport(View3D_Invalidate, window, );
}

// Call InvalidateRect on the HWND associated with 'window'
VIEW3D_API void __stdcall View3D_InvalidateRect(View3DWindow window, RECT const* rect, BOOL erase)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");
		window->InvalidateRect(rect, erase != 0);
	}
	CatchAndReport(View3D_InvalidateRect, window,);
}

// Render a window. Remember to call View3D_Present() after all render calls
VIEW3D_API void __stdcall View3D_Render(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->Render();
	}
	CatchAndReport(View3D_Render, window,);
}

// Finish rendering with a back buffer flip.
// If rendering to a texture, this does a d3ddevice->Flush instead
VIEW3D_API void __stdcall View3D_Present(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->Present();
	}
	CatchAndReport(View3D_Present, window,);
}

// Clear the 'invalidated' state of the window.
VIEW3D_API void __stdcall View3D_Validate(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->Validate();
	}
	CatchAndReport(View3D_Validate, window, );
}

// Set the render target back to the main back buffer.
// This needs to be called after 'View3D_RenderTargetSet' has been called to render to a texture
VIEW3D_API void __stdcall View3D_RenderTargetRestore(View3DWindow window)
{
	try
	{
		if (window == nullptr) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_wnd.RestoreRT();
	}
	CatchAndReport(View3D_RenderTargetRestore, window,);
}

// Render a window into a texture
// 'render_target' is the texture that is rendered onto
// 'depth_buffer' is an optional texture that will receive the depth information (can be null)
VIEW3D_API void __stdcall View3D_RenderTargetSet(View3DWindow window, View3DTexture render_target, View3DTexture depth_buffer)
{
	try
	{
		if (window == nullptr) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_wnd.SetRT(
			render_target != nullptr ? render_target->dx_tex() : nullptr,
			depth_buffer != nullptr ? depth_buffer->dx_tex() : nullptr);
	}
	CatchAndReport(View3D_RenderTargetSet, window,);
}

// Save the current render target as the main render target.
// Calling RestoreRT will restore this new main render target.
VIEW3D_API void __stdcall View3D_RenderTargetSaveAsMain(View3DWindow window)
{
	try
	{
		if (window == nullptr) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_wnd.SaveAsMainRT();
	}
	CatchAndReport(View3D_RenderTargetSaveAsMain, window,);
}

// Get/Set the dimensions of the render target
// In set, if 'width' and 'height' are zero, the RT is resized to the associated window automatically.
VIEW3D_API void __stdcall View3D_BackBufferSizeGet(View3DWindow window, int& width, int& height)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		width = 0;
		height = 0;
		auto area = window->m_wnd.BackBufferSize();
		width = area.x;
		height = area.y;
	}
	CatchAndReport(View3D_RenderTargetSize, window,);
}
VIEW3D_API void __stdcall View3D_BackBufferSizeSet(View3DWindow window, int width, int height)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		if (width  < 0) width  = 0;
		if (height < 0) height = 0;

		// Before resize, the old aspect is: Aspect0 = scale * Width0 / Height0
		// After resize, the new aspect is: Aspect1 = scale * Width1 / Height1

		// Save the current camera aspect ratio
		auto old_size = window->m_wnd.BackBufferSize();
		auto old_aspect = window->m_camera.Aspect();
		auto scale = old_aspect * old_size.y / float(old_size.x);

		// Resize the render target
		window->m_wnd.BackBufferSize(pr::iv2(width, height));

		// Adjust the camera aspect ratio to preserve it
		auto new_size = window->m_wnd.BackBufferSize();
		auto new_aspect = (new_size.x == 0 || new_size.y == 0) ? 1.0f : new_size.x / float(new_size.y);
		auto aspect = scale * new_aspect;

		window->m_camera.Aspect(aspect);
	}
	CatchAndReport(View3D_BackBufferSizeSet, window,);
}

// Get/Set the viewport within the render target
VIEW3D_API View3DViewport __stdcall View3D_Viewport(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

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
		if (!window) throw std::runtime_error("window is null");

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
VIEW3D_API EView3DFillMode __stdcall View3D_FillModeGet(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->m_fill_mode;
	}
	CatchAndReport(View3D_FillModeGet, window, EView3DFillMode());
}
VIEW3D_API void __stdcall View3D_FillModeSet(View3DWindow window, EView3DFillMode mode)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_fill_mode = mode;
		window->Invalidate();
	}
	CatchAndReport(View3D_FillModeSet, window,);
}

// Get/Set the cull mode for a faces in window
VIEW3D_API EView3DCullMode __stdcall View3D_CullModeGet(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->m_cull_mode;
	}
	CatchAndReport(View3D_CullModeGet, window, EView3DCullMode());
}
VIEW3D_API void __stdcall View3D_CullModeSet(View3DWindow window, EView3DCullMode mode)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_cull_mode = mode;
		window->Invalidate();
	}
	CatchAndReport(View3D_CullModeSet, window,);
}

// Get/Set the background colour for a window
VIEW3D_API unsigned int __stdcall View3D_BackgroundColourGet(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->BackgroundColour().argb;
	}
	CatchAndReport(View3D_BackgroundColourGet, window, 0);
}
VIEW3D_API void __stdcall View3D_BackgroundColourSet(View3DWindow window, unsigned int aarrggbb)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->BackgroundColour(pr::Colour32(aarrggbb));
	}
	CatchAndReport(View3D_BackgroundColourSet, window,);
}

// Get/Set the multi-sampling mode for a window
VIEW3D_API int  __stdcall View3D_MultiSamplingGet(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->m_wnd.MultiSampling().Count;
	}
	CatchAndReport(View3D_MultiSamplingGet, window, 1);
}
VIEW3D_API void __stdcall View3D_MultiSamplingSet(View3DWindow window, int multisampling)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		pr::rdr::MultiSamp ms(multisampling);
		window->m_wnd.MultiSampling(ms);
		window->Invalidate();
	}
	CatchAndReport(View3D_MultiSamplingSet, window, );
}

// Tools *******************************************************************

// Display the object manager UI
VIEW3D_API void __stdcall View3D_ObjectManagerShow(View3DWindow window, BOOL show)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->ShowObjectManager(show != 0);
	}
	CatchAndReport(View3D_ObjectManagerShow, window,);
}

// Show the measurement tool
VIEW3D_API BOOL __stdcall View3D_MeasureToolVisible(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->m_measure_tool_ui != nullptr && window->LdrMeasureUI().Visible();
	}
	CatchAndReport(View3D_MeasureToolVisible, window, false);
}
VIEW3D_API void __stdcall View3D_ShowMeasureTool(View3DWindow window, BOOL show)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->ShowMeasureTool(show != 0);
	}
	CatchAndReport(View3D_ShowMeasureTool, window,);
}

// Show the angle tool
VIEW3D_API BOOL __stdcall View3D_AngleToolVisible(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->m_angle_tool_ui != nullptr && window->LdrAngleUI().Visible();
	}
	CatchAndReport(View3D_AngleToolVisible, window, false);
}
VIEW3D_API void __stdcall View3D_ShowAngleTool(View3DWindow window, BOOL show)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->ShowAngleTool(show != 0);
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
		return Dll().CreateGizmo(static_cast<LdrGizmo::EMode>(mode), view3d::To<pr::m4x4>(o2w));
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
		Dll().DeleteGizmo(gizmo);
	}
	CatchAndReport(View3D_GizmoDelete, ,);
}

// Attach?Detach callbacks that are called when the gizmo moves
VIEW3D_API void __stdcall View3D_GizmoAttachCB(View3DGizmo gizmo, View3D_GizmoMovedCB cb, void* ctx)
{
	try
	{
		if (!gizmo) throw std::runtime_error("Gizmo is null");
		if (!cb) throw std::runtime_error("Callback function is null");

		DllLockGuard;
		gizmo->Attach(reinterpret_cast<pr::ldr::LdrGizmoCB::Func>(cb), ctx);
	}
	CatchAndReport(View3D_GizmoAttachCB, ,);
}
VIEW3D_API void __stdcall View3D_GizmoDetachCB(View3DGizmo gizmo, View3D_GizmoMovedCB cb)
{
	try
	{
		if (!gizmo) throw std::runtime_error("Gizmo is null");
		if (!cb) throw std::runtime_error("Callback function is null");

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
		if (!gizmo) throw std::runtime_error("Gizmo is null");
		if (!obj) throw std::runtime_error("Object is null");

		DllLockGuard;
		gizmo->Attach(obj->m_o2p);
	}
	CatchAndReport(View3D_GizmoAttach, ,);
}
VIEW3D_API void __stdcall View3D_GizmoDetach(View3DGizmo gizmo, View3DObject obj)
{
	try
	{
		if (!gizmo) throw std::runtime_error("Gizmo is null");

		DllLockGuard;
		gizmo->Detach(obj->m_o2p);
	}
	CatchAndReport(View3D_GizmoDetach, ,);
}

// Get/Set the scale factor for the gizmo
VIEW3D_API float __stdcall View3D_GizmoScaleGet(View3DGizmo gizmo)
{
	try
	{
		if (!gizmo) throw std::runtime_error("Gizmo is null");

		DllLockGuard;
		return gizmo->m_scale;
	}
	CatchAndReport(View3D_GizmoScaleGet, , 0.0f);
}
VIEW3D_API void __stdcall View3D_GizmoScaleSet(View3DGizmo gizmo, float scale)
{
	try
	{
		if (!gizmo) throw std::runtime_error("Gizmo is null");

		DllLockGuard;
		gizmo->m_scale = scale;
	}
	CatchAndReport(View3D_GizmoScaleSet, , );
}

// Get/Set the current mode of the gizmo
VIEW3D_API EView3DGizmoMode __stdcall View3D_GizmoGetMode(View3DGizmo gizmo)
{
	try
	{
		if (!gizmo) throw std::runtime_error("Gizmo is null");
		return static_cast<EView3DGizmoMode>(gizmo->Mode());
	}
	CatchAndReport(View3D_GizmoGetMode, ,static_cast<EView3DGizmoMode>(-1));
}
VIEW3D_API void __stdcall View3D_GizmoSetMode(View3DGizmo gizmo, EView3DGizmoMode mode)
{
	try
	{
		if (!gizmo) throw std::runtime_error("Gizmo is null");
		gizmo->Mode(static_cast<pr::ldr::LdrGizmo::EMode>(mode));
	}
	CatchAndReport(View3D_GizmoSetMode, ,);
}

// Get/Set the object to world transform for the gizmo
VIEW3D_API View3DM4x4 __stdcall View3D_GizmoGetO2W(View3DGizmo gizmo)
{
	try
	{
		if (!gizmo) throw std::runtime_error("Gizmo is null");
		return view3d::To<View3DM4x4>(gizmo->O2W());
	}
	CatchAndReport(View3D_GizmoGetO2W, ,View3DM4x4());
}
VIEW3D_API void __stdcall View3D_GizmoSetO2W(View3DGizmo gizmo, View3DM4x4 const& o2w)
{
	try
	{
		if (!gizmo) throw std::runtime_error("Gizmo is null");
		gizmo->O2W(view3d::To<pr::m4x4>(o2w));
	}
	CatchAndReport(View3D_GizmoSetO2W, ,);
}

// Get the offset transform that represents the difference between the gizmo's transform at the start of manipulation and now.
VIEW3D_API View3DM4x4 __stdcall View3D_GizmoGetOffset(View3DGizmo gizmo)
{
	try
	{
		if (!gizmo) throw std::runtime_error("Gizmo is null");
		return view3d::To<View3DM4x4>(gizmo->Offset());
	}
	CatchAndReport(View3D_GizmoGetOffset, ,View3DM4x4());
}

// Get/Set whether the gizmo is active to mouse interaction
VIEW3D_API BOOL __stdcall View3D_GizmoEnabled(View3DGizmo gizmo)
{
	try
	{
		if (!gizmo) throw std::runtime_error("Gizmo is null");
		return gizmo->Enabled();
	}
	CatchAndReport(View3D_GizmoEnabled, ,FALSE);
}
VIEW3D_API void __stdcall View3D_GizmoSetEnabled(View3DGizmo gizmo, BOOL enabled)
{
	try
	{
		if (!gizmo) throw std::runtime_error("Gizmo is null");
		gizmo->Enabled(enabled != 0);
	}
	CatchAndReport(View3D_GizmoSetEnabled, ,);
}

// Returns true while manipulation is in progress
VIEW3D_API BOOL __stdcall View3D_GizmoManipulating(View3DGizmo gizmo)
{
	try
	{
		if (!gizmo) throw std::runtime_error("Gizmo is null");
		return gizmo->Manipulating();
	}
	CatchAndReport(View3D_GizmoManipulating, ,FALSE);
}

// Miscellaneous **********************************************************************

// Flush any pending commands to the graphics card. Basically 'Present' but for off-screen rendering
VIEW3D_API void __stdcall View3D_Flush()
{
	try
	{
		pr::Renderer::Lock lock(Dll().m_rdr);
		lock.ImmediateDC()->Flush();
	}
	CatchAndReport(View3D_Flush, , );
}

// Handle standard keyboard shortcuts
VIEW3D_API BOOL __stdcall View3D_TranslateKey(View3DWindow window, int key_code)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		switch (key_code)
		{
		case VK_F7:
			{
				auto up = View3D_CameraAlignAxisGet(window);
				if (pr::Length3Sq(up) == 0)
					up = {0.0f, 1.0f, 0.0f, 0.0f};

				auto forward = up.z > up.y ? View3DV4{0.0f, 1.0f, 0.0f, 0.0f} : View3DV4{0.0f, 0.0f, -1.0f, 0.0f};

				View3D_ResetView(window, forward, up, 0, TRUE, TRUE);
				View3D_Invalidate(window, FALSE);
				return TRUE;
			}
		case VK_SPACE:
			{
				View3D_ObjectManagerShow(window, true);
				return TRUE;
			}
		case 'W':
			{
				if (pr::KeyDown(VK_CONTROL))
				{
					switch (View3D_FillModeGet(window)) {
					case EView3DFillMode::Solid:     View3D_FillModeSet(window, EView3DFillMode::Wireframe); break;
					case EView3DFillMode::Wireframe: View3D_FillModeSet(window, EView3DFillMode::SolidWire); break;
					case EView3DFillMode::SolidWire: View3D_FillModeSet(window, EView3DFillMode::Solid); break;
					}
					View3D_Invalidate(window, FALSE);
				}
				return TRUE;
			}
		}
		return FALSE;
	}
	CatchAndReport(View3D_TranslateKey, window, FALSE);
}

// Returns true if the depth buffer is enabled
VIEW3D_API BOOL __stdcall View3D_DepthBufferEnabledGet(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->m_scene.m_dsb.Desc().DepthEnable;
	}
	CatchAndReport(View3D_DepthBufferEnabledGet, window, TRUE);
}

// Enables or disables the depth buffer
VIEW3D_API void __stdcall View3D_DepthBufferEnabledSet(View3DWindow window, BOOL enabled)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_scene.m_dsb.Set(EDS::DepthEnable, enabled);
	}
	CatchAndReport(View3D_DepthBufferEnabledSet, window,);
}

// Return true if the focus point is visible
VIEW3D_API BOOL __stdcall View3D_FocusPointVisibleGet(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->m_focus_point_visible;
	}
	CatchAndReport(View3D_FocusPointVisibleGet, window, false);
}

// Add the focus point to a window
VIEW3D_API void __stdcall View3D_FocusPointVisibleSet(View3DWindow window, BOOL show)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_focus_point_visible = show != 0;
	}
	CatchAndReport(View3D_FocusPointVisibleSet, window,);
}

// Set the size of the focus point
VIEW3D_API void __stdcall View3D_FocusPointSizeSet(View3DWindow window, float size)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_focus_point_size = size;
	}
	CatchAndReport(View3D_FocusPointSizeSet, window,);
}

// Return true if the origin is visible
VIEW3D_API BOOL __stdcall View3D_OriginVisibleGet(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->m_origin_point_visible;
	}
	CatchAndReport(View3D_OriginVisibleGet, window, false);
}

// Add the focus point to a window
VIEW3D_API void __stdcall View3D_OriginVisibleSet(View3DWindow window, BOOL show)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_origin_point_visible = show != 0;
	}
	CatchAndReport(View3D_OriginVisibleSet, window,);
}

// Set the size of the focus point
VIEW3D_API void __stdcall View3D_OriginSizeSet(View3DWindow window, float size)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_origin_point_size = size;
	}
	CatchAndReport(View3D_OriginSizeSet, window,);
}

// Get/Set whether object bounding boxes are visible
VIEW3D_API BOOL __stdcall View3D_BBoxesVisibleGet(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->m_bboxes_visible;
	}
	CatchAndReport(View3D_BBoxesVisibleGet, window, FALSE);
}
VIEW3D_API void __stdcall View3D_BBoxesVisibleSet(View3DWindow window, BOOL visible)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_bboxes_visible = visible != 0;
	}
	CatchAndReport(View3D_BBoxesVisibleSet, window, );
}

// Get/Set the visibility of the selection box
VIEW3D_API BOOL __stdcall View3D_SelectionBoxVisibleGet(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return window->m_selection_box_visible;
	}
	CatchAndReport(View3D_SelectionBoxVisibleGet, window, FALSE);
}
VIEW3D_API void __stdcall View3D_SelectionBoxVisibleSet(View3DWindow window, BOOL visible)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->m_selection_box_visible = visible != 0;
	}
	CatchAndReport(View3D_SelectionBoxVisibleSet, window, );
}

// Set the position and size of the selection box
VIEW3D_API void __stdcall View3D_SelectionBoxPosition(View3DWindow window, View3DBBox const& bbox, View3DM4x4 const& o2w)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->SetSelectionBox(view3d::To<pr::BBox>(bbox), view3d::To<pr::m4x4>(o2w).rot);
	}
	CatchAndReport(View3D_SelectionBoxPosition, window, );
}

// Set the position of the selection box
VIEW3D_API void __stdcall View3D_SelectionBoxFitToSelected(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->SelectionBoxFitToSelected();
	}
	CatchAndReport(View3D_SelectionBoxFitToSelected, window, );
}

// Create a scene showing the capabilities of view3d (actually of ldr_object_manager)
VIEW3D_API GUID __stdcall View3D_DemoSceneCreate(View3DWindow window)
{
	using namespace pr::script;
	using namespace pr::ldr;
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		return Dll().CreateDemoScene(window);
	}
	CatchAndReport(View3D_DemoSceneCreate, window, Context::GuidDemoSceneObjects);
}

// Delete all objects belonging to the demo scene
VIEW3D_API void __stdcall View3D_DemoSceneDelete()
{
	try
	{
		DllLockGuard;
		Dll().DeleteAllObjectsById(&Context::GuidDemoSceneObjects, 1, 0);
	}
	CatchAndReport(View3D_DemoSceneDelete,,);
}

// Return the example Ldr script as a BSTR
VIEW3D_API BSTR __stdcall View3D_ExampleScriptBStr()
{
	try
	{
		DllLockGuard;
		auto example = pr::ldr::CreateDemoScene();
		return ::SysAllocStringLen(example.c_str(), UINT(example.size()));
	}
	CatchAndReport(View3D_ExampleScriptBStr,,BSTR());
}

// Show a window containing the demo scene script
VIEW3D_API void __stdcall View3D_DemoScriptShow(View3DWindow window)
{
	try
	{
		if (!window) throw std::runtime_error("window is null");

		DllLockGuard;
		window->EditorUI().Show();
		window->EditorUI().Text(pr::ldr::CreateDemoScene().c_str());
	}
	CatchAndReport(View3D_DemoScriptShow, window,);
}

// Parse an ldr *o2w {} description returning the transform
VIEW3D_API View3DM4x4 __stdcall View3D_ParseLdrTransform(char const* ldr_script)
{
	try
	{
		pr::script::PtrA src(ldr_script);
		pr::script::Reader reader(src);
		
		pr::m4x4 o2w;
		reader.TransformS(o2w);
		return view3d::To<View3DM4x4>(o2w);
	}
	CatchAndReport(View3D_ParseLdrTransform, , view3d::To<View3DM4x4>(pr::m4x4Identity));
}

// Return the current ref count of a COM pointer
VIEW3D_API ULONG __stdcall View3D_RefCount(IUnknown* pointer)
{
	try
	{
		if (pointer == nullptr) throw std::runtime_error("pointer is null");
		return pr::rdr::RefCount(pointer);
	}
	CatchAndReport(View3D_RefCount, , 0);
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
		auto editor = std::make_unique<pr::ldr::ScriptEditorUI>(parent);
		HWND hwnd = *editor;
		::SetLastError(0);
		auto prev = ::SetWindowLongPtrA(hwnd, GWLP_USERDATA, LONG_PTR(editor.get()));
		if (prev != 0 || ::GetLastError() != 0) throw std::runtime_error("Error while creating editor window");
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

		EditorPtr edt(reinterpret_cast<pr::ldr::ScriptEditorUI*>(::GetWindowLongPtrA(hwnd, GWLP_USERDATA)));
		if (!edt) throw std::runtime_error("No back reference pointer found for this window");
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
		if (!scintilla_control) throw std::runtime_error("scintilla control handle is null");

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

static_assert(int(EView3DFlags::None              ) == int(pr::ldr::ELdrFlags::None              ));
static_assert(int(EView3DFlags::Hidden            ) == int(pr::ldr::ELdrFlags::Hidden            ));
static_assert(int(EView3DFlags::Wireframe         ) == int(pr::ldr::ELdrFlags::Wireframe         ));
static_assert(int(EView3DFlags::NoZTest           ) == int(pr::ldr::ELdrFlags::NoZTest           ));
static_assert(int(EView3DFlags::NoZWrite          ) == int(pr::ldr::ELdrFlags::NoZWrite          ));
static_assert(int(EView3DFlags::Selected          ) == int(pr::ldr::ELdrFlags::Selected          ));
static_assert(int(EView3DFlags::BBoxExclude       ) == int(pr::ldr::ELdrFlags::BBoxExclude       ));
static_assert(int(EView3DFlags::SceneBoundsExclude) == int(pr::ldr::ELdrFlags::SceneBoundsExclude));
static_assert(int(EView3DFlags::HitTestExclude    ) == int(pr::ldr::ELdrFlags::HitTestExclude    ));

static_assert(int(EView3DGeom::Unknown) == int(pr::rdr::EGeom::Invalid));
static_assert(int(EView3DGeom::Vert   ) == int(pr::rdr::EGeom::Vert   ));
static_assert(int(EView3DGeom::Colr   ) == int(pr::rdr::EGeom::Colr   ));
static_assert(int(EView3DGeom::Norm   ) == int(pr::rdr::EGeom::Norm   ));
static_assert(int(EView3DGeom::Tex0   ) == int(pr::rdr::EGeom::Tex0   ));

static_assert(int(EView3DGizmoEvent::StartManip) == int(pr::ldr::ELdrGizmoEvent::StartManip));
static_assert(int(EView3DGizmoEvent::Moving    ) == int(pr::ldr::ELdrGizmoEvent::Moving    ));
static_assert(int(EView3DGizmoEvent::Commit    ) == int(pr::ldr::ELdrGizmoEvent::Commit    ));
static_assert(int(EView3DGizmoEvent::Revert    ) == int(pr::ldr::ELdrGizmoEvent::Revert    ));

static_assert(int(EView3DNavOp::None     ) == int(pr::camera::ENavOp::None     ));
static_assert(int(EView3DNavOp::Translate) == int(pr::camera::ENavOp::Translate));
static_assert(int(EView3DNavOp::Rotate   ) == int(pr::camera::ENavOp::Rotate   ));
static_assert(int(EView3DNavOp::Zoom     ) == int(pr::camera::ENavOp::Zoom     ));

static_assert(int(EView3DColourOp::Overwrite) == int(pr::ldr::EColourOp::Overwrite));
static_assert(int(EView3DColourOp::Add      ) == int(pr::ldr::EColourOp::Add      ));
static_assert(int(EView3DColourOp::Subtract ) == int(pr::ldr::EColourOp::Subtract ));
static_assert(int(EView3DColourOp::Multiply ) == int(pr::ldr::EColourOp::Multiply ));
static_assert(int(EView3DColourOp::Lerp     ) == int(pr::ldr::EColourOp::Lerp     ));

static_assert(int(EView3DCameraLockMask::None          ) == int(pr::camera::ELockMask::None          ));
static_assert(int(EView3DCameraLockMask::TransX        ) == int(pr::camera::ELockMask::TransX        ));
static_assert(int(EView3DCameraLockMask::TransY        ) == int(pr::camera::ELockMask::TransY        ));
static_assert(int(EView3DCameraLockMask::TransZ        ) == int(pr::camera::ELockMask::TransZ        ));
static_assert(int(EView3DCameraLockMask::RotX          ) == int(pr::camera::ELockMask::RotX          ));
static_assert(int(EView3DCameraLockMask::RotY          ) == int(pr::camera::ELockMask::RotY          ));
static_assert(int(EView3DCameraLockMask::RotZ          ) == int(pr::camera::ELockMask::RotZ          ));
static_assert(int(EView3DCameraLockMask::Zoom          ) == int(pr::camera::ELockMask::Zoom          ));
static_assert(int(EView3DCameraLockMask::CameraRelative) == int(pr::camera::ELockMask::CameraRelative));
static_assert(int(EView3DCameraLockMask::All           ) == int(pr::camera::ELockMask::All           ));

static_assert(int(EView3DPrim::Invalid  ) == int(pr::rdr::EPrim::Invalid  ));
static_assert(int(EView3DPrim::PointList) == int(pr::rdr::EPrim::PointList));
static_assert(int(EView3DPrim::LineList ) == int(pr::rdr::EPrim::LineList ));
static_assert(int(EView3DPrim::LineStrip) == int(pr::rdr::EPrim::LineStrip));
static_assert(int(EView3DPrim::TriList  ) == int(pr::rdr::EPrim::TriList  ));
static_assert(int(EView3DPrim::TriStrip ) == int(pr::rdr::EPrim::TriStrip ));

static_assert(int(EView3DLight::Ambient    ) == int(pr::rdr::ELight::Ambient    ));
static_assert(int(EView3DLight::Directional) == int(pr::rdr::ELight::Directional));
static_assert(int(EView3DLight::Point      ) == int(pr::rdr::ELight::Point      ));
static_assert(int(EView3DLight::Spot       ) == int(pr::rdr::ELight::Spot       ));
	
// EView3DLogLevel - unused?

static_assert(int(EView3DUpdateObject::None      ) == int(pr::ldr::EUpdateObject::None      ));
static_assert(int(EView3DUpdateObject::All       ) == int(pr::ldr::EUpdateObject::All       ));
static_assert(int(EView3DUpdateObject::Name      ) == int(pr::ldr::EUpdateObject::Name      ));
static_assert(int(EView3DUpdateObject::Model     ) == int(pr::ldr::EUpdateObject::Model     ));
static_assert(int(EView3DUpdateObject::Transform ) == int(pr::ldr::EUpdateObject::Transform ));
static_assert(int(EView3DUpdateObject::Children  ) == int(pr::ldr::EUpdateObject::Children  ));
static_assert(int(EView3DUpdateObject::Colour    ) == int(pr::ldr::EUpdateObject::Colour    ));
static_assert(int(EView3DUpdateObject::ColourMask) == int(pr::ldr::EUpdateObject::ColourMask));
static_assert(int(EView3DUpdateObject::Flags     ) == int(pr::ldr::EUpdateObject::Flags     ));
static_assert(int(EView3DUpdateObject::Animation ) == int(pr::ldr::EUpdateObject::Animation ));

static_assert(int(EView3DGizmoEvent::StartManip) == int(pr::ldr::ELdrGizmoEvent::StartManip));
static_assert(int(EView3DGizmoEvent::Moving    ) == int(pr::ldr::ELdrGizmoEvent::Moving    ));
static_assert(int(EView3DGizmoEvent::Commit    ) == int(pr::ldr::ELdrGizmoEvent::Commit    ));
static_assert(int(EView3DGizmoEvent::Revert    ) == int(pr::ldr::ELdrGizmoEvent::Revert    ));

static_assert(int(EView3DGizmoMode::Translate) == int(pr::ldr::LdrGizmo::EMode::Translate));
static_assert(int(EView3DGizmoMode::Rotate   ) == int(pr::ldr::LdrGizmo::EMode::Rotate   ));
static_assert(int(EView3DGizmoMode::Scale    ) == int(pr::ldr::LdrGizmo::EMode::Scale    ));

static_assert(int(EView3DSourcesChangedReason::NewData) == int(pr::ldr::ScriptSources::EReason::NewData));
static_assert(int(EView3DSourcesChangedReason::Reload ) == int(pr::ldr::ScriptSources::EReason::Reload ));

static_assert(int(EView3DHitTestFlags::Faces) == int(pr::rdr::EHitTestFlags::Faces));
static_assert(int(EView3DHitTestFlags::Edges) == int(pr::rdr::EHitTestFlags::Edges));
static_assert(int(EView3DHitTestFlags::Verts) == int(pr::rdr::EHitTestFlags::Verts));

static_assert(int(EView3DSnapType::NoSnap    ) == int(pr::rdr::ESnapType::NoSnap    ));
static_assert(int(EView3DSnapType::Vert      ) == int(pr::rdr::ESnapType::Vert      ));
static_assert(int(EView3DSnapType::EdgeMiddle) == int(pr::rdr::ESnapType::EdgeMiddle));
static_assert(int(EView3DSnapType::FaceCentre) == int(pr::rdr::ESnapType::FaceCentre));
static_assert(int(EView3DSnapType::Edge      ) == int(pr::rdr::ESnapType::Edge      ));
static_assert(int(EView3DSnapType::Face      ) == int(pr::rdr::ESnapType::Face      ));

// Specifically used to avoid alignment problems
static_assert(sizeof(View3DV2    ) == sizeof(pr::v2       ));
static_assert(sizeof(View3DV4    ) == sizeof(pr::v4       ));
static_assert(sizeof(View3DM4x4  ) == sizeof(pr::m4x4     ));
static_assert(sizeof(View3DBBox  ) == sizeof(pr::BBox     ));
// View3DVertex - only used in this file
// View3DImageInfo - only used in this file
// View3DLight - only used in this file
// View3DTextureOptions - only used in this file
// View3DUpdateModelKeep - only used in this file
// View3DMaterial - only used in this file
// View3DViewport - only used in this file
static_assert(equal_size_and_alignment<View3DGizmoEvent, pr::ldr::Evt_Gizmo>::value);

#pragma endregion