//*************************************************************************
// Recent Files
//  Copyright (c) Rylogic Ltd 2010
//*************************************************************************
// Usage:
//	Create an instance of RecentFiles in a GUI class
//	'Attach' to the popup menu for which recent files should be added
//	Export/Import a string containing the recent file list for saving
//	Add "CHAIN_MSG_MAP_MEMBER(m_recent_files)" to the message map for the GUI class
//	Inherit pr::RecentFiles::IHandler  in the GUI class
//
#ifndef PR_GUI_RECENT_FILES_H
#define PR_GUI_RECENT_FILES_H

#include <list>
#include <string>
#include <windows.h>
#include "pr/filesys/filesys.h"
#include "pr/gui/menu_list.h"

namespace pr
{
	namespace gui
	{
		class RecentFiles :public MenuList
		{
		public:
			typedef MenuList::IHandler IHandler;
			
			using MenuList::Attach;
			using MenuList::Items;
			using MenuList::Clear;
			using MenuList::UpdateMenu;
			using MenuList::Export;
			using MenuList::Import;
			using MenuList::ProcessWindowMessage;
			
			template <typename Char> void Add(Char const* file, bool update_menu)
			{
				auto fpath = pr::Widen(file);
				fpath = pr::filesys::StandardiseC(fpath);
				MenuList::Add(fpath.c_str(), 0, false, update_menu);
			}
			template <typename Char> void Add(Char const* file)
			{
				Add(file, true);
			}
		};
	}
}

#endif



































