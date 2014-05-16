//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
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
	struct BSphere
	{
		v4 m_ctr_rad; // x,y,z = position, 'w' = radius

		static BSphere make(v4 const& centre, float radius);
		BSphere&       set(v4 const& centre, float radius);
		BSphere&       zero();
		BSphere&       unit();
		BSphere&       reset();
		bool           IsValid() const;
		v4             Centre() const;
		float          Radius() const;
		float          RadiusSq() const;
		float          Diametre() const;
		float          DiametreSq() const;
	};

	static BSphere const BSphereZero  = {v4Zero};
	static BSphere const BSphereUnit  = {v4Origin};
	static BSphere const BSphereReset = {-v4Origin};

	// Assignment operators
	BSphere& operator += (BSphere& lhs, v4 const& offset);
	BSphere& operator -= (BSphere& lhs, v4 const& offset);
	BSphere& operator *= (BSphere& lhs, float s);
	BSphere& operator /= (BSphere& lhs, float s);

	// Binary operators
	BSphere operator + (BSphere const& bsph, v4 const& offset);
	BSphere operator - (BSphere const& bsph, v4 const& offset);
	BSphere operator * (BSphere const& bsph, float s);
	BSphere operator * (float s, BSphere const& bsph);
	BSphere operator * (m4x4 const& m, BSphere const& bsph);

	// Equality operators
	bool operator == (BSphere const& lhs, BSphere const& rhs);
    bool operator != (BSphere const& lhs, BSphere const& rhs);
	bool operator <  (BSphere const& lhs, BSphere const& rhs);
	bool operator >  (BSphere const& lhs, BSphere const& rhs);
	bool operator <= (BSphere const& lhs, BSphere const& rhs);
	bool operator >= (BSphere const& lhs, BSphere const& rhs);

	// Functions
	float           Volume(BSphere const& bsph);
	BSphere& Encompass(BSphere& bsphere, v4 const& point);
	BSphere  Encompass(BSphere const& bsphere, v4 const& point);
	BSphere& Encompass(BSphere& lhs, BSphere const& rhs);
	BSphere  Encompass(BSphere const& lhs, BSphere const& rhs);
	BSphere& EncompassLoose(BSphere& bsphere, v4 const& point);
	BSphere  EncompassLoose(BSphere const& bsphere, v4 const& point);
	BSphere& EncompassLoose(BSphere& lhs, BSphere const& rhs);
	BSphere  EncompassLoose(BSphere const& lhs, BSphere const& rhs);
	bool            IsWithin(BSphere const& bsphere, v4 const& point);
	bool            IsWithin(BSphere const& bsphere, BSphere const& test);
	bool            IsIntersection(BSphere const& lhs, BSphere const& rhs);
}

#endif
