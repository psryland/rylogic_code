// Fluid
#include "src/grid_partition.h"
#include "src/particle.h"

using namespace pr::rdr12;

namespace pr::fluid
{
	enum class EParam {};
	enum class ESamp {};

	static constexpr iv3 InitDimension(1024, 1, 1);
	static constexpr iv3 PopulateDimension(1024, 1, 1);

	// Generate a hash from a quantised grid position.
	inline uint32_t Hash(iv3 grid)
	{
		constexpr uint32_t prime1 = 73856093;
		constexpr uint32_t prime2 = 19349663;
		constexpr uint32_t prime3 = 83492791;
		auto hash = (grid.x * prime1) ^ (grid.y * prime2) ^ (grid.z * prime3);
		return hash % GridPartition::CellCount;
	}

	// ctor
	GridPartition::GridPartition(Renderer& rdr, float scale)
		: m_rdr(&rdr)
		, m_job(rdr.D3DDevice(), "GridPartition", 0xFF3178A9)
		, m_init()
		, m_positions()
		, m_grid_hash()
		, m_start_idx()
		, m_sorter(rdr)
		, m_size()
		, m_scale(scale)
	{
		auto device = rdr.D3DDevice();
		auto source = resource::Read<char>(L"GPU_GRID_PARTITION_HLSL", L"TEXT");
		auto args = std::vector<wchar_t const*>{ L"-E<entry_point_placeholder>", L"-Tcs_6_6", L"-O3", L"-Zi" };

		// Init
		{
			rdr12::RootSig<EParam, ESamp> sig(ERootSigFlags::ComputeOnly);
			sig.U32(EParam(0), ECBufReg::b0, 3); // constants
			sig.Uav(EParam(1), EUAVReg::u2); // start_idx
			m_init.m_sig = sig.Create(device);

			args[0] = L"-EInit";
			auto bytecode = rdr12::CompileShader(source, args);
			ComputePSO pso(m_init.m_sig.get(), bytecode);
			m_init.m_pso = pso.Create(device, "GridPartition:Init");
		}

		// Populate
		{
			rdr12::RootSig<EParam, ESamp> sig(ERootSigFlags::ComputeOnly);
			sig.U32(EParam(0), ECBufReg::b0, 3); // constants
			sig.Uav(EParam(1), EUAVReg::u0); // positions
			sig.Uav(EParam(2), EUAVReg::u1); // grid_hash
			sig.Uav(EParam(3), EUAVReg::u2); // start_idx
			m_populate.m_sig = sig.Create(device);

			args[0] = L"-EPopulate";
			auto bytecode = rdr12::CompileShader(source, args);
			ComputePSO pso(m_populate.m_sig.get(), bytecode);
			m_populate.m_pso = pso.Create(device, "GridPartition:Populate");
		}

		// Create static buffers
		{
			auto& desc = ResDesc::Buf(CellCount, sizeof(uint32_t), nullptr, alignof(uint32_t)).usage(EUsage::UnorderedAccess);
			m_start_idx = rdr.res().CreateResource(desc, "GridPartition:Histogram");
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
		
		// Resize the sorter
		{
			m_sorter.Resize(size);

			// Point the primary sort buffer of the sorter to our grid hash buffer
			// so that we don't need to copy data from 'm_grid_hash' to 'm_sort[0]'
			m_sorter.m_sort[0] = m_grid_hash;
		}

		m_size = size;
	}

	// Update the spatial partitioning data structure
	void GridPartition::Update(std::span<Particle const> particles)
	{
		// Ensure the buffers are large enough
		Resize(ssize(particles));

		GpuReadbackBuffer::Allocation start_idx, hashes, lookup;
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

		// Reset the histogram
		{
			std::array<uint32_t, 3> constants = { CellCount, 0, 0 };
			m_job.m_cmd_list.SetPipelineState(m_init.m_pso.get());
			m_job.m_cmd_list.SetComputeRootSignature(m_init.m_sig.get());
			m_job.m_cmd_list.SetComputeRoot32BitConstants(0, isize(constants), constants.data(), 0);
			m_job.m_cmd_list.SetComputeRootUnorderedAccessView(1, m_start_idx->GetGPUVirtualAddress());
			m_job.m_cmd_list.Dispatch(DispatchCount({ CellCount, 1, 1 }, InitDimension));
		}

		// Find the grid cell hash for each position
		{
			std::array<uint32_t, 3> constants = { CellCount, s_cast<uint32_t>(m_size), std::bit_cast<uint32_t>(m_scale) };
			m_job.m_cmd_list.SetPipelineState(m_populate.m_pso.get());
			m_job.m_cmd_list.SetComputeRootSignature(m_populate.m_sig.get());
			m_job.m_cmd_list.SetComputeRoot32BitConstants(0, isize(constants), constants.data(), 0);
			m_job.m_cmd_list.SetComputeRootUnorderedAccessView(1, m_positions->GetGPUVirtualAddress());
			m_job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_grid_hash->GetGPUVirtualAddress());
			m_job.m_cmd_list.SetComputeRootUnorderedAccessView(3, m_start_idx->GetGPUVirtualAddress());
			m_job.m_cmd_list.Dispatch(DispatchCount({ s_cast<int>(m_size), 1, 1 }, PopulateDimension));
		}

		// Initialize the payload buffer and 
		// sort the cell hashes so that they're contiguous
		{
			m_sorter.InitPayload(m_job.m_cmd_list);
			m_sorter.Sort(m_job.m_cmd_list);
		}

		// Read back the histogram and the lookup 
		{
			barriers.Transition(m_grid_hash.get(), D3D12_RESOURCE_STATE_COPY_SOURCE);
			barriers.Transition(m_start_idx.get(), D3D12_RESOURCE_STATE_COPY_SOURCE);
			barriers.Transition(m_sorter.m_payload[0].get(), D3D12_RESOURCE_STATE_COPY_SOURCE);
			barriers.Commit();
			{
				auto buf = m_job.m_readback.Alloc(CellCount * sizeof(uint32_t), alignof(uint32_t));
				m_job.m_cmd_list.CopyBufferRegion(buf.m_res, buf.m_ofs, m_start_idx.get(), 0, buf.m_size);
				start_idx = buf;
			}
			{
				auto buf = m_job.m_readback.Alloc(m_size * sizeof(uint32_t), alignof(uint32_t));
				m_job.m_cmd_list.CopyBufferRegion(buf.m_res, buf.m_ofs, m_grid_hash.get(), 0, buf.m_size);
				hashes = buf;
			}
			{
				auto buf = m_job.m_readback.Alloc(m_size * sizeof(uint32_t), alignof(uint32_t));
				m_job.m_cmd_list.CopyBufferRegion(buf.m_res, buf.m_ofs, m_sorter.m_payload[0].get(), 0, buf.m_size);
				lookup = buf;
			}
		}

		// k go
		m_job.Run();

		// Create the spatial partition structure
		{
			m_lookup.resize(CellCount);
			memcpy(m_lookup.data(), start_idx.ptr<uint32_t>(), CellCount * sizeof(uint32_t));

			m_spatial.resize(ssize(particles));
			auto hash_ptr = hashes.ptr<uint32_t>();
			auto start_ptr = start_idx.ptr<uint32_t>();
			for (int i = 0, iend = isize(m_spatial); i != iend; ++i)
				m_spatial[i] = (int64_t(*hash_ptr++) << 32) | int64_t(*start_ptr++);
		}
	}

	#pragma warning(push)
	#pragma warning(disable : 4702) // unreachable code

	// Find all particles within 'radius' of 'position'
	void GridPartition::Find(v4_cref position, float radius, std::span<Particle const> particles, std::function<void(Particle const&, float)> found) const
	{
		//assert(radius == m_radius); // for now...
		constexpr uint32_t IndexMask = 0xFFFFFFFF;
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
					auto key = Hash(cell + iv3(dx, dy, dz));
					auto idx = m_lookup[key];
					if (idx == -1)
						continue;

					for (; (m_spatial[idx] >> 32) == key; ++idx)
					{
						//auto& particle = particles[m_spatial[idx] & IndexMask];
						//auto dist_sq = LengthSq(position - particle.m_pos);
						//if (dist_sq < radius_sq)
						//	found(particle, dist_sq);
					}
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
