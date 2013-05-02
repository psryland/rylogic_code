//***************************************************************************************************
// Ldr Object Manager
//  Copyright © Rylogic Ltd 2009
//***************************************************************************************************

#include "pr/linedrawer/ldr_objects_dlg.h"
#include "pr/macros/count_of.h"
#include "pr/common/assert.h"
#include "pr/common/keystate.h"
#include "pr/script/reader.h"
#include "pr/linedrawer/ldr_object.h"
	
namespace pr
{
	namespace ldr
	{
		HTREEITEM const INVALID_TREE_ITEM =  0;
		int const       INVALID_LIST_ITEM = -1;
	}
}
namespace EColumn
{
	enum Type
	{
		Name,
		LdrType,
		Colour,
		Visible,
		Wireframe,
		Volume,
		CtxtId,
		NumberOf
	};
	inline char const* ToString(EColumn::Type type)
	{
		switch (type)
		{
		default:        return "";
		case Name:      return "Name";
		case LdrType:   return "Type";
		case Colour:    return "Colour";
		case Visible:   return "Visible";
		case Wireframe: return "Wireframe";
		case Volume:    return "Volume";
		case CtxtId:    return "ContextId";
		}
	}
}
	
// Return the LdrObject associated with a tree item or list item
inline pr::ldr::LdrObject& GetLdrObject(CTreeViewCtrl const& tree, HTREEITEM item)
{
	PR_ASSERT(PR_DBG_LDROBJMGR, item != pr::ldr::INVALID_TREE_ITEM && tree.GetItemData(item) != 0, "Tree item does not refer to an LdrObject");
	return *reinterpret_cast<pr::ldr::LdrObject*>(tree.GetItemData(item));
}
inline pr::ldr::LdrObject& GetLdrObject(CListViewCtrl const& list, int item)
{
	PR_ASSERT(PR_DBG_LDROBJMGR, item != pr::ldr::INVALID_LIST_ITEM && list.GetItemData(item) != 0, "List item does not refer to an LdrObject");
	return *reinterpret_cast<pr::ldr::LdrObject*>(list.GetItemData(item));
}
	
// For each object in the list from 'start_index' to the end, set the list index
// in the object uidata. The list control uses contiguous memory so we have to do
// this whenever objects are inserted/deleted from the list
inline void FixListCtrlReferences(CListViewCtrl& list, int start_index)
{
	// 'start_index'==-1 means all list items
	if (start_index < 0) start_index = 0;
	for (int i = start_index, iend = list.GetItemCount(); i < iend; ++i)
		GetLdrObject(list, i).m_uidata.m_list_item = i;
}
	
// Update the displayed properties in the list
inline void UpdateListItem(CListViewCtrl& list, pr::ldr::LdrObject& object, bool recursive)
{
	if (object.m_uidata.m_list_item == pr::ldr::INVALID_LIST_ITEM) return;
	list.SetItemText(object.m_uidata.m_list_item ,EColumn::Name      ,object.m_name.c_str());
	list.SetItemText(object.m_uidata.m_list_item ,EColumn::LdrType   ,pr::ldr::ELdrObject::ToString(object.m_type));
	list.SetItemText(object.m_uidata.m_list_item ,EColumn::Colour    ,pr::FmtS("%X", object.m_colour.m_aarrggbb));
	list.SetItemText(object.m_uidata.m_list_item ,EColumn::Visible   ,object.m_visible ? "Visible"   : "Hidden");
	list.SetItemText(object.m_uidata.m_list_item ,EColumn::Wireframe ,object.m_wireframe ? "Wireframe" : "Solid");
	list.SetItemText(object.m_uidata.m_list_item ,EColumn::Volume    ,pr::FmtS("%3.3f", Volume(object.BBoxMS(false))));
	list.SetItemText(object.m_uidata.m_list_item ,EColumn::CtxtId    ,pr::FmtS("%d", object.m_context_id));
	if (!recursive) return;
	for (pr::ldr::ObjectCont::iterator c = object.m_child.begin(), cend = object.m_child.end(); c != cend; ++c)
		UpdateListItem(list, *(*c).m_ptr, recursive);
}
	
// Recursively add 'obj' and its children to the tree and list control
void AddToUI(CTreeViewCtrl& tree, CListViewCtrl& list, pr::ldr::LdrObject* obj, pr::ldr::LdrObject* prev)
{
	PR_ASSERT(PR_DBG_LDROBJMGR, obj, "Attempting to add a null object to the UI");
	PR_ASSERT(PR_DBG_LDROBJMGR, !obj->m_parent || obj->m_parent->m_uidata.m_tree_item != pr::ldr::INVALID_TREE_ITEM, "Parent is not in the tree");
	
	// Add the item to the tree
	obj->m_uidata.m_tree_item = tree.InsertItem(obj->m_name.c_str(),
								obj->m_parent ? obj->m_parent->m_uidata.m_tree_item : TVI_ROOT,
								prev          ? prev->m_uidata.m_tree_item          : TVI_LAST);
								
	// Save a pointer to this object in the tree
	if (obj->m_uidata.m_tree_item != pr::ldr::INVALID_TREE_ITEM)
		tree.SetItemData(obj->m_uidata.m_tree_item, reinterpret_cast<DWORD_PTR>(obj));
	else {}
	// todo: Report errors, without spamming the user...
	
	// Add the item to the list
	// If 'obj' is a top level object, then add it to the list
	if (obj->m_parent == 0)
	{
		obj->m_uidata.m_list_item = list.InsertItem(list.GetItemCount(), obj->m_name.c_str());
		if (obj->m_uidata.m_list_item == pr::ldr::INVALID_LIST_ITEM) {}
		// todo: Report errors, without spamming the user...
	}
	// Otherwise, if 'prev' is visible in the list then display 'obj' in the list as well
	else if (prev && prev->m_uidata.m_list_item != pr::ldr::INVALID_LIST_ITEM)
	{
		obj->m_uidata.m_list_item = list.InsertItem(prev->m_uidata.m_list_item + 1, obj->m_name.c_str());
		if (obj->m_uidata.m_list_item == pr::ldr::INVALID_LIST_ITEM) {}
		// todo: Report errors, without spamming the user...
	}
	else
	{
		obj->m_uidata.m_list_item = pr::ldr::INVALID_LIST_ITEM;
	}
	
	// Save a pointer to this object in the list
	if (obj->m_uidata.m_list_item != pr::ldr::INVALID_LIST_ITEM)
	{
		list.SetItemData(obj->m_uidata.m_list_item, reinterpret_cast<DWORD_PTR>(obj));
		UpdateListItem(list, *obj, false);
	}
	
	// Add the children
	pr::ldr::LdrObject* p = 0;
	for (pr::ldr::ObjectCont::iterator c = obj->m_child.begin(), cend = obj->m_child.end(); c != cend; p = (*c).m_ptr, ++c)
		AddToUI(tree, list, (*c).m_ptr, p);
}
	
// Recursively remove 'obj' and its children from the tree control.
// Note that objects are not deleted from the ObjectManager
void RemoveFromUI(CTreeViewCtrl& tree, CListViewCtrl& list, pr::ldr::LdrObject* obj)
{
	// Recursively delete children in reverse order to prevent corrupting list control indices
	for (std::size_t c = obj->m_child.size(); c != 0; --c)
		RemoveFromUI(tree, list, obj->m_child[c - 1].m_ptr);
		
	// If the object is in the list, remove it. We'll fix up the list
	// references after all children of 'obj' have been removed.
	if (obj->m_uidata.m_list_item != pr::ldr::INVALID_LIST_ITEM)
	{
		list.DeleteItem(obj->m_uidata.m_list_item);
		obj->m_uidata.m_list_item = pr::ldr::INVALID_LIST_ITEM;
	}
	
	// Remove it from the tree.
	tree.DeleteItem(obj->m_uidata.m_tree_item);
	obj->m_uidata.m_tree_item = pr::ldr::INVALID_TREE_ITEM;
}
	
// Recursively collapse objects in the tree.
// Depth-first so that we can remove items from the list control at the same time
void CollapseRecursive(CTreeViewCtrl& tree, CListViewCtrl& list, pr::ldr::LdrObject* object)
{
	std::size_t child_count = object->m_child.size();
	for (std::size_t c = child_count; c != 0; --c)
	{
		pr::ldr::LdrObject* child = object->m_child[c - 1].m_ptr;
		CollapseRecursive(tree, list, child);
		
		// Remove this child from the list control
		if (child->m_uidata.m_list_item != pr::ldr::INVALID_LIST_ITEM)
		{
			list.DeleteItem(child->m_uidata.m_list_item);
			child->m_uidata.m_list_item = pr::ldr::INVALID_LIST_ITEM;
		}
	}
	
	// Collapse this tree item
	tree.Expand(object->m_uidata.m_tree_item, TVE_COLLAPSE);
}
	
// Collapse 'object' and its children in 'tree'.
// Remove 'object's children from the 'list'
void Collapse(CTreeViewCtrl& tree, CListViewCtrl& list, pr::ldr::LdrObject* object)
{
	CollapseRecursive(tree, list, object);
	
	// Fix the indices of the remaining list members
	FixListCtrlReferences(list, object->m_uidata.m_list_item);
}
	
// Expand this object. If 'all_children' is true, expand all of its children.
// Add all children to the list control if the parent is in the list control.
void ExpandRecursive(CTreeViewCtrl& tree, CListViewCtrl& list, pr::ldr::LdrObject* object, bool all_children, int& list_position)
{
	std::size_t child_count = object->m_child.size();
	for (std::size_t c = 0; c != child_count; ++c)
	{
		pr::ldr::LdrObject* child = object->m_child[c].m_ptr;
		
		// Add this child to the list control
		if (object->m_uidata.m_list_item != pr::ldr::INVALID_LIST_ITEM &&
			 child->m_uidata.m_list_item == pr::ldr::INVALID_LIST_ITEM)
		{
			child->m_uidata.m_list_item = list_position;
			list.InsertItem(list_position, child->m_name.c_str());
			list.SetItemData(list_position, reinterpret_cast<DWORD_PTR>(child));
			UpdateListItem(list, *child, false);
			++list_position;
		}
		
		if (all_children) ExpandRecursive(tree, list, child, all_children, list_position);
	}
	
	// Expand this tree item
	tree.Expand(object->m_uidata.m_tree_item, TVE_EXPAND);
}
	
// Expand 'object' in the tree and add its children to 'list'
void Expand(CTreeViewCtrl& tree, CListViewCtrl& list, pr::ldr::LdrObject* object, bool recursive, bool& expanding)
{
	// Calling tree.Expand causes notification messages to be sent,
	// Believe me, I've try to find a better solution, this is the best I could do
	// after several days :-/. But hey, it works.
	if (!expanding)
	{
		expanding = true;
		int list_position = object->m_uidata.m_list_item + 1;
		ExpandRecursive(tree, list, object, recursive, list_position);
		expanding = false;
	}
	
	// Fix the indices of the remaining list members
	FixListCtrlReferences(list, object->m_uidata.m_list_item + 1);
}
	
// Recursively perform 'func' on 'object' and its children
template <typename Func> void RecursiveDo(pr::ldr::LdrObject* object, Func func)
{
	func(object);
	for (ObjectCont::iterator i = object->m_child.begin(), iend = object->m_child.end(); i != iend; ++i)
		RecursiveDo(*i, func);
}
	
// ObjectManagerDlg *********************************************************
	
pr::ldr::ObjectManagerDlg::ObjectManagerDlg(HWND parent)
:m_parent(parent)
,m_status()
,m_split()
,m_tree()
,m_list()
,m_btn_expand_all()
,m_btn_collapse_all()
,m_filter()
,m_btn_apply_filter()
,m_scene_bbox(pr::BBoxReset)
,m_expanding(false)
,m_selection_changed(true)
,m_suspend_layout(false)
{
	if (Create(0) == 0)
		throw std::exception("Failed to create object manager ui");
}
pr::ldr::ObjectManagerDlg::~ObjectManagerDlg()
{
	if (IsWindow())
		DestroyWindow();
}
	
// Display the window
void pr::ldr::ObjectManagerDlg::Show(bool show)
{
	ShowWindow(show ? SW_SHOW : SW_HIDE);
	if (show) SetWindowPos(HWND_TOP,0,0,0,0,SWP_NOMOVE|SWP_NOSIZE); // bring to front
	for (int i = 0; i != EColumn::NumberOf; ++i)
		m_list.SetColumnWidth(i, LVSCW_AUTOSIZE);
}
	
// Display a window containing the 'script'
void pr::ldr::ObjectManagerDlg::ShowScript(std::string const& script, HWND parent)
{
	class ScriptWindow
		:public CIndirectDialogImpl<ScriptWindow>
		,public CDialogResize<ScriptWindow>
	{
		std::string const* m_text;
		CEdit m_info;
		CFont m_font;
		
	public:
		ScriptWindow(std::string const& text) :m_text(&text) {}
		
		enum { IDC_TEXT = 1000 };
		BEGIN_DIALOG_EX(0, 0, 500, 490, 0)
			DIALOG_STYLE(DS_CENTER|WS_CAPTION|WS_POPUP|WS_THICKFRAME|WS_MINIMIZEBOX|WS_MAXIMIZEBOX|WS_SYSMENU|WS_VISIBLE)
			DIALOG_CAPTION("Example Script:")
			DIALOG_FONT(8, TEXT("MS Shell Dlg"))
		END_DIALOG()
		BEGIN_CONTROLS_MAP()
			CONTROL_EDITTEXT(IDC_TEXT, 0, 0, 500, 490, WS_HSCROLL|WS_VSCROLL|ES_AUTOHSCROLL|ES_AUTOVSCROLL|ES_MULTILINE|ES_WANTRETURN, WS_EX_STATICEDGE)//NOT WS_BORDER|
		END_CONTROLS_MAP()
		BEGIN_DLGRESIZE_MAP(ScriptWindow)
			DLGRESIZE_CONTROL(IDC_TEXT, DLSZ_SIZE_X|DLSZ_SIZE_Y|DLSZ_REPAINT)
		END_DLGRESIZE_MAP()
		BEGIN_MSG_MAP(ScriptWindow)
			MSG_WM_INITDIALOG(OnInitDialog)
			COMMAND_ID_HANDLER_EX(IDCANCEL ,OnClose)
			CHAIN_MSG_MAP(CDialogResize<ScriptWindow>)
		END_MSG_MAP()
		
	private:
		// Handler methods
		BOOL OnInitDialog(CWindow, LPARAM)
		{
			CenterWindow(GetParent());
			m_font.CreatePointFont(80, "courier new");
			m_info.Attach(GetDlgItem(IDC_TEXT));
			m_info.SetTabStops(12);
			m_info.SetFont(m_font);
			m_info.SetWindowText(m_text->c_str());
			m_info.SetSelNone();
			DlgResize_Init();
			return TRUE;
		}
		void OnClose(UINT, int nID, CWindow)
		{
			EndDialog(nID);
		}
	};
	
	std::string text = script;
	pr::str::Replace(text, "\n", "\r\n");
	ScriptWindow example(text);
	example.DoModal(parent);
}
	
// Export settings for the object manager window
std::string pr::ldr::ObjectManagerDlg::Settings() const
{
	CRect wrect;
	GetWindowRect(&wrect);
	
	std::stringstream out;
	out << "*WindowPos "<<wrect.left<<" "<<wrect.top<<" "<<wrect.right<<" "<<wrect.bottom<<" "
		<< "*SplitterPos "<<m_split.GetSplitterPosPct()<<" ";
	return out.str();
}
	
// Import settings for the object manager window
void pr::ldr::ObjectManagerDlg::Settings(char const* settings)
{
	try
	{
		// Parse the settings
		pr::script::Reader reader;
		pr::script::PtrSrc src(settings);
		reader.AddSource(src);
		for (pr::script::string kw; reader.NextKeywordS(kw);)
		{
			if (pr::str::EqualI(kw, "WindowPos"))
			{
				CRect wrect;
				reader.ExtractInt(wrect.left   ,10);
				reader.ExtractInt(wrect.top    ,10);
				reader.ExtractInt(wrect.right  ,10);
				reader.ExtractInt(wrect.bottom ,10);
				MoveWindow(&wrect);
				continue;
			}
			if (pr::str::EqualI(kw, "SplitterPos"))
			{
				int pos; reader.ExtractInt(pos, 10);
				m_split.SetSplitterPosPct(pos);
				continue;
			}
		}
	}
	catch (pr::script::Exception const& e)
	{
		pr::script::string msg = "LdrObjectManager Settings Error\n" + e.msg();
		::MessageBoxA(::GetFocus(), msg.c_str(), "LdrObjectManager Settings Error", MB_OK);
	}
}
	
// Set the ignore state for a particular context id
// Should be called before objects are added to the obj mgr
void pr::ldr::ObjectManagerDlg::IgnoreContextId(ContextId id, bool ignore)
{
	if (ignore) m_ignore_ctxids.insert(id);
	else        m_ignore_ctxids.erase(id);
}
	
// Handle a key press in either the list or tree view controls
void pr::ldr::ObjectManagerDlg::HandleKey(int vkey)
{
	switch (tolower(vkey))
	{
	default:break;
	case VK_ESCAPE:
	{ BOOL h; OnCloseDialog(0,0,0,h); }
	break;
	case 'a':
		if (pr::KeyDown(VK_CONTROL)) { SelectNone(); InvSelection(); }
		//else OnBnClickedButtonToggleAlpha();
		break;
	case 'w':
		SetWireframe(ETriState::Toggle, !pr::KeyDown(VK_SHIFT));
		break;
	case ' ':
		SetVisibilty(ETriState::Toggle, !pr::KeyDown(VK_SHIFT));
		break;
	case VK_DELETE:
		//pr::events::Send(LdrObjMgr_DeleteObjectRequest());
		break;
	case VK_F6:
		m_filter.SetFocus();
		m_filter.SetSelAll();
		break;
	}
}
	
// Remove selection from the tree and list controls
void pr::ldr::ObjectManagerDlg::SelectNone()
{
	for (int i = m_list.GetNextItem(-1, LVNI_SELECTED); i != -1; i = m_list.GetNextItem(i, LVNI_SELECTED))
		m_list.SetItemState(i, 0, LVIS_SELECTED);
}
	
// Select an ldr object
void pr::ldr::ObjectManagerDlg::SelectLdrObject(pr::ldr::LdrObject& object, bool make_visible)
{
	// Select in the tree
	m_tree.SetItemState(object.m_uidata.m_tree_item, TVIS_SELECTED, TVIS_SELECTED);
	if (make_visible) m_tree.EnsureVisible(object.m_uidata.m_tree_item);
	
	// Select in the list and make visible
	if (object.m_uidata.m_list_item != INVALID_LIST_ITEM)
	{
		m_list.SetItemState(object.m_uidata.m_list_item, LVIS_SELECTED, LVIS_SELECTED);
		if (make_visible) m_list.EnsureVisible(object.m_uidata.m_list_item, FALSE);
	}
	
	// Flag the selection data as invalid
	m_selection_changed = true;
	pr::events::Send(pr::ldr::Evt_LdrObjectSelectionChanged());
}
	
// Invert the selection from the tree and list controls
void pr::ldr::ObjectManagerDlg::InvSelection()
{
	for (int i = m_list.GetNextItem(-1, LVNI_ALL); i != -1; i = m_list.GetNextItem(i, LVNI_ALL))
		m_list.SetItemState(i, ~m_list.GetItemState(i, LVIS_SELECTED), LVIS_SELECTED);
}
	
// Return the number of selected objects
pr::uint pr::ldr::ObjectManagerDlg::SelectedCount() const
{
	return m_list.GetSelectedCount();
}
	
// Return a bounding box of the objects
inline bool LdrObjectIsVisible(pr::ldr::LdrObject const& obj) { return obj.m_visible; }
pr::BoundingBox pr::ldr::ObjectManagerDlg::GetBBox(EObjectBounds bbox_type) const
{
	pr::BoundingBox bbox = pr::BBoxReset;
	switch (bbox_type)
	{
	default: PR_ASSERT(PR_DBG_LDROBJMGR, false, "Unknown bounding box type requested"); return pr::BBoxUnit;
	case pr::ldr::EObjectBounds::All:
		if (m_scene_bbox == pr::BBoxReset)
		{
			m_scene_bbox = pr::BBoxReset;
			for (HTREEITEM i = m_tree.GetRootItem(); i != 0; i = m_tree.GetNextItem(i, TVGN_NEXT))
			{
				pr::BoundingBox bb = GetLdrObject(m_tree, i).BBoxWS(true);
				if (bb.IsValid()) pr::Encompase(m_scene_bbox, bb);
			}
		}
		bbox = m_scene_bbox;
		break;
		
	case pr::ldr::EObjectBounds::Selected:
		for (int i = m_list.GetNextItem(-1, LVNI_SELECTED); i != -1; i = m_list.GetNextItem(i, LVNI_SELECTED))
		{
			pr::BoundingBox bb = GetLdrObject(m_list, i).BBoxWS(true);
			if (bb.IsValid()) pr::Encompase(bbox, bb);
		}
		break;
		
	case pr::ldr::EObjectBounds::Visible:
		for (HTREEITEM i = m_tree.GetNextItem(0, TVGN_NEXT); i != 0; i = m_tree.GetNextItem(i, TVGN_NEXT))
		{
			pr::BoundingBox bb = GetLdrObject(m_tree, i).BBoxWS(true, LdrObjectIsVisible);
			if (bb.IsValid()) pr::Encompase(bbox, bb);
		}
		break;
	}
	return bbox.IsValid() ? bbox : pr::BBoxUnit;
}
	
// Set the visibility of the currently selected objects
void pr::ldr::ObjectManagerDlg::SetVisibilty(ETriState::Type state, bool include_children)
{
	for (int i = m_list.GetNextItem(-1, LVNI_SELECTED); i != -1; i = m_list.GetNextItem(i, LVNI_SELECTED))
	{
		pr::ldr::LdrObject& object = GetLdrObject(m_list, i);
		object.Visible(state == ETriState::Off ? false : state == ETriState::On ? true : !object.m_visible, include_children);
		UpdateListItem(m_list, object, include_children);
	}
	pr::events::Send(pr::ldr::Evt_Refresh());
}
	
// Set wireframe for the currently selected objects
void pr::ldr::ObjectManagerDlg::SetWireframe(ETriState::Type state, bool include_children)
{
	for (int i = m_list.GetNextItem(-1, LVNI_SELECTED); i != -1; i = m_list.GetNextItem(i, LVNI_SELECTED))
	{
		pr::ldr::LdrObject& object = GetLdrObject(m_list, i);
		object.Wireframe(state == ETriState::Off ? false : state == ETriState::On ? true : !object.m_wireframe, include_children);
		UpdateListItem(m_list, object, include_children);
	}
	pr::events::Send(pr::ldr::Evt_Refresh());
}
	
// Add/Remove items from the list view based on the filter
void pr::ldr::ObjectManagerDlg::ApplyFilter()
{
	// If the filter edit box is not empty then remove all that aren't selected
	if (m_filter.GetWindowTextLength() != 0)
	{
		for (int i = m_list.GetItemCount() - 1; i != -1; --i)
		{
			// Delete all non-selected items
			if ((m_list.GetItemState(i, LVIS_SELECTED) & LVIS_SELECTED) == 0)
			{
				GetLdrObject(m_list,i).m_uidata.m_list_item = pr::ldr::INVALID_LIST_ITEM;
				m_list.DeleteItem(i);
			}
		}
		FixListCtrlReferences(m_list, 0);
	}
	// Else, remove all items from the list and re-add them based on whats displayed in the tree
	else
	{
		// Remove all items from the list
		for (int i = m_list.GetNextItem(-1, LVNI_ALL); i != -1; i = m_list.GetNextItem(i, LVNI_ALL))
			GetLdrObject(m_list,i).m_uidata.m_list_item = pr::ldr::INVALID_LIST_ITEM;
		m_list.DeleteAllItems();
		
		// Re-add items based on what's displayed in the tree
		int list_position = 0;
		for (HTREEITEM i = m_tree.GetRootItem(); i != 0; i = m_tree.GetNextItem(i, TVGN_NEXTVISIBLE), ++list_position)
		{
			pr::ldr::LdrObject& object = GetLdrObject(m_tree,i);
			
			// Add a list item for this tree item
			object.m_uidata.m_list_item = list_position;
			m_list.InsertItem(list_position, object.m_name.c_str());
			m_list.SetItemData(list_position, reinterpret_cast<DWORD_PTR>(&object));
			UpdateListItem(m_list, object, false);
		}
	}
}
	
// Handler methods ************************
	
// First display of the object manager window
LRESULT pr::ldr::ObjectManagerDlg::OnInitDialog(UINT, WPARAM, LPARAM, BOOL&)
{
	InitCommonControls(); // remember to link to comctl32.lib
	
	CRect client_rect; GetClientRect(&client_rect); client_rect.InflateRect(-4, -4);
	CRect btn_exp_rect = client_rect; btn_exp_rect.bottom = btn_exp_rect.top + 20; btn_exp_rect.right = btn_exp_rect.left + 20;
	CRect btn_col_rect = client_rect; btn_col_rect.bottom = btn_col_rect.top + 20; btn_col_rect.left = btn_exp_rect.right + 2; btn_col_rect.right = btn_col_rect.left + 20;
	CRect btn_af_rect  = client_rect; btn_af_rect.bottom = btn_af_rect.top + 20; btn_af_rect.left = btn_af_rect.right - 100; btn_af_rect.right -= 2;
	CRect filtr_rect   = client_rect; filtr_rect.bottom = filtr_rect.top + 20; filtr_rect.left = btn_col_rect.right + 2; filtr_rect.right = btn_af_rect.left - 2;
	CRect split_rect   = client_rect; split_rect.top += filtr_rect.Height() + 2; split_rect.bottom -= 23;
	
	// Attach controls. Note: creation order defines tab order
	m_split             .Create(m_hWnd ,&split_rect ,0 ,WS_CHILD|WS_VISIBLE|WS_CLIPSIBLINGS|WS_CLIPCHILDREN|WS_GROUP|WS_TABSTOP ,0 ,IDC_SPLITTER);
	m_status            .Attach(GetDlgItem(IDC_STATUSBAR));
	m_btn_expand_all    .Attach(GetDlgItem(IDC_EXPAND));      m_btn_expand_all  .MoveWindow(&btn_exp_rect);
	m_btn_collapse_all  .Attach(GetDlgItem(IDC_COLLAPSE));    m_btn_collapse_all.MoveWindow(&btn_col_rect);
	m_filter            .Attach(GetDlgItem(IDC_FILTER_TEXT)); m_filter          .MoveWindow(&filtr_rect);
	m_btn_apply_filter  .Attach(GetDlgItem(IDC_FILTER));      m_btn_apply_filter.MoveWindow(&btn_af_rect);
	m_tree              .Attach(GetDlgItem(IDC_TREE));
	m_list              .Attach(GetDlgItem(IDC_LIST));
	m_tree.SetParent(m_split);
	m_list.SetParent(m_split);
	
	// Use the same font as the list
	HFONT hfont = m_list.GetFont();
	m_btn_expand_all.SetFont(hfont);
	m_btn_collapse_all.SetFont(hfont);
	m_btn_apply_filter.SetFont(hfont);
	m_filter.SetFont(hfont);
	
	int status_panes[] = {-1};
	m_status.SetParts(PR_COUNTOF(status_panes), status_panes);
	
	for (int i = 0; i != EColumn::NumberOf; ++i)
		m_list.AddColumn(EColumn::ToString(EColumn::Type(i)), i);
		
	m_btn_expand_all.SetWindowText("+");
	m_btn_collapse_all.SetWindowText("-");
	m_btn_apply_filter.SetWindowText("Filter");
	m_split.SetSplitterPanes(m_tree.m_hWnd, m_list.m_hWnd);
	m_split.SetSplitterPosPct(30);
	m_split.m_cxySplitBar = 4;
	
	int tree_style = 0|TVS_EX_AUTOHSCROLL|TVS_EX_FADEINOUTEXPANDOS;
	m_tree.SetExtendedStyle(tree_style, tree_style);
	m_tree.ModifyStyle(TVS_EDITLABELS, 0);
	
	int list_style = 0|LVS_EX_HEADERDRAGDROP|LVS_EX_FULLROWSELECT|LVS_EX_TWOCLICKACTIVATE|LVS_EX_FLATSB|LVS_EX_AUTOSIZECOLUMNS;
	m_list.SetExtendedListViewStyle(list_style);
	m_list.ModifyStyle(LVS_EDITLABELS, 0);
	
	DlgResize_Init();
	ShowWindow(SW_HIDE);
	return S_OK;
}
	
// Clean up the object manager windows
LRESULT pr::ldr::ObjectManagerDlg::OnDestDialog(UINT, WPARAM, LPARAM, BOOL&)
{
	return S_OK;
}
	
// Notify listeners that the settings have changed
LRESULT pr::ldr::ObjectManagerDlg::OnResized(UINT, WPARAM, LPARAM, BOOL&)
{
	pr::events::Send(Evt_SettingsChanged());
	return S_OK;
}
	
// Use hover scrolling for the tree and list views
LRESULT pr::ldr::ObjectManagerDlg::OnMouseWheel(UINT msg, WPARAM wparam, LPARAM lparam, BOOL& handled)
{
	POINT pt = {GET_X_LPARAM(lparam), GET_Y_LPARAM(lparam)};
	CRect rect;
	
	m_tree.GetWindowRect(&rect);
	if (rect.PtInRect(pt))  { SendMessage(m_tree.m_hWnd, msg, wparam, lparam); handled = TRUE; return S_OK; }
	
	m_list.GetWindowRect(&rect);
	if (rect.PtInRect(pt))  { SendMessage(m_list.m_hWnd, msg, wparam, lparam); handled = TRUE; return S_OK; }
	
	handled = FALSE;
	return S_OK;
}
	
// Handle close window events by just hiding the window
LRESULT pr::ldr::ObjectManagerDlg::OnCloseDialog(WORD, WORD, HWND, BOOL&)
{
	pr::events::Send(Evt_SettingsChanged());
	ShowWindow(SW_HIDE);
	return S_OK;
}
	
// Expand all currently visible plus signs in the tree view (or everything if shift is pressed)
LRESULT pr::ldr::ObjectManagerDlg::OnExpandAll(WORD, WORD, HWND, BOOL&)
{
	bool include_children = pr::KeyDown(VK_SHIFT);
	for (HTREEITEM i = m_tree.GetRootItem(); i != 0;)
	{
		HTREEITEM j = i;
		i = m_tree.GetNextItem(i, TVGN_NEXTVISIBLE);
		Expand(m_tree, m_list, &GetLdrObject(m_tree,j), include_children, m_expanding);
	}
	return S_OK;
}
	
// Collapse everything in the tree view
LRESULT pr::ldr::ObjectManagerDlg::OnCollapseAll(WORD, WORD, HWND, BOOL&)
{
	for (HTREEITEM i = m_tree.GetRootItem(); i != 0; i = m_tree.GetNextSiblingItem(i))
		Collapse(m_tree, m_list, &GetLdrObject(m_tree,i));
	return S_OK;
}
	
// Handle selected objects based on the list view filter
LRESULT pr::ldr::ObjectManagerDlg::OnFilterChanged(WORD, WORD, HWND, BOOL&)
{
	char filter[256];
	m_filter.GetWindowText(filter, PR_COUNTOF(filter));
	
	// Select all items that match the filter
	char buf[256];
	for (int i = m_list.GetNextItem(-1, LVNI_ALL); i != -1; i = m_list.GetNextItem(i, LVNI_ALL))
	{
		int count = m_list.GetItemText(i, EColumn::Name, buf, 256);
		bool match = pr::str::FindStrNoCase(&buf[0], &buf[count], filter) != &buf[count];
		m_list.SetItemState(i, int(match)*LVIS_SELECTED, LVIS_SELECTED);
	}
	return S_OK;
}
	
// Apply the current filter to the list
LRESULT pr::ldr::ObjectManagerDlg::OnApplyFilter(WORD, WORD, HWND, BOOL&)
{
	ApplyFilter();
	return S_OK;
}
	
// Handle clicks on a plus sign to expand or collapse items in the tree
LRESULT pr::ldr::ObjectManagerDlg::OnTreeExpand(WPARAM, LPNMHDR hdr, BOOL&)
{
	LPNMTREEVIEW tvhdr = reinterpret_cast<LPNMTREEVIEW>(hdr);
	pr::ldr::LdrObject& object = GetLdrObject(m_tree, tvhdr->itemNew.hItem);
	switch (tvhdr->action)
	{
	default:break;
	case TVE_EXPAND:
		Expand(m_tree, m_list, &object, pr::KeyDown(VK_SHIFT), m_expanding);
		break;
	case TVE_COLLAPSE:
		Collapse(m_tree, m_list, &object);
		break;
	}
	return S_OK;
}
	
// Mirror items selected in the tree with those selected in the list
LRESULT pr::ldr::ObjectManagerDlg::OnTreeItemSelected(WPARAM, LPNMHDR hdr, BOOL&)
{
	LPNMTREEVIEW tv = reinterpret_cast<LPNMTREEVIEW>(hdr);
	pr::ldr::LdrObject& object = GetLdrObject(m_tree, tv->itemNew.hItem);
	if (object.m_uidata.m_list_item == pr::ldr::INVALID_LIST_ITEM) return S_OK;
	
	SelectNone();
	SelectLdrObject(object, true);
	return S_OK;
}
	
// Handle double clicks on items in the tree
LRESULT pr::ldr::ObjectManagerDlg::OnTreeDblClick(WPARAM, LPNMHDR, BOOL&)
{
	HTREEITEM i = m_tree.GetSelectedItem();
	if (i == 0) return S_OK;
	
	pr::ldr::LdrObject& object = GetLdrObject(m_tree, i);
	if ((m_tree.GetItemState(i, TVIS_EXPANDED) & TVIS_EXPANDED) == 0)
		Expand(m_tree, m_list, &object, false, m_expanding);
	else
		Collapse(m_tree, m_list, &object);
		
	return S_OK;
}
	
// Handle key events for the tree view
LRESULT pr::ldr::ObjectManagerDlg::OnTreeKeydown(WPARAM, LPNMHDR hdr, BOOL&)
{
	HandleKey(reinterpret_cast<LPNMTVKEYDOWN>(hdr)->wVKey);
	return S_OK;
}
	
// Handle key events for the list view
LRESULT pr::ldr::ObjectManagerDlg::OnListKeydown(WPARAM, LPNMHDR hdr, BOOL&)
{
	HandleKey(reinterpret_cast<LPNMLVKEYDOWN>(hdr)->wVKey);
	return S_OK;
}
	
// Handle list items being selected
LRESULT pr::ldr::ObjectManagerDlg::OnListItemSelected(WPARAM, LPNMHDR hdr, BOOL& bHandled)
{
	if (hdr->code == LVN_ITEMCHANGED)
	{
		NMLISTVIEW* data = reinterpret_cast<NMLISTVIEW*>(hdr);
		if ((data->uNewState ^ data->uOldState) & LVIS_SELECTED) // If the selection has changed
		{
			m_selection_changed = true;
			pr::events::Send(pr::ldr::Evt_LdrObjectSelectionChanged());
			return S_OK;
		}
	}
	bHandled = false;
	return S_OK;
}
	
// Display a context menu when the user right clicks
LRESULT pr::ldr::ObjectManagerDlg::OnShowListContextMenu(WPARAM, LPNMHDR hdr, BOOL&)
{
	POINT pt = reinterpret_cast<LPNMITEMACTIVATE>(hdr)->ptAction;
	
	CMenu vis_menu; vis_menu.CreatePopupMenu();
	vis_menu.AppendMenuA(MF_STRING, ID_HIDEALL, "Hide");
	vis_menu.AppendMenuA(MF_STRING, ID_SHOWALL, "Show");
	vis_menu.AppendMenuA(MF_STRING, ID_INV_VIS, "Flip Visibility");
	
	CMenu wre_menu; wre_menu.CreatePopupMenu();
	wre_menu.AppendMenuA(MF_STRING, ID_SOLIDALL, "Solid");
	wre_menu.AppendMenuA(MF_STRING, ID_WIREALL, "Wireframe");
	wre_menu.AppendMenuA(MF_STRING, ID_INV_WIRE, "Flip Render Mode");
	
	CMenu menu; menu.CreatePopupMenu();
	menu.AppendMenuA(MF_STRING, ID_INV_SEL, "Invert Selection");
	menu.AppendMenuA(MF_POPUP, vis_menu, "Visibilty");
	menu.AppendMenuA(MF_POPUP, wre_menu, "Render Mode");
	menu.AppendMenuA(MF_STRING, ID_DETAILED_INFO, "Detailed Info");
	
	UINT flags = 0;
	if (GetSystemMetrics(SM_MENUDROPALIGNMENT) == 0)    flags |= TPM_LEFTALIGN|TPM_HORPOSANIMATION;
	else                                                flags |= TPM_RIGHTALIGN|TPM_HORNEGANIMATION;
	
	m_list.ClientToScreen(&pt);
	SetForegroundWindow(m_hWnd); // Ensure foreground so the menu doesn't get orphaned.
	menu.TrackPopupMenu(flags, pt.x, pt.y, m_hWnd);
	return S_OK;
}
	
// Change the visibilty of selected objects
LRESULT pr::ldr::ObjectManagerDlg::OnChangeVisibility(WORD, WORD id, HWND, BOOL&)
{
	switch (id)
	{
	default:break;
	case ID_HIDEALL:    SetVisibilty(ETriState::Off     ,!pr::KeyDown(VK_SHIFT)); break;
	case ID_SHOWALL:    SetVisibilty(ETriState::On      ,!pr::KeyDown(VK_SHIFT)); break;
	case ID_INV_VIS:    SetVisibilty(ETriState::Toggle  ,!pr::KeyDown(VK_SHIFT)); break;
	}
	pr::events::Send(pr::ldr::Evt_Refresh());
	return S_OK;
}
	
// Change the render mode of selected objects
LRESULT pr::ldr::ObjectManagerDlg::OnChangeSolidWire(WORD, WORD id, HWND, BOOL&)
{
	switch (id)
	{
	default:break;
	case ID_SOLIDALL:   SetWireframe(ETriState::Off     ,!pr::KeyDown(VK_SHIFT)); break;
	case ID_WIREALL:    SetWireframe(ETriState::On      ,!pr::KeyDown(VK_SHIFT)); break;
	case ID_INV_WIRE:   SetWireframe(ETriState::Toggle  ,!pr::KeyDown(VK_SHIFT)); break;
	}
	pr::events::Send(pr::ldr::Evt_Refresh());
	return S_OK;
}
	
// Invert the selection
LRESULT pr::ldr::ObjectManagerDlg::OnChangeInvertSelection(WORD, WORD, HWND, BOOL&)
{
	for (int i = m_list.GetNextItem(-1, LVNI_ALL); i != -1; i = m_list.GetNextItem(i, LVNI_ALL))
		m_list.SetItemState(i, m_list.GetItemState(i, LVIS_SELECTED) ^ LVIS_SELECTED, LVIS_SELECTED);
	return S_OK;
}
	
// Show detailed info about the first selected object
LRESULT pr::ldr::ObjectManagerDlg::OnDetailedInfo(WORD, WORD, HWND, BOOL&)
{
	int selected = m_list.GetNextItem(-1, LVNI_SELECTED);
	if (selected == -1) return S_OK;
	
	return S_OK;
}
	
// Event handlers *******************************
	
// An object has been added to the data manager
void pr::ldr::ObjectManagerDlg::OnEvent(pr::ldr::Evt_LdrObjectAdd const& e)
{
	PR_ASSERT(PR_DBG_LDROBJMGR, e.m_obj->m_uidata.m_tree_item == INVALID_TREE_ITEM, "This item is already in the UI");
	PR_ASSERT(PR_DBG_LDROBJMGR, e.m_obj->m_uidata.m_list_item == INVALID_LIST_ITEM, "This item is already in the UI");
	
	// Ignore context ids we're not showing in the obj mgr.
	if (m_ignore_ctxids.find(e.m_obj->m_context_id) != m_ignore_ctxids.end())
		return;
		
	// Ignore models that aren't instanced
	if (!e.m_obj->m_instanced)
		return;
		
	// Find the previous sibbling
	pr::ldr::LdrObject* prev = 0;
	if (e.m_obj->m_parent)
	{
		pr::ldr::ObjectCont& children = e.m_obj->m_parent->m_child;
		for (pr::ldr::ObjectCont::iterator i = children.begin(), iend = children.end(); i != iend; ++i)
		{
			if (*i != e.m_obj) continue;
			if (i == children.begin()) break;
			prev = (*(--i)).m_ptr;
			break;
		}
	}
	
	AddToUI(m_tree, m_list, e.m_obj.m_ptr, prev);
	FixListCtrlReferences(m_list, e.m_obj->m_uidata.m_list_item);
	m_scene_bbox = pr::BBoxReset;
}
	
// Empty the tree and list controls, all objects have been deleted
void pr::ldr::ObjectManagerDlg::OnEvent(pr::ldr::Evt_DeleteAll const&)
{
	m_tree.DeleteAllItems();
	m_list.DeleteAllItems();
	m_scene_bbox = pr::BBoxReset;
}
	
// Remove an object from the tree and list controls
void pr::ldr::ObjectManagerDlg::OnEvent(pr::ldr::Evt_LdrObjectDelete const& e)
{
	int list_position = e.m_obj->m_uidata.m_list_item;
	RemoveFromUI(m_tree, m_list, e.m_obj);
	FixListCtrlReferences(m_list, list_position);
	m_scene_bbox = pr::BBoxReset;
}
	
