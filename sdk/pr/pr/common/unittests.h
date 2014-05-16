//*******************************************************************************************
// UnitTests
//  Copyright (c) Rylogic Ltd 2009
//*******************************************************************************************
// Very lightweight unit test framework
// Use:
//  Add a block like this
/*
#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(TestName)
		{
			using namespace pr;
			PR_CHECK(1+1, 2);
		}
	}
}
#endif
*/
// In any project you can define PR_UNITTESTS=1 and call pr::unittests::RunAllTests()
// to have unit tests available and executed. In the specific unittests project you
// need to add an include of the file containing the unit test, so that it gets included
// in the build.

#pragma once
#ifndef PR_COMMON_UNITTESTS_H
#define PR_COMMON_UNITTESTS_H

#include <memory>
#include <iostream>
#include <string>
#include <sstream>
#include <vector>
#include <algorithm>
#include <functional>
#include <memory>
#include <chrono>
#include <cstdarg>
#include <locale>

// Cannot include pr lib headers here because they are the headers
// being unit tested. Also, this should be a standalone header

namespace pr
{
	namespace unittests
	{
		typedef std::function<void(void)> TestFunc;

		// A pointer to the stream that output is written to
		inline std::ostream*& outstream()
		{
			static std::ostream* s_ostream = &std::cout;
			return s_ostream;
		}
		inline std::ostream& out() { return *outstream(); }

		struct UnitTestItem
		{
			char const* m_name;
			TestFunc    m_func;
			UnitTestItem(char const* name, TestFunc func) :m_name(name) ,m_func(func) {}
		};
		inline bool operator < (UnitTestItem const& lhs, UnitTestItem const& rhs) { return strcmp(lhs.m_name, rhs.m_name) < 0; }

		// All registered tests
		inline std::vector<UnitTestItem>& Tests()
		{
			static std::vector<UnitTestItem> s_tests;
			return s_tests;
		}

		// A counter used for the number of tests performed
		inline int& TestCount()
		{
			static int s_test_count = 0;
			return s_test_count;
		}

		// Append a unit test to the Tests() collection
		inline bool AddTest(UnitTestItem const& test)
		{
			pr::unittests::Tests().push_back(test);
			return true;
		}

		// Run all of the registered unit tests
		inline int RunAllTests(bool wordy)
		{
			using namespace std::chrono;
			try
			{
				auto T0 = high_resolution_clock::now();
				std::sort(std::begin(Tests()), std::end(Tests()));

				int passed = 0;
				int failed = 0;
				for (auto i = std::begin(Tests()), iend = std::end(Tests()); i != iend; ++i)
				{
					UnitTestItem const& test = *i;
					TestCount() = 0;
					try
					{
						if (wordy) out() << test.m_name << std::string(40 - strlen(test.m_name), '.').c_str();

						auto t0 = high_resolution_clock::now();
						test.m_func();
						auto t1 = high_resolution_clock::now();

						if (wordy) out() << "success. (" << TestCount() << " tests in " << duration_cast<microseconds>(t1-t0).count() << "ms)" << std::endl;
						++passed;
					}
					catch (std::exception const& e)
					{
						out() << "failed." << std::endl << e.what() << std::endl;
						++failed;
					}
				}

				auto T1 = high_resolution_clock::now();
				if (failed == 0)
					out() << " **** UnitTest results: All " << (failed+passed) << " tests passed. (taking " << duration_cast<microseconds>(T1-T0).count()*0.001f << "ms) ****" << std::endl;
				else
					out() << " **** UnitTest results: " << failed << " of " << failed+passed << " failed. ****" << std::endl;
				return failed == 0 ? 0 : -1;
			}
			catch (...)
			{
				out() << "UnitTests could not complete due to an unhandled exception" << std::endl;
				return -1;
			}
		}

		// helpers
		namespace impl
		{
			// A static instance of the locale, because this thing takes ages to construct
			inline std::locale const& locale()
			{
				static std::locale s_locale("");
				return s_locale;
			}

			// Widen/Narrow strings
			inline std::string Narrow(char const* from, std::size_t len = 0)
			{
				if (len == 0) len = strlen(from);
				return std::string(from, from+len);
			}
			inline std::string Narrow(wchar_t const* from, std::size_t len = 0)
			{
				if (len == 0) len = wcslen(from);
				std::vector<char> buffer(len + 1);
				std::use_facet<std::ctype<wchar_t>>(locale()).narrow(from, from + len, '_', &buffer[0]);
				return std::string(&buffer[0], &buffer[len]);
			}
			inline std::wstring Widen(wchar_t const* from, std::size_t len = 0)
			{
				if (len == 0) len = wcslen(from);
				return std::wstring(from, from+len);
			}
			inline std::wstring Widen(char const* from, std::size_t len = 0)
			{
				if (len == 0) len = strlen(from);
				std::vector<wchar_t> buffer(len + 1);
				std::use_facet<std::ctype<wchar_t>>(locale()).widen(from, from + len, &buffer[0]);
				return std::wstring(&buffer[0], &buffer[len]);
			}

			// Stream wide strings into narrow streams
			inline std::basic_ostream<char>& operator << (std::basic_ostream<char>& ostrm, std::basic_string<wchar_t> const& str)
			{
				return ostrm << impl::Narrow(str.c_str(), str.size());
			}
		}

		template <typename T, typename U> inline bool UTEqual(T const& lhs, U const& rhs)
		{
			return lhs == rhs;
		}
		template <typename T, size_t Len1, size_t Len2> inline bool UTEqual(T const (&lhs)[Len1], T const (&rhs)[Len2])
		{
			return Len1 == Len2 && memcmp(lhs, rhs, sizeof(T) * Len1) == 0;
		}
		inline bool UTEqual(double lhs, double rhs)                 { return ::abs(rhs - lhs) < DBL_EPSILON; }
		inline bool UTEqual(float lhs, float rhs)                   { return ::fabs(rhs - lhs) < FLT_EPSILON; }
		inline bool UTEqual(char const* lhs, char const* rhs)       { return strcmp(lhs, rhs) == 0; }
		inline bool UTEqual(char* lhs, char* rhs)                   { return strcmp(lhs, rhs) == 0; }
		inline bool UTEqual(wchar_t const* lhs, wchar_t const* rhs) { return wcscmp(lhs, rhs) == 0; }
		inline bool UTEqual(wchar_t* lhs, wchar_t* rhs)             { return wcscmp(lhs, rhs) == 0; }

		// Unit test check functions
		inline void Fail(char const* msg, char const* file, int line)
		{
			++TestCount();
			std::stringstream ss; ss << file << "(" << line << "): " << msg;
			throw std::exception(ss.str().c_str());
		}
		template <typename T, typename U> inline void Check(T const& result, U const& expected, char const* expr, char const* file, int line)
		{
			using namespace impl;

			++TestCount();
			if (UTEqual(result, expected)) return;
			std::stringstream ss; ss << file << "(" << line << "): '" << expr << "' was '" << result << "', expected '" << expected << "'";
			throw std::exception(ss.str().c_str());
		}
		template <typename T> inline void Close(T const& result, T const& expected, T tol, char const* expr, char const* file, int line)
		{
			using namespace impl;

			++TestCount();
			T diff = expected - result;
			if (-tol < diff && diff < tol) return;
			std::stringstream ss; ss << file << "(" << line << "): '" << expr << "' was '" << result << "', expected '" << expected << " ±" << tol << "'";
			throw std::exception(ss.str().c_str());
		}
		template <typename TExcept, typename Func> inline void Throws(Func func, char const* expr, char const* file, int line)
		{
			using namespace impl;

			++TestCount();
			bool threw = false;
			bool threw_expected = false;
			try             { func(); }
			catch (TExcept) { threw = true; threw_expected = true; }
			catch (...)     { threw = true; }
			if (threw_expected) return;
			std::stringstream ss; ss << file << "(" << line << "): '" << expr << "' " << (threw
				? "threw an exception of an unexpected type"
				: "didn't throw when it was expected to");
			throw std::exception(ss.str().c_str());
		}
	}
}

// If this is giving an error like "int return type assumed" and PRUnitTest is
// not defined, it means you haven't included the header containing the tests in
// unittests.cpp
#define PRUnitTest(testname)/*
	*/template <typename T> void unittest_##testname();                     /* The unit test function forward declaration
	*/inline void unittest_add_##testname() { unittest_##testname<void>(); }/* A function for adding a unit test item
	*/static bool s_unittest_##testname =                                   /* A static bool, that when constructed, causes a test item to be added for the test
	*/	pr::unittests::AddTest(pr::unittests::UnitTestItem(#testname, unittest_add_##testname));\
	template <typename T> void unittest_##testname()

#define PR_FAIL(msg)\
	pr::unittests::Fail(msg, __FILE__, __LINE__)

#define PR_CHECK(expr, expected_result)\
	pr::unittests::Check((expr), (expected_result), #expr, __FILE__, __LINE__)

#define PR_CLOSE(expr, expected_result, tol)\
	pr::unittests::Close((expr), (expected_result), (tol), #expr, __FILE__, __LINE__)

#define PR_THROWS(func, what)\
	pr::unittests::Throws<what>(func, #func, __FILE__, __LINE__)

#endif
