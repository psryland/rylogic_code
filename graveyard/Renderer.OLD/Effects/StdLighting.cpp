//***************************************************************************
//
//	StdLighting
//
//***************************************************************************
//	This class contains methods related to the common variable handles

#include "Stdafx.h"
#include "PR/Renderer/Effects/StdLighting.h"
#include "PR/Renderer/Light.h"

using namespace pr;
using namespace pr::rdr;
using namespace pr::rdr::effect;

//*****
// Constructor
StdLighting::StdLighting()
:m_ws_light_position	(0)
,m_ws_light_direction	(0)
,m_light_ambient		(0)
,m_light_diffuse		(0)
,m_light_specular		(0)
,m_specular_power		(0)
{}

//*****
// Destructor
StdLighting::~StdLighting() {}

//*****
// Set the parameters handles used for std lighting
void StdLighting::GetParameterHandles(D3DPtr<ID3DXEffect> effect)
{
	m_ws_light_position		= effect->GetParameterByName(0, "g_ws_light_position"	);
	m_ws_light_direction	= effect->GetParameterByName(0, "g_ws_light_direction"	);
	m_light_ambient			= effect->GetParameterByName(0, "g_light_ambient"		);
	m_light_diffuse			= effect->GetParameterByName(0, "g_light_diffuse"		);
	m_light_specular		= effect->GetParameterByName(0, "g_light_specular"		);
	m_specular_power		= effect->GetParameterByName(0, "g_specular_power"		);
}

//*****
// Set all lighting parameters
void StdLighting::SetLightingParams(const Light& light, D3DPtr<ID3DXEffect> effect)
{
	switch( light.GetType() )
	{
	case Light::Ambient:
		Verify(effect->SetFloatArray(m_light_ambient,		reinterpret_cast<const float*>(&light.m_ambient),		4));
		Verify(effect->SetFloatArray(m_light_diffuse,		reinterpret_cast<const float*>(&v4Zero),				4));
		Verify(effect->SetFloatArray(m_light_specular,		reinterpret_cast<const float*>(&v4Zero),				4));
		//Verify(effect->SetFloat		(m_specular_power,		light.m_specular_power									 ));
		//Verify(effect->SetFloatArray(m_ws_light_direction,	reinterpret_cast<const float*>(&light.m_direction),		4));
		//Verify(effect->SetFloatArray(m_ws_light_position,	reinterpret_cast<const float*>(&light.m_position),		4));
		break;
	case Light::Directional:
		Verify(effect->SetFloatArray(m_light_ambient,		reinterpret_cast<const float*>(&light.m_ambient),		4));
		Verify(effect->SetFloatArray(m_light_diffuse,		reinterpret_cast<const float*>(&light.m_diffuse),		4));
		Verify(effect->SetFloatArray(m_light_specular,		reinterpret_cast<const float*>(&light.m_specular),		4));
		Verify(effect->SetFloat		(m_specular_power,		light.m_specular_power									 ));
		Verify(effect->SetFloatArray(m_ws_light_direction,	reinterpret_cast<const float*>(&light.m_direction),		4));
		//Verify(effect->SetFloatArray(m_ws_light_position,	reinterpret_cast<const float*>(&light.m_position),		4));
		break;
	case Light::Point:
		Verify(effect->SetFloatArray(m_light_ambient,		reinterpret_cast<const float*>(&light.m_ambient),		4));
		Verify(effect->SetFloatArray(m_light_diffuse,		reinterpret_cast<const float*>(&light.m_diffuse),		4));
		Verify(effect->SetFloatArray(m_light_specular,		reinterpret_cast<const float*>(&light.m_specular),		4));
		Verify(effect->SetFloat		(m_specular_power,		light.m_specular_power									 ));
		//Verify(effect->SetFloatArray(m_ws_light_direction,	reinterpret_cast<const float*>(&light.m_direction),		4));
		Verify(effect->SetFloatArray(m_ws_light_position,	reinterpret_cast<const float*>(&light.m_position),		4));
		break;
	case Light::Spot:
		Verify(effect->SetFloatArray(m_light_ambient,		reinterpret_cast<const float*>(&light.m_ambient),		4));
		Verify(effect->SetFloatArray(m_light_diffuse,		reinterpret_cast<const float*>(&light.m_diffuse),		4));
		Verify(effect->SetFloatArray(m_light_specular,		reinterpret_cast<const float*>(&light.m_specular),		4));
		Verify(effect->SetFloat		(m_specular_power,		light.m_specular_power									 ));
		Verify(effect->SetFloatArray(m_ws_light_direction,	reinterpret_cast<const float*>(&light.m_direction),		4));
		Verify(effect->SetFloatArray(m_ws_light_position,	reinterpret_cast<const float*>(&light.m_position),		4));
		break;
	default: PR_ERROR_STR(PR_DBG_RDR, "Unknown light type");
	};
}
