import * as Util_ from "../../../rylogic/src/utility/util";

/** Candle data time frames */
export enum ETimeFrame
{
	None = "",
	Tick1 = "T1 ",
	Min1 = "M1 ",
	Min2 = "M2 ",
	Min3 = "M3 ",
	Min4 = "M4 ",
	Min5 = "M5 ",
	Min6 = "M6 ",
	Min7 = "M7 ",
	Min8 = "M8 ",
	Min9 = "M9 ",
	Min10 = "M10",
	Min15 = "M15",
	Min20 = "M20",
	Min30 = "M30",
	Min45 = "M45",
	Hour1 = "H1 ",
	Hour2 = "H2 ",
	Hour3 = "H3 ",
	Hour4 = "H4 ",
	Hour6 = "H6 ",
	Hour8 = "H8 ",
	Hour12 = "H12",
	Day1 = "D1 ",
	Day2 = "D2 ",
	Day3 = "D3 ",
	Week1 = "W1 ",
	Week2 = "W2 ",
	Month1 = "Month1",
}

/** The value of "0 Ticks" in Unix time (seconds) */
const UnixEpochInTicks = 621355968000000000;
const ticks_per_ms = 10000;
const ms_per_s = 1000;
const ms_per_min = ms_per_s * 60;
const ms_per_hour = ms_per_min * 60;
const ms_per_day = ms_per_hour * 24;

/**
 * Convert Windows "ticks" to Unix time (in ms)
 * @param time_in_ticks The time in windows "ticks" (100ns intervals since 0001-1-1 00:00:00)
 * @returns Returns the time value in Unix time (ms)
 */
export function TicksToUnixMS(time_in_ticks: number): number
{
	let unix_time_in_ticks = time_in_ticks - UnixEpochInTicks;
	let unix_time_in_ms = unix_time_in_ticks / ticks_per_ms;
	return unix_time_in_ms;
}

/**
 * Convert a time value (in Unix ms) into units of 'time_frame'.
 *  e.g if 'unix_time_ms' is 4.3 hours, and 'time_frame' is Hour1, the 4.3 is returned.
 * @param unix_time_ms The time value in Unix time (ms)
 * @param time_frame The time frame to convert the time into
 * @returns Returns a time in multiples of 'time_frame'
 */
export function UnixMStoTimeFrame(unix_time_ms: number, time_frame: ETimeFrame): number
{
	switch (time_frame)
	{
	default: throw new Error("Unknown time frame");
	case ETimeFrame.Tick1  : return (unix_time_ms / ms_per_s  ) /  1;
	case ETimeFrame.Min1   : return (unix_time_ms / ms_per_min) /  1;
	case ETimeFrame.Min2   : return (unix_time_ms / ms_per_min) /  2;
	case ETimeFrame.Min3   : return (unix_time_ms / ms_per_min) /  3;
	case ETimeFrame.Min4   : return (unix_time_ms / ms_per_min) /  4;
	case ETimeFrame.Min5   : return (unix_time_ms / ms_per_min) /  5;
	case ETimeFrame.Min6   : return (unix_time_ms / ms_per_min) /  6;
	case ETimeFrame.Min7   : return (unix_time_ms / ms_per_min) /  7;
	case ETimeFrame.Min8   : return (unix_time_ms / ms_per_min) /  8;
	case ETimeFrame.Min9   : return (unix_time_ms / ms_per_min) /  9;
	case ETimeFrame.Min10  : return (unix_time_ms / ms_per_min) / 10;
	case ETimeFrame.Min15  : return (unix_time_ms / ms_per_min) / 15;
	case ETimeFrame.Min20  : return (unix_time_ms / ms_per_min) / 20;
	case ETimeFrame.Min30  : return (unix_time_ms / ms_per_min) / 30;
	case ETimeFrame.Min45  : return (unix_time_ms / ms_per_min) / 45;
	case ETimeFrame.Hour1  : return (unix_time_ms / ms_per_hour) / 1;
	case ETimeFrame.Hour2  : return (unix_time_ms / ms_per_hour) / 2;
	case ETimeFrame.Hour3  : return (unix_time_ms / ms_per_hour) / 3;
	case ETimeFrame.Hour4  : return (unix_time_ms / ms_per_hour) / 4;
	case ETimeFrame.Hour6  : return (unix_time_ms / ms_per_hour) / 6;
	case ETimeFrame.Hour8  : return (unix_time_ms / ms_per_hour) / 8;
	case ETimeFrame.Hour12 : return (unix_time_ms / ms_per_hour) / 12;
	case ETimeFrame.Day1   : return (unix_time_ms / ms_per_day) / 1;
	case ETimeFrame.Day2   : return (unix_time_ms / ms_per_day) / 2;
	case ETimeFrame.Day3   : return (unix_time_ms / ms_per_day) / 3;
	case ETimeFrame.Week1  : return (unix_time_ms / ms_per_day) / 7;
	case ETimeFrame.Week2  : return (unix_time_ms / ms_per_day) / 14;
	case ETimeFrame.Month1 : return (unix_time_ms / ms_per_day) / 30;
	}
}

/**
 * Convert a value in 'time_frame' units to Unix time (in ms).
 * @param units The time value in units of 'time_frame'
 * @param time_frame The time frame that 'units' is in
 * @returns The time value in Unix time (in ms)
 */
export function TimeFrameToUnixMS(units: number, time_frame: ETimeFrame): number
{
	// Use 1 second for all tick time-frames
	switch (time_frame)
	{
	default: throw new Error("Unknown time frame");
	case ETimeFrame.Tick1  : return (units *  1) * ms_per_s;
	case ETimeFrame.Min1   : return (units *  1) * ms_per_min;
	case ETimeFrame.Min2   : return (units *  2) * ms_per_min;
	case ETimeFrame.Min3   : return (units *  3) * ms_per_min;
	case ETimeFrame.Min4   : return (units *  4) * ms_per_min;
	case ETimeFrame.Min5   : return (units *  5) * ms_per_min;
	case ETimeFrame.Min6   : return (units *  6) * ms_per_min;
	case ETimeFrame.Min7   : return (units *  7) * ms_per_min;
	case ETimeFrame.Min8   : return (units *  8) * ms_per_min;
	case ETimeFrame.Min9   : return (units *  9) * ms_per_min;
	case ETimeFrame.Min10  : return (units * 10) * ms_per_min;
	case ETimeFrame.Min15  : return (units * 15) * ms_per_min;
	case ETimeFrame.Min20  : return (units * 20) * ms_per_min;
	case ETimeFrame.Min30  : return (units * 30) * ms_per_min;
	case ETimeFrame.Min45  : return (units * 45) * ms_per_min;
	case ETimeFrame.Hour1  : return (units *  1) * ms_per_hour;
	case ETimeFrame.Hour2  : return (units *  2) * ms_per_hour;
	case ETimeFrame.Hour3  : return (units *  3) * ms_per_hour;
	case ETimeFrame.Hour4  : return (units *  4) * ms_per_hour;
	case ETimeFrame.Hour6  : return (units *  6) * ms_per_hour;
	case ETimeFrame.Hour8  : return (units *  8) * ms_per_hour;
	case ETimeFrame.Hour12 : return (units * 12) * ms_per_hour;
	case ETimeFrame.Day1   : return (units *  1) * ms_per_day;
	case ETimeFrame.Day2   : return (units *  2) * ms_per_day;
	case ETimeFrame.Day3   : return (units *  3) * ms_per_day;
	case ETimeFrame.Week1  : return (units *  7) * ms_per_day;
	case ETimeFrame.Week2  : return (units * 14) * ms_per_day;
	case ETimeFrame.Month1 : return (units * 30) * ms_per_day;
	}
}

/**
 * Return a timestamp string suitable for a chart X tick value
 * @param dt_curr The time value (Unix ms) to convert to a string
 * @param dt_prev The time value (Unix ms) of the previous time (to display relative to) (use null if not available)
 * @param locale_time True if the time should be displayed in the local time-zone locale.
 * @param first True if 'dt_prev' is invalid
 */
export function ShortTimeString(dt_curr: number, dt_prev: number|null, locale_time: boolean): string
{
	let utc = locale_time ? "UTC:" : "";

	// First tick on the x axis
	if (dt_prev == null)
	{
		let dt0 = new Date(dt_curr);
		return Util_.DateFormat(dt0, utc+"HH:MM\nyyyy-mm-dd");
	}
	else
	{
		let dt0 = new Date(dt_curr);
		var dt1 = new Date(dt_prev);

		// Show more of the time stamp depending on how it differs from the previous time stamp
		if (dt0.getFullYear() != dt1.getFullYear()) return Util_.DateFormat(dt0, utc+"HH:MM\nyyyy-mm-dd");
		if (dt0.getMonth() != dt1.getMonth()) return Util_.DateFormat(dt0, utc+"HH:MM\nmmm-d");
		if (dt0.getDay() != dt1.getDay()) return Util_.DateFormat(dt0, utc+"HH:MM\nddd d");
		return Util_.DateFormat(dt0, utc+"HH:MM");
	}
}

/**
 * Convert a time frame to the nearest Bitfinex time frame
  */
export function ToBitfinexTimeFrame(tf: ETimeFrame): string
{
	switch (tf)
	{
	default: throw new Error("Unknown time frame");
	case ETimeFrame.None:
	case ETimeFrame.Tick1:
	case ETimeFrame.Min1: return "1m";
	case ETimeFrame.Min2:
	case ETimeFrame.Min3:
	case ETimeFrame.Min4:
	case ETimeFrame.Min5: return "5m";
	case ETimeFrame.Min6:
	case ETimeFrame.Min7:
	case ETimeFrame.Min8:
	case ETimeFrame.Min9:
	case ETimeFrame.Min10:
	case ETimeFrame.Min15: return "15m";
	case ETimeFrame.Min20:
	case ETimeFrame.Min30: return "30m";
	case ETimeFrame.Min45:
	case ETimeFrame.Hour1: return "1h";
	case ETimeFrame.Hour2:
	case ETimeFrame.Hour3: return "3h";
	case ETimeFrame.Hour4:
	case ETimeFrame.Hour6: return "6h";
	case ETimeFrame.Hour8:
	case ETimeFrame.Hour12: return "12h";
	case ETimeFrame.Day1: return "1D";
	case ETimeFrame.Day2:
	case ETimeFrame.Day3:
	case ETimeFrame.Week1: return "7D";
	case ETimeFrame.Week2: return "14D";
	case ETimeFrame.Month1: return "1M";
	}
}