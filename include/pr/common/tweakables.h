//***********************************************************************
// Tweakable
//  Copyright (c) Rylogic Ltd 2008
//***********************************************************************
#pragma once
#include <string>
#include <fstream>
#include <unordered_map>
#include <type_traits>
#include <filesystem>
#include <chrono>
#include "pr/common/to.h"
#include "pr/maths/conversion.h"

namespace pr::tweakables
{
	using ftime_t = std::filesystem::file_time_type;
	using timepoint_t = std::chrono::system_clock::time_point;

	// A file to contain the tweakables
	inline std::filesystem::path& filepath()
	{
		static std::filesystem::path s_file = "tweakables.ini";
		return s_file;
	}

	// The rate at which the tweakables file is polled for changes
	inline std::chrono::system_clock::duration poll_rate()
	{
		return std::chrono::milliseconds(1000);
	}

	// Return the time the tweakables file was last written to
	inline ftime_t last_write()
	{
		return std::filesystem::exists(filepath())
			? std::filesystem::last_write_time(filepath())
			: ftime_t::min();
	}

	// The variables in the tweakables file
	inline std::unordered_map<std::string, std::string>& variables(bool save = false)
	{
		static std::unordered_map<std::string, std::string> s_variables;
		static std::filesystem::file_time_type s_last_write_time;

		if (save)
		{
			std::ofstream ofile(filepath());
			for (auto const& [key, value] : s_variables)
				ofile << key << " = " << value << std::endl;

			ofile.close();
			s_last_write_time = last_write();
		}
		else if (s_last_write_time != last_write())
		{
			s_variables.clear();

			// Function to trim leading and trailing whitespace from a string
			static auto trim = [](std::string const& str) -> std::string
			{
				const std::string whitespace = " \t";

				const size_t start = str.find_first_not_of(whitespace);
				if (start == std::string::npos)
					return ""; // no content

				const size_t end = str.find_last_not_of(whitespace);
				return str.substr(start, end - start + 1);
			};

			// Ensure the tweakable exists in the file
			std::ifstream ifile(filepath());
			for (std::string line; std::getline(ifile, line); )
			{
				// Find the position of the '=' character
				auto pos = line.find('=');
				if (pos == std::string::npos)
					continue; // skip lines without '='

				// Extract the key and value, and trim any whitespace
				auto key = trim(line.substr(0, pos));
				auto value = trim(line.substr(pos + 1));

				// Store the key-value pair in the map
				s_variables[key] = value;
			}

			s_last_write_time = last_write();
		}
		return s_variables;
	}

	// Helper to allow string literals to be used as template arguments
	namespace impl
	{
		template <size_t N>
		struct StringLiteral
		{
			char value[N];
			constexpr StringLiteral(const char (&str)[N]) { std::copy_n(str, N, value); }
		};
	}

	// A tweakable value
	template <typename T, impl::StringLiteral S>
	struct Tweakable
	{
		mutable T m_value;
		mutable timepoint_t m_last_time;

		Tweakable(T const& value)
			: m_value(value)
			, m_last_time()
		{
			// Ensure the value is in the tweakables file
			auto it = variables().find(S.value);
			if (it == std::end(variables()))
			{
				variables()[S.value] = To<std::string>(value);
				variables(true);
			}
			else
			{
				m_value = To<T>(it->second.c_str());
			}
		}

		operator T const& () const
		{
			auto now = std::chrono::system_clock::now();
			if (now - m_last_time > std::chrono::seconds(1))
			{
				m_last_time = now;
				m_value = To<T>(variables()[S.value].c_str());
			}
			return m_value;
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::common::tweakables
{
	PRUnitTest(TweakablesTests)
	{
#if 0
		using namespace pr::tweakables;
		filepath() = "unit_test_tweakables.ini";

		Tweakable<float, "MY_VALUE"> my_value = 1.0f;
		PR_EXPECT(my_value == 1.0f);

		variables()["MY_VALUE"] = "2.0";
		variables(true);

		PR_EXPECT(my_value == 2.0f);

		std::filesystem::remove(filepath());
#endif
	}
}
#endif
