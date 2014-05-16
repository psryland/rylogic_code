//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/render/blend_state.h"
#include "pr/renderer11/render/raster_state.h"
#include "pr/renderer11/render/depth_state.h"
#include "pr/renderer11/shaders/shader_set.h"

namespace pr
{
	namespace rdr
	{
		struct DeviceState
		{
			RenderStep const* m_rstep;
			DrawListElement const* m_dle;
			ModelBufferPtr m_mb;
			EPrim m_topo;
			DSBlock m_dsb;
			RSBlock m_rsb;
			BSBlock m_bsb;
			ShaderSet m_shdrs;

			DeviceState();
		};

		// Maintains a history of the device state restoring it on destruction
		struct StateStack
		{
			struct Frame
			{
				StateStack& m_ss;
				DeviceState m_restore;
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
			DeviceState m_init_state;
			DeviceState m_pending;
			DeviceState m_current;

			StateStack(D3DPtr<ID3D11DeviceContext> dc, Scene& scene);
			~StateStack();

			// Apply the current state to the device
			void Commit();

			PR_NO_COPY(StateStack);
		};
	}
}
