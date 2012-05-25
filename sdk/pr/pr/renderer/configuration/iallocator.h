//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#pragma once
#ifndef PR_RDR_IALLOCATOR_H
#define PR_RDR_IALLOCATOR_H

#include "pr/renderer/types/forward.h"

namespace pr
{
	namespace rdr
	{
		struct IAllocator
		{
			//virtual void*            Alloc               (std::size_t size, std::size_t alignment) = 0;
			virtual ModelBuffer*     AllocModelBuffer    () = 0;
			virtual Model*           AllocModel          () = 0;
			virtual Effect*          AllocEffect         () = 0;
			virtual Texture*         AllocTexture        () = 0;
			virtual RenderNugget*    AllocRenderNugget   () = 0;

			//virtual void             Dealloc             (void*         p     ) = 0;
			virtual void             DeallocModelBuffer  (ModelBuffer*  buffer) = 0;
			virtual void             DeallocModel        (Model*        model ) = 0;
			virtual void             DeallocEffect       (Effect*       effect) = 0;
			virtual void             DeallocTexture      (Texture*      tex   ) = 0;
			virtual void             DeallocRenderNugget (RenderNugget* nugget) = 0;
		};

		//// A static pointer to the renderer allocator
		//struct AllocatorRef :IAllocator
		//{
		//	static IAllocator* alloc;

		//	void*             Alloc               (std::size_t size, std::size_t alignment) { return alloc->Alloc(size, alignment); }
		//	ModelBuffer*      AllocModelBuffer    ()                                        { return alloc->AllocModelBuffer(); }
		//	Model*            AllocModel          ()                                        { return alloc->AllocModel(); }
		//	Effect*           AllocEffect         ()                                        { return alloc->AllocEffect(); }
		//	Texture*          AllocTexture        ()                                        { return alloc->AllocTexture(); }
		//	RenderNugget*     AllocRenderNugget   ()                                        { return alloc->AllocRenderNugget(); }
		//	void              Dealloc             (void*         p     )                    { return alloc->Dealloc(p); }
		//	void              DeallocModelBuffer  (ModelBuffer*  buffer)                    { return alloc->DeallocModelBuffer(buffer); }
		//	void              DeallocModel        (Model*        model )                    { return alloc->DeallocModel(model); }
		//	void              DeallocEffect       (Effect*       effect)                    { return alloc->DeallocEffect(effect); }
		//	void              DeallocTexture      (Texture*      tex   )                    { return alloc->DeallocTexture(tex); }
		//	void              DeallocRenderNugget (RenderNugget* nugget)                    { return alloc->DeallocRenderNugget(nugget); }
		//};
	}
}

#endif
