//***************************************************************************************************
// View 3D
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************
#pragma once

#include "view3d/forward.h"

namespace view3d
{
	// Global data for this dll
	struct Context :pr::AlignTo<16>
	{
		using InitSet = std::unordered_set<View3DContext>;
		using WindowCont = std::vector<std::unique_ptr<Window>>;
		struct EmbCodeCB { int m_lang; EmbeddedCodeHandlerCB m_cb; };
		using EmbCodeCBCont = pr::vector<EmbCodeCB>;
		using IEmbeddedCode = pr::script::IEmbeddedCode;
		static pr::Guid const GuidDemoSceneObjects;

		InitSet                  m_inits;         // A unique id assigned to each Initialise call
		bool                     m_compatible;    // True if the renderer will work on this system
		pr::Renderer             m_rdr;           // The renderer
		WindowCont               m_wnd_cont;      // The created windows
		pr::ldr::ScriptSources   m_sources;       // A container of Ldr objects and a file watcher
		EmbCodeCBCont            m_emb;           // Embedded code execution callbacks
		std::recursive_mutex     m_mutex;

		explicit Context(HINSTANCE instance, D3D11_CREATE_DEVICE_FLAG device_flags);

		Context(Context const&) = delete;
		Context& operator=(Context const&) = delete;
		Context* This() { return this; }

		// Report an error to the global error handler
		void ReportError(wchar_t const* msg);

		// Error event. Can be called in a worker thread context
		pr::MultiCast<ReportErrorCB> OnError;

		// Event raised when script sources are parsed during adding/updating
		pr::MultiCast<AddFileProgressCB> OnAddFileProgress;

		// Event raised when the script sources are updated
		pr::MultiCast<SourcesChangedCB> OnSourcesChanged;

		// Create/Destroy windows
		Window* WindowCreate(HWND hwnd, View3DWindowOptions const& opts);
		void WindowDestroy(Window* window);

		// Load/Add a script source. Returns the Guid of the context that the objects were added to.
		pr::Guid LoadScriptSource(std::filesystem::path const& filepath, pr::EEncoding enc, bool additional, pr::script::Includes const& includes);

		// Load/Add ldr objects from a script string. Returns the Guid of the context that the objects were added to.
		pr::Guid LoadScript(std::wstring_view ldr_script, bool file, pr::EEncoding enc, pr::Guid const* context_id, pr::script::Includes const& includes);
		pr::Guid LoadScript(std::string_view ldr_script, bool file, pr::EEncoding enc, pr::Guid const* context_id, pr::script::Includes const& includes);

		// Load/Add ldr objects and return the first object from the script
		pr::ldr::LdrObject* ObjectCreateLdr(std::wstring_view ldr_script, bool file, pr::EEncoding enc, pr::Guid const* context_id, pr::script::Includes const& includes);

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
