//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************

#include "cex/src/forward.h"
#include "cex/src/icex.h"

namespace cex
{
	struct NewLines :ICex
	{
		std::string m_infile;
		std::string m_outfile;
		size_t m_min, m_max;
		std::string m_lineends;
		bool m_replace_infile;
		pr::script::FileSrc<> m_filesrc;
		pr::script::StripNewLines<> m_filter;
		pr::script::Src* m_src;

		NewLines()
			:m_infile()
			,m_outfile()
			,m_min(0)
			,m_max(~size_t())
			,m_lineends()
			,m_replace_infile()
			,m_filesrc()
			,m_filter()
			,m_src()
		{}

		// Show the main help
		void ShowHelp() const override
		{
			printf(
				"Add or remove new lines from a text file\n"
				" Syntax: Cex -newlines -f 'FileToFormat' [-o 'OutputFilename'] [-limit min max] [-lineends end-style]\n"
				"    -f <filepath> : The file to format\n"
				"    -o <out-filepath> : Output filename\n"
				"    -limit min max : Set limits on the number of consecutive new lines\n"
				"    -lineends end-style : Replace line ends with CR, LF, CRLF, or LFCR\n"
			);
		}

		// Parse the command line options
		bool CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end) override
		{
			if (pr::str::EqualI(option, "-newlines"))
			{
				return true;
			}
			if (pr::str::EqualI(option, "-f"))
			{
				if (arg_end - arg < 1) throw std::exception("-f must be followed by a filepath");
				m_infile = *arg++;
				return true;
			}
			if (pr::str::EqualI(option, "-o"))
			{
				if (arg_end - arg < 1) throw std::exception("-o must be followed by a filepath");
				m_outfile = *arg++;
				return true;
			}
			if (pr::str::EqualI(option, "-limit"))
			{
				if (arg_end - arg < 2) throw std::exception("-limit command requires two arguments; min max");
				m_min = pr::To<size_t>(*arg++, 10);
				m_max = pr::To<size_t>(*arg++, 10);
				return true;
			}
			if (pr::str::EqualI(option, "-lineends"))
			{
				if (arg_end - arg < 1) throw std::exception("-lineends must be followed by one of CR, LF, CRLF, or combinations of these, e.g. CRLFCR");
				m_lineends = pr::str::LowerCaseC(*arg++);
				pr::str::Replace(m_lineends, "cr", "\r");
				pr::str::Replace(m_lineends, "lf", "\n");
				return true;
			}
			return ICex::CmdLineOption(option, arg, arg_end);
		}

		// Sanity check the input
		void ValidateInput() override
		{
			m_replace_infile = m_outfile.empty();
			if (m_replace_infile) m_outfile = m_infile + ".tmp";

			if (!pr::filesys::FileExists(m_infile))
				throw std::exception(pr::FmtS("Input file '%s' doesn't exist", m_infile.c_str()));

			m_filesrc.Open(m_infile);
			m_src = &m_filesrc;
			if (m_min != 0 || m_max != ~size_t())
			{
				m_filter.SetLimits(m_min, m_max);
				m_filter.Source(m_filesrc);
				m_src = &m_filter;
			}

			if (m_lineends.empty())
				m_lineends = "\n";
		}

		// Run the command
		int Run() override
		{
			// Run the formatters over the input file
			std::cout << "Running formatting...";
			
			std::ofstream ofile(m_outfile.c_str());
			if (ofile.bad())
				throw std::exception(pr::FmtS("Failed to create output file '%s'\n", m_outfile.c_str()));

			for (auto& src = *m_src; *src; ++src)
			{
				if (*src == '\n')
					ofile << m_lineends;
				else
					ofile << *src;
			}

			std::cout << "done\n";

			// If we're replacing the input file...
			if (m_replace_infile)
			{
				if (!pr::filesys::RepFile(m_outfile, m_infile))
				{
					auto last = GetLastError();
					std::cout << pr::FmtS("Failed to replace '%s' with '%s'\nError code: 0x%08X\n", m_infile.c_str(), m_outfile.c_str(), last);
				}
			}

			return 0;
		}
	};
}