//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_RENDER_SORT_KEY_H
#define PR_RDR_RENDER_SORT_KEY_H

#include "pr/renderer11/forward.h"
#include "pr/renderer11/materials/material.h"
#include "pr/renderer11/materials/textures/texture2d.h"
#include "pr/renderer11/materials/shaders/shader.h"
#include "pr/renderer11/models/nugget.h"

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
			//          ######## ##                shader id   
			//        #                            has alpha   
			// #######                             sort group  highest priority, least common thing changed when processing the drawlist
			SortKey const SortKeyBits   (8U * sizeof(SortKey));
			
			SortKey const TextureIdBits (14U);
			SortKey const ShaderIdBits  (10U);
			SortKey const AlphaBits     (1U );
			SortKey const SortGroupBits (SortKeyBits - (AlphaBits + ShaderIdBits + TextureIdBits));
			
			SortKey const MaxTextureId  (1U << TextureIdBits);
			SortKey const MaxShaderId   (1U << ShaderIdBits );
			SortKey const MaxSortGroups (1U << SortGroupBits);
			
			SortKey const TextureIdOfs  (0U);
			SortKey const ShaderIdOfs   (0U + TextureIdBits);
			SortKey const AlphaOfs      (0U + TextureIdBits + ShaderIdBits);
			SortKey const SortGroupOfs  (0U + TextureIdBits + ShaderIdBits + AlphaBits);
			
			SortKey const TextureIdMask ((~0U >> (SortKeyBits - TextureIdBits)) << TextureIdOfs);
			SortKey const ShaderIdMask  ((~0U >> (SortKeyBits - ShaderIdBits )) << ShaderIdOfs );
			SortKey const AlphaMask     ((~0U >> (SortKeyBits - AlphaBits    )) << AlphaOfs    );
			SortKey const SortGroupMask ((~0U >> (SortKeyBits - SortGroupBits)) << SortGroupOfs);
			
			int const SortGroup_Default    ((MaxSortGroups >> 1) & 0xFFFFFFFF);
			int const SortGroup_Opaques    (SortGroup_Default    );
			int const SortGroup_Skybox     (SortGroup_Default + 1U);
			int const SortGroup_AlphaBack  (SortGroup_Default + 5U);
			int const SortGroup_AlphaFront (SortGroup_Default + 6U);
			
			// A sort key override is a mask that is applied to a sort key
			// to override specific parts of the sort key.
			struct Override
			{
				SortKey m_mask; // Set the bits to override
				SortKey m_key;  // Set the overridden bit values
				
				Override() :m_mask(0) ,m_key(0) {}
				SortKey Combine(SortKey key) const { return (key & ~m_mask) | m_key; }
				
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
					return static_cast<int>(((m_key & SortGroupMask) >> SortGroupOfs) - SortGroup_Default);
				}
				Override& ClearGroup()
				{
					m_mask &= ~SortGroupMask;
					m_key  &= ~SortGroupMask;
					return *this;
				}
				Override& Group(int group)
				{
					SortKey grp = group + SortGroup_Default;
					PR_ASSERT(PR_DBG_RDR, grp >= 0 && grp < MaxSortGroups, "sort group out of range");
					m_mask |= static_cast<SortKey>(SortGroupMask);
					m_key  |= static_cast<SortKey>((grp << SortGroupOfs) & SortGroupMask);
					return *this;
				}
			};
			
			// Construct a standard sort key for a render nugget
			inline pr::rdr::SortKey make(Nugget const& nugget)
			{
				pr::rdr::Material const& mat = nugget.m_material;
				bool alpha = false;
				
				SortKey key = 0;
				if (mat.m_tex_diffuse)
				{
					TextureDesc const& info = mat.m_tex_diffuse->m_info;
					key   |= info.SortId << TextureIdOfs;
					alpha |= info.Alpha;
					PR_ASSERT(PR_DBG_RDR, info.SortId < MaxTextureId, "texture sort id overflow");
				}
				if (mat.m_shader)
				{
					key |= mat.m_shader->m_sort_id << ShaderIdOfs;
					PR_ASSERT(PR_DBG_RDR, mat.m_shader->m_sort_id < MaxShaderId, "shader sort id overflow");
				}
				
				//rs::State const* rsb = mat.m_rsb.Find(D3DRS_ALPHABLENDENABLE);
				//if (rsb && rsb->m_state == TRUE)
				//{
				//	alpha |= true;
				//}

				key |= alpha ? SortGroup_AlphaBack : SortGroup_Opaques;
				key |= alpha << AlphaOfs;
				return key;
			}
		}
	}
}

#endif
