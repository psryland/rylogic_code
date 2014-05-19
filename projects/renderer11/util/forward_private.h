//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/util/stock_resources.h"

namespace pr
{
	namespace rdr
	{
		namespace EDbgRdrFlags
		{
			enum Type { WarnedNoRenderNuggets = 1 << 0 };
		}

		// Shaders
		struct FwdShaderVS;
		struct FwdShaderPS;
		struct GBufferShaderVS;
		struct GBufferShaderPS;
		struct DSLightingShaderVS;
		struct DSLightingShaderPS;
		struct ShadowMapVS;
		struct ShadowMapFaceGS;
		struct ShadowMapLineGS;
		struct ShadowMapPS;
		struct ThickLineListShaderGS;
	}
}
