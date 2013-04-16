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
			D3DPtr<ID3D11Device> m_device;

		public:
			RasterStateManager(D3DPtr<ID3D11Device>& device)
			:m_device(device)
			{}

			// Create a raster state
			D3DPtr<ID3D11RasterizerState> RasterState(pr::rdr::RasterizerDesc const& desc) const
			{
				D3DPtr<ID3D11RasterizerState> rs;
				pr::Throw(m_device->CreateRasterizerState(&desc, &rs.m_ptr));
				return rs;
			}

			// Some common raster states
			D3DPtr<ID3D11RasterizerState> SolidCullNone () const { return RasterState(RasterizerDesc(D3D11_FILL_SOLID, D3D11_CULL_NONE)); }
			D3DPtr<ID3D11RasterizerState> SolidCullBack () const { return RasterState(RasterizerDesc(D3D11_FILL_SOLID, D3D11_CULL_BACK)); }
			D3DPtr<ID3D11RasterizerState> SolidCullFront() const { return RasterState(RasterizerDesc(D3D11_FILL_SOLID, D3D11_CULL_FRONT)); }
			D3DPtr<ID3D11RasterizerState> WireCullNone  () const { return RasterState(RasterizerDesc(D3D11_FILL_WIREFRAME, D3D11_CULL_NONE)); }
		};
	}
}

#endif
