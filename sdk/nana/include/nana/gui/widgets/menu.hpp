/*
 *	A Menu implementation
 *	Copyright(C) 2009 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/widgets/menu.hpp
 */

#ifndef NANA_GUI_WIDGETS_MENU_HPP
#define NANA_GUI_WIDGETS_MENU_HPP
#include "widget.hpp"
#include <vector>
#include <nana/gui/timer.hpp>
#include <nana/pat/cloneable.hpp>

namespace nana{ namespace gui{
	namespace drawerbase
	{
		namespace menu
		{
			struct menu_type; //declaration

			struct menu_item_type
			{
				//class item_proxy
				//@brief: this class is used as parameter of menu event function.
				class item_proxy
					: nana::noncopyable
				{
				public:
					item_proxy(std::size_t, menu_item_type &);
					void enabled(bool v);
					bool enabled() const;
					std::size_t index() const;
				private:
					std::size_t index_;
					menu_item_type &item_;
				};

				typedef nana::functor<void(item_proxy&)> event_fn_t;

				//Default constructor initializes the item as a splitter
				menu_item_type();
				menu_item_type(const nana::string& text, const event_fn_t& f);

				struct
				{
					bool enabled:1;
					bool splitter:1;
					bool checked:1;
				}flags;

				menu_type		*sub_menu;
				nana::string	text;
				event_fn_t	functor;
				int				style;
				paint::image	image;
				mutable nana::char_t	hotkey;
			};

			struct menu_type
			{
				typedef std::vector<menu_item_type> item_container;
				typedef item_container::iterator iterator;
				typedef item_container::const_iterator const_iterator;

				std::vector<menu_type*>		owner;
				std::vector<menu_item_type>	items;
				unsigned max_pixels;
				unsigned item_pixels;
				nana::point gaps;
			};

			class renderer_interface
			{
			public:
				typedef nana::paint::graphics & graph_reference;

				struct state
				{
					enum t{normal, active};
				};

				struct attr
				{
					state::t item_state;
					bool enabled;
					bool checked;
					int check_style;
				};

				virtual ~renderer_interface() = 0;

				virtual void background(graph_reference, window) = 0;
				virtual void item(graph_reference, const nana::rectangle&, const attr&) = 0;
				virtual void item_image(graph_reference, const nana::point&, const paint::image&) = 0;
				virtual void item_text(graph_reference, const nana::point&, const nana::string&, unsigned text_pixels, const attr&) = 0;
				virtual void sub_arrow(graph_reference, const nana::point&, unsigned item_pixels, const attr&) = 0;

			};
		}//end namespace menu
	}//end namespace drawerbase

	class menu
		: private noncopyable
	{
		struct implement;
	public:
		enum check_t{check_none, check_option, check_highlight};

		typedef drawerbase::menu::renderer_interface renderer_interface;
		typedef drawerbase::menu::menu_item_type item_type;
		typedef item_type::item_proxy item_proxy;
		typedef item_type::event_fn_t event_fn_t;
		
		menu();
		~menu();
		void append(const nana::string& text, const event_fn_t& = event_fn_t());
		void append_splitter();
		void clear();
		void close();
		void image(std::size_t n, const paint::image&);
		void check_style(std::size_t n, check_t style);
		void checked(std::size_t n, bool check);
		bool checked(std::size_t n) const;
		void enabled(std::size_t n, bool enable);
		bool enabled(std::size_t n) const;
		void erase(std::size_t n);
		bool link(std::size_t n, menu& menu_obj);
		menu * link(std::size_t n);
		menu *create_sub_menu(std::size_t n);
		void popup(window, int x, int y, bool owner_menubar);
		void answerer(std::size_t n, const event_fn_t&);
		void destroy_answer(const nana::functor<void()>&);
		void gaps(const nana::point& pos);
		void goto_next(bool forward);
		bool goto_submen();
		bool exit_submenu();
		std::size_t size() const;
		int send_shortkey(nana::char_t key);
		menu& max_pixels(unsigned);
		unsigned max_pixels() const;

		menu& item_pixels(unsigned);
		unsigned item_pixels() const;

		void renderer(const pat::cloneable<renderer_interface>&);
		const pat::cloneable<renderer_interface>& renderer() const;
	private:
		void _m_destroy_menu_window();
	private:
		implement * impl_;
	};

	namespace detail
	{
		class popuper
		{
		public:
			popuper(menu&, mouse::t);
			popuper(menu&, window owner, const point&, mouse::t);
			void operator()(const eventinfo&);
		private:
			menu & mobj_;
			nana::gui::window owner_;
			bool take_mouse_pos_;
			nana::point pos_;
			mouse::t mouse_;
		};
	}

	detail::popuper menu_popuper(menu&, mouse::t = mouse::right_button);
	detail::popuper menu_popuper(menu&, window owner, const point&, mouse::t = mouse::right_button);
}//end namespace gui
}//end namespace nana
#endif

