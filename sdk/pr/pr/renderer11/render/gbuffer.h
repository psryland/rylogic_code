//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_RENDER_GBUFFER_H
#define PR_RDR_RENDER_GBUFFER_H

#include "pr/renderer11/forward.h"

namespace pr
{
	namespace rdr
	{
		// The render targets that make up the G-buffer
		struct GBuffer
			:pr::events::IRecv<pr::rdr::Evt_Resize>
		{
			enum { RTColour = 0, RTNormal = 1, RTDepth = 2, RTCount = 3 };
			static_assert(RTCount <= D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT, "Too many sumultaneous render targets");
			
			D3DPtr<ID3D11Device>           m_device;
			D3DPtr<ID3D11RenderTargetView> m_colour;
			D3DPtr<ID3D11RenderTargetView> m_normal;
			D3DPtr<ID3D11RenderTargetView> m_depth;
			
			// Setup or release the GBuffer. Use Init(0,0,0) to release
			void Init(D3DPtr<ID3D11Device> device, UINT width, UINT height);
			void OnEvent(pr::rdr::Evt_Resize const& evt);
			
			// Set/Clear the GBuffer as the output merger targets
			void Set();
			void Restore();
			
		private:
			
		};
	}
}

#endif
