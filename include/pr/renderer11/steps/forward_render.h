//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/steps/render_step.h"
#include "pr/renderer11/shaders/shader_set.h"

namespace pr
{
	namespace rdr
	{
		struct ForwardRender :RenderStep
		{
			static ERenderStep const Id = ERenderStep::ForwardRender;

			D3DPtr<ID3D11Buffer> m_cbuf_frame;   // Per-frame constant buffer
			D3DPtr<ID3D11Buffer> m_cbuf_nugget;  // Per-nugget constant buffer
			bool                 m_clear_bb;     // True if this render step clears the back-buffer before rendering
			ShaderSet            m_sset;

			ForwardRender(Scene& scene, bool clear_bb = true);
			ForwardRender(ForwardRender const&) = delete;
			ForwardRender& operator = (ForwardRender const&) = delete;

		private:

			// The type of render step this is
			ERenderStep GetId() const override { return Id; }

			// Add model nuggets to the draw list for this render step
			void AddNuggets(BaseInstance const& inst, TNuggetChain& nuggets) override;

			// Perform the render step
			void ExecuteInternal(StateStack& ss) override;
		};
	}
}
