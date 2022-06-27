//*******************************************************************************
// Terrain Exporter
//  Copyright (c) Rylogic Ltd 2009
//*******************************************************************************
#pragma once
#include "terrainexporter/forward.h"

namespace pr
{
	namespace terrain
	{
		enum EDim 
		{
			EDim_2d,
			EDim_3d
		};
		inline v2 Proj2d				(v4 const& v) { return v2::make(v.x, v.z); }
		float	Quantise				(float value, uint quantisation);
		v4		Quantise				(v4 const& vec, uint quantisation);
		bool	IsIntersection			(FRect const& lhs, FRect const& rhs);
		bool	IsIntersection			(FRect const& rect, v4 const& position, float radius);
		bool	IsIntersection			(FRect const& rect, Face const& face);
		bool	IsIntersection			(Face const& lhs, Face const& rhs);
		bool	IsWithin				(v4 const& point, FRect const& rect);
		bool	IsWithin				(v4 const& point, Face const& face);
		Line2d	Clip					(Line2d const& line, Face const& face);
		Line2d	Clip					(Line2d const& line, FRect const& rect);
		Line2d	Clip					(Line2d const& clippee, Line2d const& clipper);
		Line2d	ScaleToCell				(Line2d const& line, const CellEx& cell);
		bool	IsValidFace				(Face const& face);
		bool	IsEquivalent			(Face const* lhs, Face const* rhs);
		bool	IsBetweenJoinableFaces	(Edge const& edge);
		float	CalculateArea			(Face const& face);
		v4		CalculateNormal			(Face const& face);
		v4		CalculatePlane			(Face const& face);
		bool	ShareCommonVertex		(Face const& lhs, Face const& rhs);
		bool	ShareCommonEdge			(Face const& lhs, Face const& rhs);
		bool	IsColinear				(BranchEx const& lhs, BranchEx const& rhs, EDim dimension);
		bool	IsRedundant				(BranchEx const& lhs, BranchEx const& rhs);
		bool	IsDegenerate			(TreeEx const& lhs, TreeEx const& rhs);
		bool	IsDegenerate			(BranchEx const& lhs, BranchEx const& rhs);
		bool	IsDegenerate			(LeafEx const& lhs, LeafEx const& rhs);
	}//namespace terrain
}//namespace pr
