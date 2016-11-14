//*****************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
#include "linedrawer/main/forward.h"
#include "linedrawer/main/script_sources.h"
#include "linedrawer/main/user_settings.h"
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
	ScriptSources::File::File(filepath_t const& filepath, id_t const* context_id)
		:m_filepath(filepath)
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
			pr::ldr::Remove(m_store, &file.second.m_context_id, 1, 0, 0);

		// Remove all file watches
		m_watcher.RemoveAll();
		m_files.clear();
	}

	// Reload all files
	void ScriptSources::Reload()
	{
		// Make a copy of the file list
		auto files = m_files;

		// Reset the sources
		Clear();

		// Add each file again
		for (auto& file : files)
			AddFile(file.second.m_filepath.c_str(), Evt_StoreChanged::EReason::Reload);
	}

	// Add a string source
	void ScriptSources::AddString(std::wstring const& str)
	{
		try
		{
			using namespace pr::script;

			ParseResult out(m_store);
			auto bcount = m_store.size();

			PtrW src(str.c_str());
			Reader reader(src, false, nullptr, nullptr, &m_lua_src);
			Parse(m_rdr, reader, out, false);

			pr::events::Send(Evt_StoreChanged(m_store, m_store.size() - bcount, out, Evt_StoreChanged::EReason::NewData));
			pr::events::Send(Evt_Refresh());
		}
		catch (std::exception const& ex)
		{
			pr::events::Send(Evt_AppMsg(pr::FmtS(L"Script error found while parsing source string.\r\n%S", ex.what()), L"Add Script String"));
		}
	}

	// Add a file source
	void ScriptSources::AddFile(wchar_t const* filepath)
	{
		AddFile(filepath, Evt_StoreChanged::EReason::NewData);
	}

	// Internal add file
	void ScriptSources::AddFile(wchar_t const* filepath, Evt_StoreChanged::EReason reason)
	{
		// Get the normalised filepath (before Remove() because it might be an existing file)
		auto fpath = pr::filesys::Standardise<filepath_t>(filepath);

		// Ensure the same file is not added twice
		// Don't use 'filepath', Remove() may invalidate this
		Remove(fpath.c_str());
		filepath = nullptr;

		// Add the filepath to the source files collection
		auto& file = m_files[fpath] = File(fpath, nullptr);

		try
		{
			ParseResult out(m_store);
			auto bcount = m_store.size();

			// Add file watchers for the file and everything it includes
			m_watcher.Add(fpath.c_str(), this, file.m_context_id, &file);
			auto add_watch = [&](pr::script::string const& fp)
			{
				// Use the same ID for all included files as well
				m_watcher.Add(pr::filesys::Standardise(fp).c_str(), this, file.m_context_id, &file);
			};

			// Add the file based on it's file type
			auto extn = pr::filesys::GetExtension(file.m_filepath);
			if (pr::str::EqualI(extn, "lua"))
			{
				m_lua_src.Add(fpath.c_str());
			}
			else if (pr::str::EqualI(extn, "p3d"))
			{
				Buffer<> src(ESrcType::Buffered, pr::FmtS("*Model {\"%s\"}", fpath.c_str()));

				Includes<> inc;
				inc.FileOpened += add_watch;
				inc.m_ignore_missing_includes = m_settings.m_IgnoreMissingIncludes;
				inc.AddSearchPath(pr::filesys::GetDirectory(fpath));

				Reader reader(src, false, &inc, nullptr, &m_lua_src);
				Parse(m_rdr, reader, out, true, file.m_context_id);
			}
			else // assume ldr script file
			{
				pr::LockFile lock(file.m_filepath, 10, 5000);
				FileSrc src(file.m_filepath.c_str());

				Includes<> inc;
				inc.FileOpened += add_watch;
				inc.m_ignore_missing_includes = m_settings.m_IgnoreMissingIncludes;
				inc.AddSearchPath(pr::filesys::GetDirectory(fpath));

				Reader reader(src, false, &inc, nullptr, &m_lua_src);
				Parse(m_rdr, reader, out, true, file.m_context_id);
			}

			pr::events::Send(Evt_StoreChanged(m_store, m_store.size() - bcount, out, reason));
			pr::events::Send(Evt_Refresh());
		}
		catch (pr::script::Exception const& ex)
		{
			pr::events::Send(Evt_AppMsg(pr::FmtS(L"Script error found while parsing source file '%s'.\r\n%S", fpath.c_str(), ex.what()), L"Add Script File"));
		}
		catch (LdrException const& ex)
		{
			std::wstring msg;
			switch (ex.code())
			{
			default: throw;
			case ELdrException::FileNotFound:
				msg = pr::FmtS(L"Source file '%s' not found.", fpath.c_str());
				break;
			case ELdrException::FailedToLoad:
				msg = pr::FmtS(L"Failed to load source file '%s'", fpath.c_str());
				break;
			}
			pr::events::Send(Evt_AppMsg(msg, L"Add Script File"));
		}
		catch (std::exception const& ex)
		{
			pr::events::Send(Evt_AppMsg(pr::FmtS(L"Error found while parsing source file '%s'.\r\n%S", fpath.c_str(), ex.what()), L"Add Script File"));
		}
	}

	// Remove a file source
	void ScriptSources::Remove(wchar_t const* filepath)
	{
		// Find the file in the file list
		auto iter = m_files.find(pr::filesys::Standardise<filepath_t>(filepath));
		if (iter == std::end(m_files))
			return;

		auto& file = iter->second;

		// Delete all objects belonging to this file
		pr::ldr::Remove(m_store, &file.m_context_id, 1, 0, 0);

		// Delete all associated file watches
		m_watcher.RemoveAll(file.m_context_id);

		// Remove it from the file list
		m_files.erase(iter);
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
		auto& file = *static_cast<File const*>(user_data);
		AddFile(file.m_filepath.c_str());
		pr::events::Send(Evt_Refresh());
	}
}