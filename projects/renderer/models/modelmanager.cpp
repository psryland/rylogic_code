//*********************************************
// Renderer Model Manager
//  Copyright © Rylogic Ltd 2007
//*********************************************

#include "renderer/utility/stdafx.h"
#include "pr/renderer/Models/ModelManager.h"

using namespace pr;
using namespace pr::rdr;
using namespace pr::rdr::model;
	
// Constructor
pr::rdr::ModelManager::ModelManager(IAllocator& allocator, D3DPtr<IDirect3DDevice9> d3d_device)
:pr::events::IRecv<pr::rdr::Evt_DeviceLost>(EDeviceResetPriority::ModelManager)
,pr::events::IRecv<pr::rdr::Evt_DeviceRestored>(EDeviceResetPriority::ModelManager)
,m_allocator(allocator)
,m_d3d_device(d3d_device)
{}
pr::rdr::ModelManager::~ModelManager()
{
	PR_ASSERT(PR_DBG_RDR,
		m_stats.m_model_count == 0 &&
		m_stats.m_model_buffer_count == 0 &&
		m_stats.m_render_nugget_count == 0,
		"Leaked models detected");
}
	
// Return a model buffer to the allocator
void pr::rdr::ModelManager::Delete(pr::rdr::ModelBuffer* model_buffer)
{
	PR_ASSERT(PR_DBG_RDR, model_buffer, "");
	m_allocator.DeallocModelBuffer(model_buffer);
	PR_EXPAND(PR_DBG_RDR, --m_stats.m_model_buffer_count);
}
	
// Return a model to the allocator
void pr::rdr::ModelManager::Delete(pr::rdr::Model* model)
{
	PR_ASSERT(PR_DBG_RDR, model, "");
	model->DeleteRenderNuggets();
	m_allocator.DeallocModel(model);
	PR_EXPAND(PR_DBG_RDR, --m_stats.m_model_count);
}
	
// Return a render nugget to the allocator
void pr::rdr::ModelManager::Delete(RenderNugget* nugget)
{
	PR_ASSERT(PR_DBG_RDR, nugget, "");
	m_allocator.DeallocRenderNugget(nugget);
	PR_EXPAND(PR_DBG_RDR, --m_stats.m_render_nugget_count);
}
	
// Allocate a render nugget
pr::rdr::RenderNugget* pr::rdr::ModelManager::NewRenderNugget()
{
	PR_EXPAND(PR_DBG_RDR, ++m_stats.m_render_nugget_count);
	return m_allocator.AllocRenderNugget();
}
	
// Create a model buffer in which multiple models can be created
pr::rdr::ModelBufferPtr pr::rdr::ModelManager::CreateModelBuffer(model::Settings const& settings)
{
	// Allocate a model buffer and create it.
	pr::rdr::ModelBufferPtr mb(m_allocator.AllocModelBuffer());
	
	// Create the vertex buffer
	D3DPtr<IDirect3DVertexBuffer9> Vbuffer;
	if (pr::Failed(m_d3d_device->CreateVertexBuffer((UINT)(settings.m_Vcount * vf::GetSize(settings.m_vertex_type)), (DWORD)settings.m_usage, 0, D3DPOOL_MANAGED, &Vbuffer.m_ptr, 0)))
	{
		std::string msg = pr::Fmt(
			"Failed to create model buffer vertex buffer\n"
			"Reason: %s\n"
			,pr::Reason().c_str());
		PR_ASSERT(PR_DBG_RDR, false, msg.c_str());
		throw RdrException(EResult::CreateModelBufferFailed, msg);
	}
	
	// Create the index buffer
	D3DPtr<IDirect3DIndexBuffer9> Ibuffer;
	if (pr::Failed(m_d3d_device->CreateIndexBuffer((DWORD)settings.m_Icount * sizeof(Index), (DWORD)settings.m_usage, D3DFMT_INDEX16, D3DPOOL_MANAGED, &Ibuffer.m_ptr, 0)))
	{
		std::string msg = pr::Fmt(
			"Failed to create model buffer index buffer\n"
			"Reason: %s\n"
			,pr::Reason().c_str());
		PR_ASSERT(PR_DBG_RDR, false, msg.c_str());
		throw RdrException(EResult::CreateModelBufferFailed, msg);
	}
	
	// Buffers created successfully, assign the members...
	mb->m_vertex_type = settings.m_vertex_type;
	mb->m_Vbuffer = Vbuffer;
	mb->m_Ibuffer = Ibuffer;
	mb->m_mdl_mgr = this;
	mb->m_Vrange.set(0, settings.m_Vcount);
	mb->m_Irange.set(0, settings.m_Icount);
	mb->m_Vused = RangeZero;
	mb->m_Iused = RangeZero;
	PR_EXPAND(PR_DBG_RDR, ++m_stats.m_model_buffer_count);
	return mb;
}
	
// Create a model.
// A model buffer is also created for this model
pr::rdr::ModelPtr pr::rdr::ModelManager::CreateModel(model::Settings const& settings)
{
	return CreateModel(settings, CreateModelBuffer(settings));
}

// Create a model within the provided model buffer
// The buffer must contain sufficient space for the model
pr::rdr::ModelPtr pr::rdr::ModelManager::CreateModel(model::Settings const& settings, pr::rdr::ModelBufferPtr model_buffer)
{
	PR_ASSERT(PR_DBG_RDR, model_buffer->IsCompatible(settings), "Incompatible model buffer provided");
	PR_ASSERT(PR_DBG_RDR, model_buffer->IsRoomFor(settings.m_Vcount, settings.m_Icount), "Insufficent room for a model of this size in this model buffer");
	
	pr::rdr::ModelPtr mdl(m_allocator.AllocModel());
	mdl->m_model_buffer = model_buffer;
	mdl->m_Vrange       = model_buffer->AllocateVertices(settings.m_Vcount);
	mdl->m_Irange       = model_buffer->AllocateIndices(settings.m_Icount);
	PR_EXPAND(PR_DBG_RDR, ++m_stats.m_model_count);
	return mdl;
}
	
