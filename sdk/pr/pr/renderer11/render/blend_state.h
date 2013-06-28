//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_RENDER_BLEND_STATE_H
#define PR_RDR_RENDER_BLEND_STATE_H

#include "pr/renderer11/forward.h"
#include "pr/renderer11/util/lookup.h"
#include "pr/renderer11/util/wrappers.h"
#include "pr/renderer11/render/state_block.h"

namespace pr
{
	namespace rdr
	{
		#define PR_ENUM(x)\
			x(AlphaToCoverageEnable  ,= 1 << 0)\
			x(IndependentBlendEnable ,= 1 << 1)\
			x(BlendEnable            ,= 1 << 2)\
			x(SrcBlend               ,= 1 << 3)\
			x(DestBlend              ,= 1 << 4)\
			x(BlendOp                ,= 1 << 5)\
			x(SrcBlendAlpha          ,= 1 << 6)\
			x(DestBlendAlpha         ,= 1 << 7)\
			x(BlendOpAlpha           ,= 1 << 8)\
			x(RenderTargetWriteMask  ,= 1 << 9)
		PR_DEFINE_ENUM2_FLAGS(EBS, PR_ENUM);
		#undef PR_ENUM

		struct BSBlock :private StateBlock<BlendStateDesc, EBS, 8>
		{
			using base::Hash;
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
				Merge(rhs, [this](BlendStateDesc const& r, EBS mask, int i)
					{
						if (mask & EBS::AlphaToCoverageEnable ) Set(EBS::AlphaToCoverageEnable  ,r.AlphaToCoverageEnable                );
						if (mask & EBS::IndependentBlendEnable) Set(EBS::IndependentBlendEnable ,r.IndependentBlendEnable               );
						if (mask & EBS::BlendEnable           ) Set(EBS::BlendEnable            ,r.RenderTarget[i].BlendEnable          );
						if (mask & EBS::SrcBlend              ) Set(EBS::SrcBlend               ,r.RenderTarget[i].SrcBlend             );
						if (mask & EBS::DestBlend             ) Set(EBS::DestBlend              ,r.RenderTarget[i].DestBlend            );
						if (mask & EBS::BlendOp               ) Set(EBS::BlendOp                ,r.RenderTarget[i].BlendOp              );
						if (mask & EBS::SrcBlendAlpha         ) Set(EBS::SrcBlendAlpha          ,r.RenderTarget[i].SrcBlendAlpha        );
						if (mask & EBS::DestBlendAlpha        ) Set(EBS::DestBlendAlpha         ,r.RenderTarget[i].DestBlendAlpha       );
						if (mask & EBS::BlendOpAlpha          ) Set(EBS::BlendOpAlpha           ,r.RenderTarget[i].BlendOpAlpha         );
						if (mask & EBS::RenderTargetWriteMask ) Set(EBS::RenderTargetWriteMask  ,r.RenderTarget[i].RenderTargetWriteMask);
					});
				return *this;
			}
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

#endif
