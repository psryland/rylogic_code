//********************************
// ImagerN
//  Copyright © Rylogic Ltd 2011
//********************************
#pragma once
#ifndef IMAGERN_MEDIA_FILE_H
#define IMAGERN_MEDIA_FILE_H

#include "imagern/main/forward.h"

namespace EMedia
{
	enum Type
	{
		Unknown,
		Image,
		Video,
		Audio,
	};
}

// A media filepath and associated file properties
struct MediaFile
{
	string m_path;
	time_t m_timestamp;
	
	PR_SQLITE_TABLE(MediaFile, "")
	PR_SQLITE_COLUMN(Path      ,m_path      ,text    ,"primary key")
	PR_SQLITE_COLUMN(Timestamp ,m_timestamp ,integer ,"")
	PR_SQLITE_TABLE_END()
	
	string File() const { return pr::filesys::GetFilename(m_path); }
	string Dir() const  { return pr::filesys::GetDirectory(m_path); }
	string Extn() const { return pr::filesys::GetExtension(m_path); }
	
	MediaFile(string const& path, long long timestamp) :m_path(path) ,m_timestamp(timestamp) {}
	MediaFile(string const& path)
	{
		m_path = pr::filesys::StandardiseC(pr::filesys::GetFullPath(path));
		m_timestamp = pr::filesys::GetFileTimeStats(m_path).m_created;


		// Use this: WNetAddConnection3 to log onto network shares
	}
};
inline bool operator == (MediaFile const& lhs, MediaFile const& rhs)
{
	return lhs.m_path == rhs.m_path && lhs.m_timestamp == rhs.m_timestamp;
}


// A directory to include in the search for media files
struct SearchDir
{
	string m_path;
	
	PR_SQLITE_TABLE(SearchDir, "")
	PR_SQLITE_COLUMN(Path   ,m_path    ,text   ,"primary key")
	PR_SQLITE_TABLE_END()
};

#endif
