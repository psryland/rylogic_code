//*******************************************************************************************
//
//	This header contains the things that can be drawn
//
//*******************************************************************************************
#ifndef LDR_OBJECT_STATE_H
#define LDR_OBJECT_STATE_H

#include "pr/common/StdMap.h"
#include "pr/common/Crc.h"
#include "pr/geometry/geometry.h"

// The state of an object, used for maintaining object state across reloads
struct ObjectState
{
	bool		m_wireframe;
	bool		m_enabled;
	Colour32	m_colour;
	bool		m_valid;
};
typedef std::map<uint, ObjectState> TObjectState;

#endif//LDR_OBJECT_STATE_H
