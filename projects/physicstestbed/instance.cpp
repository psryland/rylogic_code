//******************************************
// Instance
//******************************************
#include "PhysicsTestbed/Stdafx.h"
#include "PhysicsTestbed/Instance.h"

#if PHYSICS_ENGINE==REFLECTIONS_PHYSICS

InstanceDataEx::InstanceDataEx()
{
	Clear();
}

// Re-enable all instances and reset the instance count
void InstanceDataEx::Clear()
{
	m_inst_state.m_disabled.ClearAll();
	m_instance_data.clear();
	m_inst_data_header.m_instances			= 0;
	m_inst_data_header.m_num_instancesX		= 0;
	m_inst_data_header.m_region_id			= ri::GlobalRegionId();
	m_inst_data_header.m_physics_only		= m_inst_data_header.m_num_instancesX;
	m_inst_data_header.m_anim_static		= m_inst_data_header.m_num_instancesX;
	m_inst_data_header.m_smashable			= m_inst_data_header.m_num_instancesX;
	m_inst_data_header.m_anim_smashable		= m_inst_data_header.m_num_instancesX;
	m_inst_data_header.m_non_physical		= m_inst_data_header.m_num_instancesX;
	m_inst_data_header.m_anim_non_physical	= m_inst_data_header.m_num_instancesX;
}

// Add an instance to the instance data
void InstanceDataEx::Add(const pr::m4x4& i2w)
{
	static_inst::Instance new_inst;
	new_inst.SetTransform				(m4x4_to_mam4(i2w));
	new_inst.SetModelHandle				(PI_HANDLE(0, 0));
	new_inst.SetNextGroupInstanceIndex	(0);
	new_inst.SetInstanceType			(static_inst::EType_Building);
	
	m_instance_data						.push_back(new_inst);
	m_inst_data_header.m_instances		= &m_instance_data[0];
	m_inst_data_header.m_num_instancesX	= (ri::uint16_t)m_instance_data.size();
}

#endif

