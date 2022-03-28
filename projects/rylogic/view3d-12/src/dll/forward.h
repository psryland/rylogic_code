//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once

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
#include "pr/script/embedded_lua.h"
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
