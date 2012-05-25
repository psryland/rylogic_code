//**********************************************************************************************
//
// Texture Filter
//
//**********************************************************************************************

#ifndef PR_RDR_TEXTURE_FILTER_H
#define PR_RDR_TEXTURE_FILTER_H

#include "PR/Renderer/D3DHeaders.h"

namespace pr
{
	namespace rdr
	{
		struct TextureFilter
		{
			enum { Mag, Mip, Min, NumberOf };
			TextureFilter()
			{
				m_filter[Mag] = D3DTEXF_POINT;
				m_filter[Mip] = D3DTEXF_POINT;
				m_filter[Min] = D3DTEXF_POINT;
			}
			D3DTEXTUREFILTERTYPE m_filter[NumberOf];
		};
	}//namespace rdr
}//namespace pr

#endif//PR_RDR_TEXTURE_FILTER_H
