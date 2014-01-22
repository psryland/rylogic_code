/*
 *	A Functor implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/detail/functor_invoker_0.hpp
 *	@description:
 *		This is a functor invoker definition. It specifies an invoker without any
 *	parameters.
 */
 
#ifndef NANA_DETAIL_FUNCTOR_INVOKER_0_HPP
#define NANA_DETAIL_FUNCTOR_INVOKER_0_HPP
#include "functor_invoker.hpp"

namespace nana
{
	namespace detail
	{
		/*
		 * 0-Parameter Implementation
		 */
		template<typename R>
		class abs_invoker<R()>
		{
		public:
            typedef R return_type;   
			virtual ~abs_invoker(){}
			virtual return_type apply() const = 0;
			virtual abs_invoker* clone() const = 0;
		};
		
		//class invoker
		//@brief: invoker is used for saving a function address and invoking it.
		template<typename R>
		class invoker<R()>: public abs_invoker<R()>
		{
			typedef abs_invoker<R()> base_type;
			typedef R(*address_t)();
		public:
			invoker(address_t address)
				: address_(address)
			{}
			
			R apply() const{ return (address_ ? address_(): R());}
			base_type* clone() const { return new invoker(*this); }
		private:
			address_t address_;
		
		};

		template<typename R, typename Concept>
		class invoker<R(Concept::*)()>: public abs_invoker<R()>
		{
			typedef R(Concept::*mfptr)();
		public:
			invoker(Concept& obj, mfptr mf)
				: obj_(obj), mf_(mf)
			{}
			
			R apply() const{ return (mf_ ? (obj_.*mf_)(): R());}
			abs_invoker<R()>* clone() const { return new invoker(*this); }
		private:
			Concept & obj_;
			mfptr mf_;
		};

		template<typename R, typename Concept>
		class invoker<R(Concept::*)() const>: public abs_invoker<R()>
		{
			typedef R(Concept::*mfptr)() const;
		public:
			invoker(const Concept& obj, mfptr mf)
				: obj_(obj), mf_(mf)
			{}
			
			R apply() const{ return (mf_ ? (obj_.*mf_)(): R());}
			abs_invoker<R()>* clone() const { return new invoker(*this); }
		private:
			const Concept & obj_;
			mfptr mf_;
		};

		template<typename R, typename Concept>
		class invoker<R(Concept::*)() volatile>: public abs_invoker<R()>
		{
			typedef R(Concept::*mfptr)() volatile;
		public:
			invoker(volatile Concept& obj, mfptr mf)
				: obj_(obj), mf_(mf)
			{}
			
			R apply() const{ return (mf_ ? (obj_.*mf_)(): R());}
			abs_invoker<R()>* clone() const { return new invoker(*this); }
		private:
			volatile Concept & obj_;
			mfptr mf_;
		};

		template<typename R, typename Concept>
		class invoker<R(Concept::*)() const volatile>: public abs_invoker<R()>
		{
			typedef R(Concept::*mfptr)() const volatile;
		public:
			invoker(const volatile Concept& obj, mfptr mf)
				: obj_(obj), mf_(mf)
			{}
			
			R apply() const{ return (mf_ ? (obj_.*mf_)(): R());}
			abs_invoker<R()>* clone() const { return new invoker(*this); }
		private:
			const volatile Concept & obj_;
			mfptr mf_;
		};

		template<typename FO, typename R>
		class fo_invoker<FO, R()>: public abs_invoker<R()>
		{
		public:
			fo_invoker(FO& fobj)
				: fobj_(fobj)
			{}

			R apply() const
			{
				return fobj_();
			}

			abs_invoker<R()> * clone() const
			{
				return (new fo_invoker(*this));
			}
		private:
			mutable FO fobj_;
		};

		template<typename R>
		class interface_holder<R()>
		{
		public:
			typedef R function_type();
			typedef abs_invoker<function_type> invoker_type;

			interface_holder()
				:invoker_(0)
			{}

			interface_holder(const interface_holder& rhs)
				:invoker_(rhs.invoker_?rhs.invoker_->clone():0)
			{}

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

			R operator()() const
			{
				return (invoker_? invoker_->apply() : R());
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
