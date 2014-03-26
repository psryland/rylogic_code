/*
 *	Smart Pointer Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	@file: nana/refer.h
 */

#ifndef NANA_MEMORY_HPP
#define NANA_MEMORY_HPP

#ifdef NANA_USE_BOOST_SMART_PTR

#include <boost/shared_ptr.hpp>

namespace nana
{
	using boost::shared_ptr;
}

#else

#include <nana/functor.hpp>

namespace nana
{
	namespace detail
	{
		class shared_block
		{
			struct block_impl;
		public:
			shared_block();
			shared_block(nana::functor<void()> deleter);
			
			shared_block(const shared_block&);
			shared_block& operator=(const shared_block&);
			
			~shared_block();
			
			bool unique() const;
			void swap(shared_block&);
		private:
			block_impl * impl_;
		};	//end class shared_block
	}//end namespace detail
	
	template<typename T>
	class shared_ptr
	{
		template<typename U>
		struct deleter
		{
			U * px;
			
			deleter(U* p): px(p)
			{
			}
			
			void operator()()
			{
				delete px;
			}
		};

		template<typename U, typename D>
		struct delete_wrapper
		{
			U * px;
			D	dx;
			
			delete_wrapper(U* p, D d): px(p), dx(d)
			{
			}
			
			void operator()()
			{
				dx(px);
			}
		};

		typedef T* shared_ptr::* unspecified_bool_t;
		
	public:
		typedef T element_type;
		
		shared_ptr()
			: ptr_(0)
		{}
		
		template<typename Y>
		explicit shared_ptr(Y* p)
			: ptr_(p)
		{
			if(p)
				detail::shared_block(deleter<Y>(p)).swap(block_);
		}
		
		template<typename Y, typename Deleter>
		explicit shared_ptr(Y* p, Deleter d)
			: ptr_(p)
		{
			if(p)
				detail::shared_block(nana::functor<void()>(delete_wrapper<Y, Deleter>(p, d))).swap(block_);
		}
		
		shared_ptr(const shared_ptr& r)
			: ptr_(r.ptr_), block_(r.block_)
		{
		}
		
		shared_ptr& operator=(const shared_ptr& r)
		{
			if(this != &r)
				shared_ptr(r).swap(*this);

			return *this;
		}
		
		void swap(shared_ptr& r)
		{
			element_type * tmp = r.ptr_;
			r.ptr_ = ptr_;
			ptr_ = tmp;
			block_.swap(r.block_);
		}
		
		void reset()
		{
			shared_ptr().swap(*this);
		}

		void reset(element_type * p)
		{
			if(p != ptr_)
				shared_ptr(p).swap(*this);
		}

		template<typename Deleter>
		void reset(element_type* p, Deleter d)
		{
			if(p != ptr_)
				shared_ptr(p, d).swap(*this);
		}
		
		element_type * get() const
		{
			return ptr_;
		}
		
		element_type& operator*() const
		{
			return *ptr_;
		}
		
		element_type * operator->() const
		{
			return ptr_;
		}
		
		bool unique() const
		{
			return block_.unique();
		}
	    
	    operator unspecified_bool_t() const
	    {
	    	return (ptr_ != 0 ? &shared_ptr::ptr_ : 0);
	    }
	private:
		element_type *	ptr_;
		detail::shared_block	block_;
	};
}// end namespace nana

template<typename T, typename U>
bool operator==(const nana::shared_ptr<T> & a, const nana::shared_ptr<U> & b)
{
	return (a.get() == b.get());
}

#endif
	
#endif

