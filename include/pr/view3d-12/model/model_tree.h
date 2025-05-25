//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12
{
	// A tree of models.
	//        A
	//      /   \
	//     B     C
	//   / | \   |
	//  D  E  F  G
	//  Serialised as: A0 B1 D2 E2 F2 C1 G2 (i.e. a depth first traversal of the tree)
	//  Children are all nodes to the right with level > the current.
	//
	// Notes:
	//  - A model tree represents a single hierarchy of models.
	//  - If there are multiple root level models, then there is an implicit empty model that contains them all 
	struct ModelTreeNode
	{
		// Transform from this model to its parent
		m4x4 m_o2p;

		// The renderer model for this node in the model tree
		ModelPtr m_model;

		// The height of this node in the tree
		int m_level;
		
		ModelTreeNode() = default;
		ModelTreeNode(ModelPtr model, m4_cref o2p, int level)
			:m_o2p(o2p)
			,m_model(model)
			,m_level(level)
		{}
	};
	using ModelTree = pr::vector<ModelTreeNode>;
}