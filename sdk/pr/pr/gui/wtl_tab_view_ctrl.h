//*******************************************************************
// Tab Control
//*******************************************************************
// @author     Stephen Jones (Stephen@Jones-net.com)
// @date       June 13th, 2002
// Copyright 2002, Stephen Jones
//
// History:
// --------
//  06/18/2002  Creation
//  01/08/2014  Refactored by P.Ryland.
//
// Usage notes:
//  For the tab pages (or views) create dialogs using CDialogImpl<MyDialog>.
//  (use Style=Child, Border=None, SystemMenu=false, Control=true)
//    Example RC script:
//       IDD_TAB_GENERAL DIALOGEX 0, 0, 203, 164
//       STYLE DS_CONTROL | DS_SHELLFONT | WS_CHILDWINDOW | WS_CLIPCHILDREN
//       EXSTYLE WS_EX_CONTROLPARENT
//       FONT 8, "MS Shell Dlg", 400, 0, 1
//       {
//           // Controls... (remember GROUPBOX's need to be WS_EX_TRANSPARENT)
//       }
//      - DS_CONTROL, WS_CHILDWINDOW - make this dialog a child,
//      - WS_CLIPCHILDREN - causes child control regions to be excluded when painting
//        the dialog background.
//      - WS_EX_CONTROLPARENT - tells the parent not to treat this dialog as one big
//        control but as a tree of controls.
//
//  In the main UI (or where ever the tab control is), create an instance of a
//  WTL::CTabViewCtrl and instances of all of the tab pages.
//      WTL::CTabViewCtrl m_tab_view_ctrl;
//      Page1             m_page1;
//      Page2             m_page2;
//    Example RC script:
//        IDD_DIALOG_OPTIONS DIALOGEX 0, 0, 230, 200
//        STYLE DS_SHELLFONT | WS_CAPTION | WS_VISIBLE | WS_CLIPCHILDREN | WS_POPUP | WS_THICKFRAME | WS_SYSMENU
//        CAPTION "Options"
//        FONT 8, "MS Shell Dlg", 400, 0, 1
//        {
//            CONTROL         "", IDC_TAB_MAIN, WC_TABCONTROL, WS_CLIPCHILDREN, 2, 2, 226, 174, WS_EX_LEFT
//            DEFPUSHBUTTON   "OK", IDOK, 123, 181, 50, 14, 0, WS_EX_LEFT
//            PUSHBUTTON      "Cancel", IDCANCEL, 175, 181, 50, 14, 0, WS_EX_LEFT
//        }
//
//  In the OnInitDialog method do:
//      m_tab_view_ctrl.Attach(GetDlgItem(IDC_TAB_MAIN));
//      m_page1.Create(m_tab_view_ctrl.m_hWnd); // Parent the tab pages to the tab control
//      m_page2.Create(m_tab_view_ctrl.m_hWnd);
//      m_tab_view_ctrl.AddTab(m_page1.Title() ,m_page1 ,TRUE  ,-1 ,(LPARAM)&m_page1);
//      m_tab_view_ctrl.AddTab(m_page2.Title() ,m_page2 ,FALSE ,-1 ,(LPARAM)&m_page2);
//
//  In the OnDestroy method do:
//      m_tab_view_ctrl.Detach();
//  In the main UI message map, add:
//      CHAIN_MSG_MAP_MEMBER(m_tab_view_ctrl)
//      REFLECT_NOTIFICATIONS()
//  or (the above didn't seem to work...:-/)
//      NOTIFY_HANDLER(IDC_TAB ,TCN_SELCHANGE ,m_tab_view_ctrl.OnSelectionChanged)
//  For resizing do:
//      add: MESSAGE_HANDLER(WM_SIZE ,OnSize) to the main UI
//      and: LRESULT OnSize(UINT, WPARAM, LPARAM, BOOL& bHandled) { m_tab_view_ctrl.UpdateViews(); bHandled = FALSE; return S_OK; }

#pragma once

#include <vector>
#include <string>
#include <exception>

#include <atlbase.h>
#include <atlwin.h>
#include <atlmisc.h>
#include <atlctrls.h>
#include <atlcrack.h>

namespace WTL
{
	// A class that encapsulates the functionality for managing a tab
	// control and associated tab windows.
	//
	// The standard tab control does not provide facilities for managing the
	// child windows associated with each tab. This class  encapsulates the
	// functionality for automatically managing each tab.
	//
	// Parent windows must have REFLECT_NOTIFICATIONS_EX() in the message map
	// to pass along the TCN_SELCHANGE message.
	//
	// This class started out as a port of a similar MFC class
	// (CSizingTabCtrlBar - author unknown), but turned into a total rewrite.
	class CTabViewCtrl : public CWindowImpl<CTabViewCtrl, CTabCtrl>
	{
		std::vector<CWindow> m_views; // An array of views for the tab
		LONG     m_active_tab_index;   // The index of the active tab
		CWindow  m_active_tab;         // The active tab window
		CFont    m_horiz_font;         // Top/Bottom font used by tab control
		CFont    m_left_font;          // Left font used by tab control
		CFont    m_right_font;         // Right font used by tab control
		int      m_border_size;
		int      m_top_pad;

	public:
		CTabViewCtrl(int border_width = 3, int top_pad = 5)
			:m_views()
			,m_active_tab_index(-1)
			,m_active_tab()
			,m_horiz_font()
			,m_left_font()
			,m_right_font()
			,m_border_size(border_width)
			,m_top_pad(top_pad)
		{}

		// Returns the current number of tabs.
		int TabCount() const
		{
			return int(m_views.size());
		}

		// Return the window handle for a tab
		HWND Tab(int tab_index) const
		{
			return tab_index >= 0 && tab_index < TabCount() ? m_views[tab_index] : 0;
		}

		// Return the HWND of the active tab.
		HWND ActiveTab() const
		{
			return Tab(m_active_tab_index);
		}

		// Get/Set the active tab index.
		int ActiveTabIndex() const
		{
			return m_active_tab_index;
		}
		void ActiveTabIndex(int tab_index)
		{
			if (tab_index != -1)
			{
				ValidateTabIndex(tab_index);

				// Don't select if already selected
				if (tab_index == m_active_tab_index)
					return;

				// Disable the old tab
				if (m_active_tab.IsWindow())
				{
					m_active_tab.EnableWindow(FALSE);
					m_active_tab.ShowWindow(SW_HIDE);
				}

				// Enable the new tab
				m_active_tab = m_views[tab_index];
				m_active_tab.EnableWindow(TRUE);
				m_active_tab.ShowWindow(SW_SHOW);
				m_active_tab.SetFocus();
				m_active_tab.Invalidate(TRUE);
			}

			// Select the tab (if tab programmatically changed)
			m_active_tab_index = tab_index;
			SetCurSel(m_active_tab_index);
		}

		// Append a tab to the end of the tab control.
		// inLabel - The label to appear on the tab control.
		// inTabWindow - The child window to use as the view for the tab. The window must have the WS_CHILD style bit set and the WS_VISIBLE style bit not set.
		// inActiveFlag - TRUE to make the tab active, FALSE to just add the tab.
		// inImage - The index into the image list for the image to display by the tab label.
		// inParam - The param value to associate with the tab.
		// Returns the zero based index of the new tab, -1 on failure
		BOOL AddTab(LPCTSTR inLabel, HWND inTabWindow, BOOL inActiveFlag = TRUE, int inImage = -1, LPARAM inParam = 0)
		{
			CWindow theTabWindow(inTabWindow);
		
			// Make sure it's a real window
			ATLASSERT(theTabWindow.IsWindow());
		
			// Make sure it's a child window and is not visible
			ATLASSERT((theTabWindow.GetStyle() & WS_CHILD) != 0);
			ATLASSERT((theTabWindow.GetStyle() & WS_VISIBLE) == 0);
		
			// Hide the view window
			theTabWindow.EnableWindow(FALSE);
			theTabWindow.ShowWindow(SW_HIDE);
		
			// Store the required data for the list
			m_views.push_back(theTabWindow);
		
			// Add the tab to the tab control
			TC_ITEM theItem = {0};
			theItem.mask = TCIF_TEXT;
			theItem.pszText = const_cast<LPTSTR>(inLabel);
		
			// Add an image for the tab
			if (inImage != -1)
			{
				theItem.mask |= TCIF_IMAGE;
				theItem.iImage = inImage;
			}
		
			// Add the param for the tab
			if (inParam != 0)
			{
				theItem.mask |= TCIF_PARAM;
				theItem.lParam = inParam;
			}
		
			// Insert the item at the end of the tab control
			BOOL theReturn = InsertItem(32768, &theItem);
		
			// Set the position for the window
			theTabWindow.MoveWindow(CalcViewRect());
		
			// Select the tab that is being added, if desired
			auto last = TabCount() - 1;
			if (inActiveFlag || last == 0)
				ActiveTabIndex(last);

			return theReturn;
		}

		// Remove the specified tab.
		// tab_index - The index of the tab to remove.
		// Returns the HWND of the deleted view window.
		HWND RemoveTab(int tab_index)
		{
			ValidateTabIndex(tab_index);

			// Select a new tab if the tab is active
			int new_tab_index = -1;
			if (m_active_tab_index == tab_index)
			{
				m_active_tab_index = -1;
				m_active_tab = 0;
				if (TabCount() > 1)
					new_tab_index = (tab_index > 0) ? (tab_index - 1) : 0;
			}

			// Save the window handle that is being removed
			HWND tab_hwnd = m_views[tab_index];

			// Notify subclasses that a tab was removed
			OnTabRemoved(tab_index);

			// Remove the item from the view list
			m_views.erase(std::begin(m_views) + tab_index);

			// Remove the tab
			if (IsWindow())
				DeleteItem(tab_index);

			ActiveTabIndex(new_tab_index);
			return tab_hwnd;
		}

		// Remove all the tabs from the tab control.
		void RemoveAllTabs()
		{
			// Delete tabs in reverse order to preserve indices
			for (auto i = TabCount(); i-- != 0;)
				RemoveTab(i);
		}

		// Return the label of the specified tab.
		std::string TabText(int tab_index) const
		{
			ValidateTabIndex(tab_index);

			// Get tab item info
			char buf[128] = {};
			TCITEM tci;
			tci.mask = TCIF_TEXT;
			tci.pszText = buf;
			tci.cchTextMax = sizeof(buf);
			GetItem(tab_index, &tci);
			return buf;
		}
	
		// Return the param of the specified tab.
		LPARAM TabParam(int tab_index) const
		{
			ValidateTabIndex(tab_index);

			// Get tab item info
			TCITEM tci = {};
			tci.mask = TCIF_PARAM;
			GetItem(tab_index, &tci);
			return tci.lParam;
		}

		// Return the image of the specified tab.
		int TabImage(int tab_index) const
		{
			ValidateTabIndex(tab_index);

			// Get tab item info
			TCITEM tci = {};
			tci.mask = TCIF_IMAGE;
			GetItem(tab_index, &tci);
			return tci.iImage;
		}
	
		// This method modifies the window styles of the CWindow object.
		// Styles to be added or removed can be combined by using the bitwise OR (|) operator.
		// dwRemove - Specifies the window styles to be removed during style modification.
		// dwAdd - Specifies the window styles to be added during style modification.
		// nFlags - Window-positioning flags. For a list of possible values, see the
		// SetWindowPos function in the Win32 SDK.
		// If nFlags is nonzero, ModifyStyle calls the Win32 function SetWindowPos,
		// and redraws the window by combining nFlags with the following four flags:
		//  SWP_NOSIZE   Retains the current size.
		//  SWP_NOMOVE   Retains the current position.
		//  SWP_NOZORDER   Retains the current Z order.
		//  SWP_NOACTIVATE   Does not activate the window.
		//  To modify a window's extended styles, call ModifyStyleEx.
		// Returns TRUE if the window styles are modified; otherwise, FALSE.
		BOOL ModifyTabStyle(DWORD dwRemove, DWORD dwAdd, UINT nFlags)
		{
			// Modify the style
			BOOL r = ModifyStyle(dwRemove, dwAdd, nFlags);
			UpdateViews();     // Update all the views in case the window positions changes
			SetTabFont(dwAdd); // Set the font in case the tab position changed
			return r;
		}
	
		// Update the position of all the contained views.
		void UpdateViews()
		{
			CRect rect = CalcViewRect();
			for (auto& wnd : m_views)
				wnd.MoveWindow(rect);
		}
	
		// Message Map
		BEGIN_MSG_MAP(CTabViewCtrl)
			MESSAGE_HANDLER(WM_CREATE, OnCreate)
			MESSAGE_HANDLER(WM_DESTROY, OnDestroy)
			MESSAGE_HANDLER(WM_WINDOWPOSCHANGED, OnWindowPosChanged)
			//REFLECTED_NOTIFY_CODE_HANDLER_EX(TCN_SELCHANGE, OnSelectionChanged)
		END_MSG_MAP()
	
		// Windows Message Handlers
		LRESULT OnSelectionChanged(WPARAM, LPNMHDR, BOOL&)
		{
			ActiveTabIndex(GetCurSel());
			return 0;
		}
	
	protected:

		// Throw if 'tab_index' is invalid
		void ValidateTabIndex(int tab_index) const
		{
			if (tab_index < 0 || tab_index >= TabCount())
				throw std::exception("Tab index out of range");
		}

		// Virtual method that is called when a tab is removed.
		virtual void OnTabRemoved(int tab_index)
		{
			(void)tab_index;
		}

		LRESULT OnCreate(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL&)
		{
			LRESULT lRet = DefWindowProc(uMsg, wParam, lParam);
		
			// Get the log font.
			NONCLIENTMETRICS ncm;
			ncm.cbSize = sizeof(NONCLIENTMETRICS);
			::SystemParametersInfo(SPI_GETNONCLIENTMETRICS, sizeof(NONCLIENTMETRICS), &ncm, 0);
		
			// Top and Bottom Tab Font
			m_horiz_font.CreateFontIndirect(&ncm.lfMessageFont);
		
			// Left Tab Font
			ncm.lfMessageFont.lfOrientation = 900;
			ncm.lfMessageFont.lfEscapement  = 900;
			m_left_font.CreateFontIndirect(&ncm.lfMessageFont);
		
			// Right Tab Font
			ncm.lfMessageFont.lfOrientation = 2700;
			ncm.lfMessageFont.lfEscapement  = 2700;
			m_right_font.CreateFontIndirect(&ncm.lfMessageFont);
		
			// Check styles to determine which font to set
			SetTabFont(GetStyle());
		
			return lRet;
		}
		LRESULT OnDestroy(UINT, WPARAM, LPARAM, BOOL& bHandled)
		{
			RemoveAllTabs();
			bHandled = FALSE;
			return 0;
		}
		LRESULT OnWindowPosChanged(UINT, WPARAM, LPARAM, BOOL& bHandled)
		{
			if (!IsWindow()) return 0;
			// Called in response to the WM_WNDPOSCHANGED message
			UpdateViews();
			bHandled = FALSE;
			return 0;
		}
		LRESULT OnSelectionChanged(LPNMHDR)
		{
			// Called in response to the TCN_SELCHANGE message
			ActiveTabIndex(GetCurSel());
			return 0;
		}
	
		// Calculate the client rect for contained views.
		CRect CalcViewRect() const
		{
			CRect rect;
			GetClientRect(rect);
			rect.right -= 1;
			rect.bottom -= 1;

			if (rect.Height() <= 0 || rect.Width() <= 0)
				return CRect();

			// Calculate the Height (or Width) of the tab cause it could be Multiline
			CRect tab_rect; GetItemRect(0, tab_rect);
			LONG row_count   = GetRowCount();
			LONG edge_width  = (tab_rect.Width()  * row_count) + m_top_pad;
			LONG edge_height = (tab_rect.Height() * row_count) + m_top_pad;

			// Set the size based on the style
			DWORD dwStyle = GetStyle();
			if ((dwStyle & TCS_BOTTOM) && !(dwStyle & TCS_VERTICAL))        // Bottom
			{
				rect.top    += m_border_size;
				rect.left   += m_border_size;
				rect.right  -= m_border_size;
				rect.bottom -= edge_height;
			}
			else if ((dwStyle & TCS_RIGHT) && (dwStyle & TCS_VERTICAL))     // Right
			{
				rect.top    += m_border_size;
				rect.left   += m_border_size;
				rect.right  -= edge_width;
				rect.bottom -= m_border_size;
			}
			else if (dwStyle & TCS_VERTICAL)                                // Left
			{
				rect.top    += m_border_size;
				rect.left   += edge_width;
				rect.right  -= m_border_size;
				rect.bottom -= m_border_size;
			}
			else                                                            // Top
			{
				rect.top    += edge_height;
				rect.left   += m_border_size;
				rect.right  -= m_border_size;
				rect.bottom -= m_border_size;
			}

			return rect;
		}
	
		// Set the font to be used by the tab control.
		// 'inStyleBits' - the style bits to use to calculate the font to use.
		void SetTabFont(DWORD inStyleBits)
		{
			if    (!(inStyleBits & TCS_VERTICAL)) SetFont(m_horiz_font);
			else if (inStyleBits & TCS_RIGHT    ) SetFont(m_right_font);
			else                                  SetFont(m_left_font);
		}
	};
}