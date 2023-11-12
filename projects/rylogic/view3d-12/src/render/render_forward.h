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
		// Compile-time derived type
		inline static constexpr ERenderStep Id = ERenderStep::RenderForward;

		shaders::Forward m_shader;
		explicit RenderForward(Scene& scene);

	private:

		// Add model nuggets to the draw list for this render step
		void AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets, drawlist_t& drawlist) override;

		// Perform the render step
		void ExecuteInternal(BackBuffer& bb) override;

		// Draw a single nugget
		void DrawNugget(Nugget const& nugget, PipeStateDesc& desc);
	};
}
