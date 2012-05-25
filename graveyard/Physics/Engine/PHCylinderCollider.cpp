//****************************************************
//
//	Cylinder vs. cylinder Collision detection methods
//
//****************************************************

#include "PR/Physics/Physics.h"
#include "PR/Physics/Engine/PHCollision.h"
#include "PR/Physics/Engine/PHCylinderCollider.h"

using namespace pr;
using namespace pr::ph;

//*****
// Calculates the most like point of contact between two orientated cylinders
CylinderCollider::CylinderCollider(const CylinderCollider::Cylinder& cylinderA, const CylinderCollider::Cylinder& cylinderB, Contact& contact)
{
	// Assume no collision to start with
	contact.SetNoCollision();

	// Check for coincident cylinders
	if( FEql((cylinderB.m_centre - cylinderA.m_centre).Length3Sq(), 0.0f) )
	{
		PR_ERROR_STR(PR_DBG_PHYSICS, "Two cylinders exactly on top of each other");
		return;
	}

	// Find the extent on each of the separating axes
	Overlap min_overlap;
	for( int i = 0; i < 2; ++i )
	{
		GetMinOverlap(cylinderA.m_normal[i], cylinderA, cylinderB, min_overlap);
		if( min_overlap.m_penetration < 0.0f ) return; // No collision
	}
	
	for( int i = 0; i < 2; ++i )
	{
		GetMinOverlap(cylinderB.m_normal[i], cylinderA, cylinderB, min_overlap);
		if( min_overlap.m_penetration < 0.0f ) return; // No collision
	}

	v4 axis = Cross3(cylinderA.m_normal[0], cylinderB.m_normal[0]);
	if( !axis.IsZero3() )
	{
		axis.Normalise3();
		GetMinOverlap(axis, cylinderA, cylinderB, min_overlap);
		if( min_overlap.m_penetration < 0.0f ) return; // No collision
	}

	// Find the point of contact based on the overlap type
	GetPointOfContact(cylinderA, cylinderB, min_overlap);
	
	// Save the contact point (converted into the local space of cylinderA and cylinderB)
	min_overlap.m_A.m_point	-= cylinderA.m_centre;
	min_overlap.m_B.m_point	-= cylinderB.m_centre;
//PSR...	contact.m_pointA = min_overlap.m_A.m_point;
//PSR...	contact.m_pointB = min_overlap.m_B.m_point;
//PSR...	contact.m_pointA[2] = cylinderA.m_normal[0].Dot3(min_overlap.m_A.m_point);


//PSR...	contact.m_pointA		.Set(	cylinderA.m_normal[0].Dot3(min_overlap.m_A.m_point),
//PSR...									cylinderA.m_normal[1].Dot3(min_overlap.m_A.m_point),
//PSR...									cylinderA.m_normal[2].Dot3(min_overlap.m_A.m_point),
//PSR...									1.0f);
//PSR...	contact.m_pointB		.Set(	cylinderB.m_normal[0].Dot3(min_overlap.m_B.m_point),
//PSR...									cylinderB.m_normal[1].Dot3(min_overlap.m_B.m_point),
//PSR...									cylinderB.m_normal[2].Dot3(min_overlap.m_B.m_point),
//PSR...									1.0f);
	contact.m_normal	= min_overlap.m_axis;
	contact.m_depth		= min_overlap.m_penetration;
}

//*****
// This function finds the overlap of 'cylinderA' and 'cylinderB' when projected on 'axis'.
// If the overlap is less than 'min_overlap.m_penetration' then 'min_overlap' is updated
void CylinderCollider::GetMinOverlap(const v4& axis, const CylinderCollider::Cylinder& cylinderA, const CylinderCollider::Cylinder& cylinderB, CylinderCollider::Overlap& min_overlap)
{
	if( axis.IsZero3() ) return;

	Overlap overlap;
	overlap.m_axis			= axis;
	overlap.m_penetration	= -Dot3(axis, cylinderB.m_centre - cylinderA.m_centre);
	overlap.m_A.m_point		= cylinderA.m_centre;
	overlap.m_B.m_point		= cylinderB.m_centre;
	overlap.m_A.m_type		= Overlap::Edge;
	overlap.m_B.m_type		= Overlap::Edge;
	if( overlap.m_penetration > 0.0f )
	{
		overlap.m_axis			= -overlap.m_axis;
		overlap.m_penetration	= -overlap.m_penetration;
	}

	// Find the nearest points
	for( int j = 0; j < 2; ++j )
	{
		// CylinderA
		float distA = Dot3(overlap.m_axis, cylinderA.m_radius[j]);
		if( FEql(distA, 0.0f) )
		{
			overlap.m_A.m_DOF[j]	= true;
			++overlap.m_A.m_type;
		}
		else if( distA > 0.0f )
		{
			overlap.m_A.m_DOF[j]	= false;
			overlap.m_A.m_point		+= cylinderA.m_radius[j];
			overlap.m_penetration	+= distA;
		}
		else // distA < 0.0f
		{
			overlap.m_A.m_DOF[j]	= false;
			overlap.m_A.m_point		-= cylinderA.m_radius[j];
			overlap.m_penetration	-= distA;
		}
		
		// CylinderB
		float distB = Dot3(overlap.m_axis, cylinderB.m_radius[j]);
		if( FEql(distB, 0.0f) )
		{
			overlap.m_B.m_DOF[j]	= true;
			++overlap.m_B.m_type;
		}
		else if( distB > 0.0f )
		{
			overlap.m_B.m_DOF[j]	= false;
			overlap.m_B.m_point		-= cylinderB.m_radius[j];
			overlap.m_penetration	+= distB;
		}
		else
		{
			overlap.m_B.m_DOF[j]	= false;
			overlap.m_B.m_point		+= cylinderB.m_radius[j];
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
//PSR...	PR_ASSERT(!(overlap.m_A.m_type == Overlap::Corner && overlap.m_B.m_type == Overlap::Corner));
//PSR...	PR_ASSERT(!(overlap.m_A.m_type == Overlap::Corner && overlap.m_B.m_type == Overlap::Edge));
//PSR...	PR_ASSERT(!(overlap.m_A.m_type == Overlap::Edge && overlap.m_B.m_type == Overlap::Corner));
}

//*****
// This function takes the points A and B in 'min_overlap' and
// adjusts them to the mostly likely point of contact based on the
// constraints described in radii.
void CylinderCollider::GetPointOfContact(const CylinderCollider::Cylinder& cylinderA, const CylinderCollider::Cylinder& cylinderB, CylinderCollider::Overlap& min_overlap)
{
	cylinderA;
	cylinderB;
	min_overlap;
//PSR...	switch( min_overlap.m_A.m_type )
//PSR...	{
//PSR...	case Overlap::Corner: CornerToFace(cylinderA, cylinderB, min_overlap); break;
//PSR...	case Overlap::Edge:
//PSR...		{
//PSR...			switch( min_overlap.m_B.m_type )
//PSR...			{
//PSR...			case Overlap::Edge: EdgeToEdge(cylinderA, cylinderB, min_overlap); break;
//PSR...			case Overlap::Face:	EdgeToFace(cylinderA, cylinderB, min_overlap); break;
//PSR...			default: PR_ERROR_STR("Should not get this type of contact");
//PSR...			};
//PSR...		}break;
//PSR...	case Overlap::Face:
//PSR...		{
//PSR...			switch( min_overlap.m_B.m_type )
//PSR...			{
//PSR...			case Overlap::Corner:
//PSR...				min_overlap.Reverse();
//PSR...				CornerToFace(cylinderB, cylinderA, min_overlap);
//PSR...				min_overlap.Reverse();
//PSR...				break;
//PSR...			case Overlap::Edge:
//PSR...				min_overlap.Reverse();
//PSR...				EdgeToFace(cylinderB, cylinderA, min_overlap);
//PSR...				min_overlap.Reverse();
//PSR...				break;
//PSR...			case Overlap::Face:
//PSR...				FaceToFace(cylinderA, cylinderB, min_overlap);
//PSR...				break;
//PSR...			default: PR_ERROR_STR("Unknown overlap type");
//PSR...			};
//PSR...		}break;
//PSR...	default:	PR_ERROR_STR("Unknown overlap type");
//PSR...	};
}

//*****
// Corner vs. Face: Move point B onto the axis that passes through point A
void CylinderCollider::CornerToFace(const CylinderCollider::Cylinder& cylinderA, const CylinderCollider::Cylinder& cylinderB, CylinderCollider::Overlap& overlap)
{
	cylinderA;
	cylinderB;
	overlap;
//PSR...	PR_ASSERT(overlap.m_pA->m_type == Overlap::Corner);
//PSR...	PR_ASSERT(overlap.m_pB->m_type == Overlap::Face);
//PSR...	
//PSR...	v4 a2b = overlap.m_pB->m_point - overlap.m_pA->m_point;
//PSR...	overlap.m_pB->m_point -= a2b - overlap.m_axis.Dot3(a2b) * overlap.m_axis;
}

//*****
// Edge vs. Edge: Move both points to the closest point between the two edges
void CylinderCollider::EdgeToEdge(const CylinderCollider::Cylinder& cylinderA, const CylinderCollider::Cylinder& cylinderB, CylinderCollider::Overlap& overlap)
{
	cylinderA;
	cylinderB;
	overlap;

//PSR...	PR_ASSERT(overlap.m_pA->m_type == Overlap::Edge);
//PSR...	PR_ASSERT(overlap.m_pB->m_type == Overlap::Edge);
//PSR...
//PSR...	// Find the edge axes
//PSR...	int edge_axisA; for( edge_axisA = 0; edge_axisA < 3 && !overlap.m_pA->m_DOF[edge_axisA]; ++edge_axisA ) {} PR_ASSERT(edge_axisA != 3);
//PSR...	int edge_axisB; for( edge_axisB = 0; edge_axisB < 3 && !overlap.m_pB->m_DOF[edge_axisB]; ++edge_axisB ) {} PR_ASSERT(edge_axisB != 3);
//PSR...
//PSR...	v4 Penetration	= overlap.m_penetration * overlap.m_axis;
//PSR...	v4 BonA			= overlap.m_pB->m_point + Penetration;
//PSR...
//PSR...	v4 BS	 = BonA	- cylinderB.m_radius[edge_axisB];
//PSR...	v4 BE	 = BonA	+ cylinderB.m_radius[edge_axisB];
//PSR...
//PSR...	v4 edge_norm	= overlap.m_axis.Cross(cylinderA.m_normal[edge_axisA]);
//PSR...	float d1 = edge_norm.Dot3(BS - overlap.m_pA->m_point);
//PSR...	float d2 = edge_norm.Dot3(BE - overlap.m_pA->m_point);
//PSR...	float t = Clamp(d1 / (d1 - d2), 0.0f, 1.0f);
//PSR...	
//PSR...	overlap.m_pA->m_point = BS + t * 2.0f * cylinderB.m_radius[edge_axisB];
//PSR...	overlap.m_pB->m_point = overlap.m_pA->m_point - Penetration;
}

//*****
// Edge vs. Face: Clip A's edge to B's face 
// Move the points to the midpoint of the clipped line segment
void CylinderCollider::EdgeToFace(const CylinderCollider::Cylinder& cylinderA, const CylinderCollider::Cylinder& cylinderB, CylinderCollider::Overlap& overlap)
{
	cylinderA;
	cylinderB;
	overlap;
//PSR...	PR_ASSERT(overlap.m_pA->m_type == Overlap::Edge);
//PSR...	PR_ASSERT(overlap.m_pB->m_type == Overlap::Face);
//PSR...
//PSR...	// Find the edge and face axes
//PSR...	int edge_axis;  for( edge_axis  = 0;				edge_axis  < 3 && !overlap.m_pA->m_DOF[edge_axis];  ++edge_axis  ) {} PR_ASSERT(edge_axis  != 3);
//PSR...	int face_axis1; for( face_axis1 = 0;				face_axis1 < 3 && !overlap.m_pB->m_DOF[face_axis1]; ++face_axis1 ) {} PR_ASSERT(face_axis1 != 3);
//PSR...	int face_axis2; for( face_axis2 = face_axis1 + 1;	face_axis2 < 3 && !overlap.m_pB->m_DOF[face_axis2]; ++face_axis2 ) {} PR_ASSERT(face_axis2 != 3);
//PSR...
//PSR...	v4 Penetration	= overlap.m_penetration * overlap.m_axis;
//PSR...	v4 BonA			= overlap.m_pB->m_point + Penetration;
//PSR...
//PSR...	v4 AS = overlap.m_pA->m_point - cylinderA.m_radius[edge_axis];
//PSR...	v4 AE = overlap.m_pA->m_point + cylinderA.m_radius[edge_axis];
//PSR...
//PSR...	// Clip AS->AE against the edges of the face
//PSR...	Clip(AS, AE, BonA - cylinderB.m_radius[face_axis1],  cylinderB.m_normal[face_axis1]);
//PSR...	Clip(AS, AE, BonA - cylinderB.m_radius[face_axis2],  cylinderB.m_normal[face_axis2]);
//PSR...	Clip(AS, AE, BonA + cylinderB.m_radius[face_axis1], -cylinderB.m_normal[face_axis1]);
//PSR...	Clip(AS, AE, BonA + cylinderB.m_radius[face_axis2], -cylinderB.m_normal[face_axis2]);
//PSR...
//PSR...	overlap.m_pA->m_point = (AS + AE) * 0.5f;
//PSR...	overlap.m_pB->m_point = overlap.m_pA->m_point - Penetration;
}

//*****
// Face vs. Face: Clip the edges of A to the edges of B
// Move the points to the centre of the clipped lines
void CylinderCollider::FaceToFace(const CylinderCollider::Cylinder& cylinderA, const CylinderCollider::Cylinder& cylinderB, CylinderCollider::Overlap& overlap)
{
	cylinderA;
	cylinderB;
	overlap;
//PSR...	PR_ASSERT(overlap.m_pA->m_type == Overlap::Face);
//PSR...	PR_ASSERT(overlap.m_pB->m_type == Overlap::Face);
//PSR...
//PSR...	// Find the edge and face axes
//PSR...	int face_axisA1; for( face_axisA1 = 0;					face_axisA1 < 3 && !overlap.m_pA->m_DOF[face_axisA1]; ++face_axisA1 ) {} PR_ASSERT(face_axisA1 != 3);
//PSR...	int face_axisA2; for( face_axisA2 = face_axisA1 + 1;	face_axisA2 < 3 && !overlap.m_pA->m_DOF[face_axisA2]; ++face_axisA2 ) {} PR_ASSERT(face_axisA2 != 3);
//PSR...	int face_axisB1; for( face_axisB1 = 0;					face_axisB1 < 3 && !overlap.m_pB->m_DOF[face_axisB1]; ++face_axisB1 ) {} PR_ASSERT(face_axisB1 != 3);
//PSR...	int face_axisB2; for( face_axisB2 = face_axisB1 + 1;	face_axisB2 < 3 && !overlap.m_pB->m_DOF[face_axisB2]; ++face_axisB2 ) {} PR_ASSERT(face_axisB2 != 3);
//PSR...
//PSR...	v4 Penetration	= overlap.m_penetration * overlap.m_axis;
//PSR...	v4 BonA			= overlap.m_pB->m_point + Penetration;
//PSR...
//PSR...	v4 TR	= overlap.m_pA->m_point + cylinderA.m_radius[face_axisA1] + cylinderA.m_radius[face_axisA2];
//PSR...	v4 BR	= overlap.m_pA->m_point + cylinderA.m_radius[face_axisA2] - cylinderA.m_radius[face_axisA1];
//PSR...	v4 BL	= overlap.m_pA->m_point - cylinderA.m_radius[face_axisA1] - cylinderA.m_radius[face_axisA2];
//PSR...	v4 TL	= overlap.m_pA->m_point - cylinderA.m_radius[face_axisA2] + cylinderA.m_radius[face_axisA1];
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
//PSR...		Clip(AS[i], AE[i], BonA - cylinderB.m_radius[face_axisB1],  cylinderB.m_normal[face_axisB1]);
//PSR...		Clip(AS[i], AE[i], BonA - cylinderB.m_radius[face_axisB2],  cylinderB.m_normal[face_axisB2]);
//PSR...		Clip(AS[i], AE[i], BonA + cylinderB.m_radius[face_axisB1], -cylinderB.m_normal[face_axisB1]);
//PSR...		Clip(AS[i], AE[i], BonA + cylinderB.m_radius[face_axisB2], -cylinderB.m_normal[face_axisB2]);
//PSR...		overlap.m_pA->m_point += AS[i] + AE[i];
//PSR...	}
//PSR...
//PSR...	overlap.m_pA->m_point *= 0.125f;
//PSR...	overlap.m_pB->m_point = overlap.m_pA->m_point - Penetration;
}

//*****
// Clip a line between 'start' and 'end' to a plane that passes through 'pt' with 'normal'
void CylinderCollider::Clip(v4& start, v4& end, const v4& pt, const v4& normal)
{
	start;
	end;
	pt;
	normal;
//PSR...	float d1 = normal.Dot3(start - pt);
//PSR...	float d2 = normal.Dot3(end   - pt);
//PSR...	float t = Clamp(d1 / (d1 - d2), 0.0f, 1.0f);
//PSR...	if( d1 < 0.0f )	start = start + t          * (end - start);
//PSR...	else			end   = end   + (1.0f - t) * (start - end);
}
