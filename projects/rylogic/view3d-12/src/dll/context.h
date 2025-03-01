//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/main/renderer.h"
#include "pr/view3d-12/ldraw/ldraw_sources.h"
#include "view3d-12/src/dll/dll_forward.h"

namespace pr::rdr12
{
	struct Context
	{
		using InitSet = std::unordered_set<view3d::DllHandle>;
		using WindowCont = std::vector<V3dWindow*>;
		using ScriptSources = pr::rdr12::ldraw::ScriptSources;

		inline static Guid const GuidDemoSceneObjects = { 0xFE51C164, 0x9E57, 0x456F, 0x9D, 0x8D, 0x39, 0xE3, 0xFA, 0xAF, 0xD3, 0xE7 };

		Renderer             m_rdr;             // The renderer
		WindowCont           m_wnd_cont;        // The created windows
		ScriptSources        m_sources;         // A container of Ldr objects and a file watcher
		InitSet              m_inits;           // A unique id assigned to each Initialise call
		std::recursive_mutex m_mutex;

		Context(HINSTANCE instance, StaticCB<view3d::ReportErrorCB> global_error_cb);
		Context(Context&&) = delete;
		Context(Context const&) = delete;
		Context& operator=(Context&) = delete;
		Context& operator=(Context const&) = delete;
		~Context();
		
		// Access functions
		Context* This() { return this; }
		Renderer& rdr() { return m_rdr; }

		// Report an error handled at the DLL API layer
		void ReportAPIError(char const* func_name, view3d::Window wnd, std::exception const* ex);

		// Create/Destroy windows
		V3dWindow* WindowCreate(HWND hwnd, view3d::WindowOptions const& opts);
		void WindowDestroy(V3dWindow* window);

		// Global error callback. Can be called in a worker thread context
		MultiCast<StaticCB<view3d::ReportErrorCB>, true> ReportError;

		// Event raised when script sources are parsed during adding/updating
		MultiCast<StaticCB<view3d::AddFileProgressCB>, true> OnAddFileProgress;

		// Event raised when the script sources are updated
		MultiCast<StaticCB<view3d::SourcesChangedCB>, true> OnSourcesChanged;

		// Load/Add ldr objects from a script file. Returns the Guid of the context that the objects were added to.
		Guid LoadScriptFile(std::filesystem::path ldr_script, EEncoding enc, Guid const* context_id, PathResolver const& includes, ScriptSources::OnAddCB on_add);

		// Load/Add ldr objects from a script string or file. Returns the Guid of the context that the objects were added to.
		template <typename Char>
		Guid LoadScriptString(std::basic_string_view<Char> ldr_script, EEncoding enc, Guid const* context_id, PathResolver const& includes, ScriptSources::OnAddCB on_add);

		// Create an object from geometry
		LdrObject* ObjectCreate(char const* name, Colour32 colour, std::span<view3d::Vertex const> verts, std::span<uint16_t const> indices, std::span<view3d::Nugget const> nuggets, Guid const& context_id);

		// Load/Add ldr objects and return the first object from the script
		template <typename Char>
		LdrObject* ObjectCreateLdr(std::basic_string_view<Char> ldr_script, bool file, EEncoding enc, Guid const* context_id, view3d::Includes const* includes);

		// Create an LdrObject from the p3d model
		LdrObject* ObjectCreateP3D(char const* name, Colour32 colour, std::filesystem::path const& p3d_filepath, Guid const* context_id);
		LdrObject* ObjectCreateP3D(char const* name, Colour32 colour, std::span<std::byte const> p3d_data, Guid const* context_id);

		// Modify an ldr object using a callback to populate the model data.
		LdrObject* ObjectCreateByCallback(char const* name, Colour32 colour, int vcount, int icount, int ncount, StaticCB<view3d::EditObjectCB> edit_cb, Guid const& context_id);
		void ObjectEdit(LdrObject* object, StaticCB<view3d::EditObjectCB> edit_cb);

		// Update the model in an existing object
		template <typename Char>
		void UpdateObject(LdrObject* object, std::basic_string_view<Char> ldr_script, ldraw::EUpdateObject flags);

		// Delete a single object
		void DeleteObject(LdrObject* object);

		// Delete all objects
		void DeleteAllObjects();

		// Delete all objects with matching ids
		void DeleteAllObjectsById(Guid const* context_ids, int include_count, int exclude_count);
		
		// Delete all objects not displayed in any windows
		void DeleteUnused(Guid const* context_ids, int include_count, int exclude_count);

		// Enumerate the GUIDs in the sources collection
		void SourceEnumGuids(StaticCB<bool, GUID const&> enum_guids_cb);

		// Create a gizmo object and add it to the gizmo collection
		LdrGizmo* GizmoCreate(ldraw::EGizmoMode mode, m4x4 const& o2w);

		// Destroy a gizmo
		void GizmoDelete(LdrGizmo* gizmo);
		
		// Reload file sources
		void ReloadScriptSources();

		// Poll for changed script source files, and reload any that have changed
		void CheckForChangedSources();

		// Return the context id for objects created from 'filepath' (if filepath is an existing source)
		Guid const* ContextIdFromFilepath(char const* filepath) const;
	};
}
