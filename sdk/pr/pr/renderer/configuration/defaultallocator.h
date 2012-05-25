//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#pragma once
#ifndef PR_RDR_DEFAULT_ALLOCATOR_H
#define PR_RDR_DEFAULT_ALLOCATOR_H

#include "pr/common/objectpool.h"
#include "pr/renderer/types/forward.h"
#include "pr/renderer/configuration/iallocator.h"
#include "pr/renderer/models/modelbuffer.h"
#include "pr/renderer/models/model.h"
#include "pr/renderer/models/rendernugget.h"
#include "pr/renderer/materials/effects/effect.h"
#include "pr/renderer/materials/textures/texture.h"
#include "pr/renderer/viewport/drawlistelement.h"

namespace pr
{
	namespace rdr
	{
		// Allocator. This allocator dynamically allocates memory
		struct Allocator :IAllocator
		{
			typedef pr::ObjectPool<ModelBuffer, 100>   TModelBufferPool;
			typedef pr::ObjectPool<Model, 100>         TModelPool;
			typedef pr::ObjectPool<Effect, 8>          TEffectPool;
			typedef pr::ObjectPool<Texture, 100>       TTexturePool;
			typedef pr::ObjectPool<RenderNugget, 1000> TRenderNuggetPool;
			
			TModelBufferPool  m_model_buffer_pool;
			TModelPool        m_model_pool;
			TEffectPool       m_effect_pool;
			TTexturePool      m_texture_pool;
			TRenderNuggetPool m_render_nugget_pool;
			
			//void*             Alloc               (std::size_t size, std::size_t alignment) { return _aligned_malloc(size, alignment); }
			ModelBuffer*      AllocModelBuffer    ()                                        { return m_model_buffer_pool  .Get(); }
			Model*            AllocModel          ()                                        { return m_model_pool         .Get(); }
			Effect*           AllocEffect         ()                                        { return m_effect_pool        .Get(); }
			Texture*          AllocTexture        ()                                        { return m_texture_pool       .Get(); }
			RenderNugget*     AllocRenderNugget   ()                                        { return m_render_nugget_pool .Get(); }
			
			//void              Dealloc             (void*         p     )                    { _aligned_free(p); }
			void              DeallocModelBuffer  (ModelBuffer*  buffer)                    { m_model_buffer_pool          .Return(buffer); }
			void              DeallocModel        (Model*        model )                    { m_model_pool                 .Return(model ); }
			void              DeallocEffect       (Effect*       effect)                    { m_effect_pool                .Return(effect); }
			void              DeallocTexture      (Texture*      tex   )                    { m_texture_pool               .Return(tex   ); }
			void              DeallocRenderNugget (RenderNugget* nugget)                    { m_render_nugget_pool         .Return(nugget); }
		};

	}
}

#endif
