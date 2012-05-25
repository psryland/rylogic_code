//*************************************************************
// Unit Test for PRString
//*************************************************************
#include "unittest++/1.3/src/unittest++.h"
#include "pr/filesys/filesys.h"
#include "pr/storage/csv.h"
#include "pr/str/prstring.h"

using namespace pr;
using namespace pr::csv;

SUITE(PRCsv)
{
	TEST(SaveCsv)
	{
		Csv csv;
		SetItem(csv, 1, 1, "Hello");
		SetItem(csv, 1, 2, "World");
		CHECK(Save("test_csv.csv", csv));
		CHECK(Item(csv, 1,1) == "Hello");
		CHECK(Item(csv, 1,2) == "World");
	}
	TEST(LoadCsv)
	{
		Csv csv;
		CHECK(Load("test_csv.csv", csv));
		CHECK(csv.size() == 2);
		CHECK(csv[1].size() == 3);
		CHECK(csv[1][1] == "Hello");
		CHECK(csv[1][2] == "World");
	}
	TEST(SaveCsvStream)
	{
		std::ofstream csv("test_csv.csv");
		CHECK(csv.is_open());
		csv << "Hello,World\n";
		csv << "a,,b,c\n";
		csv << '\n';
		csv << 1 << ',' << 2.0f << ',' << 3.0 << '\n';
	}
	TEST(LoadCsvStream)
	{
		std::ifstream csv("test_csv.csv");
		CHECK(csv.is_open());
		Loc loc;
		
		std::string s0; char s1[10];
		CHECK(Read(csv, s0, &loc));
		CHECK(Read(csv, s1, &loc));
		CHECK(s0 == "Hello");
		CHECK(std::string(s1) == "World");
		
		char ch;
		csv >> ch >> loc >> loc; CHECK(ch == 'a');
		csv >> ch >> loc; CHECK(ch == 'b');
		csv >> ch >> loc; CHECK(ch == 'c');
		
		NextItem(csv, &loc);
		int i; float f; double d;
		csv >> i >> loc >> f >> loc >> d >> loc;
		CHECK(i == 1);
		CHECK(f == 2.0f);
		CHECK(d == 3.0);
		CHECK(loc.row == 4 && loc.col == 0);
	}
	TEST(CsvCleanUp)
	{
		CHECK(pr::filesys::EraseFile<std::string>("test_csv.csv"));
	}
}
