//***************************************************************************************************
// View 3D
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************
#pragma once

#include "view3d/forward.h"

namespace view3d
{
	struct Window :pr::AlignTo<16>
		//,pr::events::IRecv<pr::ldr::Evt_Refresh>
		//,pr::events::IRecv<pr::ldr::Evt_LdrMeasureUpdate>
		//,pr::events::IRecv<pr::ldr::Evt_LdrAngleDlgUpdate>
	{
		ErrorCBStack              m_error_cb;                 // Stack of error callback functions for the dll
		View3D_SettingsChanged    m_settings_cb;              // Callback on settings changed
		View3D_RenderCB           m_render_cb;                // Callback to invoke a render
		HWND                      m_hwnd;                     // The associated window handle
		pr::Renderer&             m_rdr;                      // Reference to the renderer
		pr::rdr::Window           m_wnd;                      // The window being drawn on
		pr::rdr::Scene            m_scene;                    // Scene manager
		view3d::ObjectCont        m_objects;                  // References to objects to draw
		pr::Camera                m_camera;                   // Camera control
		pr::rdr::Light            m_light;                    // Light source for the set
		bool                      m_light_is_camera_relative; // Whether the light is attached to the camera or not
		EView3DFillMode           m_fill_mode;                // Fill mode
		pr::Colour32              m_background_colour;        // The background colour for this drawset
		view3d::Instance          m_focus_point;
		view3d::Instance          m_origin_point;
		float                     m_focus_point_size;         // The base size of the focus point object
		float                     m_origin_point_size;        // The base size of the origin instance
		bool                      m_focus_point_visible;      // True if we should draw the focus point
		bool                      m_origin_point_visible;     // True if we should draw the origin point
		pr::ldr::ScriptEditorDlg  m_editor_ui;                // Object manager for objects added to this window
		pr::ldr::ObjectManagerDlg m_obj_cont_ui;              // Object manager for objects added to this window
		pr::ldr::MeasureDlg       m_measure_tool_ui;
		pr::ldr::AngleDlg         m_angle_tool_ui;
		EditorCont                m_editors;                  // User created editors
		std::string               m_settings;                 // Allows a char const* to be returned

		Window(pr::Renderer& rdr, HWND hwnd, BOOL gdi_compat, View3D_SettingsChanged settings_cb, View3D_RenderCB render_cb)
			:m_settings_cb(settings_cb)
			,m_render_cb(render_cb)
			,m_hwnd(hwnd)
			,m_rdr(rdr)
			,m_wnd(m_rdr, Settings(hwnd, gdi_compat))
			,m_scene(m_wnd)
			,m_objects()
			,m_camera()
			,m_light()
			,m_light_is_camera_relative(true)
			,m_fill_mode(EView3DFillMode::Solid)
			,m_background_colour(pr::Colour32::make(0xFF808080))
			,m_focus_point()
			,m_origin_point()
			,m_focus_point_size(0.05f)
			,m_origin_point_size(0.05f)
			,m_focus_point_visible(false)
			,m_origin_point_visible(false)
			,m_editor_ui()
			,m_obj_cont_ui()
			,m_measure_tool_ui(ReadPoint, this, m_rdr, hwnd)
			,m_angle_tool_ui(ReadPoint, this, m_rdr, hwnd)
			,m_editors()
			,m_settings()
		{
			// Set the initial aspect ratio
			pr::iv2 client_area = m_wnd.RenderTargetSize();
			m_camera.Aspect(client_area.x / float(client_area.y));

			// The light for the scene
			m_light.m_type           = pr::rdr::ELight::Directional;
			m_light.m_on             = true;
			m_light.m_ambient        = pr::Colour32::make(0x00101010);
			m_light.m_diffuse        = pr::Colour32::make(0xFF808080);
			m_light.m_specular       = pr::Colour32::make(0x00404040);
			m_light.m_specular_power = 1000.0f;
			m_light.m_direction      = -pr::v4ZAxis;

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
			m_focus_point .m_model = pr::rdr::ModelGenerator<>::Mesh(m_rdr, pr::rdr::EPrim::LineList, PR_COUNTOF(verts), PR_COUNTOF(lines), verts, lines, PR_COUNTOF(coloursFF), coloursFF, 0, 0);
			m_focus_point .m_i2w   = pr::m4x4Identity;
			m_origin_point.m_model = pr::rdr::ModelGenerator<>::Mesh(m_rdr, pr::rdr::EPrim::LineList, PR_COUNTOF(verts), PR_COUNTOF(lines), verts, lines, PR_COUNTOF(colours80), colours80, 0, 0);
			m_origin_point.m_i2w   = pr::m4x4Identity;
		}

		// Close any window handles
		void Close()
		{
			m_editor_ui      .Close();
			m_obj_cont_ui    .Close();
			m_measure_tool_ui.Close();
			m_angle_tool_ui  .Close();
			for (auto& e : m_editors) e->Close();

			m_editor_ui      .Detach();
			m_obj_cont_ui    .Detach();
			m_measure_tool_ui.Detach();
			m_angle_tool_ui  .Detach();

			// Don't destroy 'm_hwnd' because it doesn't belong to us,
			// we're simply drawing on that window. Signal close by nulling it;
			m_hwnd = 0;
		}

		static pr::rdr::WndSettings Settings(HWND hwnd, BOOL gdi_compat)
		{
			if (hwnd == 0) throw pr::Exception<HRESULT>(E_FAIL, "Provided window handle is null");
			RECT rect; ::GetClientRect(hwnd, &rect);
			return pr::rdr::WndSettings(hwnd, TRUE, gdi_compat, pr::To<pr::iv2>(rect));
		}

		// 'ctx' should be a Drawset
		// Return the focus point of the camera in this drawset
		static pr::v4 __stdcall ReadPoint(void* ctx)
		{
			if (ctx == 0) return pr::v4Origin;
			return static_cast<Window const*>(ctx)->m_camera.FocusPoint();
		}

		// Invoke the settings changed callback
		void NotifySettingsChanged()
		{
			if (m_settings_cb) return;
			m_settings_cb(this);
		}

	private:
		Window(Window const&);
		Window& operator=(Window const&);

		//// Event handlers
		//void OnEvent(pr::ldr::Evt_Refresh const&) override { m_render_cb(); }
		//void OnEvent(pr::ldr::Evt_LdrMeasureUpdate const&) override { m_render_cb(); }
		//void OnEvent(pr::ldr::Evt_LdrAngleDlgUpdate const&) override { m_render_cb(); }
	};
}
