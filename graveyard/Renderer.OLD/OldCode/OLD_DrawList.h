//***************************************************************************
//
//	Draw List
//
//***************************************************************************
//
// This class is used to build the draw list. There is one of these in each viewport
//

#ifndef DRAW_LIST_H
#define DRAW_LIST_H

#include "Common/PRBucketSorter.h"
#include "Renderer/DrawListElement.h"
#include "Renderer/RenderNugget.h"

class DrawList
{
public:
	//enum { NUM_BUCKETS_POW2 = 8, INITIAL_BUCKET_SIZE = 32 };
	//DrawList()													{ m_sorter.Initialise(NUM_BUCKETS_POW2, INITIAL_BUCKET_SIZE); }
	//void				Release()								{ m_sorter.ReleaseMemory(); }
	//uint				GetCount() const						{ return m_sorter.GetCount(); }
	//void				Reset()									{ m_sorter.Reset(); }
	//void				Add(const DrawListElement& element)		{ m_sorter.Add(element, element.m_nugget->m_sort_key); }
	//void				Sort()									{ m_sorter.Sort(); }
	//void				AdjustBoundaries()						{ m_sorter.AdjustBoundaries(); }
	//DrawListElement*	First()									{ return m_sorter.FirstP(); }
	//DrawListElement*	Next()									{ return m_sorter.NextP(); }
	//DrawListElement*	Current()								{ return m_sorter.CurrentP(); }
	//void				Bookmark() const						{ m_sorter.Bookmark(); }
	//void				RestoreBookmark() const					{ m_sorter.RestoreBookmark(); }
	
private:
	PR::BucketSorter<DrawListElement>	m_sorter;
};

#endif//DRAW_LIST_H