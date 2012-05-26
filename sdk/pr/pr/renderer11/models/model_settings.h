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
			
			MdlSettings()
			:m_vb()
			,m_ib()
			{}
			
			// Construct using a set number of verts and indices
			MdlSettings(VBufferDesc const& vb, IBufferDesc const& ib)
			:m_vb(vb)
			,m_ib(ib)
			{}
			
			// Construct the model buffer with typical defaults
			template <class Vert, size_t VSize, typename Indx, size_t ISize>
			MdlSettings(Vert const (&vert)[VSize], Indx const (&idxs)[ISize])
			:m_vb(VBufferDesc(vert))
			,m_ib(IBufferDesc(idxs))
			{}
		};
	}
}

#endif


