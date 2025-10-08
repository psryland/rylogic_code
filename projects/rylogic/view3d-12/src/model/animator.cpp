//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/model/animator.h"
#include "pr/view3d-12/model/animation.h"
#include "pr/view3d-12/utility/utility.h"

namespace pr::rdr12
{
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

	Animator_SingleKeyFrameAnimation::Animator_SingleKeyFrameAnimation(KeyFrameAnimationPtr anim)
		: Animator()
		, m_anim(anim)
	{
	}

	// Return the ID of the skeleton we're animating
	uint64_t Animator_SingleKeyFrameAnimation::SkelId() const
	{
		return m_anim->m_skel_id;
	}

	// Return the frame rate of the underlying animation
	double Animator_SingleKeyFrameAnimation::FrameRate() const
	{
		return m_anim->frame_rate();
	}

	// Apply an animation to the given bones
	void Animator_SingleKeyFrameAnimation::Animate(std::span<m4x4> bones, float time_s)
	{
		m_anim->EvaluateAtTime(time_s, bones);
	}

	// -----------------------------------------------------------------------------------------------

	Animator_InterpolatedAnimation::Animator_InterpolatedAnimation(KinematicKeyFrameAnimationPtr anim)
		: Animator()
		, m_anim(anim)
	{
	}

	// Return the ID of the skeleton we're animating
	uint64_t Animator_InterpolatedAnimation::SkelId() const
	{
		return m_anim->m_skel_id;
	}

	// Return the frame rate of the underlying animation
	double Animator_InterpolatedAnimation::FrameRate() const
	{
		return m_anim->m_frame_rate;
	}

	// Apply an animation to the given bones
	void Animator_InterpolatedAnimation::Animate(std::span<m4x4> bones, float time_s)
	{
		(void)bones, time_s;
		throw std::runtime_error("not implemented");
		//m_anim->EvaluateAtTime(time_s, bones);
	}
}

