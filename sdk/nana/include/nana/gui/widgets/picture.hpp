/*
 *	A Picture Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/widgets/picture.hpp
 *
 *	Used for showing a picture
 */
#ifndef NANA_GUI_WIDGET_PICTURE_HPP
#define NANA_GUI_WIDGET_PICTURE_HPP
#include "widget.hpp"

namespace nana
{
namespace gui
{
	namespace xpicture
	{
		class picture_drawer: public nana::gui::drawer_trigger
		{
		public:
			picture_drawer();
			void bind_window(nana::gui::widget&);
			void attached(graph_reference);
			void load(const nana::char_t* file);
			void load(const nana::paint::image&);
			void set_shadow_background(unsigned begin_color, unsigned end_color, bool horizontal);
			bool bgstyle(bool is_stretch, nana::arrange, int beg, int end);
		private:
			void refresh(graph_reference);
			void _m_draw_background();
		private:
			nana::gui::widget* widget_;
			nana::paint::graphics* graph_;

			struct	runtime_type
			{
				runtime_type();
				unsigned background_shadow_start;
				unsigned background_shadow_end;
				bool	horizontal;
			}runtime_;

			struct back_image_tag
			{
				nana::paint::image	image;
				bool is_stretch;
				nana::arrange arg;
				int beg, end;
			}backimg_;

		};
		
	}//end namespace xpicture

	class picture
		: public widget_object<category::widget_tag, xpicture::picture_drawer>
	{
	public:
		picture();
		picture(window, bool visible);
		picture(window, const rectangle& = rectangle(), bool visible= true);

		void load(const nana::char_t* file);
		void load(const nana::paint::image&);
		void bgstyle(bool stretchable, nana::arrange arg, int beg, int end);
		void set_shadow_background(unsigned begin_color, unsigned end_color, bool horizontal);
		void transparent(bool);
		bool transparent() const;
	};
}//end namespace gui
}//end namespace nana
#endif
