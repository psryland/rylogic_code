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
	template <class T> struct container_traits
	{
		using value_type = typename T::value_type;
	};
	template <typename U> struct container_traits<U[]>
	{
		using value_type = U;
	};

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
	template <typename TCont, typename Pred> inline auto find_or_default(TCont& cont, Pred pred) -> decltype(*std::begin(cont))
	{
		using Elem = std::remove_cv_t<std::remove_reference_t<decltype(*std::begin(cont))>>;

		auto iter = find_if(cont, pred);
		if (iter == std::end(cont)) return Elem();
		return *iter;
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

	// Erase the first instance of 'value' from 'cont'
	template <typename TCont, typename Value = TCont::value_type> inline void erase(TCont& cont, Value const& value)
	{
		auto iter = std::remove(std::begin(cont), std::end(cont), value);
		if (iter == std::end(cont)) return;
		cont.erase(iter);
	}
	template <typename TCont, typename Value = TCont::value_type> inline void erase_unstable(TCont& cont, Value const& value)
	{
		auto iter = std::find(std::begin(cont), std::end(cont), value);
		if (iter == std::end(cont)) return;
		*iter = std::move(cont.back());
		cont.pop_back();
	}

	// Erase the first match to 'pred' from 'cont'
	template <typename TCont, typename Pred> inline void erase_first(TCont& cont, Pred pred)
	{
		auto iter = std::find_if(std::begin(cont), std::end(cont), pred);
		if (iter == std::end(cont)) return;
		cont.erase(iter);
	}
	template <typename TCont, typename Pred> inline void erase_first_unstable(TCont& cont, Pred pred)
	{
		auto iter = std::find_if(std::begin(cont), std::end(cont), pred);
		if (iter == std::end(cont)) return;
		*iter = std::move(cont.back());
		cont.pop_back();
	}

	// Erase all elements from 'cont' that match 'pred'
	template <typename TCont, typename Pred> inline void erase_if(TCont& cont, Pred pred)
	{
		auto end = std::remove_if(std::begin(cont), std::end(cont), pred);
		cont.erase(end, std::end(cont));
	}
	template <typename TCont, typename Pred> inline void erase_if_unstable(TCont& cont, Pred pred)
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
	template <typename TType, typename Pred> inline void erase_if(std::set<TType>& cont, Pred pred)
	{
		impl::associative_erase_if(cont, pred);
	}
	template <typename TType, typename Pred> inline void erase_if(std::unordered_set<TType>& cont, Pred pred)
	{
		impl::associative_erase_if(cont, pred);
	}
	template <typename TKey, typename TValue, typename Pred> inline void erase_if(std::unordered_map<TKey,TValue>& cont, Pred pred)
	{
		impl::associative_erase_if(cont, pred);
	}
	template <typename TKey, typename TValue, typename Pred> inline void erase_if(std::map<TKey,TValue>& cont, Pred pred)
	{
		impl::associative_erase_if(cont, pred);
	}
	namespace impl
	{
		template <typename TAssocCont, typename Pred> inline void associative_erase_if(TAssocCont& cont, Pred pred)
		{
			// 'std::remove_if' does not work on associative containers because they cannot be reordered.
			for (auto i = std::begin(cont);;)
			{
				i = std::find_if(i, std::end(cont), pred);
				if (i == std::end(cont)) break;
				i = cont.erase(i);
			}
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
		TContOut out;
		std::set_intersection(
			std::begin(cont0), std::end(cont0),
			std::begin(cont1), std::end(cont1),
			std::back_inserter(out));
		return std::move(out);
	}
	template <typename TContOut, typename TCont0, typename TCont1> TContOut set_union(TCont0 const& cont0, TCont1 const& cont1)
	{
		TContOut out;
		std::set_union(
			std::begin(cont0), std::end(cont0),
			std::begin(cont1), std::end(cont1),
			std::back_inserter(out));
		return std::move(out);
	}
}

