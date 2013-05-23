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
#include <chrono>
#include <cstdarg>

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
		inline int RunAllTests(bool wordy)
		{
			try
			{
				auto T0 = std::chrono::high_resolution_clock::now();
				std::sort(std::begin(Tests()), std::end(Tests()));

				int passed = 0;
				int failed = 0;
				for (auto i = std::begin(Tests()), iend = std::end(Tests()); i != iend; ++i)
				{
					UnitTestItem const& test = *i;
					TestCount() = 0;
					try
					{
						if (wordy) printf("%s%s", test.m_name, std::string(40 - strlen(test.m_name), '.').c_str());

						auto t0 = std::chrono::high_resolution_clock::now();
						test.m_func();
						auto t1 = std::chrono::high_resolution_clock::now();

						if (wordy) printf("success. (%-4d tests in %7.3fms)\n", TestCount(), std::chrono::duration_cast<std::chrono::microseconds>(t1-t0).count());
						++passed;
					}
					catch (std::exception const& e)
					{
						std::cout << "failed.\n" << e.what() << "\n";
						++failed;
					}
				}

				auto T1 = std::chrono::high_resolution_clock::now();
				if (failed == 0)
					printf(" **** UnitTest results: All %d tests passed. (taking %7.3fms) **** \n", failed+passed, std::chrono::duration_cast<std::chrono::microseconds>(T1-T0).count());
				else
					printf(" **** UnitTest results: %d of %d failed. **** \n", failed, failed+passed);
				return failed == 0 ? 0 : -1;
			}
			catch (...)
			{
				std::cout << "UnitTests could not complete due to an unhandled exception\n";
				return -1;
			}
		}

		// Printf helper
		inline char const* FmtS(char const* format, ...)
		{
			static char buf[512] = {};
			va_list arglist;
			va_start(arglist, format);
			_vsprintf_p(buf, 1023, format, arglist);
			va_end(arglist);
			return buf;
		}

		template <typename T, typename U> inline bool UTEqual(T const& lhs, U const& rhs)
		{
			return lhs == rhs;
		}
		template <typename T, size_t Len1, size_t Len2> inline bool UTEqual(T const (&lhs)[Len1], T const (&rhs)[Len2])
		{
			return Len1 == Len2 && memcmp(lhs, rhs, sizeof(T) * Len1) == 0;
		}
		inline bool UTEqual(double lhs, double rhs)
		{
			return ::abs(rhs - lhs) < DBL_EPSILON;
		}
		inline bool UTEqual(float lhs, float rhs)
		{
			return ::fabs(rhs - lhs) < FLT_EPSILON;
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
		inline void Fail(char const* msg, char const* file, int line)
		{
			++TestCount();
			throw std::exception(FmtS("%s(%d):%s",file,line,msg));
		}
		template <typename T, typename U> inline void Check(T const& result, U const& expected, char const* expr, char const* file, int line)
		{
			++TestCount();
			if (UTEqual(result, expected)) return;
			std::string r = pr::To<std::string>(result);
			std::string e = pr::To<std::string>(expected);
			throw std::exception(FmtS("%s(%d): '%s' was '%s', expected '%s'",file,line,expr,r.c_str(),e.c_str()));
		}
		template <typename T> inline void Close(T const& result, T const& expected, T tol, char const* expr, char const* file, int line)
		{
			++TestCount();
			T diff = expected - result;
			if (-tol < diff && diff < tol) return;
			std::string r = pr::To<std::string>(result);
			std::string e = pr::To<std::string>(expected);
			std::string t = pr::To<std::string>(tol);
			throw std::exception(FmtS("%s(%d): '%s' was '%s', expected '%s ±%s'",file,line,expr,r.c_str(),e.c_str(),t.c_str()));
		}
		template <typename TExcept, typename Func> inline void Throws(Func func, char const* expr, char const* file, int line)
		{
			++TestCount();
			bool threw = false;
			bool threw_expected = false;
			try             { func(); }
			catch (TExcept) { threw = true; threw_expected = true; }
			catch (...)     { threw = true; }
			if (threw_expected) return;
			char const* e = threw
				? "threw an exception of an unexpected type"
				: "didn't throw when it was expected to";
			throw std::exception(FmtS("%s(%d): '%s' %s",file,line,expr,e));
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

