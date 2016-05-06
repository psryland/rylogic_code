//*****************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
#include "linedrawer/main/stdafx.h"
#include "linedrawer/main/linedrawer.h"
#include "linedrawer/main/ldrevent.h"
#include "linedrawer/main/ldrexception.h"
#include "linedrawer/gui/linedrawergui.h"
#include "linedrawer/utility/misc.h"
#include "linedrawer/utility/debug.h"
#include "pr/linedrawer/ldr_object.h"

using namespace pr::ldr;

namespace ldr
{
	wchar_t const AppNameW[] = L"LineDrawer";
	char const AppNameA[]    = "LineDrawer";
	char const Version[]     = "4.00.00";
	char const Copyright[]   = "Copyright (c) Rylogic Limited 2002";

	wchar_t const* AppTitleW()     { return AppNameW; }
	char const*    AppTitleA()     { return AppNameA; }
	char const*    AppString()     { return pr::FmtX<Main, 128>("%s - Version: %s\r\n%s\r\nAll Rights Reserved.", AppNameA, Version, Copyright); }
	char const*    AppStringLine() { return pr::FmtX<Main, 128>("%s - Version: %s %s", AppNameA, Version, Copyright); }

	// App set up
	struct Setup
	{
		ldr::UserSettings UserSettings() const
		{
			// Determine the directory we're running in
			char temp[MAX_PATH];
			GetModuleFileNameA(0, temp, MAX_PATH);
			std::string path = temp;
			pr::filesys::RmvExtension(path);
			path += ".ini";
			return ldr::UserSettings(path, true);
		}

		static BOOL const gdi_support = FALSE;
		//#pragma message(PR_LINK "gdi support enabled")

		// Return settings to configure the render
		pr::rdr::RdrSettings RdrSettings()
		{
			return pr::rdr::RdrSettings(gdi_support);
		}

		// Return settings for the render window
		pr::rdr::WndSettings RdrWindowSettings(HWND hwnd, pr::iv2 const& client_area)
		{
			return pr::rdr::WndSettings(hwnd, TRUE, gdi_support, client_area);
		}
	};

	Main::Main(MainGUI& gui)
		:base(Setup(), gui)
		,m_store()
		,m_nav(m_cam, m_window.RenderTargetSize(), m_settings.m_CameraAlignAxis)
		,m_manip(m_cam, m_rdr)
		,m_lua_src()
		,m_sources(m_settings, m_rdr, m_store, m_lua_src)
		,m_bbox_scene(pr::BBoxReset)
		,m_ctrl_mode(EControlMode::Navigation)
		,m_input(&m_nav)
		,m_scene_rdr_pass(0)
		,m_step_objects()
		,m_focus_point()
		,m_origin_point()
		,m_selection_box()
		,m_bbox_model()
		,m_test_model()
		,m_test_model_enable(false)
	{
		// Create stock models such as the focus point, origin, selection box, etc
		CreateStockModels();
	}
	Main::~Main()
	{
		m_input->LostInputFocus(nullptr);
		m_input = nullptr;
		m_settings.Save();
	}

	// Get/Set the nav mode
	EControlMode Main::ControlMode() const
	{
		return m_ctrl_mode;
	}
	void Main::ControlMode(EControlMode mode)
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

	// Reset the camera to view all, selected, or visible objects
	void Main::ResetView(EObjectBounds view_type)
	{
		// Reset the scene to view the bounding box
		m_nav.ResetView(GetSceneBounds(view_type));
	}

	// Update the display
	void Main::DoRender(bool force)
	{
		// Only render if asked to
		if (!m_rdr_pending && !force)
			return;

		// Allow new render requests now
		m_rdr_pending = false;

		// Ignore render calls if the user settings say rendering is disabled
		if (!m_settings.m_RenderingEnabled)
			return;

		// Update the position of the focus point
		if (m_settings.m_ShowFocusPoint)
		{
			float scale = m_settings.m_FocusPointScale * m_nav.FocusDistance();
			m_focus_point.m_i2w = pr::m4x4::Scale(scale, m_nav.FocusPoint());
		}

		// Update the scale of the origin
		if (m_settings.m_ShowOrigin)
		{
			float scale = m_settings.m_FocusPointScale * pr::Length3(m_cam.CameraToWorld().pos);
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

	// Reload all data
	void Main::ReloadSourceData()
	{
		try
		{
			m_sources.Reload();
		}
		catch (LdrException const& e)
		{
			switch (e.code())
			{
			default:
				pr::events::Send(ldr::Event_Error(pr::FmtS("Error found while reloading source data.\nError details: %s", e.what())));
				break;
			case ELdrException::OperationCancelled:
				pr::events::Send(ldr::Event_Info(pr::FmtS("Reloading data cancelled")));
				break;
			}
		}
	}

	// The size of the window has changed
	void Main::Resize(pr::IRect const& area)
	{
		base::Resize(area);
		m_nav.ViewSize(area.Size());
		m_settings.Save();
	}

	// Generate a scene containing the supported line drawer objects.
	void Main::CreateDemoScene()
	{
		try
		{
			{// For testing..
				using namespace pr;
				using namespace pr::rdr;
			/*
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

			/*
				//if (m_scene.FindRStep<ShadowMap>() == nullptr)
				//	m_scene.m_render_steps.insert(begin(m_scene.m_render_steps), std::make_shared<ShadowMap>(m_scene, m_scene.m_global_light, pr::iv2::make(1024,1024)));

				//pr::ldr::AddString(m_rdr, "*Rect r FF00FF00 {3 1 *Solid}", m_store, pr::ldr::DefaultContext, false, 0, &m_lua_src);
				//m_nav.m_camera.LookAt(pr::v4Origin, pr::v4::make(0,0,2,1), pr::v4YAxis);

				//auto thick_line = m_rdr.m_shdr_mgr.FindShader(EStockShader::ThickLineListGS);

				//NuggetProps mat;
				//mat.m_sset.push_back(thick_line);
			
				////std::vector<pr::v4> lines;
				////pr::Spline s = pr::Spline::make(pr::v4Origin, pr::v4XAxis.w1, pr::v4YAxis.w1, pr::v4Origin);
				////pr::Raster(s, lines, 100);

				//pr::v4 lines[] =
				//{
				//	pr::v4::make(0,-1,0,1), pr::v4::make(0,1,0,1),
				//	pr::v4::make(-1,0,0,1), pr::v4::make(1,0,0,1),
				//};
				//auto model = ModelGenerator<>::Lines(m_rdr, 2, lines, 0, nullptr, &mat);
				//m_test_model.m_model = model;
				//m_test_model_enable = true;

				//pr::rdr::ProjectedTexture pt;
				//pt.m_tex = m_rdr.m_tex_mgr.FindTexture(pr::rdr::EStockTexture::Checker);
				//pt.m_o2w = pr::rdr::ProjectedTexture::MakeTransform(pr::v4::make(15,0,0,1), pr::v4Origin, pr::v4YAxis, 1.0f, pr::maths::tau_by_4, 0.01f, 100.0f, false);
				//m_scene.m_render_steps[0]->as<pr::rdr::ForwardRender>().m_proj_tex.push_back(pt);
			//*/
			}

			m_sources.AddString(pr::ldr::CreateDemoScene());
		}
		catch (pr::script::Exception const& e) { pr::events::Send(ldr::Event_Error(pr::FmtS("Error found while parsing demo scene\nError details: %s", e.what()))); }
		catch (LdrException const& e)          { pr::events::Send(ldr::Event_Error(pr::FmtS("Error found while parsing demo scene\nError details: %s", e.what()))); }
	}

	// Create stock models such as the focus point, origin, etc
	void Main::CreateStockModels()
	{
		using namespace pr::rdr;
		{
			// Create the focus point models
			static pr::v4 const verts[] = // 'static' is a workaround to make 'pt' aligned in VS2015 update 2
			{
				pr::v4(0.0f,  0.0f,  0.0f, 1.0f),
				pr::v4(1.0f,  0.0f,  0.0f, 1.0f),
				pr::v4(0.0f,  0.0f,  0.0f, 1.0f),
				pr::v4(0.0f,  1.0f,  0.0f, 1.0f),
				pr::v4(0.0f,  0.0f,  0.0f, 1.0f),
				pr::v4(0.0f,  0.0f,  1.0f, 1.0f),
			};
			pr::Colour32 const coloursFF[] = { 0xFFFF0000, 0xFFFF0000, 0xFF00FF00, 0xFF00FF00, 0xFF0000FF, 0xFF0000FF };
			pr::Colour32 const colours80[] = { 0xFF800000, 0xFF800000, 0xFF008000, 0xFF008000, 0xFF000080, 0xFF000080 };
			pr::uint16 const lines[]       = { 0, 1, 2, 3, 4, 5 };
			assert(pr::maths::is_aligned(&verts[0]));

			m_focus_point .m_model = ModelGenerator<>::Mesh(m_rdr, EPrim::LineList, PR_COUNTOF(verts), PR_COUNTOF(lines), verts, lines, PR_COUNTOF(coloursFF), coloursFF);
			m_focus_point .m_model->m_name = "focus point";
			m_focus_point .m_i2w   = pr::m4x4Identity;

			m_origin_point.m_model = ModelGenerator<>::Mesh(m_rdr, EPrim::LineList, PR_COUNTOF(verts), PR_COUNTOF(lines), verts, lines, PR_COUNTOF(colours80), colours80);
			m_origin_point.m_model->m_name = "origin point";
			m_origin_point.m_i2w   = pr::m4x4Identity;
		}
		{
			// Create the selection box model
			static pr::v4 const verts[] = // 'static' is a workaround to make 'pt' aligned in VS2015 update 2
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
			pr::uint16 const lines[] =
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
			assert(pr::maths::is_aligned(&verts[0]));
			m_selection_box.m_model = ModelGenerator<>::Mesh(m_rdr, EPrim::LineList, PR_COUNTOF(verts), PR_COUNTOF(lines), verts, lines);
			m_selection_box.m_model->m_name = "selection box";
			m_selection_box.m_i2w   = pr::m4x4Identity;
		}
		{
			// Create a bounding box model
			static pr::v4 const verts[] = // 'static' is a workaround to make 'pt' aligned in VS2015 update 2
			{
				pr::v4(-0.5f, -0.5f, -0.5f, 1.0f),
				pr::v4(0.5f, -0.5f, -0.5f, 1.0f),
				pr::v4(0.5f,  0.5f, -0.5f, 1.0f),
				pr::v4(-0.5f,  0.5f, -0.5f, 1.0f),
				pr::v4(-0.5f, -0.5f,  0.5f, 1.0f),
				pr::v4(0.5f, -0.5f,  0.5f, 1.0f),
				pr::v4(0.5f,  0.5f,  0.5f, 1.0f),
				pr::v4(-0.5f,  0.5f,  0.5f, 1.0f),
			};
			pr::uint16 const lines[] =
			{
				0, 1, 1, 2, 2, 3, 3, 0,
				4, 5, 5, 6, 6, 7, 7, 4,
				0, 4, 1, 5, 2, 6, 3, 7
			};
			assert(pr::maths::is_aligned(&verts[0]));
			m_bbox_model.m_model = ModelGenerator<>::Mesh(m_rdr, EPrim::LineList, PR_COUNTOF(verts), PR_COUNTOF(lines), verts, lines, 1, &pr::Colour32Blue);
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
	pr::BBox Main::GetSceneBounds(EObjectBounds bound_type) const
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
				for (auto obj = m_gui.m_store_ui.EnumSelected(iter); obj; obj = m_gui.m_store_ui.EnumSelected(iter))
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

	// User settings have been changed
	void Main::OnEvent(pr::ldr::Evt_SettingsChanged const&)
	{
		m_settings.m_ObjectManagerSettings = m_gui.m_store_ui.Settings();
	}

	// The selected objects have changed
	void Main::OnEvent(pr::ldr::Evt_LdrObjectSelectionChanged const&)
	{
		// Only do something while the selection box is visible
		if (!m_settings.m_ShowSelectionBox)
			return;

		// Update the transform of the selection box
		pr::BBox bbox = GetSceneBounds(EObjectBounds::Selected);
		m_selection_box.m_i2w = pr::m4x4::Scale(bbox.SizeX(), bbox.SizeY(), bbox.SizeZ(), bbox.Centre());

		// Request a refresh when the selection changes (if the selection box is visible)
		pr::events::Send(Event_Refresh());
	}

	// Called when the scene needs updating
	void Main::OnEvent(pr::rdr::Evt_UpdateScene const& e)
	{
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

		// Setup the scene/render steps

		// Update the lighting. If lighting is camera relative, adjust the position and direction
		pr::rdr::Light& light = m_scene.m_global_light;
		light = m_settings.m_Light;
		if (m_settings.m_LightIsCameraRelative)
		{
			light.m_direction = m_cam.CameraToWorld() * m_settings.m_Light.m_direction;
			light.m_position  = m_cam.CameraToWorld() * m_settings.m_Light.m_position;
		}

		// Set the background colour
		m_scene.m_bkgd_colour = m_settings.m_BackgroundColour;
	}

	// Called per render step
	void Main::OnEvent(pr::rdr::Evt_RenderStepExecute const& e)
	{
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

	// Called when the store of objects has changed
	void Main::OnEvent(Event_StoreChanged const& evt)
	{
		// Reset the scene bounding box
		m_bbox_scene = pr::BBoxReset;

		// See if a camera description was given in the script
		// If so, update the camera position (if not a reload)
		if (evt.m_reason != Event_StoreChanged::EReason::Reload && evt.m_result.m_cam_fields != ParseResult::ECamField::None)
		{
			auto fields = evt.m_result.m_cam_fields;
			if (fields & ParseResult::ECamField::C2W    ) m_cam.CameraToWorld     (evt.m_result.m_cam.CameraToWorld());
			if (fields & ParseResult::ECamField::Focus  ) m_cam.FocusDist         (evt.m_result.m_cam.FocusDist());
			if (fields & ParseResult::ECamField::Align  ) m_cam.SetAlign          (evt.m_result.m_cam.m_align);
			if (fields & ParseResult::ECamField::Aspect ) m_cam.Aspect            (evt.m_result.m_cam.m_aspect);
			if (fields & ParseResult::ECamField::FovY   ) m_cam.FovY              (evt.m_result.m_cam.FovY());
			if (fields & ParseResult::ECamField::Near   ) m_cam.m_near           = evt.m_result.m_cam.m_near;
			if (fields & ParseResult::ECamField::Far    ) m_cam.m_far            = evt.m_result.m_cam.m_far;
			if (fields & ParseResult::ECamField::AbsClip) m_cam.m_focus_rel_clip = evt.m_result.m_cam.m_focus_rel_clip;
			if (fields & ParseResult::ECamField::Ortho  ) m_cam.m_orthographic   = evt.m_result.m_cam.m_orthographic;
		}
	}
}