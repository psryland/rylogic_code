#define SQLITE_HANDLES
#define COMPILED_LAMBDAS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using Rylogic.Extn;
using Rylogic.Interop.Win32;

// Provides methods for creating database tables by reflecting type definitions.
//
// Notes/Usage:
//  Use Case 1:
//   Create a type that defines the columns of a table (using the Sqlite attributes).
//   Use CreateTable, DropTable, etc to create the table in the DB.
//   Use db.Table<Type>() to get an instance of the table interface.
//   Use table.Insert/Get/Update/etc to access rows in the table.
//
//  Use Case 2:
//   Create a table from arbitrary types using CreateTableCmd or raw sql:
//     e.g. Sql("create table if not exists ",table_name," (\n",
//              "[",nameof(Type.Timestamp),"] integer unique,\n",
//              "[",nameof(Type.Rate     ),"] real,\n",
//              "[",nameof(Type.Name     ),"] text)"
//              );
//  Insert data into the table using
//    e.g. Sql("insert or replace into ",table_name," (",
//             "[",nameof(Type.Timestamp),"],",
//             "[",nameof(Type.Rate     ),"],",
//             "[",nameof(Type.Name     ),"])",
//             " values (",
//             "?,", // Timestamp
//             "?,", // Rate     
//             "?)"  // Name     
//             );
//
// - Your domain objects must be classes with a default constructor.
// - Create a type and attribute it using these attribute types
//   (see comments within each type for details):
//      Sqlite.TableAttribute
//      Sqlite.ColumnAttribute
//      Sqlite.IgnoreAttribute
// - Open a database connection using an instance of 'Sqlite.Database'
// - Create/Access tables, insert, update, delete, transact, etc
// - See the unit tests for example usages
//
// Decimals:
//  Decimals cannot be stored as a number in sqlite due to loss of precision
//  so this implementation stores them as a text field.
//
// Custom Accessors:
//  I considered an ICustomAccessors interface where custom types would
//  provide custom method for 'getting' and 'setting' themselves, however
//  this approach won't work for 3rd party types (e.g. Size, Point, etc)
//  Also, ICustomAccessors implementers would have to return a built in
//  ClrType to indicate the type returned by their get/set methods, since
//  the meta data is generated from type information without an instance,
//  an interface is not appropriate for getting this type.
//  For custom types use the BindFunction/ReadFunction map to convert your
//  custom type to one of the supported sqlite data types.
//
// Thread Safety:
//  Sqlite is thread safe if the dll has been built with SQLITE_THREADSAFE
//  defined != 0 and the DB connection is opened with OpenFlags.FullMutex.
//  For performance however, sqlite should be used in a single-threaded way.
//
// Caching:
//  Caching compiled SQL statements does increase performance, however there
//  are issues with multi-threading. An SQL statement cannot be created on
//  one thread and released on another.

namespace Rylogic.Db
{
	public static class Sqlite
	{
		// Consider Graveyarding this class.
		// Use: System.Data.SQLite.SQLiteConnection, Dapper, and raw SQL
		// is a much better option that ORMs.

		#region Constants

		/// <summary>Use this as the file path when you want a database in RAM</summary>
		public const string DBInMemory = ":memory:";

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
			NoMem      = 7,   // A 'malloc' failed
			ReadOnly   = 8,   // Attempt to write a readonly database
			Interrupt  = 9,   // Operation terminated by sqlite3_interrupt()
			IOError    = 10,  // Some kind of disk I/O error occurred
			Corrupt    = 11,  // The database disk image is malformed
			NotFound   = 12,  // Unknown op-code in sqlite3_file_control()
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

		/// <summary></summary>
		public enum AuthorizerActionCode
		{                               //   3rd             4th parameter
			CREATE_INDEX        =  1,   // Index Name      Table Name
			CREATE_TABLE        =  2,   // Table Name      NULL
			CREATE_TEMP_INDEX   =  3,   // Index Name      Table Name
			CREATE_TEMP_TABLE   =  4,   // Table Name      NULL
			CREATE_TEMP_TRIGGER =  5,   // Trigger Name    Table Name
			CREATE_TEMP_VIEW    =  6,   // View Name       NULL
			CREATE_TRIGGER      =  7,   // Trigger Name    Table Name
			CREATE_VIEW         =  8,   // View Name       NULL
			DELETE              =  9,   // Table Name      NULL
			DROP_INDEX          = 10,   // Index Name      Table Name
			DROP_TABLE          = 11,   // Table Name      NULL
			DROP_TEMP_INDEX     = 12,   // Index Name      Table Name
			DROP_TEMP_TABLE     = 13,   // Table Name      NULL
			DROP_TEMP_TRIGGER   = 14,   // Trigger Name    Table Name
			DROP_TEMP_VIEW      = 15,   // View Name       NULL
			DROP_TRIGGER        = 16,   // Trigger Name    Table Name
			DROP_VIEW           = 17,   // View Name       NULL
			INSERT              = 18,   // Table Name      NULL
			PRAGMA              = 19,   // Pragma Name     1st arg or NULL
			READ                = 20,   // Table Name      Column Name
			SELECT              = 21,   // NULL            NULL
			TRANSACTION         = 22,   // Operation       NULL
			UPDATE              = 23,   // Table Name      Column Name
			ATTACH              = 24,   // Filename        NULL
			DETACH              = 25,   // Database Name   NULL
			ALTER_TABLE         = 26,   // Database Name   Table Name
			REINDEX             = 27,   // Index Name      NULL
			ANALYZE             = 28,   // Table Name      NULL
			CREATE_VTABLE       = 29,   // Table Name      Module Name
			DROP_VTABLE         = 30,   // Table Name      Module Name
			FUNCTION            = 31,   // NULL            Function Name
			SAVEPOINT           = 32,   // Operation       Save point Name
			COPY                =  0,   // No longer used
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

		/// <summary>Behaviour to perform when creating a table detects an already existing table with the same name</summary>
		public enum OnCreateConstraint
		{
			/// <summary>The create operation will produce an error</summary>
			Reject,

			/// <summary>The create operation will ignore the create command if a table with the same name already exists</summary>
			IfNotExists,

			/// <summary>If a table with the same name already exists, it will be altered to match the new schema</summary>
			AlterTable,
		}

		/// <summary>Behaviour to perform when an insert operation detects a constraint</summary>
		public enum OnInsertConstraint
		{
			/// <summary>The insert operation will produce an error</summary>
			Reject,

			/// <summary>The insert operation will be ignored</summary>
			Ignore,

			/// <summary>The insert operation will replace the existing item</summary>
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

		/// <summary>Change type provided in the DataChanged event</summary>
		public enum ChangeType
		{
			Insert      = AuthorizerActionCode.INSERT,
			Update      = AuthorizerActionCode.UPDATE,
			Delete      = AuthorizerActionCode.DELETE,
			CreateTable = AuthorizerActionCode.CREATE_TABLE,
			AlterTable  = AuthorizerActionCode.ALTER_TABLE,
			DropTable   = AuthorizerActionCode.DROP_TABLE,
		}

		#endregion

		#region Handles

		/// <summary>A database handle</summary>
		public interface sqlite3
		{
			/// <summary>The result from closing this handle</summary>
			Result CloseResult { get; }

			/// <summary>True if the handle is invalid</summary>
			bool IsInvalid { get; }

			/// <summary>True if the handle has been closed</summary>
			bool IsClosed { get; }

			/// <summary>Close the handle</summary>
			void Close();
		}

		/// <summary>A compiled query handle (Statement)</summary>
		public interface sqlite3_stmt
		{
			/// <summary>The result from closing this handle</summary>
			Result CloseResult { get; }

			/// <summary>True if the handle is invalid</summary>
			bool IsInvalid { get; }

			/// <summary>True if the handle has been closed</summary>
			bool IsClosed { get; }

			/// <summary>Close the handle</summary>
			void Close();
		}

		#endregion

		#region Callbacks

		/// <summary>Callback type for sqlite3_update_hook</summary>
		/// <param name="ctx">A copy of the third argument to sqlite3_update_hook()</param>
		/// <param name="change_type">One of SQLITE_INSERT, SQLITE_DELETE, or SQLITE_UPDATE</param>
		/// <param name="db_name">The name of the affected database</param>
		/// <param name="table_name">The name of the table containing the changed item</param>
		/// <param name="row_id">The row id of the changed row</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void UpdateHookCB(IntPtr ctx, int change_type, string db_name, string table_name, long row_id);

		#endregion

		#region Global methods

		/// <summary>
		/// Sets a global configuration option for sqlite. Must be called prior
		/// to initialisation or after shutdown of sqlite. Initialisation happens
		/// implicitly when Open is called.</summary>
		public static void Configure(ConfigOption option)
		{
			NativeDll.Config(option);
		}

		/// <summary>Returns the sqlite data type to use for a given .NET type.</summary>
		public static DataType SqlType(Type type)
		{
			type = Nullable.GetUnderlyingType(type) ?? type;
			if (type == null)
				return DataType.Null;
			if (type == typeof(bool)           ||
				type == typeof(byte)           ||
				type == typeof(sbyte)          ||
				type == typeof(Char)           ||
				type == typeof(UInt16)         ||
				type == typeof(Int16)          ||
				type == typeof(Int32)          ||
				type == typeof(UInt32)         ||
				type == typeof(Int64)          ||
				type == typeof(UInt64)         ||
				type == typeof(DateTimeOffset) ||
				type == typeof(Color) ||
				type.IsEnum)
				return DataType.Integer;
			if (type == typeof(Single)         ||
				type == typeof(Double))
				return DataType.Real;
			if (type == typeof(String)         ||
				type == typeof(Guid)           ||
				type == typeof(Decimal))
				return DataType.Text;
			if (type == typeof(byte[])         ||
				type == typeof(int[])          ||
				type == typeof(long[]))
				return DataType.Blob;

			throw new NotSupportedException(
				"No default type mapping for type "+type.Name+"\n" +
				"Custom data types should specify a value for the " +
				"'SqlDataType' property in their ColumnAttribute");
		}

		#region Sql String Helpers
		
		/// <summary>A helper for gluing strings together</summary>
		[DebuggerStepThrough] public static string Sql(params object[] parts)
		{
			m_sql_cached_sb ??= new StringBuilder();
			return Sql(m_sql_cached_sb, parts);
		}
		[DebuggerStepThrough] public static string Sql(StringBuilder sb, params object[] parts)
		{
			sb.Length = 0;
			SqlAppend(sb, parts);
			return sb.ToString();
		}
		[DebuggerStepThrough] public static void SqlAppend(StringBuilder sb, params object[] parts)
		{
			// Do not change this to automatically add white space,
			// 'p' might be within quoted string which can turn this:
			//  name='Table0' into this: name=' Table0 ';
			foreach (var p in parts)
				sb.Append(p);
		}
		[ThreadStatic] private static StringBuilder? m_sql_cached_sb; // no initialised for thread statics

		#endregion

		/// <summary>
		/// Returns the primary key values read from 'item'.
		/// The returned array can be pass to methods that take 'params object[]' arguments</summary>
		public static object[] PrimaryKeys(object item)
		{
			var meta = TableMetaData.GetMetaData(item.GetType());
			return meta.Pks.Select(x => x.Get(item)).ToArray();
		}

		/// <summary>Return the sql string used to create a table for 'type'</summary>
		public static string CreateTableCmd(Type type, OnCreateConstraint on_constraint = OnCreateConstraint.Reject)
		{
			return CreateTableCmd(TableMetaData.GetMetaData(type).Name, type, on_constraint);
		}
		public static string CreateTableCmd(string table_name, Type type, OnCreateConstraint on_constraint = OnCreateConstraint.Reject)
		{
			var meta = TableMetaData.GetMetaData(type);
			return Sql("create table ",on_constraint == OnCreateConstraint.Reject?string.Empty:"if not exists ",table_name,"(\n",meta.Decl(),")");
		}

		/// <summary>Returns the sql 'insert' command string adding a row to a table based on 'type'</summary>
		public static string InsertCmd(Type type, OnInsertConstraint on_constraint = OnInsertConstraint.Reject)
		{
			return InsertCmd(TableMetaData.GetMetaData(type).Name, type, on_constraint);
		}
		public static string InsertCmd(string table_name, Type type, OnInsertConstraint on_constraint = OnInsertConstraint.Reject)
		{
			// Get the constraints
			string cons;
			switch (on_constraint)
			{
			default: throw new SqliteException(Result.Misuse, "Unknown OnInsertConstraint behaviour");
			case OnInsertConstraint.Reject:  cons = ""; break;
			case OnInsertConstraint.Ignore:  cons = "or ignore"; break;
			case OnInsertConstraint.Replace: cons = "or replace"; break;
			}

			// Generate the insert command string
			var meta = TableMetaData.GetMetaData(type);
			if (meta.NonAutoIncs.Length == 0) throw new SqliteException(Result.Misuse, "Cannot insert an item no fields (or with auto increment fields only)");
			return Sql(
				"insert ",cons," into ",table_name," (",
				string.Join(",",meta.NonAutoIncs.Select(c => c.NameBracketed)),
				") values (",
				string.Join(",",meta.NonAutoIncs.Select(c => "?")),
				")");
		}

		/// <summary>Returns the sql string for the update command for type 'type'</summary>
		public static string UpdateCmd(Type type)
		{
			return UpdateCmd(TableMetaData.GetMetaData(type).Name, type);
		}
		public static string UpdateCmd(string table_name, Type type)
		{
			var meta = TableMetaData.GetMetaData(type);
			if (meta.Pks.Length    == 0) throw new SqliteException(Result.Misuse, "Cannot update an item with no primary keys since it cannot be identified");
			if (meta.NonPks.Length == 0) throw new SqliteException(Result.Misuse, "Cannot update an item with no non-primary key fields since there are no fields to change");
			return Sql(
				"update ",table_name," set ",
				string.Join(",", meta.NonPks.Select(x => x.NameBracketed+" = ?")),
				" where ",
				string.Join(" and ", meta.Pks.Select(x => x.NameBracketed+" = ?"))
				);
		}

		/// <summary>Returns the sql string for the delete command for type 'type'</summary>
		public static string DeleteCmd(Type type)
		{
			return DeleteCmd(TableMetaData.GetMetaData(type).Name, type);
		}
		public static string DeleteCmd(string table_name, Type type)
		{
			var meta = TableMetaData.GetMetaData(type);
			if (meta.Pks.Length == 0) throw new SqliteException(Result.Misuse, "Cannot delete an item with no primary keys since it cannot be identified");
			return Sql("delete from ",table_name," where ",meta.PkConstraints());
		}

		/// <summary>Returns the sql string for the get command for type 'type'</summary>
		public static string GetCmd(Type type)
		{
			return GetCmd(TableMetaData.GetMetaData(type).Name, type);
		}
		public static string GetCmd(string table_name, Type type)
		{
			var meta = TableMetaData.GetMetaData(type);
			if (meta.Pks.Length == 0) throw new SqliteException(Result.Misuse, "Cannot get an item with no primary keys since it cannot be identified");
			return Sql("select * from ",table_name," where ",meta.PkConstraints());
		}

		/// <summary>Returns the sql string for returning all rows of type 'type'</summary>
		public static string SelectAllCmd(Type type)
		{
			return SelectAllCmd(TableMetaData.GetMetaData(type).Name, type);
		}
		public static string SelectAllCmd(string table_name, Type type)
		{
			var meta = TableMetaData.GetMetaData(type);
			return Sql("select * from ",table_name);
		}

		/// <summary>Compiles an sql string into an sqlite3 prepared statement</summary>
		private static sqlite3_stmt Compile(Database db, string sql_string)
		{
			return NativeDll.Prepare(db.Handle, sql_string);
		}

		#endregion

		#region Database

		/// <summary>
		/// Represents a connection to an sqlite database file.<para/>
		/// Database connections are relatively light-weight, applications may create multiple
		/// connections to the same database. Database connections should *NOT* be shared across
		/// multiple threads.<para/>
		/// SQLite provides isolation between operations in separate database connections.
		/// However, there is no isolation between operations that occur within the same
		/// database connection.<para/>
		/// i.e. you can have multiple of these concurrently, e.g. one per thread</summary>
		public class Database :IDisposable
		{
			private readonly sqlite3 m_db;
			private readonly OpenFlags m_flags;
			private readonly UpdateHookCB m_update_cb_handle;
			private GCHandle m_this_handle;
			#if SQLITE_HANDLES
			public HashSet<int> m_queries = new HashSet<int>();
			public int m_query_id = 0;
			#endif

			/// <summary>
			/// Opens a connection to the database.
			/// Use the copy constructor to create a database connection for a different thread</summary>
			public Database(string filepath, OpenFlags flags = OpenFlags.Create|OpenFlags.ReadWrite)
			{
				Filepath = filepath;
				ReadItemHook = x => x;
				WriteItemHook = x => x;
				OwningThreadId = Thread.CurrentThread.ManagedThreadId;
				SyncContext = SynchronizationContext.Current ?? new SynchronizationContext();
				m_this_handle = GCHandle.Alloc(this);

				// Open the database file
				m_db = NativeDll.Open(filepath, flags);
				m_flags = flags;

				// Connect the update callback
				m_update_cb_handle = UpdateCB;
				NativeDll.UpdateHook(m_db, m_update_cb_handle, GCHandle.ToIntPtr(m_this_handle));

				// Default to no busy timeout
				BusyTimeout = 0;
			}
			public Database(Database rhs)
				:this(rhs.Filepath, rhs.m_flags)
			{
				if (rhs.Filepath == DBInMemory)
					throw new SqliteException("Cannot open multiple connections to an in-memory database");
			}
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}
			protected virtual void Dispose(bool _)
			{
				Close();
				m_this_handle.Free();
			}

			/// <summary>The main synchronisation context</summary>
			private SynchronizationContext SyncContext { get; }

			/// <summary>The filepath of the database file</summary>
			public string Filepath { get; private set; }

			///<summary>
			/// The Id of the thread that created this DB handle.
			/// For asynchronous access to the database, create 'Database' instances for each thread.</summary>
			public int OwningThreadId { get; private set; }

			/// <summary>Returns the DB handle after asserting it's validity</summary>
			public sqlite3 Handle
			{
				get
				{
					Debug.Assert(AssertCorrectThread());
					Debug.Assert(!m_db.IsInvalid, "Invalid database handle");
					return m_db;
				}
			}

			/// <summary>
			/// Sets the busy timeout period in milliseconds. This controls how long the thread
			/// sleeps for when waiting to gain a lock on a table. After this timeout period Step()
			/// will return Result.Busy. Set to 0 to turn off the busy wait handler.</summary>
			public int BusyTimeout
			{
				set { NativeDll.BusyTimeout(Handle, value); }
			}

			/// <summary>A method that allows interception of all database object reads</summary>
			public Func<object,object> ReadItemHook { get; set; }

			/// <summary>A method that allows interception of all database object writes</summary>
			public Func<object,object> WriteItemHook { get; set; }

			/// <summary>Close a database file</summary>
			public void Close()
			{
				Debug.Assert(AssertCorrectThread());
				if (m_db.IsClosed) return;

				// Release the DB handle
				m_db.Close();
				if (m_db.CloseResult == Result.Busy)
					throw new SqliteException(m_db.CloseResult, "Could not close database handle, there are still prepared statements that haven't been 'finalized' or blob handles that haven't been closed.");
				if (m_db.CloseResult != Result.OK)
					throw new SqliteException(m_db.CloseResult, "Failed to close database connection");
			}

			/// <summary>
			/// Executes an sql command that is expected to not return results. e.g. Insert/Update/Delete.
			/// Returns the number of rows affected by the operation. Remember 'Query' requires disposing.</summary>
			public int Execute(string sql, int first_idx = 1, IEnumerable<object?>? parms = null)
			{
				using var query = new Query(this, sql, first_idx, parms);
				return Execute(query);
			}
			public int Execute(Query query)
			{
				Debug.Assert(AssertCorrectThread());
				return query.Run();
			}

			/// <summary>Execute an sql query that returns a scalar (i.e. int) result</summary>
			public int ExecuteScalar(string sql, int first_idx = 1, IEnumerable<object?>? parms = null)
			{
				using var query = new Query(this, sql, first_idx, parms);
				return ExecuteScalar(query);
			}
			public int ExecuteScalar(Query query)
			{
				Debug.Assert(AssertCorrectThread());

				if (!query.Step())
					throw new SqliteException(Result.Error, "Scalar query returned no results");

				var value = (int)Read.Int(query.Stmt, 0);

				if (query.Step())
					throw new SqliteException(Result.Error, "Scalar query returned more than one result");

				return value;
			}

			/// <summary>Returns the RowId for the last inserted row</summary>
			public long LastInsertRowId => NativeDll.LastInsertRowId(m_db);

			// Notes:
			//  How to Enumerate rows of queries that select specific columns, or use functions:
			//  Use (or create) a type with members mapped to the column names returned.
			//  e.g. Create a type and use the Sqlite.Column attribute to set the Name for
			//  each member so that is matches the names returned in the query.
			//  Or, change the query so that the returned columns are given names matching the
			//  members of your type.

			/// <summary>Executes a query and enumerates the rows, returning each row as an instance of type 'T'</summary>
			public IEnumerable<T> EnumRows<T>(string sql, int first_idx = 1, IEnumerable<object?>? parms = null)
			{
				return EnumRows(typeof(T), sql, first_idx, parms).Cast<T>();
			}
			public IEnumerable EnumRows(Type type, string sql, int first_idx = 1, IEnumerable<object?>? parms = null)
			{
				// Can't just return 'EnumRows(type, query)' because Dispose() is called on 'query'
				using var query = new Query(this, sql, first_idx, parms);
				foreach (var r in query.Rows(type))
					yield return r;
			}

			/// <summary>Drop any existing table created for type 'T'</summary>
			public void DropTable<T>(bool if_exists = true)
			{
				DropTable(typeof(T), if_exists);
			}
			public void DropTable(Type type, bool if_exists = true)
			{
				var meta = TableMetaData.GetMetaData(type);
				Execute(Sql("drop table ",if_exists?"if exists ":string.Empty, meta.Name));
				NotifyDataChanged(ChangeType.DropTable, meta.Name, 0);
			}

			/// <summary>
			/// Creates a table in the database based on type 'T'. Throws if not successful.<para/>
			/// 'factory' is a custom factory method to use to create instances of 'type'. If null,
			/// then 'Activator.CreateInstance' will be used and the type will require a default constructor.<para/>
			/// For information about sql create table syntax see:<para/>
			///  http://www.sqlite.org/syntaxdiagrams.html#create-table-stmt <para/>
			///  http://www.sqlite.org/syntaxdiagrams.html#table-constraint <para/>
			/// Notes: <para/>
			///  auto increment must follow primary key without anything in between<para/></summary>
			public void CreateTable<T>(OnCreateConstraint on_constraint = OnCreateConstraint.Reject) where T : new()
			{
				CreateTable<T>(() => new T(), on_constraint);
			}
			public void CreateTable<T>(Func<object> factory, OnCreateConstraint on_constraint = OnCreateConstraint.Reject)
			{
				CreateTable(typeof(T), factory, on_constraint);
			}
			public void CreateTable(Type type, Func<object>? factory = null, OnCreateConstraint on_constraint = OnCreateConstraint.Reject)
			{
				CreateTable(TableMetaData.GetMetaData(type).Name, type, factory, on_constraint);
			}
			public void CreateTable(string table_name, Type type, Func<object>? factory = null, OnCreateConstraint on_constraint = OnCreateConstraint.Reject)
			{
				var meta = TableMetaData.GetMetaData(type);
				if (factory != null)
					meta.Factory = factory;

				if (on_constraint == OnCreateConstraint.AlterTable && TableExists(type))
				{
					AlterTable(type);
				}
				else
				{
					Execute(Sql(CreateTableCmd(table_name, type, on_constraint)));
					NotifyDataChanged(ChangeType.CreateTable, meta.Name, 0);
				}
			}

			/// <summary>Alters an existing table to match the columns for 'type'</summary>
			public void AlterTable<T>()
			{
				AlterTable(typeof(T));
			}
			public void AlterTable(Type type)
			{
				// Read the columns that we want the table to have
				var meta = TableMetaData.GetMetaData(type);
				var cols0 = meta.Columns.Select(x => x.Name).ToList();

				// Read the existing columns that the table has
				var sql = Sql("pragma table_info(",meta.Name,")");
				var cols1 = EnumRows<TableInfo>(sql).Select(x => x.name).ToList();

				// Sqlite does not support the drop column syntax
				// // For each column in cols1, that isn't in cols0, drop it
				// foreach (var c in cols1)
				// {
				//     if (cols0.Contains(c)) continue;
				//     Execute(Sql("alter table ",meta.Name," drop column ",c));
				// }

				// For each column in cols0, that isn't in cols1, add it
				for (int i = 0, iend = cols0.Count; i != iend; ++i)
				{
					var c = cols0[i];
					if (cols1.Contains(c)) continue;
					Execute(Sql("alter table ",meta.Name," add column ",meta.Column(i).ColumnDef(true)));
				}
				NotifyDataChanged(ChangeType.AlterTable, meta.Name, 0);
			}

			/// <summary>Returns true if a table for type 'T' exists</summary>
			public bool TableExists<T>()
			{
				return TableExists(typeof(T));
			}
			public bool TableExists(Type type)
			{
				var sql = Sql("select count(*) from sqlite_master where type='table' and name='",type.Name,"'");
				return ExecuteScalar(sql) != 0;
			}

			/// <summary>Change the name of an existing table.</summary>
			public void RenameTable<T>(string new_name, bool update_meta_data = true)
			{
				RenameTable(typeof(T), new_name, update_meta_data);
			}
			public void RenameTable(Type type, string new_name, bool update_meta_data = true)
			{
				var meta = Sqlite.TableMetaData.GetMetaData(type);
				Execute(Sql("alter table ",meta.Name," rename to ",new_name));
				if (update_meta_data)
					meta.Name = new_name;
			}

			/// <summary>Return an interface to a table for type 'T'</summary>
			public Table<T> Table<T>()
			{
				return new Table<T>(this);
			}
			public Table<T> Table<T>(string table_name)
			{
				return new Table<T>(table_name, this);
			}
			public Table Table(Type type)
			{
				return new Table(type, this);
			}
			public Table Table(string table_name, Type type)
			{
				return new Table(table_name, type, this);
			}

			/// <summary>Factory method for creating 'Transaction' instances</summary>
			public Transaction NewTransaction()
			{
				if (m_transaction_in_progress != null)
					throw new SqliteException("Nested transactions on a single DB connection are not allowed");

				return m_transaction_in_progress = new Transaction(this, () => m_transaction_in_progress = null);
			}
			private Transaction? m_transaction_in_progress;

			/// <summary>Returns free DB pages to the OS reducing the DB file size</summary>
			public void Vacuum()
			{
				Execute("vacuum");
			}

			/// <summary>
			/// Raised whenever a row in the database is inserted, updated, or deleted. This event
			/// is invoked on Dispatcher.CurrentDispatcher to prevent accidentally modifying the database
			/// connection during the sqlite update callback. For immediate DataChanged notification use
			/// DataChangedImmediate.</summary>
			public event EventHandler<DataChangedArgs>? DataChanged;

			/// <summary>
			/// Raised whenever a row in the database is inserted, updated, or deleted.
			/// Handlers of this event *must not* do anything that will modify the database connection
			/// that invoked this event. Any actions to modify the database connection must be deferred
			/// until after the completion of the Step() call that triggered the update event. Note that
			/// sqlite3_prepare_v2() and sqlite3_step() both modify their database connections.</summary>
			public event EventHandler<DataChangedArgs>? DataChangedImmediate;

			/// <summary>Raise the data changed events</summary>
			private void NotifyDataChanged(ChangeType change_type, string table_name, long row_id)
			{
				var args = new DataChangedArgs(change_type, table_name, row_id);

				if (DataChangedImmediate != null)
					DataChangedImmediate(this, args);

				if (DataChanged != null && SyncContext != null)
					SyncContext.Post(_ => DataChanged?.Invoke(this, args), null);
					//Dispatcher.CurrentDispatcher.BeginInvoke(DataChanged, this,  args);
			}

			/// <summary>Callback passed to the sqlite dll when DataChanged is subscribed to</summary>
			private static void UpdateCB(IntPtr ctx, int change_type, string db_name, string table_name, long row_id)
			{
				// Mark with [MonoTouch.MonoPInvokeCallbackAttribute(typeof(UpdateHookCB))] for MONOTOUCH

				// 'db_name' is always "main". sqlite doesn't allow renaming of the DB
				var h = GCHandle.FromIntPtr(ctx);
				var db = (Database?)h.Target ?? throw new NullReferenceException();
				db.NotifyDataChanged((ChangeType)change_type, table_name, row_id);
			}

			/// <summary>Throw if called from a different thread than the one that created this connection</summary>
			public bool AssertCorrectThread()
			{
				// Sqlite can actually support multi-thread access via the same DB connection but it is
				// not recommended. In this ORM, I'm enforcing one thread per connection.
				// From the Sqlite Docs, these are the requirements:
				// If ConfigOption.SingleThreaded is used, all access must be from the thread that opened the DB connection.
				// If ConfigOption.MultiThreaded is used, only one thread at a time can use the DB connection.
				// If ConfigOption.Serialized is used, it's open session for any threads
				if (OwningThreadId != Thread.CurrentThread.ManagedThreadId)
				{
					throw new SqliteException(Result.Misuse, string.Format("Cross-thread use of Sqlite ORM.\n{0}", new StackTrace()));
				}
				return true;
			}
		}

		#endregion

		#region Table

		/// <summary>Represents a single table in the database</summary>
		public class Table :IEnumerable
		{
			protected readonly CmdExpr m_cmd;

			public Table(Type type, Database db)
				:this(null, type, db)
			{}
			public Table(string? table_name, Type type, Database db)
			{
				Debug.Assert(db.AssertCorrectThread());
				if (db.Handle.IsInvalid)
					throw new ArgumentNullException(nameof(db), "Invalid database handle");

				MetaData = TableMetaData.GetMetaData(type);
				DB = db;
				Name = table_name ?? MetaData.Name;
				m_cmd  = new CmdExpr();
			}

			/// <summary>The database connection this table uses associated with this table</summary>
			public Database DB { get; }

			/// <summary>Return the meta data for this table</summary>
			public TableMetaData MetaData { get; }

			/// <summary>The name of this table</summary>
			public string Name { get; }

			/// <summary>Gets the number of columns in this table</summary>
			public int ColumnCount => MetaData.ColumnCount;

			/// <summary>Get the number of rows in this table</summary>
			public int RowCount
			{
				get
				{
					GenerateExpression(null, "count");
					if (m_cmd.SqlString == null) throw new Exception($"Invalid Sql");
					try { return DB.ExecuteScalar(m_cmd.SqlString, 1, m_cmd.Arguments); }
					finally { ResetExpression(); }
				}
			}

			/// <summary>Returns a row in the table or null if not found</summary>
			public T Find<T>(params object?[] keys)
			{
				return (T)(Find(keys) ?? default!);
			}
			public T Find<T>(object key1, object key2) // overload for performance
			{
				return (T)(Find(key1, key2) ?? default!);
			}
			public T Find<T>(object key1) // overload for performance
			{
				return (T)(Find(key1) ?? default!);
			}
			public object? Find(params object?[] keys)
			{
				using var get = new Query(DB, GetCmd(MetaData.Type));
				get.BindPks(MetaData.Type, 1, keys);
				foreach (var x in get.Rows(MetaData.Type)) return x;
				return null;
			}
			public object? Find(object key1, object key2) // overload for performance
			{
				using var get = new Query(DB, GetCmd(MetaData.Type));
				get.BindPks(MetaData.Type, 1, key1, key2);
				foreach (var x in get.Rows(MetaData.Type)) return x;
				return null;
			}
			public object? Find(object key1) // overload for performance
			{
				using var get = new Query(DB, GetCmd(MetaData.Type));
				get.BindPks(MetaData.Type, 1, key1);
				foreach (var x in get.Rows(MetaData.Type)) return x;
				return null;
			}

			/// <summary>Returns a row in the table (throws if not found)</summary>
			public T Get<T>(params object?[] keys)
			{
				return (T)Get(keys);
			}
			public T Get<T>(object key1, object key2) // overload for performance
			{
				return (T)Get(key1, key2);
			}
			public T Get<T>(object key1) // overload for performance
			{
				return (T)Get(key1);
			}
			public object Get(params object?[] keys)
			{
				return Find(keys) ?? throw new SqliteException(Result.NotFound, "Row not found for key(s): " + string.Join(",", keys.Select(x => x?.ToString() ?? "<unk>")));
			}
			public object Get(object key1, object key2) // overload for performance
			{
				var item = Find(key1, key2);
				if (ReferenceEquals(item, null)) throw new SqliteException(Result.NotFound, "Row not found for keys: "+key1+","+key2);
				return item;
			}
			public object Get(object key1) // overload for performance
			{
				var item = Find(key1);
				if (ReferenceEquals(item, null)) throw new SqliteException(Result.NotFound, "Row not found for key: "+key1);
				return item;
			}

			/// <summary>Insert an item into the table.</summary>
			public int Insert(object item, OnInsertConstraint on_constraint = OnInsertConstraint.Reject)
			{
				using var insert = new Query(DB, InsertCmd(MetaData.Type, on_constraint));

				// Allow the object to be changed just prior to writing to the DB
				item = DB.WriteItemHook(item);

				// Bind the properties of the object to the query parameters
				MetaData.BindObj(insert.Stmt, 1, item, MetaData.NonAutoIncs);

				// Run the query
				var count = insert.Run();

				// Update the AutoIncrement property in 'item'
				MetaData.SetAutoIncPK(item, DB.Handle);

				// Return the number of rows affected
				return count;
			}

			/// <summary>Insert many items into the table.</summary>
			public int Insert(IEnumerable<object> items, OnInsertConstraint on_constraint = OnInsertConstraint.Reject)
			{
				using var insert = new Query(DB, InsertCmd(MetaData.Type, on_constraint));

				int count = 0;
				foreach (var item_ in items)
				{
					// Allow the object to be changed just prior to writing to the DB
					var item = DB.WriteItemHook(item_);

					// Bind the properties of the object to the query parameters
					MetaData.BindObj(insert.Stmt, 1, item, MetaData.NonAutoIncs);

					// Run the query
					count += insert.Run();

					// Update the AutoIncrement property in 'item'
					MetaData.SetAutoIncPK(item, DB.Handle);

					// Reset the query for the next item
					insert.Reset();
				}
				return count;
			}

			/// <summary>Update 'item' in the table</summary>
			public int Update(object item)
			{
				using var update = new Query(DB, UpdateCmd(MetaData.Type));

				// Allow the object to be changed just prior to writing to the DB
				item = DB.WriteItemHook(item);

				// Bind the properties of the object to the query parameters
				MetaData.BindObj(update.Stmt, 1, item, MetaData.NonPks);
				MetaData.BindObj(update.Stmt, 1 + MetaData.NonPks.Length, item, MetaData.Pks);

				// Run the query
				var count = update.Run();

				// Return the number of rows affected
				return count;
			}

			/// <summary>Delete 'item' from the table</summary>
			public int Delete(object item)
			{
				return DeleteByKey(PrimaryKeys(item));
			}

			/// <summary>Delete a row from the table. Returns the number of rows affected</summary>
			public int DeleteByKey(params object[] keys)
			{
				using var query = new Query(DB, DeleteCmd(MetaData.Type));
				query.BindParms(1, keys);
				query.Step();
				return query.RowsChanged;
			}

			/// <summary>Delete a row from the table. Returns the number of rows affected</summary>
			public int DeleteByKey(object key1, object key2) // overload for performance
			{
				using var query = new Query(DB, DeleteCmd(MetaData.Type));
				query.BindParms(1, key1, key2);
				query.Step();
				return query.RowsChanged;
			}

			/// <summary>Delete a row from the table. Returns the number of rows affected</summary>
			public int DeleteByKey(object key1) // overload for performance
			{
				using var query = new Query(DB, DeleteCmd(MetaData.Type));
				query.BindParms(1, key1);
				query.Step();
				return query.RowsChanged;
			}

			/// <summary>Update a single column for a single row in the table</summary>
			public int Update<TValueType>(string column_name, TValueType value, params object[] keys)
			{
				if (MetaData.Pks.Length == 0)
					throw new SqliteException(Result.Misuse, "Cannot update an item with no primary keys since it cannot be identified");

				// Get the affected column meta data
				var column_meta = MetaData.Column(column_name);
				if (column_meta == null)
					throw new SqliteException(Result.Misuse, $"No column with name {column_name} found");

				// Create the query for updating the column
				var sql = Sql("update ",Name," set ",column_meta.NameBracketed," = ? where ",MetaData.PkConstraints());
				using var query = new Query(DB, sql);

				// Bind the 'value' as the query parameter
				column_meta.BindFn(query.Stmt, 1, value!);

				// Bind the primary keys
				query.BindPks(MetaData.Type, 2, keys);

				// Run the update command
				query.Step();

				// Return the rows changed
				return query.RowsChanged;
			}

			/// <summary>Update the value of a column for all rows in a table</summary>
			public int UpdateAll<TValueType>(string column_name, TValueType value)
			{
				// Get the affected column meta data
				var column_meta = MetaData.Column(column_name);
				if (column_meta == null)
					throw new SqliteException(Result.Misuse, $"No column with name {column_name} found");

				// Create the query for updating all values in a column
				var sql = Sql("update ",Name," set ",column_name," = ?");
				using var query = new Query(DB, sql);

				// Bind the 'value' as the query parameter
				column_meta.BindFn(query.Stmt, 1, value!);

				// Run the update command
				query.Step();

				// Return the rows changed
				return query.RowsChanged;
			}

			/// <summary>Return the value of a specific column, in a single row, in the table</summary>
			public TValueType ColumnValue<TValueType>(string column_name, params object[] keys)
			{
				// Get the column meta data
				var column_meta = MetaData.Column(column_name);
				if (column_meta == null)
					throw new SqliteException(Result.Misuse, $"No column with name {column_name} found");

				// Create the query for selecting the column value for the row
				var sql = Sql("select ",column_meta.NameBracketed," from ",Name," where ",MetaData.PkConstraints());
				using var query = new Query(DB, sql);

				// Bind the primary keys to the query
				query.BindPks(MetaData.Type, 1, keys);

				// Run the query
				query.Step();

				// Return the column value
				return (TValueType)column_meta.ReadFn(query.Stmt, 0);
			}

			/// <summary>IEnumerable interface</summary>
			IEnumerator IEnumerable.GetEnumerator()
			{
				GenerateExpression();
				ResetExpression(); // Don't put this in a finally block because it gets called for each yield return
				if (m_cmd.SqlString == null) throw new Exception($"Invalid Sql");
				return DB.EnumRows(MetaData.Type, m_cmd.SqlString, 1, m_cmd.Arguments).GetEnumerator();
			}

			/// <summary>Generate the sql string and arguments</summary>
			public Table GenerateExpression(string? select = null, string? func = null)
			{
				m_cmd.Generate(MetaData.Type, select, func);
				return this;
			}

			/// <summary>Resets the generated sql string and arguments. Mainly used when an expression is generated but not used</summary>
			public Table ResetExpression()
			{
				m_cmd.Reset();
				return this;
			}

			/// <summary>Read only access to the generated sql string</summary>
			public string? SqlString => m_cmd.SqlString;

			/// <summary>Read only access to the generated sql arguments</summary>
			public List<object?>? Arguments => m_cmd.Arguments;

			/// <summary>Return the first row. Throws if no rows found</summary>
			public T First<T>()
			{
				m_cmd.Take(1);
				try { return ((IEnumerable<T>)this).First(); }
				finally { m_cmd.Reset(); }
			}
			public T First<T>(Expression<Func<T,bool>> pred)
			{
				m_cmd.Where(pred);
				return First<T>();
			}

			/// <summary>Return the first row or null</summary>
			public T FirstOrDefault<T>()
			{
				m_cmd.Take(1);
				try { return this.Cast<T>().FirstOrDefault(); }
				finally { m_cmd.Reset(); }
			}
			public T FirstOrDefault<T>(Expression<Func<T,bool>> pred)
			{
				m_cmd.Where(pred);
				return FirstOrDefault<T>();
			}

			/// <summary>Add a 'count' clause to row enumeration</summary>
			public int Count<T>(Expression<Func<T,bool>> pred)
			{
				m_cmd.Where(pred);
				return RowCount;
			}
			public int Count() // Overload added to prevent accidental use of 'LINQ-to-Objects' Count() extension
			{
				return RowCount;
			}

			/// <summary>Delete rows from the table</summary>
			public int Delete<T>(Expression<Func<T,bool>> pred)
			{
				m_cmd.Where(pred);
				return Delete();
			}
			public int Delete()
			{
				GenerateExpression("delete");
				if (m_cmd.SqlString == null) throw new Exception($"Invalid Sql");
				try { return DB.Execute(m_cmd.SqlString, 1, m_cmd.Arguments); }
				finally { ResetExpression(); }
			}

			/// <summary>Add a 'select' clause to row enumeration</summary>
			public IEnumerable<U> Select<T,U>(Expression<Func<T,U>> pred)
			{
				m_cmd.Select(pred);
				GenerateExpression();
				if (m_cmd.SqlString == null) throw new Exception($"Invalid Sql");
				try { return DB.EnumRows<U>(m_cmd.SqlString, 1, m_cmd.Arguments); }
				finally { ResetExpression(); }
			}

			/// <summary>Add a 'where' clause to row enumeration</summary>
			public Table Where<T>(Expression<Func<T,bool>> pred)
			{
				m_cmd.Where(pred);
				return this;
			}

			/// <summary>Add an 'OrderBy' clause to row enumeration</summary>
			public Table OrderBy<T,U>(Expression<Func<T,U>> pred)
			{
				m_cmd.OrderBy(pred, true);
				return this;
			}

			/// <summary>Add an 'OrderByDescending' clause to row enumeration</summary>
			public Table OrderByDescending<T,U>(Expression<Func<T,U>> pred)
			{
				m_cmd.OrderBy(pred, false);
				return this;
			}

			/// <summary>Add a 'limit' clause to row enumeration</summary>
			public Table Take(int n)
			{
				m_cmd.Take(n);
				return this;
			}

			/// <summary>Add an 'offset' clause to row enumeration</summary>
			public Table Skip(int n)
			{
				m_cmd.Skip(n);
				return this;
			}

			#region Command Expression

			/// <summary>Wraps an sql expression for enumerating the rows of this table</summary>
			protected class CmdExpr
			{
				private Expression? m_select;
				private Expression? m_where;
				private string? m_order;
				private int? m_take;
				private int? m_skip;

				/// <summary></summary>
				public override string ToString()
				{
					var sb = new StringBuilder();
					sb.Append(SqlString ?? "<not generated>");
					if (Arguments != null)
					{
						sb.Append('[');
						foreach (var a in Arguments) sb.Append(a != null ? a.ToString() : "null").Append(',');
						sb.Length--;
						sb.Append(']');
					}
					return sb.ToString();
				}

				/// <summary>The command translated into sql. Invalid until 'Generate()' is called</summary>
				public string? SqlString { get; private set; }

				/// <summary>Arguments for '?'s in the generated SqlString. Invalid until 'Generate()' is called</summary>
				public List<object?>? Arguments { get; private set; }

				/// <summary>Reset the expression</summary>
				public void Reset()
				{
					m_select = null;
					m_where = null;
					m_order = null;
					m_take = null;
					m_skip = null;
				}

				/// <summary>Helper for resetting the generated string an arguments</summary>
				public void Ungenerate()
				{
					SqlString = null;
					Arguments = null;
				}

				/// <summary>Add a specific select clause to the command</summary>
				public void Select(Expression expr)
				{
					Ungenerate();
					var lambda = expr as LambdaExpression;
					if (lambda != null) expr = lambda.Body;
					m_select = expr;
				}

				/// <summary>Add a 'where' clause to the command</summary>
				public void Where(Expression expr)
				{
					Ungenerate();
					var lambda = expr as LambdaExpression;
					if (lambda != null) expr = lambda.Body;
					m_where = m_where == null ? expr : Expression.AndAlso(m_where, expr);
				}

				/// <summary>Add an 'order by' clause to the command</summary>
				public void OrderBy(Expression expr, bool ascending)
				{
					Ungenerate();
					var lambda = expr as LambdaExpression;
					if (lambda != null) expr = lambda.Body;

					var clause = Translate(expr);
					if (clause.Args.Count != 0)
						throw new NotSupportedException("OrderBy expressions do not support parameters");

					if (m_order == null) m_order = ""; else m_order += ",";
					m_order += clause.Text;
					if (!ascending) m_order += " desc";
				}

				/// <summary>Add a limit to the number of rows returned</summary>
				public void Take(int n)
				{
					Ungenerate();
					m_take = n;
				}

				/// <summary>Add an offset to the first row returned</summary>
				public void Skip(int n)
				{
					Ungenerate();
					m_skip = n;
				}

				/// <summary>Return an sql string representing this command</summary>
				public void Generate(Type type, string? select = null, string? func = null)
				{
					var meta = TableMetaData.GetMetaData(type);
					var cmd = new TranslateResult();
					select = select ?? "select";

					cmd.Text.Append(select);
					if (select == "select")
					{
						cmd.Text.Append(' ');
						if (func != null) cmd.Text.Append(func).Append('(');
						var clause = m_select != null ? Translate(m_select).Text.ToString() : "";
						cmd.Text.Append(clause.Length != 0 ? clause : "*");
						if (func != null) cmd.Text.Append(')');
					}
					cmd.Text.Append(" from ").Append(meta.Name);
					if (m_where != null)
					{
						var clause = Translate(m_where);
						cmd.Text.Append(" where ").Append(clause.Text);
						cmd.Args.AddRange(clause.Args);
					}
					if (m_order != null)
					{
						cmd.Text.Append(" order by ").Append(m_order);
					}
					if (m_take.HasValue)
					{
						cmd.Text.Append(" limit ").Append(m_take.Value);
					}
					if (m_skip.HasValue)
					{
						if (!m_take.HasValue) cmd.Text.Append(" limit -1 ");
						cmd.Text.Append(" offset ").Append(m_skip.Value);
					}

					SqlString = cmd.Text.ToString();
					Arguments = cmd.Args;
				}

				/// <summary>The result of a call to 'Translate'</summary>
				private class TranslateResult
				{
					public readonly StringBuilder Text = new StringBuilder();
					public readonly List<object?> Args = new List<object?>();
				}

				/// <summary>
				/// Translates an expression into an sql sub-string in 'text'.
				/// Returns an object associated with 'expr' where appropriate, otherwise null.</summary>
				private TranslateResult Translate(Expression expr, TranslateResult? result = null)
				{
					result ??= new TranslateResult();

					var binary_expr = expr as BinaryExpression;
					if (binary_expr != null)
					{
						result.Text.Append("(");
						Translate(binary_expr.Left ,result);
					}

					// Helper for testing if an expression is a null constant
					Func<Expression, bool> IsNullConstant = exp =>
					{
						if (exp.NodeType == ExpressionType.Constant) return ((ConstantExpression)exp).Value == null;
						if (Nullable.GetUnderlyingType(exp.Type) != null)
						{
							var res = Translate(exp);
							return res.Args.Count == 1 && res.Args[0] == null;
						}
						return false;
					};

					switch (expr.NodeType)
					{
					default:
						throw new NotSupportedException();
					case ExpressionType.Parameter:
						{
							return result;
						}
					case ExpressionType.New:
						{
							var ne = (NewExpression)expr;
							result.Text.Append(string.Join(",", ne.Arguments.Select(x => Translate(x).Text)));
							break;
						}
					case ExpressionType.MemberAccess:
						{
							var me = (MemberExpression)expr;
							if (me.Expression.NodeType == ExpressionType.Parameter)
							{
								result.Text.Append(me.Member.Name);
								return result;
							}

							// Try to evaluate the expression
							var res = Translate(me.Expression);

							// If a value cannot be determined from the expression, write the parameter name instead
							if (res.Args.Count == 0)
							{
								// Special cases for null-able's.
								if (Nullable.GetUnderlyingType(me.Expression.Type) != null)
								{
									if (me.Member.Name == "HasValue")
									{
										result.Text.Append("(").Append(res.Text).Append(" is not null)");
									}
									else if (me.Member.Name == "Value")
									{
										// This handles this case "table.Where(x => x.nullable.Value == 5)",
										// i.e. the resulting sql should be "select * from table where nullable == 5"
										result.Text.Append(res.Text);
									}
									else
									{
										throw new NotSupportedException("Unsupported member of nullable type for MemberExpression: " + me);
									}
								}
								else
								{
									result.Text.Append(me.Member.Name);
								}
								return result;
							}
							if (res.Args.Count != 1 || res.Args[0] == null)
								throw new NotSupportedException("Could not find the object instance for MemberExpression: " + me);
							
							var ob = me.Member.MemberType switch
							{
								MemberTypes.Property => ((PropertyInfo)me.Member).GetValue(res.Args[0], null),
								MemberTypes.Field => ((FieldInfo)me.Member).GetValue(res.Args[0]),
								_ => throw new NotSupportedException("MemberExpression: " + me.Member.MemberType.ToString()),
							};

							// Handle IEnumerable
							if (ob is IEnumerable && !(ob is string))
							{
								result.Text.Append("(");
								foreach (var a in (IEnumerable)ob)
								{
									result.Text.Append("?,");
									result.Args.Add(a);
								}
								if (result.Text[result.Text.Length-1] != ')') result.Text.Length--;
								result.Text.Append(")");
							}
							else
							{
								result.Text.Append("?");
								result.Args.Add(ob);
							}
							break;
						}
					case ExpressionType.Call:
						#region
						{
							var mc = (MethodCallExpression)expr;
							var method_name = mc.Method.Name.ToLowerInvariant();
							switch (method_name)
							{
							default: throw new NotSupportedException();
							case "like":
								{
									if (mc.Arguments.Count != 2) throw new NotSupportedException("The only supported 'like' method has 2 parameters");
									var p0 = Translate(mc.Arguments[0]);
									var p1 = Translate(mc.Arguments[1]);
									result.Text.Append("(").Append(p0.Text).Append(" like ").Append(p1.Text).Append(")");
									result.Args.AddRange(p0.Args);
									result.Args.AddRange(p1.Args);
									break;
								}
							case "contains":
								{
									if (mc.Object != null && mc.Arguments.Count == 1) // obj.Contains() calls...
									{
										var p0 = Translate(mc.Object);
										var p1 = Translate(mc.Arguments[0]);
										result.Text.Append("(").Append(p1.Text).Append(" in ").Append(p0.Text).Append(")");
										result.Args.AddRange(p0.Args);
										result.Args.AddRange(p1.Args);
									}
									else if (mc.Object == null && mc.Arguments.Count == 2) // StaticClass.Contains(thing, in_things);
									{
										var p0 = Translate(mc.Arguments[0]);
										var p1 = Translate(mc.Arguments[1]);
										result.Text.Append("(").Append(p1.Text).Append(" in ").Append(p0.Text).Append(")");
										result.Args.AddRange(p0.Args);
										result.Args.AddRange(p1.Args);
									}
									else
										throw new NotSupportedException("Unsupported 'contains' method");
									break;
								}
							}
							break;
						}
						#endregion
					case ExpressionType.Constant:
						#region
						{
							var ob = ((ConstantExpression)expr).Value;
							if (ob == null) result.Text.Append("null");
							else
							{
								result.Text.Append("?");
								result.Args.Add(ob);
							}
							break;
						}
						#endregion
					case ExpressionType.Convert:
					case ExpressionType.ConvertChecked:
						#region
						checked
						{
							var ue = (UnaryExpression)expr;
							var ty = ue.Type;
							var start = result.Args.Count;
							Translate(ue.Operand, result);
							for (; start != result.Args.Count; ++start)
								result.Args[start] = Convert.ChangeType(result.Args[start], Nullable.GetUnderlyingType(ty) ?? ty, null);
							break;
						}
						#endregion
					case ExpressionType.Not:
					case ExpressionType.UnaryPlus:
					case ExpressionType.Negate:
					#if !MONOTOUCH
					case ExpressionType.OnesComplement:
					#endif
						#region
						{
							switch (expr.NodeType) {
							case ExpressionType.Not:            result.Text.Append(" not "); break;
							case ExpressionType.UnaryPlus:      result.Text.Append('+'); break;
							case ExpressionType.Negate:         result.Text.Append('-'); break;
							#if !MONOTOUCH
							case ExpressionType.OnesComplement: result.Text.Append('~'); break;
							#endif
							}
							result.Text.Append("(");
							Translate(((UnaryExpression)expr).Operand, result);
							result.Text.Append(")");
							break;
						}
						#endregion
					case ExpressionType.Quote:
						#region
						{
							result.Text.Append('"');
							Translate(((UnaryExpression)expr).Operand, result);
							result.Text.Append('"');
							break;
						}
						#endregion
					case ExpressionType.Equal:              result.Text.Append(binary_expr != null && IsNullConstant(binary_expr.Right) ? " is " : "=="); break;
					case ExpressionType.NotEqual:           result.Text.Append(binary_expr != null && IsNullConstant(binary_expr.Right) ? " is not " : "!="); break;
					case ExpressionType.OrElse:             result.Text.Append(" or "); break;
					case ExpressionType.AndAlso:            result.Text.Append(" and "); break;
					case ExpressionType.Multiply:           result.Text.Append( "*"); break;
					case ExpressionType.Divide:             result.Text.Append( "/"); break;
					case ExpressionType.Modulo:             result.Text.Append( "%"); break;
					case ExpressionType.Add:                result.Text.Append( "+"); break;
					case ExpressionType.Subtract:           result.Text.Append( "-"); break;
					case ExpressionType.LeftShift:          result.Text.Append("<<"); break;
					case ExpressionType.RightShift:         result.Text.Append("<<"); break;
					case ExpressionType.And:                result.Text.Append( "&"); break;
					case ExpressionType.Or:                 result.Text.Append( "|"); break;
					case ExpressionType.LessThan:           result.Text.Append( "<"); break;
					case ExpressionType.LessThanOrEqual:    result.Text.Append("<="); break;
					case ExpressionType.GreaterThan:        result.Text.Append( ">"); break;
					case ExpressionType.GreaterThanOrEqual: result.Text.Append(">="); break;
					}

					if (binary_expr != null)
					{
						Translate(binary_expr.Right ,result);
						result.Text.Append(")");
					}

					return result;
				}
			}

			#endregion
		}

		/// <summary>Represents a table within the database for a compile-time type 'T'.</summary>
		public class Table<T> :Table ,IEnumerable<T>
		{
			public Table(Database db)
				:base(typeof(T), db)
			{ }
			public Table(string table_name, Database db)
				:base(table_name, typeof(T), db)
			{ }

			/// <summary>Returns a row in the table or null if not found</summary>
			public new T Find(params object?[] keys)
			{
				return Find<T>(keys);
			}
			public new T Find(object key1, object key2) // overload for performance
			{
				return Find<T>(key1, key2);
			}
			public new T Find(object key1) // overload for performance
			{
				return Find<T>(key1);
			}

			/// <summary>Returns a row in the table (throws if not found)</summary>
			public new T Get(params object?[] keys)
			{
				return Get<T>(keys);
			}
			public new T Get(object key1, object key2) // overload for performance
			{
				return Get<T>(key1, key2);
			}
			public new T Get(object key1) // overload for performance
			{
				return Get<T>(key1);
			}

			/// <summary>Return the first row. Throws if no rows found</summary>
			public T First()
			{
				return First<T>();
			}
			public T First(Expression<Func<T,bool>> pred)
			{
				return First<T>(pred);
			}

			/// <summary>Return the first row or null</summary>
			public T FirstOrDefault()
			{
				return FirstOrDefault<T>();
			}
			public T FirstOrDefault(Expression<Func<T,bool>> pred)
			{
				return FirstOrDefault<T>(pred);
			}

			/// <summary>Add a 'count' clause to row enumeration</summary>
			public int Count(Expression<Func<T,bool>> pred)
			{
				return Count<T>(pred);
			}

			/// <summary>Delete rows from the table</summary>
			public int Delete(Expression<Func<T,bool>> pred)
			{
				return Delete<T>(pred);
			}

			/// <summary>Add a 'select' clause to row enumeration</summary>
			public IEnumerable<U> Select<U>(Expression<Func<T,U>> pred)
			{
				return Select<T,U>(pred);
			}

			/// <summary>Add a 'where' clause to row enumeration</summary>
			public Table<T> Where(Expression<Func<T,bool>> pred)
			{
				return (Table<T>)base.Where(pred);
			}

			/// <summary>Add an 'OrderBy' clause to row enumeration</summary>
			public Table<T> OrderBy<U>(Expression<Func<T,U>> pred)
			{
				return (Table<T>)base.OrderBy(pred);
			}

			/// <summary>Add an 'OrderByDescending' clause to row enumeration</summary>
			public Table<T> OrderByDescending<U>(Expression<Func<T,U>> pred)
			{
				return (Table<T>)base.OrderByDescending(pred);
			}

			/// <summary>Add a 'limit' clause to row enumeration</summary>
			public new Table<T> Take(int n)
			{
				return (Table<T>)base.Take(n);
			}

			/// <summary>Add an 'offset' clause to row enumeration</summary>
			public new Table<T> Skip(int n)
			{
				return (Table<T>)base.Skip(n);
			}

			/// <summary>IEnumerable(T) interface</summary>
			IEnumerator<T> IEnumerable<T>.GetEnumerator()
			{
				GenerateExpression();
				ResetExpression(); // Don't put this in a finally block because it gets called for each yield return
				if (m_cmd.SqlString == null) throw new Exception($"Invalid Sql");
				return DB.EnumRows<T>(m_cmd.SqlString, 1, m_cmd.Arguments).GetEnumerator();
			}
		}

		#endregion

		#region Query

		/// <summary>Represents an sqlite prepared statement and iterative result (wraps an 'sqlite3_stmt' handle).</summary>
		public class Query :IDisposable
		{
			protected readonly sqlite3_stmt m_stmt; // sqlite managed memory for this query
			#if SQLITE_HANDLES
			private int m_query_id {get;set;}
			#endif

			public Query(Database db, sqlite3_stmt stmt)
			{
				#if SQLITE_HANDLES
				m_query_id = ++db.m_query_id;
				db.m_queries.Add(m_query_id);
				#endif

				Debug.Assert(db.AssertCorrectThread());
				if (stmt.IsInvalid)
					throw new ArgumentNullException("stmt", "Invalid sqlite prepared statement handle");

				DB = db;
				m_stmt = stmt;
			}
			public Query(Database db, string sql_string, int first_idx = 1, IEnumerable<object?>? parms = null)
				:this(db, Compile(db, sql_string))
			{
				if (parms != null)
					BindParms(first_idx, parms);
			}
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}
			protected virtual void Dispose(bool _)
			{
				Close();
			}

			/// <summary>The associated database connection</summary>
			public Database DB { get; private set; }

			/// <summary>The string used to construct this query statement</summary>
			public string? SqlString => NativeDll.SqlString(m_stmt);

			/// <summary>Returns the 'm_stmt' field after asserting it's validity</summary>
			public sqlite3_stmt Stmt
			{
				get { Debug.Assert(!m_stmt.IsInvalid, "Invalid query object"); return m_stmt; }
			}

			/// <summary>Call when done with this query</summary>
			public void Close()
			{
				if (m_stmt.IsClosed)
					return;

				// Call the event to see if we should cancel closing the query
				var cancel = new QueryClosingEventArgs();
				Closing?.Invoke(this, cancel);
				if (cancel.Cancel) return;

				#if SQLITE_HANDLES
				DB.m_queries.Remove(m_query_id);
				#endif

				// After 'sqlite3_finalize()', it is illegal to use 'm_stmt'. So save 'db' here first
				Reset(); // Call reset to clear any error code from failed queries.
				m_stmt.Close();
				if (m_stmt.CloseResult != Result.OK)
					throw new SqliteException(m_stmt.CloseResult, NativeDll.ErrorMsg(DB.Handle));
			}

			/// <summary>An event raised when this query is closing</summary>
			public event EventHandler<QueryClosingEventArgs>? Closing;

			/// <summary>Return the number of parameters in this statement</summary>
			public int ParmCount
			{
				get { return NativeDll.BindParameterCount(Stmt); }
			}

			/// <summary>Return the index for the parameter named 'name'</summary>
			public int ParmIndex(string name)
			{
				int idx = NativeDll.BindParameterIndex(Stmt, name);
				if (idx == 0) throw new SqliteException(Result.Error, "Parameter name not found");
				return idx;
			}

			/// <summary>Return the name of a parameter by index</summary>
			public string? ParmName(int idx)
			{
				return NativeDll.BindParameterName(Stmt, idx);
			}

			/// <summary>Bind a value to a specific parameter</summary>
			public void BindParm<T>(int idx, T value)
			{
				var bind = Bind.FuncFor(typeof(T));
				bind(m_stmt, idx, value!);
			}

			/// <summary>
			/// Bind an array of parameters starting at parameter index 'first_idx' (remember parameter indices start at 1)
			/// To reuse the Query, call Reset(), then bind new keys, then call Step() again</summary>
			public void BindParms(int first_idx, IEnumerable<object?> parms)
			{
				foreach (var p in parms)
				{
					if (p == null)
						Bind.Null(m_stmt, first_idx++);
					else
						Bind.FuncFor(p.GetType())(m_stmt, first_idx++, p);
				}
			}

			/// <summary>
			/// Bind an array of parameters starting at parameter index 'first_idx' (remember parameter indices start at 1)
			/// To reuse the Query, call Reset(), then bind new keys, then call Step() again</summary>
			public void BindParms(int first_idx, object parm1, object parm2) // overload for performance
			{
				Bind.FuncFor(parm1.GetType())(m_stmt, first_idx+0, parm1);
				Bind.FuncFor(parm1.GetType())(m_stmt, first_idx+1, parm2);
			}

			/// <summary>
			/// Bind an array of parameters starting at parameter index 'first_idx' (remember parameter indices start at 1)
			/// To reuse the Query, call Reset(), then bind new keys, then call Step() again</summary>
			public void BindParms(int first_idx, object parm1) // overload for performance
			{
				Bind.FuncFor(parm1.GetType())(m_stmt, first_idx, parm1);
			}

			/// <summary>
			/// Bind primary keys to this query starting at parameter index
			/// 'first_idx' (remember parameter indices start at 1)
			/// This is functionally the same as BindParms but it validates
			/// the types of the primary keys against the table meta data.</summary>
			public void BindPks(Type type, int first_idx, params object?[] keys)
			{
				var meta = TableMetaData.GetMetaData(type);
				meta.BindPks(m_stmt, first_idx, keys);
			}

			/// <summary>
			/// Bind primary keys to this query starting at parameter index
			/// 'first_idx' (remember parameter indices start at 1)
			/// This is functionally the same as BindParms but it validates
			/// the types of the primary keys against the table meta data.</summary>
			public void BindPks(Type type, int first_idx, object key1, object key2) // overload for performance
			{
				var meta = TableMetaData.GetMetaData(type);
				meta.BindPks(m_stmt, first_idx, key1, key2);
			}

			/// <summary>
			/// Bind primary keys to this query starting at parameter index
			/// 'first_idx' (remember parameter indices start at 1)
			/// This is functionally the same as BindParms but it validates
			/// the types of the primary keys against the table meta data.</summary>
			public void BindPks(Type type, int first_idx, object key1) // overload for performance
			{
				var meta = TableMetaData.GetMetaData(type);
				meta.BindPks(m_stmt, first_idx, key1);
			}

			/// <summary>Reset the prepared statement object back to its initial state, ready to be re-executed.</summary>
			public void Reset()
			{
				NativeDll.Reset(Stmt);
			}

			/// <summary>Iterate to the next row in the result. Returns true if there are more rows available</summary>
			public bool Step()
			{
				Debug.Assert(DB.AssertCorrectThread());
				for (;;)
				{
					var res = NativeDll.Step(Stmt);
					switch (res)
					{
					default: throw new SqliteException(res, NativeDll.ErrorMsg(DB.Handle));
					case Result.Busy: Thread.Yield(); break;
					case Result.Done: return false;
					case Result.Row: return true;
					}
				}
			}

			/// <summary>Run the query until Step returns false. Returns the number of rows changed. Call 'Reset()' before running the command again</summary>
			public virtual int Run()
			{
				int rows_changed = 0;
				while (Step()) rows_changed += RowsChanged;
				return rows_changed + RowsChanged;
			}

			/// <summary>Returns the number of rows changed as a result of the last 'step()'</summary>
			public int RowsChanged
			{
				get { return NativeDll.Changes(DB.Handle); }
			}

			/// <summary>Returns the number of columns in the result of this query</summary>
			public int ColumnCount
			{
				get { return NativeDll.ColumnCount(Stmt); }
			}

			/// <summary>Return the sql data type for a specific column</summary>
			public DataType ColumnType(int idx)
			{
				return NativeDll.ColumnType(m_stmt, idx);
			}

			/// <summary>Returns the name of the column at position 'idx'</summary>
			public string ColumnName(int idx)
			{
				return NativeDll.ColumnName(Stmt, idx);
			}

			/// <summary>Enumerates the columns in the result</summary>
			public IEnumerable<string> ColumnNames
			{
				get
				{
					for (int i = 0, iend = ColumnCount; i != iend; ++i)
						yield return ColumnName(i);
				}
			}

			/// <summary>Read the value of a particular column</summary>
			public T ReadColumn<T>(int idx)
			{
				return (T)Read.FuncFor(typeof(T))(m_stmt, idx);
			}

			/// <summary>Enumerate over the result rows of this query, interpreting each row as type 'T'</summary>
			public IEnumerable<T> Rows<T>()
			{
				return Rows(typeof(T)).Cast<T>();
			}
			public IEnumerable Rows(Type type)
			{
				Debug.Assert(DB.AssertCorrectThread());

				var meta = TableMetaData.GetMetaData(type);
				while (Step())
				{
					var obj = meta.ReadObj(Stmt);
					if (obj != null) obj = DB.ReadItemHook(obj);
					yield return obj;
				}
			}

			/// <summary></summary>
			public override string ToString() => SqlString ?? string.Empty;
		}

		#endregion

		#region Transaction

		/// <summary>An RAII class for transactional interaction with a database</summary>
		public sealed class Transaction :IDisposable
		{

			/// <summary>Typically created using the Database.NewTransaction() method</summary>
			public Transaction(Database db, Action on_dispose)
			{
				Debug.Assert(db.AssertCorrectThread());

				DB = db;
				m_disposed = on_dispose;
				m_completed = false;

				// Begin the transaction
				DB.Execute("begin transaction");
			}
			public void Dispose()
			{
				try
				{
					if (m_completed) return;
					Rollback();
				}
				finally
				{
					m_disposed();
				}
			}
			private readonly Action m_disposed;
			private bool m_completed;

			/// <summary>The database connection</summary>
			private Database DB { get; set; }

			/// <summary>Commit changes to the database</summary>
			public void Commit()
			{
				if (m_completed)
					throw new SqliteException("Transaction already completed");

				DB.Execute("commit");
				m_completed = true;
			}

			/// <summary>Abort changes</summary>
			public void Rollback()
			{
				if (m_completed)
					throw new SqliteException("Transaction already completed");

				DB.Execute("rollback");
				m_completed = true;
			}
		}

		#endregion

		#region Attribute types

		/// <summary>
		/// Controls the mapping from .NET types to database tables.
		/// By default, all public properties (including inherited properties) are used as columns,
		/// this can be changed using the PropertyBindingFlags/FieldBindingFlags properties.</summary>
		[AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
		public class TableAttribute :Attribute
		{
			public TableAttribute()
			{
				AllByDefault         = true;
				PropertyBindingFlags = BindingFlags.Public;
				FieldBindingFlags    = BindingFlags.Default;
				Constraints          = null;
				PrimaryKey           = null;
				PKAutoInc            = false;
			}

			/// <summary>
			/// If true, all properties/fields (selected by the given binding flags) are used as
			/// columns in the created table unless marked with the Sqlite.IgnoreAttribute.<para/>
			/// If false, only properties/fields marked with the Sqlite.ColumnAttribute will be included.<para/>
			/// Default value is true.</summary>
			public bool AllByDefault { get; set; }

			/// <summary>
			/// Binding flags used to reflect on properties in the type.<para/>
			/// Only used if 'AllByDefault' is true. BindingFlag.Instance is added automatically.
			/// Use BindingFlags.Default for none.<para/>
			/// Default value is BindingFlags.Public</summary>
			public BindingFlags PropertyBindingFlags { get; set; }

			/// <summary>
			/// Binding flags used to reflect on fields in the type.<para/>
			/// Only used if 'AllByDefault' is true. BindingFlag.Instance is added automatically.
			/// Use BindingFlags.Default for none.<para/>
			/// Default value is BindingFlags.Default</summary>
			public BindingFlags FieldBindingFlags { get; set; }

			/// <summary>
			/// Defines any table constraints to use when creating the table.<para/>
			/// This can be used to specify multiple primary keys for the table.<para/>
			/// Primary keys are ordered as given in the constraint, starting with Order = 0.<para/>
			/// e.g.<para/>
			///  Constraints = "unique (C1), primary key (C2, C3)"<para/>
			///  Column 'C1' is unique, columns C2 and C3 are the primary keys (in that order)<para/>
			/// Default value is null.</summary>
			public string? Constraints { get; set; }

			/// <summary>
			/// The name of the property or field to use as the primary key for a table.
			/// This property can be used to specify the primary key at a class level which
			/// is helpful if the class is part of an inheritance hierarchy or is a partial
			/// class. This property is used in addition to primary keys specified by
			/// property/field attributes or table constraints. If given, the column Order
			/// value will be set to 0 for that column.<para/>
			/// Default is value is null.</summary>
			public string? PrimaryKey { get; set; }

			/// <summary>
			/// Set to true if the column given by 'PrimaryKey' is also an auto increment
			/// column. Not used if PrimaryKey is not specified. Default is false.</summary>
			public bool PKAutoInc { get; set; }
		}

		/// <summary>
		/// Allows a class or struct to list members that do not create table columns.
		/// This attribute can be inherited from partial or base classes, the resulting
		/// ignored members are the union of all ignore column attributes for a type</summary>
		[AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
		public class IgnoreColumnsAttribute :Attribute
		{
			public IgnoreColumnsAttribute(string column_names)
			{
				Ignore = column_names;
			}

			/// <summary>A comma separated list of properties/fields to ignore</summary>
			public string? Ignore { get; private set; }
		}

		/// <summary>Marks a property or field as a column in a table</summary>
		[AttributeUsage(AttributeTargets.Property|AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
		public class ColumnAttribute :Attribute
		{
			public ColumnAttribute()
			{
				PrimaryKey  = false;
				AutoInc     = false;
				Name        = string.Empty;
				Order       = 0;
				SqlDataType = DataType.Null;
				Constraints = null;
			}

			/// <summary>
			/// True if this column should be used as a primary key.
			/// If multiple primary keys are specified, ensure the Order
			/// property is used so that the order of primary keys is defined.
			/// Default value is false.</summary>
			public bool PrimaryKey { get; set; }

			/// <summary>True if this column should auto increment. Default is false</summary>
			public bool AutoInc { get; set; }

			/// <summary>The column name to use. If null or empty, the member name is used</summary>
			public string? Name { get; set; }

			/// <summary>Defines the relative order of columns in the table. Default is '0'</summary>
			public int Order { get; set; }

			/// <summary>
			/// The sqlite data type used to represent this column.
			/// If set to DataType.Null, then the default mapping from .NET data type to sqlite type is used.
			/// Default value is DataType.Null</summary>
			public DataType SqlDataType { get; set; }

			/// <summary>Custom constraints to add to this column. Default is null</summary>
			public string? Constraints { get; set; }
		}

		/// <summary>Marks a property or field as not a column in the db table for a type.</summary>
		[AttributeUsage(AttributeTargets.Property|AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
		public sealed class IgnoreAttribute :Attribute
		{}

		#endregion

		#region Table Meta Data

		/// <summary>Mapping information from a type to columns in the table</summary>
		public class TableMetaData
		{
			/// <summary>Get the meta data for a table based on 'type'</summary>
			public static TableMetaData GetMetaData<T>()
			{
				return GetMetaData(typeof(T));
			}
			public static TableMetaData GetMetaData(Type type)
			{
				if (type == typeof(object))
					throw new ArgumentException("Type 'object' cannot have TableMetaData", "type");

				if (!Meta.TryGetValue(type, out var meta))
					Meta.Add(type, meta = new TableMetaData(type));

				return meta;
			}
			private static readonly Dictionary<Type, TableMetaData> Meta = new Dictionary<Type, TableMetaData>();

			/// <summary>
			/// Constructs the meta data for mapping a type to a database table.
			/// By default, 'Activator.CreateInstance' is used as the factory function.
			/// This is slow however so use a static delegate if possible</summary>
			public TableMetaData(Type type)
			{
				var column_name_trim = new[]{' ','\t','\'','\"','[',']'};

				// Get the table attribute
				var attrs = type.GetCustomAttributes(typeof(TableAttribute), true);
				var attr = attrs.Length != 0 ? (TableAttribute)attrs[0] : new TableAttribute();

				Type = type;
				Name = type.Name;
				Constraints = attr.Constraints ?? "";
				Factory = () => Type.New();
				TableKind = Kind.Unknown;

				// Build a collection of columns to ignore
				var ignored = new List<string>();
				foreach (var ign in type.GetCustomAttributes(typeof(IgnoreColumnsAttribute), true).Cast<IgnoreColumnsAttribute>())
				{
					if (ign.Ignore == null) continue;
					ignored.AddRange(ign.Ignore.Split(',').Select(x => x.Trim(column_name_trim)));
				}

				// Tests if a member should be included as a column in the table
				bool IncludeMember(MemberInfo mi, List<string> marked) =>
					!mi.GetCustomAttributes(typeof(IgnoreAttribute), false).Any() &&  // doesn't have the ignore attribute and,
					(ignored != null && !ignored.Contains(mi.Name)) &&                // isn't in the ignore list and,
					(mi.GetCustomAttributes(typeof(ColumnAttribute), false).Any() ||  // has the column attribute or,
					(attr != null && attr.AllByDefault && marked.Contains(mi.Name))); // all in by default and 'mi' is in the collection of found columns

				const BindingFlags binding_flags = BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
				var pflags = attr.PropertyBindingFlags != BindingFlags.Default ? attr.PropertyBindingFlags |BindingFlags.Instance : BindingFlags.Default;
				var fflags = attr.FieldBindingFlags    != BindingFlags.Default ? attr.FieldBindingFlags    |BindingFlags.Instance : BindingFlags.Default;

				// Create a collection of the columns of this table
				var cols = new List<ColumnMetaData>();
				{
					// If AllByDefault is enabled, get a collection of the properties/fields indicated by the binding flags in the attribute
					var mark = !attr.AllByDefault ? new List<string>() :
						type.GetProperties(pflags).Where(x => x.CanRead && x.CanWrite).Select(x => x.Name).Concat(
						type.GetFields    (fflags).Select(x => x.Name)).ToList();

					// Check all public/non-public properties/fields
					cols.AddRange(AllProps (type, binding_flags).Where(pi => IncludeMember(pi,mark)).Select(pi => new ColumnMetaData(pi)));
					cols.AddRange(AllFields(type, binding_flags).Where(fi => IncludeMember(fi,mark)).Select(fi => new ColumnMetaData(fi)));

					// If we found read/write properties or fields then this is a normal db table type
					if (cols.Count != 0)
						TableKind = Kind.Table;
				}

				// If no read/write columns were found, look for readonly columns
				if (TableKind == Kind.Unknown)
				{
					// If AllByDefault is enabled, get a collection of the properties/fields indicated by the binding flags in the attribute
					var mark = !attr.AllByDefault ? new List<string>() :
						type.GetProperties(pflags).Where(x => x.CanRead).Select(x => x.Name).Concat(
						type.GetFields    (fflags).Select(x => x.Name)).ToList();

					// If we find public readonly properties or fields
					cols.AddRange(AllProps(type, binding_flags).Where(pi => IncludeMember(pi,mark)).Select(x => new ColumnMetaData(x)));
					cols.AddRange(AllProps(type, binding_flags).Where(fi => IncludeMember(fi,mark)).Select(x => new ColumnMetaData(x)));
					if (cols.Count != 0)
						TableKind = Kind.AnonType;
				}

				// If still not columns found, check whether 'type' is a primitive type
				if (TableKind == Kind.Unknown)
				{
					cols.Add(new ColumnMetaData(type));
					TableKind = Kind.PrimitiveType;
				}

				// If a primary key is named in the class level attribute, mark it
				if (attr.PrimaryKey != null)
				{
					var col = cols.FirstOrDefault(x => x.Name == attr.PrimaryKey);
					if (col == null) throw new ArgumentException($"Named primary key column '{attr.PrimaryKey}' (given by Sqlite.TableAttribute) is not a found table column for type '{Name} '");
					col.IsPk = true;
					col.IsAutoInc = attr.PKAutoInc;
					col.Order = 0;
				}

				// Check the table constraints for primary key definitions
				const string primary_key = "primary key";
				var pk_ofs = Constraints.IndexOf(primary_key, StringComparison.OrdinalIgnoreCase);
				if (pk_ofs != -1)
				{
					var s = Constraints.IndexOf('(', pk_ofs + primary_key.Length);
					var e = Constraints.IndexOf(')', s + 1);
					if (s == -1 || e == -1) throw new ArgumentException($"Table constraints '{Constraints}' are invalid");

					// Check that every named primary key is actually a column
					// and also ensure primary keys are ordered as given.
					int order = 0;
					foreach (var pk in Constraints.Substring(s+1, e-s-1).Split(',').Select(x => x.Trim(column_name_trim)))
					{
						var col = cols.FirstOrDefault(x => x.Name == pk);
						if (col == null) throw new ArgumentException($"Named primary key column '{pk}' was not found as a table column for type '{Name}'");
						col.IsPk = true;
						col.Order = order++;
					}
				}

				// Sort the columns by the given order
				var cmp = Comparer<int>.Default;
				cols.Sort((lhs,rhs) => cmp.Compare(lhs.Order, rhs.Order));

				// Create the column arrays
				Columns     = cols.ToArray();
				Pks         = Columns.Where(x => x.IsPk).ToArray();
				NonPks      = Columns.Where(x => !x.IsPk).ToArray();
				NonAutoIncs = Columns.Where(x => !x.IsAutoInc).ToArray();
				m_single_pk = Pks.Count() == 1 ? Pks.First() : null;

				// Initialise the generated methods for this type
				#if COMPILED_LAMBDAS
				m_method_equal = typeof(MethodGenerator<>).MakeGenericType(Type).GetMethod("Equal", BindingFlags.Static|BindingFlags.Public) ?? throw new Exception("MethodGenerator<> failed");
				m_method_clone = typeof(MethodGenerator<>).MakeGenericType(Type).GetMethod("Clone", BindingFlags.Static|BindingFlags.Public) ?? throw new Exception("MethodGenerator<> failed");
				#endif
			}

			/// <summary>The .NET type that this meta data is for</summary>
			public Type Type { get; private set; }

			/// <summary>The table name (defaults to the type name)</summary>
			public string Name { get; set; }
			public string NameQuoted { get { return "'"+Name+"'"; } }

			/// <summary>Table constraints for this table (default is none)</summary>
			public string Constraints { get; set; }

			/// <summary>Enumerate the column meta data</summary>
			public ColumnMetaData[] Columns { get; private set; }

			/// <summary>The primary key columns, in order</summary>
			public ColumnMetaData[] Pks { get; private set; }

			/// <summary>The non primary key columns</summary>
			public ColumnMetaData[] NonPks { get; private set; }

			/// <summary>The columns that aren't auto increment columns</summary>
			public ColumnMetaData[] NonAutoIncs { get; private set; }

			/// <summary>Gets the number of columns in this table</summary>
			public int ColumnCount => Columns.Length;

			// 'm_single_pk' is a pointer to the single primary key column for
			// this table or null if the table has multiple primary keys.
			/// <summary>Returns true of there is only one primary key for this type and it is an integer (i.e. an alias for the row id)</summary>
			public bool SingleIntegralPK => m_single_pk?.SqlDataType == DataType.Integer;
			private readonly ColumnMetaData? m_single_pk;

			/// <summary>True if the table uses multiple primary keys</summary>
			public bool MultiplePK => Pks.Length > 1;

			/// <summary>The kind of table represented by this meta data</summary>
			public Kind TableKind { get; private set; }
			public enum Kind
			{
				Unknown,  // Unknown
				Table,    // A normal sql table
				AnonType, // An anonymous class
				PrimitiveType // A primitive type
			}

			/// <summary>A factory method for creating instances of the type for this table</summary>
			public Func<object> Factory { get; set; }

			/// <summary>Return the column meta data for a column by name</summary>
			public ColumnMetaData Column(int column_index)
			{
				return Columns[column_index];
			}

			/// <summary>Return the column meta data for a column by name</summary>
			public ColumnMetaData? Column(string column_name)
			{
				foreach (var c in Columns)
					if (string.CompareOrdinal(c.Name, column_name) == 0)
						return c;

				return null;
			}

			/// <summary>
			/// Bind the primary keys 'keys' to the parameters in a
			/// prepared statement starting at parameter 'first_idx'.</summary>
			public void BindPks(sqlite3_stmt stmt, int first_idx, params object?[] keys)
			{
				if (first_idx < 1) throw new ArgumentException("Parameter binding indices start at 1 so 'first_idx' must be >= 1");
				if (Pks.Length != keys.Length) throw new ArgumentException("Incorrect number of primary keys passed for type "+Name);
				if (Pks.Length == 0) throw new ArgumentException("Attempting to bind primary keys for a type without primary keys");

				int idx = 0;
				foreach (var c in Pks)
				{
					var key = keys[idx];
					if (key == null) throw new ArgumentException($"Primary key value {idx + 1} should be of type {c.ClrType.Name} not null");
					if (key.GetType() != c.ClrType) throw new ArgumentException($"Primary key {idx + 1} should be of type {c.ClrType.Name} not type {key.GetType().Name}");
					c.BindFn(stmt, first_idx+idx, key); // binding parameters are indexed from 1 (hence the +1)
					++idx;
				}
			}

			/// <summary>
			/// Bind the primary keys 'keys' to the parameters in a
			/// prepared statement starting at parameter 'first_idx'.</summary>
			public void BindPks(sqlite3_stmt stmt, int first_idx, object key1, object key2) // overload for performance
			{
				if (first_idx < 1) throw new ArgumentException("Parameter binding indices start at 1 so 'first_idx' must be >= 1");
				if (Pks.Length != 2) throw new ArgumentException("Insufficient primary keys provided for this table type");
				if (key1.GetType() != Pks[0].ClrType) throw new ArgumentException("Primary key "+1+" should be of type "+Pks[0].ClrType.Name+" not type "+key1.GetType().Name);
				if (key2.GetType() != Pks[1].ClrType) throw new ArgumentException("Primary key "+2+" should be of type "+Pks[1].ClrType.Name+" not type "+key2.GetType().Name);
				Pks[0].BindFn(stmt, first_idx+0, key1);
				Pks[1].BindFn(stmt, first_idx+1, key2);
			}

			/// <summary>
			/// Bind the primary keys 'keys' to the parameters in a
			/// prepared statement starting at parameter 'first_idx'.</summary>
			public void BindPks(sqlite3_stmt stmt, int first_idx, object key1) // overload for performance
			{
				if (first_idx < 1) throw new ArgumentException("parameter binding indices start at 1 so 'first_idx' must be >= 1");
				if (Pks.Length != 1) throw new ArgumentException("Insufficient primary keys provided for this table type");
				if (key1.GetType() != Pks[0].ClrType) throw new ArgumentException("Primary key "+1+" should be of type "+Pks[0].ClrType.Name+" not type "+key1.GetType().Name);
				Pks[0].BindFn(stmt, first_idx, key1);
			}

			/// <summary>
			/// Bind the values of the properties/fields in 'item' to the parameters
			/// in prepared statement 'stmt'. 'ofs' is the index offset of the first
			/// parameter to start binding from.</summary>
			public void BindObj(sqlite3_stmt stmt, int first_idx, object item, IEnumerable<ColumnMetaData> columns)
			{
				if (first_idx < 1) throw new ArgumentException("parameter binding indices start at 1 so 'first_idx' must be >= 1");
				if (item.GetType() != Type) throw new ArgumentException("'item' is not the correct type for this table");

				int idx = 0; // binding parameters are indexed from 1
				foreach (var c in columns)
				{
					c.BindFn(stmt, first_idx+idx, c.Get(item));
					++idx;
				}
			}

			/// <summary>Populate the properties and fields of 'item' from the column values read from 'stmt'</summary>
			public object? ReadObj(sqlite3_stmt stmt)
			{
				var column_count = NativeDll.ColumnCount(stmt);
				if (column_count == 0)
					return null;

				object? obj;
				if (TableKind == Kind.Table)
				{
					obj = Factory();
					for (int i = 0; i != column_count; ++i)
					{
						var cname = NativeDll.ColumnName(stmt, i);
						var col = Column(cname);

						// Since sqlite does not support dropping columns in a table, it's likely,
						// for backwards compatibility, that a table will contain columns that don't
						// correspond to a property or field in 'item'. These columns are silently ignored.
						if (col == null) continue;

						col.Set(obj, col.ReadFn(stmt, i));
					}
				}
				else if (TableKind == Kind.AnonType)
				{
					var args = new List<object>();
					for (int i = 0; i != column_count; ++i)
					{
						var cname = NativeDll.ColumnName(stmt, i);
						var col = Column(cname);
						if (col == null) continue;

						args.Add(col.ReadFn(stmt,i));
					}
					obj = Activator.CreateInstance(Type, args.ToArray(), null);
				}
				else if (TableKind == Kind.PrimitiveType)
				{
					var col = Column(0);
					obj = col.ReadFn(stmt,0);
				}
				else
				{
					obj = null;
				}
				return obj;
			}

			///// <summary>Returns a shallow copy of 'item' as a new instance</summary>
			//public object? Clone(object? item)
			//{
			//	Debug.Assert(item.GetType() == Type, "'item' is not the correct type for this table");
			//	#if COMPILED_LAMBDAS
			//	return m_method_clone.Invoke(null, new[]{item});
			//	#else
			//	return MethodGenerator.Clone(item);
			//	#endif
			//}
			//public T Clone<T>(T item)
			//{
			//	return (T)Clone((object?)item)!;
			//}

			/// <summary>Returns true of 'lhs' and 'rhs' are equal instances of this table type</summary>
			public bool Equal(object lhs, object rhs)
			{
				Debug.Assert(lhs.GetType() == Type, "'lhs' is not the correct type for this table");
				Debug.Assert(rhs.GetType() == Type, "'rhs' is not the correct type for this table");

				#if COMPILED_LAMBDAS
				return (bool)m_method_equal.Invoke(null, new[]{lhs, rhs})!;
				#else
				return MethodGenerator.Equal(lhs,rhs);
				#endif
			}

			#if COMPILED_LAMBDAS

			/// <summary>Compiled lambda method for testing two 'Type' instances as equal</summary>
			private readonly MethodInfo m_method_equal;

			/// <summary>Compiled lambda method for returning a shallow copy of an object</summary>
			private readonly MethodInfo m_method_clone;

			/// <summary>Helper class for generating compiled lambda expressions</summary>
			private static class MethodGenerator<T>
			{
				/// <summary>Test two instances of 'T' for having equal fields</summary>
				public static bool Equal(object lhs, object rhs) { return m_func_equal((T)lhs, (T)rhs); }
				private static readonly Func<T,T,bool> m_func_equal = EqualFunc();
				private static Func<T,T,bool> EqualFunc()
				{
					var lhs = Expression.Parameter(typeof(T), "lhs");
					var rhs = Expression.Parameter(typeof(T), "rhs");
					Expression body = Expression.Constant(true);
					foreach (var f in AllFields(typeof(T), BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public))
						body = Expression.AndAlso(Expression.Equal(Expression.Field(lhs, f), Expression.Field(rhs, f)), body);
					return Expression.Lambda<Func<T,T,bool>>(body, lhs, rhs).Compile();
				}

				/// <summary>Returns a shallow copy of 'obj' as a new instance</summary>
				public static object Clone(object obj) { return m_func_clone((T)obj)!; }
				private static readonly Func<T,T> m_func_clone = CloneFunc();
				private static Func<T,T> CloneFunc()
				{
					var p = Expression.Parameter(typeof(T), "obj");
					var bindings = AllFields(typeof(T), BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)
						.Select(x => (MemberBinding)Expression.Bind(x, Expression.Field(p,x)));
					Expression body = Expression.MemberInit(Expression.New(typeof(T)), bindings);
					return Expression.Lambda<Func<T,T>>(body, p).Compile();
				}
			}

			#else

			/// <summary>Helper class for housing the runtime equivalents of the methods in MethodGenerator(T)</summary>
			private static class MethodGenerator
			{
				public static bool Equal(object lhs, object rhs)
				{
					if (lhs.GetType() != rhs.GetType()) return false;
					foreach (var f in AllFields(lhs.GetType(), BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public))
						if (!Equals(f.GetValue(lhs), f.GetValue(rhs)))
							return false;
					return true;
				}
				public static object Clone(object obj)
				{
					object clone = Activator.CreateInstance(obj.GetType());
					foreach (var f in AllFields(obj.GetType(), BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public))
						f.SetValue(clone, f.GetValue(obj));
					return clone;
				}
			}

			#endif

			/// <summary>Returns a table declaration string for this table</summary>
			public string Decl()
			{
				var sb = new StringBuilder();
				sb.Append(string.Join(",\n", Columns.Select(c => c.ColumnDef(m_single_pk != null))));
				if (!string.IsNullOrEmpty(Constraints))
				{
					if (sb.Length != 0) sb.Append(",\n");
					sb.Append(Constraints);
				}
				return sb.ToString();
			}

			/// <summary>
			/// Returns the constraint string for the primary keys of this table.<para/>
			/// i.e. select * from Table where {Index = ? and Key2 = ?}-this bit</summary>
			public string PkConstraints()
			{
				return m_single_pk != null
					? m_single_pk.NameBracketed + " = ?"
					: string.Join(" and ", Pks.Select(x => x.NameBracketed + " = ?"));
			}

			/// <summary>Updates the value of the auto increment primary key (if there is one)</summary>
			public void SetAutoIncPK(object obj, sqlite3 db)
			{
				if (m_single_pk == null || !m_single_pk.IsAutoInc) return;
				int id = (int)NativeDll.LastInsertRowId(db);
				m_single_pk.Set(obj, id);
			}

			/// <summary>Returns all inherited properties for a type</summary>
			private static IEnumerable<PropertyInfo> AllProps(Type type, BindingFlags flags)
			{
				if (type == null || type == typeof(object) || type.BaseType == null) return Enumerable.Empty<PropertyInfo>();
				return AllProps(type.BaseType, flags).Concat(type.GetProperties(flags|BindingFlags.DeclaredOnly));
			}

			/// <summary>Returns all inherited fields for a type</summary>
			private static IEnumerable<FieldInfo> AllFields(Type type, BindingFlags flags)
			{
				if (type == null || type == typeof(object) || type.BaseType == null) return Enumerable.Empty<FieldInfo>();
				return AllFields(type.BaseType, flags).Concat(type.GetFields(flags|BindingFlags.DeclaredOnly));
			}
		}

		#endregion

		#region Column Meta Data

		/// <summary>A column within a db table</summary>
		public class ColumnMetaData
		{
			public const int OrderBaseValue = 0xFFFF;
			
			#nullable disable
			private void Init(MemberInfo mi, Type type)
			{
				ColumnAttribute attr = null;
				if (mi != null) attr = (ColumnAttribute)mi.GetCustomAttributes(typeof(ColumnAttribute), true).FirstOrDefault();
				if (attr == null) attr = new ColumnAttribute();
				var is_nullable = Nullable.GetUnderlyingType(type) != null;

				MemberInfo = mi;
				Name = !string.IsNullOrEmpty(attr.Name) ? attr.Name : (mi != null ? mi.Name : string.Empty);
				SqlDataType = attr.SqlDataType != DataType.Null ? attr.SqlDataType : SqlType(type);
				Constraints = attr.Constraints ?? "";
				IsPk = attr.PrimaryKey;
				IsAutoInc = attr.AutoInc;
				IsNotNull = type.IsValueType && !is_nullable;
				IsCollate = false;
				Order = OrderBaseValue + attr.Order;
				ClrType = type;

				// Set up the bind and read methods
				BindFn = Bind.FuncFor(type);
				ReadFn = Read.FuncFor(type);
			}
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
			public ColumnMetaData(Type type)
			{
				Get = obj => obj;
				Set = (obj,val) => { throw new NotImplementedException(); };
				Init(null, type);
			}
			#nullable enable

			/// <summary>The member info for the member represented by this column</summary>
			public MemberInfo MemberInfo;

			/// <summary>The name of the column</summary>
			public string Name;
			public string NameBracketed => $"[{Name}]";

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
			public Bind.Func BindFn;

			/// <summary>Reads column 'index' from 'stmt' and sets the corresponding property or field in 'obj'</summary>
			public Read.Func ReadFn;

			/// <summary>Returns the column definition for this column</summary>
			public string ColumnDef(bool incl_pk)
			{
				return Sql(NameBracketed," ",SqlDataType.ToString().ToLowerInvariant()," ",incl_pk&&IsPk?"primary key ":"",IsAutoInc?"autoincrement ":"",Constraints);
			}

			/// <summary></summary>
			public override string ToString()
			{
				var sb = new StringBuilder();
				if (IsPk) sb.Append("*");
				sb.Append("[").Append(SqlDataType).Append("]");
				sb.Append(" ").Append(Name);
				if (IsAutoInc) sb.Append("<auto inc>");
				if (IsNotNull) sb.Append("<not null>");
				sb.Append(" ").Append("(").Append(ClrType.Name).Append(")");
				return sb.ToString();
			}
		}

		#endregion

		#region Binding

		/// <summary>Methods for binding data to an sqlite prepared statement</summary>
		public static class Bind
		{
			// Could use Convert.ToXYZ() here, but straight casts are faster

			public static void Null(sqlite3_stmt stmt, int idx)
			{
				NativeDll.BindNull(stmt, idx);
			}
			public static void Bool(sqlite3_stmt stmt, int idx, object value)
			{
				NativeDll.BindInt(stmt, idx, (bool)value ? 1 : 0);
			}
			public static void SByte(sqlite3_stmt stmt, int idx, object value)
			{
				NativeDll.BindInt(stmt, idx, (sbyte)value);
			}
			public static void Byte(sqlite3_stmt stmt, int idx, object value)
			{
				NativeDll.BindInt(stmt, idx, (byte)value);
			}
			public static void Char(sqlite3_stmt stmt, int idx, object value)
			{
				NativeDll.BindInt(stmt, idx, (Char)value);
			}
			public static void Short(sqlite3_stmt stmt, int idx, object value)
			{
				NativeDll.BindInt(stmt, idx, (short)value);
			}
			public static void UShort(sqlite3_stmt stmt, int idx, object value)
			{
				NativeDll.BindInt(stmt, idx, (ushort)value);
			}
			public static void Int(sqlite3_stmt stmt, int idx, object value)
			{
				NativeDll.BindInt(stmt, idx, (int)value);
			}
			public static void UInt(sqlite3_stmt stmt, int idx, object value)
			{
				NativeDll.BindInt64(stmt, idx, (uint)value);
			}
			public static void Long(sqlite3_stmt stmt, int idx, object value)
			{
				NativeDll.BindInt64(stmt, idx, (long)value);
			}
			public static void ULong(sqlite3_stmt stmt, int idx, object value)
			{
				unchecked { NativeDll.BindInt64(stmt, idx, (long)(ulong)value); }
			}
			public static void Decimal(sqlite3_stmt stmt, int idx, object value)
			{
				var dec = (decimal)value;
				NativeDll.BindText(stmt, idx, dec.ToString(CultureInfo.InvariantCulture));
			}
			public static void Float(sqlite3_stmt stmt, int idx, object value)
			{
				NativeDll.BindDouble(stmt, idx, (float)value);
			}
			public static void Double(sqlite3_stmt stmt, int idx, object value)
			{
				NativeDll.BindDouble(stmt, idx, (double)value);
			}
			public static void Text(sqlite3_stmt stmt, int idx, object value)
			{
				if (value == null) Null(stmt, idx);
				else NativeDll.BindText(stmt, idx, (string)value);
			}
			public static void ByteArray(sqlite3_stmt stmt, int idx, object value)
			{
				if (value == null) Null(stmt, idx);
				else NativeDll.BindBlob(stmt, idx, (byte[])value, ((byte[])value).Length);
			}
			public static void IntArray(sqlite3_stmt stmt, int idx, object value)
			{
				if (value == null) Null(stmt, idx);
				else
				{
					var iarr = (int[])value;
					var barr = new byte[Buffer.ByteLength(iarr)];
					Buffer.BlockCopy(iarr, 0, barr, 0, barr.Length);
					NativeDll.BindBlob(stmt, idx, barr, barr.Length);
				}
			}
			public static void LongArray(sqlite3_stmt stmt, int idx, object value)
			{
				if (value == null) Null(stmt, idx);
				else
				{
					var iarr = (long[])value;
					var barr = new byte[Buffer.ByteLength(iarr)];
					Buffer.BlockCopy(iarr, 0, barr, 0, barr.Length);
					NativeDll.BindBlob(stmt, idx, barr, barr.Length);
				}
			}
			public static void Guid(sqlite3_stmt stmt, int idx, object value)
			{
				if (value == null) Null(stmt, idx);
				else Text(stmt, idx, ((Guid)value).ToString());
			}
			public static void DateTimeOffset(sqlite3_stmt stmt, int idx, object value)
			{
				if (value == null) Null(stmt, idx);
				else
				{
					var dto = (DateTimeOffset)value;
					if (dto.Offset != TimeSpan.Zero) throw new SqliteException("Only UTC DateTimeOffset values can be stored");
					Long(stmt, idx, dto.Ticks);
				}
			}
			public static void Color(sqlite3_stmt stmt, int idx, object value)
			{
				if (value == null) Null(stmt, idx);
				else
				{
					var col = (Color)value;
					Int(stmt, idx, col.ToArgb());
				}
			}

			public delegate void Func(sqlite3_stmt stmt, int index, object obj);
			public class Map :Dictionary<Type, Func>
			{
				public Map()
				{
					Add(typeof(bool)           ,Bool          );
					Add(typeof(sbyte)          ,SByte         );
					Add(typeof(byte)           ,Byte          );
					Add(typeof(char)           ,Char          );
					Add(typeof(short)          ,Short         );
					Add(typeof(ushort)         ,UShort        );
					Add(typeof(int)            ,Int           );
					Add(typeof(uint)           ,UInt          );
					Add(typeof(long)           ,Long          );
					Add(typeof(ulong)          ,ULong         );
					Add(typeof(decimal)        ,Decimal       );
					Add(typeof(float)          ,Float         );
					Add(typeof(double)         ,Double        );
					Add(typeof(string)         ,Text          );
					Add(typeof(byte[])         ,ByteArray     );
					Add(typeof(int[])          ,IntArray      );
					Add(typeof(long[])         ,LongArray     );
					Add(typeof(Guid)           ,Guid          );
					Add(typeof(DateTimeOffset) ,DateTimeOffset); // Note: DateTime deliberately not supported, use DateTimeOffset instead
					Add(typeof(Color)          ,Color         );
				}
			}

			/// <summary>
			/// A lookup table from CLR type to binding function.
			/// Users can add custom types and binding functions to this map if needed</summary>
			public static readonly Map FunctionMap = new Map();

			/// <summary>Returns a bind function appropriate for 'type'</summary>
			public static Func FuncFor(Type type)
			{
				try
				{
					if (Nullable.GetUnderlyingType(type) is Type base_type)
					{
						var is_enum = base_type.IsEnum;
						var bind_type = is_enum ? Enum.GetUnderlyingType(base_type) : base_type;
						return (stmt, idx, obj) =>
						{
							if (obj != null) FunctionMap[bind_type](stmt, idx, obj);
							else NativeDll.BindNull(stmt, idx);
						};
					}
					else
					{
						var bind_type = type.IsEnum ? Enum.GetUnderlyingType(type) : type;
						return FunctionMap[bind_type];
					}
				}
				catch (KeyNotFoundException) { }
				throw new KeyNotFoundException(
					$"A bind function was not found for type '{type.Name}'\r\n" +
					"Custom types need to register a Bind/Read function in the " +
					"BindFunction/ReadFunction map before being used");
			}
		}

		#endregion

		#region Reading

		/// <summary>Methods for reading data from an sqlite prepared statement</summary>
		public static class Read
		{
			public static object Bool(sqlite3_stmt stmt, int idx)
			{
				return NativeDll.ColumnInt(stmt, idx) != 0; // Sqlite returns 0 if this column is null
			}
			public static object SByte(sqlite3_stmt stmt, int idx)
			{
				return (sbyte)NativeDll.ColumnInt(stmt, idx); // Sqlite returns 0 if this column is null
			}
			public static object Byte(sqlite3_stmt stmt, int idx)
			{
				return (byte)NativeDll.ColumnInt(stmt, idx); // Sqlite returns 0 if this column is null
			}
			public static object Char(sqlite3_stmt stmt, int idx)
			{
				return (char)NativeDll.ColumnInt(stmt, idx); // Sqlite returns 0 if this column is null
			}
			public static object Short(sqlite3_stmt stmt, int idx)
			{
				return (short)NativeDll.ColumnInt(stmt, idx); // Sqlite returns 0 if this column is null
			}
			public static object UShort(sqlite3_stmt stmt, int idx)
			{
				return (ushort)NativeDll.ColumnInt(stmt, idx); // Sqlite returns 0 if this column is null
			}
			public static object Int(sqlite3_stmt stmt, int idx)
			{
				return NativeDll.ColumnInt(stmt, idx); // Sqlite returns 0 if this column is null
			}
			public static object UInt(sqlite3_stmt stmt, int idx)
			{
				return (uint)NativeDll.ColumnInt64(stmt, idx); // Sqlite returns 0 if this column is null
			}
			public static object Long(sqlite3_stmt stmt, int idx)
			{
				return NativeDll.ColumnInt64(stmt, idx); // Sqlite returns 0 if this column is null
			}
			public static object ULong(sqlite3_stmt stmt, int idx)
			{
				unchecked { return (ulong)NativeDll.ColumnInt64(stmt, idx); } // Sqlite returns 0 if this column is null
			}
			public static object Decimal(sqlite3_stmt stmt, int idx)
			{
				var str = NativeDll.ColumnString(stmt, idx);
				return str.Length != 0 ? decimal.Parse(str) : 0m;
			}
			public static object Float(sqlite3_stmt stmt, int idx)
			{
				return (float)NativeDll.ColumnDouble(stmt, idx); // Sqlite returns 0.0 if this column is null
			}
			public static object Double(sqlite3_stmt stmt, int idx)
			{
				return NativeDll.ColumnDouble(stmt, idx); // Sqlite returns 0.0 if this column is null
			}
			public static object Text(sqlite3_stmt stmt, int idx)
			{
				return NativeDll.ColumnString(stmt, idx);
			}
			public static object ByteArray(sqlite3_stmt stmt, int idx)
			{
				IntPtr ptr; int len;
				NativeDll.ColumnBlob(stmt, idx, out ptr, out len);

				// Copy the blob out of the DB
				byte[] blob = new byte[len];
				if (len != 0) Marshal.Copy(ptr, blob, 0, blob.Length);
				return blob;
			}
			public static object IntArray(sqlite3_stmt stmt, int idx)
			{
				IntPtr ptr; int len;
				NativeDll.ColumnBlob(stmt, idx, out ptr, out len);
				if ((len % sizeof(int)) != 0)
					throw new SqliteException(Result.Corrupt, "Blob data is not an even multiple of Int32s");

				// Copy the blob out of the DB
				int[] blob = new int[len / sizeof(int)];
				if (len != 0) Marshal.Copy(ptr, blob, 0, blob.Length);
				return blob;
			}
			public static object LongArray(sqlite3_stmt stmt, int idx)
			{
				IntPtr ptr; int len;
				NativeDll.ColumnBlob(stmt, idx, out ptr, out len);
				if ((len % sizeof(long)) != 0)
					throw new SqliteException(Result.Corrupt, "Blob data is not an even multiple of Int64s");

				// Copy the blob out of the DB
				long[] blob = new long[len / sizeof(long)];
				if (len != 0) Marshal.Copy(ptr, blob, 0, blob.Length);
				return blob;
			}
			public static object Guid(sqlite3_stmt stmt, int idx)
			{
				string text = (string)Text(stmt, idx);
				return System.Guid.Parse(text);
			}
			public static object DateTimeOffset(sqlite3_stmt stmt, int idx)
			{
				long ticks = (long)Long(stmt, idx);
				return new DateTimeOffset(ticks, TimeSpan.Zero);
			}
			public static object Color(sqlite3_stmt stmt, int idx)
			{
				var argb = (int)Int(stmt, idx);
				return System.Drawing.Color.FromArgb(argb);
			}

			public delegate object Func(sqlite3_stmt stmt, int index);
			public class Map :Dictionary<Type, Func>
			{
				public Map()
				{
					Add(typeof(bool)           ,Bool          );
					Add(typeof(sbyte)          ,SByte         );
					Add(typeof(byte)           ,Byte          );
					Add(typeof(char)           ,Char          );
					Add(typeof(short)          ,Short         );
					Add(typeof(ushort)         ,UShort        );
					Add(typeof(int)            ,Int           );
					Add(typeof(uint)           ,UInt          );
					Add(typeof(long)           ,Long          );
					Add(typeof(ulong)          ,ULong         );
					Add(typeof(decimal)        ,Decimal       );
					Add(typeof(float)          ,Float         );
					Add(typeof(double)         ,Double        );
					Add(typeof(string)         ,Text          );
					Add(typeof(byte[])         ,ByteArray     );
					Add(typeof(int[])          ,IntArray      );
					Add(typeof(long[])         ,LongArray     );
					Add(typeof(Guid)           ,Guid          );
					Add(typeof(DateTimeOffset) ,DateTimeOffset); // Note: DateTime deliberately not supported, use DateTimeOffset instead
					Add(typeof(Color)          ,Color         );
				}
			}

			/// <summary>
			/// A lookup table from ClrType to reading function.
			/// Users can add custom types and reading functions to this map if needed</summary>
			public static readonly Map FunctionMap = new Map();

			/// <summary>Returns a read function appropriate for 'type'</summary>
			public static Func FuncFor(Type type)
			{
				// Set up the bind and read methods
				try
				{
					if (Nullable.GetUnderlyingType(type) is Type base_type)
					{
						var is_enum = base_type.IsEnum;
						var bind_type = is_enum ? Enum.GetUnderlyingType(base_type) : base_type;
						return (stmt, idx) =>
						{
							if (NativeDll.ColumnType(stmt, idx) == DataType.Null) return null!;
							var obj = FunctionMap[bind_type](stmt, idx);
							if (is_enum) obj = Enum.ToObject(base_type, obj);
							return obj;
						};
					}
					else // nullable type, wrap the binding functions
					{
						var bind_type = type.IsEnum ? Enum.GetUnderlyingType(type) : type;
						return type.IsEnum
							? (stmt, idx) => Enum.ToObject(type, FunctionMap[bind_type](stmt, idx))
							: FunctionMap[type];
					}
				}
				catch (KeyNotFoundException) {}
				throw new KeyNotFoundException(
					"A bind or read function was not found for type '"+type.Name+"'\r\n" +
					"Custom types need to register a Bind/Read function in the " +
					"BindFunction/ReadFunction map before being used");
			}
		}

		#endregion

		#region EventArgs

		/// <summary>Event args for the Query.Closing event</summary>
		public class QueryClosingEventArgs :EventArgs
		{
			/// <summary>Set to true to cancel closing the query</summary>
			public bool Cancel
			{
				get { return m_cancel; }
				set { m_cancel = m_cancel || value; }
			}
			private bool m_cancel;
		}

		/// <summary>Event args for the Database DataChanged event</summary>
		public class DataChangedArgs :EventArgs
		{
			[DebuggerStepThrough] public DataChangedArgs(ChangeType change_type, string table_name, long row_id)
			{
				ChangeType   = change_type;
				TableName    = table_name;
				RowId        = row_id;
			}

			/// <summary>How the row was changed. One of Inserted, Updated, or Deleted</summary>
			public ChangeType ChangeType { get; set; }

			/// <summary>The name of the changed table</summary>
			public string TableName { get; set; }

			/// <summary>The 'rowid'/primary key of the effected row</summary>
			public long RowId { get; set; }
		}

		#endregion

		#region Support Types

		/// <summary>Represents the internal type used by sqlite to store information about a table</summary>
		private class TableInfo
		{
			public int     cid        { get; set; }
			public string? name       { get; set; }
			public string? type       { get; set; }
			public int     notnull    { get; set; }
			public string? dflt_value { get; set; }
			public int     pk         { get; set; }
		}

		#endregion

		#region DLL Functions

		/// <summary>Helper method for loading the dll from a platform specific path</summary>
		public static void LoadDll(string dir = @".\lib\$(platform)\$(config)") => NativeDll.LoadDll(dir);

		/// <summary>True if the sqlite dll has been loaded</summary>
		public static bool ModuleLoaded => NativeDll.ModuleLoaded;

		/// <summary>The exception created if the module fails to load</summary>
		public static Exception? LoadError => NativeDll.LoadError;

		/// <summary>Interop functions</summary>
		private static class NativeDll
		{
			private const string Dll = "sqlite3";

			/// <summary>True if the sqlite3.dll has been loaded into memory</summary>
			public static bool ModuleLoaded => m_module != IntPtr.Zero;
			private static IntPtr m_module;

			/// <summary>The exception created if the module fails to load</summary>
			public static Exception? LoadError;

			/// <summary>Helper method for loading the dll from a platform specific path</summary>
			public static void LoadDll(string dir)
			{
				if (ModuleLoaded) return;
				m_module = Win32.LoadDll(Dll+".dll", out LoadError, dir);
			}

			/// <summary>Base class for wrappers of native sqlite handles</summary>
			private abstract class SQLiteHandle :SafeHandle
			{
				/// <summary>Ensure sqlite handles are created and released from the same thread</summary>
				private int m_thread_id;

				#if SQLITE_HANDLES
				private StackTrace m_stack_at_creation;
				private static int g_sqlite_handle_id;
				private int m_sqlite_handle_id;
				#endif

				/// <summary>Default constructor contains an owned, but invalid handle</summary>
				protected SQLiteHandle()
					:this(IntPtr.Zero, true)
				{ }

				/// <summary>Internal constructor used to create safe handles that optionally own the handle</summary>
				protected SQLiteHandle(IntPtr initial_ptr, bool owns_handle)
					:base(IntPtr.Zero, owns_handle)
				{
					m_thread_id = Thread.CurrentThread.ManagedThreadId;

					// Initialise to 'empty' so we know when its been set
					CloseResult = Result.Empty;
					SetHandle(initial_ptr);

					#if SQLITE_HANDLES
					m_stack_at_creation = new StackTrace(true);
					m_sqlite_handle_id = ++g_sqlite_handle_id;
					#endif
				}

				/// <summary>Frees the handle.</summary>
				protected sealed override bool ReleaseHandle()
				{
					if (Thread.CurrentThread.ManagedThreadId != m_thread_id)
					{
						// This typically happens when the garbage collector cleans up the handles.
						// The garbage collector should never see these, so the bug is you've created
						// a query somewhere that hasn't been disposed.
						// Note: the garbage collector calls dispose in no particular order, so it's
						// possible you have a Database instance still live and this exception is being
						// thrown for an instance in the cache of that instance.

						#if SQLITE_HANDLES
						// Output the stack dump of where this handle was allocated
						var msg = string.Format("Sqlite handle ({0}) released from a different thread to the one it was created on\r\n", m_sqlite_handle_id);
						if (m_stack_at_creation != null) msg += "Handle was created here:\r\n" + m_stack_at_creation.ToString();

						// Add the stack dump of where the owning DB handle was created
						// If this is the garbage collector thread, it's possible the owner has been disposed already
						var stmt = this as NativeSqlite3StmtHandle;
						if (stmt != null && stmt.m_db != null)
						{
							try { msg += "\r\nThe DB that the handle is associated with was created:\r\n" + (stmt.m_db.m_stack_at_creation != null ? stmt.m_db.m_stack_at_creation.ToString() : string.Empty); }
							catch (Exception ex) { msg += "Owning DB handle creation stack not available\r\n" + ex.Message; }
						}

						throw new Exception(msg);
						#else
						throw new SqliteException("Sqlite handle released from a different thread to the one it was created on");
						#endif
					}

					CloseResult = Release();
					handle = IntPtr.Zero;
					return true;
				}

				/// <summary>Release the handle</summary>
				protected abstract Result Release();

				/// <summary>The result from the 'sqlite3_close' call</summary>
				public Result CloseResult { get; private set; }

				/// <summary>True if the contained handle is invalid</summary>
				public override bool IsInvalid
				{
					get { return handle == IntPtr.Zero; }
				}
			}

			/// <summary>A wrapper for unmanaged sqlite database connection handles</summary>
			private class NativeSqlite3Handle :SQLiteHandle, sqlite3
			{
				public NativeSqlite3Handle()
				{}
				public NativeSqlite3Handle(IntPtr handle, bool owns_handle)  // Called through reflection during interop
					:base(handle, owns_handle)
				{ }
				protected override Result Release()
				{
					return sqlite3_close(handle);
				}
			}

			/// <summary>A wrapper for unmanaged sqlite prepared statement handles</summary>
			private class NativeSqlite3StmtHandle :SQLiteHandle, sqlite3_stmt
			{
				#if SQLITE_HANDLES
				public NativeSqlite3Handle? m_db; // The owner DB handle
				#endif

				public NativeSqlite3StmtHandle()
				{ }
				public NativeSqlite3StmtHandle(IntPtr handle, bool owns_handle) // Called through reflection during interop
					:base(handle, owns_handle)
				{ }
				protected override Result Release()
				{
					return sqlite3_finalize(handle);
				}
			}

			#region sqlite C API

			/// <summary>Sqlite library version string</summary>
			public static string LibVersion()
			{
				return Marshal.PtrToStringAnsi(sqlite3_libversion()) ?? throw new NullReferenceException("Library version returned null");
			}
			[DllImport(Dll, EntryPoint = "sqlite3_libversion", CallingConvention = CallingConvention.Cdecl)]
			private static extern IntPtr sqlite3_libversion();

			/// <summary>Sqlite library version number</summary>
			public static int LibVersionNumber()
			{
				return sqlite3_libversion_number();
			}
			[DllImport(Dll, EntryPoint = "sqlite3_libversion_number", CallingConvention = CallingConvention.Cdecl)]
			private static extern int sqlite3_libversion_number();

			/// <summary></summary>
			public static string SourceId()
			{
				return Marshal.PtrToStringAnsi(sqlite3_sourceid()) ?? throw new NullReferenceException("SourceId returned null");
			}
			[DllImport(Dll, EntryPoint = "sqlite3_sourceid", CallingConvention = CallingConvention.Cdecl)]
			private static extern IntPtr sqlite3_sourceid();

			/// <summary>True if asynchronous support is enabled (via the same DB connection. If not, use separate DB connections)</summary>
			public static bool ThreadSafe
			{
				get { return sqlite3_threadsafe() != 0 && m_config_option != ConfigOption.SingleThread; }
			}
			[DllImport(Dll, EntryPoint = "sqlite3_libversion_number", CallingConvention = CallingConvention.Cdecl)]
			private static extern int sqlite3_threadsafe();

			/// <summary>Close a database connection</summary>
			[DllImport(Dll, EntryPoint = "sqlite3_close", CallingConvention=CallingConvention.Cdecl)]
			private static extern Result sqlite3_close(IntPtr db);

			/// <summary>Release a prepared sql statement</summary>
			[DllImport(Dll, EntryPoint = "sqlite3_finalize", CallingConvention=CallingConvention.Cdecl)]
			private static extern Result sqlite3_finalize(IntPtr stmt);

			/// <summary></summary>
			[DllImport(Dll, EntryPoint = "sqlite3_free", CallingConvention=CallingConvention.Cdecl)]
			private static extern void sqlite3_free(IntPtr ptr);

			/// <summary></summary>
			[DllImport(Dll, EntryPoint = "sqlite3_db_handle", CallingConvention=CallingConvention.Cdecl)]
			private static extern NativeSqlite3Handle sqlite3_db_handle(NativeSqlite3StmtHandle stmt);

			/// <summary></summary>
			[DllImport(Dll, EntryPoint = "sqlite3_limit", CallingConvention=CallingConvention.Cdecl)]
			private static extern int sqlite3_limit(NativeSqlite3Handle db, Limit limit_category, int new_value);

			/// <summary>Set a configuration setting for the database</summary>
			public static Result Config(ConfigOption option)
			{
				if (sqlite3_threadsafe() == 0 && option != ConfigOption.SingleThread)
					throw new SqliteException(Result.Misuse, "sqlite3 dll compiled with SQLITE_THREADSAFE=0, multi threading cannot be used");

				return sqlite3_config(m_config_option = option);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_config", CallingConvention=CallingConvention.Cdecl)]
			private static extern Result sqlite3_config(ConfigOption option);

			/// <summary>Return the threading mode (config option) that the dll has been set to</summary>
			public static ConfigOption ThreadingMode { get { return m_config_option; } }
			private static ConfigOption m_config_option = ConfigOption.SingleThread;

			/// <summary>Open a database file</summary>
			public static sqlite3 Open(string filepath, OpenFlags flags)
			{
				if ((flags & OpenFlags.FullMutex) != 0 && !ThreadSafe)
					throw new SqliteException(Result.Misuse, "sqlite3 dll compiled with SQLITE_THREADSAFE=0, multi threading cannot be used");

				NativeSqlite3Handle db;
				var res = sqlite3_open_v2(filepath, out db, (int)flags, IntPtr.Zero);
				if (res != Result.OK) throw new SqliteException(res, "Failed to open database connection to file "+filepath);
				return db;
			}
			[DllImport(Dll, EntryPoint = "sqlite3_open_v2", CallingConvention=CallingConvention.Cdecl)]
			private static extern Result sqlite3_open_v2([MarshalAs(UnmanagedType.LPStr)] string filepath, out NativeSqlite3Handle db, int flags, IntPtr zvfs);

			/// <summary>Set the busy wait timeout on the DB</summary>
			public static void BusyTimeout(sqlite3 db, int milliseconds)
			{
				var r = sqlite3_busy_timeout((NativeSqlite3Handle)db, milliseconds);
				if (r != Result.OK) throw new SqliteException(r, "Failed to set the busy timeout to "+milliseconds+"ms");
			}
			[DllImport(Dll, EntryPoint = "sqlite3_busy_timeout", CallingConvention=CallingConvention.Cdecl)]
			private static extern Result sqlite3_busy_timeout(NativeSqlite3Handle db, int milliseconds);

			/// <summary>Creates a prepared statement from an sql string</summary>
			public static sqlite3_stmt Prepare(sqlite3 db, string sql_string)
			{
				NativeSqlite3StmtHandle stmt;
				var buf_utf8 = StrToUTF8(sql_string);
				var res = sqlite3_prepare_v2((NativeSqlite3Handle)db, buf_utf8, buf_utf8.Length, out stmt, IntPtr.Zero);
				if (res != Result.OK)
				{
					var err = ErrorMsg(db);
					var msg = string.Format("Error compiling sql string '{0}'\n{1}" ,sql_string, err);
					throw new SqliteException(res, msg, err);
				}
				#if SQLITE_HANDLES
				stmt.m_db = (NativeSqlite3Handle)db;
				#endif
				return stmt;
			}
			[DllImport(Dll, EntryPoint = "sqlite3_prepare_v2", CallingConvention=CallingConvention.Cdecl)]
			private static extern Result sqlite3_prepare_v2(NativeSqlite3Handle db, byte[] sql, int num_bytes, out NativeSqlite3StmtHandle stmt, IntPtr pzTail);

			/// <summary>Returns the number of rows changed by the last operation</summary>
			public static int Changes(sqlite3 db)
			{
				return sqlite3_changes((NativeSqlite3Handle)db);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_changes", CallingConvention=CallingConvention.Cdecl)]
			private static extern int sqlite3_changes(NativeSqlite3Handle db);

			/// <summary>Returns the RowId for the last inserted row</summary>
			public static long LastInsertRowId(sqlite3 db)
			{
				return sqlite3_last_insert_rowid((NativeSqlite3Handle)db);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_last_insert_rowid", CallingConvention=CallingConvention.Cdecl)]
			private static extern long sqlite3_last_insert_rowid(NativeSqlite3Handle db);

			/// <summary>Returns the error message for the last error returned from sqlite</summary>
			public static string ErrorMsg(sqlite3 db)
			{
				// sqlite3 manages the memory allocated for the returned error message
				// so we don't need to call sqlite_free on the returned pointer
				return Marshal.PtrToStringUni(sqlite3_errmsg16((NativeSqlite3Handle)db)) ?? string.Empty;
			}
			[DllImport(Dll, EntryPoint = "sqlite3_errmsg16", CallingConvention=CallingConvention.Cdecl)]
			private static extern IntPtr sqlite3_errmsg16(NativeSqlite3Handle db);

			/// <summary>Reset a prepared statement</summary>
			public static void Reset(sqlite3_stmt stmt)
			{
				// The result from 'sqlite3_reset' reflects the error code of the last 'sqlite3_step'
				// call. This is legacy behaviour, now step returns the error code immediately. For
				// this reason we can ignore the error code returned by reset.
				sqlite3_reset((NativeSqlite3StmtHandle)stmt);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_reset", CallingConvention=CallingConvention.Cdecl)]
			private static extern Result sqlite3_reset(NativeSqlite3StmtHandle stmt);

			/// <summary>Step a prepared statement</summary>
			public static Result Step(sqlite3_stmt stmt)
			{
				return sqlite3_step((NativeSqlite3StmtHandle)stmt);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_step", CallingConvention=CallingConvention.Cdecl)]
			private static extern Result sqlite3_step(NativeSqlite3StmtHandle stmt);

			/// <summary>Returns the string used to create a prepared statement</summary>
			public static string? SqlString(sqlite3_stmt stmt)
			{
				return UTF8toStr(sqlite3_sql((NativeSqlite3StmtHandle)stmt)); // this assumes sqlite3_prepare_v2 was used to create 'stmt'
			}
			[DllImport(Dll, EntryPoint = "sqlite3_sql", CallingConvention=CallingConvention.Cdecl)]
			private static extern IntPtr sqlite3_sql(NativeSqlite3StmtHandle stmt);

			/// <summary>Returns the name of the column with 0-based index 'index'</summary>
			public static string ColumnName(sqlite3_stmt stmt, int index)
			{
				return Marshal.PtrToStringUni(sqlite3_column_name16((NativeSqlite3StmtHandle)stmt, index)) ?? string.Empty;
			}
			[DllImport(Dll, EntryPoint = "sqlite3_column_name16", CallingConvention=CallingConvention.Cdecl)]
			private static extern IntPtr sqlite3_column_name16(NativeSqlite3StmtHandle stmt, int index);

			/// <summary>Returns the number of columns in the result of a prepared statement</summary>
			public static int ColumnCount(sqlite3_stmt stmt)
			{
				return sqlite3_column_count((NativeSqlite3StmtHandle)stmt);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_column_count", CallingConvention=CallingConvention.Cdecl)]
			private static extern int sqlite3_column_count(NativeSqlite3StmtHandle stmt);

			/// <summary>Returns the internal data type for the column with 0-based index 'index'</summary>
			public static DataType ColumnType(sqlite3_stmt stmt, int index)
			{
				return sqlite3_column_type((NativeSqlite3StmtHandle)stmt, index);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_column_type", CallingConvention=CallingConvention.Cdecl)]
			private static extern DataType sqlite3_column_type(NativeSqlite3StmtHandle stmt, int index);

			/// <summary>Returns the value from the column with 0-based index 'index' as an int</summary>
			public static Int32 ColumnInt(sqlite3_stmt stmt, int index)
			{
				return sqlite3_column_int((NativeSqlite3StmtHandle)stmt, index);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_column_int", CallingConvention=CallingConvention.Cdecl)]
			private static extern Int32 sqlite3_column_int(NativeSqlite3StmtHandle stmt, int index);

			/// <summary>Returns the value from the column with 0-based index 'index' as an int64</summary>
			public static Int64 ColumnInt64(sqlite3_stmt stmt, int index)
			{
				return sqlite3_column_int64((NativeSqlite3StmtHandle)stmt, index);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_column_int64", CallingConvention=CallingConvention.Cdecl)]
			private static extern Int64 sqlite3_column_int64(NativeSqlite3StmtHandle stmt, int index);

			/// <summary>Returns the value from the column with 0-based index 'index' as a double</summary>
			public static Double ColumnDouble(sqlite3_stmt stmt, int index)
			{
				return sqlite3_column_double((NativeSqlite3StmtHandle)stmt, index);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_column_double", CallingConvention=CallingConvention.Cdecl)]
			private static extern Double sqlite3_column_double(NativeSqlite3StmtHandle stmt, int index);

			/// <summary>Returns the value from the column with 0-based index 'index' as a string</summary>
			public static string ColumnString(sqlite3_stmt stmt, int index)
			{
				var ptr = sqlite3_column_text16((NativeSqlite3StmtHandle)stmt, index); // Sqlite returns null if this column is null
				if (ptr != IntPtr.Zero) return Marshal.PtrToStringUni(ptr) ?? string.Empty;
				return string.Empty;
			}
			[DllImport(Dll, EntryPoint = "sqlite3_column_text16", CallingConvention=CallingConvention.Cdecl)]
			private static extern IntPtr sqlite3_column_text16(NativeSqlite3StmtHandle stmt, int index);

			/// <summary>Returns the value from the column with 0-based index 'index' as an IntPtr</summary>
			public static void ColumnBlob(sqlite3_stmt stmt, int index, out IntPtr ptr, out int len)
			{
				// Read the blob size limit
				using (var db = sqlite3_db_handle((NativeSqlite3StmtHandle)stmt))
				{
					var max_size = sqlite3_limit(db, Limit.Length, -1);

					// sqlite returns null if this column is null
					ptr = sqlite3_column_blob((NativeSqlite3StmtHandle)stmt, index); // have to call this first
					len = sqlite3_column_bytes((NativeSqlite3StmtHandle)stmt, index);
					if (len < 0 || len > max_size) throw new SqliteException(Result.Corrupt, "Blob data size exceeds database maximum size limit");
				}
			}
			[DllImport(Dll, EntryPoint = "sqlite3_column_blob", CallingConvention=CallingConvention.Cdecl)]
			private static extern IntPtr sqlite3_column_blob(NativeSqlite3StmtHandle stmt, int index);

			/// <summary>Returns the size of the data in the column with 0-based index 'index'</summary>
			public static int ColumnBytes(sqlite3_stmt stmt, int index)
			{
				return sqlite3_column_bytes((NativeSqlite3StmtHandle)stmt, index);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_column_bytes", CallingConvention=CallingConvention.Cdecl)]
			private static extern int sqlite3_column_bytes(NativeSqlite3StmtHandle stmt, int index);

			/// <summary>Return the number of parameters in a prepared statement</summary>
			public static int BindParameterCount(sqlite3_stmt stmt)
			{
				return sqlite3_bind_parameter_count((NativeSqlite3StmtHandle)stmt);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_bind_parameter_count", CallingConvention=CallingConvention.Cdecl)]
			private static extern int sqlite3_bind_parameter_count(NativeSqlite3StmtHandle stmt);

			/// <summary>Return the index for the parameter named 'name'</summary>
			public static int BindParameterIndex(sqlite3_stmt stmt, string name)
			{
				return sqlite3_bind_parameter_index((NativeSqlite3StmtHandle)stmt, name);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_bind_parameter_index", CallingConvention=CallingConvention.Cdecl)]
			private static extern int sqlite3_bind_parameter_index(NativeSqlite3StmtHandle stmt, [MarshalAs(UnmanagedType.LPStr)] string name);

			/// <summary>Return the name of a parameter from its index</summary>
			public static string? BindParameterName(sqlite3_stmt stmt, int index)
			{
				return UTF8toStr(sqlite3_bind_parameter_name((NativeSqlite3StmtHandle)stmt, index));
			}
			[DllImport(Dll, EntryPoint = "sqlite3_bind_parameter_name", CallingConvention=CallingConvention.Cdecl)]
			private static extern IntPtr sqlite3_bind_parameter_name(NativeSqlite3StmtHandle stmt, int index);

			/// <summary>Bind null to 1-based parameter index 'index'</summary>
			public static void BindNull(sqlite3_stmt stmt, int index)
			{
				var r = sqlite3_bind_null((NativeSqlite3StmtHandle)stmt, index);
				if (r != Result.OK) throw new SqliteException(r, "Bind null failed");
			}
			[DllImport(Dll, EntryPoint = "sqlite3_bind_null", CallingConvention=CallingConvention.Cdecl)]
			private static extern Result sqlite3_bind_null(NativeSqlite3StmtHandle stmt, int index);

			/// <summary>Bind an integer value to 1-based parameter index 'index'</summary>
			public static void BindInt(sqlite3_stmt stmt, int index, int val)
			{
				var r = sqlite3_bind_int((NativeSqlite3StmtHandle)stmt, index, val);
				if (r != Result.OK) throw new SqliteException(r, "Bind int failed");
			}
			[DllImport(Dll, EntryPoint = "sqlite3_bind_int", CallingConvention=CallingConvention.Cdecl)]
			private static extern Result sqlite3_bind_int(NativeSqlite3StmtHandle stmt, int index, int val);

			/// <summary>Bind an integer64 value to 1-based parameter index 'index'</summary>
			public static void BindInt64(sqlite3_stmt stmt, int index, long val)
			{
				var r = sqlite3_bind_int64((NativeSqlite3StmtHandle)stmt, index, val);
				if (r != Result.OK) throw new SqliteException(r, "Bind int64 failed");
			}
			[DllImport(Dll, EntryPoint = "sqlite3_bind_int64", CallingConvention=CallingConvention.Cdecl)]
			private static extern Result sqlite3_bind_int64(NativeSqlite3StmtHandle stmt, int index, long val);

			/// <summary>Bind a double value to 1-based parameter index 'index'</summary>
			public static void BindDouble(sqlite3_stmt stmt, int index, double val)
			{
				var r = sqlite3_bind_double((NativeSqlite3StmtHandle)stmt, index, val);
				if (r != Result.OK) throw new SqliteException(r, "Bind double failed");
			}
			[DllImport(Dll, EntryPoint = "sqlite3_bind_double", CallingConvention=CallingConvention.Cdecl)]
			private static extern Result sqlite3_bind_double(NativeSqlite3StmtHandle stmt, int index, double val);

			/// <summary>Bind a string to 1-based parameter index 'index'</summary>
			public static void BindText(sqlite3_stmt stmt, int index, string val)
			{
				var r = sqlite3_bind_text16((NativeSqlite3StmtHandle)stmt, index, val, -1, TransientData);
				if (r != Result.OK) throw new SqliteException(r, "Bind string failed");
			}
			[DllImport(Dll, EntryPoint = "sqlite3_bind_text16", CallingConvention=CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			private static extern Result sqlite3_bind_text16(NativeSqlite3StmtHandle stmt, int index, string val, int n, IntPtr destructor_cb);

			/// <summary>Bind a byte array to 1-based parameter index 'index'</summary>
			public static void BindBlob(sqlite3_stmt stmt, int index, byte[] val, int length)
			{
				var r = sqlite3_bind_blob((NativeSqlite3StmtHandle)stmt, index, val, length, TransientData);
				if (r != Result.OK) throw new SqliteException(r, "Bind blob failed");
			}
			[DllImport(Dll, EntryPoint = "sqlite3_bind_blob", CallingConvention=CallingConvention.Cdecl)]
			private static extern Result sqlite3_bind_blob(NativeSqlite3StmtHandle stmt, int index, byte[] val, int n, IntPtr destructor_cb);

			/// <summary>Set the update hook callback function</summary>
			public static void UpdateHook(sqlite3 db, UpdateHookCB cb, IntPtr ctx)
			{
				sqlite3_update_hook((NativeSqlite3Handle)db, cb, ctx);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_update_hook", CallingConvention=CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
			private static extern IntPtr sqlite3_update_hook(NativeSqlite3Handle db, UpdateHookCB cb, IntPtr ctx);

			#endregion

			private static readonly IntPtr TransientData = new IntPtr(-1);
			private delegate void sqlite3_destructor_type(IntPtr ptr);

			/// <summary>Converts an IntPtr that points to a null terminated UTF-8 string into a .NET string</summary>
			public static string? UTF8toStr(IntPtr utf8ptr)
			{
				if (utf8ptr == IntPtr.Zero)
					return null;

				// There is no Marshal.PtrToStringUtf8 unfortunately, so we have to do it manually
				// Read up to (but not including) the null terminator
				byte b;
				var buf = new List<byte>(256);
				for (int i = 0; (b = Marshal.ReadByte(utf8ptr, i)) != 0; ++i)
					buf.Add(b);

				var bytes = buf.ToArray();
				return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
			}

			/// <summary>Converts a C# string (in UTF-16) to a byte array in UTF-8</summary>
			public static byte[] StrToUTF8(string str)
			{
				return Encoding.Convert(Encoding.Unicode, Encoding.UTF8, Encoding.Unicode.GetBytes(str));
			}
		}

		#endregion
	}

	#region SqliteException

	/// <summary>An exception type specifically for sqlite exceptions</summary>
	[Serializable]
	public class SqliteException :Exception
	{
		public SqliteException()
		{
			SqlErrMsg = string.Empty;
		}
		public SqliteException(string message)
			:base(message)
		{
			SqlErrMsg = string.Empty;
		}
		public SqliteException(string message, Exception inner_exception)
			:base(message, inner_exception)
		{
			SqlErrMsg = string.Empty;
		}
		public SqliteException(Sqlite.Result res, string message, string? sql_error_msg = null)
			:this(message)
		{
			Result = res;
			SqlErrMsg = sql_error_msg ?? string.Empty;
		}
		protected SqliteException(SerializationInfo serializationInfo, StreamingContext streamingContext)
		{
			throw new NotImplementedException();
		}

		/// <summary>The result code associated with this exception</summary>
		public Sqlite.Result Result { get; }

		/// <summary>The sqlite error message</summary>
		public string SqlErrMsg { get; }

		/// <summary></summary>
		public override string ToString()
		{
			return string.Format("{0} - {1}" ,Result ,Message);
		}
	}

	#endregion
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using System.IO;
	using System.Text.RegularExpressions;
	using Db;
	using Common;
	using Utility;

	[TestFixture]
	public class TestSqlite3
	{
		// Use an in-memory DB for normal unit tests, use an actual file when debugging
		private static string FilePath = Sqlite.DBInMemory; //new FileInfo("tmpDB.db").FullName;

		#region Custom types
		public enum SomeEnum
		{
			One = 1,
			Two = 2,
			Three = 3,
		}

		/// <summary>Tests user types can be mapped to table columns</summary>
		private class Custom
		{
			public Custom()
			{
				Str = "I'm a custom object";
			}
			public Custom(string str)
			{
				Str = str;
			}

			public string Str;

			public bool Equals(Custom other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				return other.Str == Str;
			}

			/// <summary>Binds this type to a parameter in a prepared statement</summary>
			public static void SqliteBind(Sqlite.sqlite3_stmt stmt, int idx, object obj)
			{
				Sqlite.Bind.Text(stmt, idx, ((Custom)obj).Str);
			}
			public static Custom SqliteRead(Sqlite.sqlite3_stmt stmt, int idx)
			{
				return new Custom((string)Sqlite.Read.Text(stmt, idx));
			}
		}
		#endregion

		#region Table Type 0

		public interface ITable0
		{
			int Inc_Key { get; set; }
			SomeEnum Inc_Enum { get; set; }
		}
		private class Table0 :ITable0
		{
			public Table0()
			{
				Inc_Key = 0;
				Inc_Value = string.Empty;
				Inc_Enum = SomeEnum.One;
				Ign_NoGetter = 0;
				Ign_PrivateProp = false;
				m_ign_private_field = 0;
				Ign_PublicField = (short)m_ign_private_field;
			}
			public Table0(ref int key, int seed)
			{
				Inc_Key = ++key;
				Inc_Value = seed.ToString(CultureInfo.InvariantCulture);
				Inc_Enum = (SomeEnum)((seed % 3) + 1);
				Ign_NoGetter = seed;
				Ign_PrivateProp = seed != 0;
				m_ign_private_field = seed;
				Ign_PublicField = (short)m_ign_private_field;
			}

			[Sqlite.Column(PrimaryKey = true)]
			public int Inc_Key { get; set; }
			public string Inc_Value { get; set; }
			public SomeEnum Inc_Enum { get; set; }
			public float Ign_NoGetter { set { } }
			public int Ign_NoSetter { get { return 42; } }
			private bool Ign_PrivateProp { get; set; }
			private int m_ign_private_field;
			public short Ign_PublicField;

			public override string ToString()
			{
				return Inc_Key + " " + Inc_Value;
			}
		}

		#endregion
		#region Table Type 1

		/// <summary>Single primary key table</summary>
		[Sqlite.Table(AllByDefault = false)]
		private class Table1
		{
			public Table1()
				: this(0)
			{ }
			public Table1(int val)
			{
				m_key = -1;
				m_bool = true;
				m_char = 'X';
				m_sbyte = 12;
				m_byte = 12;
				m_short = 1234;
				m_ushort = 1234;
				m_int = 12345678;
				m_uint = 12345678;
				m_int64 = 1234567890000;
				m_uint64 = 1234567890000;
				m_decimal = 1234567890.123467890m;
				m_float = 1.234567f;
				m_double = 1.2345678987654321;
				m_string = "string";
				m_buf = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
				m_empty_buf = null;
				m_int_buf = new[] { 0x10000000, 0x20000000, 0x30000000, 0x40000000 };
				m_guid = Guid.NewGuid();
				m_enum = SomeEnum.One;
				m_nullenum = SomeEnum.Two;
				m_dt_offset = DateTimeOffset.UtcNow;
				m_custom = new Custom();
				m_nullint = 23;
				m_nulllong = null;
				Ignored = val;
			}

			// Fields
			[Sqlite.Column(Order = 0, PrimaryKey = true, AutoInc = true, Constraints = "not null")] public int m_key;
			[Sqlite.Column(Order = 1)] protected bool m_bool;
			[Sqlite.Column(Order = 2)] public sbyte m_sbyte;
			[Sqlite.Column(Order = 3)] public byte m_byte;
			[Sqlite.Column(Order = 4)] private char m_char;
			[Sqlite.Column(Order = 5)] private short m_short { get; set; }
			[Sqlite.Column(Order = 6)] public ushort m_ushort { get; set; }
			[Sqlite.Column(Order = 7)] public int m_int;
			[Sqlite.Column(Order = 8)] public uint m_uint;
			[Sqlite.Column(Order = 9)] public long m_int64;
			[Sqlite.Column(Order = 10)] public ulong m_uint64;
			[Sqlite.Column(Order = 11)] public decimal m_decimal;
			[Sqlite.Column(Order = 12)] public float m_float;
			[Sqlite.Column(Order = 13)] public double m_double;
			[Sqlite.Column(Order = 14)] public string? m_string;
			[Sqlite.Column(Order = 15)] public byte[]? m_buf;
			[Sqlite.Column(Order = 16)] public byte[]? m_empty_buf;
			[Sqlite.Column(Order = 17)] public int[]? m_int_buf;
			[Sqlite.Column(Order = 18)] public Guid m_guid;
			[Sqlite.Column(Order = 19)] public SomeEnum m_enum;
			[Sqlite.Column(Order = 20)] public SomeEnum? m_nullenum;
			[Sqlite.Column(Order = 21)] public DateTimeOffset m_dt_offset;
			[Sqlite.Column(Order = 22, SqlDataType = Sqlite.DataType.Text)] public Custom m_custom;
			[Sqlite.Column(Order = 23)] public int? m_nullint;
			[Sqlite.Column(Order = 24)] public long? m_nulllong;

			// A field that isn't added to the table
			public int Ignored { get; set; }

			public bool Equals(Table1 other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				if (other.m_key != m_key) return false;
				if (other.m_bool != m_bool) return false;
				if (other.m_sbyte != m_sbyte) return false;
				if (other.m_byte != m_byte) return false;
				if (other.m_char != m_char) return false;
				if (other.m_short != m_short) return false;
				if (other.m_ushort != m_ushort) return false;
				if (other.m_int != m_int) return false;
				if (other.m_uint != m_uint) return false;
				if (other.m_int64 != m_int64) return false;
				if (other.m_uint64 != m_uint64) return false;
				if (other.m_decimal != m_decimal) return false;
				if (other.m_nullint != m_nullint) return false;
				if (other.m_nulllong != m_nulllong) return false;
				if (!Equals(other.m_string, m_string)) return false;
				if (!Equals(other.m_buf, m_buf)) return false;
				if (!Equals(other.m_empty_buf, m_empty_buf)) return false;
				if (!Equals(other.m_int_buf, m_int_buf)) return false;
				if (!Equals(other.m_enum, m_enum)) return false;
				if (!Equals(other.m_nullenum, m_nullenum)) return false;
				if (!other.m_guid.Equals(m_guid)) return false;
				if (!other.m_dt_offset.Equals(m_dt_offset)) return false;
				if (!other.m_custom.Equals(m_custom)) return false;
				if (Math.Abs(other.m_float - m_float) > float.Epsilon) return false;
				if (Math.Abs(other.m_double - m_double) > double.Epsilon) return false;
				return true;
			}
			private static bool Equals<T>(T[]? arr1, T[]? arr2)
			{
				if (arr1 == null) return arr2 == null || arr2.Length == 0;
				if (arr2 == null) return arr1.Length == 0;
				if (arr1.Length != arr2.Length) return false;
				return arr1.SequenceEqual(arr2);
			}
		}
		#endregion
		#region Table Type 2
		/// <summary>Tests PKs named at class level, Unicode, non-int type primary keys</summary>
		private class Table2Base
		{
			protected Table2Base()
			{
				PK = string.Empty;
				Inc_Explicit = -2;
			}

			// Should be a column, because explicitly named
			[Sqlite.Column] private int Inc_Explicit;

			// Notice the DOMType2 class indicates this is the primary key from a separate class.
			public string PK { get; protected set; }

			protected bool Equals(Table2Base other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				if (other.PK != PK) return false;
				if (other.Inc_Explicit != Inc_Explicit) return false;
				return true;
			}
		}

		[Sqlite.Table(PrimaryKey = "PK", PKAutoInc = false, FieldBindingFlags = BindingFlags.Public)]
		private class Table2 :Table2Base
		{
			public Table2()
			   : this(string.Empty, string.Empty)
			{ }
			public Table2(string key, string str)
			{
				PK = key;
				UniStr = str;
				Ign_PrivateField = true;
				Ign_PrivateField = !Ign_PrivateField;
			}

			// Should be a column, because of the FieldBindingFlags
			public string UniStr;

			// Should not be a column because private
			private bool Ign_PrivateField;

			public bool Equals(Table2 other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				if (other.UniStr != UniStr) return false;
				return base.Equals(other);
			}
		}
		#endregion
		#region Table Type 3
		/// <summary>Tests multiple primary keys, and properties in inherited/partial classes</summary>
		public class Table3Base
		{
			public int Parent1 { get; set; }
			public string? Key3 { get; set; }
			public double Ignored2 { get; set; }
		}

		[Sqlite.Table(Constraints = "primary key ([Index], [Key2], [Key3])")]
		[Sqlite.IgnoreColumns("Ignored1")]
		public partial class Table3
		{
			public Table3()
				: this(0, false, string.Empty)
			{ }
			public Table3(int key1, bool key2, string key3)
			{
				Index = key1;
				Key2 = key2;
				Key3 = key3;
				Prop1 = key1.ToString(CultureInfo.InvariantCulture) + " " + key2.ToString(CultureInfo.InvariantCulture);
				Prop2 = key1;
				Prop3 = Guid.NewGuid();
				Parent1 = key1;
				PropA = key1.ToString(CultureInfo.InvariantCulture) + " " + key2.ToString(CultureInfo.InvariantCulture);
				PropB = (SomeEnum)key1;
				Ignored1 = 1f;
				Ignored2 = 2.0;
			}

			public int Index { get; set; }
			public bool Key2 { get; set; }
			public string Prop1 { get; set; }
			public float Prop2 { get; set; }
			public Guid Prop3 { get; set; }
			public float Ignored1 { get; set; }

			public bool Equals(Table3 other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				if (other.Index != Index) return false;
				if (other.Key2 != Key2) return false;
				if (other.Prop1 != Prop1) return false;
				if (Math.Abs(other.Prop2 - Prop2) > float.Epsilon) return false;
				if (other.Prop3 != Prop3) return false;
				if (other.Parent1 != Parent1) return false;
				if (other.PropA != PropA) return false;
				if (other.PropB != PropB) return false;
				return true;
			}
		}

		[Sqlite.IgnoreColumns("Ignored2")]
		public partial class Table3 :Table3Base
		{
			public string PropA { get; set; }
			public SomeEnum PropB { get; set; }
		}
		#endregion
		#region Table Type 4
		/// <summary>Tests altering a table</summary>
		public class Table4
		{
			[Sqlite.Column(PrimaryKey = true)]
			public int Index { get; set; }
			public bool Key2 { get; set; }
			public string? Prop1 { get; set; }
			public float Prop2 { get; set; }
			public Guid Prop3 { get; set; }
			public int NewProp { get; set; }
		}
		#endregion
		#region Table Type 5
		/// <summary>Tests inherited Sqlite attributes</summary>
		[Sqlite.Table(PrimaryKey = "PK", PKAutoInc = true)]
		public class Table5Base
		{
			public int PK { get; set; }
		}
		public class Table5 :Table5Base
		{
			public Table5() : this(string.Empty) { }
			public Table5(string data) { Data = data; }
			public string Data { get; set; }
			[Sqlite.Ignore] public int Tmp { get; set; }
		}
		#endregion

		[TestFixtureSetUp]
		public void Setup()
		{
			Sqlite.LoadDll($"{UnitTest.LibPath}\\$(platform)\\$(config)");

			// Register custom type bind/read methods
			Sqlite.Bind.FunctionMap.Add(typeof(Custom), Custom.SqliteBind);
			Sqlite.Read.FunctionMap.Add(typeof(Custom), Custom.SqliteRead);

			// Use single threading
			Sqlite.Configure(Sqlite.ConfigOption.SingleThread);
		}
		[TestFixtureTearDown]
		public void Cleanup()
		{
			if (FilePath != Sqlite.DBInMemory)
				File.Delete(FilePath);
		}
		[Test]
		public void StandardUse()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create | Sqlite.OpenFlags.ReadWrite | Sqlite.OpenFlags.NoMutex))
			{
				// Tweak some DB settings for performance
				db.Execute(Sqlite.Sql("PRAGMA synchronous = OFF"));
				db.Execute(Sqlite.Sql("PRAGMA journal_mode = MEMORY"));

				// Create a simple table
				db.DropTable<Table0>();
				db.CreateTable<Table0>();
				Assert.True(db.TableExists<Table0>());

				// Check the table
				var table = db.Table<Table0>();
				Assert.Equal(3, table.ColumnCount);

				// Check the columns
				var column_names = new[] { "Inc_Key", "Inc_Value", "Inc_Enum" };
				using (var q = new Sqlite.Query(db, "select * from " + table.Name))
				{
					Assert.Equal(3, q.ColumnCount);
					Assert.True(column_names.Contains(q.ColumnName(0)));
					Assert.True(column_names.Contains(q.ColumnName(1)));
					Assert.True(column_names.Contains(q.ColumnName(2)));
				}

				// Create some objects to stick in the table
				int key = 0;
				var obj1 = new Table0(ref key, 5);
				var obj2 = new Table0(ref key, 6);
				var obj3 = new Table0(ref key, 7);

				// Insert stuff
				Assert.Equal(1, table.Insert(obj1));
				Assert.Equal(1, table.Insert(obj2));
				Assert.Equal(1, table.Insert(obj3));
				Assert.Equal(3, table.RowCount);

				// Check the query string
				var sql_count = "select count(*) from " + table.Name;
				using (var q = new Sqlite.Query(db, sql_count))
					Assert.Equal(sql_count, q.SqlString);
			}
		}
		[Test]
		public void AllTypes()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create | Sqlite.OpenFlags.ReadWrite | Sqlite.OpenFlags.NoMutex))
			{
				// Create a table
				db.DropTable<Table1>();
				db.CreateTable<Table1>();
				Assert.True(db.TableExists<Table1>());

				// Check the table
				var table = db.Table<Table1>();
				Assert.Equal(25, table.ColumnCount);

				// Check the columns
				using (var q = new Sqlite.Query(db, "select * from " + table.Name))
				{
					Assert.Equal(25, q.ColumnCount);
					Assert.Equal("m_key", q.ColumnName(0));
					Assert.Equal("m_bool", q.ColumnName(1));
					Assert.Equal("m_sbyte", q.ColumnName(2));
					Assert.Equal("m_byte", q.ColumnName(3));
					Assert.Equal("m_char", q.ColumnName(4));
					Assert.Equal("m_short", q.ColumnName(5));
					Assert.Equal("m_ushort", q.ColumnName(6));
					Assert.Equal("m_int", q.ColumnName(7));
					Assert.Equal("m_uint", q.ColumnName(8));
					Assert.Equal("m_int64", q.ColumnName(9));
					Assert.Equal("m_uint64", q.ColumnName(10));
					Assert.Equal("m_decimal", q.ColumnName(11));
					Assert.Equal("m_float", q.ColumnName(12));
					Assert.Equal("m_double", q.ColumnName(13));
					Assert.Equal("m_string", q.ColumnName(14));
					Assert.Equal("m_buf", q.ColumnName(15));
					Assert.Equal("m_empty_buf", q.ColumnName(16));
					Assert.Equal("m_int_buf", q.ColumnName(17));
					Assert.Equal("m_guid", q.ColumnName(18));
					Assert.Equal("m_enum", q.ColumnName(19));
					Assert.Equal("m_nullenum", q.ColumnName(20));
					Assert.Equal("m_dt_offset", q.ColumnName(21));
					Assert.Equal("m_custom", q.ColumnName(22));
					Assert.Equal("m_nullint", q.ColumnName(23));
					Assert.Equal("m_nulllong", q.ColumnName(24));
				}

				// Create some objects to stick in the table
				var obj1 = new Table1(5);
				var obj2 = new Table1(6);
				var obj3 = new Table1(7);
				obj2.m_dt_offset = DateTimeOffset.UtcNow;

				// Insert stuff
				Assert.Equal(1, table.Insert(obj1));
				Assert.Equal(1, table.Insert(obj2));
				Assert.Equal(1, table.Insert(obj3));
				Assert.Equal(3, table.RowCount);

				// Check Get() throws and Find() returns null if not found
				Assert.Null(table.Find(0));
				SqliteException? err = null;
				try { table.Get(4); } catch (SqliteException ex) { err = ex; }
				Assert.True(err != null && err.Result == Sqlite.Result.NotFound);

				// Get stuff and check it's the same
				var OBJ1 = table.Get(obj1.m_key);
				var OBJ2 = table.Get(obj2.m_key);
				var OBJ3 = table.Get(obj3.m_key);
				Assert.True(obj1.Equals(OBJ1));
				Assert.True(obj2.Equals(OBJ2));
				Assert.True(obj3.Equals(OBJ3));

				// Check parameter binding
				using (var q = new Sqlite.Query(db, Sqlite.Sql("select m_string,m_int from ", table.Name, " where m_string = @p1 and m_int = @p2")))
				{
					Assert.Equal(2, q.ParmCount);
					Assert.Equal("@p1", q.ParmName(1));
					Assert.Equal("@p2", q.ParmName(2));
					Assert.Equal(1, q.ParmIndex("@p1"));
					Assert.Equal(2, q.ParmIndex("@p2"));
					q.BindParm(1, "string");
					q.BindParm(2, 12345678);

					// Run the query
					Assert.True(q.Step());

					// Read the results
					Assert.Equal(2, q.ColumnCount);
					Assert.Equal(Sqlite.DataType.Text, q.ColumnType(0));
					Assert.Equal(Sqlite.DataType.Integer, q.ColumnType(1));
					Assert.Equal("m_string", q.ColumnName(0));
					Assert.Equal("m_int", q.ColumnName(1));
					Assert.Equal("string", q.ReadColumn<string>(0));
					Assert.Equal(12345678, q.ReadColumn<int>(1));

					// There should be 3 rows
					Assert.True(q.Step());
					Assert.True(q.Step());
					Assert.False(q.Step());
				}

				// Update stuff
				obj2.m_string = "I've been modified";
				Assert.Equal(1, table.Update(obj2));

				// Get the updated stuff and check it's been updated
				OBJ2 = table.Find(obj2.m_key);
				Assert.NotNull(OBJ2);
				Assert.True(obj2.Equals(OBJ2));

				// Delete something and check it's gone
				Assert.Equal(1, table.Delete(obj3));
				OBJ3 = table.Find(obj3.m_key);
				Assert.Null(OBJ3);

				// Update a single column and check it
				obj1.m_byte = 55;
				Assert.Equal(1, table.Update("m_byte", obj1.m_byte, 1));
				OBJ1 = table.Get(obj1.m_key);
				Assert.NotNull(OBJ1);
				Assert.True(obj1.Equals(OBJ1));

				// Read a single column
				var val = table.ColumnValue<ushort>("m_ushort", 2);
				Assert.Equal(obj2.m_ushort, val);

				// Add something back
				Assert.Equal(1, table.Insert(obj3));
				OBJ3 = table.Get(obj3.m_key);
				Assert.NotNull(OBJ3);
				Assert.True(obj3.Equals(OBJ3));

				// Update the column value for all rows
				obj1.m_byte = obj2.m_byte = obj3.m_byte = 0xAB;
				Assert.Equal(3, table.UpdateAll("m_byte", (byte)0xAB));

				// Enumerate objects
				var objs = table.Select(x => x).ToArray();
				Assert.Equal(3, objs.Length);
				Assert.True(obj1.Equals(objs[0]));
				Assert.True(obj2.Equals(objs[1]));
				Assert.True(obj3.Equals(objs[2]));

				// LINQ expressions
				objs = (from a in table where a.m_string == "I've been modified" select a).ToArray();
				Assert.Equal(1, objs.Length);
				Assert.True(obj2.Equals(objs[0]));
			}
		}
		[Test]
		public void Unicode()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create | Sqlite.OpenFlags.ReadWrite | Sqlite.OpenFlags.NoMutex))
			{
				// Create a table
				db.DropTable<Table2>();
				db.CreateTable<Table2>();
				Assert.True(db.TableExists<Table2>());

				// Check the table
				var table = db.Table<Table2>();
				Assert.Equal(3, table.ColumnCount);
				var column_names = new[] { "PK", "UniStr", "Inc_Explicit" };
				using (var q = new Sqlite.Query(db, "select * from " + table.Name))
				{
					Assert.Equal(3, q.ColumnCount);
					Assert.Equal("PK", q.ColumnName(0));
					Assert.True(column_names.Contains(q.ColumnName(0)));
					Assert.True(column_names.Contains(q.ColumnName(1)));
				}

				// Insert some stuff and check it stores/reads back ok
				var obj1 = new Table2("123", "€€€€");
				var obj2 = new Table2("abc", "⽄畂卧湥敳慈摮敬⡲㐲ㄴ⤷›慃獵摥戠㩹樠癡⹡慬杮吮牨睯扡敬›潣⹭湩牴湡汥洮扯汩扥汵敬⹴牃獡剨灥牯楴杮匫獥楳湯瑓牡䕴捸灥楴湯›敓獳潩⁮瑓牡整㩤眠楚塄婐桹㐰慳扲汬穷䌰㡶啩扁搶睄畎䐱䭡䝭牌夳䉡獁െ");
				Assert.Equal(1, table.Insert(obj1));
				Assert.Equal(1, table.Insert(obj2));
				Assert.Equal(2, table.RowCount);
				var OBJ1 = table.Get(obj1.PK);
				var OBJ2 = table.Get(obj2.PK);
				Assert.True(obj1.Equals(OBJ1));
				Assert.True(obj2.Equals(OBJ2));

				// Update Unicode stuff
				obj2.UniStr = "獁㩹獁";
				Assert.Equal(1, table.Update(obj2));
				OBJ2 = table.Get(obj2.PK);
				Assert.True(obj2.Equals(OBJ2));
			}
		}
		[Test]
		public void MultiplePks()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create | Sqlite.OpenFlags.ReadWrite | Sqlite.OpenFlags.NoMutex))
			{
				// Create a table
				db.DropTable<Table3>();
				db.CreateTable<Table3>();
				Assert.True(db.TableExists<Table3>());

				// Check the table
				var table = db.Table<Table3>();
				Assert.Equal(9, table.ColumnCount);
				using (var q = new Sqlite.Query(db, "select * from " + table.Name))
				{
					var cols = q.ColumnNames.ToList();
					Assert.Equal(9, q.ColumnCount);
					Assert.True(cols.Contains("Index"));
					Assert.True(cols.Contains("Key2"));
					Assert.True(cols.Contains("Key3"));
					Assert.True(cols.Contains("Prop1"));
					Assert.True(cols.Contains("Prop2"));
					Assert.True(cols.Contains("Prop3"));
					Assert.True(cols.Contains("PropA"));
					Assert.True(cols.Contains("PropB"));
					Assert.True(cols.Contains("Parent1"));
				}

				// Create some stuff
				var obj1 = new Table3(1, false, "first");
				var obj2 = new Table3(1, true, "first");
				var obj3 = new Table3(2, false, "first");
				var obj4 = new Table3(2, true, "first");

				// Insert it an check they're there
				Assert.Equal(1, table.Insert(obj1));
				Assert.Equal(1, table.Insert(obj2));
				Assert.Equal(1, table.Insert(obj3));
				Assert.Equal(1, table.Insert(obj4));
				Assert.Equal(4, table.RowCount);

				Assert.Throws<ArgumentException>(() => table.Get(obj1.Index, obj1.Key2));

				var OBJ1 = table.Get(obj1.Index, obj1.Key2, obj1.Key3);
				var OBJ2 = table.Get(obj2.Index, obj2.Key2, obj2.Key3);
				var OBJ3 = table.Get(obj3.Index, obj3.Key2, obj3.Key3);
				var OBJ4 = table.Get(obj4.Index, obj4.Key2, obj4.Key3);
				Assert.True(obj1.Equals(OBJ1));
				Assert.True(obj2.Equals(OBJ2));
				Assert.True(obj3.Equals(OBJ3));
				Assert.True(obj4.Equals(OBJ4));

				// Check insert collisions
				obj1.Prop1 = "I've been modified";
				{
					SqliteException? err = null;
					try { table.Insert(obj1); } catch (SqliteException ex) { err = ex; }
					Assert.True(err != null && err.Result == Sqlite.Result.Constraint);
					OBJ1 = table.Get(obj1.Index, obj1.Key2, obj1.Key3);
					Assert.NotNull(OBJ1);
					Assert.False(obj1.Equals(OBJ1));
				}
				{
					SqliteException? err = null;
					try { table.Insert(obj1, Sqlite.OnInsertConstraint.Ignore); } catch (SqliteException ex) { err = ex; }
					Assert.Null(err);
					OBJ1 = table.Get(obj1.Index, obj1.Key2, obj1.Key3);
					Assert.NotNull(OBJ1);
					Assert.False(obj1.Equals(OBJ1));
				}
				{
					SqliteException? err = null;
					try { table.Insert(obj1, Sqlite.OnInsertConstraint.Replace); } catch (SqliteException ex) { err = ex; }
					Assert.Null(err);
					OBJ1 = table.Get(obj1.Index, obj1.Key2, obj1.Key3);
					Assert.NotNull(OBJ1);
					Assert.True(obj1.Equals(OBJ1));
				}

				// Update in a multiple PK table
				obj2.PropA = "I've also been modified";
				Assert.Equal(1, table.Update(obj2));
				OBJ2 = table.Get(obj2.Index, obj2.Key2, obj2.Key3);
				Assert.NotNull(OBJ2);
				Assert.True(obj2.Equals(OBJ2));

				// Delete in a multiple PK table
				var keys = Sqlite.PrimaryKeys(obj3);
				Assert.Equal(1, table.DeleteByKey(keys));
				OBJ3 = table.Find(obj3.Index, obj3.Key2, obj3.Key3);
				Assert.Null(OBJ3);
			}
		}
		[Test]
		public void Transactions()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create | Sqlite.OpenFlags.ReadWrite | Sqlite.OpenFlags.NoMutex))
			{
				// Create a table
				db.DropTable<Table0>();
				db.CreateTable<Table0>();
				Assert.True(db.TableExists<Table0>());

				var table = db.Table<Table0>();

				// Create and insert some objects
				int key = 0;
				var objs = Enumerable.Range(0, 10).Select(i => new Table0(ref key, i)).ToList();
				foreach (var x in objs.Take(3))
					table.Insert(x, Sqlite.OnInsertConstraint.Replace);

				// Check they've been inserted
				var rows = db.EnumRows<Table0>("select * from Table0").ToList();
				Assert.Equal(3, rows.Count);

				// Add objects within a transaction that is not committed
				try
				{
					using (var tranny = db.NewTransaction())
					{
						int i = 0;
						foreach (var x in objs)
						{
							if (++i == 5) throw new Exception("aborting insert");
							table.Insert(x, Sqlite.OnInsertConstraint.Replace);
						}
						tranny.Commit();
					}
				}
				catch { }
				Assert.Equal(3, table.RowCount);

				// Add objects with no call to commit
				using (db.NewTransaction())
				{
					foreach (var x in objs)
						table.Insert(x, Sqlite.OnInsertConstraint.Replace);

					// No commit
				}
				Assert.Equal(3, table.RowCount);

				// Add object with commit
				using (var tranny = db.NewTransaction())
				{
					foreach (var x in objs.Take(5))
						table.Insert(x, Sqlite.OnInsertConstraint.Replace);

					tranny.Commit();
				}
				Assert.Equal(5, table.RowCount);

				// Add objects from a worker thread
				// In-memory DB's can't have multiple connections
				if (FilePath != Sqlite.DBInMemory)
				{
					using (var mre = new ManualResetEvent(false))
					{
						ThreadPool.QueueUserWorkItem(_ =>
						{
							using (var conn = new Sqlite.Database(db))
							using (var tranny = conn.NewTransaction())
							{
								var table_ = conn.Table<Table0>();
								foreach (var x in objs.Take(7))
									table_.Insert(x, Sqlite.OnInsertConstraint.Replace);

								tranny.Commit();
							}
							mre.Set();
						});
						mre.WaitOne();
						Assert.Equal(7, table.RowCount);
					}
				}
			}
		}
		[Test]
		public void RuntimeTypes()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create | Sqlite.OpenFlags.ReadWrite | Sqlite.OpenFlags.NoMutex))
			{
				// Create a table
				db.DropTable<Table1>();
				db.CreateTable<Table1>();
				Assert.True(db.TableExists<Table1>());
				var table = db.Table(typeof(Table1));

				// Create objects
				var objs = Enumerable.Range(0, 10).Select(i => new Table1(i)).ToList();
				foreach (var x in objs)
					Assert.Equal(1, table.Insert(x)); // insert without compile-time type info

				objs[5].m_string = "I am number 5";
				Assert.Equal(1, table.Update(objs[5]));

				var OBJS = table.Cast<Table1>().Select(x => x).ToList();
				for (int i = 0, iend = objs.Count; i != iend; ++i)
					Assert.True(objs[i].Equals(OBJS[i]));
			}
		}
		[Test]
		public void AlterTable()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create | Sqlite.OpenFlags.ReadWrite | Sqlite.OpenFlags.NoMutex))
			{
				// Create a table
				db.DropTable<Table3>();
				db.CreateTable<Table3>();
				Assert.True(db.TableExists<Table3>());

				// Check the table
				var table3 = db.Table<Table3>();
				Assert.Equal(9, table3.ColumnCount);
				using (var q = new Sqlite.Query(db, "select * from " + table3.Name))
				{
					var cols = q.ColumnNames.ToList();
					Assert.Equal(9, q.ColumnCount);
					Assert.Equal(9, cols.Count);
					Assert.True(cols.Contains("Index"));
					Assert.True(cols.Contains("Key2"));
					Assert.True(cols.Contains("Key3"));
					Assert.True(cols.Contains("Prop1"));
					Assert.True(cols.Contains("Prop2"));
					Assert.True(cols.Contains("Prop3"));
					Assert.True(cols.Contains("PropA"));
					Assert.True(cols.Contains("PropB"));
					Assert.True(cols.Contains("Parent1"));
				}

				// Create some stuff
				var obj1 = new Table3(1, false, "first");
				var obj2 = new Table3(1, true, "first");
				var obj3 = new Table3(2, false, "first");
				var obj4 = new Table3(2, true, "first");

				// Insert it an check they're there
				Assert.Equal(1, table3.Insert(obj1));
				Assert.Equal(1, table3.Insert(obj2));
				Assert.Equal(1, table3.Insert(obj3));
				Assert.Equal(1, table3.Insert(obj4));
				Assert.Equal(4, table3.RowCount);

				// Rename the table
				db.DropTable<Table4>();
				db.RenameTable<Table3>("Table4", false);

				// Alter the table to Table4
				db.AlterTable<Table4>();
				Assert.True(db.TableExists<Table4>());

				// Check the table
				var table4 = db.Table<Table4>();
				Assert.Equal(6, table4.ColumnCount);
				using (var q = new Sqlite.Query(db, "select * from " + table4.Name))
				{
					var cols = q.ColumnNames.ToList();
					Assert.Equal(10, q.ColumnCount);
					Assert.Equal(10, cols.Count);
					Assert.True(cols.Contains("Index"));
					Assert.True(cols.Contains("Key2"));
					Assert.True(cols.Contains("Key3"));
					Assert.True(cols.Contains("Prop1"));
					Assert.True(cols.Contains("Prop2"));
					Assert.True(cols.Contains("Prop3"));
					Assert.True(cols.Contains("PropA"));
					Assert.True(cols.Contains("PropB"));
					Assert.True(cols.Contains("Parent1"));
					Assert.True(cols.Contains("NewProp"));
				}
			}
		}
		[Test]
		public void ExprTree()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create | Sqlite.OpenFlags.ReadWrite | Sqlite.OpenFlags.NoMutex))
			{
				// Create a simple table
				db.DropTable<Table0>();
				db.CreateTable<Table0>();
				Assert.True(db.TableExists<Table0>());
				var table = db.Table<Table0>();

				// Insert stuff
				int key = 0;
				var values = new[] { 4, 1, 0, 5, 7, 9, 6, 3, 8, 2 };
				foreach (var v in values)
					Assert.Equal(1, table.Insert(new Table0(ref key, v)));
				Assert.Equal(10, table.RowCount);

				string sql_count = "select count(*) from " + table.Name;
				using (var q = new Sqlite.Query(db, sql_count))
					Assert.Equal(sql_count, q.SqlString);

				// Do some expression tree queries
				{// Count clause
					var q = table.Count(x => (x.Inc_Key % 3) == 0);
					Assert.Equal(3, q);
				}
				{// Where clause
					var q = from x in table where x.Inc_Key % 2 == 1 select x;
					var list = q.ToList();
					Assert.Equal(5, list.Count);
				}
				{// Where clause
					var q = table.Where(x => ((ITable0)x).Inc_Enum == SomeEnum.One || ((ITable0)x).Inc_Enum == SomeEnum.Three); // Cast needed to test expressions
					var list = q.ToList();
					Assert.Equal(7, list.Count);
					Assert.Equal(3, list[0].Inc_Key);
					Assert.Equal(4, list[1].Inc_Key);
					Assert.Equal(6, list[2].Inc_Key);
					Assert.Equal(7, list[3].Inc_Key);
					Assert.Equal(8, list[4].Inc_Key);
					Assert.Equal(9, list[5].Inc_Key);
					Assert.Equal(10, list[6].Inc_Key);
				}
				{// Where clause with 'like' method calling 'RowCount'
				 //var q = (from x in table where SqlMethods.Like(x.Inc_Value, "5") select x).RowCount;
				 //Assert.AreEqual(1, q);
				}
				{// Where clause with x => true
					var q = table.Where(x => true);
					var list = q.ToList();
					Assert.Equal(10, list.Count);
					Assert.Equal(1, list[0].Inc_Key);
					Assert.Equal(2, list[1].Inc_Key);
					Assert.Equal(3, list[2].Inc_Key);
					Assert.Equal(4, list[3].Inc_Key);
					Assert.Equal(5, list[4].Inc_Key);
					Assert.Equal(6, list[5].Inc_Key);
					Assert.Equal(7, list[6].Inc_Key);
					Assert.Equal(8, list[7].Inc_Key);
					Assert.Equal(9, list[8].Inc_Key);
					Assert.Equal(10, list[9].Inc_Key);
				}
				{// Contains clause
					var set = new[] { "2", "4", "8" };
					var q = from x in table where set.Contains(x.Inc_Value) select x;
					var list = q.ToList();
					Assert.Equal(3, list.Count);
					Assert.Equal(1, list[0].Inc_Key);
					Assert.Equal(9, list[1].Inc_Key);
					Assert.Equal(10, list[2].Inc_Key);
				}
				{// NOT Contains clause
					var set = new List<string> { "2", "4", "8", "5", "9" };
					var q = from x in table where set.Contains(x.Inc_Value) == false select x;
					var list = q.ToList();
					Assert.Equal(5, list.Count);
					Assert.Equal(2, list[0].Inc_Key);
					Assert.Equal(3, list[1].Inc_Key);
					Assert.Equal(5, list[2].Inc_Key);
					Assert.Equal(7, list[3].Inc_Key);
					Assert.Equal(8, list[4].Inc_Key);
				}
				{// NOT Contains clause
					var set = new List<string> { "2", "4", "8", "5", "9" };
					var q = from x in table where !set.Contains(x.Inc_Value) select x;
					var list = q.ToList();
					Assert.Equal(5, list.Count);
					Assert.Equal(2, list[0].Inc_Key);
					Assert.Equal(3, list[1].Inc_Key);
					Assert.Equal(5, list[2].Inc_Key);
					Assert.Equal(7, list[3].Inc_Key);
					Assert.Equal(8, list[4].Inc_Key);
				}
				{// OrderBy clause
					var q = from x in table orderby x.Inc_Key descending select x;
					var list = q.ToList();
					Assert.Equal(10, list.Count);
					for (int i = 0; i != 10; ++i)
						Assert.Equal(10 - i, list[i].Inc_Key);
				}
				{// Where and OrderBy clause
					var q = from x in table where x.Inc_Key >= 5 orderby x.Inc_Value select x;
					var list = q.ToList();
					Assert.Equal(6, list.Count);
					Assert.Equal(10, list[0].Inc_Key);
					Assert.Equal(8, list[1].Inc_Key);
					Assert.Equal(7, list[2].Inc_Key);
					Assert.Equal(5, list[3].Inc_Key);
					Assert.Equal(9, list[4].Inc_Key);
					Assert.Equal(6, list[5].Inc_Key);
				}
				{// Skip
					var q = table.Where(x => x.Inc_Key <= 5).Where(x => x.Inc_Value != "").Skip(2);
					var list = q.ToList();
					Assert.Equal(3, list.Count);
					Assert.Equal(3, list[0].Inc_Key);
					Assert.Equal(4, list[1].Inc_Key);
					Assert.Equal(5, list[2].Inc_Key);
				}
				{// Take
					var q = table.Where(x => x.Inc_Key >= 5).Take(2);
					var list = q.ToList();
					Assert.Equal(2, list.Count);
					Assert.Equal(5, list[0].Inc_Key);
					Assert.Equal(6, list[1].Inc_Key);
				}
				{// Skip and Take
					var q = table.Where(x => x.Inc_Key >= 5).Skip(2).Take(2);
					var list = q.ToList();
					Assert.Equal(2, list.Count);
					Assert.Equal(7, list[0].Inc_Key);
					Assert.Equal(8, list[1].Inc_Key);
				}
				{// Null test
					var q = from x in table where x.Inc_Value != null select x;
					var list = q.ToList();
					Assert.Equal(10, list.Count);
				}
				{// Type conversions
					var q = from x in table where (float)x.Inc_Key > 2.5f && (float)x.Inc_Key < 7.5f select x;
					var list = q.ToList();
					Assert.Equal(5, list.Count);
					Assert.Equal(3, list[0].Inc_Key);
					Assert.Equal(4, list[1].Inc_Key);
					Assert.Equal(5, list[2].Inc_Key);
					Assert.Equal(6, list[3].Inc_Key);
					Assert.Equal(7, list[4].Inc_Key);
				}
				{// Delete
					var q = table.Delete(x => x.Inc_Key > 5);
					var list = table.ToList();
					Assert.Equal(5, q);
					Assert.Equal(5, list.Count);
					Assert.Equal(1, list[0].Inc_Key);
					Assert.Equal(2, list[1].Inc_Key);
					Assert.Equal(3, list[2].Inc_Key);
					Assert.Equal(4, list[3].Inc_Key);
					Assert.Equal(5, list[4].Inc_Key);
				}
				{// Select
					var q = table.Select(x => x.Inc_Key);
					var list = q.ToList();
					Assert.Equal(5, list.Count);
					Assert.Equal(typeof(List<int>), list.GetType());
					Assert.Equal(1, list[0]);
					Assert.Equal(2, list[1]);
					Assert.Equal(3, list[2]);
					Assert.Equal(4, list[3]);
					Assert.Equal(5, list[4]);
				}
				{// Select tuple
					var q = table.Select(x => new { x.Inc_Key, x.Inc_Enum });
					var list = q.ToList();
					Assert.Equal(5, list.Count);
					Assert.Equal(1, list[0].Inc_Key);
					Assert.Equal(2, list[1].Inc_Key);
					Assert.Equal(3, list[2].Inc_Key);
					Assert.Equal(4, list[3].Inc_Key);
					Assert.Equal(5, list[4].Inc_Key);
					Assert.Equal(SomeEnum.Two, list[0].Inc_Enum);
					Assert.Equal(SomeEnum.Two, list[1].Inc_Enum);
					Assert.Equal(SomeEnum.One, list[2].Inc_Enum);
					Assert.Equal(SomeEnum.Three, list[3].Inc_Enum);
					Assert.Equal(SomeEnum.Two, list[4].Inc_Enum);
				}
#pragma warning disable 168
				{// Check sql strings are correct
					string? sql;

					var a = table.Where(x => x.Inc_Key == 3).Select(x => x.Inc_Enum).ToList();
					sql = table.SqlString;
					Assert.Equal("select Inc_Enum from Table0 where (Inc_Key==?)", sql);

					var b = table.Where(x => x.Inc_Key == 3).Select(x => new { x.Inc_Value, x.Inc_Enum }).ToList();
					sql = table.SqlString;
					Assert.Equal("select Inc_Value,Inc_Enum from Table0 where (Inc_Key==?)", sql);

					sql = table.Where(x => (x.Inc_Key & 0x3) == 0x1).GenerateExpression().ResetExpression().SqlString;
					Assert.Equal("select * from Table0 where ((Inc_Key&?)==?)", sql);

					sql = table.Where(x => x.Inc_Key == 3).GenerateExpression().ResetExpression().SqlString;
					Assert.Equal("select * from Table0 where (Inc_Key==?)", sql);

					sql = table.Where(x => x.Inc_Key == 3).Take(1).GenerateExpression().ResetExpression().SqlString;
					Assert.Equal("select * from Table0 where (Inc_Key==?) limit 1", sql);

					var t = table.FirstOrDefault(x => x.Inc_Key == 4);
					sql = table.SqlString;
					Assert.Equal("select * from Table0 where (Inc_Key==?) limit 1", sql);

					var l = table.Where(x => x.Inc_Key == 4).Take(4).Skip(2).ToList();
					sql = table.SqlString;
					Assert.Equal("select * from Table0 where (Inc_Key==?) limit 4 offset 2", sql);

					var q = (from x in table where x.Inc_Key == 3 select new { x.Inc_Key, x.Inc_Value }).ToList();
					sql = table.SqlString;
					Assert.Equal("select Inc_Key,Inc_Value from Table0 where (Inc_Key==?)", sql);

					var w = table.Delete(x => x.Inc_Key == 2);
					sql = table.SqlString;
					Assert.Equal("delete from Table0 where (Inc_Key==?)", sql);
				}
#pragma warning restore 168
			}
		}
		[Test]
		public void UntypedExprTree()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create | Sqlite.OpenFlags.ReadWrite | Sqlite.OpenFlags.NoMutex))
			{
				// Create a simple table
				db.DropTable<Table0>();
				db.CreateTable<Table0>();
				Assert.True(db.TableExists<Table0>());
				var table = db.Table(typeof(Table0));

				// Insert stuff
				int key = 0;
				var values = new[] { 4, 1, 0, 5, 7, 9, 6, 3, 8, 2 };
				foreach (var v in values)
					Assert.Equal(1, table.Insert(new Table0(ref key, v)));
				Assert.Equal(10, table.RowCount);

				string sql_count = "select count(*) from " + table.Name;
				using (var q = new Sqlite.Query(db, sql_count))
					Assert.Equal(sql_count, q.SqlString);

				// Do some expression tree queries
				{// Count clause
					var q = table.Count<Table0>(x => (x.Inc_Key % 3) == 0);
					Assert.Equal(3, q);
				}
				{// Where clause
					var q = table.Where<Table0>(x => ((ITable0)x).Inc_Enum == SomeEnum.One || ((ITable0)x).Inc_Enum == SomeEnum.Three).Cast<ITable0>(); // Cast needed to test expressions
					var list = q.ToList();
					Assert.Equal(7, list.Count);
					Assert.Equal(3, list[0].Inc_Key);
					Assert.Equal(4, list[1].Inc_Key);
					Assert.Equal(6, list[2].Inc_Key);
					Assert.Equal(7, list[3].Inc_Key);
					Assert.Equal(8, list[4].Inc_Key);
					Assert.Equal(9, list[5].Inc_Key);
					Assert.Equal(10, list[6].Inc_Key);
				}
				{// Where clause with 'like' method calling 'RowCount'
				 //var q = table.Where<Table0>(x => SqlMethods.Like(x.Inc_Value, "5")).RowCount;
				 //Assert.AreEqual(1, q);
				}
				{// Where clause with x => true
					var q = table.Where<Table0>(x => true).Cast<Table0>();
					var list = q.ToList();
					Assert.Equal(10, list.Count);
					Assert.Equal(1, list[0].Inc_Key);
					Assert.Equal(2, list[1].Inc_Key);
					Assert.Equal(3, list[2].Inc_Key);
					Assert.Equal(4, list[3].Inc_Key);
					Assert.Equal(5, list[4].Inc_Key);
					Assert.Equal(6, list[5].Inc_Key);
					Assert.Equal(7, list[6].Inc_Key);
					Assert.Equal(8, list[7].Inc_Key);
					Assert.Equal(9, list[8].Inc_Key);
					Assert.Equal(10, list[9].Inc_Key);
				}
				{// Contains clause
					var set = new[] { "2", "4", "8" };
					var q = table.Where<Table0>(x => set.Contains(x.Inc_Value)).Cast<Table0>();
					var list = q.ToList();
					Assert.Equal(3, list.Count);
					Assert.Equal(1, list[0].Inc_Key);
					Assert.Equal(9, list[1].Inc_Key);
					Assert.Equal(10, list[2].Inc_Key);
				}
				{// NOT Contains clause
					var set = new List<string> { "2", "4", "8", "5", "9" };
					var q = table.Where<Table0>(x => set.Contains(x.Inc_Value) == false).Cast<Table0>();
					var list = q.ToList();
					Assert.Equal(5, list.Count);
					Assert.Equal(2, list[0].Inc_Key);
					Assert.Equal(3, list[1].Inc_Key);
					Assert.Equal(5, list[2].Inc_Key);
					Assert.Equal(7, list[3].Inc_Key);
					Assert.Equal(8, list[4].Inc_Key);
				}
				{// NOT Contains clause
					var set = new List<string> { "2", "4", "8", "5", "9" };
					var q = table.Where<Table0>(x => !set.Contains(x.Inc_Value)).Cast<Table0>();
					var list = q.ToList();
					Assert.Equal(5, list.Count);
					Assert.Equal(2, list[0].Inc_Key);
					Assert.Equal(3, list[1].Inc_Key);
					Assert.Equal(5, list[2].Inc_Key);
					Assert.Equal(7, list[3].Inc_Key);
					Assert.Equal(8, list[4].Inc_Key);
				}
				{// OrderBy clause
					var q = table.OrderByDescending<Table0, int>(x => x.Inc_Key).Cast<Table0>();
					var list = q.ToList();
					Assert.Equal(10, list.Count);
					for (int i = 0; i != 10; ++i)
						Assert.Equal(10 - i, list[i].Inc_Key);
				}
				{// Where and OrderBy clause
					var q = table.Where<Table0>(x => x.Inc_Key >= 5).OrderBy<Table0, string>(x => x.Inc_Value).Cast<Table0>();
					var list = q.ToList();
					Assert.Equal(6, list.Count);
					Assert.Equal(10, list[0].Inc_Key);
					Assert.Equal(8, list[1].Inc_Key);
					Assert.Equal(7, list[2].Inc_Key);
					Assert.Equal(5, list[3].Inc_Key);
					Assert.Equal(9, list[4].Inc_Key);
					Assert.Equal(6, list[5].Inc_Key);
				}
				{// Skip
					var q = table.Where<Table0>(x => x.Inc_Key <= 5).Skip(2).Cast<Table0>();
					var list = q.ToList();
					Assert.Equal(3, list.Count);
					Assert.Equal(3, list[0].Inc_Key);
					Assert.Equal(4, list[1].Inc_Key);
					Assert.Equal(5, list[2].Inc_Key);
				}
				{// Take
					var q = table.Where<Table0>(x => x.Inc_Key >= 5).Take(2).Cast<Table0>();
					var list = q.ToList();
					Assert.Equal(2, list.Count);
					Assert.Equal(5, list[0].Inc_Key);
					Assert.Equal(6, list[1].Inc_Key);
				}
				{// Skip and Take
					var q = table.Where<Table0>(x => x.Inc_Key >= 5).Skip(2).Take(2).Cast<Table0>();
					var list = q.ToList();
					Assert.Equal(2, list.Count);
					Assert.Equal(7, list[0].Inc_Key);
					Assert.Equal(8, list[1].Inc_Key);
				}
				{// Null test
					var q = table.Where<Table0>(x => x.Inc_Value != null).Cast<Table0>();
					var list = q.ToList();
					Assert.Equal(10, list.Count);
				}
				{// Type conversions
					var q = table.Where<Table0>(x => (float)x.Inc_Key > 2.5f && (float)x.Inc_Key < 7.5f).Cast<Table0>();
					var list = q.ToList();
					Assert.Equal(5, list.Count);
					Assert.Equal(3, list[0].Inc_Key);
					Assert.Equal(4, list[1].Inc_Key);
					Assert.Equal(5, list[2].Inc_Key);
					Assert.Equal(6, list[3].Inc_Key);
					Assert.Equal(7, list[4].Inc_Key);
				}
				{// Delete
					var q = table.Delete<Table0>(x => x.Inc_Key > 5);
					var list = table.Cast<Table0>().ToList();
					Assert.Equal(5, q);
					Assert.Equal(5, list.Count);
					Assert.Equal(1, list[0].Inc_Key);
					Assert.Equal(2, list[1].Inc_Key);
					Assert.Equal(3, list[2].Inc_Key);
					Assert.Equal(4, list[3].Inc_Key);
					Assert.Equal(5, list[4].Inc_Key);
				}
#pragma warning disable 168
				{// Check sql strings are correct
					string? sql;

					sql = table.Where<Table0>(x => x.Inc_Key == 3).GenerateExpression().ResetExpression().SqlString;
					Assert.Equal("select * from Table0 where (Inc_Key==?)", sql);

					sql = table.Where<Table0>(x => x.Inc_Key == 3).Take(1).GenerateExpression().ResetExpression().SqlString;
					Assert.Equal("select * from Table0 where (Inc_Key==?) limit 1", sql);

					var t = table.FirstOrDefault<Table0>(x => x.Inc_Key == 4);
					sql = table.SqlString;
					Assert.Equal("select * from Table0 where (Inc_Key==?) limit 1", sql);

					var l = table.Where<Table0>(x => x.Inc_Key == 4).Take(4).Skip(2).Cast<Table0>().ToList();
					sql = table.SqlString;
					Assert.Equal("select * from Table0 where (Inc_Key==?) limit 4 offset 2", sql);

					var w = table.Delete<Table0>(x => x.Inc_Key == 2);
					sql = table.SqlString;
					Assert.Equal("delete from Table0 where (Inc_Key==?)", sql);
				}
#pragma warning restore 168
			}
		}
		[Test]
		public void Nullables()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create | Sqlite.OpenFlags.ReadWrite | Sqlite.OpenFlags.NoMutex))
			{
				// Create a simple table
				db.DropTable<Table1>();
				db.CreateTable<Table1>();
				Assert.True(db.TableExists<Table1>());
				var table = db.Table<Table1>();

				// Create some objects to stick in the table
				var obj1 = new Table1(1) { m_nullint = 1, m_int = 4 };
				var obj2 = new Table1(2) { m_nulllong = null };
				var obj3 = new Table1(3) { m_nullint = null };
				var obj4 = new Table1(4) { m_nulllong = 2 };

				// Insert stuff
				Assert.Equal(1, table.Insert(obj1));
				Assert.Equal(1, table.Insert(obj2));
				Assert.Equal(1, table.Insert(obj3));
				Assert.Equal(1, table.Insert(obj4));
				Assert.Equal(4, table.RowCount);

				{// non-null nullable
					int? nullable = 1;
					var q = table.Where(x => x.m_nullint == nullable);
					var list = q.ToList();
					Assert.Equal(1, list.Count);
					Assert.Equal(1, list[0].m_nullint);
				}
				{// non-null nullable
					long? nullable = 2;
					var q = table.Where(x => x.m_nulllong == nullable.Value);
					var list = q.ToList();
					Assert.Equal(1, list.Count);
					Assert.Equal((long?)2, list[0].m_nulllong);
				}
				{// null nullable
					int? nullable = null;
					var q = table.Where(x => x.m_nullint == nullable);
					var list = q.ToList();
					Assert.Equal(1, list.Count);
					Assert.Equal(null, list[0].m_nullint);
				}
				{// null nullable
					long? nullable = null;
					var q = table.Where(x => x.m_nullint == nullable);
					var list = q.ToList();
					Assert.Equal(1, list.Count);
					Assert.Equal(null, list[0].m_nulllong);
				}
				{// expression nullable(not null) == non-nullable
					const int target = 1;
					var q = table.Where(x => x.m_nullint == target);
					var list = q.ToList();
					Assert.Equal(1, list.Count);
					Assert.Equal(1, list[0].m_nullint);
				}
				{// expression non-nullable == nullable(not null)
					int? target = 4;
					var q = table.Where(x => x.m_int == target);
					var list = q.ToList();
					Assert.Equal(1, list.Count);
					Assert.Equal(4, list[0].m_int);
				}
				{// expression nullable(null) == non-nullable
					const long target = 2;
					var q = table.Where(x => x.m_nulllong == target);
					var list = q.ToList();
					Assert.Equal(1, list.Count);
					Assert.Equal((long?)2, list[0].m_nulllong);
				}
				{// expression non-nullable == nullable(null)
					int? target = null;
					var q = table.Where(x => x.m_int == target);
					var list = q.ToList();
					Assert.Equal(0, list.Count);
				}
				{// Testing members on nullable types
					var q = table.Where(x => x.m_nullint.HasValue == false);
					var list = q.ToList();
					Assert.Equal(1, list.Count);
					Assert.Equal(null, list[0].m_nullint);
				}
				{// Testing members on nullable types
					var q = table.Where(x => x.m_nullint.HasValue && x.m_nullint.Value == 23);
					var list = q.ToList();
					Assert.Equal(2, list.Count);
					Assert.Equal(23, list[0].m_nullint);
					Assert.Equal(23, list[1].m_nullint);
				}
			}
		}
		[Test]
		public void AttributeInheritance()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create | Sqlite.OpenFlags.ReadWrite | Sqlite.OpenFlags.NoMutex))
			{
				// Create a simple table
				db.DropTable<Table5>();
				db.CreateTable<Table5>();
				Assert.True(db.TableExists<Table5>());
				var table = db.Table<Table5>();

				var meta = Sqlite.TableMetaData.GetMetaData<Table5>();
				Assert.Equal(2, meta.ColumnCount);
				Assert.Equal(1, meta.Pks.Length);
				Assert.Equal("PK", meta.Pks[0].Name);
				Assert.Equal(1, meta.NonPks.Length);
				Assert.Equal("Data", meta.NonPks[0].Name);

				// Create some objects to stick in the table
				var obj1 = new Table5 { Data = "1" };
				var obj2 = new Table5 { Data = "2" };
				var obj3 = new Table5 { Data = "3" };
				var obj4 = new Table5 { Data = "4" };

				// Insert stuff
				Assert.Equal(1, table.Insert(obj1));
				Assert.Equal(1, table.Insert(obj2));
				Assert.Equal(1, table.Insert(obj3));
				Assert.Equal(1, table.Insert(obj4));
				Assert.Equal(4, table.RowCount);
			}
		}
		[Test]
		public void RowChangedEvents()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create | Sqlite.OpenFlags.ReadWrite | Sqlite.OpenFlags.NoMutex))
			{
				// Sign up a handler for row changed
				Sqlite.DataChangedArgs? args = null;
				db.DataChangedImmediate += (s, a) => args = a;

				// Create a simple table
				db.DropTable<Table5>();
				db.CreateTable<Table5>();
				Assert.True(db.TableExists<Table5>());
				var table = db.Table<Table5>();

				// Create some objects to stick in the table
				var obj1 = new Table5("One");
				var obj2 = new Table5("Two");

				// Insert stuff and check the event fires
				table.Insert(obj1);
				Assert.Equal(Sqlite.ChangeType.Insert, args!.ChangeType);
				Assert.Equal("Table5", args!.TableName);
				Assert.Equal(1L, args!.RowId);

				table.Insert(obj2);
				Assert.Equal(Sqlite.ChangeType.Insert, args!.ChangeType);
				Assert.Equal("Table5", args!.TableName);
				Assert.Equal(2L, args!.RowId);

				obj1.Data = "Updated";
				table.Update(obj1);
				Assert.Equal(Sqlite.ChangeType.Update, args!.ChangeType);
				Assert.Equal("Table5", args!.TableName);
				Assert.Equal(1L, args!.RowId);

				table.Delete(obj2);
				Assert.Equal(Sqlite.ChangeType.Delete, args!.ChangeType);
				Assert.Equal("Table5", args!.TableName);
				Assert.Equal(2L, args!.RowId);
			}
		}

#if false
		[Test] public void QueryCache()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create|Sqlite.OpenFlags.ReadWrite|Sqlite.OpenFlags.NoMutex))
			{
				// Create a simple table
				db.DropTable<Table5>();
				db.CreateTable<Table5>();
				Assert.True(db.TableExists<Table5>());
				var table = db.Table<Table5>();

				var obj1 = new Table5("One");
				var obj2 = new Table5("Two");
				var obj3 = new Table5("Fre");

				// Insert some stuff
				table.Insert(obj1);
				table.Insert(obj2);
				table.Insert(obj3);

				// Create a cache to optimise
				var cache_count = db.QueryCache.Count;

				var sql = "select * from Table5 where Data = ?";
				using (var q = db.Query(sql, 1, new object[]{"One"}))
				{
					var r = q.Rows<Table5>();
					var r0 = r.FirstOrDefault();
					Assert.True(r0 != null);
					Assert.True(r0.Data == "One");
				}

				Assert.True(db.QueryCache.IsCached(sql)); // in the cache while not in use

				using (var q = db.Query(sql, 1, new object[]{"Two"}))
				{
					Assert.True(!db.QueryCache.IsCached(sql)); // not in the cache while in use

					var r = q.Rows<Table5>();
					var r0 = r.FirstOrDefault();
					Assert.True(r0 != null);
					Assert.True(r0.Data == "Two");
				}

				Assert.True(db.QueryCache.Count == cache_count + 1); // back in the cache while not in use
			}
		[Test] public void ObjectCache()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create|Sqlite.OpenFlags.ReadWrite|Sqlite.OpenFlags.NoMutex))
			{
				// Create a simple table
				db.DropTable<Table5>();
				db.CreateTable<Table5>();
				Assert.True(db.TableExists<Table5>());
				var table = db.Table<Table5>();

				var obj1 = new Table5("One");
				var obj2 = new Table5("Two");
				var obj3 = new Table5("Fre");

				// Insert some stuff
				table.Insert(obj1);
				table.Insert(obj2);
				table.Insert(obj3);

				// Check the cache
				Assert.False(table.Cache.IsCached(obj1.PK));
				Assert.False(table.Cache.IsCached(obj2.PK));
				Assert.False(table.Cache.IsCached(obj3.PK));

				table.Get<Table5>(1);
				table.Get<Table5>(2);
				table.Get<Table5>(3);
				Assert.True(table.Cache.IsCached(obj1.PK));
				Assert.True(table.Cache.IsCached(obj2.PK));
				Assert.True(table.Cache.IsCached(obj3.PK));

				table.Cache.Capacity = 2;
				Assert.False(table.Cache.IsCached(obj1.PK));
				Assert.True(table.Cache.IsCached(obj2.PK));
				Assert.True(table.Cache.IsCached(obj3.PK));

				table.Cache.Capacity = 1;
				Assert.False(table.Cache.IsCached(obj1.PK));
				Assert.False(table.Cache.IsCached(obj2.PK));
				Assert.True(table.Cache.IsCached(obj3.PK));

				var o2_a = table.Get<Table5>(2);
				Assert.False(table.Cache.IsCached(obj1.PK));
				Assert.True(table.Cache.IsCached(obj2.PK));
				Assert.False(table.Cache.IsCached(obj3.PK));

				var o2_b = table.Get<Table5>(2);
				Assert.True(o2_a != null);
				Assert.True(o2_b != null);
				Assert.True(ReferenceEquals(o2_a,o2_b));

				o2_b.Tmp = 5;
				var o2_c = table.MetaData.Clone(o2_b);
				Assert.True(!ReferenceEquals(o2_b, o2_c));
				Assert.AreEqual(o2_b.Tmp, o2_c.Tmp);

				// Check that changes to the object automatically invalidate the cache
				obj2.Data = "Changed";
				table.Update(obj2);
				Assert.False(table.Cache.IsCached(obj2.PK));

				var o2_d = table.Get<Table5>(2);
				Assert.True(table.Cache.IsCached(obj2.PK));
				Assert.True(table.MetaData.Equal(obj2, o2_d));

				// Check that changes via individual column updates also invalidate the cache
				obj2.Data = "ChangedAgain";
				table.Update(nameof(Table5.Data), obj2.Data, obj2.PK);
				Assert.False(table.Cache.IsCached(obj2.PK));
				var o2_e = table.Get<Table5>(2);
				Assert.True(table.Cache.IsCached(obj2.PK));
				Assert.True(table.MetaData.Equal(obj2, o2_e));

				// Check deleting an object also removes it from the cache
				table.Delete(obj2);
				Assert.False(table.Cache.IsCached(obj2.PK));
			}
		}
		}
#endif
	}
}
#endif