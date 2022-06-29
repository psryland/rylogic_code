//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "view3d-12/src/dll/dll_forward.h"
#include "pr/view3d-12/main/renderer.h"
#include "pr/view3d-12/ldraw/ldr_sources.h"

namespace pr::rdr12
{
	struct Context
	{
		using InitSet = std::unordered_set<view3d::DllHandle>;
		using WindowCont = std::vector<V3dWindow*>;
		using EmbCodeCB = struct { int m_lang; EmbeddedCodeHandlerCB m_cb; };
		using EmbCodeCBCont = std::vector<EmbCodeCB>;
		using IEmbeddedCode = pr::script::IEmbeddedCode;

		inline static Guid const GuidDemoSceneObjects = { 0xFE51C164, 0x9E57, 0x456F, 0x9D, 0x8D, 0x39, 0xE3, 0xFA, 0xAF, 0xD3, 0xE7 };

		Renderer             m_rdr;             // The renderer
		WindowCont           m_wnd_cont;        // The created windows
		ScriptSources        m_sources;         // A container of Ldr objects and a file watcher
		InitSet              m_inits;           // A unique id assigned to each Initialise call
		EmbCodeCBCont        m_emb;             // Embedded code execution callbacks
		std::recursive_mutex m_mutex;

		Context(HINSTANCE instance, ReportErrorCB global_error_cb);
		Context(Context&&) = delete;
		Context(Context const&) = delete;
		Context& operator=(Context&) = delete;
		Context& operator=(Context const&) = delete;
		~Context();
		
		Context* This() { return this; }

		// Report an error handled at the DLL API layer
		void ReportAPIError(char const* func_name, view3d::Window wnd, std::exception const* ex);

		// Create/Destroy windows
		V3dWindow* WindowCreate(HWND hwnd, view3d::WindowOptions const& opts);
		void WindowDestroy(V3dWindow* window);

		// Global error callback. Can be called in a worker thread context
		MultiCast<ReportErrorCB, true> ReportError;

		// Event raised when script sources are parsed during adding/updating
		MultiCast<AddFileProgressCB, true> OnAddFileProgress;

		// Event raised when the script sources are updated
		MultiCast<SourcesChangedCB, true> OnSourcesChanged;

		// Load/Add ldr objects from a script string or file. Returns the Guid of the context that the objects were added to.
		template <typename Char>
		Guid LoadScript(std::basic_string_view<Char> ldr_script, bool file, EEncoding enc, Guid const* context_id, script::Includes const& includes, OnAddCB on_add);

		// Load/Add ldr objects and return the first object from the script
		template <typename Char>
		LdrObject* ObjectCreateLdr(std::basic_string_view<Char> ldr_script, bool file, EEncoding enc, Guid const* context_id, view3d::Includes const* includes);

		// Delete a single object
		void DeleteObject(LdrObject* object);

		// Delete all objects
		void DeleteAllObjects();

		// Delete all objects with matching ids
		void DeleteAllObjectsById(Guid const* context_ids, int include_count, int exclude_count);

		// Create an embedded code handler for the given language
		std::unique_ptr<IEmbeddedCode> CreateHandler(wchar_t const* lang);
	};
}
