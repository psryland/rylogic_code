//#define SQLITE_HANDLES
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Rylogic.Common;
using Rylogic.Interop.Win32;

namespace Rylogic.Db
{
	public static partial class Sqlite
	{
		private const string Dll = "sqlite3";

		/// <summary>Helper method for loading the dll from a platform specific path</summary>
		public static void LoadDll(string dir = @".\lib\$(platform)\$(config)", bool throw_if_missing = true, EConfigOption threading = EConfigOption.MultiThread)
		{
			if (ModuleLoaded) return;
			m_module = Win32.LoadDll($"{Dll}.dll", out LoadError, dir, throw_if_missing);
			Configure(threading);
		}

		/// <summary>True if the sqlite3.dll has been loaded into memory</summary>
		public static bool ModuleLoaded => m_module != IntPtr.Zero;
		private static IntPtr m_module;

		/// <summary>The exception created if the module fails to load</summary>
		public static Exception? LoadError;

		/// <summary>Return the threading mode (config option) that the dll has been set to</summary>
		public static EConfigOption ThreadingMode { get; set; } = EConfigOption.SingleThread;

		/// <summary>Base class for wrappers of native sqlite handles</summary>
		[DebuggerDisplay("{Label} {handle} (ThreadId={ThreadId})")]
		private abstract class SqliteHandle : SafeHandle
		{
			// Notes:
			//  - Although SafeHandles can be created by the interop layer, it doesn't work as you'd expect.
			//    See: https://stackoverflow.com/questions/47934248/cannot-pass-safehandle-instance-in-releasehandle-to-native-method
			//  - P/Invoke code should use 'IntPtr' with the SafeHandles constructed by user code.
			protected SqliteHandle()
				: this(IntPtr.Zero, true)
			{ }
			protected SqliteHandle(IntPtr initial_ptr, bool owns_handle)
				: base(IntPtr.Zero, owns_handle)
			{
				ThreadId = Thread.CurrentThread.ManagedThreadId;
				Label = string.Empty;

				// Initialise to 'empty' so we know when its been set
				CloseResult = EResult.Empty;
				SetHandle(initial_ptr);

				#if SQLITE_HANDLES
				#warning SQLITE_HANDLES is enabled
				if (this is NativeSqlite3Handle db)
					ExistingDB.Add(db);
				if (this is NativeSqlite3StmtHandle stmt)
					ExistingStmt.Add(stmt);
				StackAtCreation = new StackTrace(true).ToString();
				CreationId = ++g_sqlite_handle_id;
				#endif
			}
			protected override void Dispose(bool disposing)
			{
				#if SQLITE_HANDLES
				if (this is NativeSqlite3Handle db)
					ExistingDB.Remove(db);
				if (this is NativeSqlite3StmtHandle stmt)
					ExistingStmt.Remove(stmt);
				#endif

				base.Dispose(disposing);
			}

			/// <inheritdoc/>
			protected sealed override bool ReleaseHandle()
			{
				// Check this isn't called from the GC thread.
				if (Thread.CurrentThread.ManagedThreadId != ThreadId)
				{
					// This typically happens when the garbage collector cleans up the handles.
					// The garbage collector should never see these, so the bug is you've created
					// a query somewhere that hasn't been disposed.
					// Note: the garbage collector calls dispose in no particular order, so it's
					// possible you have a Database instance still live and this exception is being
					// thrown for an instance in the cache of that instance.

					#if SQLITE_HANDLES

					// Output the stack dump of where this handle was allocated
					var msg =
						$"Sqlite handle ({CreationId}) released from a different thread to the one it was created on\r\n" +
						$"Handle was created here:\r\n{StackAtCreation}";

					// Add the stack dump of where the owning DB handle was created
					// If this is the garbage collector thread, it's possible the owner has been disposed already
					if (this is NativeSqlite3StmtHandle stmt)
					{
						using var db = (NativeSqlite3Handle)stmt.OwnerDB();
						msg += $"\r\nThe DB that the handle is associated with was created:\r\n{db.StackAtCreation}";
					}

					throw new Exception(msg);

					#else
					throw new SqliteException(EResult.Misuse, "Sqlite handle released from a different thread to the one it was created on", string.Empty);
					#endif
				}

				// This is called when "Close" or "Dispose" is called on the handle.
				// At this point, the handle is already marked as 'IsClosed' and any use of the
				// handle results in an 'ObjectDisposedException'.
				CloseResult = Release();
				handle = IntPtr.Zero;
				return true;
			}

			/// <summary>Release the handle</summary>
			protected abstract EResult Release();

			/// <summary>The result from the 'sqlite3_close' call</summary>
			public EResult CloseResult { get; internal set; }

			/// <summary>Ensure sqlite handles are created and released from the same thread</summary>
			private int ThreadId { get; }

			/// <summary>Debugging helper for labelling handles</summary>
			public string Label { get; set; }

			#if SQLITE_HANDLES
			public static List<SqliteHandle> ExistingDB = new List<SqliteHandle>();
			public static List<SqliteHandle> ExistingStmt = new List<SqliteHandle>();
			private static int g_sqlite_handle_id;
			private string StackAtCreation { get; }
			private int CreationId { get; }
			#endif

			/// <summary>True if the contained handle is invalid</summary>
			public override bool IsInvalid => handle == IntPtr.Zero;
			public bool IsValid => !IsInvalid;
		}

		/// <summary>A wrapper for unmanaged sqlite database connection handles</summary>
		private class NativeSqlite3Handle : SqliteHandle, sqlite3
		{
			public NativeSqlite3Handle()
				: this(IntPtr.Zero, true)
			{ }
			public NativeSqlite3Handle(IntPtr handle, bool owns_handle)
				: base(handle, owns_handle)
			{}

			/// <inheritdoc/>
			public IntPtr Handle => handle;

			/// <inheritdoc/>
			protected override EResult Release() => NativeAPI.Close(this);
		}

		/// <summary>A wrapper for unmanaged sqlite prepared statement handles</summary>
		[DebuggerDisplay("{Sql,nq}")]
		private class NativeSqlite3StmtHandle : SqliteHandle, sqlite3_stmt
		{
			public NativeSqlite3StmtHandle()
				: this(IntPtr.Zero, true)
			{ }
			public NativeSqlite3StmtHandle(IntPtr handle, bool owns_handle)
				: base(handle, owns_handle)
			{}

			/// <inheritdoc/>
			public IntPtr Handle => handle;

			/// <inheritdoc/>
			protected override EResult Release() => NativeAPI.Finalise(this);

			/// <summary>The SQL string of this statement </summary>
			public string Sql => IsValid ? NativeAPI.SqlString(this) ?? string.Empty : "Invalid Handle";

			#if SQLITE_HANDLES
			public sqlite3 OwnerDB() => NativeAPI.Connection(this); // The owner DB handle
			#endif
		}

		/// <summary>Sqlite3 C API</summary>
		public static class NativeAPI
		{
			#region Callback functions

			/// <summary>Callback function called for each log event</summary>
			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			public delegate void LogCB(IntPtr ctx, int lvl, [MarshalAs(UnmanagedType.LPStr)] string msg);

			/// <summary>Callback type for sqlite3_update_hook</summary>
			/// <param name="ctx">A copy of the third argument to sqlite3_update_hook()</param>
			/// <param name="change_type">One of SQLITE_INSERT, SQLITE_DELETE, or SQLITE_UPDATE</param>
			/// <param name="db_name">The name of the affected database</param>
			/// <param name="table_name">The name of the table containing the changed item</param>
			/// <param name="row_id">The row id of the changed row</param>
			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			public delegate void UpdateHookCB(IntPtr ctx, int change_type, string db_name, string table_name, long row_id);

			/// <summary>Callback type for sqlite3_update_hook</summary>
			/// <param name="argv">A pointer to the address of 'ctx'</param>
			/// <param name="argc">The number of pointers</param>
			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			public delegate void UnlockCB(IntPtr argv, int argc);

			#endregion
			#region Configuration

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

			/// <summary>Returns a string constant whose value is the same as the SQLITE_SOURCE_ID C preprocessor macro</summary>
			public static string SourceId()
			{
				return Marshal.PtrToStringAnsi(sqlite3_sourceid()) ?? throw new NullReferenceException("SourceId returned null");
			}
			[DllImport(Dll, EntryPoint = "sqlite3_sourceid", CallingConvention = CallingConvention.Cdecl)]
			private static extern IntPtr sqlite3_sourceid();

			/// <summary>Set a configuration setting for the database</summary>
			public static EResult Config(EConfigOption option, object[] args)
			{
				switch (option)
				{
					case EConfigOption.SingleThread:
					case EConfigOption.MultiThread:
					case EConfigOption.Serialized:
					{
						if (sqlite3_threadsafe() == 0 && option != EConfigOption.SingleThread)
							throw new SqliteException(EResult.Misuse, "sqlite3 dll compiled with SQLITE_THREADSAFE=0, multi threading cannot be used", string.Empty);

						ThreadingMode = option;
						return sqlite3_config(ThreadingMode, __arglist());
					}
					case EConfigOption.Log:
					{
						var func = args.Length > 0 ? (LogCB)args[0] : throw new ArgumentException("Configuring the log requires a callback function");
						var ctx = args.Length > 1 ? (IntPtr)args[1] : IntPtr.Zero;
						return sqlite3_config(ThreadingMode, __arglist(func, ctx));
					}
					default:
					{
						throw new NotSupportedException();
					}
				}
			}
			[DllImport(Dll, EntryPoint = "sqlite3_config", CallingConvention = CallingConvention.Cdecl)]
			private static extern EResult sqlite3_config(EConfigOption option, __arglist);

			/// <summary>Returns the threading mode that the library was built with</summary>
			public static EConfigOption CompiledThreadingMode
			{
				get
				{
					return sqlite3_threadsafe() switch
					{
						0 => EConfigOption.SingleThread,
						1 => EConfigOption.MultiThread,
						2 => EConfigOption.Serialized,
						_ => throw new Exception("Unexpected result from 'sqlite3_threadsafe()'"),
					};
				}
			}
			[DllImport(Dll, EntryPoint = "sqlite3_threadsafe", CallingConvention = CallingConvention.Cdecl)]
			private static extern int sqlite3_threadsafe();

			/// <summary>Set a DB limit</summary>
			[DllImport(Dll, EntryPoint = "sqlite3_limit", CallingConvention = CallingConvention.Cdecl)]
			private static extern int sqlite3_limit(IntPtr db, ELimit limit_category, int new_value);

			/// <summary>Set the busy wait timeout on the DB</summary>
			public static void BusyTimeout(sqlite3 db, int milliseconds)
			{
				var r = sqlite3_busy_timeout(db.Handle, milliseconds);
				if (r == EResult.OK) return;
				throw new SqliteException(r, $"Failed to set the busy timeout to {milliseconds}ms", ErrorMsg(db));
			}
			[DllImport(Dll, EntryPoint = "sqlite3_busy_timeout", CallingConvention = CallingConvention.Cdecl)]
			private static extern EResult sqlite3_busy_timeout(IntPtr db, int milliseconds);

			/// <summary>Free memory previously obtained from sqlite3Malloc()</summary>
			[DllImport(Dll, EntryPoint = "sqlite3_free", CallingConvention = CallingConvention.Cdecl)]
			private static extern void sqlite3_free(IntPtr ptr);

			#endregion

			/// <summary>Open a database file</summary>
			public static sqlite3 Open(string filepath, EOpenFlags flags)
			{
				// Because the library has no mutexes
				if (CompiledThreadingMode == EConfigOption.SingleThread && (flags.HasFlag(EOpenFlags.FullMutex) || flags.HasFlag(EOpenFlags.NoMutex)))
					throw new SqliteException(EResult.Misuse, "sqlite3 dll compiled with SQLITE_THREADSAFE=0, multi threading cannot be used", string.Empty);

				// Because the cache is shared between connections and not protected
				if (flags.HasFlag(EOpenFlags.SharedCache) && ThreadingMode == EConfigOption.MultiThread)
					throw new SqliteException(EResult.Misuse, "Shared cache can't be used threading mode 'MultiThreaded'", string.Empty);

				// Convert the filepath to UTF8
				var filepath_utf8 = StrToUTF8(filepath);

				// Open the connection
				var res = sqlite3_open_v2(filepath_utf8, out var db, (int)flags, IntPtr.Zero);
				if (res != EResult.OK)
					throw new SqliteException(res, $"Failed to open database connection to file {filepath}", string.Empty);

				return new NativeSqlite3Handle(db, true);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_open_v2", CallingConvention = CallingConvention.Cdecl)]
			private static extern EResult sqlite3_open_v2(byte[] filepath_utf8, out IntPtr db, int flags, IntPtr zvfs);

			/// <summary>Close a database connection</summary>
			public static EResult Close(sqlite3 db)
			{
				return sqlite3_close(db.Handle);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_close", CallingConvention = CallingConvention.Cdecl)]
			private static extern EResult sqlite3_close(IntPtr db);

			/// <summary>Creates prepared statements from an sql string (possible multi-expressions)</summary>
			public static sqlite3_stmt Prepare(sqlite3 db, string sql)
			{
				int offset = 0;
				var sql_utf8 = Encoding.Convert(Encoding.Unicode, Encoding.UTF8, Encoding.Unicode.GetBytes(sql));
				var stmt = Prepare(db, sql_utf8, ref offset);
				if (offset != sql_utf8.Length)
					throw new SqliteException(EResult.Misuse, $"Prepared multi-statement command when only a single state was expected.", string.Empty);

				return stmt;
			}
			public static sqlite3_stmt Prepare(sqlite3 db, byte[] sql_utf8, ref int offset)
			{
				// Pin the sql text before passing a pointer into it to the native library
				using var ptr = Marshal_.Pin(sql_utf8, GCHandleType.Pinned);

				// Prepare the next statement
				for (; ; )
				{
					var res = sqlite3_prepare_v2(db.Handle, ptr.Pointer + offset, sql_utf8.Length - offset, out var stmt, out var tail);
					switch (res.BasicCode())
					{
						case EResult.OK:
						{
							offset = (int)(tail.ToInt64() - ptr.Pointer.ToInt64());
							return new NativeSqlite3StmtHandle(stmt, true);
						}
						case EResult.Busy:
						{
							Thread.Yield();
							break;
						}
						case EResult.Locked:
						{
							if (res != EResult.Locked_SharedCache)
								throw new SqliteException(res, "Database locked", ErrorMsg(db));

							// Wait for the DB to unlock
							using var mre = new ManualResetEvent(false);
							void WaitForUnlock(IntPtr argv, int argc) => mre.Set();
							var rc = UnlockNotify(db, WaitForUnlock, IntPtr.Zero);
							if (rc != EResult.OK && rc.BasicCode() != EResult.Locked)
								throw new SqliteException(rc, $"Failed to register callback in UnlockNotify", ErrorMsg(db));

							mre.WaitOne();
							break;
						}
						default:
						{
							var sql = Encoding.UTF8.GetString(sql_utf8, offset, sql_utf8.Length - offset);
							throw new SqliteException(res, $"Error preparing statement '{sql}'", ErrorMsg(db));
						}
					}
				}
			}
			[DllImport(Dll, EntryPoint = "sqlite3_prepare_v2", CallingConvention = CallingConvention.Cdecl)]
			private static extern EResult sqlite3_prepare_v2(IntPtr db, IntPtr sql_utf8, int num_bytes, out IntPtr stmt, out IntPtr pzTail);

			/// <summary>Release a prepared sql statement</summary>
			public static EResult Finalise(sqlite3_stmt stmt)
			{
				return sqlite3_finalize(stmt.Handle);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_finalize", CallingConvention = CallingConvention.Cdecl)]
			private static extern EResult sqlite3_finalize(IntPtr stmt);

			/// <summary>Find The Database Handle Of A Prepared Statement</summary>
			public static sqlite3 Connection(sqlite3_stmt stmt)
			{
				var db = sqlite3_db_handle(stmt.Handle);
				return new NativeSqlite3Handle(db, false);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_db_handle", CallingConvention = CallingConvention.Cdecl)]
			private static extern IntPtr sqlite3_db_handle(IntPtr stmt);

			/// <summary>Returns the number of rows changed by the last operation</summary>
			public static int Changes(sqlite3 db)
			{
				return sqlite3_changes(db.Handle);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_changes", CallingConvention = CallingConvention.Cdecl)]
			private static extern int sqlite3_changes(IntPtr db);

			/// <summary>Returns the RowId for the last inserted row</summary>
			public static long LastInsertRowId(sqlite3 db)
			{
				return sqlite3_last_insert_rowid(db.Handle);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_last_insert_rowid", CallingConvention = CallingConvention.Cdecl)]
			private static extern long sqlite3_last_insert_rowid(IntPtr db);

			/// <summary>Last error code</summary>
			public static EResult ErrorCode(sqlite3 db)
			{
				// The values returned by sqlite3_errcode() and/or sqlite3_extended_errcode() might change with each
				// API call. Except, there are some interfaces that are guaranteed to never change the value of the
				// error code. The error-code preserving interfaces include the following:
				//   The sqlite3_errstr() interface returns the English-language text that describes the result code,
				//   as UTF-8. Memory to hold the error message string is managed internally and must not be freed by the application.
				//
				// If the most recent error references a specific token in the input SQL, the sqlite3_error_offset() interface returns
				// the byte offset of the start of that token.The byte offset returned by sqlite3_error_offset() assumes that the input
				// SQL is UTF8. If the most recent error does not reference a specific token in the input SQL, then the sqlite3_error_offset() function returns -1.
				//
				// When the serialized threading mode is in use, it might be the case that a second error occurs on a separate thread
				// in between the time of the first error and the call to these interfaces.When that happens, the second error will be
				// reported since these interfaces always report the most recent result. To avoid this, each thread can obtain exclusive
				// use of the database connection D by invoking sqlite3_mutex_enter(sqlite3_db_mutex(D)) before beginning to use D and
				// invoking sqlite3_mutex_leave(sqlite3_db_mutex(D)) after all calls to the interfaces listed here are completed.
				//
				// If an interface fails with SQLITE_MISUSE, that means the interface was invoked incorrectly by the application.
				// In that case, the error code and message may or may not be set.
				//
				// If the most recent sqlite3_* API call associated with the database connection failed, then the
				// sqlite3_errcode(db) interface returns the numeric result code or extended result code for that API call. 
				// The sqlite3_extended_errcode() interface is the same except that it always returns the extended
				// result code even when extended result codes are disabled.
				return (EResult)sqlite3_errcode(db);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_errcode", CallingConvention = CallingConvention.Cdecl)]
			private static extern int sqlite3_errcode(sqlite3 db);

			/// <summary>Extended last error code</summary>
			public static EResult ErrorCodeExtended(sqlite3 db)
			{
				return (EResult)sqlite3_extended_errcode(db);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_extended_errcode", CallingConvention = CallingConvention.Cdecl)]
			private static extern int sqlite3_extended_errcode(sqlite3 db);

			/// <summary>Returns the error message for the last error returned from sqlite</summary>
			public static string ErrorMsg(sqlite3 db)
			{
				// sqlite3 manages the memory allocated for the returned error message
				// so we don't need to call sqlite_free on the returned pointer
				return Marshal.PtrToStringUni(sqlite3_errmsg16(db.Handle)) ?? string.Empty;
			}
			public static string ErrorMsg(sqlite3_stmt stmt)
			{
				// The sqlite3_errmsg() and sqlite3_errmsg16() return English - language text that describes the error,
				// as either UTF-8 or UTF-16 respectively. Memory to hold the error message string is managed internally.
				// The application does not need to worry about freeing the result.However, the error string might
				// be overwritten or deallocated by subsequent calls to other SQLite interface functions.

				// sqlite3 manages the memory allocated for the returned error message
				// so we don't need to call sqlite_free on the returned pointer
				using var db = Connection(stmt);
				return ErrorMsg(db);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_errmsg16", CallingConvention = CallingConvention.Cdecl)]
			private static extern IntPtr sqlite3_errmsg16(IntPtr db);

			/// <summary>Reset a prepared statement</summary>
			public static void Reset(sqlite3_stmt stmt)
			{
				// The result from 'sqlite3_reset' reflects the error code of the last 'sqlite3_step'
				// call. This is legacy behaviour, now step returns the error code immediately. For
				// this reason we can ignore the error code returned by reset.
				sqlite3_reset(stmt.Handle);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_reset", CallingConvention = CallingConvention.Cdecl)]
			private static extern EResult sqlite3_reset(IntPtr stmt);

			/// <summary>Step a prepared statement</summary>
			public static EResult Step(sqlite3_stmt stmt)
			{
				return sqlite3_step(stmt.Handle);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_step", CallingConvention = CallingConvention.Cdecl)]
			private static extern EResult sqlite3_step(IntPtr stmt);

			/// <summary>Return the next prepared statement in the linked list of statements that the 'db' knows about</summary>
			public static bool NextStmt(sqlite3 db, sqlite3_stmt stmt, out sqlite3_stmt next)
			{
				// This does *not* return the next result set, it just iterates the db's interna linked list
				var next_ptr = sqlite3_next_stmt(db.Handle, stmt.Handle);
				next = new NativeSqlite3StmtHandle(next_ptr, false);
				return next_ptr != IntPtr.Zero;
			}
			[DllImport(Dll, EntryPoint = "sqlite3_next_stmt", CallingConvention = CallingConvention.Cdecl)]
			private static extern IntPtr sqlite3_next_stmt(IntPtr db, IntPtr pStmt);

			/// <summary>Returns the string used to create a prepared statement</summary>
			public static string? SqlString(sqlite3_stmt stmt)
			{
				// this assumes sqlite3_prepare_v2 was used to create 'stmt'
				return UTF8toStr(sqlite3_sql(stmt.Handle));
			}
			[DllImport(Dll, EntryPoint = "sqlite3_sql", CallingConvention = CallingConvention.Cdecl)]
			private static extern IntPtr sqlite3_sql(IntPtr stmt);

			/// <summary>Returns the name of the column with 0-based index 'index'</summary>
			public static string ColumnName(sqlite3_stmt stmt, int index)
			{
				return Marshal.PtrToStringUni(sqlite3_column_name16(stmt.Handle, index)) ?? string.Empty;
			}
			[DllImport(Dll, EntryPoint = "sqlite3_column_name16", CallingConvention = CallingConvention.Cdecl)]
			private static extern IntPtr sqlite3_column_name16(IntPtr stmt, int index);

			/// <summary>Returns the number of columns in the result of a prepared statement</summary>
			public static int ColumnCount(sqlite3_stmt stmt)
			{
				return sqlite3_column_count(stmt.Handle);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_column_count", CallingConvention = CallingConvention.Cdecl)]
			private static extern int sqlite3_column_count(IntPtr stmt);

			/// <summary>Returns the internal data type for the column with 0-based index 'index'</summary>
			public static EDataType ColumnType(sqlite3_stmt stmt, int index)
			{
				return sqlite3_column_type(stmt.Handle, index);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_column_type", CallingConvention = CallingConvention.Cdecl)]
			private static extern EDataType sqlite3_column_type(IntPtr stmt, int index);

			/// <summary>Returns the value from the column with 0-based index 'index' as an int</summary>
			public static int ColumnInt32(sqlite3_stmt stmt, int index)
			{
				return sqlite3_column_int(stmt.Handle, index);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_column_int", CallingConvention = CallingConvention.Cdecl)]
			private static extern int sqlite3_column_int(IntPtr stmt, int index);

			/// <summary>Returns the value from the column with 0-based index 'index' as an int64</summary>
			public static long ColumnInt64(sqlite3_stmt stmt, int index)
			{
				return sqlite3_column_int64(stmt.Handle, index);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_column_int64", CallingConvention = CallingConvention.Cdecl)]
			private static extern long sqlite3_column_int64(IntPtr stmt, int index);

			/// <summary>Returns the value from the column with 0-based index 'index' as a double</summary>
			public static double ColumnDouble(sqlite3_stmt stmt, int index)
			{
				return sqlite3_column_double(stmt.Handle, index);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_column_double", CallingConvention = CallingConvention.Cdecl)]
			private static extern double sqlite3_column_double(IntPtr stmt, int index);

			/// <summary>Returns the value from the column with 0-based index 'index' as a string</summary>
			public static string ColumnText(sqlite3_stmt stmt, int index)
			{
				// Sqlite returns null if this column is null
				var ptr = sqlite3_column_text16(stmt.Handle, index);
				if (ptr != IntPtr.Zero) return Marshal.PtrToStringUni(ptr) ?? string.Empty;
				return string.Empty;
			}
			[DllImport(Dll, EntryPoint = "sqlite3_column_text16", CallingConvention = CallingConvention.Cdecl)]
			private static extern IntPtr sqlite3_column_text16(IntPtr stmt, int index);

			/// <summary>Returns the value from the column with 0-based index 'index' as an IntPtr</summary>
			public static byte[] ColumnBlob(sqlite3_stmt stmt, int index)
			{
				ColumnBlob(stmt, index, out var ptr, out var len);

				// Copy the blob out of the DB
				var blob = new byte[len];
				if (len != 0) Marshal.Copy(ptr, blob, 0, blob.Length);
				return blob;
			}
			public static void ColumnBlob(sqlite3_stmt stmt, int index, out IntPtr ptr, out int len)
			{
				// Read the blob size limit
				var db = sqlite3_db_handle(stmt.Handle);
				var max = sqlite3_limit(db, ELimit.Length, -1);

				// sqlite returns null if this column is null
				ptr = sqlite3_column_blob(stmt.Handle, index); // have to call this first
				len = sqlite3_column_bytes(stmt.Handle, index);
				if (len < 0 || len > max)
					throw new SqliteException(EResult.Corrupt, "Blob data size exceeds database maximum size limit", string.Empty);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_column_blob", CallingConvention = CallingConvention.Cdecl)]
			private static extern IntPtr sqlite3_column_blob(IntPtr stmt, int index);

			/// <summary>Returns the size of the data in the column with 0-based index 'index'</summary>
			public static int ColumnBytes(sqlite3_stmt stmt, int index)
			{
				return sqlite3_column_bytes(stmt.Handle, index);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_column_bytes", CallingConvention = CallingConvention.Cdecl)]
			private static extern int sqlite3_column_bytes(IntPtr stmt, int index);

			/// <summary>Return the number of parameters in a prepared statement</summary>
			public static int BindParameterCount(sqlite3_stmt stmt)
			{
				return sqlite3_bind_parameter_count(stmt.Handle);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_bind_parameter_count", CallingConvention = CallingConvention.Cdecl)]
			private static extern int sqlite3_bind_parameter_count(IntPtr stmt);

			/// <summary>Return the index for the parameter named 'name'</summary>
			public static int BindParameterIndex(sqlite3_stmt stmt, string name)
			{
				return sqlite3_bind_parameter_index(stmt.Handle, name);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_bind_parameter_index", CallingConvention = CallingConvention.Cdecl)]
			private static extern int sqlite3_bind_parameter_index(IntPtr stmt, [MarshalAs(UnmanagedType.LPStr)] string name);

			/// <summary>Return the name of a parameter from its index</summary>
			public static string? BindParameterName(sqlite3_stmt stmt, int index)
			{
				return UTF8toStr(sqlite3_bind_parameter_name(stmt.Handle, index));
			}
			[DllImport(Dll, EntryPoint = "sqlite3_bind_parameter_name", CallingConvention = CallingConvention.Cdecl)]
			private static extern IntPtr sqlite3_bind_parameter_name(IntPtr stmt, int index);

			/// <summary>Bind null to 1-based parameter index 'index'</summary>
			public static void BindNull(sqlite3_stmt stmt, int index)
			{
				var r = sqlite3_bind_null(stmt.Handle, index);
				if (r != EResult.OK)
					throw new SqliteException(r, "Bind null failed", ErrorMsg(stmt));
			}
			[DllImport(Dll, EntryPoint = "sqlite3_bind_null", CallingConvention = CallingConvention.Cdecl)]
			private static extern EResult sqlite3_bind_null(IntPtr stmt, int index);

			/// <summary>Bind an integer value to 1-based parameter index 'index'</summary>
			public static void BindInt32(sqlite3_stmt stmt, int index, int val)
			{
				var r = sqlite3_bind_int(stmt.Handle, index, val);
				if (r != EResult.OK)
					throw new SqliteException(r, "Bind int failed", ErrorMsg(stmt));
			}
			[DllImport(Dll, EntryPoint = "sqlite3_bind_int", CallingConvention = CallingConvention.Cdecl)]
			private static extern EResult sqlite3_bind_int(IntPtr stmt, int index, int val);

			/// <summary>Bind an integer64 value to 1-based parameter index 'index'</summary>
			public static void BindInt64(sqlite3_stmt stmt, int index, long val)
			{
				var r = sqlite3_bind_int64(stmt.Handle, index, val);
				if (r != EResult.OK)
					throw new SqliteException(r, "Bind int64 failed", ErrorMsg(stmt));
			}
			[DllImport(Dll, EntryPoint = "sqlite3_bind_int64", CallingConvention = CallingConvention.Cdecl)]
			private static extern EResult sqlite3_bind_int64(IntPtr stmt, int index, long val);

			/// <summary>Bind a double value to 1-based parameter index 'index'</summary>
			public static void BindDouble(sqlite3_stmt stmt, int index, double val)
			{
				var r = sqlite3_bind_double(stmt.Handle, index, val);
				if (r != EResult.OK)
					throw new SqliteException(r, "Bind double failed", ErrorMsg(stmt));
			}
			[DllImport(Dll, EntryPoint = "sqlite3_bind_double", CallingConvention = CallingConvention.Cdecl)]
			private static extern EResult sqlite3_bind_double(IntPtr stmt, int index, double val);

			/// <summary>Bind a string to 1-based parameter index 'index'</summary>
			public static void BindText(sqlite3_stmt stmt, int index, string val)
			{
				var r = sqlite3_bind_text16(stmt.Handle, index, val, -1, TransientData);
				if (r != EResult.OK)
					throw new SqliteException(r, "Bind string failed", ErrorMsg(stmt));
			}
			[DllImport(Dll, EntryPoint = "sqlite3_bind_text16", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
			private static extern EResult sqlite3_bind_text16(IntPtr stmt, int index, string val, int n, IntPtr destructor_cb);

			/// <summary>Bind a byte array to 1-based parameter index 'index'</summary>
			public static void BindBlob(sqlite3_stmt stmt, int index, byte[] val, int length)
			{
				var r = sqlite3_bind_blob(stmt.Handle, index, val, length, TransientData);
				if (r != EResult.OK)
					throw new SqliteException(r, "Bind blob failed", ErrorMsg(stmt));
			}
			[DllImport(Dll, EntryPoint = "sqlite3_bind_blob", CallingConvention = CallingConvention.Cdecl)]
			private static extern EResult sqlite3_bind_blob(IntPtr stmt, int index, byte[] val, int n, IntPtr destructor_cb);

			/// <summary>Set the update hook callback function</summary>
			public static void UpdateHook(sqlite3 db, UpdateHookCB cb, IntPtr ctx)
			{
				sqlite3_update_hook(db.Handle, cb, ctx);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_update_hook", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
			private static extern IntPtr sqlite3_update_hook(IntPtr db, UpdateHookCB cb, IntPtr ctx);

			/// <summary>Register a callback for when a db is unlocked</summary>
			public static EResult UnlockNotify(sqlite3 db, UnlockCB cb, IntPtr ctx)
			{
				// When running in shared-cache mode, a database operation may fail with an SQLITE_LOCKED error if the required
				// locks on the shared-cache or individual tables within the shared-cache cannot be obtained.
				// (See SQLite Shared-Cache Mode for a description of shared-cache locking).This API may be used to register a
				// callback that SQLite will invoke when the connection currently holding the required lock relinquishes it.
				// This API is only available if the library was compiled with the SQLITE_ENABLE_UNLOCK_NOTIFY C - preprocessor symbol defined.

				// The call to sqlite3_unlock_notify() always returns either SQLITE_LOCKED or SQLITE_OK.
				// If SQLITE_LOCKED was returned, then the system is deadlocked. In this case this function needs to return
				// SQLITE_LOCKED to the caller so that the current transaction can be rolled back.
				// Otherwise, block until the unlock-notify callback is invoked, then return SQLITE_OK.
				return sqlite3_unlock_notify(db.Handle, cb, ctx);
			}
			[DllImport(Dll, EntryPoint = "sqlite3_unlock_notify", CallingConvention = CallingConvention.Cdecl)]
			private static extern EResult sqlite3_unlock_notify(IntPtr db, UnlockCB cb, IntPtr ctx);

			/// <summary></summary>
			private static readonly IntPtr TransientData = new IntPtr(-1);
			private delegate void sqlite3_destructor_type(IntPtr ptr);

			/// <summary>Converts an IntPtr that points to a null terminated UTF-8 string into a .NET string</summary>
			private static string? UTF8toStr(IntPtr utf8ptr)
			{
				return Marshal_.PtrToStringUTF8(utf8ptr);
				//if (utf8ptr == IntPtr.Zero)
				//	return null;
				//
				//// There is no Marshal.PtrToStringUtf8 unfortunately, so we have to do it manually
				//// Read up to (but not including) the null terminator
				//byte b;
				//var buf = new List<byte>(256);
				//for (int i = 0; (b = Marshal.ReadByte(utf8ptr, i)) != 0; ++i)
				//	buf.Add(b);
				//
				//var bytes = buf.ToArray();
				//return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
			}

			/// <summary>Converts a C# string (in UTF-16) to a byte array in UTF-8</summary>
			private static byte[] StrToUTF8(string str)
			{
				return Encoding.Convert(Encoding.Unicode, Encoding.UTF8, Encoding.Unicode.GetBytes(str));
			}
		}
	}
}
