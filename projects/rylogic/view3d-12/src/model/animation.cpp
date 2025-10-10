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

	KeyFrameAnimation::KeyFrameAnimation(uint32_t skel_id, double duration, double native_frame_rate)
		: m_skel_id(skel_id)
		, m_duration(duration)
		, m_native_frame_rate(native_frame_rate)
		, m_bone_map()
		, m_rotation()
		, m_position()
		, m_scale()
	{}
	
	// Number of bone tracks in this animation
	int KeyFrameAnimation::bone_count() const
	{
		return isize(m_bone_map);
	}

	// Number of keys in this animation
	int KeyFrameAnimation::key_count() const
	{
		auto count = s_cast<int>(
			!m_rotation.empty() ? m_rotation.size() :
			!m_position.empty() ? m_position.size() :
			!m_scale   .empty() ? m_scale   .size() :
			bone_count());

		assert(count % bone_count() == 0 && "Expect track length to be a multiple of the bone count");
		return count / bone_count();
	}

	// The effective frame rate implied by the duration and number of keys
	double KeyFrameAnimation::frame_rate() const
	{
		return (key_count() - 1) / m_duration;
	}
	
	// Get the keys on either side of 'time_s' (to interpolate between)
	std::tuple<BoneKey, BoneKey> KeyFrameAnimation::Key(float time_s, int bone_index) const
	{
		auto bcount = bone_count();
		auto kcount = key_count();
		auto period = s_cast<float>(1.0 / frame_rate());

		// Convert the time into a frame number.
		// Note, by scaling 'm_duration' or 'time_s' the playback rate of the animation can be changed.
		auto kidx0 = Lerp(0, kcount - 1, Frac<double>(0.0, time_s, m_duration));
		auto kidx1 = kidx0 + 1; // Note: +1 before clamping
		kidx0 = Clamp(kidx0, 0, kcount - 1);
		kidx1 = Clamp(kidx1, 0, kcount - 1);

		BoneKey key0 = {
			.m_rot = !m_rotation.empty() ? m_rotation[kidx0 * bcount + bone_index] : quat::Identity(),
			.m_pos = !m_position.empty() ? m_position[kidx0 * bcount + bone_index] : v3::Zero(),
			.m_scl = !m_scale.empty() ? m_scale[kidx0 * bcount + bone_index] : v3::One(),
			.m_time = std::floor(time_s / period) * period,
			.m_idx = kidx0,
		};
		BoneKey key1 = {
			.m_rot = !m_rotation.empty() ? m_rotation[kidx1 * bcount + bone_index] : quat::Identity(),
			.m_pos = !m_position.empty() ? m_position[kidx1 * bcount + bone_index] : v3::Zero(),
			.m_scl = !m_scale.empty() ? m_scale[kidx1 * bcount + bone_index] : v3::One(),
			.m_time = key0.m_time + period,
			.m_idx = kidx1,
		};
		return { key0, key1 };
	}

	// Sample each track at 'time_s'
	template <typename Key> requires (std::is_assignable_v<Key, BoneKey>)
	static void EvaluateAtTime(float time_s, KeyFrameAnimation const& anim, std::span<Key> out)
	{
		// For each bone...
		assert(anim.bone_count() == out.size());
		std::for_each(std::execution::par_unseq, std::begin(out), std::end(out), [&](Key& key)
		{
			// Get the keys to interpolate between
			auto bone_index = s_cast<int>(&key - out.data());
			auto [key0, key1] = anim.Key(time_s, bone_index);

			// Interpolate between key frames
			auto frac =
				time_s <= key0.m_time ? 0.0f :
				time_s >= key1.m_time ? 1.0f :
				Frac(key0.m_time, time_s, key1.m_time);

			key = Interp(key0, key1, frac, key0.m_interp);
		});
	}

	// Returns the linearly interpolated key frames a 'time_s'
	void KeyFrameAnimation::EvaluateAtTime(float time_s, std::span<m4x4> out) const
	{
		rdr12::EvaluateAtTime(time_s, *this, out);
	}
	void KeyFrameAnimation::EvaluateAtTime(float time_s, KeyFrameAnimation::Sample& out) const
	{
		out.resize(bone_count());
		rdr12::EvaluateAtTime(time_s, *this, out.span());
	}
	KeyFrameAnimation::Sample KeyFrameAnimation::EvaluateAtTime(float time_s) const
	{
		Sample sample(bone_count());
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
