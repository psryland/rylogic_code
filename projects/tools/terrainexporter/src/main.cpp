//*******************************************************************************
// Terrain Exporter
//	(c)opyright Rylogic Limited 2009
//*******************************************************************************
#pragma once
#include <vector>
#include <string>
#include "pr/common/commandline.h"
#include "pr/terrain/exporter/terrainexporter.h"

struct Main :pr::cmdline::IOptionReceiver
{
	typedef std::vector<std::string> TFiles;
	TFiles m_files;

	int Run(int argc, char* argv[])
	{
		if (!EnumCommandLine(argc, argv, *this))	{ ShowHelp(); return -1; }
		
		TerrainExporter te;
		te.
	}

	void ShowHelp()
	{
		printf(	"\n"
				"*****************************************************\n"
				" --- Terrain Exporter - (c)opyright Rylogic 2009 --- \n"
				"*****************************************************\n"
				"\n"
				"  Syntax: TerrainExporter -origin minx minz -region sizex sizez -divisions divx divz -o output.terrain -i file1.x -i file2.x ... \n"
				"	-origin minx minz : the origin of the terrain region (i.e. the minx,minz corner)\n"
				"	-region sizex sizez : the size of the terrain region\n"
				"	-divisions divx divz : the number of divisions in the x and z directions\n"
				"	-o file.terrain : the name of the file to output\n"
				"	-i file.x : a source terrain xfile
				"\n"
				);
	}
};

int main(int argc, char* argv[])
{
	Main m;
	return m.Run(argc, argv);
}