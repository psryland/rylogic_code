//*******************************************************************************
// Terrain Exporter
//  Copyright (c) Rylogic Ltd 2009
//*******************************************************************************
#pragma once
#include "pr/terrain/terrain.h"
#include "terrainexporter/forward.h"
#include "terrainexporter/lineeqn.h"
#include "terrainexporter/line2d.h"
#include "terrainexporter/utility.h"
#include "terrainexporter/edge.h"

namespace pr
{
	namespace terrain
	{
		struct BranchEx
		{
			Branch			m_branch;				// The branch that will go into the final data
			BranchEx*		m_Lbranch;				// Pointer to the left child tree
			BranchEx*		m_Rbranch;				// Pointer to the right child tree
			LeafEx*			m_Lleaf;				// Pointer to the left leaf
			LeafEx*			m_Rleaf;				// Pointer to the right leaf
			Edge const*		m_edge;					// The original edge used to create this branch
			Line2d			m_line;					// A line that is progressively clipped during creation of a BSP tree
			int				m_index;				// The index of this branch within the branch list for a bsp tree

			BranchEx::BranchEx()
			{
				m_Lbranch	= 0;
				m_Rbranch	= 0;
				m_Lleaf		= 0;
				m_Rleaf		= 0;
				m_edge		= 0;
				m_index		= -1;
			}

			BranchEx::BranchEx(Edge const* edge, CellEx const& cell)
			{
				m_edge		= edge;
				m_line		= m_edge->Line();
				m_Lbranch	= 0;
				m_Rbranch	= 0;
				m_Lleaf		= 0;
				m_Rleaf		= 0;
				m_index		= -1;

				// Make the line for this branch cell relative
				Line2d scaled_line = ScaleToCell(m_line, cell);

				// Convert to lower precision
				LineEqn eqn		= scaled_line.Eqn(); eqn.Normalise();
				m_branch.m_a	= pr::value_cast<BranchUnit>(Floor(0.5f + Clamp(eqn.m_a, -1.0f, 1.0f) * BranchQuantisation));
				m_branch.m_b	= pr::value_cast<BranchUnit>(Floor(0.5f + Clamp(eqn.m_b, -1.0f, 1.0f) * BranchQuantisation));
				m_branch.m_c	= pr::value_cast<BranchUnit>(Floor(0.5f + Clamp(eqn.m_c, -1.0f, 1.0f) * BranchQuantisation));
			}
		};
		inline bool operator == (BranchEx const& lhs, BranchEx const& rhs)
		{
			return	lhs.m_branch.m_a == rhs.m_branch.m_a && lhs.m_branch.m_b == rhs.m_branch.m_b && lhs.m_branch.m_c == rhs.m_branch.m_c;
		}
		inline bool operator < (BranchEx const& lhs, BranchEx const& rhs)
		{
			if (lhs.m_branch.m_a != rhs.m_branch.m_a) return lhs.m_branch.m_a < rhs.m_branch.m_a;
			if (lhs.m_branch.m_b != rhs.m_branch.m_b) return lhs.m_branch.m_b < rhs.m_branch.m_b;
			return lhs.m_branch.m_c < rhs.m_branch.m_c;
		}
	}//namespace terrain
}//namespace pr
