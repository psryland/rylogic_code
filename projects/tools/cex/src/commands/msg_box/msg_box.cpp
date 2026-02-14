//**********************************************
// Console Extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
#include "src/forward.h"

namespace cex
{
	struct Cmd_MsgBox
	{
		std::string  m_title; // Title of the message box
		std::string  m_text;  // Body text of the message box
		unsigned int m_style; // Message box style

		Cmd_MsgBox()
			:m_title("Message")
			,m_text()
			,m_style(0)
		{}

		void ShowHelp() const
		{
			std::cout <<
				"MsgBox : Display a message box.\n"
				" Syntax: Cex -msgbox -title \"title text\" -body \"body text\" -style style_id\n";
		}

		int Run(pr::CmdLine const& args)
		{
			if (args.count("help") != 0)
				return ShowHelp(), 0;

			if (args.count("title") != 0) { m_title = args("title").as<std::string>(); }
			if (args.count("body")  != 0) { m_text  = args("body").as<std::string>();  }
			if (args.count("style") != 0) { m_style = args("style").as<unsigned int>(); }

			return MessageBoxA(0, m_text.c_str(), m_title.c_str(), m_style);
		}
	};

	int MsgBox(pr::CmdLine const& args)
	{
		Cmd_MsgBox cmd;
		return cmd.Run(args);
	}
}
