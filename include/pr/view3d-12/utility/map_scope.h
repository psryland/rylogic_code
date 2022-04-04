//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12
{
	// A scope object for locking (or "mapping") a resource so that the CPU can access it.
	// Mapping: Gets a pointer to the data contained in a sub-resource, and denies the GPU access to that sub-resource.
	// Unmapping: Invalidate the pointer to a resource and re-enable the GPU's access to that resource.
	struct MapScope
	{
		ID3D12Resource* m_res;    // The resource to be locked
		UINT            m_sub;    // e.g. mip level (use 0 for V/I buffers)
		std::size_t     m_stride; // The size of each element (in bytes)
		Range           m_range;  // The range locked (while mapped), the range modified after Unmap
		void*           m_data;   // The pointer to the mapped resource data

		// Provide a 'dc' if you want to use a deferred context to lock the resource.
		// This will override the 'dc' passed to Map by the renderer manager classes (which will pass in the immediate DC)
		// Note: that to map a deferred context, you can only use write discard or write no overwrite.
		MapScope()
			:m_res()
			,m_sub()
			,m_stride()
			,m_range()
		{}
		MapScope(ID3D12Resource* res, UINT sub, size_t stride, Range range = RangeZero)
			:MapScope()
		{
			Map(res, sub, stride, range);
		}
		MapScope(MapScope const&) = delete;
		MapScope(MapScope&& rhs)
			:m_res(rhs.m_res)
			,m_sub(rhs.m_sub)
			,m_stride(rhs.m_stride)
			,m_range(rhs.m_range)
			,m_data(rhs.m_data)
		{
			rhs.m_res = nullptr;
			rhs.m_sub = 0;
			rhs.m_stride = 0;
			rhs.m_range = Range {};
			rhs.m_data = nullptr;
		}
		~MapScope()
		{
			Unmap();
		}

		// Returns a pointer to the mapped memory
		// SDK Notes: *Don't read from a sub-resource mapped for writing*
		//  When you pass D3D11_MAP_WRITE, D3D11_MAP_WRITE_DISCARD, or D3D11_MAP_WRITE_NO_OVERWRITE to the MapType parameter,
		//  you must ensure that your app does not read the sub-resource data to which the pData member of D3D11_MAPPED_SUBRESOURCE
		//  points because doing so can cause a significant performance penalty. The memory region to which pData points can be
		//  allocated with PAGE_WRITECOMBINE, and your app must honour all restrictions that are associated with such memory.
		// The SDK recommends using volatile pointers (but struct assignment for volatiles requires CV-qualified assignment operators)
		// Just don't read from ptr()...
		uint8_t const* data() const                { return byte_ptr(m_data) + m_stride * m_range.m_beg; }
		uint8_t*       data()                      { return byte_ptr(m_data) + m_stride * m_range.m_beg; }
		uint8_t const* end() const                 { return byte_ptr(m_data) + m_stride * m_range.m_end; }
		uint8_t*       end()                       { return byte_ptr(m_data) + m_stride * m_range.m_end; }
		template <typename T> T const* ptr() const { return type_ptr<T>(m_data) + m_range.m_beg; }
		template <typename T> T* ptr()             { return type_ptr<T>(m_data) + m_range.m_beg; }
		template <typename T> T const* end() const { return type_ptr<T>(m_data) + m_range.m_end; }
		template <typename T> T* end()             { return type_ptr<T>(m_data) + m_range.m_end; }

		//// For mapped textures
		//UINT RowPitch() const
		//{
		//	return type_ptr<D3D11_MAPPED_SUBRESOURCE>(this)->RowPitch;
		//}
		//UINT DepthPitch() const
		//{
		//	return type_ptr<D3D11_MAPPED_SUBRESOURCE>(this)->DepthPitch;
		//}

		// Maps a resource to CPU accessible memory
		// Only returns false if 'D3D11_MAP_FLAG_DO_NOT_WAIT' is used in 'flags'
		// Mapping a resource maps the entire thing. The 'range' and 'stride' parameters
		// just allow passing of the size of the mapped data around with the lock object.
		bool Map(ID3D12Resource* res, UINT sub, size_t stride, Range range)
		{
			PR_ASSERT(PR_DBG_RDR, m_res == nullptr, "Already mapped");

			// Do not wait means the caller expects the map to potentially not to work
			void* data;
			auto r = To<D3D12_RANGE>(range);
			Throw(res->Map(sub, &r, &data));

			// Update only on success
			m_res    = res;
			m_sub    = sub;
			m_stride = stride;
			m_range  = range;
			m_data   = data;
			return true;
		}
		void Unmap()
		{
			if (!m_res) return;
			D3D12_RANGE range;
			m_res->Unmap(m_sub, &range);
			m_range = To<Range>(range);
			m_data = nullptr;
			m_res = nullptr;
		}
	};
}
