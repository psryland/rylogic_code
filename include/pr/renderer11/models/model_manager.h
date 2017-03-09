//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/util/allocator.h"
#include "pr/renderer11/models/model_buffer.h"
#include "pr/renderer11/models/model.h"
#include "pr/renderer11/models/nugget.h"

namespace pr
{
	namespace rdr
	{
		class ModelManager
		{
			Allocator<ModelBuffer>          m_alex_mdlbuf;
			Allocator<Model>                m_alex_model;
			Allocator<Nugget>               m_alex_nugget;
			AllocationsTracker<ModelBuffer> m_dbg_mem_mdlbuf;
			AllocationsTracker<Model>       m_dbg_mem_mdl;
			AllocationsTracker<Nugget>      m_dbg_mem_nugget;
			D3DPtr<ID3D11Device>            m_device;
			std::recursive_mutex            m_mutex;

			// Delete methods that models/model buffers call to clean themselves up
			friend struct ModelBuffer;
			friend struct Model;
			friend struct Nugget;
			void Delete(ModelBuffer* model_buffer);
			void Delete(Model* model);
			void Delete(Nugget* nugget);

			// Create stock models
			void CreateStockModels();

		public:

			// Models and ModelBuffers must be created by the ModelManager
			// because the model manager has the allocator
			ModelManager(MemFuncs& mem, D3DPtr<ID3D11Device>& device);
			ModelManager(ModelManager const&) = delete;
			ModelManager& operator =(ModelManager const&) = delete;

			// Create a model buffer in which one or more models can be created
			ModelBufferPtr CreateModelBuffer(MdlSettings const& settings);

			// Create a model. A model buffer is also created for this model
			ModelPtr CreateModel(MdlSettings const& settings);

			// Create a model within the provided model buffer.
			// The buffer must contain sufficient space for the model
			ModelPtr CreateModel(MdlSettings const& settings, ModelBufferPtr& model_buffer);

			// Create a nugget using our allocator
			Nugget* CreateNugget(NuggetProps props, ModelBuffer* model_buffer, Model* model);

			// Stock models
			ModelPtr m_unit_quad;
		};
	}
}
