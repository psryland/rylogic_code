//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2013
//********************************
#pragma once
#include "pr/geometry/common.h"

namespace pr::geometry
{
	// Return the model buffer requirements of a mesh
	constexpr BufSizes MeshSize(int num_verts, int num_indices)
	{
		return
		{
			num_verts,
			num_indices,
		};
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
	template <typename TVertCIter, typename TIdxCIter, typename TNormCIter, typename VOut, typename IOut>
	Props Mesh(
		int num_verts,
		int num_indices,
		TVertCIter verts,
		TIdxCIter  indices,
		int num_colours,
		Colour32 const* colours,
		int num_normals,
		TNormCIter normals,
		v2 const* tex_coords,
		VOut vout, IOut iout)
	{
		Props props;
		props.m_geom = EGeom::Vert | (colours ? EGeom::Colr : EGeom::None) | (normals ? EGeom::Norm : EGeom::None) | (tex_coords ? EGeom::Tex0 : EGeom::None);

		// Colour iterator wrapper
		auto col = CreateRepeater(colours, num_colours, num_verts, Colour32White);
		auto cc = [&](Colour32 c) { props.m_has_alpha |= HasAlpha(c); return c; };

		// Normal iterator wrapper
		auto norm = CreateRepeater(normals, num_normals, num_verts, v4Zero);

		// UV iterator wrapper
		auto uv = CreateRepeater(tex_coords, tex_coords != 0 ? num_verts : 0, num_verts, v2Zero);

		// Bounding box
		auto bb = [&](v4 const& v) { Encompass(props.m_bbox, v); return v; };

		// Verts
		for (auto v = 0; v != num_verts; ++v)
			vout(bb(*verts++), cc(*col++), *norm++, *uv++);

		// Faces or edges or whatever
		for (auto i = 0; i != num_indices; ++i)
			iout(*indices++);

		return props;
	}
}
