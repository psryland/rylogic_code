//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

// A collection of functions for adding primitives to models

#pragma once
#ifndef PR_RDR_MODEL_GENERATOR_H
#define PR_RDR_MODEL_GENERATOR_H

#include "pr/renderer/types/forward.h"
#include "pr/renderer/models/types.h"
#include "pr/renderer/models/model.h"

namespace pr
{
	namespace rdr
	{
		namespace model
		{
			// Model Lock - a helper for locking a model vertex and index buffer
			// and for keeping track of ranges as it you fill it with stuff
			struct MLock
			{
				VLock    m_local_vlock;  // A local vertex lock to allow the caller to not provide one
				ILock    m_local_ilock;  // A local index lock to allow the caller to not provide one
				ModelPtr m_model;        // Pointer to the model we're adding to
				VLock&   m_vlock;        // The vertex buffer lock for the model
				ILock&   m_ilock;        // The index buffer lock for the model
				Range    m_vrange;       // The editable range of the model vertices
				Range    m_irange;       // The editable range of the model indices

				MLock(ModelPtr model)
				:m_model(model)
				,m_vlock(m_local_vlock)
				,m_ilock(m_local_ilock)
				{
					m_model->LockVBuffer(m_vlock);
					m_model->LockIBuffer(m_ilock);
					m_vrange = m_vlock.m_range;
					m_irange = m_ilock.m_range;
				}
				MLock(ModelPtr model, VLock& vlock, ILock& ilock)
				:m_model(model)
				,m_vlock(vlock)
				,m_ilock(ilock)
				{
					if (!m_vlock.m_ptr) m_model->LockVBuffer(m_vlock);
					if (!m_ilock.m_ptr) m_model->LockIBuffer(m_ilock);
					m_vrange = m_vlock.m_range;
					m_irange = m_ilock.m_range;
				}
				MLock(ModelPtr model, Range const& vrange, Range const& irange)
				:m_model(model)
				,m_vlock(m_local_vlock)
				,m_ilock(m_local_ilock)
				,m_vrange(vrange)
				,m_irange(irange)
				{
					m_model->LockVBuffer(m_vlock, m_vrange);
					m_model->LockIBuffer(m_ilock, m_irange);
					m_vrange = m_vlock.m_range;
					m_irange = m_ilock.m_range;
				}
				MLock(ModelPtr model, VLock& vlock, ILock& ilock, Range const& vrange, Range const& irange)
				:m_model(model)
				,m_vlock(vlock)
				,m_ilock(ilock)
				,m_vrange(vrange)
				,m_irange(irange)
				{
					if (!m_vlock.m_ptr) m_model->LockVBuffer(m_vlock, m_vrange);
					if (!m_ilock.m_ptr) m_model->LockIBuffer(m_ilock, m_irange);
				}
				MLock(MLock const&); // no copying
				MLock& operator=(MLock const&); // no copying

				// Pointers to the locked vert/index range
				vf::iterator VPtr() { return m_vlock.m_ptr + m_vrange.m_begin; }
				rdr::Index*  IPtr() { return m_ilock.m_ptr + m_irange.m_begin; }
			};
	
			// General
			void        GenerateNormals(MLock& mlock, Range const* v_range = 0, Range const* i_range = 0);
			void        GenerateNormals(ModelPtr& model, Range const* v_range = 0, Range const* i_range = 0);
			void        SetVertexColours(MLock& mlock, Colour32 colour, Range const* v_range = 0);

			// Line
			void        LineSize(Range& v_range, Range& i_range, std::size_t num_lines);
			Settings    LineModelSettings(std::size_t num_lines);
			ModelPtr    Line    (MLock& mlock  ,MaterialManager& matmgr ,v4 const* point ,std::size_t num_lines                        ,Colour32 const* colours, std::size_t num_colours ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);
			ModelPtr    Line    (Renderer& rdr                          ,v4 const* point ,std::size_t num_lines                        ,Colour32 const* colours, std::size_t num_colours ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);
			ModelPtr    Line    (MLock& mlock  ,MaterialManager& matmgr ,v4 const* point ,std::size_t num_lines                        ,Colour32 colour = Colour32White                          ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);
			ModelPtr    Line    (Renderer& rdr                          ,v4 const* point ,std::size_t num_lines                        ,Colour32 colour = Colour32White                          ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);
			ModelPtr    LineD   (MLock& mlock  ,MaterialManager& matmgr ,v4 const* points ,v4 const* directions ,std::size_t num_lines ,Colour32 const* colours, std::size_t num_colours ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);
			ModelPtr    LineD   (Renderer& rdr                          ,v4 const* points ,v4 const* directions ,std::size_t num_lines ,Colour32 const* colours, std::size_t num_colours ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);
			ModelPtr    LineD   (MLock& mlock  ,MaterialManager& matmgr ,v4 const* points ,v4 const* directions ,std::size_t num_lines ,Colour32 colour = Colour32White                          ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);
			ModelPtr    LineD   (Renderer& rdr                          ,v4 const* points ,v4 const* directions ,std::size_t num_lines ,Colour32 colour = Colour32White                          ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);

			// Quad
			void        QuadSize(Range& v_range, Range& i_range, std::size_t num_quads);
			Settings    QuadModelSettings(std::size_t num_quads);
			ModelPtr    Quad    (MLock& mlock  ,MaterialManager& matmgr ,v4 const* point ,std::size_t num_quads ,Colour32 const* colours = 0 ,std::size_t num_colours = 0 ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);
			ModelPtr    Quad    (Renderer& rdr                          ,v4 const* point ,std::size_t num_quads ,Colour32 const* colours = 0 ,std::size_t num_colours = 0 ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);
			ModelPtr    Quad    (MLock& mlock  ,MaterialManager& matmgr ,v4 const& centre ,v4 const& forward ,float width ,float height ,Colour32 const* colours = 0 ,std::size_t num_colours = 0 ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);
			ModelPtr    Quad    (Renderer& rdr                          ,v4 const& centre ,v4 const& forward ,float width ,float height ,Colour32 const* colours = 0 ,std::size_t num_colours = 0 ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);

			// Sphere
			void        SphereSize(Range& v_range, Range& i_range, std::size_t divisions);
			Settings    SphereModelSettings(std::size_t divisions);
			ModelPtr    SphereRxyz(MLock& mlock ,MaterialManager& matmgr ,float xradius ,float yradius ,float zradius ,v4 const& position ,std::size_t divisions = 1 ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);
			ModelPtr    SphereRxyz(Renderer& rdr                         ,float xradius ,float yradius ,float zradius ,v4 const& position ,std::size_t divisions = 1 ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);

			// Box
			void        BoxSize(Range& v_range, Range& i_range, std::size_t num_boxes);
			Settings    BoxModelSettings(std::size_t num_boxes);
			ModelPtr    Box     (MLock& mlock  ,MaterialManager& matmgr ,v4 const* point ,std::size_t num_boxes, m4x4 const& o2w = pr::m4x4Identity ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);
			ModelPtr    Box     (Renderer& rdr                          ,v4 const* point ,std::size_t num_boxes, m4x4 const& o2w = pr::m4x4Identity ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);
			ModelPtr    Box     (MLock& mlock  ,MaterialManager& matmgr ,v4 const& dim ,m4x4 const& o2w = pr::m4x4Identity ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);
			ModelPtr    Box     (Renderer& rdr                          ,v4 const& dim ,m4x4 const& o2w = pr::m4x4Identity ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);
			ModelPtr    BoxList (MLock& mlock  ,MaterialManager& matmgr ,v4 const& dim ,v4 const* positions ,std::size_t num_boxes ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);
			ModelPtr    BoxList (Renderer& rdr                          ,v4 const& dim ,v4 const* positions ,std::size_t num_boxes ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);

			// Cone
			void        ConeSize(Range& v_range, Range& i_range, std::size_t layers, std::size_t wedges);
			Settings    ConeModelSettings(std::size_t layers, std::size_t wedges);
			ModelPtr    Cone(MLock& mlock  ,MaterialManager& matmgr         ,float height ,float radius0 ,float radius1 ,float xscale ,float yscale ,m4x4 const& o2w = pr::m4x4Identity ,std::size_t layers = 1 ,std::size_t wedges = 20 ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);
	 		ModelPtr    Cone(Renderer& rdr                                  ,float height ,float radius0 ,float radius1 ,float xscale ,float yscale ,m4x4 const& o2w = pr::m4x4Identity ,std::size_t layers = 1 ,std::size_t wedges = 20 ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);
			ModelPtr    CylinderHRxy(MLock& mlock  ,MaterialManager& matmgr ,float height ,float xradius ,float yradius ,m4x4 const& o2w ,std::size_t layers = 1 ,std::size_t wedges = 20 ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);
	 		ModelPtr    CylinderHRxy(Renderer& rdr                          ,float height ,float xradius ,float yradius ,m4x4 const& o2w ,std::size_t layers = 1 ,std::size_t wedges = 20 ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);

			// Capsule
			void        CapsuleSize(Range& v_range, Range& i_range, std::size_t divisions);
			Settings    CapsuleModelSettings(std::size_t divisions);
			ModelPtr    CapsuleHRxy(MLock& mlock  ,MaterialManager& matmgr ,float height ,float xradius ,float yradius ,m4x4 const& o2w = pr::m4x4Identity ,std::size_t divisions = 3 ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);
	 		ModelPtr    CapsuleHRxy(Renderer& rdr                          ,float height ,float xradius ,float yradius ,m4x4 const& o2w = pr::m4x4Identity ,std::size_t divisions = 3 ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);

			// Mesh
			void        MeshSize(Range& v_range, Range& i_range, std::size_t num_verts, std::size_t num_indices);
			Settings    MeshModelSettings(std::size_t num_verts, std::size_t num_indices, pr::GeomType geom_type);
			ModelPtr    Mesh(MLock& mlock  ,MaterialManager& matmgr ,EPrimitive::Type prim_type ,pr::GeomType geom_type ,std::size_t num_indices ,std::size_t num_verts ,rdr::Index const* indices ,v4 const* verts ,v4 const* normals ,Colour32 const* colours ,v2 const* tex_coords ,m4x4 const& o2w = pr::m4x4Identity ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);
			ModelPtr    Mesh(Renderer& rdr                          ,EPrimitive::Type prim_type ,pr::GeomType geom_type ,std::size_t num_indices ,std::size_t num_verts ,rdr::Index const* indices ,v4 const* verts ,v4 const* normals ,Colour32 const* colours ,v2 const* tex_coords ,m4x4 const& o2w = pr::m4x4Identity ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* v_range = 0 ,Range* i_range = 0);
		}
	}
}

#endif
