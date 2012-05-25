//*******************************************************************
//
//	Default Effect
//
//*******************************************************************
//	This file contains the fragments used by the default effect file
//*****
// Global variables
VertexShader	g_VertexShader		= NULL;									// Set by the default effect wrapper
PixelShader		g_PixelShader		= NULL;									// Set by the default effect wrapper
float4x4		g_WorldViewProj		: WorldViewProjection;
float4x4		g_World				: World;

float4			g_LightAmbient		: Ambient = float4(0.4, 0.4, 0.4, 1.0);
float4			g_LightDiffuse		: Diffuse = float4(0.5, 0.5, 0.5, 1.0);
float3			g_LightPosition		: Position	= float3(0.0, 0.0, 0.0);
float3			g_LightDirection	: Direction = float3(0.0, 0.0, -1.0);

float4			g_MaterialAmbient	: Ambient = float4(1.0, 1.0, 1.0, 1.0);
float4			g_MaterialDiffuse	: Diffuse = float4(1.0, 1.0, 1.0, 1.0);

texture			g_Texture			= NULL;

//*****
// Texture sampler
sampler TextureSampler = 
sampler_state
{
    Texture = <g_Texture>;    
    MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};


//**********************************************
// Vertex Shader Fragments
//**********************************************
//*****
// Project an object position only
void ProjectionP(	float3 position_obj		: Position,
				out float4 position_proj	: Position,
				out float3 position_wld		: r_PosWorld
				)
{
	position_proj	= mul(float4(position_obj, 1.0), g_WorldViewProj);
	position_wld	= mul(position_obj, (float3x3)g_World);
}
vertexfragment Frag_ProjectionP = compile_fragment vs_1_1 ProjectionP();

//*****
// Project an object normal
void ProjectionN(	float3 normal_obj		: Normal,
				out float3 normal_wld		: r_NormalWorld
				)
{
    normal_wld		= mul(normal_obj,   (float3x3)g_World);
}
vertexfragment Frag_ProjectionN = compile_fragment vs_1_1 ProjectionN();

//*****
// Project an object colour
void ProjectionC(	float4 colour_in		: Color0,
				out float4 colour_out		: Color0
				)
{
    colour_out		= colour_in;
}
vertexfragment Frag_ProjectionC = compile_fragment vs_1_1 ProjectionC();

//*****
// Project an object texture
void ProjectionT(	float2 tex_coord_in		: TexCoord0,
				out float2 tex_coord_out	: TexCoord0
				)
{
	tex_coord_out	= tex_coord_in;
}
vertexfragment Frag_ProjectionT = compile_fragment vs_1_1 ProjectionT();


//*****
// Lighting start
float4 LightStart(out float4 Tcolour_out : r_Color0) : Color0
{
    Tcolour_out = float4(1,1,1,1);
    return Tcolour_out;
}
vertexfragment Frag_LightStart = compile_fragment vs_1_1 LightStart();

//*****
// Light an object using ambient and vertex colour
float4 Diffuse(float4 vertex_colour	: Color0, float4 Tcolour_in : r_Color0, out float4 Tcolour_out : r_Color0) : Color0
{
   Tcolour_out = Tcolour_in * vertex_colour * g_LightDiffuse * g_MaterialDiffuse;
   return Tcolour_out;
}
vertexfragment Frag_Diffuse = compile_fragment vs_1_1 Diffuse();

//*****
// Light an object using a point light source
float4 Point(float3 position_wld : r_PosWorld, float3 normal_wld : r_NormalWorld, float4 Tcolour_in : r_Color0, out float4 Tcolour_out : r_Color0) : Color0
{
	Tcolour_out = Tcolour_in * saturate(dot(normalize(g_LightPosition - position_wld), normal_wld));
	return Tcolour_out;
}
vertexfragment Frag_Point = compile_fragment vs_1_1 Point();

//*****
// Light an object using a spot light source
float4 Spot(float3 position_wld : r_PosWorld, float3 normal_wld : r_NormalWorld, float4 Tcolour_in : r_Color0, out float4 Tcolour_out : r_Color0) : Color0
{
	Tcolour_out = Tcolour_in * saturate(dot(normalize(g_LightPosition - position_wld), normal_wld));
	return Tcolour_out;
}
vertexfragment Frag_Spot = compile_fragment vs_1_1 Spot();

//*****
// Light an object using a directional light source
float4 Directional(float3 normal_wld : r_NormalWorld, float4 Tcolour_in : r_Color0, out float4 Tcolour_out : r_Color0) : Color0
{
    Tcolour_out = Tcolour_in * saturate(dot(g_LightDirection, normal_wld));
    return Tcolour_out;
}
vertexfragment Frag_Directional = compile_fragment vs_1_1 Directional();

//*****
// Ambient lighting
float4 Ambient(float4 Tcolour_in : r_Color0, out float4 Tcolour_out : r_Color0) : Color0
{
    Tcolour_out = saturate(Tcolour_in + g_LightAmbient * g_MaterialAmbient);
    return Tcolour_out;
}
vertexfragment Frag_Ambient = compile_fragment vs_1_1 Ambient();


//**********************************************
// Pixel Shader Fragments
//**********************************************
//PSR...float4 Colour(float4 colour_in : Color0, out float4 Tcolour_out : r_Color0) : Color0
//PSR...{
//PSR...	Tcolour_out = colour_in;
//PSR...	return Tcolour_out;
//PSR...}
//PSR...pixelfragment Frag_Colour = compile_fragment ps_1_1 Colour();
//PSR...
//PSR...float4 Texture(float4 colour_in : Color0, float4 Tcolour_in : r_Color0, out float4 Tcolour_out : r_Color0) : Color0
//PSR...{
//PSR...	return colour_in;
//PSR...}
//PSR...pixelfragment Frag_Colour = compile_fragment ps_1_1 Colour();


//**********************************************
// Techniques
//**********************************************
technique RenderScene
{
	pass P0
	{
       	ColorOp[0]		= SelectArg1;
        AlphaOp[0]  	= SelectArg1;
        ColorArg1[0]	= Diffuse;
        ColorArg2[0]	= Texture;
        AlphaArg1[0]	= Diffuse;
        AlphaArg2[0]	= Texture;
		
//		VertexShader	= compile vs_1_1 _VS();
//		PixelShader		= compile ps_1_1 _PS();
		VertexShader	= <g_VertexShader>;
		PixelShader		= <g_PixelShader>;
	}
}
