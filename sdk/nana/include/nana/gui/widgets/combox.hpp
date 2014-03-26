/*
 *	A Combox Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/widgets/combox.hpp
 */

#ifndef NANA_GUI_WIDGETS_COMBOX_HPP
#define NANA_GUI_WIDGETS_COMBOX_HPP
#include "widget.hpp"
#include "float_listbox.hpp"
#include <nana/concepts.hpp>

namespace nana{ namespace gui
{
	class combox;
	namespace drawerbase
	{
		namespace combox
		{
			struct extra_events
			{
				nana::fn_group<void(nana::gui::combox&)> selected;
			};

			class drawer_impl;
			
			class trigger
				: public drawer_trigger
			{
			public:
				typedef extra_events ext_event_type;

				trigger();
				~trigger();

				drawer_impl& get_drawer_impl();
				const drawer_impl& get_drawer_impl() const;
			private:
				void attached(widget_reference, graph_reference)	override;
				void detached()	override;
				void refresh(graph_reference)	override;
				void focus(graph_reference, const eventinfo&)	override;
				void mouse_enter(graph_reference, const eventinfo&)	override;
				void mouse_leave(graph_reference, const eventinfo&)	override;
				void mouse_down(graph_reference, const eventinfo&)	override;
				void mouse_up(graph_reference, const eventinfo&)	override;
				void mouse_move(graph_reference, const eventinfo&)	override;
				void mouse_wheel(graph_reference, const eventinfo&)	override;
				void key_down(graph_reference, const eventinfo&)	override;
				void key_char(graph_reference, const eventinfo&)	override;
			private:
				drawer_impl * drawer_;
			};
		}
	}//end namespace drawerbase

	class combox
		:	public widget_object<category::widget_tag, drawerbase::combox::trigger>,
			public concepts::any_objective<std::size_t, 1>
	{
	public:
		typedef float_listbox::item_renderer item_renderer;
		typedef drawerbase::combox::extra_events ext_event_type;

		combox();
		combox(window, bool visible);
		combox(window, const nana::string& text, bool visible = true);
		combox(window, const nana::char_t* text, bool visible = true);
		combox(window, const rectangle& r = rectangle(), bool visible = true);
		
		void clear();
		void editable(bool);
		bool editable() const;
		combox& push_back(const nana::string&);
		std::size_t the_number_of_options() const;
		std::size_t option() const;
		void option(std::size_t);
		nana::string text(std::size_t) const;

		ext_event_type& ext_event() const;
		void renderer(item_renderer*);

		void image(std::size_t, const nana::paint::image&);
		nana::paint::image image(std::size_t) const;
		void image_pixels(unsigned);
	private:
		//Override _m_caption for caption()
		nana::string _m_caption() const;
		void _m_caption(const nana::string&);
		nana::any* _m_anyobj(std::size_t, bool allocate_if_empty) const;
	};
}//end namespace gui
}
#endif
