//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/render/render_step.h"
#include "pr/view3d-12/shaders/shader_forward.h"
#include "pr/view3d-12/utility/pipe_state.h"

namespace pr::rdr12
{
	struct RenderForward :RenderStep
	{
	private:

		PipeStatePool m_pipe_state_pool;  // Pool of pipeline state objects
		shaders::Forward m_shader; // The forward renderer shader

	public:

		explicit RenderForward(Scene& scene);
		static ERenderStep const Id = ERenderStep::RenderForward;

	private:

		// The type of render step this is
		ERenderStep GetId() const override { return Id; }

		// Add model nuggets to the draw list for this render step
		void AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets, drawlist_t& drawlist) override;

		// Update the provided shader set appropriate for this render step
		void ConfigShaders(ShaderSet1& ss, ETopo topo) const override;

		// Perform the render step
		void ExecuteInternal(BackBuffer& bb, ID3D12GraphicsCommandList* cmd_list) override;

		// Draw a single nugget
		void DrawNugget(Nugget const& nugget, PipeStateDesc& desc, ID3D12GraphicsCommandList* cmd_list);
	};
}
