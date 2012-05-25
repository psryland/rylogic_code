//***************************************************************************
//
//
//
//***************************************************************************
#include "Stdafx.h"
#include "Crypt/Crypt.h"
#include "Renderer/Effects/EffectXYZ.h"
#include "Renderer/Renderer.h"

using namespace Effect;

const char* EffextXYZ_FX = 
"//*****************************************************************************		\n"
"//																						\n"
"// An effect file to reproduce the behaviour of the fixed function pipeline			\n"
"//																						\n"
"//*****************************************************************************		\n"
"																						\n"
"float4x4	g_World					: World;											\n"
"float4x4	g_View					: View;												\n"
"float4x4	g_Projection			: Projection;										\n"
"																						\n"
"int		g_CullMode								= 1;								\n"
"bool		g_ZWriteEnable							= true;								\n"
"bool		g_AlphaBlendEnable						= false;							\n"
"																						\n"
"bool		g_Lighting								= false;							\n"
"float4		g_GlobalAmbient			: Ambient		= float4(0.5, 0.5, 0.5, 1.0);		\n"
"																						\n"
"float4		g_MaterialAmbient		: Ambient		= float4(1.0, 1.0, 1.0, 1.0);		\n"
"float4		g_MaterialDiffuse		: Diffuse		= float4(1.0, 1.0, 1.0, 1.0);		\n"
"float4		g_MaterialSpecular		: Specular		= float4(1.0, 1.0, 1.0, 1.0);		\n"
"float4		g_MaterialEmissive		: Emissive		= float4(0.0, 0.0, 0.0, 0.0);		\n"
"float		g_MaterialSpecularPower	: SpecularPower	= 10.0f;							\n"
"																						\n"
"texture	g_Texture								= 0;								\n"
"																						\n"
"//-----------------------------------													\n"
"technique XYZ																			\n"
"{																						\n"
"	pass p0																				\n"
"	{																					\n"
"		VertexShaderConstant4[0]	= <g_World>;										\n"
"		//WorldTransform[0]			= <g_World>;										\n"
"		//ViewTransform				= <g_View>;											\n"
"		//ProjectionTransform		= <g_Projection>;									\n"
"																						\n"
"		Lighting			= <g_Lighting>;												\n"
"		CullMode			= <g_CullMode>;												\n"
"		SpecularEnable		= false;													\n"
"		ZWriteEnable		= <g_ZWriteEnable>;											\n"
"		AlphaBlendEnable	= <g_AlphaBlendEnable>;										\n"
"		BlendOp				= Add;														\n"
"		SrcBlend			= SrcAlpha;													\n"
"		DestBlend			= DestAlpha;												\n"
"		Ambient				= <g_GlobalAmbient>;										\n"
"																						\n"
"		LightEnable[0]		= false;													\n"
"																						\n"
"		MaterialAmbient		= <g_MaterialAmbient>;										\n"
"		MaterialDiffuse		= <g_MaterialDiffuse>;										\n"
"		MaterialSpecular	= <g_MaterialSpecular>;										\n"
"		MaterialEmissive	= <g_MaterialEmissive>;										\n"
"		MaterialPower		= <g_MaterialSpecularPower>;								\n"
"																						\n"
"		// Just use the color															\n"
"		Texture[0]			= <g_Texture>;												\n"
"		ColorArg1[0]		= Diffuse;													\n"
"		ColorArg2[0]		= Texture;													\n"
"		AlphaArg1[0]		= Diffuse;													\n"
"		AlphaArg2[0]		= Texture;													\n"
"		ColorOp[0]			= Modulate;													\n"
"		AlphaOp[0]			= Modulate;													\n"
"      	ColorOp[1]			= Disable;													\n"
"		AlphaOp[1]			= Disable;													\n"
"	}																					\n"
"}																						\n";

//*****
// Constructor
EffectXYZ::EffectXYZ()
:Base()
,m_World(0)
,m_View(0)
,m_Projection(0)
,m_CullMode(0)
,m_ZWriteEnable(0)
,m_AlphaBlendEnable(0)
,m_GlobalAmbient(0)
,m_MaterialAmbient(0)
,m_MaterialDiffuse(0)
,m_MaterialSpecular(0)
,m_MaterialEmissive(0)
,m_MaterialSpecularPower(0)
,m_Texture(0)
,m_last_dle(0)
,m_last_cullmode(D3DCULL_FORCE_DWORD)
{}

//*****
// Return a unique id for this effect
uint EffectXYZ::GetId() const
{
	const char* uid = "Builtin effect xyz";
	static Crypt::CRC key = Crypt::Crc(uid, (uint)strlen(uid));
	return key;
}

//*****
// Load the source data for this effect and return a CRC for the data
bool EffectXYZ::LoadSourceData()
{
	uint size = (uint)strlen(EffextXYZ_FX);
	m_source_data.Resize(size);
	CopyMemory(&m_source_data[0], EffextXYZ_FX, size);
	return true;
}

//*****
// Get the parameters from this technique
bool EffectXYZ::GetParameterHandles()
{
	m_World					= m_effect->GetParameterByName(0, "g_World"					);	if( !m_World					) return false;
	m_View					= m_effect->GetParameterByName(0, "g_View"					);	if( !m_View						) return false;
	m_Projection			= m_effect->GetParameterByName(0, "g_Projection"				);	if( !m_Projection				) return false;
	m_CullMode				= m_effect->GetParameterByName(0, "g_CullMode"				);	if( !m_CullMode					) return false;
	m_ZWriteEnable			= m_effect->GetParameterByName(0, "g_ZWriteEnable"			);	if( !m_ZWriteEnable				) return false;
	m_AlphaBlendEnable		= m_effect->GetParameterByName(0, "g_AlphaBlendEnable"		);	if( !m_AlphaBlendEnable			) return false;
	m_GlobalAmbient			= m_effect->GetParameterByName(0, "g_GlobalAmbient"			);	if( !m_GlobalAmbient			) return false;
	m_MaterialAmbient		= m_effect->GetParameterByName(0, "g_MaterialAmbient"		);	if( !m_MaterialAmbient			) return false;
	m_MaterialDiffuse		= m_effect->GetParameterByName(0, "g_MaterialDiffuse"		);	if( !m_MaterialDiffuse			) return false;
	m_MaterialSpecular		= m_effect->GetParameterByName(0, "g_MaterialSpecular"		);	if( !m_MaterialSpecular			) return false;
	m_MaterialEmissive		= m_effect->GetParameterByName(0, "g_MaterialEmissive"		);	if( !m_MaterialEmissive			) return false;
	m_MaterialSpecularPower	= m_effect->GetParameterByName(0, "g_MaterialSpecularPower"	);	if( !m_MaterialSpecularPower	) return false;
	m_Texture				= m_effect->GetParameterByName(0, "g_Texture"				);	if( !m_Texture					) return false;
	return true;
}

//*****
// Set the parameter block for this effect
bool EffectXYZ::SetParameterBlock()
{
	return true;
}

//*****
// Set transform effect parameters
void EffectXYZ::SetTransforms(DrawListElement* draw_list_element)
{
	const RenderStateManager::RendererState& state = m_renderer->GetCurrentState();
	const m4x4* proj = draw_list_element->m_instance->GetProjectionTransform();
	if( !proj ) proj = &state.m_proj_transform;

	//Verify(m_effect->SetMatrix(m_World			,&draw_list_element->m_instance->GetInstanceToWorld().m));
	//Verify(m_effect->SetMatrix(m_View			,&state.m_view_transform.m));
	//Verify(m_effect->SetMatrix(m_Projection		,&proj->m));

	m4x4 inst_to_world = draw_list_element->m_instance->GetInstanceToWorld();
	m_renderer->GetD3DDevice()->SetTransform(D3DTS_WORLD		,&inst_to_world.m);
	m_renderer->GetD3DDevice()->SetTransform(D3DTS_VIEW			,&state.m_view_transform.m);
	m_renderer->GetD3DDevice()->SetTransform(D3DTS_PROJECTION	,&proj->m);
}

//*****
// Set the material parameters
void EffectXYZ::SetMaterialParameters(MaterialIndex mat_index)
{
	const D3DMATERIAL9* mat = m_renderer->GetMaterial(mat_index);
	IDirect3DTexture9*  tex = m_renderer->GetTexture (mat_index);

	Verify(m_effect->SetFloatArray	(m_MaterialAmbient			,(float*)&mat->Ambient	,4));
	Verify(m_effect->SetFloatArray	(m_MaterialDiffuse			,(float*)&mat->Diffuse	,4));
	Verify(m_effect->SetFloatArray	(m_MaterialSpecular			,(float*)&mat->Specular	,4));
	Verify(m_effect->SetFloatArray	(m_MaterialEmissive			,(float*)&mat->Emissive	,4));
	Verify(m_effect->SetFloat		(m_MaterialSpecularPower	,mat->Power));
	Verify(m_effect->SetTexture		(m_Texture					,tex));

	// Turn alpha on if the material has alpha
	if( mat_index.HasAlpha() )
	{
		Verify(m_effect->SetBool	(m_ZWriteEnable				,false));
		Verify(m_effect->SetBool	(m_AlphaBlendEnable			,true ));
	}
	else
	{
		Verify(m_effect->SetBool	(m_ZWriteEnable				,true ));
		Verify(m_effect->SetBool	(m_AlphaBlendEnable			,false));
	}
}

//*****
// Set parameters for an instance midway through a pass
bool EffectXYZ::MidPass(DrawListElement* draw_list_element)
{
	bool commit_needed = false;

	uint cullmode = m_renderer->GetCurrentRenderState(D3DRS_CULLMODE);
	//if( cullmode != m_last_cullmode )
	{
		Verify(m_effect->SetInt(m_CullMode, cullmode));
		m_last_cullmode = cullmode;
		commit_needed = true;
	}

	// If the instance has changed then we may need a different technique
	//if( !m_last_dle || m_last_dle->m_instance != draw_list_element->m_instance )
	{
		SetTransforms(draw_list_element);
		commit_needed = true;
	}

	// If the material index has changed we may need a different material or texture
	//if( !m_last_dle || m_last_dle->GetMaterialIndex() != draw_list_element->GetMaterialIndex() )
	{
		SetMaterialParameters(draw_list_element->GetMaterialIndex());
		commit_needed = true;
	}

	m_last_dle = draw_list_element;
	return commit_needed;
}

//*****
// Set parameters at the end of a pass
void EffectXYZ::PostPass()
{
	m_last_dle		= 0;
	m_last_cullmode	= D3DCULL_FORCE_DWORD;
}

//*****
// Return the render states used in this effect
void EffectXYZ::GetRenderStates(const D3DRENDERSTATETYPE*& rs, uint& num_rs) const
{
	static const D3DRENDERSTATETYPE RenderStates[] =
	{ D3DRS_AMBIENT, D3DRS_LIGHTING, D3DRS_CULLMODE, D3DRS_SPECULARENABLE, D3DRS_ZWRITEENABLE, D3DRS_ALPHABLENDENABLE };
	static const uint				NumRenderStates = sizeof(RenderStates) / sizeof(D3DRENDERSTATETYPE);
	rs		= RenderStates;
	num_rs	= 0;//NumRenderStates;
}
