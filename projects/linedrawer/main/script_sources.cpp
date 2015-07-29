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
	void ScriptSources::AddString(std::wstring const& str)
	{
		try
		{
			ParseResult out(m_store);
			std::size_t bcount = m_store.size();

			pr::script::PtrW<> src(str.c_str());
			pr::script::Reader reader(src, false, nullptr, nullptr, &m_lua_src);
			Parse(m_rdr, reader, out, false, pr::ldr::DefaultContext);

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
	void ScriptSources::AddFile(wchar_t const* filepath)
	{
		AddFile(filepath, Event_StoreChanged::EReason::NewData);
	}

	// Internal add file
	void ScriptSources::AddFile(wchar_t const* filepath, Event_StoreChanged::EReason reason)
	{
		// Ensure the same file is not added twice
		Remove(filepath);

		// Add the filepath to the source files collection
		m_files.push_back(pr::filesys::StandardiseC<pr::string<wchar_t>>(filepath));
		auto& file = m_files.back();

		try
		{
			ParseResult out(m_store);
			auto bcount = m_store.size();

			// All objects added as a result of this file will have this context id
			int context_id = pr::hash::HashC(file.c_str());

			// Add file watchers for the file and everything it included
			m_watcher.Add(filepath, this, context_id, &file[0]);
			auto watch = [&](pr::script::string const& fpath)
			{
				m_watcher.Add(fpath.c_str(), this, context_id, &file[0]);
			};

			// Add the file based on it's file type
			auto extn = pr::filesys::GetExtension(file);
			if (pr::str::EqualI(extn, "lua"))
			{
				m_lua_src.Add(filepath);
			}
			else if (pr::str::EqualI(extn, "p3d"))
			{
				FileIncludes<> inc;
				inc.FileOpened += watch;
				inc.m_ignore_missing_includes = m_settings.m_IgnoreMissingIncludes;

				Buffer<> src(ESrcType::Buffered, pr::FmtS("*Model {\"%s\"}", filepath));
				Reader reader(src, false, &inc, nullptr, &m_lua_src);

				Parse(m_rdr, reader, out, true, context_id);
			}
			else // assume ldr script file
			{
				FileIncludes<> inc;
				inc.FileOpened += watch;
				inc.m_ignore_missing_includes = m_settings.m_IgnoreMissingIncludes;

				FileSrc<> src(file.c_str());
				Reader reader(src, false, &inc, nullptr, &m_lua_src);

				Parse(m_rdr, reader, out, true, context_id);
			}

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
	void ScriptSources::Remove(wchar_t const* filepath)
	{
		// Delete all objects belonging to this file
		auto fpath = pr::filesys::StandardiseC<pr::string<wchar_t>>(filepath);
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
	void ScriptSources::FileWatch_OnFileChanged(wchar_t const*, void* user_data, bool&)
	{
		// Get the filepath of the root file and add it as though it is a new file.
		// The changed file may have been included from other files.
		// We want to reload from the root of the file hierarchy
		auto filepath = static_cast<wchar_t const*>(user_data);
		AddFile(filepath);
	}
}