/*
 *	The fundamental widget class implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/widgets/widget.hpp
 */

#ifndef NANA_GUI_WIDGET_HPP
#define NANA_GUI_WIDGET_HPP
#include <nana/traits.hpp>
#include "../basis.hpp"
#include "../programming_interface.hpp"
#include <nana/gui/detail/drawer.hpp>
#include <nana/gui/layout_utility.hpp>
#include <nana/functor.hpp>
#include <functional>

namespace nana
{
namespace gui
{
	class drawer_trigger;

	//class widget
	//@brief: this is a abstract class for defining the capacity interface.
	class widget
		: nana::noncopyable, nana::nonmovable
	{
		typedef void(*dummy_bool_type)(widget* (*)(const widget&));
	public:
		virtual ~widget();
		virtual window handle() const = 0;
		bool empty() const;
		void close();

		window parent() const;

		nana::string caption() const;
		void caption(const nana::string& str);

		void cursor(nana::gui::cursor);
		nana::gui::cursor cursor() const;

		void typeface(const nana::paint::font& font);
		nana::paint::font typeface() const;

		bool enabled() const;
		void enabled(bool);

		void focus();
		bool focused() const;

		void show();
		void hide();
		bool visible() const;

		nana::size size() const;
		void size(unsigned width, unsigned height);
		
		nana::point pos() const;
		void move(int x, int y);
		void move(int x, int y, unsigned width, unsigned height);

		void foreground(nana::color_t);
		nana::color_t foreground() const;
		void background(nana::color_t);
		nana::color_t background() const;

		template<typename Event, typename Function>
		event_handle make_event(Function function) const
		{
			return API::make_event<Event, Function>(this->handle(), function);
		}

		template<typename Event, typename Class, typename Concept>
		event_handle make_event(Class& obj, void (Concept::*mf)(const eventinfo&)) const
		{
			return API::make_event<Event>(this->handle(), std::bind(mf, &obj, std::placeholders::_1));
		}

		template<typename Event, typename Class, typename Concept>
		event_handle make_event(Class& obj, void (Concept::*mf)()) const
		{
			return API::make_event<Event>(this->handle(), std::bind(mf, &obj));
		}

		template<typename Event, typename Function>
		event_handle bind_event(widget& wdg, Function function) const
		{
			return API::bind_event<Event, Function>(wdg.handle(), this->handle(), function);
		}

		template<typename Event, typename Class, typename Concept>
		event_handle bind_event(widget& wdg, Class& obj, void (Concept::*mf)(const eventinfo&)) const
		{
			return API::bind_event<Event>(wdg.handle(), this->handle(), std::bind(mf, &obj, std::placeholders::_1));
		}

		void umake_event(event_handle eh) const;
		widget& tooltip(const nana::string&);

		operator dummy_bool_type() const;
		operator window() const;
	protected:
		//protected members, a derived class must call this implementation if it overrides an implementation
		virtual void _m_complete_creation();

		virtual nana::string _m_caption() const;
		virtual void _m_caption(const nana::string&);
		virtual nana::gui::cursor _m_cursor() const;
		virtual void _m_cursor(nana::gui::cursor);
		virtual void _m_close();
		virtual bool _m_enabled() const;
		virtual void _m_enabled(bool);
		virtual bool _m_show(bool);
		virtual bool _m_visible() const;
		virtual void _m_size(unsigned width, unsigned height);
		virtual void _m_move(int x, int y);
		virtual void _m_move(int x, int y, unsigned width, unsigned height);

		virtual void _m_typeface(const nana::paint::font& font);
		virtual nana::paint::font _m_typeface() const;

		virtual void _m_foreground(nana::color_t);
		virtual nana::color_t _m_foreground() const;
		virtual void _m_background(nana::color_t);
		virtual nana::color_t _m_background() const;
	};

	//class widget_object
	//@brief: defaultly a widget_tag
	template<typename Category, typename DrawerTrigger>
	class widget_object: public widget
	{
	protected:
		typedef DrawerTrigger drawer_trigger_t;
	public:
		widget_object()
			:handle_(nullptr)
		{}

		~widget_object()
		{
			if(handle_)
				API::close_window(handle_);
		}

		bool create(window wd, bool visible)
		{
			return create(wd, rectangle(), visible);
		}

		bool create(window wd, const rectangle & r = rectangle(), bool visible = true)
		{
			if(wd && this->empty())
			{
				handle_ = API::dev::create_widget(wd, r);
				API::dev::attach_signal(handle_, *this, &widget_object::signal);
				API::dev::attach_drawer(*this, trigger_);
				if(visible)
					API::show_window(handle_, true);
				
				this->_m_complete_creation();
			}
			return (this->empty() == false);
		}

		window handle() const
		{
			return handle_;
		}
	protected:
		DrawerTrigger& get_drawer_trigger()
		{
			return trigger_;
		}

		const DrawerTrigger& get_drawer_trigger() const
		{
			return trigger_;
		}
	private:
		void signal(detail::signals::code code, const detail::signals& sig)
		{
			typedef detail::signals::code codes;
			switch(code)
			{
			case codes::caption:
				this->_m_caption(sig.info.caption);
				break;
			case codes::read_caption:
				*sig.info.str = this->_m_caption();
				break;
			case codes::destroy:
				handle_ = nullptr;
				break;
			default:
				break;
			}
		}
	private:
		window handle_;
		DrawerTrigger trigger_;
	};//end class widget_object

	template<typename DrawerTrigger>
	class widget_object<category::lite_widget_tag, DrawerTrigger>: public widget
	{
	protected:
		typedef DrawerTrigger drawer_trigger_t;
	public:

		widget_object()
			:handle_(nullptr)
		{}

		~widget_object()
		{
			if(handle_)
				API::close_window(handle_);
		}

		bool create(window wd, bool visible)
		{
			return create(wd, rectangle(), visible);
		}

		bool create(window wd, const rectangle& r = rectangle(), bool visible = true)
		{
			if(wd && this->empty())
			{
				handle_ = API::dev::create_lite_widget(wd, r);
				if(visible)
					API::show_window(handle_, true);
				this->_m_complete_creation();
			}
			return (this->empty() == false);
		}

		window handle() const
		{
			return handle_;
		}
	private:
		void signal(detail::signals::code code, const detail::signals& sig)
		{
			typedef detail::signals::code codes;
			switch(code)
			{
			case codes::caption:
				this->_m_caption(sig.info.caption);
				break;
			case codes::read_caption:
				*sig.info.str = this->_m_caption();
				break;
			case codes::destroy:
				handle_ = nullptr;
				break;
			default:
				break;
			}
		}
	private:
		window handle_;
	};//end class widget_object

	template<typename DrawerTrigger>
	class widget_object<category::root_tag, DrawerTrigger>: public widget
	{
	protected:
		typedef DrawerTrigger drawer_trigger_t;
	public:

		widget_object()
			:handle_(API::dev::create_window(nullptr, false, API::make_center(300, 150), appearance()))
		{
			_m_bind_and_attach();
		}

		widget_object(const rectangle& r, const appearance& apr = appearance())
			: handle_(API::dev::create_window(nullptr, false, r, apr))
		{
			_m_bind_and_attach();
		}

		widget_object(window owner, bool nested, const rectangle& r = rectangle(), const appearance& apr = appearance())
			:	handle_(API::dev::create_window(owner, nested, r, apr))
		{
			_m_bind_and_attach();
		}

		~widget_object()
		{
			if(handle_)
				API::close_window(handle_);
		}

		void activate()
		{
			API::activate_window(handle_);
		}

		void bring_to_top()
		{
			API::bring_to_top(handle_);
		}

		window handle() const
		{
			return handle_;
		}

		native_window_type native_handle() const
		{
			return API::root(handle_);
		}

		window owner() const
		{
			return API::get_owner_window(handle_);
		}

		void icon(const nana::paint::image& ico)
		{
			API::window_icon(handle_, ico);
		}

		void restore()
		{
			API::restore_window(handle_);
		}

		void zoom(bool ask_for_max)
		{
			API::zoom_window(handle_, ask_for_max);
		}

		bool is_zoomed(bool ask_for_max) const
		{
			return API::is_window_zoomed(handle_, ask_for_max);
		}
	protected:
		DrawerTrigger& get_drawer_trigger()
		{
			return trigger_;
		}

		const DrawerTrigger& get_drawer_trigger() const
		{
			return trigger_;
		}
	private:
		void signal(detail::signals::code code, const detail::signals& sig)
		{
			typedef detail::signals::code codes;
			switch(code)
			{
			case codes::caption:
				this->_m_caption(sig.info.caption);
				break;
			case codes::read_caption:
				*sig.info.str = this->_m_caption();
				break;
			case codes::destroy:
				handle_ = nullptr;
				break;
			default:
				break;
			}
		}

		void _m_bind_and_attach()
		{
			API::dev::attach_signal(handle_, *this, &widget_object::signal);
			API::dev::attach_drawer(*this, trigger_);
		}
	private:
		window handle_;
		DrawerTrigger trigger_;
	};//end class widget_object<root_tag>

	template<typename Drawer>
	class widget_object<category::frame_tag, Drawer>: public widget{};

	template<>
	class widget_object<category::frame_tag, int>: public widget
	{
	protected:
		typedef int drawer_trigger_t;
	public:
		widget_object()
			:handle_(nullptr)
		{}

		~widget_object()
		{
			if(handle_)
				API::close_window(handle_);
		}

		bool create(window wd, bool visible)
		{
			return create(wd, rectangle(), visible);
		}

		bool create(window wd, const rectangle& r = rectangle(), bool visible = true)
		{
			if(wd && this->empty())
			{
				handle_ = API::dev::create_frame(wd, r);
				API::dev::attach_signal(handle_, *this, &widget_object::signal);
				API::show_window(handle_, visible);
				this->_m_complete_creation();
			}
			return (this->empty() == false);
		}

		window handle() const
		{
			return handle_;
		}
	private:
		virtual drawer_trigger* get_drawer_trigger()
		{
			return nullptr;
		}

		void signal(detail::signals::code code, const detail::signals& sig)
		{
			typedef detail::signals::code codes;
			switch(code)
			{
			case codes::caption:
				this->_m_caption(sig.info.caption);
				break;
			case codes::read_caption:
				*sig.info.str = this->_m_caption();
				break;
			case codes::destroy:
				handle_ = nullptr;
				break;
			default:
				break;
			}
		}
	private:
		window handle_;
	};//end class widget_object<category::frame_tag>
}//end namespace gui
}//end namespace nana
#endif

