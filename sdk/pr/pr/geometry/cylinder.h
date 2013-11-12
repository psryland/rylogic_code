//********************************
// Geometry
//  Copyright © Rylogic Ltd 2013
//********************************
#pragma once
#ifndef PR_GEOMETRY_CYLINDER_H
#define PR_GEOMETRY_CYLINDER_H

#include "pr/geometry/common.h"

namespace pr
{
	namespace geometry
	{
		// Returns the number of verts and number of indices needed to hold geometry for a cylinder
		template <typename Tvr, typename Tir>
		void CylinderSize(std::size_t wedges, std::size_t layers, Tvr& vcount, Tir& icount)
		{
			if (wedges < 3) wedges = 3;
			if (layers < 1) layers = 1;
			vcount = value_cast<Tvr>(2 + (wedges + 1) * (layers + 3));
			icount = value_cast<Tir>(6 * (wedges + 0) * (layers + 1));
		}

		// Generate a cylinder given by a height and radius at each end, orientated with the long axis along 'Z'
		// 'radius0' is the radius of the bottom face (i.e. -z axis face) of the cylinder
		// 'radius1' is the radius of the top face (i.e. +z axis face) of the cylinder
		// 'height' is the length of the cylinder along the z axis
		// 'o2w' an object to world transform to apply to the verts of the cylinder
		// 'xscale'/'yscale' are scaling factors that can be used to make the cylinder ellipsoidal
		// 'wedges' is the number of divisions around the z axis
		// 'layers' is the number of sections along the zaxis, must be >= 1
		// 'num_colours' should be either, 0, 1, num_boxes, num_boxes*8 representing; no colour, 1 colour for all, 1 colour per box, or 1 colour per box vertex
		// 'colours' is an input array of colour values, a pointer to a single colour, or null.
		// The texture coords assigned to the cylinder map a quad around the 'barrel' of the cylinder and a circle
		// on the ends of the cylinder since this is the most likely way it would be textured
		template <typename TVertIter, typename TIdxIter>
		Props Cylinder(float radius0, float radius1, float height, m4x4 const& o2w, float xscale ,float yscale ,std::size_t wedges, std::size_t layers, std::size_t num_colours, Colour32 const* colours, TVertIter v_out, TIdxIter i_out)
		{
			if (wedges < 3) wedges = 3;
			if (layers < 1) layers = 1;

			std::size_t vcount,icount;
			CylinderSize(wedges, layers, vcount, icount);

			// Colour iterator wrapper
			ColourRepeater col(colours, num_colours, vcount, pr::Colour32White);

			Props props;
			typedef decltype(impl::remove_ref(*i_out)) VIdx;

			float z  = -height * 0.5f;
			float dz = height / layers;
			float da = maths::tau / wedges;
			std::size_t verts_per_layer = wedges + 1;
			std::size_t ibase = 0, last = vcount - 1;

			v4 pt = o2w * v4::make(0, 0, z, 1.0f);
			v4 nm = o2w * -v4ZAxis;
			v2 uv = v2::make(0.5f, 0.5f);

			// Verts
			SetPCNT(*v_out++, pt, *col++, nm, uv);
			for (std::size_t w = 0; w <= wedges; ++w) // Bottom face
			{
				float a = da*w;
				pt = o2w * v4::make(cos(a) * radius0 * xscale, sin(a) * radius0 * yscale, z, 1.0f);
				uv = v2::make(cos(a) * 0.5f + 0.5f, sin(a) * 0.5f + 0.5f);
				SetPCNT(*v_out++, pt, *col++, nm, uv);
			}
			for (std::size_t l = 0; l <= layers; ++l) // The walls
			{
				float r  = Lerp(radius0, radius1, l/(float)layers);
				float nz = radius0 - radius1;
				for (std::size_t w = 0; w <= wedges; ++w)
				{
					float a = da*w + (l%2)*da*0.5f;
					pt = o2w * v4::make(cos(a) * r * xscale, sin(a) * r * yscale, z, 1.0f);
					nm = o2w * v4::normal3(height * cos(a + da*0.5f) / xscale, height * sin(a + da*0.5f) / yscale ,nz ,0.0f);
					uv = v2::make(a / maths::tau, 1.0f - (z + height*0.5f) / height);
					SetPCNT(*v_out++, pt, *col++, nm, uv);
				}
				if (l != layers) z += dz;
			}
			nm = o2w * v4ZAxis;
			for (std::size_t w = 0; w <= wedges; ++w) // Top face
			{
				float a = da*w + (layers%2)*da*0.5f;
				pt = o2w * v4::make(cos(a) * radius1 * xscale, sin(a) * radius1 * yscale, z, 1.0f);
				uv = v2::make(cos(a) * 0.5f + 0.5f, sin(a) * 0.5f + 0.5f);
				SetPCNT(*v_out++, pt, *col++, nm, uv);
			}
			pt = o2w * v4::make(0 ,0 ,z ,1.0f);
			uv = v2::make(0.5, 0.5f);
			SetPCNT(*v_out++, pt, *col++, nm, uv);

			// Faces
			ibase = 1;
			for (std::size_t w = 0; w != wedges; ++w) // Bottom face
			{
				*i_out++ = value_cast<VIdx>(0);
				*i_out++ = value_cast<VIdx>(ibase + w + 1);
				*i_out++ = value_cast<VIdx>(ibase + w);
			}
			ibase += verts_per_layer;
			for (std::size_t l = 0; l != layers; ++l) // The walls
			{
				for (std::size_t w = 0; w != wedges; ++w)
				{
					*i_out++ = value_cast<VIdx>(ibase + w);
					*i_out++ = value_cast<VIdx>(ibase + w + 1);
					*i_out++ = value_cast<VIdx>(ibase + w + verts_per_layer);
					*i_out++ = value_cast<VIdx>(ibase + w + verts_per_layer);
					*i_out++ = value_cast<VIdx>(ibase + w + 1);
					*i_out++ = value_cast<VIdx>(ibase + w + verts_per_layer + 1);
				}
				ibase += verts_per_layer;
			}
			ibase += verts_per_layer;
			for (std::size_t w = 0; w != wedges; ++w) // Top face
			{
				*i_out++ = value_cast<VIdx>(ibase + w);
				*i_out++ = value_cast<VIdx>(ibase + w + 1);
				*i_out++ = value_cast<VIdx>(last);
			}

			float max_radius = std::max(radius0, radius1);
			pr::Encompase(props.m_bbox, o2w * v4::make(-max_radius * xscale, -max_radius * yscale, -height * 0.5f, 1.0f));
			pr::Encompase(props.m_bbox, o2w * v4::make(+max_radius * xscale, +max_radius * yscale, +height * 0.5f, 1.0f));

			props.m_geom = EGeom::Vert | (colours != 0 ? EGeom::Colr : 0) | EGeom::Norm | EGeom::Tex0;
			props.m_has_alpha = col.m_alpha;
			return props;
		}
	}
}

#endif
