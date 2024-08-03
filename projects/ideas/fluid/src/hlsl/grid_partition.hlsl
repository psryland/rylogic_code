// Fluid
// Alg:
//  1. Find the cell hash value for each position
//  2. Build a histogram of positions per cell
//  3. Sort the 'm_grid_hash' buffer by hash value so that all positions in the same cell are contiguous
//  4. Create a lookup from cell hash to the start index of the cell in the sorted buffer

// The positions to sort into the grid
RWStructuredBuffer<float3> m_positions : register(u0);

// The grid cell hash for each position. (length of m_positions)
RWStructuredBuffer<uint> m_grid_hash : register(u1);

// The lowest index (from m_positions) for each cell hash (length CellCount)
RWStructuredBuffer<uint> m_idx_start : register(u2);

// The number of positions for each cell hash (length CellCount)
RWStructuredBuffer<uint> m_idx_count : register(u3);

// Constants
cbuffer cbGridPartition : register(b0)
{
	uint CellCount;    // The maximum number of grid cells
	uint NumPositions; // The length of 'm_positions'
	float GridScale;   // The quantising factor to apply to the positions
};

// Generate a hash from a quantised grid position.
inline uint Hash(int3 grid)
{
	const int3 prime = int3(73856093, 19349663, 83492791);
	int3 product = grid * prime;
	uint hash = uint(product.x ^ product.y ^ product.z);
	return hash % CellCount;
}

// Reset the start index array
[numthreads(1024, 1, 1)]
void Init(uint3 gtid : SV_DispatchThreadID, uint3 gid : SV_GroupID)
{
	if (gtid.x >= CellCount)
		return;
	
	m_idx_start[gtid.x] = 0xFFFFFFFF;
	m_idx_count[gtid.x] = 0;

}

// Populate the grid hash buffer with the hash value for each position
[numthreads(1024, 1, 1)]
void Populate(uint3 gtid : SV_DispatchThreadID, uint3 gid : SV_GroupID)
{
	if (gtid.x >= NumPositions)
		return;

	int3 grid = int3(m_positions[gtid.x] * GridScale);
	uint hash = Hash(grid);
	m_grid_hash[gtid.x] = hash;
	
	// Record the smallest index for each cell hash value
	InterlockedMin(m_idx_start[hash], gtid.x);
	
	// Record the number of positions for each cell hash value
	InterlockedAdd(m_idx_count[hash], 1);
}

