//********************************
// Algorithm
//  Copyright (c) Rylogic Ltd 2006
//********************************
// Helper wrappers to 'std::algorithm'
#pragma once

#include <exception>
#include <algorithm>
#include <unordered_set>
#include <unordered_map>
#include <set>
#include <map>

namespace pr
{
	// Container traits
	#pragma region Container Traits
	namespace impl
	{
		template <typename T> struct std_container_traits
		{
			using value_type              = typename T::value_type;
			using const_iterator          = typename T::const_iterator;
			using iterator                = typename T::iterator;
			static bool const associative = false;
		};
		template <typename K, typename V> struct map_container_traits
		{
			using value_type = std::pair<K,V>;
			static bool const associative = true;
		};
		template <typename K> struct set_container_traits
		{
			using value_type = K;
			static bool const associative = true;
		};
	}

	template <typename T> struct container_traits
		:impl::std_container_traits<T>
	{};
	template <typename U> struct container_traits<U[]>
	{
		using value_type              = U;
		using const_iterator          = typename value_type const*;
		using iterator                = typename value_type*;
		static bool const associative = false;
	};
	template <typename K, typename V> struct container_traits<std::unordered_map<K,V>>
		:impl::map_container_traits<K,V>
	{};
	template <typename K, typename V> struct container_traits<std::map<K,V>>
		:impl::map_container_traits<K,V>
	{};
	template <typename K> struct container_traits<std::unordered_set<K>>
		:impl::set_container_traits<K>
	{};
	template <typename K> struct container_traits<std::set<K>>
		:impl::set_container_traits<K>
	{};

	static_assert(container_traits<std::vector<int>>::associative == false, "");
	static_assert(container_traits<std::unordered_set<int>>::associative == true, "");
	#pragma endregion

	// Return the length of a container or array
	template <typename TCont> size_t length(TCont const& cont)
	{
		return cont.size();
	}
	template <typename Type, size_t N> size_t length(Type const (&cont)[N])
	{
		return N;
	}

	// Return the minimum of a set of values
	template <typename Type> inline Type min(Type const& a) { return a; }
	template <typename Type, typename... Args> inline Type min(Type const& a, Args&&... args)
	{
		return std::min(a, pr::min(std::forward<Args>(args)...));
	}

	// Return the maximum of a set of values
	template <typename Type> inline Type max(Type const& a) { return a; }
	template <typename Type, typename... Args> inline Type max(Type const& a, Args&&... args)
	{
		return std::max(a, pr::max(std::forward<Args>(args)...));
	}

	// Returns true if all in 'cont' pass 'pred'
	template <typename TCont, typename Pred> inline bool all(TCont const& cont, Pred pred)
	{
		return std::all_of(std::begin(cont), std::end(cont), pred);
	}

	// True if 'func' returns true for any element in 'cont'
	template <typename TCont, typename TElem = typename container_traits<TCont>::value_type> inline bool contains(TCont const& cont, TElem const& item)
	{
		auto iter = std::find(std::begin(cont), std::end(cont), item);
		return iter != std::end(cont);
	}
	template <typename TIter, typename TElem = typename std::iterator_traits<TIter>::value_type> inline bool contains(TIter beg, TIter end, TElem const& item)
	{
		auto iter = std::find(beg, end, item);
		return iter != end;
	}
	template <typename TKey, typename TValue> inline bool contains(std::unordered_map<TKey, TValue> const& cont, TKey const& item)
	{
		return cont.count(item) != 0;
	}
	template <typename TKey, typename TValue> inline bool contains(std::map<TKey, TValue> const& cont, TKey const& item)
	{
		return cont.count(item) != 0;
	}
	template <typename TKey> inline bool contains(std::unordered_set<TKey> const& cont, TKey const& item)
	{
		return cont.count(item) != 0;
	}
	template <typename TKey> inline bool contains(std::set<TKey> const& cont, TKey const& item)
	{
		return cont.count(item) != 0;
	}

	// Return the lower bound
	template <typename TCont, typename TValue> inline auto lower_bound(TCont& cont, TValue const& val) -> decltype(std::begin(cont))
	{
		return std::lower_bound(std::begin(cont), std::end(cont), val);
	}
	template <typename TCont, typename TValue, typename TFunc> inline auto lower_bound(TCont& cont, TValue const& val, TFunc pred) -> decltype(std::begin(cont))
	{
		return std::lower_bound(std::begin(cont), std::end(cont), val, pred);
	}

	// Return the upper bound
	template <typename TCont, typename TValue> inline auto upper_bound(TCont& cont, TValue const& val) -> decltype(std::begin(cont))
	{
		return std::upper_bound(std::begin(cont), std::end(cont), val);
	}
	template <typename TCont, typename TValue, typename TFunc> inline auto upper_bound(TCont& cont, TValue const& val, TFunc pred) -> decltype(std::begin(cont))
	{
		return std::upper_bound(std::begin(cont), std::end(cont), val, pred);
	}

	// Returns a pair [lower, upper) for the range equal to 'val'
	template <typename TCont, typename TValue> inline auto equal_range(TCont& cont, TValue const& val) -> decltype(std::make_pair(std::begin(cont), std::begin(cont)))
	{
		return std::equal_range(std::begin(cont), std::end(cont), val);
	}
	template <typename TCont, typename TValue, typename TFunc> inline auto equal_range(TCont& cont, TValue const& val, TFunc pred) -> decltype(std::make_pair(std::begin(cont), std::begin(cont)))
	{
		return std::equal_range(std::begin(cont), std::end(cont), val, pred);
	}

	// Returns a pair [lower, upper) for the range from 'first' to 'last'
	template <typename TCont, typename TValue> inline auto find_bounds(TCont& cont, TValue const& first, TValue const& last) -> decltype(std::make_pair(std::begin(cont), std::begin(cont)))
	{
		assert(first <= last);
		auto lwr = std::lower_bound(std::begin(cont), std::end(cont), first);
		auto upr = std::upper_bound(lwr, std::end(cont), last);
		return std::make_pair(lwr, upr);
	}
	
	// True if 'func' returns true for any element in 'cont'
	template <typename TCont, typename TFunc> inline bool contains_if(TCont const& cont, TFunc pred)
	{
		auto iter = std::find_if(std::begin(cont), std::end(cont), pred);
		return iter != std::end(cont);
	}

	// Return the index of the first occurrence to satisfy 'pred'
	template <typename TCont, typename TFunc> inline int index_if(TCont const& cont, TFunc pred)
	{
		auto iter = std::find_if(std::begin(cont), std::end(cont), pred);
		return static_cast<int>(std::distance(std::begin(cont), iter));
	}

	// Return the index of 'val' in 'cont' or cont.size() if not found
	template <typename TCont, typename TValue> inline int index_of(TCont const& cont, TValue const& val)
	{
		auto iter = std::find(std::begin(cont), std::end(cont), val);
		return static_cast<int>(std::distance(std::begin(cont), iter));
	}

	// Return the first element in 'cont' that matches 'pred' or nullptr
	template <typename TCont, typename TValue> inline auto find(TCont& cont, TValue const& val) -> decltype(std::begin(cont))
	{
		return std::find(std::begin(cont), std::end(cont), val);
	}

	// Returns the first element in 'cont' that matches 'pred' or end iterator
	template <typename TCont, typename Pred> inline auto find_if(TCont& cont, Pred pred) -> decltype(std::begin(cont))
	{
		return std::find_if(std::begin(cont), std::end(cont), pred);
	}

	// Return the first element in 'cont' that matches 'pred' or throw
	template <typename TCont, typename Pred> inline auto get_if(TCont& cont, Pred pred) -> decltype(*std::begin(cont))
	{
		auto iter = find_if(cont, pred);
		if (iter == std::end(cont)) throw std::exception("get_if() - no match found");
		return *iter;
	}

	// Return the first element in 'cont' that matches 'pred' or return a default element instance
	template <typename TCont, typename Pred, typename Elem = typename container_traits<TCont>::value_type> inline Elem first_or_default(TCont& cont, Pred pred, Elem def = Elem())
	{
		auto iter = find_if(cont, pred);
		return iter != std::end(cont) ? *iter : def;
	}

	// Return the number of elements in 'cont' that match 'pred'
	template <typename TCont, typename Pred> inline typename TCont::difference_type count_if(TCont const& cont, Pred pred)
	{
		return std::count_if(std::begin(cont), std::end(cont), pred);
	}

	// Insert 'val' into 'cont' if there is no element in 'cont' equal to 'val'
	// 'cont' is assumed to be ordered. Returns true if 'val' was added to 'cont'
	template <typename TCont, typename TValue, typename OrderPred> inline bool insert_unique(TCont& cont, TValue const& val, OrderPred order_pred)
	{
		// '*iter' will be >= 'val'. So if 'val' is not < '*iter' it must be equal
		auto iter = std::lower_bound(std::begin(cont), std::end(cont), val, order_pred);
		if (iter != std::end(cont) && !order_pred(val,*iter)) return false;
		cont.insert(iter, val);
		return true;
	}
	template <typename TCont, typename TValue> inline bool insert_unique(TCont& cont, TValue const& val)
	{
		// '*iter' will be >= 'val'. So if 'val' is not < '*iter' it must be equal
		auto iter = std::lower_bound(std::begin(cont), std::end(cont), val);
		if (iter != std::end(cont) && *iter == val) return false;
		cont.insert(iter, val);
		return true;
	}

	// Insert 'val' into 'cont' in order
	template <typename TCont, typename Value, typename OrderPred> inline void insert_ordered(TCont& cont, Value const& val, OrderPred order_pred)
	{
		auto iter = std::lower_bound(std::begin(cont), std::end(cont), val, order_pred);
		cont.insert(iter, val);
	}
	template <typename TCont, typename Value> inline void insert_ordered(TCont& cont, Value const& val)
	{
		auto iter = std::lower_bound(std::begin(cont), std::end(cont), val);
		cont.insert(iter, val);
	}

	// Erase 'where' from 'cont'
	template <typename TCont> inline void erase(TCont& cont, typename container_traits<TCont>::const_iterator where)
	{
		if (where == std::end(cont)) return;
		cont.erase(where);
	}
	template <typename TCont> inline void erase_unstable(TCont& cont, typename container_traits<TCont>::iterator where)
	{
		if (where == std::end(cont)) return;
		*where = std::move(cont.back());
		cont.pop_back();
	}

	// Erase the first instance of 'value' from 'cont'
	template <typename TCont> inline void erase(TCont& cont, typename container_traits<TCont>::value_type const& value)
	{
		auto iter = std::remove(std::begin(cont), std::end(cont), value);
		erase(cont, iter);
	}
	template <typename TCont> inline void erase_unstable(TCont& cont, typename container_traits<TCont>::value_type const& value)
	{
		auto iter = std::find(std::begin(cont), std::end(cont), value);
		erase_unstable(cont, iter);
	}

	// Erase the first match to 'pred' from 'cont'
	template <typename TCont, typename Pred> inline void erase_first(TCont& cont, Pred pred)
	{
		auto iter = std::find_if(std::begin(cont), std::end(cont), pred);
		erase(cont, iter);
	}
	template <typename TCont, typename Pred> inline void erase_first_unstable(TCont& cont, Pred pred)
	{
		auto iter = std::find_if(std::begin(cont), std::end(cont), pred);
		erase_unstable(cont, iter);
	}

	// Erase all elements from 'cont' that match 'pred'
	template <typename TCont, typename Pred> inline void erase_if(TCont& cont, Pred pred)
	{
		if constexpr (container_traits<TCont>::associative)
		{
			// 'std::remove_if' does not work on associative containers because they cannot be reordered.
			for (auto i = std::begin(cont); i != std::end(cont);)
			{
				if (!pred(*i)) ++i;
				else i = cont.erase(i);
			}
		}
		else
		{
			auto end = std::remove_if(std::begin(cont), std::end(cont), pred);
			cont.erase(end, std::end(cont));
		}
	}
	template <typename TCont, typename Pred> inline void erase_if_unstable(TCont& cont, Pred pred)
	{
		if constexpr (container_traits<TCont>::associative)
		{
			erase_if(cont, pred);
		}
		else
		{
			auto beg = std::begin(cont);
			auto end = std::end(cont);
			for (;beg != end;)
			{
				if (!pred(*beg)) ++beg;
				else *beg = std::move(*(--end));
			}
			cont.erase(end, std::end(cont));
		}
	}

	// Sort a container
	template <typename TCont> inline void sort(TCont& cont)
	{
		std::sort(std::begin(cont), std::end(cont));
	}
	template <typename TCont, typename Pred> inline void sort(TCont& cont, Pred pred)
	{
		std::sort(std::begin(cont), std::end(cont), pred);
	}

	// Transform
	template <typename TCont, typename Func> inline void transform(TCont& cont, Func func)
	{
		std::transform(std::begin(cont), std::end(cont), std::begin(cont), func);
	}

	// Set intersection/union
	template <typename TContOut, typename TCont0, typename TCont1> TContOut set_intersection(TCont0 const& cont0, TCont1 const& cont1)
	{
		if constexpr (container_traits<TCont0>::associative)
		{
			return impl::set_intersection_associative<TContOut>(cont0, cont1);
		}
		else if constexpr (container_traits<TCont1>::associative)
		{
			return impl::set_intersection_associative<TContOut>(cont1, cont0);
		}
		else
		{
			return impl::set_intersection_ordered<TContOut>(cont0, cont1);
		}
	}
	template <typename TContOut, typename TCont0, typename TCont1> TContOut set_union(TCont0 const& cont0, TCont1 const& cont1)
	{
		if constexpr (container_traits<TCont0>::associative)
		{
			return impl::set_union_associative<TContOut>(cont0, cont1);
		}
		else if constexpr (container_traits<TCont1>::associative)
		{
			return impl::set_union_associative<TContOut>(cont1, cont0);
		}
		else
		{
			return impl::set_union_ordered<TContOut>(cont0, cont1);
		}
	}
	namespace impl
	{
		template <typename TContOut, typename TOrderedCont0, typename TOrderedCont1> TContOut set_intersection_ordered(TOrderedCont0 const& cont0, TOrderedCont1 const& cont1)
		{
			TContOut out;
			std::set_intersection(
				std::begin(cont0), std::end(cont0),
				std::begin(cont1), std::end(cont1),
				std::back_inserter(out));
			return std::move(out);
		}
		template <typename TContOut, typename TOrderedCont0, typename TOrderedCont1> TContOut set_union_ordered(TOrderedCont0 const& cont0, TOrderedCont1 const& cont1)
		{
			TContOut out;
			std::set_union(
				std::begin(cont0), std::end(cont0),
				std::begin(cont1), std::end(cont1),
				std::back_inserter(out));
			return std::move(out);
		}
		template <typename TContOut, typename TAssocCont0, typename TCont1> TContOut set_intersection_associative(TAssocCont0 const& cont0, TCont1 const& cont1)
		{
			TContOut out;
			auto write = std::back_inserter(out);
			for (auto& item : cont1)
			{
				if (!contains(cont0, item)) continue;
				*write++ = item;
			}
			return std::move(out);
		}
		template <typename TContOut, typename TAssocCont0, typename TCont1> TContOut set_union_associative(TAssocCont0 const& cont0, TCont1 const& cont1)
		{
			TContOut out(std::begin(cont0), std::end(cont0));
			auto write = std::back_inserter(out);
			for (auto& item : cont1)
			{
				if (contains(cont0, item)) continue;
				*write++ = item;
			}
			return std::move(out);
		}
	}
}

