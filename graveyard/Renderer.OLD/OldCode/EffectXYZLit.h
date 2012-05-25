//***************************************************************************
//
//	A default effect file
//
//***************************************************************************
#ifndef EFFECT_XYZ_LIT_H
#define EFFECT_XYZ_LIT_H

#include "Renderer/Effects/EffectXYZ.h"
#include "Renderer/Light.h"

namespace Effect
{
	class EffectXYZLit : public EffectXYZ
	{
	public:
		EffectXYZLit();
		virtual ~EffectXYZLit(){}	// Do not call Release() here because any temporary copy of an EffectXYZLit will get destroyed and release the effect.

		// Overrides
		virtual bool	MidPass(DrawListElement* draw_list_element);
		virtual void	PostPass();
		virtual void	GetRenderStates(const D3DRENDERSTATETYPE*& rs, uint& num_rs) const;
		virtual uint	GetId() const;

	protected:
		virtual bool	LoadSourceData();
		virtual bool	GetParameterHandles();
		virtual bool	SetParameterBlock();
		void			SetLightingParameters();

	protected:
		D3DXHANDLE	m_Lighting;
		D3DXHANDLE	m_SpecularEnable;
		D3DXHANDLE	m_LightEnable;
		D3DXHANDLE	m_LightType;
		D3DXHANDLE	m_LightAmbient;
		D3DXHANDLE	m_LightDiffuse;
		D3DXHANDLE	m_LightSpecular;
		D3DXHANDLE	m_LightPosition;
		D3DXHANDLE	m_LightDirection;
		D3DXHANDLE	m_LightRange;
		D3DXHANDLE	m_LightFalloff;
		D3DXHANDLE	m_LightTheta;
		D3DXHANDLE	m_LightPhi;
		D3DXHANDLE	m_LightAttenuation0;
		D3DXHANDLE	m_LightAttenuation1;
		D3DXHANDLE	m_LightAttenuation2;

		Light::Type m_last_lt;
	};

}//namespace Effect

#endif//EFFECT_XYZ_LIT_H
