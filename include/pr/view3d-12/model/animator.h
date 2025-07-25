//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

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

		// Apply an animation to the given bones
		virtual void Animate(std::span<m4x4> bones, double time_s) = 0;

		// Ref-counting clean up function
		static void RefCountZero(RefCounted<Animator>* doomed);
	};

	struct Animator_SingleKeyFrameAnimation : Animator
	{
		// Notes:
		//  - This animator reads from a single key frame animation

		KeyFrameAnimationPtr m_anim;   // The animation sequence to read from

		Animator_SingleKeyFrameAnimation(KeyFrameAnimationPtr anim);

		// Return the ID of the skeleton we're animating
		uint64_t SkelId() const override;

		// Return the frame rate of the underlying animation
		double FrameRate() const override;

		// Apply an animation to the given bones
		void Animate(std::span<m4x4> bones, double time_s) override;
	};

	struct Animator_InterpolatedAnimation : Animator
	{
		// Notes:
		//  - This animator reads from a single key frame animation

		KinematicKeyFrameAnimationPtr m_anim; // The animation sequence to read from

		Animator_InterpolatedAnimation(KinematicKeyFrameAnimationPtr anim);

		// Return the ID of the skeleton we're animating
		uint64_t SkelId() const override;

		// Return the frame rate of the underlying animation
		double FrameRate() const override;

		// Apply an animation to the given bones
		void Animate(std::span<m4x4> bones, double time_s) override;
	};
}
