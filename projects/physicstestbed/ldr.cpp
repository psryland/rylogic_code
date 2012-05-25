//******************************************
// Ldr
//******************************************

#include "PhysicsTestbed/Stdafx.h"
#include "PhysicsTestbed/Ldr.h"
#include "PhysicsTestbed/PhysicsTestbed.h"

void Ldr::UpdateGfx(std::string const& str, bool render_on)
{
	Render(false);
	m_source = str;
	Render(render_on);
}

void Ldr::Render(bool on)
{
	if( on == (m_ldr != 0) )
		return;
	
	if( m_ldr )
	{
		Testbed().PushHookState(EHookType_DeleteObjects, false);
		ldrUnRegisterObject(m_ldr); m_ldr = 0;
		Testbed().PopHookState(EHookType_DeleteObjects);
	}
	else
	{
		m_ldr = ldrRegisterObject(m_source.c_str(), m_source.size());
		if( m_ldr ) ldrSetObjectUserData(m_ldr, this);
	}
}