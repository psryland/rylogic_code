//**********************************************
// P3D Graphics Tool
//  Copyright (c) Rylogic Ltd 2019
//**********************************************

#include "p3d/src/forward.h"
#include "p3d/src/commands/model_io.h"
#include "p3d/src/commands/remove_degenerates.h"
#include "p3d/src/commands/generate_normals.h"
//#include "cex/src/NEW_COMMAND.h"

using namespace pr;
using namespace pr::geometry;
using namespace std::literals;
using namespace std::filesystem;

struct Main
{
	std::unique_ptr<p3d::File> m_model;
	path m_base_dir;
	path m_infile;
	int m_verbosity;

	Main()
		:m_model()
		,m_base_dir()
		,m_infile()
		,m_verbosity(1)
	{}

	// Show the main help
	void ShowHelp() const
	{
		std::cout <<
			"\n"
			"-------------------------------------------------------------\n"
			"  P3D Graphics Tool \n"
			"   Copyright (c) Rylogic 2019 \n"
			"   Version: v1.0\n"
			"-------------------------------------------------------------\n"
			"\n"
			"  This tool is used to generate and modify p3d format geometry models.\n"
			"  It can be driven via script or command line parameters:\n"
			"  Syntax:\n"
			"     p3d.exe script.ldr\n"
			"     p3d.exe [ordered sequence of commands]\n"
			"\n"
			"  Commands:\n"
			"    -verbosity <level>:\n"
			"        Set the level of feedback from this tool (0 .. 3).\n"
			"\n"
			"    -fi <filepath>:\n"
			"        Load a model into memory.\n"
			"        Supported formats: p3d, 3ds, stl (so far)\n"
			"\n"
			"    -fo <filepath>:\n"
			"        Export a p3d format model file.\n"
			"\n"
			"    -RemoveDegenerates [<Tolerance>:<NormalSmoothingAngle>:<ColourDistance>:<UVDistance>]\n"
			"        Simplify a model by removing degenerate verticies.\n"
			"        Parameters can be omitted, in which case defaults are used. e.g.  -RemoveDegenerates 30:::0.001\n"
			"        <Tolerance> - Vertex position quantisation value: [0,32) (default is 10 = 1<<10 = 1024).\n"
			"        <NormalSmoothingAngle> - Vertices with normals different by more than this angle (deg)\n"
			"             are not degenerate. (default normals ignored)\n"
			"        <ColourDistance> - Vertices with colours different by more than this distance are not\n"
			"             degenerate. (default colours ignored)\n"
			"        <UVDistance> - Vertices with  UVs different by more than this distance are not degenerate.\n"
			"            (default UVs ignored)\n"
			"\n"
			"    -GenerateNormals [<SmoothingAngle>]\n"
			"        Generate normals from face data within the model.\n"
			"        SmoothingAngle -  All faces within the smoothing angle of each other are smoothed.\n"
			"\n"
			"    -Transform <m4x4>\n"
			"        Apply a transform to the model.\n"
			"        <m4x4> - A 4x4 matrix given as: 'x.x x.y x.z ... w.z w.w'\n"
			"\n"
			// NEW_COMMAND - add a help string
			"\n"
			;
	}

	// Convert the command line into a script source
	std::unique_ptr<script::Src> ParseCommandLine(std::string args)
	{
		using namespace pr::script;

		// If the command line is a script filepath, return a file source
		if (!cmdline::IsOption(args))
		{
			// If the only argument is a filepath, assume a script file
			auto script_filepath = path(filesys::ResolvePath(args));
			if (script_filepath.empty())
				return nullptr;

			if (!exists(script_filepath))
				throw std::runtime_error("Script '"s + args + "' does not exist");

			m_base_dir = script_filepath.parent_path();
			return std::unique_ptr<FileSrc>(new FileSrc(script_filepath));
		}
		else
		{
			// Otherwise, convert the command line parameters into a script
			struct Parser :cmdline::IOptionReceiver<>
			{
				std::string m_str;
				Parser()
					:m_str()
				{}

				// Read the option passed to Cex
				bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end) override
				{
					for (;;)
					{
						if (str::EqualI(option, "-fi"))
						{
							auto fi = ldr::Section(m_str, "*fi");
							ldr::Append(m_str, ldr::Str(*arg++));
							break;
						}
						if (str::EqualI(option, "-fo"))
						{
							auto fo = ldr::Section(m_str, "*fo");
							ldr::Append(m_str, ldr::Str(*arg++));
							break;
						}
						if (str::EqualI(option, "-RemoveDegenerates"))
						{
							auto sec = ldr::Section(m_str, "*RemoveDegenerates");
							if (!IsOption(*arg))
							{
								int field = 0;
								str::Split(*arg++, ":", [&](std::string const& s, auto i, auto j, int)
								{
									switch (field++)
									{
									case 0: if (i != j) ldr::Append(m_str, "*Quantistation", "{", s.substr(i, j - i), "}"); break;
									case 1: if (i != j) ldr::Append(m_str, "*NormalSmoothingAngle", "{", s.substr(i, j - i), "}"); break;
									case 2: if (i != j) ldr::Append(m_str, "*ColourDistance", "{", s.substr(i, j - i), "}"); break;
									case 3: if (i != j) ldr::Append(m_str, "*UVDistance", "{", s.substr(i, j - i), "}"); break;
									default: throw std::runtime_error(FmtS("RemoveDegenerates - too many parameter fields. Expected %d", field - 1));
									}
								});
							}
							break;
						}
						if (str::EqualI(option, "-GenerateNormals"))
						{
							auto sec = ldr::Section(m_str, "*GenerateNormals");
							for (; arg != arg_end && !IsOption(*arg); ++arg)
							{
								if (int i; str::ExtractIntC(i, 10, arg->c_str())) { ldr::Append(m_str, "*SmoothingAngle {", i, "}"); continue; }
								throw std::runtime_error(FmtS("GenerateNormals - unknown argument:  %s", arg->c_str()));
							}
							break;
						}
						if (str::EqualI(option, "-Transform"))
						{
							float m[16]; int i;
							for (i = 0; i != 16 && arg != arg_end && !IsOption(*arg) && str::ExtractRealC(m[i], arg->c_str()); ++i, ++arg) {}
							if (i != 16) throw std::runtime_error("Transform argument should be followed by 16 values");

							auto sec = ldr::Section(m_str, "*Transform");
							ldr::Append(m_str, "*m4x4 { ", m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7], m[8], m[9], m[10], m[11], m[12], m[13], m[14], m[15], "}");
							break;
						}

						// NEW_COMMAND
						throw std::runtime_error(FmtS("Unknown command line option: %S", option.c_str()));
					}
					return true;
				}
			};

			Parser p;
			EnumCommandLine(args.c_str(), p);
			if (p.m_str.empty())
				throw std::runtime_error("Invalid command line");

			// Create a string source
			m_base_dir = current_path();
			return std::unique_ptr<StringSrc>(new StringSrc(p.m_str, StringSrc::EFlags::BufferLocally));
		}
	}

	// Main program run
	int Run(std::string args)
	{
		//NEW_COMMAND - Test the new command
		//if (!args.empty()) printf("warning: debugging overriding arguments");
		//args = R("-input blah -msg "Type a value> ")";
		//args = R("-shcopy "c:/deleteme/SQ.bin,c:/deleteme/TheList.txt" "c:/deleteme/cexitime/" -title "Testing shcopy")";
		//args = R"(-clip -lwr -bkslash "C:/blah" "Boris" "F:\\Jef/wan")";
		//args = R"(-p3d -fi "\dump\test.3ds" -verbosity 5 -remove_degenerates 4096 -gen_normals 40)";
		//args = R"(-p3d -fi R:\localrepo\PC\vrex\res\pelvis.3ds -verbosity 3 -remove_degenerates 16384 -gen_normals 40)";

		try
		{
			// Get the script source from the command line
			auto src = ParseCommandLine(args);
			if (src == nullptr)
			{
				ShowHelp();
				return -1;
			}

			// Execute the script
			script::Reader reader(*src);
			for (char kw[32]; reader.NextKeywordS(kw);)
			{
				if (str::EqualI(kw, "verbosity"))
				{
					reader.IntS(m_verbosity, 10);
					continue;
				}
				if (str::EqualI(kw, "fi"))
				{
					ImportFile(reader);
					continue;
				}
				if (str::EqualI(kw, "fo"))
				{
					ExportFile(reader);
					continue;
				}
				if (str::EqualI(kw, "RemoveDegenerates"))
				{
					RemoveDegenerates(reader);
					continue;
				}
				if (str::EqualI(kw, "GenerateNormals"))
				{
					GenerateNormals(reader);
					continue;
				}
				if (str::EqualI(kw, "Transform"))
				{
					Transform(reader);
					continue;
				}
				// NEW_COMMAND
				throw std::runtime_error(FmtS("Unknown command: %s (line: %d)", kw, reader.Location().Line()));
			}
		}
		catch (std::exception const& ex)
		{
			std::wcerr << ex.what() << std::endl;
			return -1;
		}

		// Run the script
		return 0;
	}

	// Import a geometry model file
	void ImportFile(script::Reader& reader)
	{
		// Read the file name
		std::string infile;
		reader.StringS(infile);

		// Resolve the file
		m_infile = path(infile);
		m_infile = m_infile.is_relative() ? m_base_dir / m_infile : m_infile;

		// Import the file
		if (!exists(m_infile))
		{
			if (m_verbosity >= 1)
				std::cout << "Could not locate '" << infile << "'. Does the file exist?" << std::endl;

			m_model = nullptr;
		}
		else
		{
			if (m_verbosity >= 1)
				std::cout << "Loading '" << m_infile << "'." << std::endl;

			auto extn = m_infile.extension().string();
			m_model =
				str::EqualI(extn, ".p3d") ? CreateFromP3D(m_infile) :
				str::EqualI(extn, ".3ds") ? CreateFrom3DS(m_infile) :
				str::EqualI(extn, ".stl") ? CreateFromSTL(m_infile) :
				nullptr;
			if (m_model == nullptr)
				throw std::runtime_error("Model format '"s + extn + "' is not supported");
		}
	}

	// Export a p3d model file
	void ExportFile(script::Reader& reader) const
	{
		// Generate an output filepath based on 'infile'
		std::string outfile, extn = "p3d";
		if (reader.IsSectionStart())
		{
			// Parse the optional *fo section
			reader.SectionStart();

			// If a filepath is given, read it
			if (!reader.IsKeyword())
				reader.String(outfile);

			// Parse optional keywords
			for (char kw[32]; reader.NextKeywordS(kw);)
			{
				if (str::EqualI(kw, "Code"))
				{
					extn = "cpp";
					continue;
				}
			}

			reader.SectionEnd();
		}

		// If there is no model, then there's nothing to export. (We still need to parse the script tho)
		if (m_model == nullptr)
			return;

		// Resolve the output file path
		auto outpath =
			outfile.empty() ? path(m_infile).replace_extension(extn) :
			path(outfile).is_relative() ? m_base_dir / outfile :
			path(outfile);

		if (m_verbosity >= 1)
			std::cout << "Writing '" << outpath << "'..." << std::endl;

		// Ensure the output directory exists
		if (!exists(outpath.parent_path()) && !create_directories(outpath.parent_path()))
			throw std::runtime_error("Failed to create directory: "s + outpath.parent_path().string());

		// Determine the output format from the extn
		extn = outpath.extension().string();
		str::EqualI(extn, ".p3d") ? WriteP3d(m_model, outpath) :
		str::EqualI(extn, ".cpp") ? WriteCpp(m_model, outpath, "\t") :
		throw std::runtime_error("Unsupported output file format: "s + extn);

		if (m_verbosity >= 1)
			std::cout << "'" << outpath << "' saved." << std::endl;
	}

	// Remove degenerate verts from the model
	void RemoveDegenerates(script::Reader& reader) const
	{
		if (m_model == nullptr)
			return;

		auto quantisation = 10;
		auto normal_smoothing_angle = -1.0f;
		auto colour_distance = -1.0f;
		auto uv_distance = -1.0f;

		// Read parameters
		reader.SectionStart();
		for (char kw[32]; reader.NextKeywordS(kw);)
		{
			if (str::EqualI(kw, "Quantisation"))
			{
				reader.IntS(quantisation, 10);
				continue;
			}
			if (str::EqualI(kw, "NormalSmoothingAngle"))
			{
				reader.RealS(normal_smoothing_angle);
				continue;
			}
			if (str::EqualI(kw, "ColourDistance"))
			{
				reader.RealS(colour_distance);
				continue;
			}
			if (str::EqualI(kw, "UVDistance"))
			{
				reader.RealS(uv_distance);
				continue;
			}
		}
		reader.SectionEnd();

		// Remove the degenerates
		RemoveDegenerateVerts(*m_model, quantisation, normal_smoothing_angle, colour_distance, uv_distance, m_verbosity);
	}

	// Generate normals for the model
	void GenerateNormals(script::Reader& reader) const
	{
		if (m_model == nullptr)
			return;

		auto smoothing_angle = 10.0f;

		// Read parameters
		reader.SectionStart();
		for (char kw[32]; reader.NextKeywordS(kw);)
		{
			if (str::EqualI(kw, "SmoothingAngle"))
			{
				reader.RealS(smoothing_angle);
				continue;
			}
		}
		reader.SectionEnd();

		// Remove the degenerates
		GenerateVertNormals(*m_model, smoothing_angle, m_verbosity);
	}

	// Apply a transform to the model
	void Transform(script::Reader& reader) const
	{
		if (m_model == nullptr)
			return;

		// Read the object to world transform
		auto o2w = m4x4Identity;
		reader.TransformS(o2w);
		
		// Create a normals to world transform
		auto n2w = o2w;
		n2w.x = Normalise(n2w.x);
		n2w.y = Normalise(n2w.y);
		n2w.z = Normalise(n2w.z);

		if (m_verbosity >= 2)
			std::cout << "  Applying transform to model" << std::endl;
		if (m_verbosity >= 3)
			std::cout << "    Position transform: " << o2w << std::endl;
		if (m_verbosity >= 3)
			std::cout << "    Normal transform: " << n2w << std::endl;

		for (auto& mesh : m_model->m_scene.m_meshes)
		{
			auto bbox = BBoxReset;
			for (auto& vert : mesh.m_verts)
			{
				vert.pos = o2w * static_cast<v4>(vert.pos);
				vert.norm = n2w * static_cast<v4>(vert.norm);
				Encompass(bbox, vert.pos);
			}
			mesh.m_bbox = bbox;
		}
	}
};

int __cdecl wmain(int argc, wchar_t* argv[])
{
	Main m;
	std::string args;
	for (int i = 1; i < argc; ++i) args.append(Narrow(argv[i])).append(" ");
	return m.Run(args);
}