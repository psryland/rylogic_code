//***************************************************************************************************
// View 3D
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************
#pragma once

#include "view3d/dll/forward.h"

namespace view3d
{
	// Global data for this dll
	struct Context :pr::AlignTo<16>
	{
		using InitSet = std::unordered_set<View3DContext>;
		using WindowCont = std::vector<std::unique_ptr<Window>>;
		using EmbCodeCB = struct { int m_lang; EmbeddedCodeHandlerCB m_cb; };
		using EmbCodeCBCont = pr::vector<EmbCodeCB>;
		using IEmbeddedCode = pr::script::IEmbeddedCode;
		using ScriptSources = pr::ldr::ScriptSources;
		using Includes = pr::script::Includes;

		static pr::Guid const GuidDemoSceneObjects;

		InitSet              m_inits;           // A unique id assigned to each Initialise call
		pr::Renderer         m_rdr;             // The renderer
		WindowCont           m_wnd_cont;        // The created windows
		ScriptSources        m_sources;         // A container of Ldr objects and a file watcher
		EmbCodeCBCont        m_emb;             // Embedded code execution callbacks
		std::recursive_mutex m_mutex;

		explicit Context(HINSTANCE instance, ReportErrorCB global_error_cb, D3D11_CREATE_DEVICE_FLAG device_flags);

		Context(Context const&) = delete;
		Context& operator=(Context const&) = delete;
		Context* This() { return this; }

		// Global error callback. Can be called in a worker thread context
		pr::MultiCast<ReportErrorCB, true> ReportError;

		// Event raised when script sources are parsed during adding/updating
		pr::MultiCast<AddFileProgressCB, true> OnAddFileProgress;

		// Event raised when the script sources are updated
		pr::MultiCast<SourcesChangedCB, true> OnSourcesChanged;

		// Create/Destroy windows
		Window* WindowCreate(HWND hwnd, View3DWindowOptions const& opts);
		void WindowDestroy(Window* window);

		// Report an error handled at the DLL API layer
		void ReportAPIError(char const* func_name, View3DWindow wnd, std::exception const* ex);

		// Load/Add ldr objects from a script string or file. Returns the Guid of the context that the objects were added to.
		pr::Guid LoadScript(std::wstring_view ldr_script, bool file, pr::EEncoding enc, pr::Guid const* context_id, Includes const& includes, OnAddCB on_add);
		pr::Guid LoadScript(std::string_view ldr_script, bool file, pr::EEncoding enc, pr::Guid const* context_id, Includes const& includes, OnAddCB on_add);

		// Load/Add ldr objects and return the first object from the script
		pr::ldr::LdrObject* ObjectCreateLdr(std::wstring_view ldr_script, bool file, pr::EEncoding enc, pr::Guid const* context_id, Includes const& includes);

		// Create an object from geometry
		pr::ldr::LdrObject* ObjectCreate(char const* name, pr::Colour32 colour, int vcount, int icount, int ncount, View3DVertex const* verts, pr::uint16 const* indices, View3DNugget const* nuggets, pr::Guid const& context_id);

		// Reload file sources
		void ReloadScriptSources();

		// Poll for changed script source files, and reload any that have changed
		void CheckForChangedSources();

		// Edit the geometry of a model after it has been allocated
		void EditObject(pr::ldr::LdrObject* object, View3D_EditObjectCB edit_cb, void* ctx);

		// Update the model in an existing object
		void UpdateObject(pr::ldr::LdrObject* object, wchar_t const* ldr_script, pr::ldr::EUpdateObject flags);

		// Delete all objects
		void DeleteAllObjects();

		// Delete all objects with matching ids
		void DeleteAllObjectsById(pr::Guid const* context_ids, int include_count, int exclude_count);

		// Delete all objects not displayed in any windows
		void DeleteUnused(GUID const* context_ids, int include_count, int exclude_count);

		// Delete a single object
		void DeleteObject(pr::ldr::LdrObject* object);

		// Return the context id for objects created from 'filepath' (if filepath is an existing source)
		pr::Guid const* ContextIdFromFilepath(wchar_t const* filepath);

		// Enumerate the Guids in the sources collection
		void SourceEnumGuids(View3D_EnumGuidsCB enum_guids_cb, void* ctx);

		// Create a gizmo object and add it to the gizmo collection
		pr::ldr::LdrGizmo* CreateGizmo(pr::ldr::LdrGizmo::EMode mode, pr::m4x4 const& o2w);

		// Destroy a gizmo
		void DeleteGizmo(pr::ldr::LdrGizmo* gizmo);

		// Callback function called from pr::ldr::CreateEditCB to populate the model data
		struct ObjectEditCBData { View3D_EditObjectCB edit_cb; void* ctx; };
		void static __stdcall ObjectEditCB(pr::rdr::Model* model, void* ctx, pr::Renderer&);

		// Create the demo scene objects
		pr::Guid CreateDemoScene(Window* window);

		// Create an embedded code handler for the given language
		std::unique_ptr<IEmbeddedCode> CreateHandler(wchar_t const* lang);

		// Add an embedded code handler for 'lang'
		void SetEmbeddedCodeHandler(wchar_t const* lang, View3D_EmbeddedCodeHandlerCB embedded_code_cb, void* ctx, bool add);
	};
}
