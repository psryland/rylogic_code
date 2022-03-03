//*******************************************************************************
// Terrain Exporter
//  Copyright © Rylogic Ltd 2009
//*******************************************************************************
#include "stdafx.h"
#include "terrainexporter/utility.h"
#include "pr/common/valuecast.h"
#include "terrainexporter/line2d.h"
#include "terrainexporter/lineeqn.h"
#include "terrainexporter/vertex.h"
#include "terrainexporter/edge.h"
#include "terrainexporter/face.h"
#include "terrainexporter/leafex.h"
#include "terrainexporter/branchex.h"
#include "terrainexporter/treeex.h"
#include "terrainexporter/cellex.h"

using namespace pr;
using namespace pr::terrain;

void pr::Verify(pr::terrain::EResult result)
{
	PR_ASSERT(PR_DBG_TERRAIN, Succeeded(result), "Verify failure");
	PR_UNUSED(result); 
}
char const* pr::GetErrorString(pr::terrain::EResult result)
{
	switch (result)
	{
	default: PR_ASSERT(PR_DBG_TERRAIN, false, "Unknown terrain error code"); return "";
	case EResult_Success:					return "EResult_Success";
	case EResult_Failed:					return "EResult_Failed";
	case EResult_Cancelled:					return "EResult_Cancelled";
	case EResult_ErrorAlreadyReported:		return "EResult_ErrorAlreadyReported";
	case EResult_MaxTreesPerCellExceeded:	return "EResult_MaxTreesPerCellExceeded";
	case EResult_CellNeedsSplitting:		return "EResult_CellNeedsSplitting";
	case EResult_CellSplitTooOften:			return "EResult_CellSplitTooOften";
	case EResult_TooManyCells:				return "EResult_TooManyCells";
	case EResult_TooManySplitCells:			return "EResult_TooManySplitCells";
	case EResult_TooManyPlanes:				return "EResult_TooManyPlanes";
	case EResult_TooManyTrees:				return "EResult_TooManyTrees";
	case EResult_FailedToOpenTHDFile:		return "EResult_FailedToOpenTHDFile";
	case EResult_FailedToWriteTHDData:		return "EResult_FailedToWriteTHDData";
	case EResult_FailedToOpenTestDataFile:	return "EResult_FailedToOpenTestDataFile";
	case EResult_RootObjectNotFound:		return "EResult_RootObjectNotFound";
	case EResult_MaterialIdOutOfRange:		return "EResult_MaterialIdOutOfRange";
	}
}


inline Line2d LeftEdge  (FRect const& rect)	{ return Line2d(v4::make(rect.m_min.x, 0, rect.m_min.y, 0), v4::make(0, 0, rect.m_max.y - rect.m_min.y, 0)); }
inline Line2d TopEdge   (FRect const& rect)	{ return Line2d(v4::make(rect.m_min.x, 0, rect.m_max.y, 0), v4::make(rect.m_max.x - rect.m_min.x, 0, 0, 0)); }
inline Line2d RightEdge (FRect const& rect)	{ return Line2d(v4::make(rect.m_max.x, 0, rect.m_max.y, 0), v4::make(0, 0, rect.m_min.y - rect.m_max.y, 0)); }
inline Line2d BottomEdge(FRect const& rect)	{ return Line2d(v4::make(rect.m_max.x, 0, rect.m_min.y, 0), v4::make(rect.m_min.x - rect.m_max.x, 0, 0, 0)); }

// Quantise a float by converting it to an integer and rounding up from 0.5
float pr::terrain::Quantise(float value, uint quantisation)
{
	return int(value * quantisation + 0.5f) / float(quantisation);
}

// Quantise a vector
v4 pr::terrain::Quantise(v4 const& vec, uint quantisation)
{
	return v4::make(
		Quantise(vec.x, quantisation),
		Quantise(vec.y, quantisation),
		Quantise(vec.z, quantisation),
		vec.w);
}

// Return true if the bounding rect 'position' + 'radius' is within 'rect'
bool pr::terrain::IsIntersection(FRect const& rect, v4 const& position, float radius)
{
	return !(position.x + radius < rect.m_min.x || position.x - radius > rect.m_max.x ||
			 position.z + radius < rect.m_min.y || position.z - radius > rect.m_max.y);
}

// Return true if 'rect' overlaps 'face'
bool pr::terrain::IsIntersection(FRect const& rect, Face const& face)
{
	// Check the face clipped against the rect, and the rect clipped against the face
	return	IsIntersection(rect, face.MidPoint(), 0.0f) ||
			Clip(face.Line(0) ,rect).Length() > 0.0f ||
			Clip(face.Line(1) ,rect).Length() > 0.0f ||
			Clip(face.Line(2) ,rect).Length() > 0.0f ||
			Clip(LeftEdge  (rect) ,face).Length() > 0.0f ||
			Clip(TopEdge   (rect) ,face).Length() > 0.0f ||
			Clip(RightEdge (rect) ,face).Length() > 0.0f ||
			Clip(BottomEdge(rect) ,face).Length() > 0.0f;
}

// Return true 'lhs' overlaps 'rhs'
bool pr::terrain::IsIntersection(Face const& lhs, Face const& rhs)
{
	return	IsWithin(lhs.MidPoint(), rhs) ||
			Clip(lhs.Line(0), rhs).Length() > 0.0f ||
			Clip(lhs.Line(1), rhs).Length() > 0.0f ||
			Clip(lhs.Line(2), rhs).Length() > 0.0f ||
			Clip(rhs.Line(0), lhs).Length() > 0.0f ||
			Clip(rhs.Line(1), lhs).Length() > 0.0f ||
			Clip(rhs.Line(2), lhs).Length() > 0.0f;
}

// Return true if 'point' is within 'face' (but not on the edge)
bool pr::terrain::IsWithin(pr::v4 const& point, Face const& face)
{
	return	face.Line(0).Distance(point) > 0.0f &&
			face.Line(1).Distance(point) > 0.0f &&
			face.Line(2).Distance(point) > 0.0f;
}

// Clip 'clippee' to 'clipper' returning the portion of 'clippee' that is to the left of 'clipper'
Line2d pr::terrain::Clip(Line2d const& clippee, Line2d const& clipper)
{
	Line2d result = clippee;

	// Always clip the line assuming 'start' is to the left
	pr::v4 start = clippee.m_point;
	pr::v4 end   = clippee.m_point + clippee.m_edge;
	
	float start_dist = clipper.Distance(start);
	float end_dist   = clipper.Distance(end);

	// If both ends of 'clippee' are to the left then nothing is clipped
	// Consider co-linear lines as not-clipped. Why? because lines that are separated only in Y have to be considered
	// as intersecting otherwise one of the edges will be discarded in "DivideBranches" and a face will be lost.
	if (start_dist >= 0.0f && end_dist >= 0.0f) return result;

	// If neither end of 'clippee' is on the left of 'clipper' then the resulting line is clipped away
	if (start_dist < 0.0f && end_dist < 0.0f) 
	{
		result.m_t1 = result.m_t0 = 1.0f;
		return result;
	}

	PR_ASSERT(PR_DBG_TERRAIN, Cross3(clippee.m_edge, clipper.m_edge).Length3() > 0.0f, "");
	bool start_is_to_the_left = Cross3(clippee.m_edge, clipper.m_edge).y > 0.0f;
	float x = start_dist / (start_dist - end_dist);

	// If 'x' is beyond the parametric values in 'clippee' then it is wholely clipped or wholely not clipped.
	if (start_is_to_the_left)
	{
		if (x <= clippee.m_t0) { result.m_t1 = result.m_t0 = 1.0f; return result;	}	// all clipped
		if (x >= clippee.m_t1) { return result;										}	// not clipped
		result.m_t1 = x;																// clip the end
	}
	else
	{
		if (x <= clippee.m_t0) { return result;										}	// not clipped
		if (x >= clippee.m_t1) { result.m_t1 = result.m_t0 = 1.0f; return result;	}	// all clipped
		result.m_t0 = x;																// clip the start
	}

	PR_ASSERT(PR_DBG_TERRAIN, result.m_t1 >= result.m_t0, "");
	return result;
}

// Clip 'line' to 'rect'
Line2d pr::terrain::Clip(Line2d const& line, FRect const& rect)
{
	Line2d result = line;
	result = Clip(result, LeftEdge  (rect));
	result = Clip(result, TopEdge   (rect));
	result = Clip(result, RightEdge (rect));
	result = Clip(result, BottomEdge(rect));
	return result;
}

// Clip 'line' to 'face'
Line2d pr::terrain::Clip(Line2d const& line, Face const& face)
{
	Line2d result = line;
	result = Clip(result, face.Line(0));
	result = Clip(result, face.Line(1));
	result = Clip(result, face.Line(2));
	return result;
}

// Translate and scale 'line' into cell co-ordinates
Line2d pr::terrain::ScaleToCell(Line2d const& line, const CellEx& cell)
{
	pr::v4 offset = pr::v4::make(cell.m_bounds.m_min.x, 0.0f, cell.m_bounds.m_min.y, 0.0f);
	pr::v4 scale  = pr::v4::make(cell.m_scale_X,        1.0f, cell.m_scale_Z,        1.0f);
	return Line2d((line.m_point - offset) * scale, line.m_edge * scale, line.m_t0, line.m_t1);
}

// Returns true if 'face' passes our criteria for a valid face
bool pr::terrain::IsValidFace(Face const& face)
{
	// If any of the verts are degenerate, then it's not a valid face
	if (*face.m_vertices[0] == *face.m_vertices[1]) return false;
	if (*face.m_vertices[1] == *face.m_vertices[2]) return false;
	if (*face.m_vertices[2] == *face.m_vertices[0]) return false;

	// If the face is downward pointing or vertical, or has no area, then it's not valid
	pr::v4 const& v0 = face.m_vertices[0]->m_position;
	pr::v4 const& v1 = face.m_vertices[1]->m_position;
	pr::v4 const& v2 = face.m_vertices[2]->m_position;
	pr::v4 normal = Cross3(v2 - v1, v0 - v1);
	if (normal.y <= 0.0f) return false;

	// It's not just good, it's good enough
	return true;
}

// Returns true if two faces are joinable. Two faces are joinable if they are
// in the same tree, have the same material, same surface flags, and are co-planar.
bool pr::terrain::IsEquivalent(Face const* lhs, Face const* rhs)
{
	if ((lhs == 0)				&& (rhs == 0))				return true;
	if ((lhs == 0)				!= (rhs == 0))				return false;
	if (lhs->m_material_index	!= rhs->m_material_index)	return false;
	if (lhs->m_surface_flags	!= rhs->m_surface_flags)	return false;
	if (lhs->m_plane			!= rhs->m_plane)			return false;
	return true;
}

// Returns true if 'edge' lies between two "joinable" faces.
bool pr::terrain::IsBetweenJoinableFaces(Edge const& edge)
{
	if (edge.m_Lface == 0 || edge.m_Rface == 0) return false;
	if (edge.m_Lface->m_tree_id != edge.m_Rface->m_tree_id) return false;
	return IsEquivalent(edge.m_Lface, edge.m_Rface);
}

// Calculate the area of a face
float pr::terrain::CalculateArea(Face const& face)
{
	pr::v4 const& v0 = face.m_original_vertex[0];
	pr::v4 const& v1 = face.m_original_vertex[1];
	pr::v4 const& v2 = face.m_original_vertex[2];
	return Length3(Cross3(v2 - v1, v0 - v1)) * 0.5f;
}

// Calculate the normal for a face.
pr::v4 pr::terrain::CalculateNormal(Face const& face)
{
	pr::v4 const& v0 = face.m_original_vertex[0];
	pr::v4 const& v1 = face.m_original_vertex[1];
	pr::v4 const& v2 = face.m_original_vertex[2];
	pr::v4 normal = Cross3(v2 - v1, v0 - v1);
	normal /= Length3(normal); // The cross product is guaranteed to be non-zero in the TerrainExporter::AddFace method (IsValidFace)
	return normal;
}

// Calculate the plane for a face
Plane pr::terrain::CalculatePlane(Face const& face)
{
	pr::v4 plane = CalculateNormal(face);
	plane.w = -Dot3(plane, face.m_original_vertex[0]);
	return plane;
}

// Search for a vertex that is common between lhs and rhs
bool pr::terrain::ShareCommonVertex(Face const& lhs, Face const& rhs)
{
	for (int i = 0; i != 3; ++i)
		for (int j = 0; j != 3; ++j)
			if (lhs.m_index[i] == rhs.m_index[j])
				return true;
	return false;
}

// Search for an edge that is common between lhs and rhs
bool pr::terrain::ShareCommonEdge(Face const& lhs, Face const& rhs)
{
	return	((lhs.m_edges[0]->m_Lface == &lhs) && (lhs.m_edges[0]->m_Rface == &rhs)) || 
			((lhs.m_edges[0]->m_Rface == &lhs) && (lhs.m_edges[0]->m_Lface == &rhs)) || 
			((lhs.m_edges[1]->m_Lface == &lhs) && (lhs.m_edges[1]->m_Rface == &rhs)) || 
			((lhs.m_edges[1]->m_Rface == &lhs) && (lhs.m_edges[1]->m_Lface == &rhs)) || 
			((lhs.m_edges[2]->m_Lface == &lhs) && (lhs.m_edges[2]->m_Rface == &rhs)) || 
			((lhs.m_edges[2]->m_Rface == &lhs) && (lhs.m_edges[2]->m_Lface == &rhs));
}

// Return true if 'lhs' and 'rhs' are colinear branches
bool pr::terrain::IsColinear(BranchEx const& lhs, BranchEx const& rhs, EDim dimension)
{
	// All positions are quantised which means they all lie on a 3d grid with cell size 1.0 / PositionQuantisation.
	// Convert the 3 lines: lhs.start-lhs.end, lhs.start-rhs.start, lhs.start-rhs.end into grid positions
	// The ratio of the lengths along x, y, z should be the same for all lines if they are colinear.
	iv4 lhs_start	= iv4::make(lhs.m_edge->m_vertex0->m_position * PositionQuantisation);
	iv4 lhs_end		= iv4::make(lhs.m_edge->m_vertex1->m_position * PositionQuantisation);
	iv4 rhs_start	= iv4::make(rhs.m_edge->m_vertex0->m_position * PositionQuantisation);
	iv4 rhs_end		= iv4::make(rhs.m_edge->m_vertex1->m_position * PositionQuantisation);
	if (dimension == EDim_2d)
	{
		lhs_start	.y = 0;
		lhs_end		.y = 0;	
		rhs_start	.y = 0;
		rhs_end		.y = 0;
	}
	iv4 diff1 = lhs_end   - lhs_start;
	iv4 diff2 = rhs_start - lhs_start;
	iv4 diff3 = rhs_end   - lhs_start;

	if (!IsZero3(Cross3(diff1, diff2))) return false;
	if (!IsZero3(Cross3(diff2, diff3))) return false;
	if (!IsZero3(Cross3(diff3, diff1))) return false;
	return true;
}

// Returns true if lhs and rhs represent the same line with the same faces on each side
bool pr::terrain::IsRedundant(BranchEx const& lhs, BranchEx const& rhs)
{
	if (!IsColinear(lhs, rhs, EDim_3d)) return false;

	// If the branches are colinear they also need to have pointers to coplanar
	// faces with the same properties on each side (allowing for edge orientation)
	bool same_direction = Dot3(lhs.m_edge->Direction(), rhs.m_edge->Direction()) > 0.0f;
	
	Face const* lhs_Lface = lhs.m_edge->m_Lface;
	Face const* lhs_Rface = lhs.m_edge->m_Rface;
	Face const* rhs_Lface = (same_direction) ? (rhs.m_edge->m_Lface) : (rhs.m_edge->m_Rface);
	Face const* rhs_Rface = (same_direction) ? (rhs.m_edge->m_Rface) : (rhs.m_edge->m_Lface);

	return IsEquivalent(lhs_Lface, rhs_Lface) && IsEquivalent(lhs_Rface, rhs_Rface);
}

// Return true if two trees are exactly the same
bool pr::terrain::IsDegenerate(TreeEx const& lhs, TreeEx const& rhs)
{
	// Must have the same number of branches and leaves
	if (lhs.m_branch.size() != rhs.m_branch.size()) return false;
	if (lhs.m_leaf.size()	!= rhs.m_leaf.size()) return false;

	// Recursively navigate the tree checking for degeneracy
	return IsDegenerate(lhs.m_branch.front(), rhs.m_branch.front());
}

// Return true if two branches are exactly the same. Recursive
bool pr::terrain::IsDegenerate(BranchEx const& lhs, BranchEx const& rhs)
{
	// Branches with the same orientation
	if (lhs.m_branch.m_a == rhs.m_branch.m_a &&
		lhs.m_branch.m_b == rhs.m_branch.m_b &&
		lhs.m_branch.m_c == rhs.m_branch.m_c)
	{
		// Check the left sides
		if (lhs.m_Lbranch)
		{
			if (!rhs.m_Lbranch) return false;
			if (!IsDegenerate(*lhs.m_Lbranch,*rhs.m_Lbranch)) return false;
		}
		else
		{
			PR_ASSERT(PR_DBG_TERRAIN, lhs.m_Lleaf, "Each branch should refer to another branch or a leaf");
			if (!rhs.m_Lleaf) return false;
			if (!IsDegenerate(*lhs.m_Lleaf, *rhs.m_Lleaf)) return false;
		}

		// Check the right sides
		if (lhs.m_Rbranch)
		{
			if (!rhs.m_Rbranch) return false;
			if (!IsDegenerate(*lhs.m_Rbranch, *rhs.m_Rbranch)) return false;
		}
		else
		{
			PR_ASSERT(PR_DBG_TERRAIN, lhs.m_Rleaf, "Each branch should refer to another branch or a leaf");
			if (!rhs.m_Rleaf) return false;
			if (!IsDegenerate(*lhs.m_Rleaf, *rhs.m_Rleaf)) return false;
		}
		return true;
	}
	
	// Branches with opposite orientation
	if (lhs.m_branch.m_a == -rhs.m_branch.m_a &&
		lhs.m_branch.m_b == -rhs.m_branch.m_b &&
		lhs.m_branch.m_c == -rhs.m_branch.m_c)
	{
		// Check the left-right sides
		if (lhs.m_Lbranch)
		{
			if (!rhs.m_Rbranch) return false;
			if (!IsDegenerate(*lhs.m_Lbranch, *rhs.m_Rbranch)) return false;
		}
		else
		{
			PR_ASSERT(PR_DBG_TERRAIN, lhs.m_Lleaf, "Each branch should refer to another branch or a leaf");
			if (!rhs.m_Rleaf) return false;
			if (!IsDegenerate(*lhs.m_Lleaf, *rhs.m_Rleaf)) return false;
		}

		// Check the right-left sides
		if (lhs.m_Rbranch)
		{
			if (!rhs.m_Lbranch) return false;
			if (!IsDegenerate(*lhs.m_Rbranch, *rhs.m_Lbranch)) return false;
		}
		else
		{
			PR_ASSERT(PR_DBG_TERRAIN, lhs.m_Rleaf, "Each branch should refer to another branch or a leaf");
			if (!rhs.m_Lleaf) return false;
			if (!IsDegenerate(*lhs.m_Rleaf, *rhs.m_Lleaf)) return false;
		}
		return true;
	}

	return false;
}

// Return true if two leaves are exactly the same
bool pr::terrain::IsDegenerate(LeafEx const& lhs, LeafEx const& rhs)
{
	return IsEquivalent(lhs.m_face, rhs.m_face);
}
