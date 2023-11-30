//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/view3d-dll.h"
#include "pr/view3d-12/ldraw/ldr_object.h"
#include "pr/view3d-12/ldraw/ldr_gizmo.h"
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
		PR_ASSERT(PR_DBG, meta::is_aligned_to<16>(this), "dll data not aligned");
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
	template <typename Char>
	Guid Context::LoadScript(std::basic_string_view<Char> ldr_script, bool file, EEncoding enc, Guid const* context_id, script::Includes const& includes, ScriptSources::OnAddCB on_add) // worker thread context
	{
		return m_sources.Add(ldr_script, file, enc, ScriptSources::EReason::NewData, context_id, includes, on_add);
	}
	template Guid Context::LoadScript<wchar_t>(std::wstring_view ldr_script, bool file, EEncoding enc, Guid const* context_id, script::Includes const& includes, ScriptSources::OnAddCB on_add);
	template Guid Context::LoadScript<char>(std::string_view ldr_script, bool file, EEncoding enc, Guid const* context_id, script::Includes const& includes, ScriptSources::OnAddCB on_add);

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
		auto count = iter != end(srcs) ? iter->second.m_objects.size() : 0U;

		// Load the ldr script
		LoadScript(ldr_script, file, enc, &id, include_handler, nullptr);

		// Return the first object, expecting 'ldr_script' to define one object only.
		// It doesn't matter if more are defined however, they're just created as part of the context.
		iter = srcs.find(id);
		return iter != std::end(srcs) && iter->second.m_objects.size() > count
			? iter->second.m_objects[count].get()
			: nullptr;
	}
	template LdrObject* Context::ObjectCreateLdr<wchar_t>(std::wstring_view ldr_script, bool file, EEncoding enc, Guid const* context_id, view3d::Includes const* includes);
	template LdrObject* Context::ObjectCreateLdr<char>(std::string_view ldr_script, bool file, EEncoding enc, Guid const* context_id, view3d::Includes const* includes);

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

	// Enumerate the Guids in the sources collection
	void Context::SourceEnumGuids(StaticCB<bool, GUID const&> enum_guids_cb)
	{
		for (auto& src : m_sources.Sources())
			if (!enum_guids_cb(src.second.m_context_id))
				return;
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
					bstr_t res, err;
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