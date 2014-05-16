//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_PHYSICS_IMATERIAL_H
#define PR_PHYSICS_IMATERIAL_H

#include "pr/physics/types/forward.h"

namespace pr
{
	namespace ph
	{
		struct IMaterial
		{
			virtual ~IMaterial() {}
			virtual const ph::Material& GetMaterial(MaterialId material_id) const = 0;
		};

		// Assign the material interface to use. This must remain in
		// scope for the lifetime of the physics engine
		void RegisterMaterials(const ph::IMaterial* material_interface);
		
		// Return a physics material from an id
		const ph::Material& GetMaterial(MaterialId material_id);

	}
}

#endif
