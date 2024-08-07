// Fluid
#include "src/spatial_partition.h"
#include "src/particle.h"

using namespace pr::rdr12;

namespace pr::fluid
{
	static constexpr iv3 CellCountDimension(1024, 1, 1);
	static constexpr iv3 PosCountDimension(1024, 1, 1);

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
		uint32_t NumPositions;
		uint32_t CellCount;
		float GridScale;
	};

	// ctor
	SpatialPartition::SpatialPartition(Renderer& rdr, int cell_count, float grid_scale, std::wstring_view position_layout)
		: m_rdr(&rdr)
		//, m_job(rdr.D3DDevice(), "SpatialPartition", 0xFF3178A9)
		, m_init()
		, m_populate()
		, m_build()
		, m_grid_hash()
		, m_pos_index()
		, m_idx_start()
		, m_idx_count()
		, m_sorter(rdr)
		, m_size()
		, m_grid_scale(grid_scale)
		, m_cell_count(cell_count)
		, m_spatial()
		, m_lookup()
	{
		auto device = rdr.D3DDevice();
		auto source = resource::Read<char>(L"SPATIAL_PARTITION_HLSL", L"TEXT");
		auto pos_type = std::format(L"-DPOS_TYPE={}", position_layout);
		auto include_handler = rdr12::ResourceIncludeHandler{};

		// Init
		{
			auto args = std::array<wchar_t const*, 5>{ L"-EInit", pos_type.c_str(), L"-Tcs_6_6", L"-O3", L"-Zi" };
			auto bytecode = rdr12::CompileShader(source, args, &include_handler);
			m_init.m_sig = rdr12::RootSig(ERootSigFlags::ComputeOnly)
				.U32(EReg::Constants, 3)
				.Uav(EReg::IdxStart)
				.Uav(EReg::IdxCount)
				.Create(device, "SpatialPartition:InitSig");
			m_init.m_pso = ComputePSO(m_init.m_sig.get(), bytecode)
				.Create(device, "SpatialPartition:InitPSO");
		}

		// Populate
		{
			auto args = std::array<wchar_t const*, 5>{ L"-EPopulate", pos_type.c_str(), L"-Tcs_6_6", L"-O3", L"-Zi" };
			auto bytecode = rdr12::CompileShader(source, args, &include_handler);
			m_populate.m_sig = rdr12::RootSig(ERootSigFlags::ComputeOnly)
				.U32(EReg::Constants, 3)
				.Uav(EReg::Positions)
				.Uav(EReg::GridHash)
				.Uav(EReg::PosIndex)
				.Create(device, "SpatialPartition:PopulateSig");
			m_populate.m_pso = ComputePSO(m_populate.m_sig.get(), bytecode)
				.Create(device, "SpatialPartition:PopulatePSO");
		}

		// Build lookup
		{
			auto args = std::array<wchar_t const*, 5>{ L"-EBuildLookup", pos_type.c_str(), L"-Tcs_6_6", L"-O3", L"-Zi" };
			auto bytecode = rdr12::CompileShader(source, args, &include_handler);
			m_build.m_sig = rdr12::RootSig(ERootSigFlags::ComputeOnly)
				.U32(EReg::Constants, 3)
				.Uav(EReg::GridHash)
				.Uav(EReg::IdxStart)
				.Uav(EReg::IdxCount)
				.Create(device, "SpatialPartition:BuildLookupSig");
			m_build.m_pso = ComputePSO(m_build.m_sig.get(), bytecode)
				.Create(device, "SpatialPartition:BuildLookupPSO");
		}

		// Create static buffers
		{
			auto desc = ResDesc::Buf(m_cell_count, sizeof(uint32_t), nullptr, alignof(uint32_t)).usage(EUsage::UnorderedAccess);
			m_idx_start = rdr.res().CreateResource(desc, "SpatialPartition:IdxStart");
			m_idx_count = rdr.res().CreateResource(desc, "SpatialPartition:IdxCount");
		}
	}
	
	// The number of cells in the grid
	int SpatialPartition::CellCount() const
	{
		return m_cell_count;
	}

	
	// The scaling factor to convert from world space to grid cell coordinate
	float SpatialPartition::GridScale() const
	{
		return m_grid_scale;
	}

	// Ensure the buffers are large enough
	void SpatialPartition::Resize(int64_t size)
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

	// Update the spatial partitioning data structure
	void SpatialPartition::Update(rdr12::ComputeJob& job, int64_t count, D3DPtr<ID3D12Resource> positions, bool readback)
	{
		// Ensure the buffer sizes are correct
		Resize(count);

		BarrierBatch barriers(job.m_cmd_list);

		// Reset the index start/count buffers
		{
			auto constants = cbGridPartition{ .NumPositions = s_cast<uint32_t>(count), .CellCount = s_cast<uint32_t>(m_cell_count), .GridScale = m_grid_scale };
			job.m_cmd_list.SetPipelineState(m_init.m_pso.get());
			job.m_cmd_list.SetComputeRootSignature(m_init.m_sig.get());
			job.m_cmd_list.SetComputeRoot32BitConstants(0, 3, &constants, 0);
			job.m_cmd_list.SetComputeRootUnorderedAccessView(1, m_idx_start->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_idx_count->GetGPUVirtualAddress());
			job.m_cmd_list.Dispatch(DispatchCount({ m_cell_count, 1, 1 }, CellCountDimension));
		}

		// Find the grid cell hash for each position
		{
			auto constants = cbGridPartition{ .NumPositions = s_cast<uint32_t>(count), .CellCount = s_cast<uint32_t>(m_cell_count), .GridScale = m_grid_scale };
			job.m_cmd_list.SetPipelineState(m_populate.m_pso.get());
			job.m_cmd_list.SetComputeRootSignature(m_populate.m_sig.get());
			job.m_cmd_list.SetComputeRoot32BitConstants(0, 3, &constants, 0);
			job.m_cmd_list.SetComputeRootUnorderedAccessView(1, positions->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_grid_hash->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(3, m_pos_index->GetGPUVirtualAddress());
			job.m_cmd_list.Dispatch(DispatchCount({ s_cast<int>(m_size), 1, 1 }, PosCountDimension));
		}

		// Sort the cell hashes and position indices so that they're contiguous
		{
			m_sorter.Sort(job.m_cmd_list);
		}

		// Build the lookup data structure
		{
			auto constants = cbGridPartition{ .NumPositions = s_cast<uint32_t>(count), .CellCount = s_cast<uint32_t>(m_cell_count), .GridScale = m_grid_scale };
			job.m_cmd_list.SetPipelineState(m_build.m_pso.get());
			job.m_cmd_list.SetComputeRootSignature(m_build.m_sig.get());
			job.m_cmd_list.SetComputeRoot32BitConstants(0, 3, &constants, 0);
			job.m_cmd_list.SetComputeRootUnorderedAccessView(1, m_grid_hash->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_idx_start->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(3, m_idx_count->GetGPUVirtualAddress());
			job.m_cmd_list.Dispatch(DispatchCount({ s_cast<int>(m_size), 1, 1 }, PosCountDimension));
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
				auto buf = job.m_readback.Alloc(m_size * sizeof(uint32_t), alignof(uint32_t));
				job.m_cmd_list.CopyBufferRegion(buf.m_res, buf.m_ofs, m_pos_index.get(), 0, buf.m_size);
				lookup = buf;
			}
			{
				auto buf = job.m_readback.Alloc(m_cell_count * sizeof(uint32_t), alignof(uint32_t));
				job.m_cmd_list.CopyBufferRegion(buf.m_res, buf.m_ofs, m_idx_start.get(), 0, buf.m_size);
				idx_start = buf;
			}
			{
				auto buf = job.m_readback.Alloc(m_cell_count * sizeof(uint32_t), alignof(uint32_t));
				job.m_cmd_list.CopyBufferRegion(buf.m_res, buf.m_ofs, m_idx_count.get(), 0, buf.m_size);
				idx_count = buf;
			}
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
			m_lookup.resize(m_cell_count);
			auto idx_start_ptr = idx_start.ptr<int32_t>();
			auto idx_count_ptr = idx_count.ptr<int32_t>();
			for (auto& cell : m_lookup)
				cell = {*idx_start_ptr++, *idx_count_ptr++};
		}
		else
		{
			m_spatial.clear();
			m_lookup.clear();
		}
	}
}
