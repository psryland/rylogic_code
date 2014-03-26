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

			enum class seekdir
			{
				bilateral, forward, backward
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
				: public drawer_trigger
			{
			public:
				typedef controller controller_t;

				trigger();
				~trigger();
				controller_t* ctrl() const;
			private:
				void attached(widget_reference, graph_reference)	override;
				void detached()	override;
				void refresh(graph_reference)	override;
				void mouse_down(graph_reference, const eventinfo&)	override;
				void mouse_up(graph_reference, const eventinfo&)	override;
				void mouse_move(graph_reference, const eventinfo&)	override;
				void mouse_leave(graph_reference, const eventinfo&)	override;
				void resize(graph_reference, const eventinfo&)		override;
			private:
				controller_t * impl_;
			};
		}//end namespace slider
	}//end namespace drawerbase

	class slider
		: public widget_object<category::widget_tag, drawerbase::slider::trigger>
	{
	public:
		typedef drawerbase::slider::renderer renderer;
		typedef drawerbase::slider::provider provider;
		typedef drawerbase::slider::seekdir seekdir;
		typedef drawerbase::slider::extra_events ext_event_type;

		slider();
		slider(window, bool visible);
		slider(window, const rectangle& = rectangle(), bool visible = true);
		ext_event_type& ext_event() const;
		void seek(seekdir);
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
