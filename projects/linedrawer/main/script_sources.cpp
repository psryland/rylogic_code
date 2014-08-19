//*****************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
#include "linedrawer/main/stdafx.h"
#include "linedrawer/main/script_sources.h"
#include "linedrawer/main/user_settings.h"
#include "linedrawer/main/ldrexception.h"
#include "linedrawer/main/ldrevent.h"
#include "pr/linedrawer/ldr_object.h"
#include "pr/linedrawer/ldr_objects_dlg.h"
#include "pr/common/events.h"
#include "pr/script/reader.h"

using namespace pr::ldr;
using namespace pr::script;

namespace ldr
{
	ScriptSources::ScriptSources(UserSettings& settings, pr::Renderer& rdr, pr::ldr::ObjectCont& store, LuaSource& lua_src)
		:m_files()
		,m_watcher()
		,m_settings(settings)
		,m_rdr(rdr)
		,m_store(store)
		,m_lua_src(lua_src)
	{}

	// Remove all file sources
	void ScriptSources::Clear()
	{
		while (!m_files.empty())
			Remove(m_files.front().c_str());
	}

	// Reload all files
	void ScriptSources::Reload()
	{
		// Make a copy of the file list
		StrList files = m_files;
		m_files.clear();

		// Delete all objects belonging to these files
		for (auto& file : files)
		{
			int context_id = pr::hash::HashC(file.c_str());
			pr::ldr::Remove(m_store, &context_id, 1, 0, 0);
		}

		// Add each file again
		for (auto& file : files)
			AddFile(file.c_str(), Event_StoreChanged::EReason::Reload);
	}

	// Add a string source
	void ScriptSources::AddString(std::string const& str)
	{
		try
		{
			ParseResult out(m_store);
			std::size_t bcount = m_store.size();
			ParseString(m_rdr, str.c_str(), out, false, pr::ldr::DefaultContext, nullptr, nullptr, &m_lua_src);

			pr::events::Send(Event_StoreChanged(m_store, m_store.size() - bcount, out, Event_StoreChanged::EReason::NewData));
			pr::events::Send(Event_Refresh());
		}
		catch (pr::script::Exception const& e)
		{
			auto msg = pr::FmtS("Script error found while parsing source string.\n%s", e.what());
			pr::events::Send(Event_Error(msg));
		}
		catch (std::exception const& e)
		{
			auto msg = pr::FmtS("Error encountered while parsing source string.\n%s", e.what());
			pr::events::Send(Event_Error(msg));
		}
	}

	// Add a file source
	void ScriptSources::AddFile(char const* filepath)
	{
		AddFile(filepath, Event_StoreChanged::EReason::NewData);
	}

	// Internal add file
	void ScriptSources::AddFile(char const* filepath, Event_StoreChanged::EReason reason)
	{
		// Ensure the same file is not added twice
		Remove(filepath);

		// Add the filepath to the source files collection
		m_files.push_back(pr::filesys::StandardiseC<std::string>(filepath));

		// All objects added as a result of this file will have this context id
		int context_id = pr::hash::HashC(m_files.back().c_str());

		try
		{
			typedef std::vector<std::string> StrCont;
			std::size_t bcount = m_store.size();
			ParseResult out(m_store);

			// An include handler that records all of the files opened
			// so that we can detect changes in those files
			struct LdrIncludes :FileIncludes
			{
				StrCont m_paths;
				std::unique_ptr<Src> Open(pr::script::string const& include, Loc const& loc, bool search_paths_only) override
				{
					auto src = FileIncludes::Open(include, loc, search_paths_only);
					if (src)
					{
						auto fsrc = static_cast<FileSrc*>(src.get());
						m_paths.push_back(fsrc->m_file_loc.m_file);
					}
					return src;
				}
			} includes;

			// Add the file based on it's file type
			std::string extn = pr::filesys::GetExtension(m_files.back());
			if (pr::str::EqualI(extn, "lua"))
			{
				m_lua_src.Add(filepath);
			}
			else if (pr::str::EqualI(extn, "x"))
			{
			}
			else // assume ldr script file
			{
				FileSrc src(m_files.back().c_str());
				Reader reader(src);
				reader.IncludeHandler(&includes);
				reader.CodeHandler(&m_lua_src);
				reader.IgnoreMissingIncludes(m_settings.m_IgnoreMissingIncludes);
				Parse(m_rdr, reader, out, true, context_id);
			}

			// Add file watchers for the file and everything it included
			m_watcher.Add(filepath, this, context_id, &m_files.back()[0]);
			for (auto path : includes.m_paths)
				m_watcher.Add(path.c_str(), this, context_id, &m_files.back()[0]);

			pr::events::Send(Event_StoreChanged(m_store, m_store.size() - bcount, out, reason));
			pr::events::Send(Event_Refresh());
		}
		catch (pr::script::Exception const& e)
		{
			auto msg = pr::FmtS("Script error found while parsing source file '%s'.\n%s", filepath, e.what());
			pr::events::Send(Event_Error(msg));
		}
		catch (LdrException const& e)
		{
			switch (e.code())
			{
			default: throw;
			case ELdrException::FileNotFound:
				pr::events::Send(Event_Error(pr::FmtS("Source file '%s' not found", filepath)));
				break;
			case ELdrException::FailedToLoad:
				pr::events::Send(Event_Error(pr::FmtS("Failed to load source file '%s'", filepath)));
				break;
			}
		}
	}

	// Remove a file source
	void ScriptSources::Remove(char const* filepath)
	{
		// Delete all objects belonging to this file
		std::string fpath = pr::filesys::StandardiseC<std::string>(filepath);
		int context_id = pr::hash::HashC(fpath.c_str());
		pr::ldr::Remove(m_store, &context_id, 1, 0, 0);

		// Delete all associated file watches
		m_watcher.RemoveAll(context_id);

		// Remove it from the file list
		auto iter = std::find(m_files.begin(), m_files.end(), fpath);
		if (iter != m_files.end()) m_files.erase(iter);
	}

	// Check all file sources for modifications and reload any that have changed
	void ScriptSources::RefreshChangedFiles()
	{
		m_watcher.CheckForChangedFiles();
	}

	// 'filepath' is the name of the changed file
	// 'handled' should be set to false if the file should be reported as changed the next time 'CheckForChangedFiles' is called (true by default)
	void ScriptSources::FileWatch_OnFileChanged(char const*, void* user_data, bool&)
	{
		// Get the filepath of the root file and add it as though it is a new file.
		// The changed file may have been included from other files.
		// We want to reload from the root of the file hierarchy
		std::string filepath = static_cast<char const*>(user_data);
		AddFile(filepath.c_str());
	}
}