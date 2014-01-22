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
		:nana::noncopyable
	{
	public:
		virtual ~object(){}

		virtual bool diehard() const
		{
			return false;
		}

		virtual void draw(nana::paint::graphics&) const = 0;
	};

	class user_draw_function
		: public object
	{
	public:
		user_draw_function(const nana::functor<void(nana::paint::graphics&)> & f, bool diehard)
			: diehard_(diehard), fn_(f)
		{}

		bool diehard() const
		{
			return diehard_;
		}

		void draw(paint::graphics& graph) const
		{
			fn_(graph);
		}
	private:
		bool diehard_;
		nana::functor<void(nana::paint::graphics&)> fn_;
	};

	//string
	class string
		: public object
	{
	public:
		string(int x, int y, unsigned color, const nana::char_t* text)
			:x_(x), y_(y), color_(color), text_(0)
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

	class bitblt
		:public object
	{
	public:
		bitblt(int x, int y, unsigned width, unsigned height, const nana::paint::graphics& source, int srcx, int srcy)
			: r_dst_(x, y, width, height), p_src_(srcx, srcy), graph_(source)
		{
		}

		void draw(nana::paint::graphics& graph) const
		{
			graph.bitblt(r_dst_, graph_, p_src_);
		}
	private:
		nana::rectangle r_dst_;
		nana::point p_src_;
		nana::paint::graphics graph_;
	};

	class bitblt_image
		: public object
	{
	public:
		bitblt_image(int x, int y, unsigned width, unsigned height, const nana::paint::image& img, int srcx, int srcy)
			:r_(srcx, srcy, width, height), p_dst_(x, y), img_(img)
		{}

		void draw(nana::paint::graphics& graph) const
		{
			img_.paste(r_, graph, p_dst_);
		}
	private:
		nana::rectangle r_;
		nana::point p_dst_;
		nana::paint::image img_;
	};

	class stretch
		: public object
	{
		enum kind_t {kind_graph, kind_image};
	public:
		enum { fixed_buffer_size = sizeof(nana::paint::graphics) < sizeof(nana::paint::image) ? sizeof(nana::paint::image) : sizeof(nana::paint::graphics)};

		stretch(const nana::rectangle& r_dst, const nana::paint::graphics& graph, const nana::rectangle& r_src)
			: r_dst_(r_dst), r_src_(r_src), kind_(kind_graph)
		{
			new (buffer) nana::paint::graphics(graph);
		}

		stretch(const nana::rectangle& r_dst, const nana::paint::image& img, const nana::rectangle& r_src)
			: r_dst_(r_dst), r_src_(r_src), kind_(kind_image)
		{
			new (buffer) nana::paint::image(img);
		}

		~stretch()
		{
			switch(kind_)
			{
			case kind_graph:
				reinterpret_cast<nana::paint::graphics*>(buffer)->~graphics();
				break;
			case kind_image:
				reinterpret_cast<nana::paint::image*>(buffer)->~image();
				break;
			}
		}

		void draw(nana::paint::graphics & graph) const
		{
			if(kind_ == kind_graph)
				reinterpret_cast<const nana::paint::graphics*>(buffer)->stretch(r_src_, graph, r_dst_);
			else
				reinterpret_cast<const nana::paint::image*>(buffer)->stretch(r_src_, graph, r_dst_);
		}
	private:
		nana::rectangle r_dst_;
		nana::rectangle r_src_;
		kind_t kind_;

		char buffer[fixed_buffer_size];
	};

}//end namespace dynamic_drawing
}//end namespace detail
}//end namespace gui
}//end namespace nana

#endif

