//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/view3d-dll.h"
#include "pr/view3d-12/ldraw/ldraw_object.h"
#include "pr/view3d-12/ldraw/ldraw_gizmo.h"
#include "pr/view3d-12/ldraw/ldraw_parsing.h"
#include "pr/view3d-12/ldraw/ldraw_serialiser_text.h"
#include "pr/view3d-12/model/model_generator.h"
#include "pr/view3d-12/model/vertex_layout.h"
#include "view3d-12/src/dll/context.h"
#include "view3d-12/src/dll/v3d_window.h"

namespace pr::rdr12
{
	Context::Context(HINSTANCE instance, StaticCB<view3d::ReportErrorCB> global_error_cb)
		: m_rdr(RdrSettings(instance).DebugLayer(PR_DBG_RDR).DefaultAdapter())
		, m_windows()
		, m_sources(m_rdr, *this)
		, m_inits()
		, m_mutex()
		, ReportError()
		, ParsingProgress()
		, SourcesChanged()
	{
		ReportError += global_error_cb;
	}
	Context::~Context()
	{
		for (auto& wnd : m_windows)
			delete wnd;
	}

	// Report an error handled at the DLL API layer
	void Context::ReportAPIError(char const* func_name, view3d::Window wnd, std::exception const* ex)
	{
		// Create the error message
		auto msg = Fmt<pr::string<char>>("%s failed.\n%s", func_name, ex ? ex->what() : "Unknown exception occurred.");
		if (msg.last() != '\n')
			msg.push_back('\n');

		// If a window handle is provided, report via the window's event.
		// Otherwise, fall back to the global error handler
		if (wnd != nullptr)
			wnd->ReportError(msg.c_str(), "", 0, 0);
		else
			ReportError(msg.c_str(), "", 0, 0);
	}

	// Create/Destroy windows
	V3dWindow* Context::WindowCreate(HWND hwnd, view3d::WindowOptions const& opts)
	{
		try
		{
			V3dWindow* win;
			m_windows.push_back(win = new V3dWindow(rdr(), hwnd, opts));
			return win;
		}
		catch (std::exception const& e)
		{
			if (opts.m_error_cb) opts.m_error_cb(opts.m_error_cb_ctx, FmtS("Failed to create View3D Window.\n%s", e.what()), "", 0, 0);
			return nullptr;
		}
		catch (...)
		{
			if (opts.m_error_cb) opts.m_error_cb(opts.m_error_cb_ctx, FmtS("Failed to create View3D Window.\nUnknown reason"), "", 0, 0);
			return nullptr;
		}
	}
	void Context::WindowDestroy(V3dWindow* window)
	{
		erase_first(m_windows, [=](auto& wnd) { return wnd == window; });
		delete window;
	}

	// Create an include handler that can load from directories or embedded resources.
	PathResolver IncludeHandler(view3d::Includes const* includes)
	{
		PathResolver inc;
		if (includes != nullptr)
		{
			if (includes->m_include_paths != nullptr)
				inc.SearchPathList(includes->m_include_paths);

			if (includes->m_module_count != 0)
				inc.ResourceModules(std::initializer_list<HMODULE>(includes->m_modules, includes->m_modules + includes->m_module_count));
		}
		return std::move(inc);
	}

	// Load/Add ldr objects from a script file. Returns the Guid of the context that the objects were added to.
	Guid Context::LoadScriptFile(std::filesystem::path ldr_script, EEncoding enc, Guid const* context_id, PathResolver const& includes, ScriptSources::OnAddCB on_add) // worker thread context
	{
		return m_sources.AddFile(ldr_script, enc, ldraw::ESourceChangeReason::NewData, context_id, includes, on_add);
	}

	// Load/Add ldr objects from a script string. Returns the Guid of the context that the objects were added to.
	template <typename Char>
	Guid Context::LoadScriptString(std::basic_string_view<Char> ldr_script, EEncoding enc, Guid const* context_id, PathResolver const& includes, ScriptSources::OnAddCB on_add) // worker thread context
	{
		return m_sources.AddString(ldr_script, enc, ldraw::ESourceChangeReason::NewData, context_id, includes, on_add);
	}
	template Guid Context::LoadScriptString<wchar_t>(std::wstring_view ldr_script, EEncoding enc, Guid const* context_id, PathResolver const& includes, ScriptSources::OnAddCB on_add);
	template Guid Context::LoadScriptString<char>(std::string_view ldr_script, EEncoding enc, Guid const* context_id, PathResolver const& includes, ScriptSources::OnAddCB on_add);

	// Load/Add ldraw objects from binary data. Returns the Guid of the context that the objects were added to.
	Guid Context::LoadScriptBinary(std::span<std::byte const> data, Guid const* context_id, ScriptSources::OnAddCB on_add)
	{
		return m_sources.AddBinary(data, ldraw::ESourceChangeReason::NewData, context_id, on_add);
	}

	// Enable/Disable streaming script sources.
	void Context::StreamingEnable(bool enabled, uint16_t port)
	{
		if (enabled)
			m_sources.AllowConnections(port);
		else
			m_sources.StopConnections();
	}

	// Create an object from geometry
	LdrObject* Context::ObjectCreate(char const* name, Colour32 colour, std::span<view3d::Vertex const> verts, std::span<uint16_t const> indices, std::span<view3d::Nugget const> nuggets, Guid const& context_id)
	{
		using namespace pr::script;

		auto geom = EGeom::None;
		pr::vector<NuggetDesc> ngt;

		// Generate the nuggets first so we can tell what geometry data is needed
		for (auto const& nugget : nuggets)
		{
			// Create the renderer nugget
			NuggetDesc nug = NuggetDesc(static_cast<ETopo>(nugget.m_topo), static_cast<EGeom>(nugget.m_geom))
				.vrange(nugget.m_v0 != nugget.m_v1 ? Range(nugget.m_v0, nugget.m_v1) : Range(0, verts.size()))
				.irange(nugget.m_i0 != nugget.m_i1 ? Range(nugget.m_i0, nugget.m_i1) : Range(0, indices.size()))
				.tex_diffuse(Texture2DPtr(nugget.m_tex_diffuse, true))
				.sam_diffuse(SamplerPtr(nugget.m_sam_diffuse, true))
				.flags(static_cast<ENuggetFlag>(nugget.m_nflags))
				.rel_reflec(nugget.m_rel_reflec)
				.tint(nugget.m_tint);

			if (nugget.m_cull_mode != view3d::ECullMode::Default) nug.pso<EPipeState::CullMode>(static_cast<D3D12_CULL_MODE>(nugget.m_cull_mode));
			if (nugget.m_fill_mode != view3d::EFillMode::Default) nug.pso<EPipeState::FillMode>(static_cast<D3D12_FILL_MODE>(nugget.m_fill_mode));
			for (auto const& shdr : nugget.shader_span())
				nug.use_shader(static_cast<ERenderStep>(shdr.m_rdr_step), ShaderPtr(shdr.m_shader, true));

			ngt.push_back(nug);

			// Sanity check the nugget
			PR_ASSERT(PR_DBG, nug.m_vrange.begin() <= nug.m_vrange.end() && nug.m_vrange.end() <= isize(verts), "Invalid nugget V-range");
			PR_ASSERT(PR_DBG, nug.m_irange.begin() <= nug.m_irange.end() && nug.m_irange.end() <= isize(indices), "Invalid nugget I-range");

			// Union of geometry data type
			geom |= nug.m_geom;
		}

		// Vertex buffer
		pr::vector<v4> pos;
		{
			pos.resize(verts.size());
			for (int i = 0, iend = isize(verts); i != iend; ++i)
				pos[i] = To<v4>(verts[i].pos);
		}

		// Colour buffer
		pr::vector<Colour32> col;
		if (AllSet(geom, EGeom::Colr))
		{
			col.resize(verts.size());
			for (int i = 0, iend = isize(verts); i != iend; ++i)
				col[i] = verts[i].col;
		}

		// Normals
		pr::vector<v4> nrm;
		if (AllSet(geom, EGeom::Norm))
		{
			nrm.resize(verts.size());
			for (int i = 0, iend = isize(verts); i != iend; ++i)
				nrm[i] = To<v4>(verts[i].norm);
		}

		// Texture coords
		pr::vector<v2> tex;
		if (AllSet(geom, EGeom::Tex0))
		{
			tex.resize(verts.size());
			for (int i = 0, iend = isize(verts); i != iend; ++i)
				tex[i] = To<v2>(verts[i].tex);
		}

		// Indices
		auto& ind = indices;

		// Create the model
		MeshCreationData cdata = MeshCreationData().verts(pos).indices(ind).nuggets(ngt).colours(col).normals(nrm).tex(tex);
		auto obj = Create(m_rdr, ldraw::ELdrObject::Custom, cdata, context_id);

		// Add to the sources
		obj->m_name = name;
		obj->m_base_colour = colour;
		m_sources.Add(obj);

		// Return the created object
		return obj.get();
	}

	// Load/Add ldr objects and return the first object from the script
	template <typename Char>
	LdrObject* Context::ObjectCreateLdr(std::basic_string_view<Char> ldr_script, bool file, EEncoding enc, Guid const* context_id, view3d::Includes const* includes)
	{
		// Get the context id for this script
		auto id = context_id ? *context_id : GenerateGUID();

		// Create an include handler
		auto include_handler = IncludeHandler(includes);

		// Record how many objects there are already for the context id (if it exists)
		auto& srcs = m_sources.Sources();
		auto iter = srcs.find(id);
		auto count = iter != end(srcs) ? iter->second->m_output.m_objects.size() : 0U;

		// Load the ldr script
		if (file)
			LoadScriptFile(ldr_script, enc, &id, include_handler, nullptr);
		else
			LoadScriptString(ldr_script, enc, &id, include_handler, nullptr);

		// Return the first object, expecting 'ldr_script' to define one object only.
		// It doesn't matter if more are defined however, they're just created as part of the context.
		iter = srcs.find(id);
		return iter != std::end(srcs) && iter->second->m_output.m_objects.size() > count
			? iter->second->m_output.m_objects[count].get()
			: nullptr;
	}
	template LdrObject* Context::ObjectCreateLdr<wchar_t>(std::wstring_view ldr_script, bool file, EEncoding enc, Guid const* context_id, view3d::Includes const* includes);
	template LdrObject* Context::ObjectCreateLdr<char>(std::string_view ldr_script, bool file, EEncoding enc, Guid const* context_id, view3d::Includes const* includes);

	// Create an LdrObject from the p3d model
	LdrObject* Context::ObjectCreateP3D(char const* name, Colour32 colour, std::filesystem::path const& p3d_filepath, Guid const* context_id)
	{
		// Get the context id
		auto id = context_id ? *context_id : GenerateGUID();

		// Create an ldr object
		auto obj = ldraw::CreateP3D(m_rdr, ldraw::ELdrObject::Model, p3d_filepath, id);
		obj->m_name = name;
		obj->m_base_colour = colour;
		m_sources.Add(obj);
		return obj.get();
	}
	LdrObject* Context::ObjectCreateP3D(char const* name, Colour32 colour, std::span<std::byte const> p3d_data, Guid const* context_id)
	{
		// Get the context id
		auto id = context_id ? *context_id : pr::GenerateGUID();

		// Create an ldr object
		auto obj = rdr12::ldraw::CreateP3D(m_rdr, ldraw::ELdrObject::Model, p3d_data, id);
		obj->m_name = name;
		obj->m_base_colour = colour;
		m_sources.Add(obj);
		return obj.get();
	}

	// Modify an ldr object using a callback to populate the model data.
	static void __stdcall EditModel(Model* model, void* ctx, Renderer&)
	{
		using namespace pr::rdr12;

		// Thread local storage for editing dynamic models
		thread_local static pr::vector<view3d::Vertex>         cache_vbuf;
		thread_local static pr::vector<uint16_t>               cache_ibuf;
		thread_local static pr::vector<view3d::Nugget>         cache_nbuf;

		if (!model)
			throw std::runtime_error("model is null");

		auto& vbuf = cache_vbuf;
		auto& ibuf = cache_ibuf;
		auto& nbuf = cache_nbuf;

		// Create buffers to be filled by the user callback
		// Note: We can't fill the buffers with the existing model data because that requires
		// reading from video memory (slow, or not possible for some model types).
		vbuf.resize(s_cast<size_t>(model->m_vcount));
		ibuf.resize(s_cast<size_t>(model->m_icount));
		nbuf.resize(0);

#if 0
		// If the model already has nuggets, initialise 'nbuf' with them
		if (!model->m_nuggets.empty())
		{
			auto ValueOrDefault = [](auto* v, decltype(*v) d) -> decltype(*v) { return v ? *v : d; };

			for (auto& nug : model->m_nuggets)
			{
				nbuf.push_back(view3d::Nugget{
					.m_topo = static_cast<view3d::ETopo>(nug.m_topo),
					.m_geom = static_cast<view3d::EGeom>(nug.m_geom),
					.m_tex_diffuse = nug.m_tex_diffuse.get(),
					.m_sam_diffuse = nug.m_sam_diffuse.get(),
					.m_shaders = {.m_shaders = nug.m_shaders.data(), .m_count = isize(nug.m_shaders) },
					.m_v0 = s_cast<int>(nug.m_vrange.begin()),
					.m_v1 = s_cast<int>(nug.m_vrange.end()),
					.m_i0 = s_cast<int>(nug.m_irange.begin()),
					.m_i1 = s_cast<int>(nug.m_irange.end()),
					.m_nflags = static_cast<view3d::ENuggetFlag>(nug.m_nflags),
					.m_cull_mode = static_cast<view3d::ECullMode>(ValueOrDefault(nug.m_pso.Find<EPipeState::CullMode>(), D3D12_CULL_MODE(0))),
					.m_fill_mode = static_cast<view3d::EFillMode>(ValueOrDefault(nug.m_pso.Find<EPipeState::FillMode>(), D3D12_FILL_MODE(0))),
					.m_tint = To<view3d::Colour>(nug.m_tint),
					.m_rel_reflec = nug.m_rel_reflec,

				});
			}
		}
#endif

		// Get the user to generate/update the model
		auto& edit_cb = *static_cast<StaticCB<view3d::EditObjectCB>*>(ctx);
		auto [new_vcount, new_icount] = edit_cb(isize(vbuf), isize(ibuf), vbuf.data(), ibuf.data(),
			[](void* ctx, view3d::Nugget const& n)
		{
			static_cast<pr::vector<view3d::Nugget>*>(ctx)->push_back(n);
		}, &nbuf);

		// Sanity check results
		if (new_vcount > isize(vbuf)) throw std::runtime_error("Dynamic model buffer overrun (v-buf)");
		if (new_icount > isize(ibuf)) throw std::runtime_error("Dynamic model buffer overrun (i-buf)");

		ResourceFactory factory(model->rdr());

		{// Update the model geometry
			auto update_v = model->UpdateVertices(factory, { 0, new_vcount });
			auto update_i = model->UpdateIndices(factory, { 0, new_icount });

			model->m_bbox.reset();

			auto vin = vbuf.data();
			auto iin = ibuf.data();
			auto vout = update_v.ptr<Vert>();
			auto iout = update_i.ptr<uint16_t>();

			// Copy the model data into the model
			for (int i = 0; i != new_vcount; ++i, ++vin)
			{
				SetPCNT(*vout++, To<v4>(vin->pos), Colour(vin->col), To<v4>(vin->norm), To<v2>(vin->tex));
				Grow(model->m_bbox, To<v4>(vin->pos));
			}
			for (int i = 0; i != new_icount; ++i, ++iin)
			{
				*iout++ = *iin;
			}

			update_v.Commit(D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER);
			update_i.Commit(D3D12_RESOURCE_STATE_INDEX_BUFFER);
		}

		// Update the model nuggets
		model->DeleteNuggets();
		for (auto& nug : nbuf)
		{
			auto n = NuggetDesc{};
			n.m_topo = static_cast<ETopo>(nug.m_topo);
			n.m_geom = static_cast<EGeom>(nug.m_geom);
			n.m_tex_diffuse = Texture2DPtr(nug.m_tex_diffuse, true);
			n.m_sam_diffuse = SamplerPtr(nug.m_sam_diffuse, true);

			for (auto& shdr : nug.shader_span())
				n.m_shaders.push_back({ ShaderPtr(shdr.m_shader, true), static_cast<ERenderStep>(shdr.m_rdr_step) });

			if (nug.m_cull_mode != view3d::ECullMode::Default)
				n.m_pso.Set<EPipeState::CullMode>(static_cast<D3D12_CULL_MODE>(nug.m_cull_mode));
			if (nug.m_fill_mode != view3d::EFillMode::Default)
				n.m_pso.Set<EPipeState::FillMode>(static_cast<D3D12_FILL_MODE>(nug.m_fill_mode));

			n.m_nflags = static_cast<ENuggetFlag>(nug.m_nflags);
			n.m_tint = nug.m_tint != 0 ? To<Colour>(nug.m_tint) : Colour32White;
			n.m_rel_reflec = nug.m_rel_reflec;
			n.m_vrange = nug.m_v0 < nug.m_v1 ? Range{ nug.m_v0, nug.m_v1 } : Range{ 0, new_vcount };
			n.m_irange = nug.m_i0 < nug.m_i1 ? Range{ nug.m_i0, nug.m_i1 } : Range{ 0, new_icount };
			model->CreateNugget(factory, n);
		}

		// Release memory after large allocations
		if (vbuf.capacity() > 0x100000) vbuf.clear();
		if (ibuf.capacity() > 0x100000) ibuf.clear();
		if (nbuf.capacity() > 0x100000) nbuf.clear();
	}
	LdrObject* Context::ObjectCreateByCallback(char const* name, Colour32 colour, int vcount, int icount, int ncount, StaticCB<view3d::EditObjectCB> edit_cb, Guid const& context_id)
	{
		auto obj = ldraw::CreateEditCB(m_rdr, ldraw::ELdrObject::Custom, vcount, icount, ncount, EditModel, &edit_cb, context_id);
		obj->m_name = name;
		obj->m_base_colour = colour;
		m_sources.Add(obj);
		return obj.get();
	}
	void Context::ObjectEdit(LdrObject* object, StaticCB<view3d::EditObjectCB> edit_cb)
	{
		// Remove the object from any windows it might be in
		for (auto& wnd : m_windows)
			wnd->Remove(object);

		// Callback to edit the geometry
		Edit(m_rdr, object, EditModel, &edit_cb);
	}

	// Update the model in an existing object
	template <typename Char>
	void Context::UpdateObject(LdrObject* object, std::basic_string_view<Char> ldr_script, ldraw::EUpdateObject flags)
	{
		// Remove the object from any windows it might be in
		for (auto& wnd : m_windows)
			wnd->Remove(object);

		// Update the object model
		mem_istream<Char> src{ ldr_script, 0 };
		rdr12::ldraw::TextReader reader(src, {});
		ldraw::Update(m_rdr, object, reader, flags);
	}
	template void Context::UpdateObject<wchar_t>(LdrObject* object, std::wstring_view, ldraw::EUpdateObject flags);
	template void Context::UpdateObject<char>(LdrObject* object, std::string_view, ldraw::EUpdateObject flags);

	// Delete a single object
	void Context::DeleteObject(LdrObject* object)
	{
		// Remove the object from any windows it's in
		for (auto& wnd : m_windows)
			wnd->Remove(object);

		// Delete the object from the object container
		m_sources.Remove(object);
	}

	// Delete all objects
	void Context::DeleteAllObjects()
	{
		// Remove the objects from any windows they're in
		for (auto& wnd : m_windows)
			wnd->RemoveAllObjects();

		// Clear the object container. The unique pointers should delete the objects
		m_sources.ClearAll();
	}

	// Delete all objects with matching ids
	void Context::DeleteAllObjectsById(std::span<Guid const> include, std::span<Guid const> exclude)
	{
		// Remove objects from any windows they might be assigned to
		for (auto& wnd : m_windows)
			wnd->Remove(include, exclude, false);

		// Remove sources that match the given set of context ids to delete
		m_sources.Remove(include, exclude);
	}

	// Delete all objects not displayed in any windows
	void Context::DeleteUnused(std::span<Guid const> include, std::span<Guid const> exclude)
	{
		// Build a set of context ids, included in 'context_ids', and not used in any windows
		GuidSet unused;

		// Initialise 'unused' with all context ids (filtered by 'context_ids')
		for (auto& src : m_sources.Sources())
		{
			if (!IncludeFilter(src.first, include, exclude)) continue;
			unused.insert(src.first);
		}

		// Remove those that are used in the windows
		for (auto& wnd : m_windows)
		{
			for (auto& id : wnd->m_guids)
				unused.erase(id);
		}

		// Remove unused sources
		if (!unused.empty())
		{
			pr::vector<Guid> ids(std::begin(unused), std::end(unused));
			m_sources.Remove(ids, {}, ldraw::ESourceChangeReason::Removal);
		}
	}

	// Enumerate the GUIDs in the sources collection
	void Context::SourceEnumGuids(StaticCB<bool, GUID const&> enum_guids_cb)
	{
		for (auto& src : m_sources.Sources())
			if (!enum_guids_cb(src.second->m_context_id))
				return;
	}

	// Create a gizmo object and add it to the gizmo collection
	LdrGizmo* Context::GizmoCreate(ldraw::EGizmoMode mode, m4x4 const& o2w)
	{
		return m_sources.CreateGizmo(mode, o2w);
	}

	// Destroy a gizmo
	void Context::GizmoDelete(LdrGizmo* gizmo)
	{
		// Remove the gizmo from any windows it's in
		for (auto& wnd : m_windows)
			wnd->Remove(gizmo);

		// Delete the gizmo from the sources
		m_sources.RemoveGizmo(gizmo);
	}

	// Reload sources
	void Context::ReloadScriptSources()
	{
		m_sources.Reload();
	}

	// Poll for changed script source files, and reload any that have changed
	void Context::CheckForChangedSources()
	{
		m_sources.RefreshChangedFiles();
	}

	// Return the context id for objects created from 'filepath' (if filepath is an existing source)
	Guid const* Context::ContextIdFromFilepath(char const* filepath) const
	{
		return m_sources.ContextIdFromFilepath(filepath);
	}

	// Parse error event.
	void Context::OnError(ldraw::ParseErrorEventArgs const& args)
	{
		auto filepath = args.m_loc.m_filepath.generic_string();
		ReportError(args.m_msg.c_str(), filepath.c_str(), args.m_loc.m_line, args.m_loc.m_offset);
	}

	// An event raised during parsing. This is called in the context of the threads that call 'AddFile'. Do not sign up while AddFile calls are running.
	void Context::OnParsingProgress(ldraw::ParsingProgressEventArgs& args)
	{
		auto context_id = args.m_context_id;
		auto filepath = args.m_loc.m_filepath.generic_string();
		auto file_offset = s_cast<int64_t>(args.m_loc.m_offset);
		BOOL complete = args.m_complete;
		BOOL cancel = false;
		ParsingProgress(context_id, filepath.c_str(), file_offset, complete, cancel);
		args.m_cancel = cancel != 0;
	}

	// Reload event. Note: Don't AddFile() or RefreshChangedFiles() during this event.
	void Context::OnReload()
	{
		SourcesChanged(view3d::ESourcesChangedReason::Reload, true);
	}

	// Store change event. Called before and after a change to the collection of objects in the store.
	void Context::OnStoreChange(ldraw::StoreChangeEventArgs& args)
	{
		if (args.m_before)
			return;

		switch (args.m_reason)
		{
			case ldraw::ESourceChangeReason::NewData:
			{
				// On NewData, do nothing. Callers will add objects to windows as they see fit.
				break;
			}
			case ldraw::ESourceChangeReason::Removal:
			{
				// On Removal, do nothing. Removed objects should already have been removed from the windows.
				break;
			}
			case ldraw::ESourceChangeReason::Reload:
			{
				// On Reload, for each object currently in the window and in the set of affected context ids, remove and re-add.
				for (auto& wnd : m_windows)
				{
					wnd->Add(m_sources.Sources(), args.m_context_ids, {});
					wnd->Invalidate();
				}
				break;
			}
			default:
			{
				throw std::runtime_error("Unknown store changed reason");
			}
		}

		// Notify of updated sources
		SourcesChanged(static_cast<view3d::ESourcesChangedReason>(args.m_reason), false);
	}

	// Source removed event (i.e. objects deleted by Id)
	void Context::OnSourceRemoved(ldraw::SourceRemovedEventArgs const& args)
	{
		auto reload = args.m_reason == ldraw::ESourceChangeReason::Reload;

		// When a source is about to be removed, remove it's objects from the windows.
		// If this is a reload, save a reference to the removed objects so we know what to reload.
		for (auto& wnd : m_windows)
			wnd->Remove({ &args.m_context_id, 1 }, {}, reload);
	}

	// Process any received commands in the source
	void Context::OnHandleCommands(ldraw::SourceBase& source)
	{
		ldraw::ExecuteCommands(source, *this);
	}
}