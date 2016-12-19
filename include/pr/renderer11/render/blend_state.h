//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/util/lookup.h"
#include "pr/renderer11/util/wrappers.h"
#include "pr/renderer11/render/state_block.h"

namespace pr
{
	namespace rdr
	{
		enum class EBS
		{
			AlphaToCoverageEnable  = 1 << 0,
			IndependentBlendEnable = 1 << 1,
			BlendEnable            = 1 << 2,
			SrcBlend               = 1 << 3,
			DestBlend              = 1 << 4,
			BlendOp                = 1 << 5,
			SrcBlendAlpha          = 1 << 6,
			DestBlendAlpha         = 1 << 7,
			BlendOpAlpha           = 1 << 8,
			RenderTargetWriteMask  = 1 << 9,
			_bitwise_operators_allowed,
		};
		static_assert(has_bitwise_operators_allowed<EBS>::value, "");

		struct BSBlock :private StateBlock<BlendStateDesc, EBS, 8>
		{
			typedef StateBlock<BlendStateDesc, EBS, 8> base;

			BSBlock() :base() {}

			using base::Desc;

			// Clear a field in the state description
			void Clear(EBS field)
			{
				PR_ASSERT(PR_DBG_RDR, field == EBS::AlphaToCoverageEnable || field == EBS::IndependentBlendEnable, "Incorrect field provided");
				base::Clear(field);
			}
			void Clear(EBS field, int render_target)
			{
				PR_ASSERT(PR_DBG_RDR, field != EBS::AlphaToCoverageEnable && field != EBS::IndependentBlendEnable, "Incorrect field provided");
				base::Clear(field, render_target);
			}

			// Set the value of a field in the state description
			void Set(EBS field, BOOL value)
			{
				switch (field)
				{
				default: PR_ASSERT(PR_DBG_RDR, false, "Incorrect field provided"); break;
				case EBS::AlphaToCoverageEnable  : AlphaToCoverageEnable  = value; break;
				case EBS::IndependentBlendEnable : IndependentBlendEnable = value; break;
				}
				base::Set(field);
			}
			void Set(EBS field, BOOL value, int render_target)
			{
				PR_ASSERT(PR_DBG_RDR, field == EBS::BlendEnable, "Incorrect field provided");
				RenderTarget[render_target].BlendEnable = value;
				base::Set(field, render_target);
			}
			void Set(EBS field, D3D11_BLEND value, int render_target)
			{
				switch (field)
				{
				default: PR_ASSERT(PR_DBG_RDR, false, "Incorrect field provided"); break;
				case EBS::SrcBlend             : RenderTarget[render_target].SrcBlend       = value; break;
				case EBS::DestBlend            : RenderTarget[render_target].DestBlend      = value; break;
				case EBS::SrcBlendAlpha        : RenderTarget[render_target].SrcBlendAlpha  = value; break;
				case EBS::DestBlendAlpha       : RenderTarget[render_target].DestBlendAlpha = value; break;
				}
				base::Set(field, render_target);
			}
			void Set(EBS field, D3D11_BLEND_OP value, int render_target)
			{
				switch (field)
				{
				default: PR_ASSERT(PR_DBG_RDR, false, "Incorrect field provided"); break;
				case EBS::BlendOp              : RenderTarget[render_target].BlendOp        = value; break;
				case EBS::BlendOpAlpha         : RenderTarget[render_target].BlendOpAlpha   = value; break;
				}
				base::Set(field, render_target);
			}
			void Set(EBS field, UINT8 value, int render_target)
			{
				PR_ASSERT(PR_DBG_RDR, field == EBS::RenderTargetWriteMask, "Incorrect field provided");
				RenderTarget[render_target].RenderTargetWriteMask = value;
				base::Set(field, render_target);
			}

			// Combine two states into one. 'rhs' has priority over 'this'
			BSBlock& operator |= (BSBlock const& rhs)
			{
				Merge(rhs, [this](EBS field, int i, BlendStateDesc const& r)
					{
						switch (field)
						{
						default: PR_ASSERT(PR_DBG_RDR, false, "Unknown blend state field"); break;
						case EBS::AlphaToCoverageEnable : Set(EBS::AlphaToCoverageEnable  ,r.AlphaToCoverageEnable                   ); break;
						case EBS::IndependentBlendEnable: Set(EBS::IndependentBlendEnable ,r.IndependentBlendEnable                  ); break;
						case EBS::BlendEnable           : Set(EBS::BlendEnable            ,r.RenderTarget[i].BlendEnable          , i); break;
						case EBS::SrcBlend              : Set(EBS::SrcBlend               ,r.RenderTarget[i].SrcBlend             , i); break;
						case EBS::DestBlend             : Set(EBS::DestBlend              ,r.RenderTarget[i].DestBlend            , i); break;
						case EBS::BlendOp               : Set(EBS::BlendOp                ,r.RenderTarget[i].BlendOp              , i); break;
						case EBS::SrcBlendAlpha         : Set(EBS::SrcBlendAlpha          ,r.RenderTarget[i].SrcBlendAlpha        , i); break;
						case EBS::DestBlendAlpha        : Set(EBS::DestBlendAlpha         ,r.RenderTarget[i].DestBlendAlpha       , i); break;
						case EBS::BlendOpAlpha          : Set(EBS::BlendOpAlpha           ,r.RenderTarget[i].BlendOpAlpha         , i); break;
						case EBS::RenderTargetWriteMask : Set(EBS::RenderTargetWriteMask  ,r.RenderTarget[i].RenderTargetWriteMask, i); break;
						}
					});
				return *this;
			}

			bool operator == (BSBlock const& rhs) const { return (base&)*this == (base&)rhs; }
			bool operator != (BSBlock const& rhs) const { return (base&)*this != (base&)rhs; }
		};

		// Provides a pool of BlendState objects
		class BlendStateManager :private StateManager<BSBlock, ID3D11BlendState>
		{
		public:
			BlendStateManager(pr::rdr::MemFuncs& mem, D3DPtr<ID3D11Device>& device)
				:base(mem, device)
			{}

			// Get/Create a state object for 'desc'
			D3DPtr<ID3D11BlendState> State(pr::rdr::BSBlock const& desc)
			{
				return base::GetState(desc, [this](pr::rdr::BlendStateDesc const& d)
					{
						ID3D11BlendState* bs;
						pr::Throw(m_device->CreateBlendState(&d, &bs));
						return bs;
					});
			}

			// Called to limit the number of pooled state objects
			// Must be called while no state objects are in use
			using base::Flush;
		};
	}
}
