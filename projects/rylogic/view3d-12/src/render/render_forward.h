//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/render/render_step.h"
#include "pr/view3d-12/shaders/shader_forward.h"

namespace pr::rdr12
{
	struct RenderForward :RenderStep
	{
		static ERenderStep const Id = ERenderStep::RenderForward;

	private:

		// The forward renderer shader
		shaders::Forward m_shader;

	public:

		explicit RenderForward(Scene& scene);

	private:

		// The type of render step this is
		ERenderStep GetId() const override { return Id; }

		// Add model nuggets to the draw list for this render step
		void AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets, drawlist_t& drawlist) override;

		// Perform the render step
		void ExecuteInternal(BackBuffer& bb, ID3D12GraphicsCommandList* cmd_list) override;

		// Update the provided shader set appropriate for this render step
		void ConfigShaders(ShaderSet1& ss, ETopo topo) const override;
	};
}
