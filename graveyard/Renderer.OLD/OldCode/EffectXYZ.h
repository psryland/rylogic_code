//***************************************************************************
//
//	A default effect file
//
//***************************************************************************

#ifndef EFFECT_XYZ_H
#define EFFECT_XYZ_H

#include "Renderer/Effects/EffectBase.h"

namespace Effect
{
	class EffectXYZ : public Base
	{
	public:
		EffectXYZ();
		virtual ~EffectXYZ(){}	// Do not call Release() here because any temporary copy of an EffectEffectXYZ will get destroyed and release the effect.

		// Overrides
		virtual bool	MidPass(DrawListElement* draw_list_element);
		virtual void	PostPass();
		virtual void	GetRenderStates(const D3DRENDERSTATETYPE*& rs, uint& num_rs) const;
		virtual uint	GetId() const;

	protected:
		virtual bool	LoadSourceData();
		virtual bool	GetParameterHandles();
		virtual bool	SetParameterBlock();
		void			SetTransforms(DrawListElement* draw_list_element);
		void			SetMaterialParameters(MaterialIndex mat_index);

	protected:
		D3DXHANDLE	m_World;
		D3DXHANDLE	m_View;
		D3DXHANDLE	m_Projection;
		D3DXHANDLE	m_CullMode;
		D3DXHANDLE	m_ZWriteEnable;
		D3DXHANDLE	m_AlphaBlendEnable;
		D3DXHANDLE	m_GlobalAmbient;
		D3DXHANDLE	m_MaterialAmbient;
		D3DXHANDLE	m_MaterialDiffuse;
		D3DXHANDLE	m_MaterialSpecular;
		D3DXHANDLE	m_MaterialEmissive;
		D3DXHANDLE	m_MaterialSpecularPower;
		D3DXHANDLE	m_Texture;

		// Caching members
		DrawListElement*	m_last_dle;
		uint				m_last_cullmode;
	};
}//namespace Effect

#endif//EFFECT_XYZ_H
