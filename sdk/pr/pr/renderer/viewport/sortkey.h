//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#pragma once
#ifndef PR_RDR_SORT_KEY_H
#define PR_RDR_SORT_KEY_H

#include "pr/renderer/types/forward.h"
#include "pr/renderer/models/rendernugget.h"
#include "pr/renderer/materials/material.h"
#include "pr/renderer/materials/textures/texture.h"
#include "pr/renderer/materials/effects/effect.h"

namespace pr
{
	namespace rdr
	{
		// General sorting notes: From the word of Al:
		// Z Buffering:
		//   Always try to maintain the z buffer (i.e. writeenable) even for huds etc
		//   Stereoscope rendering requires everything to have correct depth
		//   Render the skybox after all opaques to reduce overdraw
		// Alpha:
		//   Two sided objects should be rendered twice, 1st with front face
		//   culling, 2nd with back face culling
		namespace sort_key
		{
			SortKey const Min = 0x00000000U;
			SortKey const Max = 0xFFFFFFFFU;
			
			// Bit layout:
			// 11111111 11111111 11111111 11111111
			//                     ###### ######## texture id  lowest priority, most common thing changed when processing the drawlist
			//          ######## ##                effect id   
			//        #                            has alpha   
			// #######                             sort group  highest priority, least common thing changed when processing the drawlist
			enum
			{
				SortKeyBits      = sizeof(SortKey) * 8,
				
				TextureIdBits    = 14,
				EffectIdBits     = 10,
				AlphaBits        = 1,
				SortGroupBits    = SortKeyBits - (AlphaBits + EffectIdBits + TextureIdBits),
				
				MaxTextureId     = 1 << TextureIdBits,
				MaxEffectId      = 1 << EffectIdBits,
				MaxSortGroups    = 1 << SortGroupBits,
				
				TextureIdOfs     = 0,
				EffectIdOfs      = 0 + TextureIdBits,
				AlphaOfs         = 0 + TextureIdBits + EffectIdBits,
				SortGroupOfs     = 0 + TextureIdBits + EffectIdBits + AlphaBits,
				
				TextureIdMask    = (~0 >> (SortKeyBits - TextureIdBits)) << TextureIdOfs,
				EffectIdMask     = (~0 >> (SortKeyBits - EffectIdBits )) << EffectIdOfs,
				AlphaMask        = (~0 >> (SortKeyBits - AlphaBits    )) << AlphaOfs,
				SortGroupMask    = (~0 >> (SortKeyBits - SortGroupBits)) << SortGroupOfs,
				
				SortGroup_Default    = MaxSortGroups >> 1,
				SortGroup_Opaques    = SortGroup_Default,
				SortGroup_Skybox     = SortGroup_Default + 1,
				SortGroup_AlphaBack  = SortGroup_Default + 5,
				SortGroup_AlphaFront = SortGroup_Default + 6,
				
				//TextureIdOfs       = 0,
				//EffectIdOfs        = MaxTextureIdBits,
				//AlphaOfs         = MaxEffectIdBits + EffectIdOfs,
				//MaterialOfs      = MaxAlphaBits    + AlphaOfs,
				//RenderSortKeyOfs = MaterialOfs,
			};
			
			// A sort key override is a mask that is applied to a sort key
			// to override specific parts of the sort key.
			struct Override
			{
				SortKey m_mask; // Set the bits to override
				SortKey m_key;  // Set the overridden bit values
				
				Override() :m_mask(0) ,m_key(0) {}
				SortKey Combine(SortKey key) const { return (key & ~m_mask) | m_key; }
				
				//void    Set    (SortKey mask, SortKey key) { m_mask = mask; m_key = key; }
				
				// Get/Set the alpha component of the sort key
				bool HasAlpha() const
				{
					return (m_mask & AlphaMask) != 0;
				}
				bool Alpha() const
				{
					return ((m_key & AlphaMask) >> AlphaOfs) != 0;
				}
				Override& ClearAlpha()
				{
					m_mask &= ~AlphaMask;
					m_key  &= ~AlphaMask;
					return *this;
				}
				Override& Alpha(bool has_alpha)
				{
					m_mask |= static_cast<SortKey>(AlphaMask);
					m_key  |= static_cast<SortKey>((has_alpha << AlphaOfs) & AlphaMask);
					return *this;
				}
				
				// Get/Set the sort group component of the sort key
				// 'group' = 0 is the default group, negative groups draw earlier, positive groups draw later
				bool HasGroup() const
				{
					return (m_mask & SortGroupMask) != 0;
				}
				int Group() const
				{
					return ((m_key & SortGroupMask) >> SortGroupOfs) - SortGroup_Default;
				}
				Override& ClearGroup()
				{
					m_mask &= ~SortGroupMask;
					m_key  &= ~SortGroupMask;
					return *this;
				}
				Override& Group(int group)
				{
					group += SortGroup_Default;
					PR_ASSERT(PR_DBG_RDR, group >= 0 && group < MaxSortGroups, "sort group out of range");
					m_mask |= static_cast<SortKey>(SortGroupMask);
					m_key  |= static_cast<SortKey>((group << SortGroupOfs) & SortGroupMask);
					return *this;
				}
			};
			
			// Construct a standard sort key for a render nugget
			inline pr::rdr::SortKey make(RenderNugget const& nugget)
			{
				pr::rdr::Material const& mat = nugget.m_material;
				bool alpha = false;
				
				SortKey key = 0;
				if (mat.m_diffuse_texture)
				{
					TexInfo const& info = mat.m_diffuse_texture->m_info;
					key   |= info.SortId << TextureIdOfs;
					alpha |= info.Alpha;
					PR_ASSERT(PR_DBG_RDR, info.SortId < MaxTextureId, "texture sort id overflow");
				}
				if (mat.m_effect)
				{
					key |= mat.m_effect->m_sort_id << EffectIdOfs;
					PR_ASSERT(PR_DBG_RDR, mat.m_effect->m_sort_id < MaxEffectId, "effect sort id overflow");
				}
				
				rs::State const* rsb = mat.m_rsb.Find(D3DRS_ALPHABLENDENABLE);
				if (rsb && rsb->m_state == TRUE)
				{
					alpha |= true;
				}

				key |= alpha ? SortGroup_AlphaBack : SortGroup_Opaques;
				key |= alpha << AlphaOfs;
				return key;
			}
		}
	}
}

#endif
