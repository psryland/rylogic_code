/*
 *	Paint Graphics Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0.
 *	(See accompanying file LICENSE_1_0.txt or copy at
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/paint/graphics.hpp
 */

#ifndef NANA_PAINT_GRAPHICS_HPP
#define NANA_PAINT_GRAPHICS_HPP

#include "../basic_types.hpp"
#include "../gui/basis.hpp"
#include <nana/memory.hpp>

namespace nana
{
	namespace paint
	{
		namespace detail
		{
			struct native_font_signature;
		}// end namespace detail

		typedef detail::native_font_signature*		native_font_type;

		class font
		{
			friend class graphics;
		public:
			font();
			font(drawable_type);
			font(const font&);
			font(const nana::char_t* name, unsigned size, bool bold = false, bool italic = false, bool underline = false, bool strike_out = false);
			~font();
			bool empty() const;
			void make(const nana::char_t* name, unsigned size, bool bold = false, bool italic = false, bool underline = false, bool strike_out = false);
			void make_raw(const nana::char_t*, unsigned height, unsigned weight, bool italic, bool underline, bool strike_out);

			void set_default() const;
			nana::string name() const;
			unsigned size() const;
			bool bold() const;
			unsigned height() const;
			unsigned weight() const;
			bool italic() const;
			native_font_type handle() const;
			void release();

			font& operator=(const font&);
			bool operator==(const font&) const;
			bool operator!=(const font&) const;
		private:
			struct impl_type;
			impl_type * impl_;
		};

		//class graphics
		class graphics
		{
		public:
			typedef nana::gui::native_window_type native_window_type;

			graphics();
			graphics(unsigned width, unsigned height);
			graphics(const nana::size&);
			graphics(const graphics&);
			graphics& operator=(const graphics&);
			bool changed() const;
			bool empty() const;
			operator const void*() const;

			drawable_type handle() const;
			const void* pixmap() const;
			const void* context() const;
			void make(unsigned width, unsigned height);
			void resize(unsigned width, unsigned height);
			void typeface(const font&);
			font typeface() const;
			nana::size	text_extent_size(const nana::char_t*) const;
			nana::size	text_extent_size(const nana::string&) const;
			nana::size	text_extent_size(const nana::char_t*, std::size_t length) const;
			nana::size	text_extent_size(const nana::string&, std::size_t length) const;
			nana::size	glyph_extent_size(const nana::char_t*, std::size_t length, std::size_t begin, std::size_t end) const;
			nana::size	glyph_extent_size(const nana::string&, std::size_t length, std::size_t begin, std::size_t end) const;
			bool glyph_pixels(const nana::char_t *, std::size_t length, unsigned* pxbuf) const;
			nana::size	bidi_extent_size(const nana::string&) const;

			bool text_metrics(unsigned & ascent, unsigned& descent, unsigned& internal_leading) const;


			unsigned bidi_string(int x, int y, color_t, const nana::char_t *, std::size_t len);
			void string(int x, int y, color_t, const nana::string&, std::size_t len);
			void string(int x, int y, color_t, const nana::string&);
			void string(int x, int y, color_t, const nana::char_t*, std::size_t len);
			void string(int x, int y, color_t, const nana::char_t*);

			void set_pixel(int x, int y, color_t);
			void rectangle(int x, int y, unsigned width, unsigned height, color_t, bool solid);
			void rectangle(color_t, bool solid);
			void rectangle(const nana::rectangle&, color_t, bool solid);
			void round_rectangle(int x, int y, unsigned width, unsigned height, unsigned radius_x, unsigned radius_y, color_t, bool solid, color_t color_if_solid);
			void round_rectangle(const nana::rectangle&, unsigned radius_x, unsigned radius_y, color_t, bool solid, color_t color_if_solid);
			void shadow_rectangle(const nana::rectangle&, color_t beg_color, color_t end_color, bool vertical);
			void shadow_rectangle(int x, int y, unsigned width, unsigned height, color_t beg_color, color_t end_color, bool vertical);

			void line(int x1, int y1, int x2, int y2, color_t color);
			void line(const point& beg, const point& end, color_t color);
			void bitblt(const nana::rectangle& r_dst, native_window_type src);
			void bitblt(const nana::rectangle& r_dst, native_window_type source, const nana::point&);
			void bitblt(int x, int y, const graphics& source);
			void bitblt(const nana::rectangle& r_dst, const graphics& src);
			void bitblt(const nana::rectangle& r_dst, const graphics& src, const nana::point& p_src);

			void blend(const nana::rectangle& s_r, graphics& dst, const nana::point& d_pos, double fade_rate) const;
			void blend(const nana::rectangle& r, nana::color_t, double fade_rate);

			void blur(const nana::rectangle& r, std::size_t radius);

			void paste(const graphics& dst, int x, int y) const;
			void paste(native_window_type dst, const nana::rectangle&, int sx, int sy) const;
			void paste(native_window_type dst, int dx, int dy, unsigned width, unsigned height, int sx, int sy) const;
			void paste(drawable_type dst, int x, int y) const;
			void paste(const nana::rectangle& r_src, graphics& dst, int x, int y) const;
			void rgb_to_wb();

			void stretch(const nana::rectangle& src_r, graphics& dst, const nana::rectangle& r) const;
			void stretch(graphics& dst, const nana::rectangle& r) const;

			void flush();

			unsigned width() const;
			unsigned height() const;
			nana::size size() const;
			void setsta();
			void release();
			void save_as_file(const char*);

			static color_t mix(color_t colorX, color_t colorY, double persent);
		private:
			nana::shared_ptr<nana::detail::drawable_impl_type> dwptr_;
			font font_shadow_;

            drawable_type	handle_;
			nana::size	size_;
			bool changed_;
		};
	}//end namespace paint
}	//end namespace nana
#endif

