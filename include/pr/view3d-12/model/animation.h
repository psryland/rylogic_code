//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12
{
	// Notes:
	//  - Animation has these parts:
	//    - KeyFrameAnimation = a buffer of Tracks (one per bone), each containing KeyFrames at specific times.
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

	// A transform key frame
	struct KeyFrame
	{
		// Should really compress this...
		quat m_rotation;
		v4 m_translation;
		v4 m_scale;
		double m_time;
		uint64_t m_flags;

		// Convert to an affine transform
		operator m4x4() const
		{
			return m4x4(m3x4(m_rotation) * m3x4::Scale(m_scale.x, m_scale.y, m_scale.z), m_translation);
		}

		// Identity key frame
		static KeyFrame Identity()
		{
			return KeyFrame {
				.m_rotation = quat::Identity(),
				.m_translation = v4::Origin(),
				.m_scale = v4::Zero(),
				.m_time = 0.0,
				.m_flags = 0,
			};
		}

		// Linearly interpolate between two key frames
		friend KeyFrame Lerp(KeyFrame const& lhs, KeyFrame const& rhs, float frac)
		{
			return KeyFrame{
				.m_rotation = Slerp(lhs.m_rotation, rhs.m_rotation, frac),
				.m_translation = pr::Lerp(lhs.m_translation, rhs.m_translation, frac),
				.m_scale = pr::Lerp(lhs.m_scale, rhs.m_scale, frac),
				.m_time = pr::Lerp(lhs.m_time, rhs.m_time, frac),
				.m_flags = 0
			};
		}
	};

	// 2nd order polynomial animation
	struct SimpleAnimation : RefCounted<SimpleAnimation>
	{
		v4         m_vel;    // Linear velocity of the animation in m/s
		v4         m_acc;    // Linear velocity of the animation in m/s
		v4         m_avel;   // Angular velocity of the animation in rad/s
		v4         m_aacc;   // Angular velocity of the animation in rad/s
		double     m_period; // Seconds
		EAnimStyle m_style;  // The animation style

		SimpleAnimation();

		// Return a transform representing the offset added by this object at time 'time_s'
		m4x4 EvaluateAtTime(double time_s) const;

		// Ref-counting clean up function
		static void RefCountZero(RefCounted<SimpleAnimation>* doomed);
	};

	// Animation using key frame data
	struct KeyFrameAnimation : RefCounted<KeyFrameAnimation>
	{
		// Notes:
		//  - Sample is a vertical slice of the tracks for each key at a time.
		//  - Track is the keys as a function of time.

		using Sample = pr::vector<KeyFrame>;
		using Track = pr::vector<KeyFrame>;
		using Tracks = pr::vector<Track>;

		uint64_t m_skel_id; // The skeleton that this animation is intended for (mainly for debugging)
		EAnimStyle m_style; // The animation style
		Tracks m_tracks;    // A track for each skeleton bone

		KeyFrameAnimation(uint64_t skel_id, EAnimStyle style);

		// Returns the linearly interpolated key frames a 'time_s'
		Sample EvaluateAtTime(double time_s) const;

		// Ref-counting clean up function
		static void RefCountZero(RefCounted<KeyFrameAnimation>* doomed);
	};
}
