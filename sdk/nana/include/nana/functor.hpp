/*
 *	A Functor implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0.
 *	(See accompanying file LICENSE_1_0.txt or copy at
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/functor.hpp
 *	@description:
 *		This functor is used for binding a member function of a class, it's class-independent
 */

#ifndef NANA_FUNCTOR_HPP
#define NANA_FUNCTOR_HPP
#include "traits.hpp"
#include <vector>
#include <functional>
#include <utility>

namespace nana
{
	namespace detail
	{
		template<bool ConceptMatched, typename MFPtr, int ArgSize>
		struct only_bind_this
		{
			typedef typename traits::mfptr_traits<MFPtr>::function function;

			template<typename T>
			static std::function<function> act(T & obj, MFPtr mf)
			{
				return std::function<function>();
			}
		};

		template<typename MFPtr, int ArgSize>
		struct only_bind_this<true, MFPtr, ArgSize>
		{
			typedef typename traits::mfptr_traits<MFPtr>::function function;
			typedef typename traits::mfptr_traits<MFPtr>::concept_type concept_type;

			static std::function<function> act(concept_type & obj, MFPtr mf)
			{
				return std::bind(mf, &obj);
			}
		};

		template<typename MFPtr>
		struct only_bind_this<true, MFPtr, 1>
		{
			typedef typename traits::mfptr_traits<MFPtr>::function function;
			typedef typename traits::mfptr_traits<MFPtr>::concept_type concept_type;

			static std::function<function> act(concept_type & obj, MFPtr mf)
			{
				return std::bind(mf, &obj, std::placeholders::_1);
			}
		};

		template<typename MFPtr>
		struct only_bind_this<true, MFPtr, 2>
		{
			typedef typename traits::mfptr_traits<MFPtr>::function function;
			typedef typename traits::mfptr_traits<MFPtr>::concept_type concept_type;

			static std::function<function> act(concept_type & obj, MFPtr mf)
			{
				return std::bind(mf, &obj, std::placeholders::_1, std::placeholders::_2);
			}
		};

		template<typename MFPtr>
		struct only_bind_this<true, MFPtr, 3>
		{
			typedef typename traits::mfptr_traits<MFPtr>::function function;
			typedef typename traits::mfptr_traits<MFPtr>::concept_type concept_type;

			static std::function<function> act(concept_type & obj, MFPtr mf)
			{
				return std::bind(mf, &obj, std::placeholders::_1, std::placeholders::_2, std::placeholders::_3);
			}
		};

		template<typename MFPtr>
		struct only_bind_this<true, MFPtr, 4>
		{
			typedef typename traits::mfptr_traits<MFPtr>::function function;
			typedef typename traits::mfptr_traits<MFPtr>::concept_type concept_type;

			static std::function<function> act(concept_type & obj, MFPtr mf)
			{
				return std::bind(mf, &obj, std::placeholders::_1, std::placeholders::_2, std::placeholders::_3, std::placeholders::_4);
			}
		};

		template<typename MFPtr>
		struct only_bind_this<true, MFPtr, 5>
		{
			typedef typename traits::mfptr_traits<MFPtr>::function function;
			typedef typename traits::mfptr_traits<MFPtr>::concept_type concept_type;

			static std::function<function> act(concept_type & obj, MFPtr mf)
			{
				return std::bind(mf, &obj, std::placeholders::_1, std::placeholders::_2, std::placeholders::_3, std::placeholders::_4, std::placeholders::_5);
			}
		};
	}//end namespace detail

	template<typename T, typename MFPtr>
	std::function<typename traits::mfptr_traits<MFPtr>::function> make_fun(T& obj, MFPtr mf)
	{
		typedef typename traits::mfptr_traits<MFPtr>::function function;
		typedef typename traits::mfptr_traits<MFPtr>::concept_type concept_type;

		return detail::only_bind_this<std::is_base_of<concept_type, typename std::remove_pointer<T>::type>::value, MFPtr, traits::mfptr_traits<MFPtr>::parameter>::act(obj, mf);
	}

	namespace detail
	{
		template<typename Ft, typename FnGroup>
		class functors_holder
		{
		public:
			typedef Ft function_type;
			typedef FnGroup fn_group_type;
			typedef std::vector<std::function<function_type>> container;

			fn_group_type & operator=(const std::function<function_type> & f)
			{
				clear();
				append(f);
				return *static_cast<fn_group_type*>(this);
			}

			fn_group_type & operator=(std::function<function_type>&& f)
			{
				clear();
				append(std::move(f));
				return *static_cast<fn_group_type*>(this);
			}

			fn_group_type & operator+=(const std::function<function_type> & f)
			{
				append(f);
				return *static_cast<fn_group_type*>(this);
			}

			fn_group_type & operator+=(std::function<function_type> && f)
			{
				append(std::move(f));
				return *static_cast<fn_group_type*>(this);
			}

			void append(const std::function<function_type> & f)
			{
				fobjs_.push_back(f);
			}

			void append(std::function<function_type> && f)
			{
				fobjs_.push_back(std::move(f));
			}

			void assign(const std::function<function_type> & f)
			{
				clear();
				fobjs_.push_back(f);
			}

			void assign(std::function<function_type> && f)
			{
				clear();
				fobjs_.push_back(std::move(f));
			}

			void clear()
			{
				container().swap(fobjs_);
			}

			bool empty() const
			{
				return (0 == fobjs_.size());
			}

			operator void*() const
			{
				return (fobjs_.size() ? const_cast<functors_holder*>(this) : 0);
			}
		protected:
			const container & _m_cont() const
			{
				return fobjs_;
			}
		private:
			container fobjs_;
		};
	}//end namespace detail

	template<typename Ft>
	class fn_group;

	template<typename R>
	class fn_group<R()>
		: public detail::functors_holder<R(), fn_group<R()> >
	{
	public:
		typedef R function_type();
		typedef typename detail::functors_holder<function_type, fn_group>::container container;
		using detail::functors_holder<function_type, fn_group>::assign;
		using detail::functors_holder<function_type, fn_group>::operator=;

		template<typename T, typename Concept>
		void assign(T& obj, R(Concept::*mf)())
		{
			append(make_fun(obj, mf));
		}

		R operator()() const
		{
			auto & fobjs = this->_m_cont();
			if(fobjs.size())
			{
				auto last = fobjs.cend() - 1;
				for(auto i = fobjs.cbegin(); i != last; ++i)
					(*i)();
				return (*last)();
			}
			return R();
		}
	};

	template<typename R, typename P0>
	class fn_group<R(P0)>
		: public detail::functors_holder<R(P0), fn_group<R(P0)>>
	{
	public:
		typedef R function_type(P0);
		typedef typename detail::functors_holder<function_type, fn_group>::container container;
		using detail::functors_holder<function_type, fn_group>::assign;
		using detail::functors_holder<function_type, fn_group>::operator=;

		template<typename T, typename Concept>
		void assign(T& obj, R(Concept::*mf)(P0))
		{
			append(make_fun(obj, mf));
		}

		R operator()(P0 p0) const
		{
			auto & fobjs = this->_m_cont();
			if(fobjs.size())
			{
				auto last = fobjs.cend() - 1;
				for(auto i = fobjs.cbegin(); i != last; ++i)
					(*i)(p0);
				return (*last)(p0);
			}
			return R();
		}
	};

	template<typename R, typename P0, typename P1>
	class fn_group<R(P0, P1)>
		: public detail::functors_holder<R(P0, P1), fn_group<R(P0, P1)>>
	{
	public:
		typedef R function_type(P0, P1);
		typedef typename detail::functors_holder<function_type, fn_group>::container container;
		using detail::functors_holder<function_type, fn_group>::assign;
		using detail::functors_holder<function_type, fn_group>::operator=;

		template<typename T, typename Concept>
		void assign(T& obj, R(Concept::*mf)(P0, P1))
		{
			append(make_fun(obj, mf));
		}

		R operator()(P0 p0, P1 p1) const
		{
			auto & fobjs = this->_m_cont();
			if(fobjs.size())
			{
				auto last = fobjs.cend() - 1;
				for(auto i = fobjs.cbegin(); i != last; ++i)
					(*i)(p0, p1);
				return (*last)(p0, p1);
			}
			return R();
		}
	};

	template<typename R, typename P0, typename P1, typename P2>
	class fn_group<R(P0, P1, P2)>
		: public detail::functors_holder<R(P0, P1, P2), fn_group<R(P0, P1, P2)>>
	{
	public:
		typedef R function_type(P0, P1, P2);
		typedef typename detail::functors_holder<function_type, fn_group>::container container;
		using detail::functors_holder<function_type, fn_group>::assign;
		using detail::functors_holder<function_type, fn_group>::operator=;

		template<typename T, typename Concept>
		void assign(T& obj, R(Concept::*mf)(P0, P1, P2))
		{
			append(make_fun(obj, mf));
		}

		R operator()(P0 p0, P1 p1, P2 p2) const
		{
			auto & fobjs = this->_m_cont();
			if(fobjs.size())
			{
				auto last = fobjs.cend() - 1;
				for(auto i = fobjs.cbegin(); i != last; ++i)
					(*i)(p0, p1, p2);
				return (*last)(p0, p1, p2);
			}
			return R();
		}
	};

	template<typename R, typename P0, typename P1, typename P2, typename P3>
	class fn_group<R(P0, P1, P2, P3)>
		: public detail::functors_holder<R(P0, P1, P2, P3), fn_group<R(P0, P1, P2, P3)>>
	{
	public:
		typedef R function_type(P0, P1, P2, P3);
		typedef typename detail::functors_holder<function_type, fn_group>::container container;
		using detail::functors_holder<function_type, fn_group>::assign;
		using detail::functors_holder<function_type, fn_group>::operator=;

		template<typename T, typename Concept>
		void assign(T& obj, R(Concept::*mf)(P0, P1, P2, P3))
		{
			append(make_fun(obj, mf));
		}

		R operator()(P0 p0, P1 p1, P2 p2, P3 p3) const
		{
			auto & fobjs = this->_m_cont();
			if(fobjs.size())
			{
				auto last = fobjs.cend() - 1;
				for(auto i = fobjs.cbegin(); i != last; ++i)
					(*i)(p0, p1, p2, p3);
				return (*last)(p0, p1, p2, p3);
			}
			return R();
		}
	};

	template<typename R, typename P0, typename P1, typename P2, typename P3, typename P4>
	class fn_group<R(P0, P1, P2, P3, P4)>
		: public detail::functors_holder<R(P0, P1, P2, P3, P4), fn_group<R(P0, P1, P2, P3, P4)>>
	{
	public:
		typedef R function_type(P0, P1, P2, P3, P4);
		typedef typename detail::functors_holder<function_type, fn_group>::container container;
		using detail::functors_holder<function_type, fn_group>::assign;
		using detail::functors_holder<function_type, fn_group>::operator=;

		template<typename T, typename Concept>
		void assign(T& obj, R(Concept::*mf)(P0, P1, P2, P3, P4))
		{
			append(make_fun(obj, mf));
		}

		R operator()(P0 p0, P1 p1, P2 p2, P3 p3, P4 p4) const
		{
			auto & fobjs = this->_m_cont();
			if(fobjs.size())
			{
				auto last = fobjs.cend() - 1;
				for(auto i = fobjs.cbegin(); i != last; ++i)
					(*i)(p0, p1, p2, p3, p4);
				return (*last)(p0, p1, p2, p3, p4);
			}
			return R();
		}
	};
}//end namespace nana

#endif
