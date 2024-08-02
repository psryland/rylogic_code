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
		
		//using SignaturePtr = D3DPtr<ID3D12RootSignature>;
		//using PipelineStatePtr = D3DPtr<ID3D12PipelineState>;
		//using GpuViewHeap = rdr12::GpuDescriptorHeap<D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV>;
		//using GpuSync = rdr12::GpuSync;
		//using GpuUploadBuffer = rdr12::GpuUploadBuffer;
		//using GpuReadbackBuffer = rdr12::GpuReadbackBuffer;
		//using CmdAllocPool = rdr12::ComCmdAllocPool;
		//using CmdList = rdr12::ComCmdList;
		//using KeepAlive = rdr12::KeepAlive;

		static constexpr int CellCount = 64 * 64 * 64;

		rdr12::Renderer* m_rdr;                // The renderer instance to use to run the compute shader
		rdr12::ComputeJob m_job;               // The job to run the compute shader
		rdr12::ComputeStep m_init;             // Reset buffers
		rdr12::ComputeStep m_populate;         // Populate the grid cells
		rdr12::GpuRadixSort<int,int> m_sorter; //
		D3DPtr<ID3D12Resource> m_positions;    // The positions of the objects/particles
		D3DPtr<ID3D12Resource> m_grid_hash;    // The cell hash for each position
		D3DPtr<ID3D12Resource> m_histogram;    // The number of positions in each cell
		int64_t m_size;                        // The maximum number of positions in m_positions
		float m_radius;                        // The radius of the particles

		//KeepAlive m_keep_alive;       // Keeps D3D objects alive until the GPU has finished with them
		//GpuViewHeap m_view_heap;      // GPU visible descriptor heap for CBV/SRV/UAV
		//SignaturePtr m_sig;           // Root signature for the compute shader
		//PipelineStatePtr m_pso;       // Pipeline state for the compute shader
		//GpuUploadBuffer m_upload;     // Upload buffer for the compute shader
		//GpuReadbackBuffer m_readback; // Readback buffer for the compute shader
		//D3DPtr<ID3D12Resource> m_bufB;
		//D3DPtr<ID3D12Resource> m_bufC;
		//D3D12_GPU_DESCRIPTOR_HANDLE m_uavA;
		//D3D12_GPU_DESCRIPTOR_HANDLE m_uavB;
		//D3D12_GPU_DESCRIPTOR_HANDLE m_uavC;


		GridPartition(rdr12::Renderer& rdr, float radius);

		// Ensure the buffers are large enough
		void Resize(int64_t size);

		// Spatially partition the particles for faster locality testing
		void Update(std::span<Particle const> particles) override;

		// Find all particles within 'radius' of 'position'
		void Find(v4_cref position, float radius, std::span<Particle const> particles, std::function<void(Particle const&, float)> found) const override;
	};
}
