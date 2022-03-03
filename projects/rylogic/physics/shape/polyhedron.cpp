//****************************************************
//
//	Physics Primitive
//
//****************************************************
// Primitives that a collision model is made out of
// Sphere:		m_radius[0] - radius of the sphere, other radii ignored
// Cylinder:	m_radius[0] - radius of the cylinder, m_radius[2] - half height of the cylinder (along Z)
// Box:			m_radius[0], m_radius[1], m_radius[2] - half lengths of the box edges

#include "PR/Physics/Utility/Stdafx.h"
#include "PR/Physics/PhysicsAssertEnable.h"
#include "PR/Physics/Model/Polyhedron.h"

using namespace pr;
using namespace pr::ph;

////*****
//// Returns a bounding box orientated to the primitive
//BoundingBox Polyhedron::BBox() const
//{
//	switch( m_type )
//	{
//	case EPrimitive_Box:		return BoundingBox::construct(-m_radius[0], -m_radius[1], -m_radius[2], m_radius[0], m_radius[1], m_radius[2]);
//	case EPrimitive_Cylinder:	return BoundingBox::construct(-m_radius[0], -m_radius[0], -m_radius[2], m_radius[0], m_radius[0], m_radius[2]);
//	case EPrimitive_Sphere:		return BoundingBox::construct(-m_radius[0], -m_radius[0], -m_radius[0], m_radius[0], m_radius[0], m_radius[0]);
//	default:					PR_ERROR_STR(PR_DBG_PHYSICS, "Unknown primitive type");
//	}
//	return BoundingBox();
//}

//*****
// Return the volume of the polyhedron
float Polyhedron::Volume() const
{
	PR_ASSERT(PR_DBG_PHYSICS, (m_vertex.size() % 3) == 0);

	float volume = 0;
	std::vector<v4>::const_iterator v = m_vertex.begin();
	for( std::size_t i = 0, i_end = m_vertex.size() / 3; i != i_end; ++i )
	{
		m4x4 tetra;
		tetra.x	= *v++;
		tetra.y	= *v++;
		tetra.z	= *v++;
		tetra.w	= v4Zero;
		volume  += tetra.Determinant3();
	}
	return volume / 6.0f;
}

//*****
// Return the centre of mass of the polyhedron
v4 Polyhedron::CenterOfMass() const
{
	PR_ASSERT(PR_DBG_PHYSICS, (m_vertex.size() % 3) == 0);

	float volume		= 0;
	v4 centre_of_mass	= v4Zero;
	std::vector<v4>::const_iterator v = m_vertex.begin();
	for( std::size_t i = 0, i_end = m_vertex.size() / 3; i != i_end; ++i )
	{
		m4x4 tetra;
		tetra.x			= *v++;
		tetra.y			= *v++;
		tetra.z			= *v++;
		tetra.w			= v4Zero;
		float vol		= tetra.Determinant3();		// Dont bother dividing by 6 
		volume			+= vol;
		centre_of_mass	+= vol * (tetra.x + tetra.y + tetra.z);	// Divide by 4 at end
	}
	centre_of_mass /= volume * 4.0f; 
	centre_of_mass.w = 0.0f;
	return centre_of_mass;
}

//*****
// Return the inertia tensor for the polyhedron
m4x4 Polyhedron::InertiaTensor() const
{
	// count is the number of triangles (tris) 
	// The moments are calculated based on the center of rotation which is [0,0,0]
	// Assume mass == 1.0, you can multiply by mass later.
	// For improved accuracy the next 3 variables, the determinant vol, and its calculation should be changed to double
	float	volume = 0;								// Technically this variable accumulates the volume times 6
	v4		diagonal_integrals = v4Zero;			// Accumulate matrix main diagonal integrals [x*x, y*y, z*z]
	v4		off_diagonal_integrals= v4Zero;			// Accumulate matrix off-diagonal  integrals [y*z, x*z, x*y]
	std::vector<v4>::const_iterator v = m_vertex.begin();
	for( std::size_t i = 0, i_end = m_vertex.size() / 3; i != i_end; ++i )
	{
		m4x4 tetra;
		tetra.x			= *v++;
		tetra.y			= *v++;
		tetra.z			= *v++;
		tetra.w			= v4Zero;
		float vol		= tetra.Determinant3();		// Dont bother dividing by 6 
		volume			+= vol;
		
		for( int j = 0; j != 3; ++j )
		{
			int j1 = (j + 1) % 3;   
			int j2 = (j + 2) % 3;   

			diagonal_integrals[j] += (
				tetra.x[j] * tetra.y[j] +
				tetra.y[j] * tetra.z[j] +
				tetra.z[j] * tetra.x[j] + 
				tetra.x[j] * tetra.x[j] +
				tetra.y[j] * tetra.y[j] +
				tetra.z[j] * tetra.z[j]
				) * vol;		// Divide by 60.0f later

			off_diagonal_integrals[j] += (
				tetra.x[j1] * tetra.y[j2] +
				tetra.y[j1] * tetra.z[j2] +
				tetra.z[j1] * tetra.x[j2] +
				tetra.x[j1] * tetra.z[j2] +
				tetra.y[j1] * tetra.x[j2] +
				tetra.z[j1] * tetra.y[j2] +
				tetra.x[j1] * tetra.x[j2] * 2 +
				tetra.y[j1] * tetra.y[j2] * 2 +
				tetra.z[j1] * tetra.z[j2] * 2
				) * vol;		// Divide by 120.0f later
		}
	}

	diagonal_integrals		/= volume * (60.0f  / 6.0f);  // Divide by total volume (vol/6) since density = 1 / volume
	off_diagonal_integrals	/= volume * (120.0f / 6.0f);
	
	return m4x4::construct(
		diagonal_integrals.y + diagonal_integrals.z  , -off_diagonal_integrals.z                   , -off_diagonal_integrals.y                 , 0,
		-off_diagonal_integrals.z                    , diagonal_integrals.x + diagonal_integrals.z , -off_diagonal_integrals.x                 , 0,
		-off_diagonal_integrals.y                    , -off_diagonal_integrals.x                   , diagonal_integrals.x+diagonal_integrals.y , 0,
		0,0,0,0);
}
