//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
module;

#include "src/dll/forward.h"
#include "pr/view3d-12/view3d.h"

export module View3d.dll:Context;
import :Forward;
import :Window;
import View3d;

namespace pr::view3d
{
	struct Context
	{
		using InitSet = std::unordered_set<View3DContext>;
		using WindowCont = std::vector<std::unique_ptr<Window>>;
		using EmbCodeCB = struct { int m_lang; EmbeddedCodeHandlerCB m_cb; };
		using EmbCodeCBCont = std::vector<EmbCodeCB>;
		using IEmbeddedCode = pr::script::IEmbeddedCode;
		using ScriptSources = pr::ldr::ScriptSources;
		using Includes = pr::script::Includes;

		inline static Guid const GuidDemoSceneObjects = { 0xFE51C164, 0x9E57, 0x456F, 0x9D, 0x8D, 0x39, 0xE3, 0xFA, 0xAF, 0xD3, 0xE7 };

		Renderer             m_rdr;             // The renderer
		WindowCont           m_wnd_cont;        // The created windows
		//ScriptSources        m_sources;         // A container of Ldr objects and a file watcher
		InitSet              m_inits;           // A unique id assigned to each Initialise call
		EmbCodeCBCont        m_emb;             // Embedded code execution callbacks
		std::recursive_mutex m_mutex;

		explicit Context(HINSTANCE instance, ReportErrorCB global_error_cb, EDebugLayer debug_flags)
			:m_rdr(RdrSettings(instance, D3D_FEATURE_LEVEL_11_0, debug_flags))
			,m_wnd_cont()
			//,m_sources(m_rdr, [this](auto lang){ return CreateHandler(lang); })
			,m_inits()
			,m_emb()
			,m_mutex()
			,ReportError()
		{
			PR_ASSERT(PR_DBG, pr::meta::is_aligned_to<16>(this), "dll data not aligned");
			ReportError += global_error_cb;

			//// Hook up the sources events
			//m_sources.OnAddFileProgress += [&](ScriptSources&, ScriptSources::AddFileProgressEventArgs& args)
			//{
			//	auto context_id  = args.m_context_id;
			//	auto filepath    = args.m_loc.Filepath();
			//	auto file_offset = args.m_loc.Pos();
			//	auto complete    = args.m_complete;
			//	BOOL cancel      = FALSE;
			//	OnAddFileProgress(context_id, filepath.c_str(), file_offset, complete, &cancel);
			//	args.m_cancel = cancel != 0;
			//};
			//m_sources.OnReload += [&](ScriptSources&, EmptyArgs const&)
			//{
			//	OnSourcesChanged(EView3DSourcesChangedReason::Reload, true);
			//};
			//m_sources.OnSourceRemoved += [&](ScriptSources&, ScriptSources::SourceRemovedEventArgs const& args)
			//{
			//	auto reload = args.m_reason == ScriptSources::EReason::Reload;

			//	// When a source is about to be removed, remove it's objects from the windows.
			//	// If this is a reload, save a reference to the removed objects so we know what to reload.
			//	for (auto& wnd : m_wnd_cont)
			//		wnd->RemoveObjectsById(&args.m_context_id, 1, false, reload);
			//};
			//m_sources.OnStoreChange += [&](ScriptSources&, ScriptSources::StoreChangeEventArgs const& args)
			//{
			//	if (args.m_before)
			//		return;

			//	switch (args.m_reason)
			//	{
			//	default:
			//		throw std::exception("Unknown store changed reason");
			//
			//	// On NewData, do nothing. Callers will add objects to windows as they see fit.
			//	case ScriptSources::EReason::NewData:
			//		break;

			//	// On Removal, do nothing. Removed objects should already have been removed from the windows.
			//	case ScriptSources::EReason::Removal:
			//		break;

			//	// On Reload, for each object currently in the window and in the set of affected context ids, remove and re-add.
			//	case ScriptSources::EReason::Reload:
			//		{
			//			for (auto& wnd : m_wnd_cont)
			//			{
			//				wnd->AddObjectsById(args.m_context_ids.data(), static_cast<int>(args.m_context_ids.size()), 0);
			//				wnd->Invalidate();
			//			}
			//			break;
			//		}
			//	}

			//	// Notify of updated sources
			//	OnSourcesChanged(static_cast<EView3DSourcesChangedReason>(args.m_reason), false);
			//};
			//m_sources.OnError += [&](ScriptSources&, ScriptSources::ParseErrorEventArgs const& args)
			//{
			//	ReportError(args.m_msg.c_str(), args.m_loc.Filepath().c_str(), args.m_loc.Line(), args.m_loc.Pos());
			//};
		}

		Context(Context const&) = delete;
		Context& operator=(Context const&) = delete;
		Context* This() { return this; }

		// Global error callback. Can be called in a worker thread context
		MultiCast<ReportErrorCB, true> ReportError;

		// Event raised when script sources are parsed during adding/updating
		MultiCast<AddFileProgressCB, true> OnAddFileProgress;

		// Event raised when the script sources are updated
		MultiCast<SourcesChangedCB, true> OnSourcesChanged;

	};
}