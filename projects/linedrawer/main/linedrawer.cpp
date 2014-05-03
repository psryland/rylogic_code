//*****************************************************************************************
// LineDrawer
//  Copyright © Rylogic Ltd 2009
//*****************************************************************************************
#include "linedrawer/main/stdafx.h"
#include "linedrawer/main/linedrawer.h"
#include "linedrawer/main/ldrevent.h"
#include "linedrawer/main/ldrexception.h"
#include "linedrawer/gui/linedrawergui.h"
#include "linedrawer/utility/misc.h"
#include "linedrawer/utility/debug.h"
#include "pr/linedrawer/ldr_object.h"

namespace ldr
{
	wchar_t const AppNameW[] = L"LineDrawer";
	char const AppNameA[]    = "LineDrawer";
	char const Version[]     = "4.00.00";
	char const Copyright[]   = "Copyright © Rylogic Limited 2002";

	pr::ldr::ContextId LdrContext = 0xFFFFFFFF;

	wchar_t const* AppTitleW()     { return AppNameW; }
	char const*    AppTitleA()     { return AppNameA; }
	char const*    AppString()     { return pr::FmtX<Main, 128>("%s - Version: %s\r\n%s\r\nAll Rights Reserved.", AppNameA, Version, Copyright); }
	char const*    AppStringLine() { return pr::FmtX<Main, 128>("%s - Version: %s %s", AppNameA, Version, Copyright); }

	// App setup
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

		// Return settings to configure the render
		pr::rdr::RdrSettings RdrSettings(HWND hwnd, pr::iv2 const& client_area)
		{
			return pr::rdr::RdrSettings(hwnd, TRUE, client_area);
		}
	};

	Main::Main(MainGUI& gui)
		:base(Setup(), gui)
		,m_nav(m_cam, m_rdr.RenderTargetSize(), m_settings.m_CameraAlignAxis)
		,m_store()
		,m_plugin_mgr(this)
		,m_lua_src()
		,m_files(m_settings, m_rdr, m_store, m_lua_src)
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
		m_settings.Save();
	}

	// Reset the camera to view all, selected, or visible objects
	void Main::ResetView(pr::ldr::EObjectBounds view_type)
	{
		m_nav.ResetView(m_gui.m_store_ui.GetBBox(view_type));
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
			pr::Scale4x4(m_focus_point.m_i2w, scale, m_nav.FocusPoint());
		}

		// Update the scale of the origin
		if (m_settings.m_ShowOrigin)
		{
			float scale = m_settings.m_FocusPointScale * pr::Length3(m_cam.CameraToWorld().pos);
			pr::Scale4x4(m_origin_point.m_i2w, scale, pr::v4Origin);
		}

		// Allow the navigation manager to adjust the camera, ready for this frame
		//m_nav.PositionCamera();

		// Set the camera view
		m_scene.SetView(m_cam);

		// Add objects to the viewport
		m_scene.ClearDrawlists();
		m_scene.UpdateDrawlists();

		// Render the scene
		m_scene_rdr_pass = 0;
		m_scene.Render();

		// Render wire frame over solid
		if (m_settings.m_GlobalRenderMode == EGlobalRenderMode::SolidAndWire)
		{
			m_scene_rdr_pass = 1;
			m_scene.Render();
		}

		m_rdr.Present();
	}

	// Reload all data
	void Main::ReloadSourceData()
	{
		try
		{
			m_files.Reload();
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
	void Main::Resize(pr::iv2 const& size)
	{
		base::Resize(size);
		m_nav.SetViewSize(size);
		m_settings.Save();
	}

	// Generate a scene containing the supported line drawer objects.
	void Main::CreateDemoScene()
	{
		try
		{
			std::string scene = pr::ldr::CreateDemoScene();
			pr::ldr::AddString(m_rdr, scene.c_str(), m_store, pr::ldr::DefaultContext, false, 0, &m_lua_src);

			/*{// For testing..
				using namespace pr::rdr;

				auto gditex = m_rdr.m_tex_mgr.CreateTextureGdi(AutoId, Image::make(100,100), "gdi test");
				{
					using namespace Gdiplus;
					TextureGdi::Gfx gfx(gditex);
					SolidBrush b(Color(255,0,0));
					gfx.FillEllipse(&b, 0,0,100,100);
				}

				NuggetProps mat;
				//mat.m_tex_diffuse = m_rdr.m_tex_mgr.FindTexture(EStockTexture::Checker);
				mat.m_tex_diffuse = gditex;
				mat.m_rsb = RSBlock::SolidCullNone();
				auto model = ModelGenerator<>::Quad(m_rdr, 1, 1, pr::iv2Zero, pr::Colour32White, &mat);
				m_test_model.m_model = model;
				m_test_model_enable = true;

				//pr::rdr::ProjectedTexture pt;
				//pt.m_tex = m_rdr.m_tex_mgr.FindTexture(pr::rdr::EStockTexture::Checker);
				//pt.m_o2w = pr::rdr::ProjectedTexture::MakeTransform(pr::v4::make(15,0,0,1), pr::v4Origin, pr::v4YAxis, 1.0f, pr::maths::tau_by_4, 0.01f, 100.0f, false);
				//m_scene.m_render_steps[0]->as<pr::rdr::ForwardRender>().m_proj_tex.push_back(pt);
			}//*/
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
			pr::v4 verts[] =
			{
				pr::v4::make(0.0f,  0.0f,  0.0f, 1.0f),
				pr::v4::make(1.0f,  0.0f,  0.0f, 1.0f),
				pr::v4::make(0.0f,  0.0f,  0.0f, 1.0f),
				pr::v4::make(0.0f,  1.0f,  0.0f, 1.0f),
				pr::v4::make(0.0f,  0.0f,  0.0f, 1.0f),
				pr::v4::make(0.0f,  0.0f,  1.0f, 1.0f),
			};
			pr::Colour32 coloursFF[] = { 0xFFFF0000, 0xFFFF0000, 0xFF00FF00, 0xFF00FF00, 0xFF0000FF, 0xFF0000FF };
			pr::Colour32 colours80[] = { 0xFF800000, 0xFF800000, 0xFF008000, 0xFF008000, 0xFF000080, 0xFF000080 };
			pr::uint16 lines[]       = { 0, 1, 2, 3, 4, 5 };

			m_focus_point .m_model = ModelGenerator<VertPC>::Mesh(m_rdr, EPrim::LineList, PR_COUNTOF(verts), PR_COUNTOF(lines), verts, lines, PR_COUNTOF(coloursFF), coloursFF);
			m_focus_point .m_model->m_name = "focus point";
			m_focus_point .m_i2w   = pr::m4x4Identity;

			m_origin_point.m_model = ModelGenerator<VertPC>::Mesh(m_rdr, EPrim::LineList, PR_COUNTOF(verts), PR_COUNTOF(lines), verts, lines, PR_COUNTOF(colours80), colours80);
			m_origin_point.m_model->m_name = "origin point";
			m_origin_point.m_i2w   = pr::m4x4Identity;
		}
		{
			// Create the selection box model
			pr::v4 verts[] =
			{
				pr::v4::make(-0.5f, -0.5f, -0.5f, 1.0f), pr::v4::make(-0.4f, -0.5f, -0.5f, 1.0f), pr::v4::make(-0.5f, -0.4f, -0.5f, 1.0f), pr::v4::make(-0.5f, -0.5f, -0.4f, 1.0f),
				pr::v4::make(0.5f, -0.5f, -0.5f, 1.0f), pr::v4::make(0.5f, -0.4f, -0.5f, 1.0f), pr::v4::make(0.4f, -0.5f, -0.5f, 1.0f), pr::v4::make(0.5f, -0.5f, -0.4f, 1.0f),
				pr::v4::make(0.5f,  0.5f, -0.5f, 1.0f), pr::v4::make(0.4f,  0.5f, -0.5f, 1.0f), pr::v4::make(0.5f,  0.4f, -0.5f, 1.0f), pr::v4::make(0.5f,  0.5f, -0.4f, 1.0f),
				pr::v4::make(-0.5f,  0.5f, -0.5f, 1.0f), pr::v4::make(-0.5f,  0.4f, -0.5f, 1.0f), pr::v4::make(-0.4f,  0.5f, -0.5f, 1.0f), pr::v4::make(-0.5f,  0.5f, -0.4f, 1.0f),
				pr::v4::make(-0.5f, -0.5f,  0.5f, 1.0f), pr::v4::make(-0.4f, -0.5f,  0.5f, 1.0f), pr::v4::make(-0.5f, -0.4f,  0.5f, 1.0f), pr::v4::make(-0.5f, -0.5f,  0.4f, 1.0f),
				pr::v4::make(0.5f, -0.5f,  0.5f, 1.0f), pr::v4::make(0.5f, -0.4f,  0.5f, 1.0f), pr::v4::make(0.4f, -0.5f,  0.5f, 1.0f), pr::v4::make(0.5f, -0.5f,  0.4f, 1.0f),
				pr::v4::make(0.5f,  0.5f,  0.5f, 1.0f), pr::v4::make(0.4f,  0.5f,  0.5f, 1.0f), pr::v4::make(0.5f,  0.4f,  0.5f, 1.0f), pr::v4::make(0.5f,  0.5f,  0.4f, 1.0f),
				pr::v4::make(-0.5f,  0.5f,  0.5f, 1.0f), pr::v4::make(-0.5f,  0.4f,  0.5f, 1.0f), pr::v4::make(-0.4f,  0.5f,  0.5f, 1.0f), pr::v4::make(-0.5f,  0.5f,  0.4f, 1.0f),
			};
			pr::uint16 lines[] =
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
			m_selection_box.m_model = ModelGenerator<VertPC>::Mesh(m_rdr, EPrim::LineList, PR_COUNTOF(verts), PR_COUNTOF(lines), verts, lines);
			m_selection_box.m_model->m_name = "selection box";
			m_selection_box.m_i2w   = pr::m4x4Identity;
		}
		{
			// Create a bounding box model
			pr::v4 verts[] =
			{
				pr::v4::make(-0.5f, -0.5f, -0.5f, 1.0f),
				pr::v4::make(0.5f, -0.5f, -0.5f, 1.0f),
				pr::v4::make(0.5f,  0.5f, -0.5f, 1.0f),
				pr::v4::make(-0.5f,  0.5f, -0.5f, 1.0f),
				pr::v4::make(-0.5f, -0.5f,  0.5f, 1.0f),
				pr::v4::make(0.5f, -0.5f,  0.5f, 1.0f),
				pr::v4::make(0.5f,  0.5f,  0.5f, 1.0f),
				pr::v4::make(-0.5f,  0.5f,  0.5f, 1.0f),
			};
			pr::uint16 lines[] =
			{
				0, 1, 1, 2, 2, 3, 3, 0,
				4, 5, 5, 6, 6, 7, 7, 4,
				0, 4, 1, 5, 2, 6, 3, 7
			};
			m_bbox_model.m_model = ModelGenerator<VertPC>::Mesh(m_rdr, EPrim::LineList, PR_COUNTOF(verts), PR_COUNTOF(lines), verts, lines, 1, &pr::Colour32Blue);
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

	// User settings have been changed
	void Main::OnEvent(pr::ldr::Evt_SettingsChanged const&)
	{
		m_settings.m_ObjectManagerSettings = m_gui.m_store_ui.Settings();
	}

	// An object has been added to the object manager
	void Main::OnEvent(pr::ldr::Evt_LdrObjectAdd const& e)
	{
		pr::chain::Insert(m_step_objects, e.m_obj->m_step.m_link);
	}

	// An object has been deleted from the object manager
	void Main::OnEvent(pr::ldr::Evt_LdrObjectDelete const& e)
	{
		pr::chain::Remove(e.m_obj->m_step.m_link);
	}

	// The selected objects have changed
	void Main::OnEvent(pr::ldr::Evt_LdrObjectSelectionChanged const&)
	{
		// Update the transform of the selection box
		pr::BBox bbox = m_gui.m_store_ui.GetBBox(pr::ldr::EObjectBounds::Selected);
		pr::Scale4x4(m_selection_box.m_i2w, bbox.SizeX(), bbox.SizeY(), bbox.SizeZ(), bbox.Centre());

		// Request a refresh when the selection changes (if the selection box is visible)
		if (m_settings.m_ShowSelectionBox)
			pr::events::Send(ldr::Event_Refresh());
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

		// Setup the render steps (just one in this case)
		auto fr = m_scene.FindRStep<pr::rdr::ForwardRender>();
		if (fr != nullptr)
		{
			// Update the lighting. If lighting is camera relative, adjust the position and direction
			pr::rdr::Light& light = fr->m_global_light;
			light = m_settings.m_Light;
			if (m_settings.m_LightIsCameraRelative)
			{
				light.m_direction = m_cam.CameraToWorld() * m_settings.m_Light.m_direction;
				light.m_position  = m_cam.CameraToWorld() * m_settings.m_Light.m_position;
			}

			// Set the background colour
			fr->m_background_colour = m_settings.m_BackgroundColour;
		}
	}

	// Called per render step
	void Main::OnEvent(pr::rdr::Evt_RenderStepExecute const& e)
	{
		if (e.m_rstep.GetId() != pr::rdr::ERenderStep::ForwardRender)
			return;

		// Update the fill mode for the scene
		auto& fr = e.m_rstep.as<pr::rdr::ForwardRender>();
		if (m_scene_rdr_pass == 0 || e.m_complete)
		{
			fr.m_rsb.Set(pr::rdr::ERS::FillMode, D3D11_FILL_SOLID);
			fr.m_bsb.Clear(pr::rdr::EBS::BlendEnable, 0);
			fr.m_clear_bb = true;
		}
		else
		{
			fr.m_rsb.Set(pr::rdr::ERS::FillMode, D3D11_FILL_WIREFRAME);
			fr.m_bsb.Set(pr::rdr::EBS::BlendEnable, FALSE, 0);
			fr.m_clear_bb = false;
		}
	}
}