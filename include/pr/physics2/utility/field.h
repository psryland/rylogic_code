//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once
#include "pr/common/space_filling.h"
#include "pr/physics2/forward.h"

namespace pr::physics
{
	// Predefined kernels
	namespace field
	{
		// A smoothing kernel defines the influence of a point at a given distance, the rate of
		// change of that influence at a given distance, and the maximum range of the influence.
		template <typename T> concept KernalType = requires(T k, float distance)
		{
			{ k.Radius() } -> std::convertible_to<float>;
			{ k.InfluenceAt(distance) } -> std::convertible_to<float>;
			{ k.dInfluenceAt(distance) } -> std::convertible_to<float>;
		};

		struct KernelSpike2D
		{
			// Smoothing curve is: Influence(r) = (m_radius - distance)^2, where distance < m_radius
			// To make the smoothing kernel independent of the radius, we need to normalize by the volume
			// under the curve. Volume is found by taking the double integral (in polar coordinates) of Influence(r)
			// between theta=[0,tau) and r=[0,m_radius). This gives: Volume = (1/12) * tau * m_radius^4

			float const m_radius;
			float const m_volume;

			KernelSpike2D(float radius)
				: m_radius(radius)
				, m_volume((1.0f / 12.0f) * maths::tauf * Pow(radius, 4.0f))
			{}
			float Radius() const
			{
				return m_radius;
			}
			float InfluenceAt(float distance) const
			{
				if (distance >= m_radius) return 0.0f;
				return Sqr(m_radius - distance) / m_volume;
			}
			float dInfluenceAt(float distance) const
			{
				if (distance >= m_radius) return 0.0f;
				return 2.0f * (m_radius - distance) / m_volume;
			}
		};

		struct KernelSpike3D
		{
			// Smoothing curve is: Influence(r) = (m_radius - distance)^2, where distance < m_radius
			// To make the smoothing kernel independent of the radius, we need to normalize by the hyper-volume
			// "under" the curve. Volume is found by taking the triple integral (in polar coordinates) of Influence(r)
			// between theta=[0,tau), phi=[0,pi), and r=[0,m_radius). This gives: Volume = (1/15) * tau * m_radius^5

			float const m_radius;
			float const m_volume;

			KernelSpike3D(float radius)
				: m_radius(radius)
				, m_volume((1.0f / 15.0f) * maths::tauf * Pow(radius, 5.0f))
			{}
			float Radius() const
			{
				return m_radius;
			}
			float InfluenceAt(float distance) const
			{
				if (distance >= m_radius) return 0.0f;
				return Sqr(m_radius - distance) / m_volume;
			}
			float dInfluenceAt(float distance) const
			{
				if (distance >= m_radius) return 0.0f;
				return 2.0f * (m_radius - distance) / m_volume;
			}
		};
	}

	// 'Dim' is the dimension of the field (2 or 3)
	// 'TProperty' is the type of the property stored at each point in space (scalar or vector)
	// 'Resolution' is the distance between adjacent grid points (in meters)
	// 'Kernel' is the smoothing kernel used to calculate the influence of a point at a given distance
	template <int Dim, typename TProperty, float Resolution, field::KernalType Kernel>
	struct Field
	{
		// Notes:
		//  - A field has a property (Scalar or Vector) defined at every point in space.
		//  - Storing every point in space is inefficient, quantize to a 2D/3D grid.
		//  - Map 2D/3D grid points to 1D using the Z-Order curve.
		//  - Only need to store grid points with values.
		//  - Inserting a value means updating values within the kernel radius.
		//  - Returning a value means calculating the value at a point from points within the kernel radius.
		//  - For efficiency, the kernel radius should span ~12 grid points (2D) or ~20 grid points (3D).
		//  - There are no range limits on the field, but the more points stored, the more memory consumed.
	
		static_assert(Dim == 2 || Dim == 3, "Invalid dimension");

		using IVec = std::conditional_t<Dim == 2, iv2, iv4>;
		using FVec = std::conditional_t<Dim == 2, v2, v4>;
		using Map = std::unordered_map<int, int>;
		using Prop = pr::vector<TProperty>;

		Map m_map;                 // This maps a Z-Order index to an index in 'm_field'
		Prop m_field;              // Stores the field values
		Kernel m_kernel;           // The smoothing kernel
		TProperty m_default_value; // The value for non-stored field points.

		Field(Kernel kernal = {}, TProperty default_value = {})
			: m_map()
			, m_field()
			, m_kernel(kernal)
			, m_default_value(default_value)
		{}

		// Reset the field to the default value
		void Reset()
		{
			m_map.resize(0);
			m_field.resize(0);
		}

		// Loops over the grid points that fall within the kernel radius of 'position'.
		void EnumSamplePoints(FVec position, std::function<TProperty(FVec)> value)
		{
			auto const radius = m_kernel.Radius();
			auto const radius_sq = Sqr(radius);
			auto const range = (radius * 2 + 1);

			// Lower and upper bounds in grid space
			auto min = GridPoint(position - FVec(radius));
			auto max = GridPoint(position + FVec(radius));

			// Loop over X,Y,(Z) grid points
			#pragma omp parallel for
			for (int i = 0; i != Dim * range; ++i)
			{
				auto pt = min + ZOrder<Dim>(i);

				// Skip points outside the kernel radius
				if (LengthSq(pt - position) > radius_sq)
					continue;
				
				value(pt);
			}
		}

		// Get the field value at a point in space.
		TProperty ValueAt(FVec position) const
		{
			// Loop over the sample points within the kernel radius of 'position'
			auto value = m_default_value;
			EnumSamplePoints(position, [&]()
			{
				// Accumulate the weighted value of each sample point
			//	value += m_kernel.InfluenceAt(Length(position - sample_point)) * m_field[sample_point];
			});


			//// Convert the grid point to a Z-Order index
			//auto index = ZOrder(grid_point);

			//// Lookup the index in the map
			//auto it = m_map.find(index);
			//return it != m_map.end() ? m_field[it->second] : m_default_value;
		}

		// Set a field value at a point in space.
		void ValueAt(FVec position, TProperty value)
		{
			// Convert the floating point position to an infinite grid position
			auto grid_point = To<IVec>(position / (1 << Bits));

			// Convert the grid point to a Z-Order index
			auto index = ZOrder(grid_point);

			// Lookup the index in the map
			auto it = m_map.find(index);
			if (it != m_map.end())
			{
				m_field[it->second] = value;
			}
			else
			{
				m_map[index] = m_field.size();
				m_field.push_back(value);
			}
		}

		// Set a field value at a volume in space.
		// 'value' is a function that calculates the value at a given point in space
		// 'position + radius' define the volume of space affected by 'value'.
		void ValueAt(FVec position, float radius, std::function<TProperty(FVec)> value)
		{
			// Convert the floating point position to an infinite grid position
			auto grid_point = To<IVec>(position / (1 << Bits));

			// Convert the grid point to a Z-Order index
			auto index = ZOrder(grid_point);

			// Lookup the index in the map
			auto it = m_map.find(index);
			if (it != m_map.end())
			{
				m_field[it->second] = value(position);
			}
			else
			{
				m_map[index] = m_field.size();
				m_field.push_back(value(position));
			}
		}

	private:

		// Convert a grid point to a Z-Order index
		int64_t ZOrder(IVec grid_point) const
		{
			if constexpr (Dim == 2)
			{
				return space_filling::ZOrder2D(grid_point);
			}
			else if constexpr (Dim == 3)
			{
				return space_filling::ZOrder3D(grid_point);
			}
			else
			{
				static_assert(pr::dependent_false<TProperty>, "Invalid dimension");
			}
		}

		// Convert a Z-Order index to a grid point
		IVec ZOrder(int64_t index) const
		{
			if constexpr (Dim == 2)
			{
				return space_filling::ZOrder2D(index);
			}
			else if constexpr (Dim == 3)
			{
				return space_filling::ZOrder3D(index);
			}
			else
			{
				static_assert(pr::dependent_false<TProperty>, "Invalid dimension");
			}
		}

		// Convert a floating point position to a grid position
		IVec GridPoint(FVec position) const
		{
			return To<IVec>(position / Resolution);
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/ldraw/ldr_helper.h"

namespace pr::physics
{
	PRUnitTest(FieldTests)
	{
		// 2D Vector field, 10cm grid
		Field<2, v2, 0.01f, field::KernelSpike2D> field(0.05f);

		/*
		// Draw the vector field
		ldr::Builder builder;
		auto& points = ldr.Point("Field", 0xFF00FF00).size(0.1f);
		for (int y = 0; y != 100; ++y)
		{
			for (int x = 0; x != 100; ++x)
			{
				auto p = v2(x, y) * 0.01f;
				auto v = field.ValueAt(p);
				points.pt(p, p + v);
			}
		}
		builder.Write("E:/Dump/Field.ldr");
		//*/

		/*
		// Insert some vectors into the field
		std::default_random_engine rng;
		for (int i = 0; i != 1000; ++i)
		{
			auto v = v2::Random(rng, v2(-1000), v2(+1000));
			
			// Insert an "explosion" at 'v'
			field.ValueAt(v, [&](v2 p)
			{
				auto d = Length(p - v);
				return v2::Normalize(p - v) * (1.0f - d);
			});
		}

		auto v0 = field.get(v2(5, 5));
		PR_CHECK(v0 == v2::Zero(), true);
		//*/
	}
}
#endif
