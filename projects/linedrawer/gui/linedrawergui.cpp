//*****************************************************************************************
// LineDrawer
//  Copyright © Rylogic Ltd 2009
//*****************************************************************************************
#include "linedrawer/main/stdafx.h"
#include "linedrawer/gui/linedrawergui.h"
#include "linedrawer/gui/options_dlg.h"
#include "linedrawer/gui/about_dlg.h"
#include "linedrawer/gui/text_panel_dlg.h"
#include "linedrawer/resources/linedrawer.resources.h"
#include "linedrawer/types/ldrexception.h"
#include "linedrawer/plugin/plugin_manager_dlg.h"
#include "linedrawer/utility/misc.h"
#include "linedrawer/utility/debug.h"

namespace
{
	TCHAR LdrFileOpenFilter[] = TEXT("Ldr Script (*.ldr)\0*.ldr\0Lua Script (*.lua)\0*.lua\0DirectX Files (*.x)\0*.x\0All Files (*.*)\0*.*\0\0");
}

// Constructor
LineDrawerGUI::LineDrawerGUI()
	:m_ldr(0)
	,m_status()
	,m_recent_files()
	,m_saved_views()
	,m_sizing(false)
	,m_refresh(false)
	,m_suspend_render(false)
	,m_mouse_status_updates(true)
{}
LineDrawerGUI::~LineDrawerGUI()
{
	delete m_ldr;
}

// Init dialog
BOOL LineDrawerGUI::OnInitDialog(CWindow, LPARAM)
{
	//// center the dialog on the screen
	//CenterWindow();
	extern CAppModule g_app_module;

	// set icons
	HICON hIcon      = (HICON)::LoadImage(g_app_module.GetResourceInstance(), MAKEINTRESOURCE(IDI_ICON_MAIN), IMAGE_ICON, ::GetSystemMetrics(SM_CXICON),   ::GetSystemMetrics(SM_CYICON),   LR_DEFAULTCOLOR);
	HICON hIconSmall = (HICON)::LoadImage(g_app_module.GetResourceInstance(), MAKEINTRESOURCE(IDI_ICON_MAIN), IMAGE_ICON, ::GetSystemMetrics(SM_CXSMICON), ::GetSystemMetrics(SM_CYSMICON), LR_DEFAULTCOLOR);
	SetIcon(hIcon, TRUE);
	SetIcon(hIconSmall, FALSE);

	// Load accelerators
	m_haccel = (HACCEL)::LoadAccelerators(g_app_module.GetResourceInstance(), MAKEINTRESOURCE(IDR_ACCELERATOR2));

	// Register object for message filtering and idle updates
	CMessageLoop* loop = g_app_module.GetMessageLoop(); ATLASSERT(loop != NULL);
	loop->AddMessageFilter(this);

	// Controls
	int status_panes[] = {-1};
	m_status.Create(m_hWnd, 0, TEXT(""), WS_CHILD|WS_VISIBLE|CCS_BOTTOM|CCS_ADJUSTABLE|SBARS_SIZEGRIP, 0, IDC_STATUSBAR_MAIN);
	m_status.SetParts(1, status_panes);

	// Initialise the menu lists
	m_recent_files.Attach(pr::gui::GetMenuByName(GetMenu(), TEXT("&File,&Recent Files"))      ,ID_FILE_RECENTFILES      ,0xffffffff ,this);
	m_saved_views .Attach(pr::gui::GetMenuByName(GetMenu(), TEXT("&Navigation,&Saved Views")) ,ID_NAVIGATION_SAVEDVIEWS ,0xffffffff ,this);

	// Initialise the resize dialog, controls to be resized need to have been created
	DlgResize_Init();

	// Set minimum window size
	m_ptMinTrackSize.x = 320;
	m_ptMinTrackSize.y = 200;

	// Create the main app object
	try { m_ldr = new LineDrawer(*this, GetCommandLineA()); }
	catch (std::exception const& e)
	{
		std::string msg = "Line Drawer failed to start due to an exception.\nReason: ";
		msg += e.what();
		::MessageBoxA(m_hWnd, msg.c_str(), "Startup failure", MB_OK|MB_ICONERROR);
		CloseApp(-1);
		return TRUE;
	}

	// Initialise the object manager
	m_ldr->m_store_ui.Settings(m_ldr->Settings().m_ObjectManagerSettings.c_str());

	// Initialise the recent files list and saved views
	m_recent_files.MaxLength(m_ldr->Settings().m_MaxRecentFiles);
	m_saved_views .MaxLength(m_ldr->Settings().m_MaxSavedViews);
	m_recent_files.Import(m_ldr->Settings().m_RecentFiles);

	// Update the state of the UI
	UpdateUI();

	// Set the initial camera position
	m_ldr->ResetView(pr::ldr::EObjectBounds::All);
	m_ldr->m_nav.CameraAlign(m_ldr->Settings().m_CameraAlignAxis);

	// Start the main timer
	OnTimerTick(0);
	return TRUE;
}

// Pre translate windows messages
BOOL LineDrawerGUI::PreTranslateMessage(MSG* pMsg)
{
	//pr::DebugMessage(pMsg);

	// Forward messages for the store ui to that dialog
	if (m_ldr != 0 && m_ldr->m_store_ui.IsChild(pMsg->hwnd))
		return IsDialogMessage(pMsg);

	// Handle key accelerators
	if (::TranslateAccelerator(m_hWnd, m_haccel, pMsg) != 0)
		return TRUE;

	// Intersept key presses
	if (pMsg->message == WM_KEYDOWN)
	{
		LRESULT result;
		ProcessWindowMessage(pMsg->hwnd, pMsg->message, pMsg->wParam, pMsg->lParam, result);
		return TRUE; // has been translated and dispatched
	}

	// Don't call IsDialogMessage(pMsg) because we don't want typical
	// dialog window-like keyboard behaviour (e.g. tab or arrow keys
	// to switch focus between controls, etc)
	return FALSE;
}

// Idle processing
BOOL LineDrawerGUI::OnIdle(int)
{
	// If the settings have changed, save them
	if (m_ldr->Settings().SaveRequired())
		m_ldr->Settings().Save();

	return FALSE;
}

// Handler timer messages
void LineDrawerGUI::OnTimerTick(UINT_PTR)
{
	// If file watching is turned on, look for changed files
	if (m_ldr->Settings().m_WatchForChangedFiles)
		m_ldr->m_files.RefreshChangedFiles();

	// Orbit the camera if enabled
	if (m_ldr->Settings().m_CameraOrbit)
	{
		m_ldr->m_nav.OrbitCamera(m_ldr->Settings().m_CameraOrbitSpeed);
		m_refresh = true;
	}

	// Poll stepable plugin's
	m_ldr->m_plugin_mgr.Poll();

	// If a refresh has been flagged, render now
	//if (m_refresh)
	{
		m_ldr->Render();
		m_refresh = false;
	}

	SetTimer(ID_MAIN_TIMER, 1, 0);
}

// System commands
void LineDrawerGUI::OnSysCommand(UINT nID, CPoint)
{
	switch (nID)
	{
	default: SetMsgHandled(FALSE); break;
	case SC_CLOSE: CloseApp(0); break;
	}
}

// Resizing the window has finished.
void LineDrawerGUI::OnEnterSizeMove()
{
	m_sizing = true;
}

// Cause redraws while sizing, but don't change the render target size, just use stretch blt
void LineDrawerGUI::OnSize(UINT nType, CSize)
{
	// Don't "handle" size messages so that the WTL
	// resizing stuff gets a chance to see these messages
	SetMsgHandled(FALSE);
	if (!m_sizing && nType != SIZE_MINIMIZED) Resize();
	m_refresh = true;
}

// Resizing the window has finished.
void LineDrawerGUI::OnExitSizeMove()
{
	m_sizing = false;
	Resize();
}

// Clear the background during resize
BOOL LineDrawerGUI::OnEraseBkgnd(CDCHandle dc)
{
	SetMsgHandled(FALSE);
	if (m_sizing)
	{
		CBrush brush; brush.CreateSolidBrush(m_ldr->Settings().m_BackgroundColour.GetColorRef());
		CRect r; GetClientRect(&r);
		CPoint ctr = r.CenterPoint();
		dc.FillRect(&r, brush);
		dc.SetTextAlign(TA_CENTER|TA_BASELINE);
		dc.SetBkMode(TRANSPARENT);
		dc.TextOutA(ctr.x, ctr.y, "...resizing...");
	}
	return TRUE;
}

// Paint the window
void LineDrawerGUI::OnPaint(CDCHandle)
{
	SetMsgHandled(FALSE);
	if (m_ldr && !m_sizing && !m_suspend_render)
		m_ldr->Render();
}

// Handle files dropped onto the main window
void LineDrawerGUI::OnDropFiles(HDROP hDropInfo)
{
	UINT num_files = ::DragQueryFileA(hDropInfo, 0xFFFFFFFF, NULL, 0);
	if (num_files == 0) return;

	// Clear the data unless shift is held down
	if (!pr::KeyDown(VK_SHIFT))
		m_ldr->m_files.Clear();

	// Load the files
	std::string path;
	for (UINT i = 0; i != num_files; ++i)
	{
		path.resize(::DragQueryFileA(hDropInfo, i, 0, 0) + 1, 0);
		if (::DragQueryFile(hDropInfo, i, &path[0], UINT(path.size())) != 0)
			m_ldr->m_files.Add(path.c_str());
	}
}

// Open a text panel for adding new ldr objects immediately
LRESULT LineDrawerGUI::OnFileNew(WORD, WORD, HWND, BOOL&)
{
	CTextEntryDlg dlg(m_hWnd, "Create new ldr objects:", m_ldr->Settings().m_NewObjectString.c_str(), true);
	pr::IRect r  = pr::WindowBounds(m_hWnd);
	dlg.m_width  = pr::Max(100, r.SizeX() - 50);
	dlg.m_height = pr::Max(60,  r.SizeY() - 50);
	if (dlg.DoModal() != IDOK) return S_OK;

	try
	{
		m_ldr->Settings().m_NewObjectString = dlg.m_body;
		m_ldr->Settings().Save();
		pr::ldr::AddString(m_ldr->m_rdr, m_ldr->Settings().m_NewObjectString.c_str(), m_ldr->m_store);
		m_refresh = true;
	}
	catch (LdrException const& e)
	{
		switch (e.code())
		{
		default: throw;
		case ELdrException::SourceScriptError:
			pr::events::Send(ldr::Event_Error(pr::FmtS("Script error found while parsing source.\nError details: %s", e.m_msg.c_str())));
			break;
		}
	}
	return S_OK;
}

// Create a new text file for ldr script
LRESULT LineDrawerGUI::OnFileNewScript(WORD, WORD, HWND, BOOL&)
{
	WTL::CFileDialog fd(FALSE,0,0,0,LdrFileOpenFilter,m_hWnd);
	if (fd.DoModal() != IDOK) return S_OK;
	FileNew(fd.m_szFileName);
	return S_OK;
}

// Open a line drawer script file
LRESULT LineDrawerGUI::OnFileOpen(WORD, WORD, HWND, BOOL&)
{
	WTL::CFileDialog fd(TRUE,0,0,0,LdrFileOpenFilter,m_hWnd);
	if (fd.DoModal() != IDOK) return S_OK;
	FileOpen(fd.m_szFileName, false);
	return S_OK;
}

// Open a file and add it to the current scene
LRESULT LineDrawerGUI::OnFileOpenAdditive(WORD, WORD, HWND, BOOL&)
{
	CFileDialog fd(TRUE,0,0,0,LdrFileOpenFilter,m_hWnd);
	if (fd.DoModal() != IDOK) return S_OK;
	FileOpen(fd.m_szFileName, true);
	return S_OK;
}

// Display the options dialog
LRESULT LineDrawerGUI::OnFileShowOptions(WORD, WORD, HWND, BOOL&)
{
	COptionsDlg dlg(m_ldr->Settings(), m_hWnd);
	if (dlg.DoModal() != IDOK) return S_OK;
	dlg.GetSettings(m_ldr->Settings());
	m_refresh = true;
	return S_OK;
}

// Display the plugin manager dialog
LRESULT LineDrawerGUI::OnFilePluginMgr(WORD, WORD, HWND, BOOL&)
{
	PluginManagerDlg dlg(m_ldr->m_plugin_mgr, m_hWnd);
	if (dlg.DoModal() != IDOK) return S_OK;
	return S_OK;
}

// Close the dialog event
LRESULT LineDrawerGUI::OnAppClose(WORD, WORD wID, HWND, BOOL&)
{
	CloseApp(wID);
	return S_OK;
}

// Reset the view to all, selected, or visible objects
LRESULT LineDrawerGUI::OnResetView(WORD, WORD wID, HWND, BOOL&)
{
	switch (wID)
	{
	default:
	case ID_NAV_RESETVIEW_ALL:      m_ldr->ResetView(pr::ldr::EObjectBounds::All); break;
	case ID_NAV_RESETVIEW_SELECTED: m_ldr->ResetView(pr::ldr::EObjectBounds::Selected); break;
	case ID_NAV_RESETVIEW_VISIBLE:  m_ldr->ResetView(pr::ldr::EObjectBounds::Visible); break;
	}
	m_refresh = true;
	return S_OK;
}

// Align the camera to the selected axis
LRESULT LineDrawerGUI::OnNavAlign(WORD, WORD wID, HWND, BOOL&)
{
	switch (wID)
	{
	default:
	case ID_NAV_ALIGN_NONE:    m_ldr->m_nav.CameraAlign(pr::v4Zero); break;
	case ID_NAV_ALIGN_X:       m_ldr->m_nav.CameraAlign(pr::v4XAxis); break;
	case ID_NAV_ALIGN_Y:       m_ldr->m_nav.CameraAlign(pr::v4YAxis); break;
	case ID_NAV_ALIGN_Z:       m_ldr->m_nav.CameraAlign(pr::v4ZAxis); break;
	case ID_NAV_ALIGN_CURRENT: m_ldr->m_nav.CameraAlign(m_ldr->m_nav.CameraToWorld().y); break;
	}
	m_ldr->Settings().m_CameraAlignAxis = m_ldr->m_nav.CameraAlign();
	UpdateUI();
	m_refresh = true;
	return S_OK;
}

// View the current focus point looking down the selected axis
LRESULT LineDrawerGUI::OnViewAxis(WORD, WORD wID, HWND, BOOL&)
{
	pr::v4 axis;
	switch (wID)
	{
	default: axis = m_ldr->m_nav.CameraToWorld().z; break;
	case ID_VIEW_AXIS_POSX:     axis =  pr::v4XAxis; break;
	case ID_VIEW_AXIS_NEGX:     axis = -pr::v4XAxis; break;
	case ID_VIEW_AXIS_POSY:     axis =  pr::v4YAxis; break;
	case ID_VIEW_AXIS_NEGY:     axis = -pr::v4YAxis; break;
	case ID_VIEW_AXIS_POSZ:     axis =  pr::v4ZAxis; break;
	case ID_VIEW_AXIS_NEGZ:     axis = -pr::v4ZAxis; break;
	case ID_VIEW_AXIS_POSXYZ:   axis = -pr::v4::make(0.577350f, 0.577350f, 0.577350f, 0.0f); break;
	}

	pr::m4x4 c2w = m_ldr->m_nav.CameraToWorld();
	pr::v4 focus = m_ldr->m_nav.FocusPoint();
	pr::v4 cam = focus + axis * m_ldr->m_nav.FocusDistance();
	pr::v4 up = pr::Parallel(axis, c2w.y) ? pr::Cross3(axis, c2w.x) : c2w.y;
	m_ldr->m_nav.LookAt(cam, focus, up);
	m_refresh = true;
	return S_OK;
}

// Receord the current camera position as a saved camera view
LRESULT LineDrawerGUI::OnSaveView(WORD, WORD wID, HWND, BOOL&)
{
	if (wID == ID_NAVIGATION_CLEARSAVEDVIEWS)
	{
		m_ldr->m_nav.ClearSavedViews();
		m_saved_views.Clear();
	}
	else
	{
		CTextEntryDlg dlg(m_hWnd, "Label for this view", pr::FmtS("view%d", m_saved_views.Items().size()), false);
		if (dlg.DoModal() != IDOK) return S_OK;

		NavManager::SavedViewID id = m_ldr->m_nav.SaveView();
		m_saved_views.Add(dlg.m_body.c_str(), (void*)id, false, true);
	}
	return S_OK;
}

// Set the position of the camera focus point in world space
LRESULT LineDrawerGUI::OnSetFocusPosition(WORD, WORD, HWND, BOOL&)
{
	CTextEntryDlg dlg(m_hWnd, "Entry focus point position", "0 0 0", false);
	if (dlg.DoModal() != IDOK) return S_OK;

	float pos[3];
	if (pr::str::ExtractRealArrayC(&pos[0], 3, dlg.m_body.c_str()))
		m_ldr->m_nav.FocusPoint(pr::v4::make(pos, 1.0f));
	else
		MessageBoxA("Format incorrect", "Focus point not set", MB_OK|MB_ICONERROR);

	m_refresh = true;
	return S_OK;
}

// Toggle camera orbit mode
LRESULT LineDrawerGUI::OnOrbit(WORD, WORD, HWND, BOOL&)
{
	m_ldr->Settings().m_CameraOrbit = !m_ldr->Settings().m_CameraOrbit;
	m_ldr->m_nav.OrbitCamera(0.0f);
	UpdateUI();
	return S_OK;
}

// Generate a self created scene of objects
LRESULT LineDrawerGUI::OnCreateDemoScene(WORD, WORD, HWND, BOOL&)
{
	m_ldr->CreateDemoScene();
	m_ldr->ResetView(pr::ldr::EObjectBounds::All);
	m_refresh = true;
	return S_OK;
}

// Display the object manager UI
LRESULT LineDrawerGUI::OnShowObjectManagerUI(WORD, WORD, HWND, BOOL&)
{
	m_ldr->m_store_ui.Show(true);
	return S_OK;
}

// Spawn the text editor with the source files
LRESULT LineDrawerGUI::OnEditSourceFiles(WORD, WORD, HWND, BOOL&)
{
	OpenTextEditor(m_ldr->m_files.List());
	return S_OK;
}

// Remove all objects from the object manager
LRESULT LineDrawerGUI::OnDataClearScene(WORD, WORD, HWND, BOOL&)
{
	m_ldr->m_store.clear();
	m_refresh = true;
	return S_OK;
}

// Toggle auto refresh file sources
LRESULT LineDrawerGUI::OnDataAutoRefresh(WORD, WORD, HWND, BOOL&)
{
	m_ldr->Settings().m_WatchForChangedFiles = !m_ldr->Settings().m_WatchForChangedFiles;
	UpdateUI();
	return S_OK;
}

// Toggle visibility of the focus point
LRESULT LineDrawerGUI::OnShowFocus(WORD, WORD, HWND, BOOL&)
{
	m_ldr->Settings().m_ShowFocusPoint = !m_ldr->Settings().m_ShowFocusPoint;
	UpdateUI();
	m_refresh = true;
	return S_OK;
}

// Toggle visibility of the origin point
LRESULT LineDrawerGUI::OnShowOrigin(WORD, WORD, HWND, BOOL&)
{
	m_ldr->Settings().m_ShowOrigin = !m_ldr->Settings().m_ShowOrigin;
	UpdateUI();
	m_refresh = true;
	return S_OK;
}

// Toggle visibility of the selection box
LRESULT LineDrawerGUI::OnShowSelection(WORD, WORD, HWND, BOOL&)
{
	m_ldr->Settings().m_ShowSelectionBox = !m_ldr->Settings().m_ShowSelectionBox;
	UpdateUI();
	m_refresh = true;
	return S_OK;
}

// Toggle visibility of the object space bounding boxes
LRESULT LineDrawerGUI::OnShowObjBBoxes(WORD, WORD, HWND, BOOL&)
{
	m_ldr->Settings().m_ShowObjectBBoxes = !m_ldr->Settings().m_ShowObjectBBoxes;
	UpdateUI();
	m_refresh = true;
	return S_OK;
}

// Cycle through solid, wireframe, and solid+wire
LRESULT LineDrawerGUI::OnToggleRenderMode(WORD, WORD, HWND, BOOL&)
{
	int mode = (m_ldr->Settings().m_GlobalRenderMode + 1) % EGlobalRenderMode::NumberOf;
	m_ldr->Settings().m_GlobalRenderMode = static_cast<EGlobalRenderMode::Type>(mode);
	UpdateUI();
	m_refresh = true;
	return S_OK;
}

// Toggle between perspective and orthographic
LRESULT LineDrawerGUI::OnRender2D(WORD, WORD, HWND, BOOL&)
{
	m_ldr->m_nav.Render2D(!m_ldr->m_nav.Render2D());
	UpdateUI();
	m_refresh = true;
	return S_OK;
}

// Display the lighting dialog
LRESULT LineDrawerGUI::OnShowLightingDlg(WORD, WORD, HWND, BOOL&)
{
	struct PreviewLighting
	{
		LineDrawer* m_ldr;
		PreviewLighting(LineDrawer* ldr) :m_ldr(ldr) {}
		void operator()(pr::rdr::Light const& light, bool camera_relative)
		{
			pr::rdr::Light prev_light   = m_ldr->Settings().m_Light;
			bool           prev_cam_rel = m_ldr->Settings().m_LightIsCameraRelative;
			m_ldr->Settings().m_Light                    = light;
			m_ldr->Settings().m_LightIsCameraRelative = camera_relative;
			m_ldr->Render();
			m_ldr->Settings().m_Light                 = prev_light;
			m_ldr->Settings().m_LightIsCameraRelative = prev_cam_rel;
		}
	};

	PreviewLighting pv(m_ldr);
	pr::rdr::LightingDlg<PreviewLighting> dlg(pv);
	dlg.m_light           = m_ldr->Settings().m_Light;
	dlg.m_camera_relative = m_ldr->Settings().m_LightIsCameraRelative;
	if (dlg.DoModal(m_hWnd) != IDOK) return S_OK;
	m_ldr->Settings().m_Light                 = dlg.m_light;
	m_ldr->Settings().m_LightIsCameraRelative = dlg.m_camera_relative;
	m_refresh = true;
	return S_OK;
}

// Display a tool dialog
LRESULT LineDrawerGUI::OnShowToolDlg(WORD, WORD wID, HWND, BOOL&)
{
	switch (wID)
	{
	case ID_TOOLS_MEASURE: m_ldr->m_measure_tool_ui.Show(m_ldr->m_measure_tool_ui.IsWindowVisible() == FALSE); break;
	case ID_TOOLS_ANGLE:   m_ldr->m_angle_tool_ui  .Show(m_ldr->m_angle_tool_ui  .IsWindowVisible() == FALSE); break;
	}
	UpdateUI();
	return S_OK;
}

// Set the window draw order so that the line drawer window is always on top
LRESULT LineDrawerGUI::OnWindowAlwaysOnTop(WORD, WORD, HWND, BOOL&)
{
	m_ldr->Settings().m_AlwaysOnTop = !m_ldr->Settings().m_AlwaysOnTop;
	SetWindowPos(m_ldr->Settings().m_AlwaysOnTop ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE|SWP_NOSIZE);
	UpdateUI();
	return S_OK;
}

// Set the background colour
LRESULT LineDrawerGUI::OnWindowBackgroundColour(WORD, WORD, HWND, BOOL&)
{
	CColorDialog dlg(m_ldr->Settings().m_BackgroundColour.GetColorRef(), 0, m_hWnd);
	if (dlg.DoModal() != IDOK) return S_OK;
	m_ldr->Settings().m_BackgroundColour = dlg.GetColor() & 0x00FFFFFF;
	m_refresh = true;
	return S_OK;
}

// Show a window containing the demo scene script
LRESULT LineDrawerGUI::OnWindowExampleScript(WORD, WORD, HWND, BOOL&)
{
	m_ldr->m_store_ui.ShowScript(pr::ldr::CreateDemoScene(), m_hWnd);
	return S_OK;
}

// Check the web for the latest version
LRESULT LineDrawerGUI::OnCheckForUpdates(WORD, WORD, HWND, BOOL&)
{
	std::string version;
	pr::network::WebGet("http://www.rylogic.co.nz/latest_versions.html", version);

	pr::xml::Node root;
	HRESULT hr = pr::xml::Load(version.c_str(), version.size(), root);
	if (FAILED(hr))
	{
		MessageBoxA("Version information invalid", "Check For Updates", MB_OK|MB_ICONERROR);
		return S_OK;
	}

	return S_OK;
}

// Show the about box
LRESULT LineDrawerGUI::OnWindowShowAboutBox(WORD, WORD, HWND, BOOL&)
{
	ShowAbout();
	return S_OK;
}

// Handle key presses
LRESULT LineDrawerGUI::OnKeyDown(UINT, WPARAM wParam, LPARAM, BOOL& handled)
{
	switch (wParam)
	{
	default:
		handled = FALSE;
		break;
	case VK_SPACE:
		m_ldr->m_store_ui.Show(true);
		break;
	case VK_F5:
		m_ldr->ReloadSourceData();
		m_refresh = true;
		break;
	case VK_F7:
		m_ldr->ResetView(pr::ldr::EObjectBounds::All);
		m_refresh = true;
		break;
	}
	return S_OK;
}

// Convert a windows message wParam into a pr::camera::ENavBtn mask
inline int ButtonState(WPARAM wParam)
{
	int button_state = 0;
	if (wParam & MK_LBUTTON)  button_state |= pr::camera::ENavBtn::Left;
	if (wParam & MK_RBUTTON)  button_state |= pr::camera::ENavBtn::Right;
	if (wParam & MK_MBUTTON)  button_state |= pr::camera::ENavBtn::Middle;
	if (wParam & MK_SHIFT)    button_state |= pr::camera::ENavBtn::Shift;
	if (wParam & MK_CONTROL)  button_state |= pr::camera::ENavBtn::Ctrl;
	if (wParam & MK_XBUTTON1) button_state |= pr::camera::ENavBtn::XButton1;
	if (wParam & MK_XBUTTON2) button_state |= pr::camera::ENavBtn::XButton2;
	return button_state;
}

// Convert a windows message lParam into a screen space position
inline pr::v2 MouseLocation(LPARAM lParam)
{
	return pr::v2::make(float(GET_X_LPARAM(lParam)), float(GET_Y_LPARAM(lParam)));
}

// Convert a windows message wParam into a mouse wheel delta
// Returns '1.0f' for a single wheel click
inline float WheelDelta(WPARAM wParam)
{
	return GET_WHEEL_DELTA_WPARAM(wParam) / 120.0f;
}

// Mouse button down or up
LRESULT LineDrawerGUI::OnMouseButton(UINT, WPARAM wParam, LPARAM lParam, BOOL&)
{
	pr::v2 mouse_loc = MouseLocation(lParam);
	int    btn_state = ButtonState(wParam);
	if (btn_state != 0) SetCapture();
	else                ReleaseCapture();
	if (m_ldr->m_nav.MouseInput(mouse_loc, btn_state, true))
		m_ldr->Render();

	MouseStatusUpdate(mouse_loc);
	return S_OK;
}

// Mouse move events to the UI input
LRESULT LineDrawerGUI::OnMouseMove(UINT, WPARAM wParam, LPARAM lParam, BOOL&)
{
	pr::v2 mouse_loc = MouseLocation(lParam);
	int    btn_state = ButtonState(wParam);
	if (m_ldr->m_nav.MouseInput(mouse_loc, btn_state, false))
		m_ldr->Render();

	MouseStatusUpdate(mouse_loc);
	return S_OK;
}

// Mouse wheel scroll
LRESULT LineDrawerGUI::OnMouseWheel(UINT, WPARAM wParam, LPARAM lParam, BOOL&)
{
	pr::v2 mouse_loc = MouseLocation(lParam);
	if (m_ldr->m_nav.MouseWheel(mouse_loc, WheelDelta(wParam)))
		m_ldr->Render();

	MouseStatusUpdate(mouse_loc);
	return S_OK;
}

// Mouse button double click
LRESULT LineDrawerGUI::OnMouseDblClick(UINT, WPARAM wParam, LPARAM lParam, BOOL&)
{
	pr::v2 mouse_loc = MouseLocation(lParam);
	int    btn_state = ButtonState(wParam);
	if (m_ldr->m_nav.MouseDblClick(mouse_loc, btn_state))
		m_ldr->Render();

	MouseStatusUpdate(mouse_loc);
	return S_OK;
}

// Shut the app down
void LineDrawerGUI::CloseApp(int exit_code)
{
	DestroyWindow();
	PostQuitMessage(exit_code);
}

// Complete a resize operation
void LineDrawerGUI::Resize()
{
	// Update the new client area and refresh
	if (m_ldr) m_ldr->SetViewArea(pr::ClientArea(m_hWnd));
	m_refresh = true;
}

// Create a new file
void LineDrawerGUI::FileNew(char const* filepath)
{
	CloseHandle(pr::FileOpen(filepath, pr::EFileOpen::Writing));

	FileOpen(filepath, false);
	StrList list; list.push_back(filepath);
	OpenTextEditor(list);
}

// Add a file to the file sources
void LineDrawerGUI::FileOpen(char const* filepath, bool additive)
{
	// Add the file to the recent files list
	m_recent_files.Add(filepath, true);

	// Clear data from other files, unless this is an additive open
	if (!additive) m_ldr->m_files.Clear();
	m_ldr->m_files.Add(filepath);

	// Reset the camera if flagged
	if (m_ldr->Settings().m_ResetCameraOnLoad)
		m_ldr->ResetView(pr::ldr::EObjectBounds::All);

	// Set the window title
	pr::string<> title;
	title += ldr::AppTitle();
	title += " - ";
	title += filepath;
	SetWindowTextA(title.c_str());

	// Refresh
	m_refresh = true;
}

// Open the text editor with the provided file list
void LineDrawerGUI::OpenTextEditor(StrList const& files)
{
	// If no path to a text editor is provided, ignore the command
	std::string cmd = m_ldr->Settings().m_TextEditorCmd;
	if (cmd.empty())
	{
		MessageBoxA("Text editor not provided. Check options", "Editor startup error", MB_OK);
		return;
	}

	// Build the command line string
	for (StrList::const_iterator i = files.begin(), iend = files.end(); i != iend; ++i)
		cmd += " \"" + *i + "\"";

	STARTUPINFO suinfo = {sizeof(STARTUPINFO)};
	PROCESS_INFORMATION proc_info;
	if (CreateProcessA(0, &cmd[0], 0, 0, FALSE, NORMAL_PRIORITY_CLASS, 0, 0, &suinfo, &proc_info) == FALSE)
		MessageBoxA(pr::FmtS("Failed to start text editor: '%s'", cmd.c_str()), "Editor startup error", MB_OK);

	CloseHandle(proc_info.hThread);
	CloseHandle(proc_info.hProcess);
}

// Set UI elements to reflect their current state
void LineDrawerGUI::UpdateUI()
{
	// Camera orbit
	CheckMenuItem(GetMenu(), ID_NAVIGATION_ORBIT ,m_ldr->Settings().m_CameraOrbit ? MF_CHECKED : MF_UNCHECKED);

	// Auto refresh
	CheckMenuItem(GetMenu(), ID_DATA_AUTOREFRESH ,m_ldr->Settings().m_WatchForChangedFiles ? MF_CHECKED : MF_UNCHECKED);

	// Stock models
	CheckMenuItem(GetMenu(), ID_RENDERING_SHOWFOCUS        ,m_ldr->Settings().m_ShowFocusPoint   ? MF_CHECKED : MF_UNCHECKED);
	CheckMenuItem(GetMenu(), ID_RENDERING_SHOWORIGIN       ,m_ldr->Settings().m_ShowOrigin        ? MF_CHECKED : MF_UNCHECKED);
	CheckMenuItem(GetMenu(), ID_RENDERING_SHOWSELECTION    ,m_ldr->Settings().m_ShowSelectionBox ? MF_CHECKED : MF_UNCHECKED);
	CheckMenuItem(GetMenu(), ID_RENDERING_SHOWOBJECTBBOXES ,m_ldr->Settings().m_ShowObjectBBoxes ? MF_CHECKED : MF_UNCHECKED);

	// Set the text to the "next" mode
	switch (m_ldr->Settings().m_GlobalRenderMode)
	{
	case 0: ModifyMenu(GetMenu(), ID_RENDERING_WIREFRAME, MF_BYCOMMAND, ID_RENDERING_WIREFRAME, "&Wireframe\tCtrl+W");  break;
	case 1: ModifyMenu(GetMenu(), ID_RENDERING_WIREFRAME, MF_BYCOMMAND, ID_RENDERING_WIREFRAME, "&Wire + Solid\tCtrl+W"); break;
	case 2: ModifyMenu(GetMenu(), ID_RENDERING_WIREFRAME, MF_BYCOMMAND, ID_RENDERING_WIREFRAME, "&Solid\tCtrl+W");      break;
	}

	// Align axis checked items
	pr::v4 cam_align = m_ldr->Settings().m_CameraAlignAxis;
	CheckMenuItem(GetMenu() ,ID_NAV_ALIGN_NONE    ,cam_align == pr::v4Zero  ? MF_CHECKED : MF_UNCHECKED);
	CheckMenuItem(GetMenu() ,ID_NAV_ALIGN_X       ,cam_align == pr::v4XAxis ? MF_CHECKED : MF_UNCHECKED);
	CheckMenuItem(GetMenu() ,ID_NAV_ALIGN_Y       ,cam_align == pr::v4YAxis ? MF_CHECKED : MF_UNCHECKED);
	CheckMenuItem(GetMenu() ,ID_NAV_ALIGN_Z       ,cam_align == pr::v4ZAxis ? MF_CHECKED : MF_UNCHECKED);
	CheckMenuItem(GetMenu() ,ID_NAV_ALIGN_CURRENT ,cam_align != pr::v4Zero && cam_align != pr::v4XAxis &&  cam_align != pr::v4YAxis && cam_align != pr::v4ZAxis ? MF_CHECKED : MF_UNCHECKED);

	// Render 2d menu item
	ModifyMenu(GetMenu(), ID_RENDERING_RENDER2D, MF_BYCOMMAND, ID_RENDERING_RENDER2D, m_ldr->m_nav.Render2D() ? "&Perspective" : "&Orthographic");

	// The tools windows
	CheckMenuItem(GetMenu(), ID_TOOLS_MEASURE ,m_ldr->m_measure_tool_ui.IsWindowVisible() ? MF_CHECKED : MF_UNCHECKED);
	CheckMenuItem(GetMenu(), ID_TOOLS_ANGLE   ,m_ldr->m_angle_tool_ui  .IsWindowVisible() ? MF_CHECKED : MF_UNCHECKED);

	// Topmost window
	CheckMenuItem(GetMenu(), ID_WINDOW_ALWAYSONTOP, m_ldr->Settings().m_AlwaysOnTop ? MF_CHECKED : MF_UNCHECKED);
}

// Update the status text
void LineDrawerGUI::MouseStatusUpdate(pr::v2 const& mouse_location)
{
	if (!m_mouse_status_updates) return;
	ldr::string status;
	{
		// Display mouse coordinates
		pr::v4 mouse_ss = pr::v4::make(mouse_location, m_ldr->m_nav.FocusDistance(), 0.0f);
		pr::v4 mouse_ws = m_ldr->m_nav.WSPointFromScreenPoint(mouse_ss);
		pr::v4 focus_ws = m_ldr->m_nav.FocusPoint();
		status += pr::FmtS("Mouse: {%3.3f %3.3f %3.3f} Focus: {%3.3f %3.3f %3.3f}"
			,mouse_ws.x ,mouse_ws.y ,mouse_ws.z
			,focus_ws.x ,focus_ws.y ,focus_ws.z);
	}
	{
		// Display zoom
		float zoom = m_ldr->m_nav.Zoom();
		if (!pr::FEql(zoom, 1.0f, 0.001f))
			status += pr::FmtS(" Zoom: %3.3f", zoom);
	}
	pr::events::Send(ldr::Event_Status(status.c_str()));
}

// Display the about dialog box
void LineDrawerGUI::ShowAbout() const
{
	CAboutLineDrawer dlg;
	dlg.DoModal(m_hWnd);
}

// Handle info events
void LineDrawerGUI::OnEvent(ldr::Event_Info const& e)
{
	(void)e;
	PR_INFO(PR_DBG_LDR, e.m_msg.c_str());
}

// Handle warning events
void LineDrawerGUI::OnEvent(ldr::Event_Warn const& e)
{
	(void)e;
	PR_INFO(PR_DBG_LDR, e.m_msg.c_str());
}

// Handle error events
void LineDrawerGUI::OnEvent(ldr::Event_Error const& e)
{
	if (!m_ldr || m_ldr->Settings().m_ErrorOutputMsgBox)
		::MessageBoxA(m_hWnd, e.m_msg.c_str(), "Linedrawer Error", MB_OK|MB_ICONERROR);
	else
	{} // error msg on status line
}

// Status text update
void LineDrawerGUI::OnEvent(ldr::Event_Status const& e)
{
	DWORD now = GetTickCount();
	bool timed_out = now - m_status_pri.m_last_update > m_status_pri.m_min_display_time_ms;
	if (timed_out || e.m_priority > m_status_pri.m_priority)
	{
		m_status_pri.m_last_update = now;
		m_status_pri.m_priority = e.m_priority;
		m_status_pri.m_min_display_time_ms = e.m_min_display_time_ms;
		m_status.SetWindowText(e.m_msg.c_str());
		m_status.SetFont(e.m_bold ? m_status_pri.m_bold_font : m_status_pri.m_normal_font);
	}
}

// Handle refresh requests
void LineDrawerGUI::OnEvent(ldr::Event_Refresh const&)
{
	m_refresh = true;
}
void LineDrawerGUI::OnEvent(pr::ldr::Evt_Refresh const&)
{
	m_refresh = true;
}

// The measure tool window was closed
void LineDrawerGUI::OnEvent(pr::ldr::Evt_LdrMeasureCloseWindow const&)
{
	UpdateUI();
	m_refresh = true;
}

// The measurement info has updated
void LineDrawerGUI::OnEvent(pr::ldr::Evt_LdrMeasureUpdate const&)
{
	m_refresh = true;
}

// The angle tool window was closed
void LineDrawerGUI::OnEvent(pr::ldr::Evt_LdrAngleDlgCloseWindow const&)
{
	UpdateUI();
	m_refresh = true;
}

// The angle info has updated
void LineDrawerGUI::OnEvent(pr::ldr::Evt_LdrAngleDlgUpdate const&)
{
	m_refresh = true;
}

// A number of objects are about to be added
void LineDrawerGUI::OnEvent(pr::ldr::Evt_AddBegin const&)
{
	m_suspend_render = true;
}

// The last object in a group has been added
void LineDrawerGUI::OnEvent(pr::ldr::Evt_AddEnd const&)
{
	m_suspend_render = false;
	m_refresh = true;
}

// Occurs when an error happens during UserSetting parsing
void LineDrawerGUI::OnEvent(pr::settings::Evt<UserSettings> const& e)
{
	MessageBox(e.m_msg.c_str(), "Settings Error", MB_OK);
}

// Recent files onclick
void LineDrawerGUI::MenuList_OnClick(pr::gui::MenuList* sender, pr::gui::MenuList::Item const& item)
{
	if (static_cast<pr::gui::RecentFiles*>(sender) == &m_recent_files)
	{
		FileOpen(item.m_name.c_str(), pr::KeyDown(VK_SHIFT));
	}
	if (sender == &m_saved_views)
	{
		m_ldr->m_nav.RestoreView((NavManager::SavedViewID)item.m_tag);
		UpdateUI();
		m_refresh = true;
	}
}

// When the recent files list changes, save the settings
void LineDrawerGUI::MenuList_ListChanged(pr::gui::MenuList* sender)
{
	if (static_cast<pr::gui::RecentFiles*>(sender) == &m_recent_files)
	{
		m_ldr->Settings().m_RecentFiles = m_recent_files.Export();
	}
	if (sender == &m_saved_views)
	{
	}
}
