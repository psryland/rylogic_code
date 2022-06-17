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
namespace pr::unittests
{
	PRUnitTest(TestName)
	{
		using namespace pr;
		PR_CHECK(1+1, 2);
	}
}
#endif
*/
// In any project you can define PR_UNITTESTS=1 and call pr::unittests::RunAllTests()
// to have unit tests available and executed. In the specific unittests project you
// need to add an include of the file containing the unit test, so that it gets included
// in the build.
#pragma once
#include <memory>
#include <iostream>
#include <string>
#include <sstream>
#include <vector>
#include <filesystem>
#include <algorithm>
#include <functional>
#include <memory>
#include <random>
#include <chrono>
#include <cstdarg>
#include <locale>

// Optionally use Microsoft's C++ unit test framework
#define USE_MS_UNITTESTS 0 // Set this to 0 when compiling as an executable
#if USE_MS_UNITTESTS
	#pragma message ("Using MS Unitest Framework")
	#include <SDKDDKVer.h>
	#include "CppUnitTest.h"
	using namespace Microsoft::VisualStudio::CppUnitTestFramework;
#else
	#pragma message ("Using Rylogic Unitest Framework")
	#define TEST_CLASS(testname) class testname
	#define TEST_METHOD(testmethod) void unused()
	#define ONLY_USED_AT_NAMESPACE_SCOPE namespace ___CUT___ {extern int YOU_CAN_ONLY_DEFINE_TEST_CLASS_AT_NAMESPACE_SCOPE;}
#endif

// Cannot include pr lib headers here because they are the headers
// being unit tested. Also, this should be a standalone header

namespace pr::unittests
{
	using TestFunc = std::function<void(void)>;

	struct UnitTestItem
	{
		char const* m_name;
		TestFunc    m_func;
		UnitTestItem(char const* name, TestFunc func)
			:m_name(name)
			, m_func(func)
		{}
		friend bool operator < (UnitTestItem const& lhs, UnitTestItem const& rhs)
		{
			return strcmp(lhs.m_name, rhs.m_name) < 0;
		}
	};

	// Platform string constant
	constexpr wchar_t const* const Platform =
		sizeof(void*) == 8 ? L"x64" :
		sizeof(void*) == 4 ? L"x86" :
		L"";

	// Config string constant
	#ifdef NDEBUG
	constexpr wchar_t const* Config = L"release";
	#else
	constexpr wchar_t const* Config = L"debug";
	#endif

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
		inline std::string Narrow(std::string const& from)
		{
			return from;
		}
		inline std::string Narrow(std::wstring const& from)
		{
			return Narrow(from.c_str(), from.size());
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
		inline std::wstring Widen(std::string const& from)
		{
			return Widen(from.c_str(), from.size());
		}
		inline std::wstring Widen(std::wstring const& from)
		{
			return from;
		}

		// Stream std::string into a wide stream
		inline std::basic_ostream<wchar_t>& operator <<(std::basic_ostream<wchar_t>& stream, std::string const& s)
		{
			return stream << Widen(s);
		}
	}

	// Test Framework functions
	struct TestFramework
	{
		// A counter used for the number of tests performed
		inline static int TestCount = 0;

		// All registered tests
		inline static std::vector<UnitTestItem> Tests;

		// A pointer to the stream that output is written to
		inline static std::ostream* ostream = &std::cout;
		inline static std::ostream& out() { return *ostream; }

		// A directory for temporary files needed by unit tests. Note: automatically cleaned
		inline static std::filesystem::path CreateTempDir(wchar_t const* testname)
		{
			using namespace std::filesystem;
			return weakly_canonical(path(__FILE__).parent_path() / L".." / L".." / L".." / L"obj" / L"unittests" / testname / Platform / Config / "");
		}

		// Append a unit test to the Tests() collection
		inline static bool AddTest(UnitTestItem const& test)
		{
			Tests.push_back(test);
			return true;
		}

		// Unit test check functions
		static void Fail(wchar_t const* msg, char const* file, int line)
		{
			using namespace impl;

			++TestCount;
			#if USE_MS_UNITTESTS
			Assert::Fail(msg);
			#endif
			std::wstringstream ss; ss << file << "(" << line << "): " << msg;
			throw std::runtime_error(Narrow(ss.str()).c_str());
		}

		// Assert that a unit test result is true
		static void IsTrue(bool result, wchar_t const* expr, char const* file, int line)
		{
			using namespace impl;

			++TestCount;
			#if USE_MS_UNITTESTS
			Assert::IsTrue(result, expr);
			#endif
			if (result) return;
			std::wstringstream ss; ss << file << "(" << line << "): '" << expr << "' failed";
			throw std::runtime_error(Narrow(ss.str()).c_str());
		}

		// Check that 'func' throws a 'TExcept' exception
		template <typename TExcept, typename Func>
		static void Throws(Func func, wchar_t const* expr, char const* file, int line)
		{
			using namespace impl;

			++TestCount;
			#if USE_MS_UNITTESTS
			Assert::ExpectException<TExcept>(func, expr);
			#endif

			bool threw = false;
			bool threw_expected = false;
			try
			{
				func();
			}
			catch (TExcept const&)
			{
				threw = true;
				threw_expected = true;
			}
			catch (...)
			{
				threw = true;
			}
			if (threw_expected)
				return;

			std::wstringstream ss; ss << file << "(" << line << "): '" << expr << "' " << (threw
				? "threw an exception of an unexpected type"
				: "didn't throw when it was expected to");
			throw std::runtime_error(Narrow(ss.str()).c_str());
		}
	};

	// Run all of the registered unit tests
	inline int RunAllTests(bool wordy)
	{
		using namespace std::chrono;
		try
		{
			TestFramework::out() << " **** Begin Unit Tests **** " << std::endl;
			std::sort(std::begin(TestFramework::Tests), std::end(TestFramework::Tests));

			// Run the tests
			int passed = 0, failed = 0;
			auto T0 = high_resolution_clock::now();
			for (auto const& test : TestFramework::Tests)
			{
				TestFramework::TestCount = 0;
				try
				{
					if (wordy) TestFramework::out() << test.m_name << std::string(40 - strlen(test.m_name), '.').c_str();

					auto t0 = high_resolution_clock::now();
					test.m_func();
					auto t1 = high_resolution_clock::now();

					if (wordy) TestFramework::out() << "success. (" << TestFramework::TestCount << " tests in " << duration_cast<microseconds>(t1-t0).count() << "ms)" << std::endl;
					++passed;
				}
				catch (std::exception const& e)
				{
					TestFramework::out() << test.m_name << " failed:" << std::endl << e.what() << std::endl;
					++failed;
				}
			}
			auto T1 = high_resolution_clock::now();

			// Print the results
			if (failed == 0)
				TestFramework::out() << " **** UnitTest results: All " << (failed+passed) << " tests passed. (taking " << duration_cast<microseconds>(T1-T0).count()*0.001f << "ms) ****" << std::endl;
			else
				TestFramework::out() << " **** UnitTest results: " << failed << " of " << failed+passed << " failed. ****" << std::endl;

			return failed == 0 ? 0 : -1;
		}
		catch (...)
		{
			TestFramework::out() << "UnitTests could not complete due to an unhandled exception" << std::endl;
			return -1;
		}
	}

	// Unit test Equal functions
	template <typename T, typename U> inline bool UTEqual(T const& lhs, U const& rhs)
	{
		return lhs == rhs;
	}
	template <typename T, size_t Len1, size_t Len2> inline bool UTEqual(T const (&lhs)[Len1], T const (&rhs)[Len2])
	{
		return Len1 == Len2 && memcmp(lhs, rhs, sizeof(T) * Len1) == 0;
	}
	template <typename T, typename U> inline bool UTEqual(T const* lhs, std::initializer_list<U> rhs)
	{
		int i = 0;
		for (auto& r : rhs)
		{
			if (lhs[i++] == r) continue;
			return false;
		}
		return true;
	}
	inline bool UTEqual(double lhs, double rhs)
	{
		if (isnan(lhs) && isnan(rhs)) return true;
		if (isinf(lhs) && isinf(rhs)) return signbit(lhs) == signbit(rhs);
		return ::abs(rhs - lhs) < DBL_EPSILON;
	}
	inline bool UTEqual(float lhs, float rhs)
	{
		if (isnan(lhs) && isnan(rhs)) return true;
		if (isinf(lhs) && isinf(rhs)) return signbit(lhs) == signbit(rhs);
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
	inline bool UTEqual(wchar_t const* lhs, wchar_t const* rhs)
	{
		return wcscmp(lhs, rhs) == 0;
	}
	inline bool UTEqual(wchar_t* lhs, wchar_t* rhs)
	{
		return wcscmp(lhs, rhs) == 0;
	}
}

// This macro creates a class using the unit test name, followed by a method that is the body of the test case
#define PRUnitTest(testname, ...)\
	TEST_CLASS(testname)\
	{\
	public:\
		inline static std::filesystem::path const temp_dir = pr::unittests::TestFramework::CreateTempDir(L#testname);\
		TEST_METHOD(UnitTest) { func(); }\
		static void func()\
		{\
			std::filesystem::remove_all(temp_dir);\
			std::filesystem::create_directories(temp_dir);\
			tests<##__VA_ARGS__>();\
		}\
		template <typename T = void, typename... Args> static void tests()\
		{\
			test<T>();\
			if constexpr (sizeof...(Args) > 0)\
				tests<Args...>();\
		}\
		template <typename T> static void test();\
	};\
	static bool s_unittest_##testname = pr::unittests::TestFramework::AddTest(pr::unittests::UnitTestItem(#testname, &testname::func));\
	template <typename T> void testname::test()

#define PR_FAIL(msg)\
	pr::unittests::TestFramework::Fail(msg, __FILE__, __LINE__)

#define PR_CHECK(expr, ...)\
	pr::unittests::TestFramework::IsTrue(pr::unittests::UTEqual((expr), __VA_ARGS__), L#expr, __FILE__, __LINE__)

#define PR_THROWS(func, what)\
	pr::unittests::TestFramework::Throws<what>(func, L#func, __FILE__, __LINE__)

#define PR_UNITTEST_OUT\
	pr::unittests::TestFramework::out()

#if USE_MS_UNITTESTS
	#define PRTestClass(testname)\
		ONLY_USED_AT_NAMESPACE_SCOPE class testname : public ::Microsoft::VisualStudio::CppUnitTestFramework::TestClass<testname>
	#define PRTestMethod(testname)\
		TEST_METHOD(testname)
#endif
