//************************************************************************
// Property (like C# properties)
//  Copyright (c) Rylogic Ltd 2009
//************************************************************************
#pragma once
#ifndef PR_PROPERTY_H
#define PR_PROPERTY_H

#include "pr/meta/alignment_of.h"
#include "pr/meta/aligned_type.h"
#include "pr/common/assert.h"

#pragma warning (disable: 4355) // 'this' : used in base member initializer list

namespace pr
{
	namespace EPropertyType { enum Type { Get = 1, Set = 2, GetSet = 3 }; }

	namespace impl
	{
		template <EPropertyType::Type RW, typename GetSet, typename Get, typename Set> struct PropType { typedef void type; };
		template <typename GetSet, typename Get, typename Set> struct PropType<EPropertyType::GetSet, GetSet, Get, Set> { typedef GetSet type; };
		template <typename GetSet, typename Get, typename Set> struct PropType<EPropertyType::Get   , GetSet, Get, Set> { typedef Get    type; };
		template <typename GetSet, typename Get, typename Set> struct PropType<EPropertyType::Set   , GetSet, Get, Set> { typedef Set    type; };

		typedef pr::meta::aligned_type<8>::type C;

		template <typename ValueType> struct PropertyBase
		{
			typedef typename void (C::*Setter)(ValueType value);
			typedef typename ValueType (C::*Getter)();

			Getter m_get;
			Setter m_set;
			void*  m_cont;

			PropertyBase()
			:m_get()
			,m_set()
			,m_cont()
			{}

			template <typename Cont> void Bind(Cont* this_, ValueType (Cont::*getter)() const, void (Cont::*setter)(ValueType))
			{
				static_assert((pr::meta::alignment_of<C>::value % pr::meta::alignment_of<Cont>::value) == 0, "");

				m_cont = this_;
				m_get = reinterpret_cast<Getter>(getter);
				m_set = reinterpret_cast<Setter>(setter);
			}
		};
	}

	// Getter property
	template <typename ValueType> struct PropertyR :private impl::PropertyBase<ValueType>
	{
		PropertyR(){}

		template <typename Cont> PropertyR(Cont* this_, ValueType (Cont::*getter)() const)
		{
			Bind(this_, getter);
		}

		template <typename Cont> void Bind(Cont* this_, ValueType (Cont::*getter)() const)
		{
			impl::PropertyBase<ValueType>::Bind<Cont>(this_, getter, 0);
		}

		operator ValueType() const
		{
			PR_ASSERT(PR_DBG, m_cont != 0, "");
			PR_ASSERT(PR_DBG, m_get  != 0, "");
			return (static_cast<impl::C*>(m_cont)->*m_get)();
		}
	};

	// Setter property
	template <typename ValueType> struct PropertyW :private impl::PropertyBase<ValueType>
	{
		PropertyW(){}

		template <typename Cont> PropertyW(Cont* this_, void (Cont::*setter)(ValueType))
		{
			Bind(this_, setter);
		}

		template <typename Cont> void Bind(Cont* this_, void (Cont::*setter)(ValueType))
		{
			impl::PropertyBase<ValueType>::Bind<Cont>(this_, 0, setter);
		}

		ValueType operator = (ValueType value) const
		{
			PR_ASSERT(PR_DBG, m_cont != 0, "");
			PR_ASSERT(PR_DBG, m_set  != 0, "");
			(static_cast<impl::C*>(m_cont)->*m_set)(value);
			return value;
		}
	};

	// Getter/Setter property
	template <typename ValueType> struct PropertyRW :private impl::PropertyBase<ValueType>
	{
		PropertyRW(){}

		template <typename Cont> PropertyRW(Cont* this_, ValueType (Cont::*getter)() const, void (Cont::*setter)(ValueType))
		{
			Bind(this_, getter, setter);
		}

		template <typename Cont> void Bind(Cont* this_, ValueType (Cont::*getter)() const, void (Cont::*setter)(ValueType))
		{
			impl::PropertyBase<ValueType>::Bind<Cont>(this_, getter, setter);
		}

		operator ValueType() const
		{
			PR_ASSERT(PR_DBG, m_cont != 0, "");
			PR_ASSERT(PR_DBG, m_get  != 0, "");
			return (static_cast<impl::C*>(m_cont)->*m_get)();
		}

		operator PropertyR<ValueType>() const
		{
			return PropertyR<ValueType>(m_cont, m_get);
		}

		ValueType operator = (ValueType value)
		{
			PR_ASSERT(PR_DBG, m_cont != 0, "");
			PR_ASSERT(PR_DBG, m_set  != 0, "");
			(static_cast<impl::C*>(m_cont)->*m_set)(value);
			return value;
		}
	};

	// Getter with included field
	template <typename ValueType> struct PropertyRF :private PropertyR<ValueType>
	{
		ValueType m_value;

		PropertyRF(){}

		template <typename Cont> PropertyRF(Cont* this_, ValueType (Cont::*getter)() const)
		{
			Bind(this_, getter);
		}

		using PropertyR<ValueType>::Bind;
		using PropertyR<ValueType>::operator =;
		using PropertyR<ValueType>::operator ValueType;
	};

	// Setter with included field
	template <typename ValueType> struct PropertyWF :private PropertyW<ValueType>
	{
		ValueType m_value;

		PropertyWF(){}

		template <typename Cont> PropertyWF(Cont* this_, void (Cont::*setter)(ValueType))
		{
			Bind(this_, setter);
		}

		using PropertyW<ValueType>::Bind;
		using PropertyW<ValueType>::operator =;
		using PropertyW<ValueType>::operator ValueType;
	};

	// Getter/Setter with included field
	template <typename ValueType> struct PropertyRWF :private PropertyRW<ValueType>
	{
		ValueType m_value;

		PropertyRWF(){}

		template <typename Cont> PropertyRWF(Cont* this_, ValueType (Cont::*getter)() const, void (Cont::*setter)(ValueType))
		{
			Bind(this_, getter, setter);
		}

		using PropertyRW<ValueType>::Bind;
		using PropertyRW<ValueType>::operator =;
		using PropertyRW<ValueType>::operator ValueType;
		using PropertyRW<ValueType>::operator pr::PropertyR<ValueType>;
	};
}

#endif
