//==============================================================================
/**
*       @file       WTLTabViewCtrl.h
*
*       Interface for the CWTLTabViewCtrl class.
*
*       @author     Stephen Jones (Stephen@Jones-net.com)
*       @date       June 13th, 2002
*
*       Copyright 2002, Stephen Jones
*
*       History:
*       --------
*
*       06/18/2002  Creation
*/
//==============================================================================

// pr::Usage notes:
//  For the tab pages (or views) create WS_CHILD dialogs using CDialogImpl<MyDialog>.
//  (use Style=Child, Border=None, SystemMenu=false, Control=true)
//  In the main UI (or whereever the tab control is), create an instance of a
//  CWTLTabViewCtrl and instances of all of the pages.
//      CWTLTabViewCtrl     m_tab_view_ctrl;
//      Page1               m_page1;
//      Page2               m_page2;
//  In the OnInitDialog method do:
//      m_tab_view_ctrl.Attach(GetDlgItem(IDC_TAB));
//      m_page1.Create(m_hWnd); // Use the m_hWnd of the window not 'm_tab_view_ctrl'
//      m_page2.Create(m_hWnd);
//      m_tab_view_ctrl.AddTab(m_page1.Title() ,m_page1 ,TRUE  ,-1 ,(LPARAM)&m_page1);
//      m_tab_view_ctrl.AddTab(m_page2.Title() ,m_page2 ,FALSE ,-1 ,(LPARAM)&m_page2);
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

//==============================================================================
/**
*       @mainpage CWTLTabViewCtrl
*
*       @htmlinclude .\CodeProject\TabViewCtrl.html
*/
//==============================================================================

#ifndef INCLUDED_WTLTABVIEWCTRL
#define INCLUDED_WTLTABVIEWCTRL

#ifndef __ATLMISC_H__
#include <atlmisc.h>
#endif

#ifndef __ATLCTRLS_H__
#include <atlctrls.h>
#endif

#ifndef __ATLCRACK_H__
#include <atlcrack.h>
#endif


//==============================================================================
//
//  @class CWTLTabViewCtrl
//
//  A WTL class that encapsulates the functionality for managing a tab
//  control and associated tab windows.
//
//  The standard tab control does not provide facilities for managing the
//  child windows associated with each tab. This class  encapsulates the
//  functionality for automatically managing each tab.
//
//  Parent windows must have REFLECT_NOTIFICATIONS() in the message map
//  to pass along the TCN_SELCHANGE message.
//
//  This class started out as a port of a similar MFC class
//  (CSizingTabCtrlBar - author unknown), but turned into a total rewrite.
//
//==============================================================================
class CWTLTabViewCtrl : public CWindowImpl< CWTLTabViewCtrl, CTabCtrl >
{
	const LONG                  TABVIEW_BORDER;     ///< Border Width
	const LONG                  TABVIEW_EDGE;       ///< Distance of tab from content
	CSimpleArray<CWindow>       m_Views;            ///< An array of views for the tab
	LONG                        m_ActiveTabIndex;   ///< The index of the active tab
	CWindow                     m_ActiveTabWindow;  ///< The active tab window
	CFont                       m_HorizFont;        ///< Top/Bottom font used by tab control
	CFont                       m_LeftFont;         ///< Left font used by tab control
	CFont                       m_RightFont;        ///< Right font used by tab control
	
public:
	CWTLTabViewCtrl()
		:TABVIEW_BORDER(3)
		,TABVIEW_EDGE(5)
		,m_ActiveTabIndex(-1)
	{}
	
	// Append a tab to the end of the tab control.
	//
	//  @param  inLabel
	//          [in] The label to appear on the tab control.
	//  @param  inTabWindow
	//          [in] The child window to use as the view for the tab. The window
	//          must have the WS_CHILD style bit set and the WS_VISIBLE style bit not set.
	//  @param  inActiveFlag
	//          [in, optional] TRUE to make the tab active, FALSE to just add the tab.
	//  @param  inImage
	//          [in, optional] The index into the image list for the image to display by the tab label.
	//  @param  inParam
	//          [in, optional] The param value to associate with the tab.
	//  @return Zero based index of the new tab, -1 on failure
	BOOL AddTab(LPCTSTR inLabel, HWND inTabWindow, BOOL inActiveFlag = TRUE, int inImage = -1, LPARAM inParam = 0)
	{
		BOOL    theReturn;
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
		m_Views.Add(theTabWindow);
		
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
		theReturn = InsertItem(32768, &theItem);
		
		// Set the position for the window
		CRect rcChild;
		CalcViewRect(&rcChild);
		theTabWindow.MoveWindow(rcChild);
		
		// Select the tab that is being added, if desired
		LONG theTabIndex = GetTabCount() - 1;
		if (inActiveFlag || theTabIndex == 0)
		{
			SetActiveTab(theTabIndex);
		}
		
		return theReturn;
	}
	
	// Returns the current number of tabs.
	LONG GetTabCount() const
	{
		return m_Views.GetSize();
	}
	
	// Return the window handle for a tab
	HWND GetTab(int inTab) const
	{
		return inTab >= 0 && inTab < GetTabCount() ? m_Views[inTab] : 0;
	}
	
	// Virtual method that is called when a tab is removed.
	virtual void OnTabRemoved(int inTabIndex)
	{
		UNREFERENCED_PARAMETER(inTabIndex);
	}
	
	//==============================================================================
	/**
	*       RemoveTab:  Remove the specified tab.
	*
	*       @param  inTab
	*               [in] The index of the tab to remove.
	*       @return Returns the HWND of the deleted view window.
	*/
	//==============================================================================
	HWND RemoveTab(int inTab)
	{
		HWND theTabHwnd = NULL;
		LONG theNewTab = -1;
		
		if (inTab >= 0 && inTab < GetTabCount())
		{
			// Select a new tab if the tab is active
			if (m_ActiveTabIndex == inTab)
			{
				m_ActiveTabIndex = -1;
				m_ActiveTabWindow = NULL;
				
				if (GetTabCount() > 1)
				{
					theNewTab = (inTab > 0) ? (inTab - 1) : 0;
				}
			}
			
			// Save the window that is begin removed
			theTabHwnd = m_Views[ inTab ];
			
			// Virtual method to notify subclasses that a tab was removed
			OnTabRemoved(inTab);
			
			// Remove the item from the view list
			m_Views.RemoveAt(inTab);
			
			// Remove the tab
			if (IsWindow())
			{
				DeleteItem(inTab);
			}
			
			SetActiveTab(theNewTab);
		}
		
		return theTabHwnd;
	}
	
	// Remove all the tabs from the tab control.
	void RemoveAllTabs()
	{
		LONG theCount = GetTabCount();
		for (LONG theIndex = theCount - 1; theIndex >= 0; theIndex--)
		{
			RemoveTab(theIndex);
		}
	}
	
	// Activate the specified tab.
	void SetActiveTab(int inNewTab)
	{
		// Don't select if already selected
		if (inNewTab != m_ActiveTabIndex && inNewTab >= 0 && inNewTab < GetTabCount())
		{
			// Disable the old tab
			if (m_ActiveTabWindow.IsWindow())
			{
				m_ActiveTabWindow.EnableWindow(FALSE);
				m_ActiveTabWindow.ShowWindow(SW_HIDE);
			}
			
			// Enable the new tab
			m_ActiveTabWindow = m_Views[ inNewTab ];
			
			m_ActiveTabWindow.EnableWindow(TRUE);
			m_ActiveTabWindow.ShowWindow(SW_SHOW);
			m_ActiveTabWindow.SetFocus();
			
			m_ActiveTabWindow.Invalidate(TRUE);
			
			m_ActiveTabIndex = inNewTab;
			
			// Select the tab (if tab programmatically changed)
			SetCurSel(m_ActiveTabIndex);
		}
	}
	
	// Return the HWND of the active tab.
	HWND GetActiveTab() const
	{
		return GetTab(m_ActiveTabIndex);
	}
	
	// Return the index of the active tab.
	LONG GetActiveTabIndex() const
	{
		return m_ActiveTabIndex;
	}
	
	// Return the label of the specified tab.
	void GetTabText(int inTab, int inSize, LPTSTR outText) const
	{
		if (inTab >= 0 && inTab < GetTabCount())
		{
			// Get tab item info
			TCITEM tci;
			tci.mask = TCIF_TEXT;
			tci.pszText = outText;
			tci.cchTextMax = inSize;
			GetItem(inTab, &tci);
		}
	}
#ifdef __ATLSTR_H__
	CString GetTabText(int inTab) const
	{
		CString theTabText;
		if (inTab >= 0 && inTab < GetTabCount())
		{
			// Get tab item info
			char theText[128];
			
			GetTabText(inTab, sizeof(theText), theText);
			theTabText = theText;
		}
		
		return theTabText;
	}
#endif
	
	// Return the param of the specified tab.
	LPARAM GetTabParam(int inTab) const
	{
		TCITEM tci = {0};
		if (inTab >= 0 && inTab < GetTabCount())
		{
			// Get tab item info
			tci.mask = TCIF_PARAM;
			GetItem(inTab, &tci);
		}
		return tci.lParam;
	}
	
	// Return the image of the specified tab.
	int GetTabImage(int inTab) const
	{
		TCITEM tci = {0};
		if (inTab >= 0 && inTab < GetTabCount())
		{
			// Get tab item info
			tci.mask = TCIF_IMAGE;
			GetItem(inTab, &tci);
		}
		return tci.iImage;
	}
	
	//==============================================================================
	//
	//  ModifyTabStyle: This method modifies the window styles of the CWindow
	//      object. Styles to be added or removed can be combined by using the
	//      bitwise OR (|) operator.
	//
	//  @param  dwRemove
	//          [in] Specifies the window styles to be removed during style modification.
	//  @param  dwAdd
	//          [in] Specifies the window styles to be added during style modification.
	//  @param  nFlags
	//          [in] Window-positioning flags. For a list of possible values,
	//          see theSetWindowPos function in the Win32 SDK.
	//
	//  If nFlags is nonzero, ModifyStyle calls the Win32 function SetWindowPos,
	//  and redraws the window by combining nFlags with the following four flags:
	//
	//  SWP_NOSIZE   Retains the current size.
	//  SWP_NOMOVE   Retains the current position.
	//  SWP_NOZORDER   Retains the current Z order.
	//  SWP_NOACTIVATE   Does not activate the window.
	//  To modify a window's extended styles, call ModifyStyleEx.
	//
	//  @return TRUE if the window styles are modified; otherwise, FALSE.
	//
	//==============================================================================
	BOOL ModifyTabStyle(DWORD dwRemove, DWORD dwAdd, UINT nFlags)
	{
		// Modify the style
		BOOL theReturn = ModifyStyle(dwRemove, dwAdd, nFlags);
		
		// Update all the views in case the window positions changes
		UpdateViews();
		
		// Set the font in case the tab position changed
		SetTabFont(dwAdd);
		
		return theReturn;
	}
	
	// Update the position of all the contained views.
	void UpdateViews()
	{
		CRect rcChild;
		CalcViewRect(&rcChild);
		
		LONG theCount = GetTabCount();
		for (LONG theIndex = 0; theIndex < theCount; theIndex++)
		{
			m_Views[ theIndex ].MoveWindow(rcChild);
		}
	}
	
	// Message Map
	BEGIN_MSG_MAP_EX(CWTLTabViewCtrl)
	MESSAGE_HANDLER(WM_CREATE, OnCreate)
	MESSAGE_HANDLER(WM_DESTROY, OnDestroy)
	MESSAGE_HANDLER(WM_WINDOWPOSCHANGED, OnWindowPosChanged)
	REFLECTED_NOTIFY_CODE_HANDLER_EX(TCN_SELCHANGE, OnSelectionChanged)
	END_MSG_MAP()
	
	// Windows Message Handlers
	LRESULT OnSelectionChanged(WPARAM, LPNMHDR, BOOL&)
	{
		SetActiveTab(GetCurSel());
		return 0;
	}
	
protected:

	//==============================================================================
	/**
	*       OnCreate:   Called in response to the WM_CREATE message
	*
	*       @return 0
	*/
	//==============================================================================
	LRESULT OnCreate(UINT uMsg, WPARAM wParam, LPARAM lParam, BOOL&)
	{
		LRESULT lRet = DefWindowProc(uMsg, wParam, lParam);
		
		// Get the log font.
		NONCLIENTMETRICS ncm;
		ncm.cbSize = sizeof(NONCLIENTMETRICS);
		::SystemParametersInfo(SPI_GETNONCLIENTMETRICS, sizeof(NONCLIENTMETRICS), &ncm, 0);
		
		// Top and Bottom Tab Font
		m_HorizFont.CreateFontIndirect(&ncm.lfMessageFont);
		
		// Left Tab Font
		ncm.lfMessageFont.lfOrientation = 900;
		ncm.lfMessageFont.lfEscapement  = 900;
		m_LeftFont.CreateFontIndirect(&ncm.lfMessageFont);
		
		// Right Tab Font
		ncm.lfMessageFont.lfOrientation = 2700;
		ncm.lfMessageFont.lfEscapement  = 2700;
		m_RightFont.CreateFontIndirect(&ncm.lfMessageFont);
		
		// Check styles to determine which font to set
		SetTabFont(GetStyle());
		
		return lRet;
	}
	
	//==============================================================================
	/**
	*       OnDestroy:  Called in response to the WM_DESTROY message
	*
	*       @return 0
	*/
	//==============================================================================
	LRESULT OnDestroy(UINT, WPARAM, LPARAM, BOOL& bHandled)
	{
		RemoveAllTabs();
		
		bHandled = FALSE;
		
		return 0;
	}
	
	// Called in response to the WM_WNDPOSCHANGED message
	LRESULT OnWindowPosChanged(UINT, WPARAM, LPARAM, BOOL& bHandled)
	{
		UpdateViews();
		bHandled = FALSE;
		return 0;
	}
	
	// Called in response to the TCN_SELCHANGE message
	LRESULT OnSelectionChanged(LPNMHDR)
	{
		SetActiveTab(GetCurSel());
		return 0;
	}
	
protected:

	// Calculate the client rect for contained views.
	void CalcViewRect(CRect* pRect) const
	{
		GetClientRect((*pRect));
		
		if (pRect->Height() > 0 && pRect->Width() > 0)
		{
			// Calculate the Height (or Width) of the tab . . .
			// cause it could be Multiline
			CRect theTabRect;
			GetItemRect(0, &theTabRect);
			
			LONG theRowCount = GetRowCount();
			LONG theEdgeWidth = (theTabRect.Width() * theRowCount) + TABVIEW_EDGE;
			LONG theEdgeHeight = (theTabRect.Height() * theRowCount) + TABVIEW_EDGE;
			
			// Set the size based on the style
			DWORD dwStyle = GetStyle();
			if ((dwStyle & TCS_BOTTOM) && !(dwStyle & TCS_VERTICAL))        // Bottom
			{
				(*pRect).top    += TABVIEW_BORDER;
				(*pRect).left   += TABVIEW_BORDER;
				(*pRect).right  -= TABVIEW_BORDER;
				(*pRect).bottom -= theEdgeHeight;
			}
			else if ((dwStyle & TCS_RIGHT) && (dwStyle & TCS_VERTICAL))     // Right
			{
				(*pRect).top    += TABVIEW_BORDER;
				(*pRect).left   += TABVIEW_BORDER;
				(*pRect).right  -= theEdgeWidth;
				(*pRect).bottom -= TABVIEW_BORDER;
			}
			else if (dwStyle & TCS_VERTICAL)                                // Left
			{
				(*pRect).top    += TABVIEW_BORDER;
				(*pRect).left   += theEdgeWidth;
				(*pRect).right  -= TABVIEW_BORDER;
				(*pRect).bottom -= TABVIEW_BORDER;
			}
			else                                                            // Top
			{
				(*pRect).top    += theEdgeHeight;
				(*pRect).left   += TABVIEW_BORDER;
				(*pRect).right  -= TABVIEW_BORDER;
				(*pRect).bottom -= TABVIEW_BORDER;
			}
			
			WINDOWPLACEMENT wndpl;
			GetWindowPlacement(&wndpl);
			(*pRect).OffsetRect(wndpl.rcNormalPosition.left, wndpl.rcNormalPosition.top);
		}
	}
	
	// Set the font to be used by the tab control.
	//  'inStyleBits' - the style bits to use to calculate the font to use.
	void SetTabFont(DWORD inStyleBits)
	{
		if (inStyleBits & TCS_VERTICAL)
		{
			if (inStyleBits & TCS_RIGHT)    { SetFont(m_RightFont); }
			else                            { SetFont(m_LeftFont); }
		}
		else
		{
			SetFont(m_HorizFont);
		}
	}
};

#endif


