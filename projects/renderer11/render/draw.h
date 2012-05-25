//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_RENDER_DRAW_H
#define PR_RDR_RENDER_DRAW_H

#include "pr/renderer11/forward.h"

namespace pr
{
	namespace rdr
	{
		// A helper object for managing the rendering of render nuggets
		struct Draw
		{
			// The device context to render too
			D3DPtr<ID3D11DeviceContext> m_dc;
			
			Draw(D3DPtr<ID3D11DeviceContext>& dc) :m_dc(dc) {}
			
			// Clear the back buffer to 'colour'
			void ClearBB(pr::Colour const& colour = pr::ColourBlack);
			
			// Clear the depth buffer to 'depth'
			void ClearDB(UINT flags = D3D11_CLEAR_DEPTH|D3D11_CLEAR_STENCIL, float depth = 1.0f, UINT8 stencil = 0U);
			
			// Setup the input assembler for the given nugget
			void Setup(DrawListElement const& dle, SceneView const& view);
			
			// Add the nugget to the device context
			void Render(Nugget const& nugget);
		};
	}
}

#endif
