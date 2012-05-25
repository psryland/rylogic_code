//*******************************************************************
//
//	A base class for an Event
//
//*******************************************************************
#ifndef EVENT_H
#define EVENT_H

#include "PR/Common/StdString.h"
#include "PR/Common/StdVector.h"
#include "PR/Maths/Maths.h"
#include "PhysicsLab_LDPI/Forward.h"

class Event
{
public:
	enum Type { eImpulse, eMoment };
	
	Event();
	virtual ~Event();
	
	float	Time() const							{ return m_time; }
	bool operator < (const Event& other) const		{ return m_time < other.m_time; }
	void	Apply();

private:
	friend class SceneParser;
	Type			m_type;
	PhysicsObject*	m_target;
	pr::v4			m_position;
	pr::v4			m_direction;
	float			m_magnitude;
	float			m_time;

	// Generation members
	std::string		m_target_name;
};

typedef std::vector<Event> TEventContainer;

#endif//EVENT_H