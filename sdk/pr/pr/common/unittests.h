//*******************************************************************************************
// UnitTests
//  Copyright © Rylogic Ltd 2009
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
#include <vector>
#include <algorithm>
#include <functional>
#include <memory>
#include "pr/common/assert.h"
#include "pr/common/timers.h"
#include "pr/common/fmt.h"
//#include "pr/str/tostring.h" - don't include tostring, so that it can have unit tests

namespace pr
{
	namespace unittests
	{
		typedef std::function<void(void)> TestFunc;

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
		inline int RunAllTests()
		{
			try
			{
				pr::rtc::StopWatch global_sw;
				global_sw.start();

				std::sort(std::begin(Tests()), std::end(Tests()));

				int passed = 0;
				int failed = 0;
				for (auto i = std::begin(Tests()), iend = std::end(Tests()); i != iend; ++i)
				{
					UnitTestItem const& test = *i;
					TestCount() = 0;
					try
					{
						pr::rtc::StopWatch test_sw;
						std::cout << test.m_name << std::string(40 - strlen(test.m_name), '.');

						test_sw.start();
						test.m_func();
						test_sw.stop();

						std::cout << "success. (" << TestCount() << " tests in " << test_sw.period_ms() << "ms)\n";
						++passed;
					}
					catch (std::exception const& e)
					{
						std::cout << "failed.\n" << e.what() << "\n";
						++failed;
					}
				}

				global_sw.stop();
				if (failed == 0)
					std::cout << " **** UnitTest results: All passed. (taking " << global_sw.period_ms() << "ms) **** \n";
				else
					std::cout << " **** UnitTest results: " << failed << " of " << failed+passed << " failed. **** \n";
				return failed == 0 ? 0 : -1;
			}
			catch (...)
			{
				std::cout << "UnitTests could not complete due to an unhandled exception\n";
				return -1;
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
		inline bool UTEqual(char const* lhs, char const* rhs)
		{
			return strcmp(lhs, rhs) == 0;
		}
		inline bool UTEqual(char* lhs, char* rhs)
		{
			return strcmp(lhs, rhs) == 0;
		}

		// Unit test check functions
		static void Fail(char const* msg, char const* file, int line)
		{
			++TestCount();
			throw std::exception((pr::Fmt("%s(%d):",file,line) + msg).c_str());
		}
		template <typename T, typename U> static void Check(T const& result, U const& expected, char const* expr, char const* file, int line)
		{
			++TestCount();
			if (UTEqual(result, expected)) return;
			std::string r = pr::To<std::string>(result);
			std::string e = pr::To<std::string>(expected);
			throw std::exception(pr::Fmt("%s(%d): '%s' was '%s', expected '%s'",file,line,expr,r.c_str(),e.c_str()).c_str());
		}
		template <typename T> static void Close(T const& result, T const& expected, T tol, char const* expr, char const* file, int line)
		{
			++TestCount();
			T diff = expected - result;
			if (-tol < diff && diff < tol) return;
			std::string r = pr::To<std::string>(result);
			std::string e = pr::To<std::string>(expected);
			std::string t = pr::To<std::string>(tol);
			throw std::exception(pr::Fmt("%s(%d): '%s' was '%s', expected '%s ±%s'",file,line,expr,r.c_str(),e.c_str(),t.c_str()).c_str());
		}
		template <typename TExcept> static void Throws(std::function<void(void)> lambda, char const* expr, char const* file, int line)
		{
			++TestCount();
			bool threw = false;
			bool threw_expected = false;
			try             { lambda(); }
			catch (TExcept) { threw = true; threw_expected = true; }
			catch (...)     { threw = true; }
			if (threw_expected) return;
			char const* e = threw
				? "threw an exception of an unexpected type"
				: "didn't throw when it was expected to";
			throw std::exception(pr::Fmt("%s(%d): '%s' %s",file,line,expr,e).c_str());
		}
	}
}

// If this is giving an error like "int return type assumed" and PRUnitTest is
// not defined, it means you having included the header containing the tests in
// unittests.cpp
#define PRUnitTest(test_name) \
	template <typename T> void unittest_##test_name();\
	static bool s_unittest_##test_name##__LINE__ = pr::unittests::AddTest(\
		pr::unittests::UnitTestItem(#test_name, [](){ unittest_##test_name<void>(); })\
		);\
	template <typename T> void unittest_##test_name()

#define PR_FAIL(msg)\
	pr::unittests::Fail(msg, __FILE__, __LINE__)

#define PR_CHECK(expr, expected_result)\
	pr::unittests::Check((expr), (expected_result), #expr, __FILE__, __LINE__)

#define PR_CLOSE(expr, expected_result, tol)\
	pr::unittests::Close((expr), (expected_result), (tol), #expr, __FILE__, __LINE__)

#define PR_THROWS(expr, what)\
	pr::unittests::Throws<what>([&](){expr;}, #expr, __FILE__, __LINE__)

#endif

