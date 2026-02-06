#pragma once
#include "src/forward.h"
#include "src/icommand.h"

using namespace pr::geometry;

namespace gltf_cmd
{
	struct DumpGltf :ICommand
	{
		// Notes:
		//  - Test command lines:
		//   gltf-cmd -dump path/to/model.gltf
		//   gltf-cmd -dump path/to/model.glb

		std::filesystem::path m_filepath;
		ESceneParts m_parts = ESceneParts::All;

		void ShowHelp() const override
		{
			std::cout <<
				"Dump the structure of a glTF file\n"
				" Syntax: gltf-cmd -dump filename.gltf\n"
				;
		}

		bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end) override
		{
			if (pr::str::EqualI(option, "-dump")) { return true; }
			return ICommand::CmdLineOption(option, arg, arg_end);
		}

		bool CmdLineData(TArgIter& arg, TArgIter) override
		{
			m_filepath = *arg++;
			return true;
		}

		int Run() override
		{
			if (m_filepath.empty())
				throw std::runtime_error("No input file specified");

			auto filepath_str = m_filepath.string();
			gltf::Scene scene(filepath_str.c_str(), gltf::LoadOptions{});

			scene.Dump(std::cout, {
				.m_parts = m_parts,
			});

			return 0;
		}
	};
}
