//***************************************************************************************************
// View 3D
//  Copyright © Rylogic Ltd 2009
//***************************************************************************************************

#include "stdafx.h"
#include "view3d/renderer_instance.h"
#include "pr/renderer11/lights/light_dlg.h"
#include "pr/common/assert.h"
#include "pr/common/array.h"
#include "pr/filesys/fileex.h"
#include "pr/script/reader.h"
#include "pr/script/embedded_lua.h"
#include "pr/view3d/view3d.h"
#include "pr/renderer11/renderer.h"

using namespace view3d;

// Global data for this dll
struct DllData
{
	CAppModule        m_module;
	HINSTANCE         m_hInstance;
	RendererInstance* m_rdr;
	std::string       m_settings;

	DllData(HINSTANCE hInstance)
		:m_hInstance(hInstance)
		,m_rdr(0)
	{
		AtlInitCommonControls(ICC_BAR_CLASSES); // add flags to support other controls
		m_module.Init(0, m_hInstance);
	}
	~DllData()
	{
		delete m_rdr;
		m_module.Term();
	}
};
DllData* g_dll_data = 0;
inline DllData&          Dll() { return *g_dll_data; }
inline RendererInstance& Rdr() { return *g_dll_data->m_rdr; }

// Dll entry point
#ifdef _MANAGED
#pragma managed(push, off)
#endif
BOOL APIENTRY DllMain(HMODULE hInstance, DWORD ul_reason_for_call, LPVOID)
{
	switch (ul_reason_for_call)
	{
	case DLL_THREAD_ATTACH:  break;
	case DLL_THREAD_DETACH:  break;
	case DLL_PROCESS_ATTACH: g_dll_data = new DllData(hInstance); break;
	case DLL_PROCESS_DETACH: delete g_dll_data; g_dll_data = 0; break;
	}
	return TRUE;
}
#ifdef _MANAGED
#pragma managed(pop)
#endif

// Initialise the dll
VIEW3D_API EView3DResult __stdcall View3D_Initialise(HWND hwnd, View3D_ReportErrorCB error_cb, View3D_SettingsChanged settings_changed_cb)
{
	try
	{
		if (Dll().m_rdr == 0)
		{
			pr::rdr::TestSystemCompatibility();
			g_dll_data->m_rdr = new RendererInstance(hwnd, error_cb, settings_changed_cb);
			Rdr().CreateStockObjects();
		}

		return EView3DResult::Success;
	}
	catch (std::exception const& e)
	{
		char const* msg = pr::FmtS("Failed to initialise View3D.\nReason: %s\n", e.what());
		error_cb(msg);
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
	delete g_dll_data->m_rdr;
	g_dll_data->m_rdr = 0;
}

// Generate a settings string for the view
VIEW3D_API char const* __stdcall View3D_GetSettings(View3DDrawset drawset)
{
	std::stringstream out;
	out << "*SceneSettings {" << Rdr().m_obj_cont_ui.Settings() << "}\n";
	out << "*Light {\n" << drawset->m_light.Settings() << "}\n";
	g_dll_data->m_settings = out.str();
	return g_dll_data->m_settings.c_str();
}

// Parse a settings string and apply to the view
VIEW3D_API void __stdcall View3D_SetSettings(View3DDrawset drawset, char const* settings)
{
	try
	{
		// Parse the settings
		pr::script::Reader reader;
		pr::script::PtrSrc src(settings);
		reader.AddSource(src);

		std::string desc;
		for (pr::script::string kw; reader.NextKeywordS(kw);)
		{
			if (pr::str::EqualI(kw, "SceneSettings"))
			{
				reader.ExtractSection(desc, false);
				Rdr().m_obj_cont_ui.Settings(desc.c_str());
				continue;
			}
			if (pr::str::EqualI(kw, "Light"))
			{
				reader.ExtractSection(desc, false);
				drawset->m_light.Settings(desc.c_str());
				continue;
			}
		}
		View3D_GetSettings(drawset);
		if (Rdr().m_settings_changed_cb)
			Rdr().m_settings_changed_cb();
	}
	catch (std::exception const& e)
	{
		Rdr().m_error_cb(e.what());
	}
}

// Create/Delete a draw set
VIEW3D_API EView3DResult __stdcall View3D_DrawsetCreate(View3DDrawset& drawset)
{
	drawset = new Drawset();
	Rdr().m_drawset.insert(drawset);

	// Set the initial aspect ratio
	pr::iv2 client_area = Rdr().m_renderer.RenderTargetSize();
	float aspect = client_area.x / float(client_area.y);
	drawset->m_camera.Aspect(aspect);

	return EView3DResult::Success;
}
VIEW3D_API void __stdcall View3D_DrawsetDelete(View3DDrawset drawset)
{
	View3D_DrawsetRemoveAllObjects(drawset);
	Rdr().m_drawset.erase(drawset);
	delete drawset;
}

// Add/Remove objects by context id
VIEW3D_API void __stdcall View3D_DrawsetAddObjectsById(View3DDrawset drawset, int context_id)
{
	for (std::size_t i = 0, iend = Rdr().m_obj_cont.size(); i != iend; ++i)
		if (Rdr().m_obj_cont[i]->m_context_id == context_id)
			View3D_DrawsetAddObject(drawset, Rdr().m_obj_cont[i].m_ptr);
}
VIEW3D_API void __stdcall View3D_DrawsetRemoveObjectsById(View3DDrawset drawset, int context_id)
{
	pr::ldr::LdrObject::MatchId in_this_context(context_id);
	for (ObjectCont::iterator i = drawset->m_objects.begin(), iend = drawset->m_objects.end(); i != iend; ++i)
		if ((*i)->m_context_id == context_id) drawset->m_objects.erase(i);
}

// Add/Remove an object to/from a drawset
VIEW3D_API void __stdcall View3D_DrawsetAddObject(View3DDrawset drawset, View3DObject object)
{
	PR_ASSERT(PR_DBG, drawset != 0 && object != 0, "");
	ObjectCont::const_iterator iter = drawset->m_objects.find(object);
	if (iter == drawset->m_objects.end()) drawset->m_objects.insert(iter, object);
}
VIEW3D_API void __stdcall View3D_DrawsetRemoveObject(View3DDrawset drawset, View3DObject object)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	if (!object) return;
	drawset->m_objects.erase(object);
}

// Remove all objects from the drawset
VIEW3D_API void __stdcall View3D_DrawsetRemoveAllObjects(View3DDrawset drawset)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	drawset->m_objects.clear();
}

// Return the number of objects assigned to this drawset
VIEW3D_API int __stdcall View3D_DrawsetObjectCount(View3DDrawset drawset)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	return int(drawset->m_objects.size());
}

// Return true if 'object' is included in 'drawset'
VIEW3D_API BOOL __stdcall View3D_DrawsetHasObject(View3DDrawset drawset, View3DObject object)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	return drawset->m_objects.find(object) != drawset->m_objects.end();
}

// Return the camera to world transform
VIEW3D_API void __stdcall View3D_CameraToWorld(View3DDrawset drawset, pr::m4x4& c2w)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	c2w = drawset->m_camera.m_c2w;
}

// Set the camera to world transform
VIEW3D_API void __stdcall View3D_SetCameraToWorld(View3DDrawset drawset, pr::m4x4 const& c2w)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	drawset->m_camera.m_c2w = c2w;
}

// Position the camera for a drawset
VIEW3D_API void __stdcall View3D_PositionCamera(View3DDrawset drawset, pr::v4 const& position, pr::v4 const& lookat, pr::v4 const& up)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	drawset->m_camera.LookAt(position, lookat, up, true);
}

// Return the distance to the camera focus point
VIEW3D_API float __stdcall View3D_FocusDistance(View3DDrawset drawset)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	return drawset->m_camera.FocusDist();
}

// Set the camera focus distance
VIEW3D_API void __stdcall View3D_SetFocusDistance(View3DDrawset drawset, float dist)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	drawset->m_camera.FocusDist(dist);
}

// Return the aspect ratio for the camera field of view
VIEW3D_API float __stdcall View3D_CameraAspect(View3DDrawset drawset)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	return drawset->m_camera.Aspect();
}

// Set the aspect ratio for the camera field of view
VIEW3D_API void __stdcall View3D_SetCameraAspect(View3DDrawset drawset, float aspect)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	drawset->m_camera.Aspect(aspect);
}

// Return the horizontal field of view (in radians).
VIEW3D_API float __stdcall View3D_CameraFovX(View3DDrawset drawset)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	return drawset->m_camera.FovX();
}

// Set the horizontal field of view (in radians). Note aspect ratio is preserved, setting FovX changes FovY and visa versa
VIEW3D_API void __stdcall View3D_SetCameraFovX(View3DDrawset drawset, float fovX)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	drawset->m_camera.FovX(fovX);
}

// Return the vertical field of view (in radians).
VIEW3D_API float __stdcall View3D_CameraFovY(View3DDrawset drawset)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	return drawset->m_camera.FovY();
}

// Set the vertical field of view (in radians). Note aspect ratio is preserved, setting FovY changes FovX and visa versa
VIEW3D_API void __stdcall View3D_SetCameraFovY(View3DDrawset drawset, float fovY)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	drawset->m_camera.FovY(fovY);
}

// General mouse navigation
// void OnMouseDown(UINT nFlags, CPoint point) { View3D_Navigate(m_drawset, pr::NormalisePoint(m_hWnd, point, -1.0f), nFlags, TRUE); }
// void OnMouseMove(UINT nFlags, CPoint point) { View3D_Navigate(m_drawset, pr::NormalisePoint(m_hWnd, point, -1.0f), nFlags, FALSE); } if 'nFlags' is zero, this will have no effect
// void OnMouseUp  (UINT nFlags, CPoint point) { View3D_Navigate(m_drawset, pr::NormalisePoint(m_hWnd, point, -1.0f), 0, TRUE); }
VIEW3D_API void __stdcall View3D_Navigate(View3DDrawset drawset, pr::v2 point, int button_state, BOOL nav_start_or_end)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	if (nav_start_or_end)  drawset->m_camera.MoveRef(point, button_state);
	else if (button_state) drawset->m_camera.Move   (point, button_state);
}

// Z axis mouse movement
// BOOL OnMouseWheel(UINT nFlags, short zDelta, CPoint) { if (nFlags == 0) View3D_NavigateZ(m_drawset, zDelta / 120.0f); return TRUE; }
VIEW3D_API void __stdcall View3D_NavigateZ(View3DDrawset drawset, float delta)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	drawset->m_camera.MoveZ(delta, true);
}

// Reset to the default zoom
VIEW3D_API void __stdcall View3D_ResetZoom(View3DDrawset drawset)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	drawset->m_camera.ResetZoom();
}

// Return the camera align axis
VIEW3D_API void __stdcall View3D_CameraAlignAxis(View3DDrawset drawset, pr::v4& axis)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	axis = drawset->m_camera.m_align;
}

// Align the camera to an axis
VIEW3D_API void __stdcall View3D_AlignCamera(View3DDrawset drawset, pr::v4 const& axis)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	drawset->m_camera.SetAlign(axis);
}

// Move the camera to a position that can see the whole scene
VIEW3D_API void __stdcall View3D_ResetView(View3DDrawset drawset, pr::v4 const& forward, pr::v4 const& up)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");

	// The bounding box for the scene
	auto bbox = pr::BBoxReset;
	for (auto obj : drawset->m_objects)
		pr::Encompass(bbox, obj->BBoxWS(true));
	if (bbox == pr::BBoxReset) bbox = pr::BBoxUnit;
	drawset->m_camera.View(bbox, forward, up, true);
}

// Get/Set the camera focus point position
VIEW3D_API void __stdcall View3D_GetFocusPoint(View3DDrawset drawset, pr::v4& position)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	position = drawset->m_camera.FocusPoint();
}
VIEW3D_API void __stdcall View3D_SetFocusPoint(View3DDrawset drawset, pr::v4 const& position)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	drawset->m_camera.FocusPoint(position);
}

// Return a point in world space corresponding to a normalised screen space point.
// The x,y components of 'screen' should be in normalised screen space, i.e. (-1,-1)->(1,1)
// The z component should be the world space distance from the camera
VIEW3D_API pr::v4 __stdcall View3D_WSPointFromNormSSPoint(View3DDrawset drawset, pr::v4 const& screen)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	return drawset->m_camera.WSPointFromNormSSPoint(screen);
}

// Return a point in normalised screen space corresponding to a world space point.
// The returned z component will be the world space distance from the camera.
VIEW3D_API pr::v4 __stdcall View3D_NormSSPointFromWSPoint (View3DDrawset drawset, pr::v4 const& world)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	return drawset->m_camera.NormSSPointFromWSPoint(world);
}

// Return a point and direction in world space corresponding to a normalised sceen space point.
// The x,y components of 'screen' should be in normalised screen space, i.e. (-1,-1)->(1,1)
// The z component should be the world space distance from the camera
VIEW3D_API void __stdcall View3D_WSRayFromNormSSPoint(View3DDrawset drawset, pr::v4 const& screen, pr::v4& ws_point, pr::v4& ws_direction)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	drawset->m_camera.WSRayFromNormSSPoint(screen, ws_point, ws_direction);
}

// Lighting ********************************************************

// Return the configuration of the single light source
VIEW3D_API View3DLight __stdcall View3D_LightProperties(View3DDrawset drawset)
{
	return reinterpret_cast<View3DLight const&>(drawset->m_light);
}

// Configure the single light source
VIEW3D_API void __stdcall View3D_SetLightProperties(View3DDrawset drawset, View3DLight const& light)
{
	drawset->m_light = reinterpret_cast<pr::rdr::Light const&>(light);
}

// Set up a single light source for a drawset
VIEW3D_API void __stdcall View3D_LightSource(View3DDrawset drawset, pr::v4 const& position, pr::v4 const& direction, BOOL camera_relative)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	drawset->m_light.m_position = position;
	drawset->m_light.m_direction = direction;
	drawset->m_light_is_camera_relative = camera_relative != 0;
}

// Show the lighting UI
struct PreviewLighting
{
	View3DDrawset m_drawset;
	PreviewLighting(View3DDrawset drawset) :m_drawset(drawset) {}
	void operator()(pr::rdr::Light const& light, bool camera_relative)
	{
		pr::rdr::Light prev_light				= m_drawset->m_light;
		bool prev_camera_relative				= m_drawset->m_light_is_camera_relative;
		m_drawset->m_light						= light;
		m_drawset->m_light_is_camera_relative	= camera_relative;

		View3D_Render(m_drawset);

		m_drawset->m_light						= prev_light;
		m_drawset->m_light_is_camera_relative	= prev_camera_relative;
	}
};
VIEW3D_API void __stdcall View3D_ShowLightingDlg(View3DDrawset drawset, HWND parent)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	PreviewLighting pv(drawset);
	pr::rdr::LightingDlg<PreviewLighting> dlg(pv);
	dlg.m_light           = drawset->m_light;
	dlg.m_camera_relative = drawset->m_light_is_camera_relative;
	if (dlg.DoModal(parent) != IDOK) return;
	drawset->m_light                    = dlg.m_light;
	drawset->m_light_is_camera_relative = dlg.m_camera_relative;
	View3D_Render(drawset);
	if (Rdr().m_settings_changed_cb)
		Rdr().m_settings_changed_cb();
}

// Create/Delete objects ********************************************************
// Create objects given in a file.
// These objects will not have handles but can be deleted by their context id
VIEW3D_API EView3DResult __stdcall View3D_ObjectsCreateFromFile(char const* ldr_filepath, int context_id, BOOL async)
{
	try
	{
		pr::ldr::AddFile(Rdr().m_renderer, ldr_filepath, Rdr().m_obj_cont, context_id, async != 0, 0, &Rdr().m_lua);
		return EView3DResult::Success;
	}
	catch (std::exception const& ex)
	{
		Rdr().m_error_cb(ex.what());
		return EView3DResult::Failed;
	}
}

// If multiple objects are created, the handle returned is to the last object only
VIEW3D_API EView3DResult __stdcall View3D_ObjectCreateLdr(char const* ldr_script, int context_id, View3DObject& object, BOOL async)
{
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
		Rdr().m_error_cb(ex.what());
		return EView3DResult::Failed;
	}
}

// Modify the geometry of an existing object
struct ObjectEditCBData
{
	View3D_EditObjectCB edit_cb;
	void* ctx;
};
void __stdcall ObjectEditCB(pr::rdr::ModelPtr model, void* ctx, pr::Renderer& rdr)
{
	PR_ASSERT(PR_DBG, model != 0, "");
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
	if (!model->m_nmap[pr::rdr::ERenderStep::ForwardRender].empty())
	{
		auto nug = model->m_nmap[pr::rdr::ERenderStep::ForwardRender].front();
		auto mat = nug.m_draw;
		model_type = static_cast<EView3DPrim>(nug.m_prim_topo.value);
		geom_type  = static_cast<EView3DGeom>(mat.m_shader->m_geom_mask.value);
		v3dmat.m_diff_tex = mat.m_tex_diffuse.m_ptr;
		v3dmat.m_env_map  = mat.m_tex_env_map.m_ptr;
	}

	// Get the user to generate the model
	size_t new_vcount, new_icount;
	cbdata.edit_cb(vrange.size(), irange.size(), &verts[0], &indices[0], new_vcount, new_icount, model_type, geom_type, v3dmat, cbdata.ctx);
	PR_ASSERT(PR_DBG, new_vcount <= vrange.size(), "");
	PR_ASSERT(PR_DBG, new_icount <= irange.size(), "");
	PR_ASSERT(PR_DBG, model_type != EView3DPrim::Invalid, "");
	PR_ASSERT(PR_DBG, geom_type != EView3DGeom::Unknown, "");

	// Update the material
	pr::rdr::DrawMethod mat;
	mat.m_shader = rdr.m_shdr_mgr.FindShaderFor(static_cast<pr::rdr::EGeom::Enum_>(geom_type));
	mat.m_tex_diffuse = v3dmat.m_diff_tex;
	mat.m_tex_env_map = v3dmat.m_env_map;

	{// Lock and update the model
		pr::rdr::MLock mlock(model, D3D11_MAP_WRITE_DISCARD);
		model->m_bbox.reset();

		// Copy the model data into the model
		auto vin = begin(verts);
		auto vout = mlock.m_vlock.ptr<pr::rdr::VertPCNT>();
		for (size_t i = 0; i != new_vcount; ++i, ++vin)
		{
			SetPCNT(*vout++, vin->pos, pr::Colour32::make(vin->col), vin->norm, vin->tex);
			pr::Encompass(model->m_bbox, vin->pos);
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
	model->DeleteNuggets(pr::rdr::ERenderStep::ForwardRender);
	model->CreateNugget(pr::rdr::ERenderStep::ForwardRender, mat, static_cast<pr::rdr::EPrim::Enum_>(model_type), &vrange, &irange);
}

// Create an object via callback
VIEW3D_API EView3DResult __stdcall View3D_ObjectCreate(char const* name, pr::uint colour, int icount, int vcount, View3D_EditObjectCB edit_cb, void* ctx, int context_id, View3DObject& object)
{
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
		Rdr().m_error_cb(ex.what());
		return EView3DResult::Failed;
	}
}

// Edit an existing model
VIEW3D_API void __stdcall View3D_ObjectEdit(View3DObject object, View3D_EditObjectCB edit_cb, void* ctx)
{
	ObjectEditCBData cbdata = {edit_cb, ctx};
	pr::ldr::Edit(Rdr().m_renderer, object, ObjectEditCB, &cbdata);
}

// Delete all objects matching a context id
VIEW3D_API void __stdcall View3D_ObjectsDeleteById(int context_id)
{
	for (DrawsetCont::iterator i = Rdr().m_drawset.begin(), iend = Rdr().m_drawset.end(); i != iend; ++i)
		View3D_DrawsetRemoveObjectsById(*i, context_id);
	pr::ldr::Remove(Rdr().m_obj_cont, &context_id, 1, 0, 0);
}

// Delete an object
VIEW3D_API void __stdcall View3D_ObjectDelete(View3DObject object)
{
	if (!object) return;
	for (DrawsetCont::iterator i = Rdr().m_drawset.begin(), iend = Rdr().m_drawset.end(); i != iend; ++i)
		View3D_DrawsetRemoveObject(*i, object);
	pr::ldr::Remove(Rdr().m_obj_cont, object);
}

// Get/Set the object to parent transform for an object
// This is the object to world transform for objects without parents
// Note: In "*Box b { 1 1 1 *o2w{*pos{1 2 3}} }" setting this transform overwrites the "*o2w{*pos{1 2 3}}"
VIEW3D_API pr::m4x4 __stdcall View3D_ObjectGetO2P(View3DObject object)
{
	return object->m_o2p;
}
VIEW3D_API void __stdcall View3D_ObjectSetO2P(View3DObject object, pr::m4x4 const& o2p)
{
	PR_ASSERT(PR_DBG, pr::FEql(o2p.w.w,1.0f), "View3D_ObjectSetO2P: invalid object transform");
	object->m_o2p = o2p;
}

// Set the object colour
VIEW3D_API void __stdcall View3D_ObjectSetColour(View3DObject object, pr::uint colour, pr::uint mask, BOOL include_children)
{
	object->SetColour(pr::Colour32::make(colour), mask, include_children != 0);
}

// Assign a texture to an object
VIEW3D_API void __stdcall View3D_ObjectSetTexture(View3DObject object, View3DTexture tex)
{
	auto& nuggets = object->m_model->m_nmap[pr::rdr::ERenderStep::ForwardRender];
	for (auto& nug : nuggets)
		nug.m_draw.m_tex_diffuse = tex;
}

// Return the model space bounding box for 'object'
VIEW3D_API pr::BoundingBox __stdcall View3D_ObjectBBoxMS(View3DObject object)
{
	return object->BBoxMS(true);
}

// Materials ***************************************************************

// Create a texture from data in memory.
// Set 'data' to 0 to leave the texture uninitialised, if not 0 then data must point to width x height pixel data
// of the size appropriate for the given format. e.g. pr::uint px_data[width * height] for D3DFMT_A8R8G8B8
// Note: careful with stride, 'data' is expected to have the appropriate stride for pr::rdr::BytesPerPixel(format) * width
VIEW3D_API EView3DResult __stdcall View3D_TextureCreate(size_t width, size_t height, DXGI_FORMAT format, void const* data, size_t data_size, size_t mips, View3DTexture& tex)
{
	try
	{
		pr::rdr::Image src = pr::rdr::Image::make(width, height, data, format);
		if (src.m_pitch.x * src.m_pitch.y != data_size)
			throw std::exception("Incorrect data size provided");

		pr::rdr::TextureDesc tdesc(src, mips);
		pr::rdr::SamplerDesc sdesc;
		pr::rdr::Texture2DPtr t = Rdr().m_renderer.m_tex_mgr.CreateTexture2D(pr::rdr::AutoId, src, tdesc, sdesc);
		tex = t.m_ptr; t.m_ptr = 0; // rely on the caller for correct reference counting
		return EView3DResult::Success;
	}
	catch (std::exception const& e)
	{
		PR_LOGE(Rdr().m_log, Error, e, "Failed to create texture");
		return EView3DResult::Failed;
	}
}

// Load a texture from file. Specify width == 0, height == 0 to use the dimensions of the file
VIEW3D_API EView3DResult __stdcall View3D_TextureCreateFromFile(char const* tex_filepath, pr::uint width, pr::uint height, pr::uint mips, pr::uint filter, pr::uint mip_filter, pr::uint colour_key, View3DTexture& tex)
{
	try
	{
		(void)width;
		(void)height;
		(void)mips;
		(void)colour_key;
		(void)filter;
		(void)mip_filter;
		pr::rdr::SamplerDesc sdesc;
		pr::rdr::Texture2DPtr t = Rdr().m_renderer.m_tex_mgr.CreateTexture2D(pr::rdr::AutoId, sdesc, tex_filepath);
		tex = t.m_ptr; t.m_ptr = 0; // rely on the caller for correct reference counting
		return EView3DResult::Success;
	}
	catch (std::exception& e)
	{
		PR_LOGE(Rdr().m_log, Error, e, "Failed to create texture from file");
		return EView3DResult::Failed;
	}
}

// Load a texture surface from file
VIEW3D_API EView3DResult __stdcall View3D_TextureLoadSurface(View3DTexture tex, int level, char const* tex_filepath, RECT const* dst_rect, RECT const* src_rect, pr::uint filter, pr::uint colour_key)
{
	(void)tex;
	(void)level;
	(void)tex_filepath;
	(void)dst_rect;
	(void)src_rect;
	(void)filter;
	(void)colour_key;
	return EView3DResult::Failed;
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

// Release a texture to free memory
VIEW3D_API void __stdcall View3D_TextureDelete(View3DTexture tex)
{
	tex->Release();
}

// Read the properties of an existing texture
VIEW3D_API void __stdcall View3D_TextureGetInfo(View3DTexture tex, View3DImageInfo& info)
{
	PR_ASSERT(PR_DBG, tex != 0, "");
	auto tex_info = tex->TexDesc();
	info.m_width             = tex_info.Width;
	info.m_height            = tex_info.Height;
	info.m_depth             = 0;
	info.m_mips              = tex_info.MipLevels;
	info.m_format            = tex_info.Format;
	info.m_image_file_format = 0;
}

// Read the properties of an image file
VIEW3D_API EView3DResult __stdcall View3D_TextureGetInfoFromFile(char const* tex_filepath, View3DImageInfo& info)
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
	return EView3DResult::Success;
}

// Rendering ***************************************************************

// Redraw the last rendered drawset
VIEW3D_API void __stdcall View3D_Refresh()
{
	if (!Dll().m_rdr) return;
	if (Rdr().m_last_drawset)
		View3D_Render(Rdr().m_last_drawset);
}

// Get/Set the dimensions of the render target
// In set, if 'width' and 'height' are zero, the RT is resized to the associated window automatically.
VIEW3D_API void __stdcall View3D_RenderTargetSize(int& width, int& height)
{
	if (!Dll().m_rdr) return;
	auto area = Rdr().m_renderer.RenderTargetSize();
	width     = area.x;
	height    = area.y;
}
VIEW3D_API void __stdcall View3D_SetRenderTargetSize(int width, int height)
{
	if (!Dll().m_rdr) return;
	if (width  < 0) width  = 0;
	if (height < 0) height = 0;
	Rdr().m_renderer.RenderTargetSize(pr::iv2::make(width, height));
	auto size = Rdr().m_renderer.RenderTargetSize();

	// Update the aspect ratio for all drawsets
	float aspect = (size.x == 0 || size.y == 0) ? 1.0f : size.x / float(size.y);
	for (auto ds : Rdr().m_drawset)
		ds->m_camera.Aspect(aspect);
}

// Get/Set the viewport within the render target
VIEW3D_API View3DViewport __stdcall View3D_Viewport()
{
	PR_ASSERT(PR_DBG, Dll().m_rdr, "");
	return reinterpret_cast<View3DViewport const&>(Rdr().m_scene.m_viewport);
}
VIEW3D_API void __stdcall View3D_SetViewport(View3DViewport vp)
{
	if (!Dll().m_rdr) return;
	Rdr().m_scene.m_viewport = reinterpret_cast<pr::rdr::Viewport const&>(vp);
}

// Render a drawset
VIEW3D_API void __stdcall View3D_Render(View3DDrawset drawset)
{
	if (!Dll().m_rdr) return;
	PR_ASSERT(PR_DBG, drawset != 0, "");
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
		pr::Scale4x4(Rdr().m_focus_point.m_i2w, scale, drawset->m_camera.FocusPoint());
		scene.AddInstance(Rdr().m_focus_point);
	}
	// Scale the origin point
	if (drawset->m_origin_point_visible)
	{
		float scale = drawset->m_origin_point_size * pr::Length3(drawset->m_camera.CameraToWorld().pos);
		pr::Scale4x4(Rdr().m_origin_point.m_i2w, scale, pr::v4Origin);
		scene.AddInstance(Rdr().m_origin_point);
	}

	{// Set the view and projection matrices
		pr::Camera& cam = drawset->m_camera;
		scene.SetView(cam);
	}

	// Set the light source
	auto& fr = scene.RdrStep<pr::rdr::ForwardRender>();
	fr.m_global_light = drawset->m_light;
	if (drawset->m_light_is_camera_relative)
	{
		pr::rdr::Light& light = fr.m_global_light;
		light.m_direction = drawset->m_camera.CameraToWorld() * drawset->m_light.m_direction;
		light.m_position  = drawset->m_camera.CameraToWorld() * drawset->m_light.m_position;
	}

	// Set the background colour
	fr.m_background_colour = drawset->m_background_colour;

	// Set the global fill mode
	switch (drawset->m_fill_mode) {
	case EView3DFillMode::Solid:     fr.m_rsb.Set(pr::rdr::ERS::FillMode, D3D11_FILL_SOLID); break;
	case EView3DFillMode::Wireframe: fr.m_rsb.Set(pr::rdr::ERS::FillMode, D3D11_FILL_WIREFRAME); break;
	case EView3DFillMode::SolidWire: fr.m_rsb.Set(pr::rdr::ERS::FillMode, D3D11_FILL_SOLID); break;
	}

	// Render the scene
	scene.Render();

	// Render wire frame over solid for 'SolidWire' mode
	if (drawset->m_fill_mode == EView3DFillMode::SolidWire)
	{
		fr.m_rsb.Set(pr::rdr::ERS::FillMode, D3D11_FILL_WIREFRAME);
		fr.m_bsb.Set(pr::rdr::EBS::BlendEnable, FALSE, 0);
		fr.m_clear_bb = false;

		scene.Render();

		fr.m_clear_bb = true;
		fr.m_rsb.Clear(pr::rdr::ERS::FillMode);
		fr.m_bsb.Clear(pr::rdr::EBS::BlendEnable, 0);
	}

	Rdr().m_renderer.Present();
}

// Get/Set the fill mode for a drawset
VIEW3D_API EView3DFillMode __stdcall View3D_FillMode(View3DDrawset drawset)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	return drawset->m_fill_mode;
}
VIEW3D_API void __stdcall View3D_SetFillMode(View3DDrawset drawset, EView3DFillMode mode)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	drawset->m_fill_mode = mode;
}

// Selected between perspective and orthographic projection
VIEW3D_API BOOL __stdcall View3D_Orthographic(View3DDrawset drawset)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	return drawset->m_camera.m_orthographic;
}
VIEW3D_API void __stdcall View3D_SetOrthographic(View3DDrawset drawset, BOOL render2d)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	drawset->m_camera.m_orthographic = render2d != 0;
}

// Get/Set the background colour for a drawset
VIEW3D_API int __stdcall View3D_BackgroundColour(View3DDrawset drawset)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	return drawset->m_background_colour;
}
VIEW3D_API void __stdcall View3D_SetBackgroundColour(View3DDrawset drawset, int aarrggbb)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	drawset->m_background_colour = pr::Colour32::make(aarrggbb);
}

// Show the measurement tool
VIEW3D_API BOOL __stdcall View3D_MeasureToolVisible()
{
	return Rdr().m_measure_tool_ui.IsWindowVisible();
}
VIEW3D_API void __stdcall View3D_ShowMeasureTool(View3DDrawset drawset, BOOL show)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	Rdr().m_measure_tool_ui.SetReadPointCtx(drawset);
	Rdr().m_measure_tool_ui.Show(show != 0);
}

// Show the angle tool
VIEW3D_API BOOL __stdcall View3D_AngleToolVisible()
{
	return Rdr().m_angle_tool_ui.IsWindowVisible();
}
VIEW3D_API void __stdcall View3D_ShowAngleTool(View3DDrawset drawset, BOOL show)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	Rdr().m_angle_tool_ui.SetReadPointCtx(drawset);
	Rdr().m_angle_tool_ui.Show(show != 0);
}

// Create a scene showing the capabilities of view3d (actually of ldr_object_manager)
VIEW3D_API void __stdcall View3D_CreateDemoScene(View3DDrawset drawset)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");

	size_t initial = Rdr().m_obj_cont.size();
	pr::ldr::AddString(Rdr().m_renderer, pr::ldr::CreateDemoScene().c_str(), Rdr().m_obj_cont, pr::ldr::DefaultContext, true, 0, &Rdr().m_lua);
	size_t final = Rdr().m_obj_cont.size();
	for (size_t i = initial; i != final; ++i)
	{
		View3D_DrawsetAddObject(drawset, Rdr().m_obj_cont[i].m_ptr);
	}
}

// Show a window containing the demo scene script
VIEW3D_API void __stdcall View3D_ShowDemoScript()
{
	Rdr().m_obj_cont_ui.ShowScript(pr::ldr::CreateDemoScene(), 0);
}

// Return true if the focus point is visible
VIEW3D_API BOOL __stdcall View3D_FocusPointVisible(View3DDrawset drawset)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	return drawset->m_focus_point_visible;
}

// Add the focus point to a drawset
VIEW3D_API void __stdcall View3D_ShowFocusPoint(View3DDrawset drawset, BOOL show)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	drawset->m_focus_point_visible = show != 0;
}

// Set the size of the focus point
VIEW3D_API void __stdcall View3D_SetFocusPointSize(View3DDrawset drawset, float size)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	drawset->m_focus_point_size = size;
}

// Return true if the origin is visible
VIEW3D_API BOOL __stdcall View3D_OriginVisible(View3DDrawset drawset)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	return drawset->m_origin_point_visible;
}

// Add the focus point to a drawset
VIEW3D_API void __stdcall View3D_ShowOrigin(View3DDrawset drawset, BOOL show)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	drawset->m_origin_point_visible = show != 0;
}

// Set the size of the focus point
VIEW3D_API void __stdcall View3D_SetOriginSize(View3DDrawset drawset, float size)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	drawset->m_origin_point_size = size;
}

// Display the object manager ui
VIEW3D_API void __stdcall View3D_ShowObjectManager(BOOL show)
{
	Rdr().m_obj_cont_ui.Show(show != 0);
}

// Parse an ldr *o2w {} description returning the transform
VIEW3D_API pr::m4x4 __stdcall View3D_ParseLdrTransform(char const* ldr_script)
{
	try
	{
		pr::script::Reader reader;
		pr::script::PtrSrc src(ldr_script);
		reader.AddSource(src);
		return pr::ldr::ParseLdrTransform(reader);
	}
	catch (std::exception const& e)
	{
		Rdr().m_error_cb(e.what());
		return pr::m4x4Identity;
	}
}
