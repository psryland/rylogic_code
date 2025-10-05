#pragma once
#include "src/forward.h"
#include "src/icommand.h"

using namespace pr;
using namespace pr::geometry;

namespace fbx_cmd
{
	struct Triangulate :ICommand
	{
		// Notes:
		//  - Test command lines:
		//   fbx-cmd -triangulate E:\Rylogic\Code\art\models\AnimCharacter\AnimatedCharacter.fbx
		//   fbx-cmd -triangulate E:/Dump/Hyperpose/fbx/hyperpose_sample.fbx

		std::filesystem::path m_ifilepath;
		std::filesystem::path m_ofilepath;

		void ShowHelp() const override
		{
			std::cout <<
				"Triangulate the meshes in an FBX file\n"
				" Syntax: fbx-cmd -triangulate filename.fbx [-ofile filename.fbx]\n"
				;
		}

		bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end) override
		{
			if (pr::str::EqualI(option, "-triangulate"))
			{
				m_ifilepath = *arg++;
				return true;
			}
			if (pr::str::EqualI(option, "-ofile"))
			{
				m_ofilepath = *arg++;
				return true;
			}
			return ICommand::CmdLineOption(option, arg, arg_end);
		}

		bool CmdLineData(TArgIter&, TArgIter) override
		{
			return true;
		}

		int Run() override
		{
			if (m_ifilepath.empty())
			{
				throw std::runtime_error("No input file specified");
			}
			if (m_ofilepath.empty())
			{
				m_ofilepath = m_ifilepath;
				m_ofilepath.replace_extension(".triangulated.fbx");
			}

			std::ifstream ifile(m_ifilepath, std::ios::binary);
			std::ofstream ofile(m_ofilepath, std::ios::trunc | std::ios::binary);

			pr::geometry::fbx::Scene scene(ifile);
			scene.Read(fbx::ReadOptions{});
			scene.Write(ofile, fbx::EFormat::Binary);

			return 0;
		}
	};
}
