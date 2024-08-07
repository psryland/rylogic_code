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
#include <memory>
#include <chrono>
#include <atomic>
#include "pr/common/to.h"
#include "pr/maths/conversion.h"

namespace pr::tweakables
{
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

	// Tweakables singleton
	struct Tweakables
	{
		static Tweakables& Instance()
		{
			static Tweakables s_instance;
			return s_instance;
		}

		// Master switch
		inline static constexpr bool Enable = false;

		// A file to contain the tweakables
		inline static std::filesystem::path filepath = "tweakables.ini";

		// The rate at which the tweakables file is polled for changes
		inline static std::chrono::system_clock::duration poll_rate = std::chrono::milliseconds(1000);

		// Return the value of a tweakable. The first call to this function must have 'def != nullptr'
		template <typename T, impl::StringLiteral S>
		static T Value(T const* def = nullptr)
		{
			static T s_value = *def;
			static int s_issue = 0;

			if constexpr (Enable)
			{
				// Ensure the tweakable exists in the file
				if (s_issue == 0)
					Instance().Add(S.value, To<std::string>(s_value));

				// If the tweakables have changed, update the value
				auto issue = Instance().m_issue.load();
				if (s_issue != issue)
				{
					std::lock_guard<std::mutex> lock(Instance().m_mutex);
					auto map = Instance().m_variables;

					// Read the value
					auto it = map->find(S.value);
					if (it != std::end(*map))
						s_value = To<T>(it->second.c_str());

					s_issue = issue;
				}
			}
			return s_value;
		}

	private:

		using ftime_t = std::filesystem::file_time_type;
		using timepoint_t = std::chrono::system_clock::time_point;
		using map_t = std::unordered_map<std::string, std::string>;
		using map_ptr_t = std::shared_ptr<map_t>;
		
		std::thread m_thread;
		std::atomic_bool m_shutdown;
		std::atomic_int m_issue;
		std::mutex m_mutex;
		map_ptr_t m_variables;
		ftime_t m_last_write_time;

		Tweakables()
			:m_thread()
			,m_shutdown(false)
			,m_issue(1)
			,m_mutex()
			,m_variables()
			,m_last_write_time()
		{
			if constexpr (Enable)
			{
				m_variables = LoadVariables();
				m_last_write_time = LastWrite();
				m_thread = std::thread([this]
				{
					for (; !m_shutdown; std::this_thread::sleep_for(poll_rate))
					{
						auto last_write = LastWrite();
						if (m_last_write_time == last_write)
							continue;

						auto variables = LoadVariables();
						Variables(variables, last_write);
					}
				});
			}
		}
		~Tweakables()
		{
			if constexpr (Enable)
			{
				m_shutdown = true;
				m_thread.join();
			}
		}
		
		// Return a pointer to the latest version of the variables
		void Variables(map_ptr_t variables, ftime_t last_write)
		{
			std::lock_guard<std::mutex> lock(m_mutex);

			m_variables = variables;
			m_last_write_time = last_write;
			++m_issue;
		}

		// Ensure a tweakable value exists in the file
		void Add(std::string const& key, std::string const& value)
		{
			std::lock_guard<std::mutex> lock(m_mutex);

			auto variables = m_variables;
			if (!variables->contains(key))
			{
				(*variables)[key] = value;
				SaveVariables(*variables);
			}
		}

		// Return the time the tweakables file was last written to
		ftime_t LastWrite()
		{
			return std::filesystem::exists(filepath)
				? std::filesystem::last_write_time(filepath)
				: ftime_t::min();
		}

		// Function to trim leading and trailing whitespace from a string
		std::string Trim(std::string const& str) const
		{
			const std::string whitespace = " \t";

			const size_t start = str.find_first_not_of(whitespace);
			if (start == std::string::npos)
				return ""; // no content

			const size_t end = str.find_last_not_of(whitespace);
			return str.substr(start, end - start + 1);
		};

		// Read the tweakables from file
		map_ptr_t LoadVariables() const
		{
			map_ptr_t variables(new map_t);

			std::ifstream ifile(filepath);
			for (std::string line; std::getline(ifile, line); )
			{
				// Find the position of the '=' character
				auto pos = line.find('=');
				if (pos == std::string::npos)
					continue; // skip lines without '='

				// Extract the key and value, and trim any whitespace
				auto key = Trim(line.substr(0, pos));
				auto value = Trim(line.substr(pos + 1));

				// Store the key-value pair in the map
				(*variables)[key] = value;
			}

			return variables;
		}

		// Save the tweakables to file
		void SaveVariables(map_t const& variables)
		{
			std::ofstream ofile(filepath);
			for (auto const& [key, value] : variables)
				ofile << key << " = " << value << std::endl;
		}

		#if PR_UNITTESTS
		friend class TweakablesTests;
		#endif
	};


	// A tweakable value.
	//  Use: Tweakable<float, "MY_VALUE"> my_value = 1.0f;
	template <typename T, impl::StringLiteral S>
	struct Tweakable
	{
		Tweakable(T const& value)
		{
			if constexpr (Tweakables::Enable)
				m_value = Tweakables::Value<T, S>(&value);
			else
				m_value = value;
		}

		operator T() const
		{
			if constexpr (Tweakables::Enable)
				return m_value = Tweakables::Value<T, S>(nullptr);
			else
				return m_value;
		}

	private:
		mutable T m_value; // To ensure the size and alignment of this type is the same as T (and for debugging)
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::tweakables
{
	PRUnitTest(TweakablesTests)
	{
		#if 1
		using namespace pr::tweakables;
		Tweakables::filepath = "unit_test_tweakables.ini";
		Tweakables::poll_rate = std::chrono::milliseconds(100);
		std::filesystem::remove(Tweakables::filepath);

		auto& tweakables = Tweakables::Instance();

		Tweakable<float, "MY_VALUE"> my_value = 1.0f;
		PR_EXPECT(my_value == 1.0f);

		if constexpr (Tweakables::Enable)
		{
			auto variables = tweakables.m_variables;
			(*variables)["MY_VALUE"] = "2.0";
			tweakables.SaveVariables(*variables);

			for (; variables == tweakables.m_variables; )
				std::this_thread::sleep_for(Tweakables::poll_rate);

			PR_EXPECT(my_value == 2.0f);
		}

		std::filesystem::remove(Tweakables::filepath);
		#endif
	}
}
#endif
