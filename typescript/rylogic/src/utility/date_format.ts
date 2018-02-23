/*
   Date Format 1.2.3
     (c) 2007-2009 Steven Levithan <stevenlevithan.com>
     http://blog.stevenlevithan.com/archives/date-time-format
     MIT license
    
     Includes enhancements by Scott Trenda <scott.trenda.net>
     and Kris Kowal <cixar.com/~kris.kowal/>
    
     Accepts a date, a mask, or a date and a mask.
     Returns a formatted version of the given date.
     The date defaults to the current date/time.
     The mask defaults to DateFormat.masks.default.
  
   Formatting:
     d          = Day of the month as digits; no leading zero for single- digit days.
     dd         = Day of the month as digits; leading zero for single- digit days.
     ddd        = Day of the week as a three-letter abbreviation.
     dddd       = Day of the week as its full name.
     m          = Month as digits; no leading zero for single- digit months.
     mm         = Month as digits; leading zero for single- digit months.
     mmm        = Month as a three- letter abbreviation.
     mmmm       = Month as its full name.
     yy         = Year as last two digits; leading zero for years less than 10.
     yyyy       = Year represented by four digits.
     h          = Hours; no leading zero for single-digit hours (12 - hour clock).
     hh         = Hours; leading zero for single-digit hours (12 - hour clock).
     H          = Hours; no leading zero for single-digit hours (24 - hour clock).
     HH         = Hours; leading zero for single-digit hours (24 - hour clock).
     M          = Minutes; no leading zero for single-digit minutes. Upper case M unlike CF timeFormat's m to avoid conflict with months.
     MM         = Minutes; leading zero for single-digit minutes. Upper case MM unlike CF timeFormat's mm to avoid conflict with months.
     s          = Seconds; no leading zero for single-digit seconds.
     ss         = Seconds; leading zero for single-digit seconds.
     l or L     = Milliseconds. l gives 3 digits. L gives 2 digits.
     t          = Lower case, single - character time marker string: a or p. No equivalent in CF.
     tt         = Lower case, two - character time marker string: am or pm. No equivalent in CF.
     T          = Upper case, single - character time marker string: A or P. Upper case T unlike CF's t to allow for user-specified casing.
     TT         = Upper case, two - character time marker string: AM or PM. Upper case TT unlike CF's tt to allow for user-specified casing.
     Z          = US time zone abbreviation, e.g.EST or MDT.With non-US time zones or in the Opera browser, the GMT/ UTC offset is returned, e.g.GMT - 0500. No equivalent in CF.
     o          = GMT / UTC time zone offset, e.g. -0500 or + 0230. No equivalent in CF.
     S          = The date's ordinal suffix (st, nd, rd, or th). Works well with d. No equivalent in CF.
     '…' or "…" = Literal character sequence. Surrounding quotes are removed. No equivalent in CF.
     UTC:       = Must be the first four characters of the mask. Converts the date from local time to UTC/ GMT / Zulu time before applying the mask.The "UTC:" prefix is removed. No equivalent in CF.
  
    Usage:
     var now = new Date();
    
    now.format("m/dd/yy");
     => e.g., 6/09/07
    
    Can also be used as a standalone function
    DateFormat(now, "dddd, mmmm dS, yyyy, h:MM:ss TT");
     => Saturday, June 9th, 2007, 5:46:21 PM
    
    Can use one of several named masks
    now.format("isoDateTime");
     => 2007-06-09T17:46:21
    
    ...Or add your own
    DateFormat.masks.hammerTime = 'HH:MM! "Can\'t touch this!"';
    now.format("hammerTime");
     => 17:46! Can't touch this!
    
    When using the standalone DateFormat function, you can also provide the date as a string
    DateFormat("Jun 9 2007", "fullDate");
     => Saturday, June 9, 2007
    
    Note that if you don't include the mask argument, DateFormat.masks.default is used
    now.format();
     => Sat Jun 09 2007 17:46:21
    
    And if you don't include the date argument, the current date and time is used
    DateFormat();
     => Sat Jun 09 2007 17:46:22
    
    You can also skip the date argument (as long as your mask doesn't contain any numbers), in which case the current date/time is used
    DateFormat("longTime");
     => 5:46:22 PM EST
    
    And finally, you can convert local time to UTC time. Either pass in true as an additional argument (no argument skipping allowed in this case):
    DateFormat(now, "longTime", true);
    now.format("longTime", true);
     => Both lines return, e.g., 10:46:21 PM UTC
    
    ...Or add the prefix "UTC:" to your mask.
    now.format("UTC:h:MM:ss TT Z");
     => 10:46:21 PM UTC
*/

/** Format a Date/Time string */
export interface IDateFormatFunction
{
	/**
	 * Format a date into a string
	 * @param data The date value to format. (Defaults to Date.now())
	 * @param mask A description of the output format to use
	 * @param utc True if the output should be in UTC
	 */
	(date?: Date | number | string, mask?: string, utc?: boolean): string;

	/** A store of format masks */
	masks: {};

	/** Day/Month strings  */
	i18n: {};
}
export var DateFormat: IDateFormatFunction =
	(function()
	{
		// Regular expressions
		const token = /d{1,4}|m{1,4}|yy(?:yy)?|([HhMsTt])\1?|[LloSZ]|"[^"]*"|'[^']*'/g;
		const timezone = /\b(?:[PMCEA][SDP]T|(?:Pacific|Mountain|Central|Eastern|Atlantic) (?:Standard|Daylight|Prevailing) Time|(?:GMT|UTC)(?:[-+]\d{4})?)\b/g;
		const timezone_clip = /[^-+\dA-Z]/g;

		/** Pad a string to 'len' with leading zeros */
		const pad = function(val: number, len: number = 2): string
		{
			let value = String(val);
			while (value.length < len) value = "0" + value;
			return value;
		}

		// The formatting function
		var df: any = function(date?: Date | number | string, mask?: string, utc?: boolean)
		{
			// Validate input
			if (typeof date === "number" && isNaN(date))
				throw SyntaxError("invalid date");

			// A single string argument is a mask
			if (date && typeof date === "string" && !/\d/.test(date))
			{
				mask = date;
				date = undefined;
			}

			// Passing date through Date applies Date.parse, if necessary
			var date_:any =
				typeof date === 'number' ? new Date(date) :
				typeof date === 'string' ? new Date(date) :
				new Date();

			// Lookup the mask, or use the given custom mask
			var mask_:string =
				typeof mask === 'undefined' ? df.masks["default"] :
				typeof mask === 'string' ? (df.masks[mask] || mask) :
				"";

			// Allow setting the UTC argument via the mask
			if (mask_.slice(0, 4) == "UTC:")
			{
				mask_ = mask_.slice(4);
				utc = true;
			}

			let get = utc ? "getUTC" : "get";
			let d = date_[get + "Date"]();
			let D = date_[get + "Day"]();
			let m = date_[get + "Month"]();
			let y = date_[get + "FullYear"]();
			let H = date_[get + "Hours"]();
			let M = date_[get + "Minutes"]();
			let s = date_[get + "Seconds"]();
			let L = date_[get + "Milliseconds"]();
			let o = utc ? 0 : date_.getTimezoneOffset();
			let flags: { [x: string]: string } =
				{
					d: String(d),
					dd: pad(d),
					ddd: df.i18n.DayNames[D],
					dddd: df.i18n.DayNames[D + 7],
					m: m + 1,
					mm: pad(m + 1),
					mmm: df.i18n.MonthNames[m],
					mmmm: df.i18n.MonthNames[m + 12],
					yy: String(y).slice(2),
					yyyy: y,
					h: String(H % 12 || 12),
					hh: pad(H % 12 || 12),
					H: H,
					HH: pad(H),
					M: M,
					MM: pad(M),
					s: s,
					ss: pad(s),
					l: pad(L, 3),
					L: pad(L > 99 ? Math.round(L / 10) : L),
					t: H < 12 ? "a" : "p",
					tt: H < 12 ? "am" : "pm",
					T: H < 12 ? "A" : "P",
					TT: H < 12 ? "AM" : "PM",
					Z: utc ? "UTC" : (String(date_).match(timezone) || [""]).pop()!.replace(timezone_clip, ""),
					o: (o > 0 ? "-" : "+") + pad(Math.floor(Math.abs(o) / 60) * 100 + Math.abs(o) % 60, 4),
					S: ["th", "st", "nd", "rd"][d % 10 > 3 ? 0 : +((d % 100 - d % 10) != 10) * d % 10]
				};

			return mask_.replace(token, function(x): string
			{
				return x in flags ? String(flags[x]) : x.slice(1, x.length - 1);
			});
		};

		// Some common format strings
		df.masks =
			{
				"default": "yyyy-mm-dd HH:MM:ss",
				shortDate: "m/d/yy",
				mediumDate: "mmm d, yyyy",
				longDate: "mmmm d, yyyy",
				fullDate: "dddd, mmmm d, yyyy",
				shortTime: "h:MM TT",
				mediumTime: "h:MM:ss TT",
				longTime: "h:MM:ss TT Z",
				isoDate: "yyyy-mm-dd",
				isoTime: "HH:MM:ss",
				isoDateTime: "yyyy-mm-dd'T'HH:MM:ss",
				isoUtcDateTime: "UTC:yyyy-mm-dd'T'HH:MM:ss'Z'"
			};

		// Internationalization strings
		df.i18n =
			{
				DayNames:
				[
					"Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat",
					"Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
				],
				MonthNames:
				[
					"Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec",
					"January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"
				]
			};

		return df;
	}());

//// For convenience...
//(<any>Date.prototype).format = function(mask:string, utc:boolean): string
//{
//	return DateFormat(this, mask, utc);
//};