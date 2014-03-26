
#include <nana/gui/dragger.hpp>

namespace nana{ namespace gui{


	class dragger::dragger_impl_t
	{
		struct drag_target_t
		{
			window wd;
			nana::point origin;
		};

		struct trigger_t
		{
			window wd;
			event_handle press;
			event_handle over;
			event_handle release;
			event_handle destroy;
		};
	public:
		dragger_impl_t()
			: dragging_(false)
		{}

		~dragger_impl_t()
		{
			_m_clear_triggers();
		}

		void drag_target(window wd)
		{
			drag_target_t dt;
			dt.wd = wd;
			targets_.push_back(dt);
		}

		void trigger(window wd)
		{
			trigger_t tg;
			tg.wd = wd;
			auto f = std::bind(&dragger_impl_t::_m_trace, this, std::placeholders::_1);
			tg.press = API::make_event<events::mouse_down>(wd, f);
			tg.over = API::make_event<events::mouse_move>(wd, f);
			tg.release = API::make_event<events::mouse_up>(wd, f);
			tg.destroy = API::make_event<events::destroy>(wd, f);

			triggers_.push_back(tg);
		}
	private:
		void _m_clear_triggers()
		{
			for(auto & t : triggers_)
			{
				API::umake_event(t.press);
				API::umake_event(t.over);
				API::umake_event(t.release);
				API::umake_event(t.destroy);
				API::capture_window(t.wd, false);
			}
			triggers_.clear();
		}

		void _m_destroy(const eventinfo& ei)
		{
			for(auto i = triggers_.begin(); i != triggers_.end(); ++i)
			{
				if(i->wd == ei.window)
				{
					triggers_.erase(i);
					API::capture_window(ei.window, false);
					return;
				}
			}
		}

		void _m_trace(const eventinfo& ei)
		{
			switch(ei.identifier)
			{
			case events::mouse_down::identifier:
				dragging_ = true;
				API::capture_window(ei.window, true);
				origin_ = API::cursor_position();
				for(auto & t : targets_)
				{
					t.origin = API::window_position(t.wd);
					window owner = API::get_owner_window(t.wd);
					if(owner)
						API::calc_screen_point(owner, t.origin);
				}
				break;
			case events::mouse_move::identifier:
				if(dragging_ && ei.mouse.left_button)
				{
					nana::point pos = API::cursor_position();
					pos.x -= origin_.x;
					pos.y -= origin_.y;
					for(auto & t : targets_)
					{
						if(API::is_window_zoomed(t.wd, true) == false)
						{
							window owner = API::get_owner_window(t.wd);
							if(owner)
							{
								nana::point wdps = t.origin;
								API::calc_window_point(owner, wdps);
								wdps.x += pos.x;
								wdps.y += pos.y;
								API::move_window(t.wd, wdps.x, wdps.y);
							}
							else
								API::move_window(t.wd, t.origin.x + pos.x, t.origin.y + pos.y);
						}
					}
				}
				break;
			case events::mouse_up::identifier:
				API::capture_window(ei.window, false);
				dragging_ = false;
				break;
			default:
				break;
			}
		}

	private:
		bool dragging_;
		nana::point origin_;
		std::vector<drag_target_t> targets_;
		std::vector<trigger_t> triggers_;
	};

	//class dragger
		dragger::dragger()
			: impl_(new dragger_impl_t)
		{
		}

		dragger::~dragger()
		{
			delete impl_;
		}

		void dragger::target(window wd)
		{
			impl_->drag_target(wd);
		}

		void dragger::trigger(window tg)
		{
			impl_->trigger(tg);
		}
	//end class dragger

}//end namespace gui
}//end namespace nana
