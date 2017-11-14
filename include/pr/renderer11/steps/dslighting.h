//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/instances/instance.h"
#include "pr/renderer11/steps/render_step.h"

namespace pr
{
	namespace rdr
	{
		// Uses g-buffer data to perform post process lighting
		struct DSLighting :RenderStep
		{
			static const ERenderStep Id = ERenderStep::DSLighting;

			// An instance type for the full screen quad
			#define PR_RDR_INST(x)\
				x(ModelPtr ,m_model ,EInstComp::ModelPtr)
			PR_RDR_DEFINE_INSTANCE(Instance, PR_RDR_INST)
			#undef PR_RDR_INST

			GBuffer&             m_gbuffer;       // The gbuffer render step for access to the gbuffer textures
			D3DPtr<ID3D11Buffer> m_cbuf_camera;   // A constant buffer for the frame constant shader variables
			D3DPtr<ID3D11Buffer> m_cbuf_lighting; // A constant buffer for the frame constant shader variables
			Instance             m_unit_quad;     // The quad drawn to the screen for post processing
			bool                 m_clear_bb;      // True if this render step clears the back-buffer before rendering
			ShaderPtr            m_vs;            //
			ShaderPtr            m_ps;            //

			explicit DSLighting(Scene& scene);
			DSLighting(DSLighting const&) = delete;
			DSLighting& operator = (DSLighting const&) = delete;

		private:

			// The type of render step this is
			ERenderStep GetId() const override { return Id; }

			// Add model nuggets to the draw list for this render step
			void AddNuggets(BaseInstance const&, TNuggetChain&) override {}

			// Perform the render step
			void ExecuteInternal(StateStack& ss) override;
		};
	}
}
