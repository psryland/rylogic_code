//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/view3d-dll.h"
#include "pr/view3d-12/ldraw/ldr_object.h"
#include "pr/view3d-12/ldraw/ldr_gizmo.h"
#include "pr/view3d-12/model/model_generator.h"
#include "pr/view3d-12/model/vertex_layout.h"
#include "view3d-12/src/dll/context.h"
#include "view3d-12/src/dll/v3d_window.h"
#include "pr/script/embedded_lua.h"

// Include 'embedded_lua.h" in here so that additional include directories
// are not needed for anyone including "pr/view3d-12/forward.h"

namespace pr::rdr12
{
	Context::Context(HINSTANCE instance, StaticCB<view3d::ReportErrorCB> global_error_cb)
		: m_rdr(RdrSettings(instance).DebugLayer(PR_DBG_RDR).DefaultAdapter())
		, m_wnd_cont()
		, m_sources(m_rdr, [this](auto lang) { return CreateCodeHandler(lang); })
		, m_inits()
		, m_emb()
		, m_mutex()
		, ReportError()
		, OnAddFileProgress()
		, OnSourcesChanged()
	{
		ReportError += global_error_cb;

		// Hook up the sources events
		m_sources.OnAddFileProgress += [&](ScriptSources&, ScriptSources::AddFileProgressEventArgs& args)
		{
			auto context_id = args.m_context_id;
			auto filepath = args.m_loc.Filepath().generic_string();
			auto file_offset = s_cast<int64_t>(args.m_loc.Pos());
			BOOL complete = args.m_complete;
			BOOL cancel = false;
			OnAddFileProgress(context_id, filepath.c_str(), file_offset, complete, cancel);
			args.m_cancel = cancel != 0;
		};
		m_sources.OnReload += [&](ScriptSources&, EmptyArgs const&)
		{
			OnSourcesChanged(view3d::ESourcesChangedReason::Reload, true);
		};
		m_sources.OnSourceRemoved += [&](ScriptSources&, ScriptSources::SourceRemovedEventArgs const& args)
		{
			auto reload = args.m_reason == ScriptSources::EReason::Reload;

			// When a source is about to be removed, remove it's objects from the windows.
			// If this is a reload, save a reference to the removed objects so we know what to reload.
			for (auto& wnd : m_wnd_cont)
				wnd->Remove(&args.m_context_id, 1, false, reload);
		};
		m_sources.OnStoreChange += [&](ScriptSources&, ScriptSources::StoreChangeEventArgs const& args)
		{
			if (args.m_before)
				return;

			switch (args.m_reason)
			{
				// On NewData, do nothing. Callers will add objects to windows as they see fit.
				case ScriptSources::EReason::NewData:
				{
					break;
				}
				// On Removal, do nothing. Removed objects should already have been removed from the windows.
				case ScriptSources::EReason::Removal:
				{
					break;
				}
				// On Reload, for each object currently in the window and in the set of affected context ids, remove and re-add.
				case ScriptSources::EReason::Reload:
				{
					for (auto& wnd : m_wnd_cont)
					{
						wnd->Add(args.m_context_ids.data(), static_cast<int>(args.m_context_ids.size()), 0);
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
			OnSourcesChanged(static_cast<view3d::ESourcesChangedReason>(args.m_reason), false);
		};
		m_sources.OnError += [&](ScriptSources&, ScriptSources::ParseErrorEventArgs const& args)
		{
			auto msg = Narrow(args.m_msg);
			auto filepath = args.m_loc.Filepath().generic_string();
			auto line = s_cast<int>(args.m_loc.Line());
			auto pos = s_cast<int64_t>(args.m_loc.Pos());
			ReportError(msg.c_str(), filepath.c_str(), line, pos);
		};
	}
	Context::~Context()
	{
		for (auto& wnd : m_wnd_cont)
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
			m_wnd_cont.push_back(win = new V3dWindow(hwnd, *this, opts));
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
		erase_first(m_wnd_cont, [=](auto& wnd){ return wnd == window; });
		delete window;
	}

	// Create a script include handler that can load from directories or embedded resources.
	script::Includes IncludeHandler(view3d::Includes const* includes)
	{
		script::Includes inc;
		if (includes != nullptr)
		{
			if (includes->m_include_paths != nullptr)
				inc.SearchPathList(includes->m_include_paths);

			if (includes->m_module_count != 0)
				inc.ResourceModules(std::initializer_list<HMODULE>(includes->m_modules, includes->m_modules + includes->m_module_count));
		}
		return std::move(inc);
	}

	// Load/Add ldr objects from a script string. Returns the Guid of the context that the objects were added to.
	Guid Context::LoadScriptFile(std::filesystem::path ldr_script, EEncoding enc, std::optional<Guid const> context_id, script::Includes const& includes, ScriptSources::OnAddCB on_add) // worker thread context
	{
		return m_sources.AddFile(ldr_script, enc, ScriptSources::EReason::NewData, context_id, includes, on_add);
	}

	// Load/Add ldr objects from a script string. Returns the Guid of the context that the objects were added to.
	template <typename Char>
	Guid Context::LoadScriptString(std::basic_string_view<Char> ldr_script, EEncoding enc, std::optional<Guid const> context_id, script::Includes const& includes, ScriptSources::OnAddCB on_add) // worker thread context
	{
		return m_sources.AddString(ldr_script, enc, ScriptSources::EReason::NewData, context_id, includes, on_add);
	}
	template Guid Context::LoadScriptString<wchar_t>(std::wstring_view ldr_script, EEncoding enc, std::optional<Guid const> context_id, script::Includes const& includes, ScriptSources::OnAddCB on_add);
	template Guid Context::LoadScriptString<char>(std::string_view ldr_script, EEncoding enc, std::optional<Guid const> context_id, script::Includes const& includes, ScriptSources::OnAddCB on_add);

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
				.irange(nugget.m_i0 != nugget.m_i1 ? Range(nugget.m_i0, nugget.m_i1) : Range(0, indices.size()));

			//todo nug.m_shaders = nugget.m_mat.m_shader_map;
			if (nugget.m_cull_mode != view3d::ECullMode::Default) nug.pso<EPipeState::CullMode>(static_cast<D3D12_CULL_MODE>(nugget.m_cull_mode));
			if (nugget.m_fill_mode != view3d::EFillMode::Default) nug.pso<EPipeState::FillMode>(static_cast<D3D12_FILL_MODE>(nugget.m_fill_mode));
			nug.tex_diffuse(Texture2DPtr(nugget.m_tex_diffuse, true));
			nug.sam_diffuse(SamplerPtr(nugget.m_sam_diffuse, true));
			nug.flags(static_cast<ENuggetFlag>(nugget.m_nflags));
			nug.rel_reflec(nugget.m_rel_reflec);
			nug.tint(nugget.m_tint);
			for (auto const& shdr : std::span(nugget.m_shaders.m_shaders, nugget.m_shaders.m_count))
				nug.use_shader(static_cast<ERenderStep>(shdr.m_rdr_step), ShaderPtr(shdr.m_shader, true));

			ngt.push_back(nug);

			// Sanity check the nugget
			PR_ASSERT(PR_DBG, nug.m_vrange.begin() <= nug.m_vrange.end() && int(nug.m_vrange.end()) <= verts.size(), "Invalid nugget V-range");
			PR_ASSERT(PR_DBG, nug.m_irange.begin() <= nug.m_irange.end() && int(nug.m_irange.end()) <= indices.size(), "Invalid nugget I-range");

			// Union of geometry data type
			geom |= nug.m_geom;
		}

		// Vertex buffer
		pr::vector<v4> pos;
		{
			pos.resize(verts.size());
			for (auto i = 0; i != verts.size(); ++i)
				pos[i] = To<v4>(verts[i].pos);
		}

		// Colour buffer
		pr::vector<Colour32> col;
		if (AllSet(geom, EGeom::Colr))
		{
			col.resize(verts.size());
			for (auto i = 0; i != verts.size(); ++i)
				col[i] = verts[i].col;
		}

		// Normals
		pr::vector<v4> nrm;
		if (AllSet(geom, EGeom::Norm))
		{
			nrm.resize(verts.size());
			for (auto i = 0; i != verts.size(); ++i)
				nrm[i] = To<v4>(verts[i].norm);
		}

		// Texture coords
		pr::vector<v2> tex;
		if (AllSet(geom, EGeom::Tex0))
		{
			tex.resize(verts.size());
			for (auto i = 0; i != verts.size(); ++i)
				tex[i] = To<v2>(verts[i].tex);
		}

		// Indices
		auto& ind = indices;

		// Create the model
		auto attr  = ObjectAttributes(ELdrObject::Custom, name, Colour32(colour));
		auto cdata = MeshCreationData().verts(pos).indices(ind).nuggets(ngt).colours(col).normals(nrm).tex(tex);
		auto obj = Create(m_rdr, attr, cdata, context_id);
	
		// Add to the sources
		if (obj != nullptr)
			m_sources.Add(obj);

		// Return the created object
		return obj.get();
	}

	// Load/Add ldr objects and return the first object from the script
	template <typename Char>
	LdrObject* Context::ObjectCreateLdr(std::basic_string_view<Char> ldr_script, bool file, EEncoding enc, std::optional<Guid const> context_id, view3d::Includes const* includes)
	{
		// Get the context id for this script
		auto id = context_id ? *context_id : GenerateGUID();

		// Create an include handler
		auto include_handler = IncludeHandler(includes);

		// Record how many objects there are already for the context id (if it exists)
		auto& srcs = m_sources.Sources();
		auto iter = srcs.find(id);
		auto count = iter != end(srcs) ? iter->second.m_objects.size() : 0U;

		// Load the ldr script
		if (file)
			LoadScriptFile(ldr_script, enc, id, include_handler, nullptr);
		else
			LoadScriptString(ldr_script, enc, id, include_handler, nullptr);

		// Return the first object, expecting 'ldr_script' to define one object only.
		// It doesn't matter if more are defined however, they're just created as part of the context.
		iter = srcs.find(id);
		return iter != std::end(srcs) && iter->second.m_objects.size() > count
			? iter->second.m_objects[count].get()
			: nullptr;
	}
	template LdrObject* Context::ObjectCreateLdr<wchar_t>(std::wstring_view ldr_script, bool file, EEncoding enc, std::optional<Guid const> context_id, view3d::Includes const* includes);
	template LdrObject* Context::ObjectCreateLdr<char>(std::string_view ldr_script, bool file, EEncoding enc, std::optional<Guid const> context_id, view3d::Includes const* includes);

	// Create an LdrObject from the p3d model
	LdrObject* Context::ObjectCreateP3D(char const* name, Colour32 colour, std::filesystem::path const& p3d_filepath, std::optional<Guid const> context_id)
	{
		// Get the context id
		auto id = context_id ? *context_id : GenerateGUID();

		// Create an ldr object
		ObjectAttributes attr(ELdrObject::Model, name, colour);
		auto obj = CreateP3D(m_rdr, attr, p3d_filepath, id);
		m_sources.Add(obj);
		return obj.get();
	}
	LdrObject* Context::ObjectCreateP3D(char const* name, Colour32 colour, size_t size, void const* p3d_data, std::optional<Guid const> context_id)
	{
		// Get the context id
		auto id = context_id ? *context_id : pr::GenerateGUID();

		// Create an ldr object
		ObjectAttributes attr(ELdrObject::Model, name, colour);
		auto obj = CreateP3D(m_rdr, attr, size, p3d_data, id);
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
		vbuf.resize(model->m_vcount);
		ibuf.resize(model->m_icount);
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
		auto [new_vcount, new_icount] = edit_cb(isize(vbuf), isize(ibuf), vbuf.data(), ibuf.data(), [](void* ctx, view3d::Nugget const& n) { static_cast<pr::vector<view3d::Nugget>*>(ctx)->push_back(n); }, &nbuf);
		if (new_vcount > isize(vbuf)) throw std::runtime_error("Dynamic model buffer overrun (v-buf)");
		if (new_icount > isize(ibuf)) throw std::runtime_error("Dynamic model buffer overrun (i-buf)");

		ResourceFactory factory(model->rdr());

		{// Update the model geometry
			auto update_v = model->UpdateVertices(factory, { 0, new_vcount });
			auto update_i = model->UpdateIndices(factory, { 0, new_icount });
			
			model->m_bbox.reset();

			auto vin = vbuf.data();
			auto iin = ibuf.data();

			// Copy the model data into the model
			auto vout = update_v.ptr<Vert>();
			for (size_t i = 0; i != new_vcount; ++i, ++vin)
			{
				SetPCNT(*vout++, To<v4>(vin->pos), Colour(vin->col), To<v4>(vin->norm), To<v2>(vin->tex));
				Grow(model->m_bbox, To<v4>(vin->pos));
			}
			auto iout = update_v.ptr<uint16_t>();
			for (size_t i = 0; i != new_icount; ++i, ++iin)
			{
				*iout++ = *iin;
			}
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

			for (auto& shdr : nug.m_shaders.span())
				n.m_shaders.push_back({ static_cast<ERenderStep>(shdr.m_rdr_step), ShaderPtr(shdr.m_shader, true) });

			if (nug.m_cull_mode != view3d::ECullMode::Default)
				n.m_pso.Set<EPipeState::CullMode>(static_cast<D3D12_CULL_MODE>(nug.m_cull_mode));
			if (nug.m_fill_mode != view3d::EFillMode::Default)
				n.m_pso.Set<EPipeState::FillMode>(static_cast<D3D12_FILL_MODE>(nug.m_fill_mode));

			n.m_nflags = static_cast<ENuggetFlag>(nug.m_nflags);
			n.m_tint = To<Colour>(nug.m_tint);
			n.m_rel_reflec = nug.m_rel_reflec;
			n.m_vrange = Range{ nug.m_v0, nug.m_v1 };
			n.m_irange = Range{ nug.m_i0, nug.m_i1 };
			model->CreateNugget(factory, n);
		}

		// Release memory after large allocations
		if (vbuf.capacity() > 0x100000) vbuf.clear();
		if (ibuf.capacity() > 0x100000) ibuf.clear();
		if (nbuf.capacity() > 0x100000) nbuf.clear();
	}
	LdrObject* Context::ObjectCreateByCallback(char const* name, Colour32 colour, int vcount, int icount, int ncount, StaticCB<view3d::EditObjectCB> edit_cb, Guid const& context_id)
	{
		auto attr = ObjectAttributes{ ELdrObject::Custom, name, colour };
		auto obj = CreateEditCB(m_rdr, attr, vcount, icount, ncount, EditModel, &edit_cb, context_id);
		if (obj != nullptr)
			m_sources.Add(obj);

		return obj.get();
	}
	void Context::ObjectEdit(LdrObject* object, StaticCB<view3d::EditObjectCB> edit_cb)
	{
		// Remove the object from any windows it might be in
		for (auto& wnd : m_wnd_cont)
			wnd->Remove(object);

		// Callback to edit the geometry
		Edit(m_rdr, object, EditModel, &edit_cb);
	}

	// Update the model in an existing object
	void Context::UpdateObject(LdrObject* object, wchar_t const* ldr_script, EUpdateObject flags)
	{
		// Remove the object from any windows it might be in
		for (auto& wnd : m_wnd_cont)
			wnd->Remove(object);

		// Update the object model
		script::StringSrc src(ldr_script);
		script::Reader reader(src, false);
		Update(m_rdr, object, reader, flags);
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
	void Context::DeleteAllObjectsById(Guid const* context_ids, int include_count, int exclude_count)
	{
		// Remove objects from any windows they might be assigned to
		for (auto& wnd : m_wnd_cont)
			wnd->Remove(context_ids, include_count, exclude_count, false);

		// Remove sources that match the given set of context ids to delete
		m_sources.Remove(context_ids, include_count, exclude_count);
	}

	// Delete all objects not displayed in any windows
	void Context::DeleteUnused(Guid const* context_ids, int include_count, int exclude_count)
	{
		// Build a set of context ids, included in 'context_ids', and not used in any windows
		GuidSet unused;

		// Initialise 'unused' with all context ids (filtered by 'context_ids')
		for (auto& src : m_sources.Sources())
		{
			if (!IncludeFilter(src.first, context_ids, include_count, exclude_count)) continue;
			unused.insert(src.first);
		}

		// Remove those that are used in the windows
		for (auto& wnd :m_wnd_cont)
		{
			for (auto& id : wnd->m_guids)
				unused.erase(id);
		}

		// Remove unused sources
		if (!unused.empty())
		{
			pr::vector<Guid> ids(std::begin(unused), std::end(unused));
			m_sources.Remove(ids.data(), s_cast<int>(ids.ssize()), 0, ScriptSources::EReason::Removal);
		}
	}

	// Enumerate the GUIDs in the sources collection
	void Context::SourceEnumGuids(StaticCB<bool, GUID const&> enum_guids_cb)
	{
		for (auto& src : m_sources.Sources())
			if (!enum_guids_cb(src.second.m_context_id))
				return;
	}
	
	// Create a gizmo object and add it to the gizmo collection
	LdrGizmo* Context::GizmoCreate(ELdrGizmoMode mode, m4x4 const& o2w)
	{
		return m_sources.CreateGizmo(mode, o2w);
	}
	
	// Destroy a gizmo
	void Context::GizmoDelete(LdrGizmo* gizmo)
	{
		// Remove the gizmo from any windows it's in
		for (auto& wnd : m_wnd_cont)
			wnd->Remove(gizmo);
		
		// Delete the gizmo from the sources
		m_sources.RemoveGizmo(gizmo);
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

	// Create an embedded code handler for the given language
	std::unique_ptr<Context::IEmbeddedCode> Context::CreateCodeHandler(wchar_t const* lang)
	{
		// Embedded code handler that buffers support code and forwards to a provided code handler function
		struct EmbeddedCode :IEmbeddedCode
		{
			using EmbeddedCodeHandlerCB = StaticCB<view3d::EmbeddedCodeHandlerCB>;

			std::wstring m_lang;
			std::wstring m_code;
			std::wstring m_support;
			EmbeddedCodeHandlerCB m_handler;
			
			EmbeddedCode(wchar_t const* lang, EmbeddedCodeHandlerCB handler)
				:m_lang(lang)
				,m_code()
				,m_support()
				,m_handler(handler)
			{}

			// The language code that this handler is for
			wchar_t const* Lang() const override
			{
				return m_lang.c_str();
			}

			// A handler function for executing embedded code
			// 'code' is the code source
			// 'support' is true if the code is support code
			// 'result' is the output of the code after execution, converted to a string.
			// Return true, if the code was executed successfully, false if not handled.
			// If the code can be handled but has errors, throw 'std::exception's.
			bool Execute(wchar_t const* code, bool support, script::string_t& result) override
			{
				if (support)
				{
					// Accumulate support code
					m_support.append(code);
				}
				else
				{
					// Return false if the handler did not handle the given code
					BSTR_t res, err;
					if (m_handler(code, m_support.c_str(), res, err) == 0)
						return false;

					// If errors are reported, raise them as an exception
					if (err != nullptr)
						throw std::runtime_error(Narrow(err.wstr()));

					// Add the string result to 'result'
					if (res != nullptr)
						result.assign(res, res.size());
				}
				return true;
			}
		};

		auto hash = hash::HashICT(lang);

		// Lua code
		if (hash == hash::HashICT(L"Lua"))
			return std::unique_ptr<script::EmbeddedLua>(new script::EmbeddedLua());

		// Look for a code handler for this language
		for (auto& emb : m_emb)
		{
			if (emb.m_lang != hash) continue;
			return std::unique_ptr<EmbeddedCode>(new EmbeddedCode(lang, emb.m_cb));
		}

		// No code handler found, unsupported
		throw std::runtime_error(FmtS("Unsupported embedded code language: %S", lang));
	}

	// Add an embedded code handler for 'lang'
	void Context::SetEmbeddedCodeHandler(char const* lang, StaticCB<view3d::EmbeddedCodeHandlerCB> embedded_code_cb, bool add)
	{
		auto hash = hash::HashICT(lang);
		pr::erase_if(m_emb, [=](auto& emb){ return emb.m_lang == hash; });
		if (add) m_emb.push_back(EmbCodeCB{hash, embedded_code_cb});
	}

	// Return the context id for objects created from 'filepath' (if filepath is an existing source)
	Guid const* Context::ContextIdFromFilepath(char const* filepath) const
	{
		return m_sources.ContextIdFromFilepath(filepath);
	}
}