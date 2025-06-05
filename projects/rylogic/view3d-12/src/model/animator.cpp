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

	// Apply an animation to the given bones
	void Animator_SingleKeyFrameAnimation::Animate(std::span<m4x4> bones, double time_s)
	{
		auto sample = m_anim->EvaluateAtTime(time_s);
		if (ssize(sample) != ssize(bones))
			throw std::runtime_error("Mismatch between animation and bones array. Likely due to mismatched skeletons");

		for (int i = 0, iend = isize(sample); i != iend; ++i)
			bones[i] = sample[i];
	}
}
