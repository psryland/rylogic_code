using System.ComponentModel;

namespace CoinFlip
{
	public enum ETimeFrame
	{
		None = 0,
		[Description("T1 ")]     Tick1  = 1,
		[Description("M1 ")]     Min1   = 1 * 60,
		[Description("M2 ")]     Min2   = 2 * 60,
		[Description("M3 ")]     Min3   = 3 * 60,
		[Description("M4 ")]     Min4   = 4 * 60,
		[Description("M5 ")]     Min5   = 5 * 60,
		[Description("M6 ")]     Min6   = 6 * 60,
		[Description("M7 ")]     Min7   = 7 * 60,
		[Description("M8 ")]     Min8   = 8 * 60,
		[Description("M9 ")]     Min9   = 9 * 60,
		[Description("M10")]     Min10  = 10 * 60,
		[Description("M15")]     Min15  = 15 * 60,
		[Description("M20")]     Min20  = 20 * 60,
		[Description("M30")]     Min30  = 30 * 60,
		[Description("M45")]     Min45  = 45 * 60,
		[Description("H1 ")]     Hour1  = 1 * 60 * 60,
		[Description("H2 ")]     Hour2  = 2 * 60 * 60,
		[Description("H3 ")]     Hour3  = 3 * 60 * 60,
		[Description("H4 ")]     Hour4  = 4 * 60 * 60,
		[Description("H6 ")]     Hour6  = 6 * 60 * 60,
		[Description("H8 ")]     Hour8  = 8 * 60 * 60,
		[Description("H12")]     Hour12 = 12 * 60 * 60,
		[Description("D1 ")]     Day1   = 1 * 24 * 60 * 60,
		[Description("D2 ")]     Day2   = 2 * 24 * 60 * 60,
		[Description("D3 ")]     Day3   = 3 * 24 * 60 * 60,
		[Description("W1 ")]     Week1  = 1 * 7 * 24 * 60 * 60,
		[Description("W2 ")]     Week2  = 2 * 7 * 24 * 60 * 60,
		[Description("Month1")]  Month1 = 30 * 24 * 60 * 60,
	}
}
