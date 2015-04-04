//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************

#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/scalar.h"
#include "pr/maths/vector4.h"
#include "pr/maths/matrix4x4.h"
#include "pr/maths/boundingsphere.h"

namespace pr
{
	struct OBox
	{
		enum { Point = 1 << 0, Edge = 1 << 1, Face = 1 << 2, Bits = 1 << 3, Mask = Bits - 1 };
		m4x4 m_box_to_world;
		v4   m_radius;

		OBox& set(v4 const& centre, v4 const& radii, m3x4 const& ori) { m_box_to_world.set(ori, centre); m_radius = radii; return *this; }
		float        SizeX() const                                    { return 2.0f * m_radius.x; }
		float        SizeY() const                                    { return 2.0f * m_radius.y; }
		float        SizeZ() const                                    { return 2.0f * m_radius.z; }
		v4 const&    Centre() const                                   { return m_box_to_world.pos; }
		float        DiametreSq() const                               { return 4.0f * Length3Sq(m_radius); }
		float        Diametre() const                                 { return Sqrt(DiametreSq()); }

		static OBox  make(v4 const& centre, v4 const& radii, m3x4 const& ori) { OBox bbox; return bbox.set(centre, radii, ori); }
	};

	static OBox const OBoxZero  = {m4x4Identity, v4Zero};
	static OBox const OBoxUnit  = {m4x4Identity, {0.5f, 0.5f, 0.5f, 1.0f}};
	static OBox const OBoxReset = {m4x4Identity, v4Zero};

	// Assignment operators
	inline OBox& operator += (OBox& lhs, v4 const& offset) { lhs.m_box_to_world.pos += offset; return lhs; }
	inline OBox& operator -= (OBox& lhs, v4 const& offset) { lhs.m_box_to_world.pos -= offset; return lhs; }

	// Binary operators
	inline OBox operator + (OBox const& lhs, v4 const& offset) { OBox ob = lhs; return ob += offset; }
	inline OBox operator - (OBox const& lhs, v4 const& offset) { OBox ob = lhs; return ob -= offset; }
	OBox operator * (m4x4 const& m, OBox const& ob);

	// Equality operators
	inline bool operator == (OBox const& lhs, OBox const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
	inline bool operator != (OBox const& lhs, OBox const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
	inline bool operator <  (OBox const& lhs, OBox const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
	inline bool operator >  (OBox const& lhs, OBox const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
	inline bool operator <= (OBox const& lhs, OBox const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
	inline bool operator >= (OBox const& lhs, OBox const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }

	// Functions
	float       Volume(OBox const& ob);
	m4x4 const& Getm4x4(OBox const& ob);
	m4x4&       Getm4x4(OBox& ob);
	BSphere     GetBoundingSphere(OBox const& ob);
}
