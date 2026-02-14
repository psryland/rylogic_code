//**********************************************
// Console Extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
#include "src/forward.h"
#include "pr/script/script_core.h"
#include "pr/script/filter.h"
#include "pr/str/string_util.h"

namespace cex
{
	struct Cmd_NewLines
	{
		std::filesystem::path m_infile;
		std::filesystem::path m_outfile;
		int m_min, m_max;
		std::string m_lineends;
		bool m_replace_infile;

		Cmd_NewLines()
			:m_infile()
			,m_outfile()
			,m_min(0)
			,m_max(~0U >> 1)
			,m_lineends()
			,m_replace_infile()
		{}

		void ShowHelp() const
		{
			std::cout <<
				"Add or remove new lines from a text file\n"
				" Syntax: Cex -newlines -f 'FileToFormat' [-o 'OutputFilename'] [-limit min max] [-lineends end-style]\n"
				"    -f <filepath> : The file to format\n"
				"    -o <out-filepath> : Output filename\n"
				"    -limit min max : Set limits on the number of consecutive new lines\n"
				"    -lineends end-style : Replace line ends with CR, LF, CRLF, or LFCR\n";
		}

		int Run(pr::CmdLine const& args)
		{
			using namespace pr::script;
			using namespace std::filesystem;

			if (args.count("help") != 0)
				return ShowHelp(), 0;

			// Parse arguments
			if (args.count("f") != 0) { m_infile = args("f").as<std::string>(); }
			if (args.count("o") != 0) { m_outfile = args("o").as<std::string>(); }
			if (args.count("limit") != 0)
			{
				auto const& arg = args("limit");
				m_min = arg.as<int>(0);
				m_max = arg.as<int>(1);
			}
			if (args.count("lineends") != 0)
			{
				m_lineends = args("lineends").as<std::string>();
				pr::str::LowerCase(m_lineends);
				pr::str::Replace(m_lineends, "cr", "\r");
				pr::str::Replace(m_lineends, "lf", "\n");
			}

			// Validate input
			m_replace_infile = m_outfile.empty();
			if (m_replace_infile)
				m_outfile = path(m_infile).concat(".tmp");

			if (!exists(m_infile))
				throw std::runtime_error(std::format("Input file '{}' doesn't exist", m_infile.string()));

			if (m_lineends.empty())
				m_lineends = "\n";

			// Run the formatters over the input file
			std::cout << "Running formatting...";

			std::ofstream ofile(m_outfile);
			if (ofile.bad())
				throw std::runtime_error(std::format("Failed to create output file '{}'\n", m_outfile.string()));

			FileSrc filesrc(m_infile);
			StripNewLines filter(filesrc, m_min, m_max);
			for (auto& src = filter; *src; ++src)
			{
				if (*src == '\n')
					ofile << m_lineends;
				else
					ofile << static_cast<char>(*src);
			}

			std::cout << "done\n";

			// If we're replacing the input file...
			if (m_replace_infile)
			{
				try { copy_file(m_outfile, m_infile, copy_options::overwrite_existing); }
				catch (filesystem_error const& ex)
				{
					std::cout << std::format("Failed to replace '{}' with '{}'\n{}\n", m_infile.string(), m_outfile.string(), ex.code().message());
				}
			}

			return 0;
		}
	};

	int NewLines(pr::CmdLine const& args)
	{
		Cmd_NewLines cmd;
		return cmd.Run(args);
	}
}