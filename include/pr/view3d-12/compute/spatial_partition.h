//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/compute/gpu_job.h"
#include "pr/view3d-12/compute/compute_pso.h"
#include "pr/view3d-12/compute/compute_step.h"
#include "pr/view3d-12/compute/gpu_radix_sort.h"
#include "pr/view3d-12/shaders/shader.h"
#include "pr/view3d-12/shaders/shader_include_handler.h"
#include "pr/view3d-12/utility/root_signature.h"
#include "pr/view3d-12/utility/barrier_batch.h"
#include "pr/view3d-12/main/renderer.h"

namespace pr::rdr12::compute::spatial_partition
{
	inline static constexpr uint32_t FNV_offset_basis32 = 2166136261U;
	inline static constexpr uint32_t FNV_prime32 = 16777619U;

	// Convert a floating point position into a grid cell coordinate
	inline iv3 GridCell(v4_cref position, float grid_scale)
	{
		return To<iv3>(Ceil(position.xyz * grid_scale));
	}

	// Accumulative hash function
	inline uint32_t Hash(int value, uint32_t hash = FNV_offset_basis32)
	{
		return hash = (value ^ hash) * FNV_prime32;
	}

	// Generate a hash from a grid cell coordinate.
	inline uint32_t Hash(iv3 grid, int cell_count)
	{
		return Hash(grid.x, Hash(grid.y, Hash(grid.z))) % cell_count;
	}

	// Grid-based Spatial Partitioning
	struct SpatialPartition
	{
		// Notes:
		//  - Although this is a "grid" it actually doesn't matter what the grid dimensions are.
		//    Really, it's just hashing positions to a 1D array.
		//  - The 'm_positions' buffer is expected to be provided by the caller. The control its
		//    layout and inform this type by providing a 'position_layout' string. This string should
		//    have this form: "struct PosType { float4 _dummy; float4 pos; float4 _dummy2; }". A field
		//    called 'pos' must exist and be a float4.

		inline static constexpr iv3 CellCountDimension = { 1024, 1, 1 };
		inline static constexpr iv3 PosCountDimension = { 1024, 1, 1 };

		struct EReg
		{
			inline static constexpr ECBufReg Constants = ECBufReg::b0;
			inline static constexpr EUAVReg Positions = EUAVReg::u0;
			inline static constexpr EUAVReg GridHash = EUAVReg::u1;
			inline static constexpr EUAVReg PosIndex = EUAVReg::u2;
			inline static constexpr EUAVReg IdxStart = EUAVReg::u3;
			inline static constexpr EUAVReg IdxCount = EUAVReg::u4;
		};
		struct Constants
		{
			int NumPositions; // The maximum number of positions in m_positions
			int CellCount;    // Scale positions to grid cells. E.g. scale = 10, then 0.1 -> 1, 0.2 -> 2, etc
			float GridScale;  // The number of cells in the grid

		};
		inline static constexpr int NumConstants = sizeof(Constants) / sizeof(uint32_t);

		using GpuRadixSorter = compute::gpu_radix_sort::GpuRadixSort<uint32_t, uint32_t>;
		using Cell = struct { int32_t start; int32_t count; };

		Renderer* m_rdr;                    // The renderer instance to use to run the compute shader
		ComputeStep m_init;                 // Reset buffers
		ComputeStep m_populate;             // Populate the grid cells
		ComputeStep m_build;                // Build the lookup data structure
		D3DPtr<ID3D12Resource> m_grid_hash; // The cell hash for each position
		D3DPtr<ID3D12Resource> m_pos_index; // The spatially sorted position indices
		D3DPtr<ID3D12Resource> m_idx_start; // The smallest index for each cell hash value
		D3DPtr<ID3D12Resource> m_idx_count; // The number of particles in each cell
		GpuRadixSorter m_sorter;            // Sort the cell hashes on the GPU
		Constants m_constants;        // The constants to pass to the compute shaders

		// This fields are used if the spatial partitioning data is copied back to the CPU.
		std::vector<int32_t> m_spatial;     // The spatially sorted position indices
		std::vector<Cell> m_lookup;         // A map (length CellCount) from cell hash to (start,count) into 'm_spatial'

		SpatialPartition(Renderer& rdr, int cell_count, float grid_scale, std::wstring_view position_layout)
			: m_rdr(&rdr)
			, m_init()
			, m_populate()
			, m_build()
			, m_grid_hash()
			, m_pos_index()
			, m_idx_start()
			, m_idx_count()
			, m_sorter(rdr)
			, m_constants({0, cell_count, grid_scale})
			, m_spatial()
			, m_lookup()
		{
			auto device = rdr.D3DDevice();
			auto compiler = ShaderCompiler{}
				.Source(resource::Read<char>(L"SPATIAL_PARTITION_HLSL", L"TEXT"))
				.Includes({ new ResourceIncludeHandler, true })
				.Define(L"POS_TYPE", position_layout)
				.ShaderModel(L"cs_6_6")
				.Optimise();

			// Init
			{
				auto bytecode = compiler.EntryPoint(L"Init").Compile();
				m_init.m_sig = RootSig(ERootSigFlags::ComputeOnly)
					.U32(EReg::Constants, NumConstants)
					.Uav(EReg::IdxStart)
					.Uav(EReg::IdxCount)
					.Create(device, "SpatialPartition:InitSig");
				m_init.m_pso = ComputePSO(m_init.m_sig.get(), bytecode)
					.Create(device, "SpatialPartition:InitPSO");
			}

			// Populate
			{
				auto bytecode = compiler.EntryPoint(L"Populate").Compile();
				m_populate.m_sig = RootSig(ERootSigFlags::ComputeOnly)
					.U32(EReg::Constants, NumConstants)
					.Uav(EReg::Positions)
					.Uav(EReg::GridHash)
					.Uav(EReg::PosIndex)
					.Create(device, "SpatialPartition:PopulateSig");
				m_populate.m_pso = ComputePSO(m_populate.m_sig.get(), bytecode)
					.Create(device, "SpatialPartition:PopulatePSO");
			}

			// Build lookup
			{
				auto bytecode = compiler.EntryPoint(L"BuildSpatial").Compile();
				m_build.m_sig = RootSig(ERootSigFlags::ComputeOnly)
					.U32(EReg::Constants, NumConstants)
					.Uav(EReg::GridHash)
					.Uav(EReg::IdxStart)
					.Uav(EReg::IdxCount)
					.Create(device, "SpatialPartition:BuildLookupSig");
				m_build.m_pso = ComputePSO(m_build.m_sig.get(), bytecode)
					.Create(device, "SpatialPartition:BuildLookupPSO");
			}

			// Create static buffers
			{
				auto desc = ResDesc::Buf(m_constants.CellCount, sizeof(uint32_t), nullptr, alignof(uint32_t)).usage(EUsage::UnorderedAccess);
				m_idx_start = rdr.res().CreateResource(desc, "SpatialPartition:IdxStart");
				m_idx_count = rdr.res().CreateResource(desc, "SpatialPartition:IdxCount");
			}
		}

		// The number of cells in the grid
		int CellCount() const
		{
			return m_constants.CellCount;
		}

		// The scaling factor to convert from world space to grid cell coordinate
		float GridScale() const
		{
			return m_constants.GridScale;
		}

		// Ensure the buffers are large enough
		void Resize(int size)
		{
			if (size <= m_constants.NumPositions)
				return;

			// Grid hash
			{
				auto desc = ResDesc::Buf(size, sizeof(uint32_t), nullptr, alignof(uint32_t)).usage(EUsage::UnorderedAccess);
				m_grid_hash = m_rdr->res().CreateResource(desc, "SpatialPartition:GridHash");
			}

			// Position indices
			{
				auto desc = ResDesc::Buf(size, sizeof(uint32_t), nullptr, alignof(uint32_t)).usage(EUsage::UnorderedAccess);
				m_pos_index = m_rdr->res().CreateResource(desc, "SpatialPartition:PosIndex");
			}

			// Resize the sorter
			{
				// Point the sort and payload buffers of the sorter to our grid-hash and pos-index
				// buffer so that we don't need to copy data from 'm_grid_hash' to 'm_sort[0]' etc.
				m_sorter.Bind(size, m_grid_hash, m_pos_index);
			}

			m_constants.NumPositions = size;
		}

		// Spatially partition the particles for faster locality testing
		void Update(ComputeJob& job, int count, D3DPtr<ID3D12Resource> positions, bool readback)
		{
			// Ensure the buffer sizes are correct
			Resize(count);

			BarrierBatch barriers(job.m_cmd_list);

			// Reset the index start/count buffers
			{
				job.m_cmd_list.SetPipelineState(m_init.m_pso.get());
				job.m_cmd_list.SetComputeRootSignature(m_init.m_sig.get());
				job.m_cmd_list.SetComputeRoot32BitConstants(0, NumConstants, &m_constants, 0);
				job.m_cmd_list.SetComputeRootUnorderedAccessView(1, m_idx_start->GetGPUVirtualAddress());
				job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_idx_count->GetGPUVirtualAddress());
				job.m_cmd_list.Dispatch(DispatchCount({ m_constants.CellCount, 1, 1 }, CellCountDimension));
			}

			// Find the grid cell hash for each position
			{
				job.m_cmd_list.SetPipelineState(m_populate.m_pso.get());
				job.m_cmd_list.SetComputeRootSignature(m_populate.m_sig.get());
				job.m_cmd_list.SetComputeRoot32BitConstants(0, NumConstants, &m_constants, 0);
				job.m_cmd_list.SetComputeRootUnorderedAccessView(1, positions->GetGPUVirtualAddress());
				job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_grid_hash->GetGPUVirtualAddress());
				job.m_cmd_list.SetComputeRootUnorderedAccessView(3, m_pos_index->GetGPUVirtualAddress());
				job.m_cmd_list.Dispatch(DispatchCount({ m_constants.NumPositions, 1, 1 }, PosCountDimension));
			}

			// Sort the cell hashes and position indices so that they're contiguous
			{
				m_sorter.Sort(job.m_cmd_list);
			}

			// Build the lookup data structure
			{
				job.m_cmd_list.SetPipelineState(m_build.m_pso.get());
				job.m_cmd_list.SetComputeRootSignature(m_build.m_sig.get());
				job.m_cmd_list.SetComputeRoot32BitConstants(0, NumConstants, &m_constants, 0);
				job.m_cmd_list.SetComputeRootUnorderedAccessView(1, m_grid_hash->GetGPUVirtualAddress());
				job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_idx_start->GetGPUVirtualAddress());
				job.m_cmd_list.SetComputeRootUnorderedAccessView(3, m_idx_count->GetGPUVirtualAddress());
				job.m_cmd_list.Dispatch(DispatchCount({ m_constants.NumPositions, 1, 1 }, PosCountDimension));
			}

			// Read back the index start/count buffers and lookup table
			GpuReadbackBuffer::Allocation lookup, idx_start, idx_count;
			if (readback)
			{
				barriers.Transition(m_idx_start.get(), D3D12_RESOURCE_STATE_COPY_SOURCE);
				barriers.Transition(m_idx_count.get(), D3D12_RESOURCE_STATE_COPY_SOURCE);
				barriers.Transition(m_pos_index.get(), D3D12_RESOURCE_STATE_COPY_SOURCE);
				barriers.Commit();
				{
					auto buf = job.m_readback.Alloc(m_constants.NumPositions * sizeof(uint32_t), alignof(uint32_t));
					job.m_cmd_list.CopyBufferRegion(buf.m_res, buf.m_ofs, m_pos_index.get(), 0, buf.m_size);
					lookup = buf;
				}
				{
					auto buf = job.m_readback.Alloc(m_constants.CellCount * sizeof(uint32_t), alignof(uint32_t));
					job.m_cmd_list.CopyBufferRegion(buf.m_res, buf.m_ofs, m_idx_start.get(), 0, buf.m_size);
					idx_start = buf;
				}
				{
					auto buf = job.m_readback.Alloc(m_constants.CellCount * sizeof(uint32_t), alignof(uint32_t));
					job.m_cmd_list.CopyBufferRegion(buf.m_res, buf.m_ofs, m_idx_count.get(), 0, buf.m_size);
					idx_count = buf;
				}
				barriers.Transition(m_idx_start.get(), D3D12_RESOURCE_STATE_UNORDERED_ACCESS);
				barriers.Transition(m_idx_count.get(), D3D12_RESOURCE_STATE_UNORDERED_ACCESS);
				barriers.Transition(m_pos_index.get(), D3D12_RESOURCE_STATE_UNORDERED_ACCESS);
				barriers.Commit();
			}

			// Create the spatial partition structure
			if (readback)
			{
				// k go
				job.Run();

				// The spatially ordered list of particle indices
				m_spatial.resize(count);
				memcpy(m_spatial.data(), lookup.ptr<int32_t>(), m_spatial.size() * sizeof(int32_t));

				// The map from cell hash to index start/count
				m_lookup.resize(m_constants.CellCount);
				auto idx_start_ptr = idx_start.ptr<int32_t>();
				auto idx_count_ptr = idx_count.ptr<int32_t>();
				for (auto& cell : m_lookup)
					cell = { *idx_start_ptr++, *idx_count_ptr++ };
			}
			else
			{
				m_spatial.clear();
				m_lookup.clear();
			}
		}

		// Find all particles in the cells overlapping 'volume'.
		template <typename PosType, typename FoundCB> requires std::is_invocable_v<FoundCB, PosType const&>
		void Find(BBox_cref volume, std::span<PosType const> particles, FoundCB found) const
		{
			assert(!m_spatial.empty() && "Requires Update() with 'readback' = true");

			auto lwr = GridCell(volume.Lower(), m_constants.GridScale);
			auto upr = GridCell(volume.Upper(), m_constants.GridScale);

			for (auto z = lwr.z; z <= upr.z; ++z)
			{
				for (auto y = lwr.y; y <= upr.y; ++y)
				{
					for (auto x = lwr.x; x <= upr.x; ++x)
					{
						auto cell = iv3(x, y, z);
						auto hash = Hash(cell, m_constants.CellCount);
						auto& idx = m_lookup[hash];
						for (int i = idx.start, iend = idx.start + idx.count; i != iend; ++i)
						{
							auto& particle = particles[m_spatial[i]];

							// Ignore cell hash collisions
							if (GridCell(particle.m_pos, m_constants.GridScale) != cell)
								continue;

							found(particle);
						}
					}
				}
			}
		}

		// Find all particles within 'radius' of 'position'
		template <typename PosType, typename FoundCB> requires std::is_invocable_v<FoundCB, PosType const&, float>
		void Find(v4_cref position, float radius, std::span<PosType const> particles, FoundCB found) const
		{
			auto radius_sq = radius * radius;
			Find(BBox(position, v4(radius)), particles, [=](auto const& particle)
			{
				auto dist_sq = LengthSq(position - particle.m_pos);
				if (dist_sq > radius_sq)
					return;

				found(particle, dist_sq);
			});
		}
	};
}
