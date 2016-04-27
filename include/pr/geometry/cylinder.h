//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2013
//********************************
#pragma once

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
			vcount = checked_cast<Tvr>(2 + (wedges + 1) * (layers + 3));
			icount = checked_cast<Tir>(6 * (wedges + 0) * (layers + 1));
		}

		// Generate a cylinder given by a height and radius at each end, orientated with the long axis along 'Z'
		// 'radius0' is the radius of the bottom face (i.e. -z axis face) of the cylinder
		// 'radius1' is the radius of the top face (i.e. +z axis face) of the cylinder
		// 'height' is the length of the cylinder along the z axis
		// 'xscale'/'yscale' are scaling factors that can be used to make the cylinder ellipsoidal
		// 'wedges' is the number of divisions around the z axis
		// 'layers' is the number of sections along the zaxis, must be >= 1
		// 'num_colours' should be either, 0, 1, num_boxes, num_boxes*8 representing; no colour, 1 colour for all, 1 colour per box, or 1 colour per box vertex
		// 'colours' is an input array of colour values, a pointer to a single colour, or null.
		// The texture coords assigned to the cylinder map a quad around the 'barrel' of the cylinder and a circle
		// on the ends of the cylinder since this is the most likely way it would be textured
		template <typename TVertIter, typename TIdxIter>
		Props Cylinder(float radius0, float radius1, float height, float xscale ,float yscale ,std::size_t wedges, std::size_t layers, std::size_t num_colours, Colour32 const* colours, TVertIter v_out, TIdxIter i_out)
		{
			typedef decltype(impl::remove_ref(*i_out)) VIdx;
			
			std::size_t vcount,icount;
			if (wedges < 3) wedges = 3;
			if (layers < 1) layers = 1;
			CylinderSize(wedges, layers, vcount, icount);

			Props props;
			props.m_geom = EGeom::Vert | (colours != 0 ? EGeom::Colr : 0) | EGeom::Norm | EGeom::Tex0;

			// Bounding box
			float max_radius = std::max(radius0, radius1);
			Encompass(props.m_bbox, v4(-max_radius * xscale, -max_radius * yscale, -height * 0.5f, 1.0f));
			Encompass(props.m_bbox, v4(+max_radius * xscale, +max_radius * yscale, +height * 0.5f, 1.0f));

			// Colour iterator wrapper
			auto col = pr::CreateRepeater(colours, num_colours, vcount, pr::Colour32White);
			auto cc = [&](pr::Colour32 c) { props.m_has_alpha |= c.a() != 0xff; return c; };

			float z  = -height * 0.5f;
			float dz = height / layers;
			float da = maths::tau / wedges;
			std::size_t verts_per_layer = wedges + 1;
			std::size_t ibase = 0, last = vcount - 1;

			v4 pt = v4(0, 0, z, 1.0f);
			v4 nm = -v4ZAxis;
			v2 uv = v2(0.5f, 0.5f);

			// Verts
			SetPCNT(*v_out++, pt, cc(*col++), nm, uv);
			for (std::size_t w = 0; w <= wedges; ++w) // Bottom face
			{
				float a = da*w;
				pt = v4(cos(a) * radius0 * xscale, sin(a) * radius0 * yscale, z, 1.0f);
				uv = v2(cos(a) * 0.5f + 0.5f, sin(a) * 0.5f + 0.5f);
				SetPCNT(*v_out++, pt, cc(*col++), nm, uv);
			}
			for (std::size_t l = 0; l <= layers; ++l) // The walls
			{
				float r  = Lerp(radius0, radius1, l/(float)layers);
				float nz = radius0 - radius1;
				for (std::size_t w = 0; w <= wedges; ++w)
				{
					float a = da*w + (l%2)*da*0.5f;
					pt = v4(cos(a) * r * xscale, sin(a) * r * yscale, z, 1.0f);
					nm = v4::Normal3(height * cos(a + da*0.5f) / xscale, height * sin(a + da*0.5f) / yscale ,nz ,0.0f);
					uv = v2(a / maths::tau, 1.0f - (z + height*0.5f) / height);
					SetPCNT(*v_out++, pt, cc(*col++), nm, uv);
				}
				if (l != layers) z += dz;
			}
			nm = v4ZAxis;
			for (std::size_t w = 0; w <= wedges; ++w) // Top face
			{
				float a = da*w + (layers%2)*da*0.5f;
				pt = v4(cos(a) * radius1 * xscale, sin(a) * radius1 * yscale, z, 1.0f);
				uv = v2(cos(a) * 0.5f + 0.5f, sin(a) * 0.5f + 0.5f);
				SetPCNT(*v_out++, pt, cc(*col++), nm, uv);
			}
			pt = v4(0 ,0 ,z ,1.0f);
			uv = v2(0.5, 0.5f);
			SetPCNT(*v_out++, pt, cc(*col++), nm, uv);

			// Faces
			ibase = 1;
			for (std::size_t w = 0; w != wedges; ++w) // Bottom face
			{
				*i_out++ = checked_cast<VIdx>(0);
				*i_out++ = checked_cast<VIdx>(ibase + w + 1);
				*i_out++ = checked_cast<VIdx>(ibase + w);
			}
			ibase += verts_per_layer;
			for (std::size_t l = 0; l != layers; ++l) // The walls
			{
				for (std::size_t w = 0; w != wedges; ++w)
				{
					*i_out++ = checked_cast<VIdx>(ibase + w);
					*i_out++ = checked_cast<VIdx>(ibase + w + 1);
					*i_out++ = checked_cast<VIdx>(ibase + w + verts_per_layer);
					*i_out++ = checked_cast<VIdx>(ibase + w + verts_per_layer);
					*i_out++ = checked_cast<VIdx>(ibase + w + 1);
					*i_out++ = checked_cast<VIdx>(ibase + w + verts_per_layer + 1);
				}
				ibase += verts_per_layer;
			}
			ibase += verts_per_layer;
			for (std::size_t w = 0; w != wedges; ++w) // Top face
			{
				*i_out++ = checked_cast<VIdx>(ibase + w);
				*i_out++ = checked_cast<VIdx>(ibase + w + 1);
				*i_out++ = checked_cast<VIdx>(last);
			}

			return props;
		}
	}
}
