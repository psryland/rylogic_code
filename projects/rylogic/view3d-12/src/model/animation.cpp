//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/model/animation.h"
#include "pr/view3d-12/utility/utility.h"

namespace pr::rdr12
{
	SimpleAnimation::SimpleAnimation()
		: m_vel(v4::Zero())
		, m_acc(v4::Zero())
		, m_avel(v4::Zero())
		, m_aacc(v4::Zero())
		, m_period(1.0)
		, m_style(EAnimStyle::NoAnimation)
	{
	}

	// Return a transform representing the offset added by this object at time 'time_s'
	m4x4 SimpleAnimation::EvaluateAtTime(double time_s) const
	{
		auto t = 0.0;
		switch (m_style)
		{
			case EAnimStyle::NoAnimation:
			{
				t = 0.0;
				break;
			}
			case EAnimStyle::Once:
			{
				t = Clamp(time_s, 0.0, m_period);
				break;
			}
			case EAnimStyle::Repeat:
			{
				t = Wrap(time_s, 0.0, m_period);
				break;
			}
			case EAnimStyle::Continuous:
			{
				t = time_s;
				break;
			}
			case EAnimStyle::PingPong:
			{
				t = Wrap(time_s, 0.0, 2.0 * m_period);
				t = t >= m_period ? 2.0 * m_period - t : t;
				break;
			}
			default:
			{
				throw std::runtime_error("Unknown animation style");
			}
		}

		auto time = static_cast<float>(t);
		auto ang = 0.5f * m_aacc * Sqr(time) + m_avel * time;
		auto lin = 0.5f * m_acc * Sqr(time) + m_vel * time + v4::Origin();
		return m4x4::Transform(ang, lin);
	}

	// Ref-counting clean up function
	void SimpleAnimation::RefCountZero(RefCounted<SimpleAnimation>* doomed)
	{
		auto anim = static_cast<SimpleAnimation*>(doomed);
		rdr12::Delete(anim);
	}

	// --------------------------------------------------------------------------------------------

	KeyFrameAnimation::KeyFrameAnimation(uint64_t skel_id, EAnimStyle style)
		: m_skel_id(skel_id)
		, m_style(style)
		, m_tracks()
	{}

	// Returns the linearly interpolated key frames a 'time_s'
	void KeyFrameAnimation::EvaluateAtTime(double time_s, KeyFrameAnimation::Sample& out) const
	{
		out.resize(m_tracks.size());

		// Sample each track at 'time_s'
		std::for_each(std::execution::par, std::begin(m_tracks), std::end(m_tracks), [&](Track const& track)
		{
			auto& sam = out[&track - m_tracks.data()];

			// Degenerate tracks
			if (track.empty())
			{
				sam = KeyFrame::Identity();
				return;
			}

			// The total length of the track
			auto time0_s = track.front().m_time; // Don't assume front().m_time is 0.0
			auto period_s = track.back().m_time - time0_s;
			if (period_s <= maths::tinyd) // handles track.size() == 1 as well
			{
				sam = track.front();
				return;
			}

			// Wrap time into the track's time range
			auto rtime_s = time_s - time0_s; // Relative time
			switch (m_style)
			{
				case EAnimStyle::NoAnimation:
				{
					rtime_s = 0.0;
					break;
				}
				case EAnimStyle::Once:
				{
					rtime_s = Clamp(rtime_s, 0.0, period_s);
					break;
				}
				case EAnimStyle::Repeat:
				{
					rtime_s = Wrap(rtime_s, 0.0, period_s);
					break;
				}
				case EAnimStyle::Continuous:
				{
					// todo: root motion
					rtime_s = Wrap(rtime_s, 0.0, period_s);
					break;
				}
				case EAnimStyle::PingPong:
				{
					rtime_s = Wrap(rtime_s, 0.0, 2.0 * period_s);
					rtime_s = rtime_s >= period_s ? 2.0 * period_s - rtime_s : rtime_s;
					break;
				}
				default:
				{
					throw std::runtime_error("Unknown animation style");
				}
			}

			// Convert the wrapped time back to absolute time
			time_s = rtime_s + time0_s;

			// Find the frames to interpolate between. Lower bound finds the first element with 'key.m_time >= time_s'
			auto iter = std::lower_bound(std::begin(track), std::end(track), time_s, [](KeyFrame const& key, double time_s) { return key.m_time < time_s; });
			if (iter == std::begin(track))
			{
				sam = track.front();
				return;
			}
			if (iter == std::end(track))
			{
				sam = track.back();
				return;
			}

			// Linear interpolation between key frames
			auto const& lhs = *(iter - 1);
			auto const& rhs = *(iter - 0);
			auto frac = Frac(lhs.m_time, time_s, rhs.m_time);
			sam = Lerp(lhs, rhs, s_cast<float>(frac));
		});
	}
	KeyFrameAnimation::Sample KeyFrameAnimation::EvaluateAtTime(double time_s) const
	{
		KeyFrameAnimation::Sample sample;
		EvaluateAtTime(time_s, sample);
		return sample;
	}

	// Ref-counting clean up function
	void KeyFrameAnimation::RefCountZero(RefCounted<KeyFrameAnimation>* doomed)
	{
		auto anim = static_cast<KeyFrameAnimation*>(doomed);
		rdr12::Delete(anim);
	}
}
