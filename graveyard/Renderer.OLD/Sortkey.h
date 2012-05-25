//**************************************************************************
//
//	Sort key
//
//**************************************************************************

#ifndef PR_RDR_SORT_KEY_H
#define PR_RDR_SORT_KEY_H

#include "PR/Common/PRSortKey.h"
#include "PR/Renderer/Materials/Material.h"
#include "PR/Renderer/Materials/Texture.h"
#include "PR/Renderer/Effects/EffectBase.h"

namespace pr
{
	namespace rdr
	{
		inline SortKey MakeSortKey(uint render_bin, rdr::Material material)
		{
			SortKey key;
			sortkey::High(key)  = render_bin << 26;
			sortkey::High(key) |= (material.m_texture->Alpha()) ? (1 << 25) : (0);
			sortkey::High(key) |= material.m_effect->m_id >> 16;
			sortkey::Low (key)  = material.m_texture->m_id << 16;
			return key;
		}
	}//namespace rdr
}//namespace pr

#endif//PR_RDR_SORT_KEY_H
