/*
 *	A text editor implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/widgets/skeletons/text_editor.hpp
 *	@description: 
 */

#ifndef NANA_GUI_SKELETONS_TEXT_EDITOR_HPP
#define NANA_GUI_SKELETONS_TEXT_EDITOR_HPP
#include "textbase.hpp"
#include <nana/gui/widgets/scroll.hpp>
#include <nana/unicode_bidi.hpp>

namespace nana{	namespace gui{	namespace widgets
{
	namespace skeletons
	{
		class text_editor
		{
			struct attributes;
		public:
			typedef nana::char_t	char_type;
			typedef textbase<char_type>::size_type size_type;
			typedef textbase<char_type>::string_type string_type;

			typedef nana::paint::graphics & graph_reference;

			struct ext_renderer_tag
			{
				std::function<void(graph_reference, const nana::rectangle& text_area, nana::color_t)> background;
			};

			text_editor(window, graph_reference);
			~text_editor();

			void border_renderer(std::function<void(nana::paint::graphics&)>);

			void load(const char*);

			//text_area
			//@return: Returns true if the area of text is changed.
			bool text_area(const nana::rectangle&);
			bool tip_string(const nana::string&);

			const attributes & attr() const;
			bool multi_lines(bool);
			void editable(bool);
			void enable_background(bool);
			void enable_background_counterpart(bool);

			ext_renderer_tag& ext_renderer() const;

			unsigned line_height() const;
			unsigned screen_lines() const;

			bool getline(std::size_t pos, nana::string&) const;
			void setline(std::size_t pos, const nana::string&);
			void text(const nana::string&);
			nana::string text() const;

			//move_caret
			//@brief: Set caret position through text coordinate
			void move_caret(size_type x, size_type y);
			void move_caret_end();
			void reset_caret_height() const;
			void reset_caret();
			void show_caret(bool isshow);

			bool selected() const;
			bool select(bool);
			//Set the end position of a selected string
			void set_end_caret();
			bool hit_text_area(int x, int y) const;
			bool hit_select_area(nana::upoint pos) const;
			bool move_select();
			bool mask(char_t);
		public:
			void draw_scroll_rectangle();
			void redraw(bool has_focus);
		public:
			void put(const nana::string&);
			void put(nana::char_t);
			void copy() const;
			void paste();
			void enter();
			void del();
			void backspace();
			bool move(nana::char_t);
			void move_up();
			void move_down();
			void move_left();
			void move_right();
			nana::upoint mouse_caret(int screen_x, int screen_y);
			nana::upoint caret() const;
			bool scroll(bool upwards, bool vertical);
			bool mouse_enter(bool);
			bool mouse_down(bool left_button, int screen_x, int screen_y);
			bool mouse_move(bool left_button, int screen_x, int screen_y);
			bool mouse_up(bool left_button, int screen_x, int screen_y);

			skeletons::textbase<nana::char_t>& textbase();
			const skeletons::textbase<nana::char_t>& textbase() const;
		private:
			bool _m_scroll_text(bool vertical);
			void _m_on_scroll(const eventinfo& ei);
			void _m_scrollbar();
			nana::size _m_text_area() const;
			void _m_get_scrollbar_size();
			void _m_reset();
			nana::upoint _m_put(nana::string);
			nana::upoint _m_erase_select();

			bool _m_make_select_string(nana::string&) const;

			//_m_make_simple_nl
			//@brief: Transfers a string if it contains "0xD\0xA" or "0xA\0xD"
			static std::size_t _m_make_simple_nl(nana::string&);

			bool _m_cancel_select(int align);
			unsigned _m_tabs_pixels(size_type tabs) const;
			nana::size _m_text_extent_size(const char_type*) const;
			nana::size _m_text_extent_size(const char_type*, size_type n) const;

			//_m_move_offset_x_while_over_border
			//@brief: Moves the view window
			bool _m_move_offset_x_while_over_border(int many);

			int _m_text_top_base() const;
			//_m_endx
			//@brief: Gets the right point of text area
			int _m_endx() const;
			//_m_endy
			//@brief: Get the bottom point of text area
			int _m_endy() const;

			void _m_draw_tip_string() const;
			void _m_update_line(std::size_t textline) const;
			//_m_draw_string
			//@brief: Draw a line of string
			void _m_draw_string(int top, unsigned color, std::size_t textline, bool if_mask) const;
			//_m_draw
			//@brief: Draw a character at a position specified by caret pos. 
			//@return: true if beyond the border
			bool _m_draw(nana::char_t);
			void _m_get_sort_select_points(nana::upoint& a, nana::upoint& b) const;

			void _m_offset_y(int y);

			//_m_adjust_caret_into_screen
			//@brief: Adjust the text offset in order to moving caret into visible area if it is out of the visible area
			//@note:	the function assumes the points_.caret is correct
			bool _m_adjust_caret_into_screen();

			//_m_screen_to_caret
			//@brief: Set the caret position from the screen point specified by (x, y)
			//@param x, y: screen point
			nana::upoint _m_screen_to_caret(int x, int y) const;
			unsigned _m_pixels_by_char(std::size_t textline, std::size_t pos) const;
			static bool _m_is_right_text(const unicode_bidi::entity&);
		private:
			nana::gui::window window_;
			graph_reference graph_;
			skeletons::textbase<nana::char_t> textbase_;
			nana::char_t mask_char_;

			mutable ext_renderer_tag ext_renderer_;

			struct attributes
			{
				attributes();

				nana::string tip_string;

				bool multi_lines;
				bool editable;
				bool enable_background;
				bool enable_counterpart;
				nana::paint::graphics counterpart; //this is used to keep the background that painted by external part.

				nana::gui::scroll<true>*	vscroll;
				nana::gui::scroll<false>*	hscroll;
			}attributes_;

			struct text_area
			{
				nana::rectangle	area;

				bool captured;
				unsigned long tab_space;
				unsigned long vscroll;
				unsigned long hscroll;
				std::function<void(nana::paint::graphics&)> border_renderer;
			}text_area_;

			struct selection
			{
				enum mode_selection_t{mode_no_selected, mode_mouse_selected, mode_method_selected};

				mode_selection_t mode_selection;
				bool dragged;
				nana::upoint a, b;
			}select_;

			struct coordinate
			{
				coordinate();
				nana::point offset;	//x stands for pixels, y for lines
				nana::upoint caret;	//position of caret by text, it specifies the position of a new character
				unsigned long xpos;	//This data is used for move up/down
			}points_;
		};
	}//end namespace skeletons
}//end namespace widgets
}//end namespace gui
}//end namespace nana

#endif

