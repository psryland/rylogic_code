//***************************************************************************************************
// Ldr Object Manager
//  Copyright © Rylogic Ltd 2009
//***************************************************************************************************

#pragma once
#ifndef PR_LDR_OBJECT_MANAGER_DLG_H
#define PR_LDR_OBJECT_MANAGER_DLG_H

#include "pr/common/min_max_fix.h"
#include <string>
#include <set>
#include <string>
#include <sstream>
#include <windows.h>
#include <shellapi.h>
#include <atlbase.h>
#include <atlapp.h>
#include <atlwin.h>
#include <atlframe.h>
#include <atlctrls.h>
#include <atlctrlx.h>
#include <atlctrlw.h>
#include <atlsplit.h>
#include <atldlgs.h>
#include <atlmisc.h>
#include <atlcrack.h>
#include "pr/common/events.h"
#include "pr/linedrawer/ldr_object.h"
	
namespace pr
{
	namespace ldr
	{
		extern HTREEITEM const INVALID_TREE_ITEM;
		extern int const       INVALID_LIST_ITEM;
		
		namespace ETriState
		{
			enum Type { Off, On, Toggle };
		}

		//TODO, this should use the pImpl pattern and hide all the windows/atl includes in the cpp

		// An interface for modifying the LdrObjects in existence.
		// LdrObject is completely unaware that this class exists.
		// Note: this object does not add references to LdrObjects
		class ObjectManagerDlg
			:public CIndirectDialogImpl<ObjectManagerDlg>
			,public CDialogResize<ObjectManagerDlg>
			,pr::events::IRecv<pr::ldr::Evt_LdrObjectAdd>
			,pr::events::IRecv<pr::ldr::Evt_DeleteAll>
			,pr::events::IRecv<pr::ldr::Evt_LdrObjectDelete>
		{
			HWND                    m_parent;               // Parent window
			WTL::CStatusBarCtrl     m_status;               // The status bar
			WTL::CSplitterWindow    m_split;                // Splitter window
			WTL::CTreeViewCtrl      m_tree;                 // Tree control
			WTL::CListViewCtrl      m_list;                 // List control
			WTL::CButton            m_btn_expand_all;       // Expand all button
			WTL::CButton            m_btn_collapse_all;     // Collapse all button
			WTL::CEdit              m_filter;               // Object filter
			WTL::CButton            m_btn_apply_filter;     // Apply filter button
			std::set<ContextId>     m_ignore_ctxids;        // Context ids not to display in the object manager
			mutable pr::BoundingBox m_scene_bbox;           // A cached bounding box of all objects we know about (lazy updated)
			bool                    m_expanding;            // True during a recursive expansion of a node in the tree view
			bool                    m_selection_changed;    // Dirty flag for the selection bbox/object
			bool                    m_suspend_layout;       // True while a block of changes are occurring.
			// Selected objects
			// Selection bbox
			
			ObjectManagerDlg(ObjectManagerDlg const&);
			ObjectManagerDlg& operator=(ObjectManagerDlg const&);
			
		public:
			enum {IDC_EXPAND=1000, IDC_COLLAPSE, IDC_FILTER_TEXT, IDC_FILTER, IDC_SPLITTER, IDC_TREE, IDC_LIST, IDC_STATUSBAR};
			enum {ID_HIDEALL=1100, ID_SHOWALL, ID_INV_VIS, ID_SOLIDALL, ID_WIREALL, ID_INV_WIRE, ID_INV_SEL, ID_DETAILED_INFO};
			BEGIN_DIALOG_EX(0, 0, 251, 164, 0)
				DIALOG_STYLE(DS_CENTER|DS_SHELLFONT|WS_CAPTION|WS_GROUP|WS_MAXIMIZEBOX|WS_POPUP|WS_THICKFRAME|WS_SYSMENU)
				DIALOG_EXSTYLE(WS_EX_APPWINDOW)
				DIALOG_CAPTION("Object Manager")
				DIALOG_FONT(8, "MS Shell Dlg")
			END_DIALOG()
			BEGIN_CONTROLS_MAP()
				CONTROL_PUSHBUTTON(TEXT("+"), IDC_EXPAND, 3, 2, 15, 14, 0, 0)
				CONTROL_PUSHBUTTON(TEXT("-"), IDC_COLLAPSE, 22, 2, 15, 14, 0, 0)
				CONTROL_EDITTEXT(IDC_FILTER_TEXT, 40, 2, 160, 14, ES_AUTOHSCROLL, 0)
				CONTROL_PUSHBUTTON(TEXT("Filter"), IDC_FILTER, 201, 2, 48, 14, 0, 0)
				CONTROL_CONTROL(TEXT(""), IDC_TREE, WC_TREEVIEW, WS_TABSTOP | WS_BORDER | TVS_HASBUTTONS | TVS_HASLINES | TVS_LINESATROOT | TVS_EDITLABELS | TVS_DISABLEDRAGDROP | TVS_SHOWSELALWAYS | TVS_FULLROWSELECT | TVS_NOSCROLL, 2, 20, 114, 133, 0)
				CONTROL_CONTROL(TEXT(""), IDC_LIST, WC_LISTVIEW, WS_TABSTOP | WS_BORDER | LVS_ALIGNLEFT | LVS_SHOWSELALWAYS | LVS_EDITLABELS | LVS_NOLABELWRAP | LVS_REPORT, 125, 20, 122, 133, 0)
				//CONTROL_CONTROL(TEXT(""), IDC_SPLITTER, CSplitterWindow::GetWndClassName(), CCS_BOTTOM|CCS_ADJUSTABLE|SBARS_SIZEGRIP, 0, 0, 250, 160, 0);
				CONTROL_CONTROL(TEXT(""), IDC_STATUSBAR, STATUSCLASSNAME, CCS_BOTTOM|CCS_ADJUSTABLE|SBARS_SIZEGRIP, 0, 150, 250, 80, 0);
			END_CONTROLS_MAP()
			BEGIN_DLGRESIZE_MAP(ObjectManagerDlg)
				DLGRESIZE_CONTROL(IDC_EXPAND      ,0)
				DLGRESIZE_CONTROL(IDC_COLLAPSE    ,0)
				DLGRESIZE_CONTROL(IDC_FILTER_TEXT ,DLSZ_SIZE_X)
				DLGRESIZE_CONTROL(IDC_FILTER      ,DLSZ_MOVE_X)
				DLGRESIZE_CONTROL(IDC_SPLITTER      ,DLSZ_SIZE_X|DLSZ_SIZE_Y)
				DLGRESIZE_CONTROL(IDC_STATUSBAR     ,DLSZ_SIZE_X|DLSZ_MOVE_Y)
			END_DLGRESIZE_MAP()
			BEGIN_MSG_MAP(ObjectManagerDlg)
				MESSAGE_HANDLER(WM_INITDIALOG                       ,OnInitDialog)
				MESSAGE_HANDLER(WM_DESTROY                          ,OnDestDialog)
				MESSAGE_HANDLER(WM_MOUSEWHEEL                       ,OnMouseWheel)
				MESSAGE_HANDLER(WM_EXITSIZEMOVE                     ,OnResized)
				COMMAND_ID_HANDLER(IDOK                             ,OnCloseDialog)
				COMMAND_ID_HANDLER(IDCLOSE                          ,OnCloseDialog)
				COMMAND_ID_HANDLER(IDCANCEL                         ,OnCloseDialog)
				COMMAND_HANDLER(IDC_EXPAND      ,BN_CLICKED         ,OnExpandAll)
				COMMAND_HANDLER(IDC_COLLAPSE    ,BN_CLICKED         ,OnCollapseAll)
				COMMAND_HANDLER(IDC_FILTER_TEXT ,EN_CHANGE          ,OnFilterChanged)
				COMMAND_HANDLER(IDC_FILTER      ,BN_CLICKED         ,OnApplyFilter)
				NOTIFY_HANDLER(IDC_TREE         ,TVN_ITEMEXPANDED   ,OnTreeExpand)
				NOTIFY_HANDLER(IDC_TREE         ,TVN_SELCHANGED     ,OnTreeItemSelected)
				NOTIFY_HANDLER(IDC_TREE         ,NM_DBLCLK          ,OnTreeDblClick)
				NOTIFY_HANDLER(IDC_TREE         ,TVN_KEYDOWN        ,OnTreeKeydown)
				NOTIFY_HANDLER(IDC_LIST         ,LVN_KEYDOWN        ,OnListKeydown)
				NOTIFY_HANDLER(IDC_LIST         ,LVN_ITEMCHANGED    ,OnListItemSelected)
				NOTIFY_HANDLER(IDC_LIST         ,NM_RCLICK          ,OnShowListContextMenu)
				COMMAND_ID_HANDLER(ID_HIDEALL       ,OnChangeVisibility)
				COMMAND_ID_HANDLER(ID_SHOWALL       ,OnChangeVisibility)
				COMMAND_ID_HANDLER(ID_INV_VIS       ,OnChangeVisibility)
				COMMAND_ID_HANDLER(ID_SOLIDALL      ,OnChangeSolidWire)
				COMMAND_ID_HANDLER(ID_WIREALL       ,OnChangeSolidWire)
				COMMAND_ID_HANDLER(ID_INV_WIRE      ,OnChangeSolidWire)
				COMMAND_ID_HANDLER(ID_INV_SEL       ,OnChangeInvertSelection)
				COMMAND_ID_HANDLER(ID_DETAILED_INFO ,OnDetailedInfo)
				CHAIN_MSG_MAP(CDialogResize<ObjectManagerDlg>)
			END_MSG_MAP()
			
			ObjectManagerDlg(HWND parent = 0);
			~ObjectManagerDlg();
			
			// Display the object manager window
			void Show(bool show);
			
			// Display a window containing the example script
			void ShowScript(std::string const& script, HWND parent);
			
			// Get/Set settings for the object manager window
			std::string Settings() const;
			void Settings(char const* settings);
			
			// Set the ignore state for a particular context id
			void IgnoreContextId(ContextId id, bool ignore);
			
			// Handle a key press in either the list or tree view controls
			void HandleKey(int vkey);
			
			// Return the number of selected objects
			pr::uint SelectedCount() const;
			
			// Remove selection from the tree and list controls
			void SelectNone();
			void SelectLdrObject(pr::ldr::LdrObject& object, bool make_visible);
			void InvSelection();
			
			// Return a bounding box of the objects
			pr::BoundingBox GetBBox(EObjectBounds bbox_type) const;
			
			// Set the visibility of the currently selected objects
			void SetVisibilty(ETriState::Type state, bool include_children);
			
			// Set wireframe for the currently selected objects
			void SetWireframe(ETriState::Type state, bool include_children);
			
			// Add/Remove items from the list view based on the filter
			// If the filter is empty the list is re-populated
			void ApplyFilter();
			
			// Handler methods
			LRESULT OnInitDialog(UINT, WPARAM, LPARAM, BOOL&);
			LRESULT OnDestDialog(UINT, WPARAM, LPARAM, BOOL&);
			LRESULT OnResized(UINT, WPARAM, LPARAM, BOOL&);
			LRESULT OnMouseWheel(UINT, WPARAM, LPARAM, BOOL&);
			LRESULT OnCloseDialog(WORD, WORD, HWND, BOOL&);
			LRESULT OnExpandAll(WORD, WORD, HWND, BOOL&);
			LRESULT OnCollapseAll(WORD, WORD, HWND, BOOL&);
			LRESULT OnFilterChanged(WORD, WORD, HWND, BOOL&);
			LRESULT OnApplyFilter(WORD, WORD, HWND, BOOL&);
			LRESULT OnTreeExpand(WPARAM, LPNMHDR, BOOL&);
			LRESULT OnTreeItemSelected(WPARAM, LPNMHDR, BOOL&);
			LRESULT OnTreeKeydown(WPARAM, LPNMHDR, BOOL&);
			LRESULT OnTreeDblClick(WPARAM, LPNMHDR, BOOL&);
			LRESULT OnListKeydown(WPARAM, LPNMHDR, BOOL&);
			LRESULT OnListItemSelected(WPARAM, LPNMHDR, BOOL&);
			LRESULT OnShowListContextMenu(WPARAM, LPNMHDR, BOOL&);
			LRESULT OnChangeVisibility(WORD, WORD, HWND, BOOL&);
			LRESULT OnChangeSolidWire(WORD, WORD, HWND, BOOL&);
			LRESULT OnChangeInvertSelection(WORD, WORD, HWND, BOOL&);
			LRESULT OnDetailedInfo(WORD, WORD, HWND, BOOL&);
			
		private:
			// Event handlers
			void OnEvent(pr::ldr::Evt_LdrObjectAdd const&);
			void OnEvent(pr::ldr::Evt_DeleteAll const&);
			void OnEvent(pr::ldr::Evt_LdrObjectDelete const&);
		};
	}
}

// Add a manifest dependency on common controls version 6
#if defined _M_IX86
#   pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='x86' publicKeyToken='6595b64144ccf1df' language='*'\"")
#elif defined _M_IA64
#   pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='ia64' publicKeyToken='6595b64144ccf1df' language='*'\"")
#elif defined _M_X64
#   pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='amd64' publicKeyToken='6595b64144ccf1df' language='*'\"")
#else
#   pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='*' publicKeyToken='6595b64144ccf1df' language='*'\"")
#endif

#endif
