//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

#include "pr/physics2/forward.h"
#include "pr/physics2/shape/mass.h"

namespace pr
{
	namespace physics
	{
		// Calculate the mass properties of a shape
		template <typename = void> inline MassProperties CalcMassProperties(Shape const& shape, float density)
		{
			switch (shape.m_type)
			{
			default: assert("Unknown primitive type" && false); return MassProperties();
			case EShape::Sphere: return CalcMassProperties(shape_cast<ShapeSphere>(shape), density);
			case EShape::Box:    return CalcMassProperties(shape_cast<ShapeBox>   (shape), density);
			}
		}

		// Return the inertia tensor for the triangle
		inline Inertia CalcInertiaTensor(ShapeTriangle const& shape)
		{
			m3x4 inertia = m3x4Zero;
			for (int i = 0; i != 3; ++i)
			{
				auto& vert = shape.m_v[i];
				inertia.x.x += Sqr(vert.y) + Sqr(vert.z);
				inertia.y.y += Sqr(vert.z) + Sqr(vert.x);
				inertia.z.z += Sqr(vert.x) + Sqr(vert.y);
				inertia.x.y += vert.x * vert.y;
				inertia.x.z += vert.x * vert.z;
				inertia.y.z += vert.y * vert.z;
			}
			inertia.x.y = -inertia.x.y;
			inertia.x.z = -inertia.x.y;
			inertia.y.z = -inertia.x.y;
			inertia.y.x = inertia.x.y;
			inertia.z.x = inertia.x.z;
			inertia.z.y = inertia.y.z;
			return Inertia(inertia);
		}

		// Returns the unit inertia tensor for the polytope (i.e. assumes mass = 1.0).
		// Ensure the polytope is in it's final space before calculating its inertia.
		// Calling 'ShiftCentre' invalidates the inertia matrix.
		inline Inertia CalcInertiaTensor(ShapePolytope const& shape)
		{
			auto volume   = 0.0f;   // Technically this variable accumulates the volume times 6
			auto diagonal = v4Zero; // Accumulate matrix main diagonals [x*x, y*y, z*z]
			auto off_diag = v4Zero; // Accumulate matrix off-diagonals  [y*z, x*z, x*y]
			for (ShapePolyFace const *f = shape.face_beg(), *fend = shape.face_end(); f != fend; ++f)
			{
				auto& a = shape.vertex(f->m_index[0]);
				auto& b = shape.vertex(f->m_index[1]);
				auto& c = shape.vertex(f->m_index[2]);
				auto vol_x6 = Triple3(a, b, c); // Triple product is volume x 6
				volume += vol_x6;

				for (int i = 0, j = 1, k = 2; i != 3; ++i, (++j) %= 3, (++k) %= 3)
				{
					diagonal[i] += (
						a[i] * b[i] +
						b[i] * c[i] +
						c[i] * a[i] +
						a[i] * a[i] +
						b[i] * b[i] +
						c[i] * c[i]
						) * vol_x6; // Divide by 60.0f later
					off_diag[i] += (
						a[j] * b[k] +
						b[j] * c[k] +
						c[j] * a[k] +
						a[j] * c[k] +
						b[j] * a[k] +
						c[j] * b[k] +
						a[j] * a[k] * 2.0f +
						b[j] * b[k] * 2.0f +
						c[j] * c[k] * 2.0f
						) * vol_x6; // Divide by 120.0f later
				}
			}

			// If the polytope is degenerate, use the weighted average vertex positions
			if (FEql(volume, 0))
			{
				auto centre = v4Zero;
				for (v4 const *v = shape.vert_beg(), *vend = shape.vert_end(); v != vend; ++v) centre += *v;
				return Inertia::PointAt(centre);
			}

			// Divide by total volume
			volume   /= 6.0f;
			diagonal /= volume * 60.0f;
			off_diag /= volume * 120.0f;
			return Inertia(m3x4(
				v4(diagonal.y + diagonal.z , -off_diag.z             , -off_diag.y           ,0),
				v4(-off_diag.z             , diagonal.x + diagonal.z , -off_diag.x           ,0),
				v4(-off_diag.y             , -off_diag.x             , diagonal.x+diagonal.y ,0)));
		}

		// Return the mass properties
		inline MassProperties CalcMassProperties(ShapeSphere const& shape, float density)
		{
			// A solid sphere:  'Ixx = Iyy = Izz = (2/5)mr^2'
			// A hollow sphere: 'Ixx = Iyy = Izz = (2/3)mr^2'
			auto scale = shape.m_hollow ? (2.0f/3.0f) : (2.0f/5.0f);
			auto volume = (2.0f/3.0f) * maths::tau * shape.m_radius * shape.m_radius * shape.m_radius;

			MassProperties mp;
			mp.m_centre_of_mass    = v4Zero;
			mp.m_mass              = volume * density;
			mp.m_os_inertia_tensor = Inertia(scale * Sqr(shape.m_radius));
			return mp;
		}

		// Return the mass properties
		inline MassProperties CalcMassProperties(ShapeBox const& shape, float density)
		{
			auto volume = 8.0f * shape.m_radius.x * shape.m_radius.y * shape.m_radius.z;

			MassProperties mp;
			mp.m_centre_of_mass    = v4Zero;
			mp.m_mass              = volume * density;
			mp.m_os_inertia_tensor = Inertia((1.0f / 3.0f) * (Sqr(shape.m_radius.y) + Sqr(shape.m_radius.z))); // (1/12)m(Y^2 + Z^2)
			return mp;
		}

		// Return the mass properties
		inline MassProperties CalcMassProperties(ShapeTriangle const& shape, float density)
		{
			MassProperties mp;
			mp.m_centre_of_mass = (1.0f / 3.0f) * (shape.m_v.x + shape.m_v.y + shape.m_v.z).w0();
			mp.m_mass = Length3(Cross3(shape.m_v.y-shape.m_v.x, shape.m_v.z-shape.m_v.y)) * 0.5f * density;
			mp.m_os_inertia_tensor = CalcInertiaTensor(shape);
			return mp;
		}

		// Return mass properties for the polytope
		inline MassProperties CalcMassProperties(ShapePolytope const& shape, float density)
		{
			MassProperties mp;
			mp.m_centre_of_mass    = CalcCentreOfMass(shape);
			mp.m_mass              = CalcVolume(shape) * density;
			mp.m_os_inertia_tensor = CalcInertiaTensor(shape);
			return mp;
		}
	}
}
