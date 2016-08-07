//***************************************************************************************************
// View 3D
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************
#pragma once

#include "view3d/forward.h"

namespace view3d
{
	struct Window :pr::AlignTo<16>
	{
		ErrorCBStack                m_error_cb;                 // Stack of error callback functions for the dll
		HWND                        m_hwnd;                     // The associated window handle
		pr::Renderer&               m_rdr;                      // Reference to the renderer
		pr::rdr::Window             m_wnd;                      // The window being drawn on
		pr::rdr::Scene              m_scene;                    // Scene manager
		view3d::ObjectCont          m_objects;                  // References to objects to draw
		view3d::GizmoCont           m_gizmos;                   // References to gizmos to draw
		pr::Camera                  m_camera;                   // Camera control
		pr::rdr::Light              m_light;                    // Light source for the set
		bool                        m_light_is_camera_relative; // Whether the light is attached to the camera or not
		EView3DFillMode             m_fill_mode;                // Fill mode
		pr::Colour32                m_background_colour;        // The background colour for this draw set
		view3d::Instance            m_focus_point;
		view3d::Instance            m_origin_point;
		float                       m_focus_point_size;         // The base size of the focus point object
		float                       m_origin_point_size;        // The base size of the origin instance
		bool                        m_focus_point_visible;      // True if we should draw the focus point
		bool                        m_origin_point_visible;     // True if we should draw the origin point
		pr::ldr::ScriptEditorUI     m_editor_ui;                // Object manager for objects added to this window
		pr::ldr::LdrObjectManagerUI m_obj_cont_ui;              // Object manager for objects added to this window
		pr::ldr::LdrMeasureUI       m_measure_tool_ui;
		pr::ldr::LdrAngleUI         m_angle_tool_ui;
		EditorCont                  m_editors;                  // User created editors
		std::string                 m_settings;                 // Allows a char const* to be returned

		// Default window construction settings
		static pr::rdr::WndSettings Settings(HWND hwnd, View3DWindowOptions const& opts)
		{
			if (hwnd == 0)
				throw pr::Exception<HRESULT>(E_FAIL, "Provided window handle is null");

			RECT rect;
			::GetClientRect(hwnd, &rect);

			auto settings        = pr::rdr::WndSettings(hwnd, true, opts.m_gdi_compatible != 0, pr::To<pr::iv2>(rect));
			settings.m_multisamp = pr::rdr::MultiSamp(opts.m_multisampling);
			settings.m_name      = opts.m_dbg_name;
			return settings;
		}
		Window* this_() { return this; }

		Window(pr::Renderer& rdr, HWND hwnd, View3DWindowOptions const& opts)
			:m_error_cb({pr::StaticCallBack(opts.m_error_cb, opts.m_error_cb_ctx)})
			,m_hwnd(hwnd)
			,m_rdr(rdr)
			,m_wnd(m_rdr, Settings(hwnd, opts))
			,m_scene(m_wnd)
			,m_objects()
			,m_camera()
			,m_light()
			,m_light_is_camera_relative(true)
			,m_fill_mode(EView3DFillMode::Solid)
			,m_background_colour(0xFF808080U)
			,m_focus_point()
			,m_origin_point()
			,m_focus_point_size(0.05f)
			,m_origin_point_size(0.05f)
			,m_focus_point_visible(false)
			,m_origin_point_visible(false)
			,m_editor_ui(hwnd)
			,m_obj_cont_ui(hwnd)
			,m_measure_tool_ui(hwnd, ReadPoint, this_(), m_rdr)
			,m_angle_tool_ui(hwnd, ReadPoint, this_(), m_rdr)
			,m_editors()
			,m_settings()
		{
			// Set the initial aspect ratio
			pr::iv2 client_area = m_wnd.RenderTargetSize();
			m_camera.Aspect(client_area.x / float(client_area.y));

			// The light for the scene
			m_light.m_type           = pr::rdr::ELight::Directional;
			m_light.m_on             = true;
			m_light.m_ambient        = pr::Colour32(0x00101010U);
			m_light.m_diffuse        = pr::Colour32(0xFF808080U);
			m_light.m_specular       = pr::Colour32(0x00404040U);
			m_light.m_specular_power = 1000.0f;
			m_light.m_direction      = -pr::v4ZAxis;

			// Create the focus point/origin models
			auto cdata = pr::rdr::MeshCreationData()
				.verts({
					pr::v4( 0.0f,  0.0f,  0.0f, 1.0f),
					pr::v4( 1.0f,  0.0f,  0.0f, 1.0f),
					pr::v4( 0.0f,  0.0f,  0.0f, 1.0f),
					pr::v4( 0.0f,  1.0f,  0.0f, 1.0f),
					pr::v4( 0.0f,  0.0f,  0.0f, 1.0f),
					pr::v4( 0.0f,  0.0f,  1.0f, 1.0f)})
				.indices({ 0, 1, 2, 3, 4, 5 })
				.nuggets({ pr::rdr::NuggetProps(pr::rdr::EPrim::LineList, pr::rdr::EGeom::Vert|pr::rdr::EGeom::Colr) });

			cdata.colours({ 0xFFFF0000, 0xFFFF0000, 0xFF00FF00, 0xFF00FF00, 0xFF0000FF, 0xFF0000FF });
			m_focus_point .m_model = pr::rdr::ModelGenerator<>::Mesh(m_rdr, cdata);
			m_focus_point .m_i2w   = pr::m4x4Identity;
			
			cdata.colours({ 0xFF800000, 0xFF800000, 0xFF008000, 0xFF008000, 0xFF000080, 0xFF000080 });
			m_origin_point.m_model = pr::rdr::ModelGenerator<>::Mesh(m_rdr, cdata);
			m_origin_point.m_i2w   = pr::m4x4Identity;
		}
		Window(Window const&) = delete;
		Window& operator=(Window const&) = delete;
		~Window()
		{
			assert("Error callback stack is in consistent. Number of pushes != number of pops" && m_error_cb.size() == 1U);
			if (!m_error_cb.empty())
				m_error_cb.pop_back();
		}

		// Settings changed event
		pr::MultiCast<SettingsChangedCB> OnSettingsChanged;

		// Push/Pop error callbacks from the error callback stack
		void PushErrorCB(View3D_ReportErrorCB cb, void* ctx)
		{
			m_error_cb.emplace_back(ReportErrorCB(cb, ctx));
		}
		void PopErrorCB(View3D_ReportErrorCB cb)
		{
			if (m_error_cb.empty())
				throw std::exception("Error callback stack is empty, cannot pop");
			if (m_error_cb.back().m_cb != cb)
				throw std::exception("Attempt to pop an error callback that is not the most recently pushed callback. This is likely a destruction order probably");

			m_error_cb.pop_back();
		}

		// Close any window handles
		void Close()
		{
			// Don't destroy 'm_hwnd' because it doesn't belong to us,
			// we're simply drawing on that window. Signal close by setting it to null
			m_hwnd = 0;
		}

		// 'ctx' should be a Draw set
		// Return the focus point of the camera in this draw set
		static pr::v4 __stdcall ReadPoint(void* ctx)
		{
			if (ctx == 0) return pr::v4Origin;
			return static_cast<Window const*>(ctx)->m_camera.FocusPoint();
		}

		// Convert a screen space point to a normalised screen space point
		pr::v2 SSPointToNSSPoint(pr::v2 const& ss_point) const
		{
			return m_scene.m_viewport.SSPointToNSSPoint(ss_point);
		}
		pr::v2 NSSPointToSSPoint(pr::v2 const& nss_point) const
		{
			return m_scene.m_viewport.NSSPointToSSPoint(nss_point);
		}

		// Invoke the settings changed callback
		void NotifySettingsChanged()
		{
			OnSettingsChanged.Raise(this);
		}

		// Call InvalidateRect on the HWND associated with this window
		void InvalidateRect(RECT const* rect, bool erase)
		{
			::InvalidateRect(m_hwnd, rect, erase);
		}
	};
}
