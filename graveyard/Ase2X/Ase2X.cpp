//********************************************************************
//
//	An 3DS-Max "ASC" to "DirectX" file converter
//
//********************************************************************

#ifndef NDEBUG
#include <conio.h>
#endif//NDEBUG
#include "Ase2X.h"
#include "Common\Fmt.h"
#include "Geometry\AseLoader\AseLoader.h"
#include "Geometry\XSaver\XSaver.h"
#include "Geometry\PRGeometry.h"
#include "Geometry\GeometryManipulator\GeometryManipulator.h"

int main(int argc, char* argv[])
{
	Ase2X ase2x;
	if( !ase2x.ParseOptions(argc, argv) )
	{
		ase2x.ShowHelp();
		return -1;
	}

	ase2x.Convert();
	PR_DEBUG_ONLY(getch();)
	return 0;
}

//********************************************************************
// Ase2X implementation
//*****
// Confuse people
void Ase2X::ShowHelp() const
{
	printf(	"Ase2X - ASE file to X file converter\n"
			"\n"
			"Usage: Ase2X [options] file.ase\n"
			"Options:\n"
			"   -O filename - specify an output filename\n"
			"   -V          - verbose output\n"
			"   -G          - generate normals\n"
			"   -GT x       - set the geometry type to 'x'\n"
			"                       0 = Vertex only\n"
			"                       1 = VertexRHW only\n"
			"                       2 = Vertex and normal\n"
			"                       3 = Vertex and colour\n"
			"                       4 = VertexRHW and colour\n"
			"                       5 = Vertex, normal, and colour\n"
			"                       6 = Vertex and texture\n"
			"                       7 = Vertex, normal, and texture\n"
			"                       8 = Vertex, colour, and texture\n"
			"                       9 = Vertex, normal, colour, and texture\n"	
			);
}

//*****
// Read options
bool Ase2X::ParseOptions(int argc, char* argv[])
{
	// Set up the default options
	m_verbose			= false;
	m_generate_normals	= false;
	m_geometry_type		= PR::GeometryType::Invalid;

	bool parse_success = argc > 1;
	for( int arg = 1; arg < argc; ++arg )
	{
		// Output filename
		if( stricmp(argv[arg], "-O") == 0 )
		{
			++arg;
			m_output_filename = argv[arg];
		}
		// Up the wordiness
		else if( stricmp(argv[arg], "-V") == 0 )
		{
			m_verbose = true;
		}
		// Generate normals
		else if( stricmp(argv[arg], "-G") == 0 )
		{
			m_generate_normals = true;
		}
		// Set the geometry type
		else if( stricmp(argv[arg], "-GT") == 0 )
		{
			++arg;
			m_geometry_type = strtoul(argv[arg], NULL, 10);
			if( !PR::GeometryType::IsValid(m_geometry_type) )
			{
				Error(Fmt("Geometry type = %d is invalid. Geometry type ignored\n", m_geometry_type));
				m_geometry_type = PR::GeometryType::Invalid;
			}
		}
		// Assume source filename
		else
		{
			m_source_filename = argv[arg];

			// Use 'argv[arg]' for the output filename if none has been given
			if( m_output_filename.length() == 0 )
			{
                m_output_filename = argv[arg];
				while( m_output_filename.length() > 0 && m_output_filename.Last() != '.' ) m_output_filename.Shorten(1);
				m_output_filename += "x";
			}
			break;
		}
	}
	return parse_success;
}

//*****
// Convert the ASE file to an X file
bool Ase2X::Convert()
{
	PR::Geometry geometry;
	AseLoader ase_loader;
	Info(Fmt("Loading: %s...", m_source_filename.c_str()));
	if( Failed(ase_loader.Load(m_source_filename.c_str(), geometry)) ) { return Error("Failed to load the ASE file.\n"); }
	Info("Done.\n");
	
	// Do the optional stuff
	geometry.m_name = m_output_filename;
	if( m_generate_normals )
	{
		PR::GeometryManipulator manipulator;
		for( DWORD f = 0; f < geometry.m_frame.GetCount(); ++f )
		{
			Info("Generating Normals...");
			manipulator.GenerateNormals(geometry.m_frame[f].m_mesh);
			Info("Done.\n");
		}
	}
	if( m_geometry_type != PR::GeometryType::Invalid )
	{
		for( DWORD f = 0; f < geometry.m_frame.GetCount(); ++f )
		{
			geometry.m_frame[f].m_mesh.m_geometry_type = m_geometry_type;
		}
	}

	XSaver xsaver;
	Info(Fmt("Saving: %s...", geometry.m_name.c_str()));
	if( Failed(!xsaver.Save(geometry)) )		return Error("Failed to write X File.\n");
	return Msg("Export done.\n");
}

//*************************************
// Private methods
//*****
// Display stuff.
bool Ase2X::Error(const char* str)
{
	printf("%s", str);
	return false;
}
bool Ase2X::Info(const char* str)
{
	if( m_verbose ) printf("%s", str);
	return false;
}
bool Ase2X::Msg(const char* str)
{
	printf("%s", str);
	return false;
}