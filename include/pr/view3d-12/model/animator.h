//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/model/animation.h"

namespace pr::rdr12
{
	struct Animator :RefCounted<Animator>
	{
		// Notes:
		//  - This is a base class for a type that can update 'Skinning' instances.
		//  - The idea is that this could actually be a graph of Animator derived types
		//    that all feed into one to handle state machines, blend spaces, etc.
		//  - A skinning instance has an animator. It asks the animator to update its
		//    bone transforms as needed.
		//  - Animators should be state less because one Animator might be used my multiple
		//    skinning instances.

		Animator();
		virtual ~Animator() = default;

		// Return the ID of the skeleton we're animating
		virtual uint64_t SkelId() const = 0;

		// Return the frame rate of the underlying animation
		virtual double FrameRate() const = 0;

		// The length of the underlying animation
		virtual double Duration() const = 0;

		// Apply an animation to the given bones
		virtual void Animate(std::span<m4x4> bones, double time_s, EAnimFlags flags) = 0;

		// Clone this animator
		virtual AnimatorPtr Clone() const = 0;

		// Ref-counting clean up function
		static void RefCountZero(RefCounted<Animator>* doomed);
	};

	struct Animator_KeyFrameAnimation : Animator
	{
		// Notes:
		//  - This animator reads from a single key frame animation

		KeyFrameAnimationPtr m_anim;   // The animation sequence to read from

		Animator_KeyFrameAnimation(KeyFrameAnimationPtr anim);

		// Return the ID of the skeleton we're animating
		uint64_t SkelId() const override;

		// Return the frame rate of the underlying animation
		double FrameRate() const override;
		
		// The length of the underlying animation
		double Duration() const override;

		// Apply an animation to the given bones
		void Animate(std::span<m4x4> bones, double time_s, EAnimFlags flags) override;

		// Clone this animator
		AnimatorPtr Clone() const override;
	};

	struct Animator_InterpolatedAnimation : Animator
	{
		// Interpolate sparse kinematic keys while preserving endpoint velocities and accelerations.
		using HermiteTransform = Hermite5Transform<float>;

		KinematicKeyFrameAnimationPtr m_anim; // The animation sequence to read from
		vector<HermiteTransform, 0> m_interp; // Interpolators for each track
		vector<KinematicKey, 0> m_keys; // A recycling buffer for reading key frames into
		TimeRange m_interp_time_range; // The time range of the current interpolation period

		Animator_InterpolatedAnimation(KinematicKeyFrameAnimationPtr anim);

		// Return the ID of the skeleton we're animating
		uint64_t SkelId() const override;

		// Return the frame rate of the underlying animation
		double FrameRate() const override;

		// The length of the underlying animation
		double Duration() const override;

		// Apply an animation to the given bones
		void Animate(std::span<m4x4> bones, double time_s, EAnimFlags flags) override;

		// Clone this animator
		AnimatorPtr Clone() const override;
	};

	struct Animator_PoseSnapshots : Animator
	{
		// Notes:
		//  - Plays back bone poses that the caller publishes at its own rate, e.g. a procedural animation system stepping at physics rate.
		//  - Holds the two most recent snapshots. 'Animate' interpolates between them, so poses published at a fixed step display smoothly
		//    at any render rate. Times before the older snapshot hold the older pose; times after the newer snapshot hold the newer pose.
		//  - Shows the rest pose until the first snapshot is published. The first snapshot fills both slots so nothing blends from the rest pose.
		//  - Snapshot bones are bone-to-parent transforms, in skeleton bone order, without scale.
		//  - Unlike the other animators, this one has per-object state, so each posed object needs its own instance.

		SkeletonPtr m_skeleton;          // The skeleton the snapshots are for
		int m_bone_count;                // The number of bones in each snapshot
		vector<m4x4, 0> m_bones;         // Two snapshots, older first: [0, bone_count) and [bone_count, 2*bone_count)
		double m_time[2];                // The times of the older and newer snapshots. -infinity until the first snapshot is published.

		// Start with both snapshots set to the rest pose of 'skeleton'.
		explicit Animator_PoseSnapshots(SkeletonPtr skeleton);

		// Return the ID of the skeleton we're animating
		uint64_t SkelId() const override;

		// Return the rate at which snapshots are being published (zero before two snapshots exist)
		double FrameRate() const override;

		// The time between the older and newer snapshots (zero before two snapshots exist)
		double Duration() const override;

		// Interpolate the snapshots at 'time_s'
		void Animate(std::span<m4x4> bones, double time_s, EAnimFlags flags) override;

		// Clone this animator, including its current snapshots
		AnimatorPtr Clone() const override;

		// Publish a new snapshot. The previous newest snapshot becomes the older one. 'time_s' must be later than the newest snapshot.
		void Publish(std::span<m4x4 const> bones, double time_s);
	};
}

#if PR_UNITTESTS
#include "pr/view3d-12/model/skeleton.h"
namespace pr::rdr12::tests
{
	// Verify sparse playback preserves sampled motion, including at the sequence boundaries.
	PRUnitTestClass(InterpolatedAnimationTests)
	{
		// Construct non-uniform keys from polynomial motion with nonzero endpoint derivatives.
		static KinematicKeyFrameAnimationPtr MakeAnimation()
		{
			auto anim = KinematicKeyFrameAnimationPtr{ ::pr::compute::New<KinematicKeyFrameAnimation>(1), true };
			anim->m_key_count = 3;
			anim->m_native_duration = 1.0;
			anim->m_native_frame_rate = 2.0;
			anim->m_bone_map = {0};
			anim->m_times = {0.f, 0.35f, 1.f};
			for (auto t : anim->m_times)
			{
				anim->m_position.push_back(v3(1 + 2*t + 2*t*t + t*t*t*t, 0, 0));
				anim->m_lin_vel.push_back(v3(2 + 4*t + 4*t*t*t, 0, 0));
				anim->m_lin_acc.push_back(v3(4 + 12*t*t, 0, 0));
				anim->m_rotation.push_back(quat(v4::ZAxis(), 0.1f + 0.2f*t + 0.3f*t*t + 0.1f*t*t*t));
				anim->m_ang_vel.push_back(v3(0, 0, 0.2f + 0.6f*t + 0.3f*t*t));
				anim->m_ang_acc.push_back(v3(0, 0, 0.6f + 0.6f*t));
			}
			return anim;
		}

		// Quintic playback reproduces polynomial motion when seeking forward and backward across keys.
		PRUnitTestMethod(PreservesKinematicConstraints, Quick)
		{
			auto anim = MakeAnimation();
			auto animator = Animator_InterpolatedAnimation(anim);
			auto bones = std::array<m4x4, 1>{};
			for (auto t : {1.f, 0.f, 0.1f, 0.35f, 0.6f, 0.99f, 1.f, 0.7f, 0.2f, 0.f})
			{
				animator.Animate(bones, t, EAnimFlags::None);
				auto local_time = t - s_cast<float>(animator.m_interp_time_range.begin());
				auto const& interp = animator.m_interp[0];
				PR_EXPECT(FEqlAbsolute(bones[0].pos, v4(1 + 2*t + 2*t*t + t*t*t*t, 0, 0, 1), 0.0001f));
				PR_EXPECT(FEqlAbsolute(interp.pos.EvalDerivative(local_time), v4(2 + 4*t + 4*t*t*t, 0, 0, 0), 0.0001f));
				PR_EXPECT(FEqlAbsolute(interp.pos.EvalDerivative2(local_time), v4(4 + 12*t*t, 0, 0, 0), 0.001f));
				PR_EXPECT(FEqlAbsolute(interp.rot.Eval(local_time), quat(v4::ZAxis(), 0.1f + 0.2f*t + 0.3f*t*t + 0.1f*t*t*t), 0.0001f));
				PR_EXPECT(FEqlAbsolute(interp.rot.EvalDerivative(local_time), v4(0, 0, 0.2f + 0.6f*t + 0.3f*t*t, 0), 0.0001f));
				PR_EXPECT(FEqlAbsolute(interp.rot.EvalDerivative2(local_time), v4(0, 0, 0.6f + 0.6f*t, 0), 0.001f));
			}
		}

		// Out-of-range seeks hold the endpoint pose instead of extrapolating sampled derivatives.
		PRUnitTestMethod(SeeksHoldEndpoints, Quick)
		{
			auto animator = Animator_InterpolatedAnimation(MakeAnimation());
			auto bones = std::array<m4x4, 1>{};
			animator.Animate(bones, 2.f, EAnimFlags::None);
			PR_EXPECT(FEqlAbsolute(bones[0].pos, v4(6, 0, 0, 1), 0.0001f));
			PR_EXPECT(FEqlAbsolute(s_cast<float>(animator.m_interp_time_range.begin()), 0.35f, 0.0001f));
			animator.Animate(bones, -1.f, EAnimFlags::None);
			PR_EXPECT(FEqlAbsolute(bones[0].pos, v4(1, 0, 0, 1), 0.0001f));
		}

		// Root-motion display flags do not change the underlying interpolated motion.
		PRUnitTestMethod(RootMotionFlagsPreserveCurves, Quick)
		{
			auto animator = Animator_InterpolatedAnimation(MakeAnimation());
			auto bones = std::array<m4x4, 1>{};
			animator.Animate(bones, 0.2f, EAnimFlags::NoTranslation | EAnimFlags::NoRotation);
			PR_EXPECT(FEqlAbsolute(bones[0], m4x4::Identity(), 0.0001f));
			animator.Animate(bones, 0.2f, EAnimFlags::None);
			PR_EXPECT(bones[0].pos.x > 1.f);
		}
	};

	// Verify caller-published poses are interpolated by time and held outside the snapshot interval.
	PRUnitTestClass(PoseSnapshotsTests)
	{
		// A root bone at the origin with one child bone at x = 1 in the bind pose.
		static SkeletonPtr MakeSkeleton()
		{
			auto bone_ids = std::array<uint32_t, 2>{ 0, 1 };
			auto names = std::array<string32, 2>{ "root", "child" };
			auto o2bp = std::array<m4x4, 2>{ m4x4::Identity(), m4x4::Translation(-1.f, 0.f, 0.f) };
			auto hierarchy = std::array<uint8_t, 2>{ 0, 1 };
			return SkeletonPtr{ ::pr::compute::New<Skeleton>(1U, bone_ids, names, o2bp, hierarchy), true };
		}

		// The rest pose is shown until the first snapshot is published.
		PRUnitTestMethod(RestPoseBeforePublish, Quick)
		{
			auto animator = Animator_PoseSnapshots(MakeSkeleton());
			auto bones = std::array<m4x4, 2>{};
			animator.Animate(bones, 0.0, EAnimFlags::None);
			PR_EXPECT(FEqlAbsolute(bones[0], m4x4::Identity(), 0.0001f));
			PR_EXPECT(FEqlAbsolute(bones[1], m4x4::Translation(1.f, 0.f, 0.f), 0.0001f));
			PR_EXPECT(animator.FrameRate() == 0.0);
			PR_EXPECT(animator.Duration() == 0.0);
		}

		// The first snapshot fills both slots, so no time blends from the rest pose.
		PRUnitTestMethod(FirstPublishFillsBothSlots, Quick)
		{
			auto animator = Animator_PoseSnapshots(MakeSkeleton());
			auto pose = std::array<m4x4, 2>{ m4x4::Identity(), m4x4::Translation(2.f, 0.f, 0.f) };
			animator.Publish(pose, 1.0);

			auto bones = std::array<m4x4, 2>{};
			for (auto t : { 0.0, 1.0, 5.0 })
			{
				animator.Animate(bones, t, EAnimFlags::None);
				PR_EXPECT(FEqlAbsolute(bones[1], pose[1], 0.0001f));
			}
			PR_EXPECT(animator.Duration() == 0.0);
		}

		// Times inside the interval blend rotation and position. Times outside hold the nearest snapshot.
		PRUnitTestMethod(InterpolatesAndHolds, Quick)
		{
			auto animator = Animator_PoseSnapshots(MakeSkeleton());
			auto pose0 = std::array<m4x4, 2>{ m4x4::Identity(), m4x4::Translation(2.f, 0.f, 0.f) };
			auto pose1 = std::array<m4x4, 2>{ m4x4::Identity(), m4x4::Transform(v4::ZAxis(), constants<float>::tau_by_4, v4(4, 0, 0, 1)) };
			animator.Publish(pose0, 1.0);
			animator.Publish(pose1, 1.5);
			PR_EXPECT(FEqlAbsolute(animator.Duration(), 0.5, 1e-12));
			PR_EXPECT(FEqlAbsolute(animator.FrameRate(), 2.0, 1e-12));

			auto bones = std::array<m4x4, 2>{};
			animator.Animate(bones, 1.25, EAnimFlags::None);
			PR_EXPECT(FEqlAbsolute(bones[1], m4x4::Transform(v4::ZAxis(), constants<float>::tau_by_8, v4(3, 0, 0, 1)), 0.0001f));
			animator.Animate(bones, 0.5, EAnimFlags::None);
			PR_EXPECT(FEqlAbsolute(bones[1], pose0[1], 0.0001f));
			animator.Animate(bones, 9.0, EAnimFlags::None);
			PR_EXPECT(FEqlAbsolute(bones[1], pose1[1], 0.0001f));
		}

		// A clone reproduces the source's published state.
		PRUnitTestMethod(CloneCopiesSnapshots, Quick)
		{
			auto animator = Animator_PoseSnapshots(MakeSkeleton());
			auto pose0 = std::array<m4x4, 2>{ m4x4::Identity(), m4x4::Translation(2.f, 0.f, 0.f) };
			auto pose1 = std::array<m4x4, 2>{ m4x4::Identity(), m4x4::Translation(4.f, 0.f, 0.f) };
			animator.Publish(pose0, 1.0);
			animator.Publish(pose1, 2.0);

			auto clone = animator.Clone();
			auto expected = std::array<m4x4, 2>{};
			auto actual = std::array<m4x4, 2>{};
			animator.Animate(expected, 1.5, EAnimFlags::None);
			clone->Animate(actual, 1.5, EAnimFlags::None);
			PR_EXPECT(FEqlAbsolute(actual[1], expected[1], 0.0001f));
			PR_EXPECT(clone->SkelId() == animator.SkelId());
		}

		// Root-motion flags remove the root bone's translation or rotation only.
		PRUnitTestMethod(RootMotionFlags, Quick)
		{
			auto animator = Animator_PoseSnapshots(MakeSkeleton());
			auto pose = std::array<m4x4, 2>{ m4x4::Transform(v4::ZAxis(), 0.5f, v4(1, 2, 3, 1)), m4x4::Translation(2.f, 0.f, 0.f) };
			animator.Publish(pose, 1.0);

			auto bones = std::array<m4x4, 2>{};
			animator.Animate(bones, 1.0, EAnimFlags::NoTranslation);
			PR_EXPECT(FEqlAbsolute(bones[0], m4x4::Transform(v4::ZAxis(), 0.5f, v4::Origin()), 0.0001f));
			animator.Animate(bones, 1.0, EAnimFlags::NoRotation);
			PR_EXPECT(FEqlAbsolute(bones[0], m4x4::Translation(1.f, 2.f, 3.f), 0.0001f));
			PR_EXPECT(FEqlAbsolute(bones[1], pose[1], 0.0001f));
		}
	};
}
#endif
