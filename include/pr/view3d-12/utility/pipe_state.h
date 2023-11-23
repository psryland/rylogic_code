//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12
{
	// Notes:
	//  - The enum values 'EPipeState' are encoded with (offset, size) pairs, corresponding to the
	//    parts of the PSO description that can be changed. Add as required.

	// Parts of the pipeline state that can be changed.
	#define PR_RDR_PIPE_STATE_FIELDS(x)/*(EPipeState::<name>, PipelineStateDesc::<field>) */\
		x(RootSignature         , pRootSignature                        )\
		x(VS                    , VS                                    )\
		x(PS                    , PS                                    )\
		x(DS                    , DS                                    )\
		x(HS                    , HS                                    )\
		x(GS                    , GS                                    )\
		x(TopologyType          , PrimitiveTopologyType                 )\
		x(FillMode              , RasterizerState.FillMode              )\
		x(CullMode              , RasterizerState.CullMode              )\
		x(DepthEnable           , DepthStencilState.DepthEnable         )\
		x(DepthWriteMask        , DepthStencilState.DepthWriteMask      )\
		x(DepthFunc             , DepthStencilState.DepthFunc           )\
		x(BlendState0           , BlendState.RenderTarget[0]            )\
		x(RTVFormats            , RTVFormats                            )\
		x(DSVFormat             , DSVFormat                             )\
		x(SampleDesc            , SampleDesc                            )\
		//x(StreamOutput          , StreamOutput                          )\
		//x(BlendEnable0          , BlendState.RenderTarget[0].BlendEnable)\
		//x(BlendEnable1          , BlendState.RenderTarget[1].BlendEnable)\
		//x(BlendEnable2          , BlendState.RenderTarget[2].BlendEnable)\
		//x(BlendEnable3          , BlendState.RenderTarget[3].BlendEnable)\
		//x(BlendEnable4          , BlendState.RenderTarget[4].BlendEnable)\
		//x(BlendEnable5          , BlendState.RenderTarget[5].BlendEnable)\
		//x(BlendEnable6          , BlendState.RenderTarget[6].BlendEnable)\
		//x(BlendEnable7          , BlendState.RenderTarget[7].BlendEnable)\
		//x(SampleMask            , SampleMask                            )\
		//x(RasterizerState       , RasterizerState                       )\
		//x(DepthStencilState     , DepthStencilState                     )\
		//x(InputLayout           , InputLayout                           )\
		//x(IBStripCutValue       , IBStripCutValue                       )\
		//x(PrimitiveTopologyType , PrimitiveTopologyType                 )\
		//x(NumRenderTargets      , NumRenderTargets                      )\
		//x(NodeMask              , NodeMask                              )\
		//x(CachedPSO             , CachedPSO                             )\
		//x(Flags                 , Flags                                 )

	// IDs for the pipeline state description fields
	enum class EPipeState :uint32_t
	{
		// Encode the byte offset and size into the enum value
		#define PR_RDR_PIPE_STATE_FIELD(name, member) name = \
			(((0xFFFF & offsetof(D3D12_GRAPHICS_PIPELINE_STATE_DESC, member)) << 16) | \
			 ((0xFFFF & sizeof(std::declval<D3D12_GRAPHICS_PIPELINE_STATE_DESC>().member)))),
		PR_RDR_PIPE_STATE_FIELDS(PR_RDR_PIPE_STATE_FIELD)
		#undef PR_RDR_PIPE_STATE_FIELD
	};

	// Convert 'EPipeState' to the corresponding type
	template <EPipeState pls> struct pipe_state_field { using type = void; };
	#define PR_RDR_PIPE_STATE_FIELD(name, member)\
	template <> struct pipe_state_field<EPipeState::name> { using type = std::decay_t<decltype(std::declval<D3D12_GRAPHICS_PIPELINE_STATE_DESC>().member)>; };
	PR_RDR_PIPE_STATE_FIELDS(PR_RDR_PIPE_STATE_FIELD)
	#undef PR_RDR_PIPE_STATE_FIELD
	template <EPipeState PS> using pipe_state_field_t = typename pipe_state_field<PS>::type;

	#undef PR_RDR_PIPE_STATE_FILES

	// Represents a change to the pipeline state description
	struct PipeState
	{
		using field_t = struct { uint16_t size, ofs; };
		using state_t = pr::vector<uint8_t, 16>;

		union {
		EPipeState m_id;  // Identifies the offset and size of the field in the PSO description.
		field_t m_field;  // Ofs/Size into the PSO description
		};
		state_t m_value;  // The data that replaces the PSO description field

		PipeState() = default;
		PipeState(EPipeState ps, state_t value)
			: m_id(ps)
			, m_value(value)
		{}

		// Returns a pointer into 'desc' for this pipe state field
		void const* ptr(D3D12_GRAPHICS_PIPELINE_STATE_DESC const& desc) const
		{
			return byte_ptr(&desc) + m_field.ofs;
		}
		void* ptr(D3D12_GRAPHICS_PIPELINE_STATE_DESC& desc) const
		{
			return byte_ptr(&desc) + m_field.ofs;
		}
		void const* value() const
		{
			return m_value.data();
		}
		int size() const
		{
			return m_field.size;
		}
	};

	// Create a PipeState object
	template <EPipeState PS> inline PipeState PSO(pipe_state_field_t<PS> const& data)
	{
		return PipeState(PS, {byte_ptr(&data), byte_ptr(&data + 1)});
	}

	// A collection of pipe state changes
	struct PipeStates
	{
		pr::vector<PipeState, 4> m_states;
		int m_fixed; // The first modifiable state. Below this index cannot be cleared/set.

		PipeStates()
			:m_states()
			, m_fixed()
		{}

		// The number of overrides
		int count() const
		{
			return s_cast<int>(m_states.size());
		}

		// Iteration access
		PipeState const* begin() const
		{
			return m_states.data();
		}
		PipeState const* end() const
		{
			return begin() + m_states.size();
		}

		// Remove a pipe state override
		template <EPipeState PS> void Clear()
		{
			pr::erase_if(m_states, [&](auto& ps) { return &ps - begin() >= m_fixed && ps.m_id == PS; });
		}

		// Set the pipeline state PS to 'data'
		template <EPipeState PS> void Set(pipe_state_field_t<PS> const& data)
		{
			Clear<PS>();
			m_states.push_back(PipeState(PS, {byte_ptr(&data), byte_ptr(&data + 1)}));
		}

		// See if the given pipe state is in the set of overrides
		template <EPipeState PS> pipe_state_field_t<PS> const* Find() const
		{
			for (auto iter = end(); iter-- != begin();)
			{
				auto& state = *iter;
				if (state.m_id != PS) continue;
				return type_ptr<pipe_state_field_t<PS>>(state.m_value.data());
			}
			return nullptr;
		}
	};

	// Pipe state object description
	struct PipeStateDesc
	{
		D3D12_GRAPHICS_PIPELINE_STATE_DESC m_desc;
		int m_hash;

		PipeStateDesc()
			:m_desc()
			,m_hash(hash::HashBytes(&m_desc, &m_desc + 1))
		{}
		PipeStateDesc(D3D12_GRAPHICS_PIPELINE_STATE_DESC const& rhs)
			:m_desc(rhs)
			,m_hash(hash::HashBytes(&m_desc, &m_desc + 1))
		{}

		// Apply changes to the pipeline state description
		void Apply(PipeState const& ps)
		{
			// Record the change in the hash
			m_hash = hash::HashBytes(&ps, &ps + 1, m_hash);

			// Set the value of a pipeline state description field
			memcpy(ps.ptr(m_desc), ps.value(), ps.size());
		}

		// Return the current value of a pipeline state member
		template <EPipeState PS> pipe_state_field_t<PS> const& Get() const
		{
			PipeState ps;
			return *static_cast<pipe_state_field_t<PS> const*>(ps.ptr(m_desc));
		}

		// Convert to a pointer for passing to functions
		operator D3D12_GRAPHICS_PIPELINE_STATE_DESC const* () const
		{
			return &m_desc;
		}
	};

	// A pipe state object instance
	struct PipeStateObject
	{
		D3DPtr<ID3D12PipelineState> m_pso; // The pipeline state object
		int64_t m_frame_number;            // The frame number when last used
		int m_hash;                        // Hash of the pipeline state data used to create 'm_pso'

		PipeStateObject(D3DPtr<ID3D12PipelineState> pso, int64_t frame_number, int hash)
			: m_pso(pso)
			, m_frame_number(frame_number)
			, m_hash(hash)
		{}

		// Access the allocator
		ID3D12PipelineState* operator ->() const
		{
			return m_pso.get();
		}

		// Convert to the allocator pointer
		operator ID3D12PipelineState* () const
		{
			return m_pso.get();
		}
	};

	// A pool of pipe states
	struct PipeStatePool
	{
		using pool_t = pr::vector<PipeStateObject, 16, false>;
		Window* m_wnd;
		pool_t m_pool;

		explicit PipeStatePool(Window& wnd);
		PipeStatePool(PipeStatePool&&) = default;
		PipeStatePool(PipeStatePool const&) = delete;
		PipeStatePool& operator=(PipeStatePool&&) = default;
		PipeStatePool& operator=(PipeStatePool const&) = delete;

		// Return a pipeline state instance for the given description
		ID3D12PipelineState* Get(PipeStateDesc const& desc);
	};
}