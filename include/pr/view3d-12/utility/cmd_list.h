//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/utility/gpu_sync.h"
#include "pr/view3d-12/utility/cmd_alloc.h"
#include "pr/view3d-12/resource/resource_state.h"

namespace pr::rdr12
{
	// Forward declare the pool
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
		using ICommandList = std::conditional_t<ListType == D3D12_COMMAND_LIST_TYPE_DIRECT, ID3D12GraphicsCommandList, ID3D12CommandList>;

	private:

		using pool_t = typename CmdListPool<ListType>;
		using res_states_t = struct
		{
			pr::vector<ID3D12Resource*> m_res;
			pr::vector<ResState> m_state;
		};

	private:

		D3DPtr<ICommandList> m_list; // The interface for buffering GPU commands.
		res_states_t m_resources_states;
		std::thread::id m_thread_id; // The thread id of the thread that called Reset.
		pool_t* m_pool;              // The pool to return this allocator too. (can be null)

	public:

		CmdList()
			: m_list()
			, m_thread_id(std::this_thread::get_id())
			, m_pool()
		{}
		CmdList(D3DPtr<ICommandList> list, pool_t* pool)
			: m_list(list)
			, m_thread_id(std::this_thread::get_id())
			, m_pool(pool)
		{}
		CmdList(CmdList&&) = default;
		CmdList(CmdList const&) = delete;
		CmdList& operator =(CmdList&& rhs)
		{
			// 'default' is member-wise std::move, not swap.
			// We need swap so that replaced values return to the pool.
			if (&rhs == this) return *this;
			std::swap(m_list, rhs.m_list);
			std::swap(m_thread_id, rhs.m_thread_id);
			std::swap(m_pool, rhs.m_pool);
			return *this;
		}
		CmdList& operator =(CmdList const&) = delete;
		~CmdList()
		{
			if (m_pool != nullptr && m_list != nullptr)
				m_pool->Return(*this);
		}

		// Reset the command list to use the given allocator
		void Reset(GfxCmdAlloc& alloc)
		{
			// Create an open command list for use by the resource manager, since it is independent of any windows or scenes.
			Throw(device->CreateCommandList(0, D3D12_COMMAND_LIST_TYPE_DIRECT, m_gfx_cmd_alloc, nullptr, __uuidof(ID3D12GraphicsCommandList), (void**)&m_gfx_cmd_list.m_ptr));
			m_gfx_cmd_list->SetName(L"ResManCmdListGfx");

		}
		// Set the ID of the thread using this command list
		void UseThisThread()
		{
			m_thread_id = std::this_thread::get_id();
		}

		// Access the list
		ICommandList* operator ->() const
		{
			ThrowOnCrossThreadUse();
			return m_list.get();
		}

		// Convert to the list pointer
		operator ICommandList* () const
		{
			ThrowOnCrossThreadUse();
			return m_list.get();
		}

	private:

		// Ensure calls are from the thread that owns this command list
		void ThrowOnCrossThreadUse()
		{
			if (std::this_thread::get_id() != m_thread_id)
				throw std::runtime_error("Cross thread use of a command list");
		}
	};

	// A pool of command lists
	template <D3D12_COMMAND_LIST_TYPE ListType>
	struct CmdListPool
	{
		// Notes:
		//  - The pool manages recycling command lists.
		//  - It basically just reduces allocations.

		using ICommandList = std::conditional_t<ListType == D3D12_COMMAND_LIST_TYPE_DIRECT, ID3D12GraphicsCommandList, ID3D12CommandList>;
		using pool_t = pr::vector<CmdList<ListType>, 16, false>;

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
		CmdList<ListType> Get()
		{
			// Create a new command list if there isn't one available
			if (m_pool.empty())
			{
				// Create a command list for internal use by the window
				D3DPtr<ICommandList> cmd_list;
				auto device4 = m_gsync->D3DDevice();
				Throw(device4->CreateCommandList1(0, ListType, D3D12_COMMAND_LIST_FLAG_NONE, __uuidof(ICommandList), (void**)&cmd_list.m_ptr));
				Throw(cmd_list->SetName(L"CmdListPool:CmdList"));
				m_pool.push_back(CmdList<ListType>(cmd_list, nullptr));
			}

			// Get a command list from the pool
			auto list = std::move(m_pool.back());
			m_pool.pop_back();
			list.m_pool = this;
			return std::move(list);
		}

		// Return the list to the pool
		void Return(CmdList<ListType>& cmd_list)
		{
			PR_ASSERT(PR_DBG_RDR, m_gsync != nullptr, "This pool has already been destructed");
			PR_ASSERT(PR_DBG_RDR, cmd_list != nullptr, "Don't add null lists to the pool");
			PR_ASSERT(PR_DBG_RDR, cmd_list.m_pool == this, "Returned object didn't come from this pool");
			cmd_list.m_pool = nullptr;
			m_pool.push_back(std::move(cmd_list));
		}
	};

	// Flavours
	using GfxCmdListPool = CmdListPool<D3D12_COMMAND_LIST_TYPE_DIRECT>;
	using ComCmdListPool = CmdListPool<D3D12_COMMAND_LIST_TYPE_COMPUTE>;
	using GfxCmdList = CmdList<D3D12_COMMAND_LIST_TYPE_DIRECT>;
	using ComCmdList = CmdList<D3D12_COMMAND_LIST_TYPE_COMPUTE>;
}