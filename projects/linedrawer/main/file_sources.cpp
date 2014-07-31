//*****************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
#include "linedrawer/main/stdafx.h"
#include "linedrawer/main/file_sources.h"
#include "linedrawer/main/user_settings.h"
#include "linedrawer/main/ldrexception.h"
#include "linedrawer/main/ldrevent.h"
#include "pr/linedrawer/ldr_object.h"
#include "pr/linedrawer/ldr_objects_dlg.h"
#include "pr/common/events.h"
#include "pr/script/reader.h"

namespace ldr
{
	FileSources::FileSources(UserSettings& settings, pr::Renderer& rdr, pr::ldr::ObjectCont& store, LuaSource& lua_src)
		:m_files()
		,m_watcher()
		,m_settings(settings)
		,m_rdr(rdr)
		,m_store(store)
		,m_lua_src(lua_src)
	{}

	// Remove all file sources
	void FileSources::Clear()
	{
		while (!m_files.empty())
			Remove(m_files.front().c_str());
	}

	// Reload all files
	void FileSources::Reload()
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
			Add(file.c_str());
	}

	// Add a file source
	void FileSources::Add(char const* filepath)
	{
		using namespace pr::script;

		// Ensure the same file is not added twice
		Remove(filepath);

		// Add the filepath to the source files collection
		m_files.push_back(pr::filesys::StandardiseC<std::string>(filepath));

		// All objects added as a result of this file will have this context id
		int context_id = pr::hash::HashC(m_files.back().c_str());

		try
		{
			typedef std::vector<std::string> StrCont;

			// An include handler that records all of the files opened
			// so that we can detect changes in those files
			struct LdrIncludes :FileIncludes
			{
				StrCont m_paths;
				LdrIncludes(bool ignore_missing) :FileIncludes(ignore_missing) {}
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
			} includes(m_settings.m_IgnoreMissingIncludes);

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
				Reader  reader(src);
				reader.CodeHandler()    = &m_lua_src;
				reader.IncludeHandler() = &includes;
				pr::ldr::Add(m_rdr, reader, m_store, context_id);
			}

			// Add file watches for the file and everything it included
			m_watcher.Add(filepath, this, context_id, &m_files.back()[0]);
			for (auto path : includes.m_paths)
				m_watcher.Add(path.c_str(), this, context_id, &m_files.back()[0]);

			pr::events::Send(Event_StoreChanged(m_store));
			pr::events::Send(Event_Refresh());
		}
		catch (pr::script::Exception const& e)
		{
			pr::script::string msg = pr::FmtS("Script error found while parsing source file '%s'.\n'%s'", filepath, e.what());
			pr::events::Send(Event_Error(msg.c_str()));
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
	void FileSources::Remove(char const* filepath)
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
	void FileSources::RefreshChangedFiles()
	{
		m_watcher.CheckForChangedFiles();
	}

	// 'filepath' is the name of the changed file
	// 'handled' should be set to false if the file should be reported as changed the next time 'CheckForChangedFiles' is called (true by default)
	void FileSources::FileWatch_OnFileChanged(char const*, void* user_data, bool&)
	{
		// Get the filepath of the root file and add it as though it is a new file.
		// The changed file may have been included from other files.
		// We want to reload from the root of the file hierarchy
		std::string filepath = static_cast<char const*>(user_data);
		Add(filepath.c_str());
	}
}