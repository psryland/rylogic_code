//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_GEOMETRY_FUNCTIONS_H
#define PR_MATHS_GEOMETRY_FUNCTIONS_H

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/vector4.h"
#include "pr/maths/boundingbox.h"

namespace pr
{
	float Distance_PointToPlane(v4 const& point, v4 const& a, v4 const& b, v4 const& c);
	float Distance_PointToPlane(v4 const& point, Plane const& plane);
	float Distance_PointToInfiniteLine(v4 const& point, v4 const& start, v4 const& end);
	float Distance_InfiniteLineToInfiniteLine(v4 const& s0, v4 const& line0, v4 const& s1, v4 const& line1);
	float DistanceSq_PointToInfiniteLine(v4 const& point, v4 const& start, v4 const& line);
	float DistanceSq_PointToLineSegment(v4 const& point, const Line3& line);
	float DistanceSq_PointToBoundingBox(v4 const& point, BBox const& bbox);
	float Volume_Triangle(v4 const& a, v4 const& b, v4 const& c);
	float Volume_Tetrahedron(v4 const& a, v4 const& b, v4 const& c, v4 const& d);
	bool  PointInFrontOfPlane(v4 const& point, v4 const& a, v4 const& b, v4 const& c);
	v4    ClosestPoint_PointToPlane(v4 const& point, Plane const& plane);
	v4    ClosestPoint_PointToPlane(v4 const& point, v4 const& a, v4 const& b, v4 const& c);
	v4    ClosestPoint_PointToInfiniteLine(v4 const& point, v4 const& start, v4 const& end, float& t);
	v4    ClosestPoint_PointToInfiniteLine(v4 const& point, v4 const& start, v4 const& end);
	v4    ClosestPoint_PointToInfiniteLine(v4 const& point, const Line3& line, float& t);
	v4    ClosestPoint_PointToInfiniteLine(v4 const& point, const Line3& line);
	v4    ClosestPoint_PointToLineSegment(v4 const& point, v4 const& start, v4 const& end, float& t);
	v4    ClosestPoint_PointToLineSegment(v4 const& point, v4 const& start, v4 const& end);
	v4    ClosestPoint_PointToLineSegment(v4 const& point, const Line3& line, float& t);
	v4    ClosestPoint_PointToLineSegment(v4 const& point, const Line3& line);
	v4    ClosestPoint_PointToBoundingBox(v4 const& point, BBox const& bbox);
	v2    ClosestPoint_PointToEllipse(float x, float y, float major, float minor);
	v4    ClosestPoint_PointToTriangle(v4 const& point, v4 const& a, v4 const& b, v4 const& c, v4& barycentric);
	v4    ClosestPoint_PointToTriangle(v4 const& point, v4 const& a, v4 const& b, v4 const& c);
	v4    ClosestPoint_PointToTriangle(v4 const& point, const v4* tri, v4& barycentric);
	v4    ClosestPoint_PointToTriangle(v4 const& point, const v4* tri);
	v4    ClosestPoint_PointToTetrahedron(v4 const& point, v4 const& a, v4 const& b, v4 const& c, v4 const& d, v4& barycentric);
	v4    ClosestPoint_PointToTetrahedron(v4 const& point, v4 const& a, v4 const& b, v4 const& c, v4 const& d);
	v4    ClosestPoint_PointToTetrahedron(v4 const& point, const v4* tetra, v4& barycentric);
	v4    ClosestPoint_PointToTetrahedron(v4 const& point, const v4* tetra);
	void  ClosestPoint_LineSegmentToLineSegment(v4 const& s0, v4 const& e0, v4 const& s1, v4 const& e1, float& t0, float& t1);
	void  ClosestPoint_LineSegmentToLineSegmentFast(v4 const& s0, v4 const& e0, v4 const& s1, v4 const& e1, float& t0, float& t1);
	void  ClosestPoint_LineSegmentToLineSegment(v4 const& s0, v4 const& e0, v4 const& s1, v4 const& e1, v4& pt0, v4& pt1);
	void  ClosestPoint_LineSegmentToLineSegment(v4 const& s0, v4 const& e0, v4 const& s1, v4 const& e1, v4& pt0, v4& pt1, float& t0, float& t1);
	void  ClosestPoint_LineSegmentToLineSegment(v4 const& s0, v4 const& e0, v4 const& s1, v4 const& e1, float& dist_sq);
	void  ClosestPoint_LineSegmentToInfiniteLine(v4 const& s0, v4 const& e0, v4 const& s1, v4 const& line1, float& t0, float& t1);
	void  ClosestPoint_LineSegmentToInfiniteLine(v4 const& s0, v4 const& e0, v4 const& s1, v4 const& line1, float& t0, float& t1, float& dist_sq);
	void  ClosestPoint_InfiniteLineToInfiniteLine(v4 const& s0, v4 const& line0, v4 const& s1, v4 const& line1, float& t0, float& t1);
	v4    BaryPoint(v4 const& a, v4 const& b, v4 const& c, v4 const& bary);
	v4    BaryCentric(v4 const& point, v4 const& a, v4 const& b, v4 const& c);
	bool  PointWithinTriangle(v4 const& point, v4 const& a, v4 const& b, v4 const& c, float tol);
	bool  PointWithinTriangle2(v4 const& point, v4 const& a, v4 const& b, v4 const& c, float tol);
	bool  PointWithinTriangle(v4 const& point, v4 const& a, v4 const& b, v4 const& c, v4& pt);
	bool  PointWithinTetrahedron(v4 const& point, v4 const& a, v4 const& b, v4 const& c, v4 const& d);
	bool  Intersect2D_InfiniteLineToInfiniteLine(v2 const& b, v2 const& a, v2 const& d, v2 const& c, v2& intersect);
	bool  Intersect_LineToTriangle(v4 const& s, v4 const& e, v4 const& a, v4 const& b, v4 const& c, float* t = 0, v4* bary = 0, float* f2b = 0, float tmin = -pr::maths::float_max, float tmax = pr::maths::float_max);
	bool  Intersect_LineToTriangle(v4 const& s, v4 const& e, v4 const& a, v4 const& b, v4 const& c, float& front_to_back, v4& bary);
	bool  Intersect_LineSegmentToBoundingBox(v4 const& lineS, v4 const& lineE, BBox const& bbox);
	bool  Intersect_LineToPlane(Plane const& plane, v4 const& s, v4 const& e, float* t = 0, float tmin = -pr::maths::float_max, float tmax = pr::maths::float_max);
	bool  Clip_LineSegmentToPlane(Plane const& plane, v4 const& lineS, v4 const& lineE, float& t0, float& t1);
	bool  Clip_LineSegmentToPlane(Plane const& plane, v4& lineS, v4& lineE);
	bool  Clip_LineSegmentToBoundingBox(v4 const& point, v4 const& line, BBox const& bbox, float& t0, float& t1);
	bool  Clip(Plane const& plane, Line3& line);
	bool  Clip(BBox const& bbox, Line3& line);
	bool  ClipToSlab(v4 const& norm, float dist1, float dist2, v4& s, v4& e);
	float CircumRadius(v4 const& a, v4 const& b, v4 const& c, v4& centre);
}

#endif
