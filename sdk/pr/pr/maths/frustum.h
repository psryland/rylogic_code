//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_FRUSTUM_H
#define PR_MATHS_FRUSTUM_H

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/matrix4x4.h"
#include "pr/maths/vector4.h"
#include "pr/maths/boundingsphere.h"
#include "pr/maths/boundingbox.h"

namespace pr
{
	struct Frustum
	{
		// The inward pointing normals of the faces of the frustum
		// Note: the frustum grows down the negative z axis, i.e. the
		// z value of the pointy end is greater than the base end.
		// This is because cameras generally look down the negative z axis
		// in righthanded space so that the x axis is to the right and the y axis is up.
		m4x4 m_Tnorms;

		static Frustum makeWH(float width, float height, float z, float zfar = 0);
		static Frustum makeFA(float fovY, float aspect, float zfar = 0);
		static Frustum makeHV(float horz_angle, float vert_angle, float zfar = 0);

		Frustum& setWH(float width, float height, float z, float zfar = 0);
		Frustum& setFA(float fovY, float aspect, float zfar = 0);
		Frustum& setHV(float horz_angle, float vert_angle, float zfar = 0);

		// Get/Set the z position of the sharp end.
		// This is the distance to the far clip plane
		void ZDist(float zero);
		float ZDist() const;

		// Return the frustum width/height at the far plane
		float Width() const;
		float Height() const;

		// Return the Y field of view/aspect ratio
		float FovY() const;
		float Aspect() const;

		// Return a vector containing the dimensions of the frustum
		// width, height, zfar, longest_edge_length
		pr::v4 Dim() const;

		// Return a plane of the frustum
		enum EPlane {XPos = 0, XNeg = 1, YPos = 2, YNeg = 3, ZFar, EPlane_NumberOf};
		Plane Plane(int plane_index) const;

		// Return a matrix containing the inward pointing normals as the x,y,z,w vectors
		// where: x=left, y=right, z=top, w=bottom
		pr::m4x4 Normals() const;
		pr::v4   Normal(int plane_index) const;
	};

	// Binary operators
	Frustum operator * (m4x4 const& m, Frustum const& rhs);

	// Equality operators
	bool operator == (Frustum const& lhs, Frustum const& rhs);
	bool operator != (Frustum const& lhs, Frustum const& rhs);
	bool operator <  (Frustum const& lhs, Frustum const& rhs);
	bool operator >  (Frustum const& lhs, Frustum const& rhs);
	bool operator <= (Frustum const& lhs, Frustum const& rhs);
	bool operator >= (Frustum const& lhs, Frustum const& rhs);

	// Functions
	bool IsWithin(Frustum const& frustum, v4 const& point);
	bool IsWithin(Frustum const& frustum, BBox const& bbox);
	bool IsWithin(Frustum const& frustum, BSphere const& sphere);

	// Intersect the line passing through 's' and 'e' to 'frustum' returning
	// parametric values 't0' and 't1'. Note: this is an accumulative function,
	// 't0' and 't1' must be initialised. Returns true if t0 < t1 i.e. some of
	// the line is within the frustum.
	bool Intersect(Frustum const& frustum, v4 const& s, v4 const& e, float& t0, float& t1, bool include_far_plane);
}

#endif
