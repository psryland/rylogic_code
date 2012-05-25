//***************************************************************************
//
//	Common
//
//***************************************************************************
//	This class contains methods related to the common variable handles

#ifndef EFFECT_Common_H
#define EFFECT_Common_H

#include "PR/Common/D3DPtr.h"
#include "PR/Renderer/Effects/EffectBase.h"

namespace pr
{
	namespace rdr
	{
		namespace effect
		{
			class Common
			{
			public:
				Common();
				virtual ~Common();

			protected:
				void	GetParameterHandles(D3DPtr<ID3DXEffect> effect);
				void	SetTransforms(const Viewport& viewport, const DrawListElement& draw_list_element, D3DPtr<ID3DXEffect> effect);

			protected:
				// Common parameter handles
				D3DXHANDLE	m_object_to_world;
				D3DXHANDLE	m_object_to_camera;
				D3DXHANDLE	m_object_to_screen;		
				D3DXHANDLE	m_world_to_screen;
				D3DXHANDLE	m_world_to_camera;
				D3DXHANDLE	m_camera_to_world;
			};
		}//namespace effect
	}//namespace rdr
}//namespace pr

#endif//EFFECT_Common_H
