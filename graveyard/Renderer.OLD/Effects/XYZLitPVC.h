//***************************************************************************
//
//	XYZLitPVC
//
//***************************************************************************

#ifndef RDR_EFFECT_XYZLitPVC_H
#define RDR_EFFECT_XYZLitPVC_H

#include "PR/Renderer/Effects/EffectBase.h"
#include "PR/Renderer/Effects/Common.h"
#include "PR/Renderer/Effects/StdLighting.h"

namespace pr
{
	namespace rdr
	{
        namespace effect
		{
			class XYZLitPVC : public Base, public Common, public StdLighting
			{
			public:
				bool	MidPass(const Viewport& viewport, const DrawListElement& draw_list_element);
			protected:
				void	GetParameterHandles();
			};
		}//namespace effect
	}//namespace rdr
}//namespace pr

#endif//RDR_EFFECT_XYZLitPVC_H
