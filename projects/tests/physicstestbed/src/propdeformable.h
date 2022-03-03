//***************************************
// Deformable prop
//***************************************
#pragma once

#include "PhysicsTestbed/Forwards.h"
#include "PhysicsTestbed/ParseOutput.h"
#include "PhysicsTestbed/Ldr.h"
#include "PhysicsTestbed/Prop.h"
#include "PhysicsTestbed/DeformableModel.h"
#include "PhysicsTestbed/Skeleton.h"

class PropDeformable : public Prop
{
public:
	PropDeformable(parse::Output const& output, parse::PhysObj const& phys, PhysicsEngine& engine);
	void UpdateGraphics();
	void Step(float step_size);
	void ExportTo(pr::Handle& file, bool physics_scene) const;
	void OnCollision(col::Data const& col_data);

private:
	PhysicsEngine*	m_engine;
	parse::PhysObj	m_phys;
	DeformableModel	m_deform;
	Ldr				m_skel_ldr;				// Graphics for the skeleton
	bool			m_deformed;
	bool			m_generate_col_models;
};