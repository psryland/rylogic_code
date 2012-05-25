//*******************************************************************
//
//	A base class for an Event
//
//*******************************************************************
#include "Stdafx.h"
#include "PhysicsLab_LDPI/Event.h"
#include "PhysicsLab_LDPI/PhysicsObject.h"

//*****
// Construction
Event::Event()
{}
Event::~Event()
{}

//*****
// Apply this event to the target
void Event::Apply()
{
	switch( m_type )
	{
	case eImpulse:
		m_target->Physics().ApplyWorldImpulseAt(m_direction * m_magnitude, m_position);
		break;
	case eMoment:
		m_target->Physics().ApplyWorldMoment(m_direction * m_magnitude);
		break;
	default: PR_ERROR_STR(PR_DBG_PHLAB, "Unknown type");
	}	
}
