//*************************************************************************
// Recent Files
//  Copyright (c) Rylogic Ltd 2010
//*************************************************************************
// Usage:
//	Create an instance of RecentFiles in a GUI class
//	'Attach' to the pop up menu for which recent files should be added
//	Export/Import a string containing the recent file list for saving
//	Add "CHAIN_MSG_MAP_MEMBER(m_recent_files)" to the message map for the GUI class
//	Inherit pr::RecentFiles::IHandler  in the GUI class
//
#pragma once

#include <list>
#include <string>
#include <filesystem>
#include <windows.h>
#include "pr/gui/menu_list.h"

namespace pr::gui
{
	// Extends menu-list with a method for adding filenames
	struct RecentFiles :MenuList
	{
		void Add(std::filesystem::path const& file, bool update_menu = true)
		{
			MenuList::Add(file.wstring().c_str(), nullptr, false, update_menu);
		}
	};
}
