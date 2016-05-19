//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

// A box collision shape

#include "pr/physics2/forward.h"
#include "pr/physics2/shape/shape.h"

namespace pr
{
	namespace physics
	{
		struct ShapeBox
		{
			Shape m_base;
			v4    m_radius;

			ShapeBox() = default;
			ShapeBox(v4_cref dim, m4x4_cref shape_to_model = m4x4Identity, MaterialId material_id = 0, Shape::EFlags flags = Shape::EFlags::None)
				:m_base(EShape::Box, sizeof(ShapeBox), shape_to_model, material_id, flags)
				,m_radius(dim * 0.5f)
			{
				assert(dim.w == 0.0f);
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
		static_assert(is_shape<ShapeBox>::value, "");

		// Return the bounding box for a box shape
		inline BBox CalcBBox(ShapeBox const& shape)
		{
			return BBox(v4Origin, shape.m_radius);
		}

		// Return the mass properties
		inline MassProperties CalcMassProperties(ShapeBox const& shape, float density)
		{
			auto volume = 8.0f * shape.m_radius.x * shape.m_radius.y * shape.m_radius.z;

			MassProperties mp;
			mp.m_centre_of_mass        = v4Zero;
			mp.m_mass                  = volume * density;
			mp.m_os_inertia_tensor     = m3x4Identity;
			mp.m_os_inertia_tensor.x.x = (1.0f / 3.0f) * (shape.m_radius.y * shape.m_radius.y + shape.m_radius.z * shape.m_radius.z); // (1/12)m(Y^2 + Z^2)
			mp.m_os_inertia_tensor.y.y = (1.0f / 3.0f) * (shape.m_radius.x * shape.m_radius.x + shape.m_radius.z * shape.m_radius.z); // (1/12)m(X^2 + Z^2)
			mp.m_os_inertia_tensor.z.z = (1.0f / 3.0f) * (shape.m_radius.y * shape.m_radius.y + shape.m_radius.x * shape.m_radius.x); // (1/12)m(Y^2 + Z^2)
			return mp;
		}

		// Shift the centre of a box shape
		inline void ShiftCentre(ShapeBox&, v4& shift)
		{
			assert("impossible to shift the centre of an implicit object" && FEql3(shift, v4Zero));
			(void)shift; 
		}

		// Return a support vertex for a box shape
		inline v4 SupportVertex(ShapeBox const& shape, v4_cref direction, size_t, size_t& sup_vert_id)
		{
			int sign_x = (direction.x > 0.0f);
			int sign_y = (direction.y > 0.0f);
			int sign_z = (direction.z > 0.0f);

			sup_vert_id = (sign_z << 2) | (sign_y << 1) | (sign_x);
			return v4(
				(2.0f * sign_x - 1.0f) * shape.m_radius.x,
				(2.0f * sign_y - 1.0f) * shape.m_radius.y,
				(2.0f * sign_z - 1.0f) * shape.m_radius.z,
				1.0f);
		}

		// Returns the closest point on 'shape' to 'point'. 'shape' and 'point' are in the same space
		inline void ClosestPoint(ShapeBox const& shape, v4_cref point, float& distance, v4& closest)
		{
			closest = point;
			distance = 0.0f;
			for (int i = 0; i != 3; ++i)
			{
				if (point[i] > shape.m_radius[i])
				{
					distance += Sqr(point[i] - shape.m_radius[i]);
					closest[i] = shape.m_radius[i];
				}
				else if (point[i] < -shape.m_radius[i])
				{
					distance += Sqr(point[i] + shape.m_radius[i]);
					closest[i] = -shape.m_radius[i];
				}
			}
			distance = Sqrt(distance);
		}
	}
}