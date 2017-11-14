//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/steps/render_step.h"
#include "pr/renderer11/shaders/shader_set.h"

namespace pr
{
	namespace rdr
	{
		// Flags controlling the behaviour of hit testing
		enum class EHitTestFlags :int
		{
			SnapToFaces = 1 << 0,
			SnapToEdges = 1 << 1,
			SnapToVerts = 1 << 2,

			_bitwise_operators_allowed = 0x7FFFFFF,
		};

		// A single hit test ray into the scene
		struct HitTestRay
		{
			// The world space origin and direction of the ray (normalisation not required)
			v4 m_ws_origin;
			v4 m_ws_direction;
		};

		// The output of a ray cast into the scene
		struct HitTestResult
		{
			// The origin and direction of the cast ray (in world space)
			v4 m_ws_origin;
			v4 m_ws_direction;

			// The intercept point (in world space)
			v4 m_ws_intercept;

			// The UID of the instance that was hit (0 if none)
			int m_instance_id;
		};

		struct RayCastStep :RenderStep
		{
			static ERenderStep const Id = ERenderStep::RayCast;
			using RayCont = pr::vector<HitTestRay>;

			RayCont                        m_rays;          // The rays to cast into the scene
			float                          m_snap_distance; // The world space snap distance 
			EHitTestFlags                  m_flags;         // What primitives are snapped to
			bool                           m_continuous;    // Whether this step is used as a one-shot or for every frame render
			D3DPtr<ID3D11Texture2D>        m_rt;            // The render target texture
			D3DPtr<ID3D11Texture2D>        m_db;            // The depth buffer texture
			D3DPtr<ID3D11Texture2D>        m_stage[3];      // A staging texture for copying output back to the CPU (triple buffered)
			D3DPtr<ID3D11RenderTargetView> m_rtv;           // The render target view of the RT texture
			D3DPtr<ID3D11DepthStencilView> m_dsv;           // The depth stencil view of the depth buffer
			D3DPtr<ID3D11RenderTargetView> m_main_rtv;      // The main RT for restoring after the 'rstep'
			D3DPtr<ID3D11DepthStencilView> m_main_dsv;      // The main DB for restoring after the 'rstep'
			D3DPtr<ID3D11Buffer>           m_cbuf_frame;    // Per-frame constant buffer
			D3DPtr<ID3D11Buffer>           m_cbuf_nugget;   // Per-nugget constant buffer
			ShaderPtr                      m_vs;
			ShaderPtr                      m_gs_face;
			ShaderPtr                      m_gs_edge;
			ShaderPtr                      m_gs_vert;
			ShaderPtr                      m_ps;
			ShaderPtr                      m_cs;
			int                            m_stage_idx;

			RayCastStep(Scene& scene, bool continuous);
			RayCastStep(RayCastStep const&) = delete;
			RayCastStep& operator = (RayCastStep const&) = delete;

			// Set the rays to cast.
			// 'snap_distance' is the distance (in world space) for point snapping
			// 'flags' controls what primitives snapping applies to
			void SetRays(HitTestRay const* rays, int count, float snap_distance, EHitTestFlags flags);

			// Read the results from the ray casts
			void ReadOutput(HitTestResult* results, int count);

		private:

			// The type of render step this is
			ERenderStep GetId() const override { return Id; }

			// Create the render target for the output of the ray cast
			void InitRT();

			// Bind the render target to the output merger for this step
			void BindRT(bool bind);

			// Add model nuggets to the draw list for this render step
			void AddNuggets(BaseInstance const& inst, TNuggetChain& nuggets) override;

			// Perform the render step
			void ExecuteInternal(StateStack& ss) override;

			// Copy the results back to the CPU
			void ReadOutput(ID3D11DeviceContext* dc);
		};
	}
}
