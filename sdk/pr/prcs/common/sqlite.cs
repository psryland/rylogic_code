using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

// Usage:
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
// Notes:
// Trace/Debugging:
//  Define SQLITE_TRACE to trace query object creation and destruction.
//  This will help if you get an exception on shutdown saying there are
//  still open statements that haven't been finalised.
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

		/// <summary>The format string used to store/retrieve date time objects</summary>
		public const string DateTimeFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK";

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
			if (type == typeof(bool)     ||
				type == typeof(byte)     ||
				type == typeof(sbyte)    ||
				type == typeof(Char)     ||
				type == typeof(UInt16)   ||
				type == typeof(Int16)    ||
				type == typeof(Int32)    ||
				type == typeof(UInt32)   ||
				type == typeof(Int64)    ||
				type == typeof(UInt64)   ||
				type.IsEnum)
				return DataType.Integer;
			if (type == typeof(Single)   ||
				type == typeof(Double))
				return DataType.Real;
			if (type == typeof(String)         ||
				type == typeof(DateTime)       ||
				type == typeof(DateTimeOffset) ||
				type == typeof(Guid)           ||
				type == typeof(Decimal))
				return DataType.Text;
			if (type == typeof(byte[]))
				return DataType.Blob;
			
			throw new NotSupportedException(
				"No default type mapping for type "+type.Name+"\n" +
				"Custom data types should specify a value for the " +
				"'SqlDataType' property in their ColumnAttribute");
		}

		/// <summary>A helper for gluing strings together</summary>
		public static string Sql(params string[] parts)
		{
			return Sql(m_sql_cached_sb, parts);
		}
		public static string Sql(StringBuilder sb, params string[] parts)
		{
			sb.Length = 0;
			foreach (var p in parts) sb.Append(p);
			return sb.ToString();
		}
		private static readonly StringBuilder m_sql_cached_sb = new StringBuilder();

		/// <summary>
		/// Returns the primary key values read from 'item'. 
		/// The returned array can be pass to methods that take 'params object[]' arguments</summary>
		public static object[] PrimaryKeys(object item)
		{
			var meta = TableMetaData.GetMetaData(item.GetType());
			return meta.Pks.Select(x => x.Get(item)).ToArray();
		}

		/// <summary>Returns the sql string for the insert command for type 'type'</summary>
		public static string SqlInsertCmd(Type type, OnInsertConstraint on_constraint = OnInsertConstraint.Reject)
		{
			string cons;
			switch (on_constraint)
			{
			default: throw Exception.New(Result.Misuse, "Unknown OnInsertConstraint behaviour");
			case OnInsertConstraint.Reject:  cons = ""; break;
			case OnInsertConstraint.Ignore:  cons = "or ignore"; break;
			case OnInsertConstraint.Replace: cons = "or replace"; break;
			}
			
			var meta = TableMetaData.GetMetaData(type);
			return Sql(
				"insert ",cons," into ",meta.Name," (",
				string.Join(",", from c in meta.NonAutoIncs select c.Name),
				") values (",
				string.Join(",", from c in meta.NonAutoIncs select "?"),
				")");
		}

		/// <summary>Returns the sql string for the update command for type 'type'</summary>
		public static string SqlUpdateCmd(Type type)
		{
			var meta = TableMetaData.GetMetaData(type);
			return Sql(
				"update ",meta.Name," set ",
				string.Join(",", meta.NonPks.Select(x => x.Name+" = ?")),
				" where ",
				string.Join(" and ", meta.Pks.Select(x => x.Name+" = ?"))
				);
		}

		/// <summary>Returns the sql string for the delete command for type 'type'</summary>
		public static string SqlDeleteCmd(Type type)
		{
			var meta = TableMetaData.GetMetaData(type);
			return Sql("delete from ",meta.Name," where ",meta.PkConstraints());
		}

		/// <summary>Returns the sql string for the get command for type 'type'</summary>
		public static string SqlGetCmd(Type type)
		{
			var meta = TableMetaData.GetMetaData(type);
			return Sql("select * from ",meta.Name," where ",meta.PkConstraints());
		}

		/// <summary>Returns the sql string for the get command for type 'type'</summary>
		public static string SqlSelectAllCmd(Type type)
		{
			var meta = TableMetaData.GetMetaData(type);
			return Sql("select * from ",meta.Name);
		}

		/// <summary>Converts a C# string (in UTF-16) to a byte array in UTF-8</summary>
		private static byte[] StrToUTF8(string str)
		{
			return Encoding.Convert(Encoding.Unicode, Encoding.UTF8, Encoding.Unicode.GetBytes(str));
		}

		/// <summary>Converts an IntPtr that points to a null terminated UTF-8 string into a .NET string</summary>
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
			Trace.WriteLine(string.Format("Compiling sql string '{0}'", sql_string));
			Debug.Assert(!db.IsInvalid, "Database handle invalid");
			
			sqlite3_stmt stmt;
			var buf_utf8 = StrToUTF8(sql_string);
			var res = sqlite3_prepare_v2(db, buf_utf8, buf_utf8.Length, out stmt, IntPtr.Zero);
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
			public static void Decimal(sqlite3_stmt stmt, int idx, object value)
			{
				var dec = (decimal)value;
				sqlite3_bind_text16(stmt, idx, dec.ToString(), -1, TransientData);
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
			public static void DateTime(sqlite3_stmt stmt, int idx, object value)
			{
				if (value == null) sqlite3_bind_null(stmt, idx);
				else Text(stmt, idx, ((DateTime)value).ToString(DateTimeFormatString, CultureInfo.InvariantCulture));
			}
			public static void DateTimeOffset(sqlite3_stmt stmt, int idx, object value)
			{
				if (value == null) sqlite3_bind_null(stmt, idx);
				else Text(stmt, idx, ((DateTimeOffset)value).ToString(DateTimeFormatString, CultureInfo.InvariantCulture));
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
					Add(typeof(byte[])         ,Blob          );
					Add(typeof(Guid)           ,Guid          );
					Add(typeof(DateTime)       ,DateTime      );
					Add(typeof(DateTimeOffset) ,DateTimeOffset);
				}
			}
		}

		/// <summary>
		/// A lookup table from ClrType to binding function.
		/// Users can add custom types and binding functions to this map if needed</summary>
		public static readonly Bind.Map BindFunction = new Bind.Map();
		
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
			public static object Decimal(sqlite3_stmt stmt, int idx)
			{
				var ptr = sqlite3_column_text16(stmt, idx); // Sqlite returns null if this column is null
				if (ptr == IntPtr.Zero) return 0m;
				var str = Marshal.PtrToStringUni(ptr);
				if (str == null) return 0m;
				return decimal.Parse(str);
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
			public static object DateTime(sqlite3_stmt stmt, int idx)
			{
				string text = (string)Text(stmt, idx);
				return System.DateTime.ParseExact(text, DateTimeFormatString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
			}
			public static object DateTimeOffset(sqlite3_stmt stmt, int idx)
			{
				string text = (string)Text(stmt, idx);
				return System.DateTimeOffset.ParseExact(text, DateTimeFormatString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
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
					Add(typeof(byte[])         ,Blob          );
					Add(typeof(Guid)           ,Guid          );
					Add(typeof(DateTime)       ,DateTime      );
					Add(typeof(DateTimeOffset) ,DateTimeOffset);
				}
			}
		}

		/// <summary>
		/// A lookup table from ClrType to reading function.
		/// Users can add custom types and reading functions to this map if needed</summary>
		public static readonly Read.Map ReadFunction = new Read.Map();

		#endregion

		#region Sqlite3 handle wrappers

		public abstract class SQLiteHandle :SafeHandle
		{
			/// <summary>The result from the 'sqlite3_close' call</summary>
			public Result CloseResult { get; protected set; }

			/// <summary>Internal constructor used to create safehandles that optionally own the handle</summary>
			protected SQLiteHandle(IntPtr initial_ptr, bool owns_handle) :base(IntPtr.Zero, owns_handle)
			{
				CloseResult = Result.Empty; // Initialise to 'empty' so we know when its been set
				SetHandle(initial_ptr);
			}

			/// <summary>True if the contained handle is invalid</summary>
			public override bool IsInvalid
			{
				[PrePrepareMethod]
				[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
				get { return handle == IntPtr.Zero; }
			}

		}
		
		/// <summary>A wrapper for unmanaged sqlite database connection handles</summary>
		public sealed class sqlite3 :SQLiteHandle
		{
			public sqlite3() :base(IntPtr.Zero, true) {}
			public sqlite3(IntPtr non_owned_handle) :base(non_owned_handle, false) {}

			/// <summary>Frees the handle.</summary>
			[PrePrepareMethod][ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			protected override bool ReleaseHandle()
			{
				Trace.WriteLine(string.Format("Releasing sqlite3 handle ({0})", handle));
				CloseResult = sqlite3_close(handle);
				handle = IntPtr.Zero;
				return true;
			}
		}
		
		/// <summary>A wrapper for unmanaged sqlite prepared statement handles</summary>
		public sealed class sqlite3_stmt :SQLiteHandle
		{
			public sqlite3_stmt() :base(IntPtr.Zero, true) {}
			public sqlite3_stmt(IntPtr non_owned_handle) :base(non_owned_handle, false) {}
			
			/// <summary>Frees the handle.</summary>
			[PrePrepareMethod][ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			protected override bool ReleaseHandle()
			{
				Trace.WriteLine(string.Format("Releasing sqlite3_stmt handle ({0})", handle));
				CloseResult = sqlite3_finalize(handle);
				handle = IntPtr.Zero;
				return true;
			}
		}

		#endregion

		/// <summary>Represents a connection to an sqlite database file</summary>
		public class Database :IDisposable
		{
			private readonly sqlite3 m_db;

			/// <summary>The filepath of the database file opened</summary>
			public string Filepath { get; set; }

			/// <summary>Opens a connection to the database</summary>
			public Database(string filepath, OpenFlags flags = OpenFlags.Create|OpenFlags.ReadWrite)
			{
				Filepath = filepath;
				
				// Open the database file
				var res = sqlite3_open_v2(filepath, out m_db, (int)flags, IntPtr.Zero);
				if (res != Result.OK) throw Exception.New(res, "Failed to open database connection to file "+filepath);
				Trace.WriteLine(string.Format("Database connection opened for '{0}'", filepath));
				
				// Default to no busy timeout
				BusyTimeout = 0;
			}

			/// <summary>IDisposable interface</summary>
			public void Dispose()
			{
				Close();
			}

			/// <summary>Returns the db handle after asserting it's validity</summary>
			public sqlite3 Handle
			{
				get { Debug.Assert(!m_db.IsInvalid, "Invalid database handle"); return m_db; }
			}

			/// <summary>
			/// Sets the busy timeout period in milliseconds. This controls how long the thread
			/// sleeps for when waiting to gain a lock on a table. After this timeout period Step()
			/// will return Result.Busy. Set to 0 to turn off the busy wait handler.</summary>
			public int BusyTimeout
			{
				set
				{
					var res = sqlite3_busy_timeout(Handle, value);
					if (res != Result.OK) throw Exception.New(res, "Failed to set the busy timeout to "+value+"ms");
				}
			}

			/// <summary>Close a database file</summary>
			public void Close()
			{
				Trace.WriteLine(string.Format("Closing database connection{0}", m_db.IsClosed ? " (already closed)" : ""));
				if (m_db.IsClosed) return;
				
				// Release the db handle
				m_db.Close();
				if (m_db.CloseResult == Result.Busy)
				{
					Trace.Dump();
					throw Exception.New(m_db.CloseResult, "Could not close database handle, there are still prepared statements that haven't been 'finalized' or blob handles that haven't been closed.");
				}
				if (m_db.CloseResult != Result.OK)
				{
					throw Exception.New(m_db.CloseResult, "Failed to close database connection");
				}
			}

			/// <summary>
			/// Executes an sql query string that doesn't require binding, returning a 'Query' result.
			/// Remember to wrap the returned query in a 'using' statement or you might have trouble
			/// closing the database connection due to outstanding un-finalized prepared statements.<para/>
			/// e.g.<para/>
			/// <code>
			///   using (var query = ExecuteQuery(sql))<para/>
			///      int result = query.ReadColumn&lt;int&gt;(0);<para/>
			/// </code>
			/// </summary>
			public Query ExecuteQuery(string sql, IEnumerable<object> parms = null, int first_idx = 1)
			{
				var query = new Query(this, sql, parms, first_idx);
				query.Step();
				return query;
			}

			/// <summary>Executes an sql query that returns a scalar (i.e. int) result</summary>
			public int ExecuteScalar(string sql, IEnumerable<object> parms = null, int first_idx = 1)
			{
				using (Query query = ExecuteQuery(sql, parms, first_idx))
				{
					if (query.RowEnd) throw Exception.New(Result.Error, "Scalar query returned no results");
					int value = (int)Read.Int(query.Stmt, 0);
					if (query.Step()) throw Exception.New(Result.Error, "Scalar query returned more than one result");
					return value;
				}
			}

			/// <summary>Executes an sql statement returning the number of affected rows</summary>
			public int Execute(string sql, IEnumerable<object> parms = null, int first_idx = 1)
			{
				using (ExecuteQuery(sql, parms, first_idx))
					return sqlite3_changes(m_db);
			}

			/// <summary>Executes a query and enumerates the rows, returning each row as an instance of type 'T'</summary>
			public IEnumerable<T> EnumRows<T>(string sql, IEnumerable<object> parms = null, int first_idx = 1)
			{
				using (var query = new Query(this, sql, parms, first_idx))
					foreach (var r in query.Rows<T>())
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
				var opts = if_exists ? "if exists " : "";
				var sql  = Sql("drop table ",opts,meta.Name);
				Execute(sql);
			}

			/// <summary>Return the sql string used to create a table for 'type'</summary>
			public static string CreateTableString(Type type, OnCreateConstraint on_constraint = OnCreateConstraint.Reject)
			{
				var meta = TableMetaData.GetMetaData(type);
				return Sql("create table ",on_constraint == OnCreateConstraint.Reject?string.Empty:"if not exists ",meta.Name,"(\n",meta.Decl(),")");
			}

			/// <summary>
			/// Creates a table in the database based on type 'T'. Throws if not successful.<para/>
			/// 'factory' is a custom factory method to use to create instances of 'type', if null
			/// then Activator.CreateInstance will be used and the type will require a default constructor.<para/>
			/// For information about sql create table syntax see:<para/>
			///  http://www.sqlite.org/syntaxdiagrams.html#create-table-stmt <para/>
			///  http://www.sqlite.org/syntaxdiagrams.html#table-constraint <para/>
			/// Notes: <para/>
			///  auto increment must follow primary key without anything in between<para/></summary>
			public void CreateTable<T>(OnCreateConstraint on_constraint = OnCreateConstraint.Reject) where T:new()
			{
				CreateTable(typeof(T), ()=>new T(), on_constraint);
			}
			public void CreateTable<T>(Func<object> factory, OnCreateConstraint on_constraint = OnCreateConstraint.Reject)
			{
				CreateTable(typeof(T), factory, on_constraint);
			}
			public void CreateTable(Type type, Func<object> factory = null, OnCreateConstraint on_constraint = OnCreateConstraint.Reject)
			{
				var meta = TableMetaData.GetMetaData(type);
				if (factory != null) meta.Factory = factory;
				
				if (on_constraint == OnCreateConstraint.AlterTable && TableExists(type))
					AlterTable(type);
				else
					Execute(Sql(CreateTableString(type, on_constraint)));
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
				//// For each column in cols1, that isn't in cols0, drop it
				//foreach (var c in cols1)
				//{
				//    if (cols0.Contains(c)) continue;
				//    Execute(Sql("alter table ",meta.Name," drop column ",c));
				//}
					
				// For each column in cols0, that isn't in cols1, add it
				for (int i = 0, iend = cols0.Count; i != iend; ++i)
				{
					var c = cols0[i];
					if (cols1.Contains(c)) continue;
					Execute(Sql("alter table ",meta.Name," add column ",meta.Column(i).ColumnDef(true)));
				}
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
				var meta = TableMetaData.GetMetaData(type);
				var sql = Sql("alter table ",meta.Name," rename to ",new_name);
				Execute(sql);
				if (update_meta_data)
					meta.Name = new_name;
			}

			// ReSharper disable MemberHidesStaticFromOuterClass
			/// <summary>Return an existing table for type 'T'</summary>
			public Table<T> Table<T>()
			{
				return new Table<T>(this);
			}

			/// <summary>
			/// Return an existing table for runtime type 'type'.
			/// 'factory' is a factory method for creating instances of type 'type'. It null, then Activator.CreateInstance is used.</summary>
			public Table Table(Type type)
			{
				return new Table(type, this);
			}
			// ReSharper restore MemberHidesStaticFromOuterClass

			/// <summary>Find a row in a table of type 'T'</summary>
			public T Find<T>(params object[] keys)
			{
				return Table<T>().Find(keys);
			}

			/// <summary>Get a row from a table of type 'T'</summary>
			public T Get<T>(params object[] keys)
			{
				return Table<T>().Get(keys);
			}

			/// <summary>Insert 'item' into a table of type 'T'</summary>
			public int Insert<T>(T item, OnInsertConstraint on_constraint = OnInsertConstraint.Reject)
			{
				return Table<T>().Insert(item, on_constraint);
			}

			/// <summary>Update the row corresponding to 'item' in a table of type 'T'</summary>
			public int Update<T>(T item)
			{
				return Table<T>().Update(item);
			}

			/// <summary>Delete the row corresponding to 'item' from a table of type 'T'</summary>
			public int Delete<T>(T item)
			{
				return Table<T>().Delete(item);
			}
		}

		/// <summary>Represents a single table in the database</summary>
		public class Table :IEnumerable
		{
			protected readonly TableMetaData m_meta;
			protected readonly Database m_db; // The handle for the database connection
			
			/// <summary>Lazy created command for table enumeration. Set to null to start a new command</summary>
			protected CmdExpr Cmd { get { return m_cmd ?? (m_cmd = new CmdExpr()); } set { Debug.Assert(value == null); m_cmd = null; } }
			private CmdExpr m_cmd;
			
			public Table(Type type, Database db)
			{
				if (db.Handle.IsInvalid) throw new ArgumentNullException("db", "Invalid database handle");
				m_meta = TableMetaData.GetMetaData(type);
				m_db   = db;
				m_cmd  = null;
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

			/// <summary>Returns the number of rows in this table</summary>
			public int RowCount
			{
				get
				{
					try
					{
						Cmd.Generate(m_meta.Type, "count(*)");
						return m_db.ExecuteScalar(Cmd.SqlString, Cmd.Arguments);
					}
					finally { Cmd = null; }
				}
			}

			// ReSharper disable MemberHidesStaticFromOuterClass
			/// <summary>General sql query on the table</summary>
			public Query Query(string sql, IEnumerable<object> parms = null, int first_idx = 1)
			{
				return new Query(m_db, sql, parms, first_idx);
			}
			// ReSharper restore MemberHidesStaticFromOuterClass

			/// <summary>Returns a row in the table or null if not found</summary>
			public T Find<T>(params object[] keys)
			{
				using (var get = new GetCmd(m_meta, m_db))
				{
					get.BindPks(typeof(T), 1, keys);
					return (T)get.Find();
				}
			}

			/// <summary>Returns a row in the table or null if not found</summary>
			public T Find<T>(object key1, object key2) // overload for performance
			{
				using (var get = new GetCmd(m_meta, m_db))
				{
					get.BindPks(typeof(T), 1, key1, key2);
					return (T)get.Find();
				}
			}

			/// <summary>Returns a row in the table or null if not found</summary>
			public T Find<T>(object key1) // overload for performance
			{
				using (var get = new GetCmd(m_meta, m_db))
				{
					get.BindPks(typeof(T), 1, key1);
					return (T)get.Find();
				}
			}

			/// <summary>Returns a row in the table (throws if not found)</summary>
			public T Get<T>(params object[] keys)
			{
				var item = Find<T>(keys);
				if (ReferenceEquals(item, null)) throw Exception.New(Result.NotFound, "Row not found for key(s): "+string.Join(",", keys.Select(x=>x.ToString())));
				return item;
			}

			/// <summary>Returns a row in the table (throws if not found)</summary>
			public T Get<T>(object key1, object key2) // overload for performance
			{
				var item = Find<T>(key1, key2);
				if (ReferenceEquals(item, null)) throw Exception.New(Result.NotFound, "Row not found for keys: "+key1+","+key2);
				return item;
			}

			/// <summary>Returns a row in the table (throws if not found)</summary>
			public T Get<T>(object key1) // overload for performance
			{
				var item = Find<T>(key1);
				if (ReferenceEquals(item, null)) throw Exception.New(Result.NotFound, "Row not found for key: "+key1);
				return item;
			}

			/// <summary>
			/// Insert an item into the table.<para/>
			/// Note: insert will *NOT* change the primary keys/autoincrement members of 'item'.
			/// Make sure you call 'Get()' or the other overload of Insert to get the updated item.</summary>
			public int Insert<T>(T item, OnInsertConstraint on_constraint = OnInsertConstraint.Reject)
			{
				return Insert((object)item, on_constraint);
			}

			/// <summary>Insert an item into the table.<para/>
			/// Note: insert will *NOT* change the primary keys/autoincrement members of 'item'.
			/// Make sure you call 'Get()' or the other overload of Insert to get the updated item.</summary>
			public int Insert(object item, OnInsertConstraint on_constraint = OnInsertConstraint.Reject)
			{
				using (var insert = new InsertCmd(m_meta, m_db, on_constraint)) // Create the sql query
				{
					insert.BindObj(item); // Bind 'item' to it
					var count = insert.Run();  // Run the query
					m_meta.SetAutoIncPK(item, m_db.Handle);
					return count;
				}
			}

			/// <summary>Update 'item' in the table</summary>
			public int Update(object item)
			{
				using (var query = new Query(m_db, SqlUpdateCmd(m_meta.Type)))
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
			public int Delete(object item)
			{
				return DeleteByKey(PrimaryKeys(item));
			}

			/// <summary>Delete a row from the table. Returns the number of rows affected</summary>
			public int DeleteByKey(params object[] keys)
			{
				using (var query = new Query(m_db, SqlDeleteCmd(m_meta.Type)))
				{
					query.BindParms(1, keys);
					query.Step();
					return query.RowsChanged;
				}
			}

			/// <summary>Delete a row from the table. Returns the number of rows affected</summary>
			public int DeleteByKey(object key1, object key2) // overload for performance
			{
				using (var query = new Query(m_db, SqlDeleteCmd(m_meta.Type)))
				{
					query.BindParms(1, key1, key2);
					query.Step();
					return query.RowsChanged;
				}
			}

			/// <summary>Delete a row from the table. Returns the number of rows affected</summary>
			public int DeleteByKey(object key1) // overload for performance
			{
				using (var query = new Query(m_db, SqlDeleteCmd(m_meta.Type)))
				{
					query.BindParms(1, key1);
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
					query.BindPks(m_meta.Type, 2, keys);
					query.Step();
					return query.RowsChanged;
				}
			}

			/// <summary>Update the value of a column for all rows in a table</summary>
			public int UpdateAll<TValueType>(string column_name, TValueType value)
			{
				var column_meta = m_meta.Column(column_name);
				var sql = Sql("update ",m_meta.Name," set ",column_name," = ?");
				using (var query = new Query(m_db, sql))
				{
					column_meta.BindFn(query.Stmt, 1, value);
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
					query.BindPks(m_meta.Type, 1, keys);
					query.Step();
					return (TValueType)column_meta.ReadFn(query.Stmt, 0);
				}
			}

			/// <summary>IEnumerable interface</summary>
			IEnumerator IEnumerable.GetEnumerator()
			{
				try
				{
					Cmd.Generate(m_meta.Type, "*");
					using (var query = new Query(m_db, Cmd.SqlString, Cmd.Arguments))
					{
						while (query.Step())
						{
							var item = m_meta.Factory();
							m_meta.ReadObj(query.Stmt, item);
							yield return item;
						}
					}
				}
				finally { Cmd = null; }
			}

			/// <summary>Wraps an sql expression for enumerating the rows of this table</summary>
			protected class CmdExpr
			{
				private Expression m_where;
				private string m_order;
				private int? m_take;
				private int? m_skip;

				public override string ToString() { return (SqlString ?? "<not generated>") + (Arguments != null ? " ["+string.Join(",",Arguments)+"]" : ""); }

				/// <summary>The command translated into sql. Invalid until 'Generate()' is called</summary>
				public string SqlString { get; private set; }

				/// <summary>Arguments for '?'s in the generated SqlString. Invalid until 'Generate()' is called</summary>
				public List<object> Arguments { get; private set; }

				/// <summary>Add a 'where' clause to the command</summary>
				public void Where(Expression expr)
				{
					var lambda = expr as LambdaExpression;
					if (lambda != null) expr = lambda.Body;
					m_where = m_where == null ? expr : Expression.AndAlso(m_where, expr);
				}

				/// <summary>Add an 'order by' clause to the command</summary>
				public void OrderBy(Expression expr, bool ascending)
				{
					var lambda = expr as LambdaExpression;
					if (lambda != null) expr = lambda.Body;
					
					var args = new List<object>();
					var name = Translate(expr, args);
					if (args.Count != 0) throw new NotSupportedException("OrderBy expressions do not support parameters");
					
					if (m_order == null) m_order = ""; else m_order += ",";
					m_order += name;
					if (!ascending) m_order += " desc";
				}

				/// <summary>Add a limit to the number of rows returned</summary>
				public void Take(int n)
				{
					m_take = n;
				}

				/// <summary>Add an offset to the first row returned</summary>
				public void Skip(int n)
				{
					m_skip = n;
				}

				/// <summary>Return an sql string representing this command</summary>
				public string Generate(Type type, string select)
				{
					var meta = TableMetaData.GetMetaData(type);
					var cmd = new StringBuilder(Sql("select ",select," from ",meta.Name));
					var args = new List<object>();
					
					if (m_where != null)
					{
						var clause = Translate(m_where, args);
						cmd.Append(" where ").Append(clause);
					}
					if (m_order != null)
					{
						cmd.Append(" order by ").Append(m_order);
					}
					if (m_take.HasValue)
					{
						cmd.Append(" limit ").Append(m_take.Value);
					}
					if (m_skip.HasValue)
					{
						if (!m_take.HasValue) cmd.Append(" limit -1 ");
						cmd.Append(" offset ").Append(m_skip.Value);
					}

					SqlString = cmd.ToString();
					Arguments = args;
					return SqlString;
				}

				/// <summary>
				/// Translates an expression into an sql sub-string in 'sb'.
				/// Returns an object associated with 'expr' where appropriate, otherwise null.</summary>
				private StringBuilder Translate(Expression expr, List<object> args, StringBuilder sb = null)
				{
					sb = sb ?? new StringBuilder();
					
					var binary_expr = expr as BinaryExpression;
					if (binary_expr != null)
					{
						sb.Append("(");
						Translate(binary_expr.Left ,args ,sb);
					}
					
					// Helper for testing if an expression is a null constant
					Func<Expression, bool> IsNullConstant = exp => exp.NodeType == ExpressionType.Constant && ((ConstantExpression)exp).Value == null;
					
					switch (expr.NodeType)
					{
					default: throw new NotSupportedException();
					case ExpressionType.MemberAccess:
						#region Member Access
						{
							var me = (MemberExpression)expr;
							if (me.Expression.NodeType == ExpressionType.Parameter)
								return sb.Append(me.Member.Name);
							
							// Get the object that has the member
							var arrgs = new List<object>();
							Translate(me.Expression, arrgs, null);
							if (arrgs.Count != 1) throw new NotSupportedException("Could not find the object instance for MemberExpression: " + me);
							
							// Get the member value
							object ob;
							switch (me.Member.MemberType)
							{
							default: throw new NotSupportedException("MemberExpression: " + me.Member.MemberType.ToString());
							case MemberTypes.Property: ob = ((PropertyInfo)me.Member).GetValue(arrgs[0], null); break;
							case MemberTypes.Field:    ob = (   (FieldInfo)me.Member).GetValue(arrgs[0]); break;
							}
							
							Debug.Assert(args != null);
							
							// Handle IEnumerable
							if (ob != null && ob is IEnumerable && !(ob is string))
							{
								sb.Append("(");
								foreach (var a in (IEnumerable)ob)
								{
									args.Add(a);
									sb.Append("?,");
								}
								if (sb[sb.Length-1] != ')') sb.Length--;
								sb.Append(")");
							}
							else
							{
								args.Add(ob);
								sb.Append("?");
							}
							break;
						}
						#endregion
					case ExpressionType.Call:
						#region Method Call
						{
							var mc = (MethodCallExpression)expr;
							var method_name = mc.Method.Name.ToLowerInvariant();
							switch (method_name)
							{
							default: throw new NotSupportedException();
							case "like":
								{
									if (mc.Arguments.Count != 2) throw new NotSupportedException("The only supported 'like' method has 2 parameters");
									var p0 = Translate(mc.Arguments[0], args);
									var p1 = Translate(mc.Arguments[1], args);
									sb.Append("(").Append(p0).Append(" like ").Append(p1).Append(")");
									break;
								}
							case "contains":
								{
									if (mc.Object != null && mc.Arguments.Count == 1) // obj.Contains() calls...
									{
										var p0 = Translate(mc.Object, args);
										var p1 = Translate(mc.Arguments[0], args);
										sb.Append("(").Append(p1).Append(" in ").Append(p0).Append(")");
									}
									else if (mc.Object == null && mc.Arguments.Count == 2) // StaticClass.Contains(thing, in_things);
									{
										var p0 = Translate(mc.Arguments[0], args);
										var p1 = Translate(mc.Arguments[1], args);
										sb.Append("(").Append(p1).Append(" in ").Append(p0).Append(")");
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
						#region Constant
						{
							var ob = ((ConstantExpression)expr).Value;
							if (ob == null) sb.Append("null");
							else
							{
								sb.Append("?");
								args.Add(ob);
							}
							break;
						}
						#endregion
					case ExpressionType.Convert:
						#region Convert
						{
							var ue = (UnaryExpression)expr;
							var ty = ue.Type;
							var start = args.Count;
							Translate(ue.Operand, args, sb);
							for (; start != args.Count; ++start)
								args[start] = Convert.ChangeType(args[start], ty, null);
							break;
						}
						#endregion
					case ExpressionType.Not:
					case ExpressionType.UnaryPlus:
					case ExpressionType.Negate:
					case ExpressionType.OnesComplement:
						#region Unary Expressions
						{
							switch (expr.NodeType) {
							case ExpressionType.Not:            sb.Append(" not "); break;
							case ExpressionType.UnaryPlus:      sb.Append('+'); break;
							case ExpressionType.Negate:         sb.Append('-'); break;
							case ExpressionType.OnesComplement: sb.Append('~'); break;
							}
							sb.Append("(");
							Translate(((UnaryExpression)expr).Operand, args, sb);
							sb.Append(")");
							break;
						}
						#endregion
					case ExpressionType.Quote:
						#region Quote
						{
							sb.Append('"');
							Translate(((UnaryExpression)expr).Operand, args, sb);
							sb.Append('"');
							break;
						}
						#endregion
					case ExpressionType.Equal:              sb.Append(binary_expr != null && IsNullConstant(binary_expr.Right) ? " is " : "=="); break;
					case ExpressionType.NotEqual:           sb.Append(binary_expr != null && IsNullConstant(binary_expr.Right) ? " is not " : "!="); break;
					case ExpressionType.OrElse:             sb.Append(" or "); break;
					case ExpressionType.AndAlso:            sb.Append(" and "); break;
					case ExpressionType.Multiply:           sb.Append( "*"); break;
					case ExpressionType.Divide:             sb.Append( "/"); break;
					case ExpressionType.Modulo:             sb.Append( "%"); break;
					case ExpressionType.Add:                sb.Append( "+"); break;
					case ExpressionType.Subtract:           sb.Append( "-"); break;
					case ExpressionType.LeftShift:          sb.Append("<<"); break;
					case ExpressionType.RightShift:         sb.Append("<<"); break;
					case ExpressionType.And:                sb.Append( "&"); break;
					case ExpressionType.Or:                 sb.Append( "|"); break;
					case ExpressionType.LessThan:           sb.Append( "<"); break;
					case ExpressionType.LessThanOrEqual:    sb.Append("<="); break;
					case ExpressionType.GreaterThan:        sb.Append( ">"); break;
					case ExpressionType.GreaterThanOrEqual: sb.Append(">="); break;
					}
					
					if (binary_expr != null)
					{
						Translate(binary_expr.Right ,args ,sb);
						sb.Append(")");
					}
					return sb;
				}
			}
		}

		/// <summary>Represents a table within the database for a compile-time type 'T'.</summary>
		public class Table<T> :Table ,IEnumerable<T>
		{
			public Table(Database db) :base(typeof(T), db) {}

			/// <summary>Returns a row in the table or null if not found</summary>
			public T Find(params object[] keys)
			{
				return Find<T>(keys);
			}

			/// <summary>Returns a row in the table or null if not found</summary>
			public T Find(object key1, object key2) // overload for performance
			{
				return Find<T>(key1, key2);
			}

			/// <summary>Returns a row in the table or null if not found</summary>
			public T Find(object key1) // overload for performance
			{
				return Find<T>(key1);
			}

			/// <summary>Returns a row in the table (throws if not found)</summary>
			public T Get(params object[] keys)
			{
				return Get<T>(keys);
			}

			/// <summary>Returns a row in the table (throws if not found)</summary>
			public T Get(object key1, object key2) // overload for performance
			{
				return Get<T>(key1, key2);
			}

			/// <summary>Returns a row in the table (throws if not found)</summary>
			public T Get(object key1) // overload for performance
			{
				return Get<T>(key1);
			}

			/// <summary>IEnumerable interface</summary>
			IEnumerator<T> IEnumerable<T>.GetEnumerator()
			{
				foreach (var t in (IEnumerable)this)
					yield return (T)t;
			}

			/// <summary>Add a 'count' clause to row enumeration</summary>
			public int Count(Expression<Func<T,bool>> pred)
			{
				Cmd.Where(pred);
				return RowCount;
			}

			/// <summary>Add a 'where' clause to row enumeration</summary>
			public Table<T> Where(Expression<Func<T,bool>> pred)
			{
				Cmd.Where(pred);
				return this;
			}

			/// <summary>Add an 'OrderBy' clause to row enumeration</summary>
			public Table<T> OrderBy<U>(Expression<Func<T,U>> pred)
			{
				Cmd.OrderBy(pred, true);
				return this;
			}

			/// <summary>Add an 'OrderByDescending' clause to row enumeration</summary>
			public Table<T> OrderByDescending<U>(Expression<Func<T,U>> pred)
			{
				Cmd.OrderBy(pred, false);
				return this;
			}

			/// <summary>Add a 'limit' clause to row enumeration</summary>
			public Table<T> Take(int n)
			{
				Cmd.Take(n);
				return this;
			}

			/// <summary>Add an 'offset' clause to row enumeration</summary>
			public Table<T> Skip(int n)
			{
				Cmd.Skip(n);
				return this;
			}
		}

		/// <summary>Represents an sqlite prepared statement and iterative result (wraps an sqlite3_stmt handle).</summary>
		public class Query :IDisposable
		{
			protected readonly sqlite3_stmt m_stmt; // sqlite managed memory for this query
			protected bool m_row_end;      // True once the last row has been read
			
			public override string ToString()  { return SqlString; }
			
			public Query(sqlite3_stmt stmt)
			{
				if (stmt.IsInvalid) throw new ArgumentNullException("stmt", "Invalid sqlite prepared statement handle");
				m_stmt = stmt;
				m_row_end = false;
				Trace.QueryCtor(this);
				Trace.WriteLine(string.Format("Query created '{0}'", SqlString));
			}
			public Query(Database db, string sql_string, IEnumerable<object> parms = null, int first_idx = 1)
			:this(Compile(db.Handle, sql_string))
			{
				if (parms != null)
					BindParms(first_idx, parms);
			}
			~Query()
			{
				Trace.QueryDtor(this);
			}

			/// <summary>IDisposable</summary>
			public void Dispose()
			{
				Close();
			}

			/// <summary>The string used to construct this query statement</summary>
			public string SqlString
			{
				get { return sqlite3_sql(m_stmt); }
			}

			/// <summary>Returns the m_stmt field after asserting it's validity</summary>
			public sqlite3_stmt Stmt
			{
				get { Debug.Assert(!m_stmt.IsInvalid, "Invalid query object"); return m_stmt; }
			}

			/// <summary>Call when done with this query</summary>
			public void Close()
			{
				Trace.WriteLine(string.Format("Query closed{0}", m_stmt.IsClosed ? " (already closed)" : ""));
				if (m_stmt.IsClosed) return;
				
				// After 'sqlite3_finalize()', it is illegal to use m_stmt. So save 'db' here first
				sqlite3 db = sqlite3_db_handle(m_stmt);
				m_stmt.Close();
				if (m_stmt.CloseResult != Result.OK)
					throw Exception.New(m_stmt.CloseResult, ErrorMsg(db));
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
			public void BindParms(int first_idx, IEnumerable<object> parms)
			{
				foreach (var p in parms)
				{
					var bind = BindFunction[p.GetType()];
					bind(m_stmt, first_idx++, p);
				}
			}

			/// <summary>
			/// Bind a sequence of parameters starting at parameter index 'first_idx' (remember parameter indices start at 1)
			/// To reuse the Query, call Reset(), then bind new keys, then call Step() again</summary>
			public void BindParms(int first_idx, params object[] parms)
			{
				BindParms(first_idx, parms.AsEnumerable());
			}

			/// <summary>
			/// Bind an array of parameters starting at parameter index 'first_idx' (remember parameter indices start at 1)
			/// To reuse the Query, call Reset(), then bind new keys, then call Step() again</summary>
			public void BindParms(int first_idx, object parm1, object parm2) // overload for performance
			{
				BindFunction[parm1.GetType()](m_stmt, first_idx+0, parm1);
				BindFunction[parm1.GetType()](m_stmt, first_idx+1, parm2);
			}

			/// <summary>
			/// Bind an array of parameters starting at parameter index 'first_idx' (remember parameter indices start at 1)
			/// To reuse the Query, call Reset(), then bind new keys, then call Step() again</summary>
			public void BindParms(int first_idx, object parm1) // overload for performance
			{
				BindFunction[parm1.GetType()](m_stmt, first_idx, parm1);
			}

			/// <summary>
			/// Bind primary keys to this query starting at parameter index 
			/// 'first_idx' (remember parameter indices start at 1)
			/// This is functionally the same as BindParms but it validates
			/// the types of the primary keys against the table meta data.</summary>
			public void BindPks(Type type, int first_idx, params object[] keys)
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
				return sqlite3_column_name(Stmt, idx);
			}

			/// <summary>Enumerates the columns in the result</summary>
			public IEnumerable<string> ColumnNames
			{
				get { for (int i = 0, iend= ColumnCount; i != iend; ++i) yield return ColumnName(i); }
			}

			/// <summary>Read the value of a particular column</summary>
			public T ReadColumn<T>(int idx)
			{
				var read = ReadFunction[typeof(T)];
				return (T)read(m_stmt, idx);
			}

			/// <summary>Enumerate over the result rows of this query, interpreting each row as type 'T'</summary>
			public IEnumerable<T> Rows<T>()
			{
				var meta = TableMetaData.GetMetaData(typeof(T));
				while (Step())
				{
					var obj = meta.Factory();
					meta.ReadObj(Stmt, obj);
					yield return (T)obj;
				}
			}
		}

		#region Table Meta Data
		/// <summary>Mapping information from a type to columns in the table</summary>
		public class TableMetaData
		{
			private static readonly Dictionary<Type, TableMetaData> Meta = new Dictionary<Type, TableMetaData>();
			public static TableMetaData GetMetaData(Type type)
			{
				if (type == typeof(object)) throw new ArgumentException("Type 'object' cannot have TableMetaData", "type");
				
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
			
			/// <summary>Enumerate the column meta data</summary>
			public IEnumerable<ColumnMetaData> Columns { get { return m_column; } }
			
			/// <summary>The primary key columns, in order</summary>
			public ColumnMetaData[] Pks { get; private set; }
			
			/// <summary>The non primary key columns</summary>
			public ColumnMetaData[] NonPks { get; private set; }
			
			/// <summary>The columns that aren't auto increment columns</summary>
			public ColumnMetaData[] NonAutoIncs { get; private set; }
			
			/// <summary>Gets the number of columns in this table</summary>
			public int ColumnCount { get { return m_column.Length; } }
			
			/// <summary>True if the table uses multiple primary keys</summary>
			public bool MultiplePK { get { return Pks.Length > 1; } }
			
			/// <summary>A factory method for creating instances of the type for this table</summary>
			public Func<object> Factory { get; set; }

			/// <summary>A default method to use as a factory method for creating instances of type 'Type'</summary>
			public object DefaultFactory() { return Activator.CreateInstance(Type); }

			/// <summary>
			/// Constructs the meta data for mapping a type to a database table.
			/// By default, 'Activator.CreateInstance' is used as the factory function.
			/// This is slow however so use a static delegate if possible</summary>
			public TableMetaData(Type type)
			{
				Trace.WriteLine(string.Format("Creating table meta data for '{0}'", type.Name));
				
				// Get the table attribute
				var attr = type.GetCustomAttributes(typeof(TableAttribute), false).Cast<TableAttribute>().FirstOrDefault() ?? new TableAttribute();
				
				Type = type;
				Name = type.Name;
				Constraints = attr.Constraints ?? "";
				Factory = DefaultFactory;
				
				// Build a collection of columns to ignore
				var ignored = new List<string>();
				foreach (var ign in type.GetCustomAttributes(typeof(IgnoreColumnsAttribute), true).Cast<IgnoreColumnsAttribute>())
					ignored.AddRange(ign.Ignore.Split(',').Select(x => x.Trim()));
				
				// Create a collection of the columns of this table
				var cols = new List<ColumnMetaData>();
				{
					// Tests if a member should be included as a column in the table
					Func<MemberInfo, List<string>, bool> inc_member = (mi,def) =>
						!mi.GetCustomAttributes(typeof(IgnoreAttribute), false).Any() &&  // doesn't have the ignore attribute and,
						!ignored.Contains(mi.Name) &&                                     // isn't in the ignore list and,
						(mi.GetCustomAttributes(typeof(ColumnAttribute), false).Any() ||  // has the column attribute or,
						(attr.AllByDefault && def.Contains(mi.Name)));                         // all in by default and 'mi' is in the collection of found columns
					
					// If AllByDefault is enabled, get a collection of the properties/fields indicated by the binding flags in the attribute
					var pflags = attr.PropertyBindingFlags != BindingFlags.Default ? attr.PropertyBindingFlags |BindingFlags.Instance : BindingFlags.Default;
					var fflags = attr.FieldBindingFlags    != BindingFlags.Default ? attr.FieldBindingFlags    |BindingFlags.Instance : BindingFlags.Default;
					var by_def = !attr.AllByDefault ? null :
						type.GetProperties(pflags).Where(x => x.CanRead && x.CanWrite).Select(x => x.Name).Concat(
						type.GetFields    (fflags).Select(x => x.Name)).ToList();
					
					// Check all public/non-public properties/fields
					const BindingFlags binding_flags = BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
					cols.AddRange(AllProps (type, binding_flags).Where(pi => inc_member(pi, by_def)).Select(pi => new ColumnMetaData(pi)));
					cols.AddRange(AllFields(type, binding_flags).Where(fi => inc_member(fi, by_def)).Select(fi => new ColumnMetaData(fi)));
				}
				
				// If a primary key is named in the class level attribute, mark it
				if (attr.PrimaryKey != null)
				{
					var col = cols.FirstOrDefault(x => x.Name == attr.PrimaryKey);
					if (col == null) throw new ArgumentException("Named primary key column '"+attr.PrimaryKey+"' (given by Sqlite.TableAttribute) is not a found table column for type '"+Name+"'");
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
					if (s == -1 || e == -1) throw new ArgumentException("Table constraints '"+Constraints+"' are invalid");
					
					// Check that every named primary key is actually a column
					// and also ensure primary keys are ordered as given.
					int order = 0;
					foreach (var pk in Constraints.Substring(s+1, e-s-1).Split(',').Select(x => x.Trim()))
					{
						var col = cols.FirstOrDefault(x => x.Name == pk);
						if (col == null) throw new ArgumentException("Named primary key column '"+pk+"' was not found as a table column for type '"+Name+"'");
						col.IsPk = true;
						col.Order = order++;
					}
				}
				
				// Sort the columns by the given order
				var cmp = Comparer<int>.Default;
				cols.Sort((lhs,rhs) => cmp.Compare(lhs.Order, rhs.Order));
				
				// Create the column arrays
				m_column    = cols.ToArray();
				Pks         = m_column.Where(x => x.IsPk).ToArray();
				NonPks      = m_column.Where(x => !x.IsPk).ToArray();
				NonAutoIncs = m_column.Where(x => !x.IsAutoInc).ToArray();
				m_single_pk = Pks.Count() == 1 ? Pks.First() : null;
			}

			/// <summary>Returns all inherited properties for a type</summary>
			private IEnumerable<PropertyInfo> AllProps(Type type, BindingFlags flags)
			{
				if (type == null || type == typeof(object)) return Enumerable.Empty<PropertyInfo>();
				return AllProps(type.BaseType, flags).Concat(type.GetProperties(flags|BindingFlags.DeclaredOnly));
			}

			/// <summary>Returns all inherited fields for a type</summary>
			private IEnumerable<FieldInfo> AllFields(Type type, BindingFlags flags)
			{
				if (type == null || type == typeof(object)) return Enumerable.Empty<FieldInfo>();
				return AllFields(type.BaseType, flags).Concat(type.GetFields(flags|BindingFlags.DeclaredOnly));
			}

			/// <summary>Return the column meta data for a column by name</summary>
			public ColumnMetaData Column(int column_index)
			{
				return m_column[column_index];
			}

			/// <summary>Return the column meta data for a column by name</summary>
			public ColumnMetaData Column(string column_name)
			{
				return m_column.FirstOrDefault(x => string.CompareOrdinal(x.Name, column_name) == 0);
			}

			/// <summary>
			/// Bind the primary keys 'keys' to the parameters in a
			/// prepared statement starting at parameter 'first_idx'.</summary>
			public void BindPks(sqlite3_stmt stmt, int first_idx, params object[] keys)
			{
				if (first_idx < 1) throw new ArgumentException("Parameter binding indices start at 1 so 'first_idx' must be >= 1");
				if (Pks.Length != keys.Length) throw new ArgumentException("Not enough primary key values passed for type "+Name);
				
				int idx = 0;
				foreach (var c in Pks)
				{
					if (keys[idx].GetType() != c.ClrType) throw new ArgumentException("Primary key "+(idx+1)+" should be of type "+c.ClrType.Name+" not type "+keys[idx].GetType().Name);
					c.BindFn(stmt, first_idx+idx, keys[idx]); // binding parameters are indexed from 1 (hence the +1)
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
			public void BindObj<T>(sqlite3_stmt stmt, int first_idx, T item)
			{
				if (first_idx < 1) throw new ArgumentException("parameter binding indices start at 1 so 'first_idx' must be >= 1");
				if (item.GetType() != Type) throw new ArgumentException("'item' is not the correct type for this table");
				
				int idx = 0; // binding parameters are indexed from 1
				foreach (var c in NonAutoIncs)
				{
					c.BindFn(stmt, first_idx+idx, c.Get(item));
					++idx;
				}
			}

			/// <summary>Populate the properties and fields of 'item' from the column values read from 'stmt'</summary>
			public void ReadObj<T>(sqlite3_stmt stmt, T item)
			{
				if (item.GetType() != Type) throw new ArgumentException("'item' is not the correct type for this table");
				
				for (int i = 0, iend = sqlite3_column_count(stmt); i != iend; ++i)
				{
					var cname = sqlite3_column_name(stmt, i);
					var col = Column(cname);
					
					// Since sqlite does not support dropping columns in a table, it's likely,
					// for backwards compatibility, that a table will contain columns that don't
					// correspond to a property or field in 'item'. These columns are silently ignored.
					//if (col == null) throw new ArgumentException("'item' does not contain a property or field called '"+cname+"'");
					if (col == null) continue;
					
					col.Set(item, col.ReadFn(stmt, i));
				}
			}

			/// <summary>Returns a table declaration string for this table</summary>
			public string Decl()
			{
				var sb = new StringBuilder();
				sb.Append(string.Join(",\n", from c in m_column select c.ColumnDef(m_single_pk != null)));
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

			/// <summary>Updates the value of the auto increment primary key (if there is one)</summary>
			public void SetAutoIncPK(object obj, sqlite3 db)
			{
				if (m_single_pk == null || !m_single_pk.IsAutoInc) return;
				int id = (int)sqlite3_last_insert_rowid(db);
				m_single_pk.Set(obj, id);
			}
		}
		#endregion

		#region Column Meta Data
		/// <summary>A column within a db table</summary>
		public class ColumnMetaData
		{
			public const int OrderBaseValue = 0xFFFF;
			
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
			public Bind.Func BindFn;
			
			/// <summary>Reads column 'index' from 'stmt' and sets the corresponding property or field in 'obj'</summary>
			public Read.Func ReadFn;
			
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
				Trace.WriteLine(string.Format("   Column: '{0}'", mi.Name));
				var attr = (ColumnAttribute)mi.GetCustomAttributes(typeof(ColumnAttribute), true).FirstOrDefault() ?? new ColumnAttribute();
				var underlying_type = Nullable.GetUnderlyingType(type);
				
				Name            = mi.Name;
				SqlDataType     = attr.SqlDataType != DataType.Null ? attr.SqlDataType : SqlType(type);
				Constraints     = attr.Constraints ?? "";
				IsPk            = attr.PrimaryKey;
				IsAutoInc       = attr.AutoInc;
				IsNotNull       = type.IsValueType && underlying_type == null;
				IsCollate       = false;
				Order           = OrderBaseValue + attr.Order;
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
					else if (underlying_type != null)
					{
						var bind = BindFunction[underlying_type];
						BindFn = (stmt,idx,obj) =>
							{
								if (obj != null) bind(stmt, idx, obj);
								else sqlite3_bind_null(stmt, idx);
							};
						
						var read = ReadFunction[underlying_type];
						ReadFn = (stmt,idx) =>
							{
								return sqlite3_column_type(stmt, idx) != DataType.Null
									? read(stmt, idx)
									: null;
							};
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
					"A bind or read function was not found for type '"+ClrType.Name+"'\r\n" +
					"Custom types need to register a Bind/Read function in the " +
					"BindFunction/ReadFunction map before being used");
			}

			/// <summary>Returns the column definition for this column</summary>
			public string ColumnDef(bool incl_pk)
			{
				return Sql(Name," ",SqlDataType.ToString().ToLowerInvariant()," ",incl_pk&&IsPk?"primary key ":"",IsAutoInc?"autoincrement ":"",Constraints);
			}

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

		#region Specialised query sub-classes

		/// <summary>A specialised query used for inserting objects into a table</summary>
		public class InsertCmd :Query
		{
			protected readonly TableMetaData m_meta;

			/// <summary>Creates a compiled query for inserting an object of type 'type' into a table.</summary>
			public InsertCmd(TableMetaData meta, Database db, OnInsertConstraint on_constraint = OnInsertConstraint.Reject)
			:base(db, SqlInsertCmd(meta.Type, on_constraint))
			{
				m_meta = meta;
			}

			/// <summary>Bind the values in 'item' to this insert query making it ready for running</summary>
			public void BindObj(object item)
			{
				m_meta.BindObj(m_stmt, 1, item);
			}

			/// <summary>Run the insert command. Call 'Reset()' before running the command again</summary>
			public int Run()
			{
				Step();
				if (!RowEnd) throw Exception.New(Result.Misuse, "Insert returned more than one row");
				return RowsChanged;
			}
		}

		/// <summary>A specialised query used for getting objects from the db</summary>
		public class GetCmd :Query
		{
			protected readonly TableMetaData m_meta;

			/// <summary>
			/// Create a compiled query for getting an object of type 'T' from a table.
			/// Users can then bind primary keys, and run the query repeatedly to get multiple items.</summary>
			public GetCmd(TableMetaData meta, Database db) :base(db, SqlGetCmd(meta.Type))
			{
				m_meta = meta;
			}

			/// <summary>
			/// Populates 'item' and returns true if the query finds a row, otherwise returns false.
			/// Remember to call 'Reset()' before running the command again</summary>
			public object Find()
			{
				Step();
				if (RowEnd) return null;
				var item = m_meta.Factory();
				m_meta.ReadObj(m_stmt, item);
				return item;
			}
		}

		#endregion

		#region Attribute types

		/// <summary>
		/// Controls the mapping from .NET type to database table.
		/// By default, all public properties (including inherited properties) are used as columns,
		/// this can be changed using the PropertyBindingFlags/FieldBindingFlags properties.</summary>
		[AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
		public class TableAttribute :Attribute
		{
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
			public string Constraints { get; set; }
			
			/// <summary>
			/// The name of the property or field to use as the primary key for a table.
			/// This property can be used to specify the primary key at a class level which
			/// is helpful if the class is part of an inheritance hierarchy or is a partial
			/// class. This property is used in addition to primary keys specified by
			/// property/field attributes or table constraints. If given, the column Order
			/// value will be set to 0 for that column.<para/>
			/// Default is value is null.</summary>
			public string PrimaryKey { get; set; }
			
			/// <summary>
			/// Set to true if the column given by 'PrimaryKey' is also an auto increment
			/// column. Not used if PrimaryKey is not specified. Default is false.</summary>
			public bool PKAutoInc { get; set; }
			
			public TableAttribute()
			{
				AllByDefault         = true;
				PropertyBindingFlags = BindingFlags.Public;
				FieldBindingFlags    = BindingFlags.Default;
				Constraints          = null;
				PrimaryKey           = null;
				PKAutoInc            = false;
			}
		}

		/// <summary>
		/// Allows a class or struct to list members that do not create table columns.
		/// This attribute can be inherited from partial or base classes, the resulting
		/// ignored members are the union of all ignore column attributes for a type</summary>
		[AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
		public class IgnoreColumnsAttribute :Attribute
		{
			/// <summary>A comma separated list of properties/fields to ignore</summary>
			public string Ignore { get; private set; }
			
			public IgnoreColumnsAttribute(string column_names)
			{
				Ignore = column_names;
			}
		}

		/// <summary>Marks a property or field as a column in the db table for a type</summary>
		[AttributeUsage(AttributeTargets.Property|AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
		public class ColumnAttribute :Attribute
		{
			/// <summary>
			/// True if this column should be used as a primary key.
			/// If multiple primary keys are specified, ensure the Order
			/// property is used so that the order of primary keys is defined.
			/// Default value is false.</summary>
			public bool PrimaryKey { get; set; }
			
			/// <summary>True if this column should auto increment. Default is false</summary>
			public bool AutoInc { get; set; }
			
			/// <summary>Defines the relative order of columns in the table. Default is '0'</summary>
			public int Order { get; set; }
			
			/// <summary>
			/// The sqlite data type used to represent this column. 
			/// If set to DataType.Null, then the default mapping from .NET data type to sqlite type is used.
			/// Default value is DataType.Null</summary>
			public DataType SqlDataType { get; set; }
			
			/// <summary>Custom constraints to add to this column. Default is null</summary>
			public string Constraints { get; set; }
			
			public ColumnAttribute()
			{
				PrimaryKey  = false;
				AutoInc     = false;
				Order       = 0;
				SqlDataType = DataType.Null;
				Constraints = null;
			}
		}

		/// <summary>Marks a property or field as not a column in the db table for a type.</summary>
		[AttributeUsage(AttributeTargets.Property|AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
		public class IgnoreAttribute :Attribute
		{}

		#endregion

		#region Sqlite.TableInfo
		// ReSharper disable ClassNeverInstantiated.Local,UnusedMember.Local

		/// <summary>Represents the internal type used by sqlite to store information about a table</summary>
		private class TableInfo
		{
			public int    cid        { get; set; }
			public string name       { get; set; }
			public string type       { get; set; }
			public int    notnull    { get; set; }
			public string dflt_value { get; set; }
			public int    pk         { get; set; }
		}

		// ReSharper restore ClassNeverInstantiated.Local,UnusedMember.Local
		#endregion

		#region Sqlite.Transaction

		/// <summary>An RAII class for transactional interaction with a database</summary>
		public class Transaction :IDisposable
		{
			private readonly static object m_lock = new object();
			private static bool m_transaction_in_progress;
			private readonly Database m_db;
			private bool m_completed;
			
			public Transaction(Database db)
			{
				m_db = db;
				m_completed = false;
				lock (m_lock)
				{
					if (m_transaction_in_progress) throw new ApplicationException("Nested transactions are not allowed");
					m_db.Execute("begin transaction");
					m_transaction_in_progress = true;
				}
			}
			public void Commit()
			{
				lock (m_lock)
				{
					if (m_completed) throw new ApplicationException("Transaction already completed");
					m_db.Execute("commit");
					m_completed = true;
					m_transaction_in_progress = false;
				}
			}
			public void Rollback()
			{
				lock (m_lock)
				{
					if (m_completed) throw new ApplicationException("Transaction already completed");
					m_db.Execute("rollback");
					m_completed = true;
					m_transaction_in_progress = false;
				}

			}
			public void Dispose()
			{
				try
				{
					lock (m_lock) if (m_completed) return;
					Rollback();
				}
				finally { m_transaction_in_progress = false; }
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
		//private static readonly IntPtr StaticData  = new IntPtr(0);
		private static readonly IntPtr TransientData = new IntPtr(-1);
		private delegate void sqlite3_destructor_type(IntPtr ptr);

		[DllImport("sqlite3", EntryPoint = "sqlite3_config", CallingConvention=CallingConvention.Cdecl)]
		private static extern Result sqlite3_config(ConfigOption option);

		[DllImport("sqlite3", EntryPoint = "sqlite3_open_v2", CallingConvention=CallingConvention.Cdecl)]
		private static extern Result sqlite3_open_v2(string filepath, out sqlite3 db, int flags, IntPtr zvfs);

		[DllImport("sqlite3", EntryPoint = "sqlite3_busy_timeout", CallingConvention=CallingConvention.Cdecl)]
		private static extern Result sqlite3_busy_timeout(sqlite3 db, int milliseconds);

		[DllImport("sqlite3", EntryPoint = "sqlite3_close", CallingConvention=CallingConvention.Cdecl)]
		private static extern Result sqlite3_close(IntPtr db);

		[DllImport("sqlite3", EntryPoint = "sqlite3_free", CallingConvention=CallingConvention.Cdecl)]
		private static extern void sqlite3_free(IntPtr ptr);

		[DllImport("sqlite3", EntryPoint = "sqlite3_db_handle", CallingConvention=CallingConvention.Cdecl)]
		private static extern IntPtr sqlite3_db_handle_impl(sqlite3_stmt stmt);
		private static sqlite3 sqlite3_db_handle(sqlite3_stmt stmt) { return new sqlite3(sqlite3_db_handle_impl(stmt)); }

		[DllImport("sqlite3", EntryPoint = "sqlite3_limit", CallingConvention=CallingConvention.Cdecl)]
		private static extern int sqlite3_limit(sqlite3 db, Limit limit_category, int new_value);

		[DllImport("sqlite3", EntryPoint = "sqlite3_prepare_v2", CallingConvention=CallingConvention.Cdecl)]
		private static extern Result sqlite3_prepare_v2(sqlite3 db, byte[] sql, int num_bytes, out sqlite3_stmt stmt, IntPtr pzTail);

		[DllImport("sqlite3", EntryPoint = "sqlite3_changes", CallingConvention=CallingConvention.Cdecl)]
		private static extern int sqlite3_changes(sqlite3 db);

		[DllImport("sqlite3", EntryPoint = "sqlite3_last_insert_rowid", CallingConvention=CallingConvention.Cdecl)]
		private static extern long sqlite3_last_insert_rowid(sqlite3 db);

		[DllImport("sqlite3", EntryPoint = "sqlite3_errmsg16", CallingConvention=CallingConvention.Cdecl)]
		private static extern IntPtr sqlite3_errmsg16(sqlite3 db);

		[DllImport("sqlite3", EntryPoint = "sqlite3_reset", CallingConvention=CallingConvention.Cdecl)]
		private static extern Result sqlite3_reset(sqlite3_stmt stmt);

		[DllImport("sqlite3", EntryPoint = "sqlite3_step", CallingConvention=CallingConvention.Cdecl)]
		private static extern Result sqlite3_step(sqlite3_stmt stmt);

		[DllImport("sqlite3", EntryPoint = "sqlite3_finalize", CallingConvention=CallingConvention.Cdecl)]
		private static extern Result sqlite3_finalize(IntPtr stmt);

		[DllImport("sqlite3", EntryPoint = "sqlite3_sql", CallingConvention=CallingConvention.Cdecl)]
		private static extern IntPtr sqlite3_sql_impl(sqlite3_stmt stmt);
		private static string sqlite3_sql(sqlite3_stmt stmt) { return UTF8toStr(sqlite3_sql_impl(stmt)); } // this assumes sqlite3_prepare_v2 was used to create 'stmt'

		[DllImport("sqlite3", EntryPoint = "sqlite3_column_name16", CallingConvention=CallingConvention.Cdecl)]
		private static extern IntPtr sqlite3_column_name16_impl(sqlite3_stmt stmt, int index);
		private static string sqlite3_column_name(sqlite3_stmt stmt, int index) { return Marshal.PtrToStringUni(sqlite3_column_name16_impl(stmt, index)); }

		[DllImport("sqlite3", EntryPoint = "sqlite3_column_count", CallingConvention=CallingConvention.Cdecl)]
		private static extern int sqlite3_column_count(sqlite3_stmt stmt);

		[DllImport("sqlite3", EntryPoint = "sqlite3_column_type", CallingConvention=CallingConvention.Cdecl)]
		private static extern DataType sqlite3_column_type(sqlite3_stmt stmt, int index);

		[DllImport("sqlite3", EntryPoint = "sqlite3_column_int64", CallingConvention=CallingConvention.Cdecl)]
		private static extern Int64 sqlite3_column_int64(sqlite3_stmt stmt, int index);

		[DllImport("sqlite3", EntryPoint = "sqlite3_column_int", CallingConvention=CallingConvention.Cdecl)]
		private static extern Int32 sqlite3_column_int(sqlite3_stmt stmt, int index);

		[DllImport("sqlite3", EntryPoint = "sqlite3_column_double", CallingConvention=CallingConvention.Cdecl)]
		private static extern double sqlite3_column_double(sqlite3_stmt stmt, int index);

		[DllImport("sqlite3", EntryPoint = "sqlite3_column_text16", CallingConvention=CallingConvention.Cdecl)]
		private static extern IntPtr sqlite3_column_text16(sqlite3_stmt stmt, int index);

		[DllImport("sqlite3", EntryPoint = "sqlite3_column_blob", CallingConvention=CallingConvention.Cdecl)]
		private static extern IntPtr sqlite3_column_blob(sqlite3_stmt stmt, int index);

		[DllImport("sqlite3", EntryPoint = "sqlite3_column_bytes", CallingConvention=CallingConvention.Cdecl)]
		private static extern int sqlite3_column_bytes(sqlite3_stmt stmt, int index);


		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_parameter_count", CallingConvention=CallingConvention.Cdecl)]
		private static extern int sqlite3_bind_parameter_count(sqlite3_stmt stmt);

		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_parameter_index", CallingConvention=CallingConvention.Cdecl)]
		private static extern int sqlite3_bind_parameter_index(sqlite3_stmt stmt, string name);

		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_parameter_name", CallingConvention=CallingConvention.Cdecl)]
		private static extern IntPtr sqlite3_bind_parameter_name(sqlite3_stmt stmt, int index);

		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_null", CallingConvention=CallingConvention.Cdecl)]
		private static extern int sqlite3_bind_null(sqlite3_stmt stmt, int index);

		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_int", CallingConvention=CallingConvention.Cdecl)]
		private static extern int sqlite3_bind_int(sqlite3_stmt stmt, int index, int val);

		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_int64", CallingConvention=CallingConvention.Cdecl)]
		private static extern int sqlite3_bind_int64(sqlite3_stmt stmt, int index, long val);

		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_double", CallingConvention=CallingConvention.Cdecl)]
		private static extern int sqlite3_bind_double(sqlite3_stmt stmt, int index, double val);

		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_text16", CallingConvention=CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
		private static extern int sqlite3_bind_text16(sqlite3_stmt stmt, int index, string val, int n, IntPtr destructor_cb);

		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_blob", CallingConvention=CallingConvention.Cdecl)]
		private static extern int sqlite3_bind_blob(sqlite3_stmt stmt, int index, byte[] val, int n, IntPtr destructor_cb);
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
			
			/// <summary>Write trace information to the debug output window</summary>
			[Conditional("SQLITE_TRACE")] public static void WriteLine(string str)
			{
				Debug.WriteLine("[SQLite]:"+str);
			}
		}
		#endregion
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	using System.Data.Linq.SqlClient;
	using System.IO;
	using System.Text.RegularExpressions;
	using common;
	
	[TestFixture] internal static partial class UnitTests
	{
		#region DomTypes
		// ReSharper disable FieldCanBeMadeReadOnly.Local,MemberCanBePrivate.Local,UnusedMember.Local,NotAccessedField.Local,ValueParameterNotUsed
		public enum SomeEnum { One, Two, Three }

		// Tests user types can be mapped to table columns
		private class Custom
		{
			public string Str;
			public Custom()           { Str = "I'm a custom object"; }
			public Custom(string str) { Str = str; }
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

		// Tests a basic default table
		private class DomType0
		{
			public  int    Inc_Key                 { get; set; }
			public  string Inc_Value               { get; set; }
			public  float  Ign_NoGetter            { set { } }
			public  int    Ign_NoSetter            { get { return 42; } }
			private bool   Ign_PrivateProp         { get; set; }
			private int    m_ign_private_field;
			public  short  Ign_PublicField;
			
			public DomType0(){}
			public DomType0(int seed)
			{
				Inc_Key = seed;
				Inc_Value = seed.ToString();
				Ign_NoGetter = seed;
				Ign_PrivateProp = seed != 0;
				m_ign_private_field = seed;
				Ign_PublicField = (short)m_ign_private_field;
			}
			public override string ToString() { return Inc_Key + " " + Inc_Value; }
		}

		// Single primary key table
		[Sqlite.Table(AllByDefault = false)]
		private class DomType1
		{
			[Sqlite.Column(Order= 0, PrimaryKey = true, AutoInc = true, Constraints = "not null")] public int m_key;
			[Sqlite.Column(Order= 1)] protected bool        m_bool;
			[Sqlite.Column(Order= 2)] public sbyte          m_sbyte;
			[Sqlite.Column(Order= 3)] public byte           m_byte;
			[Sqlite.Column(Order= 4)] private char          m_char;
			[Sqlite.Column(Order= 5)] private short         m_short {get;set;}
			[Sqlite.Column(Order= 6)] public ushort         m_ushort {get;set;}
			[Sqlite.Column(Order= 7)] public int            m_int;
			[Sqlite.Column(Order= 8)] public uint           m_uint;
			[Sqlite.Column(Order= 9)] public long           m_int64;
			[Sqlite.Column(Order=10)] public ulong          m_uint64;
			[Sqlite.Column(Order=11)] public decimal        m_decimal;
			[Sqlite.Column(Order=12)] public float          m_float;
			[Sqlite.Column(Order=13)] public double         m_double;
			[Sqlite.Column(Order=14)] public string         m_string;
			[Sqlite.Column(Order=15)] public byte[]         m_buf;
			[Sqlite.Column(Order=16)] public byte[]         m_empty_buf;
			[Sqlite.Column(Order=17)] public Guid           m_guid;
			[Sqlite.Column(Order=18)] public SomeEnum       m_enum;
			[Sqlite.Column(Order=19)] public DateTime       m_datetime;
			[Sqlite.Column(Order=20)] public DateTimeOffset m_dt_offset;
			[Sqlite.Column(Order=21, SqlDataType = Sqlite.DataType.Text)] public Custom m_custom;
			[Sqlite.Column(Order=22)] public int?           m_nullint;
			[Sqlite.Column(Order=23)] public long?          m_nulllong;
			
			// ReSharper disable UnusedAutoPropertyAccessor.Local
			public int Ignored { get; set; }
			// ReSharper restore UnusedAutoPropertyAccessor.Local
			
			public DomType1() {}
			public DomType1(int val)
			{
				m_key       = -1;
				m_bool      = true;
				m_char      = 'X';
				m_sbyte     = 12;
				m_byte      = 12;
				m_short     = 1234;
				m_ushort    = 1234;
				m_int       = 12345678;
				m_uint      = 12345678;
				m_int64     = 1234567890000;
				m_uint64    = 1234567890000;
				m_decimal   = 1234567890.123467890m;
				m_float     = 1.234567f;
				m_double    = 1.2345678987654321;
				m_string    = "string";
				m_buf       = new byte[]{0,1,2,3,4,5,6,7,8,9};
				m_empty_buf = null;
				m_guid      = Guid.NewGuid();
				m_enum      = SomeEnum.One;
				m_datetime  = DateTime.Now;
				m_dt_offset = DateTimeOffset.Now;
				m_custom    = new Custom();
				m_nullint   = 23;
				m_nulllong  = null;
				Ignored     = val;
			}
			public bool Equals(DomType1 other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				if (other.m_key      != m_key               ) return false;
				if (other.m_bool     != m_bool              ) return false;
				if (other.m_sbyte    != m_sbyte             ) return false;
				if (other.m_byte     != m_byte              ) return false;
				if (other.m_char     != m_char              ) return false;
				if (other.m_short    != m_short             ) return false;
				if (other.m_ushort   != m_ushort            ) return false;
				if (other.m_int      != m_int               ) return false;
				if (other.m_uint     != m_uint              ) return false;
				if (other.m_int64    != m_int64             ) return false;
				if (other.m_uint64   != m_uint64            ) return false;
				if (other.m_decimal  != m_decimal           ) return false;
				if (other.m_nullint  != m_nullint           ) return false;
				if (other.m_nulllong != m_nulllong          ) return false;
				if (!Equals(other.m_string, m_string)       ) return false;
				if (!Equals(other.m_buf, m_buf)             ) return false;
				if (!Equals(other.m_empty_buf, m_empty_buf) ) return false;
				if (!Equals(other.m_enum, m_enum)           ) return false;
				if (!other.m_guid.Equals(m_guid)            ) return false;
				if (!other.m_datetime.Equals(m_datetime) || other.m_datetime.Kind != m_datetime.Kind) return false;
				if (!other.m_dt_offset.Equals(m_dt_offset)) return false;
				if (!other.m_custom.Equals(m_custom)        ) return false;
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

		// Tests PKs named at class level, Unicode, non-int type primary keys
		[Sqlite.Table(PrimaryKey = "PK", PKAutoInc = false, FieldBindingFlags = BindingFlags.Public)]
		private class DomType2 :DomType2Base
		{
			public string UniStr; // Should be a column, because of the FieldBindingFlags
			private bool Ign_PrivateField; // Should not be a column because private
			
			public DomType2() {}
			public DomType2(string key, string str)
			{
				PK     = key;
				UniStr = str;
				Ign_PrivateField = true;
				Ign_PrivateField = !Ign_PrivateField;
			}
			public bool Equals(DomType2 other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				if (other.UniStr != UniStr) return false;
				return base.Equals(other);
			}
		}
		private class DomType2Base
		{
			[Sqlite.Column] private int Inc_Explicit; // Should be a column, because explicitly named
			
			// Notice the DOMType2 class indicates this is the primary key from a separate class.
			public string PK { get; protected set; }

			protected DomType2Base()
			{
				Inc_Explicit = -2;
			}
			protected bool Equals(DomType2Base other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				if (other.PK           != PK          ) return false;
				if (other.Inc_Explicit != Inc_Explicit) return false;
				return true;
			}
		}

		// Tests multiple primary keys, and properties in inherited/partial classes
		[Sqlite.Table(Constraints = "primary key (Key1, Key2, Key3)")]
		[Sqlite.IgnoreColumns("Ignored1")]
		public partial class DomType3
		{
			public int    Key1 { get; set; }
			public bool   Key2 { get; set; }
			public string Prop1 { get; set; }
			public float  Prop2 { get; set; }
			public Guid   Prop3 { get; set; }
			public float Ignored1 { get; set; }

			public DomType3(){}
			public DomType3(int key1, bool key2, string key3)
			{
				Key1 = key1;
				Key2 = key2;
				Key3 = key3;
				Prop1 = key1.ToString() + " " + key2.ToString();
				Prop2 = key1;
				Prop3 = Guid.NewGuid();
				Parent1 = key1;
				PropA = key1.ToString() + " " + key2.ToString();
				PropB = (SomeEnum)key1;
				Ignored1 = 1f;
				Ignored2 = 2.0;
			}
			public bool Equals(DomType3 other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				if (other.Key1    != Key1   ) return false;
				if (other.Key2    != Key2   ) return false;
				if (other.Prop1   != Prop1  ) return false;
				if (Math.Abs(other.Prop2 - Prop2) > float.Epsilon) return false;
				if (other.Prop3   != Prop3  ) return false;
				if (other.Parent1 != Parent1) return false;
				if (other.PropA   != PropA  ) return false;
				if (other.PropB   != PropB  ) return false;
				return true;
			}
		}
		[Sqlite.IgnoreColumns("Ignored2")]
		public partial class DomType3 :DomType3Base
		{
			public string   PropA { get; set; }
			public SomeEnum PropB { get; set; }
		}
		public class DomType3Base
		{
			public int Parent1 { get; set; }
			public string Key3 { get; set; }
			public double Ignored2 { get; set; }
		}

		// Tests altering a table
		public class DomType4
		{
			[Sqlite.Column(PrimaryKey = true)] public int Key1 { get; set; }
			public bool   Key2 { get; set; }
			public string Prop1 { get; set; }
			public float  Prop2 { get; set; }
			public Guid   Prop3 { get; set; }
			public int    NewProp { get; set; }
		}
		// ReSharper restore FieldCanBeMadeReadOnly.Local,MemberCanBePrivate.Local,UnusedMember.Local,NotAccessedField.Local,ValueParameterNotUsed
		#endregion

		// The test methods can run in any order, but we only want to do this stuff once
		private static bool TestSqlite_OneTimeOnly_Done = false;
		private static void TestSqlite_OneTimeOnly()
		{
			if (TestSqlite_OneTimeOnly_Done) return;
			TestSqlite_OneTimeOnly_Done = true;

			// Copy sqlite3.dll to test folder
			var src_dir = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\..\..\sqlite\lib\"));
			var src_file = Regex.Replace(Environment.CurrentDirectory, @"(.*)\\(.*?)\\(.*?)$", "sqlite3.$2.$3.dll");
			File.Copy(Path.Combine(src_dir, src_file), Path.Combine(Environment.CurrentDirectory, "sqlite3.dll"), true);

			// Register custom type bind/read methods
			Sqlite.BindFunction.Add(typeof(Custom), Custom.SqliteBind);
			Sqlite.ReadFunction.Add(typeof(Custom), Custom.SqliteRead);
			
			// Use single threading
			Sqlite.Configure(Sqlite.ConfigOption.SingleThread);
		}

		[Test] public static void TestSqlite_DefaultUse()
		{
			TestSqlite_OneTimeOnly();
			
			// Create/Open the database connection
			const string FilePath = "tmpDB.db";
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create|Sqlite.OpenFlags.ReadWrite|Sqlite.OpenFlags.NoMutex))
			{
				// Create a simple table
				db.DropTable<DomType0>();
				db.CreateTable<DomType0>();
				Assert.IsTrue(db.TableExists<DomType0>());
				
				// Check the table
				var table = db.Table<DomType0>();
				Assert.AreEqual(2, table.ColumnCount);
				var column_names = new[]{"Inc_Key", "Inc_Value"};
				using (var q = table.Query("select * from "+table.Name))
				{
					Assert.AreEqual(2, q.ColumnCount);
					Assert.IsTrue(column_names.Contains(q.ColumnName(0)));
					Assert.IsTrue(column_names.Contains(q.ColumnName(1)));
				}
				
				// Create some objects to stick in the table
				var obj1 = new DomType0(5);
				var obj2 = new DomType0(6);
				var obj3 = new DomType0(7);
				
				// Insert stuff
				Assert.AreEqual(1, table.Insert(obj1));
				Assert.AreEqual(1, table.Insert(obj2));
				Assert.AreEqual(1, table.Insert(obj3));
				Assert.AreEqual(3, table.RowCount);
				
				string sql_count = "select count(*) from "+table.Name;
				using (var q = table.Query(sql_count))
					Assert.AreEqual(sql_count, q.SqlString);
			}
		}
		[Test] public static void TestSqlite_TypicalUse()
		{
			TestSqlite_OneTimeOnly();
			
			// Create/Open the database connection
			const string FilePath = "tmpDB.db";
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create|Sqlite.OpenFlags.ReadWrite|Sqlite.OpenFlags.NoMutex))
			{
				// Create a table
				db.DropTable<DomType1>();
				db.CreateTable<DomType1>();
				Assert.IsTrue(db.TableExists<DomType1>());
				
				// Check the table
				var table = db.Table<DomType1>();
				Assert.AreEqual(24, table.ColumnCount);
				using (var q = table.Query("select * from "+table.Name))
				{
					Assert.AreEqual(24, q.ColumnCount);
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
					Assert.AreEqual("m_decimal"   ,q.ColumnName(11));
					Assert.AreEqual("m_float"     ,q.ColumnName(12));
					Assert.AreEqual("m_double"    ,q.ColumnName(13));
					Assert.AreEqual("m_string"    ,q.ColumnName(14));
					Assert.AreEqual("m_buf"       ,q.ColumnName(15));
					Assert.AreEqual("m_empty_buf" ,q.ColumnName(16));
					Assert.AreEqual("m_guid"      ,q.ColumnName(17));
					Assert.AreEqual("m_enum"      ,q.ColumnName(18));
					Assert.AreEqual("m_datetime"  ,q.ColumnName(19));
					Assert.AreEqual("m_dt_offset" ,q.ColumnName(20));
					Assert.AreEqual("m_custom"    ,q.ColumnName(21));
					Assert.AreEqual("m_nullint"   ,q.ColumnName(22));
					Assert.AreEqual("m_nulllong"  ,q.ColumnName(23));
				}
				
				// Create some objects to stick in the table
				var obj1 = new DomType1(5);
				var obj2 = new DomType1(6);
				var obj3 = new DomType1(7);
				obj2.m_datetime = obj2.m_datetime.ToLocalTime();
				obj3.m_datetime = obj3.m_datetime.ToUniversalTime();
				obj2.m_dt_offset = DateTimeOffset.UtcNow;
				
				// Insert stuff
				Assert.AreEqual(1, table.Insert(obj1));
				Assert.AreEqual(1, table.Insert(obj2));
				Assert.AreEqual(1, table.Insert(obj3));
				Assert.AreEqual(3, table.RowCount);
				
				// Check Get() throws and Find() returns null if not found
				Assert.IsNull(table.Find(0));
				Sqlite.Exception err = null;
				try { table.Get(4); } catch (Sqlite.Exception ex) { err = ex; }
				Assert.IsTrue(err != null && err.Result == Sqlite.Result.NotFound);
				
				// Get stuff and check it's the same
				var OBJ1 = table.Get(obj1.m_key);
				var OBJ2 = table.Get(obj2.m_key);
				var OBJ3 = table.Get(obj3.m_key);
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
				Assert.AreEqual(1, table.Insert(obj3));
				OBJ3 = table.Get(obj3.m_key);
				Assert.IsNotNull(OBJ3);
				Assert.IsTrue(obj3.Equals(OBJ3));
				
				// Update the column value for all rows
				obj1.m_byte = obj2.m_byte = obj3.m_byte = 0xAB;
				Assert.AreEqual(3, table.UpdateAll("m_byte", (byte)0xAB));
				
				// Enumerate objects
				var objs = table.Select(x => x).ToArray();
				Assert.AreEqual(3, objs.Length);
				Assert.IsTrue(obj1.Equals(objs[0]));
				Assert.IsTrue(obj2.Equals(objs[1]));
				Assert.IsTrue(obj3.Equals(objs[2]));
				
				// Linq expressions
				objs = (from a in table where a.m_string == "I've been modified" select a).ToArray();
				Assert.AreEqual(1, objs.Length);
				Assert.IsTrue(obj2.Equals(objs[0]));
			}
		}
		[Test] public static void TestSqlite_MultiplePks()
		{
			TestSqlite_OneTimeOnly();
			
			// Create/Open the database connection
			const string FilePath = "tmpDB.db";
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create|Sqlite.OpenFlags.ReadWrite|Sqlite.OpenFlags.NoMutex))
			{
				// Create a table
				db.DropTable<DomType3>();
				db.CreateTable<DomType3>();
				Assert.IsTrue(db.TableExists<DomType3>());
				
				// Check the table
				var table = db.Table<DomType3>();
				Assert.AreEqual(9, table.ColumnCount);
				using (var q = table.Query("select * from "+table.Name))
				{
					var cols = q.ColumnNames.ToList();
					Assert.AreEqual(9, q.ColumnCount);
					Assert.IsTrue(cols.Contains("Key1"));
					Assert.IsTrue(cols.Contains("Key2"));
					Assert.IsTrue(cols.Contains("Key3"));
					Assert.IsTrue(cols.Contains("Prop1"));
					Assert.IsTrue(cols.Contains("Prop2"));
					Assert.IsTrue(cols.Contains("Prop3"));
					Assert.IsTrue(cols.Contains("PropA"));
					Assert.IsTrue(cols.Contains("PropB"));
					Assert.IsTrue(cols.Contains("Parent1"));
				}
				
				// Create some stuff
				var obj1 = new DomType3(1, false, "first");
				var obj2 = new DomType3(1, true , "first");
				var obj3 = new DomType3(2, false, "first");
				var obj4 = new DomType3(2, true , "first");
				
				// Insert it an check they're there
				Assert.AreEqual(1, table.Insert(obj1));
				Assert.AreEqual(1, table.Insert(obj2));
				Assert.AreEqual(1, table.Insert(obj3));
				Assert.AreEqual(1, table.Insert(obj4));
				Assert.AreEqual(4, table.RowCount);
				
				Assert.Throws<ArgumentException>(()=>table.Get(obj1.Key1, obj1.Key2));
				
				var OBJ1 = table.Get(obj1.Key1, obj1.Key2, obj1.Key3);
				var OBJ2 = table.Get(obj2.Key1, obj2.Key2, obj2.Key3);
				var OBJ3 = table.Get(obj3.Key1, obj3.Key2, obj3.Key3);
				var OBJ4 = table.Get(obj4.Key1, obj4.Key2, obj4.Key3);
				Assert.IsTrue(obj1.Equals(OBJ1));
				Assert.IsTrue(obj2.Equals(OBJ2));
				Assert.IsTrue(obj3.Equals(OBJ3));
				Assert.IsTrue(obj4.Equals(OBJ4));
				
				// Check insert collisions
				obj1.Prop1 = "I've been modified";
				{
					Sqlite.Exception err = null;
					try { table.Insert(obj1); } catch (Sqlite.Exception ex) { err = ex; }
					Assert.IsTrue(err != null && err.Result == Sqlite.Result.Constraint);
					OBJ1 = table.Get(obj1.Key1, obj1.Key2, obj1.Key3);
					Assert.IsNotNull(OBJ1);
					Assert.IsFalse(obj1.Equals(OBJ1));
				}
				{
					Sqlite.Exception err = null;
					try { table.Insert(obj1, Sqlite.OnInsertConstraint.Ignore); } catch (Sqlite.Exception ex) { err = ex; }
					Assert.IsNull(err);
					OBJ1 = table.Get(obj1.Key1, obj1.Key2, obj1.Key3);
					Assert.IsNotNull(OBJ1);
					Assert.IsFalse(obj1.Equals(OBJ1));
				}
				{
					Sqlite.Exception err = null;
					try { table.Insert(obj1, Sqlite.OnInsertConstraint.Replace); } catch (Sqlite.Exception ex) { err = ex; }
					Assert.IsNull(err);
					OBJ1 = table.Get(obj1.Key1, obj1.Key2, obj1.Key3);
					Assert.IsNotNull(OBJ1);
					Assert.IsTrue(obj1.Equals(OBJ1));
				}
				
				// Update in a multiple pk table
				obj2.PropA = "I've also been modified";
				Assert.AreEqual(1, table.Update(obj2));
				OBJ2 = table.Get(obj2.Key1, obj2.Key2, obj2.Key3);
				Assert.IsNotNull(OBJ2);
				Assert.IsTrue(obj2.Equals(OBJ2));
				
				// Delete in a multiple pk table
				var keys = Sqlite.PrimaryKeys(obj3);
				Assert.AreEqual(1, table.DeleteByKey(keys));
				OBJ3 = table.Find(obj3.Key1, obj3.Key2, obj3.Key3);
				Assert.IsNull(OBJ3);
			}
		}
		[Test] public static void TestSqlite_Unicode()
		{
			TestSqlite_OneTimeOnly();
			
			// Create/Open the database connection
			const string FilePath = "tmpDB.db";
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create|Sqlite.OpenFlags.ReadWrite|Sqlite.OpenFlags.NoMutex))
			{
				// Create a table
				db.DropTable<DomType2>();
				db.CreateTable<DomType2>();
				Assert.IsTrue(db.TableExists<DomType2>());
				
				// Check the table
				var table = db.Table<DomType2>();
				Assert.AreEqual(3, table.ColumnCount);
				var column_names = new[]{"PK", "UniStr", "Inc_Explicit"};
				using (var q = table.Query("select * from "+table.Name))
				{
					Assert.AreEqual(3        ,q.ColumnCount);
					Assert.AreEqual("PK"     ,q.ColumnName(0));
					Assert.IsTrue(column_names.Contains(q.ColumnName(0)));
					Assert.IsTrue(column_names.Contains(q.ColumnName(1)));
				}
				
				// Insert some stuff and check it stores/reads back ok
				var obj1 = new DomType2("123", "€€€€");
				var obj2 = new DomType2("abc", "⽄畂卧湥敳慈摮敬⡲㐲ㄴ⤷›慃獵摥戠㩹樠癡⹡慬杮吮牨睯扡敬›潣⹭湩牴湡汥洮扯汩扥汵敬⹴牃獡剨灥牯楴杮匫獥楳湯瑓牡䕴捸灥楴湯›敓獳潩⁮瑓牡整㩤眠楚塄婐桹㐰慳扲汬穷䌰㡶啩扁搶睄畎䐱䭡䝭牌夳䉡獁െ");
				Assert.AreEqual(1, table.Insert(obj1));
				Assert.AreEqual(1, table.Insert(obj2));
				Assert.AreEqual(2, table.RowCount);
				var OBJ1 = table.Get(obj1.PK);
				var OBJ2 = table.Get(obj2.PK);
				Assert.IsTrue(obj1.Equals(OBJ1));
				Assert.IsTrue(obj2.Equals(OBJ2));
				
				// Update Unicode stuff
				obj2.UniStr = "獁㩹獁";
				Assert.AreEqual(1, table.Update(obj2));
				OBJ2 = table.Get(obj2.PK);
				Assert.IsTrue(obj2.Equals(OBJ2));
			}
		}
		[Test] public static void TestSqlite_Transactions()
		{
			TestSqlite_OneTimeOnly();
			
			// Create/Open the database connection
			const string FilePath = "tmpDB.db";
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create|Sqlite.OpenFlags.ReadWrite|Sqlite.OpenFlags.NoMutex))
			{
				// Create a table
				db.DropTable<DomType1>();
				db.CreateTable<DomType1>();
				Assert.IsTrue(db.TableExists<DomType1>());
				var table = db.Table<DomType1>();
				
				// Create objects
				var objs = Enumerable.Range(0,10).Select(i => new DomType1(i)).ToList();
				
				// Add objects
				try { using (new Sqlite.Transaction(db))
				{
					foreach (var x in objs) table.Insert(x);
					throw new Exception("aborting insert");
				}} catch {}
				Assert.AreEqual(0, table.RowCount);
				using (new Sqlite.Transaction(db))
				{
					foreach (var x in objs) table.Insert(x);
				}
				Assert.AreEqual(0, table.RowCount);
				using (var tranny = new Sqlite.Transaction(db))
				{
					foreach (var x in objs) table.Insert(x);
					tranny.Commit();
				}
				Assert.AreEqual(objs.Count, table.RowCount);
			}
		}
		[Test] public static void TestSqlite_RuntimeTypes()
		{
			TestSqlite_OneTimeOnly();
			
			// Create/Open the database connection
			const string FilePath = "tmpDB.db";
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create|Sqlite.OpenFlags.ReadWrite|Sqlite.OpenFlags.NoMutex))
			{
				// Create a table
				db.DropTable<DomType1>();
				db.CreateTable<DomType1>();
				Assert.IsTrue(db.TableExists<DomType1>());
				var table = db.Table(typeof(DomType1));
				
				// Create objects
				var objs = Enumerable.Range(0,10).Select(i => new DomType1(i)).ToList();
				foreach (var x in objs)
					Assert.AreEqual(1, table.Insert(x)); // insert without compile-time type info
				
				objs[5].m_string = "I am number 5";
				Assert.AreEqual(1, table.Update(objs[5]));
				
				var OBJS = table.Cast<DomType1>().Select(x => x).ToList();
				for (int i = 0, iend = objs.Count; i != iend; ++i)
					Assert.IsTrue(objs[i].Equals(OBJS[i]));
			}
		}
		[Test] public static void TestSqlite_AlterTable()
		{
			TestSqlite_OneTimeOnly();
			
			// Create/Open the database connection
			const string FilePath = "tmpDB.db";
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create|Sqlite.OpenFlags.ReadWrite|Sqlite.OpenFlags.NoMutex))
			{
				// Create a table
				db.DropTable<DomType3>();
				db.CreateTable<DomType3>();
				Assert.IsTrue(db.TableExists<DomType3>());
				
				// Check the table
				var table3 = db.Table<DomType3>();
				Assert.AreEqual(9, table3.ColumnCount);
				using (var q = table3.Query("select * from "+table3.Name))
				{
					var cols = q.ColumnNames.ToList();
					Assert.AreEqual(9, q.ColumnCount);
					Assert.AreEqual(9, cols.Count);
					Assert.IsTrue(cols.Contains("Key1"));
					Assert.IsTrue(cols.Contains("Key2"));
					Assert.IsTrue(cols.Contains("Key3"));
					Assert.IsTrue(cols.Contains("Prop1"));
					Assert.IsTrue(cols.Contains("Prop2"));
					Assert.IsTrue(cols.Contains("Prop3"));
					Assert.IsTrue(cols.Contains("PropA"));
					Assert.IsTrue(cols.Contains("PropB"));
					Assert.IsTrue(cols.Contains("Parent1"));
				}
				
				// Create some stuff
				var obj1 = new DomType3(1, false, "first");
				var obj2 = new DomType3(1, true , "first");
				var obj3 = new DomType3(2, false, "first");
				var obj4 = new DomType3(2, true , "first");
				
				// Insert it an check they're there
				Assert.AreEqual(1, table3.Insert(obj1));
				Assert.AreEqual(1, table3.Insert(obj2));
				Assert.AreEqual(1, table3.Insert(obj3));
				Assert.AreEqual(1, table3.Insert(obj4));
				Assert.AreEqual(4, table3.RowCount);
				
				// Rename the table
				db.DropTable<DomType4>();
				db.RenameTable<DomType3>("DomType4", false);
				
				// Alter the table to DOMType4
				db.AlterTable<DomType4>();
				Assert.IsTrue(db.TableExists<DomType4>());
				
				// Check the table
				var table4 = db.Table<DomType4>();
				Assert.AreEqual(6, table4.ColumnCount);
				using (var q = table4.Query("select * from "+table4.Name))
				{
					var cols = q.ColumnNames.ToList();
					Assert.AreEqual(10, q.ColumnCount);
					Assert.AreEqual(10, cols.Count);
					Assert.IsTrue(cols.Contains("Key1"));
					Assert.IsTrue(cols.Contains("Key2"));
					Assert.IsTrue(cols.Contains("Key3"));
					Assert.IsTrue(cols.Contains("Prop1"));
					Assert.IsTrue(cols.Contains("Prop2"));
					Assert.IsTrue(cols.Contains("Prop3"));
					Assert.IsTrue(cols.Contains("PropA"));
					Assert.IsTrue(cols.Contains("PropB"));
					Assert.IsTrue(cols.Contains("Parent1"));
					Assert.IsTrue(cols.Contains("NewProp"));
				}
			}
		}
		[Test] public static void TestSqlite_ExprTree()
		{
			TestSqlite_OneTimeOnly();
			
			// Create/Open the database connection
			const string FilePath = "tmpDB.db";
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create|Sqlite.OpenFlags.ReadWrite|Sqlite.OpenFlags.NoMutex))
			{
				// Create a simple table
				db.DropTable<DomType0>();
				db.CreateTable<DomType0>();
				Assert.IsTrue(db.TableExists<DomType0>());
				
				// Check the table
				var table = db.Table<DomType0>();
				Assert.AreEqual(2, table.ColumnCount);
				var column_names = new[]{"Inc_Key", "Inc_Value"};
				using (var q = table.Query("select * from "+table.Name))
				{
					Assert.AreEqual(2, q.ColumnCount);
					Assert.IsTrue(column_names.Contains(q.ColumnName(0)));
					Assert.IsTrue(column_names.Contains(q.ColumnName(1)));
				}
				
				// Insert stuff
				Assert.AreEqual(1, table.Insert(new DomType0(4)));
				Assert.AreEqual(1, table.Insert(new DomType0(1)));
				Assert.AreEqual(1, table.Insert(new DomType0(0)));
				Assert.AreEqual(1, table.Insert(new DomType0(5)));
				Assert.AreEqual(1, table.Insert(new DomType0(7)));
				Assert.AreEqual(1, table.Insert(new DomType0(9)));
				Assert.AreEqual(1, table.Insert(new DomType0(6)));
				Assert.AreEqual(1, table.Insert(new DomType0(3)));
				Assert.AreEqual(1, table.Insert(new DomType0(8)));
				Assert.AreEqual(1, table.Insert(new DomType0(2)));
				Assert.AreEqual(10, table.RowCount);
				
				string sql_count = "select count(*) from "+table.Name;
				using (var q = table.Query(sql_count))
					Assert.AreEqual(sql_count, q.SqlString);
				
				// Do some expression tree queries
				{// Count clause
					var q = table.Count(x => (x.Inc_Key % 3) == 0);
					Assert.AreEqual(4, q);
				}
				{// Where clause
					var q = from x in table where x.Inc_Key % 2 == 1 select x;
					var list = q.ToList();
					Assert.AreEqual(5, list.Count);
				}
				{// Where clause with 'like' method calling 'RowCount'
					var q = (from x in table where SqlMethods.Like(x.Inc_Value, "5") select x).RowCount;
					Assert.AreEqual(1, q);
				}
				{// Contains clause
					var set = new List<string>{"2","4","8"};
					var q = from x in table where set.Contains(x.Inc_Value) select x;
					var list = q.ToList();
					Assert.AreEqual(3, list.Count);
					Assert.AreEqual(4, list[0].Inc_Key);
					Assert.AreEqual(8, list[1].Inc_Key);
					Assert.AreEqual(2, list[2].Inc_Key);
				}
				{// NOT Contains clause
					var set = new List<string>{"2","4","8","5","9"};
					var q = from x in table where set.Contains(x.Inc_Value) == false select x;
					var list = q.ToList();
					Assert.AreEqual(5, list.Count);
					Assert.AreEqual(1, list[0].Inc_Key);
					Assert.AreEqual(0, list[1].Inc_Key);
					Assert.AreEqual(7, list[2].Inc_Key);
					Assert.AreEqual(6, list[3].Inc_Key);
					Assert.AreEqual(3, list[4].Inc_Key);
				}
				{// NOT Contains clause
					var set = new List<string>{"2","4","8","5","9"};
					var q = from x in table where !set.Contains(x.Inc_Value) select x;
					var list = q.ToList();
					Assert.AreEqual(5, list.Count);
					Assert.AreEqual(1, list[0].Inc_Key);
					Assert.AreEqual(0, list[1].Inc_Key);
					Assert.AreEqual(7, list[2].Inc_Key);
					Assert.AreEqual(6, list[3].Inc_Key);
					Assert.AreEqual(3, list[4].Inc_Key);
				}
				{// OrderBy clause
					var q = from x in table orderby x.Inc_Key descending select x;
					var list = q.ToList();
					Assert.AreEqual(10, list.Count);
					for (int i = 0; i != 10; ++i)
						Assert.AreEqual(9-i, list[i].Inc_Key);
				}
				{// Where and OrderBy clause
					var q = from x in table where ((x.Inc_Key * 4 + 2 - 1) / 3) >= 5 orderby x.Inc_Value select x;
					var list = q.ToList();
					Assert.AreEqual(6, list.Count);
					for (int i = 0; i != 6; ++i)
						Assert.AreEqual(4+i, list[i].Inc_Key);
				}
				{// Skip
					var q = table.Where(x => x.Inc_Key <= 5).Skip(2);
					var list = q.ToList();
					Assert.AreEqual(4, list.Count);
					Assert.AreEqual(0, list[0].Inc_Key);
					Assert.AreEqual(5, list[1].Inc_Key);
					Assert.AreEqual(3, list[2].Inc_Key);
					Assert.AreEqual(2, list[3].Inc_Key);
				}
				{// Take
					var q = table.Where(x => x.Inc_Key >= 5).Take(2);
					var list = q.ToList();
					Assert.AreEqual(2, list.Count);
					Assert.AreEqual(5, list[0].Inc_Key);
					Assert.AreEqual(7, list[1].Inc_Key);
				}
				{// Skip and Take
					var q = table.Where(x => x.Inc_Key >= 5).Skip(2).Take(2);
					var list = q.ToList();
					Assert.AreEqual(2, list.Count);
					Assert.AreEqual(9, list[0].Inc_Key);
					Assert.AreEqual(6, list[1].Inc_Key);
				}
				{// Null test
					var q = from x in table where x.Inc_Value != null select x;
					var list = q.ToList();
					Assert.AreEqual(10, list.Count);
				}
				{// Type conversions
					var q = from x in table where (float)x.Inc_Key > 2.5f && (float)x.Inc_Key < 7.5f select x;
					var list = q.ToList();
					Assert.AreEqual(5, list.Count);
					Assert.AreEqual(4, list[0].Inc_Key);
					Assert.AreEqual(5, list[1].Inc_Key);
					Assert.AreEqual(7, list[2].Inc_Key);
					Assert.AreEqual(6, list[3].Inc_Key);
					Assert.AreEqual(3, list[4].Inc_Key);
				}
			}
		}
	}
}
#endif