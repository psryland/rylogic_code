//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "view3d-12/src/dll/v3d_window.h"
#include "pr/view3d-12/ldraw/ldr_object.h"
#include "pr/view3d-12/ldraw/ldr_gizmo.h"
#include "view3d-12/src/dll/context.h"

namespace pr::rdr12
{
	// Default window construction settings
	WndSettings ToWndSettings(HWND hwnd, RdrSettings const& rsettings, view3d::WindowOptions const& opts)
	{
		// Null hwnd is allowed when off-screen only rendering
		auto rect = RECT{};
		if (hwnd != 0)
			::GetClientRect(hwnd, &rect);

		auto settings = WndSettings(hwnd, true, rsettings).DefaultOutput().Size(rect.right - rect.left, rect.bottom - rect.top);
		settings.m_multisamp = MultiSamp(opts.m_multisampling);
		settings.m_name = opts.m_dbg_name;
		return settings;
	}

	// View3d Window ****************************
	V3dWindow::V3dWindow(HWND hwnd, Context& context, view3d::WindowOptions const& opts)
		:m_dll(&context)
		,m_hwnd(hwnd)
		,m_wnd(context.m_rdr, ToWndSettings(hwnd, context.m_rdr.Settings(), opts))
		,m_scene(m_wnd)
		,m_objects()
		,m_gizmos()
		,m_guids()
		,m_focus_point()
		,m_origin_point()
		,m_bbox_model()
		,m_selection_box()
		,m_visible_objects()
		,m_anim_data()
		,m_bbox_scene(BBox::Reset())
		,m_global_pso()
		,m_main_thread_id(std::this_thread::get_id())
		,m_invalidated(false)
		,ReportError()
		,OnSettingsChanged()
		,OnInvalidated()
		,OnRendering()
		,OnSceneChanged()
		,OnAnimationEvent()
	{
		try
		{
			// Notes:
			// - Don't observe the Context sources store for changes. The context handles this for us.
			ReportError += StaticCallBack(opts.m_error_cb, opts.m_error_cb_ctx);

			// Set the initial aspect ratio
			auto rt_area = m_wnd.BackBufferSize();
			if (rt_area != iv2Zero)
				m_scene.m_cam.Aspect(rt_area.x / float(rt_area.y));

			// The light for the scene
			m_scene.m_global_light.m_type           = ELight::Directional;
			m_scene.m_global_light.m_ambient        = Colour32(0xFF404040U);
			m_scene.m_global_light.m_diffuse        = Colour32(0xFF404040U);
			m_scene.m_global_light.m_specular       = Colour32(0xFF808080U);
			m_scene.m_global_light.m_specular_power = 1000.0f;
			m_scene.m_global_light.m_direction      = -v4ZAxis;
			m_scene.m_global_light.m_on             = true;
			m_scene.m_global_light.m_cam_relative   = true;

			// Create the stock models
			CreateStockObjects();
		}
		catch (...)
		{
			this->~V3dWindow();
			throw;
		}
	}
	V3dWindow::~V3dWindow()
	{
		#if 0 // todo
		AnimControl(EView3DAnimCommand::Stop);

		Close();
		m_scene.RemoveInstance(m_focus_point);
		m_scene.RemoveInstance(m_origin_point);
		m_scene.RemoveInstance(m_bbox_model);
		m_scene.RemoveInstance(m_selection_box);
		#endif
	}

	// Renderer access
	Renderer& V3dWindow::rdr() const
	{
		return m_dll->m_rdr;
	}
	ResourceManager& V3dWindow::res() const
	{
		return rdr().res();
	}

	// Get/Set the settings
	wchar_t const* V3dWindow::Settings() const
	{
		std::wstringstream out;
		out << "*Light {\n" << m_scene.m_global_light.Settings() << "}\n";
		m_settings = out.str();
		return m_settings.c_str();
	}
	void V3dWindow::Settings(wchar_t const* settings)
	{
		using namespace pr::script;
		using namespace pr::str;

		// Parse the settings
		StringSrc src(settings);
		Reader reader(src);
		for (string_t kw; reader.NextKeywordS(kw);)
		{
			if (EqualI(kw, "SceneSettings"))
			{
				pr::string<> desc;
				reader.Section(desc, false);
				//window->m_obj_cont_ui.Settings(desc.c_str());
				continue;
			}
			if (EqualI(kw, "Light"))
			{
				std::wstring desc;
				reader.Section(desc, false);
				m_scene.m_global_light.Settings(desc);
				OnSettingsChanged(this, view3d::ESettings::Lighting_All);
				continue;
			}
		}
	}

	// Get/Set the back buffer size
	iv2 V3dWindow::BackBufferSize() const
	{
		return m_wnd.BackBufferSize();
	}
	void V3dWindow::BackBufferSize(iv2 sz)
	{
		if (sz.x < 0) sz.x = 0;
		if (sz.y < 0) sz.y = 0;

		// Before resize, the old aspect is: Aspect0 = scale * Width0 / Height0
		// After resize, the new aspect is: Aspect1 = scale * Width1 / Height1

		// Save the current camera aspect ratio
		auto old_size = m_wnd.BackBufferSize();
		auto old_aspect = m_scene.m_cam.Aspect();
		auto scale = old_aspect * old_size.y / float(old_size.x);

		// Resize the render target
		m_wnd.BackBufferSize(sz, false);

		// Adjust the camera aspect ratio to preserve it
		auto new_size = m_wnd.BackBufferSize();
		auto new_aspect = (new_size.x == 0 || new_size.y == 0) ? 1.0f : new_size.x / float(new_size.y);
		auto aspect = scale * new_aspect;
		m_scene.m_cam.Aspect(aspect);
	}

	// Get/Set the scene viewport
	view3d::Viewport V3dWindow::Viewport() const
	{
		auto& vp = m_scene.m_viewport;
		return view3d::Viewport {
			.m_x         = vp.TopLeftX,
			.m_y         = vp.TopLeftY,
			.m_width     = vp.Width,
			.m_height    = vp.Height,
			.m_min_depth = vp.MinDepth,
			.m_max_depth = vp.MaxDepth,
			.m_screen_w  = vp.ScreenW,
			.m_screen_h  = vp.ScreenH,
		};
	}
	void V3dWindow::Viewport(view3d::Viewport const& vp)
	{
		m_scene.m_viewport.Set(vp.m_x, vp.m_y, vp.m_width, vp.m_height, vp.m_screen_w, vp.m_screen_h, vp.m_min_depth, vp.m_max_depth);
		OnSettingsChanged(this, view3d::ESettings::Scene_Viewport);
	}

	// Enumerate the object collection guids associated with this window
	void V3dWindow::EnumGuids(StaticCB<bool, Guid const&> enum_guids_cb)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);
		for (auto& guid : m_guids)
		{
			if (enum_guids_cb(guid)) continue;
			break;
		}
	}

	// Enumerate the objects associated with this window
	void V3dWindow::EnumObjects(StaticCB<bool, view3d::Object> enum_objects_cb)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);
		for (auto& object : m_objects)
		{
			if (enum_objects_cb(object)) continue;
			break;
		}
	}
	void V3dWindow::EnumObjects(StaticCB<bool, view3d::Object> enum_objects_cb, GUID const* context_ids, int include_count, int exclude_count)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);
		for (auto& object : m_objects)
		{
			if (!IncludeFilter(object->m_context_id, context_ids, include_count, exclude_count)) continue;
			if (enum_objects_cb(object)) continue;
			break;
		}
	}

	// Add/Remove an object to this window
	void V3dWindow::Add(LdrObject* object)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);
		auto iter = m_objects.find(object);
		if (iter == end(m_objects))
		{
			m_objects.insert(iter, object);
			m_guids.insert(object->m_context_id);
			ObjectContainerChanged(view3d::ESceneChanged::ObjectsAdded, &object->m_context_id, 1, object);
		}
	}
	void V3dWindow::Remove(LdrObject* object)
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
			ObjectContainerChanged(view3d::ESceneChanged::ObjectsRemoved, &object->m_context_id, 1, object);
	}

	// Add/Remove all objects to this window with the given context ids (or not with)
	void V3dWindow::Add(GUID const* context_ids, int include_count, int exclude_count)
	{
		assert(std::this_thread::get_id() == m_main_thread_id);

		pr::vector<Guid> new_guids;
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
				auto changed = view3d::ESettings::Camera;
				if (AllSet(src.m_cam_fields, ECamField::C2W))
				{
					m_scene.m_cam.CameraToWorld(cam.CameraToWorld());
					changed |= view3d::ESettings::Camera_Position;
				}
				if (AllSet(src.m_cam_fields, ECamField::Focus))
				{
					m_scene.m_cam.LookAt(cam.CameraToWorld().pos, cam.FocusPoint(), cam.CameraToWorld().y);
					changed |= view3d::ESettings::Camera_Position;
					changed |= view3d::ESettings::Camera_FocusDist;
				}
				if (AllSet(src.m_cam_fields, ECamField::Align))
				{
					m_scene.m_cam.Align(cam.m_align);
					changed |= view3d::ESettings::Camera_AlignAxis;
				}
				if (AllSet(src.m_cam_fields, ECamField::Aspect))
				{
					m_scene.m_cam.Aspect(cam.Aspect());
					changed |= view3d::ESettings::Camera_Aspect;
				}
				if (AllSet(src.m_cam_fields, ECamField::FovY))
				{
					m_scene.m_cam.FovY(cam.FovY());
					changed |= view3d::ESettings::Camera_Fov;
				}
				if (AllSet(src.m_cam_fields, ECamField::Near))
				{
					m_scene.m_cam.Near(cam.Near(true), true);
					changed |= view3d::ESettings::Camera_ClipPlanes;
				}
				if (AllSet(src.m_cam_fields, ECamField::Far))
				{
					m_scene.m_cam.Far(cam.Far(true), true);
					changed |= view3d::ESettings::Camera_ClipPlanes;
				}
				if (AllSet(src.m_cam_fields, ECamField::Ortho))
				{
					m_scene.m_cam.Orthographic(cam.Orthographic());
					changed |= view3d::ESettings::Camera_Orthographic;
				}

				// Notify if the camera was changed
				if (changed != view3d::ESettings::Camera)
					OnSettingsChanged(this, changed);
			}
		}
		if (m_objects.size() != old_count)
		{
			m_guids.insert(std::begin(new_guids), std::end(new_guids));
			ObjectContainerChanged(view3d::ESceneChanged::ObjectsAdded, new_guids.data(), int(new_guids.size()), nullptr);
		}
	}
	void V3dWindow::Remove(GUID const* context_ids, int include_count, int exclude_count, bool keep_context_ids)
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
				pr::vector<Guid> guids(std::begin(removed), std::end(removed));
				ObjectContainerChanged(view3d::ESceneChanged::ObjectsRemoved, guids.data(), int(guids.size()), nullptr);
			}
		}
	}

	// Remove all objects from this scene
	void V3dWindow::RemoveAllObjects()
	{
		assert(std::this_thread::get_id() == m_main_thread_id);

		// Make a copy of the guids
		pr::vector<GUID> context_ids(std::begin(m_guids), std::end(m_guids));

		// Remove the objects and guids
		m_objects.clear();
		m_guids.clear();

		// Notify that the scene has changed
		ObjectContainerChanged(view3d::ESceneChanged::ObjectsRemoved, context_ids.data(), int(context_ids.size()), nullptr);
	}

	// Render this window into whatever render target is currently set
	void V3dWindow::Render()
	{
		// Notes:
		// - Don't be tempted to call 'Validate()' at the start of Render so that objects
		//   added to the scene during the render re-invalidate. Instead defer the invalidate
		//   to the next windows event.

		assert(std::this_thread::get_id() == m_main_thread_id);

		// Reset the drawlist
		m_scene.ClearDrawlists();

		// Notify of a render about to happen
		OnRendering(this);

		//// Set the view and projection matrices. Do this before adding objects to the
		//// scene as they do last minute transform adjustments based on the camera position.
		//auto& cam = m_scene.m_cam;
		//m_scene.SetView(cam);
		//cam.m_moved = false;

		// Set the light source
		m_scene.ShadowCasting(m_scene.m_global_light.m_cast_shadow != 0, 1024);

		// Position and scale the focus point and origin point
		if (AnySet(m_visible_objects, EStockObject::FocusPoint | EStockObject::OriginPoint))
		{
			// Draw the point with perspective or orthographic projection based on the camera settings,
			// but with an aspect ratio matching the viewport regardless of the camera's aspect ratio.
			float const screen_fraction = 0.05f;
			auto aspect_v = float(m_scene.m_viewport.Width) / float(m_scene.m_viewport.Height);

			// Create a camera with the same aspect as the viewport
			auto& scene_cam = m_scene.m_cam;
			auto v_camera = scene_cam;
			auto fd = scene_cam.FocusDist();
			v_camera.Aspect(aspect_v);

			// Get the scaling factors from 'm_camera' to 'v_camera'
			auto viewarea_c = scene_cam.ViewArea(fd);
			auto viewarea_v = v_camera.ViewArea(fd);

			if (AllSet(m_visible_objects, EStockObject::FocusPoint))
			{
				// Scale the camera space X,Y coords
				// Note: this cannot be added as a matrix to 'i2w' or 'c2s' because we're
				// only scaling the instance position, not the whole instance geometry
				auto pt_cs = scene_cam.WorldToCamera() * scene_cam.FocusPoint();
				pt_cs.x *= viewarea_v.x / viewarea_c.x;
				pt_cs.y *= viewarea_v.y / viewarea_c.y;
				auto pt_ws = scene_cam.CameraToWorld() * pt_cs;

				auto sz = m_focus_point.m_size * screen_fraction * abs(pt_cs.z);
				m_focus_point.m_i2w = m4x4::Scale(sz, sz, sz, pt_ws);
				m_focus_point.m_c2s = v_camera.CameraToScreen();
				m_scene.AddInstance(m_focus_point);
			}
			if (AllSet(m_visible_objects, EStockObject::OriginPoint))
			{
				// Scale the camera space X,Y coords
				auto pt_cs = scene_cam.WorldToCamera() * v4Origin;
				pt_cs.x *= viewarea_v.x / viewarea_c.x;
				pt_cs.y *= viewarea_v.y / viewarea_c.y;
				auto pt_ws = scene_cam.CameraToWorld() * pt_cs;

				auto sz = m_origin_point.m_size * screen_fraction * abs(pt_cs.z);
				m_origin_point.m_i2w = m4x4::Scale(sz, sz, sz, pt_ws);
				m_origin_point.m_c2s = v_camera.CameraToScreen();
				m_scene.AddInstance(m_origin_point);
			}
		}

		// Selection box
		if (AnySet(m_visible_objects, EStockObject::SelectionBox))
		{
			// Transform is updated by the user or by a call to SetSelectionBoxToSelected()
			// 'm_selection_box.m_i2w.pos.w' is zero when there is no selection.
			// Update the selection box if necessary
			SelectionBoxFitToSelected();
			if (m_selection_box.m_i2w.pos.w != 0)
				m_scene.AddInstance(m_selection_box);
		}

		// Get the animation clock time
		auto anim_time = (float)m_anim_data.m_clock.load().count();
		assert(IsFinite(anim_time));

		// Add objects from the window to the scene
		for (auto& obj : m_objects)
		{
			#if 0 // todo, global pso?
			// Apply the fill mode and cull mode to user models
			obj->Apply([=](LdrObject* obj)
				{
					if (obj->m_model == nullptr || AllSet(obj->m_ldr_flags, ELdrFlags::SceneBoundsExclude)) return true;
					for (auto& nug : obj->m_model->m_nuggets)
					{
						nug.FillMode(m_fill_mode);
						nug.CullMode(m_cull_mode);
					}
					return true;
				}, "");
			#endif

			// Recursively add the object to the scene
			obj->AddToScene(m_scene, anim_time);

			// Only show bounding boxes for things that contribute to the scene bounds.
			if (m_wnd.m_diag.m_bboxes_visible && !AllSet(obj->m_ldr_flags, ELdrFlags::SceneBoundsExclude))
				obj->AddBBoxToScene(m_scene, anim_time);
		}

		// Add gizmos from the window to the scene
		for (auto& giz : m_gizmos)
		{
			giz->AddToScene(m_scene);
		}

		#if 0
		// Add the measure tool objects if the window is visible
		if (m_measure_tool_ui != nullptr && LdrMeasureUI().Visible() && LdrMeasureUI().Gfx())
			LdrMeasureUI().Gfx()->AddToScene(m_scene);

		// Add the angle tool objects if the window is visible
		if (m_angle_tool_ui != nullptr && LdrAngleUI().Visible() && LdrAngleUI().Gfx())
			LdrAngleUI().Gfx()->AddToScene(m_scene);
		#endif

		// Render the scene
		auto frame = m_wnd.RenderFrame();
		frame.Render(m_scene);
		frame.Present();
	}
	void V3dWindow::Present()
	{
		#if 0 // todo
		m_wnd.Present();
		#endif

		// No longer invalidated
		Validate();
	}

	// Call InvalidateRect on the HWND associated with this window
	void V3dWindow::InvalidateRect(RECT const* rect, bool erase)
	{
		if (m_hwnd != nullptr)
			::InvalidateRect(m_hwnd, rect, erase);
		
		if (!m_invalidated)
			OnInvalidated(this);

		// The window becomes validated again when 'Present()' or 'Validate()' is called.
		m_invalidated = true;
	}
	void V3dWindow::Invalidate(bool erase)
	{
		InvalidateRect(nullptr, erase);
	}

	// Clear the invalidated state for the window
	void V3dWindow::Validate()
	{
		m_invalidated = false;
	}

	// General mouse navigation
	// 'ss_pos' is the mouse pointer position in 'window's screen space
	// 'nav_op' is the navigation type (typically Rotate=LButton, Translate=RButton)
	// 'nav_start_or_end' should be TRUE on mouse down/up events, FALSE for mouse move events
	// void OnMouseDown(UINT nFlags, CPoint point) { View3D_MouseNavigate(win, point, nav_op, TRUE); }
	// void OnMouseMove(UINT nFlags, CPoint point) { View3D_MouseNavigate(win, point, nav_op, FALSE); } if 'nav_op' is None, this will have no effect
	// void OnMouseUp  (UINT nFlags, CPoint point) { View3D_MouseNavigate(win, point, 0, TRUE); }
	// BOOL OnMouseWheel(UINT nFlags, short zDelta, CPoint) { if (nFlags == 0) View3D_MouseNavigateZ(win, 0, 0, zDelta / 120.0f); return TRUE; }
	bool V3dWindow::MouseNavigate(v2 ss_point, camera::ENavOp nav_op, bool nav_start_or_end)
	{
		auto nss_point = m_scene.m_viewport.SSPointToNSSPoint(ss_point);
		
		// This is true-ish. 'ss_pos' is allowed to be outside the window area which breaks this check
		//if (nss_point.x < -1.0 || nss_point.x > +1.0 || nss_point.y < -1.0 || nss_point.y > +1.0)
		//	throw std::runtime_error("Window viewport has not been set correctly. The ScreenW/H values should match the window size (not the viewport size)");

		auto refresh = false;
		auto gizmo_in_use = false;

		// Check any gizmos in the scene for interaction with the mouse
		for (auto& giz : m_gizmos)
		{
			refresh |= giz->MouseControl(m_scene.m_cam, nss_point, nav_op, nav_start_or_end);
			gizmo_in_use |= giz->m_manipulating;
			if (gizmo_in_use)
				break;
		}

		// If no gizmos are using the mouse, use standard mouse control
		if (!gizmo_in_use)
		{
			if (m_scene.m_cam.MouseControl(nss_point, nav_op, nav_start_or_end))
				refresh |= true;
		}

		return refresh;
	}
	bool V3dWindow::MouseNavigateZ(v2 ss_point, float delta, bool along_ray)
	{
		auto nss_point = m_scene.m_viewport.SSPointToNSSPoint(ss_point);

		auto refresh = false;
		auto gizmo_in_use = false;

		// Check any gizmos in the scene for interaction with the mouse
		#if 0 // todo, gizmo mouse wheel behaviour
		for (auto& giz : m_gizmos)
		{
			refresh |= giz->MouseControlZ(m_scene.m_cam, nss_point, dist);
			gizmo_in_use |= giz->m_manipulating;
			if (gizmo_in_use)
				break;
		}
		#endif

		// If no gizmos are using the mouse, use standard mouse control
		if (!gizmo_in_use)
		{
			if (m_scene.m_cam.MouseControlZ(nss_point, delta, along_ray))
				refresh |= true;
		}

		return refresh;
	}

	// Get/Set the window background colour
	Colour32 V3dWindow::BackgroundColour() const
	{
		return m_scene.m_bkgd_colour;
	}
	void V3dWindow::BackgroundColour(Colour32 colour)
	{
		if (BackgroundColour() == colour)
			return;

		m_scene.m_bkgd_colour = colour;
		OnSettingsChanged(this, view3d::ESettings::Scene_BackgroundColour);
		Invalidate();
	}

	// Get/Set the global scene light
	Light V3dWindow::GlobalLight() const
	{
		return m_scene.m_global_light;
	}
	void V3dWindow::GlobalLight(Light const& light)
	{
		if (GlobalLight() == light)
			return;

		auto settings = view3d::ESettings::Lighting;
		if (m_scene.m_global_light.m_type != light.m_type) settings |= view3d::ESettings::Lighting_Type;
		if (m_scene.m_global_light.m_position != light.m_position) settings |= view3d::ESettings::Lighting_Position;
		if (m_scene.m_global_light.m_direction != light.m_direction) settings |= view3d::ESettings::Lighting_Direction;
		if (m_scene.m_global_light.m_ambient != light.m_ambient) settings |= view3d::ESettings::Lighting_Colour;
		if (m_scene.m_global_light.m_diffuse != light.m_diffuse) settings |= view3d::ESettings::Lighting_Colour;
		if (m_scene.m_global_light.m_specular != light.m_specular) settings |= view3d::ESettings::Lighting_Colour;
		if (m_scene.m_global_light.m_specular_power != light.m_specular_power) settings |= view3d::ESettings::Lighting_Range;
		if (m_scene.m_global_light.m_range != light.m_range) settings |= view3d::ESettings::Lighting_Range;
		if (m_scene.m_global_light.m_falloff != light.m_falloff) settings |= view3d::ESettings::Lighting_Range;
		if (m_scene.m_global_light.m_inner_angle != light.m_inner_angle) settings |= view3d::ESettings::Lighting_Range;
		if (m_scene.m_global_light.m_outer_angle != light.m_outer_angle) settings |= view3d::ESettings::Lighting_Range;
		if (m_scene.m_global_light.m_cast_shadow != light.m_cast_shadow) settings |= view3d::ESettings::Lighting_Shadows;
		if (m_scene.m_global_light.m_cam_relative != light.m_cam_relative) settings |= view3d::ESettings::Lighting_Position | view3d::ESettings::Lighting_Direction;
		if (m_scene.m_global_light.m_on != light.m_on) settings |= view3d::ESettings::Lighting_All;

		m_scene.m_global_light = light;
		OnSettingsChanged(this, settings);
		Invalidate();
	}

	// Get/Set the global environment map for this window
	view3d::CubeMap V3dWindow::EnvMap() const
	{
		return m_scene.m_global_envmap.get();
	}
	void V3dWindow::EnvMap(view3d::CubeMap env_map)
	{
		if (EnvMap() == env_map)
			return;

		m_scene.m_global_envmap = TextureCubePtr(env_map, true);
		OnSettingsChanged(this, view3d::ESettings::Scene_EnvMap);
		Invalidate();
	}

	// Called when objects are added/removed from this window
	void V3dWindow::ObjectContainerChanged(view3d::ESceneChanged change_type, GUID const* context_ids, int count, LdrObject* object)
	{
		// Reset the drawlists so that removed objects are no longer in the drawlist
		if (change_type == view3d::ESceneChanged::ObjectsRemoved)
			m_scene.ClearDrawlists();

		// Invalidate cached members
		m_bbox_scene = BBox::Reset();

		// Notify scene changed
		view3d::SceneChanged args = {change_type, context_ids, count, object};
		OnSceneChanged(this, args);
	}

	// Set the position and size of the selection box. If 'bbox' is 'BBox::Reset()' the selection box is not shown
	void V3dWindow::SetSelectionBox(BBox const& bbox, m3x4 const& ori)
	{
		if (bbox == BBox::Reset())
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
	void V3dWindow::SelectionBoxFitToSelected()
	{
		// Find the bounds of the selected objects
		auto bbox = BBox::Reset();
		for (auto& obj : m_objects)
		{
			obj->Apply([&](LdrObject const* c)
			{
				if (!AllSet(c->m_ldr_flags, ELdrFlags::Selected) || AllSet(c->m_ldr_flags, ELdrFlags::SceneBoundsExclude))
					return true;

				auto bb = c->BBoxWS(true);
				Grow(bbox, bb);
				return false;
			}, "");
		}
		SetSelectionBox(bbox);
	}

	// Create stock models such as the focus point, origin, etc
	void V3dWindow::CreateStockObjects()
	{
		// Create the focus point/origin models
		m_focus_point.m_model = res().CreateModel(EStockModel::Basis);
		m_focus_point.m_tint = Colour32One;
		m_focus_point.m_i2w = m4x4Identity;
		m_origin_point.m_model = res().CreateModel(EStockModel::Basis);
		m_origin_point.m_tint = Colour32Gray;
		m_origin_point.m_i2w = m4x4Identity;

		// Create the selection box model
		m_selection_box.m_model = res().CreateModel(EStockModel::SelectionBox);
		m_selection_box.m_tint = Colour32White;
		m_selection_box.m_i2w = m4x4Identity;
	}
}