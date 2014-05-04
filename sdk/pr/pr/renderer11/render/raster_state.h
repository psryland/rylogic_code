//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_RENDER_RASTER_STATE_H
#define PR_RDR_RENDER_RASTER_STATE_H

#include "pr/renderer11/forward.h"
#include "pr/renderer11/util/lookup.h"
#include "pr/renderer11/util/wrappers.h"

namespace pr
{
	namespace rdr
	{
		#define PR_ENUM(x)\
			x(FillMode               ,= 1 << 0)\
			x(CullMode               ,= 1 << 1)\
			x(DepthClipEnable        ,= 1 << 2)\
			x(FrontCCW               ,= 1 << 3)\
			x(MultisampleEnable      ,= 1 << 4)\
			x(AntialiasedLineEnable  ,= 1 << 5)\
			x(ScissorEnable          ,= 1 << 6)\
			x(DepthBias              ,= 1 << 7)\
			x(DepthBias_clamp        ,= 1 << 8)\
			x(SlopeScaledDepthBias   ,= 1 << 9)
		PR_DEFINE_ENUM2_FLAGS(ERS, PR_ENUM);
		#undef PR_ENUM

		struct RSBlock :private StateBlock<RasterStateDesc, ERS, 1>
		{
			RSBlock() {}
			RSBlock(D3D11_FILL_MODE fill, D3D11_CULL_MODE cull)
			{
				Set(ERS::FillMode, fill);
				Set(ERS::CullMode, cull);
			}

			using base::Hash;
			using base::Desc;

			// Clear a field in the state description
			void Clear(ERS field)
			{
				base::Clear(field);
			}

			// Set the value of a field in the state description
			void Set(ERS field, D3D11_FILL_MODE value)
			{
				PR_ASSERT(PR_DBG_RDR, field == ERS::FillMode, "Incorrect field provided");
				FillMode = value;
				base::Set(field);
			}
			void Set(ERS field, D3D11_CULL_MODE value)
			{
				PR_ASSERT(PR_DBG_RDR, field == ERS::CullMode, "Incorrect field provided");
				CullMode = value;
				base::Set(field);
			}
			void Set(ERS field, int value)
			{
				switch (field)
				{
				default: PR_ASSERT(PR_DBG_RDR, false, "Incorrect field provided");
				case ERS::DepthClipEnable      : FrontCounterClockwise = value != 0; break;
				case ERS::FrontCCW             : DepthBias             = value;      break;
				case ERS::MultisampleEnable    : MultisampleEnable     = value != 0; break;
				case ERS::ScissorEnable        : DepthClipEnable       = value != 0; break;
				case ERS::DepthBias            : ScissorEnable         = value != 0; break;
				case ERS::SlopeScaledDepthBias : AntialiasedLineEnable = value != 0; break;
				}
				base::Set(field);
			}
			void Set(ERS field, float value)
			{
				switch (field)
				{
				default: PR_ASSERT(PR_DBG_RDR, false, "Incorrect field provided");
				case ERS::AntialiasedLineEnable: SlopeScaledDepthBias = value; break;
				case ERS::DepthBias_clamp      : DepthBiasClamp       = value; break;
				}
				base::Set(field);
			}

			// Combine two states into one. 'rhs' has priority over 'this'
			RSBlock& operator |= (RSBlock const& rhs)
			{
				Merge(rhs ,[this](RasterStateDesc const& r, ERS mask, int)
					{
						if (mask & ERS::FillMode             ) Set(ERS::FillMode              ,r.FillMode             );
						if (mask & ERS::CullMode             ) Set(ERS::CullMode              ,r.CullMode             );
						if (mask & ERS::DepthClipEnable      ) Set(ERS::DepthClipEnable       ,r.FrontCounterClockwise);
						if (mask & ERS::FrontCCW             ) Set(ERS::FrontCCW              ,r.DepthBias            );
						if (mask & ERS::MultisampleEnable    ) Set(ERS::MultisampleEnable     ,r.MultisampleEnable    );
						if (mask & ERS::AntialiasedLineEnable) Set(ERS::AntialiasedLineEnable ,r.SlopeScaledDepthBias );
						if (mask & ERS::ScissorEnable        ) Set(ERS::ScissorEnable         ,r.DepthClipEnable      );
						if (mask & ERS::DepthBias            ) Set(ERS::DepthBias             ,r.ScissorEnable        );
						if (mask & ERS::DepthBias_clamp      ) Set(ERS::DepthBias_clamp       ,r.DepthBiasClamp       );
						if (mask & ERS::SlopeScaledDepthBias ) Set(ERS::SlopeScaledDepthBias  ,r.AntialiasedLineEnable);
					});
				return *this;
			}

			bool operator == (RSBlock const& rhs) const { return (base&)*this == (base&)rhs; }
			bool operator != (RSBlock const& rhs) const { return (base&)*this != (base&)rhs; }

			// Some common raster states
			static RSBlock SolidCullNone () { static RSBlock s_rs(D3D11_FILL_SOLID     ,D3D11_CULL_NONE ); return s_rs; }
			static RSBlock SolidCullBack () { static RSBlock s_rs(D3D11_FILL_SOLID     ,D3D11_CULL_BACK ); return s_rs; }
			static RSBlock SolidCullFront() { static RSBlock s_rs(D3D11_FILL_SOLID     ,D3D11_CULL_FRONT); return s_rs; }
			static RSBlock WireCullNone  () { static RSBlock s_rs(D3D11_FILL_WIREFRAME ,D3D11_CULL_NONE ); return s_rs; }
		};

		// Provides a pool of RasterizerState objects
		class RasterStateManager :private StateManager<RSBlock, ID3D11RasterizerState>
		{
		public:
			RasterStateManager(pr::rdr::MemFuncs& mem, D3DPtr<ID3D11Device>& device)
				:base(mem, device)
			{}

			// Get/Create a state object for 'desc'
			D3DPtr<ID3D11RasterizerState> State(pr::rdr::RSBlock const& desc)
			{
				return base::GetState(desc, [this](pr::rdr::RasterStateDesc const& d)
					{
						ID3D11RasterizerState* rs;
						pr::Throw(m_device->CreateRasterizerState(&d, &rs));
						return rs;
					});
			}

			// Called to limit the number of pooled state objects
			// Must be called while no state objects are in use
			using base::Flush;
		};
	}
}

#endif
