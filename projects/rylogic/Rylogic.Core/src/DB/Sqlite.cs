using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Utility;

namespace Rylogic.Db
{
	public static partial class Sqlite
	{
		// Notes:
		//  - This is a Dapper-like, thin wrapper around sqlite3.dll.
		//  - This code uses the native library directly, not SQLite.NET because that would add a dependency.
		//  - It doesn't bother with the type reflection. That is left to the caller as it provides direct
		//    control over the types used to read/write from the database.
		//  - The 'sqlite3' and 'sqlite3_stmt' interfaces are just to add type safety around the 'IntPtr' handles.
		//
		// Threading:
		//  - SQLite supports three different threading modes:
		//      Single-thread = In this mode, all mutexes are disabled and SQLite is unsafe to use in more than a single thread at once.
		//      Serialized    = In serialized mode, SQLite can be safely used by multiple threads with no restriction.
		//      Multi-thread  = In this mode, SQLite can be safely used by multiple threads provided that no single database connection is used simultaneously in two or more threads.
		//  - If the native library is compiled with no 'SQLITE_THREADSAFE' flag or 'SQLITE_THREADSAFE=1', then serialized mode is used.
		//    With 'SQLITE_THREADSAFE=0' the threading mode is single-thread.
		//    With 'SQLITE_THREADSAFE=2' the threading mode is multi-thread.
		//
		// Mutexes:
		//  - The 'SQLITE_OPEN_NOMUTEX' flag causes the database connection to be in the multi-thread mode.
		//  - The 'SQLITE_OPEN_FULLMUTEX' flag causes the connection to be in serialized mode.
		//  - If neither flag is given, the default mode determined by the compile-time and start-time settings.
		//    So, NoMutex = Multi-Threaded, FullMutex = Serialised

		/// <summary>Use this as the file path when you want a database in RAM</summary>
		public const string DBInMemory = ":memory:";

		/// <summary>A database handle</summary>
		public interface sqlite3 : IDisposable
		{
			/// <summary>The raw DB handle</summary>
			IntPtr Handle { get; }

			/// <summary>The result from closing this handle</summary>
			EResult CloseResult { get; }

			/// <summary>Close the handle</summary>
			void Close();
		}

		/// <summary>A compiled query handle (Statement)</summary>
		public interface sqlite3_stmt : IDisposable
		{
			/// <summary>The raw DB handle</summary>
			IntPtr Handle { get; }

			/// <summary>The result from closing this handle</summary>
			EResult CloseResult { get; }

			/// <summary>Close the handle</summary>
			void Close();
		}

		#region Enumerations

		/// <summary>
		/// The data types supported by sqlite.<para/>
		/// SQLite uses these data types:<para/>
		///   integer - A signed integer, stored in 1, 2, 3, 4, 6, or 8 bytes depending on the magnitude of the value.<para/>
		///   real    - A floating point value, stored as an 8-byte IEEE floating point number.<para/>
		///   text    - A text string, stored using the database encoding (UTF-8, UTF-16BE or UTF-16LE).<para/>
		///   blob    - A blob of data, stored exactly as it was input.<para/>
		///   null    - the null value<para/>
		/// All other type keywords are mapped to these types<para/></summary>
		public enum EDataType
		{
			Integer = 1,
			Real = 2,
			Text = 3,
			Blob = 4,
			Null = 5,
		}

		/// <summary>Result codes returned by sqlite dll calls</summary>
		public enum EResult
		{
			/// <summary>Successful result</summary>
			OK = 0,

			/// <summary>SQL error or missing database</summary>
			Error = 1,

			/// <summary>Internal logic error in SQLite</summary>
			Internal = 2,

			/// <summary>Access permission denied</summary>
			Perm = 3,

			/// <summary>Callback routine requested an abort</summary>
			Abort = 4,

			/// <summary>The database file is locked</summary>
			Busy = 5,

			/// <summary>A table in the database is locked</summary>
			Locked = 6,

			/// <summary>A 'malloc' failed</summary>
			NoMem = 7,

			/// <summary>Attempt to write a readonly database</summary>
			ReadOnly = 8,

			/// <summary>Operation terminated by sqlite3_interrupt()</summary>
			Interrupt = 9,

			/// <summary>Some kind of disk I/O error occurred</summary>
			IOError = 10,

			/// <summary>The database disk image is malformed</summary>
			Corrupt = 11,

			/// <summary>Unknown op-code in sqlite3_file_control()</summary>
			NotFound = 12,

			/// <summary>Insertion failed because database is full</summary>
			Full = 13,

			/// <summary>Unable to open the database file</summary>
			CantOpen = 14,

			/// <summary>Database lock protocol error</summary>
			Protocol = 15,

			/// <summary>Database is empty</summary>
			Empty = 16,

			/// <summary>The database schema changed</summary>
			Schema = 17,

			/// <summary>String or BLOB exceeds size limit</summary>
			TooBig = 18,

			/// <summary>Abort due to constraint violation</summary>
			Constraint = 19,

			/// <summary>Data type mismatch</summary>
			Mismatch = 20,

			/// <summary>Library used incorrectly</summary>
			Misuse = 21,

			/// <summary>Uses OS features not supported on host</summary>
			NoLfs = 22,

			/// <summary>Authorization denied</summary>
			Auth = 23,

			/// <summary>Auxiliary database format error</summary>
			Format = 24,

			/// <summary>2nd parameter to sqlite3_bind out of range</summary>
			Range = 25,

			/// <summary>File opened that is not a database file</summary>
			NotADB = 26,

			/// <summary>Not returned by any C/C++ interface. However, SQLITE_NOTICE (or rather one of its extended error codes) is sometimes used as the first argument in an sqlite3_log() callback to indicate that an unusual operation is taking place.</summary>
			Notice = 27,

			/// <summary>Not returned by any C/C++ interface. However, SQLITE_WARNING (or rather one of its extended error codes) is sometimes used as the first argument in an sqlite3_log() callback to indicate that an unusual and possibly ill-advised operation is taking place.</summary>
			Warning = 28,

			/// <summary>sqlite3_step() has another row ready</summary>
			Row = 100,

			/// <summary>sqlite3_step() has finished executing</summary>
			Done = 101,

			// *** Extended Error Codes ***

			///<summary>The sqlite3_load_extension() interface loads an extension into a single database connection.The default behavior is for that extension to be automatically unloaded when the database connection closes.However, if the extension entry point returns SQLITE_OK_LOAD_PERMANENTLY instead of SQLITE_OK, then the extension remains loaded into the process address space after the database connection closes.In other words, the xDlClose methods of the sqlite3_vfs object is not called for the extension when the database connection closes. The SQLITE_OK_LOAD_PERMANENTLY return code is useful to loadable extensions that register new VFSes, for example.</summary>
			OK_LOAD_PERMANENTLY = 256,

			///<summary>The SQLITE_ERROR_MISSING_COLLSEQ result code means that an SQL statement could not be prepared because a collating sequence named in that SQL statement could not be located. Sometimes when this error code is encountered, the sqlite3_prepare_v2() routine will convert the error into SQLITE_ERROR_RETRY and try again to prepare the SQL statement using a different query plan that does not require the use of the unknown collating sequence.</summary>
			ERROR_MISSING_COLLSEQ = 257,

			///<summary>The SQLITE_BUSY_RECOVERY error code is an extended error code for SQLITE_BUSY that indicates that an operation could not continue because another process is busy recovering a WAL mode database file following a crash.The SQLITE_BUSY_RECOVERY error code only occurs on WAL mode databases.</summary>
			Busy_Recovery = 261,

			///<summary>The SQLITE_LOCKED_SHAREDCACHE result code indicates that access to an SQLite data record is blocked by another database connection that is using the same record in shared cache mode.When two or more database connections share the same cache and one of the connections is in the middle of modifying a record in that cache, then other connections are blocked from accessing that data while the modifications are on-going in order to prevent the readers from seeing a corrupt or partially completed change.</summary>
			Locked_SharedCache = 262,

			///<summary>The SQLITE_READONLY_RECOVERY error code is an extended error code for SQLITE_READONLY.The SQLITE_READONLY_RECOVERY error code indicates that a WAL mode database cannot be opened because the database file needs to be recovered and recovery requires write access but only read access is available.</summary>
			Readonly_Recovery = 264,

			///<summary>The SQLITE_IOERR_READ error code is an extended error code for SQLITE_IOERR indicating an I/O error in the VFS layer while trying to read from a file on disk.This error might result from a hardware malfunction or because a filesystem came unmounted while the file was open.</summary>
			IOERR_READ = 266,

			///<summary>The SQLITE_CORRUPT_VTAB error code is an extended error code for SQLITE_CORRUPT used by virtual tables.A virtual table might return SQLITE_CORRUPT_VTAB to indicate that content in the virtual table is corrupt.</summary>
			CORRUPT_VTAB = 267,

			///<summary>The SQLITE_CANTOPEN_NOTEMPDIR error code is no longer used.</summary>
			CANTOPEN_NOTEMPDIR = 270,

			///<summary>The SQLITE_CONSTRAINT_CHECK error code is an extended error code for SQLITE_CONSTRAINT indicating that a CHECK constraint failed.</summary>
			CONSTRAINT_CHECK = 275,

			///<summary>The SQLITE_AUTH_USER error code is an extended error code for SQLITE_AUTH indicating that an operation was attempted on a database for which the logged in user lacks sufficient authorization.</summary>
			AUTH_USER = 279,

			///<summary>The SQLITE_NOTICE_RECOVER_WAL result code is passed to the callback of sqlite3_log() when a WAL mode database file is recovered.</summary>
			NOTICE_RECOVER_WAL = 283,

			///<summary>The SQLITE_WARNING_AUTOINDEX result code is passed to the callback of sqlite3_log() whenever automatic indexing is used.This can serve as a warning to application designers that the database might benefit from additional indexes.</summary>
			WARNING_AUTOINDEX = 284,

			///<summary>The SQLITE_ERROR_RETRY is used internally to provoke sqlite3_prepare_v2() (or one of its sibling routines for creating prepared statements) to try again to prepare a statement that failed with an error on the previous attempt.</summary>
			ERROR_RETRY = 513,

			///<summary>The SQLITE_ABORT_ROLLBACK error code is an extended error code for SQLITE_ABORT indicating that an SQL statement aborted because the transaction that was active when the SQL statement first started was rolled back.Pending write operations always fail with this error when a rollback occurs.A ROLLBACK will cause a pending read operation to fail only if the schema was changed within the transaction being rolled back.</summary>
			ABORT_ROLLBACK = 516,

			///<summary>The SQLITE_BUSY_SNAPSHOT error code is an extended error code for SQLITE_BUSY that occurs on WAL mode databases when a database connection tries to promote a read transaction into a write transaction but finds that another database connection has already written to the database and thus invalidated prior reads. The following scenario illustrates how an SQLITE_BUSY_SNAPSHOT error might arise: Process A starts a read transaction on the database and does one or more SELECT statement. Process A keeps the transaction open. Process B updates the database, changing values previous read by process A. Process A now tries to write to the database.But process A's view of the database content is now obsolete because process B has modified the database file after process A read from it. Hence process A gets an SQLITE_BUSY_SNAPSHOT error.</summary>
			BUSY_SNAPSHOT = 517,

			///<summary>The SQLITE_LOCKED_VTAB result code is not used by the SQLite core, but it is available for use by extensions.Virtual table implementations can return this result code to indicate that they cannot complete the current operation because of locks held by other threads or processes. The R-Tree extension returns this result code when an attempt is made to update the R-Tree while another prepared statement is actively reading the R-Tree.The update cannot proceed because any change to an R-Tree might involve reshuffling and rebalancing of nodes, which would disrupt read cursors, causing some rows to be repeated and other rows to be omitted.</summary>
			LOCKED_VTAB = 518,

			///<summary>The SQLITE_READONLY_CANTLOCK error code is an extended error code for SQLITE_READONLY. The SQLITE_READONLY_CANTLOCK error code indicates that SQLite is unable to obtain a read lock on a WAL mode database because the shared-memory file associated with that database is read-only.</summary>
			READONLY_CANTLOCK = 520,

			///<summary>The SQLITE_IOERR_SHORT_READ error code is an extended error code for SQLITE_IOERR indicating that a read attempt in the VFS layer was unable to obtain as many bytes as was requested. This might be due to a truncated file.</summary>
			IOERR_SHORT_READ = 522,

			///<summary>The SQLITE_CORRUPT_SEQUENCE result code means that the schema of the sqlite_sequence table is corrupt. The sqlite_sequence table is used to help implement the AUTOINCREMENT feature. The sqlite_sequence table should have the following format: CREATE TABLE sqlite_sequence(name,seq); If SQLite discovers that the sqlite_sequence table has any other format, it returns the SQLITE_CORRUPT_SEQUENCE error.</summary>
			CORRUPT_SEQUENCE = 523,

			///<summary>The SQLITE_CANTOPEN_ISDIR error code is an extended error code for SQLITE_CANTOPEN indicating that a file open operation failed because the file is really a directory.</summary>
			CANTOPEN_ISDIR = 526,

			///<summary>The SQLITE_CONSTRAINT_COMMITHOOK error code is an extended error code for SQLITE_CONSTRAINT indicating that a commit hook callback returned non-zero that thus caused the SQL statement to be rolled back.</summary>
			CONSTRAINT_COMMITHOOK = 531,

			///<summary>The SQLITE_NOTICE_RECOVER_ROLLBACK result code is passed to the callback of sqlite3_log() when a hot journal is rolled back.</summary>
			NOTICE_RECOVER_ROLLBACK = 539,

			///<summary>The SQLITE_ERROR_SNAPSHOT result code might be returned when attempting to start a read transaction on an historical version of the database by using the sqlite3_snapshot_open() interface. If the historical snapshot is no longer available, then the read transaction will fail with the SQLITE_ERROR_SNAPSHOT.This error code is only possible if SQLite is compiled with -DSQLITE_ENABLE_SNAPSHOT.</summary>
			ERROR_SNAPSHOT = 769,

			///<summary>The SQLITE_BUSY_TIMEOUT error code indicates that a blocking Posix advisory file lock request in the VFS layer failed due to a timeout.Blocking Posix advisory locks are only available as a proprietary SQLite extension and even then are only supported if SQLite is compiled with the SQLITE_EANBLE_SETLK_TIMEOUT compile-time option.</summary>
			BUSY_TIMEOUT = 773,

			///<summary>The SQLITE_READONLY_ROLLBACK error code is an extended error code for SQLITE_READONLY.The SQLITE_READONLY_ROLLBACK error code indicates that a database cannot be opened because it has a hot journal that needs to be rolled back but cannot because the database is readonly.</summary>
			READONLY_ROLLBACK = 776,

			///<summary>The SQLITE_IOERR_WRITE error code is an extended error code for SQLITE_IOERR indicating an I/O error in the VFS layer while trying to write into a file on disk.This error might result from a hardware malfunction or because a filesystem came unmounted while the file was open. This error should not occur if the filesystem is full as there is a separate error code (SQLITE_FULL) for that purpose.</summary>
			IOERR_WRITE = 778,

			///<summary>The SQLITE_CORRUPT_INDEX result code means that SQLite detected an entry is or was missing from an index.This is a special case of the SQLITE_CORRUPT error code that suggests that the problem might be resolved by running the REINDEX command, assuming no other problems exist elsewhere in the database file.</summary>
			CORRUPT_INDEX = 779,

			///<summary>The SQLITE_CANTOPEN_FULLPATH error code is an extended error code for SQLITE_CANTOPEN indicating that a file open operation failed because the operating system was unable to convert the filename into a full pathname.</summary>
			CANTOPEN_FULLPATH = 782,

			///<summary>The SQLITE_CONSTRAINT_FOREIGNKEY error code is an extended error code for SQLITE_CONSTRAINT indicating that a foreign key constraint failed.</summary>
			CONSTRAINT_FOREIGNKEY = 787,

			///<summary>The SQLITE_READONLY_DBMOVED error code is an extended error code for SQLITE_READONLY.The SQLITE_READONLY_DBMOVED error code indicates that a database cannot be modified because the database file has been moved since it was opened, and so any attempt to modify the database might result in database corruption if the processes crashes because the rollback journal would not be correctly named.</summary>
			READONLY_DBMOVED = 1032,

			///<summary>The SQLITE_IOERR_FSYNC error code is an extended error code for SQLITE_IOERR indicating an I/O error in the VFS layer while trying to flush previously written content out of OS and/or disk-control buffers and into persistent storage.In other words, this code indicates a problem with the fsync() system call in unix or the FlushFileBuffers() system call in windows.</summary>
			IOERR_FSYNC = 1034,

			///<summary>The SQLITE_CANTOPEN_CONVPATH error code is an extended error code for SQLITE_CANTOPEN used only by Cygwin VFS and indicating that the cygwin_conv_path() system call failed while trying to open a file.See also: SQLITE_IOERR_CONVPATH</summary>
			CANTOPEN_CONVPATH = 1038,

			///<summary>The SQLITE_CONSTRAINT_FUNCTION error code is not currently used by the SQLite core.However, this error code is available for use by extension functions.</summary>
			CONSTRAINT_FUNCTION = 1043,

			///<summary>The SQLITE_READONLY_CANTINIT result code originates in the xShmMap method of a VFS to indicate that the shared memory region used by WAL mode exists buts its content is unreliable and unusable by the current process since the current process does not have write permission on the shared memory region. (The shared memory region for WAL mode is normally a file with a "-wal" suffix that is mmapped into the process space. If the current process does not have write permission on that file, then it cannot write into shared memory.) Higher level logic within SQLite will normally intercept the error code and create a temporary in-memory shared memory region so that the current process can at least read the content of the database. This result code should not reach the application interface layer.</summary>
			READONLY_CANTINIT = 1288,

			///<summary>The SQLITE_IOERR_DIR_FSYNC error code is an extended error code for SQLITE_IOERR indicating an I/O error in the VFS layer while trying to invoke fsync() on a directory.The unix VFS attempts to fsync() directories after creating or deleting certain files to ensure that those files will still appear in the filesystem following a power loss or system crash.This error code indicates a problem attempting to perform that fsync().</summary>
			IOERR_DIR_FSYNC = 1290,

			///<summary>The SQLITE_CANTOPEN_DIRTYWAL result code is not used at this time.</summary>
			CANTOPEN_DIRTYWAL = 1294,

			///<summary>The SQLITE_CONSTRAINT_NOTNULL error code is an extended error code for SQLITE_CONSTRAINT indicating that a NOT NULL constraint failed.</summary>
			CONSTRAINT_NOTNULL = 1299,

			///<summary>The SQLITE_READONLY_DIRECTORY result code indicates that the database is read-only because process does not have permission to create a journal file in the same directory as the database and the creation of a journal file is a prerequisite for writing.</summary>
			READONLY_DIRECTORY = 1544,

			///<summary>The SQLITE_IOERR_TRUNCATE error code is an extended error code for SQLITE_IOERR indicating an I/O error in the VFS layer while trying to truncate a file to a smaller size.</summary>
			IOERR_TRUNCATE = 1546,

			///<summary>The SQLITE_CANTOPEN_SYMLINK result code is returned by the sqlite3_open() interface and its siblings when the SQLITE_OPEN_NOFOLLOW flag is used and the database file is a symbolic link.</summary>
			CANTOPEN_SYMLINK = 1550,

			///<summary>The SQLITE_CONSTRAINT_PRIMARYKEY error code is an extended error code for SQLITE_CONSTRAINT indicating that a PRIMARY KEY constraint failed.</summary>
			CONSTRAINT_PRIMARYKEY = 1555,

			///<summary>The SQLITE_IOERR_FSTAT error code is an extended error code for SQLITE_IOERR indicating an I/O error in the VFS layer while trying to invoke fstat() (or the equivalent) on a file in order to determine information such as the file size or access permissions.</summary>
			IOERR_FSTAT = 1802,

			///<summary>The SQLITE_CONSTRAINT_TRIGGER error code is an extended error code for SQLITE_CONSTRAINT indicating that a RAISE function within a trigger fired, causing the SQL statement to abort.</summary>
			CONSTRAINT_TRIGGER = 1811,

			///<summary>The SQLITE_IOERR_UNLOCK error code is an extended error code for SQLITE_IOERR indicating an I/O error within xUnlock method on the sqlite3_io_methods object.</summary>
			IOERR_UNLOCK = 2058,

			///<summary>The SQLITE_CONSTRAINT_UNIQUE error code is an extended error code for SQLITE_CONSTRAINT indicating that a UNIQUE constraint failed.</summary>
			CONSTRAINT_UNIQUE = 2067,

			///<summary>The SQLITE_IOERR_UNLOCK error code is an extended error code for SQLITE_IOERR indicating an I/O error within xLock method on the sqlite3_io_methods object while trying to obtain a read lock.</summary>
			IOERR_RDLOCK = 2314,

			///<summary>The SQLITE_CONSTRAINT_VTAB error code is not currently used by the SQLite core.However, this error code is available for use by application-defined virtual tables.</summary>
			CONSTRAINT_VTAB = 2323,

			///<summary>The SQLITE_IOERR_UNLOCK error code is an extended error code for SQLITE_IOERR indicating an I/O error within xDelete method on the sqlite3_vfs object.</summary>
			IOERR_DELETE = 2570,

			///<summary>The SQLITE_CONSTRAINT_ROWID error code is an extended error code for SQLITE_CONSTRAINT indicating that a rowid is not unique.</summary>
			CONSTRAINT_ROWID = 2579,

			///<summary>The SQLITE_IOERR_BLOCKED error code is no longer used.</summary>
			IOERR_BLOCKED = 2826,

			///<summary>The SQLITE_CONSTRAINT_PINNED error code is an extended error code for SQLITE_CONSTRAINT indicating that an UPDATE trigger attempted do delete the row that was being updated in the middle of the update.</summary>
			CONSTRAINT_PINNED = 2835,

			///<summary>The SQLITE_IOERR_NOMEM error code is sometimes returned by the VFS layer to indicate that an operation could not be completed due to the inability to allocate sufficient memory. This error code is normally converted into SQLITE_NOMEM by the higher layers of SQLite before being returned to the application.</summary>
			IOERR_NOMEM = 3082,

			///<summary>The SQLITE_CONSTRAINT_DATATYPE error code is an extended error code for SQLITE_CONSTRAINT indicating that an insert or update attempted to store a value inconsistent with the column's declared type in a table defined as STRICT.</summary>
			CONSTRAINT_DATATYPE = 3091,

			///<summary>The SQLITE_IOERR_ACCESS error code is an extended error code for SQLITE_IOERR indicating an I/O error within the xAccess method on the sqlite3_vfs object.</summary>
			IOERR_ACCESS = 3338,

			///<summary>The SQLITE_IOERR_CHECKRESERVEDLOCK error code is an extended error code for SQLITE_IOERR indicating an I/O error within the xCheckReservedLock method on the sqlite3_io_methods object.</summary>
			IOERR_CHECKRESERVEDLOCK = 3594,

			///<summary>The SQLITE_IOERR_LOCK error code is an extended error code for SQLITE_IOERR indicating an I/O error in the advisory file locking logic. Usually an SQLITE_IOERR_LOCK error indicates a problem obtaining a PENDING lock. However it can also indicate miscellaneous locking errors on some of the specialized VFSes used on Macs.</summary>
			IOERR_LOCK = 3850,

			///<summary>The SQLITE_IOERR_ACCESS error code is an extended error code for SQLITE_IOERR indicating an I/O error within the xClose method on the sqlite3_io_methods object.</summary>
			IOERR_CLOSE = 4106,

			///<summary>The SQLITE_IOERR_DIR_CLOSE error code is no longer used.</summary>
			IOERR_DIR_CLOSE = 4362,

			///<summary>The SQLITE_IOERR_SHMOPEN error code is an extended error code for SQLITE_IOERR indicating an I/O error within the xShmMap method on the sqlite3_io_methods object while trying to open a new shared memory segment.</summary>
			IOERR_SHMOPEN = 4618,

			///<summary>The SQLITE_IOERR_SHMSIZE error code is an extended error code for SQLITE_IOERR indicating an I/O error within the xShmMap method on the sqlite3_io_methods object while trying to enlarge a "shm" file as part of WAL mode transaction processing.This error may indicate that the underlying filesystem volume is out of space.</summary>
			IOERR_SHMSIZE = 4874,

			///<summary>The SQLITE_IOERR_SHMLOCK error code is no longer used.</summary>
			IOERR_SHMLOCK = 5130,

			///<summary>The SQLITE_IOERR_SHMMAP error code is an extended error code for SQLITE_IOERR indicating an I/O error within the xShmMap method on the sqlite3_io_methods object while trying to map a shared memory segment into the process address space.</summary>
			IOERR_SHMMAP = 5386,

			///<summary>The SQLITE_IOERR_SEEK error code is an extended error code for SQLITE_IOERR indicating an I/O error within the xRead or xWrite methods on the sqlite3_io_methods object while trying to seek a file descriptor to the beginning point of the file where the read or write is to occur.</summary>
			IOERR_SEEK = 5642,

			///<summary>The SQLITE_IOERR_DELETE_NOENT error code is an extended error code for SQLITE_IOERR indicating that the xDelete method on the sqlite3_vfs object failed because the file being deleted does not exist.</summary>
			IOERR_DELETE_NOENT = 5898,

			///<summary>The SQLITE_IOERR_MMAP error code is an extended error code for SQLITE_IOERR indicating an I/O error within the xFetch or xUnfetch methods on the sqlite3_io_methods object while trying to map or unmap part of the database file into the process address space.</summary>
			IOERR_MMAP = 6154,

			///<summary>The SQLITE_IOERR_GETTEMPPATH error code is an extended error code for SQLITE_IOERR indicating that the VFS is unable to determine a suitable directory in which to place temporary files.</summary>
			IOERR_GETTEMPPATH = 6410,

			///<summary>The SQLITE_IOERR_CONVPATH error code is an extended error code for SQLITE_IOERR used only by Cygwin VFS and indicating that the cygwin_conv_path() system call failed.See also: SQLITE_CANTOPEN_CONVPATH</summary>
			IOERR_CONVPATH = 6666,

			///<summary>The SQLITE_IOERR_VNODE error code is a code reserved for use by extensions.It is not used by the SQLite core.</summary>
			IOERR_VNODE = 6922,

			///<summary>The SQLITE_IOERR_AUTH error code is a code reserved for use by extensions.It is not used by the SQLite core.</summary>
			IOERR_AUTH = 7178,

			///<summary>The SQLITE_IOERR_BEGIN_ATOMIC error code indicates that the underlying operating system reported and error on the SQLITE_FCNTL_BEGIN_ATOMIC_WRITE file-control.This only comes up when SQLITE_ENABLE_ATOMIC_WRITE is enabled and the database is hosted on a filesystem that supports atomic writes.</summary>
			IOERR_BEGIN_ATOMIC = 7434,

			///<summary>The SQLITE_IOERR_COMMIT_ATOMIC error code indicates that the underlying operating system reported and error on the SQLITE_FCNTL_COMMIT_ATOMIC_WRITE file-control.This only comes up when SQLITE_ENABLE_ATOMIC_WRITE is enabled and the database is hosted on a filesystem that supports atomic writes.</summary>
			IOERR_COMMIT_ATOMIC = 7690,

			///<summary>The SQLITE_IOERR_ROLLBACK_ATOMIC error code indicates that the underlying operating system reported and error on the SQLITE_FCNTL_ROLLBACK_ATOMIC_WRITE file-control.This only comes up when SQLITE_ENABLE_ATOMIC_WRITE is enabled and the database is hosted on a filesystem that supports atomic writes.</summary>
			IOERR_ROLLBACK_ATOMIC = 7946,

			///<summary>The SQLITE_IOERR_DATA error code is an extended error code for SQLITE_IOERR used only by checksum VFS shim to indicate that the checksum on a page of the database file is incorrect.</summary>
			IOERR_DATA = 8202,

			///<summary>The SQLITE_IOERR_CORRUPTFS error code is an extended error code for SQLITE_IOERR used only by a VFS to indicate that a seek or read failure was due to the request not falling within the file's boundary rather than an ordinary device failure. This often indicates a corrupt filesystem.</summary>
			IOERR_CORRUPTFS = 8458,
		}

		/// <summary></summary>
		public enum EAuthorizerActionCode
		{                             //   3rd             4th parameter
			CREATE_INDEX        = 1,  // Index Name      Table Name
			CREATE_TABLE        = 2,  // Table Name      NULL
			CREATE_TEMP_INDEX   = 3,  // Index Name      Table Name
			CREATE_TEMP_TABLE   = 4,  // Table Name      NULL
			CREATE_TEMP_TRIGGER = 5,  // Trigger Name    Table Name
			CREATE_TEMP_VIEW    = 6,  // View Name       NULL
			CREATE_TRIGGER      = 7,  // Trigger Name    Table Name
			CREATE_VIEW         = 8,  // View Name       NULL
			DELETE              = 9,  // Table Name      NULL
			DROP_INDEX          = 10, // Index Name      Table Name
			DROP_TABLE          = 11, // Table Name      NULL
			DROP_TEMP_INDEX     = 12, // Index Name      Table Name
			DROP_TEMP_TABLE     = 13, // Table Name      NULL
			DROP_TEMP_TRIGGER   = 14, // Trigger Name    Table Name
			DROP_TEMP_VIEW      = 15, // View Name       NULL
			DROP_TRIGGER        = 16, // Trigger Name    Table Name
			DROP_VIEW           = 17, // View Name       NULL
			INSERT              = 18, // Table Name      NULL
			PRAGMA              = 19, // Pragma Name     1st arg or NULL
			READ                = 20, // Table Name      Column Name
			SELECT              = 21, // NULL            NULL
			TRANSACTION         = 22, // Operation       NULL
			UPDATE              = 23, // Table Name      Column Name
			ATTACH              = 24, // Filename        NULL
			DETACH              = 25, // Database Name   NULL
			ALTER_TABLE         = 26, // Database Name   Table Name
			REINDEX             = 27, // Index Name      NULL
			ANALYZE             = 28, // Table Name      NULL
			CREATE_VTABLE       = 29, // Table Name      Module Name
			DROP_VTABLE         = 30, // Table Name      Module Name
			FUNCTION            = 31, // NULL            Function Name
			SAVEPOINT           = 32, // Operation       Save point Name
			COPY                = 0,  // No longer used
		}

		/// <summary>Flags passed to the sqlite3_open_v2 function</summary>
		[Flags]
		public enum EOpenFlags
		{
			None = 0,

			/// <summary>The database is opened in read-only mode. If the database does not already exist, an error is returned.</summary>
			ReadOnly = 1 << 0,

			/// <summary>The database is opened for reading and writing if possible, or reading only if the file is write protected by the operating system. In either case the database must already exist, otherwise an error is returned.</summary>
			ReadWrite = 1 << 1,

			/// <summary>The database is created if it does not already exist. This is the behavior that is always used for sqlite3_open() and sqlite3_open16().</summary>
			Create = 1 << 2,

			/// <summary></summary>
			DeleteOnClose = 1 << 3,
			Exclusive = 1 << 4,
			AutoProxy = 1 << 5,

			/// <summary>The filename can be interpreted as a URI if this flag is set.</summary>
			URI = 1 << 6,

			/// <summary>The database will be opened as an in-memory database. The database is named by the "filename" argument for the purposes of cache-sharing, if shared cache mode is enabled, but the "filename" is otherwise ignored.</summary>
			Memory = 1 << 7,

			/// <summary></summary>
			MainDB = 1 << 8,
			TempDB = 1 << 9,
			TransientDB = 1 << 10,
			MainJournal = 1 << 11,
			TempJournal = 1 << 12,
			SubJournal = 1 << 13,
			SuperJournal = 1 << 14,

			/// <summary>The new database connection will use the "multi-thread" threading mode. This means that separate threads are allowed to use SQLite at the same time, as long as each thread is using a different database connection.</summary>
			NoMutex = 1 << 15,

			/// <summary>The new database connection will use the "serialized" threading mode. This means the multiple threads can safely attempt to use the same database connection at the same time. (Mutexes will block any actual concurrency, but in this mode there is no harm in trying.)</summary>
			FullMutex = 1 << 16,

			/// <summary>The database is opened shared cache enabled, overriding the default shared cache setting provided by sqlite3_enable_shared_cache().</summary>
			SharedCache = 1 << 17,

			/// <summary>The database is opened shared cache disabled, overriding the default shared cache setting provided by sqlite3_enable_shared_cache().</summary>
			PrivateCache = 1 << 18,

			/// <summary></summary>
			WAL = 1 << 19,

			/// <summary></summary>
			ProtectionComplete = 1 << 20,
			ProtectionCompleteUnlessOpen = 2 << 20,
			ProtectionCompleteUntilFirstUserAuthentication = 3 << 20,
			ProtectionNone = 4 << 20,

			/// <summary>The database filename is not allowed to be a symbolic link</summary>
			NoFollow = 1 << 24,

			/// <summary>The database connection comes up in "extended result code mode". In other words, the database behaves has if sqlite3_extended_result_codes(db,1) where called on the database connection as soon as the connection is created. In addition to setting the extended result code mode, this flag also causes sqlite3_open_v2() to return an extended result code.</summary>
			ExResCode = 1 << 25,
		}

		/// <summary>Sqlite limit categories</summary>
		public enum ELimit
		{
			Length = 0,
			SqlLength = 1,
			Column = 2,
			ExprDepth = 3,
			CompoundSelect = 4,
			VdbeOp = 5,
			FunctionArg = 6,
			Attached = 7,
			LikePatternLength = 8,
			VariableNumber = 9,
			TriggerDepth = 10,
		}

		/// <summary>Sqlite configuration options</summary>
		public enum EConfigOption
		{
			/// <summary>Sqlite has no mutexs and should only be used from a single thread (across all connections)</summary>
			SingleThread = 1,

			/// <summary>Sqlite is compiled with mutexes. One-thread per connection and statements created on that connection</summary>
			MultiThread = 2,

			/// <summary>Sqlite is compiled with mutexes. Any thread, any connection.</summary>
			Serialized = 3,

			/// <summary>The SQLITE_CONFIG_MALLOC option takes a single argument which is a pointer to an instance of the sqlite3_mem_methods structure. The argument specifies alternative low-level memory allocation routines to be used in place of the memory allocation routines built into SQLite. SQLite makes its own private copy of the content of the sqlite3_mem_methods structure before the sqlite3_config() call returns.</summary>
			Malloc = 4, // sqlite3_mem_methods

			///<summary>The SQLITE_CONFIG_GETMALLOC option takes a single argument which is a pointer to an instance of the sqlite3_mem_methods structure. The sqlite3_mem_methods structure is filled with the currently defined memory allocation routines. This option can be used to overload the default memory allocation routines with a wrapper that simulations memory allocation failure or tracks memory usage, for example.</summary>
			GetMalloc = 5, // sqlite3_mem_methods*

			///<summary>No longer used</summary>
			Scratch = 6,

			///<summary>The SQLITE_CONFIG_PAGECACHE option specifies a memory pool that SQLite can use for the database page cache with the default page cache implementation. This configuration option is a no-op if an application-defined page cache implementation is loaded using the SQLITE_CONFIG_PCACHE2. There are three arguments to SQLITE_CONFIG_PAGECACHE: A pointer to 8-byte aligned memory (pMem), the size of each page cache line (sz), and the number of cache lines (N). The sz argument should be the size of the largest database page (a power of two between 512 and 65536) plus some extra bytes for each page header. The number of extra bytes needed by the page header can be determined using SQLITE_CONFIG_PCACHE_HDRSZ. It is harmless, apart from the wasted memory, for the sz parameter to be larger than necessary. The pMem argument must be either a NULL pointer or a pointer to an 8-byte aligned block of memory of at least sz*N bytes, otherwise subsequent behavior is undefined. When pMem is not NULL, SQLite will strive to use the memory provided to satisfy page cache needs, falling back to sqlite3_malloc() if a page cache line is larger than sz bytes or if all of the pMem buffer is exhausted. If pMem is NULL and N is non-zero, then each database connection does an initial bulk allocation for page cache memory from sqlite3_malloc() sufficient for N cache lines if N is positive or of -1024*N bytes if N is negative, . If additional page cache memory is needed beyond what is provided by the initial allocation, then SQLite goes to sqlite3_malloc() separately for each additional cache line.</summary>
			PageCache = 7, // void*, int sz, int N

			///<summary>The SQLITE_CONFIG_HEAP option specifies a static memory buffer that SQLite will use for all of its dynamic memory allocation needs beyond those provided for by SQLITE_CONFIG_PAGECACHE. The SQLITE_CONFIG_HEAP option is only available if SQLite is compiled with either SQLITE_ENABLE_MEMSYS3 or SQLITE_ENABLE_MEMSYS5 and returns SQLITE_ERROR if invoked otherwise. There are three arguments to SQLITE_CONFIG_HEAP: An 8-byte aligned pointer to the memory, the number of bytes in the memory buffer, and the minimum allocation size. If the first pointer (the memory pointer) is NULL, then SQLite reverts to using its default memory allocator (the system malloc() implementation), undoing any prior invocation of SQLITE_CONFIG_MALLOC. If the memory pointer is not NULL then the alternative memory allocator is engaged to handle all of SQLites memory allocation needs. The first pointer (the memory pointer) must be aligned to an 8-byte boundary or subsequent behavior of SQLite will be undefined. The minimum allocation size is capped at 2**12. Reasonable values for the minimum allocation size are 2**5 through 2**8.</summary>
			Heap = 8, // void*, int nByte, int min

			///<summary>The SQLITE_CONFIG_MEMSTATUS option takes single argument of type int, interpreted as a boolean, which enables or disables the collection of memory allocation statistics. When memory allocation statistics are disabled, the following SQLite interfaces become non-operational: sqlite3_hard_heap_limit64() sqlite3_memory_used() sqlite3_memory_highwater() sqlite3_soft_heap_limit64() sqlite3_status64() Memory allocation statistics are enabled by default unless SQLite is compiled with SQLITE_DEFAULT_MEMSTATUS=0 in which case memory allocation statistics are disabled by default.</summary>
			MemStatus = 9, // boolean

			///<summary>The SQLITE_CONFIG_MUTEX option takes a single argument which is a pointer to an instance of the sqlite3_mutex_methods structure. The argument specifies alternative low-level mutex routines to be used in place the mutex routines built into SQLite. SQLite makes a copy of the content of the sqlite3_mutex_methods structure before the call to sqlite3_config() returns. If SQLite is compiled with the SQLITE_THREADSAFE=0 compile-time option then the entire mutexing subsystem is omitted from the build and hence calls to sqlite3_config() with the SQLITE_CONFIG_MUTEX configuration option will return SQLITE_ERROR.</summary>
			Mutex = 10, // sqlite3_mutex_methods*

			///<summary>The SQLITE_CONFIG_GETMUTEX option takes a single argument which is a pointer to an instance of the sqlite3_mutex_methods structure. The sqlite3_mutex_methods structure is filled with the currently defined mutex routines. This option can be used to overload the default mutex allocation routines with a wrapper used to track mutex usage for performance profiling or testing, for example. If SQLite is compiled with the SQLITE_THREADSAFE=0 compile-time option then the entire mutexing subsystem is omitted from the build and hence calls to sqlite3_config() with the SQLITE_CONFIG_GETMUTEX configuration option will return SQLITE_ERROR.</summary>
			GetMutex = 11, // sqlite3_mutex_methods*

			///<summary>The SQLITE_CONFIG_LOOKASIDE option takes two arguments that determine the default size of lookaside memory on each database connection. The first argument is the size of each lookaside buffer slot and the second is the number of slots allocated to each database connection. SQLITE_CONFIG_LOOKASIDE sets the default lookaside size. The SQLITE_DBCONFIG_LOOKASIDE option to sqlite3_db_config() can be used to change the lookaside configuration on individual connections.</summary>
			LookAside = 13, // int int

			///<summary>Obsolete and should not be used by new code. They are retained for backwards compatibility but are now no-ops.</summary>
			PCache = 14, // no-op

			///<summary>Obsolete and should not be used by new code. They are retained for backwards compatibility but are now no-ops.</summary>
			GetPCache = 15, // no-op

			///<summary>The SQLITE_CONFIG_LOG option is used to configure the SQLite global error log. (The SQLITE_CONFIG_LOG option takes two arguments: a pointer to a function with a call signature of void(*)(void*,int,const char*), and a pointer to void. If the function pointer is not NULL, it is invoked by sqlite3_log() to process each logging event. If the function pointer is NULL, the sqlite3_log() interface becomes a no-op. The void pointer that is the second argument to SQLITE_CONFIG_LOG is passed through as the first parameter to the application-defined logger function whenever that function is invoked. The second parameter to the logger function is a copy of the first parameter to the corresponding sqlite3_log() call and is intended to be a result code or an extended result code. The third parameter passed to the logger is log message after formatting via sqlite3_snprintf(). The SQLite logging interface is not reentrant; the logger function supplied by the application must not invoke any SQLite interface. In a multi-threaded application, the application-defined logger function must be threadsafe.</summary>
			Log = 16, // xFunc, void*

			///<summary>The SQLITE_CONFIG_URI option takes a single argument of type int. If non-zero, then URI handling is globally enabled. If the parameter is zero, then URI handling is globally disabled. If URI handling is globally enabled, all filenames passed to sqlite3_open(), sqlite3_open_v2(), sqlite3_open16() or specified as part of ATTACH commands are interpreted as URIs, regardless of whether or not the SQLITE_OPEN_URI flag is set when the database connection is opened. If it is globally disabled, filenames are only interpreted as URIs if the SQLITE_OPEN_URI flag is set when the database connection is opened. By default, URI handling is globally disabled. The default value may be changed by compiling with the SQLITE_USE_URI symbol defined.</summary>
			Uri = 17, // int

			///<summary>The SQLITE_CONFIG_PCACHE2 option takes a single argument which is a pointer to an sqlite3_pcache_methods2 object. This object specifies the interface to a custom page cache implementation. SQLite makes a copy of the sqlite3_pcache_methods2 object.</summary>
			PCache2 = 18, // sqlite3_pcache_methods2*

			///<summary>The SQLITE_CONFIG_GETPCACHE2 option takes a single argument which is a pointer to an sqlite3_pcache_methods2 object. SQLite copies of the current page cache implementation into that object.</summary>
			GetPCache2 = 19, // sqlite3_pcache_methods2*

			///<summary>The SQLITE_CONFIG_COVERING_INDEX_SCAN option takes a single integer argument which is interpreted as a boolean in order to enable or disable the use of covering indices for full table scans in the query optimizer. The default setting is determined by the SQLITE_ALLOW_COVERING_INDEX_SCAN compile-time option, or is "on" if that compile-time option is omitted. The ability to disable the use of covering indices for full table scans is because some incorrectly coded legacy applications might malfunction when the optimization is enabled. Providing the ability to disable the optimization allows the older, buggy application code to work without change even with newer versions of SQLite.</summary>
			CoveringIndexScan = 20, // int

			///<summary>This option is only available if sqlite is compiled with the SQLITE_ENABLE_SQLLOG pre-processor macro defined. The first argument should be a pointer to a function of type void(*)(void*,sqlite3*,const char*, int). The second should be of type (void*). The callback is invoked by the library in three separate circumstances, identified by the value passed as the fourth parameter. If the fourth parameter is 0, then the database connection passed as the second argument has just been opened. The third argument points to a buffer containing the name of the main database file. If the fourth parameter is 1, then the SQL statement that the third parameter points to has just been executed. Or, if the fourth parameter is 2, then the connection being passed as the second parameter is being closed. The third parameter is passed NULL In this case. An example of using this configuration option can be seen in the "test_sqllog.c" source file in the canonical SQLite source tree.</summary>
			SqlLog = 21, // xSqllog, void*

			///<summary>SQLITE_CONFIG_MMAP_SIZE takes two 64-bit integer (sqlite3_int64) values that are the default mmap size limit (the default setting for PRAGMA mmap_size) and the maximum allowed mmap size limit. The default setting can be overridden by each database connection using either the PRAGMA mmap_size command, or by using the SQLITE_FCNTL_MMAP_SIZE file control. The maximum allowed mmap size will be silently truncated if necessary so that it does not exceed the compile-time maximum mmap size set by the SQLITE_MAX_MMAP_SIZE compile-time option. If either argument to this option is negative, then that argument is changed to its compile-time default.</summary>
			MmapSize = 22, // sqlite3_int64, sqlite3_int64

			///<summary>The SQLITE_CONFIG_WIN32_HEAPSIZE option is only available if SQLite is compiled for Windows with the SQLITE_WIN32_MALLOC pre-processor macro defined. SQLITE_CONFIG_WIN32_HEAPSIZE takes a 32-bit unsigned integer value that specifies the maximum size of the created heap.</summary>
			Win32HeapSize = 23, // int nByte

			///<summary>The SQLITE_CONFIG_PCACHE_HDRSZ option takes a single parameter which is a pointer to an integer and writes into that integer the number of extra bytes per page required for each page in SQLITE_CONFIG_PAGECACHE. The amount of extra space required can change depending on the compiler, target platform, and SQLite version.</summary>
			PCacheHdrSz = 24, // int *psz

			///<summary>The SQLITE_CONFIG_PMASZ option takes a single parameter which is an unsigned integer and sets the "Minimum PMA Size" for the multithreaded sorter to that integer. The default minimum PMA Size is set by the SQLITE_SORTER_PMASZ compile-time option. New threads are launched to help with sort operations when multithreaded sorting is enabled (using the PRAGMA threads command) and the amount of content to be sorted exceeds the page size times the minimum of the PRAGMA cache_size setting and this value.</summary>
			PMASz = 25, // unsigned int szPma

			///<summary>The SQLITE_CONFIG_STMTJRNL_SPILL option takes a single parameter which becomes the statement journal spill-to-disk threshold. Statement journals are held in memory until their size (in bytes) exceeds this threshold, at which point they are written to disk. Or if the threshold is -1, statement journals are always held exclusively in memory. Since many statement journals never become large, setting the spill threshold to a value such as 64KiB can greatly reduce the amount of I/O required to support statement rollback. The default value for this setting is controlled by the SQLITE_STMTJRNL_SPILL compile-time option.</summary>
			StmtJrnlSpill = 26, // int nByte

			///<summary>The SQLITE_CONFIG_SMALL_MALLOC option takes single argument of type int, interpreted as a boolean, which if true provides a hint to SQLite that it should avoid large memory allocations if possible. SQLite will run faster if it is free to make large memory allocations, but some application might prefer to run slower in exchange for guarantees about memory fragmentation that are possible if large allocations are avoided. This hint is normally off.</summary>
			SmallMalloc = 27, // boolean

			///<summary>The SQLITE_CONFIG_SORTERREF_SIZE option accepts a single parameter of type (int) - the new value of the sorter-reference size threshold. Usually, when SQLite uses an external sort to order records according to an ORDER BY clause, all fields required by the caller are present in the sorted records. However, if SQLite determines based on the declared type of a table column that its values are likely to be very large - larger than the configured sorter-reference size threshold - then a reference is stored in each sorted record and the required column values loaded from the database as records are returned in sorted order. The default value for this option is to never use this optimization. Specifying a negative value for this option restores the default behaviour. This option is only available if SQLite is compiled with the SQLITE_ENABLE_SORTER_REFERENCES compile-time option.</summary>
			SorterRefSize = 28, // int nByte

			///<summary>The SQLITE_CONFIG_MEMDB_MAXSIZE option accepts a single parameter sqlite3_int64 parameter which is the default maximum size for an in-memory database created using sqlite3_deserialize(). This default maximum size can be adjusted up or down for individual databases using the SQLITE_FCNTL_SIZE_LIMIT file-control. If this configuration setting is never used, then the default maximum is determined by the SQLITE_MEMDB_DEFAULT_MAXSIZE compile-time option. If that compile-time option is not set, then the default maximum is 1073741824.</summary>
			MemDBMaxSize = 29, // sqlite3_int64
		}

		/// <summary>Behaviour to perform when creating a table detects an already existing table with the same name</summary>
		public enum ECreateConstraint
		{
			/// <summary>The create operation will produce an error if the table already exists</summary>
			Default,

			/// <summary>The create operation will ignore the create command if a table with the same name already exists</summary>
			IfNotExists,
		}

		/// <summary>Behaviour to perform when an insert operation detects a constraint</summary>
		public enum EInsertConstraint
		{
			/// <summary>The insert operation will produce an error on constraint violation</summary>
			Default,

			/// <summary>
			/// Aborts the current SQL statement with an SQLITE_CONSTRAINT error and rolls back the current transaction. If no transaction is active (other than the
			/// implied transaction that is created on every command) then the ROLLBACK resolution algorithm works the same as the ABORT algorithm.
			/// </summary>
			Rollback,

			/// <summary>
			/// Aborts the current SQL statement with an SQLITE_CONSTRAINT error and backs out any changes made by the current SQL statement;
			/// but changes caused by prior SQL statements within the same transaction are preserved and the transaction remains active.
			/// This is the default behavior and the behavior specified by the SQL standard.
			/// </summary>
			Abort,

			/// <summary>
			/// Aborts the current SQL statement with an SQLITE_CONSTRAINT error. But the FAIL resolution does not back out prior changes of
			/// the SQL statement that failed nor does it end the transaction. For example, if an UPDATE statement encountered a constraint
			/// violation on the 100th row that it attempts to update, then the first 99 row changes are preserved but changes to rows 100
			/// and beyond never occur. The FAIL behavior only works for uniqueness, NOT NULL, and CHECK constraints. A foreign key constraint violation causes an ABORT.
			/// </summary>
			Fail,

			/// <summary>
			/// Skips the one row that contains the constraint violation and continues processing subsequent rows of the SQL statement as if
			/// nothing went wrong. Other rows before and after the row that contained the constraint violation are inserted or updated normally.
			/// No error is returned for uniqueness, NOT NULL, and UNIQUE constraint errors when the IGNORE conflict resolution algorithm is used.
			/// However, the IGNORE conflict resolution algorithm works like ABORT for foreign key constraint errors
			/// </summary>
			Ignore,

			/// <summary>
			/// When a UNIQUE or PRIMARY KEY constraint violation occurs, the REPLACE algorithm deletes pre-existing rows that are causing the constraint
			/// violation prior to inserting or updating the current row and the command continues executing normally. If a NOT NULL constraint violation occurs,
			/// the REPLACE conflict resolution replaces the NULL value with the default value for that column, or if the column has no default value, then the ABORT
			/// algorithm is used. If a CHECK constraint or foreign key constraint violation occurs, the REPLACE conflict resolution algorithm works like ABORT.
			/// When the REPLACE conflict resolution strategy deletes rows in order to satisfy a constraint, delete triggers fire if and only if recursive triggers are enabled.
			/// The update hook is not invoked for rows that are deleted by the REPLACE conflict resolution strategy. Nor does REPLACE increment the change counter.
			/// The exceptional behaviors defined in this paragraph might change in a future release.
			/// </summary>
			Replace,
		}

		#endregion
		#region Global Methods

		/// <summary>Convert from an extended error code to a basic error code</summary>
		public static EResult BasicCode(this EResult result)
		{
			return (EResult)((int)result & 0xFF);
		}

		/// <summary>
		/// Sets a global configuration option for sqlite.
		/// Must be called prior to initialisation or after shutdown of sqlite.
		/// Initialisation happens implicitly when Open is called.</summary>
		public static EResult Configure(EConfigOption option, params object[] args)
		{
			return NativeAPI.Config(option, args);
		}

		/// <summary>Convert an SQL data type to a basic type</summary>
		public static Type ToType(this EDataType dt)
		{
			return dt switch
			{
				EDataType.Text => typeof(string),
				EDataType.Integer => typeof(long),
				EDataType.Real => typeof(double),
				EDataType.Blob => typeof(byte[]),
				EDataType.Null => typeof(DBNull),
				_ => throw new Exception($"Unknown SQL data type: {dt}"),
			};
		}

		/// <summary>The Sqlite column data type used to store the given type</summary>
		public static EDataType SqlColumnType(Type type)
		{
			return BindMap.ColumnType(type);
		}

		/// <summary>Return a default instance for the sqlite column type (assuming it is not null)</summary>
		public static object DefaultValue(this EDataType dt)
		{
			return dt switch
			{
				EDataType.Text => string.Empty,
				EDataType.Integer => 0,
				EDataType.Real => 0.0,
				EDataType.Blob => new byte[0],
				EDataType.Null => null!,
				_ => throw new Exception($"Unknown SQL data type: {dt}"),
			};
		}

		/// <summary>Convert the constraint to sql text</summary>
		public static string ToSql(this ECreateConstraint constraint)
		{
			return constraint switch
			{
				ECreateConstraint.Default => string.Empty,
				ECreateConstraint.IfNotExists => "if not exists",
				_ => throw new Exception($"Unknown create table constraint: {constraint}"),
			};
		}
		public static string ToSql(this EInsertConstraint constraint)
		{
			return constraint switch
			{
				EInsertConstraint.Default => string.Empty,
				EInsertConstraint.Rollback => "or rollback",
				EInsertConstraint.Abort => "or abort",
				EInsertConstraint.Fail => "or fail",
				EInsertConstraint.Ignore => "or ignore",
				EInsertConstraint.Replace => "or replace",
				_ => throw new Exception($"Unknown insert constraint: {constraint}"),
			};
		}

		/// <summary>Return the number of parameters in this prepared statement</summary>
		public static int ParameterCount(this sqlite3_stmt stmt) => NativeAPI.BindParameterCount(stmt);

		/// <summary>Return the (0-based) index for the parameter named 'name' in this prepared statement</summary>
		public static int ParameterIndex(this sqlite3_stmt stmt, string name)
		{
			var idx = NativeAPI.BindParameterIndex(stmt, name); // 1-based
			if (idx == 0) throw new SqliteException(EResult.Error, $"No parameter named {name} was found.\nSql: {NativeAPI.SqlString(stmt)}", string.Empty);
			return idx - 1;
		}

		/// <summary>Return the name of a parameter by (0-based) index in this prepared statement</summary>
		public static string ParameterName(this sqlite3_stmt stmt, int idx)
		{
			return NativeAPI.BindParameterName(stmt, idx + 1) // 1-based
				?? throw new SqliteException(EResult.Error, $"No parameter at index {idx}.\nSql: {NativeAPI.SqlString(stmt)}", string.Empty);
		}

		#endregion
		#region TypeMap

		/// <summary>Read function for converting a row result into a 'type'</summary>
		public delegate object MapFunc(DataReader reader, Type ty);

		/// <summary>Methods for converting a row result to a type instance</summary>
		public static TypeMapData TypeMap { get; } = new TypeMapData();
		public class TypeMapData
		{
			// Notes:
			//  - Used to map from SQL result rows to types
			private readonly Dictionary<Type, MapFunc> m_map = new();

			/// <summary>Get/Set the constructor that takes a DataReader for 'Item'</summary>
			public MapFunc this[Type ty]
			{
				get
				{
					lock (m_map)
					{
						// Look for a handler function
						if (m_map.TryGetValue(ty, out var func))
							return func;

						// Note nullable types
						var is_nullable = Nullable.GetUnderlyingType(ty) != null;
						var ty0 = Nullable.GetUnderlyingType(ty) ?? ty;

						// Primitive type, expect one column
						if (ty0.IsPrimitive || ty0.IsEnum || ty0 == typeof(string))
							return m_map[ty] = PopulatePrimitive;
						if (ty0.IsArray)
							return m_map[ty] = PopulateArray;
						return m_map[ty] = PopulateObject;
					}

					// Populate functions
					static object PopulatePrimitive(DataReader reader, Type ty)
					{
						if (reader.ColumnCount != 1)
							throw new Exception($"Result for primitive type has more than one column");

						// This handles Nullable<ty> too
						return reader.Get(ty, 0);
					}
					static object PopulateArray(DataReader reader, Type ty)
					{
						var elem_ty = ty.GetElementType() ?? throw new Exception("Expected 'ty' to be an array type");
						var arr = Array.CreateInstance(elem_ty, reader.ColumnCount);
						for (int i = 0; i != arr.Length; ++i)
							arr.SetValue(TypeMap[elem_ty](reader, elem_ty), i);
						return arr;
					}
					static object PopulateObject(DataReader reader, Type ty)
					{
						var item = ty.New();
						foreach (var prop in ty.GetProperties(BindingFlags.Public | BindingFlags.Instance))
						{
							// Property is a column
							if (reader.FindColumnIndex(prop.Name) is not int idx)
								continue;

							// Read the column value
							var value = reader.Get(prop.PropertyType, idx);

							// If the property has a setter, use that.
							if (prop.CanWrite && prop.GetSetMethod(true) is MethodInfo setter)
							{
								setter.Invoke(item, new[] { value });
							}
							// Try to set the value of the backing field so that 'get only' properties are also updated
							else if (prop.BackingField() is FieldInfo backing_field)
							{
								backing_field.SetValue(item, value);
							}
						}
						foreach (var field in ty.GetFields(BindingFlags.Public | BindingFlags.Instance))
						{
							// Field is a column
							if (reader.FindColumnIndex(field.Name) is not int idx)
								continue;

							// Read the column value
							var value = reader.Get(field.FieldType, idx);

							// Set the field
							field.SetValue(item, value);
						}
						return item;
					}
				}
				set
				{
					lock (m_map)
					{
						m_map[ty] = value;
					}
				}
			}
		}

		#endregion
		#region BindMap

		/// <summary>Reading function that reads 0-based column index 'cidx' into a value</summary>
		public delegate object ReadFunc(sqlite3_stmt stmt, int cidx); // "column" idx is 0-based
		
		/// <summary>Binding function that binds 'obj' to 1-based parameter index 'pidx'</summary>
		public delegate void BindFunc(sqlite3_stmt stmt, int pidx, object obj); // "parameter" idx is 1-based

		/// <summary>The map from type to binding function.</summary>
		public static BindMapData BindMap { get; } = new BindMapData();
		public class BindMapData
		{
			private readonly Dictionary<Type, Mapping> m_map = new();
			private struct Mapping
			{
				public EDataType ColumnType;
				public BindFunc Bind;
				public ReadFunc Read;
			}

			public BindMapData()
			{
				Register(typeof(DBNull), EDataType.Null,
					(stmt, i, obj) =>
					{
						NativeAPI.BindNull(stmt, i);
					},
					(stmt, i) =>
					{
						return null!;
					});
				Register(typeof(object), EDataType.Null,
					(stmt, i, obj) =>
					{
						if (obj == null || obj.GetType() == typeof(object))
							NativeAPI.BindNull(stmt, i);
						else
							Bind(obj.GetType())(stmt, i, obj);
					},
					(stmt, i) =>
					{
						return NativeAPI.ColumnType(stmt, i) switch
						{
							EDataType.Null => null!,
							EDataType.Text => NativeAPI.ColumnText(stmt, i),
							EDataType.Integer => NativeAPI.ColumnInt64(stmt, i),
							EDataType.Real => NativeAPI.ColumnDouble(stmt, i),
							EDataType.Blob => NativeAPI.ColumnBlob(stmt, i),
							_ => throw new Exception($"Unknown SQL data type"),
						};
					});
				Register(typeof(string), EDataType.Text,
					(stmt, i, obj) =>
					{
						NativeAPI.BindText(stmt, i, (string)obj);
					},
					(stmt, i) =>
					{
						return NativeAPI.ColumnText(stmt, i);
					});
				Register(typeof(bool), EDataType.Integer,
					(stmt, i, obj) =>
					{
						NativeAPI.BindInt32(stmt, i, (bool)obj ? 1 : 0);
					},
					(stmt, i) =>
					{
						return NativeAPI.ColumnInt32(stmt, i) != 0;
					});
				Register(typeof(byte), EDataType.Integer,
					(stmt, i, obj) =>
					{
						NativeAPI.BindInt32(stmt, i, (byte)obj);
					},
					(stmt, i) =>
					{
						return (byte)NativeAPI.ColumnInt32(stmt, i);
					});
				Register(typeof(sbyte), EDataType.Integer,
					(stmt, i, obj) =>
					{
						NativeAPI.BindInt32(stmt, i, (sbyte)obj);
					},
					(stmt, i) =>
					{
						return (sbyte)NativeAPI.ColumnInt32(stmt, i);
					});
				Register(typeof(char), EDataType.Integer,
					(stmt, i, obj) =>
					{
						NativeAPI.BindInt32(stmt, i, (char)obj);
					},
					(stmt, i) =>
					{
						return (char)NativeAPI.ColumnInt32(stmt, i);
					});
				Register(typeof(short), EDataType.Integer,
					(stmt, i, obj) =>
					{
						NativeAPI.BindInt32(stmt, i, (short)obj);
					},
					(stmt, i) =>
					{
						return (short)NativeAPI.ColumnInt32(stmt, i);
					});
				Register(typeof(ushort), EDataType.Integer,
					(stmt, i, obj) =>
					{
						NativeAPI.BindInt32(stmt, i, (ushort)obj);
					},
					(stmt, i) =>
					{
						return (ushort)NativeAPI.ColumnInt32(stmt, i);
					});
				Register(typeof(int), EDataType.Integer,
					(stmt, i, obj) =>
					{
						NativeAPI.BindInt32(stmt, i, (int)obj);
					},
					(stmt, i) =>
					{
						return NativeAPI.ColumnInt32(stmt, i);
					});
				Register(typeof(uint), EDataType.Integer,
					(stmt, i, obj) =>
					{
						NativeAPI.BindInt32(stmt, i, unchecked((int)(uint)obj));
					},
					(stmt, i) =>
					{
						return unchecked((uint)NativeAPI.ColumnInt32(stmt, i));
					});
				Register(typeof(long), EDataType.Integer,
					(stmt, i, obj) =>
					{
						NativeAPI.BindInt64(stmt, i, (long)obj);
					},
					(stmt, i) =>
					{
						return NativeAPI.ColumnInt64(stmt, i);
					});
				Register(typeof(ulong), EDataType.Integer,
					(stmt, i, obj) =>
					{
						NativeAPI.BindInt64(stmt, i, unchecked((long)(ulong)obj));
					},
					(stmt, i) =>
					{
						return unchecked((ulong)NativeAPI.ColumnInt64(stmt, i));
					});
				Register(typeof(float), EDataType.Real,
					(stmt, i, obj) =>
					{
						NativeAPI.BindDouble(stmt, i, (float)obj);
					},
					(stmt, i) =>
					{
						return (float)NativeAPI.ColumnDouble(stmt, i);
					});
				Register(typeof(double), EDataType.Real,
					(stmt, i, obj) =>
					{
						NativeAPI.BindDouble(stmt, i, (double)obj);
					},
					(stmt, i) =>
					{
						return NativeAPI.ColumnDouble(stmt, i);
					});
				Register(typeof(decimal), EDataType.Text,
					(stmt, i, obj) =>
					{
						NativeAPI.BindText(stmt, i, ((decimal)obj).ToString(CultureInfo.InvariantCulture));
					},
					(stmt, i) =>
					{
						var str = NativeAPI.ColumnText(stmt, i);
						if (str == null || str.Length == 0) return null!;
						return decimal.Parse(str, NumberStyles.Number);
					});
				Register(typeof(byte[]), EDataType.Blob,
					(stmt, i, obj) =>
					{
						var barr = (byte[])obj;
						NativeAPI.BindBlob(stmt, i, barr, barr.Length);
					},
					(stmt, i) =>
					{
						return NativeAPI.ColumnBlob(stmt, i);
					});
				Register(typeof(char[]), EDataType.Blob,
					(stmt, i, obj) =>
					{
						var carr = (char[])obj;
						var barr = new byte[Buffer.ByteLength(carr)];
						Buffer.BlockCopy(carr, 0, barr, 0, barr.Length);
						NativeAPI.BindBlob(stmt, i, barr, barr.Length);
					},
					(stmt, i) =>
					{
						NativeAPI.ColumnBlob(stmt, i, out var ptr, out var len);
						var chars = new char[len / Marshal.SizeOf<char>()];
						Marshal.Copy(ptr, chars, 0, chars.Length);
						return chars;
					});
				Register(typeof(int[]), EDataType.Blob,
					(stmt, i, obj) =>
					{
						var iarr = (int[])obj;
						var barr = new byte[Buffer.ByteLength(iarr)];
						Buffer.BlockCopy(iarr, 0, barr, 0, barr.Length);
						NativeAPI.BindBlob(stmt, i, barr, barr.Length);
					},
					(stmt, i) =>
					{
						NativeAPI.ColumnBlob(stmt, i, out var ptr, out var len);
						var ints = new int[len / Marshal.SizeOf<int>()];
						Marshal.Copy(ptr, ints, 0, ints.Length);
						return ints;
					});
				Register(typeof(long[]), EDataType.Blob,
					(stmt, i, obj) =>
					{
						var iarr = (long[])obj;
						var barr = new byte[Buffer.ByteLength(iarr)];
						Buffer.BlockCopy(iarr, 0, barr, 0, barr.Length);
						NativeAPI.BindBlob(stmt, i, barr, barr.Length);
					},
					(stmt, i) =>
					{
						NativeAPI.ColumnBlob(stmt, i, out var ptr, out var len);
						var longs = new long[len / Marshal.SizeOf<long>()];
						Marshal.Copy(ptr, longs, 0, longs.Length);
						return longs;
					});
				Register(typeof(Guid), EDataType.Text,
					(stmt, i, obj) =>
					{
						var guid = (Guid)obj;
						NativeAPI.BindText(stmt, i, guid.ToString());
					},
					(stmt, i) =>
					{
						return Guid.Parse(NativeAPI.ColumnText(stmt, i));
					});
				Register(typeof(DateTime), EDataType.Integer,
					(stmt, i, obj) =>
					{
						var dt = (DateTime)obj;
						NativeAPI.BindInt64(stmt, i, dt.Ticks);
					},
					(stmt, i) =>
					{
						return NativeAPI.ColumnType(stmt, i) switch
						{
							EDataType.Text => DateTime.Parse(NativeAPI.ColumnText(stmt, i)),
							EDataType.Integer => new DateTime(NativeAPI.ColumnInt64(stmt, i), DateTimeKind.Unspecified),
							EDataType.Null => throw new NullReferenceException($"Can't convert 'Null' to DateTime"),
							_ => throw new Exception($"Column data type can't be converted to DateTime"),
						};
					});
				Register(typeof(DateTimeOffset), EDataType.Integer,
					(stmt, i, obj) =>
					{
						var dto = (DateTimeOffset)obj;
						if (dto.Offset != TimeSpan.Zero)
							throw new Exception("Only UTC DateTimeOffset values can be stored");
						NativeAPI.BindInt64(stmt, i, dto.Ticks);
					},
					(stmt, i) =>
					{
						return NativeAPI.ColumnType(stmt, i) switch
						{
							EDataType.Text => DateTimeOffset.Parse(NativeAPI.ColumnText(stmt, i)),
							EDataType.Integer => new DateTimeOffset(NativeAPI.ColumnInt64(stmt, i), TimeSpan.Zero),
							EDataType.Null => throw new NullReferenceException($"Can't convert 'Null' to DateTimeOffset"),
							_ => throw new Exception($"Column data type can't be converted to DateTimeOffset"),
						};
					});
				Register(typeof(TimeSpan), EDataType.Integer,
					(stmt, i, obj) =>
					{
						var ts = (TimeSpan)obj;
						NativeAPI.BindInt64(stmt, i, ts.Ticks);
					},
					(stmt, i) =>
					{
						return NativeAPI.ColumnType(stmt, i) switch
						{
							EDataType.Text => TimeSpan.Parse(NativeAPI.ColumnText(stmt, i)),
							EDataType.Integer => new TimeSpan(NativeAPI.ColumnInt64(stmt, i)),
							EDataType.Null => throw new NullReferenceException($"Can't convert 'Null' to TimeSpan"),
							_ => throw new Exception($"Column data type can't be converted to TimeSpan"),
						};
					});
				Register(typeof(Colour32), EDataType.Integer,
					(stmt, i, obj) =>
					{
						var col = (Colour32)obj;
						NativeAPI.BindInt32(stmt, i, unchecked((int)col.ARGB));
					},
					(stmt, i) =>
					{
						var argb = unchecked((uint)NativeAPI.ColumnInt32(stmt, i));
						return new Colour32(argb);
					});
			}

			/// <summary>Register a binding function and the column type needed to store it</summary>
			public BindMapData Register(Type ty, EDataType column_type, BindFunc bind, ReadFunc read)
			{
				lock (m_map)
				{
					m_map.Add(BaseType(ty), new Mapping
					{
						ColumnType = column_type,
						Bind = bind,
						Read = read,
					});
				}
				return this;// Fluent
			}

			/// <summary>Return the column type needed to store 'type' instances</summary>
			public EDataType ColumnType(Type ty)
			{
				lock (m_map)
				{
					ty = BaseType(ty);
					if (!m_map.TryGetValue(ty, out var mapping))
						throw new KeyNotFoundException(
							$"Type '{ty.Name}' does not have a column type for binding to a DB column. " +
							$"Callers should register custom bindings in this map.");
					
					return mapping.ColumnType;
				}
			}

			/// <summary>Get the function used to bind 'type' to a column</summary>
			public BindFunc Bind(Type ty)
			{
				lock (m_map)
				{
					ty = BaseType(ty);
					if (!m_map.TryGetValue(ty, out var mapping))
						throw new KeyNotFoundException(
							$"Type '{ty.Name}' does not have a bind/read method for binding to a DB column. " +
							$"Callers should register custom bindings in this map. " +
							$"Parameters may also be bound with custom methods on the parameter directly.");
				
					return mapping.Bind;
				}
			}

			/// <summary>Get the function used to read 'type' from a column</summary>
			public ReadFunc Read(Type ty)
			{
				lock (m_map)
				{
					var is_nullable = ty.IsClass || Nullable.GetUnderlyingType(ty) != null;
					var ty0 = Nullable.GetUnderlyingType(ty) ?? ty;
					var ty1 = ty0.IsEnum ? Enum.GetUnderlyingType(ty0) : ty0;
					if (!m_map.TryGetValue(ty1, out var mapping))
						throw new KeyNotFoundException(
							$"Type '{ty1.Name}' does not have a bind/read method for binding to a DB column. " +
							$"Callers should register custom bindings in this map.");

					// Return a read function that wraps 'mapping.Read' and handles nullables and enums
					object ReadFn(sqlite3_stmt stmt, int idx)
					{
						// Return if null
						if (NativeAPI.ColumnType(stmt, idx) == EDataType.Null)
							return is_nullable ? null! : throw new SqliteException(EResult.Error, $"Column contains null for non-nullable type {ty.Name}", string.Empty);

						// Read the value (as the underlying type)
						var obj = mapping.Read(stmt, idx);

						// Convert to enum if necessary
						obj = ty0.IsEnum ? Enum.ToObject(ty0, obj) : obj;
						return obj;
					};

					return ReadFn;
				}
			}

			/// <summary>Strip Nullable and Enum from a type</summary>
			private Type BaseType(Type ty)
			{
				ty = Nullable.GetUnderlyingType(ty) ?? ty;
				ty = ty.IsEnum ? Enum.GetUnderlyingType(ty) : ty;
				return ty;
			}
		}

		#endregion
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using System.IO;
	using Common;
	using Db;

	[TestFixture]
	public class TestSqlite3
	{
		// Use an in-memory DB for normal unit tests, use an actual file when debugging
		private static string DBFilepath = Path.Combine(UnitTest.ResourcePath, "test.db");
		private static string ConnectionString =
			$"Data Source=file:SharedMem.db?mode=memory&cache=shared;Version=3;Journal Mode=Memory;ThreadSafe=OFF;Synchronous=OFF;Foreign Keys=ON";
			//$"Data Source={DBFilepath};Version=3;Journal Mode=Memory;ThreadSafe=OFF;Synchronous=OFF;Foreign Keys=ON";
			//$"Data Source={Sqlite3.DBInMemory};Version=3;Journal Mode=Memory;ThreadSafe=OFF;Synchronous=OFF;Foreign Keys=ON";

		#region Custom types
		public enum SomeEnum
		{
			One = 1,
			Two = 2,
			Three = 3,
		}
		private class Custom
		{
			public string Name { get; set; } = string.Empty;
			public double Value { get; set; }

			public bool Equals(Custom rhs)
			{
				return Name == rhs.Name && Value == rhs.Value;
			}
			public override bool Equals(object? obj)
			{
				return obj is Custom rhs && Equals(rhs);
			}
			public override int GetHashCode()
			{
				return new { Name, Value }.GetHashCode();
			}
			public static void SqlBind(Sqlite.sqlite3_stmt stmt, int i, object obj)
			{
				var custom = (Custom)obj;
				Sqlite.NativeAPI.BindText(stmt, i, $"{custom.Name}={custom.Value}");
			}
			public static Custom SqlRead(Sqlite.sqlite3_stmt stmt, int i)
			{
				var pair = Sqlite.NativeAPI.ColumnText(stmt, i).Split('=');
				return new Custom { Name = pair[0], Value = double.Parse(pair[1]) };
			}
		}
		private class Record0
		{
			public Record0()
			{
				StrValue = string.Empty;
				EnumValue = SomeEnum.One;
			}
			public Record0(int seed, long? id = null)
				:this()
			{
				ID = id;
				StrValue = seed.ToString();
				EnumValue = (SomeEnum)((seed % 3) + 1);
			}
			public long? ID { get; set; }
			public string StrValue { get; set; }
			public SomeEnum EnumValue { get; set; }
			public override string ToString() => $"{ID} {StrValue} {EnumValue}";
		}
		private class Record1
		{
			public Record1()
				: this(0)
			{ }
			public Record1(long val)
			{
				ID = null;
				Bool = (val & 1) != 0;
				Char = (char)(('A' + val) % 26);
				SByte = (sbyte)val;
				Byte = (byte)val;
				Short = (short)((val << 8) | val);
				UShort = (ushort)((val << 8) | val);
				Int32 = (int)((val << 24) | (val << 16) | (val << 8) | (val));
				UInt32 = (uint)((val << 24) | (val << 16) | (val << 8) | (val));
				Int64 = (long)((val << 56) | (val << 48) | (val << 40) | (val << 32) | (val << 24) | (val << 16) | (val << 8) | (val));
				UInt64 = (ulong)((val << 56) | (val << 48) | (val << 40) | (val << 32) | (val << 24) | (val << 16) | (val << 8) | (val)); ;
				Decimal = (decimal)(val * 1234567890.123467890m);
				Float = (float)(val * 1.234567f);
				Double = (double)(val * 1.2345678987654321);
				String = $"string: {val}";
				Buf = Util.ToBytes(val);
				EmptyBuf = null;
				IntBuf = new[] { (int)val, (int)val, (int)val, (int)val };
				Guid = Guid.NewGuid();
				Enum = (SomeEnum)(val % 3);
				NullEnum = SomeEnum.Two;
				DTO = DateTimeOffset.UtcNow;
				Custom = new Custom { Name=$"Bob{val}", Value=1.234 };
				NullInt32 = (int)(val * 13);
				NullInt64 = null;
			}

			// Fields
			public long? ID;
			public bool Bool;
			public sbyte SByte;
			public byte Byte;
			public char Char;
			public short Short { get; set; }
			public ushort UShort { get; set; }
			public int Int32;
			public uint UInt32;
			public long Int64;
			public ulong UInt64;
			public decimal Decimal;
			public float Float;
			public double Double;
			public string? String;
			public byte[]? Buf;
			public byte[]? EmptyBuf;
			public int[]? IntBuf;
			public Guid Guid;
			public SomeEnum Enum;
			public SomeEnum? NullEnum;
			public DateTimeOffset DTO;
			public Custom Custom;
			public int? NullInt32;
			public long? NullInt64;

			public bool Equals(Record1 other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				if (other.Bool != Bool) return false;
				if (other.SByte != SByte) return false;
				if (other.Byte != Byte) return false;
				if (other.Char != Char) return false;
				if (other.Short != Short) return false;
				if (other.UShort != UShort) return false;
				if (other.Int32 != Int32) return false;
				if (other.UInt32 != UInt32) return false;
				if (other.Int64 != Int64) return false;
				if (other.UInt64 != UInt64) return false;
				if (other.Decimal != Decimal) return false;
				if (other.NullInt32 != NullInt32) return false;
				if (other.NullInt64 != NullInt64) return false;
				if (!Equals(other.String, String)) return false;
				if (!Equals(other.Buf, Buf)) return false;
				if (!Equals(other.EmptyBuf, EmptyBuf)) return false;
				if (!Equals(other.IntBuf, IntBuf)) return false;
				if (!Equals(other.Enum, Enum)) return false;
				if (!Equals(other.NullEnum, NullEnum)) return false;
				if (!other.Guid.Equals(Guid)) return false;
				if (!other.DTO.Equals(DTO)) return false;
				if (!other.Custom.Equals(Custom)) return false;
				if (Math.Abs(other.Float - Float) > float.Epsilon) return false;
				if (Math.Abs(other.Double - Double) > double.Epsilon) return false;
				return true;
			}
			public override bool Equals(object? obj)
			{
				return obj is Record1 rhs && Equals(rhs);
			}
			public override int GetHashCode()
			{
				return base.GetHashCode();
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

		[TestFixtureSetUp]
		public void Setup()
		{
			Sqlite.LoadDll($"{UnitTest.LibPath}\\$(platform)\\$(config)", threading: Sqlite.EConfigOption.MultiThread);
		}
		[TestFixtureTearDown]
		public void Cleanup()
		{
			if (Path_.FileExists(DBFilepath))
				File.Delete(DBFilepath);
		}
		[Test]
		public void StandardUse()
		{
			// Create/Open the database connection
			using var db = new Sqlite.Connection(ConnectionString, "StandardUse");

			// Create a simple table
			db.Cmd(
				$"drop table if exists {nameof(Record0)};\n" +
				$"create table if not exists {nameof(Record0)} (\n" +
				$"  [{nameof(Record0.ID)}] integer unique primary key autoincrement,\n" +
				$"  [{nameof(Record0.StrValue)}] text,\n" +
				$"  [{nameof(Record0.EnumValue)}] integer not null\n" +
				$")").Execute();
			Assert.True(db.Table<Record0>().Exists);

			// Create some objects to stick in the table
			var obj1 = new Record0(5);
			var obj2 = new Record0(6);
			var obj3 = new Record0(7);

			// Insert stuff
			{
				using var cmd = db.Cmd(
					$"insert into {nameof(Record0)} (\n" +
					$"  [{nameof(Record0.StrValue)}],\n" +
					$"  [{nameof(Record0.EnumValue)}]" +
					$") values (\n" +
					$"  @str, @num\n" +
					$")");

				Assert.Equal(1, cmd
					.Clear()
					.AddParam("str", obj1.StrValue)
					.AddParam("num", obj1.EnumValue)
					.Execute());
				Assert.Equal(1, cmd
					.Clear()
					.AddParam("str", obj2.StrValue)
					.AddParam("num", obj2.EnumValue)
					.Execute());
				Assert.Equal(1, cmd
					.Clear()
					.AddParam("str", obj3.StrValue)
					.AddParam("num", obj3.EnumValue)
					.Execute());
			}

			// Check it was inserted
			{
				using var cmd = db.Cmd($"select count(*) from {nameof(Record0)}");
				var count = cmd.Scalar<int>();
				Assert.Equal(3, count);
			}

			// Multi-statement commands
			{
				var sql =
					$"insert into {nameof(Record0)} ([{nameof(Record0.StrValue)}], [{nameof(Record0.EnumValue)}]) values (@str0, @num0);\n" +
					$"insert into {nameof(Record0)} ([{nameof(Record0.StrValue)}], [{nameof(Record0.EnumValue)}]) values (@str0, @num1);\n" +
					$"insert into {nameof(Record0)} ([{nameof(Record0.StrValue)}], [{nameof(Record0.EnumValue)}]) values (@str1, @num1);\n";
				var rows_changed = db.Cmd(sql)
					.AddParam("str0", "String0")
					.AddParam("str1", "String1")
					.AddParam("num0", SomeEnum.One)
					.AddParam("num1", SomeEnum.Two)
					.Execute();
				Assert.Equal(3, rows_changed);
			}
		}
		[Test]
		public void AllTypes()
		{
			// Create/Open the database connection
			using var db = new Sqlite.Connection(ConnectionString, "AllTypes");
			Sqlite.BindMap.Register(typeof(Custom), Sqlite.EDataType.Text, Custom.SqlBind, Custom.SqlRead);

			// Define a table based on 'Record1'
			var table = new Sqlite.Table<Record1>(db);

			// Create the table
			table.Drop();
			table.Create();
			Assert.True(table.Exists);

			// Create some objects to stick in the table
			var obj1 = new Record1(5);
			var obj2 = new Record1(6) { DTO = DateTimeOffset.UtcNow };
			var obj3 = new Record1(7);

			// Check the columns (no data yet)
			{
				using var q = table.Reader();
				Assert.Equal(25, q.ColumnCount);
				for (int i = 0; i != q.ColumnCount; ++i)
				{
					var col = q.ColumnName(i);
					var idx = table.Columns.IndexOf(x => x.Name == col);
					Assert.True(idx != -1);
				}
			}

			// Insert stuff
			{
				Assert.Equal(1, table.Insert(obj1));
				Assert.Equal(1, table.Insert(obj2));
				Assert.Equal(1, table.Insert(obj3));
				Assert.Equal(3, table.RowCount);
			}
			// Get stuff and check it's the same
			{
				using var reader = table.Reader();
				Assert.True(reader.Read()); var OBJ1 = reader.ToObject<Record1>();
				Assert.True(reader.Read()); var OBJ2 = reader.ToObject<Record1>();
				Assert.True(reader.Read()); var OBJ3 = reader.ToObject<Record1>();
				Assert.Equal(obj1, OBJ1);
				Assert.Equal(obj2, OBJ2);
				Assert.Equal(obj3, OBJ3);
			}
			// Check parameter binding
			{
				using var q = db.Cmd(
					$"select [{nameof(Record1.Bool)}], [{nameof(Record1.String)}] from {nameof(Record1)} " +
					$"where [{nameof(Record1.Bool)}] = @p1 and [{nameof(Record1.String)}] like @p2");

				var stmt = q.Stmt ?? throw new Exception("Stmt expected");
				Assert.Equal(2, stmt.ParameterCount());
				Assert.Equal("@p1", stmt.ParameterName(0));
				Assert.Equal("@p2", stmt.ParameterName(1));
				Assert.Equal(0, stmt.ParameterIndex("@p1"));
				Assert.Equal(1, stmt.ParameterIndex("@p2"));
				q.AddParam("p1", 1);
				q.AddParam("p2", "string:%");

				// Get the first row of the result
				var reader = q.ExecuteQuery();

				// Read the 1st result
				Assert.True(reader.Read());
				Assert.Equal(2, reader.ColumnCount);
				Assert.Equal(Sqlite.EDataType.Integer, reader.ColumnType(0));
				Assert.Equal(Sqlite.EDataType.Text, reader.ColumnType(1));
				Assert.Equal(nameof(Record1.Bool), reader.ColumnName(0));
				Assert.Equal(nameof(Record1.String), reader.ColumnName(1));
				Assert.True(reader.Get<bool>(0));
				Assert.True(reader.Get<string>(1).StartsWith("string:"));

				// There should be 2 results in total
				Assert.True(reader.Read());
				Assert.False(reader.Read());
			}
			// Update stuff
			{
				const string nue_string = "I've been modified";

				var rows_changed = db.Cmd($"update {table.Name} set [{nameof(Record1.String)}] = @str")
					.AddParam("str", nue_string)
					.Execute();
				Assert.Equal(3, rows_changed);

				// Get the updated stuff and check it's been updated
				var nue = db.Cmd($"select [{nameof(Record1.String)}] from {table.Name} limit 1").Scalar<string>();
				Assert.True(nue == nue_string);
			}
			// Delete something and check it's gone
			{
				var rows_changed = db.Cmd($"delete from {table.Name} where [{nameof(Record1.Bool)}] = @value").AddParam("value", false).Execute();
				Assert.Equal(1, rows_changed);

				// Check it's gone
				using var reader = db.Cmd($"select * from {table.Name} where [{nameof(Record1.Bool)}] = @value").AddParam("value", 0).ExecuteQuery();
				Assert.False(reader.Read());
			}
			// Add some more stuff
			{
				var obj4 = new
				{
					String = "我觉得你是笨蛋",
					UShort = 666,
					UInt32 = 66666666,
					Bool = true,
				};
				Assert.Equal(1, table.Insert(obj4));
			}
			// LINQ expressions
			{
				var objs = (from a in table where a.String == "I've been modified" select a).ToArray();
				Assert.Equal(2, objs.Length);
			}
		}
		[Test]
		public void Transactions()
		{
			// Create/Open the database connection
			using var db = new Sqlite.Connection(ConnectionString, "Transactions");

			// Create a table
			var table = db.Table<Record0>();
			table.Drop();
			table.Create();
			Assert.True(table.Exists);

			// Create and insert some objects
			var objs = Enumerable.Range(0, 10).Select(i => new Record0(i, i+1)).ToList();
			foreach (var x in objs.Take(3))
			{
				table.Insert(x);
			}
			Assert.Equal(3, table.RowCount);

			// Add objects within a transaction that is not committed
			using (var tranny = db.BeginTransaction())
			{
				foreach (var x in objs.Take(5))
					table.Insert(x, Sqlite.EInsertConstraint.Replace);

				// No commit
			}
			Assert.Equal(3, table.RowCount);

			// Add object with commit
			using (var tranny = db.BeginTransaction())
			{
				foreach (var x in objs.Take(5))
					table.Insert(x, Sqlite.EInsertConstraint.Replace);

				tranny.Commit();
			}
			Assert.Equal(5, table.RowCount);

			// Add objects from a worker thread
			if (db.Database != Sqlite.DBInMemory) // Non-shared memory DBs can't use multiple threads
			{
				using (var mre = new ManualResetEvent(false))
				{
					ThreadPool.QueueUserWorkItem(_ =>
					{
						using var conn = new Sqlite.Connection(ConnectionString, "Transactions-Worker");
						using (var tranny = conn.BeginTransaction())
						{
							var table_ = conn.Table<Record0>();
							foreach (var x in objs.Take(7))
								table_.Insert(x, Sqlite.EInsertConstraint.Replace);

							tranny.Commit();
						}
						mre.Set();
					});
					mre.WaitOne();
					Assert.Equal(7, table.RowCount);
				}
			}
		}
		[Test]
		public void Multithreading()
		{
			// Create/Open the database connection
			using var db = new Sqlite.Connection(ConnectionString, "Multithreading");

			// Create a simple table
			db.Cmd(
				$"drop table if exists {nameof(Record0)};\n" +
				$"create table if not exists {nameof(Record0)} (\n" +
				$"  [{nameof(Record0.ID)}] integer unique primary key autoincrement,\n" +
				$"  [{nameof(Record0.StrValue)}] text,\n" +
				$"  [{nameof(Record0.EnumValue)}] integer not null\n" +
				$")").Execute();
			Assert.True(db.Table<Record0>().Exists);

			const int BatchCount = 100;
			const int BatchSize = 100;

			var semi = new Semaphore(0, 3);

			// Multiple threads stuffing things in the db
			ThreadPool.QueueUserWorkItem(AddStuff);
			ThreadPool.QueueUserWorkItem(AddStuff);
			ThreadPool.QueueUserWorkItem(AddStuff);
			void AddStuff(object? _) // worker thread context
			{
				using var db = new Sqlite.Connection(ConnectionString, "Multithreading-Worker");
				var tbl = db.Table<Record0>();
				for (int batch = 0; batch != BatchCount; ++batch)
				{
					using (var tx = db.BeginTransaction())
					{
						for (int i = 0; i != BatchSize; ++i, Thread.Yield())
						{
							var obj = new Record0(i);
							tbl.Insert(obj);
						}
						tx.Commit();
					}
				}
				semi.Release();
			}

			semi.WaitOne();
			semi.WaitOne();
			semi.WaitOne();
			Assert.Equal(3 * BatchCount * BatchSize, db.Table<Record0>().RowCount);
		}
	}
}
#endif

#if false
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

#endif
#if false
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
		public partial class Table3 : Table3Base
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
		public class Table5 : Table5Base
		{
			public Table5() : this(string.Empty) { }
			public Table5(string data) { Data = data; }
			public string Data { get; set; }
			[Sqlite.Ignore] public int Tmp { get; set; }
		}
#endregion
#endif
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
#if false
		// Notes:
		//  - This is a work in progress. The plan is to create helpers for reflecting types
		//    so they can be mapped to an Sql DB.
		//  - I'm basically porting the parts of sqlite3.cs that are most useful and don't have
		//    a dependency on the native dll interface.

		/// <summary>Convert this type to is equivalent DbType</summary>
		public static DbType DbType(this Type type)
		{
			type = Nullable.GetUnderlyingType(type) ?? type;
			type = type.IsEnum ? type.GetEnumUnderlyingType() : type;

			return type.Name switch
			{
				"String" => System.Data.DbType.String,
				"Byte[]" => System.Data.DbType.Binary,
				"Boolean" => System.Data.DbType.Boolean,
				"Byte" => System.Data.DbType.Byte,
				"SByte" => System.Data.DbType.SByte,
				"Int16" => System.Data.DbType.Int16,
				"Int32" => System.Data.DbType.Int32,
				"Int64" => System.Data.DbType.Int64,
				"UInt16" => System.Data.DbType.UInt16,
				"UInt32" => System.Data.DbType.UInt32,
				"UInt64" => System.Data.DbType.UInt64,
				"Single" => System.Data.DbType.Single,
				"Double" => System.Data.DbType.Double,
				"Decimal" => System.Data.DbType.Decimal,
				"Guid" => System.Data.DbType.Guid,
				_ => throw new Exception($"Unknown conversion from {type.Name} to DbType"),
			};
		}

		/// <summary>
		/// Controls the mapping from .NET types to database tables.
		/// By default, all public properties (including inherited properties) are used as columns,
		/// this can be changed using the PropertyBindingFlags/FieldBindingFlags properties.</summary>
		[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
		public sealed class TableAttribute : Attribute
		{
			public TableAttribute()
			{
				AllByDefault = true;
				PropertyBindingFlags = BindingFlags.Public;
				FieldBindingFlags = BindingFlags.Default;
				Constraints = null;
				PrimaryKey = null;
				PKAutoInc = false;
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

		/// <summary>Marks a property or field as a column in a table</summary>
		[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
		public sealed class ColumnAttribute : Attribute
		{
			public ColumnAttribute()
			{
				PrimaryKey = false;
				AutoInc = false;
				Name = string.Empty;
				Order = 0;
				SqlDataType = null;
				Constraints = null;
			}

			/// <summary>
			/// True if this column should be used as a primary key. If multiple primary keys are specified, ensure the Order
			/// property is used so that the order of primary keys is defined. Default value is false.</summary>
			public bool PrimaryKey { get; set; }

			/// <summary>True if this column should auto increment. Default is false</summary>
			public bool AutoInc { get; set; }

			/// <summary>The column name to use. If null or empty, the member name is used. Default is null</summary>
			public string? Name { get; set; }

			/// <summary>Defines the relative order of columns in the table. Default is '0'</summary>
			public int Order { get; set; }

			/// <summary>
			/// The sqlite data type used to represent this column.
			/// If set to null, then the default mapping from .NET data type to sqlite type is used. Default is null</summary>
			public SqlDbType? SqlDataType { get; set; }

			/// <summary>Custom constraints to add to this column. Default is null</summary>
			public string? Constraints { get; set; }
		}

		/// <summary>Marks a property or field as not a column in the db table for a type.</summary>
		[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
		public sealed class IgnoreAttribute : Attribute
		{ }

		/// <summary>Mapping information from a type to columns in the table</summary>
		public sealed class TableMetaData
		{
			/// <summary>Get the meta data for a table based on 'type'</summary>
			public static TableMetaData Get<T>()
			{
				return Get(typeof(T));
			}
			public static TableMetaData Get(Type type)
			{
				if (type == typeof(object))
					throw new ArgumentException("Type 'object' cannot have TableMetaData", "type");

				return new TableMetaData(type);
			}

			/// <summary>
			/// Constructs the meta data for mapping a type to a database table.
			/// By default, 'Activator.CreateInstance' is used as the factory function. This is slow however so use a static delegate if possible</summary>
			private TableMetaData(Type type)
			{
				var column_name_trim = new[] { ' ', '\t', '\'', '\"', '[', ']' };

				// Get the table attribute
				var attrs = type.GetCustomAttributes(typeof(TableAttribute), true);
				var attr = attrs.Length != 0 ? (TableAttribute)attrs[0] : new TableAttribute();

				Type = type;
				Name = type.Name;
				Constraints = attr.Constraints ?? "";
				Factory = () => Type.New();
				TableKind = Kind.Unknown;

				// Tests if a member should be included as a column in the table
				bool IncludeMember(MemberInfo mi, List<string> marked) =>
					!mi.GetCustomAttributes(typeof(IgnoreAttribute), false).Any() &&  // doesn't have the ignore attribute and,
					(mi.GetCustomAttributes(typeof(ColumnAttribute), false).Any() ||  // has the column attribute or,
					(attr != null && attr.AllByDefault && marked.Contains(mi.Name))); // all in by default and 'mi' is in the collection of found columns

				const BindingFlags binding_flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
				var pflags = attr.PropertyBindingFlags != BindingFlags.Default ? attr.PropertyBindingFlags | BindingFlags.Instance : BindingFlags.Default;
				var fflags = attr.FieldBindingFlags != BindingFlags.Default ? attr.FieldBindingFlags | BindingFlags.Instance : BindingFlags.Default;

				// Create a collection of the columns of this table
				var cols = new List<ColumnMetaData>();
				{
					// If AllByDefault is enabled, get a collection of the properties/fields indicated by the binding flags in the attribute
					var mark = !attr.AllByDefault ? new List<string>() :
						type.GetProperties(pflags).Where(x => x.CanRead && x.CanWrite).Select(x => x.Name).Concat(
						type.GetFields(fflags).Select(x => x.Name)).ToList();

					// Check all public/non-public properties/fields
					cols.AddRange(AllProps(type, binding_flags).Where(pi => IncludeMember(pi, mark)).Select(pi => new ColumnMetaData(pi)));
					cols.AddRange(AllFields(type, binding_flags).Where(fi => IncludeMember(fi, mark)).Select(fi => new ColumnMetaData(fi)));

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
						type.GetFields(fflags).Select(x => x.Name)).ToList();

					// If we find public readonly properties or fields
					cols.AddRange(AllProps(type, binding_flags).Where(pi => IncludeMember(pi, mark)).Select(x => new ColumnMetaData(x)));
					cols.AddRange(AllProps(type, binding_flags).Where(fi => IncludeMember(fi, mark)).Select(x => new ColumnMetaData(x)));
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
					foreach (var pk in Constraints.Substring(s + 1, e - s - 1).Split(',').Select(x => x.Trim(column_name_trim)))
					{
						var col = cols.FirstOrDefault(x => x.Name == pk);
						if (col == null) throw new ArgumentException($"Named primary key column '{pk}' was not found as a table column for type '{Name}'");
						col.IsPk = true;
						col.Order = order++;
					}
				}

				// Sort the columns by the given order
				var cmp = Comparer<int>.Default;
				cols.Sort((lhs, rhs) => cmp.Compare(lhs.Order, rhs.Order));

				// Create the column arrays
				Columns = cols.ToArray();
				Pks = Columns.Where(x => x.IsPk).ToArray();
				NonPks = Columns.Where(x => !x.IsPk).ToArray();
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
			public string NameQuoted { get { return "'" + Name + "'"; } }

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
			public bool SingleIntegralPK => m_single_pk?.SqlDataType == SqlDbType.BigInt;
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

#if false
			/// <summary>
			/// Bind the primary keys 'keys' to the parameters in a
			/// prepared statement starting at parameter 'first_idx'.</summary>
			public void BindPks(sqlite3_stmt stmt, int first_idx, params object?[] keys)
			{
				if (first_idx < 1) throw new ArgumentException("Parameter binding indices start at 1 so 'first_idx' must be >= 1");
				if (Pks.Length != keys.Length) throw new ArgumentException("Incorrect number of primary keys passed for type " + Name);
				if (Pks.Length == 0) throw new ArgumentException("Attempting to bind primary keys for a type without primary keys");

				int idx = 0;
				foreach (var c in Pks)
				{
					var key = keys[idx];
					if (key == null) throw new ArgumentException($"Primary key value {idx + 1} should be of type {c.ClrType.Name} not null");
					if (key.GetType() != c.ClrType) throw new ArgumentException($"Primary key {idx + 1} should be of type {c.ClrType.Name} not type {key.GetType().Name}");
					c.BindFn(stmt, first_idx + idx, key); // binding parameters are indexed from 1 (hence the +1)
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
				if (key1.GetType() != Pks[0].ClrType) throw new ArgumentException("Primary key " + 1 + " should be of type " + Pks[0].ClrType.Name + " not type " + key1.GetType().Name);
				if (key2.GetType() != Pks[1].ClrType) throw new ArgumentException("Primary key " + 2 + " should be of type " + Pks[1].ClrType.Name + " not type " + key2.GetType().Name);
				Pks[0].BindFn(stmt, first_idx + 0, key1);
				Pks[1].BindFn(stmt, first_idx + 1, key2);
			}

			/// <summary>
			/// Bind the primary keys 'keys' to the parameters in a
			/// prepared statement starting at parameter 'first_idx'.</summary>
			public void BindPks(sqlite3_stmt stmt, int first_idx, object key1) // overload for performance
			{
				if (first_idx < 1) throw new ArgumentException("parameter binding indices start at 1 so 'first_idx' must be >= 1");
				if (Pks.Length != 1) throw new ArgumentException("Insufficient primary keys provided for this table type");
				if (key1.GetType() != Pks[0].ClrType) throw new ArgumentException("Primary key " + 1 + " should be of type " + Pks[0].ClrType.Name + " not type " + key1.GetType().Name);
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
					c.BindFn(stmt, first_idx + idx, c.Get(item));
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

						args.Add(col.ReadFn(stmt, i));
					}
					obj = Activator.CreateInstance(Type, args.ToArray(), null);
				}
				else if (TableKind == Kind.PrimitiveType)
				{
					var col = Column(0);
					obj = col.ReadFn(stmt, 0);
				}
				else
				{
					obj = null;
				}
				return obj;
			}
#endif

			/// <summary>Returns true of 'lhs' and 'rhs' are equal instances of this table type</summary>
			public bool Equal(object lhs, object rhs)
			{
				Debug.Assert(lhs.GetType() == Type, "'lhs' is not the correct type for this table");
				Debug.Assert(rhs.GetType() == Type, "'rhs' is not the correct type for this table");

#if COMPILED_LAMBDAS
				return (bool)m_method_equal.Invoke(null, new[]{lhs, rhs})!;
#else
				return MethodGenerator.Equal(lhs, rhs);
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
					foreach (var f in AllFields(lhs.GetType(), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
						if (!Equals(f.GetValue(lhs), f.GetValue(rhs)))
							return false;
					return true;
				}
				public static object Clone(object obj)
				{
					object clone = Activator.CreateInstance(obj.GetType());
					foreach (var f in AllFields(obj.GetType(), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
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

#if false
			/// <summary>Updates the value of the auto increment primary key (if there is one)</summary>
			public void SetAutoIncPK(object obj, sqlite3 db)
			{
				if (m_single_pk == null || !m_single_pk.IsAutoInc) return;
				int id = (int)NativeDll.LastInsertRowId(db);
				m_single_pk.Set(obj, id);
			}
#endif

			/// <summary>Returns all inherited properties for a type</summary>
			private static IEnumerable<PropertyInfo> AllProps(Type type, BindingFlags flags)
			{
				if (type == null || type == typeof(object) || type.BaseType == null) return Enumerable.Empty<PropertyInfo>();
				return AllProps(type.BaseType, flags).Concat(type.GetProperties(flags | BindingFlags.DeclaredOnly));
			}

			/// <summary>Returns all inherited fields for a type</summary>
			private static IEnumerable<FieldInfo> AllFields(Type type, BindingFlags flags)
			{
				if (type == null || type == typeof(object) || type.BaseType == null) return Enumerable.Empty<FieldInfo>();
				return AllFields(type.BaseType, flags).Concat(type.GetFields(flags | BindingFlags.DeclaredOnly));
			}
		}

		/// <summary>A column within a db table</summary>
		public sealed class ColumnMetaData

		{
			private ColumnMetaData(MemberInfo? mi, Type type, Func<object, object?> get, Action<object, object?> set)
			{
				var attr = (ColumnAttribute?)null;
				attr ??= (ColumnAttribute?)mi?.GetCustomAttributes(typeof(ColumnAttribute), true).FirstOrDefault();
				attr ??= new ColumnAttribute();
				
				var is_nullable = Nullable.GetUnderlyingType(type) != null;

				// Setup accessors
				Get = get;
				Set = set;

				MemberInfo = mi;
				Name = attr.Name ?? mi?.Name ?? string.Empty;
				SqlDataType = attr.SqlDataType ?? type.SqlType();
				Constraints = attr.Constraints ?? "";
				IsPk = attr.PrimaryKey;
				IsAutoInc = attr.AutoInc;
				IsNotNull = type.IsValueType && !is_nullable;
				IsCollate = false;
				Order = OrderBaseValue + attr.Order;
				ClrType = type;

				//// Set up the bind and read methods
				//BindFn = Bind.FuncFor(type);
				//ReadFn = Read.FuncFor(type);
			}
			internal ColumnMetaData(PropertyInfo pi)
				: this(pi, pi.PropertyType, obj => pi.GetValue(obj, null), (obj, val) => pi.SetValue(obj, val, null))
			{}
			internal ColumnMetaData(FieldInfo fi)
				:this(fi, fi.FieldType, fi.GetValue, fi.SetValue)
			{}
			internal ColumnMetaData(Type type)
				:this(null, type, obj => obj, (obj, val) => throw new NotImplementedException())
			{}

			/// <summary>The member info for the member represented by this column</summary>
			public MemberInfo? MemberInfo;

			/// <summary>The name of the column</summary>
			public string Name;
			public string NameBracketed => $"[{Name}]";

			/// <summary>The data type of the column</summary>
			public SqlDbType SqlDataType;

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
			public Func<object, object?> Get { get; set; }

			/// <summary>Sets the value of this column in an object of type 'ClrType'</summary>
			public Action<object, object?> Set { get; set; }

			///// <summary>Binds the value from this column to parameter 'index' in 'stmt'</summary>
			//public Bind.Func BindFn;

			///// <summary>Reads column 'index' from 'stmt' and sets the corresponding property or field in 'obj'</summary>
			//public Read.Func ReadFn;

			/// <summary>Returns the column definition for this column</summary>
			public string ColumnDef(bool incl_pk)
			{
				var sql_type = SqlDataType.ToString().ToLowerInvariant();
				var pk = incl_pk && IsPk ? "primary key " : "";
				var ainc = IsAutoInc ? "autoincrement " : "";
				return $"[{Name}] {sql_type} {pk} {ainc} {Constraints}";
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

			public const int OrderBaseValue = 0xFFFF;
		}


		/// <summary>Convert from SQL data type to 'DbType'</summary>
		public static DbType ToDbType(this EDataType type)
		{
			return type switch
			{
				EDataType.Null => DbType.Object,
				EDataType.Text => DbType.String,
				EDataType.Integer => DbType.Int64,
				EDataType.Real => DbType.Double,
				EDataType.Blob => DbType.Binary,
				_ => throw new Exception($"Unknown conversion from {type} to {nameof(DbType)}"),
			};
		}

		/// <summary>Convert a type to its equivalent Sqlite data type</summary>
		public static EDataType SqlType(DbType type)
		{
			return type switch
			{
				DbType.Object                => EDataType.Null,
				DbType.String                => EDataType.Text,
				DbType.StringFixedLength     => EDataType.Text,
				DbType.AnsiString            => EDataType.Text,
				DbType.AnsiStringFixedLength => EDataType.Text,
				DbType.Boolean               => EDataType.Integer,
				DbType.Byte                  => EDataType.Integer,
				DbType.SByte                 => EDataType.Integer,
				DbType.Int16                 => EDataType.Integer,
				DbType.Int32                 => EDataType.Integer,
				DbType.Int64                 => EDataType.Integer,
				DbType.UInt16                => EDataType.Integer,
				DbType.UInt32                => EDataType.Integer,
				DbType.UInt64                => EDataType.Integer,
				DbType.Currency              => EDataType.Integer,
				DbType.Single                => EDataType.Real,
				DbType.Double                => EDataType.Real,
				DbType.Decimal               => EDataType.Real,
				DbType.Date                  => EDataType.Integer,
				DbType.Guid                  => EDataType.Text,
				DbType.Binary                => EDataType.Blob,

				DbType.DateTime       => throw new NotImplementedException(),
				DbType.DateTime2      => throw new NotImplementedException(),
				DbType.DateTimeOffset => throw new NotImplementedException(),
				DbType.Time           => throw new NotImplementedException(),
				DbType.VarNumeric     => throw new NotImplementedException(),
				DbType.Xml            => throw new NotImplementedException(),
				
				_ => throw new Exception($"Unknown conversion from {type} to {nameof(EDataType)}"),
			};
		}

#endif
