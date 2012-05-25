//*****************************************************************************
//
// An effect file to reproduce the behaviour of the fixed function pipeline
//
//*****************************************************************************

float4x4	g_World					: World;
float4x4	g_View					: View;
float4x4	g_Projection			: Projection;

int			g_CullMode								= 1;
bool		g_ZWriteEnable							= true;
bool		g_AlphaBlendEnable						= false;

bool		g_Lighting								= false;
float4		g_GlobalAmbient			: Ambient		= float4(0.5, 0.5, 0.5, 1.0);

float4		g_MaterialAmbient		: Ambient		= float4(1.0, 1.0, 1.0, 1.0);
float4		g_MaterialDiffuse		: Diffuse		= float4(1.0, 1.0, 1.0, 1.0);
float4		g_MaterialSpecular		: Specular		= float4(1.0, 1.0, 1.0, 1.0);
float4		g_MaterialEmissive		: Emissive		= float4(0.0, 0.0, 0.0, 0.0);
float		g_MaterialSpecularPower	: SpecularPower	= 10.0f;

texture		g_Texture								= NULL;

//-----------------------------------
technique XYZ
{
	pass p0 
	{
		VertexShaderConstant4[0]	= <g_World>;
//PSR...		WorldTransform[0]	= <g_World>;
//PSR...		ViewTransform		= <g_View>;
//PSR...		ProjectionTransform	= <g_Projection>;

		Lighting			= <g_Lighting>;
		CullMode			= <g_CullMode>;
		SpecularEnable		= false;
		ZWriteEnable		= <g_ZWriteEnable>;
		AlphaBlendEnable	= <g_AlphaBlendEnable>;
		BlendOp				= Add;
		SrcBlend			= SrcAlpha;
		DestBlend			= DestAlpha;
		Ambient				= <g_GlobalAmbient>;
        
		LightEnable[0]		= false;

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