//***********************************************************
// Profiler
//  Copyright © Rylogic Ltd 2007
//***********************************************************
// Usage:
//	Put the PR_PROFILE_FRAME_BEGIN, PR_PROFILE_FRAME_END, and PR_PROFILE_OUTPUT
//	macros around the portion of the code you want to profile
//	Add profiles:
//		PR_DECLARE_PROFILE(PR_PROFILE_TETRAMESH, MyProfile);
//		PR_PROFILE_SCOPE(PR_PROFILE_TETRAMESH, MyProfile);
//	To get an idea of what you're profiling, step through the code between
//	PR_PROFILE_FRAME_BEGIN and PR_PROFILE_FRAME_END

#ifndef PR_PROFILER_H
#define PR_PROFILER_H

// Don't put this anywhere else in the code
//#define PR_PROFILE_ON // The one and only on/off switch.
// Don't put this anywhere else in the code

#ifndef PR_PROFILE_ON

#define PR_DECLARE_PROFILE(grp, name)
#define PR_PROFILE_START(grp, name)
#define PR_PROFILE_STOP(grp, name)
#define PR_PROFILE_SCOPE(grp, name)
#define PR_PROFILE_FRAME_BEGIN
#define PR_PROFILE_FRAME_END
#define PR_PROFILE_FRAME
#define PR_PROFILE_OUTPUT(steps_per_update)

#else//PR_PROFILE_ON

#include <vector>
#include <algorithm>
#include "pr/macros/join.h"
#include "pr/common/assert.h"
#include "pr/common/fmt.h"
#include "pr/common/vectormap.h"
#include "pr/common/timers.h"

#define PR_DECLARE_PROFILE(grp, name)				PR_EXPAND(grp, static pr::profile::Profile g_pr_profile_##name(#name))
#define PR_DECLARE_PROFILE(grp, name)				PR_EXPAND(grp, static pr::profile::Profile g_pr_profile_##name(#name))
#define PR_PROFILE_START(grp, name)					PR_EXPAND(grp, pr::profile::get().Start(g_pr_profile_##name))
#define PR_PROFILE_STOP(grp, name)					PR_EXPAND(grp, pr::profile::get().Stop (g_pr_profile_##name))
#define PR_PROFILE_SCOPE(grp, name)					PR_EXPAND(grp, pr::profile::Scoped pr_profile_##name_##__LINE__(g_pr_profile_##name))
#define PR_PROFILE_FRAME_BEGIN						pr::profile::get().FrameBegin()
#define PR_PROFILE_FRAME_END						pr::profile::get().FrameEnd()
#define PR_PROFILE_FRAME							pr::profile::get().FrameEnd(); pr::profile::get().FrameBegin()
#define PR_PROFILE_OUTPUT(steps_per_update)			static pr::profile::Proxy g_pr_profile_mgr(steps_per_update); g_pr_profile_mgr.Output()

#define PR_DBG_PROFILE 0
#define PR_PROFILE_ENABLE_CALLERS 0

namespace pr
{
	namespace profile
	{
		enum { NameSize = 16 };
		typedef unsigned long ID;
		struct Profile;

		// Profile data
		struct Data
		{
			ID					m_id;
			unsigned long		m_count;
			unsigned __int64	m_time_incl;
			unsigned __int64	m_time_excl;
		};
		typedef std::vector<Data> TData;

		// Caller data
		struct Caller
		{
			Caller() : m_id(0), m_count(0), m_time(0) {}
			ID					m_id;		// The ID of the profile making the call
			unsigned long		m_count;	// The number of times this profile has called
			unsigned __int64	m_time;		// The amount of time spent in the profile when called from this caller
		};
		typedef std::vector<Caller> TCaller;
		typedef pr::vec_map<Profile*, Caller> TCallerMap;

		// Don't use 'start'/'stop' method on this cause we want to remove the
		// time spent in the house keeping of starting/stopping profiles
		struct Profile
		{
			explicit Profile(char const* name);
			~Profile();

			// Reset the profile data, called after profile output has been generated
			void Reset()
			{
				PR_INFO(PR_DBG_PROFILE, FmtS("[%s] reset\n", m_name));
				m_active = 0;
				m_data.m_count = 0;
				m_data.m_time_incl = 0;
				m_data.m_time_excl = 0;
				m_caller.clear();
			}

			Data				m_data;				// Frame data
			char				m_name[NameSize];	// The name of the profile
			TCallerMap			m_caller;			// A map of parents of this profile, if parent == 0, then its a global scope profile
			unsigned __int64	m_start;			// The rtc value on leaving the profile start method
			unsigned __int64	m_time;				// The time within one Start()/Stop() cycle
			int					m_active;			// > 0 while this profile is running, == 0 when it is not
			bool				m_disabled;			// True if this profile is disabled
			Profile*			m_parent;
		};
		typedef std::vector<Profile*> TProfile;
		inline bool operator == (Profile const& lhs, char const* rhs)
		{
			char const *l = lhs.m_name, *r = rhs;
			while( *l == *r && *l != 0 ) { ++l; ++r; }
			return *l == *r;
		}

		// A manager of profiles. Contains an array of pointers to the profiles
		// Keeps track of frames
		struct Profiler
		{
			Profiler()
			:m_stack(0)
			,m_frame_start(0)
			,m_frame_time(0)
			,m_frames(0)
			,m_to_ms(1000.0 / pr::rtc::ReadCPUFreq())
			,m_frame_started(false)
			,m_report_empty_profiles(false)
			{
				SetAffinityToCPU0();
			}
			void RegisterProfile(Profile& profile)
			{
				m_profiles.push_back(&profile);
			}
			void UnregisterProfile(Profile& profile)
			{
				PR_ASSERT(PR_DBG, profile.m_active == 0, "This profile currently in use");
				TProfile::iterator p = std::find(m_profiles.begin(), m_profiles.end(), &profile);
				if( p != m_profiles.end() ) m_profiles.erase(p);
			}
			void Start(Profile& profile)
			{
				if( !m_frame_started || ++profile.m_active > 1 ) return;
				profile.m_parent = m_stack;
				m_stack = &profile;
				profile.m_start = pr::rtc::Read();
			}
			void Stop(Profile& profile)
			{
				unsigned __int64 now = pr::rtc::Read();
				if( profile.m_active == 0 || --profile.m_active > 0 ) return;
				PR_ASSERT(PR_DBG, profile.m_active == 0, "This profile has been stopped more times than it's been started"); 
				PR_ASSERT(PR_DBG, &profile == m_stack, "This profile hasn't been started or a child profile hasn't been stopped");

				profile.m_time				= now - profile.m_start;
				profile.m_data.m_time_incl += profile.m_time;
				profile.m_data.m_time_excl += profile.m_time;
				profile.m_data.m_count++;
				#if PR_PROFILE_ENABLE_CALLERS == 1
				Caller& caller	= profile.m_caller[m_parent];
				caller.m_id		= profile.m_parent ? profile.m_parent->m_data.m_id : 0;
				caller.m_time	+= profile.m_time;		// The amount of time spent in this profile when called from 'caller'
				caller.m_count++;
				#endif//PR_PROFILE_ENABLE_CALLERS==1

				m_stack = profile.m_parent;
				profile.m_parent = 0;
				unsigned __int64 stop_overhead = pr::rtc::Read() - now;
				m_frame_time -= stop_overhead;
				if( m_stack ) m_stack->m_data.m_time_excl -= profile.m_time + stop_overhead;
			}
			void FrameBegin()
			{
				PR_INFO(PR_DBG_PROFILE, "Profile frame begin\n");
				m_frame_started = true;
				m_frame_start = pr::rtc::Read();
			}
			void FrameEnd()
			{
				m_frame_time += pr::rtc::Read() - m_frame_start;
				m_frame_started = false;
				++m_frames;
				++m_frame_count;
				PR_INFO(PR_DBG_PROFILE, "Profile frame end\n");
			}
			void Reset()
			{
				m_frame_time = 0;
				m_frames = 0;
				m_stack = 0;
			}

			// Call to output data from the profiles and then reset them
			// struct Out { void operator()(profile::Profile const& data); };
			template <typename Output>
			void ReadAndReset(Output& out)
			{
				//PR_ASSERT(PR_DBG, m_stack == 0, "Output should be called outside of the frame");
				for( TProfile::iterator p = m_profiles.begin(), p_end = m_profiles.end(); p != p_end; ++p )
				{
					Profile& profile = **p;
					out(profile);
					profile.Reset();
				}
				Reset();
			}

			TProfile			m_profiles;			// Pointers to the profile data objects
			Profile*			m_stack;			// The stack of nested profiles
			unsigned __int64	m_frame_start;		// Used to calculate m_frame_time
			unsigned __int64	m_frame_time;		// The accumulate time for a frame (div by m_frames to get fps)
			unsigned long		m_frames;			// Number of frames since last output
			unsigned long		m_frame_count;		// Total number of frames profiled
			double				m_to_ms;			// A value to multiply by to convert uint64's to milliseconds
			bool				m_frame_started;	// True when we're between PR_PROFILE_FRAME_BEGIN and PR_PROFILE_FRAME_END
			bool				m_report_empty_profiles; // True if profiles with m_count = 0 are reported in Output
		};

		// Singleton access
		inline Profiler& get()		{ static Profiler s_profiler; return s_profiler; }

		// Implementation ********************************************
		inline Profile::Profile(char const* name)
		:m_active(0)
		,m_disabled(false)
		,m_parent(0)
		{
			PR_INFO(PR_DBG_PROFILE, FmtS("[%s] instantiated\n", name));

			// Assign unique ids to the profiles
			static profile::ID profile_id = 0;
			m_data.m_id = ++profile_id;

			int i = 0;
			for( ; i != sizeof(m_name) - 1 && name[i]; ++i ) m_name[i] = name[i];
			m_name[i] = 0;
			Reset();
			get().RegisterProfile(*this);
		}
		inline Profile::~Profile()
		{
			PR_INFO(PR_DBG_PROFILE, FmtS("[%s] destructed\n", m_name));
			get().UnregisterProfile(*this);
		}

		// Scoped profile
		struct Scoped
		{
			Profile& m_profile;
			Scoped(Profile& profile) : m_profile(profile)	{ get().Start(m_profile); }
			~Scoped()										{ get().Stop (m_profile); }
			Scoped(Scoped const&);
			Scoped& operator=(Scoped const&);
		};
	}//namespace profile
}//namespace pr

#endif//PR_PROFILE_ON
#endif//PR_PROFILER_H


			//// Activate/Deactive profiles. If 'name' is null, all profiles are activated/deactivated
			//void ProfileActive(char const* name, bool on)
			//{
			//	for( TProfile::iterator p = m_profiles.begin(), p_end = m_profiles.end(); p != p_end; ++p )
			//	{
			//		Profile& profile = **p;
			//		if     ( !name )			{ profile.m_active = on; }
			//		else if( profile == name )	{ profile.m_active = on; break; }
			//	}
			//}
