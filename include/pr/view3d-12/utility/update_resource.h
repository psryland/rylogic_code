//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/main/renderer.h"
#include "pr/view3d-12/resource/resource_manager.h"
#include "pr/view3d-12/resource/gpu_upload_buffer.h"
#include "pr/view3d-12/utility/conversion.h"
#include "pr/view3d-12/utility/wrappers.h"
#include "pr/view3d-12/utility/barrier_batch.h"

namespace pr::rdr12
{
	// A scope object for updating data in a resource
	struct UpdateSubresourceScope
	{
		// Notes:
		//  - Updating model verts? Use Model::UpdateVertices() instead.
		//  - This class is a scope object for updating a sub resource (including mip maps).
		//    The usage is to create an instance of this class, call 'write' or directly fill
		//    the staging buffer via the 'ptr' methods, then call 'commit' to submit the update
		//    commands to the resource manager command list.
		//  - Treat this class like a transaction. It does nothing unless Commit() is called.
		//  - This class is used for both texture and buffer updates (which makes it a bit confusing).
		//  - Sub Resources are arranged in the following order:
		//      Resource:
		//        +- Plane Slice[0]
		//        |  +- Array Slice[0]
		//        |     +- Mip Slice[0]    (SubRexIdx: 0)
		//        |     +- Mip Slice[1]    (SubRexIdx: 1)
		//        |     +- Mip Slice[2]    (SubRexIdx: 2)
		//        |  +- Array Slice[1]
		//        |     +- Mip Slice[0]    (SubRexIdx: 3)
		//        |     +- Mip Slice[1]    (SubRexIdx: 4)
		//        |     +- Mip Slice[2]    (SubRexIdx: 5)
		//        |  +- Array Slice[2]
		//        |     +- Mip Slice[0]    (SubRexIdx: 6)
		//        |     +- Mip Slice[1]    (SubRexIdx: 7)
		//        |     +- Mip Slice[2]    (SubRexIdx: 8)
		//        +- Plane Slice[1]
		//        |  etc...
		//        |
		//    A Plane slice is for planar textures (like DXGI_FORMAT_NV12). Typical textures have only one plane.
		//    An Array slice is for texture arrays (like cubemaps). Typical textures have only one array slice. 3D textures only have one array slice
		//  - It's too complicated to handle all possible resource types (arrays, mips, planes, etc), so this class only handles single images (1D, 2D, or 3D)
		//    Array textures should use an UpdateSubresourceScope for each array slice.

		using footprints_t = pr::vector<D3D12_PLACED_SUBRESOURCE_FOOTPRINT, 16>;
		using staging_t = GpuUploadBuffer::Allocation;

		ResourceManager& m_res;          // The resource manager
		ID3D12Resource*  m_dest;         // The destination resource to be updated
		int              m_mip0, m_mipN; // The mipmap range to update. *Note* != the subresource index
		int              m_sub0;         // The subresource index of the 0th mip of the array slice
		int              m_alignment;    // The alignment requirements of the data in the upload buffer
		Box              m_range;        // The subrange to update within the resource (in elements not bytes (except for 1-D buffers), and always relative to mip 0)
		footprints_t     m_layout;       // The memory layout of the subresources within 'm_dest' start at mip 'm_sub0'
		staging_t        m_staging;      // The allocation within the upload buffer

		UpdateSubresourceScope(ResourceManager& res, ID3D12Resource* dest, int alignment, int first = 0, int range = limits<int>::max())
			: UpdateSubresourceScope(res, dest, 0, 0, 1, alignment, { first, 0, 0 }, { range, 1, 1 })
		{}
		UpdateSubresourceScope(ResourceManager& res, ID3D12Resource* dest, int array_slice, int mip0, int mipN, int alignment, iv3 first = iv3::Zero(), iv3 range = iv3::Max())
			: m_res(res)
			, m_dest(dest)
			, m_mip0(mip0)
			, m_mipN(mipN)
			, m_sub0()
			, m_alignment(alignment)
			, m_range(first, range)
			, m_layout()
			, m_staging()
		{
			auto device = m_res.rdr().D3DDevice();

			// Check buffer types. Buffers don't have mip levels or array slices
			auto ddesc = dest->GetDesc();
			if (m_mip0 < 0 || m_mip0 >= ddesc.MipLevels || m_mipN < 0 || m_mip0 + m_mipN > ddesc.MipLevels)
				throw std::runtime_error("Mip range is out of bounds for this texture");
			if (ddesc.Dimension == D3D12_RESOURCE_DIMENSION_BUFFER && (m_mip0 != 0 || m_mipN != 1 || array_slice != 0))
				throw std::runtime_error("Destination resource is a buffer, but sub-resource range is given");
			if (ddesc.Dimension == D3D12_RESOURCE_DIMENSION_TEXTURE3D && (array_slice != 0))
				throw std::runtime_error("Arrays of 3D textures are not supported");
			if (ddesc.Dimension != D3D12_RESOURCE_DIMENSION_TEXTURE3D && (array_slice < 0 || array_slice >= ddesc.DepthOrArraySize))
				throw std::runtime_error("Array slice is out of bounds for this texture");

			// Clip the range to the destination resource dimensions
			m_range.Clip(iv3::Zero(), ddesc.Dimension == D3D12_RESOURCE_DIMENSION_TEXTURE3D
				? iv3(s_cast<int>(ddesc.Width), s_cast<int>(ddesc.Height), s_cast<int>(ddesc.DepthOrArraySize))
				: iv3(s_cast<int>(ddesc.Width), s_cast<int>(ddesc.Height), 1));

			// If the update volume is clipped away, then there's nothing to do
			if (range.x == 0 || range.y == 0 || range.z == 0)
				return;

			// Calculate the sub resource range spanned by [m_mip0, m_mip0 + m_mipN)
			auto array_length = ddesc.Dimension == D3D12_RESOURCE_DIMENSION_TEXTURE3D ? 1 : ddesc.DepthOrArraySize;
			m_sub0 = SubResIndex(0, ddesc.MipLevels, array_slice, array_length); // Subresource index of the array slice

			// Get the size and footprints for copying subresources.
			m_layout.resize(m_mipN);
			device->GetCopyableFootprints(&ddesc, m_sub0 + m_mip0, m_mipN, 0ULL, m_layout.data(), nullptr, nullptr, nullptr);

			// Determine the total size of the staging memory needed to hold the update data
			auto total_size = 0LL;
			for (auto i = 0; i != m_mipN; ++i)
			{
				auto const& footprint = m_layout[i].Footprint;

				// The size of the box at mip 'i' (in elements, not bytes)
				auto size_at_mip = m_range.size(m_mip0 + i);
				total_size += footprint.RowPitch * size_at_mip.y * size_at_mip.z;
			}

			// Get a staging buffer big enough for all of the subresources.
			// The staging buffer is just big enough to contain the range to be updated, not the full resource.
			// If 'dest' is a volume texture, and [first, range) is box within that texture, then the size of the
			// staging buffer is just the size of the box + subsequent mip levels.
			m_staging = m_res.m_upload_buffer.Alloc(total_size, m_alignment);

			// 'GetCopyableFootprints' returns values relative to the start of the staging resource, but,
			// 'staging' is an allocation *within* the staging resource, so we need to adjust the Offset values.
			for (auto i = 0; i != m_mipN; ++i)
				m_layout[i].Offset += m_staging.m_ofs;
		}
		UpdateSubresourceScope(UpdateSubresourceScope&& rhs) noexcept
			: m_res(rhs.m_res)
			, m_dest(std::move(rhs.m_dest))
			, m_mip0(rhs.m_mip0)
			, m_mipN(rhs.m_mipN)
			, m_sub0(rhs.m_sub0)
			, m_alignment(rhs.m_alignment)
			, m_range(std::move(rhs.m_range))
			, m_layout(std::move(rhs.m_layout))
			, m_staging(std::move(rhs.m_staging))
		{
			// Shouldn't need to worry about Commit() being called on a moved-from object.
		}
		UpdateSubresourceScope(UpdateSubresourceScope const&) = delete;
		UpdateSubresourceScope& operator =(UpdateSubresourceScope&& rhs) = delete;
		UpdateSubresourceScope& operator =(UpdateSubresourceScope const&) = delete;
		~UpdateSubresourceScope() = default;

		// Return a pointer to the staging buffer memory for the given mip level.
		// 'mip' is relative to 'm_mip0' that was passed to the constructor.
		template <typename TElement> TElement* ptr(int mip = 0) const
		{
			// Make sure the mip is within the range of mips being updated
			if (mip < 0 || mip >= m_mipN)
				throw std::runtime_error("Mip level out of range for this update");

			// The mip in the staging buffer
			auto const& layout = m_layout[mip];
			return type_ptr<TElement>(m_staging.m_mem + layout.Offset);
		}
		template <typename TElement> TElement* ptr(iv3 pos, int mip = 0) const
		{
			// The z slice in the staging buffer
			auto slice = byte_ptr(ptr(mip));

			// The dimensions of the range being updated
			auto const& dim = m_range.size(mip);
			if (pos.x < 0 || pos.x >= dim.x ||
				pos.y < 0 || pos.y >= dim.y ||
				pos.z < 0 || pos.z >= dim.z)
				throw std::runtime_error("Position out of range for this update");

			// Return a pointer to the position on the mip
			auto const& layout = m_layout[mip];
			return type_ptr<TElement>(
				slice + 
				(layout.Footprint.RowPitch * layout.Footprint.Height) * pos.z +
				(layout.Footprint.RowPitch) * pos.y +
				sizeof(TElement) * pos.x);
		}

		// Return a pointer to the end of the staging buffer memory for the given mip level.
		// 'mip' is relative to 'm_mip0' that was passed to the constructor.
		template <typename TElement> TElement* end(int mip = 0) const
		{
			auto const& layout = m_layout[mip];
			return ptr<TElement>(mip) + layout.Footprint.RowPitch * layout.Footprint.Height * layout.Footprint.Depth;
		}

		// Copy data from the given images to the staging buffer. Each image is a mip.
		void Write(std::span<Image const> images)
		{
			// Note: 'images' is an image for each mip level. The images must be 1D, 2D, or 3D textures and *NOT* texture arrays.
			// This object only handles a single array slice. So, if the destination resource is a texture array, then the caller
			// must call this function once for each array slice.

			// There must be one image per mip level being updated
			if (s_cast<int>(images.size()) != m_mipN)
				throw std::runtime_error("Insufficient image data provided");

			// Copy data from 'images' into the staging buffer
			for (auto i = 0; i != m_mipN; ++i)
			{
				auto const& image = images[i];                  // The initialisation data for the subresource at mip level 'm_mip0 + i'.
				auto const& layout = m_layout[i];               // The dimensions of the subresource within 'm_dest' at 'sub0 + m_mip0 + i'.
				auto size = m_range.size(m_mip0 + i);           // The size of the range to be updated (in both m_dest and m_staging) at 'm_mip0 + i'
				auto staging = m_staging.m_mem + layout.Offset; // The pointer to the start of the mip in staging buffer memory

				if (size.z != image.m_dim.z)
					throw std::runtime_error("Image size mismatch (depth)");
				if (size.y != image.m_dim.y)
					throw std::runtime_error("Image size mismatch (height)");
				if (size.x != image.m_dim.x && image.m_format != DXGI_FORMAT_R8_UNORM)
					throw std::runtime_error("Image size mismatch (width)");
				if (size.x != image.m_pitch.x && image.m_format == DXGI_FORMAT_R8_UNORM)
					throw std::runtime_error("Image size mismatch (pitch)");
				if (image.m_pitch.x > s_cast<int>(layout.Footprint.RowPitch))
					throw std::runtime_error("Image size mismatch (row pitch)");

				// Copy from 'image' to the staging resource. Remember, 'image' and the staging buffer are logically
				// the same size and represent a box within 'm_dest'. So, no position offset is needed here, we only
				// need the position in the final UpdateResource call.
				for (auto z = 0; z != size.z; ++z)
				{
					// The minimum row pitch for the staging memory seems to be 256 bytes. So don't assume
					// bytes_per_element = Footprint.RowPitch / dim.x. This is why we need to copy row by row.
					// 'layout.Offset' is the byte offset from the start of 'm_dest' to the current mip.
					auto src_slice = image.Slice(z);
					auto dst_slice = staging + (layout.Footprint.RowPitch * layout.Footprint.Height) * z;

					// Copy each row of the slice
					for (auto y = 0; y != size.y; ++y)
					{
						memcpy(
							dst_slice + layout.Footprint.RowPitch * y,
							src_slice.m_data.bptr + image.m_pitch.x * y,
							image.m_pitch.x);
					}
				}
			}
		}
		void Write(Image const& image)
		{
			Write({ &image, 1 });
		}

		// Submit the command to the command list.
		// 'final_state' is the state to transition the resource to after the update.
		// Set this to 'std::nullopt' to manage the resource state outside of this class.
		void Commit(std::optional<D3D12_RESOURCE_STATES> final_state)
		{
			BarrierBatch barriers(m_res.m_gfx_cmd_list);
			auto ddesc = m_dest->GetDesc();

			// Add the command to copy from the staging resource to the destination resource
			if (ddesc.Dimension == D3D12_RESOURCE_DIMENSION_BUFFER)
			{
				barriers.Transition(m_dest, D3D12_RESOURCE_STATE_COPY_DEST);
				barriers.Commit();

				auto pos = m_range.pos(m_mip0);
				auto size = m_range.size(m_mip0);
				m_res.m_gfx_cmd_list.CopyBufferRegion(m_dest, s_cast<UINT64>(pos.x), m_staging.m_buf, m_staging.m_ofs, s_cast<UINT64>(size.x) * 1);
			}
			else
			{
				for (auto i = 0; i != m_mipN; ++i)
				{
					auto const& layout = m_layout[i];
					auto box = m_range.mip(m_mip0 + i);

					barriers.Transition(m_dest, D3D12_RESOURCE_STATE_COPY_DEST, s_cast<uint32_t>(m_sub0 + m_mip0 + i));
					barriers.Commit();

					D3D12_TEXTURE_COPY_LOCATION src =
					{
						.pResource = m_staging.m_buf,
						.Type = D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT,
						.PlacedFootprint = layout,
					};
					D3D12_TEXTURE_COPY_LOCATION dst =
					{
						.pResource = m_dest,
						.Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX,
						.SubresourceIndex = s_cast<UINT>(m_sub0 + m_mip0 + i),
					};
					m_res.m_gfx_cmd_list.CopyTextureRegion(&dst, s_cast<UINT>(box.left), s_cast<UINT>(box.top), s_cast<UINT>(box.front), &src, &box);
				}
			}

			// Transition the resource to the final state (if provided)
			if (final_state)
			{
				barriers.Transition(m_dest, *final_state);
				barriers.Commit();
			}

			// Signal that the resource manager command list needs executing
			m_res.m_flush_required = true;
		}

		// Return the sub resource index for the given mip level, array slice, and plane slice
		constexpr static int SubResIndex(int mip, int mip_count, int array_slice, int array_length, int plane_slice = 0)
		{
			return mip + mip_count * (array_slice + array_length * plane_slice);
		}
	};
}