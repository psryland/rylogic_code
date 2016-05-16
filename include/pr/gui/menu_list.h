//*****************************************
// Menu Helper
//  Copyright (c) Rylogic Ltd 2009
//*****************************************
#pragma once

#include <string>
#include <list>
#include <windows.h>
#include "pr/common/scope.h"

namespace pr
{
	namespace gui
	{
		// A class for managing a dynamic list of menu options.
		// Usage:
		//  Add an instance of MenuList to a GUI class
		//  'Attach' to the pop-up menu for which the list should be added
		//  (you need to create a dummy element in the pop-up menu and attach to that)
		//  Export/Import a string containing the items
		//  Add "CHAIN_MSG_MAP_MEMBER(m_menu_list)" to the message map for the GUI class
		//  Inherit MenuList::IHandler in the GUI class
		struct MenuList
		{
			struct Item
			{
				std::wstring m_name; // The string name of the menu item
				void*        m_tag;  // User data associated with this menu option

				Item(wchar_t const* name, void* tag)
					:m_name(name)
					,m_tag(tag)
				{}
				bool operator == (wchar_t const* name) const
				{
					return m_name.compare(name) == 0;
				}
			};
			using ItemList = std::list<Item>;

			// Menu event handler
			struct IHandler
			{
				virtual ~IHandler() {}
				virtual void MenuList_OnClick(MenuList* sender, Item const& item) = 0;
				virtual void MenuList_ListChanged(MenuList*) {}
			};

		private:

			ItemList  m_menu_items;  // A container of recent filenames
			HMENU     m_menu;        // The menu we're adding items to
			UINT      m_base_id;     // The resource id of the first entry in the recent files menu
			size_t    m_max_length;  // The maximum number of entries to allow in the menu
			IHandler* m_handler;     // Interface that handles events

		public:

			MenuList()
				:MenuList(nullptr, 0, ~size_t(), nullptr)
			{}
			MenuList(HMENU menu, UINT base_id, size_t max_length, IHandler* handler)
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

			// Attach this list to a pop-up menu
			void Attach(HMENU menu, UINT base_id = 0, size_t max_length = ~size_t(), IHandler* handler = nullptr)
			{
				if (m_menu)
				{
					for (; ::RemoveMenu(m_menu, 0, MF_BYPOSITION); ) {}
				}

				m_menu       = menu;
				m_base_id    = base_id;
				m_max_length = max_length;
				m_handler    = handler;

				UpdateMenu();
			}

			// Get/Set the maximum length of the menu list
			std::size_t MaxLength() const
			{
				return m_max_length;
			}
			void MaxLength(size_t max_length)
			{
				m_max_length = max_length;
				if (m_menu_items.size() <= m_max_length)
					return;

				for (; m_menu_items.size() > m_max_length; m_menu_items.pop_back()) {}

				UpdateMenu();
				if (m_handler)
					m_handler->MenuList_ListChanged(this);
			}

			// Remove all items from the menu list
			void Clear()
			{
				auto list_changed = !m_menu_items.empty();
				m_menu_items.clear();

				UpdateMenu();
				if (m_handler && list_changed)
					m_handler->MenuList_ListChanged(this);
			}

			// Add a menu item or sub menu
			// 'item' is the string name of the menu item
			// 'user_data' is context data associated with the menu item
			// 'allow_duplicates' if true allows menu items with the same string name to be added
			// 'update_menu' if true will cause the items in the menu to be refreshed
			void Add(wchar_t const* item, void* user_data, bool allow_duplicates, bool update_menu)
			{
				// Remove duplicates of 'item'
				if (!allow_duplicates)
				{
					auto iter = std::find(std::begin(m_menu_items), std::end(m_menu_items), item);
					if (iter != std::end(m_menu_items)) m_menu_items.erase(iter);
				}

				// Trim the number of items to the maximum
				for (; m_menu_items.size() >= m_max_length; )
					m_menu_items.pop_back();

				// Insert the new item at the front
				m_menu_items.emplace_front(item, user_data);
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
				for (; ::RemoveMenu(m_menu, 0, MF_BYPOSITION); ) {}
				
				// Add the items to the menu
				unsigned int j = 0;
				for (auto& item : m_menu_items)
					::AppendMenuW(m_menu, MF_STRING, m_base_id + j++, item.m_name.c_str());
			}

			// Export a string representation of all of the items in the menu list
			std::wstring Export(wchar_t delimiter = L',') const
			{
				std::wstring s;
				for (auto& item : m_menu_items)
				{
					s.append(item.m_name);
					s.append(1, delimiter);
				}
				if (!s.empty()) s.resize(s.size() - 1);
				return s;
			}

			// Import a string of the items in the menu list
			void Import(std::wstring s)
			{
				// Prevent call backs while we import
				auto handler = m_handler;
				auto disable_handler = pr::CreateScope([&]{ m_handler = nullptr; }, [&]{ m_handler = handler; });

				// Import the menu items
				auto beg = std::begin(s);
				auto end = std::end(s);
				auto last = beg;
				for (; beg != end; beg = last + (last != end))
				{
					for (last = beg; last != end && *last != ','; ++last) {}
					if (last == beg) break;
					std::wstring name(beg, last);
					Add(name.c_str(), 0, true, false);
				}
				m_menu_items.reverse();
				UpdateMenu();
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
						auto i = std::begin(m_menu_items);
						std::advance(i, LOWORD(wParam) - m_base_id);

						// Use a copy to prevent reentrancy issues
						Item item = *i;
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
