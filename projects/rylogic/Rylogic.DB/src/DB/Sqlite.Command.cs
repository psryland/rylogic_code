using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Rylogic.Str;
using Rylogic.Utility;

namespace Rylogic.DB
{
	public static partial class Sqlite
	{
		/// <summary>A command</summary>
		[DebuggerDisplay("{Sql,nq}")]
		public sealed class Command : IDbCommand, IDisposable
		{
			// Notes:
			//  - Sql Prepare only consumes up to the first ';' in a multi statement expression.
			//  - Each expression is treated as a separate command, so only the parameters of that statement can be assigned.
			//  - Subsequent statements depend on the result of prior statements, so Prepare on the next statement must be
			//    called after executing the prior statement.

			internal Command(Connection db, string sql, Transaction? transaction = null)
			{
				Stmt = null!;
				Parameters = new ParameterCollection();
				Connection = db;
				Sql = SplitStatements(sql).ToList();
				Transaction = transaction;
				NextStmtIndex = 0;

				PrepareNext();
			}
			public void Dispose()
			{
				Stmt = null;
			}

			/// <summary>The DB connection used to create this command</summary>
			public Connection Connection { get; }

			/// <summary>The current prepared statements</summary>
			internal sqlite3_stmt? Stmt
			{
				get => m_stmt;
				set
				{
					if (m_stmt == value) return;
					if (m_stmt != null)
					{
						// Reset the statement to release any locks
						NativeAPI.Reset(m_stmt);

						// Try to return the statement to the pool. Returns false if 'stmt' should be disposed instead.
						if (!Connection.ReturnToCache(m_stmt))
						{
							// Close instead
							m_stmt.Close(); // After 'sqlite3_finalize()', it is illegal to use 'm_stmt'.
							if (m_stmt.CloseResult != EResult.OK)
								throw new SqliteException(m_stmt.CloseResult, string.Empty, ErrorMsg);
						}
					}
					m_stmt = value;
				}
			}
			private sqlite3_stmt? m_stmt = null;

			/// <summary>Last error message</summary>
			private string ErrorMsg => Connection.ErrorMsg;

			/// <summary>The parameter bindings</summary>
			public ParameterCollection Parameters { get; }

			/// <summary>The SQL commands</summary>
			public List<string> Sql { get; }
			private int NextStmtIndex;

			/// <summary>Split multiple sql statements into separate strings</summary>
			private IEnumerable<string> SplitStatements(string sql)
			{
				var lit = new InLiteral();
				for (int s = 0, e = 0;;)
				{
					// End of the string
					if (e == sql.Length)
					{
						var sub = sql.Substring(s, e - s).Trim(' ', '\t', '\r', '\n');
						if (sub.Length != 0) yield return sub;
						break;
					}

					// Consume string literals
					if (lit.WithinLiteral(sql[e]))
					{
						for (++e; e != sql.Length && lit.WithinLiteral(sql[e]); ++e) { }
						continue;
					}

					// Statement delimiter
					if (sql[e] == ';')
					{
						var sub = sql.Substring(s, e - s).Trim(' ', '\t', '\r', '\n');
						yield return sub;
						s = ++e;
						continue;
					}

					++e;
				}
			}

			/// <summary>The transaction scope for the command</summary>
			public Transaction? Transaction
			{
				get => m_transaction;
				set
				{
					if (value != null && !ReferenceEquals(Connection, value.Connection))
						throw new SqliteException(EResult.Misuse, "This Transaction belongs to a diffent DB connection", string.Empty);
					
					m_transaction = value;
				}
			}
			public Transaction? m_transaction;

			/// <summary>Returns the number of rows changed as a result of the last 'step()'</summary>
			public int RowsChanged => NativeAPI.Changes(Connection.Handle);

			/// <summary>Clear the parameter collection</summary>
			public Command Clear()
			{
				Parameters.Clear();
				return this; // Fluent
			}

			/// <summary>Add a parameter to the parameter collection</summary>
			public Command AddParam(string name, object? value, BindFunc? bind = null)
			{
				Parameters.Add(name, value, bind);
				return this; // Fluent
			}

			/// <summary>Parse the SQL string into a compiled statement</summary>
			internal bool PrepareNext()
			{
				if (NextStmtIndex == Sql.Count)
					return false;

				// Prepare the next statement
				Stmt = Connection.Cache.Get(Sql[NextStmtIndex]);
				NextStmtIndex++;
				return true;
			}

			/// <summary>Bind the parameters to the statement</summary>
			internal void BindParameters()
			{
				// This is called just before the command is executed. Parameters are buffered in 'Parameters'
				// to allow them to be queried, changed, updated, returned from IDbCommand, etc up until that point.
				// The SQL should be prepared as soon as the SQL is known (and the command is in the cache).
				if (Stmt == null)
					throw new SqliteException(EResult.Misuse, $"Statement has not been prepared", ErrorMsg);

				// Bind parameters to each statement
				NativeAPI.Reset(Stmt);

				// Bind the parameters. Parameter binding starts at 1.
				for (int p = 0, pend = NativeAPI.BindParameterCount(Stmt); p != pend; ++p)
				{
					var pname = NativeAPI.BindParameterName(Stmt, p + 1) ?? throw new SqliteException(EResult.Error, $"No parameter at index {p}.\nSql: {NativeAPI.SqlString(Stmt)}", ErrorMsg);
					var parameter = Parameters[pname];
					parameter.Bind(Stmt, p + 1, parameter.Value!);
				}
			}

			/// <summary>Executes an sql query that is expected to not return results. e.g. Insert/Update/Delete. Returns the number of rows affected by the operation.</summary>
			public int Execute()
			{
				Debug.Assert(AssertCorrectThread());
				using var exec = ExecutionScope();
				BindParameters();

				var reader = Reader();
				for (; ; )
				{
					for (; reader.Read();) {}
					if (!reader.NextResult())
						break;
				}

				return reader.RecordsAffected;
			}

			/// <summary>Executes an sql query that is expected to return a single value. Returns the signal value as type 'Value'</summary>
			public Value Scalar<Value>()
			{
				Debug.Assert(AssertCorrectThread());
				using var exec = ExecutionScope();
				BindParameters();

				var reader = Reader();

				// Step to the first result
				if (!reader.Read())
					throw new SqliteException(EResult.Error, "Scalar query returned no results", string.Empty);
				if (reader.ColumnCount != 1)
					throw new SqliteException(EResult.Error, "Scalar query returned multiple values", string.Empty);

				// Read the value
				var value = reader.Get<Value>(0);

				// Try to step to the next result, to check there's only one
				if (reader.Read())
					throw new SqliteException(EResult.Error, "Scalar query returned more than one result", string.Empty);

				return value;
			}

			/// <summary>Return the results of a query</summary>
			public IEnumerable<Item> ExecuteQuery<Item>()
			{
				Debug.Assert(AssertCorrectThread());
				using var exec = ExecutionScope();
				BindParameters();

				var reader = Reader();
				for (; reader.Read();)
					yield return reader.ToObject<Item>();
			}
			public DataReader ExecuteQuery() // Remember to Dispose
			{
				Debug.Assert(AssertCorrectThread());
				BindParameters();
				return Reader();
			}

			/// <summary>Get a DataReader interface to this command</summary>
			private DataReader Reader() => new(this);

			/// <summary>Helper for resetting/disposing the command</summary>
			private IDisposable ExecutionScope()
			{
				return Scope.Create(
				() =>
				{
					if (Stmt != null) return;
					NextStmtIndex = 0;
					PrepareNext();
				},
				() =>
				{
					Stmt = null;
				});
			}

			/// <summary>Throw if called from a different thread than the one that created this connection</summary>
			public bool AssertCorrectThread() => Connection.AssertCorrectThread();

			#region IDbCommand
			#pragma warning disable CS8769
			string IDbCommand.CommandText
			{
				get => Sql[NextStmtIndex];
				set => throw new NotSupportedException();
			}
			int IDbCommand.CommandTimeout
			{
				get => throw new NotImplementedException();
				set => throw new NotImplementedException();
			}
			void IDbCommand.Prepare() => PrepareNext();
			IDbDataParameter IDbCommand.CreateParameter() => new Parameter();
			IDataParameterCollection IDbCommand.Parameters => Parameters;
			int IDbCommand.ExecuteNonQuery() => Execute();
			object IDbCommand.ExecuteScalar() => Scalar<object>();
			IDbTransaction IDbCommand.Transaction
			{
				get => Transaction!;
				set => Transaction = (Transaction?)value!;
			}
			IDataReader IDbCommand.ExecuteReader() => ExecuteQuery();
			void IDbCommand.Cancel() => throw new NotSupportedException();
			IDataReader IDbCommand.ExecuteReader(CommandBehavior behavior)
			{
				throw new NotImplementedException();
			}
			CommandType IDbCommand.CommandType
			{
				get => throw new NotImplementedException();
				set => throw new NotImplementedException();
			}
			IDbConnection IDbCommand.Connection
			{
				get => Connection;
				set => throw new NotSupportedException();
			}
			UpdateRowSource IDbCommand.UpdatedRowSource
			{
				get => throw new NotImplementedException();
				set => throw new NotImplementedException();
			}
			#pragma warning restore CS8769
			#endregion
		}
	}
}
