//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/steps/render_step.h"

namespace pr
{
	namespace rdr
	{
		// Constructs a shadow map
		struct ShadowMap :RenderStep
		{
			static const ERenderStep::Enum_ Id = ERenderStep::ShadowMap;

			//D3DPtr<ID3D11Texture2D>          m_tex[RTCount];
			//D3DPtr<ID3D11RenderTargetView>   m_rtv[RTCount];
			//D3DPtr<ID3D11ShaderResourceView> m_srv[RTCount];
			//D3DPtr<ID3D11DepthStencilView>   m_dsv;
			//D3DPtr<ID3D11RenderTargetView>   m_main_rtv;
			//D3DPtr<ID3D11DepthStencilView>   m_main_dsv;
			//D3DPtr<ID3D11Buffer>             m_cbuf_camera;  // A constant buffer for the frame constant shader variables
			ShaderPtr                        m_shader;      // The shader used to generate the g-buffer

			explicit ShadowMap(Scene& scene);

		private:

			ShadowMap(ShadowMap const&);
			ShadowMap& operator = (ShadowMap const&);

			// The type of render step this is
			ERenderStep::Enum_ GetId() const override { return Id; }

			// Create render targets for the gbuffer based on the current render target size
			void InitBuffer(bool create_buffers);
		};
	}
}
