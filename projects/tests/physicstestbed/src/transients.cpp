//************************************
// Collision Watch
//************************************
#include "PhysicsTestbed/Stdafx.h"
#include "PhysicsTestbed/Transients.h"

// Collision Watch impulses *******************************

CWImpulse::CWImpulse(const pr::v4& position, const pr::v4& impulse, float scale, unsigned int frame_number)
:m_frame(frame_number)
,m_position(position)
,m_impulse(impulse)
,m_ldr(0)
{
	std::string ldr_str	= MakeLdrString(scale);
	m_ldr				= ldrRegisterObject(ldr_str.c_str(), ldr_str.size());
}
CWImpulse::~CWImpulse()
{
	ldrUnRegisterObject(m_ldr);
}
bool CWImpulse::Step(unsigned int frame_number)
{
	return frame_number <= m_frame;
}
void CWImpulse::Recreate(float scale)
{
	ldrUnRegisterObject(m_ldr);
	std::string ldr_str = MakeLdrString(scale);
	m_ldr				= ldrRegisterObject(ldr_str.c_str(), ldr_str.size());
}
std::string CWImpulse::MakeLdrString(float scale)
{
	return pr::Fmt("*LineD impulse FFFFFF00 { %f %f %f  %f %f %f }"
		,m_position[0], m_position[1], m_position[2]
		,m_impulse[0] * scale,  m_impulse[1] * scale,  m_impulse[2] * scale);
}


// Collision Watch impulses *******************************

CWContact::CWContact(const pr::v4& position, const pr::v4& normal, float scale, unsigned int frame_number)
:m_frame(frame_number)
,m_position(position)
,m_normal(normal)
,m_ldr(0)
{
	std::string ldr_str = MakeLdrString(scale);
	m_ldr				= ldrRegisterObject(ldr_str.c_str(), ldr_str.size());
}
CWContact::~CWContact()
{
	ldrUnRegisterObject(m_ldr);
}
bool CWContact::Step(unsigned int frame_number)
{
	return frame_number <= m_frame + 0;
}
void CWContact::Recreate(float scale)
{
	ldrUnRegisterObject(m_ldr);
	std::string ldr_str = MakeLdrString(scale);
	m_ldr				= ldrRegisterObject(ldr_str.c_str(), ldr_str.size());
}
std::string CWContact::MakeLdrString(float scale)
{
	return pr::Fmt(	"*Line contact FF00FFFF "
					"{ "
						"%f 0 0 %f 0 0 "
						"0 %f 0 0 %f 0 "
						"0 0 0 0 0 %f "
						"*Position {%f %f %f} "
						"*Direction {2 %f %f %f} "
					"}"
		,-0.2f * scale ,0.2f * scale
		,-0.2f * scale ,0.2f * scale
		,scale
		,m_position[0] ,m_position[1], m_position[2]
		,m_normal[0], m_normal[1], m_normal[2]
		);
}

// Ray Casts *******************************

RayCast::RayCast(const pr::v4& start, const pr::v4& end, unsigned int frame_number)
:m_frame(frame_number)
,m_start(start)
,m_end(end)
,m_ldr(0)
{
	std::string ldr_str = MakeLdrString();
	m_ldr				= ldrRegisterObject(ldr_str.c_str(), ldr_str.size());
}
RayCast::~RayCast()
{
	ldrUnRegisterObject(m_ldr);
}
bool RayCast::Step(unsigned int frame_number)
{
	return frame_number <= m_frame + 0;
}
std::string RayCast::MakeLdrString()
{
	return pr::Fmt(	"*Line ray_cast FF0000FF "
					"{ "
						"%f %f %f "
						"%f %f %f "
					"}"
		,m_start.x ,m_start.y ,m_start.z
		,m_end.x ,m_end.y, m_end.z
		);
}
