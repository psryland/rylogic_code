//***************************************************************************
//
//	A default effect file
//
//***************************************************************************
#include "Stdafx.h"
#include "Crypt/Crypt.h"
#include "Renderer/Effects/EffectXYZLit.h"
#include "Renderer/Renderer.h"

using namespace Effect;

const char* EffectXYZLit_FX = 
"//*****************************************************************************	\n"
"//																					\n"
"// An effect file to reproduce the behaviour of the fixed function pipeline		\n"
"//																					\n"
"//*****************************************************************************	\n"
"																					\n"
"float4x4	g_World					: World;										\n"
"float4x4	g_View					: View;											\n"
"float4x4	g_Projection			: Projection;									\n"
"																					\n"
"int			g_CullMode								= 1;						\n"
"bool		g_SpecularEnable						= false;						\n"
"bool		g_ZWriteEnable							= true;							\n"
"bool		g_AlphaBlendEnable						= false;						\n"
"																					\n"
"float4		g_GlobalAmbient			: Ambient		= float4(0.5, 0.5, 0.5, 1.0);	\n"
"																					\n"
"float4		g_MaterialAmbient		: Ambient		= float4(1.0, 1.0, 1.0, 1.0);	\n"
"float4		g_MaterialDiffuse		: Diffuse		= float4(1.0, 1.0, 1.0, 1.0);	\n"
"float4		g_MaterialSpecular		: Specular		= float4(1.0, 1.0, 1.0, 1.0);	\n"
"float4		g_MaterialEmissive		: Emissive		= float4(0.0, 0.0, 0.0, 0.0);	\n"
"float		g_MaterialSpecularPower	: SpecularPower	= 10.0f;						\n"
"																					\n"
"bool		g_Lighting								= true;							\n"
"bool		g_LightEnable							= true;							\n"
"int			g_LightType								= 3;						\n"
"float4		g_LightAmbient			: Ambient		= float4(0.1, 0.1, 0.1, 1.0);	\n"
"float4		g_LightDiffuse			: Diffuse		= float4(1.0, 1.0, 1.0, 1.0);	\n"
"float4		g_LightSpecular			: Specular		= float4(1.0, 1.0, 1.0, 1.0);	\n"
"float3		g_LightPosition			: Position		= float3(0.0, 0.0, 0.0);		\n"
"float3		g_LightDirection		: Direction		= float3(0.0, 0.0, 1.0);		\n"
"float		g_LightRange							= 1000.0;						\n"
"float		g_LightFalloff							= 0.0;							\n"
"float		g_LightTheta							= 0.0;	// inner				\n"
"float		g_LightPhi								= 0.0;	// outer				\n"
"float		g_LightAttenuation0						= 1.0;							\n"
"float		g_LightAttenuation1						= 0.0;							\n"
"float		g_LightAttenuation2						= 0.0;							\n"
"																					\n"
"texture		g_Texture								= 0;						\n"
"																					\n"
"//-----------------------------------												\n"
"technique XYZLit																	\n"
"{																					\n"
"	pass p0																			\n"
"	{																				\n"
"//PSR...		WorldTransform[0]	= <g_World>;									\n"
"//PSR...		ViewTransform		= <g_View>;										\n"
"//PSR...		ProjectionTransform	= <g_Projection>;								\n"
"																					\n"
"		Lighting			= <g_Lighting>;											\n"
"		CullMode			= <g_CullMode>;											\n"
"		SpecularEnable		= <g_SpecularEnable>;									\n"
"		ZWriteEnable		= <g_ZWriteEnable>;										\n"
"		AlphaBlendEnable	= <g_AlphaBlendEnable>;									\n"
"		BlendOp				= Add;													\n"
"		SrcBlend			= SrcAlpha;												\n"
"		DestBlend			= DestAlpha;											\n"
"		Ambient				= <g_GlobalAmbient>;									\n"
"																					\n"
"		LightEnable[0]		= <g_LightEnable>;										\n"
"		LightType[0]		= <g_LightType>;										\n"
"		LightAmbient[0]		= <g_LightAmbient>;										\n"
"		LightDiffuse[0]		= <g_LightDiffuse>;										\n"
"		LightSpecular[0]	= <g_LightSpecular>;									\n"
"		LightPosition[0]	= <g_LightPosition>;									\n"
"		LightDirection[0]	= <g_LightDirection>;									\n"
"		LightRange[0]		= <g_LightRange>;										\n"
"		LightFalloff[0]		= <g_LightFalloff>;										\n"
"		LightTheta[0]		= <g_LightTheta>;										\n"
"		LightPhi[0]			= <g_LightPhi>;											\n"
"		LightAttenuation0[0]= <g_LightAttenuation0>;								\n"
"		LightAttenuation1[0]= <g_LightAttenuation1>;								\n"
"		LightAttenuation2[0]= <g_LightAttenuation2>;								\n"
"																					\n"
"		MaterialAmbient		= <g_MaterialAmbient>;									\n"
"		MaterialDiffuse		= <g_MaterialDiffuse>;									\n"
"		MaterialSpecular	= <g_MaterialSpecular>;									\n"
"		MaterialEmissive	= <g_MaterialEmissive>;									\n"
"		MaterialPower		= <g_MaterialSpecularPower>;							\n"
"																					\n"
"		// Just use the color														\n"
"		Texture[0]			= <g_Texture>;											\n"
"		ColorArg1[0]		= Diffuse;												\n"
"		ColorArg2[0]		= Texture;												\n"
"		AlphaArg1[0]		= Diffuse;												\n"
"		AlphaArg2[0]		= Texture;												\n"
"		ColorOp[0]			= Modulate;												\n"
"		AlphaOp[0]			= Modulate;												\n"
"       ColorOp[1]			= Disable;												\n"
"		AlphaOp[1]			= Disable;												\n"
"	}																				\n"
"}																					\n";

//*****
// Constructor
EffectXYZLit::EffectXYZLit()
:EffectXYZ()
,m_Lighting(0)
,m_SpecularEnable(0)
,m_LightEnable(0)
,m_LightType(0)
,m_LightAmbient(0)
,m_LightDiffuse(0)
,m_LightSpecular(0)
,m_LightPosition(0)
,m_LightDirection(0)
,m_LightRange(0)
,m_LightFalloff(0)
,m_LightTheta(0)
,m_LightPhi(0)
,m_LightAttenuation0(0)
,m_LightAttenuation1(0)
,m_LightAttenuation2(0)
,m_last_lt(Light::Ambient)
{}

//*****
// Return a unique id for this effect
uint EffectXYZLit::GetId() const
{
	const char* uid = "Builtin effect xyz lit";
	static Crypt::CRC key = Crypt::Crc(uid, (uint)strlen(uid));
	return key;
}

//*****
// Load the source data for this effect and return a CRC for the data
bool EffectXYZLit::LoadSourceData()
{
	uint size = (uint)strlen(EffectXYZLit_FX);
	m_source_data.Resize(size);
	CopyMemory(&m_source_data[0], EffectXYZLit_FX, size);
	return true;
}

//*****
// Get the parameters from this technique
bool EffectXYZLit::GetParameterHandles()
{
	EffectXYZ::GetParameterHandles();
	m_Lighting				= m_effect->GetParameterByName(0, "g_Lighting"				);	if( !m_Lighting					) return false;
	m_SpecularEnable		= m_effect->GetParameterByName(0, "g_SpecularEnable"			);	if( !m_SpecularEnable			) return false;
	m_LightEnable			= m_effect->GetParameterByName(0, "g_LightEnable"			);	if( !m_LightEnable				) return false;
	m_LightType				= m_effect->GetParameterByName(0, "g_LightType"				);	if( !m_LightType				) return false;
	m_LightAmbient			= m_effect->GetParameterByName(0, "g_LightAmbient"			);	if( !m_LightAmbient				) return false;
	m_LightDiffuse			= m_effect->GetParameterByName(0, "g_LightDiffuse"			);	if( !m_LightDiffuse				) return false;
	m_LightSpecular			= m_effect->GetParameterByName(0, "g_LightSpecular"			);	if( !m_LightSpecular			) return false;
	m_LightPosition			= m_effect->GetParameterByName(0, "g_LightPosition"			);	if( !m_LightPosition			) return false;
	m_LightDirection		= m_effect->GetParameterByName(0, "g_LightDirection"			);	if( !m_LightDirection			) return false;
	m_LightRange			= m_effect->GetParameterByName(0, "g_LightRange"				);	if( !m_LightRange				) return false;
	m_LightFalloff			= m_effect->GetParameterByName(0, "g_LightFalloff"			);	if( !m_LightFalloff				) return false;
	m_LightTheta			= m_effect->GetParameterByName(0, "g_LightTheta"				);	if( !m_LightTheta				) return false;
	m_LightPhi				= m_effect->GetParameterByName(0, "g_LightPhi"				);	if( !m_LightPhi					) return false;
	m_LightAttenuation0		= m_effect->GetParameterByName(0, "g_LightAttenuation0"		);	if( !m_LightAttenuation0		) return false;
	m_LightAttenuation1		= m_effect->GetParameterByName(0, "g_LightAttenuation1"		);	if( !m_LightAttenuation1		) return false;
	m_LightAttenuation2		= m_effect->GetParameterByName(0, "g_LightAttenuation2"		);	if( !m_LightAttenuation2		) return false;
	return true;
}

//*****
// Set lighting effect parameters
void EffectXYZLit::SetLightingParameters()
{
	const Light&			light			= m_renderer->GetLight(0);			PR_ASSERT(light.IsValid());
	const D3DCOLORVALUE&	global_ambient	= m_renderer->GetGlobalAmbient();

	m_last_lt = light.GetType();
	if( m_last_lt == Light::Ambient )
	{
		Verify(m_effect->SetBool		(m_Lighting				,false));
		Verify(m_effect->SetFloatArray	(m_GlobalAmbient		,(float*)&global_ambient		,4));
		Verify(m_effect->SetBool		(m_SpecularEnable		,false));
		Verify(m_effect->SetBool		(m_LightEnable			,false));
	}
	else
	{
		Verify(m_effect->SetBool		(m_Lighting				,true));
		Verify(m_effect->SetFloatArray	(m_GlobalAmbient		,(float*)&global_ambient		,4));
		Verify(m_effect->SetBool		(m_SpecularEnable		,true));
		Verify(m_effect->SetBool		(m_LightEnable			,true));
		Verify(m_effect->SetInt			(m_LightType			,m_last_lt));
		Verify(m_effect->SetFloatArray	(m_LightAmbient			,(float*)&light.m_ambient		,4));
		Verify(m_effect->SetFloatArray	(m_LightDiffuse			,(float*)&light.m_diffuse		,4));
		Verify(m_effect->SetFloatArray	(m_LightSpecular		,(float*)&light.m_specular		,4));
		Verify(m_effect->SetFloatArray	(m_LightPosition		,(float*)&light.m_position.v	,3));
		Verify(m_effect->SetFloatArray	(m_LightDirection		,(float*)&light.m_direction.v	,3));
		Verify(m_effect->SetFloat		(m_LightRange			,light.m_range));
		Verify(m_effect->SetFloat		(m_LightFalloff			,light.m_falloff));
		Verify(m_effect->SetFloat		(m_LightTheta			,light.m_inner_angle));
		Verify(m_effect->SetFloat		(m_LightPhi				,light.m_outer_angle));
		Verify(m_effect->SetFloat		(m_LightAttenuation0	,light.m_attenuation0));
		Verify(m_effect->SetFloat		(m_LightAttenuation1	,light.m_attenuation1));	
		Verify(m_effect->SetFloat		(m_LightAttenuation2	,light.m_attenuation2));
	}
}

//*****
// Set the parameter block for this effect
bool EffectXYZLit::SetParameterBlock()
{
	if( !EffectXYZ::SetParameterBlock() ) return false;
	SetLightingParameters();
	return true;
}

//*****
// Set parameters for an instance midway through a pass
bool EffectXYZLit::MidPass(DrawListElement* draw_list_element)
{
	bool commit_needed = EffectXYZ::MidPass(draw_list_element);

	//const Light& light = m_renderer->GetLight(0);
	//if( m_last_lt != light.GetType() )
	{
		SetLightingParameters();
		commit_needed = true;
	}

	return commit_needed;
}

//*****
// Set parameters at the end of a pass
void EffectXYZLit::PostPass()
{
	EffectXYZ::PostPass();
	m_last_lt = Light::Ambient;
}

//*****
// Return the render states used in this effect
void EffectXYZLit::GetRenderStates(const D3DRENDERSTATETYPE*& rs, uint& num_rs) const
{
	static const D3DRENDERSTATETYPE RenderStates[] =
	{ D3DRS_AMBIENT, D3DRS_LIGHTING, D3DRS_CULLMODE, D3DRS_SPECULARENABLE, D3DRS_ZWRITEENABLE, D3DRS_ALPHABLENDENABLE };
	static const uint				NumRenderStates = sizeof(RenderStates) / sizeof(D3DRENDERSTATETYPE);
	rs		= RenderStates;
	num_rs	= 0;//NumRenderStates;
}
