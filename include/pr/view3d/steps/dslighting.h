//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/view3d/forward.h"
#include "pr/view3d/instances/instance.h"
#include "pr/view3d/steps/render_step.h"

namespace pr::rdr
{
	// Uses g-buffer data to perform post process lighting
	struct DSLighting :RenderStep
	{
		static const ERenderStep Id = ERenderStep::DSLighting;

		// An instance type for the full screen quad
		#define PR_RDR_INST(x)\
			x(ModelPtr, m_model, EInstComp::ModelPtr)\
			x(uint64_t, pad, EInstComp::None)
		PR_RDR_DEFINE_INSTANCE(Instance, PR_RDR_INST)
		#undef PR_RDR_INST

		Instance             m_unit_quad;     // The quad drawn to the screen for post processing
		GBuffer&             m_gbuffer;       // The gbuffer render step for access to the gbuffer textures
		D3DPtr<ID3D11Buffer> m_cbuf_camera;   // A constant buffer for the frame constant shader variables
		D3DPtr<ID3D11Buffer> m_cbuf_lighting; // A constant buffer for the frame constant shader variables
		bool                 m_clear_bb;      // True if this render step clears the back-buffer before rendering
		ShaderPtr            m_vs;            // Deferred lighting shaders
		ShaderPtr            m_ps;            // Deferred lighting shaders

		explicit DSLighting(Scene& scene);
		DSLighting(DSLighting const&) = delete;
		DSLighting& operator = (DSLighting const&) = delete;

	private:

		// The type of render step this is
		ERenderStep GetId() const override { return Id; }

		// Update the provided shader set appropriate for this render step
		void ConfigShaders(ShaderSet1&, EPrim) const override {}

		// Add model nuggets to the draw list for this render step
		void AddNuggets(BaseInstance const&, TNuggetChain const&) override {}

		// Perform the render step
		void ExecuteInternal(StateStack& ss) override;
	};
}
