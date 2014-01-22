/*
 *	Paint Image Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/paint/image.hpp
 *	@description:	class image is used for load an image file into memory.
 */

#ifndef NANA_PAINT_IMAGE_HPP
#define NANA_PAINT_IMAGE_HPP

#include "graphics.hpp"

namespace nana
{
namespace paint
{
	class image
	{
		friend class image_accessor;
	public:
		class image_impl_interface;

		image();
		image(const image& rhs);
		image(const nana::char_t* file);
		image(const nana::string& filename);
		~image();
		image & operator=(const image& rhs);
		bool open(const nana::string& filename);
		bool empty() const;
		operator const void*() const;
		void close();
		nana::size size() const;
		void paste(graphics& dst, int x, int y) const;
		void paste(const nana::rectangle& r_src, graphics& dst, const nana::point& p_dst) const;
		void stretch(const nana::rectangle& r_src, graphics& dst, const nana::rectangle& r_dst) const;
	private:
		nana::shared_ptr<image_impl_interface> image_ptr_;
	};//end class image

}//end namespace paint
}//end namespace nana

#endif

