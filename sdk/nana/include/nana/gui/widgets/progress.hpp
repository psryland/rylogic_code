/*
 *	A Progress Indicator Implementation
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0. 
 *	(See accompanying file LICENSE_1_0.txt or copy at 
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/widgets/progress.hpp
 */

#ifndef NANA_GUI_WIDGET_PROGRESS_HPP
#define NANA_GUI_WIDGET_PROGRESS_HPP
#include "widget.hpp"

namespace nana
{
namespace gui
{
	namespace drawerbase
	{
		namespace progress
		{
			class trigger: public drawer_trigger
			{
			public:
				trigger();
				unsigned value() const;
				unsigned value(unsigned);
				unsigned inc();
				unsigned Max() const;
				unsigned Max(unsigned);
				void unknown(bool);
				bool unknown() const;
			private:
				void attached(widget_reference, graph_reference)	override;
				void refresh(graph_reference)	override;
			private:
				void _m_draw();
				void _m_draw_box(graph_reference);
				void _m_draw_progress(graph_reference);
				bool _m_check_changing(unsigned) const;
			private:
				static const unsigned border = 2;
				widget * widget_;
				nana::paint::graphics* graph_;
				unsigned draw_width_;
				bool has_value_;
				bool unknown_;
				unsigned max_;
				unsigned value_;
			}; //end class drawer
		}
	}//end namespace drawerbase

	class progress
		: public widget_object<category::widget_tag, drawerbase::progress::trigger>
	{
	public:
		progress();
		progress(window, bool visible);
		progress(window, const rectangle & = rectangle(), bool visible = true);

		unsigned value() const;
		unsigned value(unsigned val);
		unsigned inc();
		unsigned amount() const;
		unsigned amount(unsigned value);
		void unknown(bool);
		bool unknown() const;
	};
}//end namespace gui
}//end namespace nana
#endif
