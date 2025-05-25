//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/utility/lookup.h"

namespace pr::rdr12
{
	// Simple animation styles
	enum class EAnimStyle : uint8_t
	{
		NoAnimation,
		Once,
		Repeat,
		Continuous,
		PingPong,
	};

	// 2nd order polynomial animation
	struct SimpleAnimation : RefCounted<SimpleAnimation>
	{
		EAnimStyle m_style;  // The animation style
		double     m_period; // Seconds
		v4         m_vel;    // Linear velocity of the animation in m/s
		v4         m_acc;    // Linear velocity of the animation in m/s
		v4         m_avel;   // Angular velocity of the animation in rad/s
		v4         m_aacc;   // Angular velocity of the animation in rad/s

		SimpleAnimation();

		// Return a transform representing the offset added by this object at time 'time_s'
		m4x4 Step(double time_s) const;

		// Ref-counting clean up function
		static void RefCountZero(RefCounted<SimpleAnimation>* doomed);
	};

	// Animation using key frame data
	struct KeyFrameAnimation : RefCounted<KeyFrameAnimation>
	{
		struct Key
		{
			// Should compress this..
			quat m_rotation;
			v4 m_translation;
			v4 m_scale;
			double m_time;
		};

		using KeyCont = pr::vector<Key>;
		using TrackCont = Lookup<string32, KeyCont>;
	
		// Map from bone name to animation keys
		TrackCont m_tracks;

		KeyFrameAnimation();

		// Ref-counting clean up function
		static void RefCountZero(RefCounted<KeyFrameAnimation>* doomed);
	};
}
