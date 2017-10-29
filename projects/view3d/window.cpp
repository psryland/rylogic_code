//***************************************************************************************************
// View 3D
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************

#include "view3d/forward.h"
#include "view3d/window.h"
#include "view3d/context.h"

using namespace pr::rdr;
using namespace pr::ldr;

namespace view3d
{
	// Default window construction settings
	WndSettings Window::Settings(HWND hwnd, View3DWindowOptions const& opts)
	{
		if (hwnd == 0)
			throw pr::Exception<HRESULT>(E_FAIL, "Provided window handle is null");

		RECT rect;
		::GetClientRect(hwnd, &rect);

		auto settings        = pr::rdr::WndSettings(hwnd, true, opts.m_gdi_compatible_backbuffer != 0, pr::To<pr::iv2>(rect));
		settings.m_multisamp = pr::rdr::MultiSamp(opts.m_multisampling);
		settings.m_name      = opts.m_dbg_name;
		return settings;
	}

	// Return the focus point of the camera in this draw set
	pr::v4 __stdcall ReadPoint(void* ctx)
	{
		if (ctx == 0) return pr::v4Origin;
		return static_cast<Window const*>(ctx)->m_camera.FocusPoint();
	}

	// Constructor
	Window::Window(HWND hwnd, Context* dll, View3DWindowOptions const& opts)
		:m_dll(dll)
		,m_hwnd(hwnd)
		,m_wnd(m_dll->m_rdr, Settings(hwnd, opts))
		,m_scene(m_wnd)
		,m_objects()
		,m_gizmos()
		,m_guids()
		,m_camera()
		,m_light()
		,m_fill_mode(EView3DFillMode::Solid)
		,m_cull_mode(EView3DCullMode::Back)
		,m_background_colour(0xFF808080U)
		,m_focus_point()
		,m_origin_point()
		,m_bbox_model()
		,m_selection_box()
		,m_anim_time_s(0.0f)
		,m_focus_point_size(1.0f)
		,m_origin_point_size(1.0f)
		,m_focus_point_visible(false)
		,m_origin_point_visible(false)
		,m_bboxes_visible(false)
		,m_selection_box_visible(false)
		,m_editor_ui()
		,m_obj_cont_ui()
		,m_measure_tool_ui()
		,m_angle_tool_ui()
		,m_editors()
		,m_settings()
		,m_bbox_scene(pr::BBoxReset)
		,m_main_thread_id(std::this_thread::get_id())
	{
		try
		{
			// Notes:
			// - Don't observe the Context sources store for changes. The context handles this for us

			// Attach the error handler
			if (opts.m_error_cb != nullptr)
				OnError += pr::StaticCallBack(opts.m_error_cb, opts.m_error_cb_ctx);
			

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
	Window::~Window()
	{
		Close();
		m_scene.RemoveInstance(m_focus_point);
		m_scene.RemoveInstance(m_origin_point);
		m_scene.RemoveInstance(m_bbox_model);
		m_scene.RemoveInstance(m_selection_box);
	}

	// Report an error for this window
	void Window::ReportError(wchar_t const* msg)
	{
		OnError.Raise(msg);
	}

	// Render this window into whatever render target is currently set
	void Window::Render()
	{
		using namespace pr::rdr;
		assert(std::this_thread::get_id() == m_main_thread_id);

		// Reset the drawlist
		m_scene.ClearDrawlists();

		// Notify of a render about to happen
		NotifyRendering();

		// Set the view and projection matrices. Do this before adding objects to the
		// scene as they do last minute transform adjustments based on the camera position.
		auto& cam = m_camera;
		m_scene.SetView(cam);
		cam.m_moved = false;

		// Position and scale the focus point and origin point
		if (m_focus_point_visible || m_origin_point_visible)
		{
			// Draw the point with perspective or orthographic projection based on the camera settings,
			// but with an aspect ratio matching the viewport regardless of the camera's aspect ratio.
			float const screen_fraction = 0.05f;
			auto aspect_v = float(m_scene.m_viewport.Width) / float(m_scene.m_viewport.Height);

			// Create a camera with the same aspect as the viewport
			auto v_camera = m_camera; v_camera.Aspect(aspect_v);
			auto fd = m_camera.FocusDist();

			// Get the scaling factors from 'm_camera' to 'v_camera'
			auto viewarea_c = m_camera.ViewArea(fd);
			auto viewarea_v = v_camera.ViewArea(fd);

			if (m_focus_point_visible)
			{
				// Scale the camera space X,Y coords
				// Note: this cannot be added as a matrix to 'i2w' or 'c2s' because we're
				// only scaling the instance position, not the whole instance geometry
				auto pt_cs = m_camera.WorldToCamera() * m_camera.FocusPoint();
				pt_cs.x *= viewarea_v.x / viewarea_c.x;
				pt_cs.y *= viewarea_v.y / viewarea_c.y;
				auto pt_ws = m_camera.CameraToWorld() * pt_cs;

				auto sz = m_focus_point_size * screen_fraction * abs(pt_cs.z);
				m_focus_point.m_i2w = pr::m4x4::Scale(sz, sz, sz, pt_ws);
				m_focus_point.m_c2s = v_camera.CameraToScreen();
				m_scene.AddInstance(m_focus_point);
			}
			if (m_origin_point_visible)
			{
				// Scale the camera space X,Y coords
				auto pt_cs = m_camera.WorldToCamera() * pr::v4Origin;
				pt_cs.x *= viewarea_v.x / viewarea_c.x;
				pt_cs.y *= viewarea_v.y / viewarea_c.y;
				auto pt_ws = m_camera.CameraToWorld() * pt_cs;

				auto sz = m_origin_point_size * screen_fraction * abs(pt_cs.z);
				m_origin_point.m_i2w = pr::m4x4::Scale(sz, sz, sz, pt_ws);
				m_origin_point.m_c2s = v_camera.CameraToScreen();
				m_scene.AddInstance(m_origin_point);
			}
		}

		// Bounding boxes
		if (m_bboxes_visible)
		{
			for (auto& obj : m_objects)
			{
				if (pr::AllSet(obj->m_flags, pr::ldr::ELdrFlags::BBoxInvisible)) continue;
				obj->AddBBoxToScene(m_scene, m_bbox_model.m_model);
			}
		}

		// Selection box
		if (m_selection_box_visible)
		{
			// Transform is updated by the user or by a call to SetSelectionBoxToSelected()
			// 'm_selection_box.m_i2w.pos.w' is zero when there is no selection.
			if (m_selection_box.m_i2w.pos.w != 0)
				m_scene.AddInstance(m_selection_box);
		}

		// Set the light source
		auto& light = m_scene.m_global_light;
		light = m_light;
		if (m_light.m_cam_relative)
		{
			light.m_direction = m_camera.CameraToWorld() * m_light.m_direction;
			light.m_position  = m_camera.CameraToWorld() * m_light.m_position;
		}

		// Add objects from the window to the scene
		for (auto& obj : m_objects)
			obj->AddToScene(m_scene, m_anim_time_s);

		// Add gizmos from the window to the scene
		for (auto& giz : m_gizmos)
			giz->AddToScene(m_scene);

		// Add the measure tool objects if the window is visible
		if (m_measure_tool_ui != nullptr && LdrMeasureUI().Visible() && LdrMeasureUI().Gfx())
			LdrMeasureUI().Gfx()->AddToScene(m_scene);

		// Add the angle tool objects if the window is visible
		if (m_angle_tool_ui != nullptr && LdrAngleUI().Visible() && LdrAngleUI().Gfx())
			LdrAngleUI().Gfx()->AddToScene(m_scene);

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
	void Window::Present()
	{
		m_wnd.Present();
	}

	// Close any window handles
	void Window::Close()
	{
		// Don't destroy 'm_hwnd' because it doesn't belong to us,
		// we're simply drawing on that window. Signal close by setting it to null
		m_hwnd = 0;
	}

	// The script editor UI
	ScriptEditorUI& Window::EditorUI()
	{
		// Lazy create
		if (m_editor_ui == nullptr)
			m_editor_ui.reset(new ScriptEditorUI(m_hwnd));
			
		return *m_editor_ui;
	}

	// The Ldr Object manager UI
	LdrObjectManagerUI& Window::ObjectManagerUI()
	{
		if (m_obj_cont_ui == nullptr)
			m_obj_cont_ui.reset(new LdrObjectManagerUI(m_hwnd));
			
		return *m_obj_cont_ui;
	}

	// The distance measurement tool UI
	LdrMeasureUI& Window::LdrMeasureUI()
	{
		if (m_measure_tool_ui == nullptr)
			m_measure_tool_ui.reset(new pr::ldr::LdrMeasureUI(m_hwnd, ReadPoint, this, m_dll->m_rdr));

		return *m_measure_tool_ui;
	}

	// The angle measurement tool UI
	LdrAngleUI& Window::LdrAngleUI()
	{
		if (m_angle_tool_ui == nullptr)
			m_angle_tool_ui.reset(new pr::ldr::LdrAngleUI(m_hwnd, ReadPoint, this, m_dll->m_rdr));

		return *m_angle_tool_ui;
	}

	// Return true if 'object' is part of this scene
	bool Window::Has(pr::ldr::LdrObject* object) const
	{
		assert(std::this_thread::get_id() == m_main_thread_id);
		return m_objects.find(object) != std::end(m_objects);
	}
	bool Window::Has(pr::ldr::LdrGizmo* gizmo) const
	{
		return m_gizmos.find(gizmo) != std::end(m_gizmos);
	}

	// Return the number of objects or object groups in this scene
	int Window::ObjectCount() const
	{
		assert(std::this_thread::get_id() == m_main_thread_id);
		return int(m_objects.size());
	}
	int Window::GizmoCount() const
	{
		return int(m_gizmos.size());
	}
	int Window::GuidCount() const
	{
		assert(std::this_thread::get_id() == m_main_thread_id);
		return int(m_guids.size());
	}

	// Enumerate the guids associated with this window
	void Window::EnumGuids(View3D_EnumGuidsCB enum_guids_cb, void* ctx)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);
		for (auto& guid : m_guids)
		{
			if (enum_guids_cb(ctx, guid)) continue;
			break;
		}
	}

	// Enumerate the objects associated with this window
	void Window::EnumObjects(View3D_EnumObjectsCB enum_objects_cb, void* ctx)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);
		for (auto& object : m_objects)
		{
			if (enum_objects_cb(ctx, object)) continue;
			break;
		}
	}
	void Window::EnumObjects(View3D_EnumObjectsCB enum_objects_cb, void* ctx, GUID const* context_id, int count, bool all_except)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);
		for (auto& object : m_objects)
		{
			if (!all_except && !pr::contains(context_id, context_id+count, object->m_context_id)) continue;
			if ( all_except &&  pr::contains(context_id, context_id+count, object->m_context_id)) continue;
			if (enum_objects_cb(ctx, object)) continue;
			break;
		}
	}

	// Add/Remove an object to this window
	void Window::Add(pr::ldr::LdrObject* object)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);
		auto iter = m_objects.find(object);
		if (iter == std::end(m_objects))
		{
			m_objects.insert(iter, object);
			m_guids.insert(object->m_context_id);
			ObjectContainerChanged(&object->m_context_id, 1);
		}
	}
	void Window::Remove(pr::ldr::LdrObject* object)
	{
		// 'm_guids' may be out of date now, but it doesn't really matter.
		// It's used to track the groups of objects added to the window.
		// A group with zero members is still a group.
		assert(std::this_thread::get_id() == m_main_thread_id);
		auto count = m_objects.size();

		// Remove the object
		m_objects.erase(object);

		// Notify if changed
		if (m_objects.size() != count)
			ObjectContainerChanged(&object->m_context_id, 1);

		// Sanity check, make sure there are no other references to 'object' still
		#if PR_DBG
		{
			pr::MultiCast<RenderingCB>::Lock mclock(OnRendering);
			for (auto& cb : mclock)
				assert(cb.m_ctx != object);
		}
		{
			pr::MultiCast<SceneChangedCB>::Lock mclock(OnSceneChanged);
			for (auto& cb : mclock)
				assert(cb.m_ctx != object);
		}
		#endif
	}

	// Add/Remove a gizmo to this window
	void Window::Add(pr::ldr::LdrGizmo* gizmo)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);
		auto iter = m_gizmos.find(gizmo);
		if (iter == std::end(m_gizmos))
		{
			m_gizmos.insert(iter, gizmo);
			ObjectContainerChanged(nullptr, 0);
		}
	}
	void Window::Remove(pr::ldr::LdrGizmo* gizmo)
	{
		m_gizmos.erase(gizmo);
		ObjectContainerChanged(nullptr, 0);
	}

	// Remove all objects from this scene
	void Window::RemoveAllObjects()
	{
		assert(std::this_thread::get_id() == m_main_thread_id);

		// Make a copy of the guids
		pr::vector<GUID> context_ids(std::begin(m_guids), std::end(m_guids));

		// Remove the objects and guids
		m_objects.clear();
		m_guids.clear();

		// Notify that the scene has changed
		ObjectContainerChanged(context_ids.data(), int(context_ids.size()));
	}

	// Add/Remove all objects to this window with the given context id (or not with)
	void Window::AddObjectsById(GUID const* context_id, int count, bool all_except)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);

		GuidCont new_guids;
		auto old_count = m_objects.size();
		for (auto& src : m_dll->m_sources.Sources())
		{
			if (!all_except && !pr::contains(context_id, context_id+count, src.second.m_context_id)) continue;
			if ( all_except &&  pr::contains(context_id, context_id+count, src.second.m_context_id)) continue;
			if (all_except) new_guids.push_back(src.second.m_context_id);

			// Add objects from this source
			for (auto& obj : src.second.m_objects)
				m_objects.insert(obj.get());
		}
		if (m_objects.size() != old_count)
		{
			if (!all_except)
			{
				m_guids.insert(context_id, context_id + count);
				ObjectContainerChanged(context_id, count);
			}
			else
			{
				m_guids.insert(std::begin(new_guids), std::end(new_guids));
				ObjectContainerChanged(new_guids.data(), int(new_guids.size()));
			}
		}
	}
	void Window::RemoveObjectsById(GUID const* context_id, int count, bool all_except, bool remove_objects_only)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);
		auto given = std::initializer_list<GUID>(context_id, context_id + count);
		auto old_count = m_objects.size();

		// Build a collection of the guids to keep
		auto ids = m_guids;
		if (all_except)
			pr::erase_if(ids, [=](auto& id){ return !pr::contains(given, id); });
		else
			pr::erase_if(ids, [=](auto& id){ return pr::contains(given, id); });

		// Remove objects not in the valid set
		pr::erase_if(m_objects, [&](auto* obj){ return ids.count(obj->m_context_id) == 0; });

		// Remove context ids
		if (!remove_objects_only)
			m_guids = std::move(ids);

		// Notify if changed
		if (m_objects.size() != old_count)
		{
			if (!all_except)
			{
				ObjectContainerChanged(context_id, count);
			}
			else
			{
				pr::vector<GUID> guids(std::begin(m_guids), std::end(m_guids));
				ObjectContainerChanged(guids.data(), int(guids.size()));
			}
		}
	}

	// Return a bounding box containing the scene objects
	pr::BBox Window::BBox() const
	{
		return BBox([](pr::ldr::LdrObject const&) { return true; });
	}
		
	// Reset the scene camera, using it's current forward and up directions, to view all objects in the scene
	void Window::ResetView()
	{
		auto c2w = m_camera.CameraToWorld();
		ResetView(-c2w.z, c2w.y);
	}

	// Reset the scene camera to view all objects in the scene
	void Window::ResetView(pr::v4 const& forward, pr::v4 const& up, float dist, bool preserve_aspect, bool commit)
	{
		ResetView(SceneBounds(EView3DSceneBounds::All, 0, nullptr), forward, up, dist, preserve_aspect, commit);
	}

	// Reset the camera to view a bbox
	void Window::ResetView(pr::BBox const& bbox, pr::v4 const& forward, pr::v4 const& up, float dist, bool preserve_aspect, bool commit)
	{
		m_camera.View(bbox, forward, up, dist, preserve_aspect, commit);
	}

	// Return the bounding box of objects in this scene
	pr::BBox Window::SceneBounds(EView3DSceneBounds bounds, int except_count, GUID const* except)
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
				{
					bbox = pr::BBoxReset;
					for (auto& obj : m_objects)
					{
						if (pr::AllSet(obj->m_flags, pr::ldr::ELdrFlags::BBoxInvisible)) continue;
						if (pr::contains(except_arr, obj->m_context_id)) continue;
						pr::Encompass(bbox, obj->BBoxWS(true));
					}
					m_bbox_scene = bbox;
				}
				bbox = m_bbox_scene;
				break;
			}
		case EView3DSceneBounds::Selected:
			{
				bbox = pr::BBoxReset;
				for (auto& obj : m_objects)
				{
					if (pr::AllSet(obj->m_flags, pr::ldr::ELdrFlags::BBoxInvisible)) continue;
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
					if (pr::AllSet(obj->m_flags, pr::ldr::ELdrFlags::BBoxInvisible)) continue;
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
		return bbox.valid() ? bbox : pr::BBoxUnit;
	}

	// Set the position and size of the selection box. If 'bbox' is 'BBoxReset' the selection box is not shown
	void Window::SetSelectionBox(pr::BBox const& bbox, pr::m3x4 const& ori)
	{
		if (bbox == pr::BBoxReset)
		{
			// Flag to not include the selection box
			m_selection_box.m_i2w.pos.w = 0;
		}
		else
		{
			m_selection_box.m_i2w =
				pr::m4x4(ori, pr::v4Origin) *
				pr::m4x4::Scale(bbox.m_radius.x, bbox.m_radius.y, bbox.m_radius.z, bbox.m_centre);
		}
	}

	// Position the selection box to include the selected objects
	void Window::SelectionBoxFitToSelected()
	{
		// Find the bounds of the selected objects
		auto bbox = pr::BBoxReset;
		for (auto& obj : m_objects)
		{
			obj->Apply([&](pr::ldr::LdrObject const* c)
			{
				if (!pr::AllSet(c->m_flags, pr::ldr::ELdrFlags::Selected))
					return true;

				auto bb = c->BBoxWS(true);
				pr::Encompass(bbox, bb);
				return false;
			}, "");
		}
		SetSelectionBox(bbox);
	}

	// Convert a screen space point to a normalised screen space point
	pr::v2 Window::SSPointToNSSPoint(pr::v2 const& ss_point) const
	{
		return m_scene.m_viewport.SSPointToNSSPoint(ss_point);
	}
	pr::v2 Window::NSSPointToSSPoint(pr::v2 const& nss_point) const
	{
		return m_scene.m_viewport.NSSPointToSSPoint(nss_point);
	}

	// Invoke the settings changed callback
	void Window::NotifySettingsChanged()
	{
		OnSettingsChanged.Raise(this);
	}

	// Invoke the rendering event
	void Window::NotifyRendering()
	{
		OnRendering.Raise(this);
	}

	// Call InvalidateRect on the HWND associated with this window
	void Window::InvalidateRect(RECT const* rect, bool erase)
	{
		::InvalidateRect(m_hwnd, rect, erase);
	}

	// Called when objects are added/removed from this window
	void Window::ObjectContainerChanged(GUID const* context_ids, int count)
	{
		// Reset the drawlists so that removed objects are no longer in the drawlist
		m_scene.ClearDrawlists();

		// Invalidate cached members
		m_bbox_scene = pr::BBoxReset;

		// Notify scene changed
		OnSceneChanged.Raise(this, context_ids, count);
	}

	// Show/Hide the object manager for the scene
	void Window::ShowObjectManager(bool show)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);
		auto& ui = ObjectManagerUI();
		ui.Show();
		ui.Populate(m_objects);
		ui.Visible(show);
	}

	// Show/Hide the measure tool
	void Window::ShowMeasureTool(bool show)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);
		auto& ui = LdrMeasureUI();
		ui.SetReadPoint(&ReadPoint, this);
		ui.Visible(show);
	}

	// Show/Hide the angle tool
	void Window::ShowAngleTool(bool show)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);
		auto& ui = LdrAngleUI();
		ui.SetReadPoint(&ReadPoint, this);
		ui.Visible(show);
	}

	// Create stock models such as the focus point, origin, etc
	void Window::CreateStockModels()
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
			static float const sz = 1.0f;
			static float const dd = 0.8f;
			static pr::v4 const verts[] =
			{
				pr::v4(-sz, -sz, -sz, 1.0f), pr::v4(-dd, -sz, -sz, 1.0f), pr::v4(-sz, -dd, -sz, 1.0f), pr::v4(-sz, -sz, -dd, 1.0f),
				pr::v4( sz, -sz, -sz, 1.0f), pr::v4( sz, -dd, -sz, 1.0f), pr::v4( dd, -sz, -sz, 1.0f), pr::v4( sz, -sz, -dd, 1.0f),
				pr::v4( sz,  sz, -sz, 1.0f), pr::v4( dd,  sz, -sz, 1.0f), pr::v4( sz,  dd, -sz, 1.0f), pr::v4( sz,  sz, -dd, 1.0f),
				pr::v4(-sz,  sz, -sz, 1.0f), pr::v4(-sz,  dd, -sz, 1.0f), pr::v4(-dd,  sz, -sz, 1.0f), pr::v4(-sz,  sz, -dd, 1.0f),
				pr::v4(-sz, -sz,  sz, 1.0f), pr::v4(-dd, -sz,  sz, 1.0f), pr::v4(-sz, -dd,  sz, 1.0f), pr::v4(-sz, -sz,  dd, 1.0f),
				pr::v4( sz, -sz,  sz, 1.0f), pr::v4( sz, -dd,  sz, 1.0f), pr::v4( dd, -sz,  sz, 1.0f), pr::v4( sz, -sz,  dd, 1.0f),
				pr::v4( sz,  sz,  sz, 1.0f), pr::v4( dd,  sz,  sz, 1.0f), pr::v4( sz,  dd,  sz, 1.0f), pr::v4( sz,  sz,  dd, 1.0f),
				pr::v4(-sz,  sz,  sz, 1.0f), pr::v4(-sz,  dd,  sz, 1.0f), pr::v4(-dd,  sz,  sz, 1.0f), pr::v4(-sz,  sz,  dd, 1.0f),
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
}
