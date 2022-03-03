//*******************************************************************************
// Terrain Exporter
//  Copyright © Rylogic Ltd 2009
//*******************************************************************************
// This header should only be included in cpp files otherwise you'll end up with
// circular include problems.
#pragma once
#include "terrainexporter/forward.h"
#include "terrainexporter/debug.h"

#if PR_DBG_TERRAIN

#include <string>
#include <stdio.h>
#include <stdarg.h>
#include "pr/common/fmt.h"
#include "pr/terrain/terrain.h"
#include "terrainexporter/cellex.h"

namespace pr
{
	namespace terrain
	{
		// Output the contributing edges for a cell (in region space)
		template <typename Out> void DumpContributingEdges(terrain::CellEx const& cell, Out& out)
		{
			int tree_index = 0;
			out += FmtS("*Group ContributingEdges_cell%d FFFFFF00 {\n", cell.m_cell_index);
			for (TTreeExList::const_iterator t = cell.m_tree.begin(), t_end = cell.m_tree.end(); t != t_end; ++t)
			{
				TreeEx const& tree = *t;
				out += FmtS("*Group Tree_%d FFFFFFFF {\n", tree_index++);
				for (TEdgeCPtrSet::const_iterator e = tree.m_edges.begin(), e_end = tree.m_edges.end(); e != e_end; ++e)
				{
					Edge const& edge = **e;
					if (edge.m_contributes)
					{
						out += FmtS("*Line edge FFFFFFFF {%f %f %f %f %f %f}\n"
							,(float)edge.m_vertex0->m_position[0] ,(float)edge.m_vertex0->m_position[1] ,(float)edge.m_vertex0->m_position[2]
							,(float)edge.m_vertex1->m_position[0] ,(float)edge.m_vertex1->m_position[1] ,(float)edge.m_vertex1->m_position[2]);
					}
				}
				out += "}\n";
			}
			out += "}\n";
		}

		// Output a single branch
		template <typename Out> void DumpEdge(char const* name, pr::uint colour, BranchEx const& branch, Out& out)
		{
			out += FmtS("*Line %s %X {%f %f %f  %f %f %f}\n"
				,name ,colour
				,(float)branch.m_edge->m_vertex0->m_position[0] ,(float)branch.m_edge->m_vertex0->m_position[1] ,(float)branch.m_edge->m_vertex0->m_position[2]
				,(float)branch.m_edge->m_vertex1->m_position[0] ,(float)branch.m_edge->m_vertex1->m_position[1] ,(float)branch.m_edge->m_vertex1->m_position[2]
				);
		}

		// Output a list of edges
		template <typename Out> void DumpEdgeList(char const* name, pr::uint colour, TBranchExList const& edges, Out& out)
		{
			out += FmtS("*Group %s %X {\n", name, colour);
			for (TBranchExList::const_iterator i = edges.begin(), i_end = edges.end(); i != i_end; ++i)
				DumpEdge("branch", colour, *i, out);
			out += "}\n";
		}

		// Output graphics for a tree
		struct ParentList { terrain::Branch const* m_branch; int m_side; ParentList const* m_next; };
		template <typename Out> void DumpTree(terrain::Branch const& tree, float x, float z, float sizeX, float sizeZ, ParentList const& parent, Out& out)
		{
			if (parent.m_branch == 0) out += FmtS("*Group tree FFFFFFFF {\n");

			// Draw the branch
			float x0 = 0.0f, z0 = 0.0f, x1 = 1.0f, z1 = 1.0f;		
			if (tree.m_a != 0 && tree.m_b != 0)
			{
				z0 = -(tree.m_c + tree.m_a * x0) / tree.m_b;
				if     ( z0 < 0.0f ) { z0 = 0.0f; x0 = -(tree.m_c + tree.m_b * z0) / tree.m_a; }
				else if( z0 > 1.0f ) { z0 = 1.0f; x0 = -(tree.m_c + tree.m_b * z0) / tree.m_a; }

				z1 = -(tree.m_c + tree.m_a * x1) / tree.m_b;
				if     ( z1 < 0.0f ) { z1 = 0.0f; x1 = -(tree.m_c + tree.m_b * z1) / tree.m_a; }
				else if( z1 > 1.0f ) { z1 = 1.0f; x1 = -(tree.m_c + tree.m_b * z1) / tree.m_a; }
			}
			else if (tree.m_a == 0)
			{
				z0 = -(tree.m_c + tree.m_a * x0) / tree.m_b;
				z1 = -(tree.m_c + tree.m_a * x1) / tree.m_b;
			}
			else if (tree.m_b == 0)
			{
				x0 = -(tree.m_c + tree.m_b * z0) / tree.m_a;
				x1 = -(tree.m_c + tree.m_b * z1) / tree.m_a;
			}
			// Clip to the parents
			for (ParentList const* p = &parent; p->m_branch != 0; p = p->m_next)
			{
				float d0 = p->m_side * Compare(*p->m_branch, x0, z0);
				float d1 = p->m_side * Compare(*p->m_branch, x1, z1);
				if (d0 < 0.0f)
				{
					float t = -d0 / (d1 - d0);
					x0 += t * (x1 - x0);
					z0 += t * (z1 - z0);
				}
				if (d1 < 0.0f)
				{
					float t = -d1 / (d0 - d1);
					x1 += t * (x0 - x1);
					z1 += t * (z0 - z1);
				}
			}

			out += FmtS("*Line branch FFFFFFFF { %f 0 %f  %f 0 %f\n", x + x0*sizeX, z + z0*sizeZ, x + x1*sizeX, z + z1*sizeZ);
			if (tree.m_left  >= 0)
			{
				ParentList p = {&tree, +1, &parent};
				DumpTree((&tree)[tree.m_left ], x, z, sizeX, sizeZ, p, out);
			}
			if (tree.m_right >= 0)
			{
				ParentList p = {&tree, -1, &parent};
				DumpTree((&tree)[tree.m_right], x, z, sizeX, sizeZ, p, out);
			}
			out += "}\n";

			if (parent.m_branch == 0) out += "}\n";
		}
		
		// Output graphics for a cell
		template <typename Out> void DumpCell(terrain::Cell const& cell, float x, float z, Out& out)
		{
			out += FmtS("*Group cell FFFFFFFF {\n");
			out += FmtS("*Rectangle cell_bounds FF00A000 { %f %f %f  %f %f %f  %f %f %f  %f %f %f }\n",
				x,                0.0f, z,
				x + cell.m_sizeX, 0.0f, z,
				x + cell.m_sizeX, 0.0f, z + cell.m_sizeZ,
				x,                0.0f, z + cell.m_sizeZ);
			
			for (pr::uint i = 0, i_end = cell.TreeCount(); i != i_end; ++i)
			{
				ParentList p = {0, 0, 0};
				DumpTree(*cell.Tree(i), x, z, cell.m_sizeX, cell.m_sizeZ, p, out);
			}
			out += "}\n";
		}
	}//namespace terrain
}//namespace pr

#endif//PR_DBG_TERRAIN
