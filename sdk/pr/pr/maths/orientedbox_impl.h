//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_ORIENTED_BOX_IMPL_H
#define PR_MATHS_ORIENTED_BOX_IMPL_H

#include "pr/maths/orientedbox.h"

namespace pr
{
	inline OrientedBox operator * (m4x4 const& m, OrientedBox const& ob)
	{
		OrientedBox obox;
		obox.m_box_to_world = m * ob.m_box_to_world;
		obox.m_radius       = ob.m_radius;
		return obox;
	}

	inline float Volume(OrientedBox const& ob)
	{
		return ob.SizeX() * ob.SizeY() * ob.SizeZ();
	}

	inline m4x4 const& Getm4x4(OrientedBox const& ob)
	{
		return ob.m_box_to_world;
	}
	inline m4x4& Getm4x4(OrientedBox& ob)
	{
		return ob.m_box_to_world;
	}

	inline BoundingSphere GetBoundingSphere(OrientedBox const& ob)
	{
		return BoundingSphere::make(ob.m_box_to_world.pos, Length3(ob.m_radius));
	}

	// Returns a support vertex for the box in world space for a given direction
	inline v4 SupportVertex(OrientedBox const& ob, v4 const& direction, int& feature_type)
	{
		feature_type = OrientedBox::Point;
		v4 vert = ob.m_box_to_world.pos;
		for (int i = 0; i != 3; ++i)
		{
			float d = Dot3(direction, ob.m_box_to_world[i]);
			if (FGtr(d, 0.0f)) vert += ob.m_box_to_world[i] * ob.m_radius[i];
			else if (FLess(d, 0.0f)) vert -= ob.m_box_to_world[i] * ob.m_radius[i];
			else feature_type <<= 1;
		}
		return vert;
	}
	inline v4 SupportVertex(OrientedBox const& ob, v4 const& direction)
	{
		int feature_type;
		return SupportVertex(ob, direction, feature_type);
	}

	// Return the feature of the box in a given direction. 'points' should be an array of 4 v4s
	// The number of points returns equals 'feature_type'
	inline void SupportFeature(OrientedBox const& ob, v4 const& direction, v4* points, int& feature_type)
	{
		feature_type = OrientedBox::Point;
		*points = ob.m_box_to_world.pos;
		for (int i = 0; i != 3; ++i)
		{
			float d = Dot3(direction, ob.m_box_to_world[i]);
			if (FGtr(d, 0.0f)) for (int f = 0; f != feature_type; ++f) {points[f] += ob.m_box_to_world[i] * ob.m_radius[i];}
			else if (FLess(d, 0.0f)) for (int f = 0; f != feature_type; ++f) {points[f] -= ob.m_box_to_world[i] * ob.m_radius[i];}
			else
			{
				switch (feature_type)
				{
				case OrientedBox::Point:
					points[1]  = points[0];
					points[0] += ob.m_box_to_world[i] * ob.m_radius[i];
					points[1] -= ob.m_box_to_world[i] * ob.m_radius[i];
					feature_type = OrientedBox::Edge;
					break;
				case OrientedBox::Edge:
					points[3]  = points[0];
					points[2]  = points[1];
					points[0] += ob.m_box_to_world[i] * ob.m_radius[i];
					points[1] += ob.m_box_to_world[i] * ob.m_radius[i];
					points[2] -= ob.m_box_to_world[i] * ob.m_radius[i];
					points[3] -= ob.m_box_to_world[i] * ob.m_radius[i];
					feature_type = OrientedBox::Face;
					break;
				}
			}
		}
	}
	inline void SupportFeature(OrientedBox const& ob, v4 const& direction, v4* points)
	{
		int feature_type;
		return SupportFeature(ob, direction, points, feature_type);
	}

	namespace impl
	{
		struct IgnorePenetration    { void operator()(v4 const&, float) {} };
		struct MinPenetration
		{
			MinPenetration() : m_penetration(maths::float_max) {}
			void operator()(v4 const& separating_axis, float penetration_depth)
			{
				float axis_len = Length3(separating_axis);
				penetration_depth /= axis_len;

				if (penetration_depth >= m_penetration) return;
				m_separating_axis = separating_axis / axis_len;
				m_penetration = penetration_depth;
			}
			v4 m_separating_axis;   // Not normalised
			float m_penetration;
		};

		template <typename T, typename Penetration>
		bool IsIntersection(OrientedBox const& lhs, OrientedBox const& rhs, Penetration& pen)
		{
			// Compute a transform for 'rhs' in 'lhs's frame
			m4x4 R = GetInverseFast(lhs.m_box_to_world) * rhs.m_box_to_world;

			// Compute common subexpressions. Add in an epsilon term to counteract arithmetic
			// errors when two edges are parallel and their cross product is (near) null
			m3x3 AbsR = Abs(cast_m3x3(R)) + maths::tiny;

			float ra, rb, sp;

			// Test axes L = lhs.x, L = lhs.y, L = lhs.z
			for (int i = 0; i != 3; ++i)
			{
				ra = lhs.m_radius[i];
				rb = rhs.m_radius.x * AbsR.x[i] + rhs.m_radius.y * AbsR.y[i] + rhs.m_radius.z * AbsR.z[i];
				sp = Abs(R.pos[i]);
				if (sp > ra + rb) return false;
				pen(lhs.m_box_to_world[i], ra + rb - sp);
			}

			// Test axes L = rhs.x, L = rhs.y, L = rhs.z
			for (int i = 0; i != 3; ++i)
			{
				ra = Dot3(lhs.m_radius, AbsR[i]);
				rb = rhs.m_radius[i];
				sp = Abs(Dot3(R.pos, R[i]));
				if (sp > ra + rb) return false;
				pen(rhs.m_box_to_world[i], ra + rb - sp);
			}

			// Test axis L = lhs.x X rhs.x
			ra = lhs.m_radius.y * AbsR.x.z + lhs.m_radius.z * AbsR.x.y;
			rb = rhs.m_radius.y * AbsR.z.x + rhs.m_radius.z * AbsR.y.x;
			sp = Abs(R.pos.z * R.x.y - R.pos.y * R.x.z);
			if (sp > ra + rb) return false;
			pen(Cross3(lhs.m_box_to_world.x, rhs.m_box_to_world.x), ra + rb - sp);

			// Test axis L = lhs.x X rhs.y
			ra = lhs.m_radius.y * AbsR.y.z + lhs.m_radius.z * AbsR.y.y;
			rb = rhs.m_radius.x * AbsR.z.x + rhs.m_radius.z * AbsR.x.x;
			sp = Abs(R.pos.z * R.y.y - R.pos.y * R.y.z);
			if (sp > ra + rb) return false;
			pen(Cross3(lhs.m_box_to_world.x, rhs.m_box_to_world.y), ra + rb - sp);

			// Test axis L = lhs.x X rhs.z
			ra = lhs.m_radius.y * AbsR.z.z + lhs.m_radius.z * AbsR.z.y;
			rb = rhs.m_radius.x * AbsR.y.x + rhs.m_radius.y * AbsR.x.x;
			sp = Abs(R.pos.z * R.z.y - R.pos.y * R.z.z);
			if (sp > ra + rb) return false;
			pen(Cross3(lhs.m_box_to_world.x, rhs.m_box_to_world.z), ra + rb - sp);

			// Test axis L = lhs.y X rhs.x
			ra = lhs.m_radius.x * AbsR.x.z + lhs.m_radius.z * AbsR.x.x;
			rb = rhs.m_radius.y * AbsR.z.y + rhs.m_radius.z * AbsR.y.y;
			sp = Abs(R.pos.x * R.x.z - R.pos.z * R.x.x);
			if (sp > ra + rb) return false;
			pen(Cross3(lhs.m_box_to_world.y, rhs.m_box_to_world.x), ra + rb - sp);

			// Test axis L = lhs.y X rhs.y
			ra = lhs.m_radius.x * AbsR.y.z + lhs.m_radius.z * AbsR.y.x;
			rb = rhs.m_radius.x * AbsR.z.y + rhs.m_radius.z * AbsR.x.y;
			sp = Abs(R.pos.x * R.y.z - R.pos.z * R.y.x);
			if (sp > ra + rb) return false;
			pen(Cross3(lhs.m_box_to_world.y, rhs.m_box_to_world.y), ra + rb - sp);

			// Test axis L = lhs.y X rhs.z
			ra = lhs.m_radius.x * AbsR.z.z + lhs.m_radius.z * AbsR.z.x;
			rb = rhs.m_radius.x * AbsR.y.y + rhs.m_radius.y * AbsR.x.y;
			sp = Abs(R.pos.x * R.z.z - R.pos.z * R.z.x);
			if (sp > ra + rb) return false;
			pen(Cross3(lhs.m_box_to_world.y, rhs.m_box_to_world.z), ra + rb - sp);

			// Test axis L = lhs.z X rhs.x
			ra = lhs.m_radius.x * AbsR.x.y + lhs.m_radius.y * AbsR.x.x;
			rb = rhs.m_radius.y * AbsR.z.z + rhs.m_radius.z * AbsR.y.z;
			sp = Abs(R.pos.y * R.x.x - R.pos.x * R.x.y);
			if (sp > ra + rb) return false;
			pen(Cross3(lhs.m_box_to_world.z, rhs.m_box_to_world.x), ra + rb - sp);

			// Test axis L = lhs.z X rhs.y
			ra = lhs.m_radius.x * AbsR.y.y + lhs.m_radius.y * AbsR.y.x;
			rb = rhs.m_radius.x * AbsR.z.z + rhs.m_radius.z * AbsR.x.z;
			sp = Abs(R.pos.y * R.y.x - R.pos.x * R.y.y);
			if (sp > ra + rb) return false;
			pen(Cross3(lhs.m_box_to_world.z, rhs.m_box_to_world.y), ra + rb - sp);

			// Test axis L = lhs.z X rhs.z
			ra = lhs.m_radius.x * AbsR.z.y + lhs.m_radius.y * AbsR.z.x;
			rb = rhs.m_radius.x * AbsR.y.z + rhs.m_radius.y * AbsR.x.z;
			sp = Abs(R.pos.y * R.z.x - R.pos.x * R.z.y);
			if (sp > ra + rb) return false;
			pen(Cross3(lhs.m_box_to_world.z, rhs.m_box_to_world.z), ra + rb - sp);

			// Since no separating axis is found, the OBBs must be intersecting
			return true;
		}
	}//namespace impl

	// Returns true if 'lhs' and 'rhs' are intersecting.
	inline bool IsIntersection(OrientedBox const& lhs, OrientedBox const& rhs)
	{
		impl::IgnorePenetration p;
		return impl::IsIntersection<void, impl::IgnorePenetration>(lhs, rhs, p);
	}

	// Returns true if 'lhs' and 'rhs' are intersecting.
	// 'axis' is the collision normal from 'lhs' to 'rhs'
	// 'penetration' is the depth of penetration between the boxes
	inline bool IsIntersection(OrientedBox const& lhs, OrientedBox const& rhs, v4& axis, float& penetration)
	{
		impl::MinPenetration p;
		if (!impl::IsIntersection<void, impl::MinPenetration>(lhs, rhs, p)) return false;

		axis = p.m_separating_axis;
		penetration = p.m_penetration;

		// Fix up the sign
		axis = (static_cast<float>(Dot3(lhs.m_box_to_world.pos, axis) < Dot3(rhs.m_box_to_world.pos, axis)) * 2 - 1) * axis;
		return true;
	}

	// Returns true if 'lhs' and 'rhs' are intersecting.
	// 'axis' is the collision normal from 'lhs' to 'rhs'
	// 'penetration' is the depth of penetration between the boxes
	// 'pointA' is the world space contact point for 'lhs'  (Only valid when true is returned)
	// 'pointB' is the world space contact point for 'rhs'  (Only valid when true is returned)
	inline bool IsIntersection(OrientedBox const& lhs, OrientedBox const& rhs, v4& axis, float& penetration, v4& pointA, v4& pointB)
	{
		if (!IsIntersection(lhs, rhs, axis, penetration)) return false;

		int feature_typeA, feature_typeB;
		v4  featureA[4], featureB[4];
		SupportFeature(lhs, axis, featureA, feature_typeA);
		SupportFeature(rhs, -axis, featureB, feature_typeB);

		switch (feature_typeA << OrientedBox::Bits | feature_typeB)
		{
		case OrientedBox::Point << OrientedBox::Bits | OrientedBox::Point:
		case OrientedBox::Point << OrientedBox::Bits | OrientedBox::Edge:
		case OrientedBox::Point << OrientedBox::Bits | OrientedBox::Face:
			pointA = featureA[0];
			pointB = featureA[0] + axis * penetration;
			break;
		case OrientedBox::Edge << OrientedBox::Bits | OrientedBox::Point:
		case OrientedBox::Face << OrientedBox::Bits | OrientedBox::Point:
			pointB = featureB[0];
			pointA = featureB[0] - axis * penetration;
			break;
		case OrientedBox::Edge << OrientedBox::Bits | OrientedBox::Edge:
			ClosestPoint_LineSegmentToLineSegment(featureA[0], featureA[1], featureB[0], featureB[1], pointA, pointB);
			break;
		case OrientedBox::Edge << OrientedBox::Bits | OrientedBox::Face:
		case OrientedBox::Face << OrientedBox::Bits | OrientedBox::Edge:
		case OrientedBox::Face << OrientedBox::Bits | OrientedBox::Face:
			assert(false && "Implement me");
			break;
		default:
			assert(false && "Unknown feature type combination");
		}
		return true;
	}
}

#endif
