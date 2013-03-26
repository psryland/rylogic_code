//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_RENDER_RENDER_STATES_H
#define PR_RDR_RENDER_RENDER_STATES_H

#include "pr/renderer11/forward.h"
#include "pr/renderer11/util/lookup.h"
#include "pr/renderer11/util/wrappers.h"
#include "pr/renderer11/util/util.h"
//#include "pr/renderer/models/rendernugget.h"
//#include "pr/renderer/materials/material.h"
//#include "pr/renderer/materials/textures/texture.h"
//#include "pr/renderer/materials/effects/effect.h"

namespace pr
{
	namespace rdr
	{
		// Render states are now rasterizer states
		class RasterStateManager
		{
			typedef Lookup<RdrId, ID3D11RasterizerState*> RasterStateLookup;

			D3DPtr<ID3D11Device> m_device;
			RasterStateLookup m_lookup_rs;  // A map from RasterState id to an existing raster state instance

		public:
			RasterStateManager(pr::rdr::MemFuncs& mem, D3DPtr<ID3D11Device>& device);
			~RasterStateManager();

			// Create a raster state
			// If 'out_id' is given it's set to the id assigned to the raster state
			// and can be used in the other overload of this method
			D3DPtr<ID3D11RasterizerState> RasterState(RdrId id, pr::rdr::RasterizerDesc const& desc, RdrId* out_id = 0);

			// Get a pre-existing raster state by it's id
			D3DPtr<ID3D11RasterizerState> RasterState(RdrId id);
		};
	}
}

#endif
