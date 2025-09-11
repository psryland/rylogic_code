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
		auto lin = 0.5f * m_acc * Sqr(time) + m_vel * time + v4::Origin();
		auto ang = RotationAt(time, m3x4::Identity(), m_avel, m_aacc);
		return m4x4{ ang, lin };
	}

	// Ref-counting clean up function
	void RootAnimation::RefCountZero(RefCounted<RootAnimation>* doomed)
	{
		auto anim = static_cast<RootAnimation*>(doomed);
		rdr12::Delete(anim);
	}

	// --------------------------------------------------------------------------------------------

	// Get the bone at the given frame index
	BoneKey BoneTrack::operator[](int frame_index) const
	{
		return BoneKey{
			.m_rot = m_rotation.empty() ? quat::Identity() : m_rotation[std::clamp(frame_index, 0, isize(m_rotation) - 1)],
			.m_pos = m_position.empty() ? v4::Origin() : m_position[std::clamp(frame_index, 0, isize(m_position) - 1)].w1(),
			.m_scale = m_scale.empty() ? v4::One() : m_scale[std::clamp(frame_index, 0, isize(m_scale) - 1)].w0(),
			.m_timekey = m_times.empty()
				? TimeKey(frame_index, m_frame_rate) // If the times channel is empty, assume one frame per 1/fps
				: m_times[std::clamp(frame_index, 0, isize(m_times) - 1)],
			.m_kf_index = frame_index,
		};
	}

	// Get the bone key at a given time
	BoneKey BoneTrack::operator[](double time_s) const
	{
		auto kf = m_times.empty()
			? s_cast<int>(std::floor(time_s * m_frame_rate)) // If the times channel is empty, assume one frame per 1/fps
			: s_cast<int>(std::distance(begin(m_times), std::ranges::lower_bound(m_times, time_s, {}, [](TimeKey const& tk) { return static_cast<double>(tk.m_time); })));

		return (*this)[kf];
	}

	// --------------------------------------------------------------------------------------------

	KeyFrameAnimation::KeyFrameAnimation(uint64_t skel_id, TimeRange time_range, double frame_rate, int bone_count)
		: m_skel_id(skel_id)
		, m_time_range(time_range)
		, m_frame_rate(frame_rate)
		, m_bone_count(bone_count)
		, m_offsets()
		, m_times()
		, m_rotation()
		, m_position()
		, m_scale()
	{}

	// Return the animation data for a single bone
	BoneTrack KeyFrameAnimation::Track(int bone_index) const
	{
		assert(bone_index >= 0 && bone_index < isize(m_offsets) - 1);
		auto i0 = s_cast<size_t>(m_offsets[bone_index + 0]);
		auto i1 = s_cast<size_t>(m_offsets[bone_index + 1]);
		return BoneTrack{
			.m_times = m_times.empty() ? m_times.span() : m_times.span(i0 , i1 - i0),
			.m_rotation = m_rotation.empty() ? m_rotation.span() : m_rotation.span(i0 , i1 - i0),
			.m_position = m_position.empty() ? m_position.span() : m_position.span(i0 , i1 - i0),
			.m_scale = m_scale.empty() ? m_scale.span() : m_scale.span(i0 , i1 - i0),
			.m_frame_rate = m_frame_rate,
			.m_bone_index = bone_index,
		};
	}

	// Sample each track at 'time_s'
	template <typename Key> requires (std::is_assignable_v<Key, BoneKey>)
	static void EvaluateAtTime(float time_s, KeyFrameAnimation const& anim, std::span<Key> out)
	{
		// For each bone...
		assert(anim.m_bone_count == out.size());
		std::for_each(std::execution::par_unseq, std::begin(out), std::end(out), [&](Key& key)
		{
			auto bone_index = s_cast<int>(&key - out.data());

			// Read the track for this bone
			auto track = anim.Track(bone_index);

			// Get the keys to interpolate between
			auto bonekey0 = track[time_s];
			auto bonekey1 = track[bonekey0.m_kf_index + 1];

			// Interpolate between key frames
			auto frac =
				time_s <= bonekey0.m_timekey.m_time ? 0.0f :
				time_s >= bonekey1.m_timekey.m_time ? 1.0f :
				Frac(bonekey0.m_timekey.m_time, time_s, bonekey1.m_timekey.m_time);

			key = Interp(bonekey0, bonekey1, frac, bonekey0.m_timekey.m_interp);
		});
	}

	// Returns the linearly interpolated key frames a 'time_s'
	void KeyFrameAnimation::EvaluateAtTime(float time_s, std::span<m4x4> out) const
	{
		rdr12::EvaluateAtTime(time_s, *this, out);
	}
	void KeyFrameAnimation::EvaluateAtTime(float time_s, KeyFrameAnimation::Sample& out) const
	{
		out.resize(m_bone_count);
		rdr12::EvaluateAtTime(time_s, *this, out.span());
	}
	KeyFrameAnimation::Sample KeyFrameAnimation::EvaluateAtTime(float time_s) const
	{
		Sample sample(m_bone_count);
		rdr12::EvaluateAtTime(time_s, *this, sample.span());
		return sample;
	}

	// Ref-counting clean up function
	void KeyFrameAnimation::RefCountZero(RefCounted<KeyFrameAnimation>* doomed)
	{
		auto anim = static_cast<KeyFrameAnimation*>(doomed);
		rdr12::Delete(anim);
	}

	// --------------------------------------------------------------------------------------------

	KinematicKeyFrameAnimation::KinematicKeyFrameAnimation(uint64_t skel_id, TimeRange time_range, double frame_rate)
		: m_skel_id(skel_id)
		, m_rotation()
		, m_position()
		, m_scale()
		, m_velocity()
		, m_ang_vel()
		, m_accel()
		, m_ang_accel()
		, m_time_range(time_range)
		, m_frame_rate(frame_rate)
	{}

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
