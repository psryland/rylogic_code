//*****************************************
// KD Tree
//  Copyright (c) March 2005 Paul Ryland
//*****************************************
// Updated 2023
#pragma once
#include <span>
#include <concepts>
#include <algorithm>
#include <limits>

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

	// KDTree methods
	template <int Dim, typename Item, ValueType S = float>
	struct KdTree
	{
		using EStrategy = kdtree::EStrategy;
		static constexpr int Dimensions = Dim;

		struct Neighbour
		{
			Item const* item;
			S dist_sq;
		};
		struct Pair
		{
			Item const* item0;
			Item const* item1;
			S dist_sq;

			friend bool operator == (Pair const& lhs, Pair const& rhs)
			{
				return
					(lhs.item0 == rhs.item0 && lhs.item1 == rhs.item1) ||
					(lhs.item0 == rhs.item1 && lhs.item1 == rhs.item0);
			}
			friend bool operator != (Pair const& lhs, Pair const& rhs)
			{
				return !(lhs == rhs);
			}
		};

		// Build a kdtree
		// 'get_value' = S GetValue(Item const& item, int axis)
		// 'set_axis' = void SetAxis(Item& item, int axis)
		template <EStrategy SelectAxis, GetValueFunc<S, Item> GetValue, SetAxisFunc<Item> SetAxis>
		static void Build(std::span<Item> items, GetValue get_value, SetAxis set_axis)
		{
			struct L
			{
				GetValue m_get_value;
				SetAxis m_set_axis;

				// Select the axis simply based on level
				int AxisByLevel(std::span<Item const>, int level)
				{
					return level % Dim;
				}

				// Find the axis with the greatest range
				int LongestAxis(std::span<Item const> items, int)
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

		// Search a kdtree
		// 'get_value' = S GetValue(Item const& item, int axis)
		// 'get_axis' = int GetAxis(Item const& item)
		// 'found' = void Found(Item const&, S dist_sq)
		template <GetValueFunc<S, Item> GetValue, GetAxisFunc<Item> GetAxis, FoundFunc<S, Item> Found>
		static void Find(std::span<Item const> items, S const (&search)[Dim], S radius, GetValue get_value, GetAxis get_axis, Found found)
		{
			struct L
			{
				S m_radius_sq;
				S const (&m_search)[Dim];
				GetValue m_get_value;
				GetAxis m_get_axis;
				Found m_found;

				// Square a value
				S Sqr(S s) const
				{
					return s * s;
				}

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
						if (Sqr(distance) < m_radius_sq)
							DoFind(split_point + 1, last);
					}
					// Otherwise, the test point is to the right of the split point
					else
					{
						// Search the right sub tree
						DoFind(split_point + 1, last);

						// If the search area overlaps the split value, we need to search the left side too
						auto distance = m_search[split_axis] - split_value;
						if (Sqr(distance) < m_radius_sq)
							DoFind(first, split_point);
					}
				}
			};

			// Recursive search
			L x = { radius * radius, search, get_value, get_axis, found };
			x.DoFind(items.data(), items.data() + items.size());
		}

		// Search a kdtree for the 'N' nearest neighbours.
		// Neighbours are returned in order of distance from the search point.
		template <GetValueFunc<S, Item> GetValue, GetAxisFunc<Item> GetAxis>
		static void FindNearest(std::span<Item const> items, S const (&search)[Dim], std::span<Neighbour> nearest_out, GetValue get_value, GetAxis get_axis)
		{
			struct L
			{
				std::span<Neighbour> m_nearest;
				S const (&m_search)[Dim];
				GetValue m_get_value;
				GetAxis m_get_axis;
				int m_count;

				// Square a value
				S Sqr(S s) const
				{
					return s * s;
				}

				// Return the upper bound on distance to the 'N'th nearest
				S LeastNearestSq() const
				{
					return m_count == std::ssize(m_nearest) ? m_nearest[m_count - 1].dist_sq : std::numeric_limits<S>::max();
				}

				// Track the distance at which there are 'N' closer items
				void TrackNearest(Item const& item)
				{
					// Find the squared distance from the search point to 'item'
					auto dist_sq = S{};
					for (int a = 0; a != Dim; ++a)
						dist_sq += Sqr(m_get_value(item, a) - m_search[a]);

					// If we have less than 'N' nearest, add it
					if (m_count != std::ssize(m_nearest))
						m_nearest[m_count++] = { &item, dist_sq };

					// Otherwise, if it is closer than the furthest, replace it
					else if (dist_sq < m_nearest[m_count-1].dist_sq)
						m_nearest[m_count-1] = { &item, dist_sq };

					// Sort the nearest list
					std::sort(m_nearest.data(), m_nearest.data() + m_count, [](auto const& lhs, auto const& rhs) { return lhs.dist_sq < rhs.dist_sq; });
				}

				// Find nodes within a spherical search region
				void DoFind(Item const* first, Item const* last)
				{
					if (first == last)
						return;

					auto split_point = first + (last - first) / 2;
					TrackNearest(*split_point);

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
				
						// If the other subtree is furtherer than the least-nearest, we don't need to search
						auto distance = split_value - m_search[split_axis];
						if (Sqr(distance) < LeastNearestSq())
							DoFind(split_point + 1, last);
					}
					// Otherwise, the test point is to the right of the split point
					else
					{
						// Search the right sub tree
						DoFind(split_point + 1, last);

						// If the other subtree is furtherer than the least-nearest, we don't need to search
						auto distance = m_search[split_axis] - split_value;
						if (Sqr(distance) < LeastNearestSq())
							DoFind(first, split_point);
					}
				}
			};

			// Recursive search
			L x = { nearest_out, search, get_value, get_axis, 0 };
			x.DoFind(items.data(), items.data() + items.size());
		}

		// Find pairs of items that are the nearest to each other
		template <GetValueFunc<S, Item> GetValue, GetAxisFunc<Item> GetAxis>
		static void Closest(std::span<Item const> items, std::span<Pair> pairs_out, GetValue get_value, GetAxis get_axis)
		{
			struct L
			{
				std::span<Pair> m_pairs;
				GetValue m_get_value;
				GetAxis m_get_axis;
				int m_count;

				// Square a value
				S Sqr(S s) const
				{
					return s * s;
				}

				// Return the upper bound on distance to the 'N'th nearest
				S LeastNearestSq() const
				{
					return m_count == std::ssize(m_pairs) ? m_pairs[m_count - 1].dist_sq : std::numeric_limits<S>::max();
				}

				// Track the distance at which there are 'N' closer items
				void TrackNearest(Item const& lhs, Item const& rhs)
				{
					// Find the squared distance from the search point to 'item'
					S dist_sq = {};
					for (int a = 0; a != Dim; ++a)
						dist_sq += Sqr(m_get_value(lhs, a) - m_get_value(rhs, a));

					// If we have less than 'N' nearest, add it
					if (m_count != std::ssize(m_pairs))
					{
						m_pairs[m_count++] = { &lhs, &rhs, dist_sq };
					}

					// Otherwise, if it is closer than the furthest, replace it
					else if (dist_sq < m_pairs[m_count - 1].dist_sq)
					{
						m_pairs[m_count - 1] = { &lhs, &rhs, dist_sq };
						std::sort(m_pairs.data(), m_pairs.data() + m_count, [](auto const& lhs, auto const& rhs) { return lhs.dist_sq < rhs.dist_sq; });
					}
				}
				
				// Find the closest items to 'target'
				void FindClosest(Item const& target, Item const* first, Item const* last)
				{
					if (first == last)
						return;

					auto split_point = first + (last - first) / 2;

					// Only compare 'target' with items after 'target' in the collection
					if (&target < split_point)
						TrackNearest(target, *split_point);

					// Bottom of the tree? Time to leave
					if (last - first <= 1)
						return;

					// Get the axis to search on
					auto split_axis = m_get_axis(*split_point);
					auto split_value = m_get_value(*split_point, split_axis);
					auto search_value = m_get_value(target, split_axis);

					// 'target' is the test point.
					if (search_value < split_value)
					{
						// Search the left sub tree
						if (&target < split_point)
							FindClosest(target, first, split_point);

						// If the search area overlaps the split value, we need to search the right side too
						auto distance_sq = Sqr(split_value - search_value);
						if (&target < last && distance_sq < LeastNearestSq())
							FindClosest(target, split_point + 1, last);
					}
					// Otherwise, the test point is to the right of the split point
					else
					{
						// Search the right sub tree
						if (&target < last)
							FindClosest(target, split_point + 1, last);

						// If the search area overlaps the split value, we need to search the left side too
						auto distance_sq = Sqr(search_value - split_value);
						if (&target < split_point && distance_sq < LeastNearestSq())
							FindClosest(target, first, split_point);
					}
				}

				// Find nodes within the minimum separation
				void DoFind(Item const* first, Item const* last)
				{
					// The brute force method would be to compare the item with all other items (O(N^2)).
					// However, we can use the tree to reduce the number of comparisons by not searching trees
					// that can't be closer to the target than the least-nearest pair.
					for (auto target = first; target != last; ++target)
						FindClosest(*target, first, last);
				}
			};

			// Recursive search
			L x = { pairs_out, get_value, get_axis, 0 };
			x.DoFind(items.data(), items.data() + items.size());
		}

		#if 0 // Not yet working
		// Find pairs of items that are the farthest from each other
		template <GetValueFunc<S, Item> GetValue, GetAxisFunc<Item> GetAxis>
		static void Farthest(std::span<Item const> items, std::span<Pair> pairs_out, GetValue get_value, GetAxis get_axis)
		{
			struct L
			{
				struct Bound
				{
					S lower, upper;
					S size() const { return upper - lower; }
				};

				std::span<Pair> m_pairs;
				GetValue m_get_value;
				GetAxis m_get_axis;
				Bound m_bounds[Dim];
				int m_count;

				// Square a value
				S Sqr(S s) const
				{
					return s * s;
				}

				// Returns the lower bound on distance to the 'N'th farthest
				S LeastFarthestSq() const
				{
					return m_count == std::ssize(m_pairs) ? m_pairs[m_count - 1].dist_sq : 0;
				}

				// Returns the squared diameter of the bounds
				S BoundsSizeSq() const
				{
					S size_sq = {};
					for (int a = 0; a != Dim; ++a)
						size_sq += Sqr(m_bounds[a].size());

					return size_sq;
				}

				// Track the distance at which there are 'N' most separated items
				void TrackFarthest(Item const& lhs, Item const& rhs)
				{
					// Find the squared distance from the search point to 'item'
					S dist_sq = {};
					for (int a = 0; a != Dim; ++a)
						dist_sq += Sqr(m_get_value(lhs, a) - m_get_value(rhs, a));

					// If we have less than 'N' fartherest, add it
					if (m_count != std::ssize(m_pairs))
					{
						m_pairs[m_count++] = { &lhs, &rhs, dist_sq };
					}

					// Otherwise, if it is further than the closest, replace it
					else if (dist_sq > m_pairs[m_count - 1].dist_sq)
					{
						m_pairs[m_count - 1] = { &lhs, &rhs, dist_sq };
						std::sort(m_pairs.data(), m_pairs.data() + m_count, [](auto const& lhs, auto const& rhs) { return lhs.dist_sq > rhs.dist_sq; });
					}
				}
				
				// Find the farthest items to 'target'
				void FindFarthest(Item const& target, Item const* first, Item const* last)
				{
					if (first == last)
						return;

					auto split_point = first + (last - first) / 2;

					// Only compare 'target' with items after 'target' in the collection
					if (&target < split_point)
						TrackFarthest(target, *split_point);

					// Bottom of the tree? Time to leave
					if (last - first <= 1)
						return;

					// Get the axis to search on
					auto split_axis = m_get_axis(*split_point);
					auto split_value = m_get_value(*split_point, split_axis);
					//auto search_value = m_get_value(target, split_axis);
					//auto bounds_sq = BoundsSizeSq() - Sqr(m_bounds[split_axis].size());

					// Search the left sub tree
					if (&target < split_point)
					{
						//auto distance_sq = Sqr(std::max(split_value, search_value) - m_bounds[split_axis].lower);
						//if (bounds_sq + distance_sq > LeastFarthestSq())
						{
							std::swap(m_bounds[split_axis].upper, split_value);
							FindFarthest(target, first, split_point);
							std::swap(m_bounds[split_axis].upper, split_value);
						}
					}

					// Search the right sub tree
					if (&target < last)
					{
						//auto distance_sq = Sqr(m_bounds[split_axis].upper - std::min(split_value, search_value));
						//if (bounds_sq + distance_sq > LeastFarthestSq())
						{
							std::swap(m_bounds[split_axis].lower, split_value);
							FindFarthest(target, split_point + 1, last);
							std::swap(m_bounds[split_axis].lower, split_value);
						}
					}
				}

				// Find the data bounds
				void CalcDataBounds(Item const* first, Item const* last)
				{
					for (int a = 0; a != Dim; ++a)
					{
						m_bounds[a].lower = std::numeric_limits<S>::max();
						m_bounds[a].upper = std::numeric_limits<S>::lowest();
					}
					for (auto target = first; target != last; ++target)
					{
						for (int a = 0; a != Dim; ++a)
						{
							auto value = m_get_value(*target, a);
							if (value < m_bounds[a].lower) m_bounds[a].lower = value;
							if (value > m_bounds[a].upper) m_bounds[a].upper = value;
						}
					}
				}

				// Find nodes within the minimum separation
				void DoFind(Item const* first, Item const* last)
				{
					CalcDataBounds(first, last);

					// The brute force method would be to compare the item with all other items (O(N^2)).
					// However, we can use the tree to reduce the number of comparisons by not searching trees
					// that can't be further from the target than the least-farthest pair.
					for (auto target = first; target != last; ++target)
						FindFarthest(*target, first, last);
				}
			};

			// Recursive search
			L x = { pairs_out, get_value, get_axis, {}, 0 };
			x.DoFind(items.data(), items.data() + items.size());
		}
		#endif
};
}

#if PR_UNITTESTS
#include <bitset>
#include "pr/common/unittests.h"
#include "pr/ldraw/ldr_helper.h"

namespace pr::container
{
	PRUnitTest(KDTreeTests)
	{
		using KdTree = pr::kdtree::KdTree<2, v2, float>;

		std::vector<v2> points(100);
		std::bitset<100> pivots;

		auto GetValue = [](v2 const& p, int a) { return p[a]; };
		auto GetAxis = [&](v2 const& p) { return pivots[&p - &points[0]]; };
		auto SetAxis = [&](v2 const& p, int a) { pivots[&p - &points[0]] = a != 0; };
		auto IsFound = [&](v2 const& p, std::set<v2> const& results) { return results.find(p) != std::end(results); };

		// Degenerate case
		{
			v2 const search_point = { 2, 1 };
			float const search_radius = 3.0f;

			// Test some pathological cases
			for (auto& pt : points)
				pt.y = 0;

			// Build the tree
			KdTree::Build<kdtree::EStrategy::AxisByLevel>(points, GetValue, SetAxis);

			// Test search
			std::set<v2> results;
			KdTree::Find(points, search_point.arr, search_radius, GetValue, GetAxis, [&](v2 const& p, float d_sq)
			{
				results.insert(p);
				PR_EXPECT(Sqrt(d_sq) < search_radius + maths::tinyf);
				PR_EXPECT(Length(search_point - p) < search_radius + maths::tinyf);
			});

			// Check all points are found or not found appropriately
			for (auto const& p : points)
			{
				auto sep = Length(search_point - p);
				if (IsFound(p, results))
					PR_CHECK(sep < search_radius + maths::tinyf, true);
				else
					PR_CHECK(sep > search_radius - maths::tinyf, true);
			}
		}

		// Degenerate case
		{
			v2 const search_point = { 2, 1 };
			float const search_radius = 3.0f;

			// Test some pathological cases
			for (auto& pt : points)
				pt = v2::Zero();

			// Build the tree
			KdTree::Build<kdtree::EStrategy::AxisByLevel>(points, GetValue, SetAxis);

			// Test search
			std::set<v2> results;
			KdTree::Find(points, search_point.arr, search_radius, GetValue, GetAxis, [&](v2 const& p, float d_sq)
			{
				results.insert(p);
				PR_EXPECT(Sqrt(d_sq) < search_radius + maths::tinyf);
				PR_EXPECT(Length(search_point - p) < search_radius + maths::tinyf);
			});

			// Check all points are found or not found appropriately
			for (auto const& p : points)
			{
				auto sep = Length(search_point - p);
				if (IsFound(p, results))
					PR_CHECK(sep < search_radius + maths::tinyf, true);
				else
					PR_CHECK(sep > search_radius - maths::tinyf, true);
			}
		}

		// Normal case
		{
			v2 const search_point = { 4.3f, 6.4f };
			float const search_radius = 3.0f;

			// Create a regular grid of points
			for (int i = 0; i != isize(points); ++i)
				points[i] = v2(s_cast<float>(i % 10), s_cast<float>(i / 10));

			// Randomise the order of the points
			std::default_random_engine rng;
			std::shuffle(std::begin(points), std::end(points), rng);

			// Build the tree
			KdTree::Build<kdtree::EStrategy::LongestAxis>(points, GetValue, SetAxis);

			// Test search
			std::set<v2> results;
			KdTree::Find(points, search_point.arr, search_radius, GetValue, GetAxis, [&](v2 const& p, float d_sq)
			{
				results.insert(p);
				PR_EXPECT(Sqrt(d_sq) < search_radius + maths::tinyf);
				PR_EXPECT(Length(search_point - p) < search_radius + maths::tinyf);
			});

			// Searching for 'results.size()' nearest should return the same set
			std::vector<KdTree::Neighbour> nearest(isize(results));
			KdTree::FindNearest(points, search_point.arr, nearest, GetValue, GetAxis);
			for (auto& neighbour : nearest)
			{
				PR_EXPECT(results.contains(*neighbour.item));
			}

			// Expect the neighbours to be in order of distance
			for (int i = 1; i != isize(nearest); ++i)
			{
				PR_EXPECT(nearest[i - 1].dist_sq <= nearest[i].dist_sq);
			}

			// Check there aren't any closer points
			auto limit_sq = nearest.back().dist_sq;
			for (auto& point : points)
			{
				if (results.contains(point))
					continue;

				auto dist_sq = LengthSq(point - search_point);
				PR_EXPECT(dist_sq >= limit_sq);
			}

			// Check all points are found or not found appropriately
			for (auto const& p : points)
			{
				auto sep = Length(search_point - p);
				if (IsFound(p, results))
					PR_CHECK(sep < search_radius + maths::tinyf, true);
				else
					PR_CHECK(sep > search_radius - maths::tinyf, true);
			}

			#if 0
			{
				ldr::Builder builder;
				builder.Circle("search", 0x8000FF00).radius(search_radius).pos(v4(search_point, 0, 1));

				auto& ldr_points = builder.Point("Points").size(10.0f);
				for (auto const& p : points)
					ldr_points.pt(v4(p, 0.0f, 1), IsFound(p, results) ? 0xFFFF0000 : 0xFF0000FF);

				builder.Write("E:/Dump/kdtree.ldr");
			}
			#endif
		}

		// Closest
		{
			std::random_device rd;
			std::default_random_engine rng(rd());
			std::uniform_real_distribution dist_pos(0.0f, 1.0f);
			for (int i = 0; i != isize(points); ++i)
				points[i] = v2(dist_pos(rng), dist_pos(rng));

			// Build the tree
			KdTree::Build<kdtree::EStrategy::LongestAxis>(points, GetValue, SetAxis);

			// Test search
			std::vector<KdTree::Pair> pairs(10);
			KdTree::Closest(points, pairs, GetValue, GetAxis);

			// Check pairs are ordered by increasing separation
			for (int i = 1; i != isize(pairs); ++i)
			{
				PR_EXPECT(pairs[i - 1].dist_sq <= pairs[i + 0].dist_sq);
			}

			// Check no other pairs are closer
			for (auto& objA : points)
			{
				for (auto& objB : points)
				{
					if (&objA == &objB)
						continue;

					auto dist_sq = LengthSq(objA - objB);
					KdTree::Pair pair{ &objA, &objB, dist_sq };

					// If the pair are closer, they should be in the list
					if (dist_sq < pairs.back().dist_sq)
					{
						PR_EXPECT(std::ranges::find(pairs, pair) != std::end(pairs));
					}

					// If the pair are further, they should not be in the list
					if (dist_sq > pairs.back().dist_sq)
					{
						PR_EXPECT(std::ranges::find(pairs, pair) == std::end(pairs));
					}

					// Don't worry about equal, because the results may or may not include them
				}
			}

			// Show output
			#if	1
			{
				ldr::Builder builder;

				auto& ldr_points = builder.Point("Points").size(10.0f);
				for (auto const& p : points)
					ldr_points.pt(v4(p, 0.0f, 1), 0xFF0000FF);

				auto& ldr_lines = builder.Line("Closest", 0xFFFF0000);
				for (auto const& p : pairs)
				{
					auto p0 = v4(*p.item0, 0, 1);
					auto p1 = v4(*p.item1, 0, 1);

					ldr_lines.line(p0, p1);
					builder.Sphere("Found", 0x80FF0000).r(sqrt(p.dist_sq) * 0.5f).pos((p0 + p1) * 0.5f);
				}

				builder.Write("E:/Dump/kdtree.ldr");
			}
			#endif
		}

		// Farthest
		#if 0
		{
			std::random_device rd;
			std::default_random_engine rng(rd());
			std::uniform_real_distribution dist_pos(0.0f, 1.0f);
			for (int i = 0; i != isize(points); ++i)
				points[i] = v2(dist_pos(rng), dist_pos(rng));

			// Build the tree
			KdTree::Build<kdtree::EStrategy::LongestAxis>(points, GetValue, SetAxis);

			// Test search
			std::vector<KdTree::Pair> pairs(10);
			KdTree::Farthest(points, pairs, GetValue, GetAxis);

			// Check pairs are ordered by decreasing separation
			for (int i = 1; i != isize(pairs); ++i)
			{
				PR_EXPECT(pairs[i - 1].dist_sq >= pairs[i + 0].dist_sq);
			}

			// Check no other pairs are further
			for (auto& objA : points)
			{
				for (auto& objB : points)
				{
					if (&objA == &objB)
						continue;

					auto dist_sq = LengthSq(objA - objB);
					KdTree::Pair pair{ &objA, &objB, dist_sq };

					// If the pair are closer, they should not be in the list
					if (dist_sq < pairs.back().dist_sq)
					{
						auto is_found = std::ranges::find(pairs, pair) != std::end(pairs);
						PR_EXPECT(!is_found);
					}

					// If the pair are further, they should be in the list
					if (dist_sq > pairs.back().dist_sq)
					{
						auto is_found = std::ranges::find(pairs, pair) != std::end(pairs);
						PR_EXPECT(is_found);
					}

					// Don't worry about equal, because the results may or may not include them
				}
			}

			// Show output
			#if	0
			{
				ldr::Builder builder;

				auto& ldr_points = builder.Point("Points", 0xFF0000FF).size(10.0f);
				for (auto const& p : points)
					ldr_points.pt(v4(p, 0.0f, 1));

				auto& ldr_lines = builder.Line("Farthest", 0xFFFFFFFF);
				for (auto const& p : pairs)
				{
					auto p0 = v4(*p.item0, 0, 1);
					auto p1 = v4(*p.item1, 0, 1);

					ldr_lines.line(p0, p1);
				}

				builder.Write("E:/Dump/kdtree.ldr");
			}
			#endif
		}
		#endif
	}
}
#endif
