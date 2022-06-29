//*******************************************************************************
// Terrain Exporter
//  Copyright (c) Rylogic Ltd 2009
//*******************************************************************************
#include "stdafx.h"
#include "terrainexporter/cellex.h"
#include "pr/terrain/terrain.h"
#include "terrainexporter/utility.h"
#include "terrainexporter/edge.h"
#include "terrainexporter/face.h"
#include "terrainexporter/planedictionary.h"
#include "terrainexporter/leafex.h"
#include "terrainexporter/branchex.h"
#include "terrainexporter/treeex.h"
#include "terrainexporter/debughelpers.h"

using namespace pr;
using namespace pr::terrain;

// Return a unique id for a tree within 'cell'
// The x100 is to ensure all tree indexes are unique across all cells
// I.e. we're assuming there will never be 100 trees in one cell
inline pr::uint GetTreeId(CellEx const& cell, pr::uint tree_number)
{
	return cell.m_cell_index * 100 + tree_number;
}

// Create a minimal set of planes that represent the faces in this cell
EResult CreatePlanes(CellEx& cell, TPlaneVec& planes)
{
	PlaneDictionary plane_lookup;

	bool changes_made;
	int iteration_count = 3;
	do
	{
		plane_lookup.m_position_tolerance = 1.0f / PositionQuantisation;
		plane_lookup.Average();

		changes_made = false;
		float accum_error = 0.0f;
		for( TFacePtrVec::iterator f = cell.m_holding_pen.begin(), f_end = cell.m_holding_pen.end(); f != f_end; ++f )
		{
			Face& face = **f;

			float err;
			Plane const* plane = plane_lookup.GetPlane(face, err);
			changes_made |= face.m_plane != plane;
			face.m_plane = plane;
			accum_error += err;

			PR_ASSERT(PR_DBG_TERRAIN, err < plane_lookup.m_position_tolerance, "");
		}

		plane_lookup.RemoveEmptyEntries();

		PR_EXPAND(PR_DBG_TERRAIN, std::size_t num_planes = plane_lookup.m_lookup.size());
		PR_EXPAND(PR_DBG_TERRAIN, float avr_err = accum_error / (cell.m_holding_pen.empty() + cell.m_holding_pen.size()));
		PR_EXPAND(PR_DBG_TERRAIN, printf("Smoothing: normals %d   avr error %f   quality %f\n", num_planes, avr_err, num_planes * avr_err));
	}
	while( --iteration_count && changes_made );

	// Copy the planes into the provided array of planes
	int index = 0;
	planes.clear();
	planes.reserve(plane_lookup.m_lookup.size());
	for( PlaneDictionary::TPlaneLookup::iterator p = plane_lookup.m_lookup.begin(), p_end = plane_lookup.m_lookup.end(); p != p_end; ++p, ++index )
	{
		PlaneDictionary::Page& page = *p;
		planes.push_back(page.m_plane);
		page.m_index = index;
	}

	// Setup the pointers in the faces
	for( TFacePtrVec::iterator f = cell.m_holding_pen.begin(), f_end = cell.m_holding_pen.end(); f != f_end; ++f )
	{
		Face& face = **f;
		PlaneDictionary::Page const* page = reinterpret_cast<PlaneDictionary::Page const*>(face.m_plane);
		face.m_plane = &planes[page->m_index];

		// Check that the verts of this face are within 1.0f / PositionQuantisation of the plane
		#if PR_DBG_TERRAIN
		for( int i = 0; i != 3; ++i )
		{
			float dist = maAbs(face.m_plane->dotWithW(face.m_original_vertex[i]));
			if( dist >= 1.0f / PositionQuantisation )
			{
				PR_ASSERT(PR_DBG_TERRAIN, false, "Face found that deviates from the plane by more than the tolerance");
			}
		}
		#endif//PR_DBG_TERRAIN
	}
	return EResult_Success;
}

// Sort faces into buckets of non-overlapping faces (trees)
EResult SortIntoTrees(CellEx& cell)
{
	PR_ASSERT(PR_DBG_TERRAIN, cell.m_tree.empty(), "");
	std::size_t num_faces = cell.m_holding_pen.size();
	while( num_faces )
	{
		if( cell.m_tree.size() == ELimit_MaxLayers )
			return EResult_MaxTreesPerCellExceeded;

		// Create a tree
		cell.m_tree.push_back(TreeEx());
		TreeEx& tree	= cell.m_tree.back();
		tree.m_cell		= &cell;
		tree.m_tree_id	= GetTreeId(cell, uint(cell.m_tree.size()));

		// Loop through the holding pen adding faces to this tree. If any overlap add them back to 
		// the holding pen. Repeat until no more faces can be added to this tree.
		bool faces_added;
		do
		{
			faces_added = false;
			TFacePtrVec::iterator return_to_pen = cell.m_holding_pen.begin();
			TFacePtrVec::iterator get_from_pen  = cell.m_holding_pen.begin();
			for( std::size_t f = 0; f != num_faces; ++f, ++get_from_pen )
			{
				Face* face = *get_from_pen;

				if( tree.AddFace(face) )
					faces_added = true;
				else
					*return_to_pen++ = face;
			}

			num_faces = return_to_pen - cell.m_holding_pen.begin();
		}
		while( faces_added && num_faces != 0 );
	}

	// All faces have been copied to trees so we don't need the holding pen anymore
	cell.m_holding_pen.clear();
	return EResult_Success;
}

// Build a list of the contributing edges in each tree
void IdentifyContributingEdges(CellEx& cell)
{
	// Mark the faces in each tree with their tree id and reset the 'contributes' flag in each edge
	for( TTreeExList::iterator t = cell.m_tree.begin(), t_end = cell.m_tree.end(); t != t_end; ++t)
	{
		TreeEx& tree = *t;
		PR_ASSERT(PR_DBG_TERRAIN, tree.m_edges.empty(), "");
		for( TFacePtrSet::const_iterator f = tree.m_faces.begin(), f_end = tree.m_faces.end(); f != f_end; ++f )
		{
			Face const& face = **f;
			face.m_tree_id = tree.m_tree_id;
			for( pr::uint i = 0; i != 3; ++i )
			{
				Edge const& edge = *face.m_edges[i];
				if( tree.m_edges.find(&edge) != tree.m_edges.end() ) continue; // Only new edges
				if( Clip(edge.Line(), cell.m_bounds).Length() == 0.0f ) continue; // Only edges that intersect the cell

				edge.m_contributes = true;	// Mark the edge as contributing for now, we know it intersects the cell at least...
				tree.m_edges.insert(&edge);
			}
		}
	}

	// Now go through and find the edges that actually contribute to the terrain data in each tree
	// I.e. the edges that are not between joinable (co-planar with the same material) faces
	for( TTreeExList::iterator t = cell.m_tree.begin(), t_end = cell.m_tree.end(); t != t_end; ++t )
	{
		TreeEx& tree = *t;
		tree.m_num_contrib_edges = 0;
		for( TEdgeCPtrSet::iterator e = tree.m_edges.begin(), e_end = tree.m_edges.end(); e != e_end; ++e )
		{
			Edge const& edge = **e;
			edge.m_contributes = !IsBetweenJoinableFaces(edge);
			if( edge.m_contributes ) ++tree.m_num_contrib_edges;
		}
	}

	#if TERRAIN_ONECELL
	if( cell.m_cell_index == TARGET_CELL )
	{
		std::string str;
		DumpContributingEdges(cell, str);
		StringToFile(str, "D:/deleteme/terrain_cell6edges.pr_script");
	}
	#endif//TERRAIN_ONECELL
}

// CellEx *************************************************************
CellEx::CellEx()
:m_bounds(FRectReset)
,m_cell_index(pr::uint(-1))
,m_scale_X(1.0f)
,m_scale_Z(1.0f)
,m_child_index(0)
,m_split_count(0)
,m_degenerate_cell(0)
{}

// Return the type of cell this is
// Note: cell type is inferred from the number of trees and whether the split pointers are null or not
ECellType CellEx::CellType() const
{
	if( m_degenerate_cell )		return m_degenerate_cell->CellType();
	if( m_child_index != 0 )	return ECellType_Split;
	if( !m_tree.empty() )		return ECellType_Tree;
	return ECellType_Empty;
}

// Return the size in bytes of the header data for a cell
pr::uint CellEx::CellHeaderSizeInBytes() const
{
	uint size = sizeof(Cell);															// The cell itself
	size += uint(m_tree.size() * sizeof(uint8));										// Followed by a table of offsets
	return ((size + ELimit_BIndexUnit - 1) / ELimit_BIndexUnit) * ELimit_BIndexUnit;	// Rounded up to the nearest branch index unit
}

// Return the size in bytes required for this cell in the exported terrain data
pr::uint CellEx::RequiredSizeInBytes() const
{
	pr::uint size = CellHeaderSizeInBytes();
	for( TTreeExList::const_iterator t = m_tree.begin(), t_end = m_tree.end(); t != t_end; ++t )
		size += t->RequiredSizeInBytes();

	// Round up to the nearest multiple of ELimit_UnitSize
	return ((size + ELimit_UnitSize - 1) / ELimit_UnitSize) * ELimit_UnitSize;
}

// Make this cell empty (preserving split and degenerate pointers)
// This is to free up any memory it might be using
void CellEx::Clear()
{
	m_holding_pen.clear();
	m_tree.clear();
	m_planes.clear();
}

// Add a face to this cell.
void CellEx::AddFace(Face* face)
{
	m_holding_pen.push_back(face);
}

// This method builds a BSP tree for each TreeEx in this cell.
EResult CellEx::BuildBSPTrees()
{
	EResult result;

	// Create the minimal set of planes needed to represent the faces in this cell
	result = CreatePlanes(*this, m_planes);
	if (Failed(result)) return result;
	
	// Sort the faces into non-overlapping groups. Each group = 1 tree
	result = SortIntoTrees(*this);
	if (Failed(result)) return result;

	// Identify the edges that contribute to the terrain data in each tree
	IdentifyContributingEdges(*this);

	// Build a BSP tree for each tree in this cell
	pr::uint cell_size_in_bytes = CellHeaderSizeInBytes();
	for (TTreeExList::iterator t = m_tree.begin(), t_end = m_tree.end(); t != t_end; ++t)
	{
		// Ensure the total size of the cell is less than the max cell size.
		// Having a max cell size allows terrain cells to be copied into fixed size buffers.
		if (cell_size_in_bytes > ELimit_MaxCellSizeInBytes)
			return EResult_CellNeedsSplitting;

		TreeEx& tree = *t;
		EResult res = tree.BuildBSPTree();
		if (Failed(res)) { return res; }
		
		cell_size_in_bytes += tree.RequiredSizeInBytes();
	}
	return EResult_Success;
}
