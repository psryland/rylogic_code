//***************************************************************************************************
// View 3D
//  Copyright © Rylogic Ltd 2009
//***************************************************************************************************

#include "stdafx.h"
#include "view3d/renderer_instance.h"

using namespace view3d;

// Return default settings for the renderer
pr::rdr::RdrSettings GetRdrSettings(HWND hwnd)//pr::rdr::Allocator& allocator, D3DDEVTYPE device_type, pr::uint d3dcreate_flags)
{
	RECT rect; ::GetClientRect(hwnd, &rect);
	pr::rdr::RdrSettings s(hwnd, TRUE, pr::To<pr::iv2>(rect));
	return s;
}

// 'ctx' should be a Drawset
// Return the focus point of the camera in this drawset
pr::v4 __stdcall ReadPoint(void* ctx)
{
	if (ctx == 0) return pr::v4Origin;
	return static_cast<Drawset const*>(ctx)->m_camera.FocusPoint();
}

// RendererInstance ***********************************
RendererInstance::RendererInstance(HWND hwnd, View3D_ReportErrorCB error_cb, View3D_SettingsChanged settings_changed_cb)
	:pr::events::IRecv<pr::ldr::Evt_Refresh>(false)
	,pr::events::IRecv<pr::ldr::Evt_LdrMeasureUpdate>(false)
	,pr::events::IRecv<pr::ldr::Evt_LdrAngleDlgUpdate>(false)
	,m_renderer(GetRdrSettings(hwnd))
	,m_scene(m_renderer)
	,m_obj_cont()
	,m_obj_cont_ui(hwnd)
	,m_measure_tool_ui(ReadPoint, 0, m_renderer, hwnd)
	,m_angle_tool_ui(ReadPoint, 0, m_renderer, hwnd)
	,m_lua()
	,m_drawset()
	,m_last_drawset(0)
	,m_focus_point()
	,m_origin_point()
	,m_error_cb(error_cb)
	,m_settings_changed_cb(settings_changed_cb)
{
	m_obj_cont_ui.IgnoreContextId(pr::ldr::LdrMeasurePrivateContextId, true);
	m_obj_cont_ui.IgnoreContextId(pr::ldr::LdrAngleDlgPrivateContextId, true);

	// Sign up for events now
	static_cast<pr::events::IRecv<pr::ldr::Evt_Refresh         >&>(*this).subscribe();
	static_cast<pr::events::IRecv<pr::ldr::Evt_LdrMeasureUpdate>&>(*this).subscribe();
}

RendererInstance::~RendererInstance()
{
	// Clean up drawsets
	for (DrawsetCont::iterator i = m_drawset.begin(), i_end = m_drawset.end(); i != i_end; ++i)
		delete *i;
}

void RendererInstance::CreateStockObjects()
{
	// Create the focus point/origin models
	pr::v4 verts[] =
	{
		pr::v4::make( 0.0f,  0.0f,  0.0f, 1.0f),
		pr::v4::make( 1.0f,  0.0f,  0.0f, 1.0f),
		pr::v4::make( 0.0f,  0.0f,  0.0f, 1.0f),
		pr::v4::make( 0.0f,  1.0f,  0.0f, 1.0f),
		pr::v4::make( 0.0f,  0.0f,  0.0f, 1.0f),
		pr::v4::make( 0.0f,  0.0f,  1.0f, 1.0f),
	};
	pr::Colour32 coloursFF[] = { 0xFFFF0000, 0xFFFF0000, 0xFF00FF00, 0xFF00FF00, 0xFF0000FF, 0xFF0000FF };
	pr::Colour32 colours80[] = { 0xFF800000, 0xFF800000, 0xFF008000, 0xFF008000, 0xFF000080, 0xFF000080 };
	pr::uint16 lines[]       = { 0, 1, 2, 3, 4, 5 };
	m_focus_point .m_model = pr::rdr::ModelGenerator<pr::rdr::VertPC>::Mesh(m_renderer, pr::rdr::EPrim::LineList, PR_COUNTOF(verts), PR_COUNTOF(lines), verts, lines, PR_COUNTOF(coloursFF), coloursFF, 0, 0);
	m_focus_point .m_i2w   = pr::m4x4Identity;
	m_origin_point.m_model = pr::rdr::ModelGenerator<pr::rdr::VertPC>::Mesh(m_renderer, pr::rdr::EPrim::LineList, PR_COUNTOF(verts), PR_COUNTOF(lines), verts, lines, PR_COUNTOF(colours80), colours80, 0, 0);
	m_origin_point.m_i2w   = pr::m4x4Identity;
}

// Event handlers
void RendererInstance::OnEvent(pr::ldr::Evt_Refresh const&)
{
	View3D_Refresh();
}
void RendererInstance::OnEvent(pr::ldr::Evt_LdrMeasureUpdate const&)
{
	View3D_Refresh();
}
void RendererInstance::OnEvent(pr::ldr::Evt_LdrAngleDlgUpdate const&)
{
	View3D_Refresh();
}

// Drawset ***********************************
Drawset::Drawset()
	:m_objects()
	,m_camera()
	,m_light()
	,m_light_is_camera_relative(true)
	,m_render_mode(EView3DRenderMode::Solid)
	,m_background_colour(pr::Colour32::make(0xFF808080))
	,m_focus_point_visible(false)
	,m_focus_point_size(0.05f)
	,m_origin_point_visible(false)
	,m_origin_point_size(0.05f)
{
	m_light.m_type           = pr::rdr::ELight::Directional;
	m_light.m_on             = true;
	m_light.m_ambient        = pr::Colour32::make(0x00101010);
	m_light.m_diffuse        = pr::Colour32::make(0xFF808080);
	m_light.m_specular       = pr::Colour32::make(0x00404040);
	m_light.m_specular_power = 1000.0f;
	m_light.m_direction      = -pr::v4ZAxis;
}
