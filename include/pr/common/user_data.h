//***************************************************************************************************
// User Data
//  Copyright (c) Rylogic Ltd 2014
//***************************************************************************************************
// Mix-in class

#include <memory>
#include <unordered_map>
#include "pr/common/new.h"

#pragma once

namespace pr
{
	class UserData
	{
		struct IUData :pr::AlignTo<16>
		{
			virtual ~IUData() {}
		};
		template <typename Id, typename TData> struct UData :std::unordered_map<Id,TData> ,IUData
		{};

		typedef size_t typeid_t;
		typedef std::unordered_map<typeid_t, std::unique_ptr<IUData>> UDataMap;
		UDataMap m_user_data;

		static typeid_t NextId()                           { static typeid_t id = 0; return ++id; }
		template <typename TData> static typeid_t TypeId() { static typeid_t id = NextId(); return id; }

		// Find a pointer to the user data table for 'T'
		template <typename Id, typename TData> UData<Id,TData> const* find() const
		{
			auto iter = m_user_data.find(TypeId<TData>());
			return iter != std::end(m_user_data) ? static_cast<UData<Id,TData> const*>(iter->second.get()) : nullptr;
		}
		template <typename Id, typename TData> UData<Id,TData>* find()
		{
			auto iter = m_user_data.find(TypeId<TData>());
			return iter != std::end(m_user_data) ? static_cast<UData<Id,TData>*>(iter->second.get()) : nullptr;
		}

		// Get/Create the user data table for 'TData'
		template <typename Id, typename TData> UData<Id,TData>& table()
		{
			auto& ptr = m_user_data[TypeId<TData>()];
			if (ptr == nullptr) ptr = std::make_unique<UData<Id,TData>>();
			return static_cast<UData<Id,TData>&>(*ptr.get());
		}

	public:

		// Access user data by type and unique id
		template <typename TData, typename Id = intptr_t> TData const& get(Id id = Id()) const
		{
			// Get the pointer to the UData<T> table
			auto tab = find<Id,TData>();
			if (tab == nullptr) throw std::exception("user data not found");

			// Find the item within the table corresponding to 'id'
			auto iter = tab->find(id);
			if (iter == std::end(*tab)) throw std::exception("user data not found");

			// Return it
			return iter->second;
		}

		// Access user data by type and unique id. User data is lazily created.
		template <typename TData, typename Id = intptr_t> TData& get(Id id = Id())
		{
			return table<Id,TData>()[id];
		}

		// True if this object contains user data matching 'id'
		template <typename TData, typename Id = intptr_t> bool has(Id id = Id()) const
		{
			auto tab = find<Id,TData>();
			return tab != nullptr && tab->count(id) != 0;
		}

		// Remove user data
		template <typename TData, typename Id = intptr_t> void erase(Id id = Id())
		{
			auto tab = find<Id,TData>();
			if (tab != nullptr)
				tab->erase(id);
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_common_userdata)
		{
			struct UDTest :pr::UserData
			{
				int i;
			};

			struct Blob
			{
				char str[20];
				Blob() :str() {}
				Blob(char const* s) :str() { strcat(str, s); }
			} blob("HelloWorld\0");

			UDTest udtest;
			udtest.get<Blob>() = blob;
			udtest.get<double>() = 3.14;

			PR_CHECK(udtest.has<Blob>(), true);
			PR_CHECK(udtest.has<double>(), true);
			PR_CHECK(udtest.has<int>(), false);

			PR_CHECK(udtest.get<double>(), 3.14);
			PR_CHECK(udtest.get<Blob>().str, "HelloWorld\0");
			PR_CHECK(&udtest.get<Blob>() != &blob, true);

			udtest.get<double>() = 6.28;
			PR_CHECK(udtest.get<double>(), 6.28);

			udtest.erase<double>();
			udtest.erase<Blob>();
			PR_CHECK(udtest.has<double>(), false);
			PR_CHECK(udtest.has<Blob>(), false);
		}
	}
}
#endif