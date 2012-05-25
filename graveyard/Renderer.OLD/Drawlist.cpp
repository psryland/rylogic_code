//************************************************************
//
//	Drawlist
//
//************************************************************
#include "Stdafx.h"
#include "PR/Renderer/Drawlist.h"

using namespace pr;
using namespace pr::rdr;

//*****
// Constructor
Drawlist::Drawlist()
{
	Clear();
}

//*****
// Reset the drawlist
void Drawlist::Clear()
{
	m_drawlist_end.m_drawlist_next = &m_drawlist_end;
	m_drawlist_end.m_drawlist_prev = &m_drawlist_end;
	m_drawlist_end.m_instance_next = 0;
	m_drawlist_end.m_instance = 0;
	m_drawlist_end.m_nugget = 0;

	m_drawlist_element_pool.ReclaimAll();
	m_instance_to_dle.clear();
	m_sorter.clear();

	m_sorter[sortkey::Max] = &m_drawlist_end;
}

//*****
// Add an instance to the draw list. Instances persist in the drawlist until they
// are removed or 'Clear()' is called.
void Drawlist::AddInstance(const InstanceBase& instance)
{
	// Check that the instance has not been added to the same viewport twice
	PR_ASSERT_STR(PR_DBG_RDR, m_instance_to_dle.find(&instance) == m_instance_to_dle.end(), "This instance is already in this draw list");

	// Add an entry to the instance to drawlist element lookup table
	DrawListElement*& instance_dle_head = m_instance_to_dle[&instance];
	instance_dle_head = 0;

	// Create a chain of drawlist elements for this instance that
	// correspond to the render nuggets of teh renderable
	TNuggetList::const_iterator nug_iter     = instance.m_model->m_render_nugget.begin();
	TNuggetList::const_iterator nug_iter_end = instance.m_model->m_render_nugget.end();
	for( ; nug_iter != nug_iter_end; ++nug_iter )
	{
		// Allocate and fill out a dle for this nugget
		DrawListElement& element = GetDLE();
		element.m_instance	= &instance;
		element.m_nugget	= &*nug_iter;

		// Add it to the chain list for the instance
		element.m_instance_next = instance_dle_head;
		instance_dle_head		= &element;
	}

	// Now add each drawlist element to the draw list
	for( DrawListElement* element = instance_dle_head; element; element = element->m_instance_next )
	{
		// Use the sorter to locate the drawlist element that should succeed 'element'.
		TSorter::iterator location = m_sorter.lower_bound(element->m_nugget->m_sort_key);
		DrawListElement* next_element = location->second;

		// If the sort key already exists in the sorter then the drawlist element it points
		// to is the one we want to insert this element before. If not, then we need to add
		// the sort key and element pointer. There should always be an element to insert before
		// because 'm_drawlist_end' is inserted at sortkey::Max.
		if( location->first != element->m_nugget->m_sort_key )
		{
			m_sorter.insert(location, TSorter::value_type(element->m_nugget->m_sort_key, element));
		}
		
		// Insert 'element' before 'next_element'
		element->m_drawlist_prev = next_element->m_drawlist_prev;
		element->m_drawlist_next = next_element;
		next_element->m_drawlist_prev->m_drawlist_next = element;
		next_element->m_drawlist_prev = element;
	}
}

//*****
// Remove an instance from the drawlist
void Drawlist::RemoveInstance(const InstanceBase& instance)
{
	// Check that the instance has not been added to the same viewport twice
	TInstanceToDLE::iterator dle_iter = m_instance_to_dle.find(&instance);
	if( dle_iter == m_instance_to_dle.end() ) return; // Not in the instance list

	// Remove each of the drawlist elements from the drawlist
	for( DrawListElement* element = dle_iter->second; element; element = element->m_instance_next )
	{
		element->m_drawlist_prev->m_drawlist_next = element->m_drawlist_next;
		element->m_drawlist_next->m_drawlist_prev = element->m_drawlist_prev;
	}

	// Return the drawlist elements to the free pool
	ReturnDLEList(dle_iter->second);
}

//*****
// Get a drawlist element from the pool
DrawListElement& Drawlist::GetDLE()
{
	return *m_drawlist_element_pool.Get();
}

//*****
// Return a list of drawlist elements to the pool. Elements should be
// connected using their 'm_instance_next' member
void Drawlist::ReturnDLEList(DrawListElement* element)
{
	while( element )
	{
		DrawListElement* current = element;
		element = element->m_instance_next;

		m_drawlist_element_pool.Return(current);
	}
}
