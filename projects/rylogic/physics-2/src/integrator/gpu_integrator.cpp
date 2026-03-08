//*********************************************
// Physics Engine — GPU Integration Implementation
//  Copyright (C) Rylogic Ltd 2025
//*********************************************
// When PR_PHYSICS_GPU is defined, this file includes view3d-12 headers and
// provides the full D3D12 compute shader pipeline. When not defined, stub
// implementations are provided that assert on use. This prevents the linker
// from pulling in view3d-12 symbols when GPU integration is not needed.
//
// To enable GPU integration:
//   1. Define PR_PHYSICS_GPU in the consuming project
//   2. Link view3d-12-static.lib (or equivalent) for compute shader support
//   3. Set Engine::UseGpu = true in engine.h
#include "pr/physics-2/integrator/gpu_integrator.h"

#ifdef PR_PHYSICS_GPU

#include "pr/view3d-12/compute/gpu.h"
#include "pr/view3d-12/compute/gpu_job.h"
#include "pr/view3d-12/compute/compute_pso.h"
#include "pr/view3d-12/compute/compute_step.h"
#include "pr/view3d-12/shaders/shader.h"
#include "pr/view3d-12/shaders/shader_include_handler.h"
#include "pr/view3d-12/shaders/shader_registers.h"
#include "pr/view3d-12/utility/root_signature.h"
#include "pr/view3d-12/utility/wrappers.h"
#include "pr/common/resource.h"

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

	// The full GpuIntegrator implementation, hidden behind the pimpl in the public header.
	// Owns the D3D12 compute pipeline: device wrapper, shader, root signature, PSO,
	// structured buffers for body dynamics and debug output.
	struct GpuIntegrator
	{
		Gpu m_gpu;                                // Lightweight D3D12 wrapper (device + command queue)
		ComputeStep m_cs_integrate;               // Root signature + PSO for the integration shader
		D3DPtr<ID3D12Resource> m_r_bodies;        // GPU buffer: RWStructuredBuffer<RigidBodyDynamics>
		D3DPtr<ID3D12Resource> m_r_output;        // GPU buffer: RWStructuredBuffer<IntegrateOutput>
		int m_capacity;                           // Maximum number of bodies the buffers can hold

		explicit GpuIntegrator(ID3D12Device4* device, int max_bodies)
			: m_gpu(device)
			, m_cs_integrate()
			, m_r_bodies()
			, m_r_output()
			, m_capacity(max_bodies)
		{
			CompileShader();
			CreateBuffers();
		}

		// Integrate all bodies in the dynamics buffer on the GPU.
		// Uploads the buffer → dispatches compute → reads back results.
		// On return, 'dynamics' is updated in-place with the post-integration state.
		void Integrate(std::span<RigidBodyDynamics> dynamics, float dt, std::span<IntegrateOutput> output)
		{
			auto body_count = static_cast<int>(dynamics.size());
			if (body_count == 0) return;
			assert(body_count <= m_capacity && "Too many bodies for GPU buffer capacity");

			auto device = static_cast<ID3D12Device4*>(m_gpu);

			// Create a compute job for this integration step.
			// GpuJob is self-contained: owns command list, allocator, barriers, upload/readback.
			GpuJob<D3D12_COMMAND_LIST_TYPE_COMPUTE> job(device, "PhysicsIntegrate", 0xFF4080FF, 2);

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

	private:

		// Compile the integration compute shader from embedded resources and create the
		// root signature and pipeline state object.
		void CompileShader()
		{
			auto device = static_cast<ID3D12Device4*>(m_gpu);

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
				.Create(device, "Physics:IntegrateSig");

			m_cs_integrate.m_pso = ComputePSO(m_cs_integrate.m_sig.get(), bytecode)
				.Create(device, "Physics:IntegratePSO");
		}

		// Create GPU buffers for body dynamics and debug output using raw D3D12 API.
		// We bypass ResourceFactory (which requires a Renderer) since we only need
		// simple DEFAULT heap buffers with UAV access for compute.
		void CreateBuffers()
		{
			auto device = static_cast<ID3D12Device4*>(m_gpu);

			auto create_buffer = [&](UINT64 size_bytes, D3DPtr<ID3D12Resource>& out_resource, char const* name)
			{
				auto desc = D3D12_RESOURCE_DESC
				{
					.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
					.Alignment = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT,
					.Width = size_bytes,
					.Height = 1,
					.DepthOrArraySize = 1,
					.MipLevels = 1,
					.Format = DXGI_FORMAT_UNKNOWN,
					.SampleDesc = {.Count = 1, .Quality = 0},
					.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
					.Flags = D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS,
				};
				auto heap_props = D3D12_HEAP_PROPERTIES
				{
					.Type = D3D12_HEAP_TYPE_DEFAULT,
					.CPUPageProperty = D3D12_CPU_PAGE_PROPERTY_UNKNOWN,
					.MemoryPoolPreference = D3D12_MEMORY_POOL_UNKNOWN,
					.CreationNodeMask = 1,
					.VisibleNodeMask = 1,
				};
				auto hr = device->CreateCommittedResource(
					&heap_props, D3D12_HEAP_FLAG_NONE,
					&desc, D3D12_RESOURCE_STATE_UNORDERED_ACCESS,
					nullptr, __uuidof(ID3D12Resource),
					reinterpret_cast<void**>(out_resource.address_of()));
				if (FAILED(hr))
					throw std::runtime_error(std::string("Failed to create GPU buffer: ") + name);
			};

			create_buffer(m_capacity * sizeof(RigidBodyDynamics), m_r_bodies, "Physics:BodyDynamics");
			create_buffer(m_capacity * sizeof(IntegrateOutput), m_r_output, "Physics:IntegrateOutput");
		}
	};

	// Custom deleter implementation (GpuIntegrator is complete here)
	void GpuIntegratorDeleter::operator()(GpuIntegrator* p) const
	{
		delete p;
	}

	// Factory function — creates a GpuIntegrator instance.
	// Called by the application to initialise GPU-accelerated integration.
	GpuIntegratorPtr CreateGpuIntegrator(ID3D12Device4* device, int max_bodies)
	{
		return GpuIntegratorPtr(new GpuIntegrator(device, max_bodies));
	}

	// Free function to run GPU integration. This is the pimpl boundary — engine.cpp
	// calls this without needing the full GpuIntegrator definition.
	void GpuIntegrate(GpuIntegrator& gpu, std::span<RigidBodyDynamics> dynamics, float dt, std::span<IntegrateOutput> output)
	{
		gpu.Integrate(dynamics, dt, output);
	}

	// Return the D3D12 device owned by this integrator.
	ID3D12Device4* GpuIntegratorDevice(GpuIntegrator& gpu)
	{
		return static_cast<ID3D12Device4*>(gpu.m_gpu);
	}

	// Return the Gpu instance as a void* for sharing with other GPU subsystems.
	void* GpuIntegratorGpuPtr(GpuIntegrator& gpu)
	{
		return &gpu.m_gpu;
	}
}

#else // !PR_PHYSICS_GPU — Stub implementations when GPU support is not enabled

#include <cassert>

namespace pr::physics
{
	void GpuIntegratorDeleter::operator()(GpuIntegrator*) const
	{
		// No-op: GPU integrator should never be created when PR_PHYSICS_GPU is not defined
	}

	GpuIntegratorPtr CreateGpuIntegrator(ID3D12Device4*, int)
	{
		assert(false && "GPU integration requires PR_PHYSICS_GPU to be defined");
		return {};
	}

	void GpuIntegrate(GpuIntegrator&, std::span<RigidBodyDynamics>, float, std::span<IntegrateOutput>)
	{
		assert(false && "GPU integration requires PR_PHYSICS_GPU to be defined");
	}

	ID3D12Device4* GpuIntegratorDevice(GpuIntegrator&)
	{
		assert(false && "GPU integration requires PR_PHYSICS_GPU to be defined");
		return nullptr;
	}

	void* GpuIntegratorGpuPtr(GpuIntegrator&)
	{
		assert(false && "GPU integration requires PR_PHYSICS_GPU to be defined");
		return nullptr;
	}
}

#endif // PR_PHYSICS_GPU
