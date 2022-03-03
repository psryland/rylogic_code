//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
#include "src/forward.h"
#include "src/icex.h"

namespace cex
{
	struct Clip :ICex
	{
		std::string m_text;
		bool m_lwr, m_upr, m_fwdslash, m_bkslash, m_cstr, m_dopaste;
		std::string m_newline;
		
		Clip()
			:m_text()
			,m_lwr(false)
			,m_upr(false)
			,m_fwdslash(false)
			,m_bkslash(false)
			,m_cstr(false)
			,m_dopaste(false)
			,m_newline()
		{}

		void ShowHelp() const override
		{
			std::cout <<
				"Clip text to the system clipboard\n"
				" Syntax: Cex -clip [-lwr][-upr][-fwdslash][-bkslash][-cstr] [-crlf|cr|lf] text_to_copy ...\n"
				"  -lwr : converts copied text to lower case\n"
				"  -upr : converts copied text to upper case\n"
				"  -fwdslash : converts any directory marks to forward slashes\n"
				"  -bkslash : converts any directory marks to back slashes\n"
				"  -cstr : converts the copied text to a C\\C++ style string by adding escape characters\n"
				"  -crlf|cr|lf : convert newlines to the dos,mac,linux format\n"
				"\n"
				" Syntax: Cex -clip -paste\n"
				"   Paste the clipboard contents to stdout\n"
				;
		}

		bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end) override
		{
			if (pr::str::EqualI(option, "-clip"    )) { return true; }
			if (pr::str::EqualI(option, "-lwr"     )) { m_lwr = true; return true; }
			if (pr::str::EqualI(option, "-upr"     )) { m_upr = true; return true; }
			if (pr::str::EqualI(option, "-fwdslash")) { m_fwdslash = true; return true; }
			if (pr::str::EqualI(option, "-bkslash" )) { m_bkslash = true; return true; }
			if (pr::str::EqualI(option, "-cstr"    )) { m_cstr = true; return true; }
			if (pr::str::EqualI(option, "-crlf"    )) { m_newline = "\r\n"; return true; }
			if (pr::str::EqualI(option, "-cr"      )) { m_newline = "\r"; return true; }
			if (pr::str::EqualI(option, "-lf"      )) { m_newline = "\n"; return true; }
			if (pr::str::EqualI(option, "-paste"   )) { m_dopaste = true; return true; }
			return ICex::CmdLineOption(option, arg, arg_end);
		}

		bool CmdLineData(TArgIter& arg, TArgIter) override
		{
			if (!m_text.empty()) m_text += "\r\n";
			m_text += *arg++;
			return true;
		}

		int Run() override
		{
			if (m_dopaste)
			{
				std::string output;
				if (!pr::GetClipBoardText(GetConsoleWindow(), output)) return -1;
				std::cout << output;
				return 0;
			}

			// Perform optional conversions
			if (m_lwr)              { pr::str::LowerCase(m_text); }
			if (m_upr)              { pr::str::UpperCase(m_text); }
			if (m_fwdslash)         { pr::str::Replace(m_text, "\\\\", "/");  pr::str::Replace(m_text, "\\", "/"); }
			if (m_bkslash)          { pr::str::Replace(m_text, "\\\\", "\\"); pr::str::Replace(m_text, "/", "\\"); }
			if (!m_newline.empty()) { pr::str::Replace(m_text, "\r\n", "\n"); pr::str::Replace(m_text, "\r", "\n"); pr::str::Replace(m_text, "\n", m_newline.c_str()); }
			if (m_cstr)             { m_text = pr::str::StringToCString<std::string>(m_text); }
			return pr::SetClipBoardText(GetConsoleWindow(), m_text) ? 0 : -1;
		}
	};
}
