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
#include <map>
#include <algorithm>
#include <nana/gui/detail/event_manager.hpp>

namespace nana
{
namespace gui
{
namespace detail
{
	class handle_queue
	{
	public:
		handle_queue()
			:queue_(fixed_buffer_), size_(0), capacity_(10)
		{}

		~handle_queue()
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
			std::size_t size = container.size();

			//Firstly check whether the capacity is enough for the size
			if(capacity_ - size_ < size)
			{
				capacity_ = size_ + size;
				abstract_handler* *newbuf = new abstract_handler*[capacity_];
				memcpy(newbuf, queue_, sizeof(abstract_handler*) * size_);
				if(queue_ != fixed_buffer_)
					delete [] queue_;
				queue_ = newbuf;
			}

			auto iter = container.cbegin();
			abstract_handler * *i = queue_ + size_;
			abstract_handler * * const end = i + size;
			while(i < end)
				*i++ = *iter++;

			size_ += size;
		}

		void invoke(event_manager::handle_manager_type& handle_manager, const eventinfo& ei)
		{
			for(auto i = queue_, end = queue_ + size_; i != end; ++i)
			{
				if(handle_manager.available(*i))
					(*i)->exec(ei);
			}
		}
		std::size_t size() const {return size_;}
	private:
		abstract_handler * fixed_buffer_[10];
		abstract_handler** queue_;
		std::size_t size_;
		std::size_t capacity_;
	};//end class handle_queue

	//callback_storage
	//@brief: This is a class defines a data structure about the event callbacks mapping
	struct callback_storage
	{
		typedef std::map<window, std::pair<std::vector<abstract_handler*>, std::vector<abstract_handler*> > > event_table_type;

		event_table_type table[static_cast<int>(event_code::end)];
	};

	namespace nana_runtime
	{
		callback_storage callbacks;
	}

	abstract_handler::~abstract_handler(){}

	category::flags check::event_category[static_cast<int>(event_code::end)] = {
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
			if(nullptr == eh) return;

			abstract_handler* abs_handler = reinterpret_cast<abstract_handler*>(eh);

			//Thread-Safe Required!
			std::lock_guard<decltype(mutex_)> lock(mutex_);

			if(handle_manager_.available(abs_handler))
			{
				write_off_bind(eh);

				auto v = abs_handler->container;
				auto i = std::find(v->begin(), v->end(), abs_handler);
				if(i != v->cend())
				{
					v->erase(i);
					if(v->empty())
					{
						auto & evt = nana_runtime::callbacks.table[static_cast<int>(abs_handler->event_identifier)];
						auto i_evt = evt.find(abs_handler->window);
						auto & pair_v  = i_evt->second;
						if(pair_v.first.empty() && pair_v.second.empty())
							evt.erase(i_evt);
					}
				}
				handle_manager_(abs_handler);
			}
		}

		void event_manager::umake(window wd, bool only_for_drawer)
		{
			//Thread-Safe Required!
			std::lock_guard<decltype(mutex_)> lock(mutex_);

			auto deleter_wrapper = [this](abstract_handler * handler)
			{
				this->write_off_bind(reinterpret_cast<event_handle>(handler));
				this->handle_manager_(handler);
			};

			for (auto i = std::begin(nana_runtime::callbacks.table), end = std::end(nana_runtime::callbacks.table); i != end; ++i)
			{
				auto element = i->find(wd);
				if(element != i->end())
				{
					auto & hdpair = element->second;
					std::for_each(hdpair.first.rbegin(), hdpair.first.rend(), deleter_wrapper);
					if(only_for_drawer)
					{
						if(hdpair.second.size())
							hdpair.first.clear();
						else
							i->erase(element);
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
				auto bi = bind_cont_.find(wd);
				if(bi != bind_cont_.end())
				{
					for(auto handler : bi->second)
						handle_manager_(reinterpret_cast<abstract_handler*>(handler));
					bind_cont_.erase(bi);
				}
			}
		}

		bool event_manager::answer(event_code eventid, window wd, eventinfo& ei, event_kind evtkind)
		{
			if(eventid >= event_code::end)	return false;

			auto * const evtobj = nana_runtime::callbacks.table + static_cast<int>(eventid);
			handle_queue queue;
			{
				//Thread-Safe Required!
				std::lock_guard<decltype(mutex_)> lock(mutex_);

				auto element = evtobj->find(wd);
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
				auto & v = bind_cont_[reinterpret_cast<abstract_handler*>(eh)->listener];
				auto i = std::find(v.begin(), v.end(), eh);
				if(i != v.cend())
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

		std::size_t event_manager::the_number_of_handles(window wd, event_code eventid, bool is_for_drawer)
		{
			if (eventid < event_code::end)
			{
				auto* const evtobj = nana_runtime::callbacks.table + static_cast<int>(eventid);

				//Thread-Safe Required!
				std::lock_guard<decltype(mutex_)> lock(mutex_);
				auto element = evtobj->find(wd);
				if(element != evtobj->end()) //Test if the window installed event_id event
					return (is_for_drawer ? element->second.first : element->second.second).size();
			}
			return 0;
		}

		//_m_make
		//@brief: _m_make inserts a handler into callback storage with an given event_id and window
		//@param:eventid, the event type identifier
		//@param:wd, the triggering window
		//@param:abs_handler, the handle of event object handler
		//@param:drawer_handler, whether the event is installing for drawer or user callback
		//@param:listener, a listener for the event, it is ignored when drawer_handler is true
		event_handle event_manager::_m_make(event_code eventid, window wd, abstract_handler* abs_handler, bool drawer_handler, window listener)
		{
			if(nullptr == abs_handler)	return nullptr;

			//The bind event is only avaiable for non-drawer handler.
			if(drawer_handler)
				listener = nullptr;

			abs_handler->window = wd;
			abs_handler->listener = listener;
			abs_handler->event_identifier = eventid;

			{
				//Thread-Safe Required!
				std::lock_guard<decltype(mutex_)> lock(mutex_);
				abs_handler->container = (drawer_handler ?
												&(nana_runtime::callbacks.table[static_cast<int>(eventid)][wd].first) :
												&(nana_runtime::callbacks.table[static_cast<int>(eventid)][wd].second));

				abs_handler->container->push_back(abs_handler);
				handle_manager_.insert(abs_handler, 0);

				if(listener)
					bind_cont_[listener].push_back(reinterpret_cast<event_handle>(abs_handler));
			}

			if(drawer_handler == false)
				bedrock::instance().wd_manager.event_filter(reinterpret_cast<bedrock::core_window_t*>(wd), true, eventid);
			//call the event_register
			nana::detail::platform_spec::instance().event_register_filter(
					bedrock::instance().root(reinterpret_cast<bedrock::core_window_t*>(wd)), eventid);
			return reinterpret_cast<event_handle>(abs_handler);
		}
	//end class event_manager
}//mespace detail
}//end namespace gui
}//end namespace nana
