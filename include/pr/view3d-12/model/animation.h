﻿//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12
{
	// Notes:
	//  - Animation has these parts:
	//    - KeyFrameAnimation = a buffer of KeyFrames where each KeyFrame contains transforms for each bone in a skeleton.
	//        KeyFrames can be sparse.
	//        The transform at each key frame is the *local transform* of that bone — that is: the transform from
	//        the parent bone’s space to this bone’s space at that time. Therefore, in the rest pose (i.e., bind pose),
	//        these transforms match the skeleton's local bone transforms.
	//    - Skeleton = Source data describing a hierarchy of bone transforms. Bone transforms are parent relative
	//        with the root bone in object space.
	//    - Pose = A runtime skeleton instance, updated by an Animator using interpolated transforms from an Animation.
	//        The pose transform array is used in the shader to skin the model so the transforms must be from object
	//        space to deformed object space. They are then transformed using o2w into world space.
	//    - Skin = A set of bones indices and weights for each unique vertex of a model.
	//        Since model verts can be duplicated because of different normals, UVs, etc, each vert in the vertex buffer
	//        should have an 'original vert index' value. This is used to look up the skin vert which then gives the
	//        bone indices and weights.
	//    - Animator = The class that determines the pose at a given time. Animator is intended to be a base class that
	//        might one day support blend spaces or other things. For now, it just interpolates KeyFrameAnimation instances.
	//
	//  - Terms:
	//    - An animation with frame rate 'FPS' has 'time_length / FPS' frames.
	//    - Only some frames are 'KeyFrames' (aka Keys). An animation can have N KeyFrames != FrameCount.
	//    - For looped animations, the last KeyFrame should match the first KeyFrame
	//
	//  - Graphics Models contain a skin because the skin is 1:1 with the model and doesn't change.
	//  - Instances contain a pose because poses change with time and can use the same model but at different animation times.
	//  - Poses reference an Animator, a Skeleton, and an animation time. Multiple Instances can reference the same pose.
	//    Think of a Pose as a dynamic instance of a skeleton.
	//  - A skeleton can be referenced by many poses. A skeleton is basically static data that animations are relative to.
	//  - An Animator is used when updating the transforms in a Pose. The Animator interpolates the bone offsets which are
	//    then used when calculating the pose's bone-to-object space transforms.
	//  - Model hierarchies need to include a transform from child model to root model space, because the pose transforms
	//    are the same for all models in the hierarchy, i.e., in object space.

	// Simple animation styles
	enum class EAnimStyle : uint8_t
	{
		// Animation stays as if at time = 0
		NoAnimation,

		// Animation plays through to the end then stops
		Once,

		// Animation plays through to the end, then jumps back to the start
		Repeat,

		// Same as repeat, except that the root motion continues from the end
		Continuous,

		// Animation bounces from start to end to start continuously
		PingPong,
	};

	// Different methods for interpolating between key-frames
	enum class EAnimInterpolation
	{
		Constant = 0,
		Linear = 1,
		Cubic = 2,
	};

	// Data at a KeyFrame time
	struct TimeKey
	{
		float m_time;         // In seconds
		EAnimInterpolation m_interp;

		TimeKey() = default;
		TimeKey(float time_s, EAnimInterpolation interp)
			: m_time(time_s)
			, m_interp(interp)
		{}
		TimeKey(int frame_index, double frame_rate, EAnimInterpolation interp = EAnimInterpolation::Linear)
			: m_time(s_cast<float>(frame_index / frame_rate))
			, m_interp(interp)
		{}
		friend std::partial_ordering operator <=>(TimeKey lhs, TimeKey rhs)
		{
			return lhs.m_time <=> rhs.m_time;
		}
	};

	// Transient type for a bone transform
	struct BoneKey
	{
		quat m_rot;
		v4 m_pos;
		v4 m_scale;
		TimeKey m_timekey;
		int m_kf_index;

		// Convert to an affine transform
		operator m4x4() const
		{
			return m4x4(m3x4(m_rot) * m3x4::Scale(m_scale.x, m_scale.y, m_scale.z), m_pos);
		}

		// Interpolate between two key frames
		friend BoneKey Interp(BoneKey const& lhs, BoneKey const& rhs, float frac, EAnimInterpolation interp)
		{
			switch (interp)
			{
				case EAnimInterpolation::Constant: frac = 0; break;
				case EAnimInterpolation::Linear: frac = frac; break;
				case EAnimInterpolation::Cubic: frac = SmoothStep(0.0f, 1.0f, frac); break;
				default: throw std::runtime_error("Unknown interpolation style");
			}
			return BoneKey{
				.m_rot = Slerp(lhs.m_rot, rhs.m_rot, frac),
				.m_pos = Lerp(lhs.m_pos, rhs.m_pos, frac),
				.m_scale = Lerp(lhs.m_scale, rhs.m_scale, frac),
				.m_timekey = TimeKey(Lerp(lhs.m_timekey.m_time, rhs.m_timekey.m_time, frac), EAnimInterpolation::Linear)
			};
		}
	};

	// The animation data for a bone
	struct BoneTrack
	{
		std::span<TimeKey const> m_times;
		std::span<quat const> m_rotation;
		std::span<v3 const> m_position;
		std::span<v3 const> m_scale;
		double m_frame_rate; // Only used if m_times.empty()
		int m_bone_index;

		// Get the bone key (transform) at the given frame index
		BoneKey operator[](int frame_index) const;

		// Get the latest bone key (transform) <= 'time_s'
		BoneKey operator[](double time_s) const;
	};

	// A single key frame that includes kinematic data
	struct KinematicKeyFrame
	{
		quat m_rotation;
		v3 m_translation;
		v3 m_scale;
		v3 m_velocity;
		v3 m_ang_velocity;
		v3 m_acceleration;
		v3 m_ang_accel;
		double m_time;
		uint64_t m_flags;

		// Convert to an affine transform
		operator m4x4() const
		{
			return m4x4(m3x4(m_rotation) * m3x4::Scale(m_scale.x, m_scale.y, m_scale.z), m_translation.w1());
		}

		// Get/Set the interpolation style of this key frame
		EAnimInterpolation Interpolation() const
		{
			return static_cast<EAnimInterpolation>(GrabBits<int, uint64_t>(m_flags, 2, 0));
		}
		void Interpolation(EAnimInterpolation interp)
		{
			m_flags = PackBits<int>(m_flags, static_cast<int>(interp), 2, 0);
		}

		// Identity key frame
		static KinematicKeyFrame Identity()
		{
			return KinematicKeyFrame {
				.m_rotation = quat::Identity(),
				.m_translation = v3::Zero(),
				.m_scale = v3::One(),
				.m_velocity = v3::One(),
				.m_ang_velocity = v3::One(),
				.m_acceleration = v3::One(),
				.m_ang_accel = v3::One(),
				.m_time = 0.0,
				.m_flags = 0,
			};
		}

		// Linearly interpolate between two key frames
		friend KinematicKeyFrame Interp(KinematicKeyFrame const& lhs, KinematicKeyFrame const& rhs, float frac)
		{
			switch (lhs.Interpolation())
			{
				case EAnimInterpolation::Constant: frac = 0; break;
				case EAnimInterpolation::Linear: frac = frac; break;
				case EAnimInterpolation::Cubic: frac = SmoothStep(0.0f, 1.0f, frac); break;
				default: throw std::runtime_error("Unknown interpolation style");
			}
			return KinematicKeyFrame{
				.m_rotation = Slerp(lhs.m_rotation, rhs.m_rotation, frac),
				.m_translation = Lerp(lhs.m_translation, rhs.m_translation, frac),
				.m_scale = Lerp(lhs.m_scale, rhs.m_scale, frac),
				.m_velocity = Lerp(lhs.m_velocity, rhs.m_velocity, frac),
				.m_ang_velocity = Lerp(lhs.m_velocity, rhs.m_velocity, frac),
				.m_acceleration = Lerp(lhs.m_velocity, rhs.m_velocity, frac),
				.m_ang_accel = Lerp(lhs.m_velocity, rhs.m_velocity, frac),
				.m_time = Lerp(lhs.m_time, rhs.m_time, frac),
				.m_flags = 0,
			};
		}
	};

	// 2nd order polynomial animation
	struct RootAnimation : RefCounted<RootAnimation>
	{
		v4         m_vel;    // Linear velocity of the animation in m/s
		v4         m_acc;    // Linear velocity of the animation in m/s
		v4         m_avel;   // Angular velocity of the animation in rad/s
		v4         m_aacc;   // Angular velocity of the animation in rad/s
		double     m_period; // Time range in seconds
		EAnimStyle m_style;  // The animation style

		RootAnimation();

		// Return a transform representing the offset added by this object at time 'time_s'
		m4x4 EvaluateAtTime(double time_s) const;

		// Ref-counting clean up function
		static void RefCountZero(RefCounted<RootAnimation>* doomed);
	};

	// Animation using key frame data
	struct KeyFrameAnimation : RefCounted<KeyFrameAnimation>
	{
		// Notes:
		//  - Bone transform data are stored concatenated for each bone, e.g.,
		//       m_rotation: [(bone0,,,)(bone1,,,)(bone2,,)...]
		//       m_position: [(bone0,,,)(bone1,,,)(bone2,,)...]
		//       m_scale:    [(bone0,,,)(bone1,,,)(bone2,,)...]
		//  - Each (boneN...) is a "track", not all tracks are the same length. However, the length of a bone's track is the same in each array.
		//  - Some arrays can be empty if unused. Track lengths only apply to non-empty arrays
		//  - '[m_offsets[bone_index], m_offsets[bone_index+1])' is the index range into the arrays for a bone.

		//  - 'Sample' is a key frame for each bone at some time, t.
		//  - Frame spacing is not necessarily fixed.
		//  - Tracks store contiguous frames, where each frame is an array of length 'm_bone_count'.
		//  - This arrangement is easier to move/copy, less fragmented, and allows for some tracks being empty.
		//  - Any of the tracks can be empty. The lengths will be either equal or zero.
		using Sample = vector<BoneKey, 0>;

		uint64_t m_skel_id;         // The skeleton that this animation is intended for (mainly for debugging)
		TimeRange m_time_range;     // The time range spanned by this animation
		double m_frame_rate;        // The native frame rate of the animation, so we can convert from frames <-> seconds
		int m_bone_count;           // The number of bones per key frame

		// Tracks
		vector<int, 0> m_offsets;   // Index offsets to the start of each bone track (one for each bone in the skeleton)
		vector<TimeKey, 0> m_times; // The key frame time for each entry. If empty, assume a key every 1/fps.
		vector<quat, 0> m_rotation;
		vector<v3, 0> m_position;
		vector<v3, 0> m_scale;

		KeyFrameAnimation(uint64_t skel_id, TimeRange time_range, double frame_rate, int bone_count);

		// Return the animation data for a single bone
		BoneTrack Track(int bone_index) const;

		// Returns the interpolated key frames a 'time_s'
		void EvaluateAtTime(float time_s, std::span<m4x4> out) const;
		void EvaluateAtTime(float time_s, Sample& out) const;
		Sample EvaluateAtTime(float time_s) const;

		// Ref-counting clean up function
		static void RefCountZero(RefCounted<KeyFrameAnimation>* doomed);
	};

	// Animation data where each key frame also contains velocities and accelerations
	struct KinematicKeyFrameAnimation : RefCounted<KinematicKeyFrameAnimation>
	{
		using Vec3Track = pr::vector<v3,0>;

		// Any of these tracks can be empty
		uint64_t m_skel_id;     // The skeleton that this animation is intended for (mainly for debugging)
		Vec3Track m_rotation;   // Rotation data per frame. Compressed normalised quaternion
		Vec3Track m_position;   // Bone position data per frame.
		Vec3Track m_scale;      // Bone scale data per frame.
		Vec3Track m_velocity;   // Linear velocity per frame.
		Vec3Track m_ang_vel;    // Angular velocity per frame.
		Vec3Track m_accel;      // Acceleration per frame.
		Vec3Track m_ang_accel;  // Angular acceleration per frame.
		TimeRange m_time_range; // The time range spanned by this animation
		double m_frame_rate;    // The native frame rate of the animation, so we can convert from frames <-> seconds

		KinematicKeyFrameAnimation(uint64_t skel_id, TimeRange time_range, double frame_rate);
	};

	// Use 'style' to adjust 'time_s' so that it is within the given time range
	double AdjTime(double time_s, TimeRange time_range, EAnimStyle style);

	// Convert a frame range to a time range based on the given frame rate
	inline TimeRange ToTimeRange(FrameRange frames, double frame_rate)
	{
		assert(frame_rate > 0.0);
		return TimeRange {
			static_cast<double>(frames.m_beg / frame_rate),
			static_cast<double>(frames.m_end / frame_rate),
		};
	}

	// Convert a frame range to a time range based on the given frame rate
	inline FrameRange ToFrameRange(TimeRange times, double frame_rate)
	{
		assert(frame_rate > 0.0);
		return FrameRange {
			static_cast<int>(times.m_beg * frame_rate),
			static_cast<int>(times.m_end * frame_rate),
		};
	}
}
