//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************
#ifndef LDR_TEXFILTER
#define LDR_TEXFILTER(name, hashvalue)
#endif
#ifndef LDR_TEXADDR
#define LDR_TEXADDR(name, hashvalue)
#endif

LDR_TEXFILTER(NONE          ,0x0a3c9f03)
LDR_TEXFILTER(POINT         ,0x036f06fc)
LDR_TEXFILTER(LINEAR        ,0x12fd8c42)
LDR_TEXFILTER(ANISOTROPIC   ,0x1265d915)
LDR_TEXFILTER(PYRAMIDALQUAD ,0x197a413d)
LDR_TEXFILTER(GAUSSIANQUAD  ,0x1cd9e882)

LDR_TEXADDR(WRAP       ,0x00d43ffa)
LDR_TEXADDR(MIRROR     ,0x03d86932)
LDR_TEXADDR(CLAMP      ,0x1d0d7e7e)
LDR_TEXADDR(BORDER     ,0x07e35b1c)
LDR_TEXADDR(MIRRORONCE ,0x1149dd8a)

#undef LDR_TEXFILTER
#undef LDR_TEXADDR

#ifndef PR_RDR_TEXTURE_FILTER_H
#define PR_RDR_TEXTURE_FILTER_H

#include "pr/renderer/types/forward.h"

namespace pr
{
	namespace rdr
	{
		struct TextureFilter
		{
			D3DTEXTUREFILTERTYPE m_mag;
			D3DTEXTUREFILTERTYPE m_mip;
			D3DTEXTUREFILTERTYPE m_min;
			TextureFilter(D3DTEXTUREFILTERTYPE type = D3DTEXF_LINEAR) :m_mag(type) ,m_mip(type) ,m_min(type) {}
			TextureFilter(D3DTEXTUREFILTERTYPE mag, D3DTEXTUREFILTERTYPE mip, D3DTEXTUREFILTERTYPE min) :m_mag(mag) ,m_mip(mip) ,m_min(min) {}
		};

		struct TextureAddrMode
		{
			D3DTEXTUREADDRESS m_addrU, m_addrV, m_addrW;
			TextureAddrMode(D3DTEXTUREADDRESS addrU = D3DTADDRESS_CLAMP, D3DTEXTUREADDRESS addrV = D3DTADDRESS_CLAMP, D3DTEXTUREADDRESS addrW = D3DTADDRESS_CLAMP) :m_addrU(addrU) ,m_addrV(addrV) ,m_addrW(addrW) {}
		};

		// Convert a texture filter type into a string
		inline char const* ToString(D3DTEXTUREFILTERTYPE tex_filter_type)
		{
			switch (tex_filter_type)
			{
			default: return "";
			#define LDR_TEXFILTER(name, hashvalue) case D3DTEXF_##name: return #name;
			#include "texturefilter.h"
			}
		}
		inline D3DTEXTUREFILTERTYPE ToTexFilter(char const* filter)
		{
			switch (pr::hash::HashLwr(filter))
			{
			default: return D3DTEXF_NONE;
			#define LDR_TEXFILTER(name, hashvalue) case hashvalue: return D3DTEXF_##name;
			#include "texturefilter.h"
			}
		}

		// Convert a texture addressing mode into a string
		inline char const* ToString(D3DTEXTUREADDRESS tex_addr_mode)
		{
			switch (tex_addr_mode)
			{
			default: return "";
			#define LDR_TEXADDR(name, hashvalue) case D3DTADDRESS_##name: return #name;
			#include "texturefilter.h"
			}
		}
		inline D3DTEXTUREADDRESS ToTexAddr(char const* addr_mode)
		{
			switch (pr::hash::HashLwr(addr_mode))
			{
			default: return D3DTADDRESS_CLAMP;
			#define LDR_TEXADDR(name, hashvalue) case hashvalue: return D3DTADDRESS_##name;
			#include "texturefilter.h"
			}
		}
	}
}

#endif
