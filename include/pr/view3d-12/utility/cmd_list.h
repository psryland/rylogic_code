//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/resource/resource_state_store.h"
#include "pr/view3d-12/resource/gpu_transfer_buffer.h"
#include "pr/view3d-12/utility/cmd_alloc.h"
#include "pr/view3d-12/utility/wrappers.h"
#include "pr/view3d-12/utility/utility.h"

namespace pr::rdr12
{
	template <typename Idx> concept RootParamIdx = std::integral<Idx> || std::integral<std::underlying_type_t<Idx>>;

	// Forward declaration
	template <D3D12_COMMAND_LIST_TYPE ListType>
	struct CmdListPool;

	// A command list instance
	template <D3D12_COMMAND_LIST_TYPE ListType>
	struct CmdList
	{
		// Notes:
		//  - One list per thread.
		//  - Each allocator can only be recording one command list at a time.
		//  - Only reset allocator when the GPU has finished with the list (see GpuSync).
		//  - Can reset a command list immediately after executing it, but it has to use a different allocator.
		//  - Need to wrap the command list because state (such as Resource States) needs to be buffered per command list.
		//  - Not storing a ref to a cmd_alloc_pool in here, because not all CmdLists are used where there is an allocator pool.

		using ICommandList = std::conditional_t<ListType == D3D12_COMMAND_LIST_TYPE_DIRECT, ID3D12GraphicsCommandList, ID3D12GraphicsCommandList>;

	private:

		friend CmdListPool<ListType>;
		using alloc_t = typename CmdAlloc<ListType>;
		using pool_t = typename CmdListPool<ListType>;

	private:

		D3DPtr<ICommandList> m_list;     // The interface for buffering GPU commands.
		alloc_t m_cmd_allocator;         // The current allocator in use by this cmd list.
		std::thread::id m_thread_id;     // The thread id of the thread that called Reset.
		ResStateStore m_res_state;       // Track the state of resources used in this command list
		pool_t* m_pool;                  // The pool to return this list too. (can be null)

		CmdList(D3DPtr<ICommandList> list, alloc_t&& cmd_alloc, pool_t* pool)
			: m_list(list)
			, m_cmd_allocator(std::move(cmd_alloc))
			, m_thread_id(std::this_thread::get_id())
			, m_pool(pool)
		{}

	public:

		CmdList(ID3D12Device4* device, pool_t* pool, char const* name, Colour32 pix_colour)
			: CmdList(nullptr, {}, pool)
		{
			// Create an instance of a cmd list with no allocator assigned yet
			Check(device->CreateCommandList1(0, ListType, D3D12_COMMAND_LIST_FLAG_NONE, __uuidof(ICommandList), (void**)&m_list.m_ptr));
			if (name) DebugName(m_list, name);
			DebugColour(m_list, pix_colour);
		}
		CmdList(ID3D12Device4* device, alloc_t&& cmd_alloc, pool_t* pool, char const* name, Colour32 pix_colour)
			: CmdList(nullptr, std::forward<alloc_t>(cmd_alloc), pool)
		{
			// Create an instance of an open cmd list based on 'cmd_alloc'
			Check(device->CreateCommandList(0, ListType, m_cmd_allocator, nullptr, __uuidof(ICommandList), (void**)&m_list.m_ptr));
			if (name) DebugName(m_list, name);
			DebugColour(m_list, pix_colour);

			// The command list is open, so start the pix event
			PIXBeginEvent(m_list.get(), DebugColour(m_list).argb, DebugName(m_list));
		}

		CmdList(CmdList&& rhs) = default;
		CmdList(CmdList const&) = delete;
		CmdList& operator =(CmdList&& rhs)
		{
			if (&rhs == this) return *this;

			// Return this to the pool before replacing it with 'rhs'
			if (m_pool != nullptr && m_list != nullptr)
				m_pool->Return(std::move(*this));

			// Replace this with 'rhs'
			m_list = std::move(rhs.m_list);
			m_cmd_allocator = std::move(rhs.m_cmd_allocator);
			m_thread_id = std::move(rhs.m_thread_id);
			m_pool = std::move(rhs.m_pool);
			return *this;
		}
		CmdList& operator =(CmdList const&) = delete;
		~CmdList()
		{
			if (m_pool != nullptr && m_list != nullptr)
				m_pool->Return(std::move(*this));
		}

		// Set the ID of the thread using this command list
		void UseThisThread()
		{
			m_thread_id = std::this_thread::get_id();
		}

		// Access the list
		ICommandList* get() const
		{
			ThrowOnCrossThreadUse();
			return m_list.get();
		}
		operator ICommandList* () const
		{
			return get();
		}

		// Get the current state (according to this command list) of a resource
		ResStateData& ResState(ID3D12Resource const* res)
		{
			return m_res_state.Get(res);
		}

		// Assign the shader pipeline state to the command list
		void SetPipelineState(ID3D12PipelineState* pipeline_state)
		{
			ThrowOnCrossThreadUse();
			m_list->SetPipelineState(pipeline_state);
		}

		// Assign the descriptor heaps to the command list
		void SetDescriptorHeaps(std::span<ID3D12DescriptorHeap* const> heaps)
		{
			m_list->SetDescriptorHeaps(s_cast<UINT>(heaps.size()), heaps.data());
		}

		// Copy resource data from source to destination
		void CopyResource(ID3D12Resource* destination, ID3D12Resource* source)
		{
			ThrowOnCrossThreadUse();
			m_list->CopyResource(destination, source);
		}

		// Copy a region within a resource
		void CopyBufferRegion(ID3D12Resource *pDstBuffer, UINT64 DstOffset, ID3D12Resource *pSrcBuffer, UINT64 SrcOffset, UINT64 NumBytes)
		{
			m_list->CopyBufferRegion(pDstBuffer, DstOffset, pSrcBuffer, SrcOffset, NumBytes);
		}
		void CopyBufferRegion(GpuTransferAllocation DstBuffer, ID3D12Resource *pSrcBuffer, UINT64 SrcOffset = 0)
		{
			CopyBufferRegion(DstBuffer.m_res, DstBuffer.m_ofs, pSrcBuffer, SrcOffset, DstBuffer.m_size);
		}
		void CopyBufferRegion(ID3D12Resource *DstBuffer, UINT64 DstOffset, GpuTransferAllocation pSrcBuffer)
		{
			CopyBufferRegion(DstBuffer, DstOffset, pSrcBuffer.m_res, pSrcBuffer.m_ofs, pSrcBuffer.m_size);
		}

		// Copy a region within a texture
		void CopyTextureRegion(D3D12_TEXTURE_COPY_LOCATION const* pDst, UINT DstX, UINT DstY, UINT DstZ, D3D12_TEXTURE_COPY_LOCATION const* pSrc, D3D12_BOX const* pSrcBox)
		{
			m_list->CopyTextureRegion(pDst, DstX, DstY, DstZ, pSrc, pSrcBox);
		}

		// Add a resource barrier to the command list
		void ResourceBarrier(D3D12_RESOURCE_BARRIER const& barriers)
		{
			ResourceBarrier(std::span<D3D12_RESOURCE_BARRIER const>(&barriers, 1));
		}
		void ResourceBarrier(std::span<D3D12_RESOURCE_BARRIER const> barriers)
		{
			ThrowOnCrossThreadUse();
			m_list->ResourceBarrier(s_cast<UINT>(barriers.size()), barriers.data());
		}

		// Mark the command list as closed
		void Close()
		{
			ThrowOnCrossThreadUse();
			if constexpr (ListType == D3D12_COMMAND_LIST_TYPE_DIRECT)
			{
				RestoreResourceStateDefaults(*this);
			}
			PIXEndEvent(m_list.get());
			m_list->Close();
		}

		// Set the sync point for when the GPU is finished with this command list
		void SyncPoint(uint64_t sync_point)
		{
			// This only affects the allocator, so the command list can be used again after 'Reset' is called.
			ThrowOnCrossThreadUse();
			m_cmd_allocator.m_sync_point = sync_point; // Can't use this allocator until the GPU has completed 'sync_point'
		}

		// Reset the command list
		void Reset(alloc_t&& cmd_alloc, ID3D12PipelineState* pipeline_state = nullptr)
		{
			ThrowOnCrossThreadUse();
			m_cmd_allocator = std::move(cmd_alloc);
			Check(m_list->Reset(m_cmd_allocator, pipeline_state));
			m_res_state.Reset();

			PIXBeginEvent(m_list.get(), DebugColour(m_list).argb, DebugName(m_list));
		}
	
	public : // Graphics

		// Set the signature for the command list
		void SetGraphicsRootSignature(ID3D12RootSignature* signature)
		{
			ThrowOnCrossThreadUse();
			m_list->SetGraphicsRootSignature(signature);
		}

		// Reset a render target to a single colour
		void ClearRenderTargetView(D3D12_CPU_DESCRIPTOR_HANDLE RenderTargetView, FLOAT const ColorRGBA[4], std::span<D3D12_RECT const> rects = {})
		{
			m_list->ClearRenderTargetView(RenderTargetView, ColorRGBA, s_cast<UINT>(rects.size()), rects.data());
		}

		// Reset a depth stencil to a single value
		void ClearDepthStencilView(D3D12_CPU_DESCRIPTOR_HANDLE DepthStencilView, D3D12_CLEAR_FLAGS ClearFlags, FLOAT Depth, int Stencil, std::span<D3D12_RECT const> rects = {})
		{
			m_list->ClearDepthStencilView(DepthStencilView, ClearFlags, Depth, s_cast<UINT8>(Stencil), s_cast<UINT>(rects.size()), rects.data());
		}

		// Bind the render target and depth buffer to the command list
		void OMSetRenderTargets(std::span<D3D12_CPU_DESCRIPTOR_HANDLE const> RenderTargetDescriptors, bool RTsSingleHandleToDescriptorRange, D3D12_CPU_DESCRIPTOR_HANDLE const* pDepthStencilDescriptor)
		{
			m_list->OMSetRenderTargets(s_cast<UINT>(RenderTargetDescriptors.size()), RenderTargetDescriptors.data(), RTsSingleHandleToDescriptorRange, pDepthStencilDescriptor);
		}

		// Set the viewports
		void RSSetViewports(std::span<Viewport const> viewports)
		{
			m_list->RSSetViewports(s_cast<UINT>(viewports.size()), viewports.data());
		}

		// Set the scissor rects
		void RSSetScissorRects(std::span<D3D12_RECT const> rects)
		{
			m_list->RSSetScissorRects(s_cast<UINT>(rects.size()), rects.data());
		}

		// Set the primitive topology
		void IASetPrimitiveTopology(ETopo PrimitiveTopology)
		{
			m_list->IASetPrimitiveTopology(To<D3D12_PRIMITIVE_TOPOLOGY>(PrimitiveTopology));
		}

		// Set the vertex buffers
		void IASetVertexBuffers(UINT StartSlot, std::span<D3D12_VERTEX_BUFFER_VIEW const> views)
		{
			m_list->IASetVertexBuffers(StartSlot, s_cast<UINT>(views.size()), views.data());
		}

		// Set the index buffer
		void IASetIndexBuffer(D3D12_INDEX_BUFFER_VIEW const* view)
		{
			m_list->IASetIndexBuffer(view);
		}

		// Set a compute shader's root parameter descriptor table
		template <RootParamIdx Idx>
		void SetGraphicsRootDescriptorTable(Idx RootParameterIndex, D3D12_GPU_DESCRIPTOR_HANDLE descriptor)
		{
			m_list->SetGraphicsRootDescriptorTable(s_cast<UINT>(RootParameterIndex), descriptor);
		}

		// Dispatch a draw shader
		void DrawInstanced(size_t VertexCountPerInstance, size_t InstanceCount, size_t StartVertexLocation, size_t StartInstanceLocation)
		{
			m_list->DrawInstanced(
				s_cast<UINT>(VertexCountPerInstance), s_cast<UINT>(InstanceCount),
				s_cast<UINT>(StartVertexLocation), s_cast<UINT>(StartInstanceLocation));
		}

		// Dispatch a draw shader
		void DrawIndexedInstanced(size_t IndexCountPerInstance, size_t InstanceCount, size_t StartIndexLocation, ptrdiff_t BaseVertexLocation, size_t StartInstanceLocation)
		{
			m_list->DrawIndexedInstanced(
				s_cast<UINT>(IndexCountPerInstance), s_cast<UINT>(InstanceCount),
				s_cast<UINT>(StartIndexLocation), s_cast<INT>(BaseVertexLocation), s_cast<UINT>(StartInstanceLocation));
		}

		// Resolve an MSAA texture to a non-MSAA texture
		void ResolveSubresource(ID3D12Resource* pDstResource, ID3D12Resource* pSrcResource, DXGI_FORMAT Format)
		{
			m_list->ResolveSubresource(pDstResource, 0U, pSrcResource, 0U, Format);
		}
		void ResolveSubresource(ID3D12Resource* pDstResource, int DstSubresource, ID3D12Resource* pSrcResource, int SrcSubresource, DXGI_FORMAT Format)
		{
			m_list->ResolveSubresource(pDstResource, s_cast<UINT>(DstSubresource), pSrcResource, s_cast<UINT>(SrcSubresource), Format);
		}

	public: // Compute
	
		// Assign the shader root signature to the command list
		void SetComputeRootSignature(ID3D12RootSignature* signature)
		{
			ThrowOnCrossThreadUse();
			m_list->SetComputeRootSignature(signature);
		}

		// Set a compute shader's root parameter constant
		template <RootParamIdx Idx>
		void SetComputeRoot32BitConstant(Idx RootParameterIndex, F32U32 SrcData, UINT DestOffsetIn32BitValues = 0)
		{
			m_list->SetComputeRoot32BitConstant(s_cast<UINT>(RootParameterIndex), SrcData.u32, DestOffsetIn32BitValues);
		}

		// Set a contiguous set of root parameter constants
		template <RootParamIdx Idx>
		void SetComputeRoot32BitConstants(Idx RootParameterIndex, int Num32BitValuesToSet, void const* pSrcData, size_t DestOffsetIn32BitValues = 0)
		{
			m_list->SetComputeRoot32BitConstants(s_cast<UINT>(RootParameterIndex), s_cast<UINT>(Num32BitValuesToSet), pSrcData, s_cast<UINT>(DestOffsetIn32BitValues));
		}
		template <RootParamIdx Idx, typename CBufType>
		void SetComputeRoot32BitConstants(Idx RootParameterIndex, CBufType const& cb, size_t DestOffsetIn32BitValues = 0)
		{
			static_assert(sizeof(CBufType) % sizeof(uint32_t) == 0);
			auto count = s_cast<int>(sizeof(CBufType) / sizeof(uint32_t));
			SetComputeRoot32BitConstants(RootParameterIndex, count, &cb, s_cast<UINT>(DestOffsetIn32BitValues));
		}

		// Set a GPU descriptor handle for a constant buffer view
		template <RootParamIdx Idx>
		void SetComputeRootConstantBufferView(Idx RootParameterIndex, D3D12_GPU_VIRTUAL_ADDRESS BufferLocation)
		{
			m_list->SetComputeRootConstantBufferView(s_cast<UINT>(RootParameterIndex), BufferLocation);
		}

		// Sets a GPU descriptor handle for the unordered-access-view resource in the compute root signature.
		template <RootParamIdx Idx>
		void SetComputeRootUnorderedAccessView(Idx RootParameterIndex, D3D12_GPU_VIRTUAL_ADDRESS buffer_address)
		{
			m_list->SetComputeRootUnorderedAccessView(s_cast<UINT>(RootParameterIndex), buffer_address);
		}

		// Sets a GPU descriptor handle for the shared-resource-view resource in the compute root signature.
		template <RootParamIdx Idx>
		void SetComputeRootShaderResourceView(Idx RootParameterIndex, D3D12_GPU_VIRTUAL_ADDRESS buffer_address)
		{
			m_list->SetComputeRootShaderResourceView(s_cast<UINT>(RootParameterIndex), buffer_address);
		}

		// Set a compute shader's root parameter descriptor table
		template <RootParamIdx Idx>
		void SetComputeRootDescriptorTable(Idx RootParameterIndex, D3D12_GPU_DESCRIPTOR_HANDLE  descriptor)
		{
			m_list->SetComputeRootDescriptorTable(s_cast<UINT>(RootParameterIndex), descriptor);
		}

		// Dispatch a compute shader
		void Dispatch(int ThreadGroupCountX, int ThreadGroupCountY, int ThreadGroupCountZ)
		{
			m_list->Dispatch(s_cast<UINT>(ThreadGroupCountX), s_cast<UINT>(ThreadGroupCountY), s_cast<UINT>(ThreadGroupCountZ));
		}
		void Dispatch(iv3 ThreadGroupCount)
		{
			m_list->Dispatch(s_cast<UINT>(ThreadGroupCount.x), s_cast<UINT>(ThreadGroupCount.y), s_cast<UINT>(ThreadGroupCount.z));
		}

	private:

		// Ensure calls are from the thread that owns this command list
		void ThrowOnCrossThreadUse() const
		{
			if (std::this_thread::get_id() != m_thread_id)
				throw std::runtime_error("Cross thread use of a command list");
		}

		// Restores the resource state of resources used in this command list to their default state
		friend void RestoreResourceStateDefaults(CmdList<D3D12_COMMAND_LIST_TYPE_DIRECT>& cmd_list);
	};

	// A pool of command lists
	template <D3D12_COMMAND_LIST_TYPE ListType>
	struct CmdListPool
	{
		// Notes:
		//  - The pool manages recycling command lists.
		//  - It basically just reduces allocations.

		using ICommandList = std::conditional_t<ListType == D3D12_COMMAND_LIST_TYPE_DIRECT, ID3D12GraphicsCommandList, ID3D12CommandList>;
		using cmd_list_t = CmdList<ListType>;
		using pool_t = pr::vector<cmd_list_t, 16, false>;

		GpuSync* m_gsync;
		pool_t m_pool;

		explicit CmdListPool(GpuSync& gsync)
			:m_gsync(&gsync)
			,m_pool()
		{}
		CmdListPool(CmdListPool&&) = default;
		CmdListPool(CmdListPool const&) = delete;
		CmdListPool& operator =(CmdListPool&&) = default;
		CmdListPool& operator =(CmdListPool const&) = delete;
		~CmdListPool()
		{
			m_gsync = nullptr; // Used to catch return to destructed pool
		}

		// Get a command list that returns to the pool when out of scope
		cmd_list_t Get()
		{
			// Create a new command list if there isn't one available
			if (m_pool.empty())
				m_pool.push_back(cmd_list_t(m_gsync->D3DDevice(), nullptr, L"CmdListPool:CmdList"));

			// Get a command list from the pool
			auto list = std::move(m_pool.back());
			m_pool.pop_back();
			list.m_pool = this;
			return list;
		}

		// Return the list to the pool
		void Return(cmd_list_t&& cmd_list)
		{
			PR_ASSERT(PR_DBG_RDR, m_gsync != nullptr, "This pool has already been destructed");
			PR_ASSERT(PR_DBG_RDR, cmd_list != nullptr, "Don't add null lists to the pool");
			PR_ASSERT(PR_DBG_RDR, cmd_list.m_pool == this, "Returned object didn't come from this pool");
			cmd_list.m_pool = nullptr;
			m_pool.push_back(std::move(cmd_list));
		}
	};

	// Flavours
	using GfxCmdList = CmdList<D3D12_COMMAND_LIST_TYPE_DIRECT>;
	using ComCmdList = CmdList<D3D12_COMMAND_LIST_TYPE_COMPUTE>;
	using GfxCmdListPool = CmdListPool<D3D12_COMMAND_LIST_TYPE_DIRECT>;
	using ComCmdListPool = CmdListPool<D3D12_COMMAND_LIST_TYPE_COMPUTE>;

}