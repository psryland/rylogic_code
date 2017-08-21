//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/util/lookup.h"
#include "pr/renderer11/util/wrappers.h"

namespace pr
{
	namespace rdr
	{
		enum class ERS
		{
			FillMode               = 1 << 0,
			CullMode               = 1 << 1,
			DepthClipEnable        = 1 << 2,
			FrontCCW               = 1 << 3,
			MultisampleEnable      = 1 << 4,
			AntialiasedLineEnable  = 1 << 5,
			ScissorEnable          = 1 << 6,
			DepthBias              = 1 << 7,
			DepthBias_clamp        = 1 << 8,
			SlopeScaledDepthBias   = 1 << 9,
			_bitwise_operators_allowed,
		};
		static_assert(has_bitwise_operators_allowed<ERS>::value, "");

		struct RSBlock :private StateBlock<RasterStateDesc, ERS, 1>
		{
			typedef StateBlock<RasterStateDesc, ERS, 1> base;
			
			RSBlock() :base() {}
			RSBlock(D3D11_FILL_MODE fill, D3D11_CULL_MODE cull) :base()
			{
				Set(ERS::FillMode, fill);
				Set(ERS::CullMode, cull);
			}

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
				Merge(rhs ,[this](ERS field, int, RasterStateDesc const& r)
					{
						switch (field)
						{
						default: PR_ASSERT(PR_DBG_RDR, false, "Unknown raster state field"); break;
						case ERS::FillMode             : Set(ERS::FillMode              ,r.FillMode             ); break;
						case ERS::CullMode             : Set(ERS::CullMode              ,r.CullMode             ); break;
						case ERS::DepthClipEnable      : Set(ERS::DepthClipEnable       ,r.FrontCounterClockwise); break;
						case ERS::FrontCCW             : Set(ERS::FrontCCW              ,r.DepthBias            ); break;
						case ERS::MultisampleEnable    : Set(ERS::MultisampleEnable     ,r.MultisampleEnable    ); break;
						case ERS::AntialiasedLineEnable: Set(ERS::AntialiasedLineEnable ,r.SlopeScaledDepthBias ); break;
						case ERS::ScissorEnable        : Set(ERS::ScissorEnable         ,r.DepthClipEnable      ); break;
						case ERS::DepthBias            : Set(ERS::DepthBias             ,r.ScissorEnable        ); break;
						case ERS::DepthBias_clamp      : Set(ERS::DepthBias_clamp       ,r.DepthBiasClamp       ); break;
						case ERS::SlopeScaledDepthBias : Set(ERS::SlopeScaledDepthBias  ,r.AntialiasedLineEnable); break;
						}
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
			RasterStateManager(MemFuncs& mem, ID3D11Device* d3d_device)
				:base(mem, d3d_device)
			{}

			// Get/Create a state object for 'desc'
			D3DPtr<ID3D11RasterizerState> State(RSBlock const& desc)
			{
				return base::GetState(desc, [this](RasterStateDesc const& d)
				{
					ID3D11RasterizerState* rs;
					pr::Throw(m_d3d_device->CreateRasterizerState(&d, &rs));
					return rs;
				});
			}

			// Called to limit the number of pooled state objects
			// Must be called while no state objects are in use
			using base::Flush;
		};
	}
}
