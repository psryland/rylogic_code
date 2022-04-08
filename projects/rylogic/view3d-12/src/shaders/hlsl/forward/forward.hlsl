//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2010
//***********************************************
// Shader for forward rendering face data
#include "forward.hlsli"

// Main vertex shader
#ifdef PR_RDR_VSHADER_forward
PSIn main(VSIn In)
{
	return VSDefault(In);
}
#endif

// Main pixel shader
#ifdef PR_RDR_PSHADER_forward
PSOut main(PSIn In)
{
	return PSDefault(In);
}
#endif
