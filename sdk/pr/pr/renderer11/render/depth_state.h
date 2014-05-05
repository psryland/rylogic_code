//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_RENDER_DEPTH_STATE_H
#define PR_RDR_RENDER_DEPTH_STATE_H

#include "pr/renderer11/forward.h"
#include "pr/renderer11/util/lookup.h"
#include "pr/renderer11/util/wrappers.h"
#include "pr/renderer11/render/state_block.h"

namespace pr
{
	namespace rdr
	{
		#define PR_ENUM(x)\
			x(DepthEnable        ,= 1 << 0)\
			x(DepthWriteMask     ,= 1 << 1)\
			x(DepthFunc          ,= 1 << 2)\
			x(StencilEnable      ,= 1 << 3)\
			x(StencilReadMask    ,= 1 << 4)\
			x(StencilWriteMask   ,= 1 << 5)\
			x(StencilFunc        ,= 1 << 6)\
			x(StencilDepthFailOp ,= 1 << 7)\
			x(StencilPassOp      ,= 1 << 8)\
			x(StencilFailOp      ,= 1 << 9)
		PR_DEFINE_ENUM2_FLAGS(EDS, PR_ENUM);
		#undef PR_ENUM

		struct DSBlock :private StateBlock<DepthStateDesc, EDS, 2>
		{
			using base::Hash;
			using base::Desc;

			// Clear a field in the state description
			void Clear(EDS field)
			{
				PR_ASSERT(PR_DBG_RDR,
					field == EDS::DepthEnable      ||
					field == EDS::DepthWriteMask   ||
					field == EDS::DepthFunc        ||
					field == EDS::StencilEnable    ||
					field == EDS::StencilReadMask  ||
					field == EDS::StencilWriteMask
					,"Incorrect field provided");
				base::Clear(field);
			}
			void Clear(EDS field, bool back_face)
			{
				PR_ASSERT(PR_DBG_RDR,
					field == EDS::StencilFunc        ||
					field == EDS::StencilDepthFailOp ||
					field == EDS::StencilPassOp      ||
					field == EDS::StencilFailOp
					,"Incorrect field provided");
				base::Clear(field, (int)back_face);
			}

			// Set the value of a field in the state description
			void Set(EDS field, BOOL value)
			{
				switch (field)
				{
				default: PR_ASSERT(PR_DBG_RDR, false, "Incorrect field provided"); break;
				case EDS::DepthEnable   : DepthEnable   = value; break;
				case EDS::StencilEnable : StencilEnable = value; break;
				}
				base::Set(field);
			}
			void Set(EDS field, D3D11_DEPTH_WRITE_MASK value)
			{
				PR_ASSERT(PR_DBG_RDR, field == EDS::DepthWriteMask, "Incorrect field provided");
				DepthWriteMask = value;
				base::Set(field);
			}
			void Set(EDS field, D3D11_COMPARISON_FUNC value)
			{
				PR_ASSERT(PR_DBG_RDR, field == EDS::DepthFunc, "Incorrect field provided");
				DepthFunc = value;
				base::Set(field);
			}
			void Set(EDS field, UINT8 value)
			{
				switch (field)
				{
				default: PR_ASSERT(PR_DBG_RDR, false, "Incorrect field provided"); break;
				case EDS::StencilReadMask  : StencilReadMask  = value; break;
				case EDS::StencilWriteMask : StencilWriteMask = value; break;
				}
				base::Set(field);
			}
			void Set(EDS field, D3D11_COMPARISON_FUNC value, bool back_face)
			{
				PR_ASSERT(PR_DBG_RDR, field == EDS::StencilFunc, "Incorrect field provided");
				(back_face ? BackFace : FrontFace).StencilFunc = value;
				base::Set(field, (int)back_face);
			}
			void Set(EDS field, D3D11_STENCIL_OP value, bool back_face)
			{
				auto& face = back_face ? BackFace : FrontFace;
				switch (field)
				{
				default: PR_ASSERT(PR_DBG_RDR, false, "Incorrect field provided"); break;
				case EDS::StencilDepthFailOp : face.StencilDepthFailOp = value; break;
				case EDS::StencilPassOp      : face.StencilPassOp      = value; break;
				case EDS::StencilFailOp      : face.StencilFailOp      = value; break;
				}
				base::Set(field, (int)back_face);
			}

			// Combine two states into one. 'rhs' has priority over 'this'
			DSBlock& operator |= (DSBlock const& rhs)
			{
				Merge(rhs, [this](EDS field, int i, DepthStateDesc const& r)
					{
						switch (field)
						{
						default: PR_ASSERT(PR_DBG_RDR, false, "Unknown depth state field"); break;
						case EDS::DepthEnable       : Set(EDS::DepthEnable        ,r.DepthEnable       ); break;
						case EDS::DepthWriteMask    : Set(EDS::DepthWriteMask     ,r.DepthWriteMask    ); break;
						case EDS::DepthFunc         : Set(EDS::DepthFunc          ,r.DepthFunc         ); break;
						case EDS::StencilEnable     : Set(EDS::StencilEnable      ,r.StencilEnable     ); break;
						case EDS::StencilReadMask   : Set(EDS::StencilReadMask    ,r.StencilReadMask   ); break;
						case EDS::StencilWriteMask  : Set(EDS::StencilWriteMask   ,r.StencilWriteMask  ); break;
						case EDS::StencilFunc       : Set(EDS::StencilFunc        ,(i == 0 ? r.FrontFace : r.BackFace).StencilFunc       , i != 0); break;
						case EDS::StencilDepthFailOp: Set(EDS::StencilDepthFailOp ,(i == 0 ? r.FrontFace : r.BackFace).StencilDepthFailOp, i != 0); break;
						case EDS::StencilPassOp     : Set(EDS::StencilPassOp      ,(i == 0 ? r.FrontFace : r.BackFace).StencilPassOp     , i != 0); break;
						case EDS::StencilFailOp     : Set(EDS::StencilFailOp      ,(i == 0 ? r.FrontFace : r.BackFace).StencilFailOp     , i != 0); break;
						}
					});
				return *this;
			}

			bool operator == (DSBlock const& rhs) const { return (base&)*this == (base&)rhs; }
			bool operator != (DSBlock const& rhs) const { return (base&)*this != (base&)rhs; }
		};

		// Provides a pool of BlendState objects
		class DepthStateManager :private StateManager<DSBlock, ID3D11DepthStencilState>
		{
		public:
			DepthStateManager(pr::rdr::MemFuncs& mem, D3DPtr<ID3D11Device>& device)
				:base(mem, device)
			{}

			// Get/Create a state object for 'desc'
			D3DPtr<ID3D11DepthStencilState> State(pr::rdr::DSBlock const& desc)
			{
				return base::GetState(desc, [this](pr::rdr::DepthStateDesc const& d)
					{
						ID3D11DepthStencilState* ds;
						pr::Throw(m_device->CreateDepthStencilState(&d, &ds));
						return ds;
					});
			}

			// Called to limit the number of pooled state objects
			// Must be called while no state objects are in use
			using base::Flush;
		};
	}
}

#endif
