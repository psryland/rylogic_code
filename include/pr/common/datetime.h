//***********************************************
// DateTime
//  Copyright (c) Rylogic Ltd 2013
//***********************************************
#pragma once

#include <chrono>
#include <exception>
#include "pr/macros/constexpr.h"
#include "pr/macros/noexcept.h"
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

		// These functions are from the 'chrono-Compatible Low-Level Date Algorithms'
		// by http://howardhinnant.github.io/date_algorithms.html
		// Notes:
		//  These algorithms internally assume that March 1 is the first day of the year.
		//  This is convenient because it puts the leap day, Feb. 29 as the last day of
		//  the year, or actually the preceding year.

		// Returns number of days since civil 1970-01-01.
		// Negative values indicate days prior to 1970-01-01.
		//  y-m-d represents a date in the civil (Gregorian) calendar
		//  m is in [1, 12]
		//  d is in [1, last_day_of_month(y, m)]
		//  y is "approximately" in     [numeric_limits<Int>::min()/366, numeric_limits<Int>::max()/366]
		//  Exact range of validity is: [civil_from_days(numeric_limits<Int>::min()), civil_from_days(numeric_limits<Int>::max()-719468)]
		template <class Int> constexpr Int days_from_civil(Int y, unsigned m, unsigned d) noexcept
		{
			static_assert(std::numeric_limits<unsigned>::digits >= 18, "This algorithm has not been ported to a 16 bit unsigned integer");
			static_assert(std::numeric_limits<Int>::digits >= 20, "This algorithm has not been ported to a 16 bit signed integer");
			y -= m <= 2;
			const Int era = (y >= 0 ? y : y-399) / 400;
			const unsigned yoe = static_cast<unsigned>(y - era * 400);      // [0, 399]
			const unsigned doy = (153*(m + (m > 2 ? -3 : 9)) + 2)/5 + d-1;  // [0, 365]
			const unsigned doe = yoe * 365 + yoe/4 - yoe/100 + doy;         // [0, 146096]
			return era * 146097 + static_cast<Int>(doe) - 719468;
		}

		// Returns year/month/day triple in civil calendar
		//  z is number of days since 1970-01-01 and is in the range: [numeric_limits<Int>::min(), numeric_limits<Int>::max()-719468].
		template <class Int> constexpr std::tuple<Int, unsigned, unsigned> civil_from_days(Int z) noexcept
		{
			static_assert(std::numeric_limits<unsigned>::digits >= 18, "This algorithm has not been ported to a 16 bit unsigned integer");
			static_assert(std::numeric_limits<Int>::digits >= 20, "This algorithm has not been ported to a 16 bit signed integer");
			z += 719468;
			const Int era = (z >= 0 ? z : z - 146096) / 146097;
			const unsigned doe = static_cast<unsigned>(z - era * 146097);          // [0, 146096]
			const unsigned yoe = (doe - doe/1460 + doe/36524 - doe/146096) / 365;  // [0, 399]
			const Int y = static_cast<Int>(yoe) + era * 400;
			const unsigned doy = doe - (365*yoe + yoe/4 - yoe/100);                // [0, 365]
			const unsigned mp = (5*doy + 2)/153;                                   // [0, 11]
			const unsigned d = doy - (153*mp+2)/5 + 1;                             // [1, 31]
			const unsigned m = mp + (mp < 10 ? 3 : -9);                            // [1, 12]
			return std::tuple<Int, unsigned, unsigned>(y + (m <= 2), m, d);
		}

		// Returns: true if y is a leap year in the civil calendar, else false
		template <class Int> constexpr inline bool is_leap(Int y) noexcept
		{
			return  y % 4 == 0 && (y % 100 != 0 || y % 400 == 0);
		}

		// Preconditions: m is in [1, 12]
		// Returns: The number of days in the month m of common year
		// The result is always in the range [28, 31].
		constexpr inline unsigned last_day_of_month_common_year(unsigned m) noexcept
		{
			constexpr unsigned char a[] = {31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};
			return a[m-1];
		}

		// Preconditions: m is in [1, 12]
		// Returns: The number of days in the month m of leap year
		// The result is always in the range [29, 31].
		constexpr inline unsigned last_day_of_month_leap_year(unsigned m) noexcept
		{
			constexpr unsigned char a[] = {31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};
			return a[m-1];
		}

		// Preconditions: m is in [1, 12]
		// Returns: The number of days in the month m of year y
		// The result is always in the range [28, 31].
		template <class Int> constexpr inline unsigned last_day_of_month(Int y, unsigned m) noexcept
		{
			return m != 2 || !is_leap(y) ? last_day_of_month_common_year(m) : 29u;
		}

		// Returns day of week in civil calendar [0, 6] -> [Sun, Sat]
		// z is number of days since 1970-01-01 and is in the range: [numeric_limits<Int>::min(), numeric_limits<Int>::max()-4].
		template <class Int> constexpr inline unsigned weekday_from_days(Int z) noexcept
		{
			return static_cast<unsigned>(z >= -4 ? (z+4) % 7 : (z+5) % 7 + 6);
		}

		// Returns: The number of days from the weekday y to the weekday x.
		// Preconditions: x <= 6 && y <= 6
		// The result is always in the range [0, 6].
		constexpr inline unsigned weekday_difference(unsigned x, unsigned y) noexcept
		{
			x -= y;
			return x <= 6 ? x : x + 7;
		}

		// Returns: The weekday following wd
		// Preconditions: wd <= 6
		// The result is always in the range [0, 6].
		constexpr inline unsigned next_weekday(unsigned wd) noexcept
		{
			return wd < 6 ? wd+1 : 0;
		}

		// Returns: The weekday prior to wd
		// Preconditions: wd <= 6
		// The result is always in the range [0, 6].
		constexpr inline unsigned prev_weekday(unsigned wd) noexcept
		{
			return wd > 0 ? wd-1 : 6;
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
#include <iomanip>
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_common_datetime)
		{
			using namespace std::chrono;
			using namespace pr::datetime;

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
			{ // unit test of chrono-Compatible Low-Level Date Algorithms
				PR_CHECK(days_from_civil(1970, 1, 1) == 0, true);                    // 1970-01-01 is day 0
				PR_CHECK(civil_from_days(0) == std::make_tuple(1970, 1, 1), true);   // 1970-01-01 is day 0
				PR_CHECK(weekday_from_days(days_from_civil(1970, 1, 1)) == 4, true); // 1970-01-01 is a Thursday

				auto ystart = -10;// for speed instead of -1000000;
				auto prev_z = days_from_civil(ystart, 1, 1) - 1;
				PR_CHECK(prev_z < 0, true);
				
				auto prev_wd = weekday_from_days(prev_z);
				PR_CHECK(0 <= prev_wd && prev_wd <= 6, true);

				for (auto y = ystart; y <= -ystart; ++y)
				{
					for (auto m = 1U; m <= 12; ++m)
					{
						auto e = last_day_of_month(y, m);
						for (auto d = 1U; d <= e; ++d)
						{
							int z = days_from_civil(y, m, d);
							PR_CHECK(prev_z < z, true);
							PR_CHECK(z == prev_z+1, true);

							int yp; unsigned mp, dp;
							std::tie(yp, mp, dp) = civil_from_days(z);
							PR_CHECK(y == yp, true);
							PR_CHECK(m == mp, true);
							PR_CHECK(d == dp, true);

							auto wd = weekday_from_days(z);
							PR_CHECK(0 <= wd && wd <= 6, true);
							PR_CHECK(wd == next_weekday(prev_wd), true);
							PR_CHECK(prev_wd == prev_weekday(wd), true);
							prev_z = z;
							prev_wd = wd;
						}
					}
				}
				auto count_days = days_from_civil(1000000, 12, 31) - days_from_civil(-1000000, 1, 1);
				PR_CHECK(count_days, 730485365);
			}
			{// Example of using the datetime functions to avoid C time interfaces
				typedef duration<int, ratio_multiply<hours::period, ratio<24>>> days;
				int year; unsigned month; unsigned day;
				hours h; minutes m; seconds s; microseconds us;
				std::stringstream ss; std::string str;
				auto utc_offset = hours(+12);  // my current UTC offset

				// Get duration in local units
				auto now = system_clock::now().time_since_epoch() + utc_offset;

				// Get duration in days
				auto today = duration_cast<days>(now);

				// Convert days into year/month/day
				std::tie(year, month, day) = civil_from_days(today.count());

				// Subtract off days, leaving now containing time since local midnight
				now -= today; h  = duration_cast<hours>(now);
				now -= h;     m  = duration_cast<minutes>(now);
				now -= m;     s  = duration_cast<seconds>(now);
				now -= s;     us = duration_cast<microseconds>(now);

				ss = std::stringstream{};
				ss << "Today is "
					 << year << '-' << std::setw(2) << month << '-' << std::setw(2) << day << " at "
					 << std::setw(2) << h.count() << ':'
					 << std::setw(2) << m.count() << ':'
					 << std::setw(2) << s.count() << '.'
					 << std::setw(6) << us.count();
				str = ss.str();

				// Can also go the other way: Specify a date in terms of a year/month/day triple and then convert that into a system_clock::time_point:
				// Build a time point in local days::hours::minutes and then convert to UTC
				auto birthdate = system_clock::time_point(days(days_from_civil(1976, 12, 29)) + hours(03) + minutes(45) - utc_offset);
				ss = std::stringstream{};
				ss << "Paul is " << duration_cast<seconds>(system_clock::now() - birthdate).count() << " seconds old\n";
				str = ss.str();

				// current utc date time
				now = system_clock::now().time_since_epoch();
				today = duration_cast<days>(now);

				std::tie(year, month, day) = civil_from_days(today.count());

				now -= today; h = duration_cast<hours>(now);
				now -= h;     m = duration_cast<minutes>(now);
				now -= m;     s = duration_cast<seconds>(now);

				ss = std::stringstream{};
				ss << "Today is " << year << '-' << setw(2) << (unsigned)month << '-' << setw(2) << (unsigned)day << " at "
					<< setw(2) << h.count() << ':'
					<< setw(2) << m.count() << ':'
					<< setw(2) << s.count() << " UTC\n";
				str = ss.str();
			}
		}
	}
}
#endif
