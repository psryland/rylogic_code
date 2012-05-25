//*************************************************************
// Unit Test for PRString
//*************************************************************
#include "unittest++/1.3/src/unittest++.h"
#include "pr/common/assert.h"
#include "pr/storage/sqlite.h"
#include "pr/filesys/filesys.h"
#include "pr/common/guid.h"

using namespace pr;
using namespace pr::sqlite;

SUITE(PRSqlite)
{
	typedef unsigned char byte;
	typedef unsigned short ushort;
	typedef unsigned int uint;
	typedef unsigned __int64 uint64;
	typedef std::vector<byte> buffer;
	
	namespace Enum
	{
		enum Type { One, Two, Three };
	}
	
	struct DB :pr::sqlite::Database
	{
		DB() :pr::sqlite::Database("tmpDB.db") {}
		~DB() { pr::filesys::EraseFile<std::string>("tmpDB.db"); }
	};
	
	TEST(SimpleTypeStorage)
	{
		struct Record
		{
			int          m_key;
			bool         m_bool;
			char         m_char;
			byte         m_byte;
			short        m_short;
			ushort       m_ushort;
			int          m_int;
			uint         m_uint;
			__int64      m_int64;
			uint64       m_uint64;
			float        m_float;
			double       m_double;
			char         m_char_array[10];
			int          m_int_array[10];
			Enum::Type   m_enum;
			std::string  m_string;
			buffer       m_buf;
			buffer       m_empty_buf;
			
			// Create sqlite table mapping meta data
			PR_SQLITE_TABLE(Record, "")
			PR_SQLITE_COLUMN(Key        ,m_key        ,integer , "primary key autoincrement not null")
			PR_SQLITE_COLUMN(Bool       ,m_bool       ,integer , "")
			PR_SQLITE_COLUMN(Char       ,m_char       ,integer , "")
			PR_SQLITE_COLUMN(Byte       ,m_byte       ,integer , "")
			PR_SQLITE_COLUMN(Short      ,m_short      ,integer , "")
			PR_SQLITE_COLUMN(Ushort     ,m_ushort     ,integer , "")
			PR_SQLITE_COLUMN(Int        ,m_int        ,integer , "")
			PR_SQLITE_COLUMN(Uint       ,m_uint       ,integer , "")
			PR_SQLITE_COLUMN(Int64      ,m_int64      ,integer , "")
			PR_SQLITE_COLUMN(Uint64     ,m_uint64     ,integer , "")
			PR_SQLITE_COLUMN(Float      ,m_float      ,real    , "")
			PR_SQLITE_COLUMN(Double     ,m_double     ,real    , "")
			PR_SQLITE_COLUMN(CharArray  ,m_char_array ,text    , "")
			PR_SQLITE_COLUMN(IntArray   ,m_int_array  ,blob    , "")
			PR_SQLITE_COL_AS(Enum       ,m_enum ,int  ,integer , "")
			PR_SQLITE_COLUMN(String     ,m_string     ,text    , "")
			PR_SQLITE_COLUMN(Buf        ,m_buf        ,blobcont, "")
			PR_SQLITE_COLUMN(EmptyBuf   ,m_empty_buf  ,blobcont, "")
			PR_SQLITE_TABLE_END()
			Record()
				:m_key()
				,m_bool()
				,m_char()
				,m_byte()
				,m_short()
				,m_ushort()
				,m_int()
				,m_uint()
				,m_int64()
				,m_uint64()
				,m_float()
				,m_double()
				,m_char_array()
				,m_int_array()
				,m_enum()
				,m_string()
				,m_buf()
				,m_empty_buf()
			{}
		};
		
		DB db;
		db.DropTable<Record>();
		CHECK_EQUAL(SQLITE_OK, db.CreateTable<Record>());
		auto table = db.Table<Record>();
		
		Record r;
		r.m_key         = 0;
		r.m_bool        = true;
		r.m_char        = char(123);
		r.m_byte        = 255;
		r.m_short       = 12345;
		r.m_ushort      = 65432;
		r.m_int         = -12345678;
		r.m_uint        = 876543210;
		r.m_int64       = 12345678900000;
		r.m_uint64      = 98765432100000;
		r.m_float       = 6.28f;
		r.m_double      = 6.28;
		r.m_char_array; for (int i = 0; i != 10; ++i) r.m_char_array[i] = char('0'+i);
		r.m_int_array;  for (int i = 0; i != 10; ++i) r.m_int_array[i] = i;
		r.m_enum        = Enum::Two;
		r.m_string      = "Paul Was Here";
		r.m_buf;        for (int i = 0; i != 10; ++i) r.m_buf.push_back(byte(i));
		r.m_empty_buf   .clear();
		CHECK_EQUAL(1, table.Insert(r, r.m_key));
		
		Record R = table.Get(PKs(r.m_key));
		CHECK_EQUAL(r.m_key        , R.m_key);
		CHECK_EQUAL(r.m_bool       , R.m_bool);
		CHECK_EQUAL(r.m_char       , R.m_char);
		CHECK_EQUAL(r.m_byte       , R.m_byte);
		CHECK_EQUAL(r.m_short      , R.m_short);
		CHECK_EQUAL(r.m_ushort     , R.m_ushort);
		CHECK_EQUAL(r.m_int        , R.m_int);
		CHECK_EQUAL(r.m_uint       , R.m_uint);
		CHECK_EQUAL(r.m_int64      , R.m_int64);
		CHECK_EQUAL(r.m_uint64     , R.m_uint64);
		CHECK_EQUAL(r.m_float      , R.m_float);
		CHECK_EQUAL(r.m_double     , R.m_double);
		for (int i = 0; i != 10; ++i) CHECK_EQUAL(r.m_char_array[i] , R.m_char_array[i]);
		for (int i = 0; i != 10; ++i) CHECK_EQUAL(r.m_int_array[i]  , R.m_int_array[i]);
		CHECK_EQUAL(r.m_enum       , R.m_enum);
		CHECK_EQUAL(r.m_string     , R.m_string);
		CHECK_EQUAL(r.m_buf.size() , R.m_buf.size());
		for (size_t i = 0; i != r.m_buf.size() && i != R.m_buf.size(); ++i) CHECK_EQUAL(r.m_buf[i], R.m_buf[i]);
		CHECK_EQUAL(0U, R.m_empty_buf.size());
		
		int key = r.m_key;
		
		r.m_string = "Modified string";
		r.m_empty_buf.push_back(42);
		CHECK_EQUAL(1, table.Update(r));
		CHECK_EQUAL(key, r.m_key);
		
		R = table.Get(PKs(r.m_key));
		CHECK_EQUAL(r.m_string, R.m_string);
		CHECK_EQUAL(r.m_empty_buf.size() , R.m_empty_buf.size());
		for (size_t i = 0; i != r.m_empty_buf.size() && i != R.m_empty_buf.size(); ++i)
			CHECK_EQUAL(r.m_empty_buf[i], R.m_empty_buf[i]);
	}
	
	TEST(Insert)
	{
		struct Record
		{
			int  m_key;
			char m_char;
			
			// Create sqlite table mapping meta data
			PR_SQLITE_TABLE(Record, "") // whitespace shouldn't affect constraints
			PR_SQLITE_COLUMN(Key    ,m_key  ,integer , "primary key not null")
			PR_SQLITE_COLUMN(Bool   ,m_char ,integer , "unique")
			PR_SQLITE_TABLE_END()
			
			Record() :m_key() ,m_char() {}
			Record(int key, char ch) :m_key(key) ,m_char(ch) {}
		};
		
		DB db;
		db.DropTable<Record>();
		db.CreateTable<Record>();
		auto table = db.Table<Record>();
		
		CHECK_EQUAL(1, table.Insert(Record(1, 'a')));
		CHECK_EQUAL(1, table.Insert(Record(2, 'b')));
		
		try { table.Insert(Record(1, 'c'), EOnConstraint::Reject); }
		catch (pr::sqlite::Exception const& ex)
		{
			CHECK_EQUAL(SQLITE_CONSTRAINT, ex.code());
		}
		
		// Ignore, should ignore constraints volations
		CHECK_EQUAL(0, table.Insert(Record(1, 'd'), EOnConstraint::Ignore));
		CHECK_EQUAL('a', table.Get(PKs(1)).m_char);
		
		// Replace, should replace on constraint volation
		CHECK_EQUAL(1, table.Insert(Record(1, 'e'), EOnConstraint::Replace));
		CHECK_EQUAL('e', table.Get(PKs(1)).m_char);
		
	}
	
	TEST(PartialObjectUpdates)
	{
		struct Record
		{
			int          m_key;
			std::string  m_string;
			
			// Create sqlite table mapping meta data
			PR_SQLITE_TABLE(Record, "")
			PR_SQLITE_COLUMN(Key    , m_key    ,integer ,"primary key autoincrement not null")
			PR_SQLITE_COLUMN(String , m_string ,text    ,"")
			PR_SQLITE_TABLE_END()
			
			Record() :m_key() ,m_string() {}
			Record(char const* str) :m_key() ,m_string(str) {}
		};
		
		DB db;
		db.DropTable<Record>();
		db.CreateTable<Record>();
		auto table = db.Table<Record>();
		
		CHECK_EQUAL(1, table.Insert(Record("Elem1")));
		CHECK_EQUAL(1, table.Insert(Record("Elem2")));
		CHECK_EQUAL(1, table.Insert(Record("Elem3")));
		
		Record r = table.Get(PKs(2));
		CHECK_EQUAL("Elem2", r.m_string.c_str());
		
		CHECK_EQUAL(1, table.Update("String", std::string("Modified"), PKs(r.m_key)));
		
		Record r2 = table.Get(PKs(r.m_key));
		CHECK_EQUAL("Modified", r2.m_string.c_str());
	}
	
	TEST(MultiplePKs)
	{
		struct Record
		{
			int          m_key;
			bool         m_bool;
			std::string  m_string;
			
			// Create sqlite table mapping meta data
			PR_SQLITE_TABLE(Record, "unique (String), primary key (Key, Bool)") // whitespace shouldn't affect constraints
			PR_SQLITE_COLUMN(Key    ,m_key    ,integer , "not null")
			PR_SQLITE_COLUMN(Bool   ,m_bool   ,integer , "not null")
			PR_SQLITE_COLUMN(String ,m_string ,text    , "")
			PR_SQLITE_TABLE_END()
			
			Record()
				:m_key()
				,m_bool()
				,m_string()
			{}
		};
		typedef pr::sqlite::PKArgs<int,bool> PKArgs;
		
		DB db;
		db.DropTable<Record>();
		CHECK_EQUAL(SQLITE_OK, db.CreateTable<Record>());
		auto table = db.Table<Record>();
		
		Record r[4];
		r[0].m_key    = 1;
		r[0].m_bool   = false;
		r[0].m_string = "1 false";
		r[1].m_key    = 1;
		r[1].m_bool   = true;
		r[1].m_string = "1 true";
		r[2].m_key    = 2;
		r[2].m_bool   = false;
		r[2].m_string = "2 false";
		r[3].m_key    = 2;
		r[3].m_bool   = true;
		r[3].m_string = "2 true";
		
		CHECK_EQUAL(1, table.Insert(r[0]));
		CHECK_EQUAL(1, table.Insert(r[1]));
		CHECK_EQUAL(1, table.Insert(r[2]));
		CHECK_EQUAL(1, table.Insert(r[3]));
		
		Record R[4];
		R[0] = table.Get(PKs(1, false));
		R[1] = table.Get(PKs(1, true));
		R[2] = table.Get(PKs(2, false));
		R[3] = table.Get(PKs(2, true));
		
		for (int i = 0; i != 4; ++i)
		{
			CHECK_EQUAL(r[i].m_key    , R[i].m_key);
			CHECK_EQUAL(r[i].m_bool   , R[i].m_bool);
			CHECK_EQUAL(r[i].m_string , R[i].m_string);
		}
		
		PKArgs args = PrimaryKeys<PKArgs>(r[3]);
		CHECK_EQUAL(r[3].m_key , args.pk1);
		CHECK_EQUAL(r[3].m_bool, args.pk2);
		
		r[3].m_string = "2 true - modified";
		CHECK_EQUAL(1, table.Update("String", r[3].m_string, PrimaryKeys<PKArgs>(r[3])));
		
		R[3] = table.Get(PrimaryKeys<PKArgs>(r[3]));
		CHECK_EQUAL(r[3].m_key    , R[3].m_key);
		CHECK_EQUAL(r[3].m_bool   , R[3].m_bool);
		CHECK_EQUAL(r[3].m_string , R[3].m_string);
	}
	
	TEST(Collation)
	{
		struct Record
		{
			int          m_key;
			std::string  m_collate_default; //"CollateDefault" varchar(140) ,
			std::string  m_collate_binary;  //"CollateBinary" varchar(140) collate BINARY ,
			std::string  m_collate_rtrim;   //"CollateRTrim" varchar(140) collate RTRIM ,
			std::string  m_collate_nocase;  //"CollateNoCase" varchar(140) collate NOCASE )
			
			// Create sqlite table mapping meta data
			PR_SQLITE_TABLE(Record, "")
			PR_SQLITE_COLUMN(x ,m_key             ,integer ,"primary key autoincrement not null")
			PR_SQLITE_COLUMN(a ,m_collate_default ,text    ,"");
			PR_SQLITE_COLUMN(b ,m_collate_binary  ,text    ,"collate BINARY");
			PR_SQLITE_COLUMN(c ,m_collate_rtrim   ,text    ,"collate RTRIM");
			PR_SQLITE_COLUMN(d ,m_collate_nocase  ,text    ,"collate NOCASE");
			PR_SQLITE_TABLE_END()
		};
		
		DB db;
		db.DropTable<Record>();
		CHECK_EQUAL(SQLITE_OK, db.CreateTable<Record>());
		CHECK_EQUAL(1, db.Execute("insert into Record values (1 , 'abc' , 'abc'  , 'abc  ' , 'abc')"));
		CHECK_EQUAL(1, db.Execute("insert into Record values (2 , 'abc' , 'abc'  , 'abc'   , 'ABC')"));
		CHECK_EQUAL(1, db.Execute("insert into Record values (3 , 'abc' , 'abc'  , 'abc '  , 'Abc')"));
		CHECK_EQUAL(1, db.Execute("insert into Record values (4 , 'abc' , 'abc ' , 'ABC'   , 'abc')"));
		
		int value;
		{
			// Text comparison a=b is performed using the BINARY collating sequence.
			Query q(db, "select x from Record where a = b order by x");
			//--result 1 2 3
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(1, read_int(q, 0, value));
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(2, read_int(q, 0, value));
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(3, read_int(q, 0, value));
			q.Step(); CHECK(q.RowEnd());
		}
		{
			// Text comparison a=b is performed using the RTRIM collating sequence.
			Query q(db, "select x from Record where a = b collate rtrim order by x");
			//--result 1 2 3 4
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(1, read_int(q, 0, value));
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(2, read_int(q, 0, value));
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(3, read_int(q, 0, value));
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(4, read_int(q, 0, value));
			q.Step(); CHECK(q.RowEnd());
		}
		{
			// Text comparison d=a is performed using the NOCASE collating sequence.
			Query q(db, "select x from Record where d = a order by x");
			//--result 1 2 3 4
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(1, read_int(q, 0, value));
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(2, read_int(q, 0, value));
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(3, read_int(q, 0, value));
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(4, read_int(q, 0, value));
			q.Step(); CHECK(q.RowEnd());
		}
		{
			// Text comparison a=d is performed using the BINARY collating sequence.
			Query q(db, "select x from Record where a = d order by x");
			//--result 1 4
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(1, read_int(q, 0, value));
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(4, read_int(q, 0, value));
			q.Step(); CHECK(q.RowEnd());
		}
		{
			// Text comparison 'abc'=c is performed using the RTRIM collating sequence.
			Query q(db, "select x from Record where 'abc' = c order by x");
			//--result 1 2 3
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(1, read_int(q, 0, value));
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(2, read_int(q, 0, value));
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(3, read_int(q, 0, value));
			q.Step(); CHECK(q.RowEnd());
		}
		{
			// Text comparison c='abc' is performed using the RTRIM collating sequence.
			Query q(db, "select x from Record where c = 'abc' order by x");
			//--result 1 2 3
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(1, read_int(q, 0, value));
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(2, read_int(q, 0, value));
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(3, read_int(q, 0, value));
			q.Step(); CHECK(q.RowEnd());
		}
		{
			// Grouping is performed using the NOCASE collating sequence. (Values 'abc', 'ABC', and 'Abc' are placed in the same group).
			Query q(db, "select count(*) from Record group by d order by 1");
			//--result 4
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(4, read_int(q, 0, value));
			q.Step(); CHECK(q.RowEnd());
		}
		{
			// Grouping is performed using the BINARY collating sequence. 'abc' and 'ABC' and 'Abc' form different groups
			Query q(db, "select count(*) from Record group by (d || '') order by 1");
			//--result 1 1 2
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(1, read_int(q, 0, value));
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(1, read_int(q, 0, value));
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(2, read_int(q, 0, value));
			q.Step(); CHECK(q.RowEnd());
		}
		{
			// Sorting or column c is performed using the RTRIM collating sequence.
			Query q(db, "select x from Record order by c, x");
			//--result 4 1 2 3
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(4, read_int(q, 0, value));
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(1, read_int(q, 0, value));
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(2, read_int(q, 0, value));
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(3, read_int(q, 0, value));
			q.Step(); CHECK(q.RowEnd());
		}
		{
			// Sorting of (c||'') is performed using the BINARY collating sequence.
			Query q(db, "select x from Record order by (c||''), x");
			//--result 4 2 3 1
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(4, read_int(q, 0, value));
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(2, read_int(q, 0, value));
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(3, read_int(q, 0, value));
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(1, read_int(q, 0, value));
			q.Step(); CHECK(q.RowEnd());
		}
		{
			// Sorting of column c is performed using the NOCASE collating sequence.
			Query q(db, "select x from Record order by c collate nocase, x");
			//--result 2 4 3 1
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(2, read_int(q, 0, value));
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(4, read_int(q, 0, value));
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(3, read_int(q, 0, value));
			q.Step(); CHECK(!q.RowEnd()); CHECK_EQUAL(1, read_int(q, 0, value));
			q.Step(); CHECK(q.RowEnd());
		}
	}
	
	TEST(Unique)
	{
		struct Record
		{
			int  m_key;
			char m_char;
			
			// Create sqlite table mapping meta data
			PR_SQLITE_TABLE(Record, "") // whitespace shouldn't affect constraints
			PR_SQLITE_COLUMN(Key    ,m_key  ,integer , "primary key autoincrement not null")
			PR_SQLITE_COLUMN(Bool   ,m_char ,integer , "unique")
			PR_SQLITE_TABLE_END()
			
			Record() :m_key() ,m_char() {}
			Record(char ch) :m_key() ,m_char(ch) {}
		};
		
		DB db;
		db.DropTable<Record>();
		db.CreateTable<Record>();
		auto table = db.Table<Record>();
		
		CHECK_EQUAL(1, table.Insert(Record('a')));
		CHECK_EQUAL(1, table.Insert(Record('b')));
		CHECK_THROW(table.Insert(Record('a')), pr::sqlite::Exception);
		try { table.Insert(Record('b')); }
		catch (pr::sqlite::Exception const& ex)
		{
			CHECK_EQUAL(SQLITE_CONSTRAINT, ex.code());
		}
	}
	
	TEST(Find)
	{
		struct Record
		{
			int  m_key;
			char m_char;
			
			// Create sqlite table mapping meta data
			PR_SQLITE_TABLE(Record, "") // whitespace shouldn't affect constraints
			PR_SQLITE_COLUMN(Key    ,m_key  ,integer , "primary key autoincrement not null")
			PR_SQLITE_COLUMN(Bool   ,m_char ,integer , "")
			PR_SQLITE_TABLE_END()
			
			Record() :m_key() ,m_char() {}
			Record(char ch) :m_key() ,m_char(ch) {}
		};
		
		DB db;
		db.DropTable<Record>();
		db.CreateTable<Record>();
		auto table = db.Table<Record>();
		
		CHECK_EQUAL(1, table.Insert(Record('a')));
		CHECK_EQUAL(1, table.Insert(Record('b')));
		CHECK_EQUAL(1, table.Insert(Record('c')));
		CHECK_EQUAL(1, table.Insert(Record('d')));
		CHECK_EQUAL(1, table.Insert(Record('e')));
		
		Record r = table.Get(PKs(3));
		try { table.Get(PKs(6)); }
		catch (pr::sqlite::Exception const& ex)
		{
			CHECK_EQUAL(SQLITE_NOTFOUND, ex.code());
		}
		
		Record R;
		CHECK(table.Find(PKs(3), R));
		CHECK(!table.Find(PKs(6), R));
	}
	
	TEST(Unicode)
	{
		struct Record
		{
			int          m_key;
			std::wstring m_wstr;
			
			// Create sqlite table mapping meta data
			PR_SQLITE_TABLE(Record, "")
			PR_SQLITE_COLUMN(Key ,m_key  ,integer ,"primary key autoincrement not null")
			PR_SQLITE_COLUMN(Str ,m_wstr ,text    ,"")
			PR_SQLITE_TABLE_END()
			
			Record() :m_key() ,m_wstr() {}
			Record(std::wstring const& str) :m_key() ,m_wstr(str) {}
		};
		
		DB db;
		db.DropTable<Record>();
		db.CreateTable<Record>();
		auto table = db.Table<Record>();
		
		int row;
		std::wstring str = L"€€€€";
		table.Insert(Record(str), row);
		
		std::wstring STR = table.GetColumn<std::wstring>(PKs(row), 1);
		CHECK(str == STR);
	}
	
	TEST(GUIDs)
	{
		struct Record
		{
			GUID m_guid;
			Record() :m_guid(pr::GenerateGUID()) {}
			
			// Create sqlite table mapping meta data
			PR_SQLITE_TABLE(Record, "")
			PR_SQLITE_COLUMN(Guid ,m_guid, blob, "primary key not null")
			PR_SQLITE_TABLE_END()
		};
		//struct GUIDConvFunc
		//{
		//  // Custom converter for GUIDs
		//  typedef pr::string<> ColumnType;
		//  typedef GUID         FieldType;
		//  static void GetField (TableType const& item, FieldType&  value) { pr::sqlite::Assign(value, item.member); }
		//  static void SetField (TableType& item, FieldType const&  value) { pr::sqlite::Assign(item.member, value); }
		//  static void GetColumn(TableType const& item, ColumnType& value) { pr::sqlite::Assign(value, item.member); }
		//  static void SetColumn(TableType& item, ColumnType const& value) { pr::sqlite::Assign(item.member, value); }
		//};
		
		DB db;
		db.DropTable<Record>();
		db.CreateTable<Record>();
		auto table = db.Table<Record>();
		
		CHECK_EQUAL(1, table.Insert(Record()));
	}
}
