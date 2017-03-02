//*****************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
#include "linedrawer/main/forward.h"
#include "linedrawer/main/main.h"
#include "linedrawer/gui/options_dlg.h"
#include "linedrawer/gui/about_ui.h"
#include "linedrawer/gui/text_panel_ui.h"
#include "linedrawer/utility/debug.h"

using namespace pr::gui;
using namespace pr::ldr;

namespace ldr
{
	wchar_t const* AppTitleW()     { return L"LineDrawer"; }
	char const*    AppTitleA()     { return "LineDrawer"; }
	char const*    AppVersion()    { return "4.01.00"; }
	char const*    AppCopyright()  { return "Copyright (c) Rylogic Limited 2002"; }
	char const*    AppString()     { return pr::FmtX<struct Ldr, 128>("%s - Version: %s\r\n%s\r\nAll Rights Reserved.", AppTitleA(), AppVersion(), AppCopyright()); }
	char const*    AppStringLine() { return pr::FmtX<struct Ldr, 128>("%s - Version: %s %s", AppTitleA(), AppVersion(), AppCopyright()); }

	// Returns the settings filepath to use (from the local executable directory)
	std::wstring UserSettingsFilePath()
	{
		wchar_t temp[MAX_PATH];
		GetModuleFileNameW(0, temp, MAX_PATH);
		std::wstring path = temp;
		pr::filesys::RmvExtension(path);
		path += L".ini";
		return path;
	}

	// Callback function for reading a point in world space
	// Used by the tool UIs to measure distances and angles
	pr::v4 __stdcall ReadPoint(void* ctx)
	{
		return static_cast<pr::Camera*>(ctx)->FocusPoint();
	}

	MainUI::MainUI(wchar_t const* cmdline, int)
		:Form(MakeFormParams<>()
			.name("ldr_main")
			.title(AppTitleW())
			.start_pos(EStartPosition::CentreParent)
			.menu(IDR_MENU_MAIN)
			.accel(IDR_ACCELERATOR)
			.icon(IDI_ICON_MAIN)
			.size_min(320,200)
			.padding(0)
			.wndclass(RegisterWndClass<MainUI>()))
		
		// App Settings
		,m_settings(UserSettingsFilePath(), true)

		// Main UI
		,m_status(StatusBar::Params<>().name("status-bar").parent(this_).dock(EDock::Bottom).parts({-1}).text(L"Idle").id(IDC_STATUSBAR_MAIN))
		,m_panel (Panel    ::Params<>().name("3d-scene")  .parent(this_).dock(EDock::Fill).margin(0).allow_drop())
		,m_recent_files()
		,m_saved_views()

		// 3D Scene
		,m_rdr(pr::rdr::RdrSettings(FALSE))
		,m_window(m_rdr, pr::rdr::WndSettings(m_panel, TRUE, FALSE, pr::To<pr::iv2>(m_panel.CreateHandle().ClientRect().size())))
		,m_scene(m_window,{pr::rdr::ERenderStep::ForwardRender})
		,m_cam(pr::v4(0, 0, 2.41421f, 1), pr::v4Origin, pr::v4YAxis) // 1/tan(tau/16)

		// Object Container
		,m_store()

		// Child windows/dialogs
		,m_store_ui(*this)
		,m_editor_ui(*this)
		,m_measure_tool_ui(*this, ReadPoint, &m_cam, m_rdr)
		,m_angle_tool_ui(*this, ReadPoint, &m_cam, m_rdr)
		,m_options_ui(this, m_settings)

		// Stock Objects
		,m_step_objects()
		,m_focus_point()
		,m_origin_point()
		,m_selection_box()
		,m_bbox_model()
		,m_test_model()
		,m_test_model_enable(false)

		// Modules
		,m_nav(m_cam, m_window.RenderTargetSize(), m_settings.m_CameraAlignAxis)
		,m_manip(m_cam, m_rdr)
		,m_lua_src()
		,m_sources(m_settings, m_rdr, m_store, m_lua_src)

		,m_bbox_scene(pr::BBoxReset)
		,m_ctrl_mode(EControlMode::Navigation)
		,m_input(&m_nav)
		,m_scene_rdr_pass(0)
		,m_mouse_status_updates(true)
		,m_suspend_render(false)
		,m_render_needed(false)
		,m_status_mgr(m_status)
	{
		// Create stock models such as the focus point, origin, selection box, etc
		CreateStockModels();

		// Initialise the recent files list
		m_recent_files.Attach(Menu::ByName(m_menu, L"&File,&Recent Files"), ID_FILE_RECENTFILES);
		m_recent_files.MaxLength(m_settings.m_MaxRecentFiles);
		m_recent_files.Import(m_settings.m_RecentFiles);
		m_recent_files.ListChanged += [&](MenuList&, EmptyArgs const&)
		{
			m_settings.m_RecentFiles = m_recent_files.Export();
			m_settings.Save();
		};
		m_recent_files.ItemClicked += [&](MenuList&, MenuList::Item const& item)
		{
			LoadScripts({item.m_name}, pr::KeyDown(VK_SHIFT));
		};

		// Initialise the saved views
		m_saved_views.Attach(Menu::ByName(m_menu, L"&Navigation,&Saved Views"), ID_NAV_SAVEDVIEWS);
		m_saved_views.MaxLength(m_settings.m_MaxSavedViews);
		m_saved_views.ItemClicked += [&](MenuList&, MenuList::Item const& item)
		{
			auto id = static_cast<Navigation::SavedViewID>((char*)item.m_tag - (char*)0);
			m_nav.RestoreView(id);
			RenderNeeded();
			UpdateUI();
		};

		// Initialise the script editor
		m_editor_ui.Text(m_settings.m_NewObjectString.c_str());
		m_editor_ui.Render += [&](ScriptEditorUI&, std::wstring const& script)
		{
			try
			{
				m_settings.m_NewObjectString = std::move(script);
				m_settings.Save();
				m_sources.AddString(m_settings.m_NewObjectString);
				RenderNeeded();
			}
			catch (std::exception const& ex)
			{
				pr::events::Send(Evt_AppMsg(pr::FmtS(L"Script error found while parsing source.\r\n%S", ex.what()), L"Render Script"));
			}
		};

		// Initialise the tools
		m_measure_tool_ui.MeasurementChanged += std::bind(&MainUI::RenderNeeded, this);
		m_angle_tool_ui.MeasurementChanged += std::bind(&MainUI::RenderNeeded, this);
		m_measure_tool_ui.VisibilityChanged += std::bind(&MainUI::UpdateUI, this);
		m_angle_tool_ui.VisibilityChanged += std::bind(&MainUI::UpdateUI, this);

		// Make the 3d-panel handle painting/resizing/mouse nav
		m_panel.Paint           += std::bind(&MainUI::Paint, this, _1, _2);
		m_panel.WindowPosChange += std::bind(&MainUI::Resize, this, _1, _2);
		m_panel.MouseButton     += std::bind(&MainUI::MouseButton, this, _1, _2); 
		m_panel.MouseMove       += std::bind(&MainUI::MouseMove, this, _1, _2);
		m_panel.MouseWheel      += std::bind(&MainUI::MouseWheel, this, _1, _2);
		m_panel.MouseClick      += std::bind(&MainUI::MouseClick, this, _1, _2);
		m_panel.Key             += std::bind(&MainUI::KeyEvent, this, _1, _2);
		m_panel.DropFiles       += std::bind(&MainUI::DropFiles, this, _1, _2);
		m_panel.AllowDrop(true);

		// Update the state of the UI
		UpdateUI();

		// Set the initial camera position
		ResetView(EObjectBounds::All);
		m_nav.CameraAlign(m_settings.m_CameraAlignAxis);
		m_nav.SetResetOrientation(m_settings.m_CameraResetForward, m_settings.m_CameraResetUp);

		// Register for drag drop
		AllowDrop(true);

		// Parse the command line
		pr::EnumCommandLine(cmdline, *this);
	}
	MainUI::~MainUI()
	{
		m_input->LostInputFocus(nullptr);
		m_input = nullptr;
		m_settings.Save();
	}

	// Run the application
	int MainUI::Run()
	{
		// Create a message loop and set it running
		pr::gui::SimMsgLoop loop;
		loop.AddMessageFilter(*this);

		// Add a step context for polling file state
		loop.AddStepContext("watch-files", [this](double)
		{
			// If file watching is turned on, look for changed files
			if (m_settings.m_WatchForChangedFiles)
				m_sources.RefreshChangedFiles();
		}, 10.0f, false);

		// Add a step context for 30Hz stepping
		loop.AddStepContext("step-30hz", [this](double s){ Step30Hz(s); }, 30.0f, true);

		// Add a step context to refresh the view
		loop.AddStepContext("refresh", [this](double s){ Step60Hz(s); }, 60.0f, true);

		#if 0
		loop.AddStepContext("debug", [this](double)
		{
			auto nc = NonClientAreas();
			auto h = {50,300,500,700,1000,700,500,300};
			static int i = 0;
			OutputDebugStringA(pr::FmtS("\n%s PreMoveWindow (%d,%d) %d %d\n", m_name, WindowRect().left, WindowRect().top, ClientRect().width(), ClientRect().height()));
			::MoveWindow(*this,
				2500,0,
				h.begin()[i]-nc.left+nc.right,
				h.begin()[i]-nc.top+nc.bottom, TRUE);
			OutputDebugStringA(pr::FmtS("%s PostMoveWindow (%d,%d) %d %d\n\n", m_name, WindowRect().left, WindowRect().top, ClientRect().width(), ClientRect().height()));
			++i %= h.size();
		}, 0.5f, false);
		#endif

		// Show the window, and pump the loop
		Show();
		return loop.Run();
	}

	// Handler timer messages
	void MainUI::Step30Hz(double)
	{
		// Check if timed-status text should disappear
		m_status_mgr.Update();
	}

	// Handler timer messages
	void MainUI::Step60Hz(double)
	{
		// Orbit the camera if enabled
		if (m_settings.m_CameraOrbit)
			m_nav.OrbitCamera(m_settings.m_CameraOrbitSpeed);

		// Refresh at 60Hz
		if (m_render_needed)
			Render();
	}

	// Reset the camera to view all, selected, or visible objects
	void MainUI::ResetView(EObjectBounds view_type)
	{
		// Reset the scene to view the bounding box
		m_nav.ResetView(GetSceneBounds(view_type));
	}

	// Render the 3D scene
	void MainUI::Paint(Control&, PaintEventArgs& args)
	{
		// Ignore render calls if the user settings say rendering is disabled
		args.m_handled = m_settings.m_RenderingEnabled;
		if (!args.m_handled)
			return;

		Render();
	}

	// Render the 3D scene
	void MainUI::Render()
	{
		m_render_needed = false;

		// Update the position/scale of the focus point
		if (m_settings.m_ShowFocusPoint)
		{
			auto scale = m_settings.m_FocusPointScale * m_nav.FocusDistance();
			m_focus_point.m_i2w = pr::m4x4::Scale(scale, m_nav.FocusPoint());
		}

		// Update the scale of the origin
		if (m_settings.m_ShowOrigin)
		{
			auto scale = m_settings.m_FocusPointScale * pr::Length3(m_cam.CameraToWorld().pos);
			m_origin_point.m_i2w = pr::m4x4::Scale(scale, pr::v4Origin);
		}

		// Allow the navigation manager to adjust the camera, ready for this frame
		m_nav.PositionCamera();

		// Set the camera view
		m_scene.SetView(m_cam);

		// Add objects to the viewport
		m_scene.ClearDrawlists();
		m_scene.UpdateDrawlists();

		// Render the scene
		m_scene_rdr_pass = 0;
		m_scene.Render();

		// Render wire frame over solid
		if (m_settings.m_GlobalFillMode == EFillMode::SolidAndWire)
		{
			m_scene_rdr_pass = 1;
			m_scene.Render();
		}

		m_window.Present();
	}

	// Request a render.
	void MainUI::RenderNeeded()
	{
		// Note: this can be called many times per frame with minimal cost
		m_panel.Invalidate();
		m_render_needed = true;
	}

	// Enable/Disable full screen mode
	void MainUI::FullScreenMode(bool enable_fullscreen)
	{
		if (enable_fullscreen)
		{
			// Hide the menu and status bar so that the client area is calculated correctly.
			m_menu = MenuStrip();
			MenuStrip(nullptr);
			m_status.Visible(false);

			//todo, make this correct
			pr::rdr::SystemConfig config;
			pr::rdr::SystemConfig::ModeCont modes;
			config.m_adapters[0].m_outputs[0].GetDisplayModes(DXGI_FORMAT_R8G8B8A8_UNORM, modes);

			// Get the full screen display mode from the settings
			pr::rdr::DisplayMode mode(1920, 1080);
			m_window.FullScreenMode(true, mode);
		}
		else
		{
			pr::rdr::DisplayMode mode;
			m_window.FullScreenMode(false, mode);

			// Show the status and menu controls again
			MenuStrip(m_menu);
			m_status.Visible(true);
		}
	}

	// The size of the window has changed
	void MainUI::Resize(Control&, WindowPosEventArgs const& args)
	{
		if (!args.m_before && args.IsResize() && !args.Iconic())
		{
			auto area = args.ParentRect();
			auto size = pr::To<pr::iv2>(area.size());
			if (area.area() <= 0)
				return;

			// Change the render target size
			m_window.RenderTargetSize(size);

			// Adjust the viewport
			m_scene.m_viewport.TopLeftX = float(area.left);
			m_scene.m_viewport.TopLeftY = float(area.top );
			m_scene.m_viewport.Width    = float(area.width());
			m_scene.m_viewport.Height   = float(area.height());

			// Update the camera
			m_cam.Aspect(area.aspect());

			m_nav.ViewSize(size);
			m_settings.Save();
		}
	}

	// Mouse/Key navigation/manipulation
	// Convert screen space to normalised screen space
	inline pr::v2 ToNormSS(pr::v2 const& pt_ss, pr::iv2 const& view_size)
	{
		auto view = pr::IRect(pr::iv2Zero, view_size);
		return pr::NormalisePoint(view, pt_ss, 1.0f, -1.0f);
	}
	void MainUI::MouseButton(Control&, MouseEventArgs& args)
	{
		// Capture the mouse on mouse down
		if (args.m_down)
			::SetCapture(m_panel);
		else
			::ReleaseCapture();

		// Get the button pressed and the location
		auto op = pr::camera::MouseBtnToNavOp(int(args.m_button));
		auto mouse_loc = pr::To<pr::v2>(args.m_point);
		auto pt = ToNormSS(mouse_loc, m_nav.ViewSize());

		// Forward the mouse input to the input handler
		if (m_input->MouseInput(pt, args.m_down ? op : pr::camera::ENavOp::None, true))
		{
			RenderNeeded();
			args.m_handled = true;
		}

		MouseStatusUpdate(mouse_loc);
	}
	void MainUI::MouseMove(Control&, MouseEventArgs& args)
	{
		// Get the button pressed and the location
		auto op = pr::camera::MouseBtnToNavOp(int(args.m_button));
		auto mouse_loc = pr::To<pr::v2>(args.m_point);
		auto pt = ToNormSS(mouse_loc, m_nav.ViewSize());

		if (m_input->MouseInput(pt, op, false))
		{
			args.m_handled = true;
			Render(); // Render directly, for nice smooth scrolling
		}

		MouseStatusUpdate(mouse_loc);
	}
	void MainUI::MouseClick(Control&, MouseEventArgs& args)
	{
		// Get the button pressed and the location
		auto op = pr::camera::MouseBtnToNavOp(int(args.m_button));
		auto mouse_loc = pr::To<pr::v2>(args.m_point);
		auto pt = ToNormSS(mouse_loc, m_nav.ViewSize());

		// Forward the mouse input to the input handler
		if (m_input->MouseClick(pt, op))
		{
			RenderNeeded();
			args.m_handled = true;
		}

		MouseStatusUpdate(mouse_loc);
	}
	void MainUI::MouseWheel(Control&, MouseWheelArgs& args)
	{
		// Get the button pressed and the location
		auto mouse_loc = pr::To<pr::v2>(args.m_point);
		auto pt = ToNormSS(mouse_loc, m_nav.ViewSize());

		// Delta is '1.0f' for a single wheel click
		if (m_input->MouseWheel(pt, args.m_delta/120.0f))
		{
			args.m_handled = true;
			Render(); // Render directly, for nice smooth scrolling
		}

		MouseStatusUpdate(mouse_loc);
	}
	void MainUI::KeyEvent(Control&, KeyEventArgs& args)
	{
		// Forward key presses to the input handler
		if (m_input->KeyInput(args.m_vk_key, args.m_down, args.m_flags, args.m_repeats))
			args.m_handled = true;
	}

	// Default main menu handler
	// 'item_id' - the menu item id or accelerator id
	// 'event_source' - 0 = menu, 1 = accelerator, 2 = control-defined notification code
	// 'ctrl_hwnd' - the control that sent the notification. Only valid when src == 2
	// Typically you'll only need 'menu_item_id' unless your accelerator ids
	// overlap your menu ids, in which case you'll need to check 'event_source'
	bool MainUI::HandleMenu(UINT item_id, UINT event_source, HWND ctrl_hwnd)
	{
		if (Form::HandleMenu(item_id, event_source, ctrl_hwnd))
			return true;

		switch (item_id)
		{
		case ID_FILE_NEW:
		case ID_ACCELERATOR_FILENEW:
			#pragma region
			{
				OpenScriptEditor();
				return true;
			}
			#pragma endregion
		case ID_FILE_NEWSCRIPT:
		case ID_ACCELERATOR_FILENEWSCRIPT:
			#pragma region
			{
				CreateNewScript();
				return true;
			}
			#pragma endregion
		case ID_FILE_OPEN:
		case ID_ACCELERATOR_FILEOPEN:
			#pragma region
			{
				LoadScripts({}, false);
				return true;
			}
			#pragma endregion
		case ID_FILE_ADDITIVEOPEN:
		case ID_ACCELERATOR_FILEOPEN_ADDITIVE:
			#pragma region
			{
				LoadScripts({}, true); 
				return true;
			}
			#pragma endregion
		case ID_RENDERING_WIREFRAME:
		case ID_ACCELERATOR_WIREFRAME:
			#pragma region
			{
				// Cycle through solid, wireframe, and solid+wire
				int mode = (int(m_settings.m_GlobalFillMode) + 1) % int(EFillMode::NumberOf);
				m_settings.m_GlobalFillMode = static_cast<EFillMode>(mode);
				RenderNeeded();
				UpdateUI();
				return true;
			}
			#pragma endregion
		case ID_DATA_EDITSOURCEFILES:
		case ID_ACCELERATOR_EDITOR:
			#pragma region
			{
				// Open an external text editor with the source files
				StrList files;
				for (auto& f : m_sources.List())
					files.push_back(f.second.m_filepath);

				OpenExternalTextEditor(files);
				return true;
			}
			#pragma endregion
		case ID_NAV_SETCAMERAPOSITION:
		case ID_ACCELERATOR_CAMERAPOS:
			#pragma region
			{
				// Set the position of the camera
				pr::camera::PositionUI dlg(*this, m_cam);
				if (dlg.ShowDialog(this) != EDialogResult::Ok)
					return true;

				m_cam = dlg.m_cam;
				RenderNeeded();
				return true;
			}
			#pragma endregion
		case ID_RENDERING_LIGHTING:
		case ID_ACCELERATOR_LIGHTING_DLG:
			#pragma region
			{
				ShowLightingUI();
				return true;
			}
			#pragma endregion
		case ID_NAV_RESETVIEW_ALL:
		case ID_NAV_RESETVIEW_SELECTED:
		case ID_NAV_RESETVIEW_VISIBLE:
			#pragma region
			{
				// Reset the view to all, selected, or visible objects
				ResetView(
					item_id == ID_NAV_RESETVIEW_VISIBLE ? EObjectBounds::Visible :
					item_id == ID_NAV_RESETVIEW_SELECTED ? EObjectBounds::Selected :
					EObjectBounds::All);
				RenderNeeded();
				return true;
			}
			#pragma endregion
		case ID_NAV_ALIGN_NONE:
		case ID_NAV_ALIGN_X:
		case ID_NAV_ALIGN_Y:
		case ID_NAV_ALIGN_Z:
		case ID_NAV_ALIGN_CURRENT:
			#pragma region
			{
				// Align the camera to the selected axis
				m_nav.CameraAlign(
					item_id == ID_NAV_ALIGN_NONE ? pr::v4Zero  :
					item_id == ID_NAV_ALIGN_X    ? pr::v4XAxis :
					item_id == ID_NAV_ALIGN_Y    ? pr::v4YAxis :
					item_id == ID_NAV_ALIGN_Z    ? pr::v4ZAxis :
					m_nav.CameraToWorld().y);
				m_settings.m_CameraAlignAxis = m_nav.CameraAlign();
				RenderNeeded();
				UpdateUI();
				return true;
			}
			#pragma endregion
		case ID_NAV_VIEW_AXIS_POSX:
		case ID_NAV_VIEW_AXIS_NEGX:
		case ID_NAV_VIEW_AXIS_POSY:
		case ID_NAV_VIEW_AXIS_NEGY:
		case ID_NAV_VIEW_AXIS_POSZ:
		case ID_NAV_VIEW_AXIS_NEGZ:
		case ID_NAV_VIEW_AXIS_POSXYZ:
			#pragma region
			{
				CamForwardAxis(
					item_id == ID_NAV_VIEW_AXIS_POSX   ?  pr::v4XAxis :
					item_id == ID_NAV_VIEW_AXIS_NEGX   ? -pr::v4XAxis :
					item_id == ID_NAV_VIEW_AXIS_POSY   ?  pr::v4YAxis :
					item_id == ID_NAV_VIEW_AXIS_NEGY   ? -pr::v4YAxis :
					item_id == ID_NAV_VIEW_AXIS_POSZ   ?  pr::v4ZAxis :
					item_id == ID_NAV_VIEW_AXIS_NEGZ   ? -pr::v4ZAxis :
					-pr::v4(0.577350f, 0.577350f, 0.577350f, 0.0f));
				return true;
			}
			#pragma endregion
		case ID_NAV_CLEARSAVEDVIEWS:
			#pragma region
			{
				m_nav.ClearSavedViews();
				m_saved_views.Clear();
				return true;
			}
			#pragma endregion
		case ID_NAV_SAVEVIEW:
			#pragma region
			{
				// Record the current camera position as a saved camera view
				TextEntryUI dlg(*this, L"Label for this view", pr::FmtS(L"view%d", m_saved_views.Items().size()), false);
				if (dlg.ShowDialog(this) != EDialogResult::Ok)
					return true;

				auto id = m_nav.SaveView();
				m_saved_views.Add(pr::Widen(dlg.m_body).c_str(), (char*)0 + id, false, true);
				return true;
			}
			#pragma endregion
		case ID_NAV_SETFOCUSPOSITION:
			#pragma region
			{
				ShowFocusPositionUI();
				return true;
			}
			#pragma endregion
		case ID_NAV_ORBIT:
			#pragma region
			{
				// Toggle camera orbit mode
				m_settings.m_CameraOrbit = !m_settings.m_CameraOrbit;
				m_nav.OrbitCamera(0.0f);
				UpdateUI();
				return true;
			}
			#pragma endregion
		case ID_DATA_OBJECTMANAGER:
			#pragma region
			{
				ShowObjectManagerUI();
				return true;
			}
			#pragma endregion
		case ID_DATA_CLEARSCENE:
			#pragma region
			{
				// Remove all objects from the object manager
				m_store.clear();
				RenderNeeded();
				return true;
			}
			#pragma endregion
		case ID_DATA_AUTOREFRESH:
			#pragma region
			{
				// Toggle auto refresh file sources
				m_settings.m_WatchForChangedFiles = !m_settings.m_WatchForChangedFiles;
				UpdateUI();
				return true;
			}
			#pragma endregion
		case ID_DATA_CREATE_DEMO_SCENE:
			#pragma region
			{
				// Generate a self created scene of objects
				CreateDemoScene();
				ResetView(EObjectBounds::All);
				RenderNeeded();
				return true;
			}
			#pragma endregion
		case ID_RENDERING_SHOWFOCUS:
			#pragma region
			{
				// Toggle visibility of the focus point
				m_settings.m_ShowFocusPoint = !m_settings.m_ShowFocusPoint;
				RenderNeeded();
				UpdateUI();
				return true;
			}
			#pragma endregion
		case ID_RENDERING_SHOWORIGIN:
			#pragma region
			{
				// Toggle visibility of the origin point
				m_settings.m_ShowOrigin = !m_settings.m_ShowOrigin;
				RenderNeeded();
				UpdateUI();
				return true;
			}
			#pragma endregion
		case ID_RENDERING_SHOWSELECTION:
			#pragma region
			{
				// Toggle visibility of the selection box
				m_settings.m_ShowSelectionBox = !m_settings.m_ShowSelectionBox;
				RenderNeeded();
				UpdateUI();
				return true;
			}
			#pragma endregion
		case ID_RENDERING_SHOWOBJECTBBOXES:
			#pragma region
			{
				// Toggle visibility of the object space bounding boxes
				m_settings.m_ShowObjectBBoxes = !m_settings.m_ShowObjectBBoxes;
				RenderNeeded();
				UpdateUI();
				return true;
			}
			#pragma endregion
		case ID_RENDERING_RENDER2D:
			#pragma region
			{
				// Toggle between perspective and orthographic
				m_nav.Render2D(!m_nav.Render2D());
				RenderNeeded();
				UpdateUI();
				return true;
			}
			#pragma endregion
		case ID_RENDERING_TECHNIQUE:
			#pragma region
			{
				// Toggle between forward and deferred rendering
				auto fwd = {pr::rdr::ERenderStep::ForwardRender};
				auto def = {pr::rdr::ERenderStep::GBuffer, pr::rdr::ERenderStep::DSLighting};
		
				if (m_scene.FindRStep<pr::rdr::ForwardRender>() != nullptr)
					m_scene.SetRenderSteps(pr::rdr::Scene::DeferredRendering());
				else
					m_scene.SetRenderSteps(pr::rdr::Scene::ForwardRendering());

				RenderNeeded();
				UpdateUI();
				return true;
			}
			#pragma endregion
		case ID_TOOLS_MEASURE:
			#pragma region
			{
				// Display the measure tool UI
				m_measure_tool_ui.Visible(true);
				return true;
			}
			#pragma endregion
		case ID_TOOLS_ANGLE:
			#pragma region
			{
				// Display the angle tool UI
				m_angle_tool_ui.Visible(true);
				return true;
			}
			#pragma endregion
		case ID_TOOLS_MOVE:
			#pragma region
			{
				// Toggle between navigation and manipulation mode
				auto turn_on = ControlMode() != EControlMode::Manipulation;
				ControlMode(turn_on ? EControlMode::Manipulation : EControlMode::Navigation);
				UpdateUI();
				return true;
			}
			#pragma endregion
		case ID_TOOLS_OPTIONS:
			#pragma region
			{
				ShowOptionsUI();
				return true;
			}
			#pragma endregion
		case ID_WINDOW_ALWAYSONTOP:
			#pragma region
			{
				// Set the window draw order so that this window is always on top
				m_settings.m_AlwaysOnTop = !m_settings.m_AlwaysOnTop;
				TopMost(m_settings.m_AlwaysOnTop);
				UpdateUI();
				return true;
			}
			#pragma endregion
		case ID_WINDOW_BACKGROUNDCOLOUR:
			#pragma region
			{
				// Set the background colour
				pr::gui::ColourUI dlg(*this, m_settings.m_BackgroundColour);
				if (dlg.ShowDialog(this) != EDialogResult::Ok)
					return true;

				m_settings.m_BackgroundColour = dlg.m_colour.a0();
				RenderNeeded();
				return true;
			}
			#pragma endregion
		case ID_WINDOW_EXAMPLESCRIPT:
			#pragma region
			{
				// Show a window containing the demo scene script
				m_editor_ui.Text(pr::ldr::CreateDemoScene().c_str());
				m_editor_ui.Visible(true);
				return true;
			}
			#pragma endregion
		case ID_WINDOW_CHECKFORUPDATES:
			#pragma region
			{
				CheckForUpdates();
				return true;
			}
			#pragma endregion
		case ID_WINDOW_ABOUTLINEDRAWER:
			#pragma region
			{
				// Display the about dialog box
				AboutUI().ShowDialog(this);
				return true;
			}
			#pragma endregion
		case ID_ACCELERATOR_RELOAD:
			#pragma region
			{
				ReloadSourceData();
				RenderNeeded();
				return true;
			}
			#pragma endregion
		case ID_ACCELERATOR_RESETVIEW:
			#pragma region
			{
				ResetView(EObjectBounds::All);
				RenderNeeded();
				return true;
			}
			#pragma endregion
		case ID_ACCELERATOR_SHOWOBJECTSUI:
			#pragma region
			{
				m_store_ui.Show();
				m_store_ui.Populate(m_store);
				return true;
			}
			#pragma endregion
		}
		return false;
	}

	// Create a new text file for ldr script
	void MainUI::CreateNewScript(std::wstring filepath)
	{
		try
		{
			// Prompt for a file name if none given
			if (filepath.empty())
			{
				COMDLG_FILTERSPEC const filters[] =
				{
					{ L"Ldr Script (*.ldr)" , L"*.ldr" },
					{ L"Lua Script (*.lua)" , L"*.lua" },
					{ L"DirectX Files (*.x)" , L"*.x" },
					{ L"All Files (*.*)", L"*.*" }
				};

				filepath = SaveFileUI(*this, FileUIOptions(L"ldr", filters, _countof(filters)));
				if (filepath.empty())
					return;
			}

			// Create a new blank file
			std::ofstream(filepath.c_str(), std::ofstream::out).close();

			// Add the blank file to the file sources
			LoadScripts({filepath}, false);

			// Display the blank file in an external text editor
			StrList list;
			list.push_back(filepath);
			OpenExternalTextEditor(list);
		}
		catch (std::exception const& ex)
		{
			pr::events::Send(Evt_AppMsg(pr::FmtS(L"Creating a new script failed.\r\n%S", ex.what()), L"Create New Script"));
		}
	}

	// Add a file to the file sources
	void MainUI::LoadScripts(std::vector<std::wstring> filepaths, bool additive)
	{
		try
		{
			// Prompt for a filepath in none given
			if (filepaths.empty())
			{
				COMDLG_FILTERSPEC const filters[] =
				{
					{ L"Ldr Script (*.ldr)" , L"*.ldr" },
					{ L"Lua Script (*.lua)" , L"*.lua" },
					{ L"DirectX Files (*.x)" , L"*.x" },
					{ L"All Files (*.*)", L"*.*" }
				};

				filepaths = OpenFileUI(m_hwnd, FileUIOptions(L"ldr", filters, _countof(filters)));
				if (filepaths.empty())
					return;
			}

			// Add the files to the recent files list
			for (auto& fp : filepaths)
				m_recent_files.Add(fp.c_str(), true);

			// Clear data from other files, unless this is an additive open
			if (!additive)
				m_sources.Clear();

			// Add the files to the source
			for (auto& fp : filepaths)
				m_sources.AddFile(fp.c_str());

			// Reset the camera if flagged
			if (m_settings.m_ResetCameraOnLoad)
				ResetView(EObjectBounds::All);

			// Set the window title
			pr::gui::string title = AppTitleW();
			if (!filepaths.empty())
			{
				title += L" - ";
				title += filepaths.front();
			}
			Text(title);

			// Refresh
			RenderNeeded();
		}
		catch (std::exception const& ex)
		{
			pr::events::Send(Evt_AppMsg(pr::FmtS(L"Script error found while parsing source.\r\n%S", ex.what()), L"Load Script"));
		}
	}

	// Reload all data
	void MainUI::ReloadSourceData()
	{
		try
		{
			m_sources.Reload();
		}
		catch (LdrException const& ex)
		{
			if (ex.code() == ELdrException::OperationCancelled)
				pr::events::Send(Evt_Status(L"Reloading data cancelled", 2000));
			else
				pr::events::Send(Evt_AppMsg(pr::FmtS(L"Error found while reloading source data.\r\n%S", ex.what()), L"Reload Failed"));
		}
	}

	// Open the built-in script editor
	void MainUI::OpenScriptEditor()
	{
		m_editor_ui.Visible(true);
	}

	// Open a text editor with the given files
	void MainUI::OpenExternalTextEditor(StrList const& files)
	{
		try
		{
			// If no path to a text editor is provided, ignore the command
			auto cmd = m_settings.m_TextEditorCmd;
			if (cmd.empty())
				throw std::exception("Text editor not provided. Check options");

			// Build the command line string
			for (auto& file : files)
				cmd.append(L" \"").append(file).append(L"\"");

			// Launch the text editor as a new process
			PROCESS_INFORMATION proc_info;
			auto close_handles = pr::CreateScope([]{}, [&]
			{
				CloseHandle(proc_info.hThread);
				CloseHandle(proc_info.hProcess);
			});

			// Launch the text editor in a new process
			STARTUPINFOW suinfo = {sizeof(STARTUPINFOW)};
			if (CreateProcessW(nullptr, &cmd[0], 0, 0, FALSE, NORMAL_PRIORITY_CLASS, 0, 0, &suinfo, &proc_info) == FALSE)
				throw std::exception(pr::FmtS("Failed to start text editor: '%s'", cmd.c_str()));
		}
		catch (std::exception const& ex)
		{
			pr::events::Send(Evt_AppMsg(pr::FmtS(L"No Text Editor available.\r\n%S", ex.what()), L"Open Editor Failed"));
		}
	}

	// Get/Set the navigation/manipulation mode
	EControlMode MainUI::ControlMode() const
	{
		return m_ctrl_mode;
	}
	void MainUI::ControlMode(EControlMode mode)
	{
		if (m_ctrl_mode == mode)
			return;

		m_ctrl_mode = mode;

		// Switch input handler
		IInputHandler* new_handler = nullptr;
		switch (m_ctrl_mode)
		{
		default:
			assert(false && "Unknown control mode");
			break;
		case EControlMode::Navigation:
			new_handler = &m_nav;
			break;
		case EControlMode::Manipulation:
			new_handler = &m_manip;
			break;
		}
		m_input->LostInputFocus(new_handler);
		new_handler->GainInputFocus(m_input);
		m_input = new_handler;
	}

	// View the current focus point looking down the selected axis
	void MainUI::CamForwardAxis(pr::v4 const& fwd)
	{
		// axis = m_nav.CameraToWorld().z; use this for non-menu option
		auto c2w   = m_nav.CameraToWorld();
		auto focus = m_nav.FocusPoint();
		auto cam   = focus + fwd * m_nav.FocusDistance();
		auto up    = pr::Parallel(fwd, c2w.y) ? pr::Cross3(fwd, c2w.x) : c2w.y;
		m_nav.LookAt(cam, focus, up);

		m_settings.m_CameraResetForward = fwd;
		m_settings.m_CameraResetUp = up;
		m_nav.SetResetOrientation(m_settings.m_CameraResetForward, m_settings.m_CameraResetUp);

		RenderNeeded();
	}

	// Set the position of the camera focus point in world space
	void MainUI::ShowFocusPositionUI()
	{
		// Prompt for the focus point
		TextEntryUI dlg(m_hwnd, L"Enter focus point position", L"0 0 0", false);
		if (dlg.ShowDialog(this) != EDialogResult::Ok)
			return;

		try
		{
			auto pos = pr::To<pr::v4>(dlg.m_body, 1.0f);
			m_nav.FocusPoint(pos);
			RenderNeeded();
		}
		catch (std::exception const& ex)
		{
			pr::events::Send(Evt_AppMsg(pr::FmtS(L"Format incorrect. Focus point not set.\r\n%S", ex.what()), L"Set Focus Point"));
		}
	}

	// Display the lighting options UI
	void MainUI::ShowLightingUI()
	{
		using Light = pr::rdr::Light;

		// Preserve the current light settings
		Light prev_light   = m_settings.m_Light;
		auto pv = [&](Light const& light)
		{
			m_settings.m_Light = light;
			RenderNeeded();
		};

		// Show the lighting options UI
		pr::rdr::LightingUI dlg(*this, m_settings.m_Light, pv);
		if (dlg.ShowDialog(this) == EDialogResult::Ok)
		{
			// Save the new options
			m_settings.m_Light = dlg.m_light;
		}
		else
		{
			// Restore the old light settings
			m_settings.m_Light = prev_light;
		}

		// Refresh
		RenderNeeded();
	}

	// Display the object manager UI
	void MainUI::ShowObjectManagerUI()
	{
		m_store_ui.Visible(true);
		m_store_ui.Populate(m_store);
	}

	// Display the options UI
	void MainUI::ShowOptionsUI()
	{
		m_options_ui.Visible(true);
	}

	// Check the web for the latest version
	void MainUI::CheckForUpdates()
	{
		std::string version;
		pr::network::WebGet("http://www.rylogic.co.nz/latest_versions.html", version);

		try
		{
			auto root = pr::xml::Load(version.c_str(), version.size());
			(void)root;
		}
		catch (std::exception const& ex)
		{
			pr::events::Send(Evt_AppMsg(pr::FmtS(L"Version information unavailable.\r\n%S", ex.what()), L"Check for Updates"));
		}
	}

	// Create stock models such as the focus point, origin, etc
	void MainUI::CreateStockModels()
	{
		using namespace pr::rdr;
		{
			// Create the focus point models
			static pr::v4 verts_[] = // Work around for VS2015 alignment bug
			{
				pr::v4(0.0f,  0.0f,  0.0f, 1.0f),
				pr::v4(1.0f,  0.0f,  0.0f, 1.0f),
				pr::v4(0.0f,  0.0f,  0.0f, 1.0f),
				pr::v4(0.0f,  1.0f,  0.0f, 1.0f),
				pr::v4(0.0f,  0.0f,  0.0f, 1.0f),
				pr::v4(0.0f,  0.0f,  1.0f, 1.0f),
			};
			const unsigned short indices_[] =
			{
				0, 1, 2, 3, 4, 5,
			};
			const NuggetProps nuggets_[] =
			{
				NuggetProps(EPrim::LineList, EGeom::Vert|EGeom::Colr),
			};
			const pr::Colour32 fp_cols[] = { 0xFFFF0000, 0xFFFF0000, 0xFF00FF00, 0xFF00FF00, 0xFF0000FF, 0xFF0000FF };
			const pr::Colour32 op_cols[] = { 0xFF800000, 0xFF800000, 0xFF008000, 0xFF008000, 0xFF000080, 0xFF000080 };
			
			auto cdata = pr::rdr::MeshCreationData()
				.verts(verts_, _countof(verts_))
				.indices(indices_, _countof(indices_))
				.nuggets(nuggets_, _countof(nuggets_));

			cdata.colours(fp_cols, _countof(fp_cols));
			m_focus_point .m_model = ModelGenerator<>::Mesh(m_rdr, cdata);
			m_focus_point .m_model->m_name = "focus point";
			m_focus_point .m_i2w   = pr::m4x4Identity;

			cdata.colours(op_cols, _countof(op_cols));
			m_origin_point.m_model = ModelGenerator<>::Mesh(m_rdr, cdata);
			m_origin_point.m_model->m_name = "origin point";
			m_origin_point.m_i2w   = pr::m4x4Identity;
		}
		{
			// Create the selection box model
			static pr::v4 verts_[] = // Work around for VS2015 alignment bug
			{
				pr::v4(-0.5f, -0.5f, -0.5f, 1.0f), pr::v4(-0.4f, -0.5f, -0.5f, 1.0f), pr::v4(-0.5f, -0.4f, -0.5f, 1.0f), pr::v4(-0.5f, -0.5f, -0.4f, 1.0f),
				pr::v4( 0.5f, -0.5f, -0.5f, 1.0f), pr::v4( 0.5f, -0.4f, -0.5f, 1.0f), pr::v4( 0.4f, -0.5f, -0.5f, 1.0f), pr::v4( 0.5f, -0.5f, -0.4f, 1.0f),
				pr::v4( 0.5f,  0.5f, -0.5f, 1.0f), pr::v4( 0.4f,  0.5f, -0.5f, 1.0f), pr::v4( 0.5f,  0.4f, -0.5f, 1.0f), pr::v4( 0.5f,  0.5f, -0.4f, 1.0f),
				pr::v4(-0.5f,  0.5f, -0.5f, 1.0f), pr::v4(-0.5f,  0.4f, -0.5f, 1.0f), pr::v4(-0.4f,  0.5f, -0.5f, 1.0f), pr::v4(-0.5f,  0.5f, -0.4f, 1.0f),
				pr::v4(-0.5f, -0.5f,  0.5f, 1.0f), pr::v4(-0.4f, -0.5f,  0.5f, 1.0f), pr::v4(-0.5f, -0.4f,  0.5f, 1.0f), pr::v4(-0.5f, -0.5f,  0.4f, 1.0f),
				pr::v4( 0.5f, -0.5f,  0.5f, 1.0f), pr::v4( 0.5f, -0.4f,  0.5f, 1.0f), pr::v4( 0.4f, -0.5f,  0.5f, 1.0f), pr::v4( 0.5f, -0.5f,  0.4f, 1.0f),
				pr::v4( 0.5f,  0.5f,  0.5f, 1.0f), pr::v4( 0.4f,  0.5f,  0.5f, 1.0f), pr::v4( 0.5f,  0.4f,  0.5f, 1.0f), pr::v4( 0.5f,  0.5f,  0.4f, 1.0f),
				pr::v4(-0.5f,  0.5f,  0.5f, 1.0f), pr::v4(-0.5f,  0.4f,  0.5f, 1.0f), pr::v4(-0.4f,  0.5f,  0.5f, 1.0f), pr::v4(-0.5f,  0.5f,  0.4f, 1.0f),
			};
			const unsigned short indices_[] =
			{
				0,  1,  0,  2,  0,  3,
				4,  5,  4,  6,  4,  7,
				8,  9,  8, 10,  8, 11,
				12, 13, 12, 14, 12, 15,
				16, 17, 16, 18, 16, 19,
				20, 21, 20, 22, 20, 23,
				24, 25, 24, 26, 24, 27,
				28, 29, 28, 30, 28, 31,
			};
			const NuggetProps nuggets_[] =
			{
				NuggetProps(EPrim::LineList, EGeom::Vert),
			};
			auto cdata = pr::rdr::MeshCreationData()
				.verts(verts_, _countof(verts_))
				.indices(indices_, _countof(indices_))
				.nuggets(nuggets_, _countof(nuggets_));
			m_selection_box.m_model = ModelGenerator<>::Mesh(m_rdr, cdata);
			m_selection_box.m_model->m_name = "selection box";
			m_selection_box.m_i2w   = pr::m4x4Identity;
		}
		{
			// Create a bounding box model
			static pr::v4 verts_[] = // Work around for VS2015 alignment bug
			{
				pr::v4(-0.5f, -0.5f, -0.5f, 1.0f),
				pr::v4(+0.5f, -0.5f, -0.5f, 1.0f),
				pr::v4(+0.5f, +0.5f, -0.5f, 1.0f),
				pr::v4(-0.5f, +0.5f, -0.5f, 1.0f),
				pr::v4(-0.5f, -0.5f, +0.5f, 1.0f),
				pr::v4(+0.5f, -0.5f, +0.5f, 1.0f),
				pr::v4(+0.5f, +0.5f, +0.5f, 1.0f),
				pr::v4(-0.5f, +0.5f, +0.5f, 1.0f),
			};
			const unsigned short indices_[] =
			{
				0, 1, 1, 2, 2, 3, 3, 0,
				4, 5, 5, 6, 6, 7, 7, 4,
				0, 4, 1, 5, 2, 6, 3, 7,
			};
			const pr::Colour32 colours_[] =
			{
				pr::Colour32Blue,
			};
			const NuggetProps nuggets_[] =
			{
				NuggetProps(EPrim::LineList),
			};

			auto cdata = pr::rdr::MeshCreationData()
				.verts(verts_, _countof(verts_))
				.indices(indices_, _countof(indices_))
				.colours(colours_, _countof(colours_))
				.nuggets(nuggets_, _countof(nuggets_));
			m_bbox_model.m_model = ModelGenerator<>::Mesh(m_rdr, cdata);
			m_bbox_model.m_model->m_name = "bbox";
			m_bbox_model.m_i2w   = pr::m4x4Identity;
		}
		{
			// Create a test point box model
			m_test_model.m_model = ModelGenerator<>::Box(m_rdr, 0.1f, pr::m4x4Identity, pr::Colour32Green);
			m_test_model.m_model->m_name = "test model";
			m_test_model.m_i2w   = pr::m4x4Identity;
		}
	}

	// Return the bounding box of objects in the current scene for the given bounds type
	pr::BBox MainUI::GetSceneBounds(EObjectBounds bound_type) const
	{
		pr::BBox bbox;
		switch (bound_type)
		{
		default:
			{
				PR_ASSERT(PR_DBG_LDR, false, "Unknown view type");
				bbox = pr::BBoxUnit;
				break;
			}
		case EObjectBounds::All:
			{
				// Update the scene bounding box if out of date
				if (m_bbox_scene == pr::BBoxReset)
				{
					for (auto& obj : m_store)
					{
						auto bb = obj->BBoxWS(true);
						if (!bb.empty())
							pr::Encompass(m_bbox_scene, bb);
					}
				}
				bbox = m_bbox_scene;
				break;
			}
		case EObjectBounds::Selected:
			{
				bbox = pr::BBoxReset;
				int iter = -1;
				for (auto obj = m_store_ui.EnumSelected(iter); obj; obj = m_store_ui.EnumSelected(iter))
				{
					auto bb = obj->BBoxWS(true);
					if (!bb.empty())
						pr::Encompass(bbox, bb);
				}
				break;
			}
		case EObjectBounds::Visible:
			{
				bbox = pr::BBoxReset;
				for (auto& obj : m_store)
				{
					obj->Apply([&](pr::ldr::LdrObject* o)
					{
						auto bb = o->BBoxWS(false);
						if (!bb.empty()) pr::Encompass(bbox, bb);
						return true;
					}, "");
				}
				break;
			}
		}
		return !bbox.empty() ? bbox : pr::BBoxUnit;
	}

	// Generate a scene containing the supported line drawer objects.
	void MainUI::CreateDemoScene()
	{
		try
		{
			// Create a standard renderer demo scene
			m_sources.AddString(pr::ldr::CreateDemoScene());

			using namespace pr;
			using namespace pr::rdr;

			// For testing..
			#if 0
				//pr::v4 lines[] =
				//{
				//	pr::v4::make(0,-1,0,1), pr::v4::make(0,1,0,1),
				//	pr::v4::make(-1,0,0,1), pr::v4::make(1,0,0,1),
				//};
				auto gditex = m_rdr.m_tex_mgr.CreateTextureGdi(AutoId, 512,512, "test");
				auto rt = gditex->GetD2DRenderTarget();
				
				rt->BeginDraw();
				D3DPtr<ID2D1SolidColorBrush> bsh;
				rt->Clear(D2D1::ColorF(D2D1::ColorF::Enum::DarkGreen));
				pr::Throw(rt->CreateSolidColorBrush(D2D1::ColorF(1.0f,1.0f,0.0f), &bsh.m_ptr));
				rt->FillEllipse(D2D1::Ellipse(D2D1::Point2F(256,256),200,100), bsh.m_ptr);
				rt->EndDraw();

				NuggetProps mat;
				mat.m_tex_diffuse = gditex;//m_rdr.m_tex_mgr.FindTexture(EStockTexture::Checker);//
				auto model = ModelGenerator<>::Quad(m_rdr, 2, 2, iv2Zero, Colour32White, &mat);
				m_test_model.m_model = model;
				m_test_model_enable = true;
			#endif
			#if 0
				if (m_scene.FindRStep<ShadowMap>() == nullptr)
					m_scene.m_render_steps.insert(begin(m_scene.m_render_steps), std::make_shared<ShadowMap>(m_scene, m_scene.m_global_light, pr::iv2::make(1024,1024)));

				pr::ldr::AddString(m_rdr, "*Rect r FF00FF00 {3 1 *Solid}", m_store, pr::ldr::DefaultContext, false, 0, &m_lua_src);
				m_nav.m_camera.LookAt(pr::v4Origin, pr::v4::make(0,0,2,1), pr::v4YAxis);

				auto thick_line = m_rdr.m_shdr_mgr.FindShader(EStockShader::ThickLineListGS);

				NuggetProps mat;
				mat.m_sset.push_back(thick_line);
			
				//std::vector<pr::v4> lines;
				//pr::Spline s = pr::Spline::make(pr::v4Origin, pr::v4XAxis.w1, pr::v4YAxis.w1, pr::v4Origin);
				//pr::Raster(s, lines, 100);

				pr::v4 lines[] =
				{
					pr::v4::make(0,-1,0,1), pr::v4::make(0,1,0,1),
					pr::v4::make(-1,0,0,1), pr::v4::make(1,0,0,1),
				};
				auto model = ModelGenerator<>::Lines(m_rdr, 2, lines, 0, nullptr, &mat);
				m_test_model.m_model = model;
				m_test_model_enable = true;

				pr::rdr::ProjectedTexture pt;
				pt.m_tex = m_rdr.m_tex_mgr.FindTexture(pr::rdr::EStockTexture::Checker);
				pt.m_o2w = pr::rdr::ProjectedTexture::MakeTransform(pr::v4::make(15,0,0,1), pr::v4Origin, pr::v4YAxis, 1.0f, pr::maths::tau_by_4, 0.01f, 100.0f, false);
				m_scene.m_render_steps[0]->as<pr::rdr::ForwardRender>().m_proj_tex.push_back(pt);
			#endif
		}
		catch (std::exception const& ex)
		{
			pr::events::Send(Evt_AppMsg(pr::FmtS(L"Error found while parsing demo scene.\r\n%S", ex.what()), L"Script Error"));
		}
	}

	// Handle files dropped onto the 3d-panel
	void MainUI::DropFiles(Control&, DropFilesEventArgs const& drop)
	{
		if (drop.m_filepaths.empty())
			return;

		// Load the dropped files
		LoadScripts(drop.m_filepaths, pr::KeyDown(VK_SHIFT));
	}

	// Application error message
	void MainUI::OnEvent(Evt_AppMsg const& e)
	{
		if (m_settings.m_ErrorOutputMsgBox)
			MsgBox::Show(*this, e.m_msg.c_str(), AppTitleW(), MsgBox::EButtons::Ok, e.m_icon);
		else
		{} // todo log?
	}
	void MainUI::OnEvent(Evt_Status const& e)
	{
		m_status_mgr.Apply(e);
	}
	void MainUI::OnEvent(Evt_Refresh const&)
	{
		RenderNeeded();
		Invalidate();
	}
	void MainUI::OnEvent(Evt_StoreChanging const&)
	{
		// A number of objects are about to be added
		m_suspend_render = true;
	}
	void MainUI::OnEvent(Evt_StoreChanged const& evt)
	{
		// The last object in a group has been added
		// Reset the scene bounding box
		m_bbox_scene = pr::BBoxReset;

		// See if a camera description was given in the script
		// If so, update the camera position (if not a reload)
		if (evt.m_reason != Evt_StoreChanged::EReason::Reload && evt.m_result.m_cam_fields != ParseResult::ECamField::None)
		{
			auto fields = evt.m_result.m_cam_fields;
			if (AllSet(fields, ParseResult::ECamField::C2W    )) m_cam.CameraToWorld     (evt.m_result.m_cam.CameraToWorld());
			if (AllSet(fields, ParseResult::ECamField::Focus  )) m_cam.FocusDist         (evt.m_result.m_cam.FocusDist());
			if (AllSet(fields, ParseResult::ECamField::Align  )) m_cam.SetAlign          (evt.m_result.m_cam.m_align);
			if (AllSet(fields, ParseResult::ECamField::Aspect )) m_cam.Aspect            (evt.m_result.m_cam.m_aspect);
			if (AllSet(fields, ParseResult::ECamField::FovY   )) m_cam.FovY              (evt.m_result.m_cam.FovY());
			if (AllSet(fields, ParseResult::ECamField::Near   )) m_cam.m_near           = evt.m_result.m_cam.m_near;
			if (AllSet(fields, ParseResult::ECamField::Far    )) m_cam.m_far            = evt.m_result.m_cam.m_far;
			if (AllSet(fields, ParseResult::ECamField::AbsClip)) m_cam.m_focus_rel_clip = evt.m_result.m_cam.m_focus_rel_clip;
			if (AllSet(fields, ParseResult::ECamField::Ortho  )) m_cam.m_orthographic   = evt.m_result.m_cam.m_orthographic;
		}

		m_suspend_render = false;
		RenderNeeded();
		UpdateUI();
	}
	void MainUI::OnEvent(Evt_SettingsError const& e)
	{
		pr::events::Send(Evt_AppMsg(pr::Widen(e.m_msg), L"Settings Error"));
	}
	void MainUI::OnEvent(Evt_UpdateScene const& e)
	{
		// Called when the scene needs updating

		// Render the selection box
		if (m_settings.m_ShowSelectionBox && m_store_ui.SelectedCount() != 0)
			e.m_scene.AddInstance(m_selection_box);

		// Tools instances
		if (m_measure_tool_ui.Gfx())
			m_measure_tool_ui.Gfx()->AddToScene(e.m_scene);
		if (m_angle_tool_ui.Gfx())
			m_angle_tool_ui.Gfx()->AddToScene(e.m_scene);

		// Render the focus point
		if (m_settings.m_ShowFocusPoint)
			e.m_scene.AddInstance(m_focus_point);

		// Render the origin
		if (m_settings.m_ShowOrigin)
			e.m_scene.AddInstance(m_origin_point);

		// Render the test point
		if (m_test_model_enable)
			e.m_scene.AddInstance(m_test_model);

		// Add instances from the store
		for (std::size_t i = 0, iend = m_store.size(); i != iend; ++i)
			m_store[i]->AddToScene(e.m_scene);

		// Add model bounding boxes
		if (m_settings.m_ShowObjectBBoxes)
		{
			for (std::size_t i = 0, iend = m_store.size(); i != iend; ++i)
				m_store[i]->AddBBoxToScene(e.m_scene, m_bbox_model.m_model);
		}

		// Set up the scene/render steps

		// Update the lighting. If lighting is camera relative, adjust the position and direction
		pr::rdr::Light& light = m_scene.m_global_light;
		light = m_settings.m_Light;
		if (m_settings.m_Light.m_cam_relative)
		{
			light.m_direction = m_cam.CameraToWorld() * m_settings.m_Light.m_direction;
			light.m_position  = m_cam.CameraToWorld() * m_settings.m_Light.m_position;
		}

		// Set the background colour
		m_scene.m_bkgd_colour = m_settings.m_BackgroundColour;
	}
	void MainUI::OnEvent(Evt_SelectionChanged const&)
	{
		// The selected objects have changed
		// Only do something while the selection box is visible
		if (!m_settings.m_ShowSelectionBox)
			return;

		// Update the transform of the selection box
		pr::BBox bbox = GetSceneBounds(EObjectBounds::Selected);
		m_selection_box.m_i2w = pr::m4x4::Scale(bbox.SizeX(), bbox.SizeY(), bbox.SizeZ(), bbox.Centre());

		// Request a refresh when the selection changes (if the selection box is visible)
		pr::events::Send(Evt_Refresh());
	}
	void MainUI::OnEvent(Evt_SettingsChanged const&)
	{
		// User settings have been changed
		m_settings.m_ObjectManagerSettings = m_store_ui.Settings();
	}
	void MainUI::OnEvent(Evt_RenderStepExecute const& e)
	{
		// Called per render step
		if (e.m_rstep.GetId() != pr::rdr::ERenderStep::ForwardRender)
			return;

		// Update the fill mode for the scene
		auto& fr = e.m_rstep.as<pr::rdr::ForwardRender>();
		switch (m_settings.m_GlobalFillMode)
		{
		default:
			PR_ASSERT(PR_DBG_LDR, false, "Unknown fill mode");
			break;
		case EFillMode::Solid:
			m_scene.m_rsb.Set(pr::rdr::ERS::FillMode, D3D11_FILL_SOLID);
			m_scene.m_bsb.Clear(pr::rdr::EBS::BlendEnable, 0);
			fr.m_clear_bb = true;
			break;
		case EFillMode::Wireframe:
			m_scene.m_rsb.Set(pr::rdr::ERS::FillMode, D3D11_FILL_WIREFRAME);
			m_scene.m_bsb.Set(pr::rdr::EBS::BlendEnable, FALSE, 0);
			fr.m_clear_bb = true;
			break;
		case EFillMode::SolidAndWire:
			if (m_scene_rdr_pass == 0 || e.m_complete)
			{
				m_scene.m_rsb.Set(pr::rdr::ERS::FillMode, D3D11_FILL_SOLID);
				m_scene.m_bsb.Clear(pr::rdr::EBS::BlendEnable, 0);
				fr.m_clear_bb = true;
			}
			else
			{
				m_scene.m_rsb.Set(pr::rdr::ERS::FillMode, D3D11_FILL_WIREFRAME);
				m_scene.m_bsb.Set(pr::rdr::EBS::BlendEnable, FALSE, 0);
				fr.m_clear_bb = false;
			}
			break;
		}
	}

	// Update the mouse coordinates in the status bar
	void MainUI::MouseStatusUpdate(pr::v2 const& mouse_location)
	{
		if (!m_mouse_status_updates)
			return;

		string status;
		{// Display mouse coordinates
			auto mouse_ss = pr::v4(mouse_location, m_nav.FocusDistance(), 0.0f);
			auto mouse_ws = m_nav.SSPointToWSPoint(mouse_ss);
			auto focus_ws = m_nav.FocusPoint();

			status += pr::FmtS(L"Mouse: {%3.3f %3.3f %3.3f} Focus: {%3.3f %3.3f %3.3f} Focus Distance: %3.3f"
				,mouse_ws.x ,mouse_ws.y ,mouse_ws.z
				,focus_ws.x ,focus_ws.y ,focus_ws.z
				,m_cam.FocusDist());
		}
		{// Display zoom
			auto zoom = m_nav.Zoom();
			if (!pr::FEql(zoom, 1.0f, 0.001f))
				status += pr::FmtS(L" Zoom: %3.3f", zoom);
		}
		pr::events::Send(Evt_Status(status));
	}

	// Message map function
	bool MainUI::ProcessWindowMessage(HWND parent_hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result)
	{
		switch (message)
		{
		case WM_SYSKEYDOWN:
			#pragma region
			{
				// Watch for full screen alt-enter transitions
				auto vk_key  = UINT(wparam);
				if (vk_key == VK_RETURN)
				{
					FullScreenMode(!m_window.FullScreenMode());
					result = S_OK;
					return true;
				}
				break;
			}
			#pragma endregion
		}
		return
			m_recent_files.ProcessWindowMessage(parent_hwnd, message, wparam, lparam, result) ||
			m_saved_views .ProcessWindowMessage(parent_hwnd, message, wparam, lparam, result) ||
			Form::ProcessWindowMessage(parent_hwnd, message, wparam, lparam, result);
	}

	// Set UI elements to reflect their current state
	void MainUI::UpdateUI()
	{
		// Camera orbit
		CheckMenuItem(MenuStrip(), ID_NAV_ORBIT ,m_settings.m_CameraOrbit ? MF_CHECKED : MF_UNCHECKED);

		// Auto refresh
		CheckMenuItem(MenuStrip(), ID_DATA_AUTOREFRESH ,m_settings.m_WatchForChangedFiles ? MF_CHECKED : MF_UNCHECKED);

		// Stock models
		CheckMenuItem(MenuStrip(), ID_RENDERING_SHOWFOCUS        ,m_settings.m_ShowFocusPoint   ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(MenuStrip(), ID_RENDERING_SHOWORIGIN       ,m_settings.m_ShowOrigin       ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(MenuStrip(), ID_RENDERING_SHOWSELECTION    ,m_settings.m_ShowSelectionBox ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(MenuStrip(), ID_RENDERING_SHOWOBJECTBBOXES ,m_settings.m_ShowObjectBBoxes ? MF_CHECKED : MF_UNCHECKED);

		// Set the text to the *next* mode
		switch (m_settings.m_GlobalFillMode)
		{
		case EFillMode::Solid:        ModifyMenuW(MenuStrip(), ID_RENDERING_WIREFRAME, MF_BYCOMMAND, ID_RENDERING_WIREFRAME, L"&Wireframe\tCtrl+W");  break;
		case EFillMode::Wireframe:    ModifyMenuW(MenuStrip(), ID_RENDERING_WIREFRAME, MF_BYCOMMAND, ID_RENDERING_WIREFRAME, L"&Wire + Solid\tCtrl+W"); break;
		case EFillMode::SolidAndWire: ModifyMenuW(MenuStrip(), ID_RENDERING_WIREFRAME, MF_BYCOMMAND, ID_RENDERING_WIREFRAME, L"&Solid\tCtrl+W");      break;
		}

		// Align axis checked items
		auto cam_align = m_settings.m_CameraAlignAxis;
		CheckMenuItem(MenuStrip() ,ID_NAV_ALIGN_NONE    ,cam_align == pr::v4Zero  ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(MenuStrip() ,ID_NAV_ALIGN_X       ,cam_align == pr::v4XAxis ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(MenuStrip() ,ID_NAV_ALIGN_Y       ,cam_align == pr::v4YAxis ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(MenuStrip() ,ID_NAV_ALIGN_Z       ,cam_align == pr::v4ZAxis ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(MenuStrip() ,ID_NAV_ALIGN_CURRENT ,cam_align != pr::v4Zero && cam_align != pr::v4XAxis &&  cam_align != pr::v4YAxis && cam_align != pr::v4ZAxis ? MF_CHECKED : MF_UNCHECKED);

		// Render 2d menu item
		ModifyMenuW(MenuStrip(), ID_RENDERING_RENDER2D, MF_BYCOMMAND, ID_RENDERING_RENDER2D, m_nav.Render2D() ? L"&Perspective" : L"&Orthographic");
		ModifyMenuW(MenuStrip(), ID_RENDERING_TECHNIQUE, MF_BYCOMMAND, ID_RENDERING_TECHNIQUE, m_scene.FindRStep<pr::rdr::ForwardRender>() ? L"&Deferred Rendering" : L"&Forward Rendering");

		// The tools windows
		CheckMenuItem(MenuStrip()  ,ID_TOOLS_MEASURE ,m_measure_tool_ui.Visible() ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(MenuStrip()  ,ID_TOOLS_ANGLE   ,m_angle_tool_ui  .Visible() ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(MenuStrip()  ,ID_TOOLS_MOVE    ,ControlMode() == EControlMode::Manipulation ? MF_CHECKED : MF_UNCHECKED);
		//EnableMenuItem(MenuStrip() ,ID_TOOLS_MOVE    ,m_store.empty() ? MF_DISABLED : MF_ENABLED);

		// Topmost window
		CheckMenuItem(MenuStrip(), ID_WINDOW_ALWAYSONTOP, m_settings.m_AlwaysOnTop ? MF_CHECKED : MF_UNCHECKED);
	}
}

// Main Entry Point
int __stdcall WinMain(HINSTANCE hInstance, HINSTANCE /*hPrevInstance*/, LPTSTR lpstrCmdLine, int nCmdShow)
{
	(void)hInstance;
	int nRet;
	{
		std::string err_msg;
		try
		{
			// CoInitialise COM
			pr::InitCom init_com;

			// Load required dlls
			pr::win32::LoadDll<struct Scintilla>(L"scintilla.dll");

			// Create and run the main GUI
			ldr::MainUI main(pr::Widen(lpstrCmdLine).c_str(), nCmdShow);
			nRet = main.Run();
		}
		catch (std::exception const& ex)
		{
			DWORD last_error = GetLastError();
			HRESULT res = HRESULT_FROM_WIN32(last_error);

			std::string ex_msg(ex.what());
			ex_msg.substr(0, ex_msg.find_last_not_of(" \t\r\n") + 1).swap(ex_msg);
			err_msg = pr::Fmt("Application shutdown due to unhandled error:\r\nError Message: '%s'", ex_msg.c_str());
			if (res != S_OK) err_msg += pr::Fmt("\r\nLast Error Code: %X - %s", res, pr::HrMsg(res).c_str());
			nRet = -1;
		}
		catch (...)
		{
			err_msg = "Shutting down due to an unknown exception";
			nRet = -1;
		}

		if (nRet == -1)
		{
			std::thread([&] { ::MessageBoxA(0, err_msg.c_str(), "Application Error", MB_OK|MB_ICONERROR); }).join();
		}
	}
	return nRet;
}

#if defined _M_IX86
#pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='x86' publicKeyToken='6595b64144ccf1df' language='*'\"")
#elif defined _M_IA64
#pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='ia64' publicKeyToken='6595b64144ccf1df' language='*'\"")
#elif defined _M_X64
#pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='amd64' publicKeyToken='6595b64144ccf1df' language='*'\"")
#else
#pragma comment(linker, "/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='*' publicKeyToken='6595b64144ccf1df' language='*'\"")
#endif
