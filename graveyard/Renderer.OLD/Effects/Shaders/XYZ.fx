//******************************************************************
//
//	XYZ
//
//******************************************************************

#include "Identifiers.fxh"
#include "VSGeneric.fxh"
#include "PSGeneric.fxh"

//******************************************************************
// Techniques
technique T_1_1
{
    pass P0
    {		
		VertexShader	= compile vs_1_1 VS11_Generic(	VertexDiffuse_None,
														VertexSpecular_None,
														TexCoordsDiffuse_None,
														TexCoordsEnviroMap_None,
														VSDiffOut_Zero);
														
		PixelShader		= compile ps_1_1 PS11_Generic(	TexDiffuse_None,
														EnviroMap_None,
														PSOut_Amb);
    }
}