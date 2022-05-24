using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Rylogic.Extn;

namespace Rylogic.Db
{
	public static partial class Sqlite
	{
		/// <summary>Iterator (of sorts) for reading query results</summary>
		[DebuggerDisplay("{Desc,nq}")]
		public sealed class DataReader : IDataReader, IDisposable
		{
			// Notes:
			//  - Reading results:
			//    A query can contain multiple statements, each statement returns a 'Result', the set of results is the 'ResultSet'
			//    Use 'Read()' to iterate over the rows in a single Result, and 'NextResult()' to skip to the next result in the set.

			internal DataReader(Command cmd)
			{
				Cmd = cmd;
				RecordsAffected = 0;
				ResultSet = 0;
				StepCount = 0;
				StepInc = 1;
			}
			public void Dispose()
			{
				Cmd.Dispose();
			}

			/// <summary>The command that created the statements</summary>
			private Command Cmd { get; }

			/// <summary>The last error message</summary>
			private string ErrorMsg => Cmd.Connection.ErrorMsg;

			/// <summary>The statement handle</summary>
			private sqlite3_stmt Stmt
			{
				get => Cmd.Stmt ?? throw new ArgumentNullException(nameof(Stmt), "To statement to execute");
				set => Cmd.Stmt = value;
			}

			/// <summary>The index of the current result set</summary>
			public int ResultSet { get; private set; }

			/// <summary>The current step number within the current result set</summary>
			public int StepCount { get; private set; }
			private int StepInc;

			/// <summary>The accumulated number of rows affected</summary>
			public int RecordsAffected { get; private set; }

			/// <summary>Iterate to the next row in the result. Returns true if there are more rows available (see Notes)</summary>
			public bool Read()
			{
				Debug.Assert(AssertCorrectThread());

				// No statement to execute
				if (Stmt == null)
					return false;

				for (; ; )
				{
					var res = NativeAPI.Step(Stmt);
					switch (res.BasicCode())
					{
						case EResult.Row:
						{
							RecordsAffected += Cmd.RowsChanged;
							StepCount += StepInc;
							return true;
						}
						case EResult.Done:
						{
							RecordsAffected += Cmd.RowsChanged;
							StepCount += StepInc;
							StepInc = 0;
							Stmt = null!;
							return false;
						}
						case EResult.Busy:
						{
							Thread.Yield();
							break;
						}
						case EResult.Locked:
						{
							// When a call to sqlite3_step() returns SQLITE_LOCKED, it is almost always appropriate to call sqlite3_unlock_notify().
							// There is however, one exception. When executing a "DROP TABLE" or "DROP INDEX" statement, SQLite checks if there are
							// any currently executing SELECT statements that belong to the same connection. If there are, SQLITE_LOCKED is returned.
							// In this case there is no "blocking connection", so invoking sqlite3_unlock_notify() results in the unlock-notify callback
							// being invoked immediately. If the application then re-attempts the "DROP TABLE" or "DROP INDEX" query, an infinite loop might
							// be the result.
							// One way around this problem is to check the extended error code returned by an sqlite3_step() call. If there is a blocking
							// connection, then the extended error code is set to SQLITE_LOCKED_SHAREDCACHE. Otherwise, in the special "DROP TABLE/INDEX"
							// case, the extended error code is just SQLITE_LOCKED.
							if (res != EResult.Locked_SharedCache)
								throw new SqliteException(res, "Database locked", ErrorMsg);

							// Wait for the DB to unlock
							using var mre = new ManualResetEvent(false);
							void WaitForUnlock(IntPtr argv, int argc) => mre.Set();
							var rc = NativeAPI.UnlockNotify(Cmd.Connection.Handle, WaitForUnlock, IntPtr.Zero);
							if (rc != EResult.OK && rc.BasicCode() != EResult.Locked)
								throw new SqliteException(rc, $"Failed to register callback in UnlockNotify", ErrorMsg);

							mre.WaitOne();
							break;
						}
						default:
						{
							throw new SqliteException(res, string.Empty, ErrorMsg);
						}
					}
				}
			}

			/// <summary>Advance to the next result (see Notes)</summary>
			public bool NextResult()
			{
				// Advance to the next statement
				if (!Cmd.PrepareNext())
					return false;

				// Bind parameters for the next statement
				Cmd.BindParameters();
				++ResultSet;

				// Reset the step count
				StepCount = 0;
				StepInc = 1;

				// Invalidate cached data
				m_column_info = null!;
				return true;
			}

			/// <summary>The number of columns in the result</summary>
			public int ColumnCount => ColumnInfo.Length;

			/// <summary>The native data type of the column</summary>
			public EDataType ColumnType(int i) => ColumnInfo[i].Type;

			/// <inheritdoc/>
			public string ColumnName(int i) => ColumnInfo[i].Name;

			/// <summary>Return the index of the column with the given name</summary>
			public int ColumnIndex(string column)
			{
				return FindColumnIndex(column) ?? throw new SqliteException(EResult.NotFound, $"No column named {column} in result", string.Empty);
			}

			/// <summary>Look for a column with the given name. Returns null if not found</summary>
			public int? FindColumnIndex(string name)
			{
				var idx = ColumnInfo.IndexOf(x => x.Name == name);
				return idx >= 0 ? idx : null;
			}

			/// <summary>Cached column info (for performance)</summary>
			private ColumnInfoData[] ColumnInfo
			{
				get
				{
					if (m_column_info == null)
					{
						var count = NativeAPI.ColumnCount(Stmt);
						var column_info = new List<ColumnInfoData>(count);
						for (int i = 0, iend = count; i != iend; ++i)
						{
							column_info.Add(new ColumnInfoData
							{
								Name = NativeAPI.ColumnName(Stmt, i),
								Type = NativeAPI.ColumnType(Stmt, i),
							});
						}
						m_column_info = column_info.ToArray();
					}
					return m_column_info;
				}
			}
			private ColumnInfoData[]? m_column_info;

			/// <summary>Read the value of column 'i'</summary>
			public object Get(Type ty, int i) => BindMap.Read(ty)(Stmt, i);
			public object Get(Type ty, string column) => Get(ty, ColumnIndex(column));
			public Item Get<Item>(int i) => (Item)Get(typeof(Item), i);
			public Item Get<Item>(string column) => (Item)Get(typeof(Item), column);

			/// <summary>Read the whole row as 'item'</summary>
			public Item ToObject<Item>()
			{
				var ty = typeof(Item);
				return (Item)TypeMap[ty](this, ty);
			}

			/// <inheritdoc/>
			public object this[int i] => Get<object>(i);

			/// <inheritdoc/>
			public object this[string column] => Get<object>(ColumnIndex(column));

			/// <inheritdoc/>
			public long GetBytes(int i, long field_offset, byte[] buffer, int buffer_offset, int length)
			{
				NativeAPI.ColumnBlob(Stmt, i, out var ptr, out var len);
				var count = Math.Max(0, Math.Min(len - (int)field_offset, length));
				Marshal.Copy(ptr + (int)field_offset, buffer, buffer_offset, count);
				return count;
			}

			/// <inheritdoc/>
			public long GetChars(int i, long field_offset, char[] buffer, int buffer_offset, int length)
			{
				NativeAPI.ColumnBlob(Stmt, i, out var ptr, out var len);
				var count = Math.Max(0, Math.Min((len - (int)field_offset) / Marshal.SizeOf<char>(), length));
				Marshal.Copy(ptr + (int)field_offset, buffer, buffer_offset, count);
				return count;
			}

			/// <summary>Throw if called from a different thread than the one that created this connection</summary>
			public bool AssertCorrectThread() => Cmd.AssertCorrectThread();

			/// <summary>Debugging description</summary>
			public string Desc => $"ResultSet={ResultSet} Step={StepCount} ColumnCount={ColumnCount}";

			/// <summary>Cached result columns</summary>
			private class ColumnInfoData
			{
				public string Name = string.Empty;
				public EDataType Type;
			}

			#region IDataReader
			int IDataRecord.FieldCount => ColumnCount;
			Type IDataRecord.GetFieldType(int i) => ColumnType(i).ToType();
			string IDataRecord.GetName(int i) => ColumnName(i);
			string IDataRecord.GetDataTypeName(int i) => ColumnType(i).ToType().Name;
			int IDataRecord.GetOrdinal(string column) => ColumnIndex(column);
			object IDataRecord.GetValue(int i) => Get<object>(i);
			int IDataRecord.GetValues(object[] values)
			{
				int i = 0, iend = Math.Min(values.Length, ColumnCount);
				for (; i != iend; ++i) values[i] = Get<object>(i);
				return iend;
			}
			bool IDataRecord.IsDBNull(int i) => ColumnType(i) == EDataType.Null;
			bool IDataRecord.GetBoolean(int i) => Get<bool>(i);
			byte IDataRecord.GetByte(int i) => Get<byte>(i);
			char IDataRecord.GetChar(int i) => Get<char>(i);
			string IDataRecord.GetString(int i) => Get<string>(i);
			short IDataRecord.GetInt16(int i) => Get<short>(i);
			int IDataRecord.GetInt32(int i) => Get<int>(i);
			long IDataRecord.GetInt64(int i) => Get<long>(i);
			float IDataRecord.GetFloat(int i) => Get<float>(i);
			double IDataRecord.GetDouble(int i) => Get<double>(i);
			decimal IDataRecord.GetDecimal(int i) => Get<decimal>(i);
			Guid IDataRecord.GetGuid(int i) => Get<Guid>(i);
			DateTime IDataRecord.GetDateTime(int i) => Get<DateTime>(i);
			int IDataReader.Depth => 0;
			void IDataReader.Close() => Dispose();
			bool IDataReader.IsClosed => Cmd.Stmt == null!;
			bool IDataReader.NextResult() => NextResult();
			IDataReader IDataRecord.GetData(int i) => throw new NotSupportedException();
			DataTable IDataReader.GetSchemaTable() => throw new NotSupportedException();
			#endregion
		}
	}
}
