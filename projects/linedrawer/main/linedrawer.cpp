//*****************************************************************************************
// LineDrawer
//  Copyright © Rylogic Ltd 2009
//*****************************************************************************************
#include "linedrawer/main/stdafx.h"
#include "linedrawer/main/linedrawer.h"
#include "linedrawer/types/ldrevent.h"
#include "linedrawer/types/ldrexception.h"
#include "linedrawer/gui/linedrawergui.h"
#include "linedrawer/utility/misc.h"
#include "linedrawer/utility/debug.h"
#include "pr/linedrawer/ldr_object.h"

namespace ldr
{
	char const AppName[]   = "Linedrawer";
	char const Version[]   = "4.00.00";
	char const Copyright[] = "Copyright © Rylogic Limited 2002";
	pr::ldr::ContextId LdrContext = 0xFFFFFFFF;
	char const* AppTitle()      { return AppName; }
	char const* AppString()     { return pr::FmtX<LineDrawer, 128>("%s - Version: %s\r\n%s\r\nAll Rights Reserved.", AppName, Version, Copyright); }
	char const* AppStringLine() { return pr::FmtX<LineDrawer, 128>("%s - Version: %s %s", AppName, Version, Copyright); }
}

// Return the filename for the user settings file
std::string GetUserSettingsFilename()
{
	// Determine the directory we're running in
	char temp[MAX_PATH];
	GetModuleFileNameA(0, temp, MAX_PATH);
	std::string path = temp;
	pr::filesys::RmvExtension(path);
	path += ".ini";
	return path;
}

// Main app constructor
LineDrawer::LineDrawer(LineDrawerGUI& ui, char const* cmdline)
	:m_user_settings(GetUserSettingsFilename(), true)
	,m_renderer(pr::rdr::RdrSettings(ui.m_hWnd, TRUE, pr::To<pr::iv2>(pr::ClientArea(ui.m_hWnd))))
	,m_scene(m_renderer)
	,m_camera()
	,m_step_objects()
	,m_focus_point()
	,m_origin_point()
	,m_selection_box()
	,m_bbox_model()
	,m_test_point()
	,m_test_point_enable(false)
	,m_ui(ui)
	,m_rdr(m_renderer)
	,m_nav(m_camera, pr::To<pr::IRect>(m_rdr.DisplayArea()), m_user_settings.m_CameraAlignAxis)
	,m_store()
	,m_store_ui(ui.m_hWnd)
	,m_plugin_mgr(this)
	,m_measure_tool_ui(LineDrawer::ReadPoint, this, m_rdr, ui.m_hWnd)
	,m_angle_tool_ui(LineDrawer::ReadPoint, this, m_rdr, ui.m_hWnd)
	,m_lua_src()
	,m_files(m_user_settings, m_rdr, m_store, m_lua_src)
{
	// Configure the lighting
	pr::rdr::Light light;
	light.m_on = true;
	m_scene.m_global_light = light;

	// Ignore internal context ids
	m_store_ui.IgnoreContextId(pr::ldr::LdrMeasurePrivateContextId, true);
	m_store_ui.IgnoreContextId(pr::ldr::LdrAngleDlgPrivateContextId, true);

	// Create stock models such as the focus point, origin, selection box, etc
	CreateStockModels();

	// Parse the command line
	pr::EnumCommandLine(cmdline, *this);
}

// Main app destructor
LineDrawer::~LineDrawer()
{
	m_user_settings.Save();
}

// Callback function for reading a point in world space
pr::v4 __stdcall LineDrawer::ReadPoint(void* ctx)
{
	return static_cast<LineDrawer*>(ctx)->m_camera.FocusPoint();
}

// Return access to the user settings
UserSettings const& LineDrawer::Settings() const
{
	return m_user_settings;
}
UserSettings& LineDrawer::Settings()
{
	return m_user_settings;
}

// Create stock models such as the focus point, origin, etc
void LineDrawer::CreateStockModels()
{
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
		m_focus_point .m_model = pr::rdr::ModelGenerator<pr::rdr::VertPC>::Mesh(m_rdr, pr::rdr::EPrim::LineList, PR_COUNTOF(verts), PR_COUNTOF(lines), verts, lines, PR_COUNTOF(coloursFF), coloursFF);
		m_focus_point .m_i2w   = pr::m4x4Identity;
		m_origin_point.m_model = pr::rdr::ModelGenerator<pr::rdr::VertPC>::Mesh(m_rdr, pr::rdr::EPrim::LineList, PR_COUNTOF(verts), PR_COUNTOF(lines), verts, lines, PR_COUNTOF(colours80), colours80);
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
		m_selection_box.m_model = pr::rdr::ModelGenerator<pr::rdr::VertPC>::Mesh(m_rdr, pr::rdr::EPrim::LineList, PR_COUNTOF(verts), PR_COUNTOF(lines), verts, lines);
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
		m_bbox_model.m_model = pr::rdr::ModelGenerator<pr::rdr::VertPC>::Mesh(m_rdr, pr::rdr::EPrim::LineList, PR_COUNTOF(verts), PR_COUNTOF(lines), verts, lines, 1, &pr::Colour32Blue);
		m_bbox_model.m_i2w   = pr::m4x4Identity;
	}
	{
		// Create a test point box model
		m_test_point.m_model = pr::rdr::ModelGenerator<>::Box(m_rdr, 0.1f, pr::m4x4Identity, pr::Colour32Green);
		m_test_point.m_i2w   = pr::m4x4Identity;
	}
}

// Update the display
void LineDrawer::Render()
{
	// Ignore render calls if the user settings say rendering is disabled
	if (!m_user_settings.m_RenderingEnabled)
		return;

	// Update the position of the focus point
	if (m_user_settings.m_ShowFocusPoint)
	{
		float scale = m_user_settings.m_FocusPointScale * m_nav.FocusDistance();
		pr::Scale4x4(m_focus_point.m_i2w, scale, m_nav.FocusPoint());
	}

	// Update the scale of the origin
	if (m_user_settings.m_ShowOrigin)
	{
		float scale = m_user_settings.m_FocusPointScale * pr::Length3(m_camera.CameraToWorld().pos);
		pr::Scale4x4(m_origin_point.m_i2w, scale, pr::v4Origin);
	}

	// Allow the navigation manager to adjust the camera, ready for this frame
	//m_nav.PositionCamera();

	// Update the lighting. If lighting is camera relative, adjust the position and direction
	m_scene.m_global_light = m_user_settings.m_Light;
	if (m_user_settings.m_LightIsCameraRelative)
	{
		pr::rdr::Light& light = m_scene.m_global_light;
		light.m_direction = m_camera.CameraToWorld() * m_user_settings.m_Light.m_direction;
		light.m_position  = m_camera.CameraToWorld() * m_user_settings.m_Light.m_position;
	}

	// Set the background colour
	m_scene.m_background_colour = m_user_settings.m_BackgroundColour;

	// Add objects to the viewport
	m_scene.ClearDrawlist();
	m_scene.UpdateDrawlist();

	// Set the camera view
	m_scene.SetView(m_camera);

	// Update the fill mode for the scene
	switch (m_user_settings.m_GlobalRenderMode) {
	case EGlobalRenderMode::Solid:        m_scene.m_rsb.Set(pr::rdr::ERS::FillMode, D3D11_FILL_SOLID); break;
	case EGlobalRenderMode::Wireframe:    m_scene.m_rsb.Set(pr::rdr::ERS::FillMode, D3D11_FILL_WIREFRAME); break;
	case EGlobalRenderMode::SolidAndWire: m_scene.m_rsb.Set(pr::rdr::ERS::FillMode, D3D11_FILL_SOLID); break;
	}

	// Render the scene
	m_scene.Render();

	// Render wire frame over solid
	if (m_user_settings.m_GlobalRenderMode == EGlobalRenderMode::SolidAndWire)
	{
		m_scene.m_rsb.Set(pr::rdr::ERS::FillMode, D3D11_FILL_WIREFRAME);
		m_scene.m_bsb.Set(pr::rdr::EBS::BlendEnable, FALSE, 0);

		m_scene.Render(false);

		m_scene.m_rsb.Clear(pr::rdr::ERS::FillMode);
		m_scene.m_bsb.Clear(pr::rdr::EBS::BlendEnable, 0);
	}

	//// If model space bounding boxes are enabled render those
	//if (m_user_settings.m_show_object_bboxes)
	//{
	//	m_scene.ClearDrawlist();
	//	for (std::size_t i = 0, iend = m_store.size(); i != iend; ++i)
	//		m_store[i]->AddBBoxToViewport(viewport, m_bbox_model.m_model);
	//	m_scene.Render(false);
	//}

	m_rdr.Present();
}

// Parse command line options
bool LineDrawer::CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end)
{
	// Syntax: Linedrawer -plugin "c:\myplugin.dll" arg1 arg2
	if (pr::str::EqualI(option, "-plugin") && arg != arg_end)
	{
		std::string plugin_name = *arg;
		std::string plugin_args = "";
		for (++arg; arg != arg_end && !pr::cmdline::IsOption(*arg); ++arg) plugin_args += *arg;
		try { m_plugin_mgr.Add(plugin_name.c_str(), plugin_args.c_str()); }
		catch (LdrException const& e) { pr::events::Send(ldr::Event_Error(pr::Fmt("Failed to load plugin %s.\nReason: %s", plugin_name.c_str(), e.what()))); }
		return true;
	}
	return false;
}

// Set the area and view of the output
void LineDrawer::SetViewArea(pr::IRect client_area)
{
	m_user_settings.Save();
	m_nav.SetViewArea(client_area);
	m_rdr.Resize(pr::To<pr::iv2>(client_area));
}

// Reload all data
void LineDrawer::ReloadSourceData()
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
			pr::events::Send(ldr::Event_Error(pr::FmtS("Error found while reloading source data.\nError details: %s", e.m_msg.c_str())));
			break;
		case ELdrException::OperationCancelled:
			pr::events::Send(ldr::Event_Info(pr::FmtS("Reloading data cancelled")));
			break;
		}
	}
}

// Test point methods
void LineDrawer::TestPoint_Enable(bool yes)
{
	m_test_point_enable = yes;
}
void LineDrawer::TestPoint_SetPosition(pr::v4 const& pos)
{
	m_test_point.m_i2w.pos = pos;
}

// Reset the camera to view all, selected, or visible objects
void LineDrawer::ResetView(pr::ldr::EObjectBounds view_type)
{
	m_nav.ResetView(m_store_ui.GetBBox(view_type));
}

// Generate a scene containing the supported line drawer objects.
void LineDrawer::CreateDemoScene()
{
	try
	{
		std::string scene = pr::ldr::CreateDemoScene();
		pr::ldr::AddString(m_rdr, scene.c_str(), m_store, pr::ldr::DefaultContext, false, 0, &m_lua_src);
	}
	catch (pr::script::Exception const& e) { pr::events::Send(ldr::Event_Error(pr::FmtS("Error found while parsing demo scene\nError details: %s", e.m_msg.c_str()))); }
	catch (LdrException const& e)          { pr::events::Send(ldr::Event_Error(pr::FmtS("Error found while parsing demo scene\nError details: %s", e.m_msg.c_str()))); }
}

// Event handlers ******************************
// User settings have been changed
void LineDrawer::OnEvent(pr::ldr::Evt_SettingsChanged const&)
{
	m_user_settings.m_ObjectManagerSettings = m_store_ui.Settings();
}

// An object has been added to the object manager
void LineDrawer::OnEvent(pr::ldr::Evt_LdrObjectAdd const& e)
{
	pr::chain::Insert(m_step_objects, e.m_obj->m_step.m_link);
}

// An object has been deleted from the object manager
void LineDrawer::OnEvent(pr::ldr::Evt_LdrObjectDelete const& e)
{
	pr::chain::Remove(e.m_obj->m_step.m_link);
}

// The selected objects have changed
void LineDrawer::OnEvent(pr::ldr::Evt_LdrObjectSelectionChanged const&)
{
	// Update the transform of the selection box
	pr::BoundingBox bbox = m_store_ui.GetBBox(pr::ldr::EObjectBounds::Selected);
	pr::Scale4x4(m_selection_box.m_i2w, bbox.SizeX(), bbox.SizeY(), bbox.SizeZ(), bbox.Centre());

	// Request a refresh when the selection changes (if the selection box is visible)
	if (m_user_settings.m_ShowSelectionBox)
		pr::events::Send(ldr::Event_Refresh());
}

// Called when the viewport is being built
void LineDrawer::OnEvent(pr::rdr::Evt_SceneRender const& e)
{
	// Render the focus point
	if (m_user_settings.m_ShowFocusPoint)
		e.m_scene->AddInstance(m_focus_point);

	// Render the origin
	if (m_user_settings.m_ShowOrigin)
		e.m_scene->AddInstance(m_origin_point);

	// Render the test point
	if (m_test_point_enable)
		e.m_scene->AddInstance(m_test_point);

	// Render the selection box
	if (m_user_settings.m_ShowSelectionBox && m_store_ui.SelectedCount() != 0)
		e.m_scene->AddInstance(m_selection_box);

	// Tools instances
	if (m_measure_tool_ui.Gfx())
		m_measure_tool_ui.Gfx()->AddToScene(*e.m_scene);
	if (m_angle_tool_ui.Gfx())
		m_angle_tool_ui.Gfx()->AddToScene(*e.m_scene);

	// Add instances from the store
	for (std::size_t i = 0, iend = m_store.size(); i != iend; ++i)
		m_store[i]->AddToScene(*e.m_scene);
}
