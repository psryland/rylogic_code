//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/models/model.h"

namespace pr
{
	namespace rdr
	{
		template <typename VType = Vert, typename IType = pr::uint16>
		struct ModelGenerator
		{
			// A container for the model data
			struct Cont
			{
				typedef std::vector<VType> VCont;
				typedef std::vector<IType> ICont;
				typedef typename VCont::iterator VIter;
				typedef typename ICont::iterator IIter;
				VCont m_vcont;
				ICont m_icont;

				enum { GeomMask = VType::GeomMask };

				Cont(std::size_t vcount, std::size_t icount)
					:m_vcont(vcount)
					,m_icont(icount)
				{}
			};

			// Helper function for creating a model
			template <typename GenFunc>
			static ModelPtr Create(Renderer& rdr, std::size_t vcount, std::size_t icount, EPrim topo, NuggetProps const* ddata_, GenFunc& GenerateFunc)
			{
				// Generate the model in local buffers
				Cont cont(vcount, icount);
				pr::geometry::Props props = GenerateFunc(begin(cont.m_vcont), begin(cont.m_icont));

				// Create the model
				VBufferDesc vb(cont.m_vcont.size(), &cont.m_vcont[0]);
				IBufferDesc ib(cont.m_icont.size(), &cont.m_icont[0]);
				ModelPtr model = rdr.m_mdl_mgr.CreateModel(MdlSettings(vb, ib, props.m_bbox));

				// Default nugget creation for the model
				NuggetProps ddata;

				// If draw data is provided, use it
				if (ddata_ != nullptr)
					ddata = *ddata_;

				// Set primitive type, this is non-negotiable
				ddata.m_topo = topo;
				
				// Default the geometry type from the generate function
				if (ddata.m_geom == EGeom::Invalid)
					ddata.m_geom = props.m_geom;

				// If no shader has been provided, choose one based on the model geometry
				if (ddata.m_shader == nullptr)
					ddata.m_shader = rdr.m_shdr_mgr.FindShaderFor(ddata.m_geom).m_ptr;

				// If the model geom has valid texture data but no texture, use white
				if (AllSet(ddata.m_geom, EGeom::Tex0) && ddata.m_tex_diffuse == nullptr)
					ddata.m_tex_diffuse = rdr.m_tex_mgr.FindTexture(EStockTexture::White);

				// If the model has alpha, set the alpha blending state
				if (props.m_has_alpha)
					SetAlphaBlending(ddata, true);

				// Create the render nugget
				model->CreateNugget(ddata);
				return model;
			}

			// Lines ******************************************************************************
			// Generate lines from an array of start point, end point pairs.
			// 'num_lines' is the number of start/end point pairs in the following arrays
			// 'points' is the input array of start and end points for lines.
			// 'num_colours' should be either, 0, 1, or num_lines * 2
			// 'colours' is an input array of colour values or a pointer to a single colour.
			// 'mat' is an optional material to use for the lines
			static ModelPtr Lines(Renderer& rdr ,std::size_t num_lines ,v4 const* points ,std::size_t num_colours = 0 ,Colour32 const* colours = 0 ,NuggetProps const* mat = 0)
			{
				auto gen = [=](Cont::VIter vb, Cont::IIter ib){ return pr::geometry::Lines(num_lines, points, num_colours, colours, vb, ib); };

				std::size_t vcount, icount;
				pr::geometry::LineSize(num_lines, vcount, icount);
				return Create(rdr, vcount, icount, EPrim::LineList, mat, gen);
			}
			static ModelPtr LinesD(Renderer& rdr ,std::size_t num_lines ,v4 const* points ,v4 const* directions ,std::size_t num_colours = 0 ,Colour32 const* colours = 0 ,NuggetProps const* mat = 0)
			{
				auto gen = [=](Cont::VIter vb, Cont::IIter ib){ return pr::geometry::LinesD(num_lines, points, directions, num_colours, colours, vb, ib); };

				std::size_t vrange, irange;
				pr::geometry::LineSize(num_lines, vcount, icount);
				return Create(rdr, vcount, icount, EPrim::LineList, mat, gen);
			}
			static ModelPtr LinesD(Renderer& rdr ,std::size_t num_lines ,v4 const* points ,v4 const* directions ,Colour32 colour ,NuggetProps const* mat = 0)
			{
				auto gen = [=](Cont::VIter vb, Cont::IIter ib){ return pr::geometry::LinesD(num_lines, points, directions, colour, vb, ib); };

				std::size_t vcount, icount;
				pr::geometry::LineSize(num_lines, vcount, icount);
				return Create(rdr, vcount, icount, EPrim::LineList, mat, gen);
			}

			// Quad *******************************************************************************
			static ModelPtr Quad(Renderer& rdr, size_t num_quads, v4 const* verts, size_t num_colours = 0, Colour32 const* colours = 0, m4x4 const& t2q = m4x4Identity, NuggetProps const* mat = 0)
			{
				auto gen = [=](Cont::VIter vb, Cont::IIter ib){ return pr::geometry::Quad(num_quads, verts, num_colours, colours, t2q, vb, ib); };

				std::size_t vcount, icount;
				pr::geometry::QuadSize(num_quads, vcount, icount);
				return Create(rdr, vcount, icount, EPrim::TriList, mat, gen);
			}
			static ModelPtr Quad(Renderer& rdr, v4 const& origin, v4 const& patch_x, v4 const& patch_y, iv2 const& divisions = iv2Zero, Colour32 colour = Colour32White, m4x4 const& t2q = m4x4Identity, NuggetProps const* mat = 0)
			{
				auto gen = [=](Cont::VIter vb, Cont::IIter ib){ return pr::geometry::Quad(origin, quad_x, quad_z, divisions, colour, t2q, vb, ib); };

				std::size_t vcount, icount;
				pr::geometry::QuadSize(divisions, vcount, icount);
				return Create(rdr, vcount, icount, EPrim::TriList, mat, gen);
			}
			static ModelPtr Quad(Renderer& rdr, float width, float height, iv2 const& divisions = iv2Zero, Colour32 colour = Colour32White, NuggetProps const* mat = 0)
			{
				auto gen = [=](Cont::VIter vb, Cont::IIter ib){ return pr::geometry::Quad(width, height, divisions, colour, vb, ib); };

				std::size_t vcount, icount;
				pr::geometry::QuadSize(divisions, vcount, icount);
				return Create(rdr, vcount, icount, EPrim::TriList, mat, gen);
			}
			static ModelPtr Quad(Renderer& rdr, v4 const& centre, v4 const& forward, v4 const& top, float width, float height, iv2 const& divisions = iv2Zero, Colour32 colour = Colour32White, v2 const& tex_origin = v2Zero, v2 const& tex_dim = v2One, NuggetProps const* mat = 0)
			{
				auto gen = [=](Cont::VIter vb, Cont::IIter ib){ return pr::geometry::Quad(centre, forward, top, width, height, divisions, colour, tex_origin, tex_dim, vb, ib); };

				std::size_t vcount, icount;
				pr::geometry::QuadSize(divisions, vcount, icount);
				return Create(rdr, vcount, icount, EPrim::TriList, mat, gen);
			}

			// Boxes ******************************************************************************
			static ModelPtr Boxes(Renderer& rdr, std::size_t num_boxes, v4 const* points, std::size_t num_colours = 0, Colour32 const* colours = 0, NuggetProps const* mat = 0)
			{
				auto gen = [=](Cont::VIter vb, Cont::IIter ib){ return pr::geometry::Boxes(num_boxes, points, num_colours, colours, vb, ib); };

				std::size_t vcount, icount;
				pr::geometry::BoxSize(num_boxes, vcount, icount);
				return Create(rdr, vcount, icount, EPrim::TriList, mat, gen);
			}
			static ModelPtr Boxes(Renderer& rdr, std::size_t num_boxes, v4 const* points, m4x4 const& o2w, std::size_t num_colours = 0, Colour32 const* colours = 0, NuggetProps const* mat = 0)
			{
				auto gen = [=](Cont::VIter vb, Cont::IIter ib){ return pr::geometry::Boxes(num_boxes, points, o2w, num_colours, colours, vb, ib); };

				std::size_t vcount, icount;
				pr::geometry::BoxSize(num_boxes, vcount, icount);
				return Create(rdr, vcount, icount, EPrim::TriList, mat, gen);
			}
			static ModelPtr Box(Renderer& rdr, v4 const& rad, m4x4 const& o2w = m4x4Identity, Colour32 colour = Colour32White, NuggetProps const* mat = 0)
			{
				auto gen = [=](Cont::VIter vb, Cont::IIter ib){ return pr::geometry::Box(rad, o2w, colour, vb, ib); };

				std::size_t vcount, icount;
				pr::geometry::BoxSize(1, vcount, icount);
				return Create(rdr, vcount, icount, EPrim::TriList, mat, gen);
			}
			static ModelPtr Box(Renderer& rdr, float rad, m4x4 const& o2w = m4x4Identity, Colour32 colour = Colour32White, NuggetProps const* mat = 0)
			{
				return Box(rdr, v4::make(rad), o2w, colour, mat);
			}
			static ModelPtr BoxList(Renderer& rdr, std::size_t num_boxes, v4 const* positions, v4 const& rad, std::size_t num_colours = 0, Colour32 const* colours = 0, NuggetProps const* mat = 0)
			{
				auto gen = [=](Cont::VIter vb, Cont::IIter ib){ return pr::geometry::BoxList(num_boxes, positions, rad, num_colours, colours, vb, ib); };

				std::size_t vcount, icount;
				pr::geometry::BoxSize(num_boxes, vcount, icount);
				return Create(rdr, vcount, icount, EPrim::TriList, mat, gen);
			}

			// Sphere *****************************************************************************
			static ModelPtr Geosphere(Renderer& rdr, v4 const& radius, std::size_t divisions = 3, Colour32 colour = Colour32White, NuggetProps const* mat = 0)
			{
				auto gen = [=](Cont::VIter vb, Cont::IIter ib){ return pr::geometry::Geosphere(radius, divisions, colour, vb, ib); };

				std::size_t vcount, icount;
				pr::geometry::GeosphereSize(divisions, vcount, icount);
				return Create(rdr, vcount, icount, EPrim::TriList, mat, gen);
			}
			static ModelPtr Geosphere(Renderer& rdr, float radius, std::size_t divisions = 3, Colour32 colour = Colour32White, NuggetProps const* mat = 0)
			{
				return Geosphere(rdr, v4::make(radius, 0.0f), divisions, colour, mat);
			}
			static ModelPtr Sphere(Renderer& rdr, v4 const& radius, std::size_t wedges = 20, std::size_t layers = 5, Colour32 colour = Colour32White, NuggetProps const* mat = 0)
			{
				auto gen = [=](Cont::VIter vb, Cont::IIter ib){ return pr::geometry::Sphere(radius, wedges, layers, colour, vb, ib); };

				std::size_t vcount, icount;
				pr::geometry::SphereSize(wedges, layers, vcount, icount);
				return Create(rdr, vcount, icount, EPrim::TriList, mat, gen);
			}
			static ModelPtr Sphere(Renderer& rdr, float radius, std::size_t wedges = 20, std::size_t layers = 5, Colour32 colour = Colour32White, NuggetProps const* mat = 0)
			{
				return Sphere(rdr, v4::make(radius, 0.0f), wedges, layers, colour, mat);
			}

			// Cylinder ***************************************************************************
			static ModelPtr Cylinder(Renderer& rdr, float radius0, float radius1, float height, m4x4 const& o2w = m4x4Identity, float xscale = 1.0f, float yscale = 1.0f, std::size_t wedges = 20, std::size_t layers = 1, std::size_t num_colours = 0, Colour32 const* colours = 0, NuggetProps const* mat = 0)
			{
				auto gen = [=](Cont::VIter vb, Cont::IIter ib){ return pr::geometry::Cylinder(radius0, radius1, height, o2w, xscale, yscale, wedges, layers, num_colours, colours, vb, ib); };

				std::size_t vcount, icount;
				pr::geometry::CylinderSize(wedges, layers, vcount, icount);
				return Create(rdr, vcount, icount, EPrim::TriList, mat, gen);
			}

			// Capsule ****************************************************************************
			//void        CapsuleSize(Range& vrange, Range& irange, std::size_t divisions);
			//Settings    CapsuleModelSettings(std::size_t divisions);
			//ModelPtr    CapsuleHRxy(MLock& mlock  ,MaterialManager& matmgr ,float height ,float xradius ,float yradius ,m4x4 const& o2w = pr::m4x4Identity ,std::size_t divisions = 3 ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* vrange = 0 ,Range* irange = 0);
			//ModelPtr    CapsuleHRxy(Renderer& rdr                          ,float height ,float xradius ,float yradius ,m4x4 const& o2w = pr::m4x4Identity ,std::size_t divisions = 3 ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* vrange = 0 ,Range* irange = 0);

			// Mesh *******************************************************************************
			static ModelPtr Mesh(Renderer& rdr, EPrim prim_type, std::size_t num_verts, std::size_t num_indices, v4 const* verts, pr::uint16 const* indices, std::size_t num_colours = 0, Colour32 const* colours = 0, std::size_t num_normals = 0, v4 const* normals = 0, v2 const* tex_coords = 0, NuggetProps const* mat = 0)
			{
				auto gen = [=](Cont::VIter vb, Cont::IIter ib){ return pr::geometry::Mesh(num_verts, num_indices, verts, indices, num_colours, colours, num_normals, normals, tex_coords, vb, ib); };

				std::size_t vcount, icount;
				pr::geometry::MeshSize(num_verts, num_indices, vcount, icount);
				return Create(rdr, vcount, icount, prim_type, mat, gen);
			}

			//// Utility ****************************************************************************
			//// Generate normals for this model
			//// Assumes the locked region of the model contains a triangle list
			//// Note: you're probably better off generating the normals before creating the model.
			//// That way you don't need to create a buffer that has CPU read/write access
			//template <typename TVert> void GenerateNormals(MLock& mlock, Range const* vrange = 0, Range const* irange = 0)
			//{
			//	if (!vrange) vrange = &mlock.m_vrange;
			//	if (!irange) irange = &mlock.m_irange;
			//	PR_ASSERT(PR_DBG_RDR, IsWithin(mlock.m_vlock.m_range, *vrange), "The provided vertex range is not within the locked range");
			//	PR_ASSERT(PR_DBG_RDR, IsWithin(mlock.m_ilock.m_range, *irange), "The provided index range is not within the locked range");
			//	PR_ASSERT(PR_DBG_RDR, (irange->size() % 3) == 0, "This function assumes the index range refers to a triangle list");

			//	TVert* verts = mlock.m_vlock.ptr<TVert>()
			//	pr::geometry::GenerateNormals();
			//}
			//template <typename TVert> void GenerateNormals(ModelPtr& model, Range const* vrange, Range const* irange)
			//{
			//	MLock mlock(model);
			//	GenerateNormals<TVert>(mlock, vrange, irange);
			//}
			//void SetVertexColours(MLock& mlock, Colour32 colour, Range const* vrange = 0)
			//{
			//	if (!vrange) vrange = &mlock.m_vrange;
			//	PR_ASSERT(PR_DBG_RDR, IsWithin(mlock.m_vlock.m_range, *vrange), "The provided vertex range is not within the locked range");

			//	TVert* vb = mlock.m_vlock.ptr + v_range->m_begin;
			//	for (std::size_t v = 0; v != v_range->size(); ++v, ++vb)
			//		vb->colour() = colour;
			//}
		};
	}
}
