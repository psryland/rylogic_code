//*****************************************
// KD Tree
//  Copyright (c) March 2005 Paul Ryland
//*****************************************
// Updated 2023
#pragma once
#include <concepts>
#include <algorithm>

namespace pr::kdtree
{
	// Concept for a value to pivot on
	template <typename S> concept ValueType = requires(S s)
	{
		{ s - s };
		{ s * s };
		{ s < s };
	};

	// Concept for 'int GetAxis(Item const& item)' function
	template <typename T, typename Item> concept GetAxisFunc = requires(T t)
	{
		{ t(std::declval<Item const&>()) } -> std::convertible_to<int>;
	};

	// Concept for a 'void SetAxis(Item& item, int axis)' function
	template <typename T, typename Item> concept SetAxisFunc = requires(T t)
	{
		{ t(std::declval<Item&>(), std::declval<int>()) };
	};

	// Concept for a 'S GetValue(Item const& item, int axis)' function
	template <typename T, typename S, typename Item> concept GetValueFunc = requires(T t)
	{
		ValueType<S>;
		{ t(std::declval<Item const&>(), std::declval<int>()) } -> std::convertible_to<S>;
	};

	// Concept for a 'void Found(Item const&, S dist_sq)' function
	template <typename T, typename S, typename Item> concept FoundFunc = requires(T t)
	{
		ValueType<S>;
		{ t(std::declval<Item const&>(), std::declval<S>()) };
	};

	// Strategies for selecting the axis to split on
	enum class EStrategy
	{
		AxisByLevel,
		LongestAxis,
	};

	// Search a kdtree
	// 'get_value' = S GetValue(Item const& item, int axis)
	// 'get_axis' = int GetAxis(Item const& item)
	// 'found' = void Found(Item const&, S dist_sq)
	template <int Dim, ValueType S, typename Item, GetValueFunc<S, Item> GetValue, GetAxisFunc<Item> GetAxis, FoundFunc<S, Item> Found>
	void Find(std::span<Item const> items, S const (&search)[Dim], S radius, GetValue get_value, GetAxis get_axis, Found found)
	{
		struct L
		{
			S m_radius_sq;
			S const (&m_search)[Dim];
			GetValue m_get_value;
			GetAxis m_get_axis;
			Found m_found;

			// Emits an 'item' if it is within the search region.
			void AddIfInRegion(Item const& item)
			{
				// Find the squared distance from the search point to 'item'
				auto dist_sq = S{};
				for (int a = 0; a != Dim; ++a)
				{
					auto dist = m_get_value(item, a) - m_search[a];
					dist_sq += dist * dist;
				}

				// If within the search radius, add to the results
				if (dist_sq <= m_radius_sq)
				{
					m_found(item, dist_sq);
				}
			}

			// Find nodes within a spherical search region
			void DoFind(Item const* first, Item const* last)
			{
				if (first == last)
					return;

				auto split_point = first + (last - first) / 2;
				AddIfInRegion(*split_point);

				// Bottom of the tree? Time to leave
				if (last - first <= 1)
					return;

				// Get the axis to search on
				auto split_axis = m_get_axis(*split_point);
				auto split_value = m_get_value(*split_point, split_axis);

				// If the test point is to the left of the split point
				if (m_search[split_axis] < split_value)
				{
					// Search the left sub tree
					DoFind(first, split_point);
				
					// If the search area overlaps the split value, we need to search the right side too
					auto distance = split_value - m_search[split_axis];
					if (distance * distance < m_radius_sq)
						DoFind(split_point + 1, last);
				}
				// Otherwise, the test point is to the right of the split point
				else
				{
					// Search the right sub tree
					DoFind(split_point + 1, last);

					// If the search area overlaps the split value, we need to search the left side too
					auto distance = m_search[split_axis] - split_value;
					if (distance * distance < m_radius_sq)
						DoFind(first, split_point);
				}
			}
		};

		// Recursive search
		L x = { radius * radius, search, get_value, get_axis, found };
		x.DoFind(items.data(), items.data() + items.size());
	}

	// Build a kdtree
	// 'get_value' = S GetValue(Item const& item, int axis)
	// 'set_axis' = void SetAxis(Item& item, int axis)
	template <int Dim, ValueType S, typename Item, EStrategy SelectAxis, GetValueFunc<S, Item> GetValue, SetAxisFunc<Item> SetAxis>
	void Build(std::span<Item> items, GetValue get_value, SetAxis set_axis)
	{
		struct L
		{
			GetValue m_get_value;
			SetAxis m_set_axis;

			// Select the axis simply based on level
			int AxisByLevel(std::span<Item>, int level)
			{
				return level % Dim;
			}

			// Find the axis with the greatest range
			int LongestAxis(std::span<Item> items, int)
			{
				auto ptr = items.data();
				auto end = ptr + items.size();

				// Find the bounds over all axes
				S lower[Dim], upper[Dim];
				for (int a = 0; a != Dim; ++a)
				{
					lower[a] = m_get_value(*ptr, a);
					upper[a] = lower[a];
				}
				for (++ptr; ptr != end; ++ptr)
				{
					for (int a = 0; a != Dim; ++a)
					{
						auto value = m_get_value(*ptr, a);
						if (value < lower[a]) lower[a] = value;
						if (value > upper[a]) upper[a] = value;
					}
				}

				// Select the axis with the greatest range
				for (int a = 0; a != Dim; ++a)
					upper[a] -= lower[a];

				int largest = 0;
				for (int a = 1; a != Dim; ++a)
					largest = upper[a] > upper[largest] ? a : largest;

				return largest;
			}

			// Ensure that the element at the centre of the range has only values less than it on
			// the left and values greater or equal than it on the right, where the values are the
			// component of the axis to split on
			Item* MedianSplit(std::span<Item> items, int split_axis)
			{
				auto split_point = items.data() + items.size() / 2;
				std::nth_element(items.data(), split_point, items.data() + items.size(), [=](auto const& lhs, auto const& rhs)
				{
					return m_get_value(lhs, split_axis) < m_get_value(rhs, split_axis);
				});
				return split_point;
			}

			// Sort values based on the median value of the longest axis
			void DoBuild(std::span<Item> items, int level)
			{
				if (items.size() <= 1)
					return;

				// Set the axis to split on.
				int split_axis = 0;
				if constexpr (SelectAxis == EStrategy::AxisByLevel)
					split_axis = AxisByLevel(items, level);
				if constexpr (SelectAxis == EStrategy::LongestAxis)
					split_axis = LongestAxis(items, level);

				auto split_point = MedianSplit(items, split_axis);
				m_set_axis(*split_point, split_axis);

				// Construct recursively
				DoBuild({ items.data(), split_point }, level + 1);
				DoBuild({ split_point + 1, items.data() + items.size() }, level + 1);
			};
		};

		// Recursive build
		L x = { get_value, set_axis };
		x.DoBuild(items, 0);
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/ldraw/ldr_helper.h"

namespace pr::container
{
	PRUnitTest(KDTreeTests)
	{
		std::vector<v2> points(100);
		std::bitset<100> pivots;

		auto GetValue = [](v2 const& p, int a) { return p[a]; };
		auto GetAxis = [&](v2 const& p) { return pivots[&p - &points[0]]; };
		auto SetAxis = [&](v2& p, int a) { pivots[&p - &points[0]] = a != 0; };
		auto IsFound = [&](v2 const& p, std::set<v2> const& results) { return results.find(p) != std::end(results); };
		auto Search = [&](v2 search_point, float search_radius)
		{
			std::set<v2> results;
			kdtree::Find<2, float, v2>(points, search_point.arr, search_radius, GetValue, GetAxis, [&](v2 const& p, float d_sq)
			{
				results.insert(p);
				PR_CHECK(Sqrt(d_sq) < search_radius + maths::tinyf, true);
				PR_CHECK(Length(search_point - p) < search_radius + maths::tinyf, true);
			});
			return results;
		};
		auto CheckResults = [&](v2 search_point, float search_radius, std::set<v2> const& results)
		{
			// Check all points are found or not found appropriately
			for (auto const& p : points)
			{
				auto sep = Length(search_point - p);
				if (IsFound(p, results))
					PR_CHECK(sep < search_radius + maths::tinyf, true);
				else
					PR_CHECK(sep > search_radius - maths::tinyf, true);
			}
		};
		auto LdrDump = [&](v2 search_point, float search_radius, std::set<v2> const& results)
		{
			ldr::Builder L;
			L.Circle("search", 0x8000FF00).radius(search_radius).pos(v4(search_point, 0, 1));
			for (auto const& p : points)
			{
				auto colour = IsFound(p, results) ? 0xFFFF0000 : 0xFF0000FF;
				L.Circle("pt", colour).radius(0.1f).solid().pos(v4(p, 0, 1));
			}
			L.Write("E:/Dump/kdtree.ldr");
		};

		// Normal case
		{
			// Create a regular grid of points
			for (int i = 0; i != isize(points); ++i)
				points[i] = v2(s_cast<float>(i % 10), s_cast<float>(i / 10));

			// Randomise the order of the points
			std::default_random_engine rng;
			std::shuffle(std::begin(points), std::end(points), rng);

			// Build the tree
			kdtree::Build<2, float, v2, kdtree::EStrategy::LongestAxis>(points, GetValue, SetAxis);

			v2 const search_point = { 2, 1 };
			float const search_radius = 3.0f;
			auto results = Search(search_point, search_radius);
			LdrDump(search_point, search_radius, results);
			CheckResults(search_point, search_radius, results);
		}

		// Degenerate case
		{
			// Test some pathological cases
			for (auto& pt : points)
				pt.y = 0;

			// Build the tree
			kdtree::Build<2, float, v2, kdtree::EStrategy::AxisByLevel>(points, GetValue, SetAxis);

			v2 const search_point = { 2, 1 };
			float const search_radius = 3.0f;
			auto results = Search(search_point, search_radius);
			LdrDump(search_point, search_radius, results);
			CheckResults(search_point, search_radius, results);
		}

		// Degenerate case
		{
			// Test some pathological cases
			for (auto& pt : points)
				pt = v2::Zero();

			// Build the tree
			kdtree::Build<2, float, v2, kdtree::EStrategy::AxisByLevel>(points, GetValue, SetAxis);

			v2 const search_point = { 2, 1 };
			float const search_radius = 3.0f;
			auto results = Search(search_point, search_radius);
			LdrDump(search_point, search_radius, results);
			CheckResults(search_point, search_radius, results);
		}
	}
}
#endif
