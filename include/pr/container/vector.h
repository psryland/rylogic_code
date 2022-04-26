//******************************************
// pr::vector<>
//  Copyright (c) Rylogic Ltd 2003
//******************************************
// A version of std::vector with configurable local caching
// and proper type alignment.
//
// Notes:
//  - this file is made to match pr::string<> as much as possible
//  - This class cannot be replaced by a suitably designed allocator because the allocator would
//    have to be a value type for the local buffer. That, however, means when large vectors are copied
//    the heap allocations would have to be reallocated.
#pragma once

#include <memory>
#include <iterator>
#include <stdexcept>
#include <algorithm>
#include <type_traits>
#include <span>
#include <cassert>
#include "pr/common/allocator.h"

#pragma intrinsic(memcmp, memcpy, memset, strcmp)

namespace pr
{
	namespace impl::vector
	{
		template <typename Type> struct citer // const iterator
		{
			using value_type = Type;
			using pointer = Type const*;
			using reference = Type const&;
			using difference_type = typename std::pointer_traits<pointer>::difference_type;
			using iterator_category = std::random_access_iterator_tag;

			pointer m_ptr;

			citer()
			{}
			citer(pointer ptr)
				:m_ptr(ptr)
			{}
			pointer operator ->() const
			{
				return m_ptr;
			}
			reference operator *() const
			{
				return *m_ptr;
			}
			citer& operator ++()
			{
				++m_ptr;
				return *this;
			}
			citer& operator --()
			{
				--m_ptr;
				return *this;
			}
			citer operator ++(int)
			{
				citer i(m_ptr);
				++m_ptr;
				return i;
			}
			citer operator --(int)
			{
				citer i(m_ptr);
				--m_ptr;
				return i;
			}
			reference operator[](difference_type i) const
			{
				return m_ptr[i];
			}

			friend bool operator == (citer<Type> lhs, citer<Type> rhs)
			{
				return lhs.m_ptr == rhs.m_ptr;
			}
			friend bool operator != (citer<Type> lhs, citer<Type> rhs)
			{
				return lhs.m_ptr != rhs.m_ptr;
			}
			friend bool operator <  (citer<Type> lhs, citer<Type> rhs)
			{
				return lhs.m_ptr < rhs.m_ptr;
			}
			friend bool operator <= (citer<Type> lhs, citer<Type> rhs)
			{
				return lhs.m_ptr <= rhs.m_ptr;
			}
			friend bool operator >  (citer<Type> lhs, citer<Type> rhs)
			{
				return lhs.m_ptr > rhs.m_ptr;
			}
			friend bool operator >= (citer<Type> lhs, citer<Type> rhs)
			{
				return lhs.m_ptr >= rhs.m_ptr;
			}

			friend citer<Type>& operator += (citer& lhs, difference_type rhs)
			{
				lhs.m_ptr += rhs;
				return lhs;
			}
			friend citer<Type>& operator -= (citer& lhs, difference_type rhs)
			{
				lhs.m_ptr -= rhs;
				return lhs;
			}
			friend citer<Type> operator + (citer lhs, difference_type rhs)
			{
				return lhs.m_ptr + rhs;
			}
			friend citer<Type> operator - (citer lhs, difference_type rhs)
			{
				return lhs.m_ptr - rhs;
			}
			friend difference_type operator - (citer lhs, citer rhs)
			{
				return lhs.m_ptr - rhs.m_ptr;
			}
		};
		template <typename Type> struct miter // mutable iterator
		{
			using value_type = Type;
			using pointer = Type*;
			using reference = Type&;
			using difference_type = typename std::pointer_traits<pointer>::difference_type;
			using iterator_category = std::random_access_iterator_tag;

			pointer m_ptr;

			miter()
			{}
			miter(pointer ptr)
				:m_ptr(ptr)
			{}
			pointer operator ->() const
			{
				return m_ptr;
			}
			reference operator *() const
			{
				return *m_ptr;
			}
			miter& operator ++()
			{
				++m_ptr;
				return *this;
			}
			miter& operator --()
			{
				--m_ptr;
				return *this;
			}
			miter operator ++(int)
			{
				miter i(m_ptr);
				++m_ptr;
				return i;
			}
			miter operator --(int)
			{
				miter i(m_ptr);
				--m_ptr;
				return i;
			}
			reference operator[](difference_type i) const
			{
				return m_ptr[i];
			}
			operator citer<Type>() const
			{
				return citer<Type>(m_ptr);
			}

			friend bool operator == (miter<Type> lhs, miter<Type> rhs)
			{
				return lhs.m_ptr == rhs.m_ptr;
			}
			friend bool operator != (miter<Type> lhs, miter<Type> rhs)
			{
				return lhs.m_ptr != rhs.m_ptr;
			}
			friend bool operator <  (miter<Type> lhs, miter<Type> rhs)
			{
				return lhs.m_ptr < rhs.m_ptr;
			}
			friend bool operator <= (miter<Type> lhs, miter<Type> rhs)
			{
				return lhs.m_ptr <= rhs.m_ptr;
			}
			friend bool operator >  (miter<Type> lhs, miter<Type> rhs)
			{
				return lhs.m_ptr > rhs.m_ptr;
			}
			friend bool operator >= (miter<Type> lhs, miter<Type> rhs)
			{
				return lhs.m_ptr >= rhs.m_ptr;
			}

			friend miter<Type>& operator += (miter& lhs, difference_type rhs)
			{
				lhs.m_ptr += rhs; return lhs;
			}
			friend miter<Type>& operator -= (miter& lhs, difference_type rhs)
			{
				lhs.m_ptr -= rhs; return lhs;
			}
			friend miter<Type> operator + (miter lhs, difference_type rhs)
			{
				return lhs.m_ptr + rhs;
			}
			friend miter<Type> operator - (miter lhs, difference_type rhs)
			{
				return lhs.m_ptr - rhs;
			}
			friend difference_type operator - (miter lhs, miter rhs)
			{
				return lhs.m_ptr - rhs.m_ptr;
			}
		};
	}

	// Not intended to be a complete replacement, just a 90% substitute
	// Allocator = the type to do the allocation/deallocation. *Can be a pointer to an std::allocator like object*
	template <typename Type, int LocalCount=16, bool Fixed=false, int Alignment = alignof(Type), typename Allocator=aligned_alloc<Type>>
	class vector
	{
	public:
		using type            = vector<Type, LocalCount, Fixed, Alignment, Allocator>;
		using allocator_type  = Allocator;
		using alloc_traits    = std::allocator_traits<std::remove_pointer_t<allocator_type>>;
		using const_iterator  = pr::impl::vector::citer<Type>;
		using iterator        = pr::impl::vector::miter<Type>;
		using value_type      = typename alloc_traits::value_type;
		using pointer         = typename alloc_traits::pointer;
		using const_pointer   = typename alloc_traits::const_pointer;
		using difference_type = typename alloc_traits::difference_type;
		using size_type       = typename alloc_traits::size_type;

		static constexpr bool type_is_pod_v      = std::is_trivially_copyable_v<Type>;
		static constexpr bool type_is_copyable_v = std::is_copy_constructible_v<Type>;
		static constexpr int  type_alignment_v   = Alignment;
		static constexpr int  local_size_v       = LocalCount;

		struct traits
		{
			static void destruct(allocator_type& alloc, Type* first, size_type count)
			{
				if constexpr (type_is_pod_v)
				{
					assert((::memset(first, 0xdd, sizeof(Type) * count), true));
					(void)alloc, first, count;
				}
				else
				{
					for (; count--;)
						alloc_traits::destroy(alloc, first++);
				}
			}
			static void construct(allocator_type& alloc, Type* first, size_type count)
			{
				if constexpr (type_is_pod_v)
				{
					(void)alloc, first, count;
				}
				else
				{
					for (; count--;)
						alloc_traits::construct(alloc, first++);
				}
			}
			static void copy_constr(allocator_type& alloc, Type* first, Type const* src, size_type count)
			{
				if constexpr (type_is_pod_v)
				{
					::memcpy(first, src, sizeof(Type) * count);
					(void)alloc;
				}
				else
				{
					for (; count--;)
						alloc_traits::construct(alloc, first++, *src++);
				}
			}
			static void move_constr(allocator_type& alloc, Type* first, Type* src, size_type count)
			{
				if constexpr (type_is_pod_v)
				{
					::memcpy(first, src, sizeof(Type) * count);
					(void)alloc;
				}
				else
				{
					for (; count--;)
						alloc_traits::construct(alloc, first++, std::move(*src++));
				}
			}
			static void copy_assign(Type* first, Type const* src, size_type count)
			{
				if constexpr (type_is_pod_v)
				{
					::memcpy(first, src, sizeof(Type) * count);
				}
				else
				{
					for (; count--; ++first, ++src)
						*first = *src;
				}
			}
			static void move_assign(Type* first, Type* src, size_type count)
			{
				if constexpr (type_is_pod_v)
				{
					::memcpy(first, src, sizeof(Type) * count);
				}
				else
				{
					for (; count--; ++first, ++src)
						*first = std::move(*src);
				}
			}
			static void move_left(Type* first, Type* src, size_type count)
			{
				assert(first < src);
				if constexpr (type_is_pod_v)
				{
					::memmove(first, src, sizeof(Type) * count);
				}
				else
				{
					for (; count--; ++first, ++src)
						*first = std::move(*src);
				}
			}
			static void move_right(Type* first, Type* src, size_type count)
			{
				assert(first > src);
				if constexpr (type_is_pod_v)
				{
					::memmove(first, src, sizeof(Type) * count);
				}
				else
				{
					for (first += count, src += count; count--;)
						*--first = std::move(*--src);
				}
			}
			static void fill_constr(allocator_type& alloc, Type* first, size_type count, Type const& val)
			{
				for (; count--;)
					copy_constr(alloc, first++, &val, 1);
			}
			static void fill_assign(Type* first, size_type count, Type const& val)
			{
				for (; count--;)
					copy_assign(first++, &val, 1);
			}
			template <class... Args> static void constr(allocator_type& alloc, Type* first, Args&&... args)
			{
				alloc_traits::construct(alloc, first, std::forward<Args>(args)...);
			}
		};

	private:

		using TLocalStore = typename std::aligned_storage_t<sizeof(Type), type_alignment_v>;
		static_assert((std::alignment_of_v<TLocalStore> % type_alignment_v) == 0, "Local storage doesn't have the correct alignment");

		TLocalStore m_local[local_size_v]; // Local cache for small arrays
		union {                            //
		Type      (*m_data)[local_size_v]; // Debugging helper for viewing the data as an array
		Type*       m_ptr;                 // Pointer to the array of data
		};                                 //
		size_type m_capacity;              // The reserved space for elements. m_capacity * sizeof(Type) = size in bytes pointed to by m_ptr.
		size_type m_count;                 // The number of used elements in the array
		allocator_type m_alloc;            // The memory allocator

		// Any combination of type, local count, fixed, alignment, and allocator is a friend
		template <class T, int L, bool F, int A, class C> friend class vector;

		// return true if 'ptr' points with the current container
		bool inside(const_pointer ptr) const
		{
			return m_ptr <= ptr && ptr < m_ptr + m_count;
		}

		// return a pointer to the local buffer
		Type const* local_ptr() const
		{
			return reinterpret_cast<Type const*>(&m_local[0]);
		}
		Type* local_ptr()
		{
			return reinterpret_cast<Type*>(&m_local[0]);
		}

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
		void reverse(Type* beg, Type* end)
		{
			for (; beg != end && beg != --end; ++beg)
				std::swap(*beg, *end);
		}

		//// return the iterator category for 'iter'
		//template <class iter> typename std::iterator_traits<iter>::iterator_category iter_cat(iter const&) const
		//{
		//	typename std::iterator_traits<iter>::iterator_category cat;
		//	return cat;
		//}

		// Make sure 'm_ptr' is big enough to hold 'new_count' elements
		void ensure_space(size_type new_count, [[maybe_unused]] bool autogrow)
		{
			if constexpr (!Fixed)
			{
				assert(m_capacity >= local_size_v);
				if (new_count <= m_capacity)
					return;

				// Allocate 50% more space
				size_type bigger, new_cap = new_count;
				if (autogrow && new_cap < (bigger = m_count * 3 / 2))
					new_cap = bigger;

				assert(autogrow || new_count >= m_count && "don't use ensure_space to trim the allocated memory");
				
				void* mem = alloc_traits::allocate(alloc(), new_cap);
				assert(((static_cast<char*>(mem) - static_cast<char*>(nullptr)) % type_alignment_v) == 0 && "allocated array has incorrect alignment");
				auto new_array = static_cast<Type*>(mem);

				// Move elements from the old array to the new array
				traits::move_constr(alloc(), new_array, m_ptr, m_count);
				traits::destruct(alloc(), m_ptr, m_count);

				// Deallocate the old array if not the local buffer
				if (!local())
					alloc_traits::deallocate(alloc(), m_ptr, m_capacity);

				m_ptr = new_array;
				m_capacity = new_cap;
				assert(m_capacity >= local_size_v);
			}
			else
			{
				assert(new_count <= m_capacity && "non-allocating container capacity exceeded");
				if (new_count > m_capacity)
					throw std::overflow_error("pr::vector<> out of memory");
			}
		}

	public:

		// construct empty collection
		vector()
			:m_ptr(local_ptr())
			,m_capacity(local_size_v)
			,m_count(0)
			,m_alloc()
		{}

		// construct with custom allocator
		explicit vector(allocator_type const& allocator)
			:m_ptr(local_ptr())
			,m_capacity(local_size_v)
			,m_count(0)
			,m_alloc(allocator)
		{}

		// construct from count * Type()
		explicit vector(size_type count)
			:vector()
		{
			ensure_space(count, false);
			traits::fill_constr(alloc(), m_ptr, count, Type());
			m_count = count;
		}

		// construct from initialiser list
		vector(std::initializer_list<Type> list)
			:vector()
		{
			insert(end(), std::begin(list), std::end(list));
		}

		// construct from count * value, with allocator
		vector(size_type count, Type const& value, allocator_type const& allocator = allocator_type())
			:vector(allocator)
		{
			ensure_space(count, false);
			traits::fill_constr(alloc(), m_ptr, count, value);
			m_count = count;
		}

		// copy construct (explicit copy constructor needed to prevent auto generated one even tho there's a template one that would work)
		vector(vector const& right)
			:vector(right.m_alloc)
		{
			impl_assign(right);
		}

		// copy construct from any pr::vector type
		template <int L, bool F, int A, class C> vector(vector<Type,L,F,A,C> const& right)
			:vector(right.m_alloc)
		{
			impl_assign(right);
		}

		// construct from [first, lastt), with optional allocator
		template <class iter> vector(iter first, iter lastt, allocator_type const& allocator = allocator_type())
			:vector(allocator)
		{
			insert(end(), first, lastt);
		}

		// construct from another array-like object. 2nd parameter used to prevent overload issues with vector(count)
		template <class tarr> vector(tarr const& right, typename tarr::size_type = 0, allocator_type const& allocator = allocator_type())
			:vector(allocator)
		{
			insert(end(), std::begin(right), std::end(right));
		}

		// move construct
		vector(vector&& right) noexcept
			:vector(right.m_alloc)
		{
			impl_assign(std::forward<vector>(right));
		}

		// move construct from similar vector
		template <int L, bool F, int A, class C> vector(vector<Type,L,F,A,C>&& right) noexcept
			:vector(right.m_alloc)
		{
			impl_assign(std::forward< vector<Type,L,F,A> >(right));
		}

		// destruct
		~vector()
		{
			clear();
		}

		// Access to the allocator object (independent of whether its a pointer or instance)
		// (enable_if requires type inference to work, hence the 'A' template parameter)
		allocator_type const& get_allocator() const
		{
			if constexpr (std::is_pointer_v<allocator_type>)
				return *m_alloc;
			else
				return m_alloc;
		}
		allocator_type& get_allocator()
		{
			if constexpr (std::is_pointer_v<allocator_type>)
				return *m_alloc;
			else
				return m_alloc;
		}
		allocator_type const& alloc() const
		{
			return get_allocator();
		}
		allocator_type& alloc()
		{
			return get_allocator();
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
		value_type const& front() const
		{
			assert(!empty() && "container empty");
			return m_ptr[0];
		}
		value_type const& back() const
		{
			assert(!empty() && "container empty");
			return m_ptr[m_count - 1];
		}
		value_type& front()
		{
			assert(!empty() && "container empty");
			return m_ptr[0];
		}
		value_type& back()
		{
			assert(!empty() && "container empty");
			return m_ptr[m_count - 1];
		}

		// insert element at end
		void push_back(value_type&& value)
		{
			// insert by moving into element at end, provide strong guarantee
			emplace_back(std::move(value));
		}
		void push_back(value_type const& value)
		{
			if (inside(&value))
			{
				auto idx = &value - &front();
				ensure_space(m_count + 1, true);
				push_back_fast((*this)[idx]);
			}
			else
			{
				ensure_space(m_count + 1, true);
				push_back_fast(value);
			}
		}

		// add an element to the end of the array without "ensure_space" first
		void push_back_fast(value_type const& value)
		{
			static_assert(std::is_copy_constructible<Type>::value, "Cannot copy construct 'Type'");
			assert(m_count + 1 <= m_capacity && "Container overflow");
			//why not? assert(!inside(&value) && "Cannot push_back_fast an item from this container");
			traits::fill_constr(alloc(), m_ptr + m_count, 1, value);
			++m_count;
		}

		// deletes the element at the end of the array.
		void pop_back()
		{
			assert(!empty());
			traits::destruct(alloc(), m_ptr + m_count - 1, 1);
			--m_count;
		}

		// insert by moving into element at end
		template <class... Args>
		void emplace_back(Args&&... args)
		{
			ensure_space(m_count + 1, true);
			traits::constr(alloc(), m_ptr + m_count, std::forward<Args>(args)...);
			++m_count;
		}

		// insert by moving element at pos
		template <class... Args>
		iterator emplace(const_iterator pos, Args&&... args)
		{
			difference_type ofs = pos - begin();
			emplace_back(std::forward<Args>(args)...);
			std::rotate(begin() + ofs, end() - 1, end());
			return begin() + ofs;
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
		int64_t ssize() const
		{
			return static_cast<int64_t>(m_count);
		}

		// return the available length within allocation
		size_type capacity() const
		{
			return m_capacity;
		}

		// return maximum possible length of sequence
		size_type max_size() const
		{
			if constexpr (Fixed)
				return m_capacity;
			else
				return std::numeric_limits<size_type>::max() / sizeof(Type);
		}

		// indexed access
		value_type const& at(size_type pos) const
		{
			assert(pos < size() && "out of range");
			return m_ptr[pos];
		}
		value_type& at(size_type pos)
		{
			assert(pos < size() && "out of range");
			return m_ptr[pos];
		}

		// resize the collection to 0 and free memory
		void clear()
		{
			traits::destruct(alloc(), m_ptr, m_count);
			if (!local()) alloc().deallocate(m_ptr, m_capacity);
			m_ptr      = local_ptr();
			m_capacity = local_size_v;
			m_count    = 0;
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
			if (m_count < newsize)
			{
				ensure_space(newsize, false);
				traits::construct(alloc(), m_ptr + m_count, newsize - m_count);
			}
			else if (m_count > newsize)
			{
				traits::destruct(alloc(), m_ptr + newsize, m_count - newsize);
			}
			m_count = newsize;
		}
		void resize(size_type newsize, Type const& value)
		{
			if (m_count < newsize)
			{
				static_assert(std::is_copy_constructible<Type>::value, "Cannot copy construct 'Type'");
				if (inside(&value))
				{
					auto idx = &value - data();
					ensure_space(newsize, false);
					traits::fill_constr(alloc(), m_ptr + m_count, newsize - m_count, (*this)[idx]);
				}
				else
				{
					ensure_space(newsize, false);
					traits::fill_constr(alloc(), m_ptr + m_count, newsize - m_count, value);
				}
			}
			else if (m_count > newsize)
			{
				traits::destruct(alloc(), m_ptr + newsize, m_count - newsize);
			}
			m_count = newsize;
		}

		// Index operator
		value_type const& operator [](size_type i) const
		{
			assert(i < size() && "out of range");
			return m_ptr[i];
		}
		value_type& operator [](size_type i)
		{
			assert(i < size() && "out of range");
			return m_ptr[i];
		}

		// assign right (explicit assignment operator needed to prevent auto generated one even tho there's a template one that would work)
		vector& operator = (vector const& right)
		{
			impl_assign(right);
			return *this;
		}

		// assign right from any pr::vector<Type,...>
		template <int L, bool F, int A, class C> vector& operator = (vector<Type,L,F,A,C> const& right)
		{
			impl_assign(right);
			return *this;
		}

		// assign right
		template <int Len> vector& operator = (Type const (&right)[Len])
		{
			insert(end(), &right[0], &right[0] + Len);
			return *this;
		}

		// assign right
		template <typename tarr> vector& operator = (tarr const& right)
		{
			#if _MSC_VER >= 1600
			insert(end(), std::begin(right), std::end(right));
			#else
			insert(end(), right.begin(), right.end());
			#endif
			return *this;
		}

		// move right
		vector& operator = (vector&& right) noexcept
		{
			impl_assign(std::forward<vector>(right));
			return *this;
		}

		// move right from any pr::vector<>
		template <int L, bool F, int A, class C> vector& operator = (vector<Type,L,F,A,C>&& right) noexcept
		{
			impl_assign(std::forward< vector<Type,L,F,A> >(right));
			return *this;
		}

		// assign count * value
		void assign(size_type count, Type const& value)
		{
			impl_assign(count, value);
		}

		// assign [first, lastt), iterators
		template <typename iter> void assign(iter first, iter lastt)
		{
			erase(begin(), end());
			insert(end(), first, lastt);
		}

		// insert value at pos
		iterator insert(const_iterator pos, Type const& value)
		{
			return insert(pos, size_type(1), value);
		}

		// insert value at pos
		iterator insert(const_iterator pos, Type&& value)
		{
			assert(begin() <= pos && pos <= end() && "insert position must be within the array");
			assert(!inside(&value) && "Don't move insert a value already in this array");

			size_type ofs = pos - begin();
			if (ofs == m_count)
			{
				emplace_back(std::move(value));
			}
			else
			{
				ensure_space(m_count + 1, true);
				auto ins = m_ptr + ofs;                          // The insert point
				auto end = m_ptr + m_count;                      // The current end of the array
				traits::move_constr(alloc(), end, end-1, 1);     // move construct into the hole at the end
				traits::move_right(ins + 1, ins, end - ins - 1); // move those right of the insert point
				traits::move_assign(ins, &value, 1);             // fill in the hole
				m_count += 1;
			}
			return begin() + ofs;
		}

		// insert count * value at pos
		iterator insert(const_iterator pos, size_type count, Type const& value)
		{
			assert(begin() <= pos && pos <= end() && "insert position must be within the array");
			auto ofs = pos - begin();
			if (count > max_size() - size())
				throw std::overflow_error("pr::vector<> size too large");
			if (count == 0)
				return begin() + ofs;

			// Algorithm:
			//   Grow the allocation
			//   Copy the last min(rem, count) elements to the end.
			//     If 'count' > 'rem' then there is an unconstructed hole between the old end
			//       and 'end-rem' which needs to be filled with copies of 'value'
			//     If 'rem' > 'count' then only some of the remainder can be moved to the added
			//       space, the others need to be moved right.
			//   Fill with 'count' copies of 'value' from 'ofs'

			auto* elem = &value;
			if (inside(&value))
			{
				// The index of 'value' after inserting space
				auto idx = (&value - data());
				idx += int(idx >= ofs) * count;    
				ensure_space(m_count + count, true);
				elem = &m_ptr[idx];
			}
			else
			{
				ensure_space(m_count + count, true);
			}

			auto ins = m_ptr + ofs;                                    // The insert point
			auto end = m_ptr + m_count;                                // The current end of the array
			auto rem = size() - ofs;                                   // The number of remaining elements after 'ofs'
			auto n = rem > count ? count : rem;                        // min(count, rem)
			traits::move_constr(alloc(), end + count - n, end - n, n); // move the last 'n' elements
			traits::fill_constr(alloc(), end, count - n, *elem);       // fill from the current end to the element that was at ofs but has now moved. (count - n is zero if rem > count)
			traits::move_right (ins + count, ins, rem - n);            // move those right of the insert point to butt up with the now moved remainder
			traits::fill_assign(ins, n, *elem);                        // fill in the hole
			m_count += count;
			return begin() + ofs;
		}

		// insert [first, lastt) at pos
		template <typename iter> void insert(const_iterator pos, iter first, iter lastt)
		{
			if (first == lastt)
				return;

			// assert(first <= lastt && "lastt must follow first"); <- Can't assume 'first' and 'lastt' define operators < or -
			assert(begin() <= pos && pos <= end() && "pos must be within the array");
			assert(!inside(&*first) && "Cannot insert a subrange because iterators are invalidated after the allocation grows");

			auto ofs = pos - begin();
			auto old_count = static_cast<ptrdiff_t>(m_count);
			for (; first != lastt; ++first, ++m_count)
			{
				ensure_space(m_count + 1, true);
				traits::fill_constr(alloc(), m_ptr + m_count, 1, *first);
			}

			if (ofs != old_count)
			{
				reverse(m_ptr + ofs, m_ptr + old_count);
				reverse(m_ptr + old_count, m_ptr + m_count);
				reverse(m_ptr + ofs, m_ptr + m_count);
			}
		}

		// erase element at pos
		iterator erase(const_iterator pos)
		{
			return erase(pos, pos + 1);
		}

		// erase [first, lastt)
		iterator erase(const_iterator first, const_iterator lastt)
		{
			assert(first <= lastt && "lastt must follow first");
			assert(begin() <= first && lastt <= end() && "iterator range must be within the array");
			size_type n = lastt - first, ofs = first - begin();
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
			assert(begin() <= pos && pos < end() && "pos must be within the array");
			size_type ofs = pos - begin();
			if (end() - pos > 1) { *(begin() + ofs) = std::move(back()); }
			pop_back();
			return begin() + ofs;
		}

		// erase [first, lastt) without preserving order
		iterator erase_fast(const_iterator first, const_iterator lastt)
		{
			assert(first <= lastt && "lastt must follow first");
			assert(begin() <= first && lastt <= end() && "iterator range must be within the array");
			size_type n = lastt - first, ofs = first - begin();
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
					traits::move_assign(del, m_ptr + m_count - n, n);
					traits::destruct(alloc(), m_ptr + m_count - n, n);
				}
				m_count -= n;
			}
			return begin() + ofs;
		}

		// Requests the removal of unused capacity
		void shrink_to_fit()
		{
			assert(m_capacity >= local_size_v);
			if (m_capacity != local_size_v)
			{
				assert(!local());

				Type* new_array; size_type new_count;
				if (m_count <= local_size_v)
				{
					new_count = local_size_v;
					new_array = local_ptr();
				}
				else
				{
					new_count = m_count;
					new_array = alloc().allocate(new_count);
				}

				// Move elements from the old array to the new array
				traits::move_constr(alloc(), new_array, m_ptr, m_count);

				// Destruct the old array
				traits::destruct(alloc(), m_ptr, m_count);
				if (!local()) alloc().deallocate(m_ptr, m_count);

				m_ptr = new_array;
				m_capacity = new_count;
			}
		}

		// Implementation ****************************************************

		// Assign count * value
		void impl_assign(size_type count, Type const& value)
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

		// Assign right of any pr::vector<>
		template <int L,bool F,int A,class C> void impl_assign(vector<Type,L,F,A,C> const& right)
		{
			// Notes:
			//  - copying does not copy right.capacity() (same as std::vector)

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
		template <int L,bool F,int A,class C> void impl_assign(vector<Type,L,F,A,C>&& right) noexcept
		{
			// Notes:
			// - moving *does* move right.capacity() (same as std::vector)

			if (!isthis(right)) // not this object
			{
				// If using different allocators => can't steal
				// If right is locally buffered => can't steal
				// If right's capacity is <= our local buffer size, no point in stealing
				if (!(alloc() == right.alloc()) || right.local() || right.capacity() <= local_size_v)
				{
					// Move the elements of right
					if (right.size() == 0) // new sequence empty, erase existing sequence
					{
						clear();
					}
					else if (right.size() <= size()) // enough elements, move new and destroy old
					{
						traits::move_assign(m_ptr, right.m_ptr, right.size());
						traits::destruct(alloc(), m_ptr + right.size(), size() - right.size());
					}
					else if (right.size() <= capacity()) // enough room, move and move construct new
					{
						traits::move_assign(m_ptr, right.m_ptr, size());
						traits::move_constr(alloc(), m_ptr + size(), right.m_ptr + size(), right.size() - size());
					}
					else // not enough room, allocate new array and move construct new
					{
						resize(0);
						ensure_space(right.capacity(), false);
						traits::move_constr(alloc(), m_ptr, right.m_ptr, right.size());
					}
					m_count = right.size();
					
					// Resize, don't just set the m_count to 0. The elements of 'right' have had their guts
					// moved into our container but they still need destructing.
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

					// Set right to empty. 'right's elements don't need destructing in this case because we're grabbed them
					right.m_ptr      = right.local_ptr();
					right.m_capacity = right.local_size_v;
					right.m_count    = 0;
				}
			}
		}

		// Implicit conversion to a type that can be constructed from begin/end iterators 
		// This allows cast to std::vector<> among others. 
		// Note: converting to a std::vector<> when 'Type' has an alignment greater than the 
		// default alignment causes a compiler error because of std::vector.resize(). 
		template <typename ArrayType>
			requires std::is_same_v<typename ArrayType::value_type, value_type>
			//&& std::is_constructible_v<ArrayType, std::decay_t<decltype(begin())>, std::decay_t<decltype(end())>>
		operator ArrayType() const
		{
			return ArrayType(begin(), end());
		}

		// Implicit conversion to initialiser list.
		// Note: converting to a std::vector<> when 'Type' has an alignment greater than the
		// default alignment causes a compiler error because of std::vector.resize().
		operator std::initializer_list<Type const>() const
		{
			return std::initializer_list<Type const>(data(), data() + size());
		}

		// Implicit conversion to span.
		operator std::span<Type const>() const
		{
			return std::span<Type const>(data(), size());
		}
		operator std::span<Type>()
		{
			return std::span<Type>(data(), size());
		}

		// Operators
		template <typename T, int L, bool F, int A, class C>
		friend bool operator == (vector const& lhs, vector<T, L, F, A, C> const& rhs)
		{
			if (lhs.size() != rhs.size()) return false;
			auto lptr = std::begin(lhs);
			auto rptr = std::begin(rhs);
			for (auto n = lhs.size(); n-- != 0;)
				if (!(*lptr == *rptr)) return false;

			return true;
		}
		template <typename T, int L, bool F, int A, class C> 
		friend bool operator != (vector const& lhs, vector<T, L, F, A, C> const& rhs)
		{
			return !(lhs == rhs);
		}
	};
}

#if PR_UNITTESTS
#include <algorithm>
#include <random>
#include "pr/common/unittests.h"
#include "pr/common/refcount.h"
#include "pr/common/refptr.h"
#include "pr/maths/maths.h"
namespace pr::container
{
	namespace unittests::vector
	{
		// The number of 'Type' instances in existence
		inline static int object_count = 0;

		// A ref-counted singleton
		struct Single :RefCount<Single>
		{
			static void RefCountZero(RefCount<Single>*) {}
			static Single& instance()
			{
				static Single single;
				return single;
			}
		};

		// A copy constructable type
		struct Type
		{
			int val;
			RefPtr<Single> ptr;

			Type()
				:Type(0)
			{}
			Type(int w)
				:val(w)
				, ptr(&Single::instance(), true)
			{
				++object_count;
			}
			Type(Type&& rhs) noexcept
				:Type()
			{
				std::swap(val, rhs.val);
				std::swap(ptr, rhs.ptr);
			}
			Type(Type const& rhs)
				:val(rhs.val)
				, ptr(rhs.ptr)
			{
				++object_count;
			}
			Type& operator = (Type&& rhs) noexcept
			{
				if (this != &rhs)
				{
					std::swap(val, rhs.val);
					std::swap(ptr, rhs.ptr);
				}
				return *this;
			}
			Type& operator = (Type const& rhs)
			{
				if (this != &rhs)
				{
					val = rhs.val;
					ptr = rhs.ptr;
				}
				return *this;
			}
			virtual ~Type()
			{
				--object_count;
				PR_CHECK(ptr.m_ptr == &Single::instance(), true); // destructing an invalid Type
				val = 0xdddddddd;
			}
			friend bool operator == (Type const& lhs, Type const& rhs)
			{
				return lhs.val == rhs.val;
			}
		};
		static_assert(std::is_move_constructible<Type>::value);
		static_assert(std::is_copy_constructible<Type>::value);
		static_assert(std::is_move_assignable<Type>::value);
		static_assert(std::is_copy_assignable<Type>::value);

		// A move-only type
		struct NonCopyable :Type
		{
			NonCopyable()
				:Type()
			{}
			NonCopyable(int w)
				:Type(w)
			{}
			NonCopyable(NonCopyable&& rhs) noexcept
				:Type(std::move(rhs))
			{}
			NonCopyable& operator = (NonCopyable&& rhs) noexcept
			{
				Type::operator=(std::move(rhs));
				return *this;
			}
			NonCopyable(NonCopyable const&) = delete;
			NonCopyable& operator = (NonCopyable const&) = delete;
		};
		static_assert(std::is_move_constructible<NonCopyable>::value);
		static_assert(!std::is_copy_constructible<NonCopyable>::value);
		static_assert(std::is_move_assignable<NonCopyable>::value);
		static_assert(!std::is_copy_assignable<NonCopyable>::value);

		// Leaked objects checker
		struct Check
		{
			int m_count;
			long m_refs;

			Check()
				:m_count(object_count)
				,m_refs(Single::instance().m_ref_count)
			{}
			~Check()
			{
				PR_CHECK(object_count, m_count);
				PR_CHECK(Single::instance().m_ref_count, m_refs);
			}
		};

		using Array0 = pr::vector<Type, 8, false>;
		using Array1 = pr::vector<Type, 16, true>;
		using Array2 = pr::vector<NonCopyable, 4, false>;
	}
	PRUnitTest(VectorTests)
	{
		using namespace unittests::vector;
		std::vector<Type> ints;
		for (int i = 0; i != 16; ++i)
			ints.push_back(Type(i));

		Check global_chk;
		{// Constructors
			{
				Check chk;
				{
					Array0 arr;
					PR_CHECK(arr.empty(), true);
					PR_CHECK(arr.size(), 0U);
				}
			}{
				Check chk;
				{
					Array1 arr(15);
					PR_CHECK(!arr.empty(), true);
					PR_CHECK(arr.size(), 15U);
				}
			}{
				Check chk;
				{
					Array0 arr(5U, 3);
					PR_CHECK(arr.size(), 5U);
					for (int i = 0; i != 5; ++i)
						PR_CHECK(arr[i].val, 3);
				}
			}{
				Check chk;
				{
					Array0 arr0(5U,3);
					Array1 arr1(arr0);
					PR_CHECK(arr1.size(), arr0.size());
					for (int i = 0; i != int(arr0.size()); ++i)
						PR_CHECK(arr1[i].val, arr0[i].val);
				}
			}{
				Check chk;
				{
					std::vector<Type> vec0(4U, Type(6));
					Array0 arr1(vec0);
					PR_CHECK(arr1.size(), vec0.size());
					for (int i = 0; i != int(vec0.size()); ++i)
						PR_CHECK(arr1[i].val, vec0[i]);
				}
			}
		}
		{//Assign
			Check chk;
			{
				Array0 arr0;
				arr0.assign(3U, 5);
				PR_CHECK(arr0.size(), 3U);
				for (int i = 0; i != 3; ++i)
					PR_CHECK(arr0[i].val, 5);

				Array1 arr1;
				arr1.assign(&ints[0], &ints[8]);
				PR_CHECK(arr1.size(), 8U);
				for (int i = 0; i != 8; ++i)
					PR_CHECK(arr1[i].val, ints[i].val);
			}
		}
		{//Clear
			{
				Check chk;
				{// Basic clear
					Array0 arr0(ints.begin(), ints.end());
					arr0.clear();
					PR_CHECK(arr0.empty(), true);
				}
			}{
				Check chk;
				{// Non-copyable clear
					Array2 arr0;
					for (auto i : ints) arr0.emplace_back(i.val);
					arr0.clear();
					PR_CHECK(arr0.empty(), true);
				}
			}
		}
		{//Erase
			{
				Check chk;
				{// Erase range, stable order
					Array0 arr0(ints.begin(), ints.begin() + 8);
					Array0::const_iterator b = arr0.begin();
					arr0.erase(b + 3, b + 5);
					PR_CHECK(arr0.size(), 6U);
					for (int i = 0; i != 3; ++i) PR_CHECK(arr0[i].val, ints[i]  .val);
					for (int i = 3; i != 6; ++i) PR_CHECK(arr0[i].val, ints[i+2].val);
				}
			}{
				Check chk;
				{// Erase single, stable order
					Array1 arr1(ints.begin(), ints.begin() + 4);
					arr1.erase(arr1.begin() + 2);
					PR_CHECK(arr1.size(), 3U);
					for (int i = 0; i != 2; ++i) PR_CHECK(arr1[i].val, ints[i]  .val);
					for (int i = 2; i != 3; ++i) PR_CHECK(arr1[i].val, ints[i+1].val);
				}
			}{
				Check chk;
				{// Erase single, unstable order
					Array0 arr2(ints.begin(), ints.begin() + 5);
					arr2.erase_fast(arr2.begin() + 2);
					PR_CHECK(arr2.size(), 4U);
					for (int i = 0; i != 2; ++i) PR_CHECK(arr2[i].val, ints[i].val);
					PR_CHECK(arr2[2].val, ints[4].val);
					for (int i = 3; i != 4; ++i) PR_CHECK(arr2[i].val, ints[i].val);
				}
			}{
				Check chk;
				{// Erase non-copyable, stable + unstable order
					Array2 arr0;
					arr0.emplace_back(0);
					arr0.emplace_back(1);
					arr0.emplace_back(2);
					arr0.emplace_back(3);
					arr0.emplace_back(4);

					arr0.erase(std::begin(arr0) + 1);
					PR_CHECK(arr0.size(), 4U);
					PR_CHECK(arr0[0].val, 0);
					PR_CHECK(arr0[1].val, 2);
					PR_CHECK(arr0[2].val, 3);
					PR_CHECK(arr0[3].val, 4);

					arr0.erase_fast(std::begin(arr0) + 1);
					PR_CHECK(arr0.size(), 3U);
					PR_CHECK(arr0[0].val, 0);
					PR_CHECK(arr0[1].val, 4);
					PR_CHECK(arr0[2].val, 3);
				}
			}{
				Check chk;
				{// Erase range non-copyable, stable order
					Array2 arr1;
					arr1.emplace_back(0);
					arr1.emplace_back(1);
					arr1.emplace_back(2);
					arr1.emplace_back(3);
					arr1.emplace_back(4);

					arr1.erase(std::begin(arr1) + 1, std::begin(arr1) + 3);
					PR_CHECK(arr1.size(), 3U);
					PR_CHECK(arr1[0].val, 0);
					PR_CHECK(arr1[1].val, 3);
					PR_CHECK(arr1[2].val, 4);
				}
			}{
				Check chk;
				{// Erase range non-copyable, unstable order
					Array2 arr2;
					arr2.emplace_back(0);
					arr2.emplace_back(1);
					arr2.emplace_back(2);
					arr2.emplace_back(3);
					arr2.emplace_back(4);
					arr2.emplace_back(5);
					arr2.emplace_back(6);

					arr2.erase_fast(std::begin(arr2) + 1, std::begin(arr2) + 3);
					PR_CHECK(arr2.size(), 5U);
					PR_CHECK(arr2[0].val, 0);
					PR_CHECK(arr2[1].val, 5);
					PR_CHECK(arr2[2].val, 6);
					PR_CHECK(arr2[3].val, 3);
					PR_CHECK(arr2[4].val, 4);
				}
			}
		}
		{//Insert
			{
				Check chk;
				{
					Array0 arr0;
					arr0.insert(arr0.end(), 4U, 9);
					PR_CHECK(arr0.size(), 4U);
					for (int i = 0; i != 4; ++i)
						PR_CHECK(arr0[i].val, 9);
				}
			}{
				Check chk;
				{
					Array1 arr1(4U, 6);
					arr1.insert(arr1.begin() + 2, &ints[2], &ints[7]);
					PR_CHECK(arr1.size(), 9U);
					for (int i = 0; i != 2; ++i) PR_CHECK(arr1[i].val, 6);
					for (int i = 2; i != 7; ++i) PR_CHECK(arr1[i].val, ints[i].val);
					for (int i = 7; i != 9; ++i) PR_CHECK(arr1[i].val, 6);
				}
			}{
				Check chk;
				{ // Insert aliased element
					Array1 arr1;
					arr1.push_back(0);
					arr1.push_back(1);
					arr1.push_back(2);
					arr1.insert(arr1.begin() + 1, 3U, arr1[2]);
					PR_CHECK(arr1[0].val, 0);
					PR_CHECK(arr1[1].val, 2);
					PR_CHECK(arr1[2].val, 2);
					PR_CHECK(arr1[3].val, 2);
					PR_CHECK(arr1[4].val, 1);
					PR_CHECK(arr1[5].val, 2);
				}
			}
		}
		{//PushPop
			{
				Check chk;
				{
					Array0 arr;
					arr.insert(arr.begin(), &ints[0], &ints[4]);
					arr.pop_back();
					PR_CHECK(arr.size(), 3U);
					for (int i = 0; i != 3; ++i)
						PR_CHECK(arr[i].val, ints[i].val);
				}
			}{
				Check chk;
				{
					Array1 arr;
					arr.reserve(4);
					for (int i = 0; i != 4; ++i) arr.push_back_fast(i);
					for (int i = 4; i != 9; ++i) arr.push_back(i);
					for (int i = 0; i != 9; ++i)
						PR_CHECK(arr[i].val, ints[i].val);
				}
			}{
				Check chk;
				{
					Array1 arr;
					arr.insert(arr.begin(), &ints[0], &ints[4]);
					arr.resize(3);
					PR_CHECK(arr.size(), 3U);
					for (int i = 0; i != 3; ++i)
						PR_CHECK(arr[i].val, ints[i].val);
					arr.resize(6);
					PR_CHECK(arr.size(), 6U);
					for (int i = 0; i != 3; ++i)
						PR_CHECK(arr[i].val, ints[i].val);
					for (int i = 3; i != 6; ++i)
						PR_CHECK(arr[i].val, 0);
				}
			}
		}
		{//Operators
			{
				Check chk;
				{
					Array0 arr0(4U, 1);
					Array0 arr1(3U, 2);
					arr1 = arr0;
					PR_CHECK(arr0.size(), 4U);
					PR_CHECK(arr1.size(), 4U);
					for (int i = 0; i != 4; ++i)
						PR_CHECK(arr1[i].val, arr0[i].val);
				}
			}{
				Check chk;
				{
					Array0 arr0(4U, 1);
					Array1 arr2;
					arr2 = arr0;
					PR_CHECK(arr0.size(), 4U);
					PR_CHECK(arr2.size(), 4U);
					for (int i = 0; i != 4; ++i)
						PR_CHECK(arr2[i].val, arr0[i].val);

					struct L
					{
						static std::vector<Type> Conv(std::vector<Type> v) { return v; }
					};
					std::vector<Type> vec0 = L::Conv(arr0);
					PR_CHECK(vec0.size(), 4U);
					for (int i = 0; i != 4; ++i)
						PR_CHECK(vec0[i].val, arr0[i].val);
				}
			}
		}
		{//Mem
			{
				Check chk;
				{
					Array0 arr0;
					arr0.reserve(100);
					for (int i = 0; i != 50; ++i) arr0.push_back(i);
					PR_CHECK(arr0.capacity(), 100U);
					arr0.shrink_to_fit();
					PR_CHECK(arr0.capacity(), 50U);
					arr0.resize(1);
					arr0.shrink_to_fit();
					PR_CHECK(arr0.capacity(), (size_t)arr0.local_size_v);
				}
			}
		}
		{// Copy
			{
				Check chk;
				{
					Array0 arr0 = {10,20,30};
					{
						Array0 arr1 = {0,1,2,3,4,5,6,7,8,9};
						arr0 = arr1;
					}
					PR_CHECK(arr0.size(), 10U);
					for (int i = 0; i != int(arr0.size()); ++i)
						PR_CHECK(arr0[i].val, i);
				}
			}
		}
		{// Move
			{
				Check chk;
				{
					// arr0 local, arr1 local
					Array0 arr0 = {0,10,20,30};
					{
						Array0 arr1 = {0,1,2,3,4,5,6};
						arr0 = std::move(arr1);
					}
					PR_CHECK(arr0.size(), 7U);
					for (int i = 0; i != int(arr0.size()); ++i)
						PR_CHECK(arr0[i].val, i);
				}
			}{
				Check chk;
				{
					// arr0 !local, arr1 local
					Array0 arr0 = {0,10,20,30,40,50,60,70,80,90};
					{
						Array0 arr1 = {0,1,2,3};
						arr0 = std::move(arr1);
					}
					PR_CHECK(arr0.size(), 4U);
					for (int i = 0; i != int(arr0.size()); ++i)
						PR_CHECK(arr0[i].val, i);
				}
			}{
				Check chk;
				{
					// arr0 local, arr1 !local
					Array0 arr0 = {0,10,20,30};
					{
						Array0 arr1 = {0,1,2,3,4,5,6,7,8,9};
						arr0 = std::move(arr1);
					}
					PR_CHECK(arr0.size(), 10U);
					for (int i = 0; i != int(arr0.size()); ++i)
						PR_CHECK(arr0[i].val, i);
				}
			}{
				Check chk;
				{
					// arr0 !local, arr1 !local
					Array0 arr0 = {0,10,20,30,40,50,60,70,80,90};
					{
						Array0 arr1 = {0,1,2,3,4,5,6,7,8,9};
						arr0 = std::move(arr1);
					}
					PR_CHECK(arr0.size(), 10U);
					for (int i = 0; i != int(arr0.size()); ++i)
						PR_CHECK(arr0[i].val, i);
				}
			}
		}
		{// Non-copyable types
			{
				Check chk;
				{
					Array2 arr0;
					arr0.emplace_back(0);
					arr0.emplace_back(1);
					arr0.emplace_back(2);
					arr0.emplace_back(3);
					arr0.emplace_back(4);

					PR_CHECK(arr0.size(), 5U);
					for (int i = 0; i != int(arr0.size()); ++i)
						PR_CHECK(arr0[i].val, i);
				}
			}{
				Check chk;
				{
					Array2 arr0;
					arr0.emplace(arr0.end(), 2);
					arr0.emplace(arr0.begin(), 1);
					arr0.emplace(arr0.end(), 3);
					Array2 arr1 = std::move(arr0);
					arr1.emplace(arr1.begin(), 0);
					arr0 = std::move(arr1);

					PR_CHECK(arr0.size(), 4U);
					for (int i = 0; i != int(arr0.size()); ++i)
						PR_CHECK(arr0[i].val, i);
				}
			}{
				Check chk;
				{
					Array2 arr0;
					arr0.emplace(arr0.end(), 2);
					arr0.emplace(arr0.begin(), 1);
					arr0.emplace(arr0.end(), 3);

					Array2 arr1 = std::move(arr0);
					arr1.emplace(arr1.begin(), 0);
					arr0 = std::move(arr1);

					PR_CHECK(arr0.size(), 4U);
					for (int i = 0; i != int(arr0.size()); ++i)
						PR_CHECK(arr0[i].val, i);
				}
			}{
				Check chk;
				{
					Array2 arr0;
					arr0.emplace_back(0);
					arr0.emplace_back(1);
					arr0.emplace_back(2);
					arr0.emplace_back(3);
					arr0.emplace_back(4);

					NonCopyable ins1(100);
					arr0.insert(arr0.begin() + 2, std::move(ins1));

					NonCopyable ins2(200);
					arr0.insert(arr0.begin(), std::move(ins2));

					NonCopyable ins3(300);
					arr0.insert(arr0.end(), std::move(ins3));

					PR_CHECK(arr0.size(), 8U);
					PR_CHECK(arr0[0].val, 200);
					PR_CHECK(arr0[1].val, 0);
					PR_CHECK(arr0[2].val, 1);
					PR_CHECK(arr0[3].val, 100);
					PR_CHECK(arr0[4].val, 2);
					PR_CHECK(arr0[5].val, 3);
					PR_CHECK(arr0[6].val, 4);
					PR_CHECK(arr0[7].val, 300);
				}
			}
		}
	}
}
#endif

