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
			pr::rdr::Allocator<ModelBuffer> m_alex_mdlbuf;
			pr::rdr::Allocator<Model>       m_alex_model;
			pr::rdr::Allocator<Nugget>      m_alex_nugget;
			D3DPtr<ID3D11Device>            m_device;

			ModelManager(const ModelManager&);
			ModelManager& operator =(const ModelManager&);

			// Delete methods that models/model buffers call to clean themselves up
			friend struct pr::rdr::ModelBuffer;
			friend struct pr::rdr::Model;
			void Delete(pr::rdr::ModelBuffer* model_buffer);
			void Delete(pr::rdr::Model* model);
			void Delete(pr::rdr::Nugget* nugget);

		public:
			// Models and ModelBuffers must be created by the ModelManager
			// because the model manager has the allocator
			ModelManager(pr::rdr::MemFuncs& mem, D3DPtr<ID3D11Device>& device);
			~ModelManager();

			// Create a model buffer in which one or more models can be created
			pr::rdr::ModelBufferPtr CreateModelBuffer(MdlSettings const& settings);

			// Create a model. A model buffer is also created for this model
			pr::rdr::ModelPtr CreateModel(MdlSettings const& settings);

			// Create a model within the provided model buffer.
			// The buffer must contain sufficient space for the model
			pr::rdr::ModelPtr CreateModel(MdlSettings const& settings, pr::rdr::ModelBufferPtr& model_buffer);
		};
	}
}

#endif

