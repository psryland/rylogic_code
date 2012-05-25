//******************************************
// Graphics
//******************************************
#pragma once

#include "PhysicsTestbed/Forwards.h"
#include "pr/linedrawer/plugininterface.h"

// Graphics instances
class Graphics
{
public:
	Graphics(const parse::Gfx& gfx);
	~Graphics();

	pr::ldr::ObjectHandle	m_ldr;
};
typedef std::map<pr::ldr::ObjectHandle, Graphics*> TGraphics;
