//***************************************************************************
//
//	XYZLitTextured
//
//***************************************************************************

#ifndef RDR_EFFECT_XYZLitTextured_H
#define RDR_EFFECT_XYZLitTextured_H

#include "PR/Renderer/Effects/EffectBase.h"
#include "PR/Renderer/Effects/Common.h"
#include "PR/Renderer/Effects/StdLighting.h"
#include "PR/Renderer/Effects/StdTexturing.h"

namespace pr
{
	namespace rdr
	{
        namespace effect
		{
			class XYZLitTextured : public Base, public Common, public StdLighting, public StdTexturing
			{
			public:
				bool	MidPass(const Viewport& viewport, const DrawListElement& draw_list_element);
			protected:
				void	GetParameterHandles();
			};
		}//namespace effect
	}//namespace rdr
}//namespace pr

#endif//RDR_EFFECT_XYZLitTextured_H
