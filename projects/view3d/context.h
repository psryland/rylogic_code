//***************************************************************************************************
// View 3D
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************
#pragma once

#include "view3d/forward.h"

namespace view3d
{
	// Global data for this dll
	struct Context :pr::AlignTo<16>
	{
		using InitSet = std::set<View3DContext>;

		InitSet                 m_inits;      // A unique id assigned to each Initialise call
		bool                    m_compatible; // True if the renderer will work on this system
		pr::Renderer            m_rdr;        // The renderer
		WindowCont              m_wnd_cont;   // The created windows
		pr::ldr::ScriptSources  m_sources;    // A container of Ldr objects and a file watcher
		pr::script::EmbeddedLua m_lua;
		std::recursive_mutex    m_mutex;

		explicit Context(HINSTANCE instance, BOOL gdi_compatible)
			:m_inits()
			,m_compatible(pr::rdr::TestSystemCompatibility())
			,m_rdr(pr::rdr::RdrSettings(instance, gdi_compatible))
			,m_wnd_cont()
			,m_sources(m_rdr, &m_lua)
			,m_lua()
			,m_mutex()
		{
			PR_ASSERT(PR_DBG, pr::meta::is_aligned_to<16>(this), "dll data not aligned");

			// Hook up the sources events
			m_sources.OnAddFileProgress += [&](pr::ldr::ScriptSources&, pr::ldr::ScriptSources::AddFileProgressEventArgs& args)
			{
				auto context_id  = args.m_context_id;
				auto filepath    = args.m_loc.StreamName();
				auto file_offset = args.m_loc.Pos();
				auto complete    = args.m_complete;
				OnAddFileProgress.Raise(args.m_cancel, context_id, filepath.c_str(), file_offset, complete);
			};
			m_sources.OnReload += [&](pr::ldr::ScriptSources&, pr::ldr::ScriptSources::ReloadEventArgs const& args)
			{
				OnSourcesChanged.Raise(static_cast<ESourcesChangedReason>(args.m_reason), true);
			};
			m_sources.OnStoreChanged += [&](pr::ldr::ScriptSources&, pr::ldr::ScriptSources::StoreChangedEventArgs const& args)
			{
				OnSourcesChanged.Raise(static_cast<ESourcesChangedReason>(args.m_reason), false);
			};
			m_sources.OnError += [&](pr::ldr::ScriptSources&, pr::ErrorEventArgs const& args)
			{
				ReportError(args.m_msg.c_str());
			};
		}
		~Context()
		{
			while (!m_wnd_cont.empty())
				View3D_DestroyWindow(*m_wnd_cont.begin());
		}
		Context(Context const&) = delete;
		Context& operator=(Context const&) = delete;

		// Error event. Can be called in a worker thread context
		pr::MultiCast<ReportErrorCB> OnError;

		// Event raised when script sources are parsed during adding/updating
		pr::MultiCast<AddFileProgressCB> OnAddFileProgress;

		// Event raised when the script sources are updated
		pr::MultiCast<SourcesChangedCB> OnSourcesChanged;

		// Report an error to the global error handler
		void ReportError(wchar_t const* msg)
		{
			OnError.Raise(msg);
		}

		// Remove all Ldr script sources
		void ClearScriptSources()
		{
			// Reset the sources collection
			m_sources.Clear();
		}

		// Load/Add a script source. Returns the Guid of the context that the objects were added to.
		pr::Guid LoadScriptSource(wchar_t const* filepath, bool additional, pr::script::Includes const& includes)
		{
			// Note: this can be called from a worker thread
			return m_sources.AddFile(filepath, includes, additional);
		}

		// Load/Add ldr objects from a script string. Returns the Guid of the context that the objects were added to.
		pr::Guid LoadScript(wchar_t const* ldr_script, bool file, pr::Guid const* context_id, pr::script::Includes const& includes)
		{
			// Note: this can be called from a worker thread
			return m_sources.AddScript(ldr_script, file, context_id, includes);
		}

		// Reload script source files
		void ReloadScriptSources()
		{
			// Remove script source objects from all windows
			for (auto src : m_sources.Files())
			{
				auto id = src.second.m_file_group_id;
				DeleteAllObjectsById(id);
			}

			// Reload the source data
			m_sources.Reload();
		}

		// Poll for changed script source files, and reload any that have changed
		void CheckForChangedSources()
		{
			m_sources.RefreshChangedFiles();
		}

		// Edit the geometry of a model after it has been allocated
		void EditObject(pr::ldr::LdrObject* object, View3D_EditObjectCB edit_cb, void* ctx)
		{
			// Remove the object from any windows it might be in
			for (auto wnd : m_wnd_cont)
				View3D_RemoveObject(wnd, object);

			// Callback to edit the geometry
			ObjectEditCBData cbdata = { edit_cb, ctx };
			pr::ldr::Edit(m_rdr, object, ObjectEditCB, &cbdata);
		}

		// Update the model in an existing object
		void UpdateObject(pr::ldr::LdrObject* object, wchar_t const* ldr_script, pr::ldr::EUpdateObject flags)
		{
			// Remove the object from any windows it might be in
			for (auto wnd : m_wnd_cont)
				View3D_RemoveObject(wnd, object);

			// Update the object model
			pr::script::PtrW src(ldr_script);
			pr::script::Reader reader(src, false);
			pr::ldr::Update(m_rdr, object, reader, flags);
		}

		// Delete all objects
		void DeleteAllObjects()
		{
			// Remove the objects from any windows they're in
			for (auto wnd : m_wnd_cont)
				View3D_RemoveAllObjects(wnd);
		
			// Clear the object container. The unique pointers should delete the objects
			m_sources.ClearAll();
		}

		// Delete all objects with matching ids
		void DeleteAllObjectsById(pr::Guid const& context_id)
		{
			// Remove objects from any windows they might be assigned to
			for (auto wnd : m_wnd_cont)
				View3D_RemoveObjectsById(wnd, false, context_id);

			m_sources.Remove(context_id);
		}

		// Delete a single object
		void DeleteObject(pr::ldr::LdrObject* object)
		{
			// Remove the object from any windows it's in
			for (auto wnd : m_wnd_cont)
				View3D_RemoveObject(wnd, object);
		
			// Delete the object from the object container
			m_sources.Remove(object);
		}

		// Return the context id for objects created from 'filepath' (if filepath is an existing source)
		bool ContextIdFromFilepath(wchar_t const* filepath, pr::Guid& id)
		{
			return m_sources.ContextIdFromFilepath(filepath, id);
		}

		// Create a gizmo object and add it to the gizmo collection
		pr::ldr::LdrGizmo* CreateGizmo(pr::ldr::LdrGizmo::EMode mode, pr::m4x4 const& o2w)
		{
			return m_sources.CreateGizmo(mode, o2w);
		}

		// Destroy a gizmo
		void DeleteGizmo(pr::ldr::LdrGizmo* gizmo)
		{
			// Remove the gizmo from any windows it's in
			for (auto wnd : m_wnd_cont)
				View3D_RemoveGizmo(wnd, gizmo);
		
			// Delete the gizmo from the sources
			m_sources.RemoveGizmo(gizmo);
		}

		// Callback function called from pr::ldr::CreateEditCB to populate the model data
		struct ObjectEditCBData { View3D_EditObjectCB edit_cb; void* ctx; };
		void static __stdcall ObjectEditCB(pr::rdr::ModelPtr model, void* ctx, pr::Renderer&)
		{
			using namespace pr::rdr;

			PR_ASSERT(PR_DBG, model != nullptr, "");
			if (!model) throw std::exception("model is null");
			ObjectEditCBData& cbdata = *static_cast<ObjectEditCBData*>(ctx);

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
			cbdata.edit_cb(UINT32(vrange.size()), UINT32(irange.size()), UINT32(nuggets.size()), &verts[0], &indices[0], &nuggets[0], new_vcount, new_icount, new_ncount, cbdata.ctx);
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
					mat.m_tex_diffuse = nug.m_mat.m_diff_tex;
					model->CreateNugget(mat);
				}
			}
		}
	};
}
