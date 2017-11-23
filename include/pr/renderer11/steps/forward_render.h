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

			D3DPtr<ID3D11Buffer> m_cbuf_frame;  // Per-frame constant buffer
			D3DPtr<ID3D11Buffer> m_cbuf_nugget; // Per-nugget constant buffer
			bool                 m_clear_bb;    // True if this render step clears the back-buffer before rendering
			ShaderPtr            m_vs;          // The VS for forward rendering
			ShaderPtr            m_ps;          // The PS for forward rendering

			ForwardRender(Scene& scene, bool clear_bb = true);
			ForwardRender(ForwardRender const&) = delete;
			ForwardRender& operator = (ForwardRender const&) = delete;

		private:

			// The type of render step this is
			ERenderStep GetId() const override { return Id; }

			// Update the provided shader set appropriate for this render step
			void ConfigShaders(ShaderSet1& ss, EPrim topo) const override;

			// Add model nuggets to the draw list for this render step
			void AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets) override;

			// Perform the render step
			void ExecuteInternal(StateStack& ss) override;
		};
	}
}
