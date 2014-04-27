//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_MODELS_MODEL_MANAGER_H
#define PR_RDR_MODELS_MODEL_MANAGER_H

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
			Allocator<ModelBuffer> m_alex_mdlbuf;
			Allocator<Model>       m_alex_model;
			Allocator<Nugget>      m_alex_nugget;
			D3DPtr<ID3D11Device>   m_device;

			ModelManager(const ModelManager&);
			ModelManager& operator =(const ModelManager&);

			// Delete methods that models/model buffers call to clean themselves up
			friend struct ModelBuffer;
			friend struct Model;
			void Delete(ModelBuffer* model_buffer);
			void Delete(Model* model);
			void Delete(Nugget* nugget);

		public:
			// Models and ModelBuffers must be created by the ModelManager
			// because the model manager has the allocator
			ModelManager(MemFuncs& mem, D3DPtr<ID3D11Device>& device);
			~ModelManager();

			// Create a model buffer in which one or more models can be created
			ModelBufferPtr CreateModelBuffer(MdlSettings const& settings);

			// Create a model. A model buffer is also created for this model
			ModelPtr CreateModel(MdlSettings const& settings);

			// Create a model within the provided model buffer.
			// The buffer must contain sufficient space for the model
			ModelPtr CreateModel(MdlSettings const& settings, ModelBufferPtr& model_buffer);
		};
	}
}

#endif
