//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_MODELS_MODEL_SETTINGS_H
#define PR_RDR_MODELS_MODEL_SETTINGS_H

#include "pr/renderer11/forward.h"
#include "pr/renderer11/util/wrappers.h"

namespace pr
{
	namespace rdr
	{
		// Model buffer / Model creation settings
		struct MdlSettings
		{
			VBufferDesc m_vb;
			IBufferDesc m_ib;
			string32 m_name;
			
			MdlSettings()
				:m_vb()
				,m_ib()
				,m_name()
			{}
			
			// Construct using a set number of verts and indices
			MdlSettings(VBufferDesc const& vb, IBufferDesc const& ib, char const* name = "")
				:m_vb(vb)
				,m_ib(ib)
				,m_name(name)
			{}
			
			// Construct the model buffer with typical defaults
			template <class Vert, size_t VSize, typename Indx, size_t ISize>
			MdlSettings(Vert const (&vert)[VSize], Indx const (&idxs)[ISize], char const* name = "")
				:m_vb(VBufferDesc(vert))
				,m_ib(IBufferDesc(idxs))
				,m_name(name)
			{}

			// Construct from containers of verts and indices
			template <typename VCont, typename ICont>
			MdlSettings(VCont const& vcont, ICont const& icont, char const* name = "")
				:m_vb(VBufferDesc(vcont.size(), vcont.data()))
				,m_ib(IBufferDesc(icont.size(), icont.data()))
				,m_name(name)
			{}
		};
	}
}

#endif


