//***************************************************************************
//
//	StdLighting
//
//***************************************************************************
//	This class contains methods related to the standard lighting variable handles

#ifndef EFFECT_StdLighting_H
#define EFFECT_StdLighting_H

#include "PR/Common/D3DPtr.h"
#include "PR/Renderer/Forward.h"

namespace pr
{
	namespace rdr
	{
        namespace effect
		{
			class StdLighting
			{
			public:
				StdLighting();
				virtual ~StdLighting();

			protected:
				void	GetParameterHandles(D3DPtr<ID3DXEffect> effect);
				void	SetLightingParams(const Light& light, D3DPtr<ID3DXEffect> effect);

			protected:
				// StdLighting parameter handles
				D3DXHANDLE	m_ws_light_position;
				D3DXHANDLE	m_ws_light_direction;
				D3DXHANDLE	m_light_ambient;
				D3DXHANDLE	m_light_diffuse;
				D3DXHANDLE	m_light_specular;
				D3DXHANDLE	m_specular_power;
			};
		}//namespace effect
	}//namespace rdr
}//namespace pr

#endif//EFFECT_StdLighting_H
