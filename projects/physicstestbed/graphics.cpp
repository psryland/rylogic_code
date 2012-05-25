//******************************************
// Static
//******************************************
#include "PhysicsTestbed/Stdafx.h"
#include "PhysicsTestbed/Graphics.h"
#include "PhysicsTestbed/Parser.h"
#include "PhysicsTestbed/ParseOutput.h"

Graphics::Graphics(const parse::Gfx& gfx)
{
	m_ldr = ldrRegisterObject(gfx.m_ldr_str.c_str(), gfx.m_ldr_str.size());
	ldrSetObjectUserData(m_ldr, this);
}

Graphics::~Graphics()
{
	ldrUnRegisterObject(m_ldr);
}
