//******************************************************************
//
//	XYZPVC
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
														TexDiffuse_None,
														EnviroMap_None,
														VSDiffOut_InDiff );
														
		PixelShader		= compile ps_1_1 PS11_Generic(	TexDiffuse_None,
														EnviroMap_None,
														PSOut_Amb_p_InDiff);
    }
}
