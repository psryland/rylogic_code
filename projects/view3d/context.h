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
		typedef std::set<View3DContext> InitSet;


		InitSet                  m_inits;            // A unique id assigned to each Initialise call
		ErrorCBStack             m_error_cb;         // A stack of error callback functions
		bool                     m_compatible;       // True if the renderer will work on this system
		pr::Renderer             m_rdr;              // The renderer
		WindowCont               m_wnd_cont;         // The created windows
		pr::ldr::ObjectCont      m_obj_cont;         // The created ldr objects
		WTL::InitScintilla       m_init_scintilla;
		pr::script::EmbeddedLua  m_lua;
		std::recursive_mutex     m_mutex;

		Context()
			:m_inits()
			,m_error_cb()
			,m_compatible(pr::rdr::TestSystemCompatibility())
			,m_rdr(pr::rdr::RdrSettings(FALSE))
			,m_wnd_cont()
			,m_obj_cont()
			,m_init_scintilla()
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

	private:
		Context(Context const&);
		Context& operator=(Context const&);
	};

}
