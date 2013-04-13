//*******************************************************************************************
// UnitTests
//  Copyright © Rylogic Ltd 2009
//*******************************************************************************************
// Very lightweight unit test framework
// Use:
//  Add a block like this
//    #if PR_UNITTESTS
//    #include "pr/common/unittests.h"
//    namespace pr
//    {
//        PRUnitTest(ExampleUnitTest)
//        {
//            CHECK(1+1, 2);
//        }
//    }
//    #endif
// In any project you can define PR_UNITTESTS=1 and call pr::unittests::RunAllTests()
// to have unit tests available and executed. In the specific unittests project you
// need to add an include of the file containing the unit test, so that it gets included
// in the build.

#pragma once
#ifndef PR_UNITTESTS_H
#define PR_UNITTESTS_H

#include <memory>
#include <iostream>
#include <string>
#include <vector>
#include <functional>
#include "pr/common/assert.h"
#include "pr/common/timers.h"
#include "pr/common/fmt.h"
#include "pr/str/tostring.h"

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
		inline void RunAllTests()
		{
			pr::rtc::StopWatch global_sw;
			global_sw.start();

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
			std::cout << "UnitTest results: " << passed << " passed, " << failed << " failed. (taking " << global_sw.period_ms() << "ms)\n";
		}

		template <typename T> static bool Equal(T lhs, T rhs) { return lhs == rhs; }
		template <>           static bool Equal<char const*>(char const* lhs, char const* rhs) { return strcmp(lhs, rhs) == 0; }

		// Unit test check functions
		template <typename T> static void Check(T result, T expected, char const* expr, char const* file, int line)
		{
			++TestCount();
			if (Equal(result, expected)) return;
			std::string r = pr::To<std::string>(result);
			std::string e = pr::To<std::string>(expected);
			throw std::exception(pr::Fmt("%s(%d): '%s' was '%s', expected '%s'",file,line,expr,r.c_str(),e.c_str()).c_str());
		}
		static void Throws(std::function<void(void)> lambda, bool should_throw, char const* expr, char const* file, int line)
		{
			++TestCount();
			bool threw = false;
			try { lambda(); } catch (...) { threw = true; }
			if (threw == should_throw) return;
			char const* e = should_throw ? "didn't throw, but was expected to" : "threw an exception but wasn't expected to";
			throw std::exception(pr::Fmt("%s(%d): '%s' %s",file,line,expr,e).c_str());
		}
	}
}

#define PRUnitTest(test_name)\
	template <typename UnitTest> void unittest_##test_name();\
	static bool s_unittest_##test_name = pr::unittests::AddTest(pr::unittests::UnitTestItem(#test_name, [](){ unittest_##test_name<void>(); }));\
	template <typename UnitTest> void unittest_##test_name()

#define PR_CHECK(expr, expected_result)\
	pr::unittests::Check((expr), (expected_result), #expr, __FILE__, __LINE__)

#define PR_THROWS(expr, should_throw)\
	pr::unittests::Throws([&](){expr;}, (should_throw), #expr, __FILE__, __LINE__)
#endif

