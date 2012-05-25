//*****************************************
// Menu Helper
//  Copyright © Rylogic Ltd 2009
//*****************************************

#ifndef PR_GUI_MENU_HELPER_H
#define PR_GUI_MENU_HELPER_H

#include <windows.h>

#ifndef PR_ASSERT
#	define PR_ASSERT_DEFINED
#	define PR_ASSERT(grp, exp, str)
#endif

namespace pr
{
	namespace gui
	{
		// Return a sub menu by address
		// Use: HMENU menu = GetMenuByName(GetMenu(), "&File,&Recent Files");
		// Returns 0 if the sub menu isn't found
		inline HMENU GetMenuByName(HMENU root, TCHAR const* address)
		{
			PR_ASSERT(PR_DBG, root != 0, "");
			for (TCHAR const* addr = address; *addr != 0;)
			{
				// Find ',' in 'address'
				TCHAR const* end = addr;
				for (; *end != 0 && *end != TEXT(','); ++end) {}
				if (end == addr) return 0;

				// Look for the first part of the address in the items of 'root'
				for (int i = 0, iend = ::GetMenuItemCount(root); i != iend; ++i)
				{
					// Get the menu item name and length
					// Check the item name matches the first part of the address
					TCHAR item_name[256];
					int item_name_len = ::GetMenuString(root, (UINT)i, item_name, 256, MF_BYPOSITION);
					if (item_name_len != end - addr || _tcsncmp(addr, item_name, item_name_len) != 0)
						continue;

					// If this is the last part of the address, then return the submenu
					// Note, if the menu is not a submenu then you'll get 0, turn it into a popup menu before here
					HMENU sub_menu = ::GetSubMenu(root, i);
					if (*end == 0 || sub_menu == 0)
						return sub_menu;
					
					root = sub_menu;
					addr = end + 1;
					break;
				}
			}
			return 0;
		}
	
		// A helper class for managing a dynamic list of menu options
		// Usage:
		//  Add an instance of MenuList to a gui class
		//  'Attach' to the popup menu for which the list should be added
		//  (you need to create a dummy element in the popup menu and attach to that)
		//  Export/Import a string containing the items
		//	Add "CHAIN_MSG_MAP_MEMBER(m_menu_list)" to the message map for the GUI class
		//	Inherit MenuList::IHandler in the GUI class
		class MenuList
		{
		public:
			struct Item
			{
				std::string m_name; // The string name of the menu item
				void*       m_tag;  // User data associated with this menu option
				Item(char const* name, void* tag) :m_name(name) ,m_tag(tag) {}
				bool operator == (char const* name) const { return m_name.compare(name) == 0; }
			};
			typedef std::list<Item> ItemList;
			
			// Menu event handler
			struct IHandler
			{
				virtual ~IHandler() {}
				virtual void MenuList_OnClick(pr::gui::MenuList* sender, pr::gui::MenuList::Item const& item) = 0;
				virtual void MenuList_ListChanged(pr::gui::MenuList*) {}
			};
			
		private:
			ItemList    m_menu_items;  // A container of recent filenames
			HMENU       m_menu;        // The menu we're adding items to
			UINT        m_base_id;     // The resource id of the first entry in the recent files menu
			std::size_t m_max_length;  // The maximum number of entries to allow in the menu
			IHandler*   m_handler;     // Interface that handles events
			
		public:
			MenuList(HMENU menu = 0, UINT base_id = 0, std::size_t max_length = 0xffffffff, IHandler* handler = 0)
			:m_menu_items()
			,m_menu(menu)
			,m_base_id(base_id)
			,m_max_length(max_length)
			,m_handler(handler)
			{}
			
			// Access the items of the menu list
			ItemList const& Items() const
			{
				return m_menu_items;
			}
			
			// Attach this list to a popup menu
			void Attach(HMENU menu, UINT base_id = 0, std::size_t max_length = 0xffffffff, IHandler* handler = 0)
			{
				if (m_menu) while (::RemoveMenu(m_menu, 0, MF_BYPOSITION)) {}
				m_menu = menu;
				m_base_id = base_id;
				m_max_length = max_length;
				m_handler = handler;
				UpdateMenu();
			}
			
			// Get/Set the maximum length of the menu list
			std::size_t MaxLength() const
			{
				return m_max_length;
			}
			void MaxLength(int max_length)
			{
				m_max_length = max_length;
				if (m_menu_items.size() <= m_max_length) return;
				while (m_menu_items.size() > m_max_length) { m_menu_items.pop_back(); }
				UpdateMenu();
				if (m_handler) m_handler->MenuList_ListChanged(this);
			}
			
			// Remove all items from the menu list
			void Clear()
			{
				bool list_changed = !m_menu_items.empty();
				m_menu_items.clear();
				UpdateMenu();
				if (m_handler && list_changed) m_handler->MenuList_ListChanged(this);
			}
			
			// Add a menu item or sub menu
			// 'item' is the string name of the menu item
			// 'user_data' is context data associated with the menu item
			// 'allow_duplicates' if true allows menu items with the same string name to be added
			// 'update_menu' if true will cause the items in the menu to be refreshed
			void Add(char const* item, void* user_data, bool allow_duplicates, bool update_menu)
			{
				if (!allow_duplicates)
				{
					ItemList::iterator iter = std::find(m_menu_items.begin(), m_menu_items.end(), item);
					if (iter != m_menu_items.end()) m_menu_items.erase(iter);
				}
				while (m_menu_items.size() >= m_max_length) { m_menu_items.pop_back(); }
				m_menu_items.push_front(Item(item, user_data));
				if (update_menu) UpdateMenu();
				if (m_handler) m_handler->MenuList_ListChanged(this);
			}
			
			// Remove a single item from the menu list
			// Use 'Items()' to find an iterator to the item to be removed
			void Remove(ItemList::const_iterator item, bool update_menu)
			{
				m_menu_items.erase(item);
				if (update_menu) UpdateMenu();
				if (m_handler) m_handler->MenuList_ListChanged(this);
			}
			
			// Repopulate the menu from the items in this list
			void UpdateMenu()
			{
				if (!m_menu) return;
				
				// Empty the menu
				while (::RemoveMenu(m_menu, 0, MF_BYPOSITION)) {}
				
				// Add the items to the menu
				unsigned int j = 0;
				for (ItemList::const_iterator i = m_menu_items.begin(), iend = m_menu_items.end(); i != iend; ++i, ++j)
					::AppendMenu(m_menu, MF_STRING, m_base_id + j, (LPCTSTR)i->m_name.c_str());
			}
			
			// Export a string representation of all of the items in the menu list
			std::string Export(char delimiter = ',') const
			{
				std::string s;
				for (ItemList::const_iterator i = m_menu_items.begin(), iend = m_menu_items.end(); i != iend; ++i)
				{
					s.append(i->m_name);
					s.append(1, delimiter);
				}
				if (!s.empty()) s.resize(s.size() - 1);
				return s;
			}
			
			// Import a string of the items in the menu list
			void Import(std::string s)
			{
				// Prevent call backs while we import
				IHandler* handler = m_handler; m_handler = 0;
				for (std::string::iterator first = s.begin(), end = s.end(), last; first != end; first = last + (last != end))
				{
					for (last = first; last != end && *last != ','; ++last) {}
					if (last == first) break;
					std::string name(first, last);
					Add(name.c_str(), 0, true, false);
				}
				m_menu_items.reverse();
				UpdateMenu();
				m_handler = handler;
			}
			
			// Message handler, use CHAIN_MSG_MAP_MEMBER()
			BOOL ProcessWindowMessage(HWND, UINT uMsg, WPARAM wParam, LPARAM, LRESULT& lResult)
			{
				// Use of the wParam and lParam parameters are summarized here. 
				// Message Source  wParam (high word)   wParam (low word)       lParam 
				//    Menu               0              Menu identifier        (IDM_*)0 
				//  Accelerator          1           Accelerator identifier    (IDM_*)0 
				if (uMsg == WM_COMMAND && LOWORD(wParam) >= m_base_id && LOWORD(wParam) < m_base_id + m_menu_items.size())
				{
					if (m_handler)
					{
						ItemList::const_iterator i = m_menu_items.begin();
						std::advance(i, LOWORD(wParam) - m_base_id);
						Item item = *i; // To prevent reentracy issues
						m_handler->MenuList_OnClick(this, item);
					}
					lResult = 0;
					return TRUE;
				}
				return FALSE;
			}
		};
	}
}
	
#ifdef PR_ASSERT_DEFINED
#	undef PR_ASSERT_DEFINED
#	undef PR_ASSERT
#endif

#endif
