using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Text;
using Dapper;
using EweLink;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace SolarHotWater
{
	public sealed class History :IDisposable
	{
		/// <summary>Time Zero</summary>
		public static readonly DateTimeOffset Epoch = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

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

		/// <summary>Return the combined consumption data</summary>
		public IEnumerable<ConsumptionRecord> Consumption(DateTimeOffset? beg = null, DateTimeOffset? end = null)
		{
			return DB.Query<ConsumptionRecord>(new CommandDefinition(
				$"select * from {Table.Consumption}\n" +
				$"where 1\n" +
				(beg != null ? $"and [{nameof(ConsumptionRecord.Timestamp)}] >= @beg\n" : string.Empty) +
				(end != null ? $"and [{nameof(ConsumptionRecord.Timestamp)}] <  @end\n" : string.Empty) +
				$"order by [{nameof(ConsumptionRecord.Timestamp)}]\n" +
				$"",
				new
				{
					beg = beg?.ToUnixTimeSeconds(),
					end = end?.ToUnixTimeSeconds(),
				},
				flags: CommandFlags.Buffered));
		}

		/// <summary>Return the consumer data</summary>
		public IEnumerable<ConsumerRecord> Consumer(string? device_id = null, bool? state = null, DateTimeOffset? beg = null, DateTimeOffset? end = null)
		{
			return DB.Query<ConsumerRecord>(new CommandDefinition(
				$"select * from {Table.Consumer}\n" +
				$"where 1\n" +
				(device_id != null ? $"and [{nameof(ConsumerRecord.DeviceID)}] = @device_id\n" : string.Empty) +
				(state != null ? $"and [{nameof(ConsumerRecord.On)}] = @state\n" : string.Empty) +
				(beg != null ? $"and [{nameof(ConsumerRecord.Timestamp)}] >= @beg\n" : string.Empty) +
				(end != null ? $"and [{nameof(ConsumerRecord.Timestamp)}] <  @end\n" : string.Empty) +
				$"order by [{nameof(ConsumerRecord.Timestamp)}]\n" +
				$"",
				new
				{
					device_id,
					state,
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
			DataAdded?.Invoke(this, new DataAddedEventArgs(solar: rec));
		}

		/// <summary>Add combined consumption to the history</summary>
		public void Add(IEnumerable<Consumer> consumers)
		{
			var rec = new ConsumptionRecord
			{
				Power = consumers.Sum(c => c.On ? c.Power ?? c.RequiredPower : 0.0),
				Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
			};
			var sql = $"insert into {Table.Consumption} (\n" +
				$"  [{nameof(ConsumptionRecord.Power)}],\n" +
				$"  [{nameof(ConsumptionRecord.Timestamp)}]\n" +
				$") values (\n" +
				$"  @Power, @Timestamp\n" +
				$")\n";

			DB.Execute(sql, rec);
			DataAdded?.Invoke(this, new DataAddedEventArgs(consumption: rec));
		}

		/// <summary>Add a record of consumer power usage to the history</summary>
		public void Add(EweSwitch sw)
		{
			var rec = new ConsumerRecord
			{
				DeviceID = sw.DeviceID,
				Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
				On = sw.State == EweSwitch.ESwitchState.On ? 1 : 0,
				Power = sw.Power,
				Voltage = sw.Voltage,
				Current = sw.Current,
			};
			var sql = $"insert into {Table.Consumer} (\n" +
				$"  [{nameof(ConsumerRecord.DeviceID)}],\n" +
				$"  [{nameof(ConsumerRecord.Timestamp)}],\n" +
				$"  [{nameof(ConsumerRecord.On)}],\n" +
				$"  [{nameof(ConsumerRecord.Power)}],\n" +
				$"  [{nameof(ConsumerRecord.Voltage)}],\n" +
				$"  [{nameof(ConsumerRecord.Current)}]\n" +
				$") values (\n" +
				$"  @DeviceID, @Timestamp, @On, @Power, @Voltage, @Current\n" +
				$")\n";

			DB.Execute(sql, rec);
			DataAdded?.Invoke(this, new DataAddedEventArgs(consumer: rec));
		}

		/// <summary>Raised when new data is added to the history</summary>
		public event EventHandler<DataAddedEventArgs>? DataAdded;

		/// <summary></summary>
		private static class Table
		{
			public const string SolarOutput = "SolarOutput";
			public const string Consumption = "Consumption";
			public const string Consumer = "Consumer";
		}
		public class SolarOutputRecord
		{
			/// <summary>The power produced by the solar panels in kWatts</summary>
			public double Output { get; set; }

			/// <summary>The unix time that the output was sampled at</summary>
			public long Timestamp { get; set; }
		}
		public class ConsumptionRecord
		{
			/// <summary>The power consumed by all consumers (in kWatts)</summary>
			public double Power { get; set; }

			/// <summary>The unix time of the sample</summary>
			public long Timestamp { get; set; }
		}
		public class ConsumerRecord
		{
			/// <summary>The device GUID</summary>
			public string DeviceID { get; set; } = string.Empty;

			/// <summary>The unix time that the output was sampled at</summary>
			public long Timestamp { get; set; }

			/// <summary>True if the consumer is on</summary>
			public int On { get; set; }

			/// <summary>The power consumption</summary>
			public double? Power { get; set; }

			/// <summary></summary>
			public double? Voltage { get; set; }

			/// <summary></summary>
			public double? Current { get; set; }
		}
		public class DataAddedEventArgs :EventArgs
		{
			public DataAddedEventArgs(SolarOutputRecord? solar = null, ConsumptionRecord? consumption = null, ConsumerRecord? consumer = null)
			{
				Solar = solar;
				Consumption = consumption;
				Consumer = consumer;
			}

			/// <summary>The new solar data record</summary>
			public SolarOutputRecord? Solar { get; }

			/// <summary>The new consumption record</summary>
			public ConsumptionRecord? Consumption { get; }

			/// <summary>The new consumer data record</summary>
			public ConsumerRecord? Consumer { get; }
		}
	}
}
