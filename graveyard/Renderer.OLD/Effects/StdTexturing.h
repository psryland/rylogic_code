//***************************************************************************
//
//	StdTexturing
//
//***************************************************************************

#ifndef EFFECT_StdTexturing_H
#define EFFECT_StdTexturing_H

#include "PR/Common/D3DPtr.h"
#include "PR/Renderer/Forward.h"

namespace pr
{
	namespace rdr
	{
        namespace effect
		{
			class StdTexturing
			{
			public:
				StdTexturing();
				virtual ~StdTexturing();

			protected:
				void	GetParameterHandles(D3DPtr<ID3DXEffect> effect);
				void	SetTextures(const DrawListElement& draw_list_element, D3DPtr<ID3DXEffect> effect);

			protected:
				// StdTexturing parameter handles
				D3DXHANDLE	m_texture0;
			};
		}//namespace effect
	}//namespace rdr
}//namespace pr

#endif//EFFECT_StdTexturing_H
