//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_BOUNDING_SPHERE_H
#define PR_MATHS_BOUNDING_SPHERE_H

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/vector4.h"
#include "pr/maths/matrix4x4.h"

namespace pr
{
	struct BoundingSphere
	{
		v4 m_ctr_rad; // x,y,z = position, 'w' = radius

		static BoundingSphere make(v4 const& centre, float radius);
		BoundingSphere&       set(v4 const& centre, float radius);
		BoundingSphere&       zero();
		BoundingSphere&       unit();
		BoundingSphere&       reset();
		bool                  IsValid() const;
		v4                    Centre() const;
		float                 Radius() const;
		float                 RadiusSq() const;
		float                 Diametre() const;
		float                 DiametreSq() const;
	};

	BoundingSphere const BSphereZero  = {v4Zero};
	BoundingSphere const BSphereUnit  = {v4Origin};
	BoundingSphere const BSphereReset = {-v4Origin};

	// Assignment operators
	BoundingSphere& operator += (BoundingSphere& lhs, v4 const& offset);
	BoundingSphere& operator -= (BoundingSphere& lhs, v4 const& offset);
	BoundingSphere& operator *= (BoundingSphere& lhs, float s);
	BoundingSphere& operator /= (BoundingSphere& lhs, float s);

	// Binary operators
	BoundingSphere operator + (BoundingSphere const& bsph, v4 const& offset);
	BoundingSphere operator - (BoundingSphere const& bsph, v4 const& offset);
	BoundingSphere operator * (BoundingSphere const& bsph, float s);
	BoundingSphere operator * (float s, BoundingSphere const& bsph);
	BoundingSphere operator * (m4x4 const& m, BoundingSphere const& bsph);

	// Equality operators
	bool operator == (BoundingSphere const& lhs, BoundingSphere const& rhs);
    bool operator != (BoundingSphere const& lhs, BoundingSphere const& rhs);
	bool operator <  (BoundingSphere const& lhs, BoundingSphere const& rhs);
	bool operator >  (BoundingSphere const& lhs, BoundingSphere const& rhs);
	bool operator <= (BoundingSphere const& lhs, BoundingSphere const& rhs);
	bool operator >= (BoundingSphere const& lhs, BoundingSphere const& rhs);

	// Functions
	float           Volume(BoundingSphere const& bsph);
	BoundingSphere& Encompass(BoundingSphere& bsphere, v4 const& point);
	BoundingSphere  Encompass(BoundingSphere const& bsphere, v4 const& point);
	BoundingSphere& Encompass(BoundingSphere& lhs, BoundingSphere const& rhs);
	BoundingSphere  Encompass(BoundingSphere const& lhs, BoundingSphere const& rhs);
	BoundingSphere& EncompassLoose(BoundingSphere& bsphere, v4 const& point);
	BoundingSphere  EncompassLoose(BoundingSphere const& bsphere, v4 const& point);
	BoundingSphere& EncompassLoose(BoundingSphere& lhs, BoundingSphere const& rhs);
	BoundingSphere  EncompassLoose(BoundingSphere const& lhs, BoundingSphere const& rhs);
	bool            IsWithin(BoundingSphere const& bsphere, v4 const& point);
	bool            IsWithin(BoundingSphere const& bsphere, BoundingSphere const& test);
	bool            IsIntersection(BoundingSphere const& lhs, BoundingSphere const& rhs);
}

#endif
