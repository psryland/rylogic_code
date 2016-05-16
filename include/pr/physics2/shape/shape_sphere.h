//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

// A sphere collision shape

#include "pr/physics2/forward.h"
#include "pr/physics2/shape/shape.h"

namespace pr
{
	namespace physics
	{
		struct ShapeSphere
		{
			Shape m_base;
			float m_radius;
			bool  m_hollow;

			ShapeSphere() = default;
			ShapeSphere(float radius, m4x4_cref shape_to_model = m4x4Identity, bool hollow = false, MaterialId material_id = 0, Shape::EFlags flags = Shape::EFlags::None)
				:m_base(EShape::Sphere, sizeof(ShapeSphere), shape_to_model, material_id, flags)
				,m_radius(radius)
				,m_hollow(hollow)
			{
				m_base.m_bbox = CalcBBox(*this);
			}
			operator Shape const&() const
			{
				return m_base;
			}
			operator Shape&()
			{
				return m_base;
			}
		};
		static_assert(is_shape<ShapeSphere>::value, "");

		// Return the bounding box for a sphere shape
		inline BBox& CalcBBox(ShapeSphere const& shape)
		{
			return BBox(v4Origin, v4(shape.m_radius, shape.m_radius, shape.m_radius, 0.0f));
		}

		// Return the mass properties
		inline MassProperties CalcMassProperties(ShapeSphere const& shape, float density)
		{
			// A solid sphere:  'Ixx = Iyy = Izz = (2/5)mr^2'
			// A hollow sphere: 'Ixx = Iyy = Izz = (2/3)mr^2'
			auto scale = shape.m_hollow ? (2.0f/3.0f) : (2.0f/5.0f);
			auto volume = (2.0f/3.0f) * maths::tau * shape.m_radius * shape.m_radius * shape.m_radius;

			MassProperties mp;
			mp.m_centre_of_mass        = v4Zero;
			mp.m_mass                  = volume * density;
			mp.m_os_inertia_tensor     = m3x4Identity;
			mp.m_os_inertia_tensor.x.x = scale * (shape.m_radius * shape.m_radius);
			mp.m_os_inertia_tensor.y.y = mp.m_os_inertia_tensor.x.x;
			mp.m_os_inertia_tensor.z.z = mp.m_os_inertia_tensor.x.x;
			return mp;
		}

		// Shift the centre of a sphere
		inline void ShiftCentre(ShapeSphere&, v4& shift)
		{
			assert("impossible to shift the centre of an implicit object" && FEql3(shift, v4Zero));
			(void)shift; 
		}

		// Return a support vertex for a sphere
		inline v4 SupportVertex(ShapeSphere const& shape, v4_cref direction, size_t, size_t& sup_vert_id)
		{
			// We need to quantise the normal otherwise the iterative algorithms perform badly
			auto dir = Normalise3(direction);

			// Generate an id for the vertex in this direction
			sup_vert_id =
				static_cast<size_t>((dir.x + 1.0f) * 0.5f * (1 << 4)) << 20 |
				static_cast<size_t>((dir.y + 1.0f) * 0.5f * (1 << 4)) << 10 |
				static_cast<size_t>((dir.z + 1.0f) * 0.5f * (1 << 4)) << 0;

			return dir * shape.m_radius + v4Origin;
		}

		// Find the nearest point and distance from a point to a shape. 'shape' and 'point' are in the same space
		inline void ClosestPoint(ShapeSphere const& shape, v4 const& point, float& distance, v4& closest)
		{
			distance  = Length3(point);
			closest   = ((shape.m_radius / distance) * point).w1();
			distance -= shape.m_radius;
		}
	}
}