//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/view3d/forward.h"
#include "pr/view3d/steps/render_step.h"
#include "pr/view3d/shaders/shader_set.h"

namespace pr
{
	namespace rdr
	{
		// Constructs the g-buffer for a scene
		struct GBuffer :RenderStep
		{
			enum RTEnum_ { RTDiffuse = 0, RTNormal = 1, RTDepth = 2, RTCount = 3 };
			static_assert(RTCount <= D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT, "Too many simultaneous render targets");

			static ERenderStep const Id = ERenderStep::GBuffer;

			D3DPtr<ID3D11Texture2D>          m_tex[RTCount];
			D3DPtr<ID3D11RenderTargetView>   m_rtv[RTCount];
			D3DPtr<ID3D11ShaderResourceView> m_srv[RTCount];
			D3DPtr<ID3D11DepthStencilView>   m_dsv;
			D3DPtr<ID3D11RenderTargetView>   m_main_rtv;
			D3DPtr<ID3D11DepthStencilView>   m_main_dsv;
			D3DPtr<ID3D11Buffer>             m_cbuf_camera;  // Per-frame camera constants
			D3DPtr<ID3D11Buffer>             m_cbuf_nugget;  // Per-nugget constants
			ShaderPtr                        m_vs;
			ShaderPtr                        m_ps;
			AutoSub                          m_eh_resize;    // RT resize

			explicit GBuffer(Scene& scene);

		private:

			GBuffer(GBuffer const&);

			// The type of render step this is
			ERenderStep GetId() const override { return Id; }

			// Create render targets for the GBuffer based on the current render target size
			void InitRT(bool create_buffers);

			// Set the g-buffer as the render target
			void BindRT(bool bind);

			// Add model nuggets to the draw list for this render step
			void AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets) override;

			// Update the provided shader set appropriate for this render step
			void ConfigShaders(ShaderSet1& ss, EPrim topo) const override;

			// Perform the render step
			void ExecuteInternal(StateStack& ss) override;
		};

		// Debugging helper for printing the render target resource name
		inline char const* ToString(GBuffer::RTEnum_ rt)
		{
			switch (rt){
			default: return "unknown";
			case GBuffer::RTDiffuse: return "diffuse";
			case GBuffer::RTNormal:  return "normal";
			case GBuffer::RTDepth:   return "depth";
			}
		}
	}
}
