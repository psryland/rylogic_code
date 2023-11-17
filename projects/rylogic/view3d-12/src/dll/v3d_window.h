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
		using AnimData = struct AnimData
		{
			std::thread  m_thread;
			std::atomic_int m_issue;
			std::atomic<seconds_t> m_clock;
			AnimData() :m_thread() ,m_issue() ,m_clock() {}
		};

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
			x(Colour32 ,m_tint  ,EInstComp::TintColour32)\
			x(float    ,m_size  ,EInstComp::Float1)
		PR_RDR12_DEFINE_INSTANCE(PointInstance, PR_RDR_INST) // An instance type for the focus point and origin point models
		#undef PR_RDR_INST
		PointInstance m_focus_point;     // Focus point graphics
		PointInstance m_origin_point;    // Origin point graphics
		Instance      m_bbox_model;      // Bounding box graphics
		Instance      m_selection_box;   // Selection box graphics
		EStockObject  m_visible_objects; // Visible stock objects

		// Misc
		mutable std::wstring m_settings;       // Window settings
		AnimData             m_anim_data;      // Animation time in seconds
		mutable pr::BBox     m_bbox_scene;     // Bounding box for all objects in the scene (Lazy updated)
		PipeStates           m_global_pso;     // Global pipe state overrides
		std::thread::id      m_main_thread_id; // The thread that created this window
		bool                 m_invalidated;    // True after Invalidate has been called but before Render has been called
		
		V3dWindow(HWND hwnd, Context& context, view3d::WindowOptions const& opts);
		V3dWindow(V3dWindow&&) = default;
		V3dWindow(V3dWindow const&) = delete;
		V3dWindow& operator=(V3dWindow&&) = default;
		V3dWindow& operator=(V3dWindow const&) = delete;
		~V3dWindow();

		// Renderer access
		Renderer& rdr() const;
		ResourceManager& res() const;

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

		// Get/Set the settings
		wchar_t const* Settings() const;
		void Settings(wchar_t const* settings);

		// Get/Set the back buffer size
		iv2 BackBufferSize() const;
		void BackBufferSize(iv2 sz);

		// Get/Set the window viewport
		view3d::Viewport Viewport() const;
		void Viewport(view3d::Viewport const& vp);

		// Add/Remove an object to this window
		void Add(LdrObject* object);
		void Remove(LdrObject* object);

		// Add/Remove all objects to this window with the given context ids (or not with)
		void Add(GUID const* context_ids, int include_count, int exclude_count);
		void Remove(GUID const* context_ids, int include_count, int exclude_count, bool keep_context_ids = false);

		// Remove all objects from this scene
		void RemoveAllObjects();

		// Render this window into whatever render target is currently set
		void Render();
		void Present();

		// Call InvalidateRect on the HWND associated with this window
		void InvalidateRect(RECT const* rect, bool erase = false);
		void Invalidate(bool erase = false);

		// Clear the invalidated state for the window
		void Validate();

		// General mouse navigation
		// 'ss_pos' is the mouse pointer position in 'window's screen space
		// 'nav_op' is the navigation type
		// 'nav_start_or_end' should be TRUE on mouse down/up events, FALSE for mouse move events
		// void OnMouseDown(UINT nFlags, CPoint point) { View3D_MouseNavigate(win, point, nav_op, TRUE); }
		// void OnMouseMove(UINT nFlags, CPoint point) { View3D_MouseNavigate(win, point, nav_op, FALSE); } if 'nav_op' is None, this will have no effect
		// void OnMouseUp  (UINT nFlags, CPoint point) { View3D_MouseNavigate(win, point, 0, TRUE); }
		// BOOL OnMouseWheel(UINT nFlags, short zDelta, CPoint) { if (nFlags == 0) View3D_MouseNavigateZ(win, 0, 0, zDelta / 120.0f); return TRUE; }
		bool MouseNavigate(v2 ss_point, camera::ENavOp nav_op, bool nav_start_or_end);
		bool MouseNavigateZ(v2 ss_point, float delta, bool along_ray);

		// Get/Set the window background colour
		Colour32 BackgroundColour() const;
		void BackgroundColour(Colour32 colour);

		// Get/Set the global scene light
		Light GlobalLight() const;
		void GlobalLight(Light const& light);

		// Called when objects are added/removed from this window
		void ObjectContainerChanged(view3d::ESceneChanged change_type, GUID const* context_ids, int count, LdrObject* object);

		// Set the position and size of the selection box. If 'bbox' is 'BBoxReset' the selection box is not shown
		void SetSelectionBox(BBox const& bbox, m3x4 const& ori = m3x4::Identity());

		// Position the selection box to include the selected objects
		void SelectionBoxFitToSelected();

		// Create stock models such as the focus point, origin, etc
		void CreateStockObjects();
	};
}
