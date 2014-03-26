/*
 *	A Tree Box Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/widgets/treebox.hpp
 *	@brief:
 *		The treebox organizes the nodes by a key string. 
 *		The treebox would have a vertical scrollbar if the node
 *	is too many to display. And it does not have a horizontal scrollbar,
 *	the widget will adjust the node's displaying position for fitting.
 */

#ifndef NANA_GUI_WIDGETS_TREEBOX_HPP
#define NANA_GUI_WIDGETS_TREEBOX_HPP
#include "widget.hpp"
#include "detail/compset.hpp"
#include <nana/paint/gadget.hpp>
#include "detail/tree_cont.hpp"
#include <nana/gui/timer.hpp>
#include <nana/any.hpp>
#include <nana/pat/cloneable.hpp>
#include <stdexcept>

namespace nana
{
namespace gui
{
	namespace drawerbase
	{
		namespace treebox
		{
			enum class component
			{
				begin, expender = begin, crook, icon, text, bground, end
			};

			template<typename ItemProxy>
			struct extra_events
			{
				typedef ItemProxy item_proxy;

				nana::fn_group<void(window, item_proxy, bool)> expand;
				nana::fn_group<void(window, item_proxy, bool)> checked;
				nana::fn_group<void(window, item_proxy, bool)> selected;
			};

			struct node_image_tag
			{
				nana::paint::image normal;
				nana::paint::image hovered;
				nana::paint::image expanded;
			};

			struct node_attribute
			{
				bool has_children;
				bool expended;
				checkstate checked;
				bool selected;
				bool mouse_pointed;
				nana::paint::image icon_normal;
				nana::paint::image icon_hover;
				nana::paint::image icon_expanded;
				nana::string text;
			};

			typedef ::nana::gui::widgets::detail::compset<component, node_attribute> compset_interface;
			typedef ::nana::gui::widgets::detail::compset_placer<component, node_attribute> compset_placer_interface;
			
			class renderer_interface
			{
			public:
				typedef drawerbase::treebox::component component;
				typedef ::nana::paint::graphics& graph_reference;
				typedef drawerbase::treebox::compset_interface compset_interface;
				typedef compset_interface::item_attribute_t item_attribute_t;
				typedef compset_interface::comp_attribute_t comp_attribute_t;

				virtual ~renderer_interface()
				{}

				virtual void bground(graph_reference, nana::color_t bgcolor, nana::color_t fgcolor, const compset_interface *) const = 0;
				virtual void expander(graph_reference, nana::color_t bgcolor, nana::color_t fgcolor, const compset_interface *) const = 0;
				virtual void crook(graph_reference, nana::color_t bgcolor, nana::color_t fgcolor, const compset_interface *) const = 0;
				virtual void icon(graph_reference, nana::color_t bgcolor, nana::color_t fgcolor, const compset_interface *) const = 0;
				virtual void text(graph_reference, nana::color_t bgcolor, nana::color_t fgcolor, const compset_interface *) const = 0;
			};

			class item_proxy;

			class trigger
				:public drawer_trigger
			{
				template<typename Renderer>
				struct basic_implement;

				class item_renderer;
				class item_locator;

				typedef basic_implement<item_renderer> implement;
			public:
				struct treebox_node_type
				{
					treebox_node_type();
					treebox_node_type(const nana::string& text);
					treebox_node_type& operator=(const treebox_node_type&);

					nana::string text;
					nana::any value;
					bool expanded;
					checkstate checked;
					nana::string img_idstr;
				};

				struct pseudo_node_type{};

				typedef nana::gui::widgets::detail::tree_cont<treebox_node_type> tree_cont_type;
				typedef tree_cont_type::node_type	node_type;
				typedef extra_events<item_proxy>	ext_event_type;

				trigger();
				~trigger();

				implement * impl() const;

				void auto_draw(bool);
				void checkable(bool);
				bool checkable() const;
				void check(node_type*, checkstate);
				bool draw();

				const tree_cont_type & tree() const;
				tree_cont_type & tree();

				void renderer(::nana::pat::cloneable<renderer_interface>&&);
				const ::nana::pat::cloneable<renderer_interface>& renderer() const;
				void placer(::nana::pat::cloneable<compset_placer_interface>&&);
				const ::nana::pat::cloneable<compset_placer_interface>& placer() const;

				nana::any & value(node_type*) const;
				node_type* insert(node_type*, const nana::string& key, const nana::string& title);
				node_type* insert(const nana::string& path, const nana::string& title);

				bool verify(const void*) const;
				bool verify_kinship(node_type* parent, node_type* child) const;

				void remove(node_type*);
				node_type * selected() const;
				void selected(node_type*);
				void set_expand(node_type*, bool);
				void set_expand(const nana::string& path, bool);

				//void image(const nana::string& id, const node_image_tag&);
				node_image_tag& icon(const nana::string&) const;
				void icon_erase(const nana::string&);
				void node_icon(node_type*, const nana::string& id);

				unsigned node_width(const node_type*) const;

				bool rename(node_type*, const nana::char_t* key, const nana::char_t* name);
				ext_event_type& ext_event() const;
			private:
				//Overrides drawer_trigger methods
				void attached(widget_reference, graph_reference)		override;
				void refresh(graph_reference)	override;
				void dbl_click(graph_reference, const eventinfo&)	override;
				void mouse_down(graph_reference, const eventinfo&)	override;
				void mouse_up(graph_reference, const eventinfo&)	override;
				void mouse_move(graph_reference, const eventinfo&)	override;
				void mouse_wheel(graph_reference, const eventinfo&)	override;
				void resize(graph_reference, const eventinfo&)		override;
				void key_down(graph_reference, const eventinfo&)	override;
				void key_char(graph_reference, const eventinfo&)	override;
			private:
				void _m_deal_adjust();
			private:
				implement * const impl_;
			}; //end class trigger


			/// A proxy for accessing the node.
			class item_proxy
				: public std::iterator<std::input_iterator_tag, item_proxy>
			{
			public:
				item_proxy();

				//Undocumented constructor.
				item_proxy(trigger*, trigger::node_type*);

				/// Append a child.
				item_proxy append(const nana::string& key, const nana::string& name);

				/// Append a child with a specified value.
				template<typename T>
				item_proxy append(const nana::string& key, const nana::string& name, const T&t)
				{
					item_proxy ip = append(key, name);
					if(false == ip.empty())
						ip.value(t);
					return ip;
				}

				/// Return true if the proxy does not refer to a node
				bool empty() const;

				/// Return the distance between the ROOT node and this node.
				/// @return, only available when emtpy() is false.
				std::size_t level() const;

				/// Return the check state
				bool checked() const;

				/// Set the check state
				item_proxy& check(bool);

				/// Return true when the node expended
				bool expended() const;

				/// Expend/Shrink children of the node
				item_proxy& expend(bool);

				/// Return true when the node is selected.
				bool selected() const;

				/// Select the node.
				item_proxy& select(bool);

				/// Return the icon.
				const nana::string& icon() const;

				/// Set the icon.
				item_proxy& icon(const nana::string& id);

				/// Return the text.
				const nana::string& text() const;

				/// Set a new key.
				item_proxy& key(const nana::string& s);

				/// Return the key.
				const nana::string& key() const;

				/// Set the text.
				item_proxy& text(const nana::string&);

				std::size_t size() const;

				/// Return the first child of the node.
				item_proxy child() const;

				/// Return the owner of the node.
				item_proxy owner() const;

				/// Return the sibling of the node.
				item_proxy sibling() const;

				/// Return the first child of the node
				item_proxy begin() const;

				/// An end node.
				item_proxy end() const;

				bool operator==(const nana::string&) const;
				bool operator==(const char*) const;
				bool operator==(const wchar_t*) const;

				/// Behavior of Iterator
				item_proxy& operator=(const item_proxy&);

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

				template<typename T>
				T * value_ptr() const
				{
					return _m_value().template get<T>();
				}

				template<typename T>
				T& value() const
				{
					T* p = _m_value().template get<T>();
					if(0 == p)
						throw std::runtime_error("treebox::value<T>() Invalid type of value.");
					return *p;
				}

				template<typename T>
				item_proxy & value(const T& t)
				{
					_m_value() = t;
					return *this;
				};

				template<typename T>
				item_proxy & value(T&& t)
				{
					_m_value() = std::move(t);
					return *this;
				};

				// Undocumentated methods for internal use
				trigger::node_type * _m_node() const;
			private:
				nana::any& _m_value();
				const nana::any& _m_value() const;
			private:
				trigger * trigger_;
				trigger::node_type * node_;
			};//end class item_proxy
		}//end namespace treebox
	}//end namespace drawerbase


	class treebox
		:public widget_object<category::widget_tag, drawerbase::treebox::trigger>
	{
	public:
		typedef drawerbase::treebox::item_proxy	item_proxy;

		typedef drawer_trigger_t::ext_event_type ext_event_type;
		typedef drawerbase::treebox::node_image_tag node_image_type;

		/// The interface of treebox renderer
		typedef drawerbase::treebox::renderer_interface renderer_interface;

		/// The interface of treebox compset_placer
		typedef drawerbase::treebox::compset_placer_interface compset_placer_interface;

		/// The default constructor without creating the widget.
		treebox();

		/// The construct that creates a widget.
		/// @param wd, A handle to the parent window of the widget being created.
		/// @param visible, specifying the visible after creating.
		treebox(window wd, bool visible);

		/// The construct that creates a widget.
		/// @param wd, A handle to the parent window of the widget being created.
		/// @param r, the size and position of the widget in its parent window coordinate.
		/// @param visible, specifying the visible after creating.
		treebox(window, const nana::rectangle& = rectangle(), bool visible = true);

		template<typename ItemRenderer>
		treebox & renderer(const ItemRenderer & rd)
		{
			get_drawer_trigger().renderer(::nana::pat::cloneable<renderer_interface>(rd));
			return *this;
		}

		const nana::pat::cloneable<renderer_interface> & renderer() const;

		template<typename Placer>
		treebox & placer(const Placer & r)
		{
			get_drawer_trigger().placer(::nana::pat::cloneable<compset_placer_interface>(r));
			return *this;
		}

		const nana::pat::cloneable<compset_placer_interface> & placer() const;

		/// Eanble the widget that draws automatically when it is operating.
		/// @param bool, whether to enable.
		void auto_draw(bool);

		/// Enable the checkbox for each item of the widget.
		/// @param bool, wheter to enable.
		treebox & checkable(bool enable);

		/// Determinte whether the checkbox is enabled.
		bool checkable() const;

		ext_event_type& ext_event() const;

		treebox& icon(const nana::string& id, const node_image_type& node_img);

		node_image_type& icon(const nana::string& id) const;

		void icon_erase(const nana::string& id);

		item_proxy find(const nana::string& keypath);

		item_proxy insert(const nana::string& path_key, const nana::string& title);
		item_proxy insert(item_proxy i, const nana::string& key, const nana::string& title);
		item_proxy erase(item_proxy i);

		void erase(const nana::string& keypath);

		nana::string make_key_path(item_proxy i, const nana::string& splitter) const;
		item_proxy selected() const;
	};//end class treebox
}//end namespace gui
}//end namespace nana

#endif
