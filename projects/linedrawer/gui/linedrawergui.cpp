//*****************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
#include "linedrawer/main/stdafx.h"
#include "linedrawer/gui/linedrawergui.h"
#include "linedrawer/gui/options_dlg.h"
#include "linedrawer/gui/about_dlg.h"
#include "linedrawer/gui/text_panel_dlg.h"
#include "linedrawer/resources/linedrawer.res.h"
#include "linedrawer/main/ldrexception.h"
#include "linedrawer/plugin/plugin_manager_dlg.h"
#include "linedrawer/utility/misc.h"
#include "linedrawer/utility/debug.h"

// Create the GUI window
std::shared_ptr<ATL::CWindow> pr::app::CreateGUI(LPTSTR lpstrCmdLine)
{
	return pr::app::CreateGUI<::ldr::MainGUI>(lpstrCmdLine);
}

namespace ldr
{
	TCHAR FileOpenFilter[] = TEXT("Ldr Script (*.ldr)\0*.ldr\0Lua Script (*.lua)\0*.lua\0DirectX Files (*.x)\0*.x\0All Files (*.*)\0*.*\0\0");

	// Callback function for reading a point in world space
	// Used by the tool UIs to measure distances and angles
	pr::v4 __stdcall ReadPoint(void* ctx)
	{
		return static_cast<pr::Camera*>(ctx)->FocusPoint();
	}

	MainGUI::MainGUI(LPTSTR cmdline)
		:base()
		,m_menu()
		,m_status()
		,m_recent_files()
		,m_saved_views()
		,m_store_ui(m_hWnd)
		,m_measure_tool_ui(ReadPoint, &m_main->m_cam, m_main->m_rdr, m_hWnd)
		,m_angle_tool_ui(ReadPoint, &m_main->m_cam, m_main->m_rdr, m_hWnd)
		,m_mouse_status_updates(true)
		,m_suspend_render(false)
		,m_status_pri()
	{
		//// Ignore internal context ids
		//m_store_ui.IgnoreContextId(pr::ldr::LdrMeasurePrivateContextId, true);
		//m_store_ui.IgnoreContextId(pr::ldr::LdrAngleDlgPrivateContextId, true);

		// Parse the command line
		pr::EnumCommandLine(cmdline, *this);
	}

	// Init Window
	LRESULT MainGUI::OnCreate(LPCREATESTRUCT create)
	{
		pr::Throw(base::OnCreate(create));

		// Note, 'm_main' can be null if the SimMsgLoop runs these contexts after the
		// window has been destroyed, but before the message loop has been shutdown.
		// This shouldn't really happen and needs investigating

		// Create a step context for rendering
		enum { force_render = false };
		m_msg_loop.AddStepContext("rdr main loop", [this](double) { if (m_main) m_main->DoRender(force_render); }, 60.0f, false);

		// Add a step context for stepping plugins
		m_msg_loop.AddStepContext("plugin step", [this](double elapsed_s){ if (m_main) m_main->m_plugin_mgr.Poll(elapsed_s); }, 30.0f, true);

		// Set icons
		SetIcon((HICON)::LoadImage(create->hInstance, MAKEINTRESOURCE(IDI_ICON_MAIN), IMAGE_ICON, ::GetSystemMetrics(SM_CXICON),   ::GetSystemMetrics(SM_CYICON),   LR_DEFAULTCOLOR) ,TRUE);
		SetIcon((HICON)::LoadImage(create->hInstance, MAKEINTRESOURCE(IDI_ICON_MAIN), IMAGE_ICON, ::GetSystemMetrics(SM_CXSMICON), ::GetSystemMetrics(SM_CYSMICON), LR_DEFAULTCOLOR) ,FALSE);

		// Load menu
		HMENU hMenu = LoadMenu(create->hInstance, MAKEINTRESOURCE(IDR_MENU_MAIN));
		m_menu.Attach(hMenu);
		SetMenu(hMenu);

		// Load accelerators
		m_hAccel = (HACCEL)::LoadAccelerators(create->hInstance, MAKEINTRESOURCE(IDR_ACCELERATOR));
		m_hand_cursor.Attach((HCURSOR)LoadCursorA(create->hInstance, MAKEINTRESOURCE(IDC_HAND)));

		// Status bar
		CreateSimpleStatusBar(TEXT(""), WS_CHILD|WS_VISIBLE|CCS_BOTTOM|CCS_ADJUSTABLE|SBARS_SIZEGRIP);
		m_status.Attach(m_hWndStatusBar);
		int status_panes[] = {-1};
		m_status.SetParts(1, status_panes);

		// Initialise the menu lists
		m_recent_files.Attach(pr::gui::GetMenuByName(GetMenu(), TEXT("&File,&Recent Files"))      ,ID_FILE_RECENTFILES ,0xffffffff ,this);
		m_saved_views .Attach(pr::gui::GetMenuByName(GetMenu(), TEXT("&Navigation,&Saved Views")) ,ID_NAV_SAVEDVIEWS   ,0xffffffff ,this);

		// Initialise the object manager
		m_store_ui.Settings(m_main->m_settings.m_ObjectManagerSettings.c_str());

		// Initialise the recent files list and saved views
		m_recent_files.MaxLength(m_main->m_settings.m_MaxRecentFiles);
		m_saved_views .MaxLength(m_main->m_settings.m_MaxSavedViews);
		m_recent_files.Import(m_main->m_settings.m_RecentFiles);

		// Update the state of the UI
		UpdateUI();

		// Set the initial camera position
		m_main->ResetView(EObjectBounds::All);
		m_main->m_nav.CameraAlign(m_main->m_settings.m_CameraAlignAxis);

		// Register for drag drop
		DragAcceptFiles(m_hWnd, true);

		// Start the main timer
		OnTimer(0);
		return TRUE;
	}

	// Handler timer messages
	void MainGUI::OnTimer(UINT_PTR)
	{
		// If file watching is turned on, look for changed files
		if (m_main->m_settings.m_WatchForChangedFiles)
			m_main->m_files.RefreshChangedFiles();

		// Orbit the camera if enabled
		if (m_main->m_settings.m_CameraOrbit)
		{
			m_main->m_nav.OrbitCamera(m_main->m_settings.m_CameraOrbitSpeed);
			m_main->RenderNeeded();
		}

		SetTimer(ID_MAIN_TIMER, 1, 0);
	}

	// Paint the window
	void MainGUI::OnPaint(HDC hDC)
	{
		SetMsgHandled(FALSE);
		if (m_suspend_render) return;
		base::OnPaint(hDC);
	}

	// Set minimum window size
	void MainGUI::OnGetMinMaxInfo(LPMINMAXINFO lpMMI)
	{
		lpMMI->ptMinTrackSize.x = 320;
		lpMMI->ptMinTrackSize.x = 200;
	}

	// Get the cursor to show for drag-drop operations
	HCURSOR MainGUI::OnQueryDragIcon()
	{
		return m_hand_cursor;
	}

	// Handle files dropped onto the main window
	void MainGUI::OnDropFiles(HDROP hDropInfo)
	{
		UINT num_files = ::DragQueryFileA(hDropInfo, 0xFFFFFFFF, NULL, 0);
		if (num_files == 0)
			return;

		// Clear the data unless shift is held down
		if (!pr::KeyDown(VK_SHIFT))
			m_main->m_files.Clear();

		// Load the files
		std::string path;
		for (UINT i = 0; i != num_files; ++i)
		{
			path.resize(::DragQueryFileA(hDropInfo, i, 0, 0) + 1, 0);
			if (::DragQueryFile(hDropInfo, i, &path[0], UINT(path.size())) != 0)
				m_main->m_files.Add(path.c_str());
		}
	}

	// Handle system menu keys
	void MainGUI::OnSysKeyDown(UINT nChar, UINT, UINT)
	{
		// Watch for full screen alt-enter transitions
		if (nChar == VK_RETURN)
		{
			// If currently in full screen mode, switch to windowed
			if (m_main->m_window.FullScreenMode())
			{
				pr::rdr::DisplayMode mode;
				m_main->m_window.FullScreenMode(false, mode);

				// Show the status and menu controls again
				SetMenu(m_menu);
				m_status.ShowWindow(SW_SHOW);
			}
			else
			{
				// Hide the menu and status bar so that the the client area
				// is calculated correctly.
				SetMenu(0);
				m_status.ShowWindow(SW_HIDE);

				pr::rdr::SystemConfig config;
				pr::rdr::SystemConfig::ModeCont modes;
				config.m_adapters[0].m_outputs[0].GetDisplayModes(DXGI_FORMAT_R8G8B8A8_UNORM, modes);

				// Get the full screen display mode from the settings
				pr::rdr::DisplayMode mode(1920, 1080);
				m_main->m_window.FullScreenMode(true, mode);
			}
			m_main->RenderNeeded();
			m_main->DoRender();
		}
	}

	// Handle key presses
	void MainGUI::OnKeyDown(UINT nChar, UINT, UINT)
	{
		switch (nChar)
		{
		default:
			SetMsgHandled(FALSE);
			break;
		case VK_SPACE:
			m_store_ui.Show(true, m_main->m_store);
			break;
		case VK_F5:
			m_main->ReloadSourceData();
			m_main->RenderNeeded();
			break;
		case VK_F7:
			m_main->ResetView(EObjectBounds::All);
			m_main->RenderNeeded();
			break;
		}
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

	void MainGUI::OnMouseDown(UINT btn, UINT, CPoint point)
	{
		IsClick(btn, false);

		SetCapture();
		int btn_state = ButtonState(btn);
		pr::v2 mouse_loc = pr::To<pr::v2>(point);
		if (m_main->m_nav.MouseInput(mouse_loc, btn_state, true))
			m_main->RenderNeeded();

		MouseStatusUpdate(mouse_loc);
	}
	void MainGUI::OnMouseUp(UINT btn, UINT flags, CPoint point)
	{
		ReleaseCapture();
		pr::v2 mouse_loc = pr::To<pr::v2>(point);
		if (IsClick(btn, true))
		{
			OnMouseClick(btn, flags, point);
			m_main->RenderNeeded();
		}
		else
		{
			if (m_main->m_nav.MouseInput(mouse_loc, 0, true))
				m_main->RenderNeeded();
		}

		MouseStatusUpdate(mouse_loc);
	}
	void MainGUI::OnMouseMove(UINT flags, CPoint point)
	{
		int btn_state = ButtonState(flags);
		pr::v2 mouse_loc = pr::To<pr::v2>(point);
		if (m_main->m_nav.MouseInput(mouse_loc, btn_state, false))
			m_main->RenderNeeded();

		MouseStatusUpdate(mouse_loc);
	}
	void MainGUI::OnMouseClick(UINT btn, UINT, CPoint point)
	{
		int  btn_state = ButtonState(btn);
		pr::v2 mouse_loc = pr::To<pr::v2>(point);
		if (m_main->m_nav.MouseClick(mouse_loc, btn_state))
			m_main->RenderNeeded();

		MouseStatusUpdate(mouse_loc);
	}
	BOOL MainGUI::OnMouseWheel(UINT, short delta, CPoint point)
	{
		pr::v2 mouse_loc = pr::To<pr::v2>(point);

		// delta is '1.0f' for a single wheel click
		if (m_main->m_nav.MouseWheel(mouse_loc, delta/120.0f))
			m_main->RenderNeeded();

		MouseStatusUpdate(mouse_loc);
		return FALSE; // ie. we handled this wheel message
	}

	// Open a text panel for adding new ldr objects immediately
	LRESULT MainGUI::OnFileNew(WORD, WORD, HWND, BOOL&)
	{
		try
		{
			CTextEntryDlg dlg(m_hWnd, "Create new ldr objects:", m_main->m_settings.m_NewObjectString.c_str(), true);
			pr::IRect r  = pr::WindowBounds(m_hWnd);
			dlg.m_width  = pr::Max(100, r.SizeX() - 50);
			dlg.m_height = pr::Max(60,  r.SizeY() - 50);
			if (dlg.DoModal() != IDOK) return S_OK;

			m_main->m_settings.m_NewObjectString = dlg.m_body;
			m_main->m_settings.Save();

			pr::ldr::AddString(m_main->m_rdr, m_main->m_settings.m_NewObjectString.c_str(), nullptr, m_main->m_store);
			m_main->RenderNeeded();
		}
		catch (std::exception const& e)
		{
			pr::events::Send(Event_Error(pr::FmtS("Script error found while parsing source.\nError details: %s", e.what())));
		}
		return S_OK;
	}

	// Create a new text file for ldr script
	LRESULT MainGUI::OnFileNewScript(WORD, WORD, HWND, BOOL&)
	{
		try
		{
			WTL::CFileDialog fd(FALSE,0,0,0,FileOpenFilter,m_hWnd);
			if (fd.DoModal() != IDOK) return S_OK;
			FileNew(fd.m_szFileName);
		}
		catch (std::exception const& e)
		{
			pr::events::Send(Event_Error(pr::FmtS("Creating a new script failed.\nError details: %s", e.what())));
		}
		return S_OK;
	}

	// Open a line drawer script file
	LRESULT MainGUI::OnFileOpen(WORD, WORD, HWND, BOOL&)
	{
		WTL::CFileDialog fd(TRUE,0,0,0,FileOpenFilter,m_hWnd);
		if (fd.DoModal() != IDOK) return S_OK;
		FileOpen(fd.m_szFileName, false);
		return S_OK;
	}

	// Open a file and add it to the current scene
	LRESULT MainGUI::OnFileOpenAdditive(WORD, WORD, HWND, BOOL&)
	{
		CFileDialog fd(TRUE,0,0,0,FileOpenFilter,m_hWnd);
		if (fd.DoModal() != IDOK) return S_OK;
		FileOpen(fd.m_szFileName, true);
		return S_OK;
	}

	// Display the options dialog
	LRESULT MainGUI::OnShowOptions(WORD, WORD, HWND, BOOL&)
	{
		COptionsDlg dlg(m_main->m_settings, m_hWnd);
		if (dlg.DoModal() != IDOK) return S_OK;
		dlg.GetSettings(m_main->m_settings);
		m_main->RenderNeeded();
		return S_OK;
	}

	// Display the plugin manager dialog
	LRESULT MainGUI::OnShowPluginMgr(WORD, WORD, HWND, BOOL&)
	{
		PluginManagerDlg dlg(m_main->m_plugin_mgr, m_hWnd);
		if (dlg.DoModal() != IDOK) return S_OK;
		return S_OK;
	}

	// Close the dialog event
	LRESULT MainGUI::OnAppClose(WORD, WORD wID, HWND, BOOL&)
	{
		CloseApp(wID);
		return S_OK;
	}

	// Reset the view to all, selected, or visible objects
	LRESULT MainGUI::OnResetView(WORD, WORD wID, HWND, BOOL&)
	{
		switch (wID)
		{
		default:
		case ID_NAV_RESETVIEW_ALL:      m_main->ResetView(EObjectBounds::All); break;
		case ID_NAV_RESETVIEW_SELECTED: m_main->ResetView(EObjectBounds::Selected); break;
		case ID_NAV_RESETVIEW_VISIBLE:  m_main->ResetView(EObjectBounds::Visible); break;
		}
		m_main->RenderNeeded();
		return S_OK;
	}

	// View the current focus point looking down the selected axis
	LRESULT MainGUI::OnViewAxis(WORD, WORD wID, HWND, BOOL&)
	{
		pr::v4 axis;
		switch (wID)
		{
		default: axis = m_main->m_nav.CameraToWorld().z; break;
		case ID_NAV_VIEW_AXIS_POSX:   axis =  pr::v4XAxis; break;
		case ID_NAV_VIEW_AXIS_NEGX:   axis = -pr::v4XAxis; break;
		case ID_NAV_VIEW_AXIS_POSY:   axis =  pr::v4YAxis; break;
		case ID_NAV_VIEW_AXIS_NEGY:   axis = -pr::v4YAxis; break;
		case ID_NAV_VIEW_AXIS_POSZ:   axis =  pr::v4ZAxis; break;
		case ID_NAV_VIEW_AXIS_NEGZ:   axis = -pr::v4ZAxis; break;
		case ID_NAV_VIEW_AXIS_POSXYZ: axis = -pr::v4::make(0.577350f, 0.577350f, 0.577350f, 0.0f); break;
		}

		pr::m4x4 c2w = m_main->m_nav.CameraToWorld();
		pr::v4 focus = m_main->m_nav.FocusPoint();
		pr::v4 cam = focus + axis * m_main->m_nav.FocusDistance();
		pr::v4 up = pr::Parallel(axis, c2w.y) ? pr::Cross3(axis, c2w.x) : c2w.y;
		m_main->m_nav.LookAt(cam, focus, up);
		m_main->RenderNeeded();
		return S_OK;
	}

	// Set the position of the camera focus point in world space
	LRESULT MainGUI::OnSetFocusPosition(WORD, WORD, HWND, BOOL&)
	{
		CTextEntryDlg dlg(m_hWnd, "Entry focus point position", "0 0 0", false);
		if (dlg.DoModal() != IDOK) return S_OK;

		float pos[3];
		if (pr::str::ExtractRealArrayC(&pos[0], 3, dlg.m_body.c_str()))
			m_main->m_nav.FocusPoint(pr::v4::make(pos, 1.0f));
		else
			MessageBoxA("Format incorrect", "Focus point not set", MB_OK|MB_ICONERROR);

		m_main->RenderNeeded();
		return S_OK;
	}

	// Set the position of the camera
	LRESULT MainGUI::OnSetCameraPosition(WORD, WORD, HWND, BOOL&)
	{
		pr::camera::PositionDlg dlg;
		dlg.m_cam = m_main->m_cam;
		if (dlg.DoModal(m_hWnd) != IDOK) return S_OK;
		m_main->m_cam = dlg.m_cam;
		m_main->RenderNeeded();
		return S_OK;
	}

	// Align the camera to the selected axis
	LRESULT MainGUI::OnNavAlign(WORD, WORD wID, HWND, BOOL&)
	{
		switch (wID)
		{
		default:
		case ID_NAV_ALIGN_NONE:    m_main->m_nav.CameraAlign(pr::v4Zero); break;
		case ID_NAV_ALIGN_X:       m_main->m_nav.CameraAlign(pr::v4XAxis); break;
		case ID_NAV_ALIGN_Y:       m_main->m_nav.CameraAlign(pr::v4YAxis); break;
		case ID_NAV_ALIGN_Z:       m_main->m_nav.CameraAlign(pr::v4ZAxis); break;
		case ID_NAV_ALIGN_CURRENT: m_main->m_nav.CameraAlign(m_main->m_nav.CameraToWorld().y); break;
		}
		m_main->m_settings.m_CameraAlignAxis = m_main->m_nav.CameraAlign();
		UpdateUI();
		m_main->RenderNeeded();
		return S_OK;
	}

	// Record the current camera position as a saved camera view
	LRESULT MainGUI::OnSaveView(WORD, WORD wID, HWND, BOOL&)
	{
		if (wID == ID_NAV_CLEARSAVEDVIEWS)
		{
			m_main->m_nav.ClearSavedViews();
			m_saved_views.Clear();
		}
		else
		{
			CTextEntryDlg dlg(m_hWnd, "Label for this view", pr::FmtS("view%d", m_saved_views.Items().size()), false);
			if (dlg.DoModal() != IDOK) return S_OK;

			NavManager::SavedViewID id = m_main->m_nav.SaveView();
			m_saved_views.Add(dlg.m_body.c_str(), (void*)id, false, true);
		}
		return S_OK;
	}

	// Toggle camera orbit mode
	LRESULT MainGUI::OnOrbit(WORD, WORD, HWND, BOOL&)
	{
		m_main->m_settings.m_CameraOrbit = !m_main->m_settings.m_CameraOrbit;
		m_main->m_nav.OrbitCamera(0.0f);
		UpdateUI();
		return S_OK;
	}

	// Display the object manager UI
	LRESULT MainGUI::OnShowObjectManagerUI(WORD, WORD, HWND, BOOL&)
	{
		m_store_ui.Show(true, m_main->m_store);
		return S_OK;
	}

	// Spawn the text editor with the source files
	LRESULT MainGUI::OnEditSourceFiles(WORD, WORD, HWND, BOOL&)
	{
		OpenTextEditor(m_main->m_files.List());
		return S_OK;
	}

	// Remove all objects from the object manager
	LRESULT MainGUI::OnDataClearScene(WORD, WORD, HWND, BOOL&)
	{
		m_main->m_store.clear();
		m_main->RenderNeeded();
		return S_OK;
	}

	// Toggle auto refresh file sources
	LRESULT MainGUI::OnDataAutoRefresh(WORD, WORD, HWND, BOOL&)
	{
		m_main->m_settings.m_WatchForChangedFiles = !m_main->m_settings.m_WatchForChangedFiles;
		UpdateUI();
		return S_OK;
	}

	// Generate a self created scene of objects
	LRESULT MainGUI::OnCreateDemoScene(WORD, WORD, HWND, BOOL&)
	{
		m_main->CreateDemoScene();
		m_main->ResetView(EObjectBounds::All);
		m_main->RenderNeeded();
		return S_OK;
	}

	// Toggle visibility of the focus point
	LRESULT MainGUI::OnShowFocus(WORD, WORD, HWND, BOOL&)
	{
		m_main->m_settings.m_ShowFocusPoint = !m_main->m_settings.m_ShowFocusPoint;
		UpdateUI();
		m_main->RenderNeeded();
		return S_OK;
	}

	// Toggle visibility of the origin point
	LRESULT MainGUI::OnShowOrigin(WORD, WORD, HWND, BOOL&)
	{
		m_main->m_settings.m_ShowOrigin = !m_main->m_settings.m_ShowOrigin;
		UpdateUI();
		m_main->RenderNeeded();
		return S_OK;
	}

	// Toggle visibility of the selection box
	LRESULT MainGUI::OnShowSelection(WORD, WORD, HWND, BOOL&)
	{
		m_main->m_settings.m_ShowSelectionBox = !m_main->m_settings.m_ShowSelectionBox;
		UpdateUI();
		m_main->RenderNeeded();
		return S_OK;
	}

	// Toggle visibility of the object space bounding boxes
	LRESULT MainGUI::OnShowObjBBoxes(WORD, WORD, HWND, BOOL&)
	{
		m_main->m_settings.m_ShowObjectBBoxes = !m_main->m_settings.m_ShowObjectBBoxes;
		UpdateUI();
		m_main->RenderNeeded();
		return S_OK;
	}

	// Cycle through solid, wireframe, and solid+wire
	LRESULT MainGUI::OnToggleFillMode(WORD, WORD, HWND, BOOL&)
	{
		int mode = (m_main->m_settings.m_GlobalFillMode + 1) % EFillMode::NumberOf;
		m_main->m_settings.m_GlobalFillMode = static_cast<EFillMode>(mode);
		UpdateUI();
		m_main->RenderNeeded();
		return S_OK;
	}

	// Toggle between perspective and orthographic
	LRESULT MainGUI::OnRender2D(WORD, WORD, HWND, BOOL&)
	{
		m_main->m_nav.Render2D(!m_main->m_nav.Render2D());
		UpdateUI();
		m_main->RenderNeeded();
		return S_OK;
	}

	// Toggle between forward and deferred rendering
	LRESULT MainGUI::OnRenderTechnique(WORD, WORD, HWND, BOOL&)
	{
		auto fwd = {pr::rdr::ERenderStep::ForwardRender};
		auto def = {pr::rdr::ERenderStep::GBuffer, pr::rdr::ERenderStep::DSLighting};
		
		if (m_main->m_scene.FindRStep<pr::rdr::ForwardRender>() != nullptr)
			m_main->m_scene.SetRenderSteps(pr::rdr::Scene::DeferredRendering());
		else
			m_main->m_scene.SetRenderSteps(pr::rdr::Scene::ForwardRendering());

		UpdateUI();
		m_main->RenderNeeded();
		return S_OK;
	}

	// Display the lighting dialog
	LRESULT MainGUI::OnShowLightingDlg(WORD, WORD, HWND, BOOL&)
	{
		struct PreviewLighting
		{
			Main* m_main;
			PreviewLighting(Main* main) :m_main(main) {}
			void operator()(pr::rdr::Light const& light, bool camera_relative)
			{
				pr::rdr::Light prev_light   = m_main->m_settings.m_Light;
				bool           prev_cam_rel = m_main->m_settings.m_LightIsCameraRelative;
				m_main->m_settings.m_Light                    = light;
				m_main->m_settings.m_LightIsCameraRelative = camera_relative;
				m_main->RenderNeeded();
				m_main->m_settings.m_Light                 = prev_light;
				m_main->m_settings.m_LightIsCameraRelative = prev_cam_rel;
			}
		};

		PreviewLighting pv(m_main.get());
		pr::rdr::LightingDlg<PreviewLighting> dlg(pv);
		dlg.m_light           = m_main->m_settings.m_Light;
		dlg.m_camera_relative = m_main->m_settings.m_LightIsCameraRelative;
		if (dlg.DoModal(m_hWnd) != IDOK) return S_OK;
		m_main->m_settings.m_Light                 = dlg.m_light;
		m_main->m_settings.m_LightIsCameraRelative = dlg.m_camera_relative;
		m_main->RenderNeeded();
		return S_OK;
	}

	// Display a tool dialog
	LRESULT MainGUI::OnShowToolDlg(WORD, WORD wID, HWND, BOOL&)
	{
		switch (wID)
		{
		case ID_TOOLS_MEASURE: m_measure_tool_ui.Show(m_measure_tool_ui.IsWindowVisible() == FALSE); break;
		case ID_TOOLS_ANGLE:   m_angle_tool_ui  .Show(m_angle_tool_ui  .IsWindowVisible() == FALSE); break;
		}
		UpdateUI();
		return S_OK;
	}

	// Set the window draw order so that the line drawer window is always on top
	LRESULT MainGUI::OnWindowAlwaysOnTop(WORD, WORD, HWND, BOOL&)
	{
		m_main->m_settings.m_AlwaysOnTop = !m_main->m_settings.m_AlwaysOnTop;
		SetWindowPos(m_main->m_settings.m_AlwaysOnTop ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE|SWP_NOSIZE);
		UpdateUI();
		return S_OK;
	}

	// Set the background colour
	LRESULT MainGUI::OnWindowBackgroundColour(WORD, WORD, HWND, BOOL&)
	{
		CColorDialog dlg(m_main->m_settings.m_BackgroundColour.GetColorRef(), 0, m_hWnd);
		if (dlg.DoModal() != IDOK) return S_OK;
		m_main->m_settings.m_BackgroundColour = dlg.GetColor() & 0x00FFFFFF;
		m_main->RenderNeeded();
		return S_OK;
	}

	// Show a window containing the demo scene script
	LRESULT MainGUI::OnWindowExampleScript(WORD, WORD, HWND, BOOL&)
	{
		m_store_ui.ShowScript(pr::ldr::CreateDemoScene(), m_hWnd);
		return S_OK;
	}

	// Check the web for the latest version
	LRESULT MainGUI::OnCheckForUpdates(WORD, WORD, HWND, BOOL&)
	{
		std::string version;
		pr::network::WebGet("http://www.rylogic.co.nz/latest_versions.html", version);

		pr::xml::Node root;
		try { pr::xml::Load(version.c_str(), version.size(), root); }
		catch (std::exception const&)
		{
			MessageBoxA("Version information invalid", "Check For Updates", MB_OK|MB_ICONERROR);
			return S_OK;
		}

		return S_OK;
	}

	// Show the about box
	LRESULT MainGUI::OnWindowShowAboutBox(WORD, WORD, HWND, BOOL&)
	{
		ShowAbout();
		return S_OK;
	}

	// Shut the app down
	void MainGUI::CloseApp(int exit_code)
	{
		m_angle_tool_ui.Close();
		m_measure_tool_ui.Close();
		m_store_ui.Close();
		base::CloseApp(exit_code);
	}

	// Create a new file
	void MainGUI::FileNew(char const* filepath)
	{
		try
		{
			CloseHandle(pr::FileOpen(filepath, pr::EFileOpen::Writing));

			FileOpen(filepath, false);
			StrList list; list.push_back(filepath);
			OpenTextEditor(list);
		}
		catch (std::exception const& e)
		{
			pr::events::Send(Event_Error(pr::FmtS("Error opening new script.\nError details: %s", e.what())));
		}
	}

	// Add a file to the file sources
	void MainGUI::FileOpen(char const* filepath, bool additive)
	{
		try
		{
			// Add the file to the recent files list
			m_recent_files.Add(filepath, true);

			// Clear data from other files, unless this is an additive open
			if (!additive) m_main->m_files.Clear();
			m_main->m_files.Add(filepath);

			// Reset the camera if flagged
			if (m_main->m_settings.m_ResetCameraOnLoad)
				m_main->ResetView(EObjectBounds::All);

			// Set the window title
			pr::string<> title;
			title += AppTitleA();
			title += " - ";
			title += filepath;
			SetWindowTextA(title.c_str());

			// Refresh
			m_main->RenderNeeded();
		}
		catch (std::exception const& ex)
		{
			pr::events::Send(Event_Error(pr::FmtS("Error when attempting to open file '%s'.\r\nDetails: %s", filepath, ex.what())));
		}
	}

	// Open the text editor with the provided file list
	void MainGUI::OpenTextEditor(StrList const& files)
	{
		try
		{
			// If no path to a text editor is provided, ignore the command
			std::string cmd = m_main->m_settings.m_TextEditorCmd;
			if (cmd.empty())
				throw std::exception("Text editor not provided. Check options");

			// Build the command line string
			for (auto& file : files)
				cmd += " \"" + file + "\"";

			// Launch the text editor in a new process
			STARTUPINFO suinfo = {sizeof(STARTUPINFO)};
			PROCESS_INFORMATION proc_info;
			auto close_handles = pr::CreateScope([]{}, [&]{ CloseHandle(proc_info.hThread); CloseHandle(proc_info.hProcess); });
			if (CreateProcessA(0, &cmd[0], 0, 0, FALSE, NORMAL_PRIORITY_CLASS, 0, 0, &suinfo, &proc_info) == FALSE)
				throw std::exception(pr::FmtS("Failed to start text editor: '%s'", cmd.c_str()));
		}
		catch (std::exception const& e)
		{
			pr::events::Send(Event_Error(pr::FmtS("OpenTextEditor failed.\r\nError details: %s", e.what())));
		}
	}

	// Set UI elements to reflect their current state
	void MainGUI::UpdateUI()
	{
		// Camera orbit
		CheckMenuItem(GetMenu(), ID_NAV_ORBIT ,m_main->m_settings.m_CameraOrbit ? MF_CHECKED : MF_UNCHECKED);

		// Auto refresh
		CheckMenuItem(GetMenu(), ID_DATA_AUTOREFRESH ,m_main->m_settings.m_WatchForChangedFiles ? MF_CHECKED : MF_UNCHECKED);

		// Stock models
		CheckMenuItem(GetMenu(), ID_RENDERING_SHOWFOCUS        ,m_main->m_settings.m_ShowFocusPoint   ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(GetMenu(), ID_RENDERING_SHOWORIGIN       ,m_main->m_settings.m_ShowOrigin        ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(GetMenu(), ID_RENDERING_SHOWSELECTION    ,m_main->m_settings.m_ShowSelectionBox ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(GetMenu(), ID_RENDERING_SHOWOBJECTBBOXES ,m_main->m_settings.m_ShowObjectBBoxes ? MF_CHECKED : MF_UNCHECKED);

		// Set the text to the "next" mode
		switch (m_main->m_settings.m_GlobalFillMode)
		{
		case 0: ModifyMenu(GetMenu(), ID_RENDERING_WIREFRAME, MF_BYCOMMAND, ID_RENDERING_WIREFRAME, "&Wireframe\tCtrl+W");  break;
		case 1: ModifyMenu(GetMenu(), ID_RENDERING_WIREFRAME, MF_BYCOMMAND, ID_RENDERING_WIREFRAME, "&Wire + Solid\tCtrl+W"); break;
		case 2: ModifyMenu(GetMenu(), ID_RENDERING_WIREFRAME, MF_BYCOMMAND, ID_RENDERING_WIREFRAME, "&Solid\tCtrl+W");      break;
		}

		// Align axis checked items
		pr::v4 cam_align = m_main->m_settings.m_CameraAlignAxis;
		CheckMenuItem(GetMenu() ,ID_NAV_ALIGN_NONE    ,cam_align == pr::v4Zero  ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(GetMenu() ,ID_NAV_ALIGN_X       ,cam_align == pr::v4XAxis ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(GetMenu() ,ID_NAV_ALIGN_Y       ,cam_align == pr::v4YAxis ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(GetMenu() ,ID_NAV_ALIGN_Z       ,cam_align == pr::v4ZAxis ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(GetMenu() ,ID_NAV_ALIGN_CURRENT ,cam_align != pr::v4Zero && cam_align != pr::v4XAxis &&  cam_align != pr::v4YAxis && cam_align != pr::v4ZAxis ? MF_CHECKED : MF_UNCHECKED);

		// Render 2d menu item
		ModifyMenu(GetMenu(), ID_RENDERING_RENDER2D, MF_BYCOMMAND, ID_RENDERING_RENDER2D, m_main->m_nav.Render2D() ? "&Perspective" : "&Orthographic");
		ModifyMenu(GetMenu(), ID_RENDERING_TECHNIQUE, MF_BYCOMMAND, ID_RENDERING_TECHNIQUE, m_main->m_scene.FindRStep<pr::rdr::ForwardRender>() ? "&Deferred Rendering" : "&Forward Rendering");

		// The tools windows
		CheckMenuItem(GetMenu(), ID_TOOLS_MEASURE ,m_measure_tool_ui.IsWindowVisible() ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(GetMenu(), ID_TOOLS_ANGLE   ,m_angle_tool_ui  .IsWindowVisible() ? MF_CHECKED : MF_UNCHECKED);

		// Topmost window
		CheckMenuItem(GetMenu(), ID_WINDOW_ALWAYSONTOP, m_main->m_settings.m_AlwaysOnTop ? MF_CHECKED : MF_UNCHECKED);
	}

	// Update the status text
	void MainGUI::MouseStatusUpdate(pr::v2 const& mouse_location)
	{
		if (!m_mouse_status_updates) return;
		string status;
		{
			// Display mouse coordinates
			pr::v4 mouse_ss = pr::v4::make(mouse_location, m_main->m_nav.FocusDistance(), 0.0f);
			pr::v4 mouse_ws = m_main->m_nav.WSPointFromSSPoint(mouse_ss);
			pr::v4 focus_ws = m_main->m_nav.FocusPoint();
			status += pr::FmtS("Mouse: {%3.3f %3.3f %3.3f} Focus: {%3.3f %3.3f %3.3f} Focus Distance: %3.3f"
				,mouse_ws.x ,mouse_ws.y ,mouse_ws.z
				,focus_ws.x ,focus_ws.y ,focus_ws.z
				,m_main->m_cam.FocusDist());
		}
		{
			// Display zoom
			float zoom = m_main->m_nav.Zoom();
			if (!pr::FEql(zoom, 1.0f, 0.001f))
				status += pr::FmtS(" Zoom: %3.3f", zoom);
		}
		pr::events::Send(Event_Status(status.c_str()));
	}

	// Display the about dialog box
	void MainGUI::ShowAbout() const
	{
		CAboutLineDrawer dlg;
		dlg.DoModal(m_hWnd);
	}

	// Recent files on click
	void MainGUI::MenuList_OnClick(pr::gui::MenuList* sender, pr::gui::MenuList::Item const& item)
	{
		if (static_cast<pr::gui::RecentFiles*>(sender) == &m_recent_files)
		{
			FileOpen(item.m_name.c_str(), pr::KeyDown(VK_SHIFT));
		}
		if (sender == &m_saved_views)
		{
			m_main->m_nav.RestoreView((NavManager::SavedViewID)item.m_tag);
			UpdateUI();
			m_main->RenderNeeded();
		}
	}

	// When the recent files list changes, save the settings
	void MainGUI::MenuList_ListChanged(pr::gui::MenuList* sender)
	{
		if (static_cast<pr::gui::RecentFiles*>(sender) == &m_recent_files)
		{
			m_main->m_settings.m_RecentFiles = m_recent_files.Export();
		}
		if (sender == &m_saved_views)
		{
		}
	}

	// Handle info events
	void MainGUI::OnEvent(Event_Info const& e)
	{
		(void)e;
		PR_INFO(PR_DBG_LDR, e.m_msg.c_str());
	}

	// Handle warning events
	void MainGUI::OnEvent(Event_Warn const& e)
	{
		(void)e;
		PR_INFO(PR_DBG_LDR, e.m_msg.c_str());
	}

	// Handle error events
	void MainGUI::OnEvent(Event_Error const& e)
	{
		if (!m_main || m_main->m_settings.m_ErrorOutputMsgBox)
			::MessageBoxA(m_hWnd, e.m_msg.c_str(), pr::FmtS("%s Error", AppTitleA()), MB_OK|MB_ICONERROR);
		else
		{} // error msg on status line
	}

	// Status text update
	void MainGUI::OnEvent(Event_Status const& e)
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

	// Called when the viewport is being built
	void MainGUI::OnEvent(pr::rdr::Evt_UpdateScene const& e)
	{
		// Render the selection box
		if (m_main->m_settings.m_ShowSelectionBox && m_store_ui.SelectedCount() != 0)
			e.m_scene.AddInstance(m_main->m_selection_box);

		// Tools instances
		if (m_measure_tool_ui.Gfx())
			m_measure_tool_ui.Gfx()->AddToScene(e.m_scene);
		if (m_angle_tool_ui.Gfx())
			m_angle_tool_ui.Gfx()->AddToScene(e.m_scene);
	}

	// Handle refresh requests
	void MainGUI::OnEvent(Event_Refresh const&)
	{
		m_main->RenderNeeded();
	}
	void MainGUI::OnEvent(pr::ldr::Evt_Refresh const&)
	{
		m_main->RenderNeeded();
	}

	// The measure tool window was closed
	void MainGUI::OnEvent(pr::ldr::Evt_LdrMeasureCloseWindow const&)
	{
		UpdateUI();
		m_main->RenderNeeded();
	}

	// The measurement info has updated
	void MainGUI::OnEvent(pr::ldr::Evt_LdrMeasureUpdate const&)
	{
		m_main->RenderNeeded();
	}

	// The angle tool window was closed
	void MainGUI::OnEvent(pr::ldr::Evt_LdrAngleDlgCloseWindow const&)
	{
		UpdateUI();
		m_main->RenderNeeded();
	}

	// The angle info has updated
	void MainGUI::OnEvent(pr::ldr::Evt_LdrAngleDlgUpdate const&)
	{
		m_main->RenderNeeded();
	}

	// A number of objects are about to be added
	void MainGUI::OnEvent(pr::ldr::Evt_AddBegin const&)
	{
		m_suspend_render = true;
	}

	// The last object in a group has been added
	void MainGUI::OnEvent(pr::ldr::Evt_AddEnd const&)
	{
		m_suspend_render = false;
		m_main->RenderNeeded();
	}

	// Occurs when an error happens during UserSetting parsing
	void MainGUI::OnEvent(pr::settings::Evt<UserSettings> const& e)
	{
		MessageBox(e.m_msg.c_str(), "Settings Error", MB_OK);
	}

	// Parse command line options
	bool MainGUI::CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end)
	{
		// Syntax: LineDrawer -plugin "c:\myplugin.dll" arg1 arg2
		if (pr::str::EqualI(option, "-plugin") && arg != arg_end)
		{
			std::string plugin_name = *arg;
			std::string plugin_args = "";
			for (++arg; arg != arg_end && !pr::cmdline::IsOption(*arg); ++arg) plugin_args += *arg;
			try { m_main->m_plugin_mgr.Add(plugin_name.c_str(), plugin_args.c_str()); }
			catch (LdrException const& e) { pr::events::Send(Event_Error(pr::Fmt("Failed to load plugin %s.\nReason: %s", plugin_name.c_str(), e.what()))); }
			return true;
		}
		return false;
	}
}
/*

// Clear the background during resize
BOOL ldr::MainGUI::OnEraseBkgnd(CDCHandle dc)
{
	SetMsgHandled(FALSE);
	if (m_sizing)
	{
		CBrush brush; brush.CreateSolidBrush(m_main->m_settings.m_BackgroundColour.GetColorRef());
		CRect r; GetClientRect(&r);
		CPoint ctr = r.CenterPoint();
		dc.FillRect(&r, brush);
		dc.SetTextAlign(TA_CENTER|TA_BASELINE);
		dc.SetBkMode(TRANSPARENT);
		dc.TextOutA(ctr.x, ctr.y, "...resizing...");
	}
	return TRUE;
}

// Pre translate windows messages
BOOL ldr::MainGUI::PreTranslateMessage(MSG* pMsg)
{
	//pr::debug_wm::DebugMessage(pMsg,[](int wm){ return wm != WM_TIMER; });

	// Forward messages for the store ui to that dialog
	if (m_ldr != 0 && m_main->m_store_ui.IsChild(pMsg->hwnd))
		return IsDialogMessage(pMsg);

	// Handle key accelerators
	if (::TranslateAccelerator(m_hWnd, m_haccel, pMsg) != 0)
		return TRUE;

	// Intercept key presses
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
BOOL ldr::MainGUI::OnIdle(int)
{
	// If the settings have changed, save them
	if (m_main->m_settings.SaveRequired())
		m_main->m_settings.Save();

	return FALSE;
}
*/
