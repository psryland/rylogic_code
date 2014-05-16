//*****************************************
// KD Tree
//  Copyright (c) March 2005 Paul Ryland
//*****************************************
//
// Example use:
//
// struct AccessorFunctions
// {
//		static kdtree::AxisType	GetAxis  (const Thing& elem)						{ return elem.m_axis; }
//		static void				SetAxis  (      Thing& elem, kdtree::AxisType axis)	{ elem.m_axis = axis; }
//		static float			GetValue (const Thing& elem, kdtree::AxisType axis)	{ return elem.m_position[axis]; }
//		static void				AddResult(const Thing& elem, float distSq)			{ ... }
//	};
//
//	// Build a kd tree
//	inline void BuildKDTree()
//	{
//		std::vector<Thing>::iterator first = m_things.begin();
//		std::vector<Thing>::iterator last  = m_things.end();
//		AccessorFunctions func;
//		kdtree::Build<3>(first, last, func);
//	}
//
//	// Find things in a kd tree
//	inline void Find()
//	{
//		std::vector<Thing>::const_iterator first = m_thing.begin();
//		std::vector<Thing>::const_iterator last  = m_thing.end();
//		MAv4 where = (...);
//		float radius = (...);
//		
//		AccessorFunctions results;
//		kdtree::Search<3>	search;
//		search.m_where[0]	= where[0];
//		search.m_where[1]	= where[2];
//		search.m_where[2]	= where[2];
//		search.m_radius		= radius;
//		kdtree::Find<3>(first, last, search, results);
//	}

#ifndef PR_KD_TREE_H
#define PR_KD_TREE_H

#include <iterator>
#include <algorithm>

namespace pr
{
	namespace kdtree
	{
		typedef unsigned int AxisType;

		// The 'AccessorFunctions' object must be a type containing these member functions:
		// struct AccessorFunctions
		// {
		//		AxisType	GetAxis  (const T& elem);					// Only needed for "Find"
		//		void		SetAxis  (      T& elem, AxisType axis);	// Only needed for "Build"
		//		float		GetValue (const T& elem, AxisType axis);
		//		void		AddResult(const T& elem, float distSq);		// Only needed for "Find"
		//	};

		// Search parameters
		template <unsigned int dimensions>
		struct Search
		{
			float	m_where[dimensions];
			float	m_radius;
		};

		// KD tree building function
		template <unsigned int dimension, typename Iter, typename AccessorFunctions>
		void Build(Iter first, Iter last, AccessorFunctions& func);
		
		// Find
		template <unsigned int dimensions, typename Iter, typename AccessorFunctions>
		void Find(Iter first, Iter last, const Search<dimensions>& search, AccessorFunctions& func);

		// Implementation **********************************
		namespace impl
		{
			// Find in Area Implementation **********************************
			template <unsigned int dimensions>
			struct Search
			{
				float	m_where[dimensions];
				float	m_radius;
				float	m_radiusSq;
			};

			// Calls the client if 'elem' is within the search region.
			template <unsigned int dimensions, typename Iter, typename AccessorFunctions>
			void AddIfInRegion(Iter elem_iter, const Search<dimensions>& search, AccessorFunctions& func)
			{
				(void)(func); // Prevent "unreferenced" compile warning
				float distSq = 0;
				for( AxisType a = 0; a < dimensions; ++a )
				{
					float dist = func.GetValue(*elem_iter, a) - search.m_where[a];
					distSq += dist * dist;
				}
				if( distSq <= search.m_radiusSq )
				{
					func.AddResult(*elem_iter, distSq);
				}
			}

			// Find nodes within a region
			template <unsigned int dimensions, typename Iter, typename AccessorFunctions>
			void Find(Iter first, Iter last, const Search<dimensions>& search, AccessorFunctions& func)
			{
				if( first == last ) return;

				Iter split_point = first + (last - first) / 2;
				AddIfInRegion(split_point, search, func);
					
				// Bottom of the tree? Time to leave
				if( (last - first) <= 1 ) return;
				
				AxisType	split_axis	= func.GetAxis (*split_point);
				float		split_value = func.GetValue(*split_point, split_axis);

				// If the test point is to the right of the split point
				if( search.m_where[split_axis] > split_value )
				{
					Iter right = split_point; ++right;
					Find(right, last, search, func);

					float distance = search.m_where[split_axis] - split_value;
					if( distance - search.m_radius < 0.0f )
						Find(first, split_point, search, func);
				}

				// Otherwise the test point is to the left of the split point
				else
				{
					Find(first, split_point, search, func);

					float distance = split_value - search.m_where[split_axis];
					if( distance - search.m_radius < 0.0f )
					{
						Iter right = split_point; ++right;
						Find(right, last, search, func);
					}
				}
			}

			// Build Implementation **********************************
			// Find the axis with the greatest range
			template <unsigned int dimensions, typename Iter, typename AccessorFunctions>
			AxisType LongestAxis(Iter first, Iter last, AccessorFunctions& func)
			{
				func; // Prevent weird "unreferenced formal parameter" warning
				float lower[dimensions];
				float upper[dimensions];
				for( AxisType a = 0; a != dimensions; ++a )
				{
					lower[a] = func.GetValue(*first, a);
					upper[a] = lower[a];
				}
				for( ++first; first != last; ++first )
				{
					for( AxisType a = 0; a != dimensions; ++a )
					{
						float value = func.GetValue(*first, a);
						lower[a] = std::min(lower[a], value);
						upper[a] = std::max(upper[a], value);
					}
				}
				AxisType largest = 0;
				float largest_range = upper[0] - lower[0];
				for( AxisType a = 1; a != dimensions; ++a )
				{
					float range = upper[a] - lower[a];
					if( range > largest_range )
					{
						largest_range = range;
						largest = a;
					}
				}
				return largest;
			}

			// Predicate for nth element sort
			template <typename AccessorFunctions>
			struct LessThanPred
			{
				template <typename T> bool operator () (const T& a, const T& b) const { return m_func->GetValue(a, m_axis) < m_func->GetValue(b, m_axis); }
				AxisType			m_axis;
				AccessorFunctions*	m_func;
			};

			// Ensure that the element at the centre of the range has only values less than it on
			// the left and values greater or equal than it on the right, where the values are the
			// component of the axis to split on
			template <unsigned int dimensions, typename Iter, typename AccessorFunctions>
			Iter MedianSplit(Iter first, Iter last, AxisType split_axis, AccessorFunctions& func)
			{
				Iter split_point = first + (last - first) / 2;
				LessThanPred<AccessorFunctions> pred = { split_axis, &func };
				std::nth_element(first, split_point, last, pred);
				return split_point;
			}

		}//namespace impl

		// Build a kdtree
		template <unsigned int dimensions, typename Iter, typename AccessorFunctions>
		void Build(Iter first, Iter last, AccessorFunctions& func)
		{
			if( last - first <= 1 ) return;

			AxisType	split_axis	= impl::LongestAxis<dimensions>(first, last, func);
			Iter		split_point	= impl::MedianSplit<dimensions>(first, last, split_axis, func);

			func.SetAxis(*split_point, split_axis);

			Build<dimensions>(first, split_point, func);
			++split_point;
			Build<dimensions>(split_point, last, func);
		}

		// Find in area
		template <unsigned int dimensions, typename Iter, typename AccessorFunctions>
		void Find(Iter first, Iter last, const Search<dimensions>& search, AccessorFunctions& func)
		{
			impl::Search<dimensions> impl_search;
			for( AxisType a = 0; a < dimensions; ++a )	{ impl_search.m_where[a] = search.m_where[a]; }
			impl_search.m_radius	= search.m_radius;
			impl_search.m_radiusSq	= search.m_radius * search.m_radius;

			impl::Find(first, last, impl_search, func);
		}

	}//namespace kdtree
}//namespace pr

#endif//PR_KD_TREE_H
