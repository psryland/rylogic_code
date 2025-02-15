﻿//*********************************************
// View3D
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include <exception>
#include <unordered_set>
#include <mutex>
#include <thread>
#include <chrono>

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
#include "pr/common/algorithm.h"
#include "pr/common/static_callback.h"
#include "pr/common/event_handler.h"
#include "pr/common/cast.h"
#include "pr/common/bstr_t.h"
#include "pr/common/flags_enum.h"
#include "pr/container/vector.h"
#include "pr/container/span.h"
#include "pr/container/byte_data.h"
#include "pr/filesys/filewatch.h"
#include "pr/script/reader.h"
#include "pr/script/embedded_lua.h"
#include "pr/macros/count_of.h"
#include "pr/meta/alignment_of.h"
#include "pr/maths/maths.h"
#include "pr/camera/camera.h"
#include "pr/gui/scintilla_ctrl.h"
#include "pr/view3d/renderer.h"
#include "pr/view3d/lights/light_ui.h"
#include "pr/view3d/util/dx9_context.h"
#include "pr/ldraw/ldr_object.h"
#include "pr/ldraw/ldr_objects_dlg.h"
#include "pr/ldraw/ldr_gizmo.h"
#include "pr/ldraw/ldr_tools.h"
#include "pr/ldraw/ldr_script_editor_dlg.h"
#include "pr/ldraw/ldr_sources.h"
#include "pr/ldraw/ldr_helper.h"
#include "pr/win32/win32.h"
#include "pr/win32/key_codes.h"

#include "pr/view3d/dll/view3d.h"
#include "pr/view3d/dll/conversion.h"

namespace view3d
{
	struct Context;
	struct Window;
	using Renderer              = pr::rdr::Renderer;
	using seconds_t             = std::chrono::duration<double, std::ratio<1, 1>>;
	using time_point_t          = std::chrono::system_clock::time_point;
	using GuidCont              = pr::vector<GUID>;
	using EditorPtr             = std::unique_ptr<pr::ldr::ScriptEditorUI>;
	using CodeHandlerPtr        = std::unique_ptr<pr::script::IEmbeddedCode>;
	using ObjectSet             = std::unordered_set<View3DObject>;
	using GizmoSet              = std::unordered_set<View3DGizmo>;
	using GuidSet               = std::unordered_set<GUID>;
	using EditorCont            = std::unordered_set<EditorPtr>;
	using LockGuard             = std::lock_guard<std::recursive_mutex>;
	using OnAddCB               = std::function<void(pr::Guid const&, bool)>;
	using SettingsChangedCB     = pr::StaticCB<void, Window*, EView3DSettings>;
	using AddFileProgressCB     = pr::StaticCB<void, pr::Guid const&, wchar_t const*, long long, BOOL, BOOL*>;
	using SourcesChangedCB      = pr::StaticCB<void, EView3DSourcesChangedReason, BOOL>;
	using EmbeddedCodeHandlerCB = pr::StaticCB<BOOL, wchar_t const*, wchar_t const*, BSTR&, BSTR&>;
	using InvalidatedCB         = pr::StaticCB<void, Window*>;
	using RenderingCB           = pr::StaticCB<void, Window*>;
	using SceneChangedCB        = pr::StaticCB<void, Window*, View3DSceneChanged const&>;
	using AnimationCB           = pr::StaticCB<void, Window*, EView3DAnimCommand, double>;
	//using ReportErrorCB         = void(__stdcall*)(void*, wchar_t const*, wchar_t const*, int, int64_t);

	// An instance type for other models used in LDraw
	#define PR_RDR_INST(x)\
		x(pr::m4x4          ,m_i2w   ,pr::rdr::EInstComp::I2WTransform)\
		x(pr::rdr::ModelPtr ,m_model ,pr::rdr::EInstComp::ModelPtr)\
		x(pr::Colour32      ,m_tint  ,pr::rdr::EInstComp::TintColour32)
	PR_RDR_DEFINE_INSTANCE(Instance, PR_RDR_INST)
	#undef PR_RDR_INST

	// An instance type for the focus point and origin point models
	#define PR_RDR_INST(x)\
		x(pr::m4x4          ,m_c2s   ,pr::rdr::EInstComp::C2STransform)\
		x(pr::m4x4          ,m_i2w   ,pr::rdr::EInstComp::I2WTransform)\
		x(pr::rdr::ModelPtr ,m_model ,pr::rdr::EInstComp::ModelPtr)\
		x(pr::Colour32      ,m_tint  ,pr::rdr::EInstComp::TintColour32)
	PR_RDR_DEFINE_INSTANCE(PointInstance, PR_RDR_INST)
	#undef PR_RDR_INST
}

// Maths type traits
namespace pr::maths
{
	template <> struct is_vec<View3DV2> :std::true_type
	{
		using elem_type = float;
		using cp_type = float;
		static int const dim = 2;
	};
	template <> struct is_vec<View3DV4> :std::true_type
	{
		using elem_type = float;
		using cp_type = float;
		static int const dim = 4;
	};
	template <> struct is_vec<View3DM4x4> :std::true_type
	{
		using elem_type = View3DV4;
		using cp_type = typename is_vec<View3DV4>::cp_type;
		static int const dim = 4;
	};
}
