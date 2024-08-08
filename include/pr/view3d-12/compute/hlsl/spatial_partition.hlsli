// Spatial partition
#pragma once

#if 0 // Expected Buffers

// The indices of particle positions sorted spatially
//RWStructuredBuffer<uint> m_spatial;

// The lowest index (in m_spatial) for each cell hash (length CellCount)
RWStructuredBuffer<uint> m_idx_start;

// The number of positions for each cell hash (length CellCount)
RWStructuredBuffer<uint> m_idx_count;

#endif

static const uint FNV_offset_basis32 = 2166136261U;
static const uint FNV_prime32 = 16777619U;

// Convert a floating point position into a grid cell coordinate
inline int3 GridCell(float3 position, uniform float grid_scale)
{
	return int3(ceil(position * grid_scale));
}

// Accumulative hash function
inline uint Hash(int value, uint hash = FNV_offset_basis32)
{
	return hash = (value ^ hash) * FNV_prime32;
}

// Generate a hash from a grid cell coordinate.
inline uint Hash(int3 grid, uniform uint cell_count)
{
	return Hash(grid.x, Hash(grid.y, Hash(grid.z))) % cell_count;
}

// A coroutine context for iterating over neighbouring particles
struct FindIter
{
	int3 lwr;
	int3 upr;
	int3 cell;
	int2 idx_range;
};

// Find all particles within 'radius' of 'position'
inline FindIter Find(float3 position, float3 radius, uniform float grid_scale, uniform uint cell_count)
{
	FindIter iter;
	iter.lwr = GridCell(position - radius, grid_scale);
	iter.upr = GridCell(position + radius, grid_scale);
	iter.cell = iter.lwr;
	iter.idx_range = int2(0, 0);
	--iter.cell.x;
	return iter;
}

// Advance the find iterator to the next cell. On return the 'idx_range' field contains the range
// of indices in the spatially sorted index buffer.
inline bool DoFind(inout FindIter iter, uniform uint cell_count)
{
	// Advance to the next cell
	++iter.cell.x;
	if (iter.cell.x == iter.upr.x)
	{
		iter.cell.x = iter.lwr.x;
		++iter.cell.y;
		if (iter.cell.y == iter.upr.y)
		{
			iter.cell.y = iter.lwr.y;
			++iter.cell.z;
			if (iter.cell.z == iter.upr.z)
				return false;
		}
	}

	// Get the range of indices in 'cell'
	uint hash = Hash(iter.cell, cell_count);
	iter.idx_range = int2(m_idx_start[hash], m_idx_start[hash] + m_idx_count[hash]);
	return true;
}