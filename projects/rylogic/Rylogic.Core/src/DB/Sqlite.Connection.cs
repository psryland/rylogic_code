using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Db
{
	public static partial class Sqlite
	{
		/// <summary>Open a connection to a database</summary>
		[DebuggerDisplay("{Label,nq} {Status,nq}")]
		public class Connection : IDbConnection, IDisposable
		{
			private readonly GCHandle m_gc_handle;
			public Connection(string connection_string, string label, bool cache_stmts = false)
			{
				m_gc_handle = GCHandle.Alloc(this);
				Label = label;
				OwnerThreadId = Thread.CurrentThread.ManagedThreadId;
				Cache = new StmtCache(this, cache_stmts);
				ConnectionString = connection_string;

				Open();
			}
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}
			protected virtual void Dispose(bool _)
			{
				Cache.Dispose();
				Close();
				m_gc_handle.Free();
			}

			/// <summary>True if the connection is open</summary>
			public bool IsOpen => Handle is NativeSqlite3Handle handle && handle.IsValid && !handle.IsClosed;

			/// <summary>The db connection handle</summary>
			internal sqlite3 Handle
			{
				get => m_handle;
				private set
				{
					if (m_handle == value) return;
					if (m_handle != null)
					{
						if (IsOpen)
						{
							Handle.Close();
							if (Handle.CloseResult == EResult.Busy)
								throw new SqliteException(Handle.CloseResult, "Could not close database handle, there are still prepared statements that haven't been 'finalized' or blob handles that haven't been closed.", string.Empty);
							if (Handle.CloseResult != EResult.OK)
								throw new SqliteException(Handle.CloseResult, "Failed to close database connection", string.Empty);
						}
						Util.Dispose(m_handle!);
					}
					m_handle = (NativeSqlite3Handle)value;
					if (m_handle != null)
					{
						m_handle.Label = Label;
					}
				}
			}
			private NativeSqlite3Handle m_handle = null!;

			///<summary>The Id of the thread that created this DB handle.</summary>
			public int OwnerThreadId { get; }

			/// <summary>Connection string</summary>
			public string ConnectionString
			{
				get => m_connection_string;
				set
				{
					if (IsOpen) throw new SqliteException(EResult.Misuse, "Can't change the connection string while the connection is open", string.Empty);
					m_connection_string = value;
				}
			}
			private string m_connection_string = string.Empty;

			/// <summary>Debugging label for the connection</summary>
			public string Label { get; set; }

			/// <summary>The name of the database this connection is too</summary>
			public string Database { get; private set; } = string.Empty;

			/// <summary>Open the connection to the DB</summary>
			public Connection Open()
			{
				return Open(ConnectionString);
			}
			public Connection Open(string connection_string)
			{
				ConnectionString = connection_string;

				// Parse the connection string
				var options = new Dictionary<string, string>();
				foreach (var kv in connection_string.Split(';'))
				{
					var idx = kv.IndexOf('=');
					if (idx == -1) throw new SqliteException(EResult.Misuse, $"Connection string is invalid near '{kv}'", string.Empty);
					options.Add(
						kv.Substring(0, idx).ToLowerInvariant().Trim(),
						kv.Substring(idx + 1).Trim());
				}

				// Create the dll version
				if (options.TryGetValue("version", out var version_string))
				{
					var version = int.TryParse(version_string, out var v) ? v : throw new SqliteException(EResult.Error, $"Invalid value for 'Version': {version_string}", string.Empty);
					var lib_version = NativeAPI.LibVersionNumber(); // Version encoded as Mmmmppp, M = Major, mmm = minor, ppp = point
					if (version * 1_000_000 > lib_version)
						throw new SqliteException(EResult.Error, $"SQL library version is {NativeAPI.LibVersion()}. Version >= {version}.0.0 was requested", string.Empty);
				}

				// Set the connection flags
				var flags = EOpenFlags.None;
				if (options.TryGetValue("mode", out var mode))
				{
					switch (mode.ToLower())
					{
						case "readonly": flags |= EOpenFlags.ReadOnly; break;
						case "readwrite": flags |= EOpenFlags.ReadWrite; break;
						case "readwritecreate": flags |= EOpenFlags.ReadWrite | EOpenFlags.Create; break;
						case "memory": flags |= EOpenFlags.ReadWrite | EOpenFlags.Create | EOpenFlags.Memory; break;
						case "default": flags |= EOpenFlags.ReadWrite | EOpenFlags.Create; break;
						default: throw new SqliteException(EResult.Error, $"Mode {mode} is not supported.", string.Empty);
					}
				}
				else
				{
					flags |= EOpenFlags.ReadWrite | EOpenFlags.Create;
				}
				if (options.TryGetValue("cache", out var cache))
				{
					switch (cache.ToLower())
					{
						case "default": flags |= EOpenFlags.None; break;
						case "private": flags |= EOpenFlags.PrivateCache; break;
						case "shared": flags |= EOpenFlags.SharedCache; break;
						default: throw new SqliteException(EResult.Error, $"Cache mode {cache} is not supported.", string.Empty);
					}
				}
				else
				{
					flags |= EOpenFlags.None;
				}
				if (options.TryGetValue("threadsafe", out var threadsafe))
				{
					// 'threadsafe' means threadsafe *per connection*, so NoMutex = one thread per connection, FullMutex = N threads per connection.
					switch (threadsafe.ToLower())
					{
						case "off": flags |= EOpenFlags.NoMutex; break;
						case "on": flags |= EOpenFlags.FullMutex; break;
						default: throw new SqliteException(EResult.Error, $"ThreadSafe option {threadsafe} is not supported.", string.Empty);
					}
				}
				else if (NativeAPI.CompiledThreadingMode != EConfigOption.SingleThread)
				{
					flags |= EOpenFlags.FullMutex;
				}

				// Open the connection
				if (!options.TryGetValue("data source", out var db_source))
					throw new SqliteException(EResult.Error, $"No 'Data Source' provided in connection string", string.Empty);

				// Handle URI data sources
				if (db_source.StartsWith("file:"))
					flags |= EOpenFlags.URI;

				// Allow extended error codes
				flags |= EOpenFlags.ExResCode;

				// Open the connection
				Open(db_source, flags);

				// Set other DB settings
				if (options.TryGetValue("synchronous", out var sync))
					Cmd($"PRAGMA synchronous = {sync.ToUpper()}").Execute();
				if (options.TryGetValue("journal mode", out var journal_mode) ||
					options.TryGetValue("journal_mode", out journal_mode))
					Cmd($"PRAGMA journal_mode = {journal_mode.ToUpper()}").Execute();
				if (options.TryGetValue("foreign keys", out var foreign_keys) ||
					options.TryGetValue("foreign_keys", out foreign_keys))
					Cmd($"PRAGMA foreign_keys = {(foreign_keys.ToUpper() == "ON" ? "1" : "0")}").Execute();
				if (options.TryGetValue("recursive triggers", out var recursive_triggers) ||
					options.TryGetValue("recursive_triggers", out recursive_triggers))
					Cmd($"PRAGMA recursive_triggers = {(recursive_triggers.ToUpper() == "TRUE" ? "1" : "0")}").Execute();

				return this; // Fluent
			}
			public Connection Open(string db_source, EOpenFlags flags)
			{
				Close();

				Handle = NativeAPI.Open(db_source, flags);
				Database = db_source;
				Flags = flags;

				return this; // Fluent
			}

			/// <summary>Close a database file</summary>
			public void Close()
			{
				Debug.Assert(AssertCorrectThread());
				Database = string.Empty;
				Handle = new NativeSqlite3Handle();
			}

			/// <summary>
			/// Create a query based on 'sql'.<para/>
			/// Set 'reuse' to true to prevent the command execution disposing the command and returning it to the cache.</summary>
			public Command Cmd(string sql, Transaction? transaction = null)
			{
				// Notes:
				//  - A command is a batch of statements.
				//  - Each statement in the batch can only be prepared after the prior one has executed.
				//  - Commands are not cached, but statements are.
				sql = sql.Trim(' ', '\t', ';', '\r', '\n');
				return new Command(this, sql, transaction);
			}

			/// <summary>A cache of prepared statements</summary>
			internal StmtCache Cache { get; }

			/// <summary>Create a transaction scope</summary>
			public Transaction BeginTransaction() => BeginTransaction(IsolationLevel.Serializable);
			public Transaction BeginTransaction(IsolationLevel il) => new Transaction(this, il, null);

			/// <summary>Return a table helper instance for a table based on 'Record'</summary>
			public Table<Record> Table<Record>() => new Table<Record>(this);

			/// <summary>Sets the busy wait timeout period in milliseconds.</summary>
			public int BusyTimeout
			{
				// This controls how long the thread sleeps for when waiting to gain a lock on a table.
				// After this timeout period Step() will return Result.Busy. Set to 0 to turn off the busy wait handler.
				set { NativeAPI.BusyTimeout(Handle, value); }
			}

			/// <summary>Returns the RowId for the last inserted row</summary>
			public long LastInsertRowId => NativeAPI.LastInsertRowId(Handle);

			/// <summary>Last error code</summary>
			public EResult ErrorCode => NativeAPI.ErrorCode(Handle);

			/// <summary>Last error code</summary>
			public EResult ErrorCodeEx => NativeAPI.ErrorCodeExtended(Handle);

			/// <summary>Return the error message for the last error on this connection</summary>
			public string ErrorMsg => NativeAPI.ErrorMsg(Handle);

			/// <summary>The flags used to open this connection</summary>
			public EOpenFlags Flags { get; private set; }

			/// <summary>Helper status string for the state of the connection</summary>
			public string Status => $"{Database}: {(((NativeSqlite3Handle)Handle).IsValid ? "Active" : ((NativeSqlite3Handle)Handle).IsClosed ? "Closed" : "Invalid")}";

			/// <summary>Return a Command to the cache 'free' list</summary>
			internal bool ReturnToCache(sqlite3_stmt stmt) => Cache.ReturnToPool(stmt);

			/// <summary>Throw if called from a different thread than the one that created this connection</summary>
			public bool AssertCorrectThread()
			{
				// Sqlite can actually support multi-thread access via the same DB connection but it is not recommended.
				// If ConfigOption.SingleThreaded is used, all access must be from one thread
				// If ConfigOption.MultiThreaded is used, only one thread at a time can use the DB connection.
				// If ConfigOption.Serialized is used, it's open session for any threads
				if (OwnerThreadId == Thread.CurrentThread.ManagedThreadId) return true;
				if (Flags.HasFlag(EOpenFlags.FullMutex)) return true;
				throw new SqliteException(EResult.Misuse, $"Cross-thread use of Sqlite.\n{new StackTrace()}", string.Empty);
			}

			#region IDbConnection
			string IDbConnection.ConnectionString
			{
				get => ConnectionString;
				set => ConnectionString = value;
			}
			string IDbConnection.Database => Database;
			void IDbConnection.Open() => Open();
			void IDbConnection.Close() => Close();
			IDbCommand IDbConnection.CreateCommand() => Cmd(string.Empty, null);
			IDbTransaction IDbConnection.BeginTransaction() => BeginTransaction();
			IDbTransaction IDbConnection.BeginTransaction(IsolationLevel il) => BeginTransaction(il);
			void IDbConnection.ChangeDatabase(string databaseName) => throw new NotImplementedException();
			int IDbConnection.ConnectionTimeout => throw new NotImplementedException();
			ConnectionState IDbConnection.State => throw new NotImplementedException();
			#endregion

			/// <summary>A store of prepared statements</summary>
			[DebuggerDisplay("Count={m_cache.Count}")]
			internal sealed class StmtCache : IDisposable
			{
				// Notes:
				//  - There is one of these per 'Connection', since all prepared statements
				//    are associated with a connection and must be Disposed before the connection
				//    is closed.
				//  - Since each connection belongs to one thread, we don't need thread safety here.
				//  - The cache controls the life time of all 'Statements' because we can't have the
				//    GC collecting them.
				//  - A 'Command' is a list of statements

				private readonly Dictionary<string, StmtList> m_cache;

				public StmtCache(Connection db, bool enabled)
				{
					m_cache = new Dictionary<string, StmtList>();
					Enabled = enabled;
					Connection = db;
				}
				public void Dispose()
				{
					Debug.Assert(Connection.AssertCorrectThread());
					m_disposing = true;

					foreach (var list in m_cache.Values)
					{
						if (list.InUse != 0)
							throw new SqliteException(EResult.Misuse, $"There are still prepared statements in use", string.Empty);

						Util.DisposeRange(list.Stmts);
					}

					m_cache.Clear();
				}
				private bool m_disposing;

				/// <summary>The owning connection</summary>
				public Connection Connection { get; }

				/// <summary>Allow the cache to be disabled</summary>
				public bool Enabled { get; }

				/// <summary>Look for an existing prepared statement for the next statement in 'sql_utf8' starting from 'next_byte'</summary>
				public sqlite3_stmt Get(string sql)
				{
					if (!Enabled)
						return NativeAPI.Prepare(Connection.Handle, sql);

					var list = m_cache.GetOrAdd(sql, _ => new StmtList());
					var stmt = list.InUse != list.Stmts.Count
						? list.Stmts[list.InUse]
						: list.Stmts.Add2((NativeSqlite3StmtHandle)NativeAPI.Prepare(Connection.Handle, sql));
					++list.InUse;

					if (list.InUse > 10)
						throw new Exception("10 Concurrent uses of this sql statement?");

					// Return the recycled statement
					return stmt;
				}

				/// <summary>Return a command to the object pool</summary>
				public bool ReturnToPool(sqlite3_stmt stmt)
				{
					Debug.Assert(Connection.AssertCorrectThread());
					if (m_disposing || !Enabled)
						return false;

					// Move from the used to free commands
					var sql = NativeAPI.SqlString(stmt) ?? throw new NullReferenceException("Statement has no sql");
					var list = m_cache.GetOrAdd(sql, _ => new StmtList());
					var idx = list.Stmts.IndexOf(stmt);
					if (idx >= 0 && idx < list.InUse)
					{
						// Allow statements to be disposed multiple times. This can happen if 'Prepare'
						// throws for a 'Reuse' command that is used with a 'using var cmd = ' statement.
						list.Stmts.Swap(idx, list.InUse - 1); // Swap to the end of the used list
						--list.InUse;                         // Return to the free list
					}
					else if (idx < 0)
					{
						// 'stmt' wasn't in the list already? Take it I guess...
						list.Stmts.Add((NativeSqlite3StmtHandle)stmt);
					}

					// Limit the maximum size of the cache. Should I?
					return true;
				}

				/// <summary>Release all not-in-use statements</summary>
				public void Flush()
				{
					foreach (var kv in m_cache.ToList())
					{
						var list = kv.Value;
						Util.DisposeRange(list.Stmts, list.InUse, list.Stmts.Count - list.InUse);
						
						if (list.InUse != 0)
							list.Stmts.Resize(list.InUse);
						else
							m_cache.Remove(kv.Key);
					}
				}

				/// <summary>A list of free and used statements</summary>
				[DebuggerDisplay("{InUse} / {Stmts.Count}")]
				private class StmtList
				{
					/// <summary>The list of in-use and free Command instances</summary>
					public List<NativeSqlite3StmtHandle> Stmts { get; } = new List<NativeSqlite3StmtHandle>();

					/// <summary>The number of commands in use</summary>
					public int InUse { get; set; } = 0;
				}
			}
		}
	}
}
