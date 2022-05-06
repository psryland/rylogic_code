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
		,m_main_thread_id(std::this_thread::get_id())
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
	ResourceManager& V3dWindow::res_mgr() const
	{
		return rdr().res_mgr();
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

	// Create stock models such as the focus point, origin, etc
	void V3dWindow::CreateStockObjects()
	{
		// Create the focus point/origin models
		m_focus_point.m_model = res_mgr().FindModel(EStockModel::Basis);
		m_focus_point.m_tint = Colour32One;
		m_focus_point.m_i2w = m4x4Identity;
		m_origin_point.m_model = res_mgr().FindModel(EStockModel::Basis);
		m_origin_point.m_tint = Colour32Gray;
		m_origin_point.m_i2w = m4x4Identity;

		// Create the selection box model
		m_selection_box.m_model = res_mgr().FindModel(EStockModel::SelectionBox);
		m_selection_box.m_tint = Colour32White;
		m_selection_box.m_i2w = m4x4Identity;
	}
}