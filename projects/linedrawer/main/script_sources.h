//*****************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
#pragma once

#include "linedrawer/main/forward.h"
#include "pr/filesys/filewatch.h"
#include "pr/script/forward.h"
#include "linedrawer/main/lua_source.h"

namespace ldr
{
	// This class is a collection of the file sources currently loaded in ldr
	class ScriptSources :pr::IFileChangedHandler
	{
	public:
		using filepath_t = pr::string<wchar_t>;
		using id_t = pr::Guid;

		struct File
		{
			filepath_t m_filepath;   // The file to watch
			id_t       m_context_id; // Context id for files

			File();
			File(filepath_t const& filepath, id_t const* context_id);
		};

		// A container that doesn't invalidate on add/remove is needed because
		// the file watcher contains a pointer to 'File' objects.
		using FileCont = std::unordered_map<filepath_t, File>;

	private:

		FileCont             m_files;
		pr::FileWatch        m_watcher;
		UserSettings&        m_settings;
		pr::Renderer&        m_rdr;
		pr::ldr::ObjectCont& m_store;
		LuaSource&           m_lua_src;

		// 'filepath' is the name of the changed file
		// 'handled' should be set to false if the file should be reported as changed the next time 'CheckForChangedFiles' is called (true by default)
		void FileWatch_OnFileChanged(wchar_t const* filepath, pr::Guid const& id, void* user_data, bool& handled);

		// Internal add file
		void AddFile(wchar_t const* filepath, Evt_StoreChanged::EReason reason);

	public:
		ScriptSources(UserSettings& settings, pr::Renderer& rdr, pr::ldr::ObjectCont& store, LuaSource& lua_src);

		// Return const access to the source files
		FileCont const& List() const
		{
			return m_files;
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
