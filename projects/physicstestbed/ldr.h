//******************************************
// Ldr
//******************************************
#pragma once

#include "pr/linedrawer/plugininterface.h"
#include "PhysicsTestbed/Forwards.h"

// Wrapper class for a ldr object
struct Ldr
{
	pr::ldr::ObjectHandle m_ldr;
	std::string m_source;
	
	Ldr() :m_ldr(0)								{}
	~Ldr()										{ ldrUnRegisterObject(m_ldr); }
	Ldr& operator = (pr::ldr::ObjectHandle ldr)	{ m_ldr = ldr; return *this; }
	operator pr::ldr::ObjectHandle() const		{ return m_ldr; }
	void UpdateO2W(pr::m4x4 const& o2w)			{ if( m_ldr ) ldrSetObjectTransform(m_ldr, o2w); }
	void SetSemiTransparent(bool on)			{ if( m_ldr ) ldrSetObjectSemiTransparent(m_ldr, on); }
	void UpdateGfx(std::string const& str, bool render_on = true);
	void Render(bool on);
};

