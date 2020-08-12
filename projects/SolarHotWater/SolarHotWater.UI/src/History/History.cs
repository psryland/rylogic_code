using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Documents;
using Dapper;
//using Dapper;
using Newtonsoft.Json.Bson;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace SolarHotWater
{
	public sealed class History :IDisposable
	{
		public History()
		{
			Path_.CreateDirs(Path_.Directory(Filepath));
			DB = new SQLiteConnection(DBConnectionString);
			InitDBTables();
		}
		public void Dispose()
		{
			DB = null!;
		}

		/// <summary>History data database</summary>
		public static string Filepath => Util.ResolveUserDocumentsPath("Rylogic", "SolarHotWater", "history.db");
		private static string DBConnectionString = $"Data Source={Filepath};Version=3;journal mode=Memory;synchronous=Off";

		/// <summary>Database access</summary>
		private SQLiteConnection DB
		{
			[DebuggerStepThrough] get => m_db;
			set
			{
				if (m_db == value) return;
				if (m_db != null)
				{
					m_db.Close();
					Util.Dispose(ref m_db!);
				}
				m_db = value;
				if (m_db != null)
				{
					m_db.Open();
				}
			}
		}
		private SQLiteConnection m_db = null!;

		/// <summary>Initialise the DB tables</summary>
		private void InitDBTables()
		{
			using var sql_stream = GetType().Assembly.GetManifestResourceStream($"SolarHotWater.src.History.Setup.sql") ?? throw new Exception("History DB setup script not available");
			using var sr = new StreamReader(sql_stream, Encoding.UTF8);
			DB.Execute(sr.ReadToEnd());
		}

		/// <summary>Return the solar data</summary>
		public IEnumerable<SolarOutputRecord> Solar()
		{
			return DB.Query<SolarOutputRecord>(new CommandDefinition(
				$"select * from {Table.SolarOutput}\n" +
				$"",
				//$"where 1\n" +
				//(match_name != null ? $"and [{nameof(StarSystem.Name)}] like @match_name\n" : string.Empty) +
				//(ignore_permitted == true ? $"and [{nameof(StarSystem.NeedPermit)}] = 0\n" : string.Empty) +
				//(max_count != null ? $"limit @max_count\n" : string.Empty),
				//new
				//{
				//	match_name = $"{match_name}%",
				//	max_count,
				//},
				flags: CommandFlags.Buffered));
		}

		/// <summary>Add solar data to the history</summary>
		public void Add(SolarData solar)
		{
			var sql = $"insert into {Table.SolarOutput} (\n" +
				$"  [{nameof(SolarOutputRecord.Output)}],\n" +
				$"  [{nameof(SolarOutputRecord.Timestamp)}]\n" +
				$") values (\n" +
				$"  @output, @timestamp\n" +
				$")\n";

			DB.Execute(sql, new
			{
				output = solar.CurrentPower,
				timestamp = solar.Timestamp.ToUnixTimeSeconds()
			});
		}

		/// <summary></summary>
		private static class Table
		{
			public const string SolarOutput = "SolarOutput";
			public const string Consumer = "Consumer";
		}
		public class SolarOutputRecord
		{
			/// <summary>The power produced by the solar panels in kWatts</summary>
			public double Output { get; set; }

			/// <summary>The unix time that the output was sampled at</summary>
			public long Timestamp { get; set; }
		}
	}
}
