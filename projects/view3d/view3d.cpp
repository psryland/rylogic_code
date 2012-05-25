//***************************************************************************************************
// View 3D
//  Copyright © Rylogic Ltd 2009
//***************************************************************************************************
	
#include "stdafx.h"
#include "view3d/renderer_instance.h"
#include "pr/renderer/lighting/lighting_dlg.h"
#include "pr/common/assert.h"
#include "pr/common/array.h"
#include "pr/filesys/fileex.h"
#include "pr/script/reader.h"
#include "pr/view3d/view3d.h"
	
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
		AtlInitCommonControls(ICC_BAR_CLASSES);	// add flags to support other controls
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
	
// Convert the public interface 'EModelType' into the renderers primitive type
inline pr::rdr::model::EPrimitive::Type ModelTypeToPrimType(EView3DPrim::Type type)
{
	switch (type)
	{
	default: PR_ASSERT(PR_DBG, false, ""); return pr::rdr::model::EPrimitive::PointList;
	case EView3DPrim::PointList:                  return pr::rdr::model::EPrimitive::PointList;
	case EView3DPrim::LineList:                   return pr::rdr::model::EPrimitive::LineList;
	case EView3DPrim::TriangleList:               return pr::rdr::model::EPrimitive::TriangleList;
	}
}
	
// Initialise the dll
VIEW3D_API EView3DResult::Type __stdcall View3D_Initialise(HWND hwnd, pr::uint d3dcreate_flags, View3D_ReportErrorCB error_cb, View3D_SettingsChanged settings_changed_cb)
{
	try
	{
		if (Dll().m_rdr == 0)
		{
			pr::rdr::CheckDependencies();
			g_dll_data->m_rdr = new RendererInstance(hwnd, D3DDEVTYPE_HAL, d3dcreate_flags, error_cb, settings_changed_cb);
			Rdr().CreateStockObjects();
		}
		
		return EView3DResult::Success;
	}
	catch (std::exception const& e)
	{
		error_cb(pr::FmtS("Failed to initialise D3D.\nReason: %s\n", e.what()));
		return EView3DResult::Failed;
	}
	catch (...)
	{
		error_cb("Failed to initialise D3D.\nReason: An unknown exception occurred\n");
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
	out << "*SceneSettings {" << Rdr().m_scene_ui.Settings() << "}\n";
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
				Rdr().m_scene_ui.Settings(desc.c_str());
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
VIEW3D_API EView3DResult::Type __stdcall View3D_DrawsetCreate(View3DDrawset& drawset)
{
	drawset = new Drawset();
	Rdr().m_drawset.insert(drawset);

	// Set the initial aspect ratio
	pr::IRect client_area = Rdr().m_renderer.ClientArea();
	float aspect = client_area.SizeX() / float(client_area.SizeY());
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
	for (std::size_t i = 0, iend = Rdr().m_scene.size(); i != iend; ++i)
		if (Rdr().m_scene[i]->m_context_id == context_id)
			View3D_DrawsetAddObject(drawset, Rdr().m_scene[i].m_ptr);
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
	pr::BoundingBox bbox = pr::BBoxReset;
	for (ObjectCont::const_iterator i = drawset->m_objects.begin(), iend = drawset->m_objects.end(); i != iend; ++i)
		pr::Encompase(bbox, (*i)->BBoxWS(true));
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
	
// Convert a screen space point into a position and direction in world space
// 'screen' should be in normalised screen space, i.e. (-1,-1)->(1,1) (lower left to upper right)
VIEW3D_API void __stdcall View3D_WSRayFromScreenPoint(View3DDrawset drawset, pr::v2 screen, pr::v4& ws_point, pr::v4& ws_direction)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	drawset->m_camera.WSRayFromScreenPoint(pr::v4::make(screen, 1.0f, 0.0f), ws_point, ws_direction);
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
VIEW3D_API EView3DResult::Type __stdcall View3D_ObjectsCreateFromFile(char const* ldr_filepath, int context_id, BOOL async)
{
	try
	{
		pr::ldr::AddFile(Rdr().m_renderer, ldr_filepath, Rdr().m_scene, context_id, async != 0, 0, &Rdr().m_lua);
		return EView3DResult::Success;
	}
	catch (std::exception const& ex)
	{
		Rdr().m_error_cb(ex.what());
		return EView3DResult::Failed;
	}
}
	
// If multiple objects are created, the handle returned is to the last object only
VIEW3D_API EView3DResult::Type __stdcall View3D_ObjectCreateLdr(char const* ldr_script, int context_id, View3DObject& object, BOOL async)
{
	try
	{
		object = 0;
		size_t initial = Rdr().m_scene.size();
		pr::ldr::AddString(Rdr().m_renderer, ldr_script, Rdr().m_scene, context_id, async != 0, 0, &Rdr().m_lua);
		size_t final = Rdr().m_scene.size();
		if (initial == final) return EView3DResult::Failed;
		object = Rdr().m_scene.back().m_ptr;
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
	
	// If the model already has nuggets grab some defaults from it
	pr::rdr::Material mat;
	if (!model->m_render_nugget.empty())
		mat = model->m_render_nugget.front().m_material;
	else
		mat = rdr.m_mat_mgr.GetMaterial(pr::geom::EVNCT);
	
	pr::rdr::model::MLock mlock(model);
	
	// Create buffers to be filled by the user callback
	pr::Array<View3DVertex>   verts   (mlock.m_vrange.size());
	pr::Array<pr::rdr::Index> indices (mlock.m_irange.size());
	std::size_t vcount = verts.size();
	std::size_t icount = indices.size();
	EView3DPrim::Type model_type = EView3DPrim::Invalid;

	// Create a material for the user to modify
	View3DMaterial v3dmat;
	v3dmat.m_geom_type = mat.m_effect->m_geom_type;
	v3dmat.m_diff_tex  = mat.m_diffuse_texture.m_ptr;
	v3dmat.m_env_map   = mat.m_envmap_texture.m_ptr;

	// Get the user to generate the model
	cbdata.edit_cb(vcount, icount, &verts[0], &indices[0], vcount, icount, model_type, v3dmat, cbdata.ctx);

	PR_ASSERT(PR_DBG, vcount <= mlock.m_vrange.size(), "");
	PR_ASSERT(PR_DBG, icount <= mlock.m_irange.size(), "");
	PR_ASSERT(PR_DBG, model_type != EView3DPrim::Invalid, "");
	
	model->m_bbox.reset();

	// Copy the model data into the model
	pr::Array<View3DVertex>::const_iterator vin = verts.begin();
	pr::rdr::vf::iterator vout = mlock.VPtr();
	for (std::size_t i = 0; i != vcount; ++i, ++vin, ++vout)
	{
		vout->set(vin->pos, vin->norm, pr::Colour32::make(vin->col), vin->tex);
		pr::Encompase(model->m_bbox, vin->pos);
	}
	pr::Array<pr::rdr::Index>::const_iterator iin = indices.begin();
	pr::rdr::Index* iout = mlock.IPtr();
	for (std::size_t i = 0; i != icount; ++i, ++iin, ++iout)
		*iout = *iin;
	
	// Update the material
	if (v3dmat.m_geom_type != mat.m_effect->m_geom_type) mat.m_effect = rdr.m_mat_mgr.GetEffect(v3dmat.m_geom_type);
	mat.m_diffuse_texture = v3dmat.m_diff_tex;
	mat.m_envmap_texture  = v3dmat.m_env_map;
	
	// Re-create the render nuggets
	mlock.m_vrange.resize(vcount);
	mlock.m_irange.resize(icount);
	model->SetMaterial(mat, static_cast<pr::rdr::model::EPrimitive::Type>(model_type), true, &mlock.m_vrange, &mlock.m_irange);
}
	
// Create an object via callback
VIEW3D_API EView3DResult::Type __stdcall View3D_ObjectCreate(char const* name, pr::uint colour, int icount, int vcount, View3D_EditObjectCB edit_cb, void* ctx, int context_id, View3DObject& object)
{
	try
	{
		object = 0;
		ObjectEditCBData cbdata = {edit_cb, ctx};
		pr::ldr::LdrObjectPtr obj = pr::ldr::Add(Rdr().m_renderer, pr::ldr::ObjectAttributes(pr::ldr::ELdrObject::Custom, name, pr::Colour32::make(colour)), icount, vcount, ObjectEditCB, &cbdata, context_id);
		if (!obj) return EView3DResult::Failed;
		Rdr().m_scene.push_back(obj);
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
	pr::ldr::Remove(Rdr().m_scene, &context_id, 1, 0, 0);
}
	
// Delete an object
VIEW3D_API void __stdcall View3D_ObjectDelete(View3DObject object)
{
	if (!object) return;
	for (DrawsetCont::iterator i = Rdr().m_drawset.begin(), iend = Rdr().m_drawset.end(); i != iend; ++i)
		View3D_DrawsetRemoveObject(*i, object);
	pr::ldr::Remove(Rdr().m_scene, object);
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
	for (pr::rdr::TNuggetChain::iterator i = object->m_model->m_render_nugget.begin(), iend = object->m_model->m_render_nugget.end(); i != iend; ++i)
		i->m_material.m_diffuse_texture = tex;
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
VIEW3D_API EView3DResult::Type __stdcall View3D_TextureCreate(void const* data, pr::uint data_size, pr::uint width, pr::uint height, pr::uint mips, pr::uint format, View3DTexture& tex)
{
	try
	{
		pr::rdr::TexturePtr t = Rdr().m_renderer.m_mat_mgr.CreateTexture(pr::rdr::AutoId, data, data_size, width, height, mips, 0, (D3DFORMAT)format);
		tex = t.m_ptr; t.m_ptr = 0; // rely on the caller for correct reference counting
		return EView3DResult::Success;
	}
	catch (pr::RdrException const&) { return EView3DResult::Failed; }
}
	
// Load a texture from file
// Specify width == 0, height == 0 to use the dimensions of the file
VIEW3D_API EView3DResult::Type __stdcall View3D_TextureCreateFromFile(char const* tex_filepath, pr::uint width, pr::uint height, pr::uint mips, pr::uint filter, pr::uint mip_filter, pr::uint colour_key, View3DTexture& tex)
{
	try
	{
		pr::rdr::TexturePtr t = Rdr().m_renderer.m_mat_mgr.CreateTexture(pr::rdr::AutoId, tex_filepath, width, height, mips, colour_key, filter, mip_filter);
		tex = t.m_ptr; t.m_ptr = 0; // rely on the caller for correct reference counting
		return EView3DResult::Success;
	}
	catch (pr::RdrException const&) { return EView3DResult::Failed; }
}
	
// Load a texture surface from file
VIEW3D_API EView3DResult::Type __stdcall View3D_TextureLoadSurface(View3DTexture tex, int level, char const* tex_filepath, RECT const* dst_rect, RECT const* src_rect, pr::uint filter, pr::uint colour_key)
{
	try
	{
		tex->LoadSurfaceFromFile(tex_filepath, level, dst_rect, src_rect, filter, colour_key);
		return EView3DResult::Success;
	}
	catch (pr::RdrException const&) { return EView3DResult::Failed; }
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
	pr::rdr::TexInfo tex_info = tex->m_info;
	info.m_width             = tex_info.Width;
	info.m_height            = tex_info.Height;
	info.m_depth             = tex_info.Depth;
	info.m_mips              = tex_info.MipLevels;
	info.m_format            = tex_info.Format;
	info.m_image_file_format = tex_info.ImageFileFormat;
}
	
// Read the properties of an image file
VIEW3D_API EView3DResult::Type __stdcall View3D_TextureGetInfoFromFile(char const* tex_filepath, View3DImageInfo& info)
{
	D3DXIMAGE_INFO tex_info;
	if (pr::Failed(Rdr().m_renderer.m_mat_mgr.TextureInfo(tex_filepath, tex_info)))
		return EView3DResult::Failed;

	info.m_width             = tex_info.Width;
	info.m_height            = tex_info.Height;
	info.m_depth             = tex_info.Depth;
	info.m_mips              = tex_info.MipLevels;
	info.m_format            = tex_info.Format;
	info.m_image_file_format = tex_info.ImageFileFormat;
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
	
// Resize the viewport
VIEW3D_API void __stdcall View3D_Resize(int width, int height)
{
	if (!Dll().m_rdr) return;
	if (width  < 0) width  = 0;
	if (height < 0) height = 0;
	Rdr().m_renderer.Resize(pr::IRect::make(0, 0, width, height));

	// Update the aspect ratio for all drawsets
	float aspect = (width == 0 || height == 0) ? 1.0f : width / float(height);
	for (DrawsetCont::iterator i = Rdr().m_drawset.begin(), i_end = Rdr().m_drawset.end(); i != i_end; ++i)
		(*i)->m_camera.Aspect(aspect);
}
	
// Render a drawset
VIEW3D_API void __stdcall View3D_Render(View3DDrawset drawset)
{
	if (!Dll().m_rdr) return;
	PR_ASSERT(PR_DBG, drawset != 0, "");
	Rdr().m_last_drawset = drawset;

	// Reset the drawlist
	Rdr().m_viewport.ClearDrawlist();

	// Add objects from the drawset to the viewport
	for (ObjectCont::const_iterator i = drawset->m_objects.begin(), iend = drawset->m_objects.end(); i != iend; ++i)
		(*i)->AddToViewport(Rdr().m_viewport);

	// Add the measure tool objects if the window is visible
	if (Rdr().m_measure_tool_ui.IsWindowVisible() && Rdr().m_measure_tool_ui.Gfx())
		Rdr().m_measure_tool_ui.Gfx()->AddToViewport(Rdr().m_viewport);;
	
	// Add the angle tool objects if the window is visible
	if (Rdr().m_angle_tool_ui.IsWindowVisible() && Rdr().m_angle_tool_ui.Gfx())
		Rdr().m_angle_tool_ui.Gfx()->AddToViewport(Rdr().m_viewport);;

	// Position the focus point
	if (drawset->m_focus_point_visible)
	{
		float scale = drawset->m_focus_point_size * drawset->m_camera.FocusDist();
		pr::Scale4x4(Rdr().m_focus_point.m_i2w, scale, drawset->m_camera.FocusPoint());
		Rdr().m_viewport.AddInstance(Rdr().m_focus_point);
	}
	// Scale the origin point
	if (drawset->m_origin_point_visible)
	{
		float scale = drawset->m_origin_point_size * pr::Length3(drawset->m_camera.CameraToWorld().pos);
		pr::Scale4x4(Rdr().m_origin_point.m_i2w, scale, pr::v4Origin);
		Rdr().m_viewport.AddInstance(Rdr().m_origin_point);
	}

	{// Set the view and projection matrices
		pr::Camera& cam = drawset->m_camera;
		Rdr().m_viewport.SetView(cam.m_fovY, cam.m_aspect, cam.m_focus_dist, cam.m_orthographic);
		Rdr().m_viewport.CameraToWorld(cam.CameraToWorld());
	}

	// Set the light source
	Rdr().m_renderer.m_light_mgr.m_light[0] = drawset->m_light;
	if (drawset->m_light_is_camera_relative)
	{
		pr::rdr::Light& light = Rdr().m_renderer.m_light_mgr.m_light[0];
		light.m_direction = drawset->m_camera.CameraToWorld() * drawset->m_light.m_direction;
		light.m_position  = drawset->m_camera.CameraToWorld() * drawset->m_light.m_position;
	}

	// Set the background colour
	Rdr().m_renderer.BackgroundColour(drawset->m_background_colour);
	
	// Render the viewport
	if (pr::Succeeded(Rdr().m_renderer.RenderStart()))
	{
		// Set the global wireframe mode
		switch (drawset->m_render_mode)
		{
		default:break;
		case EView3DRenderMode::Solid:    Rdr().m_viewport.RenderState(D3DRS_FILLMODE, D3DFILL_SOLID); break;
		case EView3DRenderMode::Wireframe:Rdr().m_viewport.RenderState(D3DRS_FILLMODE, D3DFILL_WIREFRAME); break;
		case EView3DRenderMode::SolidWire:Rdr().m_viewport.RenderState(D3DRS_FILLMODE, D3DFILL_SOLID); break;
		}

		// Render the viewport
		Rdr().m_viewport.Render();

		// Render wireframe over the top of the solid
		if (drawset->m_render_mode == EView3DRenderMode::SolidWire)
		{
			pr::rdr::rs::Block rsb_override;
			rsb_override.SetRenderState(D3DRS_ALPHABLENDENABLE, FALSE);
			rsb_override.SetRenderState(D3DRS_FILLMODE, D3DFILL_WIREFRAME);
			Rdr().m_viewport.Render(false, rsb_override);
		}

		// Present
		Rdr().m_renderer.RenderEnd();
		Rdr().m_renderer.Present();
	}
}
	
// Get/Set the render mode for a drawset
VIEW3D_API EView3DRenderMode::Type __stdcall View3D_RenderMode(View3DDrawset drawset)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	return drawset->m_render_mode;
}
VIEW3D_API void __stdcall View3D_SetRenderMode(View3DDrawset drawset, EView3DRenderMode::Type mode)
{
	PR_ASSERT(PR_DBG, drawset != 0, "");
	drawset->m_render_mode = mode;
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
VIEW3D_API int __stdcall View3D_BackGroundColour(View3DDrawset drawset)
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
	
	size_t initial = Rdr().m_scene.size();
	pr::ldr::AddString(Rdr().m_renderer, pr::ldr::CreateDemoScene().c_str(), Rdr().m_scene);
	size_t final = Rdr().m_scene.size();
	for (size_t i = initial; i != final; ++i)
	{
		View3D_DrawsetAddObject(drawset, Rdr().m_scene[i].m_ptr);
	}
}
	
// Show a window containing the demo scene script
VIEW3D_API void __stdcall View3D_ShowDemoScript()
{
	Rdr().m_scene_ui.ShowScript(pr::ldr::CreateDemoScene(), 0);
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
	Rdr().m_scene_ui.Show(show != 0);
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

