//***************************************************************************************************
// View 3D
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************
#pragma once

#include "view3d/forward.h"
#include "view3d/context.h"

namespace view3d
{
	struct Window :pr::AlignTo<16>
	{
		using ScriptEditorUIPtr     = std::unique_ptr<pr::ldr::ScriptEditorUI>;
		using LdrObjectManagerUIPtr = std::unique_ptr<pr::ldr::LdrObjectManagerUI>;
		using LdrMeasureUIPtr       = std::unique_ptr<pr::ldr::LdrMeasureUI>;
		using LdrAngleUIPtr         = std::unique_ptr<pr::ldr::LdrAngleUI>;

		Context*              m_dll;                      // The dll context
		HWND                  m_hwnd;                     // The associated window handle
		pr::rdr::Window       m_wnd;                      // The window being drawn on
		pr::rdr::Scene        m_scene;                    // Scene manager
		view3d::ObjectSet     m_objects;                  // References to objects to draw (note: objects are owned by the context, not the window)
		view3d::GizmoSet      m_gizmos;                   // References to gizmos to draw (note: objects are owned by the context, not the window)
		pr::Camera            m_camera;                   // Camera control
		pr::rdr::Light        m_light;                    // Light source for the set
		EView3DFillMode       m_fill_mode;                // Fill mode
		EView3DCullMode       m_cull_mode;                // Face culling mode
		pr::Colour32          m_background_colour;        // The background colour for this draw set
		view3d::PointInstance m_focus_point;              // Focus point graphics
		view3d::PointInstance m_origin_point;             // Origin point graphics
		view3d::Instance      m_bbox_model;               // Bounding box graphics
		view3d::Instance      m_selection_box;            // Selection box graphics
		float                 m_focus_point_size;         // The base size of the focus point object
		float                 m_origin_point_size;        // The base size of the origin instance
		bool                  m_focus_point_visible;      // True if we should draw the focus point
		bool                  m_origin_point_visible;     // True if we should draw the origin point
		bool                  m_bboxes_visible;           // True if we should draw object bounding boxes
		ScriptEditorUIPtr     m_editor_ui;                // A editor for editing Ldr script
		LdrObjectManagerUIPtr m_obj_cont_ui;              // Object manager for objects added to this window
		LdrMeasureUIPtr       m_measure_tool_ui;          // A UI for measuring distances between points within the 3d environment
		LdrAngleUIPtr         m_angle_tool_ui;            // A UI for measuring angles between points within the 3d environment
		EditorCont            m_editors;                  // User created editors
		std::string           m_settings;                 // Allows a char const* to be returned
		mutable pr::BBox      m_bbox_scene;               // Bounding box for all objects in the scene (Lazy updated)
		std::thread::id       m_main_thread_id;           // The thread that created this window
		pr::EventHandlerId    m_eh_store_updated;
		pr::EventHandlerId    m_eh_file_removed;

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

		Window(HWND hwnd, Context* dll, View3DWindowOptions const& opts)
			:m_dll(dll)
			,m_hwnd(hwnd)
			,m_wnd(m_dll->m_rdr, Settings(hwnd, opts))
			,m_scene(m_wnd)
			,m_objects()
			,m_gizmos()
			,m_camera()
			,m_light()
			,m_fill_mode(EView3DFillMode::Solid)
			,m_cull_mode(EView3DCullMode::Back)
			,m_background_colour(0xFF808080U)
			,m_focus_point()
			,m_origin_point()
			,m_bbox_model()
			,m_selection_box()
			,m_focus_point_size(1.0f)
			,m_origin_point_size(1.0f)
			,m_focus_point_visible(false)
			,m_origin_point_visible(false)
			,m_bboxes_visible(false)
			,m_editor_ui()
			,m_obj_cont_ui()
			,m_measure_tool_ui()
			,m_angle_tool_ui()
			,m_editors()
			,m_settings()
			,m_bbox_scene(pr::BBoxReset)
			,m_main_thread_id(std::this_thread::get_id())
			,m_eh_store_updated()
			,m_eh_file_removed()
		{
			try
			{
				// Attach the error handler
				if (opts.m_error_cb != nullptr)
					OnError += pr::StaticCallBack(opts.m_error_cb, opts.m_error_cb_ctx);
			
				// Observe the dll object store for changes
				m_eh_store_updated = m_dll->m_sources.OnStoreChanged += [&](pr::ldr::ScriptSources&, pr::ldr::ScriptSources::StoreChangedEventArgs const&)
				{
					// Don't instigate a view reset here because the window doesn't know how the caller would
					// like the view reset (e.g. all objects, just selected, etc). The caller can instead sign
					// up to the StoreUpdated event on the content sources
					InvalidateRect(nullptr, false);
				};
				m_eh_file_removed = m_dll->m_sources.OnFileRemoved += [&](pr::ldr::ScriptSources&, pr::ldr::ScriptSources::FileRemovedEventArgs const& args)
				{
					RemoveObjectsById(args.m_file_group_id, false);
				};

				// Set the initial aspect ratio
				pr::iv2 client_area = m_wnd.RenderTargetSize();
				m_camera.Aspect(client_area.x / float(client_area.y));

				// The light for the scene
				m_light.m_type           = pr::rdr::ELight::Directional;
				m_light.m_ambient        = pr::Colour32(0x00101010U);
				m_light.m_diffuse        = pr::Colour32(0xFF808080U);
				m_light.m_specular       = pr::Colour32(0x00404040U);
				m_light.m_specular_power = 1000.0f;
				m_light.m_direction      = -pr::v4ZAxis;
				m_light.m_on             = true;
				m_light.m_cam_relative   = true;

				// Create the stock models
				CreateStockModels();
			}
			catch (...)
			{
				this->~Window();
				throw;
			}
		}
		Window(Window const&) = delete;
		Window& operator=(Window const&) = delete;
		~Window()
		{
			m_dll->m_sources.OnStoreChanged -= m_eh_store_updated;
			m_dll->m_sources.OnFileRemoved -= m_eh_file_removed;
		}

		// Error event. Can be called in a worker thread context
		pr::MultiCast<ReportErrorCB> OnError;

		// Settings changed event
		pr::MultiCast<SettingsChangedCB> OnSettingsChanged;

		// Rendering event
		pr::MultiCast<RenderingCB> OnRendering;

		// Report an error for this window
		void ReportError(wchar_t const* msg)
		{
			OnError.Raise(msg);
		}

		// Render this window into whatever render target is currently set
		void Render()
		{
			using namespace pr::rdr;
			assert(std::this_thread::get_id() == m_main_thread_id);

			// Reset the drawlist
			m_scene.ClearDrawlists();

			// Notify of a render about to happen
			NotifyRendering();

			// Add objects from the window to the scene
			for (auto& obj : m_objects)
				obj->AddToScene(m_scene);

			// Add gizmos from the window to the scene
			for (auto& giz : m_gizmos)
				giz->AddToScene(m_scene);

			// Add the measure tool objects if the window is visible
			if (m_measure_tool_ui != nullptr && LdrMeasureUI().Visible() && LdrMeasureUI().Gfx())
				LdrMeasureUI().Gfx()->AddToScene(m_scene);

			// Add the angle tool objects if the window is visible
			if (m_angle_tool_ui != nullptr && LdrAngleUI().Visible() && LdrAngleUI().Gfx())
				LdrAngleUI().Gfx()->AddToScene(m_scene);

			// Position the focus point
			if (m_focus_point_visible)
			{
				float const screen_fraction = 0.1f;
				auto fd = m_camera.FocusDist();
				auto sz = m_focus_point_size * screen_fraction * fd;
				m_focus_point.m_i2w = pr::m4x4::Scale(sz, sz, sz, m_camera.FocusPoint());
				m_focus_point.m_c2s = m_camera.CameraToScreen(float(m_scene.m_viewport.Width)/float(m_scene.m_viewport.Height), float(pr::maths::tau_by_8), m_camera.FocusDist());
				m_scene.AddInstance(m_focus_point);
			}

			// Scale the origin point
			if (m_origin_point_visible)
			{
				float const screen_fraction = 0.1f;
				auto fd = pr::Length3(m_camera.CameraToWorld().pos);
				auto sz = m_origin_point_size * screen_fraction * fd;
				m_origin_point.m_i2w = pr::m4x4::Scale(sz, sz, sz, pr::v4Origin);
				m_origin_point.m_c2s = m_camera.CameraToScreen(float(m_scene.m_viewport.Width)/float(m_scene.m_viewport.Height), float(pr::maths::tau_by_8), m_camera.FocusDist());
				m_scene.AddInstance(m_origin_point);
			}

			// Bounding boxes
			if (m_bboxes_visible)
			{
				for (auto& obj : m_objects)
					obj->AddBBoxToScene(m_scene, m_bbox_model.m_model);
			}

			// Set the view and projection matrices
			auto& cam = m_camera;
			m_scene.SetView(cam);
			cam.m_moved = false;

			// Set the light source
			auto& light = m_scene.m_global_light;
			light = m_light;
			if (m_light.m_cam_relative)
			{
				light.m_direction = m_camera.CameraToWorld() * m_light.m_direction;
				light.m_position  = m_camera.CameraToWorld() * m_light.m_position;
			}

			// Set the background colour
			m_scene.m_bkgd_colour = m_background_colour;

			// Set the global fill mode
			switch (m_fill_mode) {
			case EView3DFillMode::Solid:     m_scene.m_rsb.Set(ERS::FillMode, D3D11_FILL_SOLID); break;
			case EView3DFillMode::Wireframe: m_scene.m_rsb.Set(ERS::FillMode, D3D11_FILL_WIREFRAME); break;
			case EView3DFillMode::SolidWire: m_scene.m_rsb.Set(ERS::FillMode, D3D11_FILL_SOLID); break;
			}

			// Set the global cull mode
			switch (m_cull_mode) {
			case EView3DCullMode::None:  m_scene.m_rsb.Set(ERS::CullMode, D3D11_CULL_NONE); break;
			case EView3DCullMode::Back:  m_scene.m_rsb.Set(ERS::CullMode, D3D11_CULL_BACK); break;
			case EView3DCullMode::Front: m_scene.m_rsb.Set(ERS::CullMode, D3D11_CULL_FRONT); break;
			}

			// Render the scene
			m_scene.Render();

			// Render wire frame over solid for 'SolidWire' mode
			if (m_fill_mode == EView3DFillMode::SolidWire)
			{
				auto& fr = m_scene.RStep<ForwardRender>();
				m_scene.m_rsb.Set(ERS::FillMode, D3D11_FILL_WIREFRAME);
				m_scene.m_bsb.Set(EBS::BlendEnable, FALSE, 0);
				fr.m_clear_bb = false;

				m_scene.Render();

				fr.m_clear_bb = true;
				m_scene.m_rsb.Clear(ERS::FillMode);
				m_scene.m_bsb.Clear(EBS::BlendEnable, 0);
			}
		}
		void Present()
		{
			m_wnd.Present();
		}

		// Close any window handles
		void Close()
		{
			// Don't destroy 'm_hwnd' because it doesn't belong to us,
			// we're simply drawing on that window. Signal close by setting it to null
			m_hwnd = 0;
		}

		// The script editor UI
		pr::ldr::ScriptEditorUI& EditorUI()
		{
			// Lazy create
			if (m_editor_ui == nullptr)
				m_editor_ui.reset(new pr::ldr::ScriptEditorUI(m_hwnd));
			
			return *m_editor_ui;
		}

		// The Ldr Object manager UI
		pr::ldr::LdrObjectManagerUI& ObjectManagerUI()
		{
			if (m_obj_cont_ui == nullptr)
				m_obj_cont_ui.reset(new pr::ldr::LdrObjectManagerUI(m_hwnd));
			
			return *m_obj_cont_ui;
		}

		// The distance measurement tool UI
		pr::ldr::LdrMeasureUI& LdrMeasureUI()
		{
			if (m_measure_tool_ui == nullptr)
				m_measure_tool_ui.reset(new pr::ldr::LdrMeasureUI(m_hwnd, ReadPoint, this, m_dll->m_rdr));

			return *m_measure_tool_ui;
		}

		// The angle measurement tool UI
		pr::ldr::LdrAngleUI& LdrAngleUI()
		{
			if (m_angle_tool_ui == nullptr)
				m_angle_tool_ui.reset(new pr::ldr::LdrAngleUI(m_hwnd, ReadPoint, this, m_dll->m_rdr));

			return *m_angle_tool_ui;
		}

		// Return true if 'object' is part of this scene
		bool Has(pr::ldr::LdrObject* object) const
		{
			assert(std::this_thread::get_id() == m_main_thread_id);
			return m_objects.find(object) != std::end(m_objects);
		}
		bool Has(pr::ldr::LdrGizmo* gizmo) const
		{
			return m_gizmos.find(gizmo) != std::end(m_gizmos);
		}

		// Return the number of objects in this scene
		int ObjectCount() const
		{
			assert(std::this_thread::get_id() == m_main_thread_id);
			return int(m_objects.size());
		}
		int GizmoCount() const
		{
			return int(m_gizmos.size());
		}

		// Add/Remove an object to this window
		void Add(pr::ldr::LdrObject* object)
		{
			assert(std::this_thread::get_id() == m_main_thread_id);
			auto iter = m_objects.find(object);
			if (iter == std::end(m_objects))
			{
				m_objects.insert(iter, object);
				ObjectContainerChanged();
			}
		}
		void Remove(pr::ldr::LdrObject* object)
		{
			assert(std::this_thread::get_id() == m_main_thread_id);
			m_objects.erase(object);
			ObjectContainerChanged();
		}

		// Add/Remove a gizmo to this window
		void Add(pr::ldr::LdrGizmo* gizmo)
		{
			auto iter = m_gizmos.find(gizmo);
			if (iter == std::end(m_gizmos))
				m_gizmos.insert(iter, gizmo);
		}
		void Remove(pr::ldr::LdrGizmo* gizmo)
		{
			m_gizmos.erase(gizmo);
		}

		// Remove all objects from this scene
		void RemoveAllObjects()
		{
			assert(std::this_thread::get_id() == m_main_thread_id);
			m_objects.clear();
			ObjectContainerChanged();
		}

		// Remove all objects from this window with the given context id (or not with)
		void RemoveObjectsById(GUID const& context_id, bool all_except)
		{
			assert(std::this_thread::get_id() == m_main_thread_id);
			if (all_except)
				pr::erase_if(m_objects, [&](pr::ldr::LdrObject* p){ return p->m_context_id != context_id; });
			else
				pr::erase_if(m_objects, [&](pr::ldr::LdrObject* p){ return p->m_context_id == context_id; });
			
			ObjectContainerChanged();
		}

		// Return a bounding box containing the scene objects
		template <typename Pred> pr::BBox BBox(Pred pred, bool objects = true, bool gizmos = false) const
		{
			assert(std::this_thread::get_id() == m_main_thread_id);
			auto bbox = pr::BBoxReset;
			if (objects)
			{
				for (auto obj : m_objects)
				{
					if (!pred(*obj)) continue;
					pr::Encompass(bbox, obj->BBoxWS(true));
				}
			}
			if (gizmos)
			{
				throw std::exception("not implemented");
				//for (auto giz : m_gizmos)
				//	pr::Encompass(bbox, giz->BBoxWS(true));
			}
			if (bbox == pr::BBoxReset)
			{
				bbox = pr::BBoxUnit;
			}
			return bbox;
		}
		pr::BBox BBox() const
		{
			return BBox([](pr::ldr::LdrObject const&) { return true; });
		}
		
		// Reset the scene camera, using it's current forward and up directions, to view all objects in the scene
		void ResetView()
		{
			auto c2w = m_camera.CameraToWorld();
			ResetView(-c2w.z, c2w.y);
		}

		// Reset the scene camera to view all objects in the scene
		void ResetView(pr::v4 const& forward, pr::v4 const& up, float dist = 0, bool preserve_aspect = true, bool commit = true)
		{
			ResetView(SceneBounds(EView3DSceneBounds::All, 0, nullptr), forward, up, dist, preserve_aspect, commit);
		}

		// Reset the camera to view a bbox
		void ResetView(pr::BBox const& bbox, pr::v4 const& forward, pr::v4 const& up, float dist = 0, bool preserve_aspect = true, bool commit = true)
		{
			m_camera.View(bbox, forward, up, dist, preserve_aspect, commit);
		}

		// Return the bounding box of objects in this scene
		pr::BBox SceneBounds(EView3DSceneBounds bounds, int except_count, GUID const* except)
		{
			assert(std::this_thread::get_id() == m_main_thread_id);
			pr::array_view<GUID> except_arr(except, except_count);

			pr::BBox bbox;
			switch (bounds)
			{
			default:
				{
					assert(!"Unknown scene bounds type");
					bbox = pr::BBoxUnit;
					break;
				}
			case EView3DSceneBounds::All:
				{
					// Update the scene bounding box if out of date
					if (m_bbox_scene == pr::BBoxReset)
						m_bbox_scene = except_count != 0
							? BBox([&](pr::ldr::LdrObject const& obj){ return !pr::contains(except_arr, obj.m_context_id); })
							: BBox();

					bbox = m_bbox_scene;
					break;
				}
			case EView3DSceneBounds::Selected:
				{
					bbox = pr::BBoxReset;
					for (auto& obj : m_objects)
					{
						if (!pr::AllSet(obj->m_flags, pr::ldr::ELdrFlags::Selected)) continue;
						if (pr::contains(except_arr, obj->m_context_id)) continue;
						pr::Encompass(bbox, obj->BBoxWS(true));
					}
					break;
				}
			case EView3DSceneBounds::Visible:
				{
					bbox = pr::BBoxReset;
					for (auto& obj : m_objects)
					{
						if (pr::contains(except_arr, obj->m_context_id)) continue;
						obj->Apply([&](pr::ldr::LdrObject* o)
						{
							pr::Encompass(bbox, o->BBoxWS(false));
							return true;
						}, "");
					}
					break;
				}
			}
			return !bbox.empty() ? bbox : pr::BBoxUnit;
		}

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

		// Invoke the rendering event
		void NotifyRendering()
		{
			OnRendering.Raise(this);
		}

		// Call InvalidateRect on the HWND associated with this window
		void InvalidateRect(RECT const* rect, bool erase)
		{
			::InvalidateRect(m_hwnd, rect, erase);
		}

		// Called when objects are added/removed from this window
		void ObjectContainerChanged()
		{
			// Reset the drawlists so that removed objects are no longer in the drawlist
			m_scene.ClearDrawlists();

			// Invalidate cached members
			m_bbox_scene = pr::BBoxReset;
		}

		// Show/Hide the object manager for the scene
		void ShowObjectManager(bool show)
		{
			assert(std::this_thread::get_id() == m_main_thread_id);
			auto& ui = ObjectManagerUI();
			ui.Show();
			ui.Populate(m_objects);
			ui.Visible(show);
		}

		// Create stock models such as the focus point, origin, etc
		void CreateStockModels()
		{
			using namespace pr::rdr;
			{
				// Create the focus point/origin models
				// Don't know why, but the optimiser buggers this up if I use initializer_list<>. Hence local arrays
				static pr::v4 const verts[] =
				{
						pr::v4( 0.0f,  0.0f,  0.0f, 1.0f),
						pr::v4( 1.0f,  0.0f,  0.0f, 1.0f),
						pr::v4( 0.0f,  0.0f,  0.0f, 1.0f),
						pr::v4( 0.0f,  1.0f,  0.0f, 1.0f),
						pr::v4( 0.0f,  0.0f,  0.0f, 1.0f),
						pr::v4( 0.0f,  0.0f,  1.0f, 1.0f),
				};
				static pr::uint16 const indices[] = { 0, 1, 2, 3, 4, 5 };
				static NuggetProps const nuggets[] = { NuggetProps(EPrim::LineList, EGeom::Vert|EGeom::Colr) };
				static pr::Colour32 const focus_cols[] = { 0xFFFF0000, 0xFFFF0000, 0xFF00FF00, 0xFF00FF00, 0xFF0000FF, 0xFF0000FF };
				static pr::Colour32 const origin_cols[] = { 0xFF800000, 0xFF800000, 0xFF008000, 0xFF008000, 0xFF000080, 0xFF000080 };

				{
					auto cdata = MeshCreationData().verts(verts).indices(indices).nuggets(nuggets).colours(focus_cols);
					m_focus_point.m_model = ModelGenerator<>::Mesh(m_dll->m_rdr, cdata);
					m_focus_point.m_model->m_name = "focus point";
					m_focus_point.m_i2w   = pr::m4x4Identity;
				}
				{
					auto cdata = MeshCreationData().verts(verts).indices(indices).nuggets(nuggets).colours(origin_cols);
					m_origin_point.m_model = ModelGenerator<>::Mesh(m_dll->m_rdr, cdata);
					m_focus_point.m_model->m_name = "origin point";
					m_origin_point.m_i2w   = pr::m4x4Identity;
				}
			}
			{
				// Create the selection box model
				static pr::v4 const verts[] =
				{
					pr::v4(-0.5f, -0.5f, -0.5f, 1.0f), pr::v4(-0.4f, -0.5f, -0.5f, 1.0f), pr::v4(-0.5f, -0.4f, -0.5f, 1.0f), pr::v4(-0.5f, -0.5f, -0.4f, 1.0f),
					pr::v4( 0.5f, -0.5f, -0.5f, 1.0f), pr::v4( 0.5f, -0.4f, -0.5f, 1.0f), pr::v4( 0.4f, -0.5f, -0.5f, 1.0f), pr::v4( 0.5f, -0.5f, -0.4f, 1.0f),
					pr::v4( 0.5f,  0.5f, -0.5f, 1.0f), pr::v4( 0.4f,  0.5f, -0.5f, 1.0f), pr::v4( 0.5f,  0.4f, -0.5f, 1.0f), pr::v4( 0.5f,  0.5f, -0.4f, 1.0f),
					pr::v4(-0.5f,  0.5f, -0.5f, 1.0f), pr::v4(-0.5f,  0.4f, -0.5f, 1.0f), pr::v4(-0.4f,  0.5f, -0.5f, 1.0f), pr::v4(-0.5f,  0.5f, -0.4f, 1.0f),
					pr::v4(-0.5f, -0.5f,  0.5f, 1.0f), pr::v4(-0.4f, -0.5f,  0.5f, 1.0f), pr::v4(-0.5f, -0.4f,  0.5f, 1.0f), pr::v4(-0.5f, -0.5f,  0.4f, 1.0f),
					pr::v4( 0.5f, -0.5f,  0.5f, 1.0f), pr::v4( 0.5f, -0.4f,  0.5f, 1.0f), pr::v4( 0.4f, -0.5f,  0.5f, 1.0f), pr::v4( 0.5f, -0.5f,  0.4f, 1.0f),
					pr::v4( 0.5f,  0.5f,  0.5f, 1.0f), pr::v4( 0.4f,  0.5f,  0.5f, 1.0f), pr::v4( 0.5f,  0.4f,  0.5f, 1.0f), pr::v4( 0.5f,  0.5f,  0.4f, 1.0f),
					pr::v4(-0.5f,  0.5f,  0.5f, 1.0f), pr::v4(-0.5f,  0.4f,  0.5f, 1.0f), pr::v4(-0.4f,  0.5f,  0.5f, 1.0f), pr::v4(-0.5f,  0.5f,  0.4f, 1.0f),
				};
				static pr::uint16 const indices[] =
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
				static NuggetProps const nuggets[] =
				{
					NuggetProps(EPrim::LineList, EGeom::Vert),
				};

				auto cdata = pr::rdr::MeshCreationData().verts(verts).indices(indices).nuggets(nuggets);
				m_selection_box.m_model = ModelGenerator<>::Mesh(m_dll->m_rdr, cdata);
				m_selection_box.m_model->m_name = "selection box";
				m_selection_box.m_i2w   = pr::m4x4Identity;
			}
			{
				// Create a bounding box model
				static pr::v4 const verts[] =
				{
					pr::v4(-0.5f, -0.5f, -0.5f, 1.0f),
					pr::v4(+0.5f, -0.5f, -0.5f, 1.0f),
					pr::v4(+0.5f, +0.5f, -0.5f, 1.0f),
					pr::v4(-0.5f, +0.5f, -0.5f, 1.0f),
					pr::v4(-0.5f, -0.5f, +0.5f, 1.0f),
					pr::v4(+0.5f, -0.5f, +0.5f, 1.0f),
					pr::v4(+0.5f, +0.5f, +0.5f, 1.0f),
					pr::v4(-0.5f, +0.5f, +0.5f, 1.0f),
				};
				static pr::uint16 const indices[] =
				{
					0, 1, 1, 2, 2, 3, 3, 0,
					4, 5, 5, 6, 6, 7, 7, 4,
					0, 4, 1, 5, 2, 6, 3, 7,
				};
				static pr::Colour32 const colours[] =
				{
					pr::Colour32Blue,
				};
				static NuggetProps const nuggets[] =
				{
					NuggetProps(EPrim::LineList),
				};

				auto cdata = pr::rdr::MeshCreationData().verts(verts).indices(indices).colours(colours).nuggets(nuggets);
				m_bbox_model.m_model = ModelGenerator<>::Mesh(m_dll->m_rdr, cdata);
				m_bbox_model.m_model->m_name = "bbox";
				m_bbox_model.m_i2w   = pr::m4x4Identity;
			}
		}
	};
}
