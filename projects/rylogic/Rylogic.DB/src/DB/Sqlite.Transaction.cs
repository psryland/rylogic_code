using System;
using System.Data;
using System.Diagnostics;

namespace Rylogic.DB
{
	public static partial class Sqlite
	{
		/// <summary>A transaction scope</summary>
		public sealed class Transaction : IDbTransaction, IDisposable
		{
			/// <summary>Typically created using the Database.NewTransaction() method</summary>
			internal Transaction(Connection db, IsolationLevel il, Action? on_dispose)
			{
				Debug.Assert(db.AssertCorrectThread());

				Connection = db;
				m_disposed = on_dispose;
				m_completed = false;

				// Begin the transaction
				Connection.Cmd("begin transaction").Execute();
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
					m_disposed?.Invoke();
				}
			}
			private readonly Action? m_disposed;
			private bool m_completed;

			/// <summary>The database connection</summary>
			public Connection Connection { get; }

			/// <summary>Commit changes to the database</summary>
			public void Commit()
			{
				if (m_completed)
					throw new SqliteException(EResult.Misuse, "Transaction already completed", string.Empty);

				Connection.Cmd("commit").Execute();
				m_completed = true;
			}

			/// <summary>Abort changes</summary>
			public void Rollback()
			{
				if (m_completed)
					throw new SqliteException(EResult.Misuse, "Transaction already completed", string.Empty);

				Connection.Cmd("rollback").Execute();
				m_completed = true;
			}

			#region IDbTransaction
			void IDbTransaction.Commit() => Commit();
			void IDbTransaction.Rollback() => Rollback();
			IDbConnection IDbTransaction.Connection => Connection;
			IsolationLevel IDbTransaction.IsolationLevel => throw new NotImplementedException();
			void IDisposable.Dispose() => Dispose();
			#endregion
		}
	}
}
