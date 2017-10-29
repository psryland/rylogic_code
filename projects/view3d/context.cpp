//***************************************************************************************************
// View 3D
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************

#include "view3d/forward.h"
#include "view3d/context.h"
#include "view3d/window.h"
#include "pr/view3d/view3d.h"

using namespace pr::rdr;
using namespace pr::ldr;
using namespace pr::script;

namespace view3d
{
	pr::Guid const Context::GuidDemoSceneObjects = { 0xFE51C164, 0x9E57, 0x456F, 0x9D, 0x8D, 0x39, 0xE3, 0xFA, 0xAF, 0xD3, 0xE7 };

	// Constructor
	Context::Context(HINSTANCE instance, BOOL bgra_compatible)
		:m_inits()
		,m_compatible(TestSystemCompatibility())
		,m_rdr(RdrSettings(instance, bgra_compatible))
		,m_wnd_cont()
		,m_sources(m_rdr, This())
		,m_mutex()
	{
		PR_ASSERT(PR_DBG, pr::meta::is_aligned_to<16>(this), "dll data not aligned");

		// Hook up the sources events
		m_sources.OnAddFileProgress += [&](ScriptSources&, ScriptSources::AddFileProgressEventArgs& args)
		{
			auto context_id  = args.m_context_id;
			auto filepath    = args.m_loc.StreamName();
			auto file_offset = args.m_loc.Pos();
			auto complete    = args.m_complete;
			OnAddFileProgress.Raise(args.m_cancel, context_id, filepath.c_str(), file_offset, complete);
		};
		m_sources.OnReload += [&](ScriptSources&, pr::EmptyArgs const&)
		{
			OnSourcesChanged.Raise(ESourcesChangedReason::Reload, true);
		};
		m_sources.OnSourceRemoved += [&](ScriptSources&, ScriptSources::SourceRemovedEventArgs const& args)
		{
			// When a source is about to be removed, remove it's objects from the windows
			auto reload = args.m_reason == ScriptSources::EReason::Reload;
			for (auto& wnd : m_wnd_cont)
				wnd->RemoveObjectsById(&args.m_context_id, 1, false, reload);
		};
		m_sources.OnStoreChanged += [&](ScriptSources&, ScriptSources::StoreChangedEventArgs const& args)
		{
			auto newdata = args.m_reason == ScriptSources::EReason::NewData;
			auto removal = args.m_reason == ScriptSources::EReason::Removal;
			for (auto& wnd : m_wnd_cont)
			{
				// For each window, get the intersection of the window's context ids with the ids that have changed.
				auto ids = pr::set_intersection<GuidCont>(wnd->m_guids, args.m_context_ids);
				if (ids.empty()) continue;

				// If not new data added, remove old objects
				if (!newdata)
					wnd->RemoveObjectsById(ids.data(), int(ids.size()), false, !removal);

				// If reloading or adding new data, add objects from the context ids
				if (!removal)
					wnd->AddObjectsById(ids.data(), int(ids.size()), false);
			}

			OnSourcesChanged.Raise(static_cast<ESourcesChangedReason>(args.m_reason), false);
		};
		m_sources.OnError += [&](ScriptSources&, pr::ErrorEventArgs const& args)
		{
			ReportError(args.m_msg.c_str());
		};

		// Create code handlers
		EmbeddedCodeHandlers.push_back(std::make_unique<EmbeddedLua>());
	}

	// Report an error to the global error handler
	void Context::ReportError(wchar_t const* msg)
	{
		OnError.Raise(msg);
	}

	// Create/Destroy windows
	Window* Context::WindowCreate(HWND hwnd, View3DWindowOptions const& opts)
	{
		try
		{
			Window* win;
			m_wnd_cont.emplace_back(win = new Window(hwnd, this, opts));
			return win;
		}
		catch (std::exception const& e)
		{
			if (opts.m_error_cb) opts.m_error_cb(opts.m_error_cb_ctx, pr::FmtS(L"Failed to create View3D Window.\n%S", e.what()));
			return nullptr;
		}
		catch (...)
		{
			if (opts.m_error_cb) opts.m_error_cb(opts.m_error_cb_ctx, pr::FmtS(L"Failed to create View3D Window.\nUnknown reason"));
			return nullptr;
		}
	}
	void Context::WindowDestroy(Window* window)
	{
		pr::erase_first(m_wnd_cont, [=](auto& wnd){ return wnd.get() == window; });
	}

	// Remove all Ldr script sources
	void Context::ClearScriptSources()
	{
		// Reset the sources collection
		m_sources.ClearAll();
	}

	// Load/Add a script source. Returns the Guid of the context that the objects were added to.
	pr::Guid Context::LoadScriptSource(wchar_t const* filepath, bool additional, Includes const& includes)
	{
		// Note: this can be called from a worker thread
		return m_sources.AddFile(filepath, includes, additional);
	}

	// Load/Add ldr objects from a script string. Returns the Guid of the context that the objects were added to.
	pr::Guid Context::LoadScript(wchar_t const* ldr_script, bool file, pr::Guid const* context_id, Includes const& includes)
	{
		// Note: this can be called from a worker thread
		return m_sources.AddScript(ldr_script, file, context_id, includes);
	}

	// Load/Add ldr objects and return the first object from the script
	LdrObject* Context::ObjectCreateLdr(wchar_t const* ldr_script, bool file, pr::Guid const* context_id, pr::script::Includes const& includes)
	{
		// Get the context id for this script
		auto id = context_id ? *context_id : pr::GenerateGUID();

		// Record how many objects there are already for the context id (if it exists)
		auto& srcs = m_sources.Sources();
		auto iter = srcs.find(id);
		auto count = iter != std::end(srcs) ? iter->second.m_objects.size() : 0U;

		// Load the ldr script
		LoadScript(ldr_script, file != 0, &id, includes);

		// Return the first object. expecting 'ldr_script' to define one object only.
		// It doesn't matter if more are defined however, they're just created as part of the context
		iter = srcs.find(id);
		return iter != std::end(srcs) && iter->second.m_objects.size() > count
			? iter->second.m_objects[count].get()
			: nullptr;
	}

	// Create an object from geometry
	LdrObject* Context::ObjectCreate(char const* name, pr::Colour32 colour, int vcount, int icount, int ncount, View3DVertex const* verts, pr::uint16 const* indices, View3DNugget const* nuggets, pr::Guid const& context_id)
	{
		// Strata the vertex data
		pr::vector<pr::rdr::NuggetProps> ngt;
		pr::vector<pr::v4>       pos;
		pr::vector<pr::Colour32> col;
		pr::vector<pr::v4>       nrm;
		pr::vector<pr::v2>       tex;
		for (auto n = nuggets, nend = n + ncount; n != nend; ++n)
		{
			// Create the renderer nugget
			NuggetProps nug;
			nug.m_topo = static_cast<EPrim>(n->m_topo);
			nug.m_geom = static_cast<EGeom>(n->m_geom);
			nug.m_vrange = n->m_v0 != n->m_v1 ? Range(n->m_v0, n->m_v1) : Range(0, vcount);
			nug.m_irange = n->m_i0 != n->m_i1 ? Range(n->m_i0, n->m_i1) : Range(0, icount);
			nug.m_geometry_has_alpha = n->m_has_alpha != 0;
			nug.m_tex_diffuse = Texture2DPtr(n->m_mat.m_diff_tex, true);
			switch (n->m_mat.m_shader)
			{
			default: break;
			case EView3DShader::ThickLineListGS:
				{
					auto line_width = n->m_mat.m_shader_data[0];
					PR_ASSERT(PR_DBG, line_width != 0, "The thick line shader requires a non-zero line width");
					
					auto shdr = m_rdr.m_shdr_mgr.GetShader<pr::rdr::ThickLineListGS>(pr::FmtS("thick_line_%f", line_width), EStockShader::ThickLineListGS);
					shdr->m_width = static_cast<float>(line_width);
					nug.m_smap[ERenderStep::ForwardRender].m_gs = shdr;
					break;
				}
			}
			ngt.push_back(nug);

			// Sanity check the nugget
			PR_ASSERT(PR_DBG, nug.m_vrange.begin() <= nug.m_vrange.end() && int(nug.m_vrange.end()) <= vcount, "Invalid nugget V-range");
			PR_ASSERT(PR_DBG, nug.m_irange.begin() <= nug.m_irange.end() && int(nug.m_irange.end()) <= icount, "Invalid nugget I-range");

			// Vertex positions
			{
				size_t j = pos.size();
				pos.resize(pos.size() + nug.m_vrange.size());
				for (auto i = nug.m_vrange.begin(); i != nug.m_vrange.end(); ++i)
					pos[j++] = view3d::To<pr::v4>(verts[i].pos);
			}
			// Colours
			if (pr::AllSet(nug.m_geom, EGeom::Colr))
			{
				size_t j = col.size();
				col.resize(col.size() + nug.m_vrange.size());
				for (auto i = nug.m_vrange.begin(); i != nug.m_vrange.end(); ++i)
					col[j++] = verts[i].col;
			}
			// Normals
			if (pr::AllSet(nug.m_geom, EGeom::Norm))
			{
				size_t j = nrm.size();
				nrm.resize(nrm.size() + nug.m_vrange.size());
				for (auto i = nug.m_vrange.begin(); i != nug.m_vrange.begin(); ++i)
					nrm[j++] = view3d::To<pr::v4>(verts[i].norm);
			}
			// Texture coords
			if (pr::AllSet(nug.m_geom, EGeom::Tex0))
			{
				size_t j = tex.size();
				tex.resize(tex.size() + nug.m_vrange.size());
				for (auto i = nug.m_vrange.begin(); i != nug.m_vrange.end(); ++i)
					tex[j++] = view3d::To<pr::v2>(verts[i].tex);
			}
		}

		// Create the model
		auto attr  = pr::ldr::ObjectAttributes(pr::ldr::ELdrObject::Custom, name, pr::Colour32(colour));
		auto cdata = pr::ldr::MeshCreationData()
			.verts  (pos.data(), int(pos.size()))
			.indices(indices,    icount)
			.nuggets(ngt.data(), int(ngt.size()))
			.colours(col.data(), int(col.size()))
			.normals(nrm.data(), int(nrm.size()))
			.tex    (tex.data(), int(tex.size()));
		auto obj = pr::ldr::Create(m_rdr, attr, cdata, context_id);
	
		// Add to the sources
		if (obj)
			m_sources.Add(obj);

		// Return the created object
		return obj.get();
	}

	// Reload file sources
	void Context::ReloadScriptSources()
	{
		m_sources.ReloadFiles();
	}

	// Poll for changed script source files, and reload any that have changed
	void Context::CheckForChangedSources()
	{
		m_sources.RefreshChangedFiles();
	}

	// Edit the geometry of a model after it has been allocated
	void Context::EditObject(LdrObject* object, View3D_EditObjectCB edit_cb, void* ctx)
	{
		// Remove the object from any windows it might be in
		for (auto& wnd : m_wnd_cont)
			wnd->Remove(object);

		// Callback to edit the geometry
		ObjectEditCBData cbdata = { edit_cb, ctx };
		Edit(m_rdr, object, ObjectEditCB, &cbdata);
	}

	// Update the model in an existing object
	void Context::UpdateObject(LdrObject* object, wchar_t const* ldr_script, EUpdateObject flags)
	{
		// Remove the object from any windows it might be in
		for (auto& wnd : m_wnd_cont)
			wnd->Remove(object);

		// Update the object model
		PtrW src(ldr_script);
		Reader reader(src, false);
		Update(m_rdr, object, reader, flags);
	}

	// Delete all objects
	void Context::DeleteAllObjects()
	{
		// Remove the objects from any windows they're in
		for (auto& wnd : m_wnd_cont)
			wnd->RemoveAllObjects();
		
		// Clear the object container. The unique pointers should delete the objects
		m_sources.ClearAll();
	}

	// Delete all objects with matching ids
	void Context::DeleteAllObjectsById(pr::Guid const* context_ids, int count)
	{
		// Remove objects from any windows they might be assigned to
		for (auto& wnd : m_wnd_cont)
			wnd->RemoveObjectsById(context_ids, count, false, false);

		// Clear sources associated with 'context_ids'
		for (auto& id : std::initializer_list<pr::Guid>(context_ids, context_ids+count))
			m_sources.Remove(id);
	}

	// Delete a single object
	void Context::DeleteObject(LdrObject* object)
	{
		// Remove the object from any windows it's in
		for (auto& wnd : m_wnd_cont)
			wnd->Remove(object);
		
		// Delete the object from the object container
		m_sources.Remove(object);
	}

	// Return the context id for objects created from 'filepath' (if filepath is an existing source)
	pr::Guid const* Context::ContextIdFromFilepath(wchar_t const* filepath)
	{
		return m_sources.ContextIdFromFilepath(filepath);
	}

	// Enumerate the Guids in the sources collection
	void Context::SourceEnumGuids(View3D_EnumGuidsCB enum_guids_cb, void* ctx)
	{
		for (auto& src : m_sources.Sources())
			enum_guids_cb(ctx, src.second.m_context_id);
	}

	// Create a gizmo object and add it to the gizmo collection
	LdrGizmo* Context::CreateGizmo(LdrGizmo::EMode mode, pr::m4x4 const& o2w)
	{
		return m_sources.CreateGizmo(mode, o2w);
	}

	// Destroy a gizmo
	void Context::DeleteGizmo(LdrGizmo* gizmo)
	{
		// Remove the gizmo from any windows it's in
		for (auto& wnd : m_wnd_cont)
			wnd->Remove(gizmo);
		
		// Delete the gizmo from the sources
		m_sources.RemoveGizmo(gizmo);
	}

	// Callback function called from CreateEditCB to populate the model data
	void Context::ObjectEditCB(ModelPtr model, void* ctx, pr::Renderer&)
	{
		using namespace pr::rdr;

		PR_ASSERT(PR_DBG, model != nullptr, "");
		if (!model) throw std::exception("model is null");
		auto& cbdata = *static_cast<ObjectEditCBData*>(ctx);

		// Create buffers to be filled by the user callback
		// Note: We can't fill the buffers with the existing model data because that requires
		// reading from video memory (slow, or not possible for some model types).
		auto vrange = model->m_vrange;
		auto irange = model->m_irange;
		pr::vector<View3DVertex> verts(vrange.size());
		pr::vector<pr::uint16>   indices(irange.size());
		pr::vector<View3DNugget> nuggets;

		// If the model already has nuggets, initialise 'nuggets' with them
		if (!model->m_nuggets.empty())
		{
			for (auto& nug : model->m_nuggets)
			{
				View3DNugget n = {};
				n.m_topo = static_cast<EView3DPrim>(nug.m_topo);
				n.m_geom = static_cast<EView3DGeom>(nug.m_geom);
				n.m_v0 = pr::s_cast<UINT32>(nug.m_vrange.begin());
				n.m_v1 = pr::s_cast<UINT32>(nug.m_vrange.end());
				n.m_i0 = pr::s_cast<UINT32>(nug.m_irange.begin());
				n.m_i1 = pr::s_cast<UINT32>(nug.m_irange.end());
				n.m_mat.m_diff_tex = nug.m_tex_diffuse.m_ptr;
				n.m_mat.m_env_map = nullptr;
				nuggets.push_back(n);
			}
		}
		else
		{
			nuggets.push_back(View3DNugget());
		}

		// Get the user to generate/update the model
		UINT32 new_vcount, new_icount, new_ncount;
		cbdata.edit_cb(cbdata.ctx, UINT32(vrange.size()), UINT32(irange.size()), UINT32(nuggets.size()), &verts[0], &indices[0], &nuggets[0], new_vcount, new_icount, new_ncount);
		PR_ASSERT(PR_DBG, new_vcount <= vrange.size(), "");
		PR_ASSERT(PR_DBG, new_icount <= irange.size(), "");
		PR_ASSERT(PR_DBG, new_ncount <= nuggets.size(), "");

		{// Lock and update the model
			MLock mlock(model, D3D11_MAP_WRITE_DISCARD);
			model->m_bbox.reset();

			// Copy the model data into the model
			auto vin = std::begin(verts);
			auto vout = mlock.m_vlock.ptr<Vert>();
			for (size_t i = 0; i != new_vcount; ++i, ++vin)
			{
				SetPCNT(*vout++, view3d::To<pr::v4>(vin->pos), pr::Colour32(vin->col), view3d::To<pr::v4>(vin->norm), view3d::To<pr::v2>(vin->tex));
				pr::Encompass(model->m_bbox, view3d::To<pr::v4>(vin->pos));
			}
			auto iin = std::begin(indices);
			auto iout = mlock.m_ilock.ptr<pr::uint16>();
			for (size_t i = 0; i != new_icount; ++i, ++iin)
			{
				*iout++ = *iin;
			}
		}
		{// Update the model nuggets
			model->DeleteNuggets();

			for (auto& nug : nuggets)
			{
				NuggetProps mat;
				mat.m_topo = static_cast<EPrim>(nug.m_topo);
				mat.m_geom = static_cast<EGeom>(nug.m_geom);
				mat.m_vrange = vrange;
				mat.m_irange = irange;
				mat.m_vrange.resize(new_vcount);
				mat.m_irange.resize(new_icount);
				mat.m_tex_diffuse = Texture2DPtr(nug.m_mat.m_diff_tex, true);
				model->CreateNugget(mat);
			}
		}
	}

	// Create the demo scene objects
	pr::Guid Context::CreateDemoScene(Window* window)
	{
		// Get the string of all ldr objects
		auto scene = pr::ldr::CreateDemoScene();

		// Add the demo objects to the sources
		m_sources.AddScript(scene.c_str(), false, &GuidDemoSceneObjects, Includes());

		// Add the demo objects to 'window'
		window->AddObjectsById(&GuidDemoSceneObjects, 1, false);

		// Position the camera to look at the scene
		View3D_ResetView(window, View3DV4{0.0f, 0.0f, -1.0f, 0.0f}, View3DV4{0.0f, 1.0f, 0.0f, 0.0f}, 0, TRUE, TRUE);
		return GuidDemoSceneObjects;
	}

	// Reset the embedded code handlers
	void Context::Reset()
	{
		for (auto& handler : EmbeddedCodeHandlers)
			handler->Reset();
	}

	// Handle embedded code within ldr script
	bool Context::Execute(string const& lang, string const& code, string& result)
	{
		for (auto& handler : EmbeddedCodeHandlers)
			if (handler->Execute(lang, code, result))
				return true;

		return false;
	}
}
