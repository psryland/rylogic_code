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
		ErrorCBStack              m_error_cb;   // A stack of error callback functions
		bool                      m_compatible; // True if the renderer will work on this system
		pr::Renderer              m_rdr;        // The renderer
		WindowCont                m_wnd_cont;   // The created windows
		pr::ldr::ObjectCont       m_obj_cont;   // The created ldr objects
		pr::ldr::GizmoCont        m_giz_cont;   // The created ldr gizmos
		pr::ldr::ScriptSources    m_sources;    // A file watcher for loaded script source files
		pr::script::EmbeddedLua<> m_lua;
		std::recursive_mutex      m_mutex;

		Context()
			:m_inits()
			,m_error_cb()
			,m_compatible(pr::rdr::TestSystemCompatibility())
			,m_rdr(pr::rdr::RdrSettings(FALSE))
			,m_wnd_cont()
			,m_obj_cont()
			,m_giz_cont()
			,m_sources(m_obj_cont, m_rdr, &m_lua)
			,m_lua()
			,m_mutex()
		{
			PR_ASSERT(PR_DBG, pr::meta::is_aligned_to<16>(this), "dll data not aligned");

			// Hook up the sources events
			m_sources.OnFileRemoved += [&](pr::ldr::ScriptSources&, pr::ldr::ScriptSources::FileRemovedEventArgs const& args)
			{
				for (auto& wnd : m_wnd_cont)
					View3D_RemoveObjectsById(wnd, false, args.m_file_group_id);
			};
			m_sources.OnError += [&](pr::ldr::ScriptSources&, pr::ErrorEventArgs const& args)
			{
				ReportError(args.m_msg.c_str());
			};
			m_sources.OnStoreChanged += [&](pr::ldr::ScriptSources&, pr::ldr::ScriptSources::StoreChangedEventArgs const& args)
			{
				OnSourcesChanged.Raise(static_cast<ESourcesChangedReason>(args.m_reason));
			};
		}
		~Context()
		{
			while (!m_wnd_cont.empty())
				View3D_DestroyWindow(*m_wnd_cont.begin());
		}
		Context(Context const&) = delete;
		Context& operator=(Context const&) = delete;

		// Push/Pop error callbacks from the error callback stack
		void PushErrorCB(View3D_ReportErrorCB cb, void* ctx)
		{
			m_error_cb.emplace_back(ReportErrorCB(cb, ctx));
		}
		void PopErrorCB(View3D_ReportErrorCB cb)
		{
			if (m_error_cb.empty())
				throw std::exception("Error callback stack is empty, cannot pop");
			if (m_error_cb.back().m_cb != cb)
				throw std::exception("Attempt to pop an error callback that is not the most recently pushed callback. This is likely a destruction order probably");

			m_error_cb.pop_back();
		}

		// Report an error for this window
		void ReportError(wchar_t const* msg)
		{
			auto error_cb = m_error_cb.back();
			error_cb(msg);
		}

		// Remove all Ldr script sources
		void ClearScriptSources()
		{
			// Reset the sources collection
			m_sources.Clear();
		}

		// Load/Add a script source
		pr::Guid LoadScriptSource(wchar_t const* filepath, bool additional, bool async, pr::script::Includes<> const& includes)
		{
			// If this is not an additional load, clear the scene first
			if (!additional)
				ClearScriptSources();

			// Add 'filepath' to the sources
			return m_sources.AddFile(filepath, async, includes);
		}

		// Load/Add ldr objects from a script string
		pr::Guid LoadScript(wchar_t const* ldr_script, bool file, bool async, pr::Guid const* context_id, pr::script::Includes<> const& includes)
		{
			using namespace pr::script;

			// Create a context id if none given
			auto guid = pr::GenerateGUID();
			if (context_id == nullptr)
				context_id = &guid;

			// Create a writeable includes handler
			auto inc = includes;

			// Parse the description
			pr::ldr::ParseResult out(m_obj_cont);
			if (file)
			{
				inc.AddSearchPath(pr::filesys::GetDirectory<pr::script::string>(ldr_script));

				FileSrc src(ldr_script);
				Reader reader(src, false, &inc, nullptr, &m_lua);
				pr::ldr::Parse(m_rdr, reader, out, async, *context_id);
			}
			else // string
			{
				PtrW src(ldr_script);
				Reader reader(src, false, &inc, nullptr, &m_lua);
				pr::ldr::Parse(m_rdr, reader, out, async, *context_id);
			}

			return *context_id;
		}

		// Reload script source files
		void ReloadScriptSources()
		{
			// Remove script source objects from all windows
			for (auto src : m_sources.List())
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

		// Event raised when the script sources are updated
		pr::MultiCast<SourcesChangedCB> OnSourcesChanged;

		// Delete all objects
		void DeleteAllObjects()
		{
			// Remove the objects from any windows they're in
			for (auto wnd : m_wnd_cont)
				View3D_RemoveAllObjects(wnd);
		
			// Clear the object container. The unique pointers should delete the objects
			m_obj_cont.clear();
		}

		// Delete all objects with matching ids
		void DeleteAllObjectsById(pr::Guid const& context_id)
		{
			// Remove objects from any windows they might be assigned to
			for (auto wnd : m_wnd_cont)
				View3D_RemoveObjectsById(wnd, false, context_id);

			pr::ldr::Remove(m_obj_cont, &context_id, 1, 0, 0);
		}
	};
}
