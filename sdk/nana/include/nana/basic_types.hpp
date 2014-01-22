/*
 *	Basic Types definition
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0.
 *	(See accompanying file LICENSE_1_0.txt or copy at
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/basic_types.hpp
 */

#ifndef NANA_BASIC_TYPES_HPP
#define NANA_BASIC_TYPES_HPP

#include "deploy.hpp"

namespace nana
{
	//A constant value for the invalid position.
	const std::size_t npos = static_cast<std::size_t>(-1);

	template<typename CharT>
	struct casei_char_traits
		: public std::char_traits<CharT>
	{
		typedef CharT char_type;

		static bool eq(char_type c1, char_type c2)
		{
			return std::toupper(c1) == std::toupper(c2);
		}

		static bool lt(char_type c1, char_type c2)
		{
			return std::toupper(c1) < std::toupper(c2);
		}

		static int compare(const char_type* s1, const char_type* s2, std::size_t n)
		{
			while(n--)
			{
				char_type c1 = std::toupper(*s1);
				char_type c2 = std::toupper(*s2);
				if(c1 < c2) return -1;
				if(c1 > c2) return 1;
				++s1;
				++s2;
			}
			return 0;
		}

		static const char_type* find(const char_type* s, std::size_t n, const char_type& a)
		{
			char_type ua = std::toupper(a);
			const char_type * end = s + n;
			while((s != end) && (std::toupper(*s) != ua))
				++s;
			return (s == end ? 0 : s);
		}
	};

	typedef std::basic_string<nana::char_t, casei_char_traits<nana::char_t> > cistring;

	namespace detail
	{
		struct drawable_impl_type;	//declearation, defined in platform_spec.hpp
	}

	namespace paint
	{
		typedef nana::detail::drawable_impl_type*	drawable_type;
	}

	namespace gui
	{
		struct mouse_action
		{
			enum t
			{
				begin, normal = begin, over, pressed, end
			};
		};

		struct element_state
		{
			enum t
			{
				normal,
				hovered,
				focus_normal,
				focus_hovered,
				pressed,
				disabled
			};
		};
	}

	typedef unsigned scalar_t;
	typedef unsigned char	uint8_t;
	typedef unsigned long	uint32_t;
	typedef unsigned		uint_t;
	typedef unsigned		color_t;
#if defined(NANA_WINDOWS)
	typedef __int64	long_long_t;
#elif defined(NANA_LINUX)
	typedef long long long_long_t;
#endif

	const color_t null_color = 0xFFFFFFFF;

	struct pixel_rgb_t
	{
		union
		{
			struct element_tag
			{
				unsigned int blue:8;
				unsigned int green:8;
				unsigned int red:8;
				unsigned int alpha_channel:8;
			}element;

			color_t color;
		}u;
	};

	struct rectangle;

	struct point
	{
		point();
		point(int x, int y);
		point(const rectangle&);

		bool operator==(const point&) const;
		bool operator!=(const point&) const;
		bool operator<(const point&) const;
		bool operator<=(const point&) const;
		bool operator>(const point&) const;
		bool operator>=(const point&) const;
		point& operator=(const rectangle&);

		int x;
		int y;
	};

	struct upoint
	{
		typedef unsigned value_type;

		upoint();
		upoint(value_type x, value_type y);
		bool operator==(const upoint&) const;
		bool operator!=(const upoint&) const;
		bool operator<(const upoint&) const;
		bool operator<=(const upoint&) const;
		bool operator>(const upoint&) const;
		bool operator>=(const upoint&) const;

		value_type x;
		value_type y;
	};

	struct size
	{
		size();
		size(unsigned width, unsigned height);
		size(const rectangle&);

		bool is_zero() const;
		bool operator==(const size& rhs) const;
		bool operator!=(const size& rhs) const;

		size& operator=(const rectangle&);


		unsigned width;
		unsigned height;
	};

	struct rectangle
	{
		rectangle();
		rectangle(int x, int y, unsigned width, unsigned height);
		rectangle(const size &);
		rectangle(const point&, const size&);

		bool operator==(const rectangle&) const;
		bool operator!=(const rectangle&) const;
		rectangle & operator=(const point&);
		rectangle & operator=(const size&);

		rectangle& pare_off(int pixels);
		bool is_hit(int x, int y) const;

		int x;
		int y;
		unsigned width;
		unsigned height;
	};

	struct arrange
	{
		enum t{unkown, horizontal, vertical, horizontal_vertical};
		t value;

		arrange();
		arrange(t);
		operator t() const;
		arrange& operator=(t);
		bool operator==(t) const;
		bool operator!=(t) const;
	};

	///The definition of horizontal alignment
	struct align
	{
		enum t{
			left, center, right
		};
	};

	///The definition of vertical alignment
	struct align_v
	{
		enum t{
			top, center, bottom
		};		
	};
}//end namespace nana

#endif


