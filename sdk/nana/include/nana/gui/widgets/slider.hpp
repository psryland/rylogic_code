#ifndef NANA_GUI_WIDGETS_SLIDER_HPP
#define NANA_GUI_WIDGETS_SLIDER_HPP
#include "widget.hpp"
#include <nana/pat/cloneable.hpp>

namespace nana{ namespace gui{
	class slider;
	namespace drawerbase
	{
		namespace slider
		{
			struct extra_events
			{
				nana::fn_group<void(nana::gui::slider&)> value_changed;
			};

			struct seekdir
			{
				enum t{bilateral, forward, backward};
			};

			class provider
			{
			public:
				virtual ~provider() = 0;
				virtual nana::string adorn_trace(unsigned vmax, unsigned vadorn) const = 0;
			};

			class renderer
			{
			public:
				typedef nana::paint::graphics & graph_reference;

				struct bar_t
				{
					bool horizontal;
					nana::rectangle r;		//the rectangle of bar.
					unsigned border_size;	//border_size of bar.
				};

				struct slider_t
				{
					bool horizontal;
					int pos;
					unsigned border;
					unsigned scale;
				};

				struct adorn_t
				{
					bool horizontal;
					nana::point bound;
					int fixedpos;
					unsigned block;
					unsigned vcur_scale;	//pixels of vcur scale.
				};

				virtual ~renderer() = 0;

				virtual void background(window, graph_reference, bool isglass) = 0;
				virtual void adorn(window, graph_reference, const adorn_t&) = 0;
				virtual void adorn_textbox(window, graph_reference, const nana::string&, const nana::rectangle&) = 0;
				virtual void bar(window, graph_reference, const bar_t&) = 0;
				virtual void slider(window, graph_reference, const slider_t&) = 0;
			};

			class controller;

			class trigger
				: public nana::gui::drawer_trigger
			{
			public:
				typedef controller controller_t;

				trigger();
				~trigger();
				controller_t* ctrl() const;
			private:
				void bind_window(widget_reference);
				void attached(graph_reference);
				void detached();
				void refresh(graph_reference);
				void mouse_down(graph_reference, const eventinfo&);
				void mouse_up(graph_reference, const eventinfo&);
				void mouse_move(graph_reference, const eventinfo&);
				void mouse_leave(graph_reference, const eventinfo&);
				void resize(graph_reference, const eventinfo&);
			private:
				controller_t * impl_;
			};
		}//end namespace slider
	}//end namespace drawerbase

	/// The slider can be used for dragging for tracking
	class slider
		: public widget_object<category::widget_tag, drawerbase::slider::trigger>
	{
	public:
		typedef drawerbase::slider::renderer renderer;
		typedef drawerbase::slider::provider provider;
		typedef drawerbase::slider::seekdir seekdir;
		typedef drawerbase::slider::extra_events ext_event_type;

		/// The default constructor without creating a widget.
		slider();

		/// The constructor that creates a widget.
		/// @param window, a handle to the parent window of the widget being created.
		/// @param visible, specifying the visible of the widget after creating.
		slider(window, bool visible);

		/// The constructor that creates a widget.
		/// @param window, a handle to the parent window of the widget being created.
		/// @param rectangle, the size and position of the widget in its parent window coordinate.
		/// @param visible, specifying the visible of the widget after creating.
		slider(window, const rectangle& = rectangle(), bool visible = true);
		ext_event_type& ext_event() const;
		void seek(seekdir::t);
		void vertical(bool);
		bool vertical() const;
		void vmax(unsigned);
		unsigned vmax() const;
		void value(unsigned);
		unsigned value() const;
		unsigned move_step(bool forward);
		unsigned adorn() const;

		pat::cloneable<renderer>& ext_renderer();
		void ext_renderer(const pat::cloneable<renderer>&);
		void ext_provider(const pat::cloneable<provider>&);
		void transparent(bool);
		bool transparent() const;
	};
}//end namespace gui
}//end namespace nana

#endif
