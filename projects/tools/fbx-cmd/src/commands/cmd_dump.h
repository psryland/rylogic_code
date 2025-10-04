#pragma once
#include "src/forward.h"
#include "src/icommand.h"

using namespace pr::geometry;

namespace fbx_cmd
{
	struct DumpFbx :ICommand
	{
		// Notes:
		//  - Test command lines:
		//   fbx-cmd -dump E:\Rylogic\Code\art\models\AnimCharacter\AnimatedCharacter.fbx

		std::filesystem::path m_filepath;

		void ShowHelp() const override
		{
			std::cout <<
				"Dump the structure of an FBX file\n"
				" Syntax: fbx-cmd -dump filename.fbx\n"
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

			std::ifstream ifile(m_filepath, std::ios::binary);
			pr::geometry::fbx::Scene scene(ifile);
			scene.Dump({
				.m_parts =
					fbx::EParts::NodeHierarchy |
					//fbx::EParts::Meshes |
					//fbx::EParts::Skeletons |
					//fbx::EParts::Skins |
					fbx::EParts::None,
				.m_coord_system = fbx::ECoordSystem::PosX_PosY_NegZ,
				.m_triangulate_meshes = true,
			}, std::cout);

			return 0;
		}
	};
}
