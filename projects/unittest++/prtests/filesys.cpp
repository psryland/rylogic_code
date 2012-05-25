//*************************************************************
// Unit Test for PRString
//*************************************************************
#include "unittest++/1.3/src/unittest++.h"
#include "pr/filesys/filesys.h"
#include "pr/filesys/findfiles.h"
#include "pr/filesys/recurse_directory.h"

SUITE(FileSys)
{
	TEST(Quotes)
	{
		std::string no_quotes = "path\\path\\file.extn";
		std::string has_quotes = "\"path\\path\\file.extn\"";
		std::string p = no_quotes;
		pr::filesys::RemoveQuotes(p);		CHECK_EQUAL(no_quotes , p);
		pr::filesys::AddQuotes(p);			CHECK_EQUAL(has_quotes, p);
		pr::filesys::AddQuotes(p);			CHECK_EQUAL(has_quotes, p);
	}
	TEST(Slashes)
	{
		std::string has_slashes1 = "\\path\\path\\";
		std::string has_slashes2 = "/path/path/";
		std::string no_slashes1 = "path\\path";
		std::string no_slashes2 = "path/path";
		
		pr::filesys::RemoveLeadingBackSlash(has_slashes1);
		pr::filesys::RemoveLastBackSlash(has_slashes1);
		CHECK_EQUAL(no_slashes1, has_slashes1);

		pr::filesys::RemoveLeadingBackSlash(has_slashes2);
		pr::filesys::RemoveLastBackSlash(has_slashes2);
		CHECK_EQUAL(no_slashes2, has_slashes2);
	}
	TEST(Canonicalise)
	{
		std::string p0 = "C:\\path/.././path\\path\\path\\../../../file.ext";
		std::string P0 = "C:\\file.ext";
		pr::filesys::Canonicalise(p0);
		CHECK_EQUAL(P0, p0);

		std::string p1 = ".././path\\path\\path\\../../../file.ext";
		std::string P1 = "..\\file.ext";
		pr::filesys::Canonicalise(p1);
		CHECK_EQUAL(P1, p1);
	}
	TEST(Standardise)
	{
		std::string p0 = "c:\\path/.././Path\\PATH\\path\\../../../PaTH\\File.EXT";
		std::string P0 = "c:\\path\\file.ext";
		pr::filesys::Standardise(p0);
		CHECK_EQUAL(P0, p0);
	}
	TEST(Make)
	{
		std::string p0 = pr::filesys::Make<std::string>("c:\\", "/./path0/path1/path2\\../", "./path3/file", "extn");
		std::string P0 = "c:\\path0\\path1\\path3\\file.extn";
		CHECK_EQUAL(P0, p0);

		std::string p1 = pr::filesys::Make<std::string>("c:\\./path0/path1/path2\\../", "./path3/file", "extn");
		std::string P1 = "c:\\path0\\path1\\path3\\file.extn";
		CHECK_EQUAL(P1, p1);

		std::string p2 = pr::filesys::Make<std::string>("c:\\./path0/path1/path2\\..", "./path3/file.extn");
		std::string P2 = "c:\\path0\\path1\\path3\\file.extn";
		CHECK_EQUAL(P2, p2);
	}
	TEST(GetDrive)
	{
		std::string p0 = pr::filesys::GetDrive<std::string>("drive:/path");
		std::string P0 = "drive";
		CHECK_EQUAL(P0, p0);
	}
	TEST(GetPath)
	{
		std::string p0 = pr::filesys::GetPath<std::string>("drive:/path0/path1/file.ext");
		std::string P0 = "path0/path1";
		CHECK_EQUAL(P0, p0);
	}
	TEST(GetDirectory)
	{
		std::string p0 = pr::filesys::GetDirectory<std::string>("drive:/path0/path1/file.ext");
		std::string P0 = "drive:/path0/path1";
		CHECK_EQUAL(P0, p0);
	}
	TEST(GetExtension)
	{
		std::string p0 = pr::filesys::GetExtension<std::string>("drive:/pa.th0/path1/file.stuff.extn");
		std::string P0 = "extn";
		CHECK_EQUAL(P0, p0);
	}
	TEST(GetFilename)
	{
		std::string p0 = pr::filesys::GetFilename<std::string>("drive:/pa.th0/path1/file.stuff.extn");
		std::string P0 = "file.stuff.extn";
		CHECK_EQUAL(P0, p0);
	}
	TEST(GetFiletitle)
	{
		std::string p0 = pr::filesys::GetFiletitle<std::string>("drive:/pa.th0/path1/file.stuff.extn");
		std::string P0 = "file.stuff";
		CHECK_EQUAL(P0, p0);
	}
	TEST(RmvDrive)
	{
		std::string p0 = "drive:/pa.th0/path1/file.stuff.extn";
		std::string P0 = "pa.th0/path1/file.stuff.extn";
		p0 = pr::filesys::RmvDrive(p0);
		CHECK_EQUAL(P0, p0);
	}
	TEST(RmvPath)
	{
		std::string p0 = "drive:/pa.th0/path1/file.stuff.extn";
		std::string P0 = "drive:/file.stuff.extn";
		p0 = pr::filesys::RmvPath(p0);
		CHECK_EQUAL(P0, p0);
	}
	TEST(RmvDirectory)
	{
		std::string p0 = "drive:/pa.th0/path1/file.stuff.extn";
		std::string P0 = "file.stuff.extn";
		p0 = pr::filesys::RmvDirectory(p0);
		CHECK_EQUAL(P0, p0);
	}
	TEST(RmvExtension)
	{
		std::string p0 = "drive:/pa.th0/path1/file.stuff.extn";
		std::string P0 = "drive:/pa.th0/path1/file.stuff";
		p0 = pr::filesys::RmvExtension(p0);
		CHECK_EQUAL(P0, p0);
	}
	TEST(RmvFilename)
	{
		std::string p0 = "drive:/pa.th0/path1/file.stuff.extn";
		std::string P0 = "drive:/pa.th0/path1";
		p0 = pr::filesys::RmvFilename(p0);
		CHECK_EQUAL(P0, p0);
	}
	TEST(RmvFiletitle)
	{
		std::string p0 = "drive:/pa.th0/path1/file.stuff.extn";
		std::string P0 = "drive:/pa.th0/path1/.extn";
		p0 = pr::filesys::RmvFiletitle(p0);
		CHECK_EQUAL(P0, p0);
	}
	TEST(Files)
	{
		std::string dir = pr::filesys::CurrentDirectory<std::string>();
		CHECK(pr::filesys::DirectoryExists(dir));

		std::string fn = pr::filesys::MakeUniqueFilename<std::string>("test_fileXXXXXX");
		CHECK(!pr::filesys::FileExists(fn));

		std::string path = pr::filesys::Make(dir, fn);

		std::ofstream f(path.c_str());
		f << "Hello World";
		f.close();
		
		CHECK(pr::filesys::FileExists(path));
		std::string fn2 = pr::filesys::MakeUniqueFilename<std::string>("test_fileXXXXXX");
		std::string path2 = pr::filesys::GetFullPath(fn2);

		pr::filesys::RenameFile(path, path2);
		CHECK( pr::filesys::FileExists(path2));
		CHECK(!pr::filesys::FileExists(path));

		pr::filesys::CpyFile(path2, path);
		CHECK( pr::filesys::FileExists(path2));
		CHECK( pr::filesys::FileExists(path));

		pr::filesys::EraseFile(path2);
		CHECK(!pr::filesys::FileExists(path2));
		CHECK( pr::filesys::FileExists(path));

		__int64 size = pr::filesys::FileLength(path);
		CHECK_EQUAL(11, size);

		unsigned int attr = pr::filesys::GetAttribs(path);
		unsigned int flags = pr::filesys::EAttrib::File|pr::filesys::EAttrib::WriteAccess|pr::filesys::EAttrib::ReadAccess;
		CHECK(attr == flags);

		attr = pr::filesys::GetAttribs(dir);
		flags = pr::filesys::EAttrib::Directory|pr::filesys::EAttrib::WriteAccess|pr::filesys::EAttrib::ReadAccess|pr::filesys::EAttrib::ExecAccess;
		CHECK(attr == flags);

		std::string drive = pr::filesys::GetDrive(path);
		unsigned __int64 disk_free = pr::filesys::GetDiskFree(drive[0]);
		unsigned __int64 disk_size = pr::filesys::GetDiskSize(drive[0]);
		CHECK(disk_size > disk_free);

		pr::filesys::EraseFile(path);
		CHECK(!pr::filesys::FileExists(path));
	}
	TEST(DirectoryOps)
	{
		{
			std::string p0 = "C:/path0/../";
			std::string p1 = "./path4/path5";
			std::string P  = "C:\\path4\\path5";
			std::string R  = pr::filesys::CombinePath(p0, p1);
			CHECK_EQUAL(P, R);
		}
		{
			std::string p0 = "C:/path0/path1/path2/path3/file.extn";
			std::string p1 = "C:/path0/path4/path5";
			std::string P  = "../../path1/path2/path3/file.extn";
			std::string R  = pr::filesys::GetRelativePath(p0, p1);
			CHECK_EQUAL(P, R);
		}
		{
			std::string p0 = "/path1/path2/file.extn";
			std::string p1 = "/path1/path3/path4";
			std::string P  = "../../path2/file.extn";
			std::string R  = pr::filesys::GetRelativePath(p0, p1);
			CHECK_EQUAL(P, R);
		}
		{
			std::string p0 = "/path1/file.extn";
			std::string p1 = "/path1";
			std::string P  = "file.extn";
			std::string R  = pr::filesys::GetRelativePath(p0, p1);
			CHECK_EQUAL(P, R);
		}
		{
			std::string p0 = "path1/file.extn";
			std::string p1 = "path2";
			std::string P  = "../path1/file.extn";
			std::string R  = pr::filesys::GetRelativePath(p0, p1);
			CHECK_EQUAL(P, R);
		}
		{
			std::string p0 = "c:/path1/file.extn";
			std::string p1 = "d:/path2";
			std::string P  = "c:/path1/file.extn";
			std::string R  = pr::filesys::GetRelativePath(p0, p1);
			CHECK_EQUAL(P, R);
		}
	}
	TEST(FindFiles)
	{
		bool found_cpp = false, found_h = false;
		std::string root = pr::filesys::GetDirectory<std::string>(__FILE__).append("\\..\\1.3\\src");
		for (pr::filesys::FindFiles<> ff(root, "*.cpp;*.h"); !ff.done(); ff.next())
		{
			found_cpp |= pr::filesys::GetExtension<std::string>(ff.fullpath()).compare("cpp") == 0;
			found_h   |= pr::filesys::GetExtension<std::string>(ff.fullpath()).compare("h") == 0;
		}
		CHECK(found_cpp);
		CHECK(found_h);
	}
	TEST(RecurseDirectory)
	{
		struct CB
		{
			static bool SkipDir  (const char*, void*) { return false; }
			static bool EnumFiles(const char* pathname, void* context)
			{
				int* found = static_cast<int*>(context);
				std::string extn = pr::filesys::GetExtension<std::string>(pathname);
				if      (extn.compare("cpp") == 0) ++found[0];
				else if (extn.compare("c")   == 0) ++found[1];
				else if (extn.compare("h")   == 0) ++found[2];
				else                               ++found[3];
				return true;
			}
		};
		
		int found[4] = {}; // 0-*.cpp, 1-*.c, 2-*.h, 3-other
		std::string root = pr::filesys::GetDirectory<std::string>(__FILE__).append("\\..");
		CHECK(pr::filesys::RecurseFiles(root, CB::EnumFiles, "*.cpp;*.c", found, CB::SkipDir));
		CHECK(pr::filesys::RecurseFiles(root, CB::EnumFiles, "*.h"      , found, CB::SkipDir));
		CHECK(found[0] >  0);
		CHECK(found[1] == 0);
		CHECK(found[2] >  0);
		CHECK(found[3] == 0);
	}
}
