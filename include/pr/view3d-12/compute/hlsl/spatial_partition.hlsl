// Fluid
// Algorithm:
//  1. Find the cell hash value for each position
//  2. Build a histogram of positions per cell
//  3. Sort the 'm_grid_hash' buffer by hash value so that all positions in the same cell are contiguous
//  4. Create a lookup from cell hash to the start index of the cell in the sorted buffer
// Defines:
//   POS_TYPE - Define the element structure of the 'positions' buffer. The field 'pos' is
//      expected to be the position information. It can be float3 or float4.
#ifndef POS_TYPE
#define POS_TYPE struct PosType { float3 pos; }
#endif

static const uint CellCountDimension = 1024;
static const uint PosCountDimension = 1024;

// Constants
cbuffer cbGridPartition : register(b0)
{
	int NumPositions; // The length of 'm_positions'
	int CellCount;    // The maximum number of grid cells
	float GridScale;   // The quantising factor to apply to the positions
};

POS_TYPE;

// The positions to sort into the grid
RWStructuredBuffer<PosType> m_positions : register(u0);

// The grid cell hash for each position. (length of m_positions)
RWStructuredBuffer<uint> m_grid_hash : register(u1);

// The spatially sorted index buffer (length m_positions)
RWStructuredBuffer<uint> m_spatial : register(u2);

// The lowest index (from m_positions) for each cell hash (length CellCount)
RWStructuredBuffer<uint> m_idx_start : register(u3);

// The number of positions for each cell hash (length CellCount)
RWStructuredBuffer<uint> m_idx_count : register(u4);

#include "spatial_partition.hlsli"

// Reset the start/count arrays
[numthreads(CellCountDimension, 1, 1)]
void Init(uint3 gtid : SV_DispatchThreadID, uint3 gid : SV_GroupID)
{
	if (gtid.x >= CellCount)
		return;
	
	m_idx_start[gtid.x] = 0xFFFFFFFF;
	m_idx_count[gtid.x] = 0;
}

// Populate the grid hash buffer with the hash value for each position
[numthreads(PosCountDimension, 1, 1)]
void Populate(uint3 gtid : SV_DispatchThreadID, uint3 gid : SV_GroupID)
{
	if (gtid.x >= NumPositions)
		return;

	int3 grid = GridCell(m_positions[gtid.x].pos.xyz, GridScale);
	uint hash = Hash(grid, CellCount);
	m_grid_hash[gtid.x] = hash;
	m_spatial[gtid.x] = gtid.x;
}

// Build the lookup structure (run post-sort)
[numthreads(PosCountDimension, 1, 1)]
void BuildSpatial(uint3 gtid : SV_DispatchThreadID, uint3 gid : SV_GroupID)
{
	if (gtid.x >= NumPositions)
		return;

	uint hash = m_grid_hash[gtid.x];
	
	// Record the smallest index for each cell hash value
	InterlockedMin(m_idx_start[hash], gtid.x);
	
	// Record the number of positions for each cell hash value
	InterlockedAdd(m_idx_count[hash], 1);
}

