//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2014
//********************************
#pragma once

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/geometry/point.h"
#include "pr/geometry/distance.h"
#include "pr/geometry/closest_point.h"
#include "pr/maths/rand_vector.h"
#include "pr/ldraw/ldr_helper.h"
namespace pr::geometry
{
	PRUnitTest(PointTests)
	{
		{// PointWithinConvexPolygon
			v4 poly[] =
			{
				v4(-2.0f, -1.0f, 0.0f, 1.0f),
				v4(+2.5f, -1.5f, 0.0f, 1.0f),
				v4(+2.0f, +0.5f, 0.0f, 1.0f),
				v4(-0.5f, +2.0f, 0.0f, 1.0f),
			};
			PR_CHECK(PointWithinConvexPolygon(v4Origin, poly, _countof(poly)), true);
			PR_CHECK(PointWithinConvexPolygon(poly[0], poly, _countof(poly)), false);
			PR_CHECK(PointWithinConvexPolygon(v4(-1.0f, +2.0f, 0.0f, 1.0f), poly, _countof(poly)), false);
			PR_CHECK(PointWithinConvexPolygon(v4(+1.0f, -0.5f, 0.0f, 1.0f), poly, _countof(poly)), true);
		}
	}
	PRUnitTest(DistanceTests)
	{
		{// DistanceSq_PointToLineSegment
			auto s = pr::v4(1.0f, 1.0f, 0.0f, 1.0f);
			auto e = pr::v4(3.0f, 2.0f, 0.0f, 1.0f);
			auto a = pr::v4(2.0f, 1.0f, 0.0f, 1.0f);
			PR_CHECK(FEql(DistanceSq_PointToLineSegment(s, s, e), 0.0f), true);
			PR_CHECK(FEql(DistanceSq_PointToLineSegment(e, s, e), 0.0f), true);
			PR_CHECK(FEql(DistanceSq_PointToLineSegment((s+e)*0.5f, s, e), 0.0f), true);
			PR_CHECK(FEql(DistanceSq_PointToLineSegment(a, s, e), Sqr(sin(atan(0.5f)))), true);
		}
	}
	PRUnitTest(ClosestPointTests)
	{
		{// ClosestPoint_PointToPlane

		}
		{// ClosestPoint_LineSegmentToBBox
			std::default_random_engine rng;
			for (int i = 0; i != 100; ++i)
			{
				auto bbox = BBox{Random3(rng, v4Origin, 3.0f, 1.0f), Random3(rng, v4(0), v4(3), 0.0f)};
				auto s = Random3(rng, v4Origin, 10.0f, 1.0f);
				auto e = Random3(rng, v4Origin, 10.0f, 1.0f);

				v4 pt0, pt1;
				auto sep = ClosestPoint_LineSegmentToBBox(s, e, bbox, pt0, pt1);
				//auto dist = -sep.Depth();
				auto axis = sep.SeparatingAxis();

				std::string str;
				ldr::Box(str, "bbox", 0x8000FF00, bbox.m_radius*2, bbox.m_centre);
				ldr::Line(str, "line", 0xFFFF0000, s, e);
				ldr::Box(str, "cp1", 0xFF0000FF, 0.01f, pt0);
				ldr::Box(str, "cp2", 0xFF0000FF, 0.01f, pt1);
				ldr::Line(str, "axis", 0xFF0000FF, pt0, pt1);
				//ldr::Write(str, L"\\dump\\test.ldr");
			}
		}
	}
}
#endif