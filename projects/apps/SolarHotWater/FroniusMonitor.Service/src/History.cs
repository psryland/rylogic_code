using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Text;
using Dapper;
using Rylogic.Common;
using Rylogic.Utility;
using SolarHotWater.Common;

namespace FroniusMonitor.Service
{
	public sealed class History :IDisposable
	{
		public History(string root_directory)
		{
			var filepath = Path.Combine(root_directory, "fronius.db");
			Path_.CreateDirs(Path_.Directory(filepath));

			var connection_string = $"Data Source={filepath};Version=3;journal mode=Memory;synchronous=Off";
			DB = new SQLiteConnection(connection_string);

			InitDBTables();
		}
		public void Dispose()
		{
			DB = null!;
		}

		/// <summary>Time Zero</summary>
		public static readonly DateTimeOffset Epoch = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

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
			using var sql_stream = GetType().Assembly.GetManifestResourceStream($"FroniusMonitor.Service.res.Setup.sql") ?? throw new Exception("History DB setup script not available");
			using var sr = new StreamReader(sql_stream, Encoding.UTF8);
			DB.Execute(sr.ReadToEnd());
		}

		/// <summary>Return the solar data</summary>
		public IEnumerable<SolarOutputRecord> Solar(DateTimeOffset? beg = null, DateTimeOffset? end = null)
		{
			return DB.Query<SolarOutputRecord>(new CommandDefinition(
				$"select * from {Table.SolarOutput}\n" +
				$"where 1\n" +
				(beg != null ? $"and [{nameof(SolarOutputRecord.Timestamp)}] >= @beg\n" : string.Empty) +
				(end != null ? $"and [{nameof(SolarOutputRecord.Timestamp)}] <  @end\n" : string.Empty) +
				$"order by [{nameof(SolarOutputRecord.Timestamp)}]\n" +
				$"",
				new
				{
					beg = beg?.ToUnixTimeSeconds(),
					end = end?.ToUnixTimeSeconds(),
				},
				flags: CommandFlags.Buffered));
		}

		/// <summary>Add solar data to the history</summary>
		public void Add(SolarData solar)
		{
			var rec = new SolarOutputRecord
			{
				Output = solar.CurrentPower,
				Timestamp = solar.Timestamp.ToUnixTimeSeconds()
			};
			var sql = $"insert into {Table.SolarOutput} (\n" +
				$"  [{nameof(SolarOutputRecord.Output)}],\n" +
				$"  [{nameof(SolarOutputRecord.Timestamp)}]\n" +
				$") values (\n" +
				$"  @Output, @Timestamp\n" +
				$")\n";

			DB.Execute(sql, rec);
		}

		/// <summary></summary>
		private static class Table
		{
			public const string SolarOutput = "SolarOutput";
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
