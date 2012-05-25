//*******************************************************************************************
// LineDrawer
//	(c)opyright 2002 Rylogic Limited
//*******************************************************************************************
// A class to interpret strings into LdrObject objects
// and past them on to the data manager

#ifndef LDR_PARSER_H
#define LDR_PARSER_H

#include "pr/common/Script.h"
#include "LineDrawer/Objects/LdrObjectsForward.h"
#include "LineDrawer/Source/Forward.h"
#include "LineDrawer/Source/CameraView.h"
#include "LineDrawer/Source/LockMask.h"

// A collection on objects and data extracted from an input source
struct ParseResult
{
	TLdrObjectPtrVec		m_objects;					// LdrObject objects
	EGlobalWireframeMode	m_global_wireframe_mode;	// -1=not set, 0=solid, 1=wireframe, 2=solid+wire
	LockMask				m_lock_mask;				// Navigation locks
	ViewMask				m_view_mask;				// A mask of bits that where set in the view
	CameraView				m_view;						// The view contained in the source

	ParseResult();
	~ParseResult();
	std::size_t NumObjects() const			{ return m_objects.size(); }
	LdrObject*	GetObject(std::size_t i)
	{
		PR_ASSERT(PR_DBG_LDR, i < m_objects.size() && m_objects[i] != 0); 
		LdrObject* to_return = m_objects[i];
		m_objects[i] = 0;
		return to_return;
	}
};

// Converts a source string into a ParseResult
// Returns true if the complete source was parsed.
bool ParseSource(LineDrawer& ldr, char const* src, ParseResult& result);

// Converts the contents of a collection of files into a ParseResult
// Returns true if the complete source was parsed.
bool ParseSource(LineDrawer& ldr, FileLoader& file_loader, ParseResult& result);

#endif//LDR_PARSER_H