/*
 *	A Functor implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/detail/functor_invoker_1.hpp
 *	@description:
 *		This is a functor invoker definition. It specifies an invoker with only one
 *	parameter.
 */
 
#ifndef NANA_DETAIL_FUNCTOR_INVOKER_1_HPP
#define NANA_DETAIL_FUNCTOR_INVOKER_1_HPP
#include "functor_invoker.hpp"

namespace nana
{
	namespace detail
	{
		/*
		 * 1-Parameter Implementation
		 */
		template<typename R, typename P0>
		class abs_invoker<R(P0)>
		{
		public:
			virtual ~abs_invoker(){}
			virtual R apply(P0) const = 0;
			virtual abs_invoker* clone() const = 0;
		};
		
		//class invoker
		//@brief: invoker is used for saving a function address and invoking it.
		template<typename R, typename P0>
		class invoker<R(P0)>: public abs_invoker<R(P0)>
		{
			typedef abs_invoker<R(P0)> base_type;
			typedef R(*fptr)(P0);
		public:
			invoker(fptr faddr)
				: faddr_(faddr)
			{}
			
			R apply(P0 p0) const{ return (faddr_ ? faddr_(p0): R());}
			base_type* clone() const { return new invoker(faddr_); }
		private:
			fptr faddr_;
		};

		template<typename R, typename P0, typename Concept>
		class invoker<R(Concept::*)(P0)>: public abs_invoker<R(P0)>
		{
			typedef R(Concept::*mfptr)(P0);
		public:
			invoker(Concept& obj, mfptr mf)
				: obj_(obj), mf_(mf)
			{}
			
			R apply(P0 p0) const{ return (mf_ ? (obj_.*mf_)(p0): R());}
			abs_invoker<R(P0)>* clone() const { return new invoker(*this); }
		private:
			Concept & obj_;
			mfptr mf_;
		};

		template<typename R, typename P0, typename Concept>
		class invoker<R(Concept::*)(P0) const>: public abs_invoker<R(P0)>
		{
			typedef R(Concept::*mfptr)(P0) const;
		public:
			invoker(const Concept& obj, mfptr mf)
				: obj_(obj), mf_(mf)
			{}
			
			R apply(P0 p0) const{ return (mf_ ? (obj_.*mf_)(p0): R());}
			abs_invoker<R(P0)>* clone() const { return new invoker(*this); }
		private:
			const Concept & obj_;
			mfptr mf_;
		};

		template<typename R, typename P0, typename Concept>
		class invoker<R(Concept::*)(P0) volatile>: public abs_invoker<R(P0)>
		{
			typedef R(Concept::*mfptr)(P0) volatile;
		public:
			invoker(volatile Concept& obj, mfptr mf)
				: obj_(obj), mf_(mf)
			{}
			
			R apply(P0 p0) const{ return (mf_ ? (obj_.*mf_)(p0): R());}
			abs_invoker<R(P0)>* clone() const { return new invoker(*this); }
		private:
			volatile Concept & obj_;
			mfptr mf_;
		};

		template<typename R, typename P0, typename Concept>
		class invoker<R(Concept::*)(P0) const volatile>: public abs_invoker<R(P0)>
		{
			typedef R(Concept::*mfptr)(P0) const volatile;
		public:
			invoker(const volatile Concept& obj, mfptr mf)
				: obj_(obj), mf_(mf)
			{}
			
			R apply(P0 p0) const{ return (mf_ ? (obj_.*mf_)(p0): R());}
			abs_invoker<R(P0)>* clone() const { return new invoker(*this); }
		private:
			const volatile Concept & obj_;
			mfptr mf_;
		};

		template<typename FO, typename R, typename P0>
		class fo_invoker<FO, R(P0)>: public abs_invoker<R(P0)>
		{
		public:
			fo_invoker(FO& fobj)
				: fobj_(fobj)
			{}

			R apply(P0 p0) const
			{
				return fobj_(p0);
			}

			abs_invoker<R(P0)> * clone() const
			{
				return (new fo_invoker(*this));
			}
		private:
			mutable FO fobj_;
		};


		template<typename R, typename P0>
		class interface_holder<R(P0)>
		{
		public:
			typedef R function_type(P0);
			typedef abs_invoker<function_type> invoker_type;

			interface_holder()
				:invoker_(0)
			{}

			interface_holder(const interface_holder& rhs)
				:invoker_(rhs.invoker_?rhs.invoker_->clone():0)
			{
			}

			virtual ~interface_holder()
			{
				delete invoker_;
			}

			void close()
			{
				delete invoker_;
				invoker_ = 0;
			}
			
			bool empty() const
			{
				return (0 == invoker_);	
			}

			interface_holder& operator=(const interface_holder& rhs)
			{
				if(this != &rhs)
				{
					delete invoker_;
					invoker_ = (rhs.invoker_ ? rhs.invoker_->clone() : 0);
				}
				return *this;
			}
			
			R operator()(P0 p0) const
			{
				return (invoker_? invoker_->apply(p0) : R());
			}
		protected:
			void assign_invoker(invoker_type* ivk)
			{
				if(ivk)
				{
					delete invoker_;
					invoker_ = ivk;	
				}	
			}
		private:
			invoker_type* invoker_;
		};
	}//end namespace detail
}//end namespace nana
#endif
