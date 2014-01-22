/*
 *	Event Manager Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/detail/event_manager.hpp
 */

#ifndef NANA_GUI_DETAIL_EVENT_MANAGER_HPP
#define NANA_GUI_DETAIL_EVENT_MANAGER_HPP

#include <vector>
#include <map>
#include "../basis.hpp"
#include "eventinfo.hpp"
#include "handle_manager.hpp"

namespace nana
{
namespace gui
{
namespace detail
{

	//abstract_handler
	//@brief: abstract_handler provides a data structure that used in event manager, and it 
	//	provides an interface exec() to fire an event callback. Every event callback
	//	has to inherit from this abstract and implement the exec().
	//		This class is implemented inside, hence it's invisible for users.
	struct abstract_handler
	{
		virtual ~abstract_handler();
		virtual void exec(const eventinfo&) = 0;

		unsigned						event_identifier;	//What event it is
		nana::gui::window				window;				//Which window creates this event
		nana::gui::window				listener;			//Which window listens this event
		std::vector<abstract_handler*>*	container;			//refer to the container which contains this object's address
	};

	//struct handler
	//@brief: an object of this class keeps a functor with a argument (const eventinfo&)
	template<typename Functor, bool HasArg>
	struct handler : public abstract_handler
	{
		handler(const Functor& f)
			:functor_(f)
		{}

		void exec(const eventinfo& ei)
		{
			functor_(ei);
		}

		Functor functor_;
	};

	//struct handler
	//@brief: an object of this class keeps a functor without any argument
	template<typename Functor>
	struct handler<Functor, false> : public abstract_handler
	{
		handler(const Functor& f)
			:functor_(f)
		{}

		void exec(const eventinfo& ei)
		{
			functor_();
		}

		Functor functor_;
	};

	//event_manager
	class event_manager
	{
	public:
		struct event_kind
		{
			enum t{both, trigger, user};
		};

		typedef nana::gui::detail::handle_manager<abstract_handler*, nana::null_type> handle_manager_type;

		template<typename Function>
		event_handle make_for_drawer(unsigned evtid, window wd, unsigned category, Function function)
		{
			return (event_tag::accept(evtid, category) ?
				_m_make(evtid, wd, handler_factory<Function>::build(function), true, 0) : 0);
		}

		template<typename Function>
		event_handle make(unsigned evtid, window wd, unsigned category, Function function)
		{
			return  (event_tag::accept(evtid, category) ?
				_m_make(evtid, wd, handler_factory<Function>::build(function), false, 0) : 0);
		}

		template<typename Function>
		event_handle bind(unsigned evtid, window trig_wnd, window listener, unsigned category, Function function)
		{
			return (event_tag::accept(evtid, category) ?
				_m_make(evtid, trig_wnd, handler_factory<Function>::build(function), false, listener) : 0);
		}

		//delete a handler
		void umake(event_handle);
		//delete user event and drawer event handlers of a specified window.
		void umake(window, bool only_for_drawer);
		bool answer(unsigned eventid, window, eventinfo&, event_kind::t);
		void remove_trash_handle(unsigned tid);

		void write_off_bind(event_handle);

		std::size_t size() const;
		std::size_t the_number_of_handles(window, unsigned event_id, bool is_for_drawer);
	private:
		template<typename Functor>
		class handler_factory
		{
		public:
			static abstract_handler* build(Functor& f)
			{
				return _m_build(f, &Functor::operator());
			}
		private:
			template<typename ReturnType>
			static abstract_handler* _m_build(Functor& f, ReturnType (Functor::*)())
			{
				return (new handler<Functor, false>(f));
			}

			template<typename ReturnType>
			static abstract_handler* _m_build(Functor& f, ReturnType (Functor::*)() const)
			{
				return (new handler<Functor, false>(f));
			}

			static abstract_handler* _m_build(Functor& f, void (Functor::*)())
			{	//build for non-args
				return new handler<Functor, false>(f);
			}

			static abstract_handler* _m_build(Functor& f, void (Functor::*)() const)
			{	//build for non-args
				return new handler<Functor, false>(f);
			}

			static abstract_handler* _m_build(Functor& f, void (Functor::*)(const eventinfo&))
			{	//buid for args
				return new handler<Functor, true>(f);
			}

			static abstract_handler* _m_build(Functor& f, void (Functor::*)(const eventinfo&) const)
			{	//buid for args
				return new handler<Functor, true>(f);
			}
		};

		template<typename R>
		class handler_factory<R(*)()>
		{
		public:
			static abstract_handler* build(R(*f)())
			{
				return new handler<R(*)(), false>(f);
			}
		};

		template<typename R>
		class handler_factory<R(*)(const eventinfo&)>
		{
		public:
			static abstract_handler* build(R(*f)(const eventinfo&))
			{
				return new handler<R(*)(const eventinfo&), true>(f);
			}
		};
	private:
		/*
		 * _m_make
		 * @brief: _m_make insert a handler into callback storage through an given event_id and window
		 * @eventid, the event type identifier
		 * @wnd, the triggering window
		 * @abs_handler, the handle of event object handler
		 * @drawer_handler, whether the event is installing for drawer or user callback
		 */
		event_handle _m_make(unsigned event_id, window wnd, abstract_handler* abs_handler, bool drawer_handler, window listener = 0);
	private:
		handle_manager_type handle_manager_;
		std::map<window, std::vector<event_handle> >	bind_cont_;
		static nana::threads::recursive_mutex mutex_;
	};
}//end namespace detail


}//end namespace gui
}//end namespace nana

#endif
