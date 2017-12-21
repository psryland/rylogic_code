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
	#if _MSC_VER < 1900
	#   define noexcept throw()
	#   define PR_NOEXCEPT_DEFINED
	#endif

	namespace impl
	{
		namespace deque
		{
			// Map of pointers to blocks
			template <typename Type, std::size_t BlockSize, typename Allocator> struct BlockPtrMap
			{
				using alloc_traits    = typename std::allocator_traits<Allocator>;
				using PtrsAlloc       = typename alloc_traits::template rebind_alloc<Type*>;
				using allocator_type  = Allocator;
				using size_type       = std::size_t;
				using difference_type = std::ptrdiff_t;
				using value_type      = Type;
				using const_pointer   = Type const*;
				using pointer         = Type*;
				using const_reference = Type const&;
				using reference       = Type&;
				using byte            = unsigned char;
				using block           = Type[BlockSize];

				enum { CountPerBlock = BlockSize };

				union {
				block**    m_blocks;     // The array of block pointers
				Type**     m_ptrs;       // The array of block pointers, size always pow2
				};
				size_type  m_first;      // Index of the first in-use block
				size_type  m_last;       // Index of one past the last in-use block
				size_type  m_capacity;   // The length of 'm_ptrs', either 0 or pow2
				Allocator  m_alloc_type; // Allocator for allocating blocks (Type[])
				PtrsAlloc  m_alloc_ptrs; // Allocator for allocating the block pointer array

				BlockPtrMap(Allocator const& allocator)
					:m_ptrs()
					,m_first()
					,m_last()
					,m_capacity()
					,m_alloc_type(allocator)
					,m_alloc_ptrs(allocator)
				{}
				~BlockPtrMap()
				{
					free_all();
				}

				// The number of in-use blocks
				size_type count() const
				{
					return size_type(m_last - m_first);
				}

				// Move assign the block ptr array (only valid if allocators are compatible)
				template <size_type B> BlockPtrMap& operator = (BlockPtrMap<Type,B,Allocator>&& rhs)
				{
					assert(m_alloc_type == rhs.m_alloc_type && "this implementation only supports same allocators");
					std::swap(m_ptrs     , rhs.m_ptrs    );
					std::swap(m_first    , rhs.m_first   );
					std::swap(m_last     , rhs.m_last    );
					std::swap(m_capacity , rhs.m_capacity);
					return *this;
				}

				// Convert a signed element index into a block index relative to 'm_ptrs[m_first]'.
				// Note: assumes that m_ptr[m_first][0] == element index zero, remember to add 'm_first'
				difference_type block_index(difference_type element_index) const
				{
					return element_index >= 0 ? (element_index / CountPerBlock) : (-element_index / CountPerBlock - 1);
				}

				// Return the block for the given element index. Automatically grows the map.
				// Note: assumes that m_ptr[m_first][0] == element index zero, remember to add 'm_first'
				Type* operator[](difference_type element_index)
				{
					// Convert the element index into a block index
					auto blk_idx = block_index(element_index);

					// Ensure the map is large enough to contain 'blk_idx'
					blk_idx = ensure_space(blk_idx);

					// Return the pointer to the block
					assert(size_type(blk_idx) < count() && "ensure space didn't work");
					return m_ptrs[m_first + blk_idx];
				}
				Type const* operator[](difference_type element_index) const
				{
					assert(size_type(element_index) < count()*CountPerBlock && "element index out of range");

					// Convert the element index into a block index
					auto blk_idx = block_index(element_index);

					// Return the pointer to the block
					return m_ptrs[m_first + blk_idx];
				}

				// Release all allocated memory
				void free_all()
				{
					for (size_type i = 0; i != m_capacity; ++i)
						m_alloc_type.deallocate(m_ptrs[i], CountPerBlock);

					m_alloc_ptrs.deallocate(m_ptrs, m_capacity);
					m_ptrs     = nullptr;
					m_first    = 0;
					m_last     = 0;
					m_capacity = 0;
				}

				// Release capacity. [first,last) are the in-use element range
				void shrink_to_fit(size_type& first, size_type& last)
				{
					// Free unused blocks at the front
					while (first >= CountPerBlock)
					{
						m_alloc_type.deallocate(m_ptrs[m_first], CountPerBlock);
						m_ptrs[m_first++] = nullptr;
						first -= CountPerBlock;
					}

					// Free unused blocks at the end
					while (count()*CountPerBlock - last >= CountPerBlock)
					{
						m_alloc_type.deallocate(m_ptrs[--m_last], CountPerBlock);
						m_ptrs[m_last] = nullptr;
					}
			
					// Reallocate the block ptr map if excessive
					size_type inuse = m_last - m_first;
					if (inuse < m_capacity/2)
					{
						auto new_capacity = m_capacity/2;
						for (; new_capacity/2 > inuse; new_capacity /= 2) {}
						Type** mem = m_alloc_ptrs.allocate(new_capacity);

						::memcpy(mem, m_ptrs + m_first, inuse * sizeof(Type*));
						::memset(mem + inuse, 0, (new_capacity - inuse) * sizeof(Type*));

						std::swap(m_ptrs, mem);
						m_first    = 0;
						m_last     = inuse;
						m_capacity = new_capacity;
					}
				}

				// Grow the block map to include 'blk_idx'.
				// If block index is outside the range [0, count()) then new blocks will be added.
				difference_type ensure_space(difference_type blk_idx)
				{
					if (size_type(blk_idx) < count())
						return blk_idx;

					// Add to the start
					if (blk_idx < 0)
					{
						size_type to_add = -blk_idx;

						// If there isn't space, reallocate the block ptr map
						if (to_add > m_first)
						{
							grow_map(to_add + count(), [](Type** mem, size_type new_capacity, BlockPtrMap& map)
							{
								// Fill the front with nullptr and the end from the old map
								auto keep_count = map.m_capacity - map.m_first;
								auto fill_count = new_capacity - keep_count;
								::memset(mem, 0, fill_count * sizeof(Type*));
								::memcpy(mem + fill_count, map.m_ptrs + map.m_first, keep_count * sizeof(Type*));
								return fill_count;
							});
						}

						// Fill with new blocks
						for (; to_add-- != 0;)
							m_ptrs[--m_first] = m_alloc_type.allocate(CountPerBlock);

						blk_idx = 0;
					}
					else // add to the end
					{
						auto to_add = blk_idx - count() + 1;

						// If there isn't space, reallocate the block ptr map
						if (to_add > m_capacity - m_last)
						{
							grow_map(to_add + count(), [](Type** mem, size_type new_capacity, BlockPtrMap& map)
							{
								// Fill the front from the old map, and the end with nullptr
								auto keep_count = map.m_last;
								auto fill_count = new_capacity - keep_count;
								::memset(mem + keep_count, 0, fill_count * sizeof(Type*));
								::memcpy(mem, map.m_ptrs, keep_count * sizeof(Type*));
								return map.m_first;
							});
						}

						// Fill with new blocks
						for (; to_add-- != 0;)
							m_ptrs[m_last++] = m_alloc_type.allocate(CountPerBlock);
					}
					return blk_idx;
				}

				// Grow the 'm_ptrs' buffer to contain at least 'min_count' items
				template <typename CopyFunc> void grow_map(size_type min_count, CopyFunc copy)
				{
					// Determine the required capacity
					auto new_capacity = m_capacity ? m_capacity * 2 : 1;
					for (; new_capacity < min_count; new_capacity *= 2) {}

					// Allocate a new pointer map
					Type** mem = m_alloc_ptrs.allocate(new_capacity);
				
					// Copy from the old map to the new
					auto new_count = count(); // same as old count
					auto new_first = copy(mem, new_capacity, *this);
				
					// Swap the pointers
					m_alloc_ptrs.deallocate(m_ptrs, m_capacity);
					m_ptrs     = mem;
					m_first    = new_first;
					m_last     = new_first + new_count;
					m_capacity = new_capacity;
				}

				// Swap the block ptr map
				void swap(BlockPtrMap& rhs)
				{
					// same allocator, swap pointers
					auto same_alloc = m_alloc_type == rhs.m_alloc_type;
					if (same_alloc || alloc_traits::propagate_on_container_swap::value)
					{
						if (!same_alloc)
						{
							std::swap(m_alloc_type , rhs.m_alloc_type);
							std::swap(m_alloc_ptrs , rhs.m_alloc_ptrs);
						}
						std::swap(m_ptrs     , rhs.m_ptrs    );
						std::swap(m_first    , rhs.m_first   );
						std::swap(m_last     , rhs.m_last    );
						std::swap(m_capacity , rhs.m_capacity);
					}
				}
			};

			// deque iterators
			template <typename Type, std::size_t BlockSize, typename Allocator, typename Ref, typename Ptr, typename BPMapPtr> struct iter
			{
				typedef std::random_access_iterator_tag iterator_category;
				typedef std::size_t size_type;
				typedef std::ptrdiff_t difference_type;
				typedef BPMapPtr map_ptr;
				typedef Type value_type;
				typedef Ptr pointer;
				typedef Ref reference;
				enum { pr_deque_iter = true };

				size_type m_idx; // element index + deque.m_first
				map_ptr m_map; // pointer to the block pointer array

				iter() :m_idx() ,m_map() {}
				iter(size_type idx, map_ptr map) :m_idx(idx) ,m_map(map) {}

				reference       operator * () const                    { return (*m_map)[m_idx][m_idx % BlockSize]; }
				pointer         operator ->() const                    { return std::pointer_traits<pointer>::pointer_to(**this); }
				iter&           operator ++()                          { ++m_idx; return *this; }
				iter            operator ++(int)                       { iter tmp = *this; ++*this; return tmp; }
				iter&           operator --()                          { --m_idx; return *this; }
				iter            operator --(int)                       { iter tmp = *this; --*this; return tmp; }
				reference       operator [](difference_type ofs) const { return *(*this + ofs); }
			};
			template <typename T, std::size_t B, typename A> struct citer // const iterator
				:iter<T, B, A, T const&, T const*, BlockPtrMap<T,B,A> const*>
			{
				citer(size_type idx, BlockPtrMap<T,B,A> const& map) :iter(idx, &map) {}
			};
			template <typename T, std::size_t B, typename A> struct miter // mutable iterator
				:iter<T, B, A, T&, T*, BlockPtrMap<T,B,A>*>
			{
				miter(size_type idx, BlockPtrMap<T,B,A>& map) :iter(idx, &map) {}
				operator citer<T,B,A>() const { return citer<T,B,A>(m_idx, *m_map); }
			};

			template <typename L, typename R, typename = std::enable_if_t<L::pr_deque_iter>> inline bool operator == (L lhs, R rhs) { return lhs.m_idx == rhs.m_idx; }
			template <typename L, typename R, typename = std::enable_if_t<L::pr_deque_iter>> inline bool operator != (L lhs, R rhs) { return lhs.m_idx != rhs.m_idx; }
			template <typename L, typename R, typename = std::enable_if_t<L::pr_deque_iter>> inline bool operator <  (L lhs, R rhs) { return lhs.m_idx <  rhs.m_idx; }
			template <typename L, typename R, typename = std::enable_if_t<L::pr_deque_iter>> inline bool operator >  (L lhs, R rhs) { return lhs.m_idx >  rhs.m_idx; }
			template <typename L, typename R, typename = std::enable_if_t<L::pr_deque_iter>> inline bool operator <= (L lhs, R rhs) { return lhs.m_idx <= rhs.m_idx; }
			template <typename L, typename R, typename = std::enable_if_t<L::pr_deque_iter>> inline bool operator >= (L lhs, R rhs) { return lhs.m_idx >= rhs.m_idx; }

			template <typename L, typename = std::enable_if_t<L::pr_deque_iter>> inline L& operator += (L& lhs, std::ptrdiff_t ofs) { lhs.m_idx += ofs; return lhs; }
			template <typename L, typename = std::enable_if_t<L::pr_deque_iter>> inline L& operator -= (L& lhs, std::ptrdiff_t ofs) { lhs.m_idx -= ofs; return lhs; }
			template <typename L, typename = std::enable_if_t<L::pr_deque_iter>> inline L operator +  (L lhs, std::ptrdiff_t ofs) { auto tmp = lhs; return tmp += ofs; }
			template <typename L, typename = std::enable_if_t<L::pr_deque_iter>> inline L operator -  (L lhs, std::ptrdiff_t ofs) { auto tmp = lhs; return tmp -= ofs; }

			template <typename L,typename R, typename = std::enable_if_t<L::pr_deque_iter>, typename = std::enable_if_t<R::pr_deque_iter>>
			inline std::ptrdiff_t operator - (L lhs, R rhs)
			{
				return rhs.m_idx <= lhs.m_idx ? lhs.m_idx - rhs.m_idx : -ptrdiff_t(rhs.m_idx - lhs.m_idx);
			}

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
	template <typename Type, std::size_t BlockSize=16, typename Allocator=pr::aligned_alloc<Type>>
	class deque
	{
	public:
		using type            = deque<Type,BlockSize,Allocator>;
		using BlockPtrMap     = impl::deque::BlockPtrMap<Type, BlockSize, Allocator>;
		using const_iterator  = impl::deque::citer<Type, BlockSize, Allocator>;
		using iterator        = impl::deque::miter<Type, BlockSize, Allocator>;
		using alloc_traits    = typename BlockPtrMap::alloc_traits;
		using allocator_type  = typename BlockPtrMap::allocator_type;
		using value_type      = typename BlockPtrMap::value_type;
		using size_type       = typename BlockPtrMap::size_type;
		using difference_type = typename BlockPtrMap::difference_type;
		using const_pointer   = typename BlockPtrMap::const_pointer;
		using pointer         = typename BlockPtrMap::pointer;
		using const_reference = typename BlockPtrMap::const_reference;
		using reference       = typename BlockPtrMap::reference;

		static_assert(((BlockSize - 1) & BlockSize) == 0, "BlockSize must be a power of two");
		static bool const TypeIsPod               = std::is_pod<Type>::value;
		static std::size_t const TypeSizeInBytes  = sizeof(Type);
		static std::size_t const TypeAlignment    = std::alignment_of<Type>::value;
		static std::size_t const CountPerBlock    = BlockSize;
		static std::size_t const BlockSizeInBytes = BlockSize * TypeSizeInBytes;
		static std::size_t const BlockIndexMask   = BlockSize - 1U;

	private:

		// Any combination of type, block size, and allocator is a friend
		template <class T, std::size_t B, class A> friend class deque;

		// type specific traits
		template <bool pod> struct base_traits;
		template <> struct base_traits<false>
		{
			static void destruct     (Allocator& alloc, Type* first, size_type count)                  { for (; count--;) { alloc.destroy(first++); } }
			static void construct    (Allocator& alloc, Type* first, size_type count)                  { for (; count--;) { alloc.default_construct(first++); } }
			static void copy_constr  (Allocator& alloc, Type* first, Type const* src, size_type count) { for (; count--;) { alloc.copy_construct(first++, *src++); } }
			static void move_constr  (Allocator& alloc, Type* first, Type* src, size_type count)       { for (; count--;) { alloc.move_construct(first++, std::move(*src++)); } }
			static void copy_assign  (Type* first, Type const* src, size_type count)                   { for (; count--;) { *first++ = *src++; } }
			static void move_assign  (Type* first, Type* src, size_type count)                         { for (; count--;) { *first++ = std::move(*src++); } }
		};
		template <> struct base_traits<true>
		{
			static void destruct     (Allocator&, Type* first, size_type count)                  { assert((::memset(first, 0xdd, sizeof(Type) * count), true)); (void)first,count; }
			static void construct    (Allocator&, Type* first, size_type count)                  { assert((::memset(first, 0xcd, sizeof(Type) * count), true)); (void)first,count; }
			static void copy_constr  (Allocator&, Type* first, Type const* src, size_type count) { ::memcpy(first, src, sizeof(Type) * count); }
			static void move_constr  (Allocator&, Type* first, Type* src, size_type count)       { ::memcpy(first, src, sizeof(Type) * count); }
			static void copy_assign  (Type* first, Type const* src, size_type count)             { ::memcpy(first, src, sizeof(Type) * count); }
			static void move_assign  (Type* first, Type* src, size_type count)                   { ::memcpy(first, src, sizeof(Type) * count); }
		};
		struct traits :base_traits<TypeIsPod>
		{
			static void destruct_range(Allocator& alloc, iterator first, size_type count)              { for (;count--; ++first) destruct(alloc, &*first, 1); }
			static void fill_constr(Allocator& alloc, Type* first, size_type count, Type const& val)   { for (; count--;) { copy_constr(alloc, first++, &val, 1); } }
			static void fill_assign(                  Type* first, size_type count, Type const& val)   { for (; count--;) { copy_assign(first++, &val, 1); } }
			
			// construct at 'first'
			template <class... Args> static void constr(Allocator& alloc, Type* first, Args&&... args)
			{
				alloc_traits::construct(alloc, first, std::forward<Args>(args)...);
			}

			// move [first, last) to [dest, ...)
			static iterator move(const_iterator first, const_iterator last, iterator dest)
			{	
				for (; first != last; ++dest, ++first)
					*dest = std::move(*first);
				return dest;
			}

			// move [first, last) backwards to [..., dest)
			static iterator move_backward(const_iterator first, const_iterator last, iterator dest)
			{
				while (first != last)
					*--dest = std::move(*--last);
				return dest;
			}
		};

		// Debugging view
		#ifndef NDEBUG
		template <typename T> struct DbgPtr { using type = T const (*)[CountPerBlock]; };
		template <> struct DbgPtr<char>     { using type = char const *; };
		template <> struct DbgPtr<wchar_t>  { using type = wchar_t const *; };
		typename DbgPtr<Type>::type m_data;
		bool set_dbg_ptr()
		{
			auto ptr = &m_map[m_first][m_first & BlockIndexMask];
			m_data = reinterpret_cast<DbgPtr<Type>::type>(ptr);
			return true;
		}
		#endif

		BlockPtrMap m_map; // The array of block pointers
		size_type m_first; // Element index of the first element == The offset from 'm_map.m_ptr' to the first element
		size_type m_last;  // Element index of the last+1 element == The offset from 'm_map.m_ptr' to one passed the last element

	public:

		// The memory allocator
		Allocator const& allocator() const { return m_map.m_alloc_type; }
		Allocator&       allocator()       { return m_map.m_alloc_type; }

		// construct empty
		deque()
			:m_map(allocator_type())
			,m_first()
			,m_last()
		{
			assert(set_dbg_ptr());
		}
		~deque()
		{
			clear();
		}

		// construct with custom allocator
		explicit deque(Allocator const& allocator)
			:m_map(allocator)
			,m_first()
			,m_last()
		{
			assert(set_dbg_ptr());
		}

		// construct from count * Type()
		explicit deque(size_type count)
			:m_map(allocator_type())
			,m_first()
			,m_last()
		{
			resize(count);
			assert(set_dbg_ptr());
		}

		// construct from count * val
		deque(size_type count, value_type const& val)
			:m_map(allocator_type())
			,m_first()
			,m_last()
		{
			for (; count-- != 0;)
				push_back(val);
			assert(set_dbg_ptr());
		}

		// copy construct (explicit copy constructor needed to prevent auto generated one even tho there's a template one that would work)
		deque(deque const& right)
			:m_map(right.allocator())
			,m_first()
			,m_last()
		{
			for (auto& r : right)
				push_back(r);
			assert(set_dbg_ptr()); 
		}

		// copy construct from any pr::deque type
		template <std::size_t B> deque(deque<Type,B,Allocator> const& right)
			:m_map(right.allocator())
			,m_first()
			,m_last()
		{
			for (auto& r : right)
				push_back(r);
			assert(set_dbg_ptr()); 
		}

		// construct from [first, last), with optional allocator
		template <class iter> deque(iter first, iter last, Allocator const& allocator = Allocator())
			:m_map(allocator)
			,m_first()
			,m_last()
		{
			for (;!(first == last); ++first)
				push_back(*first);
			assert(set_dbg_ptr()); 
		}

		// construct from initialiser list
		deque(std::initializer_list<value_type> list, Allocator const& allocator = Allocator())
			:m_map(allocator)
			,m_first()
			,m_last()
		{
			insert(begin(), list.begin(), list.end());
			assert(set_dbg_ptr()); 
		}

		// construct from another container-like object
		// 2nd parameter used to prevent overload issues with deque(count) (using SFINAE)
		template <class tcont> deque(tcont const& rhs,  typename tcont::size_type = 0, Allocator const& allocator = Allocator())
			:m_map(allocator)
			,m_first()
			,m_last()
		{
			for (auto& elem : rhs)
				emplace_back(elem);
			assert(set_dbg_ptr()); 
		}

		// construct by moving right
		deque(deque&& rhs)
			:m_map(rhs.allocator())
			,m_first()
			,m_last()
		{
			*this = std::move(rhs);
			assert(set_dbg_ptr()); 
		}

		// construct by moving right with allocator
		deque(deque&& rhs, Allocator const& allocator)
			:m_map(allocator)
			,m_first()
			,m_last()
		{
			*this = std::move(rhs);
			assert(set_dbg_ptr()); 
		}

		// assign by moving rhs
		deque& operator = (deque&& rhs)
		{
			if (this != &rhs)
				impl_assign(std::move(rhs));
			assert(set_dbg_ptr()); 
			return *this;
		}

		// assign rhs
		template <size_type B> deque& operator = (deque<Type,B,Allocator>&& rhs)
		{
			impl_assign(std::move(rhs));
			assert(set_dbg_ptr());
			return *this;
		}

		// assign rhs
		deque& operator = (deque const& rhs)
		{
			if (this != &rhs)
				impl_assign(rhs);
			assert(set_dbg_ptr()); 
			return *this;
		}

		// assign rhs
		template <size_type B> deque& operator = (deque<Type,B,Allocator> const& rhs)
		{
			impl_assign(rhs);
			assert(set_dbg_ptr());
			return *this;
		}

		// assign initializer_list
		deque& operator = (std::initializer_list<value_type> list)
		{
			assert(set_dbg_ptr());
			assign(list.begin(), list.end());
			return *this;
		}

		// test if container is empty
		bool empty() const
		{
			return m_last == m_first;
		}

		// return the length of the container
		size_type size() const
		{
			return m_last - m_first;
		}

		// element at position
		const_reference at(size_type idx) const
		{
			assert(idx < size() && "out of range");
			idx += m_first;
			return m_map[idx][idx & BlockIndexMask];
		}
		reference at(size_type idx)
		{
			assert(idx < size() && "out of range");
			idx += m_first;
			return m_map[idx][idx & BlockIndexMask];
		}

		// index operator
		const_reference operator[](size_type idx) const
		{
			return at(idx);
		}
		reference operator[](size_type idx)
		{
			return at(idx);
		}

		// return iterator for beginning of mutable sequence
		iterator begin() noexcept
		{
			return iterator(m_first, m_map);
		}

		// return iterator for beginning of non-mutable sequence
		const_iterator begin() const noexcept
		{
			return const_iterator(m_first, m_map);
		}

		// return iterator for end of mutable sequence
		iterator end() noexcept
		{
			return iterator(m_first + size(), m_map);
		}

		// return iterator for end of non-mutable sequence
		const_iterator end() const noexcept
		{
			return const_iterator(m_first + size(), m_map);
		}

		// return first element of mutable sequence
		reference front()
		{
			return *begin();
		}

		// return first element of non-mutable sequence
		const_reference front() const
		{
			return *begin();
		}

		// return last element of mutable sequence
		reference back()
		{
			return *(end() - 1);
		}

		// return last element of non-mutable sequence
		const_reference back() const
		{
			return *(end() - 1);
		}

		// erase all
		void clear()
		{
			resize(0);
			m_map.free_all();
		}

		// set new length, padding with Type() as needed
		void resize(size_type count)
		{
			while (size() < count) emplace_back();
			while (size() > count) pop_back();
		}

		// set new length, padding with 'val' as needed
		void resize(size_type count, const_reference val)
		{
			while (size() < count) push_back(val);
			while (size() > count) pop_back();
		}

		// reduce capacity
		void shrink_to_fit()
		{
			m_map.shrink_to_fit(m_first, m_last);
		}

		// number of push_front() calls before an allocation
		size_type capacity_front() const
		{
			return m_first;
		}

		// number of push_back() calls before an allocation
		size_type capacity_back() const
		{
			return m_map.count() * CountPerBlock - m_last;
		}

		// insert element at end
		void push_back(value_type const& val)
		{
			auto block = m_map[m_last];
			auto idx = m_last & BlockIndexMask;
			traits::copy_constr(allocator(), block + idx, &val, 1);
			++m_last;
		}

		// insert element at end
		template<class... Args> void emplace_back(Args&&... args)
		{
			auto block = m_map[m_last];
			auto idx = m_last & BlockIndexMask;
			traits::constr(allocator(), block + idx, std::forward<Args>(args)...);
			++m_last;
		}

		// insert element at beginning
		void push_front(value_type const& val)
		{
			auto block = m_map[m_first - 1];
			auto idx = (m_first - 1 + CountPerBlock) & BlockIndexMask;
			traits::copy_constr(allocator(), block + idx, &val, 1);
			if (m_first == 0) { m_first += CountPerBlock; m_last += CountPerBlock; }
			--m_first;
		}

		// insert element at beginning
		template<class... Args> void emplace_front(Args&&... args)
		{
			auto block = m_map[m_first - 1];
			auto idx = (m_first - 1 + CountPerBlock) & BlockIndexMask;
			traits::constr(allocator(), block + idx, std::forward<Args>(args)...);
			if (m_first == 0) { m_first += CountPerBlock; m_last += CountPerBlock; }
			--m_first;
		}

		// erase the element at the end
		void pop_back()
		{
			assert(!empty() && "pop_back() on empty deque");
			auto block = m_map[m_last - 1];
			auto idx = (m_last - 1 + CountPerBlock) & BlockIndexMask;
			traits::destruct(allocator(), block + idx, 1);
			--m_last; // last can't be 0 unless m_first > m_last
			if (m_first == m_last) m_first = m_last = 0;
		}

		// erase the element at the beginning
		void pop_front()
		{	
			assert(!empty() && "pop_front() on empty deque");
			auto block = m_map[m_first];
			auto idx = m_first & BlockIndexMask;
			traits::destruct(allocator(), block + idx, 1);
			++m_first;
			if (m_first == m_last) m_first = m_last = 0;
		}

		// assign [first, last), input iterators
		template <typename iter> void assign(iter first, iter last)
		{
			clear();
			for (; first != last; ++first)
				emplace_back(*first);
		}

		// assign count * val
		void assign(size_type count, value_type const& val)
		{
			erase(begin(), end());
			impl_insert(begin(), count, val);
		}

		// insert val at 'at'
		iterator insert(const_iterator at, value_type const& val)
		{
			size_type ofs = at - begin();
			assert(ofs <= size() && "deque insert iterator outside range");

			// closer to front, push to front then copy
			if (ofs <= size() / 2)
			{
				push_front(val);
				if (ofs != 0) std::rotate(begin(), begin() + 1, begin() + 1 + ofs);
			}
			else // closer to back, push to back then copy
			{
				push_back(val);
				if (ofs != size()-1) std::rotate(begin() + ofs, end() - 1, end());
			}
			return begin() + ofs;
		}

		// insert count * val at 'at'
		iterator insert(const_iterator at, size_type count, value_type const& val)
		{
			size_type ofs = at - begin();
			impl_insert(at, count, val);
			return begin() + ofs;
		}

		// insert [first, last) at 'at', input iterators
		template <class Iter> iterator insert(const_iterator at, Iter first, Iter last)
		{
			size_type ofs = at - begin();
			assert(ofs <= size() && "deque insert iterator outside range");
			size_type old_size = size();

			if (first == last)
			{}
			else if (ofs <= size() / 2) // closer to front, push to front then rotate
			{
				try
				{
					for (; first != last; ++first)
						push_front(*first); // prepend flipped
				}
				catch (...)
				{
					for (; old_size < size(); )
						pop_front(); // restore old size, at least
					throw;
				}

				size_type num = size() - old_size;
				std::reverse(begin(), begin() + num); // flip new stuff in place
				std::rotate(begin(), begin() + num, begin() + num + ofs);
			}
			else // closer to back
			{
				try
				{
					for (; first != last; ++first)
						push_back(*first); // append
				}
				catch (...)
				{
					for (; old_size < size(); )
						pop_back(); // restore old size, at least
					throw;
				}
				std::rotate(begin() + ofs, begin() + old_size, end());
			}
			return begin() + ofs;
		}

		// erase element at 'at'
		iterator erase(const_iterator at)
		{
			return erase(at, at + 1);
		}

		// erase [first, last)
		iterator erase(const_iterator first, const_iterator last)
		{
			assert(first <= last && first >= begin() && last <= end() && "invalid iterator range");

			size_type b = first.m_idx - m_first;
			size_type e = m_last - last.m_idx;
			size_type c = last.m_idx - first.m_idx;

			if (b < e) // closer to front, move begin() forward
			{
				traits::move_backward(begin(), first, make_iter(last)); // move [begin,first) to [..,last)
				traits::destruct_range(allocator(), begin(), c);
				m_first += c;
				if (m_first == m_last) m_first = m_last = 0;
			}
			else // closer to back, move end() backward
			{
				traits::move(last, end(), make_iter(first));
				traits::destruct_range(allocator(), end() - c, c);
				m_last -= c;
				if (m_first == m_last) m_first = m_last = 0;
			}
			return begin() + b;
		}

		// exchange contents with rhs
		void swap(deque& rhs)
		{
			if (this == &rhs) // same object, do nothing
			{}
			else if (allocator() == rhs.allocator() || alloc_traits::propagate_on_container_swap::value)
			{
				// same allocators or allocator allows swap
				std::swap(m_map  , rhs.m_map  );
				std::swap(m_first, rhs.m_first);
				std::swap(m_last , rhs.m_last );
			}
			else // containers are incompatible
			{
 				assert(false && "deque containers incompatible for swap");
			}
		}

		// Implicit conversion to a type that can be constructed from begin/end iterators
		// This allows cast to std::vector<> amoung others.
		// Note: converting to a std::vector<> when 'Type' has an alignment greater than the
		// default alignment causes a compiler error because of std::vector.resize().
		template <typename Cont> operator Cont() const
		{
			return Cont(begin(), end());
		}

	private:

		// copy assign rhs
		template <size_type B> void impl_assign(deque<Type,B,Allocator> const & rhs)
		{
			// change allocator before copying
			auto same_alloc = allocator() == rhs.allocator();
			if (!same_alloc && alloc_traits::propagate_on_container_copy_assignment::value)
			{
				clear();
				m_map.m_alloc_type = rhs.m_map.m_alloc_type;
				m_map.m_alloc_ptrs = rhs.m_map.m_alloc_ptrs;
			}

			if (rhs.empty())
			{
				clear();
			}
			else if (rhs.size() <= size()) // enough elements, copy new and destroy old
			{
				auto mid = std::copy(rhs.begin(), rhs.end(), begin());
				erase(mid, end());
			}
			else // new sequence longer, copy and construct new
			{
				auto mid = rhs.begin() + size();
				std::copy(rhs.begin(), mid, begin());
				insert(end(), mid, rhs.end());
			}
		}

		// move assign rhs
		template <size_type B> void impl_assign(deque<Type,B,Allocator>&& rhs)
		{
			clear();
			m_map = std::move(rhs.m_map);
			std::swap(m_first , rhs.m_first);
			std::swap(m_last  , rhs.m_last );
		}

		// insert count * val at 'at'
		void impl_insert(const_iterator at, size_type count, value_type const& val)
		{
			size_type num;
			size_type ofs = at - begin();
			size_type rem = size() - ofs;
			size_type old_size = size();

			assert(ofs <= size() && "deque insert iterator outside range");
 
			if (ofs < rem) // closer to front
			{
				try
				{
					// insert longer than prefix
					if (ofs < count)
					{
						// push excess values
						for (num = count - ofs; 0 < num; --num)
							push_front(val); 

						// push prefix
						for (num = ofs; 0 < num; --num)
							push_front(begin()[count - 1]);

						// fill in rest of values
						auto mid = begin() + count;
						std::fill(mid, mid + ofs, val);
					}
					else // insert not longer than prefix
					{
						// push part of prefix
						for (num = count; 0 < num; --num)
							push_front(begin()[count - 1]);

						// in case val is in sequence
						auto mid = begin() + count;
						auto tmp = val;
						traits::move(mid + count, mid + ofs, mid); // copy rest of prefix
						std::fill(begin() + ofs, mid + ofs, tmp); // fill in values
					}
				}
				catch (...)
				{
					// restore old size, at least
					for (; old_size < size();)
						pop_front();
					throw;
				}
			}
			else // closer to back
			{
				try
				{
					// insert longer than suffix
					if (rem < count)
					{
						// push excess values
						for (num = count - rem; 0 < num; --num)
							push_back(val);

						// push suffix
						for (num = 0; num < rem; ++num)
							push_back(begin()[ofs + num]);

						// fill in rest of values
						auto mid = begin() + ofs;
						std::fill(mid, mid + rem, val);
					}
					else // insert not longer than prefix
					{
						// push part of prefix
						for (num = 0; num < count; ++num)
							push_back(begin()[ofs + rem - count + num]);

						// copy rest of prefix
						auto mid = begin() + ofs;
						auto tmp = val; // in case val is in sequence
						traits::move_backward(mid, mid + rem - count, mid + rem);
						std::fill(mid, mid + count, tmp); // fill in values
					}
				}
				catch (...)
				{
					// restore old size, at least
					for (; old_size < size();)
						pop_back();
					throw;
				}
			}
		}

		// make iterator from const_iterator
		iterator make_iter(const_iterator i)
		{
			assert(i.m_map == &m_map && "iterator not for this deque");
			return iterator(i.m_idx, m_map);
		}
	};

	// operators
	template <class T, std::size_t B, typename A> inline void swap(deque<T,B,A>& lhs, deque<T,B,A>& rhs)
	{
		lhs.swap(rhs);
	}
	template <class T1, std::size_t B1, typename A1, class T2, std::size_t B2, typename A2> inline bool operator == (deque<T1,B1,A1> const& lhs, deque<T2,B2,A2> const& rhs)
	{
		return lhs.size() == rhs.size() && std::equal(lhs.begin(), lhs.end(), rhs.begin());
	}
	template <class T1, std::size_t B1, typename A1, class T2, std::size_t B2, typename A2> inline bool operator != (deque<T1,B1,A1> const& lhs, deque<T2,B2,A2> const& rhs)
	{
		return !(lhs == rhs);
	}
	template <class T1, std::size_t B1, typename A1, class T2, std::size_t B2, typename A2> inline bool operator <  (deque<T1,B1,A1> const& lhs, deque<T2,B2,A2> const& rhs)
	{
		return std::lexicographical_compare(lhs.begin(), lhs.end(), rhs.begin(), rhs.end());
	}
	template <class T1, std::size_t B1, typename A1, class T2, std::size_t B2, typename A2> inline bool operator <= (deque<T1,B1,A1> const& lhs, deque<T2,B2,A2> const& rhs)
	{
		return !(rhs < lhs);
	}
	template <class T1, std::size_t B1, typename A1, class T2, std::size_t B2, typename A2> inline bool operator >  (deque<T1,B1,A1> const& lhs, deque<T2,B2,A2> const& rhs)
	{
		return rhs < lhs;
	}
	template <class T1, std::size_t B1, typename A1, class T2, std::size_t B2, typename A2> inline bool operator >= (deque<T1,B1,A1> const& lhs, deque<T2,B2,A2> const& rhs)
	{
		return !(lhs < rhs);
	}

	#ifdef PR_NOEXCEPT_DEFINED
	#   undef PR_NOEXCEPT_DEFINED
	#   undef noexcept
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
			typedef unsigned int uint;

			struct Single :pr::RefCount<Single>
			{
				static void RefCountZero(RefCount<Single>*) {}
			};

			int& StartObjectCount() { static int start_object_count; return start_object_count; }
			int& ObjectCount() { static int object_count; return object_count; }
			Single& Refs() { static Single single; return single; }

			struct Type
			{
				uint val;
				pr::RefPtr<Single> ptr;

				Type() :val(0) ,ptr(&Refs(), true)                { ++ObjectCount(); }
				Type(uint w) :val(w) ,ptr(&Refs(), true)          { ++ObjectCount(); }
				Type(Type&& rhs) :val(rhs.val) ,ptr(rhs.ptr)      { ++ObjectCount(); }
				Type(Type const& rhs) :val(rhs.val) ,ptr(rhs.ptr) { ++ObjectCount(); }
				Type& operator = (Type&& rhs)
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
					--ObjectCount();
					PR_CHECK(ptr.m_ptr == &Refs(), true); // destructing an invalid Type
					val = 0xcccccccc;
				}
			};
			static_assert(std::is_move_constructible<Type>::value, "");
			static_assert(std::is_copy_constructible<Type>::value, "");
			static_assert(std::is_move_assignable<Type>::value, "");
			static_assert(std::is_copy_assignable<Type>::value, "");

			struct NonCopyable :Type
			{
				NonCopyable() :Type() {}
				NonCopyable(uint w) :Type(w) {}
				NonCopyable(NonCopyable const&) = delete;
				NonCopyable(NonCopyable&& rhs) :Type(std::forward<Type&&>(rhs)) {}
				NonCopyable& operator = (NonCopyable const&) = delete;
				NonCopyable& operator = (NonCopyable&& rhs)
				{
					*static_cast<Type*>(this) = std::move(rhs);
					return *this;
				}
			};
		//	static_assert( std::is_move_constructible<NonCopyable>::value, "");
		//	static_assert(!std::is_copy_constructible<NonCopyable>::value, "");
		//	static_assert( std::is_move_assignable<NonCopyable>::value, "");
		//	static_assert(!std::is_copy_assignable<NonCopyable>::value, "");

			inline bool operator == (Type const& lhs, Type const& rhs) { return lhs.val == rhs.val; }

			typedef pr::deque<Type, 8> Deque0;
			typedef pr::deque<Type, 16> Deque1;
			typedef pr::deque<NonCopyable, 4> Deque2;
		}

		PRUnitTest(pr_container_deque)
		{
			using namespace pr::unittests::deque;
			std::vector<uint> uints; uints.resize(16);
			std::vector<Type> types; types.resize(16);
			for (uint i = 0; i != 16; ++i)
			{
				uints[i] = i;
				types[i] = Type(i);
			}

			StartObjectCount() = ObjectCount();
			{
				// default constructor
				Deque0 deq;
				PR_CHECK(deq.empty(), true);
				PR_CHECK(deq.size(), 0U);
			}
			PR_CHECK(ObjectCount(), StartObjectCount());
 			StartObjectCount() = ObjectCount();
			{
				// construct with allocator
				std::allocator<int> al;
				pr::deque<int, 16, std::allocator<int>> deq0(al);
				deq0.push_back(42);
				
				// copy to deque of different type
				pr::deque<int, 8, std::allocator<int>> deq1;
				deq1 = deq0;
				PR_CHECK(deq0.size(), 1U);
				PR_CHECK(deq0[0], 42);
				PR_CHECK(deq1.size(), 1U);
				PR_CHECK(deq1[0], 42);
			}
			PR_CHECK(ObjectCount(), StartObjectCount());
			StartObjectCount() = ObjectCount();
			{
				// count constructor
				Deque1 deq(15);
				PR_CHECK(!deq.empty(), true);
				PR_CHECK(deq.size(), 15U);
			}
			PR_CHECK(ObjectCount(), StartObjectCount());
			StartObjectCount() = ObjectCount();
			{
				// count + instance constructor
				Deque0 deq(5U, 3);
				PR_CHECK(deq.size(), 5U);
				for (size_t i = 0; i != 5; ++i)
					PR_CHECK(deq[i].val, 3U);
			}
			PR_CHECK(ObjectCount(), StartObjectCount());
			StartObjectCount() = ObjectCount();
			{
				// copy constructor
				Deque0 deq0(5U,3);
				Deque1 deq1(deq0);
				PR_CHECK(deq1.size(), deq0.size());
				for (size_t i = 0; i != deq0.size(); ++i)
					PR_CHECK(deq1[i].val, deq0[i].val);
			}
			PR_CHECK(ObjectCount(), StartObjectCount());
			StartObjectCount() = ObjectCount();
			{
				// Construct from a std::deque
				std::deque<uint> deq0(4U, 6);
				Deque0 deq1(deq0);
				PR_CHECK(deq1.size(), deq0.size());
				for (size_t i = 0; i != deq0.size(); ++i)
					PR_CHECK(deq1[i].val, deq0[i]);
			}
			PR_CHECK(ObjectCount(), StartObjectCount());
			PR_CHECK(ObjectCount(), StartObjectCount());
			StartObjectCount() = ObjectCount();
			{
				// Construct from range
				uint r[] = {1,2,3,4};
				Deque0 deq1(std::begin(r), std::end(r));
				PR_CHECK(deq1.size(), 4U);
				PR_CHECK(deq1[0].val, r[0]);
				PR_CHECK(deq1[1].val, r[1]);
				PR_CHECK(deq1[2].val, r[2]);
				PR_CHECK(deq1[3].val, r[3]);
			}
			PR_CHECK(ObjectCount(), StartObjectCount());
			StartObjectCount() = ObjectCount();
			{
				// Move construct
				Deque0 deq0(4U, 6);
				Deque0 deq1 = std::move(deq0);
				PR_CHECK(deq0.size(), 0U);
				PR_CHECK(deq1.size(), 4U);
				for (size_t i = 0; i != deq1.size(); ++i)
					PR_CHECK(deq1[i].val, 6U);
			}
			PR_CHECK(ObjectCount(), StartObjectCount());
			{//RefCounting0
				PR_CHECK(Refs().m_ref_count, 16);
			}
			{//Assign
				StartObjectCount() = ObjectCount();
				{
					// Copy assign
					Deque0 deq0(4U, 5);
					Deque1 deq1;
					deq1 = deq0;
					PR_CHECK(deq0.size(), deq1.size());
					for (size_t i = 0; i != deq0.size(); ++i)
						PR_CHECK(deq1[i].val, deq0[i].val);
				}
				PR_CHECK(ObjectCount(), StartObjectCount());
				StartObjectCount() = ObjectCount();
				{
					// Move assign
					Deque0 deq0(4U, 5);
					Deque1 deq1;
					deq1 = std::move(deq0);
					PR_CHECK(deq0.size(), 0U);
					PR_CHECK(deq1.size(), 4U);
					for (auto& i : deq1)
						PR_CHECK(i.val, 5U);
				}
				PR_CHECK(ObjectCount(), StartObjectCount());
				StartObjectCount() = ObjectCount();
				{
					// assign method
					Deque0 deq0;
					deq0.assign(3U, 5);
					PR_CHECK(deq0.size(), 3U);
					for (size_t i = 0; i != 3; ++i)
						PR_CHECK(deq0[i].val, 5U);

					// assign range method
					Deque1 deq1;
					deq1.assign(&types[0], &types[8]);
					PR_CHECK(deq1.size(), 8U);
					for (size_t i = 0; i != 8; ++i)
						PR_CHECK(deq1[i].val, types[i].val);
				}
				PR_CHECK(ObjectCount(), StartObjectCount());
			}
			{//RefCounting1
				PR_CHECK(Refs().m_ref_count, 16);
			}
			{//Clear
				StartObjectCount() = ObjectCount();
				{
					pr::deque<int,8> deq0({0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15});
					PR_CHECK(!deq0.empty(), true);
					PR_CHECK(deq0.front(), 0);
					PR_CHECK(deq0.back(), 15);
					deq0.clear();
					PR_CHECK(deq0.empty(), true);
				}
				PR_CHECK(ObjectCount(), StartObjectCount());
			}
			{//RefCounting2
				PR_CHECK(Refs().m_ref_count, 16);
			}
			{//Erase
				StartObjectCount() = ObjectCount();
				{
					// erase range non-pods
					Deque0 deq0(std::begin(types), std::end(types));
					auto b = deq0.begin();
					deq0.erase(b + 3, b + 13);
					PR_CHECK(deq0.size(), 6U);
					for (size_t i = 0; i != 3; ++i) PR_CHECK(deq0[i].val, types[i]   .val);
					for (size_t i = 3; i != 6; ++i) PR_CHECK(deq0[i].val, types[i+10].val);
				}
				PR_CHECK(ObjectCount(),StartObjectCount());
				StartObjectCount() = ObjectCount();
				{
					// erase range pods
					pr::deque<uint,8> deq0(std::begin(uints), std::end(uints));
					auto b = deq0.begin();
					deq0.erase(b + 3, b + 13);
					PR_CHECK(deq0.size(), 6U);
					for (size_t i = 0; i != 3; ++i) PR_CHECK(deq0[i], uints[i]   );
					for (size_t i = 3; i != 6; ++i) PR_CHECK(deq0[i], uints[i+10]);
				}
				PR_CHECK(ObjectCount(),StartObjectCount());
				StartObjectCount() = ObjectCount();
				{
					// erase at
					Deque1 deq1(types.begin(), types.begin() + 4);
					deq1.erase(deq1.begin() + 2);
					PR_CHECK(deq1.size(), 3U);
					for (size_t i = 0; i != 2; ++i) PR_CHECK(deq1[i].val, types[i]  .val);
					for (size_t i = 2; i != 3; ++i) PR_CHECK(deq1[i].val, types[i+1].val);
				}
				PR_CHECK(ObjectCount(), StartObjectCount());
			}
			{//RefCounting3
				PR_CHECK(Refs().m_ref_count, 16);
			}
			{//Insert
				StartObjectCount() = ObjectCount();
				{
					// insert count*val at 'at'
					Deque0 deq0;
					deq0.insert(deq0.end(), 4U, 9);
					PR_CHECK(deq0.size(), 4U);
					for (size_t i = 0; i != 4; ++i)
						PR_CHECK(deq0[i].val, 9U);
				}
				PR_CHECK(ObjectCount(), StartObjectCount());
				StartObjectCount() = ObjectCount();
				{
					// insert range
					Deque1 deq1(4U, 6);
					deq1.insert(deq1.begin() + 2, &types[2], &types[7]);
					PR_CHECK(deq1.size(), 9U);
					for (size_t i = 0; i != 2; ++i) PR_CHECK(deq1[i].val, 6U);
					for (size_t i = 2; i != 7; ++i) PR_CHECK(deq1[i].val, types[i].val);
					for (size_t i = 7; i != 9; ++i) PR_CHECK(deq1[i].val, 6U);
				}
				PR_CHECK(ObjectCount(), StartObjectCount());
			}
			{//RefCounting4
				PR_CHECK(Refs().m_ref_count, 16);
			}
			{//PushPop
				StartObjectCount() = ObjectCount();
				{
					// pop_back
					Deque0 deq;
					deq.insert(std::end(deq), std::begin(types), std::begin(types) + 3);
					auto addr = &deq[1];
					deq.insert(std::end(deq), std::begin(types) + 3, std::end(types));
					PR_CHECK(deq.size(), 16U);
					PR_CHECK(&deq[1], addr);
					deq.pop_back();
					deq.pop_back();
					deq.pop_back();
					deq.pop_back();
					PR_CHECK(deq.size(), 12U);
					PR_CHECK(&deq[1], addr);
					for (size_t i = 0; i != deq.size(); ++i)
						PR_CHECK(deq[i].val, types[i].val);
				}
				PR_CHECK(ObjectCount(), StartObjectCount());
				StartObjectCount() = ObjectCount();
				{
					// push_back
					Deque1 deq;
					deq.emplace_back(0);
					auto addr = &deq[0];
					PR_CHECK(deq.size(), 1U);
					
					for (int i = 1; i != 4; ++i) deq.emplace_back(i);
					PR_CHECK(deq.size(), 4U);
					PR_CHECK(&deq[0], addr);

					for (int i = 4; i != 9; ++i) deq.push_back(i);
					PR_CHECK(deq.size(), 9U);
					PR_CHECK(&deq[0], addr);

					for (size_t i = 0; i != deq.size(); ++i)
						PR_CHECK(deq[i].val, types[i].val);
				}
				PR_CHECK(ObjectCount(), StartObjectCount());
				StartObjectCount() = ObjectCount();
				{
					// pop_front
					Deque0 deq;
					deq.insert(std::end(deq), std::begin(types), std::begin(types) + 8);
					auto addr = &deq[7];
					deq.insert(std::end(deq), std::begin(types) + 8, std::end(types));
					PR_CHECK(deq.size(), 16U);
					PR_CHECK(&deq[7], addr);
					deq.pop_front();
					deq.pop_front();
					deq.pop_front();
					deq.pop_front();
					PR_CHECK(deq.size(), 12U);
					PR_CHECK(&deq[3], addr);
					for (size_t i = 0; i != deq.size(); ++i)
						PR_CHECK(deq[i].val, types[i+4].val);
				}
				PR_CHECK(ObjectCount(), StartObjectCount());
				StartObjectCount() = ObjectCount();
				{
					// push_front
					Deque1 deq;
					deq.emplace_front(0);
					auto addr = &deq[0];
					PR_CHECK(deq.size(), 1U);
					
					for (int i = 1; i != 4; ++i) deq.emplace_front(i);
					PR_CHECK(deq.size(), 4U);
					PR_CHECK(&deq[3], addr);

					for (int i = 4; i != 9; ++i) deq.push_front(i);
					PR_CHECK(deq.size(), 9U);
					PR_CHECK(&deq[8], addr);

					for (size_t i = 0; i != deq.size(); ++i)
						PR_CHECK(deq[i].val, 8 - types[i].val);
				}
				PR_CHECK(ObjectCount(), StartObjectCount());
				StartObjectCount() = ObjectCount();
				{
					// resize
					Deque0 deq;
					deq.insert(std::begin(deq), types.data(), types.data() + 16);
					PR_CHECK(deq.size(), 16U);
					deq.resize(7);
					PR_CHECK(deq.size(), 7U);
					for (size_t i = 0; i != deq.size(); ++i)
						PR_CHECK(deq[i].val, types[i].val);
					deq.resize(12);
					PR_CHECK(deq.size(), 12U);
					for (size_t i = 0; i != 7U; ++i)
						PR_CHECK(deq[i].val, types[i].val);
					for (size_t i = 7U; i != 12U; ++i)
						PR_CHECK(deq[i].val, 0U);
				}
				PR_CHECK(ObjectCount(), StartObjectCount());
			}
			{//RefCounting5
				PR_CHECK(Refs().m_ref_count, 16);
			}
			{//Operators
				StartObjectCount() = ObjectCount();
				{
					// assign and equality
					Deque0 deq0(4U, 1);
					Deque0 deq1(3U, 2);
					deq1 = deq0;
					PR_CHECK(deq0 == deq1, true);
					PR_CHECK(deq0.size(), 4U);
					PR_CHECK(deq1.size(), 4U);
					for (size_t i = 0; i != 4; ++i)
						PR_CHECK(deq1[i].val, deq0[i].val);
				}
				PR_CHECK(ObjectCount(), StartObjectCount());
				StartObjectCount() = ObjectCount();
				{
					// inequality between different types
					Deque0 deq0(4U, 1);
					Deque1 deq1;
					deq1 = deq0;
					PR_CHECK(deq0 != deq1, false);
				}
				PR_CHECK(ObjectCount(), StartObjectCount());
				StartObjectCount() = ObjectCount();
				{
					// implicit conversion to std::deque
					struct L {
					static std::deque<Type> Conv(std::deque<Type> v) { return v; }
					};

					Deque0 deq0(4U, 1);
					std::deque<Type> deq1 = L::Conv(deq0);
					PR_CHECK(deq1.size(), 4U);
					for (size_t i = 0; i != 4; ++i)
						PR_CHECK(deq1[i].val, deq0[i].val);
				}
				PR_CHECK(ObjectCount(), StartObjectCount());
			}
			{//RefCounting6
				PR_CHECK(Refs().m_ref_count, 16);
			}
			{//Mem
				StartObjectCount() = ObjectCount();
				{
					pr::deque<int,8> deq0;
					const int count = 20;
					for (int i = 0; i != count; ++i) deq0.push_back(i);
					PR_CHECK(deq0.capacity_front(), 0U);
					PR_CHECK(deq0.capacity_back(), 4U);
					
					deq0.push_front(-1);
					PR_CHECK(deq0.capacity_front(), 7U);
					PR_CHECK(deq0.capacity_back(), 4U);
					
					deq0.erase(std::begin(deq0) + 10, std::end(deq0));
					PR_CHECK(deq0.capacity_front(), 7U);
					PR_CHECK(deq0.capacity_back(), 15U);
					deq0.erase(std::begin(deq0) + 9, std::end(deq0));
					PR_CHECK(deq0.capacity_front(), 7U);
					PR_CHECK(deq0.capacity_back(), 16U);
					deq0.emplace_back(9);

					deq0.shrink_to_fit();
					PR_CHECK(deq0.capacity_front(), 7U);
					PR_CHECK(deq0.capacity_back(), 7U);
					
					deq0.pop_front();
					deq0.pop_back();
					deq0.shrink_to_fit();
					PR_CHECK(deq0.capacity_front(), 0U);
					PR_CHECK(deq0.capacity_back(), 0U);

					deq0.resize(0);
					deq0.shrink_to_fit();
					PR_CHECK(deq0.capacity_front(), 0U);
					PR_CHECK(deq0.capacity_front(), 0U);
				}
				PR_CHECK(ObjectCount(), StartObjectCount());
			}
			{//RefCounting
				types.clear();
				PR_CHECK(Refs().m_ref_count, 0);
			}
			{//GlobalConstrDestrCount
				PR_CHECK(ObjectCount(), 0);
			}
		}
	}
}
#endif
