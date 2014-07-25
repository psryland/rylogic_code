//***********************************************
// pr::deque<>
//  Copyright (c) Rylogic Ltd 2014
//***********************************************
// A version of std::deque with configurable block size
// and proper type alignment.
#pragma once

// <type_traits> was introduced in sp1
#if defined(_MSC_FULL_VER) && _MSC_FULL_VER < 150030729
#error VS2008 SP1 or greater is required to build this file
#endif

#include <memory>
#include <iterator>
#include <stdexcept>
#include <algorithm>
#include <type_traits>
#include <cassert>
#include "pr/common/allocator.h"
#pragma intrinsic(memcmp, memcpy, memset, strcmp)

#include <deque>
namespace pr
{
	#ifndef PR_NOEXCEPT
	#   define PR_NOEXCEPT_DEFINED
	#   if _MSC_VER >= 1900
	#      define PR_NOEXCEPT noexcept
	#   else
	#      define PR_NOEXCEPT throw()
	#   endif
	#endif

	namespace impl
	{
		namespace deque
		{
			template <typename Type> struct citer
			{
				typedef std::random_access_iterator_tag iterator_category;
			};
			template <typename Type> struct miter
			{
				typedef std::random_access_iterator_tag iterator_category;
				operator citer<Type>() const { return citer<Type>(); }
			};

			// std::aligned_type doesn't work for alignments > 8 on VC++
			template <int Alignment> struct aligned_type {};
			template <> struct aligned_type<1>   {                        struct type { char   a;      }; };
			template <> struct aligned_type<2>   {                        struct type { short  a;      }; };
			template <> struct aligned_type<4>   {                        struct type { int    a;      }; };
			template <> struct aligned_type<8>   {                        struct type { double a;      }; };
			template <> struct aligned_type<16>  { __declspec(align(16 )) struct type { char   a[16];  }; };
			template <> struct aligned_type<32>  { __declspec(align(32 )) struct type { char   a[32];  }; };
			template <> struct aligned_type<64>  { __declspec(align(64 )) struct type { char   a[64];  }; };
			template <> struct aligned_type<128> { __declspec(align(128)) struct type { char   a[128]; }; };

			template <int Size, int Alignment> struct aligned_storage
			{
				union type
				{
					unsigned char bytes[Size];
					typename aligned_type<Alignment>::type aligner;
				};
			};
		}
	}

	// Not intended to be a complete replacement, just a 90% substitute
	// Allocator = the type to do the allocation/deallocation. *Can be a pointer to an std::allocator like object*
	template <typename Type, std::size_t BlockSize=16, typename Allocator=pr::aligned_alloc<Type> >
	class deque
	{
	public:
		typedef deque<Type,BlockSize,Allocator> type;
		typedef typename std::remove_pointer<Allocator>::type AllocType; // The type of the allocator ignoring pointers
		typedef impl::deque::citer<Type> const_iterator;
		typedef impl::deque::miter<Type> iterator;
		typedef Type           value_type;
		typedef std::size_t    size_type;
		typedef std::ptrdiff_t difference_type;
		typedef Type const*    const_pointer;
		typedef Type*          pointer;
		typedef Type const&    const_reference;
		typedef Type&          reference;
		typedef unsigned char  byte;

		enum
		{
			TypeSizeInBytes  = sizeof(Type),
			TypeIsPod        = std::is_pod<Type>::value,
			TypeAlignment    = std::alignment_of<Type>::value,
			CountPerBlock    = BlockSize,
			BlockSizeInBytes = BlockSize * TypeSizeInBytes,
		};

		// type specific traits
		template <bool pod> struct base_traits;
		template <> struct base_traits<false>
		{
		};
		template <> struct base_traits<true>
		{
		};
		struct traits :base_traits<TypeIsPod>
		{
		};

	private:
		typedef typename impl::deque::aligned_storage<TypeSizeInBytes, TypeAlignment>::type Block;
		static_assert((std::alignment_of<Block>::value % TypeAlignment) == 0, "Blocks don't have the correct alignment");

		// Pointer to a block
		typedef Block* BlockPtr;

		// Map of pointers to blocks
		struct BlockPtrMap
		{
			Allocator m_allocator;   // The memory allocator
			BlockPtr* m_ptrs;     // The array of block pointers, size always pow2
			size_type m_capacity; // The length of 'm_ptrs', either 0 or pow2
			size_type m_offset;   // Offset to the first in-use block
			size_type m_count;    // Number of in-use blocks

			BlockPtrMap(Allocator const& allocator = Allocator())
				:m_allocator(allocator)
				,m_ptrs()
				,m_capacity()
				,m_offset()
				,m_count()
			{}

			// The memory allocator
			// Access to the allocator object (independant of whether its a pointer or instance)
			// (enable_if requires type inference to work, hence the 'A' template parameter)
			template <typename A> typename std::enable_if<!std::is_pointer<A>::value, AllocType const&>::type alloc(A) const { return m_allocator; }
			template <typename A> typename std::enable_if< std::is_pointer<A>::value, AllocType const&>::type alloc(A) const { return *m_allocator; }
			template <typename A> typename std::enable_if<!std::is_pointer<A>::value, AllocType&      >::type alloc(A)       { return m_allocator; }
			template <typename A> typename std::enable_if< std::is_pointer<A>::value, AllocType&      >::type alloc(A)       { return *m_allocator; }
			AllocType const& alloc() const { return alloc(m_allocator); }
			AllocType&       alloc()       { return alloc(m_allocator); }

			// Return the block at a given index, automatically grows the map.
			BlockPtr operator[](difference_type block_index)
			{
				// Ensure the map is large enough to contain 'block_index'
				ensure_space(block_index);

				// Return the pointer to the block
				assert(m_offset+block_index >= 0 && m_offset+block_index < m_count && "ensure space didn't work");
				return m_ptrs + m_offset + block_index;
			}

			// Grow the block map to include 'block_index' if necessary
			void ensure_space(difference_type block_index)
			{
				difference_type index = m_offset + block_index;
				difference_type new_count = 
					index < 0        ? m_count - index :
					index >= m_count ? 1 + index : 0;

				// If no more blocks are needed, done
				if (new_count == m_count)
					return;

				// If we have the capacity, extend
				if (m_capacity >= new_count)
				{

				}

				// Grow the map if needed
				if (m_capacity < new_count)
				{
					auto new_capacity = m_capacity * 2;
					for (;new_capacity < new_count; new_capacity *= 2) {}

					// Allocate a new pointer map
					void* mem = alloc().allocate(new_capacity);
					throw std::exception("stub");
				}
				if (index < 0) 
				{
					to_add = -index;
				}
				else if (index >= m_count) // append blocks
				{
					to_add = index - m_count + 1;
				}
			}
		};

		BlockPtrMap m_map;         // Map of pointers to blocks
		size_type   m_count;       // Total number of elements in the container
		size_type   m_first;       // The offset into the first block to the first element

		// Make space for 'to_add' elements in the container at index 'at'.
		void ensure_space(size_type to_add, size_type at)
		{
			assert(at >= 0 && at <= m_count && "'at' should be in the element range [0,m_count]");

			// Get the block index of where new space is needed
			auto block_index = (at + m_first) / CountPerBlock;

			// If adding space to the first block
			if (block_index == 0)
			{
				// Check the number of free elements in the first block
				if (to_add > m_first)
				{
					// Add a block to the front
					(void)m_map;
				}
			}
			else if (block_index == m_map.m_count)
			{
			}
			else
			{
			}

		}

		// Any combination of type, block size, and allocator is a friend
		template <class T, std::size_t B, class A> friend class deque;

	public:

		// construct empty
		deque()
			:m_map()
			,m_count()
			,m_first()
		{}

		// construct with custom allocator
		explicit deque(Allocator const& allocator)
			:m_map(allocator)
			,m_count()
			,m_first()
		{}

		// construct from count * Type()
		explicit deque(size_type count)
			:m_map()
			,m_count()
			,m_first()
		{
			resize(count);
		}

		// construct from count * val
		deque(size_type count, value_type const& val)
			:m_map()
			,m_count()
			,m_first()
		{
			for (; count-- != 0;)
				push_back(val);
		}

		// copy construct (explicit copy constructor needed to prevent auto generated one even tho there's a template one that would work)
		deque(deque const& right)
			:m_map(right.m_allocator)
			,m_count()
			,m_first()
		{
			for (auto& r : right)
				push_back(r);
		}

		// copy construct from any pr::deque type
		template <std::size_t B, class A> deque(deque<Type,B,A> const& right)
			:m_map(right.m_allocator)
			,m_count()
			,m_first()
		{
			for (auto& r : right)
				push_back(r);
		}

		// construct from [first, last), with optional allocator
		template <class iter> deque(iter first, iter last, Allocator const& allocator = Allocator())
			:m_map(allocator)
			,m_count()
			,m_first()
		{
			for (;!(first == last); ++first)
				push_back(*first);
		}

		// construct by moving right
		deque(deque&& right)
			:m_map(right.m_allocator)
			,m_count()
			,m_first()
		{
			throw std::exception("stub");
			//_Assign_rv(std::forward<deque>(right), true_type());
		}

		// construct by moving right with allocator
		deque(deque&& right, Allocator const& allocator)
			:m_map(allocator)
			,m_count()
			,m_first()
		{
			throw std::exception("stub");
			//_Assign_rv(std::forward<deque>(right));
		}

		// test if container is empty
		bool empty() const
		{
			return size() == 0;
		}

		// return the length of the container
		size_type size() const
		{
			return m_count;
		}

		void resize(size_type count)
		{
			(void)count;
			throw std::exception("stub");
		}

		// insert element at end
		void push_back(value_type const& val)
		{
			ensure_space(m_count + 1, m_count);
			throw std::exception("stub");
		}
	};

	#ifdef PR_NOEXCEPT_DEFINED
	#   undef PR_NOEXCEPT_DEFINED
	#   undef PR_NOEXCEPT
	#endif
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/common/refcount.h"
#include "pr/common/refptr.h"
#include "pr/maths/maths.h"

namespace pr
{
	namespace unittests
	{
		namespace deque
		{
			struct Single :pr::RefCount<Single>
			{
				static void RefCountZero(RefCount<Single>*) {}
			} g_single;

			int g_start_object_count, g_object_count = 0;;
			inline void ConstrCall() { ++g_object_count; }
			inline void DestrCall() { --g_object_count; }

			typedef unsigned int uint;
			struct Type
			{
				uint val;
				pr::RefPtr<Single> ptr;
				operator uint() const                             { return val; }

				Type()       :val(0) ,ptr(&g_single)              { ConstrCall(); }
				Type(uint w) :val(w) ,ptr(&g_single)              { ConstrCall(); }
				Type(Type const& rhs) :val(rhs.val) ,ptr(rhs.ptr) { ConstrCall(); }
				~Type()
				{
					DestrCall();
					if (ptr.m_ptr != &g_single)
						throw std::exception("destructing an invalid Type");
					val = 0xcccccccc;
				}
			};

			typedef pr::deque<Type,  8> Deque0;
			typedef pr::deque<Type, 16> Deque1;
			std::vector<Type> ints;
		}

		PRUnitTest(pr_common_deque)
		{
			using namespace pr::unittests::deque;

			//ints.resize(16);
			//for (uint i = 0; i != 16; ++i)
			//	ints[i] = Type(i);

			//g_start_object_count = g_object_count;
			//{
			//	Deque0 deq;
			//	PR_CHECK(deq.empty(), true);
			//	PR_CHECK(deq.size(), 0U);
			//}
			//PR_CHECK(g_object_count, g_start_object_count);
			//g_start_object_count = g_object_count;
			//{
			//	Deque1 deq(15);
			//	PR_CHECK(!deq.empty(), true);
			//	PR_CHECK(deq.size(), 15U);
			//}
			//PR_CHECK(g_object_count, g_start_object_count);
			//g_start_object_count = g_object_count;
			//{
			//	Array0 arr(5U, 3);
			//	PR_CHECK(arr.size(), 5U);
			//	for (size_t i = 0; i != 5; ++i)
			//		PR_CHECK(arr[i], 3U);
			//}
			//PR_CHECK(g_object_count, g_start_object_count);
			//g_start_object_count = g_object_count;
			//{
			//	Array0 arr0(5U,3);
			//	Array1 arr1(arr0);
			//	PR_CHECK(arr1.size(), arr0.size());
			//	for (size_t i = 0; i != arr0.size(); ++i)
			//		PR_CHECK(arr1[i], arr0[i]);
			//}
			//PR_CHECK(g_object_count, g_start_object_count);
			//g_start_object_count = g_object_count;
			//{
			//	std::vector<uint> vec0(4U, 6);
			//	Array0 arr1(vec0);
			//	PR_CHECK(arr1.size(), vec0.size());
			//	for (size_t i = 0; i != vec0.size(); ++i)
			//		PR_CHECK(arr1[i], vec0[i]);
			//}
			//PR_CHECK(g_object_count, g_start_object_count);
			//{//RefCounting0
			//	PR_CHECK(g_single.m_ref_count, 16);
			//}
			//{//Assign
			//	g_start_object_count = g_object_count;
			//	{
			//		Array0 arr0;
			//		arr0.assign(3U, 5);
			//		PR_CHECK(arr0.size(), 3U);
			//		for (size_t i = 0; i != 3; ++i)
			//			PR_CHECK(arr0[i], 5U);

			//		Array1 arr1;
			//		arr1.assign(&ints[0], &ints[8]);
			//		PR_CHECK(arr1.size(), 8U);
			//		for (size_t i = 0; i != 8; ++i)
			//			PR_CHECK(arr1[i], ints[i]);
			//	}
			//	PR_CHECK(g_object_count, g_start_object_count);
			//}
			//{//RefCounting1
			//	PR_CHECK(g_single.m_ref_count, 16);
			//}
			//{//Clear
			//	g_start_object_count = g_object_count;
			//	{
			//		Array0 arr0(ints.begin(), ints.end());
			//		arr0.clear();
			//		PR_CHECK(arr0.empty(), true);
			//	}
			//	PR_CHECK(g_object_count, g_start_object_count);
			//}
			//{//RefCounting2
			//	PR_CHECK(g_single.m_ref_count, 16);
			//}
			//{//Erase
			//	g_start_object_count = g_object_count;
			//	{
			//		Array0 arr0(ints.begin(), ints.begin() + 8);
			//		Array0::const_iterator b = arr0.begin();
			//		arr0.erase(b + 3, b + 5);
			//		PR_CHECK(arr0.size(), 6U);
			//		for (size_t i = 0; i != 3; ++i) PR_CHECK(arr0[i], ints[i]  );
			//		for (size_t i = 3; i != 6; ++i) PR_CHECK(arr0[i], ints[i+2]);
			//	}
			//	PR_CHECK(g_object_count,g_start_object_count);
			//	g_start_object_count = g_object_count;
			//	{
			//		Array1 arr1(ints.begin(), ints.begin() + 4);
			//		arr1.erase(arr1.begin() + 2);
			//		PR_CHECK(arr1.size(), 3U);
			//		for (size_t i = 0; i != 2; ++i) PR_CHECK(arr1[i], ints[i]  );
			//		for (size_t i = 2; i != 3; ++i) PR_CHECK(arr1[i], ints[i+1]);
			//	}
			//	PR_CHECK(g_object_count, g_start_object_count);
			//	g_start_object_count = g_object_count;
			//	{
			//		Array0 arr2(ints.begin(), ints.begin() + 5);
			//		arr2.erase_fast(arr2.begin() + 2);
			//		PR_CHECK(arr2.size(), 4U);
			//		for (size_t i = 0; i != 2; ++i) PR_CHECK(arr2[i], ints[i]);
			//		PR_CHECK(arr2[2], ints[4]);
			//		for (size_t i = 3; i != 4; ++i) PR_CHECK(arr2[i], ints[i]);
			//	}
			//	PR_CHECK(g_object_count, g_start_object_count);
			//}
			//{//RefCounting3
			//	PR_CHECK(g_single.m_ref_count, 16);
			//}
			//{//Insert
			//	g_start_object_count = g_object_count;
			//	{
			//		Array0 arr0;
			//		arr0.insert(arr0.end(), 4U, 9);
			//		PR_CHECK(arr0.size(), 4U);
			//		for (size_t i = 0; i != 4; ++i)
			//			PR_CHECK(arr0[i], 9U);
			//	}
			//	PR_CHECK(g_object_count, g_start_object_count);
			//	g_start_object_count = g_object_count;
			//	{
			//		Array1 arr1(4U, 6);
			//		arr1.insert(arr1.begin() + 2, &ints[2], &ints[7]);
			//		PR_CHECK(arr1.size(), 9U);
			//		for (size_t i = 0; i != 2; ++i) PR_CHECK(arr1[i], 6U);
			//		for (size_t i = 2; i != 7; ++i) PR_CHECK(arr1[i], ints[i]);
			//		for (size_t i = 7; i != 9; ++i) PR_CHECK(arr1[i], 6U);
			//	}
			//	PR_CHECK(g_object_count, g_start_object_count);
			//}
			//{//RefCounting4
			//	PR_CHECK(g_single.m_ref_count, 16);
			//}
			//{//PushPop
			//	g_start_object_count = g_object_count;
			//	{
			//		Array0 arr;
			//		arr.insert(arr.begin(), &ints[0], &ints[4]);
			//		arr.pop_back();
			//		PR_CHECK(arr.size(), 3U);
			//		for (size_t i = 0; i != 3; ++i)
			//			PR_CHECK(arr[i], ints[i]);
			//	}
			//	PR_CHECK(g_object_count, g_start_object_count);
			//	g_start_object_count = g_object_count;
			//	{
			//		Array1 arr;
			//		arr.reserve(4);
			//		for (int i = 0; i != 4; ++i) arr.push_back_fast(i);
			//		for (int i = 4; i != 9; ++i) arr.push_back(i);
			//		for (size_t i = 0; i != 9; ++i)
			//			PR_CHECK(arr[i], ints[i]);
			//	}
			//	PR_CHECK(g_object_count, g_start_object_count);
			//	g_start_object_count = g_object_count;
			//	{
			//		Array1 arr;
			//		arr.insert(arr.begin(), &ints[0], &ints[4]);
			//		arr.resize(3);
			//		PR_CHECK(arr.size(), 3U);
			//		for (size_t i = 0; i != 3; ++i)
			//			PR_CHECK(arr[i], ints[i]);
			//		arr.resize(6);
			//		PR_CHECK(arr.size(), 6U);
			//		for (size_t i = 0; i != 3; ++i)
			//			PR_CHECK(arr[i], ints[i]);
			//		for (size_t i = 3; i != 6; ++i)
			//			PR_CHECK(arr[i], 0U);
			//	}
			//	PR_CHECK(g_object_count, g_start_object_count);
			//}
			//{//RefCounting5
			//	PR_CHECK(g_single.m_ref_count, 16);
			//}
			//{//Operators
			//	g_start_object_count = g_object_count;
			//	{
			//		Array0 arr0(4U, 1);
			//		Array0 arr1(3U, 2);
			//		arr1 = arr0;
			//		PR_CHECK(arr0.size(), 4U);
			//		PR_CHECK(arr1.size(), 4U);
			//		for (size_t i = 0; i != 4; ++i)
			//			PR_CHECK(arr1[i], arr0[i]);
			//	}
			//	PR_CHECK(g_object_count, g_start_object_count);
			//	g_start_object_count = g_object_count;
			//	{
			//		Array0 arr0(4U, 1);
			//		Array1 arr2;
			//		arr2 = arr0;
			//		PR_CHECK(arr0.size(), 4U);
			//		PR_CHECK(arr2.size(), 4U);
			//		for (size_t i = 0; i != 4; ++i)
			//			PR_CHECK(arr2[i], arr0[i]);

			//		struct L
			//		{
			//			static std::vector<Type> Conv(std::vector<Type> v) { return v; }
			//		};
			//		std::vector<Type> vec0 = L::Conv(arr0);
			//		PR_CHECK(vec0.size(), 4U);
			//		for (size_t i = 0; i != 4; ++i)
			//			PR_CHECK(vec0[i], arr0[i]);
			//	}
			//	PR_CHECK(g_object_count, g_start_object_count);
			//}
			//{//RefCounting6
			//	PR_CHECK(g_single.m_ref_count, 16);
			//}
			//{//Mem
			//	g_start_object_count = g_object_count;
			//	{
			//		Array0 arr0;
			//		arr0.reserve(100);
			//		for (uint i = 0; i != 50; ++i) arr0.push_back(i);
			//		PR_CHECK(arr0.capacity(), 100U);
			//		arr0.shrink_to_fit();
			//		PR_CHECK(arr0.capacity(), 50U);
			//		arr0.resize(1);
			//		arr0.shrink_to_fit();
			//		PR_CHECK(arr0.capacity(), (size_t)arr0.LocalLength);
			//	}
			//	PR_CHECK(g_object_count, g_start_object_count);
			//}
			//{//RefCounting
			//	ints.clear();
			//	PR_CHECK(g_single.m_ref_count, 0);
			//}
			//{//AlignedTypes
			//	g_start_object_count = g_object_count;
			//	{
			//		pr::Spline spline = pr::Spline::make(pr::Random3N(1.0f), pr::Random3N(1.0f), pr::Random3N(1.0f), pr::Random3N(1.0f));

			//		pr::vector<pr::v4> arr0;
			//		pr::Raster(spline, arr0, 100);

			//		PR_CHECK(arr0.capacity() > arr0.LocalLength, true);
			//		arr0.resize(5);
			//		arr0.shrink_to_fit();
			//		PR_CHECK(arr0.size(), 5U);
			//		PR_CHECK(arr0.capacity(), size_t(arr0.LocalLength));
			//	}
			//	PR_CHECK(g_object_count, g_start_object_count);
			//}
			//{//GlobalConstrDestrCount
			//	PR_CHECK(g_object_count, 0);
			//}
		}
	}
}
#endif
