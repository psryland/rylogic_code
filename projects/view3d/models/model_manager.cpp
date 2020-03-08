//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "pr/view3d/forward.h"
#include "pr/view3d/models/model_manager.h"
#include "pr/view3d/models/model_settings.h"
#include "pr/view3d/shaders/input_layout.h"
#include "pr/view3d/render/renderer.h"
#include "pr/view3d/util/wrappers.h"
#include "pr/view3d/util/util.h"

namespace pr::rdr
{
	ModelManager::ModelManager(Renderer& rdr)
		:m_dbg_mem_mdlbuf()
		,m_dbg_mem_mdl()
		,m_dbg_mem_nugget()
		,m_rdr(rdr)
		,m_unit_quad()
		,m_bbox_model()
	{
		CreateStockModels();
	}

	// Create a model buffer in which one or more models can be created
	ModelBufferPtr ModelManager::CreateModelBuffer(MdlSettings const& settings)
	{
		if (settings.m_vb.ElemCount == 0)
			throw std::runtime_error("Attempt to create 0-length model vertex buffer");
		if (settings.m_ib.ElemCount == 0)
			throw std::runtime_error("Attempt to create 0-length model index buffer");
		if (settings.m_ib.Format != DXGI_FORMAT_R16_UINT && settings.m_ib.Format != DXGI_FORMAT_R32_UINT)
			throw std::runtime_error(Fmt("Index buffer format %d is not supported", settings.m_ib.Format));

		Renderer::Lock lock(m_rdr);
		auto& device = *lock.D3DDevice();

		// Create a new model buffer
		ModelBufferPtr mb(rdr::New<ModelBuffer>(), true);
		assert(m_dbg_mem_mdlbuf.add(mb.m_ptr));
		mb->m_mdl_mgr = this;
		{// Create a vertex buffer
			SubResourceData init(settings.m_vb.Data, 0, UINT(settings.m_vb.SizeInBytes()));
			Throw(device.CreateBuffer(&settings.m_vb, settings.m_vb.Data != 0 ? &init : 0, &mb->m_vb.m_ptr));
			mb->m_vb.m_range.set(0, settings.m_vb.ElemCount);
			mb->m_vb.m_used.set(0, 0);
			mb->m_vb.m_stride = settings.m_vb.StructureByteStride;
			PR_EXPAND(PR_DBG_RDR, NameResource(mb->m_vb.get(), FmtS("model VBuffer <V:%d,I:%d>", settings.m_vb.ElemCount, settings.m_ib.ElemCount)));
		}
		{// Create an index buffer
			SubResourceData init(settings.m_ib.Data, 0, UINT(settings.m_ib.SizeInBytes()));
			Throw(device.CreateBuffer(&settings.m_ib, settings.m_ib.Data != 0 ? &init : 0, &mb->m_ib.m_ptr));
			mb->m_ib.m_range.set(0, settings.m_ib.ElemCount);
			mb->m_ib.m_used.set(0, 0);
			mb->m_ib.m_format = settings.m_ib.Format;
			PR_EXPAND(PR_DBG_RDR, NameResource(mb->m_ib.get(), FmtS("model IBuffer <V:%d,I:%d>", settings.m_vb.ElemCount, settings.m_ib.ElemCount)));
		}
		return mb;
	}

	// Create a model. A model buffer is also created for this model
	ModelPtr ModelManager::CreateModel(MdlSettings const& settings)
	{
		ModelBufferPtr mb = CreateModelBuffer(settings);
		return CreateModel(settings, mb);
	}

	// Create a model within the provided model buffer. The buffer must contain sufficient space for the model
	ModelPtr ModelManager::CreateModel(MdlSettings const& settings, ModelBufferPtr& model_buffer)
	{
		PR_ASSERT(PR_DBG_RDR, model_buffer->IsCompatible(settings), "Incompatible model buffer provided");
		PR_ASSERT(PR_DBG_RDR, model_buffer->IsRoomFor(settings.m_vb.ElemCount, settings.m_ib.ElemCount), "Insufficient room for a model of this size in this model buffer");
		Renderer::Lock lock(m_rdr);

		ModelPtr ptr(rdr::New<Model>(settings, model_buffer), true);
		assert(m_dbg_mem_mdl.add(ptr.m_ptr));
		return ptr;
	}

	// Create a nugget using our allocator
	Nugget* ModelManager::CreateNugget(NuggetData const& ndata, ModelBuffer* model_buffer, Model* model)
	{
		Renderer::Lock lock(m_rdr);
		auto ptr = rdr::New<Nugget>(ndata, model_buffer, model);
		assert(m_dbg_mem_nugget.add(ptr));
		return ptr;
	}

	// Return a model buffer to the allocator
	void ModelManager::Delete(ModelBuffer* model_buffer)
	{
		if (!model_buffer) return;
		Renderer::Lock lock(m_rdr);
		assert(m_dbg_mem_mdlbuf.remove(model_buffer));
		rdr::Delete<ModelBuffer>(model_buffer);
	}

	// Return a model to the allocator
	void ModelManager::Delete(Model* model)
	{
		if (!model) return;
		ModelDeleted(*model);
		Renderer::Lock lock(m_rdr);
		assert(m_dbg_mem_mdl.remove(model));
		rdr::Delete<Model>(model);
	}

	// Return a render nugget to the allocator
	void ModelManager::Delete(Nugget* nugget)
	{
		if (!nugget) return;
		Renderer::Lock lock(m_rdr);
		assert(m_dbg_mem_nugget.remove(nugget));
		rdr::Delete<Nugget>(nugget);
	}

	// Create stock models
	void ModelManager::CreateStockModels()
	{
		{// Unit quad in Z = 0 plane
			Vert const verts[4] =
			{
				{v4(-0.5f,-0.5f, 0, 1), ColourWhite, v4ZAxis, v2(0.0000f,0.9999f)},
				{v4( 0.5f,-0.5f, 0, 1), ColourWhite, v4ZAxis, v2(0.9999f,0.9999f)},
				{v4( 0.5f, 0.5f, 0, 1), ColourWhite, v4ZAxis, v2(0.9999f,0.0000f)},
				{v4(-0.5f, 0.5f, 0, 1), ColourWhite, v4ZAxis, v2(0.0000f,0.0000f)},
			};
			uint16_t const idxs[] =
			{
				0, 1, 2, 0, 2, 3
			};
			BBox const bbox(v4Origin, v4(1,1,0,0));

			MdlSettings s(verts, idxs, bbox, "unit quad");
			m_unit_quad = CreateModel(s);

			NuggetProps n(EPrim::TriList, Vert::GeomMask);
			m_unit_quad->CreateNugget(n);
		}
		{// Bounding box cube
			Vert const verts[] =
			{
				{v4(-0.5f, -0.5f, -0.5f, 1.0f), Colour32Blue, v4Zero, v2Zero},
				{v4(+0.5f, -0.5f, -0.5f, 1.0f), Colour32Blue, v4Zero, v2Zero},
				{v4(+0.5f, +0.5f, -0.5f, 1.0f), Colour32Blue, v4Zero, v2Zero},
				{v4(-0.5f, +0.5f, -0.5f, 1.0f), Colour32Blue, v4Zero, v2Zero},
				{v4(-0.5f, -0.5f, +0.5f, 1.0f), Colour32Blue, v4Zero, v2Zero},
				{v4(+0.5f, -0.5f, +0.5f, 1.0f), Colour32Blue, v4Zero, v2Zero},
				{v4(+0.5f, +0.5f, +0.5f, 1.0f), Colour32Blue, v4Zero, v2Zero},
				{v4(-0.5f, +0.5f, +0.5f, 1.0f), Colour32Blue, v4Zero, v2Zero},
			};
			uint16_t const idxs[] =
			{
				0, 1, 1, 2, 2, 3, 3, 0,
				4, 5, 5, 6, 6, 7, 7, 4,
				0, 4, 1, 5, 2, 6, 3, 7,
			};
			BBox const bbox(v4Origin, v4(1,1,1,0));

			MdlSettings s(verts, idxs, bbox, "bbox cube");
			m_bbox_model = CreateModel(s);

			NuggetProps n(EPrim::LineList, EGeom::Vert|EGeom::Colr);
			m_bbox_model->CreateNugget(n);
		}
	}
}