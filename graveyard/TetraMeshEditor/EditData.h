//***************************************
// TetraMesh Editor
//***************************************
#pragma once
#include "pr/common/StdList.h"
#include "TetraMeshEx.h"
#include "Selection.h"

enum EEditType
{
	EEditType_MoveVert,
};

struct EditData
{
	EEditType	m_type;
	Selection	m_selection;
	pr::v4		m_base_pos;
	pr::v2		m_mouse_base_pos;
};

typedef std::list<EditData>	TEditHistory;
