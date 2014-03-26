/*
 *	Event Manager Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/detail/event_manager.cpp
 */

#include <nana/config.hpp>
#include PLATFORM_SPEC_HPP
#include GUI_BEDROCK_HPP
#include <nana/gui/detail/event_manager.hpp>
#include <map>
#include <algorithm>
#include <nana/threads/mutex.hpp>

namespace nana
{
namespace gui
{
namespace detail
{
	namespace inner_event_manager
	{
		//struct inner_handler_invoker
		//@brief: The definition of struct inner_handler_invoker
		struct inner_handler_invoker
		{
		public:
			inner_handler_invoker(event_manager::handle_manager_type& mgr, const eventinfo& ei)
				:ei_(ei), mgr_(mgr)
			{}

			void operator()(abstract_handler* abs_handler)
			{
				if(mgr_.available(abs_handler))
					abs_handler->exec(ei_);
			}
		private:
			const eventinfo& ei_;
			event_manager::handle_manager_type& mgr_;
		};

		struct handler_queue
		{
			handler_queue()
				:queue_(fixed_buffer_), size_(0), capacity_(10)
			{}

			~handler_queue()
			{
				if(queue_ != fixed_buffer_)
					delete [] queue_;
			}

			void clear()
			{
				size_ = 0;
			}

			template<typename Container>
			void copy(Container& container)
			{
				typename Container::iterator iter = container.begin();
				std::size_t size = container.size();
				abstract_handler * *i = queue_ + size_;
				abstract_handler * * const end = i + size;
				allocate(size);
				while(i < end)
					*i++ = *iter++;
			}

			void allocate(std::size_t size)
			{
				if(capacity_ - size_ < size)
				{
					capacity_ = size_ + size;
					abstract_handler* *newbuf = new abstract_handler*[capacity_];
					memcpy(newbuf, queue_, sizeof(abstract_handler*) * size_);
					if(queue_ != fixed_buffer_)
						delete [] queue_;
					queue_ = newbuf;
				}
				size_ += size;
			}

			void invoke(event_manager::handle_manager_type& handle_manager, const eventinfo& ei)
			{
				if(size_ != 0)
					std::for_each(queue_, queue_ + size_, inner_handler_invoker(handle_manager, ei));
			}
			std::size_t size() const {return size_;}
		private:
			abstract_handler * fixed_buffer_[10];
			abstract_handler** queue_;
			std::size_t size_;
			std::size_t capacity_;
		};
	}//end namespace inner_event_manager

	//callback_storage
	//@brief: This is a class defines a data structure about the event callbacks mapping
	struct callback_storage
	{
		typedef std::map<window, std::pair<std::vector<abstract_handler*>, std::vector<abstract_handler*> > > event_table_type;

		event_table_type table[event_code::end];
	};

	namespace nana_runtime
	{
		callback_storage callbacks;
	}

	abstract_handler::~abstract_handler(){}

	unsigned check::event_category[event_code::end] = {
		category::flags::widget,	//click
		category::flags::widget,	//dbl_click
		category::flags::widget,	//mouse_enter
		category::flags::widget,	//mouse_move
		category::flags::widget,	//mouse_leave
		category::flags::widget,	//mouse_down
		category::flags::widget,	//mouse_up
		category::flags::widget,	//mouse_wheel
		category::flags::widget,	//mouse_drop
		category::flags::widget,	//expose
		category::flags::widget,	//sizing
		category::flags::widget,	//size
		category::flags::widget,	//move
		category::flags::root,		//unload
		category::flags::widget,	//destroy
		category::flags::widget,	//focus
		category::flags::widget,	//key_down
		category::flags::widget,	//key_char
		category::flags::widget,	//key_up
		category::flags::widget,	//shortkey
		category::flags::super,		//elapse
						};

	//class event_manager
		//delete a handler
		void event_manager::umake(event_handle eh)
		{
			if(0 == eh) return;
			
			abstract_handler* abs_handler = reinterpret_cast<abstract_handler*>(eh);

			//Thread-Safe Required!
			threads::lock_guard<threads::recursive_mutex> lock(mutex_);

			if(handle_manager_.available(abs_handler))
			{
				this->write_off_bind(eh);

				std::vector<abstract_handler*>* v = abs_handler->container;
				std::vector<abstract_handler*>::iterator i = std::find(v->begin(), v->end(), abs_handler);
				if(i != v->end())
				{
					v->erase(i);
					if(v->empty())
					{
						callback_storage::event_table_type::iterator i = nana_runtime::callbacks.table[abs_handler->event_identifier].find(abs_handler->window);
						std::pair<std::vector<abstract_handler*>, std::vector<abstract_handler*> > & handler_pair = i->second;
						if(handler_pair.first.empty() && handler_pair.second.empty())
							nana_runtime::callbacks.table[abs_handler->event_identifier].erase(i);
					}
					handle_manager_(abs_handler);
				}
			}
		}

		//umake_handle_deleter_wrapper
		//@brief: a handle_manager overloaded operator() for deleting a handle,
		//it is a functor for easy-to-use to STL algorithms. but these algorithms
		//functor is value-copy semantic, but handle_manager could not be copyable,
		//so, employing umake_handle_deleter_wrapper for change it into refer-copy semantic
		class umake_handle_deleter_wrapper
		{
		public:
			umake_handle_deleter_wrapper(event_manager& evt_mgr, event_manager::handle_manager_type& mgr)
				:evt_mgr_(evt_mgr), mgr_(mgr)
			{}

			template<typename HandleType>
			void operator()(HandleType h)
			{
				evt_mgr_.write_off_bind(reinterpret_cast<nana::gui::event_handle>(h));
				mgr_(h);
			}
		private:
			event_manager&	evt_mgr_;
			event_manager::handle_manager_type& mgr_;
		};

		void event_manager::umake(window wd, bool only_for_drawer)
		{
			//Thread-Safe Required!
			threads::lock_guard<threads::recursive_mutex> lock(mutex_);

			typedef callback_storage::event_table_type table_t;

			table_t::iterator element;
			table_t *end = nana_runtime::callbacks.table + event_code::end;
			umake_handle_deleter_wrapper deleter_wrapper(*this, handle_manager_);
			for(table_t* i = nana_runtime::callbacks.table; i != end; ++i)
			{
				element = i->find(wd);
				if(element != i->end())
				{
					std::pair<std::vector<abstract_handler*>, std::vector<abstract_handler*> > & hdpair = element->second;
					std::for_each(hdpair.first.rbegin(), hdpair.first.rend(), deleter_wrapper);
					if(only_for_drawer)
					{
						//Check if user event container is empty, then remove the containers.
						if(0 == hdpair.second.size())
							i->erase(element);
						else
							hdpair.first.clear();
					}
					else
					{
						std::for_each(hdpair.second.rbegin(), hdpair.second.rend(), deleter_wrapper);
						i->erase(element);
					}
				}
			}

			if(false == only_for_drawer)
			{
				//delete the bind handler that the window had created.
				std::map<window, std::vector<event_handle> >::iterator itbind = bind_cont_.find(wd);
				if(itbind != bind_cont_.end())
				{
					for(std::vector<event_handle>::iterator i = itbind->second.begin(); i != itbind->second.end(); ++i)
						handle_manager_(reinterpret_cast<abstract_handler*>(*i));

					bind_cont_.erase(itbind);
				}
			}
		}

		bool event_manager::answer(event_code::t eventid, window wd, eventinfo& ei, event_kind::t evtkind)
		{
			if(eventid >= event_code::end)	return false;
			
			inner_event_manager::handler_queue queue;
			{
				//Thread-Safe Required!
				threads::lock_guard<threads::recursive_mutex> lock(mutex_);

				typedef callback_storage::event_table_type table_t;

				table_t* const evtobj = nana_runtime::callbacks.table + eventid;
				table_t::iterator element = evtobj->find(wd);
				
				if(element != evtobj->end()) //Test if the window installed event_id event
				{
					//copy all the handlers to the queue to avoiding dead-locking from the callback function that deletes the event handler
					switch(evtkind)
					{
					case event_kind::both:
						queue.copy(element->second.first);
						queue.copy(element->second.second);
						break;
					case event_kind::trigger:
						queue.copy(element->second.first);
						break;
					case event_kind::user:
						queue.copy(element->second.second);
						break;
					}
				}
			}
			ei.identifier = eventid;
			ei.window = wd;
			queue.invoke(handle_manager_, ei);
			return (queue.size() != 0);			
		}

		void event_manager::remove_trash_handle(unsigned tid)
		{
			handle_manager_.delete_trash(tid);
		}

		void event_manager::write_off_bind(event_handle eh)
		{
			if(eh && reinterpret_cast<abstract_handler*>(eh)->listener)
			{
				std::vector<event_handle> & v = bind_cont_[reinterpret_cast<abstract_handler*>(eh)->listener];
				std::vector<event_handle>::iterator i = std::find(v.begin(), v.end(), eh);
				
				if(i != v.end())
				{
					if(v.size() > 1)
						v.erase(i);
					else
						bind_cont_.erase(reinterpret_cast<abstract_handler*>(eh)->listener);
				}			
			}
		}

		std::size_t event_manager::size() const
		{
			return handle_manager_.size();
		}

		std::size_t event_manager::the_number_of_handles(window wd, event_code::t eventid, bool is_for_drawer)
		{
			if(eventid < event_code::end)
			{
				typedef callback_storage::event_table_type table_t;

				table_t* const evtobj = nana_runtime::callbacks.table + eventid;

				//Thread-Safe Required!
				threads::lock_guard<threads::recursive_mutex> lock(mutex_);

				table_t::iterator element = evtobj->find(wd);
				
				if(element != evtobj->end()) //Test if the window installed event_id event
				{
					return (is_for_drawer ? element->second.first : element->second.second).size();
				}
			}
			return 0;			
		}

		//_m_make
		//@brief: _m_make inserts a handler into callback storage with an given event_id and window
		//@param:eventid, the event type identifier
		//@param:wd, the triggering window
		//@param:abs_handler, the handle of event object handler
		//@param:drawer_handler, whether the event is installing for drawer or user callback
		event_handle event_manager::_m_make(event_code::t eventid, window wd, abstract_handler* abs_handler, bool drawer_handler, window listener)
		{
			if(abs_handler == 0)	return 0;

			//The bind event is only avaiable for non-drawer handler.
			if(drawer_handler)
				listener = 0;

			abs_handler->window = wd;
			abs_handler->listener = listener;
			abs_handler->event_identifier = eventid;
			
			{
				//Thread-Safe Required!
				threads::lock_guard<threads::recursive_mutex> lock(mutex_);

				abs_handler->container = (drawer_handler ?
												&(nana_runtime::callbacks.table[eventid][wd].first) :
												&(nana_runtime::callbacks.table[eventid][wd].second));

				abs_handler->container->push_back(abs_handler);
				handle_manager_.insert(abs_handler, 0);

				if(listener)
					bind_cont_[listener].push_back(reinterpret_cast<event_handle>(abs_handler));
			}

			if(drawer_handler == false)
				bedrock::instance().wd_manager.event_filter(reinterpret_cast<bedrock::core_window_t*>(wd), true, eventid);
			//call the event_register
			nana::detail::platform_spec::instance().event_register_filter(
					bedrock::instance().root(reinterpret_cast<bedrock::core_window_t*>(wd)),
					eventid);

			return reinterpret_cast<event_handle>(abs_handler);
		}
	//end class event_manager

}//mespace detail
}//end namespace gui
}//end namespace nana
