//****************************************************************
// FileWatch
//  Copyright (c) Rylogic Ltd 2010
//****************************************************************

#pragma once

#include <vector>
#include "pr/common/guid.h"
#include "pr/filesys/fileex.h"
#include "pr/filesys/filesys.h"
#include "pr/str/string.h"

namespace pr
{
	// Note about worker threads:
	// It's tempting to try and make this type a worker thread that notifies the client when
	// a file has changed. However this requires cross-thread marshalling which is only possible
	// if the client has a message queue. There are three possibilities;
	//  1) the client is a window - could use SendMessage() to notify the client (SendMessage
	//     marshals across threads) however it doesn't make sense for the FileWatch type to
	//     require a windows handle
	//  2) use PostThreadMessage - this has synchronisation problems i.e. notifications occur for
	//     all changed files plus the filename cannot be passed to the client without allocation
	//  3) use a custom message queue system - this would require the client to poll their message
	//     queue, in which case they might as well just poll the FileWatch object.

	// Receives notification of files changed
	struct IFileChangedHandler
	{
		virtual ~IFileChangedHandler() {}

		// 'filepath' is the name of the changed file
		// 'handled' should be set to false if the file should be reported as changed the next time 'CheckForChangedFiles' is called (true by default)
		virtual void FileWatch_OnFileChanged(wchar_t const* filepath, void* user_data, bool& handled) = 0;
	};

	class FileWatch
	{
		using string = pr::string<wchar_t>;

		struct File
		{
			pr::string<wchar_t>   m_filepath;  // The file to watch
			pr::filesys::FileTime m_time;      // The last modified time stats
			IFileChangedHandler*  m_onchanged; // The client to callback when a changed file is found
			pr::Guid              m_id;        // A user provided id used to identify groups of watched files
			void*                 m_user_data; // User data to provide in the callback
		
			File(pr::string<wchar_t> const& filepath, IFileChangedHandler* onchanged, pr::Guid const& id, void* user_data)
				:m_filepath(filepath)
				,m_time(pr::filesys::FileTimeStats(filepath))
				,m_onchanged(onchanged)
				,m_id(id)
				,m_user_data(user_data)
			{}

			bool operator == (string const& filepath) const { return pr::str::EqualI(m_filepath, filepath); }
			bool operator == (pr::Guid const& id) const     { return m_id == id; }
		};

		using FileCont = std::vector<File>;
		FileCont m_files; // The files being watched

	public:
		FileWatch()
			:m_files()
		{}

		// Add a file to be watched
		void Add(wchar_t const* filepath, IFileChangedHandler* onchanged, pr::Guid const& id, void* user_data)
		{
			Remove(filepath);
			auto fpath = pr::filesys::Standardise<string>(filepath);
			m_files.emplace_back(fpath, onchanged, id, user_data);
		}

		// Remove a watched file
		void Remove(wchar_t const* filepath)
		{
			auto fpath = pr::filesys::Standardise<string>(filepath);
			auto i = std::find(std::begin(m_files), std::end(m_files), fpath);
			if (i != std::end(m_files)) m_files.erase(i);
		}

		// Remove all watches matching 'user_data'
		void RemoveAll(pr::Guid const& id)
		{
			auto end = std::remove_if(std::begin(m_files), std::end(m_files), [&](File const& file) { return file.m_id == id; });
			m_files.erase(end, std::end(m_files));
		}

		// Check the timestamps of all watched files and call the callback for those that have changed.
		void CheckForChangedFiles()
		{
			// Build a collection of the changed files to prevent reentrancy problems with the callbacks
			FileCont changed_files;
			for (auto& file : m_files)
			{
				auto stamp = pr::filesys::FileTimeStats(file.m_filepath);
				if (file.m_time.m_last_modified != stamp.m_last_modified) changed_files.push_back(file);
				file.m_time = stamp;
			}

			// Report each changed file
			for (auto& file : changed_files)
			{
				bool handled = true;
				file.m_onchanged->FileWatch_OnFileChanged(file.m_filepath.c_str(), file.m_user_data, handled);

				// If the change is not handled, find the same file in 'm_files'
				// and set it's timestamp back to the previous value (should be a rare case)
				if (!handled)
				{
					auto iter = std::find(std::begin(m_files), std::end(m_files), file.m_filepath);
					if (iter != std::end(m_files)) iter->m_time = file.m_time;
				}
			}
		}
	};
}
