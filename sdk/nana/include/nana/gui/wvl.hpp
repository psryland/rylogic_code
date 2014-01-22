/*
 *	Nana GUI Library Definition
 *	Copyright(C) 2003-2013 Jinhao(cnjinhao@hotmail.com)
 *
 *	Distributed under the Boost Software License, Version 1.0.
 *	(See accompanying file LICENSE_1_0.txt or copy at
 *	http://www.boost.org/LICENSE_1_0.txt)
 *
 *	@file: nana/gui/wvl.hpp
 *	@description:
 *		the header file contains the files required for running of Nana.GUI
 */

#ifndef NANA_GUI_WVL_HPP
#define NANA_GUI_WVL_HPP

#include "programming_interface.hpp"
#include "widgets/form.hpp"
#include "drawing.hpp"
#include "msgbox.hpp"
#include "../exceptions.hpp"

namespace nana
{
namespace gui
{
	template<typename Form, bool IsMakeVisible = false>
	class form_loader
	{
	public:
		Form& operator()()	const
		{
			Form* res = detail::bedrock::instance().rt_manager.create_form<Form>();
			if(res == 0)
				throw nana::bad_window("form_loader.operator(): failed to create a window");

			if(IsMakeVisible) res->show();

			return *res;
		}

		Form& operator()(nana::gui::widget& owner)	const
		{
			Form* res = detail::bedrock::instance().rt_manager.create_form<Form, nana::gui::widget&>(owner);
			if(res == 0)
				throw nana::bad_window("form_loader.operator(): failed to create a window");
			if(IsMakeVisible) res->show();

			return *res;
		}

		Form& operator()(nana::gui::window owner) const
		{
			Form* res = detail::bedrock::instance().rt_manager.create_form<Form, nana::gui::window>(owner);
			if(res == 0)
				throw nana::bad_window("form_loader.operator(): failed to create a window");
			if(IsMakeVisible) res->show();

			return *res;
		}

		template<typename Param1, typename Param2>
		Form& operator()(Param1 p1, Param2 p2) const
		{
			Form* res = detail::bedrock::instance().rt_manager.create_form<Form, Param1, Param2>(p1, p2);
			if(res == 0)
				throw nana::bad_window("form_loader.operator(): failed to create a window");
			if(IsMakeVisible) res->show();

			return *res;
		}

		template<typename Param1, typename Param2, typename Param3>
		Form& operator()(Param1 p1, Param2 p2, Param3 p3) const
		{
			Form* res = detail::bedrock::instance().rt_manager.create_form<Form, Param1, Param2, Param3>(p1, p2, p3);
			if(res == 0)
				throw nana::bad_window("form_loader.operator(): failed to create a window");
			if(IsMakeVisible) res->show();

			return *res;
		}

		template<typename Param1, typename Param2, typename Param3, typename Param4>
		Form& operator()(Param1 p1, Param2 p2, Param3 p3, Param4 p4) const
		{
			Form* res = detail::bedrock::instance().rt_manager.create_form<Form, Param1, Param2, Param3, Param4>(p1, p2, p3, p4);
			if(res == 0)
				throw nana::bad_window("form_loader.operator(): failed to create a window");
			if(IsMakeVisible) res->show();

			return *res;
		}

		template<typename Param1, typename Param2, typename Param3, typename Param4, typename Param5>
		Form& operator()(Param1 p1, Param2 p2, Param3 p3, Param4 p4, Param5 p5) const
		{
			Form* res = detail::bedrock::instance().rt_manager.create_form<Form, Param1, Param2, Param3, Param4, Param5>(p1, p2, p3, p4, p5);
			if(res == 0)
				throw nana::bad_window("form_loader.operator(): failed to create a window");
			if(IsMakeVisible) res->show();

			return *res;
		}
	};

	void exec();
}//end namespace gui
}//end namespace nana
#endif
