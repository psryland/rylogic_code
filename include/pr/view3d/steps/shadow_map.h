//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/view3d/forward.h"
#include "pr/view3d/steps/render_step.h"

namespace pr::rdr
{
	struct ShadowMap :RenderStep
	{
		// Algorithm:
		//  - Create a 2D colour texture. R=depth,G=colour?
		//  - Directional:
		//    - Create an orthographic projection that encloses everything the view can see plus everything between the light and the view.
		//    - Render the shadow map pass before the main render pass
		//    - Shade the scene using the smap
		//  - Spot:
		//    - Create a perspective projection that encloses everything the view can see plus everything between the light and the view.
		//    - Render the shadow map pass before the main render pass
		//    - Shade the scene using the smap
		//  - Point:
		//    - Create 6 perspective projections around the light.
		//    - ?? Use a fibonacci sphere to map directions around the light to a 2D surface
		//    - ?? not sure
		//  - LiSPSM:
		//    - During the shadow map rendering pass, apply a perspective transform to the scene where the perspective
		//      view is perpendicular to the light direction.
		//    - During the main render, apply the perspective to the light lookup ray before sampling the smap.
		//
		// Notes:
		//  - The shadow map step handles generation of all shadow maps for all lights in the scene.
		//    It renders a shadow map for each shadow caster as a separate pass.
		//  - This is an implementation of light space perspective shadow mapping (LiSPSM).
		//    The main idea of perspective shadow mapping is to apply a perspective transformation
		//    to the scene before rendering it into the shadow map. In the original PSM algorithm
		//    the perspective transform was the same as the view projection, but that does weird
		//    things to the light direction. In LiSPSM, the projection is perpendicular to the light
		//    direction instead, with Zn and Zf clamped to the view frustum Zn,Zf.
		//  - The smap face must be perpendicular to the light direction otherwise the smap texels
		//    are not isotropic and the shadow will be blocky in some places.
		//  - The shadow map is not a depth buffer. It's a colour buffer with depth encoded into it.
			
		static ERenderStep const Id = ERenderStep::ShadowMap;
		struct ProjectionParams
		{
			m4x4  m_w2l;
			m4x4  m_l2s;
			BBox  m_bounds;
			float m_zn;
			float m_zf;
		};
		struct ShadowCaster
		{
			ProjectionParams                 m_params;      // Projection parameters
			Light const*                     m_light;       // The shadow casting light
			D3DPtr<ID3D11Texture2D>          m_tex;         // The shadow map texture
			D3DPtr<ID3D11RenderTargetView>   m_rtv;         // RT view of the shadow map texture for creating the shadow map
			D3DPtr<ID3D11ShaderResourceView> m_srv;         // Shader view for using the shadow map in other shaders

			ShadowCaster(ID3D11Device* device, Light const& light, iv2 size, DXGI_FORMAT format);
			void UpdateParams(Scene const& scene, BBox_cref ws_bounds);
		};
		using Casters = std::vector<ShadowCaster>;

		Casters                        m_caster;      // The light sources that cast shadows
		D3DPtr<ID3D11SamplerState>     m_samp;        // Shadow map texture sampler
		D3DPtr<ID3D11RenderTargetView> m_main_rtv;    // The main RT for restoring after the 'rstep'
		D3DPtr<ID3D11DepthStencilView> m_main_dsv;    // The main DB for restoring after the 'rstep'
		D3DPtr<ID3D11Buffer>           m_cbuf_frame;  // Per-frame constant buffer
		D3DPtr<ID3D11Buffer>           m_cbuf_nugget; // Per-nugget constant buffer
		DXGI_FORMAT                    m_smap_format; // The texture format of the smap textures
		iv2                            m_smap_size;   // Dimensions of the 'smap' textures
		BBox                           m_bbox_scene;  // The scene bounds of shadow casters
		ShaderPtr                      m_vs;
		ShaderPtr                      m_ps;

		ShadowMap(Scene& scene, Light const& light, iv2 size = {1024,1024}, DXGI_FORMAT format = DXGI_FORMAT_R32G32_FLOAT);
		ShadowMap(ShadowMap const&) = delete;
		ShadowMap& operator = (ShadowMap const&) = delete;

		// Add a shadow casting light source
		void AddLight(Light const& light);

	private:

		// The type of render step this is
		ERenderStep GetId() const override { return Id; }

		// Set the g-buffer as the render target
		void BindRT(ShadowCaster const* caster);

		// Update the provided shader set appropriate for this render step
		void ConfigShaders(ShaderSet1& ss, ETopo topo) const override;

		// Reset the drawlist
		void ClearDrawlist() override;

		// Add model nuggets to the draw list for this render step
		void AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets) override;

		// Perform the render step
		void ExecuteInternal(StateStack& ss) override;

		// Call draw for a nugget
		void DrawNugget(ID3D11DeviceContext* dc, Nugget const& nugget, StateStack& ss);
	};
}
