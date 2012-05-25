//*********************************************
// File Loader
//	(C)opyright Rylogic Limited 2007
//*********************************************
#ifndef FILELOADER_H
#define FILELOADER_H

#include "pr/common/StdVector.h"
#include "pr/common/PRString.h"
#include "pr/common/PollingToEvent.h"
#include "pr/filesys/filesys.h"

struct LdrFile
{
	explicit	LdrFile(char const* name);
	bool		GetData(std::string& data) const;
	std::string			m_name;
	pr::filesys::uint64	m_last_modified;
};
inline bool operator == (LdrFile const& lhs, LdrFile const& rhs) { return lhs.m_name == rhs.m_name; }
typedef std::vector<LdrFile> TLdrFileVec;

struct FileLoader
{
	FileLoader();
	~FileLoader();
	void			ClearSource();
	void			ClearWatchFiles();
	void			AddSource(const char* filename);
	void			SetSource(const char* filename);
	void			AddFileToWatch(const char* filename);
	bool			AreAnyFilesModified() const;
	bool			AreAnyFilesLocked() const;
	const char*		GetCurrentFilename() const;
	void			SetAutoRefresh(bool on);
	
	TLdrFileVec		m_file;						// The files to load
	TLdrFileVec		m_watch;					// Files to watch for changes
	uint			m_auto_refresh_time_ms;
	bool			m_auto_recentre;
	bool			m_refresh_pending;
	PollingToEvent	m_auto_refresh_poller;
};

#endif//FILELOADER_H