//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/textures/texture2d.h"
#include "pr/renderer11/shaders/shader.h"
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
		namespace sortkey
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
		}
		#define PR_ENUM(x)\
			x(Default    ,= ((sortkey::MaxSortGroups >> 1) & 0xFFFFFFFF))\
			x(Skybox     ,= Default + 1U)\
			x(AlphaBack  ,= Default + 5U)\
			x(AlphaFront ,= Default + 6U)
		PR_DEFINE_ENUM2(ESortGroup, PR_ENUM);
		#undef PR_ENUM

		// A sort key override is a mask that is applied to a sort key
		// to override specific parts of the sort key.
		struct SKOverride
		{
			SortKey m_mask; // Set the bits to override
			SortKey m_key;  // Set the overridden bit values

			SKOverride() :m_mask(0) ,m_key(0) {}
			SortKey Combine(SortKey key) const { return (key & ~m_mask) | m_key; }

			// Get/Set the alpha component of the sort key
			bool HasAlpha() const
			{
				return (m_mask & sortkey::AlphaMask) != 0;
			}
			bool Alpha() const
			{
				return ((m_key & sortkey::AlphaMask) >> sortkey::AlphaOfs) != 0;
			}
			SKOverride& ClearAlpha()
			{
				m_mask &= ~sortkey::AlphaMask;
				m_key  &= ~sortkey::AlphaMask;
				return *this;
			}
			SKOverride& Alpha(bool has_alpha)
			{
				m_mask |= static_cast<SortKey>(sortkey::AlphaMask);
				m_key  |= static_cast<SortKey>((has_alpha << sortkey::AlphaOfs) & sortkey::AlphaMask);
				return *this;
			}

			// Get/Set the sort group component of the sort key
			// 'group' = 0 is the default group, negative groups draw earlier, positive groups draw later
			bool HasGroup() const
			{
				return (m_mask & sortkey::SortGroupMask) != 0;
			}
			int Group() const
			{
				return static_cast<int>(((m_key & sortkey::SortGroupMask) >> sortkey::SortGroupOfs) - ESortGroup::Default);
			}
			SKOverride& ClearGroup()
			{
				m_mask &= ~sortkey::SortGroupMask;
				m_key  &= ~sortkey::SortGroupMask;
				return *this;
			}
			SKOverride& Group(ESortGroup group)
			{
				PR_ASSERT(PR_DBG_RDR, group >= 0 && group < sortkey::MaxSortGroups, "sort group out of range");
				m_mask |= static_cast<SortKey>(sortkey::SortGroupMask);
				m_key  |= static_cast<SortKey>((group << sortkey::SortGroupOfs) & sortkey::SortGroupMask);
				return *this;
			}
		};

		// Construct a standard sort key for a render nugget
		inline pr::rdr::SortKey MakeSortKey(NuggetProps const& ddata)
		{
			(void)ddata;
			bool alpha = false;

			// Make a sort key that is the same for all nuggets with the  same shaders and same state
			SortKey key = 0;
			//if (ddata.m_sset.m_vs != nullptr)
			//{
			//	key ^= ddata.m_sset.m_vs->m_sort_id;
			//}


//todo
			//if (ddata.m_shader)
			//{
			//	key |= ddata.m_shader->m_sort_id << sortkey::ShaderIdOfs;
			//	PR_ASSERT(PR_DBG_RDR, ddata.m_shader->m_sort_id < sortkey::MaxShaderId, "shader sort id overflow");
			//}
			//if (ddata.m_tex_diffuse)
			//{
			//	key   |= ddata.m_tex_diffuse->m_sort_id << sortkey::TextureIdOfs;
			//	alpha |= ddata.m_tex_diffuse->m_has_alpha;
			//	PR_ASSERT(PR_DBG_RDR, ddata.m_tex_diffuse->m_sort_id < sortkey::MaxTextureId, "texture sort id overflow");
			//}

			//rs::State const* rsb = mat.m_rsb.Find(D3DRS_ALPHABLENDENABLE);
			//if (rsb && rsb->m_state == TRUE)
			//{
			//	alpha |= true;
			//}

			key |= alpha ? ESortGroup::AlphaBack : ESortGroup::Default;
			key |= alpha << sortkey::AlphaOfs;
			return key;
		}
	}
}
