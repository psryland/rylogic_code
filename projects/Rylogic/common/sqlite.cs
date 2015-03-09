//#define SQLITE_TRACE
//#define WP8_SQLITE
#if !MONOTOUCH
#define COMPILED_LAMBDAS
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

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
//
// Thread Safety:
//  Sqlite is thread safe if the dll has been built with SQLITE_THREADSAFE
//  defined != 0 and the db connection is opened with OpenFlags.FullMutex.
//  For performance however, sqlite should be used in a single-threaded way.

namespace pr.common
{
	/// <summary>A simple sqlite .net ORM</summary>
	public static class Sqlite
	{
		#if WP8_SQLITE
		public class Dll :Wp8Binding {}
		#else
		public class Dll :NativeBinding {}
		#endif

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
			SAVEPOINT           = 32,   // Operation       Savepoint Name
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

		#region Global methods

		/// <summary>Callback type for sqlite3_update_hook</summary>
		/// <param name="ctx">A copy of the third argument to sqlite3_update_hook()</param>
		/// <param name="change_type">One of SQLITE_INSERT, SQLITE_DELETE, or SQLITE_UPDATE</param>
		/// <param name="db_name">The name of the affected database</param>
		/// <param name="table_name">The name of the table containing the changed item</param>
		/// <param name="row_id">The row id of the changed row</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void UpdateHookCB(IntPtr ctx, int change_type, string db_name, string table_name, long row_id);

		/// <summary>
		/// Sets a global configuration option for sqlite. Must be called prior
		/// to initialisation or after shutdown of sqlite. Initialisation happens
		/// implicitly when Open is called.</summary>
		public static void Configure(ConfigOption option)
		{
			Dll.Config(option);
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
				type == typeof(int[]))
				return DataType.Blob;

			throw new NotSupportedException(
				"No default type mapping for type "+type.Name+"\n" +
				"Custom data types should specify a value for the " +
				"'SqlDataType' property in their ColumnAttribute");
		}

		/// <summary>A helper for gluing strings together</summary>
		[DebuggerStepThrough] public static string Sql(params object[] parts)
		{
			m_sql_cached_sb = m_sql_cached_sb ?? new StringBuilder();
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
			//  name='DomType0' into this: name=' DomType0 ';
			foreach (var p in parts)
				sb.Append(p);
		}
		[ThreadStatic] private static StringBuilder m_sql_cached_sb; // no initialised for thread statics

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
			if (meta.NonAutoIncs.Length == 0) throw Exception.New(Result.Misuse, "Cannot insert an item no fields (or with auto increment fields only)");
			return Sql(
				"insert ",cons," into ",meta.Name," (",
				string.Join(",",meta.NonAutoIncs.Select(c => c.NameBracketed)),
				") values (",
				string.Join(",",meta.NonAutoIncs.Select(c => "?")),
				")");
		}

		/// <summary>Returns the sql string for the update command for type 'type'</summary>
		public static string SqlUpdateCmd(Type type)
		{
			var meta = TableMetaData.GetMetaData(type);
			if (meta.Pks.Length    == 0) throw Exception.New(Result.Misuse, "Cannot update an item with no primary keys since it cannot be identified");
			if (meta.NonPks.Length == 0) throw Exception.New(Result.Misuse, "Cannot update an item with no non-primary key fields since there are no fields to change");
			return Sql(
				"update ",meta.Name," set ",
				string.Join(",", meta.NonPks.Select(x => x.NameBracketed+" = ?")),
				" where ",
				string.Join(" and ", meta.Pks.Select(x => x.NameBracketed+" = ?"))
				);
		}

		/// <summary>Returns the sql string for the delete command for type 'type'</summary>
		public static string SqlDeleteCmd(Type type)
		{
			var meta = TableMetaData.GetMetaData(type);
			if (meta.Pks.Length == 0) throw Exception.New(Result.Misuse, "Cannot delete an item with no primary keys since it cannot be identified");
			return Sql("delete from ",meta.Name," where ",meta.PkConstraints());
		}

		/// <summary>Returns the sql string for the get command for type 'type'</summary>
		public static string SqlGetCmd(Type type)
		{
			var meta = TableMetaData.GetMetaData(type);
			if (meta.Pks.Length == 0) throw Exception.New(Result.Misuse, "Cannot get an item with no primary keys since it cannot be identified");
			return Sql("select * from ",meta.Name," where ",meta.PkConstraints());
		}

		/// <summary>Returns the sql string for the get command for type 'type'</summary>
		public static string SqlSelectAllCmd(Type type)
		{
			var meta = TableMetaData.GetMetaData(type);
			return Sql("select * from ",meta.Name);
		}

		/// <summary>Compiles an sql string into an sqlite3 statement</summary>
		private static sqlite3_stmt Compile(Database db, string sql_string)
		{
			Trace.WriteLine(ETrace.Query, string.Format("Compiling sql string '{0}'", sql_string));
			return Dll.Prepare(db.Handle, sql_string);
		}

		#endregion

		#region Logging

		/// <summary>Log receiver interface</summary>
		public interface ILog
		{
			/// <summary>Log an exception with the specified sender, message and exception details.</summary>
			void Exception(object sender, System.Exception e, string message);

			/// <summary>Log an info message with the specified sender and formatted message</summary>
			void Info(object sender, string message);

			/// <summary>Log a debug message with the specified sender and formatted message</summary>
			void Debug(object sender, string message);
		}

		/// <summary>The logger that this orm writes to</summary>
		public static ILog Log
		{
			get { return m_log ?? (m_log = new NullLog()); }
			set { m_log = value; }
		}
		private static ILog m_log;
		private class NullLog :ILog
		{
			public void Exception(object sender, System.Exception e, string message) {}
			public void Info(object sender, string message) {}
			public void Debug(object sender, string message) {}
		}

		#endregion

		#region Binding - Assigning parameters in a prepared statement

		/// <summary>Methods for binding data to an sqlite prepared statement</summary>
		public static class Bind
		{
			public static void Null(sqlite3_stmt stmt, int idx)
			{
				Dll.BindNull(stmt, idx);
			}
			public static void Bool(sqlite3_stmt stmt, int idx, object value) // Could use Convert.ToXYZ() here, but straight casts are faster
			{
				Dll.BindInt(stmt, idx, (bool)value ? 1 : 0);
			}
			public static void SByte(sqlite3_stmt stmt, int idx, object value)
			{
				Dll.BindInt(stmt, idx, (sbyte)value);
			}
			public static void Byte(sqlite3_stmt stmt, int idx, object value)
			{
				Dll.BindInt(stmt, idx, (byte)value);
			}
			public static void Char(sqlite3_stmt stmt, int idx, object value)
			{
				Dll.BindInt(stmt, idx, (Char)value);
			}
			public static void Short(sqlite3_stmt stmt, int idx, object value)
			{
				Dll.BindInt(stmt, idx, (short)value);
			}
			public static void UShort(sqlite3_stmt stmt, int idx, object value)
			{
				Dll.BindInt(stmt, idx, (ushort)value);
			}
			public static void Int(sqlite3_stmt stmt, int idx, object value)
			{
				Dll.BindInt(stmt, idx, (int)value);
			}
			public static void UInt(sqlite3_stmt stmt, int idx, object value)
			{
				Dll.BindInt64(stmt, idx, (uint)value);
			}
			public static void Long(sqlite3_stmt stmt, int idx, object value)
			{
				Dll.BindInt64(stmt, idx, (long)value);
			}
			public static void ULong(sqlite3_stmt stmt, int idx, object value)
			{
				Dll.BindInt64(stmt, idx, Convert.ToInt64(value)); // Use Convert to trap overflow exceptions
			}
			public static void Decimal(sqlite3_stmt stmt, int idx, object value)
			{
				var dec = (decimal)value;
				Dll.BindText(stmt, idx, dec.ToString(CultureInfo.InvariantCulture));
			}
			public static void Float(sqlite3_stmt stmt, int idx, object value)
			{
				Dll.BindDouble(stmt, idx, (float)value);
			}
			public static void Double(sqlite3_stmt stmt, int idx, object value)
			{
				Dll.BindDouble(stmt, idx, (double)value);
			}
			public static void Text(sqlite3_stmt stmt, int idx, object value)
			{
				if (value == null) Null(stmt, idx);
				else Dll.BindText(stmt, idx, (string)value);
			}
			public static void ByteArray(sqlite3_stmt stmt, int idx, object value)
			{
				if (value == null) Null(stmt, idx);
				else Dll.BindBlob(stmt, idx, (byte[])value, ((byte[])value).Length);
			}
			public static void IntArray(sqlite3_stmt stmt, int idx, object value)
			{
				if (value == null) Null(stmt, idx);
				else
				{
					var iarr = (int[])value;
					var barr = new byte[Buffer.ByteLength(iarr)];
					Buffer.BlockCopy(iarr, 0, barr, 0, barr.Length);
					Dll.BindBlob(stmt, idx, barr, barr.Length);
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
					if (dto.Offset != TimeSpan.Zero) throw new Exception("Only UTC DateTimeOffset values can be stored");
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
					Add(typeof(Guid)           ,Guid          );
					Add(typeof(DateTimeOffset) ,DateTimeOffset); // Note: DateTime deliberately not supported, use DateTimeOffset instead
					Add(typeof(Color)          ,Color         );
				}
			}

			/// <summary>
			/// A lookup table from ClrType to binding function.
			/// Users can add custom types and binding functions to this map if needed</summary>
			public static readonly Map FunctionMap = new Map();

			/// <summary>Returns a bind function appropriate for 'type'</summary>
			public static Func FuncFor(Type type)
			{
				try
				{
					var is_nullable = Nullable.GetUnderlyingType(type) != null;
					if (!is_nullable)
					{
						var bind_type = type.IsEnum ? Enum.GetUnderlyingType(type) : type;
						return FunctionMap[bind_type];
					}
					else // nullable type, wrap the binding functions
					{
						var base_type = Nullable.GetUnderlyingType(type);
						var is_enum   = base_type.IsEnum;
						var bind_type = is_enum ? Enum.GetUnderlyingType(base_type) : base_type;
						return (stmt,idx,obj) =>
							{
								if (obj != null) FunctionMap[bind_type](stmt, idx, obj);
								else Dll.BindNull(stmt, idx);
							};
					}
				}
				catch (KeyNotFoundException) {}
				throw new KeyNotFoundException(
					"A bind function was not found for type '"+type.Name+"'\r\n" +
					"Custom types need to register a Bind/Read function in the " +
					"BindFunction/ReadFunction map before being used");
			}
		}

		#endregion

		#region Reading - Reading columns from a query result into a Clr Object

		/// <summary>Methods for reading data from an sqlite prepared statement</summary>
		public static class Read
		{
			public static object Bool(sqlite3_stmt stmt, int idx)
			{
				return Dll.ColumnInt(stmt, idx) != 0; // Sqlite returns 0 if this column is null
			}
			public static object SByte(sqlite3_stmt stmt, int idx)
			{
				return (sbyte)Dll.ColumnInt(stmt, idx); // Sqlite returns 0 if this column is null
			}
			public static object Byte(sqlite3_stmt stmt, int idx)
			{
				return (byte)Dll.ColumnInt(stmt, idx); // Sqlite returns 0 if this column is null
			}
			public static object Char(sqlite3_stmt stmt, int idx)
			{
				return (char)Dll.ColumnInt(stmt, idx); // Sqlite returns 0 if this column is null
			}
			public static object Short(sqlite3_stmt stmt, int idx)
			{
				return (short)Dll.ColumnInt(stmt, idx); // Sqlite returns 0 if this column is null
			}
			public static object UShort(sqlite3_stmt stmt, int idx)
			{
				return (ushort)Dll.ColumnInt(stmt, idx); // Sqlite returns 0 if this column is null
			}
			public static object Int(sqlite3_stmt stmt, int idx)
			{
				return Dll.ColumnInt(stmt, idx); // Sqlite returns 0 if this column is null
			}
			public static object UInt(sqlite3_stmt stmt, int idx)
			{
				return (uint)Dll.ColumnInt64(stmt, idx); // Sqlite returns 0 if this column is null
			}
			public static object Long(sqlite3_stmt stmt, int idx)
			{
				return Dll.ColumnInt64(stmt, idx); // Sqlite returns 0 if this column is null
			}
			public static object ULong(sqlite3_stmt stmt, int idx)
			{
				return (ulong)Dll.ColumnInt64(stmt, idx); // Sqlite returns 0 if this column is null
			}
			public static object Decimal(sqlite3_stmt stmt, int idx)
			{
				var str = Dll.ColumnString(stmt, idx);
				return str.Length != 0 ? decimal.Parse(str) : 0m;
			}
			public static object Float(sqlite3_stmt stmt, int idx)
			{
				return (float)Dll.ColumnDouble(stmt, idx); // Sqlite returns 0.0 if this column is null
			}
			public static object Double(sqlite3_stmt stmt, int idx)
			{
				return Dll.ColumnDouble(stmt, idx); // Sqlite returns 0.0 if this column is null
			}
			public static object Text(sqlite3_stmt stmt, int idx)
			{
				return Dll.ColumnString(stmt, idx);
			}
			public static object ByteArray(sqlite3_stmt stmt, int idx)
			{
				IntPtr ptr; int len;
				Dll.ColumnBlob(stmt, idx, out ptr, out len);

				// Copy the blob out of the db
				byte[] blob = new byte[len];
				if (len != 0) Marshal.Copy(ptr, blob, 0, blob.Length);
				return blob;
			}
			public static object IntArray(sqlite3_stmt stmt, int idx)
			{
				IntPtr ptr; int len;
				Dll.ColumnBlob(stmt, idx, out ptr, out len);
				if ((len % sizeof(int)) != 0)
					throw Exception.New(Result.Corrupt, "Blob data is not an even multiple of Int32s");

				// Copy the blob out of the db
				int[] blob = new int[len / sizeof(int)];
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
				// Setup the bind and read methods
				try
				{
					var is_nullable = Nullable.GetUnderlyingType(type) != null;
					if (!is_nullable)
					{
						var bind_type = type.IsEnum ? Enum.GetUnderlyingType(type) : type;
						return type.IsEnum
							? (stmt,idx) => Enum.ToObject(type, FunctionMap[bind_type](stmt,idx))
							: FunctionMap[type];
					}
					else // nullable type, wrap the binding functions
					{
						var base_type = Nullable.GetUnderlyingType(type);
						var is_enum   = base_type.IsEnum;
						var bind_type = is_enum ? Enum.GetUnderlyingType(base_type) : base_type;
						return (stmt,idx) =>
							{
								if (Dll.ColumnType(stmt, idx) == DataType.Null) return null;
								var obj = FunctionMap[bind_type](stmt, idx);
								if (is_enum) obj = Enum.ToObject(base_type, obj);
								return obj;
							};
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

		#region SqlStr - Methods for converting clr types into sql strings

		/// <summary>Methods for converting CLR type values into sql strings</summary>
		public static class SqlStr
		{
			public static string Null(object value)
			{
				return "null";
			}
			public static string Bool(object value)
			{
				return (bool)value ? "1" : "0";
			}
			public static string SByte(object value)
			{
				return ((sbyte)value).ToString(CultureInfo.InvariantCulture);
			}
			public static string Byte(object value)
			{
				return ((byte)value).ToString(CultureInfo.InvariantCulture);
			}
			public static string Char(object value)
			{
				return "'" + (char)value + "'";
			}
			public static string Short(object value)
			{
				return ((short)value).ToString(CultureInfo.InvariantCulture);
			}
			public static string UShort(object value)
			{
				return ((ushort)value).ToString(CultureInfo.InvariantCulture);
			}
			public static string Int(object value)
			{
				return ((int)value).ToString(CultureInfo.InvariantCulture);
			}
			public static string UInt(object value)
			{
				return ((uint)value).ToString(CultureInfo.InvariantCulture);
			}
			public static string Long(object value)
			{
				return ((long)value).ToString(CultureInfo.InvariantCulture);
			}
			public static string ULong(object value)
			{
				return ((ulong)value).ToString(CultureInfo.InvariantCulture);
			}
			public static string Decimal(object value)
			{
				return "'" + ((decimal)value).ToString(CultureInfo.InvariantCulture) + "'";
			}
			public static string Float(object value)
			{
				return ((float)value).ToString(CultureInfo.InvariantCulture);
			}
			public static string Double(object value)
			{
				return ((double)value).ToString(CultureInfo.InvariantCulture);
			}
			public static string Text(object value)
			{
				return value != null
					? "'" + ((string)value).Replace("'","''") + "'"
					: "''";
			}
			public static string ByteArray(object value)
			{
				var arr = (byte[])value;
				var sb = new StringBuilder(3+arr.Length*2);
				sb.Append("x'");
				foreach (var b in arr)
					sb.Append(b.ToString("X2"));
				sb.Append("'");
				return sb.ToString();
			}
			public static string IntArray(object value)
			{
				var arr = (int[])value;
				var buf = new byte[arr.Length * sizeof(int)];
				Buffer.BlockCopy(arr, 0, buf, 0, buf.Length);
				return ByteArray(buf);
			}
			public static string Guid(object value)
			{
				return ((Guid)value).ToString();
			}
			public static string DateTimeOffset(object value)
			{
				return Long(((DateTimeOffset)value).Ticks);
			}

			public delegate object Func(string value);
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
					Add(typeof(Guid)           ,Guid          );
					Add(typeof(DateTimeOffset) ,DateTimeOffset); // Note: DateTime deliberately not supported, use DateTimeOffset instead
				}
			}
		}

		/// <summary>
		/// A lookup table from ClrType to sqlstr function.
		/// Users can add custom types and functions to this map if needed</summary>
		public static readonly SqlStr.Map SqlStrFunction = new SqlStr.Map();

		#endregion

		#region Handles

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

		#region Caches

		/// <summary>A per-table object cache for types with a single integral primary key</summary>
		internal class ObjectCache :ICache<object,object>
		{
			private readonly Cache<long,object> m_cache = new Cache<long, object>();
			private readonly TableMetaData m_meta;

			public ObjectCache(Type type, bool thread_safe)
			{
				m_meta       = TableMetaData.GetMetaData(type);
				m_cache.Mode = CacheMode.StandardCache;
				ThreadSafe   = thread_safe;
			}

			/// <summary>Get/Set whether the cache should use thread locking</summary>
			public bool ThreadSafe
			{
				get { return m_cache.ThreadSafe; }
				set { m_cache.ThreadSafe = value; }
			}

			/// <summary>The number of items in the cache</summary>
			public int Count
			{
				get { return m_cache.Count; }
			}

			/// <summary>Get/Set an upper limit on the number of cached objects</summary>
			public int Capacity
			{
				get { return m_cache.Capacity; }
				set { m_cache.Capacity = value; }
			}

			/// <summary>Return performance data for the cache</summary>
			public CacheStats Stats
			{
				get { return m_cache.Stats; }
			}

			/// <summary>Returns true if an object with the given primary key is in the cache</summary>
			public bool IsCached(object key)
			{
				return m_cache.IsCached(Convert.ToInt64(key));
			}

			/// <summary>Preload the cache with an item</summary>
			public void Add(object key, object item)
			{
				m_cache.Add(Convert.ToInt64(key), item);
			}

			/// <summary>
			/// Preload the cache with a range of items.
			/// 'keys.Count' must be greater or equal to 'items.Count'.</summary>
			public void Add(IEnumerable<object> keys, IEnumerable<object> items)
			{
				m_cache.Add(keys.Select(x => Convert.ToInt64(x)), items);
			}

			/// <summary>Remove and item from the cache (does not delete/dispose it)</summary>
			public bool Remove(object key)
			{
				return m_cache.Remove(Convert.ToInt64(key));
			}

			/// <summary>Returns an object from the cache if available, otherwise calls 'on_miss' and caches the result</summary>
			public object Get(object key, Func<object,object> on_miss, Action<object,object> on_hit = null)
			{
				var obj = m_cache.Get(Convert.ToInt64(key),
					k =>
						{
							Trace.WriteLine(ETrace.ObjectCache, string.Format("Object Cache Miss: key={0}", key.ToString()));
							var item = on_miss(key);
							Debug.Assert(item == null || item.GetType() == m_meta.Type, "Wrong object type for this cache");
							return item;
						},
					(k,v) =>
						{
							Trace.WriteLine(ETrace.ObjectCache, string.Format("Object Cache Hit: key={0}, value={1}", k.ToString(CultureInfo.InvariantCulture), v.ToString()));
							if (on_hit != null)
								on_hit(key, v);
						});

				#if CACHE_RETURNS_CLONES
				return m_meta.Clone(obj);
				#else
				return obj; // Danger, this should really be immutable
				#endif
			}

			/// <summary>Handles notification from the database that a row has changed</summary>
			public void Invalidate(object key)
			{
				var row_id = Convert.ToInt64(key);
				m_cache.Invalidate(row_id);
			}

			/// <summary>Removed all cached items from the cache</summary>
			public void Flush()
			{
				m_cache.Flush();
			}
		}

		/// <summary>A cache of compiled queries</summary>
		internal class QueryCache :ICache<string,Query>
		{
			private readonly Cache<string,Query> m_cache = new Cache<string, Query>();
			private readonly Database m_db;

			public QueryCache(Database db, bool thread_safe)
			{
				m_db         = db;
				m_cache.Mode = CacheMode.ObjectPool;
				ThreadSafe   = thread_safe;
			}

			/// <summary>Get/Set whether the cache should use thread locking</summary>
			public bool ThreadSafe
			{
				get { return m_cache.ThreadSafe; }
				set { m_cache.ThreadSafe = value; }
			}

			/// <summary>The number of items in the cache</summary>
			public int Count
			{
				get { return m_cache.Count; }
			}

			/// <summary>Get/Set an upper limit on the number of cached items</summary>
			public int Capacity
			{
				get { return m_cache.Capacity; }
				set { m_cache.Capacity = value; }
			}

			/// <summary>Return performance data for the cache</summary>
			public CacheStats Stats
			{
				get { return m_cache.Stats; }
			}

			/// <summary>Returns true if an object with the given key is in the cache</summary>
			public bool IsCached(string key)
			{
				return m_cache.IsCached(key);
			}

			/// <summary>Preload the cache with an item</summary>
			public void Add(string key, Query item)
			{
				m_cache.Add(key, item);
			}

			/// <summary>Preload the cache with a range of items.</summary>
			public void Add(IEnumerable<string> keys, IEnumerable<Query> items)
			{
				m_cache.Add(keys, items);
			}

			/// <summary>Remove and item from the cache (does not delete/dispose it)</summary>
			public bool Remove(string key)
			{
				return m_cache.Remove(key);
			}

			/// <summary>Returns an object from the cache if available, otherwise calls 'on_miss' and caches the result</summary>
			Query ICache<string,Query>.Get(string key, Func<string, Query> on_miss, Action<string, Query> on_hit)
			{
				return GetQuery(key);
			}

			/// <summary>
			/// A delegate attached to the Closing handler of a query that returns it to the
			/// pool. This is only attached while the query is "out in the wild"</summary>
			private void ReturnToPool(object sender, Query.QueryClosingEventArgs args)
			{
				var query = (Query)sender;
				Trace.QueryInUse(query, false);

				// Reset the query to release any locks on associated tables
				query.Reset();

				// Remove the delegate, if the query is disposed while in the pool we don't want to re-add it to the pool
				query.Closing -= ReturnToPool;

				// Return the query to the cache on closing
				m_cache.Add(query.SqlString, query);
				args.Cancel = true;
			}

			/// <summary>Helper method for creating new queries when a query is not in the cache</summary>
			private Query OnMiss(string key, Func<Query> new_query)
			{
				Trace.WriteLine(ETrace.QueryCache, string.Format("Query Cache Miss: {0}", key));

				// Create a new compiled query.
				var query = new_query();

				// Attach a delegate to ensure the query gets returned to the pool when closed
				query.Closing += ReturnToPool;

				Trace.QueryInUse(query, true);
				return query;
			}

			/// <summary>Helper method for resetting an existing query that has been pulled from the cache</summary>
			private void OnHit(string key, Query query, int first_idx, IEnumerable<object> parms)
			{
				Trace.WriteLine(ETrace.QueryCache, string.Format("Query Cache Hit: {0}", query));

				// Attach a delegate to ensure the query gets returned to the pool when closed
				query.Closing += ReturnToPool;

				// Before returning the cached query, reset it
				query.Reset();
				if (parms != null)
					query.BindParms(first_idx, parms);

				Trace.QueryInUse(query, true);
			}

			/// <summary>Returns a query from the cache</summary>
			public Query GetQuery(string sql_string, int first_idx = 1, IEnumerable<object> parms = null)
			{
				return m_cache.Get(sql_string
					,k     => OnMiss(k, () => new Query(m_db, sql_string, first_idx, parms))
					,(k,v) => OnHit(k, v, first_idx, parms));
			}

			/// <summary>Returns an insert query from the cache</summary>
			public InsertCmd InsertCmd(Type type, OnInsertConstraint on_constraint)
			{
				return (InsertCmd)m_cache.Get(SqlInsertCmd(type, on_constraint)
					,k     => OnMiss(k, () => new InsertCmd(type, m_db, on_constraint))
					,(k,v) => OnHit(k, v, 1, null));
			}

			/// <summary>Returns an insert query from the cache</summary>
			public UpdateCmd UpdateCmd(Type type)
			{
				return (UpdateCmd)m_cache.Get(SqlUpdateCmd(type)
					,k     => OnMiss(k, () => new UpdateCmd(type, m_db))
					,(k,v) => OnHit(k, v, 1, null));
			}

			/// <summary>Returns an insert query from the cache</summary>
			public GetCmd GetCmd(Type type)
			{
				return (GetCmd)m_cache.Get(SqlGetCmd(type)
					,k     => OnMiss(k, () => new GetCmd(type, m_db))
					,(k,v) => OnHit(k, v, 1, null));
			}

			/// <summary>Handles notification from the database that a row has changed</summary>
			public void Invalidate(string key) { m_cache.Invalidate(key); }

			/// <summary>Removed all cached items from the cache</summary>
			public void Flush() { m_cache.Flush(); }
		}
		#endregion

		#region Database
		/// <summary>
		/// Represents a connection to an sqlite database file.<para/>
		/// SQLite provides isolation between operations in separate database connections.
		/// However, there is no isolation between operations that occur within the same
		/// database connection.<para/>
		/// ie. you can have multiple of these concurrently, e.g. one per thread</summary>
		public class Database :IDisposable
		{
			private readonly sqlite3 m_db;
			private readonly Dictionary<string, ICache<object,object>> m_caches;
			private readonly QueryCache m_query_cache;
			private readonly UpdateHookCB m_update_cb_handle;
			private GCHandle m_this_handle;

			// The Id of the thread that created this db handle.
			// For asynchronous access to the database, create 'Database' instances for each thread.
			private int m_owning_thread_id;

			/// <summary>Opens a connection to the database</summary>
			public Database(string filepath, OpenFlags flags = OpenFlags.Create|OpenFlags.ReadWrite)
			{
				Filepath = filepath;
				ReadItemHook = x => x;
				WriteItemHook = x => x;
				m_owning_thread_id = Thread.CurrentThread.ManagedThreadId;
				m_this_handle = GCHandle.Alloc(this);
				m_update_cb_handle = UpdateCB;

				// Open the database file
				m_db = Dll.Open(filepath, flags);
				Trace.WriteLine(ETrace.Handles, string.Format("Database connection opened for '{0}'", filepath));

				// Initialise the per-table object caches
				m_caches = new Dictionary<string, ICache<object,object>>();
				DataChanged += InvalidateCachesOnDataChanged;

				// Initialise the query cache
				m_query_cache = new QueryCache(this, Dll.ThreadSafe){Capacity = 50};

				// Default to no busy timeout
				BusyTimeout = 0;
			}
			public virtual void Dispose()
			{
				Close();
				m_this_handle.Free();
			}

			/// <summary>The filepath of the database file opened</summary>
			public string Filepath { get; private set; }

			/// <summary>Returns the db handle after asserting it's validity</summary>
			public sqlite3 Handle
			{
				get
				{
					AssertCorrectThread();
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
				set { Dll.BusyTimeout(Handle, value); }
			}

			/// <summary>Set the maximum number of instances of a type to cache</summary>
			public void SetQueryCacheCapacity(int limit)
			{
				QueryCache.Capacity = limit;
			}

			/// <summary>Set the maximum number of instances of a type to cache</summary>
			public void SetObjectCacheCapacity(Type type, int limit)
			{
				ObjectCache(type).Capacity = limit;
			}

			/// <summary>Flush the query cache</summary>
			public void FlushQueryCache()
			{
				QueryCache.Flush();
			}

			/// <summary>Flush the object cache for a specific type</summary>
			public void FlushObjectCache(Type type)
			{
				ObjectCache(type).Flush();
			}

			/// <summary>Flush all object caches</summary>
			public void FlushObjectCaches()
			{
				foreach (var c in m_caches)
					c.Value.Flush();
			}

			/// <summary>Returns the object cache for a specific table</summary>
			internal ICache<object,object> ObjectCache(Type type)
			{
				ICache<object,object> c;
				if (!m_caches.TryGetValue(type.Name, out c))
				{
					var meta = TableMetaData(type);
					if (meta.SingleIntegralPK)
						m_caches.Add(type.Name, c = new ObjectCache(type, Dll.ThreadSafe));
					else
						m_caches.Add(type.Name, c = new PassThruCache<object,object>());
				}
				return c;
			}

			/// <summary>Return access to the query cache</summary>
			internal QueryCache QueryCache { get { return m_query_cache; } }

			/// <summary>A method that allows interception of all database object reads</summary>
			public Func<object,object> ReadItemHook { get; set; }

			/// <summary>A method that allows interception of all database object writes</summary>
			public Func<object,object> WriteItemHook { get; set; }

			/// <summary>Close a database file</summary>
			public void Close()
			{
				AssertCorrectThread();
				Trace.WriteLine(ETrace.Handles, string.Format("Closing database connection{0}", m_db.IsClosed ? " (already closed)" : ""));
				if (m_db.IsClosed) return;

				// Release cached objects
				foreach (var x in m_caches) x.Value.Capacity = 0;
				m_query_cache.Capacity = 0;

				// Release the db handle
				m_db.Close();
				if (m_db.CloseResult == Result.Busy)
				{
					Trace.Dump(false);
					throw Exception.New(m_db.CloseResult, "Could not close database handle, there are still prepared statements that haven't been 'finalized' or blob handles that haven't been closed.");
				}
				if (m_db.CloseResult != Result.OK)
				{
					throw Exception.New(m_db.CloseResult, "Failed to close database connection");
				}
			}

			/// <summary>
			/// Executes an sql command that is expected to not return results. (e.g. Insert/Update/Delete).
			/// Returns the number of rows affected by the operation</summary>
			public int Execute(string sql, int first_idx = 1, IEnumerable<object> parms = null)
			{
				AssertCorrectThread();
				using (var query = QueryCache.GetQuery(sql, first_idx, parms))
					return query.Run();
			}

			/// <summary>Executes an sql query that returns a scalar (i.e. int) result</summary>
			public int ExecuteScalar(string sql, int first_idx = 1, IEnumerable<object> parms = null)
			{
				AssertCorrectThread();
				using (var query = QueryCache.GetQuery(sql, first_idx, parms))
				{
					if (!query.Step()) throw Exception.New(Result.Error, "Scalar query returned no results");
					int value = (int)Read.Int(query.Stmt, 0);
					if (query.Step()) throw Exception.New(Result.Error, "Scalar query returned more than one result");
					return value;
				}
			}

			/// <summary>Returns the RowId for the last inserted row</summary>
			public long LastInsertRowId
			{
				get { return Dll.LastInsertRowId(m_db); }
			}

			///<remarks>
			/// How to Enumerate rows of queries that select specific columns, or use functions:
			/// Use (or create) a type with members mapped to the column names returned.
			/// e.g. Create a type and use the Sqlite.Column attribute to set the Name for
			/// each member so that is matches the names returned in the query.
			/// Or, change the query so that the returned columns are given names matching the
			/// members of your type.
			///</remarks>

			/// <summary>Executes a query and enumerates the rows, returning each row as an instance of type 'T'</summary>
			public IEnumerable EnumRows(Type type, string sql, int first_idx = 1, IEnumerable<object> parms = null)
			{
				AssertCorrectThread();
				using (var query = QueryCache.GetQuery(sql, first_idx, parms))
					foreach (var r in query.Rows(type))
						yield return r; // Can't just return query.Rows(type) because Dispose() is called on query
			}
			public IEnumerable<T> EnumRows<T>(string sql, int first_idx = 1, IEnumerable<object> parms = null)
			{
				return EnumRows(typeof(T), sql, first_idx, parms).Cast<T>();
			}

			/// <summary>Drop any existing table created for type 'T'</summary>
			public void DropTable<T>(bool if_exists = true)
			{
				DropTable(typeof(T), if_exists);
			}
			public void DropTable(Type type, bool if_exists = true)
			{
				Trace.WriteLine(ETrace.Tables, string.Format("Drop table for type {0}", type.Name));
				var meta = Sqlite.TableMetaData.GetMetaData(type);
				var opts = if_exists ? "if exists " : "";
				Execute(Sql("drop table ",opts,meta.Name));
				RaiseDataChangedEvent(ChangeType.DropTable, meta.Name, 0);
			}

			/// <summary>Return the sql string used to create a table for 'type'</summary>
			public static string CreateTableString(Type type, OnCreateConstraint on_constraint = OnCreateConstraint.Reject)
			{
				var meta = Sqlite.TableMetaData.GetMetaData(type);
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
			public Table<T> CreateTable<T>(OnCreateConstraint on_constraint = OnCreateConstraint.Reject) where T:new()
			{
				CreateTable(typeof(T), ()=>new T(), on_constraint);
				return Table<T>();
			}
			public Table<T> CreateTable<T>(Func<object> factory, OnCreateConstraint on_constraint = OnCreateConstraint.Reject)
			{
				CreateTable(typeof(T), factory, on_constraint);
				return Table<T>();
			}
			public Table CreateTable(Type type, Func<object> factory = null, OnCreateConstraint on_constraint = OnCreateConstraint.Reject)
			{
				Trace.WriteLine(ETrace.Tables, string.Format("Create table for type {0}", type.Name));
				var meta = Sqlite.TableMetaData.GetMetaData(type);
				if (factory != null) meta.Factory = factory;

				if (on_constraint == OnCreateConstraint.AlterTable && TableExists(type))
					AlterTable(type);
				else
				{
					Execute(Sql(CreateTableString(type, on_constraint)));
					RaiseDataChangedEvent(ChangeType.CreateTable, meta.Name, 0);
				}
				return Table(type);
			}

			/// <summary>Alters an existing table to match the columns for 'type'</summary>
			public void AlterTable<T>()
			{
				AlterTable(typeof(T));
			}
			public void AlterTable(Type type)
			{
				Trace.WriteLine(ETrace.Tables, string.Format("Alter table for type {0}", type.Name));

				// Read the columns that we want the table to have
				var meta = Sqlite.TableMetaData.GetMetaData(type);
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
				RaiseDataChangedEvent(ChangeType.AlterTable, meta.Name, 0);
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
				Trace.WriteLine(ETrace.Tables, string.Format("Rename table for type {0}", type.Name));
				var meta = Sqlite.TableMetaData.GetMetaData(type);
				var sql = Sql("alter table ",meta.Name," rename to ",new_name);
				Execute(sql);
				if (update_meta_data)
					meta.Name = new_name;
			}

			/// <summary>Return an existing table for type 'T'</summary>
			[DebuggerStepThrough] public Table<T> Table<T>()
			{
				return new Table<T>(this);
			}

			/// <summary>
			/// Return an existing table for runtime type 'type'.
			/// 'factory' is a factory method for creating instances of type 'type'. It null, then Activator.CreateInstance is used.</summary>
			[DebuggerStepThrough] public Table Table(Type type)
			{
				return new Table(type, this);
			}

			/// <summary>Factory method for creating Sqlite.Transaction instances</summary>
			public Transaction NewTransaction()
			{
				if (m_transaction_in_progress != null) throw new Exception("Nested transactions on a single db connection are not allowed");
				return m_transaction_in_progress = new Transaction(this, () => m_transaction_in_progress = null);
			}
			private Transaction m_transaction_in_progress;

			/// <summary>General sql querysummary>
			public Query Query(string sql, int first_idx = 1, IEnumerable<object> parms = null)
			{
				return QueryCache.GetQuery(sql, first_idx, parms);
			}

			/// <summary>Find a row in a table of type 'T'</summary>
			public object Find(Type type, params object[] keys)     { return Table(type).Find(keys); }
			public object Find(Type type, object key1, object key2) { return Table(type).Find(key1, key2); }
			public object Find(Type type, object key1)              { return Table(type).Find(key1); }
			public T Find<T>(params object[] keys)                  { return Table<T>().Find(keys); }
			public T Find<T>(object key1, object key2)              { return Table<T>().Find(key1, key2); }
			public T Find<T>(object key1)                           { return Table<T>().Find(key1); }

			/// <summary>Get a row from a table of type 'T'</summary>
			public object Get(Type type, params object[] keys)      { return Table(type).Get(keys); }
			public object Get(Type type, object key1, object key2)  { return Table(type).Get(key1, key2); }
			public object Get(Type type, object key1)               { return Table(type).Get(key1); }
			public T Get<T>(params object[] keys)                   { return Table<T>().Get(keys); }
			public T Get<T>(object key1, object key2)               { return Table<T>().Get(key1, key2); }
			public T Get<T>(object key1)                            { return Table<T>().Get(key1); }

			/// <summary>Insert 'item' into a table based on it's reflected type</summary>
			public int Insert(object item, OnInsertConstraint on_constraint = OnInsertConstraint.Reject)
			{
				return Table(item.GetType()).Insert(item, on_constraint);
			}

			/// <summary>Update the row corresponding to 'item' in a table based on it's reflected type</summary>
			public int Update(object item)
			{
				return Table(item.GetType()).Update(item);
			}

			/// <summary>Delete the row corresponding to 'item' from a table based on it's reflected type</summary>
			public int Delete(object item)
			{
				return Table(item.GetType()).Delete(item);
			}

			/// <summary>Returns free db pages to the OS reducing the db file size</summary>
			public void Vacuum()
			{
				Execute("vacuum");
			}

			/// <summary>Returns the table meta data for 'T'</summary>
			public TableMetaData TableMetaData<T>()
			{
				return TableMetaData(typeof(T));
			}

			/// <summary>Returns the table meta data for 'type'</summary>
			public TableMetaData TableMetaData(Type type)
			{
				return Sqlite.TableMetaData.GetMetaData(type);
			}

			/// <summary>
			/// Raised whenever a row in the database is inserted, updated, or deleted.
			/// Handlers of this event *must not* do anything that will modify the database connection
			/// that invoked this event. Any actions to modify the database connection must be deferred
			/// until after the completion of the Step() call that triggered the update event. Note that
			/// sqlite3_prepare_v2() and sqlite3_step() both modify their database connections.</summary>
			public event EventHandler<DataChangedArgs> DataChanged
			{
				add
				{
					if (m_RowChangedInternal == null) Dll.UpdateHook(m_db, m_update_cb_handle, GCHandle.ToIntPtr(m_this_handle));
					m_RowChangedInternal += value;
				}
				remove
				{
					m_RowChangedInternal -= value;
					if (m_RowChangedInternal == null) Dll.UpdateHook(m_db, null, IntPtr.Zero);
				}
			}
			private EventHandler<DataChangedArgs> m_RowChangedInternal;
			private void RaiseDataChangedEvent(ChangeType change_type, string table_name, long row_id)
			{
				if (m_RowChangedInternal == null) return;
				m_RowChangedInternal(this, new DataChangedArgs(change_type, table_name, row_id));
			}

			/// <summary>Callback passed to the sqlite dll when DataChanged is subscribed to</summary>
			#if MONOTOUCH
			[MonoTouch.MonoPInvokeCallbackAttribute(typeof(UpdateHookCB))]
			#endif
			private static void UpdateCB(IntPtr ctx, int change_type, string db_name, string table_name, long row_id)
			{
				// 'db_name' is always "main". sqlite doesn't allow renaming of the db
				var h = GCHandle.FromIntPtr(ctx);
				var db = (Database)h.Target;
				db.RaiseDataChangedEvent((ChangeType)change_type, table_name, row_id);
			}

			/// <summary>Handler for the data changed event to invalidate cached objects</summary>
			private void InvalidateCachesOnDataChanged(object sender, DataChangedArgs args)
			{
				// If no table name is provided, flush all objects caches,
				// since we don't know which one contained this changed.
				// This shouldn't happen, except it does and I don't know why yet.
				if (string.IsNullOrEmpty(args.TableName))
				{
					Trace.WriteLine(ETrace.ObjectCache, "Sqlite update callback called with no table name. All object caches flushed");
					FlushObjectCaches();
					return;
				}

				ICache<object,object> cache;
				if (m_caches.TryGetValue(args.TableName, out cache))
				{
					switch (args.ChangeType)
					{
					default: throw new ArgumentOutOfRangeException();
					case ChangeType.Insert:
					case ChangeType.Update:
					case ChangeType.Delete:
						cache.Invalidate(args.RowId);
						break;
					case ChangeType.CreateTable:
					case ChangeType.AlterTable:
					case ChangeType.DropTable:
						cache.Flush();
						break;
					}
				}
				else
				{
					Trace.WriteLine(ETrace.ObjectCache, string.Format("Table {0} has no cache", args.TableName));
				}
			}

			[Conditional("DEBUG")] internal void AssertCorrectThread()
			{
				// This is not strictly correct but is probably good enough.
				// If ConfigOption.SingleThreaded is used, all access must be from the thread that opened the db connection.
				// If ConfigOption.MultiThreaded is used, only one thread at a time can use the db connection.
				// If ConfigOption.Serialized is used, it's open session for any threads
				if (m_owning_thread_id != Thread.CurrentThread.ManagedThreadId && !Dll.ThreadSafe)
				{
					var ex = Exception.New(Result.Misuse, string.Format("Cross-thread use of Sqlite ORM.\n{0}", new StackTrace()));
					Log.Exception(this, ex, "Cross-thread use of Sqlite ORM");
					throw ex;
				}
			}
		}
		// ReSharper restore MemberHidesStaticFromOuterClass
		#endregion

		#region Table
		/// <summary>Represents a single table in the database</summary>
		public class Table :IEnumerable
		{
			protected readonly TableMetaData m_meta;
			protected readonly Database m_db; // The handle for the database connection
			protected readonly CmdExpr m_cmd;

			public Table(Type type, Database db)
			{
				if (db.Handle.IsInvalid) throw new ArgumentNullException("db", "Invalid database handle");
				m_meta = TableMetaData.GetMetaData(type);
				m_db   = db;
				m_cmd  = new CmdExpr();
				Cache  = db.ObjectCache(type);
			}

			/// <summary>The name of this table</summary>
			public string Name
			{
				get { return m_meta.Name; }
			}

			/// <summary>Get the database connection associated with this table</summary>
			public Database DB
			{
				get { return m_db; }
			}

			/// <summary>Return the meta data for this table</summary>
			public TableMetaData MetaData
			{
				get { return m_meta; }
			}

			/// <summary>Return access to the cache used by this table</summary>
			public ICache<object,object> Cache { get; private set; }

			/// <summary>Gets the number of columns in this table</summary>
			public int ColumnCount
			{
				get { return m_meta.ColumnCount; }
			}

			/// <summary>Get the number of rows in this table</summary>
			public int RowCount
			{
				get
				{
					GenerateExpression(null, "count");
					try { return m_db.ExecuteScalar(m_cmd.SqlString, 1, m_cmd.Arguments); } finally { ResetExpression(); }
				}
			}

			/// <summary>Returns a row in the table or null if not found</summary>
			public object Find(params object[] keys)
			{
				using (var get = m_db.QueryCache.GetCmd(m_meta.Type))
				{
					get.BindPks(m_meta.Type, 1, keys);
					return get.Find();
				}
			}
			public T Find<T>(params object[] keys)
			{
				return (T)Find(keys);
			}

			/// <summary>Returns a row in the table or null if not found</summary>
			public object Find(object key1, object key2) // overload for performance
			{
				using (var get = m_db.QueryCache.GetCmd(m_meta.Type))
				{
					get.BindPks(m_meta.Type, 1, key1, key2);
					return get.Find();
				}
			}
			public T Find<T>(object key1, object key2) // overload for performance
			{
				return (T)Find(key1, key2);
			}

			/// <summary>Returns a row in the table or null if not found</summary>
			public object Find(object key1) // overload for performance
			{
				return Cache.Get(key1, k =>
					{
						using (var get = m_db.QueryCache.GetCmd(m_meta.Type))
						{
							get.BindPks(m_meta.Type, 1, key1);
							return get.Find();
						}
					});
			}
			public T Find<T>(object key1) // overload for performance
			{
				return (T)Find(key1);
			}

			/// <summary>Returns a row in the table (throws if not found)</summary>
			public object Get(params object[] keys)
			{
				var item = Find(keys);
				if (ReferenceEquals(item, null)) throw Exception.New(Result.NotFound, "Row not found for key(s): "+string.Join(",", keys.Select(x=>x.ToString())));
				return item;
			}
			public T Get<T>(params object[] keys)
			{
				return (T)Get(keys);
			}

			/// <summary>Returns a row in the table (throws if not found)</summary>
			public object Get(object key1, object key2) // overload for performance
			{
				var item = Find(key1, key2);
				if (ReferenceEquals(item, null)) throw Exception.New(Result.NotFound, "Row not found for keys: "+key1+","+key2);
				return item;
			}
			public T Get<T>(object key1, object key2) // overload for performance
			{
				return (T)Get(key1, key2);
			}

			/// <summary>Returns a row in the table (throws if not found)</summary>
			public object Get(object key1) // overload for performance
			{
				var item = Find(key1);
				if (ReferenceEquals(item, null)) throw Exception.New(Result.NotFound, "Row not found for key: "+key1);
				return item;
			}
			public T Get<T>(object key1) // overload for performance
			{
				return (T)Get(key1);
			}

			/// <summary>Insert an item into the table.</summary>
			public int Insert(object item, OnInsertConstraint on_constraint = OnInsertConstraint.Reject)
			{
				using (var insert = m_db.QueryCache.InsertCmd(m_meta.Type, on_constraint))
				{
					insert.BindObj(item); // Bind 'item' to it
					var count = insert.Run();  // Run the query
					m_meta.SetAutoIncPK(item, m_db.Handle);
					return count;
				}
			}

			/// <summary>Insert many items into the table.</summary>
			public int Insert(IEnumerable<object> items, OnInsertConstraint on_constraint = OnInsertConstraint.Reject)
			{
				int count = 0;
				using (var insert = m_db.QueryCache.InsertCmd(m_meta.Type, on_constraint))
				{
					foreach (var item in items)
					{
						insert.BindObj(item); // Bind 'item' to it
						count += insert.Run();  // Run the query
						m_meta.SetAutoIncPK(item, m_db.Handle);
						insert.Reset();
					}
				}
				return count;
			}

			/// <summary>Update 'item' in the table</summary>
			public int Update(object item)
			{
				using (var update = m_db.QueryCache.UpdateCmd(m_meta.Type))
				{
					update.BindObj(item); // Bind 'item' to it
					var count = update.Run(); // Run the query
					return count;
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
				Trace.WriteLine(ETrace.Query, string.Format("Deleting {0} (key: {1})", m_meta.Name, string.Join(",",keys)));
				using (var query = m_db.QueryCache.GetQuery(SqlDeleteCmd(m_meta.Type)))
				{
					query.BindParms(1, keys);
					query.Step();
					return query.RowsChanged;
				}
			}

			/// <summary>Delete a row from the table. Returns the number of rows affected</summary>
			public int DeleteByKey(object key1, object key2) // overload for performance
			{
				Trace.WriteLine(ETrace.Query, string.Format("Deleting {0} (key: {1},{2})", m_meta.Name, key1, key2));
				using (var query = m_db.QueryCache.GetQuery(SqlDeleteCmd(m_meta.Type)))
				{
					query.BindParms(1, key1, key2);
					query.Step();
					return query.RowsChanged;
				}
			}

			/// <summary>Delete a row from the table. Returns the number of rows affected</summary>
			public int DeleteByKey(object key1) // overload for performance
			{
				Trace.WriteLine(ETrace.Query, string.Format("Deleting {0} (key: {1})", m_meta.Name, key1));
				using (var query = m_db.QueryCache.GetQuery(SqlDeleteCmd(m_meta.Type)))
				{
					query.BindParms(1, key1);
					query.Step();
					return query.RowsChanged;
				}
			}

			/// <summary>Update a single column for a single row in the table</summary>
			public int Update<TValueType>(string column_name, TValueType value, params object[] keys)
			{
				Trace.WriteLine(ETrace.Query, string.Format("Updating column {0} in table {1} (key: {2})", column_name, m_meta.Name, string.Join(",",keys)));
				var column_meta = m_meta.Column(column_name);
				if (m_meta.Pks.Length == 0) throw Exception.New(Result.Misuse, "Cannot update an item with no primary keys since it cannot be identified");
				var sql = Sql("update ",m_meta.Name," set ",column_meta.NameBracketed," = ? where ",m_meta.PkConstraints());
				using (var query = m_db.QueryCache.GetQuery(sql))
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
				Trace.WriteLine(ETrace.Query, string.Format("Updating column {0} in table {1} (all rows)", column_name, m_meta.Name));
				var column_meta = m_meta.Column(column_name);
				var sql = Sql("update ",m_meta.Name," set ",column_name," = ?");
				using (var query = m_db.QueryCache.GetQuery(sql))
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
				var sql = Sql("select ",column_meta.NameBracketed," from ",m_meta.Name," where ",m_meta.PkConstraints());
				using (var query = m_db.QueryCache.GetQuery(sql))
				{
					query.BindPks(m_meta.Type, 1, keys);
					query.Step();
					return (TValueType)column_meta.ReadFn(query.Stmt, 0);
				}
			}

			/// <summary>IEnumerable interface</summary>
			IEnumerator IEnumerable.GetEnumerator()
			{
				GenerateExpression();
				ResetExpression(); // Don't put this in a finally block because it gets called for each yield return
				return m_db.EnumRows(m_meta.Type, m_cmd.SqlString, 1, m_cmd.Arguments).GetEnumerator();
			}

			/// <summary>Generate the sql string and arguments</summary>
			public Table GenerateExpression(string select = null, string func = null) { m_cmd.Generate(m_meta.Type, select, func); return this; }

			/// <summary>Resets the generated sql string and arguments. Mainly used when an expression is generated but not used</summary>
			public Table ResetExpression() { m_cmd.Reset(); return this; }

			/// <summary>Read only access to the generated sql string</summary>
			public string SqlString { get { return m_cmd.SqlString; } }

			/// <summary>Read only access to the generated sql arguments</summary>
			public List<object> Arguments { get { return m_cmd.Arguments; } }

			/// <summary>Return the first row. Throws if no rows found</summary>
			public T First<T>()
			{
				m_cmd.Take(1);
				try { return ((IEnumerable<T>)this).First(); } finally { m_cmd.Reset(); }
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
				try { return this.Cast<T>().FirstOrDefault(); } finally { m_cmd.Reset(); }
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
			public int Count() // Overload added to prevent accidental use of Linq-to-Objects Count() extension
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
				try { return m_db.Execute(m_cmd.SqlString, 1, m_cmd.Arguments); } finally { ResetExpression(); }
			}

			/// <summary>Add a 'select' clause to row enumeration</summary>
			public IEnumerable<U> Select<T,U>(Expression<Func<T,U>> pred)
			{
				m_cmd.Select(pred);
				GenerateExpression();
				try { return m_db.EnumRows<U>(m_cmd.SqlString, 1, m_cmd.Arguments); } finally { ResetExpression(); }
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
				private Expression m_select;
				private Expression m_where;
				private string m_order;
				private int? m_take;
				private int? m_skip;

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
				public string SqlString { get; private set; }

				/// <summary>Arguments for '?'s in the generated SqlString. Invalid until 'Generate()' is called</summary>
				public List<object> Arguments { get; private set; }

				/// <summary>A flag to indicate that the expression has been used</summary>
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
				public void Generate(Type type, string select = null, string func = null)
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
					Trace.WriteLine(ETrace.Query, "Sql expression generated: " + ToString());
				}

				/// <summary>The result of a call to 'Translate'</summary>
				private class TranslateResult
				{
					public readonly StringBuilder Text = new StringBuilder();
					public readonly List<object>  Args = new List<object>();
				}

				/// <summary>
				/// Translates an expression into an sql sub-string in 'text'.
				/// Returns an object associated with 'expr' where appropriate, otherwise null.</summary>
				private TranslateResult Translate(Expression expr, TranslateResult result = null)
				{
					if (result == null)
						result = new TranslateResult();

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
					default: throw new NotSupportedException();
					case ExpressionType.Parameter:
						{
							return result;
						}
					case ExpressionType.New:
						#region New
						{
							var ne = (NewExpression)expr;
							result.Text.Append(string.Join(",", ne.Arguments.Select(x => Translate(x).Text)));
							break;
						}
						#endregion
					case ExpressionType.MemberAccess:
						#region Member Access
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
								// Special cases for nullables.
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

							// Get the member value
							object ob;
							switch (me.Member.MemberType)
							{
							default: throw new NotSupportedException("MemberExpression: " + me.Member.MemberType.ToString());
							case MemberTypes.Property: ob = ((PropertyInfo)me.Member).GetValue(res.Args[0], null); break;
							case MemberTypes.Field:    ob = (   (FieldInfo)me.Member).GetValue(res.Args[0]); break;
							}

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
						#region Constant
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
						#region Convert
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
						#region Unary Expressions
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
						#region Quote
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
		#endregion

		#region Table<T>
		/// <summary>Represents a table within the database for a compile-time type 'T'.</summary>
		public class Table<T> :Table ,IEnumerable<T>
		{
			public Table(Database db) :base(typeof(T), db) {}

			/// <summary>Returns a row in the table or null if not found</summary>
			public new T Find(params object[] keys)
			{
				return Find<T>(keys);
			}

			/// <summary>Returns a row in the table or null if not found</summary>
			public new T Find(object key1, object key2) // overload for performance
			{
				return Find<T>(key1, key2);
			}

			/// <summary>Returns a row in the table or null if not found</summary>
			public new T Find(object key1) // overload for performance
			{
				return Find<T>(key1);
			}

			/// <summary>Returns a row in the table (throws if not found)</summary>
			public new T Get(params object[] keys)
			{
				return Get<T>(keys);
			}

			/// <summary>Returns a row in the table (throws if not found)</summary>
			public new T Get(object key1, object key2) // overload for performance
			{
				return Get<T>(key1, key2);
			}

			/// <summary>Returns a row in the table (throws if not found)</summary>
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
				return m_db.EnumRows<T>(m_cmd.SqlString, 1, m_cmd.Arguments).GetEnumerator();
			}
		}
		#endregion

		#region Query
		/// <summary>Represents an sqlite prepared statement and iterative result (wraps an sqlite3_stmt handle).</summary>
		public class Query :IDisposable
		{
			protected readonly Database m_db;
			protected readonly sqlite3_stmt m_stmt; // sqlite managed memory for this query

			public Query(Database db, sqlite3_stmt stmt)
			{
				if (stmt.IsInvalid) throw new ArgumentNullException("stmt", "Invalid sqlite prepared statement handle");
				m_db = db;
				m_stmt = stmt;
				Trace.QueryCreated(this);
				Trace.WriteLine(ETrace.Query, string.Format("Query created '{0}'", SqlString));
			}
			public Query(Database db, string sql_string, int first_idx = 1, IEnumerable<object> parms = null)
				:this(db, Compile(db, sql_string))
			{
				if (parms != null)
					BindParms(first_idx, parms);
			}

			/// <summary>IDisposable</summary>
			public void Dispose()
			{
				Close();
			}

			/// <summary>The string used to construct this query statement</summary>
			public string SqlString
			{
				get { return Dll.SqlString(m_stmt); }
			}

			/// <summary>Returns the m_stmt field after asserting it's validity</summary>
			public sqlite3_stmt Stmt
			{
				get { Debug.Assert(!m_stmt.IsInvalid, "Invalid query object"); return m_stmt; }
			}

			/// <summary>Call when done with this query</summary>
			public void Close()
			{
				if (m_stmt.IsClosed) return;
				Trace.WriteLine(ETrace.Handles, string.Format("Query closed '{0}'", SqlString));

				// Call the event to see if we cancel closing the query
				var cancel = new QueryClosingEventArgs();
				if (Closing != null) Closing(this, cancel);
				if (cancel.Cancel) return;

				// After 'sqlite3_finalize()', it is illegal to use m_stmt. So save 'db' here first
				Reset(); // Call reset to clear any error code from failed queries.
				m_stmt.Close();
				if (m_stmt.CloseResult != Result.OK)
					throw Exception.New(m_stmt.CloseResult, Dll.ErrorMsg(m_db.Handle));

				Trace.QueryClosed(this);
			}

			/// <summary>An event raised when this query is closing</summary>
			public event EventHandler<QueryClosingEventArgs> Closing;
			public class QueryClosingEventArgs :EventArgs
			{
				public bool Cancel { get { return m_cancel; } set { m_cancel = m_cancel || value; } }
				private bool m_cancel;
			}

			/// <summary>Return the number of parameters in this statement</summary>
			public int ParmCount
			{
				get { return Dll.BindParameterCount(Stmt); }
			}

			/// <summary>Return the index for the parameter named 'name'</summary>
			public int ParmIndex(string name)
			{
				int idx = Dll.BindParameterIndex(Stmt, name);
				if (idx == 0) throw Exception.New(Result.Error, "Parameter name not found");
				return idx;
			}

			/// <summary>Return the name of a parameter by index</summary>
			public string ParmName(int idx)
			{
				return Dll.BindParameterName(Stmt, idx);
			}

			/// <summary>Bind a value to a specific parameter</summary>
			public void BindParm<T>(int idx, T value)
			{
				var bind = Bind.FuncFor(typeof(T));
				bind(m_stmt, idx, value);
			}

			/// <summary>
			/// Bind an array of parameters starting at parameter index 'first_idx' (remember parameter indices start at 1)
			/// To reuse the Query, call Reset(), then bind new keys, then call Step() again</summary>
			public void BindParms(int first_idx, IEnumerable<object> parms)
			{
				foreach (var p in parms)
				{
					if (p == null) Bind.Null(m_stmt, first_idx++);
					else Bind.FuncFor(p.GetType())(m_stmt, first_idx++, p);
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
				Dll.Reset(Stmt);
			}

			/// <summary>Iterate to the next row in the result. Returns true if there are more rows available</summary>
			public bool Step()
			{
				m_db.AssertCorrectThread();
				for (;;)
				{
					var res = Dll.Step(Stmt);
					switch (res)
					{
					default: throw Exception.New(res, Dll.ErrorMsg(m_db.Handle));
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
				get { return Dll.Changes(m_db.Handle); }
			}

			/// <summary>Returns the number of columns in the result of this query</summary>
			public int ColumnCount
			{
				get { return Dll.ColumnCount(Stmt); }
			}

			/// <summary>Return the sql data type for a specific column</summary>
			public DataType ColumnType(int idx)
			{
				return Dll.ColumnType(m_stmt, idx);
			}

			/// <summary>Returns the name of the column at position 'idx'</summary>
			public string ColumnName(int idx)
			{
				return Dll.ColumnName(Stmt, idx);
			}

			/// <summary>Enumerates the columns in the result</summary>
			public IEnumerable<string> ColumnNames
			{
				get { for (int i = 0, iend= ColumnCount; i != iend; ++i) yield return ColumnName(i); }
			}

			/// <summary>Read the value of a particular column</summary>
			public T ReadColumn<T>(int idx)
			{
				return (T)Read.FuncFor(typeof(T))(m_stmt, idx);
			}

			/// <summary>Enumerate over the result rows of this query, interpreting each row as type 'T'</summary>
			public IEnumerable Rows(Type type)
			{
				var meta = TableMetaData.GetMetaData(type);
				while (Step())
				{
					var obj = meta.ReadObj(Stmt);
					yield return m_db.ReadItemHook(obj);
				}
			}
			public IEnumerable<T> Rows<T>()
			{
				return Rows(typeof(T)).Cast<T>();
			}

			public override string ToString()  { return SqlString; }
		}
		#endregion

		#region Specialised query sub-classes

		/// <summary>A specialised query used for inserting objects into a table</summary>
		public class InsertCmd :Query
		{
			/// <summary>The type metadata for this insert command</summary>
			public TableMetaData MetaData { get; protected set; }

			/// <summary>Creates a compiled query for inserting an object of type 'type' into a table.</summary>
			public InsertCmd(Type type, Database db, OnInsertConstraint on_constraint = OnInsertConstraint.Reject)
			:base(db, SqlInsertCmd(type, on_constraint))
			{
				MetaData = db.TableMetaData(type);
			}

			/// <summary>Bind the values in 'item' to this insert query making it ready for running</summary>
			public void BindObj(object item)
			{
				item = m_db.WriteItemHook(item);
				MetaData.BindObj(m_stmt, 1, item, MetaData.NonAutoIncs);
			}

			/// <summary>Run the insert command. Call 'Reset()' before running the command again</summary>
			public override int Run()
			{
				Trace.WriteLine(ETrace.Query, string.Format("Inserting {0}", MetaData.Name));
				Step();
				return RowsChanged;
			}
		}

		/// <summary>A specialised query used for updating objects into a table</summary>
		public class UpdateCmd :Query
		{
			/// <summary>The type metadata for this update command</summary>
			public TableMetaData MetaData { get; protected set; }

			/// <summary>Creates a compiled query for updating an object of type 'type' into a table.</summary>
			public UpdateCmd(Type type, Database db)
			:base(db, SqlUpdateCmd(type))
			{
				MetaData = db.TableMetaData(type);
			}

			/// <summary>Bind the values in 'item' to this insert query making it ready for running</summary>
			public void BindObj(object item)
			{
				item = m_db.WriteItemHook(item);
				MetaData.BindObj(m_stmt, 1, item, MetaData.NonPks);
				MetaData.BindObj(m_stmt, 1 + MetaData.NonPks.Length, item, MetaData.Pks);
			}

			/// <summary>Run the insert command. Call 'Reset()' before running the command again</summary>
			public override int Run()
			{
				Trace.WriteLine(ETrace.Query, string.Format("Updating {0}", MetaData.Name));
				Step();
				return RowsChanged;
			}
		}

		/// <summary>A specialised query used for getting objects from the db</summary>
		public class GetCmd :Query
		{
			/// <summary>The type metadata for this insert command</summary>
			public TableMetaData MetaData { get; protected set; }

			/// <summary>
			/// Create a compiled query for getting an object of type 'T' from a table.
			/// Users can then bind primary keys, and run the query repeatedly to get multiple items.</summary>
			public GetCmd(Type type, Database db) :base(db, SqlGetCmd(type))
			{
				MetaData = db.TableMetaData(type);
			}

			/// <summary>
			/// Populates 'item' and returns true if the query finds a row, otherwise returns false.
			/// Remember to call 'Reset()' before running the command again</summary>
			public object Find()
			{
				foreach (var x in Rows(MetaData.Type)) return x;
				return null;
			}
		}

		#endregion

		#region DataChanged event args

		/// <summary>Event args for the Database DataChanged event</summary>
		public class DataChangedArgs :EventArgs
		{
			/// <summary>How the row was changed. One of Inserted, Updated, or Deleted</summary>
			public ChangeType ChangeType { get; set; }

			/// <summary>The name of the changed table</summary>
			public string TableName { get; set; }

			/// <summary>The id of the effected row</summary>
			public long RowId { get; set; }

			public DataChangedArgs(ChangeType change_type, string table_name, long row_id)
			{
				ChangeType   = change_type;
				TableName    = table_name;
				RowId        = row_id;
			}
		}

		#endregion

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

			#if COMPILED_LAMBDAS
			/// <summary>Compiled lambda method for testing two 'Type' instances as equal</summary>
			private readonly MethodInfo m_method_equal;

			/// <summary>Compiled lambda method for returning a shallow copy of an object</summary>
			private readonly MethodInfo m_method_clone;
			#endif

			/// <summary>The .NET type that this meta data is for</summary>
			public Type Type { get; private set; }

			/// <summary>The table name (defaults to the type name)</summary>
			public string Name { get; set; }
			public string NameQuoted { get { return "'"+Name+"'"; } }

			/// <summary>Table constraints for this table (default is none)</summary>
			public string Constraints { get; set; }

			/// <summary>Enumerate the column meta data</summary>
			public ColumnMetaData[] Columns { get { return m_column; } }

			/// <summary>The primary key columns, in order</summary>
			public ColumnMetaData[] Pks { get; private set; }

			/// <summary>The non primary key columns</summary>
			public ColumnMetaData[] NonPks { get; private set; }

			/// <summary>The columns that aren't auto increment columns</summary>
			public ColumnMetaData[] NonAutoIncs { get; private set; }

			/// <summary>Gets the number of columns in this table</summary>
			public int ColumnCount { get { return m_column.Length; } }

			/// <summary>Returns true of there is only one primary key for this type and it is an integer (i.e. an alias for the row id)</summary>
			public bool SingleIntegralPK { get { return m_single_pk != null && m_single_pk.SqlDataType == DataType.Integer; } }

			/// <summary>True if the table uses multiple primary keys</summary>
			public bool MultiplePK { get { return Pks.Length > 1; } }

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

			/// <summary>
			/// Constructs the meta data for mapping a type to a database table.
			/// By default, 'Activator.CreateInstance' is used as the factory function.
			/// This is slow however so use a static delegate if possible</summary>
			public TableMetaData(Type type)
			{
				Trace.WriteLine(ETrace.Tables, string.Format("Creating table meta data for '{0}'", type.Name));
				var column_name_trim = new[]{' ','\t','\'','\"','[',']'};

				// Get the table attribute
				var attrs = type.GetCustomAttributes(typeof(TableAttribute), true);
				var attr = attrs.Length != 0 ? (TableAttribute)attrs[0] : new TableAttribute();

				Type = type;
				Name = type.Name;
				Constraints = attr.Constraints ?? "";
				Factory = () => Activator.CreateInstance(Type);
				TableKind = Kind.Unknown;

				// Build a collection of columns to ignore
				var ignored = new List<string>();
				foreach (var ign in type.GetCustomAttributes(typeof(IgnoreColumnsAttribute), true).Cast<IgnoreColumnsAttribute>())
					ignored.AddRange(ign.Ignore.Split(',').Select(x => x.Trim(column_name_trim)));

				// Tests if a member should be included as a column in the table
				Func<MemberInfo, List<string>, bool> inc_member = (mi,marked) =>
					!mi.GetCustomAttributes(typeof(IgnoreAttribute), false).Any() &&  // doesn't have the ignore attribute and,
					!ignored.Contains(mi.Name) &&                                     // isn't in the ignore list and,
					(mi.GetCustomAttributes(typeof(ColumnAttribute), false).Any() ||  // has the column attribute or,
					(attr.AllByDefault && marked.Contains(mi.Name)));                 // all in by default and 'mi' is in the collection of found columns

				const BindingFlags binding_flags = BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
				var pflags = attr.PropertyBindingFlags != BindingFlags.Default ? attr.PropertyBindingFlags |BindingFlags.Instance : BindingFlags.Default;
				var fflags = attr.FieldBindingFlags    != BindingFlags.Default ? attr.FieldBindingFlags    |BindingFlags.Instance : BindingFlags.Default;

				// Create a collection of the columns of this table
				var cols = new List<ColumnMetaData>();
				{
					// If AllByDefault is enabled, get a collection of the properties/fields indicated by the binding flags in the attribute
					var mark = !attr.AllByDefault ? null :
						type.GetProperties(pflags).Where(x => x.CanRead && x.CanWrite).Select(x => x.Name).Concat(
						type.GetFields    (fflags).Select(x => x.Name)).ToList();

					// Check all public/non-public properties/fields
					cols.AddRange(AllProps (type, binding_flags).Where(pi => inc_member(pi,mark)).Select(pi => new ColumnMetaData(pi)));
					cols.AddRange(AllFields(type, binding_flags).Where(fi => inc_member(fi,mark)).Select(fi => new ColumnMetaData(fi)));

					// If we found read/write properties or fields then this is a normal db table type
					if (cols.Count != 0)
						TableKind = Kind.Table;
				}

				// If no read/write columns were found, look for readonly columns
				if (TableKind == Kind.Unknown)
				{
					// If AllByDefault is enabled, get a collection of the properties/fields indicated by the binding flags in the attribute
					var mark = !attr.AllByDefault ? null :
						type.GetProperties(pflags).Where(x => x.CanRead).Select(x => x.Name).Concat(
						type.GetFields    (fflags).Select(x => x.Name)).ToList();

					// If we find public readonly properties or fields
					cols.AddRange(AllProps(type, binding_flags).Where(pi => inc_member(pi,mark)).Select(x => new ColumnMetaData(x)));
					cols.AddRange(AllProps(type, binding_flags).Where(fi => inc_member(fi,mark)).Select(x => new ColumnMetaData(x)));
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
					foreach (var pk in Constraints.Substring(s+1, e-s-1).Split(',').Select(x => x.Trim(column_name_trim)))
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

				#if COMPILED_LAMBDAS
				// Initialise the generated methods for this type
				m_method_equal = typeof(MethodGenerator<>).MakeGenericType(Type).GetMethod("Equal", BindingFlags.Static|BindingFlags.Public);
				m_method_clone = typeof(MethodGenerator<>).MakeGenericType(Type).GetMethod("Clone", BindingFlags.Static|BindingFlags.Public);
				#endif
			}

			/// <summary>Returns all inherited properties for a type</summary>
			private static IEnumerable<PropertyInfo> AllProps(Type type, BindingFlags flags)
			{
				if (type == null || type == typeof(object)) return Enumerable.Empty<PropertyInfo>();
				return AllProps(type.BaseType, flags).Concat(type.GetProperties(flags|BindingFlags.DeclaredOnly));
			}

			/// <summary>Returns all inherited fields for a type</summary>
			private static IEnumerable<FieldInfo> AllFields(Type type, BindingFlags flags)
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
				foreach (var c in m_column)
					if (string.CompareOrdinal(c.Name, column_name) == 0)
						return c;

				return null;
			}

			/// <summary>
			/// Bind the primary keys 'keys' to the parameters in a
			/// prepared statement starting at parameter 'first_idx'.</summary>
			public void BindPks(sqlite3_stmt stmt, int first_idx, params object[] keys)
			{
				if (first_idx < 1) throw new ArgumentException("Parameter binding indices start at 1 so 'first_idx' must be >= 1");
				if (Pks.Length != keys.Length) throw new ArgumentException("Incorrect number of primary keys passed for type "+Name);
				if (Pks.Length == 0) throw new ArgumentException("Attempting to bind primary keys for a type without primary keys");

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
			public object ReadObj(sqlite3_stmt stmt)
			{
				var column_count = Dll.ColumnCount(stmt);
				if (column_count == 0)
					return null;

				object obj;
				if (TableKind == Kind.Table)
				{
					obj = Factory();
					for (int i = 0; i != column_count; ++i)
					{
						var cname = Dll.ColumnName(stmt, i);
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
						var cname = Dll.ColumnName(stmt, i);
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

			/// <summary>Returns a shallow copy of 'obj' as a new instance</summary>
			public object Clone(object item)
			{
				Debug.Assert(item.GetType() == Type, "'item' is not the correct type for this table");
				#if COMPILED_LAMBDAS
				return m_method_clone.Invoke(null, new[]{item});
				#else
				return MethodGenerator.Clone(item);
				#endif
			}
			public T Clone<T>(T item) { return (T)Clone((object)item); }

			/// <summary>Returns true of 'lhs' and 'rhs' are equal instances of this table type</summary>
			public bool Equal(object lhs, object rhs)
			{
				Debug.Assert(lhs.GetType() == Type, "'lhs' is not the correct type for this table");
				Debug.Assert(rhs.GetType() == Type, "'rhs' is not the correct type for this table");
				#if COMPILED_LAMBDAS
				return (bool)m_method_equal.Invoke(null, new[]{lhs, rhs});
				#else
				return MethodGenerator.Equal(lhs,rhs);
				#endif
			}

			// ReSharper disable UnusedMember.Local
			#if COMPILED_LAMBDAS
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
				public static object Clone(object obj) { return m_func_clone((T)obj); }
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
			// ReSharper restore UnusedMember.Local

			/// <summary>Returns a table declaration string for this table</summary>
			public string Decl()
			{
				var sb = new StringBuilder();
				sb.Append(string.Join(",\n",m_column.Select(c => c.ColumnDef(m_single_pk != null))));
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
				int id = (int)Dll.LastInsertRowId(db);
				m_single_pk.Set(obj, id);
			}
		}
		#endregion

		#region Column Meta Data
		/// <summary>A column within a db table</summary>
		public class ColumnMetaData
		{
			public const int OrderBaseValue = 0xFFFF;

			/// <summary>The member info for the member represented by this column</summary>
			public MemberInfo MemberInfo;

			/// <summary>The name of the column</summary>
			public string Name;
			public string NameBracketed { get { return "[" + Name + "]"; } }

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
			public ColumnMetaData(Type type)
			{
				Get = obj => obj;
				Set = (obj,val) => { throw new NotImplementedException(); };
				Init(null, type);
			}

			/// <summary>Common constructor code</summary>
			private void Init(MemberInfo mi, Type type)
			{
				ColumnAttribute attr = null;
				if (mi != null) attr = (ColumnAttribute)mi.GetCustomAttributes(typeof(ColumnAttribute), true).FirstOrDefault();
				if (attr == null) attr = new ColumnAttribute();
				var is_nullable = Nullable.GetUnderlyingType(type) != null;

				MemberInfo      = mi;
				Name            = !string.IsNullOrEmpty(attr.Name) ? attr.Name : (mi != null ? mi.Name : string.Empty);
				SqlDataType     = attr.SqlDataType != DataType.Null ? attr.SqlDataType : SqlType(type);
				Constraints     = attr.Constraints ?? "";
				IsPk            = attr.PrimaryKey;
				IsAutoInc       = attr.AutoInc;
				IsNotNull       = type.IsValueType && !is_nullable;
				IsCollate       = false;
				Order           = OrderBaseValue + attr.Order;
				ClrType         = type;

				// Setup the bind and read methods
				BindFn = Bind.FuncFor(type);
				ReadFn = Read.FuncFor(type);
				Trace.WriteLine(ETrace.Tables, string.Format("   Column: '{0}'", Name));
			}

			/// <summary>Returns the column definition for this column</summary>
			public string ColumnDef(bool incl_pk)
			{
				return Sql(NameBracketed," ",SqlDataType.ToString().ToLowerInvariant()," ",incl_pk&&IsPk?"primary key ":"",IsAutoInc?"autoincrement ":"",Constraints);
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

		#region Attribute types

		/// <summary>
		/// Controls the mapping from .NET type to database table.
		/// By default, all public properties (including inherited properties) are used as columns,
		/// this can be changed using the PropertyBindingFlags/FieldBindingFlags properties.</summary>
		[AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
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

			/// <summary>The column name to use. If null or empty, the member name is used</summary>
			public string Name { get; set; }

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
				Name        = string.Empty;
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
			private readonly Database m_db;
			private readonly Action m_disposed;
			private bool m_completed;

			/// <summary>Typically created using the Database.NewTransaction() method</summary>
			public Transaction(Database db, Action on_dispose)
			{
				m_db = db;
				m_disposed = on_dispose;
				m_completed = false;

				// Begin the transaction
				m_db.Execute("begin transaction");
			}
			public Database DB { get { return m_db; } }
			public void Commit()
			{
				if (m_completed)
					throw new Exception("Transaction already completed");

				m_db.Execute("commit");
				m_completed = true;
			}
			public void Rollback()
			{
				if (m_completed)
					throw new Exception("Transaction already completed");

				m_db.Execute("rollback");
				m_completed = true;
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
		}

		#endregion

		#region Sqlite.Exception

		/// <summary>An exception type specifically for sqlite exceptions</summary>
		public class Exception :System.Exception
		{
			/// <summary>Helper for constructing new exceptions</summary>
			public static Exception New(Result res, string message)
			{
				// If tracing is enabled, append a list of the active query objects
				#if SQLITE_TRACE
				var sb = new StringBuilder();
				Trace.Dump(sb, true);
				message += sb.ToString();
				#endif

				return new Exception(message){Result = res};
			}

			/// <summary>The result code associated with this exception</summary>
			public Result Result { get; private set; }

			public Exception() {}
			public Exception(string message):base(message) {}
			public Exception(string message, System.Exception inner_exception):base(message, inner_exception) {}
			public override string ToString() { return string.Format("{0} - {1}" ,Result ,Message); }
		}

		#endregion

		#region DLL Binding
		// ReSharper disable InconsistentNaming,UnusedMember.Local

		#if WP8_SQLITE
		public class Wp8Binding
		{
			private class Wp8Sqlite3Handle :sqlite3
			{
				public SqliteWp8.Database m_handle;

				/// <summary>The result from closing this handle</summary>
				public Result CloseResult { get; private set; }

				/// <summary>True if the handle is invalid</summary>
				public bool IsInvalid { get { return !m_handle.IsValid; } }

				/// <summary>True if the handle has been closed</summary>
				public bool IsClosed { get { return !m_handle.IsValid; } }

				/// <summary>Close the handle</summary>
				public void Close()
				{
					Trace.WriteLine("Releasing sqlite3 handle");
					CloseResult = (Result)SqliteWp8.Sqlite3.sqlite3_close(m_handle);
				}
			}

			private class Wp8Sqlite3StmtHandle :sqlite3_stmt
			{
				public SqliteWp8.Statement m_handle;

				/// <summary>The result from closing this handle</summary>
				public Result CloseResult { get; private set; }

				/// <summary>True if the handle is invalid</summary>
				public bool IsInvalid { get { return !m_handle.IsValid; } }

				/// <summary>True if the handle has been closed</summary>
				public bool IsClosed { get { return !m_handle.IsValid; } }

				/// <summary>Close the handle</summary>
				public void Close()
				{
					Trace.WriteLine("Releasing sqlite3_stmt handle");
					CloseResult = (Result)SqliteWp8.Sqlite3.sqlite3_finalize(m_handle);
				}
			}

			/// <summary>Converts an IntPtr that points to a null terminated UTF-8 string into a .NET string</summary>
			private static string UTF8toStr(IntPtr utf8ptr)
			{
				if (utf8ptr == IntPtr.Zero) return null;
				var str = Marshal.PtrToStringAnsi(utf8ptr);
				if (str == null) return null;
				var bytes = new byte[str.Length];
				Marshal.Copy(utf8ptr, bytes, 0, bytes.Length);
				return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
			}

			/// <summary>Converts a C# string (in UTF-16) to a byte array in UTF-8</summary>
			private static byte[] StrToUTF8(string str)
			{
				return Encoding.Convert(Encoding.Unicode, Encoding.UTF8, Encoding.Unicode.GetBytes(str));
			}

			/// <summary>Set a configuration setting for the database</summary>
			public static Result Config(ConfigOption option)
			{
				return (Result)SqliteWp8.Sqlite3.sqlite3_config((int)(m_config_option = option));
			}
			public static ConfigOption m_config_option = ConfigOption.SingleThread;

			/// <summary>Open a database file</summary>
			public static sqlite3 Open(string filepath, OpenFlags flags)
			{
				var db = new Wp8Sqlite3Handle();
				var res = (Result)SqliteWp8.Sqlite3.sqlite3_open_v2(filepath, out db.m_handle, (int)flags, String.Empty);
				if (res != Result.OK) throw Exception.New(res, "Failed to open database connection to file "+filepath);
				return db;
			}

			/// <summary>Set the busy wait timeout on the db</summary>
			public static void BusyTimeout(sqlite3 db, int milliseconds)
			{
				SqliteWp8.Sqlite3.sqlite3_busy_timeout(((Wp8Sqlite3Handle)db).m_handle, milliseconds);
			}

			/// <summary>Creates a prepared statement from an sql string</summary>
			public static sqlite3_stmt Prepare(sqlite3 db, string sql_string)
			{
				var stmt = new Wp8Sqlite3StmtHandle();
				var res = (Result)SqliteWp8.Sqlite3.sqlite3_prepare_v2(((Wp8Sqlite3Handle)db).m_handle, sql_string, out stmt.m_handle);
				if (res != Result.OK) throw Exception.New(res, string.Format("Error compiling sql string '{0}' "+Environment.NewLine+"Sqlite Error: {1}",sql_string, ErrorMsg(db)));
				return stmt;
			}

			/// <summary>Returns the number of rows changed by the last operation</summary>
			public static int Changes(sqlite3 db)
			{
				return SqliteWp8.Sqlite3.sqlite3_changes(((Wp8Sqlite3Handle)db).m_handle);
			}

			/// <summary>Returns the RowId for the last inserted row</summary>
			public static long LastInsertRowId(sqlite3 db)
			{
				return SqliteWp8.Sqlite3.sqlite3_last_insert_rowid(((Wp8Sqlite3Handle)db).m_handle);
			}

			/// <summary>Returns the error message for the last error returned from sqlite</summary>
			public static string ErrorMsg(sqlite3 db)
			{
				return SqliteWp8.Sqlite3.sqlite3_errmsg(((Wp8Sqlite3Handle)db).m_handle);
			}

			/// <summary>Reset a prepared statement</summary>
			public static void Reset(sqlite3_stmt stmt)
			{
				SqliteWp8.Sqlite3.sqlite3_reset(((Wp8Sqlite3StmtHandle)stmt).m_handle);
			}

			/// <summary>Step a prepared statement</summary>
			public static Result Step(sqlite3_stmt stmt)
			{
				return (Result)SqliteWp8.Sqlite3.sqlite3_step(((Wp8Sqlite3StmtHandle)stmt).m_handle);
			}

			/// <summary>Returns the string used to create a prepared statement</summary>
			public static string SqlString(sqlite3_stmt stmt)
			{
				throw new NotImplementedException();
			}

			/// <summary>Returns the name of the column with 0-based index 'index'</summary>
			public static string ColumnName(sqlite3_stmt stmt, int index)
			{
				return SqliteWp8.Sqlite3.sqlite3_column_name(((Wp8Sqlite3StmtHandle)stmt).m_handle, index);
			}

			/// <summary>Returns the number of columns in the result of a prepared statement</summary>
			public static int ColumnCount(sqlite3_stmt stmt)
			{
				return SqliteWp8.Sqlite3.sqlite3_column_count(((Wp8Sqlite3StmtHandle)stmt).m_handle);
			}

			/// <summary>Returns the internal data type for the column with 0-based index 'index'</summary>
			public static DataType ColumnType(sqlite3_stmt stmt, int index)
			{
				return (DataType)SqliteWp8.Sqlite3.sqlite3_column_type(((Wp8Sqlite3StmtHandle)stmt).m_handle, index);
			}

			/// <summary>Returns the value from the column with 0-based index 'index' as an int</summary>
			public static Int32 ColumnInt(sqlite3_stmt stmt, int index)
			{
				return SqliteWp8.Sqlite3.sqlite3_column_int(((Wp8Sqlite3StmtHandle)stmt).m_handle, index);
			}

			/// <summary>Returns the value from the column with 0-based index 'index' as an int64</summary>
			public static Int64 ColumnInt64(sqlite3_stmt stmt, int index)
			{
				return SqliteWp8.Sqlite3.sqlite3_column_int64(((Wp8Sqlite3StmtHandle)stmt).m_handle, index);
			}

			/// <summary>Returns the value from the column with 0-based index 'index' as a double</summary>
			public static Double ColumnDouble(sqlite3_stmt stmt, int index)
			{
				return SqliteWp8.Sqlite3.sqlite3_column_double(((Wp8Sqlite3StmtHandle)stmt).m_handle, index);
			}

			/// <summary>Returns the value from the column with 0-based index 'index' as a string</summary>
			public static String ColumnString(sqlite3_stmt stmt, int index)
			{
				var ptr = SqliteWp8.Sqlite3.sqlite3_column_text(((Wp8Sqlite3StmtHandle)stmt).m_handle, index);
				return ptr;
			}

			/// <summary>Returns the value from the column with 0-based index 'index' as an IntPtr</summary>
			public static void ColumnBlob(sqlite3_stmt stmt, int index, out IntPtr ptr, out int len)
			{
				throw new NotImplementedException();
			}

			/// <summary>Returns the size of the data in the column with 0-based index 'index'</summary>
			public static int ColumnBytes(sqlite3_stmt stmt, int index)
			{
				return SqliteWp8.Sqlite3.sqlite3_column_bytes(((Wp8Sqlite3StmtHandle)stmt).m_handle, index);
			}

			/// <summary>Return the number of parameters in a prepared statement</summary>
			public static int BindParameterCount(sqlite3_stmt stmt)
			{
				throw new NotImplementedException();
			}

			/// <summary>Return the index for the parameter named 'name'</summary>
			public static int BindParameterIndex(sqlite3_stmt stmt, string name)
			{
				return SqliteWp8.Sqlite3.sqlite3_bind_parameter_index(((Wp8Sqlite3StmtHandle)stmt).m_handle, name);
			}

			/// <summary>Return the name of a parameter from its index</summary>
			public static string BindParameterName(sqlite3_stmt stmt, int index)
			{
				throw new NotImplementedException();
			}

			/// <summary>Bind null to 1-based parameter index 'index'</summary>
			public static void BindNull(sqlite3_stmt stmt, int index)
			{
				SqliteWp8.Sqlite3.sqlite3_bind_null(((Wp8Sqlite3StmtHandle)stmt).m_handle, index);
			}

			/// <summary>Bind an integer value to 1-based parameter index 'index'</summary>
			public static void BindInt(sqlite3_stmt stmt, int index, int val)
			{
				SqliteWp8.Sqlite3.sqlite3_bind_int(((Wp8Sqlite3StmtHandle)stmt).m_handle, index, val);
			}

			/// <summary>Bind an integer64 value to 1-based parameter index 'index'</summary>
			public static void BindInt64(sqlite3_stmt stmt, int index, long val)
			{
				SqliteWp8.Sqlite3.sqlite3_bind_int64(((Wp8Sqlite3StmtHandle)stmt).m_handle, index, val);
			}

			/// <summary>Bind a double value to 1-based parameter index 'index'</summary>
			public static void BindDouble(sqlite3_stmt stmt, int index, double val)
			{
				SqliteWp8.Sqlite3.sqlite3_bind_double(((Wp8Sqlite3StmtHandle)stmt).m_handle, index, val);
			}

			/// <summary>Bind a string to 1-based parameter index 'index'</summary>
			public static void BindText(sqlite3_stmt stmt, int index, string val)
			{
				SqliteWp8.Sqlite3.sqlite3_bind_text(((Wp8Sqlite3StmtHandle)stmt).m_handle, index, val, -1);
			}

			/// <summary>Bind a byte array to 1-based parameter index 'index'</summary>
			public static void BindBlob(sqlite3_stmt stmt, int index, byte[] val, int length)
			{
				var r = (Result)SqliteWp8.Sqlite3.sqlite3_bind_blob(((Wp8Sqlite3StmtHandle)stmt).m_handle, index, val, length);
				if (r != Result.OK) throw Exception.New(r, "Bind blob failed");
			}

			/// <summary>Set the update hook callback function</summary>
			public static void UpdateHook(sqlite3 db, UpdateHookCB cb, IntPtr ctx)
			{
				m_wp8_callback = (context, chg_type, db_name, table_name, row_id) => cb(context, chg_type, db_name, table_name, row_id);
				try
				{
					SqliteWp8.Sqlite3.sqlite3_update_hook(((Wp8Sqlite3Handle)db).m_handle, m_wp8_callback, ctx);
				}
				catch (MarshalDirectiveException e)
				{
					Log.Exception(null, "Exception thrown when call sqlite3_update_hook", e);
				}
			}
			private static SqliteWp8.UpdateHookCB m_wp8_callback;
		}
		#else
		public class NativeBinding
		{
			private const string SqliteDll =  "sqlite3";
			private static IntPtr m_module;

			/// <summary>Helper method for loading the view3d.dll from a platform specific path</summary>
			public static void LoadDll(string dir = @".\lib\$(platform)")
			{
				if (m_module != IntPtr.Zero)
					return; // Already loaded

				var dllname = SqliteDll+".dll";
				var dllpath = dllname;

				// Try the lib folder. Load the appropriate dll for the platform
				dir = dir.Replace("$(platform)", Environment.Is64BitProcess ? "x64" : "x86");
				#if DEBUG
				dir = dir.Replace("$(config)", "debug");
				#else
				dir = dir.Replace("$(config)", "release");
				#endif
				dllpath = Path.Combine(dir, dllname);
				if (PathEx.FileExists(dllpath))
				{
					m_module = LoadLibrary(dllpath);
					if (m_module != IntPtr.Zero)
						return;
				}

				// Try the local directory
				if (PathEx.FileExists(dllpath))
				{
					m_module = LoadLibrary(dllpath);
					if (m_module != IntPtr.Zero)
						return;
				}

				throw new DllNotFoundException(string.Format("Failed to load dependency '{0}'",dllname));
			}
			[DllImport("Kernel32.dll")] private static extern IntPtr LoadLibrary(string path);

			/// <summary>Based class for wrappers of native sqlite handles</summary>
			private abstract class SQLiteHandle :SafeHandle
			{
				/// <summary>The result from the 'sqlite3_close' call</summary>
				public Result CloseResult { get; set; }

				/// <summary>Default constructor contains an owned, but invalid handle</summary>
				protected SQLiteHandle() :base(IntPtr.Zero, true)
				{}

				/// <summary>Internal constructor used to create safehandles that optionally own the handle</summary>
				protected SQLiteHandle(IntPtr initial_ptr, bool owns_handle) :base(IntPtr.Zero, owns_handle)
				{
					CloseResult = Result.Empty; // Initialise to 'empty' so we know when its been set
					SetHandle(initial_ptr);
				}

				/// <summary>True if the contained handle is invalid</summary>
				public override bool IsInvalid
				{
					get { return handle == IntPtr.Zero; }
				}
			}

			/// <summary>A wrapper for unmanaged sqlite database connection handles</summary>
			private class NativeSqlite3Handle :SQLiteHandle ,sqlite3
			{
				public NativeSqlite3Handle() {}
				public NativeSqlite3Handle(IntPtr handle, bool owns_handle) :base(handle, owns_handle) {}

				/// <summary>Frees the handle.</summary>
				protected override bool ReleaseHandle()
				{
					Trace.WriteLine(ETrace.Handles, string.Format("Releasing sqlite3 handle ({0})", handle));
					CloseResult = sqlite3_close(handle);
					handle = IntPtr.Zero;
					return true;
				}
			}

			/// <summary>A wrapper for unmanaged sqlite prepared statement handles</summary>
			private class NativeSqlite3StmtHandle :SQLiteHandle, sqlite3_stmt
			{
				public NativeSqlite3StmtHandle() {}
				public NativeSqlite3StmtHandle(IntPtr handle, bool owns_handle) :base(handle, owns_handle) {}

				/// <summary>Frees the handle.</summary>
				protected override bool ReleaseHandle()
				{
					Trace.WriteLine(ETrace.Handles, string.Format("Releasing sqlite3_stmt handle ({0})", handle));
					CloseResult = sqlite3_finalize(handle);
					handle = IntPtr.Zero;
					return true;
				}
			}

			/// <summary>Converts an IntPtr that points to a null terminated UTF-8 string into a .NET string</summary>
			public static string UTF8toStr(IntPtr utf8ptr)
			{
				if (utf8ptr == IntPtr.Zero)
					return null;

				// There is no Marshal.PtrToStringUtf8 unfortunately, so we have to do it manually
				// Read up to (but not including) the null terminator
				byte b; var buf = new List<byte>(256);
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

			private static readonly IntPtr TransientData = new IntPtr(-1);
			private delegate void sqlite3_destructor_type(IntPtr ptr);

			public static string LibVersion()
			{
				return Marshal.PtrToStringAnsi(sqlite3_libversion());
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_libversion", CallingConvention = CallingConvention.Cdecl)]
			private static extern IntPtr sqlite3_libversion();

			public static string SourceId()
			{
				return Marshal.PtrToStringAnsi(sqlite3_sourceid());
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_sourceid", CallingConvention = CallingConvention.Cdecl)]
			private static extern IntPtr sqlite3_sourceid();

			public static int LibVersionNumber()
			{
				return sqlite3_libversion_number();
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_libversion_number", CallingConvention = CallingConvention.Cdecl)]
			private static extern int sqlite3_libversion_number();

			/// <summary>True if asynchronous support is enabled (via separate db connections)</summary>
			public static bool ThreadSafe
			{
				get { return sqlite3_threadsafe() != 0 && m_config_option != ConfigOption.SingleThread; }
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_libversion_number", CallingConvention = CallingConvention.Cdecl)]
			private static extern int sqlite3_threadsafe();

			[DllImport(SqliteDll, EntryPoint = "sqlite3_close", CallingConvention=CallingConvention.Cdecl)]
			private static extern Result sqlite3_close(IntPtr db);

			[DllImport(SqliteDll, EntryPoint = "sqlite3_finalize", CallingConvention=CallingConvention.Cdecl)]
			private static extern Result sqlite3_finalize(IntPtr stmt);

			[DllImport(SqliteDll, EntryPoint = "sqlite3_free", CallingConvention=CallingConvention.Cdecl)]
			private static extern void sqlite3_free(IntPtr ptr);

			[DllImport(SqliteDll, EntryPoint = "sqlite3_db_handle", CallingConvention=CallingConvention.Cdecl)]
			private static extern NativeSqlite3Handle sqlite3_db_handle(NativeSqlite3StmtHandle stmt);

			[DllImport(SqliteDll, EntryPoint = "sqlite3_limit", CallingConvention=CallingConvention.Cdecl)]
			private static extern int sqlite3_limit(NativeSqlite3Handle db, Limit limit_category, int new_value);

			/// <summary>Set a configuration setting for the database</summary>
			public static Result Config(ConfigOption option)
			{
				if (sqlite3_threadsafe() == 0 && option != ConfigOption.SingleThread)
					throw Exception.New(Result.Misuse, "sqlite3 dll compiled with SQLITE_THREADSAFE=0, multithreading cannot be used");

				return sqlite3_config(m_config_option = option);
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_config", CallingConvention=CallingConvention.Cdecl)]
			private static extern Result sqlite3_config(ConfigOption option);

			/// <summary>Return the threading mode (config option) that the dll has been set to</summary>
			public static ConfigOption ThreadingMode { get { return m_config_option; } }
			private static ConfigOption m_config_option = ConfigOption.SingleThread;

			/// <summary>Open a database file</summary>
			public static sqlite3 Open(string filepath, OpenFlags flags)
			{
				if ((flags & OpenFlags.FullMutex) != 0 && !ThreadSafe)
					throw Exception.New(Result.Misuse, "sqlite3 dll compiled with SQLITE_THREADSAFE=0, multithreading cannot be used");

				NativeSqlite3Handle db;
				var res = sqlite3_open_v2(filepath, out db, (int)flags, IntPtr.Zero);
				if (res != Result.OK) throw Exception.New(res, "Failed to open database connection to file "+filepath);
				return db;
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_open_v2", CallingConvention=CallingConvention.Cdecl)]
			private static extern Result sqlite3_open_v2(string filepath, out NativeSqlite3Handle db, int flags, IntPtr zvfs);

			/// <summary>Set the busy wait timeout on the db</summary>
			public static void BusyTimeout(sqlite3 db, int milliseconds)
			{
				var r = sqlite3_busy_timeout((NativeSqlite3Handle)db, milliseconds);
				if (r != Result.OK) throw Exception.New(r, "Failed to set the busy timeout to "+milliseconds+"ms");
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_busy_timeout", CallingConvention=CallingConvention.Cdecl)]
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
					Log.Debug(null, msg);
					throw Exception.New(res, msg);
				}
				return stmt;
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_prepare_v2", CallingConvention=CallingConvention.Cdecl)]
			private static extern Result sqlite3_prepare_v2(NativeSqlite3Handle db, byte[] sql, int num_bytes, out NativeSqlite3StmtHandle stmt, IntPtr pzTail);

			/// <summary>Returns the number of rows changed by the last operation</summary>
			public static int Changes(sqlite3 db)
			{
				return sqlite3_changes((NativeSqlite3Handle)db);
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_changes", CallingConvention=CallingConvention.Cdecl)]
			private static extern int sqlite3_changes(NativeSqlite3Handle db);

			/// <summary>Returns the RowId for the last inserted row</summary>
			public static long LastInsertRowId(sqlite3 db)
			{
				return sqlite3_last_insert_rowid((NativeSqlite3Handle)db);
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_last_insert_rowid", CallingConvention=CallingConvention.Cdecl)]
			private static extern long sqlite3_last_insert_rowid(NativeSqlite3Handle db);

			/// <summary>Returns the error message for the last error returned from sqlite</summary>
			public static string ErrorMsg(sqlite3 db)
			{
				// sqlite3 manages the memory allocated for the returned error message
				// so we don't need to call sqlite_free on the returned pointer
				return Marshal.PtrToStringUni(sqlite3_errmsg16((NativeSqlite3Handle)db));
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_errmsg16", CallingConvention=CallingConvention.Cdecl)]
			private static extern IntPtr sqlite3_errmsg16(NativeSqlite3Handle db);

			/// <summary>Reset a prepared statement</summary>
			public static void Reset(sqlite3_stmt stmt)
			{
				// The result from 'sqlite3_reset' reflects the error code of the last 'sqlite3_step'
				// call. This is legacy behaviour, now step returns the error code immediately. For
				// this reason we can ignore the error code returned by reset.
				sqlite3_reset((NativeSqlite3StmtHandle)stmt);
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_reset", CallingConvention=CallingConvention.Cdecl)]
			private static extern Result sqlite3_reset(NativeSqlite3StmtHandle stmt);

			/// <summary>Step a prepared statement</summary>
			public static Result Step(sqlite3_stmt stmt)
			{
				return sqlite3_step((NativeSqlite3StmtHandle)stmt);
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_step", CallingConvention=CallingConvention.Cdecl)]
			private static extern Result sqlite3_step(NativeSqlite3StmtHandle stmt);

			/// <summary>Returns the string used to create a prepared statement</summary>
			public static string SqlString(sqlite3_stmt stmt)
			{
				return UTF8toStr(sqlite3_sql((NativeSqlite3StmtHandle)stmt)); // this assumes sqlite3_prepare_v2 was used to create 'stmt'
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_sql", CallingConvention=CallingConvention.Cdecl)]
			private static extern IntPtr sqlite3_sql(NativeSqlite3StmtHandle stmt);

			/// <summary>Returns the name of the column with 0-based index 'index'</summary>
			public static string ColumnName(sqlite3_stmt stmt, int index)
			{
				return Marshal.PtrToStringUni(sqlite3_column_name16((NativeSqlite3StmtHandle)stmt, index));
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_column_name16", CallingConvention=CallingConvention.Cdecl)]
			private static extern IntPtr sqlite3_column_name16(NativeSqlite3StmtHandle stmt, int index);

			/// <summary>Returns the number of columns in the result of a prepared statement</summary>
			public static int ColumnCount(sqlite3_stmt stmt)
			{
				return sqlite3_column_count((NativeSqlite3StmtHandle)stmt);
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_column_count", CallingConvention=CallingConvention.Cdecl)]
			private static extern int sqlite3_column_count(NativeSqlite3StmtHandle stmt);

			/// <summary>Returns the internal data type for the column with 0-based index 'index'</summary>
			public static DataType ColumnType(sqlite3_stmt stmt, int index)
			{
				return sqlite3_column_type((NativeSqlite3StmtHandle)stmt, index);
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_column_type", CallingConvention=CallingConvention.Cdecl)]
			private static extern DataType sqlite3_column_type(NativeSqlite3StmtHandle stmt, int index);

			/// <summary>Returns the value from the column with 0-based index 'index' as an int</summary>
			public static Int32 ColumnInt(sqlite3_stmt stmt, int index)
			{
				return sqlite3_column_int((NativeSqlite3StmtHandle)stmt, index);
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_column_int", CallingConvention=CallingConvention.Cdecl)]
			private static extern Int32 sqlite3_column_int(NativeSqlite3StmtHandle stmt, int index);

			/// <summary>Returns the value from the column with 0-based index 'index' as an int64</summary>
			public static Int64 ColumnInt64(sqlite3_stmt stmt, int index)
			{
				return sqlite3_column_int64((NativeSqlite3StmtHandle)stmt, index);
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_column_int64", CallingConvention=CallingConvention.Cdecl)]
			private static extern Int64 sqlite3_column_int64(NativeSqlite3StmtHandle stmt, int index);

			/// <summary>Returns the value from the column with 0-based index 'index' as a double</summary>
			public static Double ColumnDouble(sqlite3_stmt stmt, int index)
			{
				return sqlite3_column_double((NativeSqlite3StmtHandle)stmt, index);
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_column_double", CallingConvention=CallingConvention.Cdecl)]
			private static extern Double sqlite3_column_double(NativeSqlite3StmtHandle stmt, int index);

			/// <summary>Returns the value from the column with 0-based index 'index' as a string</summary>
			public static String ColumnString(sqlite3_stmt stmt, int index)
			{
				var ptr = sqlite3_column_text16((NativeSqlite3StmtHandle)stmt, index); // Sqlite returns null if this column is null
				if (ptr != IntPtr.Zero) return Marshal.PtrToStringUni(ptr);
				return string.Empty;
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_column_text16", CallingConvention=CallingConvention.Cdecl)]
			private static extern IntPtr sqlite3_column_text16(NativeSqlite3StmtHandle stmt, int index);

			/// <summary>Returns the value from the column with 0-based index 'index' as an IntPtr</summary>
			public static void ColumnBlob(sqlite3_stmt stmt, int index, out IntPtr ptr, out int len)
			{
				// Read the blob size limit
				var db = sqlite3_db_handle((NativeSqlite3StmtHandle)stmt);
				var max_size = sqlite3_limit(db, Limit.Length, -1);

				// sqlite returns null if this column is null
				ptr = sqlite3_column_blob((NativeSqlite3StmtHandle)stmt, index); // have to call this first
				len = sqlite3_column_bytes((NativeSqlite3StmtHandle)stmt, index);
				if (len < 0 || len > max_size) throw Exception.New(Result.Corrupt, "Blob data size exceeds database maximum size limit");
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_column_blob", CallingConvention=CallingConvention.Cdecl)]
			private static extern IntPtr sqlite3_column_blob(NativeSqlite3StmtHandle stmt, int index);

			/// <summary>Returns the size of the data in the column with 0-based index 'index'</summary>
			public static int ColumnBytes(sqlite3_stmt stmt, int index)
			{
				return sqlite3_column_bytes((NativeSqlite3StmtHandle)stmt, index);
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_column_bytes", CallingConvention=CallingConvention.Cdecl)]
			private static extern int sqlite3_column_bytes(NativeSqlite3StmtHandle stmt, int index);

			/// <summary>Return the number of parameters in a prepared statement</summary>
			public static int BindParameterCount(sqlite3_stmt stmt)
			{
				return sqlite3_bind_parameter_count((NativeSqlite3StmtHandle)stmt);
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_bind_parameter_count", CallingConvention=CallingConvention.Cdecl)]
			private static extern int sqlite3_bind_parameter_count(NativeSqlite3StmtHandle stmt);

			/// <summary>Return the index for the parameter named 'name'</summary>
			public static int BindParameterIndex(sqlite3_stmt stmt, string name)
			{
				return sqlite3_bind_parameter_index((NativeSqlite3StmtHandle)stmt, name);
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_bind_parameter_index", CallingConvention=CallingConvention.Cdecl)]
			private static extern int sqlite3_bind_parameter_index(NativeSqlite3StmtHandle stmt, string name);

			/// <summary>Return the name of a parameter from its index</summary>
			public static string BindParameterName(sqlite3_stmt stmt, int index)
			{
				return UTF8toStr(sqlite3_bind_parameter_name((NativeSqlite3StmtHandle)stmt, index));
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_bind_parameter_name", CallingConvention=CallingConvention.Cdecl)]
			private static extern IntPtr sqlite3_bind_parameter_name(NativeSqlite3StmtHandle stmt, int index);

			/// <summary>Bind null to 1-based parameter index 'index'</summary>
			public static void BindNull(sqlite3_stmt stmt, int index)
			{
				var r = sqlite3_bind_null((NativeSqlite3StmtHandle)stmt, index);
				if (r != Result.OK) throw Exception.New(r, "Bind null failed");
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_bind_null", CallingConvention=CallingConvention.Cdecl)]
			private static extern Result sqlite3_bind_null(NativeSqlite3StmtHandle stmt, int index);

			/// <summary>Bind an integer value to 1-based parameter index 'index'</summary>
			public static void BindInt(sqlite3_stmt stmt, int index, int val)
			{
				var r = sqlite3_bind_int((NativeSqlite3StmtHandle)stmt, index, val);
				if (r != Result.OK) throw Exception.New(r, "Bind int failed");
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_bind_int", CallingConvention=CallingConvention.Cdecl)]
			private static extern Result sqlite3_bind_int(NativeSqlite3StmtHandle stmt, int index, int val);

			/// <summary>Bind an integer64 value to 1-based parameter index 'index'</summary>
			public static void BindInt64(sqlite3_stmt stmt, int index, long val)
			{
				var r = sqlite3_bind_int64((NativeSqlite3StmtHandle)stmt, index, val);
				if (r != Result.OK) throw Exception.New(r, "Bind int64 failed");
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_bind_int64", CallingConvention=CallingConvention.Cdecl)]
			private static extern Result sqlite3_bind_int64(NativeSqlite3StmtHandle stmt, int index, long val);

			/// <summary>Bind a double value to 1-based parameter index 'index'</summary>
			public static void BindDouble(sqlite3_stmt stmt, int index, double val)
			{
				var r = sqlite3_bind_double((NativeSqlite3StmtHandle)stmt, index, val);
				if (r != Result.OK) throw Exception.New(r, "Bind double failed");
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_bind_double", CallingConvention=CallingConvention.Cdecl)]
			private static extern Result sqlite3_bind_double(NativeSqlite3StmtHandle stmt, int index, double val);

			/// <summary>Bind a string to 1-based parameter index 'index'</summary>
			public static void BindText(sqlite3_stmt stmt, int index, string val)
			{
				var r = sqlite3_bind_text16((NativeSqlite3StmtHandle)stmt, index, val, -1, TransientData);
				if (r != Result.OK) throw Exception.New(r, "Bind string failed");
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_bind_text16", CallingConvention=CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			private static extern Result sqlite3_bind_text16(NativeSqlite3StmtHandle stmt, int index, string val, int n, IntPtr destructor_cb);

			/// <summary>Bind a byte array to 1-based parameter index 'index'</summary>
			public static void BindBlob(sqlite3_stmt stmt, int index, byte[] val, int length)
			{
				var r = sqlite3_bind_blob((NativeSqlite3StmtHandle)stmt, index, val, length, TransientData);
				if (r != Result.OK) throw Exception.New(r, "Bind blob failed");
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_bind_blob", CallingConvention=CallingConvention.Cdecl)]
			private static extern Result sqlite3_bind_blob(NativeSqlite3StmtHandle stmt, int index, byte[] val, int n, IntPtr destructor_cb);

			/// <summary>Set the update hook callback function</summary>
			public static void UpdateHook(sqlite3 db, UpdateHookCB cb, IntPtr ctx)
			{
				sqlite3_update_hook((NativeSqlite3Handle)db, cb, ctx);
			}
			[DllImport(SqliteDll, EntryPoint = "sqlite3_update_hook", CallingConvention=CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
			private static extern IntPtr sqlite3_update_hook(NativeSqlite3Handle db, UpdateHookCB cb, IntPtr ctx);
		}
		#endif

		// ReSharper restore InconsistentNaming,UnusedMember.Local
		#endregion

		#region SQLITE_TRACE
		[Flags] private enum ETrace
		{
			None        = 0,
			Handles     = 1 << 0,
			Tables      = 1 << 1,
			Query       = 1 << 3,
			ObjectCache = 1 << 4,
			QueryCache  = 1 << 5,
		}
		private static class Trace
		{
			/// <summary>Used to filter out specific trace messages</summary>
			private const ETrace TraceFilter = ETrace.None
				|ETrace.Handles
				|ETrace.Tables
				|ETrace.Query
				//|ETrace.ObjectCache
				//|ETrace.QueryCache
				;

			private class Info
			{
				public string CallStack { get; private set; }
				public bool InUse { get; set; }
				public Info() { CallStack = new StackTrace().ToString(); }
			}
			private static readonly Dictionary<Query, Info> m_trace = new Dictionary<Query,Info>();

			/// <summary>Records the creation of a new Query object</summary>
			[Conditional("SQLITE_TRACE")] public static void QueryCreated(Query query)
			{
				m_trace.Add(query, new Info());
			}

			/// <summary>Records the destruction of a Query object</summary>
			[Conditional("SQLITE_TRACE")] public static void QueryClosed(Query query)
			{
				m_trace.Remove(query);
			}

			/// <summary>Records the destruction of a Query object</summary>
			[Conditional("SQLITE_TRACE")] public static void QueryInUse(Query query, bool in_use)
			{
				m_trace[query].InUse = in_use;
			}

			/// <summary>Dumps the existing Query objects to standard error</summary>
			[Conditional("SQLITE_TRACE")] public static void Dump(bool in_use_only)
			{
				if (m_trace.Count == 0) return;
				var sb = new StringBuilder();
				Dump(sb, in_use_only);
				Debug.WriteLine(sb.ToString());
			}

			/// <summary>Dumps the existing Query objects to 'sb'</summary>
			[Conditional("SQLITE_TRACE")] public static void Dump(StringBuilder sb, bool in_use_only)
			{
				if (m_trace.Count == 0) return;
				foreach (var t in m_trace)
				{
					if (in_use_only && !t.Value.InUse) continue;
					sb.AppendLine().Append("Query:").AppendLine().Append(t.Value.CallStack);
				}
			}

			/// <summary>Write trace information to the debug output window</summary>
			[Conditional("SQLITE_TRACE")] public static void WriteLine(ETrace trace, string str)
			{
				if ((trace & TraceFilter) != 0)
					Log.Debug(null, string.Format("[SQLite]: {0}", str));
			}
		}
		#endregion
	}

	#region SqliteLogger

	/// <summary>Map the framework logger to the Sqlite.ILog</summary>
	public class SqliteLogger :Sqlite.ILog
	{
		/// <summary>Log an exception with the specified sender, message and exception details.</summary>
		void Sqlite.ILog.Exception(object sender, Exception e, string message) { Sqlite.Log.Exception(sender, e, message); }

		/// <summary>Log an info message with the specified sender and formatted message</summary>
		void Sqlite.ILog.Info(object sender, string message) { Sqlite.Log.Info(sender, message); }

		/// <summary>Log a debug message with the specified sender and formatted message</summary>
		void Sqlite.ILog.Debug(object sender, string message) { Sqlite.Log.Debug(sender, message); }
	}

	#endregion
}

// ReSharper restore AccessToStaticMemberViaDerivedType

#if PR_UNITTESTS
namespace pr.unittests
{
	using System.Data.Linq.SqlClient;
	using System.IO;
	using common;
	using util;

	[TestFixture] public class TestSqlite
	{
		private static string FilePath;
		private const string InMemoryDB = ":memory:";

		#region DomTypes
		// ReSharper disable FieldCanBeMadeReadOnly.Local,MemberCanBePrivate.Local,UnusedMember.Local,NotAccessedField.Local,ValueParameterNotUsed
		public enum SomeEnum { One = 1, Two = 2, Three = 3 }

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

		public interface IDomType0
		{
			int      Inc_Key  { get; set; }
			SomeEnum Inc_Enum { get; set; }
		}

		// Tests a basic default table
		private class DomType0 :IDomType0
		{
			[Sqlite.Column(PrimaryKey = true)] public int Inc_Key { get; set; }
			public  string   Inc_Value               { get; set; }
			public SomeEnum  Inc_Enum                { get; set; }
			public  float    Ign_NoGetter            { set { } }
			public  int      Ign_NoSetter            { get { return 42; } }
			private bool     Ign_PrivateProp         { get; set; }
			private int      m_ign_private_field;
			public  short    Ign_PublicField;
			public DomType0(){}
			public DomType0(ref int key, int seed)
			{
				Inc_Key = ++key;
				Inc_Value = seed.ToString(CultureInfo.InvariantCulture);
				Inc_Enum = (SomeEnum)((seed % 3) + 1);
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
			[Sqlite.Column(Order=17)] public int[]          m_int_buf;
			[Sqlite.Column(Order=18)] public Guid           m_guid;
			[Sqlite.Column(Order=19)] public SomeEnum       m_enum;
			[Sqlite.Column(Order=20)] public SomeEnum?      m_nullenum;
			[Sqlite.Column(Order=21)] public DateTimeOffset m_dt_offset;
			[Sqlite.Column(Order=22, SqlDataType = Sqlite.DataType.Text)] public Custom m_custom;
			[Sqlite.Column(Order=23)] public int?           m_nullint;
			[Sqlite.Column(Order=24)] public long?          m_nulllong;

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
				m_int_buf   = new[]{0x10000000,0x20000000,0x30000000,0x40000000};
				m_guid      = Guid.NewGuid();
				m_enum      = SomeEnum.One;
				m_nullenum  = SomeEnum.Two;
				m_dt_offset = DateTimeOffset.UtcNow;
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
				if (!Equals(other.m_int_buf, m_int_buf)     ) return false;
				if (!Equals(other.m_enum, m_enum)           ) return false;
				if (!Equals(other.m_nullenum, m_nullenum)   ) return false;
				if (!other.m_guid.Equals(m_guid)            ) return false;
				if (!other.m_dt_offset.Equals(m_dt_offset)  ) return false;
				if (!other.m_custom.Equals(m_custom)        ) return false;
				if (Math.Abs(other.m_float   - m_float  ) > float .Epsilon) return false;
				if (Math.Abs(other.m_double  - m_double ) > double.Epsilon) return false;
				return true;
			}
			private static bool Equals<T>(T[] arr1, T[] arr2)
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
		[Sqlite.Table(Constraints = "primary key ([Index], [Key2], [Key3])")]
		[Sqlite.IgnoreColumns("Ignored1")]
		public partial class DomType3
		{
			public int    Index { get; set; }
			public bool   Key2 { get; set; }
			public string Prop1 { get; set; }
			public float  Prop2 { get; set; }
			public Guid   Prop3 { get; set; }
			public float Ignored1 { get; set; }

			public DomType3(){}
			public DomType3(int key1, bool key2, string key3)
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
			public bool Equals(DomType3 other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				if (other.Index    != Index   ) return false;
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
			[Sqlite.Column(PrimaryKey = true)] public int Index { get; set; }
			public bool   Key2 { get; set; }
			public string Prop1 { get; set; }
			public float  Prop2 { get; set; }
			public Guid   Prop3 { get; set; }
			public int    NewProp { get; set; }
		}

		// Tests inherited Sqlite attributes
		[Sqlite.Table(PrimaryKey = "PK", PKAutoInc = true)]
		public class DomType5Base
		{
			public int PK { get; set; }
		}
		public class DomType5 :DomType5Base
		{
			public string Data { get; set; }
			[Sqlite.Ignore] public int Tmp { get; set; }
			public DomType5() {}
			public DomType5(string data) { Data = data; }
		}

		// ReSharper restore FieldCanBeMadeReadOnly.Local,MemberCanBePrivate.Local,UnusedMember.Local,NotAccessedField.Local,ValueParameterNotUsed
		#endregion

		[TestFixtureSetUp] public void Setup()
		{
			Sqlite.Dll.LoadDll(@"p:\sdk\sqlite\lib\$(platform)\$(config)");

			// Register custom type bind/read methods
			Sqlite.Bind.FunctionMap.Add(typeof(Custom), Custom.SqliteBind);
			Sqlite.Read.FunctionMap.Add(typeof(Custom), Custom.SqliteRead);

			// Use single threading
			Sqlite.Configure(Sqlite.ConfigOption.SingleThread);

			// Use an in-memory db for normal unit tests,
			// use an actual file when debugging
			FilePath = InMemoryDB;
			//FilePath = new FileInfo("tmpDB.db").FullName;
		}
		[TestFixtureTearDown] public void Cleanup()
		{
			if (FilePath != InMemoryDB)
				File.Delete(FilePath);
		}
		[Test] public void DefaultUse()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create|Sqlite.OpenFlags.ReadWrite|Sqlite.OpenFlags.NoMutex))
			{
				// Create a simple table
				db.DropTable<DomType0>();
				db.CreateTable<DomType0>();
				Assert.True(db.TableExists<DomType0>());

				// Check the table
				var table = db.Table<DomType0>();
				Assert.AreEqual(3, table.ColumnCount);
				var column_names = new[]{"Inc_Key", "Inc_Value", "Inc_Enum"};
				using (var q = db.Query("select * from "+table.Name))
				{
					Assert.AreEqual(3, q.ColumnCount);
					Assert.True(column_names.Contains(q.ColumnName(0)));
					Assert.True(column_names.Contains(q.ColumnName(1)));
					Assert.True(column_names.Contains(q.ColumnName(2)));
				}

				// Create some objects to stick in the table
				int key = 0;
				var obj1 = new DomType0(ref key, 5);
				var obj2 = new DomType0(ref key, 6);
				var obj3 = new DomType0(ref key, 7);

				// Insert stuff
				Assert.AreEqual(1, table.Insert(obj1));
				Assert.AreEqual(1, table.Insert(obj2));
				Assert.AreEqual(1, table.Insert(obj3));
				Assert.AreEqual(3, table.RowCount);

				string sql_count = "select count(*) from "+table.Name;
				using (var q = db.Query(sql_count))
					Assert.AreEqual(sql_count, q.SqlString);
			}
		}
		[Test] public void TypicalUse()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create|Sqlite.OpenFlags.ReadWrite|Sqlite.OpenFlags.NoMutex))
			{
				// Create a table
				db.DropTable<DomType1>();
				db.CreateTable<DomType1>();
				Assert.True(db.TableExists<DomType1>());

				// Check the table
				var table = db.Table<DomType1>();
				Assert.AreEqual(25, table.ColumnCount);
				using (var q = db.Query("select * from "+table.Name))
				{
					Assert.AreEqual(25, q.ColumnCount);
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
					Assert.AreEqual("m_int_buf"   ,q.ColumnName(17));
					Assert.AreEqual("m_guid"      ,q.ColumnName(18));
					Assert.AreEqual("m_enum"      ,q.ColumnName(19));
					Assert.AreEqual("m_nullenum"  ,q.ColumnName(20));
					Assert.AreEqual("m_dt_offset" ,q.ColumnName(21));
					Assert.AreEqual("m_custom"    ,q.ColumnName(22));
					Assert.AreEqual("m_nullint"   ,q.ColumnName(23));
					Assert.AreEqual("m_nulllong"  ,q.ColumnName(24));
				}

				// Create some objects to stick in the table
				var obj1 = new DomType1(5);
				var obj2 = new DomType1(6);
				var obj3 = new DomType1(7);
				obj2.m_dt_offset = DateTimeOffset.UtcNow;

				// Insert stuff
				Assert.AreEqual(1, table.Insert(obj1));
				Assert.AreEqual(1, table.Insert(obj2));
				Assert.AreEqual(1, table.Insert(obj3));
				Assert.AreEqual(3, table.RowCount);

				// Check Get() throws and Find() returns null if not found
				Assert.Null(table.Find(0));
				Sqlite.Exception err = null;
				try { table.Get(4); } catch (Sqlite.Exception ex) { err = ex; }
				Assert.True(err != null && err.Result == Sqlite.Result.NotFound);

				// Get stuff and check it's the same
				var OBJ1 = table.Get(obj1.m_key);
				var OBJ2 = table.Get(obj2.m_key);
				var OBJ3 = table.Get(obj3.m_key);
				Assert.True(obj1.Equals(OBJ1));
				Assert.True(obj2.Equals(OBJ2));
				Assert.True(obj3.Equals(OBJ3));

				// Check parameter binding
				using (var q = db.Query(Sqlite.Sql("select m_string,m_int from ",table.Name," where m_string = @p1 and m_int = @p2")))
				{
					Assert.AreEqual(2, q.ParmCount);
					Assert.AreEqual("@p1", q.ParmName(1));
					Assert.AreEqual("@p2", q.ParmName(2));
					Assert.AreEqual(1, q.ParmIndex("@p1"));
					Assert.AreEqual(2, q.ParmIndex("@p2"));
					q.BindParm(1, "string");
					q.BindParm(2, 12345678);

					// Run the query
					Assert.True(q.Step());

					// Read the results
					Assert.AreEqual(2, q.ColumnCount);
					Assert.AreEqual(Sqlite.DataType.Text    ,q.ColumnType(0));
					Assert.AreEqual(Sqlite.DataType.Integer ,q.ColumnType(1));
					Assert.AreEqual("m_string"              ,q.ColumnName(0));
					Assert.AreEqual("m_int"                 ,q.ColumnName(1));
					Assert.AreEqual("string"                ,q.ReadColumn<string>(0));
					Assert.AreEqual(12345678                ,q.ReadColumn<int>(1));

					// There should be 3 rows
					Assert.True(q.Step());
					Assert.True(q.Step());
					Assert.False(q.Step());
				}

				// Update stuff
				obj2.m_string = "I've been modified";
				Assert.AreEqual(1, table.Update(obj2));

				// Get the updated stuff and check it's been updated
				OBJ2 = table.Find(obj2.m_key);
				Assert.NotNull(OBJ2);
				Assert.True(obj2.Equals(OBJ2));

				// Delete something and check it's gone
				Assert.AreEqual(1, table.Delete(obj3));
				OBJ3 = table.Find(obj3.m_key);
				Assert.Null(OBJ3);

				// Update a single column and check it
				obj1.m_byte = 55;
				Assert.AreEqual(1, table.Update("m_byte", obj1.m_byte, 1));
				OBJ1 = table.Get(obj1.m_key);
				Assert.NotNull(OBJ1);
				Assert.True(obj1.Equals(OBJ1));

				// Read a single column
				var val = table.ColumnValue<ushort>("m_ushort", 2);
				Assert.AreEqual(obj2.m_ushort, val);

				// Add something back
				Assert.AreEqual(1, table.Insert(obj3));
				OBJ3 = table.Get(obj3.m_key);
				Assert.NotNull(OBJ3);
				Assert.True(obj3.Equals(OBJ3));

				// Update the column value for all rows
				obj1.m_byte = obj2.m_byte = obj3.m_byte = 0xAB;
				Assert.AreEqual(3, table.UpdateAll("m_byte", (byte)0xAB));

				// Enumerate objects
				var objs = table.Select(x => x).ToArray();
				Assert.AreEqual(3, objs.Length);
				Assert.True(obj1.Equals(objs[0]));
				Assert.True(obj2.Equals(objs[1]));
				Assert.True(obj3.Equals(objs[2]));

				// Linq expressions
				objs = (from a in table where a.m_string == "I've been modified" select a).ToArray();
				Assert.AreEqual(1, objs.Length);
				Assert.True(obj2.Equals(objs[0]));
			}
		}
		[Test] public void MultiplePks()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create|Sqlite.OpenFlags.ReadWrite|Sqlite.OpenFlags.NoMutex))
			{
				// Create a table
				db.DropTable<DomType3>();
				db.CreateTable<DomType3>();
				Assert.True(db.TableExists<DomType3>());

				// Check the table
				var table = db.Table<DomType3>();
				Assert.AreEqual(9, table.ColumnCount);
				using (var q = db.Query("select * from "+table.Name))
				{
					var cols = q.ColumnNames.ToList();
					Assert.AreEqual(9, q.ColumnCount);
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

				Assert.Throws<ArgumentException>(()=>table.Get(obj1.Index, obj1.Key2));

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
					Sqlite.Exception err = null;
					try { table.Insert(obj1); } catch (Sqlite.Exception ex) { err = ex; }
					Assert.True(err != null && err.Result == Sqlite.Result.Constraint);
					OBJ1 = table.Get(obj1.Index, obj1.Key2, obj1.Key3);
					Assert.NotNull(OBJ1);
					Assert.False(obj1.Equals(OBJ1));
				}
				{
					Sqlite.Exception err = null;
					try { table.Insert(obj1, Sqlite.OnInsertConstraint.Ignore); } catch (Sqlite.Exception ex) { err = ex; }
					Assert.Null(err);
					OBJ1 = table.Get(obj1.Index, obj1.Key2, obj1.Key3);
					Assert.NotNull(OBJ1);
					Assert.False(obj1.Equals(OBJ1));
				}
				{
					Sqlite.Exception err = null;
					try { table.Insert(obj1, Sqlite.OnInsertConstraint.Replace); } catch (Sqlite.Exception ex) { err = ex; }
					Assert.Null(err);
					OBJ1 = table.Get(obj1.Index, obj1.Key2, obj1.Key3);
					Assert.NotNull(OBJ1);
					Assert.True(obj1.Equals(OBJ1));
				}

				// Update in a multiple pk table
				obj2.PropA = "I've also been modified";
				Assert.AreEqual(1, table.Update(obj2));
				OBJ2 = table.Get(obj2.Index, obj2.Key2, obj2.Key3);
				Assert.NotNull(OBJ2);
				Assert.True(obj2.Equals(OBJ2));

				// Delete in a multiple pk table
				var keys = Sqlite.PrimaryKeys(obj3);
				Assert.AreEqual(1, table.DeleteByKey(keys));
				OBJ3 = table.Find(obj3.Index, obj3.Key2, obj3.Key3);
				Assert.Null(OBJ3);
			}
		}
		[Test] public void Unicode()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create|Sqlite.OpenFlags.ReadWrite|Sqlite.OpenFlags.NoMutex))
			{
				// Create a table
				db.DropTable<DomType2>();
				db.CreateTable<DomType2>();
				Assert.True(db.TableExists<DomType2>());

				// Check the table
				var table = db.Table<DomType2>();
				Assert.AreEqual(3, table.ColumnCount);
				var column_names = new[]{"PK", "UniStr", "Inc_Explicit"};
				using (var q = db.Query("select * from "+table.Name))
				{
					Assert.AreEqual(3        ,q.ColumnCount);
					Assert.AreEqual("PK"     ,q.ColumnName(0));
					Assert.True(column_names.Contains(q.ColumnName(0)));
					Assert.True(column_names.Contains(q.ColumnName(1)));
				}

				// Insert some stuff and check it stores/reads back ok
				var obj1 = new DomType2("123", "€€€€");
				var obj2 = new DomType2("abc", "⽄畂卧湥敳慈摮敬⡲㐲ㄴ⤷›慃獵摥戠㩹樠癡⹡慬杮吮牨睯扡敬›潣⹭湩牴湡汥洮扯汩扥汵敬⹴牃獡剨灥牯楴杮匫獥楳湯瑓牡䕴捸灥楴湯›敓獳潩⁮瑓牡整㩤眠楚塄婐桹㐰慳扲汬穷䌰㡶啩扁搶睄畎䐱䭡䝭牌夳䉡獁െ");
				Assert.AreEqual(1, table.Insert(obj1));
				Assert.AreEqual(1, table.Insert(obj2));
				Assert.AreEqual(2, table.RowCount);
				var OBJ1 = table.Get(obj1.PK);
				var OBJ2 = table.Get(obj2.PK);
				Assert.True(obj1.Equals(OBJ1));
				Assert.True(obj2.Equals(OBJ2));

				// Update Unicode stuff
				obj2.UniStr = "獁㩹獁";
				Assert.AreEqual(1, table.Update(obj2));
				OBJ2 = table.Get(obj2.PK);
				Assert.True(obj2.Equals(OBJ2));
			}
		}
		[Test] public void Transactions()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create|Sqlite.OpenFlags.ReadWrite|Sqlite.OpenFlags.NoMutex))
			{
				// Create a table
				db.DropTable<DomType0>();
				db.CreateTable<DomType0>();
				Assert.True(db.TableExists<DomType0>());

				// Create and insert some objects
				int key = 0;
				var objs = Enumerable.Range(0,10).Select(i => new DomType0(ref key, i)).ToList();
				foreach (var x in objs.Take(3))
					db.Insert(x, Sqlite.OnInsertConstraint.Replace);

				var rows = db.EnumRows<DomType0>("select * from DomType0").ToList();
				Assert.AreEqual(3, rows.Count);

				// Add objects
				try
				{
					using (var tranny = db.NewTransaction())
					{
						int i = 0;
						foreach (var x in objs)
						{
							if (++i == 5) throw new Exception("aborting insert");
							db.Insert(x, Sqlite.OnInsertConstraint.Replace);
						}
						tranny.Commit();
					}
				}
				catch {}
				Assert.AreEqual(3, db.Table<DomType0>().RowCount);

				using (db.NewTransaction())
				{
					foreach (var x in objs) db.Insert(x, Sqlite.OnInsertConstraint.Replace);
					// No commit
				}
				Assert.AreEqual(3, db.Table<DomType0>().RowCount);

				using (var tranny = db.NewTransaction())
				{
					foreach (var x in objs) db.Insert(x, Sqlite.OnInsertConstraint.Replace);
					tranny.Commit();
				}
				Assert.AreEqual(objs.Count, db.Table<DomType0>().RowCount);
			}
		}
		[Test] public void RuntimeTypes()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create|Sqlite.OpenFlags.ReadWrite|Sqlite.OpenFlags.NoMutex))
			{
				// Create a table
				db.DropTable<DomType1>();
				db.CreateTable<DomType1>();
				Assert.True(db.TableExists<DomType1>());
				var table = db.Table(typeof(DomType1));

				// Create objects
				var objs = Enumerable.Range(0,10).Select(i => new DomType1(i)).ToList();
				foreach (var x in objs)
					Assert.AreEqual(1, table.Insert(x)); // insert without compile-time type info

				objs[5].m_string = "I am number 5";
				Assert.AreEqual(1, table.Update(objs[5]));

				var OBJS = table.Cast<DomType1>().Select(x => x).ToList();
				for (int i = 0, iend = objs.Count; i != iend; ++i)
					Assert.True(objs[i].Equals(OBJS[i]));
			}
		}
		[Test] public void AlterTable()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create|Sqlite.OpenFlags.ReadWrite|Sqlite.OpenFlags.NoMutex))
			{
				// Create a table
				db.DropTable<DomType3>();
				db.CreateTable<DomType3>();
				Assert.True(db.TableExists<DomType3>());

				// Check the table
				var table3 = db.Table<DomType3>();
				Assert.AreEqual(9, table3.ColumnCount);
				using (var q = db.Query("select * from "+table3.Name))
				{
					var cols = q.ColumnNames.ToList();
					Assert.AreEqual(9, q.ColumnCount);
					Assert.AreEqual(9, cols.Count);
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
				Assert.True(db.TableExists<DomType4>());

				// Check the table
				var table4 = db.Table<DomType4>();
				Assert.AreEqual(6, table4.ColumnCount);
				using (var q = db.Query("select * from "+table4.Name))
				{
					var cols = q.ColumnNames.ToList();
					Assert.AreEqual(10, q.ColumnCount);
					Assert.AreEqual(10, cols.Count);
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
		[Test] public void ExprTree()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create|Sqlite.OpenFlags.ReadWrite|Sqlite.OpenFlags.NoMutex))
			{
				// Create a simple table
				db.DropTable<DomType0>();
				db.CreateTable<DomType0>();
				Assert.True(db.TableExists<DomType0>());
				var table = db.Table<DomType0>();

				// Insert stuff
				int key = 0;
				var values = new[]{4,1,0,5,7,9,6,3,8,2};
				foreach (var v in values)
					Assert.AreEqual(1, table.Insert(new DomType0(ref key, v)));
				Assert.AreEqual(10, table.RowCount);

				string sql_count = "select count(*) from "+table.Name;
				using (var q = db.Query(sql_count))
					Assert.AreEqual(sql_count, q.SqlString);

				// Do some expression tree queries
				{// Count clause
					var q = table.Count(x => (x.Inc_Key % 3) == 0);
					Assert.AreEqual(3, q);
				}
				{// Where clause
					var q = from x in table where x.Inc_Key % 2 == 1 select x;
					var list = q.ToList();
					Assert.AreEqual(5, list.Count);
				}
				{// Where clause
					// ReSharper disable RedundantCast
					var q = table.Where(x => ((IDomType0)x).Inc_Enum == SomeEnum.One || ((IDomType0)x).Inc_Enum == SomeEnum.Three); // Cast needed to test expressions
					var list = q.ToList();
					Assert.AreEqual(7, list.Count);
					Assert.AreEqual(3, list[0].Inc_Key);
					Assert.AreEqual(4, list[1].Inc_Key);
					Assert.AreEqual(6, list[2].Inc_Key);
					Assert.AreEqual(7, list[3].Inc_Key);
					Assert.AreEqual(8, list[4].Inc_Key);
					Assert.AreEqual(9, list[5].Inc_Key);
					Assert.AreEqual(10, list[6].Inc_Key);
					// ReSharper restore RedundantCast
				}
				{// Where clause with 'like' method calling 'RowCount'
					var q = (from x in table where SqlMethods.Like(x.Inc_Value, "5") select x).RowCount;
					Assert.AreEqual(1, q);
				}
				{// Where clause with x => true
					var q = table.Where(x => true);
					var list = q.ToList();
					Assert.AreEqual(10, list.Count);
					Assert.AreEqual(1, list[0].Inc_Key);
					Assert.AreEqual(2, list[1].Inc_Key);
					Assert.AreEqual(3, list[2].Inc_Key);
					Assert.AreEqual(4, list[3].Inc_Key);
					Assert.AreEqual(5, list[4].Inc_Key);
					Assert.AreEqual(6, list[5].Inc_Key);
					Assert.AreEqual(7, list[6].Inc_Key);
					Assert.AreEqual(8, list[7].Inc_Key);
					Assert.AreEqual(9, list[8].Inc_Key);
					Assert.AreEqual(10, list[9].Inc_Key);
				}
				{// Contains clause
					var set = new[]{"2","4","8"};
					var q = from x in table where set.Contains(x.Inc_Value) select x;
					var list = q.ToList();
					Assert.AreEqual(3, list.Count);
					Assert.AreEqual(1, list[0].Inc_Key);
					Assert.AreEqual(9, list[1].Inc_Key);
					Assert.AreEqual(10, list[2].Inc_Key);
				}
				{// NOT Contains clause
					var set = new List<string>{"2","4","8","5","9"};
					var q = from x in table where set.Contains(x.Inc_Value) == false select x;
					var list = q.ToList();
					Assert.AreEqual(5, list.Count);
					Assert.AreEqual(2, list[0].Inc_Key);
					Assert.AreEqual(3, list[1].Inc_Key);
					Assert.AreEqual(5, list[2].Inc_Key);
					Assert.AreEqual(7, list[3].Inc_Key);
					Assert.AreEqual(8, list[4].Inc_Key);
				}
				{// NOT Contains clause
					var set = new List<string>{"2","4","8","5","9"};
					var q = from x in table where !set.Contains(x.Inc_Value) select x;
					var list = q.ToList();
					Assert.AreEqual(5, list.Count);
					Assert.AreEqual(2, list[0].Inc_Key);
					Assert.AreEqual(3, list[1].Inc_Key);
					Assert.AreEqual(5, list[2].Inc_Key);
					Assert.AreEqual(7, list[3].Inc_Key);
					Assert.AreEqual(8, list[4].Inc_Key);
				}
				{// OrderBy clause
					var q = from x in table orderby x.Inc_Key descending select x;
					var list = q.ToList();
					Assert.AreEqual(10, list.Count);
					for (int i = 0; i != 10; ++i)
						Assert.AreEqual(10-i, list[i].Inc_Key);
				}
				{// Where and OrderBy clause
					var q = from x in table where x.Inc_Key >= 5 orderby x.Inc_Value select x;
					var list = q.ToList();
					Assert.AreEqual(6, list.Count);
					Assert.AreEqual(10, list[0].Inc_Key);
					Assert.AreEqual(8, list[1].Inc_Key);
					Assert.AreEqual(7, list[2].Inc_Key);
					Assert.AreEqual(5, list[3].Inc_Key);
					Assert.AreEqual(9, list[4].Inc_Key);
					Assert.AreEqual(6, list[5].Inc_Key);
				}
				{// Skip
					var q = table.Where(x => x.Inc_Key <= 5).Where(x => x.Inc_Value != "").Skip(2);
					var list = q.ToList();
					Assert.AreEqual(3, list.Count);
					Assert.AreEqual(3, list[0].Inc_Key);
					Assert.AreEqual(4, list[1].Inc_Key);
					Assert.AreEqual(5, list[2].Inc_Key);
				}
				{// Take
					var q = table.Where(x => x.Inc_Key >= 5).Take(2);
					var list = q.ToList();
					Assert.AreEqual(2, list.Count);
					Assert.AreEqual(5, list[0].Inc_Key);
					Assert.AreEqual(6, list[1].Inc_Key);
				}
				{// Skip and Take
					var q = table.Where(x => x.Inc_Key >= 5).Skip(2).Take(2);
					var list = q.ToList();
					Assert.AreEqual(2, list.Count);
					Assert.AreEqual(7, list[0].Inc_Key);
					Assert.AreEqual(8, list[1].Inc_Key);
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
					Assert.AreEqual(3, list[0].Inc_Key);
					Assert.AreEqual(4, list[1].Inc_Key);
					Assert.AreEqual(5, list[2].Inc_Key);
					Assert.AreEqual(6, list[3].Inc_Key);
					Assert.AreEqual(7, list[4].Inc_Key);
				}
				{// Delete
					var q = table.Delete(x => x.Inc_Key > 5);
					var list = table.ToList();
					Assert.AreEqual(5, q);
					Assert.AreEqual(5, list.Count);
					Assert.AreEqual(1, list[0].Inc_Key);
					Assert.AreEqual(2, list[1].Inc_Key);
					Assert.AreEqual(3, list[2].Inc_Key);
					Assert.AreEqual(4, list[3].Inc_Key);
					Assert.AreEqual(5, list[4].Inc_Key);
				}
				{// Select
					var q = table.Select(x => x.Inc_Key);
					var list = q.ToList();
					Assert.AreEqual(5, list.Count);
					Assert.AreEqual(typeof(List<int>), list.GetType());
					Assert.AreEqual(1, list[0]);
					Assert.AreEqual(2, list[1]);
					Assert.AreEqual(3, list[2]);
					Assert.AreEqual(4, list[3]);
					Assert.AreEqual(5, list[4]);
				}
				{// Select tuple
					var q = table.Select(x => new{x.Inc_Key, x.Inc_Enum});
					var list = q.ToList();
					Assert.AreEqual(5, list.Count);
					Assert.AreEqual(1, list[0].Inc_Key);
					Assert.AreEqual(2, list[1].Inc_Key);
					Assert.AreEqual(3, list[2].Inc_Key);
					Assert.AreEqual(4, list[3].Inc_Key);
					Assert.AreEqual(5, list[4].Inc_Key);
					Assert.AreEqual(SomeEnum.Two   ,list[0].Inc_Enum);
					Assert.AreEqual(SomeEnum.Two   ,list[1].Inc_Enum);
					Assert.AreEqual(SomeEnum.One   ,list[2].Inc_Enum);
					Assert.AreEqual(SomeEnum.Three ,list[3].Inc_Enum);
					Assert.AreEqual(SomeEnum.Two   ,list[4].Inc_Enum);
				}
				#pragma warning disable 168
				{// Check sql strings are correct
					string sql;

					var a = table.Where(x => x.Inc_Key == 3).Select(x => x.Inc_Enum).ToList();
					sql = table.SqlString;
					Assert.AreEqual("select Inc_Enum from DomType0 where (Inc_Key==?)", sql);

					var b = table.Where(x => x.Inc_Key == 3).Select(x => new{x.Inc_Value,x.Inc_Enum}).ToList();
					sql = table.SqlString;
					Assert.AreEqual("select Inc_Value,Inc_Enum from DomType0 where (Inc_Key==?)", sql);

					sql = table.Where(x => (x.Inc_Key & 0x3) == 0x1).GenerateExpression().ResetExpression().SqlString;
					Assert.AreEqual("select * from DomType0 where ((Inc_Key&?)==?)", sql);

					sql = table.Where(x => x.Inc_Key == 3).GenerateExpression().ResetExpression().SqlString;
					Assert.AreEqual("select * from DomType0 where (Inc_Key==?)", sql);

					sql = table.Where(x => x.Inc_Key == 3).Take(1).GenerateExpression().ResetExpression().SqlString;
					Assert.AreEqual("select * from DomType0 where (Inc_Key==?) limit 1", sql);

					var t = table.FirstOrDefault(x => x.Inc_Key == 4);
					sql = table.SqlString;
					Assert.AreEqual("select * from DomType0 where (Inc_Key==?) limit 1", sql);

					var l = table.Where(x => x.Inc_Key == 4).Take(4).Skip(2).ToList();
					sql = table.SqlString;
					Assert.AreEqual("select * from DomType0 where (Inc_Key==?) limit 4 offset 2", sql);

					var q = (from x in table where x.Inc_Key == 3 select new {x.Inc_Key, x.Inc_Value}).ToList();
					sql = table.SqlString;
					Assert.AreEqual("select Inc_Key,Inc_Value from DomType0 where (Inc_Key==?)", sql);

					var w = table.Delete(x => x.Inc_Key == 2);
					sql = table.SqlString;
					Assert.AreEqual("delete from DomType0 where (Inc_Key==?)", sql);
				}
				#pragma warning restore 168
			}
		}
		[Test] public void UntypedExprTree()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create|Sqlite.OpenFlags.ReadWrite|Sqlite.OpenFlags.NoMutex))
			{
				// Create a simple table
				db.DropTable<DomType0>();
				db.CreateTable<DomType0>();
				Assert.True(db.TableExists<DomType0>());
				var table = db.Table(typeof(DomType0));

				// Insert stuff
				int key = 0;
				var values = new[]{4,1,0,5,7,9,6,3,8,2};
				foreach (var v in values)
					Assert.AreEqual(1, table.Insert(new DomType0(ref key, v)));
				Assert.AreEqual(10, table.RowCount);

				string sql_count = "select count(*) from "+table.Name;
				using (var q = db.Query(sql_count))
					Assert.AreEqual(sql_count, q.SqlString);

				// Do some expression tree queries
				{// Count clause
					var q = table.Count<DomType0>(x => (x.Inc_Key % 3) == 0);
					Assert.AreEqual(3, q);
				}
				{// Where clause
					// ReSharper disable RedundantCast
					var q = table.Where<DomType0>(x => ((IDomType0)x).Inc_Enum == SomeEnum.One || ((IDomType0)x).Inc_Enum == SomeEnum.Three).Cast<IDomType0>(); // Cast needed to test expressions
					var list = q.ToList();
					Assert.AreEqual(7, list.Count);
					Assert.AreEqual(3, list[0].Inc_Key);
					Assert.AreEqual(4, list[1].Inc_Key);
					Assert.AreEqual(6, list[2].Inc_Key);
					Assert.AreEqual(7, list[3].Inc_Key);
					Assert.AreEqual(8, list[4].Inc_Key);
					Assert.AreEqual(9, list[5].Inc_Key);
					Assert.AreEqual(10, list[6].Inc_Key);
					// ReSharper restore RedundantCast
				}
				{// Where clause with 'like' method calling 'RowCount'
					var q = table.Where<DomType0>(x => SqlMethods.Like(x.Inc_Value, "5")).RowCount;
					Assert.AreEqual(1, q);
				}
				{// Where clause with x => true
					var q = table.Where<DomType0>(x => true).Cast<DomType0>();
					var list = q.ToList();
					Assert.AreEqual(10, list.Count);
					Assert.AreEqual(1, list[0].Inc_Key);
					Assert.AreEqual(2, list[1].Inc_Key);
					Assert.AreEqual(3, list[2].Inc_Key);
					Assert.AreEqual(4, list[3].Inc_Key);
					Assert.AreEqual(5, list[4].Inc_Key);
					Assert.AreEqual(6, list[5].Inc_Key);
					Assert.AreEqual(7, list[6].Inc_Key);
					Assert.AreEqual(8, list[7].Inc_Key);
					Assert.AreEqual(9, list[8].Inc_Key);
					Assert.AreEqual(10, list[9].Inc_Key);
				}
				{// Contains clause
					var set = new[]{"2","4","8"};
					var q = table.Where<DomType0>(x => set.Contains(x.Inc_Value)).Cast<DomType0>();
					var list = q.ToList();
					Assert.AreEqual(3, list.Count);
					Assert.AreEqual(1, list[0].Inc_Key);
					Assert.AreEqual(9, list[1].Inc_Key);
					Assert.AreEqual(10, list[2].Inc_Key);
				}
				{// NOT Contains clause
					var set = new List<string>{"2","4","8","5","9"};
					var q = table.Where<DomType0>(x => set.Contains(x.Inc_Value) == false).Cast<DomType0>();
					var list = q.ToList();
					Assert.AreEqual(5, list.Count);
					Assert.AreEqual(2, list[0].Inc_Key);
					Assert.AreEqual(3, list[1].Inc_Key);
					Assert.AreEqual(5, list[2].Inc_Key);
					Assert.AreEqual(7, list[3].Inc_Key);
					Assert.AreEqual(8, list[4].Inc_Key);
				}
				{// NOT Contains clause
					var set = new List<string>{"2","4","8","5","9"};
					var q = table.Where<DomType0>(x => !set.Contains(x.Inc_Value)).Cast<DomType0>();
					var list = q.ToList();
					Assert.AreEqual(5, list.Count);
					Assert.AreEqual(2, list[0].Inc_Key);
					Assert.AreEqual(3, list[1].Inc_Key);
					Assert.AreEqual(5, list[2].Inc_Key);
					Assert.AreEqual(7, list[3].Inc_Key);
					Assert.AreEqual(8, list[4].Inc_Key);
				}
				{// OrderBy clause
					var q = table.OrderByDescending<DomType0,int>(x => x.Inc_Key).Cast<DomType0>();
					var list = q.ToList();
					Assert.AreEqual(10, list.Count);
					for (int i = 0; i != 10; ++i)
						Assert.AreEqual(10-i, list[i].Inc_Key);
				}
				{// Where and OrderBy clause
					var q = table.Where<DomType0>(x => x.Inc_Key >= 5).OrderBy<DomType0,string>(x => x.Inc_Value).Cast<DomType0>();
					var list = q.ToList();
					Assert.AreEqual(6, list.Count);
					Assert.AreEqual(10, list[0].Inc_Key);
					Assert.AreEqual(8, list[1].Inc_Key);
					Assert.AreEqual(7, list[2].Inc_Key);
					Assert.AreEqual(5, list[3].Inc_Key);
					Assert.AreEqual(9, list[4].Inc_Key);
					Assert.AreEqual(6, list[5].Inc_Key);
				}
				{// Skip
					var q = table.Where<DomType0>(x => x.Inc_Key <= 5).Skip(2).Cast<DomType0>();
					var list = q.ToList();
					Assert.AreEqual(3, list.Count);
					Assert.AreEqual(3, list[0].Inc_Key);
					Assert.AreEqual(4, list[1].Inc_Key);
					Assert.AreEqual(5, list[2].Inc_Key);
				}
				{// Take
					var q = table.Where<DomType0>(x => x.Inc_Key >= 5).Take(2).Cast<DomType0>();
					var list = q.ToList();
					Assert.AreEqual(2, list.Count);
					Assert.AreEqual(5, list[0].Inc_Key);
					Assert.AreEqual(6, list[1].Inc_Key);
				}
				{// Skip and Take
					var q = table.Where<DomType0>(x => x.Inc_Key >= 5).Skip(2).Take(2).Cast<DomType0>();
					var list = q.ToList();
					Assert.AreEqual(2, list.Count);
					Assert.AreEqual(7, list[0].Inc_Key);
					Assert.AreEqual(8, list[1].Inc_Key);
				}
				{// Null test
					var q = table.Where<DomType0>(x => x.Inc_Value != null).Cast<DomType0>();
					var list = q.ToList();
					Assert.AreEqual(10, list.Count);
				}
				{// Type conversions
					var q = table.Where<DomType0>(x => (float) x.Inc_Key > 2.5f && (float) x.Inc_Key < 7.5f).Cast<DomType0>();
					var list = q.ToList();
					Assert.AreEqual(5, list.Count);
					Assert.AreEqual(3, list[0].Inc_Key);
					Assert.AreEqual(4, list[1].Inc_Key);
					Assert.AreEqual(5, list[2].Inc_Key);
					Assert.AreEqual(6, list[3].Inc_Key);
					Assert.AreEqual(7, list[4].Inc_Key);
				}
				{// Delete
					var q = table.Delete<DomType0>(x => x.Inc_Key > 5);
					var list = table.Cast<DomType0>().ToList();
					Assert.AreEqual(5, q);
					Assert.AreEqual(5, list.Count);
					Assert.AreEqual(1, list[0].Inc_Key);
					Assert.AreEqual(2, list[1].Inc_Key);
					Assert.AreEqual(3, list[2].Inc_Key);
					Assert.AreEqual(4, list[3].Inc_Key);
					Assert.AreEqual(5, list[4].Inc_Key);
				}
				#pragma warning disable 168
				{// Check sql strings are correct
					string sql;

					sql = table.Where<DomType0>(x => x.Inc_Key == 3).GenerateExpression().ResetExpression().SqlString;
					Assert.AreEqual("select * from DomType0 where (Inc_Key==?)", sql);

					sql = table.Where<DomType0>(x => x.Inc_Key == 3).Take(1).GenerateExpression().ResetExpression().SqlString;
					Assert.AreEqual("select * from DomType0 where (Inc_Key==?) limit 1", sql);

					var t = table.FirstOrDefault<DomType0>(x => x.Inc_Key == 4);
					sql = table.SqlString;
					Assert.AreEqual("select * from DomType0 where (Inc_Key==?) limit 1", sql);

					var l = table.Where<DomType0>(x => x.Inc_Key == 4).Take(4).Skip(2).Cast<DomType0>().ToList();
					sql = table.SqlString;
					Assert.AreEqual("select * from DomType0 where (Inc_Key==?) limit 4 offset 2", sql);

					var w = table.Delete<DomType0>(x => x.Inc_Key == 2);
					sql = table.SqlString;
					Assert.AreEqual("delete from DomType0 where (Inc_Key==?)", sql);
				}
				#pragma warning restore 168
			}
		}
		[Test] public void Nullables()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create|Sqlite.OpenFlags.ReadWrite|Sqlite.OpenFlags.NoMutex))
			{
				// Create a simple table
				db.DropTable<DomType1>();
				db.CreateTable<DomType1>();
				Assert.True(db.TableExists<DomType1>());
				var table = db.Table<DomType1>();

				// Create some objects to stick in the table
				var obj1 = new DomType1(1){m_nullint = 1, m_int = 4};
				var obj2 = new DomType1(2){m_nulllong = null};
				var obj3 = new DomType1(3){m_nullint = null};
				var obj4 = new DomType1(4){m_nulllong = 2};

				// Insert stuff
				Assert.AreEqual(1, table.Insert(obj1));
				Assert.AreEqual(1, table.Insert(obj2));
				Assert.AreEqual(1, table.Insert(obj3));
				Assert.AreEqual(1, table.Insert(obj4));
				Assert.AreEqual(4, table.RowCount);

				{// non-null nullable
					int? nullable = 1;
					var q = table.Where(x => x.m_nullint == nullable);
					var list = q.ToList();
					Assert.AreEqual(1, list.Count);
					Assert.AreEqual(1, list[0].m_nullint);
				}
				{// non-null nullable
					long? nullable = 2;
					var q = table.Where(x => x.m_nulllong == nullable.Value);
					var list = q.ToList();
					Assert.AreEqual(1, list.Count);
					Assert.AreEqual((long?)2, list[0].m_nulllong);
				}
				{// null nullable
					int? nullable = null;
					var q = table.Where(x => x.m_nullint == nullable);
					var list = q.ToList();
					Assert.AreEqual(1, list.Count);
					Assert.AreEqual(null, list[0].m_nullint);
				}
				{// null nullable
					long? nullable = null;
					var q = table.Where(x => x.m_nullint == nullable);
					var list = q.ToList();
					Assert.AreEqual(1, list.Count);
					Assert.AreEqual(null, list[0].m_nulllong);
				}
				{// expression nullable(not null) == non-nullable
					const int target = 1;
					var q = table.Where(x => x.m_nullint == target);
					var list = q.ToList();
					Assert.AreEqual(1, list.Count);
					Assert.AreEqual(1, list[0].m_nullint);
				}
				{// expression non-nullable == nullable(not null)
					int? target = 4;
					var q = table.Where(x => x.m_int == target);
					var list = q.ToList();
					Assert.AreEqual(1, list.Count);
					Assert.AreEqual(4, list[0].m_int);
				}
				{// expression nullable(null) == non-nullable
					const long target = 2;
					var q = table.Where(x => x.m_nulllong == target);
					var list = q.ToList();
					Assert.AreEqual(1, list.Count);
					Assert.AreEqual((long?)2, list[0].m_nulllong);
				}
				{// expression non-nullable == nullable(null)
					int? target = null;
					var q = table.Where(x => x.m_int == target);
					var list = q.ToList();
					Assert.AreEqual(0, list.Count);
				}
				{// Testing members on nullable types
					var q = table.Where(x => x.m_nullint.HasValue == false);
					var list = q.ToList();
					Assert.AreEqual(1, list.Count);
					Assert.AreEqual(null, list[0].m_nullint);
				}
				{// Testing members on nullable types
					var q = table.Where(x => x.m_nullint.HasValue && x.m_nullint.Value == 23);
					var list = q.ToList();
					Assert.AreEqual(2, list.Count);
					Assert.AreEqual(23, list[0].m_nullint);
					Assert.AreEqual(23, list[1].m_nullint);
				}
			}
		}
		[Test] public void AttributeInheritance()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create|Sqlite.OpenFlags.ReadWrite|Sqlite.OpenFlags.NoMutex))
			{
				// Create a simple table
				db.DropTable<DomType5>();
				db.CreateTable<DomType5>();
				Assert.True(db.TableExists<DomType5>());
				var table = db.Table<DomType5>();

				var meta = db.TableMetaData<DomType5>();
				Assert.AreEqual(2, meta.ColumnCount);
				Assert.AreEqual(1, meta.Pks.Length);
				Assert.AreEqual("PK", meta.Pks[0].Name);
				Assert.AreEqual(1, meta.NonPks.Length);
				Assert.AreEqual("Data", meta.NonPks[0].Name);

				// Create some objects to stick in the table
				var obj1 = new DomType5{Data = "1"};
				var obj2 = new DomType5{Data = "2"};
				var obj3 = new DomType5{Data = "3"};
				var obj4 = new DomType5{Data = "4"};

				// Insert stuff
				Assert.AreEqual(1, table.Insert(obj1));
				Assert.AreEqual(1, table.Insert(obj2));
				Assert.AreEqual(1, table.Insert(obj3));
				Assert.AreEqual(1, table.Insert(obj4));
				Assert.AreEqual(4, table.RowCount);
			}
		}
		[Test] public void RowChangedEvents()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create|Sqlite.OpenFlags.ReadWrite|Sqlite.OpenFlags.NoMutex))
			{
				// Sign up a handler for row changed
				Sqlite.DataChangedArgs args = null;
				db.DataChanged += (s,a) => args = a;

				// Create a simple table
				db.DropTable<DomType5>();
				db.CreateTable<DomType5>();
				Assert.True(db.TableExists<DomType5>());
				var table = db.Table<DomType5>();

				// Create some objects to stick in the table
				var obj1 = new DomType5("One");
				var obj2 = new DomType5("Two");

				// Insert stuff and check the event fires
				table.Insert(obj1);
				Assert.AreEqual(Sqlite.ChangeType.Insert ,args.ChangeType);
				Assert.AreEqual("DomType5"               ,args.TableName);
				Assert.AreEqual(1L                       ,args.RowId);

				db.Insert(obj2);
				Assert.AreEqual(Sqlite.ChangeType.Insert ,args.ChangeType);
				Assert.AreEqual("DomType5"               ,args.TableName);
				Assert.AreEqual(2L                       ,args.RowId);

				obj1.Data = "Updated";
				table.Update(obj1);
				Assert.AreEqual(Sqlite.ChangeType.Update ,args.ChangeType);
				Assert.AreEqual("DomType5"               ,args.TableName);
				Assert.AreEqual(1L                       ,args.RowId);

				db.Delete(obj2);
				Assert.AreEqual(Sqlite.ChangeType.Delete ,args.ChangeType);
				Assert.AreEqual("DomType5"               ,args.TableName);
				Assert.AreEqual(2L                       ,args.RowId);
			}
		}
		[Test] public void ObjectCache()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create|Sqlite.OpenFlags.ReadWrite|Sqlite.OpenFlags.NoMutex))
			{
				// Create a simple table
				db.DropTable<DomType5>();
				db.CreateTable<DomType5>();
				Assert.True(db.TableExists<DomType5>());
				var table = db.Table<DomType5>();

				var obj1 = new DomType5("One");
				var obj2 = new DomType5("Two");
				var obj3 = new DomType5("Fre");

				// Insert some stuff
				table.Insert(obj1);
				table.Insert(obj2);
				table.Insert(obj3);

				// Check the cache
				Assert.False(table.Cache.IsCached(obj1.PK));
				Assert.False(table.Cache.IsCached(obj2.PK));
				Assert.False(table.Cache.IsCached(obj3.PK));

				table.Get<DomType5>(1);
				table.Get<DomType5>(2);
				table.Get<DomType5>(3);
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

				var o2_a = table.Get<DomType5>(2);
				Assert.False(table.Cache.IsCached(obj1.PK));
				Assert.True(table.Cache.IsCached(obj2.PK));
				Assert.False(table.Cache.IsCached(obj3.PK));

				var o2_b = table.Get<DomType5>(2);
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

				var o2_d = table.Get<DomType5>(2);
				Assert.True(table.Cache.IsCached(obj2.PK));
				Assert.True(table.MetaData.Equal(obj2, o2_d));

				// Check that changes via individual column updates also invalidate the cache
				obj2.Data = "ChangedAgain";
				table.Update(Reflect<DomType5>.MemberName(x => x.Data), obj2.Data, obj2.PK);
				Assert.False(table.Cache.IsCached(obj2.PK));
				var o2_e = table.Get<DomType5>(2);
				Assert.True(table.Cache.IsCached(obj2.PK));
				Assert.True(table.MetaData.Equal(obj2, o2_e));

				// Check deleting an object also removes it from the cache
				table.Delete(obj2);
				Assert.False(table.Cache.IsCached(obj2.PK));
			}
		}
		[Test] public void QueryCache()
		{
			// Create/Open the database connection
			using (var db = new Sqlite.Database(FilePath, Sqlite.OpenFlags.Create|Sqlite.OpenFlags.ReadWrite|Sqlite.OpenFlags.NoMutex))
			{
				// Create a simple table
				db.DropTable<DomType5>();
				db.CreateTable<DomType5>();
				Assert.True(db.TableExists<DomType5>());
				var table = db.Table<DomType5>();

				var obj1 = new DomType5("One");
				var obj2 = new DomType5("Two");
				var obj3 = new DomType5("Fre");

				// Insert some stuff
				table.Insert(obj1);
				table.Insert(obj2);
				table.Insert(obj3);

				var cache_count = db.QueryCache.Count;

				var sql = "select * from DomType5 where Data = ?";
				using (var q = db.Query(sql, 1, new object[]{"One"}))
				{
					var r = q.Rows<DomType5>();
					var r0 = r.FirstOrDefault();
					Assert.True(r0 != null);
					Assert.True(r0.Data == "One");
				}

				Assert.True(db.QueryCache.IsCached(sql)); // in the cache while not in use

				using (var q = db.Query(sql, 1, new object[]{"Two"}))
				{
					Assert.True(!db.QueryCache.IsCached(sql)); // not in the cache while in use

					var r = q.Rows<DomType5>();
					var r0 = r.FirstOrDefault();
					Assert.True(r0 != null);
					Assert.True(r0.Data == "Two");
				}

				Assert.True(db.QueryCache.Count == cache_count + 1); // back in the cache while not in use
			}
		}
	}
}

#endif