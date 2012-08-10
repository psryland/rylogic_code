//*****************************************
// Sqlite C++ wrapper
//  Copyright © Paul Ryland 2012
//*****************************************

#pragma once
#ifndef PR_STORAGE_SQLITE_H
#define PR_STORAGE_SQLITE_H

#include <sqlite3.h>
#include <ctype.h>
#include <exception>
#include <vector>
#include <algorithm>
#include <typeinfo>
#include <type_traits>

#ifndef PR_ASSERT
#   define PR_ASSERT_DEFINED
#   define PR_ASSERT(grp, exp, str) (void)0
#endif
#ifndef PR_SQL_ASSERTS
#   ifdef NDEBUG
#      define PR_SQL_ASSERTS 0
#   else
#      define PR_SQL_ASSERTS 1
#   endif
#endif

namespace pr
{
	namespace sqlite
	{
		// Macros for constructing an ORM
		// Example:
		//   struct Record
		//   {
		//       int         m_key;
		//       std::string m_string;
		//       float       m_float;
		//       
		//       PR_SQLITE_TABLE(Record,"")
		//       PR_SQLITE_COLUMN(Key    ,m_key    ,integer ,"primary key autoincrement not null")
		//       PR_SQLITE_COLUMN(String ,m_string ,text    ,"")
		//       PR_SQLITE_COLUMN(Float  ,m_float  ,text    ,"")
		//       PR_SQLITE_TABLE_END()
		//   };
		// Data Types:
		//   sqlite uses these data types:
		//      null    - the null value
		//      integer - A signed integer, stored in 1, 2, 3, 4, 6, or 8 bytes depending on the magnitude of the value.
		//      real    - A floating point value, stored as an 8-byte IEEE floating point number.
		//      text    - A text string, stored using the database encoding (UTF-8, UTF-16BE or UTF-16LE).
		//      blob    - A blob of data, stored exactly as it was input.
		//  All other type keywords are mapped to these types
		// See:
		//   http://www.sqlite.org/syntaxdiagrams.html
		//   http://www.sqlite.org/datatype3.html
		// Note:
		//  This wrapper uses simplified string searching. All constraints and datatype identifiers
		//  must be given in lower case, separated by single ' ' characters.
		//
		#pragma region Table definition macros
		#define PR_SQLITE_TABLE(type_name, table_constraints)\
			static pr::sqlite::TableMetaData<type_name> const& Sqlite_TableMetaData()\
			{\
				using namespace pr::sqlite;\
				struct MetaData :TableMetaData<type_name>\
				{\
					MetaData()\
					:TableMetaData<TableType>(#type_name, table_constraints)\
					{
		#define PR_SQLITE_TABLE_END()\
					}\
				};\
				static MetaData meta;\
				return meta;\
			}
		
		// Columns that perform type converting on read/write to the record type
		#define PR_SQLITE_COL_AS_CUST(column_name, adapter, datatype, constraints)\
			AddColumn<adapter>(#column_name, #datatype, constraints);
		
		// Member columns that don't need conversion
		#define PR_SQLITE_COLUMN(column_name, member, datatype, constraints)\
			struct column_name##Adapter\
			{\
				typedef decltype(static_cast<TableType*>(0)->member) ColumnType;\
				static void Set(TableType& item, ColumnType const& value)              { pr::sqlite::Assign(item.member, value); }\
				static void Get(TableType const& item, ColumnType& value)              { pr::sqlite::Assign(value, item.member); }\
				static void Bind(sqlite3_stmt* stmt, int col, ColumnType const& value) { pr::sqlite::bind_##datatype(stmt, col, value); }\
				static void Read(sqlite3_stmt* stmt, int col, ColumnType&       value) { pr::sqlite::read_##datatype(stmt, col, value); }\
			};\
			PR_SQLITE_COL_AS_CUST(column_name, column_name##Adapter, datatype, constraints)
		
		// Default implementation of a converting column
		#define PR_SQLITE_COL_AS(column_name, member, as_type, datatype, constraints)\
			struct column_name##Adapter\
			{\
				typedef as_type ColumnType;\
				static void Set(TableType& item, ColumnType const& value)              { pr::sqlite::Assign(item.member, value); }\
				static void Get(TableType const& item, ColumnType& value)              { pr::sqlite::Assign(value, item.member); }\
				static void Bind(sqlite3_stmt* stmt, int col, ColumnType const& value) { pr::sqlite::bind_##datatype(stmt, col, value); }\
				static void Read(sqlite3_stmt* stmt, int col, ColumnType&       value) { pr::sqlite::read_##datatype(stmt, col, value); }\
			};\
			PR_SQLITE_COL_AS_CUST(column_name, column_name##Adapter, datatype, constraints)
		
		#pragma endregion
		
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		// Repeater macros
		#define PR_SQLITE_REP0(m,s) 
		#define PR_SQLITE_REP1(m,s) m(1)
		#define PR_SQLITE_REP2(m,s) PR_SQLITE_REP1(m,s)##s##m(2)
		#define PR_SQLITE_REP3(m,s) PR_SQLITE_REP2(m,s)##s##m(3)
		#define PR_SQLITE_REP4(m,s) PR_SQLITE_REP3(m,s)##s##m(4)
		#define PR_SQLITE_REP5(m,s) PR_SQLITE_REP4(m,s)##s##m(5)
		#define PR_SQLITE_REP6(m,s) PR_SQLITE_REP5(m,s)##s##m(6)
		#define PR_SQLITE_REP7(m,s) PR_SQLITE_REP6(m,s)##s##m(7)
		#define PR_SQLITE_REP8(m,s) PR_SQLITE_REP7(m,s)##s##m(8)
		#define PR_SQLITE_REP9(m,s) PR_SQLITE_REP8(m,s)##s##m(9)
		#define PR_SQLITE_JOIN2(x,y)        x##y
		#define PR_SQLITE_JOIN(x,y)         PR_SQLITE_JOIN2(x,y)
		#define PR_SQLITE_REPEAT(N,mac,sep) PR_SQLITE_JOIN(PR_SQLITE_REP,N)(mac,sep)
		#define PR_COMMA ,
		#define PR_NEWLINE\
		// leave this line blank
		
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		typedef unsigned char byte;
		typedef union { void* v; byte* uc; char* c; wchar_t* w; } Ptr;
		typedef union { void const* v; byte const* uc; char const* c; wchar_t const* w; } CPtr;
		typedef std::vector<std::string> StrCont;
		
		// Forwards
		class Query;
		template <typename DBRecord> class DBTable;
		class Database;
		
		// Behaviours on constraint
		namespace EOnConstraint
		{
			enum Type { Reject, Ignore, Replace };
		}
		
		// Sql exception type
		struct Exception :std::exception
		{
			int m_code;
			std::string m_msg;
			Exception()                                            :m_code()     ,m_msg()    {}
			Exception(int code, char const* msg, bool sqlfree_msg) :m_code(code) ,m_msg(msg) { if (sqlfree_msg) sqlite3_free((void*)msg); }
			virtual char const* what() const { return m_msg.c_str(); }
			virtual int         code() const { return m_code; }
		};
		
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		#pragma region Primary key arguments
		#ifndef PR_SQLITE_MAX_PRIMARY_KEYS
		#define PR_SQLITE_MAX_PRIMARY_KEYS 5
		#endif

		struct Null {};
		
		// Helper struct for containing sets of primary keys
		// Generates a structure containing 'PR_SQLITE_MAX_PRIMARY_KEYS' types,
		// all with a default type of 'Null'.
		#define PR_M(n) typename T##n=Null
		template <PR_SQLITE_REPEAT(PR_SQLITE_MAX_PRIMARY_KEYS,PR_M,PR_COMMA)>
		#undef PR_M
		struct PKArgs
		{
			#define PR_M(n) T##n pk##n;
			PR_SQLITE_REPEAT(PR_SQLITE_MAX_PRIMARY_KEYS, PR_M,)
			#undef PR_M
			#define PR_M(n) T##n PK##n=T##n()
			PKArgs(PR_SQLITE_REPEAT(PR_SQLITE_MAX_PRIMARY_KEYS,PR_M,PR_COMMA))
			#undef PR_M
			#define PR_M(n) pk##n(PK##n)
			:PR_SQLITE_REPEAT(PR_SQLITE_MAX_PRIMARY_KEYS,PR_M,PR_COMMA){}
			#undef PR_M
		};
		
		template <typename T1>                                                 PKArgs<T1>             PKs(T1 p1)                         { return PKArgs<T1>(p1); }
		template <typename T1,typename T2>                                     PKArgs<T1,T2>          PKs(T1 p1,T2 p2)                   { return PKArgs<T1,T2>(p1,p2); }
		template <typename T1,typename T2,typename T3>                         PKArgs<T1,T2,T3>       PKs(T1 p1,T2 p2,T3 p3)             { return PKArgs<T1,T2,T3>(p1,p2,p3); }
		template <typename T1,typename T2,typename T3,typename T4>             PKArgs<T1,T2,T3,T4>    PKs(T1 p1,T2 p2,T3 p3,T4 p4)       { return PKArgs<T1,T2,T3,T4>(p1,p2,p3,p4); }
		template <typename T1,typename T2,typename T3,typename T4,typename T5> PKArgs<T1,T2,T3,T4,T5> PKs(T1 p1,T2 p2,T3 p3,T4 p4,T5 p5) { return PKArgs<T1,T2,T3,T4,T5>(p1,p2,p3,p4,p5); }
		#pragma endregion
		
		#pragma region Sql String Helper
		// Reuse one global std::string to prevent unnecessary allocs
		inline std::string& Sql() { static std::string sql; return sql; }
		
		// Generates:
		// template <typename P1>              char const* Sql(P1 p1)
		// template <typename P1, typename P2> char const* Sql(P1 p1, P2 p1) etc...
		#define PR_TYPENAME(n) typename P##n
		#define PR_PARAMS(n) P##n p##n
		#define PR_APPEND(n) .append(p##n)
		template <PR_SQLITE_REPEAT(1, PR_TYPENAME, PR_COMMA)> inline char const* Sql(PR_SQLITE_REPEAT(1, PR_PARAMS, PR_COMMA)) { Sql().resize(0); return Sql()PR_SQLITE_REPEAT(1,PR_APPEND,).c_str(); }
		template <PR_SQLITE_REPEAT(2, PR_TYPENAME, PR_COMMA)> inline char const* Sql(PR_SQLITE_REPEAT(2, PR_PARAMS, PR_COMMA)) { Sql().resize(0); return Sql()PR_SQLITE_REPEAT(2,PR_APPEND,).c_str(); }
		template <PR_SQLITE_REPEAT(3, PR_TYPENAME, PR_COMMA)> inline char const* Sql(PR_SQLITE_REPEAT(3, PR_PARAMS, PR_COMMA)) { Sql().resize(0); return Sql()PR_SQLITE_REPEAT(3,PR_APPEND,).c_str(); }
		template <PR_SQLITE_REPEAT(4, PR_TYPENAME, PR_COMMA)> inline char const* Sql(PR_SQLITE_REPEAT(4, PR_PARAMS, PR_COMMA)) { Sql().resize(0); return Sql()PR_SQLITE_REPEAT(4,PR_APPEND,).c_str(); }
		template <PR_SQLITE_REPEAT(5, PR_TYPENAME, PR_COMMA)> inline char const* Sql(PR_SQLITE_REPEAT(5, PR_PARAMS, PR_COMMA)) { Sql().resize(0); return Sql()PR_SQLITE_REPEAT(5,PR_APPEND,).c_str(); }
		template <PR_SQLITE_REPEAT(6, PR_TYPENAME, PR_COMMA)> inline char const* Sql(PR_SQLITE_REPEAT(6, PR_PARAMS, PR_COMMA)) { Sql().resize(0); return Sql()PR_SQLITE_REPEAT(6,PR_APPEND,).c_str(); }
		template <PR_SQLITE_REPEAT(7, PR_TYPENAME, PR_COMMA)> inline char const* Sql(PR_SQLITE_REPEAT(7, PR_PARAMS, PR_COMMA)) { Sql().resize(0); return Sql()PR_SQLITE_REPEAT(7,PR_APPEND,).c_str(); }
		template <PR_SQLITE_REPEAT(8, PR_TYPENAME, PR_COMMA)> inline char const* Sql(PR_SQLITE_REPEAT(8, PR_PARAMS, PR_COMMA)) { Sql().resize(0); return Sql()PR_SQLITE_REPEAT(8,PR_APPEND,).c_str(); }
		template <PR_SQLITE_REPEAT(9, PR_TYPENAME, PR_COMMA)> inline char const* Sql(PR_SQLITE_REPEAT(9, PR_PARAMS, PR_COMMA)) { Sql().resize(0); return Sql()PR_SQLITE_REPEAT(9,PR_APPEND,).c_str(); }
		#undef PR_TYPENAME
		#undef PR_PARAMS
		#undef PR_APPEND
		#pragma endregion
		
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		//#pragma region General functions
		
		// Assigns 'from' to 'to' by value. Works with arrays as well
		template <typename To, typename From> void Assign(To& to, From const& from)
		{
			to = static_cast<To>(from);
		}
		template <typename To, typename From, size_t Size> void Assign(To (&to)[Size], From const (&from)[Size])
		{
			for (int i = 0; i != Size; ++i)
				to[i] = static_cast<To>(from[i]);
		}
		
		// Assign a range to an array
		template <typename Array, typename RndIt> void Assign(Array& value, RndIt begin, RndIt end)
		{
			// stl-like arrays
			value.assign(begin, end);
		}
		template <typename Array, size_t Size, typename RndIt> void Assign(Array (&value)[Size], RndIt begin, RndIt end)
		{
			// C-like arrays
			if (end - begin > Size) throw Exception(SQLITE_MISMATCH, "buffer overflow in Assign()", false);
			for (size_t i = 0; begin != end; ++begin, ++i)
				Assign(value[i], *begin);
		}
		
		// Assign a pointer range
		template <typename Type> void Assign(Type* begin, Type* end, Type const* first, Type const* last)
		{
			if (last-first > end-begin) throw Exception(SQLITE_MISMATCH, "buffer overflow in Assign()", false);
			for (;first != last; ++first, ++begin)
				Assign(*begin, *first);
		}
		
		// A collection of string searching helper functions
		struct StrHelper
		{
			static bool Null(char c) { return c == 0; }
			template <typename Type> static bool False(Type const&) { return false; }
			
			// Returns a pointer into 'src' at the first occurrence of 'ch' or a pointer to the terminator
			template <typename Term> static char const* FindChar(char const* src, char ch, Term term)
			{
				for (;!term(*src) && *src != ch; ++src) {}
				return src;
			}
			static char const* FindChar(char const* src, char ch)
			{
				return FindChar(src, ch, Null);
			}
			
			// Returns a pointer to the first occurrence of any character in 'any_of_these'.
			template <typename Term> static char const* FindAny(char const* src, char const* any_of_these, Term term)
			{
				for (;!term(*src) && *FindChar(any_of_these, *src) == 0; ++src) {}
				return src;
			}
			static char const* FindAny(char const* src, char const* any_of_these)
			{
				return FindAny(src, any_of_these, Null);
			}
			
			// Returns a pointer to the start of 'substring' in 'src' or null if 'substring' is not found.
			template <typename Term> static char const* FindStr(char const* src, char const* substring, char const* sep, Term term)
			{
				for (;!term(*src); ++src)
				{
					// Seek to the next non-separator character
					for (; !term(*src) && *FindChar(sep, *src, Null) != 0; ++src) {}
					
					// Compare the word starting at 'src' with 'substring'
					char const *a = src, *b = substring;
					for (; !term(*a) && *b != 0 && *a == *b; ++a, ++b) {}
					if (*b == 0) break; // match
					
					// Seek to the separater character
					for (src = a; !term(*src) && *FindChar(sep, *src, Null) == 0; ++src) {}
				}
				return src;
			}
			static char const* FindStr(char const* src, char const* substring, char const* sep)
			{
				return FindStr(src, substring, sep, Null);
			}
			
			// Returns true if 'substring' is contained within 'src'
			static bool Contains(char const* src, char const* substring)
			{
				return *FindStr(src, substring, " ", Null) != 0;
			}
			
			// Returns a string containing the elements of 'cont' separated by 'sep'
			// 'value' returns a value to append to the list
			// 'filter' returns true if the 'ith' element in the container should be skipped
			template <typename Cont, typename Value, typename Filter> static std::string List(Cont const& cont, char const* sep, Value value, Filter filter)
			{
				std::string str;
				bool first = true;
				for (auto i = begin(cont), iend = end(cont); i != iend; ++i, first = false)
				{
					if (filter(*i)) continue;
					if (!first) str.append(sep);
					str.append(value(*i));
				}
				return str;
			}
			template <typename Cont, typename Value> static std::string List(Cont const& cont, char const* sep, Value value)
			{
				return List(cont, sep, value, False<decltype(cont[0])>);
			}
		};
		
		// Return the number of result columns in 'stmt'
		inline size_t ColumnCount(sqlite3_stmt* stmt)
		{
			PR_ASSERT(PR_SQL_ASSERTS, stmt != 0, "Invalid result object");
			return static_cast<size_t>(sqlite3_column_count(stmt));
		}
		
		// Return the column type for column number 'col' in 'stmt'
		inline char const* DeclType(sqlite3_stmt* stmt, int col)
		{
			PR_ASSERT(PR_SQL_ASSERTS, col >= 0 || col < (int)ColumnCount(stmt), "Invalid result object");
			return sqlite3_column_decltype(stmt, col);
		}
			
		// Return the data type for column number 'col'
		inline int DataType(sqlite3_stmt* stmt, int col)
		{
			PR_ASSERT(PR_SQL_ASSERTS, col >= 0 || col < (int)ColumnCount(stmt), "Invalid result object");
			return sqlite3_column_type(stmt, col);
		}
		
		// Return the name of column number 'col'
		inline char const* ColumnName(sqlite3_stmt* stmt, int col)
		{
			PR_ASSERT(PR_SQL_ASSERTS, col >= 0 || col < (int)ColumnCount(stmt), "Invalid result object");
			return sqlite3_column_name(stmt, col);
		}
		
		// Return the index of the column with name 'name'
		inline size_t ColumnIndex(sqlite3_stmt* stmt, char const* name)
		{
			PR_ASSERT(PR_SQL_ASSERTS, name, "Invalid column name");
			for (size_t i = 0, iend = ColumnCount(stmt); i != iend; ++i)
				if (strcmp(name, sqlite3_column_name(stmt, (int)i)) == 0)
					return i;
			
			throw Exception(SQLITE_NOTFOUND, "Column name not found", false);
		}
		
		// Returns true if column 'col' in 'stmt' is null
		inline bool IsNull(sqlite3_stmt* stmt, int col)
		{
			PR_ASSERT(PR_SQL_ASSERTS, col >= 0 || col < (int)ColumnCount(stmt), "Invalid result object");
			return DataType(stmt, col) == SQLITE_NULL;
		}
		
		// Returns the primary key values for 'item'
		template <typename PKArgs, typename DBRecord> inline PKArgs PrimaryKeys(DBRecord const& item)
		{
			TableMetaData<DBRecord> const& meta = DBRecord::Sqlite_TableMetaData();
			TableMetaData<DBRecord>::ColumnCont const& col = meta.PKs();
			
			PKArgs args;
			size_t col_count = col.size();
			
			// Generates:
			//if (col_count >= 1) col[0]->Get(&args.pk1, item);
			//if (col_count >= 2) col[1]->Get(&args.pk2, item); etc...
			#define PR_M(n) if (col_count >= n) col[n-1]->Get(item, args.pk##n);
			PR_SQLITE_REPEAT(PR_SQLITE_MAX_PRIMARY_KEYS, PR_M, PR_NEWLINE)
			#undef PR_M
			return args;
		}
		
		// Compile an sql string into an sqlite3 statement
		inline sqlite3_stmt* Compile(sqlite3* db, char const* sql_string)
		{
			PR_ASSERT(PR_SQL_ASSERTS, db, "Database invalid");
				
			sqlite3_stmt* stmt; char const* end = 0;
			int res = sqlite3_prepare_v2(db, sql_string, -1, &stmt, &end);
			if (res != SQLITE_OK) throw Exception(res, sqlite3_errmsg(db), false);
			return stmt;
		}
		//#pragma endregion
		
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		//#pragma region Sqlite parameter reading functions
		template <typename Type> inline Type& read_null(sqlite3_stmt*, int, Type& value)
		{
			return value = Type();
		}
		template <typename IntType> inline IntType& read_int(sqlite3_stmt* stmt, int col, IntType& value)
		{
			static_assert(std::tr1::is_integral<IntType>::value == true, "value must be an integer type");
			return value = static_cast<IntType>(sqlite3_column_int64(stmt, col)); // Sqlite returns 0 if this column is null
		}
		template <> inline bool& read_int<bool>(sqlite3_stmt* stmt, int col, bool& value)
		{
			return value = sqlite3_column_int64(stmt, col) != 0; // Sqlite returns 0 if this column is null
		}
		template <typename IntType> inline IntType& read_integer(sqlite3_stmt* stmt, int col, IntType& value)
		{
			return read_int(stmt, col, value); // Accept integer or int as a datatype
		}
		template <typename RealType> inline RealType& read_real(sqlite3_stmt* stmt, int col, RealType& value)
		{
			static_assert(std::tr1::is_floating_point<RealType>::value == true, "value must be a floating point type");
			return value = static_cast<RealType>(sqlite3_column_double(stmt, col)); // Sqlite returns 0.0 if this column is null
		}
		inline char* read_text(sqlite3_stmt* stmt, int col, size_t max_length, char* value, size_t& length)
		{
			PR_ASSERT(PR_SQL_ASSERTS, max_length > 0 && value != 0, "Zero-length buffers not allowed");
			
			// Sqlite returns null if this column is null
			CPtr ptr; ptr.uc = sqlite3_column_text(stmt, col); // have to call this first
			size_t len = static_cast<size_t>(sqlite3_column_bytes(stmt, col));
			if (len > max_length) throw Exception(SQLITE_MISMATCH, "Column data exceeds provided buffer size", false);
			Assign(value, value + max_length, ptr.c, ptr.c + len);
			length = len;
			return value;
		}
		inline wchar_t* read_text(sqlite3_stmt* stmt, int col, size_t max_length, wchar_t* value, size_t& length)
		{
			PR_ASSERT(PR_SQL_ASSERTS, max_length > 0 && value != 0, "Zero-length buffers not allowed");
			
			// Sqlite returns null if this column is null
			CPtr ptr; ptr.v = sqlite3_column_text16(stmt, col); // have to call this first
			size_t len = static_cast<size_t>(sqlite3_column_bytes(stmt, col));
			if (len > max_length) throw Exception(SQLITE_MISMATCH, "Column data exceeds provided buffer size", false);
			Assign(value, value + max_length, ptr.w, ptr.w + len/sizeof(wchar_t));
			length = len;
			return value;
		}
		template <size_t Size> inline char (&read_text(sqlite3_stmt* stmt, int col, char (&value)[Size]))[Size]
		{
			size_t len; read_text(stmt, col, Size, value, len); return value;
		}
		template <size_t Size> inline wchar_t (&read_text(sqlite3_stmt* stmt, int col, wchar_t (&value)[Size]))[Size]
		{
			size_t len; read_text(stmt, col, Size, value, len); return value;
		}
		template <typename StrType> inline StrType& read_text(sqlite3_stmt* stmt, int col, StrType& value, char)
		{
			CPtr ptr; ptr.uc = sqlite3_column_text(stmt, col); // have to call this first
			size_t length = static_cast<size_t>(sqlite3_column_bytes(stmt, col));
			Assign(value, ptr.c, ptr.c + length);
			return value;
		}
		template <typename StrType> inline StrType& read_text(sqlite3_stmt* stmt, int col, StrType& value, wchar_t)
		{
			// Sqlite returns null if this column is null
			CPtr ptr; ptr.v = sqlite3_column_text16(stmt, col); // have to call this first
			size_t length = static_cast<size_t>(sqlite3_column_bytes16(stmt, col));
			Assign(value, ptr.w, ptr.w + length/sizeof(wchar_t));
			return value;
		}
		template <typename StrType> inline StrType& read_text(sqlite3_stmt* stmt, int col, StrType& value)
		{
			return read_text(stmt, col, value, StrType::value_type());
		}
		inline void* read_blob(sqlite3_stmt* stmt, int col, size_t max_size, void* data, size_t& length)
		{
			// Sqlite returns null if this column is null
			byte const* ptr = static_cast<byte const*>(sqlite3_column_blob(stmt, col)); // have to call this first
			size_t len = static_cast<size_t>(sqlite3_column_bytes(stmt, col));
			if (len > max_size) throw Exception(SQLITE_MISMATCH, "Column data exceeds provided buffer size", false);
			byte* in = static_cast<byte*>(data);
			Assign(in, in + max_size, ptr, ptr + len);
			length = len;
			return data;
		}
		template <typename BlobType> inline BlobType& read_blob(sqlite3_stmt* stmt, int col, BlobType& value)
		{
			static_assert(std::tr1::is_pod<BlobType>::value, "Blobs must be POD");
			
			// Sqlite returns null if this column is null
			BlobType const* ptr = static_cast<BlobType const*>(sqlite3_column_blob(stmt, col)); // have to call this first
			size_t len = static_cast<size_t>(sqlite3_column_bytes(stmt, col));
			if (len != 0 && len != sizeof(BlobType)) throw Exception(SQLITE_MISMATCH, "Sqlite3 blob size does not match the size of 'value'", false);
			Assign(value, ptr == 0 ? BlobType() : *ptr);
			return value;
		}
		template <typename Elem, size_t Size> inline Elem (&read_blob(sqlite3_stmt* stmt, int col, Elem (&value)[Size]))[Size]
		{
			static_assert(std::tr1::is_pod<Elem>::value, "Blobs must be POD");
			
			// Sqlite returns null if this column is null
			Elem const* ptr = static_cast<Elem const*>(sqlite3_column_blob(stmt, col)); // have to call this first
			size_t len = static_cast<size_t>(sqlite3_column_bytes(stmt, col));
			size_t count = len / sizeof(Elem);
			if (count * sizeof(Elem) != len) throw Exception(SQLITE_MISMATCH, "Blob size is not an exact multiple of the buffer element type", false);
			Assign(value, ptr, ptr + count);
			return value;
		}
		template <typename Cont> inline Cont& read_blobcont(sqlite3_stmt* stmt, int col, Cont& value)
		{
			static_assert(std::tr1::is_pod<Cont::value_type>::value, "Blobs must be POD");
			static_assert(std::tr1::is_same<Cont::iterator::iterator_category, std::random_access_iterator_tag>::value, "Cont must be a random access container");
			
			// Sqlite returns null if this column is null
			Cont::value_type const* ptr = static_cast<Cont::value_type const*>(sqlite3_column_blob(stmt, col)); // have to call this first
			size_t len = static_cast<size_t>(sqlite3_column_bytes(stmt, col));
			size_t count = len / sizeof(Cont::value_type);
			if (count * sizeof(Cont::value_type) != len) throw Exception(SQLITE_MISMATCH, "Blob size is not an exact multiple of the buffer element type", false);
			Assign(value, ptr, ptr + count);
			return value;
		}
		//#pragma endregion
		
		//#pragma region Sqlite parameter binding functions
		inline void bind_null(sqlite3_stmt* stmt, int col)
		{
			int res = sqlite3_bind_null(stmt, col);
			if (res != SQLITE_OK) throw Exception(res, "Failed to bind null", false);
		}
		template <typename IntType> inline void bind_integer(sqlite3_stmt* stmt, int col, IntType value)
		{
			static_assert(std::tr1::is_integral<IntType>::value, "value must be an integer type");
			int res = sqlite3_bind_int64(stmt, col, value);
			if (res != SQLITE_OK) throw Exception(res, "Failed to bind int", false);
		}
		template <typename RealType> inline void bind_real(sqlite3_stmt* stmt, int col, RealType value)
		{
			static_assert(std::tr1::is_floating_point<RealType>::value, "value must be a floating point type");
			int res = sqlite3_bind_double(stmt, col, value);
			if (res != SQLITE_OK) throw Exception(res, "Failed to bind real", false);
		}
		inline void bind_text(sqlite3_stmt* stmt, int col, char const* value, int len) // use len = -1 for null terminated
		{
			// Note passing value == 0 will bind null to the column
			int res = sqlite3_bind_text(stmt, col, value, len, SQLITE_TRANSIENT);
			if (res != SQLITE_OK) throw Exception(res, "Failed to bind text", false);
		}
		inline void bind_text(sqlite3_stmt* stmt, int col, wchar_t const* value, int len) // use len = -1 for null terminated
		{
			// Note passing value == 0 will bind null to the column
			if (len != -1) len *= sizeof(wchar_t);
			int res = sqlite3_bind_text16(stmt, col, value, -1, SQLITE_TRANSIENT);
			if (res != SQLITE_OK) throw Exception(res, "Failed to bind text16", false);
		}
		template <typename StrType> inline void bind_text(sqlite3_stmt* stmt, int col, StrType const& value)
		{
			bind_text(stmt, col, value.c_str(), (int)value.size());
		}
		template <> inline void bind_text(sqlite3_stmt* stmt, int col, char const* const& value)
		{
			// Note passing value == 0 will bind null to the column
			bind_text(stmt, col, value, -1);
		}
		template <> inline void bind_text(sqlite3_stmt* stmt, int col, wchar_t const* const& value)
		{
			// Note passing value == 0 will bind null to the column
			bind_text(stmt, col, value, -1);
		}
		template <size_t Size> inline void bind_text(sqlite3_stmt* stmt, int col, char const (&value)[Size])
		{
			bind_text(stmt, col, value, Size);
		}
		template <size_t Size> inline void bind_text(sqlite3_stmt* stmt, int col, wchar_t const (&value)[Size])
		{
			bind_text(stmt, col, value, Size);
		}
		inline void bind_blob(sqlite3_stmt* stmt, int col, void const* blob, size_t length)
		{
			// Note passing blob == 0 will bind null to the column
			int res = sqlite3_bind_blob(stmt, col, blob, (int)length, SQLITE_TRANSIENT);
			if (res != SQLITE_OK) throw Exception(res, "Failed to bind blob", false);
		}
		template <typename BlobType> inline void bind_blob(sqlite3_stmt* stmt, int col, BlobType const& value)
		{
			static_assert(std::tr1::is_pod<BlobType>::value, "Blobs must be POD");
			bind_blob(stmt, col, &value, sizeof(value));
		}
		template <typename Cont> inline void bind_blobcont(sqlite3_stmt* stmt, int col, Cont const& value)
		{
			static_assert(std::tr1::is_pod<Cont::value_type>::value, "Cont must contain POD elements");
			static_assert(std::tr1::is_same<Cont::iterator::iterator_category, std::random_access_iterator_tag>::value, "Cont must be a random access container");
			
			auto first = std::begin(value), last  = std::end(value);
			size_t length = (last - first) * sizeof(*first);
			bind_blob(stmt, col, length == 0 ? 0 : &*first, length);
		}
		//#pragma endregion
		
		////#pragma region Sqlite parameter binding overloads
		//// Type mapping Bind functions (feel free to add your own if needed)
		//inline void Bind(sqlite3_stmt*     , int    , Null const&              ) {}
		//inline void Bind(sqlite3_stmt* stmt, int col, bool value               ) { bind_integer(stmt, col, value); }
		//inline void Bind(sqlite3_stmt* stmt, int col, char value               ) { bind_integer(stmt, col, value); }
		//inline void Bind(sqlite3_stmt* stmt, int col, unsigned char value      ) { bind_integer(stmt, col, value); }
		//inline void Bind(sqlite3_stmt* stmt, int col, short value              ) { bind_integer(stmt, col, value); }
		//inline void Bind(sqlite3_stmt* stmt, int col, unsigned short value     ) { bind_integer(stmt, col, value); }
		//inline void Bind(sqlite3_stmt* stmt, int col, int value                ) { bind_integer(stmt, col, value); }
		//inline void Bind(sqlite3_stmt* stmt, int col, unsigned int value       ) { bind_integer(stmt, col, value); }
		//inline void Bind(sqlite3_stmt* stmt, int col, sqlite3_int64 value      ) { bind_integer(stmt, col, value); }
		//inline void Bind(sqlite3_stmt* stmt, int col, sqlite3_uint64 value     ) { bind_integer(stmt, col, reinterpret_cast<sqlite3_int64 const&>(value)); }
		//inline void Bind(sqlite3_stmt* stmt, int col, float value              ) { bind_real(stmt, col, value); }
		//inline void Bind(sqlite3_stmt* stmt, int col, double value             ) { bind_real(stmt, col, value); }
		//inline void Bind(sqlite3_stmt* stmt, int col, std::string const& value ) { bind_text(stmt, col, value.c_str(), (int)value.size()); }
		//inline void Bind(sqlite3_stmt* stmt, int col, std::wstring const& value) { bind_text(stmt, col, value.c_str(), (int)value.size()); }
		//template <size_t Size>                inline void Bind(sqlite3_stmt* stmt, int col, char const (&value)[Size])      { bind_text(stmt, col, value, Size); }
		//template <size_t Size>                inline void Bind(sqlite3_stmt* stmt, int col, wchar_t const (&value)[Size])   { bind_text(stmt, col, value, Size); }
		//template <typename Type, size_t Size> inline void Bind(sqlite3_stmt* stmt, int col, Type const (&value)[Size])      { bind_blob(stmt, col, std::begin(value), std::end(value)); }
		//template <typename Type>              inline void Bind(sqlite3_stmt* stmt, int col, std::vector<Type> const& value) { bind_blob(stmt, col, std::begin(value), std::end(value)); }
		//template <typename Type>              inline void Bind(sqlite3_stmt* stmt, int col, Type const& value)              { bind_blob(stmt, col, &value, sizeof(value)); }
		
		//// Type mapping Read functions (feel free to add your own if needed)
		//inline void Read(sqlite3_stmt* stmt, int col, bool& value            ) { int v; read_int(stmt, col, v); value = v != 0; }
		//inline void Read(sqlite3_stmt* stmt, int col, char& value            ) { read_int(stmt, col, value); }
		//inline void Read(sqlite3_stmt* stmt, int col, unsigned char& value   ) { read_int(stmt, col, value); }
		//inline void Read(sqlite3_stmt* stmt, int col, short& value           ) { read_int(stmt, col, value); }
		//inline void Read(sqlite3_stmt* stmt, int col, unsigned short& value  ) { read_int(stmt, col, value); }
		//inline void Read(sqlite3_stmt* stmt, int col, int& value             ) { read_int(stmt, col, value); }
		//inline void Read(sqlite3_stmt* stmt, int col, unsigned int& value    ) { read_int(stmt, col, value); }
		//inline void Read(sqlite3_stmt* stmt, int col, sqlite3_int64& value   ) { read_int(stmt, col, value); }
		//inline void Read(sqlite3_stmt* stmt, int col, sqlite3_uint64& value  ) { read_int(stmt, col, reinterpret_cast<sqlite3_int64&>(value)); }
		//inline void Read(sqlite3_stmt* stmt, int col, float& value           ) { read_real(stmt, col, value); }
		//inline void Read(sqlite3_stmt* stmt, int col, double& value          ) { read_real(stmt, col, value); }
		//inline void Read(sqlite3_stmt* stmt, int col, std::string& value     ) { read_text(stmt, col, value); }
		//inline void Read(sqlite3_stmt* stmt, int col, std::wstring& value    ) { read_text16(stmt, col, value); }
		//template <size_t Size>                inline void Read(sqlite3_stmt* stmt, int col, char (&value)[Size])      { size_t len; read_text(stmt, col, Size, &value[0], len); }
		//template <size_t Size>                inline void Read(sqlite3_stmt* stmt, int col, wchar_t (&value)[Size])   { size_t len; read_text16(stmt, col, Size, &value[0], len); }
		//template <typename Type, size_t Size> inline void Read(sqlite3_stmt* stmt, int col, Type (&value)[Size])      { size_t len; read_blob(stmt, col, sizeof(value), &value[0], len); }
		//template <typename Type>              inline void Read(sqlite3_stmt* stmt, int col, std::vector<Type>& value) { ReadArray(stmt, col, value); }
		//template <typename Type>              inline void Read(sqlite3_stmt* stmt, int col, Type& value)              { read_blob(stmt, col, value); }
		
		// Bind a set of primary keys to 'stmt' starting at parameter index '1+ofs'
		template <typename DBRecord, typename PKArgs> void BindPKs(sqlite3_stmt* stmt, PKArgs const& pks, int ofs = 0)
		{
			PR_ASSERT(PR_SQL_ASSERTS, ofs >= 0, "parameter binding indices start at 1 so 'ofs' must be >= 0");
			TableMetaData<DBRecord> const& meta = DBRecord::Sqlite_TableMetaData();
			TableMetaData<DBRecord>::ColumnCont const& col = meta.PKs();
			size_t col_count = col.size();
			
			// Generates:
			// if (col_count >= 1) col[0]->Bind(stmt, ofs+1, &pks.pk1);
			// if (col_count >= 2) col[1]->Bind(stmt, ofs+2, &pks.pk2); etc...
			#define PR_M(n) if (col_count >= n) col[n-1]->Bind(stmt, ofs+n, pks.pk##n);
			PR_SQLITE_REPEAT(PR_SQLITE_MAX_PRIMARY_KEYS, PR_M, PR_NEWLINE)
			#undef PR_M
		}
		
		// Read and set all columns in 'item' from 'stmt'
		template <typename DBRecord> inline DBRecord& Read(sqlite3_stmt* stmt, DBRecord& item)
		{
			TableMetaData<DBRecord> const& meta = DBRecord::Sqlite_TableMetaData();
			
			int col = 0;
			for (auto i = begin(meta.Columns()), iend = end(meta.Columns()); i != iend; ++i)
				(*i)->Read(stmt, col++, item);
			
			return item;
		}
		template <typename DBRecord> inline DBRecord Read(sqlite3_stmt* stmt)
		{
			DBRecord item;
			return Read(stmt, item);
		}
		//#pragma endregion
		
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		// Column meta data. Including the ability to read/write values in 'RecordType'
		// http://www.sqlite.org/syntaxdiagrams.html#column-def
		// http://www.sqlite.org/syntaxdiagrams.html#column-constraint
		template <typename RecordType> struct ColumnMetaData
		{
			char const* Name;
			char const* DataType;
			char const* Constraints;
			bool        IsNotNull;
			bool        IsPK;
			bool        IsAutoInc;
			bool        IsCollate;
			
			ColumnMetaData(char const* name, char const* datatype, char const* column_constraints)
			:Name        (name)
			,DataType    (datatype)
			,Constraints (column_constraints)
			,IsNotNull   (StrHelper::Contains(column_constraints, "not null"))
			,IsPK        (StrHelper::Contains(column_constraints, "primary key"))
			,IsAutoInc   (StrHelper::Contains(column_constraints, "autoincrement"))
			,IsCollate   (StrHelper::Contains(column_constraints, "collate"))
			{
				if (IsAutoInc && !IsPK)
					throw Exception(SQLITE_MISUSE, "Only a primary key column can have the auto increment constraint", false);
			}
			
			// Return the column definition for this column
			std::string ColumnDef() const
			{
				return Sql(Name," ",DataType," ",Constraints);
			}
			
			// Set the value of this column in 'item' from 'value'.
			template <typename ValueType> void Set(RecordType& item, ValueType const& value) const
			{
				PR_ASSERT(PR_SQL_ASSERTS, typeid(ValueType) == ColumnTypeInfo(), "value is not the correct type for this column");
				SetImpl(item, &value);
			}
			
			// Get the value of this column from 'item' into 'value'.
			template <typename ValueType> void Get(RecordType const& item, ValueType& value) const
			{
				PR_ASSERT(PR_SQL_ASSERTS, typeid(ValueType) == ColumnTypeInfo(), "value is not the correct type for this column");
				GetImpl(item, &value);
			}
			
			// Bind 'value' (assumed to be of type ColumnType) to 'stmt'
			template <typename ValueType> void Bind(sqlite3_stmt* stmt, int col, ValueType const& value) const
			{
				PR_ASSERT(PR_SQL_ASSERTS, typeid(ValueType) == ColumnTypeInfo(), "value is not the correct type for this column");
				BindImpl(stmt, col, &value);
			}
			
			// Set 'value' (assumed to be of type ColumnType) from 'stmt'
			template <typename ValueType> void Read(sqlite3_stmt* stmt, int col, ValueType& value) const
			{
				PR_ASSERT(PR_SQL_ASSERTS, typeid(ValueType) == ColumnTypeInfo(), "value is not the correct type for this column");
				ReadImpl(stmt, col, &value);
			}
			
			// Bind the value of this column in 'item' to a query parameter
			virtual void Bind(sqlite3_stmt* stmt, int col, RecordType const& item) const = 0;
			
			// Set the member in 'item' to the value of this column in a query result
			virtual void Read(sqlite3_stmt* stmt, int col, RecordType& item) const = 0;
			
		protected:
			virtual std::type_info const& ColumnTypeInfo() const = 0;
			
			// Set the value of this column in 'item' from 'value'.
			// 'value' is expected to be of type ColumnType.
			virtual void SetImpl(RecordType& item, void const* value) const = 0;
			
			// Get the value of this column from 'item' into 'value'.
			// 'value' is expected to be of type ColumnType.
			virtual void GetImpl(RecordType const& item, void* value) const = 0;
			
			// Bind 'value' (assumed to be of type ColumnType) to 'stmt'
			virtual void BindImpl(sqlite3_stmt* stmt, int col, void const* value) const = 0;
			
			// Set 'value' (assumed to be of type ColumnType) from 'stmt'
			virtual void ReadImpl(sqlite3_stmt* stmt, int col, void* value) const = 0;
			
			ColumnMetaData(ColumnMetaData const&); // no copying
			ColumnMetaData& operator=(ColumnMetaData const&);
		};
		
		// This implementation of ColumnMetaData uses a provided adapter struct
		// with static methods to convert the member in the record to the column type.
		template <typename RecordType, typename Adapter>
		struct ColumnMetaDataImpl :ColumnMetaData<RecordType>
		{
			//typedef typename Adapter::FieldType  FieldType;  // The type of the member in 'RecordType'
			typedef typename Adapter::ColumnType ColumnType; // The type that the member is converted to when stored in the db
			
			ColumnMetaDataImpl(char const* name, char const* datatype, char const* column_constraints)
			:ColumnMetaData(name, datatype, column_constraints)
			{}
			
			// Bind the value of this column in 'item' to a query parameter
			void Bind(sqlite3_stmt* stmt, int col, RecordType const& item) const
			{
				ColumnType value;
				Adapter::Get(item, value);
				Adapter::Bind(stmt, col, value);
			}
			
			// Set the member in 'item' to the value of this column in a query result
			void Read(sqlite3_stmt* stmt, int col, RecordType& item) const
			{
				ColumnType value;
				Adapter::Read(stmt, col, value);
				Adapter::Set(item, value);
			}
		
		protected:
			// Returns the type info for the column type
			std::type_info const& ColumnTypeInfo() const
			{
				return typeid(ColumnType);
			}
			
			// Set the value of this column in 'item' from 'value'.
			// 'value' is expected to be of type ColumnType.
			void SetImpl(RecordType& item, void const* value) const
			{
				Adapter::Set(item, *static_cast<ColumnType const*>(value));
			}
			
			// Get the value of this column from 'item' into 'value'.
			// 'value' is expected to be of type ColumnType.
			void GetImpl(RecordType const& item, void* value) const
			{
				Adapter::Get(item, *static_cast<ColumnType*>(value));
			}
			
			// Bind 'value' (assumed to be of type ColumnType) to 'stmt'
			void BindImpl(sqlite3_stmt* stmt, int col, void const* value) const
			{
				Adapter::Bind(stmt, col, *static_cast<ColumnType const*>(value));
			}
			
			// Set 'value' (assumed to be of type ColumnType) from 'stmt'
			void ReadImpl(sqlite3_stmt* stmt, int col, void* value) const
			{
				Adapter::Read(stmt, col, *static_cast<ColumnType*>(value));
			}
		};
		
		// Meta data for a table
		// http://www.sqlite.org/syntaxdiagrams.html#create-table-stmt
		// http://www.sqlite.org/syntaxdiagrams.html#table-constraint
		template <typename RecordType> class TableMetaData
		{
		public:
			typedef RecordType                      TableType;    // The class/struct that this table is based on
			typedef ColumnMetaData<RecordType>      ColMetaData;  // The type of the column meta data for this table
			typedef std::vector<ColMetaData const*> ColumnCCont;  // A container of immutable column meta data
			typedef std::vector<ColMetaData*>       ColumnCont;   // A container of column meta data
			
		private:
			char const*  m_table_name;        // The name of the table
			char const*  m_table_constraints; // Table constraints
			ColumnCont   m_col;               // All columns of this table (including pks, autoincs, etc) (owns the allocations)
			ColumnCont   m_pks;               // The primary keys of this table
			ColumnCont   m_npks;              // The columns that aren't primary keys
			ColumnCont   m_ninc;              // The columns that aren't autoincrement columns
			ColMetaData* m_autoinc;           // The auto increment column (if there is one, null otherwise)
			char const*  m_pk_col_names;      // A pointer into 'm_table_constraints' to the list of primary key column names
			
			// Add a column to the meta data
			void AddColumn(ColMetaData* col)
			{
				PR_ASSERT(PR_SQL_ASSERTS, !col->IsAutoInc || m_autoinc == 0, "SQLite only allows one auto increment column");
				
				// If the primary keys where given as a table constraint, set the IsPK of
				// the columns with names matching the list given in the constraint.
				if (m_pk_col_names != 0)
				{
					auto term = [](char c){ return c == 0 || c == ')'; };
					col->IsPK = !term(*StrHelper::FindStr(m_pk_col_names, col->Name, " ,", term));
				}
				
				m_col.push_back(col);
				if (col->IsPK     ) m_pks.push_back(col); else m_npks.push_back(col);
				if (col->IsAutoInc) m_autoinc = col;      else m_ninc.push_back(col);
			}
			
		public:
			
			TableMetaData(char const* table_name, char const* table_constraints)
			:m_table_name(table_name)
			,m_table_constraints(table_constraints)
			,m_col()
			,m_pks()
			,m_npks()
			,m_autoinc(0)
			,m_pk_col_names(0)
			{
				// Look for a primary key constraint in the table constraints
				// and store a pointer to it so that we can mark columns as primary
				// keys in 'AddColumn'
				if (m_table_constraints != 0 && *m_table_constraints != 0)
				{
					char const *pk;
					char const pk_const[] = "primary key";
					pk = StrHelper::FindStr(m_table_constraints, pk_const, " ", StrHelper::Null);
					if (*pk != 0) pk = StrHelper::FindChar(pk + sizeof(pk_const) - 1, '(', StrHelper::Null);
					if (*pk != 0) m_pk_col_names = pk + 1;
				}
			}
			~TableMetaData()
			{
				for (auto i = begin(m_col), iend = end(m_col); i != iend; ++i)
					delete *i;
			}
			
			// Accessors
			char const*        TableName() const  { return m_table_name; }
			ColumnCont const&  Columns() const    { return m_col; }
			ColumnCont const&  PKs() const        { return m_pks; }
			ColumnCont const&  NonPKs() const     { return m_npks; }
			ColumnCont const&  NonAutoInc() const { return m_ninc; }
			ColMetaData const* AutoInc() const    { return m_autoinc; }
			
			// Return the column matching 'column_name'
			ColMetaData const* Column(char const* column_name) const
			{
				for (auto i = begin(m_col), iend = end(m_col); i != iend; ++i)
					if (strcmp((*i)->Name, column_name) == 0) return *i;
				return 0;
			}
			
			// Return the number of columns in this table
			size_t ColumnCount() const
			{
				return m_col.size();
			}
			
			// Add a column based on a custom adapter between the member type and the column type
			template <typename Adapter> void AddColumn(char const* name, char const* datatype, char const* constraints)
			{
				AddColumn(new ColumnMetaDataImpl<RecordType, Adapter>(name, datatype, constraints));
			}
			
			// Return a string containing the declarations for each column
			std::string TableDecl() const
			{
				std::string str;
				str.append(StrHelper::List(m_col, ",\n", [](ColMetaData const* c){return c->ColumnDef();}));
				if (m_table_constraints && *m_table_constraints != 0)
				{
					if (!str.empty()) str.append(",\n");
					str.append(m_table_constraints);
				}
				return str;
			}
			
			// Return a constraint string for the primary keys of this table.
			// i.e. select * from Table where {Key1 = ? and Key2 = ?}<- this bit
			std::string PKConstraints() const
			{
				return StrHelper::List(m_pks, " and ", [](ColMetaData const* c){return std::string(c->Name).append(" = ?");});
			}
			
		private:
			TableMetaData(TableMetaData const&); // no copying
			TableMetaData& operator=(TableMetaData const&);
		};
		
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		// A wrapper for an iterative result of an SQL query
		class Query
		{
			mutable sqlite3_stmt* m_stmt; // Sqlite managed memory for this query. Mutable because the interface isn't const correct
			bool m_row_end;               // True once the last row has been read
			
			Query(Query const&); // no copying
			Query& operator=(Query const&);
			
		public:
			Query(sqlite3* db, char const* sql_string)
			:m_stmt(Compile(db, sql_string))
			,m_row_end(false)
			{}
			explicit Query(sqlite3_stmt *stmt)
			:m_stmt(stmt)
			,m_row_end(false)
			{}
			Query(Query&& rhs)
			:m_stmt(rhs.m_stmt)
			,m_row_end(rhs.m_row_end)
			{
				rhs.m_stmt = 0;
			}
			~Query()
			{
				try { Finalize(); } catch (...) {}
			}
			Query& operator=(Query&& rhs)
			{
				if (this == &rhs) return *this;
				m_stmt      = rhs.m_stmt;
				m_row_end   = rhs.m_row_end;
				rhs.m_stmt  = 0;
				return *this;
			}
			
			// Call when done with this query
			void Finalize()
			{
				if (!m_stmt) return;
				
				// After 'sqlite3_finalize()', it is illegal to use m_stmt. So save the db handle here
				sqlite3* db = sqlite3_db_handle(m_stmt);
				int res = sqlite3_finalize(m_stmt); m_stmt = 0; // Ensure no use of 'm_stmt'
				if (res != SQLITE_OK) throw Exception(res, sqlite3_errmsg(db), false);
			}
			
			// Allow the query wrapper to convert to the sqlite statement handle
			operator sqlite3_stmt*() const
			{
				PR_ASSERT(PR_SQL_ASSERTS, m_stmt != 0, "Invalid query object");
				return m_stmt;
			}
			
			// Return the number of parameters in this statement
			int ParmCount() const
			{
				PR_ASSERT(PR_SQL_ASSERTS, m_stmt != 0, "Invalid query object");
				return sqlite3_bind_parameter_count(m_stmt);
			}
			
			// Return the index for the parameter named 'name'
			int ParmIndex(char const* name)
			{
				PR_ASSERT(PR_SQL_ASSERTS, m_stmt != 0, "Invalid query object");
				int idx = sqlite3_bind_parameter_index(m_stmt, name);
				if (idx == 0) throw Exception(SQLITE_ERROR, "Parameter name not found", false);
				return idx;
			}
			
			// Reset the prepared statement object back to its initial state, ready to be re-executed.
			void Reset()
			{
				PR_ASSERT(PR_SQL_ASSERTS, m_stmt != 0, "Invalid query object");
				int res = sqlite3_reset(m_stmt);
				if (res != SQLITE_OK) throw Exception(res, sqlite3_errmsg(sqlite3_db_handle(m_stmt)), false);
			}
			
			// Iterate to the next row in the result
			// Returns true if there are more rows available
			bool Step()
			{
				PR_ASSERT(PR_SQL_ASSERTS, m_stmt != 0, "Invalid query object");
				int res = sqlite3_step(m_stmt);
				switch (res)
				{
				default: throw Exception(res, sqlite3_errmsg(sqlite3_db_handle(m_stmt)), false);
				case SQLITE_DONE: m_row_end = true; return false;
				case SQLITE_ROW: return true;
				}
			}
			
			// Returns true when the last row has been read
			bool RowEnd() const
			{
				PR_ASSERT(PR_SQL_ASSERTS, m_stmt != 0, "Invalid query object");
				return m_row_end;
			}
			
			// Returns the number of rows changed as a result of the last 'step()'
			int RowsChanged() const
			{
				return sqlite3_changes(sqlite3_db_handle(m_stmt));
			}
			
			// Returns the number of columns in the result of this query
			size_t ColumnCount() const
			{
				return pr::sqlite::ColumnCount(m_stmt);
			}
			//
			//// Return a value for a column for the current row
			//template <typename Type> Type& ColumnValue(int col, Type& type) const
			//{
			//	PR_ASSERT(PR_SQL_ASSERTS, col < ColumnCount(), "Column index out of range");
			//	pr::sqlite::Read(m_stmt, col, type);
			//	return type;
			//}
			//template <typename Type> Type  ColumnValue(int col) const
			//{
			//	Type value;
			//	return ColumnValue(col, value);
			//}
		};
		
		// A specialised query used for inserting objects into a db
		template <typename DBRecord> struct InsertCmd :Query
		{
			// Returns the sql string for the insert command for type 'DBRecord'
			static char const* SqlString(EOnConstraint::Type on_constraint = EOnConstraint::Reject)
			{
				TableMetaData<DBRecord> const& meta = DBRecord::Sqlite_TableMetaData();
				typedef ColumnMetaData<DBRecord> const* ColPtr;
				
				char const* cons = "";
				if      (on_constraint == EOnConstraint::Reject ) cons = "";
				else if (on_constraint == EOnConstraint::Ignore ) cons = "or ignore ";
				else if (on_constraint == EOnConstraint::Replace) cons = "or replace ";
				else throw Exception(SQLITE_MISUSE, "Unknown OnConstraint behaviour", false);
				
				return Sql(
					"insert ",cons,"into ",meta.TableName()," (",
					StrHelper::List(meta.NonAutoInc(), ",", [](ColPtr c){return c->Name;}),
					") values (",
					StrHelper::List(meta.NonAutoInc(), ",", [](ColPtr){return "?";}),
					")");
			}
			
			// Creates a compiled query for inserting an object of type 'DBRecord' into a database.
			// Users can then bind values, and run the query repeatedly to insert multiple items
			explicit InsertCmd(sqlite3* db, EOnConstraint::Type on_constraint = EOnConstraint::Reject)
			:Query(db, SqlString(on_constraint))
			{}
			
			// Bind the values in 'item' to this insert query making it ready for running
			void Bind(DBRecord const& item)
			{
				TableMetaData<DBRecord> const& meta = DBRecord::Sqlite_TableMetaData();
				
				// Bind the item values
				int idx = 1; // binding parameters are indexed from 1
				for (auto i = begin(meta.NonAutoInc()), iend = end(meta.NonAutoInc()); i != iend; ++i)
					(*i)->Bind(*this, idx++, item);
			}
			
			// Run the insert command. Call 'Reset()' before running the command again
			int Run()
			{
				Step(); PR_ASSERT(PR_SQL_ASSERTS, RowEnd(), "Insert returned more than one row");
				return RowsChanged();
			}
		};
		
		// A specialised query used for getting objects from the db
		template <typename DBRecord> struct GetCmd :Query
		{
			// Returns the sql string for the get command for type 'DBRecord'
			static char const* SqlString()
			{
				TableMetaData<DBRecord> const& meta = DBRecord::Sqlite_TableMetaData();
				return Sql("select * from ",meta.TableName()," where ",meta.PKConstraints());
			}
			
			// Create a compiled query for getting an object of type 'DBRecord' from the database.
			// Users can then bind primary keys, and run the query repeatedly to get multiple items.
			explicit GetCmd(sqlite3* db)
			:Query(db, SqlString())
			{}
			
			// Bind primary keys to this get query
			template <typename PKArgs> void Bind(PKArgs const& pks)
			{
				pr::sqlite::BindPKs<DBRecord>(*this, pks);
			}
			
			// Run the get command. Call 'Reset()' before running the command again
			DBRecord& Get(DBRecord& item)
			{
				Step();
				return pr::sqlite::Read(*this, item);
			}
			
			// If the query finds an item, read it into 'item' and return true, false otherwise.
			bool Find(DBRecord& item)
			{
				Step();
				if (RowEnd()) return false;
				pr::sqlite::Read(*this, item);
				return true;
			}
		};
		
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		// Wrapper for a specific table in the database
		template <typename DBRecord> class DBTable
		{
			Database* m_db;
			
		public:
			explicit DBTable(Database* db) :m_db(db) {}
			
			// Insert an item into the database.
			// Note: insert will *NOT* change the primary keys/autoincrement members
			// of 'item' make sure you call 'Get()' to get the updated item.
			int Insert(DBRecord const& item, EOnConstraint::Type on_constraint = EOnConstraint::Reject)
			{
				PR_ASSERT(PR_SQL_ASSERTS, m_db->IsOpen(), "Database closed");
				InsertCmd<DBRecord> insert(*m_db, on_constraint); // Create the sql query
				insert.Bind(item);   // Bind 'item' to it
				return insert.Run(); // Run the query
			}
			
			// Inserts 'item' into the database and then sets 'at_row' to the row at which
			// 'item' was inserted. This is typically used to update the primary key in 'item'
			// which, for integer autoincrement columns, is normally the last row id.
			template <typename RowId> int Insert(DBRecord const& item, RowId& last_row_id, EOnConstraint::Type on_constraint = EOnConstraint::Reject)
			{
				PR_ASSERT(PR_SQL_ASSERTS, m_db->IsOpen(), "Database closed");
				int res = Insert(item, on_constraint);
				last_row_id = static_cast<RowId>(m_db->LastRowId());
				return res;
			}
			
			// Delete an item from the database
			// Use pr::sqlite::PKs() to create 'PRArgs'
			template <typename PKArgs> int Delete(PKArgs const& pks)
			{
				PR_ASSERT(PR_SQL_ASSERTS, m_db->IsOpen(), "Database closed");
				TableMetaData<DBRecord> const& meta = DBRecord::Sqlite_TableMetaData();
				Query query(*m_db, Sql("delete from ",meta.TableName()," where ",meta.PKConstraints()));
				pr::sqlite::Bind(query, pks);
				query.Step();
				return query.RowsChanged();
			}
			
			// Update item in the database
			int Update(DBRecord const& item)
			{
				PR_ASSERT(PR_SQL_ASSERTS, m_db->IsOpen(), "Database closed");
				TableMetaData<DBRecord> const& meta = DBRecord::Sqlite_TableMetaData();
				typedef ColumnMetaData<DBRecord> const* ColPtr;
				
				Query query(*m_db, Sql(
					"update ",meta.TableName()," set ",
					StrHelper::List(meta.NonPKs(),",",[](ColPtr c){return std::string(c->Name).append(" = ?");}),
					" where ",
					StrHelper::List(meta.PKs()," and ",[](ColPtr c){return std::string(c->Name).append(" = ?");})));
				
				int idx = 1; // binding parameters are indexed from 1
				for (auto i = begin(meta.NonPKs()), iend = end(meta.NonPKs()); i != iend; ++i)
					(*i)->Bind(query, idx++, item);
				for (auto i = begin(meta.PKs()), iend = end(meta.PKs()); i != iend; ++i)
					(*i)->Bind(query, idx++, item);
				
				query.Step();
				return query.RowsChanged();
			}
			
			// Update a single column in a table
			// Use pr::sqlite::PKArgs() to create 'PKs'
			template <typename ValueType, typename PKArgs> int Update(char const* column_name, ValueType const& value, PKArgs const& pks)
			{
				PR_ASSERT(PR_SQL_ASSERTS, m_db->IsOpen(), "Database closed");
				TableMetaData<DBRecord> const& meta = DBRecord::Sqlite_TableMetaData();
				TableMetaData<DBRecord>::ColMetaData const& column = *meta.Column(column_name);
				
				Query query(*m_db, Sql("update ",meta.TableName()," set ",column.Name," = ? where ",meta.PKConstraints()));
				column.Bind(query, 1, value);
				pr::sqlite::BindPKs<DBRecord>(query, pks, 1);
				query.Step();
				return query.RowsChanged();
			}
			
			// Return a record from the database.
			// Use pr::sqlite::PKs() to create 'PRArgs'
			template <typename PKArgs> DBRecord& Get(PKArgs const& pks, DBRecord& item) const
			{
				PR_ASSERT(PR_SQL_ASSERTS, m_db->IsOpen(), "Database closed");
				GetCmd<DBRecord> get(*m_db); // Create the sql query
				get.Bind(pks);               // Bind the primary keys
				return get.Get(item);        // Run the query
			}
			template <typename PKArgs> DBRecord Get(PKArgs const& pks) const
			{
				DBRecord item;
				return Get(pks, item);
			}
			
			// Search for a record from the database that mightn't be there
			template <typename PKArgs> bool Find(PKArgs const& pks, DBRecord& item) const
			{
				PR_ASSERT(PR_SQL_ASSERTS, m_db->IsOpen(), "Database closed");
				GetCmd<DBRecord> get(*m_db);
				get.Bind(pks);
				return get.Find(item);
			}
			
			// Return the value of a specific column
			template <typename Type, typename PKArgs> Type& GetColumn(PKArgs const& pks, int col, Type& value) const
			{
				PR_ASSERT(PR_SQL_ASSERTS, m_db->IsOpen(), "Database closed");
				TableMetaData<DBRecord> const& meta = DBRecord::Sqlite_TableMetaData();
				TableMetaData<DBRecord>::ColMetaData const& column = *meta.Columns()[col];
				
				Query query(*m_db, Sql("select ",column.Name," from ",meta.TableName()," where ",meta.PKConstraints()));
				pr::sqlite::BindPKs<DBRecord>(query, pks);
				query.Step();
				column.Read(query, 0, value);
				return value;
			}
			template <typename Type, typename PKArgs> Type  GetColumn(PKArgs const& pks, int col) const
			{
				Type type;
				return GetColumn<Type, PKArgs>(pks, col, type);
			}
		};
		
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		// Database wrapper
		class Database
		{
			enum { BusyTimeoutDefault = 60000 };
			mutable sqlite3*    m_db;  // The database connection
			mutable std::string m_sql; // A string for reducing memory allocations when constructing sql queries
			
			Database(Database const&); // no copying
			Database& operator= (Database const&);
			
		public:
			// Version number info
			static char const* SQLiteVersion()    { return SQLITE_VERSION; }
			static char const* SQLiteLibVersion() { return sqlite3_libversion(); }
			static int SQLiteLibVersionNumber()   { return sqlite3_libversion_number(); }
			
			Database()
			:m_db()
			{}
			Database(char const* db_file, int flags = SQLITE_OPEN_READWRITE|SQLITE_OPEN_CREATE, char const* vfs = 0)
			:m_db()
			{
				Open(db_file, flags, vfs);
			}
			virtual ~Database()
			{
				try { Close(); }
				catch (...) {}
			}
			
			// Make this type convertable to sqlite3* so it can be used in the standard C library functions
			operator sqlite3*() const
			{
				return m_db;
			}

			// Open a database file
			void Open(char const* db_file, int flags = SQLITE_OPEN_READWRITE|SQLITE_OPEN_CREATE, char const* vfs = 0)
			{
				int res = sqlite3_open_v2(db_file, &m_db, flags, vfs);
				if (res != SQLITE_OK) throw Exception(res, sqlite3_errmsg(m_db), false);
				BusyTimeout(BusyTimeoutDefault);
			}

			// Close a database file
			void Close()
			{
				if (!m_db) return;
				int res = sqlite3_close(m_db);
				if (res != SQLITE_OK) throw Exception(res, "Failed to close database connection", false);
				m_db = 0;
			}
			
			// Returns true if the database connection is open
			bool IsOpen() const
			{
				return m_db != 0;
			}
			
			// This function causes any pending database operation to abort and
			// return at its earliest opportunity. Typically used for Cancel functionality
			void Interrupt() { sqlite3_interrupt(m_db); }
			
			// Executes an sql query string that doesn't require binding, returning a 'Query' result
			Query ExecuteQuery(char const* sql_query)
			{
				PR_ASSERT(PR_SQL_ASSERTS, m_db, "Database not open");
				Query query(m_db, sql_query);
				query.Step();
				return query;
			}
			
			// Executes an sql query that returns a scalar (ie int) result
			int ExecuteScalar(char const* sql_query)
			{
				Query query = ExecuteQuery(sql_query);
				if (query.RowEnd()) throw Exception(SQLITE_ERROR, "Scalar query returned no results", false);
				int value; pr::sqlite::read_integer(query, 0, value);
				if (query.Step()) throw Exception(SQLITE_ERROR, "Scalar query returned more than one result", false);
				return value;
			}
			
			// Execute an sql string command
			int Execute(char const* sql_cmd)
			{
				PR_ASSERT(PR_SQL_ASSERTS, m_db, "Database not open");
				
				char* err_msg = 0;
				int res = sqlite3_exec(m_db, sql_cmd, 0, 0, &err_msg);
				if (res != SQLITE_OK) throw Exception(res, err_msg, true);
				return sqlite3_changes(m_db);
			}
			
			// Returns true if a table named 'name' exists
			bool TableExists(char const* name) const
			{
				char const* sql = Sql("select count(*) from sqlite_master where type='table' and name='",name,"'");
				return const_cast<Database*>(this)->ExecuteScalar(sql) != 0;
			}
			template <typename DBRecord> bool TableExists() const
			{
				TableMetaData<DBRecord> const& meta = DBRecord::Sqlite_TableMetaData();
				return TableExists(meta.TableName());
			}
			
			// Creates a table in the database based on type 'DBRecord'. Returns SQLITE_OK on success
			// http://www.sqlite.org/syntaxdiagrams.html#create-table-stmt
			// http://www.sqlite.org/syntaxdiagrams.html#table-constraint
			// Notes:
			//  autoincrement must follow primary key without anything in between
			template <typename DBRecord> int CreateTable(char const* options = "if not exists")
			{
				TableMetaData<DBRecord> const& meta = DBRecord::Sqlite_TableMetaData();
				return Execute(Sql("create table ",options," ",meta.TableName(),"(\n",meta.TableDecl(),")"));
			}
			
			// Drops a table from the database
			template <typename DBRecord> int DropTable(char const* options = "if exists")
			{
				TableMetaData<DBRecord> const& meta = DBRecord::Sqlite_TableMetaData();
				return Execute(Sql("drop table ",options," ",meta.TableName()));
			}
			
			// Return an object for accessing a specific db table
			template <typename DBRecord> DBTable<DBRecord> Table()
			{
				return DBTable<DBRecord>(this);
			}
			
			// Each entry in an SQLite table has a unique 64-bit signed integer key
			// called the rowid. The rowid is always available as an undeclared column
			// named ROWID, OID, or _ROWID_ as long as those names are not also used by
			// explicitly declared columns. If the table has a column of type
			// [INTEGER PRIMARY KEY] then that column is another alias for the rowid.
			sqlite_int64 LastRowId() const
			{
				return sqlite3_last_insert_rowid(m_db);
			}
			int LastRowId32() const
			{
				return static_cast<int>(LastRowId());
			}
			
			// This is the length of time sqlite spends waiting for synchronous access to the db
			// After at least "ms" milliseconds of sleeping, the handler returns 0 which causes
			// [sqlite3_step()] to return [SQLITE_BUSY] or [SQLITE_IOERR_BLOCKED].
			void BusyTimeout(int block_time_ms) { sqlite3_busy_timeout(m_db, block_time_ms); }
			
			// AutoComment returns true if the given database connection is in autocommit mode.
			// Autocommit mode is on by default.
			// Autocommit mode is disabled by a [BEGIN] statement.
			// Autocommit mode is re-enabled by a [COMMIT] or [ROLLBACK].
			bool AutoCommit() const
			{
				PR_ASSERT(PR_SQL_ASSERTS, m_db, "Database not open");
				return sqlite3_get_autocommit(m_db) ? true : false;
			}
		};
		
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		// An RAII wrapper for a database transaction
		class Transaction
		{
			Database& m_db;
			bool m_completed;
			
			Transaction(Transaction const&); // no copying
			Transaction& operator=(Transaction const&);
			
		public:
			explicit Transaction(Database& db)
			:m_db(db)
			,m_completed(false)
			{
				m_db.Execute("begin transaction");
			}
			~Transaction()
			{
				if (!m_completed)
					Rollback();
			}
			void Commit()
			{
				m_db.Execute("commit");
				m_completed = true;
			}
			void Rollback()
			{
				m_db.Execute("rollback");
				m_completed = true;
			}
		};

		//////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		// Cleanup macro definitions
		#undef PR_SQLITE_REP0
		#undef PR_SQLITE_REP1
		#undef PR_SQLITE_REP2
		#undef PR_SQLITE_REP3
		#undef PR_SQLITE_REP4
		#undef PR_SQLITE_REP5
		#undef PR_SQLITE_REP6
		#undef PR_SQLITE_REP7
		#undef PR_SQLITE_REP8
		#undef PR_SQLITE_REP9
		#undef PR_SQLITE_JOIN2
		#undef PR_SQLITE_JOIN
		#undef PR_SQLITE_REPEAT
		#undef PR_COMMA
		#undef PR_NEWLINE
		
		//////////////////////////////////////////////////////////////////////////////////////////////////////////////
	}
}

#ifdef PR_ASSERT_DEFINED
#   undef PR_ASSERT_DEFINED
#   undef PR_ASSERT
#endif

#endif

