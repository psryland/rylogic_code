//******************************************
// Prop
//******************************************
#pragma once

#include "PhysicsTestbed/Forwards.h"
#include "PhysicsTestbed/ParseOutput.h"
#include "PhysicsTestbed/Prop.h"
#include "PhysicsTestbed/Skeleton.h"

class PropRigidbody : public Prop
{
public:
	PropRigidbody(parse::Output const& output, parse::PhysObj const& phys, PhysicsEngine& engine);
	void Step(float step_size);
	void ExportTo(pr::Handle& file, bool physics_scene) const;
	void OnCollision(col::Data const& col_data);

private:
	parse::PhysObj			m_phys;			// Used by ExportTo()
	parse::Model			m_model;		// Used by ExportTo()
	//Skeleton				m_skel;			// Skeleton for the model
};
