//***************************************************************************
//
//	Material Map
//
//***************************************************************************
//
//	A struct for mapping material indices to Material's
//
#ifndef PR_RDR_MATERIAL_MAP_H
#define PR_RDR_MATERIAL_MAP_H

#include "PR/Common/StdMap.h"
#include "PR/Renderer/Materials/Material.h"

namespace pr
{
	namespace rdr
	{
		struct MaterialMap
		{
			uint size() const										{ return (uint)m_map.size(); }
			      Material& operator [] (uint material_index)		{ return m_map[material_index]; }
			const Material& operator [] (uint material_index) const	{ PR_ASSERT(PR_DBG_RDR, m_map.find(material_index) != m_map.end()); return m_map.find(material_index)->second; }

		private:
			std::map<uint, Material> m_map;
		};
	}//namespace rdr
}//namespace pr

#endif//PR_RDR_MATERIAL_MAP_H
