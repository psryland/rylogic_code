//******************************************
// pr::string<>
//  Copyright © Rylogic Ltd 2008
//******************************************
// A version of std::string with configurable local caching
// Note: this file is made to match pr::Array as much as possible
#pragma once
#ifndef PR_STD_STRING_H
#define PR_STD_STRING_H

#pragma intrinsic(memcmp, memcpy, memset, strcmp)
#include <memory>
#include <string>
#include <sstream>
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

namespace pr
{
	// Not intended to be a complete replacement, just a 90% substitute
	// Allocator = the type to do the allocation/deallocation. *Can be a pointer to an std::allocator like object*
	// Note about null termination:
	//	'm_count' is the length of the string including the null terminator
	//	therefore it's value is always >= 1
	// Note about LocalCount:
	//	Default local count is choosen to make sizeof(pr::string<>) == 256
	template <typename Type=char, int LocalCount=244, bool Fixed=false, typename Allocator=std::allocator<Type> >
	class string
	{
	public:
		typedef string<Type, LocalCount, Fixed, Allocator> type;
		typedef Type const*    const_iterator;
		typedef Type*          iterator;
		typedef Type const*    const_pointer;
		typedef Type*          pointer;
		typedef Type const&    const_reference;
		typedef Type&          reference;
		typedef std::size_t    size_type;
		typedef std::ptrdiff_t difference_type;
		typedef Type           value_type;
		typedef Allocator      allocator_type;
		typedef typename pr::mpl::remove_pointer<Allocator>::type AllocType; // The type of the allocator ignoring pointers
		
		enum
		{
			LocalLength      = LocalCount,
			LocalSizeInBytes = LocalCount * sizeof(value_type),
			TypeIsPod        = pr::mpl::is_pod<Type>::value,
			TypeAlignment    = pr::mpl::alignment_of<Type>::value,
		};
		
		// End of string index position
		static const size_type npos = static_cast<size_type>(-1);
		
		// type specific traits
		template <typename Type> struct char_traits;
		template <> struct char_traits<char>
		{
			static size_type length(const_pointer ptr)                                              { return ::strlen(ptr); }
			static void      fill(pointer ptr, size_type count, value_type ch)                      { ::memset(ptr, ch, count); }
			static void      copy(pointer dst, const_pointer src, size_type count)                  { ::memcpy(dst, src, count); }
			static void      move(pointer dst, const_pointer src, size_type count)                  { ::memmove(dst, src, count); }
			static int       compare(const_pointer first1, const_pointer first2, size_type count)   { return ::strncmp(first1, first2, count); }
			static bool      eq(value_type lhs, value_type rhs)                                     { return lhs == rhs; }
			static const_pointer find(const_pointer first, size_type count, value_type ch)          { for (; 0 < count; --count, ++first) { if (eq(*first, ch)) return first; } return 0; }
		};
		template <> struct char_traits<wchar_t>
		{
			static size_type length(const_pointer ptr)                                              { return ::wcslen(ptr); }
			static void      fill(pointer ptr, size_type count, value_type ch)                      { for (;count-- != 0; ++ptr) *ptr = ch; }
			static void      copy(pointer dst, const_pointer src, size_type count)                  { ::memcpy(dst, src, count * sizeof(wchar_t)); }
			static void      move(pointer dst, const_pointer src, size_type count)                  { ::memmove(dst, src, count * sizeof(wchar_t)); }
			static int       compare(const_pointer first1, const_pointer first2, size_type count)   { return ::wcsncmp(first1, first2, count); }
			static bool      eq(value_type lhs, value_type rhs)                                     { return lhs == rhs; }
			static const_pointer find(const_pointer first, size_type count, value_type ch)          { for (; 0 < count; --count, ++first) { if (eq(*first, ch)) return first; } return 0; }
		};
		struct traits :char_traits<Type>
		{
		};
		
	private:
		typedef typename pr::mpl::aligned_storage<sizeof(Type), pr::mpl::alignment_of<Type>::value>::type TLocalStore;
		TLocalStore m_local[LocalLength]; // Local cache for small arrays
		Type*       m_ptr;                // Pointer to the array of data
		size_type   m_capacity;           // The reserved space for elements. m_capacity * sizeof(Type) = size in bytes pointed to by m_ptr.
		size_type   m_count;              // The number of used elements in the array + 1 for the null term
		Allocator   m_allocator;          // The memory allocator
		
		// Any combination of type, local count, fixed, and allocator is a friend
		template <class T, int L, bool F, class A> friend class string;
		
		// Access to the allocator object (independant over whether its a pointer or instance)
		// (enable_if requires type inference to work, hence the 'A' template parameter)
		template <typename A> typename pr::mpl::enable_if<!pr::mpl::is_pointer<A>::value, AllocType const&>::type alloc(A) const { return m_allocator; }
		template <typename A> typename pr::mpl::enable_if< pr::mpl::is_pointer<A>::value, AllocType const&>::type alloc(A) const { return *m_allocator; }
		template <typename A> typename pr::mpl::enable_if<!pr::mpl::is_pointer<A>::value, AllocType&      >::type alloc(A)       { return m_allocator; }
		template <typename A> typename pr::mpl::enable_if< pr::mpl::is_pointer<A>::value, AllocType&      >::type alloc(A)       { return *m_allocator; }
		
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
		// 'new_count' should equal 'size() + 1' to include the null term.
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
				traits::copy(new_array, m_ptr, m_count);
				
				// Destruct the old array
				if (!local()) alloc().deallocate(m_ptr, m_capacity);
				
				m_ptr = new_array;
				m_capacity = new_cap;
				PR_ASSERT(PR_DBG, m_capacity >= LocalLength, "");
			}
			else
			{
				PR_ASSERT(PR_DBG, new_count <= m_capacity, "non-allocating container capacity exceeded");
				if (new_count > m_capacity)
					throw std::overflow_error("pr::string<> out of memory");
			}
		}
		
	public:
		
		// The memory allocator
		AllocType const& alloc() const     { return alloc(m_allocator); }
		AllocType&       alloc()           { return alloc(m_allocator); }
		Allocator const& allocator() const { return m_allocator; }
		Allocator&       allocator()       { return m_allocator; }
				
		// construct empty collection
		string()
		:m_ptr(local_ptr())
		,m_capacity(LocalLength)
		,m_count(1)
		,m_allocator()
		{
			m_ptr[0] = 0;
		}
		
		// construct with custom allocator
		explicit string(Allocator const& allocator)
		:m_ptr(local_ptr())
		,m_capacity(LocalLength)
		,m_count(1)
		,m_allocator(allocator)
		{
			m_ptr[0] = 0;
			m_allocator = allocator;
		}
		
		// construct from count * ch
		explicit string(size_type count, value_type ch)
		:m_ptr(local_ptr())
		,m_capacity(LocalLength)
		,m_count(1)
		,m_allocator()
		{
			m_ptr[0] = 0;
			assign(count, ch);
		}
		
		// construct from [ptr, <null>)
		string(const_pointer ptr)
		:m_ptr(local_ptr())
		,m_capacity(LocalLength)
		,m_count(1)
		,m_allocator()
		{
			m_ptr[0] = 0;
			assign(ptr);
		}
		
		// copy construct (explicit copy constructor needed to prevent auto generated one even tho there's a template one that would work)
		string(string const& right)
		:m_ptr(local_ptr())
		,m_capacity(LocalLength)
		,m_count(1)
		,m_allocator()
		{
			m_ptr[0] = 0;
			assign(right);
		}
		
		// copy construct from any pr::string type
		template <int L, bool F, class A> string(string<Type,L,F,A> const& right)
		:m_ptr(local_ptr())
		,m_capacity(LocalLength)
		,m_count(1)
		,m_allocator(right.m_allocator)
		{
			m_ptr[0];
			assign(right);
		}
		
		// construct from [first, last), with allocator
		template <class iter> string(iter first, iter last, Allocator const& allocator = Allocator())
		:m_ptr(local_ptr())
		,m_capacity(LocalLength)
		,m_count(1)
		,m_allocator(allocator)
		{
			m_ptr[0] = 0;
			
			// not using assign here because it's recurses
			difference_type count = std::distance(first,last);
			ensure_space(count + 1, false);
			m_count += count; // before we use count
			for (Type* ptr = m_ptr; count-- != 0; ++ptr, ++first) *ptr = *first;
			m_ptr[size()] = 0;
		}
		
		// construct from another string-like object
		// 2nd parameter used to prevent overload issues with container(count)
		template <typename tarr> string(tarr const& right, typename tarr::size_type = 0, Allocator const& allocator = Allocator())
		:m_ptr(local_ptr())
		,m_capacity(LocalLength)
		,m_count(1)
		,m_allocator(allocator)
		{
			m_ptr[0] = 0;
			assign(right);
		}
		
		// construct from a literal string
		template <int Len> string(Type const (&right)[Len])
		:m_ptr(local_ptr())
		,m_capacity(LocalLength)
		,m_count(1)
		,m_allocator()
		{
			m_ptr[0] = 0;
			assign(right);
		}

		// move construct
		#if _MSC_VER >= 1600
		string(string&& right)
		:m_ptr(local_ptr())
		,m_capacity(LocalLength)
		,m_count(1)
		,m_allocator(right.m_allocator)
		{
			m_ptr[0] = 0;
			assign(std::forward<string>(right));
		}
		template <int L, bool F, class A> string(string<Type,L,F,A>&& right)
		:m_ptr(local_ptr())
		,m_capacity(LocalLength)
		,m_count(1)
		,m_allocator(right.m_allocator)
		{
			m_ptr[0] = 0;
			assign(std::forward< string<Type,L,F,A> >(right));
		}
		#endif
		
		// construct from right [rofs, rofs + count)
		template <typename tarr> string(tarr const& right, size_type rofs, size_type count)
		:m_ptr(local_ptr())
		,m_capacity(LocalLength)
		,m_count(1)
		,m_allocator()
		{
			m_ptr[0] = 0;
			assign(right, rofs, count);
		}
		
		// destruct
		~string()
		{
			clear();
		}
		
		// Iterators
		const_iterator begin() const
		{
			return m_ptr;
		}
		const_iterator end() const
		{
			return m_ptr + m_count - 1;
		}
		iterator begin()
		{
			return m_ptr;
		}
		iterator end()
		{
			return m_ptr + m_count - 1;
		}
		
		// insert element at end
		void push_back(value_type value)
		{
			ensure_space(m_count + 1, true);
			push_back_fast(value);
		}
		
		// Add an element to the end of the array without "ensure_space" first
		void push_back_fast(value_type value)
		{
			PR_ASSERT(PR_DBG, (m_count + 1) <= m_capacity, "Container overflow");
			m_ptr[size()] = value;
			m_count++;
			m_ptr[size()] = 0;
		}
		
		// Deletes the element at the end of the array.
		void pop_back()
		{
			PR_ASSERT(PR_DBG, !empty(), "");
			--m_count;
		}
		
		// return pointer to nonmutable array
		const_pointer data() const
		{
			return m_ptr;
		}
		pointer data()
		{
			return m_ptr;
		}
		
		// const string pointer
		const_pointer c_str() const
		{
			return m_ptr;
		}
		
		// test if sequence is empty
		bool empty() const
		{
			return size() == 0;
		}
		
		// return length of sequence
		size_type size() const
		{
			return m_count - 1;
		}
		
		// return the available length within allocation
		size_type capacity() const
		{
			return m_capacity - 1;
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
			if (!local()) alloc().deallocate(m_ptr, m_capacity);
			m_ptr = local_ptr();
			m_capacity = LocalLength;
			m_count = 1;
			m_ptr[size()] = 0;
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
			resize(newsize, value_type());
		}
		void resize(size_type newsize, value_type ch)
		{
			if (newsize > size())
			{
				ensure_space(newsize + 1, false);
				traits::fill(m_ptr + m_count, newsize - m_count, ch);
			}
			m_count = newsize + 1;
			m_ptr[size()] = 0;
		}
		
		// Index operator
		const_reference operator [](size_type i) const
		{
			PR_ASSERT(PR_DBG, i < m_count, "out of range");
			return m_ptr[i];
		}
		reference operator [](size_type i)
		{
			PR_ASSERT(PR_DBG, i < m_count, "out of range");
			return m_ptr[i];
		}
		
		// assign right (explicit assignment operator needed to prevent auto generated one)
		string& operator = (string const& right)
		{
			assign(right);
			return *this;
		}
		
		// assign right from any pr::string<Type,...>
		template <int L,bool F,typename A> string& operator = (string<Type,L,F,A> const& right)
		{
			assign(right);
			return *this;
		}
		
		// assign right
		template <int Len> string& operator = (Type const (&right)[Len])
		{
			assign(right);
			return *this;
		}
		
		// assign right
		template <typename tarr> string& operator = (tarr const& right)
		{
			assign(right);
			return *this;
		}
		
		// move right
		#if _MSC_VER >= 1600
		// explicit move right
		string& operator = (string&& right)
		{
			assign(std::forward<string>(right));
			return *this;
		}
		
		// move right from any pr::string<>
		template <int L,bool F,typename A> string& operator =(string<Type,L,F,A>&& rhs)
		{
			assign(std::move(rhs));
			return *this;
		}
		#endif
		
		// assign [ptr, <null>)
		string& operator = (const_pointer ptr)
		{
			return assign(ptr);
		}
		
		// assign 1 * ch
		string& operator = (value_type ch)
		{
			return assign(1, ch);
		}
		
		// append right
		template <typename tstr> string& operator += (tstr const& right)
		{
			return append(right);
		}
		
		// append [ptr, <null>)
		string& operator += (const_pointer ptr)
		{
			return append(ptr);
		}
		
		// append 1 * ch
		string& operator += (value_type ch)
		{
			return append(size_type(1), ch);
		}
		
		// assign count * ch
		string& assign(size_type count, value_type ch)
		{
			ensure_space(count + 1, true);
			traits::fill(m_ptr, count, ch);
			m_count = count + 1;
			m_ptr[size()] = 0;
			return *this;
		}
		
		// assign [first, last), iterators
		template <typename iter> string& assign(iter first, iter last)
		{
			return replace(begin(), end(), first, last);
		}
		
		// assign [ptr, ptr + count)
		string& assign(const_pointer ptr, size_type count)
		{
			// assigning from a substring
			if (inside(ptr))
				return assign(*this, ptr - m_ptr, count);
			
			// copy the string
			ensure_space(count + 1, true);
			traits::copy(m_ptr, ptr, count);
			m_count = count + 1;
			m_ptr[size()] = 0;
			return *this;
		}
		
		// assign [ptr, <null>)
		string& assign(const_pointer ptr)
		{
			return assign(ptr, traits::length(ptr));
		}
		
		// assign [first, last), const pointers
		string& assign(const_pointer first, const_pointer last)
		{
			return replace(begin(), end(), first, last);
		}
		
		#if _MSC_VER >= 1600
		// assign by moving right
		template <int L,bool F,typename A> string& assign(string<Type,L,F,A>&& right)
		{
			// Same object, do nothing
			if (isthis(right)) {}
			
			// Using different allocators and right.m_capacity is greater than the local count
			else if (!(allocator() == right.allocator()) && right.capacity() > LocalLength) { assign(right); }
			
			// Same allocator or less than local count, steal from right
			else
			{
				if (!local())
				{
					alloc().deallocate(m_ptr, m_capacity);
					m_ptr = local_ptr();
					m_capacity = LocalLength;
				}
				if (right.m_capacity <= LocalLength)
				{
					traits::copy(m_ptr, right.m_ptr, right.m_count);
					m_count = right.m_count;
				}
				else // steal the pointer
				{
					m_ptr      = right.m_ptr;
					m_capacity = right.m_capacity;
					m_count    = right.m_count;
				}
				right.m_ptr      = right.local_ptr();
				right.m_capacity = right.LocalLength;
				right.m_count    = 1;
				right.m_ptr[0]   = 0;
			}
			return *this;
		}
		#endif
		
		// assign right [rofs, rofs + count)
		template <typename tarr> string& assign(tarr const& right, size_type rofs, size_type count)
		{
			PR_ASSERT(PR_DBG, rofs <= right.size(), "");
			
			size_type num = right.size() - rofs;
			if (count > num) count = num;
			if (isthis(right)) // subrange
			{
				erase(size_type(rofs + count));
				erase(0, rofs);
			}
			else // make room and assign new stuff
			{
				ensure_space(count + 1, true);
				traits::copy(m_ptr, right.c_str() + rofs, count);
				m_count = count + 1;
				m_ptr[count] = 0;
			}
			return *this;
		}
		
		// assign right
		template <typename tarr> string& assign(tarr const& right)
		{
			return assign(right, 0, npos);
		}
		
		// assign right
		template <int Len> string& assign(Type const (&right)[Len])
		{
			return assign(&right[0], 0, Len);
		}
		
		// insert count * ch at ofs
		string& insert(size_type ofs, size_type count, value_type ch)
		{
			PR_ASSERT(PR_DBG, ofs <= size(), "");
			ensure_space(m_count + count, true);
			traits::move(m_ptr + ofs + count, m_ptr + ofs, m_count - ofs);
			traits::fill(m_ptr + ofs, count, ch);
			m_count += count;
			m_ptr[size()] = 0;
			return *this;
		}
		
		// insert right at ofs
		template <typename tstr> string& insert(size_type ofs, tstr const& right)
		{
			return insert(ofs, right, 0, npos);
		}
		
		// insert right [rofs, rofs + count) at ofs
		template <typename tstr> string& insert(size_type ofs, tstr const& right, size_type rofs, size_type count)
		{
			PR_ASSERT(PR_DBG, size() >= ofs && right.size() >= rofs, ""); // ofs or rofs off end
			size_type num = right.size() - rofs;
			if (num < count) count = num; // trim count to size
			PR_ASSERT(PR_DBG, npos - size() > count, ""); // result too long
			if (count == 0) return *this;
			ensure_space(m_count + count, true); // make room and insert new stuff
			traits::move(m_ptr + ofs + count, m_ptr + ofs, m_count - ofs); // empty out hole
			if (isthis(right)) traits::move(m_ptr + ofs, m_ptr + (ofs < rofs ? rofs + count : rofs), count); // substring
			else               traits::copy(m_ptr + ofs, right.c_str() + rofs, count); // fill hole
			m_count += count;
			m_ptr[size()] = 0;
			return *this;
		}
		
		// insert [ptr, ptr + count) at ofs
		string& insert(size_type ofs, Type const* ptr)
		{
			return insert(ofs, ptr, traits::length(ptr));
		}
		
		// insert [ptr, ptr + count) at ofs
		string& insert(size_type ofs, Type const* ptr, size_type count)
		{
			PR_ASSERT(PR_DBG, ofs < m_count, "offset off the end of this string");
			PR_ASSERT(PR_DBG, count <= traits::length(ptr), "'count' is longer than the null terminated string 'ptr'");
			PR_ASSERT(PR_DBG, npos - size() > count, "result too long");
			
			if (count == 0)
				return *this;
			
			if (inside(ptr)) // substring
				return insert(ofs, *this, ptr - m_ptr, count);
			
			ensure_space(m_count + count, true); // make room and insert new stuff
			traits::move(m_ptr + ofs + count, m_ptr + ofs, m_count - ofs); // empty out hole
			traits::copy(m_ptr + ofs, ptr, count); // fill hole
			m_count += count;
			m_ptr[size()] = 0;
			return *this;
		}
		
		// insert right at ofs
		template <int Len> string& insert(size_type ofs, Type const (&right)[Len])
		{
			return insert(ofs, &right[0], Len);
		}
		
		// insert ch at iter
		iterator insert(const_iterator iter, value_type ch)
		{
			size_type ofs = iter - begin();
			insert(ofs, 1, ch);
			return begin() + ofs;
		}
		
		// insert <null> at iter
		iterator insert(const_iterator iter)
		{
			return insert(iter, value_type());
		}
		
		// erase elements [ofs, ofs + count)
		string& erase(size_type ofs = 0, size_type count = npos)
		{
			size_type num = size() - ofs;
			if (count > num) count = num;
			traits::move(m_ptr + ofs, m_ptr + ofs + count, m_count - (ofs + count));
			m_count -= count;
			m_ptr[size()] = 0;
			return *this;
		}
		
		// erase 1 element at 'at'
		iterator erase(const_iterator at)
		{
			size_type count = at - begin();
			erase(count, 1);
			return begin() + count;
		}
		
		// erase substring [first, last)
		iterator erase(const_iterator first, const_iterator last)
		{
			size_type count = first - begin();
			erase(count, last - first);
			return begin() + count;
		}
		
		// append right [rofs, rofs + count)
		template <typename tstr> string& append(tstr const& right, size_type rofs, size_type count)
		{
			PR_ASSERT(PR_DBG, rofs <= right.size(), "");
			
			size_type num = right.size() - rofs;
			if (num < count) count = num;
			
			ensure_space(m_count + count, true);
			traits::copy(m_ptr + size(), right.c_str() + rofs, count);
			m_count += count;
			m_ptr[size()] = 0;
			return *this;
		}
		
		// append right
		template <typename tstr> string& append(tstr const& right)
		{
			return append(right, 0, npos);
		}
		
		// append [ptr, ptr + count)
		string& append(const_pointer ptr, size_type count)
		{
			if (inside(ptr)) return append(*this, ptr - m_ptr, count); // substring
			ensure_space(m_count + count, true);
			traits::copy(m_ptr + size(), ptr, count);
			m_count += count;
			m_ptr[size()] = 0;
			return *this;
		}
		
		// append [ptr, <null>)
		string& append(const_pointer ptr)
		{
			return append(ptr, traits::length(ptr));
		}
		
		// append count * ch
		string& append(size_type count, value_type ch)
		{
			ensure_space(m_count + count, true);
			traits::fill(m_ptr + size(), count, ch);
			m_count += count;
			m_ptr[size()] = 0;
			return *this;
		}
		
		// append [first, last), const pointers
		string& append(const_pointer first, const_pointer last)
		{
			return append(first, last - first);
		}
		
		// append [first, last), input iterators
		template <typename iter> string& append(iter first, iter last)
		{
			difference_type count = std::distance(first, last);
			ensure_space(m_count + count, true);
			for (Type* s = m_ptr + size(); first != last; ++first, ++s) *s = *first;
			m_count += count;
			m_ptr[size()] = 0;
			return *this;
		}
		
		// append right
		template <int Len> string& append(Type const (&right)[Len])
		{
			return append(&right[0], Len);
		}
		
		// compare [ofs, ofs + n0) with [ptr, ptr + count)
		int compare(size_type ofs, size_type n0, const_pointer ptr, size_type count) const
		{
			if (size() - ofs < n0) n0 = size() - ofs;
			size_type ans = traits::compare(m_ptr + ofs, ptr, n0 < count ? n0 : count);
			return (ans != 0 ? int(ans) : n0 < count ? -1 : n0 == count ? 0 : +1);
		}
		
		// compare [ofs, ofs + n0) with right [rofs, rofs + count)
		template <typename tstr> int compare(size_type ofs, size_type n0, tstr const& right, size_type rofs, size_type count) const
		{
			PR_ASSERT(PR_DBG, rofs <= right.size(), "");
			if (right.size() - rofs < count) count = right.size() - rofs;
			return compare(ofs, n0, right.c_str() + rofs, count);
		}
		
		// compare [0, size()) with right
		template <typename tstr> int compare(tstr const& right) const
		{
			return compare(0, size(), right, 0, npos);
		}
		
		// compare [ofs, ofs + n0) with right
		template <typename tstr> int compare(size_type ofs, size_type n0, tstr const& right) const
		{
			return compare(ofs, n0, right, 0, npos);
		}
		
		// compare [0, size()) with [ptr, <null>)
		int compare(const_pointer ptr) const
		{
			return compare(0, size(), ptr, traits::length(ptr));
		}
		
		// compare [ofs, ofs + n0) with [ptr, <null>)
		int compare(size_type ofs, size_type n0, const_pointer ptr) const
		{
			return compare(ofs, n0, ptr, traits::length(ptr));
		}
		
		// replace [ofs, ofs + n0) with right
		template <typename tstr> string& replace(size_type ofs, size_type n0, tstr const& right)
		{
			return replace(ofs, n0, right, 0, npos);
		}
		
		// replace [ofs, ofs + n0) with right [rofs, rofs + count)
		template <typename tstr> string& replace(size_type ofs, size_type n0, tstr const& right, size_type rofs, size_type count)
		{
			PR_ASSERT(PR_DBG, ofs < size() && rofs <= right.size(), "");
			if (size()       - ofs  < n0   ) n0    = size()       - ofs;  // trim n0 to size
			if (right.size() - rofs < count) count = right.size() - rofs; // trim count to size
			PR_ASSERT(PR_DBG, !(npos - count <= size() - n0), "");        // result too long
			size_type rcount  = m_count - n0 - ofs;                       // length of preserved tail (incl null)
			ensure_space(m_count + count - n0, true);
			
			if (!isthis(right)) // no overlap, just move down and copy in new stuff
			{
				traits::move(m_ptr + ofs + count, m_ptr + ofs + n0, rcount);      // empty hole
				traits::copy(m_ptr + ofs, right.c_str() + rofs, count);           // fill hole
			}
			else if (count <= n0) // hole doesn't get larger, just copy in substring
			{
				traits::move(m_ptr + ofs, m_ptr + rofs, count);                   // fill hole
				traits::move(m_ptr + ofs + count, m_ptr + ofs + n0, rcount);      // move tail down
			}
			else if (rofs <= ofs) // hole gets larger, substring begins before hole
			{
				traits::move(m_ptr + ofs + count, m_ptr + ofs + n0, rcount);      // move tail down
				traits::move(m_ptr + ofs, m_ptr + rofs, count);                   // fill hole
			}
			else if (ofs + n0 <= rofs) // hole gets larger, substring begins after hole
			{
				traits::move(m_ptr + ofs + count, m_ptr + ofs + n0, rcount);      // move tail down
				traits::move(m_ptr + ofs, m_ptr + (rofs + count - n0), count);    // fill hole
			}
			else // hole gets larger, substring begins in hole
			{
				traits::move(m_ptr + ofs, m_ptr + rofs, n0);                      // fill old hole
				traits::move(m_ptr + ofs + count, m_ptr + ofs + n0, rcount);      // move tail down
				traits::move(m_ptr + ofs + n0, m_ptr + rofs + count, count - n0); // fill rest of new hole
			}
			m_count += count - n0;
			m_ptr[size()] = 0;
			return *this;
		}
		
		// replace [ofs, ofs + n0) with [ptr, ptr + count)
		string& replace(size_type ofs, size_type n0, const_pointer ptr, size_type count)
		{
			PR_ASSERT(PR_DBG, count == 0 || ptr != 0, "");
			if (inside(ptr)) return replace(ofs, n0, *this, ptr - m_ptr, count); // substring, replace carefully
			
			PR_ASSERT(PR_DBG, !(size() < ofs), "");                       // ofs off end
			if (size() - ofs < n0) n0 = size() - ofs;                     // trim n0 to size
			PR_ASSERT(PR_DBG, !(npos - count <= size() - n0), "");        // result too long
			size_type rcount = m_count - n0 - ofs;                        // length of preserved tail (incl null)
			
			if (count < n0) // smaller hole, move tail up
				traits::move(m_ptr + ofs + count, m_ptr + ofs + n0, rcount);
			
			// make room and rearrange
			if (0 < count || 0 < n0)
			{
				ensure_space(m_count + count - n0, true);
				if (n0 < count) traits::move(m_ptr + ofs + count, m_ptr + ofs + n0, rcount); // move tail down
				traits::copy(m_ptr + ofs, ptr, count); // fill hole
				m_count += count - n0;
				m_ptr[size()] = 0;
			}
			return *this;
		}
		
		// replace [ofs, ofs + n0) with [ptr, <null>)
		string& replace(size_type ofs, size_type n0, const_pointer ptr)
		{
			PR_ASSERT(PR_DBG, ptr != 0, "");
			return replace(ofs, n0, ptr, traits::length(ptr));
		}
		
		// replace [ofs, ofs + n0) with count * ch
		string& replace(size_type ofs, size_type n0, size_type count, value_type ch)
		{
			PR_ASSERT(PR_DBG, !(size() < ofs), "");                // ofs off end
			if (size() - ofs < n0) n0 = size() - ofs;                     // trim n0 to size
			PR_ASSERT(PR_DBG, !(npos - count <= size() - n0), ""); // result too long
			size_type rcount = m_count - n0 - ofs;                        // length of preserved tail (incl null)
			
			if (count < n0) // smaller hole, move tail up
				traits::move(m_ptr + ofs + count, m_ptr + ofs + n0, rcount);
			
			// make room and rearrange
			if (0 < count || 0 < n0)
			{
				ensure_space(m_count + count - n0, true);
				if (n0 < count) traits::move(m_ptr + ofs + count, m_ptr + ofs + n0, rcount); // move tail down
				traits::fill(m_ptr + ofs, count, ch); // fill hole
				m_count += count - n0;
				m_ptr[size()] = 0;
			}
			return *this;
		}
		
		// replace [first, last) with right
		template <typename tstr> string& replace(const_iterator first, const_iterator last, tstr const& right)
		{
			return replace(first - begin(), last - first, right);
		}
		
		// replace [first, last) with [ptr, ptr + count)
		string& replace(const_iterator first, const_iterator last, const_pointer ptr, size_type count)
		{
			return replace(first - begin(), last - first, ptr, count);
		}
		
		// replace [first, last) with [ptr, <null>)
		string& replace(const_iterator first, const_iterator last, const_pointer ptr)
		{
			return replace(first - begin(), last - first, ptr);
		}
		
		// replace [first, last) with count * ch
		string& replace(const_iterator first, const_iterator last, size_type count, value_type ch)
		{
			return replace(first - begin(), last - first, count, ch);
		}
		
		// replace [first, last) with [first2, last2)
		template <typename iter> string& replace(const_iterator first, const_iterator last, iter first2, iter last2)
		{
			return replace(first, last, string(first2, last2));
		}
		
		// replace [first, last) with [first2, last2), const pointers
		string& replace(const_iterator first, const_iterator last, const_pointer first2, const_pointer last2)
		{
			if (first2 == last2) erase(first - begin(), last - first);
			else                 replace(first - begin(), last - first, &*first2, last2 - first2);
			return *this;
		}
		
		// look for [ptr, ptr + count) beginnng at or after ofs
		size_type find(const_pointer ptr, size_type ofs, size_type count) const
		{
			// null string always matches (if inside string)
			if (count == 0 && ofs <= size())
				return ofs;
			
			// room for match, look for it
			size_type num;
			if (ofs < size() && count <= (num = size() - ofs))
			{
				const value_type *u, *v;
				for (num -= count - 1, v = m_ptr + ofs; (u = traits::find(v, num, *ptr)) != 0; num -= u - v + 1, v = u + 1)
					if (traits::compare(u, ptr, count) == 0)
						return u - m_ptr; // found a match
			}
			return npos; // no match
		}
		
		// look for right beginnng at or after ofs
		template <int Len> size_type find(Type const (&right)[Len], size_type ofs = 0) const
		{
			// Note: Len will include the null terminator for literal strings
			return find(&right[0], ofs, Len);
		}
		
		// look for right beginnng at or after ofs
		template <typename tstr> size_type find(tstr const& right, size_type ofs = 0) const
		{
			return find(right.c_str(), ofs, right.size());
		}
		
		// look for [ptr, ptr + count) beginning before ofs
		size_type rfind(const_pointer ptr, size_type ofs, size_type count) const
		{
			// null always matches
			if (count == 0)
				return (ofs < size() ? ofs : size());

			// room for match, look for it
			if (count <= size())
			{
				const_pointer u = m_ptr + (ofs < size() - count ? ofs : size() - count);
				for (; ; --u)
				{
					if (traits::eq(*u, *ptr) && traits::compare(u, ptr, count) == 0) return (u - m_ptr); // found a match
					else if (u == m_ptr) break;	// at beginning, no more chance for match
				}
			}
			return npos; // no match
		}
		
		// look for right beginning before ofs
		template <typename tstr> size_type rfind(tstr const& right, size_type ofs = npos) const
		{
			return rfind(right.c_str(), ofs, right.size());
		}
		
		// look for [ptr, <null>) beginning before ofs
		size_type rfind(const_pointer ptr, size_type ofs = npos) const
		{
			return rfind(ptr, ofs, traits::length(ptr));
		}
		
		// look for ch before ofs
		size_type rfind(value_type ch, size_type ofs = npos) const
		{
			return rfind(static_cast<const_pointer>(&ch), ofs, 1);
		}
		
		// look for one of right at or after ofs
		template <typename tstr>size_type find_first_of(tstr const& right, size_type ofs = 0) const
		{
			return find_first_of(right.c_str(), ofs, right.size());
		}
		
		// look for one of [ptr, ptr + count) at or after ofs
		size_type find_first_of(const_pointer ptr, size_type ofs, size_type count) const
		{
			// room for match, look for it
			if (0 < count && ofs < size())
			{
				const_pointer v = m_ptr + size();
				for (const_pointer u = m_ptr + ofs; u < v; ++u)
					if (traits::find(ptr, count, *u) != 0)
						return u - m_ptr;	// found a match
			}
			return npos; // no match
		}
		
		// look for one of [ptr, <null>) at or after ofs
		size_type find_first_of(const_pointer ptr, size_type ofs = 0) const
		{
			return find_first_of(ptr, ofs, traits::length(ptr));
		}
		
		// look for ch at or after ofs
		size_type find_first_of(value_type ch, size_type ofs = 0) const
		{
			return find(static_cast<const_pointer>(&ch), ofs, 1);
		}
		
		// look for one of right before ofs
		template <typename tstr> size_type find_last_of(tstr const& right, size_type ofs = npos) const
		{
			return find_last_of(right.c_str(), ofs, right.size());
		}
		
		// look for one of [ptr, ptr + count) on or before ofs
		size_type find_last_of(const_pointer ptr, size_type ofs, size_type count) const
		{
			if (0 < count && 0 < size())
			{
				for (const_pointer u = m_ptr + (ofs < size() ? ofs : size() - 1); ; --u)
				{
					if (traits::find(ptr, count, *u) != 0) return u - m_ptr; // found a match
					else if (u == m_ptr)                   break;            // at beginning, no more chance for match
				}
			}
			return npos; // no match
		}
		
		// look for one of [ptr, <null>) before ofs
		size_type find_last_of(const_pointer ptr, size_type ofs = npos) const
		{
			return find_last_of(ptr, ofs, traits::length(ptr));
		}
		
		// look for ch before ofs
		size_type find_last_of(value_type ch, size_type ofs = npos) const
		{
			return rfind(static_cast<const_pointer>(&ch), ofs, 1);
		}
		
		// look for none of right at or after ofs
		template <typename tstr> size_type find_first_not_of(tstr const& right, size_type ofs = 0) const
		{
			return find_first_not_of(right.c_str(), ofs, right.size());
		}
		
		// look for none of [ptr, ptr + count) at or after ofs
		size_type find_first_not_of(const_pointer ptr, size_type ofs, size_type count) const
		{
			// room for match, look for it
			if (ofs < size())
			{	
				const_pointer v = m_ptr + size();
				for (const_pointer u = m_ptr + ofs; u < v; ++u)
					if (traits::find(ptr, count, *u) == 0)
						return u - m_ptr;
			}
			return npos;
		}
		
		// look for one of [ptr, <null>) at or after ofs
		size_type find_first_not_of(const_pointer ptr, size_type ofs = 0) const
		{
			return find_first_not_of(ptr, ofs, traits::length(ptr));
		}
		
		// look for non ch at or after ofs
		size_type find_first_not_of(value_type ch, size_type ofs = 0) const
		{
			return find_first_not_of(static_cast<const_pointer>(&ch), ofs, 1);
		}
		
		// look for none of right before ofs
		template <typename tstr> size_type find_last_not_of(tstr const& right, size_type ofs = npos) const
		{
			return find_last_not_of(right.c_str(), ofs, right.size());
		}
		
		// look for none of [ptr, ptr + count) before ofs
		size_type find_last_not_of(const_pointer ptr, size_type ofs, size_type count) const
		{
			if (0 < size())
			{
				for (const_pointer u = m_ptr + (ofs < size() ? ofs : size() - 1); ; --u)
				{
					if (traits::find(ptr, count, *u) == 0) return u - m_ptr;
					if (u == m_ptr) break;
				}
			}
			return npos;
		}
		
		// look for none of [ptr, <null>) before ofs
		size_type find_last_not_of(const_pointer ptr, size_type ofs = npos) const
		{
			return find_last_not_of(ptr, ofs, traits::length(ptr));
		}
		
		// look for non ch before ofs
		size_type find_last_not_of(value_type ch, size_type ofs = npos) const
		{
			return find_last_not_of(static_cast<const_pointer>(&ch), ofs, 1);
		}
		
		// return [ofs, ofs + count) as new string
		string substr(size_type ofs = 0, size_type count = npos) const
		{
			return string(*this, ofs, count);
		}
		
		// allow cast to std::string/std::wstring
		operator std::basic_string<Type>() const
		{
			return std::basic_string<Type>(begin(), end());
		}
	};
	
	// string concatenation
	template <typename T, int L0, bool F0, typename A0, int L1, bool F1, typename A1>
	inline string<T,L0,F0,A0> operator + (string<T,L0,F0,A0> const& lhs, string<T,L1,F1,A1> const& rhs)
	{
		string<T,L0,F0,A0> res;
		res.reserve(lhs.size() + rhs.size());
		res += lhs;
		res += rhs;
		return res;
	}
	template <typename T, int L, bool F, typename A>
	inline string<T,L,F,A> operator + (T const* lhs, string<T,L,F,A> const& rhs)
	{
		string<T,L,F,A> res;
		res.reserve(string<T,L,F,A>::traits::length(lhs) + rhs.size());
		res += lhs;
		res += rhs;
		return res;
	}
	template <typename T, int L, bool F, typename A>
	inline string<T,L,F,A> operator + (string<T,L,F,A> const& lhs, T const* rhs)
	{
		string<T,L,F,A> res;
		res.reserve(lhs.size() + string<T,L,F,A>::traits::length(rhs));
		res += lhs;
		res += rhs;
		return res;
	}
	template <typename T, int L, bool F, typename A>
	inline string<T,L,F,A> operator + (T lhs, string<T,L,F,A> const& rhs)
	{
		string<T,L,F,A> res;
		res.reserve(1 + rhs.size());
		res += lhs;
		res += rhs;
		return res;
	}
	template <typename T, int L, bool F, typename A>
	inline string<T,L,F,A> operator + (string<T,L,F,A> const& lhs, T rhs)
	{
		string<T,L,F,A> res;
		res.reserve(lhs.size() + 1);
		res += lhs;
		res += rhs;
		return res;
	}
	#if _MSC_VER >= 1600
	template <typename T, int L0, bool F0, typename A0, int L1, bool F1, typename A1>
	inline string<T,L0,F0,A0> operator + (string<T,L0,F0,A0>&& lhs, string<T,L1,F1,A1>&& rhs)
	{
		// return string + string
		if (rhs.size() <= lhs.capacity() - lhs.size() || rhs.capacity() - rhs.size() < lhs.size())
			return std::move(lhs.append(rhs));
		else
			return std::move(rhs.insert(0, lhs));
	}
	template <typename T, int L0, bool F0, typename A0, int L1, bool F1, typename A1> inline string<T,L0,F0,A0> operator + (string<T,L0,F0,A0> const& lhs, string<T,L1,F1,A1>&& rhs) { return std::move(rhs.insert(0, lhs)); }
	template <typename T, int L0, bool F0, typename A0, int L1, bool F1, typename A1> inline string<T,L0,F0,A0> operator + (string<T,L0,F0,A0>&& lhs, string<T,L1,F1,A1> const& rhs) { return std::move(lhs.append(rhs)); }
	template <typename T, int L, bool F, typename A> inline string<T,L,F,A> operator + (T const* lhs, string<T,L,F,A>&& rhs) { return std::move(rhs.insert(0, lhs)); }
	template <typename T, int L, bool F, typename A> inline string<T,L,F,A> operator + (string<T,L,F,A>&& lhs, T const* rhs) { return std::move(lhs.append(rhs)); }
	template <typename T, int L, bool F, typename A> inline string<T,L,F,A> operator + (T lhs, string<T,L,F,A>&& rhs)        { return std::move(rhs.insert(0, 1, lhs)); }
	template <typename T, int L, bool F, typename A> inline string<T,L,F,A> operator + (string<T,L,F,A>&& lhs, T rhs)        { return std::move(lhs.append(1, rhs)); }
	#endif
	
	// equality operators
	template <typename T, int L0, bool F0, typename A0, int L1, bool F1, typename A1> inline bool operator == (string<T,L0,F0,A0> const& lhs, string<T,L1,F1,A1> const& rhs) { return lhs.compare(rhs) == 0; }
	template <typename T, int L0, bool F0, typename A0, int L1, bool F1, typename A1> inline bool operator != (string<T,L0,F0,A0> const& lhs, string<T,L1,F1,A1> const& rhs) { return !(lhs == rhs); }
	template <typename T, int L, bool F, typename A>                                  inline bool operator == (T const* lhs, string<T,L,F,A> const& rhs)                     { return rhs.compare(lhs) == 0; }
	template <typename T, int L, bool F, typename A>                                  inline bool operator != (T const* lhs, string<T,L,F,A> const& rhs)                     { return !(lhs == rhs); }
	template <typename T, int L, bool F, typename A>                                  inline bool operator == (string<T,L,F,A> const& lhs, T const* rhs)                     { return lhs.compare(rhs) == 0; }
	template <typename T, int L, bool F, typename A>                                  inline bool operator != (string<T,L,F,A> const& lhs, T const* rhs)                     { return !(lhs == rhs); }
	template <typename T, int L0, bool F0, typename A0, int L1, bool F1, typename A1> inline bool operator <  (string<T,L0,F0,A0> const& lhs, string<T,L1,F1,A1> const& rhs) { return lhs.compare(rhs) <  0; }
	template <typename T, int L, bool F, typename A>                                  inline bool operator <  (T const* lhs, string<T,L,F,A> const& rhs)                     { return rhs.compare(lhs) >  0; }
	template <typename T, int L, bool F, typename A>                                  inline bool operator <  (string<T,L,F,A> const& lhs, T const* rhs)                     { return lhs.compare(rhs) <  0; }
	template <typename T, int L0, bool F0, typename A0, int L1, bool F1, typename A1> inline bool operator >  (string<T,L0,F0,A0> const& lhs, string<T,L1,F1,A1> const& rhs) { return rhs < lhs; }
	template <typename T, int L, bool F, typename A>                                  inline bool operator >  (T const* lhs, string<T,L,F,A> const& rhs)                     { return rhs < lhs; }
	template <typename T, int L, bool F, typename A>                                  inline bool operator >  (string<T,L,F,A> const& lhs, T const* rhs)                     { return rhs < lhs; }
	template <typename T, int L0, bool F0, typename A0, int L1, bool F1, typename A1> inline bool operator <= (string<T,L0,F0,A0> const& lhs, string<T,L1,F1,A1> const& rhs) { return !(rhs < lhs); }
	template <typename T, int L, bool F, typename A>                                  inline bool operator <= (T const* lhs, string<T,L,F,A> const& rhs)                     { return !(rhs < lhs); }
	template <typename T, int L, bool F, typename A>                                  inline bool operator <= (string<T,L,F,A> const& lhs, T const* rhs)                     { return !(rhs < lhs); }
	template <typename T, int L0, bool F0, typename A0, int L1, bool F1, typename A1> inline bool operator >= (string<T,L0,F0,A0> const& lhs, string<T,L1,F1,A1> const& rhs) { return !(lhs < rhs); }
	template <typename T, int L, bool F, typename A>                                  inline bool operator >= (T const* lhs, string<T,L,F,A> const& rhs)                     { return !(lhs < rhs); }
	template <typename T, int L, bool F, typename A>                                  inline bool operator >= (string<T,L,F,A> const& lhs, T const* rhs)                     { return !(lhs < rhs); }

	// streaming operators
	template <typename T, class Traits, int L, bool F, typename A>
	inline std::basic_ostream<T,Traits>& operator << (std::basic_ostream<T,Traits>& ostrm, string<T,L,L,A> const& str)
	{
		return ostrm << str.c_str();
	}
	template <typename T, class Traits, int L, bool F, typename A>
	inline std::basic_istream<T, Traits>& operator >> (std::basic_istream<T,Traits>& istrm, string<T,L,F,A>& str)
	{
		std::basic_string<T,Traits> s;
		istrm >> s; str.append(s);
		return istrm;
	}
}

#ifdef PR_ASSERT_DEFINED
#   undef PR_ASSERT_DEFINED
#   undef PR_ASSERT
#endif

#endif
