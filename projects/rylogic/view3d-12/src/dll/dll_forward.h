//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/view3d-dll.h"

namespace pr::view3d
{
	struct Includes;
}
namespace pr::rdr12
{
	using ObjectSet = std::unordered_set<view3d::Object>;
	using GizmoSet  = std::unordered_set<view3d::Gizmo>;
	using GuidSet   = std::unordered_set<Guid>;

	using OnAddCB = std::function<void(Guid const&, bool)>;
	using ReportErrorCB = pr::StaticCB<void, wchar_t const*, wchar_t const*, int, int64_t>;
	using AddFileProgressCB = pr::StaticCB<void, Guid const&, wchar_t const*, long long, BOOL, BOOL*>;
	using SourcesChangedCB = pr::StaticCB<void, view3d::ESourcesChangedReason, BOOL>;
	using EmbeddedCodeHandlerCB = pr::StaticCB<BOOL, wchar_t const*, wchar_t const*, BSTR&, BSTR&>;
	using SettingsChangedCB = pr::StaticCB<void, V3dWindow*, view3d::ESettings>;
	using InvalidatedCB = pr::StaticCB<void, V3dWindow*>;
	using RenderingCB = pr::StaticCB<void, V3dWindow*>;
	using SceneChangedCB = pr::StaticCB<void, V3dWindow*, view3d::SceneChanged const&>;
	using AnimationCB = pr::StaticCB<void, V3dWindow*, view3d::EAnimCommand, double>;

}


#if 0
#include <stdexcept>
#include <vector>
#include <unordered_set>
#include <mutex>
#include <thread>
#include <chrono>
#include <functional>

#include <sdkddkver.h>
#include <windows.h>
#include <d3d12.h>

#include "pr/common/min_max_fix.h"
//#include "pr/common/fmt.h"
//#include "pr/common/log.h"
#include "pr/common/guid.h"
#include "pr/common/hresult.h"
#include "pr/common/assert.h"
//#include "pr/common/algorithm.h"
#include "pr/common/static_callback.h"
#include "pr/common/event_handler.h"
//#include "pr/common/cast.h"
//#include "pr/common/bstr_t.h"
//#include "pr/common/flags_enum.h"
//#include "pr/container/vector.h"
//#include "pr/container/span.h"
//#include "pr/container/byte_data.h"
//#include "pr/filesys/filewatch.h"
#include "pr/script/reader.h"
//#include "pr/macros/count_of.h"
#include "pr/meta/alignment_of.h"
#include "pr/maths/maths.h"
//#include "pr/camera/camera.h"
//#include "pr/gui/scintilla_ctrl.h"
//#include "pr/view3d/renderer.h"
//#include "pr/view3d/lights/light_ui.h"
//#include "pr/view3d/util/dx9_context.h"
#include "pr/ldraw/ldr_object.h"
#include "pr/ldraw/ldr_objects_dlg.h"
#include "pr/ldraw/ldr_gizmo.h"
#include "pr/ldraw/ldr_tools.h"
#include "pr/ldraw/ldr_script_editor_dlg.h"
#include "pr/ldraw/ldr_sources.h"
#include "pr/ldraw/ldr_helper.h"
//#include "pr/win32/win32.h"
//#include "pr/win32/key_codes.h"
#include "pr/view3d-12/view3d.h"

namespace pr::view3d
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
	//using EditorCont            = std::unordered_set<EditorPtr>;
	using LockGuard             = std::lock_guard<std::recursive_mutex>;
	using AddFileProgressCB     = pr::StaticCB<void, Guid const&, wchar_t const*, long long, BOOL, BOOL*>;
	using SourcesChangedCB      = pr::StaticCB<void, EView3DSourcesChangedReason, BOOL>;

}
#endif
