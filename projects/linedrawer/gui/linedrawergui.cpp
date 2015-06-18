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
std::shared_ptr<pr::app::IAppMainGui> pr::app::CreateGUI(LPTSTR lpstrCmdLine, int nCmdShow)
{
	return pr::app::CreateGUI<::ldr::MainGUI>(lpstrCmdLine, nCmdShow);
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

	MainGUI::MainGUI(LPTSTR cmdline, int showwnd)
		:base(AppTitleW(), CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, DefaultFormStyle, DefaultFormStyleEx, IDR_MENU_MAIN, IDR_ACCELERATOR, "ldr_main")
		,m_status(this, IDC_STATUSBAR_MAIN, L"Ready", "status bar")
		,m_recent_files()
		,m_saved_views()
		,m_store_ui()
		,m_editor_ui()
		,m_measure_tool_ui(ReadPoint, &m_main->m_cam, m_main->m_rdr, *this)
		,m_angle_tool_ui(ReadPoint, &m_main->m_cam, m_main->m_rdr, *this)
		,m_menu(Menu())
		,m_mouse_status_updates(true)
		,m_suspend_render(false)
		,m_status_pri()
	{
		// Parse the command line
		pr::EnumCommandLine(cmdline, *this);

		// Note, 'm_main' can be null if the SimMsgLoop runs these contexts after the
		// window has been destroyed, but before the message loop has been shutdown.
		// This shouldn't really happen and needs investigating

		// Set icons
		Icon((HICON)::LoadImage(m_hinst, MAKEINTRESOURCE(IDI_ICON_MAIN), IMAGE_ICON, ::GetSystemMetrics(SM_CXICON),   ::GetSystemMetrics(SM_CYICON),   LR_DEFAULTCOLOR) ,true);
		Icon((HICON)::LoadImage(m_hinst, MAKEINTRESOURCE(IDI_ICON_MAIN), IMAGE_ICON, ::GetSystemMetrics(SM_CXSMICON), ::GetSystemMetrics(SM_CYSMICON), LR_DEFAULTCOLOR) ,false);

		// Status bar
		int status_panes[] = {-1};
		m_status.Parts(1, status_panes);
		m_status.Visible(true);

		// Initialise the menu lists
		m_recent_files.Attach(pr::gui::GetMenuByName(Menu(), TEXT("&File,&Recent Files"))      ,ID_FILE_RECENTFILES ,0xffffffff ,this);
		m_saved_views .Attach(pr::gui::GetMenuByName(Menu(), TEXT("&Navigation,&Saved Views")) ,ID_NAV_SAVEDVIEWS   ,0xffffffff ,this);

		// Initialise the object manager
		m_store_ui.Create(*this);
		m_store_ui.Settings(m_main->m_settings.m_ObjectManagerSettings.c_str());

		// Initialise the script editor
		m_editor_ui.Create(*this);
		m_editor_ui.Text(m_main->m_settings.m_NewObjectString.c_str());
		m_editor_ui.Render = [&](std::string&& script)
			{
				try
				{
					m_main->m_settings.m_NewObjectString = std::move(script);
					m_main->m_settings.Save();
					m_main->m_sources.AddString(m_main->m_settings.m_NewObjectString);
					m_main->RenderNeeded();
				}
				catch (std::exception const& e)
				{
					pr::events::Send(Event_Error(pr::FmtS("Script error found while parsing source.\n%s", e.what())));
				}
			};

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
		AllowDrop(true);

		// Set the window minimum size
		m_min_max_info.ptMinTrackSize.x = 320;
		m_min_max_info.ptMinTrackSize.y = 200;

		// Create a step context for rendering
		enum { force_render = false };
		m_msg_loop.AddStepContext("rdr main loop", [this](double)
		{
			if (m_main && !m_suspend_render)
			m_main->DoRender(force_render);
		}, 60.0f, false);

		// Add a step context for 30Hz stepping
		m_msg_loop.AddStepContext("plugin step", [this](double s)
		{
			Step30Hz(s);
		}, 30.0f, true);

		// Add a step context for polling file state
		m_msg_loop.AddStepContext("watch_files", [this](double)
		{
			// If file watching is turned on, look for changed files
			if (m_main->m_settings.m_WatchForChangedFiles)
				m_main->m_sources.RefreshChangedFiles();
		}, 1.0f, false);

		//m_msg_loop.AddStepContext("debug", [this](double)
		//{
		//	auto nc = NonClientAreas();
		//	auto h = {50,300,500,700,1000,700,500,300};
		//	static int i = 0;
		//	OutputDebugStringA(pr::FmtS("\n%s PreMoveWindow (%d,%d) %d %d\n", m_name, WindowRect().left, WindowRect().top, ClientRect().width(), ClientRect().height()));
		//	::MoveWindow(*this,
		//		2500,0,
		//		h.begin()[i]-nc.left+nc.right,
		//		h.begin()[i]-nc.top+nc.bottom, TRUE);
		//	OutputDebugStringA(pr::FmtS("%s PostMoveWindow (%d,%d) %d %d\n\n", m_name, WindowRect().left, WindowRect().top, ClientRect().width(), ClientRect().height()));
		//	++i %= h.size();
		//}, 0.5f, false);

		Show(showwnd);
	}
	MainGUI::~MainGUI()
	{
		m_store_ui.Close();
		m_editor_ui.Close();
		m_measure_tool_ui.Close();
		m_angle_tool_ui.Close();
	}

	// Message map function
	bool MainGUI::ProcessWindowMessage(HWND parent_hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result)
	{
		return
			m_recent_files.ProcessWindowMessage(parent_hwnd, message, wparam, lparam, result) ||
			m_saved_views .ProcessWindowMessage(parent_hwnd, message, wparam, lparam, result) ||
			base::ProcessWindowMessage(parent_hwnd, message, wparam, lparam, result);
	}

	// Handler timer messages
	void MainGUI::Step30Hz(double elapsed_seconds)
	{
		// Poll plugins
		if (m_main)
			m_main->m_plugin_mgr.Poll(elapsed_seconds);

		// Orbit the camera if enabled
		if (m_main->m_settings.m_CameraOrbit)
		{
			m_main->m_nav.OrbitCamera(m_main->m_settings.m_CameraOrbitSpeed);
			m_main->RenderNeeded();
		}
	}

	// Paint the window
	bool MainGUI::OnPaint(PaintEventArgs const& args)
	{
		if (m_suspend_render) return false;
		return base::OnPaint(args);
	}

	// Handle files dropped onto the main window
	void MainGUI::OnDropFiles(DropFilesEventArgs const& drop)
	{
		if (drop.m_filepaths.empty())
			return;

		// Clear the data unless shift is held down
		if (!pr::KeyDown(VK_SHIFT))
			m_main->m_sources.Clear();

		// Load the files
		for (auto const& path : drop.m_filepaths)
			m_main->m_sources.AddFile(Narrow(path).c_str());
	}

	// Handle switching to/from full screen
	void MainGUI::OnFullScreenToggle(bool enable_fullscreen)
	{
		if (enable_fullscreen)
		{
			// Hide the menu and status bar so that the the client area
			// is calculated correctly.
			m_menu = Menu();
			Menu(nullptr);
			m_status.Visible(false);

			//todo, make this correct
			pr::rdr::SystemConfig config;
			pr::rdr::SystemConfig::ModeCont modes;
			config.m_adapters[0].m_outputs[0].GetDisplayModes(DXGI_FORMAT_R8G8B8A8_UNORM, modes);

			// Get the full screen display mode from the settings
			pr::rdr::DisplayMode mode(1920, 1080);
			m_main->m_window.FullScreenMode(true, mode);
		}
		else
		{
			pr::rdr::DisplayMode mode;
			m_main->m_window.FullScreenMode(false, mode);

			// Show the status and menu controls again
			Menu(m_menu);
			m_status.Visible(true);
		}
	}

	// Handle key presses
	bool MainGUI::OnKey(KeyEventArgs const& args)
	{
		switch (args.m_vk_key)
		{
		case VK_SPACE:
			m_store_ui.Show(*this);
			m_store_ui.Populate(m_main->m_store);
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

		// Forward key presses to the input handler
		if (m_main->m_input->KeyInput(args.m_vk_key, args.m_down, args.m_flags, args.m_repeats))
			return true;

		return base::OnKey(args);
	}

	// Convert screen space to normalised screen space
	pr::v2 MainGUI::ToNormSS(pr::v2 const& pt_ss)
	{
		auto view = pr::IRect::make(pr::iv2Zero, m_main->m_nav.ViewSize());
		return pr::NormalisePoint(view, pt_ss, 1.0f, -1.0f);
	}

	// Override mouse navigation
	bool MainGUI::OnMouseButton(MouseEventArgs const& args)
	{
		if (args.m_down)
			::SetCapture(*this);
		else
			::ReleaseCapture();

		auto btn = static_cast<pr::camera::ENavBtn::Enum_>(args.m_button);
		auto mouse_loc = pr::To<pr::v2>(args.m_point);

		// Forward to the input handler
		if (m_main->m_input->MouseInput(ToNormSS(mouse_loc), args.m_down ? btn : 0, true))
			pr::events::Send(Event_Refresh());

		MouseStatusUpdate(mouse_loc);
		return false;
	}
	void MainGUI::OnMouseMove(MouseEventArgs const& args)
	{
		auto btn = static_cast<pr::camera::ENavBtn::Enum_>(args.m_button);
		auto mouse_loc = pr::To<pr::v2>(args.m_point);

		if (m_main->m_input->MouseInput(ToNormSS(mouse_loc), btn, false))
			pr::events::Send(Event_Refresh());

		MouseStatusUpdate(mouse_loc);
	}
	bool MainGUI::OnMouseClick(MouseEventArgs const& args)
	{
		auto btn = static_cast<pr::camera::ENavBtn::Enum_>(args.m_button);
		auto mouse_loc = pr::To<pr::v2>(args.m_point);
		
		if (m_main->m_input->MouseClick(ToNormSS(mouse_loc), btn))
			pr::events::Send(Event_Refresh());

		MouseStatusUpdate(mouse_loc);
		return false;
	}
	bool MainGUI::OnMouseWheel(MouseWheelArgs const& args)
	{
		pr::v2 mouse_loc = pr::To<pr::v2>(args.m_point);

		// delta is '1.0f' for a single wheel click
		if (m_main->m_input->MouseWheel(ToNormSS(mouse_loc), args.m_delta/120.0f))
			pr::events::Send(Event_Refresh());

		MouseStatusUpdate(mouse_loc);
		return false;
	}

	// Handle the main menu
	bool MainGUI::HandleMenu(UINT item_id, UINT, HWND)
	{
		switch (item_id)
		{
		default: return false;
		case ID_ACCELERATOR_FILENEW          : OnFileNew(); break;
		case ID_ACCELERATOR_FILENEWSCRIPT    : OnFileNewScript(); break;
		case ID_ACCELERATOR_FILEOPEN         : OnFileOpen(false); break;
		case ID_ACCELERATOR_FILEOPEN_ADDITIVE: OnFileOpen(true); break;
		case ID_ACCELERATOR_WIREFRAME        : OnToggleFillMode(); break;
		case ID_ACCELERATOR_EDITOR           : OnEditSourceFiles(); break;
		case ID_ACCELERATOR_CAMERAPOS        : OnSetCameraPosition(); break;
		case ID_ACCELERATOR_PLUGINMGR        : OnShowPluginMgr(); break;
		case ID_ACCELERATOR_LIGHTING_DLG     : OnShowLightingDlg(); break;
		case ID_FILE_NEW1                    : OnFileNew(); break;
		case ID_FILE_NEWSCRIPT               : OnFileNewScript(); break;
		case ID_FILE_OPEN1                   : OnFileOpen(false); break;
		case ID_FILE_ADDITIVEOPEN            : OnFileOpen(true); break;
		case ID_FILE_EXIT                    : CloseApp(0); break;
		case IDCLOSE                         : CloseApp(0); break;
		case ID_NAV_RESETVIEW_ALL            : OnResetView(EObjectBounds::All); break;
		case ID_NAV_RESETVIEW_SELECTED       : OnResetView(EObjectBounds::Selected); break;
		case ID_NAV_RESETVIEW_VISIBLE        : OnResetView(EObjectBounds::Visible); break;
		case ID_NAV_ALIGN_NONE               : OnNavAlign(pr::v4Zero); break;
		case ID_NAV_ALIGN_X                  : OnNavAlign(pr::v4XAxis); break;
		case ID_NAV_ALIGN_Y                  : OnNavAlign(pr::v4YAxis); break;
		case ID_NAV_ALIGN_Z                  : OnNavAlign(pr::v4ZAxis); break;
		case ID_NAV_ALIGN_CURRENT            : OnNavAlign(m_main->m_nav.CameraToWorld().y); break;
		case ID_NAV_VIEW_AXIS_POSX           : OnViewAxis( pr::v4XAxis); break;
		case ID_NAV_VIEW_AXIS_NEGX           : OnViewAxis(-pr::v4XAxis); break;
		case ID_NAV_VIEW_AXIS_POSY           : OnViewAxis( pr::v4YAxis); break;
		case ID_NAV_VIEW_AXIS_NEGY           : OnViewAxis(-pr::v4YAxis); break;
		case ID_NAV_VIEW_AXIS_POSZ           : OnViewAxis( pr::v4ZAxis); break;
		case ID_NAV_VIEW_AXIS_NEGZ           : OnViewAxis(-pr::v4ZAxis); break;
		case ID_NAV_VIEW_AXIS_POSXYZ         : OnViewAxis(-pr::v4::make(0.577350f, 0.577350f, 0.577350f, 0.0f)); break;
		case ID_NAV_CLEARSAVEDVIEWS          : OnSaveView(true); break;
		case ID_NAV_SAVEVIEW                 : OnSaveView(false); break;
		case ID_NAV_SETFOCUSPOSITION         : OnSetFocusPosition(); break;
		case ID_NAV_SETCAMERAPOSITION        : OnSetCameraPosition(); break;
		case ID_NAV_ORBIT                    : OnOrbit(); break;
		case ID_DATA_OBJECTMANAGER           : OnShowObjectManagerUI(); break;
		case ID_DATA_EDITSOURCEFILES         : OnEditSourceFiles(); break;
		case ID_DATA_CLEARSCENE              : OnDataClearScene(); break;
		case ID_DATA_AUTOREFRESH             : OnDataAutoRefresh(); break;
		case ID_DATA_CREATE_DEMO_SCENE       : OnCreateDemoScene(); break;
		case ID_RENDERING_SHOWFOCUS          : OnShowFocus(); break;
		case ID_RENDERING_SHOWORIGIN         : OnShowOrigin(); break;
		case ID_RENDERING_SHOWSELECTION      : OnShowSelection(); break;
		case ID_RENDERING_SHOWOBJECTBBOXES   : OnShowObjBBoxes(); break;
		case ID_RENDERING_WIREFRAME          : OnToggleFillMode(); break;
		case ID_RENDERING_RENDER2D           : OnRender2D(); break;
		case ID_RENDERING_TECHNIQUE          : OnRenderTechnique(); break;
		case ID_RENDERING_LIGHTING           : OnShowLightingDlg(); break;
		case ID_TOOLS_MEASURE                : OnShowToolDlg(ID_TOOLS_MEASURE); break;
		case ID_TOOLS_ANGLE                  : OnShowToolDlg(ID_TOOLS_ANGLE); break;
		case ID_TOOLS_MOVE                   : OnManipulateMode(); break;
		case ID_TOOLS_OPTIONS                : OnShowOptions(); break;
		case ID_TOOLS_PLUGINMGR              : OnShowPluginMgr(); break;
		case ID_WINDOW_ALWAYSONTOP           : OnWindowAlwaysOnTop(); break;
		case ID_WINDOW_BACKGROUNDCOLOUR      : OnWindowBackgroundColour(); break;
		case ID_WINDOW_EXAMPLESCRIPT         : OnWindowExampleScript(); break;
		case ID_WINDOW_CHECKFORUPDATES       : OnCheckForUpdates(); break;
		case ID_WINDOW_ABOUTLINEDRAWER       : OnWindowShowAboutBox(); break;
		}
		return true;
	}

	// Open a text panel for adding new ldr objects immediately
	void MainGUI::OnFileNew()
	{
		m_editor_ui.Visible(true);
	}

	// Create a new text file for ldr script
	void MainGUI::OnFileNewScript()
	{
		try
		{
			WTL::CFileDialog fd(FALSE,0,0,0,FileOpenFilter,*this);
			if (fd.DoModal() != IDOK) return;
			FileNew(fd.m_szFileName);
		}
		catch (std::exception const& e)
		{
			pr::events::Send(Event_Error(pr::FmtS("Creating a new script failed.\nError details: %s", e.what())));
		}
	}

	// Open a line drawer script file and optionally add it to the current scene
	void MainGUI::OnFileOpen(bool additive)
	{
		WTL::CFileDialog fd(TRUE,0,0,0,FileOpenFilter,*this);
		if (fd.DoModal() != IDOK) return;
		FileOpen(fd.m_szFileName, additive);
	}

	// Reset the view to all, selected, or visible objects
	void MainGUI::OnResetView(EObjectBounds bounds)
	{
		m_main->ResetView(bounds);
		m_main->RenderNeeded();
	}

	// View the current focus point looking down the selected axis
	void MainGUI::OnViewAxis(pr::v4 const& axis)
	{
		// axis = m_main->m_nav.CameraToWorld().z; use this for non-menu option
		pr::m4x4 c2w = m_main->m_nav.CameraToWorld();
		pr::v4 focus = m_main->m_nav.FocusPoint();
		pr::v4 cam = focus + axis * m_main->m_nav.FocusDistance();
		pr::v4 up = pr::Parallel(axis, c2w.y) ? pr::Cross3(axis, c2w.x) : c2w.y;
		m_main->m_nav.LookAt(cam, focus, up);
		m_main->RenderNeeded();
	}

	// Set the position of the camera focus point in world space
	void MainGUI::OnSetFocusPosition()
	{
		CTextEntryDlg dlg(*this, "Enter focus point position", "0 0 0", false);
		if (dlg.DoModal() != IDOK) return;

		float pos[3];
		if (pr::str::ExtractRealArrayC(&pos[0], 3, dlg.m_body.c_str()))
			m_main->m_nav.FocusPoint(pr::v4::make(pos, 1.0f));
		else
			::MessageBoxA(*this, "Format incorrect", "Focus point not set", MB_OK|MB_ICONERROR);

		m_main->RenderNeeded();
	}

	// Set the position of the camera
	void MainGUI::OnSetCameraPosition()
	{
		pr::camera::PositionDlg dlg;
		dlg.m_cam = m_main->m_cam;
		if (dlg.DoModal(*this) != IDOK) return;
		m_main->m_cam = dlg.m_cam;
		m_main->RenderNeeded();
	}

	// Align the camera to the selected axis
	void MainGUI::OnNavAlign(pr::v4 const& axis)
	{
		m_main->m_nav.CameraAlign(axis);
		m_main->m_settings.m_CameraAlignAxis = m_main->m_nav.CameraAlign();
		UpdateUI();
		m_main->RenderNeeded();
	}

	// Record the current camera position as a saved camera view
	void MainGUI::OnSaveView(bool clear_saves)
	{
		if (clear_saves)
		{
			m_main->m_nav.ClearSavedViews();
			m_saved_views.Clear();
		}
		else
		{
			CTextEntryDlg dlg(*this, "Label for this view", pr::FmtS("view%d", m_saved_views.Items().size()), false);
			if (dlg.DoModal() != IDOK) return;

			auto id = m_main->m_nav.SaveView();
			m_saved_views.Add(dlg.m_body.c_str(), (void*)id, false, true);
		}
	}

	// Toggle camera orbit mode
	void MainGUI::OnOrbit()
	{
		m_main->m_settings.m_CameraOrbit = !m_main->m_settings.m_CameraOrbit;
		m_main->m_nav.OrbitCamera(0.0f);
		UpdateUI();
	}

	// Display the object manager UI
	void MainGUI::OnShowObjectManagerUI()
	{
		m_store_ui.Show(*this);
		m_store_ui.Populate(m_main->m_store);
	}

	// Spawn the text editor with the source files
	void MainGUI::OnEditSourceFiles()
	{
		OpenTextEditor(m_main->m_sources.List());
	}

	// Remove all objects from the object manager
	void MainGUI::OnDataClearScene()
	{
		m_main->m_store.clear();
		m_main->RenderNeeded();
	}

	// Toggle auto refresh file sources
	void MainGUI::OnDataAutoRefresh()
	{
		m_main->m_settings.m_WatchForChangedFiles = !m_main->m_settings.m_WatchForChangedFiles;
		UpdateUI();
	}

	// Generate a self created scene of objects
	void MainGUI::OnCreateDemoScene()
	{
		m_main->CreateDemoScene();
		m_main->ResetView(EObjectBounds::All);
		m_main->RenderNeeded();
	}

	// Toggle visibility of the focus point
	void MainGUI::OnShowFocus()
	{
		m_main->m_settings.m_ShowFocusPoint = !m_main->m_settings.m_ShowFocusPoint;
		UpdateUI();
		m_main->RenderNeeded();
	}

	// Toggle visibility of the origin point
	void MainGUI::OnShowOrigin()
	{
		m_main->m_settings.m_ShowOrigin = !m_main->m_settings.m_ShowOrigin;
		UpdateUI();
		m_main->RenderNeeded();
	}

	// Toggle visibility of the selection box
	void MainGUI::OnShowSelection()
	{
		m_main->m_settings.m_ShowSelectionBox = !m_main->m_settings.m_ShowSelectionBox;
		UpdateUI();
		m_main->RenderNeeded();
	}

	// Toggle visibility of the object space bounding boxes
	void MainGUI::OnShowObjBBoxes()
	{
		m_main->m_settings.m_ShowObjectBBoxes = !m_main->m_settings.m_ShowObjectBBoxes;
		UpdateUI();
		m_main->RenderNeeded();
	}

	// Cycle through solid, wireframe, and solid+wire
	void MainGUI::OnToggleFillMode()
	{
		int mode = (m_main->m_settings.m_GlobalFillMode + 1) % EFillMode::NumberOf;
		m_main->m_settings.m_GlobalFillMode = static_cast<EFillMode>(mode);
		UpdateUI();
		m_main->RenderNeeded();
	}

	// Toggle between perspective and orthographic
	void MainGUI::OnRender2D()
	{
		m_main->m_nav.Render2D(!m_main->m_nav.Render2D());
		UpdateUI();
		m_main->RenderNeeded();
	}

	// Toggle between forward and deferred rendering
	void MainGUI::OnRenderTechnique()
	{
		auto fwd = {pr::rdr::ERenderStep::ForwardRender};
		auto def = {pr::rdr::ERenderStep::GBuffer, pr::rdr::ERenderStep::DSLighting};
		
		if (m_main->m_scene.FindRStep<pr::rdr::ForwardRender>() != nullptr)
			m_main->m_scene.SetRenderSteps(pr::rdr::Scene::DeferredRendering());
		else
			m_main->m_scene.SetRenderSteps(pr::rdr::Scene::ForwardRendering());

		UpdateUI();
		m_main->RenderNeeded();
	}

	// Display the lighting dialog
	void MainGUI::OnShowLightingDlg()
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
		if (dlg.DoModal(*this) != IDOK) return;
		m_main->m_settings.m_Light                 = dlg.m_light;
		m_main->m_settings.m_LightIsCameraRelative = dlg.m_camera_relative;
		m_main->RenderNeeded();
	}

	// Display a tool dialog
	void MainGUI::OnShowToolDlg(int tool)
	{
		switch (tool)
		{
		case ID_TOOLS_MEASURE: m_measure_tool_ui.Show(m_measure_tool_ui.IsWindowVisible() == FALSE); break;
		case ID_TOOLS_ANGLE:   m_angle_tool_ui  .Show(m_angle_tool_ui  .IsWindowVisible() == FALSE); break;
		}
		UpdateUI();
	}

	// Switch the nav mode
	void MainGUI::OnManipulateMode()
	{
		auto turn_on = m_main->ControlMode() != EControlMode::Manipulation;
		m_main->ControlMode(turn_on ? EControlMode::Manipulation : EControlMode::Navigation);
		UpdateUI();
	}

	// Display the options dialog
	void MainGUI::OnShowOptions()
	{
		COptionsDlg dlg(m_main->m_settings, *this);
		if (dlg.DoModal() != IDOK) return;
		dlg.GetSettings(m_main->m_settings);
		m_main->RenderNeeded();
	}

	// Display the plugin manager dialog
	void MainGUI::OnShowPluginMgr()
	{
		PluginManagerDlg dlg(m_main->m_plugin_mgr, *this);
		if (dlg.DoModal() != IDOK) return;
	}

	// Set the window draw order so that the line drawer window is always on top
	void MainGUI::OnWindowAlwaysOnTop()
	{
		m_main->m_settings.m_AlwaysOnTop = !m_main->m_settings.m_AlwaysOnTop;
		::SetWindowPos(*this, m_main->m_settings.m_AlwaysOnTop ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE|SWP_NOSIZE);
		UpdateUI();
	}

	// Set the background colour
	void MainGUI::OnWindowBackgroundColour()
	{
		CColorDialog dlg(m_main->m_settings.m_BackgroundColour.GetColorRef(), 0, *this);
		if (dlg.DoModal() != IDOK) return;
		m_main->m_settings.m_BackgroundColour = dlg.GetColor() & 0x00FFFFFF;
		m_main->RenderNeeded();
	}

	// Show a window containing the demo scene script
	void MainGUI::OnWindowExampleScript()
	{
		m_editor_ui.Text(pr::ldr::CreateDemoScene().c_str());
		m_editor_ui.Visible(true);
	}

	// Check the web for the latest version
	void MainGUI::OnCheckForUpdates()
	{
		std::string version;
		pr::network::WebGet("http://www.rylogic.co.nz/latest_versions.html", version);

		pr::xml::Node root;
		try { pr::xml::Load(version.c_str(), version.size(), root); }
		catch (std::exception const&)
		{
			::MessageBoxA(*this, "Version information invalid", "Check For Updates", MB_OK|MB_ICONERROR);
		}
	}

	// Show the about box
	void MainGUI::OnWindowShowAboutBox()
	{
		ShowAbout();
	}

	// Shut the app down
	void MainGUI::CloseApp(int exit_code)
	{
		m_angle_tool_ui.Close();
		m_measure_tool_ui.Close();
		m_editor_ui.Close();
		m_store_ui.Close();
		Close(exit_code);
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
			if (!additive) m_main->m_sources.Clear();
			m_main->m_sources.AddFile(filepath);

			// Reset the camera if flagged
			if (m_main->m_settings.m_ResetCameraOnLoad)
				m_main->ResetView(EObjectBounds::All);

			// Set the window title
			pr::gui::string title;
			title += AppTitleW();
			title += L" - ";
			title += Widen(filepath);
			Text(title);

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
		CheckMenuItem(Menu(), ID_NAV_ORBIT ,m_main->m_settings.m_CameraOrbit ? MF_CHECKED : MF_UNCHECKED);

		// Auto refresh
		CheckMenuItem(Menu(), ID_DATA_AUTOREFRESH ,m_main->m_settings.m_WatchForChangedFiles ? MF_CHECKED : MF_UNCHECKED);

		// Stock models
		CheckMenuItem(Menu(), ID_RENDERING_SHOWFOCUS        ,m_main->m_settings.m_ShowFocusPoint   ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(Menu(), ID_RENDERING_SHOWORIGIN       ,m_main->m_settings.m_ShowOrigin       ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(Menu(), ID_RENDERING_SHOWSELECTION    ,m_main->m_settings.m_ShowSelectionBox ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(Menu(), ID_RENDERING_SHOWOBJECTBBOXES ,m_main->m_settings.m_ShowObjectBBoxes ? MF_CHECKED : MF_UNCHECKED);

		// Set the text to the "next" mode
		switch (m_main->m_settings.m_GlobalFillMode)
		{
		case 0: ModifyMenu(Menu(), ID_RENDERING_WIREFRAME, MF_BYCOMMAND, ID_RENDERING_WIREFRAME, "&Wireframe\tCtrl+W");  break;
		case 1: ModifyMenu(Menu(), ID_RENDERING_WIREFRAME, MF_BYCOMMAND, ID_RENDERING_WIREFRAME, "&Wire + Solid\tCtrl+W"); break;
		case 2: ModifyMenu(Menu(), ID_RENDERING_WIREFRAME, MF_BYCOMMAND, ID_RENDERING_WIREFRAME, "&Solid\tCtrl+W");      break;
		}

		// Align axis checked items
		pr::v4 cam_align = m_main->m_settings.m_CameraAlignAxis;
		CheckMenuItem(Menu() ,ID_NAV_ALIGN_NONE    ,cam_align == pr::v4Zero  ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(Menu() ,ID_NAV_ALIGN_X       ,cam_align == pr::v4XAxis ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(Menu() ,ID_NAV_ALIGN_Y       ,cam_align == pr::v4YAxis ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(Menu() ,ID_NAV_ALIGN_Z       ,cam_align == pr::v4ZAxis ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(Menu() ,ID_NAV_ALIGN_CURRENT ,cam_align != pr::v4Zero && cam_align != pr::v4XAxis &&  cam_align != pr::v4YAxis && cam_align != pr::v4ZAxis ? MF_CHECKED : MF_UNCHECKED);

		// Render 2d menu item
		ModifyMenu(Menu(), ID_RENDERING_RENDER2D, MF_BYCOMMAND, ID_RENDERING_RENDER2D, m_main->m_nav.Render2D() ? "&Perspective" : "&Orthographic");
		ModifyMenu(Menu(), ID_RENDERING_TECHNIQUE, MF_BYCOMMAND, ID_RENDERING_TECHNIQUE, m_main->m_scene.FindRStep<pr::rdr::ForwardRender>() ? "&Deferred Rendering" : "&Forward Rendering");

		// The tools windows
		CheckMenuItem(Menu()  ,ID_TOOLS_MEASURE ,m_measure_tool_ui.IsWindowVisible() ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(Menu()  ,ID_TOOLS_ANGLE   ,m_angle_tool_ui  .IsWindowVisible() ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(Menu()  ,ID_TOOLS_MOVE    ,m_main->ControlMode() == EControlMode::Manipulation ? MF_CHECKED : MF_UNCHECKED);
		//EnableMenuItem(Menu() ,ID_TOOLS_MOVE    ,m_main->m_store.empty() ? MF_DISABLED : MF_ENABLED);

		// Topmost window
		CheckMenuItem(Menu(), ID_WINDOW_ALWAYSONTOP, m_main->m_settings.m_AlwaysOnTop ? MF_CHECKED : MF_UNCHECKED);
	}

	// Update the status text
	void MainGUI::MouseStatusUpdate(pr::v2 const& mouse_location)
	{
		if (!m_mouse_status_updates) return;
		string status;
		{
			// Display mouse coordinates
			pr::v4 mouse_ss = pr::v4::make(mouse_location, m_main->m_nav.FocusDistance(), 0.0f);
			pr::v4 mouse_ws = m_main->m_nav.SSPointToWSPoint(mouse_ss);
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
		dlg.DoModal(*this);
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
			m_main->m_nav.RestoreView((Navigation::SavedViewID)item.m_tag);
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
			::MessageBoxA(*this, e.m_msg.c_str(), pr::FmtS("%s Error", AppTitleA()), MB_OK|MB_ICONERROR);
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
			m_status.Text(0, Widen(e.m_msg));
			m_status.Font(e.m_bold ? m_status_pri.m_bold_font : m_status_pri.m_normal_font);
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
		Invalidate();
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
	void MainGUI::OnEvent(ldr::Event_StoreChanging const&)
	{
		m_suspend_render = true;
	}

	// The last object in a group has been added
	void MainGUI::OnEvent(ldr::Event_StoreChanged const&)
	{
		m_suspend_render = false;
		m_main->RenderNeeded();
		UpdateUI();
	}

	// Occurs when an error happens during UserSetting parsing
	void MainGUI::OnEvent(pr::settings::Evt<UserSettings> const& e)
	{
		MessageBoxA(*this, e.m_msg.c_str(), "Settings Error", MB_OK);
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
