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
#include <functional>

#if defined(STD_THREAD_NOT_SUPPORTED)
	#include <nana/std_mutex.hpp>
#else
	#include <mutex>
#endif

namespace nana
{
namespace gui
{
namespace detail
{

	//abstract_handler
	//abstract_handler provides a data structure that used in event manager, and it 
	//	provides an interface exec() to fire an event callback. Every event callback
	//	has to inherit from this abstract and implement the exec().
	//		This class is implemented inside, hence it's invisible for users.
	struct abstract_handler
	{
		virtual ~abstract_handler();
		virtual void exec(const eventinfo&) = 0;

		event_code						event_identifier;	//What event it is
		nana::gui::window				window;				//Which window creates this event
		nana::gui::window				listener;			//Which window listens this event
		std::vector<abstract_handler*>*	container;			//refer to the container which contains this object's address
	};

	//struct handler
	//an object of this class keeps a functor with a argument (const eventinfo&)
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
	//an object of this class keeps a functor without any argument
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
		enum class event_kind
		{
			both, trigger, user
		};

		typedef nana::gui::detail::handle_manager<abstract_handler*, nana::null_type> handle_manager_type;

		template<typename Function>
		event_handle make_for_drawer(event_code evtid, window wd, category::flags categ, Function function)
		{
			return (check::accept(evtid, categ) ?
				_m_make(evtid, wd, handler_factory<Function>::build(function), true, nullptr) : nullptr);
		}

		template<typename Function>
		event_handle make(event_code evtid, window wd, category::flags categ, Function function)
		{
			return  (check::accept(evtid, categ) ?
				_m_make(evtid, wd, handler_factory<Function>::build(function), false, nullptr) : nullptr);
		}

		template<typename Function>
		event_handle bind(event_code evtid, window trig_wd, window listener, category::flags categ, Function function)
		{
			return (check::accept(evtid, categ) ?
				_m_make(evtid, trig_wd, handler_factory<Function>::build(function), false, listener) : 0);
		}

		//delete a handler
		void umake(event_handle);
		//umake
		//delete user event and drawer event handlers of a specified window.
		//If only_for_drawer is true, it only deletes user events.
		void umake(window, bool only_for_drawer);
		bool answer(event_code, window, eventinfo&, event_kind);
		void remove_trash_handle(unsigned tid);

		void write_off_bind(event_handle);

		std::size_t size() const;
		std::size_t the_number_of_handles(window, event_code, bool is_for_drawer);
	private:
		template<bool IsBind, typename Functor>
		struct factory_proxy
		{
			static abstract_handler* build(Functor & f)
			{
				return (new handler<std::function<void(const eventinfo&)>, true>(f));
			}
		};

		template<typename Functor>
		struct factory_proxy<false, Functor>
		{
			static abstract_handler * build(Functor& f)
			{
				return build(f, &Functor::operator());
			}

			template<typename ReturnType>
			static abstract_handler* build(Functor& f, ReturnType (Functor::*)())
			{
				return (new handler<Functor, false>(f));
			}

			template<typename ReturnType>
			static abstract_handler* build(Functor& f, ReturnType (Functor::*)() const)
			{
				return (new handler<Functor, false>(f));
			}

			static abstract_handler* build(Functor& f, void (Functor::*)())
			{	//build for non-args
				return new handler<Functor, false>(f);
			}

			static abstract_handler* build(Functor& f, void (Functor::*)() const)
			{	//build for non-args
				return new handler<Functor, false>(f);
			}

			static abstract_handler* build(Functor& f, void (Functor::*)(const eventinfo&))
			{	//buid for args
				return new handler<Functor, true>(f);
			}

			static abstract_handler* build(Functor& f, void (Functor::*)(const eventinfo&) const)
			{	//buid for args
				return new handler<Functor, true>(f);
			}
		};

		template<typename Functor>
		class handler_factory
		{
		public:
			static abstract_handler* build(Functor& f)
			{
				return factory_proxy<std::is_bind_expression<Functor>::value, Functor>::build(f);
			}
		};

		template<typename F>
		class handler_factory<std::function<F> >
		{
		public:
			static abstract_handler * build(const std::function<void()>& f)
			{
				return new handler<std::function<F>, false>(f);
			}

			static abstract_handler * build(const std::function<void(const eventinfo&)> & f)
			{
				return new handler<std::function<F>, true>(f);
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
		event_handle _m_make(event_code, window, abstract_handler* abs_handler, bool drawer_handler, window listener);
	private:
		mutable std::recursive_mutex mutex_;
		handle_manager_type handle_manager_;
		std::map<window, std::vector<event_handle> >	bind_cont_;
	};
}//end namespace detail


}//end namespace gui
}//end namespace nana

#endif
