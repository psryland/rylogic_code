//**********************************************************
//
//	Main header for a library for managing moving objects
//
//**********************************************************
//
// Usage:
//	Create an object that has a bounding box and a "DynamicObject"
//	Set the "m_owner" and "m_bounding_box" pointers
//	Add the "DynamicObject" to the dynamic object map using "AddDynamic"
//	Overlaps for the added object will not be available until "Update" has been called on the object
//	To access the overlaps call:
//	FirstOverlap();
//	while( GetOverlap(objectA, objectB) )
//	{
//		//do stuff between objectA and objectB
//	}
//
#ifndef DYNAMICOBJECTMAP_H
#define DYNAMICOBJECTMAP_H

#include "PR/Common/PRTypes.h"
#include "PR/Common/PRListInAnArray.h"
#include "PR/DynamicObjectMap/DynamicObject.h"
#include "PR/DynamicObjectMap/DOMBoundingCoord.h"
#include "PR/DynamicObjectMap/DOMOverlapTable.h"
#include "PR/DynamicObjectMap/DOMAssertEnable.h"

namespace pr
{
	class DynamicObjectMap
	{
	public:
		DynamicObjectMap(uint max_dynamics, uint max_overlaps);
		~DynamicObjectMap();
		
		// Add/Remove objects from the map
		void	AddDynamic(DynamicObject* object);
		void	RemoveDynamic(DynamicObject* object);

		// Update the map
		void	UpdateAll();
		void	Update(DynamicObject* object);

		// Access the overlaps in the map
		void	FirstOverlap();
		template <typename T> bool GetOverlap(T*& objectA, T*& objectB);

		void	Verify() const;

	private:	// Members
		typedef ListInAnArray<DynamicObject*>	TDynamicList;
		typedef ListInAnArray<DOMBoundingCoord>	TDOMBoundingCoordList;
		void	UpdateBound(TDOMBoundingCoordList& list, uint bound_index, float new_value, DOMOverlapTable::Bit bit);

	private:
		TDynamicList			m_dynamic;			// The dynamics that we know about
		TDOMBoundingCoordList	m_Xlist;			// A list of the bounding coordinates along the X axis for a dynamic
		TDOMBoundingCoordList	m_Ylist;			// A list of the bounding coordinates along the Y axis for a dynamic
		TDOMBoundingCoordList	m_Zlist;			// A list of the bounding coordinates along the Z axis for a dynamic
		DOMOverlapTable			m_overlap_table;	// A repository for overlaps found in m_listX and m_listZ
	};

	//*************************************************************
	// Implementation
	//*****
	// Update the map
	inline void DynamicObjectMap::UpdateAll()
	{
		// Go through each object updating the position of it's bounds
		for( DynamicObject* object = m_dynamic.First(); object; object = m_dynamic.Next() )
		{
			Update(object);
		}
	}

	//*****
	// Reset the internal iterator to the first overlap
	inline void DynamicObjectMap::FirstOverlap()
	{
		m_overlap_table.FirstOverlap();
	}

	//*****
	// Return pointers to two overlapping objects
	template <typename T>
	inline bool DynamicObjectMap::GetOverlap(T*& objectA, T*& objectB)
	{
		uint objectA_index;
		uint objectB_index;
		if( m_overlap_table.GetOverlap(objectA_index, objectB_index) )
		{
			objectA = static_cast<T*>(m_dynamic[objectA_index]->m_owner);
			objectB = static_cast<T*>(m_dynamic[objectB_index]->m_owner);
			return true;
		}
		return false;
	}
}//namespace pr

#endif//DYNAMICOBJECTMAP_H
