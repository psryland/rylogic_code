// Fluid
// Alg:
//  1. Find the cell hash value for each position
//  2. Build a histogram of positions per cell
//  3. Sort the 'm_grid_hash' buffer by hash value so that all positions in the same cell are contiguous
//  4. Create a lookup from cell hash to the start index of the cell in the sorted buffer

// The positions to sort into the grid
RWStructuredBuffer<float3> m_positions : register(u0);

// The grid cell hash for each position.
// The length of this buffer is the same as the positions buffer.
RWStructuredBuffer<uint> m_grid_hash : register(u1);

// The counts per grid cell
RWStructuredBuffer<uint> m_histogram : register(u2);

// Constants
cbuffer cbGridPartition : register(b0)
{
	uint CellCount;    // The maximum number of grid cells
	uint NumPositions; // The length of 'm_positions'
	float GridScale;   // The quantising factor to apply to the positions
};

// Generate a hash from a quantised grid position.
inline uint Hash(uint3 grid)
{
	const uint prime1 = 73856093;
	const uint prime2 = 19349663;
	const uint prime3 = 83492791;
	uint hash = (grid.x * prime1) ^ (grid.y * prime2) ^ (grid.z * prime3);
	return hash % CellCount;
}

// Reset the histogram of positions per cell
[numthreads(1024, 1, 1)]
void Init(uint3 gtid : SV_DispatchThreadID, uint3 gid : SV_GroupID)
{
	if (gtid.x >= CellCount)
		return;
	
	m_histogram[gtid.x] = 0;
}

// Populate the grid hash buffer with the hash value for each position
[numthreads(1024, 1, 1)]
void Populate(uint3 gtid : SV_DispatchThreadID, uint3 gid : SV_GroupID)
{
	if (gtid.x >= NumPositions)
		return;

	uint3 grid = uint3(m_positions[gtid.x] * GridScale);
	uint hash = Hash(grid);
	
	m_grid_hash[gtid.x] = hash;
	InterlockedAdd(m_histogram[hash], 1);
}

