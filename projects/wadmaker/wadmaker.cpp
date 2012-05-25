//************************************************************************
//
//	Wad Maker
//
//************************************************************************
// A command line tool making Wad files

#include <list>
#include <algorithm>
#include "pr/common/assert.h"
#include "pr/common/command_line.h"
#include "pr/common/exception.h"
#include "pr/common/fmt.h"
#include "pr/macros/link.h"
#include "pr/crypt/crypt.h"
#include "pr/str/prstring.h"
#include "pr/filesys/directorytree.h"
#include "pr/filesys/fileex.h"
#include "pr/filesys/filesys.h"
#include "pr/storage/nugget_file/nuggetfile.h"

using namespace pr;
using namespace pr::cmdline;
using namespace pr::filesys;
using namespace pr::nugget;

class Main : public IOptionReceiver
{
public:
	Main()
	:m_nuggets()
	,m_root()
	,m_root_directory_length(0)
	,m_output_filename("")
	,m_verbose(false)
	,m_silent(false)
	{}
	bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end)
	{
		if( str::EqualI(option, "-D") && arg != arg_end )	{ m_directory = *arg++;			return true; }
		if( str::EqualI(option, "-O") && arg != arg_end )	{ m_output_filename = *arg++;	return true; }
		if( str::EqualI(option, "-M") && arg != arg_end )	{ m_masks.push_back(*arg++);	return true; }
		if( str::EqualI(option, "-V") )					{ m_verbose = true;				return true; }
		if( str::EqualI(option, "-S") )					{ m_silent = true;				return true; }
		printf("Error: Unknown option '%s'\n", option.c_str());
		ShowHelp();
		return false;
	}
	void ShowHelp()
	{
		printf("\n"
			   "***************************************************\n"
			   " --- Wad File Maker - Copyright © Rylogic 2005 --- \n"
			   "***************************************************\n"
			   "\n"
			   "  Syntax: WadMaker -D 'DirectoryRoot' -O 'WadFilename' [-M *.txt] [-V]\n"
			   "    -D : Root directory path\n"
			   "    -O : Output filename\n"
			   "    -M : Wildcard Mask (more than one of these is allowed)\n"
			   "    -V : Verbose\n"
			   "    -S : Silent mode. Only outputs error messages\n"
			   );
	}

	//*****
	// Main program run
	int run(int argc, char* argv[])
	{
		if( !EnumCommandLine(argc, argv, *this) )	{ ShowHelp(); return -1; }
		if( m_output_filename.empty() )				{ printf("Output filename not provided\n"); ShowHelp(); return -1; }
		if( m_directory.empty() )					{ printf("Source directory not provided\n"); ShowHelp(); return -1; }

		// If the output file exists, calculate a CRC for it.
		CRC output_crc = 0;
		if( pr::filesys::FileExists(m_output_filename) )
		{
			output_crc = crypt::CrcFile(m_output_filename.c_str());
		}

		// Build the tree
		BuildDirectoryTree(m_directory, ERecurse_Recurse, m_root, m_masks);

		// Add a nugget for each file
		m_root_directory_length = (uint)m_root.m_name.length() + 1;
		AddFiles(m_root);

		// Save the nugget file out to a temporary filename
		std::string temp_output_filename = pr::filesys::MakeUniqueFilename<std::string>("WadFileTmp_XXXXXX");
		if( Failed(Save(temp_output_filename.c_str(), m_nuggets.begin(), m_nuggets.end())) )
		{
			printf("Failed to create Wad file.\nReason: Failed to save Wad file: '%s'", temp_output_filename.c_str());
			return -1;
		}

		// Take a crc of the temporary file
		CRC new_output_crc = crypt::CrcFile(temp_output_filename.c_str());

		// If the crc's are different update the output file, otherwise remove the temporary file
		if( new_output_crc != output_crc )
		{
			pr::filesys::EraseFile(m_output_filename);
			pr::filesys::RenameFile(temp_output_filename, m_output_filename);
			if( !m_silent ) { printf("Wad file '%s' created successfully\n", m_output_filename.c_str()); }
		}
		else//Same as the old file...
		{
			pr::filesys::EraseFile(temp_output_filename);
			if( m_verbose ) { printf("No changes detected. Wad file '%s' unchanged\n", m_output_filename.c_str()); }
		}
		return 0;
	}

	//*****
	// Recursive function for adding nuggets
	void AddFiles(const DirTree& directory)
	{
		// Add the files in this directory
		for( TFileVec::const_iterator i = directory.m_file.begin(), i_end = directory.m_file.end(); i != i_end; ++i )
		{
			AddFile(i->m_name);
		}

		// Add the files from each of the sub directories
		for( TDirectoryVec::const_iterator d = directory.m_sub_dir.begin(), d_end = directory.m_sub_dir.end(); d != d_end; ++d )
		{
			AddFiles(*d);
		}
	}

	//*****
	// Add a single nugget
	void AddFile(const std::string& filename)
	{
		// Get the filename minus the root directory path
		std::string name;
		std::copy(filename.begin() + m_root_directory_length, filename.end(), std::back_inserter(name));
		name		 = pr::filesys::Standardise(name);
		uint name_id = Crc(name.c_str(), (uint)name.length());
		
		if( m_verbose ) { printf("Added: (%8.8x) %s\n", name_id, name.c_str()); }

		Nugget nug(name_id, 1000, 0, name.c_str());
		nug.SetData(filename.c_str(), ECopyFlag_Reference);
		m_nuggets.push_back(nug);
	}

private:
	typedef std::list<Nugget> TNuggets;
	TNuggets			m_nuggets;
	DirTree				m_root;
	std::string			m_directory;
	uint				m_root_directory_length;
	std::string			m_output_filename;

	filesys::TMasks	m_masks;
	bool				m_verbose;
	bool				m_silent;
};

// Entry point
int main(int argc, char* argv[])
{
	Main m;
	return m.run(argc, argv);
}



