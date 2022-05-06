//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "view3d-12/src/dll/dll_forward.h"
#include "pr/view3d-12/main/window.h"
#include "pr/view3d-12/instance/instance.h"
#include "pr/view3d-12/scene/scene.h"

namespace pr::rdr12
{
	struct V3dWindow
	{
		// Notes:
		//  - Combines a renderer Window with a collection of LdrObjects

		// Renderer window/scene
		Context* m_dll;   // The dll context
		HWND     m_hwnd;  // The associated Win32 window handle
		Window   m_wnd;   // The renderer window
		Scene    m_scene; // Use one scene for the window

		// Objects
		ObjectSet m_objects; // References to objects to draw (note: objects are owned by the context, not the window)
		GizmoSet  m_gizmos;  // References to gizmos to draw (note: objects are owned by the context, not the window)
		GuidSet   m_guids;   // The context ids added to this window

		// Stock objects
		#define PR_RDR_INST(x)\
			x(m4x4     ,m_i2w   ,EInstComp::I2WTransform)\
			x(ModelPtr ,m_model ,EInstComp::ModelPtr)\
			x(Colour32 ,m_tint  ,EInstComp::TintColour32)
		PR_RDR12_DEFINE_INSTANCE(Instance, PR_RDR_INST) // An instance type for other models used in LDraw
		#undef PR_RDR_INST
		#define PR_RDR_INST(x)\
			x(m4x4     ,m_c2s   ,EInstComp::C2STransform)\
			x(m4x4     ,m_i2w   ,EInstComp::I2WTransform)\
			x(ModelPtr ,m_model ,EInstComp::ModelPtr)\
			x(Colour32 ,m_tint  ,EInstComp::TintColour32)
		PR_RDR12_DEFINE_INSTANCE(PointInstance, PR_RDR_INST) // An instance type for the focus point and origin point models
		#undef PR_RDR_INST
		PointInstance m_focus_point;   // Focus point graphics
		PointInstance m_origin_point;  // Origin point graphics
		Instance      m_bbox_model;    // Bounding box graphics
		Instance      m_selection_box; // Selection box graphics

		// Misc
		mutable pr::BBox m_bbox_scene;     // Bounding box for all objects in the scene (Lazy updated)
		std::thread::id  m_main_thread_id; // The thread that created this window

		V3dWindow(HWND hwnd, Context& context, view3d::WindowOptions const& opts);
		V3dWindow(V3dWindow&&) = default;
		V3dWindow(V3dWindow const&) = delete;
		V3dWindow& operator=(V3dWindow&&) = default;
		V3dWindow& operator=(V3dWindow const&) = delete;
		~V3dWindow();

		// Renderer access
		Renderer& rdr() const;
		ResourceManager& res_mgr() const;

		// Error event. Can be called in a worker thread context
		MultiCast<ReportErrorCB> ReportError;

		// Settings changed event
		MultiCast<SettingsChangedCB> OnSettingsChanged;

		// Window invalidated
		MultiCast<InvalidatedCB> OnInvalidated;

		// Rendering event
		MultiCast<RenderingCB> OnRendering;

		// Scene changed event
		MultiCast<SceneChangedCB> OnSceneChanged;

		// Animation event
		MultiCast<AnimationCB> OnAnimationEvent;

		// Add/Remove an object to this window
		void Add(LdrObject* object);
		void Remove(LdrObject* object);

		// Called when objects are added/removed from this window
		void ObjectContainerChanged(view3d::ESceneChanged change_type, GUID const* context_ids, int count, LdrObject* object);

		// Create stock models such as the focus point, origin, etc
		void CreateStockObjects();
	};
}
