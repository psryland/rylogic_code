using Rylogic.Maths;

namespace EscapeVelocity
{
	/// <summary>Global functions</summary>
	public static class Gbl
	{
		/// <summary>The distance from x1,y1 to x2,y2</summary>
		public static double Distance(double x1, double y1, double x2, double y2)
		{
			return Math_.Sqrt(Math_.Sqr(x2 - x1) + Math_.Sqr(y2 - y1));
		}
	}

	public struct Range
	{
		public double Min;
		public double Max;
		public Range(double min, double max) { Min = min; Max = max; }
	}
}