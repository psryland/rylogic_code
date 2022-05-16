//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/view3d-dll.h"
#include "pr/view3d-12/ldraw/ldr_object.h"
#include "pr/view3d-12/ldraw/ldr_gizmo.h"
#include "view3d-12/src/dll/context.h"
#include "view3d-12/src/dll/v3d_window.h"

namespace pr::rdr12
{
	Context::Context(HINSTANCE instance, ReportErrorCB global_error_cb)
		:m_rdr(RdrSettings(instance).DebugLayer(PR_DBG_RDR).DefaultAdapter())
		,m_wnd_cont()
		,m_sources(m_rdr, [this](auto lang){ return CreateHandler(lang); })
		,m_inits()
		,m_emb()
		,m_mutex()
		,ReportError()
		,OnAddFileProgress()
		,OnSourcesChanged()
	{
		PR_ASSERT(PR_DBG, meta::is_aligned_to<16>(this), "dll data not aligned");
		ReportError += global_error_cb;

		#if 0 // todo
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
				{
					for (auto& wnd : m_wnd_cont)
					{
						wnd->AddObjectsById(args.m_context_ids.data(), static_cast<int>(args.m_context_ids.size()), 0);
						wnd->Invalidate();
					}
					break;
				}
			}

			// Notify of updated sources
			OnSourcesChanged(static_cast<EView3DSourcesChangedReason>(args.m_reason), false);
		};
		m_sources.OnError += [&](ScriptSources&, ScriptSources::ParseErrorEventArgs const& args)
		{
			ReportError(args.m_msg.c_str(), args.m_loc.Filepath().c_str(), args.m_loc.Line(), args.m_loc.Pos());
		};
		#endif
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
		auto msg = Fmt<pr::string<wchar_t>>(L"%S failed.\n%S", func_name, ex ? ex->what() : "Unknown exception occurred.");
		if (msg.last() != '\n')
			msg.push_back('\n');

		// If a window handle is provided, report via the window's event.
		// Otherwise, fall back to the global error handler
		if (wnd != nullptr)
			wnd->ReportError(msg.c_str(), L"", 0, 0);
		else
			ReportError(msg.c_str(), L"", 0, 0);
	}

	// Create/Destroy windows
	V3dWindow* Context::WindowCreate(HWND hwnd, view3d::WindowOptions const& opts)
	{
		try
		{
			V3dWindow* win;
			m_wnd_cont.emplace_back(win = new V3dWindow(hwnd, *this, opts));
			return win;
		}
		catch (std::exception const& e)
		{
			if (opts.m_error_cb) opts.m_error_cb(opts.m_error_cb_ctx, FmtS(L"Failed to create View3D Window.\n%S", e.what()), L"", 0, 0);
			return nullptr;
		}
		catch (...)
		{
			if (opts.m_error_cb) opts.m_error_cb(opts.m_error_cb_ctx, FmtS(L"Failed to create View3D Window.\nUnknown reason"), L"", 0, 0);
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
	Guid Context::LoadScript(std::basic_string_view<Char> ldr_script, bool file, EEncoding enc, Guid const* context_id, script::Includes const& includes, OnAddCB on_add) // worker thread context
	{
		return m_sources.Add(ldr_script, file, enc, ScriptSources::EReason::NewData, context_id, includes, on_add);
	}
	template Guid Context::LoadScript<wchar_t>(std::wstring_view ldr_script, bool file, EEncoding enc, Guid const* context_id, script::Includes const& includes, OnAddCB on_add);
	template Guid Context::LoadScript<char>(std::string_view ldr_script, bool file, EEncoding enc, Guid const* context_id, script::Includes const& includes, OnAddCB on_add);

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

	// Create an embedded code handler for the given language
	std::unique_ptr<Context::IEmbeddedCode> Context::CreateHandler(wchar_t const* lang)
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
		throw std::exception(FmtS("Unsupported embedded code language: %S", lang));
	}
}