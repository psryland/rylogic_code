//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_ORIENTED_BOX_H
#define PR_MATHS_ORIENTED_BOX_H

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/scalar.h"
#include "pr/maths/vector4.h"
#include "pr/maths/matrix4x4.h"
#include "pr/maths/boundingsphere.h"
#include "pr/maths/geometryfunctions.h"

namespace pr
{
	struct OrientedBox
	{
		enum { Point = 1 << 0, Edge = 1 << 1, Face = 1 << 2, Bits = 1 << 3, Mask = Bits - 1 };
		m4x4 m_box_to_world;
		v4   m_radius;

		OrientedBox& set(v4 const& centre, v4 const& radii, m3x4 const& ori)    { m_box_to_world.set(ori, centre); m_radius = radii; return *this; }
		float        SizeX() const                                              { return 2.0f * m_radius.x; }
		float        SizeY() const                                              { return 2.0f * m_radius.y; }
		float        SizeZ() const                                              { return 2.0f * m_radius.z; }
		v4 const&    Centre() const                                             { return m_box_to_world.pos; }
		float        DiametreSq() const                                         { return 4.0f * Length3Sq(m_radius); }
		float        Diametre() const                                           { return Sqrt(DiametreSq()); }

		static OrientedBox  make(v4 const& centre, v4 const& radii, m3x4 const& ori) { OrientedBox bbox; return bbox.set(centre, radii, ori); }
	};

	OrientedBox const OBoxZero  = {m4x4Identity, v4Zero};
	OrientedBox const OBoxUnit  = {m4x4Identity, {0.5f, 0.5f, 0.5f, 1.0f}};
	OrientedBox const OBoxReset = {m4x4Identity, v4Zero};

	// Assignment operators
	inline OrientedBox& operator += (OrientedBox& lhs, v4 const& offset) { lhs.m_box_to_world.pos += offset; return lhs; }
	inline OrientedBox& operator -= (OrientedBox& lhs, v4 const& offset) { lhs.m_box_to_world.pos -= offset; return lhs; }

	// Binary operators
	inline OrientedBox operator + (OrientedBox const& lhs, v4 const& offset) { OrientedBox ob = lhs; return ob += offset; }
	inline OrientedBox operator - (OrientedBox const& lhs, v4 const& offset) { OrientedBox ob = lhs; return ob -= offset; }
	OrientedBox operator * (m4x4 const& m, OrientedBox const& ob);

	// Equality operators
	inline bool operator == (OrientedBox const& lhs, OrientedBox const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
	inline bool operator != (OrientedBox const& lhs, OrientedBox const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
	inline bool operator <  (OrientedBox const& lhs, OrientedBox const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
	inline bool operator >  (OrientedBox const& lhs, OrientedBox const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
	inline bool operator <= (OrientedBox const& lhs, OrientedBox const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
	inline bool operator >= (OrientedBox const& lhs, OrientedBox const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }

	// Functions
	float           Volume(OrientedBox const& ob);
	m4x4 const&     Getm4x4(OrientedBox const& ob);
	m4x4&           Getm4x4(OrientedBox& ob);
	BSphere  GetBoundingSphere(OrientedBox const& ob);
	v4              SupportVertex(OrientedBox const& ob, v4 const& direction, int& feature_type);
	v4              SupportVertex(OrientedBox const& ob, v4 const& direction);
	void            SupportFeature(OrientedBox const& ob, v4 const& direction, v4* points, int& feature_type);
	void            SupportFeature(OrientedBox const& ob, v4 const& direction, v4* points);
	bool            IsIntersection(OrientedBox const& lhs, OrientedBox const& rhs);
	bool            IsIntersection(OrientedBox const& lhs, OrientedBox const& rhs, v4& axis, float& penetration);
	bool            IsIntersection(OrientedBox const& lhs, OrientedBox const& rhs, v4& axis, float& penetration, v4& pointA, v4& pointB);
}

#endif
