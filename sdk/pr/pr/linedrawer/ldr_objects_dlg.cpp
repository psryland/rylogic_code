//***************************************************************************************************
// Ldr Object Manager
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************

#include "pr/common/min_max_fix.h"
#include <atlbase.h>
#include <atlapp.h>
#include <atlwin.h>
#include <atlframe.h>
#include <atlctrls.h>
#include <atlsplit.h>
#include <atldlgs.h>
#include <atlmisc.h>
#include <atlcrack.h>
#include "pr/common/assert.h"
#include "pr/common/keystate.h"
#include "pr/macros/enum.h"
#include "pr/macros/count_of.h"
#include "pr/str/prstring.h"
#include "pr/linedrawer/ldr_objects_dlg.h"
#include "pr/script/reader.h"

namespace pr
{
	namespace ldr
	{
		namespace ETriState
		{
			enum Type { Off, On, Toggle };
		}

		#define PR_ENUM(x)\
			x(Name     )\
			x(LdrType  )\
			x(Colour   )\
			x(Visible  )\
			x(Wireframe)\
			x(Volume   )\
			x(CtxtId   )
		PR_DEFINE_ENUM1(EColumn, PR_ENUM);
		#undef PR_ENUM

		// ScriptWindow *******************************************************************

		class ScriptWindow
			:public CIndirectDialogImpl<ScriptWindow>
			,public CDialogResize<ScriptWindow>
		{
			std::string m_text;
			CEdit m_info;
			CFont m_font;

		public:
			ScriptWindow(std::string text) :m_text(text) {}
			BOOL OnInitDialog(CWindow, LPARAM)
			{
				CenterWindow(GetParent());
				m_font.CreatePointFont(80, "courier new");
				m_info.Attach(GetDlgItem(IDC_TEXT));
				m_info.SetTabStops(12);
				m_info.SetFont(m_font);
				m_info.SetWindowTextA(m_text.c_str());
				m_info.SetSelNone();
				DlgResize_Init();
				return TRUE;
			}
			void OnClose(UINT, int nID, CWindow)
			{
				EndDialog(nID);
			}

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
		};

		// ObjectManagerDlgImpl *******************************************************************

		// Implementation of the LdrObject Manager GUI
		struct ObjectManagerDlgImpl
			:public CIndirectDialogImpl<ObjectManagerDlgImpl>
			,public CDialogResize<ObjectManagerDlgImpl>
		{
			// UI data for an ldr object
			struct UIData :ILdrUserData
			{
				HTREEITEM m_tree_item;
				int       m_list_item;

				static size_t Id()
				{
					static size_t s_id = pr::hash::HashC("LdrObjectUIData");
					return s_id;
				}
				UIData()
					:m_tree_item(INVALID_TREE_ITEM)
					,m_list_item(INVALID_LIST_ITEM)
				{}
			};
				
			HWND                    m_parent;               // Parent window
			WTL::CStatusBarCtrl     m_status;               // The status bar
			WTL::CSplitterWindow    m_split;                // Splitter window
			WTL::CTreeViewCtrl      m_tree;                 // Tree control
			WTL::CListViewCtrl      m_list;                 // List control
			WTL::CButton            m_btn_expand_all;       // Expand all button
			WTL::CButton            m_btn_collapse_all;     // Collapse all button
			WTL::CEdit              m_filter;               // Object filter
			WTL::CButton            m_btn_apply_filter;     // Apply filter button
			bool                    m_expanding;            // True during a recursive expansion of a node in the tree view
			bool                    m_selection_changed;    // Dirty flag for the selection bbox/object
			bool                    m_suspend_layout;       // True while a block of changes are occurring.

			ObjectManagerDlgImpl(HWND parent = 0)
				:m_parent(parent)
				,m_status()
				,m_split()
				,m_tree()
				,m_list()
				,m_btn_expand_all()
				,m_btn_collapse_all()
				,m_filter()
				,m_btn_apply_filter()
				,m_expanding(false)
				,m_selection_changed(true)
				,m_suspend_layout(false)
			{
				if (Create(0) == 0)
					throw std::exception("Failed to create object manager ui");
			}
			~ObjectManagerDlgImpl()
			{
				if (IsWindow())
					DestroyWindow();
			}

			// Return the LdrObject associated with a tree item or list item
			LdrObject const& GetLdrObject(HTREEITEM item) const
			{
				PR_ASSERT(PR_DBG_LDROBJMGR, item != INVALID_TREE_ITEM && m_tree.GetItemData(item) != 0, "Tree item does not refer to an LdrObject");
				return *reinterpret_cast<LdrObject*>(m_tree.GetItemData(item));
			}
			LdrObject& GetLdrObject(HTREEITEM item)
			{
				PR_ASSERT(PR_DBG_LDROBJMGR, item != INVALID_TREE_ITEM && m_tree.GetItemData(item) != 0, "Tree item does not refer to an LdrObject");
				return *reinterpret_cast<LdrObject*>(m_tree.GetItemData(item));
			}
			LdrObject const& GetLdrObject(int item) const
			{
				PR_ASSERT(PR_DBG_LDROBJMGR, item != INVALID_LIST_ITEM && m_list.GetItemData(item) != 0, "List item does not refer to an LdrObject");
				return *reinterpret_cast<LdrObject*>(m_list.GetItemData(item));
			}
			LdrObject& GetLdrObject(int item)
			{
				PR_ASSERT(PR_DBG_LDROBJMGR, item != INVALID_LIST_ITEM && m_list.GetItemData(item) != 0, "List item does not refer to an LdrObject");
				return *reinterpret_cast<LdrObject*>(m_list.GetItemData(item));
			}

			// Return the uidata for an object
			static UIData* GetUIData(LdrObject& obj) { return static_cast<UIData*>(obj.m_user_data[UIData::Id()].get()); }
			static UIData* GetUIData(LdrObject* obj) { return obj ? GetUIData(*obj) : nullptr; }

			// Get/Set settings for the object manager window
			std::string Settings() const
			{
				WTL::CRect wrect;
				GetWindowRect(&wrect);

				std::stringstream out;
				out << "*WindowPos "<<wrect.left<<" "<<wrect.top<<" "<<wrect.right<<" "<<wrect.bottom<<" "
					<< "*SplitterPos "<<m_split.GetSplitterPosPct()<<" ";
				return out.str();
			}
			void Settings(char const* settings)
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

			// Handle a key press in either the list or tree view controls
			void HandleKey(int vkey)
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

			// Return the number of selected objects
			size_t SelectedCount() const
			{
				return m_list.GetSelectedCount();
			}

			// Remove selection from the tree and list controls
			void SelectNone()
			{
				for (int i = m_list.GetNextItem(-1, LVNI_SELECTED); i != -1; i = m_list.GetNextItem(i, LVNI_SELECTED))
					m_list.SetItemState(i, 0, LVIS_SELECTED);
			}

			// Select an ldr object
			void SelectLdrObject(LdrObject& object, bool make_visible)
			{
				auto obj_uidata = GetUIData(object);

				// Select in the tree
				m_tree.SetItemState(obj_uidata->m_tree_item, TVIS_SELECTED, TVIS_SELECTED);
				if (make_visible) m_tree.EnsureVisible(obj_uidata->m_tree_item);

				// Select in the list and make visible
				if (obj_uidata->m_list_item != INVALID_LIST_ITEM)
				{
					m_list.SetItemState(obj_uidata->m_list_item, LVIS_SELECTED, LVIS_SELECTED);
					if (make_visible) m_list.EnsureVisible(obj_uidata->m_list_item, FALSE);
				}

				// Flag the selection data as invalid
				m_selection_changed = true;
				pr::events::Send(Evt_LdrObjectSelectionChanged());
			}

			// Invert the selection from the tree and list controls
			void InvSelection()
			{
				for (int i = m_list.GetNextItem(-1, LVNI_ALL); i != -1; i = m_list.GetNextItem(i, LVNI_ALL))
					m_list.SetItemState(i, ~m_list.GetItemState(i, LVIS_SELECTED), LVIS_SELECTED);
			}

			// Return a bounding box of the objects
			pr::BBox GetBBox(EObjectBounds bbox_type) const
			{
				pr::BBox bbox = pr::BBoxReset;
				switch (bbox_type)
				{
				default: PR_ASSERT(PR_DBG_LDROBJMGR, false, "Unknown bounding box type requested"); return pr::BBoxUnit;
				case EObjectBounds::All:
					for (HTREEITEM i = m_tree.GetRootItem(); i != 0; i = m_tree.GetNextItem(i, TVGN_NEXT))
					{
						pr::BBox bb = GetLdrObject(i).BBoxWS(true);
						if (bb.IsValid()) pr::Encompass(bbox, bb);
					}
					break;
				case EObjectBounds::Selected:
					for (int i = m_list.GetNextItem(-1, LVNI_SELECTED); i != -1; i = m_list.GetNextItem(i, LVNI_SELECTED))
					{
						pr::BBox bb = GetLdrObject(i).BBoxWS(true);
						if (bb.IsValid()) pr::Encompass(bbox, bb);
					}
					break;
				case EObjectBounds::Visible:
					for (HTREEITEM i = m_tree.GetRootItem(); i != 0; i = m_tree.GetNextItem(i, TVGN_NEXT))
					{
						pr::BBox bb = GetLdrObject(i).BBoxWS(true, [](LdrObject const& obj) { return obj.m_visible; });
						if (bb.IsValid()) pr::Encompass(bbox, bb);
					}
					break;
				}
				return bbox.IsValid() ? bbox : pr::BBoxUnit;
			}

			// Set the visibility of the currently selected objects
			void SetVisibilty(ETriState::Type state, bool include_children)
			{
				for (int i = m_list.GetNextItem(-1, LVNI_SELECTED); i != -1; i = m_list.GetNextItem(i, LVNI_SELECTED))
				{
					LdrObject& object = GetLdrObject(i);
					object.Visible(state == ETriState::Off ? false : state == ETriState::On ? true : !object.m_visible, include_children ? "" : nullptr);
					UpdateListItem(object, include_children);
				}
				pr::events::Send(Evt_Refresh());
			}

			// Set wireframe for the currently selected objects
			void SetWireframe(ETriState::Type state, bool include_children)
			{
				for (int i = m_list.GetNextItem(-1, LVNI_SELECTED); i != -1; i = m_list.GetNextItem(i, LVNI_SELECTED))
				{
					LdrObject& object = GetLdrObject(i);
					object.Wireframe(state == ETriState::Off ? false : state == ETriState::On ? true : !object.m_wireframe, include_children ? "" : nullptr);
					UpdateListItem(object, include_children);
				}
				pr::events::Send(Evt_Refresh());
			}

			// Add/Remove items from the list view based on the filter
			// If the filter is empty the list is re-populated
			void ApplyFilter()
			{
				// If the filter edit box is not empty then remove all that aren't selected
				if (m_filter.GetWindowTextLength() != 0)
				{
					for (int i = m_list.GetItemCount() - 1; i != -1; --i)
					{
						// Delete all non-selected items
						if ((m_list.GetItemState(i, LVIS_SELECTED) & LVIS_SELECTED) == 0)
						{
							GetUIData(GetLdrObject(i))->m_list_item = INVALID_LIST_ITEM;
							m_list.DeleteItem(i);
						}
					}
					FixListCtrlReferences(0);
				}
				// Else, remove all items from the list and re-add them based on whats displayed in the tree
				else
				{
					// Remove all items from the list
					for (int i = m_list.GetNextItem(-1, LVNI_ALL); i != -1; i = m_list.GetNextItem(i, LVNI_ALL))
						GetUIData(GetLdrObject(i))->m_list_item = INVALID_LIST_ITEM;
					m_list.DeleteAllItems();

					// Re-add items based on what's displayed in the tree
					int list_position = 0;
					for (HTREEITEM i = m_tree.GetRootItem(); i != 0; i = m_tree.GetNextItem(i, TVGN_NEXTVISIBLE), ++list_position)
					{
						LdrObject& object = GetLdrObject(i);

						// Add a list item for this tree item
						GetUIData(object)->m_list_item = list_position;
						m_list.InsertItem(list_position, object.m_name.c_str());
						m_list.SetItemData(list_position, reinterpret_cast<DWORD_PTR>(&object));
						UpdateListItem(object, false);
					}
				}
			}

			// Recursively perform 'func' on 'object' and its children
			template <typename Func> void RecursiveDo(LdrObject* object, Func func)
			{
				func(object);
				for (ObjectCont::iterator i = object->m_child.begin(), iend = object->m_child.end(); i != iend; ++i)
					RecursiveDo(*i, func);
			}

			// Automatically set the widths of the list view columns
			void Show(bool show)
			{
				ShowWindow(show ? SW_SHOW : SW_HIDE);
				if (show) SetWindowPos(HWND_TOP,0,0,0,0,SWP_NOMOVE|SWP_NOSIZE); // bring to front
				for (int i = 0; i != EColumn::NumberOf; ++i)
					m_list.SetColumnWidth(i, LVSCW_AUTOSIZE);
			}

			// For each object in the list from 'start_index' to the end, set the list index
			// in the object uidata. The list control uses contiguous memory so we have to do
			// this whenever objects are inserted/deleted from the list
			void FixListCtrlReferences(int start_index)
			{
				// 'start_index'==-1 means all list items
				if (start_index < 0) start_index = 0;
				for (int i = start_index, iend = m_list.GetItemCount(); i < iend; ++i)
					GetUIData(GetLdrObject(i))->m_list_item = i;
			}

			// Update the displayed properties in the list
			void UpdateListItem(LdrObject& object, bool recursive)
			{
				auto obj_uidata = GetUIData(object);
				if (obj_uidata->m_list_item == INVALID_LIST_ITEM) return;
				m_list.SetItemText(obj_uidata->m_list_item ,EColumn::Name      ,object.m_name.c_str());
				m_list.SetItemText(obj_uidata->m_list_item ,EColumn::LdrType   ,ELdrObject::ToString(object.m_type));
				m_list.SetItemText(obj_uidata->m_list_item ,EColumn::Colour    ,pr::FmtS("%X", object.m_colour.m_aarrggbb));
				m_list.SetItemText(obj_uidata->m_list_item ,EColumn::Visible   ,object.m_visible ? "Visible"   : "Hidden");
				m_list.SetItemText(obj_uidata->m_list_item ,EColumn::Wireframe ,object.m_wireframe ? "Wireframe" : "Solid");
				m_list.SetItemText(obj_uidata->m_list_item ,EColumn::Volume    ,pr::FmtS("%3.3f", Volume(object.BBoxMS(false))));
				m_list.SetItemText(obj_uidata->m_list_item ,EColumn::CtxtId    ,pr::FmtS("%d", object.m_context_id));
				if (!recursive) return;
				for (ObjectCont::iterator c = object.m_child.begin(), cend = object.m_child.end(); c != cend; ++c)
					UpdateListItem(*(*c).m_ptr, recursive);
			}

			// Recursively add 'obj' and its children to the tree and list control
			void Add(LdrObject* obj, LdrObject* prev, bool last_call = true)
			{
				PR_ASSERT(PR_DBG_LDROBJMGR, obj, "Attempting to add a null object to the UI");
				PR_ASSERT(PR_DBG_LDROBJMGR, !obj->m_parent || GetUIData(*obj->m_parent)->m_tree_item != INVALID_TREE_ITEM, "Parent is not in the tree");

				// Add UI data to the object
				PR_ASSERT(PR_DBG_LDROBJMGR, obj->m_user_data.count(UIData::Id()) == 0, "This item is already in the UI");
				obj->m_user_data[UIData::Id()] = std::make_unique<UIData>();
				
				auto obj_uidata    = GetUIData(obj);
				auto prev_uidata   = GetUIData(prev);
				auto parent_uidata = GetUIData(obj->m_parent);

				// Add the item to the tree
				obj_uidata->m_tree_item = m_tree.InsertItem(obj->m_name.c_str(),
					obj->m_parent ? parent_uidata->m_tree_item : TVI_ROOT,
					prev          ? prev_uidata->m_tree_item   : TVI_LAST);

				// Save a pointer to this object in the tree
				if (obj_uidata->m_tree_item != INVALID_TREE_ITEM)
					m_tree.SetItemData(obj_uidata->m_tree_item, reinterpret_cast<DWORD_PTR>(obj));
				else {}
				// todo: Report errors, without spamming the user...

				// Add the item to the list
				// If 'obj' is a top level object, then add it to the list
				if (obj->m_parent == 0)
				{
					obj_uidata->m_list_item = m_list.InsertItem(m_list.GetItemCount(), obj->m_name.c_str());
					if (obj_uidata->m_list_item == INVALID_LIST_ITEM) {}
					// todo: Report errors, without spamming the user...
				}
				// Otherwise, if 'prev' is visible in the list then display 'obj' in the list as well
				else if (prev && prev_uidata->m_list_item != INVALID_LIST_ITEM)
				{
					obj_uidata->m_list_item = m_list.InsertItem(prev_uidata->m_list_item + 1, obj->m_name.c_str());
					if (obj_uidata->m_list_item == INVALID_LIST_ITEM) {}
					// todo: Report errors, without spamming the user...
				}
				else
				{
					obj_uidata->m_list_item = INVALID_LIST_ITEM;
				}

				// Save a pointer to this object in the list
				if (obj_uidata->m_list_item != INVALID_LIST_ITEM)
				{
					m_list.SetItemData(obj_uidata->m_list_item, reinterpret_cast<DWORD_PTR>(obj));
					UpdateListItem(*obj, false);
				}

				// Add the children
				LdrObject* p = 0;
				for (ObjectCont::iterator c = obj->m_child.begin(), cend = obj->m_child.end(); c != cend; p = (*c).m_ptr, ++c)
					Add((*c).m_ptr, p, false);

				// On leaving the last recusive call, fix up the references
				if (last_call)
					FixListCtrlReferences(obj_uidata->m_list_item);
			}

			// Recursively remove 'obj' and its children from the tree and list controls.
			// Note that objects are not deleted from the ObjectManager
			void Remove(LdrObject* obj, bool last_call = true)
			{
				auto obj_uidata = GetUIData(obj);
				if (obj_uidata == nullptr) return; // Object wasn't added so has no ui data
				int list_position = obj_uidata->m_list_item;

				// Recursively delete children in reverse order to prevent corrupting list control indices
				for (std::size_t c = obj->m_child.size(); c != 0; --c)
					Remove(obj->m_child[c - 1].m_ptr, false);

				// If the object is in the list, remove it. We'll fix up the list
				// references after all children of 'obj' have been removed.
				if (obj_uidata->m_list_item != INVALID_LIST_ITEM)
				{
					m_list.DeleteItem(obj_uidata->m_list_item);
					obj_uidata->m_list_item = INVALID_LIST_ITEM;
				}

				// Remove it from the tree.
				m_tree.DeleteItem(obj_uidata->m_tree_item);
				obj_uidata->m_tree_item = INVALID_TREE_ITEM;

				// Remove the uidata from the object
				obj->m_user_data.erase(UIData::Id());

				if (last_call)
					FixListCtrlReferences(list_position);
			}

			// Empty the tree and list controls
			void DeleteAll()
			{
				m_tree.DeleteAllItems();
				m_list.DeleteAllItems();
			}

			// Collapse 'object' and its children in 'tree'.
			// Remove 'object's children from the 'list'
			void Collapse(LdrObject* object)
			{
				CollapseRecursive(object);

				// Fix the indices of the remaining list members
				FixListCtrlReferences(GetUIData(object)->m_list_item);
			}

			// Recursively collapse objects in the tree.
			// Depth-first so that we can remove items from the list control at the same time
			void CollapseRecursive(LdrObject* object)
			{
				std::size_t child_count = object->m_child.size();
				for (std::size_t c = child_count; c != 0; --c)
				{
					LdrObject* child = object->m_child[c - 1].m_ptr;
					CollapseRecursive(child);

					// Remove this child from the list control
					auto child_uidata = GetUIData(child);
					if (child_uidata->m_list_item != INVALID_LIST_ITEM)
					{
						m_list.DeleteItem(child_uidata->m_list_item);
						child_uidata->m_list_item = INVALID_LIST_ITEM;
					}
				}

				// Collapse this tree item
				m_tree.Expand(GetUIData(object)->m_tree_item, TVE_COLLAPSE);
			}

			// Expand 'object' in the tree and add its children to 'list'
			void Expand(LdrObject* object, bool recursive, bool& expanding)
			{
				// Calling tree.Expand causes notification messages to be sent,
				// Believe me, I've try to find a better solution, this is the best I could do
				// after several days :-/. But hey, it works.
				if (!expanding)
				{
					expanding = true;
					int list_position = GetUIData(object)->m_list_item + 1;
					ExpandRecursive(object, recursive, list_position);
					expanding = false;
				}

				// Fix the indices of the remaining list members
				FixListCtrlReferences(GetUIData(object)->m_list_item + 1);
			}

			// Expand this object. If 'all_children' is true, expand all of its children.
			// Add all children to the list control if the parent is in the list control.
			void ExpandRecursive(LdrObject* object, bool all_children, int& list_position)
			{
				std::size_t child_count = object->m_child.size();
				for (std::size_t c = 0; c != child_count; ++c)
				{
					LdrObject* child = object->m_child[c].m_ptr;

					// Add this child to the list control
					if (GetUIData(object)->m_list_item != INVALID_LIST_ITEM &&
						GetUIData(child )->m_list_item == INVALID_LIST_ITEM)
					{
						GetUIData(child)->m_list_item = list_position;
						m_list.InsertItem(list_position, child->m_name.c_str());
						m_list.SetItemData(list_position, reinterpret_cast<DWORD_PTR>(child));
						UpdateListItem(*child, false);
						++list_position;
					}

					if (all_children) ExpandRecursive(child, all_children, list_position);
				}

				// Expand this tree item
				m_tree.Expand(GetUIData(object)->m_tree_item, TVE_EXPAND);
			}

			// Handler methods
			LRESULT OnInitDialog(UINT, WPARAM, LPARAM, BOOL&)
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
					m_list.AddColumn(EColumn::MemberName(i), i);

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
			LRESULT OnDestDialog(UINT, WPARAM, LPARAM, BOOL&)
			{
				return S_OK;
			}
			LRESULT OnResized(UINT, WPARAM, LPARAM, BOOL&)
			{
				// Notify listeners that the settings have changed
				pr::events::Send(Evt_SettingsChanged());
				return S_OK;
			}
			LRESULT OnMouseWheel(UINT msg, WPARAM wparam, LPARAM lparam, BOOL& handled)
			{
				// Use hover scrolling for the tree and list views
				POINT pt = {GET_X_LPARAM(lparam), GET_Y_LPARAM(lparam)};
				CRect rect;

				m_tree.GetWindowRect(&rect);
				if (rect.PtInRect(pt))  { SendMessage(m_tree.m_hWnd, msg, wparam, lparam); handled = TRUE; return S_OK; }

				m_list.GetWindowRect(&rect);
				if (rect.PtInRect(pt))  { SendMessage(m_list.m_hWnd, msg, wparam, lparam); handled = TRUE; return S_OK; }

				handled = FALSE;
				return S_OK;
			}
			LRESULT OnCloseDialog(WORD, WORD, HWND, BOOL&)
			{
				// Handle close window events by just hiding the window
				pr::events::Send(Evt_SettingsChanged());
				ShowWindow(SW_HIDE);
				return S_OK;
			}
			LRESULT OnExpandAll(WORD, WORD, HWND, BOOL&)
			{
				// Expand all currently visible plus signs in the tree view (or everything if shift is pressed)
				bool include_children = pr::KeyDown(VK_SHIFT);
				for (HTREEITEM i = m_tree.GetRootItem(); i != 0;)
				{
					HTREEITEM j = i;
					i = m_tree.GetNextItem(i, TVGN_NEXTVISIBLE);
					Expand(&GetLdrObject(j), include_children, m_expanding);
				}
				return S_OK;
			}
			LRESULT OnCollapseAll(WORD, WORD, HWND, BOOL&)
			{
				// Collapse everything in the tree view
				for (HTREEITEM i = m_tree.GetRootItem(); i != 0; i = m_tree.GetNextSiblingItem(i))
					Collapse(&GetLdrObject(i));
				return S_OK;
			}
			LRESULT OnFilterChanged(WORD, WORD, HWND, BOOL&)
			{
				// Handle selected objects based on the list view filter
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
			LRESULT OnApplyFilter(WORD, WORD, HWND, BOOL&)
			{
				// Apply the current filter to the list
				ApplyFilter();
				return S_OK;
			}
			LRESULT OnTreeExpand(WPARAM, LPNMHDR hdr, BOOL&)
			{
				// Handle clicks on a plus sign to expand or collapse items in the tree
				LPNMTREEVIEW tvhdr = reinterpret_cast<LPNMTREEVIEW>(hdr);
				LdrObject& object = GetLdrObject(tvhdr->itemNew.hItem);
				switch (tvhdr->action)
				{
				default:break;
				case TVE_EXPAND:
					Expand(&object, pr::KeyDown(VK_SHIFT), m_expanding);
					break;
				case TVE_COLLAPSE:
					Collapse(&object);
					break;
				}
				return S_OK;
			}
			LRESULT OnTreeItemSelected(WPARAM, LPNMHDR hdr, BOOL&)
			{
				// Mirror items selected in the tree with those selected in the list
				LPNMTREEVIEW tv = reinterpret_cast<LPNMTREEVIEW>(hdr);
				LdrObject& object = GetLdrObject(tv->itemNew.hItem);
				if (GetUIData(object)->m_list_item == INVALID_LIST_ITEM) return S_OK;

				SelectNone();
				SelectLdrObject(object, true);
				return S_OK;
			}
			LRESULT OnTreeKeydown(WPARAM, LPNMHDR hdr, BOOL&)
			{
				// Handle key events for the tree view
				HandleKey(reinterpret_cast<LPNMTVKEYDOWN>(hdr)->wVKey);
				return S_OK;
			}
			LRESULT OnTreeDblClick(WPARAM, LPNMHDR, BOOL&)
			{
				// Handle double clicks on items in the tree
				HTREEITEM i = m_tree.GetSelectedItem();
				if (i == 0) return S_OK;

				LdrObject& object = GetLdrObject(i);
				if ((m_tree.GetItemState(i, TVIS_EXPANDED) & TVIS_EXPANDED) == 0)
					Expand(&object, false, m_expanding);
				else
					Collapse(&object);

				return S_OK;
			}
			LRESULT OnListKeydown(WPARAM, LPNMHDR hdr, BOOL&)
			{
				// Handle key events for the list view
				HandleKey(reinterpret_cast<LPNMLVKEYDOWN>(hdr)->wVKey);
				return S_OK;
			}
			LRESULT OnListItemSelected(WPARAM, LPNMHDR hdr, BOOL& bHandled)
			{
				// Handle list items being selected
				if (hdr->code == LVN_ITEMCHANGED)
				{
					NMLISTVIEW* data = reinterpret_cast<NMLISTVIEW*>(hdr);
					if ((data->uNewState ^ data->uOldState) & LVIS_SELECTED) // If the selection has changed
					{
						m_selection_changed = true;
						pr::events::Send(Evt_LdrObjectSelectionChanged());
						return S_OK;
					}
				}
				bHandled = false;
				return S_OK;
			}
			LRESULT OnShowListContextMenu(WPARAM, LPNMHDR hdr, BOOL&)
			{
				// Display a context menu when the user right clicks
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
			LRESULT OnChangeVisibility(WORD, WORD id, HWND, BOOL&)
			{
				// Change the visibilty of selected objects
				switch (id)
				{
				default:break;
				case ID_HIDEALL:    SetVisibilty(ETriState::Off     ,!pr::KeyDown(VK_SHIFT)); break;
				case ID_SHOWALL:    SetVisibilty(ETriState::On      ,!pr::KeyDown(VK_SHIFT)); break;
				case ID_INV_VIS:    SetVisibilty(ETriState::Toggle  ,!pr::KeyDown(VK_SHIFT)); break;
				}
				pr::events::Send(Evt_Refresh());
				return S_OK;
			}
			LRESULT OnChangeSolidWire(WORD, WORD id, HWND, BOOL&)
			{
				// Change the render mode of selected objects
				switch (id)
				{
				default:break;
				case ID_SOLIDALL:   SetWireframe(ETriState::Off     ,!pr::KeyDown(VK_SHIFT)); break;
				case ID_WIREALL:    SetWireframe(ETriState::On      ,!pr::KeyDown(VK_SHIFT)); break;
				case ID_INV_WIRE:   SetWireframe(ETriState::Toggle  ,!pr::KeyDown(VK_SHIFT)); break;
				}
				pr::events::Send(Evt_Refresh());
				return S_OK;
			}
			LRESULT OnChangeInvertSelection(WORD, WORD, HWND, BOOL&)
			{
				// Invert the selection
				for (int i = m_list.GetNextItem(-1, LVNI_ALL); i != -1; i = m_list.GetNextItem(i, LVNI_ALL))
					m_list.SetItemState(i, m_list.GetItemState(i, LVIS_SELECTED) ^ LVIS_SELECTED, LVIS_SELECTED);
				return S_OK;
			}
			LRESULT OnDetailedInfo(WORD, WORD, HWND, BOOL&)
			{
				// Show detailed info about the first selected object
				int selected = m_list.GetNextItem(-1, LVNI_SELECTED);
				if (selected == -1) return S_OK;

				return S_OK;
			}

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
			BEGIN_DLGRESIZE_MAP(ObjectManagerDlgImpl)
				DLGRESIZE_CONTROL(IDC_EXPAND      ,0)
				DLGRESIZE_CONTROL(IDC_COLLAPSE    ,0)
				DLGRESIZE_CONTROL(IDC_FILTER_TEXT ,DLSZ_SIZE_X)
				DLGRESIZE_CONTROL(IDC_FILTER      ,DLSZ_MOVE_X)
				DLGRESIZE_CONTROL(IDC_SPLITTER      ,DLSZ_SIZE_X|DLSZ_SIZE_Y)
				DLGRESIZE_CONTROL(IDC_STATUSBAR     ,DLSZ_SIZE_X|DLSZ_MOVE_Y)
			END_DLGRESIZE_MAP()
			BEGIN_MSG_MAP(ObjectManagerDlgImpl)
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
				CHAIN_MSG_MAP(CDialogResize<ObjectManagerDlgImpl>)
			END_MSG_MAP()

		private:
			ObjectManagerDlgImpl(ObjectManagerDlgImpl const&);
			ObjectManagerDlgImpl& operator=(ObjectManagerDlgImpl const&);
		};

		// ObjectManagerDlg ***********************************************************************
		ObjectManagerDlg::ObjectManagerDlg(HWND parent)
			:m_dlg(new ObjectManagerDlgImpl(parent))
			,m_ignore_ctxids()
			,m_scene_bbox(pr::BBoxReset)
		{}

		bool ObjectManagerDlg::IsChild(HWND hwnd) const
		{
			return m_dlg->IsChild(hwnd) != 0;
		}

		// Display the object manager window
		void ObjectManagerDlg::Show(bool show)
		{
			m_dlg->Show(show);
		}

		// Return the number of selected objects
		size_t ObjectManagerDlg::SelectedCount() const
		{
			return m_dlg->SelectedCount();
		}

		// Display a window containing the example script
		void ObjectManagerDlg::ShowScript(std::string script, HWND parent)
		{
			std::string text = script;
			pr::str::Replace(text, "\n", "\r\n");
			ScriptWindow example(text);
			example.DoModal(parent);
		}

		// Set the ignore state for a particular context id
		// Should be called before objects are added to the obj mgr
		void ObjectManagerDlg::IgnoreContextId(ContextId id, bool ignore)
		{
			if (ignore) m_ignore_ctxids.insert(id);
			else        m_ignore_ctxids.erase(id);
		}

		// Return a bounding box of the objects
		pr::BBox ObjectManagerDlg::GetBBox(EObjectBounds bbox_type) const
		{
			if (bbox_type == EObjectBounds::All && m_scene_bbox != pr::BBoxReset)
				return m_scene_bbox;

			return m_dlg->GetBBox(bbox_type);
		}

		// Get/Set settings for the object manager window
		std::string ObjectManagerDlg::Settings() const
		{
			return m_dlg->Settings();
		}
		void ObjectManagerDlg::Settings(char const* settings)
		{
			m_dlg->Settings(settings);
		}

		// An object has been created
		void ObjectManagerDlg::OnEvent(Evt_LdrObjectAdd const& e)
		{
			// Ignore context ids we're not showing in the obj mgr.
			if (m_ignore_ctxids.find(e.m_obj->m_context_id) != m_ignore_ctxids.end())
				return;

			// Ignore models that aren't instanced
			if (!e.m_obj->m_instanced)
				return;

			// Find the previous sibbling
			LdrObject* prev = 0;
			if (e.m_obj->m_parent)
			{
				auto& children = e.m_obj->m_parent->m_child;
				for (auto i = std::begin(children), iend = std::end(children); i != iend; ++i)
				{
					if (*i != e.m_obj) continue;
					if (i == std::begin(children)) break;
					prev = (*(--i)).m_ptr;
					break;
				}
			}

			m_dlg->Add(e.m_obj.m_ptr, prev);
			m_scene_bbox = pr::BBoxReset;
		}

		// Empty the tree and list controls, all objects have been deleted
		void ObjectManagerDlg::OnEvent(Evt_DeleteAll const&)
		{
			m_dlg->DeleteAll();
			m_scene_bbox = pr::BBoxReset;
		}

		// Remove an object from the tree and list controls
		void ObjectManagerDlg::OnEvent(Evt_LdrObjectDelete const& e)
		{
			m_dlg->Remove(e.m_obj);
			m_scene_bbox = pr::BBoxReset;
		}
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
