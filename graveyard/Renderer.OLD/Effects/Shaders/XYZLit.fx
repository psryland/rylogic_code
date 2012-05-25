//******************************************************************
//
//	XYZLit
//
//******************************************************************

#include "Identifiers.fxh"
#include "VSGeneric.fxh"
#include "PSGeneric.fxh"

//******************************************************************
// Techniques
technique T_2_0
{
    pass P0
    {		
		VertexShader	= compile vs_2_0 VS20_Generic(	VertexDiffuse_None,
														VertexSpecular_None,
														TexDiffuse_None,
														EnviroMap_None,
														VSDiffOut_Zero );
														
		PixelShader		= compile ps_2_0 PS20_Generic(	PerPixelDiffuse_Amb_p_Directional,
														PerPixelSpecular_On,
														TexDiffuse_None,
														EnviroMap_None,
														PSOut_LtDiff_p_LtSpec );
    }
}
technique T_1_1
{
    pass P0
    {		
		VertexShader	= compile vs_1_1 VS11_Generic(	VertexDiffuse_Amb_p_Directional,
														VertexSpecular_On,
														TexDiffuse_None,
														EnviroMap_None,
														VSDiffOut_LtDiff_p_LtSpec);
														
		PixelShader		= compile ps_1_1 PS11_Generic(	TexDiffuse_None,
														EnviroMap_None,
														PSOut_InDiff );
    }
}
