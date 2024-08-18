//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/compute/gpu_job.h"
#include "pr/view3d-12/compute/compute_pso.h"
#include "pr/view3d-12/compute/compute_step.h"
#include "pr/view3d-12/compute/radix_sort/radix_sort.h"
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
		return hash = (value + hash) * FNV_prime32;
	}

	// Generate a hash from a grid cell coordinate.
	inline uint32_t CellHash(iv3 grid, int cell_count)
	{
		auto h1 = Hash(grid.x);
		auto h2 = Hash(grid.y);
		auto h3 = Hash(grid.z);
		constexpr uint32_t prime1 = 73856093;
		constexpr uint32_t prime2 = 19349663;
		constexpr uint32_t prime3 = 83492791;
		return (h1 * prime1 + h2 * prime2 + h3 * prime3) % cell_count;
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
		int m_size;                         // The size that the resources are set up for

		// These fields are used if the spatial partitioning data is copied back to the CPU.
		std::vector<int32_t> m_spatial;     // The spatially sorted position indices
		std::vector<Cell> m_lookup;         // A map (length CellCount) from cell hash to (start,count) into 'm_spatial'

		// Partitioning params
		struct ConfigData
		{
			int CellCount = 1021;   // The number of cells in the grid. (Primes are a good choice: 1021, 65521, 1048573, 16777213)
			float GridScale = 1.0f; // Scale positions to grid cells. E.g. scale = 10, then 0.1 -> 1, 0.2 -> 2, etc
		} Config;

		SpatialPartition(Renderer& rdr, std::wstring_view position_layout)
			: m_rdr(&rdr)
			, m_init()
			, m_populate()
			, m_build()
			, m_grid_hash()
			, m_pos_index()
			, m_idx_start()
			, m_idx_count()
			, m_sorter(rdr)
			, m_size()
			, m_spatial()
			, m_lookup()
			, Config()
		{
			CreateComputeSteps(position_layout);
		}

		// (Re)Initialise the spatial partitioning
		void Init(ConfigData const& config, EGpuFlush flush = EGpuFlush::Block)
		{
			Config = config;

			ResDesc desc = ResDesc::Buf(Config.CellCount, sizeof(uint32_t), nullptr, alignof(uint32_t)).usage(EUsage::UnorderedAccess);
			m_idx_start = m_rdr->res().CreateResource(desc, "SpatialPartition:IdxStart");
			m_idx_count = m_rdr->res().CreateResource(desc, "SpatialPartition:IdxCount");
			m_rdr->res().FlushToGpu(flush);
		}

		// Spatially partition the particles for faster locality testing
		void Update(GraphicsJob& job, int count, D3DPtr<ID3D12Resource> positions, bool readback)
		{
			DoUpdate(job, count, positions, readback);
		}

		// Find all particles in the cells overlapping 'volume'.
		template <typename PosType, typename FoundCB> requires std::is_invocable_v<FoundCB, PosType const&>
		void Find(BBox_cref volume, std::span<PosType const> particles, FoundCB found) const
		{
			assert(!m_spatial.empty() && "Requires Update() with 'readback' = true");

			auto lwr = GridCell(volume.Lower(), Config.GridScale);
			auto upr = GridCell(volume.Upper(), Config.GridScale);

			for (auto z = lwr.z; z <= upr.z; ++z)
			{
				for (auto y = lwr.y; y <= upr.y; ++y)
				{
					for (auto x = lwr.x; x <= upr.x; ++x)
					{
						auto cell = iv3(x, y, z);
						auto hash = CellHash(cell, Config.CellCount);
						auto& idx = m_lookup[hash];
						for (int i = idx.start, iend = idx.start + idx.count; i != iend; ++i)
						{
							auto& particle = particles[m_spatial[i]];

							// Ignore cell hash collisions
							if (GridCell(particle.m_pos, Config.GridScale) != cell)
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

	private:

		inline static constexpr int ThreadGroupSize = 1024;

		struct EReg
		{
			inline static constexpr ECBufReg Constants = ECBufReg::b0;
			inline static constexpr EUAVReg Positions = EUAVReg::u0;
			inline static constexpr EUAVReg GridHash = EUAVReg::u1;
			inline static constexpr EUAVReg PosIndex = EUAVReg::u2;
			inline static constexpr EUAVReg IdxStart = EUAVReg::u3;
			inline static constexpr EUAVReg IdxCount = EUAVReg::u4;
		};
		struct cbGridPartition
		{
			int NumPositions;
			int CellCount;
			float GridScale;
		};

		// Create constant buffer data for the grid partition parameters
		cbGridPartition GridPartitionCBuf(int count)
		{
			return cbGridPartition{
				.NumPositions = count,
				.CellCount = Config.CellCount,
				.GridScale = Config.GridScale,
			};
		}

		// Compute the compute shaders
		void CreateComputeSteps(std::wstring_view position_layout)
		{
			auto device = m_rdr->D3DDevice();
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
					.U32<cbGridPartition>(EReg::Constants)
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
					.U32<cbGridPartition>(EReg::Constants)
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
					.U32<cbGridPartition>(EReg::Constants)
					.Uav(EReg::GridHash)
					.Uav(EReg::IdxStart)
					.Uav(EReg::IdxCount)
					.Create(device, "SpatialPartition:BuildLookupSig");
				m_build.m_pso = ComputePSO(m_build.m_sig.get(), bytecode)
					.Create(device, "SpatialPartition:BuildLookupPSO");
			}
		}

		// Ensure the buffers are large enough to partition 'size' positions
		void Resize(int size)
		{
			if (size <= m_size)
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

			m_size = size;
		}

		// Spatially partition the particles for faster locality testing
		void DoUpdate(GraphicsJob& job, int count, D3DPtr<ID3D12Resource> positions, bool readback)
		{
			PIXBeginEvent(job.m_cmd_list.get(), 0xFFB36529, "SpatialPartition::Update");
			
			auto cb_params = GridPartitionCBuf(count);

			// Ensure the buffer sizes are correct
			Resize(count);

			// Reset the index start/count buffers
			{
				job.m_cmd_list.SetPipelineState(m_init.m_pso.get());
				job.m_cmd_list.SetComputeRootSignature(m_init.m_sig.get());
				job.m_cmd_list.SetComputeRoot32BitConstants(0, cb_params, 0);
				job.m_cmd_list.SetComputeRootUnorderedAccessView(1, m_idx_start->GetGPUVirtualAddress());
				job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_idx_count->GetGPUVirtualAddress());
				job.m_cmd_list.Dispatch(DispatchCount({ cb_params.CellCount, 1, 1 }, { ThreadGroupSize, 1, 1 }));
			}

			job.m_barriers.UAV(positions.get());
			job.m_barriers.Commit();

			// Find the grid cell hash for each position
			{
				job.m_cmd_list.SetPipelineState(m_populate.m_pso.get());
				job.m_cmd_list.SetComputeRootSignature(m_populate.m_sig.get());
				job.m_cmd_list.SetComputeRoot32BitConstants(0, cb_params, 0);
				job.m_cmd_list.SetComputeRootUnorderedAccessView(1, positions->GetGPUVirtualAddress());
				job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_grid_hash->GetGPUVirtualAddress());
				job.m_cmd_list.SetComputeRootUnorderedAccessView(3, m_pos_index->GetGPUVirtualAddress());
				job.m_cmd_list.Dispatch(DispatchCount({ cb_params.NumPositions, 1, 1 }, { ThreadGroupSize, 1, 1 }));
			}

			job.m_barriers.UAV(m_grid_hash.get());
			job.m_barriers.UAV(m_pos_index.get());
			job.m_barriers.Commit();

			// Sort the cell hashes and position indices so that they're contiguous
			{
				m_sorter.Sort(job.m_cmd_list);
			}

			job.m_barriers.UAV(m_grid_hash.get());
			job.m_barriers.UAV(m_idx_start.get());
			job.m_barriers.UAV(m_idx_count.get());
			job.m_barriers.Commit();

			// Build the lookup data structure
			{
				job.m_cmd_list.SetPipelineState(m_build.m_pso.get());
				job.m_cmd_list.SetComputeRootSignature(m_build.m_sig.get());
				job.m_cmd_list.SetComputeRoot32BitConstants(0, cb_params, 0);
				job.m_cmd_list.SetComputeRootUnorderedAccessView(1, m_grid_hash->GetGPUVirtualAddress());
				job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_idx_start->GetGPUVirtualAddress());
				job.m_cmd_list.SetComputeRootUnorderedAccessView(3, m_idx_count->GetGPUVirtualAddress());
				job.m_cmd_list.Dispatch(DispatchCount({ cb_params.NumPositions, 1, 1 }, { ThreadGroupSize, 1, 1 }));
			}

			job.m_barriers.UAV(m_idx_start.get());
			job.m_barriers.UAV(m_idx_count.get());
			job.m_barriers.UAV(m_pos_index.get());
			job.m_barriers.Commit();

			// Read back the index start/count buffers and lookup table
			GpuReadbackBuffer::Allocation lookup, idx_start, idx_count;
			if (readback)
			{
				job.m_barriers.Transition(m_idx_start.get(), D3D12_RESOURCE_STATE_COPY_SOURCE);
				job.m_barriers.Transition(m_idx_count.get(), D3D12_RESOURCE_STATE_COPY_SOURCE);
				job.m_barriers.Transition(m_pos_index.get(), D3D12_RESOURCE_STATE_COPY_SOURCE);
				job.m_barriers.Commit();
				{
					auto buf = job.m_readback.Alloc(cb_params.NumPositions * sizeof(uint32_t), alignof(uint32_t));
					job.m_cmd_list.CopyBufferRegion(buf.m_res, buf.m_ofs, m_pos_index.get(), 0, buf.m_size);
					lookup = buf;
				}
				{
					auto buf = job.m_readback.Alloc(cb_params.CellCount * sizeof(uint32_t), alignof(uint32_t));
					job.m_cmd_list.CopyBufferRegion(buf.m_res, buf.m_ofs, m_idx_start.get(), 0, buf.m_size);
					idx_start = buf;
				}
				{
					auto buf = job.m_readback.Alloc(cb_params.CellCount * sizeof(uint32_t), alignof(uint32_t));
					job.m_cmd_list.CopyBufferRegion(buf.m_res, buf.m_ofs, m_idx_count.get(), 0, buf.m_size);
					idx_count = buf;
				}
				job.m_barriers.Transition(m_idx_start.get(), D3D12_RESOURCE_STATE_UNORDERED_ACCESS);
				job.m_barriers.Transition(m_idx_count.get(), D3D12_RESOURCE_STATE_UNORDERED_ACCESS);
				job.m_barriers.Transition(m_pos_index.get(), D3D12_RESOURCE_STATE_UNORDERED_ACCESS);
				job.m_barriers.Commit();
			}

			PIXEndEvent(job.m_cmd_list.get());

			// Create the spatial partition structure
			if (readback)
			{
				// k go
				job.Run();

				// The spatially ordered list of particle indices
				m_spatial.resize(count);
				memcpy(m_spatial.data(), lookup.ptr<int32_t>(), m_spatial.size() * sizeof(int32_t));

				// The map from cell hash to index start/count
				m_lookup.resize(cb_params.CellCount);
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
	};
}
