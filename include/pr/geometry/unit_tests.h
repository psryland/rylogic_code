//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2014
//********************************
#pragma once

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/maths/maths.h"
#include "pr/geometry/point.h"
#include "pr/geometry/distance.h"
#include "pr/geometry/closest_point.h"
#include "pr/view3d-12/ldraw/ldraw_builder.h"
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
			PR_EXPECT(PointWithinConvexPolygon(v4Origin, poly, _countof(poly)));
			PR_EXPECT(!PointWithinConvexPolygon(poly[0], poly, _countof(poly)));
			PR_EXPECT(!PointWithinConvexPolygon(v4(-1.0f, +2.0f, 0.0f, 1.0f), poly, _countof(poly)));
			PR_EXPECT(PointWithinConvexPolygon(v4(+1.0f, -0.5f, 0.0f, 1.0f), poly, _countof(poly)));
		}
	}
	PRUnitTest(DistanceTests)
	{
		{// DistanceSq_PointToLineSegment
			auto s = pr::v4(1.0f, 1.0f, 0.0f, 1.0f);
			auto e = pr::v4(3.0f, 2.0f, 0.0f, 1.0f);
			auto a = pr::v4(2.0f, 1.0f, 0.0f, 1.0f);
			PR_EXPECT(FEql(DistanceSq_PointToLineSegment(s, s, e), 0.0f));
			PR_EXPECT(FEql(DistanceSq_PointToLineSegment(e, s, e), 0.0f));
			PR_EXPECT(FEql(DistanceSq_PointToLineSegment((s+e)*0.5f, s, e), 0.0f));
			PR_EXPECT(FEql(DistanceSq_PointToLineSegment(a, s, e), Sqr(sin(atan(0.5f)))));
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
				auto bbox = BBox{v4::Random(rng, v4Origin, 3.0f, 1), v4::Random(rng, v4(0.f), v4(3.f), 0)};
				auto s = v4::Random(rng, v4Origin, 10.0f, 1);
				auto e = v4::Random(rng, v4Origin, 10.0f, 1);

				v4 pt0, pt1;
				auto sep = ClosestPoint_LineSegmentToBBox(s, e, bbox, pt0, pt1);
				//auto dist = -sep.Depth();
				auto axis = sep.SeparatingAxis();

				rdr12::ldraw::Builder builder;
				builder.Box("bbox", 0x8000FF00).bbox(bbox);
				builder.Line("line", 0xFFFF0000).line(s, e);
				builder.Box("cp1", 0xFF0000FF).dim(0.01f).pos(pt0);
				builder.Box("cp2", 0xFF0000FF).dim(0.01f).pos(pt1);
				builder.Line("axis", 0xFF0000FF).line(pt0, pt1);
				//builder.Write(L"\\dump\\test.ldr");
			}
		}
	}
}
#endif