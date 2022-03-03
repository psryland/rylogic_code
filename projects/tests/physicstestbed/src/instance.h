//******************************************
// Instance
//******************************************
#pragma once

#include "PhysicsTestbed/Forwards.h"

#if PHYSICS_ENGINE==REFLECTIONS_PHYSICS
#include "Shared/Source/Crossplatform/Scenedata/Instancestate.h"
typedef std::vector<static_inst::Instance>	TInstData;

// Static instances
class InstanceDataEx
{
public:
	InstanceDataEx();
	void Clear();
	void Add(const pr::m4x4& i2w);

private:
	static_inst::InstanceState		m_inst_state;
	static_inst::InstanceDataHeader m_inst_data_header;
	TInstData						m_instance_data;
};

#endif
