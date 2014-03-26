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

			struct index_pair
			{
				size_type cat;	//The pos of category
				size_type item;	//the pos of item in a category.

				index_pair()
					:	cat(0), item(0)
				{}

				index_pair(size_type cat_pos, size_type item_pos)
					:	cat(cat_pos),
						item(item_pos)
				{}

				bool empty() const
				{
					return (npos == cat);
				}

				void set_both(size_type n)
				{
					cat = item = n;
				}

				bool is_category() const
				{
					return (npos != cat && npos == item);
				}

				bool is_item() const
				{
					return (npos != cat && npos != item);
				}

				bool operator==(const index_pair& r) const
				{
					return (r.cat == cat && r.item == item);
				}

				bool operator!=(const index_pair& r) const
				{
					return !this->operator==(r);
				}

				bool operator>(const index_pair& r) const
				{
					return (cat > r.cat) || (cat == r.cat && item > r.item);
				}
			};

			typedef std::vector<index_pair> selection;

			//struct essence_t
			//@brief:	this struct gives many data for listbox,
			//			the state of the struct does not effect on member funcions, therefore all data members are public.
			struct essence_t;
			class drawer_header_impl;
			class drawer_lister_impl;

			class trigger: public drawer_trigger
			{
			public:
				trigger();
				~trigger();
				essence_t& essence();
				essence_t& essence() const;
				void draw();
			private:
				void _m_draw_border();
			private:
				void attached(widget_reference, graph_reference)	override;
				void detached()	override;
				void typeface_changed(graph_reference)	override;
				void refresh(graph_reference)	override;
				void mouse_move(graph_reference, const eventinfo&)	override;
				void mouse_leave(graph_reference, const eventinfo&)	override;
				void mouse_down(graph_reference, const eventinfo&)	override;
				void mouse_up(graph_reference, const eventinfo&)	override;
				void mouse_wheel(graph_reference, const eventinfo&)	override;
				void dbl_click(graph_reference, const eventinfo&)	override;
				void resize(graph_reference, const eventinfo&)		override;
				void key_down(graph_reference, const eventinfo&)	override;
				void key_char(graph_reference, const eventinfo&)	override;	//Deal with whitespace
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

				virtual nana::string decode(size_type, const target&) const = 0;
				virtual void encode(target&, size_type, const nana::string&) const = 0;
			};

			template<typename T>
			struct resolver_proxy
			{
				pat::cloneable<resolver_interface<T>> res;

				resolver_proxy()
				{}

				resolver_proxy(const resolver_proxy& rhs)
					: res(rhs.res)
				{}

				~resolver_proxy()
				{
				}
			};


			class item_proxy
				: public std::iterator<std::input_iterator_tag, item_proxy>
			{
			public:
				item_proxy();
				item_proxy(essence_t*, const index_pair&);

				bool empty() const;

				item_proxy & check(bool ck);
				bool checked() const;

				item_proxy & select(bool);
				bool selected() const;

				item_proxy & bgcolor(nana::color_t);
				nana::color_t bgcolor() const;

				item_proxy& fgcolor(nana::color_t);
				nana::color_t fgcolor() const;

				index_pair pos() const;

				size_type columns() const;
				item_proxy & text(size_type col, const nana::string&);
				item_proxy & text(size_type col, nana::string&&);
				nana::string text(size_type col) const;

				template<typename T>
				item_proxy & resolve_from(const T& t)
				{
					auto proxy = _m_resolver().template get<resolver_proxy<T> >();
					if(nullptr == proxy)
						throw std::invalid_argument("Nana.Listbox.ItemProxy: the type passed to value() does not match the resolver.");
					
					auto * res = proxy->res.get();
					auto headers = columns();

					for(size_type i = 0; i < headers; ++i)
						text(i, res->decode(i, t));
					
					return *this;
				}

				template<typename T>
				void resolve_to(T& t) const
				{
					auto proxy = _m_resolver().template get<resolver_proxy<T> >();
					if(nullptr == proxy)
						throw std::invalid_argument("Nana.Listbox.ItemProxy: the type passed to value() does not match the resolver.");
					
					auto * res = proxy->res.get();
					auto headers = columns();
					for(size_type i = 0; i < headers; ++i)
						res->encode(t, i, text(i));
				}

				template<typename T>
				T* value_ptr() const
				{
					auto * pany = _m_value();
					return (pany ? pany->template get<T>() : nullptr);
				}

				template<typename T>
				T & value() const
				{
					auto * pany = _m_value();
					if(nullptr == pany)
						throw std::runtime_error("treebox::item_proxy.value<T>() is empty");

					T * p = pany->template get<T>();
					if(nullptr == p)
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
				index_pair	pos_;
			};

			class cat_proxy
				: public std::iterator<std::input_iterator_tag, cat_proxy>
			{
			public:
				cat_proxy();
				cat_proxy(essence_t * ess, size_type pos);

				/// Append an item at end of the category
				template<typename T>
				item_proxy append(const T& t)
				{
					auto proxy = _m_resolver().template get<resolver_proxy<T> >();
					if(proxy)
					{
						auto & res = proxy->res;
						push_back(res->decode(0, t));

						item_proxy ip(ess_, index_pair(pos_, size() - 1));
						auto headers = columns();
						for(size_type i = 1; i < headers; ++i)
							ip.text(i, res->decode(i, t));
						return ip;
					}
					return item_proxy();
				}

				size_type columns() const;

				/// Behavior of a container
				void push_back(const nana::string&);
				void push_back(nana::string&&);

				item_proxy begin() const;
				item_proxy end() const;
				item_proxy cbegin() const;
				item_proxy cend() const;

				item_proxy at(size_type pos) const;
				item_proxy back() const;

				size_type size() const;

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
				essence_t*	ess_;
				size_type	pos_;
			};

			struct extra_events
			{
				nana::fn_group<void(item_proxy, bool)> checked;
				nana::fn_group<void(item_proxy, bool)> selected;
			};
		}
	}//end namespace drawerbase

	class listbox
		:	public widget_object<category::widget_tag, drawerbase::listbox::trigger>,
			public concepts::any_objective<drawerbase::listbox::size_type, 2>
	{
	public:
		typedef drawerbase::listbox::size_type	size_type;
		typedef drawerbase::listbox::index_pair	index_pair;
		typedef drawerbase::listbox::extra_events	ext_event_type;
		typedef drawerbase::listbox::cat_proxy	cat_proxy;
		typedef drawerbase::listbox::item_proxy	item_proxy;
		typedef drawerbase::listbox::selection	selection;

		/// An interface that performances a translation between the object of T and an item of listbox.
		template<typename T>
		class resolver_interface
			: public drawerbase::listbox::resolver_interface<T>
		{
		};
	public:
		listbox();
		listbox(window, bool visible);
		listbox(window, const rectangle& = rectangle(), bool visible = true);

		ext_event_type& ext_event() const;

		void auto_draw(bool);

		void append_header(const nana::string&, unsigned width = 120);

		cat_proxy append(const nana::string& text);
		cat_proxy at(size_type pos) const;
		item_proxy at(const index_pair&) const;

		void insert(const index_pair&, const nana::string&);
		void insert(const index_pair&, nana::string&&);

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

		void set_sort_compare(size_type col, std::function<bool(const nana::string&, nana::any*,
															const nana::string&, nana::any*, bool reverse)> strick_ordering);

		void sort_col(size_type col, bool reverse = false);
		size_type sort_col() const;
		void unsort();
		bool freeze_sort(bool freeze);

		selection selected() const;

		void show_header(bool);
		bool visible_header() const;
		void move_select(bool upwards);
		void icon(const index_pair&, const nana::paint::image&);
		nana::paint::image icon(const index_pair&) const;
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
