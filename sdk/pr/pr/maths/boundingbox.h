//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_BOUNDING_BOX_H
#define PR_MATHS_BOUNDING_BOX_H

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/vector4.h"
#include "pr/maths/matrix4x4.h"
#include "pr/maths/plane.h"
#include "pr/maths/boundingsphere.h"
#include "pr/maths/line3.h"
#include "pr/maths/geometryfunctions.h"

namespace pr
{
	enum EBBoxPlane
	{
		EBBoxPlane_Lx = 0,
		EBBoxPlane_Ux = 1,
		EBBoxPlane_Ly = 2,
		EBBoxPlane_Uy = 3,
		EBBoxPlane_Lz = 4,
		EBBoxPlane_Uz = 5,
		EBBoxPlane_NumberOf = 6
	};

	struct BoundingBox
	{
		v4 m_centre;
		v4 m_radius;

		static BoundingBox make(v4 const& centre, v4 const& radius);
		BoundingBox&       reset();
		BoundingBox&       unit();
		BoundingBox&       set(v4 const& centre, v4 const& radius);
		bool               IsValid() const;
		v4                 Lower() const;
		v4                 Upper() const;
		float              Lower(int axis) const;
		float              Upper(int axis) const;
		float              SizeX() const;
		float              SizeY() const;
		float              SizeZ() const;
		v4 const&          Centre() const;
		v4 const&          Radius() const;
		float              DiametreSq() const;
		float              Diametre() const;
	};

	BoundingBox const BBoxUnit  = {{0.0f,  0.0f,  0.0f, 1.0f}, {0.5f, 0.5f, 0.5f, 0.0f}};
	BoundingBox const BBoxReset = {{0.0f,  0.0f,  0.0f, 1.0f}, {-1.0f, -1.0f, -1.0f, 0.0f}};

	// Assignment operators
	BoundingBox& operator += (BoundingBox& lhs, v4 const& offset);
	BoundingBox& operator -= (BoundingBox& lhs, v4 const& offset);
	BoundingBox& operator *= (BoundingBox& lhs, float s);
	BoundingBox& operator /= (BoundingBox& lhs, float s);

	// Binary operators
	BoundingBox operator + (BoundingBox const& bb, v4 const& offset);
	BoundingBox operator - (BoundingBox const& bb, v4 const& offset);
	BoundingBox operator * (const m4x4& m, BoundingBox const& bb);

	// Equality operators
	bool operator == (BoundingBox const& lhs, BoundingBox const& rhs);
	bool operator != (BoundingBox const& lhs, BoundingBox const& rhs);
	bool operator <  (BoundingBox const& lhs, BoundingBox const& rhs);
	bool operator >  (BoundingBox const& lhs, BoundingBox const& rhs);
	bool operator <= (BoundingBox const& lhs, BoundingBox const& rhs);
	bool operator >= (BoundingBox const& lhs, BoundingBox const& rhs);

	// Functions
	float			Volume(BoundingBox const& bbox);
	Plane			GetPlane(BoundingBox const& bbox, EBBoxPlane side);
	v4				GetCorner(BoundingBox const& bbox, uint corner);
	BoundingSphere	GetBoundingSphere(BoundingBox const& bbox);
	BoundingBox&	Encompase(BoundingBox& bbox, v4 const& point);
	BoundingBox		Encompase(BoundingBox const& bbox, v4 const& point);
	BoundingBox&	Encompase(BoundingBox& lhs, BoundingBox const& rhs);
	BoundingBox		Encompase(BoundingBox const& lhs, BoundingBox const& rhs);
	bool			IsWithin(BoundingBox const& bbox, v4 const& point, float tol);
	bool			IsWithin(BoundingBox const& bbox, BoundingBox const& test);
	bool			IsIntersection(BoundingBox const& bbox, Line3 const& line);
	bool			IsIntersection(BoundingBox const& bbox, Plane const& plane);
	bool			IsIntersection(BoundingBox const& lhs, BoundingBox const& rhs);
}

#endif
