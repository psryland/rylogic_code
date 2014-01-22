
#ifndef NANA_FILESYSTEM_FS_UTILITY_HPP
#define NANA_FILESYSTEM_FS_UTILITY_HPP

#include <nana/basic_types.hpp>
#include <ctime>

namespace nana
{
namespace filesystem
{
	struct error
	{
		enum
		{
			none = 0
		};
	};

	struct attribute
	{
		long_long_t bytes;
		bool is_directory;
		::tm	modified;
	};

	bool file_attrib(const nana::string& file, attribute&);
	long_long_t filesize(const nana::char_t* file);
	bool modified_file_time(const nana::string& file, struct tm&);
	bool mkdir(const nana::string& dir, bool & if_exist);
	bool rmfile(const nana::char_t* file);
	bool rmdir(const nana::char_t* dir, bool fails_if_not_empty);
	nana::string root(const nana::string& path);

	nana::string path_user();
	nana::string path_current();

	class path
	{
	public:
		struct type
		{	enum{not_exist, file, directory};
		};

		path();
		path(const nana::string&);

		bool empty() const;
		path root() const;
		int what() const;

		nana::string name() const;
	private:
#if defined(NANA_WINDOWS)
		nana::string text_;
#else
		std::string text_;
#endif
	};
}//end namespace filesystem
}//end namespace nana

#endif
