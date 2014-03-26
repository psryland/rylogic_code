/*
 *	Window Manager Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0.
 *	(See accompanying file LICENSE_1_0.txt or copy at
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/detail/window_manager.cpp
 *
 */

#include <nana/config.hpp>
#include GUI_BEDROCK_HPP
#include <nana/gui/detail/window_manager.hpp>
#include <nana/gui/detail/native_window_interface.hpp>
#include <nana/gui/detail/inner_fwd_implement.hpp>
#include <nana/gui/layout_utility.hpp>
#include <stdexcept>
#include <algorithm>

namespace nana
{
namespace gui
{
namespace detail
{
	//class tray_event_manager
	class tray_event_manager
	{
		typedef std::function<void(const eventinfo&)> function_type;
		typedef std::vector<function_type> fvec_t;
		typedef std::map<event_code, fvec_t> event_maptable_type;
		typedef std::map<native_window_type, event_maptable_type> maptable_type;
	public:
		void fire(native_window_type, event_code, const eventinfo&);
		bool make(native_window_type, event_code, const function_type&);
		void umake(native_window_type);
	private:
		maptable_type maptable_;
	};//end class tray_event_manager

	//class tray_event_manager
		void tray_event_manager::fire(native_window_type wd, event_code identifier, const eventinfo& ei)
		{
			auto i = maptable_.find(wd);
			if(i == maptable_.end()) return;

			auto u = i->second.find(identifier);
			if(u == i->second.end()) return;

			for(auto & fn : u->second)
				fn(ei);
		}

		bool tray_event_manager::make(native_window_type wd, event_code code, const function_type & f)
		{
			if(wd)
			{
				maptable_[wd][code].push_back(f);
				return true;
			}
			return false;
		}

		void tray_event_manager::umake(native_window_type wd)
		{
			maptable_.erase(wd);
		}
	//end class tray_event_manager

	//class window_manager
		//struct wdm_private_impl
			struct window_manager::wdm_private_impl
			{
				root_register	misc_register;
				handle_manager<core_window_t*, window_manager>	wd_register;
				signal_manager	signal;
				tray_event_manager	tray_event;

				paint::image default_icon;
			};
		//end struct wdm_private_impl

		//class revertible_mutex
			window_manager::revertible_mutex::revertible_mutex()
			{
				thr_.tid = 0;
				thr_.refcnt = 0;
			}

			void window_manager::revertible_mutex::lock()
			{
				std::recursive_mutex::lock();
				if(0 == thr_.tid)
					thr_.tid = nana::system::this_thread_id();
				++thr_.refcnt;
			}

			bool window_manager::revertible_mutex::try_lock()
			{
				if(std::recursive_mutex::try_lock())
				{
					if(0 == thr_.tid)
						thr_.tid = nana::system::this_thread_id();
					++thr_.refcnt;
					return true;
				}
				return false;
			}

			void window_manager::revertible_mutex::unlock()
			{
				if(thr_.tid == nana::system::this_thread_id())
					if(0 == --thr_.refcnt)
						thr_.tid = 0;
				std::recursive_mutex::unlock();
			}

			void window_manager::revertible_mutex::revert()
			{
				if(thr_.refcnt && (thr_.tid == nana::system::this_thread_id()))
				{
					std::size_t cnt = thr_.refcnt;

					stack_.push_back(thr_);
					thr_.tid = 0;
					thr_.refcnt = 0;

					for(std::size_t i = 0; i < cnt; ++i)
						std::recursive_mutex::unlock();
				}
			}

			void window_manager::revertible_mutex::forward()
			{
				std::recursive_mutex::lock();
				if(stack_.size())
				{
					auto thr = stack_.back();
					if(thr.tid == nana::system::this_thread_id())
					{
						stack_.pop_back();
						for(std::size_t i = 0; i < thr.refcnt; ++i)
							std::recursive_mutex::lock();
						thr_ = thr;
					}
					else
						throw std::runtime_error("Nana.GUI: The forward is not matched.");
				}
				std::recursive_mutex::unlock();
			}
		//end class revertible_mutex

		window_manager::window_manager()
			: impl_(new wdm_private_impl)
		{
			attr_.capture.window = nullptr;
			attr_.capture.ignore_children = true;

			menu_.window = nullptr;
			menu_.owner = nullptr;
			menu_.has_keyboard = false;
		}

		window_manager::~window_manager()
		{
			delete impl_;
		}

		bool window_manager::is_queue(core_window_t* wd)
		{
			return (wd && (wd->other.category == category::root_tag::value));
		}

		std::size_t window_manager::number_of_core_window() const
		{
			return impl_->wd_register.size();
		}

		window_manager::mutex_type& window_manager::internal_lock() const
		{
			return mutex_;
		}

		void window_manager::all_handles(std::vector<core_window_t*> &v) const
		{
			impl_->wd_register.all(v);
		}

		void window_manager::detach_signal(core_window_t* wd)
		{
			impl_->signal.umake(wd);
		}

		void window_manager::signal_fire_caption(core_window_t* wd, const nana::char_t* str)
		{
			detail::signals sig;
			sig.info.caption = str;
			impl_->signal.call_signal(wd, signals::code::caption, sig);
		}

		nana::string window_manager::signal_fire_caption(core_window_t* wd)
		{
			nana::string str;
			detail::signals sig;
			sig.info.str = &str;
			impl_->signal.call_signal(wd, signals::code::read_caption, sig);
			return str;
		}

		void window_manager::event_filter(core_window_t* wd, bool is_make, event_code evtid)
		{
			switch(evtid)
			{
			case events::mouse_drop::identifier:
				wd->flags.dropable = (is_make ? true : (bedrock::instance().evt_manager.the_number_of_handles(reinterpret_cast<window>(wd), evtid, false) != 0));
				break;
			default:
				break;
			}
		}

		void window_manager::default_icon(const paint::image& img)
		{
			impl_->default_icon = img;
		}

		bool window_manager::available(core_window_t* wd)
		{
			return impl_->wd_register.available(wd);
		}

		bool window_manager::available(core_window_t * a, core_window_t* b)
		{
			return (impl_->wd_register.available(a) && impl_->wd_register.available(b));
		}

		bool window_manager::available(native_window_type wd)
		{
			if(wd)
			{
				std::lock_guard<decltype(mutex_)> lock(mutex_);
				return (impl_->misc_register.find(wd) != nullptr);
			}
			return false;
		}

		window_manager::core_window_t* window_manager::create_root(core_window_t* owner, bool nested, rectangle r, const appearance& app)
		{
			native_window_type native = nullptr;
			if(owner)
			{
				//Thread-Safe Required!
				std::lock_guard<decltype(mutex_)> lock(mutex_);

				if (impl_->wd_register.available(owner))
				{
					native = (owner->other.category == category::frame_tag::value ?
										owner->other.attribute.frame->container : owner->root_widget->root);
					r.x += owner->pos_root.x;
					r.y += owner->pos_root.y;
				}
				else
					owner = nullptr;
			}

			native_interface::window_result result = native_interface::create_window(native, nested, r, app);
			if(result.handle)
			{
				core_window_t* wd = new core_window_t(owner, (category::root_tag**)nullptr);
				wd->flags.take_active = !app.no_activate;
				wd->title = native_interface::window_caption(result.handle);

				//Thread-Safe Required!
				std::lock_guard<decltype(mutex_)> lock(mutex_);

				//create Root graphics Buffer and manage it
				root_misc misc(wd, result.width, result.height);
				auto* value = impl_->misc_register.insert(result.handle, misc);

				wd->bind_native_window(result.handle, result.width, result.height, result.extra_width, result.extra_height, value->root_graph);
				impl_->wd_register.insert(wd, wd->thread_id);

				if(owner && owner->other.category == category::frame_tag::value)
					insert_frame(owner, wd);

				bedrock::inc_window(wd->thread_id);
				this->icon(wd, impl_->default_icon);
				return wd;
			}
			return nullptr;
		}

		window_manager::core_window_t* window_manager::create_frame(core_window_t* parent, const rectangle& r)
		{
			if(parent == nullptr) return nullptr;
			//Thread-Safe Required!
			std::lock_guard<decltype(mutex_)> lock(mutex_);

			if (impl_->wd_register.available(parent) == false)	return nullptr;

			core_window_t * wd = new core_window_t(parent, r, (category::frame_tag**)nullptr);
			wd->frame_window(native_interface::create_child_window(parent->root, rectangle(wd->pos_root.x, wd->pos_root.y, r.width, r.height)));
			impl_->wd_register.insert(wd, wd->thread_id);

			//Insert the frame_widget into its root frames container.
			wd->root_widget->other.attribute.root->frames.push_back(wd);
			return (wd);
		}

		bool window_manager::insert_frame(core_window_t* frame, native_window wd)
		{
			if(frame)
			{
				//Thread-Safe Required!
				std::lock_guard<decltype(mutex_)> lock(mutex_);
				if(frame->other.category == category::frame_tag::value)
					frame->other.attribute.frame->attach.push_back(wd);
				return true;
			}
			return false;
		}

		bool window_manager::insert_frame(core_window_t* frame, core_window_t* wd)
		{
			if(frame)
			{
				//Thread-Safe Required!
				std::lock_guard<decltype(mutex_)> lock(mutex_);
				if(frame->other.category == category::frame_tag::value)
				{
					if (impl_->wd_register.available(wd) && wd->other.category == category::root_tag::value && wd->root != frame->root)
					{
						frame->other.attribute.frame->attach.push_back(wd->root);
						return true;
					}
				}
			}
			return false;
		}

		window_manager::core_window_t* window_manager::create_widget(core_window_t* parent, const rectangle& r, bool is_lite)
		{
			if(parent == nullptr)	return nullptr;
			//Thread-Safe Required!
			std::lock_guard<decltype(mutex_)> lock(mutex_);
			if (impl_->wd_register.available(parent) == false)	return nullptr;
			core_window_t * wd;
			if(is_lite)
				wd = new core_window_t(parent, r, (category::lite_widget_tag**)nullptr);
			else
				wd = new core_window_t(parent, r, (category::widget_tag**)nullptr);
			impl_->wd_register.insert(wd, wd->thread_id);
			return wd;
		}

		void window_manager::close(core_window_t *wd)
		{
			if(wd == nullptr) return;
			//Thread-Safe Required!
			std::lock_guard<decltype(mutex_)> lock(mutex_);
			if (impl_->wd_register.available(wd) == false)	return;

			if(wd->other.category == category::root_tag::value)
			{
				eventinfo ei;
				ei.unload.cancel = false;
				bedrock::raise_event(event_code::unload, wd, ei, true);
				if(false == ei.unload.cancel)
				{
					//Before close the window, its owner window should be actived, otherwise other window will be
					//activated due to the owner window is not enabled.
					if(wd->flags.modal || (wd->owner == nullptr) || wd->owner->flags.take_active)
						native_interface::activate_owner(wd->root);

					//Close should detach the drawer and send destroy signal to widget object.
					//Otherwise, when a widget object is been deleting in other thread by delete operator, the object will be destroyed
					//before the window_manager destroyes the window, and then, window_manager detaches the
					//non-existing drawer_trigger which is destroyed by destruction of widget. Crash!
					bedrock::instance().evt_manager.umake(reinterpret_cast<window>(wd), true);
					wd->drawer.detached();
					impl_->signal.call_signal(wd, signals::code::destroy, signals_);
					detach_signal(wd);

					native_interface::close_window(wd->root);
				}
			}
			else
				destroy(wd);
		}

		//destroy
		//@brief:	Delete the window handle
		void window_manager::destroy(core_window_t* wd)
		{
			if(wd == nullptr) return;
			core_window_t* parent = nullptr;
			{
				//Thread-Safe Required!
				std::lock_guard<decltype(mutex_)> lock(mutex_);
				if (impl_->wd_register.available(wd) == false)	return;

				parent = wd->parent;

				if(wd == attr_.capture.window)
					capture_window(wd, false);

				if(wd->other.category == category::root_tag::value)
				{
					root_runtime(wd->root)->shortkeys.clear();
					wd->other.attribute.root->focus = nullptr;
				}
				else
					unregister_shortkey(wd);

				if(parent)
				{
					auto & children = parent->children;
					auto i = std::find(children.begin(), children.end(), wd);
					if(i != children.end())
						children.erase(i);
				}
				_m_destroy(wd);
			}
			update(parent, false, false);
		}

		//destroy_handle
		//@brief:	Delete window handle, the handle type must be a root and a frame.
		void window_manager::destroy_handle(core_window_t* wd)
		{
			if(wd == nullptr) return;

			//Thread-Safe Required!
			std::lock_guard<decltype(mutex_)> lock(mutex_);
			if (impl_->wd_register.available(wd) == false)	return;

			if((wd->other.category == category::root_tag::value) || (wd->other.category != category::frame_tag::value))
			{
				impl_->misc_register.erase(wd->root);
				impl_->wd_register.remove(wd);
			}
		}

		void window_manager::icon(core_window_t* wd, const paint::image& img)
		{
			if(false == img.empty())
			{
				std::lock_guard<decltype(mutex_)> lock(mutex_);
				if (impl_->wd_register.available(wd))
				{
					if(wd->other.category == category::root_tag::value)
						native_interface::window_icon(wd->root, img);
				}
			}
		}

		//show
		//@brief: show or hide a window
		bool window_manager::show(core_window_t* wd, bool visible)
		{
			//Thread-Safe Required!
			std::lock_guard<decltype(mutex_)> lock(mutex_);
			if (impl_->wd_register.available(wd))
			{
				if(visible != wd->visible)
				{
					native_window_type nv = nullptr;
					switch(wd->other.category)
					{
					case category::root_tag::value:
						nv = wd->root; break;
					case category::frame_tag::value:
						nv = wd->other.attribute.frame->container; break;
					default:	//category::widget_tag, category::lite_widget_tag
						break;
					}

					if(visible && wd->effect.bground)
						wndlayout_type::make_bground(wd);

					//Don't set the visible attr of a window if it is a root.
					//The visible attr of a root will be set in the expose event.
					if(category::root_tag::value != wd->other.category)
						bedrock::instance().event_expose(wd, visible);

					if(nv)
						native_interface::show_window(nv, visible, wd->flags.take_active);
				}
				return true;
			}
			return false;
		}

		window_manager::core_window_t* window_manager::find_window(native_window_type root, int x, int y)
		{
			if((false == attr_.capture.ignore_children) || (nullptr == attr_.capture.window) || (attr_.capture.window->root != root))
			{
				//Thread-Safe Required!
				std::lock_guard<decltype(mutex_)> lock(mutex_);
				auto rrt = root_runtime(root);
				if(rrt && _m_effective(rrt->window, x, y))
					return _m_find(rrt->window, x, y);
			}
			return attr_.capture.window;
		}

		//move the wnd and its all children window, x and y is a relatively coordinate for wnd's parent window
		bool window_manager::move(core_window_t* wd, int x, int y, bool passive)
		{
			if(wd)
			{
				//Thread-Safe Required!
				std::lock_guard<decltype(mutex_)> lock(mutex_);
				if (impl_->wd_register.available(wd))
				{
					if(wd->other.category != category::root_tag::value)
					{
						//Move child widgets
						if(x != wd->pos_owner.x || y != wd->pos_owner.y)
						{
							int delta_x = x - wd->pos_owner.x;
							int delta_y = y - wd->pos_owner.y;

							wd->pos_owner.x = x;
							wd->pos_owner.y = y;
							_m_move_core(wd, delta_x, delta_y);

							if(wd->together.caret && wd->together.caret->visible())
								wd->together.caret->update();

							gui::eventinfo ei;
							ei.identifier = event_code::move;
							ei.window = reinterpret_cast<window>(wd);
							ei.move.x = x;
							ei.move.y = y;
							bedrock::raise_event(event_code::move, wd, ei, true);
							return true;
						}
					}
					else if(false == passive)
						native_interface::move_window(wd->root, x, y);
				}
			}
			return false;
		}

		bool window_manager::move(core_window_t* wd, int x, int y, unsigned width, unsigned height)
		{
			if(wd)
			{
				//Thread-Safe Required!
				std::lock_guard<decltype(mutex_)> lock(mutex_);
				if (impl_->wd_register.available(wd))
				{
					bool moved = false;
					const bool size_changed = (width != wd->dimension.width || height != wd->dimension.height);
					if(wd->other.category != category::root_tag::value)
					{
						//Move child widgets
						if(x != wd->pos_owner.x || y != wd->pos_owner.y)
						{
							int delta_x = x - wd->pos_owner.x;
							int delta_y = y - wd->pos_owner.y;
							wd->pos_owner.x = x;
							wd->pos_owner.y = y;
							_m_move_core(wd, delta_x, delta_y);
							moved = true;

							if(wd->together.caret && wd->together.caret->visible())
								wd->together.caret->update();

							eventinfo ei;
							ei.identifier = event_code::move;
							ei.window = reinterpret_cast<window>(wd);
							ei.move.x = x;
							ei.move.y = y;
							bedrock::raise_event(event_code::move, wd, ei, true);
						}

						if(size_changed)
							size(wd, width, height, true, false);
					}
					else
					{
						if(size_changed)
						{
							wd->dimension.width = width;
							wd->dimension.height = height;
							wd->drawer.graphics.make(width, height);
							wd->root_graph->make(width, height);
							native_interface::move_window(wd->root, x, y, width, height);

							eventinfo ei;
							ei.identifier = event_code::size;
							ei.window = reinterpret_cast<window>(wd);
							ei.size.width = width;
							ei.size.height = height;
							bedrock::raise_event(event_code::size, wd, ei, true);
						}
						else
							native_interface::move_window(wd->root, x, y);
					}

					return (moved || size_changed);
				}
			}
			return false;
		}

		//size
		//@brief: Size a window
		//@param: passive, if it is true, the function would not change the size if wd is a root_widget.
		//			e.g, when the size of window is changed by OS/user, the function should not resize the
		//			window again, otherwise, it causes an infinite loop, because when a root_widget is resized,
		//			window_manager will call the function.
		bool window_manager::size(core_window_t* wd, unsigned width, unsigned height, bool passive, bool ask_update)
		{
			if(nullptr == wd)
				return false;
			
			//Thread-Safe Required!
			std::lock_guard<decltype(mutex_)> lock(mutex_);
			if (impl_->wd_register.available(wd))
			{
				if(wd->dimension.width != width || wd->dimension.height != height)
				{
					eventinfo ei;
					ei.identifier = event_code::sizing;
					ei.window = reinterpret_cast<window>(wd);
					ei.sizing.width = width;
					ei.sizing.height = height;
					ei.sizing.border = window_border::none;
					bedrock::raise_event(event_code::sizing, wd, ei, false);

					width = ei.sizing.width;
					height = ei.sizing.height;
				}

				if(wd->dimension.width != width || wd->dimension.height != height)
				{
					if(wd->max_track_size.width && wd->max_track_size.height)
					{
						if(width > wd->max_track_size.width)
							width = wd->max_track_size.width;
						if(height > wd->max_track_size.height)
							height = wd->max_track_size.height;
					}
					if(wd->min_track_size.width && wd->min_track_size.height)
					{
						if(width < wd->min_track_size.width)
							width = wd->min_track_size.width;
						if(height < wd->min_track_size.height)
							height = wd->min_track_size.height;
					}

					if(wd->dimension.width == width && wd->dimension.height == height)
						return false;

					wd->dimension.width = width;
					wd->dimension.height = height;

					if(category::lite_widget_tag::value != wd->other.category)
					{
						bool graph_state = wd->drawer.graphics.empty();
						wd->drawer.graphics.make(width, height);

						//It shall make a typeface_changed() call when the graphics state is changing.
						//Because when a widget is created with zero-size, it may get some wrong result in typeface_changed() call
						//due to the invaliable graphics object.
						if(graph_state != wd->drawer.graphics.empty())
							wd->drawer.typeface_changed();

						if(category::root_tag::value == wd->other.category)
						{
							wd->root_graph->make(width, height);
							if(false == passive)
								native_interface::window_size(wd->root, width + wd->extra_width, height + wd->extra_height);
						}
						else if(category::frame_tag::value == wd->other.category)
						{
							native_interface::window_size(wd->other.attribute.frame->container, width, height);
							for(auto natwd : wd->other.attribute.frame->attach)
								native_interface::window_size(natwd, width, height);
						}
						else
						{
							//update the bground buffer of glass window.
							if(wd->effect.bground && wd->parent)
							{
								wd->other.glass_buffer.make(width, height);
								wndlayout_type::make_bground(wd);
							}
						}
					}

					eventinfo ei;
					ei.identifier = event_code::size;
					ei.window = reinterpret_cast<window>(wd);
					ei.size.width = width;
					ei.size.height = height;

					bedrock::raise_event(event_code::size, wd, ei, ask_update);
					return true;
				}
			}
			return false;
		}

		window_manager::core_window_t* window_manager::root(native_window_type wd) const
		{
			static std::pair<native_window_type, core_window_t*> cache;
			if(cache.first == wd) return cache.second;

			//Thread-Safe Required!
			std::lock_guard<decltype(mutex_)> lock(mutex_);

			auto rrt = root_runtime(wd);
			if(rrt)
			{
				cache.first = wd;
				cache.second = rrt->window;
				return cache.second;
			}
			return nullptr;
		}

		//Copy the root buffer that wnd specified into DeviceContext
		void window_manager::map(core_window_t* wd)
		{
			if(wd)
			{
				//Thread-Safe Required!
				std::lock_guard<decltype(mutex_)> lock(mutex_);
				if (impl_->wd_register.available(wd))
				{
					//Copy the root buffer that wd specified into DeviceContext
#if defined(NANA_LINUX)
					wd->drawer.map(reinterpret_cast<window>(wd));
#elif defined(NANA_WINDOWS)
					if(nana::system::this_thread_id() == wd->thread_id)
						wd->drawer.map(reinterpret_cast<window>(wd));
					else
						bedrock::instance().map_thread_root_buffer(wd);
#endif
				}
			}
		}

		bool window_manager::belong_to_lazy(core_window_t * wd) const
		{
			for(; wd; wd = wd->parent)
			{
				if(wd->other.upd_state == core_window_t::update_state::refresh)
					return true;
			}
			return false;
		}

		//update
		//@brief:	update is used for displaying the screen-off buffer.
		//			Because of a good efficiency, if it is called in an event procedure and the event procedure window is the
		//			same as update's, update would not map the screen-off buffer and just set the window for lazy refresh
		bool window_manager::update(core_window_t* wd, bool redraw, bool force)
		{
			if(wd == nullptr) return false;

			//Thread-Safe Required!
			std::lock_guard<decltype(mutex_)> lock(mutex_);
			if (impl_->wd_register.available(wd) == false) return false;

			if(wd->visible)
			{
				for(core_window_t* pnt = wd->parent ; pnt; pnt = pnt->parent)
				{
					if(pnt->visible == false)
						return true;
				}

				if(force || (false == belong_to_lazy(wd)))
				{
					wndlayout_type::paint(wd, redraw, false);
					this->map(wd);
				}
				else
				{
					if(redraw)
						wndlayout_type::paint(wd, true, false);
					if(wd->other.upd_state == core_window_t::update_state::lazy)
						wd->other.upd_state = core_window_t::update_state::refresh;
				}
			}
			return true;
		}

		void window_manager::refresh_tree(core_window_t* wd)
		{
			if(wd == nullptr)	return;
			//Thread-Safe Required!
			std::lock_guard<decltype(mutex_)> lock(mutex_);

			if (impl_->wd_register.available(wd) == false)
				return;

			//It's not worthy to redraw if visible is false
			if(wd->visible)
			{
				core_window_t* parent = wd->parent;
				while(parent)
				{
					if(parent->visible == false)	break;
					parent = parent->parent;
				}

				if(nullptr == parent)
					wndlayout_type::paint(wd, true, true);
			}
		}

		//do_lazy_refresh
		//@brief: defined a behavior of flush the screen
		//@return: it returns true if the wnd is available
		bool window_manager::do_lazy_refresh(core_window_t* wd, bool force_copy_to_screen)
		{
			if(wd == nullptr)	return false;
			//Thread-Safe Required!
			std::lock_guard<decltype(mutex_)> lock(mutex_);

			//It's not worthy to redraw if visible is false
			if (impl_->wd_register.available(wd) == false)
				return false;

			if(wd->visible)
			{
				core_window_t* parent = wd->parent;
				while(parent)
				{
					if(parent->visible == false)	break;
					parent = parent->parent;
				}

				if(parent)	//only refreshing if it has an invisible parent
				{
					wndlayout_type::paint(wd, true, false);
				}
				else
				{
					if((wd->other.upd_state == core_window_t::update_state::refresh) || force_copy_to_screen)
					{
						wndlayout_type::paint(wd, false, false);
						this->map(wd);
					}
				}
			}
			wd->other.upd_state = core_window_t::update_state::none;
			return true;
		}

		//get_graphics
		//@brief: Get a copy of the graphics object of a window.
		//	the copy of the graphics object has a same buf handle with the graphics object's, they are count-refered
		//	here returns a reference that because the framework does not guarantee the wnd's
		//	graphics object available after a get_graphics call.
		bool window_manager::get_graphics(core_window_t* wd, nana::paint::graphics& result)
		{
			if(wd)
			{
				//Thread-Safe Required!
				std::lock_guard<decltype(mutex_)> lock(mutex_);
				if (impl_->wd_register.available(wd))
				{
					result.make(wd->drawer.graphics.width(), wd->drawer.graphics.height());
					result.bitblt(0, 0, wd->drawer.graphics);
					wndlayout_type::paste_children_to_graphics(wd, result);
					return true;
				}
			}
			return false;
		}

		bool window_manager::get_visual_rectangle(core_window_t* wd, nana::rectangle& r)
		{
			if(wd)
			{
				//Thread-Safe Required!
				std::lock_guard<decltype(mutex_)> lock(mutex_);
				if (impl_->wd_register.available(wd))
					return wndlayout_type::read_visual_rectangle(wd, r);
			}
			return false;
		}

		bool window_manager::tray_make_event(native_window_type wd, event_code code, const event_fn_t& f)
		{
			if(native_interface::is_window(wd))
			{
				//Thread-Safe Required!
				std::lock_guard<decltype(mutex_)> lock(mutex_);
				return impl_->tray_event.make(wd, code, f);
			}
			return false;
		}

		void window_manager::tray_umake_event(native_window_type wd)
		{
			//Thread-Safe Required!
			std::lock_guard<decltype(mutex_)> lock(mutex_);
			impl_->tray_event.umake(wd);
		}

		void window_manager::tray_fire(native_window_type wd, event_code identifier, const eventinfo& ei)
		{
			//Thread-Safe Required!
			std::lock_guard<decltype(mutex_)> lock(mutex_);
			impl_->tray_event.fire(wd, identifier, ei);
		}

		//set_focus
		//@brief: set a keyboard focus to a window. this may fire a focus event.
		window_manager::core_window_t* window_manager::set_focus(core_window_t* wd)
		{
			if(nullptr == wd) return nullptr;

			//Thread-Safe Required!
			std::lock_guard<decltype(mutex_)> lock(mutex_);

			core_window_t * prev_focus = nullptr;
			if (impl_->wd_register.available(wd))
			{
				core_window_t* root_wd = wd->root_widget;
				prev_focus = root_wd->other.attribute.root->focus;

				eventinfo ei;
				if(wd != prev_focus)
				{
					//kill the previous window focus
					ei.focus.getting = false;
					root_wd->other.attribute.root->focus = wd;

					if (impl_->wd_register.available(prev_focus))
					{
						if(prev_focus->together.caret)
							prev_focus->together.caret->set_active(false);

						ei.focus.receiver = wd->root;
						bedrock::raise_event(event_code::focus, prev_focus, ei, true);
					}
				}
				else if(wd->root == native_interface::get_focus_window())
					wd = nullptr; //no new focus_window

				if(wd)
				{
					if(wd->together.caret)
						wd->together.caret->set_active(true);

					ei.focus.getting = true;
					ei.focus.receiver = wd->root;

					bedrock::raise_event(event_code::focus, wd, ei, true);
					native_interface::set_focus(root_wd->root);

					bedrock::instance().set_menubar_taken(wd);
				}
			}
			return prev_focus;
		}

		window_manager::core_window_t* window_manager::capture_redirect(core_window_t* wd)
		{
			if(attr_.capture.window && (attr_.capture.ignore_children == false) && (attr_.capture.window != wd))
			{
				//Tests if the wd is a child of captured window,
				//and returns the wd if it is.
				for(core_window_t * child = wd; child; child = child->parent)
				{
					if(child->parent == attr_.capture.window)
						return wd;
				}
			}
			return attr_.capture.window;
		}

		void window_manager::capture_ignore_children(bool ignore)
		{
			attr_.capture.ignore_children = ignore;
		}

		bool window_manager::capture_window_entered(int root_x, int root_y, bool& prev)
		{
			if(attr_.capture.window)
			{
				bool inside = _m_effective(attr_.capture.window, root_x, root_y);
				if(inside != attr_.capture.inside)
				{
					prev = attr_.capture.inside;
					attr_.capture.inside = inside;
					return true;
				}
			}
			return false;
		}

		window_manager::core_window_t * window_manager::capture_window() const
		{
			return attr_.capture.window;
		}

		//capture_window
		//@brief:	set a window that always captures the mouse event if it is not in the range of window
		//@return:	this function dose return the previous captured window. If the wnd set captured twice,
		//			the return value is NULL
		window_manager::core_window_t* window_manager::capture_window(core_window_t* wd, bool value)
		{
			nana::point pos = native_interface::cursor_position();
			auto & attr_cap = attr_.capture.history;

			if(value)
			{
				if(wd != attr_.capture.window)
				{
					//Thread-Safe Required!
					std::lock_guard<decltype(mutex_)> lock(mutex_);

					if (impl_->wd_register.available(wd))
					{
						wd->flags.capture = true;
						native_interface::capture_window(wd->root, value);
						auto prev = attr_.capture.window;
						if(prev && prev != wd)
							attr_cap.emplace_back(prev, attr_.capture.ignore_children);

						attr_.capture.window = wd;
						attr_.capture.ignore_children = true;
						native_interface::calc_window_point(wd->root, pos);
						attr_.capture.inside = _m_effective(wd, pos.x, pos.y);
						return prev;
					}
				}
				return attr_.capture.window;
			}
			else if(wd == attr_.capture.window)
			{
				attr_.capture.window = nullptr;
				if(attr_cap.size())
				{
					std::pair<core_window_t*, bool> last = attr_cap.back();
					attr_cap.pop_back();

					if (impl_->wd_register.available(last.first))
					{
						attr_.capture.window = last.first;
						attr_.capture.ignore_children = last.second;
						native_interface::capture_window(last.first->root, true);
						native_interface::calc_window_point(last.first->root, pos);
						attr_.capture.inside = _m_effective(last.first, pos.x, pos.y);
					}
				}

				if(wd && (nullptr == attr_.capture.window))
					native_interface::capture_window(wd->root, false);
			}
			else
			{
				auto i = std::find_if(attr_cap.begin(), attr_cap.end(),
					[wd](const std::pair<core_window_t*, bool> & x){ return (x.first == wd);});

				if(i != attr_cap.end())
					attr_cap.erase(i);
				return attr_.capture.window;
			}
			return wd;
		}

		//tabstop
		//@brief: when users press a TAB, the focus should move to the next widget.
		//	this method insert a window which catchs an user TAB into a TAB window container
		//	the TAB window container is held by a wd's root widget. Not every widget has a TAB window container,
		//	the container is created while a first Tab Window is setting
		void window_manager::tabstop(core_window_t* wd)
		{
			if(wd == 0) return;

			//Thread-Safe Required!
			std::lock_guard<decltype(mutex_)> lock(mutex_);
			if (impl_->wd_register.available(wd) == false)	return;

			if(detail::tab_type::none == wd->flags.tab)
			{
				wd->root_widget->other.attribute.root->tabstop.push_back(wd);
				wd->flags.tab |= detail::tab_type::tabstop;
			}
		}

		window_manager::core_window_t* window_manager::tabstop_prev(core_window_t* wd) const
		{
			if(wd)
			{
				//Thread-Safe Required!
				std::lock_guard<decltype(mutex_)> lock(mutex_);
				if (impl_->wd_register.available(wd))
				{
					auto & tabs = wd->root_widget->other.attribute.root->tabstop;
					if(tabs.size() > 1)
					{
						auto i = std::find(tabs.cbegin(), tabs.cend(), wd);
						if(i != tabs.cend())
						{
							if(tabs.cbegin() == i)
								return tabs.back();
							return *(i - 1);
						}
					}
				}
			}
			return nullptr;
		}

		window_manager::core_window_t* window_manager::tabstop_next(core_window_t* wd) const
		{
			if(wd == nullptr) return nullptr;

			//Thread-Safe Required!
			std::lock_guard<decltype(mutex_)> lock(mutex_);
			if (impl_->wd_register.available(wd) == false)	return nullptr;

			auto root_attr = wd->root_widget->other.attribute.root;
			if(detail::tab_type::none == wd->flags.tab)
			{
				if(root_attr->tabstop.size())
					return (root_attr->tabstop[0]);
			}
			else if(detail::tab_type::tabstop & wd->flags.tab)
			{
				auto & tabs = root_attr->tabstop;
				if(tabs.size())
				{
					auto end = tabs.cend();
					auto i = std::find(tabs.cbegin(), end, wd);
					if(i != end)
					{
						++i;
						core_window_t* ts = (i!= end ? (*i) : tabs.front());
						return (ts != wd ? ts : 0);
					}
					else
						return tabs.front();
				}
			}
			return nullptr;
		}

		void window_manager::remove_trash_handle(unsigned tid)
		{
			impl_->wd_register.delete_trash(tid);
		}

		bool window_manager::enable_effects_bground(core_window_t* wd, bool enabled)
		{
			if(wd)
			{
				//Thread-Safe Required!
				std::lock_guard<decltype(mutex_)> lock(mutex_);
				if (impl_->wd_register.available(wd))
					return wndlayout_type::enable_effects_bground(wd, enabled);
			}
			return false;
		}

		bool window_manager::calc_window_point(core_window_t* wd, nana::point& pos)
		{
			if(wd)
			{
				//Thread-Safe Required!
				std::lock_guard<decltype(mutex_)> lock(mutex_);
				if (impl_->wd_register.available(wd))
				{
					if(native_interface::calc_window_point(wd->root, pos))
					{
						pos.x -= wd->pos_root.x;
						pos.y -= wd->pos_root.y;
						return true;
					}
				}
			}
			return false;
		}

		root_misc* window_manager::root_runtime(native_window_type native_wd) const
		{
			return impl_->misc_register.find(native_wd);
		}

		bool window_manager::register_shortkey(core_window_t* wd, unsigned long key)
		{
			if(wd)
			{
				//Thread-Safe Required!
				std::lock_guard<decltype(mutex_)> lock(mutex_);
				if (impl_->wd_register.available(wd))
				{
					auto object = root_runtime(wd->root);
					if(object)
						return object->shortkeys.make(reinterpret_cast<window>(wd), key);
				}
			}
			return false;
		}

		void window_manager::unregister_shortkey(core_window_t* wd)
		{
			if(wd == nullptr) return;

			//Thread-Safe Required!
			std::lock_guard<decltype(mutex_)> lock(mutex_);
			if (impl_->wd_register.available(wd) == false) return;

			auto object = root_runtime(wd->root);
			if(object) object->shortkeys.umake(reinterpret_cast<window>(wd));
		}

		window_manager::core_window_t* window_manager::find_shortkey(native_window_type native_window, unsigned long key)
		{
			if(native_window)
			{
				//Thread-Safe Required!
				std::lock_guard<decltype(mutex_)> lock(mutex_);
				auto object = root_runtime(native_window);
				if(object)
					return reinterpret_cast<core_window_t*>(object->shortkeys.find(key));
			}
			return nullptr;
		}

		void window_manager::_m_attach_signal(core_window_t* wd, signal_invoker_interface* si)
		{
			impl_->signal.make(wd, si);
		}

		void window_manager::_m_destroy(core_window_t* wd)
		{
			if(wd->flags.destroying) return;

			bedrock & bedrock_instance = bedrock::instance();
			bedrock_instance.thread_context_destroy(wd);

			wd->flags.destroying = true;

			if(wd->together.caret)
			{
				//The deletion of caret wants to know whether the window is destroyed under SOME platform. Such as X11
				delete wd->together.caret;
				wd->together.caret = nullptr;
			}
			//Delete the children widgets.
			std::for_each(wd->children.rbegin(), wd->children.rend(), [this](core_window_t * child){ this->_m_destroy(child);});
			wd->children.clear();

			eventinfo ei;
			ei.identifier = event_code::destroy;
			ei.window = reinterpret_cast<window>(wd);
			bedrock::raise_event(event_code::destroy, wd, ei, true);

			auto * root_attr = wd->root_widget->other.attribute.root;
			if(root_attr->focus == wd)
				root_attr->focus = nullptr;

			if(root_attr->menubar == wd)
				root_attr->menubar = nullptr;

			wndlayout_type::enable_effects_bground(wd, false);

			//test if wd is a TABSTOP window
			if(wd->flags.tab & detail::tab_type::tabstop)
			{
				auto & tabstop = root_attr->tabstop;
				auto i = std::find(tabstop.begin(), tabstop.end(), wd);
				if(i != tabstop.cend())
					tabstop.erase(i);
			}

			if(effects::edge_nimbus::none != wd->effect.edge_nimbus)
			{
				auto & cont = root_attr->effects_edge_nimbus;
				for(auto i = cont.begin(); i != cont.end(); ++i)
					if(i->window == wd)
					{
						cont.erase(i);
						break;
					}
			}

			bedrock_instance.evt_manager.umake(reinterpret_cast<window>(wd), false);
			bedrock_instance.evt_manager.umake(reinterpret_cast<window>(wd), true);
			wd->drawer.detached();
			impl_->signal.call_signal(wd, signals::code::destroy, signals_);
			detach_signal(wd);

			if(wd->parent && (wd->parent->children.size() > 1))
			{
				for(auto i = wd->parent->children.cbegin(), end = wd->parent->children.cend();i != end; ++i)
					if(((*i)->index) > (wd->index))
					{
						for(; i != end; ++i)
							--((*i)->index);
						break;
					}
			}

			if(wd->other.category == category::frame_tag::value)
			{
				//remove the frame handle from the WM frames manager.
				auto & frames = root_attr->frames;
				auto i = std::find(frames.begin(), frames.end(), wd);
				if(i != frames.end())
					frames.erase(i);

				//The frame widget does not have an owner, and close their element windows without activating owner.
				//close the frame container window, it's a native window.
				for(auto i : wd->other.attribute.frame->attach)
					native_interface::close_window(i);

				native_interface::close_window(wd->other.attribute.frame->container);
			}

			if(wd->other.category != category::root_tag::value)	//Not a root window
				impl_->wd_register.remove(wd);
		}

		void window_manager::_m_move_core(core_window_t* wd, int delta_x, int delta_y)
		{
			if(wd->other.category != category::root_tag::value)	//A root widget always starts at (0, 0) and its childs are not to be changed
			{
				wd->pos_root.x += delta_x;
				wd->pos_root.y += delta_y;

				if(wd->other.category == category::frame_tag::value)
					native_interface::move_window(wd->other.attribute.frame->container, wd->pos_root.x, wd->pos_root.y);

				std::for_each(wd->children.cbegin(), wd->children.cend(), [this, delta_x, delta_y](core_window_t * child){ this->_m_move_core(child, delta_x, delta_y);});
			}
		}

		//_m_find
		//@brief: find a window on root window through a given root coordinate.
		//		the given root coordinate must be in the rectangle of wnd.
		window_manager::core_window_t* window_manager::_m_find(core_window_t* wd, int x, int y)
		{
			if(wd->visible == 0) return nullptr;

			for(auto i = wd->children.rbegin(); i != wd->children.rend(); ++i)
			{
				core_window_t* child = *i;
				if((child->other.category != category::root_tag::value) && _m_effective(child, x, y))
				{
					child = _m_find(child, x, y);
					if(child)
						return child;
				}
			}
			return wd;
		}

		//_m_effective, test if the window is a handle of window that specified by (root_x, root_y)
		bool window_manager::_m_effective(core_window_t* wd, int root_x, int root_y)
		{
			if(wd == nullptr || false == wd->visible)	return false;
			return is_hit_the_rectangle(nana::rectangle(wd->pos_root, wd->dimension), root_x, root_y);
		}
	//end class window_manager
}//end namespace detail
}//end namespace gui
}//end namespace nana
