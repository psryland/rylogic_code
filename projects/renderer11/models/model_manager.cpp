//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/models/model_manager.h"
#include "pr/renderer11/models/model_settings.h"
#include "pr/renderer11/util/allocator.h"
#include "pr/renderer11/util/wrappers.h"

using namespace pr::rdr;

pr::rdr::ModelManager::ModelManager(pr::rdr::MemFuncs& mem, D3DPtr<ID3D11Device>& device)
:m_alex_mdlbuf(Allocator<ModelBuffer>(mem))
,m_alex_model (Allocator<Model>(mem))
,m_alex_nugget(Allocator<Nugget>(mem))
,m_device     (device)
{
	PR_ASSERT(PR_DBG_RDR, m_device != 0, "Null d3d device");
}
ModelManager::~ModelManager()
{}

// Create a model buffer in which one or more models can be created
pr::rdr::ModelBufferPtr pr::rdr::ModelManager::CreateModelBuffer(MdlSettings const& settings)
{
	// Create a new model buffer
	pr::rdr::ModelBufferPtr mb(m_alex_mdlbuf.New());
	mb->m_mdl_mgr = this;
	{// Create a vertex buffer
		SubResourceData init(settings.m_vb.Data, 0, UINT(settings.m_vb.SizeInBytes()));
		pr::Throw(m_device->CreateBuffer(&settings.m_vb, &init, &mb->m_vb.m_ptr));
		mb->m_vb.m_range.set(0, settings.m_vb.ElemCount);
		mb->m_vb.m_used.set(0, 0);
		mb->m_vb.m_stride = settings.m_vb.StructureByteStride;
	}
	{// Create an index buffer
		SubResourceData init(settings.m_ib.Data, 0, UINT(settings.m_ib.SizeInBytes()));
		pr::Throw(m_device->CreateBuffer(&settings.m_ib, &init, &mb->m_ib.m_ptr));
		mb->m_ib.m_range.set(0, settings.m_ib.ElemCount);
		mb->m_ib.m_used.set(0, 0);
		mb->m_ib.m_format = settings.m_ib.Format;
	}
	return mb;
}

// Create a model. A model buffer is also created for this model
pr::rdr::ModelPtr pr::rdr::ModelManager::CreateModel(MdlSettings const& settings)
{
	ModelBufferPtr mb = CreateModelBuffer(settings);
	return CreateModel(settings, mb);
}

// Create a model within the provided model buffer
// The buffer must contain sufficient space for the model
pr::rdr::ModelPtr pr::rdr::ModelManager::CreateModel(MdlSettings const& settings, pr::rdr::ModelBufferPtr& model_buffer)
{
	PR_ASSERT(PR_DBG_RDR, model_buffer->IsCompatible(settings), "Incompatible model buffer provided");
	PR_ASSERT(PR_DBG_RDR, model_buffer->IsRoomFor(settings.m_vb.ElemCount, settings.m_ib.ElemCount), "Insufficent room for a model of this size in this model buffer");
	
	pr::rdr::ModelPtr mdl(m_alex_model.New());
	mdl->m_model_buffer = model_buffer;
	mdl->m_vrange       = model_buffer->ReserveVerts(settings.m_vb.ElemCount);
	mdl->m_irange       = model_buffer->ReserveIndices(settings.m_ib.ElemCount);
	return mdl;
}

// Return a model buffer to the allocator
void pr::rdr::ModelManager::Delete(pr::rdr::ModelBuffer* model_buffer)
{
	if (!model_buffer) return;
	m_alex_mdlbuf.Delete(model_buffer);
}

// Return a model to the allocator
void pr::rdr::ModelManager::Delete(pr::rdr::Model* model)
{
	if (!model) return;
	model->DeleteNuggets();
	m_alex_model.Delete(model);
}

// Return a render nugget to the allocator
void pr::rdr::ModelManager::Delete(pr::rdr::Nugget* nugget)
{
	if (!nugget) return;
	m_alex_nugget.Delete(nugget);
}
