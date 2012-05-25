//*******************************************************************************
// Terrain Exporter
//  Copyright © Rylogic Ltd 2009
//*******************************************************************************
#pragma once
#include "pr/terrain/terrain.h"
#include "terrainexporter/forward.h"
#include "terrainexporter/treeex.h"

namespace pr
{
	namespace terrain
	{
		enum ECellType
		{
			ECellType_Empty,
			ECellType_Tree,
			ECellType_Split
		};

		struct CellEx
		{
			CellEx();
			ECellType		CellType() const;
			pr::uint		CellHeaderSizeInBytes() const;
			pr::uint		RequiredSizeInBytes() const;
			void			Clear();
			void			AddFace(Face* face);
			EResult			BuildBSPTrees();

			pr::FRect		m_bounds;				// The boundary of this cell in region space.
			pr::uint		m_cell_index;			// The cell's index within the cell list
			float			m_scale_X;				// The X direction scale factor used to convert this cell to a unit cell
			float			m_scale_Z;				// The Z direction scale factor used to convert this cell to a unit cell
			TFacePtrVec		m_holding_pen;			// A container for holding faces before they are sorted into trees
			TTreeExList		m_tree;					// The trees in this cell
			TPlaneVec		m_planes;				// A minimal set of planes used to represent the faces in this cell
			pr::uint		m_child_index;			// The index of the first child cell if this is a split cell. 0 if not a split cell
			int				m_split_count;			// The number of times the original cell has been split.
			CellEx*			m_degenerate_cell;		// A pointer to a cell that this cell is degenerate with (NULL if none)
		};
	}//namespace terrain
}//namespace pr

