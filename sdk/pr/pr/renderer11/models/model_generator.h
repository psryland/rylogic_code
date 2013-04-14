//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************
#pragma once
#ifndef PR_RDR_MODELS_MODEL_GENERATOR_H
#define PR_RDR_MODELS_MODEL_GENERATOR_H

#include "pr/renderer11/forward.h"
#include "pr/renderer11/models/model.h"

namespace pr
{
	namespace rdr
	{
		namespace model
		{
			// Lines
			// Generate lines from an array of start point, end point pairs.
			// 'num_lines' is the number of start/end point pairs in the following arrays
			// 'points' is the input array of start and end points for lines.
			// 'num_colours' should be either, 0, 1, or num_lines * 2
			// 'colours' is an input array of colour values or a pointer to a single colour.
			// 'mat' is an optional material to use for the lines
			typedef VertPC LineVerts;
			ModelPtr Lines(Renderer& rdr ,std::size_t num_lines ,v4 const* points ,std::size_t num_colours = 0 ,Colour32 const* colours = 0 ,DrawMethod const* mat = 0);
			ModelPtr Lines(Renderer& rdr ,std::size_t num_lines ,v4 const* points ,Colour32 colour ,DrawMethod const* mat = 0);
			ModelPtr LinesD(Renderer& rdr ,std::size_t num_lines ,v4 const* points ,v4 const* directions ,std::size_t num_colours = 0 ,Colour32 const* colours = 0 ,DrawMethod const* mat = 0);
			ModelPtr LinesD(Renderer& rdr ,std::size_t num_lines ,v4 const* points ,v4 const* directions ,Colour32 colour ,DrawMethod const* mat = 0);

			//// Quad
			//void        QuadSize(Range& vrange, Range& irange, std::size_t num_quads);
			//Settings    QuadModelSettings(std::size_t num_quads);
			//ModelPtr    Quad    (MLock& mlock  ,MaterialManager& matmgr ,v4 const* point ,std::size_t num_quads ,Colour32 const* colours = 0 ,std::size_t num_colours = 0 ,rdr::Material const* mat = 0 ,Range* vrange = 0 ,Range* irange = 0);
			//ModelPtr    Quad    (Renderer& rdr                          ,v4 const* point ,std::size_t num_quads ,Colour32 const* colours = 0 ,std::size_t num_colours = 0 ,rdr::Material const* mat = 0 ,Range* vrange = 0 ,Range* irange = 0);
			//ModelPtr    Quad    (MLock& mlock  ,MaterialManager& matmgr ,v4 const& centre ,v4 const& forward ,float width ,float height ,Colour32 const* colours = 0 ,std::size_t num_colours = 0 ,rdr::Material const* mat = 0 ,Range* vrange = 0 ,Range* irange = 0);
			//ModelPtr    Quad    (Renderer& rdr                          ,v4 const& centre ,v4 const& forward ,float width ,float height ,Colour32 const* colours = 0 ,std::size_t num_colours = 0 ,rdr::Material const* mat = 0 ,Range* vrange = 0 ,Range* irange = 0);

			// Sphere
			typedef VertPCNT SphereVerts;
			ModelPtr Sphere(Renderer& rdr, v4 const& radius, std::size_t divisions, Colour32 colour = Colour32White, DrawMethod const* mat = 0);
			//void        SphereSize(Range& vrange, Range& irange, std::size_t divisions);
			//Settings    SphereModelSettings(std::size_t divisions);
			//ModelPtr    SphereRxyz(MLock& mlock ,MaterialManager& matmgr ,float xradius ,float yradius ,float zradius ,v4 const& position ,std::size_t divisions = 1 ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* vrange = 0 ,Range* irange = 0);
			//ModelPtr    SphereRxyz(Renderer& rdr                         ,float xradius ,float yradius ,float zradius ,v4 const& position ,std::size_t divisions = 1 ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* vrange = 0 ,Range* irange = 0);

			// Boxes
			typedef VertPCNT BoxVerts;
			ModelPtr Boxes(Renderer& rdr, std::size_t num_boxes, v4 const* points, std::size_t num_colours = 0, Colour32 const* colours = 0, DrawMethod const* mat = 0);
			ModelPtr Boxes(Renderer& rdr, std::size_t num_boxes, v4 const* points, m4x4 const& o2w, std::size_t num_colours = 0, Colour32 const* colours = 0, DrawMethod const* mat = 0);
			ModelPtr Box(Renderer& rdr, v4 const& rad, m4x4 const& o2w, Colour32 colour, DrawMethod const* mat = 0);
			ModelPtr BoxList(Renderer& rdr, std::size_t num_boxes, v4 const* positions, v4 const& dim, std::size_t num_colours = 0, Colour32 const* colours = 0, DrawMethod const* mat = 0);

			//// Cone
			//void        ConeSize(Range& vrange, Range& irange, std::size_t layers, std::size_t wedges);
			//Settings    ConeModelSettings(std::size_t layers, std::size_t wedges);
			//ModelPtr    Cone(MLock& mlock  ,MaterialManager& matmgr         ,float height ,float radius0 ,float radius1 ,float xscale ,float yscale ,m4x4 const& o2w = pr::m4x4Identity ,std::size_t layers = 1 ,std::size_t wedges = 20 ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* vrange = 0 ,Range* irange = 0);
	 	//	ModelPtr    Cone(Renderer& rdr                                  ,float height ,float radius0 ,float radius1 ,float xscale ,float yscale ,m4x4 const& o2w = pr::m4x4Identity ,std::size_t layers = 1 ,std::size_t wedges = 20 ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* vrange = 0 ,Range* irange = 0);
			//ModelPtr    CylinderHRxy(MLock& mlock  ,MaterialManager& matmgr ,float height ,float xradius ,float yradius ,m4x4 const& o2w ,std::size_t layers = 1 ,std::size_t wedges = 20 ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* vrange = 0 ,Range* irange = 0);
	 	//	ModelPtr    CylinderHRxy(Renderer& rdr                          ,float height ,float xradius ,float yradius ,m4x4 const& o2w ,std::size_t layers = 1 ,std::size_t wedges = 20 ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* vrange = 0 ,Range* irange = 0);

			//// Capsule
			//void        CapsuleSize(Range& vrange, Range& irange, std::size_t divisions);
			//Settings    CapsuleModelSettings(std::size_t divisions);
			//ModelPtr    CapsuleHRxy(MLock& mlock  ,MaterialManager& matmgr ,float height ,float xradius ,float yradius ,m4x4 const& o2w = pr::m4x4Identity ,std::size_t divisions = 3 ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* vrange = 0 ,Range* irange = 0);
	 	//	ModelPtr    CapsuleHRxy(Renderer& rdr                          ,float height ,float xradius ,float yradius ,m4x4 const& o2w = pr::m4x4Identity ,std::size_t divisions = 3 ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* vrange = 0 ,Range* irange = 0);

			//// Mesh
			//template <typename Vert, typename Indx> MdlSettings MeshModelSettings(size_t vert_count, size_t indx_count)
			//{
			//	VBufferDesc vb(vert_count, (Vert*)0);
			//	IBufferDesc ib(indx_count, (Indx*)0);
			//	return MdlSettings(vb,ib);
			//}
			//template <typename Vert, typename Indx> ModelPtr Mesh(Renderer& rdr                          ,D3D11_PRIMITIVE_TOPOLOGY topo ,pr::GeomType geom_type ,std::size_t num_indices ,std::size_t num_verts ,rdr::Index const* indices ,v4 const* verts ,v4 const* normals ,Colour32 const* colours ,v2 const* tex_coords ,m4x4 const& o2w = pr::m4x4Identity ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* vrange = 0 ,Range* irange = 0)
			//{
			//	// Handle optional parameters
			//	Range local_vrange, local_irange;
			//	if (!vrange) vrange = &local_vrange;
			//	if (!irange) irange = &local_irange;
			//	vrange->set(0, vert_count);
			//	irange->set(0, indx_count);
			//	
			//	// Shift the ranges into the editable range in the model
			//	vrange->shift((int)mlock.m_vrange.m_begin);
			//	irange->shift((int)mlock.m_irange.m_begin);
			//	PR_ASSERT(PR_DBG_RDR, mlock.m_vrange.contains(*vrange), "Insufficient space in model vertex buffer");
			//	PR_ASSERT(PR_DBG_RDR, mlock.m_irange.contains(*irange), "Insufficient space in model index buffer");

			//	pr::Array<Vert> v(vert_count);
			//	
			//	mlock.m_model = rdr.
			//}
			//template <typename Vert, typename Indx> ModelPtr Mesh(MLock& mlock  ,MaterialManager& matmgr ,D3D11_PRIMITIVE_TOPOLOGY topo ,pr::GeomType geom_type ,std::size_t indx_count ,std::size_t vert_count ,rdr::Index const* indices ,v4 const* verts ,v4 const* normals ,Colour32 const* colours ,v2 const* tex_coords ,m4x4 const& o2w = pr::m4x4Identity ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* vrange = 0 ,Range* irange = 0)
			//{
			//	

			//	bool has_alpha = false;
			//	vf::iterator vb   = mlock.m_vlock.m_ptr + vrange->m_begin;
			//	rdr::Index*  ib   = mlock.m_ilock.m_ptr + irange->m_begin;
			//	rdr::Index   base = value_cast<rdr::Index>(vrange->m_begin);
			//	for (std::size_t i = 0; i != num_verts; ++i)
			//	{
			//		v4 const*       v = verts++;
			//		v4 const*       n = normals    ? normals++    : &v4YAxis;
			//		Colour32 const* c = colours    ? colours++    : &colour;
			//		v2 const*       t = tex_coords ? tex_coords++ : &v2Zero;
			//		v4 point = o2w * *v;
			//		v4 norm  = o2w * *n;
			//		vb->set(point, norm, *c, *t); ++vb;

			//		// Grow the bounding box
			//		pr::Encompase(mlock.m_model->m_bbox, point);

			//		// Look for alpha
			//		has_alpha |= c->a() != 0xFF;
			//	}
			//	for (std::size_t i = 0; i != num_indices; ++i)
			//	{
			//		*ib++ = base + *indices++;
			//	}

			//	// Add a render nugget
			//	rdr::Material local_mat = mat ? *mat : matmgr.GetMaterial(geom_type);
			//	SetAlphaRenderStates(local_mat.m_rsb, has_alpha);
			//	mlock.m_model->SetMaterial(local_mat, prim_type, false, vrange, irange);

			//	// Update the editable ranges
			//	mlock.m_vrange.m_begin += vrange->size();
			//	mlock.m_irange.m_begin += irange->size();
			//	return mlock.m_model;
			//}
//// General
//void        GenerateNormals(MLock& mlock, Range const* vrange = 0, Range const* irange = 0);
//void        GenerateNormals(ModelPtr& model, Range const* vrange = 0, Range const* irange = 0);
//void        SetVertexColours(MLock& mlock, Colour32 colour, Range const* vrange = 0);

		}
	}
}

#endif
