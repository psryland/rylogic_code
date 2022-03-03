//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
// Functions that enable diagnostic features

#include "pr/view3d/forward.h"
#include "pr/view3d/util/diagnostic.h"
#include "pr/view3d/render/renderer.h"
#include "pr/view3d/shaders/shader_manager.h"
#include "pr/view3d/shaders/shader.h"
#include "pr/view3d/shaders/shader_set.h"
#include "pr/view3d/shaders/shdr_diagnostic.h"
#include "pr/view3d/shaders/shdr_screen_space.h"
#include "pr/view3d/models/model_manager.h"
#include "pr/view3d/models/model.h"
#include "pr/view3d/models/nugget.h"
#include "pr/view3d/util/stock_resources.h"

namespace pr::rdr
{
	constexpr RdrId ShowNormalsId = hash::HashCT("ShowNormals");

	DiagState::DiagState(Renderer& rdr)
		:m_normal_lengths(0.1f)
		,m_normal_colour(Colour32Purple)
		,m_bboxes_visible(false)
		,m_gs_fillmode_points()
	{
		auto shdr = rdr.m_shdr_mgr.GetShader<PointSpritesGS>(RdrId(hash::Hash("PointFillMode")), RdrId(EStockShader::PointSpritesGS), "Point FillMode");
		shdr->m_size = v2(5.0f, 5.0f);
		shdr->m_depth = false;
		m_gs_fillmode_points = shdr;
	}

	// Enable/Disable normals on 'model'
	void ShowNormals(Model* model, bool show)
	{
		// Notes:
		//  - The normals length property is controlled independently

		// Remove dependent nuggets used to show normals
		for (auto& nug : model->m_nuggets)
			nug.DeleteDependent([](Nugget& n) { return n.m_id == ShowNormalsId; });

		// If showing normals, add a dependent nugget for each nugget that has valid vertex normals
		if (show)
		{
			// Get or create an instance of the ShowNormals shader
			auto shdr = model->rdr().m_shdr_mgr.GetShader<ShowNormalsGS>(ShowNormalsId, RdrId(EStockShader::ShowNormalsGS));

			// Add a dependent nugget for each existing nugget that has vertex normals
			for (auto& nug : model->m_nuggets)
			{
				if (!AllSet(nug.m_geom, EGeom::Norm))
					continue;

				// Create a dependent nugget that draws the normals
				auto& dep = *model->mdl_mgr().CreateNugget(nug, nug.m_model_buffer, nullptr);
				dep.m_smap[ERenderStep::ForwardRender].m_gs = shdr;
				dep.m_topo = ETopo::PointList;
				dep.m_geom = EGeom::Vert | EGeom::Colr;
				dep.m_owner = nug.m_owner;
				dep.m_irange = RangeZero;
				dep.m_id = ShowNormalsId;
				nug.m_nuggets.push_back(dep);
			}
		}

		// Set a bit in the dbg flags
		model->m_dbg_flags = SetBits(model->m_dbg_flags, Model::EDbgFlags::NormalsVisible, show);
	}

	// Create a scale transform that positions a unit box at 'bbox'
	m4x4 BBoxTransform(BBox const& bbox)
	{
		return m4x4::Scale(
			bbox.SizeX() + maths::tinyf,
			bbox.SizeY() + maths::tinyf,
			bbox.SizeZ() + maths::tinyf,
			bbox.Centre());
	}
}
