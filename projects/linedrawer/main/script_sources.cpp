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
	ScriptSources::File::File()
		:m_filepath()
		,m_context_id(pr::GuidZero)
	{}
	ScriptSources::File::File(pr::string<wchar_t> const& filepath, pr::Guid const* context_id)
		:m_filepath(pr::filesys::StandardiseC<pr::string<wchar_t>>(filepath))
		,m_context_id(context_id ? *context_id : pr::GenerateGUID())
	{}

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
		// Delete all objects belonging to the files
		for (auto& file : m_files)
			pr::ldr::Remove(m_store, &file.m_context_id, 1, 0, 0);

		m_files.resize(0);
	}

	// Reload all files
	void ScriptSources::Reload()
	{
		// Make a copy of the file list
		auto files = m_files;
		m_files.clear();

		// Delete all objects belonging to these files
		for (auto& file : files)
			pr::ldr::Remove(m_store, &file.m_context_id, 1, 0, 0);

		// Add each file again
		for (auto& file : files)
			AddFile(file.m_filepath.c_str(), Event_StoreChanged::EReason::Reload);
	}

	// Add a string source
	void ScriptSources::AddString(std::wstring const& str)
	{
		try
		{
			using namespace pr::script;

			ParseResult out(m_store);
			auto bcount = m_store.size();

			PtrW<> src(str.c_str());
			Reader reader(src, false, nullptr, nullptr, &m_lua_src);
			Parse(m_rdr, reader, out, false);

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
		m_files.push_back(File(filepath));
		auto& file = m_files.back();

		try
		{
			ParseResult out(m_store);
			auto bcount = m_store.size();

			// Add file watchers for the file and everything it includes
			m_watcher.Add(filepath, this, file.m_context_id, &file);
			auto add_watch = [&](pr::script::string const& fpath)
			{
				// Use the same ID for all included files as well
				auto f = File(fpath, &file.m_context_id);
				m_watcher.Add(f.m_filepath.c_str(), this, f.m_context_id, &file);
			};

			// Add the file based on it's file type
			auto extn = pr::filesys::GetExtension(file.m_filepath);
			if (pr::str::EqualI(extn, "lua"))
			{
				m_lua_src.Add(filepath);
			}
			else if (pr::str::EqualI(extn, "p3d"))
			{
				Includes<> inc;
				inc.FileOpened += add_watch;
				inc.m_ignore_missing_includes = m_settings.m_IgnoreMissingIncludes;

				Buffer<> src(ESrcType::Buffered, pr::FmtS("*Model {\"%s\"}", filepath));
				Reader reader(src, false, &inc, nullptr, &m_lua_src);

				Parse(m_rdr, reader, out, true, file.m_context_id);
			}
			else // assume ldr script file
			{
				Includes<> inc;
				inc.FileOpened += add_watch;
				inc.m_ignore_missing_includes = m_settings.m_IgnoreMissingIncludes;

				FileSrc<> src(file.m_filepath.c_str());
				Reader reader(src, false, &inc, nullptr, &m_lua_src);

				Parse(m_rdr, reader, out, true, file.m_context_id);
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
		// Find the file in the file list
		File file(filepath);
		auto iter = std::find_if(std::begin(m_files), std::end(m_files), [&](File const& f){ return f.m_filepath == file.m_filepath; });
		if (iter == std::end(m_files)) return;
		file = *iter;

		// Remove it from the file list
		m_files.erase(iter);

		// Delete all objects belonging to this file
		pr::ldr::Remove(m_store, &file.m_context_id, 1, 0, 0);

		// Delete all associated file watches
		m_watcher.RemoveAll(file.m_context_id);
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