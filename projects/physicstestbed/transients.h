//************************************
// Transient objects
//************************************
#pragma once

#include "PhysicsTestbed/Forwards.h"
#include "PhysicsTestbed/res/Resource.h"
#include "pr/linedrawer/plugininterface.h"

// Impulse graphics
struct CWImpulse
{
	CWImpulse(const pr::v4& position, const pr::v4& impulse, float impulse_scale, unsigned int frame_number);
	~CWImpulse();
	bool Step(unsigned int frame_number);	// Returns false if should be deleted
	void Recreate(float impulse_scale);
	std::string MakeLdrString(float impulse_scale);

	unsigned int			m_frame;	// The frame the impulse was recorded on
	pr::v4					m_position;	// The start position for the impulse
    pr::v4					m_impulse;	// The impulse
	pr::ldr::ObjectHandle	m_ldr;		// The representation of the impulse in line drawer
};
typedef std::list<CWImpulse*> TImpulse;


// Contact graphics
struct CWContact
{
	CWContact(const pr::v4& position, const pr::v4& normal, float scale, unsigned int frame_number);
	~CWContact();
	bool Step(unsigned int frame_number);	// Returns false if should be deleted
	void Recreate(float scale);
	std::string MakeLdrString(float scale);

	unsigned int			m_frame;	// The frame the contact was recorded on
	pr::v4					m_position;	// The position of the contact
	pr::v4					m_normal;	// The surface normal of the contact
	pr::ldr::ObjectHandle	m_ldr;		// The representation of the contact in line drawer
};
typedef std::list<CWContact*> TContact;


// Ray cast graphics
struct RayCast
{
	RayCast(const pr::v4& start, const pr::v4& end, unsigned int frame_number);
	~RayCast();
	bool Step(unsigned int frame_number);	// Returns false if should be deleted
	std::string MakeLdrString();

	unsigned int			m_frame;	// The frame this was recorded on
	pr::v4					m_start;	// The start of the ray
	pr::v4					m_end;		// The end of the ray
	pr::ldr::ObjectHandle	m_ldr;		// The representation of the ray in line drawer
};
typedef std::list<RayCast*> TRayCast;
