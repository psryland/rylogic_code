//***************************************************************************
//
//	Forward declarations of renderer types
//
//***************************************************************************
#ifndef PR_RENDERER_FORWARD_H
#define PR_RENDERER_FORWARD_H

#include "PR/Common/StdString.h"
#include "PR/Common/StdList.h"
#include "PR/Renderer/Effects/Forward.h"

namespace pr
{
	class  Renderer;
	struct RendererSettings;

	namespace rdr
	{
		typedef std::list<std::string> TPathList;

		class  Viewport;
		struct Attribute;
		struct RenderNugget;
		struct DrawListElement;
		struct Light;
		struct Material;
		class  Texture;
		struct RenderState;
		struct RendererState;
		class  RenderableBase;
		struct InstanceBase;
		struct Instance;
		struct RTMaterialInstance;
		struct RendererTextureChain;
		struct RendererEffectChain;
		struct RendererViewportChain;
		struct DeviceConfig;
		struct DisplayModeIter;
		class  Adapter;
		class  System;
	}//namespace rdr
}//namespace pr

#endif//PR_RENDERER_FORWARD_H
