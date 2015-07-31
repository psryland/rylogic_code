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
		using InitSet = std::set<View3DContext>;

		InitSet                   m_inits;            // A unique id assigned to each Initialise call
		ErrorCBStack              m_error_cb;         // A stack of error callback functions
		bool                      m_compatible;       // True if the renderer will work on this system
		pr::Renderer              m_rdr;              // The renderer
		WindowCont                m_wnd_cont;         // The created windows
		pr::ldr::ObjectCont       m_obj_cont;         // The created ldr objects
		pr::ldr::GizmoCont        m_giz_cont;         // The created ldr gizmos
		pr::script::EmbeddedLua<> m_lua;
		std::recursive_mutex      m_mutex;

		Context()
			:m_inits()
			,m_error_cb()
			,m_compatible(pr::rdr::TestSystemCompatibility())
			,m_rdr(pr::rdr::RdrSettings(FALSE))
			,m_wnd_cont()
			,m_obj_cont()
			,m_giz_cont()
			,m_lua()
			,m_mutex()
		{
			PR_ASSERT(PR_DBG, pr::meta::is_aligned_to<16>(this), "dll data not aligned");
			AtlInitCommonControls(ICC_WIN95_CLASSES); // add flags to support other controls
		}
		~Context()
		{
			while (!m_wnd_cont.empty())
				View3D_DestroyWindow(*m_wnd_cont.begin());
		}

		// Push/Pop error callbacks from the error callback stack
		void PushErrorCB(View3D_ReportErrorCB cb, void* ctx)
		{
			m_error_cb.emplace_back(ReportErrorCB(cb, ctx));
		}
		void PopErrorCB(View3D_ReportErrorCB cb)
		{
			if (m_error_cb.empty())
				throw std::exception("Error callback stack is empty, cannot pop");
			if (m_error_cb.back().m_cb != cb)
				throw std::exception("Attempt to pop an error callback that is not the most recently pushed callback. This is likely a destruction order probably");

			m_error_cb.pop_back();
		}

	private:
		Context(Context const&);
		Context& operator=(Context const&);
	};

}
