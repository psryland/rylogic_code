//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/render/render_step.h"
#include "pr/view3d-12/resource/gpu_descriptor_heap.h"
#include "pr/view3d-12/shaders/shader_ray_cast.h"
#include "pr/view3d-12/utility/ray_cast.h"

namespace pr::rdr12
{
	// Render step for performing ray casts
	struct RenderRayCast :RenderStep
	{
	private:
		pr::vector<HitTestRay> m_rays;          // Rays to cast
		float                  m_snap_distance; // Snap distance: 'snap_dist = Perspectvie ? snap_distance * depth : snap_distance'
		ESnapMode              m_snap_mode;     // Snap behaviour
		RayCastFilter          m_include;       // A filter for instances to include for hit testing
		GfxCmdList             m_cmd_list;      // Command buffer
		GpuSync                m_gsync;         //
		shaders::RayCast       m_shader;        // The ray cast shader
		D3DPtr<ID3D12Resource> m_zero;          // A buffer of zeros used to reset the output counter
		D3DPtr<ID3D12Resource> m_out;           // An unstructured buffer for the number of intercepts and the intercept data
		GpuReadbackBuffer      m_readback;      // A read back buffer for reading intercept data
		GpuTransferAllocation  m_output;        // The CPU copy of the results from the last ray cast
		bool                   m_continuous;    // Whether this step is used as a one-shot or for every frame render

	public:

		RenderRayCast(Scene& scene, bool continuous);

		// Compile-time derived type
		inline static constexpr ERenderStep Id = ERenderStep::RayCast;

		// Set the rays to cast.
		// 'snap_mode' controls how point snapping is applied.
		// 'snap_distance', if the mode is 'perspective' then this is the ratio proportional to depth from the ray origin, otherwise it's in world units.
		// 'flags' controls what primitives snapping applies to.
		// 'filter' filters instances added to the render step (i.e. decides what's hit-able)
		void SetRays(std::span<HitTestRay const> rays, ESnapMode snap_mode, float snap_distance, RayCastFilter include);

		// Perform the ray cast and read the results
		std::future<void> ExecuteImmediate(RayCastResultsOut out);

	private:

		// Step up the GPU call for the ray cast
		GpuTransferAllocation ExecuteCore();

		// Perform the render step
		void Execute(Frame& frame) override;

		// Add model nuggets to the draw list for this render step
		void AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets, drawlist_t& drawlist) override;
	
		// Draw a single nugget
		void DrawNugget(Nugget const& nugget, PipeStateDesc& desc);
	};
}
