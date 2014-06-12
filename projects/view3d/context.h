//***************************************************************************************************
// View 3D
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************
#pragma once

#include "view3d/forward.h"

namespace view3d
{
	// Global data for this dll
	struct Context :pr::AlignTo<16>
	{
		std::set<View3DContext>   m_inits;      // A unique id assigned to each Initialise call
		View3D_ReportErrorCB      m_error_cb;   // Error callback for the dll
		View3D_LogOutputCB        m_log_cb;     // Log output callback
		pr::Logger                m_log;        // Logger
		bool                      m_compatible;
		pr::Renderer              m_rdr;
		WindowCont                m_wnd_cont;
		pr::ldr::ObjectCont       m_obj_cont;
		pr::script::EmbeddedLua   m_lua;
		std::recursive_mutex      m_mutex;

		Context(View3D_ReportErrorCB error_cb, View3D_LogOutputCB log_cb)
			:m_inits()
			,m_error_cb(error_cb)
			,m_log_cb(log_cb)
			,m_log("view3d", [this](pr::log::Event const& ev){ LogOutput(ev); })
			,m_compatible(pr::rdr::TestSystemCompatibility())
			,m_rdr(pr::rdr::RdrSettings(FALSE))
			,m_wnd_cont()
			,m_obj_cont()
			,m_lua()
			,m_mutex()
		{
			PR_ASSERT(PR_DBG, pr::meta::is_aligned_to<16>(this), "dll data not aligned");
			AtlInitCommonControls(ICC_BAR_CLASSES); // add flags to support other controls
		}

		// Forward log data to the callback
		void LogOutput(pr::log::Event const& ev)
		{
			if (!m_log_cb) return;
			m_log_cb(static_cast<EView3DLogLevel>(ev.m_level), ev.m_timestamp.count(), ev.m_msg.c_str());
		}

		// Report an error via the callback
		void ReportError(pr::string<> msg)
		{
			m_log.Write(pr::log::ELevel::Error, msg);
			if (!m_error_cb) return;
			if (msg.last() != '\n') msg.push_back('\n');
			m_error_cb(msg.c_str());
		}
		void ReportError(pr::string<> msg, std::exception const& ex)
		{
			m_log.Write(pr::log::ELevel::Error, ex, msg);
			if (!m_error_cb) return;
			if (msg.last() != '\n') msg.push_back('\n');
			m_error_cb(pr::FmtS("%sReason: %s\n", msg.c_str(), ex.what()));
		}

	private:
		Context(Context const&);
		Context& operator=(Context const&);
	};

}
