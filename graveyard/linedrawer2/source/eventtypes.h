//***************************************
// Events sent within LineDrawer
//***************************************

#ifndef LDR_EVENT_TYPES_H
#define LDR_EVENT_TYPES_H

struct GUIUpdate
{
	enum EType
	{
		EType_GlobalWireframe
	};
	
	EType			m_type;
	union {
	void*			m_ptr;
	unsigned int	m_data;
	};
};

#endif//LDR_EVENT_TYPES_H