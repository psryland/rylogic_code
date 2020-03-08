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
#include "pr/view3d/models/model_manager.h"
#include "pr/view3d/models/model.h"
#include "pr/view3d/models/nugget.h"
#include "pr/view3d/util/stock_resources.h"

namespace pr::rdr
{
	DiagState::DiagState()
		:m_normal_lengths(0.1f)
		,m_normal_colour(Colour32Purple)
		,m_bboxes_visible(false)
	{
	}

	// Enable/Disable normals on 'model'. Set length to 0 to disable
	void ShowNormals(Model* model, bool show)
	{
		// The length property is controlled independently
		auto id = pr::hash::Hash("ShowNormals");

		// Remove dependent nuggets used to show normals
		for (auto& nug : model->m_nuggets)
		{
			auto nuggets = chain::filter(nug.m_nuggets, [=](auto& n)
			{
				auto& gs = n.m_smap[ERenderStep::ForwardRender].m_gs;
				return gs != nullptr && gs->m_id == id;
			});
			for (;!nuggets.empty();)
				nuggets.front().Delete();
		}

		// If showing normals, add a dependent nugget for each nugget that has valid vertex normals
		if (show)
		{
			// Get or create an instance of the ShowNormals shader
			auto shdr = model->rdr().m_shdr_mgr.GetShader<ShowNormalsGS>(id, RdrId(EStockShader::ShowNormalsGS));

			// Add a dependent nugget for each existing nugget that has vertex normals
			for (auto& nug : model->m_nuggets)
			{
				if (!AllSet(nug.m_geom, EGeom::Norm))
					continue;

				// Create a dependent nugget that draws the normals
				auto& dep = *model->mdl_mgr().CreateNugget(nug, nug.m_model_buffer, nullptr);
				dep.m_smap[ERenderStep::ForwardRender].m_gs = shdr;
				dep.m_topo = EPrim::PointList;
				dep.m_geom = EGeom::Vert | EGeom::Colr;
				dep.m_owner = nug.m_owner;
				dep.m_irange = RangeZero;
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
			bbox.SizeX() + maths::tiny,
			bbox.SizeY() + maths::tiny,
			bbox.SizeZ() + maths::tiny,
			bbox.Centre());
	}
}
