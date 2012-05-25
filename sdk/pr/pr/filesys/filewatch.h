//****************************************************************
// FileWatch
//  Copyright © Rylogic Ltd 2010
//****************************************************************
// Usage:
//	
#pragma once
#ifndef PR_FILESYS_FILEWATCH_H
#define PR_FILESYS_FILEWATCH_H

#include <vector>
#include "pr/str/prstring.h"
#include "pr/filesys/fileex.h"
#include "pr/filesys/filesys.h"

namespace pr
{
	// Note about worker threads:
	// It's tempting to try and make this type a worker thread that notifies the client when
	// a file has changed. However this requires cross-thread marshelling which is only possible
	// if the client has a message queue. There are three possibilities;
	//	1) the client is a window - could use SendMessage() to notify the client (SendMessage
	//		marshalls across threads) however it doesn't make sense for the FileWatch type to
	//		require a windows handle
	//	2) use PostThreadMessage - this has synchronisation problems i.e. notifications occur for
	//		all changed files plus the filename cannot be passed to the client without allocation
	//	3) use a custom message queue system - this would require the client to poll their message
	//		queue, in which case they might as well just poll the FileWatch object.

	class FileWatch
	{
	public:
		struct IFileChangedHandler
		{
			// 'filepath' is the name of the changed file
			// 'handled' should be set to false if the file should be reported as changed the next time 'CheckForChangedFiles' is called (true by default)
			virtual void FileWatch_OnFileChanged(char const* filepath, void* user_data, bool& handled) = 0;
			virtual ~IFileChangedHandler() {}
		};

	private:
		struct File
		{
			std::string				m_filepath;		// The file to watch
			pr::filesys::FileTime	m_time;			// The last modified time stats
			IFileChangedHandler*	m_onchanged;	// The client to callback when a changed file is found
			pr::uint				m_id;			// A user provided id used to identify groups of watched files
			void*					m_user_data;	// User data to provide in the callback
		
			File(std::string filepath, IFileChangedHandler* onchanged, pr::uint id, void* user_data)
			:m_filepath(filepath)
			,m_time(pr::filesys::GetFileTimeStats(filepath))
			,m_onchanged(onchanged)
			,m_id(id)
			,m_user_data(user_data)
			{}
			bool operator == (std::string const& filepath) const	{ return pr::str::EqualI(m_filepath, filepath); }
			bool operator == (pr::uint id) const					{ return m_id == id; }
		};
		typedef std::vector<File> FileCont;
		FileCont m_files; // The files being watched

	public:
		FileWatch()
		:m_files()
		{}

		// Add a file to be watched
		void Add(char const* filepath, IFileChangedHandler* onchanged, pr::uint id, void* user_data)
		{
			Remove(filepath);
			std::string fpath = pr::filesys::StandardiseC<std::string>(filepath);
			m_files.push_back(File(fpath, onchanged, id, user_data));
		}

		// Remove a watched file
		void Remove(char const* filepath)
		{
			std::string fpath = pr::filesys::StandardiseC<std::string>(filepath);
			FileCont::iterator i = std::find(m_files.begin(), m_files.end(), fpath);
			if (i != m_files.end()) m_files.erase(i);
		}

		// Remove all watches matching 'user_data'
		void RemoveAll(pr::uint id)
		{
			struct MatchId
			{
				pr::uint m_id;
				MatchId(pr::uint id) :m_id(id) {}
				bool operator ()(File const& file) const {return file.m_id == m_id;}
			};
			m_files.erase(std::remove_if(m_files.begin(), m_files.end(), MatchId(id)), m_files.end());
		}

		// Check the timestamps of all watched files and call the callback for those that have changed.
		void CheckForChangedFiles()
		{
			// Build a collection of the changed files to prevent re-entrancy problems with the callbacks
			FileCont changed_files;
			for (FileCont::iterator i = m_files.begin(), iend = m_files.end(); i != iend; ++i)
			{
				pr::filesys::FileTime stamp = pr::filesys::GetFileTimeStats(i->m_filepath);
				if (i->m_time.m_last_modified != stamp.m_last_modified) { changed_files.push_back(*i); }
				i->m_time = stamp;
			}

			// Report each changed file
			for (FileCont::iterator i = changed_files.begin(), iend = changed_files.end(); i != iend; ++i)
			{
				bool handled = true;
				i->m_onchanged->FileWatch_OnFileChanged(i->m_filepath.c_str(), i->m_user_data, handled);
				
				// If the change is not handled, find the same file in 'm_files'
				// and set it's timestamp back to the previous value (should be a rare case)
				if (!handled)
				{
					FileCont::iterator j = std::find(m_files.begin(), m_files.end(), i->m_filepath);
					if (j != m_files.end()) j->m_time = i->m_time;
				}
			}
		}
	};
}

#endif
