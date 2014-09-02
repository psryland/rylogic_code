//*******************************************************************************
// Terrain Exporter
//  Copyright © Rylogic Ltd 2009
//*******************************************************************************
#ifndef PR_TERRAIN_H
#define PR_TERRAIN_H
#pragma once

#include "pr/common/assert.h"
#include "pr/maths/maths.h"

namespace pr
{
	namespace terrain
	{
		// Constants
		enum { Version = 1 };							// The current terrain version
		
		// Scale distances in the planes
		// 4096.0f / 65536.0f = distance range / (1 << 16)
		inline float& PlaneWScale()						{ static float plane_w_scale = 0.0625f; return plane_w_scale; }

		// The height to return if a query point does not fall within a face
		inline float& DefaultHeight()					{ static float default_height = -maths::float_max; return default_height; }

		// For multi-layer terrain, return heights up to this far above the query point
		inline float& HeightTolerance()					{ static float height_tol = 0.5f; return height_tol; }

		// Limits
		enum ELimit
		{
			// The size of a terrain cell is always a multiple of 'ELimit_UnitSize'
			// The index in a CellInfo, used to refer to a cell, is in multiples of 'ELimit_UnitSize'
			ELimit_UnitSize					= 64,
			ELimit_MaxLayers				= 16,		// The maximum number of terrain layers at any one point
			ELimit_BIndexUnit				= 8,		// The step size in bytes that a BranchIndex represents
			ELimit_BIndexMax				= 127,		// The maximum value of a branch index
			ELimit_MaxCellSizeInBytes		= 8 * 255,	// The maximum size in bytes that we want terrain cells to be
		};

		typedef pr::int8	BranchIndex;		// Used to index branches/leaves within a bsp tree
		typedef pr::int16	BranchUnit;			// The units used for the constants in a line equation in Branch

		// A leaf containing a single plane of the terrain data
		// The planes stored in the leaves are in region space. The reason for this is that it gives 
		// a reasonable accuracy of the 'w' component of the planes (when stored in 16bits),
		// while still allowing degenerate cells. Note that if the planes were stored in cell space
		// degenerate cells would be very unlikely therefore greatly increasing the size of the terrain data.
		struct Leaf
		{
			pr::uint16		m_plane_rootx;		// The square root of the x component of the plane normal. LSB bit is the sign
			pr::uint16		m_plane_rootz;		// The square root of the z component of the plane normal. LSB bit is the sign
			pr::int16		m_plane_w;			// The distance component of the plane (region space).
			pr::uint8		m_mat_and_flags;	// Material and surface flags for the plane
			pr::uint8		pad;

			// Return a plane describing the terrain surface represented by this leaf
			pr::v4 Plane() const
			{
				float rootx = m_plane_rootx / 65536.0f;
				float rootz = m_plane_rootz / 65536.0f;
				float x = rootx * rootx * ((m_plane_rootx & 1) * 2.0f - 1.0f);
				float z = rootz * rootz * ((m_plane_rootz & 1) * 2.0f - 1.0f);
				float y_sq = 1.0f - x*x - z*z;				// Note: Y is always positive. 
				float y = pr::Sqrt(y_sq * (y_sq > 0.0f));	// Prevent the sqrt of a negative occurring due to floating point issues
				float w = m_plane_w * PlaneWScale();
				return pr::v4::make(x, y, z, w);
			}

			// Set the plane for this face
			void SetPlane(pr::v4 const& plane)
			{
				using namespace pr;
				m_plane_w				= static_cast<int16> (Floor(0.5f + plane.w / PlaneWScale()));
				m_plane_rootx			= static_cast<uint16>(Clamp(Sqrt(Abs(plane[0])) * 65536.0f, 0.0f, 65535.0f));
				m_plane_rootz			= static_cast<uint16>(Clamp(Sqrt(Abs(plane[2])) * 65536.0f, 0.0f, 65535.0f));
				if (plane[0] >= 0.0f) { m_plane_rootx |= 1; } else { m_plane_rootx &= ~1; }
				if (plane[2] >= 0.0f) { m_plane_rootz |= 1; } else { m_plane_rootz &= ~1; }
				PR_ASSERT(PR_DBG, Length3(plane - Plane()) < 0.1f, ""); // Check the compressed version of the plane approximately equals 'plane'
			}

			// Material properties of the terrain surface
			enum { MaterialIdBits = 6, MaterialIdMask = ((1 << MaterialIdBits) - 1) };
			pr::uint MaterialId() const				{ return m_mat_and_flags & MaterialIdMask; } 
			void SetMaterialId(pr::uint id)			{ m_mat_and_flags = static_cast<pr::uint8>((m_mat_and_flags & ~MaterialIdMask) | (id & MaterialIdMask)); PR_ASSERT(PR_DBG, MaterialId() == id, ""); }
			
			// Surface flags of the terrain surface
			enum { SurfaceFlagsBits = 8 - MaterialIdBits, SurfaceFlagsMask = ((1 << SurfaceFlagsBits) - 1) };
			pr::uint SurfaceFlags() const			{ return (m_mat_and_flags >> MaterialIdBits) & SurfaceFlagsMask; }
			void SetSurfaceFlags(pr::uint flags)	{ m_mat_and_flags = static_cast<pr::uint8>((m_mat_and_flags &  MaterialIdMask) | ((flags & SurfaceFlagsMask) << MaterialIdBits)); PR_ASSERT(PR_DBG, SurfaceFlags() == flags, ""); }
		};
		
		// Divisions in a BSP tree
		struct Branch
		{
			BranchIndex	m_left;				// Relative index to the left  child BSP tree. Negative means the child is a leaf
			BranchIndex	m_right;			// Relative index to the right child BSP tree. Negative means the child is a leaf
			BranchUnit	m_a;				// The 'a' constant in a 2D line equation
			BranchUnit	m_b;				// The 'b' constant in a 2D line equation
			BranchUnit	m_c;				// The 'c' constant in a 2D line equation
		};
		
		// Terrain cell containing terrain data for a 2D area
		// These objects should have sizes that are multiples of ELimit_UnitSize for indexing.
		struct Cell
		{
			float				m_region_originX;	// The world space position of the region containing this cell and the
			float				m_region_originZ;	//  size of the cell. These are stored in each cell to allow terrain
			float				m_sizeX;			//  lookups using world space coordinates to be done when only the
			float				m_sizeZ;			//  terrain cell is available.
			pr::uint8			m_num_units;		// The number of units the terrian cell occupies
			pr::uint8			m_num_trees;		// The number of bsp trees (terrain layers) in this cell
			pr::uint8			pad[2];
			
			pr::uint			SizeInBytes() const				{ return m_num_units * ELimit_UnitSize; }
			pr::uint			TreeCount() const				{ return m_num_trees; }
			pr::uint8 const*	TreeOffsetTable() const			{ return reinterpret_cast<pr::uint8 const*>(this + 1); }
			Branch const*		Tree(pr::uint index) const		{ PR_ASSERT(PR_DBG, index < TreeCount(), ""); return reinterpret_cast<Branch const*>(this) + TreeOffsetTable()[index]; }
			float				RegionX(float world_x) const	{ return world_x - m_region_originX; }
			float				RegionZ(float world_z) const	{ return world_z - m_region_originZ; }
			float				CellX(float world_x) const		{ return pr::Fmod(RegionX(world_x), m_sizeX) / m_sizeX; }
			float				CellZ(float world_z) const		{ return pr::Fmod(RegionZ(world_z), m_sizeZ) / m_sizeZ; }
			// Other notes:
			// Where ever possible the leaves within a bsp tree are shared. This means all branches
			// for each tree must occur before any of the leaves of that tree since the sign of the
			// branch index is used to indicate whether the child is a leaf or branch.
		};
		//pr::uint8	m_tree_offset_table[m_num_trees];	// Offsets (in multiples of sizeof(Branch)) to the start of each bsp tree
		//pr::uint8	m_data[...];						// Branch and leaf data

		// Extra data used to locate the cell for a given world space position.
		struct CellInfo
		{
			// bit 15 = split cell flag
			// if split
			//		bits 0-14 = relative index to another CellInfo object which is the first of 
			//				  a pair representing the cells that this cell was split into
			// else
			//		bits 0-14 = index of the cell that this cell info represents (in multiples of ELimit_UnitSize).
			// Note: empty terrain cells have 'm_info == 0' and do not have any Cell data
			pr::uint16 m_info;

			enum Bits
			{
				SplitMask = 0x8000,
				IndexMask = 0x7FFF,
				EmptyCell = 0x7FFF,
				MaxCellIndex = IndexMask
			};

			bool IsEmptyCell() const					{ return m_info == (pr::uint16)EmptyCell; }
			bool IsSplit() const						{ return (m_info & SplitMask) != 0; }
			void SetEmptyCell()							{ m_info = EmptyCell; }

			// Cell access
			pr::uint CellIndex() const					{ PR_ASSERT(PR_DBG, !IsSplit(), ""); return m_info & IndexMask; }
			void SetCellIndex(unsigned int index)		{ m_info = static_cast<pr::uint16>(index & IndexMask); PR_ASSERT(PR_DBG, CellIndex() == index, ""); }

			// Split cells
			CellInfo const*	SplitL() const				{ PR_ASSERT(PR_DBG, IsSplit(), ""); return this + (m_info & IndexMask)    ; }
			CellInfo const*	SplitR() const				{ PR_ASSERT(PR_DBG, IsSplit(), ""); return this + (m_info & IndexMask) + 1; }
			void SetSplit(unsigned int relative_index)	{ PR_ASSERT(PR_DBG, relative_index == (relative_index & IndexMask), ""); m_info = static_cast<pr::uint16>(SplitMask | (relative_index & IndexMask)); }
		};
		
		// Header for a region of terrain data
		struct Header
		{
			int		m_data_size;			// The total size in bytes of this struct and it's following data
			int		m_version;				// The version of this terrain data
			int		m_num_cell_infos;		// The number of CellInfo objects following this header
			int		m_num_cells;			// The number of Cell objects following the CellInfo data
			float	m_originX;				// The X co-ordinate of the region within the world (world co-ords)
			float	m_originY;				// The Y co-ordinate of the region within the world (world co-ords)
			float	m_originZ;				// The Z co-ordinate of the region within the world (world co-ords)
			int		m_divisionsX;			// The number of divisions in the X axis direction 
			int		m_divisionsZ;			// The number of divisions in the Z axis direction 
			float	m_cell_sizeX;			// The X dimension of a cell
			float	m_cell_sizeZ;			// The Z dimension of a cell

			static Header const& make(void const* data)		{ Header const& hdr = *reinterpret_cast<Header const*>(data); PR_ASSERT(PR_DBG, hdr.m_version == pr::terrain::Version, ""); return hdr; }
			CellInfo const& cell_info(pr::uint index) const	{ return reinterpret_cast<CellInfo const*>(this + 1)[index]; }
			Cell     const& cell(pr::uint index) const		{ return reinterpret_cast<Cell const&>(reinterpret_cast<pr::uint8 const*>(&cell_info(m_num_cell_infos))[index * ELimit_UnitSize]); }
			pr::v4	origin() const							{ return pr::v4::make(m_originX, m_originY, m_originZ, 1.0f); }
			pr::v4	centre() const							{ return pr::v4::make(m_originX + 0.5f * m_divisionsX * m_cell_sizeX, 0.0f, m_originZ + 0.5f * m_divisionsZ * m_cell_sizeZ, 1.0f); } 
		};
		// CellInfo m_cell_info[m_num_cell_infos];
		// Cell		m_cell[m_num_cells];

		// Branches contain indices that are in units of ELimit_BIndexUnit.
		// These indices are used to find other branches or leaves so they must be the same size.
		static_assert(sizeof(Leaf)   == ELimit_BIndexUnit,"");
		static_assert(sizeof(Branch) == ELimit_BIndexUnit,"");

		// Interface *******************************************************************

		// SelectHeightFunctor:
		//	This type should have the following function signiture:
		//	void SelectHeightFunctor(float height, pr::v4 const& plane, int material_id, int surface_flags);
		// Note: 'plane' is in world space.

		// Bounding rectangle check to test whether a query point is with a terrain region
		inline bool PointIsWithin(Header const& terrain, float x, float z, float tolerance = 0.0f)
		{
			x -= terrain.m_originX;
			z -= terrain.m_originZ;
			return !(x < -tolerance || x >= terrain.m_cell_sizeX * terrain.m_divisionsX + tolerance ||
					 z < -tolerance || z >= terrain.m_cell_sizeZ * terrain.m_divisionsZ + tolerance);
		}

		// Bounding rectangle check to test whether a query point is with a terrain cell
		inline bool PointIsWithin(Cell const* cell, float x, float z, float tolerance = 0.0f)
		{
			if (!cell) return false;
			x -= cell->m_region_originX;
			z -= cell->m_region_originZ;
			return !(x < -tolerance || x >= cell->m_sizeX + tolerance || z < -tolerance || z >= cell->m_sizeX + tolerance);
		}

		// Evaluate the line equation in branch at the 2D point x,z
		inline float Compare(Branch const& branch, float x, float z)
		{
			return branch.m_a * x + branch.m_b * z + branch.m_c;
		}

		// Return the height of the terrain surface at coordinates x,z
		// Found by solving for y in P.X = 0 where P = plane, and X = position
		// Note, P and X must be in the same space (i.e. both in world space)
		inline float HeightAt(pr::v4 const& plane, float x, float z)
		{
			return -(plane.w + plane.x*x + plane.z*z) / plane.y;
		}

		// Return the index position of the cell within 'terrain' for world coordinates 'x,z'
		// Returns true if the cell coordinates are a valid location within 'terrain'
		inline bool CellIndex(Header const& terrain, float x, float z, int& cellX, int& cellZ)
		{
			cellX = static_cast<int>((x - terrain.m_originX) / terrain.m_cell_sizeX);
			cellZ = static_cast<int>((z - terrain.m_originZ) / terrain.m_cell_sizeZ);
			return cellX >= 0 && cellZ >= 0 && cellX < terrain.m_divisionsX && cellZ < terrain.m_divisionsZ;
		}

		// Returns a pointer to the cell containing the terrain data at world space position 'x,z'
		// Returns null if 'x,z' do not fall within the bounds of the region, or if there is no terrain data at 'x,z'
		inline Cell const* FindCell(Header const& terrain, float x, float z)
		{
			PR_ASSERT(PR_DBG, pr::IsFinite(x) && pr::IsFinite(z), "Invalid position used to query terrain");

			float region_x_scaled = (x - terrain.m_originX) / terrain.m_cell_sizeX;
			float region_z_scaled = (z - terrain.m_originZ) / terrain.m_cell_sizeZ;

			// Find the cell index position
			int cellX = static_cast<int>(region_x_scaled);
			int cellZ = static_cast<int>(region_z_scaled);
			
			// Test for a valid cell index using the positive sense boolean test so
			// that invalid floats (NaN etc) don't pass this test and cause a crash.
			// NaNs etc should be caught in the assert above.
			bool valid_cell_index = cellX >= 0 && cellZ >= 0 && cellX < terrain.m_divisionsX && cellZ < terrain.m_divisionsZ;
			if (!valid_cell_index)
				return 0;

			// Find the normalised cell relative position
			float cell_x = region_x_scaled - pr::Trunc(region_x_scaled);
			float cell_z = region_z_scaled - pr::Trunc(region_z_scaled);

			// Get the cell info and from that, the terrain cell. If the cell is split, locate the actual cell recursively
			int cell_index = cellZ * terrain.m_divisionsX + cellX;
			CellInfo const* cell_info = &terrain.cell_info(cell_index);
			bool split_vertically = true; // Split cells are always split vertically first
			while (cell_info->IsSplit())
			{
				if (split_vertically)
				{
					if (cell_x < 0.5f)	{ cell_info = cell_info->SplitL(); cell_x *= 2.0f; }
					else				{ cell_info = cell_info->SplitR(); cell_x = (cell_x - 0.5f) * 2.0f; }
				}
				else
				{
					if (cell_z < 0.5f)	{ cell_info = cell_info->SplitL(); cell_z *= 2.0f; }
					else				{ cell_info = cell_info->SplitR(); cell_z = (cell_z - 0.5f) * 2.0f; }
				}
				split_vertically = !split_vertically;
			}
			
			// If the cell info points to an empty cell return no cell
			if (cell_info->IsEmptyCell()) return 0;
			return &terrain.cell(cell_info->CellIndex());
		}
		
		// Terrain query function using a single terrain cell.
		// This function samples the terrain within a terrain cell at 2D world space position 'x,z'
		// calling 'select_height' for each terrain layer intersected. It is intended for optimised
		// terrain querying (i.e. on SPU). Implemented as a template so that 'SelectHeightFunctor' can be inlined
		template <typename SelectHeightFunctor> void Query(Cell const* terrain_cell, float x, float z, SelectHeightFunctor& select_height)
		{
			// Gracefully handle null terrain cells, this is to allow clients
			// to pass the result of 'FindCell' straight into this function
			// even if FindCell returns null.
			if (!terrain_cell)
			{
				select_height(pr::terrain::DefaultHeight(), Plane::make(0.0f, 1.0f, 0.0f, -pr::terrain::DefaultHeight()), 0, 0);
				return;
			}
		
			// Do some sanity checks on the provided terrain cell to check that a valid pointer has been passed
			PR_ASSERT(PR_DBG, terrain_cell->TreeCount() > 0 && terrain_cell->TreeCount() <= ELimit_MaxLayers, "Invalid terrain cell encountered. The provided pointer is probably corrupt");

			// Get the normalised cell relative coordinates of x,z
			float cell_x = terrain_cell->CellX(x);
			float cell_z = terrain_cell->CellZ(z);

			// Search each bsp tree within the cell returning the terrain layers intersected
			for (pr::uint i = 0, i_end = terrain_cell->TreeCount(); i != i_end; ++i)
			{
				Branch const* tree = terrain_cell->Tree(i);
				Leaf const* leaf = 0;
			
				// Search down the tree until we find a leaf
				BranchIndex index = 0;
				while (leaf == 0)
				{
					index = Compare(*tree, cell_x, cell_z) > 0.0f ? tree->m_left : tree->m_right;
					PR_ASSERT(PR_DBG, index != 0, "Invalid branch offset found. Terrain data is invalid"); // This implies bad terrain data and will cause an infinite loop
					if (index > 0) // This is a branch index
						tree += index;
					else // Otherwise, this is a leaf index
						leaf = reinterpret_cast<Leaf const*>(tree - index);
				}
				
				// Return the plane (in world space) and height
				pr::v4 plane = leaf->Plane();
				plane.w -= (plane.x*terrain_cell->m_region_originX + plane.z*terrain_cell->m_region_originZ); //i.e. plane.w += Dot(plane, worldorigin_to_regionorigon);
				float height = HeightAt(plane, x, z);
				select_height(height, plane, leaf->MaterialId(), leaf->SurfaceFlags());
			}
		}
		
		// General purpose terrain query function
		// This function samples the terrain at a 2D world space point calling 'select_height'
		// for each terrain layer intersected. Implemented as a template so that 'SelectHeightFunctor' can be inlined
		template <typename SelectHeightFunctor> void Query(Header const& terrain, float x, float z, SelectHeightFunctor& select_height)
		{
			Query(FindCell(terrain, x, z), x, z, select_height);
		}

	}//namespace terrain
}//namespace pr

#endif//PR_TERRAIN_H
