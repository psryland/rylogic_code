//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/utility/wrappers.h"

// This idea of encoded enums is cool,  not sure it's needed for the full pipeline state though,
// might only need some for BS,RS,DS 

// PipeState is going to have to be more clever, it can also be private, so move it out of this header


namespace pr::rdr12
{
	// Parts of the pipeline state that can be changed
	// x(EPipeState::<name>, PipelineStateDesc::<field>)
	#define PR_RDR_PIPE_STATE_FIELDS(x)\
		x(RootSignature         , pRootSignature                        )\
		x(VS                    , VS                                    )\
		x(PS                    , PS                                    )\
		x(DS                    , DS                                    )\
		x(HS                    , HS                                    )\
		x(GS                    , GS                                    )\
		x(StreamOutput          , StreamOutput                          )\
		x(BlendState            , BlendState                            )\
		x(BlendEnable0          , BlendState.RenderTarget[0].BlendEnable)\
		x(BlendEnable1          , BlendState.RenderTarget[1].BlendEnable)\
		x(BlendEnable2          , BlendState.RenderTarget[2].BlendEnable)\
		x(BlendEnable3          , BlendState.RenderTarget[3].BlendEnable)\
		x(BlendEnable4          , BlendState.RenderTarget[4].BlendEnable)\
		x(BlendEnable5          , BlendState.RenderTarget[5].BlendEnable)\
		x(BlendEnable6          , BlendState.RenderTarget[6].BlendEnable)\
		x(BlendEnable7          , BlendState.RenderTarget[7].BlendEnable)\
		x(SampleMask            , SampleMask                            )\
		x(RasterizerState       , RasterizerState                       )\
		x(FillMode              , RasterizerState.FillMode              )\
		x(CullMode              , RasterizerState.CullMode              )\
		x(DepthStencilState     , DepthStencilState                     )\
		x(InputLayout           , InputLayout                           )\
		x(IBStripCutValue       , IBStripCutValue                       )\
		x(PrimitiveTopologyType , PrimitiveTopologyType                 )\
		x(NumRenderTargets      , NumRenderTargets                      )\
		x(RTVFormats            , RTVFormats                            )\
		x(DSVFormat             , DSVFormat                             )\
		x(SampleDesc            , SampleDesc                            )\
		x(NodeMask              , NodeMask                              )\
		x(CachedPSO             , CachedPSO                             )\
		x(Flags                 , Flags                                 )

	// IDs for the pipeline states
	enum class EPipeState :uint32_t
	{
		// Encode the byte offset and size into the enum value
		#define PR_RDR_PIPE_STATE_FIELD(name, member)\
		name = (((0xFFFF & offsetof(PipeStateDesc, member)) << 16) | ((0xFFFF & sizeof(std::declval<PipeStateDesc>().member)))),
		PR_RDR_PIPE_STATE_FIELDS(PR_RDR_PIPE_STATE_FIELD)
		#undef PR_RDR_PIPE_STATE_FIELD
	};

	// Convert 'EPipeState' to the corresponding type
	template <EPipeState pls> struct pipe_state_field { using type = void; };
	#define PR_RDR_PIPE_STATE_FIELD(name, member)\
	template <> struct pipe_state_field<EPipeState::name> { using type = decltype(std::declval<PipeStateDesc>().member); };
	PR_RDR_PIPE_STATE_FIELDS(PR_RDR_PIPE_STATE_FIELD)
	#undef PR_RDR_PIPE_STATE_FIELD
	template <EPipeState PS> using pipe_state_field_t = typename pipe_state_field<PS>::type;

	#undef PR_RDR_PIPE_STATE_FILES

	// A dynamic list of pipeline state changes
	struct PipeState
	{
		// Notes:
		//  - This type collects modifications to the pipeline state description.
		//  - Store a pipeline state description and a list of 'set' fields.
		using states_t = pr::vector<EPipeState, 8, false>;
		union field_t
		{
			EPipeState ps;
			struct { uint16_t ofs, size; } range;
			void const* ptr(PipeStateDesc const& desc) const { return byte_ptr(&desc) + range.ofs; }
			void* ptr(PipeStateDesc& desc) const             { return byte_ptr(&desc) + range.ofs; }
		};

		PipeStateDesc m_desc;
		states_t m_states;
		
		PipeState();

		// Reset all states
		void Reset(PipeStateDesc const& desc);

		// Remove a state
		void Clear(EPipeState ps);

		// Set the pipeline state for a pipeline state field
		void Set(EPipeState ps, void const* data);

		// Copy the pipeline states from 'rhs' into this object
		void Set(PipeState const& rhs);

		// Set the pipeline states related to a nugget
		void Set(Nugget const& nugget);

		// Find the pipeline state (if it exists)
		void const* Find(EPipeState ps) const;

		// Set the pipeline state to 'data'
		template <EPipeState PS> void Set(pipe_state_field_t<PS> const& data)
		{
			Set(PS, &data);
		}

		// Find the pipeline state (if it exists)
		template <EPipeState PS> pipe_state_field_t<PS> const* Find() const
		{
			return type_ptr<pipe_state_field_t<PS>>(Find(PS));
		}
	};
}
