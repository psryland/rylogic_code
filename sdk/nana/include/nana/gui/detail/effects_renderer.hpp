#ifndef NANA_GUI_DETAIL_EFFECTS_RENDERER_HPP
#define NANA_GUI_DETAIL_EFFECTS_RENDERER_HPP
#include <nana/gui/effects.hpp>
#include <nana/paint/graphics.hpp>
#include <nana/paint/pixel_buffer.hpp>
#include "window_layout.hpp"

namespace nana{	namespace gui{
	namespace detail
	{
		template<typename CoreWindow>
		class edge_nimbus_renderer
		{
			edge_nimbus_renderer(){}
		public:
			typedef CoreWindow core_window_t;
			typedef window_layout<core_window_t> window_layer;
			typedef nana::paint::graphics & graph_reference;

			static edge_nimbus_renderer& instance()
			{
				static edge_nimbus_renderer object;
				return object;
			}

			std::size_t weight() const
			{
				return 2;
			}

			bool render(core_window_t * wd)
			{
				bool rendered = false;
				core_window_t * root_wd = wd->root_widget;
				if(root_wd->other.attribute.root->effects_edge_nimbus.size())
				{
					core_window_t * focused = root_wd->other.attribute.root->focus;
					native_window_type native = root_wd->root;
					std::size_t pixels = weight();

					nana::paint::graphics * graph = root_wd->root_graph;

					std::vector<core_window_t*> erase;
					std::vector<nana::rectangle>	r_set;
					nana::rectangle r;
					typename core_window_t::edge_nimbus_container & cont = root_wd->other.attribute.root->effects_edge_nimbus;
					for(typename core_window_t::edge_nimbus_container::iterator i = cont.begin(); i != cont.end(); ++i)
					{
						typename core_window_t::edge_nimbus_action & ena = *i;
						if(_m_edge_nimbus(focused, ena.window) && window_layer::read_visual_rectangle(ena.window, r))
						{
							if(ena.window == wd)
								rendered = true;

							r_set.push_back(r);
							ena.rendered = true;
						}
						else if(ena.rendered)
						{
							ena.rendered = false;
							erase.push_back(ena.window);
						}
					}

					//Erase
					for(typename std::vector<core_window_t*>::iterator i = erase.begin(); i != erase.end(); ++i)
					{
						core_window_t * el = *i;
						if(el == wd)
							rendered = true;

						r.x = el->pos_root.x - static_cast<int>(pixels);
						r.y = el->pos_root.y - static_cast<int>(pixels);
						r.width = static_cast<unsigned>(el->dimension.width + (pixels << 1));
						r.height = static_cast<unsigned>(el->dimension.height + (pixels << 1));
						graph->paste(native, r, r.x, r.y);
					}

					std::vector<nana::rectangle>::iterator visual_iterator = r_set.begin();
					//Render
					for(typename core_window_t::edge_nimbus_container::iterator i = cont.begin(); i != cont.end(); ++i)
					{
						if(i->rendered)
							_m_render_edge_nimbus(i->window, *(visual_iterator++));
					}
				}
				return rendered;
			}
		private:
			static bool _m_edge_nimbus(core_window_t * focused_wd, core_window_t * wd)
			{
				if((focused_wd == wd) && (wd->effect.edge_nimbus & effects::edge_nimbus::active))
					return true;
				else if((wd->effect.edge_nimbus & effects::edge_nimbus::over) && (wd->flags.action == mouse_action::over))
					return true;
				return false;
			}

			void _m_render_edge_nimbus(core_window_t* wd, const nana::rectangle & visual)
			{
				nana::rectangle r(visual);
				r.pare_off(-static_cast<int>(weight()));
				nana::rectangle good_r;
				if(nana::gui::overlap(r, nana::rectangle(wd->root_graph->size()), good_r))
				{
					if(	(good_r.x < wd->pos_root.x) || (good_r.y < wd->pos_root.y) ||
						(good_r.x + good_r.width > visual.x + visual.width) || (good_r.y + good_r.height > visual.y + visual.height))
					{
						nana::paint::graphics * graph = wd->root_graph;
						nana::paint::pixel_buffer pixbuf(graph->handle(), r);

						pixel_rgb_t px0, px1, px2, px3;
						
						px0 = pixbuf.pixel(0, 0);
						px1 = pixbuf.pixel(r.width - 1, 0);
						px2 = pixbuf.pixel(0, r.height - 1);
						px3 = pixbuf.pixel(r.width - 1, r.height - 1);

						good_r.x = good_r.y = 1;
						good_r.width = r.width - 2;
						good_r.height = r.height - 2;
						pixbuf.rectangle(good_r, wd->color.active, 0.95, false);

						good_r.x = good_r.y = 0;
						good_r.width = r.width;
						good_r.height = r.height;
						pixbuf.rectangle(good_r, wd->color.active, 0.4, false);

						pixbuf.pixel(0, 0, px0);
						pixbuf.pixel(r.width - 1, 0, px1);
						pixbuf.pixel(0, r.height - 1, px2);
						pixbuf.pixel(r.width - 1, r.height - 1, px3);

						pixbuf.paste(wd->root, r.x, r.y);

						std::vector<typename window_layer::wd_rectangle> overlaps;
						if(window_layer::read_overlaps(wd, visual, overlaps))
						{
							typename std::vector<typename window_layer::wd_rectangle>::iterator i = overlaps.begin(), end = overlaps.end();
							for(; i != end; ++i)
							{
								const nana::rectangle& r = i->r;
								graph->paste(wd->root, r, r.x, r.y);
							}
						}
					}
					else
						wd->root_graph->paste(wd->root, visual, visual.x, visual.y);
				}
			}
		};
	}
}//end namespace gui
}//end namespace nana

#endif
