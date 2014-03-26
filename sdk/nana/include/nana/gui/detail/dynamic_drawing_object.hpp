/*
 *	Dynamic Drawing Object Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/detail/dynamic_drawing_object.hpp
 *
 *	!DON'T INCLUDE THIS HEADER FILE IN YOUR SOURCE CODE
 */

#ifndef NANA_GUI_DETAIL_DYNAMIC_DRAWING_OBJECT_HPP
#define NANA_GUI_DETAIL_DYNAMIC_DRAWING_OBJECT_HPP

#include <nana/paint/graphics.hpp>
#include <nana/paint/image.hpp>

namespace nana
{
namespace gui
{
namespace detail
{
namespace dynamic_drawing
{

	class object
		: nana::noncopyable
	{
	public:
		virtual ~object(){}

		virtual bool diehard() const
		{
			return false;
		}
		
		virtual void draw(nana::paint::graphics&) const = 0;
	};

	//user_draw_function
	class user_draw_function
		: public object
	{
	public:
		user_draw_function(std::function<void(paint::graphics&)> && f, bool diehard)
			:	diehard_(diehard), fn_(std::move(f))
		{}

		void draw(paint::graphics& graph) const
		{
			fn_(graph);
		}

		bool diehard() const
		{
			return diehard_;
		}
	private:
		bool diehard_;
		std::function<void(paint::graphics&)> fn_;
	};

	//string
	class string
		: public object
	{
	public:
		string(int x, int y, unsigned color, const nana::char_t* text)
			:x_(x), y_(y), color_(color), text_(nullptr)
		{
			if(text)
			{
				std::size_t len = nana::strlen(text);
				text_ = new nana::char_t[len + 1];
				nana::strcpy(text_, text);
			}
		}

		~string()
		{
			delete [] text_;
		}

		void draw(nana::paint::graphics& graph) const
		{
			if(text_)	graph.string(x_, y_, color_, text_);
		}
	private:
		int x_, y_;
		unsigned color_;
		nana::char_t* text_;
	};//end string

	//line
	class line
		:public object
	{
	public:
		line(int x, int y, int x2, int y2, unsigned color)
			:x_(x), y_(y), x2_(x2), y2_(y2), color_(color)
		{
		}

		void draw(nana::paint::graphics& graph) const
		{
			graph.line(x_, y_, x2_, y2_, color_);
		}
	private:
		int x_, y_, x2_, y2_;
		unsigned color_;
	};//end line

	class rectangle
		:public object
	{
	public:
		rectangle(int x, int y, unsigned width, unsigned height, unsigned color, bool issolid)
			:x_(x), y_(y), width_(width), height_(height), color_(color), issolid_(issolid)
		{
		}

		void draw(nana::paint::graphics& graph) const
		{
			graph.rectangle(x_, y_, width_, height_, color_, issolid_);
		}
	private:
		int x_, y_;
		unsigned width_, height_;
		unsigned color_;
		bool	issolid_;
	};

	class shadow_rectangle
		:public object
	{
	public:
		shadow_rectangle(int x, int y, unsigned width, unsigned height, nana::color_t beg, nana::color_t end, bool vertical)
			:x_(x), y_(y), width_(width), height_(height), beg_(beg), end_(end), vertical_(vertical)
		{
		}

		void draw(nana::paint::graphics& graph) const
		{
			graph.shadow_rectangle(x_, y_, width_, height_, beg_, end_, vertical_);
		}
	private:
		int x_, y_;
		unsigned width_, height_;
		nana::color_t beg_, end_;
		bool	vertical_;
	};

	template<typename ImageGraph>
	class bitblt
		:public object
	{
	public:
		bitblt(int x, int y, unsigned width, unsigned height, const ImageGraph& source, int srcx, int srcy)
			:r_dst_(x, y, width, height), p_src_(srcx, srcy), graph_(source)
		{}

		void draw(nana::paint::graphics& graph) const
		{
			_m_bitblt(graph_, graph);
		}
	private:
		void _m_bitblt(const paint::graphics& from, paint::graphics& to) const
		{
			to.bitblt(r_dst_, from, p_src_);
		}

		void _m_bitblt(const paint::image& from, paint::graphics& to) const
		{
			from.paste(nana::rectangle(p_src_.x, p_src_.y, r_dst_.width, r_dst_.height), to, nana::point(r_dst_.x, r_dst_.y));
		}
	private:
		nana::rectangle r_dst_;
		nana::point p_src_;
		ImageGraph graph_;
	};

	template<typename ImageGraph>
	class stretch
		: public object
	{
	public:
		stretch(const nana::rectangle& r_dst, const ImageGraph& graph, const nana::rectangle& r_src)
			:	r_dst_(r_dst),
				r_src_(r_src),
				graph_(graph)
		{
		}

		void draw(nana::paint::graphics & graph) const
		{
			graph_.stretch(r_src_, graph, r_dst_);
		}
	private:
		nana::rectangle r_dst_;
		nana::rectangle	r_src_;
		ImageGraph graph_;
	};

}//end namespace dynamic_drawing
}//end namespace detail
}//end namespace gui
}//end namespace nana

#endif

