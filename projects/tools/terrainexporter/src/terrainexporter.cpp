//*******************************************************************************
// Terrain Exporter
//  Copyright (c) Rylogic Ltd 2009
//*******************************************************************************
#include "stdafx.h"
#include "pr/terrain/terrainexporter.h"
#include "pr/filesys/fileex.h"
#include "terrainexporter/utility.h"
#include "terrainexporter/vertex.h"
#include "terrainexporter/face.h"
#include "terrainexporter/edge.h"
#include "terrainexporter/leafex.h"
#include "terrainexporter/branchex.h"
#include "terrainexporter/treeex.h"
#include "terrainexporter/cellex.h"

using namespace pr;
using namespace pr::terrain;

namespace
{
	typedef std::vector<unsigned char> ByteCont;
	const float PositionWeldRadius = 1.0f / PositionQuantisation;
}

// Predicate for sorting vertex pointers
struct VertexPtrSort
{
	TVertVec const* m_verts;
	VertexPtrSort(TVertVec const& verts) :m_verts(&verts)	{}
	bool operator()(pr::terrain::Vertex const* lhs, pr::uint rhs) const		{ return *lhs < (*m_verts)[rhs]; }
	bool operator()(pr::uint lhs, pr::terrain::Vertex const* rhs) const		{ return (*m_verts)[lhs] < *rhs; }
	bool operator()(pr::uint lhs, pr::uint rhs) const						{ return (*m_verts)[lhs] < (*m_verts)[rhs]; }
};

// Add a vertex 'vert' to the container 'verts' if it is unique.
// Returns the index of the added vert. Adding a vert does not
// invalidate indices returned for previously added verts.
uint AddVert(TVertDict& vert_dict, TVertVec& verts, v4 const& vert)
{
	// Quantise the vertex position
	terrain::Vertex v = { Quantise(vert, PositionQuantisation) };

	// Look in the vert dictionary for a vert equal to 'vert' and, if found, return its index
	TVertDict::iterator iter = std::lower_bound(vert_dict.begin(), vert_dict.end(), &v, VertexPtrSort(verts));
	if (iter != vert_dict.end() && verts[*iter] == v) return *iter;

	// Otherwise, add 'vert' to the end of the 'verts' container
	// and insert a index to it into the dictionary.
	uint index = static_cast<uint>(verts.size());
	verts.push_back(v);
	vert_dict.insert(iter, index);
	return index;
}

// Assign pointers to verts for the faces in 'faces'
void SetVertexPointers(TFaceVec& faces, TVertVec const& verts)
{
	for (TFaceVec::iterator f = faces.begin(), f_end = faces.end(); f != f_end; ++f)
	{
		Face& face = *f;
		face.m_vertices[0] = &verts[face.m_index[0]];
		face.m_vertices[1] = &verts[face.m_index[1]];
		face.m_vertices[2] = &verts[face.m_index[2]];
	}
}

// Generate the edges of the terrain from the 'faces'
// This function attempts to find the common edges between faces.
// However, since we are dealing with a "face-soup" and quantised vert positions
// it's possible that a single edge is common to three or more faces.
void CreateEdges(TFaceVec& faces, TEdgeSet& edges)
{
	uint edge_number = 0;

	for (TFaceVec::iterator f = faces.begin(), f_end = faces.end(); f != f_end; ++f)
	{
		Face& face = *f;
		for (uint i = 0; i != 3; ++i)
		{
			Edge edge;
			edge.m_index0		= face.m_index[(i + 0) % 3];
			edge.m_index1		= face.m_index[(i + 1) % 3];
			edge.m_Lface		= &face;
			edge.m_Rface		= 0;
			edge.m_vertex0		= face.m_vertices[(i + 0) % 3];
			edge.m_vertex1		= face.m_vertices[(i + 1) % 3];
			edge.m_contributes	= true;
			edge.m_edge_number	= edge_number++;

			// Look for an existing edge
			TEdgeSet::iterator edge_iter = edges.find(edge);
			
			// See if we can merge 'edge' with one in the set
			// Iterate over all edges that are equivalent to 'edge'
			for (; edge_iter != edges.end() && *edge_iter == edge; ++edge_iter)
			{
				Edge& existing = *edge_iter;
			
				// We can merge the edges if the index order is opposite and there is currently no righthand face.
				// Note, it's possible that this will join faces originally from different meshes. This is what we
				// want because it will reduce the need for multiple trees within the cells.
				if (existing.m_index0 == edge.m_index1 && existing.m_index1 == edge.m_index0 && existing.m_Rface == 0)
				{
					// Merge the edges
					existing.m_Rface = &face;
					face.m_edges[i] = &existing;
					break;
				}
			}

			// If the edge is not in the set or we couldn't merge it with one
			// already in the set, then add 'edge' as a new edge to the multi set
			if (edge_iter == edges.end() || !(*edge_iter == edge))
			{
				edge_iter = edges.insert(edge_iter, edge);
				face.m_edges[i] = &*edge_iter;
			}
		}
	}
}

// This function creates the grid of cells for the region,
// then adds a face reference to the cells that overlap each face.
EResult SortIntoCells(FRect const& region_bounds, int divisionsX, int divisionsZ, TCellExList& cells, TFaceVec& faces)
{
	typedef std::vector<CellEx*> TCellExPtrVec;
	
	float cell_sizeX = region_bounds.SizeX() / float(divisionsX);
	float cell_sizeZ = region_bounds.SizeY() / float(divisionsZ);
	float cell_scaleX = 1.0f / cell_sizeX;
	float cell_scaleZ = 1.0f / cell_sizeZ;

	// Set up the cells
	uint cell_index = 0;
	cells.resize(divisionsX * divisionsZ);
	TCellExPtrVec cell_ptr(divisionsX * divisionsZ); // This is needed for array access to the cells, m_cell is a list.
	TCellExList  ::iterator  c = cells.begin();
	TCellExPtrVec::iterator pc = cell_ptr.begin();
	for (int z = 0; z != divisionsZ; ++z)
	{
		for (int x = 0; x != divisionsX; ++x)
		{
			*pc++				= &(c->get());
			CellEx& cell		= *c++;
			cell.m_scale_X		= cell_scaleX;
			cell.m_scale_Z		= cell_scaleZ;
			cell.m_cell_index	= cell_index++;
			cell.m_bounds.set(	(x    ) * cell_sizeX,
								(z    ) * cell_sizeZ,
								(x + 1) * cell_sizeX,
								(z + 1) * cell_sizeZ);
		}
	}

	for (TFaceVec::iterator f = faces.begin(), f_end = faces.end(); f != f_end; ++f)
	{
		Face& face = *f;
		
		// Loop over the cells that the bounding box of this face
		// covers and add a pointer to 'face' in each cell
		int startX = int(face.m_bounds.m_min.x / cell_sizeX);
		int startZ = int(face.m_bounds.m_min.y / cell_sizeZ);
		int endX   = int(face.m_bounds.m_max.x / cell_sizeX) + 1;
		int endZ   = int(face.m_bounds.m_max.y / cell_sizeZ) + 1;

		// Stay within the region
		if (endX < 0 || startX > divisionsX) continue;
		if (endZ < 0 || startZ > divisionsZ) continue;
		if (startX < 0)				startX	= 0;
		if (endX   > divisionsX)	endX	= divisionsX;
		if (startZ < 0)				startZ	= 0;
		if (endZ   > divisionsZ)	endZ	= divisionsZ;
		for (int z = startZ; z != endZ; ++z)
		{
			for (int x = startX; x != endX; ++x)
			{
				CellEx& cell = *cell_ptr[z * divisionsX + x];
	
				// Add the face to the cell if it actually overlaps the cell
				if (IsIntersection(cell.m_bounds, face)) // Face vs. cell check
				{
					cell.AddFace(&face);
				}
			}
		}
	}
	return EResult_Success;
}

// This function is called when a BSP tree in a cell has grown too large. We reset
// all of the trees in the cell, create two new cells, sort the mesh faces between
// the two cells and then add the new cells to the tail of the cell list.
EResult SplitCell(CellEx& cell, TCellExList& cells)
{
	// Limit the number of times we split this cell
	if (cell.m_split_count == MaxCellSubDivision) return EResult_CellSplitTooOften;
	
	// Create the new regions for the split cell
	FRect box1, box2;
	if ((cell.m_split_count % 2) == 0)		// Split vertically this time
	{
		// Define the bounds for the two new cells and set the scaling factors
		box1.set(cell.m_bounds.m_min.x, cell.m_bounds.m_min.y, (cell.m_bounds.m_min.x + cell.m_bounds.m_max.x) / 2.0f, cell.m_bounds.m_max.y);
		box2.set((cell.m_bounds.m_min.x + cell.m_bounds.m_max.x) / 2.0f, cell.m_bounds.m_min.y, cell.m_bounds.m_max.x, cell.m_bounds.m_max.y);
	}
	else									// Split horizontally this time
	{
		// Define the boundary for the two new cells and set the scaling factors
		box1.set(cell.m_bounds.m_min.x, cell.m_bounds.m_min.y, cell.m_bounds.m_max.x, (cell.m_bounds.m_min.y + cell.m_bounds.m_max.y) / 2.0f);
		box2.set(cell.m_bounds.m_min.x, (cell.m_bounds.m_min.y + cell.m_bounds.m_max.y) / 2.0f, cell.m_bounds.m_max.x, cell.m_bounds.m_max.y);
	}

	// Set the indices of 'cell' so that they point to the left and right/top and bottom half cells
	uint cell1_index = static_cast<uint>(cells.size());
	uint cell2_index = static_cast<uint>(cells.size()) + 1;	

	// Create the new cells
	cells.push_back(CellEx());	CellEx& cell1 = cells.back();
	cells.push_back(CellEx());	CellEx& cell2 = cells.back();
	cell1.m_bounds		= box1;
	cell1.m_cell_index	= cell1_index;
	cell1.m_scale_X		= 1.0f / box1.SizeX();
	cell1.m_scale_Z		= 1.0f / box1.SizeY();
	cell1.m_split_count	= cell.m_split_count + 1;
	cell2.m_bounds		= box2;
	cell2.m_cell_index	= cell2_index;
	cell2.m_scale_X		= 1.0f / box2.SizeX();
	cell2.m_scale_Z		= 1.0f / box2.SizeY();
	cell2.m_split_count	= cell.m_split_count + 1;

	// Re-sort the faces of 'cell' between 'cell1' and 'cell2'
	for (TTreeExList::const_iterator t = cell.m_tree.begin(), t_end = cell.m_tree.end(); t != t_end; ++t)
	{
		for (TFacePtrSet::const_iterator f = t->m_faces.begin(), f_end = t->m_faces.end(); f != f_end; ++f)
		{
			if (IsIntersection(cell1.m_bounds, **f)) { cell1.AddFace(*f); }
			if (IsIntersection(cell2.m_bounds, **f)) { cell2.AddFace(*f); }
		}
	}
	
	// Empty out 'cell' and record the index of the first child
	cell.Clear();
	cell.m_child_index = cell1_index;
	return EResult_Success;
}

// Create terrain data from the faces in each cell
EResult BuildBSPTrees(TCellExList& cells)
{
	// Terrain with too many split cells will be large and slow, set some limit
	// based on how many cells there are for the region.
	int split_cell_count = 0;
	int max_total_cell_splits = int(cells.size() * 2);

	// Build BSP trees in each cell.
	// Note, the length of 'cells' changes in this loop as cells are split
	for (TCellExList::iterator c = cells.begin(); c != cells.end(); ++c)
	{
		CellEx& cell = *c;

		PR_EXPAND(TERRAIN_ONECELL, if (cell.m_cell_index != TARGET_CELL) cell.m_holding_pen.clear());

		EResult res = cell.BuildBSPTrees();
		if (res == EResult_CellNeedsSplitting)
		{
			// Limit the total number of cell splits
			if (split_cell_count++ == max_total_cell_splits)
			{
				return EResult_TooManySplitCells;
			}

			// A BSP tree is too big when it has too many branches or leaves in the tree.
			// We split the cell into two sub cells and add them to the end of the CellEx
			// array so that they will be processed later.
			res = SplitCell(cell, cells);
		}
		if (Failed(res))
		{
			return res; // A maximum has been exceeded.
		}
	}
	return EResult_Success;
}

// Looks for cells with degenerate sets of trees. For any that
// are found the trees are removed and the m_degenerate_cell pointer set.
EResult RemoveDegenerateCells(TCellExList& cells)
{
	for (TCellExList::iterator c1 = cells.begin(), c_end = cells.end(); c1 != c_end; ++c1)
	{
		CellEx& cell1 = *c1;
		if (cell1.m_degenerate_cell) continue;	// This cell has already been identified as degenerate
		if (cell1.CellType() == ECellType_Split) continue;	// Ignore split cells, they're unlikely to be degenerate

		TCellExList::iterator c2 = c1;
		for (++c2; c2 != c_end; ++c2)
		{
			CellEx& cell2 = *c2;
			if (cell2.m_degenerate_cell) continue; // This cell has already been identified as degenerate
			if (cell2.CellType() == ECellType_Split) continue; // Ignore split cells, they're unlikely to be degenerate

			// The cells must have the same number of trees
			if (cell1.m_tree.size() != cell2.m_tree.size()) continue;
			
			// Each tree must be degenerate
			bool all_degenerate = true;
			TTreeExList::iterator t1 = cell1.m_tree.begin();
			TTreeExList::iterator t2 = cell2.m_tree.begin();
			std::size_t num_trees = cell1.m_tree.size();
			for (std::size_t  t = 0; t != num_trees; ++t, ++t1, ++t2)
			{
				if (!IsDegenerate(*t1, *t2))
				{
					all_degenerate = false;
					break;
				}
			}

			// If all of 'cell2's trees are degenerate then we don't need it's trees, branches, and leaves
			if (all_degenerate)
			{
				cell2.Clear();
				
				// Make 'cell2' point to 'cell1'
				cell2.m_degenerate_cell = &cell1;
			}
		}
	}
	return EResult_Success;
}

// Creates the game-side terrain data for a single cell.
void PrepareCell(CellEx const& cellex, float region_originX, float region_originZ, ByteCont& buf)
{
	PR_ASSERT(PR_DBG_TERRAIN, cellex.m_degenerate_cell == 0, "Degenerate cells should not be passed to this function");
	PR_ASSERT(PR_DBG_TERRAIN, cellex.m_tree.size() <= ELimit_MaxLayers, "This cell has too many bsp trees (a.k.a terrain layers)");
	PR_ASSERT(PR_DBG_TERRAIN, (cellex.RequiredSizeInBytes() % ELimit_UnitSize) == 0, "Cell sizes must be multiples of ELimit_UnitSize");

	// Size the buffer to fit the cell data
	buf.resize(cellex.RequiredSizeInBytes());
	memset(&buf[0], 0, buf.size());

	// Fill out the cell data
	Cell& cell = *reinterpret_cast<Cell*>(&buf[0]);
	cell.m_region_originX	= region_originX;
	cell.m_region_originZ	= region_originZ;
	cell.m_sizeX			= cellex.m_bounds.SizeX();
	cell.m_sizeZ			= cellex.m_bounds.SizeY();
	cell.m_num_units		= value_cast<uint8>(buf.size() / ELimit_UnitSize);
	cell.m_num_trees		= value_cast<uint8>(cellex.m_tree.size());

	// Get a pointer to the start of the tree offset table
	uint8* tree_table = const_cast<uint8*>(cell.TreeOffsetTable());

	// Add each tree
	int tree_index = 0;
	uint8* base = &buf[0];
	uint8* ptr = base + cellex.CellHeaderSizeInBytes();
	for (TTreeExList::const_iterator t = cellex.m_tree.begin(), t_end = cellex.m_tree.end(); t != t_end; ++t, ++tree_index)
	{
		TreeEx const& tree = *t;

		// Add the offset to the start of this tree
		PR_ASSERT(PR_DBG_TERRAIN, ((ptr - base) % ELimit_BIndexUnit) == 0, "Cell data has become un-aligned");
		PR_ASSERT(PR_DBG_TERRAIN, ((ptr - base) / ELimit_BIndexUnit) <= 0xff, "Tree offset value overflow"); // The max cell size should prevent this ever happening
		tree_table[tree_index] = value_cast<uint8>((ptr - base) / ELimit_BIndexUnit);

		// Add the branches of the tree
		Branch*& branch = reinterpret_cast<Branch*&>(ptr); // incrementing 'branch' will increment 'ptr'
		for (TBranchExList::const_iterator b = tree.m_branch.begin(), b_end = tree.m_branch.end(); b != b_end; ++b)
		{
			PR_ASSERT(PR_DBG_TERRAIN, b->m_Lbranch != 0 || b->m_Lleaf != 0, "All branches should point to either another branch or a leaf");
			PR_ASSERT(PR_DBG_TERRAIN, b->m_Rbranch != 0 || b->m_Rleaf != 0, "All branches should point to either another branch or a leaf");

			BranchEx const& bra = b->get();
			*branch = bra.m_branch;
			if (bra.m_Lbranch != 0) branch->m_left  =  value_cast<BranchIndex>(bra.m_Lbranch->m_index - bra.m_index);
			else                    branch->m_left  = -value_cast<BranchIndex>(tree.m_branch.size() - bra.m_index + bra.m_Lleaf->m_index);
			if (bra.m_Rbranch != 0) branch->m_right =  value_cast<BranchIndex>(bra.m_Rbranch->m_index - bra.m_index);
			else                    branch->m_right = -value_cast<BranchIndex>(tree.m_branch.size() - bra.m_index + bra.m_Rleaf->m_index);

			PR_ASSERT(PR_DBG_TERRAIN, branch->m_left != 0 && branch->m_right != 0, "Branches can't have zero relative offsets");
			++branch;
		}

		// Add the leaves of the tree
		Leaf*& leaf = reinterpret_cast<Leaf*&>(ptr); // incrementing 'leaf' will increment 'ptr'
		for (TLeafExList::const_iterator l = tree.m_leaf.begin(), l_end = tree.m_leaf.end(); l != l_end; ++l)
		{
			*leaf = l->m_leaf;
			++leaf;
		}
	}
}

// Creates the game-side terrain data in 'data'.
EResult PrepareData(FRect const& region_bounds, int divisionsX, int divisionsZ, TCellExList& cells, ByteCont& data)
{
	// Create buffers for the cell infos and cells.
	std::vector<CellInfo>	buf_cell_info(cells.size());
	ByteCont				buf_cell; buf_cell.reserve(cells.size()*2*ELimit_UnitSize); // Assume about 2 Cells per CellEx roughly
	ByteCont				working_buffer;

	unsigned int cell_count = 0;
	for (TCellExList::const_iterator c = cells.begin(), c_end = cells.end(); c != c_end; ++c)
	{
		CellEx const& cell = *c;
		CellInfo& cell_info = buf_cell_info[cell.m_cell_index];

		switch (cell.CellType())
		{
		default: PR_ASSERT(PR_DBG_TERRAIN, false, ""); break;
		case ECellType_Empty:
			cell_info.SetEmptyCell();
			break;
		case ECellType_Tree:
			if (cell.m_degenerate_cell)
			{
				PR_ASSERT(PR_DBG_TERRAIN, cell.m_degenerate_cell->m_cell_index < cell.m_cell_index, "Degenerate cells should always occur after the original cell");
				cell_info = buf_cell_info[cell.m_degenerate_cell->m_cell_index];
			}
			else
			{
				// Check that the number of cell units is still within the addressable range in a CellInfo
				if (cell_count >= CellInfo::MaxCellIndex)
					return EResult_TooManyCells;

				// Generate the cell data in the working buffer
				PrepareCell(cell, region_bounds.m_min.x, region_bounds.m_min.y, working_buffer);
				PR_ASSERT(PR_DBG_TERRAIN, (working_buffer.size() % ELimit_UnitSize) == 0, ""); // Cell data must be in multiples of the Cell size

				uint size = uint(working_buffer.size() / ELimit_UnitSize);
				cell_info.SetCellIndex(cell_count);
				cell_count += size;

				// Add the cell data to the cell buffer
				buf_cell.insert(buf_cell.end(), working_buffer.begin(), working_buffer.end());
			}
			break;
		case ECellType_Split:
			// If this is a split cell, then set the relative index to the cell info for the first child
			cell_info.SetSplit(cell.m_child_index - cell.m_cell_index);
			break;
		}
	}

	uint total_size_in_bytes = uint(sizeof(terrain::Header) + buf_cell_info.size()*sizeof(terrain::CellInfo) + buf_cell.size());
	data.resize(total_size_in_bytes);
	uint8* ptr = &data[0];

	// Create the terrain data header
	terrain::Header header;
	header.m_data_size			= total_size_in_bytes;
	header.m_version			= terrain::Version;
	header.m_num_cell_infos		= int(buf_cell_info.size());
	header.m_num_cells			= cell_count;
	header.m_originX			= region_bounds.m_min.x;
	header.m_originY			= 0;
	header.m_originZ			= region_bounds.m_min.y;
	header.m_divisionsX			= divisionsX;
	header.m_divisionsZ			= divisionsZ;
	header.m_cell_sizeX			= region_bounds.SizeX() / float(divisionsX);
	header.m_cell_sizeZ			= region_bounds.SizeY() / float(divisionsZ);
	
	// Write the data to the buffer
	memcpy(ptr, &header, sizeof(header));										ptr += sizeof(header);
	memcpy(ptr, &buf_cell_info[0], buf_cell_info.size()*sizeof(CellInfo));		ptr += buf_cell_info.size()*sizeof(CellInfo);
	if (!buf_cell.empty()) memcpy(ptr, &buf_cell[0], buf_cell.size());			ptr += buf_cell.size();
	PR_ASSERT(PR_DBG_TERRAIN, ptr == &data[0] + total_size_in_bytes, "");
	return EResult_Success;
}

// CTerrainExporter *****************************************************
TerrainExporter::TerrainExporter()
:m_region_origin(v4Zero)
,m_region_rect(FRectZero)
,m_divisionsX(0)
,m_divisionsZ(0)
,m_vert_dict()
,m_verts()
,m_faces()
,m_edges()
,m_face_id(0)
,m_cell()
{}

// Reset the terrain exporter in preparation for a new region of terrain data
EResult TerrainExporter::CreateRegion(v4 const& region_origin, float region_sizeX, float region_sizeZ, int divisionsX, int divisionsZ)
{
	PR_ASSERT(PR_DBG_TERRAIN, region_origin.w == 1.0f, "The region origin should be a position in world space");
	PR_ASSERT(PR_DBG_TERRAIN, divisionsX >= 1 && divisionsZ >= 1, "The region should be at least 1x1 cells");

	// Clear any old source data
	m_vert_dict.clear();
	m_verts.clear();
	m_faces.clear();
	m_edges.clear();
	m_face_id = 0;

	// Set the region parameters
	m_region_origin	= region_origin - v4Origin;
	m_region_rect	.set(0.0f, 0.0f, region_sizeX, region_sizeZ);
	m_divisionsX	= divisionsX;
	m_divisionsZ	= divisionsZ;

	return EResult_Success;
}

// Add a single face to the terrain data.
// 'Vert0', 'Vert1', 'Vert2' should be in world space.
EResult TerrainExporter::AddFace(v4 const& Vert0, v4 const& Vert1, v4 const& Vert2, uint material_id)
{
	// Check that the material id does not overflow the number of bits we have
	// available to store the material id in the data
	if ((material_id & terrain::Leaf::MaterialIdMask) != material_id )
		return EResult_MaterialIdOutOfRange;

	// Convert the verts into region space
	v4 v0 = Vert0 - m_region_origin;
	v4 v1 = Vert1 - m_region_origin;
	v4 v2 = Vert2 - m_region_origin;

	// Do a rough bounding box test for the region to see whether this face might be in the region
	FRect face_bounds = FRectReset;
	Encompase(face_bounds, Proj2d(v0));
	Encompase(face_bounds, Proj2d(v1));
	Encompase(face_bounds, Proj2d(v2));
	FRect region_bounds = Inflate(m_region_rect, PositionWeldRadius * 2.0f);
	if (!IsIntersection(region_bounds, face_bounds))
		return EResult_Success; // Should probably return a "success" error code here so the caller can tell

	// Add the vertices to the source verts and record the index positions
	uint i0 = AddVert(m_vert_dict, m_verts, v0);
	uint i1 = AddVert(m_vert_dict, m_verts, v1);
	uint i2 = AddVert(m_vert_dict, m_verts, v2);
	
	// Create a face
	terrain::Face face;

	// If the face is not valid for adding to the terrain, ignore it.
	face.m_vertices[0] = &m_verts[i0];	// Temporary point to the quantised verts. Eventually these will point
	face.m_vertices[1] = &m_verts[i1];	//  into the source 'm_vert' container, but we can't do that until we've finished 
	face.m_vertices[2] = &m_verts[i2];	//  adding faces because the source vert container is changing size.
	if (!IsValidFace(face))
		return EResult_Success; // Should probably return a "success" error code here so the caller can tell

	// Update the bounds with the quantised vert positions
	face_bounds = FRectReset;
	Encompase(face_bounds, Proj2d(face.m_vertices[0]->m_position));
	Encompase(face_bounds, Proj2d(face.m_vertices[1]->m_position));
	Encompase(face_bounds, Proj2d(face.m_vertices[2]->m_position));

	face.m_original_vertex[0]	= v0;
	face.m_original_vertex[1]	= v1;
	face.m_original_vertex[2]	= v2;
	face.m_vertices[0]			= 0;	// Invalidate the vert pointers in the face. We'll set
	face.m_vertices[1]			= 0;	//  these up again once we've finished adding faces
	face.m_vertices[2]			= 0;
	face.m_bounds				= face_bounds;
	face.m_index[0]				= i0;
	face.m_index[1]				= i1;
	face.m_index[2]				= i2;
	face.m_material_index		= material_id;
	face.m_surface_flags		= 0;	// Not using surface flags currently
	face.m_edges[0]				= 0;	// Not created yet
	face.m_edges[1]				= 0;
	face.m_edges[2]				= 0;
	face.m_plane				= 0;
	face.m_face_id				= m_face_id++;
	face.m_tree_id				= uint(-1);

	// Add the face
	m_faces.push_back(face);
	return EResult_Success;
}

// When all data has been added, these functions are used to generate the terrain height data.
//	'thd_data' - Is a byte buffer that will contain the terrain height data
//  'thd_filename' - The terrain height data will be written to a file with filename 'thd_filename'.
//	'thd_chunk' - The terrain height data will be written to the provided generic chunk.
EResult TerrainExporter::CloseRegion(ByteCont& thd_data)
{
	EResult result;

	// Assign pointers to verts in the faces now that all source data has been added
	SetVertexPointers(m_faces, m_verts);
	m_vert_dict.clear(); // Shouldn't need the vert dictionary anymore. Might as well free up some memory

	// Generate the edges of the terrain data
	CreateEdges(m_faces, m_edges);
	
	// Sort the mesh faces into the cells
	FRect region_bounds = {m_region_origin.x, m_region_origin.z, m_region_origin.x + m_region_rect.SizeX(), m_region_origin.z + m_region_rect.SizeY()};
	result = SortIntoCells(region_bounds, m_divisionsX, m_divisionsZ, m_cell, m_faces);
	if (Failed(result)) return result;

	// Build the bsp trees within the cells
	result = BuildBSPTrees(m_cell);
	if (Failed(result)) return result;

	// Look for identical cells and remove the degenerate ones
	result = RemoveDegenerateCells(m_cell);
	if (Failed(result)) return result;
	
	// Prepare the data for writing into file
	result = PrepareData(region_bounds, m_divisionsX, m_divisionsZ, m_cell, thd_data);
	if (Failed(result)) return result;

	return EResult_Success;
}
EResult TerrainExporter::CloseRegion(std::string const& thd_filename)
{
	// Generate the terrain height data
	ByteCont data;
	EResult result = CloseRegion(data);
	if (Failed(result)) return result;

	// Write a file containing the generated data
	pr::Handle file = pr::FileOpen(thd_filename.c_str(), pr::EFileOpen::Writing);
	if (file == INVALID_HANDLE_VALUE) return EResult_FailedToOpenTHDFile;
	
	DWORD bytes_written;
	if (!pr::FileWrite(file, &data[0], DWORD(data.size()), &bytes_written) || bytes_written != data.size()) return EResult_FailedToWriteTHDData;
	return EResult_Success;
}

