//***************************************************************************************************
// View 3D
//  Copyright © Rylogic Ltd 2009
//***************************************************************************************************
#pragma once
#ifndef PR_RDR_RENDERER_INSTANCE_H
#define PR_RDR_RENDERER_INSTANCE_H

#include <set>
#include "pr/macros/count_of.h"
#include "pr/maths/maths.h"
#include "pr/camera/camera.h"
#include "pr/script/embedded_lua.h"
#include "pr/renderer/renderer.h"
#include "pr/renderer/instances/instance.h"
#include "pr/linedrawer/ldr_object.h"
#include "pr/linedrawer/ldr_objects_dlg.h"
#include "pr/linedrawer/ldr_tools.h"
#include "pr/view3d/view3d.h"

namespace view3d
{
	typedef std::set<View3DObject>  ObjectCont;
	typedef std::set<View3DDrawset> DrawsetCont;

	PR_RDR_DECLARE_INSTANCE_TYPE2
	(
		Instance
		,pr::rdr::ModelPtr ,m_model ,pr::rdr::instance::ECpt_ModelPtr
		,pr::m4x4          ,m_i2w   ,pr::rdr::instance::ECpt_I2WTransform
	);

	struct Drawset
	{
		ObjectCont                m_objects;                  // References to objects to draw in this drawset
		pr::Camera                m_camera;                   // Camera control
		pr::rdr::Light            m_light;                    // Light source for the set
		bool                      m_light_is_camera_relative; // Whether the light is attached to the camera or not
		EView3DRenderMode::Type   m_render_mode;              // Render mode
		pr::Colour32              m_background_colour;        // The background colour for this drawset
		bool                      m_focus_point_visible;      // True if we should draw the focus point
		float                     m_focus_point_size;         // The base size of the focus point object
		bool                      m_origin_point_visible;     // True if we should draw the origin point
		float                     m_origin_point_size;        // The base size of the origin instance

		Drawset();
	};

	// The renderer and related components
	// This object owns the drawsets and instances.
	// References to instances are added/removed to/from drawsets
	struct RendererInstance
		:pr::events::IRecv<pr::ldr::Evt_Refresh>
		,pr::events::IRecv<pr::ldr::Evt_LdrMeasureUpdate>
		,pr::events::IRecv<pr::ldr::Evt_LdrAngleDlgUpdate>
	{
		pr::rdr::Allocator         m_allocator;
		pr::Renderer               m_renderer;
		pr::rdr::Viewport          m_viewport;
		pr::ldr::ObjectCont        m_scene;
		pr::ldr::ObjectManagerDlg  m_scene_ui;
		pr::ldr::MeasureDlg        m_measure_tool_ui;
		pr::ldr::AngleDlg          m_angle_tool_ui;
		pr::script::EmbeddedLua    m_lua;
		DrawsetCont                m_drawset;
		Drawset*                   m_last_drawset;
		view3d::Instance           m_focus_point;
		view3d::Instance           m_origin_point;
		View3D_ReportErrorCB       m_error_cb;
		View3D_SettingsChanged     m_settings_changed_cb;

		RendererInstance(HWND hwnd, D3DDEVTYPE device_type, pr::uint d3dcreate_flags, View3D_ReportErrorCB error_cb, View3D_SettingsChanged settings_changed_cb);
		~RendererInstance();
		void CreateStockObjects();

		// Event handlers
		void OnEvent(pr::ldr::Evt_Refresh const&);
		void OnEvent(pr::ldr::Evt_LdrMeasureUpdate const&);
		void OnEvent(pr::ldr::Evt_LdrAngleDlgUpdate const&);
	};
}

#endif
