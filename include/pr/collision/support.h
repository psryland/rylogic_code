//*********************************************
// Collision
//  Copyright (c) Rylogic Ltd 2006
//*********************************************
// Support vertices/features

#pragma once

#include <cassert>
#include "pr/maths/maths.h"
#include "pr/common/alloca.h"
#include "pr/collision/shape_sphere.h"
#include "pr/collision/shape_box.h"

namespace pr
{
	namespace collision
	{
		// Support features of a collision shape
		// Note: int(EFeature) is used as the number of points returned from 'SupportFeature()'
		enum class EFeature :int
		{
			Vert = 1,
			Edge = 2,
			Tri  = 3,
			Quad = 4,
			// higher order faces are supported
		};
		static int const EFeatureBits           = 3;
		static int const EFeatureMask           = (1 << EFeatureBits) - 1;
		static int const FeaturePolygonMaxSides = 8;

		// Returns a support vertex for 'shape' for a given direction.
		// Assumes 'direction' is in the shape's parent space (i.e. transformed by Invert(s2w) but not 'shape.m_s2p')
		template <typename TShape> inline v4 SupportVertex(TShape const& shape, v4 const& direction)
		{
			EFeature feature_type;
			return SupportVertex(shape, direction, feature_type);
		}

		// Returns a support vertex for a shape for a given direction.
		// Assumes 'direction' is in the shape's root parent space (i.e. transformed by Invert(shape2world) but not 'shape.m_s2p' or any nested shapes)
		inline v4 SupportVertex(ShapeSphere const& shape, v4_cref direction, EFeature& feature_type)
		{
			assert(IsNormal3(direction));
			feature_type = EFeature::Vert;
			return shape.m_base.m_s2p.pos + shape.m_radius * direction;
		}
		inline v4 SupportVertex(ShapeBox const& shape, v4_cref direction, EFeature& feature_type)
		{
			feature_type = EFeature::Vert;

			auto vert = shape.m_base.m_s2p.pos;
			for (int i = 0; i != 3; ++i)
			{
				float d = Dot3(direction, shape.m_base.m_s2p[i]);
				if      (FGtr (d, 0.0f)) vert += shape.m_base.m_s2p[i] * shape.m_radius[i];
				else if (FLess(d, 0.0f)) vert -= shape.m_base.m_s2p[i] * shape.m_radius[i];
				else feature_type = EFeature(int(feature_type) << 1);
			}
			return vert;
		}
		inline v4 SupportVertex(ShapeTriangle const& shape, v4_cref direction, EFeature& feature_type)
		{
			v4 d(Dot3(direction, shape.m_v.x), Dot3(direction, shape.m_v.y), Dot3(direction, shape.m_v.z), 0.0f);
			feature_type = EFeature::Vert;
			return shape.m_v[LargestElement3(d)];
		}
		inline v4 SupportVertex(ShapeLine const& shape, v4_cref direction, EFeature& feature_type)
		{
			auto d = Dot(direction, shape.m_base.m_s2p.z);
			auto r = shape.m_base.m_s2p.z * shape.m_radius;

			feature_type = EFeature::Vert;
			auto vert = shape.m_base.m_s2p.pos;
			if      (FGtr (d, 0.0f)) vert += r;
			else if (FLess(d, 0.0f)) vert -= r;
			else feature_type = EFeature::Edge;
			return vert;
		}

		// Return the feature of the shape in a given direction.
		// 'points' returns the feature polygon. The number of sides equals 'int(feature_type)'
		// Assumes 'axis' is in the shape's root parent space (i.e. transformed by Invert(shape2world) but not 'shape.m_s2p' or any nested shapes)
		// When a face is returned, the points should be in order such that the face normal == 'axis'
		inline void SupportFeature(ShapeSphere const& shape, v4 const& axis, EFeature& feature_type, v4 (&points)[FeaturePolygonMaxSides])
		{
			points[0] = SupportVertex(shape, axis, feature_type);
			assert(feature_type == EFeature::Vert);
		}
		inline void SupportFeature(ShapeBox const& shape, v4 const& axis, EFeature& feature_type, v4 (&points)[FeaturePolygonMaxSides])
		{
			feature_type = EFeature::Vert;
			*points = shape.m_base.m_s2p.pos;
			for (int i = 0; i != 3; ++i)
			{
				float d = Dot3(axis, shape.m_base.m_s2p[i]);
				if (FGtr(d, 0.0f))
				{
					for (int f = 0; f != int(feature_type); ++f)
						points[f] += shape.m_base.m_s2p[i] * shape.m_radius[i];
				}
				else if (FLess(d, 0.0f))
				{
					for (int f = 0; f != int(feature_type); ++f)
						points[f] -= shape.m_base.m_s2p[i] * shape.m_radius[i];
				}
				else
				{
					switch (feature_type)
					{
					case EFeature::Vert:
						feature_type = EFeature::Edge;
						points[1]  = points[0];
						points[0] += shape.m_base.m_s2p[i] * shape.m_radius[i];
						points[1] -= shape.m_base.m_s2p[i] * shape.m_radius[i];
						break;
					case EFeature::Edge:
						feature_type = EFeature::Quad;
						points[3]  = points[0];
						points[2]  = points[1];
						points[0] += shape.m_base.m_s2p[i] * shape.m_radius[i];
						points[1] += shape.m_base.m_s2p[i] * shape.m_radius[i];
						points[2] -= shape.m_base.m_s2p[i] * shape.m_radius[i];
						points[3] -= shape.m_base.m_s2p[i] * shape.m_radius[i];
						if (Triple3(axis, points[1] - points[0], points[2] - points[0]) < 0)
							std::swap(points[1],points[3]); // Flip the winding order
						break;
					}
				}
			}
		}
		inline void SupportFeature(ShapeLine const& shape, v4 const& axis, EFeature& feature_type, v4 (&points)[FeaturePolygonMaxSides])
		{
			auto d = Dot(axis, shape.m_base.m_s2p.z);
			auto r = shape.m_base.m_s2p.z * shape.m_radius;
			if (FGtr(d, 0.0f))
			{
				// Line points in the direction of the axis, return the end point
				feature_type = EFeature::Vert;
				points[0] = shape.m_base.m_s2p.pos + r;
			}
			else if (FLess(d, 0.0f))
			{
				// Line points against the direction of the axis, return the start point
				feature_type = EFeature::Vert;
				points[0] = shape.m_base.m_s2p.pos - r;
			}
			else
			{
				// Line is perpendicular to the axis, return the line
				feature_type = EFeature::Edge;
				points[0] = shape.m_base.m_s2p.pos - r;
				points[1] = shape.m_base.m_s2p.pos + r;
			}
		}

		// Returns the *single* point of contact between two shapes, 'lhs' and 'rhs'.
		// 'axis' is the collision separating axis.
		// 'pen' is the depth of penetration
		// 'l2w' and 'r2w' transform 'lhs' and 'rhs' into the same space as 'axis' and the space
		// that the contact point is returned in (typically world space).
		template <typename Shape0, typename Shape1>
		v4 FindContactPoint(Shape0 const& lhs, m4x4 const& l2w, Shape1 const& rhs, m4x4 const& r2w, v4 const& axis, float pen)
		{
			// Find the support feature on each shape (in each shape's space)
			EFeature featA, featB;
			v4 pointA[FeaturePolygonMaxSides];
			v4 pointB[FeaturePolygonMaxSides];
			SupportFeature(lhs, InvertFast(l2w) * +axis, featA, pointA);
			SupportFeature(rhs, InvertFast(r2w) * -axis, featB, pointB);

			return FindContactPoint(pointA, featA, pointB, featB, l2w, r2w, axis, pen);
		}
		template <typename = void>
		v4 FindContactPoint(v4* pointA, EFeature featA, v4* pointB, EFeature featB, m4x4 const& l2w, m4x4 const& r2w, v4 const& axis, float pen)
		{
			auto countA = int(featA);
			auto countB = int(featB);

			// Transform the contact points to world space
			for (int i = 0; i != countA; ++i) pointA[i] = l2w * pointA[i];
			for (int i = 0; i != countB; ++i) pointB[i] = r2w * pointB[i];

			// Generally, we want to project the points of featureA/B onto 'axis' to find the
			// average position along the axis as the "single point of collision". Since the feature is 
			// perpendicular to the separating axis, the distance along 'axis' will be halfway between
			// the first point from each feature (in the direction of 'axis'. Still need to find the
			// average position perpendicular to 'axis').

			// For features with area, check that the polygon is facing the correct direction, +ve for featA, -ve for featB
			assert((featA <= EFeature::Edge || (Dot3(axis, plane::make(pointA, pointA + countA)) > 0)) && "Contact polygon has incorrect winding order");
			assert((featB <= EFeature::Edge || (Dot3(axis, plane::make(pointB, pointB + countB)) < 0)) && "Contact polygon has incorrect winding order");

			// If both shapes contact at a vert, then the separating axis passes through their average position
			if (featA == EFeature::Vert && featB == EFeature::Vert)
				return (pointA[0] + pointB[0]) * 0.5f;

			// If one shape is contacting at a vert, then the separating axis must pass through this vert
			if (featA == EFeature::Vert)
				return pointA[0] + axis * (0.5f * Dot3(axis, pointB[0] - pointA[0]));
			if (featB == EFeature::Vert)
				return pointB[0] + axis * (0.5f * Dot3(axis, pointA[0] - pointB[0]));

			// If this is edge-edge contact, then the separating axis passes through the closest points
			if (featA == EFeature::Edge && featB == EFeature::Edge)
			{
				v4 pt0, pt1;
				ClosestPoint_LineSegmentToLineSegment(pointA[0], pointA[1], pointB[0], pointB[1], pt0, pt1);
				return (pt0 + pt1) * 0.5f;
			}

			// Face-Face or Face-Edge contacts require clipping.
			// Find the geometric intersection of the two polygons (in the plane of 'axis').
			// Return the average position of the remaining verts

			// Generate a container of edges for each feature
			struct Edge { float t0, t1; operator bool() const { return t0 < t1; } };
			auto edgesA = PR_ALLOCA_POD(Edge, countA);
			auto edgesB = PR_ALLOCA_POD(Edge, countB);
			for (int i = 0; i != countA; ++i) { edgesA[i].t0 = 0.0f; edgesA[i].t1 = 1.0f; }
			for (int i = 0; i != countB; ++i) { edgesB[i].t0 = 0.0f; edgesB[i].t1 = 1.0f; }

			// Clip the edges of '1' against the edges of '0'.
			// Note: the winding order for polygon '1' is always the opposite of the winding
			// order of polygon '0'. This is because the SupportFeature() function returns the
			// face in the direction of the support axis which for featB is -axis.
			auto clip = [&](v4 const* point0, int count0, v4 const* point1, int count1, Edge* edges1, float sign)
			{
				for (int i = 0; i != count0; ++i)
				{
					auto& as = point0[ i          ];
					auto& ae = point0[(i+1)%count0];
					auto n = sign * Cross3(axis, ae - as);
					for (int j = 0; j != count1; ++j)
					{
						auto& edge = edges1[j];
						if (!edge) continue; // already clipped

						auto& bs = point1[ j          ];
						auto& be = point1[(j+1)%count1];
						if (!Intersect_LineSegmentToPlane(n, bs - as.w0(), be - as.w0(), edge.t0, edge.t1))
							edge.t1 = edge.t0;
					}
				}
				#if 0 // Show the clipped polygon
				{
					string<> s;
					for (int i = 0; i != count1; ++i)
					{
						if (!edges1[i]) continue;
						ldr::Line(s, "e", 0xFF0000FF, point1[i], point1[(i+1)%count1], edges1[i].t0, edges1[i].t1);
					}
					ldr::Write(s, "collision_debug2.ldr");
				}
				#endif
			};

			// If this is edge-face contact, then clip the edge against the face
			if (featA == EFeature::Edge)
			{
				clip(pointB, countB, pointA, countA, edgesA, -1.0f);
				return pointA[0] + (0.5f*(edgesA[0].t0 + edgesA[0].t1))*(pointA[1] - pointA[0]) - (0.5f * pen)*axis;
			}
			if (featB == EFeature::Edge)
			{
				clip(pointA, countA, pointB, countB, edgesB, +1.0f);
				return pointB[0] + (0.5f*(edgesB[0].t0 + edgesB[0].t1))*(pointB[1] - pointB[0]) - (0.5f * pen)*axis;
			}

			// Face to face contact, i.e featA >= EFeature::Tri && featB >= EFeature::Tri
			clip(pointA, countA, pointB, countB, edgesB, +1.0f);
			clip(pointB, countB, pointA, countA, edgesA, -1.0f);

			// Find the average point
			auto avr = [&](v4 const* point, int count, Edge const* edges)
			{
				auto total = 0.0f;
				auto centre = v4Zero;
				for (int i = 0; i != count; ++i)
				{
					if (!edges[i]) continue;
					auto& s = point[i];
					auto& e = point[(i+1)%count];
					auto  d = e - s;
					centre += s + 0.5f * (edges[i].t0 + edges[i].t1) * d;
					total += 1.0f;
				}
				return centre / total;
			};
			auto centreA = avr(pointA, countA, edgesA);
			auto centreB = avr(pointB, countB, edgesB);

			// Shift centre to the halfway point between the faces
			return (0.5f * (centreA + centreB)).w1();
		}
	}
}
