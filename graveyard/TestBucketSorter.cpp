//*****************************************
//
//	Unit test for PRBucketSorter.h
//
//*****************************************
#include "PR/Common/PRBucketSorter.h"
#include "PR/Maths/Maths.h"

namespace TestBucketSorter
{
	using namespace pr;

	struct Thing
	{
		Thing()
		{
			m_sort_key.m_key = 0;
			m_sort_key.m_key |= (uint64)Rand(0, 15) * 0x1000000000000000;
			m_sort_key.m_key |= (uint64)Rand(0, 15) * 0x0100000000000000;
			m_sort_key.m_key |= (uint64)Rand(0, 15) * 0x0010000000000000;
			m_sort_key.m_key |= (uint64)Rand(0, 15) * 0x0001000000000000;
			m_sort_key.m_key |= (uint64)Rand(0, 15) * 0x0000100000000000;
			m_sort_key.m_key |= (uint64)Rand(0, 15) * 0x0000010000000000;
			m_sort_key.m_key |= (uint64)Rand(0, 15) * 0x0000001000000000;
			m_sort_key.m_key |= (uint64)Rand(0, 15) * 0x0000000100000000;
			m_sort_key.m_key |= (uint64)Rand(0, 15) * 0x0000000010000000;
			m_sort_key.m_key |= (uint64)Rand(0, 15) * 0x0000000001000000;
			m_sort_key.m_key |= (uint64)Rand(0, 15) * 0x0000000000100000;
			m_sort_key.m_key |= (uint64)Rand(0, 15) * 0x0000000000010000;
			m_sort_key.m_key |= (uint64)Rand(0, 15) * 0x0000000000001000;
			m_sort_key.m_key |= (uint64)Rand(0, 15) * 0x0000000000000100;
			m_sort_key.m_key |= (uint64)Rand(0, 15) * 0x0000000000000010;
			m_sort_key.m_key |= (uint64)Rand(0, 15) * 0x0000000000000001;
		}
		SortKey m_sort_key;
	};

	void Run()
	{
		typedef BucketSorter<Thing> TSorter;
		TSorter sorter;
		sorter.Initialise(4, 10);

		for( uint j = 0; j < 50; ++j )
		{
			Thing thing;
			sorter.Add(thing, thing.m_sort_key);
		}
	
		uint i = 0;
		for( TSorter::const_iterator t = sorter.begin(), t_end = sorter.end(); t != t_end; ++t )
			printf("%2.2d: %8.8x %8.8x \n", i++, t->m_sort_key.High(), (uint32)t->m_sort_key.Low());
		
		sorter.Sort();

		i = 0;
		for( TSorter::const_iterator t = sorter.begin(), t_end = sorter.end(); t != t_end; ++t )
			printf("%2.2d: %8.8x %8.8x \n", i++, t->m_sort_key.High(), (uint32)t->m_sort_key.Low());
	
		getch();
	}
}//namespace TestBucketSorter
