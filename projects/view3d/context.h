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
		using InitSet = std::set<View3DContext>;

		InitSet                   m_inits;      // A unique id assigned to each Initialise call
		bool                      m_compatible; // True if the renderer will work on this system
		pr::Renderer              m_rdr;        // The renderer
		WindowCont                m_wnd_cont;   // The created windows
		pr::ldr::ScriptSources    m_sources;    // A container of Ldr objects and a file watcher
		pr::script::EmbeddedLua<> m_lua;
		std::recursive_mutex      m_mutex;

		explicit Context(HINSTANCE instance)
			:m_inits()
			,m_compatible(pr::rdr::TestSystemCompatibility())
			,m_rdr(pr::rdr::RdrSettings(instance, FALSE))
			,m_wnd_cont()
			,m_sources(m_rdr, &m_lua)
			,m_lua()
			,m_mutex()
		{
			PR_ASSERT(PR_DBG, pr::meta::is_aligned_to<16>(this), "dll data not aligned");

			// Hook up the sources events
			m_sources.OnAddFileProgress += [&](pr::ldr::ScriptSources&, pr::ldr::ScriptSources::AddFileProgressEventArgs& args)
			{
				auto context_id  = args.m_context_id;
				auto filepath    = args.m_loc.StreamName();
				auto file_offset = args.m_loc.Pos();
				auto complete    = args.m_complete;
				OnAddFileProgress.Raise(args.m_cancel, context_id, filepath.c_str(), file_offset, complete);
			};
			m_sources.OnReload += [&](pr::ldr::ScriptSources&, pr::ldr::ScriptSources::ReloadEventArgs const& args)
			{
				OnSourcesChanged.Raise(static_cast<ESourcesChangedReason>(args.m_reason), true);
			};
			m_sources.OnStoreChanged += [&](pr::ldr::ScriptSources&, pr::ldr::ScriptSources::StoreChangedEventArgs const& args)
			{
				OnSourcesChanged.Raise(static_cast<ESourcesChangedReason>(args.m_reason), false);
			};
			m_sources.OnError += [&](pr::ldr::ScriptSources&, pr::ErrorEventArgs const& args)
			{
				ReportError(args.m_msg.c_str());
			};
		}
		~Context()
		{
			while (!m_wnd_cont.empty())
				View3D_DestroyWindow(*m_wnd_cont.begin());
		}
		Context(Context const&) = delete;
		Context& operator=(Context const&) = delete;

		// Error event. Can be called in a worker thread context
		pr::MultiCast<ReportErrorCB> OnError;

		// Event raised when script sources are parsed during adding/updating
		pr::MultiCast<AddFileProgressCB> OnAddFileProgress;

		// Event raised when the script sources are updated
		pr::MultiCast<SourcesChangedCB> OnSourcesChanged;

		// Report an error to the global error handler
		void ReportError(wchar_t const* msg)
		{
			OnError.Raise(msg);
		}

		// Remove all Ldr script sources
		void ClearScriptSources()
		{
			// Reset the sources collection
			m_sources.Clear();
		}

		// Load/Add a script source
		pr::Guid LoadScriptSource(wchar_t const* filepath, bool additional, pr::script::Includes<> const& includes)
		{
			// Note: this can be called from a worker thread
			return m_sources.AddFile(filepath, includes, additional);
		}

		// Load/Add ldr objects from a script string
		pr::Guid LoadScript(wchar_t const* ldr_script, bool file, pr::Guid const* context_id, pr::script::Includes<> const& includes)
		{
			// Note: this can be called from a worker thread
			return m_sources.AddScript(ldr_script, file, context_id, includes);
		}

		// Reload script source files
		void ReloadScriptSources()
		{
			// Remove script source objects from all windows
			for (auto src : m_sources.Files())
			{
				auto id = src.second.m_file_group_id;
				DeleteAllObjectsById(id);
			}

			// Reload the source data
			m_sources.Reload();
		}

		// Poll for changed script source files, and reload any that have changed
		void CheckForChangedSources()
		{
			m_sources.RefreshChangedFiles();
		}

		// Delete all objects
		void DeleteAllObjects()
		{
			// Remove the objects from any windows they're in
			for (auto wnd : m_wnd_cont)
				View3D_RemoveAllObjects(wnd);
		
			// Clear the object container. The unique pointers should delete the objects
			m_sources.ClearAll();
		}

		// Delete all objects with matching ids
		void DeleteAllObjectsById(pr::Guid const& context_id)
		{
			// Remove objects from any windows they might be assigned to
			for (auto wnd : m_wnd_cont)
				View3D_RemoveObjectsById(wnd, false, context_id);

			m_sources.Remove(context_id);
		}

		// Delete a single object
		void DeleteObject(pr::ldr::LdrObject* object)
		{
			// Remove the object from any windows it's in
			for (auto wnd : m_wnd_cont)
				View3D_RemoveObject(wnd, object);
		
			// Delete the object from the object container
			m_sources.Remove(object);
		}

		// Create a gizmo object and add it to the gizmo collection
		pr::ldr::LdrGizmo* CreateGizmo(pr::ldr::LdrGizmo::EMode mode, pr::m4x4 const& o2w)
		{
			return m_sources.CreateGizmo(mode, o2w);
		}

		// Destroy a gizmo
		void DeleteGizmo(pr::ldr::LdrGizmo* gizmo)
		{
			// Remove the gizmo from any windows it's in
			for (auto wnd : m_wnd_cont)
				View3D_RemoveGizmo(wnd, gizmo);
		
			// Delete the gizmo from the sources
			m_sources.RemoveGizmo(gizmo);
		}
	};
}
