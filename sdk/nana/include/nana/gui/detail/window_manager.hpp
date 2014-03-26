/*
 *	Window Manager Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0.
 *	(See accompanying file LICENSE_1_0.txt or copy at
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/detail/window_manager.hpp
 *
 *	<Knowledge: 1, 2007-8-17, "Difference between destroy and destroy_handle">
 *		destroy method destroys a window handle and the handles of its children, but it doesn't delete the handle which type is a root window or a frame
 *		destroy_handle method just destroys the handle which type is a root window or a frame
 *
 */

#ifndef NANA_GUI_DETAIL_WINDOW_MANAGER_HPP
#define NANA_GUI_DETAIL_WINDOW_MANAGER_HPP
#include <map>
#include <vector>
#include <algorithm>
#include <nana/paint/image.hpp>
#include "basic_window.hpp"
#include "handle_manager.hpp"
#include "window_layout.hpp"
#include "eventinfo.hpp"

namespace nana
{
namespace gui
{
namespace detail
{
	template<typename Key, typename Value>
	class cached_map
	{
	public:
		typedef Key key_type;
		typedef Value value_type;
		typedef std::map<key_type, value_type> std_map_type;

		cached_map()
			: key_(key_type()), value_addr_(0)
		{}

		value_type* insert(key_type key, const value_type& value)
		{
			key_ = key;
			std::pair<typename std_map_type::iterator, bool> ret = map_holder_.insert(std::pair<key_type, value_type>(key, value));
			value_addr_ = &(ret.first->second);
			return value_addr_;
		}

		value_type * find(key_type key) const
		{
			if(key_ == key) return value_addr_;

			key_ = key;
			typename std_map_type::const_iterator i = map_holder_.find(key);
			if(i != map_holder_.end())
				value_addr_ = const_cast<value_type*>(&(i->second));
			else
				value_addr_ = 0;

			return value_addr_;
		}

		void erase(key_type key)
		{
			map_holder_.erase(key);
			key_ = key;
			value_addr_ = 0;
		}
	private:
		mutable key_type key_;
		mutable value_type* value_addr_;
		std_map_type map_holder_;
	};

	struct signals
	{
		enum{caption, read_caption, destroy, size, count};

		union
		{
			const nana::char_t* caption;
			nana::string * str;
			struct
			{
				unsigned width;
				unsigned height;
			}size;
		}info;
	};

	//signal_manager
	class signal_manager
	{
	public:
        typedef void* identifier;

		~signal_manager();

		template<typename Class>
		bool make(identifier id, Class& object, void (Class::* f)(int, const signals&))
		{
			if(id)
			{
				inner_invoker * & invk = manager_[id];
				end_ = manager_.end();
				if(invk == 0)
				{
					invk = new (std::nothrow) extend_memfun<Class>(object, f);
					if(invk)	return true;
				}
			}
			return false;
		}

		template<typename Function>
		bool make(identifier id, Function f)
		{
			if(id)
			{
				inner_invoker * & invk = manager_[id];
				end_ = manager_.end();
				if(invk == 0)
				{
					invk = new (std::nothrow) extend<Function>(f);
					if(invk)	return true;
				}
			}
			return false;
		}

		void umake(identifier);

		void fireaway(identifier, int message, const signals&);
	private:
		struct inner_invoker
		{
			virtual ~inner_invoker();
			virtual void fireaway(int, const signals&) = 0;
		};

		template<typename Class>
		struct extend_memfun: inner_invoker
		{
			extend_memfun(Class& object, void(Class::*f)(int, const signals&))
				:obj_(object), f_(f)
			{}

			void fireaway(int message, const signals& info)
			{
				(obj_.*f_)(message, info);
			}
		private:
			Class& obj_;
			void(Class::*f_)(int, const signals&);
		};

		template<typename Function>
		struct extend: inner_invoker
		{
			extend(Function f):fun_(f){}

			void fireaway(int m, const signals& info)
			{
				fun_(m, info);
			}
		private:
			Function fun_;
		};
	private:
		std::map<identifier, inner_invoker*>	manager_;
		std::map<identifier, inner_invoker*>::iterator end_;
	};

	class shortkey_container
	{
		struct item_type
		{
			window handle;
			std::vector<unsigned long> keys;
		};
	public:
		void clear();
		bool make(window, unsigned long key);
		void umake(window);
		window find(unsigned long key) const;
	private:
		std::vector<item_type> keybase_;
	};

	class tray_event_manager
	{
		typedef std::vector<nana::functor<void(const eventinfo&)> > fvec_t;
		typedef std::map<event_code::t, fvec_t> event_maptable_type;
		typedef std::map<native_window_type, event_maptable_type> maptable_type;
	public:
		void fire(native_window_type, event_code::t, const eventinfo&);
		bool make(native_window_type, event_code::t, const nana::functor<void(const eventinfo&)>&);
		void umake(native_window_type);
	private:
		maptable_type maptable_;
	};

	template<typename NativeHWND, typename Bedrock>
	struct root_window_runtime
	{
		typedef basic_window core_window_t;

		core_window_t*			window;
		nana::paint::graphics	root_graph_object;
		shortkey_container		shortkeys;

		struct condition_tag
		{
			core_window_t*	mouse_window;		//the latest window that mouse pressed
			core_window_t*	mousemove_window;	//the latest window that mouse moved
			bool		tabstop_focus_changed;	//KeyDown may set it true, if it is true KeyChar will ignore the message
			condition_tag()
				:	mouse_window(0),
					mousemove_window(0),
					tabstop_focus_changed(false)
			{}

		}condition;

		root_window_runtime(core_window_t* wd, unsigned width, unsigned height)
			:	window(wd),
				root_graph_object(width, height)
		{}
	};

	class reversible_mutex
		: public nana::threads::recursive_mutex
	{
		struct thr_refcnt
		{
			unsigned tid;
			std::size_t refcnt;
		};
	public:
		reversible_mutex();

		void lock();
		bool try_lock();
		void unlock();

		void revert();
		void forward();
	private:
		mutable thr_refcnt thr_;
		mutable std::vector<thr_refcnt> stack_;
	};

	//class window_manager
	template<typename NativeHWND, typename Bedrock, typename NativeAPI>
	class window_manager
	{
	public:
		typedef NativeHWND	native_window;
		typedef Bedrock		bedrock_type;
		typedef basic_window core_window_t;
		typedef std::vector<core_window_t*> cont_type;
		typedef NativeAPI	interface_type;
		typedef window_layout<core_window_t>	wndlayout_type;
		typedef cached_map<nana::gui::native_window_type, root_window_runtime<native_window, bedrock_type> > root_table_type;

		static bool is_queue(core_window_t* wd)
		{
			return (wd && wd->other.category == static_cast<category::flags::t>(category::root_tag::value));
		}

		std::size_t number_of_core_window() const
		{
			return this->handle_manager_.size();
		}

		reversible_mutex& internal_lock()
		{
			return wnd_mgr_lock_;
		}

		void all_handles(std::vector<core_window_t*> &v) const
		{
			handle_manager_.all(v);
		}

		template<typename Class>
		bool attach_signal(core_window_t* wd, Class& object, void (Class::*answer)(int, const detail::signals&))
		{
			return signal_manager_.make(wd, object, answer);
		}

		template<typename Function>
		bool attach_signal(core_window_t* wd, Function function)
		{
			return signal_manager_.template make<Function>(wd, function);
		}

		void detach_signal(core_window_t* wd)
		{
			signal_manager_.umake(wd);
		}

		nana::string signal_fire_caption(core_window_t* wd)
		{
			nana::string str;
			detail::signals sig;
			sig.info.str = & str;
			signal_manager_.fireaway(wd, detail::signals::read_caption, sig);
			return str;
		}

		void signal_fire_caption(core_window_t* wd, const nana::char_t* str)
		{
			detail::signals sig;
			sig.info.caption = str;
			signal_manager_.fireaway(wd, detail::signals::caption, sig);
		}

		void event_filter(core_window_t* wd, bool is_make, event_code::t code)
		{
			switch(code)
			{
			case event_code::mouse_drop:
				wd->flags.dropable = (is_make ? true : (bedrock_type::instance().evt_manager.the_number_of_handles(reinterpret_cast<nana::gui::window>(wd), code, false) != 0));
				break;
			default:
				break;
			}
		}

		void default_icon(const nana::paint::image& img)
		{
			default_icon_ = img;
		}

		bool available(core_window_t* wd)
		{
			return handle_manager_.available(wd);
		}

		bool available(core_window_t * a, core_window_t* b)
		{
			return (handle_manager_.available(a) && handle_manager_.available(b));
		}

		bool available(nana::gui::native_window_type wd)
		{
			if(wd)
			{
				threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);
				return (root_table_.find(wd) != 0);
			}
			return false;
		}

		core_window_t* create_root(core_window_t* owner, bool nested, rectangle r, const appearance& app)
		{
			nana::gui::native_window_type ownerWnd = 0;

			if(owner)
			{
				//Thread-Safe Required!
				threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);

				if(handle_manager_.available(owner))
				{
					ownerWnd = (owner->other.category == static_cast<category::flags::t>(category::frame_tag::value) ?
										owner->other.attribute.frame->container : owner->root_widget->root);
					r.x += owner->pos_root.x;
					r.y += owner->pos_root.y;
				}
				else
					owner = 0;
			}

			typename interface_type::window_result result = interface_type::create_window(ownerWnd, nested, r, app);
			if(result.handle)
			{
				core_window_t* wd = new core_window_t(owner, (nana::gui::category::root_tag**)0);
				wd->flags.take_active = !app.no_activate;
				wd->title = interface_type::window_caption(result.handle);

				//Thread-Safe Required!
				threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);

				//create Root graphics Buffer and manage it
				typename root_table_type::value_type rt_window_data(wd, result.width, result.height);
				typename root_table_type::value_type* value = root_table_.insert(result.handle, rt_window_data);


				wd->bind_native_window(result.handle, result.width, result.height, result.extra_width, result.extra_height, value->root_graph_object);

				handle_manager_.insert(wd, wd->thread_id);

				if(owner && (owner->other.category == static_cast<category::flags::t>(category::frame_tag::value)))
					insert_frame(owner, wd);

				bedrock_type::inc_window(wd->thread_id);
				this->icon(wd, default_icon_);
				return wd;
			}
			return 0;
		}

		core_window_t* create_frame(core_window_t* parent, const rectangle& r)
		{
			if(parent == 0) return 0;
			//Thread-Safe Required!
			threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);

			if(handle_manager_.available(parent) == false)	return 0;

			core_window_t * wd = new core_window_t(parent, r, (nana::gui::category::frame_tag**)0);
			wd->frame_window(interface_type::create_child_window(parent->root, rectangle(wd->pos_root.x, wd->pos_root.y, r.width, r.height)));
			handle_manager_.insert(wd, wd->thread_id);

			//Insert the frame_widget into its root frames container.
			wd->root_widget->other.attribute.root->frames.push_back(wd);
			return (wd);
		}

		bool insert_frame(core_window_t* frame, native_window wd)
		{
			if(frame)
			{
				//Thread-Safe Required!
				threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);
				if(frame->other.category == static_cast<category::flags::t>(category::frame_tag::value))
					frame->other.attribute.frame->attach.push_back(wd);
				return true;
			}
			return false;
		}

		bool insert_frame(core_window_t* frame, core_window_t* wd)
		{
			if(frame)
			{
				//Thread-Safe Required!
				threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);
				if(frame->other.category == static_cast<category::flags::t>(category::frame_tag::value))
				{
					if(handle_manager_.available(wd) && (wd->other.category == static_cast<category::flags::t>(category::root_tag::value)) && wd->root != frame->root)
					{
						frame->other.attribute.frame->attach.push_back(wd->root);
						return true;
					}
				}
			}
			return false;
		}

		core_window_t* create_widget(core_window_t* parent, const rectangle& r, bool is_lite)
		{
			if(parent == 0)	return 0;
			//Thread-Safe Required!
			threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);

			if(handle_manager_.available(parent) == false)	return 0;
			core_window_t * wd;
			if(is_lite)
				wd = new core_window_t(parent, r, (category::lite_widget_tag**)0);
			else
				wd = new core_window_t(parent, r, (category::widget_tag**)0);
			handle_manager_.insert(wd, wd->thread_id);
			return wd;
		}

		void close(core_window_t *wd)
		{
			if(wd == 0) return;
			//Thread-Safe Required!
			threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);
			if(handle_manager_.available(wd) == false)	return;

			if(wd->other.category == static_cast<category::flags::t>(category::root_tag::value))
			{
				eventinfo ei;
				ei.unload.cancel = false;
				bedrock_type::raise_event(event_code::unload, wd, ei, true);
				if(false == ei.unload.cancel)
				{
					//Before close the window, its owner window should be actived, otherwise other window will be
					//activated due to the owner window is not enabled.
					if(wd->flags.modal || (wd->owner == 0) || wd->owner->flags.take_active)
						native_interface::activate_owner(wd->root);

					//Close should detach the drawer and send destroy signal to widget object.
					//Otherwise, when a widget object is been deleting in other thread by delete operator, the object will be destroyed
					//before the window_manager destroyes the window, and then, window_manager detaches the
					//non-existing drawer_trigger which is destroyed by destruction of widget. Crash!
					wd->drawer.detached();
					signal_manager_.fireaway(wd, signals::destroy, signals_);
					detach_signal(wd);

					interface_type::close_window(wd->root);
				}
			}
			else
				destroy(wd);
		}

		//destroy
		//@brief:	Delete the window handle
		void destroy(core_window_t* wd)
		{
			if(wd == 0) return;
			core_window_t* parent = 0;
			{
				//Thread-Safe Required!
				threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);
				if(handle_manager_.available(wd) == false)	return;

				parent = wd->parent;

				if(wd == attr_.capture.window)
					capture_window(wd, false);

				if(wd->other.category == static_cast<category::flags::t>(category::root_tag::value))
				{
					typename root_table_type::value_type* object = root_runtime(wd->root);
					object->shortkeys.clear();
					wd->other.attribute.root->focus = 0;
				}
				else
					unregister_shortkey(wd);

				if(parent)
				{
					cont_type & cont = parent->children;
					typename cont_type::iterator i = std::find(cont.begin(), cont.end(), wd);
					if(i != cont.end())
						cont.erase(i);
				}

				inner_delete_helper dl(this);
				this->_m_destroy(wd, dl);
			}

			this->update(parent, false, false);
		}

		//destroy_handle
		//@brief:	Delete window handle, the handle type must be a root and a frame.
		void destroy_handle(core_window_t* wd)
		{
			if(wd == 0) return;

			//Thread-Safe Required!
			threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);
			if(handle_manager_.available(wd) == false)	return;

			if((wd->other.category == static_cast<category::flags::t>(category::root_tag::value)) || (wd->other.category != static_cast<category::flags::t>(category::frame_tag::value)))
			{
				root_table_.erase(wd->root);
				handle_manager_.remove(wd);
			}
		}

		void icon(core_window_t* wd, const paint::image& img)
		{
			if(false == img.empty())
			{
				threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);
				if(handle_manager_.available(wd))
				{
					if(wd->other.category == static_cast<category::flags::t>(category::root_tag::value))
						interface_type::window_icon(wd->root, img);
				}
			}
		}

		//show
		//@brief: show or hide a window
		bool show(core_window_t* wd, bool visible)
		{
			//Thread-Safe Required!
			threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);
			if(handle_manager_.available(wd))
			{
				if(visible != wd->visible)
				{
					native_window_type nv = 0;
					switch(wd->other.category)
					{
					case category::root_tag::value:
						nv = wd->root; break;
					case category::frame_tag::value:
						nv = wd->other.attribute.frame->container; break;
                    default:
                        break;
					}

					if(visible && wd->effect.bground)
						wndlayout_type::make_bground(wd);

					//Don't set the visible attr of a window if it is a root.
					//The visible attr of a root will be set in the expose event.
					if(static_cast<category::flags::t>(category::root_tag::value) != wd->other.category)
						bedrock_type::instance().event_expose(wd, visible);

					if(nv)
						interface_type::show_window(nv, visible, wd->flags.take_active);
				}
				return true;
			}
			return false;
		}

		core_window_t* find_window(nana::gui::native_window_type root, int x, int y)
		{
			if((false == attr_.capture.ignore_children) || (0 == attr_.capture.window) || (attr_.capture.window->root != root))
			{
				//Thread-Safe Required!
				threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);
				typename root_table_type::value_type* rrt = root_runtime(root);
				if(rrt && _m_effective(rrt->window, x, y))
					return _m_find(rrt->window, x, y);
			}
			return attr_.capture.window;
		}

		//move the wnd and its all children window, x and y is a relatively coordinate for wnd's parent window
		bool move(core_window_t* wd, int x, int y, bool passive)
		{
			if(wd)
			{
				//Thread-Safe Required!
				threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);
				if(handle_manager_.available(wd))
				{
					if(wd->other.category != static_cast<category::flags::t>(category::root_tag::value))
					{
						//Move child widgets
						inner_move_helper mv_helper(this, x - wd->pos_owner.x, y - wd->pos_owner.y);
						if(mv_helper.delta_x_ || mv_helper.delta_y_)
						{
							wd->pos_owner.x += mv_helper.delta_x_;
							wd->pos_owner.y += mv_helper.delta_y_;
							this->_m_move_core(wd, mv_helper);

							if(wd->together.caret && wd->together.caret->visible())
								wd->together.caret->update();

							gui::eventinfo ei;
							ei.identifier = event_code::move;
							ei.window = reinterpret_cast<window>(wd);
							ei.move.x = x;
							ei.move.y = y;
							bedrock_type::raise_event(event_code::move, wd, ei, true);
							return true;
						}
					}
					else if(false == passive)
						interface_type::move_window(wd->root, x, y);
				}
			}
			return false;
		}

		bool move(core_window_t* wd, int x, int y, unsigned width, unsigned height)
		{
			if(wd)
			{
				//Thread-Safe Required!
				threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);
				if(handle_manager_.available(wd))
				{
					bool moved = false;
					const bool size_changed = (width != wd->dimension.width || height != wd->dimension.height);
					if(wd->other.category != static_cast<category::flags::t>(category::root_tag::value))
					{
						//Move child widgets
						inner_move_helper mv_helper(this, x - wd->pos_owner.x, y - wd->pos_owner.y);
						if(mv_helper.delta_x_ || mv_helper.delta_y_)
						{
							wd->pos_owner.x += mv_helper.delta_x_;
							wd->pos_owner.y += mv_helper.delta_y_;
							this->_m_move_core(wd, mv_helper);
							moved = true;

							if(wd->together.caret && wd->together.caret->visible())
								wd->together.caret->update();

							eventinfo ei;
							ei.identifier = event_code::move;
							ei.window = reinterpret_cast<window>(wd);
							ei.move.x = x;
							ei.move.y = y;
							bedrock_type::raise_event(event_code::move, wd, ei, true);
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
							interface_type::move_window(wd->root, x, y, width, height);

							eventinfo ei;
							ei.identifier = event_code::size;
							ei.window = reinterpret_cast<window>(wd);
							ei.size.width = width;
							ei.size.height = height;
							bedrock_type::raise_event(event_code::size, wd, ei, true);
						}
						else
							interface_type::move_window(wd->root, x, y);
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
		//
		bool size(core_window_t* wd, unsigned width, unsigned height, bool passive, bool ask_update)
		{
			if(0 == wd)
				return false;

			//Thread-Safe Required!
			threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);
			if(handle_manager_.available(wd))
			{
				if(wd->dimension.width != width || wd->dimension.height != height)
				{
					eventinfo ei;
					ei.identifier = event_code::sizing;
					ei.window = reinterpret_cast<window>(wd);
					ei.sizing.width = width;
					ei.sizing.height = height;
					ei.sizing.border = window_border::none;
					bedrock_type::raise_event(event_code::sizing, wd, ei, false);

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

					if(static_cast<category::flags::t>(category::lite_widget_tag::value) != wd->other.category)
					{
						bool graph_state = wd->drawer.graphics.empty();
						
						wd->drawer.graphics.make(width, height);
					
						//It shall make a typeface_changed() call when the graphics state is changing.
						//Because when a widget is created with zero-size, it may get some wrong result in typeface_changed() call
						//due to the invaliable graphics object.
						if(graph_state != wd->drawer.graphics.empty())
							wd->drawer.typeface_changed();

						if(static_cast<category::flags::t>(category::root_tag::value) == wd->other.category)
						{
							wd->root_graph->make(width, height);
							if(false == passive)
								interface_type::window_size(wd->root, width + wd->extra_width, height + wd->extra_height);
						}
						else if(static_cast<category::flags::t>(category::frame_tag::value) == wd->other.category)
						{
							interface_type::window_size(wd->other.attribute.frame->container, width, height);
							std::vector<native_window_type>& cont = wd->other.attribute.frame->attach;
							for(std::vector<native_window_type>::iterator i = cont.begin(); i != cont.end(); ++i)
								interface_type::window_size(*i, width, height);
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

					bedrock_type::raise_event(event_code::size, wd, ei, ask_update);
					return true;
				}
			}

			return false;
		}

		core_window_t* root(nana::gui::native_window_type wd) const
		{
			static std::pair<nana::gui::native_window_type, core_window_t*> cache;
			if(cache.first == wd) return cache.second;

			//Thread-Safe Required!
			threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);

			typename root_table_type::value_type* rrt = root_runtime(wd);
			if(rrt)
			{
				cache.first = wd;
				cache.second = rrt->window;
				return cache.second;
			}
			return 0;
		}

		//Copy the root buffer that wnd specified into DeviceContext
		void map(core_window_t* wd)
		{
			if(wd)
			{
				//Thread-Safe Required!
				threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);
				if(handle_manager_.available(wd))
				{
					//Copy the root buffer that wd specified into DeviceContext
#if defined(NANA_LINUX)
					wd->drawer.map(reinterpret_cast<window>(wd));
#elif defined(NANA_WINDOWS)
					if(nana::system::this_thread_id() == wd->thread_id)
						wd->drawer.map(reinterpret_cast<window>(wd));
					else
						bedrock_type::instance().map_thread_root_buffer(wd);
#endif
				}
			}
		}

		bool belong_to_lazy(core_window_t * wd) const
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
		bool update(core_window_t* wd, bool redraw, bool force)
		{
			if(wd == 0) return false;

			//Thread-Safe Required!
			threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);

			if(handle_manager_.available(wd) == false) return false;

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


		void refresh_tree(core_window_t* wd)
		{
			if(wd == 0)	return;
			//Thread-Safe Required!
			threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);

			//It's not worthy to redraw if visible is false
			if(handle_manager_.available(wd) == false)
				return;

			if(wd->visible)
			{
				core_window_t* parent = wd->parent;
				while(parent)
				{
					if(parent->visible == false)	break;
					parent = parent->parent;
				}

				if(0 == parent)	//only refreshing if it has an invisible parent
					wndlayout_type::paint(wd, true, true);
			}
		}

		//do_lazy_refresh
		//@brief: defined a behavior of flush the screen
		//@return: it returns true if the wnd is available
		bool do_lazy_refresh(core_window_t* wd, bool force_copy_to_screen)
		{
			if(wd == 0)	return false;
			//Thread-Safe Required!
			threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);

			//It's not worthy to redraw if visible is false
			if(handle_manager_.available(wd) == false)
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
		bool get_graphics(core_window_t* wd, nana::paint::graphics& result)
		{
			if(wd)
			{
				//Thread-Safe Required!
				threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);
				if(handle_manager_.available(wd))
				{
					result.make(wd->drawer.graphics.width(), wd->drawer.graphics.height());
					result.bitblt(0, 0, wd->drawer.graphics);
					wndlayout_type::paste_children_to_graphics(wd, result);
					return true;
				}
			}
			return false;
		}

		bool get_visual_rectangle(core_window_t* wnd, nana::rectangle& rect)
		{
			if(wnd)
			{
				//Thread-Safe Required!
				threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);
				if(handle_manager_.available(wnd))
					return wndlayout_type::read_visual_rectangle(wnd, rect);
			}
			return false;
		}

		bool tray_make_event(native_window_type wd, event_code::t code, const nana::functor<void(const eventinfo&)>& f)
		{
			if(interface_type::is_window(wd))
			{
				//Thread-Safe Required!
				threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);
				return tray_event_manager_.make(wd, code, f);
			}
			return false;
		}

		void tray_umake_event(native_window_type wd)
		{
			//Thread-Safe Required!
			threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);
			tray_event_manager_.umake(wd);
		}

		void tray_fire(native_window_type wd, event_code::t code, const eventinfo& ei)
		{
			//Thread-Safe Required!
			threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);
			tray_event_manager_.fire(wd, code, ei);
		}

		//set_focus
		//@brief: set a keyboard focus to a window. this may fire a focus event.
		core_window_t* set_focus(core_window_t* wd)
		{
			if(wd == 0) return 0;

			//Thread-Safe Required!
			threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);

			core_window_t * prev_focus = 0;

			if(handle_manager_.available(wd))
			{
				core_window_t* root_wd = wd->root_widget;
				prev_focus = root_wd->other.attribute.root->focus;

				eventinfo ei;
				if(wd != prev_focus)
				{
					//kill the previous window focus
					ei.focus.getting = false;
					root_wd->other.attribute.root->focus = wd;

					if(handle_manager_.available(prev_focus))
					{
						if(prev_focus->together.caret)
							prev_focus->together.caret->set_active(false);

						ei.focus.receiver = wd->root;
						bedrock_type::raise_event(event_code::focus, prev_focus, ei, true);
					}
				}
				else if(wd->root == interface_type::get_focus_window())
					wd = 0; //no new focus_window

				if(wd)
				{
					if(wd->together.caret)
							wd->together.caret->set_active(true);

					ei.focus.getting = true;
					ei.focus.receiver = wd->root;

					bedrock_type::raise_event(event_code::focus, wd, ei, true);
					interface_type::set_focus(root_wd->root);

					bedrock_type::instance().set_menubar_taken(wd);
				}
			}
			return prev_focus;
		}

		core_window_t* capture_redirect(core_window_t* wd)
		{
			if(attr_.capture.window && (attr_.capture.ignore_children == false) && (attr_.capture.window != wd))
			{
				//Tests if the msgwnd is a child of captured window,
				//and returns the msgwnd if it is.
				for(core_window_t * child = wd; child; child = child->parent)
				{
					if(child->parent == attr_.capture.window)
						return wd;
				}
			}
			return attr_.capture.window;
		}

		void capture_ignore_children(bool ignore)
		{
			attr_.capture.ignore_children = ignore;
		}

		bool capture_window_entered(int root_x, int root_y, bool& prev)
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

		core_window_t * capture_window() const
		{
			return attr_.capture.window;
		}

		//capture_window
		//@brief:	set a window that always captures the mouse event if it is not in the range of window
		//@return:	this function dose return the previous captured window. If the wnd set captured twice,
		//			the return value is NULL
		core_window_t* capture_window(core_window_t* wd, bool value)
		{
			nana::point pos = interface_type::cursor_position();
			std::vector<std::pair<core_window_t*, bool> > & attr_cap = attr_.capture.history;
			if(value)
			{
				if(wd != attr_.capture.window)
				{
					//Thread-Safe Required!
					threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);

					if(handle_manager_.available(wd))
					{
						wd->flags.capture = true;
						interface_type::capture_window(wd->root, value);
						core_window_t* prev = attr_.capture.window;
						if(prev && prev != wd)
							attr_cap.push_back(std::make_pair(prev, attr_.capture.ignore_children));

						attr_.capture.window = wd;
						attr_.capture.ignore_children = true;
						interface_type::calc_window_point(wd->root, pos);
						attr_.capture.inside = _m_effective(wd, pos.x, pos.y);
						return prev;
					}
				}

				return attr_.capture.window;
			}
			else if(wd == attr_.capture.window)
			{
				attr_.capture.window = 0;
				if(attr_cap.size())
				{
					std::pair<core_window_t*, bool> last = attr_cap.back();
					attr_cap.erase(attr_cap.end() - 1);

					if(handle_manager_.available(last.first))
					{
						attr_.capture.window = last.first;
						attr_.capture.ignore_children = last.second;
						interface_type::capture_window(last.first->root, true);
						interface_type::calc_window_point(last.first->root, pos);
						attr_.capture.inside = _m_effective(last.first, pos.x, pos.y);
					}
				}

				if(wd && (0 == attr_.capture.window))
					interface_type::capture_window(wd->root, false);
			}
			else
			{
				for(typename std::vector<std::pair<core_window_t*, bool> >::iterator it = attr_cap.begin(); it != attr_cap.end(); ++it)
				{
					if(it->first == wd)
					{
						attr_cap.erase(it);
						break;
					}
				}

				return attr_.capture.window;
			}
			return wd;
		}

		//tabstop
		//@brief: when users press a TAB, the focus should move to the next widget.
		//	this method insert a window which catchs an user TAB into a TAB window container
		//	the TAB window container is held by a wnd's root widget. Not every widget has a TAB window container,
		//	the container is created while a first Tab Window is setting
		void tabstop(core_window_t* wd)
		{
			if(wd == 0) return;

			//Thread-Safe Required!
			threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);
			if(handle_manager_.available(wd) == false)	return;

			if(detail::tab_type::none == wd->flags.tab)
			{
				wd->root_widget->other.attribute.root->tabstop.push_back(wd);
				wd->flags.tab |= detail::tab_type::tabstop;
			}
		}

		core_window_t* tabstop_prev(core_window_t* wd) const
		{
			if(wd)
			{
				//Thread-Safe Required!
				threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);
				if(handle_manager_.available(wd))
				{
					typedef typename core_window_t::container container;
					const container & tabs = wd->root_widget->other.attribute.root->tabstop;
					if(tabs.size() > 1)
					{
						typename container::const_iterator i = std::find(tabs.begin(), tabs.end(), wd);
						if(i != tabs.end())
						{
							if(tabs.begin() == i)
								return tabs.back();
							return *(i - 1);
						}
					}
				}
			}
			return 0;
		}

		core_window_t* tabstop_next(core_window_t* wd) const
		{
			if(wd == 0) return 0;

			//Thread-Safe Required!
			threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);
			if(handle_manager_.available(wd) == false)	return 0;

			core_window_t * root_wd = wd->root_widget;
			if(nana::gui::detail::tab_type::none == wd->flags.tab)
			{
				if(root_wd->other.attribute.root->tabstop.size())
					return (root_wd->other.attribute.root->tabstop[0]);
			}
			else if(nana::gui::detail::tab_type::tabstop & wd->flags.tab)
			{
				typedef typename core_window_t::container cont_t;

				cont_t& container = root_wd->other.attribute.root->tabstop;
				if(container.size())
				{
					typename cont_t::iterator end = container.end();
					typename cont_t::iterator i = std::find(container.begin(), end, wd);
					if(i != end)
					{
						++i;
						core_window_t* ts = (i != end? (*i) : (*(container.begin())));
						return (ts != wd ? ts : 0);
					}
					else
						return *(container.begin());
				}
			}
			return 0;
		}

		void remove_trash_handle(unsigned tid)
		{
			handle_manager_.delete_trash(tid);
		}

		bool enable_effects_bground(core_window_t* wd, bool enabled)
		{
			if(wd)
			{
				//Thread-Safe Required!
				threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);
				if(handle_manager_.available(wd))
					return wndlayout_type::enable_effects_bground(wd, enabled);
			}
			return false;
		}

		bool calc_window_point(core_window_t* wd, nana::point& pos)
		{
			if(wd)
			{
				//Thread-Safe Required!
				threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);
				if(handle_manager_.available(wd))
				{
					if(interface_type::calc_window_point(wd->root, pos))
					{
						pos.x -= wd->pos_root.x;
						pos.y -= wd->pos_root.y;
						return true;
					}
				}
			}
			return false;
		}

		typename root_table_type::value_type* root_runtime(nana::gui::native_window_type root) const
		{
			return root_table_.find(root);
		}

		bool register_shortkey(core_window_t* wd, unsigned long key)
		{
			if(wd == 0) return false;

			//Thread-Safe Required!
			threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);
			if(handle_manager_.available(wd) == false) return false;

			typename root_table_type::value_type* object = root_runtime(wd->root);
			if(object)
				return object->shortkeys.make(reinterpret_cast<nana::gui::window>(wd), key);

			return false;
		}

		void unregister_shortkey(core_window_t* wnd)
		{
			if(wnd == 0) return;

			//Thread-Safe Required!
			threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);
			if(handle_manager_.available(wnd) == false) return;

			typename root_table_type::value_type * object = root_runtime(wnd->root);
			if(object) object->shortkeys.umake(reinterpret_cast<nana::gui::window>(wnd));
		}

		core_window_t* find_shortkey(nana::gui::native_window_type native_window, unsigned long key)
		{
			if(native_window)
			{
				//Thread-Safe Required!
				threads::lock_guard<threads::recursive_mutex> lock(wnd_mgr_lock_);
				typename root_table_type::value_type* object = root_runtime(native_window);
				if(object)
					return reinterpret_cast<core_window_t*>(object->shortkeys.find(key));
			}
			return 0;
		}

	private:
		struct inner_delete_helper
		{
			inner_delete_helper(window_manager* wndmgr)
				:wmgr_(wndmgr)
			{}

			void operator()(core_window_t* wnd)
			{
				wmgr_->_m_destroy(wnd, *this);
			}
		private:
			window_manager * wmgr_;
		};

		struct inner_move_helper
		{
			inner_move_helper(window_manager* wmgr, int x, int y)
				:delta_x_(x), delta_y_(y), wmgr_(wmgr)
			{}

			void operator()(core_window_t* wd)
			{
				wmgr_->_m_move_core(wd, *this);
			}

			int delta_x_;
			int delta_y_;
		private:
			window_manager * wmgr_;
		};
	private:
		void _m_destroy(core_window_t* wd, inner_delete_helper& dl)
		{
			if(wd->flags.destroying) return;

			bedrock_type & bedrock = bedrock_type::instance();
			bedrock.thread_context_destroy(wd);

			wd->flags.destroying = true;

			if(wd->together.caret)
			{
				//The deletion of caret wants to know whether the window is destroyed under SOME platform. Such as X11
				delete wd->together.caret;
				wd->together.caret = 0;
			}
			//Delete the children widgets.
			std::for_each(wd->children.rbegin(), wd->children.rend(), dl);
			wd->children.clear();

			nana::gui::eventinfo ei;
			ei.identifier = event_code::destroy;
			ei.window = reinterpret_cast<nana::gui::window>(wd);
			bedrock_type::raise_event(event_code::destroy, wd, ei, true);

			core_window_t * root_wd = wd->root_widget;
			if(root_wd->other.attribute.root->focus == wd)
				root_wd->other.attribute.root->focus = 0;

			if(root_wd->other.attribute.root->menubar == wd)
				root_wd->other.attribute.root->menubar = 0;

			wndlayout_type::enable_effects_bground(wd, false);

			//test if wd is a TABSTOP window
			if(wd->flags.tab & nana::gui::detail::tab_type::tabstop)
			{
				typename core_window_t::container & tabs = root_wd->other.attribute.root->tabstop;
				typename core_window_t::container::iterator i = std::find(tabs.begin(), tabs.end(), wd);
				if(i != tabs.end())
					tabs.erase(i);
			}

			if(wd->effect.edge_nimbus)
			{
				typename core_window_t::edge_nimbus_container & cont = root_wd->other.attribute.root->effects_edge_nimbus;
				for(typename core_window_t::edge_nimbus_container::iterator i = cont.begin(); i != cont.end(); ++i)
				{
					if(i->window == wd)
					{
						cont.erase(i);
						break;
					}
				}
			}

			bedrock.evt_manager.umake(reinterpret_cast<nana::gui::window>(wd), false);
			wd->drawer.detached();
			signal_manager_.fireaway(wd, signals::destroy, signals_);
			detach_signal(wd);

			if(wd->parent && (wd->parent->children.size() > 1))
			{
				typename core_window_t::container::iterator it = wd->parent->children.begin(), end = wd->parent->children.end();
				for(; it != end; ++it)
				{
					if((*it)->index > wd->index)
					{
						for(; it != end; ++it)
							--((*it)->index);
						break;
					}
				}
			}

			if(wd->other.category == static_cast<category::flags::t>(category::frame_tag::value))
			{
				//remove the frame handle from the WM frames manager.
				{
					std::vector<core_window_t*> & frames = root_wd->other.attribute.root->frames;
					typename std::vector<core_window_t*>::iterator i = std::find(frames.begin(), frames.end(), wd);
					if(i != frames.end())
						frames.erase(i);
				}

				//The frame widget does not have an owner, and close their element windows without activating owner.
				//close the frame container window, it's a native window.
				typename std::vector<native_window>::iterator i = wd->other.attribute.frame->attach.begin(), end = wd->other.attribute.frame->attach.end();
				for(; i != end; ++i)
					interface_type::close_window(*i);

				interface_type::close_window(wd->other.attribute.frame->container);
			}

			if(wd->other.category != static_cast<category::flags::t>(category::root_tag::value))	//Not a root window
				handle_manager_.remove(wd);
		}

		void _m_move_core(core_window_t* wd, inner_move_helper& mv_helper)
		{
			if(wd->other.category != static_cast<category::flags::t>(category::root_tag::value))	//A root widget always starts at (0, 0) and its childs are not to be changed
			{
				wd->pos_root.x += mv_helper.delta_x_;
				wd->pos_root.y += mv_helper.delta_y_;

				if(wd->other.category == static_cast<category::flags::t>(category::frame_tag::value))
					interface_type::move_window(wd->other.attribute.frame->container, wd->pos_root.x, wd->pos_root.y);

				std::for_each(wd->children.begin(), wd->children.end(), mv_helper);
			}
		}

		//_m_find
		//@brief: find a window on root window through a given root coordinate.
		//		the given root coordinate must be in the rectangle of wnd.
		core_window_t* _m_find(core_window_t* wd, int x, int y)
		{
			if(wd->visible == 0) return 0;

			for(typename cont_type::reverse_iterator i = wd->children.rbegin(); i != wd->children.rend(); ++i)
			{
				core_window_t* result = *i;
				if((result->other.category != static_cast<category::flags::t>(category::root_tag::value)) && _m_effective(result, x, y))
				{
					result = _m_find(result, x, y);
					if(result)
						return result;
				}
			}
			return wd;
		}

		//_m_effective, test if the window is a handle of window that specified by (root_x, root_y)
		static bool _m_effective(core_window_t* wd, int root_x, int root_y)
		{
			if(wd == 0 || wd->visible == 0)	return false;
			return nana::gui::is_hit_the_rectangle(nana::rectangle(wd->pos_root, wd->dimension), root_x, root_y);
		}

	private:
		handle_manager<core_window_t*, window_manager>	handle_manager_;
		mutable reversible_mutex wnd_mgr_lock_;
		root_table_type			root_table_;
		signals					signals_;
		signal_manager	signal_manager_;
		tray_event_manager	tray_event_manager_;

		nana::paint::image default_icon_;

		struct attribute
		{
			struct captured
			{
				captured():window(0), ignore_children(true)
				{}

				core_window_t	*window;
				bool		inside;
				bool		ignore_children;
				std::vector<std::pair<core_window_t*, bool> > history;
			}capture;
		}attr_;

		struct menu_tag
		{
			menu_tag()
				:window(0), owner(0), has_keyboard(false)
			{}

			nana::gui::native_window_type window;
			nana::gui::native_window_type owner;
			bool has_keyboard;
		}menu_;
	};//end class window_manager
}//end namespace detail
}//end namespace gui
}//end namespace nana
#endif
