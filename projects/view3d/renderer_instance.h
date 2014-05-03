//***************************************************************************************************
// View 3D
//  Copyright © Rylogic Ltd 2009
//***************************************************************************************************
#pragma once

#include "view3d/forward.h"
#include "pr/view3d/view3d.h"

namespace view3d
{
	typedef std::set<View3DObject>  ObjectCont;
	typedef std::set<View3DDrawset> DrawsetCont;

	#define PR_RDR_INST(x)\
		x(pr::m4x4          ,m_i2w   ,pr::rdr::EInstComp::I2WTransform)\
		x(pr::rdr::ModelPtr ,m_model ,pr::rdr::EInstComp::ModelPtr)
	PR_RDR_DEFINE_INSTANCE(Instance, PR_RDR_INST)
	#undef PR_RDR_INST

	struct Drawset :pr::AlignTo<16>
	{
		ObjectCont      m_objects;                  // References to objects to draw in this drawset
		pr::Camera      m_camera;                   // Camera control
		pr::rdr::Light  m_light;                    // Light source for the set
		bool            m_light_is_camera_relative; // Whether the light is attached to the camera or not
		EView3DFillMode m_fill_mode;                // Fill mode
		pr::Colour32    m_background_colour;        // The background colour for this drawset
		bool            m_focus_point_visible;      // True if we should draw the focus point
		float           m_focus_point_size;         // The base size of the focus point object
		bool            m_origin_point_visible;     // True if we should draw the origin point
		float           m_origin_point_size;        // The base size of the origin instance

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
		pr::Renderer              m_renderer;
		pr::rdr::Scene            m_scene;
		pr::ldr::ObjectCont       m_obj_cont;
		pr::ldr::ObjectManagerDlg m_obj_cont_ui;
		pr::ldr::MeasureDlg       m_measure_tool_ui;
		pr::ldr::AngleDlg         m_angle_tool_ui;
		pr::script::EmbeddedLua   m_lua;
		DrawsetCont               m_drawset;
		Drawset*                  m_last_drawset;
		view3d::Instance          m_focus_point;
		view3d::Instance          m_origin_point;

		explicit RendererInstance(HWND hwnd);
		~RendererInstance();
		void CreateStockObjects();

		// Event handlers
		void OnEvent(pr::ldr::Evt_Refresh const&) override;
		void OnEvent(pr::ldr::Evt_LdrMeasureUpdate const&) override;
		void OnEvent(pr::ldr::Evt_LdrAngleDlgUpdate const&) override;
	};
}
