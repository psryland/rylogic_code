//***********************************************************
// Profiler
//  Copyright (C) Rylogic Ltd 2007
//***********************************************************
// Usage:
//  Put the PR_PROFILE_FRAME_BEGIN, PR_PROFILE_FRAME_END, and PR_PROFILE_OUTPUT
//  macros around the portion of the code you want to profile
//  Add profiles:
//     PR_DECLARE_PROFILE(MyProfile);
//     PR_PROFILE_SCOPE(MyProfile);
//  To get an idea of what you're profiling, step through the code between
//  PR_PROFILE_FRAME_BEGIN and PR_PROFILE_FRAME_END
#pragma once

#define PR_PROFILE_ENABLE 0
#define PR_PROFILE_ENABLE_CALLERS 0

#include <string>
#include <vector>
#include <hash_map>
#include <algorithm>
#include <chrono>
#include <cassert>

#if PR_PROFILE_ENABLE

	#define PR_DECLARE_PROFILE(name)     static pr::profile::Profile profile_##name(#name)
	#define PR_PROFILE_START(name)       pr::profile::get().Start(profile_##name)
	#define PR_PROFILE_STOP(name)        pr::profile::get().Stop (profile_##name)
	#define PR_PROFILE_SCOPE(name)       pr::profile::Scoped profile_##name_##__LINE__(profile_##name)
	#define PR_PROFILE_FRAME_BE          pr::profile::get().FrameBeg()
	#define PR_PROFILE_FRAME_END         pr::profile::get().FrameEnd()
	#define PR_PROFILE_FRAME\
		pr::profile::get().FrameEnd();\
		pr::profile::get().FrameBegin()
	#define PR_PROFILE_OUTPUT(steps_per_update)\
		static pr::profile::Proxy profile_mgr(steps_per_update);\
		profile_mgr.Output()

#else

	#define PR_DECLARE_PROFILE(grp, name)
	#define PR_PROFILE_START(grp, name)
	#define PR_PROFILE_STOP(grp, name)
	#define PR_PROFILE_SCOPE(grp, name)
	#define PR_PROFILE_FRAME_BEGIN
	#define PR_PROFILE_FRAME_END
	#define PR_PROFILE_FRAME
	#define PR_PROFILE_OUTPUT(steps_per_update)

#endif

namespace pr::profile
{
	enum { NameSize = 16 };
	struct Profile;

	using ID = unsigned long;
	using clock_t = std::chrono::high_resolution_clock;
	using duration_t = clock_t::duration;
	using time_point_t = clock_t::time_point;

	// Profile data
	struct Data
	{
		ID         m_id;
		size_t     m_count;
		duration_t m_time_incl;
		duration_t m_time_excl;
	};

	// Caller data
	struct Caller
	{
		ID         m_id;    // The ID of the profile making the call
		size_t     m_count; // The number of times this profile has been called
		duration_t m_time;  // The amount of time spent in the profile when called from this caller

		Caller()
			:m_id()
			,m_count()
			,m_time()
		{}
	};

	// Don't use 'start'/'stop' method on this cause we want to remove the
	// time spent in the house keeping of starting/stopping profiles
	struct Profile
	{
		using CallerMap = std::hash_map<Profile*, Caller>;

		Data         m_data;           // Frame data
		char         m_name[NameSize]; // The name of the profile
		CallerMap    m_caller;         // A map of parents of this profile, if parent == 0, then its a global scope profile
		time_point_t m_start;          // The rtc value on leaving the profile start method
		duration_t   m_time;           // The time within one Start()/Stop() cycle
		int          m_active;         // > 0 while this profile is running, == 0 when it is not
		bool         m_disabled;       // True if this profile is disabled
		Profile*     m_parent;

		explicit Profile(char const* name)
			:m_data()
			,m_name()
			,m_caller()
			,m_start()
			,m_time()
			,m_active()
			,m_disabled()
			,m_parent()
		{
			// Assign unique ids to the profiles
			static ID profile_id = 0;
			m_data.m_id = ++profile_id;

			// Save the profile name
			strncpy(&m_name[0], name, _countof(m_name)-1);

			// Prepare for data
			Reset();

			// Register with the profile manager
			get().RegisterProfile(*this);
		}
		~Profile()
		{
			get().UnregisterProfile(*this);
		}

		// Reset the profile data, called after profile output has been generated
		void Reset()
		{
			m_active = 0;
			m_data.m_count = 0;
			m_data.m_time_incl = duration_t::zero();
			m_data.m_time_excl = duration_t::zero();
			m_caller.clear();
		}

		// Operators
		friend bool operator == (Profile const& lhs, char const* rhs)
		{
			char const *l = &lhs.m_name[0], *r = rhs;
			for (;*l == *r && *l != 0; ++l, ++r) {}
			return *l == *r;
		}
	};

	// A manager of profiles. Contains an array of pointers to the profiles. Keeps track of frames.
	struct Profiler
	{
		using Profiles = std::vector<Profile*>;

		Profiles     m_profiles;              // Pointers to the profile data objects
		Profile*     m_stack;                 // The stack of nested profiles
		time_point_t m_frame_start;           // Used to calculate m_frame_time
		duration_t   m_frame_time;            // The accumulate time for a frame (div by m_frames to get fps)
		uint64_t     m_frames;                // Number of frames since last output
		uint64_t     m_frame_count;           // Total number of frames profiled
		bool         m_frame_started;         // True when we're between PR_PROFILE_FRAME_BEGIN and PR_PROFILE_FRAME_END
		bool         m_report_empty_profiles; // True if profiles with m_count = 0 are reported in Output

		Profiler()
			:m_stack()
			,m_frame_start()
			,m_frame_time()
			,m_frames()
			,m_frame_started()
			,m_report_empty_profiles()
		{}
		void RegisterProfile(Profile& profile)
		{
			m_profiles.push_back(&profile);
		}
		void UnregisterProfile(Profile& profile)
		{
			// This profile currently in use
			assert(profile.m_active == 0);

			auto p = std::find(begin(m_profiles), end(m_profiles), &profile);
			if (p != end(m_profiles))
				m_profiles.erase(p);
		}
		void Start(Profile& profile)
		{
			// Check for reentrancy
			if (!m_frame_started || ++profile.m_active > 1)
				return;

			profile.m_parent = m_stack;
			m_stack = &profile;
			profile.m_start = clock_t::now();
		}
		void Stop(Profile& profile)
		{
			// Sample the time first to reduce overhead in the profile data
			auto now = clock_t::now();

			// Still reentrant?
			if (profile.m_active == 0 || --profile.m_active > 0)
				return;

			assert(profile.m_active == 0); // This profile has been stopped more times than it's been started
			assert(&profile == m_stack);   // This profile hasn't been started or a child profile hasn't been stopped

			// Update times
			profile.m_time = now - profile.m_start;
			profile.m_data.m_time_incl += profile.m_time;
			profile.m_data.m_time_excl += profile.m_time;
			profile.m_data.m_count++;

			// Update caller data
			#if PR_PROFILE_ENABLE_CALLERS == 1
			Caller& caller = profile.m_caller[profile.m_parent];
			caller.m_id = profile.m_parent ? profile.m_parent->m_data.m_id : 0;
			caller.m_time += profile.m_time; // The amount of time spent in this profile when called from 'caller'
			caller.m_count++;
			#endif

			// Update the stack
			m_stack = profile.m_parent;
			profile.m_parent = nullptr;

			// Remove overhead
			auto stop_overhead = clock_t::now() - now;
			m_frame_time -= stop_overhead;
			if (m_stack != nullptr)
				m_stack->m_data.m_time_excl -= profile.m_time + stop_overhead;
		}
		void FrameBegin()
		{
			m_frame_started = true;
			m_frame_start = clock_t::now();
		}
		void FrameEnd()
		{
			m_frame_time += clock_t::now() - m_frame_start;
			m_frame_started = false;
			++m_frames;
			++m_frame_count;
		}
		void Reset()
		{
			m_frame_time = duration_t::zero();
			m_frames = 0;
			m_stack = 0;
		}

		// Call to output data from the profiles and then reset them.
		// struct Out { void operator()(profile::Profile const& data); };
		template <typename Output>
		void ReadAndReset(Output& out)
		{
			// assert(m_stack == nullptr); // Output should be called outside of the frame
			for (auto& profile : m_profiles)
			{
				out(*profile);
				profile->Reset();
			}
			Reset();
		}
	};

	// Singleton access
	inline Profiler& get()
	{
		static Profiler s_profiler;
		return s_profiler;
	}

	// Scoped profile
	struct Scoped
	{
		Profile& m_profile;
		explicit Scoped(Profile& profile)
			:m_profile(profile)
		{
			get().Start(m_profile);
		}
		~Scoped()
		{
			get().Stop (m_profile);
		}
		Scoped(Scoped const&) = delete;
		Scoped& operator=(Scoped const&) = delete;
	};

	// Simple object for timing a block of code and writing the output to the Output window
	struct TimeThis
	{
		using clock_t = std::chrono::high_resolution_clock;
		using duration_t = clock_t::duration;
		using time_point_t = clock_t::time_point;

		std::string m_message;
		time_point_t m_start;
		duration_t m_time;

		TimeThis()
			:m_message()
			,m_start()
			,m_time()
		{}
		explicit TimeThis(std::string_view message)
			:TimeThis()
		{
			// Note: you don't need to call Start/Stop if you use this as an RAII object.
			Start(message);
		}
		~TimeThis()
		{
			Stop();
		}
		TimeThis& Start(std::string_view message)
		{
			m_message = message;
			m_time = duration_t::zero();
			m_start = clock_t::now();
			return *this;
		}
		TimeThis& Stop()
		{
			m_time = clock_t::now() - m_start;
			return *this;
		}
		TimeThis const& Display(bool progress = false) const
		{
			// The can display output before calling Stop()
			auto lineend = progress ? '\r' : '\n';
			auto time = m_time != duration_t::zero() ? m_time : clock_t::now() - m_start;
			OutputDebugStringA(std::format("{} {}{}", m_message, time, lineend).c_str());
			return *this;
		}
	};
}
