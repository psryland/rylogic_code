//**********************************************************
//
//	Objects managed by the dynamic object map
//
//**********************************************************
// Dynamic objects should contain at least one of these objects
// 
#ifndef PR_DYNAMIC_OBJECT_H
#define PR_DYNAMIC_OBJECT_H

#include "PR/Maths/Maths.h"
#include "PR/DynamicObjectMap/DOMAssertEnable.h"

namespace pr
{
	class DynamicObject
	{
	public:
		enum { INVALID = 0x7FFFFFFF };
		DynamicObject() : m_object_index(INVALID) {}

		void*			m_owner;			// Points to the object that contains this class
		BoundingBox*	m_bounding_box;		// Points to the bounding box for the object

	private:
		friend class DynamicObjectMap;
		uint			m_object_index;
		uint			m_Xbounds[2];
		uint			m_Ybounds[2];
		uint			m_Zbounds[2];
	};
}//namespace pr

#endif//PR_DYNAMIC_OBJECT_H
