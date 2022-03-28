//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
module;

#include "src/dll/forward.h"

export module View3d.dll:Forward;
import View3d;

export namespace pr::view3d
{
	struct Context;
	struct Window;
	using Renderer              = pr::rdr12::Renderer;
	using RdrSettings           = pr::rdr12::Settings;
	using EDebugLayer           = pr::rdr12::EDebugLayer;
	using seconds_t             = std::chrono::duration<double, std::ratio<1, 1>>;
	using time_point_t          = std::chrono::system_clock::time_point;
	using GuidCont              = std::vector<Guid>;
	//using EditorPtr             = std::unique_ptr<pr::ldr::ScriptEditorUI>;
	//using CodeHandlerPtr        = std::unique_ptr<pr::script::IEmbeddedCode>;
	using ObjectSet             = std::unordered_set<View3DObject>;
	using GizmoSet              = std::unordered_set<View3DGizmo>;
	using GuidSet               = std::unordered_set<Guid>;
	//using EditorCont            = std::unordered_set<EditorPtr>;
	using LockGuard             = std::lock_guard<std::recursive_mutex>;
	using OnAddCB               = std::function<void(Guid const&, bool)>;
	//using SettingsChangedCB     = pr::StaticCB<void, Window*, EView3DSettings>;
	using AddFileProgressCB     = pr::StaticCB<void, Guid const&, wchar_t const*, long long, BOOL, BOOL*>;
	using SourcesChangedCB      = pr::StaticCB<void, EView3DSourcesChangedReason, BOOL>;
	using EmbeddedCodeHandlerCB = pr::StaticCB<BOOL, wchar_t const*, wchar_t const*, BSTR&, BSTR&>;
	using InvalidatedCB         = pr::StaticCB<void, Window*>;
	using RenderingCB           = pr::StaticCB<void, Window*>;
	//using SceneChangedCB        = pr::StaticCB<void, Window*, View3DSceneChanged const&>;
	//using AnimationCB           = pr::StaticCB<void, Window*, EView3DAnimCommand, double>;
	using ReportErrorCB         = pr::StaticCB<void, wchar_t const*, wchar_t const*, int, int64_t>;

}
