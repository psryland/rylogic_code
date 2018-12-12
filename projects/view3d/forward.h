//*********************************************
// View3D
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include <exception>
#include <unordered_set>
#include <mutex>
#include <thread>

#include <sdkddkver.h>
#include <windows.h>
#include <d3d11.h>

#ifndef _WIN32_WINNT 
#define _WIN32_WINNT _WIN32_WINNT_WIN7
#elif _WIN32_WINNT < _WIN32_WINNT_WINXP 
#error "_WIN32_WINNT >= _WIN32_WINNT_WINXP required"
#endif

#include "pr/common/min_max_fix.h"
#include "pr/common/fmt.h"
#include "pr/common/log.h"
#include "pr/common/hresult.h"
#include "pr/common/assert.h"
#include "pr/common/new.h"
#include "pr/common/algorithm.h"
#include "pr/common/static_callback.h"
#include "pr/common/multi_cast.h"
#include "pr/common/cast.h"
#include "pr/common/bstr_t.h"
#include "pr/container/vector.h"
#include "pr/container/span.h"
#include "pr/container/byte_data.h"
#include "pr/filesys/fileex.h"
#include "pr/filesys/filewatch.h"
#include "pr/script/reader.h"
#include "pr/script/embedded_lua.h"
#include "pr/macros/count_of.h"
#include "pr/meta/alignment_of.h"
#include "pr/maths/maths.h"
#include "pr/camera/camera.h"
#include "pr/gui/scintilla_ctrl.h"
#include "pr/renderer11/renderer.h"
#include "pr/renderer11/lights/light_ui.h"
#include "pr/ldraw/ldr_object.h"
#include "pr/ldraw/ldr_objects_dlg.h"
#include "pr/ldraw/ldr_gizmo.h"
#include "pr/ldraw/ldr_tools.h"
#include "pr/ldraw/ldr_script_editor_dlg.h"
#include "pr/ldraw/ldr_sources.h"
#include "pr/ldraw/ldr_helper.h"
#include "pr/win32/win32.h"

#include "pr/view3d/view3d.h"
#include "pr/view3d/pr_conv.h"

namespace view3d
{
	struct Context;
	struct Window;
	using GuidCont              = pr::vector<GUID>;
	using EditorPtr             = std::unique_ptr<pr::ldr::ScriptEditorUI>;
	using CodeHandlerPtr        = std::unique_ptr<pr::script::IEmbeddedCode>;
	using ObjectSet             = std::unordered_set<View3DObject>;
	using GizmoSet              = std::unordered_set<View3DGizmo>;
	using GuidSet               = std::unordered_set<GUID>;
	using EditorCont            = std::unordered_set<EditorPtr>;
	using LockGuard             = std::lock_guard<std::recursive_mutex>;
	using ReportErrorCB         = pr::StaticCB<void, wchar_t const*>;
	using SettingsChangedCB     = pr::StaticCB<void, Window*>;
	using AddFileProgressCB     = pr::StaticCB<BOOL, pr::Guid const&, wchar_t const*, long long, BOOL>;
	using SourcesChangedCB      = pr::StaticCB<void, EView3DSourcesChangedReason, BOOL>;
	using EmbeddedCodeHandlerCB = pr::StaticCB<BOOL, wchar_t const*, wchar_t const*, BSTR&, BSTR&>;
	using RenderingCB           = pr::StaticCB<void, Window*>;
	using SceneChangedCB        = pr::StaticCB<void, Window*, View3DSceneChanged const&>;
	using ReportErrorCB         = pr::StaticCB<void, wchar_t const*>;

	// An instance type for other models used in LDraw
	#define PR_RDR_INST(x)\
		x(pr::m4x4          ,m_i2w   ,pr::rdr::EInstComp::I2WTransform)\
		x(pr::rdr::ModelPtr ,m_model ,pr::rdr::EInstComp::ModelPtr)
	PR_RDR_DEFINE_INSTANCE(Instance, PR_RDR_INST)
	#undef PR_RDR_INST

	// An instance type for the focus point and origin point models
	#define PR_RDR_INST(x)\
		x(pr::m4x4          ,m_c2s   ,pr::rdr::EInstComp::C2STransform)\
		x(pr::m4x4          ,m_i2w   ,pr::rdr::EInstComp::I2WTransform)\
		x(pr::rdr::ModelPtr ,m_model ,pr::rdr::EInstComp::ModelPtr)
	PR_RDR_DEFINE_INSTANCE(PointInstance, PR_RDR_INST)
	#undef PR_RDR_INST
}
