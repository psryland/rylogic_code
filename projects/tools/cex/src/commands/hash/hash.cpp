//**********************************************
// Console Extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
#include "src/forward.h"
#include "pr/common/hash.h"

namespace cex
{
	struct Cmd_Hash
	{
		std::string m_text;

		Cmd_Hash()
			:m_text()
		{}

		void ShowHelp() const
		{
			std::cout <<
				"Hash the given stdin data\n"
				" Syntax: Cex -hash data_to_hash...\n";
		}

		int Run(pr::CmdLine const& args)
		{
			if (args.count("help") != 0)
				return ShowHelp(), 0;

			for (auto const& text : args("hash").values)
				m_text.append(text);

			auto hash = pr::hash::Hash(m_text.c_str());
			auto ui = static_cast<unsigned int>(hash);
			std::cout << std::format("{:08X}", ui);
			return 0;
		}
	};

	int Hash(pr::CmdLine const& args)
	{
		Cmd_Hash cmd;
		return cmd.Run(args);
	}
}
