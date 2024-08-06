// Fluid
#pragma once
#include "src/forward.h"
#include "src/ispatial_partition.h"

namespace pr::fluid
{
	struct GridPartition :ISpatialPartition
	{
		// Notes:
		//  - Although this is a "grid" it actually doesn't matter what the grid dimensions are.
		//    Really, it's just hashing positions to a 1D array.
		using GpuRadixSorter = rdr12::GpuRadixSort<uint32_t, uint32_t>;
		using Cell = struct { int32_t start; int32_t count; };
		static constexpr int CellCount = 64;// * 64 * 64;

		rdr12::Renderer* m_rdr;             // The renderer instance to use to run the compute shader
		rdr12::ComputeJob m_job;            // The job to run the compute shader
		rdr12::ComputeStep m_init;          // Reset buffers
		rdr12::ComputeStep m_populate;      // Populate the grid cells
		rdr12::ComputeStep m_build;         // Build the lookup data structure
		D3DPtr<ID3D12Resource> m_positions; // The positions of the objects/particles
		D3DPtr<ID3D12Resource> m_grid_hash; // The cell hash for each position
		D3DPtr<ID3D12Resource> m_pos_index; // The cell hash for each position
		D3DPtr<ID3D12Resource> m_idx_start; // The smallest index for each cell hash value
		D3DPtr<ID3D12Resource> m_idx_count; // The number of particles in each cell
		GpuRadixSorter m_sorter;            // Sort the cell hashes on the GPU
		int64_t m_size;                     // The maximum number of positions in m_positions
		float m_scale;                      // Scale positions to grid cells. E.g. scale = 10, then 0.1 -> 1, 0.2 -> 2, etc
		std::vector<Cell> m_lookup;         // A map (length CellCount) from cell hash to (start,count) into 'm_spatial'
		std::vector<int32_t> m_spatial;     // The indices of particles ordered by locality

		GridPartition(rdr12::Renderer& rdr, float scale);

		// Ensure the buffers are large enough
		void Resize(int64_t size);

		// Spatially partition the particles for faster locality testing
		void Update(std::span<Particle const> particles) override;

		// Find all particles within 'radius' of 'position'
		void Find(v4_cref position, float radius, std::span<Particle const> particles, std::function<void(Particle const& particle, float dist_sq)> found) const override;
	};

	uint32_t Hash(iv3 grid);
}
