//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#if SHADER_BUILD == 0
#pragma once
#endif
#ifndef PR_RDR_MATERIAL_SHADERS_CONSTANT_BUFFER_H
#define PR_RDR_MATERIAL_SHADERS_CONSTANT_BUFFER_H

#if SHADER_BUILD == 0
namespace pr
{
	namespace rdr
	{
		namespace shader
		{
			#endif
			
			// Basic constant buffer
			#if SHADER_BUILD == 1
			cbuffer cb0
			{
				matrix m_o2s : packoffset(c0);
			};
			#else
			struct cb0
			{
				
				pr::m4x4 m_o2s; // object to screen;
			};
			#endif
			
			#if SHADER_BUILD == 0
		}
	}
}
#endif
#endif
