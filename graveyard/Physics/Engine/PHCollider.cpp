//**********************************************************************************
//
//	A class for doing collision detection between orientated geometric objects
//
//**********************************************************************************

#include "PR/Physics/Physics.h"
#include "PR/Physics/Engine/PHCollision.h"
#include "PR/Physics/Engine/PHCollider.h"

using namespace pr;
using namespace pr::ph;

//*****
// Calculates the most likely point of contact between two orientated objects
Collider::Collider(Params& params)
{
	// Check for coincident objectes
	if( FEql((params.m_objectB.m_centre - params.m_objectA.m_centre).Length3Sq(), 0.0f) )
	{
		PR_ERROR_STR(PR_DBG_PHYSICS, "Two objectes exactly on top of each other");
		params.m_min_overlap.m_penetration = -1.0f;
		return;
	}

	// Find the extent on each of the separating axes
	PR_ASSERT(PR_DBG_PHYSICS, params.m_min_overlap.m_penetration > 0.0f);
	for( uint i = 0; i < params.m_num_separating_axes; ++i )
	{
		GetMinOverlap(params.m_separating_axis[i], params.m_objectA, params.m_objectB, params.m_min_overlap);
		if( params.m_min_overlap.m_penetration < 0.0f ) return; // No collision
	}

	// Find the point of contact based on the overlap type: F2F, E2F, E2E, C2F
	GetPointOfContact(params.m_objectA, params.m_objectB, params.m_min_overlap);
	
//PSR...	// Save the contact point (converted into the local space of objectA and objectB)
//PSR...	min_overlap.m_A.m_point	-= objectA.m_centre;
//PSR...	min_overlap.m_B.m_point	-= objectB.m_centre;
//PSR...	contact.m_pointA		.Set(	objectA.m_normal[0].Dot3(min_overlap.m_A.m_point),
//PSR...									objectA.m_normal[1].Dot3(min_overlap.m_A.m_point),
//PSR...									objectA.m_normal[2].Dot3(min_overlap.m_A.m_point),
//PSR...									1.0f);
//PSR...	contact.m_pointB		.Set(	objectB.m_normal[0].Dot3(min_overlap.m_B.m_point),
//PSR...									objectB.m_normal[1].Dot3(min_overlap.m_B.m_point),
//PSR...									objectB.m_normal[2].Dot3(min_overlap.m_B.m_point),
//PSR...									1.0f);
//PSR...	contact.m_normal		= min_overlap.m_axis;
//PSR...	contact.m_penetration	= min_overlap.m_penetration;
}

//*****
// This function finds the overlap of 'objectA' and 'objectB' when projected on 'axis'.
// If the overlap is less than 'min_overlap.m_penetration' then 'min_overlap' is updated
void Collider::GetMinOverlap(const v4& axis, const Collider::Info& objectA, const Collider::Info& objectB, Collider::Overlap& min_overlap)
{
	PR_ASSERT(PR_DBG_PHYSICS, objectA.m_num_radii == objectB.m_num_radii);
	Overlap overlap;
	overlap.m_axis			= axis;
	overlap.m_penetration	= -Dot3(axis, objectB.m_centre - objectA.m_centre);
	overlap.m_A.m_point		= objectA.m_centre;
	overlap.m_B.m_point		= objectB.m_centre;
	overlap.m_A.m_type		= Overlap::Point;
	overlap.m_B.m_type		= Overlap::Point;
	if( overlap.m_penetration > 0.0f )
	{
		overlap.m_axis			= -overlap.m_axis;
		overlap.m_penetration	= -overlap.m_penetration;
	}

	// Find the nearest points
	for( uint j = 0; j < objectA.m_num_radii; ++j )
	{
		// ObjectA
		float distA = Dot3(overlap.m_axis, objectA.m_radius[j]);
		if( FEql(distA, 0.0f) )
		{
			overlap.m_A.m_DOF[j]	= true;
			++overlap.m_A.m_type;
		}
		else if( distA > 0.0f )
		{
			overlap.m_A.m_DOF[j]	= false;
			overlap.m_A.m_point		+= objectA.m_radius[j];
			overlap.m_penetration	+= distA;
		}
		else // distA < 0.0f
		{
			overlap.m_A.m_DOF[j]	= false;
			overlap.m_A.m_point		-= objectA.m_radius[j];
			overlap.m_penetration	-= distA;
		}
		
		// ObjectB
		float distB = Dot3(overlap.m_axis, objectB.m_radius[j]);
		if( FEql(distB, 0.0f) )
		{
			overlap.m_B.m_DOF[j]	= true;
			++overlap.m_B.m_type;
		}
		else if( distB > 0.0f )
		{
			overlap.m_B.m_DOF[j]	= false;
			overlap.m_B.m_point		-= objectB.m_radius[j];
			overlap.m_penetration	+= distB;
		}
		else
		{
			overlap.m_B.m_DOF[j]	= false;
			overlap.m_B.m_point		+= objectB.m_radius[j];
			overlap.m_penetration	-= distB;
		}

		// If we're already penetrating more than 'min_penetration' then
		// there's no point in carrying on.
		if( overlap.m_penetration >= min_overlap.m_penetration )
		{
			return;
		}
	}

	// This must be a shallower penetration than 'min_overlap'
	min_overlap = overlap;
}

//*****
// This function takes the points A and B in 'min_overlap' and
// adjusts them to the mostly likely point of contact based on the
// constraints described in radii.
void Collider::GetPointOfContact(const Collider::Info& objectA, const Collider::Info& objectB, Collider::Overlap& min_overlap)
{
	switch( min_overlap.m_A.m_type )
	{
	case Overlap::Point:
		switch( min_overlap.m_B.m_type )
		{
		case Overlap::Point: PointToPoint(objectA, objectB, min_overlap); break;
		case Overlap::Edge:	 PointToEdge (objectA, objectB, min_overlap); break;
		case Overlap::Face:	 PointToFace (objectA, objectB, min_overlap); break;
		default: PR_ERROR_STR(PR_DBG_PHYSICS, "Unknown overlap type");
		} break;
	case Overlap::Edge:
		switch( min_overlap.m_B.m_type )
		{
		case Overlap::Point:
			min_overlap.Reverse();
			PointToEdge(objectA, objectB, min_overlap);
			min_overlap.Reverse();
			break;
		case Overlap::Edge: EdgeToEdge(objectA, objectB, min_overlap); break;
		case Overlap::Face:	EdgeToFace(objectA, objectB, min_overlap); break;
		default: PR_ERROR_STR(PR_DBG_PHYSICS, "Unknown overlap type");
		}break;
	case Overlap::Face:
		switch( min_overlap.m_B.m_type )
		{
		case Overlap::Point:
			min_overlap.Reverse();
			PointToFace(objectB, objectA, min_overlap);
			min_overlap.Reverse();
			break;
		case Overlap::Edge:
			min_overlap.Reverse();
			EdgeToFace(objectB, objectA, min_overlap);
			min_overlap.Reverse();
			break;
		case Overlap::Face: FaceToFace(objectA, objectB, min_overlap); break;
		default: PR_ERROR_STR(PR_DBG_PHYSICS, "Unknown overlap type");
		}break;
	default: PR_ERROR_STR(PR_DBG_PHYSICS, "Unknown overlap type");
	};
}

//*****
// Adjust point to point collisions.
void Collider::PointToPoint(const Collider::Info& objectA, const Collider::Info& objectB, Collider::Overlap& overlap)
{
	objectA;
	objectB;
	overlap;
}

void Collider::PointToEdge (const Collider::Info& objectA, const Collider::Info& objectB, Collider::Overlap& overlap)
{
	objectA;
	objectB;
	overlap;
}

//*****
// Point vs. Face: Move point B onto the axis that passes through point A
void Collider::PointToFace (const Collider::Info& objectA, const Collider::Info& objectB, Collider::Overlap& overlap)
{
	objectA;
	objectB;
	PR_ASSERT(PR_DBG_PHYSICS, overlap.m_pA->m_type == Overlap::Point);
	PR_ASSERT(PR_DBG_PHYSICS, overlap.m_pB->m_type == Overlap::Face);
	
	v4 a2b = overlap.m_pB->m_point - overlap.m_pA->m_point;
	overlap.m_pB->m_point -= a2b - Dot3(overlap.m_axis, a2b) * overlap.m_axis;
}

//*****
// Edge vs. Edge: Move both points to the closest point between the two edges
void Collider::EdgeToEdge(const Collider::Info& objectA, const Collider::Info& objectB, Collider::Overlap& overlap)
{
	PR_ASSERT(PR_DBG_PHYSICS, overlap.m_pA->m_type == Overlap::Edge);
	PR_ASSERT(PR_DBG_PHYSICS, overlap.m_pB->m_type == Overlap::Edge);

	// Find the edge axes
	uint edge_axisA; for( edge_axisA = 0; edge_axisA < objectA.m_num_radii && !overlap.m_pA->m_DOF[edge_axisA]; ++edge_axisA ) {} PR_ASSERT(PR_DBG_PHYSICS, edge_axisA != objectA.m_num_radii);
	uint edge_axisB; for( edge_axisB = 0; edge_axisB < objectB.m_num_radii && !overlap.m_pB->m_DOF[edge_axisB]; ++edge_axisB ) {} PR_ASSERT(PR_DBG_PHYSICS, edge_axisB != objectB.m_num_radii);

	v4 Penetration	= overlap.m_penetration * overlap.m_axis;
	v4 BonA			= overlap.m_pB->m_point + Penetration;

	v4 BS	 = BonA	- objectB.m_radius[edge_axisB];
	v4 BE	 = BonA	+ objectB.m_radius[edge_axisB];

	v4 edge_norm	= Cross3(overlap.m_axis, objectA.m_normal[edge_axisA]);
	float d1 = Dot3(edge_norm, BS - overlap.m_pA->m_point);
	float d2 = Dot3(edge_norm, BE - overlap.m_pA->m_point);
	float t = Clamp(d1 / (d1 - d2), 0.0f, 1.0f);
	
	overlap.m_pA->m_point = BS + t * 2.0f * objectB.m_radius[edge_axisB];
	overlap.m_pB->m_point = overlap.m_pA->m_point - Penetration;
}


void Collider::EdgeToFace(const Collider::Info& objectA, const Collider::Info& objectB, Collider::Overlap& overlap)
{
	objectA;
	objectB;
	overlap;
}

void Collider::FaceToFace(const Collider::Info& objectA, const Collider::Info& objectB, Collider::Overlap& overlap)
{
	objectA;
	objectB;
	overlap;
}

//PSR...//*****
//PSR...// Edge vs. Face: Clip A's edge to B's face 
//PSR...// Move the points to the midpoint of the clipped line segment
//PSR...void Collider::EdgeToFace(const Collider::Info& objectA, const Collider::Info& objectB, Collider::Overlap& overlap)
//PSR...{
//PSR...	PR_ASSERT(PR_DBG_PHYSICS, overlap.m_pA->m_type == Overlap::Edge);
//PSR...	PR_ASSERT(PR_DBG_PHYSICS, overlap.m_pB->m_type == Overlap::Face);
//PSR...
//PSR...	// Find the edge and face axes
//PSR...	int edge_axis;  for( edge_axis  = 0;				edge_axis  < 3 && !overlap.m_pA->m_DOF[edge_axis];  ++edge_axis  ) {} PR_ASSERT(PR_DBG_PHYSICS, edge_axis  != 3);
//PSR...	int face_axis1; for( face_axis1 = 0;				face_axis1 < 3 && !overlap.m_pB->m_DOF[face_axis1]; ++face_axis1 ) {} PR_ASSERT(PR_DBG_PHYSICS, face_axis1 != 3);
//PSR...	int face_axis2; for( face_axis2 = face_axis1 + 1;	face_axis2 < 3 && !overlap.m_pB->m_DOF[face_axis2]; ++face_axis2 ) {} PR_ASSERT(PR_DBG_PHYSICS, face_axis2 != 3);
//PSR...
//PSR...	v4 Penetration	= overlap.m_penetration * overlap.m_axis;
//PSR...	v4 BonA			= overlap.m_pB->m_point + Penetration;
//PSR...
//PSR...	v4 AS = overlap.m_pA->m_point - objectA.m_radius[edge_axis];
//PSR...	v4 AE = overlap.m_pA->m_point + objectA.m_radius[edge_axis];
//PSR...
//PSR...	// Clip AS->AE against the edges of the face
//PSR...	Clip(AS, AE, BonA - objectB.m_radius[face_axis1],  objectB.m_normal[face_axis1]);
//PSR...	Clip(AS, AE, BonA - objectB.m_radius[face_axis2],  objectB.m_normal[face_axis2]);
//PSR...	Clip(AS, AE, BonA + objectB.m_radius[face_axis1], -objectB.m_normal[face_axis1]);
//PSR...	Clip(AS, AE, BonA + objectB.m_radius[face_axis2], -objectB.m_normal[face_axis2]);
//PSR...
//PSR...	overlap.m_pA->m_point = (AS + AE) * 0.5f;
//PSR...	overlap.m_pB->m_point = overlap.m_pA->m_point - Penetration;
//PSR...}
//PSR...
//PSR...//*****
//PSR...// Face vs. Face: Clip the edges of A to the edges of B
//PSR...// Move the points to the centre of the clipped lines
//PSR...void Collider::FaceToFace(const Collider::Info& objectA, const Collider::Info& objectB, Collider::Overlap& overlap)
//PSR...{
//PSR...	PR_ASSERT(PR_DBG_PHYSICS, overlap.m_pA->m_type == Overlap::Face);
//PSR...	PR_ASSERT(PR_DBG_PHYSICS, overlap.m_pB->m_type == Overlap::Face);
//PSR...
//PSR...	// Find the edge and face axes
//PSR...	int face_axisA1; for( face_axisA1 = 0;					face_axisA1 < 3 && !overlap.m_pA->m_DOF[face_axisA1]; ++face_axisA1 ) {} PR_ASSERT(PR_DBG_PHYSICS, face_axisA1 != 3);
//PSR...	int face_axisA2; for( face_axisA2 = face_axisA1 + 1;	face_axisA2 < 3 && !overlap.m_pA->m_DOF[face_axisA2]; ++face_axisA2 ) {} PR_ASSERT(PR_DBG_PHYSICS, face_axisA2 != 3);
//PSR...	int face_axisB1; for( face_axisB1 = 0;					face_axisB1 < 3 && !overlap.m_pB->m_DOF[face_axisB1]; ++face_axisB1 ) {} PR_ASSERT(PR_DBG_PHYSICS, face_axisB1 != 3);
//PSR...	int face_axisB2; for( face_axisB2 = face_axisB1 + 1;	face_axisB2 < 3 && !overlap.m_pB->m_DOF[face_axisB2]; ++face_axisB2 ) {} PR_ASSERT(PR_DBG_PHYSICS, face_axisB2 != 3);
//PSR...
//PSR...	v4 Penetration	= overlap.m_penetration * overlap.m_axis;
//PSR...	v4 BonA			= overlap.m_pB->m_point + Penetration;
//PSR...
//PSR...	v4 TR	= overlap.m_pA->m_point + objectA.m_radius[face_axisA1] + objectA.m_radius[face_axisA2];
//PSR...	v4 BR	= overlap.m_pA->m_point + objectA.m_radius[face_axisA2] - objectA.m_radius[face_axisA1];
//PSR...	v4 BL	= overlap.m_pA->m_point - objectA.m_radius[face_axisA1] - objectA.m_radius[face_axisA2];
//PSR...	v4 TL	= overlap.m_pA->m_point - objectA.m_radius[face_axisA2] + objectA.m_radius[face_axisA1];
//PSR...
//PSR...	v4 AS[4], AE[4];
//PSR...	AS[0] = TL;		AE[0] = TR;
//PSR...	AS[1] = TR;		AE[1] = BR;
//PSR...	AS[2] = BR;		AE[2] = BL;
//PSR...	AS[3] = BL;		AE[3] = TL;
//PSR...
//PSR...	// Clip AS->AE against the edges of the face
//PSR...	overlap.m_pA->m_point.Zero();
//PSR...	for( int i = 0; i < 4; ++i )
//PSR...	{
//PSR...		Clip(AS[i], AE[i], BonA - objectB.m_radius[face_axisB1],  objectB.m_normal[face_axisB1]);
//PSR...		Clip(AS[i], AE[i], BonA - objectB.m_radius[face_axisB2],  objectB.m_normal[face_axisB2]);
//PSR...		Clip(AS[i], AE[i], BonA + objectB.m_radius[face_axisB1], -objectB.m_normal[face_axisB1]);
//PSR...		Clip(AS[i], AE[i], BonA + objectB.m_radius[face_axisB2], -objectB.m_normal[face_axisB2]);
//PSR...		overlap.m_pA->m_point += AS[i] + AE[i];
//PSR...	}
//PSR...
//PSR...	overlap.m_pA->m_point *= 0.125f;
//PSR...	overlap.m_pB->m_point = overlap.m_pA->m_point - Penetration;
//PSR...}
//PSR...
//PSR...//*****
//PSR...// Clip a line between 'start' and 'end' to a plane that passes through 'pt' with 'normal'
//PSR...void Collider::Clip(v4& start, v4& end, const v4& pt, const v4& normal)
//PSR...{
//PSR...	float d1 = normal.Dot3(start - pt);
//PSR...	float d2 = normal.Dot3(end   - pt);
//PSR...	float t = Clamp(d1 / (d1 - d2), 0.0f, 1.0f);
//PSR...	if( d1 < 0.0f )	start = start + t          * (end - start);
//PSR...	else			end   = end   + (1.0f - t) * (start - end);
//PSR...}
