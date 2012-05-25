//*****************************************************************************
//
// An effect file to reproduce the behaviour of the fixed function pipeline
//
//*****************************************************************************

float4x4	g_World					: World;
float4x4	g_View					: View;
float4x4	g_Projection			: Projection;

int			g_CullMode								= 1;
bool		g_SpecularEnable						= false;
bool		g_ZWriteEnable							= true;
bool		g_AlphaBlendEnable						= false;

float4		g_GlobalAmbient			: Ambient		= float4(0.5, 0.5, 0.5, 1.0);

float4		g_MaterialAmbient		: Ambient		= float4(1.0, 1.0, 1.0, 1.0);
float4		g_MaterialDiffuse		: Diffuse		= float4(1.0, 1.0, 1.0, 1.0);
float4		g_MaterialSpecular		: Specular		= float4(1.0, 1.0, 1.0, 1.0);
float4		g_MaterialEmissive		: Emissive		= float4(0.0, 0.0, 0.0, 0.0);
float		g_MaterialSpecularPower	: SpecularPower	= 10.0f;

bool		g_Lighting								= true;
bool		g_LightEnable							= true;
int			g_LightType								= 3;
float4		g_LightAmbient			: Ambient		= float4(0.1, 0.1, 0.1, 1.0);
float4		g_LightDiffuse			: Diffuse		= float4(1.0, 1.0, 1.0, 1.0);
float4		g_LightSpecular			: Specular		= float4(1.0, 1.0, 1.0, 1.0);
float3		g_LightPosition			: Position		= float3(0.0, 0.0, 0.0);
float3		g_LightDirection		: Direction		= float3(0.0, 0.0, 1.0);
float		g_LightRange							= 1000.0;
float		g_LightFalloff							= 0.0;
float		g_LightTheta							= 0.0;	// inner
float		g_LightPhi								= 0.0;	// outer
float		g_LightAttenuation0						= 1.0;
float		g_LightAttenuation1						= 0.0;
float		g_LightAttenuation2						= 0.0;

texture		g_Texture								= NULL;

//-----------------------------------
technique XYZLit
{
	pass p0 
	{
//PSR...		WorldTransform[0]	= <g_World>;
//PSR...		ViewTransform		= <g_View>;
//PSR...		ProjectionTransform	= <g_Projection>;

		Lighting			= <g_Lighting>;
		CullMode			= <g_CullMode>;
		SpecularEnable		= <g_SpecularEnable>;
		ZWriteEnable		= <g_ZWriteEnable>;
		AlphaBlendEnable	= <g_AlphaBlendEnable>;
		BlendOp				= Add;
		SrcBlend			= SrcAlpha;
		DestBlend			= DestAlpha;
		Ambient				= <g_GlobalAmbient>;
        
		LightEnable[0]		= <g_LightEnable>;
		LightType[0]		= <g_LightType>;
		LightAmbient[0]		= <g_LightAmbient>;
		LightDiffuse[0]		= <g_LightDiffuse>;
		LightSpecular[0]	= <g_LightSpecular>;
		LightPosition[0]	= <g_LightPosition>;
		LightDirection[0]	= <g_LightDirection>;
		LightRange[0]		= <g_LightRange>;
		LightFalloff[0]		= <g_LightFalloff>;
		LightTheta[0]		= <g_LightTheta>;
		LightPhi[0]			= <g_LightPhi>;
		LightAttenuation0[0]= <g_LightAttenuation0>;
		LightAttenuation1[0]= <g_LightAttenuation1>;
		LightAttenuation2[0]= <g_LightAttenuation2>;

		MaterialAmbient		= <g_MaterialAmbient>; 
		MaterialDiffuse		= <g_MaterialDiffuse>; 
		MaterialSpecular	= <g_MaterialSpecular>;
		MaterialEmissive	= <g_MaterialEmissive>;
		MaterialPower		= <g_MaterialSpecularPower>;

		// Just use the color
		Texture[0]			= <g_Texture>;
		ColorArg1[0]		= Diffuse;
		ColorArg2[0]		= Texture;
		AlphaArg1[0]		= Diffuse;
		AlphaArg2[0]		= Texture;
		ColorOp[0]			= Modulate;
		AlphaOp[0]			= Modulate;
       	ColorOp[1]			= Disable;
		AlphaOp[1]			= Disable;
	}
}
