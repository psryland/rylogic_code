//******************************************************************
// Shape2d
//  Copyright (c) Rylogic Ltd 2014
//******************************************************************

#pragma once

#include "pr/geometry/common.h"

namespace pr
{
	namespace geometry
	{
		// Circle/Ellipse *************************************************************************

		// Returns the number of verts and indices needed to hold geometry for an 'Ellipse'
		template <typename Tvr, typename Tir>
		void EllipseSize(bool solid, int facets, Tvr& vcount, Tir& icount)
		{
			facets = std::max(facets, 3);
			vcount = checked_cast<Tvr>(facets + (solid ? 1 : 0));
			icount = checked_cast<Tir>(solid ? 1 + 2*facets : facets + 1);
		}

		// Generate an ellipse shape
		// 'solid' - true = tristrip model, false = linestrip model
		template <typename TVertIter, typename TIdxIter>
		Props Ellipse(float dimx, float dimy, bool solid, int facets, Colour32 colour, TVertIter v_out, TIdxIter i_out)
		{
			typedef decltype(impl::remove_ref(*i_out)) VIdx;

			facets = std::max(facets, 3);

			Props props;
			props.m_geom = EGeom::Vert | EGeom::Colr | (solid ? EGeom::Norm : 0) | (solid ? EGeom::Tex0 : 0);
			props.m_bbox.set(pr::v4Origin, pr::v4::make(dimx, dimy, 0, 0));

			// Set Verts
			for (int i = 0; i != facets; ++i)
			{
				auto a = pr::maths::tau * i / facets;
				auto c = pr::Cos(a);
				auto s = pr::Sin(a);
				SetPCNT(*v_out++, pr::v4::make(dimx * c, dimy * s, 0, 1), colour, pr::v4ZAxis, pr::v2::make(0.5f*(c + 1), 0.5f*(1 - s)));
			}
			if (solid)
				SetPCNT(*v_out++, pr::v4Origin, colour, pr::v4ZAxis, pr::v2::make(0.5f, 0.5f));

			if (solid)
			{
				// Set faces
				*i_out++ = 0;
				for (int i = facets; i-- != 0;)
				{
					*i_out++ = checked_cast<VIdx>(facets);
					*i_out++ = checked_cast<VIdx>(i);
				}
			}
			else // border only
			{
				// Set edges
				for (int i = 0; i != facets; ++i)
					*i_out++ = checked_cast<VIdx>(i);
				*i_out++ = 0;
			}

			return props;
		}

		// Pie/Wedge ******************************************************************************

		// Returns the number of verts and indices needed to hold geometry for a 'Pie'
		template <typename Tvr, typename Tir>
		void PieSize(bool solid, float ang0, float ang1, int facets, Tvr& vcount, Tir& icount)
		{
			auto scale = abs(ang1 - ang0) / pr::maths::tau;
			facets = std::max(int(scale * facets + 0.5f), 3);
			vcount = checked_cast<Tvr>(2 * (facets + 1));
			icount = checked_cast<Tir>(solid ? 2 * (facets + 1) : 2 * facets + 3);
		}

		// Generate a pie/wedge shape
		// 'ang0','ang1' = start/end angle in radians
		// 'solid' - true = tristrip model, false = linestrip model
		// 'facets' - the number of facets for a complete ring, scaled to the actual ang0->ang1 range
		template <typename TVertIter, typename TIdxIter>
		Props Pie(float dimx, float dimy, float ang0, float ang1, float radius0, float radius1, bool solid, int facets, Colour32 colour, TVertIter v_out, TIdxIter i_out)
		{
			typedef decltype(impl::remove_ref(*i_out)) VIdx;

			auto scale = abs(ang1 - ang0) / pr::maths::tau;
			facets = std::max(int(scale * facets + 0.5f), 3);
			radius0 = std::max(0.0f, radius0);
			radius1 = std::max(radius0, radius1);
			
			Props props;
			props.m_geom = EGeom::Vert | EGeom::Colr | (solid ? EGeom::Norm : 0) | (solid ? EGeom::Tex0 : 0);

			// Bounding box
			auto bb = [&](v4 const& v) { pr::Encompass(props.m_bbox, v); return v; };

			// Tex coords
			auto tr0 = FEqlZero(radius1) ? 0.0f : radius0 / radius1;
			auto tr1 = 1.0f;

			// Set Verts
			for (int i = 0; i <= facets; ++i)
			{
				auto a = pr::Lerp(ang0, ang1, float(i) / facets);
				auto c = pr::Cos(a);
				auto s = pr::Sin(a);
				SetPCNT(*v_out++, bb(pr::v4::make(radius0 * dimx * c, radius0 * dimy * s, 0, 1)), colour, pr::v4ZAxis, pr::v2::make(0.5f + 0.5f*tr0*c, 0.5f - 0.5f*tr0*s));
				SetPCNT(*v_out++, bb(pr::v4::make(radius1 * dimx * c, radius1 * dimy * s, 0, 1)), colour, pr::v4ZAxis, pr::v2::make(0.5f + 0.5f*tr1*c, 0.5f - 0.5f*tr1*s));
			}

			if (solid)
			{
				// Set faces
				VIdx idx = 0;
				for (int i = 0; i <= facets; ++i)
				{
					*i_out++ = checked_cast<VIdx>(idx++);
					*i_out++ = checked_cast<VIdx>(idx++);
				}
			}
			else // border only
			{
				*i_out++ = 0;
				for (int i = 0; i != facets; ++i) { *i_out++ = checked_cast<VIdx>(1+2*i); }
				*i_out++ = 1;
				*i_out++ = 0;
				for (int i = facets; i-- != 0;  ) { *i_out++ = checked_cast<VIdx>(0+2*i); }
			}

			return props;
		}	

		// Rounded Rectangle **********************************************************************

		// Returns the number of verts and indices needed to hold geometry for a 'RoundedRectangle'
		template <typename Tvr, typename Tir>
		void RoundedRectangleSize(bool solid, float corner_radius, int facets, Tvr& vcount, Tir& icount)
		{
			auto verts_per_cnr = corner_radius != 0.0f ? std::max(facets / 4, 0) + 1 : 1;
			vcount = checked_cast<Tvr>(4 * verts_per_cnr);
			icount = checked_cast<Tir>(solid ? 4 * verts_per_cnr : 4 * verts_per_cnr + 1);
		}

		// Generate a Rectangle shape with rounded corners
		// 'solid' - true = tristrip model, false = linestrip model
		template <typename TVertIter, typename TIdxIter>
		Props RoundedRectangle(float dimx, float dimy, bool solid, float corner_radius, int facets, Colour32 colour, TVertIter v_out, TIdxIter i_out)
		{
			typedef decltype(impl::remove_ref(*i_out)) VIdx;

			Props props;
			props.m_geom = EGeom::Vert | EGeom::Colr | (solid ? EGeom::Norm : 0) | (solid ? EGeom::Tex0 : 0);
			props.m_bbox.set(pr::v4Origin, pr::v4::make(dimx, dimy, 0, 0));

			// Limit the rounding to half the smallest rectangle side length
			auto rad = corner_radius;
			if (rad > dimx) rad = dimx;
			if (rad > dimy) rad = dimy;
			auto verts_per_cnr = rad != 0.0f ? std::max(facets / 4, 0) + 1 : 1;

			// Texture coords
			auto tx = rad / (2 * dimx);
			auto ty = rad / (2 * dimy);
			auto t0 = 0.0000f;
			auto t1 = 0.9999f;

			if (solid)
			{
				// Set verts
				auto vb0 = v_out + verts_per_cnr * 0;
				auto vb1 = v_out + verts_per_cnr * 2;
				for (int i = 0; i != verts_per_cnr; ++i)
				{
					auto c = verts_per_cnr > 1 ? pr::Cos(pr::maths::tau_by_4 * i / (verts_per_cnr - 1)) : 0;
					auto s = verts_per_cnr > 1 ? pr::Sin(pr::maths::tau_by_4 * i / (verts_per_cnr - 1)) : 0;
					SetPCNT(*vb0++, pr::v4::make(-dimx + rad * (1 - c), +dimy - rad * (1 - s), 0, 1), colour, pr::v4ZAxis, pr::v2::make(t0 + (1 - c)*tx, t0 + (1 - s)*ty));
					SetPCNT(*vb0++, pr::v4::make(-dimx + rad * (1 - c), -dimy + rad * (1 - s), 0, 1), colour, pr::v4ZAxis, pr::v2::make(t0 + (1 - c)*tx, t1 - (1 - s)*ty));
					SetPCNT(*vb1++, pr::v4::make(+dimx - rad * (1 - s), +dimy - rad * (1 - c), 0, 1), colour, pr::v4ZAxis, pr::v2::make(t1 - (1 - s)*tx, t0 + (1 - c)*ty));
					SetPCNT(*vb1++, pr::v4::make(+dimx - rad * (1 - s), -dimy + rad * (1 - c), 0, 1), colour, pr::v4ZAxis, pr::v2::make(t1 - (1 - s)*tx, t1 - (1 - c)*ty));
				}
			}
			else // border only
			{
				// Set verts
				auto vb0 = v_out + verts_per_cnr * 0;
				auto vb1 = v_out + verts_per_cnr * 1;
				auto vb2 = v_out + verts_per_cnr * 2;
				auto vb3 = v_out + verts_per_cnr * 3;
				for (int i = 0; i != verts_per_cnr; ++i)
				{
					auto c = verts_per_cnr > 1 ? pr::Cos(pr::maths::tau_by_4 * i / (verts_per_cnr - 1)) : 0;
					auto s = verts_per_cnr > 1 ? pr::Sin(pr::maths::tau_by_4 * i / (verts_per_cnr - 1)) : 0;
					SetPCNT(*vb0++, pr::v4::make(-dimx + rad * (1 - c), -dimy + rad * (1 - s), 0, 1), colour, pr::v4ZAxis, pr::v2::make(t0 + (1 - c)*tx, t1 - (1 - s)*ty));
					SetPCNT(*vb1++, pr::v4::make(+dimx - rad * (1 - s), -dimy + rad * (1 - c), 0, 1), colour, pr::v4ZAxis, pr::v2::make(t1 - (1 - s)*tx, t1 - (1 - c)*ty));
					SetPCNT(*vb2++, pr::v4::make(+dimx - rad * (1 - c), +dimy - rad * (1 - s), 0, 1), colour, pr::v4ZAxis, pr::v2::make(t1 - (1 - c)*tx, t0 + (1 - s)*ty));
					SetPCNT(*vb3++, pr::v4::make(-dimx + rad * (1 - s), +dimy - rad * (1 - c), 0, 1), colour, pr::v4ZAxis, pr::v2::make(t0 + (1 - s)*tx, t0 + (1 - c)*ty));
				}
			}

			// Set faces/edges
			for (int i = 0; i != verts_per_cnr*4; ++i)
				*i_out++ = checked_cast<VIdx>(i);
			if (!solid)
				*i_out++ = 0;

			return props;
		}
	}
}