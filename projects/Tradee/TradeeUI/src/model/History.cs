using System;
using System.Diagnostics;
using System.IO;
using pr.common;
using pr.db;
using pr.extn;
using pr.util;

namespace Tradee
{
	/// <summary>
	/// Records all trades and the orders they involved</summary>
	public class History :IDisposable
	{
		/// <summary>A database of the trade history</summary>
		private Sqlite.Database m_db;

		public History(MainModel model)
		{
			Model = model;

			// Ensure the acct data cache directory exists
			if (!Path_.DirExists(Model.Settings.General.AcctDataCacheDir))
				Directory.CreateDirectory(Model.Settings.General.AcctDataCacheDir);
		}
		public virtual void Dispose()
		{
			Model = null;
		}

		/// <summary>Application settings</summary>
		public Settings Settings
		{
			get { return Model.Settings; }
		}

		/// <summary>Application logic</summary>
		public MainModel Model
		{
			[DebuggerStepThrough] get { return m_model; }
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.Acct.AccountChanged  -= HandleAcctChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.Acct.AccountChanged  += HandleAcctChanged;
				}
				HandleAcctChanged();
			}
		}
		private MainModel m_model;

		/// <summary>Called when a new user account has been assigned</summary>
		private void HandleAcctChanged(object sender = null, EventArgs e = null)
		{
			// Ensure closed
			Util.Dispose(ref m_db);

			// If there is no account id, don't open a database
			if (Model == null || !Model.Acct.AccountId.HasValue())
				return;

			// Open a database of the trade history and other user account related info
			var filepath = Path_.CombinePath(Settings.General.AcctDataCacheDir, "Acct_{0}.db".Fmt(Model.Acct.AccountId));
			m_db = new Sqlite.Database(filepath);

			// Tweak some DB settings for performance
			m_db.Execute(Sqlite.Sql("PRAGMA synchronous = OFF"));
			m_db.Execute(Sqlite.Sql("PRAGMA journal_mode = MEMORY"));

			// Ensure the table of trades exists
			var sql = Sqlite.Sql("create table if not exists ",Table.TradeHistory," (\n",
				"[",nameof(Trade.Id        ),"] text primary key,\n",
				"[",nameof(Trade.SymbolCode),"] text)");
			m_db.Execute(sql);

			// Ensure the table of orders exists
			sql = Sqlite.Sql("create table if not exists ",Table.Orders," (\n",
				"[",nameof(Order.Id        ),"] integer primary key,\n",
				"[",      ("TradeId"       ),"] text,\n",
				"[",nameof(Order.SymbolCode),"] text,\n",
				"[",nameof(Order.TradeType ),"] integer)");
			m_db.Execute(sql);
		}

		private static class Table
		{
			public const string TradeHistory = "TradeHistory";
			public const string Orders       = "Orders";
		}
	}
}
