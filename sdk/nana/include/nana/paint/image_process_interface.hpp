/*
 *	Image Processing Interfaces
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/paint/image_process_interface.hpp
 */

#ifndef NANA_PAINT_IMAGE_PROCESS_INTERFACE_HPP
#define NANA_PAINT_IMAGE_PROCESS_INTERFACE_HPP

#include <nana/basic_types.hpp>
#include <nana/paint/pixel_buffer.hpp>

namespace nana
{
	namespace paint
	{
		namespace image_process
		{
			class stretch_interface
			{
			public:
				virtual ~stretch_interface() = 0;
				virtual void process(const paint::pixel_buffer & s_pixbuf, const nana::rectangle& s_r, paint::pixel_buffer & d_pixbuf, const nana::rectangle& d_r) const = 0;
			};

			class alpha_blend_interface
			{
			public:
				virtual ~alpha_blend_interface() = 0;
				virtual void process(const paint::pixel_buffer& s_pixbuf, const nana::rectangle& s_r, paint::pixel_buffer& d_pixbuf, const point& d_pos) const = 0;
			};

			class blend_interface
			{
			public:
				virtual ~blend_interface() = 0;
				virtual void process(const paint::pixel_buffer& s_pixbuf, const nana::rectangle& s_r, paint::pixel_buffer& d_pixbuf, const nana::point& d_pos, double fade_rate) const = 0;
			};

			class line_interface
			{
			public:
				virtual ~line_interface() = 0;

				//process
				//@brief: interface of algorithm, pos_beg is a left point, pos_end is a right point.
				virtual void process(paint::pixel_buffer & pixbuf, const nana::point& pos_beg, const nana::point& pos_end, nana::color_t color, double fade_rate) const = 0;
			};

			class blur_interface
			{
			public:
				virtual ~blur_interface() = 0;
				virtual void process(paint::pixel_buffer&, const nana::rectangle& r, std::size_t radius) const = 0;
			};
		}
	}//end namespace paint
}//end namespace nana
#endif
