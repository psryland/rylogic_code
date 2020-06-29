//********************************
// PR3D Model file format
//  Copyright (c) Rylogic Ltd 2014
//********************************
// Index buffer for indices of unknown stride width
#pragma once
#include <cstdint>
#include <cassert>
#include "pr/common/cast.h"
#include "pr/container/byte_data.h"

namespace pr::geometry
{
	struct IdxBuf: byte_data<4>
	{
		// Notes:
		//  - Converting from a runtime stride to a compile-time type is a PITA. There will always be some indirection.
		//    Having a conditional before every index access sounds expensive, but the alternative is a function pointer
		//    which cannot be inlined. The best case is for the caller to switch on 'm_stride' and have separate loops for
		//    each possible stride size but this is a burden on the caller. I think the next best option is to switch inside
		//    loops and rely on the optimiser to move the conditional outside the loops in user code.

		// Helper const pointer type for accessing the indices
		union cptr_t
		{
			void const* vp;
			uint64_t const* u64;
			uint32_t const* u32;
			uint16_t const* u16;
			uint8_t  const* u08;
			cptr_t(void const* ptr) :vp(ptr) {}
			template <typename TOut> TOut to(int stride) const
			{
				// 'Stride' is the size of the underlying elements which are then cast to 'TOut'
				switch (stride)
				{
				case 8: return s_cast<TOut>(*u64);
				case 4: return s_cast<TOut>(*u32);
				case 2: return s_cast<TOut>(*u16);
				case 1: return s_cast<TOut>(*u08);
				default: throw std::runtime_error("Unsupported underlying data format");
				}
			}
		};

		// Const iterator for iterating over the indices
		template <typename TOut> struct citer_t
		{
			using proxy = struct proxy_ { TOut x; TOut const* operator -> () const { return &x; } };

			cptr_t m_ptr;
			int m_stride;

			citer_t(void const* p, int stride)
				:m_ptr(p)
				,m_stride(stride)
			{}
			TOut operator*() const
			{
				return m_ptr.to<TOut>(m_stride);
			}
			proxy operator->() const
			{
				return proxy{**this};
			}
			citer_t& operator ++()
			{
				m_ptr.u08 += m_stride;
				return *this;
			}
		
			friend bool operator == (citer_t const& lhs, citer_t const& rhs)
			{
				return lhs.m_ptr.vp == rhs.m_ptr.vp;
			}
			friend bool operator != (citer_t const& lhs, citer_t const& rhs)
			{
				return lhs.m_ptr.vp != rhs.m_ptr.vp;
			}
			friend bool operator < (citer_t const& lhs, citer_t const& rhs)
			{
				return lhs.m_ptr.vp < rhs.m_ptr.vp;
			}
		};

		// Proxy object for ranged for loops over indices of type 'TOut'
		template <typename TOut> struct casting_span_t
		{
			IdxBuf const* m_buf;

			casting_span_t(IdxBuf const& buf)
				:m_buf(&buf)
			{}
			citer_t<TOut> begin() const
			{
				return m_buf->begin<TOut>();
			}
			citer_t<TOut> end() const
			{
				return m_buf->end<TOut>();
			}
		};

		// Index stride. n = bytes per index
		int m_stride;

		// Constructor
		explicit IdxBuf(int stride = sizeof(uint32_t))
			:m_stride(stride)
		{}

		// The number of indices in this buffer
		size_t count() const
		{
			assert(m_stride != 0);
			return s_cast<size_t>(size() / m_stride);
		}

		// The width of each vertex in bytes
		int stride() const
		{
			return m_stride;
		}

		// Access the index buffer by index, regardless of underlying index format
		uint32_t operator[](int idx) const
		{
			// Convert from the underlying type to 'uint32_t'
			assert(idx >= 0 && idx < s_cast<int>(count()));
			auto ptr = cptr_t{data() + idx * m_stride};
			return ptr.to<uint32_t>(m_stride);
		}

		// Iteration - interpret the index buffer as TOut regardless of the stride of contained data.
		template <typename TOut = uint32_t> citer_t<TOut> begin() const
		{
			return citer_t<TOut> {data(), m_stride};
		}
		template <typename TOut = uint32_t> citer_t<TOut> end() const
		{
			// The buffer may contain padding...
			auto size_in_bytes = static_cast<size_t>(count() * m_stride);
			return citer_t<TOut> {data() + size_in_bytes, m_stride};
		}
		template <typename TOut = uint32_t> casting_span_t<TOut> casting_span() const
		{
			return casting_span_t<TOut>{*this};
		}
	};
}