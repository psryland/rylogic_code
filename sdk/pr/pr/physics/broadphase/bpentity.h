//*********************************************
// Physics engine
//  Copyright © Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_PH_BROADPHASE_ENTITY_H
#define PR_PH_BROADPHASE_ENTITY_H

#include "pr/physics/types/forward.h"
#include "pr/physics/broadphase/ibroadphase.h"

namespace pr
{
	namespace ph
	{
		// To add an object to the broadphase it must contain one of these objects.
		// The client must fill in the members shown
		struct BPEntity
		{
			// Members to be filled in by the client
			void*        m_owner;  // Pointer to the object that contains this broadphase::Entity
			BBox* m_bbox;   // Pointer to a bounding box representing the object within the broadphase

			// Extra data used by the broadphase this entity belongs to
			IBroadphase* m_broadphase;

			///<summary>Helper method for initialising this object</summary>
			template <typename Owner> void         init(Owner& owner, BBox& bbox) { m_owner = &owner; m_bbox = &bbox; m_broadphase = 0; }
			template <typename Owner> Owner const& owner() const                         { return *reinterpret_cast<Owner const*>(m_owner); }
			template <typename Owner> Owner&       owner()                               { return *reinterpret_cast<Owner*      >(m_owner); }
			void Update()                                                                { if (m_broadphase) { m_broadphase->Update(*this); } }
		};
	}
}

#endif
