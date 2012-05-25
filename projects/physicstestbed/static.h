//******************************************
// Static
//******************************************
#pragma once

#include "PhysicsTestbed/Forwards.h"
#include "PhysicsTestbed/ParseOutput.h"
#include "PhysicsTestbed/CollisionModel.h"
#include "pr/linedrawer/plugininterface.h"

// Static instances
class Static
{
public:
	Static(parse::Output const& output, parse::Static const& statik, PhysicsEngine& engine);
	~Static();

	pr::m4x4				InstToWorld() const	{ return m_static.m_inst_to_world; }
	pr::BoundingBox			Bounds() const		{ return InstToWorld() * m_static.m_bbox; }
	CollisionModel const&	ColModel() const	{ return m_col_model; }

	pr::ldr::ObjectHandle	m_ldr;

private:
	CollisionModel	m_col_model;
	parse::Static	m_static;				// Used by ExportTo()
};
typedef std::map<pr::ldr::ObjectHandle, Static*> TStatic;
