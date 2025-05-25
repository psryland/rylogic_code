//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/model/animation.h"
#include "pr/view3d-12/utility/utility.h"

namespace pr::rdr12
{
	SimpleAnimation::SimpleAnimation()
		: m_style(EAnimStyle::NoAnimation)
		, m_period(1.0)
		, m_vel(v4Zero)
		, m_acc(v4Zero)
		, m_avel(v4Zero)
		, m_aacc(v4Zero)
	{
	}

	// Return a transform representing the offset added by this object at time 'time_s'
	m4x4 SimpleAnimation::Step(double time_s) const
	{
		auto t = 0.0;
		switch (m_style)
		{
			case EAnimStyle::NoAnimation:
			{
				return m4x4::Identity();
			}
			case EAnimStyle::Once:
			{
				t = time_s < m_period ? time_s : m_period;
				break;
			}
			case EAnimStyle::Repeat:
			{
				t = Fmod(time_s, m_period);
				break;
			}
			case EAnimStyle::Continuous:
			{
				t = time_s;
				break;
			}
			case EAnimStyle::PingPong:
			{
				t = Fmod(time_s, 2.0 * m_period) >= m_period ? m_period - Fmod(time_s, m_period) : Fmod(time_s, m_period);
				break;
			}
			default:
			{
				throw std::runtime_error("Unknown animation style");
			}
		}

		auto time = static_cast<float>(t);
		auto l = 0.5f * m_acc * Sqr(time) + m_vel * time + v4::Origin();
		auto a = 0.5f * m_aacc * Sqr(time) + m_avel * time;
		return m4x4::Transform(a, l);
	}

	// Ref-counting clean up function
	void SimpleAnimation::RefCountZero(RefCounted<SimpleAnimation>* doomed)
	{
		auto anim = static_cast<SimpleAnimation*>(doomed);
		rdr12::Delete(anim);
	}

	// --------------------------------------------------------------------------------------------

	KeyFrameAnimation::KeyFrameAnimation()
		:m_tracks()
	{
	}

	// Ref-counting clean up function
	void KeyFrameAnimation::RefCountZero(RefCounted<KeyFrameAnimation>* doomed)
	{
		auto anim = static_cast<KeyFrameAnimation*>(doomed);
		rdr12::Delete(anim);
	}
}
