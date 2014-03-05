//******************************************
// pr::Array<>
//  Copyright © Rylogic Ltd 2003
//******************************************
// A version of std::vector with configurable local caching
// Note: this file is made to match pr::string<> as much as possible
#pragma once
#ifndef PR_ARRAY_H
#define PR_ARRAY_H

// <type_traits> was introduced in sp1
#if defined(_MSC_FULL_VER) && _MSC_FULL_VER < 150030729
#error VS2008 SP1 or greater is required to build this file
#endif

#pragma intrinsic(memcmp, memcpy, memset, strcmp)
#include <memory>
#include <iterator>
#include <stdexcept>
#include <algorithm>
#include <type_traits>
#include "pr/meta/if.h"  // these will be removed when pre-C++11 is a thing of the past
#include "pr/meta/ispod.h"
#include "pr/meta/ispointer.h"
#include "pr/meta/enableif.h"
#include "pr/meta/alignmentof.h"
#include "pr/meta/alignedstorage.h"
#include "pr/meta/remove_pointer.h"

#ifndef PR_ASSERT
#   define PR_ASSERT_DEFINED
#   define PR_ASSERT(grp, exp, str)
#endif
#ifndef PR_NOEXCEPT
#   define PR_NOEXCEPT_DEFINED
#   if _MSC_VER >= 1900
#      define PR_NOEXCEPT noexcept
#   else
#      define PR_NOEXCEPT throw()
#   endif
#endif

namespace pr
{
	namespace impl
	{
		namespace arr
		{
			template <typename Type> struct citer // const iterator
			{
				typedef std::ptrdiff_t                  difference_type;
				typedef Type                            value_type;
				typedef Type const&                     reference;
				typedef Type const*                     pointer;
				typedef std::random_access_iterator_tag iterator_category;
				pointer m_ptr;
				
				citer()                                 {}
				citer(pointer ptr) : m_ptr(ptr)         {}
				pointer   operator ->() const           { return  m_ptr; }
				reference operator * () const           { return *m_ptr; }
				citer&    operator ++()                 { ++m_ptr; return *this; }
				citer&    operator --()                 { --m_ptr; return *this; }
				citer     operator ++(int)              { citer i(m_ptr); ++m_ptr; return i; }
				citer     operator --(int)              { citer i(m_ptr); --m_ptr; return i; }
			};
			template <typename Type> struct miter // mutable iterator
			{
				typedef std::ptrdiff_t                  difference_type;
				typedef Type                            value_type;
				typedef Type&                           reference;
				typedef Type*                           pointer;
				typedef std::random_access_iterator_tag iterator_category;
				pointer m_ptr;
				
				miter()                                 {}
				miter(pointer ptr) : m_ptr(ptr)         {}
				pointer   operator ->() const           { return  m_ptr; }
				reference operator * () const           { return *m_ptr; }
				miter&    operator ++()                 { ++m_ptr; return *this; }
				miter&    operator --()                 { --m_ptr; return *this; }
				miter     operator ++(int)              { miter i(m_ptr); ++m_ptr; return i; }
				miter     operator --(int)              { miter i(m_ptr); --m_ptr; return i; }
				operator citer<Type>() const            { return citer<Type>(m_ptr); }
			};
			
			template <typename Type> inline bool operator == (citer<Type> lhs, citer<Type> rhs) { return lhs.m_ptr == rhs.m_ptr; }
			template <typename Type> inline bool operator == (miter<Type> lhs, miter<Type> rhs) { return lhs.m_ptr == rhs.m_ptr; }
			template <typename Type> inline bool operator == (citer<Type> lhs, miter<Type> rhs) { return lhs.m_ptr == rhs.m_ptr; }
			template <typename Type> inline bool operator == (miter<Type> lhs, citer<Type> rhs) { return lhs.m_ptr == rhs.m_ptr; }
			template <typename Type> inline bool operator != (citer<Type> lhs, citer<Type> rhs) { return lhs.m_ptr != rhs.m_ptr; }
			template <typename Type> inline bool operator != (miter<Type> lhs, miter<Type> rhs) { return lhs.m_ptr != rhs.m_ptr; }
			template <typename Type> inline bool operator != (citer<Type> lhs, miter<Type> rhs) { return lhs.m_ptr != rhs.m_ptr; }
			template <typename Type> inline bool operator != (miter<Type> lhs, citer<Type> rhs) { return lhs.m_ptr != rhs.m_ptr; }
			template <typename Type> inline bool operator <  (citer<Type> lhs, citer<Type> rhs) { return lhs.m_ptr <  rhs.m_ptr; }
			template <typename Type> inline bool operator <  (miter<Type> lhs, miter<Type> rhs) { return lhs.m_ptr <  rhs.m_ptr; }
			template <typename Type> inline bool operator <  (citer<Type> lhs, miter<Type> rhs) { return lhs.m_ptr <  rhs.m_ptr; }
			template <typename Type> inline bool operator <  (miter<Type> lhs, citer<Type> rhs) { return lhs.m_ptr <  rhs.m_ptr; }
			template <typename Type> inline bool operator <= (citer<Type> lhs, citer<Type> rhs) { return lhs.m_ptr <= rhs.m_ptr; }
			template <typename Type> inline bool operator <= (miter<Type> lhs, miter<Type> rhs) { return lhs.m_ptr <= rhs.m_ptr; }
			template <typename Type> inline bool operator <= (citer<Type> lhs, miter<Type> rhs) { return lhs.m_ptr <= rhs.m_ptr; }
			template <typename Type> inline bool operator <= (miter<Type> lhs, citer<Type> rhs) { return lhs.m_ptr <= rhs.m_ptr; }
			template <typename Type> inline bool operator >  (citer<Type> lhs, citer<Type> rhs) { return lhs.m_ptr >  rhs.m_ptr; }
			template <typename Type> inline bool operator >  (miter<Type> lhs, miter<Type> rhs) { return lhs.m_ptr >  rhs.m_ptr; }
			template <typename Type> inline bool operator >  (citer<Type> lhs, miter<Type> rhs) { return lhs.m_ptr >  rhs.m_ptr; }
			template <typename Type> inline bool operator >  (miter<Type> lhs, citer<Type> rhs) { return lhs.m_ptr >  rhs.m_ptr; }
			template <typename Type> inline bool operator >= (citer<Type> lhs, citer<Type> rhs) { return lhs.m_ptr >= rhs.m_ptr; }
			template <typename Type> inline bool operator >= (miter<Type> lhs, miter<Type> rhs) { return lhs.m_ptr >= rhs.m_ptr; }
			template <typename Type> inline bool operator >= (citer<Type> lhs, miter<Type> rhs) { return lhs.m_ptr >= rhs.m_ptr; }
			template <typename Type> inline bool operator >= (miter<Type> lhs, citer<Type> rhs) { return lhs.m_ptr >= rhs.m_ptr; }
			
			template <typename Type> inline citer<Type>& operator += (citer<Type>& lhs, typename citer<Type>::difference_type rhs) { lhs.m_ptr += rhs; return lhs; }
			template <typename Type> inline miter<Type>& operator += (miter<Type>& lhs, typename miter<Type>::difference_type rhs) { lhs.m_ptr += rhs; return lhs; }
			template <typename Type> inline citer<Type>& operator -= (citer<Type>& lhs, typename citer<Type>::difference_type rhs) { lhs.m_ptr -= rhs; return lhs; }
			template <typename Type> inline miter<Type>& operator -= (miter<Type>& lhs, typename miter<Type>::difference_type rhs) { lhs.m_ptr -= rhs; return lhs; }
			
			template <typename Type> inline citer<Type> operator +  (citer<Type>  lhs, typename citer<Type>::difference_type rhs) { return lhs.m_ptr + rhs; }
			template <typename Type> inline miter<Type> operator +  (miter<Type>  lhs, typename miter<Type>::difference_type rhs) { return lhs.m_ptr + rhs; }
			template <typename Type> inline citer<Type> operator -  (citer<Type>  lhs, typename citer<Type>::difference_type rhs) { return lhs.m_ptr - rhs; }
			template <typename Type> inline miter<Type> operator -  (miter<Type>  lhs, typename miter<Type>::difference_type rhs) { return lhs.m_ptr - rhs; }
			
			template <typename Type> inline typename citer<Type>::difference_type operator - (citer<Type> lhs, citer<Type> rhs) { return lhs.m_ptr - rhs.m_ptr; }
			template <typename Type> inline typename miter<Type>::difference_type operator - (miter<Type> lhs, miter<Type> rhs) { return lhs.m_ptr - rhs.m_ptr; }
			template <typename Type> inline typename citer<Type>::difference_type operator - (citer<Type> lhs, miter<Type> rhs) { return lhs.m_ptr - rhs.m_ptr; }
			template <typename Type> inline typename citer<Type>::difference_type operator - (miter<Type> lhs, citer<Type> rhs) { return lhs.m_ptr - rhs.m_ptr; }
		}
	}
	
	// Not intended to be a complete replacement, just a 90% substitute
	// Allocator = the type to do the allocation/deallocation. *Can be a pointer to an std::allocator like object*
	template <typename Type, int LocalCount=16, bool Fixed=false, typename Allocator=std::allocator<Type> >
	class Array
	{
	public:
		typedef Array<Type, LocalCount, Fixed, Allocator> type;
		typedef impl::arr::citer<Type> const_iterator;    // A type that provides a random-access iterator that can read a const element in a array.
		typedef impl::arr::miter<Type> iterator;          // A type that provides a random-access iterator that can read or modify any element in a array.
		typedef Type const*            const_pointer;     // A type that provides a pointer to a const element in a array.
		typedef Type*                  pointer;           // A type that provides a pointer to an element in a array.
		typedef Type const&            const_reference;   // A type that provides a reference to a const element stored in a array for reading and performing const operations.
		typedef Type&                  reference;         // A type that provides a reference to an element stored in a array.
		typedef std::size_t            size_type;         // A type that counts the number of elements in a array.
		typedef std::ptrdiff_t         difference_type;   // A type that provides the difference between the addresses of two elements in a array.
		typedef Type                   value_type;        // A type that represents the data type stored in a array.
		typedef Allocator              allocator_type;    // A type that represents the allocator class for the array object.
		typedef typename pr::meta::remove_pointer<Allocator>::type AllocType; // The type of the allocator ignoring pointers
		
		enum
		{
			LocalLength      = LocalCount,
			LocalSizeInBytes = LocalCount * sizeof(value_type),
			TypeIsPod        = pr::meta::is_pod<Type>::value,
			TypeAlignment    = pr::meta::alignment_of<Type>::value,
		};
		
		// type specific traits
		template <bool pod> struct base_traits;
		template <> struct base_traits<false>
		{
			static void construct    (Allocator& alloc, Type* first, size_type count)                  { for (; count--;) { alloc.construct(first++, Type()); } }
			static void destruct     (Allocator& alloc, Type* first, size_type count)                  { for (; count--;) { alloc.destroy(first++); } }
			static void copy_constr  (Allocator& alloc, Type* first, Type const* src, size_type count) { for (; count--;) { alloc.construct(first++, *src++); } }
			static void copy_assign  (Type* first, Type const* src, size_type count)                   { for (; count--;) { *first++ = *src++; } }
			static void move_left    (Type* first, Type const* src, size_type count)                   { PR_ASSERT(PR_DBG, first < src, ""); for (; count--;) { *first++ = *src++; } }
			static void move_right   (Type* first, Type const* src, size_type count)                   { PR_ASSERT(PR_DBG, first > src, ""); for (first+=count, src+=count; count--;) { *--first = *--src; } }
		};
		template <> struct base_traits<true>
		{
			static void construct    (Allocator&, Type*, size_type)                              {}
			static void destruct     (Allocator&, Type*, size_type)                              {}
			static void copy_constr  (Allocator&, Type* first, Type const* src, size_type count) { ::memcpy(first, src, sizeof(Type) * count); }
			static void copy_assign  (Type* first, Type const* src, size_type count)             { ::memcpy(first, src, sizeof(Type) * count); }
			static void move_left    (Type* first, Type const* src, size_type count)             { ::memmove(first, src, sizeof(Type) * count); }
			static void move_right   (Type* first, Type const* src, size_type count)             { ::memmove(first, src, sizeof(Type) * count); }
		};
		struct traits :public base_traits<TypeIsPod>
		{
			static void fill_constr  (Allocator& alloc, Type* first, size_type count, Type const& val) { for (; count--;) { alloc.construct(first++, val); } }
			static void fill_assign  (                  Type* first, size_type count, Type const& val) { for (; count--;) { *first++ = val; } }
		};
		
	private:
		typedef typename pr::meta::aligned_storage<sizeof(Type), pr::meta::alignment_of<Type>::value>::type TLocalStore;
		TLocalStore m_local[LocalLength]; // Local cache for small arrays
		Type*       m_ptr;                // Pointer to the array of data
		size_type   m_capacity;           // The reserved space for elements. m_capacity * sizeof(Type) = size in bytes pointed to by m_ptr.
		size_type   m_count;              // The number of used elements in the array
		Allocator   m_allocator;          // The memory allocator
		
		// Any combination of type, local count, fixed, and allocator is a friend
		template <class T, int L, bool F, class A> friend class Array;
		
		// Access to the allocator object (independant over whether its a pointer or instance)
		// (enable_if requires type inference to work, hence the 'A' template parameter)
		template <typename A> typename pr::meta::enable_if<!pr::meta::is_pointer<A>::value, AllocType const&>::type alloc(A) const { return m_allocator; }
		template <typename A> typename pr::meta::enable_if< pr::meta::is_pointer<A>::value, AllocType const&>::type alloc(A) const { return *m_allocator; }
		template <typename A> typename pr::meta::enable_if<!pr::meta::is_pointer<A>::value, AllocType&      >::type alloc(A)       { return m_allocator; }
		template <typename A> typename pr::meta::enable_if< pr::meta::is_pointer<A>::value, AllocType&      >::type alloc(A)       { return *m_allocator; }
		
		// return true if 'ptr' points with the current container
		bool inside(const_pointer ptr) const { return m_ptr <= ptr && ptr < m_ptr + m_count; }
		
		// return a pointer to the local buffer
		Type const* local_ptr() const { return reinterpret_cast<Type const*>(&m_local[0]); }
		Type*       local_ptr()       { return reinterpret_cast<Type*>      (&m_local[0]); }
		
		// return true if 'm_ptr' points to our local buffer
		bool local() const { return m_ptr == local_ptr(); }
		
		// return true if 'arr' is actually this object
		template <typename tarr> bool isthis(tarr const& arr) const { return static_cast<void const*>(this) == static_cast<void const*>(&arr); }
		
		// reverse a range of elements
		void reverse(Type *first, Type* last) { for (; first != last && first != --last; ++first) std::swap(*first, *last); }
		
		// return the iterator category for 'iter'
		template <class iter> typename std::iterator_traits<iter>::iterator_category iter_cat(iter const&) const { typename std::iterator_traits<iter>::iterator_category cat; return cat; }
		
		// Make sure 'm_ptr' is big enough to hold 'new_count' elements
		void ensure_space(size_type new_count, bool autogrow)
		{
			struct ConstExpr { static bool Sink(bool b) {return b;} };
			if (!ConstExpr::Sink(Fixed))
			{
				PR_ASSERT(PR_DBG, m_capacity >= LocalLength, "");
				if (new_count <= m_capacity) return;
				
				// Allocate 50% more space
				size_type bigger, new_cap = new_count;
				if (autogrow && new_cap < (bigger = m_count*3/2)) { new_cap = bigger; }
				PR_ASSERT(PR_DBG, autogrow || new_count >= m_count, "don't use ensure_space to trim the allocated memory");
				Type* new_array = alloc().allocate(new_cap);
				
				// Copy elements from the old array to the new array
				traits::copy_constr(alloc(), new_array, m_ptr, m_count);
				
				// Destruct the old array
				traits::destruct(alloc(), m_ptr, m_count);
				if (!local()) alloc().deallocate(m_ptr, m_capacity);
				
				m_ptr = new_array;
				m_capacity = new_cap;
				PR_ASSERT(PR_DBG, m_capacity >= LocalLength, "");
			}
			else
			{
				PR_ASSERT(PR_DBG, new_count <= m_capacity, "non-allocating container capacity exceeded");
				if (new_count > m_capacity)
					throw std::overflow_error("pr::Array<> out of memory");
			}
		}
		
	public:
		
		// The memory allocator
		AllocType const& alloc() const     { return alloc(m_allocator); }
		AllocType&       alloc()           { return alloc(m_allocator); }
		Allocator const& allocator() const { return m_allocator; }
		Allocator&       allocator()       { return m_allocator; }
		
		// construct empty collection
		Array()
		:m_ptr(local_ptr())
		,m_capacity(LocalLength)
		,m_count(0)
		,m_allocator()
		{
		}
		
		// construct with custom allocator
		explicit Array(Allocator const& allocator)
		:m_ptr(local_ptr())
		,m_capacity(LocalLength)
		,m_count(0)
		,m_allocator(allocator)
		{
		}
		
		// construct from count * Type()
		explicit Array(size_type count)
		:m_ptr(local_ptr())
		,m_capacity(LocalLength)
		,m_count(0)
		,m_allocator()
		{
			ensure_space(count, false);
			traits::fill_constr(alloc(), m_ptr, count, Type());
			m_count = count;
		}
		
		// construct from count * value, with allocator
		Array(size_type count, Type const& value, Allocator const& allocator = Allocator())
		:m_ptr(local_ptr())
		,m_capacity(LocalLength)
		,m_count(0)
		,m_allocator(allocator)
		{
			ensure_space(count, false);
			traits::fill_constr(alloc(), m_ptr, count, value);
			m_count = count;
		}
		
		// copy construct (explicit copy constructor needed to prevent auto generated one even tho there's a template one that would work)
		Array(Array const& right)
		:m_ptr(local_ptr())
		,m_capacity(LocalLength)
		,m_count(0)
		,m_allocator(right.m_allocator)
		{
			_Assign(right);
		}
		
		// copy construct from any pr::Array type
		template <int L, bool F, class A> Array(Array<Type,L,F,A> const& right)
		:m_ptr(local_ptr())
		,m_capacity(LocalLength)
		,m_count(0)
		,m_allocator(right.m_allocator)
		{
			_Assign(right);
		}
		
		// construct from [first, last), with allocator
		template <class iter> Array(iter first, iter last, Allocator const& allocator = Allocator())
		:m_ptr(local_ptr())
		,m_capacity(LocalLength)
		,m_count(0)
		,m_allocator(allocator)
		{
			insert(end(), first, last);
		}
		
		// construct from another array-like object
		// 2nd parameter used to prevent overload issues with Array(count)
		template <class tarr> Array(tarr const& right, typename tarr::size_type = 0, Allocator const& allocator = Allocator())
		:m_ptr(local_ptr())
		,m_capacity(LocalLength)
		,m_count(0)
		,m_allocator(allocator)
		{
			#if _MSC_VER >= 1600
			insert(end(), std::begin(right), std::end(right));
			#else
			insert(end(), right.begin(), right.end());
			#endif
		}
		
		// move construct
		#if _MSC_VER >= 1600
		Array(Array&& right) PR_NOEXCEPT
		:m_ptr(local_ptr())
		,m_capacity(LocalLength)
		,m_count(0)
		,m_allocator(right.m_allocator)
		{
			_Assign(std::forward<Array>(right));
		}
		template <int L, bool F, class A> Array(Array<Type,L,F,A>&& right) PR_NOEXCEPT
		:m_ptr(local_ptr())
		,m_capacity(LocalLength)
		,m_count(0)
		,m_allocator(right.m_allocator)
		{
			_Assign(std::forward< Array<Type,L,F,A> >(right));
		}
		#endif
		
		// destruct
		~Array()
		{
			clear();
		}
		
		// iterators
		const_iterator begin() const
		{
			return const_iterator(m_ptr);
		}
		const_iterator end() const
		{
			return const_iterator(m_ptr + m_count);
		}
		iterator begin()
		{
			return iterator(m_ptr);
		}
		iterator end()
		{
			return iterator(m_ptr + m_count);
		}
		
		// front/back accessors
		const_reference front() const
		{
			PR_ASSERT(PR_DBG, !empty(), "container empty");
			return m_ptr[0];
		}
		const_reference back() const
		{
			PR_ASSERT(PR_DBG, !empty(), "container empty");
			return m_ptr[m_count - 1];
		}
		reference front()
		{
			PR_ASSERT(PR_DBG, !empty(), "container empty");
			return m_ptr[0];
		}
		reference back()
		{
			PR_ASSERT(PR_DBG, !empty(), "container empty");
			return m_ptr[m_count - 1];
		}
		
		// insert element at end
		void push_back(const_reference value)
		{
			Type const& val = inside(&value) ? Type(value) : value;
			ensure_space(m_count + 1, true);
			push_back_fast(val);
		}
		
		// Add an element to the end of the array without "ensure_space" first
		void push_back_fast(const_reference value)
		{
			PR_ASSERT(PR_DBG, (m_count + 1) <= m_capacity, "Container overflow");
			PR_ASSERT(PR_DBG, !inside(&value), "Cannot push_back_fast an item from this container");
			traits::fill_constr(alloc(), m_ptr + m_count, 1, value);
			++m_count;
		}
		
		// Deletes the element at the end of the array.
		void pop_back()
		{
			PR_ASSERT(PR_DBG, !empty(), "");
			traits::destruct(alloc(), m_ptr + m_count - 1, 1);
			--m_count;
		}
		
		// return pointer to the first element or null if the container is empty
		const_pointer data() const
		{
			return m_count ? m_ptr : 0;
		}
		pointer data()
		{
			return m_count ? m_ptr : 0;
		}
		
		// test if sequence is empty
		bool empty() const
		{
			return size() == 0;
		}
		
		// return length of sequence
		size_type size() const
		{
			return m_count;
		}
		
		// return the available length within allocation
		size_type capacity() const
		{
			return m_capacity;
		}
		
		// return maximum possible length of sequence
		size_type max_size() const
		{
			return 0xFFFFFFFF;
		}
		
		// indexed access
		const_reference at(size_type pos) const
		{
			PR_ASSERT(PR_DBG, pos < size(), "out of range");
			return m_ptr[pos];
		}
		reference at(size_type pos)
		{
			PR_ASSERT(PR_DBG, pos < size(), "out of range");
			return m_ptr[pos];
		}
		
		// resize the collection to 0 and free memory
		void clear()
		{
			traits::destruct(alloc(), m_ptr, m_count);
			if (!local()) alloc().deallocate(m_ptr, m_capacity);
			m_ptr      = local_ptr();
			m_capacity = LocalLength;
			m_count    = 0;
		}
		
		// determine new minimum length of allocated storage
		void reserve(size_type new_cap = 0)
		{
			PR_ASSERT(PR_DBG, new_cap >= size(), "reserve amount less than current size");
			ensure_space(new_cap, false);
		}
		
		// determine new length, padding with default elements as needed
		void resize(size_type newsize)
		{
			if (m_count < newsize)
			{
				ensure_space(newsize, false);
				traits::construct(alloc(), m_ptr + m_count, newsize - m_count);
			}
			else if (m_count > newsize)
			{
				traits::destruct (alloc(), m_ptr + newsize, m_count - newsize);
			}
			m_count = newsize;
		}
		void resize(size_type newsize, Type const& value)
		{
			if (m_count < newsize)
			{
				Type const& val = inside(&value) ? Type(value) : value;
				ensure_space(newsize, false);
				traits::fill_constr(alloc(), m_ptr + m_count, newsize - m_count, val);
			}
			else if (m_count > newsize)
			{
				traits::destruct(alloc(), m_ptr + newsize, m_count - newsize);
			}
			m_count = newsize;
		}
		
		// Index operator
		const_reference operator [](size_type i) const
		{
			PR_ASSERT(PR_DBG, i < size(), "out of range");
			return m_ptr[i];
		}
		reference operator [](size_type i)
		{
			PR_ASSERT(PR_DBG, i < size(), "out of range");
			return m_ptr[i];
		}
		
		// assign right (explicit assignment operator needed to prevent auto generated one even tho there's a template one that would work)
		Array& operator = (Array const& right)
		{
			_Assign(right);
			return *this;
		}
		
		// assign right from any pr::Array<Type,...>
		template <int L,bool F,typename A> Array& operator = (Array<Type,L,F,A> const& right)
		{
			_Assign(right);
			return *this;
		}
		
		// assign right
		template <int Len> Array& operator = (Type const (&right)[Len])
		{
			insert(end(), &right[0], &right[0] + Len);
			return *this;
		}
		
		// assign right
		template <typename tarr> Array& operator = (tarr const& right)
		{
			#if _MSC_VER >= 1600
			insert(end(), std::begin(right), std::end(right));
			#else
			insert(end(), right.begin(), right.end());
			#endif
			return *this;
		}
		
		// move right
		#if _MSC_VER >= 1600
		// explicit move right
		Array& operator = (Array&& right) PR_NOEXCEPT
		{
			_Assign(std::forward<Array>(right));
			return *this;
		}
		
		// move right from any pr::Array<>
		template <int L,bool F,typename A> Array& operator = (Array<Type,L,F,A>&& right) PR_NOEXCEPT
		{
			_Assign(std::forward< Array<Type,L,F,A> >(right));
			return *this;
		}
		#endif
		
		// assign count * value
		void assign(size_type count, Type const& value)
		{
			_Assign(count, value);
		}
		
		// assign [first, last), iterators
		template <typename iter> void assign(iter first, iter last)
		{
			erase(begin(), end());
			insert(end(), first, last);
		}
		
		// insert value at pos
		iterator insert(const_iterator pos, Type const& value)
		{
			difference_type ofs = pos - begin();
			_Insert(pos, 1, value);
			return begin() + ofs;
		}
		
		// insert count * value at pos
		void insert(const_iterator pos, size_type count, Type const& value)
		{
			_Insert(pos, count, value);
		}
		
		// insert [first, last) at pos
		template <typename iter> void insert(const_iterator pos, iter first, iter last)
		{
			_Insert(pos, first, last);
		}
		
		// erase element at pos
		iterator erase(const_iterator pos)
		{
			return erase(pos, pos + 1);
		}
		
		// erase [first, last)
		iterator erase(const_iterator first, const_iterator last)
		{
			PR_ASSERT(PR_DBG, first <= last, "last must follow first");
			PR_ASSERT(PR_DBG, begin() <= first && last <= end(), "iterator range must be within the array");
			size_type n = last - first, ofs = first - begin();
			if (n)
			{
				Type*     del = m_ptr + ofs;
				size_type rem = m_count - (ofs + n);
				traits::move_left(del, del + n, rem);
				traits::destruct(alloc(), m_ptr + m_count - n, n);
				m_count -= n;
			}
			return begin() + ofs;
		}
		
		// erase element at pos without preserving order
		iterator erase_fast(const_iterator pos)
		{
			PR_ASSERT(PR_DBG, begin() <= pos && pos < end(), "pos must be within the array");
			size_type ofs = pos - begin();
			if (end() - pos > 1) { *(begin() + ofs) = back(); }
			pop_back();
			return begin() + ofs;
		}
		
		// erase [first, last) without preserving order
		iterator erase_fast(const_iterator first, const_iterator last)
		{
			PR_ASSERT(PR_DBG, first <= last, "last must follow first");
			PR_ASSERT(PR_DBG, begin() <= first && last <= end(), "iterator range must be within the array");
			size_type n = last - first, ofs = first - begin();
			if (n)
			{
				Type*     del = m_ptr + ofs;
				size_type rem = m_count - (ofs + n);
				if (rem < n)
				{
					traits::move_left(del, del + n, rem);
					traits::destruct(alloc(), m_ptr + m_count - n, n); 
				}
				else
				{
					traits::copy_assign(del, m_ptr + m_count - n, n);
					traits::destruct(alloc(), m_ptr + m_count - n, n); 
				}
				m_count -= n;
			}
			return begin() + ofs;
		}
		
		// Requests the removal of unused capacity
		void shrink_to_fit()
		{
			PR_ASSERT(PR_DBG, m_capacity >= LocalLength, "");
			if (m_capacity != LocalLength)
			{
				PR_ASSERT(PR_DBG, !local(), "");
				
				Type* new_array; size_type new_count;
				if (m_count <= LocalLength)
				{
					new_count = LocalLength;
					new_array = local_ptr();
				}
				else
				{
					new_count = m_count;
					new_array = alloc().allocate(new_count);
				}
				
				// Copy elements from the old array to the new array
				traits::copy_constr(alloc(), new_array, m_ptr, m_count);
				
				// Destruct the old array
				traits::destruct(alloc(), m_ptr, m_count);
				if (!local()) alloc().deallocate(m_ptr, m_count);
				
				m_ptr = new_array;
				m_capacity = new_count;
			}
		}
		
		// Implementation ****************************************************
		
		// Assign count * value
		void _Assign(size_type count, Type const& value)
		{
			Type const& val = inside(&value) ? Type(value) : value;
			if (count == 0) // new sequence empty, erase existing sequence
			{
				clear();
			}
			else if (count <= size()) // enough elements, copy new and destroy old
			{
				traits::fill_assign(m_ptr, count, val); // copy new
				traits::destruct(alloc(), m_ptr + count, size() - count); // destroy old
			}
			else if (count <= capacity()) // enough rom, copy and construct new
			{
				traits::fill_assign(m_ptr, size(), val);
				traits::fill_constr(alloc(), m_ptr + size(), count - size(), val);
			}
			else // not enonugh room, allocate new array and construct new
			{
				resize(0);
				ensure_space(count, false);
				traits::fill_constr(alloc(), m_ptr, count, val);
			}
			m_count = count;
		}
		
		// Assign right of any pr::Array<>
		template <int L,bool F,typename A> void _Assign(Array<Type,L,F,A> const& right)
		{
			if (!isthis(right)) // not this object
			{
				if (right.size() == 0) // new sequence empty, erase existing sequence
				{
					clear();
				}
				else if (right.size() <= size()) // enough elements, copy new and destroy old
				{
					traits::copy_assign(m_ptr, right.m_ptr, right.size()); // copy new
					traits::destruct(alloc(), m_ptr + right.size(), size() - right.size()); // destroy old
				}
				else if (right.size() <= capacity()) // enough room, copy and construct new
				{
					traits::copy_assign(m_ptr, right.m_ptr, size());
					traits::copy_constr(alloc(), m_ptr + size(), right.m_ptr + size(), right.size() - size());
				}
				else // not enough room, allocate new array and construct new
				{
					resize(0);
					ensure_space(right.capacity(), false);
					traits::copy_constr(alloc(), m_ptr, right.m_ptr, right.size());
				}
				m_count = right.size();
			}
		}


		
		// Assign by moving right
		#if _MSC_VER >= 1600
		template <int L,bool F,typename A> void _Assign(Array<Type,L,F,A>&& right) PR_NOEXCEPT
		{
			// Same object, do nothing
			if (isthis(right)) {}
			
			// Using different allocators and right.m_capacity is greater than the local count
			else if (!(allocator() == right.allocator()) && right.capacity() > LocalLength) { _Assign(right); }
			
			// Same allocator or less than local count, steal from right
			else
			{
				// Clean up any existing data
				resize(0);
				if (!local())
				{
					alloc.deallocate(m_ptr, m_capacity);
					m_ptr = local_ptr();
					m_capacity = LocalLength;
				}
				if (right.capacity() <= LocalLength)
				{
					traits::copy_assign(m_ptr, right.m_ptr, right.size());
					m_count = right.size();
				}
				else // steal the pointer
				{
					m_ptr      = right.m_ptr;
					m_capacity = right.m_capacity;
					m_count    = right.m_count;
				}
				right.m_ptr      = right.local_ptr();
				right.m_capacity = right.LocalLength;
				right.m_count    = 0;
			}
		}
		#endif
		
		// Insert count * value at iter
		void _Insert(const_iterator pos, size_type count, Type const& value)
		{
			PR_ASSERT(PR_DBG, begin() <= pos && pos <= end(), "insert position must be within the array");
			size_type ofs = pos - begin();
			if (count == 0)
			{
			}
			else if (count > max_size() - size())
			{
				throw std::overflow_error("pr::Array<> size too large");
			}
			else
			{
				ensure_space(m_count + count, true);
				Type* ins = m_ptr + ofs;                                   // The insert point
				Type* end = m_ptr + m_count;                               // The current end of the array
				size_type rem = size() - ofs;                              // The number of remaining elements after 'ofs'
				size_type n = rem > count ? count : rem;                   // min(count, rem)
				traits::copy_constr(alloc(), end + count - n, end - n, n); // copy construct the last 'n' elements
				traits::fill_constr(alloc(), end, count - n, value);       // fill from the current end to the element that was at ofs but has now moved. (might be nothing)
				traits::move_right (ins + count, ins, rem - n);            // move those right of the insert point to butt up with the now moved remainder
				traits::fill_assign(ins, n, value);                        // fill in the hole
			}
			m_count += count;
		}
		
		// insert [first, last) at pos
		template <typename iter> void _Insert(const_iterator pos, iter first, iter last)
		{
			PR_ASSERT(PR_DBG, first <= last, "last must follow first");
			PR_ASSERT(PR_DBG, begin() <= pos && pos <= end(), "pos must be within the array");
			size_type ofs = pos - begin(), count = last - first;
			if (count)
			{
				size_type old_count = m_count;
				ensure_space(m_count + count, true);
				for (; first != last; ++first, ++m_count)
					traits::fill_constr(alloc(), m_ptr + m_count, 1, *first);
				
				if (ofs != old_count)
				{
					reverse(m_ptr + ofs, m_ptr + old_count);
					reverse(m_ptr + old_count, m_ptr + m_count);
					reverse(m_ptr + ofs, m_ptr + m_count);
				}
			}
		}
		
		// Implicit conversion to a type that can be constructed from begin/end iterators
		// This allows cast to std::vector<> amoung others.
		// Note: converting to a std::vector<> when 'Type' has an alignment greater than the
		// default alignment causes a compiler error because of std::vector.resize().
		template <typename ArrayType> operator ArrayType() const
		{
			return ArrayType(begin(), end());
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/common/refcount.h"
#include "pr/common/refptr.h"

namespace pr
{
	namespace unittests
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

		typedef pr::Array<Type, 8, false> Array0;
		typedef pr::Array<Type, 16, true> Array1;
		std::vector<Type> ints;

		PRUnitTest(pr_common_array)
		{
			ints.resize(16);
			for (uint i = 0; i != 16; ++i) ints[i] = Type(i);

			g_start_object_count = g_object_count;
			{
				Array0 arr;
				PR_CHECK(arr.empty(), true);
				PR_CHECK(arr.size(), 0U);
			}
			PR_CHECK(g_object_count, g_start_object_count);
			g_start_object_count = g_object_count;
			{
				Array1 arr(15);
				PR_CHECK(!arr.empty(), true);
				PR_CHECK(arr.size(), 15U);
			}
			PR_CHECK(g_object_count, g_start_object_count);
			g_start_object_count = g_object_count;
			{
				Array0 arr(5U, 3);
				PR_CHECK(arr.size(), 5U);
				for (size_t i = 0; i != 5; ++i)
					PR_CHECK(arr[i], 3U);
			}
			PR_CHECK(g_object_count, g_start_object_count);
			g_start_object_count = g_object_count;
			{
				Array0 arr0(5U,3);
				Array1 arr1(arr0);
				PR_CHECK(arr1.size(), arr0.size());
				for (size_t i = 0; i != arr0.size(); ++i)
					PR_CHECK(arr1[i], arr0[i]);
			}
			PR_CHECK(g_object_count, g_start_object_count);
			g_start_object_count = g_object_count;
			{
				std::vector<uint> vec0(4U, 6);
				Array0 arr1(vec0);
				PR_CHECK(arr1.size(), vec0.size());
				for (size_t i = 0; i != vec0.size(); ++i)
					PR_CHECK(arr1[i], vec0[i]);
			}
			PR_CHECK(g_object_count, g_start_object_count);
			{//RefCounting0
				PR_CHECK(g_single.m_ref_count, 16);
			}
			{//Assign
				g_start_object_count = g_object_count;
				{
					Array0 arr0;
					arr0.assign(3U, 5);
					PR_CHECK(arr0.size(), 3U);
					for (size_t i = 0; i != 3; ++i)
						PR_CHECK(arr0[i], 5U);

					Array1 arr1;
					arr1.assign(&ints[0], &ints[8]);
					PR_CHECK(arr1.size(), 8U);
					for (size_t i = 0; i != 8; ++i)
						PR_CHECK(arr1[i], ints[i]);
				}
				PR_CHECK(g_object_count, g_start_object_count);
			}
			{//RefCounting1
				PR_CHECK(g_single.m_ref_count, 16);
			}
			{//Clear
				g_start_object_count = g_object_count;
				{
					Array0 arr0(ints.begin(), ints.end());
					arr0.clear();
					PR_CHECK(arr0.empty(), true);
				}
				PR_CHECK(g_object_count, g_start_object_count);
			}
			{//RefCounting2
				PR_CHECK(g_single.m_ref_count, 16);
			}
			{//Erase
				g_start_object_count = g_object_count;
				{
					Array0 arr0(ints.begin(), ints.begin() + 8);
					Array0::const_iterator b = arr0.begin();
					arr0.erase(b + 3, b + 5);
					PR_CHECK(arr0.size(), 6U);
					for (size_t i = 0; i != 3; ++i) PR_CHECK(arr0[i], ints[i]  );
					for (size_t i = 3; i != 6; ++i) PR_CHECK(arr0[i], ints[i+2]);
				}
				PR_CHECK(g_object_count,g_start_object_count);
				g_start_object_count = g_object_count;
				{
					Array1 arr1(ints.begin(), ints.begin() + 4);
					arr1.erase(arr1.begin() + 2);
					PR_CHECK(arr1.size(), 3U);
					for (size_t i = 0; i != 2; ++i) PR_CHECK(arr1[i], ints[i]  );
					for (size_t i = 2; i != 3; ++i) PR_CHECK(arr1[i], ints[i+1]);
				}
				PR_CHECK(g_object_count, g_start_object_count);
				g_start_object_count = g_object_count;
				{
					Array0 arr2(ints.begin(), ints.begin() + 5);
					arr2.erase_fast(arr2.begin() + 2);
					PR_CHECK(arr2.size(), 4U);
					for (size_t i = 0; i != 2; ++i) PR_CHECK(arr2[i], ints[i]);
					PR_CHECK(arr2[2], ints[4]);
					for (size_t i = 3; i != 4; ++i) PR_CHECK(arr2[i], ints[i]);
				}
				PR_CHECK(g_object_count, g_start_object_count);
			}
			{//RefCounting3
				PR_CHECK(g_single.m_ref_count, 16);
			}
			{//Insert
				g_start_object_count = g_object_count;
				{
					Array0 arr0;
					arr0.insert(arr0.end(), 4U, 9);
					PR_CHECK(arr0.size(), 4U);
					for (size_t i = 0; i != 4; ++i)
						PR_CHECK(arr0[i], 9U);
				}
				PR_CHECK(g_object_count, g_start_object_count);
				g_start_object_count = g_object_count;
				{
					Array1 arr1(4U, 6);
					arr1.insert(arr1.begin() + 2, &ints[2], &ints[7]);
					PR_CHECK(arr1.size(), 9U);
					for (size_t i = 0; i != 2; ++i) PR_CHECK(arr1[i], 6U);
					for (size_t i = 2; i != 7; ++i) PR_CHECK(arr1[i], ints[i]);
					for (size_t i = 7; i != 9; ++i) PR_CHECK(arr1[i], 6U);
				}
				PR_CHECK(g_object_count, g_start_object_count);
			}
			{//RefCounting4
				PR_CHECK(g_single.m_ref_count, 16);
			}
			{//PushPop
				g_start_object_count = g_object_count;
				{
					Array0 arr;
					arr.insert(arr.begin(), &ints[0], &ints[4]);
					arr.pop_back();
					PR_CHECK(arr.size(), 3U);
					for (size_t i = 0; i != 3; ++i)
						PR_CHECK(arr[i], ints[i]);
				}
				PR_CHECK(g_object_count, g_start_object_count);
				g_start_object_count = g_object_count;
				{
					Array1 arr;
					arr.reserve(4);
					for (int i = 0; i != 4; ++i) arr.push_back_fast(i);
					for (int i = 4; i != 9; ++i) arr.push_back(i);
					for (size_t i = 0; i != 9; ++i)
						PR_CHECK(arr[i], ints[i]);
				}
				PR_CHECK(g_object_count, g_start_object_count);
				g_start_object_count = g_object_count;
				{
					Array1 arr;
					arr.insert(arr.begin(), &ints[0], &ints[4]);
					arr.resize(3);
					PR_CHECK(arr.size(), 3U);
					for (size_t i = 0; i != 3; ++i)
						PR_CHECK(arr[i], ints[i]);
					arr.resize(6);
					PR_CHECK(arr.size(), 6U);
					for (size_t i = 0; i != 3; ++i)
						PR_CHECK(arr[i], ints[i]);
					for (size_t i = 3; i != 6; ++i)
						PR_CHECK(arr[i], 0U);
				}
				PR_CHECK(g_object_count, g_start_object_count);
			}
			{//RefCounting5
				PR_CHECK(g_single.m_ref_count, 16);
			}
			{//Operators
				g_start_object_count = g_object_count;
				{
					Array0 arr0(4U, 1);
					Array0 arr1(3U, 2);
					arr1 = arr0;
					PR_CHECK(arr0.size(), 4U);
					PR_CHECK(arr1.size(), 4U);
					for (size_t i = 0; i != 4; ++i)
						PR_CHECK(arr1[i], arr0[i]);
				}
				PR_CHECK(g_object_count, g_start_object_count);
				g_start_object_count = g_object_count;
				{
					Array0 arr0(4U, 1);
					Array1 arr2;
					arr2 = arr0;
					PR_CHECK(arr0.size(), 4U);
					PR_CHECK(arr2.size(), 4U);
					for (size_t i = 0; i != 4; ++i)
						PR_CHECK(arr2[i], arr0[i]);
		
					struct L
					{
						static std::vector<Type> Conv(std::vector<Type> v) { return v; }
					};
					std::vector<Type> vec0 = L::Conv(arr0);
					PR_CHECK(vec0.size(), 4U);
					for (size_t i = 0; i != 4; ++i)
						PR_CHECK(vec0[i], arr0[i]);
				}
				PR_CHECK(g_object_count, g_start_object_count);
			}
			{//RefCounting6
				PR_CHECK(g_single.m_ref_count, 16);
			}
			{//Mem
				g_start_object_count = g_object_count;
				{
					Array0 arr0;
					arr0.reserve(100);
					for (uint i = 0; i != 50; ++i) arr0.push_back(i);
					PR_CHECK(arr0.capacity(), 100U);
					arr0.shrink_to_fit();
					PR_CHECK(arr0.capacity(), 50U);
					arr0.resize(1);
					arr0.shrink_to_fit();
					PR_CHECK(arr0.capacity(), (size_t)arr0.LocalLength);
				}
				PR_CHECK(g_object_count, g_start_object_count);
			}
			{//RefCounting
				ints.clear();
				PR_CHECK(g_single.m_ref_count, 0);
			}
			{//AlignedTypes
				g_start_object_count = g_object_count;
				{
					pr::Spline spline = pr::Spline::make(pr::Random3N(1.0f), pr::Random3N(1.0f), pr::Random3N(1.0f), pr::Random3N(1.0f));
		
					pr::Array<pr::v4> arr0;
					pr::Raster(spline, arr0, 100);
		
					PR_CHECK(arr0.capacity() > arr0.LocalLength, true);
					arr0.resize(5);
					arr0.shrink_to_fit();
					PR_CHECK(arr0.size(), 5U);
					PR_CHECK(arr0.capacity(), size_t(arr0.LocalLength));
				}
				PR_CHECK(g_object_count, g_start_object_count);
			}
			{//GlobalConstrDestrCount
				PR_CHECK(g_object_count, 0);
			}
		}
	}
}
#endif

#ifdef PR_NOEXCEPT_DEFINED
#   undef PR_NOEXCEPT_DEFINED
#   undef PR_NOEXCEPT
#endif
#ifdef PR_ASSERT_DEFINED
#   undef PR_ASSERT_DEFINED
#   undef PR_ASSERT
#endif

#endif
