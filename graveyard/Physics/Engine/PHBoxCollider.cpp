//****************************************************
//
//	Box vs. box Collision detection methods
//
//****************************************************

#include "PR/Physics/Physics.h"
#include "PR/Physics/Engine/PHCollision.h"
#include "PR/Physics/Engine/PHBoxCollider.h"

using namespace pr;
using namespace pr::ph;

//*****
// Calculates the most like point of contact between two orientated cuboids
BoxCollider::BoxCollider(const BoxCollider::Box& boxA, const BoxCollider::Box& boxB, Contact& contact)
{
	// Assume no collision to start with
	contact.SetNoCollision();

	// Check for coincident boxes
	if( FEql((boxB.m_centre - boxA.m_centre).Length3Sq(), 0.0f) )
	{
		PR_ERROR_STR(PR_DBG_PHYSICS, "Two boxes exactly on top of each other");
		return;
	}

	// Find the extent on each of the separating axes
	Overlap min_overlap;
	for( int i = 0; i < 3; ++i )
	{
		GetMinOverlap(boxA.m_normal[i], boxA, boxB, min_overlap);
		if( min_overlap.m_penetration < 0.0f ) return; // No collision
	}
	for( int i = 0; i < 3; ++i )
	{
		GetMinOverlap(boxB.m_normal[i], boxA, boxB, min_overlap);
		if( min_overlap.m_penetration < 0.0f ) return; // No collision
	}
	for( int i = 0; i < 3; ++i )
	{
		for( int j = 0; j < 3; ++j )
		{
			v4 axis = Cross3(boxA.m_normal[i], boxB.m_normal[j]);
			if( !axis.IsZero3() )
			{
				axis.Normalise3();
				GetMinOverlap(axis, boxA, boxB, min_overlap);
				if( min_overlap.m_penetration < 0.0f ) return; // No collision
			}
		}
	}

	// Find the point of contact based on the overlap type: F2F, E2F, E2E, C2F
	GetPointOfContact(boxA, boxB, min_overlap);
	
	// Save the contact point (converted into the local space of boxA and boxB)
	min_overlap.m_A.m_point	-= boxA.m_centre;
	min_overlap.m_B.m_point	-= boxB.m_centre;
	contact.m_pointA		.Set(	Dot3(boxA.m_normal[0], min_overlap.m_A.m_point),
									Dot3(boxA.m_normal[1], min_overlap.m_A.m_point),
									Dot3(boxA.m_normal[2], min_overlap.m_A.m_point),
									1.0f);
	contact.m_pointB		.Set(	Dot3(boxB.m_normal[0], min_overlap.m_B.m_point),
									Dot3(boxB.m_normal[1], min_overlap.m_B.m_point),
									Dot3(boxB.m_normal[2], min_overlap.m_B.m_point),
									1.0f);
	contact.m_normal		= min_overlap.m_axis;
	contact.m_depth			= min_overlap.m_penetration;
}

//*****
// This function finds the overlap of 'boxA' and 'boxB' when projected on 'axis'.
// If the overlap is less than 'min_overlap.m_penetration' then 'min_overlap' is updated
void BoxCollider::GetMinOverlap(const v4& axis, const BoxCollider::Box& boxA, const BoxCollider::Box& boxB, BoxCollider::Overlap& min_overlap)
{
	Overlap overlap;
	overlap.m_axis			= axis;
	overlap.m_penetration	= -Dot3(axis, boxB.m_centre - boxA.m_centre);
	overlap.m_A.m_point		= boxA.m_centre;
	overlap.m_B.m_point		= boxB.m_centre;
	overlap.m_A.m_type		= Overlap::Corner;
	overlap.m_B.m_type		= Overlap::Corner;
	if( overlap.m_penetration > 0.0f )
	{
		overlap.m_axis			= -overlap.m_axis;
		overlap.m_penetration	= -overlap.m_penetration;
	}

	// Find the nearest points
	for( int j = 0; j < 3; ++j )
	{
		// BoxA
		float distA = Dot3(overlap.m_axis, boxA.m_radius[j]);
		if( FEql(distA, 0.0f) )
		{
			overlap.m_A.m_DOF[j]	= true;
			++overlap.m_A.m_type;
		}
		else if( distA > 0.0f )
		{
			overlap.m_A.m_DOF[j]	= false;
			overlap.m_A.m_point		+= boxA.m_radius[j];
			overlap.m_penetration	+= distA;
		}
		else // distA < 0.0f
		{
			overlap.m_A.m_DOF[j]	= false;
			overlap.m_A.m_point		-= boxA.m_radius[j];
			overlap.m_penetration	-= distA;
		}
		
		// BoxB
		float distB = Dot3(overlap.m_axis, boxB.m_radius[j]);
		if( FEql(distB, 0.0f) )
		{
			overlap.m_B.m_DOF[j]	= true;
			++overlap.m_B.m_type;
		}
		else if( distB > 0.0f )
		{
			overlap.m_B.m_DOF[j]	= false;
			overlap.m_B.m_point		-= boxB.m_radius[j];
			overlap.m_penetration	+= distB;
		}
		else
		{
			overlap.m_B.m_DOF[j]	= false;
			overlap.m_B.m_point		+= boxB.m_radius[j];
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
	PR_ASSERT(PR_DBG_PHYSICS, !(overlap.m_A.m_type == Overlap::Corner && overlap.m_B.m_type == Overlap::Corner));
	PR_ASSERT(PR_DBG_PHYSICS, !(overlap.m_A.m_type == Overlap::Corner && overlap.m_B.m_type == Overlap::Edge));
	PR_ASSERT(PR_DBG_PHYSICS, !(overlap.m_A.m_type == Overlap::Edge && overlap.m_B.m_type == Overlap::Corner));
}

//*****
// This function takes the points A and B in 'min_overlap' and
// adjusts them to the mostly likely point of contact based on the
// constraints described in radii.
void BoxCollider::GetPointOfContact(const BoxCollider::Box& boxA, const BoxCollider::Box& boxB, BoxCollider::Overlap& min_overlap)
{
	switch( min_overlap.m_A.m_type )
	{
	case Overlap::Corner: CornerToFace(boxA, boxB, min_overlap); break;
	case Overlap::Edge:
		{
			switch( min_overlap.m_B.m_type )
			{
			case Overlap::Edge: EdgeToEdge(boxA, boxB, min_overlap); break;
			case Overlap::Face:	EdgeToFace(boxA, boxB, min_overlap); break;
			default: PR_ERROR_STR(PR_DBG_PHYSICS, "Should not get this type of contact");
			};
		}break;
	case Overlap::Face:
		{
			switch( min_overlap.m_B.m_type )
			{
			case Overlap::Corner:
				min_overlap.Reverse();
				CornerToFace(boxB, boxA, min_overlap);
				min_overlap.Reverse();
				break;
			case Overlap::Edge:
				min_overlap.Reverse();
				EdgeToFace(boxB, boxA, min_overlap);
				min_overlap.Reverse();
				break;
			case Overlap::Face:
				FaceToFace(boxA, boxB, min_overlap);
				break;
			default: PR_ERROR_STR(PR_DBG_PHYSICS, "Unknown overlap type");
			};
		}break;
	default:	PR_ERROR_STR(PR_DBG_PHYSICS, "Unknown overlap type");
	};
}

//*****
// Corner vs. Face: Move point B onto the axis that passes through point A
void BoxCollider::CornerToFace(const BoxCollider::Box& boxA, const BoxCollider::Box& boxB, BoxCollider::Overlap& overlap)
{
	boxA;
	boxB;
	PR_ASSERT(PR_DBG_PHYSICS, overlap.m_pA->m_type == Overlap::Corner);
	PR_ASSERT(PR_DBG_PHYSICS, overlap.m_pB->m_type == Overlap::Face);
	
	v4 a2b = overlap.m_pB->m_point - overlap.m_pA->m_point;
	overlap.m_pB->m_point -= a2b - Dot3(overlap.m_axis, a2b) * overlap.m_axis;
}

//*****
// Edge vs. Edge: Move both points to the closest point between the two edges
void BoxCollider::EdgeToEdge(const BoxCollider::Box& boxA, const BoxCollider::Box& boxB, BoxCollider::Overlap& overlap)
{
	PR_ASSERT(PR_DBG_PHYSICS, overlap.m_pA->m_type == Overlap::Edge);
	PR_ASSERT(PR_DBG_PHYSICS, overlap.m_pB->m_type == Overlap::Edge);

	// Find the edge axes
	int edge_axisA; for( edge_axisA = 0; edge_axisA < 3 && !overlap.m_pA->m_DOF[edge_axisA]; ++edge_axisA ) {} PR_ASSERT(PR_DBG_PHYSICS, edge_axisA != 3);
	int edge_axisB; for( edge_axisB = 0; edge_axisB < 3 && !overlap.m_pB->m_DOF[edge_axisB]; ++edge_axisB ) {} PR_ASSERT(PR_DBG_PHYSICS, edge_axisB != 3);

	v4 Penetration	= overlap.m_penetration * overlap.m_axis;
	v4 BonA			= overlap.m_pB->m_point + Penetration;

	v4 BS	 = BonA	- boxB.m_radius[edge_axisB];
	v4 BE	 = BonA	+ boxB.m_radius[edge_axisB];

	v4 edge_norm	= Cross3(overlap.m_axis, boxA.m_normal[edge_axisA]);
	float d1 = Dot3(edge_norm, BS - overlap.m_pA->m_point);
	float d2 = Dot3(edge_norm, BE - overlap.m_pA->m_point);
	float t = Clamp(d1 / (d1 - d2), 0.0f, 1.0f);
	
	overlap.m_pA->m_point = BS + t * 2.0f * boxB.m_radius[edge_axisB];
	overlap.m_pB->m_point = overlap.m_pA->m_point - Penetration;
}

//*****
// Edge vs. Face: Clip A's edge to B's face 
// Move the points to the midpoint of the clipped line segment
void BoxCollider::EdgeToFace(const BoxCollider::Box& boxA, const BoxCollider::Box& boxB, BoxCollider::Overlap& overlap)
{
	PR_ASSERT(PR_DBG_PHYSICS, overlap.m_pA->m_type == Overlap::Edge);
	PR_ASSERT(PR_DBG_PHYSICS, overlap.m_pB->m_type == Overlap::Face);

	// Find the edge and face axes
	int edge_axis;  for( edge_axis  = 0;				edge_axis  < 3 && !overlap.m_pA->m_DOF[edge_axis];  ++edge_axis  ) {} PR_ASSERT(PR_DBG_PHYSICS, edge_axis  != 3);
	int face_axis1; for( face_axis1 = 0;				face_axis1 < 3 && !overlap.m_pB->m_DOF[face_axis1]; ++face_axis1 ) {} PR_ASSERT(PR_DBG_PHYSICS, face_axis1 != 3);
	int face_axis2; for( face_axis2 = face_axis1 + 1;	face_axis2 < 3 && !overlap.m_pB->m_DOF[face_axis2]; ++face_axis2 ) {} PR_ASSERT(PR_DBG_PHYSICS, face_axis2 != 3);

	v4 Penetration	= overlap.m_penetration * overlap.m_axis;
	v4 BonA			= overlap.m_pB->m_point + Penetration;

	v4 AS = overlap.m_pA->m_point - boxA.m_radius[edge_axis];
	v4 AE = overlap.m_pA->m_point + boxA.m_radius[edge_axis];

	// Clip AS->AE against the edges of the face
	Clip(AS, AE, BonA - boxB.m_radius[face_axis1],  boxB.m_normal[face_axis1]);
	Clip(AS, AE, BonA - boxB.m_radius[face_axis2],  boxB.m_normal[face_axis2]);
	Clip(AS, AE, BonA + boxB.m_radius[face_axis1], -boxB.m_normal[face_axis1]);
	Clip(AS, AE, BonA + boxB.m_radius[face_axis2], -boxB.m_normal[face_axis2]);

	overlap.m_pA->m_point = (AS + AE) * 0.5f;
	overlap.m_pB->m_point = overlap.m_pA->m_point - Penetration;
}

//*****
// Face vs. Face: Clip the edges of A to the edges of B
// Move the points to the centre of the clipped lines
void BoxCollider::FaceToFace(const BoxCollider::Box& boxA, const BoxCollider::Box& boxB, BoxCollider::Overlap& overlap)
{
	PR_ASSERT(PR_DBG_PHYSICS, overlap.m_pA->m_type == Overlap::Face);
	PR_ASSERT(PR_DBG_PHYSICS, overlap.m_pB->m_type == Overlap::Face);

	// Find the edge and face axes
	int face_axisA1; for( face_axisA1 = 0;					face_axisA1 < 3 && !overlap.m_pA->m_DOF[face_axisA1]; ++face_axisA1 ) {} PR_ASSERT(PR_DBG_PHYSICS, face_axisA1 != 3);
	int face_axisA2; for( face_axisA2 = face_axisA1 + 1;	face_axisA2 < 3 && !overlap.m_pA->m_DOF[face_axisA2]; ++face_axisA2 ) {} PR_ASSERT(PR_DBG_PHYSICS, face_axisA2 != 3);
	int face_axisB1; for( face_axisB1 = 0;					face_axisB1 < 3 && !overlap.m_pB->m_DOF[face_axisB1]; ++face_axisB1 ) {} PR_ASSERT(PR_DBG_PHYSICS, face_axisB1 != 3);
	int face_axisB2; for( face_axisB2 = face_axisB1 + 1;	face_axisB2 < 3 && !overlap.m_pB->m_DOF[face_axisB2]; ++face_axisB2 ) {} PR_ASSERT(PR_DBG_PHYSICS, face_axisB2 != 3);

	v4 Penetration	= overlap.m_penetration * overlap.m_axis;
	v4 BonA			= overlap.m_pB->m_point + Penetration;

	v4 TR	= overlap.m_pA->m_point + boxA.m_radius[face_axisA1] + boxA.m_radius[face_axisA2];
	v4 BR	= overlap.m_pA->m_point + boxA.m_radius[face_axisA2] - boxA.m_radius[face_axisA1];
	v4 BL	= overlap.m_pA->m_point - boxA.m_radius[face_axisA1] - boxA.m_radius[face_axisA2];
	v4 TL	= overlap.m_pA->m_point - boxA.m_radius[face_axisA2] + boxA.m_radius[face_axisA1];

	v4 AS[4], AE[4];
	AS[0] = TL;		AE[0] = TR;
	AS[1] = TR;		AE[1] = BR;
	AS[2] = BR;		AE[2] = BL;
	AS[3] = BL;		AE[3] = TL;

	// Clip AS->AE against the edges of the face
	overlap.m_pA->m_point.Zero();
	for( int i = 0; i < 4; ++i )
	{
		Clip(AS[i], AE[i], BonA - boxB.m_radius[face_axisB1],  boxB.m_normal[face_axisB1]);
		Clip(AS[i], AE[i], BonA - boxB.m_radius[face_axisB2],  boxB.m_normal[face_axisB2]);
		Clip(AS[i], AE[i], BonA + boxB.m_radius[face_axisB1], -boxB.m_normal[face_axisB1]);
		Clip(AS[i], AE[i], BonA + boxB.m_radius[face_axisB2], -boxB.m_normal[face_axisB2]);
		overlap.m_pA->m_point += AS[i] + AE[i];
	}

	overlap.m_pA->m_point *= 0.125f;
	overlap.m_pB->m_point = overlap.m_pA->m_point - Penetration;
}

//*****
// Clip a line between 'start' and 'end' to a plane that passes through 'pt' with 'normal'
void BoxCollider::Clip(v4& start, v4& end, const v4& pt, const v4& normal)
{
	float d1 = Dot3(normal, start - pt);
	float d2 = Dot3(normal, end   - pt);
	float t = Clamp(d1 / (d1 - d2), 0.0f, 1.0f);
	if( d1 < 0.0f )	start = start + t          * (end - start);
	else			end   = end   + (1.0f - t) * (start - end);
}
