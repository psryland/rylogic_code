//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/render/blend_state.h"
#include "pr/renderer11/render/raster_state.h"
#include "pr/renderer11/render/depth_state.h"

namespace pr
{
	namespace rdr
	{
		// Maintains a history of the device state restoring it on destruction
		struct StateStack
		{
			struct State
			{
				RenderStep const* m_rstep;
				DrawListElement const* m_dle;
				D3DPtr<ID3D11InputLayout> m_iplayout;
				ModelBufferPtr m_mb;
				EPrim m_topo;
				DSBlock m_dsb;
				RSBlock m_rsb;
				BSBlock m_bsb;
				BaseShader* m_shdr;

				State();
			};

			struct Frame
			{
				StateStack& m_ss;
				State m_restore;
				Frame(StateStack& ss) :m_ss(ss) ,m_restore(m_ss.m_pending) {}
				~Frame() { m_ss.m_pending = m_restore; }
				PR_NO_COPY(Frame);
			};
			struct RSFrame :Frame
			{
				RSFrame(StateStack& ss, RenderStep const& rstep);
				PR_NO_COPY(RSFrame);
			};
			struct DleFrame :Frame
			{
				DleFrame(StateStack& ss, DrawListElement const& dle);
				PR_NO_COPY(DleFrame);
			};

			D3DPtr<ID3D11DeviceContext> m_dc;
			Scene& m_scene;
			State m_init_state;
			State m_pending;
			State m_current;

			StateStack(D3DPtr<ID3D11DeviceContext> dc, Scene& scene);
			~StateStack();

			// Apply the current state to the device
			void Commit();

			PR_NO_COPY(StateStack);
		};
	}
}
