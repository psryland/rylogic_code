#ifndef NANA_GUI_FUNCTIONAL_HPP
#define NANA_GUI_FUNCTIONAL_HPP
#include "basis.hpp"

namespace nana{	namespace gui{
	class destroy
	{
	public:
		destroy(window);
		void operator()() const;
	protected:
		window wd_;
	};

	class show
	{
	public:
		show(window);
		void operator()() const;
	protected:
		window wd_;
	};

	class hide
	{
	public:
		hide(window);
		void operator()() const;
	protected:
		window wd_;
	};
}//end namespace gui
}//end namespace nana

#endif

