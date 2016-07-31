using cAlgo.API;

namespace Tradee
{
	public static class CAlgo_
	{
		/// <summary>Convert CAlgo time frames to tradee time frames</summary>
		public static ETimeFrame ToTradeeTimeframe(this TimeFrame tf)
		{
			if (tf == TimeFrame.Minute  ) return ETimeFrame.Min1   ;
			if (tf == TimeFrame.Minute2 ) return ETimeFrame.Min2   ;
			if (tf == TimeFrame.Minute3 ) return ETimeFrame.Min3   ;
			if (tf == TimeFrame.Minute4 ) return ETimeFrame.Min4   ;
			if (tf == TimeFrame.Minute5 ) return ETimeFrame.Min5   ;
			if (tf == TimeFrame.Minute6 ) return ETimeFrame.Min6   ;
			if (tf == TimeFrame.Minute7 ) return ETimeFrame.Min7   ;
			if (tf == TimeFrame.Minute8 ) return ETimeFrame.Min8   ;
			if (tf == TimeFrame.Minute9 ) return ETimeFrame.Min9   ;
			if (tf == TimeFrame.Minute10) return ETimeFrame.Min10  ;
			if (tf == TimeFrame.Minute15) return ETimeFrame.Min15  ;
			if (tf == TimeFrame.Minute20) return ETimeFrame.Min20  ;
			if (tf == TimeFrame.Minute30) return ETimeFrame.Min30  ;
			if (tf == TimeFrame.Minute45) return ETimeFrame.Min45  ;
			if (tf == TimeFrame.Hour    ) return ETimeFrame.Hour1  ;
			if (tf == TimeFrame.Hour2   ) return ETimeFrame.Hour2  ;
			if (tf == TimeFrame.Hour3   ) return ETimeFrame.Hour3  ;
			if (tf == TimeFrame.Hour4   ) return ETimeFrame.Hour4  ;
			if (tf == TimeFrame.Hour6   ) return ETimeFrame.Hour6  ;
			if (tf == TimeFrame.Hour8   ) return ETimeFrame.Hour8  ;
			if (tf == TimeFrame.Hour12  ) return ETimeFrame.Hour12 ;
			if (tf == TimeFrame.Daily   ) return ETimeFrame.Day1   ;
			if (tf == TimeFrame.Day2    ) return ETimeFrame.Day2   ;
			if (tf == TimeFrame.Day3    ) return ETimeFrame.Day3   ;
			if (tf == TimeFrame.Weekly  ) return ETimeFrame.Weekly ;
			if (tf == TimeFrame.Monthly ) return ETimeFrame.Monthly;
			return ETimeFrame.None;
		}

		/// <summary>Convert Tradee time frames to CAlgo time frames (Returns null for no corresponding value)</summary>
		public static TimeFrame ToCAlgoTimeframe(this ETimeFrame tf)
		{
			switch (tf) {
			default: return null;
			case ETimeFrame.Min1   : return TimeFrame.Minute  ;
			case ETimeFrame.Min2   : return TimeFrame.Minute2 ;
			case ETimeFrame.Min3   : return TimeFrame.Minute3 ;
			case ETimeFrame.Min4   : return TimeFrame.Minute4 ;
			case ETimeFrame.Min5   : return TimeFrame.Minute5 ;
			case ETimeFrame.Min6   : return TimeFrame.Minute6 ;
			case ETimeFrame.Min7   : return TimeFrame.Minute7 ;
			case ETimeFrame.Min8   : return TimeFrame.Minute8 ;
			case ETimeFrame.Min9   : return TimeFrame.Minute9 ;
			case ETimeFrame.Min10  : return TimeFrame.Minute10;
			case ETimeFrame.Min15  : return TimeFrame.Minute15;
			case ETimeFrame.Min20  : return TimeFrame.Minute20;
			case ETimeFrame.Min30  : return TimeFrame.Minute30;
			case ETimeFrame.Min45  : return TimeFrame.Minute45;
			case ETimeFrame.Hour1  : return TimeFrame.Hour    ;
			case ETimeFrame.Hour2  : return TimeFrame.Hour2   ;
			case ETimeFrame.Hour3  : return TimeFrame.Hour3   ;
			case ETimeFrame.Hour4  : return TimeFrame.Hour4   ;
			case ETimeFrame.Hour6  : return TimeFrame.Hour6   ;
			case ETimeFrame.Hour8  : return TimeFrame.Hour8   ;
			case ETimeFrame.Hour12 : return TimeFrame.Hour12  ;
			case ETimeFrame.Day1   : return TimeFrame.Daily   ;
			case ETimeFrame.Day2   : return TimeFrame.Day2    ;
			case ETimeFrame.Day3   : return TimeFrame.Day3    ;
			case ETimeFrame.Weekly : return TimeFrame.Weekly  ;
			case ETimeFrame.Monthly: return TimeFrame.Monthly ;
			}
		}
	}
}
