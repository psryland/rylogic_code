// Fluid
#include "src/grid_partition.h"
#include "src/particle.h"

using namespace pr::rdr12;

namespace pr::fluid
{
	struct EReg
	{
		inline static constexpr ECBufReg Constants = ECBufReg::b0;
		inline static constexpr EUAVReg Positions = EUAVReg::u0;
		inline static constexpr EUAVReg GridHash = EUAVReg::u1;
		inline static constexpr EUAVReg PosIndex = EUAVReg::u2;
		inline static constexpr EUAVReg IdxStart = EUAVReg::u3;
		inline static constexpr EUAVReg IdxCount = EUAVReg::u4;
	};

	static constexpr iv3 CellCountDimension(1024, 1, 1);
	static constexpr iv3 PosCountDimension(1024, 1, 1);

	// Generate a hash from a quantised grid position.
	inline uint32_t Hash(iv3 grid)
	{
		const iv3 prime = { 73856093, 19349663, 83492791 };
		auto product = grid * prime;
		auto hash = static_cast<uint32_t>(product.x ^ product.y ^ product.z);
		return hash % GridPartition::CellCount;
	}

	// ctor
	GridPartition::GridPartition(Renderer& rdr, float scale)
		: m_rdr(&rdr)
		, m_job(rdr.D3DDevice(), "GridPartition", 0xFF3178A9)
		, m_init()
		, m_populate()
		, m_build()
		, m_positions()
		, m_grid_hash()
		, m_pos_index()
		, m_idx_start()
		, m_idx_count()
		, m_sorter(rdr)
		, m_size()
		, m_scale(scale)
	{
		auto device = rdr.D3DDevice();
		auto source = resource::Read<char>(L"GPU_GRID_PARTITION_HLSL", L"TEXT");
		auto args = std::vector<wchar_t const*>{ L"-E<entry_point_placeholder>", L"-Tcs_6_6", L"-O3", L"-Zi" };

		// Init
		{
			m_init.m_sig = rdr12::RootSig(ERootSigFlags::ComputeOnly)
				.U32(EReg::Constants, 3)
				.Uav(EReg::IdxStart)
				.Uav(EReg::IdxCount)
				.Create(device);

			args[0] = L"-EInit";
			auto bytecode = rdr12::CompileShader(source, args);
			m_init.m_pso = ComputePSO(m_init.m_sig.get(), bytecode)
				.Create(device, "GridPartition:Init");
		}

		// Populate
		{
			m_populate.m_sig = rdr12::RootSig(ERootSigFlags::ComputeOnly)
				.U32(EReg::Constants, 3)
				.Uav(EReg::Positions)
				.Uav(EReg::GridHash)
				.Uav(EReg::PosIndex)
				.Create(device);

			args[0] = L"-EPopulate";
			auto bytecode = rdr12::CompileShader(source, args);
			m_populate.m_pso = ComputePSO(m_populate.m_sig.get(), bytecode)
				.Create(device, "GridPartition:Populate");
		}

		// Build lookup
		{
			m_build.m_sig = rdr12::RootSig(ERootSigFlags::ComputeOnly)
				.U32(EReg::Constants, 3)
				.Uav(EReg::GridHash)
				.Uav(EReg::IdxStart)
				.Uav(EReg::IdxCount)
				.Create(device);

			args[0] = L"-EBuildLookup";
			auto bytecode = rdr12::CompileShader(source, args);
			m_build.m_pso = ComputePSO(m_build.m_sig.get(), bytecode)
				.Create(device, "GridPartition:BuildLookup");
		}

		// Create static buffers
		{
			auto& desc = ResDesc::Buf(CellCount, sizeof(uint32_t), nullptr, alignof(uint32_t)).usage(EUsage::UnorderedAccess);
			m_idx_start = rdr.res().CreateResource(desc, "GridPartition:IdxStart");
			m_idx_count = rdr.res().CreateResource(desc, "GridPartition:IdxCount");
		}
	}

	// Ensure the buffers are large enough
	void GridPartition::Resize(int64_t size)
	{
		if (size <= m_size)
			return;

		// Positions
		{
			auto& desc = ResDesc::Buf(size, sizeof(v3), nullptr, alignof(v3)).usage(EUsage::UnorderedAccess);
			m_positions = m_rdr->res().CreateResource(desc, "GridPartition:Positions");
		}

		// Grid hash
		{
			auto& desc = ResDesc::Buf(size, sizeof(uint32_t), nullptr, alignof(uint32_t)).usage(EUsage::UnorderedAccess);
			m_grid_hash = m_rdr->res().CreateResource(desc, "GridPartition:GridHash");
		}
		
		// Position indices
		{
			auto& desc = ResDesc::Buf(size, sizeof(uint32_t), nullptr, alignof(uint32_t)).usage(EUsage::UnorderedAccess);
			m_pos_index = m_rdr->res().CreateResource(desc, "GridPartition:PosIndex");
		}
		
		// Resize the sorter
		{
			// Point the sort and payload buffers of the sorter to our grid-hash and pos-index
			// buffer so that we don't need to copy data from 'm_grid_hash' to 'm_sort[0]' etc.
			m_sorter.Bind(size, m_grid_hash, m_pos_index);
		}

		m_size = size;
	}

	// Update the spatial partitioning data structure
	void GridPartition::Update(std::span<Particle const> particles)
	{
		// Ensure the buffers are large enough
		Resize(ssize(particles));

		BarrierBatch barriers(m_job.m_cmd_list);

		// Upload the particle positions
		{
			auto buf = m_job.m_upload.Alloc(ssize(particles) * sizeof(v3), alignof(v3));
			auto ptr = buf.ptr<v3>();
			for (auto& particle : particles)
				*ptr++ = particle.m_pos.xyz;

			barriers.Transition(m_positions.get(), D3D12_RESOURCE_STATE_COPY_DEST);
			barriers.Commit();

			m_job.m_cmd_list.CopyBufferRegion(m_positions.get(), 0, buf.m_res, buf.m_ofs, buf.m_size);

			barriers.Transition(m_positions.get(), D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE);
			barriers.Commit();
		}

		// Reset the index start/count buffers
		{
			std::array<uint32_t, 3> constants = { CellCount, 0, 0 };
			m_job.m_cmd_list.SetPipelineState(m_init.m_pso.get());
			m_job.m_cmd_list.SetComputeRootSignature(m_init.m_sig.get());
			m_job.m_cmd_list.SetComputeRoot32BitConstants(0, isize(constants), constants.data(), 0);
			m_job.m_cmd_list.SetComputeRootUnorderedAccessView(1, m_idx_start->GetGPUVirtualAddress());
			m_job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_idx_count->GetGPUVirtualAddress());
			m_job.m_cmd_list.Dispatch(DispatchCount({ CellCount, 1, 1 }, CellCountDimension));
		}

		// Find the grid cell hash for each position
		{
			std::array<uint32_t, 3> constants = { CellCount, s_cast<uint32_t>(m_size), std::bit_cast<uint32_t>(m_scale) };
			m_job.m_cmd_list.SetPipelineState(m_populate.m_pso.get());
			m_job.m_cmd_list.SetComputeRootSignature(m_populate.m_sig.get());
			m_job.m_cmd_list.SetComputeRoot32BitConstants(0, isize(constants), constants.data(), 0);
			m_job.m_cmd_list.SetComputeRootUnorderedAccessView(1, m_positions->GetGPUVirtualAddress());
			m_job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_grid_hash->GetGPUVirtualAddress());
			m_job.m_cmd_list.SetComputeRootUnorderedAccessView(3, m_pos_index->GetGPUVirtualAddress());
			m_job.m_cmd_list.Dispatch(DispatchCount({ s_cast<int>(m_size), 1, 1 }, PosCountDimension));
		}

		// Sort the cell hashes and position indices so that they're contiguous
		{
			m_sorter.Sort(m_job.m_cmd_list);
		}

		// Build the lookup data structure
		{
			std::array<uint32_t, 3> constants = { CellCount, s_cast<uint32_t>(m_size), std::bit_cast<uint32_t>(m_scale) };
			m_job.m_cmd_list.SetPipelineState(m_build.m_pso.get());
			m_job.m_cmd_list.SetComputeRootSignature(m_build.m_sig.get());
			m_job.m_cmd_list.SetComputeRoot32BitConstants(0, isize(constants), constants.data(), 0);
			m_job.m_cmd_list.SetComputeRootUnorderedAccessView(1, m_grid_hash->GetGPUVirtualAddress());
			m_job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_idx_start->GetGPUVirtualAddress());
			m_job.m_cmd_list.SetComputeRootUnorderedAccessView(3, m_idx_count->GetGPUVirtualAddress());
			m_job.m_cmd_list.Dispatch(DispatchCount({ s_cast<int>(m_size), 1, 1 }, PosCountDimension));
		}

		// Read back the index start/count buffers and lookup table
		GpuReadbackBuffer::Allocation lookup, idx_start, idx_count;
		{
			barriers.Transition(m_idx_start.get(), D3D12_RESOURCE_STATE_COPY_SOURCE);
			barriers.Transition(m_idx_count.get(), D3D12_RESOURCE_STATE_COPY_SOURCE);
			barriers.Transition(m_pos_index.get(), D3D12_RESOURCE_STATE_COPY_SOURCE);
			barriers.Commit();
			{
				auto buf = m_job.m_readback.Alloc(m_size * sizeof(uint32_t), alignof(uint32_t));
				m_job.m_cmd_list.CopyBufferRegion(buf.m_res, buf.m_ofs, m_pos_index.get(), 0, buf.m_size);
				lookup = buf;
			}
			{
				auto buf = m_job.m_readback.Alloc(CellCount * sizeof(uint32_t), alignof(uint32_t));
				m_job.m_cmd_list.CopyBufferRegion(buf.m_res, buf.m_ofs, m_idx_start.get(), 0, buf.m_size);
				idx_start = buf;
			}
			{
				auto buf = m_job.m_readback.Alloc(CellCount * sizeof(uint32_t), alignof(uint32_t));
				m_job.m_cmd_list.CopyBufferRegion(buf.m_res, buf.m_ofs, m_idx_count.get(), 0, buf.m_size);
				idx_count = buf;
			}
		}

		// k go
		m_job.Run();

		// Create the spatial partition structure
		{
			// The spatially ordered list of particle indices
			m_spatial.resize(ssize(particles));
			memcpy(m_spatial.data(), lookup.ptr<int32_t>(), m_spatial.size() * sizeof(int32_t));

			// The map from cell hash to index start/count
			m_lookup.resize(CellCount);
			auto idx_start_ptr = idx_start.ptr<int32_t>();
			auto idx_count_ptr = idx_count.ptr<int32_t>();
			for (auto& cell : m_lookup)
				cell = {*idx_start_ptr++, *idx_count_ptr++};
		}
	}

	#pragma warning(push)
	#pragma warning(disable : 4702) // unreachable code

	// Find all particles within 'radius' of 'position'
	void GridPartition::Find(v4_cref position, float radius, std::span<Particle const> particles, std::function<void(Particle const&, float)> found) const
	{
		//assert(radius == m_radius); // for now...
		auto radius_sq = radius * radius;

		// Find the cell that 'position' is in
		auto cell = To<iv3>(position.xyz * m_scale);

		// Generate the cell keys for the neighbouring cells (in parallel)
		for (auto dz : { 0, -1, +1 })
		{
			for (auto dy : { 0, -1, +1 })
			{
				for (auto dx : { 0, -1, +1 })
				{
					auto hash = Hash(cell + iv3(dx, dy, dz));
					auto& idx = m_lookup[hash];
					for (int i = idx.start, iend = idx.start + idx.count; i != iend; ++i)
					{
						auto& particle = particles[m_spatial[i]];
						//auto dist_sq = LengthSq(position - particle.m_pos);
						//if (dist_sq < radius_sq)
						auto dist_sq = 0.0f;
							found(particle, dist_sq);
					}
					break;
				}
				if constexpr (Dimensions == 1)
					break;
			}
			if constexpr (Dimensions == 2)
				break;
		}
	}

	#pragma warning(pop)
}
