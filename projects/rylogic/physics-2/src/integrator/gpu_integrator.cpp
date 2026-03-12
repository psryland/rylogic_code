//*********************************************
// Physics Engine — GPU Integration Implementation
//  Copyright (C) Rylogic Ltd 2025
//*********************************************
#include "pr/physics-2/rigid_body/rigid_body_dynamics.h"
#include "src/integrator/gpu_integrator.h"

namespace pr::physics
{
	using namespace pr::rdr12;

	// Thread group size matching the HLSL [numthreads(64, 1, 1)] declaration.
	static constexpr int ThreadGroupSize = 64;

	// Constant buffer layout matching the HLSL cbIntegrate declaration.
	// Must be 16-byte aligned and match the register(b0) binding exactly.
	struct alignas(16) cbIntegrate
	{
		float dt;
		uint32_t body_count;
		uint32_t pad0;
		uint32_t pad1;
	};
	static_assert(sizeof(cbIntegrate) == 16);

	// Register assignments for the root signature
	struct EReg
	{
		inline static constexpr ECBufReg Params = ECBufReg::b0;
		inline static constexpr EUAVReg Bodies = EUAVReg::u0;
		inline static constexpr EUAVReg Output = EUAVReg::u1;
	};

	GpuIntegrator::GpuIntegrator(Gpu& gpu)
		: m_gpu(gpu)
		, m_cs_integrate()
		, m_r_bodies()
		, m_r_output()
		, m_capacity()
	{
		CompileShader();
	}

	// Integrate all bodies in the dynamics buffer on the GPU.
	// Uploads the buffer → dispatches compute → reads back results.
	// On return, 'dynamics' is updated in-place with the post-integration state.
	void GpuIntegrator::Integrate(GpuJob& job, std::span<RigidBodyDynamics> dynamics, float dt, std::span<IntegrateOutput> output)
	{
		auto body_count = static_cast<int>(dynamics.size());
		if (body_count == 0)
			return;

		// Create a compute job for this integration step.
		// GpuJob is self-contained: owns command list, allocator, barriers, upload/readback.

		ResizeBuffers(job.m_cmd_list, body_count);

		// Upload body dynamics to the GPU buffer
		{
			job.m_barriers.Transition(m_r_bodies.get(), D3D12_RESOURCE_STATE_COPY_DEST);
			job.m_barriers.Commit();

			auto upload = job.m_upload.template Alloc<RigidBodyDynamics>(body_count);
			memcpy(upload.template ptr<RigidBodyDynamics>(), dynamics.data(), body_count * sizeof(RigidBodyDynamics));
			job.m_cmd_list.CopyBufferRegion(m_r_bodies.get(), 0, upload);

			job.m_barriers.Transition(m_r_bodies.get(), D3D12_RESOURCE_STATE_UNORDERED_ACCESS);
			job.m_barriers.Commit();
		}

		// Dispatch the compute shader
		{
			auto cb = cbIntegrate{.dt = dt, .body_count = static_cast<uint32_t>(body_count)};

			job.m_cmd_list.SetPipelineState(m_cs_integrate.m_pso.get());
			job.m_cmd_list.SetComputeRootSignature(m_cs_integrate.m_sig.get());
			job.m_cmd_list.AddComputeRoot32BitConstants(cb);
			job.m_cmd_list.AddComputeRootUnorderedAccessView(m_r_bodies->GetGPUVirtualAddress());
			job.m_cmd_list.AddComputeRootUnorderedAccessView(m_r_output->GetGPUVirtualAddress());

			auto dispatch_count = (body_count + ThreadGroupSize - 1) / ThreadGroupSize;
			job.m_cmd_list.Dispatch(dispatch_count, 1, 1);

			// UAV barrier to ensure the compute shader finishes before readback
			job.m_barriers.UAV(m_r_bodies.get());
			job.m_barriers.UAV(m_r_output.get());
		}

		// Read back updated body dynamics
		GpuReadbackBuffer::Allocation readback_bodies;
		GpuReadbackBuffer::Allocation readback_output;
		{
			job.m_barriers.Transition(m_r_bodies.get(), D3D12_RESOURCE_STATE_COPY_SOURCE);
			job.m_barriers.Transition(m_r_output.get(), D3D12_RESOURCE_STATE_COPY_SOURCE);
			job.m_barriers.Commit();

			readback_bodies = job.m_readback.template Alloc<RigidBodyDynamics>(body_count);
			job.m_cmd_list.CopyBufferRegion(readback_bodies, m_r_bodies.get(), 0);

			readback_output = job.m_readback.template Alloc<IntegrateOutput>(body_count);
			job.m_cmd_list.CopyBufferRegion(readback_output, m_r_output.get(), 0);

			// Transition back to UAV for next step
			job.m_barriers.Transition(m_r_bodies.get(), D3D12_RESOURCE_STATE_UNORDERED_ACCESS);
			job.m_barriers.Transition(m_r_output.get(), D3D12_RESOURCE_STATE_UNORDERED_ACCESS);
		}

		// Execute and wait for GPU completion
		job.Run();

		// Copy results back to the caller's buffer
		memcpy(dynamics.data(), readback_bodies.template ptr<RigidBodyDynamics>(), body_count * sizeof(RigidBodyDynamics));
		if (!output.empty())
			memcpy(output.data(), readback_output.template ptr<IntegrateOutput>(), body_count * sizeof(IntegrateOutput));
	}

	// Compile the integration compute shader from embedded resources and create the
	// root signature and pipeline state object.
	void GpuIntegrator::CompileShader()
	{
		// Load the HLSL source from the embedded resource.
		// The resource is embedded by the consuming executable's .rc file.
		auto shader_source = resource::Read<char>(L"PHYSICS_INTEGRATE_HLSL", L"TEXT");
		auto compiler = ShaderCompiler{}
			.Source(shader_source)
			.Includes({new ResourceIncludeHandler, true})
			.EntryPoint(L"CSIntegrate")
			.ShaderModel(L"cs_6_0")
			.Optimise();

		auto bytecode = compiler.Compile();

		// Root signature: root constants (cbIntegrate) + two UAVs (bodies, output)
		m_cs_integrate.m_sig = RootSig(ERootSigFlags::ComputeOnly)
			.U32<cbIntegrate>(EReg::Params)
			.UAV(EReg::Bodies)
			.UAV(EReg::Output)
			.Create(m_gpu, "Physics:IntegrateSig");

		m_cs_integrate.m_pso = ComputePSO(m_cs_integrate.m_sig.get(), bytecode)
			.Create(m_gpu, "Physics:IntegratePSO");
	}

	// Create GPU buffers for body dynamics and debug output using raw D3D12 API.
	// We bypass ResourceFactory (which requires a Renderer) since we only need
	// simple DEFAULT heap buffers with UAV access for compute.
	void GpuIntegrator::ResizeBuffers(CmdList& cmd_list, int capacity)
	{
		capacity = std::max(1, capacity);

		if (m_r_bodies == nullptr || m_capacity < capacity)
		{
			m_r_bodies = m_gpu.CreateResource(ResDesc::Buf<RigidBodyDynamics>(capacity, {}).usage(EUsage::UnorderedAccess), cmd_list, "Physics:BodyDynamics");
			m_r_output = m_gpu.CreateResource(ResDesc::Buf<IntegrateOutput>(capacity, {}).usage(EUsage::UnorderedAccess), cmd_list, "Physics:IntegrateOutput");
			m_capacity = capacity;
		}
	}

	// Custom deleter implementation (GpuIntegrator is complete here)
	void Deleter<GpuIntegrator>::operator()(GpuIntegrator* p) const
	{
		delete p;
	}
}
