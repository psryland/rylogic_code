//********************************
// Algorithm
//  Copyright (c) Rylogic Ltd 2006
//********************************
// Helper wrappers to std::algorithim

#pragma once

#include <exception>
#include <algorithm>

namespace pr
{
	// Return the length of a container or array
	template <typename TCont> size_t length(TCont const& cont)
	{
		return cont.size();
	}
	template <typename Type, size_t N> size_t length(Type const (&cont)[N])
	{
		return N;
	}

	// True if 'func' returns true for any element in 'cont'
	template <typename TCont> inline bool contains(TCont const& cont, decltype(cont[0]) const& item)
	{
		auto iter = std::find(std::begin(cont), std::end(cont), item);
		return iter != std::end(cont);
	}

	// True if 'func' returns true for any element in 'cont'
	template <typename TCont, typename TFunc> inline bool contains_if(TCont const& cont, TFunc func)
	{
		auto iter = std::find_if(std::begin(cont), std::end(cont), func);
		return iter != std::end(cont);
	}

	// Return the first element in 'cont' that matches 'pred' or nullptr
	template <typename TCont, typename Pred> inline auto find_if(TCont& cont, Pred pred) -> decltype(&cont[0])
	{
		auto iter = std::find_if(std::begin(cont), std::end(cont), pred);
		if (iter == std::end(cont)) return nullptr;
		return &*iter;
	}

	// Return the first element in 'cont' that matches 'pred' or throw
	template <typename TCont, typename Pred> inline auto get_if(TCont& cont, Pred pred) -> decltype(cont[0])
	{
		auto ptr = find_if(cont, pred);
		if (ptr == nullptr) throw std::exception("get_if() - no match found");
		return *ptr;
	}

	// Insert 'val' into 'cont' if there is no element in 'cont' equal to 'val'
	// 'cont' is assumed to be ordered. Returns true if 'val' was added to 'cont'
	template <typename TCont, typename Value, typename OrderPred> inline bool insert_unique(TCont& cont, Value const& val, OrderPred order_pred)
	{
		// '*iter' will be >= 'val'. So if 'val' is not < '*iter' it must be equal
		auto iter = std::lower_bound(std::begin(cont), std::end(cont), val, order_pred);
		if (iter != std::end(cont) && !order_pred(val,*iter)) return false;
		cont.insert(iter, val);
		return true;
	}
	template <typename TCont, typename Value> inline bool insert_unique(TCont& cont, Value const& val)
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

	// Erase the first match to pred from 'cont'
	template <typename TCont, typename Pred> inline void erase_first(TCont& cont, Pred pred)
	{
		auto iter = std::find_if(std::begin(cont), std::end(cont), pred);
		if (iter == std::end(cont)) return;
		cont.erase(iter);
	}

	// Erase all elements from 'cont' that match 'pred'
	template <typename TCont, typename Pred> inline void erase_if(TCont& cont, Pred pred)
	{
		auto end = std::remove_if(std::begin(cont), std::end(cont), pred);
		cont.erase(end, std::end(cont));
	}
}