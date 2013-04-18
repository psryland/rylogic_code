//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_BOUNDING_BOX_IMPL_H
#define PR_MATHS_BOUNDING_BOX_IMPL_H

#include "pr/maths/boundingbox.h"

namespace pr
{
	// Type methods
	inline BoundingBox  BoundingBox::make(v4 const& centre, v4 const& radius) { BoundingBox bbox = {centre, radius}; return bbox; }
	inline BoundingBox& BoundingBox::reset()                                  { m_centre = pr::v4Origin; m_radius.set(-1.0f, -1.0f, -1.0f, 0.0f); return *this; }
	inline BoundingBox& BoundingBox::unit()                                   { m_centre = pr::v4Origin; m_radius.set(0.5f, 0.5f, 0.5f, 0.0f); return *this; }
	inline BoundingBox& BoundingBox::set(v4 const& centre, v4 const& radius)  { m_centre = centre; m_radius = radius; return *this; }
	inline bool         BoundingBox::IsValid() const                          { return Volume(*this) >= 0.0f; }
	inline v4           BoundingBox::Lower() const                            { return m_centre - m_radius; }
	inline v4           BoundingBox::Upper() const                            { return m_centre + m_radius; }
	inline float        BoundingBox::Lower(int axis) const                    { return m_centre[axis] - m_radius[axis]; }
	inline float        BoundingBox::Upper(int axis) const                    { return m_centre[axis] + m_radius[axis]; }
	inline float        BoundingBox::SizeX() const                            { return 2.0f * m_radius.x; }
	inline float        BoundingBox::SizeY() const                            { return 2.0f * m_radius.y; }
	inline float        BoundingBox::SizeZ() const                            { return 2.0f * m_radius.z; }
	inline v4 const&    BoundingBox::Centre() const                           { return m_centre; }
	inline v4 const&    BoundingBox::Radius() const                           { return m_radius; }
	inline float        BoundingBox::DiametreSq() const                       { return 4.0f * Length3Sq(m_radius); }
	inline float        BoundingBox::Diametre() const                         { return Sqrt(DiametreSq()); }
	
	// Assignment operators
	inline BoundingBox& operator += (BoundingBox& lhs, v4 const& offset)      { lhs.m_centre += offset; return lhs; }
	inline BoundingBox& operator -= (BoundingBox& lhs, v4 const& offset)      { lhs.m_centre -= offset; return lhs; }
	inline BoundingBox& operator *= (BoundingBox& lhs, float s)               { lhs.m_radius *= s; return lhs; }
	inline BoundingBox& operator /= (BoundingBox& lhs, float s)               { lhs *= (1.0f / s); return lhs; }

	// Binary operators
	inline BoundingBox operator + (BoundingBox const& lhs, v4 const& offset)  { BoundingBox bb = lhs; return bb += offset; }
	inline BoundingBox operator - (BoundingBox const& lhs, v4 const& offset)  { BoundingBox bb = lhs; return bb -= offset; }
	inline BoundingBox operator * (m4x4 const& m, BoundingBox const& rhs)
	{
		PR_ASSERT(PR_DBG_MATHS, rhs.IsValid(), "Transforming an invalid bounding box");
		BoundingBox bb = BoundingBox::make(m.pos, v4Zero);
		m4x4 mat = GetTranspose3x3(m);
		for (int i = 0; i != 3; ++i)
		{
			bb.m_centre[i] += Dot4(    mat[i] , rhs.m_centre);
			bb.m_radius[i] += Dot4(Abs(mat[i]), rhs.m_radius);
		}
		return bb;
	}
	
	// Equality operators
	inline bool	operator == (BoundingBox const& lhs, BoundingBox const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
	inline bool	operator != (BoundingBox const& lhs, BoundingBox const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
	inline bool operator <  (BoundingBox const& lhs, BoundingBox const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
	inline bool operator >  (BoundingBox const& lhs, BoundingBox const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
	inline bool operator <= (BoundingBox const& lhs, BoundingBox const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
	inline bool operator >= (BoundingBox const& lhs, BoundingBox const& rhs)  { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }
	
	// Functions
	inline float Volume(BoundingBox const& bbox)
	{
		return bbox.SizeX() * bbox.SizeY() * bbox.SizeZ();
	}
	
	// Return a plane corresponding to a side of the bounding box
	// Returns inward facing planes
	inline Plane GetPlane(BoundingBox const& bbox, EBBoxPlane side)
	{
		switch (side)
		{
		default:
		case EBBoxPlane_NumberOf: PR_ASSERT(PR_DBG_MATHS, false, "Unknown side index"); return Plane();
		case EBBoxPlane_Lx: return plane::make( 1.0f,  0.0f,  0.0f, bbox.m_centre.x + bbox.m_radius.x);
		case EBBoxPlane_Ux: return plane::make(-1.0f,  0.0f,  0.0f, bbox.m_centre.x + bbox.m_radius.x);
		case EBBoxPlane_Ly: return plane::make( 0.0f,  1.0f,  0.0f, bbox.m_centre.y + bbox.m_radius.y);
		case EBBoxPlane_Uy: return plane::make( 0.0f, -1.0f,  0.0f, bbox.m_centre.y + bbox.m_radius.y);
		case EBBoxPlane_Lz: return plane::make( 0.0f,  0.0f,  1.0f, bbox.m_centre.z + bbox.m_radius.z);
		case EBBoxPlane_Uz: return plane::make( 0.0f,  0.0f, -1.0f, bbox.m_centre.z + bbox.m_radius.z);
		}
	}
	
	// Return a corner of the bounding box
	inline v4 GetCorner(BoundingBox const& bbox, uint corner)
	{
		PR_ASSERT(PR_DBG_MATHS, corner < 8, "Invalid corner index");
		int x = ((corner >> 0) & 0x1) * 2 - 1;
		int y = ((corner >> 1) & 0x1) * 2 - 1;
		int z = ((corner >> 2) & 0x1) * 2 - 1;
		return v4::make(bbox.m_centre.x + x*bbox.m_radius.x, bbox.m_centre.y + y*bbox.m_radius.y, bbox.m_centre.z + z*bbox.m_radius.z, 1.0f);
	}
	
	// Return a bounding sphere that bounds the bounding box
	inline BoundingSphere GetBoundingSphere(BoundingBox const& bbox)
	{
		return BoundingSphere::make(bbox.m_centre, Length3(bbox.m_radius));
	}
	
	// Encompase 'point' within 'bbox'.
	inline BoundingBox&	Encompase(BoundingBox& bbox, v4 const& point)
	{
		for (int i = 0; i != 3; ++i)
		{
			if (bbox.m_radius[i] < 0.0f)
			{
				bbox.m_centre[i] = point[i];
				bbox.m_radius[i] = 0.0f;
			}
			else
			{
				float signed_dist = point[i] - bbox.m_centre[i];
				float length      = Abs(signed_dist);
				if (length > bbox.m_radius[i])
				{
					float new_radius = (length + bbox.m_radius[i]) / 2.0f;
					bbox.m_centre[i] += signed_dist * (new_radius - bbox.m_radius[i]) / length;
					bbox.m_radius[i] = new_radius;
				}
			}
		}
		return bbox;
	}
	
	// Encompase 'point' within 'bbox'.
	inline BoundingBox Encompase(BoundingBox const& bbox, v4 const& point)
	{
		BoundingBox bb = bbox;
		return Encompase(bb, point);
	}
	
	// Encompase 'rhs' in 'lhs'
	inline BoundingBox& Encompase(BoundingBox& lhs, BoundingBox const& rhs)
	{
		PR_ASSERT(PR_DBG_MATHS, rhs.IsValid(), "Encompasing an invalid bounding box");
		Encompase(lhs, rhs.m_centre + rhs.m_radius);
		Encompase(lhs, rhs.m_centre - rhs.m_radius);
		return lhs;
	}
	
	// Encompase 'rhs' in 'lhs'
	inline BoundingBox&	Encompase(BoundingBox& lhs, BoundingSphere const& rhs)
	{
		PR_ASSERT(PR_DBG_MATHS, rhs.IsValid(), "Encompasing an invalid bounding sphere");
		pr::v4 radius = pr::v4::make(rhs.Radius(), rhs.Radius(), rhs.Radius(), 0);
		Encompase(lhs, rhs.Centre() + radius);
		Encompase(lhs, rhs.Centre() - radius);
		return lhs;
	}
	
	// Encompase 'rhs' in 'lhs'
	inline BoundingBox Encompase(BoundingBox const& lhs, BoundingBox const& rhs)
	{
		BoundingBox bb = lhs;
		return Encompase(bb, rhs);
	}
	
	// Returns true if 'point' is within the bounding volume
	inline bool IsWithin(BoundingBox const& bbox, v4 const& point, float tol)
	{
		return	Abs(point.x - bbox.m_centre.x) <= bbox.m_radius.x + tol &&
				Abs(point.y - bbox.m_centre.y) <= bbox.m_radius.y + tol &&
				Abs(point.z - bbox.m_centre.z) <= bbox.m_radius.z + tol;
	}
	inline bool IsWithin(BoundingBox const& bbox, BoundingBox const& test)
	{
		return	Abs(test.m_centre.x - bbox.m_centre.x) <= (bbox.m_radius.x - test.m_radius.x) &&
				Abs(test.m_centre.y - bbox.m_centre.y) <= (bbox.m_radius.y - test.m_radius.y) &&
				Abs(test.m_centre.z - bbox.m_centre.z) <= (bbox.m_radius.z - test.m_radius.z);
	}

	// Returns true if 'line' intersects 'bbox'
	inline bool IsIntersection(BoundingBox const& bbox, Line3 const& line)
	{
		Line3 l = line;
		Clip(bbox, l);
		return Length3(l) > 0.0f;
	}

	// Returns true if 'plane' intersects 'bbox'
	inline bool IsIntersection(BoundingBox const& bbox, Plane const& plane)
	{
		// If the eight corners of the box are on the same side of the plane then there's no intersect
		bool first_side = Dot4(GetCorner(bbox, 0), plane) > 0.0f;
		for (uint corner = 1; corner != 8; ++corner)
		{
			bool this_side = Dot4(GetCorner(bbox, corner), plane) > 0.0f;
			if (this_side != first_side) return false;
		}
		return true;
	}

	// Returns true if 'lhs' and 'rhs' intersect
	inline bool IsIntersection(BoundingBox const& lhs, BoundingBox const& rhs)
	{
		return	Abs(lhs.m_centre.x - rhs.m_centre.x) <= (lhs.m_radius.x + rhs.m_radius.x) &&
				Abs(lhs.m_centre.y - rhs.m_centre.y) <= (lhs.m_radius.y + rhs.m_radius.y) &&
				Abs(lhs.m_centre.z - rhs.m_centre.z) <= (lhs.m_radius.z + rhs.m_radius.z);
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_maths_boundingbox)
		{
		
		}
	}
}
#endif

#endif
