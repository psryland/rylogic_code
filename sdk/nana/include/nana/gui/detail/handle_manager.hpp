/*
 *	Handle Manager Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/detail/handle_manager.hpp
 *
 *	@description:
 *	this manages all the window handles
 */

#ifndef NANA_GUI_DETAIL_HANDLE_MANAGER_HPP
#define NANA_GUI_DETAIL_HANDLE_MANAGER_HPP
#include <map>
#include <nana/traits.hpp>
#include <nana/threads/mutex.hpp>
#include <iterator>

namespace nana
{
namespace gui
{
	namespace detail
	{
		template<typename Key, typename Value, std::size_t CacheSize>
		class cache
			: noncopyable
		{
		public:
			typedef Key	key_type;
			typedef Value value_type;
			typedef std::pair<key_type, value_type> pair_type;
			typedef std::size_t size_type;
			
			cache()
				:addr_(reinterpret_cast<pair_type*>(::operator new(sizeof(pair_type) * CacheSize)))
			{
				for(std::size_t i = 0; i < CacheSize; ++i)
				{
					bitmap_[i] = 0;
					seq_[i] = nana::npos;
				}
			}
			
			~cache()
			{
				for(std::size_t i = 0; i < CacheSize; ++i)
				{
					if(bitmap_[i])
						addr_[i].~pair_type();
				}

				::operator delete(addr_);
			}
			
			bool insert(key_type k, value_type v)
			{
				size_type pos = _m_find_key(k);
				if(pos != nana::npos)
				{
					addr_[pos].second = v;
				}
				else
				{
					//No key exists
					pos = _m_find_pos();
					
					if(pos == nana::npos)
					{	//No room, and remove the last pair
						pos = seq_[CacheSize - 1];
						(addr_ + pos)->~pair_type();
					}
					
					if(seq_[0] != nana::npos)
					{//Need to move
						for(std::size_t i = CacheSize - 1; i > 0; --i)
							seq_[i] = seq_[i - 1];
					}
					
					seq_[0] = pos;

					new (addr_ + pos) pair_type(k, v);
					bitmap_[pos] = 1;
				}
				return v;
			}
			
			value_type * get(key_type k)
			{
				size_type pos = _m_find_key(k);
				if(pos != nana::npos)
					return &(addr_[pos].second);
				return 0;
			}
		private:
			size_type _m_find_key(key_type k) const
			{
				for(std::size_t i = 0; i < CacheSize; ++i)
				{
					if(bitmap_[i] && (addr_[i].first == k))
						return i;
				}
				return nana::npos;
			}
			
			size_type _m_find_pos() const
			{
				for(std::size_t i = 0; i < CacheSize; ++i)
				{
					if(bitmap_[i] == 0)
						return i;	
				}
				return nana::npos;
			}
		private:
			char bitmap_[CacheSize];
			size_type seq_[CacheSize];
			pair_type * addr_;
		};

		template<bool IsPtr, typename HandleType>
		struct handle_manager_deleter_impl
		{
			void operator()(const HandleType handle) const
			{
				delete handle;
			}
		};

		template<typename HandleType>
		struct handle_manager_deleter_impl<false, HandleType>
		{
			void operator()(const HandleType) const
			{
				//This Error is that you should define a deleter type for the non-pointer handle type
				int YouHaveToDefineADeleterForHandleManager3thTemplateParameterForANonPointerHandleType = (int*)0;
			}
		};

		template<typename HandleType>
		struct default_handle_manager_deleter
			:public handle_manager_deleter_impl<nana::traits::is_pointer<HandleType>::value, HandleType>
		{};

		//handle_manager
		//@brief
		//		handle_manager maintains handles of a type. removing a handle dose not destroy it,
		//	it will be inserted to a trash queue for deleting at a safe time.
		//		For efficiency, this class is not a thread-safe.
		template<typename HandleType, typename Condition, typename Deleter = default_handle_manager_deleter<HandleType> >
		class handle_manager
			:private nana::noncopyable
		{
		public:
			typedef HandleType	handle_type;
			typedef Condition	cond_type;
			typedef Deleter		deleter_type;
			typedef std::map<handle_type, unsigned>	holder_map;
			typedef std::pair<handle_type, unsigned>	holder_pair;

			~handle_manager()
			{
				this->delete_trash(0);
			}

			void insert(handle_type handle, unsigned tid)
			{
				threads::lock_guard<threads::recursive_mutex> lock(mutex_);

				holder_[handle] = tid;

				is_queue<nana::traits::same_type<cond_type, nana::null_type>::value, std::vector<handle_type> >::insert(handle, queue_);
				cacher_.insert(handle, true);
			}

			void operator()(const handle_type handle)
			{
				remove(handle);
			}

			void remove(const handle_type handle)
			{
				threads::lock_guard<threads::recursive_mutex> lock(mutex_);

				typename holder_map::iterator i = holder_.find(handle);
				if(holder_.end() != i)
				{
					is_queue<nana::traits::same_type<cond_type, nana::null_type>::value, std::vector<handle_type> >::erase(handle, queue_);
					cacher_.insert(handle, false);
					trash_.push_back(holder_pair(i->first, i->second));
					holder_.erase(i);
				}
			}

			void delete_trash(unsigned tid)
			{
				threads::lock_guard<threads::recursive_mutex> lock(mutex_);

				if(trash_.size())
				{
					deleter_type del_functor;
					if(tid == 0)
					{
						for(typename std::vector<holder_pair>::iterator i = trash_.begin(); i != trash_.end(); ++i)
							del_functor(i->first);
						trash_.clear();
					}
					else
					{
						for(typename std::vector<holder_pair>::iterator i = trash_.begin(); i != trash_.end();)
						{
							if(tid == i->second)
							{
								del_functor(i->first);
								i = trash_.erase(i);
							}
							else
								++i;
						}
					}
				}
			}

			handle_type last() const
			{
				threads::lock_guard<threads::recursive_mutex> lock(mutex_);
				if(queue_.size())
					return *(queue_.end() - 1);

				return handle_type();
			}

			std::size_t size() const
			{
				return holder_.size();
			}

			handle_type get(unsigned index) const
			{
				threads::lock_guard<threads::recursive_mutex> lock(mutex_);
				if(index < queue_.size())
					return *(queue_.begin() + index);
				return handle_type();
			}

			bool available(const handle_type handle)	const
			{
				threads::lock_guard<threads::recursive_mutex> lock(mutex_);

				bool * v = cacher_.get(handle);
				if(v) return *v;
				return cacher_.insert(handle, (holder_.count(handle) != 0));
			}

			void all(std::vector<handle_type> & v) const
			{
				threads::lock_guard<threads::recursive_mutex> lock(mutex_);
				std::copy(queue_.begin(), queue_.end(), std::back_inserter(v));
			}
		private:
			
			template<bool IsQueueOperation, typename Container>
			struct is_queue
			{
			public:
				static void insert(handle_type handle, Container& queue)
				{
					if(cond_type::is_queue(handle))
						queue.push_back(handle);
				}

				static void erase(handle_type handle, Container& queue)
				{
					if(cond_type::is_queue(handle))
					{
						typename std::vector<handle_type>::iterator it = std::find(queue.begin(), queue.end(), handle);
						if(it != queue.end())
							queue.erase(it);
					}
				}
			};

			template<typename Container>
			struct is_queue<true, Container>
			{
			public:
				static void insert(handle_type handle, Container& queue){}
				static void erase(handle_type handle, Container& queue){}
			};
			
		private:
			mutable threads::recursive_mutex mutex_;
			mutable cache<const handle_type, bool, 5> cacher_;
			std::map<handle_type, unsigned>	holder_;
			std::vector<handle_type>	queue_;
			std::vector<holder_pair>	trash_;
		};//end class handle_manager
	}//end namespace detail
}//end namespace gui
}// end namespace nana
#endif
