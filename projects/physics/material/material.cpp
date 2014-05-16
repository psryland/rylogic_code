//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#include "physics/utility/stdafx.h"
#include "physics/utility/Assert.h"
#include "physics/utility/Debug.h"
#include "pr/Physics/Material/Material.h"
#include "pr/Physics/Material/IMaterial.h"

using namespace pr;
using namespace pr::ph;

namespace pr
{
	namespace ph
	{
		// Default materials
		//	Material density in kg/m^3
		//	Co-efficient of static friction: 0 = no friction, 1 = infinite friction
		//	Co-efficient of dynamic friction: 0 = no friction, 1 = infinite friction
		//	Co-efficient of rolling friction: 0 = no friction, 1 = infinite friction
		//	Co-efficient of elasticity aka restitution: 0 = inelastic, 1 = completely elastic.
		//  Co-efficient of tangential elasticity: -1.0f = bounces forward (frictionless), 0.0f = bounces up, 1.0f = bounces back
		//  Co-efficient of tortional elasticity: -1.0f = normal ang mom unchanged (frictionless), 0.0f = normal ang mom goes to zero, 1.0f = normal ang mom reversed
		const ph::Material g_default_materials[] = 
		{
			//{1.0f, 0.7f, 0.7f, 0.05f, 0.6f, 0.0f, -0.95f}
			{1.0f, 0.5f, 0.5f, 0.0f, 0.5f, 0.0f, -1.0f}
		};

		struct DefaultMaterialInterface : IMaterial
		{
			DefaultMaterialInterface()
			:m_physics_materials	(g_default_materials)
			,m_num_physics_materials(sizeof(g_default_materials)/sizeof(g_default_materials[0]))
			{}
			const ph::Material& GetMaterial(MaterialId material_id) const
			{
				PR_ASSERT(PR_DBG_PHYSICS, material_id < m_num_physics_materials, "Physics material index out of range");
				return m_physics_materials[material_id];
			}
			const ph::Material* m_physics_materials;
			std::size_t		m_num_physics_materials;
		} g_default_material_interface;

		// A global material interface pointer
		const ph::IMaterial* g_material_interface = &g_default_material_interface;

	}//namespace ph
}//namespace pr

// Assign the materials to use. This must remain in
// scope for the lifetime of the physics engine
void pr::ph::RegisterMaterials(const ph::IMaterial* material_interface)
{
	g_material_interface = material_interface;
}

// Return a physics material from an index
const pr::ph::Material& pr::ph::GetMaterial(MaterialId material_id)
{
	const pr::ph::Material& mat = g_material_interface->GetMaterial(material_id);
	PR_ASSERT(PR_DBG_PHYSICS, IsFinite(mat), "");
	return mat;
}
