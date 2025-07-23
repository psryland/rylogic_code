//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/model/animation.h"
#include "pr/view3d-12/utility/utility.h"

namespace pr::rdr12
{
	RootAnimation::RootAnimation()
		: m_vel(v4::Zero())
		, m_acc(v4::Zero())
		, m_avel(v4::Zero())
		, m_aacc(v4::Zero())
		, m_period(1.0)
		, m_style(EAnimStyle::NoAnimation)
	{
	}

	// Return a transform representing the offset added by this object at time 'time_s'
	m4x4 RootAnimation::EvaluateAtTime(double time_s) const
	{
		auto time = static_cast<float>(AdjTime(time_s, TimeRange{ 0.0, m_period }, m_style));
		auto ang = 0.5f * m_aacc * Sqr(time) + m_avel * time;
		auto lin = 0.5f * m_acc * Sqr(time) + m_vel * time + v4::Origin();
		return m4x4::Transform(ang, lin);
	}

	// Ref-counting clean up function
	void RootAnimation::RefCountZero(RefCounted<RootAnimation>* doomed)
	{
		auto anim = static_cast<RootAnimation*>(doomed);
		rdr12::Delete(anim);
	}

	// --------------------------------------------------------------------------------------------

	KeyFrameAnimation::KeyFrameAnimation(uint64_t skel_id, TimeRange time_range, double frame_rate)
		: m_skel_id(skel_id)
		, m_tracks()
		, m_time_range(time_range)
		, m_frame_rate(frame_rate)
	{}

	// Sample each track at 'time_s'
	template <typename Key> requires (std::is_assignable_v<Key, KeyFrame>)
	static void EvaluateAtTime(double time_s, std::span<KeyFrameAnimation::Track const> tracks, std::span<Key> out)
	{
		assert(tracks.size() == out.size());
		std::for_each(std::execution::par, std::begin(tracks), std::end(tracks), [&](KeyFrameAnimation::Track const& track)
		{
			auto& sam = out[&track - tracks.data()];

			// Degenerate tracks
			if (track.empty())
			{
				sam = KeyFrame::Identity();
				return;
			}

			// The total length of the track
			auto time_range = TimeRange{ track.front().m_time, track.back().m_time };
			if (time_range.size() <= maths::tinyd) // handles track.size() == 1 as well
			{
				sam = track.front();
				return;
			}

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

			// Interpolate between key frames
			auto const& lhs = *(iter - 1);
			auto const& rhs = *(iter - 0);
			auto frac = Frac(lhs.m_time, time_s, rhs.m_time);
			sam = Lerp(lhs, rhs, s_cast<float>(frac));
		});
	}

	// Returns the linearly interpolated key frames a 'time_s'
	void KeyFrameAnimation::EvaluateAtTime(double time_s, std::span<m4x4> out) const
	{
		rdr12::EvaluateAtTime(time_s, m_tracks, out);
	}
	void KeyFrameAnimation::EvaluateAtTime(double time_s, KeyFrameAnimation::Sample& out) const
	{
		out.resize(m_tracks.size());
		rdr12::EvaluateAtTime(time_s, m_tracks, out.span());
	}
	KeyFrameAnimation::Sample KeyFrameAnimation::EvaluateAtTime(double time_s) const
	{
		KeyFrameAnimation::Sample sample(m_tracks.size());
		rdr12::EvaluateAtTime(time_s, m_tracks, sample.span());
		return sample;
	}

	// Ref-counting clean up function
	void KeyFrameAnimation::RefCountZero(RefCounted<KeyFrameAnimation>* doomed)
	{
		auto anim = static_cast<KeyFrameAnimation*>(doomed);
		rdr12::Delete(anim);
	}

	// --------------------------------------------------------------------------------------------

	double AdjTime(double time_s, TimeRange time_range, EAnimStyle style)
	{
		// Relative time
		auto rtime_s = time_s - time_range.begin();

		// Wrap time into the track's time range
		switch (style)
		{
			case EAnimStyle::NoAnimation:
			{
				rtime_s = 0.0;
				break;
			}
			case EAnimStyle::Once:
			{
				rtime_s = Clamp(rtime_s, 0.0, time_range.size());
				break;
			}
			case EAnimStyle::Repeat:
			{
				rtime_s = Wrap(rtime_s, 0.0, time_range.size());
				break;
			}
			case EAnimStyle::Continuous:
			{
				rtime_s = rtime_s;
				break;
			}
			case EAnimStyle::PingPong:
			{
				rtime_s = Wrap(rtime_s, 0.0, 2.0 * time_range.size());
				rtime_s = rtime_s >= time_range.size() ? 2.0 * time_range.size() - rtime_s : rtime_s;
				break;
			}
			default:
			{
				throw std::runtime_error("Unknown animation style");
			}
		}
		
		// Convert the wrapped time back to absolute time
		return rtime_s + time_range.begin();
	}
}
