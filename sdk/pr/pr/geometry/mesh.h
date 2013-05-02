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
		// 'num_verts' is the number of verts available through the iterator 'verts'
		// 'num_indices' is the number of indices available through the iterator 'indices'
		// 'verts' and 'indices' are the basic model data
		// 'num_colours' is the number of colours points to by 'colours', can be equal to 0, 1, or num_verts
		// 'colours', 'normals', or 'tex_coords' must be null or point to 'num_verts' of each type
		// Remember you can call "model->GenerateNormals()" to generate normals.
		template <typename TVertCIter, typename TIdxCIter, typename TNormCIter, typename TVertIter, typename TIdxIter>
		Props Mesh(
			std::size_t num_verts,
			std::size_t num_indices,
			TVertCIter verts,
			TIdxCIter  indices,
			std::size_t num_colours,
			Colour32 const* colours,
			TNormCIter normals,
			v2 const* tex_coords,
			TVertIter v_out, TIdxIter i_out)
		{
			// Colour iterator wrapper
			ColourRepeater col(colours, num_colours, num_verts, pr::Colour32White);

			// Normal iterator wrapper
			auto norm = pr::CreateRepeater(normals, normals != 0 ? num_verts : 0, num_verts, pr::v4YAxis);

			// UV iterator wrapper
			auto uv = pr::CreateRepeater(tex_coords, tex_coords != 0 ? num_verts : 0, num_verts, pr::v2Zero);

			// Verts
			Props props;
			for (std::size_t v = 0; v != num_verts; ++v)
			{
				v4 pt = *verts++;
				v4 nm = *norm++;
				SetPCNT(*v_out++, pt, *col++, nm, *uv++);
				pr::Encompase(props.m_bbox, pt);
			}

			// Faces or edges or whatever
			for (std::size_t i = 0; i != num_indices; ++i)
				*i_out++ = *indices++;

			props.m_has_alpha = col.m_alpha;
			return props;
		}
	}
}

#endif
