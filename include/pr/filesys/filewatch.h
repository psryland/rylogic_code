//****************************************************************
// FileWatch
//  Copyright (c) Rylogic Ltd 2010
//****************************************************************
#pragma once

#include <vector>
#include <mutex>
#include <filesystem>
#include "pr/common/guid.h"
#include "pr/common/event_handler.h"
#include "pr/common/algorithm.h"
#include "pr/container/vector.h"
#include "pr/str/string.h"
#include "pr/threads/synchronise.h"

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
		virtual void FileWatch_OnFileChanged(wchar_t const* filepath, pr::Guid const& id, void* user_data, bool& handled) = 0;
	};

	class FileWatch
	{
	public:

		using path           = std::filesystem::path;
		using file_time_type = std::filesystem::file_time_type;

		// File time stamp info
		struct File
		{
			path                 m_filepath;  // The file to watch
			file_time_type       m_time;      // The last modified time stats
			IFileChangedHandler* m_onchanged; // The client to callback when a changed file is found
			pr::Guid             m_id;        // A user provided id used to identify groups of watched files
			void*                m_user_data; // User data to provide in the callback
		
			File() = default;
			File(path const& filepath, IFileChangedHandler* onchanged, pr::Guid const& id, void* user_data)
				:m_filepath(filepath)
				,m_time(std::filesystem::last_write_time(filepath))
				,m_onchanged(onchanged)
				,m_id(id)
				,m_user_data(user_data)
			{}

			bool operator == (path const& filepath) const
			{
				return std::filesystem::equivalent(m_filepath, filepath); 
			}
			bool operator == (pr::Guid const& id) const
			{
				return m_id == id; 
			}
		};
		struct FileCont :pr::vector<File>
		{};

	private:

		// The files being watched. Access via a Lock instance
		FileCont m_impl_files;
		std::mutex mutable m_mutex;

	public:

		FileWatch()
			:m_impl_files()
			,m_mutex()
		{}

		// Raised when changed files are detected. Allows modification of file list
		pr::EventHandler<FileWatch&, FileCont&> OnFilesChanged;

		// Synchronise access to the file container
		struct Lock :threads::Synchronise<FileWatch>
		{
			Lock(FileWatch const& fw)
				:base(fw, fw.m_mutex)
			{}

			// The files being watched
			FileCont const& files() const
			{
				return get().m_impl_files;
			}
			FileCont& files()
			{
				return get().m_impl_files;
			}
		};

		// Return the Guid associated with the given filepath (or GuidZero, if not being watched)
		pr::Guid FindId(path const& filepath) const
		{
			auto fpath = std::filesystem::canonical(filepath);
			
			Lock lock(*this);
			auto& files = lock.files();
			auto iter = pr::find(files, fpath);
			return iter != std::end(files) ? iter->m_id : pr::GuidZero;
		}

		// Mark a file as changed, to be caught on the next 'CheckForChangedFiles' call
		void MarkAsChanged(path const& filepath)
		{
			auto fpath = std::filesystem::canonical(filepath);
			
			Lock lock(*this);
			auto& files = lock.files();
			auto iter = pr::find(files, fpath);
			if (iter != std::end(files))
				iter->m_time -= std::chrono::seconds(10);
		}

		// Add a file to be watched
		void Add(std::filesystem::path const& filepath, IFileChangedHandler* onchanged, pr::Guid const& id, void* user_data = nullptr)
		{
			// Remove if already added
			Remove(filepath);

			// Add to the files collection
			Lock lock(*this);
			auto fpath = std::filesystem::canonical(filepath);
			lock.files().emplace_back(fpath, onchanged, id, user_data);
		}

		// Remove a watched file
		void Remove(std::filesystem::path const& filepath)
		{
			Lock lock(*this);
			auto fpath = std::filesystem::canonical(filepath);
			pr::erase_first(lock.files(), [&](File const& f){ return f == fpath; });
		}

		// Remove all watches where 'm_id == id'
		void RemoveAll(pr::Guid const& id)
		{
			Lock lock(*this);
			pr::erase_if(lock.files(), [=](File const& file) { return file.m_id == id; });
		}

		// Remove all watches
		void RemoveAll()
		{
			Lock lock(*this);
			lock.files().resize(0);
		}

		// Check the timestamps of all watched files and call the callback for those that have changed.
		void CheckForChangedFiles()
		{
			// Build a collection of the changed files to prevent reentrancy problems with the callbacks
			FileCont changed_files;
			{
				Lock lock(*this);
				for (auto& file : lock.files())
				{
					auto stamp = std::filesystem::last_write_time(file.m_filepath);
					if (file.m_time != stamp) changed_files.push_back(file);
					file.m_time = stamp;
				}
			}

			if (!changed_files.empty())
			{
				// Notify of detected changes and allow changes
				OnFilesChanged(*this, changed_files);

				// Report each changed file
				for (auto& file : changed_files)
				{
					bool handled = true;
					file.m_onchanged->FileWatch_OnFileChanged(file.m_filepath.c_str(), file.m_id, file.m_user_data, handled);
					if (!handled)
						MarkAsChanged(file.m_filepath.c_str());
				}
			}
		}
	};
}
