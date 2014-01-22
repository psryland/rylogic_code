
#include <nana/gui/widgets/slider.hpp>

namespace nana{ namespace gui{
	namespace drawerbase
	{
		namespace slider
		{

			provider::~provider(){}

			renderer::~renderer(){}

			class interior_renderer
				: public renderer
			{
			private:
				virtual void background(window wd, graph_reference graph, bool isglass)
				{
					if(isglass == false)
						graph.rectangle(API::background(wd), true);
				}

				virtual void bar(window, graph_reference graph, const bar_t& bi)
				{
					//draw border
					nana::color_t dark = 0x83909F;
					nana::color_t gray = 0x9DAEC2;

					int x1 = bi.r.x + 1, x2 = bi.r.x + bi.r.width - 2;
					int y1 = bi.r.y, y2 = bi.r.y + bi.r.height - 1;

					graph.line(x1, y1, x2, y1, dark);
					graph.line(x1, y2, x2, y2, gray);
					x1 = bi.r.x;
					x2 = bi.r.x + bi.r.width - 1;
					y1 = bi.r.y + 1;
					y2 = bi.r.y + bi.r.height - 2;

					graph.line(x1, y1, x1, y2, dark);
					graph.line(x2, y1, x2, y2, gray);
				}

				virtual void adorn(window, graph_reference graph, const adorn_t& ad)
				{
					int len = ad.bound.y - ad.bound.x;
					const unsigned upperblock = ad.block - ad.block / 2;
					if(ad.horizontal)
					{
						graph.shadow_rectangle(ad.bound.x, ad.fixedpos, len, upperblock, 0x84C5FF, 0x0F41CD, true);
						graph.shadow_rectangle(ad.bound.x, ad.fixedpos + upperblock, len, ad.block - upperblock, 0x0F41CD, 0x6E96FF, true);
					}
					else
					{
						graph.shadow_rectangle(ad.fixedpos, ad.bound.x, upperblock, len, 0x84C5FF, 0x0F41CD, false);
						graph.shadow_rectangle(ad.fixedpos + upperblock, ad.bound.x, ad.block - upperblock, len, 0x0F41CD, 0x6E96FF, false);
					}
				}

				virtual void adorn_textbox(window, graph_reference graph, const nana::string& str, const nana::rectangle & r)
				{
					graph.rectangle(r, 0xFFFFFF, false);
					graph.string(r.x + 2, r.y + 1, 0xFFFFFF, str);
				}

				virtual void slider(window, graph_reference graph, const slider_t& s)
				{
					nana::rectangle r = graph.size();
					if(s.horizontal)
					{
						r.x = s.pos;
						r.width = s.scale;
					}
					else
					{
						r.y = s.pos;
						r.height = s.scale;
					}
					graph.round_rectangle(r, 3, 3, 0x0, true, 0xF0F0F0);
				}
			};

			class controller
			{
			public:
				enum dir_t{DirHorizontal, DirVertical};
				enum where_t{WhereNone, WhereBar, WhereSlider};
				
				typedef nana::gui::drawer_trigger::graph_reference graph_reference;
				mutable extra_events ext_event;

				controller()
				{
					other_.wd = 0;
					other_.widget = 0;
					other_.graph = 0;

					proto_.renderer = pat::cloneable<renderer>(interior_renderer());

					attr_.skdir = seekdir::bilateral;
					attr_.dir = this->DirHorizontal;
					attr_.vcur = 0;
					attr_.vmax = 10;
					attr_.slider_scale = 8;
					attr_.border = 1;
					attr_.is_draw_adorn = false;
				}

				void seek(seekdir::t sd)
				{
					attr_.skdir = sd;
				}

				void bind(nana::gui::slider& wd)
				{
					other_.wd = wd.handle();
					other_.widget = &wd;
				}

				window handle() const
				{
					return other_.wd;
				}

				void attached(graph_reference graph)
				{
					other_.graph = &graph;
					this->_m_mk_slider_pos_by_value();
				}

				void detached()
				{
					other_.graph = 0;
				}

				pat::cloneable<renderer>& ext_renderer()
				{
					return proto_.renderer;
				}

				void ext_renderer(const pat::cloneable<renderer>& rd)
				{
					proto_.renderer = rd;
				}

				void ext_provider(const pat::cloneable<provider>& pd)
				{
					proto_.provider = pd;
				}

				void draw()
				{
					if(other_.graph && !other_.graph->size().is_zero())
					{
						bool is_transparent = (bground_mode::basic == API::effects_bground_mode(other_.wd));
						proto_.renderer->background(other_.wd, *other_.graph, is_transparent);
						_m_draw_objects();
					}
				}

				void vertical(bool v)
				{
					dir_t dir = (v ? this->DirVertical : this->DirHorizontal);

					if(dir != attr_.dir)
					{
						attr_.dir = dir;
						this->draw();
					}
				}

				bool vertical() const
				{
					return (this->DirVertical == attr_.dir);
				}

				void vmax(unsigned m)
				{
					if(m == 0) m = 1;

					if(attr_.vmax != m)
					{
						attr_.vmax = m;
						if(attr_.vcur > m)
						{
							attr_.vcur = m;
							ext_event.value_changed(*other_.widget);
						}
						_m_mk_slider_pos_by_value();
						draw();
					}
				}

				unsigned vmax() const
				{
					return attr_.vmax;
				}

				void vcur(unsigned v)
				{
					if(attr_.vmax < v)
						v = attr_.vmax;

					if(attr_.vcur != v)
					{
						attr_.vcur = v;
						ext_event.value_changed(*other_.widget);
						this->_m_mk_slider_pos_by_value();
						draw();
					}
				}

				unsigned vcur() const
				{
					return attr_.vcur;
				}

				void resize()
				{
					this->_m_mk_slider_pos_by_value();
					attr_.adorn_pos = attr_.pos;
				}

				where_t seek_where(int x, int y) const
				{
					nana::rectangle r = _m_bar_area();
					if(attr_.dir == this->DirVertical)
					{
						std::swap(x, y);
						std::swap(r.width, r.height);
					}

					int pos = _m_slider_pos();
					if(pos <= x && x < pos + static_cast<int>(attr_.slider_scale))
						return WhereSlider;

					pos = static_cast<int>(attr_.slider_scale) / 2;
					
					if(pos <= x && x < pos + static_cast<int>(r.width))
					{
						if(y < r.y + static_cast<int>(r.height))
							return WhereBar;
					}
					return WhereNone;
				}

				//set_slider_pos
				//move the slider to a position where a mouse click on WhereBar.
				bool set_slider_pos(int x, int y)
				{
					if(this->DirVertical == attr_.dir)
						std::swap(x, y);

					x -= _m_slider_refpos();
					if(x < 0)
						return false;

					if(x > static_cast<int>(_m_scale()))
						x = static_cast<int>(_m_scale());

					double pos = attr_.pos;
					double dx = _m_evaluate_by_seekdir(x);

					attr_.pos = dx;
					attr_.adorn_pos = dx;
					_m_mk_slider_value_by_pos();

					return (attr_.pos != pos);
				}

				void set_slider_refpos(int x, int y)
				{
					if(this->DirVertical == attr_.dir)
						std::swap(x, y);

					slider_state_.trace = slider_state_.TraceCapture;
					slider_state_.snap_pos = static_cast<int>(attr_.pos);
					slider_state_.refpos.x = x;
					slider_state_.refpos.y = y;

					API::capture_window(other_.wd, true);
				}

				bool release_slider()
				{
					if(slider_state_.trace == slider_state_.TraceCapture)
					{
						API::capture_window(other_.wd, false);
						if(other_.wd != nana::gui::API::find_window(nana::gui::API::cursor_position()))
						{
							slider_state_.trace = slider_state_.TraceNone;
							attr_.is_draw_adorn = false;
						}
						else
							slider_state_.trace = slider_state_.TraceOver;

						_m_mk_slider_value_by_pos();
						_m_mk_slider_pos_by_value();
						return true;
					}
					return false;
				}

				bool if_trace_slider() const
				{
					return (slider_state_.trace == slider_state_.TraceCapture);
				}

				bool move_slider(int x, int y)
				{
					int mpos = (this->DirHorizontal == attr_.dir ? x : y);
					int pos = slider_state_.snap_pos + (mpos - slider_state_.refpos.x);
					
					if(pos > 0) 
					{
						int scale = static_cast<int>(_m_scale());
						if(pos > scale)
							pos = scale;
					}
					else
						pos = 0;

					double dstpos = _m_evaluate_by_seekdir(pos);
					attr_.is_draw_adorn = true;

					if(dstpos != attr_.pos)
					{
						attr_.pos = dstpos;
						attr_.adorn_pos = dstpos;
						return true;
					}
					return false;
				}

				bool move_adorn(int x, int y)
				{
					double xpos = (this->DirHorizontal == attr_.dir ? x : y);

					xpos -= _m_slider_refpos();
					if(xpos > static_cast<int>(_m_scale()))
						xpos = static_cast<int>(_m_scale());

					int pos = static_cast<int>(attr_.adorn_pos);
					xpos = _m_evaluate_by_seekdir(xpos);

					attr_.adorn_pos = xpos;
					attr_.is_draw_adorn = true;

					if(slider_state_.trace == slider_state_.TraceNone)
						slider_state_.trace = slider_state_.TraceOver;

					return (pos != static_cast<int>(xpos));
				}

				unsigned move_step(bool forward)
				{
					unsigned cmpvalue = attr_.vcur;
					if(forward)
					{
						if(attr_.vcur)
							--attr_.vcur;
					}
					else if(attr_.vcur < attr_.vmax)
						++attr_.vcur;

					if(cmpvalue != attr_.vcur)
					{
						this->_m_mk_slider_pos_by_value();
						this->draw();
						ext_event.value_changed(*other_.widget);
					}
					return cmpvalue;
				}

				unsigned adorn() const
				{
					return _m_value_by_pos(attr_.adorn_pos);
				}

				bool reset_adorn()
				{
					//Test if the slider is captured, the operation should be ignored. Because the mouse_leave always be generated even through
					//the slider is captured.
					if(slider_state_.trace == slider_state_.TraceCapture && (nana::gui::API::capture_window() == this->other_.wd))
						return false;

					slider_state_.trace = slider_state_.TraceNone;
					attr_.is_draw_adorn = false;
					if(attr_.adorn_pos != attr_.pos)
					{
						attr_.adorn_pos = attr_.pos;
						return true;
					}
					return false;
				}

			private:
				nana::rectangle _m_bar_area() const
				{
					nana::rectangle r;
					nana::size sz = other_.graph->size();
					if(this->DirHorizontal == attr_.dir)
					{
						r.x = attr_.slider_scale / 2 - attr_.border;
						r.y = 0;
						r.width = (static_cast<int>(sz.width) > (r.x << 1) ? sz.width - (r.x << 1) : 0);
						r.height = sz.height;
					}
					else
					{
						r.x = 0;
						r.y = attr_.slider_scale / 2 - attr_.border;
						r.width = sz.width;
						r.height = (static_cast<int>(sz.height) > (r.y << 1) ? sz.height - (r.y << 1) : 0);
					}
					return r;
				}

				unsigned _m_scale() const
				{
					nana::rectangle r = _m_bar_area();
					return ((this->DirHorizontal == attr_.dir ? r.width : r.height) - attr_.border * 2);
				}

				double _m_evaluate_by_seekdir(double pos) const
				{
					switch(attr_.skdir)
					{
					case seekdir::backward:
						if(pos < attr_.pos)
							pos = attr_.pos;
						break;
					case seekdir::forward:
						if(pos > attr_.pos)
							pos = attr_.pos;
						break;
					default:	break;
					}
					return pos;
				}

				int _m_slider_refpos() const
				{
					return static_cast<int>(attr_.slider_scale / 2);
				}

				int _m_slider_pos() const
				{
					return static_cast<int>(_m_scale() * attr_.vcur / attr_.vmax);
				}

				unsigned _m_mk_slider_value_by_pos()
				{
					if(_m_scale())
					{
						unsigned cmpvalue = attr_.vcur;
						attr_.vcur = static_cast<unsigned>(attr_.pos * attr_.vmax / _m_scale());
						if(cmpvalue != attr_.vcur)
							ext_event.value_changed(*other_.widget);
					}
					return attr_.vcur;
				}

				int _m_mk_slider_pos_by_value()
				{
					attr_.pos = double(_m_scale()) * attr_.vcur / attr_.vmax;

					if(slider_state_.trace == slider_state_.TraceNone)
						attr_.adorn_pos = attr_.pos;
					
					return static_cast<int>(attr_.pos);
				}

				unsigned _m_value_by_pos(double pos) const
				{
					if(_m_scale())
						return static_cast<int>(pos * attr_.vmax / _m_scale());
					return 0;
				}

				void _m_draw_objects()
				{
					renderer::bar_t bar;

					bar.horizontal = (this->DirHorizontal == attr_.dir);
					bar.border_size = attr_.border;
					bar.r = _m_bar_area();

					if(0 == bar.r.width || 0 == bar.r.height)
						return;

					proto_.renderer->bar(other_.wd, *other_.graph, bar);

					//adorn
					renderer::adorn_t adorn;
					adorn.horizontal = bar.horizontal;
					adorn.bound.x = (bar.horizontal ? bar.r.x : bar.r.y) + attr_.border;
					adorn.bound.y = adorn.bound.x + static_cast<int>(attr_.adorn_pos);
					adorn.vcur_scale = static_cast<unsigned>(attr_.pos);
					adorn.block = (bar.horizontal ? bar.r.height : bar.r.width) - attr_.border * 2;
					adorn.fixedpos = static_cast<int>((bar.horizontal ? bar.r.y : bar.r.x) + attr_.border);

					proto_.renderer->adorn(other_.wd, *other_.graph, adorn);

					_m_draw_slider();

					//adorn textbox
					if(proto_.provider && attr_.is_draw_adorn)
					{
						unsigned vadorn = _m_value_by_pos(attr_.adorn_pos);
						nana::string str = proto_.provider->adorn_trace(attr_.vmax, vadorn);
						if(str.size())
						{
							nana::rectangle r;
							nana::size ts = other_.graph->text_extent_size(str);
							ts.width += 6;
							ts.height += 2;

							r.width = ts.width;
							r.height = ts.height;

							const int room = static_cast<int>(attr_.adorn_pos);
							if(bar.horizontal)
							{
								r.y = adorn.fixedpos + static_cast<int>(adorn.block - ts.height) / 2;
								if(room > static_cast<int>(ts.width + 2))
									r.x = room - static_cast<int>(ts.width + 2);
								else
									r.x = room + 2;

								r.x += this->_m_slider_refpos();
							}
							else
							{
								r.x = (other_.graph->width() - ts.width) / 2;
								if(room > static_cast<int>(ts.height + 2))
									r.y = room - static_cast<int>(ts.height + 2);
								else
									r.y = room + 2;
								r.y += this->_m_slider_refpos();
							}
							proto_.renderer->adorn_textbox(other_.wd, *other_.graph, str, r);
						}
					}
				}

				void _m_draw_slider()
				{
					renderer::slider_t s;
					s.pos = static_cast<int>(attr_.pos);
					s.horizontal = (this->DirHorizontal == attr_.dir);
					s.scale = attr_.slider_scale;
					s.border = attr_.border;
					proto_.renderer->slider(other_.wd, *other_.graph, s);
				}
			private:
				struct other_tag
				{
					window wd;
					nana::gui::slider * widget;
					paint::graphics * graph;
				}other_;
				
				struct prototype_tag
				{
					pat::cloneable<slider::renderer> renderer;
					pat::cloneable<slider::provider> provider;
				}proto_;

				struct attr_tag
				{
					seekdir::t skdir;
					dir_t dir;
					unsigned border;
					unsigned vmax;
					unsigned vcur;
					double		pos;
					bool		is_draw_adorn;
					double		adorn_pos;
					unsigned slider_scale;
				}attr_;

				struct slider_state_tag
				{
					enum t{TraceNone, TraceOver, TraceCapture};

					t trace;	//true if the mouse press on slider.
					int		snap_pos;
					nana::point refpos; //a point for slider when the mouse was clicking on slider.

					slider_state_tag(): trace(TraceNone){}
				}slider_state_;
			};

			//class trigger
				trigger::trigger()
					: impl_(new controller_t)
				{}

				trigger::~trigger()
				{
					delete impl_;
				}

				trigger::controller_t* trigger::ctrl() const
				{
					return impl_;
				}

				void trigger::bind_window(trigger::widget_reference wd)
				{
					impl_->bind(static_cast<nana::gui::slider&>(wd));
				}

				void trigger::attached(graph_reference graph)
				{
					impl_->attached(graph);
					window wd = impl_->handle();
					using namespace API::dev;
					make_drawer_event<events::mouse_down>(wd);
					make_drawer_event<events::mouse_up>(wd);
					make_drawer_event<events::mouse_move>(wd);
					make_drawer_event<events::mouse_leave>(wd);
					make_drawer_event<events::size>(wd);
				}

				void trigger::detached()
				{
					API::dev::umake_drawer_event(impl_->handle());
					impl_->detached();
				}

				void trigger::refresh(graph_reference)
				{
					impl_->draw();
				}

				void trigger::mouse_down(graph_reference, const eventinfo& ei)
				{
					controller_t::where_t what = impl_->seek_where(ei.mouse.x, ei.mouse.y);
					if(controller_t::WhereBar == what || controller_t::WhereSlider == what)
					{
						bool mkdir = impl_->set_slider_pos(ei.mouse.x, ei.mouse.y);
						impl_->set_slider_refpos(ei.mouse.x, ei.mouse.y);
						if(mkdir)
						{
							impl_->draw();
							API::lazy_refresh();
						}
					}
				}

				void trigger::mouse_up(graph_reference, const eventinfo&)
				{
					bool mkdraw = impl_->release_slider();
					if(mkdraw)
					{
						impl_->draw();
						API::lazy_refresh();
					}
				}

				void trigger::mouse_move(graph_reference, const eventinfo& ei)
				{
					bool mkdraw = false;
					if(impl_->if_trace_slider())
					{
						mkdraw = impl_->move_slider(ei.mouse.x, ei.mouse.y);
					}
					else
					{
						controller_t::where_t what = impl_->seek_where(ei.mouse.x, ei.mouse.y);
						if(controller_t::WhereNone != what)
							mkdraw = impl_->move_adorn(ei.mouse.x, ei.mouse.y);
						else
							mkdraw = impl_->reset_adorn();
					}

					if(mkdraw)
					{
						impl_->draw();
						API::lazy_refresh();
					}
				}

				void trigger::mouse_leave(graph_reference, const eventinfo&)
				{
					if(impl_->reset_adorn())
					{
						impl_->draw();
						API::lazy_refresh();
					}
				}

				void trigger::resize(graph_reference, const eventinfo&)
				{
					impl_->resize();
					impl_->draw();
					API::lazy_refresh();
				}
			//end class trigger
		}//end namespace slider
	}//end namespace drawerbase

	//class slider
		slider::slider(){}
		
		slider::slider(window wd, bool visible)
		{
			create(wd, rectangle(), visible);
		}

		slider::slider(window wd, const rectangle& r, bool visible)
		{
			create(wd, r, visible);
		}

		slider::ext_event_type& slider::ext_event() const
		{
			return get_drawer_trigger().ctrl()->ext_event;
		}

		void slider::seek(slider::seekdir::t sd)
		{
			get_drawer_trigger().ctrl()->seek(sd);
		}

		void slider::vertical(bool v)
		{
			get_drawer_trigger().ctrl()->vertical(v);
			API::update_window(this->handle());
		}

		bool slider::vertical() const
		{
			return get_drawer_trigger().ctrl()->vertical();
		}

		void slider::vmax(unsigned m)
		{
			if(this->handle())
			{
				get_drawer_trigger().ctrl()->vmax(m);
				API::update_window(handle());
			}
		}

		unsigned slider::vmax() const
		{
			if(handle())
				return get_drawer_trigger().ctrl()->vmax();
			return 0;
		}

		void slider::value(unsigned v)
		{
			if(handle())
			{
				get_drawer_trigger().ctrl()->vcur(v);
				API::update_window(handle());
			}
		}

		unsigned slider::value() const
		{
			if(handle())
				return get_drawer_trigger().ctrl()->vcur();
			return 0;
		}

		unsigned slider::move_step(bool forward)
		{
			if(handle())
			{
				drawerbase::slider::controller* ctrl = this->get_drawer_trigger().ctrl();
				unsigned val = ctrl->move_step(forward);
				if(val != ctrl->vcur())
				{
					API::update_window(handle());
				}
			}
			return 0;
		}

		unsigned slider::adorn() const
		{
			if(handle())
				return get_drawer_trigger().ctrl()->adorn();
			return 0;
		}

		pat::cloneable<slider::renderer>& slider::ext_renderer()
		{
			return get_drawer_trigger().ctrl()->ext_renderer();
		}

		void slider::ext_renderer(const pat::cloneable<slider::renderer>& di)
		{
			get_drawer_trigger().ctrl()->ext_renderer(di);
		}

		void slider::ext_provider(const pat::cloneable<slider::provider>& pi)
		{
			get_drawer_trigger().ctrl()->ext_provider(pi);
		}

		void slider::transparent(bool enabled)
		{
			if(enabled)
				API::effects_bground(*this, effects::bground_transparent(0), 0.0);
			else
				API::effects_bground_remove(*this);
		}

		bool slider::transparent() const
		{
			return (bground_mode::basic == API::effects_bground_mode(*this));
		}
	//end class slider
}//end namespace gui
}//end namespace nana
