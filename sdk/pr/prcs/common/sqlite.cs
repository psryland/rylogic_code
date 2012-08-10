#define SQLITE_TRACE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using pr.common;

using sqlite3      = System.IntPtr;
using sqlite3_stmt = System.IntPtr;

// Notes:
// Trace/Debugging:
//  Define SQLITE_TRACE to trace query object creation and destruction
//
// Decimals:
//  Deliberately not supporting decimals by default because the whole
//  point of decimal is accuracy. Sqlite only uses 8bytes to store a real
//  so the 128bit decimal numbers lose too much accuracy when stored.
//  Use doubles instead, or blobs.
//
// ICustomAccessors:
//  I considered an ICustomAccessors interface where custom types would
//  provide custom method for 'getting' and 'setting' themselves, however
//  this approach won't work for 3rd party types (e.g. Size, Point, etc)
//  Also, ICustomAccessors implementers would have to return a built in
//  ClrType to indicate the type returned by their get/set methods, since
//  the meta data is generated from type information without an instance,
//  an interface is not appropriate for getting this type.

namespace pr.common
{
	/// <summary>A simple sqlite .net ORM</summary>
	public static class Sqlite
	{
		#region Constants

		/// <summary>
		/// The data types supported by sqlite.<para/>
		/// SQLite uses these data types:<para/>
		///   integer - A signed integer, stored in 1, 2, 3, 4, 6, or 8 bytes depending on the magnitude of the value.<para/>
		///   real    - A floating point value, stored as an 8-byte IEEE floating point number.<para/>
		///   text    - A text string, stored using the database encoding (UTF-8, UTF-16BE or UTF-16LE).<para/>
		///   blob    - A blob of data, stored exactly as it was input.<para/>
		///   null    - the null value<para/>
		/// All other type keywords are mapped to these types<para/></summary>
		public enum DataType
		{
			Integer = 1,
			Real    = 2,
			Text    = 3,
			Blob    = 4,
			Null    = 5,
		}

		/// <summary>Result codes returned by sqlite dll calls</summary>
		public enum Result
		{
			OK         = 0,   // Successful result
			Error      = 1,   // SQL error or missing database
			Internal   = 2,   // Internal logic error in SQLite
			Perm       = 3,   // Access permission denied
			Abort      = 4,   // Callback routine requested an abort
			Busy       = 5,   // The database file is locked
			Locked     = 6,   // A table in the database is locked
			NoMem      = 7,   // A malloc() failed
			ReadOnly   = 8,   // Attempt to write a readonly database
			Interrupt  = 9,   // Operation terminated by sqlite3_interrupt()
			IOError    = 10,  // Some kind of disk I/O error occurred
			Corrupt    = 11,  // The database disk image is malformed
			NotFound   = 12,  // Unknown opcode in sqlite3_file_control()
			Full       = 13,  // Insertion failed because database is full 
			CantOpen   = 14,  // Unable to open the database file 
			Protocol   = 15,  // Database lock protocol error 
			Empty      = 16,  // Database is empty 
			Schema     = 17,  // The database schema changed 
			TooBig     = 18,  // String or BLOB exceeds size limit
			Constraint = 19,  // Abort due to constraint violation
			Mismatch   = 20,  // Data type mismatch 
			Misuse     = 21,  // Library used incorrectly 
			NoLfs      = 22,  // Uses OS features not supported on host 
			Auth       = 23,  // Authorization denied 
			Format     = 24,  // Auxiliary database format error 
			Range      = 25,  // 2nd parameter to sqlite3_bind out of range 
			NotADB     = 26,  // File opened that is not a database file 
			Row        = 100, // sqlite3_step() has another row ready
			Done       = 101  // sqlite3_step() has finished executing
		}

		/// <summary>Flags passed to the sqlite3_open_v2 function</summary>
		[Flags] public enum OpenFlags
		{
			ReadOnly                                       = 1,
			ReadWrite                                      = 2,
			Create                                         = 4,
			NoMutex                                        = 0x8000,
			FullMutex                                      = 0x10000,
			SharedCache                                    = 0x20000,
			PrivateCache                                   = 0x40000,
			ProtectionComplete                             = 0x00100000,
			ProtectionCompleteUnlessOpen                   = 0x00200000,
			ProtectionCompleteUntilFirstUserAuthentication = 0x00300000,
			ProtectionNone                                 = 0x00400000
		}

		/// <summary>Behaviour to perform on a constraint condition</summary>
		public enum OnInsertConstraint
		{
			/// <summary>On constraint, the insert operation will produce an error</summary>
			Reject,
			
			/// <summary>On constraint, the insert operation will be ignored</summary>
			Ignore,
			
			/// <summary>On constraint, the insert operation will replace the existing item</summary>
			Replace,
		}

		/// <summary>Sqlite limit categories</summary>
		public enum Limit
		{
			Length            =  0,
			SqlLength         =  1,
			Column            =  2,
			ExprDepth         =  3,
			CompoundSelect    =  4,
			VdbeOp            =  5,
			FunctionArg       =  6,
			Attached          =  7,
			LikePatternLength =  8,
			VariableNumber    =  9,
			TriggerDepth      = 10,
		}

		/// <summary>Sqlite configuration options</summary>
		public enum ConfigOption
		{
			SingleThread = 1,
			MultiThread  = 2,
			Serialized   = 3
		}

		#endregion

		#region Global methods

		/// <summary>
		/// Sets a global configuration option for sqlite. Must be called prior
		/// to initialisation or after shutdown of sqlite. Initialisation happens
		/// implicitly when Open is called.</summary>
		public static void Configure(ConfigOption option)
		{
			sqlite3_config(option);
		}

		/// <summary>Returns the sqlite data type to use for a given .NET type.</summary>
		public static DataType SqlType(Type type)
		{
			type = Nullable.GetUnderlyingType(type) ?? type;
			if (type == null)
				return DataType.Null;
			if (type == typeof(bool) ||
				type == typeof(byte) ||
				type == typeof(sbyte) ||
				type == typeof(Char) ||
				type == typeof(UInt16) ||
				type == typeof(Int16) ||
				type == typeof(Int32) ||
				type == typeof(UInt32) ||
				type == typeof(Int64) ||
				type == typeof(UInt64) ||
				type.IsEnum)
				return DataType.Integer;
			if (type == typeof(Single) ||
				type == typeof(Double))
				return DataType.Real;
			if (type == typeof(String) ||
				type == typeof(DateTime) ||
				type == typeof(Guid))
				return DataType.Text;
			if (type == typeof(byte[]))
				return DataType.Blob;
			
			throw new NotSupportedException(
				"No default type mapping for type "+type.Name+"\n" +
				"Custom data types should specify a value for the " +
				"'SqlDataType' property in their ColumnAttribute");
		}

		/// <summary>A helper for gluing strings together that uses a provided string builder</summary>
		public static string Sql(StringBuilder sb, params string[] parts)
		{
			sb.Length = 0;
			foreach (var p in parts) sb.Append(p);
			return sb.ToString();
		}
		public static string Sql(params string[] parts)
		{
			return Sql(new StringBuilder(), parts);
		}

		/// <summary>
		/// Returns the primary key values read from 'item'. 
		/// The returned array can be pass to methods that take 'params object[]' arguments</summary>
		public static object[] PrimaryKeys<T>(T item) where T:class,new()
		{
			var meta = TableMetaData.GetMetaData<T>();
			return meta.Pks.Select(x => x.Get(item)).ToArray();
		}

		/// <summary>Converts a C# string (in UTF-16) to a byte array in UTF-8</summary>
		private static byte[] StrToUTF8(string str)
		{
			return Encoding.Convert(Encoding.Unicode, Encoding.UTF8, Encoding.Unicode.GetBytes(str));
		}

		/// <summary>Converts an IntPtr that points to a null terminated utf-8 string in a .NET string</summary>
		private static string UTF8toStr(IntPtr utf8ptr)
		{
			if (utf8ptr == IntPtr.Zero) return null;
			var str = Marshal.PtrToStringAnsi(utf8ptr);
			if (str == null) return null;
			var bytes = new byte[str.Length];
			Marshal.Copy(utf8ptr, bytes, 0, bytes.Length);
			return Encoding.UTF8.GetString(bytes);
		}

		/// <summary>Compiles an sql string into an sqlite3 statement</summary>
		private static sqlite3_stmt Compile(sqlite3 db, string sql_string)
		{
			Debug.Assert(db != IntPtr.Zero, "Database handle invalid");
			
			sqlite3_stmt stmt;
			var bytes_utf8 = StrToUTF8(sql_string);
			var res = sqlite3_prepare_v2(db, bytes_utf8, bytes_utf8.Length, out stmt, IntPtr.Zero);
			if (res != Result.OK) throw Exception.New(res, ErrorMsg(db));
			return stmt;
		}

		/// <summary>Returns the error message for the last error returned from sqlite</summary>
		private static string ErrorMsg(sqlite3 db)
		{
			// sqlite3 manages the memory allocated for the returned error message
			// so we don't need to call sqlite_free on the returned pointer
			return Marshal.PtrToStringUni(sqlite3_errmsg16(db));
		}

		#endregion

		#region Binding - Assigning parameters in a prepared statement

		/// <summary>Methods for binding data to an sqlite prepared statement</summary>
		public static class Bind
		{
			// Could use Convert.ToXYZ() here, but straight casts are faster
			public static void Bool(sqlite3_stmt stmt, int idx, object value)
			{
				sqlite3_bind_int(stmt, idx, (bool)value ? 1 : 0);
			}
			public static void SByte(sqlite3_stmt stmt, int idx, object value)
			{
				sqlite3_bind_int(stmt, idx, (sbyte)value);
			}
			public static void Byte(sqlite3_stmt stmt, int idx, object value)
			{
				sqlite3_bind_int(stmt, idx, (byte)value);
			}
			public static void Char(sqlite3_stmt stmt, int idx, object value)
			{
				sqlite3_bind_int(stmt, idx, (Char)value);
			}
			public static void Short(sqlite3_stmt stmt, int idx, object value)
			{
				sqlite3_bind_int(stmt, idx, (short)value);
			}
			public static void UShort(sqlite3_stmt stmt, int idx, object value)
			{
				sqlite3_bind_int(stmt, idx, (ushort)value);
			}
			public static void Int(sqlite3_stmt stmt, int idx, object value)
			{
				sqlite3_bind_int(stmt, idx, (int)value);
			}
			public static void UInt(sqlite3_stmt stmt, int idx, object value)
			{
				sqlite3_bind_int64(stmt, idx, (uint)value);
			}
			public static void Long(sqlite3_stmt stmt, int idx, object value)
			{
				sqlite3_bind_int64(stmt, idx, (long)value);
			}
			public static void ULong(sqlite3_stmt stmt, int idx, object value)
			{
				sqlite3_bind_int64(stmt, idx, Convert.ToInt64(value)); // Use Convert to trap overflow exceptions
			}
			public static void Float(sqlite3_stmt stmt, int idx, object value)
			{
				sqlite3_bind_double(stmt, idx, (float)value);
			}
			public static void Double(sqlite3_stmt stmt, int idx, object value)
			{
				sqlite3_bind_double(stmt, idx, (double)value);
			}
			public static void Text(sqlite3_stmt stmt, int idx, object value)
			{
				if (value == null) sqlite3_bind_null(stmt, idx);
				else sqlite3_bind_text16(stmt, idx, (string)value, -1, TransientData);
			}
			public static void Blob(sqlite3_stmt stmt, int idx, object value)
			{
				if (value == null) sqlite3_bind_null(stmt, idx);
				else sqlite3_bind_blob(stmt, idx, (byte[])value, ((byte[])value).Length, TransientData);
			}
			public static void Guid(sqlite3_stmt stmt, int idx, object value)
			{
				if (value == null) sqlite3_bind_null(stmt, idx);
				else Text(stmt, idx, ((Guid)value).ToString());
			}
		}

		/// <summary>
		/// A lookup table from ClrType to binding function.
		/// Users can add custom types and binding functions to this map if needed</summary>
		public static readonly Dictionary<Type, BindFunc> BindFunction = new BindMap();
		public delegate void BindFunc(sqlite3_stmt stmt, int index, object obj);
		private class BindMap :Dictionary<Type, BindFunc>
		{
			public BindMap()
			{
				Add(typeof(bool)   ,Bind.Bool   );
				Add(typeof(sbyte)  ,Bind.SByte  );
				Add(typeof(byte)   ,Bind.Byte   );
				Add(typeof(char)   ,Bind.Char   );
				Add(typeof(short)  ,Bind.Short  );
				Add(typeof(ushort) ,Bind.UShort );
				Add(typeof(int)    ,Bind.Int    );
				Add(typeof(uint)   ,Bind.UInt   );
				Add(typeof(long)   ,Bind.Long   );
				Add(typeof(ulong)  ,Bind.ULong  );
				Add(typeof(float)  ,Bind.Float  );
				Add(typeof(double) ,Bind.Double );
				Add(typeof(string) ,Bind.Text   );
				Add(typeof(byte[]) ,Bind.Blob   );
				Add(typeof(Guid)   ,Bind.Guid   );
			}
		}

		#endregion

		#region Reading - Reading columns from a query result into a Clr Object

		/// <summary>Methods for reading data from an sqlite prepared statement</summary>
		public static class Read
		{
			public static object Bool(sqlite3_stmt stmt, int idx)
			{
				return sqlite3_column_int(stmt, idx) != 0; // Sqlite returns 0 if this column is null
			}
			public static object SByte(sqlite3_stmt stmt, int idx)
			{
				return (sbyte)sqlite3_column_int(stmt, idx); // Sqlite returns 0 if this column is null
			}
			public static object Byte(sqlite3_stmt stmt, int idx)
			{
				return (byte)sqlite3_column_int(stmt, idx); // Sqlite returns 0 if this column is null
			}
			public static object Char(sqlite3_stmt stmt, int idx)
			{
				return (char)sqlite3_column_int(stmt, idx); // Sqlite returns 0 if this column is null
			}
			public static object Short(sqlite3_stmt stmt, int idx)
			{
				return (short)sqlite3_column_int(stmt, idx); // Sqlite returns 0 if this column is null
			}
			public static object UShort(sqlite3_stmt stmt, int idx)
			{
				return (ushort)sqlite3_column_int(stmt, idx); // Sqlite returns 0 if this column is null
			}
			public static object Int(sqlite3_stmt stmt, int idx)
			{
				return sqlite3_column_int(stmt, idx); // Sqlite returns 0 if this column is null
			}
			public static object UInt(sqlite3_stmt stmt, int idx)
			{
				return (uint)sqlite3_column_int64(stmt, idx); // Sqlite returns 0 if this column is null
			}
			public static object Long(sqlite3_stmt stmt, int idx)
			{
				return sqlite3_column_int64(stmt, idx); // Sqlite returns 0 if this column is null
			}
			public static object ULong(sqlite3_stmt stmt, int idx)
			{
				return (ulong)sqlite3_column_int64(stmt, idx); // Sqlite returns 0 if this column is null
			}
			public static object Float(sqlite3_stmt stmt, int idx)
			{
				return (float)sqlite3_column_double(stmt, idx); // Sqlite returns 0.0 if this column is null
			}
			public static object Double(sqlite3_stmt stmt, int idx)
			{
				return sqlite3_column_double(stmt, idx); // Sqlite returns 0.0 if this column is null
			}
			public static object Text(sqlite3_stmt stmt, int idx)
			{
				var ptr = sqlite3_column_text16(stmt, idx); // Sqlite returns null if this column is null
				if (ptr != IntPtr.Zero) return Marshal.PtrToStringUni(ptr);
				return string.Empty;
			}
			public static object Blob(sqlite3_stmt stmt, int idx)
			{
				// Read the blob size limit
				var db = sqlite3_db_handle(stmt);
				var max_size = sqlite3_limit(db, Limit.Length, -1);
				
				// sqlite returns null if this column is null
				var ptr = sqlite3_column_blob(stmt, idx); // have to call this first
				var len = sqlite3_column_bytes(stmt, idx);
				if (len < 0 || len > max_size) throw Exception.New(Result.Corrupt, "Blob data size exceeds database maximum size limit");
				
				// Copy the blob out of the db
				byte[] blob = new byte[len];
				if (len != 0) Marshal.Copy(ptr, blob, 0, len);
				return blob;
			}
			public static object Guid(sqlite3_stmt stmt, int idx)
			{
				string text = (string)Text(stmt, idx);
				return System.Guid.Parse(text);
			}
		}

		/// <summary>
		/// A lookup table from ClrType to reading function.
		/// Users can add custom types and reading functions to this map if needed</summary>
		public static readonly Dictionary<Type, ReadFunc> ReadFunction = new ReadMap();
		public delegate object ReadFunc(sqlite3_stmt stmt, int index);
		private class ReadMap :Dictionary<Type, ReadFunc>
		{
			public ReadMap()
			{
				Add(typeof(bool)   ,Read.Bool   );
				Add(typeof(sbyte)  ,Read.SByte  );
				Add(typeof(byte)   ,Read.Byte   );
				Add(typeof(char)   ,Read.Char   );
				Add(typeof(short)  ,Read.Short  );
				Add(typeof(ushort) ,Read.UShort );
				Add(typeof(int)    ,Read.Int    );
				Add(typeof(uint)   ,Read.UInt   );
				Add(typeof(long)   ,Read.Long   );
				Add(typeof(ulong)  ,Read.ULong  );
				Add(typeof(float)  ,Read.Float  );
				Add(typeof(double) ,Read.Double );
				Add(typeof(string) ,Read.Text   );
				Add(typeof(byte[]) ,Read.Blob   );
				Add(typeof(Guid)   ,Read.Guid   );
			}
		}

		#endregion

		/// <summary>Creates a connection to an sqlite database file</summary>
		public class Database :IDisposable
		{
			private sqlite3 m_db;

			/// <summary>The filepath of the database file opened</summary>
			public string Filepath { get; set; }

			/// <summary>Opens a connection to the database</summary>
			public Database(string filepath, OpenFlags flags)
			{
				Filepath = filepath;
				
				// Open the database file
				var res = sqlite3_open_v2(filepath, out m_db, (int)flags, IntPtr.Zero);
				if (res != Result.OK) throw Exception.New(res, "Failed to open database connection to file "+filepath);
			}

			/// <summary>
			/// Sets the busy timeout period in milliseconds. This controls how long the thread
			/// sleeps for when waiting to gain a lock on a table. After this timeout period Step()
			/// will return Result.Busy. Set to 0 to turn off the busy wait handler.</summary>
			public int BusyTimeout
			{
				set
				{
					var res = sqlite3_busy_timeout(Db, value);
					if (res != Result.OK) throw Exception.New(res, "Failed to set the busy timeout to "+value+"ms");
				}
			}

			/// <summary>Returns the m_db field after asserting it's validity</summary>
			private sqlite3 Db
			{
				get { Debug.Assert(m_db != IntPtr.Zero, "Invalid database handle"); return m_db; }
			}

			/// <summary>Close a database file</summary>
			public void Close()
			{
				if (m_db == IntPtr.Zero) return;
				GC.Collect(); // Just in case there are disposable objects awaiting collection
				var res = sqlite3_close(m_db);
				if (res == Result.Busy)
				{
					Trace.Dump();
					throw Exception.New(res, "Could not close database handle, there are still prepared statements that haven't been 'finalized' or blob handles that haven't been closed.");
				}
				if (res != Result.OK)
				{
					throw Exception.New(res, "Failed to close database connection");
				}
				m_db = IntPtr.Zero;
			}

			/// <summary>
			/// Executes an sql query string that doesn't require binding, returning a 'Query' result.
			/// Remember to wrap the returned query in a 'using' statement or you might have trouble
			/// closing the database connection due to outstanding un-finalized prepared statements.<para/>
			/// e.g.<para/>
			///   using (var query = ExecuteQuery(sql))<para/>
			///      query.ReadInt(0);<para/>
			/// </summary>
			public Query ExecuteQuery(string sql)
			{
				var query = new Query(Db, sql);
				query.Step();
				return query;
			}

			/// <summary>Executes an sql query that returns a scalar (i.e. int) result</summary>
			public int ExecuteScalar(string sql)
			{
				using (Query query = ExecuteQuery(sql))
				{
					if (query.RowEnd) throw Exception.New(Result.Error, "Scalar query returned no results");
					int value = (int)Read.Int(query.Stmt, 0);
					if (query.Step()) throw Exception.New(Result.Error, "Scalar query returned more than one result");
					return value;
				}
			}

			/// <summary>Executes an sql statement returning the number of affected rows</summary>
			public int Execute(string sql)
			{
				using (ExecuteQuery(sql))
					return sqlite3_changes(m_db);
			}

			/// <summary>Drop any existing table created for type 'T'</summary>
			public void DropTable<T>(bool if_exists = true) where T:class,new()
			{
				var meta = TableMetaData.GetMetaData<T>();
				var opts = if_exists ? "if exists " : "";
				var sql  = Sql("drop table ",opts,meta.Name);
				Execute(sql);
			}

			/// <summary>
			/// Creates a table in the database based on type 'T'. Throws if not successful.<para/>
			/// http://www.sqlite.org/syntaxdiagrams.html#create-table-stmt <para/>
			/// http://www.sqlite.org/syntaxdiagrams.html#table-constraint <para/>
			/// Notes: <para/>
			///  auto increment must follow primary key without anything in between<para/></summary>
			public void CreateTable<T>(bool if_not_exists = true) where T:class,new()
			{
				var meta = TableMetaData.GetMetaData<T>();
				var opts = if_not_exists ? "if not exists " : "";
				var sql  = Sql("create table ",opts,meta.Name,"(\n",meta.Decl(),")");
				Execute(sql);
			}

			/// <summary>Returns true if a table for type 'T' exists</summary>
			public bool TableExists<T>() where T:class,new()
			{
				var name = typeof(T).Name;
				var sql = Sql("select count(*) from sqlite_master where type='table' and name='",name,"'");
				return ExecuteScalar(sql) != 0;
			}

			// ReSharper disable MemberHidesStaticFromOuterClass
			/// <summary>Return an existing table for type 'T'</summary>
			public Table<T> Table<T>() where T:class,new()
			{
				return new Table<T>(Db);
			}
			// ReSharper restore MemberHidesStaticFromOuterClass

			/// <summary>IDisposable interface</summary>
			public void Dispose()
			{
				Close();
			}
		}

		/// <summary>Represents a table within the database</summary>
		public class Table<T> :IEnumerable<T> where T:class,new()
		{
			private readonly TableMetaData m_meta = TableMetaData.GetMetaData<T>();
			private readonly sqlite3 m_db; // The handle for the database connection
				
			public Table(sqlite3 db)
			{
				Debug.Assert(db != null, "Invalid database handle");
				m_db = db;
			}

			/// <summary>The name of this table</summary>
			public string Name
			{
				get { return m_meta.Name; }
			}

			/// <summary>Gets the number of columns in this table</summary>
			public int ColumnCount
			{
				get { return m_meta.ColumnCount; }
			}

			// ReSharper disable MemberHidesStaticFromOuterClass
			/// <summary>General sql query on the table</summary>
			public Query Query(string sql_string)
			{
				return new Query(m_db, sql_string);
			}
			// ReSharper restore MemberHidesStaticFromOuterClass

			/// <summary>
			/// Insert an item into the table.<para/>
			/// Note: insert will *NOT* change the primary keys/autoincrement members of 'item' make sure you call 'Get()' to get the updated item.</summary>
			public int Insert(T item, OnInsertConstraint on_constraint = OnInsertConstraint.Reject)
			{
				using (var insert = new InsertCmd<T>(m_db, on_constraint)) // Create the sql query
				{
					insert.BindObj(item); // Bind 'item' to it
					return insert.Run();  // Run the query
				}
			}

			/// <summary>
			/// Inserts 'item' into the database and then sets 'at_row' to the row at which
			/// 'item' was inserted. This is typically used to update the primary key in 'item'
			/// which, for integer auto increment columns, is normally the last row id.</summary>
			public int Insert(T item, out int last_row_id, OnInsertConstraint on_constraint = OnInsertConstraint.Reject)
			{
				int res = Insert(item, on_constraint);
				last_row_id = (int)sqlite3_last_insert_rowid(m_db);
				return res;
			}

			/// <summary>Update 'item' in the table</summary>
			public int Update(T item)
			{
				var sql = Sql(
					"update ",m_meta.Name," set ",
					string.Join(",", m_meta.NonPks.Select(x => x.Name+" = ?")),
					" where ",
					string.Join(" and ", m_meta.Pks.Select(x => x.Name+" = ?")));
				
				using (var query = new Query(m_db, sql))
				{
					int idx = 1; // binding parameters are indexed from 1
					foreach (var c in m_meta.NonPks)
						c.BindFn(query.Stmt, idx++, c.Get(item));
					foreach (var c in m_meta.Pks)
						c.BindFn(query.Stmt, idx++, c.Get(item));
				
					query.Step();
					return query.RowsChanged;
				}
			}

			/// <summary>Delete 'item' from the table</summary>
			public int Delete(T item)
			{
				return Delete(PrimaryKeys(item));
			}

			/// <summary>Returns a row in the table or null if not found</summary>
			public T Find(params object[] keys)
			{
				using (var get = new GetCmd<T>(m_db))
				{
					get.BindPks<T>(1, keys);
					return get.Find();
				}
			}

			/// <summary>Returns a row in the table (throws if not found)</summary>
			public T Get(params object[] keys)
			{
				var item = Find(keys);
				if (item == null) throw Exception.New(Result.NotFound, "Row not found for key(s): "+string.Join(",", keys.Select(x=>x.ToString())));
				return item;
			}

			/// <summary>Delete a row from the table. Returns the number of rows affected</summary>
			public int Delete(params object[] keys)
			{
				var sql = Sql("delete from ",m_meta.Name," where ",m_meta.PkConstraints());
				using (var query = new Query(m_db, sql))
				{
					query.BindParms(1, keys);
					query.Step();
					return query.RowsChanged;
				}
			}

			/// <summary>Update a single column for a single row in the table</summary>
			public int Update<TValueType>(string column_name, TValueType value, params object[] keys)
			{
				var column_meta = m_meta.Column(column_name);
				var sql = Sql("update ",m_meta.Name," set ",column_meta.Name," = ? where ",m_meta.PkConstraints());
				using (var query = new Query(m_db, sql))
				{
					column_meta.BindFn(query.Stmt, 1, value);
					query.BindPks<T>(2, keys);
					query.Step();
					return query.RowsChanged;
				}
			}

			/// <summary>Return the value of a specific column in a single row in the table</summary>
			public TValueType ColumnValue<TValueType>(string column_name, params object[] keys)
			{
				var column_meta = m_meta.Column(column_name);
				var sql = Sql("select ",column_meta.Name," from ",m_meta.Name," where ",m_meta.PkConstraints());
				using (var query = new Query(m_db, sql))
				{
					query.BindPks<T>(1, keys);
					query.Step();
					return (TValueType)column_meta.ReadFn(query.Stmt, 0);
				}
			}

			/// <summary>IEnumerable interface</summary>
			public IEnumerator<T> GetEnumerator()
			{
				using (var query = new SelectAllCmd<T>(m_db))
					foreach (var t in query)
						yield return t;
			}
			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		/// <summary>Represents an sqlite prepared statement and iterative result (wraps an sqlite3_stmt handle).</summary>
		public class Query :IDisposable
		{
			protected sqlite3_stmt m_stmt; // sqlite managed memory for this query
			protected bool m_row_end;      // True once the last row has been read
			
			public Query(sqlite3_stmt stmt)
			{
				Trace.QueryCtor(this);
				m_stmt = stmt;
				m_row_end = false;
			}
			public Query(sqlite3 db, string sql_string) :this(Compile(db, sql_string))
			{
			}
			~Query()
			{
				Trace.QueryDtor(this);
			}
			
			/// <summary>Returns the m_stmt field after asserting it's validity</summary>
			public sqlite3_stmt Stmt
			{
				get { Debug.Assert(m_stmt != IntPtr.Zero, "Invalid query object"); return m_stmt; }
			}
			
			/// <summary>IDisposable</summary>
			public void Dispose()
			{
				Close();
			}

			/// <summary>Call when done with this query</summary>
			public void Close()
			{
				if (m_stmt == IntPtr.Zero) return;
				
				// After 'sqlite3_finalize()', it is illegal to use m_stmt. So save 'db' here first
				sqlite3 db = sqlite3_db_handle(m_stmt);
				Result res = sqlite3_finalize(m_stmt); m_stmt = IntPtr.Zero; // Ensure no use of 'm_stmt'
				if (res != Result.OK) throw Exception.New(res, ErrorMsg(db));
			}

			/// <summary>Return the number of parameters in this statement</summary>
			public int ParmCount
			{
				get { return sqlite3_bind_parameter_count(Stmt); }
			}

			/// <summary>Return the index for the parameter named 'name'</summary>
			public int ParmIndex(string name)
			{
				int idx = sqlite3_bind_parameter_index(Stmt, name);
				if (idx == 0) throw Exception.New(Result.Error, "Parameter name not found");
				return idx;
			}

			/// <summary>Return the name of a parameter by index</summary>
			public string ParmName(int idx)
			{
				return UTF8toStr(sqlite3_bind_parameter_name(Stmt, idx));
			}

			/// <summary>Bind a value to a specific parameter</summary>
			public void BindParm<T>(int idx, T value)
			{
				var bind = BindFunction[typeof(T)];
				bind(m_stmt, idx, value);
			}

			/// <summary>
			/// Bind an array of parameters starting at parameter index 'first_idx' (remember parameter indices start at 1)
			/// To reuse the Query, call Reset(), then bind new keys, then call Step() again</summary>
			public void BindParms(int first_idx, params object[] parms)
			{
				foreach (var p in parms)
				{
					var bind = BindFunction[p.GetType()];
					bind(m_stmt, first_idx++, p);
				}
			}

			/// <summary>
			/// Bind primary keys to this query starting at parameter index 
			/// 'first_idx' (remember parameter indices start at 1)
			/// This is functionally the same as BindParms but it validates
			/// the types of the primary keys against the table meta data.</summary>
			public void BindPks<T>(int first_idx, params object[] keys)
			{
				var meta = TableMetaData.GetMetaData<T>();
				meta.BindPks(m_stmt, first_idx, keys);
			}

			/// <summary>Reset the prepared statement object back to its initial state, ready to be re-executed.</summary>
			public void Reset()
			{
				var res = sqlite3_reset(Stmt);
				if (res != Result.OK) throw Exception.New(res, ErrorMsg(sqlite3_db_handle(m_stmt)));
			}

			/// <summary>Iterate to the next row in the result. Returns true if there are more rows available</summary>
			public bool Step()
			{
				var res = sqlite3_step(Stmt);
				switch (res)
				{
				default: throw Exception.New(res, ErrorMsg(sqlite3_db_handle(m_stmt)));
				case Result.Done: m_row_end = true; return false;
				case Result.Row: return true;
				}
			}

			/// <summary>Returns true when the last row has been read</summary>
			public bool RowEnd
			{
				get { return m_row_end; }
			}

			/// <summary>Returns the number of rows changed as a result of the last 'step()'</summary>
			public int RowsChanged
			{
				get { return sqlite3_changes(sqlite3_db_handle(Stmt)); }
			}

			/// <summary>Returns the number of columns in the result of this query</summary>
			public int ColumnCount
			{
				get { return sqlite3_column_count(Stmt); }
			}

			/// <summary>Return the sql data type for a specific column</summary>
			public DataType ColumnType(int idx)
			{
				return sqlite3_column_type(m_stmt, idx);
			}

			/// <summary>Returns the name of the column at position 'idx'</summary>
			public string ColumnName(int idx)
			{
				return Marshal.PtrToStringUni(sqlite3_column_name16(Stmt, idx));
			}

			/// <summary>Read the value of a particular column</summary>
			public T ReadColumn<T>(int idx)
			{
				var read = ReadFunction[typeof(T)];
				return (T)read(m_stmt, idx);
			}
		}

		/// <summary>A specialised query used for inserting objects into a table</summary>
		public class InsertCmd<T> :Query where T:class,new()
		{
			protected readonly TableMetaData m_meta = TableMetaData.GetMetaData<T>();

			/// <summary>Returns the sql string for the insert command for type 'T'</summary>
			public static string SqlString(OnInsertConstraint on_constraint = OnInsertConstraint.Reject)
			{
				string cons;
				switch (on_constraint) {
				default: throw Exception.New(Result.Misuse, "Unknown OnConstraint behaviour");
				case OnInsertConstraint.Reject:  cons = ""; break;
				case OnInsertConstraint.Ignore:  cons = "or ignore"; break;
				case OnInsertConstraint.Replace: cons = "or replace"; break;
				}
				var meta = TableMetaData.GetMetaData<T>();
				return Sql(
					"insert ",cons," into ",meta.Name," (",
					string.Join(",", from c in meta.NonAutoIncs select c.Name),
					") values (",
					string.Join(",", from c in meta.NonAutoIncs select "?"),
					")");
			}

			/// <summary>
			/// Creates a compiled query for inserting an object of type 'T' into a table.
			/// Users can then bind values, and run the query repeatedly to insert multiple items</summary>
			public InsertCmd(sqlite3 db, OnInsertConstraint on_constraint = OnInsertConstraint.Reject) :base(db, SqlString(on_constraint))
			{}

			/// <summary>Bind the values in 'item' to this insert query making it ready for running</summary>
			public void BindObj(T item)
			{
				m_meta.BindObj(m_stmt, 1, item);
			}

			/// <summary>Run the insert command. Call 'Reset()' before running the command again</summary>
			public int Run()
			{
				Step();
				Debug.Assert(RowEnd, "Insert returned more than one row");
				return RowsChanged;
			}
		}

		/// <summary>A specialised query used for getting objects from the db</summary>
		public class GetCmd<T> :Query where T:class,new()
		{
			protected readonly TableMetaData m_meta = TableMetaData.GetMetaData<T>();

			/// <summary>Returns the sql string for the get command for type 'T'</summary>
			public static string SqlString()
			{
				var meta = TableMetaData.GetMetaData<T>();
				return Sql("select * from ",meta.Name," where ",meta.PkConstraints());
			}

			/// <summary>
			/// Create a compiled query for getting an object of type 'T' from a table.
			/// Users can then bind primary keys, and run the query repeatedly to get multiple items.</summary>
			public GetCmd(sqlite3 db) :base(db, SqlString())
			{}

			/// <summary>
			/// Populates 'item' and returns true if the query finds a row, otherwise returns false.
			/// Remember to call 'Reset()' before running the command again</summary>
			public T Find()
			{
				Step();
				if (RowEnd) return null;
				var item = new T();
				m_meta.Read(m_stmt, item);
				return item;
			}
		}

		/// <summary>A specialised query used for getting objects from the db</summary>
		public class SelectAllCmd<T> :Query ,IEnumerable<T> where T:class,new()
		{
			protected readonly TableMetaData m_meta = TableMetaData.GetMetaData<T>();

			/// <summary>Returns the sql string for the get command for type 'T'</summary>
			public static string SqlString()
			{
				var meta = TableMetaData.GetMetaData<T>();
				return Sql("select * from ",meta.Name);
			}

			/// <summary>
			/// Create a compiled query for getting an object of type 'T' from a table.
			/// Users can then bind primary keys, and run the query repeatedly to get multiple items.</summary>
			public SelectAllCmd(sqlite3 db) :base(db, SqlString())
			{}

			/// <summary>Enumerates all rows in a table</summary>
			public IEnumerator<T> GetEnumerator()
			{
				while (Step())
				{
					var item = new T();
					m_meta.Read(m_stmt, item);
					yield return item;
				}
			}
			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		/// <summary>Mapping information from a type to columns in the table</summary>
		public class TableMetaData
		{
			private static readonly Dictionary<Type, TableMetaData> Meta = new Dictionary<Type, TableMetaData>();
			public static TableMetaData GetMetaData<T>() { return GetMetaData(typeof(T)); }
			public static TableMetaData GetMetaData(Type type)
			{
				TableMetaData meta;
				if (!Meta.TryGetValue(type, out meta))
					Meta.Add(type, meta = new TableMetaData(type));
				return meta;
			}
			
			/// <summary>The columns of the table</summary>
			private readonly ColumnMetaData[] m_column;
			
			/// <summary>
			/// Pointer to the single primary key column for this table or null
			/// if the table has multiple primary keys</summary>
			private readonly ColumnMetaData m_single_pk;
			
			/// <summary>The .NET type that this meta data is for</summary>
			public Type Type { get; private set; }
			
			/// <summary>The table name (defaults to the type name)</summary>
			public string Name { get; set; }
			
			/// <summary>Table constraints for this table (default is none)</summary>
			public string Constraints { get; set; }
			
			/// <summary>The primary key columns, in order</summary>
			public ColumnMetaData[] Pks { get; private set; }
			
			/// <summary>The non primary key columns</summary>
			public ColumnMetaData[] NonPks { get; private set; }
			
			/// <summary>The columns that aren't auto increment columns</summary>
			public ColumnMetaData[] NonAutoIncs { get; private set; }
			
			/// <summary>Gets the number of columns in this table</summary>
			public int ColumnCount { get { return m_column.Length; } }
			
			/// <summary>Constructs the meta data for mapping a type to a database table</summary>
			public TableMetaData(Type type)
			{
				// Get the class level table attribute
				var attr = (TableAttribute)type.GetCustomAttributes(typeof(TableAttribute), true).FirstOrDefault() ?? new TableAttribute();
				
				Type = type;
				Name = type.Name;
				Constraints = attr.Constraints;
				
				// Tests if a member should be included as a column in the table
				Func<MemberInfo, bool> inc_member = mi =>
					!mi.GetCustomAttributes(typeof(IgnoreAttribute), true).Any() &&                      // doesn't have the ignore attribute
					(attr.AllByDefault || mi.GetCustomAttributes(typeof(ColumnAttribute), true).Any());  // all in by default, or marked with the column attribute
				
				// Get all properties and fields that are to be included in the database table
				List<ColumnMetaData> cols = new List<ColumnMetaData>();
				var props  = type.GetProperties(attr.PropertyBindingFlags).Where(pi => inc_member(pi));
				var fields = type.GetFields    (attr.FieldBindingFlags   ).Where(fi => inc_member(fi));
				foreach (var p in props ) cols.Add(new ColumnMetaData(p));
				foreach (var f in fields) cols.Add(new ColumnMetaData(f));
				
				// Sort the columns by the given order
				var cmp = Comparer<int>.Default;
				cols.Sort((lhs,rhs) => cmp.Compare(lhs.Order, rhs.Order));
				m_column = cols.ToArray();
				
				// Create optimised lists
				Pks         = m_column.Where(x => x.IsPk).ToArray();
				NonPks      = m_column.Where(x => !x.IsPk).ToArray();
				NonAutoIncs = m_column.Where(x => !x.IsAutoInc).ToArray();
				m_single_pk = Pks.Count() == 1 ? Pks.First() : null;
			}

			/// <summary>Return the column meta data for a column by name</summary>
			public ColumnMetaData Column(string column_name)
			{
				return m_column.First(x => string.CompareOrdinal(x.Name, column_name) == 0);
			}

			/// <summary>
			/// Bind the primary keys 'keys' to the parameters in a
			/// prepared statement starting at parameter 'first_idx'.</summary>
			public void BindPks(sqlite3_stmt stmt, int first_idx, params object[] keys)
			{
				Debug.Assert(first_idx >= 1, "parameter binding indices start at 1 so 'first_idx' must be >= 1");
				
				int idx = 0;
				foreach (var c in Pks)
				{
					if (idx == keys.Length) throw new ArgumentException("Not enough primary key values passed for type "+Name);
					if (keys[idx].GetType() != c.ClrType) throw new ArgumentException("Primary key "+(idx+1)+" should be of type "+c.ClrType.Name+" not type "+keys[idx].GetType().Name);
					c.BindFn(stmt, first_idx+idx, keys[idx]); // binding parameters are indexed from 1 (hence the +1)
					++idx;
				}
			}

			/// <summary>
			/// Bind the values of the properties/fields in 'item' to the parameters
			/// in prepared statement 'stmt'. 'ofs' is the index offset of the first
			/// parameter to start binding from.</summary>
			public void BindObj<T>(sqlite3_stmt stmt, int first_idx, T item) where T:class,new()
			{
				Debug.Assert(typeof(T) == Type, "'item' is not the correct type for this table");
				Debug.Assert(first_idx >= 1, "parameter binding indices start at 1 so 'first_idx' must be >= 1");
				
				int idx = 0; // binding parameters are indexed from 1
				foreach (var c in NonAutoIncs)
				{
					c.BindFn(stmt, first_idx+idx, c.Get(item));
					++idx;
				}
			}

			/// <summary>Populate the properties and fields of 'item' from the column values read from 'stmt'</summary>
			public void Read<T>(sqlite3_stmt stmt, T item) where T:class,new()
			{
				Debug.Assert(typeof(T) == Type, "'item' is not the correct type for this table");
				
				int idx = 0; // columns are indexed from 0
				foreach (var c in m_column)
					c.Set(item, c.ReadFn(stmt, idx++));
			}

			/// <summary>Returns a table declaration string for this table</summary>
			public string Decl()
			{
				var sb = new StringBuilder();
				sb.Append(string.Join(",\n", from c in m_column select c.ColumnDef()));
				if (!string.IsNullOrEmpty(Constraints))
				{
					if (sb.Length != 0) sb.Append(",\n");
					sb.Append(Constraints);
				}
				return sb.ToString();
			}

			/// <summary>
			/// Returns the constraint string for the primary keys of this table.<para/>
			/// i.e. select * from Table where {Key1 = ? and Key2 = ?}-this bit</summary>
			public string PkConstraints()
			{
				return m_single_pk != null
					? m_single_pk.Name + " = ?"
					: string.Join(" and ", Pks.Select(x => x.Name + " = ?"));
			}
		}

		/// <summary>A column within a db table</summary>
		public class ColumnMetaData
		{
			/// <summary>The name of the column</summary>
			public string Name;
			
			/// <summary>The data type of the column</summary>
			public DataType SqlDataType;
			
			/// <summary>Column constraints for this column</summary>
			public string Constraints;
			
			/// <summary>True if this column is a primary key</summary>
			public bool IsPk;
			
			/// <summary>True if this column is an auto increment column</summary>
			public bool IsAutoInc;
			
			/// <summary>True if this column cannot be null</summary>
			public bool IsNotNull;
			
			/// <summary>True for collate columns</summary>
			public bool IsCollate;
			
			/// <summary>An ordering field used to define the column order in a table</summary>
			public int Order;
			
			/// <summary>The .NET type of the property or field on the object</summary>
			public Type ClrType;
			
			/// <summary>Returns the value of this column from an object of type 'ClrType'</summary>
			public Func<object,object> Get { get; set; }
			
			/// <summary>Sets the value of this column in an object of type 'ClrType'</summary>
			public Action<object,object> Set { get; set; }
			
			/// <summary>Binds the value from this column to parameter 'index' in 'stmt'</summary>
			public BindFunc BindFn;
			
			/// <summary>Reads column 'index' from 'stmt' and sets the corresponding property or field in 'obj'</summary>
			public ReadFunc ReadFn;
			
			public ColumnMetaData(PropertyInfo pi)
			{
				Get = obj => pi.GetValue(obj, null);
				Set = (obj,val) => pi.SetValue(obj, val, null);
				Init(pi, pi.PropertyType);
			}
			public ColumnMetaData(FieldInfo fi)
			{
				Get = fi.GetValue;
				Set = fi.SetValue;
				Init(fi, fi.FieldType);
			}
			
			/// <summary>Common constructor code</summary>
			private void Init(MemberInfo mi, Type type)
			{
				var attr = (ColumnAttribute)mi.GetCustomAttributes(typeof(ColumnAttribute), true)[0];
				var name = mi.Name;
				
				Name            = name;
				SqlDataType     = attr.SqlDataType != DataType.Null ? attr.SqlDataType : SqlType(type);
				Constraints     = attr.Constraints ?? "";
				IsPk            = attr.PrimaryKey;
				IsAutoInc       = attr.AutoInc;
				Order           = attr.Order;
				ClrType         = type;
				
				// Setup the bind and read methods
				try
				{
					if (ClrType.IsEnum)
					{
						// Special case enums
						BindFn = BindFunction[typeof(int)];
						ReadFn = ReadFunction[typeof(int)];
					}
					else
					{
						BindFn = BindFunction[ClrType];
						ReadFn = ReadFunction[ClrType];
					}
					return;
				}
				catch (KeyNotFoundException) {}
				throw new KeyNotFoundException(
					"A bind or read function was not found for type "+ClrType.Name+"\n" +
					"Custom types need to register a Bind/Read function in the " +
					"BindFunction/ReadFunction map before being used");
			}

			/// <summary>Returns the column definition for this column</summary>
			public string ColumnDef()
			{
				return Sql(Name," ",SqlDataType.ToString().ToLowerInvariant()," ",IsPk?"primary key ":"",IsAutoInc?"autoincrement ":"",Constraints);
			}

			public override string ToString()
			{
				return string.Format("{0} [{1}]" ,Name ,SqlDataType);
			}
		}

		#region Attribute types

		/// <summary>Controls the mapping from .NET type to database table</summary>
		[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
		public class TableAttribute :Attribute
		{
			/// <summary>
			/// If true, all properties/fields (selected by the given binding flags) are
			/// used as columns in the created table unless marked with the Sqlite.IgnoreAttribute.<para/>
			/// If false, all properties/fields (selected by the given binding flags) are
			/// not used as columns unless marked with the Sqlite.ColumnAttribute.<para/>
			/// Default value is false.</summary>
			public bool AllByDefault { get; set; }
			
			/// <summary>
			/// Binding flags used to reflect on properties in the type.<para/>
			/// Use BindingFlags.Default for none.<para/>
			/// Default value is BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;</summary>
			public BindingFlags PropertyBindingFlags { get; set; }
			
			/// <summary>
			/// Binding flags used to reflect on fields in the type.<para/>
			/// Use BindingFlags.Default for none.<para/>
			/// Default value is BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;</summary>
			public BindingFlags FieldBindingFlags { get; set; }
			
			/// <summary>
			/// Defines any table constraints to use when creating the table.<para/>
			/// This can be used to specify multiple primary keys at the class level<para/>
			/// e.g.<para/>
			///  Constraints = "unique (C1), primary key (C2, C3)"<para/>
			///  Column 'C1' is unique, columns C2 and C3 are the primary keys<para/>
			/// Default value is none.</summary>
			public string Constraints { get; set; }
			
			public TableAttribute()
			{
				AllByDefault = false;
				PropertyBindingFlags = BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
				FieldBindingFlags    = BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
				Constraints = null;
			}
		}

		/// <summary>Marks a property or field as a column in the db table for this type</summary>
		[AttributeUsage(AttributeTargets.Property|AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
		public class ColumnAttribute :Attribute
		{
			/// <summary>
			/// True if this column should be used as a primary key.
			/// If multiple primary keys are specified, ensure the Order
			/// property is used so that the order of primary keys is defined.</summary>
			public bool PrimaryKey { get; set; }
			
			/// <summary>True if this column should auto increment. Default is false</summary>
			public bool AutoInc { get; set; }
			
			/// <summary>Defines the order of columns in the table. By default, Order is MaxValue and column order isn't defined</summary>
			public int Order { get; set; }
			
			/// <summary>The sqlite data type used to represent this column. 
			/// If DataType.Null (default), then the default mapping from .NET data type to sqlite type is used.</summary>
			public DataType SqlDataType { get; set; }
			
			/// <summary>Custom constraints to add to this column. Default is none (null)</summary>
			public string Constraints { get; set; }
			
			public ColumnAttribute()
			{
				PrimaryKey  = false;
				AutoInc     = false;
				Order       = int.MaxValue;
				SqlDataType = DataType.Null;
				Constraints = null;
			}
		}

		/// <summary>
		/// Marks a property or field as not a column in the db table for this type.
		/// This attribute has higher precedence than the Sqlite.ColumnAttribute.</summary>
		[AttributeUsage(AttributeTargets.Property|AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
		public class IgnoreAttribute :Attribute
		{
		}

		#endregion

		#region Sqlite.Transaction

		/// <summary>An RAII class for transactional interaction with a database</summary>
		public class Transaction :IDisposable
		{
			// Lock so that only one thread can use a transaction at a time
			private readonly Database m_db;
			private bool m_completed;
			
			public Transaction(Database db)
			{
				m_db = db;
				m_completed = false;
				m_db.Execute("begin transaction");
			}
			public void Commit()
			{
				m_db.Execute("commit");
				m_completed = true;
			}
			public void Rollback()
			{
				m_db.Execute("rollback");
				m_completed = true;
			}
			public void Dispose()
			{
				if (m_completed) return;
				Rollback();
			}
		}

		#endregion

		#region Sqlite.Exception

		/// <summary>An exception type specifically for sqlite exceptions</summary>
		public class Exception :System.Exception
		{
			/// <summary>Helper for constructing new exceptions</summary>
			public static Exception New(Result res, string message) { return new Exception(message){Result = res}; }
			
			/// <summary>The result code associated with this exception</summary>
			public Result Result { get; private set; }
			
			public Exception() {}
			public Exception(string message):base(message) {}
			public Exception(SerializationInfo info, StreamingContext context) :base(info, context) {}
			public Exception(string message, System.Exception inner_exception):base(message, inner_exception) {}
			public override string ToString() { return string.Format("{0} - {1}" ,Result ,Message); }
		}

		#endregion

		#region Imported functions

		// ReSharper disable InconsistentNaming,UnusedMember.Local
		private static readonly IntPtr StaticData    = new IntPtr(0);
		private static readonly IntPtr TransientData = new IntPtr(-1);
		private delegate void sqlite3_destructor_type(IntPtr ptr);

		[DllImport("sqlite3", EntryPoint = "sqlite3_config", CallingConvention=CallingConvention.Cdecl)]
		public static extern Result sqlite3_config(ConfigOption option);

		[DllImport("sqlite3", EntryPoint = "sqlite3_open_v2", CallingConvention=CallingConvention.Cdecl)]
		private static extern Result sqlite3_open_v2(string filepath, out sqlite3 db, int flags, IntPtr zvfs);

		[DllImport("sqlite3", EntryPoint = "sqlite3_busy_timeout", CallingConvention=CallingConvention.Cdecl)]
		public static extern Result sqlite3_busy_timeout(sqlite3 db, int milliseconds);

		[DllImport("sqlite3", EntryPoint = "sqlite3_close", CallingConvention=CallingConvention.Cdecl)]
		private static extern Result sqlite3_close(sqlite3 db);

		[DllImport("sqlite3", EntryPoint = "sqlite3_free", CallingConvention=CallingConvention.Cdecl)]
		private static extern void sqlite3_free(IntPtr ptr);

		[DllImport("sqlite3", EntryPoint = "sqlite3_db_handle", CallingConvention=CallingConvention.Cdecl)]
		private static extern sqlite3 sqlite3_db_handle(sqlite3_stmt stmt);

		[DllImport("sqlite3", EntryPoint = "sqlite3_limit", CallingConvention=CallingConvention.Cdecl)]
		private static extern int sqlite3_limit(sqlite3 db, Limit limit_category, int new_value);

		[DllImport("sqlite3", EntryPoint = "sqlite3_prepare_v2", CallingConvention=CallingConvention.Cdecl)]
		private static extern Result sqlite3_prepare_v2(sqlite3 db, byte[] sql, int num_bytes, out sqlite3_stmt stmt, IntPtr pzTail);

		[DllImport("sqlite3", EntryPoint = "sqlite3_prepare16_v2", CallingConvention=CallingConvention.Cdecl)]
		private static extern Result sqlite3_prepare16_v2(sqlite3 db, string sql, int num_bytes, out sqlite3_stmt stmt, IntPtr pzTail);

		[DllImport("sqlite3", EntryPoint = "sqlite3_changes", CallingConvention=CallingConvention.Cdecl)]
		private static extern int sqlite3_changes(sqlite3 db);

		[DllImport("sqlite3", EntryPoint = "sqlite3_last_insert_rowid", CallingConvention=CallingConvention.Cdecl)]
		public static extern long sqlite3_last_insert_rowid(IntPtr db);

		[DllImport("sqlite3", EntryPoint = "sqlite3_errmsg16", CallingConvention=CallingConvention.Cdecl)]
		private static extern IntPtr sqlite3_errmsg16(sqlite3 db);

		[DllImport("sqlite3", EntryPoint = "sqlite3_reset", CallingConvention=CallingConvention.Cdecl)]
		private static extern Result sqlite3_reset(sqlite3_stmt stmt);

		[DllImport("sqlite3", EntryPoint = "sqlite3_step", CallingConvention=CallingConvention.Cdecl)]
		private static extern Result sqlite3_step(sqlite3_stmt stmt);

		[DllImport("sqlite3", EntryPoint = "sqlite3_finalize", CallingConvention=CallingConvention.Cdecl)]
		private static extern Result sqlite3_finalize(sqlite3_stmt stmt);


		[DllImport("sqlite3", EntryPoint = "sqlite3_column_name16", CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr sqlite3_column_name16(sqlite3_stmt stmt, int index);

		[DllImport("sqlite3", EntryPoint = "sqlite3_column_count", CallingConvention=CallingConvention.Cdecl)]
		private static extern int sqlite3_column_count(sqlite3_stmt stmt);

		[DllImport("sqlite3", EntryPoint = "sqlite3_column_type", CallingConvention=CallingConvention.Cdecl)]
		public static extern DataType sqlite3_column_type(sqlite3_stmt stmt, int index);

		[DllImport("sqlite3", EntryPoint = "sqlite3_column_int64", CallingConvention=CallingConvention.Cdecl)]
		public static extern Int64 sqlite3_column_int64(sqlite3_stmt stmt, int index);

		[DllImport("sqlite3", EntryPoint = "sqlite3_column_int", CallingConvention=CallingConvention.Cdecl)]
		public static extern Int32 sqlite3_column_int(sqlite3_stmt stmt, int index);

		[DllImport("sqlite3", EntryPoint = "sqlite3_column_double", CallingConvention=CallingConvention.Cdecl)]
		public static extern double sqlite3_column_double(sqlite3_stmt stmt, int index);

		[DllImport("sqlite3", EntryPoint = "sqlite3_column_text16", CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr sqlite3_column_text16(sqlite3_stmt stmt, int index);

		[DllImport("sqlite3", EntryPoint = "sqlite3_column_blob", CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr sqlite3_column_blob(sqlite3_stmt stmt, int index);

		[DllImport("sqlite3", EntryPoint = "sqlite3_column_bytes", CallingConvention=CallingConvention.Cdecl)]
		public static extern int sqlite3_column_bytes(sqlite3_stmt stmt, int index);


		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_parameter_count", CallingConvention=CallingConvention.Cdecl)]
		private static extern int sqlite3_bind_parameter_count(sqlite3_stmt stmt);

		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_parameter_index", CallingConvention=CallingConvention.Cdecl)]
		private static extern int sqlite3_bind_parameter_index(sqlite3_stmt stmt, string name);

		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_parameter_name", CallingConvention=CallingConvention.Cdecl)]
		private static extern IntPtr sqlite3_bind_parameter_name(sqlite3_stmt stmt, int index);

		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_null", CallingConvention=CallingConvention.Cdecl)]
		public static extern int sqlite3_bind_null(sqlite3_stmt stmt, int index);

		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_int", CallingConvention=CallingConvention.Cdecl)]
		public static extern int sqlite3_bind_int(sqlite3_stmt stmt, int index, int val);

		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_int64", CallingConvention=CallingConvention.Cdecl)]
		public static extern int sqlite3_bind_int64(sqlite3_stmt stmt, int index, long val);

		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_double", CallingConvention=CallingConvention.Cdecl)]
		public static extern int sqlite3_bind_double(sqlite3_stmt stmt, int index, double val);

		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_text16", CallingConvention=CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
		public static extern int sqlite3_bind_text16(sqlite3_stmt stmt, int index, string val, int n, IntPtr destructor_cb);

		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_blob", CallingConvention=CallingConvention.Cdecl)]
		public static extern int sqlite3_bind_blob(sqlite3_stmt stmt, int index, byte[] val, int n, IntPtr destructor_cb);
		// ReSharper restore InconsistentNaming,UnusedMember.Local

		#endregion

		#region SQLITE_TRACE
		private static class Trace
		{
			private class Info
			{
				public string CallStack { get; private set; }
				public Info() { CallStack = Environment.StackTrace; }
			}
			private static readonly Dictionary<Query, Info> m_trace = new Dictionary<Query,Info>();
			
			/// <summary>Records the creation of a new Query object</summary>
			[Conditional("SQLITE_TRACE")] public static void QueryCtor(Query query)
			{
				m_trace.Add(query, new Info());
			}
			
			/// <summary>Records the destruction of a Query object</summary>
			[Conditional("SQLITE_TRACE")] public static void QueryDtor(Query query)
			{
				m_trace.Remove(query);
			}
			
			/// <summary>Dumps the existing Query objects to standard error</summary>
			[Conditional("SQLITE_TRACE")] public static void Dump()
			{
				foreach (var t in m_trace)
					Debug.WriteLine("\nQuery:\n"+t.Value.CallStack);
			}
		}
		#endregion
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;

	[TestFixture] internal static partial class UnitTests
	{
		// ReSharper disable FieldCanBeMadeReadOnly.Local,MemberCanBePrivate.Local,UnusedMember.Local,CSharpWarnings::CS0659
		private enum SomeEnum { One, Two, Three }

		private class Custom
		{
			public string Str;
			public Custom()           { Str = "I'm a custom object"; }
			public Custom(string str) { Str = str; }
			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != typeof (Custom)) return false;
				return Equals((Custom) obj);
			}
			public bool Equals(Custom other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				return other.Str == Str;
			}

			/// <summary>Binds this type to a parameter in a prepared statement</summary>
			public static void SqliteBind(sqlite3_stmt stmt, int idx, object obj)
			{
				Sqlite.Bind.Text(stmt, idx, ((Custom)obj).Str);
			}
			public static Custom SqliteRead(sqlite3_stmt stmt, int idx)
			{
				return new Custom((string)Sqlite.Read.Text(stmt, idx));
			}
		}

		// Single primary key table
		private class DOMType
		{
			[Sqlite.Column(Order= 0, PrimaryKey = true, AutoInc = true, Constraints = "not null")] public int m_key;
			[Sqlite.Column(Order= 1)] protected bool      m_bool;
			[Sqlite.Column(Order= 2)] public sbyte        m_sbyte;
			[Sqlite.Column(Order= 3)] public byte         m_byte;
			[Sqlite.Column(Order= 4)] private char        m_char;
			[Sqlite.Column(Order= 5)] private short       m_short {get;set;}
			[Sqlite.Column(Order= 6)] public ushort       m_ushort {get;set;}
			[Sqlite.Column(Order= 7)] public int          m_int;
			[Sqlite.Column(Order= 8)] public uint         m_uint;
			[Sqlite.Column(Order= 9)] public long         m_int64;
			[Sqlite.Column(Order=10)] public ulong        m_uint64;
			[Sqlite.Column(Order=11)] public float        m_float;
			[Sqlite.Column(Order=12)] public double       m_double;
			[Sqlite.Column(Order=13)] public string       m_string;
			[Sqlite.Column(Order=14)] public byte[]       m_buf;
			[Sqlite.Column(Order=15)] public byte[]       m_empty_buf;
			[Sqlite.Column(Order=16)] public Guid         m_guid;
			[Sqlite.Column(Order=17)] public SomeEnum     m_enum;
			[Sqlite.Column(Order=18, SqlDataType = Sqlite.DataType.Text)] public Custom m_custom;
			// ReSharper disable NotAccessedField.Local
			public int Ignored;
			// ReSharper restore NotAccessedField.Local
			
			public DOMType() {}
			public DOMType(int val)
			{
				m_key        = -1;
				m_bool       = true;
				m_char       = 'X';
				m_sbyte      = 12;
				m_byte       = 12;
				m_short      = 1234;
				m_ushort     = 1234;
				m_int        = 12345678;
				m_uint       = 12345678;
				m_int64      = 1234567890000;
				m_uint64     = 1234567890000;
				m_float      = 1.234567f;
				m_double     = 1.2345678987654321;
				m_enum       = SomeEnum.One;
				m_string     = "string";
				m_guid       = Guid.NewGuid();
				m_buf        = new byte[]{0,1,2,3,4,5,6,7,8,9};
				m_empty_buf  = null;
				m_custom     = new Custom();
				Ignored      = val;
			}
			public bool Equals(DOMType other)
			{
				if (other.m_key     != m_key      ) return false;
				if (other.m_bool    != m_bool     ) return false;
				if (other.m_sbyte   != m_sbyte    ) return false;
				if (other.m_byte    != m_byte     ) return false;
				if (other.m_char    != m_char     ) return false;
				if (other.m_short   != m_short    ) return false;
				if (other.m_ushort  != m_ushort   ) return false;
				if (other.m_int     != m_int      ) return false;
				if (other.m_uint    != m_uint     ) return false;
				if (other.m_int64   != m_int64    ) return false;
				if (other.m_uint64  != m_uint64   ) return false;
				if (!Equals(other.m_string, m_string)       ) return false;
				if (!Equals(other.m_buf, m_buf)             ) return false;
				if (!Equals(other.m_empty_buf, m_empty_buf) ) return false;
				if (!other.m_guid.Equals(m_guid)            ) return false;
				if (!Equals(other.m_enum, m_enum)           ) return false;
				if (!Equals(other.m_custom, m_custom)       ) return false;
				if (Math.Abs(other.m_float   - m_float  ) > float .Epsilon) return false;
				if (Math.Abs(other.m_double  - m_double ) > double.Epsilon) return false;
				//&& other.Ignored == Ignored
				return true;
			}
			private static bool Equals(byte[] arr1, byte[] arr2)
			{
				if (arr1 == null) return arr2 == null || arr2.Length == 0;
				if (arr2 == null) return arr1.Length == 0;
				if (arr1.Length != arr2.Length) return false;
				return arr1.SequenceEqual(arr2);
			}
		}
		// ReSharper restore FieldCanBeMadeReadOnly.Local,MemberCanBePrivate.Local,UnusedMember.Local,CSharpWarnings::CS0659

		[Test] public static void TestSqlite1()
		{
			// Register custom type bind/read methods
			Sqlite.BindFunction.Add(typeof(Custom), Custom.SqliteBind);
			Sqlite.ReadFunction.Add(typeof(Custom), Custom.SqliteRead);
			
			// Use single threading
			Sqlite.Configure(Sqlite.ConfigOption.SingleThread);
			
			// Create/Open the database connection
			const string FilePath = "tmpDB.db";
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create|Sqlite.OpenFlags.ReadWrite|Sqlite.OpenFlags.NoMutex))
			{
				// Create a table
				db.DropTable<DOMType>();
				db.CreateTable<DOMType>();
				Assert.IsTrue(db.TableExists<DOMType>());
				
				// Check the table
				var table = db.Table<DOMType>();
				Assert.AreEqual(19, table.ColumnCount);
				using (var q = table.Query("select * from "+table.Name))
				{
					Assert.AreEqual(19, q.ColumnCount);
					Assert.AreEqual("m_key"       ,q.ColumnName( 0));
					Assert.AreEqual("m_bool"      ,q.ColumnName( 1));
					Assert.AreEqual("m_sbyte"     ,q.ColumnName( 2));
					Assert.AreEqual("m_byte"      ,q.ColumnName( 3));
					Assert.AreEqual("m_char"      ,q.ColumnName( 4));
					Assert.AreEqual("m_short"     ,q.ColumnName( 5));
					Assert.AreEqual("m_ushort"    ,q.ColumnName( 6));
					Assert.AreEqual("m_int"       ,q.ColumnName( 7));
					Assert.AreEqual("m_uint"      ,q.ColumnName( 8));
					Assert.AreEqual("m_int64"     ,q.ColumnName( 9));
					Assert.AreEqual("m_uint64"    ,q.ColumnName(10));
					Assert.AreEqual("m_float"     ,q.ColumnName(11));
					Assert.AreEqual("m_double"    ,q.ColumnName(12));
					Assert.AreEqual("m_string"    ,q.ColumnName(13));
					Assert.AreEqual("m_buf"       ,q.ColumnName(14));
					Assert.AreEqual("m_empty_buf" ,q.ColumnName(15));
					Assert.AreEqual("m_guid"      ,q.ColumnName(16));
					Assert.AreEqual("m_enum"      ,q.ColumnName(17));
					Assert.AreEqual("m_custom"    ,q.ColumnName(18));
				}
				
				// Create some objects to stick in the table
				var obj1 = new DOMType(5);
				var obj2 = new DOMType(6);
				var obj3 = new DOMType(7);
				
				// Insert stuff
				Assert.AreEqual(1, table.Insert(obj1, out obj1.m_key));
				Assert.AreEqual(1, table.Insert(obj2, out obj2.m_key));
				Assert.AreEqual(1, table.Insert(obj3, out obj3.m_key));
				
				// Get stuff and check it's the same
				var OBJ0 = table.Find(0);
				var OBJ1 = table.Get(obj1.m_key);
				var OBJ2 = table.Get(obj2.m_key);
				var OBJ3 = table.Get(obj3.m_key);
				Assert.IsNull(OBJ0);
				Assert.IsTrue(obj1.Equals(OBJ1));
				Assert.IsTrue(obj2.Equals(OBJ2));
				Assert.IsTrue(obj3.Equals(OBJ3));
				
				// Check parameter binding
				using (var q = table.Query("select m_string,m_int from "+table.Name+" where m_string = @p1 and m_int = @p2"))
				{
					Assert.AreEqual(2, q.ParmCount);
					Assert.AreEqual("@p1", q.ParmName(1));
					Assert.AreEqual("@p2", q.ParmName(2));
					Assert.AreEqual(1, q.ParmIndex("@p1"));
					Assert.AreEqual(2, q.ParmIndex("@p2"));
					q.BindParm(1, "string");
					q.BindParm(2, 12345678);
					
					// Run the query
					Assert.IsTrue(q.Step());
					
					// Read the results
					Assert.AreEqual(2, q.ColumnCount);
					Assert.AreEqual(Sqlite.DataType.Text    ,q.ColumnType(0));
					Assert.AreEqual(Sqlite.DataType.Integer ,q.ColumnType(1));
					Assert.AreEqual("m_string"              ,q.ColumnName(0));
					Assert.AreEqual("m_int"                 ,q.ColumnName(1));
					Assert.AreEqual("string"                ,q.ReadColumn<string>(0));
					Assert.AreEqual(12345678                ,q.ReadColumn<int>(1));
					
					// There should be 3 rows
					Assert.IsTrue(q.Step());
					Assert.IsTrue(q.Step());
					Assert.IsFalse(q.Step());
				}
				
				// Update stuff
				obj2.m_string = "I've been modified";
				Assert.AreEqual(1, table.Update(obj2));
				
				// Get the updated stuff and check it's been updated
				OBJ2 = table.Find(obj2.m_key);
				Assert.IsNotNull(OBJ2);
				Assert.IsTrue(obj2.Equals(OBJ2));
				
				// Delete something and check it's gone
				Assert.AreEqual(1, table.Delete(obj3));
				OBJ3 = table.Find(obj3.m_key);
				Assert.IsNull(OBJ3);
				
				// Update a single column and check it
				obj1.m_byte = 55;
				Assert.AreEqual(1, table.Update("m_byte", obj1.m_byte, 1));
				OBJ1 = table.Get(obj1.m_key);
				Assert.IsNotNull(OBJ1);
				Assert.IsTrue(obj1.Equals(OBJ1));
				
				// Read a single column
				var val = table.ColumnValue<ushort>("m_ushort", 2);
				Assert.AreEqual(obj2.m_ushort, val);
				
				// Add something back
				Assert.AreEqual(1, table.Insert(obj3, out obj3.m_key));
				OBJ3 = table.Get(obj3.m_key);
				Assert.IsNotNull(OBJ3);
				Assert.IsTrue(obj3.Equals(OBJ3));
				
				// Enumerate objects
				var objs = table.Select(x => x).ToArray();
				Assert.AreEqual(3, objs.Length);
				Assert.IsTrue(obj1.Equals(objs[0]));
				Assert.IsTrue(obj2.Equals(objs[1]));
				Assert.IsTrue(obj3.Equals(objs[2]));
			}
		}

		// todo: test with multiple primary keys
	}
}
#endif