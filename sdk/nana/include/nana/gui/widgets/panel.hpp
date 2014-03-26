/*
 *	A Panel Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/widgets/panel.hpp
 *
 *	@brief: panel is a widget used for placing some widgets.
 */

#ifndef NANA_GUI_WIDGETS_PANEL_HPP
#define NANA_GUI_WIDGETS_PANEL_HPP
#include "widget.hpp"

namespace nana{	namespace gui
{
	namespace drawerbase
	{
		namespace panel
		{
			class drawer: public drawer_trigger
			{
			public:
				drawer();
			private:
				void attached(widget_reference, graph_reference)	override;
				void refresh(graph_reference)	override;
			private:
				window window_;
			};
		}// end namespace panel
	}//end namespace drawerbase

	template<bool HasBackground>
	class panel
		: public widget_object<typename metacomp::static_if<HasBackground,
		gui::category::widget_tag, gui::category::lite_widget_tag>::value_type, drawerbase::panel::drawer>
	{
	public:
		panel(){}

		panel(window wd, bool visible)
		{
			this->create(wd, rectangle(), visible);
		}

		panel(window wd, const nana::rectangle& r = rectangle(), bool visible = true)
		{
			this->create(wd, r, visible);
		}

		bool transparent() const
		{
			return (bground_mode::basic == API::effects_bground_mode(*this));
		}

		void transparent(bool tr)
		{
			if(tr)
				API::effects_bground(*this, effects::bground_transparent(0), 0);
			else
				API::effects_bground_remove(*this);
		}
	};

}//end namespace gui
}//end namespace nana
#endif
