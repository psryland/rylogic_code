//***************************************************************************************************
// View 3D
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************

#include "stdafx.h"
#include "pr/view3d/view3d.h"
#include "pr/view3d/prmaths.h"
#include "view3d/renderer_instance.h"

using namespace view3d;
using namespace pr::rdr;
using namespace pr::log;

CAppModule g_module;

#pragma region("Dll entry point")
#ifdef _MANAGED
#pragma managed(push, off)
#endif
BOOL APIENTRY DllMain(HMODULE hInstance, DWORD ul_reason_for_call, LPVOID)
{
	switch (ul_reason_for_call)
	{
	case DLL_THREAD_ATTACH:  break;
	case DLL_THREAD_DETACH:  break;
	case DLL_PROCESS_ATTACH: g_module.Init(0, hInstance); break;
	case DLL_PROCESS_DETACH: g_module.Term(); break;
	}
	return TRUE;
}
#ifdef _MANAGED
#pragma managed(pop)
#endif
#pragma endregion

// Global data for this dll
struct DllData
	:pr::AlignTo<16>
	,pr::events::IRecv<pr::ldr::Evt_Refresh>
	,pr::events::IRecv<pr::ldr::Evt_LdrMeasureUpdate>
	,pr::events::IRecv<pr::ldr::Evt_LdrAngleDlgUpdate>
{
	View3D_RenderCB        m_render_cb;
	View3D_ReportErrorCB   m_error_cb;
	View3D_LogOutputCB     m_log_cb;
	View3D_SettingsChanged m_settings_cb;
	pr::Logger             m_log;
	std::string            m_settings;
	bool                   m_compatible;
	RendererInstance       m_rdr;
	std::recursive_mutex   m_mutex;
	std::thread::id        m_this_thread;

	DllData(HWND hwnd, View3D_RenderCB render_cb, View3D_ReportErrorCB error_cb, View3D_LogOutputCB log_cb, View3D_SettingsChanged settings_cb)
		:m_render_cb(render_cb)
		,m_error_cb(error_cb)
		,m_log_cb(log_cb)
		,m_settings_cb(settings_cb)
		,m_log("view3d", [this](pr::log::Event const& ev){ LogOutput(ev); })
		,m_settings()
		,m_compatible(TestSystemCompatibility())
		,m_rdr(hwnd)
		,m_mutex()
		,m_this_thread(std::this_thread::get_id())
	{
		AtlInitCommonControls(ICC_BAR_CLASSES); // add flags to support other controls
		m_rdr.CreateStockObjects();
	}

	// Forward log data to the callback
	void LogOutput(pr::log::Event const& ev)
	{
		if (!m_log_cb) return;
		m_log_cb(static_cast<EView3DLogLevel>(ev.m_level), ev.m_timestamp.count(), ev.m_msg.c_str());
	}

	// Report an error via the callback
	void ReportError(pr::string<> msg)
	{
		m_log.Write(ELevel::Error, msg);
		if (!m_error_cb) return;
		if (msg.last() != '\n') msg.push_back('\n');
		m_error_cb(msg.c_str());
	}
	void ReportError(pr::string<> msg, std::exception const& ex)
	{
		m_log.Write(ELevel::Error, ex, msg);
		if (!m_error_cb) return;
		if (msg.last() != '\n') msg.push_back('\n');
		m_error_cb(pr::FmtS("%sReason: %s\n", msg.c_str(), ex.what()));
	}

	// Invoke the settings changed callback
	void NotifySettingsChanged()
	{
		if (m_settings_cb) return;
		m_settings_cb();
	}

private:
	DllData(DllData const&);
	DllData& operator=(DllData const&);

	// Event handlers
	void OnEvent(pr::ldr::Evt_Refresh const&) override { m_render_cb(); }
	void OnEvent(pr::ldr::Evt_LdrMeasureUpdate const&) override { m_render_cb(); }
	void OnEvent(pr::ldr::Evt_LdrAngleDlgUpdate const&) override { m_render_cb(); }
};

typedef std::lock_guard<std::recursive_mutex> LockGuard;

// Singleton accessors
static std::shared_ptr<DllData> g_dll = nullptr;
inline DllData&          Dll() { return *g_dll; }
inline RendererInstance& Rdr() { return g_dll->m_rdr; }

#define LOCK_GUARD LockGuard lock(Dll().m_mutex)

// Initialise the dll
VIEW3D_API EView3DResult __stdcall View3D_Initialise(HWND hwnd, View3D_RenderCB render_cb, View3D_ReportErrorCB error_cb, View3D_LogOutputCB log_cb, View3D_SettingsChanged settings_cb)
{
	try
	{
		// Already initialised?
		if (g_dll != nullptr)
			return EView3DResult::Success;

		// Allocate the dll data
		g_dll.reset(new DllData(hwnd, render_cb, error_cb, log_cb, settings_cb)); // Don't use std::make_shared, because it doesn't call operator new()
		PR_ASSERT(PR_DBG, pr::meta::is_aligned_to<16>(g_dll.get()), "dll data not aligned");

		return EView3DResult::Success;
	}
	catch (std::exception const& e)
	{
		error_cb(pr::FmtS("Failed to initialise View3D.\nReason: %s\n", e.what()));
		return EView3DResult::Failed;
	}
	catch (...)
	{
		error_cb("Failed to initialise View3D.\nReason: An unknown exception occurred\n");
		return EView3DResult::Failed;
	}
}
VIEW3D_API void __stdcall View3D_Shutdown()
{
	PR_ASSERT(PR_DBG, std::this_thread::get_id() == Dll().m_this_thread, "cross thread called to view3d");
	g_dll = nullptr;
}

// Generate a settings string for the view
VIEW3D_API char const* __stdcall View3D_GetSettings(View3DDrawset drawset)
{
	LOCK_GUARD;
	try
	{
		std::stringstream out;
		out << "*SceneSettings {" << Rdr().m_obj_cont_ui.Settings() << "}\n";
		out << "*Light {\n" << drawset->m_light.Settings() << "}\n";
		Dll().m_settings = out.str();
		return Dll().m_settings.c_str();
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_GetSettings failed", ex);
		return "";
	}
}

// Parse a settings string and apply to the view
VIEW3D_API void __stdcall View3D_SetSettings(View3DDrawset drawset, char const* settings)
{
	LOCK_GUARD;
	try
	{
		// Parse the settings
		pr::script::Reader reader;
		pr::script::PtrSrc src(settings);
		reader.AddSource(src);

		for (pr::script::string kw; reader.NextKeywordS(kw);)
		{
			if (pr::str::EqualI(kw, "SceneSettings"))
			{
				pr::string<> desc;
				reader.ExtractSection(desc, false);
				Rdr().m_obj_cont_ui.Settings(desc.c_str());
				continue;
			}
			if (pr::str::EqualI(kw, "Light"))
			{
				pr::string<> desc;
				reader.ExtractSection(desc, false);
				drawset->m_light.Settings(desc.c_str());
				continue;
			}
		}
		View3D_GetSettings(drawset);
		Dll().NotifySettingsChanged();
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_SetSettings failed", ex);
	}
}

// Render a drawset
// Remember to call View3D_Present() after all View3D_Render calls
VIEW3D_API void __stdcall View3D_DrawsetRender(View3DDrawset drawset)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		Rdr().m_last_drawset = drawset;

		auto& scene = Rdr().m_scene;

		// Reset the drawlist
		scene.ClearDrawlists();

		// Add objects from the drawset to the viewport
		for (auto& obj : drawset->m_objects)
			obj->AddToScene(scene);

		// Add the measure tool objects if the window is visible
		if (Rdr().m_measure_tool_ui.IsWindowVisible() && Rdr().m_measure_tool_ui.Gfx())
			Rdr().m_measure_tool_ui.Gfx()->AddToScene(scene);;

		// Add the angle tool objects if the window is visible
		if (Rdr().m_angle_tool_ui.IsWindowVisible() && Rdr().m_angle_tool_ui.Gfx())
			Rdr().m_angle_tool_ui.Gfx()->AddToScene(scene);;

		// Position the focus point
		if (drawset->m_focus_point_visible)
		{
			float scale = drawset->m_focus_point_size * drawset->m_camera.FocusDist();
			Rdr().m_focus_point.m_i2w = pr::Scale4x4(scale, drawset->m_camera.FocusPoint());
			scene.AddInstance(Rdr().m_focus_point);
		}
		// Scale the origin point
		if (drawset->m_origin_point_visible)
		{
			float scale = drawset->m_origin_point_size * pr::Length3(drawset->m_camera.CameraToWorld().pos);
			Rdr().m_origin_point.m_i2w = pr::Scale4x4(scale, pr::v4Origin);
			scene.AddInstance(Rdr().m_origin_point);
		}

		{// Set the view and projection matrices
			pr::Camera& cam = drawset->m_camera;
			scene.SetView(cam);
		}

		// Set the light source
		Light& light = scene.m_global_light;
		light = drawset->m_light;
		if (drawset->m_light_is_camera_relative)
		{
			light.m_direction = drawset->m_camera.CameraToWorld() * drawset->m_light.m_direction;
			light.m_position  = drawset->m_camera.CameraToWorld() * drawset->m_light.m_position;
		}

		// Set the background colour
		scene.m_bkgd_colour = drawset->m_background_colour;

		// Set the global fill mode
		switch (drawset->m_fill_mode) {
		case EView3DFillMode::Solid:     scene.m_rsb.Set(ERS::FillMode, D3D11_FILL_SOLID); break;
		case EView3DFillMode::Wireframe: scene.m_rsb.Set(ERS::FillMode, D3D11_FILL_WIREFRAME); break;
		case EView3DFillMode::SolidWire: scene.m_rsb.Set(ERS::FillMode, D3D11_FILL_SOLID); break;
		}

		// Render the scene
		scene.Render();

		// Render wire frame over solid for 'SolidWire' mode
		if (drawset->m_fill_mode == EView3DFillMode::SolidWire)
		{
			auto& fr = scene.RStep<ForwardRender>();
			scene.m_rsb.Set(ERS::FillMode, D3D11_FILL_WIREFRAME);
			scene.m_bsb.Set(EBS::BlendEnable, FALSE, 0);
			fr.m_clear_bb = false;

			scene.Render();

			fr.m_clear_bb = true;
			scene.m_rsb.Clear(ERS::FillMode);
			scene.m_bsb.Clear(EBS::BlendEnable, 0);
		}
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_Render failed", ex);
	}
}

// Create/Delete a draw set
VIEW3D_API EView3DResult __stdcall View3D_DrawsetCreate(View3DDrawset& drawset)
{
	LOCK_GUARD;
	try
	{
		drawset = new Drawset();
		Rdr().m_drawset.insert(drawset);

		// Set the initial aspect ratio
		pr::iv2 client_area = Rdr().m_renderer.RenderTargetSize();
		float aspect = client_area.x / float(client_area.y);
		drawset->m_camera.Aspect(aspect);

		return EView3DResult::Success;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_DrawsetCreate failed", ex);
		return EView3DResult::Failed;
	}
}
VIEW3D_API void __stdcall View3D_DrawsetDelete(View3DDrawset drawset)
{
	LOCK_GUARD;
	try
	{
		View3D_DrawsetRemoveAllObjects(drawset);
		Rdr().m_drawset.erase(drawset);
		delete drawset;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_DrawsetDelete failed", ex);
	}
}

// Add/Remove objects by context id
VIEW3D_API void __stdcall View3D_DrawsetAddObjectsById(View3DDrawset drawset, int context_id)
{
	LOCK_GUARD;
	try
	{
		for (std::size_t i = 0, iend = Rdr().m_obj_cont.size(); i != iend; ++i)
			if (Rdr().m_obj_cont[i]->m_context_id == context_id)
				View3D_DrawsetAddObject(drawset, Rdr().m_obj_cont[i].m_ptr);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_DrawsetAddObejctsById failed", ex);
	}
}
VIEW3D_API void __stdcall View3D_DrawsetRemoveObjectsById(View3DDrawset drawset, int context_id)
{
	LOCK_GUARD;
	try
	{
		pr::ldr::LdrObject::MatchId in_this_context(context_id);
		for (ObjectCont::iterator i = drawset->m_objects.begin(), iend = drawset->m_objects.end(); i != iend; ++i)
			if ((*i)->m_context_id == context_id) drawset->m_objects.erase(i);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_DrawsetRemoveObjectsById failed", ex);
	}
}

// Add/Remove an object to/from a drawset
VIEW3D_API void __stdcall View3D_DrawsetAddObject(View3DDrawset drawset, View3DObject object)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0 && object != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		if (!object) throw std::exception("object is null");
		ObjectCont::const_iterator iter = drawset->m_objects.find(object);
		if (iter == drawset->m_objects.end()) drawset->m_objects.insert(iter, object);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_DrawsetAddObject failed", ex);
	}
}
VIEW3D_API void __stdcall View3D_DrawsetRemoveObject(View3DDrawset drawset, View3DObject object)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		if (!object) return;
		drawset->m_objects.erase(object);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_DrawsetRemoveObject failed", ex);
	}
}

// Remove all objects from the drawset
VIEW3D_API void __stdcall View3D_DrawsetRemoveAllObjects(View3DDrawset drawset)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		drawset->m_objects.clear();
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_DrawsetRemoveAllObjects failed", ex);
	}
}

// Return the number of objects assigned to this drawset
VIEW3D_API int __stdcall View3D_DrawsetObjectCount(View3DDrawset drawset)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		return int(drawset->m_objects.size());
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_DrawsetObjectCount failed", ex);
		return 0;
	}
}

// Return true if 'object' is included in 'drawset'
VIEW3D_API BOOL __stdcall View3D_DrawsetHasObject(View3DDrawset drawset, View3DObject object)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		return drawset->m_objects.find(object) != std::end(drawset->m_objects);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_DrawsetHasObject failed", ex);
		return false;
	}
}

// Camera ********************************************************

// Return the camera to world transform
VIEW3D_API void __stdcall View3D_CameraToWorld(View3DDrawset drawset, View3DM4x4& c2w)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		c2w = view3d::To<View3DM4x4>(drawset->m_camera.m_c2w);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_CameraToWorld failed", ex);
	}
}

// Set the camera to world transform
VIEW3D_API void __stdcall View3D_SetCameraToWorld(View3DDrawset drawset, View3DM4x4 const& c2w)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		drawset->m_camera.m_c2w = view3d::To<pr::m4x4>(c2w);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_SetCameraToWorld failed", ex);
	}
}

// Position the camera for a drawset
VIEW3D_API void __stdcall View3D_PositionCamera(View3DDrawset drawset, View3DV4 position, View3DV4 lookat, View3DV4 up)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		drawset->m_camera.LookAt(view3d::To<pr::v4>(position), view3d::To<pr::v4>(lookat), view3d::To<pr::v4>(up), true);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_PositionCamera failed", ex);
	}
}

// Return the distance to the camera focus point
VIEW3D_API float __stdcall View3D_CameraFocusDistance(View3DDrawset drawset)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		return drawset->m_camera.FocusDist();
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_FocusDistance failed", ex);
		return 0.0f;
	}
}

// Set the camera focus distance
VIEW3D_API void __stdcall View3D_CameraSetFocusDistance(View3DDrawset drawset, float dist)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		drawset->m_camera.FocusDist(dist);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_SetFocusDistance failed", ex);
	}
}

// Return the aspect ratio for the camera field of view
VIEW3D_API float __stdcall View3D_CameraAspect(View3DDrawset drawset)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		return drawset->m_camera.Aspect();
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_CameraAspect failed", ex);
		return 1.0f;
	}
}

// Set the aspect ratio for the camera field of view
VIEW3D_API void __stdcall View3D_CameraSetAspect(View3DDrawset drawset, float aspect)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		drawset->m_camera.Aspect(aspect);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_SetCameraAspect failed", ex);
	}
}

// Return the horizontal field of view (in radians).
VIEW3D_API float __stdcall View3D_CameraFovX(View3DDrawset drawset)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		return drawset->m_camera.FovX();
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_CameraFovX failed", ex);
		return 0.0f;
	}
}

// Set the horizontal field of view (in radians). Note aspect ratio is preserved, setting FovX changes FovY and visa versa
VIEW3D_API void __stdcall View3D_CameraSetFovX(View3DDrawset drawset, float fovX)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		drawset->m_camera.FovX(fovX);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_SetCameraFovX failed", ex);
	}
}

// Return the vertical field of view (in radians).
VIEW3D_API float __stdcall View3D_CameraFovY(View3DDrawset drawset)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		return drawset->m_camera.FovY();
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_CameraFovY failed", ex);
		return 0.0f;
	}
}

// Set the vertical field of view (in radians). Note aspect ratio is preserved, setting FovY changes FovX and visa versa
VIEW3D_API void __stdcall View3D_CameraSetFovY(View3DDrawset drawset, float fovY)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		drawset->m_camera.FovY(fovY);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_SetCameraFovY failed", ex);
	}
}

// Set the near and far clip planes for the camera
VIEW3D_API void __stdcall View3D_CameraSetClipPlanes(View3DDrawset drawset, float near_, float far_, BOOL focus_relative)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		drawset->m_camera.ClipPlanes(near_, far_, focus_relative != 0);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_CameraSetClipPlanes failed", ex);
	}
}

// General mouse navigation
// void OnMouseDown(UINT nFlags, CPoint point) { View3D_Navigate(m_drawset, pr::NormalisePoint(m_hWnd, point, -1.0f), nFlags, TRUE); }
// void OnMouseMove(UINT nFlags, CPoint point) { View3D_Navigate(m_drawset, pr::NormalisePoint(m_hWnd, point, -1.0f), nFlags, FALSE); } if 'nFlags' is zero, this will have no effect
// void OnMouseUp  (UINT nFlags, CPoint point) { View3D_Navigate(m_drawset, pr::NormalisePoint(m_hWnd, point, -1.0f), 0, TRUE); }
// BOOL OnMouseWheel(UINT nFlags, short zDelta, CPoint) { if (nFlags == 0) View3D_Navigate(m_drawset, 0, 0, zDelta / 120.0f); return TRUE; }
VIEW3D_API void __stdcall View3D_MouseNavigate(View3DDrawset drawset, View3DV2 point, int button_state, BOOL nav_start_or_end)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		drawset->m_camera.MouseControl(view3d::To<pr::v2>(point), button_state, nav_start_or_end != 0);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_Navigate failed", ex);
	}
}

// Direct movement of the camera
VIEW3D_API void __stdcall View3D_Navigate(View3DDrawset drawset, float dx, float dy, float dz)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		drawset->m_camera.Translate(dx, dy, dz);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_NavigateXY failed", ex);
	}
}

// Reset to the default zoom
VIEW3D_API void __stdcall View3D_ResetZoom(View3DDrawset drawset)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		drawset->m_camera.ResetZoom();
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_ResetZoom failed", ex);
	}
}

// Return the camera align axis
VIEW3D_API void __stdcall View3D_CameraAlignAxis(View3DDrawset drawset, View3DV4& axis)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		axis = view3d::To<View3DV4>(drawset->m_camera.m_align);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_CameraAlignAxis failed", ex);
	}
}

// Align the camera to an axis
VIEW3D_API void __stdcall View3D_AlignCamera(View3DDrawset drawset, View3DV4 axis)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		drawset->m_camera.SetAlign(view3d::To<pr::v4>(axis));
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_AlignCamera failed", ex);
	}
}

// Move the camera to a position that can see the whole scene
VIEW3D_API void __stdcall View3D_ResetView(View3DDrawset drawset, View3DV4 forward, View3DV4 up)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");

		// The bounding box for the scene
		auto bbox = pr::BBoxReset;
		for (auto obj : drawset->m_objects)
			pr::Encompass(bbox, obj->BBoxWS(true));
		if (bbox == pr::BBoxReset) bbox = pr::BBoxUnit;
		drawset->m_camera.View(bbox, view3d::To<pr::v4>(forward), view3d::To<pr::v4>(up), true);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_ResetView failed", ex);
	}
}

// Return the size of the perpendicular area visible to the camera at 'dist' (in world space)
VIEW3D_API View3DV2 __stdcall View3D_ViewArea(View3DDrawset drawset, float dist)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		return view3d::To<View3DV2>(drawset->m_camera.ViewArea(dist));
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_ViewArea failed", ex);
		return view3d::To<View3DV2>(pr::v2Zero);
	}
}

// Get/Set the camera focus point position
VIEW3D_API void __stdcall View3D_GetFocusPoint(View3DDrawset drawset, View3DV4& position)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		position = view3d::To<View3DV4>(drawset->m_camera.FocusPoint());
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_GetFocusPoint failed", ex);
	}
}
VIEW3D_API void __stdcall View3D_SetFocusPoint(View3DDrawset drawset, View3DV4 position)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		drawset->m_camera.FocusPoint(view3d::To<pr::v4>(position));
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_SetFocusPoint failed", ex);
	}
}

// Return a point in world space corresponding to a normalised screen space point.
// The x,y components of 'screen' should be in normalised screen space, i.e. (-1,-1)->(1,1)
// The z component should be the world space distance from the camera
VIEW3D_API View3DV4 __stdcall View3D_WSPointFromNormSSPoint(View3DDrawset drawset, View3DV4 screen)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		return view3d::To<View3DV4>(drawset->m_camera.WSPointFromNormSSPoint(view3d::To<pr::v4>(screen)));
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_WSPointFromNormSSPoint failed", ex);
		return view3d::To<View3DV4>(pr::v4Zero);
	}
}

// Return a point in normalised screen space corresponding to a world space point.
// The returned z component will be the world space distance from the camera.
VIEW3D_API View3DV4 __stdcall View3D_NormSSPointFromWSPoint(View3DDrawset drawset, View3DV4 world)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		return view3d::To<View3DV4>(drawset->m_camera.NormSSPointFromWSPoint(view3d::To<pr::v4>(world)));
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_NormSSPointFromWSPoint failed", ex);
		return view3d::To<View3DV4>(pr::v4Zero);
	}
}

// Return a point and direction in world space corresponding to a normalised sceen space point.
// The x,y components of 'screen' should be in normalised screen space, i.e. (-1,-1)->(1,1)
// The z component should be the world space distance from the camera
VIEW3D_API void __stdcall View3D_WSRayFromNormSSPoint(View3DDrawset drawset, View3DV4 screen, View3DV4& ws_point, View3DV4& ws_direction)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		pr::v4 pt,dir;
		drawset->m_camera.WSRayFromNormSSPoint(view3d::To<pr::v4>(screen), pt, dir);
		ws_point = view3d::To<View3DV4>(pt);
		ws_direction = view3d::To<View3DV4>(dir);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_WSRayFromNormSSPoint failed", ex);
	}
}

// Lighting ********************************************************

// Return the configuration of the single light source
VIEW3D_API View3DLight __stdcall View3D_LightProperties(View3DDrawset drawset)
{
	LOCK_GUARD;
	try
	{
		if (!drawset) throw std::exception("drawset is null");
		View3DLight light = {};
		light.m_position        =  view3d::To<View3DV4>(drawset->m_light.m_position);
		light.m_direction       =  view3d::To<View3DV4>(drawset->m_light.m_direction);
		light.m_type            =  static_cast<EView3DLight>(drawset->m_light.m_type.value);
		light.m_ambient         =  drawset->m_light.m_ambient;
		light.m_diffuse         =  drawset->m_light.m_diffuse;
		light.m_specular        =  drawset->m_light.m_specular;
		light.m_specular_power  =  drawset->m_light.m_specular_power;
		light.m_inner_cos_angle =  drawset->m_light.m_inner_cos_angle;
		light.m_outer_cos_angle =  drawset->m_light.m_outer_cos_angle;
		light.m_range           =  drawset->m_light.m_range;
		light.m_falloff         =  drawset->m_light.m_falloff;
		light.m_cast_shadow     =  drawset->m_light.m_cast_shadow;
		light.m_on              =  drawset->m_light.m_on;
		return light;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_LightProperties failed", ex);
		return View3DLight();
	}
}

// Configure the single light source
VIEW3D_API void __stdcall View3D_SetLightProperties(View3DDrawset drawset, View3DLight const& light)
{
	LOCK_GUARD;
	try
	{
		if (!drawset) throw std::exception("drawset is null");
		drawset->m_light.m_position        = view3d::To<pr::v4>(light.m_position);
		drawset->m_light.m_direction       = view3d::To<pr::v4>(light.m_direction);
		drawset->m_light.m_type            = pr::rdr::ELight::From(light.m_type);
		drawset->m_light.m_ambient         = light.m_ambient;
		drawset->m_light.m_diffuse         = light.m_diffuse;
		drawset->m_light.m_specular        = light.m_specular;
		drawset->m_light.m_specular_power  = light.m_specular_power;
		drawset->m_light.m_inner_cos_angle = light.m_inner_cos_angle;
		drawset->m_light.m_outer_cos_angle = light.m_outer_cos_angle;
		drawset->m_light.m_range           = light.m_range;
		drawset->m_light.m_falloff         = light.m_falloff;
		drawset->m_light.m_cast_shadow     = light.m_cast_shadow;
		drawset->m_light.m_on              = light.m_on != 0;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_SetLightProperties failed", ex);
	}
}

// Set up a single light source for a drawset
VIEW3D_API void __stdcall View3D_LightSource(View3DDrawset drawset, View3DV4 position, View3DV4 direction, BOOL camera_relative)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		drawset->m_light.m_position = view3d::To<pr::v4>(position);
		drawset->m_light.m_direction = view3d::To<pr::v4>(direction);
		drawset->m_light_is_camera_relative = camera_relative != 0;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_LightSource failed", ex);
	}
}

// Show the lighting UI
struct PreviewLighting
{
	View3DDrawset m_drawset;
	PreviewLighting(View3DDrawset drawset) :m_drawset(drawset) {}
	void operator()(Light const& light, bool camera_relative)
	{
		Light prev_light                      = m_drawset->m_light;
		bool prev_camera_relative             = m_drawset->m_light_is_camera_relative;
		m_drawset->m_light                    = light;
		m_drawset->m_light_is_camera_relative = camera_relative;

		View3D_DrawsetRender(m_drawset);
		View3D_Present();

		m_drawset->m_light                    = prev_light;
		m_drawset->m_light_is_camera_relative = prev_camera_relative;
	}
};
VIEW3D_API void __stdcall View3D_ShowLightingDlg(View3DDrawset drawset, HWND parent)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		PreviewLighting pv(drawset);
		LightingDlg<PreviewLighting> dlg(pv);
		dlg.m_light           = drawset->m_light;
		dlg.m_camera_relative = drawset->m_light_is_camera_relative;
		if (dlg.DoModal(parent) != IDOK) return;
		drawset->m_light                    = dlg.m_light;
		drawset->m_light_is_camera_relative = dlg.m_camera_relative;
		
		View3D_DrawsetRender(drawset);
		View3D_Present();

		Dll().NotifySettingsChanged();
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_ShowLightingDlg failed", ex);
	}
}

// Create/Delete objects ********************************************************
// Create objects given in a file.
// These objects will not have handles but can be deleted by their context id
VIEW3D_API EView3DResult __stdcall View3D_ObjectsCreateFromFile(char const* ldr_filepath, int context_id, BOOL async)
{
	LOCK_GUARD;
	try
	{
		pr::ldr::AddFile(Rdr().m_renderer, ldr_filepath, Rdr().m_obj_cont, context_id, async != 0, 0, &Rdr().m_lua);
		return EView3DResult::Success;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_ObjectsCreateFromFile failed", ex);
		return EView3DResult::Failed;
	}
}

// If multiple objects are created, the handle returned is to the last object only
VIEW3D_API EView3DResult __stdcall View3D_ObjectCreateLdr(char const* ldr_script, int context_id, View3DObject& object, BOOL async)
{
	LOCK_GUARD;
	try
	{
		object = 0;
		size_t initial = Rdr().m_obj_cont.size();
		pr::ldr::AddString(Rdr().m_renderer, ldr_script, Rdr().m_obj_cont, context_id, async != 0, 0, &Rdr().m_lua);
		size_t final = Rdr().m_obj_cont.size();
		if (initial == final) return EView3DResult::Failed;
		object = Rdr().m_obj_cont.back().m_ptr;
		return EView3DResult::Success;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_ObjectCreateLdr failed", ex);
		return EView3DResult::Failed;
	}
}

// Modify the geometry of an existing object
struct ObjectEditCBData
{
	View3D_EditObjectCB edit_cb;
	void* ctx;
};
void __stdcall ObjectEditCB(ModelPtr model, void* ctx, pr::Renderer&)
{
	PR_ASSERT(PR_DBG, model != 0, "");
	if (!model) throw std::exception("model is null");
	ObjectEditCBData& cbdata = *static_cast<ObjectEditCBData*>(ctx);

	// Create buffers to be filled by the user callback
	auto vrange = model->m_vrange;
	auto irange = model->m_irange;
	pr::Array<View3DVertex> verts   (vrange.size());
	pr::Array<pr::uint16>   indices (irange.size());

	// Get default values for the topo, geom, and material
	auto model_type = EView3DPrim::Invalid;
	auto geom_type  = EView3DGeom::Vert;
	View3DMaterial v3dmat = {0,0};

	// If the model already has nuggets grab some defaults from it
	if (!model->m_nuggets.empty())
	{
		auto nug = model->m_nuggets.front();
		model_type = static_cast<EView3DPrim>(nug.m_topo.value);
		geom_type  = static_cast<EView3DGeom>(nug.m_geom.value);
		v3dmat.m_diff_tex = nug.m_tex_diffuse.m_ptr;
		v3dmat.m_env_map  = nullptr;
	}

	// Get the user to generate the model
	UINT32 new_vcount, new_icount;
	cbdata.edit_cb(UINT32(vrange.size()), UINT32(irange.size()), &verts[0], &indices[0], new_vcount, new_icount, model_type, geom_type, v3dmat, cbdata.ctx);
	PR_ASSERT(PR_DBG, new_vcount <= vrange.size(), "");
	PR_ASSERT(PR_DBG, new_icount <= irange.size(), "");
	PR_ASSERT(PR_DBG, model_type != EView3DPrim::Invalid, "");
	PR_ASSERT(PR_DBG, geom_type != EView3DGeom::Unknown, "");

	// Update the material
	NuggetProps mat;
	mat.m_topo = static_cast<EPrim::Enum_>(model_type);
	mat.m_geom = static_cast<EGeom::Enum_>(geom_type);
	mat.m_tex_diffuse = v3dmat.m_diff_tex;

	{// Lock and update the model
		MLock mlock(model, D3D11_MAP_WRITE_DISCARD);
		model->m_bbox.reset();

		// Copy the model data into the model
		auto vin = begin(verts);
		auto vout = mlock.m_vlock.ptr<Vert>();
		for (size_t i = 0; i != new_vcount; ++i, ++vin)
		{
			SetPCNT(*vout++, view3d::To<pr::v4>(vin->pos), pr::Colour32::make(vin->col), view3d::To<pr::v4>(vin->norm), view3d::To<pr::v2>(vin->tex));
			pr::Encompass(model->m_bbox, view3d::To<pr::v4>(vin->pos));
		}
		auto iin = begin(indices);
		auto iout = mlock.m_ilock.ptr<pr::uint16>();
		for (size_t i = 0; i != new_icount; ++i, ++iin)
		{
			*iout++ = *iin;
		}
	}

	// Re-create the render nuggets
	vrange.resize(new_vcount);
	irange.resize(new_icount);
	model->DeleteNuggets();
	model->CreateNugget(mat, &vrange, &irange);
}

// Create an object via callback
VIEW3D_API EView3DResult __stdcall View3D_ObjectCreate(char const* name, View3DColour colour, int icount, int vcount, View3D_EditObjectCB edit_cb, void* ctx, int context_id, View3DObject& object)
{
	LOCK_GUARD;
	try
	{
		object = 0;
		ObjectEditCBData cbdata = {edit_cb, ctx};
		pr::ldr::LdrObjectPtr obj = pr::ldr::Add(Rdr().m_renderer, pr::ldr::ObjectAttributes(pr::ldr::ELdrObject::Custom, name, pr::Colour32::make(colour)), icount, vcount, ObjectEditCB, &cbdata, context_id);
		if (!obj) return EView3DResult::Failed;
		Rdr().m_obj_cont.push_back(obj);
		object = obj.m_ptr;
		return EView3DResult::Success;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_ObjectCreate failed", ex);
		return EView3DResult::Failed;
	}
}

// Replace the model and all child objects of 'obj' with the results of 'ldr_script'
VIEW3D_API EView3DResult __stdcall View3D_ObjectUpdate(View3DObject object, char const* ldr_script, EView3DUpdateObject flags)
{
	LOCK_GUARD;
	try
	{
		if (!object) throw std::exception("object is null");
		
		pr::ldr::Update(Rdr().m_renderer, object, ldr_script, static_cast<pr::ldr::EUpdateObject::Enum_>(flags));
		return EView3DResult::Success;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_ObjectCreateLdr failed", ex);
		return EView3DResult::Failed;
	}
}

// Edit an existing model
VIEW3D_API void __stdcall View3D_ObjectEdit(View3DObject object, View3D_EditObjectCB edit_cb, void* ctx)
{
	LOCK_GUARD;
	try
	{
		ObjectEditCBData cbdata = {edit_cb, ctx};
		pr::ldr::Edit(Rdr().m_renderer, object, ObjectEditCB, &cbdata);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_ObjectEdit failed", ex);
	}
}

// Delete all objects matching a context id
VIEW3D_API void __stdcall View3D_ObjectsDeleteById(int context_id)
{
	LOCK_GUARD;
	try
	{
		for (DrawsetCont::iterator i = Rdr().m_drawset.begin(), iend = Rdr().m_drawset.end(); i != iend; ++i)
			View3D_DrawsetRemoveObjectsById(*i, context_id);
		pr::ldr::Remove(Rdr().m_obj_cont, &context_id, 1, 0, 0);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_ObjectsDeleteById failed", ex);
	}
}

// Delete an object
VIEW3D_API void __stdcall View3D_ObjectDelete(View3DObject object)
{
	LOCK_GUARD;
	try
	{
		if (!object) return;
		
		// Remove the object from any drawsets it's in
		for (auto ds : Rdr().m_drawset)
			View3D_DrawsetRemoveObject(ds, object);
		
		// Delete the object from the object container
		pr::ldr::Remove(Rdr().m_obj_cont, object);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_ObjectDelete failed", ex);
	}
}

// Get/Set the object to parent transform for an object
// This is the object to world transform for objects without parents
// Note: In "*Box b { 1 1 1 *o2w{*pos{1 2 3}} }" setting this transform overwrites the "*o2w{*pos{1 2 3}}"
VIEW3D_API View3DM4x4 __stdcall View3D_ObjectGetO2P(View3DObject object)
{
	LOCK_GUARD;
	try
	{
		if (!object) throw std::exception("object is null");
		return view3d::To<View3DM4x4>(object->m_o2p);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_ObjectGetO2P failed", ex);
		return view3d::To<View3DM4x4>(pr::m4x4Identity);
	}
}
VIEW3D_API void __stdcall View3D_ObjectSetO2P(View3DObject object, View3DM4x4 const& o2p)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, pr::FEql(o2p.w.w,1.0f), "View3D_ObjectSetO2P: invalid object transform");
		if (!object) throw std::exception("object is null");
		if (!pr::FEql(o2p.w.w,1.0f)) throw std::exception("invalid object to parent transform");
		object->m_o2p = view3d::To<pr::m4x4>(o2p);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_ObjectSetO2P failed", ex);
	}
}

// Set the object colour
// See LdrObject::Apply for docs on the format of 'name'
VIEW3D_API void __stdcall View3D_ObjectSetColour(View3DObject object, View3DColour colour, UINT32 mask, char const* name)
{
	LOCK_GUARD;
	try
	{
		if (object == nullptr)
			throw std::exception("Null object provided");

		object->SetColour(pr::Colour32::make(colour), mask, name);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_ObjectSetColour failed", ex);
	}
}

// Set the texture
// See LdrObject::Apply for docs on the format of 'name'
VIEW3D_API void __stdcall View3D_ObjectSetTexture(View3DObject object, View3DTexture tex, char const* name)
{
	LOCK_GUARD;
	try
	{
		if (object == nullptr)
			throw std::exception("Null object provided");

		object->SetTexture(tex, name);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_ObjectSetTexture failed", ex);
	}
}

// Return the model space bounding box for 'object'
VIEW3D_API View3DBBox __stdcall View3D_ObjectBBoxMS(View3DObject object)
{
	LOCK_GUARD;
	try
	{
		return view3d::To<View3DBBox>(object->BBoxMS(true));
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_ObjectBBoxMS failed", ex);
		return view3d::To<View3DBBox>(pr::BBoxUnit);
	}
}

// Materials ***************************************************************

// Create a texture from data in memory.
// Set 'data' to 0 to leave the texture uninitialised, if not 0 then data must point to width x height pixel data
// of the size appropriate for the given format. e.g. pr::uint px_data[width * height] for D3DFMT_A8R8G8B8
// Note: careful with stride, 'data' is expected to have the appropriate stride for pr::rdr::BytesPerPixel(format) * width
VIEW3D_API EView3DResult __stdcall View3D_TextureCreate(UINT32 width, UINT32 height, void const* data, UINT32 data_size, View3DTextureOptions const& options, View3DTexture& tex)
{
	LOCK_GUARD;
	try
	{
		Image src = Image::make(width, height, data, options.m_format);
		if (src.m_pixels != nullptr && src.m_pitch.x * src.m_pitch.y != data_size)
			throw std::exception("Incorrect data size provided");

		TextureDesc tdesc(src);
		tdesc.Format = options.m_format;
		tdesc.MipLevels = options.m_mips;
		tdesc.BindFlags = options.m_bind_flags | (options.m_gdi_compatible ? D3D11_BIND_SHADER_RESOURCE|D3D11_BIND_RENDER_TARGET : 0);
		tdesc.MiscFlags = options.m_misc_flags | (options.m_gdi_compatible ? D3D11_RESOURCE_MISC_GDI_COMPATIBLE : 0);

		SamplerDesc sdesc;
		sdesc.AddressU = options.m_addrU;
		sdesc.AddressV = options.m_addrV;
		sdesc.Filter = options.m_filter;

		Texture2DPtr t = options.m_gdi_compatible
			? Rdr().m_renderer.m_tex_mgr.CreateTextureGdi(AutoId, src, tdesc, sdesc)
			: Rdr().m_renderer.m_tex_mgr.CreateTexture2D(AutoId, src, tdesc, sdesc);

		t->m_has_alpha = options.m_has_alpha != 0;
		tex = t.m_ptr; t.m_ptr = nullptr; // rely on the caller for correct reference counting
		return EView3DResult::Success;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_TextureCreate failed", ex);
		return EView3DResult::Failed;
	}
}

// Load a texture from file. Specify width == 0, height == 0 to use the dimensions of the file
VIEW3D_API EView3DResult __stdcall View3D_TextureCreateFromFile(char const* tex_filepath, UINT32 width, UINT32 height, View3DTextureOptions const& options, View3DTexture& tex)
{
	LOCK_GUARD;
	try
	{
		(void)width;
		(void)height;

		SamplerDesc sdesc;
		sdesc.AddressU = options.m_addrU;
		sdesc.AddressV = options.m_addrV;
		sdesc.Filter   = options.m_filter;

		Texture2DPtr t = Rdr().m_renderer.m_tex_mgr.CreateTexture2D(AutoId, sdesc, tex_filepath);
		tex = t.m_ptr; t.m_ptr = nullptr; // rely on the caller for correct reference counting
		return EView3DResult::Success;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_TextureCreateFromFile failed", ex);
		return EView3DResult::Failed;
	}
}

// Get/Release a DC for the texture. Must be a TextureGdi texture
VIEW3D_API HDC __stdcall View3D_TextureGetDC(View3DTexture tex)
{
	LOCK_GUARD;
	try
	{
		if (tex == nullptr)
			throw std::exception("Null texture provided");

		return tex->GetDC();
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_TextureGetDC failed", ex);
		return nullptr;
	}
}
VIEW3D_API void __stdcall View3D_TextureReleaseDC(View3DTexture tex)
{
	LOCK_GUARD;
	try
	{
		if (tex == nullptr)
			throw std::exception("Null texture provided");

		tex->ReleaseDC();
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_TextureReleaseDC failed", ex);
	}
}

// Load a texture surface from file
VIEW3D_API EView3DResult __stdcall View3D_TextureLoadSurface(View3DTexture tex, int level, char const* tex_filepath, RECT const* dst_rect, RECT const* src_rect, UINT32 filter, View3DColour colour_key)
{
	LOCK_GUARD;
	try
	{
		(void)tex;
		(void)level;
		(void)tex_filepath;
		(void)dst_rect;
		(void)src_rect;
		(void)filter;
		(void)colour_key;
		//return EView3DResult::Failed;
		throw std::exception("not implemented");
		//try
		//{
		//	tex->LoadSurfaceFromFile(tex_filepath, level, dst_rect, src_rect, filter, colour_key);
		//	return EView3DResult::Success;
		//}
		//catch (std::exception const& e)
		//{
		//	PR_LOGE(Rdr().m_log, Exception, e, "Failed to load texture surface from file");
		//	return EView3DResult::Failed;
		//}
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_TextureLoadSurface failed", ex);
		return EView3DResult::Failed;
	}
}

// Release a texture to free memory
VIEW3D_API void __stdcall View3D_TextureDelete(View3DTexture tex)
{
	LOCK_GUARD;
	try
	{
		tex->Release();
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_TextureDelete failed", ex);
	}
}

// Read the properties of an existing texture
VIEW3D_API void __stdcall View3D_TextureGetInfo(View3DTexture tex, View3DImageInfo& info)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, tex != 0, "");
		if (!tex) throw std::exception("texture is null");
		auto tex_info = tex->TexDesc();
		info.m_width             = tex_info.Width;
		info.m_height            = tex_info.Height;
		info.m_depth             = 0;
		info.m_mips              = tex_info.MipLevels;
		info.m_format            = tex_info.Format;
		info.m_image_file_format = 0;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_TextureGetInfo failed", ex);
	}
}

// Read the properties of an image file
VIEW3D_API EView3DResult __stdcall View3D_TextureGetInfoFromFile(char const* tex_filepath, View3DImageInfo& info)
{
	LOCK_GUARD;
	try
	{
		(void)tex_filepath;
		(void)info;
		//D3DXIMAGE_INFO tex_info;
		//if (pr::Failed(Rdr().m_renderer.m_mat_mgr.TextureInfo(tex_filepath, tex_info)))
		//	return EView3DResult::Failed;

		//info.m_width             = tex_info.Width;
		//info.m_height            = tex_info.Height;
		//info.m_depth             = tex_info.Depth;
		//info.m_mips              = tex_info.MipLevels;
		//info.m_format            = tex_info.Format;
		//info.m_image_file_format = tex_info.ImageFileFormat;
		throw std::exception("not implemented");
		//return EView3DResult::Success;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_TextureGetInfoFromFile failed", ex);
		return EView3DResult::Failed;
	}
}

// Set the filtering and addressing modes to use on the texture
VIEW3D_API void __stdcall View3D_TextureSetFilterAndAddrMode(View3DTexture tex, D3D11_FILTER filter, D3D11_TEXTURE_ADDRESS_MODE addrU, D3D11_TEXTURE_ADDRESS_MODE addrV)
{
	LOCK_GUARD;
	try
	{
		SamplerDesc desc;
		tex->m_samp->GetDesc(&desc);
		desc.Filter = filter;
		desc.AddressU = addrU;
		desc.AddressV = addrV;

		D3DPtr<ID3D11SamplerState> samp;
		pr::Throw(Rdr().m_renderer.Device()->CreateSamplerState(&desc, &samp.m_ptr));
		tex->m_samp = samp;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_TextureGetInfoFromFile failed", ex);
	}
}

// Resize a texture to 'size' optionally preserving it's content
VIEW3D_API void __stdcall View3D_TextureResize(View3DTexture tex, UINT32 width, UINT32 height, BOOL all_instances, BOOL preserve)
{
	LOCK_GUARD;
	try
	{
		tex->Resize(width, height, all_instances != 0, preserve != 0);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_TextureResize failed", ex);
	}
}

// Return the render target as a texture
VIEW3D_API View3DTexture __stdcall View3D_TextureRenderTarget()
{
	LOCK_GUARD;
	try
	{
		auto tex = Rdr().m_renderer.m_tex_mgr.FindTexture(EStockTexture::MainRT);
		return tex.m_ptr;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_TextureResize failed", ex);
		return nullptr;
	}
}

// Rendering ***************************************************************

// Finish rendering with a back buffer flip
VIEW3D_API void __stdcall View3D_Present()
{
	LOCK_GUARD;
	try
	{
		Rdr().m_renderer.Present();
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_Present failed", ex);
	}
}

// Get/Set the dimensions of the render target
// In set, if 'width' and 'height' are zero, the RT is resized to the associated window automatically.
VIEW3D_API void __stdcall View3D_RenderTargetSize(int& width, int& height)
{
	LOCK_GUARD;
	try
	{
		auto area = Rdr().m_renderer.RenderTargetSize();
		width     = area.x;
		height    = area.y;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_RenderTargetSize failed", ex);
	}
}
VIEW3D_API void __stdcall View3D_SetRenderTargetSize(int width, int height)
{
	LOCK_GUARD;
	try
	{
		if (width  < 0) width  = 0;
		if (height < 0) height = 0;
		Rdr().m_renderer.RenderTargetSize(pr::iv2::make(width, height));
		auto size = Rdr().m_renderer.RenderTargetSize();

		// Update the aspect ratio for all drawsets
		float aspect = (size.x == 0 || size.y == 0) ? 1.0f : size.x / float(size.y);
		for (auto ds : Rdr().m_drawset)
			ds->m_camera.Aspect(aspect);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_SetRenderTargetSize failed", ex);
	}
}

// Get/Set the viewport within the render target
VIEW3D_API View3DViewport __stdcall View3D_Viewport()
{
	LOCK_GUARD;
	try
	{
		auto& scene_vp = Rdr().m_scene.m_viewport;
		View3DViewport vp = {};
		vp.m_x         = scene_vp.TopLeftX;
		vp.m_y         = scene_vp.TopLeftY;
		vp.m_width     = scene_vp.Width;
		vp.m_height    = scene_vp.Height;
		vp.m_min_depth = scene_vp.MinDepth;
		vp.m_max_depth = scene_vp.MaxDepth;
		return vp;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_Viewport failed", ex);
		return View3DViewport();
	}
}
VIEW3D_API void __stdcall View3D_SetViewport(View3DViewport vp)
{
	LOCK_GUARD;
	try
	{
		auto& scene_vp = Rdr().m_scene.m_viewport;
		scene_vp.TopLeftX = vp.m_x        ;
		scene_vp.TopLeftY = vp.m_y        ;
		scene_vp.Width    = vp.m_width    ;
		scene_vp.Height   = vp.m_height   ;
		scene_vp.MinDepth = vp.m_min_depth;
		scene_vp.MaxDepth = vp.m_max_depth;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_SetViewport failed", ex);
	}
}

// Get/Set the fill mode for a drawset
VIEW3D_API EView3DFillMode __stdcall View3D_FillMode(View3DDrawset drawset)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		return drawset->m_fill_mode;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_FillMode failed", ex);
		return EView3DFillMode();
	}
}
VIEW3D_API void __stdcall View3D_SetFillMode(View3DDrawset drawset, EView3DFillMode mode)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		drawset->m_fill_mode = mode;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_SetFillMode failed", ex);
	}
}

// Selected between perspective and orthographic projection
VIEW3D_API BOOL __stdcall View3D_Orthographic(View3DDrawset drawset)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		return drawset->m_camera.m_orthographic;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_Orthographic failed", ex);
		return false;
	}
}
VIEW3D_API void __stdcall View3D_SetOrthographic(View3DDrawset drawset, BOOL render2d)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		drawset->m_camera.m_orthographic = render2d != 0;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_SetOrthographic failed", ex);
	}
}

// Get/Set the background colour for a drawset
VIEW3D_API int __stdcall View3D_BackgroundColour(View3DDrawset drawset)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		return drawset->m_background_colour;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_BackgroundColour failed", ex);
		return 0;
	}
}
VIEW3D_API void __stdcall View3D_SetBackgroundColour(View3DDrawset drawset, int aarrggbb)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		drawset->m_background_colour = pr::Colour32::make(aarrggbb);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_SetBackgroundColour failed", ex);
	}
}

// Show the measurement tool
VIEW3D_API BOOL __stdcall View3D_MeasureToolVisible()
{
	LOCK_GUARD;
	try
	{
		return Rdr().m_measure_tool_ui.IsWindowVisible();
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_MeasureToolVisible failed", ex);
		return false;
	}
}
VIEW3D_API void __stdcall View3D_ShowMeasureTool(View3DDrawset drawset, BOOL show)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		Rdr().m_measure_tool_ui.SetReadPointCtx(drawset);
		Rdr().m_measure_tool_ui.Show(show != 0);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_ShowMeasureTool failed", ex);
	}
}

// Show the angle tool
VIEW3D_API BOOL __stdcall View3D_AngleToolVisible()
{
	LOCK_GUARD;
	try
	{
		return Rdr().m_angle_tool_ui.IsWindowVisible();
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_AngleToolVisible failed", ex);
		return false;
	}
}
VIEW3D_API void __stdcall View3D_ShowAngleTool(View3DDrawset drawset, BOOL show)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		Rdr().m_angle_tool_ui.SetReadPointCtx(drawset);
		Rdr().m_angle_tool_ui.Show(show != 0);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_ShowAngleTool failed", ex);
	}
}

// Restore the main render target and depth buffer
VIEW3D_API void __stdcall View3D_RestoreMainRT()
{
	LOCK_GUARD;
	try
	{
		Rdr().m_renderer.RestoreMainRT();
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_RestoreMainRT failed", ex);
	}
}

// Returns true if the depth buffer is enabled
VIEW3D_API BOOL __stdcall View3D_DepthBufferEnabled()
{
	LOCK_GUARD;
	try
	{
		return Rdr().m_scene.m_dsb.Desc().DepthEnable;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_DepthBufferEnabled failed", ex);
		return TRUE;
	}
}

// Enables or disables the depth buffer
VIEW3D_API void __stdcall View3D_SetDepthBufferEnabled(BOOL enabled)
{
	LOCK_GUARD;
	try
	{
		Rdr().m_scene.m_dsb.Set(EDS::DepthEnable, enabled);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_SetDepthBufferEnabled failed", ex);
	}
}

// Create a scene showing the capabilities of view3d (actually of ldr_object_manager)
VIEW3D_API void __stdcall View3D_CreateDemoScene(View3DDrawset drawset)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");

		size_t initial = Rdr().m_obj_cont.size();
		pr::ldr::AddString(Rdr().m_renderer, pr::ldr::CreateDemoScene().c_str(), Rdr().m_obj_cont, pr::ldr::DefaultContext, true, 0, &Rdr().m_lua);
		size_t final = Rdr().m_obj_cont.size();
		for (size_t i = initial; i != final; ++i)
		{
			View3D_DrawsetAddObject(drawset, Rdr().m_obj_cont[i].m_ptr);
		}
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_CreateDemoScene failed", ex);
	}
}

// Show a window containing the demo scene script
VIEW3D_API void __stdcall View3D_ShowDemoScript()
{
	LOCK_GUARD;
	try
	{
		Rdr().m_obj_cont_ui.ShowScript(pr::ldr::CreateDemoScene(), 0);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_ShowDemoScript failed", ex);
	}
}

// Return true if the focus point is visible
VIEW3D_API BOOL __stdcall View3D_FocusPointVisible(View3DDrawset drawset)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		return drawset->m_focus_point_visible;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_FocusPointVisible failed", ex);
		return false;
	}
}

// Add the focus point to a drawset
VIEW3D_API void __stdcall View3D_ShowFocusPoint(View3DDrawset drawset, BOOL show)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		drawset->m_focus_point_visible = show != 0;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_ShowFocusPoint failed", ex);
	}
}

// Set the size of the focus point
VIEW3D_API void __stdcall View3D_SetFocusPointSize(View3DDrawset drawset, float size)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		drawset->m_focus_point_size = size;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_SetFocusPointSize failed", ex);
	}
}

// Return true if the origin is visible
VIEW3D_API BOOL __stdcall View3D_OriginVisible(View3DDrawset drawset)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		return drawset->m_origin_point_visible;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_OriginVisible failed", ex);
		return false;
	}
}

// Add the focus point to a drawset
VIEW3D_API void __stdcall View3D_ShowOrigin(View3DDrawset drawset, BOOL show)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		drawset->m_origin_point_visible = show != 0;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_ShowOrigin failed", ex);
	}
}

// Set the size of the focus point
VIEW3D_API void __stdcall View3D_SetOriginSize(View3DDrawset drawset, float size)
{
	LOCK_GUARD;
	try
	{
		PR_ASSERT(PR_DBG, drawset != 0, "");
		if (!drawset) throw std::exception("drawset is null");
		drawset->m_origin_point_size = size;
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_SetOriginSize failed", ex);
	}
}

// Display the object manager ui
VIEW3D_API void __stdcall View3D_ShowObjectManager(BOOL show)
{
	LOCK_GUARD;
	try
	{
		Rdr().m_obj_cont_ui.Show(show != 0);
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_ShowObjectManager failed", ex);
	}}

// Parse an ldr *o2w {} description returning the transform
VIEW3D_API View3DM4x4 __stdcall View3D_ParseLdrTransform(char const* ldr_script)
{
	LOCK_GUARD;
	try
	{
		pr::script::Reader reader;
		pr::script::PtrSrc src(ldr_script);
		reader.AddSource(src);
		return view3d::To<View3DM4x4>(pr::ldr::ParseLdrTransform(reader));
	}
	catch (std::exception const& ex)
	{
		Dll().ReportError("View3D_ParseLdrTransform failed", ex);
		return view3d::To<View3DM4x4>(pr::m4x4Identity);
	}
}
