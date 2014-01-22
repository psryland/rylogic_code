#include <nana/gui/functional.hpp>
#include <nana/gui/programming_interface.hpp>

namespace nana{	namespace gui{
	//class destroy
		destroy::destroy(window wd)
			: wd_(wd)
		{}

		void destroy::operator()() const
		{
			API::close_window(wd_);
		}
	//end class destroy

	//class show
		show::show(window wd)
			:wd_(wd)
		{}

		void show::operator()() const
		{
			API::show_window(wd_, true);
		}
	//end class show

	//class hide
		hide::hide(window wd)
			: wd_(wd)
		{}

		void hide::operator ()() const
		{
			API::show_window(wd_, false);
		}
}
}
