//***************************************************************************************************
// View 3D
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************

#include "view3d/dll/forward.h"
#include "view3d/dll/window.h"
#include "view3d/dll/context.h"

using namespace pr;
using namespace pr::rdr;
using namespace pr::ldr;

namespace view3d
{
	// Default window construction settings
	WndSettings Window::Settings(HWND hwnd, View3DWindowOptions const& opts)
	{
		// Null hwnd is allowed when off-screen only rendering
		auto rect = RECT{};
		if (hwnd != 0)
			::GetClientRect(hwnd, &rect);

		auto settings        = WndSettings(hwnd, true, opts.m_gdi_compatible_backbuffer != 0, pr::To<iv2>(rect));
		settings.m_multisamp = MultiSamp(opts.m_multisampling);
		settings.m_name      = opts.m_dbg_name;
		return settings;
	}

	// Return the focus point of the camera in this draw set
	v4 __stdcall ReadPoint(void* ctx)
	{
		if (ctx == 0) return v4Origin;
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
		,m_fill_mode(EFillMode::Solid)
		,m_cull_mode(ECullMode::Back)
		,m_background_colour(0xFF808080U)
		,m_focus_point()
		,m_origin_point()
		,m_bbox_model()
		,m_selection_box()
		,m_anim_data()
		,m_focus_point_size(1.0f)
		,m_origin_point_size(1.0f)
		,m_focus_point_visible(false)
		,m_origin_point_visible(false)
		,m_selection_box_visible(false)
		,m_invalidated(false)
		,m_editor_ui()
		,m_obj_cont_ui()
		,m_measure_tool_ui()
		,m_angle_tool_ui()
		,m_editors()
		,m_settings()
		,m_bbox_scene(BBoxReset)
		,m_main_thread_id(std::this_thread::get_id())
		,ReportError()
	{
		try
		{
			// Notes:
			// - Don't observe the Context sources store for changes. The context handles this for us
			ReportError += StaticCallBack(opts.m_error_cb, opts.m_error_cb_ctx);

			// Set the initial aspect ratio
			auto rt_area = m_wnd.RenderTargetSize();
			if (rt_area != iv2Zero)
				m_camera.Aspect(rt_area.x / float(rt_area.y));

			// The light for the scene
			m_light.m_type           = rdr::ELight::Directional;
			m_light.m_ambient        = Colour32(0x00101010U);
			m_light.m_diffuse        = Colour32(0xFF808080U);
			m_light.m_specular       = Colour32(0x00404040U);
			m_light.m_specular_power = 1000.0f;
			m_light.m_direction      = -v4ZAxis;
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
		AnimControl(EView3DAnimCommand::Stop);

		Close();
		m_scene.RemoveInstance(m_focus_point);
		m_scene.RemoveInstance(m_origin_point);
		m_scene.RemoveInstance(m_bbox_model);
		m_scene.RemoveInstance(m_selection_box);
	}

	// Get/Set the scene viewport
	View3DViewport Window::Viewport() const
	{
		auto& scene_vp = m_scene.m_viewport;
		View3DViewport vp = {};
		vp.m_x = scene_vp.TopLeftX;
		vp.m_y = scene_vp.TopLeftY;
		vp.m_width = scene_vp.Width;
		vp.m_height = scene_vp.Height;
		vp.m_min_depth = scene_vp.MinDepth;
		vp.m_max_depth = scene_vp.MaxDepth;
		return vp;
	}
	void Window::Viewport(View3DViewport vp)
	{
		auto& scene_vp = m_scene.m_viewport;
		scene_vp.TopLeftX = vp.m_x;
		scene_vp.TopLeftY = vp.m_y;
		scene_vp.Width = vp.m_width;
		scene_vp.Height = vp.m_height;
		scene_vp.MinDepth = vp.m_min_depth;
		scene_vp.MaxDepth = vp.m_max_depth;
		NotifySettingsChanged(EView3DSettings::Scene_Viewport);
	}

	// Render this window into whatever render target is currently set
	void Window::Render()
	{
		// Notes:
		// - Don't be tempted to call 'Validate()' at the start of Render so that objects
		//   added to the scene during the render re-invalidate. Instead defer the invalidate
		//   to the next windows event.

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
				m_focus_point.m_i2w = m4x4::Scale(sz, sz, sz, pt_ws);
				m_focus_point.m_c2s = v_camera.CameraToScreen();
				m_scene.AddInstance(m_focus_point);
			}
			if (m_origin_point_visible)
			{
				// Scale the camera space X,Y coords
				auto pt_cs = m_camera.WorldToCamera() * v4Origin;
				pt_cs.x *= viewarea_v.x / viewarea_c.x;
				pt_cs.y *= viewarea_v.y / viewarea_c.y;
				auto pt_ws = m_camera.CameraToWorld() * pt_cs;

				auto sz = m_origin_point_size * screen_fraction * abs(pt_cs.z);
				m_origin_point.m_i2w = m4x4::Scale(sz, sz, sz, pt_ws);
				m_origin_point.m_c2s = v_camera.CameraToScreen();
				m_scene.AddInstance(m_origin_point);
			}
		}

		// Selection box
		if (m_selection_box_visible)
		{
			// Transform is updated by the user or by a call to SetSelectionBoxToSelected()
			// 'm_selection_box.m_i2w.pos.w' is zero when there is no selection.
			// Update the selection box if necessary
			SelectionBoxFitToSelected();
			if (m_selection_box.m_i2w.pos.w != 0)
				m_scene.AddInstance(m_selection_box);
		}

		// Set the light source
		m_scene.m_global_light = m_light;

		// Get the animation clock time
		auto anim_time = (float)m_anim_data.m_clock.load().count();
		assert(IsFinite(anim_time));

		// Add objects from the window to the scene
		for (auto& obj : m_objects)
		{
			// Apply the fill mode and cull mode to user models
			obj->Apply([=](LdrObject* obj)
				{
					if (obj->m_model == nullptr || AllSet(obj->m_flags, ELdrFlags::SceneBoundsExclude)) return true;
					for (auto& nug : obj->m_model->m_nuggets)
					{
						nug.FillMode(m_fill_mode);
						nug.CullMode(m_cull_mode);
					}
					return true;
				}, "");

			// Recursively add the object to the scene
			obj->AddToScene(m_scene, anim_time);

			// Only show bounding boxes for things that contribute to the scene bounds.
			if (m_scene.m_diag.m_bboxes_visible && !AllSet(obj->m_flags, ELdrFlags::SceneBoundsExclude))
				obj->AddBBoxToScene(m_scene, anim_time);
		}

		// Add gizmos from the window to the scene
		for (auto& giz : m_gizmos)
		{
			giz->AddToScene(m_scene);
		}

		// Add the measure tool objects if the window is visible
		if (m_measure_tool_ui != nullptr && LdrMeasureUI().Visible() && LdrMeasureUI().Gfx())
			LdrMeasureUI().Gfx()->AddToScene(m_scene);

		// Add the angle tool objects if the window is visible
		if (m_angle_tool_ui != nullptr && LdrAngleUI().Visible() && LdrAngleUI().Gfx())
			LdrAngleUI().Gfx()->AddToScene(m_scene);

		// Set the background colour
		m_scene.m_bkgd_colour = m_background_colour;

		// Render the scene
		m_scene.Render();
	}
	void Window::Present()
	{
		m_wnd.Present();

		// No longer invalidated
		Validate();
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
			m_measure_tool_ui.reset(new ldr::LdrMeasureUI(m_hwnd, ReadPoint, this, m_dll->m_rdr));

		return *m_measure_tool_ui;
	}

	// The angle measurement tool UI
	LdrAngleUI& Window::LdrAngleUI()
	{
		if (m_angle_tool_ui == nullptr)
			m_angle_tool_ui.reset(new ldr::LdrAngleUI(m_hwnd, ReadPoint, this, m_dll->m_rdr));

		return *m_angle_tool_ui;
	}

	// Return true if 'object' is part of this scene
	bool Window::Has(LdrObject const* object, bool search_children) const
	{
		assert(std::this_thread::get_id() == m_main_thread_id);

		// Search (recursively) for a match for 'object'.
		auto name = search_children ? "" : nullptr;
		for (auto& obj : m_objects)
		{
			// 'Apply' returns false if a quick out occurred (i.e. 'object' was found)
			if (obj->Apply([=](auto* ob){ return ob != object; }, name)) continue;
			return true;
		}
		return false;
	}
	bool Window::Has(LdrGizmo const* gizmo) const
	{
		assert(std::this_thread::get_id() == m_main_thread_id);
		for (auto& giz : m_gizmos)
		{
			if (giz != gizmo) continue;
			return true;
		}
		return false;
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
	void Window::EnumObjects(View3D_EnumObjectsCB enum_objects_cb, void* ctx, GUID const* context_ids, int include_count, int exclude_count)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);
		for (auto& object : m_objects)
		{
			if (!IncludeFilter(object->m_context_id, context_ids, include_count, exclude_count)) continue;
			if (enum_objects_cb(ctx, object)) continue;
			break;
		}
	}

	// Add/Remove an object to this window
	void Window::Add(LdrObject* object)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);
		auto iter = m_objects.find(object);
		if (iter == std::end(m_objects))
		{
			m_objects.insert(iter, object);
			m_guids.insert(object->m_context_id);
			ObjectContainerChanged(EView3DSceneChanged::ObjectsAdded, &object->m_context_id, 1, object);
		}
	}
	void Window::Remove(LdrObject* object)
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
			ObjectContainerChanged(EView3DSceneChanged::ObjectsRemoved, &object->m_context_id, 1, object);
	}

	// Add/Remove a gizmo to this window
	void Window::Add(LdrGizmo* gizmo)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);
		auto iter = m_gizmos.find(gizmo);
		if (iter == std::end(m_gizmos))
		{
			m_gizmos.insert(iter, gizmo);
			ObjectContainerChanged(EView3DSceneChanged::GizmoAdded, nullptr, 0, nullptr); // todo, overload and pass 'gizmo' out
		}
	}
	void Window::Remove(LdrGizmo* gizmo)
	{
		m_gizmos.erase(gizmo);
		ObjectContainerChanged(EView3DSceneChanged::GizmoRemoved, nullptr, 0, nullptr);
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
		ObjectContainerChanged(EView3DSceneChanged::ObjectsRemoved, context_ids.data(), int(context_ids.size()), nullptr);
	}

	// Add/Remove all objects to this window with the given context ids (or not with)
	void Window::AddObjectsById(GUID const* context_ids, int include_count, int exclude_count)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);

		GuidCont new_guids;
		auto old_count = m_objects.size();
		for (auto& src_iter : m_dll->m_sources.Sources())
		{
			auto& src = src_iter.second;
			if (!IncludeFilter(src.m_context_id, context_ids, include_count, exclude_count))
				continue;

			// Add objects from this source
			new_guids.push_back(src.m_context_id);
			for (auto& obj : src.m_objects)
				m_objects.insert(obj.get());

			// Apply camera settings from this source
			if (src.m_cam_fields != ECamField::None)
			{
				auto& cam = src.m_cam;
				auto changed = EView3DSettings::Camera;
				if (AllSet(src.m_cam_fields, ECamField::C2W))
				{
					m_camera.CameraToWorld(cam.CameraToWorld());
					changed |= EView3DSettings::Camera_Position;
				}
				if (AllSet(src.m_cam_fields, ECamField::Focus))
				{
					m_camera.LookAt(cam.CameraToWorld().pos, cam.FocusPoint(), cam.CameraToWorld().y);
					changed |= EView3DSettings::Camera_Position;
					changed |= EView3DSettings::Camera_FocusDist;
				}
				if (AllSet(src.m_cam_fields, ECamField::Align))
				{
					m_camera.Align(cam.m_align);
					changed |= EView3DSettings::Camera_AlignAxis;
				}
				if (AllSet(src.m_cam_fields, ECamField::Aspect))
				{
					m_camera.Aspect(cam.Aspect());
					changed |= EView3DSettings::Camera_Aspect;
				}
				if (AllSet(src.m_cam_fields, ECamField::FovY))
				{
					m_camera.FovY(cam.FovY());
					changed |= EView3DSettings::Camera_Fov;
				}
				if (AllSet(src.m_cam_fields, ECamField::Near))
				{
					m_camera.Near(cam.Near(true), true);
					changed |= EView3DSettings::Camera_ClipPlanes;
				}
				if (AllSet(src.m_cam_fields, ECamField::Far))
				{
					m_camera.Far(cam.Far(true), true);
					changed |= EView3DSettings::Camera_ClipPlanes;
				}
				if (AllSet(src.m_cam_fields, ECamField::Ortho))
				{
					m_camera.Orthographic(cam.Orthographic());
					changed |= EView3DSettings::Camera_Orthographic;
				}

				// Notify if the camera was changed
				if (changed != EView3DSettings::Camera)
					NotifySettingsChanged(changed);
			}
		}
		if (m_objects.size() != old_count)
		{
			m_guids.insert(std::begin(new_guids), std::end(new_guids));
			ObjectContainerChanged(EView3DSceneChanged::ObjectsAdded, new_guids.data(), int(new_guids.size()), nullptr);
		}
	}
	void Window::RemoveObjectsById(GUID const* context_ids, int include_count, int exclude_count, bool keep_context_ids)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);

		// Create a set of ids to remove
		GuidSet removed;
		for (auto& id : m_guids)
		{
			if (!IncludeFilter(id, context_ids, include_count, exclude_count)) continue;
			removed.insert(id);
		}

		if (!removed.empty())
		{
			// Remove objects in the 'remove' set
			auto old_count = m_objects.size();
			pr::erase_if(m_objects, [&](auto* obj){ return removed.count(obj->m_context_id); });

			// Remove context ids
			if (!keep_context_ids)
			{
				for (auto& id : removed)
					m_guids.erase(id);
			}

			// Notify if changed
			if (m_objects.size() != old_count)
			{
				GuidCont guids(std::begin(removed), std::end(removed));
				ObjectContainerChanged(EView3DSceneChanged::ObjectsRemoved, guids.data(), int(guids.size()), nullptr);
			}
		}
	}

	// Return a bounding box containing the scene objects
	BBox Window::BBox() const
	{
		return BBox([](LdrObject const&) { return true; });
	}
		
	// Reset the scene camera, using it's current forward and up directions, to view all objects in the scene
	void Window::ResetView()
	{
		auto c2w = m_camera.CameraToWorld();
		ResetView(-c2w.z, c2w.y);
	}

	// Reset the scene camera to view all objects in the scene
	void Window::ResetView(v4 const& forward, v4 const& up, float dist, bool preserve_aspect, bool commit)
	{
		ResetView(SceneBounds(EView3DSceneBounds::All, 0, nullptr), forward, up, dist, preserve_aspect, commit);
	}

	// Reset the camera to view a bbox
	void Window::ResetView(pr::BBox const& bbox, v4 const& forward, v4 const& up, float dist, bool preserve_aspect, bool commit)
	{
		m_camera.View(bbox, forward, up, dist, preserve_aspect, commit);
	}

	// Return the bounding box of objects in this scene
	BBox Window::SceneBounds(EView3DSceneBounds bounds, int except_count, GUID const* except)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);
		auto except_arr = std::make_span(except, except_count);
		auto pred = [](LdrObject const& ob){ return !AllSet(ob.m_flags, ELdrFlags::SceneBoundsExclude); };

		pr::BBox bbox;
		switch (bounds)
		{
		default:
			{
				assert(!"Unknown scene bounds type");
				bbox = BBoxUnit;
				break;
			}
		case EView3DSceneBounds::All:
			{
				// Update the scene bounding box if out of date
				if (m_bbox_scene == BBoxReset)
				{
					bbox = BBoxReset;
					for (auto& obj : m_objects)
					{
						if (!pred(*obj)) continue;
						if (pr::contains(except_arr, obj->m_context_id)) continue;
						Encompass(bbox, obj->BBoxWS(true, pred));
					}
					m_bbox_scene = bbox;
				}
				bbox = m_bbox_scene;
				break;
			}
		case EView3DSceneBounds::Selected:
			{
				bbox = BBoxReset;
				for (auto& obj : m_objects)
				{
					if (!pred(*obj)) continue;
					if (!AllSet(obj->m_flags, ELdrFlags::Selected)) continue;
					if (pr::contains(except_arr, obj->m_context_id)) continue;
					Encompass(bbox, obj->BBoxWS(true, pred));
				}
				break;
			}
		case EView3DSceneBounds::Visible:
			{
				bbox = BBoxReset;
				for (auto& obj : m_objects)
				{
					if (!pred(*obj)) continue;
					if (AllSet(obj->m_flags, ELdrFlags::Hidden)) continue;
					if (pr::contains(except_arr, obj->m_context_id)) continue;
					Encompass(bbox, obj->BBoxWS(true, pred));
				}
				break;
			}
		}
		return bbox.valid() ? bbox : BBoxUnit;
	}

	// Set the position and size of the selection box. If 'bbox' is 'BBoxReset' the selection box is not shown
	void Window::SetSelectionBox(pr::BBox const& bbox, m3x4 const& ori)
	{
		if (bbox == BBoxReset)
		{
			// Flag to not include the selection box
			m_selection_box.m_i2w.pos.w = 0;
		}
		else
		{
			m_selection_box.m_i2w =
				m4x4(ori, v4Origin) *
				m4x4::Scale(bbox.m_radius.x, bbox.m_radius.y, bbox.m_radius.z, bbox.m_centre);
		}
	}

	// Position the selection box to include the selected objects
	void Window::SelectionBoxFitToSelected()
	{
		// Find the bounds of the selected objects
		auto bbox = BBoxReset;
		for (auto& obj : m_objects)
		{
			obj->Apply([&](LdrObject const* c)
			{
				if (!AllSet(c->m_flags, ELdrFlags::Selected) || AllSet(c->m_flags, ELdrFlags::SceneBoundsExclude))
					return true;

				auto bb = c->BBoxWS(true);
				Encompass(bbox, bb);
				return false;
			}, "");
		}
		SetSelectionBox(bbox);
	}

	// True if animation is currently active
	bool Window::Animating() const
	{
		return m_anim_data.m_thread.joinable();
	}
	
	// Get/Set the value of the animation clock
	seconds_t Window::AnimTime() const
	{
		return m_anim_data.m_clock.load();
	}
	void Window::AnimTime(seconds_t clock)
	{
		assert(IsFinite(clock.count()) && clock.count() >= 0);
		m_anim_data.m_clock.store(clock);
	}

	// Control animation
	void Window::AnimControl(EView3DAnimCommand command, seconds_t time)
	{
		using namespace std::chrono;
		static constexpr auto tick_size_s = seconds_t(0.01);

		// Callback function that is polled as fast as the message queue will allow
		static auto const AnimTick = [](void* ctx)
		{
			auto& me = *reinterpret_cast<Window*>(ctx);
			me.Invalidate();
			me.OnAnimationEvent(&me, EView3DAnimCommand::Step, me.m_anim_data.m_clock.load().count());
		};

		switch (command)
		{
		case EView3DAnimCommand::Reset:
			{
				AnimControl(EView3DAnimCommand::Stop);
				assert(IsFinite(time.count()));
				m_anim_data.m_clock.store(time);
				Invalidate();
				break;
			}
		case EView3DAnimCommand::Play:
			{
				AnimControl(EView3DAnimCommand::Stop);
				m_anim_data.m_thread = std::thread([&]
				{
					// 'time' is the seconds/second step rate
					auto rate = time.count();
					auto start = system_clock::now();
					auto issue = m_anim_data.m_issue.load();
					for (; issue == m_anim_data.m_issue; std::this_thread::sleep_for(tick_size_s))
					{
						// Every loop is a tick, and the step size is 'time'. 
						// If 'time' is zero, then stepping is real-time and the step size is 'elapsed' 
						auto increment = rate == 0.0 ? system_clock::now() - start : tick_size_s * rate;
						start = system_clock::now();

						// Update the animation clock
						m_anim_data.m_clock.store(m_anim_data.m_clock.load() + increment);
					}
				});
				m_wnd.m_rdr->AddPollCB({ AnimTick, this });
				break;
			}
		case EView3DAnimCommand::Stop:
			{
				m_wnd.m_rdr->RemovePollCB({ AnimTick, this });
				++m_anim_data.m_issue;
				if (m_anim_data.m_thread.joinable())
					m_anim_data.m_thread.join();
				
				break;
			}
		case EView3DAnimCommand::Step:
			{
				AnimControl(EView3DAnimCommand::Stop);
				m_anim_data.m_clock = m_anim_data.m_clock.load() + time;
				Invalidate();
				break;
			}
		default:
			throw std::runtime_error(FmtS("Unknown animation command: %d", command));
		}

		// Notify of the animation event
		OnAnimationEvent(this, command, m_anim_data.m_clock.load().count());
	}

	// Convert a screen space point to a normalised screen space point
	v2 Window::SSPointToNSSPoint(v2 const& ss_point) const
	{
		return m_scene.m_viewport.SSPointToNSSPoint(ss_point);
	}
	v2 Window::NSSPointToSSPoint(v2 const& nss_point) const
	{
		return m_scene.m_viewport.NSSPointToSSPoint(nss_point);
	}

	// Invoke the settings changed callback
	void Window::NotifySettingsChanged(EView3DSettings setting)
	{
		OnSettingsChanged(this, setting);
	}

	// Invoke the rendering event
	void Window::NotifyRendering()
	{
		OnRendering(this);
	}

	// Call InvalidateRect on the HWND associated with this window
	void Window::InvalidateRect(RECT const* rect, bool erase)
	{
		if (m_hwnd != nullptr)
			::InvalidateRect(m_hwnd, rect, erase);
		
		if (!m_invalidated)
			OnInvalidated(this);

		// The window becomes validated again when 'Present()' or 'Validate()' is called.
		m_invalidated = true;
	}
	void Window::Invalidate(bool erase)
	{
		InvalidateRect(nullptr, erase);
	}

	// Clear the invalidated state for the window
	void Window::Validate()
	{
		m_invalidated = false;
	}

	// Called when objects are added/removed from this window
	void Window::ObjectContainerChanged(EView3DSceneChanged change_type, GUID const* context_ids, int count, LdrObject* object)
	{
		// Reset the drawlists so that removed objects are no longer in the drawlist
		if (change_type == EView3DSceneChanged::ObjectsRemoved)
			m_scene.ClearDrawlists();

		// Invalidate cached members
		m_bbox_scene = BBoxReset;

		// Notify scene changed
		View3DSceneChanged args = {change_type, context_ids, count, object};
		OnSceneChanged(this, args);
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

	// Get/Set the window fill mode
	EFillMode Window::FillMode() const
	{
		return m_fill_mode;
	}
	void Window::FillMode(EFillMode fill_mode)
	{
		if (m_fill_mode == fill_mode) return;
		m_fill_mode = fill_mode;
		NotifySettingsChanged(EView3DSettings::Scene_FilllMode);
		Invalidate();
	}

	// Get/Set the window cull mode
	ECullMode Window::CullMode() const
	{
		return m_cull_mode;
	}
	void Window::CullMode(ECullMode cull_mode)
	{
		if (m_cull_mode == cull_mode) return;
		m_cull_mode = cull_mode;
		NotifySettingsChanged(EView3DSettings::Scene_CullMode);
		Invalidate();
	}

	// Get/Set the window background colour
	Colour32 Window::BackgroundColour() const
	{
		return m_background_colour;
	}
	void Window::BackgroundColour(Colour32 colour)
	{
		if (m_background_colour == colour) return;
		m_background_colour = colour;
		NotifySettingsChanged(EView3DSettings::Scene_BackgroundColour);
		Invalidate();
	}

	// Get/Set the window background colour
	int Window::MultiSampling() const
	{
		return m_wnd.MultiSampling().Count;
	}
	void Window::MultiSampling(int multisampling)
	{
		if (MultiSampling() == multisampling) return;
		MultiSamp ms(multisampling);
		m_wnd.MultiSampling(ms);
		NotifySettingsChanged(EView3DSettings::Scene_Multisampling);
		Invalidate();
	}

	// Show/Hide the focus point
	bool Window::FocusPointVisible() const
	{
		return m_focus_point_visible;
	}
	void Window::FocusPointVisible(bool vis)
	{
		if (m_focus_point_visible == vis) return;
		m_focus_point_visible = vis;
		NotifySettingsChanged(EView3DSettings::General_FocusPointVisible);
	}

	// Show/Hide the origin point
	bool Window::OriginPointVisible() const
	{
		return m_origin_point_visible;
	}
	void Window::OriginPointVisible(bool vis)
	{
		if (m_origin_point_visible == vis) return;
		m_origin_point_visible = vis;
		NotifySettingsChanged(EView3DSettings::General_OriginPointVisible);
	}

	// Show/Hide the bounding boxes
	bool Window::BBoxesVisible() const
	{
		return m_scene.m_diag.m_bboxes_visible;
	}
	void Window::BBoxesVisible(bool vis)
	{
		if (m_scene.m_diag.m_bboxes_visible == vis) return;
		m_scene.m_diag.m_bboxes_visible = vis;
		NotifySettingsChanged(EView3DSettings::Diagnostics_BBoxesVisible);
	}

	// Get/Set the length of the displayed vertex normals
	float Window::NormalsLength() const
	{
		return m_scene.m_diag.m_normal_lengths;
	}
	void Window::NormalsLength(float length)
	{
		if (m_scene.m_diag.m_normal_lengths == length) return;
		m_scene.m_diag.m_normal_lengths = length;
		Invalidate();
		NotifySettingsChanged(EView3DSettings::Diagnostics_NormalsLength);
	}

	// Get/Set the colour of the displayed vertex normals
	Colour32 Window::NormalsColour() const
	{
		return m_scene.m_diag.m_normal_colour;
	}
	void Window::NormalsColour(Colour32 colour)
	{
		if (m_scene.m_diag.m_normal_colour == colour) return;
		m_scene.m_diag.m_normal_colour = colour;
		Invalidate();
		NotifySettingsChanged(EView3DSettings::Diagnostics_NormalsColour);
	}

	// Get/Set the colour of the displayed vertex normals
	v2 Window::FillModePointsSize() const
	{
		auto shdr = static_cast<PointSpritesGS const*>(m_scene.m_diag.m_gs_fillmode_points.get());
		return shdr->m_size;
	}
	void Window::FillModePointsSize(v2 size)
	{
		if (FillModePointsSize() == size) return;
		auto shdr = static_cast<PointSpritesGS*>(m_scene.m_diag.m_gs_fillmode_points.get());
		shdr->m_size = size;
		Invalidate();
		NotifySettingsChanged(EView3DSettings::Diagnostics_FillModePointsSize);
	}

	// Show/Hide the selection box
	bool Window::SelectionBoxVisible() const
	{
		return m_selection_box_visible;
	}
	void Window::SelectionBoxVisible(bool vis)
	{
		if (m_selection_box_visible == vis) return;
		m_selection_box_visible = vis;
		NotifySettingsChanged(EView3DSettings::General_SelectionBoxVisible);
	}

	// Cast rays into the scene, returning hit info for the nearest intercept for each ray
	void Window::HitTest(View3DHitTestRay const* rays, View3DHitTestResult* hits, int ray_count, float snap_distance, EView3DHitTestFlags flags, GUID const* context_ids, int include_count, int exclude_count)
	{
		// Set up the ray cast
		pr::vector<HitTestRay> ray_casts;
		for (auto& ray : std::make_span(rays, ray_count))
		{
			HitTestRay r = {};
			r.m_ws_origin = To<v4>(ray.m_ws_origin);
			r.m_ws_direction = To<v4>(ray.m_ws_direction);
			ray_casts.push_back(r);
		}

		// Initialise the results
		View3DHitTestResult invalid = {};
		invalid.m_distance = maths::float_max;
		for (auto& r : std::make_span(hits, ray_count))
			r = invalid;

		// Create an include function based on the context ids
		RayCastStep::InstFilter include = [=](BaseInstance const* bi)
		{
			return IncludeFilter(cast<LdrObject>(bi)->m_context_id, context_ids, include_count, exclude_count);
		};

		// Do the ray casts into the scene and save the results
		m_scene.HitTest(ray_casts.data(), int(ray_casts.size()), snap_distance, static_cast<EHitTestFlags>(flags), include, [=](HitTestResult const& hit)
		{
			// Check that 'hit.m_instance' is a valid instance in this scene.
			// It could be a child instance, we need to search recursively for a match
			auto ldr_obj = cast<LdrObject>(hit.m_instance);

			// Not an object in this scene, keep looking
			// This needs to come first in case 'ldr_obj' points to an object that has been deleted.
			if (!Has(ldr_obj, true))
				return true;

			// Not visible to hit tests, keep looking
			if (AllSet(ldr_obj->Flags(), ELdrFlags::HitTestExclude))
				return true;

			// The intercepts are already sorted from nearest to furtherest
			// So we can just accept the first intercept as the hit test.

			// Save the hit
			auto& result = hits[hit.m_ray_index];
			result.m_ws_ray_origin     = view3d::To<View3DV4>(hit.m_ws_origin);
			result.m_ws_ray_direction  = view3d::To<View3DV4>(hit.m_ws_direction);
			result.m_ws_intercept      = view3d::To<View3DV4>(hit.m_ws_intercept);
			result.m_distance          = hit.m_distance;
			result.m_obj               = const_cast<View3DObject>(ldr_obj);
			result.m_snap_type         = static_cast<EView3DSnapType>(hit.m_snap_type);
			return false;
		});
	}

	// Get/Set the global environment map for this window
	View3DCubeMap Window::EnvMap() const
	{
		return m_scene.m_global_envmap.get();
	}
	void Window::EnvMap(View3DCubeMap env_map)
	{
		m_scene.m_global_envmap = TextureCubePtr(env_map, true);
	}

	// Implements standard key bindings. Returns true if handled
	bool Window::TranslateKey(EKeyCodes key)
	{
		// Notes:
		//  - This method is intended as a simple default for key bindings. Applications should
		//    probably not call this, but handled the keys bindings separately. This helps to show
		//    the expected behaviour of some common bindings though.

		auto code = key & EKeyCodes::KeyCode;
		auto modifiers = key & EKeyCodes::Modifiers;
		switch (code)
		{
		case EKeyCodes::F7:
			{
				auto up = LengthSq(m_camera.m_align) > maths::tinyf ? m_camera.m_align : v4YAxis;
				auto forward = up.z > up.y ? v4YAxis : -v4ZAxis;

				auto bounds =
					(modifiers & EKeyCodes::Shift) != 0 ? EView3DSceneBounds::Selected :
					(modifiers & EKeyCodes::Control) != 0 ? EView3DSceneBounds::Visible :
					EView3DSceneBounds::All;

				ResetView(SceneBounds(bounds, 0, nullptr), forward, up, 0, true, true);
				Invalidate();
				return true;
			}
		case EKeyCodes::Space:
			{
				ShowObjectManager(true);
				return true;
			}
		case EKeyCodes::W:
			{
				if ((modifiers & EKeyCodes::Control) != 0)
				{
					switch (FillMode())
					{
					case EFillMode::Default:
					case EFillMode::Solid:     FillMode(EFillMode::Wireframe); break;
					case EFillMode::Wireframe: FillMode(EFillMode::SolidWire); break;
					case EFillMode::SolidWire: FillMode(EFillMode::Points); break;
					case EFillMode::Points:    FillMode(EFillMode::Solid); break;
					default: throw std::runtime_error("Unknown fill mode");
					}
					Invalidate();
				}
				return true;
			}
		}
		return false;
	}

	// Create stock models such as the focus point, origin, etc
	void Window::CreateStockModels()
	{
		// Create the focus point/origin models
		m_focus_point.m_model = m_dll->m_rdr.m_mdl_mgr.m_basis;
		m_focus_point.m_tint = Colour32One;
		m_focus_point.m_i2w = m4x4Identity;
		m_origin_point.m_model = m_dll->m_rdr.m_mdl_mgr.m_basis;
		m_origin_point.m_tint = Colour32Gray;
		m_origin_point.m_i2w = m4x4Identity;

		// Create the selection box model
		m_selection_box.m_model = m_dll->m_rdr.m_mdl_mgr.m_selection_box;
		m_selection_box.m_tint = Colour32White;
		m_selection_box.m_i2w = m4x4Identity;
	}
}
