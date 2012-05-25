//*************************************************************************
//
// A Dialog interface for the data manager
//
//*************************************************************************

#include "Stdafx.h"
#include "LineDrawer/GUI/DataManagerGUI.h"
#include "LineDrawer/Source/LineDrawer.h"
#include "LineDrawer/GUI/AddObjectDlg.h"
#include "LineDrawer/GUI/ColourTypeinDlg.h"

IMPLEMENT_DYNAMIC(DataManagerGUI, CDialog)

BEGIN_MESSAGE_MAP(DataManagerGUI, CDialog)
	ON_WM_GETMINMAXINFO()
	ON_WM_SIZE()
	ON_WM_PAINT()
	ON_WM_DESTROY()

	// Button handlers
	ON_BN_CLICKED(IDC_BUTTON_HIDE_ALL,			OnBnClickedButtonHideAll)
	ON_BN_CLICKED(IDC_BUTTON_UNHIDE_ALL,		OnBnClickedButtonUnhideAll)
	ON_BN_CLICKED(IDC_BUTTON_HIDE,				OnBnClickedButtonHide)
	ON_BN_CLICKED(IDC_BUTTON_UNHIDE,			OnBnClickedButtonUnhide)
	ON_BN_CLICKED(IDC_BUTTON_TOGGLE_VISIBILITY, OnBnClickedButtonToggleVisibility)
	ON_BN_CLICKED(IDC_BUTTON_WIREFRAME_ALL,		OnBnClickedButtonWireframeAll)
	ON_BN_CLICKED(IDC_BUTTON_UNWIREFRAME_ALL,	OnBnClickedButtonUnwireframeAll)
	ON_BN_CLICKED(IDC_BUTTON_WIREFRAME,			OnBnClickedButtonWireframe)
	ON_BN_CLICKED(IDC_BUTTON_UNWIREFRAME,		OnBnClickedButtonUnwireframe)
	ON_BN_CLICKED(IDC_BUTTON_TOGGLE_WIRE,		OnBnClickedButtonToggleWire)
	ON_BN_CLICKED(IDC_BUTTON_SET_COLOUR,		OnBnClickedButtonSetColour)
	ON_BN_CLICKED(IDC_BUTTON_INV_SELECTION,		OnBnClickedButtonInvSelection)
	ON_BN_CLICKED(IDC_BUTTON_EDIT_SELECTION,	OnBnClickedButtonEditSelection)
	ON_BN_CLICKED(IDC_BUTTON_DEL_SELECTION,		OnBnClickedButtonDelSelection)
	ON_BN_CLICKED(IDC_BUTTON_EXPAND_ALL,		OnBnClickedButtonExpandAll)
	ON_BN_CLICKED(IDC_BUTTON_COLLAPSE_ALL,		OnBnClickedButtonCollapseAll)

	// List handlers
	ON_NOTIFY(NM_DBLCLK, IDC_LIST_DATA,			OnNMDblclkListData)
	ON_NOTIFY(LVN_KEYDOWN, IDC_LIST_DATA,		OnLvnKeydownListData)
	ON_NOTIFY(LVN_ITEMCHANGED, IDC_LIST_DATA,	OnLvnItemchangedListData)

	// Tree handlers
	ON_NOTIFY(TVN_ITEMEXPANDED, IDC_DATA_TREE,	OnTvnItemexpandedDataTree)
	ON_NOTIFY(TVN_SELCHANGED, IDC_DATA_TREE,	OnTvnSelchangedDataTree)
	ON_NOTIFY(TVN_KEYDOWN, IDC_DATA_TREE,		OnTvnKeydownDataTree)

	// Miscellaneous handlers
	ON_EN_CHANGE(IDC_EDIT_SELECT_MASK,			OnEnChangeEditSelectMask)
	ON_WM_KEYDOWN()
END_MESSAGE_MAP()

HTREEITEM const	DataManagerGUI::INVALID_TREE_ITEM =  0;
int const		DataManagerGUI::INVALID_LIST_ITEM = -1;

// Construction
DataManagerGUI::DataManagerGUI()
:CDialog(DataManagerGUI::IDD, 0)
,m_linedrawer(0)
,m_data_tree()
,m_data_list()
,m_selection_mask()
,m_splitter()
,m_refresh_pending(false)
,m_selection_changed(true)
,m_selection_last_bbox(BBoxUnit)
{}

DataManagerGUI::~DataManagerGUI()
{
	Clear();
}

void DataManagerGUI::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_DATA_TREE,			m_data_tree);
	DDX_Control(pDX, IDC_LIST_DATA,			m_data_list);
	DDX_Control(pDX, IDC_SPLITTER_CTRL,		m_splitter);
	DDX_Text(pDX,    IDC_EDIT_SELECT_MASK,	m_selection_mask);
}

// Initialise the list with the data objects
BOOL DataManagerGUI::OnInitDialog()
{
	CDialog::OnInitDialog();

	m_linedrawer = &LineDrawer::Get();
	m_linedrawer->m_data_manager_GUI = this;

	SplitterCtrl::Settings settings;
	settings.m_type		= SplitterCtrl::Vertical;
	settings.m_parent	= this;
	settings.m_side1	= GetDlgItem(IDC_DATA_TREE);
	settings.m_side2	= GetDlgItem(IDC_LIST_DATA);
	m_splitter.Initialise(settings);

	int column_size[EColumn_NumColumns] = {100, 55, 55, 65, 50, 70};
	
	CRect rect;
	m_data_list.GetClientRect(&rect);
	m_data_list.InsertColumn(EColumn_Name,		"Object",		LVCFMT_LEFT, column_size[EColumn_Name     ], EColumn_Name);
	m_data_list.InsertColumn(EColumn_Type,		"Type",			LVCFMT_LEFT, column_size[EColumn_Type     ], EColumn_Type);
	m_data_list.InsertColumn(EColumn_Visible,	"Visibility",	LVCFMT_LEFT, column_size[EColumn_Visible  ], EColumn_Visible);
	m_data_list.InsertColumn(EColumn_Wireframe,	"Wireframe",	LVCFMT_LEFT, column_size[EColumn_Wireframe], EColumn_Wireframe);
	m_data_list.InsertColumn(EColumn_Volume,	"Volume",		LVCFMT_LEFT, column_size[EColumn_Volume   ], EColumn_Volume);
	m_data_list.InsertColumn(EColumn_Colour,	"Colour",		LVCFMT_LEFT, column_size[EColumn_Colour   ], EColumn_Colour);
	PostMessage(WM_SIZE);
	return TRUE;
}

// When the dialog box closes set focus back to the main linedrawer window
void DataManagerGUI::OnCancel()
{
	CDialog::OnCancel();
	::SetFocus(m_linedrawer->m_window_handle);
}

int ButtonWidth			= 62;
int ButtonHeight		= 23;
int TopAlign			= 10;
int LeftAlign			= 8;
int RightAlign			= 8;
int BottomAlign			= 8;
int ButtonSpace			= 4;
int ExpandButtonSize	= 15;
int SplitterWidth		= ButtonSpace;

// Define the limits for resizing
void DataManagerGUI::OnGetMinMaxInfo(MINMAXINFO* lpMMI)
{
	lpMMI->ptMinTrackSize.x = LeftAlign + RightAlign  +  4*ButtonWidth + ButtonSpace;
	lpMMI->ptMinTrackSize.y = TopAlign  + BottomAlign + 15*ButtonHeight;
	CDialog::OnGetMinMaxInfo(lpMMI);
}

// Resize the DataManagerGUI window
void DataManagerGUI::OnSize(UINT nType, int cx, int cy)
{
	CDialog::OnSize(nType, cx, cy);
	if( nType == SIZE_MINIMIZED ) return;

	// Get the new window size
	CWnd* wnd;
	CRect rect, wndrect;
	GetClientRect(&rect);
	rect.left	+= LeftAlign;
	rect.right	-= RightAlign;
	rect.top	+= TopAlign;
	rect.bottom	-= BottomAlign;

	// Move the 'HideAll' button
	wndrect.left	= rect.right   - 2*ButtonWidth;
	wndrect.top		= rect.top     + 0*ButtonHeight;
	wndrect.right	= wndrect.left + ButtonWidth;
	wndrect.bottom	= wndrect.top  + ButtonHeight;
	wnd = GetDlgItem(IDC_BUTTON_HIDE_ALL);
	if( wnd ) wnd->MoveWindow(wndrect);

	// Move the 'UnHideAll' button
	wndrect.left	= rect.right   - ButtonWidth;
	wndrect.top		= rect.top     + 0*ButtonHeight;
	wndrect.right	= wndrect.left + ButtonWidth;
	wndrect.bottom	= wndrect.top  + ButtonHeight;
	wnd = GetDlgItem(IDC_BUTTON_UNHIDE_ALL);
	if( wnd ) wnd->MoveWindow(wndrect);

	// Move the 'Hide' button
	wndrect.left	= rect.right   - 2*ButtonWidth;
	wndrect.top		= rect.top     + 1*ButtonHeight;
	wndrect.right	= wndrect.left + ButtonWidth;
	wndrect.bottom	= wndrect.top  + ButtonHeight;
	wnd = GetDlgItem(IDC_BUTTON_HIDE);
	if( wnd ) wnd->MoveWindow(wndrect);

	// Move the 'UnHide' button
	wndrect.left	= rect.right   - ButtonWidth;
	wndrect.top		= rect.top     + 1*ButtonHeight;
	wndrect.right	= wndrect.left + ButtonWidth;
	wndrect.bottom	= wndrect.top  + ButtonHeight;
	wnd = GetDlgItem(IDC_BUTTON_UNHIDE);
	if( wnd ) wnd->MoveWindow(wndrect);

	// Move the 'ToggleVisibility' button
	wndrect.left	= rect.right   - 2*ButtonWidth;
	wndrect.top		= rect.top     + 2*ButtonHeight;
	wndrect.right	= wndrect.left + 2*ButtonWidth;
	wndrect.bottom	= wndrect.top  + ButtonHeight;
	wnd = GetDlgItem(IDC_BUTTON_TOGGLE_VISIBILITY);
	if( wnd ) wnd->MoveWindow(wndrect);

	// Move the 'Wire All' button
	wndrect.left	= rect.right   - 2*ButtonWidth;
	wndrect.top		= rect.top     + 4*ButtonHeight;
	wndrect.right	= wndrect.left + ButtonWidth;
	wndrect.bottom	= wndrect.top  + ButtonHeight;
	wnd = GetDlgItem(IDC_BUTTON_WIREFRAME_ALL);
	if( wnd ) wnd->MoveWindow(wndrect);

	// Move the 'Solid All' button
	wndrect.left	= rect.right   - ButtonWidth;
	wndrect.top		= rect.top     + 4*ButtonHeight;
	wndrect.right	= wndrect.left + ButtonWidth;
	wndrect.bottom	= wndrect.top  + ButtonHeight;
	wnd = GetDlgItem(IDC_BUTTON_UNWIREFRAME_ALL);
	if( wnd ) wnd->MoveWindow(wndrect);

	// Move the 'Wire' button
	wndrect.left	= rect.right   - 2*ButtonWidth;
	wndrect.top		= rect.top     + 5*ButtonHeight;
	wndrect.right	= wndrect.left + ButtonWidth;
	wndrect.bottom	= wndrect.top  + ButtonHeight;
	wnd = GetDlgItem(IDC_BUTTON_WIREFRAME);
	if( wnd ) wnd->MoveWindow(wndrect);

	// Move the 'Solid' button
	wndrect.left	= rect.right   - ButtonWidth;
	wndrect.top		= rect.top     + 5*ButtonHeight;
	wndrect.right	= wndrect.left + ButtonWidth;
	wndrect.bottom	= wndrect.top  + ButtonHeight;
	wnd = GetDlgItem(IDC_BUTTON_UNWIREFRAME);
	if( wnd ) wnd->MoveWindow(wndrect);

	// Move the 'Toggle Wire' button
	wndrect.left	= rect.right   - 2*ButtonWidth;
	wndrect.top		= rect.top     + 6*ButtonHeight;
	wndrect.right	= wndrect.left + 2*ButtonWidth;
	wndrect.bottom	= wndrect.top  + ButtonHeight;
	wnd = GetDlgItem(IDC_BUTTON_TOGGLE_WIRE);
	if( wnd ) wnd->MoveWindow(wndrect);

	// Move the 'SetColour' button
	wndrect.left	= rect.right   - 2*ButtonWidth;
	wndrect.top		= rect.top     + 8*ButtonHeight;
	wndrect.right	= wndrect.left + 2*ButtonWidth;
	wndrect.bottom	= wndrect.top  + ButtonHeight;
	wnd = GetDlgItem(IDC_BUTTON_SET_COLOUR);
	if( wnd ) wnd->MoveWindow(wndrect);

	// Move the 'InvertSelection' button
	wndrect.left	= rect.right   - 2*ButtonWidth;
	wndrect.top		= rect.top     + 9*ButtonHeight;
	wndrect.right	= wndrect.left + 2*ButtonWidth;
	wndrect.bottom	= wndrect.top  + ButtonHeight;
	wnd = GetDlgItem(IDC_BUTTON_INV_SELECTION);
	if( wnd ) wnd->MoveWindow(wndrect);

	// Move the 'EditSelection' button
	wndrect.left	= rect.right   - 2*ButtonWidth;
	wndrect.top		= rect.top     + 10*ButtonHeight;
	wndrect.right	= wndrect.left + 2*ButtonWidth;
	wndrect.bottom	= wndrect.top  + ButtonHeight;
	wnd = GetDlgItem(IDC_BUTTON_EDIT_SELECTION);
	if( wnd ) wnd->MoveWindow(wndrect);

	// Move the 'DeleteSelection' button
	wndrect.left	= rect.right   - 2*ButtonWidth;
	wndrect.top		= rect.top     + 11*ButtonHeight;
	wndrect.right	= wndrect.left + 2*ButtonWidth;
	wndrect.bottom	= wndrect.top  + ButtonHeight;
	wnd = GetDlgItem(IDC_BUTTON_DEL_SELECTION);
	if( wnd ) wnd->MoveWindow(wndrect);

	// Move the 'Ok' button
	wndrect.left	= rect.right   - 2*ButtonWidth;
	wndrect.top		= rect.bottom  - ButtonHeight;
	wndrect.right	= wndrect.left + 2*ButtonWidth;
	wndrect.bottom	= wndrect.top  + ButtonHeight;
	wnd = GetDlgItem(IDCANCEL);
	if( wnd ) wnd->MoveWindow(wndrect);

	// Move the 'Mask' edit box
	wndrect.left	= rect.left;
	wndrect.right	= rect.right - 2*ButtonWidth - 2*ButtonSpace;
	wndrect.top		= rect.top;
	wndrect.bottom	= rect.top + ButtonHeight;
	wnd = GetDlgItem(IDC_EDIT_SELECT_MASK);
	if( wnd ) wnd->MoveWindow(wndrect);

	// Move the expand all button
	wndrect.left	= rect.left;
	wndrect.top		= rect.top + ButtonHeight + ButtonSpace;
	wndrect.right	= wndrect.left + ExpandButtonSize;
	wndrect.bottom	= wndrect.top + ExpandButtonSize;
	wnd = GetDlgItem(IDC_BUTTON_EXPAND_ALL);
	if( wnd ) wnd->MoveWindow(wndrect);

	// Move the collapse all button
	wndrect.left	= rect.left + ExpandButtonSize + ButtonSpace;
	wndrect.top		= rect.top + ButtonHeight + ButtonSpace;
	wndrect.right	= wndrect.left + ExpandButtonSize;
	wndrect.bottom	= wndrect.top + ExpandButtonSize;
	wnd = GetDlgItem(IDC_BUTTON_COLLAPSE_ALL);
	if( wnd ) wnd->MoveWindow(wndrect);

	int data_width = rect.Width() - 2*ButtonWidth - 2*ButtonSpace;
	float split_fraction = m_splitter.GetSplitFraction();

	// Move the 'Tree'
	wndrect.left	= rect.left;
	wndrect.right	= rect.left + static_cast<int>(data_width * split_fraction);
	wndrect.top		= rect.top + ButtonHeight + ExpandButtonSize + 2*ButtonSpace;
	wndrect.bottom	= rect.bottom;
	wnd = GetDlgItem(IDC_DATA_TREE);
	if( wnd ) wnd->MoveWindow(wndrect);

	// Move the 'List'
	wndrect.left	= rect.left + static_cast<int>(data_width * split_fraction) + ButtonSpace;
	wndrect.right	= rect.left + static_cast<int>(data_width * split_fraction) + ButtonSpace + static_cast<int>(data_width * (1.0f - split_fraction));
	wndrect.top		= rect.top + ButtonHeight + ButtonSpace;
	wndrect.bottom	= rect.bottom;
	wnd = GetDlgItem(IDC_LIST_DATA);
	if( wnd ) wnd->MoveWindow(wndrect);

	// Move the 'splitter bar'
	wndrect.left	= rect.left + static_cast<int>(data_width * split_fraction);
	wndrect.top		= rect.top + ButtonHeight + ExpandButtonSize + 2*ButtonSpace;
	wndrect.right	= rect.left + static_cast<int>(data_width * split_fraction) + ButtonSpace;
	wndrect.bottom	= rect.bottom;
	wnd = GetDlgItem(IDC_SPLITTER_CTRL);
	if( wnd ) wnd->MoveWindow(wndrect);

	m_splitter.ResetMinMaxRange();
	m_splitter.SetSplitFraction(split_fraction);

	Invalidate();
}

// Redraw the dialog
void DataManagerGUI::OnPaint()
{
	m_refresh_pending = false;
	CDialog::OnPaint();
}

// Clean up
void DataManagerGUI::OnDestroy()
{
	m_linedrawer->UnInitialise();
	CDialog::OnDestroy();
}	

// Public interface ***************************************

// Empty the tree and list controls
void DataManagerGUI::Clear()
{
	if( m_data_tree.m_hWnd ) m_data_tree.DeleteAllItems();
	if( m_data_list.m_hWnd ) m_data_list.DeleteAllItems();
}

// Add an object to the gui. 'object' is the object to be added,
// 'insert_after' is the object to insert 'object' after in the
// gui, may be '0' for root level objects
void DataManagerGUI::Add(LdrObject* object, LdrObject* insert_after)
{
	PR_ASSERT_STR(PR_DBG_LDR, object->m_tree_item == INVALID_TREE_ITEM, "This item is already in the GUI");
	PR_ASSERT_STR(PR_DBG_LDR, object->m_list_item == INVALID_LIST_ITEM, "This item is already in the GUI");

	AddToTree(object, insert_after);

	// If 'insert_after' is visible in the list control then display 'object' in the list control as well
	if( !insert_after || insert_after->m_list_item != INVALID_LIST_ITEM )
	{
		object->m_list_item = (!insert_after) ? 0 : insert_after->m_list_item + 1;
		object->m_list_item = m_data_list.InsertItem(object->m_list_item, object->m_name.c_str());
		if( object->m_list_item != INVALID_LIST_ITEM )
		{
			m_data_list.SetItemData(object->m_list_item, reinterpret_cast<DWORD_PTR>(object));
			UpdateListItem(object, false);
			FixListCtrlReferences(object->m_list_item + 1);
		}
	}
}

// Recursively add object and its children to the tree control
void DataManagerGUI::AddToTree(LdrObject* object, LdrObject* insert_after)
{
	PR_ASSERT_STR(PR_DBG_LDR, !object->m_parent || object->m_parent->m_tree_item != INVALID_TREE_ITEM, "Parent is not in the tree");

	object->m_list_item		= INVALID_LIST_ITEM;
	object->m_tree_item		= m_data_tree.InsertItem(
									object->m_name.c_str(),
									(object->m_parent)	? (object->m_parent->m_tree_item)	: (TVI_ROOT),
									(insert_after)		? (insert_after->m_tree_item)		: (TVI_FIRST));

	// Save a pointer to this object in the tree
	m_data_tree.SetItemData(object->m_tree_item, reinterpret_cast<DWORD_PTR>(object));

	// Add the children
	for( std::size_t c = 0; c != object->m_child.size(); ++c )
	{
		AddToTree(object->m_child[c], (c == 0) ? (0) : (object->m_child[c - 1]));
	}
}

// Remove an object from the tree and list controls
void DataManagerGUI::Delete(LdrObject* object)
{
	int list_position = object->m_list_item;
	DeleteFromTree(object);
	FixListCtrlReferences(list_position);
}

void DataManagerGUI::DeleteFromTree(LdrObject* object)
{
	// Recursively delete children in reverse order to prevent corrupting list control indices
	for( std::size_t c = object->m_child.size(); c != 0; --c )
	{
		DeleteFromTree(object->m_child[c - 1]);
	}

	// If the object is in the list, remove it. We'll fix up the list
	// references after all children of 'object' have been removed.
	if( object->m_list_item != INVALID_LIST_ITEM )
	{
		m_data_list.DeleteItem(object->m_list_item);
		object->m_list_item = INVALID_LIST_ITEM;
	}	

	// Remove it from the tree.
	m_data_tree.DeleteItem(object->m_tree_item);
	object->m_tree_item = INVALID_TREE_ITEM;
}

// Update the state of an item in the list
void DataManagerGUI::UpdateListItem(LdrObject* object, bool recursive)
{
	m_data_list.SetItemText(object->m_list_item, EColumn_Type,		"LdrObject");//ToString(object->m_type));
	m_data_list.SetItemText(object->m_list_item, EColumn_Visible,	(object->m_enabled) ? ("Visible") : ("Hidden"));
	m_data_list.SetItemText(object->m_list_item, EColumn_Wireframe,	(object->m_wireframe) ? ("Wireframe") : ("Solid"));
	m_data_list.SetItemText(object->m_list_item, EColumn_Volume,	Fmt("%3.3f", Volume(object->m_bbox)).c_str());
	m_data_list.SetItemText(object->m_list_item, EColumn_Colour,	Fmt("%X", object->m_instance.m_colour).c_str());

	if( recursive )
	{
		for( std::size_t c = 0; c != object->m_child.size(); ++c )
		{
			if( object->m_child[c]->m_list_item != INVALID_LIST_ITEM )
				UpdateListItem(object->m_child[c], true);
		}
	}
}

// Return a bounding box that encapsulates all of the selected objects
bool DataManagerGUI::GetSelectionBBox(BoundingBox& bbox, bool force_update)
{
	POSITION pos = m_data_list.GetFirstSelectedItemPosition();
	bool at_least_one = pos != 0;

	if( m_selection_changed || force_update )
	{
		m_selection_changed = false;
		bbox.reset();
		while( pos )
		{
			int list_item = m_data_list.GetNextSelectedItem(pos);
			LdrObject* object = reinterpret_cast<LdrObject*>(m_data_list.GetItemData(list_item));
			Encompase(bbox, object->WorldSpaceBBox(true));
		}
		m_selection_last_bbox = bbox;
	}
	else
	{
		bbox = m_selection_last_bbox;
	}
	return at_least_one;
}

// Return the transform for the first selected object
bool DataManagerGUI::GetSelectionTransform(m4x4& transform)
{
	POSITION pos = m_data_list.GetFirstSelectedItemPosition();
	if( pos )
	{
		int list_item = m_data_list.GetNextSelectedItem(pos);
		LdrObject* object = reinterpret_cast<LdrObject*>(m_data_list.GetItemData(list_item));
		transform = object->ObjectToWorld();
		return true;
	}
	return false;
}


//************************* Button Methods **************************

//*****
// Make all of the data invisible
void DataManagerGUI::OnBnClickedButtonHideAll()
{
	uint num_objects = m_linedrawer->m_data_manager.GetNumObjects();
	for( uint i = 0; i < num_objects; ++i )
	{
		LdrObject* object = m_linedrawer->m_data_manager.GetObject(i);
		object->SetEnable(false, true);
		UpdateListItem(object, true);
	}
	m_linedrawer->Refresh();
}

//*****
// Make all of the data visible
void DataManagerGUI::OnBnClickedButtonUnhideAll()
{
	uint num_objects = m_linedrawer->m_data_manager.GetNumObjects();
	for( uint i = 0; i < num_objects; ++i )
	{
		LdrObject* object = m_linedrawer->m_data_manager.GetObject(i);
		object->SetEnable(true, true);
		UpdateListItem(object, true);
	}
	m_linedrawer->Refresh();
}

//*****
// Hide the selected objects
void DataManagerGUI::OnBnClickedButtonHide()
{
	bool include_children = !((GetKeyState(VK_LSHIFT) & 0x8000) || (GetKeyState(VK_RSHIFT) & 0x8000));

	POSITION pos = m_data_list.GetFirstSelectedItemPosition();
	while( pos )
	{
		int list_item = m_data_list.GetNextSelectedItem(pos);
		LdrObject* object = reinterpret_cast<LdrObject*>(m_data_list.GetItemData(list_item));
		object->SetEnable(false, include_children);
		UpdateListItem(object, include_children);
	}
	m_linedrawer->Refresh();
}

//*****
// Unhide the selected objects
void DataManagerGUI::OnBnClickedButtonUnhide()
{
	bool include_children = !((GetKeyState(VK_LSHIFT) & 0x8000) || (GetKeyState(VK_RSHIFT) & 0x8000));

	POSITION pos = m_data_list.GetFirstSelectedItemPosition();
	while( pos )
	{
		int list_item = m_data_list.GetNextSelectedItem(pos);
		LdrObject* object = reinterpret_cast<LdrObject*>(m_data_list.GetItemData(list_item));
		object->SetEnable(true, include_children);
		UpdateListItem(object, include_children);
	}
	m_linedrawer->Refresh();
}

//*****
// Toggle visibility of selected models
void DataManagerGUI::OnBnClickedButtonToggleVisibility()
{
	bool include_children = !((GetKeyState(VK_LSHIFT) & 0x8000) || (GetKeyState(VK_RSHIFT) & 0x8000));

	POSITION pos = m_data_list.GetFirstSelectedItemPosition();
	while( pos )
	{
		int list_item = m_data_list.GetNextSelectedItem(pos);
		LdrObject* object = reinterpret_cast<LdrObject*>(m_data_list.GetItemData(list_item));
		object->SetEnable(!object->m_enabled, include_children);
		UpdateListItem(object, include_children);
	}
	m_linedrawer->Refresh();
}

//*****
// Make all objects wireframe
void DataManagerGUI::OnBnClickedButtonWireframeAll()
{
	uint num_objects = m_linedrawer->m_data_manager.GetNumObjects();
	for( uint i = 0; i < num_objects; ++i )
	{
		LdrObject* object = m_linedrawer->m_data_manager.GetObject(i);
		object->SetWireframe(true, true);
		UpdateListItem(object, true);
	}
	m_linedrawer->Refresh();
}

//*****
// Make all objects solid
void DataManagerGUI::OnBnClickedButtonUnwireframeAll()
{
	uint num_objects = m_linedrawer->m_data_manager.GetNumObjects();
	for( uint i = 0; i < num_objects; ++i )
	{
		LdrObject* object = m_linedrawer->m_data_manager.GetObject(i);
		object->SetWireframe(false, true);
		UpdateListItem(object, true);
	}
	m_linedrawer->Refresh();
}

//*****
// Make selected objects wireframe
void DataManagerGUI::OnBnClickedButtonWireframe()
{
	bool include_children = !((GetKeyState(VK_LSHIFT) & 0x8000) || (GetKeyState(VK_RSHIFT) & 0x8000));

	POSITION pos = m_data_list.GetFirstSelectedItemPosition();
	while( pos )
	{
		int list_item = m_data_list.GetNextSelectedItem(pos);
		LdrObject* object = reinterpret_cast<LdrObject*>(m_data_list.GetItemData(list_item));
		object->SetWireframe(true, include_children);
		UpdateListItem(object, include_children);
	}
	m_linedrawer->Refresh();
}

//*****
// Make selected objects solid
void DataManagerGUI::OnBnClickedButtonUnwireframe()
{
	bool include_children = !((GetKeyState(VK_LSHIFT) & 0x8000) || (GetKeyState(VK_RSHIFT) & 0x8000));

	POSITION pos = m_data_list.GetFirstSelectedItemPosition();
	while( pos )
	{
		int list_item = m_data_list.GetNextSelectedItem(pos);
		LdrObject* object = reinterpret_cast<LdrObject*>(m_data_list.GetItemData(list_item));
		object->SetWireframe(false, include_children);
		UpdateListItem(object, include_children);
	}
	m_linedrawer->Refresh();
}

// Toggle wireframe for selected objects
void DataManagerGUI::OnBnClickedButtonToggleWire()
{
	bool include_children = !((GetKeyState(VK_LSHIFT) & 0x8000) || (GetKeyState(VK_RSHIFT) & 0x8000));

	POSITION pos = m_data_list.GetFirstSelectedItemPosition();
	while( pos )
	{
		int list_item = m_data_list.GetNextSelectedItem(pos);
		LdrObject* object = reinterpret_cast<LdrObject*>(m_data_list.GetItemData(list_item));
		object->SetWireframe(!object->m_wireframe, include_children);
		UpdateListItem(object, include_children);
	}
	m_linedrawer->Refresh();
}

// Toggle alpha for selected objects
void DataManagerGUI::OnBnClickedButtonToggleAlpha()
{
	bool include_children = !((GetKeyState(VK_LSHIFT) & 0x8000) || (GetKeyState(VK_RSHIFT) & 0x8000));

	POSITION pos = m_data_list.GetFirstSelectedItemPosition();
	while( pos )
	{
		int list_item = m_data_list.GetNextSelectedItem(pos);
		LdrObject* object = reinterpret_cast<LdrObject*>(m_data_list.GetItemData(list_item));
		object->SetAlpha(object->m_instance.m_colour.a() == 255, include_children);
		UpdateListItem(object, include_children);
	}
	m_linedrawer->Refresh();
}

// Set the colour of selected objects
void DataManagerGUI::OnBnClickedButtonSetColour()
{
	// None selected? No colour setting...
	int num_selected = m_data_list.GetSelectedCount();
	if( num_selected == 0 ) return;

	// Open a colour dialog to choose a colour (initialise with the average colour)
	ColourTypein cdialog(this);
	POSITION pos = m_data_list.GetFirstSelectedItemPosition();
	while( pos )
	{
		int list_item = m_data_list.GetNextSelectedItem(pos);
		LdrObject* object = reinterpret_cast<LdrObject*>(m_data_list.GetItemData(list_item));
		cdialog.m_colour.a += object->m_instance.m_colour.a() / 255.0f;
		cdialog.m_colour.r += object->m_instance.m_colour.r() / 255.0f;
		cdialog.m_colour.g += object->m_instance.m_colour.g() / 255.0f;
		cdialog.m_colour.b += object->m_instance.m_colour.b() / 255.0f;
	}
	cdialog.m_colour.a = Clamp(cdialog.m_colour.a / num_selected, 0.0f, 1.0f);
	cdialog.m_colour.r = Clamp(cdialog.m_colour.r / num_selected, 0.0f, 1.0f);
	cdialog.m_colour.g = Clamp(cdialog.m_colour.g / num_selected, 0.0f, 1.0f);
	cdialog.m_colour.b = Clamp(cdialog.m_colour.b / num_selected, 0.0f, 1.0f);

	INT_PTR result = cdialog.DoModal();
	if( result != IDOK ) return;

	Colour32 colour = cdialog.GetColour32();

	// Set the colour on the selected objects
	pos = m_data_list.GetFirstSelectedItemPosition();
	while( pos )
	{
		int list_item = m_data_list.GetNextSelectedItem(pos);
		LdrObject* object = reinterpret_cast<LdrObject*>(m_data_list.GetItemData(list_item));
		object->SetColour(colour, false, false);
		UpdateListItem(object, false);
	}
	m_linedrawer->Refresh();
}

//*****
// Invert the selection
void DataManagerGUI::OnBnClickedButtonInvSelection()
{
	m_data_list.SetFocus();

	int num_items = m_data_list.GetItemCount();
	for( int i = 0; i < num_items; ++i )
	{
		m_data_list.SetItemState(i, ~m_data_list.GetItemState(i, LVIS_SELECTED), LVIS_SELECTED);
	}
}

//*****
// Edit the first selected item
void DataManagerGUI::OnBnClickedButtonEditSelection()
{
/*	POSITION pos = m_data_list.GetFirstSelectedItemPosition();
	if( !pos ) return;
	int list_item = m_data_list.GetNextSelectedItem(pos);
	LdrObject* object = reinterpret_cast<LdrObject*>(m_data_list.GetItemData(list_item));

	AddObjectDlg object_dlg(this, "Edit Object:");
	object_dlg.m_object_string = object->GetSourceString().c_str();
	INT_PTR result = object_dlg.DoModal();
	if( result == IDOK )
	{
		char const* source_string = object_dlg.m_object_string.GetString();
		int length = object_dlg.m_object_string.GetLength();

		// Replace the selected object with an object created using the string from the dialog
		StringParser string_parser(m_linedrawer);
		if( string_parser.Parse(source_string, length) && string_parser.GetNumObjects() > 0 )
		{
			LdrObject* new_object		= string_parser.GetObject(0);
			new_object->m_enabled	= object->m_enabled;
			new_object->m_parent	= object->m_parent;
			new_object->m_wireframe	= object->m_wireframe;

			// Replace 'object' with 'new_object'
			m_linedrawer->m_data_manager.AddObject(new_object, object);
			m_linedrawer->m_data_manager.DeleteObject(object);
		}
	}
	m_linedrawer->Refresh();
*/
}

//*****
// Delete the selection
void DataManagerGUI::OnBnClickedButtonDelSelection()
{
	// None selected? No deleting...
	if( m_data_list.GetSelectedCount() == 0 ) return;

	// Confirm
	int result = MessageBox("Delete selected objects?", "Delete Confirmation:", MB_YESNO);
	if( result != IDYES ) return;

	// Delete the selected objects
	POSITION pos;
	do
	{
		pos = m_data_list.GetFirstSelectedItemPosition();
		if( !pos ) break; // This can happen if a child of a deleted object is also selected
		int list_item = m_data_list.GetNextSelectedItem(pos);
		m_data_list.SetItemState(list_item, 0, LVIS_SELECTED);		
		LdrObject* object = reinterpret_cast<LdrObject*>(m_data_list.GetItemData(list_item));
		m_linedrawer->m_data_manager.DeleteObject(object);
	}
	while( pos != 0 );
	m_linedrawer->Refresh();
}

//*****
// Expand the whole tree or all of the children of a selected item
void DataManagerGUI::OnBnClickedButtonExpandAll()
{
	HTREEITEM selected = m_data_tree.GetSelectedItem();
	if( selected )
	{
		LdrObject* object = reinterpret_cast<LdrObject*>(m_data_tree.GetItemData(selected));
		Expand(object, true);
	}
	else
	{
		uint num_objects = m_linedrawer->m_data_manager.GetNumObjects();
		for( uint i = 0; i < num_objects; ++i )
		{
			LdrObject* object = m_linedrawer->m_data_manager.GetObject(i);
			Expand(object, true);
		}
	}
}

//*****
// Collapse the whole tree or all of the children of a selected item
void DataManagerGUI::OnBnClickedButtonCollapseAll()
{
	HTREEITEM selected = m_data_tree.GetSelectedItem();
	if( selected )
	{
		LdrObject* object = reinterpret_cast<LdrObject*>(m_data_tree.GetItemData(selected));
		Collapse(object);
	}
	else
	{
		uint num_objects = m_linedrawer->m_data_manager.GetNumObjects();
		for( uint i = 0; i < num_objects; ++i )
		{
			LdrObject* object = m_linedrawer->m_data_manager.GetObject(i);
			Collapse(object);
		}
	}
}

//************************* List Ctrl Members **************************
//*****
// Expand an object in the tree from the list
void DataManagerGUI::OnNMDblclkListData(NMHDR *pNMHDR, LRESULT *pResult)
{
	LPNMHEADER header = reinterpret_cast<LPNMHEADER>(pNMHDR);
	*pResult = 0;
	if( header->iItem == INVALID_LIST_ITEM ) return;

	LdrObject* object = reinterpret_cast<LdrObject*>(m_data_list.GetItemData(header->iItem));
	if( !object->m_child.empty() && object->m_child[0]->m_list_item == INVALID_LIST_ITEM )
	{
		Expand(object, false);
	}
	else
	{
		Collapse(object);
	}
}

//*****
// Accept key presses
void DataManagerGUI::OnLvnKeydownListData(NMHDR *pNMHDR, LRESULT *pResult)
{
	LPNMLVKEYDOWN	keydown = reinterpret_cast<LPNMLVKEYDOWN>(pNMHDR);
	*pResult = 0;
	CWnd* wnd;
	switch( keydown->wVKey )
	{
	case 'a':
	case 'A':
		if( GetKeyState(VK_CONTROL) & 0x80000000 )
		{
            SelectNone();
			OnBnClickedButtonInvSelection();
		}
		else
		{
			OnBnClickedButtonToggleAlpha();
		}
		break;

	case 'w':
	case 'W':
		OnBnClickedButtonToggleWire();
		break;

	case 'v':
	case 'V':
	case ' ':	// Space
		OnBnClickedButtonToggleVisibility();
		break;

	case VK_DELETE:
		OnBnClickedButtonDelSelection();
		break;

	case 96:	// '0' on the numpad
		::SetFocus(m_linedrawer->m_window_handle);
		break;

	case 115:	//F4
		wnd = GetDlgItem(IDC_EDIT_SELECT_MASK);
		if( wnd ) { wnd->SetFocus(); }
		break;

	default:
		break;
	};
	m_linedrawer->Refresh();
}

//*****
// A list item state's changed
void DataManagerGUI::OnLvnItemchangedListData(NMHDR *pNMHDR, LRESULT *pResult)
{
	LPNMLISTVIEW pNMLV = reinterpret_cast<LPNMLISTVIEW>(pNMHDR);
	*pResult = 0;
	
	// An item was selected
	if( pNMLV->uNewState & LVIS_SELECTED )
	{
		LdrObject* object = reinterpret_cast<LdrObject*>(m_data_list.GetItemData(pNMLV->iItem));
		SelectObject(object, true);
	}
}

//************************* Tree Ctrl Members **************************
//*****
// Expand an item in the tree
void DataManagerGUI::OnTvnItemexpandedDataTree(NMHDR *pNMHDR, LRESULT *pResult)
{
	// FIX ME
	// Prevent some insane recursive loop :/
	static int doin_it = false;
	if( !doin_it )
	{
		doin_it = true;
		
		LPNMTREEVIEW pNMTreeView = reinterpret_cast<LPNMTREEVIEW>(pNMHDR);
		LdrObject* object = reinterpret_cast<LdrObject*>(m_data_tree.GetItemData(pNMTreeView->itemNew.hItem));
		if( pNMTreeView->action == TVE_EXPAND )
		{
			Expand(object, false);
		}
		else if( pNMTreeView->action == TVE_COLLAPSE )
		{
			Collapse(object);
		}
		else PR_ASSERT_STR(PR_DBG_LDR, false, "Unknown action");
		
		doin_it = false;
	}
	*pResult = 0;
}

//*****
// An item in the tree has been selected
void DataManagerGUI::OnTvnSelchangedDataTree(NMHDR *pNMHDR, LRESULT *pResult)
{
	LPNMTREEVIEW pNMTreeView = reinterpret_cast<LPNMTREEVIEW>(pNMHDR);
	*pResult = 0;

	SelectNone();
	LdrObject* object = reinterpret_cast<LdrObject*>(m_data_tree.GetItemData(pNMTreeView->itemNew.hItem));
	PR_ASSERT(PR_DBG_LDR, object->m_list_item != INVALID_LIST_ITEM);
	SelectObject(object, true);
}

//*****
// Accept key presses
void DataManagerGUI::OnTvnKeydownDataTree(NMHDR *pNMHDR, LRESULT *pResult)
{
	OnLvnKeydownListData(pNMHDR, pResult);
}

//************************* Misc Members **************************
//*****
// Select using a string mask
void DataManagerGUI::OnEnChangeEditSelectMask()
{
	UpdateData(TRUE);

	CWnd* wnd = GetFocus();
	m_data_list.SetFocus();

	int num_items = m_data_list.GetItemCount();
	for( int i = 0; i < num_items; ++i )
	{
		CString name = m_data_list.GetItemText(i, EColumn_Name);
		if( name.Find(m_selection_mask) != INVALID_LIST_ITEM )
		{
            m_data_list.SetItemState(i, LVIS_SELECTED, LVIS_SELECTED);
		}
		else
		{
            m_data_list.SetItemState(i, 0, LVIS_SELECTED);
		}
	}
	
	wnd->SetFocus();
}

//*****
// Accept key presses at dialog level
void DataManagerGUI::OnKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags)
{
	CWnd* wnd;
	switch( nChar )
	{
	case 96:	// '0' on the numpad
		wnd = GetDlgItem(IDC_LIST_DATA);
		if( wnd ) wnd->SetFocus();
		break;
	case 115:	//F4
		wnd = GetDlgItem(IDC_EDIT_SELECT_MASK);
		if( wnd ) wnd->SetFocus();
		break;
	};
	CDialog::OnKeyDown(nChar, nRepCnt, nFlags);
}


//**********************************************************************************8
// Private Methods
//*****
// Collapse 'object' and its children in the tree. Remove 'object's children from the list
void DataManagerGUI::Collapse(LdrObject* object)
{
	CollapseRecursive(object);

	// Fix the indices of the remaining list members
	FixListCtrlReferences(object->m_list_item);
}

//*****
// Recursively collapse objects in the tree. Depth-first so that we can remove items from the list control
void DataManagerGUI::CollapseRecursive(LdrObject* object)
{
	uint num_children = (uint)object->m_child.size();
	for( uint c = num_children - 1; c < num_children; --c )
	{
		LdrObject* child = object->m_child[c];

		CollapseRecursive(child);

		// Remove this child from the list control
		if( child->m_list_item != INVALID_LIST_ITEM )
		{
			m_data_list.DeleteItem(child->m_list_item);
			child->m_list_item = INVALID_LIST_ITEM;
		}
	}

	// Collapse this tree item
	m_data_tree.Expand(object->m_tree_item, TVE_COLLAPSE);
}

//*****
// Expand this object in the tree and add its children to the list control
void DataManagerGUI::Expand(LdrObject* object, bool recursive)
{
	ExpandRecursive(object, recursive, object->m_list_item + 1);

	// Fix the indices of the remaining list members
	FixListCtrlReferences(object->m_list_item + 1);
}

// Expand this object and all of its children. Add all children to the list control.
// Use depth-first so that we can insert into the list using the same list index each time
void DataManagerGUI::ExpandRecursive(LdrObject* object, bool recursive, int list_position)
{
	uint num_children = (uint)object->m_child.size();
	for( uint c = num_children - 1; c < num_children; --c )
	{
		LdrObject* child = object->m_child[c];

		if( recursive ) ExpandRecursive(child, true, list_position);

		// Add this child to the list control
		if( child->m_list_item == INVALID_LIST_ITEM )
		{
			child->m_list_item = list_position;
			m_data_list.InsertItem (list_position, child->m_name.c_str());
			m_data_list.SetItemData(list_position, reinterpret_cast<DWORD_PTR>(child));
			UpdateListItem(child, false);
		}
	}

	// Expand this tree item
	m_data_tree.Expand(object->m_tree_item, TVE_EXPAND);
}

// For each object in the list update its index
void DataManagerGUI::FixListCtrlReferences(int start_index)
{
	for( int i = start_index, i_end = m_data_list.GetItemCount(); i < i_end; ++i )
	{
		LdrObject* object = reinterpret_cast<LdrObject*>(m_data_list.GetItemData(i));
		object->m_list_item = i;
	}
}

// Clear the selection
void DataManagerGUI::SelectNone()
{
	CWnd* wnd = GetFocus();
	m_data_list.SetFocus();

	POSITION pos;
	while( (pos = m_data_list.GetFirstSelectedItemPosition()) != 0 )
	{
		m_data_list.SetItemState(m_data_list.GetNextSelectedItem(pos), 0, LVIS_SELECTED);
	}

	if( wnd ) wnd->SetFocus();
}

// Select a particular element
void DataManagerGUI::SelectObject(LdrObject* object, bool make_visible)
{
	PR_ASSERT(PR_DBG_LDR, object);
	if( object->m_list_item != INVALID_LIST_ITEM )
	{
		CWnd* wnd = GetFocus();
		m_data_list.SetFocus();
		m_data_list.SetItemState(object->m_list_item, LVIS_SELECTED, LVIS_SELECTED);
		if( make_visible )
		{
			m_data_list.EnsureVisible(object->m_list_item, FALSE);
			m_data_tree.EnsureVisible(object->m_tree_item);
		}
		if( wnd ) wnd->SetFocus();

		m_selection_changed = true;

		if( m_linedrawer->m_user_settings.m_show_selection_box )
			m_linedrawer->Refresh();
	}
}

// Post a message to repaint the dialog
void DataManagerGUI::Refresh()
{
	if( !m_refresh_pending )
	{
		m_refresh_pending = true;
		PostMessage(WM_PAINT);
	}
}

