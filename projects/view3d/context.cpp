//***************************************************************************************************
// View 3D
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************

#include "view3d/forward.h"
#include "view3d/context.h"
#include "view3d/window.h"
#include "pr/view3d/view3d.h"

using namespace pr;
using namespace pr::rdr;
using namespace pr::ldr;
using namespace pr::script;

namespace view3d
{
	pr::Guid const Context::GuidDemoSceneObjects = { 0xFE51C164, 0x9E57, 0x456F, 0x9D, 0x8D, 0x39, 0xE3, 0xFA, 0xAF, 0xD3, 0xE7 };

	// Constructor
	Context::Context(HINSTANCE instance, ReportErrorCB global_error_cb, D3D11_CREATE_DEVICE_FLAG device_flags)
		:m_inits()
		,m_rdr(RdrSettings(instance, device_flags))
		,m_wnd_cont()
		,m_sources(m_rdr, [this](auto lang){ return CreateHandler(lang); })
		,m_emb()
		,m_mutex()
		,ReportError()
	{
		PR_ASSERT(PR_DBG, pr::meta::is_aligned_to<16>(this), "dll data not aligned");
		ReportError += global_error_cb;

		// Hook up the sources events
		m_sources.OnAddFileProgress += [&](ScriptSources&, ScriptSources::AddFileProgressEventArgs& args)
		{
			auto context_id  = args.m_context_id;
			auto filepath    = args.m_loc.Filepath();
			auto file_offset = args.m_loc.Pos();
			auto complete    = args.m_complete;
			BOOL cancel      = FALSE;
			OnAddFileProgress(context_id, filepath.c_str(), file_offset, complete, &cancel);
			args.m_cancel = cancel != 0;
		};
		m_sources.OnReload += [&](ScriptSources&, EmptyArgs const&)
		{
			OnSourcesChanged(EView3DSourcesChangedReason::Reload, true);
		};
		m_sources.OnSourceRemoved += [&](ScriptSources&, ScriptSources::SourceRemovedEventArgs const& args)
		{
			auto reload = args.m_reason == ScriptSources::EReason::Reload;

			// When a source is about to be removed, remove it's objects from the windows.
			// If this is a reload, save a reference to the removed objects so we know what to reload.
			for (auto& wnd : m_wnd_cont)
				wnd->RemoveObjectsById(&args.m_context_id, 1, false, reload);
		};
		m_sources.OnStoreChange += [&](ScriptSources&, ScriptSources::StoreChangeEventArgs const& args)
		{
			if (args.m_before)
				return;

			switch (args.m_reason)
			{
			default:
				throw std::exception("Unknown store changed reason");
			
			// On NewData, do nothing. Callers will add objects to windows as they see fit.
			case ScriptSources::EReason::NewData:
				break;

			// On Removal, do nothing. Removed objects should already have been removed from the windows.
			case ScriptSources::EReason::Removal:
				break;

			// On Reload, for each object currently in the window and in the set of affected context ids, remove and re-add.
			case ScriptSources::EReason::Reload:
				for (auto& wnd : m_wnd_cont)
					wnd->AddObjectsById(args.m_context_ids.data(), static_cast<int>(args.m_context_ids.size()), 0);

				break;
			}

			OnSourcesChanged(static_cast<EView3DSourcesChangedReason>(args.m_reason), false);
		};
		m_sources.OnError += [&](ScriptSources&, pr::ErrorEventArgs const& args)
		{
			ReportError(args.m_msg.c_str());
		};
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

	// Report an error handled at the DLL API layer
	void Context::ReportAPIError(char const* func_name, View3DWindow wnd, std::exception const* ex)
	{
		// Create the error message
		auto msg = pr::Fmt<pr::string<wchar_t>>(L"%S failed.\n%S", func_name, ex ? ex->what() : "Unknown exception occurred.");
		if (msg.last() != '\n')
			msg.push_back('\n');

		// If a window handle is provided, report via the window's event.
		// Otherwise, fallback to the global error handler
		if (wnd != nullptr)
			wnd->ReportError(msg.c_str());
		else
			ReportError(msg.c_str());
	}

	// Load/Add ldr objects from a script string. Returns the Guid of the context that the objects were added to.
	pr::Guid Context::LoadScript(std::wstring_view ldr_script, bool file, pr::EEncoding enc, pr::Guid const* context_id, Includes const& includes, OnAddCB on_add) // worker thread context
	{
		return m_sources.Add(ldr_script, file, enc, ScriptSources::EReason::NewData, context_id, includes, on_add);
	}
	pr::Guid Context::LoadScript(std::string_view ldr_script, bool file, pr::EEncoding enc, pr::Guid const* context_id, Includes const& includes, OnAddCB on_add) // worker thread context
	{
		return m_sources.Add(ldr_script, file, enc, ScriptSources::EReason::NewData, context_id, includes, on_add);
	}

	// Load/Add ldr objects and return the first object from the script
	LdrObject* Context::ObjectCreateLdr(std::wstring_view ldr_script, bool file, pr::EEncoding enc, pr::Guid const* context_id, pr::script::Includes const& includes)
	{
		// Get the context id for this script
		auto id = context_id ? *context_id : pr::GenerateGUID();

		// Record how many objects there are already for the context id (if it exists)
		auto& srcs = m_sources.Sources();
		auto iter = srcs.find(id);
		auto count = iter != std::end(srcs) ? iter->second.m_objects.size() : 0U;

		// Load the ldr script
		LoadScript(ldr_script, file, enc, &id, includes, nullptr);

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
		using namespace pr::script;
		auto geom = EGeom::None;

		// Generate the nuggets first so we can tell what geometry data is needed
		pr::vector<pr::rdr::NuggetProps> ngt;
		for (auto n = nuggets, nend = n + ncount; n != nend; ++n)
		{
			// Create the renderer nugget
			NuggetProps nug;
			nug.m_topo = static_cast<EPrim>(n->m_topo);
			nug.m_geom = static_cast<EGeom>(n->m_geom);
			if (n->m_cull_mode != EView3DCullMode::Default) nug.m_rsb.Set(ERS::CullMode, static_cast<D3D11_CULL_MODE>(n->m_cull_mode));
			if (n->m_fill_mode != EView3DFillMode::Default) nug.m_rsb.Set(ERS::FillMode, static_cast<D3D11_FILL_MODE>(n->m_fill_mode));
			nug.m_vrange = n->m_v0 != n->m_v1 ? rdr::Range(n->m_v0, n->m_v1) : rdr::Range(0, vcount);
			nug.m_irange = n->m_i0 != n->m_i1 ? rdr::Range(n->m_i0, n->m_i1) : rdr::Range(0, icount);
			nug.m_flags = static_cast<ENuggetFlag>(n->m_flags);
			nug.m_tex_diffuse = Texture2DPtr(n->m_mat.m_diff_tex, true);
			nug.m_range_overlaps = n->m_range_overlaps;
			nug.m_tint = n->m_mat.m_tint;
		
			for (int rs = 1; rs != ERenderStep_::NumberOf; ++rs)
			{
				auto& rstep0 = n->m_mat.m_shader_map.m_rstep[rs];
				auto& rstep1 = nug.m_smap[static_cast<ERenderStep>(rs)];
				{// VS
					switch (rstep0.m_vs.shdr)
					{
					default: throw std::runtime_error("Unknown vertex shader");
					case EView3DShaderVS::Standard: break;
					}
				}
				{// PS
					switch (rstep0.m_ps.shdr)
					{
					default: throw std::runtime_error("Unknown pixel shader");
					case EView3DShaderPS::Standard: break;
					case EView3DShaderPS::RadialFadePS:
						{
							Reader reader(rstep0.m_ps.params);
							auto type = reader.Keyword(L"Type").EnumS<pr::rdr::ERadial>();
							auto radius = reader.Keyword(L"Radius").Vector2S();
							auto centre = reader.FindKeyword(L"Centre") ? reader.Vector3S(1) : pr::v4Zero;
							auto focus_relative = reader.FindKeyword(L"Absolute") == false;
							auto id = pr::hash::Hash("RadialFadePS", centre, radius, type, focus_relative);
							auto shdr = m_rdr.m_shdr_mgr.GetShader<FwdRadialFadePS>(id, RdrId(EStockShader::FwdRadialFadePS));
							shdr->m_fade_centre = centre;
							shdr->m_fade_radius = radius;
							shdr->m_fade_type = type;
							shdr->m_focus_relative = focus_relative;
							rstep1.m_ps = shdr;
							break;
						}
					}
				}
				{// GS
					switch (rstep0.m_gs.shdr)
					{
					default: throw std::runtime_error("Unknown geometry shader");
					case EView3DShaderGS::Standard: break;
					case EView3DShaderGS::PointSpritesGS:
						{
							Reader reader(rstep0.m_gs.params);
							auto point_size = reader.Keyword(L"PointSize").Vector2S();
							auto depth = reader.Keyword(L"Depth").BoolS<bool>();
							auto id = pr::hash::Hash("PointSprites", point_size, depth);
							auto shdr = m_rdr.m_shdr_mgr.GetShader<PointSpritesGS>(id, RdrId(EStockShader::PointSpritesGS));
							shdr->m_size = point_size;
							shdr->m_depth = depth;
							rstep1.m_gs = shdr;
							break;
						}
					case EView3DShaderGS::ThickLineListGS:
						{
							Reader reader(rstep0.m_gs.params);
							auto line_width = reader.Keyword(L"LineWidth").RealS<float>();
							auto id = pr::hash::Hash("ThickLineList", line_width);
							auto shdr = m_rdr.m_shdr_mgr.GetShader<ThickLineListGS>(id, RdrId(EStockShader::ThickLineListGS));
							shdr->m_width = line_width;
							rstep1.m_gs = shdr;
							break;
						}
					case EView3DShaderGS::ThickLineStripGS:
						{
							Reader reader(rstep0.m_gs.params);
							auto line_width = reader.Keyword(L"LineWidth").RealS<float>();
							auto id = pr::hash::Hash("ThickLineStrip", line_width);
							auto shdr = m_rdr.m_shdr_mgr.GetShader<ThickLineStripGS>(id, RdrId(EStockShader::ThickLineStripGS));
							shdr->m_width = line_width;
							rstep1.m_gs = shdr;
							break;
						}
					case EView3DShaderGS::ArrowHeadGS:
						{
							Reader reader(rstep0.m_gs.params);
							auto size = reader.Keyword(L"Size").RealS<float>();
							auto id = pr::hash::Hash("ArrowHead", size);
							auto shdr = m_rdr.m_shdr_mgr.GetShader<ArrowHeadGS>(id, RdrId(EStockShader::ArrowHeadGS));
							shdr->m_size = size;
							rstep1.m_gs = shdr;
							break;
						}
					}
				}
				{// CS
					switch (rstep0.m_cs.shdr)
					{
					default: throw std::runtime_error("Unknown compute shader");
					case EView3DShaderCS::None: break;
					}
				}
			}
			ngt.push_back(nug);

			// Sanity check the nugget
			PR_ASSERT(PR_DBG, nug.m_vrange.begin() <= nug.m_vrange.end() && int(nug.m_vrange.end()) <= vcount, "Invalid nugget V-range");
			PR_ASSERT(PR_DBG, nug.m_irange.begin() <= nug.m_irange.end() && int(nug.m_irange.end()) <= icount, "Invalid nugget I-range");

			// Union of geometry data type
			geom |= nug.m_geom;
		}

		// Vertex buffer
		pr::vector<pr::v4> pos;
		{
			pos.resize(vcount);
			for (auto i = 0; i != vcount; ++i)
				pos[i] = view3d::To<pr::v4>(verts[i].pos);
		}

		// Colour buffer
		pr::vector<pr::Colour32> col;
		if (pr::AllSet(geom, EGeom::Colr))
		{
			col.resize(vcount);
			for (auto i = 0; i != vcount; ++i)
				col[i] = verts[i].col;
		}

		// Normals
		pr::vector<pr::v4> nrm;
		if (pr::AllSet(geom, EGeom::Norm))
		{
			nrm.resize(vcount);
			for (auto i = 0; i != vcount; ++i)
				nrm[i] = view3d::To<pr::v4>(verts[i].norm);
		}

		// Texture coords
		pr::vector<pr::v2> tex;
		if (pr::AllSet(geom, EGeom::Tex0))
		{
			tex.resize(vcount);
			for (auto i = 0; i != vcount; ++i)
				tex[i] = view3d::To<pr::v2>(verts[i].tex);
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
		StringSrc src(ldr_script);
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
	void Context::DeleteAllObjectsById(pr::Guid const* context_ids, int include_count, int exclude_count)
	{
		// Remove objects from any windows they might be assigned to
		for (auto& wnd : m_wnd_cont)
			wnd->RemoveObjectsById(context_ids, include_count, exclude_count, false);

		// Remove sources that match the given set of context ids to delete
		m_sources.Remove(context_ids, include_count, exclude_count);
	}

	// Delete all objects not displayed in any windows
	void Context::DeleteUnused(GUID const* context_ids, int include_count, int exclude_count)
	{
		// Build a set of context ids, included in 'context_ids', and not used in any windows
		GuidSet unused;

		// Initialise 'unused' with all context ids (filtered by 'context_ids')
		for (auto& src : m_sources.Sources())
		{
			if (!pr::IncludeFilter(src.first, context_ids, include_count, exclude_count)) continue;
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
			GuidCont ids(std::begin(unused), std::end(unused));
			m_sources.Remove(ids.data(), int(ids.size()), 0);
		}
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
	void Context::ObjectEditCB(Model* model, void* ctx, pr::Renderer&)
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
				n.m_cull_mode = static_cast<EView3DCullMode>(nug.m_rsb.Desc().CullMode);
				n.m_fill_mode = static_cast<EView3DFillMode>(nug.m_rsb.Desc().FillMode);
				n.m_v0 = pr::s_cast<UINT32>(nug.m_vrange.begin());
				n.m_v1 = pr::s_cast<UINT32>(nug.m_vrange.end());
				n.m_i0 = pr::s_cast<UINT32>(nug.m_irange.begin());
				n.m_i1 = pr::s_cast<UINT32>(nug.m_irange.end());
				n.m_mat.m_diff_tex = nug.m_tex_diffuse.m_ptr;
				n.m_mat.m_relative_reflectivity = nug.m_relative_reflectivity;
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
				if (nug.m_cull_mode != EView3DCullMode::Default) mat.m_rsb.Set(ERS::CullMode, static_cast<D3D11_CULL_MODE>(nug.m_cull_mode));
				if (nug.m_fill_mode != EView3DFillMode::Default) mat.m_rsb.Set(ERS::FillMode, static_cast<D3D11_FILL_MODE>(nug.m_fill_mode));
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
		m_sources.Add(scene, false, pr::EEncoding::utf8, ScriptSources::EReason::NewData, &GuidDemoSceneObjects, Includes(), [=](pr::Guid const& id, bool before)
		{
			if (before)
				window->RemoveObjectsById(&id, 1, 0);
			else
				window->AddObjectsById(&id, 1, 0);
		});

		// Position the camera to look at the scene
		View3D_ResetView(window, View3DV4{0.0f, 0.0f, -1.0f, 0.0f}, View3DV4{0.0f, 1.0f, 0.0f, 0.0f}, 0, TRUE, TRUE);
		return GuidDemoSceneObjects;
	}

	// Create an embedded code handler for the given language
	std::unique_ptr<IEmbeddedCode> Context::CreateHandler(wchar_t const* lang)
	{
		// Embedded code handler that buffers support code and forwards to a provided code handler function
		struct EmbeddedCode :IEmbeddedCode
		{
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
			bool Execute(wchar_t const* code, bool support, string_t& result) override
			{
				if (support)
				{
					// Accumulate support code
					m_support.append(code);
				}
				else
				{
					// Return false if the handler did not handle the given code
					pr::bstr_t res, err;
					if (m_handler(code, m_support.c_str(), res, err) == 0)
						return false;

					// If errors are reported, raise them as an exception
					if (err != nullptr)
						throw std::runtime_error(pr::Narrow(err.wstr()));

					// Add the string result to 'result'
					if (res != nullptr)
						result.assign(res, res.size());
				}
				return true;
			}
		};

		auto hash = pr::hash::HashICT(lang);

		// Lua code
		if (hash == pr::hash::HashICT(L"Lua"))
			return std::make_unique<EmbeddedLua>();

		// Look for a code handler for this language
		for (auto& emb : m_emb)
		{
			if (emb.m_lang != hash) continue;
			return std::make_unique<EmbeddedCode>(lang, emb.m_cb);
		}

		// No code handler found, unsupported
		throw std::exception(pr::FmtS("Unsupported embedded code language: %S", lang));
	}

	// Add an embedded code handler for 'lang'
	void Context::SetEmbeddedCodeHandler(wchar_t const* lang, View3D_EmbeddedCodeHandlerCB embedded_code_cb, void* ctx, bool add)
	{
		auto hash = pr::hash::HashICT(lang);
		if (add)
		{
			auto cb = pr::StaticCallBack(embedded_code_cb, ctx);

			// Look for and replace the execution function 
			for (auto& emb : m_emb)
			{
				if (emb.m_lang != hash) continue;
				emb.m_cb = cb;
				return;
			}
			m_emb.push_back(EmbCodeCB{hash, cb});
		}
		else
		{
			pr::erase_if(m_emb, [=](auto& emb){ return emb.m_lang == hash; });
		}
	}
}
