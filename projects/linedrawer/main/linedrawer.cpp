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
#include "pr/maths/maths.h"
#include "pr/filesys/filesys.h"
#include "pr/macros/count_of.h"
#include "pr/common/events.h"
	
namespace ldr
{
	char const AppName[]   = "LineDrawer";
	char const Version[]   = "3.01.00";
	char const Copyright[] = "Copyright © Rylogic Limited 2002";
	pr::ldr::ContextId LdrContext = 0xFFFFFFFF;
	char const* AppTitle()      { return AppName; }
	char const* AppString()     { return pr::FmtX<struct X>("%s - Version: %s\r\n%s\r\nAll Rights Reserved.", AppName, Version, Copyright); }
	char const* AppStringLine() { return pr::FmtX<struct X>("%s - Version: %s %s", AppName, Version, Copyright); }
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
	
// Return settings to use when creating the renderer
pr::rdr::RdrSettings GetRendererSettings(HWND hwnd, UserSettings const& user_settings, pr::rdr::Allocator& rdr_allocator, pr::IRect const& client_area)
{
	pr::rdr::RdrSettings s;
	s.m_window_handle      = hwnd;
	s.m_device_config      = pr::rdr::GetDefaultDeviceConfigWindowed(D3DDEVTYPE_HAL, D3DCREATE_MULTITHREADED);//rdr::GetDefaultDeviceConfigFullScreen(1024, 768);//
	s.m_allocator          = &rdr_allocator;
	s.m_client_area        = client_area;
	s.m_zbuffer_format     = D3DFMT_D24S8;
	s.m_swap_effect        = D3DSWAPEFFECT_DISCARD;//D3DSWAPEFFECT_COPY;// - have to use discard for antialiasing, but that means no blt during resize
	s.m_back_buffer_count  = 1;
	s.m_geometry_quality   = user_settings.m_geometry_quality;
	s.m_texture_quality    = user_settings.m_texture_quality;
	s.m_background_colour  = user_settings.m_background_colour;
	s.m_max_shader_version = user_settings.m_shader_version;
	return s;
}
	
// Return viewport settings
pr::rdr::VPSettings GetViewportSettings(pr::Renderer& rdr, pr::rdr::ViewportId id)
{
	pr::rdr::VPSettings s;
	s.m_renderer   = &rdr;
	s.m_identifier = id;
	return s;
}
	
// Main app constructor
LineDrawer::LineDrawer(LineDrawerGUI& ui, char const* cmdline)
:m_user_settings(GetUserSettingsFilename(), true)
,m_allocator()
,m_renderer(GetRendererSettings(ui.m_hWnd, m_user_settings, m_allocator, pr::ClientArea(ui.m_hWnd)))
,m_viewport0(GetViewportSettings(m_renderer, 0))
,m_viewport1(GetViewportSettings(m_renderer, 1))
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
,m_nav(m_camera, m_rdr.ClientArea(), m_user_settings.m_camera_align)
,m_store()
,m_store_ui(ui.m_hWnd)
,m_plugin_mgr(this)
,m_measure_tool_ui(LineDrawer::ReadPoint, this, m_rdr, ui.m_hWnd)
,m_angle_tool_ui(LineDrawer::ReadPoint, this, m_rdr, ui.m_hWnd)
,m_lua_src()
,m_files(m_user_settings, m_rdr, m_store, m_lua_src)
{
	// Configure the renderer
	m_viewport0.RenderState(D3DRS_CULLMODE, D3DCULL_NONE);
	m_viewport1.RenderState(D3DRS_CULLMODE, D3DCULL_NONE);
	
	// Configure the lighting
	pr::rdr::Light light;
	light.m_on = true;
	m_rdr.m_light_mgr.m_light[0] = light;
	
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
		m_focus_point .m_model = pr::rdr::model::Mesh(m_rdr, pr::rdr::model::EPrimitive::LineList, pr::geom::EVC, PR_COUNTOF(lines), PR_COUNTOF(verts), lines, verts, 0, coloursFF, 0, pr::m4x4Identity);
		m_focus_point .m_i2w   = pr::m4x4Identity;
		m_origin_point.m_model = pr::rdr::model::Mesh(m_rdr, pr::rdr::model::EPrimitive::LineList, pr::geom::EVC, PR_COUNTOF(lines), PR_COUNTOF(verts), lines, verts, 0, colours80, 0, pr::m4x4Identity);
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
		m_selection_box.m_model = pr::rdr::model::Mesh(m_rdr, pr::rdr::model::EPrimitive::LineList, pr::geom::EVC, PR_COUNTOF(lines), PR_COUNTOF(verts), lines, verts, 0, 0, 0, pr::m4x4Identity);
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
		m_bbox_model.m_model = pr::rdr::model::Mesh(m_rdr, pr::rdr::model::EPrimitive::LineList, pr::geom::EVC, PR_COUNTOF(lines), PR_COUNTOF(verts), lines, verts, 0, 0, 0, pr::m4x4Identity, pr::Colour32Blue);
		m_bbox_model.m_i2w   = pr::m4x4Identity;
	}
	{
		// Create a test point box model
		m_test_point.m_model = pr::rdr::model::Box(m_rdr, pr::v4::make(0.1f, 0.1f, 0.1f, 0.0f), pr::m4x4Identity, pr::Colour32Green);
		m_test_point.m_i2w   = pr::m4x4Identity;
	}
}
	
// Update the display
void LineDrawer::Render()
{
	// Ignore render calls if the user settings say rendering is disabled
	if (!m_user_settings.m_rendering_enabled)
		return;
		
	// Update the position of the focus point
	if (m_user_settings.m_show_focus_point)
	{
		float scale = m_user_settings.m_focus_point_scale * m_nav.FocusDistance();
		pr::Scale4x4(m_focus_point.m_i2w, scale, m_nav.FocusPoint());
	}
	
	// Update the scale of the origin
	if (m_user_settings.m_show_origin)
	{
		float scale = m_user_settings.m_focus_point_scale * pr::Length3(m_camera.CameraToWorld().pos);
		pr::Scale4x4(m_origin_point.m_i2w, scale, pr::v4Origin);
	}
	
	// Allow the navigation manager to adjust the camera, ready for this frame
	//m_nav.PositionCamera();
	
	// Update the lighting. If lighting is camera relative, adjust the position and direction
	m_rdr.m_light_mgr.m_light[0] = m_user_settings.m_light;
	if (m_user_settings.m_light_is_camera_relative)
	{
		pr::rdr::Light& light = m_rdr.m_light_mgr.m_light[0];
		light.m_direction = m_camera.CameraToWorld() * m_user_settings.m_light.m_direction;
		light.m_position  = m_camera.CameraToWorld() * m_user_settings.m_light.m_position;
	}
	
	// Set the background colour
	m_rdr.BackgroundColour(m_user_settings.m_background_colour);
	
	// Render the viewports
	if (pr::Failed(m_rdr.RenderStart()))
		return;
		
	// Render the viewport(s)
	switch (m_user_settings.m_screen_view)
	{
	default:
		PR_ASSERT(PR_DBG_LDR, false, "");
		break;
	case EScreenView::Default:
		RenderViewport(m_viewport0);
		break;
	case EScreenView::Stereo:
	{
		//// Get the distance to the focus point from the camera
		//float yaw = ATan2(m_user_settings.m_stereo_view_eye_separation, m_nav.FocusDistance());
		
		//// Render the left eye
		//m_camera.DTranslateRel(m_user_settings.m_stereo_view_eye_separation, 0.0f, 0.0f);
		//m_camera.DRotateRel(0.0f, -yaw, 0.0f);
		//RenderViewport(m_viewport0);
		//m_camera.DRotateRel(0.0f,  yaw, 0.0f);
		//m_camera.DTranslateRel(-1.0f * m_user_settings.m_stereo_view_eye_separation, 0.0f, 0.0f);
		
		//// Render the right eye
		//m_camera.DTranslateRel(-1.0f * m_user_settings.m_stereo_view_eye_separation, 0.0f, 0.0f);
		//m_camera.DRotateRel(0.0f,  yaw, 0.0f);
		//RenderViewport(m_viewport1);
		//m_camera.DRotateRel(0.0f, -yaw, 0.0f);
		//m_camera.DTranslateRel(m_user_settings.m_stereo_view_eye_separation, 0.0f, 0.0f);
	} break;
	}
	
	m_rdr.RenderEnd();
	m_rdr.Present();
}
	
// Render the view from a single viewport
void LineDrawer::RenderViewport(pr::rdr::Viewport& viewport)
{
	// Set the viewport view
	viewport.SetView(m_camera);
	
	// Add objects to the viewport
	viewport.ClearDrawlist();
	pr::events::Send(ldr::Event_AddToViewport(viewport));
	
	// Set the global wireframe mode
	switch (m_user_settings.m_global_render_mode)
	{
	case EGlobalRenderMode::Solid:        viewport.RenderState(D3DRS_FILLMODE, D3DFILL_SOLID); break;
	case EGlobalRenderMode::Wireframe:    viewport.RenderState(D3DRS_FILLMODE, D3DFILL_WIREFRAME); break;
	case EGlobalRenderMode::SolidAndWire: viewport.RenderState(D3DRS_FILLMODE, D3DFILL_SOLID); break;
	}
	
	// Render the viewport
	viewport.Render();
	
	// Render the wireframe over the top of the solid
	if (m_user_settings.m_global_render_mode == EGlobalRenderMode::SolidAndWire)
	{
		pr::rdr::rs::Block rsb_override;
		rsb_override.SetRenderState(D3DRS_ALPHABLENDENABLE, FALSE);
		rsb_override.SetRenderState(D3DRS_FILLMODE, D3DFILL_WIREFRAME);
		viewport.Render(false, rsb_override);
	}
	
	// If model space bounding boxes are enabled render those
	if (m_user_settings.m_show_object_bboxes)
	{
		viewport.ClearDrawlist();
		for (std::size_t i = 0, iend = m_store.size(); i != iend; ++i)
			m_store[i]->AddBBoxToViewport(viewport, m_bbox_model.m_model);
		viewport.Render(false);
	}
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
	m_rdr.Resize(client_area);
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
void LineDrawer::ResetView(pr::ldr::EObjectBounds::Type view_type)
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
	m_user_settings.m_objmgr_settings = m_store_ui.Settings();
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
	if (m_user_settings.m_show_selection_box)
		pr::events::Send(ldr::Event_Refresh());
}
	
// Called when the viewport is being built
void LineDrawer::OnEvent(ldr::Event_AddToViewport const& e)
{
	// Render the focus point
	if (m_user_settings.m_show_focus_point)
		e.m_viewport->AddInstance(m_focus_point);
		
	// Render the origin
	if (m_user_settings.m_show_origin)
		e.m_viewport->AddInstance(m_origin_point);
		
	// Render the test point
	if (m_test_point_enable)
		e.m_viewport->AddInstance(m_test_point);
		
	// Render the selection box
	if (m_user_settings.m_show_selection_box && m_store_ui.SelectedCount() != 0)
		e.m_viewport->AddInstance(m_selection_box);
	
	// Tools instances
	if (m_measure_tool_ui.Gfx())
		m_measure_tool_ui.Gfx()->AddToViewport(*e.m_viewport);
	if (m_angle_tool_ui.Gfx())
		m_angle_tool_ui.Gfx()->AddToViewport(*e.m_viewport);

	// Add instances from the store
	for (std::size_t i = 0, iend = m_store.size(); i != iend; ++i)
		m_store[i]->AddToViewport(*e.m_viewport);
}
	