//*****************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
#pragma once

#include "linedrawer/main/forward.h"
#include "pr/filesys/filewatch.h"
#include "pr/script/script_forward.h"
#include "linedrawer/main/lua_source.h"
#include "linedrawer/main/ldrevent.h"

namespace ldr
{
	// This class is a collection of the file sources currently loaded in ldr
	class ScriptSources
		:pr::FileWatch::IFileChangedHandler
	{
		typedef std::list<std::string> StrList;
		StrList              m_files;
		pr::FileWatch        m_watcher;
		UserSettings&        m_settings;
		pr::Renderer&        m_rdr;
		pr::ldr::ObjectCont& m_store;
		LuaSource&           m_lua_src;

		// 'filepath' is the name of the changed file
		// 'handled' should be set to false if the file should be reported as changed the next time 'CheckForChangedFiles' is called (true by default)
		void FileWatch_OnFileChanged(char const* filepath, void* user_data, bool& handled);

		// Internal add file
		void AddFile(char const* filepath, ldr::Event_StoreChanged::EReason reason);

	public:
		ScriptSources(ldr::UserSettings& settings, pr::Renderer& rdr, pr::ldr::ObjectCont& store, LuaSource& lua_src);

		// Return const access to the source files
		StrList const& List() const { return m_files; }

		// Remove all file sources
		void Clear();

		// Reload all files
		void Reload();

		// Add a string source
		void AddString(std::string const& str);

		// Add a file source
		void AddFile(char const* filepath);

		// Remove a file source
		void Remove(char const* filepath);

		// Check all file sources for modifications and reload any that have changed
		void RefreshChangedFiles();
	};
}
