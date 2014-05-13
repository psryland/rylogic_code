//********************************
// Geometry
//  Copyright © Rylogic Ltd 2013
//********************************
#pragma once
#ifndef PR_GEOMETRY_MESH_H
#define PR_GEOMETRY_MESH_H

#include "pr/geometry/common.h"

namespace pr
{
	namespace geometry
	{
		// Return the model buffer requirements of a mesh
		template <typename Tvr, typename Tir>
		void MeshSize(std::size_t num_verts, std::size_t num_indices, Tvr& vcount, Tir& icount)
		{
			vcount = value_cast<Tvr>(num_verts  );
			icount = value_cast<Tir>(num_indices);
		}

		// Generate a model from mesh data
		// 'num_verts' - the number of verts available through the iterator 'verts'
		// 'num_indices' - the number of indices available through the iterator 'indices'
		// 'verts' and 'indices' are the basic model data
		// 'num_colours' - the number of colours pointed to by 'colours', can be equal to 0, 1, or num_verts
		// 'colours' - the array of colours of length 'num_colours'
		// 'num_normals' - the number of normals pointed to by 'normals', can be equal to 0, 1, or num_verts
		// 'normals' - the array of normals of length 'num_normals'
		// 'tex_coords' - must be null or an array of length 'num_verts'
		// Remember you can call "GenerateNormals()" to generate normals.
		template <typename TVertCIter, typename TIdxCIter, typename TNormCIter, typename TVertIter, typename TIdxIter>
		Props Mesh(
			std::size_t num_verts,
			std::size_t num_indices,
			TVertCIter verts,
			TIdxCIter  indices,
			std::size_t num_colours,
			Colour32 const* colours,
			std::size_t num_normals,
			TNormCIter normals,
			v2 const* tex_coords,
			TVertIter v_out, TIdxIter i_out)
		{
			// Colour iterator wrapper
			ColourRepeater col(colours, num_colours, num_verts, pr::Colour32White);

			// Normal iterator wrapper
			auto norm = pr::CreateRepeater(normals, num_normals, num_verts, pr::v4Zero);

			// UV iterator wrapper
			auto uv = pr::CreateRepeater(tex_coords, tex_coords != 0 ? num_verts : 0, num_verts, pr::v2Zero);

			// Verts
			pr::BBox bbox = pr::BBoxReset;
			for (std::size_t v = 0; v != num_verts; ++v)
			{
				auto pt = *verts++;
				auto cl = *col++;
				auto nm = *norm++;
				auto tx = *uv++;
				SetPCNT(*v_out++, pt, cl, nm, tx);
				pr::Encompass(bbox, pt);
			}

			// Faces or edges or whatever
			for (std::size_t i = 0; i != num_indices; ++i)
				*i_out++ = *indices++;

			Props props;
			props.m_geom = EGeom::Vert | (colours != 0 ? EGeom::Colr : 0) | (normals != 0 ? EGeom::Norm : 0) | (tex_coords != 0 ? EGeom::Tex0 : 0);
			props.m_bbox = bbox;
			props.m_has_alpha = col.m_alpha;
			return props;
		}
	}
}

#endif
