//*******************************************************************************
// Terrain Exporter
//  Copyright © Rylogic Ltd 2009
//*******************************************************************************
#include "stdafx.h"
#include "terrainexporter/treeex.h"
#include "terrainexporter/utility.h"
#include "terrainexporter/edge.h"
#include "terrainexporter/face.h"
#include "terrainexporter/leafex.h"
#include "terrainexporter/branchex.h"
#include "terrainexporter/cellex.h"
#include "terrainexporter/debughelpers.h"

using namespace pr;
using namespace pr::terrain;

// This method checks whether 'face' overlaps any of the other faces already in this tree
// If not, then 'face' is added to the tree and true is returned, otherwise false is returned.
bool TreeEx::AddFace(Face* face)
{
	for (TFacePtrSet::const_iterator f = m_faces.begin(), f_end = m_faces.end(); f != f_end; ++f)
	{
		Face const& existing = **f;
		if (!ShareCommonEdge(existing, *face) && IsIntersection(existing, *face)) return false;
	}

	// If we get to here then 'face' didn't overlap any
	// of our existing faces so we add it to the list.
	face->m_tree_id = m_tree_id;
	m_faces.insert(face);
	return true;
}

// This method builds a BSP tree using the edges in this tree
EResult TreeEx::BuildBSPTree()
{
	// Make a list of branches from the contributing edges
	TBranchExList branch_list;
	BuildBranchList(branch_list);

	// If this tree contains no contributing edges then create a single leaf
	if (branch_list.empty())
	{
		LeafEx* leaf;
		EResult result = GetLeaf(*m_faces.begin(), leaf);
		if (Failed(result)) return result;

		// Create a dummy branch
		BranchEx b;
		b.m_Lleaf = leaf;
		b.m_Rleaf = leaf;
		b.m_index = int(m_branch.size() - 1);
		m_branch.push_back(b);
	}
	else
	{
		// Grow the BSP tree in this cell
		BranchEx* tree_top = 0;
		EResult res = GrowBSPTree(tree_top, branch_list);
		if (Failed(res)) return res;
	}

	// Check that the relative indices from branches to leaves are within the range of BranchIndex
	// We only need to test the distance from branches to leaves, because the branch to branch
	// distance was checked during GrowBSPTree().
	for (TBranchExList::const_iterator i = m_branch.begin(), i_end = m_branch.end(); i != i_end; ++i)
	{
		BranchEx const& branch = *i;

		// Every branch should point to another branch or a leaf
		PR_ASSERT(PR_DBG_TERRAIN, (branch.m_Lbranch != 0) != (branch.m_Lleaf != 0), "Every branch must point to a leaf or another branch but not both");
		PR_ASSERT(PR_DBG_TERRAIN, (branch.m_Rbranch != 0) != (branch.m_Rleaf != 0), "Every branch must point to a leaf or another branch but not both");

		// This should be ensured during GrowBSPTree()
		PR_ASSERT(PR_DBG_TERRAIN, branch.m_Lbranch == 0 || branch.m_Lbranch->m_index - branch.m_index <= ELimit_BIndexMax, "");
		PR_ASSERT(PR_DBG_TERRAIN, branch.m_Rbranch == 0 || branch.m_Rbranch->m_index - branch.m_index <= ELimit_BIndexMax, "");

		// Check the distances to leaves
		if (branch.m_Lleaf != 0 && branch.m_Lleaf->m_index + m_branch.size() - branch.m_index > ELimit_BIndexMax) return EResult_CellNeedsSplitting;
		if (branch.m_Rleaf != 0 && branch.m_Rleaf->m_index + m_branch.size() - branch.m_index > ELimit_BIndexMax) return EResult_CellNeedsSplitting;
	}
	return EResult_Success;
}

// This method constructs a list of branches from the contributing edges in this tree
void TreeEx::BuildBranchList(TBranchExList& branch_list) const
{
	// Create branches from all of the edges
	for (TEdgeCPtrSet::const_iterator e = m_edges.begin(), e_end = m_edges.end(); e != e_end; ++e)
	{
		Edge const& edge = **e;
		if (!edge.m_contributes) continue;
		
		// We only want the parts of edges that intersect the cell
		BranchEx branch(&edge, *m_cell);
		branch.m_line = Clip(branch.m_line, m_cell->m_bounds);
		branch_list.push_back(branch);

		PR_ASSERT(PR_DBG_TERRAIN, branch.m_line.Length() != 0.0f, "Edges that don't intersect the cell shouldn't be in this set");
	}
}

// This method recursively grows a BSP tree.
// If EResult_Success is returned, 'tree' points to the remainder of the tree.
EResult TreeEx::GrowBSPTree(BranchEx*& tree, TBranchExList& branch_list)
{
	m_branch.push_back(SelectBranch(branch_list));
	tree = m_branch.back().m_ptr;
	tree->m_index = int(m_branch.size() - 1);
	PR_ASSERT(PR_DBG_TERRAIN, tree->m_line.Length() > 0.0f, "");

	// Divide the remaining branches from 'branch_list' into two new branch lists
	TBranchExList left_list;	// The branches to the left of 'tree'
	TBranchExList right_list;	// The branches to the right of 'tree'
	DivideBranches(*tree, branch_list, left_list, right_list);
	#if TERRAIN_ONECELL
	if (m_cell->m_cell_index == TARGET_CELL)
	{
		std::string str;
		DumpEdge("divider", 0xFFFFFFFF, *tree, str);
		DumpEdgeList("left", 0xFFFF0000, left_list, str);
		DumpEdgeList("right", 0xFF0000FF, right_list, str);
		StringToFile(str, "D:/deleteme/terrain_dividededges.pr_script");
	}
	#endif

	// If there are no more branches to the left of the current branch, add a leaf
	if (left_list.empty())
	{
		EResult result = GetLeaf(tree->m_edge->m_Lface, tree->m_Lleaf);
		if (Failed(result)) return result;
	}
	// Otherwise there are more edges to the left and we need to keep growing
	else
	{
		EResult result = GrowBSPTree(tree->m_Lbranch, left_list);
		if (Failed(result)) return result;
		if (tree->m_Lbranch->m_index - tree->m_index > ELimit_BIndexMax) return EResult_CellNeedsSplitting;
	}

	// If there are no more branches to the right of the current branch, add a leaf
	if (right_list.empty())
	{
		EResult result = GetLeaf(tree->m_edge->m_Rface, tree->m_Rleaf);
		if (Failed(result)) return result;
	}
	// Otherwise there are more edges to the right and we need to keep growing
	else
	{
		EResult result = GrowBSPTree(tree->m_Rbranch, right_list);
		if (Failed(result)) return result;
		if (tree->m_Rbranch->m_index - tree->m_index > ELimit_BIndexMax) return EResult_CellNeedsSplitting;
	}
	return EResult_Success;
}

typedef std::pair<TBranchExList::iterator, int> TColinearCount;
typedef std::vector<TColinearCount>				TColinearCountVec;
inline bool Pred_ColinearMaxCount(const TColinearCount& lhs, const TColinearCount& rhs) { return lhs.second < rhs.second; }

// This method returns the branch from 'branch_list' that is the "best" choice for
// dividing the other branches. A branch from the largest set of colinear branches
// is choosen because this will make the BSP trees has short as possible.
BranchEx TreeEx::SelectBranch(TBranchExList& branch_list) const
{
	PR_ASSERT(PR_DBG_TERRAIN, !branch_list.empty(), "This method should not be called for empty branch lists");
	if (branch_list.size() == 1) { BranchEx branch = branch_list.front(); branch_list.pop_front(); return branch; }

	// Choose the branch that is colinear with the most other branches
	TColinearCountVec colinear_count;
	for (TBranchExList::iterator b = branch_list.begin(), b_end = branch_list.end(); b != b_end; ++b)
	{
		bool colinear_edge_found = false;
		for (TColinearCountVec::iterator i = colinear_count.begin(), i_end = colinear_count.end(); i != i_end; ++i)
		{
			if (IsColinear(*i->first, *b, EDim_2d))
			{
				colinear_edge_found = true;
				++i->second;
				if ((*b).get().m_line.Length() > (*i->first).get().m_line.Length()) { i->first = b; }
				break;
			}
		}
		if (!colinear_edge_found)
		{
			colinear_count.push_back(TColinearCount(b, 0));
		}
	}

	// Find the branch with the most colinear branches
	TColinearCountVec::iterator most_colinear = std::max_element(colinear_count.begin(), colinear_count.end(), Pred_ColinearMaxCount);
	BranchEx result = *most_colinear->first;
	branch_list.erase(most_colinear->first);
	return result;
}

// Distribute the branches in 'list' into 'left_list' and 'right_list'
void TreeEx::DivideBranches(BranchEx& divider, TBranchExList& list, TBranchExList& Llist, TBranchExList& Rlist) const
{
	while (!list.empty())
	{
		BranchEx branch = list.front();
		list.pop_front();

		// Note: This code throws away co-linear branches. The justification for this is:
		// At each stage of building the bsp tree we are free to choose any branch. If we find a
		// branch that is co-linear with the current parent branch then it will divide all of
		// the remaining branches in exactly the same way and therefore it shouldn't be needed.
		if (IsColinear(branch, divider, EDim_2d)) continue;

		Line2d Lbit = Clip(branch.m_line,  divider.m_line);
		Line2d Rbit = Clip(branch.m_line, -divider.m_line);
	
		if (Lbit.Length() > 0.0f) { branch.m_line = Lbit; Llist.push_back(branch); }
		if (Rbit.Length() > 0.0f) { branch.m_line = Rbit; Rlist.push_back(branch); }
	}
}

// This method returns a leaf that points to 'face' or one that points to a face that is equivalent to 'face'
EResult TreeEx::GetLeaf(Face const* face, LeafEx*& leaf)
{
	// Faces that aren't in this tree can't add leaves to this tree
	if (face && face->m_tree_id != m_tree_id) face = 0;

	// Look for an existing leaf with a face that is equivalent to 'face'
	for (TLeafExList::iterator l = m_leaf.begin(), l_end = m_leaf.end(); l != l_end; ++l)
	{
		if (IsEquivalent(l->m_face, face))
		{
			leaf = &*l;
			PR_EXPAND(PR_DBG_TERRAIN, leaf->m_faces.insert(face)); // Add the face to the set of equivalent faces for this leaf
			return EResult_Success;
		}
	}

	// If the number of leaves exceeds this limit then it won't be
	// possible for branches to index to these leaves using a BranchIndex
	if (m_leaf.size() > ELimit_BIndexMax) { return EResult_CellNeedsSplitting; }

	// If we get here then we do not already have a leaf that points to 
	// a face that is the same as 'face' so create one and add it to the list.
	m_leaf.push_back(LeafEx(int(m_leaf.size()), face));
	leaf = &m_leaf.back();
	return EResult_Success;
}

// Return the size in bytes required for this tree in the final exported data
uint TreeEx::RequiredSizeInBytes() const
{
	uint size = uint(m_branch.size() * sizeof(terrain::Branch) + m_leaf.size() * sizeof(terrain::Leaf));
	PR_ASSERT(PR_DBG_TERRAIN, (size % ELimit_BIndexUnit) == 0, "branchs and leaves should be in multiples of ELimit_BIndexUnit's");
	return size;
}
