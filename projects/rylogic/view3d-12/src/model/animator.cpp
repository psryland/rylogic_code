//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/model/animator.h"
#include "pr/view3d-12/model/animation.h"
#include "pr/view3d-12/model/skeleton.h"

namespace pr::rdr12
{
	Animator::Animator()
	{
	}

	// Ref-counting clean up function
	void Animator::RefCountZero(RefCounted<Animator>* doomed)
	{
		auto animator = static_cast<Animator*>(doomed);
		::pr::compute::Delete<Animator>(animator);
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

	// The length of the underlying animation
	double Animator_KeyFrameAnimation::Duration() const
	{
		return m_anim->duration();
	}

	// Apply an animation to the given bones
	void Animator_KeyFrameAnimation::Animate(std::span<m4x4> bones, double time_s, EAnimFlags flags)
	{
		auto const& kfa = *m_anim.get();

		// Key frame times are stored in single precision
		auto const key_time = s_cast<float>(time_s);

		// Can evaluate a subset of the bones
		assert(isize(bones) <= kfa.track_count());
		auto EvaluateKey = [&kfa, key_time, flags, &bones](m4x4& key)
		{
			auto track_index = s_cast<int>(&key - bones.data());

			// Get the keys to interpolate between
			BoneKey keys[2];
			kfa.ReadKeys(kfa.TimeToKeyIndex(key_time), track_index, keys);

			// Interpolate between key frames
			auto dt = keys[1].m_time - keys[0].m_time;
			auto time = Clamp(key_time, keys[0].m_time, keys[1].m_time);
			auto frac = dt != 0 ? Frac(keys[0].m_time, time, keys[1].m_time) : 0.f;

			// Interpolate the key
			auto interp_key = Interp(keys[0], keys[1], frac, keys[0].m_interp);
			if (AllSet(flags, EAnimFlags::NoTranslation) && track_index == RootBoneTrack)
				interp_key.m_pos = v3::Zero();
			if (AllSet(flags, EAnimFlags::NoRotation) && track_index == RootBoneTrack)
				interp_key.m_rot = quat::Identity();

			// Type convert
			key = interp_key;
		};

		// Evaluate each bone
		constexpr size_t ParallelizeCount = 10;
		if (bones.size() >= ParallelizeCount)
			std::for_each(std::execution::par_unseq, std::begin(bones), std::end(bones), EvaluateKey);
		else
			std::for_each(std::execution::seq, std::begin(bones), std::end(bones), EvaluateKey);
	}

	// Clone this animator
	AnimatorPtr Animator_KeyFrameAnimation::Clone() const
	{
		return AnimatorPtr{ ::pr::compute::New<Animator_KeyFrameAnimation>(m_anim), true };
	}

	// -----------------------------------------------------------------------------------------------

	Animator_InterpolatedAnimation::Animator_InterpolatedAnimation(KinematicKeyFrameAnimationPtr anim)
		: Animator()
		, m_anim(anim)
		, m_interp(anim->track_count())
		, m_keys(2 * anim->track_count())
		, m_interp_time_range(1.f, 1.f)
	{
		Animate({}, 0.0, EAnimFlags::None);
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

	// The length of the underlying animation
	double Animator_InterpolatedAnimation::Duration() const
	{
		return m_anim->duration();
	}

	// Apply an animation to the given bones
	void Animator_InterpolatedAnimation::Animate(std::span<m4x4> bones, double time_s, EAnimFlags flags)
	{
		auto const& kkfa = *m_anim.get();

		// Clamp seeks to the sequence and retain the final interval at its endpoint rather than extrapolating an earlier curve.
		auto const key_time = s_cast<float>(Clamp(time_s, 0.0, kkfa.duration()));
		if (m_interp_time_range.empty() || key_time < m_interp_time_range.begin() || key_time > m_interp_time_range.end())
		{
			auto kidx = std::min(kkfa.TimeToKeyIndex(key_time), std::max(0, kkfa.key_count() - 2));
			auto tcount = kkfa.track_count();
			assert(m_keys.size() >= 2 * tcount && "Need 2 keys per track");

			// Read the keys that span the time 'time_s'
			kkfa.ReadKeys(kidx, m_keys);

			// Record the time range
			m_interp_time_range.set(m_keys[0].m_time, m_keys[tcount].m_time);
			auto interval = !m_interp_time_range.empty() ? s_cast<float>(m_interp_time_range.size()) : 1.f;

			// Preserve sampled derivatives at every key, including the beginning and end of the sequence.
			int i0 = 0, i1 = tcount;
			for (auto& interp : m_interp)
			{
				interp = HermiteTransform(
					m_keys[i0].m_pos.w1(), m_keys[i0].m_lin_vel.w0(), m_keys[i0].m_lin_acc.w0(),
					m_keys[i0].m_rot, m_keys[i0].m_ang_vel.w0(), m_keys[i0].m_ang_acc.w0(),
					m_keys[i1].m_pos.w1(), m_keys[i1].m_lin_vel.w0(), m_keys[i1].m_lin_acc.w0(),
					m_keys[i1].m_rot, m_keys[i1].m_ang_vel.w0(), m_keys[i1].m_ang_acc.w0(),
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
			auto time0 = s_cast<float>(m_interp_time_range.begin());
			auto rot = interp.rot.Eval(key_time - time0);
			auto pos = interp.pos.Eval(key_time - time0);

			// Apply root motion flags
			if (AllSet(flags, EAnimFlags::NoTranslation) && track_index == RootBoneTrack)
				pos = v4::Origin();
			if (AllSet(flags, EAnimFlags::NoRotation) && track_index == RootBoneTrack)
				rot = quat::Identity();

			// Update the bone
			bone = m4x4{ToMatrix<m3x3>(rot), pos};
		}
	}

	// Clone this animator
	AnimatorPtr Animator_InterpolatedAnimation::Clone() const
	{
		return AnimatorPtr{ ::pr::compute::New<Animator_InterpolatedAnimation>(m_anim), true };
	}

	// -----------------------------------------------------------------------------------------------

	Animator_PoseSnapshots::Animator_PoseSnapshots(SkeletonPtr skeleton)
		: Animator()
		, m_skeleton(skeleton)
		, m_bone_count(skeleton->BoneCount())
		, m_bones(2 * skeleton->BoneCount())
		, m_time{ -std::numeric_limits<double>::infinity(), -std::numeric_limits<double>::infinity() }
	{
		// Derive the rest pose bone-to-parent transforms from the skeleton's inverse bind pose: b2p = o2bp(parent) * bp2o.
		skeleton->WalkHierarchy<m4x4>([this, &o2bp = skeleton->m_o2bp](int idx, m4x4 const* parent_o2bp) -> m4x4
		{
			// Store the rest transform in both snapshots
			auto b2o = InvertOrthonormal(o2bp[idx]);
			m_bones[idx] = parent_o2bp ? *parent_o2bp * b2o : b2o;
			m_bones[m_bone_count + idx] = m_bones[idx];
			return o2bp[idx];
		});
	}

	// Return the ID of the skeleton we're animating
	uint64_t Animator_PoseSnapshots::SkelId() const
	{
		return m_skeleton->Id();
	}

	// Return the rate at which snapshots are being published
	double Animator_PoseSnapshots::FrameRate() const
	{
		// Infinite times before the first publication produce a NaN interval, so test for a positive interval explicitly
		auto interval = m_time[1] - m_time[0];
		return interval > 0 ? 1.0 / interval : 0.0;
	}

	// The time between the older and newer snapshots
	double Animator_PoseSnapshots::Duration() const
	{
		// Infinite times before the first publication produce a NaN interval, so test for a positive interval explicitly
		auto interval = m_time[1] - m_time[0];
		return interval > 0 ? interval : 0.0;
	}

	// Interpolate the snapshots at 'time_s'
	void Animator_PoseSnapshots::Animate(std::span<m4x4> bones, double time_s, EAnimFlags flags)
	{
		assert(isize(bones) <= m_bone_count && "More bones requested than the skeleton has");

		// Find the blend weight of the newer snapshot. Hold the end poses outside the snapshot interval rather than extrapolating.
		auto interval = m_time[1] - m_time[0];
		auto frac = interval > 0 ? s_cast<float>(Clamp((time_s - m_time[0]) / interval, 0.0, 1.0)) : 1.f;

		// Blend each requested bone
		for (int i = 0, iend = isize(bones); i != iend; ++i)
		{
			// Interpolate rotation and position between the snapshots
			auto bone = Slerp(m_bones[i], m_bones[m_bone_count + i], frac);

			// Apply root motion flags
			if (AllSet(flags, EAnimFlags::NoTranslation) && i == RootBoneTrack)
				bone.pos = v4::Origin();
			if (AllSet(flags, EAnimFlags::NoRotation) && i == RootBoneTrack)
				bone.rot = m3x3::Identity();

			bones[i] = bone;
		}
	}

	// Clone this animator, including its current snapshots
	AnimatorPtr Animator_PoseSnapshots::Clone() const
	{
		// Copy the snapshot state into a new animator for the same skeleton
		auto clone = RefPtr<Animator_PoseSnapshots>{ ::pr::compute::New<Animator_PoseSnapshots>(m_skeleton), true };
		clone->m_bones = m_bones;
		clone->m_time[0] = m_time[0];
		clone->m_time[1] = m_time[1];
		return clone;
	}

	// Publish a new snapshot
	void Animator_PoseSnapshots::Publish(std::span<m4x4 const> bones, double time_s)
	{
		assert(isize(bones) == m_bone_count && "A snapshot must contain every bone in the skeleton");
		assert(time_s > m_time[1] && "Snapshots must be published in increasing time order");

		// The first snapshot fills both slots so that no blending from the rest pose is shown
		auto first = m_time[1] == -std::numeric_limits<double>::infinity();
		auto older = std::span{ m_bones }.subspan(0, m_bone_count);
		auto newer = std::span{ m_bones }.subspan(m_bone_count, m_bone_count);
		if (first)
		{
			std::ranges::copy(bones, older.begin());
			m_time[0] = time_s;
		}
		else
		{
			std::ranges::copy(newer, older.begin());
			m_time[0] = m_time[1];
		}

		// Record the new snapshot as the newest
		std::ranges::copy(bones, newer.begin());
		m_time[1] = time_s;
	}
}
