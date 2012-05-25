//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#pragma once
#ifndef PR_RDR_DEFAULT_TYPES_H
#define PR_RDR_DEFAULT_TYPES_H

#include <hash_map>
#include "pr/renderer/types/forward.h"

namespace pr
{
	namespace rdr
	{
		typedef stdext::hash_map<RdrId, Model*>             TModelLookup;
		typedef stdext::hash_map<RdrId, Effect*>            TEffectLookup;
		typedef stdext::hash_map<RdrId, Texture*>           TTextureLookup;
		typedef stdext::hash_map<RdrId, IDirect3DTexture9*> TTexFileLookup;
	}
}

#endif
