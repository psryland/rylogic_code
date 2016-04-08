//*****************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
#pragma once

#include "linedrawer/main/forward.h"
#include "pr/filesys/filewatch.h"
#include "pr/script/forward.h"
#include "linedrawer/main/lua_source.h"
#include "linedrawer/main/ldrevent.h"

namespace ldr
{
	// This class is a collection of the file sources currently loaded in ldr
	class ScriptSources :pr::IFileChangedHandler
	{
	public:
		struct File
		{
			pr::string<wchar_t> m_filepath;
			pr::Guid m_context_id;
			
			File();
			File(pr::string<wchar_t> const& filepath, pr::Guid const* context_id = nullptr);
		};
		using FileCont = pr::vector<File>;

	private:

		FileCont             m_files;
		pr::FileWatch        m_watcher;
		UserSettings&        m_settings;
		pr::Renderer&        m_rdr;
		pr::ldr::ObjectCont& m_store;
		LuaSource&           m_lua_src;

		// 'filepath' is the name of the changed file
		// 'handled' should be set to false if the file should be reported as changed the next time 'CheckForChangedFiles' is called (true by default)
		void FileWatch_OnFileChanged(wchar_t const* filepath, void* user_data, bool& handled);

		// Internal add file
		void AddFile(wchar_t const* filepath, Event_StoreChanged::EReason reason);

	public:
		ScriptSources(UserSettings& settings, pr::Renderer& rdr, pr::ldr::ObjectCont& store, LuaSource& lua_src);

		// Return const access to the source files
		StrList List() const
		{
			StrList files;
			for (auto& file : m_files) files.push_back(file.m_filepath);
			return files;
		}

		// Remove all file sources
		void Clear();

		// Reload all files
		void Reload();

		// Add a string source
		void AddString(std::wstring const& str);

		// Add a file source
		void AddFile(wchar_t const* filepath);

		// Remove a file source
		void Remove(wchar_t const* filepath);

		// Check all file sources for modifications and reload any that have changed
		void RefreshChangedFiles();
	};
}
