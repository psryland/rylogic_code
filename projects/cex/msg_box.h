//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************

#include "cex/forward.h"
#include "cex/icex.h"

namespace cex
{
	struct MsgBox :ICex
	{
		std::string m_title;    // Title of the message box
		std::string m_text;     // Body text of the message box
		unsigned int m_style;   // Message box style
		
		MsgBox()
			:m_title("Message")
			,m_text("")
			,m_style(0)
		{}

		void ShowHelp() const override
		{
			std::cout <<
				"MsgBox : Display a message box.\n"
				" Syntax: Cex -msgbox -title \"title text\" -body \"body text\" -style style_id\n";
		}

		bool CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end) override
		{
			if (pr::str::EqualI(option, "-msgbox")) { return true; }
			if (pr::str::EqualI(option, "-title") && arg != arg_end) { m_title = *arg++; return true; }
			if (pr::str::EqualI(option, "-body" ) && arg != arg_end) { m_text  = *arg++; return true; }
			if (pr::str::EqualI(option, "-style") && arg != arg_end) { m_style = (unsigned int)strtoul(arg->c_str(), 0, 10); ++arg; return true; }
			return ICex::CmdLineOption(option, arg, arg_end);
		}

		int Run() override
		{
			return MessageBoxA(0, m_text.c_str(), m_title.c_str(), m_style);
		}
	};
}
