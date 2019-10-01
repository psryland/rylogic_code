using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rylogic.Maths
{
	public static partial class Geometry
	{
		/// <summary>Returns the squared distance from 'point' to 'brect'</summary>
		public static float DistanceSq(v2 point, BRect brect)
		{
			float dist_sq = 0.0f;
			v2 lower = brect.Lower;
			v2 upper = brect.Upper;
			if      (point.x < lower.x) dist_sq += Math_.Sqr(lower.x - point.x);
			else if (point.x > upper.x) dist_sq += Math_.Sqr(point.x - upper.x);
			if      (point.y < lower.y) dist_sq += Math_.Sqr(lower.y - point.y);
			else if (point.y > upper.y) dist_sq += Math_.Sqr(point.y - upper.y);
			return dist_sq;
		}

		/// <summary>Returns the squared distance from 'point' to 'bbox'</summary>
		public static float DistanceSq(v4 point, BBox bbox)
		{
			float dist_sq = 0.0f;
			v4 lower = bbox.Lower();
			v4 upper = bbox.Upper();
			if      (point.x < lower.x) dist_sq += Math_.Sqr(lower.x - point.x);
			else if (point.x > upper.x) dist_sq += Math_.Sqr(point.x - upper.x);
			if      (point.y < lower.y) dist_sq += Math_.Sqr(lower.y - point.y);
			else if (point.y > upper.y) dist_sq += Math_.Sqr(point.y - upper.y);
			if      (point.z < lower.z) dist_sq += Math_.Sqr(lower.z - point.z);
			else if (point.z > upper.z) dist_sq += Math_.Sqr(point.z - upper.z);
			return dist_sq;
		}
	}
}
