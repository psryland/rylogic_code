/*
 *	A Drawing Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/drawing.cpp
 */

#include <nana/gui/drawing.hpp>
#include <nana/gui/programming_interface.hpp>

namespace nana
{
namespace gui
{
	//restrict
	//@brief:	This name is only visible for this compiling-unit
	namespace restrict
	{
		typedef gui::detail::bedrock::core_window_t core_window_t;
		extern gui::detail::bedrock& bedrock;

		inline nana::gui::detail::drawer& get_drawer(nana::gui::window wnd)
		{
			return reinterpret_cast<core_window_t*>(wnd)->drawer;
		}
	}
    
    //class drawing
  		drawing::drawing(window wd)
			:handle_(wd)
  		{}
  		
  		drawing::~drawing(){} //Just for polymorphism

		bool drawing::empty() const
		{
			return API::empty_window(handle_) ||  reinterpret_cast<restrict::core_window_t*>(handle_)->root_graph->empty();
		}

		void drawing::update() const
		{
			API::refresh_window(handle_);
		}

		void drawing::string(int x, int y, unsigned color, const nana::char_t* text)
		{
			if(API::empty_window(handle_))	return;
			restrict::get_drawer(handle_).string(x, y, color, text);
		}

		void drawing::bitblt(int x, int y, unsigned width, unsigned height, const nana::paint::graphics& source, int srcx, int srcy)
		{
			if(API::empty_window(handle_))	return;
			restrict::get_drawer(handle_).bitblt(x, y, width, height, source, srcx, srcy);
		}

		void drawing::bitblt(int x, int y, unsigned width, unsigned height, const nana::paint::image& source, int srcx, int srcy)
		{
			if(API::empty_window(handle_))	return;
			restrict::get_drawer(handle_).bitblt(x, y, width, height, source, srcx, srcy);
		}

		void drawing::draw(const draw_fn_t & f)
		{
			if(API::empty_window(handle_))	return;
			restrict::get_drawer(handle_).draw(f, false);			
		}

		drawing::diehard_t drawing::draw_diehard(const draw_fn_t& f)
		{
			if(API::empty_window(handle_))	return 0;
			return reinterpret_cast<drawing::diehard_t>(restrict::get_drawer(handle_).draw(f, true));		
		}

		void drawing::erase(diehard_t d)
		{
			if(API::empty_window(handle_))
				restrict::get_drawer(handle_).erase(d);
		}

		void drawing::line(int x, int y, int x2, int y2, unsigned color)
		{
			if(API::empty_window(handle_))	return;
			restrict::get_drawer(handle_).line(x, y, x2, y2, color);
		}

		void drawing::rectangle(int x, int y, unsigned width, unsigned height, unsigned color, bool issolid)
		{
			if(API::empty_window(handle_))	return;
			restrict::get_drawer(handle_).rectangle(x, y, width, height, color, issolid);
		}

		void drawing::shadow_rectangle(int x, int y, unsigned width, unsigned height, nana::color_t beg, nana::color_t end, bool vertical)
		{
			if(API::empty_window(handle_))	return;
			restrict::get_drawer(handle_).shadow_rectangle(x, y, width, height, beg, end, vertical);
		}

		void drawing::stretch(const nana::rectangle& r_dst, const nana::paint::graphics & src, const nana::rectangle& r_src)
		{
			if(API::empty_window(handle_)) return;
			restrict::get_drawer(handle_).stretch(r_dst, src, r_src);
		}

		void drawing::stretch(const nana::rectangle& r_dst, const nana::paint::image & src, const nana::rectangle& r_src)
		{
			if(API::empty_window(handle_)) return;
			restrict::get_drawer(handle_).stretch(r_dst, src, r_src);
		}

		void drawing::clear()
		{
			if(API::empty_window(handle_))	return;
			restrict::get_drawer(handle_).clear();
		}
	//end class drawing
}//end namespace gui
}//end namespace nana

