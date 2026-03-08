#include "src/forward.h"
#include "src/ui/sandbox_ui.h"

namespace physics_sandbox
{
	// ===== RecentFiles implementation =====

	std::filesystem::path RecentFiles::StoragePath()
	{
		wchar_t* appdata = nullptr;
		_wdupenv_s(&appdata, nullptr, L"APPDATA");
		if (!appdata)
			return {};

		auto dir = std::filesystem::path(appdata) / L"RylogicPhysicsSandbox";
		free(appdata);
		std::filesystem::create_directories(dir);
		return dir / L"recent.txt";
	}

	void RecentFiles::Load()
	{
		m_paths.clear();
		auto path = StoragePath();
		if (path.empty() || !std::filesystem::exists(path))
			return;

		std::ifstream f(path);
		std::string line;
		while (std::getline(f, line))
		{
			if (!line.empty())
				m_paths.emplace_back(line);
		}
	}

	void RecentFiles::Save() const
	{
		auto path = StoragePath();
		if (path.empty())
			return;

		std::ofstream f(path);
		for (auto const& p : m_paths)
			f << p.string() << "\n";
	}

	// Add a path to the front (MRU order), removing duplicates and capping at max
	void RecentFiles::Add(std::filesystem::path const& filepath)
	{
		// Remove any existing entry for this path
		auto canonical = std::filesystem::weakly_canonical(filepath);
		std::erase_if(m_paths, [&](auto const& p)
		{
			return std::filesystem::weakly_canonical(p) == canonical;
		});

		// Insert at front (most recent first)
		m_paths.insert(m_paths.begin(), canonical);

		// Cap at maximum
		if (static_cast<int>(m_paths.size()) > MaxRecentFiles)
			m_paths.resize(MaxRecentFiles);

		Save();
	}

	// ===== SandboxUI implementation =====

	SandboxUI::SandboxUI()
		: Form(Params<>()
			.name("physics-sandbox")
			.title(L"Rylogic Physics Sandbox")
			.wh(1600, 1000)
			.start_pos(EStartPosition::CentreParent)
			.padding(0)
			.menu({ {L"&File", Menu(Menu::EKind::Popup, {
				MenuItem(L"&Open Scene...\tCtrl+O", MenuID::OpenFile),
				MenuItem(L"Recent Files", ::CreatePopupMenu()),
				MenuItem(MenuItem::Separator),
				MenuItem(L"E&xit", IDCLOSE),
			})} })
			.main_wnd(true)
			.wndclass(RegisterWndClass<SandboxUI>()))
		, m_status(StatusBar::Params<>().parent(this_).dock(EDock::Bottom))
		, m_media(MediaPanel::Params<>().parent(this_))
		, m_details(DetailsPanel::Params<>().parent(this_))
		, m_view3d(View3DPanelStatic::Params()
			.parent(this_)
			.dock(EDock::Fill))
		, m_scene()
		, m_pause_on_collision(false)
		, m_frame_count(0)
		, m_fps_elapsed(0)
		, m_fps(0)
		, m_title_elapsed(0)
		, m_details_elapsed(0)
		, m_closing(false)
	{
		// Load the recent files list from disk and populate the submenu
		m_recent.Load();
		RebuildRecentFilesMenu();

		// Wire media panel events to simulation control
		m_media.OnPlay += [&](auto&, auto&)
		{
			m_scene.m_steps_remaining = -1; // Run continuously
		};
		m_media.OnPause += [&](auto&, auto&)
		{
			m_scene.m_steps_remaining = 0; // Pause
		};
		m_media.OnReset += [&](auto&, auto&)
		{
			ResetScene();
		};

		// Keyboard shortcuts for power-user control
		m_view3d.Key += [&](Control&, KeyEventArgs const& args)
		{
			if (!args.m_down)
				return;

			// 0-5: select scenario (0 = sandbox, 1-5 = test scenarios)
			if (args.m_vk_key >= '0' && args.m_vk_key <= '5')
			{
				m_scene.m_scenario = static_cast<EScenario>(args.m_vk_key - '0');
				ResetScene();
				m_scene.m_steps_remaining = -1; // Auto-start
			}

			// R=reset, S=single step, G=go (play), P=pause
			if (args.m_vk_key == 'R')
				ResetScene();
			if (args.m_vk_key == 'S')
				m_scene.m_steps_remaining = 1;
			if (args.m_vk_key == 'G')
				m_scene.m_steps_remaining = -1;
			if (args.m_vk_key == 'P')
				m_scene.m_steps_remaining = 0;

			// C=toggle pause-on-collision
			if (args.m_vk_key == 'C')
				m_pause_on_collision = !m_pause_on_collision;

			// T=run all test scenarios
			if (args.m_vk_key == 'T')
				m_scene.RunAllTests();

			// Speed control: [=slower, ]=faster, \=reset to 1.0x
			// Each press adjusts by 0.1x (10 slider ticks)
			if (args.m_vk_key == VK_OEM_4) // '[' key
				m_media.TimeScale(m_media.TimeScale() - 0.1f);
			if (args.m_vk_key == VK_OEM_6) // ']' key
				m_media.TimeScale(m_media.TimeScale() + 0.1f);
			if (args.m_vk_key == VK_OEM_5) // '\' key
				m_media.TimeScale(1.0f);

			// Ctrl+O=open scene file
			if (args.m_vk_key == 'O' && (GetKeyState(VK_CONTROL) & 0x8000))
				OpenSceneFile();
		};

		// Give Body access to the renderer for creating graphics objects from LDraw script
		Body::s_rdr = &m_view3d.m_rdr;

		// Hook the scene population event — called each frame during DoRender() to add
		// objects to the scene's drawlist before rendering.
		m_view3d.OnAddToScene += [&](auto&, rdr12::Scene& scene)
		{
			for (int i = 0; i != m_scene.m_body_count; ++i)
				m_scene.m_body[i].AddToScene(scene);

			if (m_scene.m_ground_gfx)
				m_scene.m_ground_gfx->AddToScene(scene);
		};

		// Start with the sandbox scenario
		ResetScene();
	}

	SandboxUI::~SandboxUI()
	{
		// Release all graphics objects before the renderer is destroyed.
		// LdrObjectPtr ref-counting handles the actual deletion.
		for (int i = 0; i != m_scene.m_body_count; ++i)
			m_scene.m_body[i].m_gfx = nullptr;

		m_scene.m_ground_gfx = nullptr;
	}

	// Override message processing to ensure clean shutdown
	bool SandboxUI::ProcessWindowMessage(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam, LRESULT& result)
	{
		if (message == WM_CLOSE)
		{
			// Set closing flag so the step/render lambdas stop accessing the renderer
			m_closing = true;

			// Release all graphics objects before the window is destroyed
			for (int i = 0; i != m_scene.m_body_count; ++i)
				m_scene.m_body[i].m_gfx = nullptr;

			m_scene.m_ground_gfx = nullptr;
		}

		// Handle menu commands
		if (message == WM_COMMAND)
		{
			auto id = LOWORD(wparam);
			if (id == MenuID::OpenFile)
			{
				OpenSceneFile();
				result = 0;
				return true;
			}

			// Recent Files submenu items (IDs in range RecentFileBase..RecentFileBase+MaxRecentFiles-1)
			if (id >= MenuID::RecentFileBase && id < MenuID::RecentFileBase + MaxRecentFiles)
			{
				auto index = id - MenuID::RecentFileBase;
				if (index < static_cast<int>(m_recent.m_paths.size()))
					LoadSceneFile(m_recent.m_paths[index]);

				result = 0;
				return true;
			}
		}

		return Form::ProcessWindowMessage(hwnd, message, wparam, lparam, result);
	}

	// Reset the scene and sync graphics.
	// If a scene file was loaded, reload it from disk. Otherwise, reset the built-in scenario.
	void SandboxUI::ResetScene()
	{
		// If a scene file was loaded, reload it from scratch
		if (!m_scene_filepath.empty())
		{
			LoadSceneFile(m_scene_filepath);
			return;
		}

		m_scene.Reset();

		// Frame the camera to see the whole scene: look from +Y toward origin, Z-up
		m_view3d.m_cam.LookAt(
			v4(0, -35, 10, 1),
			v4::Origin(),
			v4{0, 0, 1, 0});

		Render();
	}

	// Show the Open File dialog and load a JSON scene file
	void SandboxUI::OpenSceneFile()
	{
		COMDLG_FILTERSPEC filters[] = {
			{L"Scene Files (*.json)", L"*.json"},
			{L"All Files (*.*)", L"*.*"},
		};
		auto opts = FileUIOptions()
			.def_extn(L"json")
			.filters(filters, std::size(filters), 0);

		auto files = OpenFileUI(m_hwnd, opts);
		if (files.empty())
			return;

		LoadSceneFile(std::filesystem::path(files[0]));
	}

	// Rebuild the Recent Files submenu from the current m_recent list.
	// Finds the "Recent Files" popup in the File menu and replaces its items
	// with numbered entries for each recently opened scene file.
	void SandboxUI::RebuildRecentFilesMenu()
	{
		// Navigate to the File menu (first submenu of the menu bar)
		auto menu_bar = ::GetMenu(m_hwnd);
		if (!menu_bar)
			return;

		auto file_menu = ::GetSubMenu(menu_bar, 0);
		if (!file_menu)
			return;

		// The "Recent Files" item is at position 1 (after "Open Scene...")
		auto recent_menu = ::GetSubMenu(file_menu, 1);
		if (!recent_menu)
			return;

		// Clear existing items
		while (::GetMenuItemCount(recent_menu) > 0)
			::RemoveMenu(recent_menu, 0, MF_BYPOSITION);

		// Populate with current recent files list
		if (m_recent.m_paths.empty())
		{
			::AppendMenuW(recent_menu, MF_STRING | MF_GRAYED, 0, L"(empty)");
		}
		else
		{
			for (int i = 0; i != static_cast<int>(m_recent.m_paths.size()); ++i)
			{
				// Format as "&1 filename.json" for keyboard accelerator
				auto label = pr::FmtS(L"&%d %s", i + 1, m_recent.m_paths[i].filename().c_str());
				::AppendMenuW(recent_menu, MF_STRING, MenuID::RecentFileBase + i, label);
			}
		}

		::DrawMenuBar(m_hwnd);
	}

	// Load a scene from a JSON file path.
	// Takes filepath by value to avoid dangling references when m_recent.Add()
	// reorders the MRU list (the caller may pass m_recent.m_paths[i]).
	void SandboxUI::LoadSceneFile(std::filesystem::path filepath)
	{
		try
		{
			// Remember the filepath so Reset can reload it
			m_scene_filepath = filepath;

			// Add to the recent files list (MRU order) and rebuild the submenu
			m_recent.Add(filepath);
			RebuildRecentFilesMenu();

			// Load the scene from JSON (creates new body graphics automatically)
			m_scene.LoadFromJson(filepath);

			// Frame the camera to see all loaded bodies
			auto bbox = ComputeSceneBBox();
			m_view3d.m_cam.View(bbox,
				v4{ 0, -1, 0, 0 },  // Forward direction
				v4{ 0, 0, 1, 0 });  // Up direction (Z-up)

			Render();
		}
		catch (std::exception const& ex)
		{
			::MessageBoxA(m_hwnd, pr::FmtS("Failed to load scene:\n%s", ex.what()), "Load Error", MB_OK | MB_ICONERROR);
		}
	}

	// Advance the simulation by one timestep
	void SandboxUI::Step(double elapsed_seconds)
	{
		// Don't step after close begins
		if (m_closing)
			return;

		// Accumulate time for FPS measurement and rate-limited UI updates.
		// These use wall-clock time (not scaled time) so the UI stays responsive.
		m_fps_elapsed += elapsed_seconds;
		m_title_elapsed += elapsed_seconds;
		m_details_elapsed += elapsed_seconds;
		if (m_fps_elapsed >= 1.0)
		{
			m_fps = m_frame_count / m_fps_elapsed;
			m_frame_count = 0;
			m_fps_elapsed = 0;
		}

		// Check if we should be stepping
		if (m_scene.m_steps_remaining == 0)
			return;
		if (m_scene.m_steps_remaining > 0)
			--m_scene.m_steps_remaining;

		// Apply the time scale from the slow-mo slider.
		// This scales the physics dt so that 0.5x = half speed, 2.0x = double speed, etc.
		// Wall-clock elapsed_seconds drives the render loop; sim_dt drives the physics.
		auto sim_dt = elapsed_seconds * m_media.TimeScale();

		// Step the physics scene with the scaled timestep
		auto collision = m_scene.Step(sim_dt);

		// Pause on first collision if requested
		if (collision && m_pause_on_collision && m_scene.m_diag.count == 1)
			m_scene.m_steps_remaining = 0;
	}

	// Render a frame: sync graphics, rebuild overlays, update details panel.
	// Expensive UI operations are rate-limited to avoid dominating frame time.
	void SandboxUI::Render()
	{
		// Don't render after close begins
		if (m_closing)
			return;

		++m_frame_count;

		// Sync each body's View3D graphics to its physics transform.
		// This is cheap — just copying an O2W matrix per body.
		for (int i = 0; i != m_scene.m_body_count; ++i)
			m_scene.m_body[i].UpdateGfx();

		// Render the 3D viewport. Objects are added to the scene via the
		// OnAddToScene event during DoRender(), so no explicit Add/Remove needed.
		m_view3d.DoRender();

		// Update the details panel text at reduced rate (~5 Hz).
		if (m_details_elapsed >= 0.2)
		{
			m_details_elapsed = 0;
			m_details.Update(m_scene);
		}

		// Update title bar at reduced rate (~4 Hz). SetWindowTextA triggers
		// non-client repaint which is expensive at high frequency.
		if (m_title_elapsed >= 0.25)
		{
			m_title_elapsed = 0;

			// Keep the slider's speed label text in sync with the trackbar position
			m_media.UpdateSpeedLabel();

			SetWindowTextA(*this, pr::FmtS("Physics Sandbox [%d: %s] t=%.3f col=%d  FPS: %.0f",
				static_cast<int>(m_scene.m_scenario),
				ScenarioName(m_scene.m_scenario),
				m_scene.m_clock,
				m_scene.m_diag.count,
				m_fps));
		}

		// Update status bar (only when text changes to avoid flicker)
		auto new_status = std::wstring(pr::FmtS(L"t=%.3f  %hs  Collisions: %d  %s  FPS: %.0f",
			m_scene.m_clock,
			ScenarioName(m_scene.m_scenario),
			m_scene.m_diag.count,
			m_scene.m_steps_remaining == 0 ? L"[Paused]" : L"[Running]",
			m_fps));
		if (new_status != m_last_status)
		{
			m_last_status = std::move(new_status);
			m_status.Text(0, m_last_status.c_str());
		}
	}

	// Compute a bounding box that encompasses all bodies in the scene.
	// Used to frame the camera when loading a new scene.
	BBox SandboxUI::ComputeSceneBBox() const
	{
		if (m_scene.m_body_count == 0)
			return BBox{ v4{0, 0, 0, 1}, v4{5, 5, 5, 0} };

		// Find the min/max extents of all dynamic body positions.
		// Also include the ground plane height (Z=0 typically) so the camera
		// can see the ground surface, not just the bodies floating in space.
		auto lo = pr::v4{ +1e10f, +1e10f, +1e10f, 1 };
		auto hi = pr::v4{ -1e10f, -1e10f, -1e10f, 1 };
		for (int i = 0; i != m_scene.m_body_count; ++i)
		{
			// Skip static (ground) bodies — they're huge and would dominate the bbox
			if (m_scene.m_body[i].Mass() > pr::physics::InfiniteMass * 0.5f)
				continue;

			auto pos = m_scene.m_body[i].O2W().pos;
			lo.x = pr::Min(lo.x, pos.x - 2.0f);
			lo.y = pr::Min(lo.y, pos.y - 2.0f);
			lo.z = pr::Min(lo.z, pos.z - 2.0f);
			hi.x = pr::Max(hi.x, pos.x + 2.0f);
			hi.y = pr::Max(hi.y, pos.y + 2.0f);
			hi.z = pr::Max(hi.z, pos.z + 2.0f);
		}

		// If there's a ground visual, include the ground height in the bbox
		// so the camera can see where objects will land.
		if (m_scene.m_ground_gfx)
		{
			lo.z = pr::Min(lo.z, 0.0f);
		}

		auto centre = (lo + hi) * 0.5f;
		auto radius = (hi - lo) * 0.5f;
		centre.w = 1;
		radius.w = 0;
		return BBox{ centre, radius };
	}

}
