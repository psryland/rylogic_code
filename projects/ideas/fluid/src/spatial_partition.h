// Fluid
#pragma once
#include "src/forward.h"
#include "src/ispatial_partition.h"

namespace pr::fluid
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
	struct SpatialPartition :ISpatialPartition
	{
		// Notes:
		//  - Although this is a "grid" it actually doesn't matter what the grid dimensions are.
		//    Really, it's just hashing positions to a 1D array.
		//  - The 'm_positions' buffer is expected to be provided by the caller. The control its
		//    layout and inform this type by providing a 'position_layout' string. This string should
		//    have this form: "struct PosType { float4 _dummy; float4 pos; float4 _dummy2; }". A field
		//    called 'pos' must exist and be a float4.

		using GpuRadixSorter = rdr12::GpuRadixSort<uint32_t, uint32_t>;
		using Cell = struct { int32_t start; int32_t count; };

		rdr12::Renderer* m_rdr;             // The renderer instance to use to run the compute shader
		rdr12::ComputeStep m_init;          // Reset buffers
		rdr12::ComputeStep m_populate;      // Populate the grid cells
		rdr12::ComputeStep m_build;         // Build the lookup data structure
		D3DPtr<ID3D12Resource> m_grid_hash; // The cell hash for each position
		D3DPtr<ID3D12Resource> m_pos_index; // The spatially sorted position indices
		D3DPtr<ID3D12Resource> m_idx_start; // The smallest index for each cell hash value
		D3DPtr<ID3D12Resource> m_idx_count; // The number of particles in each cell
		GpuRadixSorter m_sorter;            // Sort the cell hashes on the GPU
		int64_t m_size;                     // The maximum number of positions in m_positions
		float m_grid_scale;                 // Scale positions to grid cells. E.g. scale = 10, then 0.1 -> 1, 0.2 -> 2, etc
		int m_cell_count;                   // The number of cells in the grid 
		std::vector<int32_t> m_spatial;     // The spatially sorted position indices
		std::vector<Cell> m_lookup;         // A map (length CellCount) from cell hash to (start,count) into 'm_spatial'

		SpatialPartition(rdr12::Renderer& rdr, int cell_count, float grid_scale, std::wstring_view position_layout);

		// The number of cells in the grid
		int CellCount() const;

		// The scaling factor to convert from world space to grid cell coordinate
		float GridScale() const;

		// Ensure the buffers are large enough
		void Resize(int64_t size);

		// Spatially partition the particles for faster locality testing
		void Update(rdr12::ComputeJob& job, int64_t count, D3DPtr<ID3D12Resource> positions, bool readback) override;

		// Find all particles in the cells overlapping 'volume'
		template <typename PosType, typename FoundCB> requires std::is_invocable_v<FoundCB, PosType const&>
		void Find(BBox_cref volume, std::span<PosType const> particles, FoundCB found) const
		{
			auto lwr = GridCell(volume.Lower(), m_grid_scale);
			auto upr = GridCell(volume.Upper(), m_grid_scale);

			for (auto z = lwr.z; z <= upr.z; ++z)
			{
				for (auto y = lwr.y; y <= upr.y; ++y)
				{
					for (auto x = lwr.x; x <= upr.x; ++x)
					{
						auto cell = iv3(x, y, z);
						auto hash = Hash(cell, m_cell_count);
						auto& idx = m_lookup[hash];
						for (int i = idx.start, iend = idx.start + idx.count; i != iend; ++i)
						{
							auto& particle = particles[m_spatial[i]];

							// Ignore cell hash collisions
							if (GridCell(particle.m_pos, m_grid_scale) != cell)
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
