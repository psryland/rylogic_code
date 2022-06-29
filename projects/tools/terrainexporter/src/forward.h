//*******************************************************************************
// Terrain Exporter
//  Copyright (c) Rylogic Ltd 2009
//*******************************************************************************
#pragma once
#include <vector>
#include <set>
#include <list>
#include <string>
#include "pr/maths/maths.h"
#include "pr/common/array.h"
#include "pr/common/proxy.h"
#include "pr/common/valuecast.h"

namespace pr
{
	namespace terrain
	{
		enum
		{
			// Quantisation values have to be powers of two to prevent errors due to floating point accuracy
			PositionQuantisationBits	= 6,
			PositionQuantisation		= 1 << PositionQuantisationBits,
			BranchQuantisationBits		= 16,
			BranchQuantisation			= ((1 << BranchQuantisationBits) - 1) / 2,
			NoTree						= 0xFFFF,
			MaxCellSubDivision			= 10		// This is the maximum number of times we'll allow a cell to be subdivided
		};

		enum EResult
		{
			EResult_Success						= 0,
			EResult_Failed						= 0x8000000,
			EResult_Cancelled,
			EResult_ErrorAlreadyReported,
			EResult_MaxTreesPerCellExceeded,
			EResult_CellNeedsSplitting,
			EResult_CellSplitTooOften,
			EResult_TooManyCells,
			EResult_TooManySplitCells,
			EResult_TooManyPlanes,
			EResult_TooManyTrees,
			EResult_FailedToOpenTHDFile,
			EResult_FailedToWriteTHDData,
			EResult_FailedToOpenTestDataFile,
			EResult_RootObjectNotFound,
			EResult_MaterialIdOutOfRange,
		};

		struct LineEqn;
		struct Line2d;
		struct Material;
		struct Vertex;
		struct Face;
		struct Edge;

		struct CellEx;
		struct CellSplitEx;
		struct TreeEx;
		struct BranchEx;
		struct LeafEx;
		
		struct Pred
		{
			bool operator () (Face const* lhs, Face const* rhs) const; // In Face.h
			bool operator () (Edge const* lhs, Edge const* rhs) const; // In Edge.h
		};

		typedef pr::vector<pr::uint>			TVertDict;
		typedef pr::vector<Plane>			TPlaneVec;
		typedef pr::vector<Vertex>			TVertVec;
		typedef pr::vector<Edge>				TEdgeVec;
		typedef pr::vector<Edge*>			TEdgePtrVec;
		typedef pr::vector<Edge const*>		TEdgeCPtrVec;
		typedef std::set<Edge const*, Pred>	TEdgeCPtrSet;
		typedef std::multiset<Edge>			TEdgeSet;
		typedef pr::vector<Face>				TFaceVec;
		typedef pr::vector<Face*>			TFacePtrVec;
		typedef pr::vector<Face const*>		TFaceCPtrVec;
		typedef std::set<Face*, Pred>		TFacePtrSet;
		typedef std::set<Face const*, Pred>	TFaceCPtrSet;

		typedef std::list<LeafEx>               TLeafExList;
		typedef std::list<pr::Proxy<BranchEx> > TBranchExList;
		typedef std::list<TreeEx>               TTreeExList;
		typedef std::list<pr::Proxy<CellEx> >   TCellExList;
		typedef pr::vector<LeafEx const*>        TLeafExCPtrVec;
		typedef pr::vector<BranchEx const*>      TBranchExCPtrVec;
		typedef pr::vector<TreeEx const*>        TTreeExCPtrVec;

	}//namespace terrain

	// Result testing
	inline bool        Failed        (terrain::EResult result)	{ return result  < 0; }
	inline bool        Succeeded     (terrain::EResult result)	{ return result >= 0; }
	inline void        Verify        (terrain::EResult result);
	inline char const* GetErrorString(terrain::EResult result);
}//namespace pr
