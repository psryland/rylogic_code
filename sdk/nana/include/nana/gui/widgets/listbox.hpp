/*
 *	A List Box Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/widgets/listbox.hpp
 *
 */

#ifndef NANA_GUI_WIDGETS_LISTBOX_HPP
#define NANA_GUI_WIDGETS_LISTBOX_HPP
#include "widget.hpp"
#include <nana/pat/cloneable.hpp>
#include <nana/concepts.hpp>

namespace nana{ namespace gui{
	class listbox;
	namespace drawerbase
	{
		namespace listbox
		{
			typedef std::size_t size_type;

			//struct essence_t
			//@brief:	this struct gives many data for listbox,
			//			the state of the struct does not effect on member funcions, therefore all data members are public.
			struct essence_t;
			class drawer_header_impl;
			class drawer_lister_impl;

			class trigger: public nana::gui::drawer_trigger
			{
			public:
				typedef std::size_t size_type;

				trigger();
				~trigger();
				essence_t& essence();
				essence_t& essence() const;
				void draw();
			private:
				void _m_draw_border();
			private:
				void bind_window(widget_reference);

				void attached(graph_reference);
				void detached();
				void typeface_changed(graph_reference);
				void refresh(graph_reference);
				void mouse_move(graph_reference, const eventinfo&);
				void mouse_leave(graph_reference, const eventinfo&);
				void mouse_down(graph_reference, const eventinfo&);
				void mouse_up(graph_reference, const eventinfo&);
				void mouse_wheel(graph_reference, const eventinfo&);
				void dbl_click(graph_reference, const eventinfo&);
				void resize(graph_reference, const eventinfo&);
				void key_down(graph_reference, const eventinfo&);
			private:
				essence_t * essence_;
				drawer_header_impl *drawer_header_;
				drawer_lister_impl *drawer_lister_;
			};//end class trigger

			/// An interface that performances a translation between the object of T and an item of listbox.
			template<typename T>
			class resolver_interface
			{
			public:
				/// The type that will be resolved.
				typedef T target;

				/// The destructor
				virtual ~resolver_interface(){}

				virtual nana::string decode(std::size_t, const target&) const = 0;
				virtual void encode(target&, std::size_t, const nana::string&) const = 0;
			};

			template<typename T>
			struct resolver_proxy
			{
				pat::cloneable<resolver_interface<T> > res;

				resolver_proxy()
				{}

				resolver_proxy(const resolver_proxy& rhs)
					: res(rhs.res)
				{}

				~resolver_proxy()
				{
				}
			};	//end class resolver_proxy

			class item_proxy
				: public std::iterator<std::input_iterator_tag, item_proxy>
			{
			public:
				item_proxy();
				item_proxy(essence_t* ess, std::size_t cat, std::size_t pos);

				bool empty() const;

				item_proxy & check(bool ck);
				bool checked() const;

				item_proxy & select(bool);
				bool selected() const;

				item_proxy & bgcolor(nana::color_t);
				nana::color_t bgcolor() const;

				item_proxy& fgcolor(nana::color_t);
				nana::color_t fgcolor() const;

				std::size_t columns() const;
				item_proxy & text(std::size_t pos, const nana::string&);
				nana::string text(std::size_t pos) const;

				std::pair<std::size_t, std::size_t> pos() const;

				template<typename T>
				item_proxy & resolve_from(const T& t)
				{
					resolver_proxy<T> * proxy = _m_resolver().template get<resolver_proxy<T> >();
					if(0 == proxy)
						throw std::invalid_argument("Nana.Listbox.ItemProxy: the type passed to value() does not match the resolver.");
					
					resolver_proxy<T> * res = proxy->res.get();
					const std::size_t headers = columns();

					for(std::size_t i = 0; i < headers; ++i)
						text(i, res->decode(i, t));
					
					return *this;
				}

				template<typename T>
				void resolve_to(T& t) const
				{
					resolver_proxy<T>* proxy = _m_resolver().template get<resolver_proxy<T> >();
					if(0 == proxy)
						throw std::invalid_argument("Nana.Listbox.ItemProxy: the type passed to value() does not match the resolver.");

					resolver_interface<T> * res = proxy->res.get();
					const std::size_t headers = columns();
					for(std::size_t i = 0; i < headers; ++i)
						res->encode(t, i, text(i));
				}

				template<typename T>
				T* value_ptr() const
				{
					const nana::any * pany = _m_value();
					return (pany ? pany->template get<T>() : 0);
				}

				template<typename T>
				T & value() const
				{
					const nana::any * pany = _m_value();
					if(0 == pany)
						throw std::runtime_error("treebox::item_proxy.value<T>() is empty");

					T * p = pany->template get<T>();
					if(0 == p)
						throw std::runtime_error("treebox::item_proxy.value<T>() invalid type of value");
					return *p;
				}

				template<typename T>
				item_proxy & value(const T& t)
				{
					*_m_value(true) = t;
					return *this;
				}

				/// Behavior of Iterator's value_type
				bool operator==(const nana::string& s) const;
				bool operator==(const char * s) const;
				bool operator==(const wchar_t * s) const;

				/// Behavior of Iterator
				item_proxy & operator=(const item_proxy&);

				/// Behavior of Iterator
				item_proxy & operator++();

				/// Behavior of Iterator
				item_proxy	operator++(int);

				/// Behavior of Iterator
				item_proxy& operator*();

				/// Behavior of Iterator
				const item_proxy& operator*() const;

				/// Behavior of Iterator
				item_proxy* operator->();

				/// Behavior of Iterator
				const item_proxy* operator->() const;

				/// Behavior of Iterator
				bool operator==(const item_proxy&) const;

				/// Behavior of Iterator
				bool operator!=(const item_proxy&) const;

				//Undocumented method
				essence_t * _m_ess() const;
			private:
				const nana::any & _m_resolver() const;
				nana::any * _m_value(bool alloc_if_empty);
				const nana::any * _m_value() const;
				
			private:
				essence_t * ess_;
				std::size_t cat_;
				std::size_t pos_;
			};

			class cat_proxy
				: public std::iterator<std::input_iterator_tag, cat_proxy>
			{
			public:
				cat_proxy();
				cat_proxy(essence_t * ess, std::size_t pos);

				/// Append an item at end of the category
				template<typename T>
				item_proxy append(const T& t)
				{
					resolver_proxy<T>* proxy = _m_resolver().template get<resolver_proxy<T> >();
					if(proxy)
					{
						pat::cloneable<resolver_interface<T> > & res = proxy->res;
						std::size_t pos = size();
						push_back(res->decode(0, t));
						item_proxy ip(ess_, pos_, pos);
						const std::size_t headers = columns();
						for(std::size_t i = 1; i < headers; ++i)
							ip.text(i, res->decode(i, t));
						return ip;
					}
					return item_proxy();
				}

				std::size_t columns() const;

				/// Behavior of a container
				void push_back(const nana::string&);
				item_proxy begin() const;
				item_proxy end() const;
				item_proxy cbegin() const;
				item_proxy cend() const;

				item_proxy at(std::size_t pos) const; 

				std::size_t size() const;

				/// Behavior of Iterator
				cat_proxy& operator=(const cat_proxy&);

				/// Behavior of Iterator
				cat_proxy & operator++();

				/// Behavior of Iterator
				cat_proxy	operator++(int);

				/// Behavior of Iterator
				cat_proxy& operator*();

				/// Behavior of Iterator
				const cat_proxy& operator*() const;

				/// Behavior of Iterator
				cat_proxy* operator->();

				/// Behavior of Iterator
				const cat_proxy* operator->() const;

				/// Behavior of Iterator
				bool operator==(const cat_proxy&) const;

				/// Behavior of Iterator
				bool operator!=(const cat_proxy&) const;
			private:
				const nana::any & _m_resolver() const;
			private:
				essence_t * ess_;
				std::size_t pos_;
			};

			struct extra_events
			{
				nana::fn_group<void(item_proxy, bool)> checked;
				nana::fn_group<void(item_proxy, bool)> selected;
			};
		}//end namespace listbox
	}//end namespace drawerbase

	class listbox
		:	public widget_object<category::widget_tag, drawerbase::listbox::trigger>,
			public concepts::any_objective<drawerbase::listbox::size_type, 2>
	{
	public:
		typedef drawerbase::listbox::size_type size_type;
		typedef std::pair<size_type, size_type>	index_pair_t;
		typedef drawerbase::listbox::extra_events ext_event_type;
		typedef drawerbase::listbox::cat_proxy cat_proxy;
		typedef drawerbase::listbox::item_proxy	item_proxy;
		typedef std::vector<index_pair_t> selection;

		/// An interface that performances a translation between the object of T and an item of listbox.
		template<typename T>
		class resolver_interface
			: public drawerbase::listbox::resolver_interface<T>
		{
		};
	public:
		/// The defaut constructor without creating the widget.
		listbox();
		listbox(window, bool visible);
		listbox(window, const rectangle& = rectangle(), bool visible = true);

		ext_event_type & ext_event() const;

		void auto_draw(bool);
		void append_categ(const nana::string& text);
		void append_header(const nana::string&, unsigned width = 120);

		cat_proxy append(const nana::string& text);
		cat_proxy at(std::size_t pos) const;
		item_proxy at(std::size_t pos, std::size_t index) const;

		void insert(size_type cat, size_type index, const nana::string&);

		void checkable(bool);
		selection checked() const;

		void clear(size_type cat);
		void clear();
		void erase(size_type cat);
		void erase();
		item_proxy erase(item_proxy);

		template<typename Resolver>
		void resolver(const Resolver & res)
		{
			drawerbase::listbox::resolver_proxy<typename Resolver::target> proxy;
			proxy.res = pat::cloneable<drawerbase::listbox::resolver_interface<typename Resolver::target> >(res);
			_m_resolver(nana::any(proxy));
		}

		void set_sort_compare(size_type sub, nana::functor<bool(const nana::string&, nana::any*, const nana::string&, nana::any*, bool reverse)> strick_ordering);
		void sort_col(std::size_t col, bool reverse = false);
		std::size_t sort_col() const;
		void unsort();
		bool freeze_sort(bool freeze);

		selection selected() const;
		void show_header(bool);
		bool visible_header() const;
		void move_select(bool upwards);
		void icon(size_type cat, size_type index, const nana::paint::image&);
		nana::paint::image icon(size_type cat, size_type index) const;
		size_type size_categ() const;
		size_type size_item() const;
		size_type size_item(size_type cat) const;
	private:
		nana::any* _m_anyobj(size_type cat, size_type index, bool allocate_if_empty) const;
		void _m_resolver(const nana::any&);
		const nana::any & _m_resolver() const;
		std::size_t _m_headers() const;
	};
}//end namespace gui
}//end namespace nana

#endif
