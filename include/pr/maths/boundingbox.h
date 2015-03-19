//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
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

	struct alignas(16) BBox
	{
		v4 m_centre;
		v4 m_radius;

		static BBox make(v4 const& centre, v4 const& radius);
		static BBox makeLU(v4 const& lower, v4 const& upper);
		BBox&       reset();
		BBox&       unit();
		BBox&       set(v4 const& centre, v4 const& radius);
		bool        IsValid() const;
		v4          Lower() const;
		v4          Upper() const;
		float       Lower(int axis) const;
		float       Upper(int axis) const;
		float       SizeX() const;
		float       SizeY() const;
		float       SizeZ() const;
		v4 const&   Centre() const;
		v4 const&   Radius() const;
		float       DiametreSq() const;
		float       Diametre() const;
	};

	static BBox const BBoxUnit  = {{0.0f,  0.0f,  0.0f, 1.0f}, {0.5f, 0.5f, 0.5f, 0.0f}};
	static BBox const BBoxReset = {{0.0f,  0.0f,  0.0f, 1.0f}, {-1.0f, -1.0f, -1.0f, 0.0f}};

	// Assignment operators
	BBox& operator += (BBox& lhs, v4 const& offset);
	BBox& operator -= (BBox& lhs, v4 const& offset);
	BBox& operator *= (BBox& lhs, float s);
	BBox& operator /= (BBox& lhs, float s);

	// Binary operators
	BBox operator + (BBox const& bb, v4 const& offset);
	BBox operator - (BBox const& bb, v4 const& offset);
	BBox operator * (const m4x4& m, BBox const& bb);

	// Equality operators
	bool operator == (BBox const& lhs, BBox const& rhs);
	bool operator != (BBox const& lhs, BBox const& rhs);
	bool operator <  (BBox const& lhs, BBox const& rhs);
	bool operator >  (BBox const& lhs, BBox const& rhs);
	bool operator <= (BBox const& lhs, BBox const& rhs);
	bool operator >= (BBox const& lhs, BBox const& rhs);

	template <typename VertCont> BBox BBoxMake(VertCont const& verts);
	template <typename Vert> BBox BBoxMake(std::initializer_list<Vert>&& verts);

	// Functions
	float   Volume(BBox const& bbox);
	Plane   GetPlane(BBox const& bbox, EBBoxPlane side);
	v4      GetCorner(BBox const& bbox, uint corner);
	BSphere GetBoundingSphere(BBox const& bbox);
	BBox&   Encompass(BBox& bbox, v4 const& point);
	BBox    Encompass(BBox const& bbox, v4 const& point);
	BBox&   Encompass(BBox& lhs, BBox const& rhs);
	BBox    Encompass(BBox const& lhs, BBox const& rhs);
	bool    IsWithin(BBox const& bbox, v4 const& point, float tol);
	bool    IsWithin(BBox const& bbox, BBox const& test);
}

#endif
