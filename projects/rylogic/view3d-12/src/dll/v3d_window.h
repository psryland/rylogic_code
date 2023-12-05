//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/main/window.h"
#include "pr/view3d-12/instance/instance.h"
#include "pr/view3d-12/scene/scene.h"
#include "pr/view3d-12/utility/ray_cast.h"
#include "view3d-12/src/dll/dll_forward.h"

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
		MultiCast<StaticCB<view3d::ReportErrorCB>, true> ReportError;

		// Settings changed event
		MultiCast<StaticCB<view3d::SettingsChangedCB>, true> OnSettingsChanged;

		// Window invalidated
		MultiCast<StaticCB<view3d::InvalidatedCB>, true> OnInvalidated;

		// Rendering event
		MultiCast<StaticCB<view3d::RenderingCB>, true> OnRendering;

		// Scene changed event
		MultiCast<StaticCB<view3d::SceneChangedCB>, true> OnSceneChanged;

		// Animation event
		MultiCast<StaticCB<view3d::AnimationCB>, true> OnAnimationEvent;

		// Get/Set the settings
		wchar_t const* Settings() const;
		void Settings(wchar_t const* settings);

		// The DPI of the monitor that this window is displayed on
		v2 Dpi() const;

		// Get/Set the back buffer size
		iv2 BackBufferSize() const;
		void BackBufferSize(iv2 sz);

		// Get/Set the window viewport
		view3d::Viewport Viewport() const;
		void Viewport(view3d::Viewport const& vp);

		// Enumerate the object collection guids associated with this window
		void EnumGuids(StaticCB<bool, Guid const&> enum_guids_cb);

		// Enumerate the objects associated with this window
		void EnumObjects(StaticCB<bool, view3d::Object> enum_objects_cb);
		void EnumObjects(StaticCB<bool, view3d::Object> enum_objects_cb, GUID const* context_ids, int include_count, int exclude_count);

		// Return true if 'object' is part of this scene
		bool Has(LdrObject const* object, bool search_children) const;
		bool Has(LdrGizmo const* gizmo) const;

		// Return the number of objects or object groups in this scene
		int ObjectCount() const;
		int GizmoCount() const;
		int GuidCount() const;

		// Return the bounding box of objects in this scene
		BBox SceneBounds(view3d::ESceneBounds bounds, int except_count, GUID const* except) const;

		// Add/Remove an object to/from this window
		void Add(LdrObject* object);
		void Remove(LdrObject* object);

		// Add/Remove a gizmo to/from this window
		void Add(LdrGizmo* gizmo);
		void Remove(LdrGizmo* gizmo);
	
		// Add/Remove all objects to this window with the given context ids (or not with)
		void Add(GUID const* context_ids, int include_count, int exclude_count);
		void Remove(GUID const* context_ids, int include_count, int exclude_count, bool keep_context_ids = false);

		// Remove all objects from this scene
		void RemoveAllObjects();

		// Render this window into whatever render target is currently set
		void Render();

		// Call InvalidateRect on the HWND associated with this window
		void InvalidateRect(RECT const* rect, bool erase = false);
		void Invalidate(bool erase = false);

		// Clear the invalidated state for the window
		void Validate();
		
		// Reset the scene camera, using it's current forward and up directions, to view all objects in the scene
		void ResetView();

		// Reset the scene camera to view all objects in the scene
		void ResetView(v4 const& forward, v4 const& up, float dist = 0.0f, bool preserve_aspect = true, bool commit = true);

		// Reset the camera to view a bbox
		void ResetView(BBox const& bbox, v4 const& forward, v4 const& up, float dist = 0.0f, bool preserve_aspect = true, bool commit = true);

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
		Colour BackgroundColour() const;
		void BackgroundColour(Colour_cref colour);
			
		// Get/Set the window fill mode
		EFillMode FillMode() const;
		void FillMode(EFillMode fill_mode);

		// Get/Set the window cull mode
		ECullMode CullMode() const;
		void CullMode(ECullMode cull_mode);

		// Enable/Disable orthographic projection
		bool Orthographic() const;
		void Orthographic(bool on);

		// Get/Set the distance to the camera focus point
		float FocusDistance() const;
		void FocusDistance(float dist);

		// Get/Set the camera focus point position
		v4 FocusPoint() const;
		void FocusPoint(v4_cref position);

		// Get/Set the aspect ratio for the camera field of view
		float Aspect() const;
		void Aspect(float aspect);

		// Get/Set the camera field of view. null means don't change
		v2 Fov() const;
		void Fov(float* fovX, float* fovY);

		// Adjust the FocusDist, FovX, and FovY so that the average FOV equals 'fov'
		void BalanceFov(float fov);

		// Get/Set (using fov and focus distance) the size of the perpendicular area visible to the camera at 'dist' (in world space). Use 'focus_dist != 0' to set a specific focus distance
		v2 ViewRectAtDistance(float dist) const;
		void ViewRectAtDistance(v2_cref rect, float focus_dist = 0);

		// Get/Set the near and far clip planes for the camera
		v2 ClipPlanes(bool focus_relative) const;
		void ClipPlanes(float* near_, float* far_, bool focus_relative);

		// Get/Set the scene camera lock mask
		camera::ELockMask LockMask() const;
		void LockMask(camera::ELockMask mask);

		// Get/Set the camera align axis
		v4 AlignAxis() const;
		void AlignAxis(v4_cref axis);

		// Reset to the default zoom
		void ResetZoom();

		// Get/Set the FOV zoom
		float Zoom() const;
		void Zoom(float zoom);

		// Get/Set the global scene light
		Light GlobalLight() const;
		void GlobalLight(Light const& light);

		// Get/Set the global environment map for this window
		TextureCube* EnvMap() const;
		void EnvMap(TextureCube* env_map);

		// Called when objects are added/removed from this window
		void ObjectContainerChanged(view3d::ESceneChanged change_type, GUID const* context_ids, int count, LdrObject* object);

		// Set the position and size of the selection box. If 'bbox' is 'BBoxReset' the selection box is not shown
		void SetSelectionBox(BBox const& bbox, m3x4 const& ori = m3x4::Identity());

		// Position the selection box to include the selected objects
		void SelectionBoxFitToSelected();
	
		// Get/Set the window background colour
		int MultiSampling() const;
		void MultiSampling(int multisampling);

		// Control animation
		void AnimControl(view3d::EAnimCommand command, seconds_t time = seconds_t::zero());
	
		// True if animation is currently active
		bool Animating() const;
	
		// Get/Set the value of the animation clock
		seconds_t AnimTime() const;
		void AnimTime(seconds_t clock);

		// Cast rays into the scene, returning hit info for the nearest intercept for each ray
		void HitTest(std::span<view3d::HitTestRay const> rays, std::span<view3d::HitTestResult> hits, float snap_distance, view3d::EHitTestFlags flags, RayCastInstancesCB instances);
		void HitTest(std::span<view3d::HitTestRay const> rays, std::span<view3d::HitTestResult> hits, float snap_distance, view3d::EHitTestFlags flags, LdrObject const* const* objects, int object_count);
		void HitTest(std::span<view3d::HitTestRay const> rays, std::span<view3d::HitTestResult> hits, float snap_distance, view3d::EHitTestFlags flags, GUID const* context_ids, int include_count, int exclude_count);
	
		// Show/Hide the focus point
		bool FocusPointVisible() const;
		void FocusPointVisible(bool vis);

		// Show/Hide the bounding boxes
		bool BBoxesVisible() const;
		void BBoxesVisible(bool vis);

		// Get/Set the length of the displayed vertex normals
		float NormalsLength() const;
		void NormalsLength(float length);
		
		// Get/Set the colour of the displayed vertex normals
		Colour32 NormalsColour() const;
		void NormalsColour(Colour32 colour);

		// Get/Set the colour of the displayed vertex normals
		v2 FillModePointsSize() const;
		void FillModePointsSize(v2 size);

	private:

		// Create stock models such as the focus point, origin, etc
		void CreateStockObjects();
	};
}
