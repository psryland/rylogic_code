//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once
#include "pr/physics-2/forward.h"
#include "src/utility/gpu.h"

namespace pr::physics
{
	struct GpuIntegrator
	{
		Gpu& m_gpu;                        // Lightweight D3D12 wrapper (device + command queue)
		ComputeStep m_cs_integrate;        // Root signature + PSO for the integration shader
		D3DPtr<ID3D12Resource> m_r_bodies; // GPU buffer: RWStructuredBuffer<RigidBodyDynamics>
		D3DPtr<ID3D12Resource> m_r_output; // GPU buffer: RWStructuredBuffer<IntegrateOutput>
		int m_capacity;                    // Maximum number of bodies the buffers can hold

		explicit GpuIntegrator(Gpu& gpu);

		// Integrate all bodies in the dynamics buffer on the GPU.
		// Uploads the buffer → dispatches compute → reads back results.
		// On return, 'dynamics' is updated in-place with the post-integration state.
		void Integrate(GpuJob& job, std::span<RigidBodyDynamics> dynamics, float dt, std::span<IntegrateOutput> output);

	private:

		// Compile the integration compute shader from embedded resources and create the
		// root signature and pipeline state object.
		void CompileShader();

		// Create GPU buffers for body dynamics and debug output using raw D3D12 API.
		// We bypass ResourceFactory (which requires a Renderer) since we only need
		// simple DEFAULT heap buffers with UAV access for compute.
		void ResizeBuffers(CmdList& cmd_list, int capacity);
	};
}
