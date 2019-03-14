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
			Faces = 1 << 0,
			Edges = 1 << 1,
			Verts = 1 << 2,
			_bitwise_operators_allowed = 0x7FFFFFF,
		};

		// Snap types (in priority order) (Keep in sync with SNAP_TYPE_ in 'ray_cast_cbuf.hlsli')
		enum class ESnapType :int
		{
			NoSnap = 0,
			Vert = 1,
			EdgeMiddle = 2,
			FaceCentre = 3,
			Edge = 4,
			Face = 5,
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
			v4                  m_ws_origin;     // The origin of the ray that hit something
			v4                  m_ws_direction;  // The direction of the ray that hit something
			v4                  m_ws_intercept;  // Where the intercept is in world space
			BaseInstance const* m_instance;      // The instance that was hit. (const because it's a pointer from the drawlist. Callers should use this pointer to find in ObjectSets)
			float               m_distance;      // The distance from the ray origin to the intercept
			int                 m_ray_index;     // The index of the input ray
			ESnapType           m_snap_type;     // How the point was snapped (if at all)
		};

		// Render step for performing ray casts
		struct RayCastStep :RenderStep
		{
			static ERenderStep const Id = ERenderStep::RayCast;
			using InstFilter = std::function<bool(BaseInstance const*)>;
			using ResultsOut = std::function<bool(HitTestResult const&)>;

			pr::vector<HitTestRay> m_rays;          // Rays to cast
			float                  m_snap_distance; // Snap distance (in world space units)
			EHitTestFlags          m_flags;         // Types of primitives to hit
			InstFilter             m_include;       // A filter for instances to include for hit testing
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
			bool                   m_continuous;    // Whether this step is used as a one-shot or for every frame render

			RayCastStep(Scene& scene, bool continuous);
			RayCastStep(RayCastStep const&) = delete;
			RayCastStep& operator = (RayCastStep const&) = delete;

			// Set the rays to cast.
			// 'snap_distance' is the distance (in world space) for point snapping.
			// 'flags' controls what primitives snapping applies to.
			// 'filter' filters instances added to the render step (i.e. decides what's hit-able)
			void SetRays(HitTestRay const* rays, int count, float snap_distance, EHitTestFlags flags, InstFilter const& include);

			// Read the results from the ray casts
			void ReadOutput(ResultsOut const& cb);

		private:

			// The type of render step this is
			ERenderStep GetId() const override { return Id; }

			// Create the buffers used by the shaders
			void InitBuffers();

			// Add model nuggets to the draw list for this render step
			void AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets) override;

			// Update the provided shader set appropriate for this render step
			void ConfigShaders(ShaderSet1& ss, EPrim topo) const override;

			// Perform the render step
			void ExecuteInternal(StateStack& ss) override;
		};
	}
}
