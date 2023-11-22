//******************************************
// pr::string<>
//  Copyright (c) Rylogic Ltd 2008
//******************************************
// A version of std::string with configurable local caching
// Note: this file is made to match pr::vector as much as possible
#pragma once

// DEPRECATED - Stop using this, it's got bugs, it's basically just pr::vector<>
//   it doesn't handle string encoding, I almost never use the LocalCount property

// <type_traits> was introduced in sp1
#if defined(_MSC_FULL_VER) && _MSC_FULL_VER < 150030729
#error VS2008 SP1 or greater is required to build this file
#endif

#pragma intrinsic(memcmp, memcpy, memset, strcmp)
#include <memory>
#include <string>
#include <sstream>
#include <stdexcept>
#include <algorithm>
#include <type_traits>
#include <utility>
#include <cwchar>
#include <cassert>
#include "pr/str/string_core.h"

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
	// Not intended to be a complete replacement, just a 90% substitute
	// Allocator = the type to do the allocation/deallocation. *Can be a pointer to an std::allocator like object*
	// Note about null termination:
	//	'm_count' is the length of the string including the null terminator
	//	therefore it's value is always >= 1
	// Note about LocalCount:
	//	Default local count is chosen to make sizeof(pr::string<>) == 256
	template <typename Type=char, int LocalCount=244, bool Fixed=false, typename Allocator=std::allocator<Type>>
	class string
	{
	public:
		using type = string<Type, LocalCount, Fixed, Allocator>;
		using value_type = Type;
		using size_type = std::size_t;
		using difference_type = std::ptrdiff_t;
		using allocator_type = Allocator;
		using const_reference = Type const&;
		using const_iterator = Type const*;
		using const_pointer = Type const*;
		using reference = Type&;
		using iterator = Type*;
		using pointer = Type*;
		using AllocType = std::remove_pointer_t<Allocator>; // The type of the allocator ignoring pointers

		enum
		{
			LocalLength      = LocalCount,
			LocalSizeInBytes = LocalCount * sizeof(value_type),
			TypeIsPod        = std::is_trivially_copyable_v<Type>,
			TypeAlignment    = std::alignment_of_v<Type>,
		};

		// End of string index position
		static const size_type npos = static_cast<size_type>(-1);

		#pragma region Traits

		// type specific traits
		struct traits :pr::char_traits<Type>
		{
			#if 0
			static void assign(value_type* dst, size_type count, value_type ch)
			{
				if constexpr (sizeof(value_type) == sizeof(char))
					memset(dst, ch, count);
				else if constexpr (std::is_same_v<value_type, wchar_t>)
					wmemset(dst, ch, count);
				else
					for (; count-- != 0; ) *dst++ = ch;
			}
			static void move(value_type* dst, value_type const* src, size_type count)
			{
				memmove(dst, src, count * sizeof(value_type));
			}
			static void copy(value_type* dst, value_type const* src, size_type count)
			{
				memcpy(dst, src, count * sizeof(value_type));
			}
			static value_type const* find(value_type const* ptr, size_type count, value_type ch)
			{
				for (; count-- != 0 && *ptr != 0 && *ptr != ch; ++ptr) {}
				return ptr;
			}
			static bool equal(value_type const* lhs, value_type const* rhs, size_type count)
			{
				return memcmp(lhs, rhs, count * sizeof(value_type)) == 0;
			}
			static bool compare(value_type const* lhs, value_type const* rhs, size_type count)
			{
				if constexpr (std::is_same_v<char, value_type>)
					return strncmp(lhs, rhs, count);
				else if constexpr (std::is_same_v<wchar_t, value_type>)
					return wcsncmp(lhs, rhs, count);
				else
					static_assert(false, "not implemented");
			}
			#endif
		};

		// true if 'tchar' is the same as 'Type', ignoring references
		template <typename tchar> using enable_if_same_char = std::enable_if_t<std::is_same_v<std::decay_t<tchar>,Type>>;

		// true if 'tstr' is a 'pr::string<Type,...>' string
		template <typename tstr> struct is_pr_str
		{
			static tstr* str() { return nullptr; }
			template <int L, bool F, typename A> static std::true_type  check(string<Type, L, F, A>*);
			template <int = 0>                   static std::false_type check(...);
			using type = decltype(check(str()));
			static bool const value = type::value;
		};
		static_assert( is_pr_str<string<Type, 1, true>>::value);
		static_assert(!is_pr_str<std::basic_string<Type>>::value);
		static_assert(!is_pr_str<Type[]>::value);
		static_assert(!is_pr_str<Type*>::value);

		// true if 'tstr' is a 'std::basic_string<Type>'
		template <typename tstr> using is_std_str = typename std::conditional_t<std::is_same_v<tstr, std::basic_string<Type>>, std::true_type, std::false_type>;
		static_assert( is_std_str<std::basic_string<Type>>::value);
		static_assert(!is_std_str<string<Type, 1, true>>::value);
		static_assert(!is_std_str<Type[]>::value);
		static_assert(!is_std_str<Type*>::value);
		
		// true if 'tarr' has array-like semantics and an element type of 'Type' but is not a pointer
		template <typename tarr> using is_char_array = typename std::conditional_t<std::is_same_v<tarr, typename std::basic_string<Type>> || is_pr_str<tarr>::value, std::true_type, std::false_type>;
		static_assert( is_char_array<std::basic_string<Type>>::value);
		static_assert( is_char_array<string<Type, 1, true>>::value);
		static_assert(!is_char_array<Type[]>::value);
		static_assert(!is_char_array<Type*>::value);
		static_assert(!is_char_array<std::vector<int>>::value);
		static_assert(!is_char_array<int[]>::value);
		static_assert(!is_char_array<int*>::value);

		// true if 'tptr' is a 'Type' iterator
		template <typename tptr> using is_char_pointer = std::conditional_t<std::is_same_v<typename std::iterator_traits<tptr>::value_type, Type>, std::true_type, std::false_type>;
		static_assert( is_char_pointer<Type const*>::value);
		static_assert( is_char_pointer<Type*>::value);
		static_assert(!is_char_pointer<int*>::value);
		static_assert( is_char_pointer<decltype(begin(std::vector<Type>()))>::value);
		static_assert(!is_char_pointer<decltype(begin(std::vector<int>()))>::value);

		// Enable if 'tarr' is an array-like container of 'Type'
		template <typename tarr> using enable_if_char_array = typename std::enable_if<is_char_array<tarr>::value>::type;

		// Enable if 'tptr' is a pointer or iterator to and contiguous buffer of 'Type'
		template <typename tptr> using enable_if_char_iter = typename std::enable_if<is_char_pointer<tptr>::value>::type;

		#pragma endregion

	private:

		// Use aligned storage so that we don't construct 'Type's in the local array.
		using TLocalStore = struct alignas(std::alignment_of_v<Type>) { std::byte _[sizeof(Type)]; };

		Type*       m_ptr;                // Pointer to the array of data
		TLocalStore m_local[LocalLength]; // Local cache for small arrays
		size_type   m_capacity;           // The reserved space for elements. m_capacity * sizeof(Type) = size in bytes pointed to by m_ptr.
		size_type   m_count;              // The number of used elements in the array + 1 for the null term
		Allocator   m_allocator;          // The memory allocator

		// Any combination of type, local count, fixed, and allocator is a friend
		template <class T, int L, bool F, class A> friend class string;

		// Access to the allocator object (independent over whether its a pointer or instance)
		// (enable_if requires type inference to work, hence the 'A' template parameter)
		template <typename A> typename std::enable_if_t<!std::is_pointer_v<A>, AllocType const&> alloc(A) const { return  m_allocator; }
		template <typename A> typename std::enable_if_t< std::is_pointer_v<A>, AllocType const&> alloc(A) const { return *m_allocator; }
		template <typename A> typename std::enable_if_t<!std::is_pointer_v<A>, AllocType&      > alloc(A)       { return  m_allocator; }
		template <typename A> typename std::enable_if_t< std::is_pointer_v<A>, AllocType&      > alloc(A)       { return *m_allocator; }

		// return true if 'ptr' points with the current container
		bool inside(const_pointer ptr) const { return m_ptr <= ptr && ptr < m_ptr + m_count; }

		// return a pointer to the local buffer
		Type const* local_ptr() const { return reinterpret_cast<Type const*>(&m_local[0]); }
		Type*       local_ptr()       { return reinterpret_cast<Type*>      (&m_local[0]); }

		// return true if 'm_ptr' points to our local buffer
		bool local() const
		{
			return m_ptr == local_ptr();
		}

		// return true if 'arr' is actually this object
		template <typename tarr> bool isthis(tarr const& arr) const
		{
			return static_cast<void const*>(this) == static_cast<void const*>(&arr);
		}

		// reverse a range of elements
		void reverse(Type* first, Type* last)
		{
			for (; first != last && first != --last; ++first)
				std::swap(*first, *last);
		}

		// return the iterator category for 'iter'
		template <class iter> typename std::iterator_traits<iter>::iterator_category iter_cat(iter const&) const
		{
			typename std::iterator_traits<iter>::iterator_category cat;
			return cat;
		}

		// Make sure 'm_ptr' is big enough to hold 'new_count' elements
		// 'new_count' should equal 'size() + 1' to include the null term.
		void ensure_space(size_type new_count, bool autogrow)
		{
			struct ConstExpr { static bool Sink(bool b) {return b;} };
			if (!ConstExpr::Sink(Fixed))
			{
				assert(m_capacity >= LocalLength);
				if (new_count <= m_capacity) return;

				// Allocate 50% more space
				size_type bigger, new_cap = new_count;
				if (autogrow && new_cap < (bigger = m_count*3/2)) { new_cap = bigger; }
				assert(autogrow || new_count >= m_count && "don't use ensure_space to trim the allocated memory");
				value_type* new_array = alloc().allocate(new_cap);

				// Copy elements from the old array to the new array
				traits::copy(new_array, m_ptr, m_count);

				// Destruct the old array
				if (!local()) alloc().deallocate(m_ptr, m_capacity);

				m_ptr = new_array;
				m_capacity = new_cap;
				assert(m_capacity >= LocalLength);
			}
			else
			{
				assert(new_count <= m_capacity && "non-allocating container capacity exceeded");
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
		string(size_type count, value_type ch)
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
			:string(std::basic_string_view<Type>(ptr))
		{}

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
		template <int L, bool F, class A>
		string(string<Type,L,F,A> const& right)
			:m_ptr(local_ptr())
			,m_capacity(LocalLength)
			,m_count(1)
			,m_allocator(right.m_allocator)
		{
			m_ptr[0];
			assign(right);
		}

		// construct from [first, last), with allocator
		template <class iter, typename = enable_if_char_iter<iter>>
		string(iter first, iter last, Allocator const& allocator = Allocator())
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
		template <typename tarr, typename = enable_if_char_array<tarr>>
		string(tarr const& right, Allocator const& allocator = Allocator())
			:m_ptr(local_ptr())
			,m_capacity(LocalLength)
			,m_count(1)
			,m_allocator(allocator)
		{
			m_ptr[0] = 0;
			assign(right);
		}

		// move construct
		string(string&& right) PR_NOEXCEPT
			:m_ptr(local_ptr())
			,m_capacity(LocalLength)
			,m_count(1)
			,m_allocator(right.m_allocator)
		{
			m_ptr[0] = 0;
			assign(std::forward<string>(right));
		}
		template <int L, bool F, class A>
		string(string<Type,L,F,A>&& right) PR_NOEXCEPT
			:m_ptr(local_ptr())
			,m_capacity(LocalLength)
			,m_count(1)
			,m_allocator(right.m_allocator)
		{
			m_ptr[0] = 0;
			assign(std::forward< string<Type,L,F,A> >(right));
		}

		// construct from right [rofs, rofs + count)
		template <typename tarr, typename = enable_if_char_array<tarr>>
		string(tarr const& right, size_type rofs, size_type count)
			:m_ptr(local_ptr())
			,m_capacity(LocalLength)
			,m_count(1)
			,m_allocator()
		{
			m_ptr[0] = 0;
			assign(right, rofs, count);
		}

		// construct from string_view
		string(std::basic_string_view<Type> right, Allocator const& allocator = Allocator())
			:string(right.data(), right.data() + right.size(), allocator)
		{}

		// construct from string_view subrange
		string(std::basic_string_view<Type> right, size_type ofs, size_type count, Allocator const& allocator = Allocator())
			:string(right.substr(ofs, count), allocator)
		{}

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
			assert(m_count + 1 <= m_capacity && "Container overflow");
			m_ptr[size()] = value;
			m_count++;
			m_ptr[size()] = 0;
		}

		// Deletes the element at the end of the array.
		void pop_back()
		{
			assert(!empty());
			--m_count;
		}

		// The last character in the string (or the terminator for empty strings)
		value_type last() const
		{
			return empty() ? value_type() : *(end() - 1);
		}

		// return pointer to non-mutable array
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
			assert(pos < size() && "out of range");
			return m_ptr[pos];
		}
		reference at(size_type pos)
		{
			assert(pos < size() && "out of range");
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
			assert(new_cap >= size() && "reserve amount less than current size");
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
				traits::assign(m_ptr + m_count, newsize - m_count, ch);
			}
			m_count = newsize + 1;
			m_ptr[size()] = 0;
		}

		// Index operator
		const_reference operator [](size_type i) const
		{
			assert(i < m_count && "out of range");
			return m_ptr[i];
		}
		reference operator [](size_type i)
		{
			assert(i < m_count && "out of range");
			return m_ptr[i];
		}

		// assign right (explicit assignment operator needed to prevent auto generated one)
		string& operator = (string const& right)
		{
			assign(right);
			return *this;
		}

		// assign right from any pr::string<Type,...>
		template <int L,bool F,typename A>
		string& operator = (string<Type,L,F,A> const& right)
		{
			assign(right);
			return *this;
		}

		// assign right
		template <typename tchar, int Len, typename = enable_if_same_char<tchar>>
		string& operator = (tchar const (&right)[Len])
		{
			// using 'tchar' instead of Type so that the assign<tarr>(..) overload isn't choosen
			assign(right);
			return *this;
		}

		// assign right
		template <typename tarr, typename = enable_if_char_array<tarr>>
		string& operator = (tarr const& right)
		{
			assign(right);
			return *this;
		}

		// move right
		string& operator = (string&& right) PR_NOEXCEPT
		{
			assign(std::forward<string>(right));
			return *this;
		}

		// move right from any pr::string<>
		template <int L,bool F,typename A>
		string& operator =(string<Type,L,F,A>&& rhs) PR_NOEXCEPT
		{
			assign(std::move(rhs));
			return *this;
		}

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
		template <typename tarr, typename = enable_if_char_array<tarr>>
		string& operator += (tarr const& right)
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
			traits::assign(m_ptr, count, ch);
			m_count = count + 1;
			m_ptr[size()] = 0;
			return *this;
		}

		// assign [ptr, ptr + count)
		string& assign(const_pointer ptr, size_type count)
		{
			// assigning from a substring
			if (inside(ptr))
				return assign(*this, size_type(ptr - m_ptr), count);

			// copy the string
			ensure_space(count + 1, true);
			traits::copy(m_ptr, ptr, count);
			m_count = count + 1;
			m_ptr[size()] = 0;
			return *this;
		}

		// assign from std::basic_string_view
		string& assign(std::basic_string_view<Type> sv)
		{
			if (sv.data() != nullptr)
				return assign(sv.data(), sv.size());

			resize(0);
			return *this;
		}

		// assign [first, last), const pointers
		string& assign(const_pointer first, const_pointer last)
		{
			return replace(begin(), end(), first, last);
		}

		// assign [first, last), iterators
		template <typename iter, typename = enable_if_char_iter<iter>>
		string& assign(iter first, iter last)
		{
			return replace(begin(), end(), first, last);
		}

		// assign right [rofs, rofs + count)
		template <typename tarr, typename = enable_if_char_array<tarr>>
		string& assign(tarr const& right, size_type rofs, size_type count)
		{
			assert(rofs <= string_traits<tarr>::size(right));

			size_type num = string_traits<tarr>::size(right) - rofs;
			if (count > num) count = num;
			if (isthis(right)) // sub-range
			{
				erase(size_type(rofs + count));
				erase(0, rofs);
			}
			else // make room and assign new stuff
			{
				ensure_space(count + 1, true);
				traits::copy(m_ptr, &right[0] + rofs, count);
				m_count = count + 1;
				m_ptr[count] = 0;
			}
			return *this;
		}

		// assign right
		template <typename tarr, typename = enable_if_char_array<tarr>>
		string& assign(tarr const& right)
		{
			return assign(right, 0, npos);
		}

		// assign by moving right
		template <int L,bool F,typename A>
		string& assign(string<Type,L,F,A>&& right) PR_NOEXCEPT
		{
			// Notes:
			// - moving *does* move right.capacity() (same as std::string)

			if (!isthis(right)) // not this object
			{
				// If using different allocators => can't steal
				// If right is locally buffered => can't steal
				// If right's capacity is <= our local buffer size, no point in stealing
				if (!(allocator() == right.allocator()) || right.local() || right.capacity() <= LocalCount)
				{
					// If this was a vector we'd move the elements of 'right', but since we're
					// dealing with POD types, copy is the same thing.
					assign(right); // copy right
					right.resize(0);
				}
				// Right is not locally buffered, and right's buffer exceeds our local buffer size. Steal it.
				else
				{
					// Clean up anything in this container
					resize(0);

					// Steal from 'right'
					m_ptr      = right.m_ptr;
					m_capacity = right.m_capacity;
					m_count    = right.m_count;

					// Set 'right' to empty
					right.m_ptr      = right.local_ptr();
					right.m_capacity = right.LocalLength;
					right.m_count    = 1;
					right.m_ptr[0]   = 0;
				}
			}
			return *this;
		}

		//// assign right
		//template <typename tchar, int Len> string& assign(tchar const (&right)[Len])
		//{
		//	// using 'tchar' instead of Type so that the assign<tarr>(..) overload isn't choosen
		//	// Don't use 'Len' as the string length, right may be null terminated before 'Len'
		//	return assign(&right[0]);
		//}

		// append right [rofs, rofs + count)
		template <typename tarr, typename = enable_if_char_array<tarr>>
		string& append(tarr const& right, size_type rofs, size_type count)
		{
			assert(rofs <= string_traits<tarr>::size(right));

			size_type num = string_traits<tarr>::size(right) - rofs;
			if (num < count) count = num;
			if (count != 0)
			{
				ensure_space(m_count + count, true);
				traits::copy(m_ptr + size(), &right[0] + rofs, count);
				m_count += count;
				m_ptr[size()] = 0;
			}
			return *this;
		}

		// append right
		template <typename tarr, typename = enable_if_char_array<tarr>>
		string& append(tarr const& right)
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
			traits::assign(m_ptr + size(), count, ch);
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
		template <typename iter, typename = enable_if_char_iter<iter>>
		string& append(iter first, iter last)
		{
			difference_type count = std::distance(first, last);
			ensure_space(m_count + count, true);
			for (Type* s = m_ptr + size(); first != last; ++first, ++s) *s = *first;
			m_count += count;
			m_ptr[size()] = 0;
			return *this;
		}

		//// append right
		//template <int Len> string& append(Type const (&right)[Len])
		//{
		//	return append(&right[0], Len);
		//}

		// insert count * ch at ofs
		string& insert(size_type ofs, size_type count, value_type ch)
		{
			assert(ofs <= size() && "");
			ensure_space(m_count + count, true);
			traits::move(m_ptr + ofs + count, m_ptr + ofs, m_count - ofs);
			traits::assign(m_ptr + ofs, count, ch);
			m_count += count;
			m_ptr[size()] = 0;
			return *this;
		}

		// insert right at ofs
		template <typename tarr, typename = enable_if_char_array<tarr>>
		string& insert(size_type ofs, tarr const& right)
		{
			return insert(ofs, right, 0, npos);
		}

		// insert right [rofs, rofs + count) at ofs
		template <typename tarr, typename = enable_if_char_array<tarr>>
		string& insert(size_type ofs, tarr const& right, size_type rofs, size_type count)
		{
			assert(size() >= ofs && right.size() >= rofs); // ofs or rofs off end
			size_type num = right.size() - rofs;
			if (num < count) count = num; // trim count to size
			assert(npos - size() > count); // result too long
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
			assert(ofs < m_count && "offset off the end of this string");
			assert(count <= traits::length(ptr) && "'count' is longer than the null terminated string 'ptr'");
			assert(npos - size() > count && "result too long");

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

		//// insert right at ofs
		//template <int Len> string& insert(size_type ofs, Type const (&right)[Len])
		//{
		//	return insert(ofs, &right[0], Len);
		//}

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

		// compare [ofs, ofs + n0) with [ptr, ptr + count)
		int compare(size_type ofs, size_type n0, const_pointer ptr, size_type count) const
		{
			if (size() - ofs < n0) n0 = size() - ofs;
			size_type ans = traits::compare(m_ptr + ofs, ptr, n0 < count ? n0 : count);
			return (ans != 0 ? int(ans) : n0 < count ? -1 : n0 == count ? 0 : +1);
		}

		// compare [ofs, ofs + n0) with right [rofs, rofs + count)
		template <typename tarr, typename = enable_if_char_array<tarr>>
		int compare(size_type ofs, size_type n0, tarr const& right, size_type rofs, size_type count) const
		{
			auto right_size = string_traits<tarr>::size(right);
			auto right_cstr = string_traits<tarr>::ptr(right);
			assert(rofs <= right_size);

			if (right_size - rofs < count) count = right_size - rofs;
			return compare(ofs, n0, right_cstr + rofs, count);
		}

		// compare [0, size()) with right
		template <typename tarr, typename = enable_if_char_array<tarr>>
		int compare(tarr const& right) const
		{
			return compare(0, size(), right, 0, npos);
		}

		// compare [ofs, ofs + n0) with right
		template <typename tarr, typename = enable_if_char_array<tarr>>
		int compare(size_type ofs, size_type n0, tarr const& right) const
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
		template <typename tarr, typename = enable_if_char_array<tarr>>
		string& replace(size_type ofs, size_type n0, tarr const& right)
		{
			return replace(ofs, n0, right, 0, npos);
		}

		// replace [ofs, ofs + n0) with right [rofs, rofs + count)
		template <typename tarr, typename = enable_if_char_array<tarr>>
		string& replace(size_type ofs, size_type n0, tarr const& right, size_type rofs, size_type count)
		{
			auto right_size = string_traits<tarr>::size(right);
			auto right_cstr = string_traits<tarr>::ptr(right);
			assert(ofs < size() && rofs <= right_size);

			if (size()     - ofs  < n0   ) n0    = size()       - ofs; // trim n0 to size
			if (right_size - rofs < count) count = right_size - rofs;  // trim count to size
			assert(!(npos - count <= size() - n0));                    // result too long
			size_type rcount  = m_count - n0 - ofs;                    // length of preserved tail (incl null)
			ensure_space(m_count + count - n0, true);

			if (!isthis(right)) // no overlap, just move down and copy in new stuff
			{
				traits::move(m_ptr + ofs + count, m_ptr + ofs + n0, rcount);      // empty hole
				traits::copy(m_ptr + ofs, right_cstr + rofs, count);              // fill hole
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
			assert(count == 0 || ptr != 0);
			if (inside(ptr)) return replace(ofs, n0, *this, ptr - m_ptr, count); // substring, replace carefully

			assert(!(size() < ofs));                  // ofs off end
			if (size() - ofs < n0) n0 = size() - ofs; // trim n0 to size
			assert(!(npos - count <= size() - n0));   // result too long
			size_type rcount = m_count - n0 - ofs;    // length of preserved tail (incl null)

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
			assert(ptr != nullptr);
			return replace(ofs, n0, ptr, traits::length(ptr));
		}

		// replace [ofs, ofs + n0) with count * ch
		string& replace(size_type ofs, size_type n0, size_type count, value_type ch)
		{
			assert(!(size() < ofs));                  // ofs off end
			if (size() - ofs < n0) n0 = size() - ofs; // trim n0 to size
			assert(!(npos - count <= size() - n0));   // result too long
			size_type rcount = m_count - n0 - ofs;    // length of preserved tail (incl null)

			if (count < n0) // smaller hole, move tail up
				traits::move(m_ptr + ofs + count, m_ptr + ofs + n0, rcount);

			// make room and rearrange
			if (0 < count || 0 < n0)
			{
				ensure_space(m_count + count - n0, true);
				if (n0 < count) traits::move(m_ptr + ofs + count, m_ptr + ofs + n0, rcount); // move tail down
				traits::assign(m_ptr + ofs, count, ch); // fill hole
				m_count += count - n0;
				m_ptr[size()] = 0;
			}
			return *this;
		}

		// replace [first, last) with right
		template <typename tarr, typename = enable_if_char_array<tarr>>
		string& replace(const_iterator first, const_iterator last, tarr const& right)
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
		template <typename iter, typename = enable_if_char_iter<iter>>
		string& replace(const_iterator first, const_iterator last, iter first2, iter last2)
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

		// look for [ptr, ptr + count) beginning at or after ofs
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
				for (num -= count - 1, v = m_ptr + ofs; (u = traits::find(v, num, *ptr)) != nullptr; num -= u - v + 1, v = u + 1)
					if (traits::compare(u, ptr, count) == 0)
						return u - m_ptr; // found a match
			}
			return npos; // no match
		}

		// lock for [ptr,null) beginning at or after ofs
		size_type find(const_pointer ptr, size_type ofs = 0) const
		{
			assert(ptr != nullptr);
			return find(ptr, ofs, traits::length(ptr));
		}

		//// look for right beginnng at or after ofs
		//// CAREFUL! this method is a bit dangerous because literal strings have a length 1 character
		//// longer than you'd expect. This method has been setup for the common case like this:
		////    find("Me") searches for {'M','e'}
		//// If you do this:
		////    char me[] = {'M','e','\0'};      find(me); <=== the last '\0' character will not be matched.
		////    char me[] = {'M','e','\0','\0'}; find(me); <=== the last '\0' character will not be matched but the first '\0' will.
		//// These are equivalent
		////    char me[] = {'M','e'};  find(me) == find("Me");
		//template <int Len> size_type find(Type const (&right)[Len], size_type ofs = 0) const
		//{
		//	// Note: Len will include the null terminator for literal strings
		//	return find(&right[0], ofs, Len - (right[Len-1] == 0));
		//}

		// look for right beginning at or after ofs
		template <typename tarr, typename = enable_if_char_array<tarr>>
		size_type find(tarr const& right, size_type ofs = 0) const
		{
			auto right_size = string_traits<tarr>::size(right);
			auto right_cstr = string_traits<tarr>::ptr(right);
			return find(right_cstr, ofs, right_size);
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
		template <typename tarr, typename = enable_if_char_array<tarr>>
		size_type rfind(tarr const& right, size_type ofs = npos) const
		{
			auto right_size = string_traits<tarr>::size(right);
			auto right_cstr = string_traits<tarr>::ptr(right);
			return rfind(right_cstr, ofs, right_size);
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
		template <typename tarr, typename = enable_if_char_array<tarr>>
		size_type find_first_of(tarr const& right, size_type ofs = 0) const
		{
			auto right_size = string_traits<tarr>::size(right);
			auto right_cstr = string_traits<tarr>::ptr(right);
			return find_first_of(right_cstr, ofs, right_size);
		}

		// look for one of [ptr, ptr + count) at or after ofs
		size_type find_first_of(const_pointer ptr, size_type ofs, size_type count) const
		{
			// room for match, look for it
			if (0 < count && ofs < size())
			{
				const_pointer v = m_ptr + size();
				for (const_pointer u = m_ptr + ofs; u < v; ++u)
					if (traits::find(ptr, count, *u) != nullptr)
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
		template <typename tarr, typename = enable_if_char_array<tarr>>
		size_type find_last_of(tarr const& right, size_type ofs = npos) const
		{
			auto right_size = string_traits<tarr>::size(right);
			auto right_cstr = string_traits<tarr>::ptr(right);
			return find_last_of(right_cstr, ofs, right_size);
		}

		// look for one of [ptr, ptr + count) on or before ofs
		size_type find_last_of(const_pointer ptr, size_type ofs, size_type count) const
		{
			if (0 < count && 0 < size())
			{
				for (const_pointer u = m_ptr + (ofs < size() ? ofs : size() - 1); ; --u)
				{
					if (traits::find(ptr, count, *u) != nullptr)
						return u - m_ptr; // found a match
					if (u == m_ptr)
						break; // at beginning, no more chance for match
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
		template <typename tarr, typename = enable_if_char_array<tarr>>
		size_type find_first_not_of(tarr const& right, size_type ofs = 0) const
		{
			auto right_size = string_traits<tarr>::size(right);
			auto right_cstr = string_traits<tarr>::ptr(right);
			return find_first_not_of(right_cstr, ofs, right_size);
		}

		// look for none of [ptr, ptr + count) at or after ofs
		size_type find_first_not_of(const_pointer ptr, size_type ofs, size_type count) const
		{
			// room for match, look for it
			if (ofs < size())
			{
				const_pointer v = m_ptr + size();
				for (const_pointer u = m_ptr + ofs; u < v; ++u)
					if (traits::find(ptr, count, *u) == nullptr)
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
		template <typename tarr, typename = enable_if_char_array<tarr>>
		size_type find_last_not_of(tarr const& right, size_type ofs = npos) const
		{
			auto right_size = string_traits<tarr>::size(right);
			auto right_cstr = string_traits<tarr>::ptr(right);
			return find_last_not_of(right_cstr, ofs, right_size);
		}

		// look for none of [ptr, ptr + count) before ofs
		size_type find_last_not_of(const_pointer ptr, size_type ofs, size_type count) const
		{
			if (0 < size())
			{
				for (const_pointer u = m_ptr + (ofs < size() ? ofs : size() - 1); ; --u)
				{
					if (traits::find(ptr, count, *u) == nullptr)
						return u - m_ptr;
					if (u == m_ptr)
						break;
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

		// allow implicit cast to std::string/std::wstring
		operator std::basic_string<Type, std::char_traits<Type>, std::allocator<Type>>() const
		{
			return std::basic_string<Type, std::char_traits<Type>, std::allocator<Type>>(begin(), end());
		}

		// Allow implicit cast to std::basic_string_view
		operator std::basic_string_view<Type>() const
		{
			return std::basic_string_view<Type>(data(), size());
		}

		// equality operators - note: "friends" so the operators don't use ADL
		bool operator == (type const& right) const { return compare(right) == 0; }
		bool operator != (type const& right) const { return compare(right) != 0; }
		bool operator <  (type const& right) const { return compare(right) <  0; }
		bool operator <= (type const& right) const { return compare(right) <= 0; }
		bool operator >= (type const& right) const { return compare(right) >= 0; }
		bool operator >  (type const& right) const { return compare(right) >  0; }
		template <typename tstr> bool operator == (tstr const& right) const { return compare(right) == 0; }
		template <typename tstr> bool operator != (tstr const& right) const { return compare(right) != 0; }
		template <typename tstr> bool operator <  (tstr const& right) const { return compare(right) <  0; }
		template <typename tstr> bool operator <= (tstr const& right) const { return compare(right) <= 0; }
		template <typename tstr> bool operator >= (tstr const& right) const { return compare(right) >= 0; }
		template <typename tstr> bool operator >  (tstr const& right) const { return compare(right) >  0; }
		//template <typename tstr> friend inline bool operator == (tstr const& lhs, type const& rhs) { return rhs == lhs; }
		//template <typename tstr> friend inline bool operator != (tstr const& lhs, type const& rhs) { return rhs != lhs; }
		//template <typename tstr> friend inline bool operator <  (tstr const& lhs, type const& rhs) { return !(rhs >= lhs); }
		//template <typename tstr> friend inline bool operator <= (tstr const& lhs, type const& rhs) { return !(rhs >  lhs); }
		//template <typename tstr> friend inline bool operator >= (tstr const& lhs, type const& rhs) { return !(rhs <  lhs); }
		//template <typename tstr> friend inline bool operator >  (tstr const& lhs, type const& rhs) { return !(rhs <= lhs); }

		// string concatenation
		template <int L, bool F, typename A> friend string operator + (string const& lhs, string<value_type,L,F,A> const& rhs)
		{
			string res;
			res.reserve(lhs.size() + rhs.size());
			res += lhs;
			res += rhs;
			return res;
		}
		template <int L, bool F, typename A> friend string operator + (string const& lhs, string<value_type,L,F,A>&& rhs)
		{
			return std::move(rhs.insert(0, lhs));
		}
		template <int L, bool F, typename A> friend string operator + (string&& lhs, string<value_type,L,F,A> const& rhs)
		{
			return std::move(lhs.append(rhs));
		}
		template <int L, bool F, typename A> friend string operator + (string&& lhs, string<value_type,L,F,A>&& rhs)
		{
			// return string + string
			if (rhs.size() <= lhs.capacity() - lhs.size() || rhs.capacity() - rhs.size() < lhs.size())
				return std::move(lhs.append(rhs));
			else
				return std::move(rhs.insert(0, lhs));
		}
		friend string operator + (value_type const* lhs, string const& rhs)
		{
			string res;
			res.reserve(traits::length(lhs) + rhs.size());
			res += lhs;
			res += rhs;
			return res;
		}
		friend string operator + (string const& lhs, value_type const* rhs)
		{
			string res;
			res.reserve(lhs.size() + traits::length(rhs));
			res += lhs;
			res += rhs;
			return res;
		}
		friend string operator + (value_type lhs, string const& rhs)
		{
			string res;
			res.reserve(1 + rhs.size());
			res += lhs;
			res += rhs;
			return res;
		}
		friend string operator + (string const& lhs, value_type rhs)
		{
			string res;
			res.reserve(lhs.size() + 1);
			res += lhs;
			res += rhs;
			return res;
		}
		friend string operator + (value_type const* lhs, string&& rhs)
		{
			return std::move(rhs.insert(0, lhs));
		}
		friend string operator + (string&& lhs, value_type const* rhs)
		{
			return std::move(lhs.append(rhs));
		}
		friend string operator + (value_type lhs, string&& rhs)
		{
			return std::move(rhs.insert(0, 1, lhs));
		}
		friend string operator + (string&& lhs, value_type rhs)
		{
			return std::move(lhs.append(1, rhs));
		}

		// streaming operators
		//friend std::basic_ostream<value_type>& operator << (std::basic_ostream<value_type>& ostrm, string const& str)
		//{
		//	return ostrm << str.c_str();
		//}
		//friend std::basic_istream<value_type>& operator >> (std::basic_istream<value_type>& istrm, string& str)
		//{
		//	std::basic_string<value_type> s;
		//	istrm >> s; str.append(s);
		//	return istrm;
		//}
		friend std::basic_ostream<char>& operator << (std::basic_ostream<char>& ostrm, string const& str)
		{
			if constexpr (std::is_same_v<char, value_type>)
				return ostrm << str.c_str();
			else
				return ostrm << std::string(Narrow(str));
		}
		friend std::basic_ostream<wchar_t>& operator << (std::basic_ostream<wchar_t>& ostrm, string const& str)
		{
			if constexpr (std::is_same_v<wchar_t, value_type>)
				return ostrm << str.c_str();
			else
				return ostrm << std::wstring(Widen(str));
		}
		friend std::basic_istream<char>& operator >> (std::basic_istream<char>& istrm, string& str)
		{
			std::basic_string<char> s; istrm >> s; 
			if constexpr (std::is_same_v<char, value_type>)
				str.append(s);
			else
				str.append(Widen(s));
			return istrm;
		}
		friend std::basic_istream<wchar_t>& operator >> (std::basic_istream<wchar_t>& istrm, string& str)
		{
			std::basic_string<wchar_t> s; istrm >> s;
			if constexpr (std::is_same_v<wchar_t, value_type>)
				str.append(s);
			else
				str.append(Narrow(s));
			return istrm;
		}
	};

	// string concatenation
	#if 0
	template <typename T, int L0, bool F0, typename A0, int L1, bool F1, typename A1>
	inline string<T,L0,F0,A0> operator + (string<T,L0,F0,A0> const& lhs, string<T,L1,F1,A1> const& rhs)
	{
		using tstr = string<T,L0,F0,A0>;

		tstr res;
		res.reserve(lhs.size() + rhs.size());
		res += lhs;
		res += rhs;
		return res;
	}
	template <typename T, int L, bool F, typename A>
	inline string<T,L,F,A> operator + (T const* lhs, string<T,L,F,A> const& rhs)
	{
		using tstr = string<T,L,F,A>;

		tstr res;
		res.reserve(tstr::traits::length(lhs) + rhs.size());
		res += lhs;
		res += rhs;
		return res;
	}
	template <typename T, int L, bool F, typename A>
	inline string<T,L,F,A> operator + (string<T,L,F,A> const& lhs, T const* rhs)
	{
		using tstr = string<T,L,F,A>;

		tstr res;
		res.reserve(lhs.size() + tstr::traits::length(rhs));
		res += lhs;
		res += rhs;
		return res;
	}
	template <typename T, int L, bool F, typename A>
	inline string<T,L,F,A> operator + (T lhs, string<T,L,F,A> const& rhs)
	{
		using tstr = string<T,L,F,A>;

		tstr res;
		res.reserve(1 + rhs.size());
		res += lhs;
		res += rhs;
		return res;
	}
	template <typename T, int L, bool F, typename A>
	inline string<T,L,F,A> operator + (string<T,L,F,A> const& lhs, T rhs)
	{
		using tstr = string<T,L,F,A>;

		tstr res;
		res.reserve(lhs.size() + 1);
		res += lhs;
		res += rhs;
		return res;
	}
	template <typename T, int L0, bool F0, typename A0, int L1, bool F1, typename A1>
	inline string<T,L0,F0,A0> operator + (string<T,L0,F0,A0>&& lhs, string<T,L1,F1,A1>&& rhs)
	{
		// return string + string
		if (rhs.size() <= lhs.capacity() - lhs.size() || rhs.capacity() - rhs.size() < lhs.size())
			return std::move(lhs.append(rhs));
		else
			return std::move(rhs.insert(0, lhs));
	}
	template <typename T, int L0, bool F0, typename A0, int L1, bool F1, typename A1>
	inline string<T,L0,F0,A0> operator + (string<T,L0,F0,A0> const& lhs, string<T,L1,F1,A1>&& rhs)
	{
		return std::move(rhs.insert(0, lhs));
	}
	template <typename T, int L0, bool F0, typename A0, int L1, bool F1, typename A1>
	inline string<T,L0,F0,A0> operator + (string<T,L0,F0,A0>&& lhs, string<T,L1,F1,A1> const& rhs)
	{
		return std::move(lhs.append(rhs));
	}
	template <typename T, int L, bool F, typename A>
	inline string<T,L,F,A> operator + (T const* lhs, string<T,L,F,A>&& rhs)
	{
		return std::move(rhs.insert(0, lhs));
	}
	template <typename T, int L, bool F, typename A>
	inline string<T,L,F,A> operator + (string<T,L,F,A>&& lhs, T const* rhs)
	{
		return std::move(lhs.append(rhs));
	}
	template <typename T, int L, bool F, typename A>
	inline string<T,L,F,A> operator + (T lhs, string<T,L,F,A>&& rhs)
	{
		return std::move(rhs.insert(0, 1, lhs));
	}
	template <typename T, int L, bool F, typename A>
	inline string<T,L,F,A> operator + (string<T,L,F,A>&& lhs, T rhs)
	{
		return std::move(lhs.append(1, rhs));
	}
	#endif

	// streaming operators
	#if 0
	template <typename T, int L, bool F, typename A> inline std::basic_ostream<T>& operator << (std::basic_ostream<T>& ostrm, string<T,L,F,A> const& str)
	{
		return ostrm << str.c_str();
	}
	template <typename T, int L, bool F, typename A> inline std::basic_istream<T>& operator >> (std::basic_istream<T>& istrm, string<T,L,F,A>& str)
	{
		std::basic_string<T,Traits> s;
		istrm >> s; str.append(s);
		return istrm;
	}
	template <int L, bool F, typename A> inline std::basic_ostream<char>& operator << (std::basic_ostream<char>& ostrm, string<wchar_t,L,F,A> const& str)
	{
		return ostrm << Narrow(str);
	}
	template <int L, bool F, typename A> inline std::basic_ostream<wchar_t>& operator << (std::basic_ostream<wchar_t>& ostrm, string<char,L,F,A> const& str)
	{
		return ostrm << impl::widen<string<wchar_t>>(str.c_str(), str.size());
	}
	template <int L, bool F, typename A> inline std::basic_istream<char>& operator >> (std::basic_istream<char>& istrm, string<wchar_t,L,F,A>& str)
	{
		std::basic_string<char> s;
		istrm >> s; str.append(impl::widen<string<wchar_t>>(s.c_str(), s.size()));
		return istrm;
	}
	template <int L, bool F, typename A> inline std::basic_istream<wchar_t>& operator >> (std::basic_istream<wchar_t>& istrm, string<char,L,F,A>& str)
	{
		std::basic_string<wchar_t> s;
		istrm >> s; str.append(impl::narrow<string<char>>(s.c_str(), s.size()));
		return istrm;
	}
	#endif

	// 'string_traits' specialisation
	template <typename T, int L, bool F, typename A>
	struct string_traits<string<T,L,F,A>> :char_traits<T>
	{
		using value_type = T;
		using string_type = string<T,L,F,A>;
		static bool const null_terminated = true;
		static bool const dynamic_size = true;

		static value_type const* c_str(string_type const& str) { return str.c_str(); }
		static value_type const* ptr(string_type const& str)   { return str.data(); }
		static value_type* ptr(string_type& str)               { return str.data(); }
		static size_t size(string_type const& str)             { return str.size(); }
		static bool empty(string_type const& str)              { return str.empty(); }
		static void resize(string_type& str, size_t n)         { str.resize(n); }
	};
	template <typename T, int L, bool F, typename A>
	struct string_traits<string<T,L,F,A> const> :char_traits<T>
	{
		using value_type = T const;
		using string_type = string<T,L,F,A> const;
		static bool const null_terminated = true;
		static bool const dynamic_size = true;

		static value_type* c_str(string_type& str)     { return str.c_str(); }
		static value_type* ptr(string_type& str)       { return str.data(); }
		static size_t size(string_type const& str)     { return str.size(); }
		static bool empty(string_type const& str)      { return str.empty(); }
		static void resize(string_type& str, size_t n) { static_assert(dependent_false<T>, "Immutable string cannot be resized"); }
	};
	static_assert(std::is_same_v<string_traits<string<char>>::value_type, char>);
	static_assert(std::is_same_v<string_traits<string<wchar_t>>::value_type, wchar_t>);

	// 'is_string' trait
	template <typename T, int L, bool F, typename A>
	struct is_string<string<T,L,F,A>> :is_char<T> {};
	static_assert(is_string_v<string<char>>);
}

// Specialise std:: types for pr::string
namespace std
{
	// Specialise hash functor for pr::string
	template <typename T, int L, bool F, typename A>
	struct hash<pr::string<T,L,F,A>>
	{
		using _Kty = pr::string<T,L,F,A>;

		// hash 'key' to a size_t value by pseudo-randomizing transform
		size_t operator()(_Kty const& key) const
		{
			auto hash = size_t{0};
			const size_t prime = 31;
			for (auto c : key)
				hash = size_t(c) + hash * prime;

			return hash;
		}
	};
}

#ifdef PR_NOEXCEPT_DEFINED
#   undef PR_NOEXCEPT_DEFINED
#   undef PR_NOEXCEPT
#endif

#if PR_UNITTESTS
#include <string>
#include "pr/common/unittests.h"
#include "pr/str/string_core.h"
namespace pr::str
{
	PRUnitTest(StringTests)
	{
		char const*    src = "abcdefghij";
		wchar_t const* wsrc = L"abcdefghij";
		std::string s0 = "std::string";
		std::wstring w0 = L"std::wstring";

		pr::string<> str0;              PR_CHECK(str0.empty(), true);
		pr::string<> str1 = "Test1";    PR_CHECK(str1, "Test1");
		pr::string<> str2 = str1;       PR_CHECK(str2, str1);
		PR_CHECK(str2.c_str() == str1.c_str(), false);

		pr::string<> str3(str1, 2, pr::string<>::npos);
		PR_CHECK(str3.compare("st1"), 0);

		pr::string<> str4 = s0;
		PR_CHECK(str4, pr::string<>(s0));

		pr::string<wchar_t> wstr0 = wsrc;
		PR_CHECK(wstr0.compare(wsrc), 0);

		pr::string<wchar_t> wstr1 = w0;
		PR_CHECK(wstr1.compare(w0), 0);

		pr::string<wchar_t> wstr2 = L"C++ string";
		std::wstring        w2    = L"C++ string";
		PR_CHECK(wstr2 == w2, true);
		wstr2 = L"pr C++ string";
		w2    = L"std C++ string";
		PR_CHECK(wstr2 != w2, true);

		str0.assign(10, 'A');                               PR_CHECK(str0, "AAAAAAAAAA");
		str1.assign(s0);                                    PR_CHECK(str1, "std::string");
		str2.assign("Test2");                               PR_CHECK(str2, "Test2");
		str4.assign(src, src + 6);                          PR_CHECK(str4, "abcdef");
		str4.assign(s0.begin(), s0.begin() + 5);            PR_CHECK(str4, "std::");

		str0.append(str1, 0, 3);                            PR_CHECK(str0  , "AAAAAAAAAAstd");
		str1.append(str2);                                  PR_CHECK(str1  , "std::stringTest2");
		str2.append(3, 'B');                                PR_CHECK(str2  , "Test2BBB");
		str0.append("Hello", 4);                            PR_CHECK(str0  , "AAAAAAAAAAstdHell");
		str0.append("o");                                   PR_CHECK(str0  , "AAAAAAAAAAstdHello");
		str4.append(s0.begin()+7, s0.end());                PR_CHECK(str4  , "std::ring");
		wstr0.append(4, L'x');                              PR_CHECK(wstr0 , L"abcdefghijxxxx");

		str0.insert(2, 3, 'C');                             PR_CHECK(str0, "AACCCAAAAAAAAstdHello");
		str1.insert(str1.begin(), 'D');                     PR_CHECK(str1, "Dstd::stringTest2");
		str2.insert(str2.begin());                          PR_CHECK(str2[0] == 0 && !str2.empty(), true);
		str3.insert(2, pr::string<>("and"));                PR_CHECK(str3, "stand1");

		str0.erase(0, 13);                                  PR_CHECK(str0, "stdHello");
		str2.erase(0, 1);                                   PR_CHECK(str2, "Test2BBB");
		str2.erase(str2.begin()+4);                         PR_CHECK(str2, "TestBBB");
		str2.erase(str2.begin()+4, str2.begin()+7);         PR_CHECK(str2, "Test");
		str2 += "2BBB";

		PR_CHECK(str0.compare(1, 2, "te", 2)                       < 0, true);
		PR_CHECK(str1.compare(1, 5, pr::string<>("Dstd::"), 1, 5) == 0, true);
		PR_CHECK(str2.compare(pr::string<>("Test2BBB"))           == 0, true);
		PR_CHECK(str0.compare(0, 2, pr::string<>("sr"))            > 0, true);
		PR_CHECK(str1.compare("Dstd::string")                      > 0, true);
		PR_CHECK(str2.compare(5, 3, "BBB")                        == 0, true);

		str0.clear();
		PR_CHECK(str0.empty() && str0.capacity() == str0.LocalLength - 1, true);
		PR_CHECK(::size_t(str1.end() - str1.begin()), str1.size());
		str1.resize(0);                                     PR_CHECK(str1.empty(), true);
		str1.push_back('E');                                PR_CHECK(str1.size() == 1 && str1[0] == 'E', true);

		str0 = pr::string<>("Test0");                       PR_CHECK(str0, "Test0");
		str1 = "Test1";                                     PR_CHECK(str1, "Test1");
		str2 = 'F';                                         PR_CHECK(str2, "F");

		str0 += pr::string<>("Pass");                       PR_CHECK(str0, "Test0Pass");
		str1 += "Pass";                                     PR_CHECK(str1, "Test1Pass");
		str2 += 'G';                                        PR_CHECK(str2, "FG");

		str0 = pr::string<>("Jin") + pr::string<>("Jang");  PR_CHECK(str0, "JinJang");
		str1 = pr::string<>("Purple") + "Monkey";           PR_CHECK(str1, "PurpleMonkey");
		str2 = pr::string<>("H") + 'I';                     PR_CHECK(str2, "HI");

		wstr0 = L"A";                                       PR_CHECK(wstr0, L"A");
		wstr0 += L'b';                                      PR_CHECK(wstr0, L"Ab");

		PR_CHECK(pr::string<>("A") == pr::string<>("A") , true);
		PR_CHECK(pr::string<>("A") != pr::string<>("B") , true);
		PR_CHECK(pr::string<>("A")  < pr::string<>("B") , true);
		PR_CHECK(pr::string<>("B")  > pr::string<>("A") , true);
		PR_CHECK(pr::string<>("A") <= pr::string<>("AB"), true);
		PR_CHECK(pr::string<>("B") >= pr::string<>("B") , true);

		PR_CHECK(str0.find("Jang", 1, 4)                                         , 3U);
		PR_CHECK(str0.find(pr::string<>("ang"), 2)                               , 4U);
		PR_CHECK(str0.find_first_of(pr::string<>("n"), 0)                        , 2U);
		PR_CHECK(str0.find_first_of("J", 1, 1)                                   , 3U);
		PR_CHECK(str0.find_first_of("J", 0)                                      , 0U);
		PR_CHECK(str0.find_first_of('n', 3)                                      , 5U);
		PR_CHECK(str0.find_last_of(pr::string<>("n"), pr::string<>::npos)        , 5U);
		PR_CHECK(str0.find_last_of("J", 3, 1)                                    , 3U);
		PR_CHECK(str0.find_last_of("J", pr::string<>::npos)                      , 3U);
		PR_CHECK(str0.find_last_of('a', pr::string<>::npos)                      , 4U);
		PR_CHECK(str0.find_first_not_of(pr::string<>("Jin"), 0)                  , 4U);
		PR_CHECK(str0.find_first_not_of("ing", 1, 3)                             , 3U);
		PR_CHECK(str0.find_first_not_of("inJ", 0)                                , 4U);
		PR_CHECK(str0.find_first_not_of('J', 1)                                  , 1U);
		PR_CHECK(str0.find_last_not_of(pr::string<>("Jang"), pr::string<>::npos) , 1U);
		PR_CHECK(str0.find_last_not_of("Jang", 4, 4)                             , 1U);
		PR_CHECK(str0.find_last_not_of("an", 5)                                  , 3U);
		PR_CHECK(str0.find_last_not_of('n', 5)                                   , 4U);

		PR_CHECK(str1.substr(6, 4), "Monk");

		str0.resize(0);
		for (int i = 0; i != 500; ++i)
		{
			str0.insert(str0.begin() ,'A'+(i%24));
			str0.insert(str0.end()   ,'A'+(i%24));
			PR_CHECK(str0.size(), (1ULL+i) * 2);
		}

		str4 = "abcdef";
		std::string stdstr = str4;     PR_CHECK(pr::str::Equal(stdstr, str4), true);
		stdstr = str3;                 PR_CHECK(pr::str::Equal(stdstr, str3), true);

		std::string str5 = "ABCDEFG";
		str5.replace(1, 3, "bcde", 2);
		PR_CHECK(str5.size(), 6U);

		pr::string<> str6 = "abcdefghij";
		str6.replace(0, 3, pr::string<>("AB"));              PR_CHECK(str6, "ABdefghij");
		str6.replace(3, 3, pr::string<>("DEFGHI"), 1, 3);    PR_CHECK(str6, "ABdEFGhij");
		str6.replace(1, pr::string<>::npos, "bcdefghi", 4);  PR_CHECK(str6, "Abcde");
		str6.replace(1, pr::string<>::npos, "bcdefghi");     PR_CHECK(str6, "Abcdefghi");
		str6.replace(4, 20, 3, 'X');                         PR_CHECK(str6, "AbcdXXX");

		// Test move constructor/assignment
		pr::string<> str7 = "my_string";
		pr::string<> str8 = std::move(str7);
		//PR_CHECK(str7.empty(), true);
		PR_CHECK(str8, "my_string");

		pr::string<char,4> str9 = "very long string that has been allocated";
		pr::string<char,8> str10 = "a different very long string that's been allocated";
		str10 = std::move(str9);
		//PR_CHECK(str9.empty(), true);
		PR_CHECK(str10, "very long string that has been allocated");
		PR_CHECK(str9.c_str() == str10.c_str(), false);

		// implicit cast to string view
		pr::string<char, 16> str11 = "abcdef";
		std::string_view sv = str11;
		PR_CHECK(Equal(sv, "abcdef"), true);
	}
}
#endif
