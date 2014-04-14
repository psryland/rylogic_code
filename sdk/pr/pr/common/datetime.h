//***********************************************
// DateTime
//  Copyright © Rylogic Ltd 2013
//***********************************************
#pragma once
#ifndef PR_COMMON_TIME_H
#define PR_COMMON_TIME_H

#include <chrono>
#include <exception>
#include "pr/common/to.h"
#include "pr/common/fmt.h"
#include "pr/str/prstring.h"

namespace pr
{
	// Julian year, these are not in the standard because of ambiguity over calender type
	typedef std::chrono::duration<int, std::ratio<86400>> days;
	typedef std::chrono::duration<int, std::ratio<31557600>> years;

	namespace datetime
	{
		enum EMaxUnit { Years, Days, Hours, Minutes, Seconds };

		// Ignores leap years
		static double const seconds_p_min  = 60.0;
		static double const seconds_p_hour = 60.0 * seconds_p_min;
		static double const seconds_p_day  = 24.0 * seconds_p_hour;
		static double const seconds_p_year = 365.0 * seconds_p_day;

		// Conversion helpers
		inline double DaysToSeconds(double days) { return days * seconds_p_day; }
		inline double SecondsToDays(double secs) { return secs / seconds_p_day; }

		// Convert a duration into a count down
		// XXX days XX hours XX mins XX secs
		inline std::string ToCountdownString(double seconds, EMaxUnit max_unit)
		{
			std::string s;
			switch (max_unit)
			{
			case Years:
				{
					long years = long(seconds / seconds_p_year);
					s.append(pr::FmtS("%dyrs ", years));
					seconds -= years * seconds_p_year;
				}// fallthru
			case Days:
				{
					long days = long(seconds / seconds_p_day);
					s.append(pr::FmtS("%ddays ", days));
					seconds -= days * seconds_p_day;
				}// fallthru
			case Hours:
				{
					long hours = long(seconds / seconds_p_hour);
					s.append(pr::FmtS("%dhrs ", hours));
					seconds -= hours * seconds_p_hour;
				}// fallthru
			case Minutes:
				{
					long mins = long(seconds / seconds_p_min);
					s.append(pr::FmtS("%dmins ", mins));
					seconds -= mins * seconds_p_min;
				}// fallthru
			case Seconds:
				{
					s.append(pr::FmtS("%2.3fsecs ", seconds));
				}// fallthru
			}
			s.resize(s.size() - 1);
			return s;
		}
	}

	// To<X>
	template <typename TRep, typename TPeriod>
	struct Convert<std::string, std::chrono::duration<TRep,TPeriod> >
	{
		// To formatted string.
		// Supported format specifiers:
		//   %Y - years,        %y - years
		//   %D - days,         %d - days % 365
		//   %H - hours,        %h - hours % 24
		//   %M - minutes       %m - minutes % 60
		//   %S - seconds       %s - seconds % 60
		//   %F - milliseconds  %f - milliseconds % 1000
		//   %U - microseconds  %u - microseconds % 1000
		//   %N - nanoseconds   %n - nanoseconds % 1000
		// Use repeated format specifiers to indicate minimum characters
		// e.g. %sss for 23seconds = 023
		static std::string To(std::chrono::duration<TRep,TPeriod> duration, char const* fmt = "%s")
		{
			return pr::FmtF(fmt, [=](char const*& code)
				{
					using namespace std::chrono;
					int dp = 1;
					for (auto start = code; *(code + 1) == *start; ++code) { ++dp; }
					switch (*code)
					{
					default: throw std::exception("unknown string format code");
					case 'Y': return pr::Fmt("%0*d", dp, duration_cast<years        >(duration).count()       );
					case 'y': return pr::Fmt("%0*d", dp, duration_cast<years        >(duration).count()       );
					case 'D': return pr::Fmt("%0*d", dp, duration_cast<days         >(duration).count()       );
					case 'd': return pr::Fmt("%0*d", dp, duration_cast<days         >(duration).count() % 365 );
					case 'H': return pr::Fmt("%0*d", dp, duration_cast<hours        >(duration).count()       );
					case 'h': return pr::Fmt("%0*d", dp, duration_cast<hours        >(duration).count() % 24  );
					case 'M': return pr::Fmt("%0*d", dp, duration_cast<minutes      >(duration).count()       );
					case 'm': return pr::Fmt("%0*d", dp, duration_cast<minutes      >(duration).count() % 60  );
					case 'S': return pr::Fmt("%0*d", dp, duration_cast<seconds      >(duration).count()       );
					case 's': return pr::Fmt("%0*d", dp, duration_cast<seconds      >(duration).count() % 60  );
					case 'F': return pr::Fmt("%0*d", dp, duration_cast<milliseconds >(duration).count()       );
					case 'f': return pr::Fmt("%0*d", dp, duration_cast<milliseconds >(duration).count() % 1000);
					case 'U': return pr::Fmt("%0*d", dp, duration_cast<microseconds >(duration).count()       );
					case 'u': return pr::Fmt("%0*d", dp, duration_cast<microseconds >(duration).count() % 1000);
					case 'N': return pr::Fmt("%0*d", dp, duration_cast<nanoseconds  >(duration).count()       );
					case 'n': return pr::Fmt("%0*d", dp, duration_cast<nanoseconds  >(duration).count() % 1000);
					}
				});
		}
	};

	// Helper wrapper around 'tm'
	struct DateTimeStruct :tm
	{
		DateTimeStruct() :tm() {}

		enum EDaylightSaving { Unknown = -1, NotInEffect = 0, InEffect = 1 };
		EDaylightSaving daylight_savings() const { return EDaylightSaving((tm_isdst > 0) - (tm_isdst < 0)); }

		// Get/Set the second [0,59]
		int second() const { return tm_sec; }
		void second(int s) { tm_sec = s; }

		// Get/Set the minute [0,59]
		int minute() const { return tm_min; }
		void minute(int m) { tm_min = m; }

		// Get/Set the hour [0,23]
		int hour() const { return tm_hour; }
		void hour(int h) { tm_hour = h; }

		// Get/Set the day of the month [1,31]
		int month_day() const { return tm_mday; }
		void month_day(int mday) { tm_mday = mday; }

		// Get/Set the month [1,12]
		int month() const { return tm_mon + 1; }
		void month(int m) { tm_mon = m - 1; }

		// Get/Set the year [1900,3000]
		int year() const { return tm_year + 1900; }
		void year(int y) { tm_year = y - 1900; }

		// Get/Set the weekday [0,6]
		enum EWeekday { Sunday = 0, Monday, Tuesday, Wednesday, Thursday, Friday, Saturday };
		EWeekday week_day() const { return EWeekday(tm_wday); }
		void week_day(EWeekday wday) { tm_wday = wday; }

		// Get/Set the year day[1,365]
		int year_day() const { return tm_yday + 1; }
		void year_day(int yday) { tm_yday = yday - 1; }

		// Return the time as a string
		std::string ToString() const
		{
			char buf[256];
			if (asctime_s(buf, this) == 0) throw ::std::exception();
			std::string s = buf;
			while (s.rbegin() != s.rend() && *s.rbegin() == '\n') s.resize(s.size() - 1);
			return pr::str::TrimChars(s, "\n", false, true);
		}

		// Return a pretty string representation of the time
		// The formatting codes are:
		// %a - Abbreviated weekday name
		// %A - Full weekday name
		// %b - Abbreviated month name
		// %B - Full month name
		// %c - Date and time representation appropriate for locale
		// %d - Day of month as decimal number (01 – 31)
		// %H - Hour in 24-hour format (00 – 23)
		// %I - Hour in 12-hour format (01 – 12)
		// %j - Day of year as decimal number (001 – 366)
		// %m - Month as decimal number (01 – 12)
		// %M - Minute as decimal number (00 – 59)
		// %p - Current locale's A.M./P.M. indicator for 12-hour clock
		// %S - Second as decimal number (00 – 59)
		// %U - Week of year as decimal number, with Sunday as first day of week (00 – 53)
		// %w - Weekday as decimal number (0 – 6; Sunday is 0)
		// %W - Week of year as decimal number, with Monday as first day of week (00 – 53)
		// %x - Date representation for current locale
		// %X - Time representation for current locale
		// %y - Year without century, as decimal number (00 – 99)
		// %Y - Year with century, as decimal number
		// %z, %Z - Either the time-zone name or time zone abbreviation, depending on registry settings; no characters if time zone is unknown
		// %% - Percent sign
		// As in the printf function, the # flag may prefix any formatting code. In that case,
		// the meaning of the format code is changed as follows.
		// Format code | Meaning
		// %#a, %#A, %#b, %#B, %#p, %#X, %#z, %#Z, %#% | # flag is ignored.
		// %#c | Long date and time representation, appropriate for current locale. For example: "Tuesday, March 14, 1995, 12:41:29".
		// %#x | Long date representation, appropriate to current locale. For example: "Tuesday, March 14, 1995".
		// %#d, %#H, %#I, %#j, %#m, %#M, %#S, %#U, %#w, %#W, %#y, %#Y | Remove leading zeros (if any).
		std::string ToString(char const* fmt) const
		{
			char buf[256];
			if (strftime(buf, sizeof(buf), fmt, this) == 0) throw std::exception();
			return buf;
		}
	};

	struct DateTime
	{
		time_t ticks;  // Seconds since 1970-1-1 00:00:00
		time_t offset; // The offset from utc to the local time

		DateTime() {}
		DateTime(time_t t, time_t ofs) :ticks(t) ,offset(ofs) {}
		DateTime(int yr, int mon, int mday, int hr, int min, int sec, time_t ofs = 0) :ticks() ,offset(ofs)
		{
			DateTimeStruct t;
			t.tm_year = yr - 1900;
			t.tm_mon  = mon - 1;
			t.tm_mday = mday;
			t.tm_hour = hr;
			t.tm_min  = min;
			t.tm_sec  = sec;
			ticks = _mkgmtime(&t);
			if (ticks == -1) throw ::std::exception();
		}

		// Return a structure describing this datetime as local time
		DateTimeStruct local_time() const
		{
			DateTimeStruct t;
			errno_t err = localtime_s(&t, &ticks);
			if (err != 0) throw ::std::exception();
			return t;
		}

		// Return a structure describing this datetime as utc time
		DateTimeStruct utc_time() const
		{
			DateTimeStruct t;
			errno_t err = gmtime_s(&t, &ticks);
			if (err != 0) throw ::std::exception();
			return t;
		}

		static DateTime NowUTC() { return DateTime(time(nullptr), 0); }
		static DateTime Now()
		{
			time_t now = time(nullptr);
			tm localtm; if (localtime_s(&localtm, &now) != 0) throw ::std::exception();
			time_t local_now = _mkgmtime(&localtm);
			return DateTime(now, local_now - now);
		}
		static DateTime Min() { return DateTime(1900,1,1,0,0,0); }
		static DateTime Max() { return DateTime(3000,12,31,23,59,59); }

		static DateTime FromSeconds(double seconds, time_t ofs = 0) { return DateTime(static_cast<time_t>(seconds), ofs); }
	};
	inline bool operator == (DateTime const& lhs, DateTime const& rhs) { return lhs.ticks == rhs.ticks; }
	inline bool operator != (DateTime const& lhs, DateTime const& rhs) { return lhs.ticks != rhs.ticks; }
	inline bool operator <= (DateTime const& lhs, DateTime const& rhs) { return lhs.ticks <= rhs.ticks; }
	inline bool operator >= (DateTime const& lhs, DateTime const& rhs) { return lhs.ticks >= rhs.ticks; }
	inline bool operator <  (DateTime const& lhs, DateTime const& rhs) { return lhs.ticks <  rhs.ticks; }
	inline bool operator >  (DateTime const& lhs, DateTime const& rhs) { return lhs.ticks >  rhs.ticks; }
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_common_datetime)
		{
			using namespace std::chrono;

			{
				auto t = seconds(1234);
				auto s = pr::To<std::string>(t, "%S seconds");
				PR_CHECK(s, "1234 seconds");
			}
			{
				auto t = hours(1) + minutes(23) + seconds(45) + milliseconds(67);
				auto s = pr::To<std::string>(t, "%hh:%mm:%ss.%fff");
				PR_CHECK(s, "01:23:45.067");
			}
		}
	}
}
#endif

#endif
