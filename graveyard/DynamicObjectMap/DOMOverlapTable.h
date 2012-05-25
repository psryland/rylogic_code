//**********************************************************
//
//	A triangular table of dynamic objects
//
//**********************************************************
#ifndef DOM_OVERLAP_TABLE_H
#define DOM_OVERLAP_TABLE_H

#include "PR/Common/PRListInAnArray.h"
#include "PR/DynamicObjectMap/DynamicObject.h"

namespace pr
{
	struct DOMOverlap
	{
		struct pr_is_pod { enum { value = true }; };

		DOMOverlap(uint objA, uint objB) : m_objectA(objA), m_objectB(objB) {}
		uint m_objectA;
		uint m_objectB;
	};

	//*****
	// A triangular table for recording overlaps
	class DOMOverlapTable
	{
	public:
		enum BitState	{ CLEAR = 0, SET = 1 };
		enum Bit		{ XBIT = 0x80000000, YBIT = 0x40000000, ZBIT = 0x20000000, XYZBIT = 0xE0000000, INDEXMASK = 0x1FFFFFFF };

		DOMOverlapTable(uint max_dynamics, uint max_overlaps);
		~DOMOverlapTable();
		void	Reset();
		void	ObjectRemoved(uint object_index);
		void	Overlap(Bit bit, BitState state, uint objectA, uint objectB, uint* overlap_table_index = NULL);

		void	FirstOverlap();
		bool	GetOverlap(uint& objectA, uint& objectB);

		void	Verify() const;

	private:
		typedef ListInAnArray<DOMOverlap> DOMOverlapList;
		uint	GetOverlapTableIndex(uint objectA, uint objectB) const;
		bool	IsOverlap(uint overlap_table_index) const;

	private:
		uint			m_max_dynamics;		// The width of the top row of the m_overlap_table + 1
		uint*			m_overlap_table;	// 1 bit = X overlap, 1 bit = Y overlap, 1 bit = Z overlap,
		DOMOverlapList	m_overlap_list;		// 29 bits = index in m_overlap_list of the position whose
		uint			m_overlap_index;	// 'm_next' points to our pair. This is like this so that
	};										// we can remove elements from 'm_overlap_list' in order 1 time.

	//***************************************************************
	// Implementation
	//*****
	// Reset the internal iterator to the first overlap
	inline void DOMOverlapTable::FirstOverlap()
	{
		m_overlap_index = 0;
	}

	//*****
	// Retrieve an overlap from the overlap list
	inline bool DOMOverlapTable::GetOverlap(uint& objectA, uint& objectB)
	{
		if( m_overlap_index == m_overlap_list.GetCount() ) return false;
		objectA = m_overlap_list[m_overlap_index].m_objectA;
		objectB = m_overlap_list[m_overlap_index].m_objectB;
		++m_overlap_index;
		return true;
	}

	//*****
	// Return the index into the overlap table for two object indices
	inline uint DOMOverlapTable::GetOverlapTableIndex(uint objectA, uint objectB) const
	{
		PR_ASSERT_STR(PR_DBG_DOM, objectA != objectB, "Invalid pair of objects, no index available");
		if( objectA < objectB ) return objectB * (objectB - 1) / 2 + objectA;
		else					return objectA * (objectA - 1) / 2 + objectB;
	}

	//*****
	// Returns true if 'overlap_table_index' is a complete overlap
	inline bool DOMOverlapTable::IsOverlap(uint overlap_table_index) const
	{
		return (m_overlap_table[overlap_table_index] & XYZBIT) == XYZBIT;
	}
}//namespace pr

#endif//DOM_OVERLAP_TABLE_H
