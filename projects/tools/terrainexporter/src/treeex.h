//*******************************************************************************
// Terrain Exporter
//  Copyright (c) Rylogic Ltd 2009
//*******************************************************************************
#pragma once
#include "terrainexporter/forward.h"
#include "terrainexporter/branchex.h"
#include "terrainexporter/leafex.h"

namespace pr
{
	namespace terrain
	{
		struct TreeEx
		{
			bool			AddFace(Face* face);
			EResult			BuildBSPTree();
			EResult			GrowBSPTree(BranchEx*& tree, TBranchExList& branch_list);
			void			BuildBranchList(TBranchExList& branch_list) const;
			BranchEx		SelectBranch(TBranchExList& branch_list) const;
			void			DivideBranches(BranchEx& divider, TBranchExList& list, TBranchExList& left_list, TBranchExList& right_list) const;
			EResult			GetLeaf(Face const* face, LeafEx*& leaf);
			pr::uint		RequiredSizeInBytes() const;

			TFacePtrSet		m_faces;				// Pointers to a set of non-overlapping faces from which to build a BSP tree
			TEdgeCPtrSet	m_edges;				// Edges that contribute to the terrain
			TBranchExList	m_branch;				// The branches for this BSP tree
			TLeafExList		m_leaf;					// The leaves for this BSP tree
			CellEx const*	m_cell;					// The owner cell of this tree
			pr::uint		m_tree_id;				// A unique id for this tree
			pr::uint		m_num_contrib_edges;	// The number of edges in 'm_edges' that actually contribute
		};

	}//namespace terrain
}//namespace pr
