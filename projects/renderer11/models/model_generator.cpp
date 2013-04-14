//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/models/model_generator.h"
#include "pr/renderer11/models/model_settings.h"
#include "pr/renderer11/models/model_manager.h"
#include "pr/renderer11/models/input_layout.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/util/lock.h"
#include "pr/geometry/line.h"
#include "pr/geometry/box.h"

using namespace pr::rdr;
using namespace pr::rdr::model;

namespace pr { namespace rdr { namespace model
{
	// Line *****************************************************************************************
	namespace lines
	{
		typedef std::vector<LineVerts>  VCont;
		typedef std::vector<pr::uint16> ICont;
		template <typename GenFunc> pr::rdr::ModelPtr Create(Renderer& rdr, std::size_t num_lines, DrawMethod const* mat, GenFunc GenerateFunc)
		{
			// Determine the buffer requirements for the lines
			Range vrange,irange;
			pr::geometry::LineSize(num_lines, vrange, irange);

			// Generate the lines in local buffers
			VCont verts  (vrange.size());
			ICont indices(irange.size());
			pr::geometry::Props props = GenerateFunc(begin(verts), begin(indices));

			// Create the model
			VBufferDesc vb(verts.size(), &verts[0]);
			IBufferDesc ib(indices.size(), &indices[0]);
			ModelPtr model = rdr.m_mdl_mgr.CreateModel(MdlSettings(vb, ib));
			model->m_bbox = props.m_bbox;

			// Create the render nugget
			auto local_mat = mat ? *mat : DrawMethod(rdr.m_shdr_mgr.FindShaderFor(LineVerts::GeomMask));
			//SetAlphaRenderStates(local_mat.m_rsb, has_alpha);
			model->CreateNugget(local_mat, D3D11_PRIMITIVE_TOPOLOGY_LINELIST);

			return model;
		}
	}
	pr::rdr::ModelPtr pr::rdr::model::Lines(Renderer& rdr, std::size_t num_lines, v4 const* points, std::size_t num_colours, Colour32 const* colours, DrawMethod const* mat)
	{
		auto gen = [=](lines::VCont::iterator vb, lines::ICont::iterator ib){ return pr::geometry::Lines(num_lines, points, num_colours, colours, vb, ib); };
		return lines::Create(rdr, num_lines, mat, gen);
	}
	pr::rdr::ModelPtr pr::rdr::model::Lines(Renderer& rdr, std::size_t num_lines, v4 const* points, Colour32 colour, DrawMethod const* mat)
	{
		auto gen = [=](lines::VCont::iterator vb, lines::ICont::iterator ib){ return pr::geometry::Lines(num_lines, points, colour, vb, ib); };
		return lines::Create(rdr, num_lines, mat, gen);
	}
	pr::rdr::ModelPtr pr::rdr::model::LinesD(Renderer& rdr, std::size_t num_lines, v4 const* points, v4 const* directions, std::size_t num_colours, Colour32 const* colours, DrawMethod const* mat)
	{
		auto gen = [=](lines::VCont::iterator vb, lines::ICont::iterator ib){ return pr::geometry::LinesD(num_lines, points, directions, num_colours, colours, vb, ib); };
		return lines::Create(rdr, num_lines, mat, gen);
	}
	pr::rdr::ModelPtr pr::rdr::model::LinesD(Renderer& rdr, std::size_t num_lines, v4 const* points, v4 const* directions, Colour32 colour, DrawMethod const* mat)
	{
		auto gen = [=](lines::VCont::iterator vb, lines::ICont::iterator ib){ return pr::geometry::LinesD(num_lines, points, directions, colour, vb, ib); };
		return lines::Create(rdr, num_lines, mat, gen);
	}

	// Box *****************************************************************************************
	namespace boxes
	{
		typedef std::vector<BoxVerts> VCont;
		typedef std::vector<pr::uint16> ICont;
		template <typename GenFunc> pr::rdr::ModelPtr Create(Renderer& rdr, std::size_t num_boxes, DrawMethod const* mat, GenFunc& GenerateFunc)
		{
			// Determine the buffer requirements for the lines
			Range vrange,irange;
			pr::geometry::BoxSize(num_boxes, vrange, irange);

			// Generate the lines in local buffers
			VCont verts  (vrange.size());
			ICont indices(irange.size());
			pr::geometry::Props props = GenerateFunc(begin(verts), begin(indices));

			// Create the model
			VBufferDesc vb(verts.size(), &verts[0]);
			IBufferDesc ib(indices.size(), &indices[0]);
			ModelPtr model = rdr.m_mdl_mgr.CreateModel(MdlSettings(vb, ib));
			model->m_bbox = props.m_bbox;

			// Create the render nugget
			auto local_mat = mat ? *mat : DrawMethod(rdr.m_shdr_mgr.FindShaderFor(BoxVerts::GeomMask));
			//SetAlphaRenderStates(local_mat.m_rsb, has_alpha);
			model->CreateNugget(local_mat, D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);

			return model;
		}
	}
	pr::rdr::ModelPtr Boxes(Renderer& rdr, std::size_t num_boxes, v4 const* points, std::size_t num_colours, Colour32 const* colours, DrawMethod const* mat)
	{
		auto gen = [=](boxes::VCont::iterator vb, boxes::ICont::iterator ib){ return pr::geometry::Boxes(num_boxes, points, num_colours, colours, vb, ib); };
		return boxes::Create(rdr, num_boxes, mat, gen);
	}
	pr::rdr::ModelPtr Boxes(Renderer& rdr, std::size_t num_boxes, v4 const* points, m4x4 const& o2w, std::size_t num_colours, Colour32 const* colours, DrawMethod const* mat)
	{
		auto gen = [=](boxes::VCont::iterator vb, boxes::ICont::iterator ib){ return pr::geometry::Boxes(num_boxes, points, o2w, num_colours, colours, vb, ib); };
		return boxes::Create(rdr, num_boxes, mat, gen);
	}
	pr::rdr::ModelPtr Box(Renderer& rdr, v4 const& rad, m4x4 const& o2w, Colour32 colour, DrawMethod const* mat)
	{
		auto gen = [=](boxes::VCont::iterator vb, boxes::ICont::iterator ib){ return pr::geometry::Box(rad, o2w, colour, vb, ib); };
		return boxes::Create(rdr, 1, mat, gen);
	}
	pr::rdr::ModelPtr BoxList(Renderer& rdr, std::size_t num_boxes, pr::v4 const* positions, pr::v4 const& dim, std::size_t num_colours, Colour32 const* colours, DrawMethod const* mat)
	{
		auto gen = [=](boxes::VCont::iterator vb, boxes::ICont::iterator ib){ return pr::geometry::BoxList(num_boxes, positions, dim, num_colours, colours, vb, ib); };
		return boxes::Create(rdr, num_boxes, mat, gen);
	}
		
}}}