/*
 *	A Panel Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: source/gui/widgets/panel.cpp
 *
 *	@brief: panel is a widget used for placing some widgets.
 */

#include <nana/gui/widgets/panel.hpp>

namespace nana{	namespace gui
{
	namespace drawerbase
	{
		namespace panel
		{
		//class drawer
			drawer::drawer()
				:window_(0)
			{}

			void drawer::bind_window(widget_reference widget)
			{
				widget.caption(STR("Nana Panel"));
				window_ = widget.handle();
			}

			void drawer::refresh(graph_reference graph)
			{
				if(bground_mode::basic != API::effects_bground_mode(window_))
					graph.rectangle(API::background(window_), true);
			}
		//end class drawer
		}//end namespace panel
	}//end namespace drawerbase
}//end namespace gui
}//end namespace nana
