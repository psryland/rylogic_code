//*****************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
#pragma once

#include <iostream>
#include <string>
#include <sstream>
#include <fstream>
#include <vector>
#include <list>
#include <memory>
#include <guiddef.h>
#include <cguid.h>
#include <thread>

#include "pr/common/min_max_fix.h"
#include "pr/macros/count_of.h"
#include "pr/macros/enum.h"
#include "pr/common/assert.h"
#include "pr/common/exception.h"
#include "pr/common/hresult.h"
#include "pr/common/fmt.h"
#include "pr/common/command_line.h"
#include "pr/common/events.h"
#include "pr/common/new.h"
#include "pr/common/keystate.h"
#include "pr/common/colour.h"
#include "pr/common/scope.h"
#include "pr/common/flags_enum.h"
#include "pr/maths/maths.h"
#include "pr/filesys/fileex.h"
#include "pr/filesys/filesys.h"
#include "pr/camera/camera.h"
#include "pr/camera/camera_ui.h"
#include "pr/gui/wingui.h"
#include "pr/gui/colour_ctrl.h"
#include "pr/gui/messagemap_dbg.h"
#include "pr/gui/sim_message_loop.h"
#include "pr/gui/menu_list.h"
#include "pr/gui/recent_files.h"
#include "pr/gui/progress_ui.h"
#include "pr/gui/scintilla_ctrl.h"
#include "pr/renderer11/renderer.h"
#include "pr/renderer11/lights/light_ui.h"
#include "pr/script/forward.h"
#include "pr/linedrawer/ldr_object.h"
#include "pr/linedrawer/ldr_objects_dlg.h"
#include "pr/linedrawer/ldr_tools.h"
#include "pr/linedrawer/ldr_gizmo.h"
#include "pr/linedrawer/ldr_script_editor_dlg.h"
#include "pr/network/web_get.h"
#include "pr/storage/xml.h"
#include "pr/storage/settings.h"
#include "pr/win32/win32.h"
#include "pr/win32/windows_com.h"

namespace ldr
{
	using namespace pr::gui;

	enum class ELdrException
	{
		NotSpecified,
		FileNotFound,
		FailedToLoad,
		IncorrectVersion,
		InvalidUserSettings,
		SourceScriptError,
		OperationCancelled,
	};

	enum class EFillMode
	{
		Solid,
		Wireframe,
		SolidAndWire,
		NumberOf,
	};

	// Input control mode, Navigation or Manipulation
	enum class EControlMode
	{
		Navigation,
		Manipulation,
	};

	// Stereo view
	enum class EScreenView
	{
		Default,
		Stereo,
	};

	// Modes for bounding groups of objects
	enum class EObjectBounds
	{
		All,
		Selected,
		Visible,
	};

	using LdrException  = pr::Exception<ELdrException>;
	using ContextIdCont = std::vector<pr::Guid>;
	using StrList       = std::list<pr::string<wchar_t>>;

	struct MainUI;
	struct UserSettings;
	class  PluginManager;
	struct Plugin;
	struct NavManager;

	// Application string constants
	wchar_t const* AppTitleW();
	char const*    AppTitleA();
	char const*    AppVersion();
	char const*    AppCopyright();
	char const*    AppString();
	char const*    AppStringLine();

	#pragma region Events

	// Event to signal a refresh of the display
	using Evt_Refresh = pr::ldr::Evt_Refresh;

	// Event raised by the settings system when an error is detected
	using Evt_SettingsError = pr::settings::Evt<UserSettings>;

	// Event raised by the renderer when it's building a scene
	using Evt_UpdateScene = pr::rdr::Evt_UpdateScene;

	// Event to signal report an application message to the user
	struct Evt_AppMsg
	{
		std::wstring m_msg;
		std::wstring m_title;
		MsgBox::EIcon m_icon;

		Evt_AppMsg(std::wstring const& msg, std::wstring const& title, MsgBox::EIcon icon = MsgBox::EIcon::Error)
			:m_msg(msg)
			,m_title(title)
			,m_icon(icon)
		{}
	};

	// Event to update the status bar
	struct Evt_Status
	{
		std::wstring m_msg;
		DWORD        m_duration_ms;
		bool         m_bold;
		COLORREF     m_col;

		Evt_Status(std::wstring const& msg, DWORD duration_ms = INFINITE, bool bold = false, COLORREF col = 0)
			:m_msg(msg)
			,m_duration_ms(duration_ms)
			,m_bold(bold)
			,m_col(col)
		{}
		bool IsTimed() const
		{
			return m_duration_ms != INFINITE;
		}
	};

	// Raised just before parsing begins and 'm_store' is changed
	struct Evt_StoreChanging
	{
		// The store that will be added to
		pr::ldr::ObjectCont const& m_store;

		Evt_StoreChanging(pr::ldr::ObjectCont const& store)
			:m_store(store)
		{}
		Evt_StoreChanging(Evt_StoreChanging const&) = delete;
		Evt_StoreChanging& operator = (Evt_StoreChanging const&) = delete;
	};

	// Event raised when the store of ldr objects is added to or removed from
	struct Evt_StoreChanged
	{
		enum class EReason { NewData, Reload };

		// The store that was added to
		pr::ldr::ObjectCont const& m_store;

		// Contains the results of parsing including the object container that the objects where added to
		pr::ldr::ParseResult const& m_result;

		// The number of objects added as a result of the parsing.
		int m_count;

		// The origin of the store change
		EReason m_reason;

		Evt_StoreChanged(pr::ldr::ObjectCont const& store, int count, pr::ldr::ParseResult const& result, EReason why)
			:m_store(store)
			,m_count(count)
			,m_result(result)
			,m_reason(why)
		{}
		Evt_StoreChanged(Evt_StoreChanged const&) = delete;
		Evt_StoreChanged& operator = (Evt_StoreChanged const&) = delete;
	};

	// Event raised by the object manager whenever the object selection changes
	using Evt_SelectionChanged = pr::ldr::Evt_LdrObjectSelectionChanged;

	// Event raised when user settings change
	using Evt_SettingsChanged = pr::ldr::Evt_SettingsChanged;

	// Event raised by the renderer for each render step
	using Evt_RenderStepExecute = pr::rdr::Evt_RenderStepExecute;

	#pragma endregion
}
