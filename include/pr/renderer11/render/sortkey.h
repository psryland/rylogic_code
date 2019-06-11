//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"

namespace pr
{
	namespace rdr
	{
		// Bit layout:
		// 11111111 11111111 11111111 11111111
		//                     ###### ######## texture id  lowest priority, most common thing changed when processing the drawlist
		//          ######## ##                shader id
		//        #                            has alpha
		// #######                             sort group  highest priority, least common thing changed when processing the drawlist
		//
		// General sorting notes: From the word of Al:
		// Z Buffering:
		//   Always try to maintain the z buffer (i.e. write enable) even for HUDs etc
		//   Stereoscopic rendering requires everything to have correct depth
		//   Render the sky box after all opaques to reduce overdraw
		// Alpha:
		//   Two sided objects should be rendered twice, 1st with front face
		//   culling, 2nd with back face culling
		//

		// Define sort groups
		enum class ESortGroup
		{
			PreOpaques  = 63, // 
			Default     = 64, // Make opaques the middle group
			Skybox          , // Sky-box after opaques
			PostOpaques     , // 
			PreAlpha    = 80, // 
			AlphaBack       , // 
			AlphaFront      , // 
			PostAlpha       , // 
			
			_arithmetic_operators_allowed = 0x7FFFFFFF,
		};

		// The sort key type (wraps a uint32)
		struct SortKey
		{
			using value_type = unsigned int;
			static value_type const Bits = 32U;

			// GGGGGGGA SSSSSSSS SSTTTTTT TTTTTTTT
			static value_type const TextureIdBits = 14U;
			static value_type const ShaderIdBits  = 10U;
			static value_type const AlphaBits     = 1U ;
			static value_type const SortGroupBits = Bits - (AlphaBits + ShaderIdBits + TextureIdBits);
			static_assert(Bits > AlphaBits + ShaderIdBits + TextureIdBits, "Sort key is not large enough");

			static value_type const MaxTextureId  = 1U << TextureIdBits;
			static value_type const MaxShaderId   = 1U << ShaderIdBits ;
			static value_type const MaxSortGroups = 1U << SortGroupBits;

			static value_type const TextureIdOfs  = value_type();
			static value_type const ShaderIdOfs   = value_type() + TextureIdBits;
			static value_type const AlphaOfs      = value_type() + TextureIdBits + ShaderIdBits;
			static value_type const SortGroupOfs  = value_type() + TextureIdBits + ShaderIdBits + AlphaBits;

			static value_type const TextureIdMask = (~value_type() >> (Bits - TextureIdBits)) << TextureIdOfs;
			static value_type const ShaderIdMask  = (~value_type() >> (Bits - ShaderIdBits )) << ShaderIdOfs ;
			static value_type const AlphaMask     = (~value_type() >> (Bits - AlphaBits    )) << AlphaOfs    ;
			static value_type const SortGroupMask = (~value_type() >> (Bits - SortGroupBits)) << SortGroupOfs;

			value_type m_value;

			SortKey() = default;
			explicit SortKey(value_type value)
				:m_value(value)
			{}
			explicit SortKey(ESortGroup grp)
				:m_value()
			{
				Group(grp);
			}
			operator value_type() const
			{
				return m_value;
			}

			// Get/Set the sort group
			ESortGroup Group() const
			{
				return static_cast<ESortGroup>((m_value & SortGroupMask) >> SortGroupOfs);
			}
			void Group(ESortGroup group)
			{
				auto g = value_type(group);
				PR_ASSERT(PR_DBG_RDR, g >= 0 && g < MaxSortGroups, "sort group out of range");
				m_value &= ~SortGroupMask;
				m_value |= (g << SortGroupOfs) & SortGroupMask;
			}

			SortKey& operator |= (SortKey::value_type rhs)
			{
				m_value |= rhs;
				return *this;
			}
			SortKey& operator &= (SortKey::value_type rhs)
			{ 
				m_value &= rhs;
				return *this;
			}
		};
		static_assert(std::is_pod<SortKey>::value, "SortKey must be a pod type");
		static_assert(8U * sizeof(SortKey) == SortKey::Bits, "8 * sizeof(Sortkey) != SortKey::Bits");
		static_assert(uint32(ESortGroup::Default) == SortKey::MaxSortGroups/2, "ESortGroup::Default should be the middle");

		// A sort key override is a mask that is applied to a sort key
		// to override specific parts of the sort key.
		struct SKOverride
		{
			using value_type = SortKey::value_type;
			value_type m_mask; // The bits to override
			value_type m_key;  // The overridden bit values

			SKOverride()
				:m_mask(0)
				,m_key(0)
			{}

			// Combine this override with a sort key to produce a new sort key
			SortKey Combine(SortKey key) const
			{
				return SortKey{(key.m_value & ~m_mask) | m_key};
			}

			// Get/Set the alpha component of the sort key
			bool HasAlpha() const
			{
				return (m_mask & SortKey::AlphaMask) != 0;
			}
			bool Alpha() const
			{
				return ((m_key & SortKey::AlphaMask) >> SortKey::AlphaOfs) != 0;
			}
			SKOverride& ClearAlpha()
			{
				m_mask &= ~SortKey::AlphaMask;
				m_key  &= ~SortKey::AlphaMask;
				return *this;
			}
			SKOverride& Alpha(bool has_alpha)
			{
				m_mask |= SortKey::AlphaMask;
				m_key  |= (has_alpha << SortKey::AlphaOfs) & SortKey::AlphaMask;
				return *this;
			}

			// Get/Set the sort group component of the sort key
			bool HasGroup() const
			{
				return (m_mask & SortKey::SortGroupMask) != 0;
			}
			int Group() const
			{
				return static_cast<int>(((m_key & SortKey::SortGroupMask) >> SortKey::SortGroupOfs) - value_type(ESortGroup::Default));
			}
			SKOverride& ClearGroup()
			{
				m_mask &= ~SortKey::SortGroupMask;
				m_key  &= ~SortKey::SortGroupMask;
				return *this;
			}
			SKOverride& Group(ESortGroup group)
			{
				auto g = value_type(group);
				PR_ASSERT(PR_DBG_RDR, g >= 0 && g < SortKey::MaxSortGroups, "sort group out of range");
				m_mask |= SortKey::SortGroupMask;
				m_key  |= (g << SortKey::SortGroupOfs) & SortKey::SortGroupMask;
				return *this;
			}
		};
	}
}
