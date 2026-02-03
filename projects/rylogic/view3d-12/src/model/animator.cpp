//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/model/animator.h"
#include "pr/view3d-12/model/animation.h"
#include "pr/view3d-12/utility/utility.h"

namespace pr::rdr12
{
	static constexpr int RootBoneTrack = 0;

	Animator::Animator()
	{
	}

	// Ref-counting clean up function
	void Animator::RefCountZero(RefCounted<Animator>* doomed)
	{
		auto animator = static_cast<Animator*>(doomed);
		rdr12::Delete<Animator>(animator);
	}

	// -----------------------------------------------------------------------------------------------

	Animator_KeyFrameAnimation::Animator_KeyFrameAnimation(KeyFrameAnimationPtr anim)
		: Animator()
		, m_anim(anim)
	{
	}

	// Return the ID of the skeleton we're animating
	uint64_t Animator_KeyFrameAnimation::SkelId() const
	{
		return m_anim->m_skel_id;
	}

	// Return the frame rate of the underlying animation
	double Animator_KeyFrameAnimation::FrameRate() const
	{
		return m_anim->frame_rate();
	}

	// Returns the interpolated key frame a 'time_s'
	template <typename Key> requires (std::is_assignable_v<Key, BoneKey>)
	static void EvaluateAtTime(KeyFrameAnimation const& kfa, float time_s, EAnimFlags flags, std::span<Key> out)
	{
		// Can evaluate a subset of the bones
		assert(isize(out) <= kfa.track_count());
		auto EvaluateKey = [&kfa, time_s, flags, &out](Key& key)
		{
			auto track_index = s_cast<int>(&key - out.data());

			// Get the keys to interpolate between
			BoneKey keys[2];
			kfa.ReadKeys(kfa.TimeToKeyIndex(time_s), track_index, keys);

			// Interpolate between key frames
			auto dt = keys[1].m_time - keys[0].m_time;
			auto time = Clamp(time_s, keys[0].m_time, keys[1].m_time);
			auto frac = dt != 0 ? Frac(keys[0].m_time, time, keys[1].m_time) : 0.f;

			// Interpolate the key
			auto interp_key = Interp(keys[0], keys[1], frac, keys[0].m_interp);
			if (AllSet(flags, EAnimFlags::NoRootTranslation) && track_index == RootBoneTrack)
				interp_key.m_pos = v3::Zero();
			if (AllSet(flags, EAnimFlags::NoRootRotation) && track_index == RootBoneTrack)
				interp_key.m_rot = quat::Identity();

			// Type convert
			key = interp_key;
		};

		// Evaluate each bone
		constexpr size_t ParallelizeCount = 10;
		if (out.size() >= ParallelizeCount)
			std::for_each(std::execution::par_unseq, std::begin(out), std::end(out), EvaluateKey);
		else
			std::for_each(std::execution::seq, std::begin(out), std::end(out), EvaluateKey);
	}

	// Apply an animation to the given bones
	void Animator_KeyFrameAnimation::Animate(std::span<m4x4> bones, float time_s, EAnimFlags flags)
	{
		EvaluateAtTime(*m_anim.get(), time_s, flags, bones);
	}

	// -----------------------------------------------------------------------------------------------

	Animator_InterpolatedAnimation::Animator_InterpolatedAnimation(KinematicKeyFrameAnimationPtr anim)
		: Animator()
		, m_anim(anim)
		, m_interp(anim->track_count())
		, m_keys(2 * anim->track_count())
		, m_time_range(1.f, 1.f)
	{
		Animate({}, 0.f, EAnimFlags::None);
	}

	// Return the ID of the skeleton we're animating
	uint64_t Animator_InterpolatedAnimation::SkelId() const
	{
		return m_anim->m_skel_id;
	}

	// Return the frame rate of the underlying animation
	double Animator_InterpolatedAnimation::FrameRate() const
	{
		return m_anim->m_native_frame_rate;
	}

	// Apply an animation to the given bones
	void Animator_InterpolatedAnimation::Animate(std::span<m4x4> bones, float time_s, EAnimFlags flags)
	{
		// If 'time_s' is outside the current interpolation interval, update the interpolators
		// Unless, the time is beyond the end of the animation and the interpolators are already updated.
		if (!m_time_range.contains(time_s) && TimeRange(0, m_anim->duration()).contains(time_s))
		{
			auto kidx = m_anim->TimeToKeyIndex(time_s);
			auto tcount = m_anim->track_count();
			auto kcount = m_anim->key_count();
			assert(m_keys.size() >= 2 * tcount && "Need 2 keys per track");

			// Read the keys that span the time 'time_s'
			m_anim->ReadKeys(kidx, m_keys);

			// Record the time range
			m_time_range.set(m_keys[0].m_time, m_keys[tcount].m_time);
			auto interval = !m_time_range.empty() ? s_cast<float>(m_time_range.size()) : 1.f;

			// Update the interpolators
			int i0 = 0, i1 = tcount;
			for (auto& interp : m_interp)
			{
				auto first = m_keys[i0].m_idx == 0;
				auto last = m_keys[i1].m_idx == kcount - 1;

				interp.rot = InterpolateRotation(
					m_keys[i0].m_rot, !first ? m_keys[i0].m_ang_vel.w0() : v4::Zero(),
					m_keys[i1].m_rot, !last ? m_keys[i1].m_ang_vel.w0() : v4::Zero(),
					interval
				);
				interp.pos = InterpolateVector(
					m_keys[i0].m_pos.w1(), !first ? m_keys[i0].m_lin_vel.w0() : v4::Zero(),
					m_keys[i1].m_pos.w1(), !last ? m_keys[i1].m_lin_vel.w0() : v4::Zero(),
					interval
				);
				++i0;
				++i1;
			}
		}

		// Read the bone transforms from the interpolators
		for (auto& bone : bones)
		{
			auto track_index = s_cast<int>(&bone - bones.data());
			auto& interp = m_interp[track_index];

			// Evaluate the interpolators
			auto time0 = s_cast<float>(m_time_range.begin());
			auto rot = interp.rot.Eval(time_s - time0);
			auto pos = interp.pos.Eval(time_s - time0);

			// Apply root motion flags
			if (AllSet(flags, EAnimFlags::NoRootTranslation) && track_index == RootBoneTrack)
				pos = v4::Origin();
			if (AllSet(flags, EAnimFlags::NoRootRotation) && track_index == RootBoneTrack)
				rot = quat::Identity();

			// Update the bone
			bone = m4x4(m3x4(rot), pos);
		}
	}
}

