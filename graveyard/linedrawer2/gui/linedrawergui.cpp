//*********************************************
// The Line Drawer main GUI
//	(C)opyright Rylogic Limited 2007
//*********************************************
#include "Stdafx.h"
#include "pr/common/Fmt.h"
#include "pr/common/PRString.h"
#include "pr/common/keystate.h"
#include "pr/gui/mfc_helper.h"
#include "LineDrawer/Source/LineDrawer.h"
#include "LineDrawer/GUI/LineDrawerGUI.h"
#include "LineDrawer/GUI/About.h"
#include "LineDrawer/Source/DataManager.h"
#include "LineDrawer/GUI/ViewPropertiesDlg.h"
#include "LineDrawer/GUI/AutoClearDlg.h"
#include "LineDrawer/GUI/LightingDlg.h"
#include "LineDrawer/GUI/AutoRefreshDlg.h"
#include "LineDrawer/GUI/AddObjectDlg.h"
#include "LineDrawer/GUI/OptionsDlg.h"
#include "LineDrawer/GUI/LineDrawerGUI.h"
#include "LineDrawer/Source/EventTypes.h"
#include "LineDrawer/Objects/LdrObjects.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

BEGIN_MESSAGE_MAP(LineDrawerGUI, CDialog)
	ON_WM_DESTROY()
	ON_WM_SYSCOMMAND()
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	ON_WM_DROPFILES()
	ON_WM_MOUSEMOVE()
	ON_WM_MOUSEWHEEL()
	ON_WM_LBUTTONDOWN()
	ON_WM_LBUTTONUP()
	ON_WM_LBUTTONDBLCLK()
	ON_WM_MBUTTONDOWN()
	ON_WM_MBUTTONUP()
	ON_WM_MBUTTONDBLCLK()
	ON_WM_RBUTTONDOWN()
	ON_WM_RBUTTONUP()
	ON_WM_RBUTTONDBLCLK()
	ON_WM_KEYDOWN()
	ON_WM_KEYUP()
	ON_WM_GETMINMAXINFO()
	ON_WM_SIZE()
	ON_WM_MOVE()
	ON_COMMAND(ID_HELP,							&LineDrawerGUI::OnHelp)
	ON_COMMAND(ID_AUTO_REFRESH_FROM_FILE,		&LineDrawerGUI::OnAutoRefreshFromFile)
	ON_COMMAND(ID_POLL_CAMERA,					&LineDrawerGUI::OnPollCamera)
	ON_COMMAND(ID_STEP_PLUGIN,					&LineDrawerGUI::OnStepPlugIn)
	ON_COMMAND(ID_REFRESH,						&LineDrawerGUI::OnRefresh)
	ON_COMMAND(ID_FILE_LDR_CONSOLE,				&LineDrawerGUI::OnFileLdrConsole)
	ON_COMMAND(ID_FILE_LUA_CONSOLE,				&LineDrawerGUI::OnFileLuaConsole)
	ON_COMMAND(ID_FILE_OPEN,					&LineDrawerGUI::OnFileOpen)
	ON_COMMAND(ID_FILE_ADDITIVEOPEN,			&LineDrawerGUI::OnFileAdditiveopen)
	ON_COMMAND(ID_FILE_SAVE,					&LineDrawerGUI::OnFileSave)
	ON_COMMAND(ID_FILE_SAVEAS,					&LineDrawerGUI::OnFileSaveas)
	ON_COMMAND(ID_FILE_OPTIONS,					&LineDrawerGUI::OnFileOptions)
	ON_COMMAND(ID_FILE_RUNPLUGIN,				&LineDrawerGUI::OnFileRunplugin)
	ON_COMMAND(ID_JUMPTO_ORIGIN,				&LineDrawerGUI::OnNavigationJumptoOrigin)
	ON_COMMAND(ID_JUMPTO_VISIBLE,				&LineDrawerGUI::OnNavigationJumptoVisible)
	ON_COMMAND(ID_JUMPTO_SELECTED,				&LineDrawerGUI::OnNavigationJumptoSelected)
	ON_COMMAND(ID_NAVIGATION_SELECTNEXT,		&LineDrawerGUI::OnNavigationSelectNext)
	ON_COMMAND(ID_NAVIGATION_SELECTPREV,		&LineDrawerGUI::OnNavigationSelectPrev)
	ON_COMMAND(ID_ALIGNTO_X,					&LineDrawerGUI::OnNavigationAligntoX)
	ON_COMMAND(ID_ALIGNTO_Y,					&LineDrawerGUI::OnNavigationAligntoY)
	ON_COMMAND(ID_ALIGNTO_Z,					&LineDrawerGUI::OnNavigationAligntoZ)
	ON_COMMAND(ID_ALIGNTO_SELECTED,				&LineDrawerGUI::OnNavigationAligntoSelected)
	ON_COMMAND(ID_NAVIGATION_SHOWORIGIN,		&LineDrawerGUI::OnNavigationShoworigin)
	ON_COMMAND(ID_NAVIGATION_SHOWAXIS,			&LineDrawerGUI::OnNavigationShowaxis)
	ON_COMMAND(ID_NAVIGATION_SHOWFOCUSPOINT,	&LineDrawerGUI::OnNavigationShowfocuspoint)
	ON_COMMAND(ID_NAVIGATION_LOCK,				&LineDrawerGUI::OnNavigationLock)
	ON_COMMAND(ID_NAVIGATION_LOCKTOSELECTION,	&LineDrawerGUI::OnNavigationLocktoselection)
	ON_COMMAND(ID_NAVIGATION_LOOKATORIGIN,		&LineDrawerGUI::OnNavigationVieworigin)
	ON_COMMAND(ID_VIEW_TOPDOWN,					&LineDrawerGUI::OnNavigationViewTopdown)
	ON_COMMAND(ID_VIEW_BOTTOMUP,				&LineDrawerGUI::OnNavigationViewBottomup)
	ON_COMMAND(ID_VIEW_LEFTSIDE,				&LineDrawerGUI::OnNavigationViewLeftside)
	ON_COMMAND(ID_VIEW_RIGHTSIDE,				&LineDrawerGUI::OnNavigationViewRightside)
	ON_COMMAND(ID_VIEW_FRONT,					&LineDrawerGUI::OnNavigationViewFront)
	ON_COMMAND(ID_VIEW_BACK,					&LineDrawerGUI::OnNavigationViewBack)
	ON_COMMAND(ID_NAVIGATION_VIEWPROPERTIES,	&LineDrawerGUI::OnNavigationViewproperties)
	ON_COMMAND(ID_FREECAMERA_OFF,				&LineDrawerGUI::OnNavigationFreeCameraOff)
	ON_COMMAND(ID_FREECAMERA_FREECAMERA,		&LineDrawerGUI::OnNavigationFreeCameraFreeCam)
	ON_COMMAND(ID_DATA_SELECTNONE,				&LineDrawerGUI::OnDataSelectNone)
	ON_COMMAND(ID_DATA_SHOWSELECTION,			&LineDrawerGUI::OnDataShowselection)
	ON_COMMAND(ID_DATA_CLEAR,					&LineDrawerGUI::OnDataClear)
	ON_COMMAND(ID_DATA_AUTOCLEAR,				&LineDrawerGUI::OnDataAutoclear)
	ON_COMMAND(ID_DATA_AUTOREFRESH,				&LineDrawerGUI::OnDataAutorefresh)
	ON_COMMAND(ID_DATA_PERSISTSTATE,			&LineDrawerGUI::OnDataPersiststate)
	ON_COMMAND(ID_DATA_LISTENER,				&LineDrawerGUI::OnDatalistener)
	ON_COMMAND(ID_DATA_STARTCYCLICOBJECTS,		&LineDrawerGUI::OnDataStartcyclicobjects)
	ON_COMMAND(ID_DATA_STARTANIMATIONS,			&LineDrawerGUI::OnDataAnimation)
	ON_COMMAND(ID_DATA_DATALIST,				&LineDrawerGUI::OnDataDatalist)
	ON_COMMAND(ID_RENDERING_WIREFRAME,			&LineDrawerGUI::OnRenderingWireframe)
	ON_COMMAND(ID_RENDERING_COORDINATES,		&LineDrawerGUI::OnRenderingCoordinates)
	ON_COMMAND(ID_RENDERING_CAMERAWANDER,		&LineDrawerGUI::OnRenderingCamerawander)
	ON_COMMAND(ID_RENDERING_DISABLERENDERING,	&LineDrawerGUI::OnRenderingDisableRendering)
	ON_COMMAND(ID_RENDERING_RENDER2D,			&LineDrawerGUI::OnRenderingRender2d)
	ON_COMMAND(ID_RENDERING_RIGHTHANDED,		&LineDrawerGUI::OnRenderingRighthanded)
	ON_COMMAND(ID_RENDERING_STEREOVIEW,			&LineDrawerGUI::OnRenderingStereoview)
	ON_COMMAND(ID_RENDERING_LIGHTING,			&LineDrawerGUI::OnRenderingLighting)
	ON_COMMAND(ID_WINDOW_ALWAYSONTOP,			&LineDrawerGUI::OnWindowAlwaysontop)
	ON_COMMAND(ID_WINDOW_BACKGROUNDCOLOUR,		&LineDrawerGUI::OnWindowBackgroundcolour)
	ON_COMMAND(ID_WINDOW_LINEDRAWERHELP,		&LineDrawerGUI::OnWindowLinedrawerhelp)
	ON_COMMAND(ID_WINDOW_ABOUT,					&LineDrawerGUI::OnWindowAbout)
	ON_COMMAND(ID_ACCELERATOR_New,				&LineDrawerGUI::OnAcceleratorNew)
	ON_COMMAND(ID_ACCELERATOR_Console,			&LineDrawerGUI::OnAcceleratorConsole)
	ON_COMMAND(ID_ACCELERATOR_Open,				&LineDrawerGUI::OnAcceleratorOpen)
	ON_COMMAND(ID_ACCELERATOR_AdditiveOpen,		&LineDrawerGUI::OnAcceleratorAdditiveopen)
	ON_COMMAND(ID_ACCELERATOR_Save,				&LineDrawerGUI::OnAcceleratorSave)
	ON_COMMAND(ID_ACCELERATOR_SaveAs,			&LineDrawerGUI::OnAcceleratorSaveas)
	ON_COMMAND(ID_ACCELERATOR_RunPlugin,		&LineDrawerGUI::OnAcceleratorRunplugin)
	ON_COMMAND(ID_ACCELERATOR_Lighting,			&LineDrawerGUI::OnAcceleratorLighting)
	ON_COMMAND(ID_ACCELERATOR_Focus,			&LineDrawerGUI::OnAcceleratorFocus)
	ON_COMMAND(ID_ACCELERATOR_Wireframe,		&LineDrawerGUI::OnAcceleratorWireframe)
	ON_COMMAND_RANGE(ID_RECENTFILES_RECENTFILESSTART, ID_RECENTFILES_RECENTFILESLAST, &LineDrawerGUI::OnRecentfilesSelect)
END_MESSAGE_MAP()

// Converts a CPoint to a pr::v2
inline v2 CPointTov2(const CPoint& pt) { return v2::make(static_cast<float>(pt.x), static_cast<float>(pt.y)); }

//*****
// LineDrawerGUI dialog constructor
LineDrawerGUI::LineDrawerGUI(CWnd* pParent)
:CDialog(LineDrawerGUI::IDD, pParent)
,m_linedrawer(&LineDrawer::Get())
,m_nav(LineDrawer::Get().m_navigation_manager)
,m_dm(LineDrawer::Get().m_data_manager)
,m_coords(pParent)
,m_new_object_string("")
,m_mouse_down_pt(0,0)
,m_mouse_left_down_at(BUTTON_NOT_DOWN)
,m_mouse_middle_down_at(BUTTON_NOT_DOWN)
,m_mouse_right_down_at(BUTTON_NOT_DOWN)
,m_resize_needed(false)
{
	m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
	ZeroMemory(m_menu_item_state, sizeof(m_menu_item_state));
}

//*****
// LineDrawerGUI destructor
LineDrawerGUI::~LineDrawerGUI()
{}

// Data exchange
void LineDrawerGUI::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
}

// Return the state of a menu item
bool LineDrawerGUI::GetMenuItemState(EMenuItemsWithState item) const
{
	return m_menu_item_state[item];
}

// Update the check on a menu item
void LineDrawerGUI::UpdateMenuItemState(EMenuItemsWithState item, bool new_state)
{
	m_menu_item_state[item] = new_state;
	UINT menu_id = 0;
	switch( item )
	{
	case EMenuItemsWithState_PlugInRunning:			menu_id = ID_FILE_RUNPLUGIN;				break;
	case EMenuItemsWithState_ShowOrigin:			menu_id = ID_NAVIGATION_SHOWORIGIN;			break;
	case EMenuItemsWithState_ShowAxis:				menu_id = ID_NAVIGATION_SHOWAXIS;			break;
	case EMenuItemsWithState_ShowFocus:				menu_id = ID_NAVIGATION_SHOWFOCUSPOINT;		break;
	case EMenuItemsWithState_AlignToX:				menu_id = ID_ALIGNTO_X;						break;
	case EMenuItemsWithState_AlignToY:				menu_id = ID_ALIGNTO_Y;						break;
	case EMenuItemsWithState_AlignToZ:				menu_id = ID_ALIGNTO_Z;						break;
	case EMenuItemsWithState_AlignToSelected:		menu_id = ID_ALIGNTO_SELECTED;				break;
	case EMenuItemsWithState_LockToSelection:		menu_id = ID_NAVIGATION_LOCKTOSELECTION;	break;
	case EMenuItemsWithState_FreeCamera_Off:		menu_id = ID_FREECAMERA_OFF;				break;
	case EMenuItemsWithState_FreeCamera_FreeCam:	menu_id = ID_FREECAMERA_FREECAMERA;			break;
	case EMenuItemsWithState_ShowSelectionBox:		menu_id = ID_DATA_SHOWSELECTION;			break;
	case EMenuItemsWithState_PersistState:			menu_id = ID_DATA_PERSISTSTATE;				break;
	case EMenuItemsWithState_AutoRefresh:			menu_id = ID_DATA_AUTOREFRESH;				break;
	case EMenuItemsWithState_Listener:				menu_id = ID_DATA_LISTENER;					break;
	case EMenuItemsWithState_CyclicsStarted:		menu_id = ID_DATA_STARTCYCLICOBJECTS;		break;
	case EMenuItemsWithState_ShowCoords:			menu_id = ID_RENDERING_COORDINATES;			break;
	case EMenuItemsWithState_CameraWander:			menu_id = ID_RENDERING_CAMERAWANDER;		break;
	case EMenuItemsWithState_DisableRendering:		menu_id = ID_RENDERING_DISABLERENDERING;	break;
	case EMenuItemsWithState_Render2d:				menu_id = ID_RENDERING_RENDER2D;			break;
	case EMenuItemsWithState_RightHanded:			menu_id = ID_RENDERING_RIGHTHANDED;			break;
	case EMenuItemsWithState_StereoView:			menu_id = ID_RENDERING_STEREOVIEW;			break;
	case EMenuItemsWithState_AlwaysOnTop:			menu_id = ID_WINDOW_ALWAYSONTOP;			break;
	default: PR_ASSERT_STR(PR_DBG_LDR, false, "Unknown menu item");
	};

	CMenu* menu = GetMenu();
	if( menu ) menu->CheckMenuItem(menu_id, (new_state) ? (MF_CHECKED) : (MF_UNCHECKED));
}

// Add menu items for each of the recent files
void LineDrawerGUI::UpdateRecentFiles()
{
	HMENU menu = GetMenuByName(m_linedrawer->m_window_handle, "&File,&Recent Files");

	// Empty the menu
	while( ::RemoveMenu(menu, 0, MF_BYPOSITION) ) {}

	// Add the recent files to the menu
	uint f = 0;
	const UserSettings& settings = m_linedrawer->m_user_settings;
	for( UserSettings::TStringList::const_iterator iter = settings.m_recent_files.begin(), iter_end = settings.m_recent_files.end(); iter != iter_end; ++iter )
	{
		::AppendMenu(menu, MF_STRING, ID_RECENTFILES_RECENTFILESSTART + f, (LPCTSTR)iter->c_str());
		++f;
	}
}

// Return true if the camera should be aligned to 'axis'
bool LineDrawerGUI::GetCameraAlignAxis(v4& axis)
{
	if( GetMenuItemState(EMenuItemsWithState_AlignToX) )
	{
		m4x4 o2w = m4x4Identity;
		if( GetMenuItemState(EMenuItemsWithState_AlignToSelected) )
			m_linedrawer->m_data_manager_GUI->GetSelectionTransform(o2w);
		axis = o2w.x;
		return true;
	}
	if( GetMenuItemState(EMenuItemsWithState_AlignToY) )
	{
		m4x4 o2w = m4x4Identity;
		if( GetMenuItemState(EMenuItemsWithState_AlignToSelected) )
			m_linedrawer->m_data_manager_GUI->GetSelectionTransform(o2w);
		axis = o2w.y;
		return true;
	}
	if( GetMenuItemState(EMenuItemsWithState_AlignToZ) )
	{
		m4x4 o2w = m4x4Identity;
		if( GetMenuItemState(EMenuItemsWithState_AlignToSelected) )
			m_linedrawer->m_data_manager_GUI->GetSelectionTransform(o2w);
		axis = o2w.z;
		return true;
	}
	return false;
}

// Position the window so that it's on screen but near where it was last time it was shut down
void LineDrawerGUI::SetInitialWindowPosition()
{
	int screen_left   = GetSystemMetrics(SM_XVIRTUALSCREEN);
	int screen_top    = GetSystemMetrics(SM_YVIRTUALSCREEN);
	int screen_width  = GetSystemMetrics(SM_CXVIRTUALSCREEN);
	int screen_height = GetSystemMetrics(SM_CYVIRTUALSCREEN);

	CRect& window_pos = m_linedrawer->m_user_settings.m_window_pos;
	if( window_pos.Width() == 0 || window_pos.Height() == 0 )
	{
		screen_width  = GetSystemMetrics(SM_CXSCREEN);
		screen_height = GetSystemMetrics(SM_CYSCREEN);
		const int WIDTH   = 800;
		const int HEIGHT  = 600;
		window_pos.left   = (screen_width - WIDTH) / 2;
		window_pos.top    = (screen_height - HEIGHT) / 2;
		window_pos.right  = window_pos.left + WIDTH;
		window_pos.bottom = window_pos.top + HEIGHT;
	}
	if( window_pos.left < screen_left )
	{
		window_pos.right = screen_left + window_pos.Width();
		window_pos.left  = screen_left;
	}
	if( window_pos.top < screen_top )
	{
		window_pos.bottom = screen_top + window_pos.Height();
		window_pos.top    = screen_top;
	}
	if( window_pos.Width() > screen_width )
	{
		window_pos.right = window_pos.left + screen_width;
	}
	if( window_pos.Height() > screen_height )
	{
		window_pos.bottom = window_pos.top + screen_height;
	}
	if( window_pos.right > screen_left + screen_width )
	{
		window_pos.left -= (window_pos.right - (screen_left + screen_width));
		window_pos.right = screen_left + screen_width;
	}
	if( window_pos.bottom > screen_top + screen_height )
	{
		window_pos.top   -= (window_pos.bottom - (screen_top + screen_height));
		window_pos.bottom = screen_top + screen_height;
	}
	MoveWindow(&window_pos);
}

// Handle mouse movements as manipulation of a selected object
void LineDrawerGUI::MouseMoveManipulate(CPoint const& point)
{
// TODO, on tab key down, turn on show selection, on up, turn it off...
// if not on from the menu...hmm, nasty was_already_down bool probably needed

	LdrObject* obj = m_dm.GetSelectedObject();
	if( !obj ) return;

	float const accurate_scale = 0.05f;
	if( MouseM() || (MouseL() && MouseR()) )
	{
		float delta = static_cast<float>(m_mouse_down_pt.y - point.y);
		if( KeyDown(VK_SHIFT) ) delta *= accurate_scale;
		v4 translation = m_nav.ConvertToWSTranslationZ();
		obj->m_object_to_parent.pos += translation;
		m_linedrawer->Refresh();
	}
	else if( MouseL() )
	{
		v2 delta = CPointTov2(point - m_mouse_down_pt);
		if( KeyDown(VK_SHIFT) ) delta *= accurate_scale;
	
		m4x4 o2w = obj->ObjectToWorld();
		v4 translation = m_nav.ConvertToWSTranslation(delta, o2w.pos);
		obj->m_object_to_parent.pos += translation;
		m_linedrawer->Refresh();
	}
	else if( MouseR() )
	{
		v2 delta = CPointTov2(point - m_mouse_down_pt);
		if( KeyDown(VK_SHIFT) ) delta *= accurate_scale;
		m4x4 rotation = m_nav.ConvertToWSRotation(delta, CPointTov2(m_mouse_down_pt));
		obj->m_object_to_parent = obj->m_object_to_parent * rotation;
		m_linedrawer->Refresh();
	}
}

// Handle mouse movements as navigation within the scene
void LineDrawerGUI::MouseMoveNavigate(CPoint const& point)
{
	float const accurate_scale = 0.05f;
	if( MouseL() && MouseR() )
	{
		if( KeyDown(VK_SHIFT) )	m_nav.Zoom (static_cast<float>(m_mouse_down_pt.y - point.y));
		else					m_nav.MoveZ(static_cast<float>(point.y - m_mouse_down_pt.y));
		m_linedrawer->Refresh();
	}
	else if( MouseL() )
	{
		v2 delta = CPointTov2(point - m_mouse_down_pt);
		if( KeyDown(VK_SHIFT) ) delta *= accurate_scale;
		m_nav.Translate(delta);
		m_linedrawer->Refresh();
	}
	else if( MouseR() )
	{
		v2 delta = CPointTov2(point - m_mouse_down_pt);
		if( KeyDown(VK_SHIFT) ) delta *= accurate_scale;
		m_nav.Rotate(delta, CPointTov2(m_mouse_down_pt));
		m_linedrawer->Refresh();
	}
	else if( MouseM() )
	{
		float delta = static_cast<float>(m_mouse_down_pt.y - point.y);
		if( KeyDown(VK_SHIFT) ) delta *= accurate_scale;
		m_nav.Zoom(delta);
		m_linedrawer->Refresh();
	}
}

// LineDrawerGUI message handlers **********************************************

// Initialise Dialog
BOOL LineDrawerGUI::OnInitDialog()
{
//	CDialog::OnInitDialog();
	m_haccel = LoadAccelerators(m_linedrawer->m_app_instance, MAKEINTRESOURCE(IDR_ACCELERATOR2));

	// Set big icon then the small icon
	SetIcon(m_hIcon, TRUE);
	SetIcon(m_hIcon, FALSE);

	// Position/Size the window
	SetInitialWindowPosition();

	// Initialise the main app
	m_linedrawer->m_line_drawer_GUI = this;
	m_linedrawer->m_window_handle	= GetSafeHwnd();
	if( !m_linedrawer->Initialise() ) PostQuitMessage(0);

	// Create the coordinates dialog
	m_coords.Create(CCoordinatesDlg::IDD, this);

	OnNavigationFreeCameraOff();
	return TRUE;
}

// Override the process message filter to include translating accelerator keys
BOOL LineDrawerGUI::PreTranslateMessage(MSG* pMsg) 
{
    if( m_haccel )
    {
        if( ::TranslateAccelerator(m_hWnd, m_haccel, pMsg) )
			return TRUE;
    }
	return FALSE;
}

// Called when LineDrawer is closed. Used to clean up everything
void LineDrawerGUI::OnCancel()
{
	if( MessageBox("Quit LineDrawer?", "Are you mad?!?", MB_YESNO) != IDYES ) return;
	::WinHelp(m_linedrawer->m_window_handle, "LineDrawer.hlp", HELP_QUIT, 0);
	CDialog::OnCancel();
}

// Called when return is pressed
//void LineDrawerGUI::OnOK()
//{
//	CDialog::OnOK();
//	// Ignore return's
//}

// Receive an event
void LineDrawerGUI::OnEvent(GUIUpdate const& e)
{
	switch( e.m_type )
	{
	case GUIUpdate::EType_GlobalWireframe:
		{
			CMenu* menu = GetMenu();
			if( !menu ) break;
			switch( m_linedrawer->GetGlobalWireframeMode() )
			{// Set the text to the "next" mode
			case 0: menu->ModifyMenu(ID_RENDERING_WIREFRAME, MF_BYCOMMAND, ID_RENDERING_WIREFRAME, "&Wireframe");	break;
			case 1: menu->ModifyMenu(ID_RENDERING_WIREFRAME, MF_BYCOMMAND, ID_RENDERING_WIREFRAME, "&Wire + Solid");break;
			case 2: menu->ModifyMenu(ID_RENDERING_WIREFRAME, MF_BYCOMMAND, ID_RENDERING_WIREFRAME, "&Solid");		break;
			}
		}break;
	}
}

// Called when LineDrawer is closed. Used to clean up everything
void LineDrawerGUI::OnDestroy()
{
	// Turn off camera wander if it's on
	if( m_menu_item_state[EMenuItemsWithState_CameraWander] ) OnRenderingCamerawander();

	m_linedrawer->UnInitialise();
	m_linedrawer->m_line_drawer_GUI = 0;
	CDialog::OnDestroy();
}

// System Command message handler
void LineDrawerGUI::OnSysCommand(UINT nID, LPARAM lParam)
{
	if( (nID & 0xFFF0) == IDM_ABOUTBOX )
	{
		CAboutDlg dlgAbout;
		dlgAbout.DoModal();
	}
	else
		CDialog::OnSysCommand(nID, lParam);
}

// Help was requested
void LineDrawerGUI::OnHelp()
{
	::WinHelp(m_linedrawer->m_window_handle, "LineDrawer.hlp", HELP_FINDER, 0);
}

// Paint the dialog box
void LineDrawerGUI::OnPaint() 
{
	CPaintDC dc(this); // device context for painting

	if( IsIconic() )	// a.k.a is minimised
	{
		SendMessage(WM_ICONERASEBKGND, (WPARAM) dc.GetSafeHdc(), 0);

		// Center icon in client rectangle
		int cxIcon = GetSystemMetrics(SM_CXICON);
		int cyIcon = GetSystemMetrics(SM_CYICON);
		CRect rect;
		GetClientRect(&rect);
		int x = (rect.Width() - cxIcon + 1) / 2;
		int y = (rect.Height() - cyIcon + 1) / 2;

		// Draw the icon
		dc.DrawIcon(x, y, m_hIcon);
	}
	else if( m_linedrawer )
	{
		CDialog::OnPaint();
		m_linedrawer->Render();
	}
}

// Define the limits for resizing
void LineDrawerGUI::OnGetMinMaxInfo(MINMAXINFO* lpMMI)
{
	lpMMI->ptMinTrackSize.x = 260;
	lpMMI->ptMinTrackSize.y = 100;
	CDialog::OnGetMinMaxInfo(lpMMI);
}

// Window re-size
void LineDrawerGUI::OnSize(UINT nType, int cx, int cy) 
{
	CDialog::OnSize(nType, cx, cy);
	if( nType == SIZE_MINIMIZED ) return;
	
	// Todo: Find a way of doing this at the end of resizing
	m_resize_needed = true;
	m_linedrawer->Resize();
	m_linedrawer->Refresh();
	m_resize_needed = false;
}

// Window move
void LineDrawerGUI::OnMove(int x, int y)
{
	if( m_linedrawer->m_window_handle == 0 ) return;

	int width  = m_linedrawer->m_user_settings.m_window_pos.Width();
	int height = m_linedrawer->m_user_settings.m_window_pos.Height();
	m_linedrawer->m_user_settings.m_window_pos.SetRect(x, y, x + width, y + height);
	m_linedrawer->m_user_settings.Save();
}

// No idea...
HCURSOR LineDrawerGUI::OnQueryDragIcon()
{
	return (HCURSOR)m_hIcon;
}

// Accept dropped files
void LineDrawerGUI::OnDropFiles(HDROP hDropInfo)
{
	uint num_files = DragQueryFile(hDropInfo, 0xFFFFFFFF, NULL, 0);
	if( num_files == 0 ) return;

	// Clear the data unless shift is held down
	if( !((GetKeyState(VK_LSHIFT) & 0x8000) || (GetKeyState(VK_RSHIFT) & 0x8000)) )
	{
		m_linedrawer->m_file_loader.ClearSource();
	}
	
	// Load the files
	char filename[1024];
	for( uint i = 0; i < num_files; ++i )
	{
		if( DragQueryFile(hDropInfo, i, filename, 1024) > 0 )
		{
			m_linedrawer->InputFile(filename, true, i == num_files - 1);
		}
	}
}

// Mouse control
void LineDrawerGUI::OnMouseMove(UINT nFlags, CPoint point) 
{
	if( m_linedrawer->m_plugin_manager.Hook_OnMouseMove(CPointTov2(point)) != pr::ldr::EPlugInResult_Handled )
	{
		if( KeyDown(VK_TAB) )
			MouseMoveManipulate(point);
		else
			MouseMoveNavigate(point);

		if( m_menu_item_state[EMenuItemsWithState_ShowCoords] )
		{
			IRect client_area = m_linedrawer->GetClientArea();
			float z =   (m_nav.GetFocusDistance()                    - m_nav.m_camera.GetViewProperty(Camera::Near)) / 
						(m_nav.m_camera.GetViewProperty(Camera::Far) - m_nav.m_camera.GetViewProperty(Camera::Near));

			v4 worldpt = m_nav.m_camera.ScreenToWorld(
				v4::make(
					point.x / (float)client_area.SizeX(),
					point.y / (float)client_area.SizeY(),
					z,
					1.0f));

			v4 focuspt = m_nav.GetFocusPoint();

			m_coords.m_mouse.SetWindowText(Fmt("{%f %f %f}", worldpt[0], worldpt[1], worldpt[2]).c_str());
			m_coords.m_focus.SetWindowText(Fmt("{%f %f %f}", focuspt[0], focuspt[1], focuspt[2]).c_str());
		}

		// Screen wrap
		if( MouseL() || MouseM() || MouseR() ) // if a button is pressed
		{
			CPoint pt = m_mouse_down_pt;
			ClientToScreen(&pt);
			SetCursorPos(pt.x, pt.y);
			//CPoint pt = point;
			//CRect area; GetClientRect(&area);
			//bool update_cursor = false;
			//if( pt.x <= 0 )					{ pt.x = area.right - 2;	m_mouse_move_last_point[0] += (float)area.Width();	update_cursor = true; }
			//if( pt.x >= area.right - 1 )	{ pt.x = 1;					m_mouse_move_last_point[0] -= (float)area.Width();	update_cursor = true; }
			//if( pt.y <= 0 )					{ pt.y = area.bottom - 2;	m_mouse_move_last_point[1] += (float)area.Height();	update_cursor = true; }
			//if( pt.y >= area.bottom - 1 )	{ pt.y = 1;					m_mouse_move_last_point[1] -= (float)area.Height();	update_cursor = true; }
			//if( update_cursor )
			//{
			//	ClientToScreen(&pt);
			//	SetCursorPos(pt.x, pt.y);
			//}
		}
	}
	CDialog::OnMouseMove(nFlags, point);
}

// Zooooooom
BOOL LineDrawerGUI::OnMouseWheel(UINT nFlags, short zDelta, CPoint pt) 
{
	if( m_linedrawer->m_plugin_manager.Hook_OnMouseWheel(nFlags, zDelta, CPointTov2(pt)) != pr::ldr::EPlugInResult_Handled )
	{
		if( KeyDown(VK_SHIFT) )	m_nav.TranslateZ(static_cast<float>(zDelta));
		else					m_nav.MoveZ     (static_cast<float>(zDelta));
		m_linedrawer->Refresh();
	}
	return CDialog::OnMouseWheel(nFlags, zDelta, pt);
}

// Mouse left click in the client area
void LineDrawerGUI::OnLButtonDown(UINT nFlags, CPoint point)
{
	if( m_linedrawer->m_plugin_manager.Hook_OnMouseDown(VK_LBUTTON, CPointTov2(point)) != pr::ldr::EPlugInResult_Handled )
	{
		SetCapture();
		m_mouse_down_pt = point;
		m_mouse_left_down_at = static_cast<uint>(GetMessageTime());
	}
	CDialog::OnLButtonDown(nFlags, point);
}
void LineDrawerGUI::OnLButtonUp(UINT nFlags, CPoint point)
{
	if( m_linedrawer->m_plugin_manager.Hook_OnMouseUp(VK_LBUTTON, CPointTov2(point)) != pr::ldr::EPlugInResult_Handled )
	{
		if( !MouseM() && !MouseR() ) { ReleaseCapture(); }
		uint mouse_up_at = static_cast<uint>(GetMessageTime());
		bool click = (mouse_up_at - m_mouse_left_down_at) < MAX_SINGLE_CLICK_TIME;
		m_mouse_left_down_at = BUTTON_NOT_DOWN;
		if( click )
		{
			if( m_linedrawer->m_plugin_manager.Hook_OnMouseClk(VK_LBUTTON, CPointTov2(point)) != pr::ldr::EPlugInResult_Handled )
			{
				if( m_menu_item_state[EMenuItemsWithState_ShowSelectionBox] )
				{
					CRect client_area = m_linedrawer->IRectToCRect(m_linedrawer->GetClientArea());
					float x = point.x / static_cast<float>(client_area.Width());
					float y = point.y / static_cast<float>(client_area.Height());
					m_linedrawer->m_data_manager.Select(v2::make(x, y));
					m_linedrawer->Refresh();
				}
			}
		}
	}
	CDialog::OnLButtonUp(nFlags, point);
}
void LineDrawerGUI::OnLButtonDblClk(UINT nFlags, CPoint point)
{
	m_linedrawer->m_plugin_manager.Hook_OnMouseDblClk(VK_LBUTTON, CPointTov2(point));
	CDialog::OnLButtonDblClk(nFlags, point);
}

// Mouse middle click in the client area
void LineDrawerGUI::OnMButtonDown(UINT nFlags, CPoint point)
{
	if( m_linedrawer->m_plugin_manager.Hook_OnMouseDown(VK_MBUTTON, CPointTov2(point)) != pr::ldr::EPlugInResult_Handled )
	{
		SetCapture();
		m_mouse_down_pt = point;
		m_mouse_middle_down_at = static_cast<uint>(GetMessageTime());
	}
	CDialog::OnMButtonDown(nFlags, point);
}
void LineDrawerGUI::OnMButtonUp(UINT nFlags, CPoint point)
{
	if( m_linedrawer->m_plugin_manager.Hook_OnMouseUp(VK_MBUTTON, CPointTov2(point)) != pr::ldr::EPlugInResult_Handled )
	{
		if( !MouseL() && !MouseR() ) { ReleaseCapture(); }
		uint mouse_up_at = static_cast<uint>(GetMessageTime());
		bool click = (mouse_up_at - m_mouse_middle_down_at) < MAX_SINGLE_CLICK_TIME;
		m_mouse_middle_down_at = BUTTON_NOT_DOWN;
		if( click )
		{
			if( m_linedrawer->m_plugin_manager.Hook_OnMouseClk(VK_MBUTTON, CPointTov2(point)) != pr::ldr::EPlugInResult_Handled )
			{
				m_nav.SetZoom(1.0f);
				m_linedrawer->Refresh();
			}
		}
	}
	CDialog::OnMButtonUp(nFlags, point);
}
void LineDrawerGUI::OnMButtonDblClk(UINT nFlags, CPoint point)
{
	m_linedrawer->m_plugin_manager.Hook_OnMouseDblClk(VK_MBUTTON, CPointTov2(point));
	CDialog::OnMButtonDblClk(nFlags, point);
}

// Mouse right click in the client area
void LineDrawerGUI::OnRButtonDown(UINT nFlags, CPoint point)
{
	if( m_linedrawer->m_plugin_manager.Hook_OnMouseDown(VK_RBUTTON, CPointTov2(point)) != pr::ldr::EPlugInResult_Handled )
	{
		SetCapture();
		m_mouse_down_pt = point;
		m_mouse_right_down_at = static_cast<uint>(GetMessageTime());
	}
	CDialog::OnRButtonDown(nFlags, point);
}
void LineDrawerGUI::OnRButtonUp(UINT nFlags, CPoint point)
{
	if( m_linedrawer->m_plugin_manager.Hook_OnMouseUp(VK_RBUTTON, CPointTov2(point)) != pr::ldr::EPlugInResult_Handled )
	{
		// If the left mouse button is not currently down, release the mouse
		if( !MouseL() && !MouseM() ) { ReleaseCapture(); }
		uint mouse_up_at = static_cast<uint>(GetMessageTime());
		bool click = (mouse_up_at - m_mouse_right_down_at) < MAX_SINGLE_CLICK_TIME;
		m_mouse_right_down_at = BUTTON_NOT_DOWN;
		if( click )
		{
			if( m_linedrawer->m_plugin_manager.Hook_OnMouseClk(VK_RBUTTON, CPointTov2(point)) != pr::ldr::EPlugInResult_Handled )
			{
				//CRect client_area = m_linedrawer->GetClientArea();
				//float x = point.x / static_cast<float>(client_area.Width());
				//float y = point.y / static_cast<float>(client_area.Height());
				// Right click
			}
		}
	}
	CDialog::OnRButtonUp(nFlags, point);
}
void LineDrawerGUI::OnRButtonDblClk(UINT nFlags, CPoint point)
{
	m_linedrawer->m_plugin_manager.Hook_OnMouseDblClk(VK_RBUTTON, CPointTov2(point));
	CDialog::OnRButtonDblClk(nFlags, point);
}

// Key commands
void LineDrawerGUI::OnKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags) 
{
	CDialog::OnKeyDown(nChar, nRepCnt, nFlags);

	// Allow a plugin to intercept key presses
	if( m_linedrawer->m_plugin_manager.Hook_OnKeyDown(nChar, nRepCnt, nFlags) != pr::ldr::EPlugInResult_Handled )
	{
		switch( nChar )
		{
		//case 33:	m_nav.MoveZ(10.0f); m_linedrawer->Refresh(); break; // Page up 
		//case 34:	m_nav.MoveZ(-10.0f); m_linedrawer->Refresh(); break; // Page down
		//case 36:	m_nav.Zoom(10.0f); m_linedrawer->Refresh();	 break; //Home
		//case 35:	m_nav.Zoom(-10.0f); m_linedrawer->Refresh(); break; //End
		case VK_F5:	m_linedrawer->RefreshFromFile(static_cast<uint>(GetMessageTime()), false); break;
		case VK_F6:	OnNavigationJumptoOrigin(); break;
		case VK_F7:	OnNavigationJumptoVisible(); break;
		case VK_F8:	OnNavigationJumptoSelected(); break;
		case VK_OEM_PERIOD:	OnNavigationSelectNext(); break;
		case VK_OEM_COMMA:	OnNavigationSelectPrev(); break;
		case 96:	// '0' on the numpad
		case ' ':	OnDataDatalist(); break;
		};
	}
}

void LineDrawerGUI::OnKeyUp(UINT nChar, UINT nRepCnt, UINT nFlags)
{
	CDialog::OnKeyUp(nChar, nRepCnt, nFlags);
	
	// Allow a plugin to intercept key presses
	if( m_linedrawer->m_plugin_manager.Hook_OnKeyUp(nChar, nRepCnt, nFlags) != pr::ldr::EPlugInResult_Handled )
	{
		//...
	}
}

// Process a refresh from file message
void LineDrawerGUI::OnAutoRefreshFromFile()
{
	if( m_linedrawer->m_file_loader.AreAnyFilesModified() && !m_linedrawer->m_file_loader.AreAnyFilesLocked() )
	{
		m_linedrawer->RefreshFromFile(static_cast<uint>(GetMessageTime()), m_linedrawer->m_file_loader.m_auto_recentre);
	}
	else
	{
		m_linedrawer->m_file_loader.m_refresh_pending = false;
	}
}

// Process a step inertial camera message
void LineDrawerGUI::OnPollCamera()
{
	m_nav.StepCamera();
}

// Process a step PlugIn message
void LineDrawerGUI::OnStepPlugIn()
{
	m_linedrawer->m_plugin_manager.StepPlugIn();
}

// Render the scene
void LineDrawerGUI::OnRefresh()
{
	m_linedrawer->Render();
}

//*******************************************************************************
//                                                                      File Menu
// Add a new object to the scene
void LineDrawerGUI::OnFileLdrConsole()
{
	AddObjectDlg add_object_dlg(this, "Add New Object:");
	add_object_dlg.m_object_string = m_linedrawer->m_user_settings.m_new_object_string.c_str();
	INT_PTR result = add_object_dlg.DoModal();
	if( result != IDOK ) return;
	m_linedrawer->m_user_settings.m_new_object_string = add_object_dlg.m_object_string.GetString();
	m_linedrawer->m_user_settings.Save();
	m_linedrawer->RefreshFromString(m_linedrawer->m_user_settings.m_new_object_string.c_str(), (uint)m_linedrawer->m_user_settings.m_new_object_string.size(), false, false);
}

// Write some lua to add objects to the scene
void LineDrawerGUI::OnFileLuaConsole()
{
	m_linedrawer->m_lua_input.ShowConsole(true);
//	AddObjectDlg add_object_dlg(this, "Add New Object:");
//	add_object_dlg.m_object_string = m_linedrawer->m_user_settings.m_new_object_string.c_str();
//	INT_PTR result = add_object_dlg.DoModal();
//	if( result != IDOK ) return;
//	m_linedrawer->m_user_settings.m_new_object_string = add_object_dlg.m_object_string.GetString();
//	m_linedrawer->m_user_settings.Save();
//	m_linedrawer->RefreshFromString(m_linedrawer->m_user_settings.m_new_object_string.c_str(), (uint)m_linedrawer->m_user_settings.m_new_object_string.size(), false, false);
}

// Open a source file
void LineDrawerGUI::OnFileOpen() 
{
	CFileDialog filedlg(TRUE);
	filedlg.GetOFN().lpstrTitle = "Open a script file";
	INT_PTR result = filedlg.DoModal();
	if( result != IDOK ) return;

	m_linedrawer->InputFile(filedlg.GetPathName().GetString(), false);
}

// Open a source file without clearing the current data
void LineDrawerGUI::OnFileAdditiveopen()
{
	CFileDialog filedlg(TRUE);
	filedlg.GetOFN().lpstrTitle = "Open a script file (additive)";
	INT_PTR result = filedlg.DoModal();
	if( result != IDOK ) return;

	m_linedrawer->InputFile(filedlg.GetPathName().GetString(), true);
	m_linedrawer->RefreshFromFile(static_cast<uint>(GetMessageTime()), false);
}

// Save the current scene
void LineDrawerGUI::OnFileSave()
{
	std::string current_filename = m_linedrawer->m_file_loader.GetCurrentFilename();
	if( current_filename.empty() )	OnFileSaveas();
	else							m_linedrawer->m_data_manager.SaveToFile(current_filename.c_str());
}

// Save the current scene to a new file
void LineDrawerGUI::OnFileSaveas()
{
	CFileDialog filedlg(FALSE);
	INT_PTR result = filedlg.DoModal();
	if( result != IDOK ) return;

	m_linedrawer->m_data_manager.SaveToFile(filedlg.GetPathName());
}

// Display the options dialog
void LineDrawerGUI::OnFileOptions()
{
	OptionsDlg options_dlg(this);

	// Set the options from the user settings
	options_dlg.m_shader_version			= m_linedrawer->m_user_settings.m_shader_version.c_str();
	options_dlg.m_geometry_quality			= m_linedrawer->m_user_settings.m_geometry_quality - rdr::EQuality::Low;
	options_dlg.m_texture_quality			= m_linedrawer->m_user_settings.m_texture_quality  - rdr::EQuality::Low;
	options_dlg.m_ignore_missing_includes	= m_linedrawer->m_user_settings.m_ignore_missing_includes;
	options_dlg.m_error_output_msgbox		= m_linedrawer->m_user_settings.m_error_output_msgbox;
	options_dlg.m_error_output_log			= m_linedrawer->m_user_settings.m_error_output_to_file;
	options_dlg.m_error_log_filename		= m_linedrawer->m_user_settings.m_error_output_log_filename.c_str();
	options_dlg.m_focus_point_size			= (int)(m_linedrawer->m_user_settings.m_asterix_scale * 200.0f);
	options_dlg.m_reset_camera_on_load		= m_linedrawer->m_user_settings.m_reset_camera_on_load;
	options_dlg.m_enable_resource_monitor	= m_linedrawer->m_user_settings.m_enable_resource_monitor;

	INT_PTR result = options_dlg.DoModal();
	if( result != IDOK ) return;

	// Set the options from the user settings
	m_linedrawer->m_user_settings.m_shader_version				= options_dlg.m_shader_version.GetString();
	m_linedrawer->m_user_settings.m_geometry_quality			= (rdr::EQuality::Type)(options_dlg.m_geometry_quality);
	m_linedrawer->m_user_settings.m_texture_quality				= (rdr::EQuality::Type)(options_dlg.m_texture_quality);
	m_linedrawer->m_user_settings.m_ignore_missing_includes		= options_dlg.m_ignore_missing_includes == TRUE;
	m_linedrawer->m_user_settings.m_error_output_msgbox			= options_dlg.m_error_output_msgbox	== TRUE;
	m_linedrawer->m_user_settings.m_error_output_to_file		= options_dlg.m_error_output_log == TRUE;
	m_linedrawer->m_user_settings.m_error_output_log_filename	= options_dlg.m_error_log_filename.GetString();
	m_linedrawer->m_user_settings.m_asterix_scale				= options_dlg.m_focus_point_size / 200.0f;
	m_linedrawer->m_user_settings.m_reset_camera_on_load		= options_dlg.m_reset_camera_on_load == TRUE;
	m_linedrawer->m_user_settings.m_enable_resource_monitor		= options_dlg.m_enable_resource_monitor == TRUE;

	if( m_linedrawer->m_user_settings.m_error_output_to_file )
	{
		m_linedrawer->m_error_output.ResetLogFile(m_linedrawer->m_user_settings.m_error_output_log_filename.c_str());
	}

	// Save and apply the user settings
	m_linedrawer->m_user_settings.Save();
	m_linedrawer->ApplyUserSettings();
}

// Find a line drawer plugin and ask the plugin manager to run it
void LineDrawerGUI::OnFileRunplugin()
{
	if( !m_menu_item_state[EMenuItemsWithState_PlugInRunning] )
	{
		CFileDialog filedlg(TRUE, "dll");
		filedlg.GetOFN().lpstrTitle = "Select a line drawer plugin";
		INT_PTR result = filedlg.DoModal();
		if( result != IDOK ) return;
		
		m_linedrawer->m_plugin_manager.StartPlugIn(filedlg.GetPathName(), ldr::TArgs());
	}
	else
	{
		m_linedrawer->m_plugin_manager.StopPlugIn();
	}
}

//*****
// Recent files
void LineDrawerGUI::OnRecentfilesSelect(UINT nID)
{
	HMENU menu = GetMenuByName(m_linedrawer->m_window_handle, "&File,&Recent Files");

	char string[256];
	if( ::GetMenuString(menu, nID - ID_RECENTFILES_RECENTFILESSTART, string, 256, MF_BYPOSITION) )
	{
		m_linedrawer->InputFile(string, (GetKeyState(VK_LSHIFT) & 0x8000)||(GetKeyState(VK_RSHIFT) & 0x8000));
	}
}


//*******************************************************************************
//                                                                Navigation Menu
//*****
// Return the camera to the origin
void LineDrawerGUI::OnNavigationJumptoOrigin()
{
	m_nav.ApplyView();
	m_linedrawer->Refresh();
}

//*****
// View the visible objects only
void LineDrawerGUI::OnNavigationJumptoVisible()
{
	BoundingBox bbox; bbox.reset();

	uint num_enabled = 0;
	uint num_objects = m_linedrawer->m_data_manager.GetNumObjects();
	for( uint i = 0; i < num_objects; ++i )
	{
		LdrObject const* object = m_linedrawer->m_data_manager.GetObject(i);
		if( object->m_enabled )
		{
			++num_enabled;
			Encompase(bbox, object->WorldSpaceBBox(true));
		}
	}
	if( num_enabled > 0 ) { m_nav.SetView(bbox); m_nav.ApplyView(); }
	m_linedrawer->Refresh();
}

//*****
// View the selected objects
void LineDrawerGUI::OnNavigationJumptoSelected()
{
	BoundingBox bbox;
	if( m_linedrawer->m_data_manager_GUI->GetSelectionBBox(bbox) )
	{
		m_nav.SetView(bbox);
		m_nav.ApplyView();
		m_linedrawer->Refresh();
	}
}

//*****
// Jump between selected objects
void LineDrawerGUI::OnNavigationSelectNext()
{
	DataManager& data_man = m_linedrawer->m_data_manager;

	// If there is no selection, select the nearest
	// otherwise, move to the next object in the data list
	if( data_man.GetSelectedObject() == 0 )
		data_man.SelectNearest(m_nav.GetFocusPoint());
	else
		data_man.SelectNext();

	if( data_man.GetSelectedObject() )
	{
		v4 position = data_man.GetSelectedObject()->ObjectToWorld().pos
					- m_nav.GetFocusDistance() * m_nav.m_camera.GetForward();
		m_nav.RelocateCamera(position, m_nav.m_camera.GetForward(), m_nav.m_camera.GetUp());
		m_linedrawer->Refresh();
	}
}
void LineDrawerGUI::OnNavigationSelectPrev()
{
	DataManager& data_man = m_linedrawer->m_data_manager;

	// If there is no selection, select the nearest
	// otherwise, move to the previous object in the data list
	if( data_man.GetSelectedObject() == 0 )
		data_man.SelectNearest(m_nav.GetFocusPoint());
	else
		data_man.SelectPrev();

	if( data_man.GetSelectedObject() )
	{
		v4 position = data_man.GetSelectedObject()->ObjectToWorld().pos
					- m_nav.GetFocusDistance() * m_nav.m_camera.GetForward();
		m_nav.RelocateCamera(position, m_nav.m_camera.GetForward(), m_nav.m_camera.GetUp());
		m_linedrawer->Refresh();
	}
}

//*****
// Align the camera's Y axis to the x axis. If an object is selected align to it's x axis
void LineDrawerGUI::OnNavigationAligntoX()
{
	UpdateMenuItemState(EMenuItemsWithState_AlignToX, !m_menu_item_state[EMenuItemsWithState_AlignToX]);
	UpdateMenuItemState(EMenuItemsWithState_AlignToY, false);
	UpdateMenuItemState(EMenuItemsWithState_AlignToZ, false);

	m4x4 o2w = m4x4Identity;
	if( GetMenuItemState(EMenuItemsWithState_AlignToSelected) )
		m_linedrawer->m_data_manager_GUI->GetSelectionTransform(o2w);
	if( !IsZero3(Cross3(m_nav.m_camera.GetForward(), o2w.x)) ) m_nav.m_camera.SetUp(o2w.x);
	m_linedrawer->Refresh();
}
void LineDrawerGUI::OnNavigationAligntoY()
{
	UpdateMenuItemState(EMenuItemsWithState_AlignToX, false);
	UpdateMenuItemState(EMenuItemsWithState_AlignToY, !m_menu_item_state[EMenuItemsWithState_AlignToY]);
	UpdateMenuItemState(EMenuItemsWithState_AlignToZ, false);

	m4x4 o2w = m4x4Identity;
	if( GetMenuItemState(EMenuItemsWithState_AlignToSelected) )
		m_linedrawer->m_data_manager_GUI->GetSelectionTransform(o2w);
	if( !IsZero3(Cross3(m_nav.m_camera.GetForward(), o2w.y)) ) m_nav.m_camera.SetUp(o2w.y);
	m_linedrawer->Refresh();
}
void LineDrawerGUI::OnNavigationAligntoZ()
{
	UpdateMenuItemState(EMenuItemsWithState_AlignToX, false);
	UpdateMenuItemState(EMenuItemsWithState_AlignToY, false);
	UpdateMenuItemState(EMenuItemsWithState_AlignToZ, !m_menu_item_state[EMenuItemsWithState_AlignToZ]);

	m4x4 o2w = m4x4Identity;
	if( GetMenuItemState(EMenuItemsWithState_AlignToSelected) )
		m_linedrawer->m_data_manager_GUI->GetSelectionTransform(o2w);
	if( !IsZero3(Cross3(m_nav.m_camera.GetForward(), o2w.z)) ) m_nav.m_camera.SetUp(o2w.z);
	m_linedrawer->Refresh();
}

// Turn on/off aligned to the selected object
void LineDrawerGUI::OnNavigationAligntoSelected()
{
	UpdateMenuItemState(EMenuItemsWithState_AlignToSelected, !m_menu_item_state[EMenuItemsWithState_AlignToSelected]);
	m_linedrawer->Refresh();
}

// Turn on/off the origin
void LineDrawerGUI::OnNavigationShoworigin()
{
	UpdateMenuItemState(EMenuItemsWithState_ShowOrigin, !m_menu_item_state[EMenuItemsWithState_ShowOrigin]);
	m_linedrawer->m_user_settings.m_show_origin = m_menu_item_state[EMenuItemsWithState_ShowOrigin];
	m_linedrawer->m_user_settings.Save();
	m_linedrawer->Refresh();
}

//*****
// Turn on/off the axis
void LineDrawerGUI::OnNavigationShowaxis()
{
	UpdateMenuItemState(EMenuItemsWithState_ShowAxis, !m_menu_item_state[EMenuItemsWithState_ShowAxis]);
	m_linedrawer->m_user_settings.m_show_axis = m_menu_item_state[EMenuItemsWithState_ShowAxis];
	m_linedrawer->m_user_settings.Save();
	m_linedrawer->Refresh();
}

//*****
// Turn on/off the focus point
void LineDrawerGUI::OnNavigationShowfocuspoint()
{
	UpdateMenuItemState(EMenuItemsWithState_ShowFocus, !m_menu_item_state[EMenuItemsWithState_ShowFocus]);
	m_linedrawer->m_user_settings.m_show_focus_point = m_menu_item_state[EMenuItemsWithState_ShowFocus];
	m_linedrawer->m_user_settings.Save();
	m_linedrawer->Refresh();
}

//*****
// Display the camera motion lock dialog
void LineDrawerGUI::OnNavigationLock()
{
	m_linedrawer->m_camera_lock_GUI.m_locks = m_nav.GetLockMask();
	m_linedrawer->m_camera_lock_GUI.ShowGUI();
	m_linedrawer->Refresh();
}

//*****
// Fix the camera to look at the centre of the selection bounding box
void LineDrawerGUI::OnNavigationLocktoselection()
{
	UpdateMenuItemState(EMenuItemsWithState_LockToSelection, !m_menu_item_state[EMenuItemsWithState_LockToSelection]);
	m_nav.LockToSelection(m_menu_item_state[EMenuItemsWithState_LockToSelection]);
	m_linedrawer->Refresh();
}

//*****
// Rotate the camera to look at the centre of the current view
void LineDrawerGUI::OnNavigationVieworigin()
{
	m_nav.LookAtViewCentre();
	m_linedrawer->Refresh();
}

//*****
// Set the camera looking top down on the view volume
void LineDrawerGUI::OnNavigationViewTopdown()
{
	m_nav.ViewTop();
	m_linedrawer->Refresh();
}

//*****
// Set the camera looking bottom up on the view volume
void LineDrawerGUI::OnNavigationViewBottomup()
{
	m_nav.ViewBottom();
	m_linedrawer->Refresh();
}

//*****
// Set the camera looking from the left on the view volume
void LineDrawerGUI::OnNavigationViewLeftside()
{
	m_nav.ViewLeft();
	m_linedrawer->Refresh();
}

//*****
// Set the camera looking from the right on the view volume
void LineDrawerGUI::OnNavigationViewRightside()
{
	m_nav.ViewRight();
	m_linedrawer->Refresh();
}

//*****
// Set the camera looking at the front of the view volume
void LineDrawerGUI::OnNavigationViewFront()
{
	m_nav.ViewFront();
	m_linedrawer->Refresh();
}

//*****
// Set the camera looking at the back of the view volume
void LineDrawerGUI::OnNavigationViewBack()
{
	m_nav.ViewBack();
	m_linedrawer->Refresh();
}

//*****
// Set the view properties
void LineDrawerGUI::OnNavigationViewproperties() 
{
	ViewPropertiesDlg vpdialog(this);
	vpdialog.m_camera_to_world	= m_nav.m_camera.GetCameraToWorld();
	vpdialog.m_focus_point		= m_nav.GetFocusPoint();
	vpdialog.m_near_clip_plane	= m_nav.m_camera.GetViewProperty(Camera::Near);
	vpdialog.m_far_clip_plane	= m_nav.m_camera.GetViewProperty(Camera::Far);
	vpdialog.m_cull_mode		= m_linedrawer->GetCullMode() - 1;
	
	INT_PTR result = vpdialog.DoModal();
	if( result != IDOK ) return;

	m_nav.RelocateCamera(vpdialog.m_camera_to_world.pos, vpdialog.m_camera_to_world.z, vpdialog.m_camera_to_world.y);
	m_nav.m_camera.SetViewProperty(Camera::Near, vpdialog.m_near_clip_plane);
	m_nav.m_camera.SetViewProperty(Camera::Far,  vpdialog.m_far_clip_plane);

	PR_ASSERT(PR_DBG_LDR, D3DCULL_NONE <= vpdialog.m_cull_mode + 1 && vpdialog.m_cull_mode + 1 <= D3DCULL_CCW);
	D3DCULL cull_mode = static_cast<D3DCULL>(pr::Clamp<int>(vpdialog.m_cull_mode + 1, D3DCULL_NONE, D3DCULL_CCW));
	m_linedrawer->SetCullMode(cull_mode);

	m_linedrawer->Refresh();
}

// Turn off the free camera
void LineDrawerGUI::OnNavigationFreeCameraOff()
{
	UpdateMenuItemState(EMenuItemsWithState_FreeCamera_Off, true);
	UpdateMenuItemState(EMenuItemsWithState_FreeCamera_FreeCam, false);
	m_nav.SetCameraMode(ECameraMode_Off);
}

// Turn on the free camera
void LineDrawerGUI::OnNavigationFreeCameraFreeCam()
{
	UpdateMenuItemState(EMenuItemsWithState_FreeCamera_Off, false);
	UpdateMenuItemState(EMenuItemsWithState_FreeCamera_FreeCam, true);
	m_nav.SetCameraMode(ECameraMode_FreeCam);
}

//*******************************************************************************
//                                                                      Data Menu

// Select nothing
void LineDrawerGUI::OnDataSelectNone()
{
	m_linedrawer->m_data_manager_GUI->SelectNone();
	m_linedrawer->Refresh();
}

// Toggle the showing of the selection box
void LineDrawerGUI::OnDataShowselection()
{
	UpdateMenuItemState(EMenuItemsWithState_ShowSelectionBox, !m_menu_item_state[EMenuItemsWithState_ShowSelectionBox]);
	m_linedrawer->m_user_settings.m_show_selection_box = m_menu_item_state[EMenuItemsWithState_ShowSelectionBox];
	m_linedrawer->m_user_settings.Save();
	m_linedrawer->Refresh();
}

//*****
// Manually clear the data
void LineDrawerGUI::OnDataClear()
{
	m_linedrawer->m_data_manager.Clear();
	m_linedrawer->m_file_loader.ClearSource();
	m_linedrawer->m_navigation_manager.SetView(m_linedrawer->m_data_manager.m_bbox);
	m_linedrawer->m_navigation_manager.ApplyView();
	m_linedrawer->Refresh();
}

// Toggle object state persistence
void LineDrawerGUI::OnDataPersiststate()
{
	UpdateMenuItemState(EMenuItemsWithState_PersistState, !m_menu_item_state[EMenuItemsWithState_PersistState]);
	m_linedrawer->m_user_settings.m_persist_object_state = m_menu_item_state[EMenuItemsWithState_PersistState];
	m_linedrawer->m_user_settings.Save();

	CMenu* menu = GetMenu();
	if( menu ) menu->CheckMenuItem(ID_DATA_PERSISTSTATE, (m_menu_item_state[EMenuItemsWithState_PersistState] ? (MF_CHECKED) : (MF_UNCHECKED)));
}

// Set the draw order of the data sets
void LineDrawerGUI::OnDataAutoclear() 
{
	AutoClearDlg acdialog(this);
	acdialog.m_period = m_linedrawer->m_data_manager.GetAutoClearTime();
	INT_PTR result = acdialog.DoModal();
	if( result != IDOK ) return;
	m_linedrawer->m_data_manager.SetAutoClearTime(acdialog.m_period);

	CMenu* menu = GetMenu();
	if( menu ) menu->CheckMenuItem(ID_DATA_AUTOCLEAR, ((acdialog.m_period > 0.0f) ? (MF_CHECKED) : (MF_UNCHECKED)));
}

//*****
// Toogle auto refresh of the data
void LineDrawerGUI::OnDataAutorefresh()
{
	UpdateMenuItemState(EMenuItemsWithState_AutoRefresh, !m_menu_item_state[EMenuItemsWithState_AutoRefresh]);

	if( m_menu_item_state[EMenuItemsWithState_AutoRefresh] )
	{
		AutoRefreshDlg auto_refresh_dlg(this);
		auto_refresh_dlg.m_refresh_period	= m_linedrawer->m_file_loader.m_auto_refresh_time_ms;
		auto_refresh_dlg.m_auto_recentre	= m_linedrawer->m_file_loader.m_auto_recentre;
		INT_PTR result = auto_refresh_dlg.DoModal();
		if( result != IDOK ) UpdateMenuItemState(EMenuItemsWithState_AutoRefresh, false);
		else
		{
			m_linedrawer->m_file_loader.m_auto_refresh_time_ms	= auto_refresh_dlg.m_refresh_period;
			m_linedrawer->m_file_loader.m_auto_recentre			= auto_refresh_dlg.m_auto_recentre == TRUE;
		}
	}
	m_linedrawer->m_file_loader.SetAutoRefresh(m_menu_item_state[EMenuItemsWithState_AutoRefresh]);
}

// Toggle the listener
void LineDrawerGUI::OnDatalistener()
{
	UpdateMenuItemState(EMenuItemsWithState_Listener, !m_menu_item_state[EMenuItemsWithState_Listener]);

	if( m_menu_item_state[EMenuItemsWithState_Listener] )
	{
		m_linedrawer->m_listener.Start();
		//NetworkListenerSettingsDlg	networkdlg(this);
		//networkdlg.m_ip1			= 127;
		//networkdlg.m_ip2			= 0;
		//networkdlg.m_ip3			= 0;
		//networkdlg.m_ip4			= 1;
		//networkdlg.m_port			= 6550;
		//networkdlg.m_sample_period	= m_linedrawer->m_network_input.m_sample_period;
		//networkdlg.m_buffer_size	= m_linedrawer->m_network_input.m_requested_buffer_size;
		//INT_PTR result				= networkdlg.DoModal();
		//if( result != IDOK ) UpdateMenuItemState(EMenuItemsWithState_NetworkListener, false);
		//m_linedrawer->m_network_input.m_sample_period			= networkdlg.m_sample_period;
		//m_linedrawer->m_network_input.m_requested_buffer_size	= networkdlg.m_buffer_size;
		//UpdateMenuItemState(EMenuItemsWithState_NetworkListener, m_linedrawer->m_network_input.Start());
	}
	else
	{
		m_linedrawer->m_listener.Stop();
	}
	m_linedrawer->RefreshWindowText();
}

//*****
// Start / Stop any cyclic objects
void LineDrawerGUI::OnDataStartcyclicobjects()
{
	UpdateMenuItemState(EMenuItemsWithState_CyclicsStarted, !m_menu_item_state[EMenuItemsWithState_CyclicsStarted]);
	m_linedrawer->m_data_manager.SetObjectCyclic(m_menu_item_state[EMenuItemsWithState_CyclicsStarted]);
	m_linedrawer->Poller(m_menu_item_state[EMenuItemsWithState_CyclicsStarted]);
	m_linedrawer->Refresh();
}

//*****
// Start / Stop any animated objects
void LineDrawerGUI::OnDataAnimation()
{
	//UpdateMenuItemState(AnimationsStarted, !m_menu_item_state[AnimationsStarted]);
	//.SetAnimations(m_menu_item_state[AnimationsStarted]);
	//m_linedrawer->SetAnimation(m_menu_item_state[AnimationsStarted]);
	//m_linedrawer->Poller(m_menu_item_state[AnimationsStarted]);
	//m_linedrawer->Refresh();

	m_linedrawer->m_animation_control.ShowGUI();
	m_linedrawer->Refresh();
}

//*****
// Display the data list dialog
void LineDrawerGUI::OnDataDatalist()
{
	m_linedrawer->m_data_manager.ShowGUI();
	m_linedrawer->Refresh();
}

//*******************************************************************************
//                                                                 Rendering Menu
//*****
// Toggle the render mode between solid and wireframe
void LineDrawerGUI::OnRenderingWireframe() 
{
	int mode = (m_linedrawer->GetGlobalWireframeMode() + 1) % EGlobalWireframeMode_NumberOf;
	m_linedrawer->SetGlobalWireframeMode(static_cast<EGlobalWireframeMode>(mode));
	m_linedrawer->Refresh();
}

//*****
// Toggle displaying of co-ordinates
void LineDrawerGUI::OnRenderingCoordinates()
{
	UpdateMenuItemState(EMenuItemsWithState_ShowCoords, !m_menu_item_state[EMenuItemsWithState_ShowCoords]);
	m_coords.ShowWindow(m_menu_item_state[EMenuItemsWithState_ShowCoords] ? SW_SHOW : SW_HIDE);
}

//*****
// Toggle camera wandering
void LineDrawerGUI::OnRenderingCamerawander()
{
	UpdateMenuItemState(EMenuItemsWithState_CameraWander, !m_menu_item_state[EMenuItemsWithState_CameraWander]);
	m_nav.SetCameraWander(m_menu_item_state[EMenuItemsWithState_CameraWander] * m_nav.GetFocusDistance() * 0.01f);
	m_linedrawer->Poller(m_menu_item_state[EMenuItemsWithState_CameraWander]);
}

// Enable/Disable rendering
void LineDrawerGUI::OnRenderingDisableRendering()
{
	UpdateMenuItemState(EMenuItemsWithState_DisableRendering, !m_menu_item_state[EMenuItemsWithState_DisableRendering]);
	m_linedrawer->Refresh();
}

// Toggle between 3D and 2D
void LineDrawerGUI::OnRenderingRender2d()
{
	UpdateMenuItemState(EMenuItemsWithState_Render2d, !m_menu_item_state[EMenuItemsWithState_Render2d]);
	m_nav.Set3D(!m_menu_item_state[EMenuItemsWithState_Render2d]);
	m_linedrawer->Refresh();
}

//*****
// Toggle between righthanded and lefthanded
void LineDrawerGUI::OnRenderingRighthanded()
{
	UpdateMenuItemState(EMenuItemsWithState_RightHanded, !m_menu_item_state[EMenuItemsWithState_RightHanded]);
	m_nav.SetRightHanded(m_menu_item_state[EMenuItemsWithState_RightHanded]);
	m_linedrawer->Refresh();
}

//*****
// Turn on a stereo view of the scene
void LineDrawerGUI::OnRenderingStereoview()
{
	UpdateMenuItemState(EMenuItemsWithState_StereoView, !m_menu_item_state[EMenuItemsWithState_StereoView]);
	m_linedrawer->SetStereoView(m_menu_item_state[EMenuItemsWithState_StereoView]);
}

//*****
// Set the lights to use
void LineDrawerGUI::OnRenderingLighting()
{
	rdr::Light	old_light			= m_linedrawer->GetLight();
	bool		old_camera_relative	= m_linedrawer->IsLightCameraRelative();
	
	LightingDlg lightingdlg(this);
	lightingdlg.m_light				= old_light;
	lightingdlg.m_camera_relative	= old_camera_relative;
	INT_PTR result = lightingdlg.DoModal();
	if( result != IDOK )	{ m_linedrawer->SetLight(old_light, old_camera_relative); return; }
	
	m_linedrawer->SetLight(lightingdlg.m_light, lightingdlg.m_camera_relative == TRUE);

	// Save and apply the user settings
	m_linedrawer->m_user_settings.Save();
	m_linedrawer->ApplyUserSettings();
}

//*******************************************************************************
//                                                                    Window Menu
//*****
// Always onatop please
void LineDrawerGUI::OnWindowAlwaysontop() 
{
	UpdateMenuItemState(EMenuItemsWithState_AlwaysOnTop, !m_menu_item_state[EMenuItemsWithState_AlwaysOnTop]);
	SetWindowPos((m_menu_item_state[EMenuItemsWithState_AlwaysOnTop]) ? (&wndTopMost) : (&wndNoTopMost), 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
}

//*****
// Change the background colour
void LineDrawerGUI::OnWindowBackgroundcolour()
{
	Colour32 colour = m_linedrawer->m_renderer->GetBackgroundColour();
	CColorDialog cdialog(colour.GetColorRef(), 0, this);
	INT_PTR result = cdialog.DoModal();
	if( result == IDOK )
	{
		colour = cdialog.GetColor() & 0x00FFFFFF;
		m_linedrawer->m_renderer->SetBackgroundColour(colour);
	}
	m_linedrawer->Refresh();
}

//*****
// Load the help file
void LineDrawerGUI::OnWindowLinedrawerhelp()
{
	::WinHelp(m_linedrawer->m_window_handle, "LineDrawer.hlp", HELP_FINDER, 0);
}

//*****
// About box please...
void LineDrawerGUI::OnWindowAbout() 
{
	OnSysCommand(IDM_ABOUTBOX, 0);	
}

//*******************************************************************************
//                                                                   Accelerators
void LineDrawerGUI::OnAcceleratorNew()				{ OnFileLdrConsole(); }
void LineDrawerGUI::OnAcceleratorConsole()			{ OnFileLuaConsole(); }
void LineDrawerGUI::OnAcceleratorOpen()				{ OnFileOpen(); }
void LineDrawerGUI::OnAcceleratorAdditiveopen()		{ OnFileAdditiveopen(); }
void LineDrawerGUI::OnAcceleratorSave()				{ OnFileSave(); }
void LineDrawerGUI::OnAcceleratorSaveas()			{ OnFileSaveas(); }
void LineDrawerGUI::OnAcceleratorRunplugin()		{ OnFileRunplugin(); }
void LineDrawerGUI::OnAcceleratorLighting()			{ OnRenderingLighting(); }
void LineDrawerGUI::OnAcceleratorFocus()			{ OnNavigationShowfocuspoint(); }
void LineDrawerGUI::OnAcceleratorWireframe()		{ OnRenderingWireframe(); }
