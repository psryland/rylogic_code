﻿//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/view3d/forward.h"
#include "pr/view3d/util/lookup.h"
#include "pr/view3d/util/wrappers.h"

namespace pr::rdr
{
	// Blend state flags
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
		_flags_enum = 0,
	};

	// Depth state flags
	enum class EDS
	{
		DepthEnable        = 1 << 0,
		DepthWriteMask     = 1 << 1,
		DepthFunc          = 1 << 2,
		StencilEnable      = 1 << 3,
		StencilReadMask    = 1 << 4,
		StencilWriteMask   = 1 << 5,
		StencilFunc        = 1 << 6,
		StencilDepthFailOp = 1 << 7,
		StencilPassOp      = 1 << 8,
		StencilFailOp      = 1 << 9,
		_flags_enum = 0,
	};

	// Raster state flags
	enum class ERS
	{
		FillMode = 1 << 0,
		CullMode = 1 << 1,
		DepthClipEnable = 1 << 2,
		FrontCCW = 1 << 3,
		MultisampleEnable = 1 << 4,
		AntialiasedLineEnable = 1 << 5,
		ScissorEnable = 1 << 6,
		DepthBias = 1 << 7,
		DepthBias_clamp = 1 << 8,
		SlopeScaledDepthBias = 1 << 9,
		_flags_enum = 0,
	};

	// Wraps a DX state description and provides a bitmask of changed values
	template <typename TStateDesc, typename TFieldEnum, size_t N>
	struct StateBlock
	{
		// 'TStateDesc' is a dx structure containing sets of render states such as 'D3D11_DEPTH_STENCIL_DESC'
		// 'm_mask' is a bit field indicating which members in 'TStateDesc' have had a value set. The reason
		// it is an array is to handle internal arrays in 'TStateDesc'.
		// E.g., say 'TStateDesc' was:
		//  struct SomeStateDesc
		//  {
		//      int awesome;
		//      char weight[3];
		//  };
		//  m_mask[0] would have a bit for 'awesome' and 'weight[0]'
		//  m_mask[1] would have a bit for 'weight[1]' (at the same bit index as weight[0])
		//  m_mask[2] would have a bit for 'weight[2]' (at the same bit index as weight[0])
		// The bit indices in 'm_mask[1..2]' for 'awesome' are not used and should never be set.
		// This way 'm_mask' indicates which members, including those in arrays, have been changed.
		using FieldEnum = TFieldEnum;
		static_assert(is_flags_enum_v<FieldEnum>);

		// The DX State block
		TStateDesc m_state;

		// A bit field of the members in 'TStateDesc' that have had a value set.
		FieldEnum m_mask[N];

		StateBlock()
			:m_state()
			,m_mask()
		{}

		// Returns the description
		TStateDesc const& Desc() const
		{
			return m_state;
		}
		TStateDesc& Desc()
		{
			return m_state;
		}

		// Clear a field in the state description
		void Clear(FieldEnum field)
		{
			m_mask[0] = pr::SetBits(m_mask[0], field, false);
		}
		void Clear(FieldEnum field, int n)
		{
			m_mask[n] = pr::SetBits(m_mask[n], field, false);
		}

		// Set the value of a field in the state description
		void Set(FieldEnum field)
		{
			m_mask[0] = pr::SetBits(m_mask[0], field, true);
		}
		void Set(FieldEnum field, int n)
		{
			m_mask[n] = pr::SetBits(m_mask[n], field, true);
		}

		// Combine two states into one. 'rhs' has priority over 'this'
		template <typename MergeFunc> void Merge(StateBlock const& rhs, MergeFunc merge)
		{
			// If no values in 'this' have been set, we can just copy 'rhs' wholesale
			auto mask = m_mask[0];
			for (int i = 1; i < N; ++i) mask = mask | m_mask[i];
			if (mask == FieldEnum())
			{
				*this = rhs;
				return;
			}

			// If no values in 'rhs' have been set, we can ignore it
			mask = rhs.m_mask[0];
			for (int i = 1; i < N; ++i) mask = mask | rhs.m_mask[i];
			if (mask == FieldEnum())
				return;

			// Otherwise, we have to through field-by-field copying those
			// that are set in 'rhs' over to 'this'
			for (int i = 0; i != N; ++i)
			{
				for (auto field : pr::EnumerateBits(rhs.m_mask[i]))
					merge((FieldEnum)field, i, rhs.m_state);
			}
		}

		// Operators
		friend bool operator == (StateBlock const& lhs, StateBlock const& rhs)
		{
			// Direct memory compare is way faster than comparing CRCs of the blocks
			return memcmp(&lhs, &rhs, sizeof(lhs)) == 0;
		}
		friend bool operator != (StateBlock const& lhs, StateBlock const& rhs)
		{
			return !(lhs == rhs);
		}
	};

	// Provides a pool of TStateBlock objects
	template <typename TStateBlock, typename TD3DInterface>
	struct StateManager
	{
		using base = StateManager<TStateBlock, TD3DInterface>;
		using Lookup = Lookup<size_t, TD3DInterface*>;

		Renderer* m_rdr;
		Lookup m_lookup;

		explicit StateManager(Renderer& rdr)
			:m_rdr(&rdr)
			,m_lookup()
		{}
		~StateManager()
		{
			Flush(0);
		}
		StateManager(StateManager const&) = delete;
		StateManager& operator =(StateManager const&) = delete;

		// Get/Create a state buffer for 'desc'
		template <typename CreateFunc>
		D3DPtr<TD3DInterface> GetState(TStateBlock const& desc, CreateFunc create)
		{
			// Look for a corresponding state object
			auto hash = pr::hash::HashBytes64(&desc, &desc + 1);
			auto iter = m_lookup.find(hash);
			if (iter == std::end(m_lookup))
			{
				// If not found, create one
				TD3DInterface* s = create(desc.Desc());
				iter = m_lookup.insert(iter, Lookup::pair(hash, s));
			}
			return D3DPtr<TD3DInterface>(iter->second, true);
		}

		// Called to limit the number of pooled state objects
		// Must be called while no state objects are in use
		void Flush(size_t high_tide = 500)
		{
			// Only flush if we need to
			if (m_lookup.size() <= high_tide)
				return;

			// We could flush down to a low tide mark, but there isn't a sensible way of doing
			// this that doesn't risk leaving some unused states in the pool indefinitely.
			// Just flush all. Remember, 'm_lookup.size()' is the number of *unique*
			// states currently active.
			// Notice, it doesn't actually matter if there are outstanding references to the
			// states being released here. Those states will release when they go out of scope.
			for (auto i = std::begin(m_lookup); !m_lookup.empty(); i = m_lookup.erase(i))
				i->second->Release();
		}
	};

	// Blend states
	struct BSBlock
	{
		StateBlock<BlendStateDesc, EBS, 8> m_bsb;

		BSBlock();

		// Returns the description
		BlendStateDesc const& Desc() const;
		BlendStateDesc& Desc();

		// Clear a field in the state description
		void Clear(EBS field);
		void Clear(EBS field, int render_target);

		// Set the value of a field in the state description
		void Set(EBS field, BOOL value);
		void Set(EBS field, BOOL value, int render_target);
		void Set(EBS field, D3D11_BLEND value, int render_target);
		void Set(EBS field, D3D11_BLEND_OP value, int render_target);
		void Set(EBS field, UINT8 value, int render_target);

		// Combine two states into one. 'rhs' has priority over 'this'
		BSBlock& operator |= (BSBlock const& rhs);
		friend bool operator == (BSBlock const& lhs, BSBlock const& rhs) { return lhs.m_bsb == rhs.m_bsb; }
		friend bool operator != (BSBlock const& lhs, BSBlock const& rhs) { return lhs.m_bsb != rhs.m_bsb; }
	};
	static_assert(std::is_standard_layout_v<BSBlock>);

	// Depth states
	struct DSBlock
	{
		StateBlock<DepthStateDesc, EDS, 2> m_dsb;

		DSBlock();

		// Returns the description
		DepthStateDesc const& Desc() const;
		DepthStateDesc& Desc();

		// Clear a field in the state description
		void Clear(EDS field);
		void Clear(EDS field, bool back_face);

		// Set the value of a field in the state description
		void Set(EDS field, BOOL value);
		void Set(EDS field, D3D11_DEPTH_WRITE_MASK value);
		void Set(EDS field, D3D11_COMPARISON_FUNC value);
		void Set(EDS field, UINT8 value);
		void Set(EDS field, D3D11_COMPARISON_FUNC value, bool back_face);
		void Set(EDS field, D3D11_STENCIL_OP value, bool back_face);

		// Combine two states into one. 'rhs' has priority over 'this'
		DSBlock& operator |= (DSBlock const& rhs);
		friend bool operator == (DSBlock const& lhs, DSBlock const& rhs) { return lhs.m_dsb == rhs.m_dsb; }
		friend bool operator != (DSBlock const& lhs, DSBlock const& rhs) { return lhs.m_dsb != rhs.m_dsb; }
	};
	static_assert(std::is_standard_layout_v<DSBlock>);

	// Raster states
	struct RSBlock
	{
		StateBlock<RasterStateDesc, ERS, 1> m_rsb;

		RSBlock();
		RSBlock(D3D11_FILL_MODE fill, D3D11_CULL_MODE cull);

		// Returns the description
		RasterStateDesc const& Desc() const;
		RasterStateDesc& Desc();

		// Clear a field in the state description
		void Clear(ERS field);

		// Set the value of a field in the state description
		void Set(ERS field, D3D11_FILL_MODE value);
		void Set(ERS field, D3D11_CULL_MODE value);
		void Set(ERS field, int value);
		void Set(ERS field, float value);

		// Combine two states into one. 'rhs' has priority over 'this'
		RSBlock& operator |= (RSBlock const& rhs);
		friend bool operator == (RSBlock const& lhs, RSBlock const& rhs) { return lhs.m_rsb == rhs.m_rsb; }
		friend bool operator != (RSBlock const& lhs, RSBlock const& rhs) { return lhs.m_rsb != rhs.m_rsb; }

		// Some common raster states
		static RSBlock SolidCullNone()
		{
			static RSBlock s_rs(D3D11_FILL_SOLID, D3D11_CULL_NONE);
			return s_rs;
		}
		static RSBlock SolidCullBack()
		{
			static RSBlock s_rs(D3D11_FILL_SOLID, D3D11_CULL_BACK);
			return s_rs;
		}
		static RSBlock SolidCullFront()
		{
			static RSBlock s_rs(D3D11_FILL_SOLID, D3D11_CULL_FRONT);
			return s_rs;
		}
		static RSBlock WireCullNone()
		{
			static RSBlock s_rs(D3D11_FILL_WIREFRAME, D3D11_CULL_NONE);
			return s_rs;
		}
	};
	static_assert(std::is_standard_layout_v<RSBlock>);

	// Provides a pool of BlendState objects
	class BlendStateManager :private StateManager<BSBlock, ID3D11BlendState>
	{
	public:
		explicit BlendStateManager(Renderer& rdr);

		// Get/Create a state object for 'desc'
		D3DPtr<ID3D11BlendState> State(pr::rdr::BSBlock const& desc);

		// Called to limit the number of pooled state objects
		// Must be called while no state objects are in use
		using base::Flush;
	};

	// Provides a pool of BlendState objects
	class DepthStateManager :private StateManager<DSBlock, ID3D11DepthStencilState>
	{
	public:
		explicit DepthStateManager(Renderer& rdr);

		// Get/Create a state object for 'desc'
		D3DPtr<ID3D11DepthStencilState> State(DSBlock const& desc);

		// Called to limit the number of pooled state objects
		// Must be called while no state objects are in use
		using base::Flush;
	};

	// Provides a pool of RasterizerState objects
	class RasterStateManager :private StateManager<RSBlock, ID3D11RasterizerState>
	{
	public:
		explicit RasterStateManager(Renderer& rdr);

		// Get/Create a state object for 'desc'
		D3DPtr<ID3D11RasterizerState> State(RSBlock const& desc);

		// Called to limit the number of pooled state objects
		// Must be called while no state objects are in use
		using base::Flush;
	};
}
