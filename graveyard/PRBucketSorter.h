//***********************************************************************//
//                   A template bucket sorter class                      //
//                                                                       //
//                  (c)opyright Jul 2004 Paul Ryland                     //
//                                                                       //
//***********************************************************************//
//	Usage:
//		struct Thing
//		{
//			void* m_ptr;
//		};
//		BucketSorter<Thing>	sorter(100)
//
//		For(...)
//			sorter.Add(Thing(), sort_key);
//
//		sorter.Sort();
//		BucketSorter<Thing>::CIter iter = sorter.Begin();
//		while( iter != sorter.End() )
//		{ ++iter; }
//
//		or
//		sorter.GetArray(pr::Array<Thing>& array);
//
//	Optional defines
//
//	Methods and members that can be required for this template class:
//
#ifndef PRBUCKET_SORTER_H
#define PRBUCKET_SORTER_H

#include "PR/Common/StdAlgorithm.h"
#include "PR/Common/StdVector.h"
#include "PR/Common/PRTypes.h"
#include "PR/Common/PRAssert.h"
#include "PR/Common/PRSortKey.h"

namespace pr
{
	template <typename T> struct BucketSorterCIter;

	// The Bucket Sorter
	template <typename T>
	class BucketSorter
	{
	private:	// Structures
		friend BucketSorterCIter<T>;
		
		struct BucketElement				{ T m_item; SortKey m_sort_key; };
		typedef std::vector<BucketElement>	TBucketElementArray;

		struct Bucket						{ SortKey m_lower_bound;	TBucketElementArray m_bucket; };
		typedef std::vector<Bucket>			TBucketArray;

		struct Range						{ uint m_lower; };
		typedef std::vector<Range>			TRangeArray;

	public:
		BucketSorter();

		void	Initialise(uint num_buckets_pow2, uint num_elements_per_bucket);	// num_buckets must be a power of two
		bool	IsInitialised() const				{ return m_num_bucket_pow2 > 0; }
		void	Reset();
		void	Add(T item, SortKey sort_key);
		void	Sort();
		void	AdjustBoundaries();
		void	AdjustBoundariesIfNeeded()			{ if( m_boundary_adjust_needed ) AdjustBoundaries(); }

		typedef BucketSorterCIter<T> const_iterator;

		const_iterator	begin() const				{ return const_iterator(m_bucket_array.begin(), m_bucket_array.end()); }
		const_iterator	end() const					{ return const_iterator(m_bucket_array.end(),   m_bucket_array.end()); }
		std::size_t		size() const				{ return m_count; }
		bool			empty() const				{ return m_count == 0; }

	private:
		uint	RangeIndex (const SortKey& sort_key) const;
		uint	BucketIndex(const SortKey& sort_key) const;

	private:
		TBucketArray	m_bucket_array;				// The Buckets
		TRangeArray		m_range;					// The ranges of buckets for optimised searching
		uint			m_num_bucket_pow2;			// The number of buckets = 2^m_num_bucket_pow2
		uint			m_elements_per_bucket;		// The ideal number of elements in a bucket
		uint			m_nearlyfull_threshold;		// The number of elements in a bucket before we consider it nearly full
		uint			m_count;					// The total number of items in the bucket sorter
		uint			m_num_buckets_used;			// The number of buckets we're actually using
		bool			m_boundary_adjust_needed;	// True when at least one bucket is 75% full or more
	};

	//****************************************************************
	// Implementation
	template <typename T>
	inline bool operator < (const BucketSorter<T>::BucketElement& a, const BucketSorter<T>::BucketElement& b)
	{
		return a.m_sort_key.m_key < b.m_sort_key.m_key;
	}

	//*****
	// Constructor
	template <typename T>
	inline BucketSorter<T>::BucketSorter()
	:m_num_bucket_pow2			(0)
	,m_elements_per_bucket		(0)
	,m_nearlyfull_threshold		(0)
	,m_count					(0)
	,m_num_buckets_used			(0)
	,m_boundary_adjust_needed	(false)
	{}

	//*****
	// Initialise
	template <typename T>
	void BucketSorter<T>::Initialise(uint num_buckets_pow2, uint num_elements_per_bucket)
	{
		m_num_bucket_pow2			= num_buckets_pow2;
		m_elements_per_bucket		= num_elements_per_bucket;
		m_nearlyfull_threshold		= num_elements_per_bucket * 3 / 4;
		m_bucket_array				.resize(1 + (1 << num_buckets_pow2));
		m_range						.resize(1 + (1 << num_buckets_pow2));
		m_count						= 0;
		m_num_buckets_used			= 1 << num_buckets_pow2;
		m_boundary_adjust_needed	= false;

		// Initialise the buckets
		const uint64 SORT_KEY_STEP_SIZE = (SortKey::Max >> m_num_bucket_pow2) + 1;
		uint					num_buckets		= (uint)m_bucket_array.size() - 1;
		TRangeArray::iterator	range			= m_range.begin();
		TBucketArray::iterator	bucket			= m_bucket_array.begin();
		for( uint b = 0; b < num_buckets; ++b, ++bucket, ++range )
		{
			bucket->m_bucket			.reserve(m_elements_per_bucket);
			bucket->m_lower_bound.m_key	= b * SORT_KEY_STEP_SIZE;
			range->m_lower				= b;
		}

		// Initialise the dummies
		bucket->m_lower_bound.m_key	= SortKey::Max;
		range->m_lower				= num_buckets;
	}

	//*****
	// Reset the buckets
	template <typename T>
	void BucketSorter<T>::Reset()
	{
		const uint64 SORT_KEY_STEP_SIZE = (SortKey::Max >> m_num_bucket_pow2) + 1;
		PR_ASSERT(PR_DBG_COMMON, IsInitialised());

		m_count = 0;
		m_boundary_adjust_needed = false;

		// Update the ranges
		uint					bucket_index	= (uint)(-1);
		uint64					range_boundary	= 0;
		uint					num_ranges		= 1 << m_num_bucket_pow2;
		TBucketArray::iterator	bucket			= m_bucket_array.begin();
		TRangeArray::iterator	range			= m_range.begin();
		for( uint r = 0; r < num_ranges; ++r, ++range, range_boundary += SORT_KEY_STEP_SIZE )
		{
			// Watch out for overflow and infinite loops here...
			while( bucket->m_lower_bound.m_key <= range_boundary && range_boundary - bucket->m_lower_bound.m_key < SORT_KEY_STEP_SIZE )
			{
				PR_ASSERT(PR_DBG_COMMON, bucket->m_lower_bound.m_key < (bucket + 1)->m_lower_bound.m_key);
				bucket->m_bucket.clear();
				++bucket;		
				++bucket_index;
			}
			PR_ASSERT(PR_DBG_COMMON, bucket_index < m_num_buckets_used);
			range->m_lower = bucket_index;
		}

		// Initialise the remaining buckets
		while( bucket_index < m_num_buckets_used )
		{
			bucket->m_bucket.clear();
			++bucket;
			++bucket_index;
		}

		range->m_lower = m_num_buckets_used;
	}

	//*****
	// Add an element to the bucket sorter
	template <typename T>
	inline void BucketSorter<T>::Add(T item, SortKey sort_key)
	{
		PR_ASSERT(PR_DBG_COMMON, m_num_buckets_used > 0);

		BucketElement element;
		element.m_item		= item;
		element.m_sort_key	= sort_key;
		uint bucket_index	= BucketIndex(sort_key);
		m_bucket_array[bucket_index].m_bucket.push_back(element);
		++m_count;
	}

	//*****
	// Sort the buckets
	template <typename T>
	inline void BucketSorter<T>::Sort()
	{
		TBucketArray::iterator iter = m_bucket_array.begin();
		for( uint b = 0; b < m_num_buckets_used; ++b, ++iter )
		{
			if( iter->m_bucket.empty() ) continue;
			if( iter->m_bucket.size() > m_nearlyfull_threshold ) m_boundary_adjust_needed = true;
			std::sort(iter->m_bucket.begin(), iter->m_bucket.end());
		}
	}

	//*****
	// Find the bucket header range to begin the binary search from
	template <typename T>
	inline uint BucketSorter<T>::RangeIndex(const SortKey& sort_key) const
	{
		const uint SHIFT = (SortKey::BitLength - m_num_bucket_pow2);
		uint64 range_index = sort_key.m_key >> SHIFT;
		PR_ASSERT(PR_DBG_COMMON, range_index < (uint)m_range.size() - 1);
		return static_cast<uint32>(range_index);
	}

	//*****
	// Return the bucket index from a sort key
	template <typename T>
	uint BucketSorter<T>::BucketIndex(const SortKey& sort_key) const
	{
		uint range_index = RangeIndex(sort_key);

		// Binary search within this range
		uint lower = m_range[range_index + 0].m_lower;
		uint upper = m_range[range_index + 1].m_lower;
		PR_ASSERT(PR_DBG_COMMON, lower < m_num_buckets_used && upper <= m_num_buckets_used);
		while( upper > lower )
		{
			uint32 i = (upper + lower) / 2;
			if( sort_key.m_key < m_bucket_array[i + 0].m_lower_bound.m_key )
			{
				upper = i;
			}
			else if( sort_key.m_key >= m_bucket_array[i + 1].m_lower_bound.m_key )
			{
				lower = i + 1;
			}
			else
			{
				return i;
			}
		}
		PR_ASSERT(PR_DBG_COMMON, lower == upper);
		PR_ASSERT(PR_DBG_COMMON, m_bucket_array[lower + 0].m_lower_bound.m_key <= sort_key.m_key &&
								 m_bucket_array[lower + 1].m_lower_bound.m_key >  sort_key.m_key );
		return lower;
	}

	//*****
	// Adjust the bucket lower boundaries based on the current bucket sorter contents
	template <typename T>
	void BucketSorter<T>::AdjustBoundaries()
	{
		if( m_count == 0 ) return;

		// This is the number of elements we want per bucket
		uint num_buckets	= (uint)m_bucket_array.size() - 1;
		uint num_per_bucket = 1 + m_count / num_buckets;

		// Adjust the bucket boundaries so that the number of
		// elements in each bucket is approximately ideal
		TBucketArray::iterator	first_bucket	= m_bucket_array.begin();
		TBucketArray::iterator	bucket			= first_bucket;					// The current bucket
		TBucketArray::iterator	adj_bucket		= first_bucket;					// The bucket whose boundary we're adjusting
		uint					element_count	= 0;							// The number of elements in the current 'adj_bucket'
		for( uint b = 0; b < num_buckets; ++b, ++bucket )
		{
			TBucketElementArray::iterator element,  element_end = bucket->m_bucket.end();
			for( element = bucket->m_bucket.begin(); element != element_end; ++element )
			{
				// Adjust the bucket boundary
				++element_count;
				if( element_count >= num_per_bucket && element->m_sort_key.m_key > adj_bucket->m_lower_bound.m_key )
				{
					adj_bucket->m_bucket.reserve((element_count * 3)/2);

					++adj_bucket;
					PR_ASSERT(PR_DBG_COMMON, adj_bucket - first_bucket <= (int)num_buckets);
					adj_bucket->m_lower_bound.m_key = element->m_sort_key.m_key;
					element_count = 0;
				}
			}
		}
	
		++adj_bucket;
		PR_ASSERT(PR_DBG_COMMON, adj_bucket - first_bucket <= (int)num_buckets);
		adj_bucket->m_lower_bound.m_key	= SortKey::Max;
		m_num_buckets_used				= (uint)(adj_bucket - first_bucket);
	}

	//***************************************************************************
	// Iterators
	template <typename T>
	struct BucketSorterCIter
	{
		BucketSorterCIter() {}
		BucketSorterCIter(BucketSorter<T>::TBucketArray::const_iterator iter, BucketSorter<T>::TBucketArray::const_iterator end);
		BucketSorterCIter&	operator ++();
		const T* operator ->() const { return &m_elm_iter->m_item; }
		const T& operator * () const { return  m_elm_iter->m_item; }

		BucketSorter<T>::TBucketArray::const_iterator			m_bkt_iter;
		BucketSorter<T>::TBucketArray::const_iterator			m_bkt_end;
		BucketSorter<T>::TBucketElementArray::const_iterator	m_elm_iter;
		BucketSorter<T>::TBucketElementArray::const_iterator	m_elm_end;
	};

	template <typename T>
	inline bool	operator == (const BucketSorterCIter<T>& a, const BucketSorterCIter<T>& b)
	{
		return a.m_bkt_iter == b.m_bkt_iter && a.m_elm_iter == b.m_elm_iter;
	}
	template <typename T>
	inline bool	operator != (const BucketSorterCIter<T>& a, const BucketSorterCIter<T>& b)
	{
		return !(a == b);
	}

	//*****
	// Construct a const iterator
	template <typename T>
	BucketSorterCIter<T>::BucketSorterCIter(BucketSorter<T>::TBucketArray::const_iterator iter, BucketSorter<T>::TBucketArray::const_iterator end)
	:m_bkt_iter(iter)
	,m_bkt_end(end)
	,m_elm_iter(end->m_bucket.end())
	,m_elm_end(end->m_bucket.end())
	{
		// Find the first bucket with something in
		while( m_bkt_iter != m_bkt_end && m_bkt_iter->m_bucket.empty() ) { ++m_bkt_iter; }
		if( m_bkt_iter == m_bkt_end ) return;
		
		m_elm_iter	= m_bkt_iter->m_bucket.begin();
		m_elm_end	= m_bkt_iter->m_bucket.end();
	}

	//*****
	// Increment a const iterator
	template <typename T>
	inline BucketSorterCIter<T>& BucketSorterCIter<T>::operator ++()
	{
		++m_elm_iter;
		if( m_elm_iter == m_elm_end )
		{
			// Find the next bucket with something in
			++m_bkt_iter;
			while( m_bkt_iter != m_bkt_end && m_bkt_iter->m_bucket.empty() ) { ++m_bkt_iter; }
			if( m_bkt_iter == m_bkt_end )
			{
				m_elm_iter	= m_bkt_end->m_bucket.end();
				m_elm_end	= m_bkt_end->m_bucket.end();
			}
			else
			{
                m_elm_iter	= m_bkt_iter->m_bucket.begin();
				m_elm_end	= m_bkt_iter->m_bucket.end();
			}
		}
		return *this;
	}

}//namespace pr

#endif//PRBUCKET_SORTER_H
