//****************************************************
//
//	Physics Polyhedron
//
//****************************************************
// Polyhedron collision model

#ifndef PR_PHYSICS_POLYHEDRON_H
#define PR_PHYSICS_POLYHEDRON_H

#include "PR/Common/StdVector.h"
#include "PR/Maths/Maths.h"
#include "PR/Physics/PhysicsAssertEnable.h"
#include "PR/Physics/Types.h"

namespace pr
{
	namespace ph
	{
		struct Polyhedron
		{
			//BoundingBox BBox() const;				// Returns a bounding box orientated to the primitive
			float		Volume() const;				// Returns the volume of the polyhedron
			v4			CenterOfMass() const;		// Returns the centre of mass for the polyhedron
			m4x4		InertiaTensor() const;		// Returns the inertia tensor for the polyhedron
		//	v4			MomentOfInertia() const;	// Returns the moments of inertia about the primary axes for the primitive (x by mass for the mass moment of inertia)

			// Triangle soup representation
			std::vector<v4>	m_vertex;				// Array of triangles
		//	EPrimitive	m_type;						// The type of primitive this is. One of EPrimitive
		//	float		m_radius[3];				// The dimensions of the primitive in primitive space
		//	m4x4		m_primitive_to_model;		// Transform from primitive space to physics object space
		//	std::size_t	m_material_index;			// The physics material that this primitive is made out of
		};
	}//namespace ph
}//namespace pr

#endif//PR_PHYSICS_POLYHEDRON_H

