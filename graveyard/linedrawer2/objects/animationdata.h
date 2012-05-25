#ifndef ANIMATIONDATA_H
#define ANIMATIONDATA_H

#include "pr/maths/maths.h"

struct AnimationData
{
	enum Style
	{
		NoAnimation,	
		PlayOnce,
		PlayReverse,
		PingPong,
		PlayContinuous,
		NumberOf
	};

	AnimationData()
	:m_style		(NoAnimation)
	,m_period		(0)
	,m_velocity		(v4Zero)
	,m_rotation_axis(v4Zero)
	,m_angular_speed(0.0f)
	{}

	Style	m_style;
	float	m_period;			// Seconds
	v4		m_velocity;			// Linear velocity of the animation in m/s
	v4		m_rotation_axis;	// Axis of rotation
	float	m_angular_speed;	// Angular speed of the animation rad/s
};

#endif//ANIMATIONDATA_H