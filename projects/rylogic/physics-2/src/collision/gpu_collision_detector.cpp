//*********************************************
// Physics Engine — GPU Collision Detector Implementation
//  Copyright (C) Rylogic Ltd 2026
//*********************************************
// When PR_PHYSICS_GPU is defined, this file includes view3d-12 headers and
// provides the full D3D12 compute shader pipeline for narrow phase collision.
// When not defined, stub implementations are provided that assert on use.
//
// To enable GPU collision:
//   1. Define PR_PHYSICS_GPU in the consuming project
//   2. Link view3d-12-static.lib (or equivalent)
//   3. Embed physics_shaders.rc (gjk.hlsl) as a resource
#include "pr/physics-2/collision/gpu_collision_detector.h"

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
	static constexpr int CollisionThreadGroupSize = 64;

	// Constant buffer layout matching the HLSL cbCollision declaration.
	struct alignas(16) cbCollision
	{
		uint32_t pair_count;
		uint32_t pad0;
		uint32_t pad1;
		uint32_t pad2;
	};
	static_assert(sizeof(cbCollision) == 16);

	// Register assignments for the collision root signature.
	// b0 = constants, t0 = shapes (SRV), t1 = pairs (SRV), t2 = verts (SRV),
	// u0 = contacts (UAV), u1 = counters (UAV)
	struct EColReg
	{
		inline static constexpr ECBufReg Params   = ECBufReg::b0;
		inline static constexpr ESRVReg  Shapes   = ESRVReg::t0;
		inline static constexpr ESRVReg  Pairs    = ESRVReg::t1;
		inline static constexpr ESRVReg  Verts    = ESRVReg::t2;
		inline static constexpr EUAVReg  Contacts = EUAVReg::u0;
		inline static constexpr EUAVReg  Counters = EUAVReg::u1;
	};

	// The full GpuCollisionDetector implementation, hidden behind the pimpl.
	// Owns the D3D12 compute pipeline for GJK collision detection.
	struct GpuCollisionDetector
	{
		Gpu m_gpu;                                 // Lightweight D3D12 wrapper (device + command queue)
		ComputeStep m_cs_gjk;                      // Root signature + PSO for the GJK shader
		D3DPtr<ID3D12Resource> m_r_shapes;         // GPU buffer: StructuredBuffer<GpuShape>
		D3DPtr<ID3D12Resource> m_r_pairs;          // GPU buffer: StructuredBuffer<GpuCollisionPair>
		D3DPtr<ID3D12Resource> m_r_verts;          // GPU buffer: StructuredBuffer<float4>
		D3DPtr<ID3D12Resource> m_r_contacts;       // GPU buffer: RWStructuredBuffer<GpuContact>
		D3DPtr<ID3D12Resource> m_r_counters;       // GPU buffer: RWStructuredBuffer<uint>
		int m_max_pairs;
		int m_max_shapes;
		int m_max_verts;

		explicit GpuCollisionDetector(ID3D12Device4* device, int max_pairs, int max_shapes, int max_verts)
			: m_gpu(device)
			, m_cs_gjk()
			, m_r_shapes()
			, m_r_pairs()
			, m_r_verts()
			, m_r_contacts()
			, m_r_counters()
			, m_max_pairs(max_pairs)
			, m_max_shapes(max_shapes)
			, m_max_verts(max_verts)
		{
			CompileShader();
			CreateBuffers();
		}

		// Run collision detection on the GPU.
		// Uploads shapes, pairs, and vertices → dispatches GJK shader → reads back contacts.
		int DetectCollisions(
			std::span<GpuShape const> shapes,
			std::span<GpuCollisionPair const> pairs,
			std::span<v4 const> verts,
			std::vector<GpuContact>& out_contacts)
		{
			auto pair_count = static_cast<int>(pairs.size());
			auto shape_count = static_cast<int>(shapes.size());
			auto vert_count = static_cast<int>(verts.size());
			if (pair_count == 0) return 0;

			assert(pair_count <= m_max_pairs && "Too many pairs for GPU buffer capacity");
			assert(shape_count <= m_max_shapes && "Too many shapes for GPU buffer capacity");
			assert(vert_count <= m_max_verts && "Too many vertices for GPU buffer capacity");

			auto device = static_cast<ID3D12Device4*>(m_gpu);

			// Create a compute job for this collision detection step.
			GpuJob<D3D12_COMMAND_LIST_TYPE_COMPUTE> job(device, "PhysicsCollision", 0xFF80FF40, 5);

			// Upload shapes buffer
			{
				job.m_barriers.Transition(m_r_shapes.get(), D3D12_RESOURCE_STATE_COPY_DEST);
				job.m_barriers.Commit();

				auto upload = job.m_upload.Alloc<GpuShape>(shape_count);
				memcpy(upload.ptr<GpuShape>(), shapes.data(), shape_count * sizeof(GpuShape));
				job.m_cmd_list.CopyBufferRegion(m_r_shapes.get(), 0, upload);

				job.m_barriers.Transition(m_r_shapes.get(), D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE);
				job.m_barriers.Commit();
			}

			// Upload pairs buffer
			{
				job.m_barriers.Transition(m_r_pairs.get(), D3D12_RESOURCE_STATE_COPY_DEST);
				job.m_barriers.Commit();

				auto upload = job.m_upload.Alloc<GpuCollisionPair>(pair_count);
				memcpy(upload.ptr<GpuCollisionPair>(), pairs.data(), pair_count * sizeof(GpuCollisionPair));
				job.m_cmd_list.CopyBufferRegion(m_r_pairs.get(), 0, upload);

				job.m_barriers.Transition(m_r_pairs.get(), D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE);
				job.m_barriers.Commit();
			}

			// Upload vertices buffer (may be empty if no polytopes/triangles)
			if (vert_count > 0)
			{
				job.m_barriers.Transition(m_r_verts.get(), D3D12_RESOURCE_STATE_COPY_DEST);
				job.m_barriers.Commit();

				auto upload = job.m_upload.Alloc<v4>(vert_count);
				memcpy(upload.ptr<v4>(), verts.data(), vert_count * sizeof(v4));
				job.m_cmd_list.CopyBufferRegion(m_r_verts.get(), 0, upload);

				job.m_barriers.Transition(m_r_verts.get(), D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE);
				job.m_barriers.Commit();
			}

			// Zero the counter buffer before dispatch
			{
				job.m_barriers.Transition(m_r_counters.get(), D3D12_RESOURCE_STATE_COPY_DEST);
				job.m_barriers.Commit();

				auto upload = job.m_upload.Alloc<GpuCollisionCounters>(1);
				auto* counters = upload.ptr<GpuCollisionCounters>();
				*counters = {};
				job.m_cmd_list.CopyBufferRegion(m_r_counters.get(), 0, upload);

				job.m_barriers.Transition(m_r_counters.get(), D3D12_RESOURCE_STATE_UNORDERED_ACCESS);
				job.m_barriers.Commit();
			}

			// Dispatch the GJK compute shader
			{
				auto cb = cbCollision{ .pair_count = static_cast<uint32_t>(pair_count) };

				job.m_cmd_list.SetPipelineState(m_cs_gjk.m_pso.get());
				job.m_cmd_list.SetComputeRootSignature(m_cs_gjk.m_sig.get());
				job.m_cmd_list.AddComputeRoot32BitConstants(cb);
				job.m_cmd_list.AddComputeRootShaderResourceView(m_r_shapes->GetGPUVirtualAddress());
				job.m_cmd_list.AddComputeRootShaderResourceView(m_r_pairs->GetGPUVirtualAddress());
				job.m_cmd_list.AddComputeRootShaderResourceView(m_r_verts->GetGPUVirtualAddress());
				job.m_cmd_list.AddComputeRootUnorderedAccessView(m_r_contacts->GetGPUVirtualAddress());
				job.m_cmd_list.AddComputeRootUnorderedAccessView(m_r_counters->GetGPUVirtualAddress());

				auto dispatch_count = (pair_count + CollisionThreadGroupSize - 1) / CollisionThreadGroupSize;
				job.m_cmd_list.Dispatch(dispatch_count, 1, 1);

				// UAV barriers to ensure compute finishes before readback
				job.m_barriers.UAV(m_r_contacts.get());
				job.m_barriers.UAV(m_r_counters.get());
			}

			// Read back contacts and counter
			GpuReadbackBuffer::Allocation readback_contacts;
			GpuReadbackBuffer::Allocation readback_counters;
			{
				job.m_barriers.Transition(m_r_contacts.get(), D3D12_RESOURCE_STATE_COPY_SOURCE);
				job.m_barriers.Transition(m_r_counters.get(), D3D12_RESOURCE_STATE_COPY_SOURCE);
				job.m_barriers.Commit();

				readback_contacts = job.m_readback.Alloc<GpuContact>(pair_count);
				job.m_cmd_list.CopyBufferRegion(readback_contacts, m_r_contacts.get(), 0);

				readback_counters = job.m_readback.Alloc<GpuCollisionCounters>(1);
				job.m_cmd_list.CopyBufferRegion(readback_counters, m_r_counters.get(), 0);

				// Transition back for next frame
				job.m_barriers.Transition(m_r_contacts.get(), D3D12_RESOURCE_STATE_UNORDERED_ACCESS);
				job.m_barriers.Transition(m_r_counters.get(), D3D12_RESOURCE_STATE_UNORDERED_ACCESS);
			}

			// Execute and wait for GPU completion
			job.Run();

			// Read the contact count
			auto* counters = readback_counters.ptr<GpuCollisionCounters>();
			auto contact_count = static_cast<int>(counters->contact_count);
			contact_count = std::min(contact_count, pair_count); // safety clamp

			// Copy contacts to output
			out_contacts.resize(contact_count);
			if (contact_count > 0)
				memcpy(out_contacts.data(), readback_contacts.ptr<GpuContact>(), contact_count * sizeof(GpuContact));

			return contact_count;
		}

	private:

		// Compile the GJK compute shader from embedded resources.
		void CompileShader()
		{
			auto device = static_cast<ID3D12Device4*>(m_gpu);

			auto shader_source = resource::Read<char>(L"PHYSICS_GJK_HLSL", L"TEXT");
			auto compiler = ShaderCompiler{}
				.Source(shader_source)
				.Includes({new ResourceIncludeHandler, true})
				.EntryPoint(L"CSCollisionDetect")
				.ShaderModel(L"cs_6_0")
				.Optimise();

			auto bytecode = compiler.Compile();

			// Root signature: constants + 3 SRVs + 2 UAVs
			m_cs_gjk.m_sig = RootSig(ERootSigFlags::ComputeOnly)
				.U32<cbCollision>(EColReg::Params)
				.SRV(EColReg::Shapes)
				.SRV(EColReg::Pairs)
				.SRV(EColReg::Verts)
				.UAV(EColReg::Contacts)
				.UAV(EColReg::Counters)
				.Create(device, "Physics:GjkSig");

			m_cs_gjk.m_pso = ComputePSO(m_cs_gjk.m_sig.get(), bytecode)
				.Create(device, "Physics:GjkPSO");
		}

		// Create GPU buffers for collision pipeline.
		void CreateBuffers()
		{
			auto device = static_cast<ID3D12Device4*>(m_gpu);

			auto create_buffer = [&](UINT64 size_bytes, D3D12_RESOURCE_FLAGS flags, D3D12_RESOURCE_STATES initial_state,
				D3DPtr<ID3D12Resource>& out_resource, char const* name)
			{
				auto desc = D3D12_RESOURCE_DESC{
					.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
					.Alignment = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT,
					.Width = std::max<UINT64>(size_bytes, 16),
					.Height = 1,
					.DepthOrArraySize = 1,
					.MipLevels = 1,
					.Format = DXGI_FORMAT_UNKNOWN,
					.SampleDesc = {.Count = 1, .Quality = 0},
					.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
					.Flags = flags,
				};
				auto heap_props = D3D12_HEAP_PROPERTIES{
					.Type = D3D12_HEAP_TYPE_DEFAULT,
					.CPUPageProperty = D3D12_CPU_PAGE_PROPERTY_UNKNOWN,
					.MemoryPoolPreference = D3D12_MEMORY_POOL_UNKNOWN,
					.CreationNodeMask = 1,
					.VisibleNodeMask = 1,
				};
				auto hr = device->CreateCommittedResource(
					&heap_props, D3D12_HEAP_FLAG_NONE,
					&desc, initial_state,
					nullptr, __uuidof(ID3D12Resource),
					reinterpret_cast<void**>(out_resource.address_of()));
				if (FAILED(hr))
					throw std::runtime_error(std::string("Failed to create GPU buffer: ") + name);
			};

			// SRV buffers
			create_buffer(m_max_shapes * sizeof(GpuShape),
				D3D12_RESOURCE_FLAG_NONE, D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE,
				m_r_shapes, "Physics:Shapes");

			create_buffer(m_max_pairs * sizeof(GpuCollisionPair),
				D3D12_RESOURCE_FLAG_NONE, D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE,
				m_r_pairs, "Physics:Pairs");

			create_buffer(m_max_verts * sizeof(v4),
				D3D12_RESOURCE_FLAG_NONE, D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE,
				m_r_verts, "Physics:CollisionVerts");

			// UAV buffers
			create_buffer(m_max_pairs * sizeof(GpuContact),
				D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS, D3D12_RESOURCE_STATE_UNORDERED_ACCESS,
				m_r_contacts, "Physics:Contacts");

			create_buffer(sizeof(GpuCollisionCounters),
				D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS, D3D12_RESOURCE_STATE_UNORDERED_ACCESS,
				m_r_counters, "Physics:Counters");
		}
	};

	// Pimpl boundary functions
	void GpuCollisionDetectorDeleter::operator()(GpuCollisionDetector* p) const { delete p; }

	GpuCollisionDetectorPtr CreateGpuCollisionDetector(ID3D12Device4* device, int max_pairs, int max_shapes, int max_verts)
	{
		return GpuCollisionDetectorPtr(new GpuCollisionDetector(device, max_pairs, max_shapes, max_verts));
	}

	int GpuDetectCollisions(
		GpuCollisionDetector& detector,
		std::span<GpuShape const> shapes,
		std::span<GpuCollisionPair const> pairs,
		std::span<v4 const> verts,
		std::vector<GpuContact>& out_contacts)
	{
		return detector.DetectCollisions(shapes, pairs, verts, out_contacts);
	}
}

#else // !PR_PHYSICS_GPU

#include <cassert>

namespace pr::physics
{
	void GpuCollisionDetectorDeleter::operator()(GpuCollisionDetector*) const {}

	GpuCollisionDetectorPtr CreateGpuCollisionDetector(ID3D12Device4*, int, int, int)
	{
		assert(false && "GPU collision requires PR_PHYSICS_GPU");
		return {};
	}

	int GpuDetectCollisions(GpuCollisionDetector&, std::span<GpuShape const>, std::span<GpuCollisionPair const>, std::span<v4 const>, std::vector<GpuContact>&)
	{
		assert(false && "GPU collision requires PR_PHYSICS_GPU");
		return 0;
	}
}

#endif
