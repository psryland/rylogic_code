//**********************************************************
//
//	A triangular table of dynamic objects
//
//**********************************************************
#include "PR/DynamicObjectMap/DOMOverlapTable.h"

using namespace pr;

//*****
// Constructor
DOMOverlapTable::DOMOverlapTable(uint max_dynamics, uint max_overlaps)
:m_max_dynamics(max_dynamics)
,m_overlap_list(max_overlaps)
,m_overlap_index(0)
{
	PR_ASSERT_STR(PR_DBG_DOM, max_dynamics < INDEXMASK, "Too many dynamics");

	// Allocate memory for the overlap table
	m_overlap_table = new uint[m_max_dynamics * (m_max_dynamics - 1) / 2];
	PR_ASSERT(PR_DBG_DOM, m_overlap_table != 0);

	Reset();
}

//*****
// Destructor
DOMOverlapTable::~DOMOverlapTable()
{
	delete m_overlap_table;
}

//*****
// Initialise the elements in the table
void DOMOverlapTable::Reset()
{
	// Reset the overlap table.
	memset(m_overlap_table, 0, sizeof(uint) * m_max_dynamics * (m_max_dynamics - 1) / 2);

	// Reset the overlap list
	m_overlap_list.Destroy();
}

//*****
// Remove any overlaps for the index position of "object_index"
void DOMOverlapTable::ObjectRemoved(uint object_index)
{
	// Clear all of the overlap bits for this object
	uint index = 0;
	if( object_index > 0 )
	{
		index = GetOverlapTableIndex(object_index, 0);
		for( uint objB = 0; objB < object_index; ++objB )
		{
			Overlap(XYZBIT, CLEAR, object_index, objB, &index);
			++index;
		}
	}

	uint row_width	= object_index;
	index += row_width;

	for( uint objA = object_index + 1; objA < m_max_dynamics - 1; ++objA )
	{
		Overlap(XYZBIT, CLEAR, object_index, objA, &index);
		++row_width;
		index += row_width;
	}
}

//*****
// Set a bit in the overlap table
void DOMOverlapTable::Overlap(Bit bit, BitState state, uint objectA, uint objectB, uint* overlap_table_index)
{
	uint index = (overlap_table_index != 0) ? (*overlap_table_index) : (GetOverlapTableIndex(objectA, objectB));
	PR_ASSERT(PR_DBG_DOM, index == GetOverlapTableIndex(objectA, objectB));

	bool was_overlapping = IsOverlap(index);
	switch( state )
	{
	case CLEAR:	m_overlap_table[index] &= ~bit;	break;
	case SET:	m_overlap_table[index] |= bit;	break;
	};
	bool is_overlapping = IsOverlap(index);
	
	// See if the overlappingness has changed
	if( was_overlapping != is_overlapping )
	{
		if( is_overlapping )
		{
			uint overlap_index = m_overlap_list.AddToTail(DOMOverlap(objectA, objectB));
			PR_ASSERT(PR_DBG_DOM, (overlap_index & INDEXMASK) == overlap_index);
			m_overlap_table[index] |= overlap_index;
		}
		else
		{
			m_overlap_list.Detach(m_overlap_table[index] & INDEXMASK);
			m_overlap_table[index] &= ~INDEXMASK;
		}
	}
}

//*****
// Verify the overlap table self consistency
void DOMOverlapTable::Verify() const
{
	// Check every overlap in the list is an overlap in the table
	for( const DOMOverlap* overlap = m_overlap_list.FirstP(); overlap; overlap = m_overlap_list.NextP() )
	{
		PR_ASSERT(PR_DBG_DOM, IsOverlap(GetOverlapTableIndex(overlap->m_objectA, overlap->m_objectB)));
	}

	// Check every overlap in the table is in the list
	for( uint i = 0; i < m_max_dynamics; ++i )
	{
		if( IsOverlap(i) )
		{
			PR_EXPAND(PR_DBG_DOM, const DOMOverlap& overlap = m_overlap_list[m_overlap_table[i] & INDEXMASK];)
			PR_ASSERT(PR_DBG_DOM, (GetOverlapTableIndex(overlap.m_objectA, overlap.m_objectB) == i));
		}
	}
}
