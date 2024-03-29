﻿//***************************************************************************************************
// View 3D
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************
#pragma once

#include "view3d/dll/forward.h"

namespace view3d
{
	struct alignas(16) Window
	{
		using ScriptEditorUIPtr     = std::unique_ptr<pr::ldr::ScriptEditorUI>;
		using LdrObjectManagerUIPtr = std::unique_ptr<pr::ldr::LdrObjectManagerUI>;
		using LdrMeasureUIPtr       = std::unique_ptr<pr::ldr::LdrMeasureUI>;
		using LdrAngleUIPtr         = std::unique_ptr<pr::ldr::LdrAngleUI>;
		using AnimData              = struct AnimData
		{
			std::thread  m_thread;
			std::atomic_int m_issue;
			std::atomic<seconds_t> m_clock;
			
			AnimData()
				:m_thread()
				,m_issue()
				,m_clock()
			{}
		};

		Context*              m_dll;                   // The dll context
		HWND                  m_hwnd;                  // The associated window handle
		pr::rdr::Window       m_wnd;                   // The window being drawn on
		pr::rdr::Scene        m_scene;                 // Scene manager
		view3d::ObjectSet     m_objects;               // References to objects to draw (note: objects are owned by the context, not the window)
		view3d::GizmoSet      m_gizmos;                // References to gizmos to draw (note: objects are owned by the context, not the window)
		view3d::GuidSet       m_guids;                 // The context ids added to this window
		pr::Camera            m_camera;                // Camera control
		pr::rdr::Light        m_light;                 // Light source for the set
		pr::rdr::EFillMode    m_fill_mode;             // Global fill mode
		pr::rdr::ECullMode    m_cull_mode;             // Global cull mode
		pr::Colour32          m_background_colour;     // The background colour for this draw set
		view3d::PointInstance m_focus_point;           // Focus point graphics
		view3d::PointInstance m_origin_point;          // Origin point graphics
		view3d::Instance      m_bbox_model;            // Bounding box graphics
		view3d::Instance      m_selection_box;         // Selection box graphics
		AnimData              m_anim_data;             // Animation time in seconds
		float                 m_focus_point_size;      // The base size of the focus point object
		float                 m_origin_point_size;     // The base size of the origin instance
		bool                  m_focus_point_visible;   // True if we should draw the focus point
		bool                  m_origin_point_visible;  // True if we should draw the origin point
		bool                  m_selection_box_visible; // True if we should draw the selection box
		bool                  m_invalidated;           // True after Invalidate has been called but before Render has been called
		ScriptEditorUIPtr     m_editor_ui;             // A editor for editing Ldr script
		LdrObjectManagerUIPtr m_obj_cont_ui;           // Object manager for objects added to this window
		LdrMeasureUIPtr       m_measure_tool_ui;       // A UI for measuring distances between points within the 3d environment
		LdrAngleUIPtr         m_angle_tool_ui;         // A UI for measuring angles between points within the 3d environment
		EditorCont            m_editors;               // User created editors
		std::wstring          m_settings;              // Serialised window settings
		mutable pr::BBox      m_bbox_scene;            // Bounding box for all objects in the scene (Lazy updated)
		std::thread::id       m_main_thread_id;        // The thread that created this window

		// Default window construction settings
		static pr::rdr::WndSettings Settings(HWND hwnd, View3DWindowOptions const& opts);

		// Constructor
		Window(HWND hwnd, Context* dll, View3DWindowOptions const& opts);
		~Window();

		Window(Window const&) = delete;
		Window& operator=(Window const&) = delete;

		// Error event. Can be called in a worker thread context
		pr::MultiCast<pr::StaticCB<View3D_ReportErrorCB>, true> ReportError;

		// Settings changed event
		pr::MultiCast<SettingsChangedCB> OnSettingsChanged;

		// Window invalidated
		pr::MultiCast<InvalidatedCB> OnInvalidated;

		// Rendering event
		pr::MultiCast<RenderingCB> OnRendering;

		// Scene changed event
		pr::MultiCast<SceneChangedCB> OnSceneChanged;

		// Animation event
		pr::MultiCast<AnimationCB> OnAnimationEvent;

		// Get/Set the scene viewport
		View3DViewport Viewport() const;
		void Viewport(View3DViewport vp);

		// The DPI of the monitor that this window is displayed on
		pr::v2 Dpi() const;

		// Render this window into whatever render target is currently set
		void Render();
		void Present();

		// Close any window handles
		void Close();

		// The script editor UI
		pr::ldr::ScriptEditorUI& EditorUI();

		// The Ldr Object manager UI
		pr::ldr::LdrObjectManagerUI& ObjectManagerUI();

		// The distance measurement tool UI
		pr::ldr::LdrMeasureUI& LdrMeasureUI();

		// The angle measurement tool UI
		pr::ldr::LdrAngleUI& LdrAngleUI();

		// Return true if 'object' is part of this scene
		bool Has(pr::ldr::LdrObject const* object, bool search_children) const;
		bool Has(pr::ldr::LdrGizmo const* gizmo) const;

		// Return the number of objects or object groups in this scene
		int ObjectCount() const;
		int GizmoCount() const;
		int GuidCount() const;

		// Enumerate the guids associated with this window
		void EnumGuids(View3D_EnumGuidsCB enum_guids_cb, void* ctx);

		// Enumerate the objects associated with this window
		void EnumObjects(View3D_EnumObjectsCB enum_objects_cb, void* ctx);
		void EnumObjects(View3D_EnumObjectsCB enum_objects_cb, void* ctx, GUID const* context_id, int include_count, int exclude_count);

		// Add/Remove an object to this window
		void Add(pr::ldr::LdrObject* object);
		void Remove(pr::ldr::LdrObject* object);

		// Add/Remove a gizmo to this window
		void Add(pr::ldr::LdrGizmo* gizmo);
		void Remove(pr::ldr::LdrGizmo* gizmo);

		// Remove all objects from this scene
		void RemoveAllObjects();

		// Add/Remove all objects to this window with the given context id (or not with)
		void AddObjectsById(GUID const* context_id, int include_count, int exclude_count);
		void RemoveObjectsById(GUID const* context_id, int include_count, int exclude_count, bool keep_context_ids = false);

		// Return a bounding box containing the scene objects
		template <typename Pred> pr::BBox BBox(Pred pred, bool objects = true, bool gizmos = false) const
		{
			assert(std::this_thread::get_id() == m_main_thread_id);
			auto bbox = pr::BBox::Reset();
			if (objects)
			{
				for (auto obj : m_objects)
				{
					if (!pred(*obj)) continue;
					Grow(bbox, obj->BBoxWS(true));
				}
			}
			if (gizmos)
			{
				throw std::exception("not implemented");
				//for (auto giz : m_gizmos)
				//	Grow(bbox, giz->BBoxWS(true));
			}
			if (bbox == pr::BBox::Reset())
			{
				bbox = pr::BBox::Unit();
			}
			return bbox;
		}
		pr::BBox BBox() const;
		
		// Reset the scene camera, using it's current forward and up directions, to view all objects in the scene
		void ResetView();

		// Reset the scene camera to view all objects in the scene
		void ResetView(pr::v4 const& forward, pr::v4 const& up, float dist = 0, bool preserve_aspect = true, bool commit = true);

		// Reset the camera to view a bbox
		void ResetView(pr::BBox const& bbox, pr::v4 const& forward, pr::v4 const& up, float dist = 0, bool preserve_aspect = true, bool commit = true);

		// Return the bounding box of objects in this scene
		pr::BBox SceneBounds(EView3DSceneBounds bounds, int except_count, GUID const* except);

		// Set the position and size of the selection box. If 'bbox' is 'BBoxReset' the selection box is not shown
		void SetSelectionBox(pr::BBox const& bbox, pr::m3x4 const& ori = pr::m3x4Identity);

		// Position the selection box to include the selected objects
		void SelectionBoxFitToSelected();

		// Control object animation
		bool Animating() const;
		seconds_t AnimTime() const;
		void AnimTime(seconds_t clock);
		void AnimControl(EView3DAnimCommand command, seconds_t time = seconds_t::zero());

		// Convert a screen space point to a normalised screen space point
		pr::v2 SSPointToNSSPoint(pr::v2 const& ss_point) const;
		pr::v2 NSSPointToSSPoint(pr::v2 const& nss_point) const;

		// Invoke the settings changed callback
		void NotifySettingsChanged(EView3DSettings setting);

		// Invoke the rendering event
		void NotifyRendering();

		// Call InvalidateRect on the HWND associated with this window
		void InvalidateRect(RECT const* rect, bool erase = false);
		void Invalidate(bool erase = false);

		// Clear the invalidated state for the window
		void Validate();

		// Called when objects are added/removed from this window
		void ObjectContainerChanged(EView3DSceneChanged change_type, GUID const* context_ids, int count, pr::ldr::LdrObject* object);

		// Show/Hide the object manager for the scene
		void ShowObjectManager(bool show);

		// Show/Hide the measure tool
		void ShowMeasureTool(bool show);

		// Show/Hide the angle tool
		void ShowAngleTool(bool show);

		// Get/Set the window fill mode
		pr::rdr::EFillMode FillMode() const;
		void FillMode(pr::rdr::EFillMode fill_mode);

		// Get/Set the window cull mode
		pr::rdr::ECullMode CullMode() const;
		void CullMode(pr::rdr::ECullMode cull_mode);

		// Get/Set the window background colour
		pr::Colour32 BackgroundColour() const;
		void BackgroundColour(pr::Colour32 colour);

		// Get/Set the window background colour
		int MultiSampling() const;
		void MultiSampling(int multisampling);

		// Show/Hide the focus point
		bool FocusPointVisible() const;
		void FocusPointVisible(bool vis);

		// Show/Hide the origin point
		bool OriginPointVisible() const;
		void OriginPointVisible(bool vis);

		// Show/Hide the bounding boxes
		bool BBoxesVisible() const;
		void BBoxesVisible(bool vis);

		// The length of vertex normals (when displayed)
		float NormalsLength() const;
		void NormalsLength(float length);

		// The colour of vertex normals (when displayed)
		pr::Colour32 NormalsColour() const;
		void NormalsColour(pr::Colour32 colour);

		// Get/Set the size of the 'Points' fill mode points
		pr::v2 FillModePointsSize() const;
		void FillModePointsSize(pr::v2 size);

		// Show/Hide the selection box
		bool SelectionBoxVisible() const;
		void SelectionBoxVisible(bool vis);

		// Cast a ray into the scene, returning hit info
		void HitTest(View3DHitTestRay const* rays, View3DHitTestResult* hits, int ray_count, float snap_distance, EView3DHitTestFlags flags, pr::rdr::RayCastStep::Instances instances);
		void HitTest(View3DHitTestRay const* rays, View3DHitTestResult* hits, int ray_count, float snap_distance, EView3DHitTestFlags flags, pr::ldr::LdrObject const* const* objects, int object_count);
		void HitTest(View3DHitTestRay const* rays, View3DHitTestResult* hits, int ray_count, float snap_distance, EView3DHitTestFlags flags, GUID const* context_ids, int include_count, int exclude_count);

		// Get/Set the global environment map for this window
		View3DCubeMap EnvMap() const;
		void EnvMap(View3DCubeMap env_map);

		// Implements standard key bindings. Returns true if handled
		bool TranslateKey(pr::EKeyCodes vk_key);

		// Create stock models such as the focus point, origin, etc
		void CreateStockModels();
	};
}
