#ifndef NANA_GUI_WIDGET_TEXTBOX_HPP
#define NANA_GUI_WIDGET_TEXTBOX_HPP
#include <nana/gui/widgets/widget.hpp>
#include "skeletons/textbase_extra_evtbase.hpp"

namespace nana{ namespace gui{
	namespace widgets
	{
		namespace skeletons
		{
			class text_editor;
		}
	}

	namespace drawerbase
	{
		namespace textbox
		{
			//class drawer
			class drawer
				: public drawer_trigger
			{
			public:
				typedef widgets::skeletons::text_editor text_editor;

				struct extra_evtbase_t
					: widgets::skeletons::textbase_extra_evtbase<nana::char_t>
				{};

				mutable extra_evtbase_t	extra_evtbase;

				drawer();
				bool border(bool);
				text_editor * editor();
				const text_editor * editor() const;
			private:
				void attached(widget_reference, graph_reference)	override;
				void detached()	override;
				void refresh(graph_reference)	override;
				void focus(graph_reference, const eventinfo&)	override;
				void mouse_down(graph_reference, const eventinfo&)	override;
				void mouse_move(graph_reference, const eventinfo&)	override;
				void mouse_up(graph_reference, const eventinfo&)	override;
				void mouse_enter(graph_reference, const eventinfo&)	override;
				void mouse_leave(graph_reference, const eventinfo&)	override;
				void key_down(graph_reference, const eventinfo&)	override;
				void key_char(graph_reference, const eventinfo&)	override;
				void mouse_wheel(graph_reference, const eventinfo&)	override;
				void resize(graph_reference, const eventinfo&)		override;
			private:
				void _m_text_area(unsigned width, unsigned height);
				void _m_draw_border(graph_reference);
			private:
				widget*	widget_;
				struct status_type
				{
					bool border;
					bool has_focus;		//Indicates whether it has the keyboard focus
				}status_;

				widgets::skeletons::text_editor * editor_;
			};
		}//end namespace textbox
	}//end namespace drawerbase

	class textbox
		:public widget_object<category::widget_tag, drawerbase::textbox::drawer>
	{
		typedef drawer_trigger_t::extra_evtbase_t ext_event_type;
	public:
		/// The default constructor without creating the widget.
		textbox();

		/// The construct that creates a widget.
		/// @param wd, A handle to the parent window of the widget being created.
		/// @param visible, specifying the visible after creating.
		textbox(window, bool visible);

		/// The construct that creates a widget with a specified text.
		/// @param window, A handle to the parent window of the widget being created.
		/// @param text, the text that will be displayed.
		/// @param visible, specifying the visible after creating.
		textbox(window, const nana::string& text, bool visible = true);

		/// The construct that creates a widget with a specified text.
		/// @param window, A handle to the parent window of the widget being created.
		/// @param text, the text that will be displayed.
		/// @param visible, specifying the visible after creating.
		textbox(window, const nana::char_t* text, bool visible = true);

		/// The construct that creates a widget.
		/// @param window, A handle to the parent window of the widget being created.
		/// @param rectangle, the size and position of the widget in its parent window coordinate.
		/// @param visible, specifying the visible after creating.
		textbox(window, const rectangle& = rectangle(), bool visible = true);

		ext_event_type & ext_event() const;

		void load(const nana::char_t* file);
		void store(const nana::char_t* file) const;
		void store(const nana::char_t* file, nana::unicode encoding) const;

		/// The file of last store operation.
		std::string filename() const;

		/// Determine whether the text is edited.
		bool edited() const;

		/// Determine whether the changed text has been saved into the file.
		bool saved() const;

		bool getline(std::size_t n, nana::string&) const;
		textbox& append(const nana::string&, bool at_caret);
		textbox& border(bool);

		/// Determine whether the text is multi-line enabled.
		bool multi_lines() const;
		textbox& multi_lines(bool);
		bool editable() const;
		textbox& editable(bool);
		textbox& tip_string(const nana::string&);
		textbox& mask(nana::char_t);
		bool selected() const;
		void select(bool);

		void copy() const;
		void paste();
		void del();
	protected:
		//Override _m_caption for caption()
		nana::string _m_caption() const;
		void _m_caption(const nana::string&);
		//Override _m_typeface for changing the caret
		void _m_typeface(const nana::paint::font&);
	};

}//end namespace gui
}//end namespace nana
#endif
