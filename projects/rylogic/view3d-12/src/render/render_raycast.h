//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/render/render_step.h"
#include "pr/view3d-12/utility/ray_cast.h"
//#include "pr/view3d-12/shaders/shader_set.h"

namespace pr::rdr12
{
	// Render step for performing ray casts
	struct RenderRayCast :RenderStep
	{
		// Algorithm:
		// To render to a texture then read the resulting pixel data on a CPU:
		// - Create a texture that the GPU can render into (D3D11_BIND_RENDER_TARGET, EUsage::DEFAULT).
		// - Create a staging texture (EUsage::STAGING) that the GPU will copy data to (via CopyResource).
		// - Render to the render target texture.
		// - Call ID3D11DeviceContext::CopyResource() or ID3D11DeviceContext::CopySubresource()
		// - Map (ID3D11DeviceContext::Map()) the staging resource to get access to the pixels.
		// The call to Map will block until the gfx pipeline has completed the CopyResource() call. So, calling
		// CopyResource(), immediately followed by Map() effectively flushes the pipeline.
		// This can be handled in two ways; Call CopyResource(), do loads of gfx work, then call Map() some time later.
		// Or, use triple buffering like so:
		// - Frame #1 - start CopyResource() to staging texture #1
		// - Frame #2 - start CopyResource() to staging texture #2
		// - Frame #3 - start CopyResource() to staging texture #3 and Map() staging texture #1 to access data.
		// - Frame #4 - start CopyResource() to staging texture #1 and Map() staging texture #2 to access data.
		// - etc
		// This way you can keep FPS but introduce latency, which is acceptable in high frame rate applications.

	private:

		pr::vector<HitTestRay> m_rays;          // Rays to cast
		float                  m_snap_distance; // Snap distance (in world space units)
		EHitTestFlags          m_flags;         // Types of primitives to hit
		RayCastFilter          m_include;       // A filter for instances to include for hit testing
#if 0 // todo
		D3DPtr<ID3D11Buffer>   m_cbuf_frame;    // Per-frame constant buffer
		D3DPtr<ID3D11Buffer>   m_cbuf_nugget;   // Per-nugget constant buffer
		D3DPtr<ID3D11Buffer>   m_buf_results;   // A buffer that will receive the intercepts (used in the shader)
		D3DPtr<ID3D11Buffer>   m_buf_zeros;     // A buffer used to zero the intercept results buffer
		D3DPtr<ID3D11Buffer>   m_buf_stage[2];  // Staging buffers for copying output back to the CPU (multi-buffered)
		int                    m_stage_idx;     // The multi-buffering index
		ShaderPtr              m_vs;            // 
		ShaderPtr              m_gs_face;       // 
		ShaderPtr              m_gs_edge;       // 
		ShaderPtr              m_gs_vert;       // 
#endif
		bool                   m_continuous;    // Whether this step is used as a one-shot or for every frame render

	public:

		RenderRayCast(Scene& scene, bool continuous);
#if 0 // todo
		RenderRayCast(RenderRayCast const&) = delete;
		RenderRayCast& operator = (RenderRayCast const&) = delete;
#endif
		// Compile-time derived type
		inline static constexpr ERenderStep Id = ERenderStep::RayCast;

		// Set the rays to cast.
		// 'snap_distance' is the distance (in world space) for point snapping.
		// 'flags' controls what primitives snapping applies to.
		// 'filter' filters instances added to the render step (i.e. decides what's hit-able)
		void SetRays(std::span<HitTestRay const> rays, float snap_distance, EHitTestFlags flags, RayCastFilter include);

		// Read the results from the ray casts
		void ReadOutput(RayCastResultsOut const& cb);

	private:

		// Perform the render step
		void Execute(Frame& ss) override;

		// Add model nuggets to the draw list for this render step
		void AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets, drawlist_t& drawlist) override;

#if 0 // todo

		// The type of render step this is
		ERenderStep GetId() const override { return Id; }

		// Create the buffers used by the shaders
		void InitBuffers();

		// Update the provided shader set appropriate for this render step
		void ConfigShaders(ShaderSet1& ss, ETopo topo) const override;
#endif
	};
}
