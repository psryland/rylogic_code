//******************************************
// Container Functions
//  Copyright (c) March 2008 Paul Ryland
//******************************************
#pragma once

#include <vector>
#include <algorithm>

namespace pr
{
	namespace cont
	{
		// Return the length of a container or array
		template <typename Cont> size_t Length(Cont const& cont)
		{
			return cont.size();
		}
		template <typename Type, size_t N> size_t Length(Type const (&cont)[N])
		{
			return N;
		}
	
		// Insert 'val' into 'cont' if there is no element in 'cont' equal to 'val'
		// 'cont' is assumed to be ordered. Returns true if 'val' was added to 'cont'
		template <typename Cont, typename Value, typename OrderPred> bool InsertUnique(Cont& cont, Value const& val, OrderPred order_pred)
		{
			// '*iter' will be >= 'val'. So if 'val' is not < '*iter' it must be equal
			Cont::iterator iter = std::lower_bound(cont.begin(), cont.end(), val, order_pred);
			if (iter != cont.end() && !order_pred(val,*iter)) return false;
			cont.insert(iter, val);
			return true;
		}
		template <typename Cont, typename Value> bool InsertUnique(Cont& cont, Value const& val)
		{
			Cont::iterator iter = std::lower_bound(cont.begin(), cont.end(), val);
			if (iter != cont.end() && *iter == val) return false;
			cont.insert(iter, val);
			return true;
		}
	
		// Insert 'val' into 'cont' in order
		template <typename Cont, typename Value, typename OrderPred> void InsertOrdered(Cont& cont, Value const& val, OrderPred order_pred)
		{
			Cont::iterator iter = std::lower_bound(cont.begin(), cont.end(), val, order_pred);
			cont.insert(iter, val);
		}
		template <typename Cont, typename Value> void InsertOrdered(Cont& cont, Value const& val)
		{
			Cont::iterator iter = std::lower_bound(cont.begin(), cont.end(), val);
			cont.insert(iter, val);
		}
	
		// Erase the first match to pred from 'cont'
		template <typename TCont, typename Pred> inline void EraseFirst(TCont& cont, Pred pred)
		{
			auto iter = std::find_if(std::begin(cont), std::end(cont), pred);
			if (iter == std::end(cont)) return;
			cont.erase(iter);
		}

		// Erase all elements from 'cont' that match 'pred'
		template <typename TCont, typename Pred> inline void EraseIf(TCont& cont, Pred pred)
		{
			auto end = std::remove_if(std::begin(cont), std::end(cont), pred);
			cont.erase(end, std::end(cont));
		}

		// Return the first element in 'cont' that matches 'pred' or nullptr
		template <typename TCont, typename Pred> inline auto FindIf(TCont& cont, Pred pred) -> decltype(&cont[0])
		{
			auto iter = std::find_if(std::begin(cont), std::end(cont), pred);
			if (iter == std::end(cont)) return nullptr;
			return &*iter;
		}

		// Return the first element in 'cont' that matches 'pred' or throw
		template <typename TCont, typename Pred> inline auto GetIf(TCont& cont, Pred pred) -> decltype(cont[0])
	{
		auto ptr = FindIf(cont, pred);
		if (ptr == nullptr) throw std::exception("GetIf() - no matches found");
		return *ptr;
	}
	}
}