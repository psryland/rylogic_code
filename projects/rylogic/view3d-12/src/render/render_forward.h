//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/render/render_step.h"
//#include "pr/view3d-12/shaders/shader_set.h"

namespace pr::rdr12
{
	struct RenderForward :RenderStep
	{
		static ERenderStep const Id = ERenderStep::RenderForward;

	private:

		D3DPtr<ID3D12Resource> m_cbuf_frame;  // Per-frame constant buffer
		D3DPtr<ID3D12Resource> m_cbuf_nugget; // Per-nugget constant buffer
		//ShaderPtr            m_vs;          // The VS for forward rendering
		//ShaderPtr            m_ps;          // The PS for forward rendering

	public:

		explicit RenderForward(Scene& scene);
		RenderForward(RenderForward const&) = delete;
		RenderForward& operator = (RenderForward const&) = delete;

	private:

		// The type of render step this is
		ERenderStep GetId() const override { return Id; }

		// Add model nuggets to the draw list for this render step
		void AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets, drawlist_t& drawlist) override;

		// Perform the render step
		void ExecuteInternal(BackBuffer& bb, ID3D12GraphicsCommandList* cmd_list) override;

		// Update the provided shader set appropriate for this render step
		void ConfigShaders(ShaderSet1& ss, ETopo topo) const override;

		// Call draw for a nugget
		void DrawNugget(ID3D12GraphicsCommandList* cmd_list, Nugget const& nugget);//, StateStack& ss);
	};
}
