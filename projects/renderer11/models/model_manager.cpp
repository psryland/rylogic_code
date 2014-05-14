//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/models/model_manager.h"
#include "pr/renderer11/models/model_settings.h"
#include "pr/renderer11/shaders/input_layout.h"
#include "pr/renderer11/util/allocator.h"
#include "pr/renderer11/util/wrappers.h"
#include "pr/renderer11/util/util.h"

namespace pr
{
	namespace rdr
	{
		ModelManager::ModelManager(MemFuncs& mem, D3DPtr<ID3D11Device>& device)
			:m_alex_mdlbuf(Allocator<ModelBuffer>(mem))
			,m_alex_model (Allocator<Model>(mem))
			,m_alex_nugget(Allocator<Nugget>(mem))
			,m_device     (device)
		{
			PR_ASSERT(PR_DBG_RDR, m_device != 0, "Null directx device");

			// Create stock models
			CreateStockModels();
		}
		ModelManager::~ModelManager()
		{}

		// Create a model buffer in which one or more models can be created
		ModelBufferPtr ModelManager::CreateModelBuffer(MdlSettings const& settings)
		{
			// Create a new model buffer
			ModelBufferPtr mb(m_alex_mdlbuf.New());
			mb->m_mdl_mgr = this;
			{// Create a vertex buffer
				SubResourceData init(settings.m_vb.Data, 0, UINT(settings.m_vb.SizeInBytes()));
				pr::Throw(m_device->CreateBuffer(&settings.m_vb, settings.m_vb.Data != 0 ? &init : 0, &mb->m_vb.m_ptr));
				mb->m_vb.m_range.set(0, settings.m_vb.ElemCount);
				mb->m_vb.m_used.set(0, 0);
				mb->m_vb.m_stride = settings.m_vb.StructureByteStride;
				PR_EXPAND(PR_DBG_RDR, NameResource(mb->m_vb, pr::FmtS("model vbuffer <V:%d,I:%d>", settings.m_vb.ElemCount, settings.m_ib.ElemCount)));
			}
			{// Create an index buffer
				SubResourceData init(settings.m_ib.Data, 0, UINT(settings.m_ib.SizeInBytes()));
				pr::Throw(m_device->CreateBuffer(&settings.m_ib, settings.m_ib.Data != 0 ? &init : 0, &mb->m_ib.m_ptr));
				mb->m_ib.m_range.set(0, settings.m_ib.ElemCount);
				mb->m_ib.m_used.set(0, 0);
				mb->m_ib.m_format = settings.m_ib.Format;
				PR_EXPAND(PR_DBG_RDR, NameResource(mb->m_ib, pr::FmtS("model ibuffer <V:%d,I:%d>", settings.m_vb.ElemCount, settings.m_ib.ElemCount)));
			}
			return mb;
		}

		// Create a model. A model buffer is also created for this model
		ModelPtr ModelManager::CreateModel(MdlSettings const& settings)
		{
			ModelBufferPtr mb = CreateModelBuffer(settings);
			return CreateModel(settings, mb);
		}

		// Create a model within the provided model buffer
		// The buffer must contain sufficient space for the model
		ModelPtr ModelManager::CreateModel(MdlSettings const& settings, ModelBufferPtr& model_buffer)
		{
			PR_ASSERT(PR_DBG_RDR, model_buffer->IsCompatible(settings), "Incompatible model buffer provided");
			PR_ASSERT(PR_DBG_RDR, model_buffer->IsRoomFor(settings.m_vb.ElemCount, settings.m_ib.ElemCount), "Insufficent room for a model of this size in this model buffer");
			return ModelPtr(m_alex_model.New(settings, model_buffer));
		}

		// Return a model buffer to the allocator
		void ModelManager::Delete(ModelBuffer* model_buffer)
		{
			if (!model_buffer) return;
			m_alex_mdlbuf.Delete(model_buffer);
		}

		// Return a model to the allocator
		void ModelManager::Delete(Model* model)
		{
			if (!model) return;
			m_alex_model.Delete(model);
		}

		// Return a render nugget to the allocator
		void ModelManager::Delete(Nugget* nugget)
		{
			if (!nugget) return;
			m_alex_nugget.Delete(nugget);
		}

		// Create stock models
		void ModelManager::CreateStockModels()
		{
			{// Unit quad in Z = 0 plane
				Vert verts[4] =
				{
					{pr::v4::make(-0.5f,-0.5f, 0, 1), pr::ColourWhite, pr::v4ZAxis, pr::v2::make(0.0000f,0.9999f)},
					{pr::v4::make( 0.5f,-0.5f, 0, 1), pr::ColourWhite, pr::v4ZAxis, pr::v2::make(0.9999f,0.9999f)},
					{pr::v4::make( 0.5f, 0.5f, 0, 1), pr::ColourWhite, pr::v4ZAxis, pr::v2::make(0.9999f,0.0000f)},
					{pr::v4::make(-0.5f, 0.5f, 0, 1), pr::ColourWhite, pr::v4ZAxis, pr::v2::make(0.0000f,0.0000f)},
				};
				pr::uint16 idxs[] =
				{
					0, 1, 2, 0, 2, 3
				};
				auto bbox = pr::BBox::make(pr::v4Origin, pr::v4::make(1,1,0,0));

				MdlSettings s(verts, idxs, bbox, "unit quad");
				m_unit_quad = CreateModel(s);

				NuggetProps ddata(EPrim::TriList, Vert::GeomMask);
				m_unit_quad->CreateNugget(ddata);
			}
		}
	}
}