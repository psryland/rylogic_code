//*********************************************
// Renderer Model Manager
//  Copyright © Rylogic Ltd 2007
//*********************************************

#pragma once
#ifndef PR_RDR_MODEL_MANAGER_H
#define PR_RDR_MODEL_MANAGER_H

#include "pr/renderer/types/forward.h"
#include "pr/renderer/configuration/projectconfiguration.h"
#include PR_RDR_CONFIGURATION_TYPES
#include "pr/renderer/configuration/iallocator.h"
#include "pr/renderer/models/types.h"
#include "pr/renderer/models/rendernugget.h"
#include "pr/renderer/models/modelbuffer.h"
#include "pr/renderer/models/model.h"

namespace pr
{
	namespace rdr
	{
		class ModelManager
			:public pr::events::IRecv<pr::rdr::Evt_DeviceLost>
			,public pr::events::IRecv<pr::rdr::Evt_DeviceRestored>
		{
			friend struct pr::rdr::ModelBuffer;
			friend struct pr::rdr::Model;
			struct Statistics
			{
				int m_model_count;
				int m_model_buffer_count;
				int m_render_nugget_count;
				Statistics() :m_model_count(0) ,m_model_buffer_count(0) ,m_render_nugget_count(0) {}
			};
			
			IAllocator&              m_allocator;
			D3DPtr<IDirect3DDevice9> m_d3d_device;
			Statistics               m_stats;
			
			ModelManager(const ModelManager&);
			ModelManager& operator =(const ModelManager&);
			
			// Release our reference to the d3d device. The models and model buffer should not
			// need to be released because they should all belong to D3DPOOL_MANAGED.
			void OnEvent(pr::rdr::Evt_DeviceLost const&)       { m_d3d_device = 0; }
			void OnEvent(pr::rdr::Evt_DeviceRestored const& e) { m_d3d_device = e.m_d3d_device; }
			
			void Delete(pr::rdr::ModelBuffer* model_buffer);
			void Delete(pr::rdr::Model* model);
			void Delete(pr::rdr::RenderNugget* nugget);
			pr::rdr::RenderNugget* NewRenderNugget();
			
		public:
			ModelManager(IAllocator& allocator, D3DPtr<IDirect3DDevice9> d3d_device);
			~ModelManager();
			
			// Create a model buffer in which multiple models can be created
			pr::rdr::ModelBufferPtr CreateModelBuffer(model::Settings const& settings);
			
			// Create a model.
			// A model buffer is also created for this model
			pr::rdr::ModelPtr CreateModel(model::Settings const& settings);
			
			// Create a model within the provided model buffer
			// The buffer must contain sufficient space for the model
			pr::rdr::ModelPtr CreateModel(model::Settings const& settings, pr::rdr::ModelBufferPtr model_buffer);
		};
	}
}

#endif
			
			
			//// Model creation
			//EResult::Type CreateModelBuffer(const model::Settings& settings, ModelBuffer*& model_buffer_out);
			//EResult::Type CreateModel(const model::Settings& settings, Model*& model_out, RdrId model_id, ModelBuffer* model_buffer);
			//EResult::Type CreateModel(const model::Settings& settings, Model*& model_out, RdrId model_id                           );
			//EResult::Type CreateModel(const model::Settings& settings, Model*& model_out,                 ModelBuffer* model_buffer) {                   return CreateModel(settings, model_out, model::DefaultModelId, model_buffer); }
			//EResult::Type CreateModel(const model::Settings& settings, Model*& model_out                                           ) {                   return CreateModel(settings, model_out, model::DefaultModelId              ); }
			//EResult::Type CreateModel(const model::Settings& settings,                    RdrId model_id, ModelBuffer* model_buffer) { Model* model_out; return CreateModel(settings, model_out,              model_id, model_buffer); }
			//EResult::Type CreateModel(const model::Settings& settings,                    RdrId model_id                           ) { Model* model_out; return CreateModel(settings, model_out,              model_id              ); }
			//EResult::Type CreateModel(const model::Settings& settings,                                    ModelBuffer* model_buffer) { Model* model_out; return CreateModel(settings, model_out, model::DefaultModelId, model_buffer); }
			//EResult::Type CreateModel(const model::Settings& settings                                                              ) { Model* model_out; return CreateModel(settings, model_out, model::DefaultModelId              ); }
			//void          DeleteModelBuffer(ModelBuffer* buffer);
			//void          DeleteModel(Model* model);

			//// Allocation of render nuggets
			//RenderNugget* NewRenderNugget()            { return m_allocator.AllocRenderNugget(); }
			//void          Delete(RenderNugget* nugget) { return m_allocator.DeallocRenderNugget(nugget); }

			//// Get a pointer to a model or find a model. 'Find' returns 0 if the model is not found
			//Model* FindModel(RdrId model_id)
			//{
			//	TModelLookup::iterator i = m_model_lookup.find(model_id);
			//	return (i == m_model_lookup.end()) ? (0) : (i->second);
			//}
			//Model* GetModel (RdrId model_id)
			//{
			//	PR_ASSERT(1, m_model_lookup.find(model_id) != m_model_lookup.end(), "Model not found");
			//	return m_model_lookup.find(model_id)->second;
			//}
