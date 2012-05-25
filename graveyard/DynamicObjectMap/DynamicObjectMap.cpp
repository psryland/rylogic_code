//**********************************************************
//
//	Library for managing moving objects
//
//**********************************************************

#include "PR/DynamicObjectMap/DynamicObjectMap.h"

using namespace pr;

//*****
// Constructor
DynamicObjectMap::DynamicObjectMap(uint max_dynamics, uint max_overlaps)
:m_dynamic(max_dynamics)
,m_Xlist(max_dynamics * 2)
,m_Ylist(max_dynamics * 2)
,m_Zlist(max_dynamics * 2)
,m_overlap_table(max_dynamics, max_overlaps)
{
	PR_ASSERT_STR(PR_DBG_DOM, max_dynamics < 0x80000000, "Too many dynamics");	// DOMBoundingCoord cannot handle this many
}

//*****
// Destructor
DynamicObjectMap::~DynamicObjectMap()
{}

//*****
// Add a dynamic object to the map
void DynamicObjectMap::AddDynamic(DynamicObject* object)
{
	PR_ASSERT_STR(PR_DBG_DOM, object->m_bounding_box != NULL, "Objects must have a bounding box");

	// Add to the dynamics list
	object->m_object_index = m_dynamic.AddToTail(object);

	// Add the object into the overlap lists
	BoundingBox& bbox = *object->m_bounding_box;
	object->m_Xbounds[0] = m_Xlist.AddToTail(DOMBoundingCoord(DOMBoundingCoord::Lower, object->m_object_index, bbox.Lower()[0]));
	object->m_Xbounds[1] = m_Xlist.AddToTail(DOMBoundingCoord(DOMBoundingCoord::Upper, object->m_object_index, bbox.Upper()[0]));
	object->m_Ybounds[0] = m_Ylist.AddToTail(DOMBoundingCoord(DOMBoundingCoord::Lower, object->m_object_index, bbox.Lower()[1]));
	object->m_Ybounds[1] = m_Ylist.AddToTail(DOMBoundingCoord(DOMBoundingCoord::Upper, object->m_object_index, bbox.Upper()[1]));
	object->m_Zbounds[0] = m_Zlist.AddToTail(DOMBoundingCoord(DOMBoundingCoord::Lower, object->m_object_index, bbox.Lower()[2]));
	object->m_Zbounds[1] = m_Zlist.AddToTail(DOMBoundingCoord(DOMBoundingCoord::Upper, object->m_object_index, bbox.Upper()[2]));
}

//*****
// Remove a dynamic object from the map
void DynamicObjectMap::RemoveDynamic(DynamicObject* object)
{
	// Tell the overlap table to remove overlaps for this object
	m_overlap_table.ObjectRemoved(object->m_object_index);

	// Remove from the overlap lists
	m_Xlist.Detach(object->m_Xbounds[0]);
	m_Xlist.Detach(object->m_Xbounds[1]);
	m_Ylist.Detach(object->m_Ybounds[0]);
	m_Ylist.Detach(object->m_Ybounds[1]);
	m_Zlist.Detach(object->m_Zbounds[0]);
	m_Zlist.Detach(object->m_Zbounds[1]);
	
	// Remove from the dynamics list
	m_dynamic.Detach(object->m_object_index);
}

//*****
// Update an object in the map
void DynamicObjectMap::Update(DynamicObject* object)
{
	PR_ASSERT_STR(PR_DBG_DOM, object->m_bounding_box != NULL, "Objects must have a bounding box");
	PR_ASSERT(PR_DBG_DOM, object->m_object_index != DynamicObject::INVALID);
	PR_ASSERT_STR(PR_DBG_DOM, m_dynamic[object->m_object_index] == object, "This object is not in the map");

	// Update the bounds
	BoundingBox& bbox = *object->m_bounding_box;
	UpdateBound(m_Xlist, object->m_Xbounds[0], bbox.Lower()[0], DOMOverlapTable::XBIT);
	UpdateBound(m_Xlist, object->m_Xbounds[1], bbox.Upper()[0], DOMOverlapTable::XBIT);
	UpdateBound(m_Ylist, object->m_Ybounds[0], bbox.Lower()[1], DOMOverlapTable::YBIT);
	UpdateBound(m_Ylist, object->m_Ybounds[1], bbox.Upper()[1], DOMOverlapTable::YBIT);
	UpdateBound(m_Zlist, object->m_Zbounds[0], bbox.Lower()[2], DOMOverlapTable::ZBIT);
	UpdateBound(m_Zlist, object->m_Zbounds[1], bbox.Upper()[2], DOMOverlapTable::ZBIT);
}

//*****
// Update one bound in a list and the resulting overlaps
void DynamicObjectMap::UpdateBound(TDOMBoundingCoordList& list, uint bound_index, float new_value, DOMOverlapTable::Bit bit)
{
	PR_EXPAND(PR_DBG_DOM, float old_value = list[bound_index].Bound(); old_value;)
	
	// Update our value
	list[bound_index].SetBound(new_value);

	// Check that the list is still sorted
	bool is_upper			= list[bound_index].IsUpper();
	uint object_index		= list[bound_index].ObjectIndex();
	uint prev_index		= list.Prev(bound_index);
	uint next_index		= list.Next(bound_index);
	DOMBoundingCoord* prev	= list.Ptr(prev_index);
	DOMBoundingCoord* next	= list.Ptr(next_index);

	// If the new value needs to move up the list
	if( prev && new_value < prev->Bound() )
	{
		// Find the one to swap with and update our overlaps as we go
		DOMOverlapTable::BitState state = (!is_upper) ? (DOMOverlapTable::SET) : (DOMOverlapTable::CLEAR);
		while( prev && new_value < prev->Bound() )
		{
			// We are moving down so...
			// If we're crossing over an opposite bound we need to add/remove an overlap from the overlap table.
			// If we are a  lower bound (is_upper == false) then we add an overlap for every upper bound we cross
			// If we are an upper bound (is_upper == true)  then we remove an overlap for every lower bound we cross
			if( prev->IsUpper() != is_upper && object_index != prev->ObjectIndex() )
				m_overlap_table.Overlap(bit, state, object_index, prev->ObjectIndex());
			
			prev_index	= list.Prev(prev_index);
			prev		= list.Ptr(prev_index);
		}

		list.MoveToAfter(bound_index, prev_index);
	}

	// If the new value needs to move down the list
	else if( next && new_value > next->Bound() )
	{
		// Find the one to swap with and update our overlaps as we go
		DOMOverlapTable::BitState state = (is_upper) ? (DOMOverlapTable::SET) : (DOMOverlapTable::CLEAR);
		while( next && new_value > next->Bound() )
		{
			// We are moving up so...
			// If we're crossing over an opposite bound we need to add/remove an overlap from the overlap table.
			// If we are a  lower bound (is_upper == false) then we remove an overlap for every upper bound we cross
			// If we are an upper bound (is_upper == true)  then we add an overlap for every lower bound we cross
			if( next->IsUpper() != is_upper && object_index != next->ObjectIndex() )
				m_overlap_table.Overlap(bit, state, object_index, next->ObjectIndex());

			next_index	= list.Next(next_index);
			next		= list.Ptr(next_index);
		}

		list.MoveToBefore(bound_index, next_index);
	}
}

//*****
// Check for self inconsistencies
void DynamicObjectMap::Verify() const
{
	// Check the "m_dynamic" list is valid
	for( uint i = 0; i < m_dynamic.GetCount(); ++i )
	{
		PR_ASSERT(PR_DBG_DOM, m_dynamic[i]->m_object_index == i);
	}
	
	// Check every object in "m_dynamic" has valid bounds in the lists
	for( DynamicObject* object = m_dynamic.First(); object; object = m_dynamic.Next() )
	{
		PR_ASSERT(PR_DBG_DOM, m_Xlist[object->m_Xbounds[0]].ObjectIndex() == object->m_object_index);
		PR_ASSERT(PR_DBG_DOM, m_Xlist[object->m_Xbounds[1]].ObjectIndex() == object->m_object_index);
		PR_ASSERT(PR_DBG_DOM, m_Ylist[object->m_Ybounds[0]].ObjectIndex() == object->m_object_index);
		PR_ASSERT(PR_DBG_DOM, m_Ylist[object->m_Ybounds[1]].ObjectIndex() == object->m_object_index);
		PR_ASSERT(PR_DBG_DOM, m_Zlist[object->m_Zbounds[0]].ObjectIndex() == object->m_object_index);
		PR_ASSERT(PR_DBG_DOM, m_Zlist[object->m_Zbounds[1]].ObjectIndex() == object->m_object_index);
	}

	// Check every object in the lists is also in "m_dynamic"
	for( int i = 0; i < 3; ++i )
	{
		const TDOMBoundingCoordList& list = (i == 0) ? (m_Xlist) : ( (i == 1)?(m_Ylist):(m_Zlist) );

		for( const DOMBoundingCoord* coord = list.FirstP(); coord; coord = list.NextP() )
		{
			PR_ASSERT(PR_DBG_DOM, coord->ObjectIndex() == m_dynamic[coord->ObjectIndex()]->m_object_index);
		}
	}

	// Get the overlap table to check itself
	m_overlap_table.Verify();
}
