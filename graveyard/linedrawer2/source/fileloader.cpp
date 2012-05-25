//*******************************************************************************************
//
// File Loader - Loads a txt file into memory and passes it to the string parser
//
//*******************************************************************************************
#include "Stdafx.h"
#include "pr/common/PollingToEvent.h"
#include "pr/filesys/fileex.h"
#include "pr/filesys/filesys.h"
#include "pr/geometry/geometry.h"
#include "pr/storage/xfile/XFile.h"
#include "LineDrawer/Resource.h"
#include "LineDrawer/Source/LineDrawer.h"
#include "LineDrawer/Source/FileLoader.h"

using namespace pr::ldr;

// Polling function used to auto refresh
bool AutoRefreshPollingFunction(void* user)
{
	FileLoader* file_loader = static_cast<FileLoader*>(user);
	if( !file_loader->m_refresh_pending )
	{
		file_loader->m_refresh_pending = true;	
		PostMessage(LineDrawer::Get().m_window_handle, WM_COMMAND, ID_AUTO_REFRESH_FROM_FILE, 0);
	}
	return false;
}

// Return settings for the file loader poller
PollingToEventSettings FileLoaderPollerSettings(void* user_data, unsigned int poll_freq_ms)
{
	PollingToEventSettings settings;
	settings.m_polling_function		= AutoRefreshPollingFunction;
	settings.m_polling_frequency	= poll_freq_ms;
	settings.m_user_data			= user_data;
	return settings;
}

// Generate a string for loading the frames of x files
std::string GenerateXFileLDString(const std::string& filename)
{
	std::string xfile_string;

	Geometry geometry;
	xfile::TGUIDSet load_set;
	load_set.insert(TID_D3DRMFrame);
	load_set.insert(TID_D3DRMFrameTransformMatrix);
	if( Succeeded(xfile::Load(filename.c_str(), geometry, &load_set)) )
	{
		std::string title = pr::filesys::GetFilename(filename);
		str::Replace(title, " ", "_");

		xfile_string = "*Group " + title + " FFFFFFFF\n{\n";
		for( std::size_t f = 0; f < geometry.m_frame.size(); ++f )
		{
			Frame& frame = geometry.m_frame[f];
			std::string frame_name = frame.m_name;
			str::Replace(frame_name, " ", "_");

			xfile_string += Fmt(
				"\t*File frame_%s FFFFFFFF\n"
				"\t{\n"
					"\t\t*Frame %d\n"
					"\t\t*Transform { %f %f %f %f  %f %f %f %f  %f %f %f %f  %f %f %f %f }\n"
					"\t\t%s\n"
				"\t}\n",
				frame_name.c_str(),
				f,
				frame.m_transform[0][0], frame.m_transform[0][1], frame.m_transform[0][2], frame.m_transform[0][3],
				frame.m_transform[1][0], frame.m_transform[1][1], frame.m_transform[1][2], frame.m_transform[1][3],
				frame.m_transform[2][0], frame.m_transform[2][1], frame.m_transform[2][2], frame.m_transform[2][3],
				frame.m_transform[3][0], frame.m_transform[3][1], frame.m_transform[3][2], frame.m_transform[3][3],
				pr::filesys::AddQuotes(filename).c_str());
		}
		xfile_string += "}\n";
	}
	else
	{
		xfile_string = "*File file FFFFFFFF { " + pr::filesys::AddQuotes(filename) + " }\n";
	}
	return xfile_string;
}

// LdrFile **************************************

// Constructor
LdrFile::LdrFile(const char* name)
:m_name			(pr::filesys::Standardise<std::string>(name))
,m_last_modified(pr::filesys::GetFileTimeStats(m_name).m_last_modified)
{}

// Read the contents of the file into 'data'
bool LdrFile::GetData(std::string& data) const
{
	std::string extn = pr::filesys::GetExtension(m_name);

	// Special case x files
	if( str::EqualNoCase(extn, "x") )
	{
		data += GenerateXFileLDString(m_name);
		return true;
	}

	// Special case ase files
	if( str::EqualNoCase(extn, "ase") )
	{
		std::string filename = pr::filesys::AddQuotes(m_name);
		data += "*File file FFFFFFFF { " + filename + " }\n";
		return true;
	}
	
	// Read the file data into 'data'
	return FileToBuffer(m_name.c_str(), data);
}

// FileLoader *************************************

// Constructor
FileLoader::FileLoader()
:m_file()
,m_watch()
,m_auto_refresh_time_ms(100)
,m_auto_recentre(false)
,m_refresh_pending(false)
,m_auto_refresh_poller(FileLoaderPollerSettings(this, m_auto_refresh_time_ms))
{}

// Destructor
FileLoader::~FileLoader()
{
	m_auto_refresh_poller.Stop();
	ClearSource();
	ClearWatchFiles();
}

// Clear the list of source files
void FileLoader::ClearSource()
{
	m_file.clear();
}

// Clear the list of files to watch for changes
void FileLoader::ClearWatchFiles()
{
	m_watch.clear();
}

// Add a filename to load from
void FileLoader::AddSource(const char* filename)
{
	LdrFile file(filename);
	TLdrFileVec::const_iterator iter = std::find(m_file.begin(), m_file.end(), file);
	if( iter == m_file.end() ) m_file.push_back(file);

	AddFileToWatch(filename);
}

// Set the filename to load from
void FileLoader::SetSource(const char* filename)
{
	ClearSource();
	ClearWatchFiles();
	AddSource(filename);
}

// Add a file to watch for modification
void FileLoader::AddFileToWatch(const char* filename)
{
	LdrFile file(filename);
	TLdrFileVec::const_iterator iter = std::find(m_watch.begin(), m_watch.end(), file);
	if( iter == m_watch.end() ) m_watch.push_back(file);
}

// Returns true if any of the files we reference have been updated
bool FileLoader::AreAnyFilesModified() const
{
	for( TLdrFileVec::const_iterator f = m_watch.begin(), f_end = m_watch.end(); f != f_end; ++f )
	{
		if( pr::filesys::GetFileTimeStats(f->m_name).m_last_modified != f->m_last_modified ) { return true; }
	}
	return false;
}

// Returns true if any of the files we reference are currently locked
bool FileLoader::AreAnyFilesLocked() const
{
	for( TLdrFileVec::const_iterator f = m_watch.begin(), f_end = m_watch.end(); f != f_end; ++f )
	{
		if( !(pr::filesys::GetAccess(f->m_name) & pr::filesys::Read) )
		{ return true; }
	}
	return false;
}

// Return the current filename
const char* FileLoader::GetCurrentFilename() const
{
	return (!m_file.empty()) ? (m_file.front().m_name.c_str()) : ("");
}

// Toggle the auto refresh
void FileLoader::SetAutoRefresh(bool on)
{
	if( on ) { m_auto_refresh_poller.Start(); }
	else	 { m_auto_refresh_poller.Stop(); }
}
